from dataclasses import dataclass

from Options import Choice, DeathLink, OptionGroup, PerGameCommonOptions, Range, Toggle


class Goal(Choice):
    """
    The victory condition for your run.

    infinity: reach the Infinity layer (short run).
    eternity: reach the Eternity layer (medium run).
    unity: reach the Unity layer (long run).
    equality: reach Unity, collect every unlock, and earn Equality currency (longest / completionist).
    generators: get a number of base generators to a target upgrade level (see the two
        generators_goal_* options). Base-tier grind goal; reachable from the start.
    score: reach a target Score of 10^N (see score_goal_exponent). Base-tier.
    prestige_mult: reach a target prestige multiplier of 10^N (see prestige_mult_goal_exponent).
    achievement_count: unlock a target number of achievements in-game (see achievement_count_goal).
    """

    display_name = "Goal"
    # NOTE: these integer values are sent in slot_data and must stay in sync with the mod's
    # goal handling. unity=0 and equality=1 are kept stable from earlier versions.
    option_unity = 0
    option_equality = 1
    option_infinity = 2
    option_eternity = 3
    option_generators = 4
    option_score = 5
    option_prestige_mult = 6
    option_achievement_count = 7
    default = option_unity


class GeneratorsGoalCount(Range):
    """For the `generators` goal: how many base generators must reach the target level (1-10)."""

    display_name = "Generators Goal: Count"
    range_start = 1
    range_end = 10
    default = 10


class GeneratorsGoalLevel(Range):
    """For the `generators` goal: the upgrade level each of those generators must reach (1-100)."""

    display_name = "Generators Goal: Level"
    range_start = 1
    range_end = 100
    default = 100


class ScoreGoalExponent(Range):
    """For the `score` goal: reach a Score of 10^N. Higher N = much longer run (it's an exponent)."""

    display_name = "Score Goal: Exponent (10^N)"
    range_start = 6
    range_end = 9000
    default = 100


class PrestigeMultGoalExponent(Range):
    """For the `prestige_mult` goal: reach a prestige multiplier of 10^N."""

    display_name = "Prestige Mult Goal: Exponent (10^N)"
    range_start = 1
    range_end = 9000
    default = 30


class AchievementCountGoal(Range):
    """For the `achievement_count` goal: how many achievements you must unlock in-game (1-520).

    This counts your real in-game achievement unlocks (independent of how many are AP checks). The
    win region is gated by the count: higher targets require the deeper prestige layers to reach.
    """

    display_name = "Achievement Count Goal"
    range_start = 1
    range_end = 520
    default = 250


# The game splits its 520 achievements into tiers (Const.ACH_RANGES), each tied to a prestige layer.
# Each option below picks how many achievements from that tier become checks (sampled deterministically).
# Defaults are the full size of each tier, so out of the box you get all 520 (same as before).


class AchievementsBase(Range):
    """How many Base / Prestige achievements (ids 0-29, reachable from the start) become checks."""

    display_name = "Base Achievements"
    range_start = 0
    range_end = 30
    default = 30


class AchievementsInfinity(Range):
    """How many Infinity-tier achievements (ids 30-69, need the Infinity layer) become checks."""

    display_name = "Infinity Achievements"
    range_start = 0
    range_end = 40
    default = 40


class AchievementsEternity(Range):
    """How many Eternity-tier achievements (ids 70-160, need the Eternity layer) become checks."""

    display_name = "Eternity Achievements"
    range_start = 0
    range_end = 91
    default = 91


class AchievementsUnity(Range):
    """How many Unity-tier achievements (ids 161-519, need the Unity layer) become checks."""

    display_name = "Unity Achievements"
    range_start = 0
    range_end = 359
    default = 359


class SecretAchievements(Toggle):
    """
    Add the 55 secret achievements (ids 10000-10054) as checks.

    These are cryptic / hard to get, so they're treated as deep-game checks (gated behind the Unity
    layer) and are off by default. Note: every check needs an item, so very low achievement counts
    combined with this off may leave fewer locations than the ~36 required unlock items.
    """

    display_name = "Secret Achievements"


class GeneratorLevelInterval(Range):
    """
    Add a location check every N levels on each of the 10 base generators (each generator levels
    up as you buy it, from 1 to 100).

    0 disables generator-level checks. Otherwise you get a check at level N, 2N, 3N ... up to 100
    on every generator. Example: 25 -> checks at levels 25/50/75/100 (4 per generator, 40 total);
    10 -> 10 per generator (100 total). Smaller values = far more checks.
    """

    display_name = "Generator Level Interval"
    range_start = 0
    range_end = 100
    default = 0


class ProgressiveLayers(Toggle):
    """
    Replace the separate Infinity/Eternity/Unity unlock items with three "Progressive Layer" items
    that unlock the next layer in order (1st -> Infinity, 2nd -> Eternity, 3rd -> Unity).
    """

    display_name = "Progressive Layers"


class TrapChance(Range):
    """Percentage chance that each filler item is replaced by a trap.

    Filler grants ~60 seconds of your current income; traps remove ~120 seconds of progress.
    """

    display_name = "Trap Chance"
    range_start = 0
    range_end = 100
    default = 0


@dataclass
class RevolutionIdleOptions(PerGameCommonOptions):
    goal: Goal
    generators_goal_count: GeneratorsGoalCount
    generators_goal_level: GeneratorsGoalLevel
    score_goal_exponent: ScoreGoalExponent
    prestige_mult_goal_exponent: PrestigeMultGoalExponent
    achievement_count_goal: AchievementCountGoal
    achievements_base: AchievementsBase
    achievements_infinity: AchievementsInfinity
    achievements_eternity: AchievementsEternity
    achievements_unity: AchievementsUnity
    secret_achievements: SecretAchievements
    generator_level_interval: GeneratorLevelInterval
    progressive_layers: ProgressiveLayers
    trap_chance: TrapChance
    death_link: DeathLink


option_groups = [
    OptionGroup("General", [Goal, ProgressiveLayers, TrapChance]),
    OptionGroup("Goal Settings", [
        GeneratorsGoalCount, GeneratorsGoalLevel,
        ScoreGoalExponent, PrestigeMultGoalExponent, AchievementCountGoal,
    ]),
    OptionGroup("Achievement Checks", [
        AchievementsBase, AchievementsInfinity, AchievementsEternity, AchievementsUnity,
        SecretAchievements,
    ]),
    OptionGroup("Generator Checks", [GeneratorLevelInterval]),
]
