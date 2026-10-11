using Newtonsoft.Json;

namespace XAU.Services.LiveCapture;

public static class LiveCaptureSessionState
{
    public const string Idle = "Idle";
    public const string WaitingForGame = "Waiting for game";
    public const string Monitoring = "Monitoring";
    public const string Stopping = "Stopping";
    public const string Completed = "Completed";
    public const string CaptureError = "Capture error";
}

public static class LiveCaptureEvidence
{
    public const string XboxStateChange = "Xbox state change observed";
    public const string ProgressionChange = "Progression change observed";
    public const string InternalEvent = "Internal event observed";
    public const string NoInternalEventSource = "No internal event source available";
    public const string MonitoringInterrupted = "Monitoring interrupted";
    public const string CaptureFailed = "Capture failed";
    public const string ProcessObserved = "Game process observed";
    public const string MonitoringStatus = "Monitoring status";
}

public sealed record LiveCaptureTitle(
    string TitleId,
    string DisplayName,
    string? PackageFamilyName = null,
    string? ExecutablePath = null);

public sealed class CaptureSourceCapability
{
    public string Source { get; init; } = "";
    public bool Available { get; init; }
    public string Description { get; init; } = "";
    public bool RequiresGameProcess { get; init; }
    public bool RequiresAdministrator { get; init; }
    public bool CanObserveInternalEventIds { get; init; }
    public bool SupportsStop { get; init; } = true;
    public string Availability => Available ? "Available" : "Unavailable";
}

public sealed class LiveCaptureProgressChange
{
    public string RequirementId { get; init; } = "<unknown>";
    public string? PreviousValue { get; init; }
    public string? NewValue { get; init; }
    public string? TargetValue { get; init; }
    public decimal? Delta { get; init; }

    [JsonIgnore]
    public string Display =>
        $"{RequirementId}: {PreviousValue ?? "<unavailable>"} -> {NewValue ?? "<unavailable>"}" +
        (Delta.HasValue ? $" ({Delta.Value:+0.################;-0.################;0})" : "");
}

public sealed class LiveCaptureObservation
{
    public DateTimeOffset ObservedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string Source { get; init; } = "";
    public string Category { get; init; } = "";
    public string EvidenceClassification { get; init; } = LiveCaptureEvidence.MonitoringStatus;
    public string Message { get; init; } = "";
    public string? AchievementId { get; init; }
    public string? AchievementName { get; init; }
    public string? PreviousState { get; init; }
    public string? NewState { get; init; }
    public string? XboxReportedUnlockTime { get; init; }
    public string? InternalEventName { get; init; }
    public string? InternalAchievementId { get; init; }
    public IReadOnlyList<LiveCaptureProgressChange> ProgressionChanges { get; init; } = [];

    [JsonIgnore]
    public string TimestampDisplay => ObservedAtUtc.ToLocalTime().ToString("HH:mm:ss");

    [JsonIgnore]
    public string AchievementDisplay => string.IsNullOrWhiteSpace(AchievementId)
        ? ""
        : $"{AchievementId} - {AchievementName}";

    [JsonIgnore]
    public string StateDisplay => PreviousState == NewState
        ? NewState ?? ""
        : $"{PreviousState ?? "<unavailable>"} -> {NewState ?? "<unavailable>"}";

    [JsonIgnore]
    public string ProgressDisplay => ProgressionChanges.Count == 0
        ? ""
        : string.Join("; ", ProgressionChanges.Select(change => change.Display));

    [JsonIgnore]
    public string DetailsDisplay =>
        $"Observed: {ObservedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss zzz}{Environment.NewLine}" +
        $"Evidence: {EvidenceClassification}{Environment.NewLine}" +
        $"Xbox-reported unlock time: {XboxReportedUnlockTime ?? "<unavailable>"}{Environment.NewLine}" +
        $"Internal event: {InternalEventName ?? "<not observed>"}{Environment.NewLine}" +
        $"Internal achievement ID: {InternalAchievementId ?? "<not observed>"}{Environment.NewLine}" +
        $"Details: {Message}" +
        (ProgressionChanges.Count == 0
            ? ""
            : $"{Environment.NewLine}Progression: {ProgressDisplay}");
}

public sealed class LiveCaptureSession
{
    public Guid SessionId { get; init; } = Guid.NewGuid();
    public required string TitleId { get; init; }
    public required string DisplayName { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAtUtc { get; internal set; }
    public string Status { get; internal set; } = LiveCaptureSessionState.Idle;
    public string StatusMessage { get; internal set; } = "";
    public string ProcessStatus { get; internal set; } = "Not checked";
    public DateTimeOffset? LastSuccessfulApiPollUtc { get; internal set; }
    public IReadOnlyList<CaptureSourceCapability> Capabilities { get; init; } = [];
    public IReadOnlyList<string> EnabledSources { get; init; } = [];
    public List<LiveCaptureObservation> Observations { get; } = [];
    public bool InternalEventTelemetryCaptured => Observations.Any(observation =>
        observation.EvidenceClassification == LiveCaptureEvidence.InternalEvent);
}

public sealed class GameProcessIdentity
{
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = "";
    public string? ExecutablePath { get; init; }
    public string? PackageFamilyName { get; init; }
}

public sealed class LiveCaptureRequirementSnapshot
{
    public string Id { get; init; } = "<unknown>";
    public string? Current { get; init; }
    public string? Target { get; init; }
}

public sealed class LiveCaptureAchievementSnapshot
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? State { get; init; }
    public string? XboxReportedUnlockTime { get; init; }
    public IReadOnlyDictionary<string, LiveCaptureRequirementSnapshot> Requirements { get; init; }
        = new Dictionary<string, LiveCaptureRequirementSnapshot>();
}

public sealed class LiveCaptureSnapshot
{
    public IReadOnlyDictionary<string, LiveCaptureAchievementSnapshot> Achievements { get; init; }
        = new Dictionary<string, LiveCaptureAchievementSnapshot>();
}
