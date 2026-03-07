# HexMesh Simulation Engine Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a generic 2D infinite hex world simulation engine with Blazor Server, PixiJS rendering, viewport culling via chunk subscriptions, and efficient delta streaming.

**Architecture:** Blazor Server app with JS interop to PixiJS for GPU-accelerated hex rendering. Server owns all simulation state via pluggable interfaces. Client subscribes to hex-aligned chunks; server pushes only relevant deltas through the Blazor circuit.

**Tech Stack:** .NET 10, Blazor Server, PixiJS v8 (via CDN), xUnit, JavaScript ES modules

---

### Task 1: Project Scaffolding

**Files:**
- Create: `HexMesh.sln`
- Create: `src/HexMesh.Core/HexMesh.Core.csproj`
- Create: `src/HexMesh.Server/HexMesh.Server.csproj`
- Create: `tests/HexMesh.Core.Tests/HexMesh.Core.Tests.csproj`

**Step 1: Create the solution and projects**

```bash
cd /home/carbon/hexmesh-02
dotnet new sln -n HexMesh
mkdir -p src tests
dotnet new classlib -n HexMesh.Core -o src/HexMesh.Core --framework net10.0
dotnet new blazor -n HexMesh.Server -o src/HexMesh.Server --interactivity Server --empty --framework net10.0
dotnet new xunit -n HexMesh.Core.Tests -o tests/HexMesh.Core.Tests --framework net10.0
```

**Step 2: Wire up references and solution**

```bash
dotnet sln HexMesh.sln add src/HexMesh.Core/HexMesh.Core.csproj
dotnet sln HexMesh.sln add src/HexMesh.Server/HexMesh.Server.csproj
dotnet sln HexMesh.sln add tests/HexMesh.Core.Tests/HexMesh.Core.Tests.csproj
dotnet add src/HexMesh.Server/HexMesh.Server.csproj reference src/HexMesh.Core/HexMesh.Core.csproj
dotnet add tests/HexMesh.Core.Tests/HexMesh.Core.Tests.csproj reference src/HexMesh.Core/HexMesh.Core.csproj
```

**Step 3: Clean up scaffolded files**

- Delete `src/HexMesh.Core/Class1.cs`
- Delete `tests/HexMesh.Core.Tests/UnitTest1.cs`
- Create directory structure:

```bash
mkdir -p src/HexMesh.Core/Coordinates
mkdir -p src/HexMesh.Core/Simulation
mkdir -p src/HexMesh.Core/Storage
mkdir -p src/HexMesh.Server/wwwroot/js
mkdir -p src/HexMesh.Server/Services
mkdir -p tests/HexMesh.Core.Tests/Coordinates
mkdir -p tests/HexMesh.Core.Tests/Simulation
mkdir -p tests/HexMesh.Core.Tests/Storage
```

**Step 4: Verify build**

Run: `dotnet build HexMesh.sln`
Expected: Build succeeded with 0 errors.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: scaffold solution with Core, Server, and test projects"
```

---

### Task 2: HexCoord Struct

**Files:**
- Create: `src/HexMesh.Core/Coordinates/HexCoord.cs`
- Create: `tests/HexMesh.Core.Tests/Coordinates/HexCoordTests.cs`

**Step 1: Write failing tests for HexCoord**

```csharp
// tests/HexMesh.Core.Tests/Coordinates/HexCoordTests.cs
using HexMesh.Core.Coordinates;

namespace HexMesh.Core.Tests.Coordinates;

public class HexCoordTests
{
    [Fact]
    public void Constructor_SetsQAndR()
    {
        var coord = new HexCoord(3, -2);
        Assert.Equal(3, coord.Q);
        Assert.Equal(-2, coord.R);
    }

    [Fact]
    public void S_IsComputedFromQAndR()
    {
        var coord = new HexCoord(3, -2);
        Assert.Equal(-1, coord.S);
    }

    [Fact]
    public void Equality_SameCoords_AreEqual()
    {
        var a = new HexCoord(1, 2);
        var b = new HexCoord(1, 2);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentCoords_AreNotEqual()
    {
        var a = new HexCoord(1, 2);
        var b = new HexCoord(2, 1);
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void GetHashCode_SameCoords_SameHash()
    {
        var a = new HexCoord(1, 2);
        var b = new HexCoord(1, 2);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Neighbors_ReturnsAllSix()
    {
        var center = new HexCoord(0, 0);
        var neighbors = center.Neighbors().ToList();
        Assert.Equal(6, neighbors.Count);

        // Pointy-top axial neighbors
        Assert.Contains(new HexCoord(1, 0), neighbors);
        Assert.Contains(new HexCoord(1, -1), neighbors);
        Assert.Contains(new HexCoord(0, -1), neighbors);
        Assert.Contains(new HexCoord(-1, 0), neighbors);
        Assert.Contains(new HexCoord(-1, 1), neighbors);
        Assert.Contains(new HexCoord(0, 1), neighbors);
    }

    [Fact]
    public void DistanceTo_SameCell_IsZero()
    {
        var a = new HexCoord(3, -1);
        Assert.Equal(0, a.DistanceTo(a));
    }

    [Fact]
    public void DistanceTo_Adjacent_IsOne()
    {
        var a = new HexCoord(0, 0);
        var b = new HexCoord(1, 0);
        Assert.Equal(1, a.DistanceTo(b));
    }

    [Fact]
    public void DistanceTo_FarAway_IsCorrect()
    {
        var a = new HexCoord(0, 0);
        var b = new HexCoord(3, -3);
        Assert.Equal(3, a.DistanceTo(b));
    }

    [Fact]
    public void Add_CombinesCoords()
    {
        var a = new HexCoord(1, 2);
        var b = new HexCoord(3, -1);
        var result = a + b;
        Assert.Equal(new HexCoord(4, 1), result);
    }

    [Fact]
    public void Subtract_SubtractsCoords()
    {
        var a = new HexCoord(4, 1);
        var b = new HexCoord(1, 2);
        var result = a - b;
        Assert.Equal(new HexCoord(3, -1), result);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/HexMesh.Core.Tests --filter "FullyQualifiedName~HexCoordTests" --verbosity quiet`
Expected: Build error — `HexCoord` not found.

**Step 3: Implement HexCoord**

```csharp
// src/HexMesh.Core/Coordinates/HexCoord.cs
namespace HexMesh.Core.Coordinates;

public readonly record struct HexCoord(int Q, int R)
{
    public int S => -Q - R;

    private static readonly HexCoord[] Directions =
    [
        new(1, 0), new(1, -1), new(0, -1),
        new(-1, 0), new(-1, 1), new(0, 1)
    ];

    public IEnumerable<HexCoord> Neighbors()
    {
        for (int i = 0; i < Directions.Length; i++)
            yield return this + Directions[i];
    }

    public int DistanceTo(HexCoord other)
    {
        var diff = this - other;
        return (Math.Abs(diff.Q) + Math.Abs(diff.R) + Math.Abs(diff.S)) / 2;
    }

    public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);
    public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);

    public override string ToString() => $"Hex({Q}, {R})";
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/HexMesh.Core.Tests --filter "FullyQualifiedName~HexCoordTests" --verbosity quiet`
Expected: All 11 tests pass.

**Step 5: Commit**

```bash
git add src/HexMesh.Core/Coordinates/HexCoord.cs tests/HexMesh.Core.Tests/Coordinates/HexCoordTests.cs
git commit -m "feat: add HexCoord struct with neighbors, distance, arithmetic"
```

---

### Task 3: ChunkCoord + Chunk Assignment

**Files:**
- Create: `src/HexMesh.Core/Coordinates/ChunkCoord.cs`
- Create: `tests/HexMesh.Core.Tests/Coordinates/ChunkCoordTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/HexMesh.Core.Tests/Coordinates/ChunkCoordTests.cs
using HexMesh.Core.Coordinates;

namespace HexMesh.Core.Tests.Coordinates;

public class ChunkCoordTests
{
    [Fact]
    public void FromHex_OriginCell_IsChunkZero()
    {
        var chunk = ChunkCoord.FromHex(new HexCoord(0, 0));
        Assert.Equal(new ChunkCoord(0, 0), chunk);
    }

    [Fact]
    public void FromHex_CellWithinChunk_MapsCorrectly()
    {
        // ChunkSize = 16; cell (15, 15) => chunk (0, 0)
        var chunk = ChunkCoord.FromHex(new HexCoord(15, 15));
        Assert.Equal(new ChunkCoord(0, 0), chunk);
    }

    [Fact]
    public void FromHex_CellAtChunkBoundary_MapsToNextChunk()
    {
        var chunk = ChunkCoord.FromHex(new HexCoord(16, 0));
        Assert.Equal(new ChunkCoord(1, 0), chunk);
    }

    [Fact]
    public void FromHex_NegativeCoords_MapsCorrectly()
    {
        // -1 should map to chunk -1 (floor division)
        var chunk = ChunkCoord.FromHex(new HexCoord(-1, 0));
        Assert.Equal(new ChunkCoord(-1, 0), chunk);
    }

    [Fact]
    public void FromHex_NegativeBoundary_MapsCorrectly()
    {
        // -16 should map to chunk -1
        var chunk = ChunkCoord.FromHex(new HexCoord(-16, 0));
        Assert.Equal(new ChunkCoord(-1, 0), chunk);
    }

    [Fact]
    public void FromHex_NegativePastBoundary_MapsCorrectly()
    {
        // -17 should map to chunk -2
        var chunk = ChunkCoord.FromHex(new HexCoord(-17, 0));
        Assert.Equal(new ChunkCoord(-2, 0), chunk);
    }

    [Fact]
    public void Equality_SameChunks_AreEqual()
    {
        var a = new ChunkCoord(1, -1);
        var b = new ChunkCoord(1, -1);
        Assert.Equal(a, b);
    }

    [Fact]
    public void ChunkSize_Is16()
    {
        Assert.Equal(16, ChunkCoord.ChunkSize);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/HexMesh.Core.Tests --filter "FullyQualifiedName~ChunkCoordTests" --verbosity quiet`
Expected: Build error — `ChunkCoord` not found.

**Step 3: Implement ChunkCoord**

```csharp
// src/HexMesh.Core/Coordinates/ChunkCoord.cs
namespace HexMesh.Core.Coordinates;

public readonly record struct ChunkCoord(int Q, int R)
{
    public const int ChunkSize = 16;

    public static ChunkCoord FromHex(HexCoord hex)
    {
        return new ChunkCoord(
            FloorDiv(hex.Q, ChunkSize),
            FloorDiv(hex.R, ChunkSize)
        );
    }

    private static int FloorDiv(int a, int b)
    {
        return Math.DivRem(a, b, out int rem) - (rem < 0 ? 1 : 0);
    }

    public override string ToString() => $"Chunk({Q}, {R})";
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/HexMesh.Core.Tests --filter "FullyQualifiedName~ChunkCoordTests" --verbosity quiet`
Expected: All 8 tests pass.

**Step 5: Commit**

```bash
git add src/HexMesh.Core/Coordinates/ChunkCoord.cs tests/HexMesh.Core.Tests/Coordinates/ChunkCoordTests.cs
git commit -m "feat: add ChunkCoord with floor-division chunk assignment"
```

---

### Task 4: HexMath (Pixel Conversion)

**Files:**
- Create: `src/HexMesh.Core/Coordinates/HexMath.cs`
- Create: `tests/HexMesh.Core.Tests/Coordinates/HexMathTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/HexMesh.Core.Tests/Coordinates/HexMathTests.cs
using HexMesh.Core.Coordinates;

namespace HexMesh.Core.Tests.Coordinates;

public class HexMathTests
{
    private const double Tolerance = 0.001;

    [Fact]
    public void HexToPixel_Origin_IsZero()
    {
        var (x, y) = HexMath.HexToPixel(new HexCoord(0, 0), size: 10.0);
        Assert.Equal(0.0, x, Tolerance);
        Assert.Equal(0.0, y, Tolerance);
    }

    [Fact]
    public void HexToPixel_PointyTop_Q1R0()
    {
        // Pointy-top: x = size * (sqrt(3) * q + sqrt(3)/2 * r)
        //             y = size * (3/2 * r)
        var (x, y) = HexMath.HexToPixel(new HexCoord(1, 0), size: 10.0);
        Assert.Equal(10.0 * Math.Sqrt(3), x, Tolerance);
        Assert.Equal(0.0, y, Tolerance);
    }

    [Fact]
    public void HexToPixel_PointyTop_Q0R1()
    {
        var (x, y) = HexMath.HexToPixel(new HexCoord(0, 1), size: 10.0);
        Assert.Equal(10.0 * Math.Sqrt(3) / 2.0, x, Tolerance);
        Assert.Equal(15.0, y, Tolerance);
    }

    [Fact]
    public void PixelToHex_Roundtrip()
    {
        var original = new HexCoord(3, -2);
        var (px, py) = HexMath.HexToPixel(original, size: 10.0);
        var result = HexMath.PixelToHex(px, py, size: 10.0);
        Assert.Equal(original, result);
    }

    [Fact]
    public void PixelToHex_Origin_ReturnsOriginHex()
    {
        var result = HexMath.PixelToHex(0.0, 0.0, size: 10.0);
        Assert.Equal(new HexCoord(0, 0), result);
    }

    [Fact]
    public void PixelToHex_SlightlyOffCenter_RoundsCorrectly()
    {
        // Slightly off from (1,0) center, should still round to (1,0)
        var (cx, cy) = HexMath.HexToPixel(new HexCoord(1, 0), size: 10.0);
        var result = HexMath.PixelToHex(cx + 1.0, cy + 1.0, size: 10.0);
        Assert.Equal(new HexCoord(1, 0), result);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/HexMesh.Core.Tests --filter "FullyQualifiedName~HexMathTests" --verbosity quiet`
Expected: Build error — `HexMath` not found.

**Step 3: Implement HexMath**

```csharp
// src/HexMesh.Core/Coordinates/HexMath.cs
namespace HexMesh.Core.Coordinates;

public static class HexMath
{
    private static readonly double Sqrt3 = Math.Sqrt(3.0);

    /// <summary>
    /// Convert axial hex coordinate to pixel position (pointy-top).
    /// </summary>
    public static (double X, double Y) HexToPixel(HexCoord hex, double size)
    {
        double x = size * (Sqrt3 * hex.Q + Sqrt3 / 2.0 * hex.R);
        double y = size * (3.0 / 2.0 * hex.R);
        return (x, y);
    }

    /// <summary>
    /// Convert pixel position to axial hex coordinate (pointy-top) with rounding.
    /// </summary>
    public static HexCoord PixelToHex(double x, double y, double size)
    {
        double q = (Sqrt3 / 3.0 * x - 1.0 / 3.0 * y) / size;
        double r = (2.0 / 3.0 * y) / size;
        return HexRound(q, r);
    }

    private static HexCoord HexRound(double q, double r)
    {
        double s = -q - r;
        int qi = (int)Math.Round(q);
        int ri = (int)Math.Round(r);
        int si = (int)Math.Round(s);

        double qDiff = Math.Abs(qi - q);
        double rDiff = Math.Abs(ri - r);
        double sDiff = Math.Abs(si - s);

        if (qDiff > rDiff && qDiff > sDiff)
            qi = -ri - si;
        else if (rDiff > sDiff)
            ri = -qi - si;

        return new HexCoord(qi, ri);
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/HexMesh.Core.Tests --filter "FullyQualifiedName~HexMathTests" --verbosity quiet`
Expected: All 6 tests pass.

**Step 5: Commit**

```bash
git add src/HexMesh.Core/Coordinates/HexMath.cs tests/HexMesh.Core.Tests/Coordinates/HexMathTests.cs
git commit -m "feat: add HexMath with hex-to-pixel and pixel-to-hex conversion"
```

---

### Task 5: Core Interfaces

**Files:**
- Create: `src/HexMesh.Core/Simulation/CellRenderData.cs`
- Create: `src/HexMesh.Core/Simulation/ICellState.cs`
- Create: `src/HexMesh.Core/Simulation/ISimulation.cs`
- Create: `src/HexMesh.Core/Simulation/IWorldAccess.cs`
- Create: `src/HexMesh.Core/Storage/IWorldStorage.cs`

**Step 1: Create CellRenderData**

```csharp
// src/HexMesh.Core/Simulation/CellRenderData.cs
namespace HexMesh.Core.Simulation;

public readonly record struct CellRenderData(uint Color);
```

**Step 2: Create ICellState**

```csharp
// src/HexMesh.Core/Simulation/ICellState.cs
namespace HexMesh.Core.Simulation;

public interface ICellState
{
    CellRenderData GetRenderData();
}
```

**Step 3: Create IWorldAccess**

```csharp
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
```

**Step 4: Create ISimulation**

```csharp
// src/HexMesh.Core/Simulation/ISimulation.cs
namespace HexMesh.Core.Simulation;

public interface ISimulation<TCell> where TCell : ICellState
{
    void Step(IWorldAccess<TCell> world);
}
```

**Step 5: Create IWorldStorage**

```csharp
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
```

**Step 6: Verify build**

Run: `dotnet build HexMesh.sln`
Expected: Build succeeded.

**Step 7: Commit**

```bash
git add src/HexMesh.Core/Simulation/ src/HexMesh.Core/Storage/IWorldStorage.cs
git commit -m "feat: add core interfaces — ICellState, ISimulation, IWorldAccess, IWorldStorage"
```

---

### Task 6: SparseWorldStorage

**Files:**
- Create: `src/HexMesh.Core/Storage/SparseWorldStorage.cs`
- Create: `tests/HexMesh.Core.Tests/Storage/SparseWorldStorageTests.cs`

**Step 1: Write failing tests**

```csharp
// tests/HexMesh.Core.Tests/Storage/SparseWorldStorageTests.cs
using HexMesh.Core.Coordinates;
using HexMesh.Core.Simulation;
using HexMesh.Core.Storage;

namespace HexMesh.Core.Tests.Storage;

public record TestCell(bool Alive) : ICellState
{
    public CellRenderData GetRenderData() => new(Alive ? 0xFFFFFFFF : 0x000000FF);
}

public class SparseWorldStorageTests
{
    private readonly SparseWorldStorage<TestCell> _storage = new();

    [Fact]
    public void Get_EmptyWorld_ReturnsNull()
    {
        Assert.Null(_storage.Get(new HexCoord(0, 0)));
    }

    [Fact]
    public void Set_ThenGet_ReturnsCellState()
    {
        var coord = new HexCoord(1, 2);
        var cell = new TestCell(true);
        _storage.Set(coord, cell);
        Assert.Equal(cell, _storage.Get(coord));
    }

    [Fact]
    public void Set_Overwrite_ReturnsNewState()
    {
        var coord = new HexCoord(1, 2);
        _storage.Set(coord, new TestCell(true));
        _storage.Set(coord, new TestCell(false));
        Assert.Equal(new TestCell(false), _storage.Get(coord));
    }

    [Fact]
    public void Clear_RemovesCell()
    {
        var coord = new HexCoord(1, 2);
        _storage.Set(coord, new TestCell(true));
        _storage.Clear(coord);
        Assert.Null(_storage.Get(coord));
    }

    [Fact]
    public void Clear_NonExistent_DoesNotThrow()
    {
        _storage.Clear(new HexCoord(99, 99));
    }

    [Fact]
    public void GetInChunk_ReturnsOnlyCellsInThatChunk()
    {
        // Chunk(0,0) covers Q: 0..15, R: 0..15
        var inChunk = new HexCoord(5, 5);
        var outOfChunk = new HexCoord(20, 5); // Chunk(1,0)

        _storage.Set(inChunk, new TestCell(true));
        _storage.Set(outOfChunk, new TestCell(true));

        var results = _storage.GetInChunk(new ChunkCoord(0, 0)).ToList();
        Assert.Single(results);
        Assert.Equal(inChunk, results[0].Coord);
    }

    [Fact]
    public void GetActiveChunks_ReturnsChunksWithCells()
    {
        _storage.Set(new HexCoord(0, 0), new TestCell(true));
        _storage.Set(new HexCoord(20, 0), new TestCell(true));

        var chunks = _storage.GetActiveChunks();
        Assert.Equal(2, chunks.Count);
        Assert.Contains(new ChunkCoord(0, 0), chunks);
        Assert.Contains(new ChunkCoord(1, 0), chunks);
    }

    [Fact]
    public void GetActiveChunks_AfterClear_RemovesEmptyChunk()
    {
        var coord = new HexCoord(0, 0);
        _storage.Set(coord, new TestCell(true));
        _storage.Clear(coord);

        Assert.Empty(_storage.GetActiveChunks());
    }

    [Fact]
    public void GetAllCells_ReturnsAllCells()
    {
        _storage.Set(new HexCoord(0, 0), new TestCell(true));
        _storage.Set(new HexCoord(5, 5), new TestCell(false));

        var all = _storage.GetAllCells().ToList();
        Assert.Equal(2, all.Count);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/HexMesh.Core.Tests --filter "FullyQualifiedName~SparseWorldStorageTests" --verbosity quiet`
Expected: Build error — `SparseWorldStorage` not found.

**Step 3: Implement SparseWorldStorage**

```csharp
// src/HexMesh.Core/Storage/SparseWorldStorage.cs
using HexMesh.Core.Coordinates;
using HexMesh.Core.Simulation;

namespace HexMesh.Core.Storage;

public class SparseWorldStorage<TCell> : IWorldStorage<TCell> where TCell : ICellState
{
    private readonly Dictionary<HexCoord, TCell> _cells = new();
    private readonly Dictionary<ChunkCoord, HashSet<HexCoord>> _chunkIndex = new();

    public TCell? Get(HexCoord coord)
    {
        return _cells.TryGetValue(coord, out var cell) ? cell : default;
    }

    public void Set(HexCoord coord, TCell state)
    {
        _cells[coord] = state;
        var chunk = ChunkCoord.FromHex(coord);
        if (!_chunkIndex.TryGetValue(chunk, out var set))
        {
            set = new HashSet<HexCoord>();
            _chunkIndex[chunk] = set;
        }
        set.Add(coord);
    }

    public void Clear(HexCoord coord)
    {
        if (!_cells.Remove(coord))
            return;

        var chunk = ChunkCoord.FromHex(coord);
        if (_chunkIndex.TryGetValue(chunk, out var set))
        {
            set.Remove(coord);
            if (set.Count == 0)
                _chunkIndex.Remove(chunk);
        }
    }

    public IEnumerable<(HexCoord Coord, TCell State)> GetInChunk(ChunkCoord chunk)
    {
        if (!_chunkIndex.TryGetValue(chunk, out var set))
            yield break;

        foreach (var coord in set)
            yield return (coord, _cells[coord]);
    }

    public IEnumerable<(HexCoord Coord, TCell State)> GetAllCells()
    {
        foreach (var kvp in _cells)
            yield return (kvp.Key, kvp.Value);
    }

    public IReadOnlySet<ChunkCoord> GetActiveChunks()
    {
        return _chunkIndex.Keys.ToHashSet();
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/HexMesh.Core.Tests --filter "FullyQualifiedName~SparseWorldStorageTests" --verbosity quiet`
Expected: All 9 tests pass.

**Step 5: Commit**

```bash
git add src/HexMesh.Core/Storage/SparseWorldStorage.cs tests/HexMesh.Core.Tests/Storage/SparseWorldStorageTests.cs
git commit -m "feat: add SparseWorldStorage with chunk spatial indexing"
```

---

### Task 7: SimulationEngine with Change Tracking

**Files:**
- Create: `src/HexMesh.Core/Simulation/WorldAccessor.cs`
- Create: `src/HexMesh.Core/Simulation/SimulationEngine.cs`
- Create: `tests/HexMesh.Core.Tests/Simulation/SimulationEngineTests.cs`

**Step 1: Write failing tests**

```csharp
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
        {
            world.Set(coord, new EngineTestCell(state.Value + 1));
        }
    }
}

public class ClearingSimulation : ISimulation<EngineTestCell>
{
    public void Step(IWorldAccess<EngineTestCell> world)
    {
        var cells = world.GetAllCells().ToList();
        foreach (var (coord, _) in cells)
        {
            world.Clear(coord);
        }
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
    public async Task StepAsync_NoChanges_ReturnsEmpty()
    {
        var storage = new SparseWorldStorage<EngineTestCell>();
        var engine = new SimulationEngine<EngineTestCell>(storage, new SpawningSimulation());

        // First step creates the cell
        await engine.StepAsync();
        // Second step sets same cell to same value — still counts as a change (Set was called)
        var changes = await engine.StepAsync();

        // SpawningSimulation always sets (10,10) to 42, so it's reported
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
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/HexMesh.Core.Tests --filter "FullyQualifiedName~SimulationEngineTests" --verbosity quiet`
Expected: Build error — `SimulationEngine` not found.

**Step 3: Implement WorldAccessor (change-tracking wrapper)**

```csharp
// src/HexMesh.Core/Simulation/WorldAccessor.cs
using HexMesh.Core.Coordinates;
using HexMesh.Core.Storage;

namespace HexMesh.Core.Simulation;

internal class WorldAccessor<TCell> : IWorldAccess<TCell> where TCell : ICellState
{
    private readonly IWorldStorage<TCell> _storage;
    private readonly Dictionary<HexCoord, CellRenderData?> _changes = new();

    public WorldAccessor(IWorldStorage<TCell> storage)
    {
        _storage = storage;
    }

    public TCell? Get(HexCoord coord) => _storage.Get(coord);

    public void Set(HexCoord coord, TCell state)
    {
        _storage.Set(coord, state);
        _changes[coord] = state.GetRenderData();
    }

    public void Clear(HexCoord coord)
    {
        _storage.Clear(coord);
        _changes[coord] = null;
    }

    public IEnumerable<HexCoord> GetNeighbors(HexCoord coord) => coord.Neighbors();

    public IEnumerable<(HexCoord Coord, TCell State)> GetAllCells() => _storage.GetAllCells();

    public List<CellChange> GetChanges()
    {
        var result = new List<CellChange>(_changes.Count);
        foreach (var (coord, renderData) in _changes)
        {
            result.Add(new CellChange(coord, ChunkCoord.FromHex(coord), renderData));
        }
        return result;
    }
}
```

**Step 4: Implement SimulationEngine**

```csharp
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
```

**Step 5: Run tests to verify they pass**

Run: `dotnet test tests/HexMesh.Core.Tests --filter "FullyQualifiedName~SimulationEngineTests" --verbosity quiet`
Expected: All 5 tests pass.

**Step 6: Commit**

```bash
git add src/HexMesh.Core/Simulation/WorldAccessor.cs src/HexMesh.Core/Simulation/SimulationEngine.cs tests/HexMesh.Core.Tests/Simulation/SimulationEngineTests.cs
git commit -m "feat: add SimulationEngine with change-tracking WorldAccessor"
```

---

### Task 8: Sample Game of Life Simulation

**Files:**
- Create: `src/HexMesh.Server/Simulation/GameOfLifeCell.cs`
- Create: `src/HexMesh.Server/Simulation/GameOfLifeSimulation.cs`

We put the sample simulation directly in the server project for simplicity — it can be extracted later.

**Step 1: Create GameOfLifeCell**

```csharp
// src/HexMesh.Server/Simulation/GameOfLifeCell.cs
using HexMesh.Core.Simulation;

namespace HexMesh.Server.Simulation;

public record GameOfLifeCell(bool Alive) : ICellState
{
    public CellRenderData GetRenderData() => new(Alive ? 0x00FF00FF : 0x333333FF);
}
```

**Step 2: Create GameOfLifeSimulation**

Standard hex Game of Life rules: a live cell survives with 2 neighbors, a dead cell is born with exactly 2 neighbors (using hex-adapted rules that work well).

```csharp
// src/HexMesh.Server/Simulation/GameOfLifeSimulation.cs
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
```

**Step 3: Verify build**

Run: `dotnet build HexMesh.sln`
Expected: Build succeeded.

**Step 4: Commit**

```bash
git add src/HexMesh.Server/Simulation/
git commit -m "feat: add hex Game of Life sample simulation"
```

---

### Task 9: PixiJS Hex Renderer (JavaScript Module)

**Files:**
- Create: `src/HexMesh.Server/wwwroot/js/hexRenderer.js`

This is the core rendering module. It handles PixiJS setup, hex drawing, pan/zoom, viewport chunk calculation, and batch updates.

**Step 1: Create hexRenderer.js**

```javascript
// src/HexMesh.Server/wwwroot/js/hexRenderer.js

const SQRT3 = Math.sqrt(3);
const MIN_SCALE = 0.1;
const MAX_SCALE = 5.0;
const CHUNK_SIZE = 16;

let app = null;
let worldContainer = null;
let hexSize = 20;
let hexGraphics = new Map(); // key: "q,r" -> Graphics
let dotNetRef = null;
let subscribedChunks = new Set();
let isDragging = false;
let lastPointer = { x: 0, y: 0 };

export async function initialize(canvasId, dotNetReference, hexSizeParam) {
    dotNetRef = dotNetReference;
    hexSize = hexSizeParam || 20;

    const container = document.getElementById(canvasId);
    if (!container) return;

    app = new PIXI.Application();
    await app.init({
        resizeTo: container,
        background: 0x1a1a2e,
        antialias: true,
        resolution: window.devicePixelRatio || 1,
        autoDensity: true,
    });
    container.appendChild(app.canvas);

    worldContainer = new PIXI.Container();
    app.stage.addChild(worldContainer);

    // Center the world in the viewport
    worldContainer.x = app.screen.width / 2;
    worldContainer.y = app.screen.height / 2;

    setupInteraction(container);
    updateChunkSubscriptions();
}

function setupInteraction(container) {
    const canvas = app.canvas;

    canvas.addEventListener('pointerdown', (e) => {
        isDragging = true;
        lastPointer = { x: e.clientX, y: e.clientY };
        canvas.setPointerCapture(e.pointerId);
    });

    canvas.addEventListener('pointermove', (e) => {
        if (!isDragging) return;
        const dx = e.clientX - lastPointer.x;
        const dy = e.clientY - lastPointer.y;
        worldContainer.x += dx;
        worldContainer.y += dy;
        lastPointer = { x: e.clientX, y: e.clientY };
        onViewportChanged();
    });

    canvas.addEventListener('pointerup', (e) => {
        isDragging = false;
    });

    canvas.addEventListener('wheel', (e) => {
        e.preventDefault();
        const scaleFactor = e.deltaY < 0 ? 1.1 : 0.9;
        const newScale = Math.max(MIN_SCALE, Math.min(MAX_SCALE, worldContainer.scale.x * scaleFactor));

        // Zoom toward mouse position
        const rect = canvas.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        const worldBefore = {
            x: (mouseX - worldContainer.x) / worldContainer.scale.x,
            y: (mouseY - worldContainer.y) / worldContainer.scale.y,
        };

        worldContainer.scale.set(newScale);

        worldContainer.x = mouseX - worldBefore.x * newScale;
        worldContainer.y = mouseY - worldBefore.y * newScale;

        onViewportChanged();
    }, { passive: false });

    // Handle resize
    const resizeObserver = new ResizeObserver(() => {
        app.resize();
        onViewportChanged();
    });
    resizeObserver.observe(container);
}

let viewportDebounceTimer = null;

function onViewportChanged() {
    // Debounce chunk subscription updates
    if (viewportDebounceTimer) clearTimeout(viewportDebounceTimer);
    viewportDebounceTimer = setTimeout(() => {
        updateChunkSubscriptions();
    }, 50);
}

function getVisibleChunks() {
    const scale = worldContainer.scale.x;
    const w = app.screen.width;
    const h = app.screen.height;

    // Convert screen corners to world coordinates
    const left = -worldContainer.x / scale;
    const top = -worldContainer.y / scale;
    const right = (w - worldContainer.x) / scale;
    const bottom = (h - worldContainer.y) / scale;

    // Convert world pixel bounds to hex bounds (conservative)
    const margin = hexSize * 2;
    const minQ = pixelToHexQ(left - margin, top - margin);
    const maxQ = pixelToHexQ(right + margin, bottom + margin);
    const minR = pixelToHexR(left - margin, top - margin);
    const maxR = pixelToHexR(right + margin, bottom + margin);

    // Convert hex bounds to chunk bounds
    const minCQ = floorDiv(minQ, CHUNK_SIZE);
    const maxCQ = floorDiv(maxQ, CHUNK_SIZE);
    const minCR = floorDiv(minR, CHUNK_SIZE);
    const maxCR = floorDiv(maxR, CHUNK_SIZE);

    const chunks = new Set();
    for (let cq = minCQ; cq <= maxCQ; cq++) {
        for (let cr = minCR; cr <= maxCR; cr++) {
            chunks.add(`${cq},${cr}`);
        }
    }
    return chunks;
}

function pixelToHexQ(x, y) {
    return Math.floor((SQRT3 / 3 * x - 1 / 3 * y) / hexSize);
}

function pixelToHexR(x, y) {
    return Math.floor((2 / 3 * y) / hexSize);
}

function floorDiv(a, b) {
    return Math.floor(a / b);
}

async function updateChunkSubscriptions() {
    if (!dotNetRef) return;

    const visible = getVisibleChunks();

    // Compute diffs
    const toSubscribe = [];
    const toUnsubscribe = [];

    for (const key of visible) {
        if (!subscribedChunks.has(key)) {
            toSubscribe.push(key);
        }
    }
    for (const key of subscribedChunks) {
        if (!visible.has(key)) {
            toUnsubscribe.push(key);
        }
    }

    if (toSubscribe.length === 0 && toUnsubscribe.length === 0) return;

    subscribedChunks = visible;

    // Remove graphics for unsubscribed chunks
    for (const key of toUnsubscribe) {
        removeChunkGraphics(key);
    }

    // Notify server of subscription change
    try {
        await dotNetRef.invokeMethodAsync('OnChunkSubscriptionsChanged',
            toSubscribe.map(parseChunkKey),
            toUnsubscribe.map(parseChunkKey)
        );
    } catch (e) {
        console.error('Failed to update chunk subscriptions:', e);
    }
}

function parseChunkKey(key) {
    const [q, r] = key.split(',').map(Number);
    return { q, r };
}

function chunkKey(q, r) {
    return `${q},${r}`;
}

function removeChunkGraphics(chunkKeyStr) {
    const prefix = chunkKeyStr + ':';
    const toRemove = [];
    for (const [key, gfx] of hexGraphics) {
        // Keys are stored as "q,r" of the hex coord, we need to check chunk membership
        // Instead, we'll track chunk membership in the graphics object
        if (gfx._chunkKey === chunkKeyStr) {
            toRemove.push(key);
        }
    }
    for (const key of toRemove) {
        const gfx = hexGraphics.get(key);
        worldContainer.removeChild(gfx);
        gfx.destroy();
        hexGraphics.delete(key);
    }
}

function hexToPixel(q, r) {
    const x = hexSize * (SQRT3 * q + SQRT3 / 2 * r);
    const y = hexSize * (3 / 2 * r);
    return { x, y };
}

function drawHex(q, r, color, chunkQ, chunkR) {
    const key = `${q},${r}`;
    let gfx = hexGraphics.get(key);

    if (gfx) {
        // Update existing hex
        gfx.clear();
    } else {
        gfx = new PIXI.Graphics();
        const pos = hexToPixel(q, r);
        gfx.x = pos.x;
        gfx.y = pos.y;
        gfx._chunkKey = chunkKey(chunkQ, chunkR);
        worldContainer.addChild(gfx);
        hexGraphics.set(key, gfx);
    }

    // Draw pointy-top hex
    const points = [];
    for (let i = 0; i < 6; i++) {
        const angle = (Math.PI / 180) * (60 * i - 30);
        points.push(hexSize * Math.cos(angle));
        points.push(hexSize * Math.sin(angle));
    }

    gfx.poly(points, true);
    gfx.fill({ color: color });
    gfx.stroke({ width: 1, color: 0x2a2a4a });
}

function removeHex(q, r) {
    const key = `${q},${r}`;
    const gfx = hexGraphics.get(key);
    if (gfx) {
        worldContainer.removeChild(gfx);
        gfx.destroy();
        hexGraphics.delete(key);
    }
}

export function applyDeltas(deltas) {
    // deltas is an array of { q, r, chunkQ, chunkR, color, cleared }
    for (const delta of deltas) {
        if (delta.cleared) {
            removeHex(delta.q, delta.r);
        } else {
            drawHex(delta.q, delta.r, delta.color, delta.chunkQ, delta.chunkR);
        }
    }
}

export function loadChunkData(chunkQ, chunkR, cells) {
    // cells is an array of { q, r, color }
    for (const cell of cells) {
        drawHex(cell.q, cell.r, cell.color, chunkQ, chunkR);
    }
}

export function dispose() {
    if (app) {
        app.destroy(true);
        app = null;
    }
    hexGraphics.clear();
    subscribedChunks.clear();
}
```

**Step 2: Verify the file is valid JS (no syntax errors)**

Run: `node --check src/HexMesh.Server/wwwroot/js/hexRenderer.js`
Expected: No output (clean parse).

**Step 3: Commit**

```bash
git add src/HexMesh.Server/wwwroot/js/hexRenderer.js
git commit -m "feat: add PixiJS hex renderer with pan/zoom and chunk subscriptions"
```

---

### Task 10: Blazor HexCanvas Component + Chunk Subscription Service

**Files:**
- Create: `src/HexMesh.Server/Services/ChunkSubscriptionManager.cs`
- Create: `src/HexMesh.Server/Components/HexCanvas.razor`
- Create: `src/HexMesh.Server/Components/HexCanvas.razor.cs`

**Step 1: Create ChunkSubscriptionManager**

```csharp
// src/HexMesh.Server/Services/ChunkSubscriptionManager.cs
using HexMesh.Core.Coordinates;
using HexMesh.Core.Simulation;

namespace HexMesh.Server.Services;

public class ChunkSubscriptionManager
{
    private readonly HashSet<ChunkCoord> _subscribedChunks = new();
    private readonly object _lock = new();

    public IReadOnlySet<ChunkCoord> SubscribedChunks
    {
        get
        {
            lock (_lock)
            {
                return _subscribedChunks.ToHashSet();
            }
        }
    }

    public void Subscribe(IEnumerable<ChunkCoord> chunks)
    {
        lock (_lock)
        {
            foreach (var chunk in chunks)
                _subscribedChunks.Add(chunk);
        }
    }

    public void Unsubscribe(IEnumerable<ChunkCoord> chunks)
    {
        lock (_lock)
        {
            foreach (var chunk in chunks)
                _subscribedChunks.Remove(chunk);
        }
    }

    public List<CellChange> FilterChanges(List<CellChange> changes)
    {
        lock (_lock)
        {
            return changes.Where(c => _subscribedChunks.Contains(c.Chunk)).ToList();
        }
    }
}
```

**Step 2: Create HexCanvas.razor**

```razor
@* src/HexMesh.Server/Components/HexCanvas.razor *@
@implements IAsyncDisposable
@inject IJSRuntime JS

<div id="hex-canvas-container" style="width: 100%; height: 100%; position: relative;"></div>

@code {
    // Code-behind in HexCanvas.razor.cs
}
```

**Step 3: Create HexCanvas.razor.cs**

```csharp
// src/HexMesh.Server/Components/HexCanvas.razor.cs
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using HexMesh.Core.Coordinates;
using HexMesh.Core.Simulation;
using HexMesh.Core.Storage;

namespace HexMesh.Server.Components;

public partial class HexCanvas : IAsyncDisposable
{
    [Parameter] public IWorldStorage<HexMesh.Server.Simulation.GameOfLifeCell>? Storage { get; set; }
    [Parameter] public EventCallback<(List<ChunkCoord> Subscribe, List<ChunkCoord> Unsubscribe)> OnChunkSubscriptionsChanged { get; set; }

    private IJSObjectReference? _module;
    private DotNetObjectReference<HexCanvas>? _dotNetRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _module = await JS.InvokeAsync<IJSObjectReference>(
            "import", "/js/hexRenderer.js");

        _dotNetRef = DotNetObjectReference.Create(this);

        await _module.InvokeVoidAsync("initialize", "hex-canvas-container", _dotNetRef, 20);
    }

    [JSInvokable]
    public async Task OnChunkSubscriptionsChanged(ChunkDto[] toSubscribe, ChunkDto[] toUnsubscribe)
    {
        var subList = toSubscribe.Select(c => new ChunkCoord(c.Q, c.R)).ToList();
        var unsubList = toUnsubscribe.Select(c => new ChunkCoord(c.Q, c.R)).ToList();

        await OnChunkSubscriptionsChanged.InvokeAsync((subList, unsubList));

        // Load initial data for newly subscribed chunks
        if (Storage != null && _module != null)
        {
            foreach (var chunk in subList)
            {
                var cells = Storage.GetInChunk(chunk)
                    .Select(c => new CellDto(c.Coord.Q, c.Coord.R, c.State.GetRenderData().Color))
                    .ToArray();

                if (cells.Length > 0)
                {
                    await _module.InvokeVoidAsync("loadChunkData", chunk.Q, chunk.R, cells);
                }
            }
        }
    }

    public async Task ApplyDeltasAsync(List<CellChange> changes)
    {
        if (_module == null || changes.Count == 0) return;

        var deltas = changes.Select(c => new DeltaDto(
            c.Coord.Q, c.Coord.R,
            c.Chunk.Q, c.Chunk.R,
            c.RenderData?.Color ?? 0,
            !c.RenderData.HasValue
        )).ToArray();

        await _module.InvokeVoidAsync("applyDeltas", (object)deltas);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.InvokeVoidAsync("dispose");
            await _module.DisposeAsync();
        }
        _dotNetRef?.Dispose();
    }

    public record ChunkDto(int Q, int R);
    public record CellDto(int Q, int R, uint Color);
    public record DeltaDto(int Q, int R, int ChunkQ, int ChunkR, uint Color, bool Cleared);
}
```

**Step 4: Verify build**

Run: `dotnet build src/HexMesh.Server/HexMesh.Server.csproj`
Expected: Build succeeded.

**Step 5: Commit**

```bash
git add src/HexMesh.Server/Services/ChunkSubscriptionManager.cs src/HexMesh.Server/Components/HexCanvas.razor src/HexMesh.Server/Components/HexCanvas.razor.cs
git commit -m "feat: add HexCanvas Blazor component and ChunkSubscriptionManager"
```

---

### Task 11: Main Page + Wiring Everything Together

**Files:**
- Modify: `src/HexMesh.Server/Components/Pages/Home.razor` (or create if empty template)
- Modify: `src/HexMesh.Server/Program.cs`

**Step 1: Add PixiJS CDN reference to App.razor**

Read `src/HexMesh.Server/Components/App.razor` and add before closing `</head>`:

```html
<script src="https://cdn.jsdelivr.net/npm/pixi.js@8/dist/pixi.min.js"></script>
```

**Step 2: Create Home.razor with simulation wiring**

```razor
@* src/HexMesh.Server/Components/Pages/Home.razor *@
@page "/"
@rendermode InteractiveServer
@using HexMesh.Core.Coordinates
@using HexMesh.Core.Simulation
@using HexMesh.Core.Storage
@using HexMesh.Server.Components
@using HexMesh.Server.Services
@using HexMesh.Server.Simulation

<div style="display: flex; flex-direction: column; height: 100vh; overflow: hidden;">
    <div style="padding: 8px 16px; background: #16213e; border-bottom: 1px solid #2a2a4a; display: flex; align-items: center; gap: 12px;">
        <h3 style="margin: 0; color: #e0e0e0; font-size: 16px;">HexMesh</h3>
        <button @onclick="Step" disabled="@_stepping" style="padding: 6px 16px; background: #0f3460; color: #e0e0e0; border: 1px solid #2a2a4a; border-radius: 4px; cursor: pointer;">
            Step
        </button>
        <span style="color: #888; font-size: 13px;">Generation: @_generation</span>
        <span style="color: #888; font-size: 13px;">Cells: @_cellCount</span>
    </div>
    <div style="flex: 1; overflow: hidden;">
        <HexCanvas @ref="_canvas" Storage="_storage" OnChunkSubscriptionsChanged="HandleChunkSubscriptions" />
    </div>
</div>

@code {
    private SparseWorldStorage<GameOfLifeCell> _storage = new();
    private SimulationEngine<GameOfLifeCell> _engine = null!;
    private ChunkSubscriptionManager _chunkManager = new();
    private HexCanvas _canvas = null!;
    private int _generation = 0;
    private int _cellCount = 0;
    private bool _stepping = false;

    protected override void OnInitialized()
    {
        var simulation = new GameOfLifeSimulation();
        _engine = new SimulationEngine<GameOfLifeCell>(_storage, simulation);

        // Seed with a small pattern
        SeedPattern();
        _cellCount = _storage.GetAllCells().Count();
    }

    private void SeedPattern()
    {
        // A simple hex "glider" / oscillator pattern near origin
        var coords = new[]
        {
            new HexCoord(0, 0),
            new HexCoord(1, 0),
            new HexCoord(-1, 0),
            new HexCoord(0, 1),
            new HexCoord(0, -1),
            new HexCoord(1, -1),
            new HexCoord(-1, 1),
        };

        foreach (var coord in coords)
            _storage.Set(coord, new GameOfLifeCell(true));
    }

    private async Task Step()
    {
        _stepping = true;
        try
        {
            var changes = await _engine.StepAsync();
            _generation++;
            _cellCount = _storage.GetAllCells().Count();

            // Filter to subscribed chunks and push to canvas
            var filtered = _chunkManager.FilterChanges(changes);
            await _canvas.ApplyDeltasAsync(filtered);
        }
        finally
        {
            _stepping = false;
        }
    }

    private void HandleChunkSubscriptions((List<ChunkCoord> Subscribe, List<ChunkCoord> Unsubscribe) args)
    {
        _chunkManager.Subscribe(args.Subscribe);
        _chunkManager.Unsubscribe(args.Unsubscribe);
    }
}
```

**Step 3: Update Program.cs if needed**

Read `src/HexMesh.Server/Program.cs` and ensure it has interactive server components enabled. The `--interactivity Server` template should already have this but verify `app.MapRazorComponents<App>().AddInteractiveServerRenderMode()` is present.

**Step 4: Add basic CSS reset for full-viewport layout**

Add to `src/HexMesh.Server/wwwroot/app.css` (or create if the empty template doesn't have it):

```css
html, body {
    margin: 0;
    padding: 0;
    height: 100%;
    overflow: hidden;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}
```

**Step 5: Verify it builds and runs**

Run: `dotnet build HexMesh.sln`
Expected: Build succeeded.

Run: `dotnet run --project src/HexMesh.Server`
Expected: App starts, listen on a port. Open browser to verify hex canvas renders.

**Step 6: Commit**

```bash
git add src/HexMesh.Server/
git commit -m "feat: wire up Home page with simulation engine, step button, and canvas"
```

---

### Task 12: Integration Testing + Polish

**Step 1: Run all tests**

Run: `dotnet test HexMesh.sln --verbosity quiet`
Expected: All tests pass (25+ tests across all suites).

**Step 2: Manual smoke test**

Run: `dotnet run --project src/HexMesh.Server`

Verify:
- Canvas renders with dark background
- Colored hexes appear near center (the seed pattern)
- Pan by dragging
- Zoom with scroll wheel
- Zoom caps at max zoom-out
- Click "Step" — hexes update according to Game of Life rules
- Generation counter increments
- Cell count updates

**Step 3: Fix any issues found during smoke test**

Address rendering bugs, JS interop issues, or subscription edge cases.

**Step 4: Final commit**

```bash
git add -A
git commit -m "feat: integration polish and smoke test fixes"
```

---

Plan complete and saved to `docs/plans/2026-03-07-hexmesh-implementation.md`. Two execution options:

**1. Subagent-Driven (this session)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.

**2. Parallel Session (separate)** — Open a new session with executing-plans, batch execution with checkpoints.

Which approach?