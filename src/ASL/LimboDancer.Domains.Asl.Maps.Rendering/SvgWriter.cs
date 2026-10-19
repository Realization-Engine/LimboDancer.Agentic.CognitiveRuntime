using System.Buffers;
using System.Globalization;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Rendering;

/// <summary>
/// A forward-only SVG writer whose output depends only on the calls made (Architecture and Rendering Design,
/// section 3.2): no declaration, no indentation, LF only, attributes in call order, and numbers formatted exactly.
/// </summary>
/// <remarks>
/// The output goes into one pooled buffer, which <see cref="ToString"/> turns into the document and returns to the pool; numbers and
/// path data are written straight into it, and text is copied in runs between the characters it escapes (options G1 to G3 of the SVG
/// memory analysis). After <see cref="ToString"/> the writer takes no more output.
/// </remarks>
public sealed class SvgWriter
{
    private static readonly SearchValues<char> AttributeSpecials = SearchValues.Create("&<\"");
    private static readonly SearchValues<char> TextSpecials = SearchValues.Create("&<>");

    private readonly Stack<string> open = new();
    private char[] buffer = ArrayPool<char>.Shared.Rent(4096);
    private int length;
    private string? document;

    public void Start(string name, params (string Name, SvgValue Value)[] attributes)
    {
        WriteTag(name, attributes);
        Append('>');
        open.Push(name);
    }

    public void Empty(string name, params (string Name, SvgValue Value)[] attributes)
    {
        WriteTag(name, attributes);
        Append("/>");
    }

    public void Text(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Writable();
        Escaped(text, TextSpecials);
    }

    /// <summary>
    /// Appends markup another <see cref="SvgWriter"/> made, unchanged: a fragment kept from an earlier document, such as a hex of the counter
    /// layer (the Counter Layer Design, section 7). The caller vouches that it is whole elements, with every element it opens closed.
    /// </summary>
    public void Fragment(string markup)
    {
        ArgumentNullException.ThrowIfNull(markup);
        Writable();
        Append(markup);
    }

    public void End()
    {
        Writable();
        Append("</");
        Append(open.Pop());
        Append('>');
    }

    public override string ToString()
    {
        if (open.Count != 0)
        {
            throw new InvalidOperationException($"Element <{open.Peek()}> is still open.");
        }

        if (document is null)
        {
            document = new string(buffer, 0, length);
            ArrayPool<char>.Shared.Return(buffer);
            buffer = [];
        }

        return document;
    }

    /// <summary>A number in exact decimal form: the value rounded to the nearest 1/64 pixel, which leaves standard geometry exact.</summary>
    public static string Number(double value) => FixedPoint.FromPixels(value).ToString();

    public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>A raw 1/64 pixel fixed-point value, written exactly.</summary>
    public static string Fixed(int raw) => FixedPoint.FromRaw(raw).ToString();

    private void WriteTag(string name, (string Name, SvgValue Value)[] attributes)
    {
        Writable();
        Append('<');
        Append(name);
        foreach (var (attributeName, value) in attributes)
        {
            Append(' ');
            Append(attributeName);
            Append("=\"");
            Write(value);
            Append('"');
        }
    }

    private void Write(SvgValue value)
    {
        switch (value.Kind)
        {
            case SvgValueKind.Fixed:
            case SvgValueKind.Integer:
                Span<char> number = stackalloc char[FixedPoint.MaxTextLength];
                value.TryFormat(number, out var written, default, CultureInfo.InvariantCulture);
                Append(number[..written]);
                break;
            case SvgValueKind.Path:
                // Path data holds commands, digits, signs, points, and spaces only: nothing to escape.
                foreach (var chunk in value.PathValue!.Text.GetChunks())
                {
                    Append(chunk.Span);
                }

                break;
            case SvgValueKind.Trusted:
                Append(value.TextValue);
                break;
            default:
                Escaped(value.TextValue, AttributeSpecials);
                break;
        }
    }

    /// <summary>
    /// Appends text with the given characters escaped: the runs between them are copied whole, so nothing is allocated for the
    /// characters that need no escaping (option G1 of the SVG memory analysis).
    /// </summary>
    private void Escaped(ReadOnlySpan<char> text, SearchValues<char> specials)
    {
        while (text.IndexOfAny(specials) is var at and >= 0)
        {
            Append(text[..at]);
            Append(text[at] switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                _ => "&quot;",
            });
            text = text[(at + 1)..];
        }

        Append(text);
    }

    private void Append(char character)
    {
        if (length == buffer.Length)
        {
            Grow(1);
        }

        buffer[length++] = character;
    }

    private void Append(ReadOnlySpan<char> text)
    {
        if (length + text.Length > buffer.Length)
        {
            Grow(text.Length);
        }

        text.CopyTo(buffer.AsSpan(length));
        length += text.Length;
    }

    private void Grow(int more)
    {
        var larger = ArrayPool<char>.Shared.Rent(Math.Max(buffer.Length * 2, length + more));
        buffer.AsSpan(0, length).CopyTo(larger);
        ArrayPool<char>.Shared.Return(buffer);
        buffer = larger;
    }

    private void Writable()
    {
        if (document is not null)
        {
            throw new InvalidOperationException("The document has been written; the writer takes no more output.");
        }
    }
}

/// <summary>SVG path data in absolute commands with exact numbers: <c>M</c>, <c>H</c>, <c>V</c>, <c>L</c>, <c>C</c>, <c>Z</c>.</summary>
public sealed class PathData
{
    private readonly StringBuilder builder = new();

    public bool IsEmpty => builder.Length == 0;

    /// <summary>The commands written so far, for <see cref="SvgWriter"/> to copy without a string in between.</summary>
    internal StringBuilder Text => builder;

    public PathData MoveTo(SvgValue x, SvgValue y) => Append('M', x, y);

    public PathData LineTo(SvgValue x, SvgValue y) => Append('L', x, y);

    /// <summary>A cubic Bézier segment to (x, y) with two control points.</summary>
    public PathData CubicTo(SvgValue x1, SvgValue y1, SvgValue x2, SvgValue y2, SvgValue x, SvgValue y)
    {
        builder.Append('C');
        Number(x1).Append(' ');
        Number(y1).Append(' ');
        Number(x2).Append(' ');
        Number(y2).Append(' ');
        Number(x).Append(' ');
        Number(y);
        return this;
    }

    public PathData Horizontal(SvgValue x)
    {
        builder.Append('H');
        Number(x);
        return this;
    }

    public PathData Vertical(SvgValue y)
    {
        builder.Append('V');
        Number(y);
        return this;
    }

    public PathData Close()
    {
        builder.Append('Z');
        return this;
    }

    /// <summary>A rectilinear integer ring as <c>M x y</c> followed by alternating <c>H</c> and <c>V</c> commands.</summary>
    public PathData Ring(IReadOnlyList<GridPoint> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        MoveTo(SvgValue.Number(vertices[0].X), SvgValue.Number(vertices[0].Y));
        for (var index = 1; index < vertices.Count; index++)
        {
            var previous = vertices[index - 1];
            var vertex = vertices[index];
            if (vertex.Y == previous.Y)
            {
                Horizontal(SvgValue.Number(vertex.X));
            }
            else
            {
                Vertical(SvgValue.Number(vertex.Y));
            }
        }

        return Close();
    }

    /// <summary>A closed polygon of exact pixel points.</summary>
    public PathData Polygon(IReadOnlyList<PixelPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        MoveTo(SvgValue.Number(points[0].X), SvgValue.Number(points[0].Y));
        for (var index = 1; index < points.Count; index++)
        {
            LineTo(SvgValue.Number(points[index].X), SvgValue.Number(points[index].Y));
        }

        return Close();
    }

    public override string ToString() => builder.ToString();

    private PathData Append(char command, SvgValue x, SvgValue y)
    {
        builder.Append(command);
        Number(x).Append(' ');
        Number(y);
        return this;
    }

    // A coordinate: a number formatted straight into the builder, or text as given.
    private StringBuilder Number(SvgValue value)
    {
        if (value.Kind is SvgValueKind.Fixed or SvgValueKind.Integer)
        {
            Span<char> number = stackalloc char[FixedPoint.MaxTextLength];
            value.TryFormat(number, out var written, default, CultureInfo.InvariantCulture);
            return builder.Append(number[..written]);
        }

        return builder.Append(value.ToString());
    }
}
