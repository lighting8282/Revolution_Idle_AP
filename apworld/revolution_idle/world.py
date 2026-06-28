from collections.abc import Mapping
from typing import Any

from worlds.AutoWorld import World

from . import items, locations, regions, rules, web_world
from . import options as ri_options


class RevolutionIdleWorld(World):
    """
    Revolution Idle is a deep incremental/idle game with a prestige tower (Prestige, Infinity,
    Eternity, Unity, Equality) and many side systems. This Archipelago world gates those layers
    and systems behind items, and turns the game's 520 achievements into location checks.
    """

    game = "Revolution Idle"
    web = web_world.RevolutionIdleWebWorld()

    options_dataclass = ri_options.RevolutionIdleOptions
    options: ri_options.RevolutionIdleOptions

    location_name_to_id = locations.LOCATION_NAME_TO_ID
    item_name_to_id = items.ITEM_NAME_TO_ID

    origin_region_name = "Menu"

    # Victory lives in the Unity region for both goals; the equality goal adds an extra item
    # requirement on the Victory event (see locations.create_victory_event).
    @property
    def is_equality_goal(self) -> bool:
        return self.options.goal.value == ri_options.Goal.option_equality

    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)

    def set_rules(self) -> None:
        rules.set_all_rules(self)

    def create_items(self) -> None:
        items.create_all_items(self)

    def create_item(self, name: str) -> items.RevolutionIdleItem:
        return items.create_item_with_correct_classification(self, name)

    def get_filler_item_name(self) -> str:
        return items.get_random_filler_item_name(self)

    def fill_slot_data(self) -> Mapping[str, Any]:
        return self.options.as_dict("goal", "death_link")
