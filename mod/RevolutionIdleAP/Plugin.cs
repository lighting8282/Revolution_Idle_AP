using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace RevolutionIdleAP;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BasePlugin
{
    public const string Guid = "com.jontrnka.revolutionidle.ap";
    public const string Name = "Revolution Idle Archipelago";
    public const string Version = "0.1.0";

    internal static ManualLogSource Logger = null!;
    public static ArchipelagoClient? Client;
    private static bool _resynced;
    private static bool _seedChecked;

    public override void Load()
    {
        Logger = Log;
        Logger.LogInfo($"{Name} v{Version} loading...");

        var host = Config.Bind("Connection", "Host", "archipelago.gg", "Archipelago server host").Value;
        var port = Config.Bind("Connection", "Port", 38281, "Archipelago server port").Value;
        var slot = Config.Bind("Connection", "Slot", "Player1", "Slot / player name").Value;
        var pass = Config.Bind("Connection", "Password", "", "Server password (blank if none)").Value;
        var enabled = Config.Bind("Connection", "Enabled", true, "Attempt AP connection on startup").Value;

        var harmony = new Harmony(Guid);
        harmony.PatchAll(typeof(AchievementPatches));
        UnlockState.PatchGetters(harmony);

        ClassInjector.RegisterTypeInIl2Cpp<RevApTicker>();
        var go = new GameObject("RevolutionIdleAP_Ticker");
        Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<RevApTicker>();

        Client = new ArchipelagoClient();
        if (enabled) Client.ConnectAsync(host, port, slot, pass);
        else Logger.LogInfo("[AP] connection disabled via config");

        Logger.LogInfo("Revolution Idle AP loaded.");
    }

    // Called ~1/sec on the main thread by RevApTicker.
    public static void Tick()
    {
        if (Client == null || !Client.Connected) return;

        var data = GameController.data;
        if (data == null) return;

        // One-time seed/save binding check: warn if this save was used with a different seed.
        if (!_seedChecked)
        {
            string playerId = data.playerId;
            if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(Client.Seed))
            {
                _seedChecked = true;
                Client.CheckSeedBinding(playerId, data.saveId);
            }
        }

        // One-time resync: send every already-unlocked achievement as a location check.
        if (!_resynced)
        {
            _resynced = true;
            var list = data.unlockedAch;
            if (list != null)
            {
                var ids = new List<int>();
                for (int i = 0; i < list.Count; i++) ids.Add(list[i]);
                Client.SendAchievements(ids);
            }
        }

        // Goal detection.
        if (!Client.GoalSent && IsGoalReached(data))
            Client.CompleteGoal();
    }

    // unity (goal 0): the permanent Unity-reached flag achByte[160].
    // equality (goal 1): no unlock flag exists, so use the Equality currency (scoreEquality > 0).
    private static bool IsGoalReached(GameData data)
    {
        try
        {
            if (Client!.Goal == 1)
                return data.scoreEquality.ToDouble() > 0.0;

            var ab = data.achByte;
            return ab != null && ab.Length > 160 && ab[160] == 1;
        }
        catch (System.Exception e)
        {
            Logger.LogError("[AP] goal check error: " + e.Message);
            return false;
        }
    }
}
