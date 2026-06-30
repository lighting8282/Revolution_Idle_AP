using System;
using UnityEngine;

namespace RevolutionIdleAP;

// Applies filler/trap effects to the live game. Effects are queued from the AP background thread and
// applied on the main thread against GameData. Score effects scale with the player's current income,
// so they stay meaningful at any stage and never corrupt the save.
//   Score Boost (filler)  -> add ~60s of income to score
//   Slowdown Trap         -> remove ~120s of income from score (clamped at 0)
//   Freeze Trap           -> Time.timeScale = 0 for N seconds (everything stops)
//   Lag Trap              -> Time.timeScale = 0.5 for N seconds (everything at half speed)
//   Generator Drain Trap  -> every base generator loses N levels (clamped at 0)
public static class ItemEffects
{
    private const double BoostSeconds = 60.0;
    private const double TrapSeconds = 120.0;

    private static int _pendingBoosts;
    private static int _pendingTraps;
    private static int _pendingDrains;

    // Time-effect (freeze/lag) queue. factor is the target timeScale (0 freeze, 0.5 lag).
    private static float _pendingSlowSeconds;
    private static float _pendingFactor = 1f;
    private static readonly object _lock = new();

    // Active time-effect state (main thread only).
    private static bool _slowActive;
    private static float _slowRemaining;
    private static float _slowFactor = 1f;
    private static float _savedTimeScale = 1f;

    public static void QueueScoreBoost() { lock (_lock) { _pendingBoosts++; } }
    public static void QueueTrap() { lock (_lock) { _pendingTraps++; } }
    public static void QueueGeneratorDrain() { lock (_lock) { _pendingDrains++; } }

    // Queue a timeScale effect for `seconds` at `factor` (0 = freeze, 0.5 = lag). Multiple stack:
    // durations add up and the strongest (lowest) factor wins while active.
    public static void QueueSlow(float seconds, float factor)
    {
        lock (_lock)
        {
            _pendingSlowSeconds += seconds;
            if (factor < _pendingFactor) _pendingFactor = factor;
        }
    }

    // Called EVERY FRAME from RevApTicker.Update (runs even at timeScale 0). Drives the freeze/lag
    // countdown off unscaled time so it always restores, and applies timeScale itself.
    public static void UpdateTimeEffects()
    {
        float addSec; float pendFactor;
        lock (_lock)
        {
            addSec = _pendingSlowSeconds; _pendingSlowSeconds = 0f;
            pendFactor = _pendingFactor; _pendingFactor = 1f;
        }

        if (addSec > 0f)
        {
            if (!_slowActive)
            {
                _savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                _slowActive = true;
                _slowFactor = pendFactor;
            }
            else if (pendFactor < _slowFactor)
            {
                _slowFactor = pendFactor;
            }
            _slowRemaining += addSec;
            Plugin.Logger.LogInfo($"[AP] trap: timeScale {_slowFactor} for {_slowRemaining:0}s.");
        }

        if (_slowActive)
        {
            Time.timeScale = _slowFactor;
            _slowRemaining -= Time.unscaledDeltaTime;
            if (_slowRemaining <= 0f)
            {
                Time.timeScale = _savedTimeScale;
                _slowActive = false;
                _slowFactor = 1f;
                _slowRemaining = 0f;
                Plugin.Logger.LogInfo("[AP] trap: timeScale restored.");
            }
        }
    }

    // Called on the main thread (~1/sec) with the live GameData. Applies one-shot effects.
    public static void ApplyPending(GameData data)
    {
        int boosts, traps, drains;
        lock (_lock)
        {
            boosts = _pendingBoosts; _pendingBoosts = 0;
            traps = _pendingTraps; _pendingTraps = 0;
            drains = _pendingDrains; _pendingDrains = 0;
        }
        if (boosts == 0 && traps == 0 && drains == 0) return;

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
            if (drains > 0)
            {
                int lvls = (Plugin.Client?.GeneratorDrainLevels ?? 20) * drains;
                var gens = data.infinity?.generators;
                if (gens != null)
                {
                    int n = gens.Count;
                    for (int i = 0; i < n; i++)
                    {
                        var g = gens[i];
                        if (g == null) continue;
                        double a = g.amount - lvls;
                        g.amount = a < 0.0 ? 0.0 : a;
                    }
                    Plugin.Logger.LogInfo($"[AP] Generator Drain x{drains}: -{lvls} levels on each generator.");
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError("[AP] item effect apply error: " + e.Message);
        }
    }
}
