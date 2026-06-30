# Changelog

All notable changes to the Revolution Idle Archipelago integration are documented here.
This project follows [Keep a Changelog](https://keepachangelog.com/) and
[Semantic Versioning](https://semver.org/). Versions track the apworld (`world_version`).

## [Unreleased]

### Planned
- Per-side-system achievement gating (current logic gates by prestige tier only).
- Optional secret achievements as checks.
- Equality goal verified in a deep playthrough.
- PopTracker pack.

## [0.11.2] - 2026-06-30

### Fixed
- **AP Mode startup crash spam** — the game's Nakama cloud manager threw a `NullReferenceException`
  (red `[SERVER][ERROR]` text) on launch in AP Mode, because it still ran the full Steam-auth/session
  chain while we forced it offline. AP Mode now also short-circuits `NakamaManager.SteamAuth` and
  `Initialize`, so the cloud chain never runs offline. Normal play is unaffected.

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
