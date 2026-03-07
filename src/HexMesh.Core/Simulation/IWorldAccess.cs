// src/HexMesh.Core/Simulation/IWorldAccess.cs
using HexMesh.Core.Coordinates;

namespace HexMesh.Core.Simulation;

public interface IWorldAccess<TCell> where TCell : ICellState
{
    TCell? Get(HexCoord coord);
    void Set(HexCoord coord, TCell state);
    void Clear(HexCoord coord);
    IEnumerable<HexCoord> GetNeighbors(HexCoord coord);
    IEnumerable<(HexCoord Coord, TCell State)> GetAllCells();
}
