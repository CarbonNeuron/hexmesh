using HexMesh.Core.Coordinates;
using HexMesh.Core.Simulation;

namespace HexMesh.Server.Simulation;

public class GameOfLifeSimulation : ISimulation<GameOfLifeCell>
{
    public void Step(IWorldAccess<GameOfLifeCell> world)
    {
        var cellsToCheck = new HashSet<HexCoord>();

        // Gather all live cells and their neighbors
        foreach (var (coord, _) in world.GetAllCells())
        {
            cellsToCheck.Add(coord);
            foreach (var neighbor in world.GetNeighbors(coord))
                cellsToCheck.Add(neighbor);
        }

        // Compute next state
        var toSet = new List<(HexCoord, GameOfLifeCell)>();
        var toClear = new List<HexCoord>();

        foreach (var coord in cellsToCheck)
        {
            int liveNeighbors = 0;
            foreach (var neighbor in world.GetNeighbors(coord))
            {
                var cell = world.Get(neighbor);
                if (cell is { Alive: true })
                    liveNeighbors++;
            }

            var current = world.Get(coord);
            bool isAlive = current is { Alive: true };

            if (isAlive)
            {
                // Survive with exactly 2 neighbors
                if (liveNeighbors != 2)
                    toClear.Add(coord);
            }
            else
            {
                // Born with exactly 2 neighbors
                if (liveNeighbors == 2)
                    toSet.Add((coord, new GameOfLifeCell(true)));
            }
        }

        foreach (var coord in toClear)
            world.Clear(coord);
        foreach (var (coord, cell) in toSet)
            world.Set(coord, cell);
    }
}
