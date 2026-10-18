using System.Globalization;
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

    [Theory]
    [InlineData(56.25, 3600)]
    [InlineData(37.541666666666664, 2403)]
    [InlineData(0.0078125, 1)]
    [InlineData(-0.0078125, -1)]
    [InlineData(-0.0078, 0)]
    public void GeometryRoundsToTheNearestSixtyFourthAwayFromZero(double pixels, int raw) => Assert.Equal(raw, FixedPoint.FromPixels(pixels).Raw);

    [Fact]
    public void EverySixtyFourthFormatsExactly()
    {
        for (var raw = -128; raw <= 128; raw++)
        {
            var value = FixedPoint.FromRaw(raw);
            Assert.Equal((decimal)raw / 64, decimal.Parse(value.ToString(), System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    // TryFormat (option G2 of the SVG memory analysis) must give the text the SVG goldens were written with: the formatting
    // ToString had before, kept here as the reference.
    [Fact]
    public void TryFormatGivesTheReferenceText()
    {
        static string Reference(int raw)
        {
            var magnitude = Math.Abs((long)raw);
            var whole = magnitude / FixedPoint.UnitsPerPixel;
            var remainder = magnitude % FixedPoint.UnitsPerPixel;
            var sign = raw < 0 ? "-" : string.Empty;
            return remainder == 0
                ? sign + whole.ToString(CultureInfo.InvariantCulture)
                : sign + whole.ToString(CultureInfo.InvariantCulture) + "." + (remainder * 15625).ToString("D6", CultureInfo.InvariantCulture).TrimEnd('0');
        }

        Span<char> text = stackalloc char[FixedPoint.MaxTextLength];
        var values = Enumerable.Range(-100_000, 200_001).Concat([int.MaxValue, int.MinValue, int.MinValue + 1, 1_000_000_001, -999_999_999]);
        foreach (var raw in values)
        {
            Assert.True(FixedPoint.FromRaw(raw).TryFormat(text, out var written, default, CultureInfo.InvariantCulture));
            Assert.Equal(Reference(raw), new string(text[..written]));
            Assert.Equal(Reference(raw), FixedPoint.FromRaw(raw).ToString());
        }
    }

    [Fact]
    public void TryFormatRefusesAShortDestination()
    {
        Span<char> text = stackalloc char[4];
        Assert.False(FixedPoint.FromRaw(-2401).TryFormat(text, out _, default, CultureInfo.InvariantCulture));
        Assert.True(FixedPoint.FromRaw(64).TryFormat(text, out var written, default, CultureInfo.InvariantCulture));
        Assert.Equal("1", new string(text[..written]));
        Assert.Equal("37.515625", string.Create(CultureInfo.InvariantCulture, $"{FixedPoint.FromRaw(2401)}"));
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
