using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

public class EventsTokenTests : IDisposable
{
    public void Dispose()
    {
        AchievementsViewModel.EventsToken = null;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("x:XBL3.0 x=short")]
    [InlineData("XBL3.0 x=1234567890;missing-the-x-prefix-entirely")]
    public void IsEventsTokenValid_ReturnsFalse_ForInvalidTokens(string? token)
    {
        AchievementsViewModel.EventsToken = token;

        Assert.False(HomeViewModel.IsEventsTokenValid());
    }

    [Fact]
    public void IsEventsTokenValid_ReturnsTrue_ForWellFormedToken()
    {
        AchievementsViewModel.EventsToken = "x:XBL3.0 x=1234567890;" + new string('a', 40);

        Assert.True(HomeViewModel.IsEventsTokenValid());
    }

    [Fact]
    public void IsEventsTokenExpired_ReturnsFalse_WhenNoTimestampRecorded()
    {
        Assert.False(HomeViewModel.IsEventsTokenExpired());
    }
}
