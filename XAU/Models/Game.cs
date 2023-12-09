namespace XAU.Models;

public class Game
{
    public string? Title { get; set; }
    public string? Image { get; set; }
    public string? Progress { get; set; }
    public string? GamerScore { get; init; }
    public string? CurrentAchievements { get; init; }
    public string? Index { get; init; }
}