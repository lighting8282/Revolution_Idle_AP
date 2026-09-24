# Revolution Idle — Archipelago

Play [Revolution Idle](https://store.steampowered.com/app/2763740/) as an
[Archipelago](https://archipelago.gg) multiworld. Your prestige layers and side systems start
locked and are unlocked by items from the multiworld; completing achievements and owning/leveling
generators sends checks out to other players.

This release has two parts: the **apworld** (for Archipelago) and the **game mod** (for Revolution
Idle). The mod ships with a pre-patched BepInEx because stock BepInEx cannot load this game's Unity
build — you don't need to install BepInEx yourself.

**What's in this zip**

| | |
|---|---|
| `apworld/revolution_idle.apworld` | the Archipelago world |
| `Install-into-Game-Folder/` | the mod + patched BepInEx + AP launchers |
| `Revolution Idle.yaml` | options template for this version |
| `reset-save.ps1` | back up / wipe / restore the save (rarely needed — see below) |

---

## 1. Install the apworld

Copy `apworld/revolution_idle.apworld` into your Archipelago installation's **`custom_worlds`**
folder (or just double-click it). You can now generate games for "Revolution Idle".

Use the included `Revolution Idle.yaml` as your starting options file. Highlights:

- **goal** — `unity` (default), `equality`, `infinity`, `eternity`, `ascension`, `score`,
  `prestige_mult`, `achievement_count`. Each has its own threshold option where relevant.
- **achievements_base / _infinity / _eternity / _unity** — how many achievements of each tier become
  checks (defaults = all 675). These control the number of *checks*, not the win condition.
- **secret_achievements** — adds the 58 secret achievements (off by default).
- **generator_level_count / generator_level_interval** — optional per-generator level milestones.
- **ascension_check_count / ascension_check_interval** — optional ascension milestones.
- **revolution_speed_multiplier** — **10× by default**, since a 1× idle grind makes for a very long
  multiworld. Set it to `1` for vanilla pacing.
- **trap_chance** — % of filler replaced by traps; every trap and filler magnitude is tunable.
- **death_link** — accepted, but the game has no death mechanic, so it does nothing.

## 2. Install the game mod

1. Close Revolution Idle.
2. Extract everything inside **`Install-into-Game-Folder/`** directly into your game folder — the one
   containing `Revolution Idle.exe` (e.g. `...\steamapps\common\Revolution Idle`). When done, that
   folder should contain `winhttp.dll`, `dotnet\`, and `BepInEx\` next to the exe.
3. Launch the game once and wait. The first launch builds mod-support files and can take **2–4
   minutes** before the game window appears — normal, and only once.

## 3. Play in AP Mode (required)

Revolution Idle keeps your save in the **cloud**. **AP Mode** runs the game offline with its own
isolated save, so:

- Your normal (cloud) save is never touched — it's effectively a separate copy of the game.
- Each **new seed starts fresh** automatically; reconnecting to the **same** seed resumes it.
- Your existing progress can never be sent to the multiworld as checks.

That last point is why **AP Mode is required to connect** — the mod won't offer a Connect button
without it.

**AP Mode comes from how you launch the game:**

- **`Play Revolution Idle (AP).bat`** (in your game folder) → AP Mode.
- Anything else, including launching from Steam → normal play on your usual save.
- Already in-game? Press **F1** and use the **AP Mode** switch — it restarts the game for you.
- Want AP Mode straight from Steam? Put `--archipelago` in the game's launch options.

## 4. Connect

1. Launch with the **AP** shortcut.
2. Press **F1** for the **Archipelago Connection** menu.
3. Enter Hostname, Port, Slot Name (and Password if any), then click **Connect**. Your values are
   remembered. The status line at the bottom shows what's happening.

Press **F1** again to hide the menu, **F2** to toggle the live message feed (checks, items, hints,
chat), and **F3** to write a diagnostic dump if you're reporting a problem.

Prefer to connect automatically? Set `Host`/`Port`/`Slot` and leave `Enabled = true` under
`[Connection]` in `BepInEx\config\com.lighting8282.revolutionidle.ap.cfg`. Auto-connect still only
happens in AP Mode.

---

## Things worth knowing

- **Steam achievements are blocked during AP play.** An AP run is a sandboxed save, so it doesn't
  award real Steam achievements. (Normal play is unaffected.)
- **The in-game achievement panel** marks achievements you've checked in AP. That's display only —
  no rewards are granted and nothing is written to your save.
- **Secret achievements** can be used as checks via `secret_achievements`. They sit behind Unity, so
  with the default `scale_achievements_to_goal` they're skipped for goals shallower than Unity.
- **`reset-save.ps1`** is rarely needed now that AP Mode isolates saves. It backs up first, but note
  it wipes both the normal and AP saves.
- The mod never edits your save file on disk; it only changes the running game in memory.

## Troubleshooting

- **First launch is slow / no window for a few minutes** — expected, one-time setup.
- **No Connect button in the F1 menu** — you're not in AP Mode. Use the AP shortcut or the in-game
  switch.
- **Connected but nothing is sending** — the menu shows "Save not verified". The mod only sends from
  a verified AP save; relaunch with the AP shortcut. The reason is in `BepInEx\LogOutput.log`.
- **Can't connect at all** — check Hostname (no port on the end), Port, and Slot Name against the
  room page. Connection status is in `BepInEx\LogOutput.log`.
- **A goal that won't trigger** — the log line at connect spells out the exact target, e.g.
  `goal=7 (achievement_count: unlock >= 250 achievements in-game)`. Goal settings are fixed when the
  seed is generated, so changing your YAML needs a new seed.
- **Red cloud/session error on launch in AP Mode** — harmless. The game's own cloud login fails
  because AP Mode is offline, and the game catches it.

## Credits & licenses

- [Archipelago](https://github.com/ArchipelagoMW/Archipelago) and
  [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net)
- [BepInEx](https://github.com/BepInEx/BepInEx) (bundled) and
  [Cpp2IL](https://github.com/SamboyCoding/Cpp2IL) (rebuilt for this game's Unity version)
- Revolution Idle © Oni Gaming. This is an unofficial fan mod.
