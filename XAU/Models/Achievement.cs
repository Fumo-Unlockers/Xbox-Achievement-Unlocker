namespace XAU.Models;

public class Achievement
{
    public string? Id { get; init; }
    public string? ServiceConfigId { get; set; }
    public string? Name { get; init; }
    public string? TitleAssociationsName { get; set; }
    public string? TitleAssociationsId { get; set; }
    public string? ProgressState { get; init; }
    public string? ProgressionRequirementsId { get; set; }
    public string? ProgressionRequirementsCurrent { get; set; }
    public string? ProgressionRequirementsTarget { get; set; }
    public string? ProgressionRequirementsOperationType { get; set; }
    public string? ProgressionRequirementsValueType { get; set; }
    public string? ProgressionRequirementsRuleParticipationType { get; set; }
    public string? ProgressionTimeUnlocked { get; init; }
    public string? MediaAssetsName { get; set; }
    public string? MediaAssetsType { get; set; }
    public string? MediaAssetsUrl { get; set; }
    public List<string>? Platforms { get; set; }
    public string? IsSecret { get; init; }
    public string? Description { get; init; }
    public string? LockedDescription { get; set; }
    public string? ProductId { get; set; }
    public string? AchievementType { get; set; }
    public string? ParticipationType { get; set; }
    public string? TimeWindow { get; set; }
    public string? RewardsName { get; set; }
    public string? RewardsDescription { get; set; }
    public string? RewardsValue { get; init; }
    public string? RewardsType { get; init; }
    public string? RewardsMediaAsset { get; set; }
    public string? RewardsValueType { get; set; }
    public string? EstimatedTime { get; set; }
    public string? DeepLink { get; set; }
    public string? IsRevoked { get; set; }
    public string? RarityCurrentCategory { get; init; }
    public string? RarityCurrentPercentage { get; init; }
}