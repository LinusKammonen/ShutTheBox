namespace ShutTheBox_GameEngine;

public class GameState
{
    public string GameCode { get; set; } = "";
    public string RoomCode { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string UpdateReason { get; set; } = UpdateReasons.GameStarted;
    public CurrentDiceRoll? CurrentDiceRoll { get; set; }
    public List<List<int>> ValidMoves { get; set; } = new();
    public Rules Rules { get; set; } = new();
    public string CurrentPlayerUserId { get; set; } = "";
    public string CurrentPlayerName { get; set; } = "";
    public bool IsStarted { get; set; }
    public bool IsGameOver { get; set; }
    public bool TiedWinners { get; set; }
    public List<Player> Players { get; set; } = new();
    public List<Spectator> Spectators { get; set; } = new();
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }

    public int PlayerCount => Players.Count;
    public bool IsMultiPlayer => Rules.IsMultiplayer;
}
