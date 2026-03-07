// src/HexMesh.Core/Simulation/ISimulation.cs
namespace HexMesh.Core.Simulation;

public interface ISimulation<TCell> where TCell : ICellState
{
    void Step(IWorldAccess<TCell> world);
}
