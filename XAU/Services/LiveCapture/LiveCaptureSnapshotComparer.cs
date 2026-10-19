using System.Globalization;

namespace XAU.Services.LiveCapture;

public static class LiveCaptureSnapshotComparer
{
    public static LiveCaptureSnapshot Create(AchievementsResponse response)
    {
        var achievements = new Dictionary<string, LiveCaptureAchievementSnapshot>(StringComparer.Ordinal);
        foreach (var achievement in (response.achievements ?? []).Where(item =>
            item != null && !string.IsNullOrWhiteSpace(item.id)))
        {
            var requirements = new Dictionary<string, LiveCaptureRequirementSnapshot>(StringComparer.Ordinal);
            foreach (var item in (achievement.progression?.requirements ?? [])
                         .Select((value, index) => (value, index))
                         .Where(item => item.value != null))
            {
                var id = string.IsNullOrWhiteSpace(item.value.id) ? $"#{item.index}" : item.value.id!;
                requirements[id] = new LiveCaptureRequirementSnapshot
                {
                    Id = id,
                    Current = item.value.current,
                    Target = item.value.target
                };
            }

            achievements[achievement.id] = new LiveCaptureAchievementSnapshot
            {
                Id = achievement.id,
                Name = achievement.name ?? "<unnamed>",
                State = achievement.progressState,
                XboxReportedUnlockTime = achievement.progression?.timeUnlocked,
                Requirements = requirements
            };
        }

        return new LiveCaptureSnapshot { Achievements = achievements };
    }

    public static IReadOnlyList<LiveCaptureObservation> Compare(
        LiveCaptureSnapshot before,
        LiveCaptureSnapshot after,
        DateTimeOffset observedAtUtc)
    {
        var observations = new List<LiveCaptureObservation>();
        foreach (var achievementId in before.Achievements.Keys
                     .Union(after.Achievements.Keys, StringComparer.Ordinal)
                     .OrderBy(value => value, StringComparer.Ordinal))
        {
            before.Achievements.TryGetValue(achievementId, out var previous);
            after.Achievements.TryGetValue(achievementId, out var current);
            if (current == null)
                continue;

            var stateChanged = previous == null
                || !string.Equals(previous.State, current.State, StringComparison.OrdinalIgnoreCase);
            var unlockTimeChanged = previous != null
                && !string.Equals(
                    previous.XboxReportedUnlockTime,
                    current.XboxReportedUnlockTime,
                    StringComparison.Ordinal);
            var progressionChanges = previous == null
                ? []
                : CompareRequirements(previous.Requirements, current.Requirements);

            if (!stateChanged && !unlockTimeChanged && progressionChanges.Count == 0)
                continue;

            var achieved = string.Equals(current.State, "Achieved", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(previous?.State, "Achieved", StringComparison.OrdinalIgnoreCase);
            var evidence = stateChanged || unlockTimeChanged
                ? progressionChanges.Count > 0
                    ? $"{LiveCaptureEvidence.XboxStateChange}; {LiveCaptureEvidence.ProgressionChange}"
                    : LiveCaptureEvidence.XboxStateChange
                : LiveCaptureEvidence.ProgressionChange;
            var message = achieved
                ? $"Achievement unlocked: {current.Id} - {current.Name}."
                : stateChanged
                    ? $"Achievement state changed: {current.Id} - {current.Name}."
                    : unlockTimeChanged
                        ? $"Xbox-reported unlock time changed: {current.Id} - {current.Name}."
                        : $"Achievement progression changed: {current.Id} - {current.Name}.";

            observations.Add(new LiveCaptureObservation
            {
                ObservedAtUtc = observedAtUtc,
                Source = XboxAchievementCaptureProvider.ProviderName,
                Category = achieved ? "Achievement unlocked" : progressionChanges.Count > 0 ? "Progression" : "State",
                EvidenceClassification = evidence,
                Message = message,
                AchievementId = current.Id,
                AchievementName = current.Name,
                PreviousState = previous?.State,
                NewState = current.State,
                XboxReportedUnlockTime = current.XboxReportedUnlockTime,
                ProgressionChanges = progressionChanges
            });
        }

        return observations;
    }

    private static List<LiveCaptureProgressChange> CompareRequirements(
        IReadOnlyDictionary<string, LiveCaptureRequirementSnapshot> before,
        IReadOnlyDictionary<string, LiveCaptureRequirementSnapshot> after)
    {
        var changes = new List<LiveCaptureProgressChange>();
        foreach (var requirementId in before.Keys
                     .Union(after.Keys, StringComparer.Ordinal)
                     .OrderBy(value => value, StringComparer.Ordinal))
        {
            before.TryGetValue(requirementId, out var previous);
            after.TryGetValue(requirementId, out var current);
            if (previous != null && current != null
                && string.Equals(previous.Current, current.Current, StringComparison.Ordinal))
            {
                continue;
            }

            changes.Add(new LiveCaptureProgressChange
            {
                RequirementId = requirementId,
                PreviousValue = previous?.Current,
                NewValue = current?.Current,
                TargetValue = current?.Target ?? previous?.Target,
                Delta = CalculateDelta(previous?.Current, current?.Current)
            });
        }

        return changes;
    }

    private static decimal? CalculateDelta(string? before, string? after) =>
        decimal.TryParse(before, NumberStyles.Any, CultureInfo.InvariantCulture, out var beforeValue)
        && decimal.TryParse(after, NumberStyles.Any, CultureInfo.InvariantCulture, out var afterValue)
            ? afterValue - beforeValue
            : null;
}
