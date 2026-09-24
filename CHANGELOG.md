# Changelog

All notable changes to the Revolution Idle Archipelago integration are documented here.
This project follows [Keep a Changelog](https://keepachangelog.com/) and
[Semantic Versioning](https://semver.org/). Versions track the apworld (`world_version`).

## [Unreleased]

### Planned
- Per-side-system achievement gating (current logic gates by prestige tier only).
- Equality goal verified in a deep playthrough.
- PopTracker pack.

## [0.21.0] - 2026-09-24

### Added
- **Third-party licence compliance.** The release bundles a patched BepInEx, UnityDoorstop,
  Il2CppInterop, Dobby and the .NET runtime, but shipped **no licence texts at all** — BepInEx and
  Doorstop are LGPL-2.1, Il2CppInterop is LGPL-3.0 and Dobby is Apache-2.0, all of which require the
  licence to travel with the binaries.
  - New `THIRD-PARTY-NOTICES.md` lists every bundled component with its licence and upstream URL,
    each one verified against the upstream project rather than assumed, and records that the BepInEx
    build is patched and Cpp2IL rebuilt.
  - New `licenses/` folder with the full LGPL-2.1, LGPL-3.0, GPL-3.0, Apache-2.0 and MIT texts,
    fetched from gnu.org and apache.org.
  - `build_release.py` ships `LICENSE`, the notices and `licenses/` in the zip.
- **"Respecting the game" section** in both READMEs, stating plainly that no game code or assets are
  redistributed, that AP play is offline and save-isolated so nothing reaches Oni Gaming's servers or
  leaderboards, that the mod is inert during normal play, and that the project will be taken down or
  amended on request.

## [0.20.4] - 2026-09-24

### Changed
- **Generation now warns when `secret_achievements` is overridden.** With a goal shallower than
  Unity, requesting secrets still (deliberately) yields none — but that was silent, and the only
  other clue was a location count nobody has reason to be counting. The generator now logs which
  slot, which goal, and how to get them anyway (`scale_achievements_to_goal: false`). The behaviour
  itself is unchanged; short goals stay short.
- The `secret_achievements` option text now states plainly that a shallow goal overrides it.

## [0.20.3] - 2026-09-24

A documentation and consistency pass before wider release, plus one generation fix.

### Fixed
- **`secret_achievements` ignored `scale_achievements_to_goal`.** The 58 secrets are Unity-gated but
  were added regardless of the goal, so `goal: infinity` + secrets put 58 of the player's own checks
  two layers past their goal — the exact problem `scale_achievements_to_goal` exists to prevent.
  Secrets now follow the same rule as the deeper tiers (and are still included when the option is
  turned off). Verified by generation: shallow goal 0 secrets, `unity` goal 58, scaling off 58.
- **The apworld's setup guide was still a v0.1 scaffold** — the page shown on the Archipelago
  website said the mod was "not yet released" and that instructions would follow. Rewritten.
- **The README inside the release zip** described a pre-AP-Mode-gate workflow: it listed 2 of 8
  goals, told players to connect without mentioning AP Mode is required, described AP Mode as
  "recommended" and configured by a config setting, and claimed secret achievements weren't
  usable as checks. Rewritten.
- `README.md` referenced `generators_goal_*`, an option that doesn't exist (a leftover from the
  goal design that ascension replaced).
- `DESIGN.md` still described `AchievementSync` as marking achievements in `unlockedAch`/`achByte`
  "visual only, no reward" — i.e. the removed behaviour that granted hundreds of real Steam
  achievements, presented as current design. It also carried 520/55 counts, the 1-100 generator
  level model, and none of the guards added since. Updated, with the hazard recorded next to the
  code it concerns and the four-layer AP-play safety model documented.
- The diagnostic's "apworld currently assumes" line was hardcoded to the old 520/55 ranges — the one
  line whose job is to reveal apworld/game drift. It now derives from `ArchipelagoClient`'s
  constants, so it cannot go stale again.
- `launch.ps1` cleared the AP Mode override in the old config filename only, which would have
  silently stopped working after the GUID rename below. It now clears both.

### Changed
- **Plugin GUID renamed** `com.jontrnka.revolutionidle.ap` -> `com.lighting8282.revolutionidle.ap`.
  BepInEx names the config file after the GUID, so settings are migrated automatically on first run
  (the old file is left in place); nobody needs to re-enter connection details.
- Tutorial author metadata is now `lighting8282`, matching `archipelago.json`.
- `reset-save.ps1` documents that it is rarely needed under AP Mode, and that its `game_data_*`
  match clears the AP save too (everything is backed up first).
- Removed the stale root `RELEASE_NOTES.md` (v0.2.0); per-version notes live with each release.
- The mod's `revolution_speed_multiplier` fallback of 1 (vs the apworld's default of 10) is now
  documented as deliberate: a slot_data without the key plays at vanilla speed rather than silently
  running at 10x.

## [0.20.2] - 2026-09-24

### Added
- **The YAML options template now ships as a release asset** (`Revolution-Idle-template.yaml`) and
  inside the release zip, so players get a template matching the version they downloaded.
- **`build_template.py`** regenerates that template from the built apworld via a local Archipelago
  install, and `build_release.py` runs it automatically. The template is Archipelago's own output,
  so hand-editing it lets it drift: the committed copy had been stale since 0.17.0, still declaring
  that world version and missing option docs added later. The script installs the freshly built
  apworld first and refuses to write a template that doesn't declare the current version, which
  catches Archipelago silently generating from a cached build.

### Fixed
- **The F1 menu clipped the "AP Mode is required" warning**, cutting off its last line ("Use the
  switch above first") and the bottom of the panel. The warning was given a fixed `4 * lineHeight`
  allowance and the panel a hardcoded height, neither of which matches how the text actually wraps.
  Both warnings and the status line are now measured with `GUIStyle.CalcHeight` and the panel height
  is derived from the same increments the layout walks, so the box always fits its contents.

## [0.20.1] - 2026-09-24

### Added
- **Goal diagnostics.** A goal that won't fire looked identical to a goal whose threshold never
  arrived in `slot_data`, and the log printed only `goal=7`.
  - The connect line now spells out the target, e.g.
    `goal=7 (achievement_count: unlock >= 250 achievements in-game)`.
  - The F3 diagnostic dump gained a **goal state** section: configured target, whether the goal was
    already sent, and the live values it is compared against (`CountUnlockedAch`, score/pMult
    exponents, `scoreEquality`).

### Changed
- Documented that `achievements_base` / `_infinity` / `_eternity` / `_unity` choose how many
  achievements become **checks** and have no effect on the win condition, and that
  `achievement_count_goal` is the goal target. The two are easy to confuse in the YAML.

## [0.20.0] - 2026-09-24

### Changed
- **Generator level checks were built on a wrong model and mostly didn't work.** They assumed
  generators cap at level 100; a live diagnostic shows `maxAmount = 1E+64`. Consequences: no check
  could ever fire above level 100, `generator_level_interval` was capped at 100, and the mod cast
  the level to `int` (overflow at high levels) before clamping it to 100.
  - Level checks are now **indexed milestones**, the same shape the ascension milestones already
    use, and for the same reason: AP needs a `location_name_to_id` that can't depend on the chosen
    interval. Milestone k = level `k * generator_level_interval`.
  - New **`generator_level_count`** option (0-100, default 0 = off) sets how many milestones each
    generator gets. **`generator_level_interval`** is now the gap between them (1-100000,
    default 25) and no longer the on/off switch.
  - The mod compares levels as `double`, so high levels work and nothing overflows.
  - **Breaking:** locations are renamed `Generator N Level <lvl>` -> `Generator N Level Milestone
    <k>`. The id space is unchanged (40001-41000, 100 per generator), and the feature defaulted to
    off, so existing seeds are unaffected unless they enabled it.
  - Verified by generation: `generator_level_count: 8` + `interval: 5000` yields 80 milestone
    locations with the expected names, and 1943 defined locations overall (unchanged).

## [0.19.0] - 2026-09-24

### Added
- **Game 1.077 support: 675 achievements (was 520) and 58 secrets (was 55).** Confirmed from a live
  diagnostic dump: `Const.ACH_COUNT = 675`, `ACH_SECRET_COUNT = 58`, and `ACH_RANGES` =
  `[0,30) [30,70) [70,161) [161,675) [10000,10058)`. All 155 new achievements went into the
  **existing Unity tier** — the update added no new tier, and ids 0-519 are unchanged, so existing
  location ids stay valid and the new ones simply extend the map.
  - `achievements_unity` range/default 359 -> **514**; `achievement_count_goal` max 520 -> **675**.
  - `_ACH_COUNT_GATES` Unity threshold 520 -> 675, so the `achievement_count` goal still gates its
    win region on the right layer.
  - The mod's own id filter (`AchCount` / `SecretCount`) was rejecting ids >= 520, so the new
    achievements could not have been sent even once the apworld defined them.
  - Verified by generation: 1943 defined locations (675 + 58 + 10 + 1000 generator-level + 200
    ascension), and a `goal: infinity` slot still correctly drops the deeper tiers.

### Fixed
- **F2 often looked broken.** Feed lines expire after 12s, and the overlay drew nothing when no line
  was live — so toggling the feed on during a quiet moment was indistinguishable from the key not
  working. F2-on now confirms itself with a feed line and reveals recent history for 10s (the fade
  is suppressed while revealing, which would otherwise draw those lines at alpha 0).

## [0.18.1] - 2026-09-24

### Fixed
- **The AP Mode toggle still relaunched the game without BepInEx** (0.17.3's working-directory fix
  addressed a real problem but not this one). Doorstop stamps `DOORSTOP_INITIALIZED` into its own
  process environment so it can't re-enter itself, and **child processes inherit that environment**.
  The relaunched game saw the marker and skipped loading BepInEx altogether, so it started and
  played normally with no mod in it — which is why the symptom looked like "the F1 menu is gone"
  and why launching the same shortcut from Explorer always worked (fresh environment).
  - `RestartGame` now strips every `DOORSTOP_*` variable from the child environment, and logs which
    ones it cleared.
  - `launch.ps1` clears them too, so the launcher is safe whoever invokes it.

## [0.18.0] - 2026-09-24

### Changed
- **AP Mode is now decided by how you launch the game, not by sticky config state.** The
  "Play Revolution Idle (AP)" shortcut passes `--archipelago`; anything else (including launching
  from Steam) is normal play. Config state could silently disagree with reality — that is exactly
  how 0.17.2's incident happened, with the config reading AP Mode while the normal save was loaded.
  A launch argument can't drift.
  - `[AP Mode] Enabled` remains as an advanced fallback for installs without the launcher, now
    defaulting to false and documented as sticky. `launch.ps1` force-clears it so an old config can
    never contradict the argument.
  - The in-game AP Mode toggle relaunches with/without the flag, and clears the stale override when
    switching to Normal.
  - To launch in AP Mode from Steam, put `--archipelago` in the game's launch options.

### Added
- **Save-identity verification (`ApSaveGuard`)** — a fourth guard, and the only one that inspects
  the loaded *save* rather than the mode the mod believes it's in. Before anything is sent:
  1. Save isolation must be **observed** working (`SaveIsolationPatches` actually redirected a save
     key), not merely inferred from AP Mode being on.
  2. The save's identity (`playerId` + `saveId`) must match the identity stamped when this seed's
     fresh AP run was created, kept in `revolutionidle_ap_savestamps.txt`.
  Fails closed, and the reason is logged once per change. Toggle:
  `[AP Mode] Verify Save Identity` (default on).
- The F1 menu shows "Save not verified — nothing is being sent" when connected but held back, so a
  refusal doesn't look like a dead connection.

## [0.17.3] - 2026-09-24

### Fixed
- **The F1 menu (and the whole mod) was missing after the AP Mode toggle restarted the game.**
  `RestartGame` relaunched the executable without setting a working directory. Doorstop resolves
  its config paths relative to the working directory (`target_assembly = BepInEx\core\...`,
  `coreclr_path = dotnet\coreclr.dll`), so the relaunched game started with **BepInEx silently not
  loaded** — the game itself ran normally, which is why this looked like "the menu disappeared"
  rather than "the mod didn't load". Confirmed from `LogOutput.log`: the restarted instance never
  opened a new BepInEx session.
  - The restart now prefers the shipped `launch.ps1` (passing `-AP` to match the new mode), so a
    toggle-restart takes exactly the same path as the "Play Revolution Idle (AP)" shortcut.
  - The direct-exe fallback pins the working directory on both `cmd` and `start /D`.
  - The chosen path and working directory are logged.

## [0.17.2] - 2026-09-24

### Fixed
- **Critical: the mod would connect to a multiworld from your normal save.** AP Mode was a
  convenience toggle rather than a precondition, so connecting (or auto-connecting on launch) while
  playing normally immediately resynced that save's existing achievements as location checks —
  releasing other players' items for progress never made in the seed, and potentially making the
  seed unwinnable. AP Mode is now **required** to connect, enforced in three independent layers:
  1. `Plugin.ConnectFromMenu` refuses to open a session, and launch auto-connect is skipped.
  2. `Plugin.Tick` suspends all AP activity (no save scanning, no checks, no goal) if AP Mode is
     off while somehow connected.
  3. `ArchipelagoClient` routes every outbound check and the goal through a single `Sendable`
     accessor that is null unless AP Mode is on — this also covers the `UnlockAchievement` Harmony
     hook, which fires on the game's own unlocks during normal play.
- The F1 menu now withholds the Connect button entirely when AP Mode is off and explains why,
  instead of letting the attempt fail after the fact.

### Added
- `[AP Mode] Required To Connect` config (default on). Turning it off restores the old, unsafe
  behaviour; it exists for mod development only.

## [0.17.1] - 2026-09-23

### Fixed
- **Critical: receiving AP achievement checks unlocked hundreds of real Steam achievements.**
  The in-game achievement panel sync (added in 0.10.0) marked AP-checked achievements by adding
  their ids to `GameData.unlockedAch`. That list is exactly what the game's own
  `GameData.TriggerMissingAchievementsAsync()` reconciles against Steam, so it dutifully pushed
  every marked achievement to the player's Steam account — 500+ at once on a resync. **Steam
  achievements are account-global, so AP Mode's save isolation does not cover them, and they
  cannot be un-earned from in-game.**
  - `AchievementSync` no longer writes *any* game state. It keeps the AP-checked set mod-side only.
  - The panel is now updated by `AchievementDisplayPatches`, which hides each card's
    `overlayLocked` UI object — genuinely visual-only, invisible to the save and to Steam.

### Added
- **`SteamAchievementGuard`** — a hard block on the Steam achievement API (`ISteamUserStats.
  SetAchievement` and `Achievement.Trigger`) whenever AP Mode is on or an AP server is connected.
  An AP run is a sandboxed alternate save and should not mint real Steam achievements at all, so
  this closes the whole class of bug rather than just the one path that caused it. Blocked calls
  are logged. Toggleable via `[AP Mode] Block Steam Achievements` (default on; leave it on).

## [0.17.0] - 2026-07-14

### Added
- **`revolution_speed_multiplier` option (default 10)** — multiplies how fast the revolutions (the
  circles) fill, i.e. the game's core loop. A 1x idle grind makes for a very long multiworld, so AP
  runs now default to **10x** speed. Set to `1` for untouched vanilla pacing.
  - Implemented as a Harmony prefix scaling the `speedMult` argument of `Revolution.Update`.
  - **Note:** this speeds up everything downstream of the revolutions, so goal thresholds
    (`ascension_goal`, `score_goal_exponent`, `prestige_mult_goal_exponent`) are reached
    proportionally faster — their defaults were calibrated against vanilla (1x) pacing.

## [0.16.0] - 2026-07-04

### Added
- **`scale_achievements_to_goal` option (default on)** — achievement tiers deeper than your chosen
  goal requires are now automatically skipped (0 achievements), regardless of their configured
  count. Previously `achievements_base/infinity/eternity/unity` were fully independent of `goal`, so
  e.g. `goal: infinity` with default achievement counts still required reaching Eternity/Unity just
  to fill your own achievement checks — silently breaking the "short run" intent. Turn the option off
  to restore that fully-independent behavior (useful if you want a shallow goal with deep achievement
  variety). Correctly accounts for `achievement_count`'s own goal-depth gating.

## [0.15.1] - 2026-06-30

### Changed
- Lowered the `ascension` goal's default (`ascension_goal`) from 5000 to **2000**. Calibrated against
  the game's own achievement #64 ("Ascended a Lot!", all 10 colors at Ascension 40+ = 400 total,
  Infinity tier) — 5000 was ~12x that; 2000 is a more reasonable default long run. Still fully
  tunable via the option.

## [0.15.0] - 2026-06-30

### Added
- **Generator-level checks are back** (`generator_level_interval`) — a check every N levels on each
  of the 10 base generators, alongside the ascension-milestone checks. Both can be used together.
  (0.14.0 had removed these; the `generators` goal stays replaced by `ascension`.)

## [0.14.0] - 2026-06-30

### Added
- **Ascension-milestone checks** — a check for every `ascension_check_interval` total ascension
  levels (summed across all 10 revolutions), up to `ascension_check_count` of them. By default these
  hold **filler only**; `ascension_checks_progression` lets them hold progression. Off by default
  (`ascension_check_count: 0`).
- **`ascension` goal** — reach a target total ascension level (`ascension_goal`). Base-tier.

### Changed / Removed (breaking)
- Replaced the **generator-level checks** (`generator_level_interval`) with the ascension-milestone
  checks above.
- Replaced the **`generators` goal** (and `generators_goal_count` / `generators_goal_level`) with the
  new `ascension` goal. Existing YAMLs using those options/goal should switch over.
- Generator **ownership** checks (own each of the 10 base generators) are unchanged.

## [0.13.0] - 2026-06-30

### Added
- **In-game achievement panel reflects AP checks** — on connect (and live), achievements whose AP
  location is already checked on the server are marked completed in the game's own achievement list.
  This keeps the panel in sync when resuming a seed or when checks are completed remotely
  (`!collect` / co-op release). It's **visual only** — it sets the unlock flag without granting the
  achievement's in-game reward/bonus. (You may need to reopen the panel to see updates.)

## [0.12.3] - 2026-06-30

### Reverted
- Reverted 0.12.1's `NakamaManager.InternalAwake` skip — it also skipped the Steam init done there,
  causing a different NRE (`GetAuthSessionTicketAsync`) via the separate `Launcher.Launch` connect
  path. The startup cloud error is the game's own offline cloud/Steam auth failing; it is caught by
  the game ("Session failed to start") and AP play is unaffected. It can't be cleanly suppressed from
  the mod because the connect runs through multiple async startup methods Harmony can't intercept.

## [0.12.2] - 2026-06-30

### Changed
- **Message feed overlay** moved to the **bottom-left** corner and the text is now **bold** (slightly
  larger) for readability.

## [0.12.1] - 2026-06-30

### Fixed
- **AP Mode Nakama crash spam (correct fix)** — traced the startup `NullReferenceException` to
  `NakamaManager.InternalAwake()`, the game's Awake that kicks off the Steam-auth/cloud-connect at
  launch (which is why it appeared before connecting). 0.11.3's `HasInternet` gate didn't help because
  Awake doesn't check it. AP Mode now skips `InternalAwake` outright (a plain void method that
  patches reliably), so the cloud subsystem never starts and the error can't occur. Normal play
  unaffected.

## [0.12.0] - 2026-06-30

### Added
- **In-game message feed overlay** — a top-right feed shows live AP activity: checks you find, items
  you receive, other players joining/leaving, hints, chat, goals, and countdowns. Messages are
  color-coded (received = green, you-found = blue, hints = yellow, joins = light blue, goals = gold)
  and fade out after ~12s. Toggle with **F2**; default on (config: `[Overlay] Show Feed`).

## [0.11.3] - 2026-06-30

### Fixed
- **AP Mode startup crash spam (real fix)** — v0.11.2's attempt to skip the async Steam-auth methods
  didn't take (Harmony can't reliably intercept IL2CPP async method bodies). AP Mode now forces
  `NakamaManager.HasInternet` to false (a plain bool getter, reliably patchable), so the game never
  starts the cloud connect chain and the `NullReferenceException` never occurs. Normal play unaffected.

## [0.11.2] - 2026-06-30

### Fixed
- AP Mode startup crash spam — first (ineffective) attempt; see 0.11.3.

## [0.11.1] - 2026-06-30

### Changed
- The F1 connection menu now **remembers the host / port / slot / password you last connected with**
  (written back to the BepInEx config on connect, so they're prefilled next launch). The password is
  stored in plaintext in the config file. Mod-only change; the apworld is functionally unchanged.

## [0.11.0] - 2026-06-29

### Added
- **Three new filler types** (a non-trap filler slot now picks one at random, including Score Boost):
  - **Generator Boost** — every base generator gains `generator_boost_levels` levels (default 20,
    capped at each generator's max). The friendly mirror of the Generator Drain trap.
  - **Overdrive** — the game runs at double speed for `overdrive_seconds` (default 60). Mirror of Lag.
  - **Income Jackpot** — grants `income_jackpot_seconds` of income at once (default 600), a bigger
    one-shot than Score Boost.

### Changed
- The timeScale effect (Freeze/Lag/Overdrive) now uses last-wins semantics so speed-ups (factor > 1)
  and slow-downs coexist cleanly. (The two unused filler placeholders were repurposed into the new
  fillers; they had never been placed.)

## [0.10.0] - 2026-06-29

### Added
- **Three new trap types** (chosen at random when `trap_chance` rolls a trap, alongside the existing
  Slowdown Trap):
  - **Freeze Trap** — the whole game stops (`timeScale 0`) for `freeze_trap_seconds` (default 30).
  - **Lag Trap** — the game runs at half speed for `lag_trap_seconds` (default 60).
  - **Generator Drain Trap** — every base generator loses `generator_drain_levels` levels (default 20).
- Freeze/Lag are driven off unscaled time so they always restore (even though they stop the normal
  game clock), and the UI/connection menu stay usable during the effect.

## [0.9.0] - 2026-06-29

### Added
- **Three new goals:**
  - `score` — reach a Score of 10^N (`score_goal_exponent`, default 100). Base-tier.
  - `prestige_mult` — reach a prestige multiplier of 10^N (`prestige_mult_goal_exponent`, default 30).
    Base-tier.
  - `achievement_count` — unlock a target number of in-game achievements (`achievement_count_goal`,
    default 250). The win region is gated by the target (higher counts need deeper layers).
- The full goal list is now: unity, equality, infinity, eternity, generators, score, prestige_mult,
  achievement_count.

## [0.8.0] - 2026-06-29

### Added
- **New `generators` goal** — win by getting a number of base generators to a target upgrade level,
  controlled by two new options: `generators_goal_count` (1-10, default 10) and
  `generators_goal_level` (1-100, default 100). Default = "max out all 10 base generators". This is a
  base-tier grind goal (reachable from the start; no layer unlocks required). The mod detects it from
  the generator levels it already reads each tick.

## [0.7.0] - 2026-06-29

### Added
- **Per-tier achievement options** — the 520 achievements are split into the game's own categories,
  each with its own count slider:
  - `achievements_base` (0–30), `achievements_infinity` (0–40), `achievements_eternity` (0–91),
    `achievements_unity` (0–359). Defaults are each tier's full size, so the default is still all 520.
- **`secret_achievements` toggle** — adds the 55 secret achievements (ids 10000–10054) as checks.
  They have obscure requirements, so they're gated behind the Unity layer and off by default.

### Changed
- **Breaking (options):** `achievement_pool` is removed in favor of the four per-tier counts above.
  Existing YAMLs using `achievement_pool` should switch to the per-tier options (location/item IDs
  are unchanged, so this is purely a YAML change).

## [0.6.3] - 2026-06-29

### Added
- **Generator-level checks** — new `generator_level_interval` option adds a check every N levels on
  each of the 10 base generators (each levels 1–100 as you buy it). `0` disables it; `25` gives
  checks at levels 25/50/75/100 (40 total), `10` gives 100 total. Sent via slot_data so only the
  chosen milestones become locations. All sit in the base tier (reachable from the start).

## [0.6.2] - 2026-06-29

### Added
- **Generator checks** — owning each of the 10 base generators is now a location check (10 new
  checks, reachable from the start). Location pool is now 520 achievements + 10 generators.

## [0.6.1] - 2026-06-29

### Added
- **In-game AP Mode toggle** in the F1 menu — switch between AP and normal play without editing
  files or using a launcher; it flips the setting and **restarts the game automatically** so the
  offline/isolated-save patches apply.

## [0.6.0] - 2026-06-29

### Added
- **AP Mode** — a separate "AP version" of the game. When enabled, the mod runs the game **offline**
  (blocks the Nakama cloud sync) and stores progress under an **isolated save** (`game_data_ap`), so:
  - your normal cloud save is never read or written (fully separate), and
  - each **new seed automatically starts fresh**, while reconnecting to the **same** seed resumes it.
- Launchers `Play Revolution Idle (AP).bat` / `Play Revolution Idle (Normal).bat` to switch modes.

### Notes
- Needed because Revolution Idle restores its save from the cloud on every launch, which made
  wiping the local save (or `reset-save.ps1`) ineffective for fresh starts.

## [0.5.0] - 2026-06-28

### Added
- **In-game connection menu** — press **F1** to open a panel with Hostname / Port / Slot / Password
  fields, a Connect/Reconnect button, and a live status line. No more editing the config file by
  hand (the config now just seeds the menu's default values and optional auto-connect on launch).

## [0.4.0] - 2026-06-28

### Added
- **Real filler/trap effects** — `Score Boost` now grants ~60s of your current income; the
  `Slowdown Trap` removes ~120s of progress (clamped at 0). Both scale with your stage and are
  applied safely to the live game (no save corruption).
- **`progressive_layers` option** — replace the separate Infinity/Eternity/Unity unlock items with
  three "Progressive Layer" items that unlock the next layer in order.

## [0.3.0] - 2026-06-28

### Added
- **`achievement_pool` option** — choose how many of the 520 achievements are checks (50–520,
  default 520). Lower = shorter run. Checks are sampled across all tiers with a guaranteed early-tier
  foothold, so gating holds at any size.
- **Two more goals** — `infinity` (reach the Infinity layer) and `eternity` (reach Eternity), in
  addition to `unity` and `equality`. The mod auto-detects all four.

### Changed
- `goal` option values: `unity=0`, `equality=1` are unchanged; `infinity=2`, `eternity=3` added.
- Clarified the `equality` goal description.

## [0.2.0] - 2026-06-28

First public release. Apworld + game mod, distributed together.

### Added
- **apworld** (`revolution_idle.apworld`) for Archipelago:
  - 520 achievement location checks (one per in-game achievement).
  - 35 items: prestige-tower unlocks, ~20 side-system unlocks, automation unlocks, filler, traps.
  - **Tiered logic** derived from the game's own achievement ranges (`Const.ACH_RANGES`):
    Menu (achievements 0–29) → Infinity (30–69) → Eternity (70–160) → Unity (161–519), each gated
    by the matching unlock item. Prestige is granted at start so the base tier is reachable.
  - Goals: `unity` (reach the Unity layer) and `equality` (reach Unity, collect every unlock, and
    earn Equality currency).
  - Options: `goal`, `trap_chance`, `death_link`.
- **Game mod** (BepInEx plugin) for Revolution Idle:
  - Connects to an Archipelago server (`Archipelago.MultiClient.Net`), parses slot data.
  - Applies received items by force-unlocking the matching systems — 30 unlock getters across 9
    game classes (prestige tower, minerals, attacks, animals, stars, lab, elements, dilation,
    relics, tarot, macro, promotion, shop, trials, challenges, and automation).
  - Sends a location check the moment any achievement unlocks, and re-syncs already-unlocked
    achievements on connect.
  - Detects goal completion in-game: Unity via the permanent reach-flag, Equality via the Equality
    currency.
  - Save/seed binding: warns if you connect a save that was previously used with a different seed.
- **Bundled patched BepInEx** so the mod loads on this game's Unity build out of the box.
- Tooling: `reset-save.ps1` (back up / wipe / restore the save for clean runs), `build_apworld.py`,
  `build_release.py`, and a bundled setup README.

### Known limitations
- Death Link is accepted but is a no-op — Revolution Idle has no death mechanic.
- The Equality goal is a long/completionist goal; its in-game trigger is validated in code but has
  not yet been confirmed by a full deep playthrough.
- Logic gates by prestige tier, not by individual side system.

### Notes
- Internal `0.1.0` was a pre-tier scaffold (all checks in one region) and was never released.
- Item and location IDs are stable as of `0.2.0`; future releases will preserve them.
