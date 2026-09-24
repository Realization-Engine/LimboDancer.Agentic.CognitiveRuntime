using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

public enum ScenarioA1SecondDefenderState
{
    Unknown,
    RevealedSmc,
    RevealedMmc,
    RevealedOtherNonDummy,
    RevealedUnknownType,
    KnownSmcNotRevealed,
}

/// <summary>Supplied event order and live state, independent of static VASL terrain.</summary>
public sealed record ScenarioA1SecondDefenderSnapshot(
    Guid TenantId, DomainPackageRef Package, string UnitId, string LocationId,
    string Version, DateTimeOffset ObservedAt, string SourceId,
    ScenarioA1TerrainBinding Terrain)
{
    public ScenarioA1InitialConcealedOccupancy? InitialOccupancy { get; init; }
    public bool? A1215FirstRevealOccurred { get; init; }
    public string? FirstRevealedUnitId { get; init; }
    public int? FirstRevealOrdinal { get; init; }
    public ScenarioA1RevealedOccupant? FirstRevealedType { get; init; }
    public ScenarioA1OverrunElection? OverrunElection { get; init; }
    public int? ElectionOrdinal { get; init; }
    public string? SecondDefenderUnitId { get; init; }
    public string? SecondDefenderLocationId { get; init; }
    public ScenarioA1SecondDefenderState? SecondDefenderState { get; init; }
    public int? SecondRevealOrdinal { get; init; }
    public ScenarioA1OverrunNtc? Ntc { get; init; }
    public int? NtcOrdinal { get; init; }
    public ScenarioA1OverrunMf? MfAtSecondReveal { get; init; }
    public bool? IsMovementPhase { get; init; }
    public bool? IsGoodOrderUnconcealedNonDummyInfantryMmc { get; init; }
    public bool? IsAdjacentGroundLevelOrdinaryBuilding { get; init; }
    public bool? IsOrdinaryObstacleEntryNotBypass { get; init; }
    public bool? HasNoA414Exception { get; init; }
    public bool? IsFirstSmcOutsideAfv { get; init; }
    public bool? HasNoSpecialModifier { get; init; }
    public bool? HasNoLeaderExemption { get; init; }
}

public interface IScenarioA1SecondDefenderSnapshotSource
{
    ValueTask<ScenarioA1SecondDefenderSnapshot?> ReadAsync(Guid tenantId,
        DomainPackageRef package, string unitId, string locationId,
        CancellationToken cancellationToken = default);
}

/// <summary>Projects only exact, ordered supplied events into a pinned eligibility observation.</summary>
public sealed class ScenarioA1SecondDefenderObservationProvider(
    IScenarioA1SecondDefenderSnapshotSource source, Board01TerrainCatalog terrain)
    : IObservationProvider
{
    public const string QueryKind = "scenario-a1-second-defender-location";

    public async ValueTask<ObservationAcquisitionResult> ObserveAsync(ObservationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (query.Package != ScenarioA1SecondDefenderPackage.Identity
            || query.Kind.Value != QueryKind || query.Parameters.ValueKind != JsonValueKind.Object
            || query.Parameters.EnumerateObject().Count() != 4 || query.MaxResults != 1)
            return Empty(query, "asl.a1.second-defender.query-outside-exact-package");
        var unitId = Parameter(query.Parameters, "unitId");
        var locationId = Parameter(query.Parameters, "locationId");
        var version = Parameter(query.Parameters, "observationVersion");
        var expectedCaseId = Parameter(query.Parameters, "caseId");
        if (unitId is null || locationId is null || version is null || expectedCaseId is null
            || unitId == locationId)
            return Empty(query, "asl.a1.second-defender.query-incomplete");
        var snapshot = await source.ReadAsync(query.TenantId, query.Package, unitId, locationId,
            cancellationToken);
        if (snapshot is null || snapshot.TenantId != query.TenantId
            || snapshot.Package != query.Package || snapshot.UnitId != unitId
            || snapshot.LocationId != locationId || snapshot.Version != version
            || snapshot.ObservedAt.Offset != TimeSpan.Zero
            || string.IsNullOrWhiteSpace(snapshot.SourceId)
            || !terrain.IsSupportedGroundLevel(snapshot.Terrain)
            || locationId != "bd01:" + snapshot.Terrain.Hex + ":0")
            return Empty(query, "asl.a1.second-defender.snapshot-scope-version-or-terrain-mismatch");
        if (snapshot.InitialOccupancy != ScenarioA1InitialConcealedOccupancy.Concealed
            || snapshot.A1215FirstRevealOccurred != true
            || snapshot.FirstRevealedType != ScenarioA1RevealedOccupant.EnemySmc
            || string.IsNullOrWhiteSpace(snapshot.FirstRevealedUnitId)
            || string.IsNullOrWhiteSpace(snapshot.SecondDefenderUnitId)
            || snapshot.FirstRevealedUnitId == snapshot.SecondDefenderUnitId
            || snapshot.SecondDefenderLocationId != locationId
            || snapshot.OverrunElection != ScenarioA1OverrunElection.Elected
            || snapshot.FirstRevealOrdinal is not > 0
            || snapshot.ElectionOrdinal is not > 0
            || snapshot.ElectionOrdinal <= snapshot.FirstRevealOrdinal
            || snapshot.IsMovementPhase != true
            || snapshot.IsGoodOrderUnconcealedNonDummyInfantryMmc != true
            || snapshot.IsAdjacentGroundLevelOrdinaryBuilding != true
            || snapshot.IsOrdinaryObstacleEntryNotBypass != true
            || snapshot.HasNoA414Exception != true
            || snapshot.IsFirstSmcOutsideAfv != true
            || snapshot.HasNoSpecialModifier != true
            || snapshot.HasNoLeaderExemption != true)
            return Empty(query, "asl.a1.second-defender.first-reveal-or-election-incomplete");
        var reveal = Reveal(snapshot);
        var capability = Capability(snapshot);
        if (reveal is null || capability is null)
            return Empty(query, "asl.a1.second-defender.reveal-or-capability-unreviewed");
        var suffix = (reveal, capability) switch
        {
            ("revealedEnemySmc-after-election", "passedNtcAndAtLeastFourMf") => "smc-revealed",
            ("revealedEnemyMmc-after-election", "passedNtcAndAtLeastFourMf") => "mmc-revealed",
            ("revealedOtherNonDummy-after-election", "passedNtcAndAtLeastFourMf") => "other-type",
            ("revealedUnknownType-after-election", "passedNtcAndAtLeastFourMf") => "unknown-type",
            ("secondSmcKnownButNotRevealed", "passedNtcAndAtLeastFourMf") => "unrevealed-smc",
            ("revealedEnemySmc-after-election", "ntcOrMfUnresolved") => "capability-unresolved",
            ("revealedEnemySmc-after-election", "mfInsufficient") => "mf-insufficient",
            _ => null,
        };
        if (suffix is null || expectedCaseId != "A1-second-defender-" + suffix)
            return Empty(query, "asl.a1.second-defender.requested-case-does-not-match-state");
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["board"] = "bd01-ground-level-ordinary-building",
            ["phase"] = "mph", ["attacker"] = "goodOrderUnconcealedNonDummyInfantryMmc",
            ["initialDefenderState"] = "concealed", ["firstReveal"] = "enemySmc-under-A12.15",
            ["overrunElection"] = "elected-after-first-reveal",
            ["entryMode"] = "ordinaryObstacleEntryNotBypass", ["a414Exception"] = "none",
            ["smcOutsideAfv"] = "true", ["specialModifier"] = "none",
            ["leaderExemption"] = "none", ["secondReveal"] = reveal,
            ["attackerCapability"] = capability,
        };
        var observation = new Observation("asl-a1-second-defender:" + query.QueryId,
            new ObservationSource(snapshot.SourceId), query.TenantId, snapshot.ObservedAt,
            JsonSerializer.SerializeToElement(facts), locationId, snapshot.Version,
            snapshot.SourceId, query.Package);
        return new ObservationAcquisitionResult(query, [observation],
            ["asl.a1.second-defender.exact-case:" + expectedCaseId]);
    }

    private static string? Reveal(ScenarioA1SecondDefenderSnapshot state)
    {
        if (state.SecondDefenderState == ScenarioA1SecondDefenderState.KnownSmcNotRevealed)
            return state.SecondRevealOrdinal is null ? "secondSmcKnownButNotRevealed" : null;
        if (state.SecondRevealOrdinal is not > 0
            || state.SecondRevealOrdinal <= state.ElectionOrdinal)
            return null;
        return state.SecondDefenderState switch
        {
            ScenarioA1SecondDefenderState.RevealedSmc => "revealedEnemySmc-after-election",
            ScenarioA1SecondDefenderState.RevealedMmc => "revealedEnemyMmc-after-election",
            ScenarioA1SecondDefenderState.RevealedOtherNonDummy =>
                "revealedOtherNonDummy-after-election",
            ScenarioA1SecondDefenderState.RevealedUnknownType =>
                "revealedUnknownType-after-election",
            _ => null,
        };
    }

    private static string? Capability(ScenarioA1SecondDefenderSnapshot state)
    {
        if (state.Ntc == ScenarioA1OverrunNtc.Unresolved && state.NtcOrdinal is null
            && state.MfAtSecondReveal is ScenarioA1OverrunMf.AtLeastFour or ScenarioA1OverrunMf.Unknown)
            return "ntcOrMfUnresolved";
        if (state.Ntc != ScenarioA1OverrunNtc.Passed || state.NtcOrdinal is not > 0)
            return null;
        // A later check cannot establish capability at the earlier reveal decision.
        if (state.SecondRevealOrdinal is not null
            && state.NtcOrdinal >= state.SecondRevealOrdinal)
            return null;
        if (state.NtcOrdinal == state.FirstRevealOrdinal
            || state.NtcOrdinal == state.ElectionOrdinal
            || state.NtcOrdinal == state.SecondRevealOrdinal)
            return null;
        return state.MfAtSecondReveal switch
        {
            ScenarioA1OverrunMf.AtLeastFour => "passedNtcAndAtLeastFourMf",
            ScenarioA1OverrunMf.Unknown => "ntcOrMfUnresolved",
            ScenarioA1OverrunMf.Insufficient => "mfInsufficient",
            _ => null,
        };
    }

    private static ObservationAcquisitionResult Empty(ObservationQuery query, string reason) =>
        new(query, [], [reason]);

    private static string? Parameter(JsonElement parameters, string name) =>
        parameters.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;
}
