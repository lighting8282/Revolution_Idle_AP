# Revolution Idle — Archipelago

Play [Revolution Idle](https://store.steampowered.com/app/2763740/) as an
[Archipelago](https://archipelago.gg) multiworld. Your prestige layers and side systems start
**locked** and are unlocked by items from the multiworld; completing the game's **achievements** and
**owning/leveling generators** sends checks out to other players.

This repo has two parts:

- **apworld** — the Archipelago world (`apworld/revolution_idle/`)
- **mod** — a BepInEx plugin that bridges the running game to the AP server (`mod/RevolutionIdleAP/`)

## Download / Install

Grab the latest [**release**](https://github.com/lighting8282/Revolution_Idle_AP/releases/latest).

- `RevolutionIdleAP-vX.Y.Z.zip` — everything: the apworld, the mod, a **pre-patched BepInEx**, and
  setup instructions. (Stock BepInEx can't load this game's Unity build, so a patched one is bundled.)
- `revolution_idle.apworld` — the apworld on its own, if you only need to generate.

Full install steps are in the zip's `README.md`. In short: drop the `.apworld` into Archipelago's
`custom_worlds/`, extract `Install-into-Game-Folder/` into your game folder, launch once (first run
builds mod support, ~2–4 min), then press **F1** in-game to open the connection menu and connect.

## Features

**Location checks**
- Up to **520 achievement** checks, split into the game's own tiers — pick how many of each become
  checks with `achievements_base` / `achievements_infinity` / `achievements_eternity` /
  `achievements_unity` (defaults = the full 520).
- Optional **55 secret achievements** (`secret_achievements`).
- **10 generator** checks (own each base generator).
- Optional **ascension-milestone** checks — one per `ascension_check_interval` total ascension
  levels, up to `ascension_check_count` of them. Filler-only by default
  (`ascension_checks_progression` lets them hold progression).

**Items**
- Prestige-tower unlocks (Infinity / Eternity / Unity — or three `progressive_layers` items),
  ~20 side-system unlocks, and automation unlocks.
- **4 fillers**: Score Boost, Generator Boost, Income Jackpot, Overdrive.
- **4 traps**: Slowdown, Freeze, Lag, Generator Drain — picked at random; every effect's magnitude
  is tunable from the YAML.

**Goals** (all auto-detected in-game):
- `unity`, `eternity`, `infinity` — reach that layer.
- `equality` — reach Unity, collect every unlock, earn Equality currency (completionist).
- `ascension` — reach a target total ascension level (summed across all revolutions).
- `score` / `prestige_mult` — reach 10^N Score / prestige multiplier.
- `achievement_count` — unlock N achievements in-game.

**In-game integration**
- **Tiered logic** mirroring the game's progression (base → Infinity → Eternity → Unity), gated by
  each layer's unlock item (derived from the game's `Const.ACH_RANGES`).
- Receiving an item force-unlocks the matching system (30 unlocks across 9 game classes); completing
  an achievement or owning/leveling a generator sends its check.
- **F1 connection menu** (host / port / slot / password + live status) — remembers your last-used
  values; no config editing needed.
- **F2 message feed overlay** — a bottom-left feed of live AP activity (checks, items, joins, hints,
  chat, goals), color-coded.
- The in-game **achievement panel reflects AP-checked achievements** (visual only — no rewards),
  staying in sync when you resume a seed or checks are collected remotely.
- **AP Mode** — plays offline with an **isolated save** so AP never touches your normal (cloud) save,
  and **auto-starts fresh per seed** (resumes the same seed). It's effectively a separate "AP version"
  of the game. Toggle it from the **F1 menu** (auto-restarts) or launch the bundled
  `Play Revolution Idle (AP).bat`.
- `death_link` option is accepted but is a no-op (the game has no death mechanic).

### YAML

A full, commented template (standard Archipelago format) is in
[`examples/Revolution Idle.yaml`](examples/Revolution%20Idle.yaml). The website's "Generate Template"
and the Archipelago Launcher's "Generate Template Options" also produce it automatically once the
apworld is installed.

A minimal config looks like:

```yaml
name: Player{number}
description: My Revolution Idle run
game: Revolution Idle
requires:
  version: 0.6.7
Revolution Idle:
  goal: unity                 # unity|equality|infinity|eternity|ascension|score|prestige_mult|achievement_count
  # how many achievements per tier become checks (defaults shown = all 520)
  achievements_base: 30       # 0-30
  achievements_infinity: 40   # 0-40
  achievements_eternity: 91   # 0-91
  achievements_unity: 359     # 0-359
  secret_achievements: false  # add the 55 secret achievements
  ascension_check_count: 0    # 0 = off; else a check every `ascension_check_interval` total ascension levels
  ascension_check_interval: 500
  progressive_layers: false
  trap_chance: 10             # 0-100; when a trap rolls, its type is random
  death_link: false
  # Goal-specific thresholds (generators_goal_*, score_goal_exponent, ...) and trap/filler
  # magnitudes (freeze_trap_seconds, overdrive_seconds, ...) all have sensible defaults —
  # see the full template for the complete list.
```

## Repository layout

```
apworld/revolution_idle/   the Archipelago world (Python)
mod/RevolutionIdleAP/      the BepInEx plugin (C#) + SETUP.md (toolchain notes)
build_apworld.py           build dist/revolution_idle.apworld
build_release.py           assemble dist/RevolutionIdleAP-vX.Y.Z.zip
reset-save.ps1             back up / wipe / restore the save for clean runs
DESIGN.md                  design + reverse-engineering notes
CHANGELOG.md               version history
```

## Building from source

Requires Python 3.11–3.13, the .NET SDK (8 or 9), and a local copy of the game.

```sh
# apworld
python build_apworld.py            # -> dist/revolution_idle.apworld

# mod (deploys into the game's BepInEx/plugins, then assemble the release)
dotnet build mod/RevolutionIdleAP -c Release
python build_release.py            # -> dist/RevolutionIdleAP-vX.Y.Z.zip
```

The mod links against BepInEx + IL2CPP interop assemblies from your game install. Because the public
BepInEx builds don't support this game's Unity version out of the box, see
[`mod/RevolutionIdleAP/SETUP.md`](mod/RevolutionIdleAP/SETUP.md) for the one-time toolchain setup
(building Cpp2IL from source so interop generation works on metadata v31).

## Status

Playable end-to-end: generates solvable seeds, connects, applies all unlocks, and sends achievement
and generator checks. Known limitations (see [`CHANGELOG.md`](CHANGELOG.md)):

- Death Link is accepted but does nothing (no death mechanic).
- The `equality` goal trigger is implemented but not yet confirmed by a full deep playthrough.
- Logic gates by prestige tier, not per individual side system.
- In **AP Mode**, the game logs a harmless cloud-sync error on launch (its offline Nakama/Steam auth
  fails and the game catches it — AP play is unaffected). It can't be suppressed from the mod because
  it runs in the game's async startup before/outside anything the mod can intercept.

## Credits & license

Original code is MIT (see [`LICENSE`](LICENSE)). Built on
[Archipelago](https://github.com/ArchipelagoMW/Archipelago),
[Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net),
[BepInEx](https://github.com/BepInEx/BepInEx), and
[Cpp2IL](https://github.com/SamboyCoding/Cpp2IL); bundled binaries keep their own licenses.

Revolution Idle © Oni Gaming. Unofficial, non-commercial fan project — not affiliated with or
endorsed by Oni Gaming.
