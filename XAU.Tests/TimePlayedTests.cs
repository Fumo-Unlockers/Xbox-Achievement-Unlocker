using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

/// <summary>
/// Regression tests for the "0 hours 0 minutes" play-time bug in the game-search/spoof card
/// (MiscViewModel.SpoofGame).
///
/// Symptom: TrueAchievements' scanner reported real play-time while XAU showed "0 Days, 0 Hours
/// and 0 minutes" -- because the old code read GameStatsResponse.StatListsCollection[0].Stats[0].Value
/// blindly. The userstats batch returns the all-time MinutesPlayed aggregate in a bucket that is NOT
/// necessarily [0][0]; a 0-minute seasonal/device slice often sits there first. The fix selects the
/// stat by NAME across every bucket and takes the largest (aggregate) value.
/// </summary>
public class TimePlayedTests
{
    private static Stat Stat(string name, string value, string type = null)
        => new() { Name = name, Value = value, Type = type };

    private static GameStatsResponse Response(params StatListCollection[] lists)
        => new() { StatListsCollection = new List<StatListCollection>(lists) };

    private static StatListCollection List(params Stat[] stats)
        => new() { Stats = new List<Stat>(stats) };

    [Fact]
    // The exact regression: [0][0] is a 0-minute seasonal slice, the real all-time figure lives in a
    // later bucket. Blind [0][0] returned 0 (=> "0 Days, 0 Hours and 0 minutes"); the fix returns 14512.
    public void MinutesPlayed_AggregateInLaterBucket_IsFound_NotTheLeadingZeroSlice()
    {
        var response = Response(
            List(Stat("MinutesPlayed", "0", "Season1")),   // <- what [0][0] used to grab
            List(Stat("MinutesPlayed", "14512", "allTime")) // <- the truth TrueAchievements shows
        );

        Assert.Equal(14512d, MiscViewModel.GetMinutesPlayed(response));
    }

    [Fact]
    // Non-MinutesPlayed stats (e.g. Gamerscore) must never be mistaken for play-time.
    public void NonMinutesPlayedStats_AreIgnored()
    {
        var response = Response(
            List(Stat("Gamerscore", "9000"), Stat("Score", "12345"))
        );

        Assert.Equal(-1d, MiscViewModel.GetMinutesPlayed(response));
    }

    [Fact]
    public void LargestValueWins_AcrossMultipleMinutesPlayedSlices()
    {
        var response = Response(
            List(Stat("MinutesPlayed", "30"), Stat("MinutesPlayed", "4000"), Stat("MinutesPlayed", "12"))
        );

        Assert.Equal(4000d, MiscViewModel.GetMinutesPlayed(response));
    }

    [Fact]
    public void Null_AndEmpty_ReturnUnknownSentinel()
    {
        Assert.Equal(-1d, MiscViewModel.GetMinutesPlayed(null));
        Assert.Equal(-1d, MiscViewModel.GetMinutesPlayed(new GameStatsResponse()));
        Assert.Equal(-1d, MiscViewModel.GetMinutesPlayed(Response(List())));
    }

    [Fact]
    // A present-but-unparseable value must not be silently read as 0; it yields the Unknown sentinel.
    public void UnparseableValue_YieldsUnknown_NotZero()
    {
        var response = Response(List(Stat("MinutesPlayed", "not-a-number")));
        Assert.Equal(-1d, MiscViewModel.GetMinutesPlayed(response));
    }

    [Fact]
    public void ThousandsSeparatedValue_IsParsedInvariantToCulture()
    {
        // "14,512" style must parse to 14512 under InvariantCulture regardless of the host locale.
        var response = Response(List(Stat("MinutesPlayed", "14,512")));
        Assert.Equal(14512d, MiscViewModel.GetMinutesPlayed(response));
    }

    [Fact]
    public void DumpStatBuckets_ShowsEveryBucketNameValue()
    {
        var response = Response(
            List(Stat("MinutesPlayed", "0")),
            List(Stat("MinutesPlayed", "14512"))
        );
        var dump = MiscViewModel.DumpStatBuckets(response);
        Assert.Contains("[0][0]", dump);
        Assert.Contains("[1][0]", dump);
        Assert.Contains("14512", dump);
    }

    #region Fallback: figure in Properties / GroupProperties, whitespace tolerance

    [Fact]
    // The likely real-world shape behind a spurious "Unknown": MinutesPlayed present but Value null,
    // with the number surfacing via Properties. Must be read, not reported Unknown.
    public void MinutesPlayed_NullValue_FallsBackToProperties()
    {
        var st = new Stat { Name = "MinutesPlayed", Value = null };
        st.Properties["value"] = "24512";
        var response = Response(List(st));
        Assert.Equal(24512d, MiscViewModel.GetMinutesPlayed(response));
    }

    [Fact]
    // Same, but surfaced via GroupProperties.
    public void MinutesPlayed_NullValue_FallsBackToGroupProperties()
    {
        var st = new Stat { Name = "MinutesPlayed", Value = null };
        st.GroupProperties["minutesPlayed"] = "14512";
        var response = Response(List(st));
        Assert.Equal(14512d, MiscViewModel.GetMinutesPlayed(response));
    }

    [Fact]
    // The name gate must hold even for the fallback: a Gamerscore stat carrying numeric Properties must
    // NOT be harvested as play-time. (This is the guard against re-introducing the [0][0] bug.)
    public void NonMinutesPlayed_WithNumericProperties_IsStillIgnored()
    {
        var gs = new Stat { Name = "Gamerscore", Value = null };
        gs.Properties["value"] = "9000";
        var response = Response(List(gs));
        Assert.Equal(-1d, MiscViewModel.GetMinutesPlayed(response));
    }

    [Fact]
    public void MinutesPlayed_LeadingTrailingWhitespace_IsParsed()
    {
        var response = Response(List(Stat("  MinutesPlayed  ", "  24512  ")));
        Assert.Equal(24512d, MiscViewModel.GetMinutesPlayed(response));
    }

    #endregion
}
