using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

/// <summary>
/// Regression tests for the crash reports:
///  - SpoofGame -> ArgumentOutOfRangeException (blind list indexing after the shared
///    GameInfoResponse is reassigned to an empty GameTitle while a spoof poll is in flight).
///  - BackgroundWorker "is currently busy and cannot run multiple tasks concurrently"
///    (InitializeViewModel re-entered during its awaits, double-starting the workers).
/// Both crashes surfaced as the "Information / Exception" dialog because the faulting code is
/// async-void / dispatcher-marshalled. The fixes expose small pure predicates that these tests
/// pin down.
/// </summary>
public class StabilityTests
{
    #region SpoofGame title-index crash

    [Fact]
    public void GetFirstTitleName_NullResponse_ReturnsNull_DoesNotThrow()
    {
        Assert.Null(AchievementsViewModel.GetFirstTitleName(null));
    }

    [Fact]
    public void GetFirstTitleName_EmptyTitles_ReturnsNull_DoesNotThrow()
    {
        // This is the exact reported scenario: an empty Titles list must NOT throw
        // ArgumentOutOfRangeException the way a blind Titles[0] did.
        var response = new GameTitle { Titles = new List<XboxTitle>() };

        Assert.Null(AchievementsViewModel.GetFirstTitleName(response));
    }

    [Fact]
    public void GetFirstTitleName_WithTitles_ReturnsFirstTitleName()
    {
        var response = new GameTitle
        {
            Titles = new List<XboxTitle>
            {
                new XboxTitle { Name = "Halo Infinite" },
                new XboxTitle { Name = "Halo 2" }
            }
        };

        Assert.Equal("Halo Infinite", AchievementsViewModel.GetFirstTitleName(response));
    }

    [Fact]
    public void GetFirstTitleName_ClearedMidFlight_ReturnsNullInsteadOfThrowing()
    {
        // Simulates the race: response has titles, then a concurrent reload replaces it with an
        // empty GameTitle before the spoofed-name is read. The guard makes the read null, not fatal.
        var response = new GameTitle { Titles = new List<XboxTitle> { new XboxTitle { Name = "Game" } } };
        Assert.Equal("Game", AchievementsViewModel.GetFirstTitleName(response));

        response = new GameTitle(); // cleared while a spoof poll is in flight

        Assert.Null(AchievementsViewModel.GetFirstTitleName(response));
    }

    [Fact]
    public void SpoofedGameName_EmptyTitles_FallsBackWithoutThrowing()
    {
        // Exercises the property used by SpoofGame across the await boundary.
        var response = new GameTitle { Titles = new List<XboxTitle>() };

        var ex = Record.Exception(() => AchievementsViewModel.GetFirstTitleName(response));

        Assert.Null(ex);
    }

    #endregion

    #region BackgroundWorker double-start crash

    [Theory]
    [InlineData(false, false, true)]   // fresh -> run
    [InlineData(true,  false, false)]  // already initialised -> skip
    [InlineData(false, true,  false)]  // init already in progress -> skip (the re-entrancy bug)
    [InlineData(true,  true,  false)]  // both -> skip
    public void ShouldBeginInitialization_Matrix(bool initialized, bool initializing, bool expected)
    {
        Assert.Equal(expected, HomeViewModel.ShouldBeginInitialization(initialized, initializing));
    }

    [Fact]
    public void ReentrantNavigationDuringAsyncInit_DoesNotReRunInitialization()
    {
        bool initialized = false;
        bool initializing = false;
        int initsRun = 0;

        void Navigate()
        {
            if (!HomeViewModel.ShouldBeginInitialization(initialized, initializing))
                return;

            initializing = true;
            try
            {
                // First call has not yet reached "initialized = true" (it is still awaiting
                // network work). A second navigation arriving right now must be ignored.
                Navigate(); // re-entrant navigation while the first is still initialising

                initialized = true; // init completes (after its awaits) and sets this last
                initsRun++;
            }
            finally
            {
                initializing = false;
            }
        }

        Navigate();

        Assert.Equal(1, initsRun);
    }

    [Theory]
    [InlineData(false, true)]  // idle worker -> may start
    [InlineData(true,  false)] // busy worker -> must NOT start (would throw)
    public void ShouldStartWorker_Matrix(bool isBusy, bool expected)
    {
        Assert.Equal(expected, HomeViewModel.ShouldStartWorker(isBusy));
    }

    [Fact]
    public void WorkerStarts_AreIdempotentUnderDoubleCompletion()
    {
        // The RunWorkerCompleted handler restarts the worker; combined with a manual start that
        // previously produced a concurrent-start exception. With the ShouldStartWorker guard, a
        // start while already busy is a no-op instead of a crash.
        bool busy = false;
        int realStarts = 0;

        void Start()
        {
            if (!HomeViewModel.ShouldStartWorker(busy))
                return;
            busy = true;   // RunWorkerAsync flips IsBusy true
            realStarts++;
        }

        Start();            // start
        Start();            // already busy -> suppressed
        busy = false;       // worker completed
        Start();            // start again after completion
        Start();            // already busy -> suppressed

        Assert.Equal(2, realStarts);
    }

    #endregion
}
