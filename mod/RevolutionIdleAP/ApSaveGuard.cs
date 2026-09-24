using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace RevolutionIdleAP;

// Verifies that the save currently loaded really is the isolated AP save for the connected seed,
// before anything is allowed to be sent to the multiworld.
//
// Every other guard in this mod checks the *mode* — "is AP Mode on?". That is exactly the assumption
// that failed in practice: a toggle-restart dropped BepInEx, the config still said AP Mode, and the
// normal save was what actually got loaded. A mode-based check can't see that. This one looks at the
// save itself:
//
//   1. Save isolation must be observed working (SaveIsolationPatches actually redirected save keys),
//      not merely assumed from Plugin.APMode.
//   2. The save's own identity (playerId + saveId) must match the identity stamped when this seed's
//      fresh AP run was created. A different save — your normal one, or another seed's — won't match.
//
// Fails closed: no proof, nothing sends. That is the right default for a guard whose whole job is to
// keep a real save's progress out of a multiworld.
public static class ApSaveGuard
{
    private static string FilePath => Path.Combine(Paths.ConfigPath, "revolutionidle_ap_savestamps.txt");

    private static Dictionary<string, string>? _cache;
    private static string _lastRefusal = "";

    private static Dictionary<string, string> Load()
    {
        if (_cache != null) return _cache;
        var map = new Dictionary<string, string>();
        try
        {
            if (File.Exists(FilePath))
            {
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    int eq = line.IndexOf('=');
                    if (eq > 0) map[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("[AP] save-stamp read failed: " + e.Message); }
        _cache = map;
        return map;
    }

    private static void Save()
    {
        try
        {
            var lines = new List<string>();
            foreach (var kv in Load()) lines.Add(kv.Key + "=" + kv.Value);
            File.WriteAllLines(FilePath, lines);
        }
        catch (Exception e) { Plugin.Logger.LogWarning("[AP] save-stamp write failed: " + e.Message); }
    }

    private static string Identity(GameData data)
    {
        // playerId is a string and saveId an int in the interop; concatenation keeps this agnostic
        // to which is which if a game update changes either type.
        string player = "";
        string save = "";
        try { player = data.playerId + ""; } catch { }
        try { save = data.saveId + ""; } catch { }
        return player + "|" + save;
    }

    // Called every tick from Plugin.Tick, before any checks are sent. Sets Plugin.SaveVerified.
    public static void Verify(GameData data)
    {
        Plugin.SaveVerified = Check(data, out string reason);
        if (!Plugin.SaveVerified && reason != _lastRefusal)
        {
            _lastRefusal = reason;
            Plugin.Logger.LogWarning("[AP] NOT sending to the multiworld — " + reason);
        }
        else if (Plugin.SaveVerified && _lastRefusal.Length > 0)
        {
            _lastRefusal = "";
        }
    }

    private static bool Check(GameData data, out string reason)
    {
        var client = Plugin.Client;
        if (client == null || string.IsNullOrEmpty(client.Seed))
        {
            reason = "no connected seed yet.";
            return false;
        }

        if (!Plugin.APMode)
        {
            reason = "AP Mode is off, so this is not an AP save.";
            return false;
        }

        // Proof, not assumption: the game must have actually gone through the remapped save keys.
        if (SaveIsolationPatches.RemapCount == 0)
        {
            reason = "save isolation never engaged (no save key was redirected), so the loaded save "
                   + "may be your normal one. Relaunch with the 'Play Revolution Idle (AP)' shortcut.";
            return false;
        }

        string key = client.Slot + "|" + client.Seed;
        string identity = Identity(data);
        if (identity == "|")
        {
            reason = "the save has no identity yet (still loading).";
            return false;
        }

        var map = Load();
        if (!map.TryGetValue(key, out string? stamped))
        {
            // First verified sight of this seed's AP save: adopt its identity as the reference.
            // Safe because the checks above already established this is the isolated AP save.
            map[key] = identity;
            Save();
            Plugin.Logger.LogInfo($"[AP] stamped this AP save as the one for seed '{client.Seed}'.");
            reason = "";
            return true;
        }

        if (stamped != identity)
        {
            reason = $"this save is not the AP save this seed was started with (expected '{stamped}', "
                   + $"loaded '{identity}'). Nothing will be sent. If you deliberately started a new AP "
                   + $"save for this seed, delete its line from {Path.GetFileName(FilePath)}.";
            return false;
        }

        reason = "";
        return true;
    }

    // Called when a fresh AP save is created for a seed, so the next Verify re-stamps it.
    public static void Forget(string slot, string seed)
    {
        try
        {
            var map = Load();
            if (map.Remove(slot + "|" + seed)) Save();
        }
        catch (Exception e) { Plugin.Logger.LogWarning("[AP] save-stamp reset failed: " + e.Message); }
    }
}
