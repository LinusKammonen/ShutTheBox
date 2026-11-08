namespace ShutTheBox_GameEngine;

public class GameResult
{
    public string GameCode { get; set; } = "";
    public string RoomCode { get; set; } = "";
    public Guid GameSessionId { get; set; } = Guid.NewGuid();
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset EndedAtUtc { get; set; }
    public int PlayerCount { get; set; }
    public string EndReason { get; set; } = EndReasons.Completed;
    public bool HasWinner { get; set; }
    public bool TiedWinners { get; set; }
    public List<PlayerResult> Players { get; set; } = new();
}

public class PlayerResult
{
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsWinner { get; set; }
    public int Placement { get; set; }
    public int FinalScore { get; set; }
    public string? ExitReason { get; set; }
    public bool Abandoned { get; set; }
}
