from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Item, ItemClassification

if TYPE_CHECKING:
    from .world import RevolutionIdleWorld

P = ItemClassification.progression
U = ItemClassification.useful
F = ItemClassification.filler
T = ItemClassification.trap

# Item IDs must be unique, positive, and STABLE across versions (changing them breaks existing seeds).
# Layout: 1-99 layer unlocks, 100-199 side-system unlocks, 200-299 automation, 900-999 filler/traps.
ITEM_TABLE: dict[str, tuple[int, ItemClassification]] = {
    # --- Prestige tower ---
    "Prestige Unlock": (1, P),   # precollected (the basic early mechanic; base-tier checks need it)
    "Infinity Unlock": (2, P),   # gates the Infinity region
    "Eternity Unlock": (3, P),   # gates the Eternity region
    "Unity Unlock": (4, P),      # gates the Unity region
    "Equality Unlock": (5, U),   # no in-game gate exists; kept for flavor / equality goal
    "Progressive Layer": (6, P), # progressive_layers mode: unlocks Infinity -> Eternity -> Unity in order

    # --- Side systems (useful; become progression under the equality goal) ---
    "Minerals Unlock": (100, U),
    "Special Minerals Unlock": (101, U),
    "Attacks Unlock": (102, U),
    "Animals Unlock": (103, U),
    "Stars Unlock": (104, U),
    "Lab Unlock": (105, U),
    "Slowdown Unlock": (106, U),
    "Elements Unlock": (107, U),
    "Dilation Unlock": (108, U),
    "Dilation Tree Unlock": (109, U),
    "Relics Unlock": (110, U),
    "Tarot Upgrades Unlock": (111, U),
    "Tarot Challenges Unlock": (112, U),
    "Tarot Artifacts Unlock": (113, U),
    "Macro Unlock": (114, U),
    "Promotion Unlock": (115, U),
    "Shop Unlock": (116, U),
    "Trials Unlock": (117, U),
    "Infinity Challenges Unlock": (118, U),
    "Eternity Challenges Unlock": (119, U),

    # --- Automation (useful; become progression under the equality goal) ---
    "Automation": (200, U),
    "Auto-Prestige": (201, U),
    "Auto-Infinity": (202, U),
    "Auto-Eternity": (203, U),
    "Auto-Ascend": (204, U),
    "Auto-Minerals": (205, U),

    # --- Filler / traps ---
    "Score Boost": (900, F),
    "Time Flux": (901, F),
    "Soul Cache": (902, F),
    "Slowdown Trap": (950, T),
}

ITEM_NAME_TO_ID: dict[str, int] = {name: data[0] for name, data in ITEM_TABLE.items()}
DEFAULT_ITEM_CLASSIFICATIONS: dict[str, ItemClassification] = {name: data[1] for name, data in ITEM_TABLE.items()}

# Given free at game start so the base tier (achievements 0-29) is reachable; not in the random pool.
PRECOLLECTED_ITEMS: list[str] = ["Prestige Unlock"]

# The three layer gates, in order. Under progressive_layers they're replaced by 3x "Progressive Layer".
LAYER_UNLOCKS: list[str] = ["Infinity Unlock", "Eternity Unlock", "Unity Unlock"]
PROGRESSIVE_LAYER = "Progressive Layer"

FILLER_ITEM_NAME = "Score Boost"
TRAP_ITEM_NAME = "Slowdown Trap"

# Items required to win under the "equality" goal (completionist): every unlock that isn't a
# tower-region gate. Under that goal these are promoted to progression so fill places them reachably.
EQUALITY_EXTRA_ITEMS: list[str] = [
    name for name, (_id, cls) in ITEM_TABLE.items()
    if cls is U  # all the useful unlocks: side systems, automation, Equality Unlock
]

# Every non-filler item that goes into the pool exactly once (excludes precollected Prestige and the
# Progressive Layer item, which is only added under progressive_layers mode by create_all_items).
GUARANTEED_ITEMS: list[str] = [
    name for name, (_id, cls) in ITEM_TABLE.items()
    if cls not in (F, T) and name not in PRECOLLECTED_ITEMS and name != PROGRESSIVE_LAYER
]


class RevolutionIdleItem(Item):
    game = "Revolution Idle"


def get_random_filler_item_name(world: RevolutionIdleWorld) -> str:
    if world.random.randint(0, 99) < world.options.trap_chance:
        return TRAP_ITEM_NAME
    return FILLER_ITEM_NAME


def _classification_for(world: RevolutionIdleWorld, name: str) -> ItemClassification:
    cls = DEFAULT_ITEM_CLASSIFICATIONS[name]
    # Under the equality goal, the extra unlocks are logically required -> progression.
    if world.is_equality_goal and name in EQUALITY_EXTRA_ITEMS:
        cls = ItemClassification.progression
    return cls


def create_item_with_correct_classification(world: RevolutionIdleWorld, name: str) -> RevolutionIdleItem:
    return RevolutionIdleItem(name, _classification_for(world, name), ITEM_NAME_TO_ID[name], world.player)


def create_all_items(world: RevolutionIdleWorld) -> None:
    # Free starting items.
    for name in PRECOLLECTED_ITEMS:
        world.push_precollected(world.create_item(name))

    pool_names = list(GUARANTEED_ITEMS)
    if world.options.progressive_layers:
        # Swap the three distinct layer unlocks for three Progressive Layer items.
        pool_names = [n for n in pool_names if n not in LAYER_UNLOCKS]
        pool_names += [PROGRESSIVE_LAYER] * len(LAYER_UNLOCKS)

    itempool: list[Item] = [world.create_item(name) for name in pool_names]

    number_of_unfilled_locations = len(world.multiworld.get_unfilled_locations(world.player))
    needed_filler = number_of_unfilled_locations - len(itempool)
    itempool += [world.create_filler() for _ in range(max(0, needed_filler))]

    world.multiworld.itempool += itempool
