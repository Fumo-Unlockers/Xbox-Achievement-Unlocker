using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace XAU.Services.LiveCapture;

public sealed class LiveCaptureCoordinator : IHostedService, IDisposable
{
    public const int MaximumObservations = 500;

    private readonly IReadOnlyList<ILiveCaptureProvider> _providers;
    private readonly object _sync = new();
    private CancellationTokenSource? _sessionCancellation;
    private Task? _sessionTask;
    private bool _explicitStop;
    private bool _isRunning;
    private bool _disposed;

    public LiveCaptureCoordinator(IEnumerable<ILiveCaptureProvider> providers)
    {
        _providers = providers.ToList();
    }

    public LiveCaptureSession? CurrentSession { get; private set; }
    public bool IsRunning
    {
        get
        {
            lock (_sync)
                return _isRunning;
        }
    }

    public event EventHandler<LiveCaptureObservation>? ObservationAdded;
    public event EventHandler<LiveCaptureSession>? SessionUpdated;

    Task IHostedService.StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartAsync(
        LiveCaptureTitle title,
        TimeSpan achievementPollInterval,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title.TitleId))
            throw new ArgumentException("A title ID is required.", nameof(title));
        if (achievementPollInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(achievementPollInterval));

        LiveCaptureSession session;
        lock (_sync)
        {
            ThrowIfDisposed();
            if (_isRunning)
                throw new InvalidOperationException("A live capture session is already running.");

            _sessionCancellation?.Dispose();
            _sessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _explicitStop = false;
            var capabilities = _providers.Select(provider => provider.GetCapability(title)).ToList();
            capabilities.Add(new CaptureSourceCapability
            {
                Source = "Internal event telemetry",
                Available = false,
                Description = "No validated provider currently exposes internal event names or achievement IDs.",
                RequiresGameProcess = true,
                RequiresAdministrator = false,
                CanObserveInternalEventIds = false
            });
            capabilities.Add(new CaptureSourceCapability
            {
                Source = "Save-file monitoring",
                Available = false,
                Description = "No save-file provider is integrated into Live Capture for this title.",
                RequiresGameProcess = false,
                RequiresAdministrator = false,
                CanObserveInternalEventIds = false
            });

            var processAvailable = capabilities.Any(capability =>
                capability.Source == GameProcessCaptureProvider.ProviderName && capability.Available);
            session = new LiveCaptureSession
            {
                TitleId = title.TitleId,
                DisplayName = title.DisplayName,
                Status = processAvailable
                    ? LiveCaptureSessionState.WaitingForGame
                    : LiveCaptureSessionState.Monitoring,
                StatusMessage = processAvailable
                    ? "Waiting for a verified game process."
                    : "Achievement monitoring active; verified process identity is unavailable.",
                ProcessStatus = processAvailable
                    ? "Waiting for game"
                    : "Unavailable: no verified process identity",
                Capabilities = capabilities,
                EnabledSources = capabilities
                    .Where(capability => capability.Available)
                    .Select(capability => capability.Source)
                    .ToList()
            };
            CurrentSession = session;
            _isRunning = true;

            var context = new LiveCaptureProviderContext(
                title,
                achievementPollInterval,
                processAvailable,
                PublishAsync,
                SetProcessStatus,
                SetLastSuccessfulPoll,
                RequestStop);
            _sessionTask = RunSessionAsync(session, context, _sessionCancellation.Token);
        }

        NotifySessionUpdated(session);
        _ = PublishAsync(new LiveCaptureObservation
        {
            Source = "Live Capture",
            Category = "Capability",
            EvidenceClassification = LiveCaptureEvidence.NoInternalEventSource,
            Message = title.TitleId == "1144039928"
                ? "MCC internal AchievementId telemetry is not available through a validated provider."
                : "Internal achievement event telemetry is not available through a validated provider."
        });
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task? sessionTask;
        LiveCaptureSession? session;
        lock (_sync)
        {
            sessionTask = _sessionTask;
            session = CurrentSession;
            if (!_isRunning || sessionTask == null || _sessionCancellation == null)
                return;

            _explicitStop = true;
            session!.Status = LiveCaptureSessionState.Stopping;
            session.StatusMessage = "Stopping live capture...";
            _sessionCancellation.Cancel();
        }

        if (session != null)
            NotifySessionUpdated(session);

        try
        {
            await sessionTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => StopAsync(cancellationToken);

    public string ExportSessionJson()
    {
        lock (_sync)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No live capture session is available to export.");

            var export = new
            {
                schemaVersion = 1,
                sessionId = CurrentSession.SessionId,
                titleId = CurrentSession.TitleId,
                displayName = LiveCaptureSanitizer.Sanitize(CurrentSession.DisplayName),
                startedAtUtc = CurrentSession.StartedAtUtc,
                endedAtUtc = CurrentSession.EndedAtUtc,
                status = CurrentSession.Status,
                statusMessage = LiveCaptureSanitizer.Sanitize(CurrentSession.StatusMessage),
                processStatus = LiveCaptureSanitizer.Sanitize(CurrentSession.ProcessStatus),
                lastSuccessfulApiPollUtc = CurrentSession.LastSuccessfulApiPollUtc,
                enabledSources = CurrentSession.EnabledSources,
                capabilities = CurrentSession.Capabilities,
                internalEventTelemetryCaptured = CurrentSession.InternalEventTelemetryCaptured,
                observations = CurrentSession.Observations.Select(SanitizeObservation).ToList()
            };
            return JsonConvert.SerializeObject(export, Formatting.Indented, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Include
            });
        }
    }

    private async Task RunSessionAsync(
        LiveCaptureSession session,
        LiveCaptureProviderContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var activeProviders = _providers
                .Where(provider => provider.GetCapability(context.Title).Available)
                .Select(provider => RunProviderAsync(provider, context, cancellationToken))
                .ToList();
            await Task.WhenAll(activeProviders);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            lock (_sync)
            {
                session.EndedAtUtc = DateTimeOffset.UtcNow;
                _isRunning = false;
                if (_explicitStop || session.Status != LiveCaptureSessionState.CaptureError)
                {
                    session.Status = LiveCaptureSessionState.Completed;
                    session.StatusMessage = _explicitStop
                        ? "Live capture stopped by the user."
                        : "Live capture completed.";
                }
            }
            NotifySessionUpdated(session);
        }
    }

    private async Task RunProviderAsync(
        ILiveCaptureProvider provider,
        LiveCaptureProviderContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await provider.RunAsync(context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            await PublishAsync(new LiveCaptureObservation
            {
                Source = provider.Name,
                Category = "Provider error",
                EvidenceClassification = LiveCaptureEvidence.CaptureFailed,
                Message = $"{provider.Name} stopped because of an unexpected provider error."
            });
            context.RequestStop($"{provider.Name} failed.");
        }
    }

    private ValueTask PublishAsync(LiveCaptureObservation observation)
    {
        var sanitized = SanitizeObservation(observation);
        lock (_sync)
        {
            if (CurrentSession == null)
                return ValueTask.CompletedTask;
            while (CurrentSession.Observations.Count >= MaximumObservations)
                CurrentSession.Observations.RemoveAt(0);
            CurrentSession.Observations.Add(sanitized);
        }

        try
        {
            ObservationAdded?.Invoke(this, sanitized);
        }
        catch
        {
            // A UI subscriber must not terminate capture providers.
        }
        return ValueTask.CompletedTask;
    }

    private void SetProcessStatus(string status, bool running)
    {
        LiveCaptureSession? session;
        lock (_sync)
        {
            session = CurrentSession;
            if (session == null)
                return;
            session.ProcessStatus = LiveCaptureSanitizer.Sanitize(status);
            if (session.Status is LiveCaptureSessionState.Stopping or LiveCaptureSessionState.CaptureError)
                return;
            session.Status = running
                ? LiveCaptureSessionState.Monitoring
                : LiveCaptureSessionState.WaitingForGame;
            session.StatusMessage = running
                ? "Game process verified; passive monitoring is active."
                : "Waiting for a verified game process.";
        }
        NotifySessionUpdated(session);
    }

    private void SetLastSuccessfulPoll(DateTimeOffset value)
    {
        LiveCaptureSession? session;
        lock (_sync)
        {
            session = CurrentSession;
            if (session != null)
                session.LastSuccessfulApiPollUtc = value;
        }
        if (session != null)
            NotifySessionUpdated(session);
    }

    private void RequestStop(string reason)
    {
        LiveCaptureSession? session;
        lock (_sync)
        {
            session = CurrentSession;
            if (session == null || _sessionCancellation == null)
                return;
            session.Status = LiveCaptureSessionState.CaptureError;
            session.StatusMessage = LiveCaptureSanitizer.Sanitize(reason);
            _sessionCancellation.Cancel();
        }
        NotifySessionUpdated(session);
    }

    private void NotifySessionUpdated(LiveCaptureSession session)
    {
        try
        {
            SessionUpdated?.Invoke(this, session);
        }
        catch
        {
            // A UI subscriber must not terminate capture providers.
        }
    }

    private static LiveCaptureObservation SanitizeObservation(LiveCaptureObservation observation) => new()
    {
        ObservedAtUtc = observation.ObservedAtUtc,
        Source = LiveCaptureSanitizer.Sanitize(observation.Source),
        Category = LiveCaptureSanitizer.Sanitize(observation.Category),
        EvidenceClassification = LiveCaptureSanitizer.Sanitize(observation.EvidenceClassification),
        Message = LiveCaptureSanitizer.Sanitize(observation.Message),
        AchievementId = LiveCaptureSanitizer.Sanitize(observation.AchievementId),
        AchievementName = LiveCaptureSanitizer.Sanitize(observation.AchievementName),
        PreviousState = LiveCaptureSanitizer.Sanitize(observation.PreviousState),
        NewState = LiveCaptureSanitizer.Sanitize(observation.NewState),
        XboxReportedUnlockTime = LiveCaptureSanitizer.Sanitize(observation.XboxReportedUnlockTime),
        InternalEventName = LiveCaptureSanitizer.Sanitize(observation.InternalEventName),
        InternalAchievementId = LiveCaptureSanitizer.Sanitize(observation.InternalAchievementId),
        ProgressionChanges = observation.ProgressionChanges.Select(change => new LiveCaptureProgressChange
        {
            RequirementId = LiveCaptureSanitizer.Sanitize(change.RequirementId),
            PreviousValue = LiveCaptureSanitizer.Sanitize(change.PreviousValue),
            NewValue = LiveCaptureSanitizer.Sanitize(change.NewValue),
            TargetValue = LiveCaptureSanitizer.Sanitize(change.TargetValue),
            Delta = change.Delta
        }).ToList()
    };

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _sessionCancellation?.Cancel();
        _sessionCancellation?.Dispose();
    }
}
