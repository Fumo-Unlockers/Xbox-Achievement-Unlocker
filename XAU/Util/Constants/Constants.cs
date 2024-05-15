// Minimize total number of string allocations if .NET runtime is opting to not intern them
struct StringConstants
{
    public const string Gamerscore = "Gamerscore";
    public const string Achieved = "Achieved";
    public const string ZeroUid = "00000000-0000-0000-0000-000000000000";
}

struct HeaderNames
{
    public const string ContractVersion = "x-xbl-contract-version";
    public const string AcceptEncoding = "Accept-Encoding";
    public const string Accept = "accept";
    public const string Authorization = "Authorization";
    public const string AcceptLanguage = "accept-language";
    public const string Host = "Host";
    public const string Connection = "Connection";
}

struct HeaderValues
{
    public const string ContractVersion2 = "2";
    public const string ContractVersion3 = "3";
    public const string ContractVersion4 = "4";
    public const string ContractVersion5 = "5";
    public const string AcceptEncoding = "gzip, deflate";
    public const string Accept = "application/json";
    public const string KeepAlive = "Keep-Alive";

}

struct Links
{
    public const string Discord = "https://discord.gg/fCqM7287jG";
    public const string GitHubUserUrl = "https://github.com/ItsLogic";
}

struct Hosts
{
    public const string Achievements = "achievements.xboxlive.com";
    public const string Profile = "profile.xboxlive.com";
    public const string PeopleHub = "peoplehub.xboxlive.com";
    public const string TitleHub = "titlehub.xboxlive.com";
    public const string Telemetry = "v20.events.data.microsoft.com";
    public const string GitHubApi = "api.github.com";
    public const string GitHubRaw = "raw.githubusercontent.com";

}
