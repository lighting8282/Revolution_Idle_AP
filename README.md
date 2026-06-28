# Revolution Idle — Archipelago

Play [Revolution Idle](https://store.steampowered.com/app/2763740/) as an
[Archipelago](https://archipelago.gg) multiworld. Your prestige layers and side systems start
**locked** and are unlocked by items from the multiworld; completing the game's **520 achievements**
sends checks out to other players.

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
builds mod support, ~2–4 min), then edit
`BepInEx/config/com.jontrnka.revolutionidle.ap.cfg` with your server/slot.

## Features

- 520 achievement location checks; 35 items (layer unlocks, side-system unlocks, automation, filler, traps).
- **Tiered logic** mirroring the game's own progression: base → Infinity → Eternity → Unity, each
  gated by its unlock item (derived from the game's `Const.ACH_RANGES`).
- Goals: `unity` (reach the Unity layer) and `equality` (completionist).
- Full in-game integration: receiving an item unlocks the matching system (30 unlocks across 9 game
  classes); completing an achievement sends its check; goals are auto-detected.
- Save/seed binding warns if you connect a save that belongs to a different seed.
- Options: goal, trap chance, death link (no-op — the game has no death mechanic).

### YAML example

```yaml
name: Player1
game: Revolution Idle
Revolution Idle:
  goal: unity        # or: equality
  trap_chance: 10
  death_link: false
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

Playable end-to-end: generates solvable seeds, connects, applies all unlocks, and sends all
achievement checks. Known limitations (see [`CHANGELOG.md`](CHANGELOG.md)):

- Death Link is accepted but does nothing (no death mechanic).
- The `equality` goal trigger is implemented but not yet confirmed by a full deep playthrough.
- Logic gates by prestige tier, not per individual side system.
- Secret achievements aren't used as checks.

## Credits & license

Original code is MIT (see [`LICENSE`](LICENSE)). Built on
[Archipelago](https://github.com/ArchipelagoMW/Archipelago),
[Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net),
[BepInEx](https://github.com/BepInEx/BepInEx), and
[Cpp2IL](https://github.com/SamboyCoding/Cpp2IL); bundled binaries keep their own licenses.

Revolution Idle © Oni Gaming. Unofficial, non-commercial fan project — not affiliated with or
endorsed by Oni Gaming.
