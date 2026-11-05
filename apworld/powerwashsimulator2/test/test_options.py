from Options import OptionError
from . import PWS2TestBase
from ..Data import jobs, first_job
from ..Locations import categories


def job_names(*option_names):
    return [name for option in option_names for name in categories[option]]


DEFAULT_JOBS = job_names("land_vehicles", "water_vehicles", "air_vehicles", "places")


class TestDefault(PWS2TestBase):
    def test_default_jobs_are_enabled(self):
        self.assertCountEqual(self.world.all_locations, DEFAULT_JOBS)

    def test_percentsanity_20_gives_five_checks_per_job(self):
        names = {loc.name for loc in self.multiworld.get_locations(self.player) if loc.address is not None}
        expected = {f"{job} {p}%" for job in DEFAULT_JOBS for p in (20, 40, 60, 80, 100)}
        self.assertEqual(names, expected)

    def test_one_unlock_per_job_except_start(self):
        unlocks = [item.name for item in self.multiworld.itempool if item.name.endswith(" Unlock")]
        self.assertCountEqual(unlocks, [f"{job} Unlock" for job in DEFAULT_JOBS if job != self.world.starting_location])


class TestPercentsanityClampedTo7(PWS2TestBase):
    options = {"percentsanity": 3}

    def test_clamped(self):
        self.assertEqual(self.world.options.percentsanity.value, 7)


class TestObjectsanity(PWS2TestBase):
    options = {"enable_percentsanity": False, "enable_objectsanity": True}
    host_settings = {"allow_objectsanity": True}

    def test_one_check_per_object(self):
        names = {loc.name for loc in self.multiworld.get_locations(self.player) if loc.address is not None}
        self.assertEqual(names, {f"{job}: {part}" for job in DEFAULT_JOBS for part in jobs[job]["objects"]})


class TestStagesanity(PWS2TestBase):
    # alone it only fits the level goal: ~1.2 stages per job can't hold an unlock plus mcguffins per job
    options = {"enable_percentsanity": False, "enable_stagesanity": True, "goal_type": "level"}

    def test_one_check_per_stage(self):
        names = {loc.name for loc in self.multiworld.get_locations(self.player) if loc.address is not None}
        self.assertEqual(names, {f"{job} Stage {n}" for job in DEFAULT_JOBS for n in range(1, jobs[job]["stages"] + 1)})


class TestBonustasksanity(PWS2TestBase):
    options = {"enable_bonustasksanity": True}

    def test_one_check_per_bonus_task(self):
        names = {loc.name for loc in self.multiworld.get_locations(self.player) if " Bonus Task " in loc.name}
        self.assertEqual(names, {f"{job} Bonus Task {n}" for job in DEFAULT_JOBS
                                 for n in range(1, jobs[job]["bonus_tasks"] + 1)})


class TestBonustasksanityAloneTooFewLocations(PWS2TestBase):
    options = {"enable_percentsanity": False, "enable_bonustasksanity": True}

    def setUp(self) -> None:
        self._saved_settings = {}  # world_setup runs inside the test instead

    def test_raises(self):
        with self.assertRaises(Exception) as raised:
            self.world_setup()
        self.assertIn("more items than locations", str(raised.exception))


class TestAllSanities(PWS2TestBase):
    options = {"enable_objectsanity": True, "enable_stagesanity": True, "enable_bonustasksanity": True}
    host_settings = {"allow_objectsanity": True}


class TestStartWithFirstJob(PWS2TestBase):
    options = {"start_with_first_job": True}

    def test_starts_on_first_job(self):
        self.assertEqual(self.world.starting_location, first_job)


class TestLevelGoal(PWS2TestBase):
    options = {"goal_type": "level", "levels_to_goal": "all", "amount_of_levels_to_goal": "custom",
               "custom_amount_of_levels_to_goal": len(jobs)}

    def test_goal_needs_every_level(self):
        self.assertEqual(self.world.check_goal_level_count, len(DEFAULT_JOBS))
        self.assertEqual(len([loc for loc in self.multiworld.get_locations(self.player)
                              if loc.name.startswith("Urge to clean the ")]), len(DEFAULT_JOBS))


class TestDLC(PWS2TestBase):
    options = {"adventure_time_dlc": True, "star_wars_dlc": True, "bonus_jobs": True}

    def test_dlc_jobs_added(self):
        self.assertCountEqual(self.world.all_locations,
                              DEFAULT_JOBS + job_names("bonus_jobs", "adventure_time_dlc", "star_wars_dlc"))



class TestObjectsanityNeedsHostSetting(PWS2TestBase):
    options = {"enable_objectsanity": True}
    host_settings = {"allow_objectsanity": False}

    def setUp(self) -> None:
        self.apply_host_settings()  # world_setup runs inside the test instead

    def test_raises(self):
        with self.assertRaises(OptionError):
            self.world_setup()


class TestRandomGoalAmountRespects50Percent(PWS2TestBase):
    options = {"goal_type": "level"}

    def setUp(self) -> None:
        self._saved_settings = {}  # world_setup runs per seed inside the test

    def test_many_seeds(self):
        for seed in range(40):
            with self.subTest(seed=seed):
                self.world_setup(seed)
                self.assertGreaterEqual(self.world.check_goal_level_count * 2, len(DEFAULT_JOBS))


class TestDLCLevelSubset(PWS2TestBase):
    first_two = job_names("star_wars_dlc")[:2]
    options = {"star_wars_dlc": True, "star_wars_dlc_levels": first_two}

    def test_only_listed_dlc_levels(self):
        self.assertCountEqual(self.world.all_locations, DEFAULT_JOBS + self.first_two)


class TestDLCLevelsIgnoredWhenOff(PWS2TestBase):
    options = {"star_wars_dlc": False, "star_wars_dlc_levels": ["All"]}

    def test_no_dlc_levels(self):
        self.assertCountEqual(self.world.all_locations, DEFAULT_JOBS)


class TestCustomGoalLevels(PWS2TestBase):
    goal = DEFAULT_JOBS[:25]
    options = {"goal_type": "level", "levels_to_goal": "custom", "custom_levels_to_goal": goal,
               "amount_of_levels_to_goal": "custom", "custom_amount_of_levels_to_goal": 20}

    def test_goal_levels_and_amount(self):
        self.assertCountEqual(self.world.goal_levels, self.goal)
        self.assertEqual(self.world.check_goal_level_count, 20)


def mcguffins_in_pool(test):
    return sum(item.name == "A Job Well Done" for item in test.multiworld.itempool)


class TestAutomaticMcguffins(PWS2TestBase):
    def test_pws1_formula(self):
        # PWS1: max(min(5% of checks, checks - 2 per level), levels)
        total = len(DEFAULT_JOBS) * 5
        self.assertEqual(self.world.mcguffin_requirement, max(min(total // 20, total - len(DEFAULT_JOBS) * 2),
                                                              len(DEFAULT_JOBS)))
        self.assertGreaterEqual(mcguffins_in_pool(self), self.world.mcguffin_requirement)


class TestCustomMcguffins(PWS2TestBase):
    options = {"amount_of_mcguffins_to_goal": "custom", "custom_amount_of_mcguffins_to_goal": 30}

    def test_requirement(self):
        self.assertEqual(self.world.mcguffin_requirement, 30)
        self.assertGreaterEqual(mcguffins_in_pool(self), 30)


class TestCustomMcguffinsAbovePool(PWS2TestBase):
    # automatic puts 49 in the default pool
    options = {"amount_of_mcguffins_to_goal": "custom", "custom_amount_of_mcguffins_to_goal": 90}

    def test_pool_grows_to_requirement(self):
        self.assertEqual(self.world.mcguffin_requirement, 90)
        self.assertEqual(mcguffins_in_pool(self), 90)


class TestCustomMcguffinsTooMany(PWS2TestBase):
    # only land vehicles: 65 checks can't hold 12 unlocks plus 100 mcguffins
    options = {"amount_of_mcguffins_to_goal": "custom", "custom_amount_of_mcguffins_to_goal": 100,
               "water_vehicles": [], "air_vehicles": [], "places": []}

    def setUp(self) -> None:
        self.apply_host_settings()  # world_setup runs inside the test instead

    def test_raises(self):
        with self.assertRaises(Exception) as raised:
            self.world_setup()
        self.assertIn("more items than locations", str(raised.exception))
