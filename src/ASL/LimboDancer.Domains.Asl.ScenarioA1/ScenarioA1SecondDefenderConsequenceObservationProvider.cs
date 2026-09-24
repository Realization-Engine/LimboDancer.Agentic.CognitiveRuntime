using System.Text.Json;
using System.Text.RegularExpressions;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Supplied attempt, previous Location, and the already bounded second-defender events.</summary>
public sealed record ScenarioA1SecondDefenderConsequenceSnapshot(
    DomainPackageRef Package, ScenarioA1SecondDefenderSnapshot Eligibility,
    string PreviousLocationId, bool? IsPreviousLocationLastOccupied,
    int? AttemptOrdinal, int? AttemptedEntryMf, bool? OvrEntryResolved);

public interface IScenarioA1SecondDefenderConsequenceSnapshotSource
{
    ValueTask<ScenarioA1SecondDefenderConsequenceSnapshot?> ReadAsync(Guid tenantId,
        DomainPackageRef package, string unitId, string locationId,
        CancellationToken cancellationToken = default);
}

/// <summary>Projects supplied ordered events and attempted entry without executing consequences.</summary>
public sealed class ScenarioA1SecondDefenderConsequenceObservationProvider(
    IScenarioA1SecondDefenderConsequenceSnapshotSource source, Board01TerrainCatalog terrain)
    : IObservationProvider
{
    public const string QueryKind = "scenario-a1-second-defender-consequence-location";

    public async ValueTask<ObservationAcquisitionResult> ObserveAsync(ObservationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Package != ScenarioA1SecondDefenderConsequencePackage.Identity
            || query.Kind.Value != QueryKind || query.Parameters.ValueKind != JsonValueKind.Object
            || query.Parameters.EnumerateObject().Count() != 5 || query.MaxResults != 1)
            return Empty(query, "asl.a1.second-defender-consequence.query-outside-exact-package");
        var unitId = Parameter(query.Parameters, "unitId");
        var locationId = Parameter(query.Parameters, "locationId");
        var previousId = Parameter(query.Parameters, "previousLocationId");
        var version = Parameter(query.Parameters, "observationVersion");
        var caseId = Parameter(query.Parameters, "caseId");
        if (unitId is null || locationId is null || previousId is null || version is null
            || caseId is null || !caseId.StartsWith("A1-second-defender-consequence-",
                StringComparison.Ordinal)
            || new[] { unitId, locationId, previousId }.Distinct().Count() != 3)
            return Empty(query, "asl.a1.second-defender-consequence.query-incomplete");
        var snapshot = await source.ReadAsync(query.TenantId, query.Package, unitId, locationId,
            cancellationToken);
        if (snapshot is null || snapshot.Package != query.Package
            || snapshot.PreviousLocationId != previousId
            || !Regex.IsMatch(previousId, "^bd01:[A-Z][0-9]+:0$",
                RegexOptions.CultureInvariant)
            || snapshot.IsPreviousLocationLastOccupied != true
            || snapshot.AttemptedEntryMf != 2 || snapshot.AttemptOrdinal is not > 0
            || snapshot.Eligibility.TenantId != query.TenantId
            || snapshot.Eligibility.Package != ScenarioA1SecondDefenderPackage.Identity
            || snapshot.Eligibility.UnitId != unitId
            || snapshot.Eligibility.LocationId != locationId
            || snapshot.Eligibility.Version != version
            || snapshot.Eligibility.FirstRevealOrdinal <= snapshot.AttemptOrdinal
            || snapshot.OvrEntryResolved != false
            || (snapshot.Eligibility.SecondRevealOrdinal is not null
                && snapshot.Eligibility.SecondRevealOrdinal <= snapshot.Eligibility.ElectionOrdinal))
            return Empty(query, "asl.a1.second-defender-consequence.attempt-or-previous-location-unreviewed");

        var eligibilityId = "A1-second-defender-" + caseId["A1-second-defender-consequence-".Length..];
        var eligibilityQuery = new ObservationQuery(query.QueryId + ":eligibility", query.TenantId,
            ScenarioA1SecondDefenderPackage.Identity,
            new SemanticIdentifier(new DomainId("asl"),
                ScenarioA1SecondDefenderObservationProvider.QueryKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId, locationId, observationVersion = version, caseId = eligibilityId,
            }), 1);
        var eligibilityProvider = new ScenarioA1SecondDefenderObservationProvider(
            new EligibilitySource(snapshot.Eligibility), terrain);
        var observations = (await eligibilityProvider.ObserveAsync(eligibilityQuery,
            cancellationToken)).Observations;
        if (observations.Count != 1)
            return Empty(query, "asl.a1.second-defender-consequence.eligibility-events-unreviewed");
        var facts = observations[0].Data.EnumerateObject().ToDictionary(item => item.Name,
            item => item.Value.GetString()!, StringComparer.Ordinal);
        facts.Remove("smcOutsideAfv");
        facts["a414ExceptionOtherThanInfantryOvr"] = facts["a414Exception"];
        facts.Remove("a414Exception");
        facts["previousLocation"] = "knownLastOccupiedLocation";
        facts["previousLocationId"] = previousId;
        facts["attemptedEntryMf"] = "ordinaryBuildingTwoMf";
        var observation = new Observation("asl-a1-second-defender-consequence:" + query.QueryId,
            new ObservationSource(snapshot.Eligibility.SourceId), query.TenantId,
            snapshot.Eligibility.ObservedAt, JsonSerializer.SerializeToElement(facts),
            locationId, version, snapshot.Eligibility.SourceId, query.Package);
        return new ObservationAcquisitionResult(query, [observation],
            ["asl.a1.second-defender-consequence.exact-case:" + caseId]);
    }

    private static ObservationAcquisitionResult Empty(ObservationQuery query, string reason) =>
        new(query, [], [reason]);

    private static string? Parameter(JsonElement parameters, string name) =>
        parameters.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;

    private sealed class EligibilitySource(ScenarioA1SecondDefenderSnapshot snapshot)
        : IScenarioA1SecondDefenderSnapshotSource
    {
        public ValueTask<ScenarioA1SecondDefenderSnapshot?> ReadAsync(Guid tenantId,
            DomainPackageRef package, string unitId, string locationId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScenarioA1SecondDefenderSnapshot?>(snapshot);
    }
}
