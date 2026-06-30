using System;
using UnityEngine;

namespace RevolutionIdleAP;

// Applies filler/trap effects to the live game. Effects are queued from the AP background thread and
// applied on the main thread against GameData. Score effects scale with current income, so they stay
// meaningful at any stage and never corrupt the save.
//   Score Boost (filler)   -> add ~60s of income to score
//   Income Jackpot (filler)-> add IncomeJackpotSeconds of income to score
//   Generator Boost (filler)-> every base generator gains GeneratorBoostLevels (capped at its max)
//   Overdrive (filler)     -> Time.timeScale = 2 for OverdriveSeconds
//   Slowdown Trap          -> remove ~120s of income from score (clamped at 0)
//   Freeze Trap            -> Time.timeScale = 0 for N seconds
//   Lag Trap               -> Time.timeScale = 0.5 for N seconds
//   Generator Drain Trap   -> every base generator loses GeneratorDrainLevels (clamped at 0)
public static class ItemEffects
{
    private const double BoostSeconds = 60.0;
    private const double TrapSeconds = 120.0;

    private static int _pendingBoosts;
    private static int _pendingJackpots;
    private static int _pendingGenBoosts;
    private static int _pendingTraps;
    private static int _pendingDrains;

    // Pending timeScale effect (freeze / lag / overdrive). Last one queued wins (replace semantics).
    private static bool _pendTimeScaleSet;
    private static float _pendTimeScaleSeconds;
    private static float _pendTimeScaleFactor = 1f;
    private static readonly object _lock = new();

    // Active timeScale-effect state (main thread only).
    private static bool _tsActive;
    private static float _tsRemaining;
    private static float _tsFactor = 1f;
    private static float _savedTimeScale = 1f;

    public static void QueueScoreBoost() { lock (_lock) { _pendingBoosts++; } }
    public static void QueueIncomeJackpot() { lock (_lock) { _pendingJackpots++; } }
    public static void QueueGeneratorBoost() { lock (_lock) { _pendingGenBoosts++; } }
    public static void QueueTrap() { lock (_lock) { _pendingTraps++; } }
    public static void QueueGeneratorDrain() { lock (_lock) { _pendingDrains++; } }

    // Queue a timeScale effect: `factor` is the target scale (0 freeze, 0.5 lag, 2 overdrive) held for
    // `seconds`. The most recent one wins (replaces any in progress) — predictable and supports any factor.
    public static void QueueTimeScale(float seconds, float factor)
    {
        lock (_lock) { _pendTimeScaleSeconds = seconds; _pendTimeScaleFactor = factor; _pendTimeScaleSet = true; }
    }

    // Called EVERY FRAME from RevApTicker.Update (runs even at timeScale 0). Drives the timeScale
    // effect off unscaled time so it always restores, and applies Time.timeScale itself.
    public static void UpdateTimeEffects()
    {
        bool set; float seconds; float factor;
        lock (_lock)
        {
            set = _pendTimeScaleSet; _pendTimeScaleSet = false;
            seconds = _pendTimeScaleSeconds; factor = _pendTimeScaleFactor;
        }

        if (set)
        {
            if (!_tsActive) _savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            _tsActive = true;
            _tsFactor = factor;
            _tsRemaining = seconds; // last-wins: replace remaining time
            Plugin.Logger.LogInfo($"[AP] timeScale effect: x{_tsFactor} for {_tsRemaining:0}s.");
        }

        if (_tsActive)
        {
            Time.timeScale = _tsFactor;
            _tsRemaining -= Time.unscaledDeltaTime;
            if (_tsRemaining <= 0f)
            {
                Time.timeScale = _savedTimeScale;
                _tsActive = false; _tsFactor = 1f; _tsRemaining = 0f;
                Plugin.Logger.LogInfo("[AP] timeScale effect ended; restored.");
            }
        }
    }

    // Called on the main thread (~1/sec) with the live GameData. Applies one-shot effects.
    public static void ApplyPending(GameData data)
    {
        int boosts, jackpots, genBoosts, traps, drains;
        lock (_lock)
        {
            boosts = _pendingBoosts; _pendingBoosts = 0;
            jackpots = _pendingJackpots; _pendingJackpots = 0;
            genBoosts = _pendingGenBoosts; _pendingGenBoosts = 0;
            traps = _pendingTraps; _pendingTraps = 0;
            drains = _pendingDrains; _pendingDrains = 0;
        }
        if (boosts == 0 && jackpots == 0 && genBoosts == 0 && traps == 0 && drains == 0) return;

        try
        {
            if (boosts > 0)
            {
                BigDouble gain = data.income * (BigDouble)(BoostSeconds * boosts);
                data.score = data.score + gain;
                Plugin.Logger.LogInfo($"[AP] Score Boost x{boosts}: +{BoostSeconds * boosts:0}s of income.");
            }
            if (jackpots > 0)
            {
                double secs = (Plugin.Client?.IncomeJackpotSeconds ?? 600) * (double)jackpots;
                data.score = data.score + data.income * (BigDouble)secs;
                Plugin.Logger.LogInfo($"[AP] Income Jackpot x{jackpots}: +{secs:0}s of income.");
            }
            if (genBoosts > 0)
            {
                int lvls = (Plugin.Client?.GeneratorBoostLevels ?? 20) * genBoosts;
                AdjustGenerators(data, lvls);
                Plugin.Logger.LogInfo($"[AP] Generator Boost x{genBoosts}: +{lvls} levels on each generator.");
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
                AdjustGenerators(data, -lvls);
                Plugin.Logger.LogInfo($"[AP] Generator Drain x{drains}: -{lvls} levels on each generator.");
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError("[AP] item effect apply error: " + e.Message);
        }
    }

    // Add `delta` levels to every base generator, clamped to [0, maxAmount].
    private static void AdjustGenerators(GameData data, int delta)
    {
        var gens = data.infinity?.generators;
        if (gens == null) return;
        int n = gens.Count;
        for (int i = 0; i < n; i++)
        {
            var g = gens[i];
            if (g == null) continue;
            double a = g.amount + delta;
            if (a < 0.0) a = 0.0;
            if (g.maxAmount > 0.0 && a > g.maxAmount) a = g.maxAmount;
            g.amount = a;
        }
    }
}
