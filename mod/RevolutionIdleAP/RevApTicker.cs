using System;
using UnityEngine;

namespace RevolutionIdleAP;

// Injected MonoBehaviour: drives the ~1/sec main-thread tick, toggles the connection menu (F1),
// and draws that menu via IMGUI.
public class RevApTicker : MonoBehaviour
{
    public RevApTicker(IntPtr ptr) : base(ptr) { }

    private float _timer;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            Plugin.ShowMenu = !Plugin.ShowMenu;
        if (Input.GetKeyDown(KeyCode.F2))
            Plugin.ShowFeed = !Plugin.ShowFeed;

        // Drive freeze/lag traps off unscaled time so they run (and self-restore) even at timeScale 0.
        try { ItemEffects.UpdateTimeEffects(); }
        catch (Exception e) { Plugin.Logger.LogError("[AP] time-effect error: " + e.Message); }

        _timer += Time.deltaTime;
        if (_timer < 1f) return;
        _timer = 0f;
        try { Plugin.Tick(); }
        catch (Exception e) { Plugin.Logger.LogError("[AP] tick error: " + e.Message); }
    }

    private GUIStyle _feedStyle;
    private const float FeedSeconds = 12f;  // how long each message stays on screen
    private const int FeedMaxLines = 10;

    public void OnGUI()
    {
        if (Plugin.ShowFeed)
        {
            try { DrawFeed(); }
            catch (Exception e) { Plugin.Logger.LogError("[AP] feed draw error: " + e.Message); }
        }

        if (!Plugin.ShowMenu) return;

        const float x = 24f, top = 24f, w = 360f, pad = 10f, fh = 24f, lh = 18f, gap = 6f;
        GUI.Box(new Rect(x, top, w, 372f), "Archipelago Connection");

        float ix = x + pad, iw = w - pad * 2f, y = top + 30f;

        GUI.Label(new Rect(ix, y, iw, lh), "F1: this menu   F2: message feed"); y += lh + gap;

        // AP Mode toggle — flips offline/isolated-save mode and restarts the game.
        GUI.Label(new Rect(ix, y, iw, lh), Plugin.APMode
            ? "AP Mode: ON (offline, separate AP save)"
            : "AP Mode: OFF (normal cloud save)"); y += lh;
        if (GUI.Button(new Rect(ix, y, iw, fh), Plugin.APMode ? "Switch to Normal (restarts game)" : "Switch to AP (restarts game)"))
            Plugin.ToggleApModeAndRestart();
        y += fh + gap + 2f;

        GUI.Label(new Rect(ix, y, iw, lh), "Hostname:"); y += lh;
        Plugin.MenuHost = GUI.TextField(new Rect(ix, y, iw, fh), Plugin.MenuHost); y += fh + gap;

        GUI.Label(new Rect(ix, y, iw, lh), "Port:"); y += lh;
        Plugin.MenuPort = GUI.TextField(new Rect(ix, y, iw, fh), Plugin.MenuPort); y += fh + gap;

        GUI.Label(new Rect(ix, y, iw, lh), "Slot Name:"); y += lh;
        Plugin.MenuSlot = GUI.TextField(new Rect(ix, y, iw, fh), Plugin.MenuSlot); y += fh + gap;

        GUI.Label(new Rect(ix, y, iw, lh), "Password (optional):"); y += lh;
        Plugin.MenuPass = GUI.PasswordField(new Rect(ix, y, iw, fh), Plugin.MenuPass, '*'); y += fh + 8f;

        bool connected = Plugin.Client != null && Plugin.Client.Connected;
        if (GUI.Button(new Rect(ix, y, iw, fh + 4f), connected ? "Reconnect" : "Connect"))
            Plugin.ConnectFromMenu();
        y += fh + 12f;

        GUI.Label(new Rect(ix, y, iw, lh * 2f), "Status: " + (Plugin.Client?.Status ?? "-"));
    }

    // Top-right feed of recent AP messages (checks given/received, joins, hints, chat...). Each line
    // fades out after FeedSeconds. Drawn independently of the F1 menu.
    private void DrawFeed()
    {
        var all = ApFeed.Snapshot();
        if (all.Count == 0) return;

        _feedStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 12, alignment = TextAnchor.UpperLeft };

        var now = DateTime.UtcNow;
        var visible = new System.Collections.Generic.List<ApFeed.Entry>();
        for (int i = all.Count - 1; i >= 0 && visible.Count < FeedMaxLines; i--)
        {
            if ((now - all[i].Time).TotalSeconds <= FeedSeconds) visible.Add(all[i]);
        }
        if (visible.Count == 0) return;
        visible.Reverse(); // oldest at top, newest at bottom

        const float w = 460f, pad = 8f, margin = 12f;
        float x = Screen.width - w - margin, y = margin;

        var heights = new float[visible.Count];
        float total = 0f;
        for (int i = 0; i < visible.Count; i++)
        {
            heights[i] = _feedStyle.CalcHeight(new GUIContent(visible[i].Text), w - pad * 2f);
            total += heights[i] + 2f;
        }

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.Box(new Rect(x, y, w, total + pad * 2f), GUIContent.none);

        float yy = y + pad;
        for (int i = 0; i < visible.Count; i++)
        {
            double age = (now - visible[i].Time).TotalSeconds;
            float alpha = age > FeedSeconds - 3.0 ? Mathf.Clamp01((float)(FeedSeconds - age) / 3f) : 1f;
            var c = visible[i].Color; c.a = alpha;
            GUI.color = c;
            GUI.Label(new Rect(x + pad, yy, w - pad * 2f, heights[i]), visible[i].Text, _feedStyle);
            yy += heights[i] + 2f;
        }
        GUI.color = Color.white;
    }
}
