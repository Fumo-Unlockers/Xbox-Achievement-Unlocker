namespace XAU.Models;

public class Game
{
    public string Title { get; init; } = null!;
    public string Image { get; init; } = null!;
    public string GamerScore { get; init; } = null!;
    public string CurrentAchievements { get; init; } = null!;
    public string Progress { get; set; } = null!;
    public string Index { get; init; } = null!;
}