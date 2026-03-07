using HexMesh.Core.Simulation;

namespace HexMesh.Server.Simulation;

public record GameOfLifeCell(bool Alive) : ICellState
{
    public CellRenderData GetRenderData() => new(Alive ? 0x00FF00FFu : 0x333333FFu);
}
