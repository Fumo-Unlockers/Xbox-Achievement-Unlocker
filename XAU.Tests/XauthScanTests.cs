using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

/// <summary>
/// Regression tests for the "memory scan ..." debug spam every ~1s + repeated TestXAUTH against
/// an expired/out-of-scope token.
///
/// Root cause: XauthWorker fires ~every second; in the not-logged-in branch it called GetXAUTH
/// which did a FULL address-space AoBScan, then unconditionally did `XAUTH = mostCommon;
/// XAUTHTested = false;`. When TestXAUTH had just 401'd (XAUTHTested=true), the next tick
/// re-adopted the SAME still-in-memory token, un-reset the tested flag, and re-tested it — a 1 Hz
/// scan + profile-API hammering that only stops when the Xbox app is relaunched.
///
/// Fixes pinned here:
///  - ShouldScanForXauth: throttle the scan once we hold a token (in-flight guard + interval,
///    with an immediate first scan).
///  - ShouldAdoptScannedToken: only adopt a genuinely DIFFERENT token, so a dead token settles
///    instead of re-arming the test every tick.
/// </summary>
public class XauthScanTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsPlausibleXblToken_ClassifiesScanCandidates(bool makeValid)
    {
        // A real observed token is ~2.5KB; the body charset is base64url-ish with '.' separators.
        string body = new string('A', 2400) + ". BBBB=".Replace(" ", "");
        string candidate = makeValid
            ? "XBL3.0 x=2533274805248103;" + body
            : "XBL3.0 x=2533274805248103;short body";
        Assert.Equal(makeValid, HomeViewModel.IsPlausibleXblToken(candidate));
    }

    [Fact]
    public void RejectedHeldToken_RotatesToNextCandidate_BelowBar()
    {
        // 17:41 log: valid=3 plausible candidates, freq 1 each, nothing logged in.
        // Frequency can't pick a winner; the profile API is the referee, so rotate.
        const string held = "XBL3.0 x=111;" + Pad;
        const string next = "XBL3.0 x=222;" + Pad;
        Assert.True(HomeViewModel.ShouldAdoptScannedToken(
            next, held, frequency: 1, distinctNonEmptyCandidates: 3, heldTokenRejected: true));
    }

    [Fact]
    public void LoggedInState_KeepsConfidenceBar_EvenWithCompetingCandidates()
    {
        const string held = "XBL3.0 x=111;" + Pad;
        const string next = "XBL3.0 x=222;" + Pad;
        // Once logged in, a below-bar candidate must not thrash the session.
        Assert.False(HomeViewModel.ShouldAdoptScannedToken(
            next, held, frequency: 1, distinctNonEmptyCandidates: 3, heldTokenRejected: false));
    }

    private const string Pad = "tokenbodyplaceholder-that-is-long-enough-for-plausibility-000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";

    [Fact]
    public void RejectedHeldToken_RotatesToNextCandidate_BelowBar_CapsCompetitorCount()
    {
        // Rotation is bounded: a scan full of dozens of distinct candidates is not a
        // rotate-through-them-all situation.
        const string held = "XBL3.0 x=111;" + Pad;
        const string next = "XBL3.0 x=222;" + Pad;
        Assert.False(HomeViewModel.ShouldAdoptScannedToken(
            next, held, frequency: 1, distinctNonEmptyCandidates: 12, heldTokenRejected: true));
    }

    [Fact]
    public void LoneStructurallyValidCandidate_BelowBar_IsAdoptedWhenNothingHeld()
    {
        // The 5:36 PM scan: 11 distinct garbage reads (freq 1 each) around one real token.
        // After structural filtering, the real token is the lone valid candidate and must win.
        var frequency = new Dictionary<string, int>
        {
            [GarbagePrefixedToken] = 1,
            ["XBL3.0 x=99;junk " + new string('q', 16000)] = 1,
            ["XBL3.0 x=1234567890;" + new string('t', 2400)] = 1,
        };

        var filtered = HomeViewModel.FilterPlausibleTokens(frequency);
        var best = HomeViewModel.SelectBestScannedToken(filtered, out int bestFreq);

        Assert.Equal("XBL3.0 x=1234567890;" + new string('t', 2400), best);
        Assert.True(HomeViewModel.ShouldAdoptScannedToken(
            best, string.Empty, bestFreq, HomeViewModel.CountDistinctNonEmptyTokens(filtered)));
    }

    private static readonly string GarbagePrefixedToken = "XBL3.0 x=55;" + new string('g', 16383);
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    #region Scan throttling

    [Fact]
    public void ShouldScanForXauth_FirstScan_WhenNeverScanned_ReturnsTrue()
    {
        Assert.True(HomeViewModel.ShouldScanForXauth(
            DateTime.MinValue, DateTime.UtcNow, inFlight: false));
    }

    [Fact]
    public void ShouldScanForXauth_ReturnsFalse_WhenScanInFlight()
    {
        // Even before the interval elapses, an in-flight scan must not be started twice.
        Assert.False(HomeViewModel.ShouldScanForXauth(
            DateTime.MinValue, DateTime.UtcNow, inFlight: true));
    }

    [Fact]
    public void ShouldScanForXauth_ReturnsFalse_BeforeIntervalElapsed()
    {
        var now = DateTime.UtcNow;
        var lastScan = now - TimeSpan.FromSeconds(1); // < 5s ago

        Assert.False(HomeViewModel.ShouldScanForXauth(lastScan, now, inFlight: false));
    }

    [Fact]
    public void ShouldScanForXauth_ReturnsTrue_AfterIntervalElapsed()
    {
        var now = DateTime.UtcNow;
        var lastScan = now - Interval; // exactly at the boundary

        Assert.True(HomeViewModel.ShouldScanForXauth(lastScan, now, inFlight: false));
    }

    #endregion

    #region Adopt-on-change

    [Fact]
    public void ShouldAdoptScannedToken_RejectsUnchangedToken()
    {
        // THE REGRESSION: re-finding the same (expired) token must NOT be adopted, because that
        // is what reset XAUTHTested=false and re-armed TestXAUTH every tick.
        const string token = "XBL3.0 x=1234567890;sometoken";

        Assert.False(HomeViewModel.ShouldAdoptScannedToken(token, token, frequency: 5));
    }

    [Fact]
    public void ShouldAdoptScannedToken_AcceptsChangedToken()
    {
        Assert.True(HomeViewModel.ShouldAdoptScannedToken(
            "XBL3.0 x=999;newtoken", "XBL3.0 x=1234567890;oldtoken", frequency: 5));
    }

    [Fact]
    public void ShouldAdoptScannedToken_FirstAdoption_FromEmptyCurrent()
    {
        Assert.True(HomeViewModel.ShouldAdoptScannedToken(
            "XBL3.0 x=123;tok", string.Empty, frequency: 5));
    }

    [Fact]
    public void ShouldAdoptScannedToken_LoneCandidateBelowBar_AcceptedWhenNothingHeld()
    {
        // Regression: the Xbox app can expose only 3 copies of the token (log 2026-09-30 17:11,
        // bestFreq=3, need >3). With nothing held, a unique non-empty candidate must still be
        // adopted or login never happens.
        Assert.True(HomeViewModel.ShouldAdoptScannedToken(
            "XBL3.0 x=123;sometoken", "", frequency: 3, distinctNonEmptyCandidates: 1));
        Assert.True(HomeViewModel.ShouldAdoptScannedToken(
            "XBL3.0 x=123;sometoken", "", frequency: 1, distinctNonEmptyCandidates: 1));
    }

    [Theory]
    [InlineData("XBL3.0 x=1;a", "XBL3.0 x=2;b")]
    [InlineData("", "")]
    public void ShouldAdoptScannedToken_CompetingOrEmptyCandidates_KeepConfidenceBar(
        string held, string other)
    {
        // When several distinct tokens compete (or the scan found nothing), a below-bar
        // candidate stays rejected -- a truncated/garbage read must not win.
        Assert.False(HomeViewModel.ShouldAdoptScannedToken(
            held, "", frequency: 3, distinctNonEmptyCandidates: 2));
        Assert.False(HomeViewModel.ShouldAdoptScannedToken(
            other, "", frequency: 3, distinctNonEmptyCandidates: 0));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(3, false)] // original code required frequency > 3
    [InlineData(4, true)]
    public void ShouldAdoptScannedToken_EnforcesFrequencyThreshold(int frequency, bool expected)
    {
        Assert.Equal(expected,
            HomeViewModel.ShouldAdoptScannedToken("XBL3.0 x=1;a", "XBL3.0 x=2;b", frequency));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShouldAdoptScannedToken_RejectsNullOrEmpty(string? scanned)
    {
        Assert.False(HomeViewModel.ShouldAdoptScannedToken(scanned!, "XBL3.0 x=2;b", frequency: 5));
    }

    #endregion

    #region Expired-token spin simulation

    [Fact]
    public void ExpiredTokenKeptInMemory_DoesNotReArmTestEveryTick()
    {
        // Simulate the reported spin: we hold an expired token (XAUTHTested=true after a 401) and
        // the Xbox app still exposes the SAME string in memory. Across many ticks the test must be
        // armed at most once, not once per tick.
        const string expired = "XBL3.0 x=1234567890;dead";

        string xauth = expired;
        bool xauthTested = true;   // 401 handler set this
        int testArmed = 0;

        for (int tick = 0; tick < 30; tick++)
        {
            // The poll finds the identical token still sitting in the Xbox app's memory.
            string scanned = expired;
            int frequency = 5;

            if (HomeViewModel.ShouldAdoptScannedToken(scanned, xauth, frequency))
            {
                xauth = scanned;
                xauthTested = false;
            }

            // The worker's "if (!XAUTHTested && XAUTH.Length > 0) TestXAUTH();" guard.
            if (!xauthTested && xauth.Length > 0)
            {
                testArmed++;
                xauthTested = true; // a re-401
            }
        }

        Assert.Equal(0, testArmed); // unchanged token -> test never re-armed
    }

    [Fact]
    public void NewTokenAppearing_GetsAdoptedAndTestedOnce()
    {
        // Positive path: once the Xbox app is relaunched and produces a fresh token, it IS adopted
        // and the test runs again.
        string xauth = "XBL3.0 x=1234567890;dead";
        bool xauthTested = true;
        int testArmed = 0;

        string fresh = "XBL3.0 x=999;alive";
        if (HomeViewModel.ShouldAdoptScannedToken(fresh, xauth, frequency: 5))
        {
            xauth = fresh;
            xauthTested = false;
        }

        if (!xauthTested && xauth.Length > 0)
        {
            testArmed++;
            xauthTested = true;
        }

        Assert.Equal(1, testArmed);
        Assert.Equal(fresh, xauth);
    }

    #endregion

    #region Re-test cadence (regression: "not logging in on fresh launch")

    [Theory]
    [InlineData(true, false)]   // in flight -> no
    [InlineData(false, true)]   // never tested -> yes
    public void ShouldTestXauth_InFlightAndFirstAttempt(bool inFlight, bool expected)
    {
        Assert.Equal(expected, HomeViewModel.ShouldTestXauth(
            inFlight, DateTime.MinValue, DateTime.UtcNow, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void ShouldTestXauth_Blocked_BeforeIntervalElapsed()
    {
        var now = DateTime.UtcNow;
        var last = now - TimeSpan.FromSeconds(1);
        Assert.False(HomeViewModel.ShouldTestXauth(false, last, now, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void ShouldTestXauth_Allowed_AfterIntervalElapsed()
    {
        var now = DateTime.UtcNow;
        var last = now - TimeSpan.FromSeconds(5);
        Assert.True(HomeViewModel.ShouldTestXauth(false, last, now, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void TransientFirstTestFailure_StillLogsInOnceTokenIsReady_UnchangedToken()
    {
        // Reproduces the reported "not logging in on fresh launch": the held token does NOT change, so
        // adoption never re-arms; the gate must therefore be re-test cadence (not XAUTHTested) so that a
        // token that simply wasn't ready on the first attempt still logs in on a later bounded attempt.
        const string token = "XBL3.0 x=1234567890;tok"; // present from the start, becomes valid at t=6s

        string xauth = token;
        bool loggedIn = false;
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromSeconds(5);
        var lastTest = DateTime.MinValue;
        bool inFlight = false;
        int attempts = 0;

        for (int t = 0; t <= 10; t++)
        {
            var now = start + TimeSpan.FromSeconds(t);
            if (xauth.Length > 0 && HomeViewModel.ShouldTestXauth(inFlight, lastTest, now, interval))
            {
                inFlight = true;
                lastTest = now;
                attempts++;

                // The API only accepts the token from the 2nd bounded attempt onward (transient "not ready
                // yet" on the very first attempt right after launch).
                loggedIn = t >= 5;
                inFlight = false;
            }

            if (loggedIn)
                break;
        }

        Assert.True(loggedIn);
        Assert.Equal(2, attempts); // one failed attempt at t=0, one success at t=5 (bounded, not once-per-tick)
    }

    [Fact]
    public void HeldButAlwaysInvalidToken_IsTestedAtBoundedRate_NotOncePerTick()
    {
        // A genuinely dead token must still be re-tested (so recovery works when it becomes valid) but at
        // the bounded cadence, NOT every tick -- guarding against re-introducing the 1 Hz API hammering.
        string xauth = "XBL3.0 x=1234567890;dead";
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromSeconds(5);
        var lastTest = DateTime.MinValue;
        bool inFlight = false;
        int attempts = 0;

        for (int t = 0; t < 60; t++) // 60 one-second ticks
        {
            var now = start + TimeSpan.FromSeconds(t);
            if (xauth.Length > 0 && HomeViewModel.ShouldTestXauth(inFlight, lastTest, now, interval))
            {
                inFlight = true;
                lastTest = now;
                attempts++;
                inFlight = false; // test fails (still not logged in)
            }
        }

        // ~one per 5s over 60s -> ~12 attempts, definitively far fewer than 60 (no per-tick hammering).
        Assert.InRange(attempts, 10, 13);
    }

    #endregion

    #region Best-candidate selection (regression: empty string outranks the real token -> can't log in)

    private const string Valid = "XBL3.0 x=1234567890;alive";

    [Fact]
    public void SelectBestScannedToken_IgnoresEmpty_AndPicksMostFrequentNonEmpty()
    {
        // The exact reported shape: many unreadable addresses read back as "" (highest count), the real
        // token appears less often. The old top-1 pick chose "" and blocked adoption.
        var frequency = new Dictionary<string, int>
        {
            [""] = 7,
            [Valid] = 4,
            ["XBL3.0 x=2;b"] = 2,
        };

        string best = HomeViewModel.SelectBestScannedToken(frequency, out int bestFreq);

        Assert.Equal(Valid, best);
        Assert.Equal(4, bestFreq);
    }

    [Fact]
    public void SelectBestScannedToken_EmptyTopCandidate_StillAdoptableByShouldAdopt()
    {
        // End-to-end style assertion: with an empty string topping the frequency table, the real token is
        // now the one handed to ShouldAdoptScannedToken and IS adopted (was the login blocker).
        var frequency = new Dictionary<string, int> { [""] = 10, [Valid] = 4 };

        string best = HomeViewModel.SelectBestScannedToken(frequency, out int bestFreq);

        Assert.Equal(Valid, best);
        Assert.True(HomeViewModel.ShouldAdoptScannedToken(best, string.Empty, bestFreq));
    }

    [Fact]
    public void SelectBestScannedToken_AllEmptyOrNull_ReturnsEmptyZero()
    {
        var frequency = new Dictionary<string, int> { [""] = 5, ["   "] = 3 };

        Assert.Equal("", HomeViewModel.SelectBestScannedToken(frequency, out int f1));
        Assert.Equal(0, f1);

        Assert.Equal("", HomeViewModel.SelectBestScannedToken(null!, out int f2));
        Assert.Equal(0, f2);
    }

    [Fact]
    public void SelectBestScannedToken_Tie_BrokenByLongerString()
    {
        var frequency = new Dictionary<string, int> { ["short"] = 3, ["longertoken"] = 3 };

        string best = HomeViewModel.SelectBestScannedToken(frequency, out int bestFreq);

        Assert.Equal("longertoken", best);
        Assert.Equal(3, bestFreq);
    }

    #endregion

    #region Scan read-length tunable (default 16384; floor guards against truncation-into-400)

    [Theory]
    // The hazard this whole tunable must defend against: a length BELOW the real token size truncates a
    // valid token, so TestXAUTH reads a malformed string and gets a 400 -- which looks like an invalid
    // token but is really a read bug. The floor must therefore stay above any genuine token, and any
    // user-typed value below it is clamped up rather than honoured.
    [InlineData(0, HomeViewModel.MinScanReadLength)]
    [InlineData(-5, HomeViewModel.MinScanReadLength)]
    [InlineData(1024, HomeViewModel.MinScanReadLength)]
    [InlineData(2000, HomeViewModel.MinScanReadLength)]
    public void NormalizeScanReadLength_ClampsTooLowUpToFloor(int input, int expected)
    {
        Assert.Equal(expected, HomeViewModel.NormalizeScanReadLength(input));
    }

    [Fact]
    public void NormalizeScanReadLength_ClampsTooHighUpToCeiling()
    {
        Assert.Equal(HomeViewModel.MaxScanReadLength, HomeViewModel.NormalizeScanReadLength(9_000_000));
    }

    [Fact]
    public void NormalizeScanReadLength_PreservesInBandValue()
    {
        Assert.Equal(HomeViewModel.DefaultScanReadLength,
            HomeViewModel.NormalizeScanReadLength(HomeViewModel.DefaultScanReadLength));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(HomeViewModel.MinScanReadLength - 1, false)]
    [InlineData(HomeViewModel.MinScanReadLength, true)]
    [InlineData(HomeViewModel.DefaultScanReadLength, true)]
    [InlineData(HomeViewModel.MaxScanReadLength, true)]
    [InlineData(HomeViewModel.MaxScanReadLength + 1, false)]
    public void ShouldAcceptScanReadLength_EnforcesInclusiveBand(int candidate, bool expected)
    {
        Assert.Equal(expected, HomeViewModel.ShouldAcceptScanReadLength(candidate));
    }

    [Fact]
    // The floor is only a real safety floor if it clears the largest token actually seen. The one
    // genuine token observed in the field was ~2581 chars; the default/floor must dominate it.
    public void FloorAndDefault_ClearTheLargestObservedToken()
    {
        int largestObservedToken = 2581; // from the live log: "[XAUTHDBG] TestXAUTH: verifying token (len=2581)"
        Assert.True(HomeViewModel.MinScanReadLength > largestObservedToken);
        Assert.True(HomeViewModel.DefaultScanReadLength > largestObservedToken);
    }

    #endregion
}
