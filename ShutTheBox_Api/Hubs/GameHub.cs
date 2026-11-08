using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ShutTheBox_Api.Auth;
using ShutTheBox_GameEngine;

namespace ShutTheBox_Api.Hubs;

[Authorize]
public class GameHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> spectatedGameByConnection = new();

    private readonly GameServices games;
    private readonly RoomServices rooms;
    private readonly GameNotifier notifier;

    public GameHub(GameServices games, RoomServices rooms, GameNotifier notifier)
    {
        this.games = games;
        this.rooms = rooms;
        this.notifier = notifier;
    }

    private string UserId => Context.User!.GetUserId();
    private string UserName => Context.User!.GetUserName();

    public Task JoinLobby() =>
        Groups.AddToGroupAsync(Context.ConnectionId, GameNotifier.LobbyGroup);

    public Task LeaveLobby() =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GameNotifier.LobbyGroup);

    public async Task JoinRoom(string roomCode)
    {
        Run(() => rooms.GetRoom(roomCode));
        await Groups.AddToGroupAsync(Context.ConnectionId, GameNotifier.RoomGroup(roomCode));
    }

    public Task LeaveRoom(string roomCode) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GameNotifier.RoomGroup(roomCode));

    public async Task JoinGame(string gameCode, string? role)
    {
        GameState game = Run(() => games.GetGame(gameCode));
        await Groups.AddToGroupAsync(Context.ConnectionId, GameNotifier.GameGroup(gameCode));

        bool isPlayer = game.Players.Any(player => player.UserId == UserId);

        if (role == "spectator" && !isPlayer)
        {
            game = Run(() => games.AddSpectator(gameCode, UserId, UserName));
            spectatedGameByConnection[Context.ConnectionId] = gameCode;
            await notifier.GameChangedAsync(game);
        }
    }

    public async Task LeaveGame(string gameCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GameNotifier.GameGroup(gameCode));
        await StopSpectatingAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await StopSpectatingAsync();
        await base.OnDisconnectedAsync(exception);
    }

    private async Task StopSpectatingAsync()
    {
        if (spectatedGameByConnection.TryRemove(Context.ConnectionId, out string? gameCode) &&
            games.GetGameFromGameCode(gameCode) != null)
        {
            GameState game = games.RemoveSpectator(gameCode, UserId);
            await notifier.GameChangedAsync(game);
        }
    }

    private static T Run<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (GameException error)
        {
            throw new HubException(error.Message);
        }
    }
}
