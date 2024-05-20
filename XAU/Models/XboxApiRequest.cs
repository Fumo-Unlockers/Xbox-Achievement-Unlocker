// TODO: Clean up, set names, default fields, minor renames, etc.

public class GameTitleRequest
{
    public string? Pfns { get; set; }
    public List<string> TitleIds { get; set; } = new List<string>();
}

public class AchievementsArrayEntry
{
    public string? id { get; set; }
    public string percentComplete { get; set; } = "100";
}

public class UnlockTitleBasedAchievementRequest
{
    public string action { get; set; } = @"progressUpdate";
    public string serviceConfigId { get; set; } = StringConstants.ZeroUid;
    public string? titleId { get; set; }
    public string? userId { get; set; }
    public List<AchievementsArrayEntry> achievements { get; set; } = new List<AchievementsArrayEntry>();
}

public class GameStat
{
    public string Name { get; set; } = "MinutesPlayed";
    public string? TitleId { get; set; }
}

public class GameStatsRequest
{
    public string ArrangeByField { get; set; } = "xuid";
    public List<string> Xuids { get; set; } = new List<string>();
    public List<GameStat> Stats { get; set; } = new List<GameStat>();
}


public class HeartbeatRequest
{
    public List<TitleRequest> titles { get; set; } = new List<TitleRequest>();
}

public class TitleRequest
{
    public int expiration { get; set; } = 600;
    public string? id { get; set; }
    public string state { get; set; } = "active";
    public string sandbox { get; set; } = "RETAIL";
}

public class GamepassProductsRequest
{
    public List<string> Products { get; set; } = new List<string>();
}
