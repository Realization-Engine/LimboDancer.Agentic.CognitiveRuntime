using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Rendering;

/// <summary>
/// An attribute value or path coordinate as <see cref="SvgWriter"/> and <see cref="PathData"/> take it: text, escaped when written; a
/// number, formatted straight into the output; path data, copied from its builder; or text already proven to need no escaping. Numbers
/// and paths never become strings on the way (option G2 of the SVG memory analysis). A string converts to a value implicitly.
/// </summary>
public readonly struct SvgValue : ISpanFormattable
{
    private readonly string? text;
    private readonly PathData? path;

    private SvgValue(SvgValueKind kind, string? text, int number, PathData? path)
    {
        Kind = kind;
        this.text = text;
        NumberValue = number;
        this.path = path;
    }

    internal SvgValueKind Kind
    {
        get;
    }

    /// <summary>The raw 1/64 pixel value of a <see cref="SvgValueKind.Fixed"/>, or the integer of an <see cref="SvgValueKind.Integer"/>.</summary>
    internal int NumberValue
    {
        get;
    }

    internal string TextValue => text ?? string.Empty;

    internal PathData? PathValue => path;

    public static implicit operator SvgValue(string text) => FromString(text);

    public static implicit operator SvgValue(PathData path) => FromPathData(path);

    public static SvgValue FromString(string text) => new(SvgValueKind.Text, text, 0, null);

    public static SvgValue FromPathData(PathData path) => new(SvgValueKind.Path, null, 0, path);

    /// <summary>A number in exact decimal form: the value rounded to the nearest 1/64 pixel, as <see cref="SvgWriter.Number(double)"/>.</summary>
    public static SvgValue Number(double value) => new(SvgValueKind.Fixed, null, FixedPoint.FromPixels(value).Raw, null);

    public static SvgValue Number(int value) => new(SvgValueKind.Integer, null, value, null);

    /// <summary>A raw 1/64 pixel fixed-point value, written exactly.</summary>
    public static SvgValue Fixed(int raw) => new(SvgValueKind.Fixed, null, raw, null);

    /// <summary>Text that holds no character needing escaping, such as figure path data proven by its reader; it is copied as it is.</summary>
    public static SvgValue Trusted(string text) => new(SvgValueKind.Trusted, text, 0, null);

    public override string ToString() => Kind switch
    {
        SvgValueKind.Fixed => FixedPoint.FromRaw(NumberValue).ToString(),
        SvgValueKind.Integer => NumberValue.ToString(CultureInfo.InvariantCulture),
        SvgValueKind.Path => path!.ToString(),
        _ => TextValue,
    };

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>The value's text, unescaped, into a span: what an interpolated string needs, without a string in between.</summary>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        switch (Kind)
        {
            case SvgValueKind.Fixed:
                return FixedPoint.FromRaw(NumberValue).TryFormat(destination, out charsWritten, default, CultureInfo.InvariantCulture);
            case SvgValueKind.Integer:
                return NumberValue.TryFormat(destination, out charsWritten, default, CultureInfo.InvariantCulture);
            case SvgValueKind.Path:
                return Copy(path!.ToString(), destination, out charsWritten);
            default:
                return Copy(TextValue, destination, out charsWritten);
        }
    }

    private static bool Copy(ReadOnlySpan<char> source, Span<char> destination, out int charsWritten)
    {
        charsWritten = source.TryCopyTo(destination) ? source.Length : 0;
        return charsWritten == source.Length;
    }
}

internal enum SvgValueKind
{
    Text,
    Trusted,
    Fixed,
    Integer,
    Path,
}
