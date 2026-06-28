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
