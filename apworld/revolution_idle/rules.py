from __future__ import annotations

from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from .world import RevolutionIdleWorld


def set_all_rules(world: RevolutionIdleWorld) -> None:
    # Region access rules are defined on the entrances in regions.py.
    # Completion = collecting the Victory event placed in the chosen goal region.
    world.multiworld.completion_condition[world.player] = lambda state: state.has("Victory", world.player)
