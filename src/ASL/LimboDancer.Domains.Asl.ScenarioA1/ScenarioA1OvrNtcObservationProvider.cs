using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

public enum ScenarioA1OvrElection { Unknown, Requested, Elected }
public enum ScenarioA1OvrRemainingMf { Unknown, AtLeastFour, BelowFour }
public enum ScenarioA1OvrNtcResult { Unknown, Passed, Failed }
public enum ScenarioA1OtherConcealedNonDummy { Unknown, Present, None }

/// <summary>
/// Supplied state for an elected Infantry OVR after a lone concealed SMC reveal (unit step 10): the attacker and the
/// first reveal, the election, the MF left, the NTC, and whether another concealed non-Dummy unit is present and, after
/// a passed NTC, revealed by Random Selection.
/// </summary>
public sealed record ScenarioA1OvrNtcSnapshot(
    Guid TenantId, DomainPackageRef Package, string UnitId, string LocationId, string PreviousLocationId,
    string Version, DateTimeOffset ObservedAt, string SourceId, ScenarioA1TerrainBinding Terrain,
    bool? IsMovementPhase, bool? IsGoodOrderUnconcealedNonDummyInfantryMmcMovingAlone, bool? InitialOccupancyConcealed,
    bool? FirstRevealLoneSmcUnderA1215, bool? IsOrdinaryObstacleEntryNotBypass, bool? HasNoA414Exception,
    bool? HasNoSpecialModifier, bool? HasNoLeaderExemption,
    ScenarioA1OvrElection Election, ScenarioA1OvrRemainingMf RemainingMf, ScenarioA1OvrNtcResult? Ntc,
    ScenarioA1OtherConcealedNonDummy? OtherConcealedNonDummy, bool? SecondRevealRevealedNonDummy);

public interface IScenarioA1OvrNtcSnapshotSource
{
    ValueTask<ScenarioA1OvrNtcSnapshot?> ReadAsync(Guid tenantId, DomainPackageRef package, string unitId, string locationId,
        CancellationToken cancellationToken = default);
}

/// <summary>Projects supplied state into exactly one reviewed OVR NTC case, after board 01 terrain validation.</summary>
public sealed class ScenarioA1OvrNtcObservationProvider(IScenarioA1OvrNtcSnapshotSource source, IScenarioA1TerrainEvidence terrain) : IObservationProvider
{
    public const string QueryKind = "scenario-a1-ovr-ntc-location";

    public async ValueTask<ObservationAcquisitionResult> ObserveAsync(ObservationQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Package != ScenarioA1OvrNtcPackage.Identity || query.Kind.Value != QueryKind || query.Parameters.ValueKind != JsonValueKind.Object
            || query.Parameters.EnumerateObject().Count() != 3 || query.MaxResults != 1)
        {
            return Empty(query, "asl.a1.ovr-ntc.query-outside-exact-package");
        }

        var unitId = Parameter(query.Parameters, "unitId");
        var locationId = Parameter(query.Parameters, "locationId");
        var version = Parameter(query.Parameters, "observationVersion");
        if (unitId is null || locationId is null || version is null || unitId == locationId)
        {
            return Empty(query, "asl.a1.ovr-ntc.query-incomplete");
        }

        var snapshot = await source.ReadAsync(query.TenantId, query.Package, unitId, locationId, cancellationToken);
        if (snapshot is null || snapshot.TenantId != query.TenantId || snapshot.Package != query.Package || snapshot.UnitId != unitId
            || snapshot.LocationId != locationId || snapshot.Version != version || snapshot.ObservedAt.Offset != TimeSpan.Zero
            || string.IsNullOrWhiteSpace(snapshot.SourceId) || string.IsNullOrWhiteSpace(snapshot.PreviousLocationId)
            || snapshot.PreviousLocationId == locationId || !terrain.IsSupportedGroundLevel(snapshot.Terrain)
            || locationId != "bd01:" + snapshot.Terrain.Hex + ":0")
        {
            return Empty(query, "asl.a1.ovr-ntc.snapshot-scope-version-or-terrain-mismatch");
        }

        if (snapshot.IsMovementPhase != true || snapshot.IsGoodOrderUnconcealedNonDummyInfantryMmcMovingAlone != true
            || snapshot.InitialOccupancyConcealed != true || snapshot.FirstRevealLoneSmcUnderA1215 != true
            || snapshot.IsOrdinaryObstacleEntryNotBypass != true || snapshot.HasNoA414Exception != true
            || snapshot.HasNoSpecialModifier != true || snapshot.HasNoLeaderExemption != true)
        {
            return Empty(query, "asl.a1.ovr-ntc.attacker-or-reveal-state-incomplete");
        }

        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["board"] = "bd01-ground-level-ordinary-building",
            ["phase"] = "mph",
            ["attacker"] = "goodOrderUnconcealedNonDummyInfantryMmc-moving-alone",
            ["initialDefenderState"] = "concealed",
            ["firstReveal"] = "oneEnemySmc-under-A12.15",
            ["entryMode"] = "ordinaryObstacleEntryNotBypass",
            ["a414Exception"] = "none",
            ["specialModifier"] = "none",
            ["leaderExemption"] = "none",
            ["previousLocationId"] = snapshot.PreviousLocationId,
        };
        var caseSuffix = Continuation(snapshot, facts);
        if (caseSuffix is null)
        {
            return Empty(query, "asl.a1.ovr-ntc.state-outside-reviewed-cases");
        }

        var observation = new Observation("asl-a1-ovr-ntc:" + query.QueryId, new ObservationSource(snapshot.SourceId), query.TenantId, snapshot.ObservedAt,
            JsonSerializer.SerializeToElement(facts), locationId, snapshot.Version, snapshot.SourceId, query.Package);
        return new ObservationAcquisitionResult(query, [observation], ["asl.a1.ovr-ntc.exact-case:A1-ovr-ntc-" + caseSuffix]);
    }

    /// <summary>The case facts, in the order the review decides them: MF, then the NTC, then the second defender.</summary>
    private static string? Continuation(ScenarioA1OvrNtcSnapshot state, Dictionary<string, string> facts)
    {
        if (state.Election == ScenarioA1OvrElection.Requested && state.RemainingMf == ScenarioA1OvrRemainingMf.BelowFour
            && state.Ntc is null && state.OtherConcealedNonDummy is null && state.SecondRevealRevealedNonDummy is null)
        {
            facts["election"] = "requested";
            facts["remainingMf"] = "belowFour";
            return "mf-insufficient";
        }

        if (state.Election != ScenarioA1OvrElection.Elected || state.RemainingMf != ScenarioA1OvrRemainingMf.AtLeastFour)
        {
            return null;
        }

        facts["election"] = "elected";
        facts["remainingMf"] = "atLeastFour";
        switch (state.Ntc)
        {
            case ScenarioA1OvrNtcResult.Failed when state.OtherConcealedNonDummy is null && state.SecondRevealRevealedNonDummy is null:
                facts["ntc"] = "failed";
                return "failed";
            case ScenarioA1OvrNtcResult.Passed:
                facts["ntc"] = "passed";
                switch (state.OtherConcealedNonDummy)
                {
                    case ScenarioA1OtherConcealedNonDummy.Present when state.SecondRevealRevealedNonDummy == true:
                        facts["otherConcealedNonDummy"] = "present";
                        facts["secondReveal"] = "randomSelection-revealed-nonDummy";
                        return "passed-second-defender-revealed";
                    case ScenarioA1OtherConcealedNonDummy.None when state.SecondRevealRevealedNonDummy is null:
                        facts["otherConcealedNonDummy"] = "none";
                        return "passed-against-lone-smc";
                    case ScenarioA1OtherConcealedNonDummy.Unknown when state.SecondRevealRevealedNonDummy is null:
                        facts["otherConcealedNonDummy"] = "unknown";
                        return "passed-other-occupants-unknown";
                    default:
                        return null;
                }

            default:
                return null;
        }
    }

    private static ObservationAcquisitionResult Empty(ObservationQuery query, string reason) => new(query, [], [reason]);

    private static string? Parameter(JsonElement parameters, string key) =>
        parameters.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()
            : null;
}
