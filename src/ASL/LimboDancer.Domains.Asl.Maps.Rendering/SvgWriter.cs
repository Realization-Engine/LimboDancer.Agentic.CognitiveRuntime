using System.Globalization;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Rendering;

/// <summary>
/// A forward-only SVG writer whose output depends only on the calls made (Architecture and Rendering Design,
/// section 3.2): no declaration, no indentation, LF only, attributes in call order, and numbers formatted exactly.
/// </summary>
public sealed class SvgWriter
{
    private readonly StringBuilder builder = new();
    private readonly Stack<string> open = new();

    public void Start(string name, params (string Name, string Value)[] attributes)
    {
        WriteTag(name, attributes);
        builder.Append('>');
        open.Push(name);
    }

    public void Empty(string name, params (string Name, string Value)[] attributes)
    {
        WriteTag(name, attributes);
        builder.Append("/>");
    }

    public void Text(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        foreach (var character in text)
        {
            builder.Append(character switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                _ => character.ToString(),
            });
        }
    }

    public void End()
    {
        builder.Append("</").Append(open.Pop()).Append('>');
    }

    public override string ToString()
    {
        if (open.Count != 0)
        {
            throw new InvalidOperationException($"Element <{open.Peek()}> is still open.");
        }

        return builder.ToString();
    }

    /// <summary>An exact number: integers as written, other values only when they are exact multiples of 1/64.</summary>
    public static string Number(double value) => FixedPoint.FromExactPixels(value).ToString();

    public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private void WriteTag(string name, (string Name, string Value)[] attributes)
    {
        builder.Append('<').Append(name);
        foreach (var (attributeName, value) in attributes)
        {
            builder.Append(' ').Append(attributeName).Append("=\"");
            foreach (var character in value)
            {
                builder.Append(character switch
                {
                    '&' => "&amp;",
                    '<' => "&lt;",
                    '"' => "&quot;",
                    _ => character.ToString(),
                });
            }

            builder.Append('"');
        }
    }
}

/// <summary>SVG path data in absolute commands with exact numbers: <c>M</c>, <c>H</c>, <c>V</c>, <c>L</c>, <c>Z</c>.</summary>
public sealed class PathData
{
    private readonly StringBuilder builder = new();

    public bool IsEmpty => builder.Length == 0;

    public PathData MoveTo(string x, string y) => Append('M', x, y);

    public PathData LineTo(string x, string y) => Append('L', x, y);

    public PathData Horizontal(string x)
    {
        builder.Append('H').Append(x);
        return this;
    }

    public PathData Vertical(string y)
    {
        builder.Append('V').Append(y);
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
        MoveTo(SvgWriter.Number(vertices[0].X), SvgWriter.Number(vertices[0].Y));
        for (var index = 1; index < vertices.Count; index++)
        {
            var previous = vertices[index - 1];
            var vertex = vertices[index];
            if (vertex.Y == previous.Y)
            {
                Horizontal(SvgWriter.Number(vertex.X));
            }
            else
            {
                Vertical(SvgWriter.Number(vertex.Y));
            }
        }

        return Close();
    }

    /// <summary>A closed polygon of exact pixel points.</summary>
    public PathData Polygon(IReadOnlyList<PixelPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        MoveTo(SvgWriter.Number(points[0].X), SvgWriter.Number(points[0].Y));
        for (var index = 1; index < points.Count; index++)
        {
            LineTo(SvgWriter.Number(points[index].X), SvgWriter.Number(points[index].Y));
        }

        return Close();
    }

    public override string ToString() => builder.ToString();

    private PathData Append(char command, string x, string y)
    {
        builder.Append(command).Append(x).Append(' ').Append(y);
        return this;
    }
}
