using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HarmonyLib;
using Il2CppFuturLab.PW2;
using Il2CppFuturLab.PW2.TexturePainter;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.Localization;

namespace PowerwashSim2AP;

// Debug mode 4/5 (debug.txt): dumps job, shop and equipment data for apworld/gen_data.py, like PWS1's "Save Washables"
public static class DataDump
{
    public const string DumpDir = $"{Plugin.ModDir}/dump";

    // scene name -> WashTarget GameObject names seen when that scene's WashTargets started
    private static readonly Dictionary<string, SortedSet<string>> SceneWashTargets = new();
    private static readonly Dictionary<string, SortedSet<string>> SceneFurniture = new();
    private static string Status = "";
    private static float AutoDumpAt = -1;

    // dump once by itself, a few seconds after the game data finished initializing
    public static void OnUpdate()
    {
        if (AutoDumpAt == 0 || !GameDataManager.HasInstance || !GameDataManager.Instance.Initialized) return;
        if (AutoDumpAt < 0) AutoDumpAt = Time.realtimeSinceStartup + 10;
        if (Time.realtimeSinceStartup < AutoDumpAt) return;
        AutoDumpAt = 0;
        Status = Run(DumpGameData);
    }

    public static void OnGUI()
    {
        APGui.Box(new Rect(10, 10, 320, 140), "PowerwashSim2AP debug");
        if (APGui.Button(new Rect(20, 40, 300, 30), "Dump job/shop data")) Status = Run(DumpGameData);
        if (APGui.Button(new Rect(20, 75, 300, 30), $"Save washables ({SceneWashTargets.Count} scenes)"))
            Status = Run(DumpWashables);
        APGui.Label(new Rect(20, 110, 300, 40), Status, GUI.skin.label);
    }

    [HarmonyPatch(typeof(WashTarget), "Start"), HarmonyPostfix]
    public static void WashTargetStart(WashTarget __instance)
    {
        if (Plugin.IsDebug is not Plugin.DebugWant.Washables) return;
        var scene = __instance.gameObject.scene.name;
        var bucket = __instance.IsFurniture ? SceneFurniture : SceneWashTargets;
        if (!bucket.TryGetValue(scene, out var names)) bucket[scene] = names = new SortedSet<string>();
        names.Add(__instance.gameObject.name);
    }

    private static string Run(Func<string> dump)
    {
        try
        {
            var result = dump();
            Plugin.Log.Msg(result);
            return result;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
            return $"failed: {e.Message}";
        }
    }

    private static string DumpWashables()
    {
        Write("washables.json", new { wash_targets = SceneWashTargets, furniture = SceneFurniture });
        return $"wrote washables for {SceneWashTargets.Count} scenes";
    }

    // Stages write their own file, so a native crash in a later stage (e.g. localization) keeps the earlier data
    private static string DumpGameData()
    {
        Plugin.Log.Msg("dump: start");
        var manifest = GameDataManager.Instance.Manifest;
        var campaigns = new List<object>();
        var jobs = new List<object>();
        var named = new List<(string uniqueName, Func<LocalizedString> text)>();

        foreach (var campaign in Enumerate<Campaign>(manifest.Campaigns))
        {
            Plugin.Log.Msg($"dump: campaign {campaign.UniqueName}");
            var loaded = new List<GameJob>();
            foreach (var job in campaign.m_loadedJobs.Values) loaded.Add(job);
            Plugin.Log.Msg($"dump: campaign {campaign.UniqueName} has {loaded.Count} loaded jobs");
            campaigns.Add(new
            {
                unique_name = campaign.UniqueName,
                unique_id = campaign.UniqueId,
                is_dlc = campaign.IsDLCCampaign,
                bonus_jobs_only = campaign.IsBonusJobsOnly,
                tutorial_job_id = campaign.TutorialJobId,
                last_job_id = campaign.LastJobOfCampaignId,
                loaded_jobs = loaded.Count,
            });
            var c = campaign;
            named.Add((campaign.UniqueName, () => c.LocalizedName));

            foreach (var job in loaded)
            {
                Plugin.Log.Msg($"dump: job {job.UniqueName}");
                jobs.Add(DumpJob(campaign, job));
                var j = job;
                named.Add((job.UniqueName, () => j.LocalizedName));
                named.Add(($"{job.UniqueName}#free_play", () => j.GetModeSpecificTitle(GameMode.FreePlay, manifest)));
            }
        }

        Write("jobs_raw.json", new { campaigns, jobs });

        Plugin.Log.Msg("dump: shop");
        var shopContent = Resources.FindObjectsOfTypeAll<ShopContent>().ToList();
        Plugin.Log.Msg($"dump: {shopContent.Count} shop entries");
        var shop = shopContent.Select(DumpShopContent).ToList();
        foreach (var content in shopContent)
        {
            var sc = content;
            named.Add((content.UniqueName, () => sc.LocalizedName));
        }

        Plugin.Log.Msg("dump: equipment");
        var equipmentManifest = Resources.FindObjectsOfTypeAll<EquipmentManifest>().FirstOrDefault();
        var defaults = equipmentManifest is null
            ? new List<string>()
            : new List<string>
            {
                equipmentManifest.DefaultGun?.UniqueName, equipmentManifest.DefaultNozzle?.UniqueName,
                equipmentManifest.DefaultExtension?.UniqueName,
            };
        Write("shop_raw.json", new { shop, default_equipment = defaults });

        var names = new Dictionary<string, string>();
        foreach (var (uniqueName, text) in named)
        {
            Plugin.Log.Msg($"dump: localize {uniqueName}");
            names[uniqueName] = Localize(text());
        }

        Write("names.json", names);
        return $"wrote {campaigns.Count} campaigns, {jobs.Count} jobs, {shop.Count} shop entries, {names.Count} names";
    }

    private static object DumpJob(Campaign campaign, GameJob job)
    {
        var stages = job.m_stages?.ToArray() ?? Array.Empty<GameJobStage>();
        return new
        {
            unique_name = job.UniqueName,
            unique_id = job.UniqueId,
            campaign = campaign.UniqueName,
            is_dlc = campaign.IsDlc(job),
            bonus_tasks = job.m_bonusTasks?.Count ?? 0,
            stages = stages.Select(stage => new
            {
                unique_name = stage.m_uniqueName,
                tasks = (stage.m_tasks?.ToArray() ?? Array.Empty<GameJobTask>()).Select(task => new
                {
                    unique_name = task.m_TaskUniqueName,
                    washable_object = task.m_washableObject?.Name,
                    components = WashTasks(task).Select(washTask => new
                    {
                        name = washTask.m_washableComponent?.Name,
                        clean_name = washTask.m_washableComponent?.CleanName,
                        unique_id = washTask.m_washableComponent?.UniqueId ?? 0,
                    }).ToList(),
                }).ToList(),
            }).ToList(),
        };
    }

    private static List<GameJobWashTask> WashTasks(GameJobTask task)
    {
        var result = new List<GameJobWashTask>();
        if (task.m_washTaskList is null) return result;
        foreach (var washTask in task.m_washTaskList) result.Add(washTask);
        return result;
    }

    private static object DumpShopContent(ShopContent content)
    {
        var equipment = content.TryCast<BaseEquipmentData>();
        var cosmetic = content.TryCast<BaseCosmeticData>();
        var furniture = content.TryCast<WashableFurnitureData>();
        return new
        {
            unique_name = content.UniqueName,
            unique_id = content.UniqueId,
            type = content.GetIl2CppType().Name,
            category = equipment is not null ? "WasherEquipment"
                : cosmetic is not null ? "Cosmetics"
                : furniture is not null ? "FurnitureCatalog" : "Other",
            sub_type = equipment?.EquipmentType.ToString() ?? cosmetic?.CosmeticType.ToString(),
            unlocked_by_default = content.UnlockedByDefault,
            unlock_rules = content.UnlockRules?.Length ?? 0,
            price = content.Price.ToString(),
            muck_coin_price = content.PowerwashPointsPrice.ToString(),
            uses_muck_coins = content.UsingPowerwashPoints,
        };
    }

    private static string Localize(LocalizedString text)
    {
        try
        {
            return text?.GetLocalizedString();
        }
        catch (Exception e)
        {
            return $"<{e.GetType().Name}>";
        }
    }

    // Il2Cpp interface wrappers don't inherit each other, so cast whatever collection we got to IEnumerable<T>
    public static IEnumerable<T> Enumerate<T>(Il2CppObjectBase source) where T : Il2CppObjectBase
    {
        if (source is null) yield break;
        var enumerator = source.Cast<Il2CppSystem.Collections.Generic.IEnumerable<T>>().GetEnumerator();
        var untyped = enumerator.Cast<Il2CppSystem.Collections.IEnumerator>();
        while (untyped.MoveNext()) yield return enumerator.Current;
    }

    private static void Write(string file, object value)
    {
        Directory.CreateDirectory(DumpDir);
        File.WriteAllText($"{DumpDir}/{file}",
            JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }
}
