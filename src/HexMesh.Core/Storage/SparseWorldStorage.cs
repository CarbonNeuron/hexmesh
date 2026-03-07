// src/HexMesh.Core/Storage/SparseWorldStorage.cs
using HexMesh.Core.Coordinates;
using HexMesh.Core.Simulation;

namespace HexMesh.Core.Storage;

public class SparseWorldStorage<TCell> : IWorldStorage<TCell> where TCell : ICellState
{
    private readonly Dictionary<HexCoord, TCell> _cells = new();
    private readonly Dictionary<ChunkCoord, HashSet<HexCoord>> _chunkIndex = new();

    public TCell? Get(HexCoord coord)
    {
        return _cells.TryGetValue(coord, out var cell) ? cell : default;
    }

    public void Set(HexCoord coord, TCell state)
    {
        _cells[coord] = state;
        var chunk = ChunkCoord.FromHex(coord);
        if (!_chunkIndex.TryGetValue(chunk, out var set))
        {
            set = new HashSet<HexCoord>();
            _chunkIndex[chunk] = set;
        }
        set.Add(coord);
    }

    public void Clear(HexCoord coord)
    {
        if (!_cells.Remove(coord))
            return;

        var chunk = ChunkCoord.FromHex(coord);
        if (_chunkIndex.TryGetValue(chunk, out var set))
        {
            set.Remove(coord);
            if (set.Count == 0)
                _chunkIndex.Remove(chunk);
        }
    }

    public IEnumerable<(HexCoord Coord, TCell State)> GetInChunk(ChunkCoord chunk)
    {
        if (!_chunkIndex.TryGetValue(chunk, out var set))
            yield break;

        foreach (var coord in set)
            yield return (coord, _cells[coord]);
    }

    public IEnumerable<(HexCoord Coord, TCell State)> GetAllCells()
    {
        foreach (var kvp in _cells)
            yield return (kvp.Key, kvp.Value);
    }

    public IReadOnlySet<ChunkCoord> GetActiveChunks()
    {
        return _chunkIndex.Keys.ToHashSet();
    }
}
