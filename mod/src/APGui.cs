using System;
using System.IO;
using System.Linq;
using UnityEngine;
using static PowerwashSim2AP.ApClient;
using static PowerwashSim2AP.Locations;

namespace PowerwashSim2AP;

// port of PWS1's APGui, drawn from MelonMod.OnGUI instead of an injected MonoBehaviour
public static class APGui
{
    public const string ConnectionFile = $"{Plugin.ModDir}/ApConnection.txt";
    public static bool ShowGUI = true;
    public static string Ipporttext = "archipelago.gg:12345";
    public static string Password = "";
    public static string Slot = "Powerwasher";
    public static string State = "";
    public static Vector2 Offset = new(15, 15);
    private static double TimeAccumulator;
    private static GUIStyle TextStyle, TextStyleGreen, TextStyleRed;

    public static void Load()
    {
        if (!File.Exists(ConnectionFile)) return;
        var fileText = File.ReadAllText(ConnectionFile).Replace("\r", "").Split('\n');
        if (fileText.Length < 3) return;
        Ipporttext = fileText[0];
        Password = fileText[1];
        Slot = fileText[2];
    }

    private static int FocusedField = -1;

    // This build strips GUIContent.Temp (used by every string overload of GUI.Label/Box/Button) and
    // TextEditor focus handling (used by GUI.TextField), so these wrap the GUIContent overloads and type by hand
    public static void Label(Rect rect, string text, GUIStyle style) => GUI.Label(rect, new GUIContent(text), style);
    public static void Box(Rect rect, string text) => GUI.Box(rect, new GUIContent(text));
    public static bool Button(Rect rect, string text) => GUI.Button(rect, new GUIContent(text));

    public static string TextField(int field, Rect rect, string text, int maxLength)
    {
        var focused = FocusedField == field;
        if (Button(rect, focused ? $"{text}|" : text)) FocusedField = focused ? -1 : field;
        if (!focused) return text;

        var e = Event.current;
        if (e.type != EventType.KeyDown) return text;
        if (e.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Escape or KeyCode.Tab) FocusedField = -1;
        else if (e.keyCode == KeyCode.Backspace && text.Length > 0) text = text[..^1];
        else if (e.keyCode == KeyCode.V && (e.control || e.command)) text += GUIUtility.systemCopyBuffer;
        else if (e.character != '\0' && !char.IsControl(e.character)) text += e.character;
        else return text;

        e.Use();
        return text.Length > maxLength ? text[..maxLength] : text;
    }

    private static GUIStyle Style(Color color)
    {
        var style = new GUIStyle { fontSize = 12 };
        style.normal.textColor = color;
        return style;
    }

    public static void OnGUI()
    {
        TextStyle ??= Style(Color.white);
        TextStyleGreen ??= Style(Color.green);
        TextStyleRed ??= Style(Color.red);
        TimeAccumulator += Time.unscaledDeltaTime;
        if (!ShowGUI) return;

        if (!IsConnected())
        {
            Box(new Rect(10 + Offset.x, 10 + Offset.y, 200, 300), "AP Client");

            Label(new Rect(20 + Offset.x, 40 + Offset.y, 300, 30), "Address:port", TextStyle);
            Ipporttext = TextField(0, new Rect(20 + Offset.x, 60 + Offset.y, 180, 25), Ipporttext, 25);

            Label(new Rect(20 + Offset.x, 90 + Offset.y, 300, 30), "Password", TextStyle);
            Password = TextField(1, new Rect(20 + Offset.x, 110 + Offset.y, 180, 25), Password, 25);

            Label(new Rect(20 + Offset.x, 140 + Offset.y, 300, 30), "Slot", TextStyle);
            Slot = TextField(2, new Rect(20 + Offset.x, 160 + Offset.y, 180, 25), Slot, 25);
        }
        else
        {
            Box(new Rect(10 + Offset.x, 10 + Offset.y + 100, 200, 150), "AP Client");
            if (Goal is GoalType.McGuffinHunt)
            {
                var hasGoal = Jobs >= WinCondition;
                Label(new Rect(20 + Offset.x, Offset.y + 155, 150, 35),
                    $"[A Job Well Done] mcguffins:\n    {Jobs} / {WinCondition}",
                    hasGoal ? TextStyleGreen : TextStyleRed);
            }
            else if (GoalLevelsOpen.Any())
            {
                var index = (int)(Math.Floor(TimeAccumulator / 20) % GoalLevelsOpen.Length);
                Label(new Rect(20 + Offset.x, Offset.y + 150, 150, 35),
                    $"""
                     Req. Completed Levels: ({CompletedLevelCount}/{LevelCount})
                     Levels of interest ({index + 1}/{GoalLevelsOpen.Length}):
                     """, TextStyle);
                var has = Allowed.Contains(JobUniqueByName[GoalLevelsOpen[index]]);
                Label(new Rect(20 + Offset.x, Offset.y + 180, 150, 35), $"[{GoalLevelsOpen[index]}]",
                    has ? TextStyleGreen : TextStyleRed);
            }
        }

        if (!IsConnected() && Button(new Rect(20 + Offset.x, 210 + Offset.y, 180, 30), "Connect"))
        {
            var ipPortSplit = Ipporttext.Split(':');
            if (ipPortSplit.Length < 2 || !int.TryParse(ipPortSplit[1], out var port))
            {
                State = $"[{Ipporttext}] is not address:port";
                return;
            }

            var error = TryConnect(port, Slot, ipPortSplit[0], Password);

            if (error is not null)
            {
                State = string.Join("\n", error);
                return;
            }

            State = "";
            File.WriteAllText(ConnectionFile, $"{Ipporttext}\n{Password}\n{Slot}");
            TimeAccumulator = 0;
        }

        if (IsConnected() && Button(new Rect(20 + Offset.x, 210 + Offset.y, 180, 30), "Disconnect"))
        {
            Disconnect();
        }

        Label(new Rect(20 + Offset.x, 240 + Offset.y, 300, 30),
            State != "" ? State : IsConnected() ? "Connected" : "Not Connected",
            IsConnected() ? TextStyleGreen : TextStyleRed);
    }
}
