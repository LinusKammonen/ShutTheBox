using Microsoft.AspNetCore.SignalR;
using ShutTheBox_GameEngine;

namespace ShutTheBox_Api.Hubs;

public class GameNotifier
{
    public const string LobbyGroup = "lobby";
    public static string RoomGroup(string roomCode) => $"room:{roomCode}";
    public static string GameGroup(string gameCode) => $"game:{gameCode}";

    private readonly IHubContext<GameHub> hub;

    public GameNotifier(IHubContext<GameHub> hub)
    {
        this.hub = hub;
    }

    public Task LobbyChangedAsync()
    {
        return hub.Clients.Group(LobbyGroup).SendAsync("LobbyUpdated", new { updatedAtUtc = DateTimeOffset.UtcNow });
    }

    public async Task RoomChangedAsync(Room room, string updateReason, string? gameCode = null)
    {
        await hub.Clients.Group(RoomGroup(room.RoomCode)).SendAsync("RoomUpdated", new RoomUpdate(room, updateReason, gameCode));
        await LobbyChangedAsync();
    }

    public Task GameChangedAsync(GameState game)
    {
        return hub.Clients.Group(GameGroup(game.GameCode)).SendAsync("GameUpdated", game);
    }

    public async Task GameFinishedAsync(GameState game, GameResult result)
    {
        await GameChangedAsync(game);
        await hub.Clients.Group(GameGroup(game.GameCode)).SendAsync("GameFinished", result);
        await LobbyChangedAsync();
    }
}
