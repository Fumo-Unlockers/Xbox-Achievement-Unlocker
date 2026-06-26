using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XAU.Services.AutoUnlock;

namespace XAU.Services.StatEditor
{
    public class HeroStat
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Value { get; set; } = "0";
        public string StatType { get; set; } = "Integer";
        public string Scid { get; set; } = "";
    }

    public sealed class StatEditorService
    {
        private static readonly HttpClient _http = CreateClient();
        private readonly string _xauth;
        private readonly string _xuid;

        public StatEditorService(string xauth, string xuid)
        {
            _xauth = xauth;
            _xuid = xuid;
        }

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        }

        private async Task<JObject> XboxFetchAsync(
            HttpMethod method, string url, string contractVersion, string? jsonBody, CancellationToken token)
        {
            using var req = new HttpRequestMessage(method, url);
            req.Headers.TryAddWithoutValidation("Authorization", _xauth);
            req.Headers.TryAddWithoutValidation("x-xbl-contract-version", contractVersion);
            req.Headers.TryAddWithoutValidation("Accept", "application/json");
            req.Headers.TryAddWithoutValidation("Accept-Language", "en-US");
            if (jsonBody != null)
                req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, token);
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                var snippet = text.Length > 300 ? text.Substring(0, 300) : text;
                throw new Exception($"Xbox API {(int)resp.StatusCode}: {snippet}");
            }
            if (string.IsNullOrWhiteSpace(text))
                return new JObject();
            return JObject.Parse(text);
        }

        public async Task<string?> DiscoverScidAsync(string titleId, CancellationToken token)
        {
            var url = $"https://titlehub.xboxlive.com/users/xuid({_xuid})/titles/titleHistory/decoration/Achievement,scid?maxItems=10000";
            await XboxRateLimiter.StatsRead.WaitAsync(token);
            var data = await XboxFetchAsync(HttpMethod.Get, url, "2", null, token);
            var titles = data["titles"] as JArray;
            if (titles == null)
                return null;
            foreach (var title in titles)
            {
                if (Child(title, "titleId")?.ToString() == titleId)
                {
                    return Child(Child(title, "detail"), "scid")?.ToString()
                        ?? Child(title, "serviceConfigId")?.ToString();
                }
            }
            return null;
        }

        public async Task<List<HeroStat>> ReadStatsAsync(string titleId, CancellationToken token)
        {
            var scid = await DiscoverScidAsync(titleId, token) ?? titleId;

            var heroBody = JsonConvert.SerializeObject(new
            {
                arrangebyfield = "xuid",
                xuids = new[] { _xuid },
                groups = new[] { new { name = "Hero", titleId = titleId } }
            });

            JObject data;
            await XboxRateLimiter.StatsRead.WaitAsync(token);
            try
            {
                data = await XboxFetchAsync(HttpMethod.Post, "https://userstats.xboxlive.com/batch", "2", heroBody, token);
            }
            catch
            {
                var altBody = JsonConvert.SerializeObject(new
                {
                    requestedusers = new[] { _xuid },
                    requestedscids = new[] { new { scid = scid, requestedstats = Array.Empty<string>() } }
                });
                await XboxRateLimiter.StatsRead.WaitAsync(token);
                data = await XboxFetchAsync(HttpMethod.Post, "https://userstats.xboxlive.com/batch", "1", altBody, token);
            }

            return ParseBatchStats(data, scid);
        }

        public async Task<(bool ok, string message)> WriteStatAsync(
            string titleId, string scid, string statName, string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(scid))
                return (false, "Cannot write a stat without a SCID.");

            var url = $"https://statswrite.xboxlive.com/stats/users/{_xuid}/scids/{scid}";
            Exception? lastError = null;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture);
                var body = JsonConvert.SerializeObject(new Dictionary<string, object>
                {
                    ["$schema"] = "http://stats.xboxlive.com/2017-1/schema#",
                    ["previousRevision"] = 0,
                    ["revision"] = unixTime,
                    ["timestamp"] = timestamp,
                    ["stats"] = new Dictionary<string, object>
                    {
                        ["title"] = new Dictionary<string, object>
                        {
                            [statName] = new Dictionary<string, object> { ["value"] = value }
                        }
                    }
                });

                try
                {
                    await XboxRateLimiter.StatsWrite.WaitAsync(token);
                    await XboxFetchAsync(new HttpMethod("PATCH"), url, "4", body, token);
                    lastError = null;
                    break;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (attempt < 2)
                        await Task.Delay(1000, token);
                }
            }

            if (lastError != null)
                return (false, $"Stat write failed: {lastError.Message}");

            var expected = value.Trim();
            await Task.Delay(2000, token);
            for (int attempt = 0; attempt < 5; attempt++)
            {
                var actual = await ReadExactStatAsync(scid, statName, token);
                if (actual != null && actual.Trim() == expected)
                    return (true, $"{statName} = {expected} confirmed.");
                if (attempt < 4)
                    await Task.Delay(1000, token);
            }

            return (false, $"Wrote {statName} but could not confirm the new value yet (Xbox may still be processing it).");
        }

        private async Task<string?> ReadExactStatAsync(string scid, string statName, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(scid) || string.IsNullOrWhiteSpace(statName))
                return null;
            var url = $"https://userstats.xboxlive.com/users/xuid({_xuid})/scids/{scid}/stats/{Uri.EscapeDataString(statName)}?include=valuemetadata";
            try
            {
                await XboxRateLimiter.StatsRead.WaitAsync(token);
                var data = await XboxFetchAsync(HttpMethod.Get, url, "3", null, token);
                var stats = data["stats"] as JArray;
                if (stats != null)
                {
                    foreach (var s in stats)
                    {
                        var name = ValueAsString(s, new[] { "name", "statname", "statName" }, "");
                        if (name == statName)
                            return ValueAsString(s, new[] { "value" }, "0");
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        // Só JObject aceita índice por nome; indexar JValue/JArray lança exceção.
        private static JToken? Child(JToken? item, string key) => (item as JObject)?[key];

        private static string ValueAsString(JToken? item, string[] keys, string fallback)
        {
            if (item is not JObject obj) return fallback;
            foreach (var key in keys)
            {
                var v = obj[key];
                if (v == null) continue;
                if (v.Type == JTokenType.String) return v.ToString();
                if (v.Type == JTokenType.Integer || v.Type == JTokenType.Float) return v.ToString();
            }
            return fallback;
        }

        private static HeroStat ParseStatItem(JToken stat, string collectionScid)
        {
            var name = ValueAsString(stat, new[] { "name", "statname", "statName" }, "");
            var props = stat["groupproperties"] as JObject;
            var displayName = props?["DisplayName"]?.ToString()
                ?? props?["displayName"]?.ToString()
                ?? ValueAsString(stat, new[] { "displayName", "displayname", "name", "statname", "statName" }, name);
            return new HeroStat
            {
                Name = name,
                DisplayName = string.IsNullOrEmpty(displayName) ? name : displayName,
                Value = ValueAsString(stat, new[] { "value" }, "0"),
                StatType = ValueAsString(stat, new[] { "type" }, "Integer"),
                Scid = ValueAsString(stat, new[] { "scid" }, collectionScid)
            };
        }

        private static void ParseStatCollection(JToken collection, string fallbackScid, List<HeroStat> stats)
        {
            var collectionScid = ValueAsString(collection, new[] { "scid" }, fallbackScid);
            var statList = (Child(collection, "stats") ?? Child(collection, "statlist")) as JArray;
            if (statList != null)
                foreach (var stat in statList)
                    stats.Add(ParseStatItem(stat, collectionScid));
        }

        private static List<HeroStat> ParseBatchStats(JObject data, string fallbackScid)
        {
            var stats = new List<HeroStat>();

            if (data["groups"] is JArray groups)
            {
                foreach (var group in groups)
                {
                    var collections = (Child(group, "statlistscollection") ?? Child(group, "statlistcollection")) as JArray;
                    if (collections != null)
                        foreach (var c in collections)
                            ParseStatCollection(c, fallbackScid, stats);
                }
            }

            if (stats.Count == 0 && data["statlistscollection"] is JArray topCollections)
                foreach (var c in topCollections)
                    ParseStatCollection(c, fallbackScid, stats);

            if (stats.Count == 0 && data["users"] is JArray users)
                foreach (var user in users)
                    if (Child(user, "scids") is JArray scids)
                        foreach (var s in scids)
                            ParseStatCollection(s, fallbackScid, stats);

            return stats
                .GroupBy(s => s.Name)
                .Select(g => g.First())
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .ToList();
        }
    }
}
