namespace XAU.Models;

public class DgAchievement
{
    public int Index { get; init; }
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? IsSecret { get; set; }
    public DateTime DateUnlocked { get; set; }
    public int GamerScore { get; set; }
    public float RarityPercentage { get; set; }
    public string? RarityCategory { get; set; }
    public string? ProgressState { get; set; }
    public bool IsUnlockAble { get; set; }
}