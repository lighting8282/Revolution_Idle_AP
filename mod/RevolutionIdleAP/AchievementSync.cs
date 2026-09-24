using System;
using System.Collections.Generic;

namespace RevolutionIdleAP;

// Tracks which achievements are already checked on the AP server so the in-game achievement panel
// can show them as done.
//
// This is *presentation only* and deliberately keeps its own set instead of writing into the game's
// data. It used to set GameData.unlockedAch / achByte, which looked harmless but was not:
// GameData.TriggerMissingAchievementsAsync() reconciles unlockedAch against Steam and triggers
// whatever Steam is missing, so marking AP checks pushed hundreds of real Steam achievements onto
// the player's account. Steam achievements are account-global, are not covered by AP Mode's save
// isolation, and cannot be undone from in-game.
//
// Rule for this file: never write game state. The panel is updated by AchievementDisplayPatches
// reading IsApChecked(), and SteamAchievementGuard blocks the Steam path as a backstop.
public static class AchievementSync
{
    private static readonly HashSet<int> _checked = new();
    private static readonly object _lock = new();

    public static void QueueMark(int gameAchId)
    {
        lock (_lock)
        {
            if (_checked.Add(gameAchId))
                _dirty = true;
        }
    }

    private static bool _dirty;

    // True if this game achievement id corresponds to an AP location that is already checked.
    public static bool IsApChecked(int gameAchId)
    {
        lock (_lock) { return _checked.Contains(gameAchId); }
    }

    public static int Count
    {
        get { lock (_lock) { return _checked.Count; } }
    }

    // Called from Plugin.Tick. Nothing to apply to the save any more — just report progress once
    // per batch so the log still shows what the panel is reflecting.
    public static void ApplyPending(GameData data)
    {
        try
        {
            int total;
            lock (_lock)
            {
                if (!_dirty) return;
                _dirty = false;
                total = _checked.Count;
            }
            Plugin.Logger.LogInfo($"[AP] {total} achievement(s) shown as completed in the panel (AP checks; game/Steam state untouched).");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError("[AP] achievement sync error: " + e.Message);
        }
    }
}
