using LimboDancer.Domains.Asl.Maps.Rendering;

namespace LimboDancer.Domains.Asl.Units.Rendering;

/// <summary>
/// Original glyphs drawn from SVG primitives: figures for unit size (1 to 3, standing or prone) and simple
/// equipment shapes. None is taken from counter artwork (ASL-UNIT-072).
/// </summary>
internal static class Glyphs
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
        "size-figures", "mg", "ft", "dc", "radio", "mortar", "latw", "smoke",
    };

    /// <summary>Draws a glyph in a box; returns false for an unknown name.</summary>
    public static bool Draw(SvgWriter svg, string name, string? argument, Box box, string color, int figures)
    {
        switch (name)
        {
            case "size-figures":
                if (argument == "prone")
                {
                    ProneFigures(svg, box, color, figures);
                }
                else
                {
                    StandingFigures(svg, box, color, figures);
                }

                return true;
            case "mg" or "ft" or "dc" or "radio" or "mortar" or "latw" or "smoke":
                Equipment(svg, name, box, color);
                return true;
            default:
                return false;
        }
    }

    private static void StandingFigures(SvgWriter svg, Box box, string color, int count)
    {
        count = Math.Clamp(count, 1, 3);
        var height = Math.Min(box.Height * 0.92, box.Width * 0.92 / ((0.42 * count) + 0.12));
        var spacing = height * 0.42;
        var bottom = box.CenterY + (height / 2);
        for (var index = 0; index < count; index++)
        {
            var x = box.CenterX + ((index - ((count - 1) / 2.0)) * spacing);
            var width = height * 0.5;
            svg.Empty("circle", ("cx", N(x)), ("cy", N(bottom - height + (height * 0.15))), ("r", N(height * 0.15)), ("fill", color));
            var path = new PathData()
                .MoveTo(N(x - (width * 0.46)), N(bottom))
                .LineTo(N(x - (width * 0.32)), N(bottom - (height * 0.68)))
                .LineTo(N(x + (width * 0.32)), N(bottom - (height * 0.68)))
                .LineTo(N(x + (width * 0.46)), N(bottom))
                .Close();
            svg.Empty("path", ("d", path.ToString()), ("fill", color));
        }
    }

    private static void ProneFigures(SvgWriter svg, Box box, string color, int count)
    {
        count = Math.Clamp(count, 1, 3);
        var length = Math.Min(box.Width * 0.92, box.Height * 0.92 / (0.34 * count));
        var thickness = length * 0.2;
        var left = box.CenterX - (length / 2);
        for (var index = 0; index < count; index++)
        {
            var y = box.CenterY + ((index - ((count - 1) / 2.0)) * length * 0.34);
            svg.Empty("circle", ("cx", N(left + (length * 0.1))), ("cy", N(y)), ("r", N(length * 0.1)), ("fill", color));
            var path = new PathData()
                .MoveTo(N(left + (length * 0.24)), N(y - (thickness / 2)))
                .LineTo(N(left + length), N(y - (thickness * 0.3)))
                .LineTo(N(left + length), N(y + (thickness * 0.3)))
                .LineTo(N(left + (length * 0.24)), N(y + (thickness / 2)))
                .Close();
            svg.Empty("path", ("d", path.ToString()), ("fill", color));
        }
    }

    private static void Equipment(SvgWriter svg, string name, Box box, string color)
    {
        var side = Math.Min(box.Width, box.Height) * 0.92;
        var x0 = box.CenterX - (side / 2);
        var y0 = box.CenterY - (side / 2);
        string X(double u) => N(x0 + (u * side));
        string Y(double v) => N(y0 + (v * side));
        void Polygon(params (double U, double V)[] points)
        {
            var path = new PathData().MoveTo(X(points[0].U), Y(points[0].V));
            foreach (var (u, v) in points.Skip(1))
            {
                path.LineTo(X(u), Y(v));
            }

            svg.Empty("path", ("d", path.Close().ToString()), ("fill", color));
        }

        void Line(double u0, double v0, double u1, double v1) =>
            svg.Empty("line", ("x1", X(u0)), ("y1", Y(v0)), ("x2", X(u1)), ("y2", Y(v1)), ("stroke", color), ("stroke-width", N(side * 0.06)),
                ("stroke-linecap", "round"));

        void Circle(double u, double v, double r, bool filled = true) =>
            svg.Empty("circle", ("cx", X(u)), ("cy", Y(v)), ("r", N(r * side)), ("fill", filled ? color : "none"), ("stroke", color),
                ("stroke-width", N(side * 0.05)));

        switch (name)
        {
            case "mg":
                Polygon((0.02, 0.44), (0.12, 0.4), (0.5, 0.4), (0.5, 0.6), (0.12, 0.6), (0.02, 0.66));
                Polygon((0.5, 0.46), (0.97, 0.46), (0.97, 0.54), (0.5, 0.54));
                Line(0.64, 0.54, 0.56, 0.82);
                Line(0.64, 0.54, 0.74, 0.82);
                break;
            case "ft":
                Polygon((0.12, 0.28), (0.4, 0.28), (0.4, 0.86), (0.12, 0.86));
                Line(0.4, 0.4, 0.55, 0.5);
                Polygon((0.55, 0.46), (0.82, 0.46), (0.82, 0.54), (0.55, 0.54));
                Polygon((0.84, 0.5), (0.98, 0.38), (0.98, 0.62));
                break;
            case "dc":
                Polygon((0.18, 0.4), (0.82, 0.4), (0.82, 0.86), (0.18, 0.86));
                Line(0.5, 0.4, 0.62, 0.18);
                Circle(0.66, 0.12, 0.05);
                break;
            case "radio":
                Polygon((0.18, 0.38), (0.82, 0.38), (0.82, 0.88), (0.18, 0.88));
                Line(0.7, 0.38, 0.86, 0.06);
                Circle(0.38, 0.63, 0.1, filled: false);
                break;
            case "mortar":
                Line(0.14, 0.88, 0.86, 0.88);
                Polygon((0.4, 0.84), (0.66, 0.16), (0.76, 0.2), (0.5, 0.88));
                Line(0.62, 0.5, 0.8, 0.88);
                break;
            case "latw":
                Polygon((0.04, 0.45), (0.78, 0.45), (0.78, 0.55), (0.04, 0.55));
                Polygon((0.78, 0.36), (0.98, 0.5), (0.78, 0.64));
                Line(0.3, 0.55, 0.3, 0.72);
                break;
            case "smoke":
                Circle(0.32, 0.6, 0.18);
                Circle(0.55, 0.45, 0.22);
                Circle(0.72, 0.64, 0.16);
                break;
        }
    }

    private static string N(double value) => SvgWriter.Number(value);
}

/// <summary>A rectangle in board pixels.</summary>
internal readonly record struct Box(double X, double Y, double Width, double Height)
{
    public double CenterX => X + (Width / 2);

    public double CenterY => Y + (Height / 2);

    public double Right => X + Width;

    public double Bottom => Y + Height;
}
