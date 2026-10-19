namespace XAU.Services.LiveCapture;

internal sealed class LiveCaptureEvidenceStore
{
    public const int MaximumCriticalEvidenceRecords = 512;
    public const int MaximumProgressionEvidenceRecords = 2048;
    public const int MaximumBaselineValues = 4096;

    private readonly List<CriticalEvidenceEntry> _criticalEvidence = [];
    private readonly Dictionary<RequirementKey, LiveCaptureProgressionEvidence> _progressionEvidence = [];
    private readonly Dictionary<RequirementKey, string?> _baselineValues = [];
    private long _currentBaselineGeneration;

    public long TotalObservationsPublished { get; private set; }
    public long TimelineObservationsDropped { get; private set; }
    public long CriticalEvidenceRecordsDropped { get; private set; }
    public long HighPriorityEvidenceRecordsDropped { get; private set; }
    public long ProgressionEvidenceRecordsDropped { get; private set; }
    public long BaselineValuesDropped { get; private set; }
    public long ProgressionUpdatesCoalesced { get; private set; }

    public IReadOnlyList<LiveCaptureObservation> CriticalEvidence =>
        _criticalEvidence.Select(entry => entry.Observation).ToList();

    public IReadOnlyList<LiveCaptureProgressionEvidence> ProgressionEvidence =>
        _progressionEvidence.Values
            .OrderBy(entry => entry.AchievementId, StringComparer.Ordinal)
            .ThenBy(entry => entry.RequirementId, StringComparer.Ordinal)
            .ThenBy(entry => entry.BaselineGeneration)
            .ToList();

    public int CriticalEvidenceRecordsRetained => _criticalEvidence.Count;
    public int ProgressionEvidenceRecordsRetained => _progressionEvidence.Count;
    public int BaselineValuesRetained => _baselineValues.Count;
    public int EvidenceRecordsRetained =>
        CriticalEvidenceRecordsRetained + ProgressionEvidenceRecordsRetained + BaselineValuesRetained;
    public long EvidenceRecordsDropped =>
        CriticalEvidenceRecordsDropped + ProgressionEvidenceRecordsDropped + BaselineValuesDropped;
    public bool EvidenceLossOccurred => EvidenceRecordsDropped > 0;

    public void RecordBaseline(LiveCaptureSnapshot snapshot, long generation)
    {
        _currentBaselineGeneration = generation;
        foreach (var achievement in snapshot.Achievements.Values)
        {
            foreach (var requirement in achievement.Requirements.Values)
            {
                var key = new RequirementKey(generation, achievement.Id, requirement.Id);
                if (_baselineValues.ContainsKey(key))
                    continue;
                if (_baselineValues.Count >= MaximumBaselineValues)
                {
                    BaselineValuesDropped++;
                    continue;
                }
                _baselineValues.Add(key, requirement.Current);
            }
        }
    }

    public void RecordObservation(LiveCaptureObservation observation)
    {
        TotalObservationsPublished++;
        RecordProgression(observation);
        UpdateRelatedState(observation);
        if (IsCritical(observation))
            RecordCritical(observation);
    }

    public void RecordTimelineDrop() => TimelineObservationsDropped++;

    private void RecordProgression(LiveCaptureObservation observation)
    {
        if (observation.ProgressionChanges.Count == 0)
            return;

        var achievementId = string.IsNullOrWhiteSpace(observation.AchievementId)
            ? "<unknown>"
            : observation.AchievementId;
        foreach (var change in observation.ProgressionChanges)
        {
            var key = new RequirementKey(_currentBaselineGeneration, achievementId, change.RequirementId);
            if (_progressionEvidence.TryGetValue(key, out var existing))
            {
                existing.LatestValue = change.NewValue;
                existing.TargetValue = change.TargetValue ?? existing.TargetValue;
                existing.LastObservedAtUtc = observation.ObservedAtUtc;
                existing.ChangeCount++;
                ProgressionUpdatesCoalesced++;
                continue;
            }

            if (_progressionEvidence.Count >= MaximumProgressionEvidenceRecords)
            {
                ProgressionEvidenceRecordsDropped++;
                continue;
            }

            _baselineValues.TryGetValue(key, out var baselineValue);
            _progressionEvidence.Add(key, new LiveCaptureProgressionEvidence
            {
                BaselineGeneration = _currentBaselineGeneration,
                AchievementId = achievementId,
                AchievementName = observation.AchievementName ?? "<unknown>",
                RequirementId = change.RequirementId,
                OriginalBaselineValue = baselineValue,
                FirstPreviousValue = change.PreviousValue,
                FirstChangedValue = change.NewValue,
                LatestValue = change.NewValue,
                TargetValue = change.TargetValue,
                FirstObservedAtUtc = observation.ObservedAtUtc,
                LastObservedAtUtc = observation.ObservedAtUtc,
                ChangeCount = 1,
                InitialState = observation.PreviousState,
                LatestState = observation.NewState,
                XboxReportedUnlockTime = MeaningfulUnlockTime(observation.XboxReportedUnlockTime)
            });
        }
    }

    private void UpdateRelatedState(LiveCaptureObservation observation)
    {
        if (string.IsNullOrWhiteSpace(observation.AchievementId))
            return;

        var stateChanged = !string.Equals(
            observation.PreviousState,
            observation.NewState,
            StringComparison.OrdinalIgnoreCase);
        var unlockTime = MeaningfulUnlockTime(observation.XboxReportedUnlockTime);
        if (!stateChanged && unlockTime == null)
            return;

        foreach (var evidence in _progressionEvidence.Values.Where(item =>
                     item.BaselineGeneration == _currentBaselineGeneration
                     && string.Equals(item.AchievementId, observation.AchievementId, StringComparison.Ordinal)))
        {
            evidence.InitialState ??= observation.PreviousState;
            evidence.LatestState = observation.NewState ?? evidence.LatestState;
            evidence.XboxReportedUnlockTime = unlockTime ?? evidence.XboxReportedUnlockTime;
            if (stateChanged)
                evidence.StateTransitionCount++;
        }
    }

    private void RecordCritical(LiveCaptureObservation observation)
    {
        var priority = GetPriority(observation);
        if (_criticalEvidence.Count < MaximumCriticalEvidenceRecords)
        {
            _criticalEvidence.Add(new CriticalEvidenceEntry(observation, priority));
            return;
        }

        var lowestPriority = _criticalEvidence.Min(entry => entry.Priority);
        if (priority < lowestPriority || priority == lowestPriority && priority == 1)
        {
            CriticalEvidenceRecordsDropped++;
            return;
        }

        var index = _criticalEvidence.FindIndex(entry => entry.Priority == lowestPriority);
        if (_criticalEvidence[index].Priority == 4)
            HighPriorityEvidenceRecordsDropped++;
        _criticalEvidence.RemoveAt(index);
        _criticalEvidence.Add(new CriticalEvidenceEntry(observation, priority));
        CriticalEvidenceRecordsDropped++;
    }

    private static bool IsCritical(LiveCaptureObservation observation)
    {
        var stateChanged = !string.Equals(
            observation.PreviousState,
            observation.NewState,
            StringComparison.OrdinalIgnoreCase);
        return observation.ProgressionChanges.Count == 0
            || stateChanged
            || MeaningfulUnlockTime(observation.XboxReportedUnlockTime) != null
            || ContainsEvidence(observation, LiveCaptureEvidence.InternalEvent);
    }

    private static int GetPriority(LiveCaptureObservation observation)
    {
        var stateChanged = !string.Equals(
            observation.PreviousState,
            observation.NewState,
            StringComparison.OrdinalIgnoreCase);
        if (stateChanged
            || MeaningfulUnlockTime(observation.XboxReportedUnlockTime) != null
            || ContainsEvidence(observation, LiveCaptureEvidence.InternalEvent))
        {
            return 4;
        }

        if (observation.Category.Equals("Baseline", StringComparison.OrdinalIgnoreCase)
            || observation.Category.Equals("Capability", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (observation.Source.Equals(GameProcessCaptureProvider.ProviderName, StringComparison.OrdinalIgnoreCase)
            || observation.Category.StartsWith("Process", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return ContainsEvidence(observation, LiveCaptureEvidence.CaptureFailed)
            || ContainsEvidence(observation, LiveCaptureEvidence.MonitoringInterrupted)
                ? 2
                : 1;
    }

    private static bool ContainsEvidence(LiveCaptureObservation observation, string evidence) =>
        observation.EvidenceClassification.Contains(evidence, StringComparison.OrdinalIgnoreCase);

    private static string? MeaningfulUnlockTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateTimeOffset.TryParse(value, out var parsed) && parsed.Year > 1 ? value : null;
    }

    private readonly record struct RequirementKey(long BaselineGeneration, string AchievementId, string RequirementId);
    private sealed record CriticalEvidenceEntry(LiveCaptureObservation Observation, int Priority);
}
