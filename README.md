# PowerWash Simulator 2 Archipelago

An [Archipelago](https://archipelago.gg) randomizer for PowerWash Simulator 2, ported from
[SWCreeperKing's PowerWash Simulator randomizer](https://github.com/SWCreeperKing/PowerwashSimAP).
[PARITY.md](PARITY.md) lists every difference from the PWS1 randomizer.

## Randomizer Info BEFORE INSTALLING

### DISCLAIMER: THIS MOD WILL >>NOT<< GIVE YOU THE PAID DLC FOR FREE

### REMINDER: THE RANDOMIZER PLAYS ON YOUR NORMAL SAVE, BACK IT UP FIRST

The save folder is `PC_Steam/SaveData` inside `AppData/LocalLow/FuturLab/PowerWash Simulator 2`. On Linux that folder is in the Proton
prefix: `steamapps/compatdata/2968420/pfx/drive_c/users/steamuser/AppData/LocalLow/FuturLab/PowerWash Simulator 2`.

> [!Note]
> Other mods might cause problems with levels

## Features/Information

- Pick your jobs from the job menu: the Career Mode pack, and Caldera Chronicles and the DLC packs if they're enabled in your YAML
  - while connected, the menu only shows jobs you have the `[job] Unlock` for, and runs them as Free Play jobs, so your career progress isn't changed
  - this also works on a save that hasn't unlocked Free Play yet
- Levels are locked and need an unlock item to play them
  - the starting level is random unless you use `start_with_first_job` (Removals Van)
- Levels will hide themselves once they're completed
- Can choose which levels to add to the level pool
- The hub map's job pins can't be selected while connected, because they start career jobs
- Percentsanity
  - total % cleaned of a level gives checks
- Objectsanity (requires host.yaml setting to enable)
  - cleaning individual parts gives checks (4295 parts in total)
- Stagesanity
  - finishing each stage of a level gives a check (63 stages)
- Bonustasksanity
  - each bonus task gives a check (7 in the game, so use it with another sanity)
- Goal Conditions
  - McGuffin Hunt
    - find `A Job Well Done`s to goal, the amount is automatic or set in your YAML
  - Level Hunt
    - complete a certain amount of required levels to goal
- Each `$WashCoin` you receive gives 50 MuckCoins
- Includes local fill (adjustable with `local_fill`)

### Multiplayer

Working on levels you don't have a `[level] Unlock` for is considered out of logic. Online and split-screen multiplayer have not been tested.

---

## How to install

You need PowerWash Simulator 2 on Steam and Archipelago 0.6.4 or newer.

- Download `MelonLoader.x64.zip` from [MelonLoader v0.7.3](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3)
- Extract it into the game's install folder (Steam → PowerWash Simulator 2 → Manage → Browse local files)
  - `version.dll` and the `MelonLoader` folder should be next to `PowerWash Simulator 2.exe`
- If using Linux/Proton: type `WINEDLLOVERRIDES="version=n,b" %command%` into the launch options
  - without it MelonLoader never starts. Tested on Proton Experimental
- Launch the game once and wait. The first start takes a few minutes while MelonLoader generates its files
- Download `PowerwashSim2AP-v0.1.0.zip` from the [latest release page](https://github.com/naorekopilled/PowerwashSim2AP/releases/latest)
  and extract it into the game's install folder (it contains `Mods/` and `UserLibs/`)
- Download `powerwashsimulator2.apworld` from the same page and install it like any other apworld
  - the options template is `PowerWash.Simulator.2.yaml` on the release page
- Launch the game again and you should see the connection window on the top left
  - click a field to type in it (Enter to finish, Ctrl+V pastes), fill in `address:port`, password and slot, then press Connect
- To uninstall the mod, delete `Mods/PowerwashSim2AP.dll`, or delete `version.dll` to turn off all mods

---

## Options

| Option | Default | What it does |
|---|---|---|
| `start_with_first_job` | `false` | Start on Removals Van instead of a random job (PWS1's `start_with_van`) |
| `land_vehicles` | `All` | Land vehicle jobs in the pool |
| `water_vehicles` | `All` | Water vehicle jobs. PWS2 has none, so this adds nothing |
| `air_vehicles` | `All` | Air vehicle jobs (Airship) |
| `places` | `All` | Building and location jobs |
| `bonus_jobs` | `false` | Add the Caldera Chronicles bonus jobs (Bouncy Castle, Bowling Alley, Miniature Village) |
| `bonus_jobs_levels` | `All` | Which bonus jobs, when `bonus_jobs` is on |
| `adventure_time_dlc` | `false` | Add the Adventure Time DLC jobs (needs the DLC) |
| `adventure_time_dlc_levels` | `All` | Which Adventure Time jobs, when `adventure_time_dlc` is on |
| `star_wars_dlc` | `false` | Add the Star Wars DLC jobs (needs the DLC) |
| `star_wars_dlc_levels` | `All` | Which Star Wars jobs, when `star_wars_dlc` is on |
| `enable_percentsanity` | `true` | Percentsanity checks |
| `percentsanity` | `20` | Percentsanity check interval, 1–20 (20 = checks at 20/40/60/80/100%) |
| `enable_objectsanity` | `false` | Objectsanity checks. Needs the host setting below |
| `enable_stagesanity` | `false` | Stagesanity checks. On its own it only fits the level goal |
| `enable_bonustasksanity` | `false` | Bonustasksanity checks. Needs another sanity alongside it |
| `local_fill` | `55` | % of this world's filler that stays local. Minimum 55, or 97 with Percentsanity and Objectsanity both on |
| `goal_type` | `mcguffin` | `mcguffin`: find enough `A Job Well Done`s. `level`: finish enough of the goal levels |
| `levels_to_goal` | `randomized` | Goal levels for `goal_type: level`: `randomized` (a random half or more of your jobs), `all`, or `custom` |
| `custom_levels_to_goal` | `[]` | The goal levels when `levels_to_goal` is `custom` |
| `amount_of_levels_to_goal` | `randomized` | How many goal levels to finish: `randomized` (at least half of your enabled jobs) or `custom` |
| `custom_amount_of_levels_to_goal` | `20` | The amount when `amount_of_levels_to_goal` is `custom`. It must be at least half of your enabled jobs unless the host allows less |
| `amount_of_mcguffins_to_goal` | `automatic` | How many `A Job Well Done`s the mcguffin goal needs: `automatic` (PWS1's formula: 5% of your checks, at least one per job) or `custom` |
| `custom_amount_of_mcguffins_to_goal` | `30` | The amount when `amount_of_mcguffins_to_goal` is `custom` (1–100). The pool gets more mcguffins if needed; they must fit in your checks |

At least one `enable_*sanity` must be on. Archipelago reserves the word `random` in choices, so the goal presets are called `randomized`.

### host.yaml settings (`powerwashsimulator2_options`)

| Setting | Default | |
|---|---|---|
| `allow_objectsanity` | `false` | Allow players to enable Objectsanity |
| `allow_percentsanity_below_7` | `false` | Allow `percentsanity` below 7 (otherwise it is raised to 7) |
| `allow_below_localfill_minimums` | `false` | Allow `local_fill` below the minimums |
| `allow_potentially_excessive_releases` | `false` | Allow level goals needing fewer than 50% of enabled jobs |

`accessibility: minimal` is not supported and is changed to `full`.

---

## Known Issues

- Objectsanity, Stagesanity, Bonustasksanity, both goals, `$WashCoin` and reconnecting mid-run haven't been played in-game yet. Their logic is
  covered by the APWorld tests, and seeds generate with each of them
- The randomizer can't use a separate save, because PWS2 creates its save storage before MelonLoader starts mods. That's also why there is no shopsanity
- Free Play job tiles show 0% progress
- The hub map pins still show, they just can't be selected

---

## Development

Building the mod needs the .NET 8 SDK and MelonLoader installed in the game with its first start done (the build references
`MelonLoader/Il2CppAssemblies`). The build copies the mod into the game's `Mods` folder and the client library into `UserLibs`. For a game
folder other than `~/.local/share/Steam/steamapps/common/PowerWash Simulator 2`, pass `-p:GamePath=...`.

```bash
dotnet build mod -c Release
```

To regenerate the job data, put `4` into `debug.txt` in the game folder and start the game. About 10 seconds after the menu loads, the mod
writes `Mods/PowerwashSim2AP/dump/{jobs_raw,shop_raw,names}.json`. Copy them into `data/dump/` (new jobs need a category in
`data/categories.json`), set `debug.txt` back to `0` and run the generator, which writes the APWorld's `Data.py` and the mod's `ap_data.json`:

```bash
python3 apworld/gen_data.py
```

The world tests run inside an Archipelago checkout, with `apworld/powerwashsimulator2` linked into its `worlds` folder:

```bash
python -m pytest worlds/powerwashsimulator2/test
```

---

# Special Thanks

- SWCreeperKing for the [PowerWash Simulator randomizer](https://github.com/SWCreeperKing/PowerwashSimAP) this is ported from (MIT)

# Tools:

- MelonLoader (obv)
- Il2CppInterop and Cpp2IL
- [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net) (MIT), shipped in `UserLibs`

MIT, see [LICENSE](LICENSE). This is a fan project, not affiliated with FuturLab.
