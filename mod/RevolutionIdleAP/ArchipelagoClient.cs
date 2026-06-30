using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace RevolutionIdleAP;

// Wraps the Archipelago session: connection, receiving items, sending location checks, goal completion.
public class ArchipelagoClient
{
    public const long AchIdBase = 10_000;
    public const int AchCount = 520;       // normal achievements: game ids 0..519
    public const int SecretGameIdBase = 10_000;
    public const int SecretCount = 55;     // secret achievements: game ids 10000..10054 (opt-in locations)
    public const long GenIdBase = 30_000;
    public const int GenCount = 10;  // base generators (GameData.infinity.generators)
    public const long GenLevelIdBase = 40_000;
    public const int GenMaxLevel = 100; // each generator levels 1..100
    public const string GameName = "Revolution Idle";

    private ArchipelagoSession? _session;
    public bool Connected { get; private set; }

    // Goal config (from slot_data): 0 = unity, 1 = equality. Detection lives in Plugin.IsGoalReached.
    public int Goal { get; private set; } = 0;
    public bool GoalSent { get; private set; }

    // From slot_data: a generator-level check every N levels (0 = generator-level checks disabled).
    public int GenLevelInterval { get; private set; } = 0;

    // From slot_data: "generators" goal parameters (have GenGoalCount generators at level >= GenGoalLevel).
    public int GenGoalCount { get; private set; } = 10;
    public int GenGoalLevel { get; private set; } = 100;

    // From slot_data: thresholds for the score / prestige_mult / achievement_count goals.
    public int ScoreGoalExponent { get; private set; } = 100;
    public int PrestigeMultGoalExponent { get; private set; } = 30;
    public int AchievementCountGoal { get; private set; } = 250;

    // Identity of the connected multiworld, used to detect a save being reused across seeds.
    public string Slot { get; private set; } = "";
    public string Seed { get; private set; } = "";

    // Human-readable connection status for the in-game menu.
    public string Status { get; private set; } = "Not connected";

    public void SetStatus(string s) => Status = s;

    public void ConnectAsync(string host, int port, string slot, string password)
    {
        Task.Run(() =>
        {
            try { Connect(host, port, slot, password); }
            catch (Exception e) { Plugin.Logger.LogError($"[AP] connect threw: {e}"); }
        });
    }

    private void Connect(string host, int port, string slot, string password)
    {
        // Tear down any existing session so the menu's Connect can act as Reconnect.
        if (_session != null)
        {
            try { _session.Socket.DisconnectAsync(); } catch { }
            _session = null;
        }
        Connected = false;
        GoalSent = false;
        Status = $"Connecting to {host}:{port}...";

        Plugin.Logger.LogInfo($"[AP] connecting to {host}:{port} as '{slot}'...");
        Slot = slot;
        _session = ArchipelagoSessionFactory.CreateSession(host, port);

        _session.Items.ItemReceived += OnItemReceived;
        _session.MessageLog.OnMessageReceived += msg => Plugin.Logger.LogInfo("[AP] " + msg.ToString());
        _session.Socket.ErrorReceived += (e, m) => Plugin.Logger.LogError($"[AP] socket error: {m} {e}");
        _session.Socket.SocketClosed += reason =>
        {
            Connected = false;
            Status = $"Disconnected: {reason}";
            Plugin.Logger.LogWarning($"[AP] disconnected: {reason}");
        };

        LoginResult result = _session.TryConnectAndLogin(
            GameName, slot, ItemsHandlingFlags.AllItems,
            password: string.IsNullOrEmpty(password) ? null : password);

        if (result is LoginSuccessful success)
        {
            Connected = true;
            if (success.SlotData != null && success.SlotData.TryGetValue("goal", out var g) && g != null)
                Goal = Convert.ToInt32(g);
            if (success.SlotData != null && success.SlotData.TryGetValue("generator_level_interval", out var gli) && gli != null)
                GenLevelInterval = Convert.ToInt32(gli);
            if (success.SlotData != null && success.SlotData.TryGetValue("generators_goal_count", out var ggc) && ggc != null)
                GenGoalCount = Convert.ToInt32(ggc);
            if (success.SlotData != null && success.SlotData.TryGetValue("generators_goal_level", out var ggl) && ggl != null)
                GenGoalLevel = Convert.ToInt32(ggl);
            if (success.SlotData != null && success.SlotData.TryGetValue("score_goal_exponent", out var sge) && sge != null)
                ScoreGoalExponent = Convert.ToInt32(sge);
            if (success.SlotData != null && success.SlotData.TryGetValue("prestige_mult_goal_exponent", out var pme) && pme != null)
                PrestigeMultGoalExponent = Convert.ToInt32(pme);
            if (success.SlotData != null && success.SlotData.TryGetValue("achievement_count_goal", out var acg) && acg != null)
                AchievementCountGoal = Convert.ToInt32(acg);
            try { Seed = _session.RoomState?.Seed ?? ""; } catch { Seed = ""; }
            Status = $"Connected as {slot} (goal {Goal})";
            Plugin.Logger.LogInfo($"[AP] connected. goal={Goal} seed={Seed}");

            // DeathLink: Revolution Idle has no death mechanic, so we enable the service only to honor
            // the slot option — received deaths are logged and ignored, and we never send any.
            bool deathLink = success.SlotData != null && success.SlotData.TryGetValue("death_link", out var dl)
                             && dl != null && Convert.ToInt64(dl) != 0;
            if (deathLink)
            {
                var service = _session.CreateDeathLinkService();
                service.OnDeathLinkReceived += d =>
                    Plugin.Logger.LogInfo($"[AP] DeathLink received from {d.Source} ({d.Cause}) — ignored (no death mechanic).");
                service.EnableDeathLink();
                Plugin.Logger.LogInfo("[AP] DeathLink enabled (receive-only, no-op).");
            }
        }
        else if (result is LoginFailure failure)
        {
            Status = "Login failed: " + string.Join("; ", failure.Errors);
            Plugin.Logger.LogError("[AP] login failed: " + string.Join(" | ", failure.Errors));
        }
    }

    private void OnItemReceived(IReceivedItemsHelper helper)
    {
        while (helper.PeekItem() != null)
        {
            ItemInfo item = helper.DequeueItem();
            string name = item.ItemName ?? $"#{item.ItemId}";
            switch (name)
            {
                case "Progressive Layer": UnlockState.AddProgressiveLayer(); break;
                case "Score Boost": ItemEffects.QueueScoreBoost(); break;
                case "Slowdown Trap": ItemEffects.QueueTrap(); break;
                default: UnlockState.Grant(name); break;
            }
            Plugin.Logger.LogInfo($"[AP] received item: {name}");
        }
    }

    // A normal (0..519) or secret (10000..10054) achievement game id. Both map to a location via
    // AchIdBase + id (normal -> 10000..10519, secret -> 20000..20054). If the seed didn't include a
    // given achievement (per-tier counts / secrets toggle), the server simply ignores the check.
    private static bool IsAchId(int id) =>
        (id >= 0 && id < AchCount) ||
        (id >= SecretGameIdBase && id < SecretGameIdBase + SecretCount);

    // Send one achievement (game id) as an AP location check.
    public void SendAchievement(int gameAchId)
    {
        if (!Connected || _session == null) return;
        if (!IsAchId(gameAchId)) return;
        try { _session.Locations.CompleteLocationChecks(AchIdBase + gameAchId); }
        catch (Exception e) { Plugin.Logger.LogError($"[AP] send location {gameAchId} failed: {e.Message}"); }
    }

    // Send a generator check (own generator #index).
    public void SendGenerator(int index)
    {
        if (!Connected || _session == null) return;
        if (index < 0 || index >= GenCount) return;
        try { _session.Locations.CompleteLocationChecks(GenIdBase + index); }
        catch (Exception e) { Plugin.Logger.LogError($"[AP] send generator {index} failed: {e.Message}"); }
    }

    // Send a generator-level check (generator #index reached level `level`).
    public void SendGeneratorLevel(int index, int level)
    {
        if (!Connected || _session == null) return;
        if (index < 0 || index >= GenCount || level < 1 || level > GenMaxLevel) return;
        try { _session.Locations.CompleteLocationChecks(GenLevelIdBase + index * GenMaxLevel + level); }
        catch (Exception e) { Plugin.Logger.LogError($"[AP] send generator {index} level {level} failed: {e.Message}"); }
    }

    // Resync: send every already-unlocked achievement id (called once after connecting).
    public void SendAchievements(IEnumerable<int> gameAchIds)
    {
        if (!Connected || _session == null) return;
        long[] ids = gameAchIds.Where(IsAchId).Select(i => AchIdBase + i).ToArray();
        if (ids.Length == 0) return;
        try
        {
            _session.Locations.CompleteLocationChecks(ids);
            Plugin.Logger.LogInfo($"[AP] resynced {ids.Length} achievement location(s)");
        }
        catch (Exception e) { Plugin.Logger.LogError($"[AP] resync failed: {e.Message}"); }
    }

    public void CompleteGoal()
    {
        if (!Connected || _session == null || GoalSent) return;
        GoalSent = true;
        _session.Socket.SendPacketAsync(new StatusUpdatePacket { Status = ArchipelagoClientState.ClientGoal });
        Plugin.Logger.LogInfo("[AP] goal reached -> sent ClientGoal");
    }

    // Bind the current game save to this seed, and warn loudly if it was previously used with a
    // different seed (i.e. old unlocks/progress would corrupt this run).
    public void CheckSeedBinding(string playerId, int saveId)
    {
        if (string.IsNullOrEmpty(Seed)) return; // seed unknown; nothing to compare
        string key = $"{playerId}|{saveId}";
        var map = SeedBindings.Load();

        if (!map.TryGetValue(key, out var prev) || string.IsNullOrEmpty(prev))
        {
            map[key] = Seed;
            SeedBindings.Save(map);
            Plugin.Logger.LogInfo($"[AP] bound this save (saveId {saveId}) to seed {Seed}.");
        }
        else if (prev == Seed)
        {
            Plugin.Logger.LogInfo($"[AP] resuming seed {Seed} on this save. OK.");
        }
        else
        {
            Plugin.Logger.LogWarning("==================== AP SAVE MISMATCH ====================");
            Plugin.Logger.LogWarning($"[AP] This save was previously used with seed {prev},");
            Plugin.Logger.LogWarning($"[AP] but the connected server's seed is {Seed}.");
            Plugin.Logger.LogWarning("[AP] Old progress/unlocks will carry over and corrupt this run!");
            Plugin.Logger.LogWarning("[AP] Fix: in-game 'Start a new save' (or run reset-save.ps1), then reconnect.");
            Plugin.Logger.LogWarning("==========================================================");
        }
    }
}
