using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace RevolutionIdleAP;

// Tracks which AP unlock items have been received, and force-unlocks the matching IL2CPP getters.
// Granting is passive: we never write game state, we only make the getter return true when asked.
public static class UnlockState
{
    // AP item name -> (declaring IL2CPP type, getter method name).
    // Declaring types verified from the Cpp2IL dump. Getter names can repeat across classes
    // (e.g. PrestigeUnlocked exists on GameData AND AttacksData), so we always key by Type+name.
    // "Equality Unlock" has no getter in the game (Equality is reached by progression), so it is
    // intentionally absent here and has no force-unlock effect.
    public static readonly Dictionary<string, (Type Type, string Getter)> ItemToGetter = new()
    {
        // Prestige tower
        ["Prestige Unlock"]           = (typeof(GameData),       "get_PrestigeUnlocked"),
        ["Infinity Unlock"]           = (typeof(GameData),       "get_InfinityUnlocked"),
        ["Eternity Unlock"]           = (typeof(GameData),       "get_EternityUnlocked"),
        ["Unity Unlock"]              = (typeof(GameData),       "get_UnityUnlocked"),

        // Side systems
        ["Minerals Unlock"]           = (typeof(GameData),       "get_MineralsUnlocked"),
        ["Special Minerals Unlock"]   = (typeof(MineralsData),   "get_SpecialMineralsProgressionUnlocked"),
        ["Attacks Unlock"]            = (typeof(GameData),       "get_AttacksUnlocked"),
        ["Animals Unlock"]            = (typeof(EternityData),   "get_AnimalsUnlocked"),
        ["Stars Unlock"]              = (typeof(InfinityData),   "get_StarUnlocked"),
        ["Lab Unlock"]                = (typeof(EternityData),   "get_LabUnlocked"),
        ["Slowdown Unlock"]           = (typeof(GameData),       "get_SlowdownUnlocked"),
        ["Elements Unlock"]           = (typeof(ElementsData),   "get_ElementTreeUnlocked"),
        ["Dilation Unlock"]           = (typeof(EternityData),   "get_DilationUnlocked"),
        ["Dilation Tree Unlock"]      = (typeof(EternityData),   "get_DilationTreeUnlocked"),
        ["Relics Unlock"]             = (typeof(AttacksData),    "get_RelicsUnlocked"),
        ["Tarot Upgrades Unlock"]     = (typeof(TarotData),      "get_TarotUpgradesUnlocked"),
        ["Tarot Challenges Unlock"]   = (typeof(TarotData),      "get_TarotChallengesUnlocked"),
        ["Tarot Artifacts Unlock"]    = (typeof(TarotData),      "get_TarotArtifactsUnlocked"),
        ["Macro Unlock"]              = (typeof(GameData),       "get_MacroUnlocked"),
        ["Promotion Unlock"]          = (typeof(GameData),       "get_PromotionUnlocked"),
        ["Shop Unlock"]               = (typeof(GameData),       "get_ShopUnlocked"),
        ["Trials Unlock"]             = (typeof(UnityData),      "get_TrialsUnlocked"),
        ["Infinity Challenges Unlock"]= (typeof(InfinityData),   "get_ChallengeUnlocked"),
        ["Eternity Challenges Unlock"]= (typeof(EternityData),   "get_ChallengesUnlocked"),

        // Automation
        ["Automation"]                = (typeof(GameData),       "get_AutomationUnlocked"),
        ["Auto-Prestige"]             = (typeof(AutomationData), "get_HasAutoPrestige"),
        ["Auto-Infinity"]             = (typeof(AutomationData), "get_HasAutoInfinity"),
        ["Auto-Eternity"]             = (typeof(AutomationData), "get_HasAutoEternity"),
        ["Auto-Ascend"]               = (typeof(AutomationData), "get_HasAutoAscend"),
        ["Auto-Minerals"]             = (typeof(AutomationData), "get_HasAutoMinUpgrade"),
    };

    // "TypeName.getterName" -> AP item name (built from the map above).
    private static readonly Dictionary<string, string> GetterKeyToItem = new();

    private static readonly HashSet<string> _granted = new();
    private static readonly HashSet<string> _loggedForce = new();
    private static readonly object _lock = new();

    public static void Grant(string itemName)
    {
        lock (_lock) { _granted.Add(itemName); }
    }

    public static bool IsGranted(string itemName)
    {
        lock (_lock) { return _granted.Contains(itemName); }
    }

    private static string Key(Type t, string getter) => t.Name + "." + getter;

    // Apply the force-unlock postfix to every mapped getter that actually exists.
    public static void PatchGetters(Harmony harmony)
    {
        var postfix = new HarmonyMethod(typeof(UnlockState).GetMethod(nameof(GetterPostfix)));
        int ok = 0, missing = 0;
        foreach (var kv in ItemToGetter)
        {
            var (type, getter) = kv.Value;
            GetterKeyToItem[Key(type, getter)] = kv.Key;
            var method = AccessTools.Method(type, getter);
            if (method == null)
            {
                Plugin.Logger.LogWarning($"[AP] getter not found, skipping: {type.Name}.{getter}");
                missing++;
                continue;
            }
            try { harmony.Patch(method, postfix: postfix); ok++; }
            catch (Exception e) { Plugin.Logger.LogError($"[AP] failed to patch {type.Name}.{getter}: {e.Message}"); missing++; }
        }
        Plugin.Logger.LogInfo($"[AP] patched {ok} unlock getters ({missing} missing/failed).");
    }

    // Generic postfix for all mapped unlock getters. If the corresponding AP item was received,
    // force the result to true. Never flips a true result to false. Keyed by Type+name so getters
    // that share a name across classes don't collide.
    public static void GetterPostfix(MethodBase __originalMethod, ref bool __result)
    {
        if (__result) return;
        string key = (__originalMethod.DeclaringType?.Name ?? "") + "." + __originalMethod.Name;
        if (GetterKeyToItem.TryGetValue(key, out var item) && IsGranted(item))
        {
            __result = true;
            lock (_lock)
            {
                if (_loggedForce.Add(item))
                    Plugin.Logger.LogInfo($"[AP] force-unlocked '{item}' via {key}");
            }
        }
    }
}
