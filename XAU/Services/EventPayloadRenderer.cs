using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XAU.Services;

/// <summary>Renders event requests without accessing authentication or the network.</summary>
public static class EventPayloadRenderer
{
    public static IReadOnlyList<string> Render(string template, JObject achievement, string xuid,
        DateTime timestamp, Random? random = null)
    {
        random ??= Random.Shared;
        if (!achievement.HasValues)
            throw new InvalidDataException("The achievement has no event operations.");

        var entries = achievement.Properties().ToList();
        bool wrapped = entries.Any(p => p.Name.StartsWith("Request", StringComparison.Ordinal));
        var requests = new List<JObject>();
        if (wrapped)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Name != "Request" + (i + 1).ToString(CultureInfo.InvariantCulture)
                    || entries[i].Value is not JObject request || !request.HasValues)
                    throw new InvalidDataException("Requests must be nonempty objects ordered Request1 through RequestN.");
                requests.Add(request);
            }
        }
        else requests.Add(achievement);

        var payloads = new List<string>();
        string sessionId = Guid.NewGuid().ToString();
        foreach (var request in requests)
        {
            // Every request starts from the original template, including debug validation.
            string body = template;
            foreach (var entry in request.Properties())
            {
                if (entry.Value is not JObject op)
                    throw new InvalidDataException($"Operation {entry.Name} must be an object.");
                string type = op.Value<string>("ReplacementType")
                    ?? throw new InvalidDataException($"Operation {entry.Name} has no ReplacementType.");
                string target = op.Value<string>("Target")
                    ?? throw new InvalidDataException($"Operation {entry.Name} has no Target.");
                if (target.Length == 0) throw new InvalidDataException("Replacement targets cannot be empty.");
                string replacement;
                switch (type)
                {
                    case "Replace":
                        if (op["Replacement"] is not JValue value || value.Type == JTokenType.Null)
                            throw new InvalidDataException($"Operation {entry.Name} has no scalar Replacement.");
                        replacement = Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? "";
                        break;
                    case "RangeInt":
                        replacement = random.Next(op.Value<int>("Min"), op.Value<int>("Max"))
                            .ToString(CultureInfo.InvariantCulture);
                        break;
                    case "RangeFloat":
                        float min = op.Value<float>("Min"), max = op.Value<float>("Max");
                        if (!float.IsFinite(min) || !float.IsFinite(max) || min > max)
                            throw new InvalidDataException("Invalid floating point range.");
                        replacement = ((float)random.NextDouble() * (max - min) + min)
                            .ToString(CultureInfo.InvariantCulture);
                        break;
                    case "StupidFuckingLDAPTimestamp":
                        replacement = timestamp.ToUniversalTime().ToFileTimeUtc().ToString(CultureInfo.InvariantCulture);
                        break;
                    default:
                        throw new InvalidDataException($"Unknown replacement type: {type}.");
                }
                body = body.Replace(target, replacement);
            }
            body = body.Replace("REPLACETIME", timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture))
                .Replace("REPLACESEQ", new DateTimeOffset(timestamp.ToUniversalTime()).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
                .Replace("REPLACEXUID", xuid)
                .Replace("REPLACESESSIONGUID", sessionId);
            var payload = JObject.Parse(body);
            if (payload.Value<string>("ver") != "4.0" || payload["name"]?.Type != JTokenType.String
                || payload["ext"] is not JObject || payload["data"] is not JObject)
                throw new InvalidDataException("The template did not render a OneCollector event envelope.");
            payloads.Add(payload.ToString(Formatting.None));
        }
        // Finish rendering the entire sequence before the caller sends its first request.
        return payloads;
    }
}
