# PowerWash Simulator 2 Archipelago Randomizer

An [Archipelago](https://archipelago.gg) randomizer for PowerWash Simulator 2, ported from
[SWCreeperKing's PowerWash Simulator randomizer](https://github.com/SWCreeperKing/PowerwashSimAP).
The options, checks, items and goals match PWS1 wherever PWS2 allows. [PARITY.md](PARITY.md) lists every difference and the decision behind it.

- `apworld/`: the Archipelago world (`powerwashsimulator2`), its data generator and the YAML template
- `mod/`: the MelonLoader mod (C#)
- `data/`: the mod's job/shop dump and the hand-sorted job categories

## How it plays

- You can only play Free Play (and the Caldera Chronicles bonus jobs). While you're connected, the job menu opens in Free Play and Career shows no jobs, even on a save that hasn't unlocked Free Play yet.
- Each job is locked behind a `[job] Unlock` item. You start with one random job, or with Removals Van if `start_with_first_job` is on.
- A job disappears from the job list once you finish it.
- The hub map's job pins can't be selected while you're connected, because they would start career jobs.
- As in PWS1, the randomizer plays on your normal save, and the shop and equipment are vanilla.
  Back up your save first: `steamapps/compatdata/2968420/pfx/drive_c/users/steamuser/AppData/LocalLow/FuturLab/PowerWash Simulator 2/PC_Steam/SaveData`
  on Linux, `%USERPROFILE%\AppData\LocalLow\FuturLab\PowerWash Simulator 2\PC_Steam\SaveData` on Windows.
- Each `$WashCoin` you receive gives 50 MuckCoins.
- There is no DeathLink, matching PWS1.

> [!WARNING]
> This mod does not give you DLC. Only add Adventure Time or Star Wars jobs if you own that DLC.

## Install

You need PowerWash Simulator 2 on Steam and Archipelago 0.6.4 or newer.

### 1. MelonLoader

1. Download `MelonLoader.x64.zip` from [MelonLoader v0.7.3](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3). This version is tested with PWS2 (Unity 6000.0.73).
2. Extract it into the game folder, so that `version.dll` and the `MelonLoader` folder sit next to `PowerWash Simulator 2.exe`.
   Steam → PowerWash Simulator 2 → Manage → Browse local files opens that folder.
3. **Linux / Steam Deck (Proton) only:** set the launch option (Steam → PowerWash Simulator 2 → Properties → General → Launch Options):

   ```
   WINEDLLOVERRIDES="version=n,b" %command%
   ```

   Without it, Proton ignores `version.dll` and MelonLoader never starts. Tested on Proton Experimental.
4. Start the game once and wait. The first start takes a few minutes while MelonLoader generates its files. `MelonLoader/Latest.log` should end with `Support Module Loaded`.

**Windows:** steps 1, 2 and 4 only; no launch option.

### 2. The mod

1. Download `PowerwashSim2AP-v0.1.0.zip` from the [latest release](https://github.com/naorekopilled/PowerwashSim2AP/releases/latest).
2. Extract it into the game folder. It contains `Mods/PowerwashSim2AP.dll` and `UserLibs/Archipelago.MultiClient.Net.dll`.
3. Start the game. The AP connection window shows in the top left. Click a field to type in it (Enter to finish, Ctrl+V pastes), fill in `address:port`, password and slot name, then press Connect. These are saved to `Mods/PowerwashSim2AP/ApConnection.txt`.

To uninstall, delete `Mods/PowerwashSim2AP.dll`. To turn off all mods, delete `version.dll`.

### 3. The APWorld

Download `powerwashsimulator2.apworld` from the release and put it into Archipelago's `custom_worlds` folder
(on Linux AppImage installs: `~/.local/share/Archipelago/worlds`). The YAML template `PowerWash.Simulator.2.yaml` is in the release and in
[`apworld/`](apworld/PowerWash.Simulator.2.yaml). You can also generate it with the Archipelago Launcher's "Generate Template Options".

## YAML options

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
| `enable_percentsanity` | `true` | Percentsanity checks, see below |
| `percentsanity` | `20` | Percentsanity check interval, 1–20 (20 = checks at 20/40/60/80/100%) |
| `enable_objectsanity` | `false` | Objectsanity checks. Needs the host setting below |
| `enable_stagesanity` | `false` | Stagesanity checks |
| `enable_bonustasksanity` | `false` | Bonustasksanity checks |
| `local_fill` | `55` | % of this world's filler that stays local. Minimum 55, or 97 with Percentsanity and Objectsanity both on |
| `goal_type` | `mcguffin` | `mcguffin`: find enough `A Job Well Done`s. `level`: finish enough of the goal levels |
| `levels_to_goal` | `randomized` | Goal levels for `goal_type: level`: `randomized` (a random half or more of your jobs), `all`, or `custom` |
| `custom_levels_to_goal` | `[]` | The goal levels when `levels_to_goal` is `custom` |
| `amount_of_levels_to_goal` | `randomized` | How many goal levels to finish: `randomized` (at least half of your enabled jobs) or `custom` |
| `custom_amount_of_levels_to_goal` | `20` | The amount when `amount_of_levels_to_goal` is `custom`. It must be at least half of your enabled jobs unless the host allows less |
| `amount_of_mcguffins_to_goal` | `automatic` | How many `A Job Well Done`s the mcguffin goal needs: `automatic` (PWS1's formula: 5% of your checks, at least one per job) or `custom` |
| `custom_amount_of_mcguffins_to_goal` | `30` | The amount when `amount_of_mcguffins_to_goal` is `custom` (1–100). The pool gets more mcguffins if needed; they must fit in your checks |

At least one `enable_*sanity` must be on. Archipelago reserves the word `random` in choices, which is why the goal presets are called `randomized`.

Sanities:

- **Percentsanity:** every N % of a job cleaned is a check.
- **Objectsanity:** every washable part of a job is a check (4295 parts in total). Needs the host setting below.
- **Stagesanity:** every finished stage of a job is a check. With the `mcguffin` goal it needs another sanity alongside it: there are 63 stages over 52 jobs, which is too few checks for the unlock and mcguffin items.
- **Bonustasksanity:** every bonus task is a check. Bonus tasks are hidden targets you spray until they fill up. There are only 7 in the game, so this needs another sanity alongside it.

### host.yaml settings (`powerwashsimulator2_options`)

| Setting | Default | |
|---|---|---|
| `allow_objectsanity` | `false` | Allow players to enable Objectsanity |
| `allow_percentsanity_below_7` | `false` | Allow `percentsanity` below 7 (otherwise it is raised to 7) |
| `allow_below_localfill_minimums` | `false` | Allow `local_fill` below the minimums |
| `allow_potentially_excessive_releases` | `false` | Allow level goals needing fewer than 50% of enabled jobs |

`accessibility: minimal` is not supported and is changed to `full`.

## Multiplayer

As in PWS1, nothing is blocked, but cleaning a job you have no unlock for is out of logic.

## Building from source

Needs the .NET 8 SDK, and MelonLoader installed in the game with its first start done (the build references
`MelonLoader/Il2CppAssemblies`).

```bash
dotnet build mod -c Release
```

The build copies the mod into the game's `Mods` folder and the client library into `UserLibs`. For a game folder other than
`~/.local/share/Steam/steamapps/common/PowerWash Simulator 2`, pass `-p:GamePath=...`.

### Regenerating the data

1. Put `4` into `debug.txt` in the game folder and start the game. About 10 seconds after the menu loads, the mod writes
   `Mods/PowerwashSim2AP/dump/{jobs_raw,shop_raw,names}.json`. Set it back to `0` afterwards. Only `jobs_raw` and `names` are used.
2. Copy those files into `data/dump/`. New jobs need a category in `data/categories.json`.
3. Run the generator. It writes `apworld/powerwashsimulator2/Data.py` and the mod's `mod/Resources/ap_data.json`:

   ```bash
   python3 apworld/gen_data.py
   ```

### Tests

The world tests run inside an Archipelago checkout, with the world linked into its `worlds` folder:

```bash
ln -s "$PWD/apworld/powerwashsimulator2" /path/to/Archipelago/worlds/powerwashsimulator2
```

```bash
cd /path/to/Archipelago && python -m pytest worlds/powerwashsimulator2/test
```

## Status (v0.1.0)

Tested in-game on Linux (Proton Experimental): connecting, the Free Play job list and unlocks, Percentsanity checks, level completion
(failsafe resend, finished jobs hidden) and the blocked hub map pins.

Not yet tested in-game: Objectsanity, Stagesanity and Bonustasksanity checks, the mcguffin and level goals, `$WashCoin` MuckCoins, and reconnecting
mid-run. Their logic is covered by the APWorld tests, and seeds generate with each of them.

Known limitations:

- The randomizer can't use a separate save: PWS2 creates its save storage before MelonLoader starts mods (see [PARITY.md](PARITY.md), D17).
  That's also why there is no shopsanity.
- Free Play job tiles show 0% progress.
- The hub map pins still show; they just can't be selected.

## Credits and license

- [SWCreeperKing](https://github.com/SWCreeperKing/PowerwashSimAP): the PowerWash Simulator randomizer this is ported from (MIT).
- [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net) (MIT), shipped in `UserLibs`.
- [MelonLoader](https://github.com/LavaGang/MelonLoader) and Il2CppInterop.

MIT, see [LICENSE](LICENSE). This is a fan project, not affiliated with FuturLab.
