using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShutTheBox_Api.Auth;
using ShutTheBox_Api.Hubs;
using ShutTheBox_GameEngine;

namespace ShutTheBox_Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/games")]
public class GamesController : GameControllerBase
{
    private readonly GameServices games;
    private readonly RoomServices rooms;
    private readonly GameNotifier notifier;

    public GamesController(GameServices games, RoomServices rooms, GameNotifier notifier)
    {
        this.games = games;
        this.rooms = rooms;
        this.notifier = notifier;
    }

    [HttpGet("active")]
    public List<GameState> GetActiveGames() => games.GetActiveGames();

    [HttpGet("current")]
    public ActionResult<GameState> GetCurrentGame()
    {
        GameState? game = games.GetCurrentGameForUser(User.GetUserId());

        if (game == null)
        {
            return Problem(title: "Hittades inte", detail: "Du har inget pågående spel.", statusCode: StatusCodes.Status404NotFound);
        }

        return game;
    }

    [HttpPost("from-room/{roomCode}/start")]
    public Task<ActionResult<GameState>> StartGameFromRoom(string roomCode) => RunAsync(async () =>
    {
        GameState game = rooms.StartGame(roomCode, User.GetUserId());
        Room room = rooms.GetRoom(roomCode);

        await notifier.RoomChangedAsync(room, RoomUpdateReasons.GameStarted, game.GameCode);
        await notifier.GameChangedAsync(game);
        return game;
    });

    [HttpGet("{gameCode}")]
    public ActionResult<GameResult> GetFinishedGame(string gameCode) =>
        Run(() => games.GetResult(gameCode));

    [HttpGet("{gameCode}/state")]
    public ActionResult<GameState> GetState(string gameCode) =>
        Run(() => games.GetGame(gameCode));

    [HttpPost("{gameCode}/roll")]
    public Task<ActionResult<GameState>> Roll(string gameCode) => RunAsync(async () =>
    {
        GameState game = games.RollDice(gameCode, User.GetUserId());
        await PublishAsync(game);
        return game;
    });

    [HttpPost("{gameCode}/move")]
    public Task<ActionResult<GameState>> Move(string gameCode, MoveRequest request) => RunAsync(async () =>
    {
        GameState game = games.MakeMove(gameCode, User.GetUserId(), request.SelectedNumbers ?? new List<int>());
        await PublishAsync(game);
        return game;
    });

    [HttpPost("{gameCode}/leave")]
    public Task<ActionResult<GameState>> Leave(string gameCode) => RunAsync(async () =>
    {
        GameState game = games.LeaveGame(gameCode, User.GetUserId());
        await PublishAsync(game);
        return game;
    });

    private Task PublishAsync(GameState game)
    {
        return game.IsGameOver ? Task.CompletedTask : notifier.GameChangedAsync(game);
    }
}
