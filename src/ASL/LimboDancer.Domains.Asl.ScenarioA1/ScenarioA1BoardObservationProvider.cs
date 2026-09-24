using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

public enum ScenarioA1BoardOccupancy
{
    Unknown,
    KnownEmpty,
    KnownUnconcealedEnemyMmc,
    Other,
}

/// <summary>Typed state returned by a tenant-scoped board source, not a caller's case label.</summary>
public sealed record ScenarioA1BoardSnapshot(
    Guid TenantId,
    DomainPackageRef Package,
    string UnitId,
    string LocationId,
    string Version,
    DateTimeOffset ObservedAt,
    string SourceId,
    ScenarioA1BoardOccupancy Occupancy,
    bool? IsMovementPhase,
    bool? IsGoodOrderInfantrySquad,
    bool? CanMove,
    bool? IsAdjacentGroundLevelOrdinaryBuilding,
    bool? IsKnownBuildingLocation,
    bool? HasAtLeastTwoMf,
    bool? HasNoAdditionalTerrain,
    bool? BelowNormalStackingLimit,
    bool? HasNoSpecialModifier,
    bool? HasNoA414Exception,
    ScenarioA1TerrainBinding? Terrain = null);

public interface IScenarioA1BoardSnapshotSource
{
    ValueTask<ScenarioA1BoardSnapshot?> ReadAsync(Guid tenantId, DomainPackageRef package,
        string unitId, string locationId, CancellationToken cancellationToken = default);
}

/// <summary>Projects two source-backed board states into exact-case observations.</summary>
public sealed class ScenarioA1BoardObservationProvider(IScenarioA1BoardSnapshotSource source)
    : IObservationProvider
{
    public const string QueryKind = "scenario-a1-board-location";

    public async ValueTask<ObservationAcquisitionResult> ObserveAsync(ObservationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Package != ScenarioA1OccupiedPackage.Identity || query.Kind.Value != QueryKind
            || query.Parameters.EnumerateObject().Count() != 3
            || query.MaxResults != 1)
            return Empty(query, "asl.a1.board.query-outside-admitted-scope");
        var unitId = Parameter(query.Parameters, "unitId");
        var locationId = Parameter(query.Parameters, "locationId");
        var expectedVersion = Parameter(query.Parameters, "observationVersion");
        if (unitId is null || locationId is null || expectedVersion is null || unitId == locationId)
            return Empty(query, "asl.a1.board.query-incomplete");

        var snapshot = await source.ReadAsync(query.TenantId, query.Package, unitId, locationId,
            cancellationToken);
        if (snapshot is null)
            return Empty(query, "asl.a1.board.snapshot-unavailable");
        if (snapshot.TenantId != query.TenantId || snapshot.Package != query.Package
            || snapshot.UnitId != unitId || snapshot.LocationId != locationId
            || snapshot.Version != expectedVersion || string.IsNullOrWhiteSpace(snapshot.SourceId)
            || snapshot.ObservedAt.Offset != TimeSpan.Zero)
            return Empty(query, "asl.a1.board.snapshot-scope-or-version-mismatch");

        Dictionary<string, string>? facts = null;
        if (snapshot.Occupancy == ScenarioA1BoardOccupancy.KnownEmpty
            && snapshot.IsMovementPhase == true
            && snapshot.IsGoodOrderInfantrySquad == true && snapshot.CanMove == true
            && snapshot.IsAdjacentGroundLevelOrdinaryBuilding == true
            && snapshot.HasAtLeastTwoMf == true && snapshot.HasNoAdditionalTerrain == true
            && snapshot.BelowNormalStackingLimit == true && snapshot.HasNoSpecialModifier == true)
        {
            facts = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["canMove"] = "true", ["location"] = "adjacentGroundLevelOrdinaryBuilding",
                ["occupancy"] = "knownEmpty", ["phase"] = "mph",
                ["remainingMf"] = "atLeastTwo", ["roadBypassElevationAdditionalTerrain"] = "none",
                ["specialModifier"] = "none", ["stacking"] = "belowNormalLimit",
                ["unit"] = "knownGoodOrderInfantrySquad",
            };
        }
        else if (snapshot.Occupancy == ScenarioA1BoardOccupancy.KnownUnconcealedEnemyMmc
            && snapshot.IsMovementPhase == true && snapshot.IsGoodOrderInfantrySquad == true
            && snapshot.CanMove == true && snapshot.IsKnownBuildingLocation == true
            && snapshot.HasNoSpecialModifier == true && snapshot.HasNoA414Exception == true)
        {
            facts = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["a414Exception"] = "none", ["location"] = "knownBuildingLocation",
                ["occupancy"] = "knownUnconcealedEnemyMmc", ["phase"] = "mph",
                ["specialModifier"] = "none", ["unit"] = "ordinaryInfantry",
            };
        }
        if (facts is null)
            return Empty(query, "asl.a1.board.state-outside-reviewed-cases");

        var candidate = new ScenarioA1SemanticCandidate();
        var matches = candidate.Cases.Where(item => item.ReviewStatus == "reviewed-bounded"
            && candidate.Evaluate(item.Id, facts).Disposition == item.ExpectedDisposition).ToArray();
        if (matches.Length != 1)
            return Empty(query, "asl.a1.board.case-ambiguous");
        var observation = new Observation("asl-board:" + query.QueryId,
            new ObservationSource(snapshot.SourceId), query.TenantId, snapshot.ObservedAt,
            JsonSerializer.SerializeToElement(facts), locationId, snapshot.Version,
            snapshot.SourceId, query.Package);
        return new ObservationAcquisitionResult(query, [observation],
            ["asl.a1.board.exact-case:" + matches[0].Id]);
    }

    private static ObservationAcquisitionResult Empty(ObservationQuery query, string reason) =>
        new(query, [], [reason]);

    private static string? Parameter(JsonElement parameters, string key) =>
        parameters.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;
}
