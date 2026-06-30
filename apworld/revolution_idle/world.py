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

    # Region whose Victory event must be reached, per goal. The equality goal additionally requires
    # collecting every unlock (see locations.create_victory_event).
    GOAL_REGION = {
        ri_options.Goal.option_infinity: "Infinity",
        ri_options.Goal.option_eternity: "Eternity",
        ri_options.Goal.option_unity: "Unity",
        ri_options.Goal.option_equality: "Unity",
        ri_options.Goal.option_ascension: "Menu",    # ascension grind is base-tier
        ri_options.Goal.option_score: "Menu",        # score grind is base-tier
        ri_options.Goal.option_prestige_mult: "Menu",
    }

    # Cumulative achievements obtainable once each layer is unlocked (Base 30, +Infinity=70,
    # +Eternity=161, +Unity=520). Used to gate the achievement_count goal's win region.
    _ACH_COUNT_GATES = [(30, "Menu"), (70, "Infinity"), (161, "Eternity"), (520, "Unity")]

    @property
    def goal_region_name(self) -> str:
        g = self.options.goal.value
        if g == ri_options.Goal.option_achievement_count:
            n = self.options.achievement_count_goal.value
            for threshold, region in self._ACH_COUNT_GATES:
                if n <= threshold:
                    return region
            return "Unity"
        return self.GOAL_REGION[g]

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
        return self.options.as_dict(
            "goal", "death_link",
            "ascension_goal", "ascension_check_count", "ascension_check_interval",
            "score_goal_exponent", "prestige_mult_goal_exponent", "achievement_count_goal",
            "freeze_trap_seconds", "lag_trap_seconds", "generator_drain_levels",
            "generator_boost_levels", "overdrive_seconds", "income_jackpot_seconds",
        )
