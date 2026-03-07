// tests/HexMesh.Core.Tests/Simulation/SimulationEngineTests.cs
using HexMesh.Core.Coordinates;
using HexMesh.Core.Simulation;
using HexMesh.Core.Storage;

namespace HexMesh.Core.Tests.Simulation;

public record EngineTestCell(int Value) : ICellState
{
    public CellRenderData GetRenderData() => new((uint)Value);
}

public class IncrementSimulation : ISimulation<EngineTestCell>
{
    public void Step(IWorldAccess<EngineTestCell> world)
    {
        var cells = world.GetAllCells().ToList();
        foreach (var (coord, state) in cells)
            world.Set(coord, new EngineTestCell(state.Value + 1));
    }
}

public class ClearingSimulation : ISimulation<EngineTestCell>
{
    public void Step(IWorldAccess<EngineTestCell> world)
    {
        var cells = world.GetAllCells().ToList();
        foreach (var (coord, _) in cells)
            world.Clear(coord);
    }
}

public class SpawningSimulation : ISimulation<EngineTestCell>
{
    public void Step(IWorldAccess<EngineTestCell> world)
    {
        world.Set(new HexCoord(10, 10), new EngineTestCell(42));
    }
}

public class SimulationEngineTests
{
    [Fact]
    public async Task StepAsync_CallsSimulationStep()
    {
        var storage = new SparseWorldStorage<EngineTestCell>();
        storage.Set(new HexCoord(0, 0), new EngineTestCell(1));

        var engine = new SimulationEngine<EngineTestCell>(storage, new IncrementSimulation());
        var changes = await engine.StepAsync();

        Assert.Single(changes);
        Assert.Equal(new HexCoord(0, 0), changes[0].Coord);
        Assert.NotNull(changes[0].RenderData);
        Assert.Equal(2u, changes[0].RenderData!.Value.Color);
    }

    [Fact]
    public async Task StepAsync_TracksClears()
    {
        var storage = new SparseWorldStorage<EngineTestCell>();
        storage.Set(new HexCoord(0, 0), new EngineTestCell(1));

        var engine = new SimulationEngine<EngineTestCell>(storage, new ClearingSimulation());
        var changes = await engine.StepAsync();

        Assert.Single(changes);
        Assert.Equal(new HexCoord(0, 0), changes[0].Coord);
        Assert.Null(changes[0].RenderData);
    }

    [Fact]
    public async Task StepAsync_TracksNewCells()
    {
        var storage = new SparseWorldStorage<EngineTestCell>();
        var engine = new SimulationEngine<EngineTestCell>(storage, new SpawningSimulation());
        var changes = await engine.StepAsync();

        Assert.Single(changes);
        Assert.Equal(new HexCoord(10, 10), changes[0].Coord);
        Assert.Equal(42u, changes[0].RenderData!.Value.Color);
    }

    [Fact]
    public async Task StepAsync_SecondStep_StillTracksChanges()
    {
        var storage = new SparseWorldStorage<EngineTestCell>();
        var engine = new SimulationEngine<EngineTestCell>(storage, new SpawningSimulation());

        await engine.StepAsync();
        var changes = await engine.StepAsync();

        // SpawningSimulation always sets (10,10) to 42, so it's reported each step
        Assert.Single(changes);
    }

    [Fact]
    public async Task StepAsync_ChangesIncludeChunkCoord()
    {
        var storage = new SparseWorldStorage<EngineTestCell>();
        storage.Set(new HexCoord(0, 0), new EngineTestCell(1));

        var engine = new SimulationEngine<EngineTestCell>(storage, new IncrementSimulation());
        var changes = await engine.StepAsync();

        Assert.Equal(new ChunkCoord(0, 0), changes[0].Chunk);
    }
}
