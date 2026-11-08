namespace ShutTheBox_GameEngine;

public class Room
{
    public string RoomCode { get; set; } = "";
    public string Name { get; set; } = "";
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string Status { get; set; } = RoomStatuses.Open;
    public string? GameCode { get; set; }
    public List<RoomPlayer> Players { get; set; } = new();
    public int CurrentPlayers => Players.Count;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class RoomPlayer
{
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsHost { get; set; }
}

public record CreateRoomResult(Room Room, GameState? State);
