using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The counter layers the tests hold, each as the layer's SHA-256 (the Counter Layer Design, sections 7.1 and 11): the two games played through
/// the Play page at every step's end in every view, the synthetic game at every revision in every view, the example sets, and a crowded fixture,
/// in both sheets. The manifest was recorded by the builder before option E; the hex fragments and the fragment cache must reproduce it exactly.
/// Set <c>ASL_LAYER_MANIFEST_UPDATE=1</c> to write it again, and explain the change in its commit.
/// </summary>
public sealed class CounterLayerManifestTests(CounterLayerCases cases) : IClassFixture<CounterLayerCases>
{
    private const string UpdateVariable = "ASL_LAYER_MANIFEST_UPDATE";

    [Fact]
    public void EveryLayerMatchesTheManifest()
    {
        var actual = cases.All.Select(item => (item.Name, Hash: CounterLayerCases.Hash(item.Build(null).Svg))).ToArray();
        var path = CounterLayerCases.ManifestPath();
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            File.WriteAllText(path, string.Concat(actual.Select(item => $"{item.Name} {item.Hash}\n")));
        }

        Assert.True(File.Exists(path), $"Missing {path}; set {UpdateVariable}=1 to create it.");
        var expected = CounterLayerCases.Manifest();
        Assert.Equal(expected.Count, actual.Length);
        var wrong = actual.Where(item => !expected.TryGetValue(item.Name, out var hash) || hash != item.Hash).Select(item => item.Name).ToArray();
        Assert.True(wrong.Length == 0, $"{wrong.Length} layers differ from the manifest, the first: {string.Join(", ", wrong.Take(5))}");
    }
}

/// <summary>The cases of the counter layer manifest, made once for the test class: each a name and the overlay it builds.</summary>
public sealed class CounterLayerCases : IDisposable
{
    private static readonly string[] Views = [Perspective.AdjudicatorName, "german", "russian"];
    private static readonly UnitMapTarget Bd01 = UnitMapTarget.ForBoard(BoardRef.Parse("bd01"), BoardGeometry.StandardGeomorphic);
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-layers-" + Guid.NewGuid().ToString("N"));
    private readonly Lazy<IReadOnlyList<CounterLayerCase>> all;

    public CounterLayerCases()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var maps = new MapService(options, new FakeVaslMapSource());
        Library = new UnitLibrary(options);
        var boards = new SyntheticBoards(maps);
        Games = new GameLibrary(Library, boards, new LivePlay(Library, boards));

        // The played games are read as the live games they are; the test boards have no bd01, so positions are not checked, and they replay all the same.
        var folder = Path.Combine(Library.UnitsRoot, "live", LivePlay.Tenant.ToString("N"));
        Directory.CreateDirectory(folder);
        foreach (var played in Played)
        {
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", played + ".game.json"), Path.Combine(folder, played + ".game.json"));
        }

        all = new Lazy<IReadOnlyList<CounterLayerCase>>(Make);
    }

    public static IReadOnlyList<string> Played { get; } = ["guards-dl-01", "p31c-tw"];

    public static IReadOnlyList<string> Sheets { get; } = [UnitStyles.Digital, UnitStyles.Classic];

    public UnitLibrary Library { get; }

    public GameLibrary Games { get; }

    public IReadOnlyList<CounterLayerCase> All => all.Value;

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    public static string Hash(string svg) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(svg)));

    public static string ManifestPath() => Path.Combine(RepositoryRoot(), "src", "ASL", "tests", "LimboDancer.Domains.Asl.MapStudio.Tests", "CounterLayers",
        "layer-manifest.txt");

    public static IReadOnlyDictionary<string, string> Manifest() => File.ReadAllLines(ManifestPath()).Where(line => line.Length > 0)
        .Select(line => line.Split(' ')).ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);

    /// <summary>The revisions where a step of a game's replay ends: the last revision of each attempt.</summary>
    public static IReadOnlyList<long> StepEnds(GameHistory history) => [.. history.Events.GroupBy(item => ReplaySteps.AttemptOf(item.EventId))
        .Select(group => group.Max(item => item.Revision)).Order()];

    /// <summary>One played game in one view and sheet, at every step's end, in order: the sequence a Replay run forward builds.</summary>
    public IReadOnlyList<CounterLayerCase> Steps(string played, string view, string sheet) => [.. All.Where(item =>
        item.Name.StartsWith($"{played}/{view}/{sheet}/", StringComparison.Ordinal))];

    private IReadOnlyList<CounterLayerCase> Make()
    {
        var cases = new List<CounterLayerCase>();
        foreach (var played in Played)
        {
            var entry = Games.Load(GameLibrary.LivePrefix + played);
            Assert.False(entry.History!.HasErrors);
            var ends = StepEnds(entry.History);
            // Every step's end in the full view and the Studio's default sheet, the sequence a Replay run forward builds; every fourth in the
            // other sheet and in each side's view, which keeps the test near a minute.
            foreach (var view in Views)
            {
                foreach (var sheet in Sheets)
                {
                    var every = view == Perspective.AdjudicatorName && sheet == UnitStyles.Digital ? 1 : 4;
                    foreach (var revision in ends.Where((_, index) => index % every == 0))
                    {
                        var set = Games.Projection(entry, new Perspective(view), revision).Set;
                        cases.Add(Case($"{played}/{view}/{sheet}/r{revision.ToString(CultureInfo.InvariantCulture)}", set.SetId, set.Units, sheet));
                    }
                }
            }
        }

        var village = Games.Load("a1-village.synthetic");
        foreach (var view in Views)
        {
            for (var revision = 1L; revision <= village.History!.States.Count; revision++)
            {
                var set = Games.Projection(village, new Perspective(view), revision).Set;
                cases.AddRange(Sheets.Select(sheet => Case($"a1-village/{view}/{sheet}/r{revision.ToString(CultureInfo.InvariantCulture)}", set.SetId, set.Units, sheet)));
            }
        }

        foreach (var entry in Library.Sets().Where(entry => entry.BuiltIn))
        {
            cases.AddRange(Sheets.Select(sheet => Case($"set-{entry.Set.SetId}/{sheet}", entry.Set.SetId, entry.Set.Units, sheet)));
        }

        var crowded = Crowded(Library.Examples);
        cases.AddRange(Sheets.Select(sheet => Case($"crowded/{sheet}", "crowded", crowded, sheet)));
        return cases;
    }

    private CounterLayerCase Case(string name, string setId, IReadOnlyList<UnitDocument> units, string sheet)
    {
        var renderer = Library.Renderer(sheet)!;
        return new CounterLayerCase(name, cache => UnitOverlayBuilder.Build(Bd01, setId, units, renderer));
    }

    /// <summary>
    /// Every example document on bd01: eight in E4 (a count badge), two upstairs and one in the cellar there, the rest one to a hex with a facing
    /// each; and a concealed counter, an uninspected one, a Dummy, and a draft beside them.
    /// </summary>
    internal static IReadOnlyList<UnitDocument> Crowded(IReadOnlyList<UnitDocument> examples)
    {
        var units = new List<UnitDocument>();
        for (var index = 0; index < examples.Count; index++)
        {
            var location = index switch
            {
                < 8 => "bd01:E4:0",
                < 10 => "bd01:E4:1",
                10 => "bd01:E4:-1",
                _ => $"bd01:{(char)('A' + ((index - 11) % 10))}{((index - 11) / 10) + 6}:0",
            };
            units.Add(examples[index] with
            {
                Id = $"c{index.ToString(CultureInfo.InvariantCulture)}-{examples[index].Id}",
                Location = location,
                Facing = index > 10 ? (UnitFacing)(index % 6) : examples[index].Facing,
                Attached = [.. examples[index].Attached.Select(attached => attached with { Location = location })],
            });
        }

        var squad = examples.First(example => example.Kind == "asl:squad");
        units.Add(squad with { Id = "concealed-1", Location = "bd01:H2:0", Concealed = true });
        units.Add(squad with { Id = "uninspected-1", Location = "bd01:H2:0", Concealed = true, Uninspected = true });
        units.Add(squad with { Id = "dummy-1", Location = "bd01:I2:0", Concealed = true, Dummy = true });
        units.Add(squad with { Id = "draft:german-1", Location = "bd01:J2:0" });
        return units;
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "ASL", "LimboDancer.Domains.Asl.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from " + AppContext.BaseDirectory);
    }
}

/// <summary>A manifest case: its name, and the overlay it builds, with a fragment cache or without one.</summary>
public sealed record CounterLayerCase(string Name, Func<object?, UnitOverlay> Build);
