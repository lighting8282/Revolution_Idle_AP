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

// AP Mode: also stop the whole Nakama Steam-auth / session chain from running. Faking IsSessionOn
// isn't enough — the game still authenticates and then crashes in Initialize() (NullReferenceException)
// because we've forced it offline. Skipping these returns a completed task so startup proceeds cleanly
// with no cloud and no error spam. Normal play is untouched (prefix runs the original).
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
