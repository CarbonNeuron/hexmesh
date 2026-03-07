# HexMesh Simulation Engine Design

## Overview

A generic 2D infinite hex world simulation engine built with C# / Blazor Server and PixiJS for GPU-accelerated canvas rendering. The engine supports pluggable simulations, efficient viewport-scoped delta streaming, and smooth pan/zoom navigation.

## Architecture

```
Browser                              Server
┌───────────────────────┐            ┌────────────────────────────┐
│ Blazor UI (controls)  │            │ SimulationEngine           │
│ PixiJS Hex Renderer   │◄──circuit──│ ChunkManager               │
│  - pan/zoom/cull      │            │ IWorldStorage<TCell>       │
│  - chunk subscriptions│            │ ISimulation<TCell>         │
└───────────────────────┘            └────────────────────────────┘
```

- **Blazor Server** hosts the app. The persistent circuit carries all communication.
- **PixiJS** (JS interop) handles all rendering — GPU-accelerated sprite batching.
- **Server** owns simulation state and pushes deltas filtered by client chunk subscriptions.

## Hex Coordinates

- **Axial coordinates** (q, r), pointy-top orientation.
- Standard hex math: neighbors, distance, axial-to-pixel, pixel-to-axial.
- `HexCoord` is a readonly struct for zero-allocation coordinate passing.

## Chunk System

- World divided into fixed-size hex-aligned chunks (e.g., side length 16 in axial space).
- `ChunkCoord` derived from `HexCoord` via integer division.
- Client subscribes/unsubscribes to chunks based on viewport overlap.
- Server tracks per-client chunk subscriptions and filters delta pushes accordingly.

## Core Interfaces

### ICellState

Minimal contract for cell data. Simulations define concrete types.

```csharp
public interface ICellState
{
    CellRenderData GetRenderData();
}

public record CellRenderData(uint Color);
```

### ISimulation<TCell>

Defines simulation rules. Called once per step.

```csharp
public interface ISimulation<TCell> where TCell : ICellState
{
    void Step(IWorldAccess<TCell> world);
}
```

### IWorldStorage<TCell>

Pluggable world storage. Default: sparse dictionary with chunk-based spatial indexing.

```csharp
public interface IWorldStorage<TCell> where TCell : ICellState
{
    TCell? Get(HexCoord coord);
    void Set(HexCoord coord, TCell state);
    void Clear(HexCoord coord);
    IEnumerable<(HexCoord Coord, TCell State)> GetInChunk(ChunkCoord chunk);
    IReadOnlySet<ChunkCoord> GetActiveChunks();
}
```

### IWorldAccess<TCell>

Passed to simulations during a step. Wraps storage and tracks changes.

```csharp
public interface IWorldAccess<TCell> where TCell : ICellState
{
    TCell? Get(HexCoord coord);
    void Set(HexCoord coord, TCell state);
    void Clear(HexCoord coord);
    IEnumerable<HexCoord> GetNeighbors(HexCoord coord);
    IEnumerable<(HexCoord Coord, TCell State)> GetAllCells();
}
```

## Storage Default: SparseWorldStorage

- `Dictionary<HexCoord, TCell>` for cell data.
- `Dictionary<ChunkCoord, HashSet<HexCoord>>` for chunk spatial index.
- On `Set`, adds to both. On `Clear`, removes from both.

## Simulation Step Flow

1. User clicks "Step" button.
2. Blazor calls `SimulationEngine.StepAsync()`.
3. Engine creates a `WorldAccessor` wrapping `IWorldStorage`, with change tracking.
4. Engine calls `ISimulation<TCell>.Step(worldAccessor)`.
5. Engine collects changed cells as `List<(HexCoord, CellRenderData?)>` (null = cleared).
6. Engine groups changes by `ChunkCoord`.
7. Filters to client's subscribed chunks.
8. Pushes deltas to PixiJS via JS interop as a batched array.
9. PixiJS applies updates to visible sprites.

## Rendering (PixiJS)

- JS module (`hexRenderer.js`) manages the PixiJS application.
- Each hex rendered as a graphic in a PixiJS container.
- **Pan**: drag to move the viewport container transform.
- **Zoom**: scroll wheel scales the container. Capped at min scale ~0.1 to bound visible hex count.
- **Viewport change**: on pan/zoom, client computes visible chunk set from camera bounds, diffs against current subscriptions, and calls server to subscribe/unsubscribe.
- **Batch updates**: server sends deltas as typed arrays. JS applies in one batch per frame.
- **Object pooling**: reuse PixiJS graphics objects for hexes entering/leaving viewport.

## Zoom Cap

Enforce a minimum zoom scale (e.g., 0.1) so the max visible area is bounded. At typical screen resolutions this caps visible hexes at a manageable count.

## UI Controls

- Step button: triggers one simulation tick.
- Designed for future: play/pause, speed slider (not built yet, but `StepAsync` is awaitable to support it).

## Project Structure

```
HexMesh/
├── HexMesh.Core/           # Class library (.NET 9)
│   ├── Coordinates/        # HexCoord, ChunkCoord, HexMath
│   ├── Simulation/         # ISimulation, ICellState, SimulationEngine
│   └── Storage/            # IWorldStorage, SparseWorldStorage
├── HexMesh.Server/         # Blazor Server app
│   ├── Components/         # Razor components
│   ├── Services/           # ChunkManager, ViewportService
│   └── wwwroot/js/         # PixiJS renderer module
└── HexMesh.Samples/        # Example simulation (hex Game of Life)
```

## First Build Scope

1. Hex coordinate math + chunk assignment
2. Core interfaces + sparse storage default
3. SimulationEngine with StepAsync + change tracking
4. PixiJS renderer with pan/zoom/zoom-cap
5. Chunk subscription on viewport change
6. Delta push pipeline (server -> JS interop -> PixiJS)
7. Step button in Blazor UI
8. Sample simulation (hex Game of Life) to prove it works
