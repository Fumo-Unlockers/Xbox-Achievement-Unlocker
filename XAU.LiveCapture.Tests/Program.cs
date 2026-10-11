using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using XAU.Services.LiveCapture;

var tests = new (string Name, Func<Task> Run)[]
{
    ("start, overlap guard, and stop", StartStop),
    ("initial baseline suppresses existing unlocks", InitialBaseline),
    ("achievement unlock detection", AchievementUnlock),
    ("progression change detection", ProgressionChange),
    ("duplicate snapshot suppression", DuplicateSuppression),
    ("missing progression values remain unavailable", MissingProgression),
    ("API failure and recovery", ApiRecovery),
    ("malformed API response recovery", MalformedApiRecovery),
    ("cancellation during polling", CancellationDuringPolling),
    ("game process exit and restart", ProcessRestart),
    ("unsupported process identity", UnsupportedProcess),
    ("sanitized JSON export", SanitizedExport),
    ("bounded observation history", BoundedHistory),
    ("application shutdown cleanup", ShutdownCleanup),
    ("Research Lab theme resources", ThemeCompatibility)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.WriteLine($"FAIL {test.Name}: {exception.Message}");
    }
}

Console.WriteLine($"Live Capture tests: {tests.Length - failed} passed, {failed} failed.");
return failed == 0 ? 0 : 1;

static async Task StartStop()
{
    using var coordinator = CreateCoordinator(new SequenceSource(Response("1", "Already done", "Achieved", "1")));
    await coordinator.StartAsync(Title(), TimeSpan.FromMilliseconds(20));
    await AssertThrows<InvalidOperationException>(() =>
        coordinator.StartAsync(Title(), TimeSpan.FromMilliseconds(20)));
    await WaitUntil(() => coordinator.CurrentSession?.LastSuccessfulApiPollUtc != null);
    await coordinator.StopAsync();
    Assert(!coordinator.IsRunning, "Coordinator remained running after StopAsync.");
    Assert(coordinator.CurrentSession?.EndedAtUtc != null, "Session end time was not recorded.");
}

static async Task InitialBaseline()
{
    using var coordinator = CreateCoordinator(new SequenceSource(Response("1", "Already done", "Achieved", "1")));
    await coordinator.StartAsync(Title(), TimeSpan.FromMilliseconds(15));
    await WaitUntil(() => coordinator.CurrentSession?.Observations.Count >= 2);
    await coordinator.StopAsync();
    Assert(!coordinator.CurrentSession!.Observations.Any(observation =>
        observation.EvidenceClassification == LiveCaptureEvidence.XboxStateChange),
        "Baseline reported an existing unlock as a new state change.");
}

static async Task AchievementUnlock()
{
    var source = new SequenceSource(
        Response("1", "Test unlock", "NotStarted", "0"),
        Response("1", "Test unlock", "Achieved", "1", "2026-10-11T12:00:00Z"));
    using var coordinator = CreateCoordinator(source);
    await coordinator.StartAsync(Title(), TimeSpan.FromMilliseconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(observation =>
        observation.Category == "Achievement unlocked"));
    await coordinator.StopAsync();
    var observation = coordinator.CurrentSession!.Observations.Single(item => item.Category == "Achievement unlocked");
    Assert(observation.AchievementId == "1", "Unlock observation did not retain the API achievement ID.");
    Assert(observation.XboxReportedUnlockTime == "2026-10-11T12:00:00Z", "Xbox unlock time was not preserved.");
    Assert(observation.ObservedAtUtc != default, "Observation time was not recorded separately.");
}

static Task ProgressionChange()
{
    var before = LiveCaptureSnapshotComparer.Create(Response("19", "Progress", "InProgress", "5"));
    var after = LiveCaptureSnapshotComparer.Create(Response("19", "Progress", "InProgress", "7"));
    var change = LiveCaptureSnapshotComparer.Compare(before, after, DateTimeOffset.UtcNow).Single();
    Assert(change.EvidenceClassification == LiveCaptureEvidence.ProgressionChange, "Progression evidence was misclassified.");
    Assert(change.ProgressionChanges.Single().Delta == 2, "Progression delta was incorrect.");
    return Task.CompletedTask;
}

static Task DuplicateSuppression()
{
    var snapshot = LiveCaptureSnapshotComparer.Create(Response("1", "Same", "InProgress", "5"));
    Assert(LiveCaptureSnapshotComparer.Compare(snapshot, snapshot, DateTimeOffset.UtcNow).Count == 0,
        "An unchanged snapshot produced an observation.");
    return Task.CompletedTask;
}

static Task MissingProgression()
{
    var before = LiveCaptureSnapshotComparer.Create(Response("1", "Missing", "InProgress", null));
    var after = LiveCaptureSnapshotComparer.Create(Response("1", "Missing", "InProgress", "2"));
    var change = LiveCaptureSnapshotComparer.Compare(before, after, DateTimeOffset.UtcNow)
        .Single().ProgressionChanges.Single();
    Assert(change.PreviousValue == null, "Missing progression was invented instead of remaining unavailable.");
    Assert(change.Display.Contains("<unavailable>"), "Missing progression was not displayed as unavailable.");
    return Task.CompletedTask;
}

static async Task ApiRecovery()
{
    var source = new SequenceSource(
        new OperationCanceledException("request timeout"),
        Response("1", "Recovered", "NotStarted", "0"));
    using var coordinator = CreateCoordinator(source, TimeSpan.FromMilliseconds(5));
    await coordinator.StartAsync(Title(), TimeSpan.FromMilliseconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(observation =>
        observation.Category == "API recovery"));
    await coordinator.StopAsync();
    Assert(coordinator.CurrentSession!.Observations.Any(observation => observation.Category == "API interruption"),
        "Transient API failure was not reported.");
}

static async Task MalformedApiRecovery()
{
    var source = new SequenceSource(
        new AchievementsResponse { achievements = null! },
        Response("1", "Recovered", "NotStarted", "0"));
    using var coordinator = CreateCoordinator(source, TimeSpan.FromMilliseconds(5));
    await coordinator.StartAsync(Title(), TimeSpan.FromMilliseconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(observation =>
        observation.Category == "API recovery"));
    await coordinator.StopAsync();
    Assert(coordinator.CurrentSession!.Status == LiveCaptureSessionState.Completed,
        "Malformed response prevented later valid polling.");
}

static async Task CancellationDuringPolling()
{
    using var coordinator = CreateCoordinator(new SequenceSource(Response("1", "Cancel", "NotStarted", "0")));
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(30));
    await WaitUntil(() => coordinator.CurrentSession?.LastSuccessfulApiPollUtc != null);
    var stopwatch = Stopwatch.StartNew();
    await coordinator.StopAsync();
    Assert(stopwatch.Elapsed < TimeSpan.FromSeconds(1), "Cancellation waited for the polling interval.");
}

static async Task ProcessRestart()
{
    var resolver = new SequenceProcessResolver([], [Process(100)], [], [Process(200)]);
    using var coordinator = new LiveCaptureCoordinator([
        new GameProcessCaptureProvider(resolver, TimeSpan.FromMilliseconds(5))
    ]);
    await coordinator.StartAsync(Title("Package.Game_123"), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(observation =>
        observation.Category == "Process restart"));
    await coordinator.StopAsync();
    Assert(coordinator.CurrentSession!.Observations.Any(observation => observation.Category == "Process exit"),
        "Process exit was not observed before restart.");
}

static Task UnsupportedProcess()
{
    var capability = new GameProcessCaptureProvider(new SequenceProcessResolver([])).GetCapability(Title());
    Assert(!capability.Available, "A title without package/path identity was marked process-monitorable.");
    return Task.CompletedTask;
}

static async Task SanitizedExport()
{
    using var coordinator = new LiveCaptureCoordinator([new BurstProvider(1,
        "authorization=secret token=secret XBL3.0 x=very-secret")]);
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Count >= 2);
    var json = coordinator.ExportSessionJson();
    await coordinator.StopAsync();
    Assert(!json.Contains("very-secret", StringComparison.Ordinal), "XBL token leaked into export.");
    Assert(!json.Contains("authorization=secret", StringComparison.OrdinalIgnoreCase), "Authorization leaked into export.");
    Assert(!json.Contains("token=secret", StringComparison.OrdinalIgnoreCase), "Token leaked into export.");
}

static async Task BoundedHistory()
{
    using var coordinator = new LiveCaptureCoordinator([new BurstProvider(550, "bounded")]);
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Count == LiveCaptureCoordinator.MaximumObservations);
    Assert(coordinator.CurrentSession!.Observations.Count == LiveCaptureCoordinator.MaximumObservations,
        "Observation history exceeded its bound.");
    await coordinator.StopAsync();
}

static async Task ShutdownCleanup()
{
    using var coordinator = CreateCoordinator(new SequenceSource(Response("1", "Shutdown", "NotStarted", "0")));
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(30));
    await ((IHostedService)coordinator).StopAsync(CancellationToken.None);
    Assert(!coordinator.IsRunning, "Hosted-service shutdown left capture running.");
}

static Task ThemeCompatibility()
{
    var root = FindRepositoryRoot();
    var xaml = File.ReadAllText(Path.Combine(root, "XAU", "Views", "Pages", "AchievementResearchLabPage.xaml"));
    Assert(xaml.Contains("DynamicResource ApplicationBackgroundBrush", StringComparison.Ordinal),
        "Research Lab no longer uses the application theme background.");
    Assert(xaml.Contains("Live Capture (passive, read-only)", StringComparison.Ordinal),
        "Live Capture UI section was not found.");
    return Task.CompletedTask;
}

static LiveCaptureCoordinator CreateCoordinator(
    IAchievementSnapshotSource source,
    TimeSpan? retryDelay = null) =>
    new([new XboxAchievementCaptureProvider(source, retryDelay ?? TimeSpan.FromMilliseconds(5))]);

static LiveCaptureTitle Title(string? package = null) => new("123", "Test title", package);

static GameProcessIdentity Process(int id) => new()
{
    ProcessId = id,
    ProcessName = "TestGame",
    PackageFamilyName = "Package.Game_123"
};

static AchievementsResponse Response(
    string id,
    string name,
    string state,
    string? current,
    string? unlockTime = null) => new()
{
    achievements =
    [
        new OneCoreAchievementResponse
        {
            id = id,
            name = name,
            progressState = state,
            progression = new AchievementProgression
            {
                timeUnlocked = unlockTime,
                requirements =
                [
                    new AchievementRequirements
                    {
                        id = "requirement-1",
                        current = current,
                        target = "10",
                        operationType = "Maximum",
                        valueType = "Integer"
                    }
                ]
            }
        }
    ]
};

static async Task WaitUntil(Func<bool> condition, int timeoutMilliseconds = 3000)
{
    var timeout = Stopwatch.StartNew();
    while (!condition())
    {
        if (timeout.ElapsedMilliseconds >= timeoutMilliseconds)
            throw new TimeoutException("Timed out waiting for the expected condition.");
        await Task.Delay(5);
    }
}

static async Task AssertThrows<T>(Func<Task> action) where T : Exception
{
    try
    {
        await action();
    }
    catch (T)
    {
        return;
    }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory != null && !File.Exists(Path.Combine(directory.FullName, "XAU.sln")))
        directory = directory.Parent;
    return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
}

sealed class SequenceSource(params object[] results) : IAchievementSnapshotSource
{
    private readonly object[] _results = results;
    private int _index;

    public bool HasAuthentication { get; set; } = true;

    public Task<AchievementsResponse?> GetAchievementsAsync(string titleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = Math.Min(Interlocked.Increment(ref _index) - 1, _results.Length - 1);
        var result = _results[index];
        return result is Exception exception
            ? Task.FromException<AchievementsResponse?>(exception)
            : Task.FromResult((AchievementsResponse?)result);
    }
}

sealed class SequenceProcessResolver(params IReadOnlyList<GameProcessIdentity>[] results) : IGameProcessResolver
{
    private readonly IReadOnlyList<GameProcessIdentity>[] _results = results;
    private int _index;

    public ValueTask<IReadOnlyList<GameProcessIdentity>> FindProcessesAsync(
        LiveCaptureTitle title,
        CancellationToken cancellationToken)
    {
        var index = Math.Min(Interlocked.Increment(ref _index) - 1, _results.Length - 1);
        return ValueTask.FromResult(_results[index]);
    }
}

sealed class BurstProvider(int count, string message) : ILiveCaptureProvider
{
    public string Name => "Test provider";

    public CaptureSourceCapability GetCapability(LiveCaptureTitle title) => new()
    {
        Source = Name,
        Available = true,
        Description = "Test provider"
    };

    public async Task RunAsync(LiveCaptureProviderContext context, CancellationToken cancellationToken)
    {
        for (var index = 0; index < count; index++)
        {
            await context.PublishAsync(new LiveCaptureObservation
            {
                Source = Name,
                Category = "Test",
                EvidenceClassification = LiveCaptureEvidence.MonitoringStatus,
                Message = $"{message} {index}"
            });
        }
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }
}
