// Minimize total number of string allocations if .NET runtime is opting to not intern them
struct StringConstants
{
    public const string Gamerscore = @"Gamerscore";
    public const string Achieved = @"Achieved";
    public const string ZeroUid = @"00000000-0000-0000-0000-000000000000";
}

struct HeaderNames
{
    public const string ContractVersion = @"x-xbl-contract-version";
    public const string AcceptEncoding = @"Accept-Encoding";
    public const string Accept = @"accept";
    public const string Authorization = @"Authorization";
    public const string AcceptLanguage = @"accept-language";
    public const string Host = @"Host";
    public const string Connection = @"Connection";
    public const string Signature = @"Signature";

}

struct HeaderValues
{
    public const string ContractVersion2 = @"2";
    public const string ContractVersion3 = @"3";
    public const string ContractVersion4 = @"4";
    public const string ContractVersion5 = @"5";
    public const string AcceptEncoding = @"gzip, deflate";
    public const string Accept = @"application/json";
    public const string KeepAlive = @"Keep-Alive";
    public const string Signature = @"RGFtbklHb3R0YU1ha2VUaGlzU3RyaW5nU3VwZXJMb25nSHVoLkRvbnRFdmVuS25vd1doYXRTaG91bGRCZUhlcmVEcmFmZlN0cmluZw==";

}

struct SocialLinks
{
    // Hardcoded links to socials
    public const string Discord = @"https://discord.gg/fCqM7287jG";
    public const string GitHubUserUrl = @"https://github.com/ItsLogic";
}

struct Hosts
{
    // Xbox Live Header Host Values
    public const string Achievements = @"achievements.xboxlive.com";
    public const string Profile = @"profile.xboxlive.com";
    public const string PeopleHub = @"peoplehub.xboxlive.com";
    public const string TitleHub = @"titlehub.xboxlive.com";
    public const string Telemetry = @"v20.events.data.microsoft.com";
    public const string GitHubApi = @"api.github.com";
    public const string GitHubRaw = @"raw.githubusercontent.com";

}

public struct BasicXboxAPIUris
{
    public const string GamertagUrl = @"https://profile.xboxlive.com/users/me/profile/settings?settings=Gamertag";
    public const string WatermarksUrl = @"https://dlassets-ssl.xboxlive.com/public/content/ppl/watermarks/";
    public const string GamepassCatalogUrl = @"https://catalog.gamepass.com/products?market=GB&language=en-GB&hydration=PCHome";
    public const string TelemetryUrl = "https://v20.events.data.microsoft.com/OneCollector/1.0/";

}

public struct InterpolatedXboxAPIUrls
{
    // TODO: could uri build things
    public const string GamepassMembershipUrl = "https://xgrant.xboxlive.com/users/xuid({0})/programInfo?filter=profile,activities,catalog";
    public const string ProfileUrl = "https://peoplehub.xboxlive.com/users/me/people/xuids({0})/decoration/detail,preferredColor,presenceDetail,multiplayerSummary";
    public const string TitleUrl = "https://titlehub.xboxlive.com/users/xuid({0})/titles/batch/decoration/GamePass,Achievement,Stats";
    public const string TitlesUrl = "https://titlehub.xboxlive.com/users/xuid({0})/titles/titleHistory/decoration/Achievement,scid?maxItems=10000";
    public const string QueryAchievementsUrl = "https://achievements.xboxlive.com/users/xuid({0})/achievements?titleId={1}&maxItems=1000";
    public const string UpdateAchievementsUrl = "https://achievements.xboxlive.com/users/xuid({0})/achievements/{1}/update";
}

public struct ProcessNames
{
    public const string XboxPcApp = @"XboxPcApp";
}

public struct EventsUrls
{
    public const string Zip = @"https://github.com/Fumo-Unlockers/Xbox-Achievement-Unlocker/raw/Events-Data/Events.zip";
    public const string MetaUrl = @"https://raw.githubusercontent.com/Fumo-Unlockers/Xbox-Achievement-Unlocker/Events-Data/meta.json";
}
