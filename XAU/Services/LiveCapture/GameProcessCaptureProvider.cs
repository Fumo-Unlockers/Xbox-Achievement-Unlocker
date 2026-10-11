namespace XAU.Services.LiveCapture;

public sealed class GameProcessCaptureProvider : ILiveCaptureProvider
{
    public const string ProviderName = "Game process monitor";

    private readonly IGameProcessResolver _resolver;
    private readonly TimeSpan _pollInterval;

    public GameProcessCaptureProvider(
        IGameProcessResolver resolver,
        TimeSpan? pollInterval = null)
    {
        _resolver = resolver;
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(2);
    }

    public string Name => ProviderName;

    public CaptureSourceCapability GetCapability(LiveCaptureTitle title)
    {
        var available = !string.IsNullOrWhiteSpace(title.PackageFamilyName)
            || !string.IsNullOrWhiteSpace(title.ExecutablePath);
        return new CaptureSourceCapability
        {
            Source = Name,
            Available = available,
            Description = available
                ? "Verifies running processes by package family or exact executable identity and follows PID changes."
                : "No package family or verified executable identity is available for this title.",
            RequiresGameProcess = false,
            RequiresAdministrator = false,
            CanObserveInternalEventIds = false
        };
    }

    public async Task RunAsync(LiveCaptureProviderContext context, CancellationToken cancellationToken)
    {
        if (!GetCapability(context.Title).Available)
        {
            context.SetProcessStatus("Unavailable: no verified process identity", false);
            await context.PublishAsync(new LiveCaptureObservation
            {
                Source = Name,
                Category = "Process",
                EvidenceClassification = LiveCaptureEvidence.MonitoringStatus,
                Message = "Game process monitoring is unavailable; achievement API monitoring remains active."
            });
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return;
        }

        var previous = new Dictionary<int, GameProcessIdentity>();
        var hasObservedRunningProcess = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var current = (await _resolver.FindProcessesAsync(context.Title, cancellationToken))
                    .ToDictionary(process => process.ProcessId);
                var started = current.Keys.Except(previous.Keys).Select(id => current[id]).ToList();
                var stopped = previous.Keys.Except(current.Keys).Select(id => previous[id]).ToList();

                if (current.Count == 0)
                {
                    context.SetProcessStatus("Waiting for game", false);
                    if (previous.Count > 0)
                    {
                        await context.PublishAsync(new LiveCaptureObservation
                        {
                            Source = Name,
                            Category = "Process exit",
                            EvidenceClassification = LiveCaptureEvidence.MonitoringInterrupted,
                            Message = "The verified game process exited. Waiting for the game to restart."
                        });
                    }
                }
                else
                {
                    var processDisplay = string.Join(", ", current.Values.Select(process =>
                        $"{process.ProcessName} (PID {process.ProcessId})"));
                    context.SetProcessStatus(processDisplay, true);
                    if (started.Count > 0)
                    {
                        var category = hasObservedRunningProcess ? "Process restart" : "Process detected";
                        await context.PublishAsync(new LiveCaptureObservation
                        {
                            Source = Name,
                            Category = category,
                            EvidenceClassification = LiveCaptureEvidence.ProcessObserved,
                            Message = $"Verified game process: {processDisplay}."
                        });
                        hasObservedRunningProcess = true;
                    }
                    else if (stopped.Count > 0)
                    {
                        await context.PublishAsync(new LiveCaptureObservation
                        {
                            Source = Name,
                            Category = "Process change",
                            EvidenceClassification = LiveCaptureEvidence.ProcessObserved,
                            Message = $"Verified game process set changed: {processDisplay}."
                        });
                    }
                }

                previous = current;
                await Task.Delay(_pollInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                context.SetProcessStatus("Process lookup failed; retrying", false);
                await context.PublishAsync(new LiveCaptureObservation
                {
                    Source = Name,
                    Category = "Process error",
                    EvidenceClassification = LiveCaptureEvidence.CaptureFailed,
                    Message = "Verified game process lookup failed; monitoring will retry."
                });
                await Task.Delay(_pollInterval, cancellationToken);
            }
        }
    }
}
