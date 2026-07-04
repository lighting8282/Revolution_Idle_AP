# Revolution Idle — Archipelago World Design

Current as of **v0.16.0**. Grounded in an IL2CPP dump of `GameAssembly.dll` (Unity, metadata v31,
unencrypted). Game: **Revolution Idle** by Oni Gaming (Steam). Save model = `GameData` (+ typed
sub-objects); central singleton = `GameController : SingleBehaviour<GameController>`.

Key constants from the dump: `ACH_COUNT = 520` (`ACH_SECRET_COUNT = 55`), `REV_COUNT = 10`,
`GEN_COUNT = 10`, `PrestigeType { Infinity=0, Eternity=1, Unity=2 }`.

For the chronological story of how this was built (toolchain hurdles, dead ends, pivots), see
[`DEVLOG.md`](DEVLOG.md). This document describes the architecture as it stands today.

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

AP regions mirror this: `Menu` (base/Prestige tier) → `Infinity` → `Eternity` → `Unity`, each gated
by its layer-unlock item (or the matching `Progressive Layer` count if `progressive_layers` is on).

**Side systems** (each has its own `*Unlocked` flag, gated independently as AP items): Minerals
(+ Special Minerals), Attacks, Animals, Stars/Stardust, Lab/Research, Slowdown, Elements/Element
Tree, Dilation/Dilation Tree, Relics, Tarot (Upgrades / Challenges / Artifacts), Macro, Promotion,
Shop, Trials, Infinity/Eternity Challenges.

---

## 2. Items (`apworld/revolution_idle/items.py`) — 40 total

- **Layer unlocks (progression):** Prestige (precollected), Infinity, Eternity, Unity, Equality
  (flavor/completionist), or 3× `Progressive Layer` under `progressive_layers`.
- **Side-system unlocks (useful; progression under the `equality` goal):** ~20 items — Minerals,
  Special Minerals, Attacks, Animals, Stars, Lab, Slowdown, Elements, Dilation, Dilation Tree,
  Relics, Tarot ×3, Macro, Promotion, Shop, Trials, Infinity/Eternity Challenges.
- **Automation (useful):** Automation, Auto-Prestige, Auto-Infinity, Auto-Eternity, Auto-Ascend,
  Auto-Minerals.
- **Fillers (4, chosen at random for filler slots):** Score Boost (+income), Generator Boost
  (+levels on every generator), Income Jackpot (large one-shot income), Overdrive (2× game speed
  for a duration). All magnitudes are YAML-tunable.
- **Traps (4, chosen at random when `trap_chance` rolls a trap):** Slowdown (removes income),
  Freeze (`timeScale = 0`), Lag (`timeScale = 0.5`), Generator Drain (levels removed from every
  generator). All magnitudes are YAML-tunable.

---

## 3. Locations (checks)

- **Achievements** — up to 520, sampled per-tier (`achievements_base/infinity/eternity/unity`,
  defaults = full tier). `scale_achievements_to_goal` (default on) skips tiers deeper than the
  chosen goal requires, so a shallow goal stays a short run.
- **Secret achievements** — optional 55 (`secret_achievements`), gated behind Unity (their real
  requirements are unknown/obscure, so gating behind the deepest layer is a safe over-approximation).
- **Generator ownership** — 10 checks, one per base generator (own it at all).
- **Generator levels** — optional, a check every N levels per generator (`generator_level_interval`),
  each generator leveling 1→100 as it's bought (`Generator.amount`, no separate level field).
- **Ascension milestones** — optional, a check every N total ascension levels summed across all 10
  revolutions (`ascension_check_count` / `ascension_check_interval`). Filler-only by default
  (`ascension_checks_progression` opts them into holding progression).

All generator/ascension checks are placed in the base (`Menu`) region — they're reachable from the
very start of a run, independent of layer progress.

---

## 4. Goals (`goal` option, 8 total)

| Value | Goal | Detection |
|---|---|---|
| 0 | `unity` (default) | reach the Unity layer |
| 1 | `equality` | reach Unity + collect every side/automation unlock + `scoreEquality > 0` |
| 2 | `infinity` | reach the Infinity layer |
| 3 | `eternity` | reach the Eternity layer |
| 4 | `ascension` | total ascension (sum of `revolutions[i].ascension`) ≥ `ascension_goal` |
| 5 | `score` | `data.score.Exponent` ≥ `score_goal_exponent` (reach Score of 10^N) |
| 6 | `prestige_mult` | `data.pMult.Exponent` ≥ `prestige_mult_goal_exponent` |
| 7 | `achievement_count` | `data.CountUnlockedAch` ≥ `achievement_count_goal` |

The `achievement_count` goal's win region is itself gated by how many achievements the target
implies are needed (`_ACH_COUNT_GATES` in `world.py`) — a low target resolves to `Menu`, a high one
to `Unity`.

---

## 5. Logic (region access rules)

Mostly linear: each layer region requires its unlock item (or Progressive Layer count); side
systems require their own item plus the layer they naturally sit under. Achievement locations are
assigned to regions by the same `Const.ACH_RANGES`-derived tier boundaries (0–29 Base, 30–69
Infinity, 70–160 Eternity, 161–519 Unity) used for the `achievements_*` options.

---

## 6. Game-side mod (`mod/RevolutionIdleAP/`) — BepInEx 6 (IL2CPP) + Il2CppInterop + Archipelago.MultiClient.Net

Operates on the live `GameController.Instance` / `GameData` in memory; never edits the save file
directly (the game uses Anti-Cheat Toolkit / `ObscuredPrefs`, and on-disk tampering isn't needed —
everything is done through the live runtime + `UnityEngine.PlayerPrefs`).

| File | Responsibility |
|---|---|
| `Plugin.cs` | Load/patch orchestration, the ~1/sec `Tick()` (resync, generator/ascension check scanning, goal detection, AP Mode fresh-per-seed), F1 menu state, restart-to-apply-AP-Mode. |
| `ArchipelagoClient.cs` | Session lifecycle, slot_data parsing, sending checks (`SendAchievement`/`SendGenerator`/`SendAscensionMilestone`), applying received items, MessageLog → `ApFeed` routing. |
| `UnlockState.cs` | Maps each received unlock item to its `get_XxxUnlocked` getter; `PatchGetters` postfixes 30 getters across 9 game classes to force `true` once granted. |
| `AchievementPatches.cs` | Postfixes `GameData.UnlockAchievement(int)` — the single chokepoint all 520 achievements flow through — to send the matching check. |
| `AchievementSync.cs` | Marks AP-checked achievement locations as unlocked in `unlockedAch`/`achByte` **without** calling `UnlockAchievement` (visual only, no reward), keeping the panel in sync on resume/remote-collect. |
| `ItemEffects.cs` | Filler/trap effects: score-based (Score Boost, Income Jackpot, Slowdown) via `BigDouble` math on `data.score`; generator-based (Boost/Drain) via `Generator.amount`; time-based (Freeze/Lag/Overdrive) via `Time.timeScale`, driven by **unscaled** time so they self-restore even while paused. |
| `CloudPatches.cs` | AP Mode: forces `NakamaManager.IsSessionOn`/`HasInternet` false; best-effort (non-reliable) skips of the async Steam-auth chain. |
| `SaveIsolationPatches.cs` | AP Mode: remaps `PlayerPrefs` keys `game_data`/`inventory` → `..._ap` so AP play never touches the normal cloud save. |
| `FreshRuns.cs` / `SeedBindings.cs` | AP Mode fresh-start-per-seed marker; non-AP-mode save/seed mismatch warning. |
| `RevApTicker.cs` | Injected `MonoBehaviour`: F1/F2 key handling, IMGUI connection menu, IMGUI message-feed overlay (bottom-left, color-coded, fades ~12s), drives `Plugin.Tick()` and `ItemEffects.UpdateTimeEffects()`. |
| `ApFeed.cs` | Thread-safe ring buffer feeding the overlay from the network thread. |

### How unlocks gate (decoded from ISIL)
Each `get_XxxUnlocked` returns `dev-override OR (currency >= threshold) OR achByte[N] == 1`, where
`achByte` is a broad flags/save byte blob (~10055 bytes, **not** the 520-achievement count).
Confirmed indices: Prestige=`achByte[3]`, Promotion=`[11]`, Infinity=`[29]`, Eternity=`[69]`,
Unity=`[160]`, Minerals=`[239]`. A few gate on other state instead (Attacks on
`UnityData.TrialCountCompleted >= 15`; Slowdown on an eternity-milestone object). The mod
Harmony-postfixes each getter uniformly rather than special-casing the underlying gate type, and
never writes `achByte` directly (that would also mark a real achievement complete).

### AP Mode (offline + isolated save)
The game restores its save from Nakama cloud on every launch, which defeats both "reset for a fresh
seed" and "keep AP progress separate from normal play." AP Mode solves both: `IsSessionOn`/
`HasInternet` are forced false (no cloud round-trip), and `PlayerPrefs` save keys are remapped to an
`_ap` suffix (isolated from the normal save). A persistent marker file tracks which slot+seed
combinations have already had their fresh-start wipe, so reconnecting to the same seed resumes
instead of re-wiping. See `DEVLOG.md` for why the naive versions of this (patching `ObscuredPrefs`,
skipping `NakamaManager.InternalAwake`) didn't work.

### Known limitation: cosmetic cloud error in AP Mode
Forcing the game offline in AP Mode causes it to log a `NullReferenceException` from
`NakamaManager`/Steamworks during its own (now-broken) cloud auth attempt at startup. The game
catches it itself ("Session failed to start") and AP play is unaffected. It cannot be reliably
suppressed from the mod because IL2CPP async methods aren't reliably interceptable by Harmony —
see `DEVLOG.md` for the three attempts made.

---

## 7. Toolchain

Public BepInEx builds can't load this game's Unity build (metadata v31), so a patched BepInEx +
a from-source build of Cpp2IL (against AsmResolver 6.0.0-beta.5 + AssetRipper.CIL) is required to
generate working interop assemblies. Full one-time setup in `mod/RevolutionIdleAP/SETUP.md`.
