// src/HexMesh.Core/Simulation/SimulationEngine.cs
using HexMesh.Core.Coordinates;
using HexMesh.Core.Storage;

namespace HexMesh.Core.Simulation;

public readonly record struct CellChange(HexCoord Coord, ChunkCoord Chunk, CellRenderData? RenderData);

public class SimulationEngine<TCell> where TCell : ICellState
{
    private readonly IWorldStorage<TCell> _storage;
    private readonly ISimulation<TCell> _simulation;

    public SimulationEngine(IWorldStorage<TCell> storage, ISimulation<TCell> simulation)
    {
        _storage = storage;
        _simulation = simulation;
    }

    public Task<List<CellChange>> StepAsync()
    {
        var accessor = new WorldAccessor<TCell>(_storage);
        _simulation.Step(accessor);
        return Task.FromResult(accessor.GetChanges());
    }
}
