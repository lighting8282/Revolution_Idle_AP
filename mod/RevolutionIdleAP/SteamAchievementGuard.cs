using System;
using HarmonyLib;
using Steamworks.Data;

namespace RevolutionIdleAP;

// HARD STOP between the game and the Steam achievement API.
//
// Why this exists: the game owns a routine, GameData.TriggerMissingAchievementsAsync(), that
// reconciles its own achievement list against Steam and triggers anything Steam hasn't got yet.
// Earlier versions of AchievementSync wrote AP-checked achievements into GameData.unlockedAch to
// make the in-game panel reflect AP progress. That list is exactly what the reconciler reads, so
// receiving a batch of AP checks caused the game to legitimately push hundreds of *real* Steam
// achievements onto the player's account. Steam achievements are account-global and are NOT
// covered by AP Mode's save isolation, and they cannot be un-earned from inside the game.
//
// AchievementSync no longer writes game data at all, which removes the cause. This guard is the
// second line of defense: while AP is driving the game, nothing can reach Steam, no matter which
// code path tries. An AP run is a sandboxed alternate save, so it should not be minting Steam
// achievements in the first place.
public static class SteamAchievementGuard
{
    private static int _blocked;

    // Active during any AP-driven session: AP Mode (isolated save) or a live AP connection.
    public static bool Active
    {
        get
        {
            try
            {
                if (!Plugin.AllowSteamAchievementBlock) return false;
                return Plugin.APMode || (Plugin.Client?.Connected ?? false);
            }
            catch { return false; }
        }
    }

    private static void Note(string what)
    {
        _blocked++;
        // Log the first few and then every 100th, so a mass reconcile doesn't flood the log.
        if (_blocked <= 3 || _blocked % 100 == 0)
            Plugin.Logger.LogInfo($"[AP] blocked Steam achievement ({what}); total blocked this session: {_blocked}");
    }

    // The narrowest chokepoint: every Facepunch achievement trigger ends up here.
    // Named by string because the interop declares it with internal accessibility.
    [HarmonyPatch(typeof(Steamworks.ISteamUserStats), "SetAchievement", new Type[] { typeof(string) })]
    public static class SetAchievementPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(string __0, ref bool __result)
        {
            if (!Active) return true;          // normal play: let Steam achievements work as usual
            Note("SetAchievement " + (__0 ?? "?"));
            __result = false;
            return false;                      // never reaches Steam
        }
    }

    // Belt and braces: the higher-level wrapper the game actually calls.
    [HarmonyPatch(typeof(Achievement), nameof(Achievement.Trigger))]
    public static class TriggerPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (!Active) return true;
            Note("Achievement.Trigger");
            __result = false;
            return false;
        }
    }
}
