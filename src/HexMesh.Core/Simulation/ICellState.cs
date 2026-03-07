// src/HexMesh.Core/Simulation/ICellState.cs
namespace HexMesh.Core.Simulation;

public interface ICellState
{
    CellRenderData GetRenderData();
}
