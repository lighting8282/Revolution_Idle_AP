# Revolution Idle — Archipelago

Play [Revolution Idle](https://store.steampowered.com/app/2763740/) as an
[Archipelago](https://archipelago.gg) multiworld. Your prestige layers and side systems start
locked and are unlocked by items from the multiworld; completing the game's achievements sends
checks out to other players.

This release has two parts: the **apworld** (for Archipelago) and the **game mod** (for Revolution
Idle). The mod ships with a pre-patched BepInEx because stock BepInEx cannot load this game's Unity
build — you don't need to install BepInEx yourself.

---

## 1. Install the apworld

Copy `apworld/revolution_idle.apworld` into your Archipelago installation's **`custom_worlds`**
folder. You can now generate games and roll YAMLs for "Revolution Idle".

Options:
- **goal** — `unity` (reach the Unity layer) or `equality` (reach Unity, collect every unlock, and
  earn Equality currency — a long/completionist goal).
- **trap_chance** — % of filler replaced by traps.
- **death_link** — accepted, but Revolution Idle has no death mechanic, so it's effectively off.

## 2. Install the game mod

1. Close Revolution Idle.
2. Extract everything inside **`Install-into-Game-Folder/`** directly into your game folder — the one
   containing `Revolution Idle.exe` (e.g. `...\steamapps\common\Revolution Idle`). When done, that
   folder should contain `winhttp.dll`, `dotnet\`, and `BepInEx\` next to the exe.
3. Launch the game once and wait. The first launch builds mod-support files and can take **2–4
   minutes** before the game window appears — this is normal and only happens once.

## 3. Connect to a server

After the first launch, edit
`BepInEx\config\com.jontrnka.revolutionidle.ap.cfg` and set your room details:

```ini
[Connection]
Host = archipelago.gg
Port = 38281
Slot = YourSlotName
Password =
Enabled = true
```

Restart the game to connect. Check `BepInEx\LogOutput.log` for `[AP] connected.` to confirm.

## 4. Start a fresh save (important)

Use a **new, empty save** for each seed so progression is actually gated by the randomizer:

- In-game: on the save screen choose **"Start a new save"**, or
- Run the included `reset-save.ps1` (backs up your existing save first; `-Restore` to undo).

The mod warns in the log if you connect a save that was previously used with a different seed.

---

## Notes & troubleshooting

- **First launch is slow / window doesn't appear for a few minutes** — expected (one-time setup).
- **Nothing connects** — check `Host`/`Slot` in the config, and that `Enabled = true`. The server
  must be running. Connection status is in `BepInEx\LogOutput.log`.
- **Achievements aren't sending** — make sure you're on a fresh save and connected; already-unlocked
  achievements are re-sent on connect.
- Secret achievements are not used as checks.
- The mod never edits your save file on disk; it only changes the running game in memory.

## Credits & licenses

- [Archipelago](https://github.com/ArchipelagoMW/Archipelago) and
  [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net)
- [BepInEx](https://github.com/BepInEx/BepInEx) (bundled) and
  [Cpp2IL](https://github.com/SamboyCoding/Cpp2IL) (rebuilt for this game's Unity version)
- Revolution Idle © Oni Gaming. This is an unofficial fan mod.
