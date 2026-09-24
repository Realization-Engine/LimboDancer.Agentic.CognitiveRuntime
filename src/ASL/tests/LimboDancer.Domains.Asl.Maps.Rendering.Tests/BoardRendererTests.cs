using System.Globalization;
using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Outlines;
using LimboDancer.Domains.Asl.Maps.Vasl;

namespace LimboDancer.Domains.Asl.Maps.Rendering.Tests;

public sealed class VaslFactAttribute : FactAttribute
{
    public VaslFactAttribute()
    {
        if (VaslSource.FromEnvironment() is null)
        {
            Skip = $"Set {VaslSource.EnvironmentVariable} to a local VASL checkout to run this test.";
        }
    }
}

public sealed class BoardRendererTests
{
    private const string UpdateGoldenVariable = "ASL_MAPS_UPDATE_GOLDEN";
    private static readonly XNamespace Svg = BoardRenderer.SvgNamespace;
    private static readonly Lazy<BoardRenderInput> Synthetic = new(SyntheticBoard.Input);

    public static TheoryData<BoardView> Views => [BoardView.Exact, BoardView.HexFacts];

    [Theory]
    [MemberData(nameof(Views))]
    public void RenderingIsDeterministic(BoardView view)
    {
        var first = BoardRenderer.Document(Synthetic.Value, view);
        var second = BoardRenderer.Document(SyntheticBoard.Input(), view);
        Assert.Equal(first, second);
    }

    [Theory]
    [MemberData(nameof(Views))]
    public void DocumentsAreWellFormedWithUniqueIdsAndNoImages(BoardView view)
    {
        foreach (var trace in new[] { false, true })
        {
            var document = XDocument.Parse(BoardRenderer.Document(Synthetic.Value, view, trace));
            Assert.Equal(Svg + "svg", document.Root!.Name);
            Assert.Equal(BoardRenderer.ViewBox(Synthetic.Value, view), document.Root.Attribute("viewBox")!.Value);
            Assert.Empty(document.Descendants(Svg + "image"));
            Assert.DoesNotContain(document.Descendants().Attributes(), attribute => attribute.Name.LocalName == "href" && !attribute.Value.StartsWith('#'));
            var ids = document.Descendants().Select(element => (string?)element.Attribute("id")).OfType<string>().ToArray();
            Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(
                BoardRenderer.Layers(view).Select(layer => "layer-" + layer),
                document.Root.Elements(Svg + "g").Select(group => (string)group.Attribute("id")!));
        }
    }

    [Theory]
    [MemberData(nameof(Views))]
    public void FragmentsWrapOneLayerEach(BoardView view)
    {
        foreach (var layer in BoardRenderer.Layers(view))
        {
            var fragment = XDocument.Parse(BoardRenderer.Fragment(Synthetic.Value, view, layer));
            var group = Assert.Single(fragment.Root!.Elements());
            Assert.Equal("layer-" + layer, (string)group.Attribute("id")!);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => BoardRenderer.Fragment(Synthetic.Value, view, "no-such-layer"));
    }

    [Fact]
    public void ViewNamesRoundTrip()
    {
        foreach (var view in new[] { BoardView.Exact, BoardView.HexFacts })
        {
            Assert.True(BoardRenderer.TryParseView(BoardRenderer.ViewName(view), out var parsed));
            Assert.Equal(view, parsed);
        }

        Assert.False(BoardRenderer.TryParseView("styled", out _));
        Assert.False(BoardRenderer.TryParseView(null, out _));
    }

    [Fact]
    public void ExactTerrainRefillsToTheGrid() => AssertExactRefill(Synthetic.Value);

    [Fact]
    public void HexFactsViewHasOneGroupPerHex()
    {
        var document = XDocument.Parse(BoardRenderer.Fragment(Synthetic.Value, BoardView.HexFacts, "hexfacts"));
        var groups = document.Descendants(Svg + "g").Where(group => ((string?)group.Attribute("id"))?.StartsWith("h-", StringComparison.Ordinal) == true);
        Assert.Equal(SyntheticBoard.Geometry.HexCount, groups.Count());
    }

    [Theory]
    [MemberData(nameof(Views))]
    public void SyntheticBoardMatchesGolden(BoardView view)
    {
        var actual = BoardRenderer.Document(Synthetic.Value, view);
        var path = Path.Combine(GoldenDirectory(), $"synthetic-{BoardRenderer.ViewName(view)}.svg");
        if (Environment.GetEnvironmentVariable(UpdateGoldenVariable) == "1")
        {
            File.WriteAllText(path, actual);
        }

        Assert.True(File.Exists(path), $"Missing {path}; set {UpdateGoldenVariable}=1 to create it.");
        Assert.Equal(File.ReadAllText(path).ReplaceLineEndings("\n"), actual.ReplaceLineEndings("\n"));
    }

    [VaslFact]
    public void Board01ExactTerrainRefillsToTheGrid()
    {
        var vasl = VaslSource.FromEnvironment()!;
        var catalog = vasl.ReadTerrainCatalog().Catalog!;
        var board = VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, "01"), catalog).Board!;
        var facts = HexFactFidelity.Derive(board, catalog);
        AssertExactRefill(BoardRenderInput.Create(board.Board, "bd01", board.Grid, catalog, facts));
    }

    private static void AssertExactRefill(BoardRenderInput input)
    {
        var grid = input.Grid;
        var document = XDocument.Parse(BoardRenderer.Fragment(input, BoardView.Exact, "exact-terrain"));
        var owner = new bool[grid.CellCount];
        foreach (var path in document.Descendants(Svg + "path"))
        {
            var code = byte.Parse((string)path.Attribute("data-code")!, CultureInfo.InvariantCulture);
            var elevation = sbyte.Parse((string)path.Attribute("data-elev")!, CultureInfo.InvariantCulture);
            Assert.Equal($"t{code}e{elevation}", (string)path.Attribute("id")!);
            Assert.Equal("evenodd", (string)path.Attribute("fill-rule")!);
            var coverage = CellCoverage.Fill(ParseRings((string)path.Attribute("d")!), grid.Geometry.GridWidth, grid.Geometry.GridHeight);
            for (var cell = 0; cell < coverage.Length; cell++)
            {
                if (coverage[cell])
                {
                    Assert.False(owner[cell], $"Cell {cell} is painted twice.");
                    owner[cell] = true;
                    Assert.Equal(code, grid.Codes[cell]);
                    Assert.Equal(elevation, grid.Elevations[cell]);
                }
            }
        }

        Assert.DoesNotContain(false, owner);
    }

    private static List<IReadOnlyList<GridPoint>> ParseRings(string data)
    {
        var rings = new List<IReadOnlyList<GridPoint>>();
        List<GridPoint>? ring = null;
        var index = 0;
        while (index < data.Length)
        {
            var command = data[index++];
            switch (command)
            {
                case 'M':
                    ring = [new GridPoint(ReadNumber(data, ref index), ReadNumber(data, ref index))];
                    break;
                case 'H':
                    ring!.Add(ring[^1] with
                    {
                        X = ReadNumber(data, ref index)
                    });
                    break;
                case 'V':
                    ring!.Add(ring[^1] with
                    {
                        Y = ReadNumber(data, ref index)
                    });
                    break;
                case 'Z':
                    rings.Add(ring!);
                    ring = null;
                    break;
                default:
                    Assert.Fail($"Unexpected path command '{command}'.");
                    break;
            }
        }

        Assert.Null(ring);
        return rings;
    }

    private static int ReadNumber(string data, ref int index)
    {
        if (data[index] == ' ')
        {
            index++;
        }

        var start = index;
        while (index < data.Length && (data[index] == '-' || char.IsAsciiDigit(data[index])))
        {
            index++;
        }

        return int.Parse(data.AsSpan(start, index - start), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The committed golden directory, found from the test output by the solution file. <c>CallerFilePath</c> is not
    /// used because continuous-integration builds map source paths.
    /// </summary>
    private static string GoldenDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "ASL", "LimboDancer.Domains.Asl.sln")))
            {
                return Path.Combine(directory.FullName, "src", "ASL", "tests", "LimboDancer.Domains.Asl.Maps.Rendering.Tests", "Golden");
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from " + AppContext.BaseDirectory);
    }
}
