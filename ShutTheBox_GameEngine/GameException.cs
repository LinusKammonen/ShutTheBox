namespace ShutTheBox_GameEngine;

public class GameException : Exception
{
    public GameException(string message) : base(message) { }
}

public class NotFoundException : GameException
{
    public NotFoundException(string message) : base(message) { }
}
