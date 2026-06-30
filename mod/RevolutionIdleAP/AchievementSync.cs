using System;
using System.Collections.Generic;

namespace RevolutionIdleAP;

// Marks achievements whose AP location is already checked on the server as unlocked in the game's own
// data (so the in-game achievement panel reflects AP progress), WITHOUT calling UnlockAchievement —
// i.e. no reward/bonus is granted. Game ids are queued from the network thread and applied on the
// main thread (Plugin.Tick) against GameData.unlockedAch / achByte.
public static class AchievementSync
{
    private static readonly HashSet<int> _pending = new();
    private static readonly object _lock = new();

    public static void QueueMark(int gameAchId)
    {
        lock (_lock) { _pending.Add(gameAchId); }
    }

    public static void ApplyPending(GameData data)
    {
        int[] ids;
        lock (_lock)
        {
            if (_pending.Count == 0) return;
            ids = new int[_pending.Count];
            _pending.CopyTo(ids);
            _pending.Clear();
        }

        try
        {
            var list = data.unlockedAch;
            var ab = data.achByte;
            int marked = 0;
            foreach (int id in ids)
            {
                bool changed = false;
                if (ab != null && id >= 0 && id < ab.Length && ab[id] != 1) { ab[id] = 1; changed = true; }
                if (list != null && !list.Contains(id)) { list.Add(id); changed = true; }
                if (changed) marked++;
            }
            if (marked > 0)
                Plugin.Logger.LogInfo($"[AP] marked {marked} achievement(s) as completed in-game (from AP checks).");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError("[AP] achievement sync error: " + e.Message);
        }
    }
}
