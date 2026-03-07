// src/HexMesh.Core/Storage/IWorldStorage.cs
using HexMesh.Core.Coordinates;
using HexMesh.Core.Simulation;

namespace HexMesh.Core.Storage;

public interface IWorldStorage<TCell> where TCell : ICellState
{
    TCell? Get(HexCoord coord);
    void Set(HexCoord coord, TCell state);
    void Clear(HexCoord coord);
    IEnumerable<(HexCoord Coord, TCell State)> GetInChunk(ChunkCoord chunk);
    IEnumerable<(HexCoord Coord, TCell State)> GetAllCells();
    IReadOnlySet<ChunkCoord> GetActiveChunks();
}
