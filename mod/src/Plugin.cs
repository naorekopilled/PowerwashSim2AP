using System.IO;
using MelonLoader;
using PowerwashSim2AP;
using PowerwashSim2AP.Patches;
using UnityEngine;

[assembly: MelonInfo(typeof(Plugin), "PowerwashSim2AP", Plugin.VersionNumber, "naorekopilled")]
[assembly: MelonGame("FuturLab", "PowerWash Simulator 2")]

namespace PowerwashSim2AP;

public class Plugin : MelonMod
{
    public const string VersionNumber = "0.1.0";
    public const string ModDir = "Mods/PowerwashSim2AP";

    public enum DebugWant
    {
        None = 0,
        General = 1,
        Stats = 2,
        Buttons = 3,
        Jobs = 4,
        Washables = 5,
    }

    public static DebugWant IsDebug = DebugWant.None;
    public static MelonLogger.Instance Log;
    private static float NextSweep;

    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        if (File.Exists("debug.txt") && int.TryParse(File.ReadAllText("debug.txt").Trim(), out var isDebug))
        {
            IsDebug = (DebugWant)isDebug;
        }

        Log.Msg($"Debug Setting: [{IsDebug}]");
        Directory.CreateDirectory(ModDir);
        Locations.Load();
        APGui.Load();
        // like PWS1's Harmony.CreateAndPatchAll(typeof(X)): PatchAll(assembly) skips classes that only have
        // method-level [HarmonyPatch] attributes, which silently left every patch here unapplied
        foreach (var patches in new[]
                 {
                     typeof(FreePlayUnlockPatch), typeof(JobListPatch), typeof(MapPinPatch),
                     typeof(LevelProgressionPatch), typeof(DataDump),
                 })
        {
            HarmonyInstance.CreateClassProcessor(patches, true).Patch();
        }

        if (IsDebug is DebugWant.General) DebugTrace.Patch(HarmonyInstance);
        Log.Msg($"Patched {System.Linq.Enumerable.Count(HarmonyInstance.GetPatchedMethods())} methods");
        Log.Msg($"PowerwashSim2AP {VersionNumber} is loaded!");
    }

    public override void OnUpdate()
    {
        if (IsDebug is DebugWant.Jobs or DebugWant.Washables) DataDump.OnUpdate();
        ApClient.Update();
        if (Time.realtimeSinceStartup < NextSweep) return;
        NextSweep = Time.realtimeSinceStartup + 1;
        LevelProgressionPatch.Sweep();
    }

    public override void OnGUI()
    {
        if (IsDebug is DebugWant.Jobs or DebugWant.Washables)
        {
            DataDump.OnGUI();
            return;
        }

        APGui.OnGUI();
    }
}
