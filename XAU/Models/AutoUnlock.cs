using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XAU.AutoUnlock
{
    public enum UnlockCategory
    {
        None,
        Easy,
        Hard
    }

    public static class CategoryDetector
    {
        public const string LabelEasy = "Easy Unlock";
        public const string LabelHard = "Hard Unlock";

        public static bool HasRequests(JToken? achievementData)
        {
            if (achievementData is not JObject obj)
                return false;

            foreach (var prop in obj.Properties())
            {
                if (prop.Name.StartsWith("Request", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static bool HasDirectReplacements(JToken? achievementData)
        {
            if (achievementData is not JObject obj)
                return false;

            return obj["EventReplacement"] != null
                || obj["DataReplacement"] != null
                || obj["MetaDataReplacement"] != null;
        }

        public static UnlockCategory Detect(JToken? achievementData)
        {
            if (achievementData is not JObject)
                return UnlockCategory.None;

            if (HasRequests(achievementData) || HasDirectReplacements(achievementData))
                return UnlockCategory.Hard;

            return UnlockCategory.Easy;
        }

        public static string Label(UnlockCategory category)
        {
            return category switch
            {
                UnlockCategory.Easy => LabelEasy,
                UnlockCategory.Hard => LabelHard,
                _ => string.Empty
            };
        }
    }

    public class UnlockOrderItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public long DelaySeconds { get; set; }
        public int Gamerscore { get; set; }
    }

    public class UnlockOrder
    {
        public string TitleId { get; set; } = "";
        public string GameName { get; set; } = "";
        public string ReferenceGamertag { get; set; } = "";
        public System.DateTime GeneratedAt { get; set; }

        public System.Collections.Generic.List<UnlockOrderItem> Items { get; set; }
            = new System.Collections.Generic.List<UnlockOrderItem>();

        [JsonIgnore]
        public long TotalDurationSeconds
        {
            get
            {
                long total = 0;
                foreach (var item in Items)
                    total += item.DelaySeconds;
                return total;
            }
        }
    }

    public class AutoUnlockState
    {
        public string TitleId { get; set; } = "";
        public int Index { get; set; }
        public string AchievementId { get; set; } = "";
        public string AchievementName { get; set; } = "";
        public System.DateTime TargetTimeUtc { get; set; }
    }

    public class AutoProgress
    {
        public int Index { get; set; }
        public int Total { get; set; }
        public string GameName { get; set; } = "";
        public string AchievementName { get; set; } = "";
        public long SecondsRemaining { get; set; }
        public string Message { get; set; } = "";
        public bool Completed { get; set; }
    }
}
