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
        "size-figures", "mg", "ft", "dc", "radio", "mortar", "latw", "smoke", "gun", "aa-gun", "rcl", "limbered-gun",
        "tank", "halftrack", "armored-car", "truck", "motorcycle", "wreck",
        "crosshair", "foxhole", "trench", "wire", "mines", "roadblock", "pillbox", "fortified", "fire", "rubble", "residual",
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
            case "mg" or "ft" or "dc" or "radio" or "mortar" or "latw" or "smoke" or "gun" or "aa-gun" or "rcl" or "limbered-gun"
                or "tank" or "halftrack" or "armored-car" or "truck" or "motorcycle" or "wreck"
                or "crosshair" or "foxhole" or "trench" or "wire" or "mines" or "roadblock" or "pillbox" or "fortified" or "fire" or "rubble" or "residual":
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

            // Guns point east, so a sheet can rotate them to their facing.
            case "gun":
                Polygon((0.3, 0.44), (0.98, 0.47), (0.98, 0.53), (0.3, 0.56));
                Polygon((0.26, 0.3), (0.36, 0.3), (0.36, 0.7), (0.26, 0.7));
                Circle(0.34, 0.26, 0.08, filled: false);
                Circle(0.34, 0.74, 0.08, filled: false);
                Line(0.26, 0.46, 0.04, 0.3);
                Line(0.26, 0.54, 0.04, 0.7);
                break;
            case "aa-gun":
                Circle(0.45, 0.5, 0.2, filled: false);
                Polygon((0.4, 0.46), (0.95, 0.2), (0.98, 0.26), (0.45, 0.56));
                Line(0.2, 0.85, 0.7, 0.15);
                Line(0.2, 0.15, 0.7, 0.85);
                break;
            case "rcl":
                Polygon((0.06, 0.44), (0.96, 0.44), (0.96, 0.56), (0.06, 0.56));
                Polygon((0.02, 0.4), (0.1, 0.44), (0.1, 0.56), (0.02, 0.6));
                Line(0.5, 0.56, 0.35, 0.85);
                Line(0.5, 0.56, 0.65, 0.85);
                Line(0.5, 0.56, 0.5, 0.9);
                break;
            // Vehicles are drawn from above, front to the east, so a sheet can turn them to their hull facing.
            case "tank":
                Polygon((0.1, 0.22), (0.9, 0.22), (0.9, 0.32), (0.1, 0.32));
                Polygon((0.1, 0.68), (0.9, 0.68), (0.9, 0.78), (0.1, 0.78));
                Polygon((0.14, 0.34), (0.86, 0.34), (0.86, 0.66), (0.14, 0.66));
                Circle(0.46, 0.5, 0.11, filled: false);
                Line(0.57, 0.5, 0.98, 0.5);
                break;
            case "halftrack":
                Polygon((0.08, 0.26), (0.55, 0.26), (0.55, 0.36), (0.08, 0.36));
                Polygon((0.08, 0.64), (0.55, 0.64), (0.55, 0.74), (0.08, 0.74));
                Polygon((0.1, 0.36), (0.8, 0.36), (0.92, 0.44), (0.92, 0.56), (0.8, 0.64), (0.1, 0.64));
                Circle(0.78, 0.28, 0.06, filled: false);
                Circle(0.78, 0.72, 0.06, filled: false);
                break;
            case "armored-car":
                Polygon((0.12, 0.34), (0.8, 0.34), (0.92, 0.42), (0.92, 0.58), (0.8, 0.66), (0.12, 0.66));
                foreach (var u in new[] { 0.24, 0.72 })
                {
                    Circle(u, 0.27, 0.06, filled: false);
                    Circle(u, 0.73, 0.06, filled: false);
                }

                Line(0.52, 0.5, 0.96, 0.5);
                break;
            case "truck":
                Polygon((0.06, 0.32), (0.62, 0.32), (0.62, 0.68), (0.06, 0.68));
                Polygon((0.66, 0.36), (0.9, 0.36), (0.95, 0.44), (0.95, 0.56), (0.9, 0.64), (0.66, 0.64));
                foreach (var u in new[] { 0.2, 0.46, 0.8 })
                {
                    Circle(u, 0.27, 0.05, filled: false);
                    Circle(u, 0.73, 0.05, filled: false);
                }

                break;
            case "motorcycle":
                Line(0.14, 0.5, 0.86, 0.5);
                Circle(0.14, 0.5, 0.08, filled: false);
                Circle(0.86, 0.5, 0.08, filled: false);
                Line(0.7, 0.36, 0.7, 0.64);
                Polygon((0.34, 0.54), (0.5, 0.54), (0.5, 0.74), (0.34, 0.74));
                break;
            case "wreck":
                Polygon((0.12, 0.3), (0.84, 0.26), (0.9, 0.7), (0.16, 0.74));
                Line(0.2, 0.2, 0.8, 0.8);
                Line(0.8, 0.2, 0.2, 0.8);
                Circle(0.5, 0.14, 0.07);
                Circle(0.62, 0.08, 0.05);
                break;
            // Entities that are not units (ASL-UNIT-026). The roadblock's bar points east, so a sheet can turn it to its hexside.
            case "crosshair":
                Circle(0.5, 0.5, 0.3, filled: false);
                Line(0.5, 0.06, 0.5, 0.34);
                Line(0.5, 0.66, 0.5, 0.94);
                Line(0.06, 0.5, 0.34, 0.5);
                Line(0.66, 0.5, 0.94, 0.5);
                break;
            case "foxhole":
                Polygon((0.1, 0.52), (0.2, 0.7), (0.8, 0.7), (0.9, 0.52), (0.78, 0.6), (0.22, 0.6));
                break;
            case "trench":
                Polygon((0.04, 0.44), (0.3, 0.44), (0.4, 0.3), (0.6, 0.3), (0.7, 0.44), (0.96, 0.44), (0.96, 0.56), (0.64, 0.56), (0.56, 0.42),
                    (0.44, 0.42), (0.36, 0.56), (0.04, 0.56));
                break;
            case "wire":
                Line(0.06, 0.62, 0.94, 0.62);
                foreach (var u in new[] { 0.14, 0.32, 0.5, 0.68, 0.86 })
                {
                    Line(u - 0.07, 0.36, u + 0.07, 0.62);
                    Line(u + 0.07, 0.36, u - 0.07, 0.62);
                }

                break;
            case "mines":
                foreach (var (u, v) in new[] { (0.25, 0.3), (0.55, 0.25), (0.8, 0.45), (0.35, 0.62), (0.65, 0.72) })
                {
                    Circle(u, v, 0.08);
                }

                break;
            case "roadblock":
                Polygon((0.7, 0.1), (0.86, 0.1), (0.86, 0.9), (0.7, 0.9));
                Line(0.2, 0.5, 0.62, 0.5);
                Polygon((0.62, 0.42), (0.7, 0.5), (0.62, 0.58));
                break;
            case "pillbox":
                Polygon((0.12, 0.8), (0.12, 0.42), (0.5, 0.18), (0.88, 0.42), (0.88, 0.8));
                Line(0.3, 0.56, 0.7, 0.56);
                break;
            case "fortified":
                Polygon((0.14, 0.86), (0.14, 0.3), (0.32, 0.3), (0.32, 0.18), (0.44, 0.18), (0.44, 0.3), (0.56, 0.3), (0.56, 0.18), (0.68, 0.18),
                    (0.68, 0.3), (0.86, 0.3), (0.86, 0.86));
                break;
            case "fire":
                Polygon((0.5, 0.06), (0.66, 0.34), (0.8, 0.24), (0.84, 0.6), (0.7, 0.9), (0.3, 0.9), (0.16, 0.6), (0.26, 0.36), (0.38, 0.46));
                break;
            case "rubble":
                Polygon((0.1, 0.8), (0.24, 0.56), (0.4, 0.8));
                Polygon((0.34, 0.8), (0.5, 0.44), (0.7, 0.8));
                Polygon((0.62, 0.8), (0.76, 0.6), (0.92, 0.8));
                break;
            case "residual":
                Circle(0.5, 0.5, 0.34, filled: false);
                Circle(0.5, 0.5, 0.12);
                break;
            case "limbered-gun":
                Polygon((0.02, 0.47), (0.62, 0.44), (0.62, 0.56), (0.02, 0.53));
                Polygon((0.58, 0.3), (0.68, 0.3), (0.68, 0.7), (0.58, 0.7));
                Circle(0.63, 0.24, 0.1, filled: false);
                Circle(0.63, 0.76, 0.1, filled: false);
                Line(0.68, 0.5, 0.98, 0.5);
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
