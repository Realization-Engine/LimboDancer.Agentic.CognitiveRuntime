using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Los;

/// <summary>
/// The straight line an LOS check walks, from the source's LOS point to the target's, and how VASL classifies it
/// (<c>Map.LOSStatus</c>): horizontal when both points share a row, and along a 60-degree hexspine when its slope is
/// within VASL's range-dependent tolerance of tan 60.
/// </summary>
public readonly record struct LosLine(GridPoint Source, GridPoint Target)
{
    // StrictMath.tan(Math.toRadians(60.0)); toRadians multiplies by PI / 180.
    private static readonly double Tan60 = Math.Tan(60.0 * (Math.PI / 180.0));

    /// <summary>VASL's <c>LOSisHorizontal</c>: the two LOS points are on the same grid row.</summary>
    public bool IsHorizontal => Source.Y == Target.Y;

    /// <summary>The absolute slope as VASL computes it: infinite for a vertical line, NaN for a single point.</summary>
    public double Slope => Math.Abs((double)(Source.Y - Target.Y) / (Source.X - Target.X));

    /// <summary>
    /// VASL's tolerance for "fuzzy" board geometry: 0.05 below range 5, 0.03 from 5 to 15, 0.015 beyond. VASL itself
    /// calls it a kludge.
    /// </summary>
    public static double Tolerance(int range) => range switch
    {
        >= 5 and <= 15 => 0.03,
        > 15 => 0.015,
        _ => 0.05,
    };

    /// <summary>VASL's <c>LOSis60Degree</c> for a line between hexes at the given range.</summary>
    public bool Is60Degree(int range) => Math.Abs(Slope - Tan60) < Tolerance(range);

    /// <summary>
    /// <c>java.awt.geom.Line2D.linesIntersect</c>: whether two segments touch, endpoints and collinear overlaps included,
    /// with Java's exact <c>relativeCCW</c> arithmetic.
    /// </summary>
    public static bool SegmentsIntersect(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4) =>
        RelativeCcw(x1, y1, x2, y2, x3, y3) * RelativeCcw(x1, y1, x2, y2, x4, y4) <= 0
        && RelativeCcw(x3, y3, x4, y4, x1, y1) * RelativeCcw(x3, y3, x4, y4, x2, y2) <= 0;

    // Line2D.relativeCCW
    private static int RelativeCcw(double x1, double y1, double x2, double y2, double px, double py)
    {
        x2 -= x1;
        y2 -= y1;
        px -= x1;
        py -= y1;
        var ccw = (px * y2) - (py * x2);
        if (ccw == 0.0)
        {
            ccw = (px * x2) + (py * y2);
            if (ccw > 0.0)
            {
                px -= x2;
                py -= y2;
                ccw = (px * x2) + (py * y2);
                if (ccw < 0.0)
                {
                    ccw = 0.0;
                }
            }
        }

        return ccw < 0.0 ? -1 : ccw > 0.0 ? 1 : 0;
    }
}
