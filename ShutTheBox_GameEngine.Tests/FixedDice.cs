using ShutTheBox_GameEngine;

namespace ShutTheBox_GameEngine.Tests;

public class FixedDice : IDice
{
    private readonly Queue<int> values = new();

    public void Next(params int[] dice)
    {
        foreach (int value in dice)
        {
            values.Enqueue(value);
        }
    }

    public int Roll()
    {
        if (values.Count == 0)
        {
            throw new InvalidOperationException("Testet har inte bestämt fler tärningsvärden.");
        }

        return values.Dequeue();
    }
}
