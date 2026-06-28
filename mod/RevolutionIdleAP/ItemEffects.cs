using System;

namespace RevolutionIdleAP;

// Applies filler/trap effects to the live game. Effects are queued from the AP background thread and
// applied on the main thread (from Plugin.Tick) against GameData. Both effects scale with the
// player's current income, so they stay meaningful at any stage and never corrupt the save.
//   Score Boost (filler) -> add ~60s of income to score
//   Slowdown Trap        -> remove ~120s of income from score (clamped at 0)
public static class ItemEffects
{
    private const double BoostSeconds = 60.0;
    private const double TrapSeconds = 120.0;

    private static int _pendingBoosts;
    private static int _pendingTraps;
    private static readonly object _lock = new();

    public static void QueueScoreBoost() { lock (_lock) { _pendingBoosts++; } }
    public static void QueueTrap() { lock (_lock) { _pendingTraps++; } }

    // Called on the main thread (~1/sec) with the live GameData.
    public static void ApplyPending(GameData data)
    {
        int boosts, traps;
        lock (_lock)
        {
            boosts = _pendingBoosts; _pendingBoosts = 0;
            traps = _pendingTraps; _pendingTraps = 0;
        }
        if (boosts == 0 && traps == 0) return;

        try
        {
            if (boosts > 0)
            {
                BigDouble gain = data.income * (BigDouble)(BoostSeconds * boosts);
                data.score = data.score + gain;
                Plugin.Logger.LogInfo($"[AP] Score Boost x{boosts}: +{BoostSeconds * boosts:0}s of income.");
            }
            if (traps > 0)
            {
                BigDouble loss = data.income * (BigDouble)(TrapSeconds * traps);
                data.score = (loss > data.score) ? (BigDouble)0.0 : (data.score - loss);
                Plugin.Logger.LogInfo($"[AP] Slowdown Trap x{traps}: -{TrapSeconds * traps:0}s of income.");
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError("[AP] item effect apply error: " + e.Message);
        }
    }
}
