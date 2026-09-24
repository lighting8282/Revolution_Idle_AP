# Revolution Idle — Archipelago World Design

Current as of **v0.20.3**. Grounded in an IL2CPP dump of `GameAssembly.dll` (Unity, metadata v31,
unencrypted). Game: **Revolution Idle** by Oni Gaming (Steam). Save model = `GameData` (+ typed
sub-objects); central singleton = `GameController : SingleBehaviour<GameController>`.

Key constants, re-verified against game build 1.077 with the in-game diagnostic (F3):
`ACH_COUNT = 675` (`ACH_SECRET_COUNT = 58`), `REV_COUNT = 10`, `GEN_COUNT = 10`,
`RELICS_COUNT = 70`, `PrestigeType { Infinity=0, Eternity=1, Unity=2 }`. The 1.077 update added 155
achievements, all inside the existing Unity tier — ids 0-519 are unchanged, so location ids stayed
stable. `Generator.amount` (the level) has `maxAmount = 1e64`: generators do **not** cap at 100.

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

- **Achievements** — up to 675, sampled per-tier (`achievements_base/infinity/eternity/unity`,
  defaults = full tier). `scale_achievements_to_goal` (default on) skips tiers deeper than the
  chosen goal requires, so a shallow goal stays a short run.
- **Secret achievements** — optional 58 (`secret_achievements`), gated behind Unity (their real
  requirements are unknown/obscure, so gating behind the deepest layer is a safe over-approximation).
  Being Unity-gated, they obey `scale_achievements_to_goal` like any deeper tier and are skipped for
  shallower goals.
- **Generator ownership** — 10 checks, one per base generator (own it at all).
- **Generator levels** — optional, `generator_level_count` indexed milestones per generator, one
  every `generator_level_interval` levels (`Generator.amount` is the level; there is no separate
  level field). Milestones are indexed rather than keyed to an absolute level because generators run
  far past 100 and AP needs a `location_name_to_id` independent of the chosen interval.
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
Infinity, 70–160 Eternity, 161–674 Unity) used for the `achievements_*` options.

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
| `AchievementPatches.cs` | Postfixes `GameData.UnlockAchievement(int)` — the single chokepoint every achievement flows through — to send the matching check. |
| `AchievementSync.cs` | Tracks which achievements are already checked on the server, **mod-side only**. It writes no game state — see the warning below. |
| `AchievementDisplayPatches.cs` | Shows those achievements as done in the panel by hiding each card's `overlayLocked` UI object. Genuinely display-only. |
| `SteamAchievementGuard.cs` | Blocks `ISteamUserStats.SetAchievement` / `Achievement.Trigger` whenever AP Mode is on or a server is connected, so an AP run can never mint real Steam achievements. |
| `ApSaveGuard.cs` | Verifies the loaded *save* before anything is sent: save isolation observed active, plus the save's `playerId`+`saveId` matching the identity stamped when the seed's run began. Fails closed. |
| `RevolutionSpeedPatch.cs` | Scales the revolution fill rate (`revolution_speed_multiplier`, default 10). |
| `DiagnosticDump.cs` | F3 / on-launch dump of live game state (tier ranges, unlock flags, goal state, new members) for re-verifying against a new game build. |
| `ItemEffects.cs` | Filler/trap effects: score-based (Score Boost, Income Jackpot, Slowdown) via `BigDouble` math on `data.score`; generator-based (Boost/Drain) via `Generator.amount`; time-based (Freeze/Lag/Overdrive) via `Time.timeScale`, driven by **unscaled** time so they self-restore even while paused. |
| `CloudPatches.cs` | AP Mode: forces `NakamaManager.IsSessionOn`/`HasInternet` false; best-effort (non-reliable) skips of the async Steam-auth chain. |
| `SaveIsolationPatches.cs` | AP Mode: remaps `PlayerPrefs` keys `game_data`/`inventory` → `..._ap` so AP play never touches the normal cloud save. |
| `FreshRuns.cs` / `SeedBindings.cs` | AP Mode fresh-start-per-seed marker; non-AP-mode save/seed mismatch warning. |
| `RevApTicker.cs` | Injected `MonoBehaviour`: F1/F2 key handling, IMGUI connection menu, IMGUI message-feed overlay (bottom-left, color-coded, fades ~12s), drives `Plugin.Tick()` and `ItemEffects.UpdateTimeEffects()`. |
| `ApFeed.cs` | Thread-safe ring buffer feeding the overlay from the network thread. |

### Never write achievement state to make the UI look right
`GameData.TriggerMissingAchievementsAsync()` reconciles `unlockedAch` against Steam and fires
`Achievement.Trigger` for anything Steam is missing. An earlier version of `AchievementSync` wrote
AP-checked ids into `unlockedAch` to mark the panel — it skipped `UnlockAchievement`, so it granted
no in-game reward and looked harmless, but it caused the game to push **hundreds of real Steam
achievements** onto the player's account. Steam achievements are account-global (AP Mode's save
isolation does not cover them) and cannot be cleared from in-game. Reflect state by patching the UI
instead, and keep `SteamAchievementGuard` as the backstop.

### Keeping AP play off your normal save
Four independent checks, because the first three all ask "what mode does the mod think it's in?" and
that belief can be wrong:

1. **Connect** — `ConnectFromMenu` refuses to open a session outside AP Mode; launch auto-connect is
   skipped, and the F1 menu withholds the Connect button.
2. **Tick** — all AP activity suspends if AP Mode goes off while connected.
3. **Send** — every outbound check and the goal pass through `ArchipelagoClient.Sendable`, which also
   covers the Harmony hook that fires on the game's own achievement unlocks.
4. **Save** — `ApSaveGuard` checks the loaded save itself (isolation observed working + identity
   matches the stamp), which is the only check that catches AP Mode being on while a *normal* save is
   loaded.

AP Mode itself is a **launch property** (`--archipelago`, passed by the AP shortcut), not sticky
config: config state can silently disagree with how the game was actually started.

### How unlocks gate (decoded from ISIL)
Each `get_XxxUnlocked` returns `dev-override OR (currency >= threshold) OR achByte[N] == 1`, where
`achByte` is a broad flags/save byte blob (10058 bytes as of 1.077, **not** the achievement count).
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
