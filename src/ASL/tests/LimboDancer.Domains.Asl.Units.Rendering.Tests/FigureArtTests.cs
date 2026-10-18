using System.Xml.Linq;
using LimboDancer.Domains.Asl.Units.Rendering.Styles;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>Personnel figures composed from figure parts (ASL Unit Figure Art Plan, section 3; decisions F8 and F9).</summary>
public sealed class FigureArtTests
{
    private const string Fixture = """
        {
          "poses": {
            "standing": { "path": "M0 0L100 0L100 1000Z", "width": 400, "head": { "x": 200, "y": 100, "radius": 80 },
                          "grip": { "x1": 150, "y1": 450, "x2": 300, "y2": 450 }, "weapon": "rifle", "accepted": true, "source": "test" },
            "prisoner": { "path": "M7 7L300 7L300 1000Z", "width": 300, "head": { "x": 150, "y": 100, "radius": 80 }, "accepted": true, "source": "test" },
            "broken": { "path": "M0 0L1500 0L1500 300Z", "width": 1500, "height": 300, "head": { "x": 1400, "y": 800, "radius": 80 },
                        "accepted": false, "source": "test" }
          },
          "helmets": { "german": { "path": "M-100 0L100 0L0 -120Z", "accepted": true, "source": "test" } },
          "weapons": { "german": { "rifle": { "path": "M-200 -10L400 -10L400 10L-200 10Z", "accepted": true, "source": "test" } } }
        }
        """;

    [Fact]
    public void ASquadIsDrawnFromItsPoseWithItsSidesHelmetAndWeapon()
    {
        var figures = Figures(FigureArt.Read(Fixture), "example-squad");
        Assert.Equal(3, figures.Count);
        var first = figures[0];
        var paths = first.Elements().Where(element => element.Name.LocalName == "path").ToList();
        Assert.Equal("M0 0L100 0L100 1000Z", paths[0].Attribute("d")!.Value);
        Assert.Equal("translate(150 450) rotate(0)", paths.Single(path => path.Attribute("d")!.Value.StartsWith("M-200", StringComparison.Ordinal)).Attribute("transform")!.Value);
        Assert.Equal("translate(200 100) scale(0.8)", paths.Single(path => path.Attribute("data-helmet")?.Value == "german").Attribute("transform")!.Value);
        Assert.NotNull(first.Attribute("fill"));
        Assert.StartsWith("translate(", first.Attribute("transform")!.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void TheHelmetIsRingedInTheFacesColorAndTheSkullDrawnAgainSoTheFaceMeetsIt()
    {
        var first = Figures(FigureArt.Read(Fixture), "example-squad")[0];
        var elements = first.Elements().ToList();
        var ring = elements.FindIndex(element => element.Attribute("data-helmet-ring") is not null);
        var skull = elements.FindIndex(element => element.Name.LocalName == "circle");
        var helmet = elements.FindIndex(element => element.Attribute("data-helmet") is not null);
        Assert.True(ring >= 0 && ring < skull && skull < helmet);
        Assert.Equal(elements[ring].Attribute("stroke")!.Value, Face(FigureArt.Read(Fixture), "example-squad"));
    }

    [Fact]
    public void TheSquadStandsInAStaggeredFileTheManBehindHigherAndRingedApart()
    {
        var figures = Figures(FigureArt.Read(Fixture), "example-squad");
        var places = figures.Select(group => Translate(group.Attribute("transform")!.Value)).ToList();
        Assert.True(places[0].X < places[1].X && places[1].X < places[2].X);
        Assert.True(places[0].Y < places[1].Y && places[1].Y < places[2].Y);
        Assert.Null(figures[0].Elements().FirstOrDefault(element => element.Attribute("data-figure-ring") is not null));
        Assert.All(figures.Skip(1), group => Assert.NotNull(group.Elements().FirstOrDefault(element => element.Attribute("data-figure-ring") is not null)));
    }

    [Fact]
    public void AStandInIsDrawnOnlyWhenAskedFor()
    {
        var art = FigureArt.Read(Fixture);
        Assert.Null(art.Pose("broken"));
        Assert.NotNull(art.WithStandIns().Pose("broken"));
        Assert.Empty(Figures(art, "example-squad", face: "broken"));
        Assert.NotEmpty(Figures(art.WithStandIns(), "example-squad", face: "broken"));
    }

    [Fact]
    public void AnUnarmedPrisonerRaisesItsHandsAndHoldsNoWeapon()
    {
        var art = FigureArt.Read(Fixture);
        var squad = RenderingTestData.Catalog.Value["example-squad"];
        var captured = squad with
        {
            States = [.. squad.States, "asl:captured"]
        };
        var renderer = new UnitRenderer(RenderingTestData.Vocabulary.Value, RenderingTestData.Renderer(UnitStyles.Digital).Sheet,
            RenderingTestData.Renderer(UnitStyles.Digital).Palette)
        {
            Figures = art
        };
        var svg = RenderingTestData.Render(renderer, captured, DetailTier.Far);
        var paths = svg.Descendants().Where(element => element.Name.LocalName == "path").Select(element => element.Attribute("d")!.Value).ToList();
        Assert.Contains("M7 7L300 7L300 1000Z", paths);
        Assert.DoesNotContain("M-200 -10L400 -10L400 10L-200 10Z", paths);
    }

    [Fact]
    public void ASideWithoutKitGetsTheBareFigure()
    {
        var art = FigureArt.Read(Fixture.Replace("\"german\"", "\"french\"", StringComparison.Ordinal));
        var paths = Figures(art, "example-squad")[0].Elements().Where(element => element.Name.LocalName == "path").ToList();
        Assert.Single(paths);
    }

    [Fact]
    public void WithoutAcceptedArtTheFiguresAreTheDrawnOnes()
    {
        Assert.Empty(Figures(FigureArt.BuiltIn, "example-squad"));
        Assert.Empty(Figures(FigureArt.None, "example-squad"));
    }

    [Fact]
    public void APathHoldingAnythingButPathDataIsRefused()
    {
        var bad = Fixture.Replace("M0 0L100 0L100 1000Z", "M0 0\\\"/><script/>", StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => FigureArt.Read(bad));
    }

    [Fact]
    public void TheEmbeddedPartsGoByKnownNames()
    {
        var art = FigureArt.BuiltIn.WithStandIns();
        Assert.All(art.PoseKeys, pose => Assert.Contains(pose, FigureArt.PoseNames));
        Assert.All(art.WeaponKeys, key => Assert.Contains(key.Weapon, FigureArt.WeaponNames));
    }

    /// <summary>figures.json is what code-builders/unit-figures/build_figures.py makes of the SVG sources: every source's paths, joined.</summary>
    [Fact]
    public void TheManifestMatchesItsSources()
    {
        var folder = Path.Combine(RenderingTestData.UnitsDirectory(), "figures");
        var manifest = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "figures.json")))!;
        static string Joined(string file) =>
            string.Join(" ", XDocument.Load(file).Descendants().Where(element => element.Name.LocalName == "path").Select(element => element.Attribute("d")!.Value.Trim()));
        var poses = Directory.GetFiles(Path.Combine(folder, "poses"), "*.svg");
        Assert.Equal(poses.Length, manifest["poses"]!.AsObject().Count);
        Assert.All(poses, file => Assert.Equal(Joined(file), (string?)manifest["poses"]![Path.GetFileNameWithoutExtension(file)]?["path"]));
        Assert.All(Directory.GetFiles(Path.Combine(folder, "kit"), "*.svg"), file =>
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var side = name[..name.LastIndexOf('-')];
            var part = name[(name.LastIndexOf('-') + 1)..];
            var entry = part == "helmet" ? manifest["helmets"]![side] : manifest["weapons"]![side]?[part];
            Assert.Equal(Joined(file), (string?)entry?["path"]);
        });
    }

    /// <summary>The figure groups of a unit's glyph slot at the near tier: one per figure, each translated and scaled.</summary>
    private static List<XElement> Figures(FigureArt art, string unit, string? face = null)
    {
        var renderer = new UnitRenderer(RenderingTestData.Vocabulary.Value, RenderingTestData.Renderer(UnitStyles.Digital).Sheet,
            RenderingTestData.Renderer(UnitStyles.Digital).Palette)
        {
            Figures = art
        };
        var svg = RenderingTestData.Render(renderer, RenderingTestData.Catalog.Value[unit], DetailTier.Near, face);
        // An attached SW has a glyph slot of its own, drawn before the unit's; only personnel draw figures, so every slot is searched.
        return svg.Descendants().Where(element => element.Attribute("data-slot")?.Value == "glyph").SelectMany(slot => slot.Descendants()).Where(element => element.Name.LocalName == "g" && element.Attribute("transform")?.Value.Contains("scale(", StringComparison.Ordinal) == true).ToList();
    }

    private static (double X, double Y) Translate(string transform)
    {
        var numbers = transform["translate(".Length..transform.IndexOf(')', StringComparison.Ordinal)].Split(' ');
        return (double.Parse(numbers[0], System.Globalization.CultureInfo.InvariantCulture), double.Parse(numbers[1], System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>The fill of a unit's face at the near tier.</summary>
    private static string Face(FigureArt art, string unit)
    {
        var renderer = new UnitRenderer(RenderingTestData.Vocabulary.Value, RenderingTestData.Renderer(UnitStyles.Digital).Sheet,
            RenderingTestData.Renderer(UnitStyles.Digital).Palette)
        {
            Figures = art
        };
        var svg = RenderingTestData.Render(renderer, RenderingTestData.Catalog.Value[unit], DetailTier.Near);
        return svg.Descendants().First(element => element.Attribute("data-face") is not null).Attribute("fill")!.Value;
    }
}
