using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace RevolutionIdleAP;

// Persists a mapping of game-save identity -> AP seed name, in the mod's own file
// (never touches the game's obscured save). Used to detect a save reused across seeds.
// Stored as simple "key=value" lines to avoid pulling in JSON (the game bundles its own
// Il2Cpp Newtonsoft, which collides with the managed one).
public static class SeedBindings
{
    private static string FilePath => Path.Combine(Paths.ConfigPath, "revolutionidle_ap_seedbindings.txt");

    public static Dictionary<string, string> Load()
    {
        var map = new Dictionary<string, string>();
        try
        {
            if (!File.Exists(FilePath)) return map;
            foreach (var line in File.ReadAllLines(FilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                map[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("[AP] failed to read seed bindings: " + e.Message); }
        return map;
    }

    public static void Save(Dictionary<string, string> map)
    {
        try
        {
            var lines = new List<string> { "# save-identity=seed (managed by Revolution Idle AP mod)" };
            foreach (var kv in map) lines.Add($"{kv.Key}={kv.Value}");
            File.WriteAllLines(FilePath, lines);
        }
        catch (Exception e) { Plugin.Logger.LogWarning("[AP] failed to write seed bindings: " + e.Message); }
    }
}
