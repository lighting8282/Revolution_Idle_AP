from dataclasses import dataclass

from Options import Choice, DeathLink, OptionGroup, PerGameCommonOptions, Range, Toggle


class Goal(Choice):
    """
    The victory condition for your run.

    infinity: reach the Infinity layer (short run).
    eternity: reach the Eternity layer (medium run).
    unity: reach the Unity layer (long run).
    equality: reach Unity, collect every unlock, and earn Equality currency (longest / completionist).
    ascension: reach a target total ascension level across all revolutions (see ascension_goal).
        Base-tier grind goal; reachable from the start.
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
    option_ascension = 4
    option_score = 5
    option_prestige_mult = 6
    option_achievement_count = 7
    default = option_unity


class AscensionGoal(Range):
    """For the `ascension` goal: the total ascension level (summed across all revolutions) to reach."""

    display_name = "Ascension Goal: Total Levels"
    range_start = 1
    range_end = 2_000_000
    default = 2000


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


class ScaleAchievementsToGoal(Toggle):
    """
    Automatically skip achievements from tiers deeper than your chosen goal requires.

    On (default): e.g. with goal:infinity, achievements_eternity/achievements_unity are ignored (0
    achievements from those tiers) regardless of their configured count, so you aren't forced to
    reach Eternity/Unity just to fill your own achievement checks in a short run.

    Off: achievements_base/infinity/eternity/unity are fully independent of the goal (their
    configured counts always apply) — useful if you want a shallow goal but still want achievement
    variety from deeper tiers.
    """

    display_name = "Scale Achievements To Goal"
    default = 1


class AscensionCheckCount(Range):
    """
    How many ascension-milestone checks to add (0 disables them).

    With count N and interval I, you get checks at total ascension I, 2I, ... N*I (total ascension is
    summed across all 10 revolutions). By default these only hold filler items; see
    ascension_checks_progression.
    """

    display_name = "Ascension Check Count"
    range_start = 0
    range_end = 200
    default = 0


class AscensionCheckInterval(Range):
    """Spacing of ascension-milestone checks: one check per this many total ascension levels."""

    display_name = "Ascension Check Interval"
    range_start = 1
    range_end = 100_000
    default = 500


class AscensionChecksProgression(Toggle):
    """
    Allow ascension-milestone checks to hold progression/important items.

    Off (default): they only ever hold filler. On: they're treated like normal locations.
    """

    display_name = "Ascension Checks Can Hold Progression"


class RevolutionSpeedMultiplier(Range):
    """
    Multiplies how fast the revolutions (the circles) fill — the core loop of the game.

    Default 10 (10x vanilla speed), since a 1x idle grind makes for a very long multiworld. Set to 1
    for untouched vanilla pacing.

    Note: this speeds up everything downstream of the revolutions, so goal thresholds
    (ascension_goal, score_goal_exponent, prestige_mult_goal_exponent) are reached proportionally
    faster — their defaults are calibrated against vanilla (1x) pacing.
    """

    display_name = "Revolution Speed Multiplier"
    range_start = 1
    range_end = 1000
    default = 10


class GeneratorLevelInterval(Range):
    """
    Add a location check every N levels on each of the 10 base generators (each generator levels up
    as you buy it, from 1 to 100).

    0 disables generator-level checks. Otherwise you get a check at level N, 2N, 3N ... up to 100 on
    every generator. Example: 25 -> checks at 25/50/75/100 (4 per generator, 40 total); 10 -> 100 total.
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

    When a trap is rolled, its type is chosen at random from: Slowdown (remove progress), Freeze
    (everything stops), Generator Drain (generators lose levels), and Lag (everything at half speed).
    """

    display_name = "Trap Chance"
    range_start = 0
    range_end = 100
    default = 0


class FreezeTrapSeconds(Range):
    """Freeze Trap: how many seconds the game is fully frozen (timeScale 0)."""

    display_name = "Freeze Trap: Seconds"
    range_start = 1
    range_end = 300
    default = 30


class LagTrapSeconds(Range):
    """Lag Trap: how many seconds the game runs at half speed."""

    display_name = "Lag Trap: Seconds"
    range_start = 1
    range_end = 600
    default = 60


class GeneratorDrainLevels(Range):
    """Generator Drain Trap: how many levels each base generator loses (clamped at 0)."""

    display_name = "Generator Drain Trap: Levels"
    range_start = 1
    range_end = 100
    default = 20


class GeneratorBoostLevels(Range):
    """Generator Boost (filler): how many levels each base generator gains (capped at its max)."""

    display_name = "Generator Boost: Levels"
    range_start = 1
    range_end = 100
    default = 20


class OverdriveSeconds(Range):
    """Overdrive (filler): how many seconds the game runs at double speed."""

    display_name = "Overdrive: Seconds"
    range_start = 1
    range_end = 600
    default = 60


class IncomeJackpotSeconds(Range):
    """Income Jackpot (filler): how many seconds' worth of current income it grants at once."""

    display_name = "Income Jackpot: Seconds"
    range_start = 1
    range_end = 6000
    default = 600


@dataclass
class RevolutionIdleOptions(PerGameCommonOptions):
    goal: Goal
    ascension_goal: AscensionGoal
    score_goal_exponent: ScoreGoalExponent
    prestige_mult_goal_exponent: PrestigeMultGoalExponent
    achievement_count_goal: AchievementCountGoal
    achievements_base: AchievementsBase
    achievements_infinity: AchievementsInfinity
    achievements_eternity: AchievementsEternity
    achievements_unity: AchievementsUnity
    secret_achievements: SecretAchievements
    scale_achievements_to_goal: ScaleAchievementsToGoal
    ascension_check_count: AscensionCheckCount
    ascension_check_interval: AscensionCheckInterval
    ascension_checks_progression: AscensionChecksProgression
    revolution_speed_multiplier: RevolutionSpeedMultiplier
    generator_level_interval: GeneratorLevelInterval
    progressive_layers: ProgressiveLayers
    trap_chance: TrapChance
    freeze_trap_seconds: FreezeTrapSeconds
    lag_trap_seconds: LagTrapSeconds
    generator_drain_levels: GeneratorDrainLevels
    generator_boost_levels: GeneratorBoostLevels
    overdrive_seconds: OverdriveSeconds
    income_jackpot_seconds: IncomeJackpotSeconds
    death_link: DeathLink


option_groups = [
    OptionGroup("General", [Goal, ProgressiveLayers]),
    OptionGroup("Traps", [TrapChance, FreezeTrapSeconds, LagTrapSeconds, GeneratorDrainLevels]),
    OptionGroup("Fillers", [GeneratorBoostLevels, OverdriveSeconds, IncomeJackpotSeconds]),
    OptionGroup("Goal Settings", [
        AscensionGoal, ScoreGoalExponent, PrestigeMultGoalExponent, AchievementCountGoal,
    ]),
    OptionGroup("Achievement Checks", [
        AchievementsBase, AchievementsInfinity, AchievementsEternity, AchievementsUnity,
        SecretAchievements, ScaleAchievementsToGoal,
    ]),
    OptionGroup("Ascension Checks", [
        AscensionCheckCount, AscensionCheckInterval, AscensionChecksProgression,
    ]),
    OptionGroup("Generator Checks", [GeneratorLevelInterval]),
    OptionGroup("Game Speed", [RevolutionSpeedMultiplier]),
]
