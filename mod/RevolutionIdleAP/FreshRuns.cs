using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace RevolutionIdleAP;

// Remembers which (slot|seed) combos have already had their one-time fresh start in AP Mode, so a
// seed is wiped fresh only the FIRST time it's connected — later launches of the same seed resume.
public static class FreshRuns
{
    private static string FilePath => Path.Combine(Paths.ConfigPath, "revolutionidle_ap_freshruns.txt");

    public static bool Contains(string key)
    {
        try
        {
            if (!File.Exists(FilePath)) return false;
            foreach (var line in File.ReadAllLines(FilePath))
                if (line.Trim() == key) return true;
        }
        catch (Exception e) { Plugin.Logger.LogWarning("[AP] freshruns read failed: " + e.Message); }
        return false;
    }

    public static void Add(string key)
    {
        try { File.AppendAllText(FilePath, key + Environment.NewLine); }
        catch (Exception e) { Plugin.Logger.LogWarning("[AP] freshruns write failed: " + e.Message); }
    }
}
