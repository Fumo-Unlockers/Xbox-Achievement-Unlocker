using System.Net;
using System.Net.Http;
using System.Reflection;
using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

public class XboxHeartbeatTransportTests
{
    [Fact]
    public async Task CancelledSpoof_CancelsInFlightHeartbeatTransport()
    {
        HomeViewModel.XAUTH = "";
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new HttpClient(new BlockingHandler(async (_, token) =>
        {
            entered.SetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            catch (OperationCanceledException)
            {
                cancelled.SetResult();
                throw;
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var api = new XboxRestAPI("");
        typeof(XboxRestAPI).GetField("_spooferClient", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(api, client);
        using var cts = new CancellationTokenSource();

        var request = api.SendHeartbeatAsync("synthetic-xuid", "111", cts.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await request);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }

    private sealed class BlockingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
