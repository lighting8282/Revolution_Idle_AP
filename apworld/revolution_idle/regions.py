from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region

if TYPE_CHECKING:
    from .world import RevolutionIdleWorld

# The prestige tower as a linear chain of regions, matching the game's achievement tiers
# (Const.ACH_RANGES). Prestige is precollected, so the base tier (Menu) is reachable from the start.
# (region_name, item_required_to_enter_from_previous_region)
TOWER: list[tuple[str, str]] = [
    ("Menu", ""),                     # base/prestige tier (achievements 0-29), always reachable
    ("Infinity", "Infinity Unlock"),  # achievements 30-69
    ("Eternity", "Eternity Unlock"),  # achievements 70-160
    ("Unity", "Unity Unlock"),        # achievements 161-519
]


def create_and_connect_regions(world: RevolutionIdleWorld) -> None:
    for region_name, _ in TOWER:
        world.multiworld.regions.append(Region(region_name, world.player, world.multiworld))

    player = world.player
    for (prev_name, _), (next_name, required_item) in zip(TOWER, TOWER[1:]):
        world.get_region(prev_name).connect(
            world.get_region(next_name),
            f"{prev_name} -> {next_name}",
            lambda state, item=required_item: state.has(item, player),
        )
