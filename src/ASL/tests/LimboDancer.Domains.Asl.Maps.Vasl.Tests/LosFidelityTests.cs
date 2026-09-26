using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using Xunit.Abstractions;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

/// <summary>
/// U14 (LOS Design, section 7): the C# LOS read against VASL's own <c>Map.LOS</c>, pair by pair, on the LOS oracle
/// fixtures. Every pair the read answers (Clear or Blocked) must agree with VASL on whether LOS is blocked, the hex
/// where it is blocked, the range, the hindrance total, and the reason text; the number answered is pinned so it can
/// only rise. The boards are ingested from the VASL checkout, as the F2 tests do.
/// </summary>
public sealed class LosFidelityTests(ITestOutputHelper output)
{
    private static readonly string OracleDirectory = Path.Combine(AppContext.BaseDirectory, "Oracle");

    private static readonly ConcurrentDictionary<string, Comparison> Comparisons = new(StringComparer.Ordinal);

    /// <summary>Each fixture with the number of pairs the read answers, pinned at the value reached.</summary>
    public static TheoryData<string, int> Fixtures => new()
    {
        { "bd01.los.json.gz", 10701 },
        { "bd11.los.json.gz", 5598 },
        { Path.Combine("Scenarios", "bd11-over-bd01.scenario.los.json.gz"), 19028 },
        { Path.Combine("Scenarios", "bd11r-over-bd01.scenario.los.json.gz"), 19018 },
    };

    [VaslTheory]
    [MemberData(nameof(Fixtures))]
    public void EveryAnsweredPairAgreesWithVasl(string file, int pinnedAnswered)
    {
        var comparison = Compare(file);
        output.WriteLine($"{file}: {comparison.Pairs} pairs, {comparison.Answered} answered, {comparison.Agreed} agreed");
        foreach (var (rule, count) in comparison.Unsupported.OrderByDescending(item => item.Value))
        {
            output.WriteLine($"  unsupported {count,6} {rule}");
        }

        Assert.True(comparison.Disagreements.Count == 0,
            $"{comparison.Disagreements.Count} answered pairs disagree with VASL:{Environment.NewLine}{string.Join(Environment.NewLine, comparison.Disagreements.Take(20))}");
        Assert.True(comparison.Answered >= pinnedAnswered, $"{file}: {comparison.Answered} answered, below the pinned {pinnedAnswered}.");
    }

    [VaslTheory]
    [MemberData(nameof(Fixtures))]
    public void AnUnsupportedPairNamesItsRule(string file, int pinnedAnswered)
    {
        _ = pinnedAnswered;
        var comparison = Compare(file);
        var names = typeof(LosUnsupportedRule).GetFields().Select(field => (string)field.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(LosUnsupportedRule.Cellar, comparison.Unsupported.Keys);
        Assert.All(comparison.Unsupported.Keys, rule => Assert.Contains(rule, names));
        Assert.Equal(comparison.Pairs, comparison.Answered + comparison.Unsupported.Values.Sum());
    }

    [VaslFact]
    public void ABoardThatIsNotVerifiedGivesNondefinitiveAnswers()
    {
        var (vasl, catalog, rules) = Vasl();
        var map = Assert.IsType<LosMap>(LosMap.ForBoard(Handle(vasl, catalog, rules, "11", BoardReadStatus.Ingested)).Map);
        Assert.False(map.IsDefinitive);
        using var fixture = Load(Path.Combine(OracleDirectory, "bd11.los.json.gz"));
        var answered = 0;
        var blocked = 0;
        foreach (var pair in fixture.RootElement.GetProperty("pairs").EnumerateArray().Take(500))
        {
            var result = LosCalculator.Check(map, Location(pair, "source"), Location(pair, "target"));
            if (result.Status == LosStatus.Unsupported)
            {
                continue;
            }

            Assert.Equal(LosStatus.Nondefinitive, result.Status);
            Assert.Equal(pair.GetProperty("blocked").GetBoolean(), result.IsBlocked);
            answered++;
            blocked += result.IsBlocked == true ? 1 : 0;
        }

        Assert.True(answered > 400 && blocked > 100, $"{answered} answered, {blocked} blocked.");
    }

    [VaslFact]
    public void APlacedMapReadFromHandlesAnswersAsTheBuiltMap()
    {
        var (vasl, catalog, rules) = Vasl();
        var scenario = LosScenario("bd11r-over-bd01");
        var placed = scenario.Placements.Select(placement => (placement, Handle(vasl, catalog, rules, placement.Board.VaslBoardName, BoardReadStatus.Verified))).ToArray();
        var read = Assert.IsType<ComposedMapRead>(ComposedMapRead.Create(placed).Read);
        var map = Assert.IsType<LosMap>(LosMap.ForPlacedMap(read).Map);
        Assert.Same(map, LosMap.ForPlacedMap(read).Map);
        Assert.True(map.IsDefinitive);

        var built = BuildMap(scenario.Name, null);
        using var fixture = Load(Path.Combine(OracleDirectory, "Scenarios", scenario.Name + ".scenario.los.json.gz"));
        foreach (var pair in fixture.RootElement.GetProperty("pairs").EnumerateArray().Take(3000))
        {
            var source = Location(pair, "source");
            var target = Location(pair, "target");
            Assert.Equal(LosCalculator.Check(built, source, target), LosCalculator.Check(map, source, target));
        }

        // A shared seam hex is named by the board that owns it; the other name is refused.
        var other = read.Layout.Boards
            .SelectMany(board => board.Geometry.Hexes().Select(hex => (board.Placement.Board, Hex: board.Geometry.NameOf(hex))))
            .First(name => !read.Layout.IsOwnerName(name.Board, name.Hex));
        Assert.Throws<ArgumentException>(() => LosCalculator.Check(map, new BoardLocation(other.Board, other.Hex, 0), BoardLocation.Parse("bd01:E4:0")));
    }

    private static Comparison Compare(string file) => Comparisons.GetOrAdd(file, name =>
    {
        using var fixture = Load(Path.Combine(OracleDirectory, name));
        var root = fixture.RootElement;
        var map = BuildMap(root.GetProperty("scenario").GetString(), root.GetProperty("boards")[0].GetProperty("board").GetString());
        var comparison = new Comparison();
        foreach (var pair in root.GetProperty("pairs").EnumerateArray())
        {
            comparison.Pairs++;
            var source = Location(pair, "source");
            var target = Location(pair, "target");
            var result = LosCalculator.Check(map, source, target);
            if (!result.IsAnswered)
            {
                comparison.Unsupported[result.Reason] = comparison.Unsupported.GetValueOrDefault(result.Reason) + 1;
                continue;
            }

            comparison.Answered++;
            var expected = (Blocked: pair.GetProperty("blocked").GetBoolean(), Hex: pair.GetProperty("blockedHex").GetString(),
                Range: pair.GetProperty("range").GetInt32(), Hindrance: pair.GetProperty("hindrance").GetInt32(), Reason: pair.GetProperty("reason").GetString());
            var actual = (Blocked: result.IsBlocked == true, Hex: result.BlockedAt is { Board: { } board, Hex: { } hex } ? $"{board}:{hex}" : null,
                result.Range, result.Hindrance, result.Reason);
            if (expected == actual)
            {
                comparison.Agreed++;
            }
            else
            {
                comparison.Disagreements.Add($"{source} -> {target}: VASL {expected}, read {actual}");
            }
        }

        return comparison;
    });

    // A single board is read through its handle; a scenario is built as VaslMapTests builds it.
    private static LosMap BuildMap(string? scenarioName, string? boardName)
    {
        var (vasl, catalog, rules) = Vasl();
        if (scenarioName is null)
        {
            return Assert.IsType<LosMap>(LosMap.ForBoard(Handle(vasl, catalog, rules, boardName![2..], BoardReadStatus.Verified)).Map);
        }

        var import = VaslMapImporter.Build(vasl, catalog, rules, LosScenario(scenarioName).Placements);
        return LosMap.ForVaslMap(Assert.IsType<VaslMap>(import.Map), catalog);
    }

    private static (VaslSource Vasl, TerrainCatalog Catalog, LosSsRuleSet Rules) Vasl()
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var shared = vasl.ReadTerrainCatalog();
        return (vasl, Assert.IsType<TerrainCatalog>(shared.Catalog), shared.Rules);
    }

    private static BoardHandle Handle(VaslSource vasl, TerrainCatalog catalog, LosSsRuleSet rules, string vaslBoardName, BoardReadStatus status)
    {
        var board = Assert.IsType<IngestedBoard>(VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, vaslBoardName), catalog).Board);
        return new BoardHandle(board.Board, board.Provenance.LosData.ContentBlob, status, "VASL checkout", HexFactFidelity.Derive(board, catalog))
        {
            Los = new LosData(board.Grid, catalog, HexFactFidelity.Annotations(board.Metadata), rules),
        };
    }

    private static VaslScenario LosScenario(string name) =>
        VaslScenario.ParseFile(File.ReadAllText(Path.Combine(OracleDirectory, "Scenarios", "los-scenarios.txt"))).Single(scenario => scenario.Name == name);

    private static BoardLocation Location(JsonElement pair, string property) => BoardLocation.Parse(pair.GetProperty(property).GetString()!);

    private static JsonDocument Load(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        return JsonDocument.Parse(gzip);
    }

    private sealed class Comparison
    {
        public int Pairs
        {
            get; set;
        }

        public int Answered
        {
            get; set;
        }

        public int Agreed
        {
            get; set;
        }

        public Dictionary<string, int> Unsupported { get; } = new(StringComparer.Ordinal);

        public List<string> Disagreements { get; } = [];
    }
}

/// <summary>A theory that runs only when <c>AslMaps__VaslRoot</c> points to a local VASL checkout (ASL-MAP-073).</summary>
public sealed class VaslTheoryAttribute : TheoryAttribute
{
    public VaslTheoryAttribute()
    {
        if (VaslSource.FromEnvironment() is null)
        {
            Skip = $"Set {VaslSource.EnvironmentVariable} to a local VASL checkout to run this test.";
        }
    }
}
