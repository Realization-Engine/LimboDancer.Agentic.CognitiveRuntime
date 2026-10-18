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
        Assert.Equal(3, paths.Count);
        Assert.Equal("M0 0L100 0L100 1000Z", paths[0].Attribute("d")!.Value);
        Assert.Equal("translate(200 100) scale(0.8)", paths[1].Attribute("transform")!.Value);
        Assert.Equal("translate(150 450) rotate(0)", paths[2].Attribute("transform")!.Value);
        Assert.NotNull(first.Attribute("fill"));
        Assert.StartsWith("translate(", first.Attribute("transform")!.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCopiesStandSideBySideAndOverlapALittle()
    {
        var offsets = Figures(FigureArt.Read(Fixture), "example-squad").Select(group => X(group.Attribute("transform")!.Value)).ToList();
        var step = offsets[1] - offsets[0];
        Assert.True(step > 0);
        // Positions are written to 1/64 of a pixel, so the steps agree to that.
        Assert.InRange(Math.Abs(step - (offsets[2] - offsets[1])), 0, 2.0 / 64);
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

    private static double X(string transform) =>
        double.Parse(transform["translate(".Length..transform.IndexOf(' ', StringComparison.Ordinal)], System.Globalization.CultureInfo.InvariantCulture);
}
