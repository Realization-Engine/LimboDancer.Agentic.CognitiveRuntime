using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// Inexperienced status and the MF allowance (Occupied and Concealed Entry Design, section 5). The Green and Conscript
/// definitions are synthetic: no printed counter values are asserted, only the class the status is derived from.
/// </summary>
public sealed class ExperienceTests
{
    private static readonly Lazy<UnitCatalog> Published = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    private static UnitCatalog Catalog()
    {
        var squad = Published.Value.Definition("attacker-squad")!;
        return Published.Value with
        {
            Definitions =
            [
                .. Published.Value.Definitions,
                squad with { Id = "green-squad", Class = "green" },
                squad with { Id = "conscript-squad", Class = "conscript" },
                squad with { Id = "unclassed-squad", Class = null },
                squad with { Id = "unknown-class-squad", Class = "veteran" },
            ]
        };
    }

    private static UnitInstance Unit(string id, string definition, string kind = "asl:squad", string at = "bd01:D4:0", bool? broken = false, string side = "german") =>
        new(id, kind, new DefinitionReference(Published.Value.Identity, definition), side, new MapPosition(BoardLocation.Parse(at)),
            broken is { } value ? new Dictionary<string, ConditionState> { [Conditions.Broken] = value ? ConditionState.True : ConditionState.False }
                : new Dictionary<string, ConditionState>(),
            InstanceStatus.Active, []);

    private static GameState State(params UnitInstance[] units)
    {
        var history = GameProjector.Project(UnitGames.Read("a1-village.synthetic")!.Record!.Events, UnitsTestData.Asl.Value, [Published.Value], new FakeChains());
        return history.Current! with
        {
            Units = units
        };
    }

    private static ConditionState Status(GameState state, string id) =>
        Experience.Inexperienced(state, state.Unit(id)!, [Catalog()], UnitsTestData.Asl.Value);

    [Theory]
    [InlineData("attacker-squad", ConditionState.False, 4)]
    [InlineData("green-squad", ConditionState.True, 3)]
    [InlineData("conscript-squad", ConditionState.True, 3)]
    [InlineData("unclassed-squad", ConditionState.Unknown, null)]
    [InlineData("unknown-class-squad", ConditionState.Unknown, null)]
    public void TheClassSetsTheStatusAndTheAllowance(string definition, ConditionState expected, int? allowance)
    {
        var state = State(Unit("g1", definition));
        Assert.Equal(expected, Status(state, "g1"));
        Assert.Equal(allowance, Experience.MfAllowance(state, state.Unit("g1")!, [Catalog()], UnitsTestData.Asl.Value));
    }

    [Fact]
    public void AGreenSquadStackedWithAnUnbrokenLeaderIsExempt()
    {
        Assert.Equal(ConditionState.False, Status(State(Unit("g1", "green-squad"), Unit("l1", "defender-leader", "asl:leader")), "g1"));
        Assert.Equal(ConditionState.True, Status(State(Unit("g1", "green-squad"), Unit("l1", "defender-leader", "asl:leader", broken: true)), "g1"));
        Assert.Equal(ConditionState.Unknown, Status(State(Unit("g1", "green-squad"), Unit("l1", "defender-leader", "asl:leader", broken: null)), "g1"));
    }

    [Fact]
    public void OnlyAFriendlyLeaderInTheSameLocationExemptsAGreenSquad()
    {
        Assert.Equal(ConditionState.True, Status(State(Unit("g1", "green-squad"), Unit("l1", "defender-leader", "asl:leader", at: "bd01:D5:0")), "g1"));
        Assert.Equal(ConditionState.True, Status(State(Unit("g1", "green-squad"), Unit("l1", "defender-leader", "asl:leader", side: "russian")), "g1"));
    }

    [Fact]
    public void AConscriptIsNeverExemptByALeader()
    {
        Assert.Equal(ConditionState.True, Status(State(Unit("c1", "conscript-squad"), Unit("l1", "defender-leader", "asl:leader")), "c1"));
    }

    [Fact]
    public void OnlyMmcAreInexperiencedAndADefinitionOutsideTheCatalogIsUnknown()
    {
        var state = State(Unit("l1", "defender-leader", "asl:leader"), Unit("g1", "attacker-squad"));
        Assert.Equal(ConditionState.Inapplicable, Status(state, "l1"));
        Assert.Null(Experience.MfAllowance(state, state.Unit("l1")!, [Catalog()], UnitsTestData.Asl.Value));
        Assert.Equal(ConditionState.Unknown, Experience.Inexperienced(state, state.Unit("g1")!, [], UnitsTestData.Asl.Value));
    }
}
