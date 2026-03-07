using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using HexMesh.Core.Coordinates;
using HexMesh.Core.Simulation;
using HexMesh.Core.Storage;
using HexMesh.Server.Simulation;

namespace HexMesh.Server.Components;

public partial class HexCanvas : IAsyncDisposable
{
    [Parameter] public IWorldStorage<GameOfLifeCell>? Storage { get; set; }
    [Parameter] public EventCallback<(List<ChunkCoord> Subscribe, List<ChunkCoord> Unsubscribe)> OnChunkSubscriptionsChangedCallback { get; set; }

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

        await OnChunkSubscriptionsChangedCallback.InvokeAsync((subList, unsubList));

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
