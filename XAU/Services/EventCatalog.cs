using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace XAU.Services;

/// <summary>Loads the catalog and optional large per-title achievement maps.</summary>
public static class EventCatalog
{
    public static JObject Load(string directory)
    {
        var data = JObject.Parse(File.ReadAllText(Path.Combine(directory, "Data.json")));
        foreach (var title in data.Properties().ToList())
        {
            if (!title.Name.All(char.IsDigit)) continue;
            string path = Path.Combine(directory, "Catalog", title.Name + ".json");
            if (!File.Exists(path)) continue;
            if (title.Value is not JObject block || block["Achievements"] is not JObject achievements || achievements.HasValues)
                throw new InvalidDataException($"Title {title.Name} has conflicting inline and external achievement mappings.");
            block["Achievements"] = JObject.Parse(File.ReadAllText(path));
        }
        return data;
    }
}
