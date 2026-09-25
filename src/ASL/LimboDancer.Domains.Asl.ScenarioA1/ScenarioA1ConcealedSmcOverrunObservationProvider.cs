using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

public enum ScenarioA1InitialConcealedOccupancy { Unknown, Concealed, Hidden, Other }
public enum ScenarioA1RevealedOccupant { Unknown, EnemySmc, OtherNonDummy, DummiesOnly }
public enum ScenarioA1OverrunElection { Unknown, Elected, Declined }
public enum ScenarioA1OverrunNtc { Unresolved, Passed, Failed }
public enum ScenarioA1OverrunMf { Unknown, AtLeastFour, Insufficient }
public enum ScenarioA1AdditionalDefenderReveal { Unresolved, None, AnotherNonDummy }
public enum ScenarioA1AdditionalDefenderType { Unknown, Smc, Mmc, OtherNonDummy }
public enum ScenarioA1OverrunResponse { Unknown, Unresolved, ResolvedWithOutcome }

/// <summary>Explicit, versioned live state; board metadata supplies terrain alone.</summary>
public sealed record ScenarioA1ConcealedSmcOverrunSnapshot(
    Guid TenantId, DomainPackageRef Package, string UnitId, string LocationId,
    string Version, DateTimeOffset ObservedAt, string SourceId,
    ScenarioA1TerrainBinding Terrain,
    ScenarioA1InitialConcealedOccupancy? InitialOccupancy,
    bool? A1215ImmediateDefenderReveal,
    ScenarioA1RevealedOccupant? RevealedOccupant,
    bool? IsSmcOutsideAfv,
    bool? IsMovementPhase,
    bool? IsGoodOrderUnconcealedNonDummyInfantryMmc,
    bool? IsAdjacentGroundLevelOrdinaryBuilding,
    bool? IsOrdinaryObstacleEntryNotBypass,
    bool? HasNoA414Exception,
    bool? HasNoOtherModifier,
    bool? HasNoLeaderExemption,
    ScenarioA1OverrunElection? OverrunElection,
    ScenarioA1OverrunNtc? Ntc,
    ScenarioA1OverrunMf? RemainingMf,
    ScenarioA1AdditionalDefenderReveal? AdditionalDefenderReveal,
    ScenarioA1AdditionalDefenderType? AdditionalDefenderType,
    bool? SoleEnemySmcOccupancyVerified,
    ScenarioA1OverrunResponse? DefenderResponseOrImmediateCc,
    string? ResponseOrCcOutcome);

public interface IScenarioA1ConcealedSmcOverrunSnapshotSource
{
    ValueTask<ScenarioA1ConcealedSmcOverrunSnapshot?> ReadAsync(Guid tenantId,
        DomainPackageRef package, string unitId, string locationId,
        CancellationToken cancellationToken = default);
}

/// <summary>Projects a single exact reviewed case from supplied state after board terrain validation.</summary>
public sealed class ScenarioA1ConcealedSmcOverrunObservationProvider(
    IScenarioA1ConcealedSmcOverrunSnapshotSource source, IScenarioA1TerrainEvidence terrain)
    : IObservationProvider
{
    public const string QueryKind = "scenario-a1-concealed-smc-overrun-location";

    public async ValueTask<ObservationAcquisitionResult> ObserveAsync(ObservationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Package != ScenarioA1ConcealedSmcOverrunPackage.Identity
            || query.Kind.Value != QueryKind || query.Parameters.ValueKind != JsonValueKind.Object
            || query.Parameters.EnumerateObject().Count() != 4 || query.MaxResults != 1)
            return Empty(query, "asl.a1.ovr.query-outside-exact-package");
        var unitId = Parameter(query.Parameters, "unitId");
        var locationId = Parameter(query.Parameters, "locationId");
        var version = Parameter(query.Parameters, "observationVersion");
        var expectedCaseId = Parameter(query.Parameters, "caseId");
        if (unitId is null || locationId is null || version is null || expectedCaseId is null
            || unitId == locationId)
            return Empty(query, "asl.a1.ovr.query-incomplete");
        var snapshot = await source.ReadAsync(query.TenantId, query.Package, unitId, locationId,
            cancellationToken);
        if (snapshot is null || snapshot.TenantId != query.TenantId
            || snapshot.Package != query.Package || snapshot.UnitId != unitId
            || snapshot.LocationId != locationId || snapshot.Version != version
            || snapshot.ObservedAt.Offset != TimeSpan.Zero
            || string.IsNullOrWhiteSpace(snapshot.SourceId)
            || !terrain.IsSupportedGroundLevel(snapshot.Terrain)
            || locationId != "bd01:" + snapshot.Terrain.Hex + ":0")
            return Empty(query, "asl.a1.ovr.snapshot-scope-version-or-terrain-mismatch");
        if (snapshot.InitialOccupancy != ScenarioA1InitialConcealedOccupancy.Concealed
            || snapshot.A1215ImmediateDefenderReveal != true
            || snapshot.RevealedOccupant != ScenarioA1RevealedOccupant.EnemySmc
            || snapshot.IsSmcOutsideAfv != true
            || snapshot.IsMovementPhase != true
            || snapshot.IsGoodOrderUnconcealedNonDummyInfantryMmc != true
            || snapshot.IsAdjacentGroundLevelOrdinaryBuilding != true
            || snapshot.IsOrdinaryObstacleEntryNotBypass != true
            || snapshot.HasNoA414Exception != true
            || snapshot.HasNoOtherModifier != true
            || snapshot.HasNoLeaderExemption != true)
            return Empty(query, "asl.a1.ovr.reveal-or-attacker-state-incomplete");

        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["board"] = "bd01-ground-level-ordinary-building",
            ["phase"] = "mph", ["attacker"] = "goodOrderUnconcealedNonDummyInfantryMmc",
            ["entryMode"] = "ordinaryObstacleEntryNotBypass", ["a414Exception"] = "none",
            ["initialDefenderState"] = "concealed",
            ["revealProvenance"] = "A12.15-immediate-defender-reveal",
            ["revealedOccupant"] = "oneEnemySmc", ["otherModifier"] = "none",
            ["leaderExemption"] = "none", ["entryMfCost"] = "2",
        };
        var caseSuffix = ProjectContinuation(snapshot, facts);
        if (caseSuffix is null)
            return Empty(query, "asl.a1.ovr.controlling-state-incomplete-or-conflicting");
        var caseId = "A1-concealed-smc-" + caseSuffix;
        if (caseId != expectedCaseId)
            return Empty(query, "asl.a1.ovr.requested-case-does-not-match-state");
        var observation = new Observation("asl-a1-ovr:" + query.QueryId,
            new ObservationSource(snapshot.SourceId), query.TenantId, snapshot.ObservedAt,
            JsonSerializer.SerializeToElement(facts), locationId, snapshot.Version,
            snapshot.SourceId, query.Package);
        return new ObservationAcquisitionResult(query, [observation],
            ["asl.a1.ovr.exact-case:" + caseId]);
    }

    private static string? ProjectContinuation(ScenarioA1ConcealedSmcOverrunSnapshot state,
        Dictionary<string, string> facts)
    {
        if (state.OverrunElection is null)
            return null;
        if (state.OverrunElection is ScenarioA1OverrunElection.Unknown
            or ScenarioA1OverrunElection.Declined)
        {
            if (!NoLaterFacts(state)) return null;
            facts["overrunElection"] = state.OverrunElection == ScenarioA1OverrunElection.Unknown
                ? "unknown" : "declined";
            return state.OverrunElection == ScenarioA1OverrunElection.Unknown
                ? "election-unknown" : "declined";
        }
        facts["overrunElection"] = "elected";
        if (state.Ntc is null) return null;
        if (state.Ntc is ScenarioA1OverrunNtc.Unresolved or ScenarioA1OverrunNtc.Failed)
        {
            if (!NoAfterNtcFacts(state)) return null;
            facts["ntc"] = state.Ntc == ScenarioA1OverrunNtc.Unresolved ? "unresolved" : "failed";
            return state.Ntc == ScenarioA1OverrunNtc.Unresolved ? "ntc-unresolved" : "ntc-failed";
        }
        facts["ntc"] = "passed";
        if (state.RemainingMf is null) return null;
        if (state.RemainingMf is ScenarioA1OverrunMf.Unknown or ScenarioA1OverrunMf.Insufficient)
        {
            if (!NoAfterMfFacts(state)) return null;
            facts["remainingMf"] = state.RemainingMf == ScenarioA1OverrunMf.Unknown
                ? "unknown" : "insufficient";
            return state.RemainingMf == ScenarioA1OverrunMf.Unknown ? "mf-unknown" : "mf-insufficient";
        }
        facts["remainingMf"] = "atLeastFour";
        if (state.AdditionalDefenderReveal is null) return null;
        if (state.AdditionalDefenderReveal == ScenarioA1AdditionalDefenderReveal.Unresolved)
        {
            if (state.AdditionalDefenderType is not null || state.SoleEnemySmcOccupancyVerified is not null
                || state.DefenderResponseOrImmediateCc is not null || state.ResponseOrCcOutcome is not null)
                return null;
            facts["additionalDefenderReveal"] = "unresolved";
            return "other-reveal-unresolved";
        }
        if (state.AdditionalDefenderReveal == ScenarioA1AdditionalDefenderReveal.AnotherNonDummy)
        {
            if (state.AdditionalDefenderType is null or ScenarioA1AdditionalDefenderType.Unknown
                || state.SoleEnemySmcOccupancyVerified != false
                || state.DefenderResponseOrImmediateCc is not null || state.ResponseOrCcOutcome is not null)
                return null;
            facts["additionalDefenderReveal"] = "anotherNonDummyRevealed";
            return "another-defender-revealed";
        }
        if (state.AdditionalDefenderType is not null
            || state.SoleEnemySmcOccupancyVerified != true
            || state.DefenderResponseOrImmediateCc is null) return null;
        facts["additionalDefenderReveal"] = "none";
        facts["soleEnemySmcOccupancy"] = "verifiedBySuppliedState";
        if (state.DefenderResponseOrImmediateCc == ScenarioA1OverrunResponse.Unresolved
            && state.ResponseOrCcOutcome is null)
        {
            facts["defenderResponseOrImmediateCc"] = "unresolved";
            return "qualified-response-unresolved";
        }
        if (state.DefenderResponseOrImmediateCc == ScenarioA1OverrunResponse.ResolvedWithOutcome
            && !string.IsNullOrWhiteSpace(state.ResponseOrCcOutcome))
        {
            facts["defenderResponseOrImmediateCc"] = "resolvedWithOutcome";
            return "response-resolved";
        }
        return null;
    }

    private static bool NoLaterFacts(ScenarioA1ConcealedSmcOverrunSnapshot state) =>
        state.Ntc is null && NoAfterNtcFacts(state);

    private static bool NoAfterNtcFacts(ScenarioA1ConcealedSmcOverrunSnapshot state) =>
        state.RemainingMf is null && NoAfterMfFacts(state);

    private static bool NoAfterMfFacts(ScenarioA1ConcealedSmcOverrunSnapshot state) =>
        state.AdditionalDefenderReveal is null && state.AdditionalDefenderType is null
        && state.SoleEnemySmcOccupancyVerified is null
        && state.DefenderResponseOrImmediateCc is null && state.ResponseOrCcOutcome is null;

    private static ObservationAcquisitionResult Empty(ObservationQuery query, string reason) =>
        new(query, [], [reason]);

    private static string? Parameter(JsonElement parameters, string name) =>
        parameters.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;
}
