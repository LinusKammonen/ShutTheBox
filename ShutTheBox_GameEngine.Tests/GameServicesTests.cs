using ShutTheBox_GameEngine;

namespace ShutTheBox_GameEngine.Tests;

public class GameServicesTests
{
    private readonly FixedDice dice = new();
    private readonly GameServices services;

    public GameServicesTests()
    {
        services = new GameServices(dice);
    }

    private GameState NewGame(params string[] userIds)
    {
        var players = userIds
            .Select((id, index) => new RoomPlayer { UserId = id, Name = id.ToUpper(), IsHost = index == 0 })
            .ToList();

        return services.CreateGame("ROOM01", "Testrum", players);
    }

    private static Player PlayerIn(GameState game, string userId) =>
        game.Players.Single(p => p.UserId == userId);

    [Fact]
    public void NewGame_FirstPlayerStarts_WithAllTilesOpen()
    {
        var game = NewGame("anna", "bo");

        Assert.Equal("anna", game.CurrentPlayerUserId);
        Assert.True(PlayerIn(game, "anna").IsCurrentPlayer);
        Assert.False(PlayerIn(game, "bo").IsCurrentPlayer);
        Assert.Equal(45, PlayerIn(game, "anna").CurrentScore);
        Assert.True(game.Rules.IsMultiplayer);
        Assert.Null(game.CurrentDiceRoll);
    }

    [Fact]
    public void Roll_TwoDice_SetsDiceAndValidMoves()
    {
        var game = NewGame("anna");
        dice.Next(4, 5);

        services.RollDice(game.GameCode, "anna");

        Assert.Equal(UpdateReasons.DiceRolled, game.UpdateReason);
        Assert.Equal(4, game.CurrentDiceRoll!.Die1);
        Assert.Equal(5, game.CurrentDiceRoll.Die2);
        Assert.Equal(2, game.CurrentDiceRoll.DiceCount);
        Assert.Equal(9, game.CurrentDiceRoll.Total);
        Assert.Equal(8, game.ValidMoves.Count);
    }

    [Fact]
    public void Roll_OneDie_When789AreShut()
    {
        var game = NewGame("anna");
        PlayerIn(game, "anna").OpenNumbers = new List<int> { 1, 2, 3 };
        dice.Next(4);

        services.RollDice(game.GameCode, "anna");

        Assert.Equal(1, game.CurrentDiceRoll!.DiceCount);
        Assert.Equal(0, game.CurrentDiceRoll.Die2);
        Assert.Equal(4, game.CurrentDiceRoll.Total);
    }

    [Fact]
    public void Roll_Twice_WithoutMoving_Throws()
    {
        var game = NewGame("anna");
        dice.Next(4, 5);
        services.RollDice(game.GameCode, "anna");

        Assert.Throws<GameException>(() => services.RollDice(game.GameCode, "anna"));
    }

    [Fact]
    public void Roll_NotYourTurn_Throws()
    {
        var game = NewGame("anna", "bo");

        var error = Assert.Throws<GameException>(() => services.RollDice(game.GameCode, "bo"));
        Assert.Contains("inte din tur", error.Message);
    }

    [Fact]
    public void Roll_UnknownGame_ThrowsNotFound()
    {
        Assert.Throws<NotFoundException>(() => services.RollDice("NOPE00", "anna"));
    }

    [Fact]
    public void Move_Valid_ShutsTilesAndResetsDice()
    {
        var game = NewGame("anna");
        dice.Next(4, 5);
        services.RollDice(game.GameCode, "anna");

        services.MakeMove(game.GameCode, "anna", new[] { 5, 4 });

        Assert.Equal(new[] { 1, 2, 3, 6, 7, 8, 9 }, PlayerIn(game, "anna").OpenNumbers);
        Assert.Equal(36, PlayerIn(game, "anna").CurrentScore);
        Assert.Null(game.CurrentDiceRoll);
        Assert.Empty(game.ValidMoves);
        Assert.Equal(UpdateReasons.MoveMade, game.UpdateReason);
        Assert.Equal("anna", game.CurrentPlayerUserId);
    }

    [Fact]
    public void Move_WrongSum_Throws()
    {
        var game = NewGame("anna");
        dice.Next(4, 5);
        services.RollDice(game.GameCode, "anna");

        Assert.Throws<GameException>(() => services.MakeMove(game.GameCode, "anna", new[] { 1, 2 }));
        Assert.Equal(9, PlayerIn(game, "anna").OpenNumbers.Count);
    }

    [Fact]
    public void Move_NoTilesSelected_ThrowsClearMessage()
    {
        var game = NewGame("anna");
        dice.Next(6, 6);
        services.RollDice(game.GameCode, "anna");

        var error = Assert.Throws<GameException>(() => services.MakeMove(game.GameCode, "anna", new int[0]));
        Assert.Contains("Välj brickor som tillsammans blir 12", error.Message);
    }

    [Fact]
    public void Move_BeforeRoll_Throws()
    {
        var game = NewGame("anna");

        Assert.Throws<GameException>(() => services.MakeMove(game.GameCode, "anna", new[] { 9 }));
    }

    [Fact]
    public void NoValidMoves_PlayerIsOut_AndTurnPasses()
    {
        var game = NewGame("anna", "bo");
        PlayerIn(game, "anna").OpenNumbers = new List<int> { 9 };
        dice.Next(1, 1);

        services.RollDice(game.GameCode, "anna");

        var anna = PlayerIn(game, "anna");
        Assert.False(anna.IsActive);
        Assert.Equal(ExitReasons.NoValidMoves, anna.ExitReason);
        Assert.Equal("bo", game.CurrentPlayerUserId);
        Assert.True(PlayerIn(game, "bo").IsCurrentPlayer);
        Assert.Null(game.CurrentDiceRoll);
        Assert.False(game.IsGameOver);
    }

    [Fact]
    public void ShutTheBox_ScoreZero_AndWins()
    {
        var game = NewGame("anna");
        PlayerIn(game, "anna").OpenNumbers = new List<int> { 1, 2 };
        dice.Next(3);
        services.RollDice(game.GameCode, "anna");

        services.MakeMove(game.GameCode, "anna", new[] { 1, 2 });

        var result = services.GetResult(game.GameCode);
        Assert.True(game.IsGameOver);
        Assert.Equal(UpdateReasons.GameFinished, game.UpdateReason);
        Assert.Equal(ExitReasons.BoxShut, PlayerIn(game, "anna").ExitReason);
        Assert.Equal(0, result.Players[0].FinalScore);
        Assert.True(result.Players[0].IsWinner);
    }

    [Fact]
    public void GameFinished_LowestScoreWins_WithPlacements()
    {
        var game = NewGame("anna", "bo", "cia");
        PlayerIn(game, "anna").OpenNumbers = new List<int> { 9 };
        PlayerIn(game, "bo").OpenNumbers = new List<int> { 5 };
        PlayerIn(game, "cia").OpenNumbers = new List<int> { 8, 9 };

        dice.Next(1, 1); services.RollDice(game.GameCode, "anna");
        dice.Next(1); services.RollDice(game.GameCode, "bo");
        dice.Next(1, 1); services.RollDice(game.GameCode, "cia");

        var result = services.GetResult(game.GameCode);
        Assert.Equal(new[] { "bo", "anna", "cia" }, result.Players.Select(p => p.UserId));
        Assert.Equal(new[] { 1, 2, 3 }, result.Players.Select(p => p.Placement));
        Assert.True(PlayerIn(game, "bo").HasWon);
        Assert.True(PlayerIn(game, "anna").HasLost);
        Assert.False(result.TiedWinners);
        Assert.Equal(EndReasons.Completed, result.EndReason);
    }

    [Fact]
    public void GameFinished_SameScore_TiedWinners()
    {
        var game = NewGame("anna", "bo");
        PlayerIn(game, "anna").OpenNumbers = new List<int> { 9 };
        PlayerIn(game, "bo").OpenNumbers = new List<int> { 9 };

        dice.Next(1, 1); services.RollDice(game.GameCode, "anna");
        dice.Next(1, 1); services.RollDice(game.GameCode, "bo");

        var result = services.GetResult(game.GameCode);
        Assert.True(result.TiedWinners);
        Assert.All(result.Players, p => Assert.Equal(1, p.Placement));
        Assert.All(result.Players, p => Assert.True(p.IsWinner));
    }

    [Fact]
    public void CurrentPlayerLeaves_TurnPasses_AndLeaverIsPlacedLast()
    {
        var game = NewGame("anna", "bo");
        PlayerIn(game, "bo").OpenNumbers = new List<int> { 9 };

        services.LeaveGame(game.GameCode, "anna");

        Assert.True(PlayerIn(game, "anna").HasLeft);
        Assert.Equal("bo", game.CurrentPlayerUserId);

        dice.Next(1, 1);
        services.RollDice(game.GameCode, "bo");

        var result = services.GetResult(game.GameCode);
        Assert.Equal("bo", result.Players[0].UserId);
        Assert.True(result.Players[1].Abandoned);
        Assert.Equal(2, result.Players[1].Placement);
    }

    [Fact]
    public void AllPlayersLeave_GameIsAbandoned()
    {
        var game = NewGame("anna");

        services.LeaveGame(game.GameCode, "anna");

        Assert.True(game.IsGameOver);
        Assert.Equal(EndReasons.Abandoned, services.GetResult(game.GameCode).EndReason);
    }

    [Fact]
    public void GameFinishedEvent_FiresOnce()
    {
        int fired = 0;
        services.GameFinished += (_, _) => fired++;
        var game = NewGame("anna");
        PlayerIn(game, "anna").OpenNumbers = new List<int> { 9 };

        dice.Next(1, 1);
        services.RollDice(game.GameCode, "anna");
        services.LeaveGame(game.GameCode, "anna");

        Assert.Equal(1, fired);
    }

    [Fact]
    public void Spectators_CanJoinAndLeave_ButPlayersAreNotAdded()
    {
        var game = NewGame("anna");

        services.AddSpectator(game.GameCode, "eva", "Eva");
        services.AddSpectator(game.GameCode, "eva", "Eva");
        services.AddSpectator(game.GameCode, "anna", "Anna");
        Assert.Single(game.Spectators);

        services.LeaveGame(game.GameCode, "eva");
        Assert.Empty(game.Spectators);
        Assert.False(game.IsGameOver);
    }

    [Fact]
    public void ActiveAndCurrentGames_ExcludeFinishedGames()
    {
        var game = NewGame("anna");
        Assert.Same(game, services.GetCurrentGameForUser("anna"));
        Assert.Single(services.GetActiveGames());

        services.LeaveGame(game.GameCode, "anna");

        Assert.Null(services.GetCurrentGameForUser("anna"));
        Assert.Empty(services.GetActiveGames());
    }
}
