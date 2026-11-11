using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

public class XboxRestApiHeaderTests : IDisposable
{
    public XboxRestApiHeaderTests()
    {
        HomeViewModel.XAUTH = "";
    }

    public void Dispose()
    {
        HomeViewModel.XAUTH = "";
    }

    [Fact]
    public void SetDefaultHeaders_WithEmptyToken_DoesNotThrow()
    {
        var api = new XboxRestAPI("");

        api.SetDefaultHeaders();

        Assert.False(api._httpClient.DefaultRequestHeaders.Contains(HeaderNames.Authorization));
    }

    [Fact]
    public void SetDefaultSpooferHeaders_WithEmptyToken_DoesNotThrow()
    {
        var api = new XboxRestAPI("");

        api.SetDefaultSpooferHeaders();

        Assert.False(api._spooferClient.DefaultRequestHeaders.Contains(HeaderNames.Authorization));
    }

    [Fact]
    public void SetDefaultEventBasedHeaders_WithEmptyToken_DoesNotThrow()
    {
        var api = new XboxRestAPI("");

        api.SetDefaultEventBasedHeaders();

        Assert.False(api._eventBasedClient.DefaultRequestHeaders.Contains("authxtoken"));
    }

    [Fact]
    public void SetDefaultHeaders_WithToken_AddsAuthorization()
    {
        HomeViewModel.XAUTH = "XBL3.0 x=123;sometoken";
        var api = new XboxRestAPI(HomeViewModel.XAUTH);

        api.SetDefaultHeaders();

        Assert.Equal("XBL3.0 x=123;sometoken",
            api._httpClient.DefaultRequestHeaders.GetValues(HeaderNames.Authorization).Single());
    }

    [Fact]
    public void ExistingClient_DropsAuthorization_WhenSessionIsCleared()
    {
        const string synthetic = "XBL3.0 x=1;synthetic-not-a-token";
        HomeViewModel.XAUTH = synthetic;
        var api = new XboxRestAPI(synthetic);
        api.SetDefaultSpooferHeaders();
        Assert.True(api._spooferClient.DefaultRequestHeaders.Contains(HeaderNames.Authorization));

        HomeViewModel.XAUTH = "";
        api.SetDefaultSpooferHeaders();

        Assert.False(api._spooferClient.DefaultRequestHeaders.Contains(HeaderNames.Authorization));
    }

    [Fact]
    public void SetDefaultHeaders_UsesLiveToken_WhenLoginCompletesAfterConstruction()
    {
        var api = new XboxRestAPI(HomeViewModel.XAUTH);
        HomeViewModel.XAUTH = "XBL3.0 x=456;freshtoken";

        api.SetDefaultHeaders();

        Assert.Equal("XBL3.0 x=456;freshtoken",
            api._httpClient.DefaultRequestHeaders.GetValues(HeaderNames.Authorization).Single());
    }

    [Fact]
    public void SetDefaultEventBasedHeaders_StripsXuidFromAuthxtoken()
    {
        HomeViewModel.XAUTH = "XBL3.0 x=1234567890;sometoken";
        var api = new XboxRestAPI(HomeViewModel.XAUTH);

        api.SetDefaultEventBasedHeaders();

        Assert.Equal("XBL3.0 x=-;sometoken",
            api._eventBasedClient.DefaultRequestHeaders.GetValues("authxtoken").Single());
    }

    [Theory]
    [InlineData(true, "en-US", "en-GB")]
    [InlineData(false, "", "en-GB")]
    [InlineData(false, null, "en-GB")]
    [InlineData(false, "de-DE", "de-DE")]
    [InlineData(false, "en-US", "en-US")]
    public void ResolveAcceptLanguage_ReturnsValidLanguage(bool regionOverride, string? cultureName, string expected)
    {
        Assert.Equal(expected, XboxRestAPI.ResolveAcceptLanguage(regionOverride, cultureName));
    }
}
