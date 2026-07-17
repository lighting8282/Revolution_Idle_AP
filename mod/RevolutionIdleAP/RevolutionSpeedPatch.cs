using HarmonyLib;

namespace RevolutionIdleAP;

// Speeds up how fast the revolutions (the circles) fill.
//
// The game recomputes each Revolution's speed every frame in
//   Revolution.Update(BigDouble speedMult, BigDouble baseMult, BigDouble lapMult,
//                     BigDouble ascendPower, int buyAmo, BigDouble score, bool isProgress)
// so scaling the incoming speedMult (arg 0) scales the resulting fill rate. Driven by the
// revolution_speed_multiplier slot_data option; 1 = untouched vanilla speed.
[HarmonyPatch(typeof(Revolution), "Update")]
public static class RevolutionSpeedPatch
{
    [HarmonyPrefix]
    public static void Prefix(ref BigDouble __0)
    {
        int m = Plugin.Client?.RevolutionSpeedMultiplier ?? 1;
        if (m > 1) __0 = __0 * (BigDouble)m;
    }
}
