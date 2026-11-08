namespace ShutTheBox_GameEngine;

public class Rules
{
    public bool IsMultiplayer { get; set; }
    public bool HasTurnTimer { get; set; }
    public int? DiceRollTimeSeconds { get; set; }
    public int? ThinkTimeSeconds { get; set; }
}
