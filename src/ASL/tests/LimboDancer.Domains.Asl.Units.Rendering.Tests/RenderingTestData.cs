using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering.Palettes;
using LimboDancer.Domains.Asl.Units.Rendering.Styles;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>Shared inputs and helpers: the built-in vocabulary, sheets, palettes, and the example catalog.</summary>
internal static class RenderingTestData
{
    public static readonly Lazy<UnitVocabulary> Vocabulary = new(UnitVocabulary.Asl);

    public static readonly IReadOnlyDictionary<string, Lazy<UnitRenderer>> Renderers = UnitStyles.BuiltIn.ToDictionary(
        name => name,
        name => new Lazy<UnitRenderer>(() =>
        {
            var sheet = UnitStyles.Load(name);
            return new UnitRenderer(Vocabulary.Value, sheet, PaletteSetReader.Embedded(sheet.Palette!));
        }),
        StringComparer.Ordinal);

    public static readonly Lazy<IReadOnlyDictionary<string, UnitDocument>> Catalog = new(() =>
    {
        var result = UnitDocumentReader.Read(File.ReadAllBytes(Path.Combine(UnitsDirectory(), "examples", "catalog.units.json")), Vocabulary.Value);
        Assert.Empty(result.Diagnostics);
        return result.Documents.ToDictionary(document => document.Id, StringComparer.Ordinal);
    });

    public static UnitRenderer Renderer(string sheet) => Renderers[sheet].Value;

    public static UnitRenderer Renderer(StyleSheet sheet, UnitVocabulary? vocabulary = null) =>
        new(vocabulary ?? Vocabulary.Value, sheet, PaletteSetReader.Embedded("limbodancer"));

    public static StyleSheet Sheet(string text)
    {
        var result = StyleSheetParser.Parse("test", text);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics));
        return result.Sheet!;
    }

    public static UnitDocument Document(string json, UnitVocabulary? vocabulary = null)
    {
        var result = UnitDocumentReader.Read(json, vocabulary ?? Vocabulary.Value);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error);
        return Assert.Single(result.Documents);
    }

    /// <summary>One unit at one tier, as parsed SVG.</summary>
    public static XElement Render(UnitRenderer renderer, UnitDocument document, DetailTier tier = DetailTier.Near, string? face = null,
        List<RenderWarning>? warnings = null)
    {
        var svg = new SvgWriter();
        renderer.WriteUnit(svg, document, 50, 50, UnitPreview.HexHeight, [tier], face, null, warnings ?? []);
        return XElement.Parse(svg.ToString());
    }

    public static string UnitsDirectory() => Path.Combine(RepositoryRoot(), "src", "ASL", "units");

    public static string GoldenDirectory() => Path.Combine(RepositoryRoot(), "src", "ASL", "tests", "LimboDancer.Domains.Asl.Units.Rendering.Tests", "Golden");

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
