using System.Reflection;
using HarmonyLib;
using Il2CppFuturLab.PW2;
using Il2CppFuturLab.PW2.UI;

namespace PowerwashSim2AP;

// Debug mode 1 (debug.txt): logs which job-menu and free-play methods the game really calls, to find hook points
// that aren't inlined by IL2CPP
public static class DebugTrace
{
    private static readonly (System.Type type, string method)[] Targets =
    [
        (typeof(JobSelectionState), "Enter"),
        (typeof(JobSelectionState), "BuildActiveJobsList"),
        (typeof(JobSelectionState), "GetActiveJobs"),
        (typeof(JobSelectionState), "PopulateScreen"),
        (typeof(JobSelectionState), "SwitchGameMode"),
        (typeof(JobSelectionState), "get_CanSwitchGameModes"),
        (typeof(JobSelectionState), "OnJobRequested"),
        (typeof(JobSelectionState), "OnPageNavigationRequested"),
        (typeof(JobSelectionTilesWidget), "OnScreenContentChanged"),
        (typeof(JobPackSelectionState), "PopulateScreen"),
        (typeof(JobPackSelectionState), "OnPackSelected"),
        (typeof(JobPackSelectionState), "OnPackRequested"),
        (typeof(Campaign), "IsFreePlayUnlocked"),
        (typeof(Campaign), "GetFreePlayStatus"),
        (typeof(Campaign), "GetUnlockedFreePlayJobs"),
        (typeof(Campaign), "GetLevelStatus"),
        (typeof(Campaign), "GetCareerJobStatus"),
        (typeof(CampaignJobPreview), "GetInteractionStatus"),
        (typeof(PlayingState), "Enter"),
    ];

    public static void Patch(HarmonyLib.Harmony harmony)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var withResult = new HarmonyMethod(typeof(DebugTrace).GetMethod(nameof(Log), flags));
        // __result can't be asked for on void methods
        var withoutResult = new HarmonyMethod(typeof(DebugTrace).GetMethod(nameof(LogVoid), flags));
        foreach (var (type, name) in Targets)
        {
            foreach (var method in AccessTools.GetDeclaredMethods(type))
            {
                if (method.Name != name) continue;
                try
                {
                    harmony.Patch(method, postfix: method.ReturnType == typeof(void) ? withoutResult : withResult);
                    Plugin.Log.Msg($"trace: patched {type.Name}.{name}({method.GetParameters().Length} args)");
                }
                catch (System.Exception e)
                {
                    Plugin.Log.Error($"trace: can't patch {type.Name}.{name}: {e.Message}");
                }
            }
        }
    }

    private static void Log(MethodBase __originalMethod, object[] __args, object __result)
        => Plugin.Log.Msg($"trace: {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}" +
                          $"({string.Join(", ", __args ?? [])}) -> {__result}");

    private static void LogVoid(MethodBase __originalMethod, object[] __args)
        => Plugin.Log.Msg($"trace: {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}" +
                          $"({string.Join(", ", __args ?? [])})");
}
