using System;
using HarmonyLib;

namespace RevolutionIdleAP;

// Shows AP-checked achievements as completed in the in-game panel, purely by touching the UI.
//
// Each achievement card keeps a GameObject overlay ("overlayLocked") that covers it while the
// achievement is locked. Hiding that overlay is enough to read as "checked off", and it writes
// nothing to the save, so the game's Steam reconciler never sees it. This replaces the old
// approach of adding ids to GameData.unlockedAch, which is what caused mass Steam achievement
// unlocks — see SteamAchievementGuard for the full story.
public static class AchievementDisplayPatches
{
    private static void Reflect(int id, UnityEngine.GameObject? overlayLocked)
    {
        if (overlayLocked == null) return;
        if (!AchievementSync.IsApChecked(id)) return;
        if (overlayLocked.activeSelf) overlayLocked.SetActive(false);
    }

    [HarmonyPatch(typeof(DisplayAchievementItem), "Update")]
    public static class ItemUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(DisplayAchievementItem __instance)
        {
            try { Reflect(__instance.Id, __instance.overlayLocked); }
            catch (Exception) { /* per-frame path: never let a UI hiccup spam the log */ }
        }
    }

    [HarmonyPatch(typeof(DisplayAchievementSecretItem), "Update")]
    public static class SecretUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(DisplayAchievementSecretItem __instance)
        {
            try { Reflect(__instance.Id, __instance.overlayLocked); }
            catch (Exception) { }
        }
    }
}
