using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using XAU.Util.Diagnostics;
using Xunit;

namespace XAU.Tests;

public class XboxServiceProbeTests
{
    [Fact]
    public async Task UnresponsiveEndpoints_AreBoundedByPerProbeTimeout()
    {
        var api = new XboxRestAPI("");
        var client = new HttpClient(new HangingHandler());
        typeof(XboxRestAPI).GetField("_probeClient", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(api, client);
        api.ProbeTimeout = TimeSpan.FromMilliseconds(15);

        var watch = Stopwatch.StartNew();
        var report = await api.CheckServiceHealthAsync();

        Assert.Equal(5, report.Results.Count);
        Assert.All(report.Results, result => Assert.Equal(XblServiceVerdict.Unreachable, result.Verdict));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(3));
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("No response expected");
        }
    }
}
