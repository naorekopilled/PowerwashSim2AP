import logging
from typing import List, Union
from dataclasses import dataclass
from Options import Range, Toggle, DefaultOnToggle, PerGameCommonOptions, OptionSet, OptionError, Choice, \
    Accessibility
from .Data import first_job
from .Locations import categories, raw_location_dict
from settings import Group, Bool
from random import Random
import math


def location_doc(option_name: str, header: str = "") -> str:
    return f"""
    {header}Locations:
    {categories[option_name]}
    "All" - adds all locations above
    """


class StartWithFirstJob(Toggle):
    """
    Start with the campaign's first job as the starting level instead of a random one
    """
    display_name = "Start With First Job"


class LandVehicleLocations(OptionSet):
    display_name = "Land Vehicle Locations"
    valid_keys = frozenset(categories["land_vehicles"] + ["All"])
    default = frozenset(["All"])


class WaterVehicleLocations(OptionSet):
    display_name = "Water Vehicle Locations"
    valid_keys = frozenset(categories["water_vehicles"] + ["All"])
    default = frozenset(["All"])


class AirVehicleLocations(OptionSet):
    display_name = "Air Vehicle Locations"
    valid_keys = frozenset(categories["air_vehicles"] + ["All"])
    default = frozenset(["All"])


class PlaceLocations(OptionSet):
    display_name = "Place Locations"
    valid_keys = frozenset(categories["places"] + ["All"])
    default = frozenset(["All"])


class BonusJobs(Toggle):
    """
    Add the Caldera Chronicles bonus jobs (which ones: `bonus_jobs_levels`)
    """
    display_name = "Bonus Jobs"


class BonusJobLevels(OptionSet):
    display_name = "Bonus Job Levels"
    valid_keys = frozenset(categories["bonus_jobs"] + ["All"])
    default = frozenset(["All"])


class AdventureTimeDLC(Toggle):
    """
    DISCLAIMER: YOU NEED TO HAVE BOUGHT THIS DLC TO PLAY IT
    Add the Adventure Time DLC jobs (which ones: `adventure_time_dlc_levels`)
    """
    display_name = "Adventure Time DLC"


class AdventureTimeLevels(OptionSet):
    display_name = "Adventure Time DLC Levels"
    valid_keys = frozenset(categories["adventure_time_dlc"] + ["All"])
    default = frozenset(["All"])


class StarWarsDLC(Toggle):
    """
    DISCLAIMER: YOU NEED TO HAVE BOUGHT THIS DLC TO PLAY IT
    Add the Star Wars DLC jobs (which ones: `star_wars_dlc_levels`)
    """
    display_name = "Star Wars DLC"


class StarWarsLevels(OptionSet):
    display_name = "Star Wars DLC Levels"
    valid_keys = frozenset(categories["star_wars_dlc"] + ["All"])
    default = frozenset(["All"])


LandVehicleLocations.__doc__ = location_doc("land_vehicles")
WaterVehicleLocations.__doc__ = location_doc("water_vehicles")
AirVehicleLocations.__doc__ = location_doc("air_vehicles")
PlaceLocations.__doc__ = location_doc("places")
BonusJobLevels.__doc__ = location_doc("bonus_jobs", "Only used with `bonus_jobs: true`\n    ")
AdventureTimeLevels.__doc__ = location_doc("adventure_time_dlc", "Only used with `adventure_time_dlc: true`\n    ")
StarWarsLevels.__doc__ = location_doc("star_wars_dlc", "Only used with `star_wars_dlc: true`\n    ")


class EnablePercentsanity(DefaultOnToggle):
    """
    Every `percentsanity` % total cleaned of a level is a check
    At least one sanity must be enabled
    """
    display_name = "Enable Percentsanity"


class Percentsanity(Range):
    """
    What intervals of cleaned % to have checks at

    default 20: checks at 20%, 40%, 60%, 80%, and 100%
    host yaml setting `allow_percentsanity_below_7` (off by default) will allow % below 7%
    """
    display_name = "Percentsanity Interval"
    default = 20
    range_end = 20
    range_start = 1


class EnableObjectsanity(Toggle):
    """
    Each part of a cleanable object is a check
    Can only be enabled if the host has `allow_objectsanity` enabled in their host.yaml
    """
    display_name = "Enable Objectsanity"


class EnableStagesanity(Toggle):
    """
    Each finished stage of a level is a check
    On its own this only has enough checks for `goal_type: level`
    """
    display_name = "Enable Stagesanity"


class EnableBonustasksanity(Toggle):
    """
    Each bonus task of a level is a check (7 in the whole game), so another sanity is needed alongside it
    """
    display_name = "Enable Bonustasksanity"


class LocalFill(Range):
    """
    Minimums:
        - Percentsanity or Objectsanity: 55%
        - Percentsanity and Objectsanity: 97%

    the host yaml setting `allow_below_localfill_minimums` will allow for local fill to go below the minimums
    """
    default = 55
    range_start = 0
    range_end = 100


class GoalType(Choice):
    """
    What is required to goal?
    0 = mcguffin hunt (find a certain amount of `A Job Well Done`s to goal)
    1 = level hunt (complete levels to goal)
    """
    display_name = "Goal Type"
    default = 0
    option_mcguffin = 0
    option_level = 1


class LevelsToGoal(Choice):
    """
    Which levels can be beaten to goal (level hunt only)
    randomized: a random half or more of your enabled levels
    all: every enabled level
    custom: the levels in `custom_levels_to_goal`
    """
    display_name = "Levels to Goal"
    default = 0
    option_randomized = 0
    option_all = 1
    option_custom = 2


class CustomLevelsToGoal(OptionSet):
    """
    Only used with `levels_to_goal: custom`. Levels that aren't enabled are ignored
    """
    display_name = "Custom Levels to Goal"
    valid_keys = frozenset(raw_location_dict)


class AmountOfLevelsToGoal(Choice):
    """
    How many of the goal levels must be beaten (level hunt only)
    randomized: between 50% of your enabled levels and all goal levels
    custom: `custom_amount_of_levels_to_goal`
    """
    display_name = "Amount of Levels to Goal"
    default = 0
    option_randomized = 0
    option_custom = 1


class CustomAmountOfLevelsToGoal(Range):
    """
    Only used with `amount_of_levels_to_goal: custom`. More than there are goal levels means all of them
    It must be at least 50% of your enabled levels; the host setting `allow_potentially_excessive_releases` disables this
    """
    display_name = "Custom Amount of Levels to Goal"
    range_start = 1
    range_end = len(raw_location_dict)
    default = 20


class AmountOfMcguffinsToGoal(Choice):
    """
    How many `A Job Well Done`s are needed to goal (mcguffin hunt only)
    automatic: as PWS1, 5% of your checks but at least one per enabled level
    custom: `custom_amount_of_mcguffins_to_goal`
    """
    display_name = "Amount of Mcguffins to Goal"
    default = 0
    option_automatic = 0
    option_custom = 1


class CustomAmountOfMcguffinsToGoal(Range):
    """
    Only used with `amount_of_mcguffins_to_goal: custom`
    If this is more than the pool would hold, more `A Job Well Done`s are added; they must still fit in your checks
    """
    display_name = "Custom Amount of Mcguffins to Goal"
    range_start = 1
    range_end = 100
    default = 30


@dataclass
class PowerwashSimulator2Options(PerGameCommonOptions):
    start_with_first_job: StartWithFirstJob
    land_vehicles: LandVehicleLocations
    water_vehicles: WaterVehicleLocations
    air_vehicles: AirVehicleLocations
    places: PlaceLocations
    bonus_jobs: BonusJobs
    bonus_jobs_levels: BonusJobLevels
    adventure_time_dlc: AdventureTimeDLC
    adventure_time_dlc_levels: AdventureTimeLevels
    star_wars_dlc: StarWarsDLC
    star_wars_dlc_levels: StarWarsLevels
    enable_percentsanity: EnablePercentsanity
    percentsanity: Percentsanity
    enable_objectsanity: EnableObjectsanity
    enable_stagesanity: EnableStagesanity
    enable_bonustasksanity: EnableBonustasksanity
    local_fill: LocalFill
    goal_type: GoalType
    levels_to_goal: LevelsToGoal
    custom_levels_to_goal: CustomLevelsToGoal
    amount_of_levels_to_goal: AmountOfLevelsToGoal
    custom_amount_of_levels_to_goal: CustomAmountOfLevelsToGoal
    amount_of_mcguffins_to_goal: AmountOfMcguffinsToGoal
    custom_amount_of_mcguffins_to_goal: CustomAmountOfMcguffinsToGoal

    def get_locations(self) -> List[str]:
        locations = (self.flatten_locations(categories["land_vehicles"], self.land_vehicles)
                     + self.flatten_locations(categories["water_vehicles"], self.water_vehicles)
                     + self.flatten_locations(categories["air_vehicles"], self.air_vehicles)
                     + self.flatten_locations(categories["places"], self.places))
        for toggle, levels, option in ((self.bonus_jobs, self.bonus_jobs_levels, "bonus_jobs"),
                                       (self.adventure_time_dlc, self.adventure_time_dlc_levels, "adventure_time_dlc"),
                                       (self.star_wars_dlc, self.star_wars_dlc_levels, "star_wars_dlc")):
            if toggle: locations += self.flatten_locations(categories[option], levels)

        if self.start_with_first_job and first_job not in locations:
            locations.append(first_job)

        return locations

    def has_percentsanity(self) -> bool:
        return bool(self.enable_percentsanity)

    def has_objectsanity(self) -> bool:
        return bool(self.enable_objectsanity)

    def has_stagesanity(self) -> bool:
        return bool(self.enable_stagesanity)

    def has_bonustasksanity(self) -> bool:
        return bool(self.enable_bonustasksanity)

    def flatten_locations(self, list, self_list) -> List[str]:
        return list if "All" in self_list else [loc for loc in self_list if loc != "Random"]


class PowerwashSimulator2Settings(Group):
    class AllowPercentsanityBelow7(Bool):
        """Allow players to have the Percentsanity setting to be below 7%"""

    class AllowObjectsanity(Bool):
        """Allow players to enable Objectsanity"""

    class AllowBelowLocalfillMinimums(Bool):
        """Allow players to have local fill below the defined minimums"""

    class AllowPotentiallyExcessiveReleases(Bool):
        """Allow players to have less than 50% levels be required for level hunt, this can cause very large releases"""

    allow_percentsanity_below_7: Union[AllowPercentsanityBelow7, bool] = False
    allow_objectsanity: Union[AllowObjectsanity, bool] = False
    allow_below_localfill_minimums: Union[AllowBelowLocalfillMinimums, bool] = False
    allow_potentially_excessive_releases: Union[AllowPotentiallyExcessiveReleases, bool] = False


def check_options(world, locations: List[str]):
    options: PowerwashSimulator2Options = world.options
    settings: PowerwashSimulator2Settings = world.settings
    random: Random = world.random

    if options.accessibility == Accessibility.option_minimal:
        print("PowerWash Simulator 2 doesn't support accessibility minimal, defaulting accessibility to full")
        options.accessibility = Accessibility(Accessibility.option_full)

    if len(locations) == 0:
        raise_yaml_error(world.player_name, "Does not have locations listed in their yaml")

    if options.goal_type == 1:
        max_random = len(locations)

        if options.levels_to_goal == LevelsToGoal.option_all:
            levels_to_goal = locations
        elif options.levels_to_goal == LevelsToGoal.option_custom:
            levels_to_goal = [loc for loc in locations if loc in options.custom_levels_to_goal]
            if len(levels_to_goal) == 0:
                raise_yaml_error(world.player_name,
                                 "Can't pick goal levels from 0 possible levels, make sure goal levels are included in their respective locations")
        else:
            levels_to_goal = random.sample(locations, random.randint(math.ceil(max_random / 2), max_random))

        if options.amount_of_levels_to_goal == AmountOfLevelsToGoal.option_custom:
            amount_to_goal = min(options.custom_amount_of_levels_to_goal.value, len(levels_to_goal))
        else:
            # PWS1 drew from len(levels_to_goal) / 2, which can fall below 50% of enabled levels and fail the check below
            max_levels_to_goal = len(levels_to_goal)
            amount_to_goal = random.randint(min(math.ceil(max_random / 2), max_levels_to_goal), max_levels_to_goal)

        allow_below_50 = settings.allow_potentially_excessive_releases
        if (amount_to_goal < max_random / 2) and not allow_below_50:
            raise_yaml_error(world.player_name,
                             f"Goal level amount ({amount_to_goal}) required to goal can not be less than 50% ({int(max_random / 2)}) of your total enabled levels ({max_random})")

        world.goal_levels = levels_to_goal
        world.check_goal_level_count = amount_to_goal
    else:
        world.goal_levels = ["None"]

    if not settings.allow_below_localfill_minimums:
        if options.has_percentsanity() and options.has_objectsanity() and options.local_fill < 97:
            set_local_fill(world.player_name, options, 97)
        elif options.local_fill < 55:
            set_local_fill(world.player_name, options, 55)

    if options.percentsanity < 7 and not settings.allow_percentsanity_below_7:
        logging.info(
            f"PowerWash Simulator 2: {world.player_name} has {options.percentsanity} < 7 for percentsanity. since the host.yaml has allow_percentsanity_below_7 {settings.allow_percentsanity_below_7} percentsanity will be set to 7")
        options.percentsanity = Percentsanity(7)

    if options.has_objectsanity() and not settings.allow_objectsanity:
        raise_yaml_error(world.player_name,
                         "Objectsanity can not be enabled unless the host,yaml setting 'allow_objectsanity' is also enabled")

    if not (options.has_percentsanity() or options.has_objectsanity() or options.has_stagesanity()
            or options.has_bonustasksanity()):
        raise_yaml_error(world.player_name,
                         "No sanities are enabled, you must enable at least one `enable_*sanity` option")

    if not options.start_with_first_job or first_job not in locations or (
            options.goal_type == 1 and first_job in world.goal_levels and world.check_goal_level_count == 1):
        possible_locations = locations if options.goal_type != 1 else [loc for loc in locations if
                                                                       loc not in world.goal_levels]

        if len(possible_locations) == 0:
            if options.goal_type == 1 and world.check_goal_level_count == 1:
                logging.info(f"PowerWash Simulator 2: {world.player_name} will goal in sphere 1")

            possible_locations = locations

        world.starting_location = world.random.choice(possible_locations)


def set_local_fill(player_name, options, amount):
    logging.info(
        f"PowerWash Simulator 2: {player_name} has a local fill below the allowed minimum, it will be set to the minimum, if you want to allow below minimums then change the host.yaml setting `allow_below_localfill_minimums`")
    options.local_fill = LocalFill(amount)


def raise_yaml_error(player_name, error):
    raise OptionError(
        f"\n\n=== PowerWash Simulator 2 YAML ERROR ===\nPowerWash Simulator 2: {player_name} {error}, PLEASE FIX YOUR YAML\n\n")
