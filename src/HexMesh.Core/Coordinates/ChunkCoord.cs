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
