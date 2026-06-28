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


class RevolutionIdleLocation(Location):
    game = "Revolution Idle"


def create_all_locations(world: RevolutionIdleWorld) -> None:
    for start, end, region_name in TIERS:
        region = world.get_region(region_name)
        names_to_ids = {ach_location_name(i): ACH_ID_BASE + i for i in range(start, end)}
        region.add_locations(names_to_ids, RevolutionIdleLocation)

    create_victory_event(world)


def create_victory_event(world: RevolutionIdleWorld) -> None:
    # Victory lives in the Unity region (reaching Unity requires Infinity+Eternity+Unity unlocks).
    unity = world.get_region("Unity")
    victory = RevolutionIdleLocation(world.player, "Goal Reached", None, unity)

    # The equality goal additionally requires collecting every non-tower unlock (completionist).
    if world.is_equality_goal:
        player = world.player
        extra = list(items.EQUALITY_EXTRA_ITEMS)
        victory.access_rule = lambda state: state.has_all(extra, player)

    victory.place_locked_item(RevolutionIdleItem("Victory", items.P, None, world.player))
    unity.locations.append(victory)
