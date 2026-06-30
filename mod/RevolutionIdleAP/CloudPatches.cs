using HarmonyLib;
using IL2Task = Il2CppSystem.Threading.Tasks.Task;

namespace RevolutionIdleAP;

// AP Mode: make the game think it's not logged into the cloud, so it uses its own offline path
// (no cloud download/restore, no cloud upload). This isolates AP play from your normal cloud save
// and lets per-seed fresh starts actually stick. Only active when Plugin.APMode is true.
[HarmonyPatch(typeof(NakamaManager), "get_IsSessionOn")]
public static class CloudPatches
{
    [HarmonyPrefix]
    public static bool IsSessionOn_Prefix(ref bool __result)
    {
        if (!Plugin.APMode) return true;   // normal play: run the real check (cloud works as usual)
        __result = false;                  // AP mode: pretend offline
        return false;                      // skip the original getter
    }
}

// NOTE: skipping NakamaManager.InternalAwake was tried and REVERTED — it also skipped the Steam init
// done there, causing a GetAuthSessionTicketAsync NRE from the separate Launcher.Launch -> Connect
// path. The cloud connect has multiple entry points woven into async startup methods that Harmony
// can't reliably intercept, so it can't be cleanly suppressed from the mod.

// AP Mode: report no internet so the game never starts the Steam-auth / Nakama connect chain at all.
// This is a plain bool getter (reliably patchable, unlike the async connect methods), and stopping the
// chain at the source prevents the NullReferenceException the game throws when it auths while we've
// forced it offline. Normal play is untouched.
[HarmonyPatch(typeof(NakamaManager), "get_HasInternet")]
public static class NakamaHasInternetPatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        if (!Plugin.APMode) return true;
        __result = false;
        return false;
    }
}

// AP Mode: belt-and-suspenders attempt to also short-circuit the async connect chain. (Harmony can't
// always intercept IL2CPP async method bodies, so this may be a no-op; the HasInternet gate above is
// the primary fix.) Normal play is untouched.
[HarmonyPatch(typeof(NakamaManager), "SteamAuth")]
public static class NakamaSteamAuthPatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref IL2Task __result)
    {
        if (!Plugin.APMode) return true;
        __result = IL2Task.CompletedTask;
        Plugin.Logger.LogInfo("[AP] AP Mode: skipped Nakama SteamAuth (offline).");
        return false;
    }
}

[HarmonyPatch(typeof(NakamaManager), "Initialize")]
public static class NakamaInitializePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref IL2Task __result)
    {
        if (!Plugin.APMode) return true;
        __result = IL2Task.CompletedTask;
        return false;
    }
}
