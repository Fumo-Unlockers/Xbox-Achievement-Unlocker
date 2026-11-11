using System;
using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

public class EventsRefreshRegressionTests
{
    [Fact]
    public void FailedCapture_DoesNotRenewExpiredCachedToken()
    {
        string? cached = "x:XBL3.0 x=1;synthetic-old-token";
        var obtainedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = obtainedAt.AddDays(2);

        Assert.False(HomeViewModel.TryApplyCapturedEventsToken(null, ref cached, ref obtainedAt, now));
        Assert.Equal("x:XBL3.0 x=1;synthetic-old-token", cached);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), obtainedAt);
    }

    [Fact]
    public void SuccessfulCapture_ReplacesTokenAndTimestamp()
    {
        string? cached = "x:XBL3.0 x=1;synthetic-old-token";
        var obtainedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = obtainedAt.AddDays(2);

        Assert.True(HomeViewModel.TryApplyCapturedEventsToken("x:XBL3.0 x=1;synthetic-new-token",
            ref cached, ref obtainedAt, now));
        Assert.Equal("x:XBL3.0 x=1;synthetic-new-token", cached);
        Assert.Equal(now, obtainedAt);
    }
}
