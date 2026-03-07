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
