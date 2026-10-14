using System.Collections.Generic;
using System.IO;
using Aggro.Core;
using Newtonsoft.Json;

namespace CrashoutCrew_APClient;

public class Localizer
{
    public static void AddCustomLocalizations(string path)
    {
        var entries = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(path));
        if (entries == null) 
            return;
        foreach (var entry in entries)
            LocalizedText.mainTable.Add(entry.Key, entry.Value);
    }
}