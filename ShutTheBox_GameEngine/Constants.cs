namespace ShutTheBox_GameEngine;

public static class UpdateReasons
{
    public const string GameStarted = "GameStarted";
    public const string DiceRolled = "DiceRolled";
    public const string MoveMade = "MoveMade";
    public const string NoValidMoves = "NoValidMoves";
    public const string PlayerLeft = "PlayerLeft";
    public const string SpectatorJoined = "SpectatorJoined";
    public const string SpectatorLeft = "SpectatorLeft";
    public const string GameFinished = "GameFinished";
}

public static class ExitReasons
{
    public const string NoValidMoves = "NoValidMoves";
    public const string BoxShut = "BoxShut";
    public const string Left = "Left";
}

public static class EndReasons
{
    public const string Completed = "Completed";
    public const string Abandoned = "Abandoned";
}

public static class RoomStatuses
{
    public const string Open = "Open";
    public const string Playing = "Playing";
    public const string Finished = "Finished";
    public const string Closed = "Closed";
}
