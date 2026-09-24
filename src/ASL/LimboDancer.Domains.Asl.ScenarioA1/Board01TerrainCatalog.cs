using System.Reflection;
using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

public sealed record ScenarioA1TerrainBinding(
    string BoardId, string BoardVersion, string MetadataGitBlobSha,
    string Hex, int Level, string? Variant);

public sealed record Board01Building(string Material, int Levels);

/// <summary>Only the explicit building-type overrides in VASL board 01 metadata.</summary>
public sealed class Board01TerrainCatalog
{
    public const string BoardId = "01";
    public const string BoardVersion = "6.9";
    public const string MetadataGitBlobSha = "e91b0d99a7a788812444753cae245841875a8de6";

    private readonly Dictionary<string, Board01Building> buildings = Load();

    public int OverrideCount => buildings.Count;

    public bool TryGetBuilding(string hex, out Board01Building? building) =>
        buildings.TryGetValue(hex, out building);

    public bool IsSupportedGroundLevel(ScenarioA1TerrainBinding? binding)
    {
        return binding is { BoardId: BoardId, BoardVersion: BoardVersion,
            MetadataGitBlobSha: MetadataGitBlobSha, Level: 0, Variant: null }
            && buildings.ContainsKey(binding.Hex);
    }

    private static Dictionary<string, Board01Building> Load()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("ScenarioA1.board01-buildings.json")
            ?? throw new InvalidOperationException("Board 01 metadata inventory is missing.");
        using var json = JsonDocument.Parse(stream);
        var root = json.RootElement;
        if (root.GetProperty("board").GetString() != BoardId
            || root.GetProperty("version").GetString() != BoardVersion
            || root.GetProperty("metadataGitBlobSha").GetString() != MetadataGitBlobSha)
            throw new InvalidOperationException("Board 01 metadata identity differs from the pinned source.");

        var result = new Dictionary<string, Board01Building>(StringComparer.Ordinal);
        foreach (var group in root.GetProperty("overrides").EnumerateObject())
        {
            var building = group.Name switch
            {
                "Stone Building, 2 Level" => new Board01Building("stone", 2),
                "Stone Building, 1 Level" => new Board01Building("stone", 1),
                "Wooden Building, 1 Level" => new Board01Building("wooden", 1),
                _ => throw new InvalidOperationException("Unknown board building type."),
            };
            foreach (var hex in group.Value.EnumerateArray())
                if (!result.TryAdd(hex.GetString()!, building))
                    throw new InvalidOperationException("Duplicate board building override.");
        }
        return result;
    }
}

/// <summary>Checks supplied snapshot terrain claims against the pinned board 01 overrides.</summary>
public sealed class Board01ValidatedSnapshotSource(
    IScenarioA1BoardSnapshotSource source, Board01TerrainCatalog catalog)
    : IScenarioA1BoardSnapshotSource
{
    public async ValueTask<ScenarioA1BoardSnapshot?> ReadAsync(Guid tenantId,
        DomainPackageRef package, string unitId, string locationId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await source.ReadAsync(tenantId, package, unitId, locationId,
            cancellationToken);
        if (snapshot is null || snapshot.TenantId != tenantId || snapshot.Package != package
            || snapshot.UnitId != unitId || snapshot.LocationId != locationId
            || snapshot.Terrain is not { } terrain
            || !catalog.IsSupportedGroundLevel(terrain)
            || locationId != "bd01:" + terrain.Hex + ":0"
            || snapshot.IsKnownBuildingLocation != true
            || (snapshot.Occupancy == ScenarioA1BoardOccupancy.KnownEmpty
                && snapshot.IsAdjacentGroundLevelOrdinaryBuilding != true))
            return null;
        return snapshot;
    }
}
