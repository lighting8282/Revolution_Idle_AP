using System;
using HarmonyLib;

namespace RevolutionIdleAP;

// Detect achievement unlocks (= AP location checks) at the single chokepoint they all flow through.
public static class AchievementPatches
{
    [HarmonyPatch(typeof(GameData), nameof(GameData.UnlockAchievement), new Type[] { typeof(int) })]
    [HarmonyPostfix]
    public static void UnlockAchievement_Postfix(int id)
    {
        Plugin.Client?.SendAchievement(id);
    }
}
