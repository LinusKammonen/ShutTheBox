using ShutTheBox_GameEngine;

namespace ShutTheBox_Api;

public record LoginRequest(string? Username);
public record LoginResponse(string Token, string UserId, string Username);
public record MeResponse(string UserId, string Username);

public record CreateRoomRequest(string? Name, int? MinPlayers, int? MaxPlayers);

public record MoveRequest(List<int>? SelectedNumbers);

public record RoomUpdate(Room Room, string UpdateReason, string? GameCode);

public static class RoomUpdateReasons
{
    public const string PlayerJoined = "PlayerJoined";
    public const string PlayerLeft = "PlayerLeft";
    public const string GameStarted = "GameStarted";
}
