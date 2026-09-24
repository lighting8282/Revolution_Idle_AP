# Revolution Idle Setup Guide

Play [Revolution Idle](https://store.steampowered.com/app/2763740/) as an Archipelago multiworld.
Your prestige layers and side systems start **locked** and are unlocked by items from the
multiworld; completing achievements and owning/leveling generators sends checks to other players.

## Required Software

- [Archipelago](https://github.com/ArchipelagoMW/Archipelago/releases) 0.6.7 or newer
- Revolution Idle (Steam, Windows)
- The Revolution Idle mod, from the
  [latest release](https://github.com/lighting8282/Revolution_Idle_AP/releases/latest)

The release page has two downloads:

| File | Who needs it |
|---|---|
| `RevolutionIdleAP-vX.Y.Z.zip` | **Players.** The apworld, the game mod, a pre-patched BepInEx, a YAML template, and setup instructions. |
| `revolution_idle.apworld` | **Hosts who only generate** the multiworld and don't play the game themselves. |

> The mod bundles a **pre-patched BepInEx** — stock BepInEx builds cannot load this game's Unity
> version. Don't substitute your own.

## Installing the apworld

Double-click `revolution_idle.apworld` (Archipelago installs it), or copy it into Archipelago's
`custom_worlds/` folder.

If you're only generating, you're done — the rest of this guide is for players.

## Installing the mod

1. Close Revolution Idle.
2. Unzip `RevolutionIdleAP-vX.Y.Z.zip`.
3. Copy the **contents** of its `Install-into-Game-Folder/` directory into your Revolution Idle
   install folder (Steam → right-click the game → *Manage* → *Browse local files*), merging with
   what's already there. That folder should end up with `winhttp.dll`, `dotnet\` and `BepInEx\`
   sitting next to `Revolution Idle.exe`.
4. Launch the game once and wait. The **first** launch builds mod-support files and can take
   **2–4 minutes** before the window appears. Later launches are normal speed.

## Configuring your YAML

A template for the current version ships in the release (`Revolution Idle.yaml`, also a separate
download). Archipelago's own "Generate Template Options" produces it too, once the apworld is
installed.

The options worth knowing about:

- **goal** — `unity` (default), `equality`, `infinity`, `eternity`, `ascension`, `score`,
  `prestige_mult`, or `achievement_count`. Each has its own threshold option where relevant.
- **achievements_base / _infinity / _eternity / _unity** — how many achievements from each tier
  become checks (defaults = all 675). These set the number of *checks*; they do not affect winning.
- **secret_achievements** — adds the 58 secret achievements (off by default).
- **generator_level_count / generator_level_interval** — optional level-milestone checks per
  generator.
- **ascension_check_count / ascension_check_interval** — optional ascension-milestone checks.
- **trap_chance**, and per-effect magnitudes for every trap and filler.
- **revolution_speed_multiplier** — 10× by default, because a 1× idle grind makes for a very long
  multiworld. Set it to 1 for vanilla pacing.

## Joining a multiworld

**AP Mode is required.** It runs the game offline with an isolated save, so a multiworld run never
touches your normal cloud save — and so your existing progress is never sent to the server as
checks. The mod refuses to connect without it.

1. Launch the game with the **`Play Revolution Idle (AP)`** shortcut in your game folder. (AP Mode
   comes from how you launch: that shortcut passes `--archipelago`. Launching any other way,
   including from Steam, is ordinary play on your normal save. To use AP Mode from Steam, put
   `--archipelago` in the game's launch options.)
2. Press **F1** for the Archipelago Connection menu.
3. Enter the server address, port, slot name, and password if there is one, then click **Connect**.
   Your values are remembered for next time.

Each new seed automatically starts a fresh AP save; reconnecting to the same seed resumes it.

Press **F2** to toggle the message feed (checks, items, hints, chat).

## Troubleshooting

- **The first launch takes minutes / no window appears** — expected, one time only.
- **No Connect button in the F1 menu** — you're not in AP Mode. Use the AP shortcut, or the
  in-game AP Mode switch (it restarts the game for you).
- **Connected, but nothing is being sent** — the menu will say "Save not verified". The mod only
  sends from a verified AP save; relaunch with the AP shortcut. `BepInEx\LogOutput.log` gives the
  reason.
- **A red cloud/session error on launch in AP Mode** — harmless. The game's own cloud login fails
  because AP Mode is offline, and the game catches it.
- **Steam achievements** — deliberately blocked while AP is driving the game. An AP run is a
  sandboxed save and shouldn't award real achievements.
