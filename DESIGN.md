# Revolution Idle — Archipelago World Design (v0.1 draft)

Grounded in an IL2CPP dump of `GameAssembly.dll` (Unity, metadata v31, unencrypted).
Game: **Revolution Idle** by Oni Gaming (Steam). Save model = `GameData` (+ typed sub-objects);
central singleton = `GameController : SingleBehaviour<GameController>`.

Key constants from dump: `ACH_COUNT = 520` (`ACH_SECRET_COUNT = 55`), `REV_COUNT = 10`,
`GEN_COUNT = 10`, `PrestigeType { Infinity=0, Eternity=1, Unity=2 }`.

---

## 1. Progression model (the tower → AP regions)

Linear prestige stack, each layer a discrete unlock, confirmed by currency fields
(`scorePromotion / scoreInfinity / scoreEternity / scoreUnity / scoreEquality`) and `*Data` sub-objects:

```
Revolutions (10) + Generators (10)
  → Prestige        (pMult, prestigeMult[])
    → Infinity      (InfinityData: IP, infinity upgrades, infinity challenges, generators, Stars/Stardust)
      → Eternity    (EternityData: EP, eternity milestones, Animals, eternity challenges,
                     AP/Ascension, Slowdown, Lab/Research RP)
        → Unity     (UnityData: Elements, Dilation + Dilation Tree, Relics, Tarot, Quality)
          → Equality (scoreEquality / timeEquality — final layer = goal)
```

**Side systems** (each has its own `*Unlocked` flag, gateable independently):
Minerals (+ Special Minerals progression & sacrifice), Attacks (combat), Animals,
Stars/Stardust, Lab/Research, Slowdown, Elements/Element Tree, Dilation/Dilation Tree,
Relics, Tarot (Upgrades / Challenges / Artifacts), Macro, Promotion, Shop, Trials.

---

## 2. Items

### Progression items (gate access — the core of the randomizer)
Layer unlocks: `Prestige`, `Infinity`, `Eternity`, `Unity`, `Equality`.
Side-system unlocks (~one each): Minerals, Special Minerals, Attacks, Animals, Stars/Stardust,
Lab/Research, Slowdown, Elements, Element Tree, Dilation, Dilation Tree, Relics,
Tarot Upgrades, Tarot Challenges, Tarot Artifacts, Macro, Promotion, Shop, Trials,
Infinity Challenges, Eternity Challenges.

Automation tier (classic QoL-gated AP items): Automation (master), Auto-Prestige,
Auto-Infinity, Auto-Eternity, Auto-Ascend, Auto-Minerals.
(`StartFromEnum { Manual, Automation, Macro }` confirms automation is a real gate.)

Optional: progressive Generators (1..10) and progressive Revolutions (1..10) to pace the early game.

### Useful / filler items
Multiplier boosts (`AchievementBonus`, `boostStepIncome/Mult/...` from `InventoryData`),
Time Flux capacity/gain, Offline Flux capacity, premium "soul" currency grants.

### Trap items (optional, off by default)
Temporary slowdown, small score setback, etc.

---

## 3. Locations (checks) — huge pool available

- **Achievements: 520** (465 normal + 55 secret). Read from `GameData.unlockedAch : List<int>`
  / `achByte : byte[]`. Use a yaml-configurable subset (e.g. "first N", "non-secret only", or "all").
- **Layer-first milestones**: first Prestige / Infinity / Eternity / Unity / Equality.
- **Per-layer count milestones**: Nth Infinity / Eternity / Unity.
- **Milestone bool-lists (directly readable)**: `EternityData.eternityMilestones`,
  `animalMilestones`, `challengeMilestones` (all `List<bool>` + `*Amo` counts).
- **Challenge completions**: `InfinityChallenge[]` / `EternityChallenge[]` (`complete` flags).
- **Revolution purchases** (`buyRevo : List<int>`, 10 total).
- **System-entry checks**: first time unlocking Minerals / Attacks / Lab / Tarot / Dilation / etc.

This easily supports anything from a ~50-check short sync to a 500+ check long async.

---

## 4. Goal (yaml option)

- `unity` — reach the Unity layer (medium).
- `equality` — reach Equality, the final layer (long; default for full runs).
- `achievements` — complete N achievements.
- `challenges` — complete all Infinity + Eternity challenges.

---

## 5. Logic (region access rules)

Mostly linear: each layer region requires its unlock item; side systems require their own item
(+ the layer they naturally sit under). Achievements get assigned to regions by which layer/system
they require — **this assignment needs the achievement requirement data** (see open questions).
Until then, a safe v0 is to gate achievements behind the layer their `ACH_RANGES` bucket maps to
(`GameData.ACH_RANGES : Dictionary<int,(int,int)>` exists — likely groups achievements into tabs).

---

## 6. Game-side mod (BepInEx) — architecture confirmed via Cpp2IL

Stack: **BepInEx 6 (Il2Cpp) + Il2CppInterop + Archipelago.MultiClient.Net**.
Operate on the live `GameController.Instance` / `GameData` in memory.
NOTE: save uses **Anti-Cheat Toolkit** (`ObscuredFilePrefs`/`ObscuredPrefs`, tamper detection) —
never edit prefs/save files on disk; only touch live runtime state.

### How unlocks actually gate (decoded from ISIL)
Each `get_XxxUnlocked` returns: `dev-override OR (currency >= threshold) OR achByte[N] == 1`,
where `achByte` is a broad flags/save byte blob (**~10055 bytes at runtime, NOT the 520 achievement
count** — confirmed live via the smoke test). Specific indices act as permanent unlock flags.
Confirmed indices: Prestige=`achByte[3]`, Promotion=`[11]`, Infinity=`[29]`, Eternity=`[69]`,
Unity=`[160]`, Minerals=`[239]`. A few gate on other state instead — Attacks =
`UnityData.TrialCountCompleted >= 15`, Slowdown = an eternity-milestone object.

### Granting items (force-unlock) → **Harmony-postfix the getters**
Patch each `GameData.get_XxxUnlocked` (and the few on `AttacksData`/`UnityData`/etc.) with a postfix
that returns `true` when AP has delivered that layer's/system's item. Uniform regardless of the
native gate type; **does not touch the save (ACTk-safe) and does not pollute the achievement array.**
(Do NOT grant by setting `achByte[N]=1` — that would also mark achievement N complete and collide
with the location pool.)

### Detecting locations → **hook the single achievement chokepoint**
All achievements flow through `GameData.UnlockAchievement(int id)` (canonical; also a
`UnlockAchievement(bool state, int id)` overload). Harmony-postfix it to fire the AP location for
`id` the instant any achievement unlocks — one hook covers all 520, no polling. Cross-check / resync
against `GameData.unlockedAch` (`List<int>` of completed ids; `unlockedAch.Count` read live in the
smoke test). Do NOT derive achievement state from `achByte` (it's a wider blob, not 1:1).
Milestone/challenge locations: poll the `List<bool>` arrays + challenge `complete` flags, or hook
their setters. Goal checks: `get_CountUnlockedAch` / `get_CountUnlockedAchSecret`.

### Smoke test result (2026-06-27)
Minimal plugin (`mod/RevolutionIdleAP/`) confirmed live: BepInEx loads it, **ACTk does not block**,
Harmony postfix on `get_PrestigeUnlocked` fires, and `__instance.achByte` / `unlockedAch` read fine.
Full toolchain/setup in `mod/RevolutionIdleAP/SETUP.md`.

### Connection
Connect to AP server on load; persist slot/seed in the mod's own config (not the game save).

---

## 7. Remaining data needs (lower priority — architecture no longer blocked)

1. **Achievement → region mapping** for logic: which layer each of the 520 achievements requires.
   `GameData.ACH_RANGES : Dictionary<int,(int,int)>` buckets achievements into tabs/ranges — read it
   at runtime (or from `dump.cs`) to assign achievements to regions. Exact per-achievement
   requirements live in Unity assets/localization; a range-based v0 is sufficient to start.
2. Confirm the exact Equality unlock/victory trigger (likely same `achByte[N]`/score pattern).

Artifacts in scratchpad `revidle_ap/`: `dump/` (Il2CppDumper — `dump.cs` 19MB, `il2cpp.h`,
`script.json`), `cpp2il_isil/IsilDump/Assembly-CSharp/*.txt` (per-type ISIL method bodies),
plus the `Il2CppDumper/` and `Cpp2IL.exe` tools to regenerate if scratchpad is cleared.
