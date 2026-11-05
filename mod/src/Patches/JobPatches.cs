using HarmonyLib;
using Il2CppFuturLab.PW2;
using static PowerwashSim2AP.ApClient;
using static PowerwashSim2AP.Locations;
using IL2 = Il2CppSystem.Collections.Generic;

namespace PowerwashSim2AP.Patches;

public static class FreePlayUnlockPatch
{
    // PWS1's FreePlayUnlockPatch: free play is open for every job, the AP unlock items decide what shows
    [HarmonyPatch(typeof(Campaign), "IsFreePlayUnlocked"), HarmonyPrefix]
    public static bool IsFreePlayUnlocked(ref bool __result)
    {
        __result = true;
        return false;
    }
}

public static class JobListPatch
{
    // PWS1's JobLevelPatch: show only unlocked, not yet completed jobs, in Free Play only.
    // The menu reads its lists from m_unlockedJobsPerMode, built here; replacing PopulateScreen's argument did nothing.
    // A save that hasn't unlocked Free Play never offers it, so Free Play is switched on here too.
    [HarmonyPatch(typeof(JobSelectionState), "BuildActiveJobsList"), HarmonyPostfix]
    public static void BuildActiveJobsList(JobSelectionState __instance)
    {
        if (!IsConnected()) return;
        var campaign = __instance.m_currentCampaign;
        if (campaign is null) return;
        var mode = campaign.IsBonusJobsOnly ? GameMode.BonusJob : GameMode.FreePlay;

        var jobs = new IL2.List<CampaignJobPreview>();
        foreach (var job in campaign.m_loadedJobs.Values)
        {
            if (IsVisible(job.UniqueName)) jobs.Add(new CampaignJobPreview(job, 0, false));
        }

        var perMode = __instance.m_unlockedJobsPerMode;
        perMode[GameMode.Career] = new IL2.List<CampaignJobPreview>().Cast<IL2.IReadOnlyList<CampaignJobPreview>>();
        perMode[mode] = jobs.Cast<IL2.IReadOnlyList<CampaignJobPreview>>();
        __instance.m_currentGameMode = mode;
        __instance.m_isFreePlayAvailable = true;
        __instance.m_isCareerModeAvailable = false;
        Plugin.Log.Msg($"Job list for [{campaign.UniqueName}] in [{mode}]: {jobs.Count} unlocked");
    }

    public static bool IsVisible(string uniqueName)
        => Allowed.Contains(uniqueName) && JobNameByUnique.TryGetValue(uniqueName, out var name)
                                        && !CachedLevelsCompleted.Contains(name);
}

// Hub map pins start career jobs directly, around the AP job list, so they can't be selected while connected
public static class MapPinPatch
{
    [HarmonyPatch(typeof(JobSummaryInteractable), "CanInteract"), HarmonyPostfix]
    public static void CanInteract(ref bool __result)
    {
        if (IsConnected()) __result = false;
    }
}
