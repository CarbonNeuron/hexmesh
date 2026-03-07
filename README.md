# HexMesh

A generic 2D infinite hex world simulation engine built with C# / Blazor Server and PixiJS for GPU-accelerated rendering.

## What It Does

HexMesh provides a framework for building cellular automaton-style simulations on an infinite hexagonal grid. You define your cell type and simulation rules; the engine handles storage, change tracking, viewport management, and efficient rendering.

The client renders hexes using PixiJS (WebGL), supports pan/zoom navigation, and only loads data for the chunks currently visible on screen. The server tracks which chunks each client has subscribed to and only streams relevant cell changes.

A hex Game of Life simulation is included as a working example.

## Running It

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/HexMesh.Server
```

Open `http://localhost:5195` in your browser. The server binds to `0.0.0.0` so it's accessible over LAN.

### Controls

- **Drag** to pan the viewport
- **Scroll wheel** to zoom (capped at 0.1x-5x to keep performance smooth)
- **Step** button advances the simulation by one generation

### Running Tests

```bash
dotnet test
```

39 tests covering coordinates, chunk assignment, storage, and simulation engine.

## Architecture

```
Browser                              Server
+------------------------+           +-----------------------------+
| Blazor UI (controls)   |           | SimulationEngine<TCell>     |
| PixiJS Hex Renderer    |<--circuit-| ChunkSubscriptionManager    |
|  - pan/zoom/cull       |           | IWorldStorage<TCell>        |
|  - chunk subscriptions |           | ISimulation<TCell>          |
+------------------------+           +-----------------------------+
```

**Blazor Server** hosts everything. The persistent SignalR circuit carries all communication -- no separate API or hub needed.

**PixiJS v8** (loaded from CDN) handles rendering via a JS interop module. Each hex is a `PIXI.Graphics` object positioned in a transformable world container. Pan/zoom transforms the container; individual hexes are created/destroyed as chunks enter/leave the viewport.

**Delta streaming** keeps things efficient: when a simulation step runs, only changed cells are sent to the client, and only if they fall within the client's currently subscribed chunks.

## Project Structure

```
HexMesh.slnx                          .NET 10 solution (XML format)
src/
  HexMesh.Core/                        Class library -- no web dependencies
    Coordinates/
      HexCoord.cs                      Axial coordinate (q, r), readonly record struct
      ChunkCoord.cs                    Chunk assignment via floor division, ChunkSize=16
      HexMath.cs                       Hex-to-pixel and pixel-to-hex conversion (pointy-top)
    Simulation/
      ICellState.cs                    Interface: cells expose render data (color)
      ISimulation.cs                   Interface: define Step() rules over IWorldAccess
      IWorldAccess.cs                  Interface: read/write world during a step
      SimulationEngine.cs              Runs steps, collects changes via WorldAccessor
      WorldAccessor.cs                 Wraps IWorldStorage, tracks all Set/Clear calls
      CellRenderData.cs                Minimal render payload (uint Color)
    Storage/
      IWorldStorage.cs                 Interface: pluggable world storage with chunk queries
      SparseWorldStorage.cs            Default: Dictionary + chunk spatial index

  HexMesh.Server/                      Blazor Server app
    Components/
      HexCanvas.razor/.razor.cs        PixiJS canvas component with JS interop
      Pages/Home.razor                 Main page: toolbar + canvas + simulation wiring
      App.razor                        Root component (loads PixiJS CDN)
    Services/
      ChunkSubscriptionManager.cs      Tracks subscribed chunks, filters deltas
    Simulation/
      GameOfLifeCell.cs                Sample cell type (alive/dead -> green/dark)
      GameOfLifeSimulation.cs          Hex GoL: survive with 2 neighbors, born with 2
    wwwroot/js/
      hexRenderer.js                   PixiJS renderer: init, pan, zoom, draw, batch update

tests/
  HexMesh.Core.Tests/                  xUnit tests for all Core types
    Coordinates/                        HexCoord, ChunkCoord, HexMath tests
    Storage/                            SparseWorldStorage tests
    Simulation/                         SimulationEngine tests
```

## How the Pluggable Interfaces Work

### Defining a cell type

Implement `ICellState` to define what data your cells hold and how they render:

```csharp
public record MyCell(int Temperature, bool OnFire) : ICellState
{
    public CellRenderData GetRenderData() =>
        new(OnFire ? 0xFF4400FF : (uint)(Temperature * 0x010100));
}
```

### Defining simulation rules

Implement `ISimulation<TCell>` to define how the world evolves each step:

```csharp
public class FireSpreadSimulation : ISimulation<MyCell>
{
    public void Step(IWorldAccess<MyCell> world)
    {
        // Read current state, write next state
        // The engine tracks all changes automatically
    }
}
```

### Custom storage

Implement `IWorldStorage<TCell>` if the default sparse dictionary doesn't fit your use case:

```csharp
public class ChunkedGridStorage<TCell> : IWorldStorage<TCell>
    where TCell : ICellState
{
    // Dense chunk-based storage for terrain-heavy simulations
}
```

### Wiring it up

```csharp
var storage = new SparseWorldStorage<MyCell>();
var simulation = new FireSpreadSimulation();
var engine = new SimulationEngine<MyCell>(storage, simulation);

// Seed initial state
storage.Set(new HexCoord(0, 0), new MyCell(100, true));

// Run a step -- returns list of CellChange with coords and render data
var changes = await engine.StepAsync();
```

## Coordinate System

- **Axial coordinates** (q, r) with pointy-top hex orientation
- Third coordinate `s = -q - r` is computed, not stored
- Six neighbors per hex at the standard axial offsets
- Chunks are 16x16 regions in axial space, assigned by floor division
- Pixel conversion uses the standard pointy-top hex formulas

## Current Limitations

- **Single-client** -- the chunk subscription model supports multiple clients conceptually, but the current wiring in Home.razor manages one simulation instance per circuit
- **No play/pause** -- only manual stepping via the Step button (the engine's async interface is designed to support continuous playback)
- **No cell interaction** -- clicking on hexes doesn't do anything yet
- **No persistence** -- world state lives in memory only
- **JS renderer uses individual Graphics objects** -- works well for moderate cell counts but could be optimized with batched geometry or instanced rendering for very large populations
