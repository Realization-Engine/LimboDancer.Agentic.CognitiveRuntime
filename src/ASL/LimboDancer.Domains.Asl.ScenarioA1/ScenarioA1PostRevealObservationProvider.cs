using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

public enum ScenarioA1DefenderReveal
{
    Unknown,
    NonDummy,
    DummiesOnly,
}

/// <summary>Supplied state after A12.15 defender resolution, without game-state mutation.</summary>
public sealed record ScenarioA1PostRevealSnapshot(
    Guid TenantId, DomainPackageRef Package, string UnitId, string LocationId,
    string PreviousLocationId, string Version, DateTimeOffset ObservedAt, string SourceId,
    ScenarioA1TerrainBinding Terrain, ScenarioA1DefenderReveal DefenderReveal,
    bool? IsMovementPhase, bool? IsOrdinaryInfantry,
    bool? IsAttackerUnconcealedNonDummy, bool? IsObstacleEntryNotBypass,
    bool? HasNoA414EntryException, bool? HasNoOverrunElection,
    bool? HasNoSpecialModifier);

public interface IScenarioA1PostRevealSnapshotSource
{
    ValueTask<ScenarioA1PostRevealSnapshot?> ReadAsync(Guid tenantId, DomainPackageRef package,
        string unitId, string locationId, CancellationToken cancellationToken = default);
}

/// <summary>Projects only fully resolved, ordinary board 01 entry attempts.</summary>
public sealed class ScenarioA1PostRevealObservationProvider(
    IScenarioA1PostRevealSnapshotSource source, IScenarioA1TerrainEvidence terrain)
    : IObservationProvider
{
    public const string QueryKind = "scenario-a1-post-reveal-location";

    public async ValueTask<ObservationAcquisitionResult> ObserveAsync(ObservationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Package != ScenarioA1PostRevealPackage.Identity
            || query.Kind.Value != QueryKind || query.Parameters.ValueKind != JsonValueKind.Object
            || query.Parameters.EnumerateObject().Count() != 3 || query.MaxResults != 1)
            return Empty(query, "asl.a1.reveal.query-outside-scope");
        var unitId = Parameter(query.Parameters, "unitId");
        var locationId = Parameter(query.Parameters, "locationId");
        var version = Parameter(query.Parameters, "observationVersion");
        if (unitId is null || locationId is null || version is null || unitId == locationId)
            return Empty(query, "asl.a1.reveal.query-incomplete");
        var snapshot = await source.ReadAsync(query.TenantId, query.Package, unitId, locationId,
            cancellationToken);
        if (snapshot is null || snapshot.TenantId != query.TenantId
            || snapshot.Package != query.Package || snapshot.UnitId != unitId
            || snapshot.LocationId != locationId || snapshot.Version != version
            || snapshot.ObservedAt.Offset != TimeSpan.Zero
            || string.IsNullOrWhiteSpace(snapshot.SourceId)
            || string.IsNullOrWhiteSpace(snapshot.PreviousLocationId)
            || snapshot.PreviousLocationId == locationId
            || !terrain.IsSupportedGroundLevel(snapshot.Terrain)
            || locationId != "bd01:" + snapshot.Terrain.Hex + ":0")
            return Empty(query, "asl.a1.reveal.snapshot-scope-or-terrain-mismatch");
        if (snapshot.DefenderReveal is not (ScenarioA1DefenderReveal.NonDummy
            or ScenarioA1DefenderReveal.DummiesOnly)
            || snapshot.IsMovementPhase != true || snapshot.IsOrdinaryInfantry != true
            || snapshot.IsAttackerUnconcealedNonDummy != true
            || snapshot.IsObstacleEntryNotBypass != true
            || snapshot.HasNoA414EntryException != true
            || snapshot.HasNoOverrunElection != true
            || snapshot.HasNoSpecialModifier != true)
            return Empty(query, "asl.a1.reveal.unresolved-or-excluded");
        var facts = JsonSerializer.SerializeToElement(new Dictionary<string, string>
        {
            ["a414Exception"] = "none",
            ["attacker"] = "unconcealedNonDummyOrdinaryInfantry",
            ["defenderReveal"] = snapshot.DefenderReveal == ScenarioA1DefenderReveal.NonDummy
                ? "nonDummy" : "dummiesOnly",
            ["entryMode"] = "obstacleEntryNotBypass",
            ["overrunElection"] = "none",
            ["phase"] = "mph",
            ["previousLocationId"] = snapshot.PreviousLocationId,
            ["specialModifier"] = "none",
        });
        var observation = new Observation("asl-a1-reveal:" + query.QueryId,
            new ObservationSource(snapshot.SourceId), query.TenantId, snapshot.ObservedAt,
            facts, locationId, snapshot.Version, snapshot.SourceId, query.Package);
        return new ObservationAcquisitionResult(query, [observation],
            ["asl.a1.reveal.resolved"]);
    }

    private static ObservationAcquisitionResult Empty(ObservationQuery query, string reason) =>
        new(query, [], [reason]);

    private static string? Parameter(JsonElement parameters, string key) =>
        parameters.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;
}
