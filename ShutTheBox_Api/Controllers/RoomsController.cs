using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShutTheBox_Api.Auth;
using ShutTheBox_Api.Hubs;
using ShutTheBox_GameEngine;

namespace ShutTheBox_Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/rooms")]
public class RoomsController : GameControllerBase
{
    private readonly RoomServices rooms;
    private readonly GameNotifier notifier;

    public RoomsController(RoomServices rooms, GameNotifier notifier)
    {
        this.rooms = rooms;
        this.notifier = notifier;
    }

    [HttpPost]
    public Task<ActionResult<CreateRoomResult>> CreateRoom(CreateRoomRequest request) => RunAsync(async () =>
    {
        int maxPlayers = request.MaxPlayers ?? request.MinPlayers ?? 1;
        int minPlayers = request.MinPlayers ?? (maxPlayers == 1 ? 1 : 2);

        CreateRoomResult result = rooms.CreateRoom(request.Name ?? "", minPlayers, maxPlayers, User.GetUserId(), User.GetUserName());

        await notifier.LobbyChangedAsync();
        return result;
    });

    [HttpGet("open")]
    public List<Room> GetOpenRooms() => rooms.GetOpenRooms();

    [HttpGet("{roomCode}")]
    public ActionResult<Room> GetRoom(string roomCode) =>
        Run(() => rooms.GetRoom(roomCode));

    [HttpPost("{roomCode}/join")]
    public Task<ActionResult<Room>> JoinRoom(string roomCode) => RunAsync(async () =>
    {
        Room room = rooms.JoinRoom(roomCode, User.GetUserId(), User.GetUserName());
        await notifier.RoomChangedAsync(room, RoomUpdateReasons.PlayerJoined);
        return room;
    });

    [HttpPost("{roomCode}/leave")]
    public Task<ActionResult<Room>> LeaveRoom(string roomCode) => RunAsync(async () =>
    {
        Room room = rooms.LeaveRoom(roomCode, User.GetUserId());
        await notifier.RoomChangedAsync(room, RoomUpdateReasons.PlayerLeft);
        return room;
    });
}
