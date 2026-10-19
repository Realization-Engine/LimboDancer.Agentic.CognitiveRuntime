using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering;
using LimboDancer.Domains.Asl.Units.Rendering.Tests;
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

    /// <summary>
    /// Option E: with the fragment cache, every layer is the manifest's, whether the games are stepped forward, back, or by jumps, and with the
    /// cache's own bound or one so small that hexes leave it all the time.
    /// </summary>
    [Theory]
    [InlineData("forward", UnitFragmentCache.DefaultBound)]
    [InlineData("back", UnitFragmentCache.DefaultBound)]
    [InlineData("jumps", UnitFragmentCache.DefaultBound)]
    [InlineData("forward", 200_000L)]
    public void WithTheCacheEveryLayerMatchesTheManifest(string order, long bound)
    {
        var expected = CounterLayerCases.Manifest();
        var cache = new UnitFragmentCache(bound);
        // The orders that miss most (jumps, and a bound that holds a fraction of a layer) take one played game's steps, which keeps the test short.
        var steps = cases.Steps("guards-dl-01", Perspective.AdjudicatorName, UnitStyles.Digital);
        IEnumerable<CounterLayerCase> sequence = order switch
        {
            "forward" when bound == UnitFragmentCache.DefaultBound => cases.All,
            "forward" => steps,
            "back" => cases.All.Reverse(),

            // A fixed permutation: steps by a prime the step count does not divide, so every step comes once, far from its neighbours.
            _ => Enumerable.Range(0, steps.Count).Select(index => steps[(int)(index * 7919L % steps.Count)]),
        };
        var wrong = sequence.Where(item => CounterLayerCases.Hash(item.Build(cache).Svg) != expected[item.Name]).Select(item => item.Name).ToArray();
        Assert.True(wrong.Length == 0, $"{wrong.Length} layers differ from the manifest with the cache, the first: {string.Join(", ", wrong.Take(5))}");
        Assert.True(cache.Characters <= bound);

        // A Replay run forward redraws only what changed: most hexes of a played game's next step are hits.
        if (order == "forward" && bound == UnitFragmentCache.DefaultBound)
        {
            Assert.True(cache.Hits > 3 * cache.Misses, $"{cache.Hits} hits, {cache.Misses} misses");
        }
    }

    /// <summary>The placed units, their names, and the warnings are those of the layer drawn again: every tenth step of a played game, and the rest.</summary>
    [Fact]
    public void WithTheCacheThePlacedUnitsAndWarningsAreTheSame()
    {
        var cache = new UnitFragmentCache();
        var played = cases.Steps("guards-dl-01", Perspective.AdjudicatorName, UnitStyles.Digital);
        foreach (var item in played.Where((_, index) => index % 10 == 0).Concat(cases.All.Where(item => !CounterLayerCases.Played.Any(game =>
            item.Name.StartsWith(game, StringComparison.Ordinal)))))
        {
            var (cached, drawn) = (item.Build(cache), item.Build(null));
            Assert.Equal(drawn.Units.Select(unit => (unit.Document.Id, unit.Hex, unit.Shown, unit.Name)), cached.Units.Select(unit => (unit.Document.Id, unit.Hex, unit.Shown, unit.Name)));
            Assert.Equal(drawn.Warnings, cached.Warnings);
            Assert.Equal(drawn.Diagnostics, cached.Diagnostics);
        }
    }

    /// <summary>
    /// The equivalence oracle over the games (the Counter Layer Design, section 11): every fourth layer of the manifest, expanded, is the layer
    /// with every counter drawn whole. The shared definitions change how the layer is written, never what it draws.
    /// </summary>
    [Fact]
    public void TheSharedLayerDrawsWhatTheWholeCountersDraw()
    {
        var wrong = cases.All.Where((_, index) => index % 4 == 0)
            .Where(item => LayerOracle.Expanded(item.Build(null)) != LayerOracle.Expanded(item.Whole())).Select(item => item.Name).ToArray();
        Assert.True(wrong.Length == 0, $"{wrong.Length} layers draw otherwise than their whole counters, the first: {string.Join(", ", wrong.Take(5))}");
    }

    /// <summary>The Studio's one cache, used by several circuits at once: two games stepped on four threads, each layer the manifest's.</summary>
    [Fact]
    public void ManyCallersAtOnceGetTheManifestsLayers()
    {
        var expected = CounterLayerCases.Manifest();
        var cache = new UnitFragmentCache(1_000_000);
        var runs = new[]
        {
            cases.Steps("guards-dl-01", Perspective.AdjudicatorName, UnitStyles.Digital),
            [.. cases.Steps("guards-dl-01", Perspective.AdjudicatorName, UnitStyles.Digital).Reverse()],
            cases.Steps("p31c-tw", Perspective.AdjudicatorName, UnitStyles.Digital),
            [.. cases.Steps("p31c-tw", Perspective.AdjudicatorName, UnitStyles.Digital).Reverse()],
        };
        var wrong = new System.Collections.Concurrent.ConcurrentBag<string>();
        Parallel.ForEach(runs, run =>
        {
            foreach (var item in run.Where(item => CounterLayerCases.Hash(item.Build(cache).Svg) != expected[item.Name]))
            {
                wrong.Add(item.Name);
            }
        });
        Assert.Empty(wrong);
        Assert.True(cache.Characters <= cache.Bound);
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

    public UnitLibrary Library
    {
        get;
    }

    public GameLibrary Games
    {
        get;
    }

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

    private List<CounterLayerCase> Make()
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

        var crowded = LayerFixtures.Crowded(Library.Examples);
        cases.AddRange(Sheets.Select(sheet => Case($"crowded/{sheet}", "crowded", crowded, sheet)));
        return cases;
    }

    private CounterLayerCase Case(string name, string setId, IReadOnlyList<UnitDocument> units, string sheet)
    {
        var renderer = Library.Renderer(sheet)!;
        return new CounterLayerCase(name, cache => UnitOverlayBuilder.Build(Bd01, setId, units, renderer, cache),
            () => UnitOverlayBuilder.Build(Bd01, setId, units, renderer, null, shared: false));
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

/// <summary>
/// A manifest case: its name, the overlay it builds (with a fragment cache or without one), and the same layer with every counter drawn whole,
/// as a standalone counter is, for the equivalence oracle.
/// </summary>
public sealed record CounterLayerCase(string Name, Func<UnitFragmentCache?, UnitOverlay> Build, Func<UnitOverlay> Whole);
