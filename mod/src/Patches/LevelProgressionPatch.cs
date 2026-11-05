using System.Collections.Generic;
using HarmonyLib;
using Il2CppFuturLab.PW2;
using static PowerwashSim2AP.ApClient;
using static PowerwashSim2AP.Locations;

namespace PowerwashSim2AP.Patches;

// PWS1's LevelProgressionPatch + WashTargetPatch: percent checks from the game's % event,
// objects/stages/bonus tasks from a once-a-second sweep of the level (PWS1 also polled, per WashTarget)
public static class LevelProgressionPatch
{
    public static PlayingState Current;
    public static string LocationName;
    public static int LastPercentChecked = 1;
    private static readonly HashSet<int> SentTargets = [];

    [HarmonyPatch(typeof(PlayingState), "Enter"), HarmonyPostfix]
    public static void Enter(PlayingState __instance)
    {
        Current = __instance;
        LastPercentChecked = 1;
        SentTargets.Clear();
        var job = __instance.Job;
        LocationName = job is not null && JobNameByUnique.TryGetValue(job.UniqueName, out var name) ? name : null;
        Plugin.Log.Msg($"Entered [{job?.UniqueName}] as [{LocationName}] mode [{__instance.Mode}]");
    }

    [HarmonyPatch(typeof(PlayingState), "Exit"), HarmonyPostfix]
    public static void Exit()
    {
        Current = null;
        LocationName = null;
    }

    [HarmonyPatch(typeof(PlayingState), "OnWashProgressPercentageChanged"), HarmonyPostfix]
    public static void ProgressChanged(int __1)
    {
        if (Session is null || LocationName is null) return;

        while (LastPercentChecked <= __1)
        {
            var percentName = $"{LocationName} {LastPercentChecked}%";

            if (LastPercentChecked == 100)
            {
                Plugin.Log.Msg($"Completed Level: [{LocationName}]");
                SetLevelCompletion(LocationName);
                FailsafeSendLocations(LocationName);
            }

            LastPercentChecked++;
            if (Percentsanity) Check(percentName);
        }
    }

    public static void Sweep()
    {
        if (Current is null || LocationName is null || !IsConnected()) return;
        var job = Current.Job;
        var save = Current.GetLevelSaveData();

        if (Objectsanity && Current.WashManager is not null)
        {
            foreach (var target in Current.WashManager.m_washTargets)
            {
                if (target.IsFurniture || target.CleanProgress < 1 || SentTargets.Contains(target.UniqueId)) continue;
                SentTargets.Add(target.UniqueId);
                if (!job.TryGetWashTask(target, out _, out var washTask) || washTask?.m_washableComponent is null) continue;
                Check($"{LocationName}: {washTask.m_washableComponent.Name.Replace('_', ' ')}");
            }
        }

        if (save is null) return;

        if (Stagesanity && job.m_stages is not null)
        {
            for (var i = 0; i < job.m_stages.Length; i++)
            {
                if (job.m_stages[i].IsCompleted(save)) Check($"{LocationName} Stage {i + 1}");
            }
        }

        if (Bonustasksanity && job.m_bonusTasks is not null)
        {
            for (var i = 0; i < job.m_bonusTasks.Count; i++)
            {
                if (job.IsBonusTaskCompleted(job.m_bonusTasks[i].UniqueId, save)) Check($"{LocationName} Bonus Task {i + 1}");
            }
        }
    }
}
