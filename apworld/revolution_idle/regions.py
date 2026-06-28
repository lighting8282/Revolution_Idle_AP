from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region

from .items import LAYER_UNLOCKS, PROGRESSIVE_LAYER

if TYPE_CHECKING:
    from .world import RevolutionIdleWorld

# The prestige tower as a linear chain of regions, matching the game's achievement tiers
# (Const.ACH_RANGES). Prestige is precollected, so the base tier (Menu) is reachable from the start.
TOWER_REGIONS = ["Menu", "Infinity", "Eternity", "Unity"]


def create_and_connect_regions(world: RevolutionIdleWorld) -> None:
    for region_name in TOWER_REGIONS:
        world.multiworld.regions.append(Region(region_name, world.player, world.multiworld))

    player = world.player
    progressive = bool(world.options.progressive_layers)

    # Each step into the tower requires the next layer unlock. Under progressive_layers, all three
    # are the same "Progressive Layer" item, counted (1 -> Infinity, 2 -> Eternity, 3 -> Unity).
    for step, (prev_name, next_name) in enumerate(zip(TOWER_REGIONS, TOWER_REGIONS[1:]), start=1):
        if progressive:
            rule = lambda state, n=step: state.has(PROGRESSIVE_LAYER, player, n)
        else:
            rule = lambda state, item=LAYER_UNLOCKS[step - 1]: state.has(item, player)
        world.get_region(prev_name).connect(world.get_region(next_name), f"{prev_name} -> {next_name}", rule)
