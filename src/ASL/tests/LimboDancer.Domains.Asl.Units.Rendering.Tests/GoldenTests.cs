using System.Globalization;
using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering.Styles;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>
/// Determinism and goldens (section 12): one golden SVG per kind and per state under each ASL sheet, each showing the
/// far, mid, and near tiers side by side, and a pairwise set of Personnel attribute combinations. Set
/// <c>ASL_UNITS_UPDATE_GOLDEN=1</c> to rewrite them after an intended change.
/// </summary>
public sealed class GoldenTests
{
    private const string UpdateGoldenVariable = "ASL_UNITS_UPDATE_GOLDEN";

    private static readonly DetailTier[] Tiers = [DetailTier.Far, DetailTier.Mid, DetailTier.Near];

    /// <summary>Golden name to document: the catalog's kinds, and each state on a unit it applies to.</summary>
    public static TheoryData<string, string> Cases()
    {
        var data = new TheoryData<string, string>();
        foreach (var sheet in UnitStyles.BuiltIn)
        {
            foreach (var name in Documents().Keys)
            {
                data.Add(sheet, name);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EachKindAndStateMatchesItsGolden(string sheet, string name) =>
        MatchesGolden(sheet, name, Strip(RenderingTestData.Renderer(sheet), Documents()[name]));

    [Theory]
    [InlineData(UnitStyles.Classic)]
    [InlineData(UnitStyles.Digital)]
    public void PairwiseAttributeCombinationsMatchTheirGolden(string sheet)
    {
        var renderer = RenderingTestData.Renderer(sheet);
        var combinations = Pairwise().ToArray();
        var svg = new SvgWriter();
        var cell = UnitPreview.HexHeight;
        svg.Start("svg", ("xmlns", "http://www.w3.org/2000/svg"), ("viewBox", $"0 0 {SvgWriter.Number(cell * 3)} {SvgWriter.Number(cell * 3)}"));
        for (var index = 0; index < combinations.Length; index++)
        {
            renderer.WriteUnit(svg, combinations[index], (cell * (index % 3)) + (cell / 2), (cell * (index / 3)) + (cell / 2), UnitPreview.HexHeight,
                [DetailTier.Near], null, null, []);
        }

        svg.End();
        MatchesGolden(sheet, "pairwise-personnel", svg.ToString());

        // Every combination draws differently.
        var drawn = combinations.Select(unit => RenderingTestData.Render(renderer, unit).ToString()).ToArray();
        Assert.Equal(drawn.Length, drawn.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(UnitStyles.Classic)]
    [InlineData(UnitStyles.Digital)]
    public void TheSameInputsWriteTheSameBytes(string sheet)
    {
        foreach (var document in Documents().Values)
        {
            var first = Strip(RenderingTestData.Renderer(sheet), document);
            var sheetAgain = StyleSheetParser.Parse(sheet, UnitStyles.Text(sheet)).Sheet!;
            var renderer = new UnitRenderer(Units.Vocabulary.UnitVocabulary.Asl(), sheetAgain, RenderingTestData.Renderer(sheet).Palette);
            Assert.Equal(first, Strip(renderer, document));
        }
    }

    [Theory]
    [InlineData(UnitStyles.Classic)]
    [InlineData(UnitStyles.Digital)]
    public void UnitsAreAccessibleButtonsNamedFromTheVocabulary(string sheet)
    {
        var renderer = RenderingTestData.Renderer(sheet);
        var squad = RenderingTestData.Document("""
            { "id": "s", "kind": "asl:squad", "side": "german",
              "faces": { "front": { "firepower": 4, "range": 6, "morale": 7, "identity": "A", "class": "1st-line" } },
              "states": ["asl:pinned"],
              "attached": [ { "id": "m", "kind": "asl:mg", "faces": { "front": { "size": "light", "firepower": 3, "range": 6 } } } ] }
            """);
        var svg = RenderingTestData.Render(renderer, squad);
        Assert.Equal("s", svg.Attribute("data-unit-id")!.Value);
        Assert.Equal("button", svg.Attribute("role")!.Value);
        Assert.Equal("0", svg.Attribute("tabindex")!.Value);
        const string Name = "German 1st Line squad A, 4-6-7, pinned, with light MG 3-6";
        Assert.Equal(Name, svg.Attribute("aria-label")!.Value);
        Assert.Equal(Name, svg.Element("title")!.Value);
        Assert.DoesNotContain(svg.Descendants(), element => element.Name.LocalName is "image" or "script" or "foreignObject" || element.Attribute("href") is not null);
    }

    [Fact]
    public void EveryTierIsWrittenOnceAndFarShowsOnlyTheShapeAndGlyph()
    {
        var renderer = RenderingTestData.Renderer(UnitStyles.Digital);
        var squad = Documents()["state-pinned"];
        var svg = new SvgWriter();
        renderer.WriteUnit(svg, squad, 50, 50, UnitPreview.HexHeight, Tiers, null, null, []);
        var tiers = XElement.Parse(svg.ToString()).Elements("g").ToArray();
        Assert.Equal(["far", "mid", "near"], tiers.Select(tier => tier.Attribute("data-tier")!.Value));
        Assert.Equal(["glyph"], SlotsOf(tiers[0]));
        Assert.DoesNotContain(tiers[0].Descendants(), element => element.Attribute("data-badge") is not null);
        Assert.DoesNotContain(tiers[0].Descendants(), element => element.Attribute("data-attached-id") is not null && element.HasElements);
        Assert.Contains("fp", SlotsOf(tiers[1]));
        Assert.DoesNotContain("ident", SlotsOf(tiers[1]));
        Assert.Contains("ident", SlotsOf(tiers[2]));
        Assert.Contains(tiers[1].Descendants(), element => element.Attribute("data-badge") is not null);
    }

    [Fact]
    public void FaceSizeIsAFractionOfTheHexHeight()
    {
        var renderer = RenderingTestData.Renderer(UnitStyles.Digital);
        Assert.Equal(0.55 * 64.5, renderer.FaceSize(Documents()["kind-squad"], DetailTier.Near, 64.5), 6);
        var larger = new UnitRenderer(renderer.Vocabulary, RenderingTestData.Sheet("unit { face-size: 0.7; }"), renderer.Palette);
        Assert.Equal(0.7 * 100, larger.FaceSize(Documents()["kind-squad"], DetailTier.Near, 100), 6);
    }

    internal static IReadOnlyDictionary<string, UnitDocument> Documents()
    {
        var catalog = RenderingTestData.Catalog.Value;
        var documents = new SortedDictionary<string, UnitDocument>(StringComparer.Ordinal)
        {
            ["kind-squad"] = catalog["example-squad"],
            ["kind-half-squad"] = catalog["example-half-squad"],
            ["kind-crew"] = catalog["example-crew"],
            ["kind-leader"] = catalog["example-leader"],
            ["kind-hero"] = catalog["example-hero"],
            ["kind-mg"] = catalog["example-mmg"],
            ["kind-ft"] = catalog["example-ft"],
            ["kind-dc"] = catalog["example-dc"],
            ["kind-latw"] = catalog["example-latw"],
            ["kind-light-mortar"] = catalog["example-mortar"],
            ["kind-radio"] = catalog["example-radio"],
            ["kind-concealed-placeholder"] = catalog["example-concealed"],
            ["kind-gun-at"] = catalog["example-at-gun"],
            ["kind-gun-aa"] = catalog["example-aa-gun"],
            ["kind-gun-mtr"] = catalog["example-mortar-gun"],
            ["kind-gun-inf"] = catalog["example-inf-gun"],
        };
        foreach (var state in RenderingTestData.Vocabulary.Value.States)
        {
            var target = state.Name switch
            {
                "asl:malfunctioned" => catalog["example-mmg"],
                "asl:wounded" => catalog["example-hero"],
                "asl:limbered" => catalog["example-aa-gun"],
                _ => catalog["example-squad"],
            };
            documents["state-" + state.Name[4..]] = target with
            {
                Id = "state-" + state.Name[4..],
                States = [state.Name]
            };
        }

        // A Gun's malfunctioned side differs from a support weapon's: Repair and Removal Numbers (C2.2).
        documents["state-malfunctioned-gun"] = catalog["example-at-gun"] with
        {
            Id = "state-malfunctioned-gun",
            States = ["asl:malfunctioned"]
        };
        return documents;
    }

    /// <summary>
    /// An L9 orthogonal array over class, class variant, smoke exponent, and traits covers every pair of their values;
    /// the state cycles through none, pinned, and CX.
    /// </summary>
    private static IEnumerable<UnitDocument> Pairwise()
    {
        int[][] array = [[0, 0, 0, 0], [0, 1, 1, 1], [0, 2, 2, 2], [1, 0, 1, 2], [1, 1, 2, 0], [1, 2, 0, 1], [2, 0, 2, 1], [2, 1, 0, 2], [2, 2, 1, 0]];
        string[] classes = ["elite", "1st-line", "conscript"];
        string[] variants = ["", ", \"class-variant\": \"circle\"", ", \"class-variant\": \"square\""];
        string[] smoke = ["", ", \"unit\": { \"smoke-exponent\": 1 }", ", \"unit\": { \"smoke-exponent\": 3 }"];
        string[] traits = ["", ", \"traits\": [\"asl:assault-fire\"]", ", \"traits\": [\"asl:spraying-fire\", \"asl:elr-5\"]"];
        string[] states = ["", ", \"states\": [\"asl:pinned\"]", ", \"states\": [\"asl:cx\"]"];
        for (var index = 0; index < array.Length; index++)
        {
            var row = array[index];
            var id = "pair-" + index.ToString(CultureInfo.InvariantCulture);
            yield return RenderingTestData.Document(
                $$"""
                { "id": "{{id}}", "kind": "asl:squad", "side": "{{(index % 2 == 0 ? "german" : "russian")}}",
                  "faces": { "front": { "firepower": 4, "range": 6, "morale": 7, "identity": "A", "class": "{{classes[row[0]]}}"{{variants[row[1]]}}{{traits[row[3]]}} } }
                  {{smoke[row[2]]}}{{states[index % 3]}} }
                """);
        }
    }

    /// <summary>The three tiers side by side, so a golden can be read by eye.</summary>
    private static string Strip(UnitRenderer renderer, UnitDocument document)
    {
        var size = UnitPreview.HexHeight * 1.4;
        var svg = new SvgWriter();
        svg.Start("svg", ("xmlns", "http://www.w3.org/2000/svg"), ("viewBox", $"0 0 {SvgWriter.Number(size * 3)} {SvgWriter.Number(size)}"));
        for (var index = 0; index < Tiers.Length; index++)
        {
            renderer.WriteUnit(svg, document, (size * index) + (size / 2), size * 0.45, UnitPreview.HexHeight, [Tiers[index]], null, null, []);
        }

        svg.End();
        return svg.ToString();
    }

    private static void MatchesGolden(string sheet, string name, string actual)
    {
        var directory = Path.Combine(RenderingTestData.GoldenDirectory(), sheet);
        var path = Path.Combine(directory, name + ".svg");
        if (Environment.GetEnvironmentVariable(UpdateGoldenVariable) == "1")
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, actual + "\n");
        }

        Assert.True(File.Exists(path), $"Missing {path}; set {UpdateGoldenVariable}=1 to create it.");
        Assert.Equal(File.ReadAllText(path).ReplaceLineEndings("\n").TrimEnd('\n'), actual);
    }

    private static string[] SlotsOf(XElement tier) =>
        [.. tier.Descendants().Where(element => element.Attribute("data-slot") is not null && element.Ancestors().All(a => a.Attribute("data-attached-id") is null))
            .Select(element => element.Attribute("data-slot")!.Value)];
}
