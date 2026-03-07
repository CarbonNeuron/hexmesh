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
        var (cx, cy) = HexMath.HexToPixel(new HexCoord(1, 0), size: 10.0);
        var result = HexMath.PixelToHex(cx + 1.0, cy + 1.0, size: 10.0);
        Assert.Equal(new HexCoord(1, 0), result);
    }
}
