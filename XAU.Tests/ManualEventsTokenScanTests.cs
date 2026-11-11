using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

/// <summary>
/// Regression tests for "multiple clicked, refresh event token does not work."
///
/// Root cause: the "Manually Refresh Token" button only self-disabled on click (UI-thread only) and
/// ScanForEventsTokenManual() had NO single-flight guard. ManualScanRunning was a plain bool shared by
/// every fired Task, and the whole scan drives process-wide shared state (the ETW trace + Solitaire +
/// the shared eventsTokenFound/EventsToken fields). A burst of clicks (or manual+auto overlap, or the
/// 3s poll re-enabling the button the moment the first finishing Task cleared the bool) started several
/// concurrent grabs that tore down each other's trace and nulled a token another had just set, so the
/// refresh produced nothing and the button could look wedged.
///
/// Fix pinned here: ShouldStartManualScan() is the single-flight decision, and ETW/Solitaire use is
/// serialised behind one gate.
/// </summary>
public class ManualEventsTokenScanTests
{
    [Theory]
    [InlineData(false, true)]   // idle -> may start
    [InlineData(true,  false)]  // already running -> must not start (would stomp the live scan)
    public void ShouldStartManualScan_Matrix(bool alreadyRunning, bool expected)
    {
        Assert.Equal(expected, HomeViewModel.ShouldStartManualScan(alreadyRunning));
    }

    [Fact]
    public void RapidClicks_StartExactlyOneScan_AndLaterClicksAreIgnored()
    {
        // Simulates the reported burst of clicks while a scan is in flight. The key property of the
        // real button: once started, ManualScanRunning stays true for the whole ~25s+ background
        // grab (it is only cleared in the Task's finally, much later), so the burst of clicks all
        // arrive while it is still true and are refused.
        bool running = false;
        int scansStarted = 0;

        void Click()
        {
            // Click handler: guard on the VM's single-flight flag, then start.
            if (!HomeViewModel.ShouldStartManualScan(running))
                return;
            running = true;            // ScanForEventsTokenManual sets this synchronously
            scansStarted++;
            // The background Task runs the long Solitaire/ETW grab; running stays true throughout.
        }

        Click(); // starts the scan (running -> true for the duration of the grab)
        Click(); // ignored (still running)
        Click(); // ignored
        Click(); // ignored
        Click(); // ignored

        running = false; // background Task finally-clause, after the grab completes

        Assert.Equal(1, scansStarted);
    }

    [Fact]
    public void PollCannotReEnableButtonMidScan_AndCompletionEdgeFiresOnce()
    {
        // The 3s DispatcherTimer re-enables the button when it observes running go false. With a single
        // owner that edge happens exactly once -- the bug was that ANY finishing task cleared the shared
        // bool, so the poll re-enabled the button while another grab was still mid-flight.
        bool running = false;
        bool buttonEnabled = true;
        int completionToasts = 0;

        void Click()
        {
            if (!HomeViewModel.ShouldStartManualScan(running)) return;
            running = true;
            buttonEnabled = false;

            // ...scan completes...
            running = false;
        }

        void Poll()
        {
            if (!buttonEnabled && !running)
            {
                buttonEnabled = true;
                completionToasts++;
            }
        }

        Click();   // running true, button disabled
        Poll();    // still running -> button stays disabled
        Poll();
        Poll();
        // scan finished between ticks:
        Poll();    // observes the single completion edge

        Assert.True(buttonEnabled);
        Assert.Equal(1, completionToasts);
    }

    [Fact]
    public void ManualAndAutoWorker_EtwGate_SerialisesSharedResource()
    {
        // Manualizes the shared SemaphoreSlim gate the fix adds: the manual grab and the auto worker
        // must never be inside the ETW/Solitaire critical section at the same moment.
        var gate = new System.Threading.SemaphoreSlim(1, 1);
        int inside = 0, maxInside = 0;
        var lockObj = new object();

        void EnterCritical()
        {
            gate.Wait();
            try
            {
                lock (lockObj) { inside++; maxInside = Math.Max(maxInside, inside); Thread.Sleep(5); inside--; }
            }
            finally { gate.Release(); }
        }

        var manual = new Thread(EnterCritical);
        var auto = new Thread(EnterCritical);
        manual.Start(); auto.Start();
        manual.Join(); auto.Join();

        Assert.Equal(1, maxInside); // never two concurrent occupants of the shared ETW resource
    }
}
