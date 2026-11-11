using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

/// <summary>
/// Tests for the "Profile information grabbed" snackbar-spam regression.
///
/// Root cause: XauthWorker reports progress ~every second, and the progress handler called
/// the async-void GrabProfile() whenever the user was logged in and the profile had not been
/// grabbed yet. Because GrabProfile only sets GrabbedProfile=true after several awaited network
/// calls (profile + title + gamepass), every tick during that window fired another fetch, each
/// of which eventually raised its own snackbar.
///
/// Fix: a single-flight guard (ShouldStartProfileGrab + an in-flight flag). These tests encode
/// the exact decision so it cannot regress.
/// </summary>
public class ProfileGrabTests
{
    [Fact]
    public void ShouldStartProfileGrab_AutomaticFirstFetch_ReturnsTrue()
    {
        // Logged in, never grabbed, nothing running -> the one legitimate automatic fetch starts.
        Assert.True(HomeViewModel.ShouldStartProfileGrab(
            isLoggedIn: true, alreadyGrabbed: false, inFlight: false, force: false));
    }

    [Fact]
    public void ShouldStartProfileGrab_WhenNotLoggedIn_ReturnsFalse()
    {
        Assert.False(HomeViewModel.ShouldStartProfileGrab(
            isLoggedIn: false, alreadyGrabbed: false, inFlight: false, force: false));
    }

    [Fact]
    public void ShouldStartProfileGrab_AutomaticWhenAlreadyGrabbed_ReturnsFalse()
    {
        // After a successful grab the automatic path must not keep re-fetching.
        Assert.False(HomeViewModel.ShouldStartProfileGrab(
            isLoggedIn: true, alreadyGrabbed: true, inFlight: false, force: false));
    }

    [Fact]
    public void ShouldStartProfileGrab_AutomaticWhileInFlight_ReturnsFalse()
    {
        // THE REGRESSION: a tick arriving while a fetch is mid-flight must NOT start another,
        // even though alreadyGrabbed is still false (it is only set on completion).
        Assert.False(HomeViewModel.ShouldStartProfileGrab(
            isLoggedIn: true, alreadyGrabbed: false, inFlight: true, force: false));
    }

    [Fact]
    public void ShouldStartProfileGrab_ManualWhileInFlight_ReturnsFalse()
    {
        // Manual refresh cannot stack a second concurrent fetch either.
        Assert.False(HomeViewModel.ShouldStartProfileGrab(
            isLoggedIn: true, alreadyGrabbed: true, inFlight: true, force: true));
    }

    [Fact]
    public void ShouldStartProfileGrab_ManualRefresh_CanReGrabEvenWhenAlreadyGrabbed()
    {
        // The Refresh Profile button should still work after an initial grab.
        Assert.True(HomeViewModel.ShouldStartProfileGrab(
            isLoggedIn: true, alreadyGrabbed: true, inFlight: false, force: true));
    }

    [Fact]
    public void ShouldStartProfileGrab_ManualRefresh_StillRequiresLogin()
    {
        // A forced refresh must not fire while logged out (would just 401 / spam errors).
        Assert.False(HomeViewModel.ShouldStartProfileGrab(
            isLoggedIn: false, alreadyGrabbed: false, inFlight: false, force: true));
    }

    [Fact]
    public void RepeatedAutomaticTicks_DoNotStackWhileSingleFetchInFlight()
    {
        // Simulates the reported scenario: several ~1s progress ticks land while one async fetch
        // is still pending. Only the first tick may start a fetch; the rest must be suppressed.
        bool isLoggedIn = true;
        bool alreadyGrabbed = false;
        bool inFlight = false;
        int fetchesStarted = 0;

        void Tick()
        {
            if (!HomeViewModel.ShouldStartProfileGrab(isLoggedIn, alreadyGrabbed, inFlight, force: false))
                return;

            inFlight = true;   // GrabProfile sets this synchronously before its first await
            fetchesStarted++;    // one network fetch actually begins
            alreadyGrabbed = true;   // ...completed later, in order, on the dispatcher
            inFlight = false;
        }

        Tick(); // starts the fetch
        Tick(); // blocked (in-flight / already grabbed)
        Tick(); // blocked
        Tick(); // blocked
        Tick(); // blocked

        Assert.Equal(1, fetchesStarted);
    }

    [Theory]
    [InlineData(true, false, false, false, true)]   // auto first fetch -> allowed
    [InlineData(true, false, true,  false, false)]  // auto while in-flight -> blocked
    [InlineData(true, true,  false, false, false)]  // auto after grab -> blocked
    [InlineData(true, true,  false, true,  true)]   // manual re-grab -> allowed
    [InlineData(true, true,  true,  true,  false)]  // manual while in-flight -> blocked
    [InlineData(false, false, false, false, false)] // logged out auto -> blocked
    [InlineData(false, true,  false, true,  false)]  // logged out manual -> blocked
    public void ShouldStartProfileGrab_DecisionMatrix(
        bool isLoggedIn, bool alreadyGrabbed, bool inFlight, bool force, bool expected)
    {
        Assert.Equal(expected,
            HomeViewModel.ShouldStartProfileGrab(isLoggedIn, alreadyGrabbed, inFlight, force));
    }
}
