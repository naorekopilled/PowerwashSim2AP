import ast
import math
from typing import *
from Options import OptionError
from worlds.AutoWorld import World
from BaseClasses import Location, Region, Item, ItemClassification, MultiWorld
from .Data import first_job
from .Items import raw_items, PowerwashSimulator2Item, item_table, create_items, unlock_items, filler_items
from .Locations import location_dict, objectsanity_dict, stagesanity_dict, bonustasksanity_dict
from .Options import PowerwashSimulator2Options, PowerwashSimulator2Settings, check_options, Percentsanity, \
    AmountOfMcguffinsToGoal

uuid_offset = 0x50A52000


class PowerwashSimulator2World(World):
    """
    PowerWash Simulator 2
    """
    game = "PowerWash Simulator 2"
    options_dataclass = PowerwashSimulator2Options
    options: PowerwashSimulator2Options
    settings: ClassVar[PowerwashSimulator2Settings]
    location_name_to_id = {value: location_dict.index(value) + uuid_offset for value in location_dict}
    item_name_to_id = {value: raw_items.index(value) + uuid_offset for value in raw_items}

    item_name_groups = {
        "unlocks": unlock_items
    }

    topology_present = True
    ut_can_gen_without_yaml = True
    gen_puml = False

    def __init__(self, multiworld: "MultiWorld", player: int):
        super().__init__(multiworld, player)
        self.starting_location = first_job
        self.filler_locations: List[str] = []
        self.goal_levels: List[str] = []
        self.mcguffin_requirement = 0
        self.all_locations: List[str] = []

        # check calculation variables
        self.check_total_count = 0
        self.check_percentasnity = 0
        self.check_objectsanity = 0
        self.check_unlock_count = 0
        self.check_raw_mcguffin_count = 0
        self.check_before_progression_count = 0
        self.check_added_mcguffin_count = 0
        self.check_total_mcguffin_count = 0
        self.check_total_progression_count = 0
        self.check_added_filler_count = 0
        self.check_total_filler_count = 0
        self.check_goal_level_count = -1

    def generate_early(self) -> None:
        self.all_locations = self.options.get_locations()

        if hasattr(self.multiworld, "re_gen_passthrough"):
            if "PowerWash Simulator 2" not in self.multiworld.re_gen_passthrough: return
            passthrough = self.multiworld.re_gen_passthrough["PowerWash Simulator 2"]

            if "all_levels" in passthrough:
                self.all_locations = passthrough["all_levels"]

            check_options(self, self.all_locations)

            self.starting_location = passthrough["starting_location"]
            # slot data stores the goal levels as str(list)
            goal_levels = passthrough["goal_levels"]
            self.goal_levels = ast.literal_eval(goal_levels) if isinstance(goal_levels, str) else goal_levels
            self.check_goal_level_count = passthrough["goal_level_amount"]

            for key in ("percentsanity", "objectsanity", "stagesanity", "bonustasksanity"):
                getattr(self.options, f"enable_{key}").value = int(bool(passthrough.get(key)))
            if "percentsanity_amount" in passthrough:
                self.options.percentsanity = Percentsanity(passthrough["percentsanity_amount"])
        else:
            check_options(self, self.all_locations)

        option_location_count = len(self.all_locations)
        percentsanity = self.options.percentsanity

        self.check_total_count = 0
        self.check_percentasnity = (len(range(percentsanity, 100, percentsanity)) + 1) * option_location_count
        self.check_objectsanity = sum(len(objectsanity_dict[loc]) for loc in self.all_locations)

        if self.options.has_percentsanity():
            self.check_total_count += self.check_percentasnity

        if self.options.has_objectsanity():
            self.check_total_count += self.check_objectsanity

        if self.options.has_stagesanity():
            self.check_total_count += sum(len(stagesanity_dict[loc]) for loc in self.all_locations)

        if self.options.has_bonustasksanity():
            self.check_total_count += sum(len(bonustasksanity_dict[loc]) for loc in self.all_locations)


        self.check_unlock_count = option_location_count - 1
        self.check_raw_mcguffin_count = option_location_count if self.options.goal_type == 0 else 0
        self.check_before_progression_count = self.check_unlock_count + self.check_raw_mcguffin_count

        self.check_added_mcguffin_count = math.floor(
            (self.check_total_count - self.check_before_progression_count) * .1) if self.options.goal_type == 0 else 0

        self.check_total_mcguffin_count = self.check_added_mcguffin_count + self.check_raw_mcguffin_count

        if self.options.goal_type == 0:
            self.mcguffin_requirement = max(
                min(math.floor(self.check_total_count * .05), self.check_total_count - option_location_count * 2),
                option_location_count)
            if self.options.amount_of_mcguffins_to_goal == AmountOfMcguffinsToGoal.option_custom:
                self.mcguffin_requirement = self.options.custom_amount_of_mcguffins_to_goal.value
                if self.mcguffin_requirement > self.check_total_mcguffin_count:
                    self.check_added_mcguffin_count += self.mcguffin_requirement - self.check_total_mcguffin_count
                    self.check_total_mcguffin_count = self.mcguffin_requirement
        self.check_total_progression_count = self.check_before_progression_count + self.check_added_mcguffin_count

        free_slots = self.check_total_count - self.check_total_progression_count
        if free_slots < 0:
            raise OptionError(f"PowerWash Simulator 2: {self.player_name} has {-free_slots} more items than "
                              f"locations, enable more levels or sanities")

        self.check_total_filler_count = math.floor(free_slots * self.options.local_fill / 100.0)

    def create_regions(self) -> None:
        menu_region = Region("Menu", self.player, self.multiworld)
        self.multiworld.regions.append(menu_region)
        percentsanity = self.options.percentsanity

        for location in self.all_locations:
            location_list: List[str] = []
            next_region = Region(f"Clean the {location}", self.player, self.multiworld)
            self.multiworld.regions.append(next_region)

            if self.options.has_percentsanity():
                for i in range(percentsanity, 100, percentsanity):
                    location_list.append(self.make_location(f"{location} {i}%", next_region).name)

                location_list.append(self.make_location(f"{location} 100%", next_region).name)

            if self.options.has_objectsanity():
                for part in objectsanity_dict[location]:
                    location_list.append(self.make_location(part, next_region).name)

            if self.options.has_stagesanity():
                for stage in stagesanity_dict[location]:
                    location_list.append(self.make_location(stage, next_region).name)

            if self.options.has_bonustasksanity():
                for task in bonustasksanity_dict[location]:
                    location_list.append(self.make_location(task, next_region).name)

            if location in self.goal_levels:
                level_completion_loc = Location(self.player, f"Urge to clean the {location}", None, next_region)
                level_completion_loc.place_locked_item(
                    Item("Satisfied the Urge", ItemClassification.progression, None, self.player))
                next_region.locations.append(level_completion_loc)

            if location == self.starting_location:
                menu_region.connect(next_region)
            else:
                menu_region.connect(next_region,
                    rule=lambda state, location_lock=location: state.has(f"{location_lock} Unlock",
                        self.player))

            self.random.shuffle(location_list)

            if location == self.starting_location and location_list:
                location_list.pop()
            self.filler_locations += location_list

        self.check_added_filler_count = self.check_total_filler_count - len(self.filler_locations)

    def create_item(self, name: str) -> PowerwashSimulator2Item:
        return PowerwashSimulator2Item(name, item_table[name], self.item_name_to_id[name], self.player)

    def get_filler_item_name(self) -> str:
        return self.random.choice(filler_items)

    def generate_output(self, output_directory: str) -> None:
        if not self.gen_puml: return
        from Utils import visualize_regions
        state = self.multiworld.get_all_state(False)
        state.update_reachable_regions(self.player)
        visualize_regions(self.get_region("Menu"), f"{self.player_name}_world.puml",
            show_entrance_names=True,
            regions_to_highlight=state.reachable_regions[self.player])

    def create_items(self) -> None:
        create_items(self)

    def set_rules(self) -> None:
        if self.options.goal_type == 0:
            self.multiworld.completion_condition[self.player] = lambda state: state.has("A Job Well Done", self.player,
                self.mcguffin_requirement)
        else:
            self.multiworld.completion_condition[self.player] = lambda state: state.has("Satisfied the Urge",
                self.player,
                self.check_goal_level_count)

    def pre_fill(self) -> None:
        location_map: List[Location] = [self.multiworld.get_location(loc, self.player) for loc in self.filler_locations]
        filler = self.check_total_filler_count
        filler_size = min(filler, len(location_map))
        self.random.shuffle(location_map)

        while filler_size > 0:
            if len(location_map) == 0:
                raise OptionError(
                    "PowerWash Simulator 2: other worlds took this world's prefill locations; report it to the developers of the other worlds")

            location = location_map.pop()
            try:
                location.place_locked_item(self.create_item(self.random.choice(filler_items)))
                filler_size -= 1
            except:
                pass

    def fill_slot_data(self) -> Dict[str, Any]:
        slot_data: Dict[str, Any] = {
            "starting_location": str(self.starting_location),
            "jobs_done": int(self.mcguffin_requirement),
            "objectsanity": self.options.has_objectsanity(),
            "percentsanity": self.options.has_percentsanity(),
            "stagesanity": self.options.has_stagesanity(),
            "bonustasksanity": self.options.has_bonustasksanity(),
            "goal_levels": str(self.goal_levels),
            "goal_level_amount": int(self.check_goal_level_count),
            "percentsanity_amount": int(self.options.percentsanity),
            "all_levels": self.all_locations
        }

        return slot_data

    def make_location(self, location_name, region) -> Location:
        location = Location(self.player, location_name, self.location_name_to_id[location_name], region)
        region.locations.append(location)
        return location
