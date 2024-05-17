// TODO: Clean up, set names, default fields, minor renames, etc.

public class GameTitleRequest
{
    public String? Pfns { get; set; }
    public List<string> TitleIds { get; set; }
}

public class AchievementsArrayEntry
{
    public string Id { get; set; }
    public string PercentComplete {get; set; } = "100";
}

public class UnlockTitleBasedAchievementRequest
{
    public string Action { get; set; } = @"progressUpdate";
    public string ServiceConfigId { get; set; } = StringConstants.ZeroUid;
    public string TitleId {get; set;}
    public string UserId {get; set;}
    public List<AchievementsArrayEntry> Achievements { get; set; }
}

public class GameStat
{
    public string Name { get; set; } = "MinutesPlayed";
    public string TitleId { get; set; }
}

public class GameStatsRequest
{
    public string ArrangeByField {get; set;} = "xuid";
    public List<string> Xuids {get; set;}
    public List<GameStat> Stats {get; set;}
}


public class HeartbeatRequest
{
    public List<TitleRequest> titles { get; set; }
}

public class TitleRequest
{
    public int expiration { get; set; } = 600;
    public string id { get; set; }
    public string state { get; set; } = "active";
    public string sandbox { get; set; } = "RETAIL";
}

public class GamepassProductsRequest
{
    public List<string> Products { get; set; }
}
