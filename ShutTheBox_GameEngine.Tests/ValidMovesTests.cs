using ShutTheBox_GameEngine;

namespace ShutTheBox_GameEngine.Tests;

public class ValidMovesTests
{
    private static readonly int[] AllOpen = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

    [Fact]
    public void Sum9_AllOpen_GivesSameEightMovesAsOldApi()
    {
        var moves = GameServices.FindValidMoves(AllOpen, 9);

        Assert.Equal(8, moves.Count);
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 9 }));
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 1, 8 }));
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 2, 7 }));
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 3, 6 }));
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 4, 5 }));
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 1, 2, 6 }));
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 1, 3, 5 }));
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 2, 3, 4 }));
    }

    [Fact]
    public void Sum6_AllOpen_FindsThreeTileMove()
    {
        var moves = GameServices.FindValidMoves(AllOpen, 6);

        Assert.Equal(4, moves.Count);
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 1, 2, 3 }));
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 1, 5 }));
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 2, 4 }));
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 6 }));
    }

    [Fact]
    public void Sum10_AllOpen_FindsFourTileMove()
    {
        var moves = GameServices.FindValidMoves(AllOpen, 10);

        Assert.Contains(moves, m => m.SequenceEqual(new[] { 1, 2, 3, 4 }));
    }

    [Fact]
    public void ClosedTiles_AreNeverUsed()
    {
        var moves = GameServices.FindValidMoves(new[] { 1, 3, 4, 5, 6, 7, 8, 9 }, 6);

        Assert.Equal(2, moves.Count);
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 1, 5 }));
        Assert.Contains(moves, m => m.SequenceEqual(new[] { 6 }));
    }

    [Fact]
    public void NoCombination_GivesEmptyList()
    {
        var moves = GameServices.FindValidMoves(new[] { 5, 9 }, 3);

        Assert.Empty(moves);
    }

    [Fact]
    public void Moves_HaveNoDuplicatesOrReusedTiles()
    {
        var moves = GameServices.FindValidMoves(AllOpen, 12);

        Assert.All(moves, m => Assert.Equal(m.Count, m.Distinct().Count()));
        Assert.Equal(moves.Count, moves.Select(m => string.Join(",", m)).Distinct().Count());
        Assert.All(moves, m => Assert.Equal(12, m.Sum()));
    }
}
