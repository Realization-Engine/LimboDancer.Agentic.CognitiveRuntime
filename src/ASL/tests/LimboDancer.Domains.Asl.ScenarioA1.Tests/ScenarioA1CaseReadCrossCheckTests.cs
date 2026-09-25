using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// Cross-checks the unit read contract (ASL-UNIT-060) against the existing Scenario A1 snapshots, which stay unchanged
/// (ASL-MAP-081). A hook maps a case snapshot onto a Scenario A1 snapshot record: the facts the unit model supplies are
/// filled in, and the facts it does not (entry mode, rule exceptions, elections, special modifiers) stay null. The
/// records are compared with the reviewed cases' snapshots field by field. They are never given to a provider or
/// resolver: the fixture is synthetic and must not reach Scenario A1 adjudication (ASL-UNIT-050).
/// </summary>
public sealed class ScenarioA1CaseReadCrossCheckTests
{
    private const string Bd01Version = "8d77d26222b7bb21d8c1fdda6ba05b447f63c317";
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;

    private static readonly ScenarioA1TerrainBinding Terrain = new("01", Board01TerrainCatalog.BoardVersion, Board01TerrainCatalog.MetadataGitBlobSha, "E4", 0, null);

    /// <summary>The reviewed post-reveal baseline, as ScenarioA1PostRevealTests builds it.</summary>
    private static readonly ScenarioA1PostRevealSnapshot ReviewedPostReveal = new(Tenant, ScenarioA1PostRevealPackage.Identity, "squad", "bd01:E4:0",
        "bd01:D4:0", "snapshot-1", Now, "test-post-reveal-state", Terrain, ScenarioA1DefenderReveal.NonDummy, true, true, true, true, true, true, true);

    /// <summary>The reviewed concealed-SMC baseline, as ScenarioA1ConcealedSmcOverrunObservationTests builds it.</summary>
    private static readonly ScenarioA1ConcealedSmcOverrunSnapshot ReviewedConcealedSmc = new(
        Tenant, ScenarioA1ConcealedSmcOverrunPackage.Identity, "squad", "bd01:E4:0", "snapshot-1", Now, "test-supplied-state", Terrain,
        ScenarioA1InitialConcealedOccupancy.Concealed, true, ScenarioA1RevealedOccupant.EnemySmc, true, true, true, true, true, true, true, true,
        ScenarioA1OverrunElection.Elected, ScenarioA1OverrunNtc.Passed, ScenarioA1OverrunMf.AtLeastFour, ScenarioA1AdditionalDefenderReveal.None,
        null, true, ScenarioA1OverrunResponse.Unresolved, null);

    private static GameHistory History(int throughRevision)
    {
        var events = UnitGames.Read("a1-village.synthetic")!.Record!.Events.Take(throughRevision).ToArray();
        var history = GameProjector.Project(events, Vocabulary, [Catalog]);
        Assert.False(history.HasErrors);
        return history;
    }

    private static BoardHandle Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        HexFacts[] hexes = [.. geometry.Hexes().Select(index => new HexFacts(geometry.NameOf(index), index, 0, false, new LocationFacts(0, null, null),
            [new LocationFacts(0, null, null)], [], null, CenterTerrainSource.CenterSample))];
        return new BoardHandle(BoardRef.Parse("bd01"), Bd01Version, BoardReadStatus.Verified, "synthetic board for tests", new HexFactSet(geometry, "test", hexes));
    }

    private static CaseSnapshot Read(GameHistory history, string attacker, string at, string perspective = Perspective.AdjudicatorName)
    {
        var reader = new CaseReader(new HistoryGameSource([history]), new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog]);
        var result = reader.Read(new CaseRequest(history.Events[0].Scope, ScenarioA1PostRevealPackage.Identity.ToString(), attacker, BoardLocation.Parse(at),
            history.Current!.Revision, new Perspective(perspective)));
        Assert.Equal(CaseReadStatus.Definitive, result.Status);
        return result.Snapshot!;
    }

    private static bool? Known(bool value) => value;

    /// <summary>The hook for the post-reveal case: the facts a case read supplies, the rest null.</summary>
    private static ScenarioA1PostRevealSnapshot PostReveal(CaseSnapshot read)
    {
        var attacker = read.Attacker.Unit;
        var reveal = read.Reveals.Count == 0 ? ScenarioA1DefenderReveal.Unknown
            : read.Occupancy.Units.Any(unit => unit.Side != attacker.Side) ? ScenarioA1DefenderReveal.NonDummy
            : ScenarioA1DefenderReveal.Unknown;
        return new ScenarioA1PostRevealSnapshot(Tenant, ScenarioA1PostRevealPackage.Identity, attacker.Id, read.Target.Location.ToString(),
            read.Previous.Location.ToString(), $"r{read.Stamp.Revision}", read.Time, read.Source, Terrain, reveal,
            IsMovementPhase: Known(read.Phase == "mph"),
            IsOrdinaryInfantry: Known(Vocabulary.IsA(attacker.Kind, "asl:personnel")),
            IsAttackerUnconcealedNonDummy: Known(GameState.Condition(attacker, Conditions.Concealed) == ConditionState.False),
            IsObstacleEntryNotBypass: null, HasNoA414EntryException: null, HasNoOverrunElection: null, HasNoSpecialModifier: null);
    }

    /// <summary>The hook for the concealed-SMC case: occupancy, reveal, and attacker facts; elections, NTC, and responses null.</summary>
    private static ScenarioA1ConcealedSmcOverrunSnapshot ConcealedSmc(CaseSnapshot read, GameHistory history)
    {
        var attacker = read.Attacker.Unit;
        var (sole, _) = read.Occupancy.SoleEnemy(attacker.Side);
        var creation = sole is null ? null
            : history.Events.Select(item => item.Payload).OfType<InstanceCreated>().FirstOrDefault(created => created.Instance.Id == sole.Id)?.Instance;
        var initial = creation is null ? (ScenarioA1InitialConcealedOccupancy?)null
            : creation.Conditions.GetValueOrDefault(Conditions.Hidden) == ConditionState.True ? ScenarioA1InitialConcealedOccupancy.Hidden
            : creation.Conditions.GetValueOrDefault(Conditions.Concealed) == ConditionState.True ? ScenarioA1InitialConcealedOccupancy.Concealed
            : ScenarioA1InitialConcealedOccupancy.Other;
        var moveIds = read.Events.Where(item => item.Payload is InstanceMoved moved && moved.Id == attacker.Id).Select(item => item.EventId).ToHashSet();
        return new ScenarioA1ConcealedSmcOverrunSnapshot(Tenant, ScenarioA1ConcealedSmcOverrunPackage.Identity, attacker.Id, read.Target.Location.ToString(),
            $"r{read.Stamp.Revision}", read.Time, read.Source, Terrain,
            InitialOccupancy: initial,
            A1215ImmediateDefenderReveal: sole is null ? null : read.Reveals.Any(item => item.Causes.Any(moveIds.Contains)),
            RevealedOccupant: sole is null ? null : Vocabulary.IsA(sole.Kind, "asl:smc") ? ScenarioA1RevealedOccupant.EnemySmc : ScenarioA1RevealedOccupant.OtherNonDummy,
            IsSmcOutsideAfv: sole is null ? null : sole.Position is not ContainedPosition { Role: ContainmentRole.Passenger or ContainmentRole.Rider },
            IsMovementPhase: read.Phase == "mph",
            IsGoodOrderUnconcealedNonDummyInfantryMmc: Vocabulary.IsA(attacker.Kind, "asl:mmc") && read.Attacker.GoodOrder == ConditionState.True
                && GameState.Condition(attacker, Conditions.Concealed) == ConditionState.False,
            IsAdjacentGroundLevelOrdinaryBuilding: null, IsOrdinaryObstacleEntryNotBypass: null, HasNoA414Exception: null, HasNoOtherModifier: null,
            HasNoLeaderExemption: null, OverrunElection: null, Ntc: null, RemainingMf: null, AdditionalDefenderReveal: null, AdditionalDefenderType: null,
            SoleEnemySmcOccupancyVerified: sole is null ? null : true,
            DefenderResponseOrImmediateCc: null, ResponseOrCcOutcome: null);
    }

    [Fact]
    public void ThePostRevealReadSuppliesTheReviewedCasesUnitFacts()
    {
        // Revision 10: the German squad g1 in bd01:D4:0, in the MPh, after the Russian squad in bd01:E4:0 is revealed.
        var read = Read(History(10), "g1", "bd01:E4:0");
        var mapped = PostReveal(read);

        Assert.Equal(ReviewedPostReveal.LocationId, mapped.LocationId);
        Assert.Equal(ReviewedPostReveal.PreviousLocationId, mapped.PreviousLocationId);
        Assert.Equal(ReviewedPostReveal.DefenderReveal, mapped.DefenderReveal);
        Assert.Equal(ReviewedPostReveal.IsMovementPhase, mapped.IsMovementPhase);
        Assert.Equal(ReviewedPostReveal.IsOrdinaryInfantry, mapped.IsOrdinaryInfantry);
        Assert.Equal(ReviewedPostReveal.IsAttackerUnconcealedNonDummy, mapped.IsAttackerUnconcealedNonDummy);

        // What the unit model does not know stays unknown, so no reviewed case can be inherited from it.
        Assert.Null(mapped.IsObstacleEntryNotBypass);
        Assert.Null(mapped.HasNoA414EntryException);
        Assert.Null(mapped.HasNoOverrunElection);
        Assert.Null(mapped.HasNoSpecialModifier);
    }

    [Fact]
    public void BeforeTheRevealTheReadDoesNotClaimOne()
    {
        var mapped = PostReveal(Read(History(9), "g1", "bd01:E4:0"));
        Assert.Equal(ScenarioA1DefenderReveal.Unknown, mapped.DefenderReveal);
        Assert.NotEqual(ReviewedPostReveal.DefenderReveal, mapped.DefenderReveal);
    }

    [Fact]
    public void TheRevealedLeaderReadMatchesTheConcealedSmcCaseExceptItsInitialOccupancy()
    {
        // Revision 21: the German half-squad gh1 moved into bd01:D5:0, which revealed the hidden Russian leader in bd01:E5:0.
        var history = History(21);
        var mapped = ConcealedSmc(Read(history, "gh1", "bd01:E5:0"), history);

        Assert.Equal(ReviewedConcealedSmc.RevealedOccupant, mapped.RevealedOccupant);
        Assert.Equal(ReviewedConcealedSmc.A1215ImmediateDefenderReveal, mapped.A1215ImmediateDefenderReveal);
        Assert.Equal(ReviewedConcealedSmc.IsSmcOutsideAfv, mapped.IsSmcOutsideAfv);
        Assert.Equal(ReviewedConcealedSmc.IsMovementPhase, mapped.IsMovementPhase);
        Assert.Equal(ReviewedConcealedSmc.IsGoodOrderUnconcealedNonDummyInfantryMmc, mapped.IsGoodOrderUnconcealedNonDummyInfantryMmc);
        Assert.Equal(ReviewedConcealedSmc.SoleEnemySmcOccupancyVerified, mapped.SoleEnemySmcOccupancyVerified);

        // The leader started hidden, not concealed: the read tells the two apart, so this is not the reviewed case.
        Assert.Equal(ScenarioA1InitialConcealedOccupancy.Hidden, mapped.InitialOccupancy);
        Assert.NotEqual(ReviewedConcealedSmc.InitialOccupancy, mapped.InitialOccupancy);
        Assert.Null(mapped.OverrunElection);
        Assert.Null(mapped.Ntc);
        Assert.Null(mapped.RemainingMf);
    }

    [Fact]
    public void ASidesReadNeverVerifiesASoleEnemySmc()
    {
        var history = History(21);
        var mapped = ConcealedSmc(Read(history, "gh1", "bd01:E5:0", "german"), history);
        Assert.Null(mapped.SoleEnemySmcOccupancyVerified);
        Assert.Null(mapped.RevealedOccupant);
    }
}
