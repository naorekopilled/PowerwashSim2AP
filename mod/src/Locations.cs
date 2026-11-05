using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PowerwashSim2AP;

// AP name <-> game unique name, generated into Resources/ap_data.json by apworld/gen_data.py
public static class Locations
{
    public static Dictionary<string, string> JobUniqueByName = new();
    public static Dictionary<string, string> JobNameByUnique = new();

    private class ApData
    {
        public Dictionary<string, string> jobs { get; set; }
    }

    public static void Load()
    {
        using var stream = typeof(Locations).Assembly.GetManifestResourceStream("ap_data.json")!;
        var data = JsonSerializer.Deserialize<ApData>(new StreamReader(stream).ReadToEnd())!;
        JobUniqueByName = data.jobs;
        JobNameByUnique = data.jobs.ToDictionary(kv => kv.Value, kv => kv.Key);
    }
}
