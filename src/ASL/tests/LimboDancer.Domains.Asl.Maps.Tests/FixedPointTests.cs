using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class FixedPointTests
{
    [Theory]
    [InlineData(56.25, 3600, "56.25")]
    [InlineData(64.5, 4128, "64.5")]
    [InlineData(32.25, 2064, "32.25")]
    [InlineData(0.015625, 1, "0.015625")]
    [InlineData(-37.5, -2400, "-37.5")]
    [InlineData(1800, 115200, "1800")]
    [InlineData(0, 0, "0")]
    public void StandardGeometryConstantsAreExact(double pixels, int raw, string text)
    {
        var value = FixedPoint.FromExactPixels(pixels);
        Assert.Equal(raw, value.Raw);
        Assert.Equal(text, value.ToString());
        Assert.Equal(pixels, value.ToPixels());
    }

    [Fact]
    public void NonRepresentableValuesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FixedPoint.FromExactPixels(0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => FixedPoint.FromExactPixels(Math.Cos(Math.PI / 6)));
    }

    [Fact]
    public void EverySixtyFourthFormatsExactly()
    {
        for (var raw = -128; raw <= 128; raw++)
        {
            var value = FixedPoint.FromRaw(raw);
            Assert.Equal((decimal)raw / 64, decimal.Parse(value.ToString(), System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public void ArithmeticIsExact()
    {
        var sum = FixedPoint.FromExactPixels(56.25) + FixedPoint.FromExactPixels(37.5);
        Assert.Equal("93.75", sum.ToString());
        Assert.True(FixedPoint.FromPixels(1) > FixedPoint.FromRaw(63));
        Assert.Equal(FixedPoint.FromExactPixels(-18.75), -FixedPoint.FromExactPixels(18.75));
    }
}
