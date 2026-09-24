using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Maps.Vasl;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>What composing maps needs from the VASL source: the catalog and LOS rules, ingested boards, and oracle fixtures.</summary>
public interface IVaslMapSource
{
    /// <summary>The terrain catalog, the LOS scenario-specific rules, and the catalog's blob, or null when unconfigured.</summary>
    public (TerrainCatalog Catalog, LosSsRuleSet Rules, string CatalogBlob)? Terrain();

    /// <summary>An ingested VASL board, or null when it cannot be ingested.</summary>
    public IngestedBoard? Ingested(BoardRef board);

    /// <summary>The oracle fixture directory, whose <c>Scenarios</c> folder holds map fixtures.</summary>
    public string? OracleFixtures
    {
        get;
    }
}

/// <summary>A saved map: its reference, name, and placements (Model Design section 8.2).</summary>
public sealed record MapDefinition(BoardRef Ref, string Name, IReadOnlyList<BoardPlacement> Placements)
{
    /// <summary>The placements in their compact form, one per board.</summary>
    public string PlacementText => string.Join(' ', Placements);
}

/// <summary>A built map shown in the Studio: its placements and where each board's hexes went.</summary>
public sealed record StudioMap(IReadOnlyList<BoardPlacement> Placements, VaslMap Map);

/// <summary>The outcome of building or saving a map: the map reference when it was saved, and any diagnostics.</summary>
public sealed record MapOutcome(BoardRef? Ref, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool Succeeded => Ref is not null;
}

/// <summary>
/// Composed maps of VASL boards (ASL-MAP-024, VASL Board Ingestion Design section 11): saved as small definitions in
/// <c>{BoardsRoot}/maps</c>, which hold placements only and no VASL data, and built on load as VASL's runtime builds
/// them. A map whose placements match an oracle scenario is checked by F2 against its fixture.
/// </summary>
public sealed class MapService(StudioOptions options, IVaslMapSource source)
{
    public const string FormatVersion = "1";

    private readonly ConcurrentDictionary<string, Lazy<BoardLoadResult>> cache = new(StringComparer.Ordinal);

    public string MapsRoot => Path.Combine(options.ResolveBoardsRoot(), "maps");

    public IReadOnlyList<MapDefinition> List()
    {
        if (!Directory.Exists(MapsRoot))
        {
            return [];
        }

        return Directory.GetFiles(MapsRoot, "*.json")
            .Select(path => Read(Path.GetFileNameWithoutExtension(path)))
            .OfType<MapDefinition>()
            .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public MapDefinition? Find(BoardRef map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return map.Kind == BoardRefKind.ComposedMap ? Read(map.Value[4..]) : null;
    }

    /// <summary>Builds placements without saving, to report what VASL would refuse.</summary>
    public MapOutcome Check(IReadOnlyList<BoardPlacement> placements)
    {
        var (_, diagnostics) = Build(placements);
        return new MapOutcome(null, diagnostics);
    }

    /// <summary>Builds the placements and, when VASL could build them, saves them under a slug of the name.</summary>
    public MapOutcome Save(string name, IReadOnlyList<BoardPlacement> placements)
    {
        ArgumentNullException.ThrowIfNull(placements);
        var slug = Components.Pages.NewBoard.Slug(name ?? string.Empty);
        if (slug.Length == 0 || !BoardRef.TryParse("map-" + slug, out var map))
        {
            return new MapOutcome(null, [Error("STUDIO-MAP-001", "Give the map a name with letters or digits.")]);
        }

        var (built, diagnostics) = Build(placements);
        if (built is null)
        {
            return new MapOutcome(null, diagnostics);
        }

        Directory.CreateDirectory(MapsRoot);
        var json = CanonicalJson.Serialize(new Dictionary<string, object?>
        {
            ["formatVersion"] = FormatVersion,
            ["name"] = name!.Trim(),
            ["placements"] = placements.Select(placement => (object?)placement.ToString()).ToList(),
        });
        File.WriteAllBytes(Path.Combine(MapsRoot, slug + ".json"), json);
        cache.TryRemove(map.Value, out _);
        return new MapOutcome(map, diagnostics);
    }

    public bool Delete(BoardRef map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var path = map.Kind == BoardRefKind.ComposedMap ? Path.Combine(MapsRoot, map.Value[4..] + ".json") : null;
        cache.TryRemove(map.Value, out _);
        if (path is null || !File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    /// <summary>Loads a saved map, building it on first use; the result is cached until the map is saved again.</summary>
    public BoardLoadResult Load(BoardRef map)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (Find(map) is not { } definition)
        {
            return new BoardLoadResult(null, [Error("STUDIO-MAP-002", $"{map} is not a saved map.")]);
        }

        var key = map.Value + "\n" + definition.PlacementText;
        return cache.GetOrAdd(key, _ => new Lazy<BoardLoadResult>(() => LoadUncached(definition))).Value;
    }

    private BoardLoadResult LoadUncached(MapDefinition definition)
    {
        var (built, diagnostics) = Build(definition.Placements);
        if (built is null || source.Terrain() is not { } terrain)
        {
            return new BoardLoadResult(null, diagnostics);
        }

        var map = built.Map!;
        var f2 = CheckF2(built, definition.Placements);
        var status = f2 is { Passed: true } ? BoardStatus.Verified : BoardStatus.Ingested;
        var version = Version(built, terrain.CatalogBlob);
        var title = $"{definition.Name} ({string.Join(", ", definition.Placements.Select(placement => placement.Board.Value + (placement.Reversed ? " reversed" : string.Empty)))})";
        var render = BoardRenderInput.Create(definition.Ref, title, map.Grid, terrain.Catalog, map.Facts);
        return new BoardLoadResult(
            new StudioBoard(definition.Ref, version, title, status, render, terrain.Catalog, null, f2, null, diagnostics)
            {
                Composition = new StudioMap(definition.Placements, map),
            },
            diagnostics);
    }

    private (VaslMapImport? Map, IReadOnlyList<MapDiagnostic> Diagnostics) Build(IReadOnlyList<BoardPlacement> placements)
    {
        if (source.Terrain() is not { } terrain)
        {
            return (null, [Error("STUDIO-001", "Maps need a configured VASL checkout.")]);
        }

        if (placements.Count == 0)
        {
            return (null, [Error("VASL-MAP-001", "A map needs at least one board.")]);
        }

        var boards = new List<IngestedBoard>();
        foreach (var board in placements.Select(placement => placement.Board).Distinct())
        {
            if (source.Ingested(board) is not { } ingested)
            {
                return (null, [Error("VASL-MAP-005", $"{board} could not be ingested.")]);
            }

            boards.Add(ingested);
        }

        var import = VaslMapImporter.Build(boards, terrain.Catalog, terrain.Rules, placements);
        return import.Succeeded ? (import, import.Diagnostics) : (null, import.Diagnostics);
    }

    // The map version covers everything its facts depend on (ASL-MAP-072): each placement and its source blobs, the
    // catalog, and the builder and derivation versions.
    private static string Version(VaslMapImport import, string catalogBlob)
    {
        var text = new StringBuilder()
            .Append("builder ").Append(VaslMapBuilder.Version).Append('\n')
            .Append("derivation ").Append(VaslCompatibleHexFactDerivation.Version).Append('\n')
            .Append("catalog ").Append(catalogBlob).Append('\n');
        foreach (var placement in import.Map!.Boards.Select(board => board.Placement))
        {
            var board = import.Boards.Single(item => item.Board == placement.Board);
            text.Append(placement).Append(' ').Append(board.Provenance.LosData.ContentBlob).Append(' ')
                .Append(board.Provenance.Metadata.ContentBlob).Append('\n');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }

    // F2 applies when an oracle scenario has exactly these placements.
    private F2Result? CheckF2(VaslMapImport import, IReadOnlyList<BoardPlacement> placements)
    {
        var directory = source.OracleFixtures is { } fixtures ? Path.Combine(fixtures, "Scenarios") : null;
        var file = directory is null ? null : Path.Combine(directory, "scenarios.txt");
        if (file is null || !File.Exists(file))
        {
            return null;
        }

        var wanted = Canonical(placements);
        var scenario = VaslScenario.ParseFile(File.ReadAllText(file)).FirstOrDefault(item => Canonical(item.Placements) == wanted);
        return scenario is null ? null : VaslMapImporter.CompareWithFixture(import, scenario, directory!);
    }

    private static string Canonical(IEnumerable<BoardPlacement> placements) =>
        string.Join(' ', placements.OrderBy(placement => placement.Row).ThenBy(placement => placement.Column));

    private MapDefinition? Read(string slug)
    {
        var path = Path.Combine(MapsRoot, slug + ".json");
        if (!File.Exists(path) || !BoardRef.TryParse("map-" + slug, out var map))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var placements = VaslScenario.ParsePlacements(string.Join(' ', root.GetProperty("placements").EnumerateArray().Select(item => item.GetString())));
            return placements is null ? null : new MapDefinition(map, root.GetProperty("name").GetString() ?? slug, placements);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private static MapDiagnostic Error(string code, string message) => new(code, MapDiagnosticSeverity.Error, message);
}
