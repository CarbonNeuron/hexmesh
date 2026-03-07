using HexMesh.Core.Coordinates;

namespace HexMesh.Core.Tests.Coordinates;

public class ChunkCoordTests
{
    [Fact]
    public void FromHex_OriginCell_IsChunkZero()
    {
        var chunk = ChunkCoord.FromHex(new HexCoord(0, 0));
        Assert.Equal(new ChunkCoord(0, 0), chunk);
    }

    [Fact]
    public void FromHex_CellWithinChunk_MapsCorrectly()
    {
        var chunk = ChunkCoord.FromHex(new HexCoord(15, 15));
        Assert.Equal(new ChunkCoord(0, 0), chunk);
    }

    [Fact]
    public void FromHex_CellAtChunkBoundary_MapsToNextChunk()
    {
        var chunk = ChunkCoord.FromHex(new HexCoord(16, 0));
        Assert.Equal(new ChunkCoord(1, 0), chunk);
    }

    [Fact]
    public void FromHex_NegativeCoords_MapsCorrectly()
    {
        var chunk = ChunkCoord.FromHex(new HexCoord(-1, 0));
        Assert.Equal(new ChunkCoord(-1, 0), chunk);
    }

    [Fact]
    public void FromHex_NegativeBoundary_MapsCorrectly()
    {
        var chunk = ChunkCoord.FromHex(new HexCoord(-16, 0));
        Assert.Equal(new ChunkCoord(-1, 0), chunk);
    }

    [Fact]
    public void FromHex_NegativePastBoundary_MapsCorrectly()
    {
        var chunk = ChunkCoord.FromHex(new HexCoord(-17, 0));
        Assert.Equal(new ChunkCoord(-2, 0), chunk);
    }

    [Fact]
    public void Equality_SameChunks_AreEqual()
    {
        var a = new ChunkCoord(1, -1);
        var b = new ChunkCoord(1, -1);
        Assert.Equal(a, b);
    }

    [Fact]
    public void ChunkSize_Is16()
    {
        Assert.Equal(16, ChunkCoord.ChunkSize);
    }
}
