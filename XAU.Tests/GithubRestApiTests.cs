using Newtonsoft.Json.Linq;
using Xunit;

namespace XAU.Tests;

public class GithubRestApiTests
{
    [Fact]
    public void FilterStableReleases_ExcludesPrereleases()
    {
        var releases = JArray.Parse("""
            [
                {"tag_name": "26.07.01", "prerelease": true},
                {"tag_name": "26.06.14", "prerelease": false},
                {"tag_name": "26.05.01", "prerelease": false}
            ]
            """);

        var stable = GithubRestApi.FilterStableReleases(releases);

        Assert.Equal(2, stable.Count);
        Assert.Equal("26.06.14", stable[0]["tag_name"]?.ToString());
    }

    [Fact]
    public void FilterStableReleases_EmptyArray_ReturnsEmpty()
    {
        Assert.Empty(GithubRestApi.FilterStableReleases(new JArray()));
    }

    [Fact]
    public void FilterStableReleases_MissingPrereleaseField_TreatedAsStable()
    {
        var releases = JArray.Parse("""[{"tag_name": "26.06.14"}]""");

        Assert.Single(GithubRestApi.FilterStableReleases(releases));
    }
}
