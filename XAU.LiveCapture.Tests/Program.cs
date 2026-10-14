using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
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
    ("progression evidence survives timeline eviction", ProgressionEvidenceSurvivesEviction),
    ("unlock evidence survives timeline eviction", UnlockEvidenceSurvivesEviction),
    ("multiple requirements remain separate", MultipleRequirements),
    ("capture sessions keep independent evidence", SessionEvidenceIsolation),
    ("unchanged polls do not inflate evidence", UnchangedPolls),
    ("process restart re-establishes baseline", ProcessRestartBaseline),
    ("process restart separates baseline generations", ProcessRestartEvidenceGenerations),
    ("Cairo evidence remains reconstructable", CairoRetentionRegression),
    ("evidence limits report loss", EvidenceLimits),
    ("critical unlock eviction is explicit", CriticalUnlockEviction),
    ("version 2 export preserves version 1 fields", VersionOneExportCompatibility),
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
    var baseline = Snapshot(("1", "Secret", "requirement-1", "{\"token\":\"baseline-secret\"}", "10"));
    var observation = ProgressObservation(
        "1",
        "Secret",
        "requirement-1",
        "authorization=previous-secret",
        "XBL3.0 x=very-secret",
        "token=target-secret");
    using var coordinator = new LiveCaptureCoordinator([
        new EvidenceProvider([observation], baseline,
            "{\"authorization\":\"secret\",\"xuid\":\"private-xuid\"} Authorization: Bearer bearer-secret")
    ]);
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Count >= 2);
    var json = coordinator.ExportSessionJson();
    await coordinator.StopAsync();
    Assert(!json.Contains("very-secret", StringComparison.Ordinal), "XBL token leaked into export.");
    Assert(!json.Contains("authorization=secret", StringComparison.OrdinalIgnoreCase), "Authorization leaked into export.");
    Assert(!json.Contains("token=secret", StringComparison.OrdinalIgnoreCase), "Token leaked into export.");
    Assert(!json.Contains("baseline-secret", StringComparison.Ordinal), "Baseline evidence leaked a sensitive value.");
    Assert(!json.Contains("previous-secret", StringComparison.Ordinal), "Previous progression evidence leaked a sensitive value.");
    Assert(!json.Contains("target-secret", StringComparison.Ordinal), "Target progression evidence leaked a sensitive value.");
    Assert(!json.Contains("private-xuid", StringComparison.Ordinal), "Nested diagnostic XUID leaked into export.");
    Assert(!json.Contains("bearer-secret", StringComparison.Ordinal), "Bearer credential leaked into export.");
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

static async Task ProgressionEvidenceSurvivesEviction()
{
    var observations = Enumerable.Range(1, 550)
        .Select(value => ProgressObservation(
            "19", "Progress", "requirement-1", (value - 1).ToString(), value.ToString(), "1000"))
        .ToList();
    using var coordinator = new LiveCaptureCoordinator([new EvidenceProvider(observations)]);
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(item => item.Message == "change 550"));

    var export = JObject.Parse(coordinator.ExportSessionJson());
    var evidence = ProgressionEvidence(export, "19", "requirement-1");
    Assert(export["observations"]!.Count() == LiveCaptureCoordinator.MaximumObservations,
        "Recent timeline did not retain exactly its configured bound.");
    Assert(export["retention"]!["timelineObservationsDropped"]!.Value<long>() > 0,
        "Timeline eviction was not reported.");
    Assert(evidence["firstPreviousValue"]!.Value<string>() == "0", "First progression value was not retained.");
    Assert(evidence["firstChangedValue"]!.Value<string>() == "1", "First changed value was not retained.");
    Assert(evidence["latestValue"]!.Value<string>() == "550", "Latest progression value was not retained.");
    Assert(evidence["changeCount"]!.Value<long>() == 550, "Actual progression change count was incorrect.");
    Assert(export["retention"]!["evidenceRecordsCoalesced"]!.Value<long>() == 549,
        "Coalesced update count was incorrect.");
    await coordinator.StopAsync();
}

static async Task UnlockEvidenceSurvivesEviction()
{
    var observations = new List<LiveCaptureObservation>
    {
        UnlockObservation("1", "Unlocked", "2026-10-14T15:13:11Z")
    };
    observations.AddRange(Enumerable.Range(1, 550).Select(value =>
        ProgressObservation("noise", "Noise", "noise-requirement", (value - 1).ToString(), value.ToString(), "9999")));
    using var coordinator = new LiveCaptureCoordinator([new EvidenceProvider(observations)]);
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(item => item.Message == "change 550"));

    var export = JObject.Parse(coordinator.ExportSessionJson());
    Assert(!export["observations"]!.Any(item => item!["achievementId"]?.Value<string>() == "1"),
        "Unlock unexpectedly remained in the recent timeline; the test did not exercise eviction.");
    Assert(export["criticalEvidence"]!.Any(item =>
            item!["achievementId"]?.Value<string>() == "1"
            && item["category"]?.Value<string>() == "Achievement unlocked"),
        "Critical unlock evidence was lost after timeline eviction.");
    await coordinator.StopAsync();
}

static async Task MultipleRequirements()
{
    var baseline = Snapshot(
        ("1", "Multi", "requirement-a", "10", "100"),
        ("1", "Multi", "requirement-b", "20", "200"));
    var observation = new LiveCaptureObservation
    {
        Source = XboxAchievementCaptureProvider.ProviderName,
        Category = "Progression",
        EvidenceClassification = LiveCaptureEvidence.ProgressionChange,
        AchievementId = "1",
        AchievementName = "Multi",
        PreviousState = "InProgress",
        NewState = "InProgress",
        ProgressionChanges =
        [
            Change("requirement-a", "10", "11", "100"),
            Change("requirement-b", "20", "25", "200")
        ]
    };
    using var coordinator = new LiveCaptureCoordinator([new EvidenceProvider([observation], baseline)]);
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(item => item.AchievementId == "1"));

    var export = JObject.Parse(coordinator.ExportSessionJson());
    var evidence = export["progressionEvidence"]!.Where(item => item!["achievementId"]!.Value<string>() == "1").ToList();
    Assert(evidence.Count == 2, "Multiple requirements were merged into one evidence record.");
    Assert(ProgressionEvidence(export, "1", "requirement-a")["originalBaselineValue"]!.Value<string>() == "10",
        "Requirement A baseline was not retained.");
    Assert(ProgressionEvidence(export, "1", "requirement-b")["latestValue"]!.Value<string>() == "25",
        "Requirement B latest value was not retained.");
    await coordinator.StopAsync();
}

static async Task UnchangedPolls()
{
    var response = Response("1", "Unchanged", "InProgress", "5");
    var source = new SequenceSource(response, response, response, response);
    using var coordinator = CreateCoordinator(source);
    await coordinator.StartAsync(Title(), TimeSpan.FromMilliseconds(5));
    await WaitUntil(() => source.CallCount >= 4);

    var export = JObject.Parse(coordinator.ExportSessionJson());
    Assert(!export["progressionEvidence"]!.Any(), "Unchanged polling created progression evidence.");
    Assert(!export["observations"]!.Any(item => item!["category"]?.Value<string>() == "Progression"),
        "Unchanged polling created timeline progression observations.");
    await coordinator.StopAsync();
}

static async Task SessionEvidenceIsolation()
{
    var baseline = Snapshot(("1", "Separate", "requirement-1", "5", "10"));
    var provider = new EvidenceProvider([
        ProgressObservation("1", "Separate", "requirement-1", "5", "6", "10")
    ], baseline);
    using var coordinator = new LiveCaptureCoordinator([provider]);

    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(item => item.AchievementId == "1"));
    var first = JObject.Parse(coordinator.ExportSessionJson());
    await coordinator.StopAsync();

    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(item => item.AchievementId == "1"));
    var second = JObject.Parse(coordinator.ExportSessionJson());
    await coordinator.StopAsync();

    Assert(first["sessionId"]!.Value<string>() != second["sessionId"]!.Value<string>(),
        "Capture sessions reused an evidence identity.");
    Assert(second["progressionEvidence"]!.Count() == 1,
        "A later capture session inherited progression evidence from an earlier session.");
    Assert(ProgressionEvidence(second, "1", "requirement-1")["changeCount"]!.Value<long>() == 1,
        "A later capture session inherited change counts from an earlier session.");
}

static async Task ProcessRestartBaseline()
{
    var resolver = new SequenceProcessResolver(
        [], [Process(100)], [Process(100)], [Process(100)], [], [], [Process(200)]);
    var source = new SequenceSource(Response("1", "Restart", "InProgress", "5"));
    using var coordinator = new LiveCaptureCoordinator([
        new GameProcessCaptureProvider(resolver, TimeSpan.FromMilliseconds(10)),
        new XboxAchievementCaptureProvider(source, TimeSpan.FromMilliseconds(5))
    ]);
    await coordinator.StartAsync(Title("Package.Game_123"), TimeSpan.FromMilliseconds(5));
    await WaitUntil(() =>
    {
        var export = JObject.Parse(coordinator.ExportSessionJson());
        return export["criticalEvidence"]!.Count(item => item!["category"]?.Value<string>() == "Baseline") >= 2;
    });

    var json = JObject.Parse(coordinator.ExportSessionJson());
    Assert(json["criticalEvidence"]!.Any(item => item!["category"]?.Value<string>() == "Process restart"),
        "Process restart was not retained as critical evidence.");
    Assert(json["criticalEvidence"]!.Any(item =>
            item!["message"]?.Value<string>()?.Contains("baseline re-established", StringComparison.OrdinalIgnoreCase) == true),
        "Achievement baseline was not re-established after process restart.");
    await coordinator.StopAsync();
}

static async Task ProcessRestartEvidenceGenerations()
{
    using var coordinator = new LiveCaptureCoordinator([new RestartEvidenceProvider()]);
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(item => item.Message == "post-restart change"));

    var export = JObject.Parse(coordinator.ExportSessionJson());
    var evidence = export["progressionEvidence"]!
        .Where(item => item!["achievementId"]!.Value<string>() == "1"
            && item["requirementId"]!.Value<string>() == "requirement-1")
        .OrderBy(item => item!["baselineGeneration"]!.Value<long>())
        .ToList();
    Assert(evidence.Count == 2, "Pre- and post-restart progression evidence was merged.");
    Assert(evidence[0]!["baselineGeneration"]!.Value<long>() == 0
        && evidence[0]!["originalBaselineValue"]!.Value<string>() == "5"
        && evidence[0]!["latestValue"]!.Value<string>() == "6",
        "Initial process-generation evidence was not preserved.");
    Assert(evidence[1]!["baselineGeneration"]!.Value<long>() == 1
        && evidence[1]!["originalBaselineValue"]!.Value<string>() == "10"
        && evidence[1]!["latestValue"]!.Value<string>() == "11",
        "Restarted process-generation evidence used the wrong baseline.");
    await coordinator.StopAsync();
}

static async Task CairoRetentionRegression()
{
    var baseline = Snapshot(
        ("280", "Don't Hangar Around", "time-cairo", "1067400", "900000"),
        ("293", "Going Nowhere Fast", "time-all", "1067400", "900000"),
        ("294", "Bomb Squad", "score-cairo", "5344", "14000"),
        ("307", "Arcade Owner", "score-all", "5344", "14000"));
    var observations = new List<LiveCaptureObservation>
    {
        ProgressObservation("280", "Don't Hangar Around", "time-cairo", "1067400", "846383", "900000"),
        ProgressObservation("293", "Going Nowhere Fast", "time-all", "1067400", "846383", "900000"),
        ProgressObservation("294", "Bomb Squad", "score-cairo", "5344", "6371", "14000"),
        ProgressObservation("307", "Arcade Owner", "score-all", "5344", "6371", "14000"),
        UnlockObservation("280", "Don't Hangar Around", "2026-10-14T15:13:11.9351773Z")
    };
    observations.AddRange(Enumerable.Range(1, 550).Select(value =>
        ProgressObservation("noise", "Generic stat", "noise", (value - 1).ToString(), value.ToString(), "9999")));
    using var coordinator = new LiveCaptureCoordinator([new EvidenceProvider(observations, baseline)]);
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(item => item.Message == "change 550"));

    var export = JObject.Parse(coordinator.ExportSessionJson());
    Assert(!export["observations"]!.Any(item => item!["achievementId"]?.Value<string>() == "280"),
        "Cairo observations unexpectedly remained in the recent timeline.");
    AssertProgression(export, "280", "time-cairo", "1067400", "846383");
    AssertProgression(export, "293", "time-all", "1067400", "846383");
    AssertProgression(export, "294", "score-cairo", "5344", "6371");
    AssertProgression(export, "307", "score-all", "5344", "6371");
    var timeEvidence = ProgressionEvidence(export, "280", "time-cairo");
    Assert(timeEvidence["latestState"]!.Value<string>() == "Achieved", "Cairo unlock state was not related to progression evidence.");
    Assert(timeEvidence["stateTransitionCount"]!.Value<long>() == 1, "Cairo state transition count was incorrect.");
    var retainedUnlockTime = timeEvidence["xboxReportedUnlockTime"]!.Value<DateTime>().ToUniversalTime();
    var expectedUnlockTime = DateTime.Parse(
        "2026-10-14T15:13:11.9351773Z",
        null,
        System.Globalization.DateTimeStyles.RoundtripKind);
    Assert(retainedUnlockTime == expectedUnlockTime,
        $"Cairo Xbox unlock time was not preserved (actual: {retainedUnlockTime:O}).");
    Assert(export["criticalEvidence"]!.Any(item => item!["achievementId"]?.Value<string>() == "280"),
        "Cairo unlock was not retained as critical evidence.");
    await coordinator.StopAsync();
}

static async Task EvidenceLimits()
{
    var observations = new List<LiveCaptureObservation>();
    observations.AddRange(Enumerable.Range(1, LiveCaptureCoordinator.MaximumCriticalEvidenceRecords + 10).Select(value =>
        new LiveCaptureObservation
        {
            Source = "Diagnostics",
            Category = "Warning",
            EvidenceClassification = LiveCaptureEvidence.MonitoringStatus,
            Message = $"warning {value}"
        }));
    observations.Add(UnlockObservation("critical-unlock", "Priority unlock", "2026-10-14T15:13:11Z"));
    observations.AddRange(Enumerable.Range(1, LiveCaptureCoordinator.MaximumProgressionEvidenceRecords + 2).Select(value =>
        ProgressObservation($"achievement-{value}", "Bounded", $"requirement-{value}", "0", "1", "10")));
    using var coordinator = new LiveCaptureCoordinator([new EvidenceProvider(observations)]);
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(item => item.AchievementId ==
        $"achievement-{LiveCaptureCoordinator.MaximumProgressionEvidenceRecords + 2}"), 10000);

    var export = JObject.Parse(coordinator.ExportSessionJson());
    var retention = export["retention"]!;
    Assert(export["criticalEvidence"]!.Count() <= LiveCaptureCoordinator.MaximumCriticalEvidenceRecords,
        "Critical evidence exceeded its memory bound.");
    Assert(export["criticalEvidence"]!.Any(item => item!["achievementId"]?.Value<string>() == "critical-unlock"),
        "Critical unlock evidence was not prioritized over routine diagnostics.");
    Assert(export["progressionEvidence"]!.Count() == LiveCaptureCoordinator.MaximumProgressionEvidenceRecords,
        "Progression evidence exceeded or did not fill its configured bound.");
    Assert(retention["evidenceLossOccurred"]!.Value<bool>(), "Bounded evidence loss was not reported.");
    Assert(retention["evidenceRecordsDropped"]!.Value<long>() > 0, "Evidence drop counters were not updated.");
    Assert(retention["evidenceRecordsRetained"]!.Value<long>()
            == retention["criticalEvidenceRecordsRetained"]!.Value<long>()
            + retention["progressionEvidenceRecordsRetained"]!.Value<long>()
            + retention["baselineValuesRetained"]!.Value<long>(),
        "Aggregate retained evidence count is inconsistent with its component counters.");
    Assert(retention["evidenceRecordsDropped"]!.Value<long>()
            == retention["criticalEvidenceRecordsDropped"]!.Value<long>()
            + retention["progressionEvidenceRecordsDropped"]!.Value<long>()
            + retention["baselineValuesDropped"]!.Value<long>(),
        "Aggregate dropped evidence count is inconsistent with its component counters.");
    Assert(retention["totalObservationsPublished"]!.Value<long>()
            == retention["timelineObservationsRetained"]!.Value<long>()
            + retention["timelineObservationsDropped"]!.Value<long>(),
        "Timeline retention counters do not reconcile with total published observations.");
    Assert(retention["evidenceLossMessage"]!.Value<string>()!.Contains("could not be retained", StringComparison.Ordinal),
        "Evidence loss did not include an explicit diagnostic message.");
    await coordinator.StopAsync();
}

static async Task CriticalUnlockEviction()
{
    var observations = Enumerable.Range(1, LiveCaptureCoordinator.MaximumCriticalEvidenceRecords + 1)
        .Select(value => UnlockObservation(value.ToString(), $"Unlock {value}", "2026-10-14T15:13:11Z"))
        .ToList();
    using var coordinator = new LiveCaptureCoordinator([new EvidenceProvider(observations)]);
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(item =>
        item.AchievementId == (LiveCaptureCoordinator.MaximumCriticalEvidenceRecords + 1).ToString()));

    var export = JObject.Parse(coordinator.ExportSessionJson());
    var retention = export["retention"]!;
    Assert(!export["criticalEvidence"]!.Any(item => item!["achievementId"]?.Value<string>() == "1"),
        "The test did not force an unlock eviction.");
    Assert(retention["highPriorityEvidenceRecordsDropped"]!.Value<long>() == 1,
        "Evicted unlock evidence was not counted as high-priority loss.");
    Assert(retention["criticalEvidenceLossOccurred"]!.Value<bool>(),
        "Evicted unlock evidence did not raise the explicit critical-loss flag.");
    Assert(retention["evidenceLossMessage"]!.Value<string>()!.Contains("High-priority", StringComparison.Ordinal),
        "Evicted unlock evidence did not produce a clear loss warning.");
    await coordinator.StopAsync();
}

static async Task VersionOneExportCompatibility()
{
    using var coordinator = new LiveCaptureCoordinator([new EvidenceProvider([
        ProgressObservation("1", "Compatible", "requirement-1", "0", "1", "10")
    ])]);
    await coordinator.StartAsync(Title(), TimeSpan.FromSeconds(10));
    await WaitUntil(() => coordinator.CurrentSession!.Observations.Any(item => item.AchievementId == "1"));

    var export = JObject.Parse(coordinator.ExportSessionJson());
    Assert(export["schemaVersion"]!.Value<int>() == 2, "Enhanced export did not identify schema version 2.");
    foreach (var legacyField in new[]
             {
                 "sessionId", "titleId", "displayName", "startedAtUtc", "endedAtUtc", "status",
                 "statusMessage", "processStatus", "lastSuccessfulApiPollUtc", "enabledSources",
                 "capabilities", "internalEventTelemetryCaptured", "observations"
             })
    {
        Assert(export.Property(legacyField) != null, $"Version 2 export removed legacy field '{legacyField}'.");
    }
    Assert(export["observations"]!.Any(item => item!["achievementId"]?.Value<string>() == "1"),
        "Legacy observations workflow cannot read the version 2 timeline.");
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

static LiveCaptureSnapshot Snapshot(params (string AchievementId, string AchievementName,
    string RequirementId, string? Current, string? Target)[] values)
{
    var achievements = new Dictionary<string, LiveCaptureAchievementSnapshot>(StringComparer.Ordinal);
    foreach (var group in values.GroupBy(value => value.AchievementId, StringComparer.Ordinal))
    {
        achievements[group.Key] = new LiveCaptureAchievementSnapshot
        {
            Id = group.Key,
            Name = group.First().AchievementName,
            State = "InProgress",
            Requirements = group.ToDictionary(
                value => value.RequirementId,
                value => new LiveCaptureRequirementSnapshot
                {
                    Id = value.RequirementId,
                    Current = value.Current,
                    Target = value.Target
                },
                StringComparer.Ordinal)
        };
    }
    return new LiveCaptureSnapshot { Achievements = achievements };
}

static LiveCaptureProgressChange Change(
    string requirementId,
    string? previous,
    string? current,
    string? target) => new()
{
    RequirementId = requirementId,
    PreviousValue = previous,
    NewValue = current,
    TargetValue = target,
    Delta = decimal.TryParse(previous, out var before) && decimal.TryParse(current, out var after)
        ? after - before
        : null
};

static LiveCaptureObservation ProgressObservation(
    string achievementId,
    string achievementName,
    string requirementId,
    string? previous,
    string? current,
    string? target) => new()
{
    Source = XboxAchievementCaptureProvider.ProviderName,
    Category = "Progression",
    EvidenceClassification = LiveCaptureEvidence.ProgressionChange,
    Message = $"change {current}",
    AchievementId = achievementId,
    AchievementName = achievementName,
    PreviousState = "InProgress",
    NewState = "InProgress",
    ProgressionChanges = [Change(requirementId, previous, current, target)]
};

static LiveCaptureObservation UnlockObservation(
    string achievementId,
    string achievementName,
    string unlockTime) => new()
{
    Source = XboxAchievementCaptureProvider.ProviderName,
    Category = "Achievement unlocked",
    EvidenceClassification = LiveCaptureEvidence.XboxStateChange,
    Message = $"Achievement unlocked: {achievementId} - {achievementName}.",
    AchievementId = achievementId,
    AchievementName = achievementName,
    PreviousState = "InProgress",
    NewState = "Achieved",
    XboxReportedUnlockTime = unlockTime
};

static JToken ProgressionEvidence(JObject export, string achievementId, string requirementId) =>
    export["progressionEvidence"]!.Single(item =>
        item!["achievementId"]!.Value<string>() == achievementId
        && item["requirementId"]!.Value<string>() == requirementId);

static void AssertProgression(
    JObject export,
    string achievementId,
    string requirementId,
    string baseline,
    string latest)
{
    var evidence = ProgressionEvidence(export, achievementId, requirementId);
    Assert(evidence["originalBaselineValue"]!.Value<string>() == baseline,
        $"Achievement {achievementId} baseline was not reconstructable.");
    Assert(evidence["latestValue"]!.Value<string>() == latest,
        $"Achievement {achievementId} latest value was not reconstructable.");
}

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
    public int CallCount => Volatile.Read(ref _index);

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

sealed class EvidenceProvider(
    IReadOnlyList<LiveCaptureObservation> observations,
    LiveCaptureSnapshot? baseline = null,
    string? baselineMessage = null) : ILiveCaptureProvider
{
    public string Name => "Evidence test provider";

    public CaptureSourceCapability GetCapability(LiveCaptureTitle title) => new()
    {
        Source = Name,
        Available = true,
        Description = "Evidence retention test provider"
    };

    public async Task RunAsync(LiveCaptureProviderContext context, CancellationToken cancellationToken)
    {
        if (baseline != null)
        {
            context.RecordAchievementBaseline(baseline);
            await context.PublishAsync(new LiveCaptureObservation
            {
                Source = XboxAchievementCaptureProvider.ProviderName,
                Category = "Baseline",
                EvidenceClassification = LiveCaptureEvidence.MonitoringStatus,
                Message = baselineMessage ?? "Achievement baseline captured."
            });
        }

        foreach (var observation in observations)
            await context.PublishAsync(observation);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }
}

sealed class RestartEvidenceProvider : ILiveCaptureProvider
{
    public string Name => "Restart evidence test provider";

    public CaptureSourceCapability GetCapability(LiveCaptureTitle title) => new()
    {
        Source = Name,
        Available = true,
        Description = "Restart evidence test provider"
    };

    public async Task RunAsync(LiveCaptureProviderContext context, CancellationToken cancellationToken)
    {
        context.RecordAchievementBaseline(SnapshotFor("5"));
        await context.PublishAsync(Progress("5", "6", "pre-restart change"));

        context.MarkProcessRestarted();
        await context.PublishAsync(new LiveCaptureObservation
        {
            Source = GameProcessCaptureProvider.ProviderName,
            Category = "Process restart",
            EvidenceClassification = LiveCaptureEvidence.MonitoringStatus,
            Message = "Verified process restarted."
        });
        context.RecordAchievementBaseline(SnapshotFor("10"));
        await context.PublishAsync(Progress("10", "11", "post-restart change"));
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    private static LiveCaptureSnapshot SnapshotFor(string current) => new()
    {
        Achievements = new Dictionary<string, LiveCaptureAchievementSnapshot>(StringComparer.Ordinal)
        {
            ["1"] = new LiveCaptureAchievementSnapshot
            {
                Id = "1",
                Name = "Restart",
                State = "InProgress",
                Requirements = new Dictionary<string, LiveCaptureRequirementSnapshot>(StringComparer.Ordinal)
                {
                    ["requirement-1"] = new LiveCaptureRequirementSnapshot
                    {
                        Id = "requirement-1",
                        Current = current,
                        Target = "20"
                    }
                }
            }
        }
    };

    private static LiveCaptureObservation Progress(string previous, string current, string message) => new()
    {
        Source = XboxAchievementCaptureProvider.ProviderName,
        Category = "Progression",
        EvidenceClassification = LiveCaptureEvidence.ProgressionChange,
        Message = message,
        AchievementId = "1",
        AchievementName = "Restart",
        PreviousState = "InProgress",
        NewState = "InProgress",
        ProgressionChanges =
        [
            new LiveCaptureProgressChange
            {
                RequirementId = "requirement-1",
                PreviousValue = previous,
                NewValue = current,
                TargetValue = "20",
                Delta = 1
            }
        ]
    };
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
