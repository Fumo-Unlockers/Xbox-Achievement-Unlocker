namespace XAU.Util.Diagnostics;

/// <summary>
/// Waits for a new authenticated Xbox token before a spoof retries a rejected heartbeat.
/// OAuth recovery is awaitable; memory-scan recovery starts asynchronously and can return
/// false before the scan adopts and validates its replacement token.
/// </summary>
internal static class SpoofAuthRecovery
{
    internal static async Task<bool> WaitForFreshTokenAsync(
        Func<Task<bool>> recover,
        Func<bool> isFresh,
        TimeSpan timeout,
        CancellationToken stopToken,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        stopToken.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
        deadline.CancelAfter(timeout);
        try
        {
            await recover().WaitAsync(deadline.Token);
            while (!isFresh())
                await (delay ?? Task.Delay)(TimeSpan.FromMilliseconds(250), deadline.Token);
            return true;
        }
        catch (OperationCanceledException) when (!stopToken.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return false;
        }
    }
}
