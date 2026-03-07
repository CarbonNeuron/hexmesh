namespace HexMesh.Core.Coordinates;

public static class HexMath
{
    private static readonly double Sqrt3 = Math.Sqrt(3.0);

    public static (double X, double Y) HexToPixel(HexCoord hex, double size)
    {
        double x = size * (Sqrt3 * hex.Q + Sqrt3 / 2.0 * hex.R);
        double y = size * (3.0 / 2.0 * hex.R);
        return (x, y);
    }

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
