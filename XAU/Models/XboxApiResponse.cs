// OpenAPI or Swagger doesn't exist. But let's try and type the things we need to make parsing more readable
// https://learn.microsoft.com/en-us/gaming/gdk/_content/gc/reference/live/rest/atoc-xboxlivews-reference

// See 'Models.md' for a breakdown of the responses'
public class Person
{
    public string Xuid { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsFollowingCaller { get; set; }
    public bool IsFollowedByCaller { get; set; }
    public bool IsIdentityShared { get; set; }
    public DateTime? AddedDateTimeUtc { get; set; }
    public string DisplayName { get; set; }
    public string RealName { get; set; }
    public string DisplayPicRaw { get; set; }
    public string ShowUserAsAvatar { get; set; }
    public string Gamertag { get; set; }
    public string GamerScore { get; set; }
    public string ModernGamertag { get; set; }
    public string ModernGamertagSuffix { get; set; }
    public string UniqueModernGamertag { get; set; }
    public string XboxOneRep { get; set; }
    public string PresenceState { get; set; }
    public string PresenceText { get; set; }
    public object PresenceDevices { get; set; }
    public bool IsBroadcasting { get; set; }
    public bool IsCloaked { get; set; }
    public bool IsQuarantined { get; set; }
    public bool IsXbox360Gamerpic { get; set; }
    public DateTime? LastSeenDateTimeUtc { get; set; }
    public object Suggestion { get; set; }
    public object Recommendation { get; set; }
    public object Search { get; set; }
    public object TitleHistory { get; set; }
    public MultiplayerSummary MultiplayerSummary { get; set; }
    public object RecentPlayer { get; set; }
    public object Follower { get; set; }
    public PreferredColor PreferredColor { get; set; }
    public List<PresenceDetail> PresenceDetails { get; set; }
    public object TitlePresence { get; set; }
    public object TitleSummaries { get; set; }
    public object PresenceTitleIds { get; set; }
    public Detail Detail { get; set; }
    public object CommunityManagerTitles { get; set; }
    public object SocialManager { get; set; }
    public object Broadcast { get; set; }
    public object Avatar { get; set; }
    public List<LinkedAccount> LinkedAccounts { get; set; }
    public string ColorTheme { get; set; }
    public string PreferredFlag { get; set; }
    public List<object> PreferredPlatforms { get; set; }
}

public class MultiplayerSummary
{
    public List<object> JoinableActivities { get; set; }
    public List<object> PartyDetails { get; set; }
    public int InParty { get; set; }
}

public class PreferredColor
{
    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    public string TertiaryColor { get; set; }
}

public class PresenceDetail
{
    public bool IsBroadcasting { get; set; }
    public string Device { get; set; }
    public object DeviceSubType { get; set; }
    public object GameplayType { get; set; }
    public string PresenceText { get; set; }
    public string State { get; set; }
    public string TitleId { get; set; }
    public object TitleType { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsGame { get; set; }
    public object RichPresenceText { get; set; }
}

public class Detail
{
    public string AccountTier { get; set; }
    public string Bio { get; set; }
    public bool IsVerified { get; set; }
    public string Location { get; set; }
    public string Tenure { get; set; }
    public List<string> Watermarks { get; set; } = new List<string>();
    public bool Blocked { get; set; }
    public bool Mute { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public bool HasGamePass { get; set; }
}

public class LinkedAccount
{
    public string NetworkName { get; set; }
    public string DisplayName { get; set; }
    public bool ShowOnProfile { get; set; }
    public bool IsFamilyFriendly { get; set; }
    public string Deeplink { get; set; }
}

public class Profile
{
    public List<Person> People { get; set; } = new List<Person>();
    public object RecommendationSummary { get; set; }
    public object FriendFinderState { get; set; }
    public object AccountLinkDetails { get; set; }
}


public class XboxTitle
{
    public string TitleId { get; set; }
    public string Pfn { get; set; }
    public string BingId { get; set; }
    public string WindowsPhoneProductId { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public List<string> Devices { get; set; }
    public string DisplayImage { get; set; }
    public string MediaItemType { get; set; }
    public string ModernTitleId { get; set; }
    public bool IsBundle { get; set; }
    public Achievement Achievement { get; set; }
    public Stats Stats { get; set; }
    public GamePass GamePass { get; set; }
    public object Images { get; set; }
    public object TitleHistory { get; set; }
    public object TitleRecord { get; set; }
    public object Detail { get; set; }
    public object FriendsWhoPlayed { get; set; }
    public object AlternateTitleIds { get; set; }
    public object ContentBoards { get; set; }
    public string XboxLiveTier { get; set; }
}

public class Achievement
{
    public int CurrentAchievements { get; set; }
    public int TotalAchievements { get; set; }
    public int CurrentGamerscore { get; set; }
    public int TotalGamerscore { get; set; }
    public double ProgressPercentage { get; set; }
    public int SourceVersion { get; set; }
}

public class Stats
{
    public int SourceVersion { get; set; }
}

public class GamePass
{
    public bool IsGamePass { get; set; }
}

public class GameTitle
{
    public string Xuid { get; set; }
    public List<XboxTitle> Titles { get; set; }
}

public class GamepassData
{
    public string GamepassMembership { get; set; }
}

public class Gamepass
{
    // This json response is actually massive, but we don't care
    // TOOD: maybe we want to look at points/stuff later
    public string GamepassMembership {get; set;}
    public GamepassData Data { get; set; }
}

public class TitleHistory
{
    public DateTime LastTimePlayed { get; set; }
    public bool Visible { get; set; }
    public bool CanHide { get; set; }
}

public class Title
{
    public string TitleId { get; set; }
    public string Pfn { get; set; }
    public string BingId { get; set; }
    public string ServiceConfigId { get; set; }
    public string WindowsPhoneProductId { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public List<string> Devices { get; set; }
    public string DisplayImage { get; set; }
    public string MediaItemType { get; set; }
    public string ModernTitleId { get; set; }
    public bool IsBundle { get; set; }
    public Achievement Achievement { get; set; }
    public object Stats { get; set; }
    public object GamePass { get; set; }
    public object Images { get; set; }
    public TitleHistory TitleHistory { get; set; }
    public object TitleRecord { get; set; }
    public object Detail { get; set; }
    public object FriendsWhoPlayed { get; set; }
    public object AlternateTitleIds { get; set; }
    public object ContentBoards { get; set; }
    public string XboxLiveTier { get; set; }
}

public class TitlesList
{
    public string Xuid { get; set; }
    public List<Title> Titles { get; set; }
}

public class ProfileSettings
{
    public string Id {get;set;}
    public string Value {get;set;}
}

public class ProfileUser
{
    public string Id {get; set;}
    public string HostId {get;set;}
    public List<ProfileSettings> Settings {get;set;}
    public string IsSponsoredUser {get;set;}
}

public class BasicProfile
{
    public List<ProfileUser> ProfileUsers {get; set;}
}
