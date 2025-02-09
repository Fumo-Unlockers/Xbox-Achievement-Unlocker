// OpenAPI or Swagger doesn't exist. But let's try and type the things we need to make parsing more readable
// https://learn.microsoft.com/en-us/gaming/gdk/_content/gc/reference/live/rest/atoc-xboxlivews-reference

// TODO: Clean up, set names, default fields, minor renames, etc.

public class PersonResponse
{
    public string? Xuid { get; set; }

    public bool IsFavorite { get; set; }
    public bool IsFollowingCaller { get; set; }
    public bool IsFollowedByCaller { get; set; }
    public bool IsIdentityShared { get; set; }
    public DateTime? AddedDateTimeUtc { get; set; }
    public string? DisplayName { get; set; }

    public string? RealName { get; set; }

    public string? DisplayPicRaw { get; set; }

    public string? ShowUserAsAvatar { get; set; }

    public string? Gamertag { get; set; }

    public string? GamerScore { get; set; }

    public string? ModernGamertag { get; set; }

    public string? ModernGamertagSuffix { get; set; }

    public string? UniqueModernGamertag { get; set; }

    public string? XboxOneRep { get; set; }

    public string? PresenceState { get; set; }

    public string? PresenceText { get; set; }

    public object? PresenceDevices { get; set; }

    public bool IsBroadcasting { get; set; }
    public bool? IsCloaked { get; set; }
    public bool IsQuarantined { get; set; }
    public bool IsXbox360Gamerpic { get; set; }
    public DateTime? LastSeenDateTimeUtc { get; set; }
    public object? Suggestion { get; set; }

    public object? Recommendation { get; set; }

    public object? Search { get; set; }

    public object? TitleHistory { get; set; }

    public MultiplayerSummary? MultiplayerSummary { get; set; }
    public object? RecentPlayer { get; set; }

    public object? Follower { get; set; }

    public PreferredColor? PreferredColor { get; set; }
    public List<PresenceDetail> PresenceDetails { get; set; } = new List<PresenceDetail>();
    public object? TitlePresence { get; set; }

    public object? TitleSummaries { get; set; }

    public object? PresenceTitleIds { get; set; }

    public Detail? Detail { get; set; }
    public object? CommunityManagerTitles { get; set; }

    public object? SocialManager { get; set; }

    public object? Broadcast { get; set; }

    public object? Avatar { get; set; }

    public List<LinkedAccount> LinkedAccounts { get; set; } = new List<LinkedAccount>();
    public string? ColorTheme { get; set; }

    public string? PreferredFlag { get; set; }

    public List<object> PreferredPlatforms { get; set; } = new List<object>();
}

public class MultiplayerSummary
{
    public List<object> JoinableActivities { get; set; } = new List<object>();
    public List<object> PartyDetails { get; set; } = new List<object>();
    public int InParty { get; set; }
}

public class PreferredColor
{
    public string? PrimaryColor { get; set; }

    public string? SecondaryColor { get; set; }

    public string? TertiaryColor { get; set; }

}

public class PresenceDetail
{
    public bool IsBroadcasting { get; set; }
    public string? Device { get; set; }

    public object? DeviceSubType { get; set; }

    public object? GameplayType { get; set; }

    public string? PresenceText { get; set; }

    public string? State { get; set; }

    public string? TitleId { get; set; }

    public object? TitleType { get; set; }

    public bool IsPrimary { get; set; }
    public bool IsGame { get; set; }
    public object? RichPresenceText { get; set; }

}

public class Detail
{
    public string? AccountTier { get; set; }

    public string? Bio { get; set; }

    public bool IsVerified { get; set; }
    public string? Location { get; set; }

    public string? Tenure { get; set; }

    public List<string> Watermarks { get; set; } = new List<string>();
    public bool Blocked { get; set; }
    public bool Mute { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public bool HasGamePass { get; set; }
    public List<string>? Genres { get; set; }
}

public class LinkedAccount
{
    public string? NetworkName { get; set; }

    public string? DisplayName { get; set; }

    public bool ShowOnProfile { get; set; }
    public bool IsFamilyFriendly { get; set; }
    public string? Deeplink { get; set; }

}

public class Profile
{
    public List<PersonResponse> People { get; set; } = new List<PersonResponse>();
    public object? RecommendationSummary { get; set; }

    public object? FriendFinderState { get; set; }

    public object? AccountLinkDetails { get; set; }

}


public class XboxTitle
{
    public string? TitleId { get; set; }

    public string? Pfn { get; set; }

    public string? BingId { get; set; }

    public string? WindowsPhoneProductId { get; set; }

    public required string Name { get; set; }

    public string? Type { get; set; }

    public List<string> Devices { get; set; } = new List<string>();
    public string? DisplayImage { get; set; }

    public string? MediaItemType { get; set; }

    public string? ModernTitleId { get; set; }

    public bool IsBundle { get; set; }
    public BasicAchievementDetails? Achievement { get; set; }
    public Stats? Stats { get; set; }
    public GamePass? GamePass { get; set; }
    public object? Images { get; set; }

    public object? TitleHistory { get; set; }

    public object? TitleRecord { get; set; }

    public object? Detail { get; set; }

    public object? FriendsWhoPlayed { get; set; }

    public object? AlternateTitleIds { get; set; }

    public object? ContentBoards { get; set; }

    public string? XboxLiveTier { get; set; }

}

public class BasicAchievementDetails
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
    public string? Xuid { get; set; }

    public List<XboxTitle> Titles { get; set; } = new List<XboxTitle>();
}

public class GamepassData
{
    public string? GamepassMembership { get; set; }

}

public class Gamepass
{
    // This json response is actually massive, but we don't care
    // TOOD: maybe we want to look at points/stuff later
    public string? GamepassMembership { get; set; }

    public GamepassData? Data { get; set; }
}

public class TitleHistory
{
    public DateTime LastTimePlayed { get; set; }
    public bool Visible { get; set; }
    public bool CanHide { get; set; }
}

public class Title
{
    public string? TitleId { get; set; }

    public string? Pfn { get; set; }

    public string? BingId { get; set; }

    public string? ServiceConfigId { get; set; }

    public string? WindowsPhoneProductId { get; set; }

    public string? Name { get; set; }

    public string? Type { get; set; }

    public List<string> Devices { get; set; } = new List<string>();
    public string? DisplayImage { get; set; }

    public string? MediaItemType { get; set; }

    public string? ModernTitleId { get; set; }

    public bool IsBundle { get; set; }
    public BasicAchievementDetails? Achievement { get; set; }
    public object? Stats { get; set; }

    public object? GamePass { get; set; }

    public object? Images { get; set; }

    public TitleHistory? TitleHistory { get; set; }
    public object? TitleRecord { get; set; }

    public Detail? Detail { get; set; }

    public object? FriendsWhoPlayed { get; set; }

    public object? AlternateTitleIds { get; set; }

    public object? ContentBoards { get; set; }

    public string? XboxLiveTier { get; set; }

}

public class TitlesList
{
    public string? Xuid { get; set; }

    public List<Title> Titles { get; set; } = new List<Title>();
}

public class ProfileSettings
{
    public string? Id { get; set; }

    public string? Value { get; set; }

}

public class ProfileUser
{
    public string? Id { get; set; }

    public string? HostId { get; set; }

    public List<ProfileSettings> Settings { get; set; } = new List<ProfileSettings>();
    public string? IsSponsoredUser { get; set; }

}

public class BasicProfile
{
    public List<ProfileUser> ProfileUsers { get; set; } = new List<ProfileUser>();
}


public class Stat
{
    public Dictionary<string, object> GroupProperties { get; set; } = new Dictionary<string, object>();
    public string? Xuid { get; set; }

    public string? Scid { get; set; }

    public string? TitleId { get; set; }

    public string? Name { get; set; }

    public string? Type { get; set; }

    public string? Value { get; set; }

    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}

public class StatListCollection
{
    public string? ArrangeByField { get; set; }

    public string? ArrangeByFieldId { get; set; }

    public List<Stat> Stats { get; set; } = new List<Stat>();
}

public class GameStatsResponse
{
    public List<object> Groups { get; set; } = new List<object>();
    public List<StatListCollection> StatListsCollection { get; set; } = new List<StatListCollection>();
}

public class AchievementRewards
{
    public string? name { get; set; }

    public string? description { get; set; }

    public string? value { get; set; }

    public string? type { get; set; }

    public MediaAsset? mediaAsset { get; set; }
    public string? valueType { get; set; }

}

public class Rarity
{
    public string? currentCategory { get; set; }

    public string? currentPercentage { get; set; }

}


public class AchievementRequirements
{
    public string? id { get; set; }

    public string? current { get; set; }

    public string? target { get; set; }

    public string? operationType { get; set; }

    public string? valueType { get; set; }

    public string? ruleParticipationType { get; set; }

}

public class AchievementProgression
{
    public List<AchievementRequirements> requirements { get; set; } = new List<AchievementRequirements>();
    public string? timeUnlocked { get; set; }

}

public class TitleAssociation
{
    public string? name { get; set; }

    public string? id { get; set; }

}

public class MediaAsset
{
    public string? name { get; set; }

    public string? type { get; set; }

    public string? url { get; set; }

}



public class OneCoreAchievementResponse // Xbox One Achievements
{
    public Rarity? rarity { get; set; }
    public object? gamerscore { get; set; }
    public required string id { get; set; }
    public string serviceConfigId { get; set; } = StringConstants.ZeroUid;
    public required string name { get; set; }
    public List<TitleAssociation> titleAssociations { get; set; } = new List<TitleAssociation>();
    public string progressState { get; set; } = "Null";
    public AchievementProgression? progression { get; set; }
    public List<MediaAsset> mediaAssets { get; set; } = new List<MediaAsset>();
    public List<string> platforms { get; set; } = new List<string>();
    public bool isSecret { get; set; }
    public string? description { get; set; }
    public string? lockedDescription { get; set; }
    public string? productId { get; set; }
    public string? achievementType { get; set; }
    public string? participationType { get; set; }
    public TimeWindow? timeWindow { get; set; }
    public List<AchievementRewards> rewards { get; set; } = new List<AchievementRewards>();
    public string? estimatedTime { get; set; }
    public string? deeplink { get; set; }
    public string? isRevoked { get; set; }
    public string? raritycurrentCategory { get; set; }
    public string? raritycurrentPercentage { get; set; }
}

public class Xbox360AchievementEntry
{
    public int id {get; set;}
    public long titleId {get; set;}
    public string name {get; set;}
    public int gamerscore {get; set;}
    public string description {get; set;}
    public string lockedDescription {get; set;}
    public string timeUnlocked {get; set;}
    public bool isSecret {get; set;}
    public Rarity? rarity {get; set;}

}

public class Xbox360AchievementResponse
{
    public List<Xbox360AchievementEntry> achievements {get;set; } = new List<Xbox360AchievementEntry>();
}

public class AchievementsResponse
{
    public List<OneCoreAchievementResponse> achievements { get; set; } = new List<OneCoreAchievementResponse>();
}

public class TimeWindow
{
    public required string startDate { get; set; }
    public required string endDate { get; set; }

}

public class Image
{
    public string? URI { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public string? Caption { get; set; }
}

public class Product
{
    public bool PCPlatformPreinstallable { get; set; }
    public string? ProductTitle { get; set; }
    public string? ProductDescription { get; set; }
    public string? ProductDescriptionShort { get; set; }
    public string? PackageFamilyName { get; set; }
    public string? ProductType { get; set; }
    public object? ChildPackageFamilyNames { get; set; }
    public string? XboxTitleId { get; set; }
    public object? ChildXboxTitleIds { get; set; }
    public int MinimumUserAge { get; set; }
    public List<object> Children { get; set; } = new List<object>();
    public List<string> Categories { get; set; } = new List<string>();
    public List<object> Attributes { get; set; } = new List<object>();
    public object? HeroTrailer { get; set; }
    public Image? ImageBoxArt { get; set; }
    public Image? ImageHero { get; set; }
    public object? ImageTitledHero { get; set; }
    public Image? ImagePoster { get; set; }
    public object? ImageTile { get; set; }
    public object? PCComingSoonDate { get; set; }
    public object? PCExitDate { get; set; }
    public object? UltimateComingSoonDate { get; set; }
    public object? UltimateExitDate { get; set; }
    public bool IsEAPlay { get; set; }
    public List<object> XCloudSupportedInputs { get; set; } = new List<object>();
    public object? GameCatalogExtensionId { get; set; }
    public string? StoreId { get; set; }
}

public class GamePassProducts
{
    public Dictionary<string, Product> Products { get; set; } = new Dictionary<string, Product>();
    public List<object> InvalidIds { get; set; } = new List<object>();
}
