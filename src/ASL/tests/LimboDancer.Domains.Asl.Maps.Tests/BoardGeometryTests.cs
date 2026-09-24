using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class BoardGeometryTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    [Fact]
    public void StandardGeomorphicBoardMatchesVaslConstants()
    {
        Assert.Equal(33, Geometry.WidthInHexes);
        Assert.Equal(10, Geometry.HeightInHexes);
        Assert.Equal(1800, Geometry.GridWidth);
        Assert.Equal(645, Geometry.GridHeight);
        Assert.Equal(56.25, Geometry.HexWidth);
        Assert.Equal(64.5, Geometry.HexHeight);
        Assert.Equal(0.0, Geometry.A1CenterX);
        Assert.Equal(32.25, Geometry.A1CenterY);

        // 17 even columns of 10 hexes and 16 odd columns of 11, as the LOSData stairway section counts them.
        Assert.Equal(346, Geometry.HexCount);
        Assert.Equal(346, Geometry.Hexes().Count());
    }

    [Fact]
    public void HexesAreEnumeratedColumnMajorLikeVasl()
    {
        var hexes = Geometry.Hexes().Take(12).ToArray();
        Assert.Equal(new HexIndex(0, 0), hexes[0]);
        Assert.Equal(new HexIndex(0, 9), hexes[9]);
        Assert.Equal(new HexIndex(1, 0), hexes[10]);
        Assert.Equal(new HexIndex(1, 1), hexes[11]);
    }

    [Theory]
    [InlineData(0, 0, "A1")]
    [InlineData(0, 9, "A10")]
    [InlineData(1, 0, "B0")]
    [InlineData(1, 10, "B10")]
    [InlineData(4, 3, "E4")]
    [InlineData(26, 7, "AA8")]
    [InlineData(32, 9, "GG10")]
    public void NamesFollowVaslRowNumbering(int column, int row, string expected)
    {
        var hex = new HexIndex(column, row);
        Assert.Equal(expected, Geometry.NameOf(hex).ToString());
        Assert.Equal(hex, Geometry.IndexOf(HexName.Parse(expected)));
    }

    [Fact]
    public void EveryHexNameRoundTrips()
    {
        foreach (var hex in Geometry.Hexes())
        {
            var text = Geometry.NameOf(hex).ToString();
            Assert.Equal(hex, Geometry.IndexOf(HexName.Parse(text)));
        }
    }

    [Theory]
    [InlineData("A0")]
    [InlineData("A11")]
    [InlineData("B11")]
    [InlineData("HH1")]
    public void HexesOffTheBoardAreRejected(string name)
    {
        Assert.False(Geometry.TryGetIndex(HexName.Parse(name), out _));
    }

    [Fact]
    public void E4CenterAndSamplePointsMatchVaslArithmetic()
    {
        var e4 = Geometry.IndexOf(HexName.Parse("E4"));
        Assert.Equal(new PixelPoint(225.0, 225.75), Geometry.CenterDot(e4));
        Assert.Equal(new GridPoint(225, 225), Geometry.CenterPoint(e4));

        Assert.Equal(new GridPoint(225, 194), Geometry.EdgeSamplePoint(e4, HexsideDirection.North));
        Assert.Equal(new GridPoint(251, 210), Geometry.EdgeSamplePoint(e4, HexsideDirection.NorthEast));
        Assert.Equal(new GridPoint(251, 240), Geometry.EdgeSamplePoint(e4, HexsideDirection.SouthEast));
        Assert.Equal(new GridPoint(225, 257), Geometry.EdgeSamplePoint(e4, HexsideDirection.South));
        Assert.Equal(new GridPoint(198, 240), Geometry.EdgeSamplePoint(e4, HexsideDirection.SouthWest));
        Assert.Equal(new GridPoint(198, 210), Geometry.EdgeSamplePoint(e4, HexsideDirection.NorthWest));
    }

    [Fact]
    public void BorderRoundsHalfUpLikeJavaNotToEven()
    {
        var e4 = Geometry.IndexOf(HexName.Parse("E4"));
        Assert.Equal(
            new[]
            {
                new PixelPoint(206.25, 193.5),
                new PixelPoint(243.75, 193.5),
                new PixelPoint(262.5, 225.75),
                new PixelPoint(243.75, 258.0),
                new PixelPoint(206.25, 258.0),
                new PixelPoint(187.5, 225.75),
            },
            Geometry.Vertices(e4));

        // 262.5 rounds to 263 in Java; .NET's default banker's rounding would give 262.
        Assert.Equal(
            new[]
            {
                new GridPoint(206, 194),
                new GridPoint(244, 194),
                new GridPoint(263, 226),
                new GridPoint(244, 258),
                new GridPoint(206, 258),
                new GridPoint(188, 226),
            },
            Geometry.Border(e4));
    }

    [Fact]
    public void EdgeHalfHexSamplesKeepVaslTruncationAndClamping()
    {
        var a1 = new HexIndex(0, 0);
        Assert.Equal(new GridPoint(0, 32), Geometry.CenterPoint(a1));
        Assert.Equal(new GridPoint(0, 1), Geometry.EdgeSamplePoint(a1, HexsideDirection.North));

        // Off the left edge: (int)(-26.93) truncates toward zero, and only exactly -1 is clamped.
        Assert.Equal(new GridPoint(-26, 17), Geometry.EdgeSamplePoint(a1, HexsideDirection.NorthWest));

        // B0 is the top half hex of column B; its north sample lies above the grid.
        Assert.Equal(new GridPoint(56, -31), Geometry.EdgeSamplePoint(new HexIndex(1, 0), HexsideDirection.North));

        // B10 is the bottom half hex; its center y of 645 equals the grid height and clamps to 644.
        Assert.Equal(new PixelPoint(56.25, 645.0), Geometry.CenterDot(new HexIndex(1, 10)));
        Assert.Equal(new GridPoint(56, 644), Geometry.CenterPoint(new HexIndex(1, 10)));

        // GG sits on the right edge: x = 1800 clamps to 1799 before anything else is computed.
        Assert.Equal(new PixelPoint(1799.0, 32.25), Geometry.CenterDot(new HexIndex(32, 0)));
    }

    [Fact]
    public void EdgeSampleTruncationIsNotSensitiveToLastBitFloatingPointDifferences()
    {
        // Java and .NET cosine may differ in the last bit. Truncation is safe only if no sampled value lies
        // within a tiny distance of an integer, so check every x and y the standard geometry produces.
        var horizontalOffset = Math.Cos(30.0 * (Math.PI / 180.0)) * 32.25;
        foreach (var hex in Geometry.Hexes())
        {
            var dot = Geometry.CenterDot(hex);
            foreach (var value in new[] { horizontalOffset + dot.X - 1, -horizontalOffset + dot.X + 1 })
            {
                var distance = Math.Abs(value - Math.Round(value));
                Assert.True(distance > 1e-6, $"{Geometry.NameOf(hex)} sample x {value} is too close to an integer.");
            }
        }
    }

    [Fact]
    public void NeighborsFollowVaslAdjacencyAndAreSymmetric()
    {
        var e4 = Geometry.IndexOf(HexName.Parse("E4"));
        Assert.Equal("E3", Name(Geometry.Neighbor(e4, HexsideDirection.North)));
        Assert.Equal("F3", Name(Geometry.Neighbor(e4, HexsideDirection.NorthEast)));
        Assert.Equal("F4", Name(Geometry.Neighbor(e4, HexsideDirection.SouthEast)));
        Assert.Equal("E5", Name(Geometry.Neighbor(e4, HexsideDirection.South)));
        Assert.Equal("D4", Name(Geometry.Neighbor(e4, HexsideDirection.SouthWest)));
        Assert.Equal("D3", Name(Geometry.Neighbor(e4, HexsideDirection.NorthWest)));

        // Odd columns sit half a hex higher, so D4 meets E4 across D4's north-east side.
        var d4 = Geometry.IndexOf(HexName.Parse("D4"));
        Assert.Equal("E4", Name(Geometry.Neighbor(d4, HexsideDirection.NorthEast)));
        Assert.Equal("E5", Name(Geometry.Neighbor(d4, HexsideDirection.SouthEast)));

        foreach (var hex in Geometry.Hexes())
        {
            foreach (var side in HexsideDirections.All)
            {
                if (Geometry.Neighbor(hex, side) is { } neighbor)
                {
                    Assert.Equal(hex, Geometry.Neighbor(neighbor, side.Opposite()));
                }
            }
        }

        Assert.Null(Geometry.Neighbor(new HexIndex(0, 0), HexsideDirection.NorthWest));
        Assert.Null(Geometry.Neighbor(new HexIndex(1, 0), HexsideDirection.North));
    }

    [Fact]
    public void DistanceEqualsShortestPathThroughNeighbors()
    {
        foreach (var source in Geometry.Hexes())
        {
            var shortest = ShortestPaths(source);
            foreach (var target in Geometry.Hexes())
            {
                Assert.Equal(shortest[target], Geometry.Distance(source, target));
            }
        }
    }

    [Fact]
    public void CanonicalHexsideUsesSidesZeroToTwo()
    {
        var e4 = Geometry.IndexOf(HexName.Parse("E4"));
        var d3 = Geometry.IndexOf(HexName.Parse("D3"));
        Assert.Equal(new HexsideRef(d3, HexsideDirection.SouthEast), Geometry.Canonicalize(new HexsideRef(e4, HexsideDirection.NorthWest)));
        Assert.Equal(new HexsideRef(e4, HexsideDirection.North), Geometry.Canonicalize(new HexsideRef(e4, HexsideDirection.North)));

        // A board-edge hexside with no neighbor keeps its own ref.
        var a1 = new HexIndex(0, 0);
        Assert.Equal(new HexsideRef(a1, HexsideDirection.SouthWest), Geometry.Canonicalize(new HexsideRef(a1, HexsideDirection.SouthWest)));
    }

    [Fact]
    public void StandardGeometryForOtherSizesKeepsTheStandardHex()
    {
        var small = BoardGeometry.Standard(9, 9);
        Assert.Equal(450, small.GridWidth);
        Assert.Equal(581, small.GridHeight);
        Assert.Equal(56.25, small.HexWidth);
    }

    [Fact]
    public void BHalfBoardsAreNamedFromQ()
    {
        // 1b: 17 by 20 hexes, 56.3125 wide; VASL names column 0 Q, column 9 Z, column 10 AA, and column 16 GG.
        var b = BoardGeometry.Vasl(17, 20, 56.3125, 64.5, 901, 1290, columnLetterOffset: 16);
        Assert.Equal("Q1", b.NameOf(new HexIndex(0, 0)).ToString());
        Assert.Equal("R0", b.NameOf(new HexIndex(1, 0)).ToString());
        Assert.Equal("AA20", b.NameOf(new HexIndex(10, 19)).ToString());
        Assert.Equal("GG20", b.NameOf(new HexIndex(16, 19)).ToString());
        Assert.Equal(new HexIndex(0, 0), b.IndexOf(HexName.Parse("Q1")));
        Assert.False(b.TryGetIndex(HexName.Parse("A1"), out _));
        Assert.Equal(348, b.HexCount);

        // The x center of the last column is clamped to the grid, as in VASL.
        Assert.Equal(900, b.CenterDot(new HexIndex(16, 0)).X);
    }

    [Fact]
    public void LowerDoubleWidthBoardsNumberRowsFromEleven()
    {
        var lower = BoardGeometry.Vasl(33, 10, 56.25, 64.47, 1800, 645, rowNumberOffset: 10);
        Assert.Equal("A11", lower.NameOf(new HexIndex(0, 0)).ToString());
        Assert.Equal("B10", lower.NameOf(new HexIndex(1, 0)).ToString());
        Assert.Equal("GG20", lower.NameOf(new HexIndex(32, 9)).ToString());
        Assert.Equal(new HexIndex(1, 0), lower.IndexOf(HexName.Parse("B10")));
        Assert.Equal(32.235, lower.A1CenterY);
    }

    private static string Name(HexIndex? hex) => Geometry.NameOf(Assert.NotNull(hex)).ToString();

    private static Dictionary<HexIndex, int> ShortestPaths(HexIndex source)
    {
        var distances = new Dictionary<HexIndex, int> { [source] = 0 };
        var queue = new Queue<HexIndex>();
        queue.Enqueue(source);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var side in HexsideDirections.All)
            {
                if (Geometry.Neighbor(current, side) is { } next && !distances.ContainsKey(next))
                {
                    distances[next] = distances[current] + 1;
                    queue.Enqueue(next);
                }
            }
        }

        return distances;
    }
}
