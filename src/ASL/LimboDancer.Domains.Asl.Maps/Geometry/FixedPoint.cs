using System.Globalization;

namespace LimboDancer.Domains.Asl.Maps.Geometry;

/// <summary>
/// A board-space coordinate in 1/64 pixel units. Every value has an exact decimal form,
/// so Feature Model geometry and SVG output never depend on floating-point formatting.
/// </summary>
public readonly record struct FixedPoint(int Raw) : IComparable<FixedPoint>, ISpanFormattable
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

    /// <summary>
    /// The nearest 1/64 pixel value, halves away from zero. Standard geometry is exact in this unit; the hex sizes of
    /// a/b, BFP double-width, and Deluxe boards are not, so their geometry-derived points are rounded this way.
    /// </summary>
    public static FixedPoint FromPixels(double pixels)
    {
        var raw = Math.Round(pixels * UnitsPerPixel, MidpointRounding.AwayFromZero);
        if (double.IsNaN(raw) || raw < int.MinValue || raw > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(pixels), pixels, "Value is outside the fixed-point range.");
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

    /// <summary>The longest text a value formats to: a sign, eight digits of whole pixels, a point, and six decimals.</summary>
    public const int MaxTextLength = 16;

    /// <summary>Formats the exact pixel value with invariant culture and no trailing zeros.</summary>
    public override string ToString()
    {
        Span<char> text = stackalloc char[MaxTextLength];
        TryFormat(text, out var written, default, CultureInfo.InvariantCulture);
        return new string(text[..written]);
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>
    /// Writes <see cref="ToString()"/>'s text into a span, without a string in between, so SVG output can carry numbers straight
    /// into its buffer (option G2 of the SVG memory analysis). The format and provider are ignored: the text is always invariant.
    /// </summary>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    {
        charsWritten = 0;
        var magnitude = Math.Abs((long)Raw);
        var whole = magnitude / UnitsPerPixel;
        var remainder = magnitude % UnitsPerPixel;
        var at = 0;
        if (Raw < 0)
        {
            if (destination.IsEmpty)
            {
                return false;
            }

            destination[at++] = '-';
        }

        if (!whole.TryFormat(destination[at..], out var digits, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        at += digits;
        if (remainder != 0)
        {
            // remainder / 64 = remainder * 15625 / 1000000, exact in six decimal digits; trailing zeros are dropped.
            var fraction = remainder * 15625;
            var places = 6;
            while (fraction % 10 == 0)
            {
                fraction /= 10;
                places--;
            }

            if (destination.Length < at + 1 + places)
            {
                return false;
            }

            destination[at++] = '.';
            for (var place = places - 1; place >= 0; place--)
            {
                destination[at + place] = (char)('0' + (fraction % 10));
                fraction /= 10;
            }

            at += places;
        }

        charsWritten = at;
        return true;
    }
}
