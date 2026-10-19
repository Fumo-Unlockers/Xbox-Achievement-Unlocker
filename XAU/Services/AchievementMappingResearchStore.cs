using System.IO;
using Newtonsoft.Json;

namespace XAU.Services;

public static class AchievementMappingResearchStore
{
    public const string QuantumBreakTitleId = "333628240";

    private static readonly object Sync = new();
    private static readonly string HistoryDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XAU",
        "Debug",
        "achievement_mapping_research");
    private static readonly string LegacyQuantumBreakHistoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XAU",
        "Debug",
        "quantum_break_mapping_history.json");

    public static string FilePath(string titleId) =>
        Path.Combine(HistoryDirectory, $"{SanitizeTitleId(titleId)}.json");

    public static IReadOnlyList<AchievementMappingResearchEntry> Load(string titleId)
    {
        lock (Sync)
        {
            try
            {
                var path = FilePath(titleId);
                var isLegacyQuantumBreakHistory = !File.Exists(path)
                    && titleId == QuantumBreakTitleId
                    && File.Exists(LegacyQuantumBreakHistoryPath);
                if (isLegacyQuantumBreakHistory)
                    path = LegacyQuantumBreakHistoryPath;
                if (!File.Exists(path))
                    return [];

                var entries = JsonConvert.DeserializeObject<List<AchievementMappingResearchEntry>>(
                    File.ReadAllText(path)) ?? [];
                foreach (var entry in entries)
                {
                    entry.TitleId = string.IsNullOrWhiteSpace(entry.TitleId) ? titleId : entry.TitleId;
                    entry.BeforeRequirements ??= [];
                    entry.AfterRequirements ??= [];
                    entry.ChangedAchievements ??= [];
                    AchievementResearchClassifier.Apply(entry);
                }
                var titleEntries = entries.Where(entry => entry.TitleId == titleId).ToList();
                if (isLegacyQuantumBreakHistory)
                {
                    try
                    {
                        var titlePath = FilePath(titleId);
                        Directory.CreateDirectory(Path.GetDirectoryName(titlePath)!);
                        File.WriteAllText(titlePath, JsonConvert.SerializeObject(titleEntries, Formatting.Indented));
                    }
                    catch
                    {
                        // A failed migration must not hide otherwise readable legacy history.
                    }
                }
                return titleEntries;
            }
            catch
            {
                return [];
            }
        }
    }

    public static void Record(AchievementMappingResearchEntry entry)
    {
        lock (Sync)
        {
            try
            {
                entry.TitleId = string.IsNullOrWhiteSpace(entry.TitleId) ? QuantumBreakTitleId : entry.TitleId;
                AchievementResearchClassifier.Apply(entry);
                var entries = Load(entry.TitleId).ToList();
                entries.Add(entry);
                var path = FilePath(entry.TitleId);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonConvert.SerializeObject(entries, Formatting.Indented));
            }
            catch
            {
                // Research logging must never affect an explicit test event.
            }
        }
    }

    private static string SanitizeTitleId(string titleId) =>
        new(titleId.Where(char.IsLetterOrDigit).ToArray());
}

public sealed class AchievementMappingResearchEntry
{
    public string TitleId { get; set; } = "";
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public int ProgressionData { get; set; }
    public string AchievementId { get; set; } = "";
    public string AchievementName { get; set; } = "";
    public string Result { get; set; } = "Unknown state change";
    public string CandidateClassification { get; set; } = AchievementResearchClassifier.Unknown;
    public string AchievementClassification { get; set; } = AchievementResearchClassifier.Unknown;
    public string? BeforeState { get; set; }
    public string? AfterState { get; set; }
    public string? BeforeUnlockTime { get; set; }
    public string? AfterUnlockTime { get; set; }
    public Dictionary<string, string?> BeforeRequirements { get; set; } = new();
    public Dictionary<string, string?> AfterRequirements { get; set; } = new();
    public List<ResearchChangedAchievement> ChangedAchievements { get; set; } = new();
    public string ObservedChanges { get; set; } = "";

    [JsonIgnore]
    public string RecordedAt => RecordedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    [JsonIgnore]
    public string BeforeRequirementsSummary => FormatRequirements(BeforeRequirements);

    [JsonIgnore]
    public string AfterRequirementsSummary => FormatRequirements(AfterRequirements);

    [JsonIgnore]
    public string ChangedAchievementsSummary => ChangedAchievements.Count == 0
        ? "<none>"
        : string.Join("; ", ChangedAchievements.Select(change =>
            $"{change.AchievementId} - {change.AchievementName}" +
            (change.IsSelectedAchievement ? " (selected)" : " (different)") +
            $" [{change.Classification}]"));

    [JsonIgnore]
    public string FullDiffSummary => ChangedAchievements.Count == 0
        ? ObservedChanges
        : string.Join(Environment.NewLine + Environment.NewLine, ChangedAchievements.Select(change => change.Details));

    [JsonIgnore]
    public string Details =>
        $"Title ID: {TitleId}{Environment.NewLine}" +
        $"Recorded UTC: {RecordedAtUtc:O}{Environment.NewLine}" +
        $"ProgressionData: {ProgressionData}{Environment.NewLine}" +
        $"Achievement: {AchievementId} - {AchievementName}{Environment.NewLine}" +
        $"Result: {Result}{Environment.NewLine}" +
        $"Candidate classification: {CandidateClassification}{Environment.NewLine}" +
        $"Selected achievement classification: {AchievementClassification}{Environment.NewLine}" +
        $"State: {BeforeState ?? "<none>"} -> {AfterState ?? "<none>"}{Environment.NewLine}" +
        $"timeUnlocked: {BeforeUnlockTime ?? "<none>"} -> {AfterUnlockTime ?? "<none>"}{Environment.NewLine}" +
        $"Before requirements: {BeforeRequirementsSummary}{Environment.NewLine}" +
        $"After requirements: {AfterRequirementsSummary}{Environment.NewLine}" +
        $"Changed achievements: {ChangedAchievementsSummary}{Environment.NewLine}" +
        $"Full before/after diff:{Environment.NewLine}{FullDiffSummary}{Environment.NewLine}" +
        $"Observed changes:{Environment.NewLine}{ObservedChanges}";

    public bool Matches(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        return new[]
        {
            TitleId,
            ProgressionData.ToString(),
            AchievementId,
            AchievementName,
            Result,
            CandidateClassification,
            AchievementClassification,
            BeforeState,
            AfterState,
            BeforeUnlockTime,
            AfterUnlockTime,
            BeforeRequirementsSummary,
            AfterRequirementsSummary,
            ChangedAchievementsSummary,
            FullDiffSummary,
            ObservedChanges
        }.Any(value => value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string FormatRequirements(Dictionary<string, string?> requirements) =>
        requirements.Count == 0
            ? "<none>"
            : string.Join(", ", requirements.Select(requirement => $"{requirement.Key}={requirement.Value ?? "<none>"}"));
}

public sealed class ResearchChangedAchievement
{
    public string AchievementId { get; set; } = "";
    public string AchievementName { get; set; } = "";
    public bool IsSelectedAchievement { get; set; }
    public bool AchievementUnlocked { get; set; }
    public bool StateChanged { get; set; }
    public bool UnlockTimeChanged { get; set; }
    public bool RequirementChanged { get; set; }
    public string Classification { get; set; } = AchievementResearchClassifier.Unknown;
    public string? BeforeState { get; set; }
    public string? AfterState { get; set; }
    public string? BeforeUnlockTime { get; set; }
    public string? AfterUnlockTime { get; set; }
    public Dictionary<string, string?> BeforeRequirements { get; set; } = new();
    public Dictionary<string, string?> AfterRequirements { get; set; } = new();
    public string RequirementDiff { get; set; } = "";

    [JsonIgnore]
    public string BeforeRequirementsSummary => FormatRequirements(BeforeRequirements);

    [JsonIgnore]
    public string AfterRequirementsSummary => FormatRequirements(AfterRequirements);

    [JsonIgnore]
    public string Details =>
        $"{AchievementId} - {AchievementName}{(IsSelectedAchievement ? " (selected)" : " (different achievement)")}{Environment.NewLine}" +
        $"Classification: {Classification}{Environment.NewLine}" +
        $"State: {BeforeState ?? "<none>"} -> {AfterState ?? "<none>"}{Environment.NewLine}" +
        $"timeUnlocked: {BeforeUnlockTime ?? "<none>"} -> {AfterUnlockTime ?? "<none>"}{Environment.NewLine}" +
        $"Before requirements: {BeforeRequirementsSummary}{Environment.NewLine}" +
        $"After requirements: {AfterRequirementsSummary}{Environment.NewLine}" +
        $"Requirement changes: {(string.IsNullOrWhiteSpace(RequirementDiff) ? "<none>" : RequirementDiff)}";

    private static string FormatRequirements(Dictionary<string, string?> requirements) =>
        requirements.Count == 0
            ? "<none>"
            : string.Join(", ", requirements.Select(requirement => $"{requirement.Key}={requirement.Value ?? "<none>"}"));
}

public static class AchievementResearchClassifier
{
    public const string DirectUnlockMapping = "Direct Unlock Mapping";
    public const string DirectProgressionMapping = "Direct Progression Mapping";
    public const string CounterBasedAchievement = "Counter-Based Achievement";
    public const string FractionalAchievement = "Fractional/Percentage Achievement";
    public const string Unknown = "Unknown";

    private static readonly IReadOnlyDictionary<string, string> QuantumBreakKnownAchievements =
        new Dictionary<string, string>
        {
            ["10"] = DirectUnlockMapping,
            ["11"] = DirectUnlockMapping,
            ["19"] = CounterBasedAchievement,
            ["28"] = FractionalAchievement,
            ["29"] = FractionalAchievement,
            ["47"] = FractionalAchievement,
            ["48"] = FractionalAchievement,
            ["49"] = DirectUnlockMapping,
            ["52"] = DirectUnlockMapping
        };

    private static readonly HashSet<int> QuantumBreakDirectUnlockCandidates = [1, 2, 22, 25];

    public static void Apply(AchievementMappingResearchEntry entry)
    {
        foreach (var change in entry.ChangedAchievements)
            change.Classification = ClassifyAchievement(entry.TitleId, change.AchievementId, change);

        entry.CandidateClassification = ClassifyCandidate(entry);
        entry.AchievementClassification = ClassifyAchievement(
            entry.TitleId,
            entry.AchievementId,
            entry.ChangedAchievements.FirstOrDefault(change => change.AchievementId == entry.AchievementId));
    }

    public static string ClassifyCandidate(AchievementMappingResearchEntry entry)
    {
        if (entry.TitleId == AchievementMappingResearchStore.QuantumBreakTitleId
            && QuantumBreakDirectUnlockCandidates.Contains(entry.ProgressionData)
            || entry.ChangedAchievements.Any(change => change.AchievementUnlocked))
            return DirectUnlockMapping;

        return entry.ChangedAchievements.Any(change => change.RequirementChanged)
            ? DirectProgressionMapping
            : Unknown;
    }

    public static string ClassifyAchievement(
        string titleId,
        string achievementId,
        IEnumerable<AchievementMappingResearchEntry>? history = null)
    {
        if (TryGetKnownAchievement(titleId, achievementId, out var known))
            return known;

        var changes = history?.SelectMany(entry => entry.ChangedAchievements)
            .Where(change => change.AchievementId == achievementId)
            .ToList() ?? [];
        if (changes.Any(change => change.AchievementUnlocked))
            return DirectUnlockMapping;
        if (changes.Any(change => change.RequirementChanged))
            return DirectProgressionMapping;
        return Unknown;
    }

    public static string ClassifyAchievement(string titleId, string achievementId, ResearchChangedAchievement? change)
    {
        if (TryGetKnownAchievement(titleId, achievementId, out var known))
            return known;
        if (change?.AchievementUnlocked == true)
            return DirectUnlockMapping;
        if (change?.RequirementChanged == true)
            return DirectProgressionMapping;
        return Unknown;
    }

    private static bool TryGetKnownAchievement(string titleId, string achievementId, out string classification)
    {
        if (titleId == AchievementMappingResearchStore.QuantumBreakTitleId
            && QuantumBreakKnownAchievements.TryGetValue(achievementId, out classification!))
            return true;

        classification = Unknown;
        return false;
    }
}
