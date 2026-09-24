using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RevolutionIdleAP;

// AP Mode save isolation. Everything the game persists (via ObscuredPrefs.Set<T>/SetRawValue/etc.)
// ultimately goes through UnityEngine.PlayerPrefs, so we remap the save keys there. In AP mode the
// game transparently reads/writes "game_data_ap"/"inventory_ap" and never touches your normal save.
public static class SaveIsolationPatches
{
    private static readonly Dictionary<string, string> KeyMap = new()
    {
        ["game_data"] = "game_data_ap",
        ["inventory"] = "inventory_ap",
    };

    // How many save-key reads/writes were actually redirected. ApSaveGuard uses this as proof that
    // isolation is really in effect, rather than trusting that Plugin.APMode implies it: if these
    // patches ever failed to apply, APMode would still be true while the game loaded the NORMAL
    // save, and every mode-based check would happily pass.
    public static int RemapCount { get; private set; }

    private static void RemapKey(ref string key)
    {
        if (Plugin.APMode && key != null && KeyMap.TryGetValue(key, out var mapped))
        {
            key = mapped;
            RemapCount++;
        }
    }

    [HarmonyPatch(typeof(PlayerPrefs), nameof(PlayerPrefs.SetString))]
    [HarmonyPrefix]
    public static void SetString_Prefix(ref string key) => RemapKey(ref key);

    [HarmonyPatch(typeof(PlayerPrefs), nameof(PlayerPrefs.HasKey))]
    [HarmonyPrefix]
    public static void HasKey_Prefix(ref string key) => RemapKey(ref key);

    [HarmonyPatch(typeof(PlayerPrefs), nameof(PlayerPrefs.DeleteKey))]
    [HarmonyPrefix]
    public static void DeleteKey_Prefix(ref string key) => RemapKey(ref key);

    [HarmonyPatch(typeof(PlayerPrefs), nameof(PlayerPrefs.GetString), new Type[] { typeof(string) })]
    [HarmonyPrefix]
    public static void GetString1_Prefix(ref string key) => RemapKey(ref key);

    [HarmonyPatch(typeof(PlayerPrefs), nameof(PlayerPrefs.GetString), new Type[] { typeof(string), typeof(string) })]
    [HarmonyPrefix]
    public static void GetString2_Prefix(ref string key) => RemapKey(ref key);
}
