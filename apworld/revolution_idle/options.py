from dataclasses import dataclass

from Options import Choice, DeathLink, OptionGroup, PerGameCommonOptions, Range


class Goal(Choice):
    """
    The victory condition for your run.

    infinity: reach the Infinity layer (short run).
    eternity: reach the Eternity layer (medium run).
    unity: reach the Unity layer (long run).
    equality: reach Unity, collect every unlock, and earn Equality currency (longest / completionist).
    """

    display_name = "Goal"
    # NOTE: these integer values are sent in slot_data and must stay in sync with the mod's
    # goal handling. unity=0 and equality=1 are kept stable from earlier versions.
    option_unity = 0
    option_equality = 1
    option_infinity = 2
    option_eternity = 3
    default = option_unity


class AchievementPool(Range):
    """
    How many of the game's 520 achievements are used as location checks.

    Lower values make for a shorter, faster run; the full 520 is a long async. Checks are sampled
    across all progression tiers (with a guaranteed handful in the early tier), so gating is
    preserved at any size.
    """

    display_name = "Achievement Pool Size"
    range_start = 50
    range_end = 520
    default = 520


class TrapChance(Range):
    """Percentage chance that each filler item is replaced by a trap."""

    display_name = "Trap Chance"
    range_start = 0
    range_end = 100
    default = 0


@dataclass
class RevolutionIdleOptions(PerGameCommonOptions):
    goal: Goal
    achievement_pool: AchievementPool
    trap_chance: TrapChance
    death_link: DeathLink


option_groups = [
    OptionGroup("General", [Goal, AchievementPool, TrapChance]),
]
