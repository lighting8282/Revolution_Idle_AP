using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using Il2CppInterop.Runtime;

namespace RevolutionIdleAP;

// One-shot diagnostic dump of live game state, used to re-verify the mod/apworld against a new game
// build. Writes BepInEx/revidle_diagnostic.txt (and mirrors it to the BepInEx log).
//
// Member discovery deliberately uses the LOW-LEVEL il2cpp API rather than managed reflection:
// managed reflection over an interop wrapper only shows the *generated stubs*, which are stale until
// BepInEx regenerates them after a game update — so it would hide exactly the new members we're
// hunting for. The il2cpp_* calls read the live runtime metadata instead.
//
// Runs without needing an AP connection. Gated by the [Diagnostics] config option.
public static class DiagnosticDump
{
    private static bool _done;

    // Auto-dump once on launch. NOTE: this fires as soon as GameController.data exists, which is
    // BEFORE the save finishes loading — so it captures a default/empty state. Useful as a baseline,
    // but for real flag/progress data use the F3 on-demand dump once your save is actually loaded.
    public static void TryRunOnce()
    {
        if (_done || !Plugin.RunDiagnostic) return;
        _done = true;
        Dump("auto (on launch — save may not be loaded yet)");
    }

    // On-demand dump (F3). Appends, so you can capture several snapshots in one session.
    public static void RunNow() => Dump("manual (F3)");

    private static void Dump(string reason)
    {
        GameData data;
        try { data = GameController.data; } catch { return; }
        if (data == null) { Plugin.Logger.LogWarning("[AP][diag] GameController.data is null — skipped."); return; }

        var sb = new StringBuilder();
        void W(string s) { sb.AppendLine(s); }

        try
        {
            W("");
            W("================================================================");
            W("=== Revolution Idle AP — diagnostic dump ===");
            W($"trigger     : {reason}");
            W($"mod version : {Plugin.Version}");
            W($"utc         : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            W($"AP Mode     : {Plugin.APMode}");
            W($"save loaded?: unlockedAch={SafeCount(data)}  (0 usually means the save hasn't loaded)");
            W("================================================================");
            W("");

            DumpGoalState(W, data);
            DumpAchRanges(W);
            DumpAchByte(W, data);
            DumpSingularity(W, data);
            DumpRevolutionsAndGenerators(W);
            DumpConstStatics(W);
            DumpClassFields(W, "GameData", data.Pointer);
        }
        catch (Exception e) { W("FATAL during dump: " + e); }

        try
        {
            string path = Path.Combine(Paths.BepInExRootPath, "revidle_diagnostic.txt");
            File.AppendAllText(path, sb.ToString());
            Plugin.Logger.LogInfo($"[AP][diag] appended diagnostic dump -> {path}");
        }
        catch (Exception e) { Plugin.Logger.LogError("[AP][diag] write failed: " + e.Message); }

        foreach (var line in sb.ToString().Split('\n'))
            Plugin.Logger.LogInfo("[AP][diag] " + line.TrimEnd('\r'));
    }

    private static string SafeCount(GameData data)
    {
        try { return (data.unlockedAch?.Count ?? -1).ToString(); } catch { return "?"; }
    }

    // The new Singularity layer: its counter (the `singularity` goal candidate) and sub-systems,
    // now typed because BepInEx regenerated the interop for the updated game build.
    // Why the goal has or hasn't fired: the configured target next to the live value it's compared
    // against. Without this, a goal that won't trigger is indistinguishable from a goal whose
    // threshold never arrived in slot_data.
    private static void DumpGoalState(Action<string> W, GameData data)
    {
        W("--- goal state ---");
        var c = Plugin.Client;
        if (c == null || !c.Connected)
        {
            W("  not connected — goal config comes from slot_data, so it is unknown until then.");
            W("");
            return;
        }

        W($"  goal = {c.Goal}  ({c.GoalDescription})");
        W($"  already sent? {c.GoalSent}");
        try { W($"  live: CountUnlockedAch = {data.CountUnlockedAch}   (unlockedAch.Count = {data.unlockedAch?.Count ?? -1})"); }
        catch (Exception e) { W("  live: CountUnlockedAch failed: " + e.Message); }
        try { W($"  live: score exponent = {data.score.Exponent}   pMult exponent = {data.pMult.Exponent}"); }
        catch (Exception e) { W("  live: score/pMult failed: " + e.Message); }
        try { W($"  live: scoreEquality = {data.scoreEquality.ToDouble()}"); }
        catch (Exception e) { W("  live: scoreEquality failed: " + e.Message); }
        W("");
    }

    private static void DumpSingularity(Action<string> W, GameData data)
    {
        W("--- Singularity (new layer) ---");
        try
        {
            var s = data.singularity;
            if (s == null) { W("  GameData.singularity = <null>"); W(""); return; }
            W($"  singularity (COUNT — candidate for a `singularity` goal) = {s.singularity}");
            W($"  singMult = {s.singMult}   singMultNext = {s.singMultNext}");
            W($"  atoms = {s.atoms}   atomsThreshold = {s.atomsThreshold}   atomsGain = {s.atomsGain}");
            W($"  singZodiacLevel = {s.singZodiacLevel}   luck = {s.luck}");
            try { W($"  singMilestonesSingularity.Count = {s.singMilestonesSingularity?.Count ?? -1}  (check candidates)"); } catch { }
            try { W($"  singMilestonesAtoms.Count        = {s.singMilestonesAtoms?.Count ?? -1}"); } catch { }
            try { W($"  singMilestonesProg.Count         = {s.singMilestonesProg?.Count ?? -1}"); } catch { }
            try { W($"  treeNodes.Count (Singularity tree) = {s.treeNodes?.Count ?? -1}"); } catch { }
            try { W($"  inventory.Count (SingularZodiac)   = {s.inventory?.Count ?? -1}"); } catch { }
            try { W($"  houses.Count (Astrology)           = {s.houses?.Count ?? -1}"); } catch { }
        }
        catch (Exception e) { W("  ERROR: " + e.Message); }
        W("");
    }

    // ---------- typed reads (these members are confirmed to still exist post-update) ----------

    // The achievement tier ranges the apworld's TIERS table mirrors. THE key unknown for tiering
    // the 155 new achievements.
    private static void DumpAchRanges(Action<string> W)
    {
        W("--- Const.ACH_RANGES (category -> (start, endExclusive)) ---");
        W("    apworld currently assumes: 0=[0,30) 1=[30,70) 2=[70,161) 3=[161,520) secret=[10000,10055)");
        try
        {
            var ranges = Const.ACH_RANGES;
            if (ranges == null) { W("  <null>"); }
            else
            {
                W($"  Count = {ranges.Count}");
                for (int k = 0; k < 24; k++)
                {
                    try
                    {
                        if (!ranges.ContainsKey(k)) continue;
                        // The value is a BOXED ValueTuple<int,int>. ToString() prints only the type
                        // name, and reading .Item1/.Item2 through the interop wrapper lands on the
                        // il2cpp object header (garbage). Unbox to get the real data pointer first,
                        // then read the two ints directly.
                        var tup = ranges[k];
                        IntPtr data = IL2CPP.il2cpp_object_unbox(tup.Pointer);
                        int start = Marshal.ReadInt32(data, 0);
                        int end = Marshal.ReadInt32(data, 4);
                        W($"  [{k}] = [{start}, {end})   ({end - start} achievements)");
                    }
                    catch (Exception e) { W($"  [{k}] = <unreadable: {e.GetType().Name}>"); }
                }
            }
        }
        catch (Exception e) { W("  ERROR: " + e.Message); }
        W("");
    }

    // Goal detection reads achByte at fixed indices; confirm they still hold after the update.
    private static void DumpAchByte(Action<string> W, GameData data)
    {
        W("--- achByte (unlock/goal flag blob) ---");
        try
        {
            var ab = data.achByte;
            if (ab == null) { W("  <null>"); W(""); return; }
            W($"  Length = {ab.Length}   (was ~10055 pre-update)");
            int[] known = { 3, 11, 29, 69, 160, 239 };
            string[] label = { "Prestige", "Promotion", "Infinity", "Eternity", "Unity", "Minerals" };
            for (int i = 0; i < known.Length; i++)
            {
                int idx = known[i];
                string v = idx < ab.Length ? ab[idx].ToString() : "<out of range>";
                W($"  [{idx,5}] = {v}   (expected gate: {label[i]})");
            }
            var set = new StringBuilder();
            int limit = Math.Min(ab.Length, 1200);
            int count = 0;
            for (int i = 0; i < limit; i++)
                if (ab[i] == 1) { set.Append(i).Append(' '); count++; }
            W($"  set flags in 0..{limit - 1} ({count}): {set}");
            W("  (cross-reference with your actual in-game progress to identify a new layer's gate)");
        }
        catch (Exception e) { W("  ERROR: " + e.Message); }
        W("");
    }

    private static void DumpRevolutionsAndGenerators(Action<string> W)
    {
        W("--- revolutions / generators ---");
        try
        {
            var data = GameController.data;
            var revs = data.revolutions;
            if (revs == null) W("  revolutions = <null>");
            else
            {
                long total = 0;
                W($"  revolutions.Count = {revs.Count}   (REV_COUNT expected 10)");
                for (int i = 0; i < revs.Count; i++)
                {
                    var r = revs[i];
                    if (r == null) continue;
                    total += r.ascension;
                    W($"    rev[{i}] ascension={r.ascension} speed={r.speed} progress={r.progress} amount={r.amount}");
                }
                W($"  TOTAL ascension = {total}   (the `ascension` goal compares this to ascension_goal)");
            }
            var gens = data.infinity?.generators;
            if (gens == null) W("  infinity.generators = <null>");
            else
            {
                W($"  generators.Count = {gens.Count}   (GEN_COUNT expected 10)");
                for (int i = 0; i < gens.Count; i++)
                {
                    var g = gens[i];
                    if (g != null) W($"    gen[{i}] amount(level)={g.amount} maxAmount={g.maxAmount}");
                }
            }
            W($"  revolution_speed_multiplier applied = {Plugin.Client?.RevolutionSpeedMultiplier ?? 1}");
        }
        catch (Exception e) { W("  ERROR: " + e.Message); }
        W("");
    }

    // ---------- low-level il2cpp metadata walking (sees NEW members despite stale interop) ----------

    private static string Str(IntPtr p) => p == IntPtr.Zero ? "?" : (Marshal.PtrToStringAnsi(p) ?? "?");

    // Const is a static class, so it can't be a type argument — look its class up by name instead,
    // then read its static int constants (ACH_COUNT etc). Reading these at runtime matters because
    // C# would otherwise inline the OLD compile-time values from the stale interop.
    private static unsafe void DumpConstStatics(Action<string> W)
    {
        W("--- Const static fields (real runtime values; C# would inline stale consts) ---");
        try
        {
            IntPtr klass = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "Const");
            if (klass == IntPtr.Zero) { W("  <could not resolve Const class>"); W(""); return; }
            IntPtr iter = IntPtr.Zero, field;
            while ((field = IL2CPP.il2cpp_class_get_fields(klass, ref iter)) != IntPtr.Zero)
            {
                string name = Str(IL2CPP.il2cpp_field_get_name(field));
                if (!(name.Contains("ACH") || name.Contains("REV") || name.Contains("GEN")
                      || name.Contains("COUNT") || name.Contains("SINGULAR") || name.Contains("RANGE")))
                    continue;
                IntPtr ftype = IL2CPP.il2cpp_field_get_type(field);
                string tname = Str(IL2CPP.il2cpp_type_get_name(ftype));
                string val = "<not an int>";
                if (tname == "System.Int32")
                {
                    try
                    {
                        int v = 0;
                        IL2CPP.il2cpp_field_static_get_value(field, &v);
                        val = v.ToString();
                    }
                    catch (Exception e) { val = "<unreadable: " + e.GetType().Name + ">"; }
                }
                W($"  {name} : {tname} = {val}");
            }
        }
        catch (Exception e) { W("  ERROR: " + e.Message); }
        W("");
    }

    // Walk a live object's real field list. Flags anything Singularity/Equality related and, for the
    // Singularity sub-object, recurses to expose its counters / unlock gate.
    private static void DumpClassFields(Action<string> W, string label, IntPtr objPtr)
    {
        W($"--- {label} live fields (il2cpp metadata; * = Singularity/Equality related) ---");
        try
        {
            if (objPtr == IntPtr.Zero) { W("  <null object>"); W(""); return; }
            IntPtr klass = IL2CPP.il2cpp_object_get_class(objPtr);
            W($"  class = {Str(IL2CPP.il2cpp_class_get_name(klass))}");

            // METADATA ONLY (names/types/offsets). Deliberately does NOT read field *values*: a
            // previous version followed "interesting" fields as object pointers, which crashed the
            // game (0xC0000005) as soon as a struct field like BigDouble held a non-zero value —
            // those bytes are inline data, not a pointer. Values come from typed access instead
            // (see DumpSingularity), which handles value types correctly.
            IntPtr iter = IntPtr.Zero, field;
            int n = 0;
            while ((field = IL2CPP.il2cpp_class_get_fields(klass, ref iter)) != IntPtr.Zero)
            {
                n++;
                string name = Str(IL2CPP.il2cpp_field_get_name(field));
                string tname = Str(IL2CPP.il2cpp_type_get_name(IL2CPP.il2cpp_field_get_type(field)));
                uint off = IL2CPP.il2cpp_field_get_offset(field);
                bool hot = name.IndexOf("singular", StringComparison.OrdinalIgnoreCase) >= 0
                        || tname.IndexOf("singular", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("equality", StringComparison.OrdinalIgnoreCase) >= 0;
                W($"  {(hot ? "*" : " ")} 0x{off:X3} {name} : {tname}");
            }
            W($"  field count = {n}");
        }
        catch (Exception e) { W("  ERROR: " + e.Message); }
        W("");
    }
}
