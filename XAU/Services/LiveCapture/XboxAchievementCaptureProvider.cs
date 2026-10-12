using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using XAU.ViewModels.Pages;

namespace XAU.Services.LiveCapture;

public sealed class XboxAchievementSnapshotSource : IAchievementSnapshotSource
{
    private readonly object _sync = new();
    private XboxRestAPI? _api;
    private string? _apiToken;

    public bool HasAuthentication =>
        !string.IsNullOrWhiteSpace(HomeViewModel.XAUTH)
        && !string.IsNullOrWhiteSpace(HomeViewModel.XUIDOnly);

    public Task<AchievementsResponse?> GetAchievementsAsync(
        string titleId,
        CancellationToken cancellationToken)
    {
        var token = HomeViewModel.XAUTH;
        XboxRestAPI api;
        lock (_sync)
        {
            if (_api == null || !string.Equals(_apiToken, token, StringComparison.Ordinal))
            {
                _api = new XboxRestAPI(token);
                _apiToken = token;
            }
            api = _api;
        }
        return api.GetAchievementsForTitleAsync(
            HomeViewModel.XUIDOnly,
            titleId,
            cancellationToken);
    }
}

public sealed class XboxAchievementCaptureProvider : ILiveCaptureProvider
{
    public const string ProviderName = "Xbox achievement API";

    private readonly IAchievementSnapshotSource _source;
    private readonly TimeSpan _minimumRetryDelay;

    public XboxAchievementCaptureProvider(
        IAchievementSnapshotSource source,
        TimeSpan? minimumRetryDelay = null)
    {
        _source = source;
        _minimumRetryDelay = minimumRetryDelay ?? TimeSpan.FromSeconds(5);
    }

    public string Name => ProviderName;

    public CaptureSourceCapability GetCapability(LiveCaptureTitle title) => new()
    {
        Source = Name,
        Available = true,
        Description = "Observes Xbox achievement state, reported unlock time, and available requirement progress.",
        RequiresGameProcess = false,
        RequiresAdministrator = false,
        CanObserveInternalEventIds = false
    };

    public async Task RunAsync(LiveCaptureProviderContext context, CancellationToken cancellationToken)
    {
        if (context.WaitForVerifiedProcess)
            await context.WaitForVerifiedProcessAsync(cancellationToken);

        LiveCaptureSnapshot? previous = null;
        var consecutiveFailures = 0;
        var lastFailure = "";
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_source.HasAuthentication)
            {
                await context.PublishAsync(new LiveCaptureObservation
                {
                    Source = Name,
                    Category = "Authentication",
                    EvidenceClassification = LiveCaptureEvidence.MonitoringInterrupted,
                    Message = "Xbox authentication is unavailable or expired. Live capture stopped."
                });
                context.RequestStop("Xbox authentication is unavailable or expired.");
                return;
            }

            try
            {
                var response = await _source.GetAchievementsAsync(context.Title.TitleId, cancellationToken);
                if (response == null)
                    throw new InvalidDataException("The achievement response was empty.");
                if (response.achievements == null)
                    throw new InvalidDataException("The achievement response did not contain an achievements collection.");

                var observedAt = DateTimeOffset.UtcNow;
                var current = LiveCaptureSnapshotComparer.Create(response);
                context.SetLastSuccessfulPoll(observedAt);
                if (consecutiveFailures > 0)
                {
                    await context.PublishAsync(new LiveCaptureObservation
                    {
                        ObservedAtUtc = observedAt,
                        Source = Name,
                        Category = "API recovery",
                        EvidenceClassification = LiveCaptureEvidence.MonitoringStatus,
                        Message = "Xbox achievement monitoring recovered after a transient interruption."
                    });
                }

                if (previous == null)
                {
                    await context.PublishAsync(new LiveCaptureObservation
                    {
                        ObservedAtUtc = observedAt,
                        Source = Name,
                        Category = "Baseline",
                        EvidenceClassification = LiveCaptureEvidence.MonitoringStatus,
                        Message = $"Achievement baseline captured ({current.Achievements.Count} achievements)."
                    });
                }
                else
                {
                    foreach (var observation in LiveCaptureSnapshotComparer.Compare(previous, current, observedAt))
                        await context.PublishAsync(observation);
                }

                previous = current;
                consecutiveFailures = 0;
                lastFailure = "";
                await Task.Delay(context.AchievementPollInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (
                exception is HttpRequestException or JsonException or InvalidDataException or OperationCanceledException)
            {
                consecutiveFailures++;
                var statusCode = (exception as HttpRequestException)?.StatusCode;
                var authenticationFailure = statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
                var failure = statusCode.HasValue
                    ? $"HTTP {(int)statusCode.Value} {statusCode.Value}"
                    : exception.GetType().Name;
                var delay = TimeSpan.FromSeconds(Math.Min(
                    60,
                    Math.Max(context.AchievementPollInterval.TotalSeconds, _minimumRetryDelay.TotalSeconds)
                    * Math.Pow(2, Math.Min(consecutiveFailures - 1, 3))));

                if (!string.Equals(lastFailure, failure, StringComparison.Ordinal))
                {
                    await context.PublishAsync(new LiveCaptureObservation
                    {
                        Source = Name,
                        Category = authenticationFailure ? "Authentication" : "API interruption",
                        EvidenceClassification = authenticationFailure
                            ? LiveCaptureEvidence.MonitoringInterrupted
                            : LiveCaptureEvidence.CaptureFailed,
                        Message = authenticationFailure
                            ? $"Xbox achievement monitoring stopped ({failure})."
                            : $"Xbox achievement poll failed ({failure}); retrying in {delay.TotalSeconds:0} seconds."
                    });
                    lastFailure = failure;
                }

                if (authenticationFailure)
                {
                    context.RequestStop("Xbox authentication was rejected.");
                    return;
                }

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
