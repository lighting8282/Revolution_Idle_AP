# Revolution Idle — Archipelago v0.2.0

First public release. Play [Revolution Idle](https://store.steampowered.com/app/2763740/) as an
[Archipelago](https://archipelago.gg) multiworld: your prestige layers and side systems start
locked and are unlocked by the multiworld, and clearing the game's 520 achievements sends checks
to other players.

## Downloads

| File | What it's for |
|---|---|
| **`RevolutionIdleAP-v0.2.0.zip`** | Everything — apworld + game mod + patched BepInEx + docs |
| `revolution_idle.apworld` | Just the apworld, if you only need to generate |

> The mod **bundles a pre-patched BepInEx**. Stock BepInEx cannot load this game's Unity build, so
> please use the bundled one — don't install BepInEx separately.

## Quick start

1. **apworld** → copy `apworld/revolution_idle.apworld` into your Archipelago `custom_worlds/` folder.
2. **mod** → close the game, then extract everything in `Install-into-Game-Folder/` into your game
   folder (the one with `Revolution Idle.exe`).
3. **first launch** → run the game once and wait 2–4 minutes (one-time mod-support build).
4. **connect** → edit `BepInEx\config\com.jontrnka.revolutionidle.ap.cfg` (Host / Port / Slot),
   restart, and check `BepInEx\LogOutput.log` for `[AP] connected.`
5. **fresh save** → use "Start a new save" in-game (or `reset-save.ps1`) for each seed.

Full instructions are in the bundled `README.md`.

## Features

- **520 achievement checks** and **35 items** (layer unlocks, side-system unlocks, automation,
  filler/traps).
- **Real tiered logic** mirroring the game's own progression: base → Infinity → Eternity → Unity,
  each gated by its unlock item.
- **Two goals** — `unity` (reach the Unity layer) and `equality` (completionist: reach Unity, collect
  every unlock, earn Equality currency).
- **Full in-game integration** — receiving an item unlocks the matching system (30 unlocks across the
  whole game); completing an achievement sends its check; goals are detected automatically.
- **Save/seed safety** — the mod warns if you connect a save that belongs to a different seed.
- Options: goal, trap chance, death link.

## YAML example

```yaml
name: Player1
game: Revolution Idle
Revolution Idle:
  goal: unity        # or: equality
  trap_chance: 10
  death_link: false
```

## Known limitations

- Death Link is accepted but does nothing — Revolution Idle has no death mechanic.
- The `equality` goal is long/completionist; its in-game trigger is implemented and validated in
  code but hasn't yet been confirmed by a full deep playthrough.
- Logic gates by prestige tier, not by individual side system.
- Secret achievements are not used as checks.

## Compatibility

- Requires the Steam version of Revolution Idle (built/tested against Unity 2022.3.62f3).
- Archipelago 0.6.x.

## Credits

Built on [Archipelago](https://github.com/ArchipelagoMW/Archipelago),
[Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net),
[BepInEx](https://github.com/BepInEx/BepInEx), and
[Cpp2IL](https://github.com/SamboyCoding/Cpp2IL) (rebuilt for this game's Unity version).
Revolution Idle © Oni Gaming. Unofficial fan project.

---

_See `CHANGELOG.md` for the full change list._
