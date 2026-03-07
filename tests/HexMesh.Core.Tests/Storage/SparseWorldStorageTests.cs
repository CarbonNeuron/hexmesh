// tests/HexMesh.Core.Tests/Storage/SparseWorldStorageTests.cs
using HexMesh.Core.Coordinates;
using HexMesh.Core.Simulation;
using HexMesh.Core.Storage;

namespace HexMesh.Core.Tests.Storage;

public record TestCell(bool Alive) : ICellState
{
    public CellRenderData GetRenderData() => new(Alive ? 0xFFFFFFFF : 0x000000FF);
}

public class SparseWorldStorageTests
{
    private readonly SparseWorldStorage<TestCell> _storage = new();

    [Fact]
    public void Get_EmptyWorld_ReturnsNull()
    {
        Assert.Null(_storage.Get(new HexCoord(0, 0)));
    }

    [Fact]
    public void Set_ThenGet_ReturnsCellState()
    {
        var coord = new HexCoord(1, 2);
        var cell = new TestCell(true);
        _storage.Set(coord, cell);
        Assert.Equal(cell, _storage.Get(coord));
    }

    [Fact]
    public void Set_Overwrite_ReturnsNewState()
    {
        var coord = new HexCoord(1, 2);
        _storage.Set(coord, new TestCell(true));
        _storage.Set(coord, new TestCell(false));
        Assert.Equal(new TestCell(false), _storage.Get(coord));
    }

    [Fact]
    public void Clear_RemovesCell()
    {
        var coord = new HexCoord(1, 2);
        _storage.Set(coord, new TestCell(true));
        _storage.Clear(coord);
        Assert.Null(_storage.Get(coord));
    }

    [Fact]
    public void Clear_NonExistent_DoesNotThrow()
    {
        _storage.Clear(new HexCoord(99, 99));
    }

    [Fact]
    public void GetInChunk_ReturnsOnlyCellsInThatChunk()
    {
        var inChunk = new HexCoord(5, 5);
        var outOfChunk = new HexCoord(20, 5);

        _storage.Set(inChunk, new TestCell(true));
        _storage.Set(outOfChunk, new TestCell(true));

        var results = _storage.GetInChunk(new ChunkCoord(0, 0)).ToList();
        Assert.Single(results);
        Assert.Equal(inChunk, results[0].Coord);
    }

    [Fact]
    public void GetActiveChunks_ReturnsChunksWithCells()
    {
        _storage.Set(new HexCoord(0, 0), new TestCell(true));
        _storage.Set(new HexCoord(20, 0), new TestCell(true));

        var chunks = _storage.GetActiveChunks();
        Assert.Equal(2, chunks.Count);
        Assert.Contains(new ChunkCoord(0, 0), chunks);
        Assert.Contains(new ChunkCoord(1, 0), chunks);
    }

    [Fact]
    public void GetActiveChunks_AfterClear_RemovesEmptyChunk()
    {
        var coord = new HexCoord(0, 0);
        _storage.Set(coord, new TestCell(true));
        _storage.Clear(coord);
        Assert.Empty(_storage.GetActiveChunks());
    }

    [Fact]
    public void GetAllCells_ReturnsAllCells()
    {
        _storage.Set(new HexCoord(0, 0), new TestCell(true));
        _storage.Set(new HexCoord(5, 5), new TestCell(false));
        var all = _storage.GetAllCells().ToList();
        Assert.Equal(2, all.Count);
    }
}
