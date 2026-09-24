using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

public enum ScenarioA1BoardOccupancy
{
    Unknown,
    KnownEmpty,
    KnownUnconcealedEnemyMmc,
    KnownUnpinnedGoodOrderArmedEnemySquad,
    ExactlyOneKnownEnemySmc,
    KnownFriendlyOnly,
    ExactlyOneKnownUnconcealedEnemyMmc,
    ConcealedOrHidden,
    Other,
}

/// <summary>Optional, case-specific state variables supplied with a versioned snapshot.</summary>
public sealed record ScenarioA1AdditionalBoardState
{
    public bool? IsFortified { get; init; }
    public bool? HasNoBreachAtEntryHexsideAndLevel { get; init; }
    public bool? IsAdvancePhase { get; init; }
    public bool? IsGoodOrderInfantryMmc { get; init; }
    public bool? OwnNtcPassed { get; init; }
    public bool? IsSmcOutsideAfv { get; init; }
    public bool? HasNoOtherOccupants { get; init; }
    public bool? DefensiveResponseUnresolved { get; init; }
    public bool? HasAtLeastFourMf { get; init; }
    public bool? HasAtLeastThreeMf { get; init; }
    public int? FriendlySquads { get; init; }
    public int? FriendlyUnmannedCrewsOrHalfSquads { get; init; }
    public int? FriendlySmc { get; init; }
    public int? IncomingSquads { get; init; }
    public bool? HasNoVehicles { get; init; }
    public bool? HasNoMannedGun { get; init; }
    public bool? HasValidBreachAtCrossedHexsideAndLevel { get; init; }
    public bool? IsOneHorizontalHexSameLevel { get; init; }
    public bool? IsOneHorizontalHex { get; init; }
    public bool? CloseCombatUnresolved { get; init; }
    public bool? IsNotCx { get; init; }
    public bool? HasNoPortage { get; init; }
    public bool? IsGoodOrderUnpinnedInfantry { get; init; }
    public bool? IsGoodOrderUnpinnedInfantryWithoutTiOrCc { get; init; }
    public bool? IsNotDifficultTerrain { get; init; }
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
    ScenarioA1TerrainBinding? Terrain = null,
    ScenarioA1AdditionalBoardState? Additional = null);

public interface IScenarioA1BoardSnapshotSource
{
    ValueTask<ScenarioA1BoardSnapshot?> ReadAsync(Guid tenantId, DomainPackageRef package,
        string unitId, string locationId, CancellationToken cancellationToken = default);
}

/// <summary>Projects supplied state variables into reviewed exact-case observations.</summary>
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
        if (snapshot.IsMovementPhase == true && snapshot.Additional?.IsAdvancePhase == true)
            return Empty(query, "asl.a1.board.conflicting-phase");

        Dictionary<string, string>? facts = null;
        if (snapshot.Occupancy == ScenarioA1BoardOccupancy.KnownEmpty
            && snapshot.Additional is null
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
            && snapshot.Additional is null
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
        else if (snapshot is
        {
            Occupancy: ScenarioA1BoardOccupancy.KnownUnpinnedGoodOrderArmedEnemySquad,
            IsMovementPhase: true, IsKnownBuildingLocation: true,
            HasNoSpecialModifier: true, HasNoA414Exception: true,
            Additional: { IsFortified: true, HasNoBreachAtEntryHexsideAndLevel: true },
        })
        {
            if (snapshot.Additional != new ScenarioA1AdditionalBoardState
                { IsFortified = true, HasNoBreachAtEntryHexsideAndLevel = true })
                return Empty(query, "asl.a1.board.unreviewed-state-variable");
            facts = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["a414Exception"] = "none", ["breach"] = "noneAtEntryHexsideAndLevel",
                ["location"] = "knownFortifiedBuildingLocation",
                ["occupancy"] = "knownUnpinnedGoodOrderArmedEnemySquad",
                ["phase"] = "mph", ["specialModifier"] = "none", ["unit"] = "ordinaryInfantry",
            };
        }
        else if (snapshot is
        {
            Occupancy: ScenarioA1BoardOccupancy.ExactlyOneKnownEnemySmc,
            IsMovementPhase: true, CanMove: true,
            IsAdjacentGroundLevelOrdinaryBuilding: true,
            HasNoAdditionalTerrain: true, HasNoSpecialModifier: true,
            Additional:
            {
                IsGoodOrderInfantryMmc: true, OwnNtcPassed: true,
                IsSmcOutsideAfv: true, HasNoOtherOccupants: true,
                DefensiveResponseUnresolved: true, HasAtLeastFourMf: true,
            },
        })
        {
            if (snapshot.Additional != new ScenarioA1AdditionalBoardState
                {
                    IsGoodOrderInfantryMmc = true, OwnNtcPassed = true,
                    IsSmcOutsideAfv = true, HasNoOtherOccupants = true,
                    DefensiveResponseUnresolved = true, HasAtLeastFourMf = true,
                })
                return Empty(query, "asl.a1.board.unreviewed-state-variable");
            facts = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["canMove"] = "true", ["defensiveResponse"] = "unresolved",
                ["location"] = "adjacentGroundLevelOrdinaryBuilding",
                ["ntc"] = "passedByMovingMmc", ["occupancy"] = "exactlyOneKnownEnemySmc",
                ["otherOccupants"] = "none", ["phase"] = "mph",
                ["remainingMf"] = "atLeastFour",
                ["roadBypassElevationAdditionalTerrain"] = "none",
                ["smcInAfv"] = "false", ["specialModifier"] = "none",
                ["unit"] = "knownGoodOrderInfantryMmc",
            };
        }
        else if (snapshot is
        {
            Occupancy: ScenarioA1BoardOccupancy.KnownUnpinnedGoodOrderArmedEnemySquad,
            IsMovementPhase: false, IsKnownBuildingLocation: true,
            HasNoSpecialModifier: true,
            Additional:
            {
                IsFortified: true, IsAdvancePhase: true,
                HasValidBreachAtCrossedHexsideAndLevel: true,
                IsOneHorizontalHexSameLevel: true, HasNoOtherOccupants: true,
                CloseCombatUnresolved: true, IsNotCx: true, HasNoPortage: true,
                IsGoodOrderUnpinnedInfantry: true,
            },
        })
        {
            if (snapshot.Additional != new ScenarioA1AdditionalBoardState
                {
                    IsFortified = true, IsAdvancePhase = true,
                    HasValidBreachAtCrossedHexsideAndLevel = true,
                    IsOneHorizontalHexSameLevel = true, HasNoOtherOccupants = true,
                    CloseCombatUnresolved = true, IsNotCx = true, HasNoPortage = true,
                    IsGoodOrderUnpinnedInfantry = true,
                })
                return Empty(query, "asl.a1.board.unreviewed-state-variable");
            facts = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["advance"] = "oneHorizontalHexSameLevel",
                ["breach"] = "validCounterAtCrossedHexsideAndLevel",
                ["closeCombat"] = "unresolved", ["cx"] = "false",
                ["location"] = "fortifiedBuilding",
                ["occupancy"] = "exactlyOneKnownUnpinnedGoodOrderArmedEnemySquad",
                ["otherOccupants"] = "none", ["phase"] = "aph",
                ["portage"] = "none", ["specialModifier"] = "none",
                ["unit"] = "goodOrderUnpinnedInfantry",
            };
        }
        else if (snapshot is
        {
            Occupancy: ScenarioA1BoardOccupancy.KnownFriendlyOnly,
            IsMovementPhase: true, IsAdjacentGroundLevelOrdinaryBuilding: true,
            HasNoAdditionalTerrain: true, HasNoSpecialModifier: true,
            Additional:
            {
                HasAtLeastThreeMf: true, FriendlySquads: 2,
                FriendlyUnmannedCrewsOrHalfSquads: 1, FriendlySmc: 0,
                IncomingSquads: 1, HasNoVehicles: true, HasNoMannedGun: true,
            },
        })
        {
            if (snapshot.Additional != new ScenarioA1AdditionalBoardState
                {
                    HasAtLeastThreeMf = true, FriendlySquads = 2,
                    FriendlyUnmannedCrewsOrHalfSquads = 1, FriendlySmc = 0,
                    IncomingSquads = 1, HasNoVehicles = true, HasNoMannedGun = true,
                })
                return Empty(query, "asl.a1.board.unreviewed-state-variable");
            facts = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["friendlySmc"] = "0", ["friendlySquads"] = "2",
                ["friendlyUnmannedCrewsOrHalfSquads"] = "1", ["incomingSquads"] = "1",
                ["location"] = "adjacentGroundLevelOrdinaryBuilding",
                ["mannedGun"] = "none", ["occupancy"] = "knownFriendlyOnly",
                ["phase"] = "mph", ["remainingMf"] = "atLeastThree",
                ["roadBypassElevationAdditionalTerrain"] = "none",
                ["specialModifier"] = "none", ["unit"] = "ordinaryInfantrySquad",
                ["vehicles"] = "none",
            };
        }
        else if (snapshot is
        {
            Occupancy: ScenarioA1BoardOccupancy.ExactlyOneKnownUnconcealedEnemyMmc,
            IsMovementPhase: false, IsKnownBuildingLocation: true,
            HasNoSpecialModifier: true,
            Additional:
            {
                IsAdvancePhase: true, IsOneHorizontalHex: true,
                CloseCombatUnresolved: true, IsNotCx: true,
                IsNotDifficultTerrain: true, HasNoOtherOccupants: true,
                HasNoPortage: true, IsGoodOrderUnpinnedInfantryWithoutTiOrCc: true,
            },
        })
        {
            if (snapshot.Additional != new ScenarioA1AdditionalBoardState
                {
                    IsAdvancePhase = true, IsOneHorizontalHex = true,
                    CloseCombatUnresolved = true, IsNotCx = true,
                    IsNotDifficultTerrain = true, HasNoOtherOccupants = true,
                    HasNoPortage = true,
                    IsGoodOrderUnpinnedInfantryWithoutTiOrCc = true,
                })
                return Empty(query, "asl.a1.board.unreviewed-state-variable");
            facts = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["advance"] = "oneHorizontalHex", ["closeCombat"] = "unresolved",
                ["cx"] = "false", ["difficultTerrain"] = "false",
                ["location"] = "ordinaryBuilding",
                ["occupancy"] = "exactlyOneKnownUnconcealedEnemyMmc",
                ["otherOccupants"] = "none", ["phase"] = "aph",
                ["portage"] = "none", ["specialModifier"] = "none",
                ["unit"] = "goodOrderUnpinnedInfantryWithoutTiOrCc",
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
