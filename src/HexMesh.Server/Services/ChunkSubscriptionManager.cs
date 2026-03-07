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
