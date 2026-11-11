using XAU.Util.Diagnostics;
using Xunit;

namespace XAU.Tests;

/// <summary>
/// Tests for the Xbox Live service-health classifier used by the new "Xbox Status" checker.
///
/// Why this exists: the spoofer's "0h play-time" / "Unknown" profile could be (a) a genuine
/// Microsoft outage, (b) a play-time reset, or (c) our own token/request bug. The classifier's
/// whole job is to NOT mislabel case (c) as an outage: a 4xx (our bad token/request) still proves
/// Xbox is awake, so only a 5xx or a total failure-to-reach counts as "looks like an outage".
/// </summary>
public class XboxStatusTests
{
    private static XblServiceProbeResult Probe(string svc, bool reached, int code)
        => new(svc, "https://" + svc + "/", reached, code, XblServiceHealth.Classify(reached, code), null);

    #region Classify

    [Theory]
    [InlineData(200, XblServiceVerdict.Operational)]
    [InlineData(204, XblServiceVerdict.Operational)]
    [InlineData(429, XblServiceVerdict.RateLimited)]
    [InlineData(400, XblServiceVerdict.RespondingClientError)] // the BadRequest our token test actually saw
    [InlineData(401, XblServiceVerdict.RespondingClientError)] // expired/out-of-scope token
    [InlineData(403, XblServiceVerdict.RespondingClientError)]
    [InlineData(404, XblServiceVerdict.RespondingClientError)]
    [InlineData(500, XblServiceVerdict.ServerError)]
    [InlineData(502, XblServiceVerdict.ServerError)]
    [InlineData(503, XblServiceVerdict.ServerError)] // the classic Xbox Live "service down"
    public void Classify_MapsStatusCodeToVerdict(int code, XblServiceVerdict expected)
        => Assert.Equal(expected, XblServiceHealth.Classify(reached: true, code));

    [Theory]
    // Even a 5xx/4xx code is meaningless if we never got an HTTP response: "not reached" dominates.
    [InlineData(0, XblServiceVerdict.Unreachable)]
    [InlineData(503, XblServiceVerdict.Unreachable)]
    [InlineData(200, XblServiceVerdict.Unreachable)]
    public void Classify_NotReached_IsAlwaysUnreachable(int code, XblServiceVerdict _)
        => Assert.Equal(XblServiceVerdict.Unreachable, XblServiceHealth.Classify(reached: false, code));

    [Fact]
    // Central guarantee: a 4xx (our fault) must NOT be reported as a service outage.
    public void Classify_ClientErrorIsNotServerOutage()
    {
        Assert.NotEqual(XblServiceVerdict.ServerError, XblServiceHealth.Classify(true, 401));
        Assert.NotEqual(XblServiceVerdict.ServerError, XblServiceHealth.Classify(true, 400));
        Assert.False(XblServiceHealth.LooksLikeServiceOutage(new[] { Probe("userstats", true, 403) }));
    }

    #endregion

    #region LooksLikeServiceOutage

    [Fact]
    public void AllOperational_IsNotOutage()
    {
        var results = new[]
        {
            Probe("profile", true, 200),
            Probe("achievements", true, 200),
            Probe("userstats", true, 403), // token issue -> still not an outage
        };
        Assert.False(XblServiceHealth.LooksLikeServiceOutage(results));
    }

    [Fact]
    public void SingleServerError_IsOutage()
    {
        var results = new[]
        {
            Probe("profile", true, 200),
            Probe("userstats", true, 503), // stats service down => why every game reads 0h
        };
        Assert.True(XblServiceHealth.LooksLikeServiceOutage(results));
    }

    [Fact]
    public void SingleUnreachable_IsOutage()
    {
        var results = new[]
        {
            Probe("profile", true, 200),
            Probe("titlehub", false, 0),
        };
        Assert.True(XblServiceHealth.LooksLikeServiceOutage(results));
    }

    [Fact]
    public void Empty_IsNotOutage()
        => Assert.False(XblServiceHealth.LooksLikeServiceOutage(Array.Empty<XblServiceProbeResult>()));

    [Fact]
    public void Null_IsNotOutage()
        => Assert.False(XblServiceHealth.LooksLikeServiceOutage(null!));

    #endregion

    #region Summarize

    [Fact]
    public void Summarize_Healthy_MentionsHealthyAndListsServices()
    {
        var s = XblServiceHealth.Summarize(new[]
        {
            Probe("profile", true, 200),
            Probe("userstats", true, 200),
        });
        Assert.StartsWith("Xbox Status: healthy", s);
        Assert.Contains("profile=operational", s);
        Assert.Contains("userstats=operational", s);
    }

    [Fact]
    public void Summarize_Outage_FlagsPossibleOutageAndNamesDownService()
    {
        var s = XblServiceHealth.Summarize(new[]
        {
            Probe("profile", true, 200),
            Probe("userstats", true, 503),
        });
        Assert.Contains("possible OUTAGE", s);
        Assert.Contains("userstats=SERVER ERROR", s);
        // A healthy peer is still shown, so the user sees it is a *partial* outage.
        Assert.Contains("profile=operational", s);
    }

    [Fact]
    public void Summarize_Empty_SaysNothingChecked()
        => Assert.Equal("Xbox Status: no endpoints checked",
            XblServiceHealth.Summarize(Array.Empty<XblServiceProbeResult>()));

    [Fact]
    public void Summarize_Null_SaysNothingChecked()
        => Assert.Equal("Xbox Status: no endpoints checked", XblServiceHealth.Summarize(null!));

    #endregion

    #region User-facing renderer (healthy wording + list lines)

    [Theory]
    // A 4xx proves reachability, not that an authenticated operation will succeed.
    [InlineData(XblServiceVerdict.Operational, "healthy")]
    [InlineData(XblServiceVerdict.RespondingClientError, "responding")]
    [InlineData(XblServiceVerdict.RateLimited, "busy")]
    [InlineData(XblServiceVerdict.ServerError, "DOWN")]
    [InlineData(XblServiceVerdict.Unreachable, "unreachable")]
    public void UserStatus_UsesFriendlyWords(XblServiceVerdict verdict, string expected)
        => Assert.Equal(expected, XblServiceHealth.UserStatus(verdict));

    [Fact]
    // Regression for the complaint: the word "rejected" must never appear in the user-facing output.
    public void UserStatus_NeverSaysRejected()
    {
        foreach (XblServiceVerdict v in System.Enum.GetValues(typeof(XblServiceVerdict)))
            Assert.False(XblServiceHealth.UserStatus(v).Contains("reject", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StatusLines_AreShortNameDashStatus()
    {
        var lines = XblServiceHealth.StatusLines(new[]
        {
            Probe("profile", true, 200),
            Probe("titlehub", true, 404), // would have shown "(rejected request)" before
            Probe("userstats", true, 503),
        });
        Assert.Equal(3, lines.Count);
        Assert.Equal("profile - healthy", lines[0]);
        Assert.Equal("titlehub - responding", lines[1]); // reachability, not authenticated health
        Assert.Equal("userstats - DOWN", lines[2]);
    }

    [Fact]
    public void StatusLines_Empty_IsEmptyList()
        => Assert.Empty(XblServiceHealth.StatusLines(System.Array.Empty<XblServiceProbeResult>()));

    [Fact]
    public void RateLimitedEndpoints_AreBusyRatherThanHealthy()
    {
        var results = new[] { Probe("presence", true, 429) };
        Assert.Equal("busy", XblServiceHealth.OverallStatus(results));
        Assert.Contains("Xbox Status: busy", XblServiceHealth.Summarize(results));
    }

    [Fact]
    public void UnauthorizedProbe_ProvesOnlyThatTheEndpointResponded()
    {
        var results = new[] { Probe("presence", true, 403) };
        Assert.Equal("responding", XblServiceHealth.OverallStatus(results));
    }

    [Theory]
    [InlineData(true, "possible outage")]
    [InlineData(false, "healthy")]
    public void OverallStatus_OneWordVerdict(bool addOutage, string expected)
    {
        var results = new System.Collections.Generic.List<XblServiceProbeResult> { Probe("profile", true, 200) };
        if (addOutage)
            results.Add(Probe("userstats", true, 503));
        Assert.Equal(expected, XblServiceHealth.OverallStatus(results));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OverallStatus_NoData_IsNotChecked(bool useNull)
    {
        var result = useNull ? null : System.Array.Empty<XblServiceProbeResult>();
        Assert.Equal("not checked", XblServiceHealth.OverallStatus(result!));
    }

    #endregion
}
