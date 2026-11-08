namespace ShutTheBox_GameEngine;

public class Player
{
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsCurrentPlayer { get; set; }
    public bool IsHost { get; set; }
    public bool HasWon { get; set; }
    public bool HasLost { get; set; }
    public bool HasLeft { get; set; }
    public string? ExitReason { get; set; }
    public int CurrentScore => OpenNumbers.Sum();
    public DateTimeOffset? TurnDeadlineUtc { get; set; }
    public List<int> OpenNumbers { get; set; } = Enumerable.Range(1, 9).ToList();
}
