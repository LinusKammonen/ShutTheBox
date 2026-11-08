using ShutTheBox_GameEngine;

namespace ShutTheBox_GameEngine.Tests;

public class RoomServicesTests
{
    private readonly FixedDice dice = new();
    private readonly GameServices games;
    private readonly RoomServices rooms;

    public RoomServicesTests()
    {
        games = new GameServices(dice);
        rooms = new RoomServices(games);
    }

    [Fact]
    public void CreateMultiplayerRoom_CreatorIsHost_NoGameYet()
    {
        var result = rooms.CreateRoom("Mitt rum", 2, 4, "anna", "Anna");

        Assert.Null(result.State);
        Assert.Equal(RoomStatuses.Open, result.Room.Status);
        Assert.True(result.Room.Players.Single().IsHost);
        Assert.Single(rooms.GetOpenRooms());
    }

    [Fact]
    public void CreateSingleplayerRoom_StartsGameDirectly()
    {
        var result = rooms.CreateRoom("Solo", 1, 1, "anna", "Anna");

        Assert.NotNull(result.State);
        Assert.False(result.State!.Rules.IsMultiplayer);
        Assert.Equal(RoomStatuses.Playing, result.Room.Status);
        Assert.Empty(rooms.GetOpenRooms());
    }

    [Theory]
    [InlineData("", 1, "Anna", "Annas singelplayerrum")]
    [InlineData("Limpans rum", 1, "Anna", "Annas singelplayerrum")]
    [InlineData("", 1, "Linus", "Linus singelplayerrum")]
    [InlineData("", 1, "Max", "Max singelplayerrum")]
    [InlineData("", 4, "Anna", "Annas rum")]
    [InlineData("  Fredagsspel ", 4, "Anna", "Fredagsspel")]
    public void RoomName_IsBasedOnUserName(string name, int maxPlayers, string userName, string expected)
    {
        Assert.Equal(expected, RoomServices.CreateRoomName(name, maxPlayers, userName));
    }

    [Fact]
    public void CreateSingleplayerRoom_GameGetsUserBasedName()
    {
        var result = rooms.CreateRoom("", 1, 1, "anna", "Anna");

        Assert.Equal("Annas singelplayerrum", result.Room.Name);
        Assert.Equal("Annas singelplayerrum", result.State!.RoomName);
    }

    [Theory]
    [InlineData(2, 9)]
    [InlineData(1, 4)]
    [InlineData(5, 4)]
    public void CreateRoom_InvalidLimits_Throws(int min, int max)
    {
        Assert.Throws<GameException>(() => rooms.CreateRoom("x", min, max, "anna", "Anna"));
    }

    [Fact]
    public void Join_IsIdempotent_AndRespectsMaxPlayers()
    {
        var room = rooms.CreateRoom("Rum", 2, 2, "anna", "Anna").Room;

        rooms.JoinRoom(room.RoomCode, "anna", "Anna");
        rooms.JoinRoom(room.RoomCode, "bo", "Bo");

        Assert.Equal(2, room.CurrentPlayers);
        Assert.Throws<GameException>(() => rooms.JoinRoom(room.RoomCode, "cia", "Cia"));
    }

    [Fact]
    public void CannotBeInTwoRooms()
    {
        rooms.CreateRoom("Rum 1", 2, 4, "anna", "Anna");
        var room2 = rooms.CreateRoom("Rum 2", 2, 4, "bo", "Bo").Room;

        Assert.Throws<GameException>(() => rooms.JoinRoom(room2.RoomCode, "anna", "Anna"));
    }

    [Fact]
    public void OnlyHostCanStart_AndOnlyWithMinPlayers()
    {
        var room = rooms.CreateRoom("Rum", 3, 4, "anna", "Anna").Room;
        rooms.JoinRoom(room.RoomCode, "bo", "Bo");

        Assert.Throws<GameException>(() => rooms.StartGame(room.RoomCode, "anna"));

        rooms.JoinRoom(room.RoomCode, "cia", "Cia");
        Assert.Throws<GameException>(() => rooms.StartGame(room.RoomCode, "bo"));

        var game = rooms.StartGame(room.RoomCode, "anna");

        Assert.Equal(RoomStatuses.Playing, room.Status);
        Assert.Equal(game.GameCode, room.GameCode);
        Assert.Equal(3, game.Players.Count);
        Assert.Equal("anna", game.CurrentPlayerUserId);
        Assert.Throws<GameException>(() => rooms.JoinRoom(room.RoomCode, "dan", "Dan"));
    }

    [Fact]
    public void HostLeaves_NextPlayerBecomesHost_EmptyRoomIsRemoved()
    {
        var room = rooms.CreateRoom("Rum", 2, 4, "anna", "Anna").Room;
        rooms.JoinRoom(room.RoomCode, "bo", "Bo");

        rooms.LeaveRoom(room.RoomCode, "anna");
        Assert.True(room.Players.Single().IsHost);

        rooms.LeaveRoom(room.RoomCode, "bo");
        Assert.Throws<NotFoundException>(() => rooms.GetRoom(room.RoomCode));
    }

    [Fact]
    public void WhenGameFinishes_RoomIsFinished_AndUserCanPlayAgain()
    {
        var result = rooms.CreateRoom("Solo", 1, 1, "anna", "Anna");
        result.State!.Players[0].OpenNumbers = new List<int> { 9 };

        dice.Next(1, 1);
        games.RollDice(result.State.GameCode, "anna");

        Assert.Equal(RoomStatuses.Finished, result.Room.Status);
        rooms.CreateRoom("Solo igen", 1, 1, "anna", "Anna");
    }
}
