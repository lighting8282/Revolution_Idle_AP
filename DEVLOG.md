# Development Log

The story of how this project got built, in order — including the parts that didn't work. For the
current architecture, see [`DESIGN.md`](DESIGN.md); for the user-facing feature history, see
[`CHANGELOG.md`](CHANGELOG.md). This document is the "how," not the "what."

## Phase 0 — Reverse engineering (2026-06-27)

Revolution Idle has no mod API and no source; everything here started from a Steam IL2CPP build.
The first question was simply *"can we make an AP version of this game?"*, which meant answering
three things before any design could happen: what does the save look like, how does progression
gate, and can a mod actually reach into the running game.

- **Dumped the assembly** with Il2CppDumper to get `dump.cs` / `il2cpp.h` / `script.json` — enough
  to see every class, field, and method signature (though not method *bodies*).
- Confirmed the shape of the save (`GameData`, `InfinityData`/`EternityData`/`UnityData`,
  `achByte`, `unlockedAch`) and found the key constants: `ACH_COUNT = 520`, `ACH_SECRET_COUNT = 55`,
  `REV_COUNT = 10`, `GEN_COUNT = 10`.
- Needed **method bodies** next, to decode exactly how each layer's unlock check worked (dev-override?
  currency threshold? achievement flag?) — that meant **Cpp2IL**. The bundled Cpp2IL build didn't
  support this game's metadata version (v31); the fix was building Cpp2IL from source against
  AsmResolver 6.0.0-beta.5 + AssetRipper.CIL, which took a few iterations to get a consistent set of
  assembly versions. Once that worked, the ISIL dumps showed each `get_XxxUnlocked` as
  `dev-override OR currency >= threshold OR achByte[N] == 1`.

This phase produced the first version of `DESIGN.md` and answered the feasibility question: yes,
this game has a big, clean check/item pool (520 achievements, 10 generators, a linear prestige
tower with independently-gateable side systems), and the mod side was buildable.

## Phase 1 — First playable loop (2026-06-27 – 2026-06-28)

- Scaffolded the apworld (`options`/`items`/`locations`/`regions`/`rules`/`world`), modeled on an
  existing simple world (`worlds/apquest`), and confirmed it generated valid, solvable seeds.
- Built a minimal BepInEx plugin as a **smoke test**: would Harmony postfixes even fire against
  IL2CPP getters, and would the Anti-Cheat Toolkit block it? Confirmed both answers were good news —
  ACTk didn't interfere, and a postfix on `get_PrestigeUnlocked` worked.
- From there: **granting** items became "postfix each `get_XxxUnlocked` to return true once AP
  delivered it" (uniform regardless of the underlying native gate), and **detecting checks** became
  "postfix the single `GameData.UnlockAchievement(int id)` chokepoint that all 520 achievements flow
  through" — one hook, no polling needed.
- First tiered logic: achievements bucketed into regions using the game's own `Const.ACH_RANGES`
  boundaries (0–29 Base, 30–69 Infinity, 70–160 Eternity, 161–519 Unity).

By the end of this phase the mod could connect, apply items, send achievement checks, and detect the
`unity`/`equality` goals — a genuinely playable first cut.

## Phase 2 — Becoming a real release (2026-06-28)

Turned the working prototype into something distributable: a packaged release zip (apworld + mod +
a **pre-patched BepInEx**, since stock BepInEx can't load this game's Unity build), a GitHub repo,
README/CHANGELOG/LICENSE, and a build pipeline (`build_apworld.py`, `build_release.py`).

This phase also produced the first of a recurring theme in this project: **git identity mixups**.
Commits initially landed under the wrong GitHub account; fixing that meant setting the right
`user.name`/`user.email` and amending/rewriting history — a problem that would resurface later (see
Phase 8).

Options grew quickly here too: `achievement_pool` (a single dial for how many of the 520 became
checks), `progressive_layers`, `trap_chance`, and real filler/trap effects (Score Boost = +60s of
income, Slowdown Trap = −120s) instead of inert placeholders.

## Phase 3 — The cloud-save problem, and AP Mode (2026-06-28 – 2026-06-29)

The single hardest problem in this project surfaced here: **the game restores its save from Nakama
cloud on every launch.** That defeated two things AP needs — starting fresh for a new seed, and
keeping AP progress from contaminating a normal save. Wiping the local save or a reset script did
nothing, because the cloud just re-synced it back.

The fix that stuck was **AP Mode**: force the game offline (`IsSessionOn = false`) so it never
touches the cloud, and isolate the save under different keys. Getting the isolation right took a
real wrong turn:

- **First attempt:** patch `ObscuredPrefs.SetString` to remap keys. This *looked* right but didn't
  actually intercept the game's real save path — the game saves via `ObscuredPrefs.Set<T>` /
  `SetRawValue`, which internally calls `UnityEngine.PlayerPrefs`. The normal save got silently
  overwritten during testing.
- **Fix:** patch `UnityEngine.PlayerPrefs` directly (`SetString`/`GetString`/`HasKey`/`DeleteKey`),
  remapping `game_data`/`inventory` → `..._ap` only when AP Mode is on. Verified afterward by
  comparing the normal save's hash before/after — unchanged — while a new `game_data_ap` key
  appeared.

On top of that: a fresh-per-seed marker file (so a *new* seed wipes the AP save once, but
reconnecting to the same seed resumes), and later an in-game **F1 menu** toggle for AP Mode that
auto-restarts the game so the new setting actually takes effect (Unity doesn't hot-swap this kind of
state).

## Phase 4 — Filling out checks and options (2026-06-29)

With the core loop solid, this became the highest-throughput phase: generator ownership checks (the
10 base generators, each a check once owned), then generator **level** checks (a check every N
levels as each generator climbs 1→100), per-tier achievement counts (replacing the single
`achievement_pool` dial with `achievements_base/infinity/eternity/unity`), and an opt-in toggle for
the 55 secret achievements (gated behind Unity, since their real requirements are unknown).

Goals grew from two to eight over a few iterations: `infinity`/`eternity` alongside `unity`/
`equality`, then a generator-count-and-level goal, then `score`/`prestige_mult` (via `BigDouble`'s
`.Exponent`, so "reach 10^N" is just an integer dial), and `achievement_count` (with its own
dynamic goal-region gating based on how many achievements the target implies are reachable).

Traps expanded from one (Slowdown) to four: **Freeze** (`timeScale = 0`) and **Lag**
(`timeScale = 0.5`) needed a genuine design trick — the normal tick loop runs on *scaled* time, so a
freeze-driven-by-scaled-time could never un-freeze itself. The fix was driving the countdown off
`Time.unscaledDeltaTime` in a per-frame update instead. **Generator Drain** rounded out the set.
Fillers grew the same way, with **Generator Boost** and **Overdrive** as direct mirrors of two of
the traps, plus **Income Jackpot** as a bigger one-shot income grant.

## Phase 5 — Quality of life (2026-06-30)

Three player-facing improvements landed close together: the **F1 menu remembers your last
connection** (host/port/slot/password written back to the BepInEx config on connect), an **F2
message-feed overlay** (a bottom-left, color-coded, auto-fading feed of checks/items/joins/hints/
chat/goals, sourced from the AP session's own `MessageLog`), and **achievement-panel sync** — marking
AP-checked achievements as unlocked in the game's own achievement list (visual only, no reward) so
resuming a seed or a remote `!collect` doesn't leave the panel looking stale.

## Phase 6 — The Nakama error that wouldn't die (2026-06-30)

AP Mode's offline trick has a side effect: the game still *tries* to authenticate with Steam/Nakama
at launch, fails because it's forced offline, and throws a `NullReferenceException` that prints to
Unity's log in red. It's cosmetic — the game catches it and AP play is unaffected — but three real
attempts were made to suppress it before concluding it can't be done cleanly from the mod:

1. Skip the async `NakamaManager.SteamAuth`/`Initialize` methods entirely. **Didn't work** — the
   "skipped" log line never printed even when the code path was reached, confirming Harmony can't
   reliably intercept IL2CPP *async* method bodies.
2. Force `NakamaManager.HasInternet = false` (a plain bool getter, which *does* patch reliably).
   **Didn't work** — the crash's real trigger turned out to be `NakamaManager.InternalAwake()`, which
   doesn't check that flag before authenticating.
3. Skip `InternalAwake` itself. **Made it worse** — that method also runs the game's own Steam
   session init, and skipping it produced a *different*, uglier crash
   (`GetAuthSessionTicketAsync`) via a second, separate connect path (`Launcher.Start → Launch`).
   Reverted.

The conclusion: the cloud connect has multiple entry points woven through async startup code that
Harmony fundamentally can't safely intercept without also breaking Steam init. It's documented as a
known, harmless limitation rather than chased further.

## Phase 7 — Ascension, and a design correction (2026-07-04)

A pitch for adding "ascension" as a check/goal type (the game's per-revolution level-up counter,
`Revolution.ascension`) replaced the generator-level goal and, briefly, the generator-level checks —
then generator-level checks were restored alongside ascension once it became clear both were wanted,
rather than one replacing the other.

The ascension goal's default (5000 total levels) also got corrected using an unusual but effective
calibration method: searching the game's own **achievement text** (extracted via UnityPy from the
Unity Localization string tables) for anything ascension-related. Achievement #64, *"Ascended a
Lot!"*, requires all 10 colors at Ascension 40+ — a sum of 400 — and sits in the *Infinity* tier,
i.e. relatively early-mid game. The original default of 5000 was over 12× what the game itself calls
"a lot"; it was lowered to 2000.

A second, more structural bug was caught the same way a pitch-post review often catches things:
**by writing an accurate summary and fact-checking it against the actual code.** The claim "all 520
achievements" prompted the question "is that tunable per-tier independent of the goal?" — and the
answer turned out to be an oversight: `achievements_eternity`/`achievements_unity` had no relationship
to the chosen `goal` at all, so `goal: infinity` with default achievement counts still silently
required reaching Eternity/Unity just to fill the player's own achievement locations, defeating the
entire point of picking a short goal. Fixed with `scale_achievements_to_goal` (on by default): tiers
deeper than the goal requires are now skipped outright, with an escape hatch for anyone who
deliberately wants a shallow goal with deep achievement variety.

## Phase 8 — Attribution, twice (2026-06-30, 2026-07-04)

Two separate GitHub attribution problems came up, worth recording because they looked similar but
had different causes:

- **jon-weber1 showing as a contributor:** every commit was already authored as `lighting8282` by
  name, but the *email* on those commits (`teamftkd@gmail.com`) was registered to a different GitHub
  account. GitHub attributes by email, not by the name string. Fixed by rewriting all commit/tag
  authorship to an email actually verified on the `lighting8282` account, then force-pushing.
- **"claude" showing as a contributor, days later:** this one turned out to be a **stale cache** —
  GitHub's Contributors graph is computed separately from the live git data and doesn't always
  recompute immediately after a force-push/history rewrite. The `stats/contributors` API returning
  `202 Accepted` (GitHub's "still recomputing" signal) confirmed it wasn't a real, current
  attribution problem, just a UI lagging behind the phase-8-earlier rewrite.

The lesson for future maintainers: if the Contributors graph looks wrong, check the actual commit
authors (`git log --format='%an <%ae>'`) and the collaborators API before assuming anything is
actually misconfigured — the graph itself can be stale.

## Where things stand

Playable end-to-end: 8 goals, a large tunable check pool (achievements per-tier + secrets, generator
ownership + levels, ascension milestones), 4 traps and 4 fillers with tunable magnitudes, an in-game
connection menu and activity feed, and an AP Mode that keeps AP play fully separate from normal
cloud play. The one open, accepted limitation is the cosmetic Nakama error in AP Mode (Phase 6).
