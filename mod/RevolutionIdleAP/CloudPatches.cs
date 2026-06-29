using HarmonyLib;

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
