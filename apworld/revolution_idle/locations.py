from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Location, LocationProgressType

from . import items
from .items import RevolutionIdleItem

if TYPE_CHECKING:
    from .world import RevolutionIdleWorld

# Mirrors GameData.ACH_COUNT (normal achievements). Location id = ACH_ID_BASE + game_id, which also
# works for secrets (game ids 10000-10057 -> location ids 20000-20057).
# Game 1.077 raised this 520 -> 675. Achievements are append-only (ids 0-519 are byte-identical), so
# existing location ids stay valid and the new ones simply extend the map.
ACH_COUNT = 675
ACH_ID_BASE = 10_000

# Secret achievements (Const.ACH_SECRET_COUNT = 58, game ids 10000..10057). Opt-in via option.
SECRET_COUNT = 58
SECRET_GAME_ID_BASE = 10_000

# Base-layer generators (GameData.infinity.generators, GEN_COUNT=10). Owning each one is a check.
GEN_COUNT = 10
GEN_ID_BASE = 30_000

# Per-generator level checks. Generators do NOT cap at level 100 — a live save reports
# maxAmount = 1e64 — so milestones are indexed rather than keyed to an absolute level:
# milestone k on a generator = that generator reaching level k * generator_level_interval.
# (Same shape as the ascension milestones below, and for the same reason: AP needs a fixed
# location_name_to_id that cannot depend on the chosen interval.)
GEN_LEVEL_ID_BASE = 40_000
GEN_LEVEL_MAX_MILESTONES = 100   # per generator; id space 40001..41000, same size as before

# Ascension-milestone checks. Total ascension = sum of GameData.revolutions[i].ascension. A check is
# awarded for every `ascension_check_interval` total levels, up to `ascension_check_count` of them.
# Locations are named/ID'd by index (milestone k = at level k * interval) so the id map is stable
# regardless of the chosen interval.
ASC_ID_BASE = 50_000
ASC_MAX_MILESTONES = 200


def gen_location_name(index: int) -> str:
    return f"Generator {index + 1}"


def gen_level_location_name(index: int, k: int) -> str:
    return f"Generator {index + 1} Level Milestone {k}"


def gen_level_location_id(index: int, k: int) -> int:
    # 40000 + gen*100 + k -> stable, collision-free (gen 0 k=1 = 40001 .. gen 9 k=100 = 41000)
    return GEN_LEVEL_ID_BASE + index * GEN_LEVEL_MAX_MILESTONES + k


def asc_milestone_location_name(k: int) -> str:
    return f"Ascension Milestone {k}"


def asc_milestone_location_id(k: int) -> int:
    return ASC_ID_BASE + k  # k = 1..ASC_MAX_MILESTONES -> 50001..50200

# Achievement-id tiers from Const.ACH_RANGES, mapped to the tower regions they require and the
# per-tier count option that controls how many of them become checks.
# (start_id, end_id_exclusive, region_name, option_attr)
TIERS: list[tuple[int, int, str, str]] = [
    (0, 30, "Menu", "achievements_base"),         # base/prestige tier
    (30, 70, "Infinity", "achievements_infinity"),
    (70, 161, "Eternity", "achievements_eternity"),
    (161, 675, "Unity", "achievements_unity"),   # 1.077 put all 155 new achievements in this tier
]

# Secret achievements have unknown/obscure requirements; gate them behind the deepest layer so AP
# only ever expects them once everything is unlocked (a safe over-approximation of reachability).
SECRET_REGION = "Unity"

# How deep each tower region sits, for comparing against how deep the chosen goal requires.
REGION_DEPTH: dict[str, int] = {"Menu": 0, "Infinity": 1, "Eternity": 2, "Unity": 3}


def ach_location_name(game_id: int) -> str:
    return f"Achievement #{game_id}"


def secret_location_name(index: int) -> str:
    return f"Secret Achievement {index + 1}"


def secret_location_id(index: int) -> int:
    # game id 10000+index -> location id ACH_ID_BASE + game_id = 20000+index
    return ACH_ID_BASE + SECRET_GAME_ID_BASE + index


# Full, stable location_name_to_id (every location that could ever exist).
LOCATION_NAME_TO_ID: dict[str, int] = {
    ach_location_name(i): ACH_ID_BASE + i for i in range(ACH_COUNT)
}
LOCATION_NAME_TO_ID.update({
    secret_location_name(i): secret_location_id(i) for i in range(SECRET_COUNT)
})
LOCATION_NAME_TO_ID.update({
    gen_location_name(i): GEN_ID_BASE + i for i in range(GEN_COUNT)
})
# Every possible generator-level milestone (by index); only `generator_level_count` are created
# per seed, but the full id map must be stable.
LOCATION_NAME_TO_ID.update({
    gen_level_location_name(i, k): gen_level_location_id(i, k)
    for i in range(GEN_COUNT)
    for k in range(1, GEN_LEVEL_MAX_MILESTONES + 1)
})
# Every possible ascension-milestone location (by index); only `ascension_check_count` are created
# per seed, but the full id map must be stable.
LOCATION_NAME_TO_ID.update({
    asc_milestone_location_name(k): asc_milestone_location_id(k)
    for k in range(1, ASC_MAX_MILESTONES + 1)
})


class RevolutionIdleLocation(Location):
    game = "Revolution Idle"


def _deeper_than_goal(world: RevolutionIdleWorld, region_name: str) -> bool:
    """Should achievements in `region_name` be skipped for this seed?

    True when scale_achievements_to_goal is on and the region sits deeper in the tower than the
    chosen goal requires — i.e. the player would have to progress past their own goal purely to
    complete their own checks.
    """
    if not world.options.scale_achievements_to_goal:
        return False
    return REGION_DEPTH[region_name] > REGION_DEPTH[world.goal_region_name]


def selected_achievement_ids(world: RevolutionIdleWorld) -> dict[str, list[int]]:
    """Per tier, sample the requested number of achievement ids (deterministically via world.random).

    If scale_achievements_to_goal is on (default), tiers deeper than what the chosen goal requires
    are skipped entirely (0 achievements) regardless of their configured count — so e.g. goal:infinity
    doesn't force you to also reach Eternity/Unity just to fill your own Eternity/Unity achievement
    checks. Turn the option off to keep tiers fully independent of the goal.

    Returns {region_name: [game_ids]}."""
    rng = world.random
    by_region: dict[str, list[int]] = {}
    for start, end, region_name, option_attr in TIERS:
        if _deeper_than_goal(world, region_name):
            continue
        n = getattr(world.options, option_attr).value
        ids = list(range(start, end))
        if n < len(ids):
            rng.shuffle(ids)
            ids = sorted(ids[:n])
        by_region.setdefault(region_name, []).extend(ids)
    return by_region


def create_all_locations(world: RevolutionIdleWorld) -> None:
    by_region = selected_achievement_ids(world)

    for region_name, ids in by_region.items():
        names_to_ids = {ach_location_name(gid): ACH_ID_BASE + gid for gid in ids}
        if names_to_ids:
            world.get_region(region_name).add_locations(names_to_ids, RevolutionIdleLocation)

    # Secret achievements (opt-in) — gated behind the deepest layer, so they obey
    # scale_achievements_to_goal exactly like the deeper tiers do. Without this, `goal: infinity`
    # plus secrets put 58 of the player's own checks two layers past their goal.
    if world.options.secret_achievements and not _deeper_than_goal(world, SECRET_REGION):
        secret_names = {secret_location_name(i): secret_location_id(i) for i in range(SECRET_COUNT)}
        world.get_region(SECRET_REGION).add_locations(secret_names, RevolutionIdleLocation)

    # Generator checks (own each base generator) — reachable from the start, so they go in Menu.
    gen_names = {gen_location_name(i): GEN_ID_BASE + i for i in range(GEN_COUNT)}
    world.get_region("Menu").add_locations(gen_names, RevolutionIdleLocation)

    # Generator-level checks: every N levels on each generator (base-tier grind -> Menu region).
    gen_milestones = min(world.options.generator_level_count.value, GEN_LEVEL_MAX_MILESTONES)
    if gen_milestones > 0 and world.options.generator_level_interval.value > 0:
        gen_level_names = {
            gen_level_location_name(i, k): gen_level_location_id(i, k)
            for i in range(GEN_COUNT)
            for k in range(1, gen_milestones + 1)
        }
        world.get_region("Menu").add_locations(gen_level_names, RevolutionIdleLocation)

    # Ascension-milestone checks: every `interval` total ascension levels (base-tier grind -> Menu).
    count = min(world.options.ascension_check_count.value, ASC_MAX_MILESTONES)
    if count > 0:
        asc_names = {asc_milestone_location_name(k): asc_milestone_location_id(k) for k in range(1, count + 1)}
        menu = world.get_region("Menu")
        menu.add_locations(asc_names, RevolutionIdleLocation)
        # By default these only hold filler; the toggle lets them hold progression.
        if not world.options.ascension_checks_progression:
            for loc in menu.get_locations():
                if loc.name in asc_names:
                    loc.progress_type = LocationProgressType.EXCLUDED

    create_victory_event(world)


def create_victory_event(world: RevolutionIdleWorld) -> None:
    # Victory lives in the goal's region (reaching it requires the matching layer unlocks).
    region = world.get_region(world.goal_region_name)
    victory = RevolutionIdleLocation(world.player, "Goal Reached", None, region)

    # The equality goal additionally requires collecting every non-tower unlock (completionist).
    if world.is_equality_goal:
        player = world.player
        extra = list(items.EQUALITY_EXTRA_ITEMS)
        victory.access_rule = lambda state: state.has_all(extra, player)

    victory.place_locked_item(RevolutionIdleItem("Victory", items.P, None, world.player))
    region.locations.append(victory)
