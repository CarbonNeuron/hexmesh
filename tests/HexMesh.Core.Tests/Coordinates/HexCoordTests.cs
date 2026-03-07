// tests/HexMesh.Core.Tests/Coordinates/HexCoordTests.cs
using HexMesh.Core.Coordinates;

namespace HexMesh.Core.Tests.Coordinates;

public class HexCoordTests
{
    [Fact]
    public void Constructor_SetsQAndR()
    {
        var coord = new HexCoord(3, -2);
        Assert.Equal(3, coord.Q);
        Assert.Equal(-2, coord.R);
    }

    [Fact]
    public void S_IsComputedFromQAndR()
    {
        var coord = new HexCoord(3, -2);
        Assert.Equal(-1, coord.S);
    }

    [Fact]
    public void Equality_SameCoords_AreEqual()
    {
        var a = new HexCoord(1, 2);
        var b = new HexCoord(1, 2);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentCoords_AreNotEqual()
    {
        var a = new HexCoord(1, 2);
        var b = new HexCoord(2, 1);
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void GetHashCode_SameCoords_SameHash()
    {
        var a = new HexCoord(1, 2);
        var b = new HexCoord(1, 2);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Neighbors_ReturnsAllSix()
    {
        var center = new HexCoord(0, 0);
        var neighbors = center.Neighbors().ToList();
        Assert.Equal(6, neighbors.Count);
        Assert.Contains(new HexCoord(1, 0), neighbors);
        Assert.Contains(new HexCoord(1, -1), neighbors);
        Assert.Contains(new HexCoord(0, -1), neighbors);
        Assert.Contains(new HexCoord(-1, 0), neighbors);
        Assert.Contains(new HexCoord(-1, 1), neighbors);
        Assert.Contains(new HexCoord(0, 1), neighbors);
    }

    [Fact]
    public void DistanceTo_SameCell_IsZero()
    {
        var a = new HexCoord(3, -1);
        Assert.Equal(0, a.DistanceTo(a));
    }

    [Fact]
    public void DistanceTo_Adjacent_IsOne()
    {
        var a = new HexCoord(0, 0);
        var b = new HexCoord(1, 0);
        Assert.Equal(1, a.DistanceTo(b));
    }

    [Fact]
    public void DistanceTo_FarAway_IsCorrect()
    {
        var a = new HexCoord(0, 0);
        var b = new HexCoord(3, -3);
        Assert.Equal(3, a.DistanceTo(b));
    }

    [Fact]
    public void Add_CombinesCoords()
    {
        var a = new HexCoord(1, 2);
        var b = new HexCoord(3, -1);
        var result = a + b;
        Assert.Equal(new HexCoord(4, 1), result);
    }

    [Fact]
    public void Subtract_SubtractsCoords()
    {
        var a = new HexCoord(4, 1);
        var b = new HexCoord(1, 2);
        var result = a - b;
        Assert.Equal(new HexCoord(3, -1), result);
    }
}
