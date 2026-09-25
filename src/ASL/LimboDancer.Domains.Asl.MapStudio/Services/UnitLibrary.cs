using System.Collections.Concurrent;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering;
using LimboDancer.Domains.Asl.Units.Rendering.Palettes;
using LimboDancer.Domains.Asl.Units.Rendering.Styles;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>A placement set and where it came from: shipped with the Studio, or saved from the Unit Lab.</summary>
public sealed record UnitSetEntry(UnitPlacementSet Set, bool BuiltIn, IReadOnlyList<UnitDiagnostic> Diagnostics);

/// <summary>
/// The Studio's unit display inputs (Unit Display Design, sections 8 and 10): the built-in vocabulary, the two ASL
/// style sheets and palette sets, the synthetic example documents and placement sets, and what the Unit Lab saves
/// under <c>{boards folder}/units</c>. Placement sets are display input only; nothing here is game state (ASL-UNIT-070).
/// </summary>
public sealed class UnitLibrary(StudioOptions options)
{
    public const string DefaultSheet = UnitStyles.Digital;

    private readonly Lazy<UnitVocabulary> vocabulary = new(UnitVocabulary.Asl);
    private readonly ConcurrentDictionary<string, PaletteSet> palettes = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<(string SheetHash, string Palette), UnitRenderer> renderers = new();
    private IReadOnlyList<UnitDocument>? examples;

    public UnitVocabulary Vocabulary => vocabulary.Value;

    public string UnitsRoot => Path.Combine(options.ResolveBoardsRoot(), "units");

    /// <summary>The synthetic example documents the Lab offers as starting points.</summary>
    public IReadOnlyList<UnitDocument> Examples => examples ??= UnitExamples.Catalog(Vocabulary);

    public IReadOnlyList<string> PaletteNames => PaletteSetReader.BuiltIn;

    /// <summary>The built-in sheets, then saved ones by name.</summary>
    public IReadOnlyList<string> SheetNames()
    {
        IEnumerable<string> saved = Directory.Exists(StylesRoot)
            ? Directory.EnumerateFiles(StylesRoot, "*.uss").Select(Path.GetFileNameWithoutExtension).OfType<string>()
                .Where(name => VocabularyNames.IsSlug(name) && !UnitStyles.BuiltIn.Contains(name)).Order(StringComparer.Ordinal)
            : [];
        return [.. UnitStyles.BuiltIn, .. saved];
    }

    public bool IsBuiltInSheet(string name) => UnitStyles.BuiltIn.Contains(name);

    public string? SheetText(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (IsBuiltInSheet(name))
        {
            return UnitStyles.Text(name);
        }

        var path = Path.Combine(StylesRoot, name + ".uss");
        return VocabularyNames.IsSlug(name) && File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public StyleParseResult ParseSheet(string name, string text) => StyleSheetParser.Parse(name, text);

    public PaletteSet Palette(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return palettes.GetOrAdd(PaletteNames.Contains(name) ? name : "limbodancer", PaletteSetReader.Embedded);
    }

    /// <summary>A renderer for a named sheet, or null when the sheet is missing or does not parse.</summary>
    public UnitRenderer? Renderer(string sheetName, string? palette = null)
    {
        ArgumentNullException.ThrowIfNull(sheetName);
        return SheetText(sheetName) is { } text && ParseSheet(sheetName, text).Sheet is { } sheet ? Renderer(sheet, palette) : null;
    }

    /// <summary>A renderer for a parsed sheet, with its <c>@palette</c> set unless another is named.</summary>
    public UnitRenderer Renderer(StyleSheet sheet, string? palette = null)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var paletteName = palette ?? sheet.Palette ?? "limbodancer";
        return renderers.GetOrAdd((sheet.Hash, paletteName), _ => new UnitRenderer(Vocabulary, sheet, Palette(paletteName)));
    }

    /// <summary>Every placement set: the built-in synthetic examples, then sets saved from the Lab.</summary>
    public IReadOnlyList<UnitSetEntry> Sets()
    {
        var entries = new List<UnitSetEntry>();
        foreach (var result in UnitExamples.PlacementSets(Vocabulary))
        {
            if (result.Set is { } set)
            {
                entries.Add(new UnitSetEntry(set, BuiltIn: true, result.Diagnostics));
            }
        }

        if (Directory.Exists(PlacementsRoot))
        {
            foreach (var path in Directory.EnumerateFiles(PlacementsRoot, "*.units.json").Order(StringComparer.Ordinal))
            {
                var result = UnitPlacementSetReader.Read(File.ReadAllBytes(path), Vocabulary);
                if (result.Set is { } set && entries.All(entry => entry.Set.SetId != set.SetId))
                {
                    entries.Add(new UnitSetEntry(set, BuiltIn: false, result.Diagnostics));
                }
            }
        }

        return entries;
    }

    public UnitSetEntry? Set(string setId) => Sets().FirstOrDefault(entry => entry.Set.SetId == setId);

    /// <summary>The sets with at least one unit on a board the target contains.</summary>
    public IReadOnlyList<UnitSetEntry> SetsFor(StudioBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        var target = TargetFor(board);
        return [.. Sets().Where(entry => entry.Set.Units.Any(unit => BoardLocation.TryParse(unit.Location, out var location) && target.Locate(location) is not null))];
    }

    /// <summary>A board places units by its own geometry; a composed map through its placed boards.</summary>
    public static UnitMapTarget TargetFor(StudioBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return board.Composition is { } composition
            ? UnitMapTarget.ForMap(board.Ref, composition.Map)
            : UnitMapTarget.ForBoard(board.Ref, board.Render.Grid.Geometry);
    }

    public UnitOverlay Overlay(StudioBoard board, UnitPlacementSet set, UnitRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(set);
        return UnitOverlayBuilder.Build(TargetFor(board), set.SetId, set.Units, renderer);
    }

    /// <summary>Saves a style sheet; built-in names are kept read-only.</summary>
    public string? SaveSheet(string name, string text)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(text);
        if (!VocabularyNames.IsSlug(name))
        {
            return "A sheet name is a lowercase slug, such as my-sheet.";
        }

        if (IsBuiltInSheet(name))
        {
            return $"{name} ships with the Studio; save the edited sheet under another name.";
        }

        Directory.CreateDirectory(StylesRoot);
        File.WriteAllText(Path.Combine(StylesRoot, name + ".uss"), text.ReplaceLineEndings("\n"), new UTF8Encoding(false));
        return null;
    }

    public string SaveDocument(UnitDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Directory.CreateDirectory(DocumentsRoot);
        var path = Path.Combine(DocumentsRoot, FileName(document.Id) + ".unit.json");
        File.WriteAllText(path, UnitDocumentJson.Write(document, Vocabulary), new UTF8Encoding(false));
        return path;
    }

    /// <summary>Saves a placement set from the Lab; built-in set ids are kept read-only.</summary>
    public string? SaveSet(UnitPlacementSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        if (!VocabularyNames.IsSlug(set.SetId))
        {
            return "A set id is a lowercase slug, such as lab-units.";
        }

        if (Sets().Any(entry => entry.BuiltIn && entry.Set.SetId == set.SetId))
        {
            return $"{set.SetId} ships with the Studio; save under another id.";
        }

        Directory.CreateDirectory(PlacementsRoot);
        File.WriteAllText(Path.Combine(PlacementsRoot, set.SetId + ".units.json"), UnitDocumentJson.Write(set, Vocabulary), new UTF8Encoding(false));
        return null;
    }

    private string StylesRoot => Path.Combine(UnitsRoot, "styles");

    private string PlacementsRoot => Path.Combine(UnitsRoot, "placements");

    private string DocumentsRoot => Path.Combine(UnitsRoot, "documents");

    private static string FileName(string id) => string.Concat(id.Select(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' ? character : '-'));

}
