using System.Reflection;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering.Palettes;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>
/// Option E (the Counter Layer Design, sections 7, 7.1, and 11): the counter layer's hexes taken from the fragment cache are the hexes drawn
/// again. Each state a counter can show, and each thing a renderer draws with, gives another key; the bound holds; warnings come back on a hit.
/// </summary>
public sealed class FragmentCacheTests
{
    private static readonly UnitMapTarget Target = UnitMapTarget.ForBoard(BoardRef.Parse("bd01"), BoardGeometry.StandardGeomorphic);

    private static UnitRenderer Digital => RenderingTestData.Renderer(UnitStyles.Digital);

    private static IReadOnlyList<UnitDocument> Demo() => UnitPlacementSetReader.Read(
        File.ReadAllBytes(Path.Combine(RenderingTestData.UnitsDirectory(), "examples", "bd01-demo.units.json")), RenderingTestData.Vocabulary.Value).Set!.Units;

    private static UnitOverlay Build(IReadOnlyList<UnitDocument> units, UnitRenderer renderer, UnitFragmentCache? cache = null) =>
        UnitOverlayBuilder.Build(Target, "test-set", units, renderer, cache);

    /// <summary>The overlay with the cache is the overlay without it: the layer, the placed units, and the warnings.</summary>
    private static void SameAsDrawnAgain(UnitOverlay cached, IReadOnlyList<UnitDocument> units, UnitRenderer renderer)
    {
        var drawn = Build(units, renderer);
        Assert.Equal(drawn.Svg, cached.Svg);
        Assert.Equal(drawn.Units.Select(unit => (unit.Document.Id, unit.Hex, unit.Shown, unit.Name)), cached.Units.Select(unit => (unit.Document.Id, unit.Hex, unit.Shown, unit.Name)));
        Assert.Equal(drawn.Warnings, cached.Warnings);
        Assert.Equal(drawn.FaceSize, cached.FaceSize);
    }

    [Fact]
    public void TheKeyWritesEveryPropertyOfADocument()
    {
        // UnitFragmentKey.Document writes each of these. A property added to a document must be added to the key, and to this list.
        Assert.Equal(["Attached", "Concealed", "Dummy", "Faces", "Facing", "Hexside", "Id", "Kind", "Location", "OutOfLos", "Side", "SizeClass", "StackOrder", "States",
            "TurretFacing", "Uninspected", "Unit", "Vocabulary"], Properties(typeof(UnitDocument)));
        Assert.Equal(["Name", "Traits", "Values"], Properties(typeof(UnitFace)));
        Assert.Equal(["Attribute", "Items", "Number", "Text"], Properties(typeof(UnitValue)));
    }

    // A record's positional and init properties have an init setter; computed ones, such as UnitValue.Display, have none and are not inputs.
    private static string[] Properties(Type type) => [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.SetMethod is not null).Select(property => property.Name).Order(StringComparer.Ordinal)];

    [Fact]
    public void ALayerBuiltAgainComesFromTheCacheAndIsTheSame()
    {
        var cache = new UnitFragmentCache();
        var units = Demo();
        SameAsDrawnAgain(Build(units, Digital, cache), units, Digital);
        var (hits, misses) = (cache.Hits, cache.Misses);
        Assert.Equal(0, hits);

        // Equal documents, made again, are the same key: every hex is a hit.
        var copies = units.Select(unit => unit with { }).ToArray();
        SameAsDrawnAgain(Build(copies, Digital, cache), copies, Digital);
        Assert.Equal(misses, cache.Misses);
        Assert.Equal(misses, cache.Hits);
    }

    public static TheoryData<string> Changes => ["facing", "turret facing", "hexside", "concealed", "uninspected", "out of LOS", "dummy", "state added", "state removed",
        "attached removed", "attached changed", "side", "stack order", "size class", "level", "hex", "face value", "trait", "unit value", "id", "vocabulary"];

    [Theory]
    [MemberData(nameof(Changes))]
    public void EachStateACounterCanShowIsDrawnAgain(string change)
    {
        var cache = new UnitFragmentCache();
        var units = Demo();
        Build(units, Digital, cache);
        var changed = units.Select(unit => unit.Id is "demo-a" or "demo-f" ? Changed(unit, change) : unit).ToArray();
        Assert.NotEqual(units, changed);
        var misses = cache.Misses;
        SameAsDrawnAgain(Build(changed, Digital, cache), changed, Digital);
        Assert.True(cache.Misses > misses, $"'{change}' gave no new key");
    }

    private static UnitDocument Changed(UnitDocument unit, string change) => change switch
    {
        "facing" => unit with { Facing = unit.Facing is UnitFacing.West ? UnitFacing.East : UnitFacing.West },
        "turret facing" => unit with { TurretFacing = UnitFacing.SouthWest },
        "hexside" => unit with { Hexside = UnitHexside.NorthEast },
        "concealed" => unit with { Concealed = !unit.Concealed },
        "uninspected" => unit with { Concealed = true, Uninspected = true },
        "out of LOS" => unit with { Concealed = true, Uninspected = true, OutOfLos = true },
        "dummy" => unit with { Concealed = true, Dummy = true },
        "state added" => unit with { States = [.. unit.States, "asl:broken"] },
        "state removed" => unit with { States = [] },
        "attached removed" => unit with { Attached = [] },
        "attached changed" => unit with { Attached = [.. unit.Attached.Select(item => item with { States = ["asl:broken"] })] },
        "side" => unit with { Side = unit.Side == "german" ? "russian" : "german" },
        "stack order" => unit with { StackOrder = unit.StackOrder - 5 },
        "size class" => unit with { SizeClass = 3 },
        "level" => unit with { Location = unit.Location!.Replace(":0", ":1", StringComparison.Ordinal) },
        "hex" => unit with { Location = "bd01:B9:0" },
        "face value" => unit with { Faces = [.. unit.Faces.Select(face => face with { Values = [.. face.Values.Select(Bumped)] })] },
        "trait" => unit with { Faces = [.. unit.Faces.Select(face => face with { Traits = [.. face.Traits, "asl:assault-fire"] })] },
        "unit value" => unit with { Unit = [.. unit.Unit.Select(Bumped)], Faces = unit.Unit.Count == 0 ? [.. unit.Faces.Select(face => face with { Values = [.. face.Values.Skip(1)] })] : unit.Faces },
        "id" => unit with { Id = unit.Id + "x", Attached = [.. unit.Attached.Select(item => item with { Id = item.Id + "x" })] },
        "vocabulary" => unit with { Vocabulary = [.. unit.Vocabulary, "extra@1.0.0"] },
        _ => throw new ArgumentOutOfRangeException(nameof(change)),
    };

    private static UnitValue Bumped(UnitValue value) => value.Number is { } number ? value with { Number = number + 1 } : value with { Text = value.Text + "x" };

    [Fact]
    public void EachRendererHasItsOwnFragments()
    {
        var cache = new UnitFragmentCache();
        var units = Demo();
        var classic = RenderingTestData.Renderer(UnitStyles.Classic);
        var sheet = UnitStyles.Load(UnitStyles.Digital);
        var noFigures = new UnitRenderer(RenderingTestData.Vocabulary.Value, sheet, PaletteSetReader.Embedded(sheet.Palette!)) { Figures = FigureArt.None };
        var otherPalette = new UnitRenderer(RenderingTestData.Vocabulary.Value, sheet, PaletteSetReader.Embedded("limbodancer"));
        foreach (var renderer in new[] { Digital, classic, noFigures, otherPalette, Digital })
        {
            SameAsDrawnAgain(Build(units, renderer, cache), units, renderer);
        }

        // The four renderers drew their own hexes; the second build with the first renderer drew none.
        var hexes = units.Select(unit => BoardLocation.Parse(unit.Location!).Hex).Distinct().Count();
        Assert.Equal(4 * hexes, cache.Misses);
        Assert.Equal(hexes, cache.Hits);
    }

    [Fact]
    public void WarningsComeBackOnAHit()
    {
        var renderer = RenderingTestData.Renderer(RenderingTestData.Sheet("""
            unit { face-template: "fp range"; }
            unit::slot(fp) { content: attr(leadership); }
            unit::slot(range) { content: "R" attr(range); }
            """));
        var units = new[] { RenderingTestData.Document("""{ "id": "l", "kind": "asl:leader", "location": "bd01:E4:0", "faces": { "front": { "morale": 8 } } }""") };
        var cache = new UnitFragmentCache();
        var first = Build(units, renderer, cache);
        var second = Build(units, renderer, cache);
        Assert.Equal(1, cache.Hits);
        // Two slots without their attribute, at each of the layer's three tiers.
        Assert.Equal(6, second.Warnings.Count);
        Assert.Equal(first.Warnings, second.Warnings);
        SameAsDrawnAgain(second, units, renderer);
    }

    [Fact]
    public void TheBoundHoldsAndTheOldestLeaveFirst()
    {
        var units = Demo();
        var one = Build(units, Digital).Svg.Length;
        var cache = new UnitFragmentCache(one * 2);
        for (var row = 1; row <= 10; row++)
        {
            var moved = units.Select(unit => unit with { Location = $"bd01:{BoardLocation.Parse(unit.Location!).Hex.ToString()[0]}{row}:0" }).ToArray();
            SameAsDrawnAgain(Build(moved, Digital, cache), moved, Digital);
            Assert.True(cache.Characters <= cache.Bound, $"{cache.Characters} characters held, bound {cache.Bound}");
        }

        // The last layers drawn are held, the first are not.
        var misses = cache.Misses;
        var last = units.Select(unit => unit with { Location = $"bd01:{BoardLocation.Parse(unit.Location!).Hex.ToString()[0]}10:0" }).ToArray();
        Build(last, Digital, cache);
        Assert.Equal(misses, cache.Misses);
        var first = units.Select(unit => unit with { Location = $"bd01:{BoardLocation.Parse(unit.Location!).Hex.ToString()[0]}1:0" }).ToArray();
        Build(first, Digital, cache);
        Assert.True(cache.Misses > misses);
    }

    [Fact]
    public void ManyCallersAtOnceGetTheLayersDrawnAgain()
    {
        var units = Demo();
        var layers = Enumerable.Range(1, 10).Select(row => units.Select(unit => unit with { Location = $"bd01:{BoardLocation.Parse(unit.Location!).Hex.ToString()[0]}{row}:0" })
            .ToArray()).ToArray();
        var expected = layers.Select(layer => Build(layer, Digital).Svg).ToArray();
        var cache = new UnitFragmentCache(expected.Sum(layer => (long)layer.Length) / 3);
        Parallel.For(0, 8, worker =>
        {
            for (var round = 0; round < 5; round++)
            {
                for (var step = 0; step < layers.Length; step++)
                {
                    var index = worker % 2 == 0 ? step : layers.Length - 1 - step;
                    Assert.Equal(expected[index], Build(layers[index], Digital, cache).Svg);
                }
            }
        });
        Assert.True(cache.Hits > 0);
        Assert.True(cache.Characters <= cache.Bound);
    }
}
