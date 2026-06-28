from BaseClasses import Tutorial
from worlds.AutoWorld import WebWorld

from .options import option_groups


class RevolutionIdleWebWorld(WebWorld):
    game = "Revolution Idle"
    theme = "stone"

    setup_en = Tutorial(
        "Multiworld Setup Guide",
        "A guide to setting up Revolution Idle for Archipelago multiworld.",
        "English",
        "setup_en.md",
        "setup/en",
        ["Jon Trnka"],
    )
    tutorials = [setup_en]

    option_groups = option_groups
