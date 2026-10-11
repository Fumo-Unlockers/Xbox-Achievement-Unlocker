namespace XAU.Services.LiveCapture;

public interface ILiveCaptureProvider
{
    string Name { get; }
    CaptureSourceCapability GetCapability(LiveCaptureTitle title);
    Task RunAsync(LiveCaptureProviderContext context, CancellationToken cancellationToken);
}

public interface IAchievementSnapshotSource
{
    bool HasAuthentication { get; }
    Task<AchievementsResponse?> GetAchievementsAsync(
        string titleId,
        CancellationToken cancellationToken);
}

public interface IGameProcessResolver
{
    ValueTask<IReadOnlyList<GameProcessIdentity>> FindProcessesAsync(
        LiveCaptureTitle title,
        CancellationToken cancellationToken);
}

public sealed class LiveCaptureProviderContext
{
    private readonly Func<LiveCaptureObservation, ValueTask> _publish;
    private readonly Action<string, bool> _setProcessStatus;
    private readonly Action<DateTimeOffset> _setLastSuccessfulPoll;
    private readonly Action<string> _requestStop;
    private readonly TaskCompletionSource _processReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal LiveCaptureProviderContext(
        LiveCaptureTitle title,
        TimeSpan achievementPollInterval,
        bool waitForVerifiedProcess,
        Func<LiveCaptureObservation, ValueTask> publish,
        Action<string, bool> setProcessStatus,
        Action<DateTimeOffset> setLastSuccessfulPoll,
        Action<string> requestStop)
    {
        Title = title;
        AchievementPollInterval = achievementPollInterval;
        WaitForVerifiedProcess = waitForVerifiedProcess;
        _publish = publish;
        _setProcessStatus = setProcessStatus;
        _setLastSuccessfulPoll = setLastSuccessfulPoll;
        _requestStop = requestStop;
        if (!waitForVerifiedProcess)
            _processReady.TrySetResult();
    }

    public LiveCaptureTitle Title { get; }
    public TimeSpan AchievementPollInterval { get; }
    public bool WaitForVerifiedProcess { get; }

    public ValueTask PublishAsync(LiveCaptureObservation observation) => _publish(observation);

    public void SetProcessStatus(string status, bool running)
    {
        _setProcessStatus(status, running);
        if (running)
            _processReady.TrySetResult();
    }

    public Task WaitForVerifiedProcessAsync(CancellationToken cancellationToken) =>
        _processReady.Task.WaitAsync(cancellationToken);

    public void SetLastSuccessfulPoll(DateTimeOffset value) => _setLastSuccessfulPoll(value);

    public void RequestStop(string reason) => _requestStop(reason);
}
