using System.Globalization;

namespace LimboDancer.Domains.Asl.Maps.Geometry;

/// <summary>
/// A board-space coordinate in 1/64 pixel units. Every value has an exact decimal form,
/// so Feature Model geometry and SVG output never depend on floating-point formatting.
/// </summary>
public readonly record struct FixedPoint(int Raw) : IComparable<FixedPoint>
{
    public const int UnitsPerPixel = 64;

    public static FixedPoint Zero => default;

    public static FixedPoint FromPixels(int pixels) => new(checked(pixels * UnitsPerPixel));

    public static FixedPoint FromRaw(int raw) => new(raw);

    /// <summary>Converts an exact 1/64 pixel value; throws when the value is not representable.</summary>
    public static FixedPoint FromExactPixels(double pixels)
    {
        var scaled = pixels * UnitsPerPixel;
        var raw = Math.Round(scaled);
        if (raw != scaled || raw < int.MinValue || raw > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(pixels), pixels, "Value is not an exact 1/64 pixel quantity.");
        }

        return new FixedPoint((int)raw);
    }

    public double ToPixels() => (double)Raw / UnitsPerPixel;

    public int CompareTo(FixedPoint other) => Raw.CompareTo(other.Raw);

    public static FixedPoint operator +(FixedPoint left, FixedPoint right) => new(checked(left.Raw + right.Raw));

    public static FixedPoint operator -(FixedPoint left, FixedPoint right) => new(checked(left.Raw - right.Raw));

    public static FixedPoint operator -(FixedPoint value) => new(checked(-value.Raw));

    public static bool operator <(FixedPoint left, FixedPoint right) => left.Raw < right.Raw;

    public static bool operator >(FixedPoint left, FixedPoint right) => left.Raw > right.Raw;

    public static bool operator <=(FixedPoint left, FixedPoint right) => left.Raw <= right.Raw;

    public static bool operator >=(FixedPoint left, FixedPoint right) => left.Raw >= right.Raw;

    public static FixedPoint Add(FixedPoint left, FixedPoint right) => left + right;

    public static FixedPoint Subtract(FixedPoint left, FixedPoint right) => left - right;

    public static FixedPoint Negate(FixedPoint value) => -value;

    /// <summary>Formats the exact pixel value with invariant culture and no trailing zeros.</summary>
    public override string ToString()
    {
        var magnitude = Math.Abs((long)Raw);
        var whole = magnitude / UnitsPerPixel;
        var remainder = magnitude % UnitsPerPixel;
        var sign = Raw < 0 ? "-" : string.Empty;
        if (remainder == 0)
        {
            return sign + whole.ToString(CultureInfo.InvariantCulture);
        }

        // remainder / 64 = remainder * 15625 / 1000000, exact in six decimal digits.
        var fraction = (remainder * 15625).ToString("D6", CultureInfo.InvariantCulture).TrimEnd('0');
        return sign + whole.ToString(CultureInfo.InvariantCulture) + "." + fraction;
    }
}
