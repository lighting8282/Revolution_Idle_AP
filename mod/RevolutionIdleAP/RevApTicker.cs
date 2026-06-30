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

        // Drive freeze/lag traps off unscaled time so they run (and self-restore) even at timeScale 0.
        try { ItemEffects.UpdateTimeEffects(); }
        catch (Exception e) { Plugin.Logger.LogError("[AP] time-effect error: " + e.Message); }

        _timer += Time.deltaTime;
        if (_timer < 1f) return;
        _timer = 0f;
        try { Plugin.Tick(); }
        catch (Exception e) { Plugin.Logger.LogError("[AP] tick error: " + e.Message); }
    }

    public void OnGUI()
    {
        if (!Plugin.ShowMenu) return;

        const float x = 24f, top = 24f, w = 360f, pad = 10f, fh = 24f, lh = 18f, gap = 6f;
        GUI.Box(new Rect(x, top, w, 372f), "Archipelago Connection");

        float ix = x + pad, iw = w - pad * 2f, y = top + 30f;

        GUI.Label(new Rect(ix, y, iw, lh), "Press F1 to toggle this menu"); y += lh + gap;

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
}
