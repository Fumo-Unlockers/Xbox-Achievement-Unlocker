using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XAU.AutoUnlock;

namespace XAU.Services.AutoUnlock
{
    public static class OrderRepository
    {
        private const long MaxGapSeconds = 5 * 60 * 60;
        private const long RandomMinSeconds = 20 * 60;
        private const long RandomMaxSeconds = 90 * 60;

        private static readonly Random _random = new Random();

        private static string BaseFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "UnlockOrders");

        private static string StateFolder => Path.Combine(BaseFolder, "State");
        private static string SkipFolder => Path.Combine(BaseFolder, "Skip");

        private static string OrderPath(string titleId) => Path.Combine(BaseFolder, $"{titleId}.json");
        private static string StatePath(string titleId) => Path.Combine(StateFolder, $"{titleId}.json");
        private static string SkipPath(string titleId) => Path.Combine(SkipFolder, $"{titleId}.json");

        public static bool Exists(string titleId) => File.Exists(OrderPath(titleId));

        public static UnlockOrder? Load(string titleId)
        {
            var path = OrderPath(titleId);
            if (!File.Exists(path))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<UnlockOrder>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        public static void Save(UnlockOrder order)
        {
            Directory.CreateDirectory(BaseFolder);
            var json = JsonConvert.SerializeObject(order, Formatting.Indented);
            WriteAtomic(OrderPath(order.TitleId), json);
        }

        public static void Delete(string titleId)
        {
            var path = OrderPath(titleId);
            if (File.Exists(path))
                File.Delete(path);
        }

        public static AutoUnlockState? LoadState(string titleId)
        {
            var path = StatePath(titleId);
            if (!File.Exists(path))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<AutoUnlockState>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        public static void SaveState(AutoUnlockState state)
        {
            Directory.CreateDirectory(StateFolder);
            var json = JsonConvert.SerializeObject(state, Formatting.Indented);
            WriteAtomic(StatePath(state.TitleId), json);
        }

        private static void WriteAtomic(string path, string content)
        {
            var temp = path + ".tmp";
            File.WriteAllText(temp, content);
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }

        public static void ClearState(string titleId)
        {
            var path = StatePath(titleId);
            if (File.Exists(path))
                File.Delete(path);
        }

        public static HashSet<string> LoadSkip(string titleId)
        {
            var path = SkipPath(titleId);
            if (!File.Exists(path))
                return new HashSet<string>();
            try
            {
                var list = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path));
                return list == null ? new HashSet<string>() : new HashSet<string>(list);
            }
            catch
            {
                return new HashSet<string>();
            }
        }

        public static void SaveSkip(string titleId, HashSet<string> ids)
        {
            Directory.CreateDirectory(SkipFolder);
            var json = JsonConvert.SerializeObject(ids.ToList(), Formatting.Indented);
            WriteAtomic(SkipPath(titleId), json);
        }

        public static async Task<UnlockOrder> GenerateFromReferenceAsync(
            XboxRestAPI api,
            string titleId,
            string gameName,
            string gamertag,
            HashSet<string> userOwnedIds)
        {
            JObject? profile;
            try
            {
                profile = await api.GetGamertagProfileAsync(gamertag);
            }
            catch
            {
                throw new Exception($"We couldn't find the gamertag \"{gamertag}\". Check the spelling and try again.");
            }

            var referenceXuid = profile?["profileUsers"]?.FirstOrDefault()?["id"]?.ToString();
            if (string.IsNullOrWhiteSpace(referenceXuid))
                throw new Exception($"Could not resolve the gamertag \"{gamertag}\".");

            var response = await api.GetAchievementsForTitleAsync(referenceXuid, titleId);
            if (response?.achievements == null || response.achievements.Count == 0)
                throw new Exception($"\"{gamertag}\" has no visible achievements in this game (profile may be private).");

            var skip = LoadSkip(titleId);
            var earned = new List<(string id, string name, DateTime when, int gs)>();
            foreach (var ach in response.achievements)
            {
                if (ach.progressState != StringConstants.Achieved)
                    continue;
                if (userOwnedIds.Contains(ach.id))
                    continue;
                if (skip.Contains(ach.id))
                    continue;

                var when = ParseDate(ach.progression?.timeUnlocked);
                if (when == null)
                    continue;

                earned.Add((ach.id, ach.name, when.Value, ExtractGamerscore(ach)));
            }

            if (earned.Count == 0)
                throw new Exception("Nothing left to unlock -- you already have everything this reference has.");

            earned.Sort((a, b) => a.when.CompareTo(b.when));

            var items = new List<UnlockOrderItem>();
            DateTime? previous = null;
            foreach (var c in earned)
            {
                long delay;
                if (previous == null)
                {
                    delay = 0;
                }
                else
                {
                    var gap = (long)Math.Round((c.when - previous.Value).TotalSeconds);
                    if (gap > MaxGapSeconds)
                        delay = RandomMinSeconds + (long)(_random.NextDouble() * (RandomMaxSeconds - RandomMinSeconds));
                    else
                        delay = Math.Max(1, gap);
                }

                items.Add(new UnlockOrderItem
                {
                    Id = c.id,
                    Name = c.name,
                    DelaySeconds = delay,
                    Gamerscore = c.gs
                });

                previous = c.when;
            }

            return new UnlockOrder
            {
                TitleId = titleId,
                GameName = gameName,
                ReferenceGamertag = gamertag,
                GeneratedAt = DateTime.UtcNow,
                Items = items
            };
        }

        private static DateTime? ParseDate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.StartsWith("0001-01-01"))
                return null;
            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            {
                var utc = dto.UtcDateTime;
                if (utc.Year < 2005)
                    return null;
                return utc;
            }
            return null;
        }

        private static int ExtractGamerscore(OneCoreAchievementResponse ach)
        {
            var reward = ach.rewards?.FirstOrDefault(r => r.type == StringConstants.Gamerscore);
            if (reward != null && int.TryParse(reward.value, out var value))
                return value;
            return 0;
        }
    }
}
