using System.Collections.Generic;
using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
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
    public const string Guid = "com.lighting8282.revolutionidle.ap";
    // Renamed from this in 0.20.3; MigrateLegacyConfig carries settings over so nobody re-types
    // their connection details. BepInEx names the config file after the GUID.
    private const string LegacyGuid = "com.jontrnka.revolutionidle.ap";
    public const string Name = "Revolution Idle Archipelago";
    public const string Version = "0.21.0";

    internal static ManualLogSource Logger = null!;
    public static ArchipelagoClient? Client;
    private static bool _resynced;
    private static bool _seedChecked;
    private static bool _freshChecked;
    private static bool _apModeWarned;
    private static readonly HashSet<int> _genSent = new();
    private static readonly HashSet<long> _genLevelSent = new(); // key = genIndex * 1000 + milestone index
    private static readonly HashSet<int> _ascSent = new(); // ascension milestone indices already sent

    // AP Mode: run offline (cloud blocked) + isolated save so AP play never touches your normal
    // cloud save and can start fresh per seed.
    public static bool APMode = false;
    private static ConfigEntry<bool> _apModeEntry = null!;

    // Safety: while AP drives the game, block the Steam achievement API entirely. Steam
    // achievements are account-global, so AP Mode's save isolation does not cover them, and they
    // can't be un-earned. See SteamAchievementGuard.
    public static bool AllowSteamAchievementBlock = true;

    // Safety: AP play is only allowed from AP Mode's isolated save. Connecting from a normal save
    // would send that save's existing progress into the multiworld as location checks — releasing
    // other players' items from a run that never happened, and silently un-winnable-ing the seed.
    // Nothing AP-related sends, receives, or connects unless this is true.
    public static bool RequireApMode = true;

    // Set once per tick by ApSaveGuard: the loaded save really is the isolated AP save for this
    // seed. Mode alone is not enough — see ApSaveGuard for why.
    public static bool SaveVerified;
    public static bool VerifySaveIdentity = true;

    // The mode half of the gate, checked on its own where the save-identity half can't be known yet.
    public static bool ApModeOk => !RequireApMode || APMode;
    public static bool ApPlayAllowed => ApModeOk && (!VerifySaveIdentity || SaveVerified);

    // The launch argument that turns on AP Mode. The mode is a property of HOW the game was
    // launched, not sticky state in a config file: config state can silently drift out of sync with
    // reality (e.g. if BepInEx fails to load on a relaunch, the config says AP Mode but the normal
    // save is what's actually loaded), and you can't tell which mode you're in until the game is up.
    public const string ApLaunchFlag = "--archipelago";

    private static bool HasApLaunchFlag()
    {
        try
        {
            foreach (string a in System.Environment.GetCommandLineArgs())
                if (string.Equals(a, ApLaunchFlag, System.StringComparison.OrdinalIgnoreCase)) return true;
        }
        catch { }
        return false;
    }

    public const string ApModeRequiredMessage =
        "AP Mode is required. Switch to AP Mode (below) before connecting — "
        + "connecting from your normal save would send its progress to the multiworld.";

    // In-game message feed overlay (toggled with F2).
    public static bool ShowFeed = true;

    // Diagnostics: one-shot dump of live game state (tier ranges, flags, new members) for
    // re-verifying the mod/apworld against a new game build. See DiagnosticDump.
    public static bool RunDiagnostic = true;

    // In-game connection menu state (toggled with F1). Seeded from the config file, and written back
    // on connect so the last-entered values are remembered next launch.
    public static bool ShowMenu = true;
    public static string MenuHost = "archipelago.gg";
    public static string MenuPort = "38281";
    public static string MenuSlot = "Player1";
    public static string MenuPass = "";

    private static ConfigEntry<string> _cfgHost = null!;
    private static ConfigEntry<int> _cfgPort = null!;
    private static ConfigEntry<string> _cfgSlot = null!;
    private static ConfigEntry<string> _cfgPass = null!;

    public override void Load()
    {
        Logger = Log;
        Logger.LogInfo($"{Name} v{Version} loading...");

        MigrateLegacyConfig();

        _cfgHost = Config.Bind("Connection", "Host", "archipelago.gg", "Archipelago server host");
        _cfgPort = Config.Bind("Connection", "Port", 38281, "Archipelago server port");
        _cfgSlot = Config.Bind("Connection", "Slot", "Player1", "Slot / player name");
        _cfgPass = Config.Bind("Connection", "Password", "", "Server password (blank if none). Stored in plaintext.");
        MenuHost = _cfgHost.Value;
        MenuPort = _cfgPort.Value.ToString();
        MenuSlot = _cfgSlot.Value;
        MenuPass = _cfgPass.Value;
        var enabled = Config.Bind("Connection", "Enabled", true, "Auto-connect on startup using the values above").Value;
        ShowFeed = Config.Bind("Overlay", "Show Feed", true,
            "Show the in-game AP message feed (checks, joins, hints, chat). Toggle in-game with F2.").Value;
        RunDiagnostic = Config.Bind("Diagnostics", "Dump On Launch", true,
            "Write BepInEx/revidle_diagnostic.txt once per launch with live game state (achievement tier "
            + "ranges, unlock flag indices, new game members). Used to re-verify the mod after a game update.").Value;
        _apModeEntry = Config.Bind("AP Mode", "Enabled", false,
            "ADVANCED FALLBACK — normally leave this false. AP Mode is chosen by launching with the "
            + "'Play Revolution Idle (AP)' shortcut (which passes " + ApLaunchFlag + "), not by this setting. "
            + "Set it true only for installs without the launcher; it is sticky, so it can disagree with "
            + "how you actually launched.");
        bool launchFlag = HasApLaunchFlag();
        APMode = launchFlag || _apModeEntry.Value;
        Logger.LogInfo($"[AP] AP Mode = {APMode} (launch flag: {launchFlag}, config override: {_apModeEntry.Value})");
        VerifySaveIdentity = Config.Bind("AP Mode", "Verify Save Identity", true,
            "Before sending anything, require that the loaded save really is the isolated AP save for this "
            + "seed (save isolation observed active + the save's identity matches the one stamped when the "
            + "run started). Leave this ON: it is the only check that looks at the save itself rather than "
            + "at which mode the mod thinks it's in.").Value;
        AllowSteamAchievementBlock = Config.Bind("AP Mode", "Block Steam Achievements", true,
            "Block the Steam achievement API while AP Mode is on or an AP server is connected. Leave this ON. "
            + "An AP run is a sandboxed save, so it should not award real Steam achievements — and Steam "
            + "achievements cannot be un-earned once granted.").Value;
        RequireApMode = Config.Bind("AP Mode", "Required To Connect", true,
            "Refuse to connect to an Archipelago server unless AP Mode is on. Leave this ON. Connecting "
            + "from your normal save sends that save's existing progress to the multiworld as location "
            + "checks, which releases other players' items for progress you didn't make in the seed.").Value;

        var harmony = new Harmony(Guid);
        harmony.PatchAll(typeof(SteamAchievementGuard.SetAchievementPatch));
        harmony.PatchAll(typeof(SteamAchievementGuard.TriggerPatch));
        harmony.PatchAll(typeof(AchievementDisplayPatches.ItemUpdatePatch));
        harmony.PatchAll(typeof(AchievementDisplayPatches.SecretUpdatePatch));
        harmony.PatchAll(typeof(AchievementPatches));
        harmony.PatchAll(typeof(CloudPatches));
        harmony.PatchAll(typeof(NakamaHasInternetPatch));
        harmony.PatchAll(typeof(NakamaSteamAuthPatch));
        harmony.PatchAll(typeof(NakamaInitializePatch));
        harmony.PatchAll(typeof(SaveIsolationPatches));
        harmony.PatchAll(typeof(RevolutionSpeedPatch));
        UnlockState.PatchGetters(harmony);

        ClassInjector.RegisterTypeInIl2Cpp<RevApTicker>();
        var go = new GameObject("RevolutionIdleAP_Ticker");
        Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<RevApTicker>();

        Client = new ArchipelagoClient();
        if (!ApModeOk)
        {
            Client.SetStatus("AP Mode required — not connected");
            Logger.LogWarning("[AP] not in AP Mode: auto-connect skipped. " + ApModeRequiredMessage);
        }
        else if (enabled) ConnectFromMenu();
        else Logger.LogInfo("[AP] auto-connect disabled; use the F1 menu to connect.");

        Logger.LogInfo("Revolution Idle AP loaded. Press F1 in-game for the connection menu.");
    }

    // BepInEx derives the config filename from the plugin GUID, so renaming the GUID would
    // otherwise silently orphan the player's existing settings (host/port/slot/AP Mode) and look
    // like the mod had forgotten them. Copy the old file across once, then reload.
    private void MigrateLegacyConfig()
    {
        try
        {
            string dir = System.IO.Path.GetDirectoryName(Config.ConfigFilePath)!;
            string legacy = System.IO.Path.Combine(dir, LegacyGuid + ".cfg");
            if (!System.IO.File.Exists(legacy)) return;

            var current = new System.IO.FileInfo(Config.ConfigFilePath);
            if (current.Exists && current.Length > 0) return;   // already migrated or already in use

            System.IO.File.Copy(legacy, Config.ConfigFilePath, overwrite: true);
            Config.Reload();
            Logger.LogInfo($"[AP] migrated settings from {LegacyGuid}.cfg (the old file is left in place).");
        }
        catch (System.Exception e) { Logger.LogWarning("[AP] config migration skipped: " + e.Message); }
    }

    // Connect (or reconnect) using the current menu field values.
    public static void ConnectFromMenu()
    {
        if (Client == null) return;

        // Layer 1: never open a session from a normal save.
        if (!ApModeOk)
        {
            Client.SetStatus("Refused: AP Mode is required");
            Logger.LogWarning("[AP] connect refused — " + ApModeRequiredMessage);
            return;
        }

        if (!int.TryParse(MenuPort.Trim(), out int port))
        {
            Client.SetStatus("Invalid port: " + MenuPort);
            return;
        }
        _resynced = false;
        _seedChecked = false;
        SaveMenuToConfig(port);   // remember these values for next launch
        Client.ConnectAsync(MenuHost.Trim(), port, MenuSlot.Trim(), MenuPass);
    }

    // Persist the current menu field values to the BepInEx config (auto-saved to disk by BepInEx).
    private static void SaveMenuToConfig(int port)
    {
        try
        {
            _cfgHost.Value = MenuHost.Trim();
            _cfgPort.Value = port;
            _cfgSlot.Value = MenuSlot.Trim();
            _cfgPass.Value = MenuPass;
        }
        catch (System.Exception e) { Logger.LogError("[AP] save connection config failed: " + e.Message); }
    }

    // Flip AP Mode (persisted to config) and relaunch the game so the offline/save patches apply.
    public static void ToggleApModeAndRestart()
    {
        bool target = !APMode;
        // The launch flag decides the mode now, so clear the sticky config override when switching
        // to Normal — otherwise it would put the relaunch straight back into AP Mode.
        try { if (_apModeEntry != null && !target && _apModeEntry.Value) _apModeEntry.Value = false; }
        catch (System.Exception e) { Logger.LogError("[AP] clearing AP Mode override failed: " + e.Message); }
        Logger.LogInfo($"[AP] AP Mode -> {target}; restarting game...");
        RestartGame(target);
    }

    // Relaunch this game executable after the current instance exits (avoids a two-instance overlap),
    // then quit. The new launch reads the updated AP Mode from config.
    //
    // The working directory MUST be the game folder. Doorstop resolves its config paths
    // (target_assembly = BepInEx\core\..., coreclr_path = dotnet\coreclr.dll) relative to the
    // working directory, so relaunching without it starts the game with BepInEx silently not
    // loaded — the game runs fine but the mod, and therefore the F1 menu, is simply absent.
    // This is why the shipped launch.ps1 passes -WorkingDirectory. Prefer that script when it's
    // there, so the restart takes exactly the same path as the desktop shortcuts.
    private static void RestartGame(bool apMode)
    {
        try
        {
            string exe = Process.GetCurrentProcess().MainModule!.FileName;
            string dir = System.IO.Path.GetDirectoryName(exe)!;
            string launcher = System.IO.Path.Combine(dir, "launch.ps1");
            ProcessStartInfo psi;

            if (System.IO.File.Exists(launcher))
            {
                psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c timeout /t 2 /nobreak >nul & powershell -NoProfile -ExecutionPolicy Bypass "
                              + $"-File \"{launcher}\"{(apMode ? " -AP" : "")}",
                    WorkingDirectory = dir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
            }
            else
            {
                // No launcher (e.g. a dev install): start the exe directly, but pin the working
                // directory both for cmd and for `start` itself so Doorstop still finds BepInEx.
                psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c timeout /t 2 /nobreak >nul & start \"\" /D \"{dir}\" \"{exe}\""
                              + (apMode ? " " + ApLaunchFlag : ""),
                    WorkingDirectory = dir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
            }

            // Doorstop stamps DOORSTOP_INITIALIZED into its own process environment so it can't
            // re-enter itself. Child processes inherit that environment, so the relaunched game
            // sees the marker and skips loading BepInEx entirely — the game starts and plays
            // normally, just with no mod in it. That is what made the F1 menu "disappear" after an
            // AP Mode toggle, and why launching the same shortcut from Explorer works fine (fresh
            // environment). Strip the whole DOORSTOP_* set so the new process bootstraps cleanly.
            var stale = new List<string>();
            foreach (var k in psi.Environment.Keys)
                if (k != null && k.StartsWith("DOORSTOP_", System.StringComparison.OrdinalIgnoreCase)) stale.Add(k);
            foreach (var k in stale) psi.Environment.Remove(k);

            Logger.LogInfo($"[AP] relaunching via {(System.IO.File.Exists(launcher) ? "launch.ps1" : "direct exe")} "
                         + $"(cwd: {dir}; cleared {stale.Count} DOORSTOP_* var(s): {string.Join(", ", stale)})");
            Process.Start(psi);
        }
        catch (System.Exception e) { Logger.LogError("[AP] restart failed: " + e.Message); }
        Application.Quit();
    }

    // Called ~1/sec on the main thread by RevApTicker.
    public static void Tick()
    {
        if (Client == null || !Client.Connected) return;

        // Layer 2: if AP Mode got turned off while connected, stop driving the game entirely —
        // no scanning the save, no checks, no goal. (Layer 1 blocks connecting; layer 3 blocks the
        // individual sends.) Belt and braces: this is a normal save and must be left alone.
        if (!ApModeOk)
        {
            SaveVerified = false;
            if (!_apModeWarned)
            {
                _apModeWarned = true;
                Logger.LogWarning("[AP] connected but NOT in AP Mode — all AP activity suspended. " + ApModeRequiredMessage);
            }
            return;
        }

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
                ApSaveGuard.Forget(Client.Slot, Client.Seed);  // the new save gets stamped instead
                Logger.LogInfo($"[AP] New seed '{Client.Seed}' — starting a fresh AP save (reloading).");
                ObscuredPrefs.DeleteKey("game_data");   // remapped to game_data_ap in AP mode
                ObscuredPrefs.DeleteKey("inventory");    // remapped to inventory_ap
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
            Logger.LogInfo($"[AP] Resuming existing AP save for seed '{Client.Seed}'.");
        }

        // Layer 4: the save itself must check out before anything is sent. This is the only guard
        // that looks at the loaded save rather than at which mode the mod believes it's in.
        ApSaveGuard.Verify(data);
        if (!ApPlayAllowed) return;

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

        // Generator checks: send one the first time you own each base generator.
        try
        {
            var gens = data.infinity?.generators;
            if (gens != null)
            {
                int n = gens.Count;
                int interval = Client.GenLevelInterval;
                int levelCount = Client.GenLevelCount;
                for (int i = 0; i < n && i < ArchipelagoClient.GenCount; i++)
                {
                    var g = gens[i];
                    if (g == null) continue;

                    // Own check: first time this generator has any amount.
                    if (!_genSent.Contains(i) && g.amount >= 1.0)
                    {
                        _genSent.Add(i);
                        Client.SendGenerator(i);
                        Logger.LogInfo($"[AP] generator {i + 1} owned -> check");
                    }

                    // Level milestones: milestone k fires at level k * interval. Compared in double
                    // because generator levels run far past int range (maxAmount = 1e64) — the old
                    // code cast the level to int and clamped it to 100, so nothing above level 100
                    // ever fired and the cast would have overflowed anyway.
                    if (interval > 0 && levelCount > 0)
                    {
                        double lvl = g.amount;
                        int maxK = levelCount < ArchipelagoClient.GenLevelMaxMilestones
                                 ? levelCount : ArchipelagoClient.GenLevelMaxMilestones;
                        for (int k = 1; k <= maxK; k++)
                        {
                            if (lvl < (double)k * interval) break;   // milestones are ascending
                            long key = (long)i * 1000 + k;
                            if (_genLevelSent.Add(key))
                            {
                                Client.SendGeneratorLevel(i, k);
                                Logger.LogInfo($"[AP] generator {i + 1} reached level {(long)k * interval} (milestone {k}) -> check");
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception e) { Logger.LogError("[AP] generator check error: " + e.Message); }

        // Ascension-milestone checks: one per `interval` total ascension levels (across revolutions).
        try
        {
            int count = Client.AscCheckCount, interval = Client.AscCheckInterval;
            if (count > 0 && interval > 0)
            {
                long total = AscensionTotal(data);
                int max = count < ArchipelagoClient.AscMaxMilestones ? count : ArchipelagoClient.AscMaxMilestones;
                for (int k = 1; k <= max; k++)
                {
                    if (_ascSent.Contains(k)) continue;
                    if (total >= (long)k * interval)
                    {
                        _ascSent.Add(k);
                        Client.SendAscensionMilestone(k);
                        Logger.LogInfo($"[AP] ascension milestone {k} ({(long)k * interval} levels) -> check");
                    }
                }
            }
        }
        catch (System.Exception e) { Logger.LogError("[AP] ascension check error: " + e.Message); }

        // Reflect AP-checked achievements in the in-game panel (visual only, no rewards).
        AchievementSync.ApplyPending(data);

        // Apply any queued filler/trap effects (score boost / slowdown).
        ItemEffects.ApplyPending(data);

        // Goal detection.
        if (!Client.GoalSent && IsGoalReached(data))
            Client.CompleteGoal();
    }

    // Goal signals (slot_data goal value):
    //   0 unity    -> achByte[160]   1 equality -> scoreEquality > 0
    //   2 infinity -> achByte[29]    3 eternity -> achByte[69]
    //   4 ascension -> total ascension (sum of revolutions[i].ascension) >= AscensionGoal
    //   5 score -> score >= 10^ScoreGoalExponent   6 prestige_mult -> pMult >= 10^PrestigeMultGoalExponent
    //   7 achievement_count -> CountUnlockedAch >= AchievementCountGoal
    private static bool IsGoalReached(GameData data)
    {
        try
        {
            switch (Client!.Goal)
            {
                case 1: return data.scoreEquality.ToDouble() > 0.0;
                case 2: return AchByteSet(data, 29);
                case 3: return AchByteSet(data, 69);
                case 4: return AscensionTotal(data) >= Client.AscensionGoal;
                case 5: return data.score.Exponent >= Client.ScoreGoalExponent;
                case 6: return data.pMult.Exponent >= Client.PrestigeMultGoalExponent;
                case 7: return data.CountUnlockedAch >= Client.AchievementCountGoal;
                default: return AchByteSet(data, 160);
            }
        }
        catch (System.Exception e)
        {
            Logger.LogError("[AP] goal check error: " + e.Message);
            return false;
        }
    }

    // Total ascension level summed across all revolutions (each Revolution has a long `ascension`).
    private static long AscensionTotal(GameData data)
    {
        var revs = data.revolutions;
        if (revs == null) return 0;
        long total = 0;
        int n = revs.Count;
        for (int i = 0; i < n; i++)
        {
            var r = revs[i];
            if (r != null) total += r.ascension;
        }
        return total;
    }

    private static bool AchByteSet(GameData data, int index)
    {
        var ab = data.achByte;
        return ab != null && ab.Length > index && ab[index] == 1;
    }
}
