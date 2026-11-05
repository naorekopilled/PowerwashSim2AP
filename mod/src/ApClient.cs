#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static PowerwashSim2AP.Locations;

namespace PowerwashSim2AP;

// port of PWS1's ApDirtClient onto Archipelago.MultiClient.Net
public static class ApClient
{
    public const string Game = "PowerWash Simulator 2";
    // MuckCoins per received $WashCoin (D9); shortcut: flat amount, tune once free-play earnings are known
    public const int WashCoinMuckCoins = 50;

    private static readonly List<string> ChecksToSend = [];
    public static ConcurrentQueue<string> ChecksToSendQueue = [];
    public static ArchipelagoSession? Session;
    public static long WinCondition;
    public static int Jobs;
    public static bool Objectsanity;
    public static bool Percentsanity;
    public static bool Stagesanity;
    public static bool Bonustasksanity;
    // job unique names the player may play
    public static List<string> Allowed = [];
    public static string[] GoalLevelsOpen = [];
    public static GoalType Goal = 0;
    public static long LevelCount = -1;
    public static string[] Levels = [];
    public static long CompletedLevelCount;
    public static HashSet<string> CachedLevelsCompleted = [];
    private static HashSet<string> MissingLocations = [];
    private static int ItemsProcessed;
    private static int WashCoinsReceived;
    private static float NextSend = 4;
    private static readonly Dictionary<string, Regex> RegexCache = [];

    public enum GoalType
    {
        McGuffinHunt = 0,
        LevelHunt = 1
    }

    public static string[]? TryConnect(int port, string slot, string address, string password)
    {
        try
        {
            Plugin.Log.Msg($"Attempting to connect [{address}]:[{port}] [{slot}]");
            Session = ArchipelagoSessionFactory.CreateSession(address, port);
            var result = Session.TryConnectAndLogin(Game, slot, ItemsHandlingFlags.AllItems,
                password: string.IsNullOrEmpty(password) ? null : password, requestSlotData: true);

            if (result is LoginFailure failure)
            {
                Plugin.Log.Error($"Connection Error: {string.Join("\n", failure.Errors)}");
                Disconnect();
                return failure.Errors;
            }

            HasConnected(((LoginSuccessful)result).SlotData);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
            Disconnect();
            return [e.Message, e.StackTrace!];
        }

        return null;
    }

    public static void Disconnect()
    {
        try
        {
            Session?.Socket.DisconnectAsync();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }

        Session = null;
        Plugin.Log.Msg("Disconnected");
    }

    private static void HasConnected(Dictionary<string, object> slotdata)
    {
        var startingLocation = (string)slotdata["starting_location"];
        WinCondition = (long)slotdata["jobs_done"];
        Percentsanity = Flag(slotdata, "percentsanity");
        Objectsanity = Flag(slotdata, "objectsanity");
        Stagesanity = Flag(slotdata, "stagesanity");
        Bonustasksanity = Flag(slotdata, "bonustasksanity");

        if (slotdata.TryGetValue("goal_levels", out var temp3))
        {
            Levels = ((string)temp3).Split(',').Select(s => s.Trim('\'', '[', ']', ' ', '"')).ToArray();
        }

        if (slotdata.TryGetValue("goal_level_amount", out var temp4)) LevelCount = (long)temp4;
        if (LevelCount == 0) LevelCount = Levels.Length;

        Goal = Levels.Any() && Levels[0] != "None" ? GoalType.LevelHunt : GoalType.McGuffinHunt;

        Allowed = [JobUniqueByName[startingLocation]];
        Plugin.Log.Msg($"Starting: [{startingLocation}]");
        Jobs = 0;
        ItemsProcessed = 0;
        MissingLocations = Session!.Locations.AllMissingLocations
                                  .Select(id => Session.Locations.GetLocationNameFromId(id, Game))
                                  .Where(name => name is not null).ToHashSet();

        Plugin.Log.Msg("Receiving Items");
        WashCoinsReceived = Session.DataStorage[Scope.Slot, "washcoins_applied"].To<int?>() ?? 0;
        ReceiveItems();
        Plugin.Log.Msg("Connected, running failsafe checks");

        CachedLevelsCompleted = (Session.DataStorage[Scope.Slot, "levels_completed"].To<string[]?>() ?? [])
                               .Where(s => s is not null && JobUniqueByName.ContainsKey(s)).ToHashSet();
        Plugin.Log.Msg($"Levels Completed: [{CachedLevelsCompleted.Count}]:[{string.Join(", ", CachedLevelsCompleted)}]");
        GoalLevelCheck(CachedLevelsCompleted.ToArray());

        foreach (var unlock in CachedLevelsCompleted)
        {
            FailsafeSendLocations(unlock);
        }

        Plugin.Log.Msg("Failsafe checking complete");
    }

    private static bool Flag(Dictionary<string, object> slotdata, string key)
        => slotdata.TryGetValue(key, out var value) && (bool)value;

    public static bool IsConnected() => Session is not null && Session.Socket.Connected;

    public static void Update()
    {
        if (Session is null) return;
        if (!Session.Socket.Connected) return;

        NextSend -= Time.unscaledDeltaTime;
        if (ChecksToSend.Any() && NextSend <= 0)
        {
            SendChecks();
        }

        ReceiveItems();

        while (ChecksToSendQueue.TryDequeue(out var location))
        {
            ChecksToSend.Add(location);
        }

        if (Goal is not GoalType.McGuffinHunt || Jobs < WinCondition) return;
        Session.SetGoalAchieved();
    }

    private static void ReceiveItems()
    {
        var items = Session!.Items.AllItemsReceived;
        if (ItemsProcessed >= items.Count) return;
        var newItems = items.Skip(ItemsProcessed).Select(item => item.ItemName).ToArray();
        ItemsProcessed = items.Count;

        Jobs += newItems.Count(item => item == "A Job Well Done");
        var washCoins = items.Count(item => item.ItemName == "$WashCoin");
        if (washCoins > WashCoinsReceived) GrantWashCoins(washCoins);

        var newAllowed = newItems.Where(item => item.EndsWith(" Unlock"))
                                 .Select(item => JobUniqueByName[item[..^" Unlock".Length]])
                                 .ToArray();

        if (!newAllowed.Any()) return;
        Allowed.AddRange(newAllowed);
        Plugin.Log.Msg($"New allowed: [{string.Join(", ", newAllowed.Select(s => JobNameByUnique[s]))}]");
        UpdateAvailableLevelGoal();
    }

    // items are resent on every connect, so the amount already paid out lives in data storage
    private static void GrantWashCoins(int total)
    {
        if (!Il2CppFuturLab.PW2.SaveManager.HasInstance) return;
        var save = Il2CppFuturLab.PW2.SaveManager.Instance.PlayerSaveData;
        if (save is null) return;
        save.AddSecondaryCurrency(new Il2CppFuturLab.PW2.MuckCoinValue((total - WashCoinsReceived) * WashCoinMuckCoins));
        WashCoinsReceived = total;
        Session!.DataStorage[Scope.Slot, "washcoins_applied"] = total;
    }

    public static bool IsMissing(string locationName) => Session is not null && MissingLocations.Contains(locationName);

    public static void Check(string locationName)
    {
        if (!IsMissing(locationName) || ChecksToSendQueue.Contains(locationName)) return;
        ChecksToSendQueue.Enqueue(locationName);
    }

    private static void SendChecks()
    {
        NextSend = 3;
        Plugin.Log.Msg($"Send checks: [{string.Join(", ", ChecksToSend)}]");
        var ids = ChecksToSend.Select(name => Session!.Locations.GetLocationIdFromName(Game, name)).ToArray();
        Session?.Locations.CompleteLocationChecksAsync(ids);
        MissingLocations.ExceptWith(ChecksToSend);
        ChecksToSend.Clear();
    }

    public static void SetLevelCompletion(string level)
    {
        try
        {
            if (Session is null) return;
            Plugin.Log.Msg("Set Level Completion");
            CachedLevelsCompleted.Add(level);
            Session.DataStorage[Scope.Slot, "levels_completed"] = JArray.FromObject(CachedLevelsCompleted.ToArray());
            if (Goal is GoalType.McGuffinHunt) return;
            GoalLevelCheck(CachedLevelsCompleted.ToArray());
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }
    }

    public static void GoalLevelCheck(string[] levelsCompleted)
    {
        if (Session is null) return;
        Plugin.Log.Msg($"Goal Level Check: [{Goal}]");
        if (Goal is GoalType.McGuffinHunt) return;
        UpdateAvailableLevelGoal();
        if (LevelCount > levelsCompleted.Count(s => Levels.Contains(s))) return;
        Session.SetGoalAchieved();
    }

    public static void UpdateAvailableLevelGoal()
    {
        if (Session is null) return;
        if (Goal is GoalType.McGuffinHunt) return;
        var separatedLevels = Levels.GroupBy(str => CachedLevelsCompleted.Contains(str)).ToArray();
        CompletedLevelCount = separatedLevels.FirstOrDefault(g => g.Key)?.Count() ?? 0;
        GoalLevelsOpen = separatedLevels.FirstOrDefault(g => !g.Key)?.ToArray() ?? [];
        Plugin.Log.Msg($"Complete: [{CompletedLevelCount}] Open: [{GoalLevelsOpen.Length}]: [{string.Join(", ", GoalLevelsOpen)}]");
    }

    public static void FailsafeSendLocations(string locationName)
    {
        Plugin.Log.Msg($"Level Finished Detected: [{locationName}] running failsafe");
        var reg = LevelRegex(locationName);

        var missing = 0;
        foreach (var loc in MissingLocations.Where(name => reg.IsMatch(name)))
        {
            if (ChecksToSendQueue.Contains(loc)) continue;
            ChecksToSendQueue.Enqueue(loc);
            missing++;
        }

        Plugin.Log.Msg($"Failsafe Finished, Sent [{missing}] failed checks");
    }

    public static Regex LevelRegex(string locationName)
    {
        if (RegexCache.TryGetValue(locationName, out var regex)) return regex;
        return RegexCache[locationName] =
            new Regex($@"^{Regex.Escape(locationName)}( \d{{1,3}}%$|: | Stage \d+$| Bonus Task \d+$)", RegexOptions.Compiled);
    }
}
