using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Derivation;

/// <summary>
/// An integer polygon with <c>java.awt.Polygon</c> semantics: its bounds and the exact <c>contains</c> test,
/// including the bounding-box pre-check and the half-open treatment of boundary points.
/// </summary>
internal sealed class JavaPolygon
{
    private readonly GridPoint[] points;

    public JavaPolygon(IReadOnlyList<GridPoint> points)
    {
        this.points = [.. points];
        MinX = this.points.Min(point => point.X);
        MinY = this.points.Min(point => point.Y);
        Width = this.points.Max(point => point.X) - MinX;
        Height = this.points.Max(point => point.Y) - MinY;
    }

    public int MinX
    {
        get;
    }

    public int MinY
    {
        get;
    }

    /// <summary>Bounds width as <c>Polygon.getBounds</c> computes it: max x minus min x.</summary>
    public int Width
    {
        get;
    }

    public int Height
    {
        get;
    }

    public IReadOnlyList<GridPoint> Points => points;

    public bool Contains(double x, double y)
    {
        if (points.Length <= 2 || !(x >= MinX && y >= MinY && x < MinX + Width && y < MinY + Height))
        {
            return false;
        }

        var hits = 0;
        var lastX = points[^1].X;
        var lastY = points[^1].Y;
        for (var index = 0; index < points.Length; lastX = points[index].X, lastY = points[index].Y, index++)
        {
            var currentX = points[index].X;
            var currentY = points[index].Y;
            if (currentY == lastY)
            {
                continue;
            }

            int leftX;
            if (currentX < lastX)
            {
                if (x >= lastX)
                {
                    continue;
                }

                leftX = currentX;
            }
            else
            {
                if (x >= currentX)
                {
                    continue;
                }

                leftX = lastX;
            }

            double test1;
            double test2;
            if (currentY < lastY)
            {
                if (y < currentY || y >= lastY)
                {
                    continue;
                }

                if (x < leftX)
                {
                    hits++;
                    continue;
                }

                test1 = x - currentX;
                test2 = y - currentY;
            }
            else
            {
                if (y < lastY || y >= currentY)
                {
                    continue;
                }

                if (x < leftX)
                {
                    hits++;
                    continue;
                }

                test1 = x - lastX;
                test2 = y - lastY;
            }

            if (test1 < (test2 / (lastY - currentY) * (lastX - currentX)))
            {
                hits++;
            }
        }

        return (hits & 1) != 0;
    }
}
