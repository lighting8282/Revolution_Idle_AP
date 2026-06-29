from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Location

from . import items
from .items import RevolutionIdleItem

if TYPE_CHECKING:
    from .world import RevolutionIdleWorld

# Mirrors GameData.ACH_COUNT (normal achievements; secrets at id 10000+ are not used as locations).
ACH_COUNT = 520
ACH_ID_BASE = 10_000

# Base-layer generators (GameData.infinity.generators, GEN_COUNT=10). Owning each one is a check.
GEN_COUNT = 10
GEN_ID_BASE = 30_000


def gen_location_name(index: int) -> str:
    return f"Generator {index + 1}"

# Achievement-id tiers from Const.ACH_RANGES, mapped to the tower regions they require.
# (start_id, end_id_exclusive, region_name)
TIERS: list[tuple[int, int, str]] = [
    (0, 30, "Menu"),       # base/prestige tier
    (30, 70, "Infinity"),
    (70, 161, "Eternity"),
    (161, 520, "Unity"),
]


def ach_location_name(game_id: int) -> str:
    return f"Achievement #{game_id}"


# Full, stable location_name_to_id (every location that could ever exist).
LOCATION_NAME_TO_ID: dict[str, int] = {
    ach_location_name(i): ACH_ID_BASE + i for i in range(ACH_COUNT)
}
LOCATION_NAME_TO_ID.update({
    gen_location_name(i): GEN_ID_BASE + i for i in range(GEN_COUNT)
})


class RevolutionIdleLocation(Location):
    game = "Revolution Idle"


def selected_achievement_ids(world: RevolutionIdleWorld) -> list[int]:
    """Pick which achievements become checks, honoring the achievement_pool size.

    Always includes a few early (Menu-tier) achievements so there's a reachable foothold at the
    start, then samples the rest across all tiers (deterministically via world.random)."""
    n = world.options.achievement_pool.value
    if n >= ACH_COUNT:
        return list(range(ACH_COUNT))

    rng = world.random
    menu_ids = list(range(0, 30))
    rng.shuffle(menu_ids)
    floor = min(len(menu_ids), n, 8)
    chosen = set(menu_ids[:floor])

    rest = [i for i in range(ACH_COUNT) if i not in chosen]
    rng.shuffle(rest)
    for i in rest:
        if len(chosen) >= n:
            break
        chosen.add(i)
    return sorted(chosen)


def create_all_locations(world: RevolutionIdleWorld) -> None:
    ids = selected_achievement_ids(world)

    by_region: dict[str, dict[str, int]] = {region_name: {} for _, _, region_name in TIERS}
    for gid in ids:
        for start, end, region_name in TIERS:
            if start <= gid < end:
                by_region[region_name][ach_location_name(gid)] = ACH_ID_BASE + gid
                break

    for _, _, region_name in TIERS:
        names_to_ids = by_region[region_name]
        if names_to_ids:
            world.get_region(region_name).add_locations(names_to_ids, RevolutionIdleLocation)

    # Generator checks (own each base generator) — reachable from the start, so they go in Menu.
    gen_names = {gen_location_name(i): GEN_ID_BASE + i for i in range(GEN_COUNT)}
    world.get_region("Menu").add_locations(gen_names, RevolutionIdleLocation)

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
