using Newtonsoft.Json;
using Xunit;

namespace XAU.Tests;

public class ModelDeserializationTests
{
    [Fact]
    public void GameTitle_DefaultInstance_HasEmptyTitlesList()
    {
        var gameTitle = new GameTitle();

        Assert.NotNull(gameTitle.Titles);
        Assert.Empty(gameTitle.Titles);
    }

    [Fact]
    public void GameTitle_Deserializes_TitleFields()
    {
        var json = """
            {
                "xuid": "2533274800000000",
                "titles": [
                    {
                        "titleId": "1144039928",
                        "name": "Halo Infinite",
                        "devices": ["PC", "XboxSeries"],
                        "achievement": {
                            "currentAchievements": 10,
                            "currentGamerscore": 250,
                            "totalGamerscore": 1000,
                            "progressPercentage": 25.0
                        }
                    }
                ]
            }
            """;

        var gameTitle = JsonConvert.DeserializeObject<GameTitle>(json);

        Assert.NotNull(gameTitle);
        var title = Assert.Single(gameTitle.Titles);
        Assert.Equal("1144039928", title.TitleId);
        Assert.Equal("Halo Infinite", title.Name);
        Assert.Contains("PC", title.Devices);
        Assert.Equal(250, title.Achievement?.CurrentGamerscore);
    }

    [Fact]
    public void GameTitle_Deserializes_EmptyTitlesResponse()
    {
        var gameTitle = JsonConvert.DeserializeObject<GameTitle>("""{"xuid": "123", "titles": []}""");

        Assert.NotNull(gameTitle);
        Assert.Empty(gameTitle.Titles);
    }

    [Fact]
    public void TitlesList_DefaultInstance_HasEmptyTitlesList()
    {
        var titlesList = new TitlesList();

        Assert.NotNull(titlesList.Titles);
        Assert.Empty(titlesList.Titles);
    }

    [Fact]
    public void Profile_Deserializes_PersonDetails()
    {
        var json = """
            {
                "people": [
                    {
                        "xuid": "2533274800000000",
                        "gamertag": "SomeGamer",
                        "gamerScore": "12345",
                        "detail": {
                            "accountTier": "Gold",
                            "followerCount": 5,
                            "followingCount": 10
                        }
                    }
                ]
            }
            """;

        var profile = JsonConvert.DeserializeObject<Profile>(json);

        Assert.NotNull(profile);
        var person = Assert.Single(profile.People);
        Assert.Equal("SomeGamer", person.Gamertag);
        Assert.Equal("Gold", person.Detail?.AccountTier);
    }
}
