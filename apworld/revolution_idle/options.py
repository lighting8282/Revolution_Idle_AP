from dataclasses import dataclass

from Options import Choice, DeathLink, OptionGroup, PerGameCommonOptions, Range


class Goal(Choice):
    """
    The victory condition for your run.

    unity: reach the Unity layer (medium-length run).
    equality: reach Unity, collect every unlock, and earn Equality currency (long/completionist run).
    """

    display_name = "Goal"
    option_unity = 0
    option_equality = 1
    default = option_unity


class TrapChance(Range):
    """Percentage chance that each filler item is replaced by a trap."""

    display_name = "Trap Chance"
    range_start = 0
    range_end = 100
    default = 0


@dataclass
class RevolutionIdleOptions(PerGameCommonOptions):
    goal: Goal
    trap_chance: TrapChance
    death_link: DeathLink


option_groups = [
    OptionGroup("General", [Goal, TrapChance]),
]
