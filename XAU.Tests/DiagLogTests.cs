using System.IO;
using System.Text.RegularExpressions;
using XAU.Util.Logging;
using Xunit;

namespace XAU.Tests;

/// <summary>
/// Covers the persistent diagnostics log used to review auth/stats/status behaviour over a long
/// stretch. Locks the line format and -- critically -- that the file lives under %LOCALAPPDATA%
/// and NOT under the OneDrive-redirected Documents folder (the latter caused an
/// UnauthorizedAccessException plus app-wide sync lag).
/// </summary>
public class DiagLogTests
{
    [Fact]
    public void DiagnosticsRequireOptInForNewSettings()
    {
        Assert.False(new XAUSettings().EnableDiagnosticsLog);
    }

    [Fact]
    public void FormatLine_PrependsFullDateTimeStamp_AndKeepsPayload()
    {
        var line = DiagLog.FormatLine("[XAUTHDBG] TestXAUTH: SUCCESS");

        // Full date+time (unlike the old HH:mm:ss-only EventsLog), so long/horizontal stretches correlate.
        Assert.True(
            Regex.IsMatch(line, @"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\] "),
            "expected leading [yyyy-MM-dd HH:mm:ss.fff] stamp, got: " + line);
        Assert.Contains("[XAUTHDBG] TestXAUTH: SUCCESS", line);
    }

    [Fact]
    // The log MUST sit under %LOCALAPPDATA% -- never the OneDrive-redirected Documents\XAU folder.
    // That placement was the source of the intermittent UnauthorizedAccessException and the lag.
    public void LogPath_IsUnderLocalAppData_NotTheSyncedDocumentsFolder()
    {
        var path = DiagLog.LogPath;
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.StartsWith(local, path);
        Assert.True(path.IndexOf("XAU", StringComparison.Ordinal) >= 0, "path should contain XAU: " + path);
        Assert.True(path.IndexOf("logs", StringComparison.Ordinal) >= 0, "path should contain a logs dir: " + path);
        Assert.EndsWith("xau_diagnostics.log", path);
        // Must NOT be inside the OneDrive-synced Documents tree.
        Assert.True(path.IndexOf("OneDrive", StringComparison.OrdinalIgnoreCase) < 0, "path must not be under OneDrive: " + path);
    }

    [Fact]
    public void Enabled_RoundTrips()
    {
        var prev = DiagLog.Enabled;
        try
        {
            DiagLog.Enabled = false;
            Assert.False(DiagLog.Enabled);

            DiagLog.Enabled = true;
            Assert.True(DiagLog.Enabled);
        }
        finally
        {
            DiagLog.Enabled = prev;
        }
    }
}
