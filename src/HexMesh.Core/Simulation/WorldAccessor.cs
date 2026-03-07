// src/HexMesh.Core/Simulation/WorldAccessor.cs
using HexMesh.Core.Coordinates;
using HexMesh.Core.Storage;

namespace HexMesh.Core.Simulation;

internal class WorldAccessor<TCell> : IWorldAccess<TCell> where TCell : ICellState
{
    private readonly IWorldStorage<TCell> _storage;
    private readonly Dictionary<HexCoord, CellRenderData?> _changes = new();

    public WorldAccessor(IWorldStorage<TCell> storage)
    {
        _storage = storage;
    }

    public TCell? Get(HexCoord coord) => _storage.Get(coord);

    public void Set(HexCoord coord, TCell state)
    {
        _storage.Set(coord, state);
        _changes[coord] = state.GetRenderData();
    }

    public void Clear(HexCoord coord)
    {
        _storage.Clear(coord);
        _changes[coord] = null;
    }

    public IEnumerable<HexCoord> GetNeighbors(HexCoord coord) => coord.Neighbors();

    public IEnumerable<(HexCoord Coord, TCell State)> GetAllCells() => _storage.GetAllCells();

    public List<CellChange> GetChanges()
    {
        var result = new List<CellChange>(_changes.Count);
        foreach (var (coord, renderData) in _changes)
            result.Add(new CellChange(coord, ChunkCoord.FromHex(coord), renderData));
        return result;
    }
}
