using System.Collections.Generic;
using BepInEx;
using CodeStage.AntiCheat.Storage;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RevolutionIdleAP;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BasePlugin
{
    public const string Guid = "com.jontrnka.revolutionidle.ap";
    public const string Name = "Revolution Idle Archipelago";
    public const string Version = "0.6.0";

    internal static ManualLogSource Logger = null!;
    public static ArchipelagoClient? Client;
    private static bool _resynced;
    private static bool _seedChecked;
    private static bool _freshChecked;

    // AP Mode: run offline (cloud blocked) + isolated save so AP play never touches your normal
    // cloud save and can start fresh per seed.
    public static bool APMode = false;

    // In-game connection menu state (toggled with F1). Seeded from the config file.
    public static bool ShowMenu = true;
    public static string MenuHost = "archipelago.gg";
    public static string MenuPort = "38281";
    public static string MenuSlot = "Player1";
    public static string MenuPass = "";

    public override void Load()
    {
        Logger = Log;
        Logger.LogInfo($"{Name} v{Version} loading...");

        MenuHost = Config.Bind("Connection", "Host", "archipelago.gg", "Archipelago server host").Value;
        MenuPort = Config.Bind("Connection", "Port", 38281, "Archipelago server port").Value.ToString();
        MenuSlot = Config.Bind("Connection", "Slot", "Player1", "Slot / player name").Value;
        MenuPass = Config.Bind("Connection", "Password", "", "Server password (blank if none)").Value;
        var enabled = Config.Bind("Connection", "Enabled", true, "Auto-connect on startup using the values above").Value;
        APMode = Config.Bind("AP Mode", "Enabled", false,
            "Run offline with an isolated save so AP play never touches your normal cloud save (and can start fresh per seed). Turn OFF for normal play.").Value;
        Logger.LogInfo($"[AP] AP Mode = {APMode}");

        var harmony = new Harmony(Guid);
        harmony.PatchAll(typeof(AchievementPatches));
        harmony.PatchAll(typeof(CloudPatches));
        harmony.PatchAll(typeof(SaveIsolationPatches));
        UnlockState.PatchGetters(harmony);

        ClassInjector.RegisterTypeInIl2Cpp<RevApTicker>();
        var go = new GameObject("RevolutionIdleAP_Ticker");
        Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<RevApTicker>();

        Client = new ArchipelagoClient();
        if (enabled) ConnectFromMenu();
        else Logger.LogInfo("[AP] auto-connect disabled; use the F1 menu to connect.");

        Logger.LogInfo("Revolution Idle AP loaded. Press F1 in-game for the connection menu.");
    }

    // Connect (or reconnect) using the current menu field values.
    public static void ConnectFromMenu()
    {
        if (Client == null) return;
        if (!int.TryParse(MenuPort.Trim(), out int port))
        {
            Client.SetStatus("Invalid port: " + MenuPort);
            return;
        }
        _resynced = false;
        _seedChecked = false;
        Client.ConnectAsync(MenuHost.Trim(), port, MenuSlot.Trim(), MenuPass);
    }

    // Called ~1/sec on the main thread by RevApTicker.
    public static void Tick()
    {
        if (Client == null || !Client.Connected) return;

        var data = GameController.data;
        if (data == null) return;

        // AP Mode: on the first connect to a NEW seed, wipe the isolated AP save and reload so the
        // run starts fresh. Same-seed reconnects resume. Safe: only the AP save keys are touched.
        if (APMode && !_freshChecked && !string.IsNullOrEmpty(Client.Seed))
        {
            _freshChecked = true;
            string key = Client.Slot + "|" + Client.Seed;
            if (!FreshRuns.Contains(key))
            {
                FreshRuns.Add(key);
                Logger.LogInfo($"[AP] New seed '{Client.Seed}' — starting a fresh AP save (reloading).");
                ObscuredPrefs.DeleteKey("game_data");   // remapped to game_data_ap in AP mode
                ObscuredPrefs.DeleteKey("inventory");    // remapped to inventory_ap
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
            Logger.LogInfo($"[AP] Resuming existing AP save for seed '{Client.Seed}'.");
        }

        // Non-AP play: just warn if this save was used with a different seed (no auto-reset).
        if (!APMode && !_seedChecked)
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

        // Apply any queued filler/trap effects (score boost / slowdown).
        ItemEffects.ApplyPending(data);

        // Goal detection.
        if (!Client.GoalSent && IsGoalReached(data))
            Client.CompleteGoal();
    }

    // Goal signals (slot_data goal value):
    //   0 unity    -> achByte[160]   1 equality -> scoreEquality > 0
    //   2 infinity -> achByte[29]    3 eternity -> achByte[69]
    private static bool IsGoalReached(GameData data)
    {
        try
        {
            switch (Client!.Goal)
            {
                case 1: return data.scoreEquality.ToDouble() > 0.0;
                case 2: return AchByteSet(data, 29);
                case 3: return AchByteSet(data, 69);
                default: return AchByteSet(data, 160);
            }
        }
        catch (System.Exception e)
        {
            Logger.LogError("[AP] goal check error: " + e.Message);
            return false;
        }
    }

    private static bool AchByteSet(GameData data, int index)
    {
        var ab = data.achByte;
        return ab != null && ab.Length > index && ab[index] == 1;
    }
}
