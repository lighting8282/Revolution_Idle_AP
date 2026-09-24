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
        {
            Plugin.ShowFeed = !Plugin.ShowFeed;
            if (Plugin.ShowFeed)
            {
                // Each line normally expires after FeedSeconds, so toggling the feed on during a
                // quiet moment used to draw nothing at all — indistinguishable from F2 being
                // broken. Reveal recent history for a few seconds, and always confirm the toggle.
                _revealUntil = DateTime.UtcNow.AddSeconds(RevealSeconds);
                ApFeed.Add("AP message feed ON (F2 to hide).", new Color(0.7f, 0.9f, 1f));
            }
        }

        // Drive freeze/lag traps off unscaled time so they run (and self-restore) even at timeScale 0.
        try { ItemEffects.UpdateTimeEffects(); }
        catch (Exception e) { Plugin.Logger.LogError("[AP] time-effect error: " + e.Message); }

        // One-shot state dump — runs independently of any AP connection.
        try { DiagnosticDump.TryRunOnce(); }
        catch (Exception e) { Plugin.Logger.LogError("[AP] diagnostic error: " + e.Message); }

        // F3: dump on demand. Use this once your save is actually loaded — the automatic launch
        // dump fires before the save deserializes, so its flags/progress read as empty.
        if (Input.GetKeyDown(KeyCode.F3))
        {
            try { DiagnosticDump.RunNow(); }
            catch (Exception e) { Plugin.Logger.LogError("[AP] diagnostic error: " + e.Message); }
        }

        _timer += Time.deltaTime;
        if (_timer < 1f) return;
        _timer = 0f;
        try { Plugin.Tick(); }
        catch (Exception e) { Plugin.Logger.LogError("[AP] tick error: " + e.Message); }
    }

    private GUIStyle? _feedStyle;
    private GUIStyle? _warnStyle;
    private const float FeedSeconds = 12f;  // how long each message stays on screen
    private const int FeedMaxLines = 10;
    private const double RevealSeconds = 10.0;  // after F2-on, show recent history even if expired
    private DateTime _revealUntil = DateTime.MinValue;

    // Kept as constants so the height measured is exactly the text drawn.
    private const string ApModeWarning =
        "AP Mode is required before connecting.\n\nConnecting from your normal save would send its "
        + "existing progress to the multiworld as checks. Use the switch above first.";
    private const string NotVerifiedWarning = "Save not verified — nothing is being sent (see log).";

    public void OnGUI()
    {
        if (Plugin.ShowFeed)
        {
            try { DrawFeed(); }
            catch (Exception e) { Plugin.Logger.LogError("[AP] feed draw error: " + e.Message); }
        }

        if (!Plugin.ShowMenu) return;

        const float x = 24f, top = 24f, w = 360f, pad = 10f, fh = 24f, lh = 18f, gap = 6f;
        bool allowed = Plugin.ApModeOk;  // pre-connect: only the mode half is knowable here
        bool holdingSend = Plugin.Client?.Connected == true && Plugin.VerifySaveIdentity && !Plugin.SaveVerified;

        float ix = x + pad, iw = w - pad * 2f;
        _warnStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true, fontStyle = FontStyle.Bold };

        // Measure the wrapped warnings rather than guessing a line count: these strings wrap to
        // different heights depending on width and font, and a fixed allowance silently clips the
        // last line (and the box) right where the reader most needs it.
        float warnH = allowed ? 0f : _warnStyle.CalcHeight(new GUIContent(ApModeWarning), iw);
        float holdH = holdingSend ? _warnStyle.CalcHeight(new GUIContent(NotVerifiedWarning), iw) : 0f;
        string statusText = "Status: " + (Plugin.Client?.Status ?? "-");
        float statusH = _warnStyle.CalcHeight(new GUIContent(statusText), iw);

        // Same increments the layout below walks through, so the box always fits its contents.
        float boxH = 30f                                        // title bar
                   + (lh + gap)                                 // hotkey hint
                   + lh + fh + gap + 2f                         // AP Mode label + toggle button
                   + 4f * lh + 3f * (fh + gap) + (fh + 8f)      // 4 field labels + 4 fields
                   + (allowed ? fh + 12f : warnH + gap)         // Connect button OR the warning
                   + (holdingSend ? holdH + 4f : 0f)
                   + statusH + pad;
        GUI.Box(new Rect(x, top, w, boxH), "Archipelago Connection");

        float y = top + 30f;

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

        // Connecting from a normal save would send that save's progress into the multiworld, so the
        // Connect button is withheld until AP Mode is on rather than failing after the fact.
        if (!allowed)
        {
            var prev = GUI.color;
            GUI.color = new Color(1f, 0.55f, 0.55f);
            GUI.Label(new Rect(ix, y, iw, warnH), ApModeWarning, _warnStyle);
            GUI.color = prev;
            y += warnH + gap;
        }
        else
        {
            bool connected = Plugin.Client != null && Plugin.Client.Connected;
            if (GUI.Button(new Rect(ix, y, iw, fh + 4f), connected ? "Reconnect" : "Connect"))
                Plugin.ConnectFromMenu();
            y += fh + 12f;
        }

        // Surface a held-back send: connected but the loaded save hasn't been verified as this
        // seed's AP save, so nothing is going out. Silent refusal would look like a dead connection.
        if (holdingSend)
        {
            var prev = GUI.color;
            GUI.color = new Color(1f, 0.8f, 0.4f);
            GUI.Label(new Rect(ix, y, iw, holdH), NotVerifiedWarning, _warnStyle);
            GUI.color = prev;
            y += holdH + 4f;
        }

        GUI.Label(new Rect(ix, y, iw, statusH), statusText);
    }

    // Top-right feed of recent AP messages (checks given/received, joins, hints, chat...). Each line
    // fades out after FeedSeconds. Drawn independently of the F1 menu.
    private void DrawFeed()
    {
        var all = ApFeed.Snapshot();
        if (all.Count == 0) return;

        _feedStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };

        var now = DateTime.UtcNow;
        bool revealing = now < _revealUntil;
        var visible = new System.Collections.Generic.List<ApFeed.Entry>();
        for (int i = all.Count - 1; i >= 0 && visible.Count < FeedMaxLines; i--)
        {
            // While revealing, take the most recent lines regardless of age so F2 actually shows
            // you what happened; otherwise only lines still within their display window.
            if (revealing || (now - all[i].Time).TotalSeconds <= FeedSeconds) visible.Add(all[i]);
            else break;   // older entries can only be older still
        }
        if (visible.Count == 0) return;
        visible.Reverse(); // oldest at top, newest at bottom

        const float w = 460f, pad = 8f, margin = 12f;

        var heights = new float[visible.Count];
        float total = 0f;
        for (int i = 0; i < visible.Count; i++)
        {
            heights[i] = _feedStyle.CalcHeight(new GUIContent(visible[i].Text), w - pad * 2f);
            total += heights[i] + 2f;
        }

        float boxH = total + pad * 2f;
        float x = margin;                                  // bottom-left
        float y = Screen.height - boxH - margin;

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.Box(new Rect(x, y, w, boxH), GUIContent.none);

        float yy = y + pad;
        for (int i = 0; i < visible.Count; i++)
        {
            double age = (now - visible[i].Time).TotalSeconds;
            // While revealing, expired lines are deliberately on screen — the normal fade would
            // compute alpha 0 for them and draw an empty box.
            float alpha = revealing ? 1f
                        : age > FeedSeconds - 3.0 ? Mathf.Clamp01((float)(FeedSeconds - age) / 3f) : 1f;
            var c = visible[i].Color; c.a = alpha;
            GUI.color = c;
            GUI.Label(new Rect(x + pad, yy, w - pad * 2f, heights[i]), visible[i].Text, _feedStyle);
            yy += heights[i] + 2f;
        }
        GUI.color = Color.white;
    }
}
