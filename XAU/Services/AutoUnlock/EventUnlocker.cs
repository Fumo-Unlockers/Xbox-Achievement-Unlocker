using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XAU.AutoUnlock;

namespace XAU.Services.AutoUnlock
{
    public class EventUnlocker
    {
        private const int BatchSize = 500;
        private const int MaxEvents = 10000;
        private const int MaxRetries = 5;

        private readonly XboxRestAPI _api;
        private readonly string _titleId;
        private readonly string _xuid;
        private readonly string _eventsToken;

        private static readonly Random _random = new Random();

        public EventUnlocker(XboxRestAPI api, string titleId, string xuid, string eventsToken)
        {
            _api = api;
            _titleId = titleId;
            _xuid = xuid;
            _eventsToken = eventsToken;
        }

        private static string EventsFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "Events");

        private string ReadTemplate()
        {
            var path = Path.Combine(EventsFolder, $"{_titleId}.json");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Event template not found for title {_titleId}.");
            return File.ReadAllText(path);
        }

        public async Task UnlockAsync(string achievementId, JObject gameData, int loopMultiplier = 1)
        {
            var template = ReadTemplate();
            loopMultiplier = Math.Max(1, loopMultiplier);

            var node = ResolveNode(gameData, achievementId, template);

            if (node == null || (!CategoryDetector.HasRequests(node) && CollectReplacements(node).Count == 0))
                throw new InvalidOperationException(
                    $"Achievement {achievementId} is not in this game's Data.json and could not be inferred.");

            if (CategoryDetector.HasRequests(node))
            {
                await SendWithRequestsAsync(template, node, loopMultiplier);
            }
            else
            {
                var count = GetLoop(node) * loopMultiplier;
                var templateBody = BuildTemplateBody(template, CollectReplacements(node));
                await SendManyAsync(templateBody, count);
            }
        }

        private JObject? ResolveNode(JObject gameData, string achievementId, string template)
        {
            var node = gameData?["Achievements"]?[achievementId] as JObject;
            bool hasData = node != null && (CategoryDetector.HasRequests(node) || CollectReplacements(node).Count > 0);
            if (!hasData)
            {
                node = node ?? InferFromSiblings(gameData, achievementId, template);
                node = AugmentWithDynamic(template, achievementId, node);
            }
            return node;
        }

        public async Task SendEventsAsync(string achievementId, JObject gameData, int count)
        {
            var template = ReadTemplate();
            var node = ResolveNode(gameData, achievementId, template);
            if (node == null || (!CategoryDetector.HasRequests(node) && CollectReplacements(node).Count == 0))
                throw new InvalidOperationException($"Achievement {achievementId} has no event data to send.");

            count = Math.Max(1, count);
            if (CategoryDetector.HasRequests(node))
            {
                for (int i = 0; i < count; i++)
                    await SendWithRequestsAsync(template, node, 1);
            }
            else
            {
                var body = BuildTemplateBody(template, CollectReplacements(node));
                await SendManyAsync(body, count);
            }
        }

        private async Task SendWithRequestsAsync(string template, JObject achievementData, int loopMultiplier)
        {
            var generalReplacements = new List<JObject>();
            var requests = new List<(int order, JObject node)>();

            foreach (var prop in achievementData.Properties())
            {
                if (prop.Name.StartsWith("Request", StringComparison.OrdinalIgnoreCase) && prop.Value is JObject reqObj)
                {
                    var suffix = prop.Name.Substring("Request".Length);
                    int.TryParse(suffix, out var n);
                    requests.Add((n, reqObj));
                }
                else if (prop.Value is JObject obj && obj["ReplacementType"] != null)
                {
                    generalReplacements.Add(obj);
                }
            }

            foreach (var (_, reqObj) in requests.OrderBy(r => r.order))
            {
                var all = new List<JObject>();
                all.AddRange(CollectReplacements(reqObj));
                all.AddRange(generalReplacements);

                var count = GetLoop(reqObj) * loopMultiplier;
                var templateBody = BuildTemplateBody(template, all);
                await SendManyAsync(templateBody, count);
            }
        }

        private static int GetLoop(JObject node)
        {
            var token = node["Loop"] ?? node["loop"];
            if (token == null)
                return 1;
            if (token.Type == JTokenType.Integer)
                return Math.Max(1, token.Value<int>());
            if (int.TryParse(token.ToString(), out var n))
                return Math.Max(1, n);
            return 1;
        }

        private static List<JObject> CollectReplacements(JObject node)
        {
            var list = new List<JObject>();
            foreach (var prop in node.Properties())
            {
                if (prop.Value is JObject obj && obj["ReplacementType"] != null)
                    list.Add(obj);
            }
            return list;
        }

        private string BuildTemplateBody(string template, List<JObject> replacements)
        {
            var body = template;
            foreach (var repl in replacements)
                body = ApplyOne(body, repl);
            body = body.Replace("REPLACEXUID", _xuid);
            return body;
        }

        private static string FinalizeEvent(string templateBody, long seq)
        {
            var now = DateTime.UtcNow;
            var body = templateBody
                .Replace("REPLACETIME", now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"))
                .Replace("REPLACESEQ", seq.ToString());
            return JObject.Parse(body).ToString(Formatting.None);
        }

        private async Task SendManyAsync(string templateBody, int count)
        {
            count = Math.Clamp(count, 1, MaxEvents);
            var baseSeq = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            await SendBatchAsync(new List<string> { FinalizeEvent(templateBody, baseSeq) });
            if (count == 1)
                return;

            var batch = new List<string>(BatchSize);
            for (int i = 1; i < count; i++)
            {
                batch.Add(FinalizeEvent(templateBody, baseSeq + i));
                if (batch.Count >= BatchSize)
                {
                    await SendBatchAsync(batch);
                    batch.Clear();
                }
            }
            if (batch.Count > 0)
                await SendBatchAsync(batch);
        }

        private async Task SendBatchAsync(List<string> events)
        {
            var ndjson = string.Join("\n", events);

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                int status = await _api.SendEventBatchAsync(_eventsToken, ndjson);

                if (status >= 200 && status < 300)
                    return;

                if (status == 401 || status == 403)
                    throw new Exception("Event token expired or invalid. Please refresh your event token.");

                // 429 (rate limit) é transitório: espera e tenta de novo.
                bool transient = status == 0 || status == 429 || status >= 500;
                if (transient && attempt < MaxRetries - 1)
                {
                    await Task.Delay(1000 * (int)Math.Pow(2, attempt));
                    continue;
                }

                throw new Exception($"OneCollector returned HTTP {status}.");
            }
        }


        private static readonly Regex PlaceholderRegex = new Regex("REPLACE[A-Z0-9]+", RegexOptions.Compiled);

        private static (List<string> ev, List<string> meta, List<string> data, List<string> name, List<string> id)
            DetectPlaceholders(string template)
        {
            var found = PlaceholderRegex.Matches(template)
                .Select(m => m.Value)
                .Distinct()
                .Where(p => p != "REPLACETIME" && p != "REPLACESEQ" && p != "REPLACEXUID")
                .ToList();
            var ev = found.Where(p => p.Contains("EVENT") || p == "REPLACETITLE").ToList();
            var meta = found.Where(p => p.Contains("METADATA")).ToList();
            var data = found.Where(p => p.Contains("DATA") && !p.Contains("METADATA")).ToList();
            var name = found.Where(p => p == "REPLACENAME").ToList();
            var id = found.Where(p =>
                p == "REPLACEID" || p == "REPLACEINDEX" || p.Contains("CRITERIA")
                || p == "REPLACECCRITERADDEPC" || p == "REPLACESTATVALUE").ToList();
            return (ev, meta, data, name, id);
        }

        private JObject AugmentWithDynamic(string template, string achievementId, JObject? node)
        {
            var result = node != null ? (JObject)node.DeepClone() : new JObject();

            var covered = new HashSet<string>();
            foreach (var r in CollectReplacements(result))
            {
                var t = r["Target"]?.ToString();
                if (!string.IsNullOrEmpty(t))
                    covered.Add(t);
            }

            var internalId = ExtractInternalId(result, achievementId);
            var (ev, meta, data, name, id) = DetectPlaceholders(template);

            void Add(string placeholder, string replacement)
            {
                if (covered.Contains(placeholder))
                    return;
                covered.Add(placeholder);
                result[$"{placeholder}Replacement"] = new JObject
                {
                    ["ReplacementType"] = "Replace",
                    ["Target"] = placeholder,
                    ["Replacement"] = replacement
                };
            }

            foreach (var p in ev)
                Add(p, $"Microsoft.XboxLive.T{_titleId}.UnlockAchievement");
            foreach (var p in data)
                Add(p, $"{{\"baseType\":\"Microsoft.XboxLive.InGame\",\"baseData\":{{\"name\":\"UnlockAchievement\",\"serviceConfigId\":\"b4900100-fd0c-476f-b32e-b74a4ae8f9b2\",\"playerSessionId\":\"11111111-1111-1111-1111-111111111111\",\"titleId\":\"{_titleId}\",\"userId\":\"REPLACEXUID\",\"ver\":1,\"properties\":{{\"UnlockId\":{internalId}}},\"measurements\":{{\"ProgressPercent\":100}}}}}}");
            foreach (var p in meta)
                Add(p, $"{{\"properties\":{{\"f\":{{\"AchievementID\":{internalId}}}}},\"measurements\":{{\"f\":{{\"ProgressPercent\":100}}}}}}");
            foreach (var p in name)
                Add(p, achievementId);
            foreach (var p in id)
                Add(p, internalId);

            return result;
        }

        private static string ExtractInternalId(JObject? node, string xboxId)
        {
            if (node != null)
            {
                var repl = node["DataReplacement"]?["Replacement"];
                string? replStr = null;
                if (repl != null && repl.Type == JTokenType.String) replStr = repl.ToString();
                else if (repl != null && repl.Type == JTokenType.Integer) replStr = repl.ToString();

                if (replStr != null)
                {
                    if (replStr.Length > 0 && replStr.All(char.IsDigit))
                        return replStr;
                    var m = Regex.Match(replStr, "\"AchievementID\"\\s*:\\s*(\\d+)");
                    if (m.Success)
                        return m.Groups[1].Value;
                }

                if (node["Replacement"] is JObject ro && ro["Replacement"] is JToken rv)
                {
                    if (rv.Type == JTokenType.Integer) return rv.ToString();
                    if (rv.Type == JTokenType.String && rv.ToString().All(char.IsDigit)) return rv.ToString();
                }
            }
            return xboxId;
        }

        private JObject? InferFromSiblings(JObject? gameData, string achievementId, string template)
        {
            var achievements = gameData?["Achievements"] as JObject;
            if (achievements == null)
                return null;

            var (ev, meta, data, _, _) = DetectPlaceholders(template);

            foreach (var prop in achievements.Properties())
            {
                if (prop.Name == achievementId || prop.Value is not JObject other)
                    continue;

                bool hasNeeded =
                    (ev.Count > 0 && other["EventReplacement"] != null)
                    || (meta.Count > 0 && other["MetaDataReplacement"] != null)
                    || (data.Count > 0 && other["DataReplacement"] != null)
                    || CategoryDetector.HasRequests(other)
                    || CollectReplacements(other).Count > 0;
                if (!hasNeeded)
                    continue;

                var clone = (JObject)other.DeepClone();
                RewriteIds(clone, prop.Name, achievementId);
                return clone;
            }
            return null;
        }

        private static void RewriteIds(JObject node, string fromId, string toId)
        {
            foreach (var prop in node.Properties().ToList())
            {
                if (prop.Value is JObject obj && obj["ReplacementType"] != null)
                {
                    var repl = obj["Replacement"];
                    if (repl != null && repl.Type == JTokenType.String)
                    {
                        var s = repl.ToString().Replace(fromId, toId);
                        s = Regex.Replace(s, "\"UnlockId\"\\s*:\\s*\\d+", $"\"UnlockId\":{toId}");
                        s = Regex.Replace(s, "\"AchievementID\"\\s*:\\s*\\d+", $"\"AchievementID\":{toId}");
                        obj["Replacement"] = s;
                    }
                    else if (repl != null && repl.Type == JTokenType.Integer)
                    {
                        if (long.TryParse(toId, out var n)) obj["Replacement"] = n;
                        else obj["Replacement"] = toId;
                    }
                }
                else if (prop.Name.StartsWith("Request", StringComparison.OrdinalIgnoreCase) && prop.Value is JObject reqObj)
                {
                    RewriteIds(reqObj, fromId, toId);
                }
            }
        }

        private string ApplyOne(string body, JObject repl)
        {
            var type = repl["ReplacementType"]?.ToString() ?? "Replace";
            var target = repl["Target"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(target))
                return body;

            switch (type)
            {
                case "Replace":
                {
                    var value = ValueAsText(repl["Replacement"]);
                    value = value.Replace("REPLACEXUID", _xuid);
                    return body.Replace(target, value);
                }
                case "RangeInt":
                {
                    var min = (int)ReadNumber(repl["Min"], 0);
                    var max = (int)ReadNumber(repl["Max"], 100);
                    if (max < min) (min, max) = (max, min);
                    var picked = _random.Next(min, max + 1);
                    return body.Replace(target, picked.ToString());
                }
                case "RangeFloat":
                {
                    var min = ReadNumber(repl["Min"], 0);
                    var max = ReadNumber(repl["Max"], 1);
                    if (max < min) (min, max) = (max, min);
                    var picked = min + _random.NextDouble() * (max - min);
                    return body.Replace(target, picked.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                case "StupidFuckingLDAPTimestamp":
                {
                    var ldap = DateTime.Now.ToFileTime();
                    return body.Replace(target, ldap.ToString());
                }
                default:
                    return body;
            }
        }

        private static string ValueAsText(JToken? token)
        {
            if (token == null) return "";
            return token.Type == JTokenType.String ? token.ToString() : token.ToString(Formatting.None).Trim('"');
        }

        private static double ReadNumber(JToken? token, double fallback)
        {
            if (token == null) return fallback;
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                return token.Value<double>();
            return double.TryParse(token.ToString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : fallback;
        }
    }
}
