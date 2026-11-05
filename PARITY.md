# PWS1 → PWS2 parity

Phase 1 output. Nothing here is implemented yet.

## Sources

| Source | What | Where it came from |
|---|---|---|
| PWS1 APWorld v0.5.14 | `__init__.py`, `Options.py`, `Locations.py`, `Items.py`, `ApworldLocations.py`, `archipelago.json` | release asset `powerwashsimulator.apworld` (SWCreeperKing/PowerwashSimAP, tag v0.5.14) |
| PWS1 YAML template | `Powerwash.Simulator.yaml` | same release |
| PWS1 mod | `src/*.cs`, `src/Patches/*.cs` | repo master, shallow clone |
| PWS2 code | Cpp2IL `diffable-cs` dump (signatures and field offsets, no method bodies) | `Cpp2IL-2022.1.0-pre-release.21-Linux` (already on this machine) run on the PWS2 install. Log: `Using actual IL2CPP Metadata version 31.1`, `Processed 160812 OK` |
| PWS2 content | strings in the Addressables catalogs | `PowerWash Simulator 2_Data/StreamingAssets/aa/catalog.bin`, `DLC/BBG/catalog_BBG.bin`, `DLC/HUT/catalog_HUT.bin` |

PWS2 file paths below are relative to the Cpp2IL output root (`DiffableCs/`).
"Unverified" means the name exists in metadata but I have not seen it run. Cpp2IL gives no method bodies, so every PWS2 behavior claim is unverified until the mod runs.

## a) PWS1 feature → PWS2 implementation

### APWorld options

| PWS1 option | Type / default | PWS1 behavior | PWS2 implementation | Status |
|---|---|---|---|---|
| `start_with_van` | Toggle, false | Van is the starting level; otherwise random start (`check_options`) | No "Van" job in PWS2. See difference D4 | needs decision |
| `land_vehicles` | OptionSet, `All` | adds levels to pool | No such grouping in PWS2. See D1 | needs decision |
| `water_vehicles` | OptionSet, `All` | same | No water vehicle jobs found. See D1 | needs decision |
| `air_vehicles` | OptionSet, `All` | same | Possible: `GAM_PW2_RG_Airship` / `GAM_PW2_RG_DualRotorHelicopter`. See D1 | needs decision |
| `places` | OptionSet, `All` | same | See D1 | needs decision |
| `bonus_jobs` | OptionSet, `[]` | same | PWS2 has a bonus-job mode: `GameMode.BonusJob = 4` (`FuturLab.PW2.Core/FuturLab/PW2/GameMode.cs`), `Campaign.IsBonusJobsOnly`, `Campaign.GetUnlockedBonusJobs` (`FuturLab.PW2.GameContent/FuturLab/PW2/Campaign.cs`). Which jobs are bonus jobs is unknown until the Phase 3 dump. See D2 | needs decision |
| `midgar`, `tomb_raider`, `wallace_and_gromit_dlc`, `shrek_dlc`, `alice_in_wonderland_dlc`, `warhammer_40k_dlc`, `back_to_the_future_dlc`, `spongebob_dlc` | OptionSet, `[]` | DLC level sets | PWS1-only content. See D3 | needs decision |
| `sanities` | OptionSet {Percentsanity, Objectsanity}, `Percentsanity` | which check types exist | same option, same semantics | direct port |
| `percentsanity` | Range 1–20, 20 | check every N % plus 100 % | same | direct port |
| `local_fill` | Range 0–100, 55 | % of non-progression slots pre-filled locally; forced ≥55 (one sanity) or ≥97 (both) unless host allows | same | direct port |
| `goal_type` | Choice mcguffin=0 / level=1, mcguffin | | same | direct port |
| `levels_to_goal` | OptionSet (any level + `Random`, `All`), `Random` | | same, keys = PWS2 job names | direct port |
| `amount_of_levels_to_goal` | Range −1 … len(levels)+99, −1 | −1 = random ≥50 % | same | direct port |
| `accessibility` | forced `full` if `minimal` | `check_options` | same | direct port |

### host.yaml settings (`PowerwashSimulatorSettings`)

PWS1 has four, not two. The task text names two; see D22.

| Setting | Default | Port |
|---|---|---|
| `allow_percentsanity_below_7` | false | direct |
| `allow_objectsanity` | false | direct |
| `allow_below_localfill_minimums` | false | direct (proposed) |
| `allow_potentially_excessive_releases` | false | direct (proposed) |

### Locations

| PWS1 location | PWS1 source | PWS2 implementation | Evidence |
|---|---|---|---|
| `[level] N%` (Percentsanity) | `LevelProgressionSender.HandleProgressChanged` postfix reads `m_currentPercentage` (`LevelProgressionPatch.cs`) | Subscribe to `GameStateManager.WashProgressPercentageChanged` (event, args `PlayingState, int prev, int new`) or postfix `PlayingState.OnWashProgressPercentageChanged(int, int)` | `FuturLab.PW2/FuturLab/PW2/GameStateManager.cs`, `FuturLab.PW2/FuturLab/PW2/PlayingState.cs`. Unverified: whether free play reports % over all stages or only unlocked ones |
| `[level]: [part]` (Objectsanity) | per-`WashTarget` `Update` component checks `CleanProgress >= 1`, name = GameObject name with `_`→space (`WashTargetPatch.cs`) | `WashManager.HandleWashTargetCompletion(WashTarget)` postfix (or the private `WashManager.WashTargetCompleted` event). `WashTarget.CleanProgress` still exists. Name source: see D20 | `FuturLab.PW2/FuturLab/PW2/WashManager.cs`, `FuturLab.PW2.TexturePainter/FuturLab/PW2/TexturePainter/WashTarget.cs` |
| `Urge to clean the [level]` (event, level goal) | created in `create_regions` for goal levels; mod reports level completion to data storage key `levels_completed` | same; level complete = 100 % reached, or `PlayingState.OnJobCompleted()` / `PlayingFreePlayState.CompleteJob()` | `PlayingState.cs`, `FuturLab.PW2/FuturLab/PW2/PlayingFreePlayState.cs` |
| Location IDs | `uuid_offset = 0x3AF4F1BC`; IDs for every level × 1..100 % plus every objectsanity part, independent of options | same scheme, new offset for the new game | — |

### Items

| PWS1 item | Class | PWS2 | Mod effect in PWS2 |
|---|---|---|---|
| `[level] Unlock` | progression | same name pattern | job shown in job list. Hook: `JobSelectionState.BuildActiveJobsList` / `GetActiveJobs` (returns `CampaignJobPreview` list) or `JobSelectionTilesWidget.OnScreenContentChanged(IEnumerable<CampaignJobPreview>, …)`; also `Campaign.GetUnlockedFreePlayJobs(CampaignSaveData)`. Evidence: `FuturLab.PW2/FuturLab/PW2/JobSelectionState.cs`, `FuturLab.PW2.UI/FuturLab/PW2/UI/JobSelectionTilesWidget.cs`, `Campaign.cs` |
| `A Job Well Done` | progression_deprioritized_skip_balancing | same | counter in GUI; goal when count ≥ `jobs_done` |
| `Satisfied the Urge` | locked event item | same | — |
| filler: Dirt, Grime, Satisfaction, Water, Sponge, Bubblegum Flavored Soap, H2O, Positive Reviews, C17H35COONa, Dust Bnuy, Dust Bunny, $WashCoin, Suds, Washed Grass | filler | same list | none (PWS1 does nothing with them). See D9 |

Item pool math (unlocks = levels − 1, mcguffins = levels + 10 % of remaining, local filler %, `mcguffin_requirement`) ports unchanged.

### Goals

| Goal | PWS1 | PWS2 |
|---|---|---|
| mcguffin | `Jobs >= WinCondition` → `Client.Goal()` (`ApDirtClient.Update`) | same, mod-side only |
| level | `levels_completed` data storage + `GoalLevelCheck` | same, mod-side only |

### Slot data

Port unchanged: `starting_location`, `jobs_done`, `objectsanity`, `percentsanity`, `goal_levels`, `goal_level_amount`, `percentsanity_amount`, `all_levels`. Universal Tracker `re_gen_passthrough` handling ports unchanged.

### Mod behavior

| PWS1 behavior | PWS1 code | PWS2 target | Evidence / status |
|---|---|---|---|
| Free Play only | hides `MainMenuButton_Career`, `_Challenges`, shop/DLC buttons via `FuturButton.Start` blacklist (`MainMenuButtonPatch.cs`) | `JobSelectionState` has `SwitchGameMode()` / `CanSwitchGameModes`; block Career by patching `CanSwitchGameModes` and forcing `GameMode.FreePlay` in `PopulateScreen(…, GameMode)` | unverified; the UI flow (hub → van/laptop → `JobPackSelectionState` → `JobSelectionState`) is inferred from type names |
| Free play available without beating the job | `CampaignSaveData.IsFreePlayUnlocked` prefix → true (`JobVisibilityPatcher.cs`, `FreePlayUnlockPatch`) | `Campaign.IsFreePlayUnlocked(GameJob, CampaignSaveData)` (private) prefix → true; also `Campaign.GetFreePlayStatus` | `Campaign.cs`. See D14 |
| Each level locked behind `[level] Unlock` | `JobListElement.Start` postfix sets inactive (`JobLevelPatch.cs`) | filter `CampaignJobPreview` list in `JobSelectionState` (see Items) | unverified |
| Random starting level | slot data `starting_location` | same | — |
| Levels hide once all checks are done (`CachedLevelsCompleted`) | `JobLevelPatch`, `MainMenuButtonPatch.IsAnyChildVisible` | same filter, plus hide empty packs in `JobPackSelectionState.PopulateScreen(List<CampaignProgressData>)` | `FuturLab.PW2/FuturLab/PW2/JobPackSelectionState.cs` |
| Connection GUI on title screen | `APGui` IMGUI added to `PlayerUIInput` in `DontDestroyOnLoad` (`MainMenuPatch.cs`) | MelonLoader `OnGUI`, shown while `SplashState` / menu states are active; same fields, `ApConnection.txt` persistence | `FuturLab.PW2/FuturLab/PW2/SplashState.cs` |
| Check send queue (3 s batch), failsafe resend on level complete, regex per level | `ApDirtClient.cs` | port as-is | — |
| Item receiving | `ApDirtClient.ReceiveItems` | port as-is | — |
| Debug modes via `debug.txt` (Jobs, Buttons, Washables, Stats…) | `Plugin.cs`, `APGui.OnGUI` "Save Washables" | same file, Washables mode writes `data/` JSON. Job list source: `GameDataManifest.Campaigns`, `GameDataManifest.GetLoadedGameJobs()`, `GameContent.UniqueName` / `LocalizedName` | `FuturLab.PW2.GameContent/FuturLab/PW2/GameDataManifest.cs`, `GameContent.cs` |
| Local fill | APWorld `pre_fill` | port as-is | — |
| DeathLink | **not in PWS1** (no match for `deathlink` in repo or APWorld) | not added | — |
| Shop / power washer patches | `ShopPatch`, `BuyPowerWasherPatch` exist but are **commented out** in `Plugin.Load` | not ported | — |
| Separate randomizer save | PWS1 has none | see D17 | — |
| Testing aid | — | `PlayingState.CheatCompleteAllTasks`, `CheatCompleteStage`, `WashManager.SetWashTargetAsClean`, `CheatManager` (`FuturLab/FuturLab/CheatManager.cs`), `PwsCheatSettings.CheatInstantCleanOn` | debug-only |

## b) PWS2 job list

From the Addressables catalogs: one `GAM_*` GameJob asset plus one `GAM_*.unity` scene per job.
Display names below are my readings of the internal names. They are **unverified**; the Phase 3 dump replaces them with `LocalizedName`.
Which campaign or job pack owns each job is also unverified. `Campaign` owns `CampaignJobPackReference[]` and `JobPackManifest`s, and the dump will read those.

Two-letter code = zone/location prefix from the asset name. Zone scenes seen: `ZON_PW2_FM_Farm`, `ZON_PW2_HS_HighStreet`, `ZON_PW2_HW_Highway`, `ZON_PW2_RG_RecreationGround`, `ZON_PW2_RL_Rushless`, `ZON_PW2_RS_ResidentialStreet`. `GAM_PW2_HB_HomeBase` is the hub, not a job.

### Base game campaign (`Campaign_Default`, catalog.bin)

| Zone | Jobs (internal → reading) |
|---|---|
| FM (Farm) | `FM_Farm` Farm, `FM_TempleInterior` Temple Interior, `FM_Tractor` Tractor |
| HB (Home Base) | `HB_CarCaravan` Car & Caravan, `HB_DogCar` Dog Car, `HB_Limo` Limo, `HB_MobilityScooter` Mobility Scooter, `HB_RemovalVan` Removal Van |
| HS (High Street) | `HS_PublicToilet` Public Toilet, `HS_RoadSweeper` Road Sweeper, `HS_TownFloats` Town Floats, `HS_Tram` Tram |
| HW (Highway) | `HW_Billboard` Billboard, `HW_FuturisticBike` Futuristic Bike, `HW_GasStation` Gas Station, `HW_Lorry` Lorry, `HW_Motel` Motel |
| MS (?) | `MS_CementMixer` Cement Mixer, `MS_DumpTruck` Dump Truck |
| MU (?) | `MU_SolarSystem` Solar System |
| NP (?) | `NP_Campsite` Campsite, `NP_SalvageHouse` Salvage House, `NP_StoneCircle` Stone Circle, `NP_TeaRoom` Tea Room |
| RD (?) | `RD_RollerDisco` Roller Disco |
| RG (Recreation Ground) | `RG_Bandstand` Bandstand, `RG_CrocRide` Croc Ride, `RG_FunHouse` Fun House, `RG_RockClimbingPark` Rock Climbing Park, `RG_ShootingGallery` Shooting Gallery, `RG_Airship` / `RG_DualRotorHelicopter` (asset and scene names differ; probably one job) |
| RL (Rushless) | `RL_CrystalCave` Crystal Cave, `RL_SkiResort` Ski Resort |
| RS (Residential Street) | `RS_DecoHouse` Deco House, `RS_NouveauHouse` Nouveau House |
| SH (?) | `SH_ShoppingMall` Shopping Mall |
| TH (Theater) | `TH_PantoSet` Panto Set, `TH_Theater` Theater |

37 jobs, counting Airship/Helicopter once. Leftovers with an asset but no scene: `GAM_PW2_HB_FuturisticBike`, `GAM_PW2_NP_ModelVillage` (probably superseded by the HW and XMV versions).

Other campaigns in catalog.bin: `Campaign_Testing` (dev) and `Campaign_XCC` (unknown, possibly a challenge or bonus campaign).

### Extra job packs (catalog.bin, own `JobPackManifest`)

| Pack | Job |
|---|---|
| XBA | `GAM_XBA_BA_BowlingAlley` Bowling Alley |
| XBC | `GAM_XBC_RG_BouncyCastle` Bouncy Castle |
| XMV | `GAM_XMV_NP_ModelVillage` Model Village |

Free update, pre-order bonus or bonus jobs? Unknown. These are the likeliest `bonus_jobs` candidates (D2).

### DLC packs (installed: `DLC/BBG`, `DLC/HUT`)

| Pack | Campaign | Jobs |
|---|---|---|
| BBG | `Campaign_BBG`, `JobPackManifest_BBG` | `CK_CandyVehicles` Candy Vehicles, `CK_PizzaSassys` Pizza Sassys, `GL_LivingRoom` Living Room, `GL_TreeHouse` Tree House, `IK_IceKingCastle` Ice King Castle |
| HUT | `Campaign_HUT`, `JobPackManifest_HUT` | `AF_Bridge` Bridge, `HO_ATAT` AT-AT, `HR_Falcon` Falcon, `HR_XWing` X-Wing, `TO_Cantina` Cantina, `TO_Homestead` Homestead |

`ContentPack_ART`, `_RMT`, `_XLM` exist in the catalog with no jobs found; they may be future or unowned DLC.

### Jobs inside jobs

Each `GameJob` has `GameJobStage[] m_stages` and `List<GameJobBonusTask> m_bonusTasks`. A stage has `GameJobTask[]`, and a task has `GameJobWashTask`s (one per washable component, with localization keys). See D6, D7, D20.

## c) Differences

Each entry gives options. ★ marks the one I recommend.

**D1. Land / water / air vehicles and places categories.** PWS2 does not group jobs this way. It groups them by campaign and zone.
- ★ (a) Keep the four option names and sort PWS2 jobs into them by what they are (Tram, Lorry, Limo… = land; Airship/Helicopter = air; buildings = places). `water_vehicles` stays, accepting `All`, with no jobs in it unless the dump finds one. This keeps PWS1 YAMLs valid.
- (b) Replace them with one option per PWS2 zone (`farm`, `high_street`, `highway`, …).
- (c) Replace all four with a single `campaign_jobs` OptionSet, default `All`.

**D2. `bonus_jobs`.** PWS2 has a bonus-job mode (`GameMode.BonusJob`) and extra packs (XBA/XBC/XMV), but which jobs are "bonus" is unknown until the dump.
- ★ (a) Keep `bonus_jobs` (default `[]` as in PWS1). Fill it with PWS2 bonus-mode jobs plus XBA/XBC/XMV after the dump confirms them.
- (b) Put XBA/XBC/XMV into the D1 categories and drop `bonus_jobs`.
- (c) Drop it.

**D3. PWS1-only DLC options** (`midgar`, `tomb_raider`, and the six `*_dlc`).
- ★ (a) Drop them.
- (b) Keep them as no-op options that accept only `[]`, so old PWS1 YAMLs still parse after the game name is changed.
- (c) Reuse the free-DLC idea (midgar/tomb_raider were free in PWS1) for any free PWS2 pack.

**D4. `start_with_van`.** PWS2 has no "Van" job. The van is the player's vehicle.
- ★ (a) Keep the name and make it mean "start with the campaign's first job" (`Campaign.TutorialJobId`), so the mechanic stays the same.
- (b) Rename it to `start_with_first_job` with the same behavior.
- (c) Drop it; the start is always random.

**D5. PWS2 DLC (BBG, HUT).**
- ★ (a) Add one OptionSet per pack, in PWS1 DLC style (default `[]`, "you need to own this DLC" docstring). Names would be `<pack>_dlc`, confirmed from the localized pack names after the dump.
- (b) One `dlc_jobs` OptionSet covering both.
- (c) Leave DLC out.

**D6. Job stages** (`GameJobStage`, `GameJob.IsStageUnlocked`, `PlayingState.OnStageCompleted`). PWS1 levels have no stages.
- ★ (a) Ignore stages. Percentsanity uses the whole-job %. Free play is assumed to open all stages (to verify in Phase 3).
- (b) Opt-in `Stagesanity`: each stage completion is a location.
- (c) Stages as progressive items (stage n locked behind an item). This is more logic and more mod work.

**D7. Bonus tasks** (`GameJobBonusTask`, `PlayingState.OnBonusTaskCompleted`).
- ★ (a) Ignore them.
- (b) Opt-in sanity: each bonus task is a location.
- (c) Fold them into objectsanity.

**D8. Equipment / shop unlocks** (`UnlockManager.UnlockEquipment/LockEquipment`, `ShopManager`, `ShopContentScreen.PurchaseShopContent`). PWS1 left its shop vanilla (patches disabled).
- ★ (a) Leave the shop vanilla. Not in logic.
- (b) Equipment as useful (non-progression) items, with the shop locked.
- (c) Shop purchases as locations.

**D9. Currency** (credits = `PrimaryCurrencyValue`, MuckCoins = `MuckCoinValue`; `ShopManager.AddCurrency` is private).
- ★ (a) Ignore it. Filler stays cosmetic, as in PWS1.
- (b) Make `$WashCoin` filler grant MuckCoins.
- (c) Add new currency filler items.

**D10. Cosmetics** (`CosmeticsSaveData`, outfit/van/washer skins).
- ★ (a) Ignore them.
- (b) Cosmetics as filler items.
- (c) Locations for unlocking cosmetics.

**D11. Washable furniture** (`WashTarget.IsFurniture`, `WashableFurnitureManifest`). The player places it, so it is not fixed per job.
- ★ (a) Exclude furniture `WashTarget`s from objectsanity and from the dump.
- (b) Include them.
- (c) One location per furniture type.

**D12. Split-screen co-op and online multiplayer** (`PlayingState.CheatToggleSplitscreen`, `CloneForCouchPlayer`, Photon/Netcode). PWS1 documents multiplayer as working but out of logic.
- ★ (a) Same as PWS1: no special handling, README note only.
- (b) Block online multiplayer while connected to AP; split-screen allowed.
- (c) Block both.

**D13. Career mode.** PWS1 hides Career.
- ★ (a) Same: Free Play only, Career blocked while connected.
- (b) Allow Career with no checks.
- (c) Allow checks in Career too.

**D14. Free-play prerequisite.** PWS1's README says beat levels in vanilla first, but its `FreePlayUnlockPatch` forces free play open anyway.
- ★ (a) Patch `Campaign.IsFreePlayUnlocked` → true, so a fresh save works.
- (b) Keep the vanilla prerequisite and document it as PWS1's README does.

**D15. Hub world.** PWS2 boots to a splash screen and then a walkable hub (`GameMode.Hub`). There is no PWS1-style main menu.
- ★ (a) Show the connection GUI on the splash/title screen and in the hub until connected. Job filtering happens in the job selection screen.
- (b) GUI only on the splash screen.

**D16. Trophies, prestige, messages** (`TrophyData`, `Campaign.CanBePrestiged`, `MessageSequenceData`).
- ★ (a) Ignore them.
- (b) Trophies as locations (opt-in).

**D17. Separate randomizer save.** PWS1 has nothing here. PWS2 has 3 save slots (`SaveManager.MAX_SAVE_SLOTS = 3`, `LoadSlot(byte, bool, CancellationToken)`, `CurrentSlot`, PlayerPrefs key `SaveData_CurrentSlotIndex`). Only `SaveManager` calls `LoadSlot`, so slots may not be exposed in the UI.
- ★ (a) While AP is active, the mod loads a dedicated slot (slot index 2). It refuses to connect if that slot was not created by the mod (marker file), so it never overwrites a real save.
- (b) Back up the whole save folder on first connect, then play on the normal slot.
- (c) Redirect the save path (`SaveDataOverridePath` string exists in metadata; unverified what it does).

**D18. DeathLink.** PWS1 has none, so none is added (per the task). Listed only so it is explicit.

**D19. Game name and IDs.** The new world `PowerWash Simulator 2` gets a new `uuid_offset`. PWS1 YAMLs will not load as-is either way, because the `game:` key differs.
- ★ (a) New offset, same ID layout.

**D20. Objectsanity part names.** PWS1 used the `WashTarget` GameObject name (`Door 0`, `Tyre 1`). PWS2 also has `GameJobWashTask` localization keys and component unique IDs.
- ★ (a) GameObject name, as in PWS1. Duplicates get the same ` N` suffix scheme the dump finds.
- (b) Localized task name (readable, but may change with language or patches).
- (c) Unique ID (stable, unreadable).

**D21. Percentsanity granularity.** PWS1 counts 1..100 % IDs for every level. PWS2's `ProgressToPercent` / `WashProgressPercentageChanged` are ints, so it is the same.
- ★ (a) Identical port. Listed only to confirm.

**D22. host.yaml settings.** The task names two (objectsanity, percentsanity below 7). PWS1 has four (`allow_below_localfill_minimums`, `allow_potentially_excessive_releases` too).
- ★ (a) Keep all four.
- (b) Keep only the two named.

## d) Decisions (user, 2026-11-05)

| # | Decision |
|---|---|
| D1 | (a) Keep `land_vehicles`, `water_vehicles`, `air_vehicles`, `places`; sort PWS2 jobs into them by type. |
| D2 | (a) Keep `bonus_jobs` (default `[]`); fill it with bonus-mode jobs and the XBA/XBC/XMV packs once the dump confirms them. |
| D3 | Drop all PWS1 DLC options. |
| D4 | (b) Rename to `start_with_first_job` (start on `Campaign.TutorialJobId` instead of a random level). |
| D5 | (a) One OptionSet per PWS2 DLC pack, default `[]`. |
| D6 | (b) `Stagesanity` added to `sanities`: each finished stage is a location. |
| D7 | (b) `Bonustasksanity` added to `sanities`: each bonus task is a location. |
| D8 | ~~Shopsanity + `equipment_in_pool`~~ **Dropped (user, later on 2026-11-05):** the separate save (D17) could not be made to work, and both options would write to the real save. Shop and equipment stay vanilla, as in PWS1. |
| D9 | (b) `$WashCoin` filler grants 50 MuckCoins each, once per item (count kept in AP data storage). |
| D10 | Dropped with D8: cosmetics are not items or locations. |
| D11 | (a) Furniture `WashTarget`s excluded from objectsanity. |
| D12 | (a) No multiplayer blocking; README note only. |
| D13 | Free Play only, as PWS1. |
| D14 | (a) As PWS1's code: free play is forced open (`Campaign.IsFreePlayUnlocked` → true); `[job] Unlock` items decide what is playable. |
| D15 | As PWS1: IMGUI connection window from the title screen on, kept across scenes, showing goal progress once connected. |
| D16 | (a) Ignore trophies, prestige, messages. |
| D17 | ~~Dedicated save slot~~ **Changed (user):** plays on the real save, as PWS1. The early slot/redirect attempts never ran because `PatchAll(assembly)` silently skipped every patch class (fixed by patching per class, as PWS1). With patches applied, a prefix on the `FilesystemSaveStorage(string)` constructor still never fired: the game creates its save storage before MelonLoader initializes mods. A MelonLoader plugin that patches earlier is the untried remaining option. |
| D18 | No DeathLink. |
| D19 | New `uuid_offset`, PWS1 ID layout. |
| D20 | (a) GameObject names for objectsanity parts. |
| D22 | (a) Keep all four host.yaml settings. |

**YAML shape (user, 2026-11-05, after testing the Options Creator):**
- `sanities` replaced by `enable_percentsanity` (default on), `enable_objectsanity`, `enable_stagesanity`, `enable_bonustasksanity`.
- `bonus_jobs`, `adventure_time_dlc`, `star_wars_dlc` are toggles (default off) with `*_levels` lists (default All).
- `levels_to_goal` is a choice `randomized` / `all` / `custom` with `custom_levels_to_goal`.
- `amount_of_levels_to_goal` is a choice `randomized` / `custom` with `custom_amount_of_levels_to_goal`.
- New (user): `amount_of_mcguffins_to_goal` `automatic` (PWS1 formula) / `custom` with `custom_amount_of_mcguffins_to_goal`.

## Blocked until approved (all approved 2026-11-05)

These are on the stop-and-ask list:

1. **.NET SDK.** None is installed (`dotnet: command not found`). It is needed to build the mod.
2. **MelonLoader 0.7.x** in the PWS2 folder (not installed). Its first run also generates the Il2CppInterop assemblies the mod compiles against. That means launching the game, which may contact PlayFab at boot (unverified).
3. **Archipelago.MultiClient.Net** NuGet package (mod dependency).
4. For APWorld tests I would copy an existing local Archipelago 0.6.8 checkout into a scratch folder. That is a copy, not a download. Its Python deps may need installing; unknown until I try.
