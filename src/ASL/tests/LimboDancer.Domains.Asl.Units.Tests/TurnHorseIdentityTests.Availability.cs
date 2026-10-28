using System.Text.Json;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Catalog;

namespace LimboDancer.Domains.Asl.Units.Tests;

public sealed partial class TurnHorseIdentityTests
{
    [Fact]
    public void OwnerAndAdjudicatorCanProveAnEmptyMuleWithoutInventingMissingWorkFacts()
    {
        var state = PackingState();
        var before = JsonSerializer.Serialize(state);
        foreach (var perspective in new[] { Perspective.Side("german"), Perspective.Adjudicator })
        {
            var read = ReadActors(state, perspective: perspective);
            Assert.True(read.Facts.MuleAvailableForSubject);
            Assert.Null(read.LoadedOnMule);
            Assert.Null(read.Facts.FirstRallyWorkWindow);
            Assert.Null(read.Facts.SameLocationAndTerrain);
            Assert.Equal(Rules.TurnPackingEligibility.NotEvaluated, read.Verdict.State);
        }
        Assert.Equal(before, JsonSerializer.Serialize(state));
    }

    [Fact]
    public void EnemyEmptyAndHiddenLoadWorldsHaveTheSameUnknownAvailability()
    {
        var state = PackingState() with
        {
            SetupClosed = true
        };
        var hidden = state.Equipment[0] with
        {
            Id = "hidden-load",
            Holding = null,
            Position = new ContainedPosition("mule", ContainmentRole.AnimalPack),
            Conditions = new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.True }
        };
        var loaded = state with
        {
            Equipment = [.. state.Equipment, hidden]
        };
        var emptyView = ReadActors(state, perspective: Perspective.Side("russian"));
        var loadedView = ReadActors(loaded, perspective: Perspective.Side("russian"));
        Assert.Equal(emptyView, loadedView);
        Assert.Null(emptyView.Facts.MuleAvailableForSubject);
        Assert.True(ReadActors(state).Facts.MuleAvailableForSubject);
        Assert.False(ReadActors(loaded).Facts.MuleAvailableForSubject);
        Assert.False(ReadActors(loaded, perspective: Perspective.Adjudicator).Facts.MuleAvailableForSubject);
    }

    [Fact]
    public void UnboundOwnLegacyPackingPreventsAnEmptyClaimButForeignRecordsDoNot()
    {
        var state = PackingState();
        var legacy = state with
        {
            PackedGuns = ["gun"]
        };
        Assert.Null(ReadActors(legacy).Facts.MuleAvailableForSubject);
        Assert.Null(ReadActors(legacy, perspective: Perspective.Adjudicator).Facts.MuleAvailableForSubject);
        var other = state.Equipment[0] with
        {
            Id = "foreign-gun",
            Side = "russian",
            Holding = null
        };
        var foreign = state with
        {
            Equipment = [.. state.Equipment, other],
            PackedGuns = [other.Id]
        };
        Assert.True(ReadActors(foreign).Facts.MuleAvailableForSubject);
        Assert.True(ReadActors(foreign, perspective: Perspective.Adjudicator).Facts.MuleAvailableForSubject);
        Assert.Null(ReadActors(state with
        {
            Equipment = []
        }).Facts.MuleAvailableForSubject);
    }

    [Theory]
    [InlineData("attacker-mmg", "asl:mg")]
    [InlineData("attacker-hmg", "asl:mg")]
    public void KnownNonRussianMgMustBeDismantledAndLimberedCannotSubstitute(string definition, string kind)
    {
        var state = PackingState(definition, kind);
        Assert.Null(ReadActors(state).Facts.DismantledOrLimberedIfPossible);
        var assembled = state with
        {
            Equipment = [state.Equipment[0] with
        {
            Conditions = new Dictionary<string, ConditionState> { [Conditions.Dismantled] = ConditionState.False, ["asl:limbered"] = ConditionState.True }
        }]
        };
        var refused = ReadActors(assembled);
        Assert.False(refused.Facts.DismantledOrLimberedIfPossible);
        Assert.Equal(Rules.TurnPackingEligibility.Ineligible, refused.Verdict.State);
        var dismantled = state with
        {
            Equipment = [state.Equipment[0] with
        {
            Conditions = new Dictionary<string, ConditionState> { [Conditions.Dismantled] = ConditionState.True }
        }]
        };
        Assert.True(ReadActors(dismantled).Facts.DismantledOrLimberedIfPossible);
        Assert.Equal(Rules.TurnPackingEligibility.NotEvaluated, ReadActors(dismantled).Verdict.State);
        Assert.Null(ReadActors(dismantled, catalogs: []).Facts.DismantledOrLimberedIfPossible);
    }
    [Fact]
    public void LightMortarAssemblyUsesPrintedPortageAndDoesNotTreatMissingSourceAsZero()
    {
        var state = PackingState("attacker-light-mortar", "asl:light-mortar");
        var definition = Catalog.Value.Definition("attacker-light-mortar")!;
        var portage = definition.Printed("front", "asl:portage")!;
        Assert.True(portage.Value!.Number >= 4);
        state = state with
        {
            Equipment = [state.Equipment[0] with
        {
            Conditions = new Dictionary<string, ConditionState> { [Conditions.Dismantled] = ConditionState.False }
        }]
        };
        Assert.False(ReadActors(state).Facts.DismantledOrLimberedIfPossible);
        var small = definition with
        {
            Values = [.. definition.Values.Select(value => value == portage
            ? value with { Value = value.Value! with { Number = 3 } } : value)]
        };
        var smallCatalog = Catalog.Value with
        {
            Definitions = [.. Catalog.Value.Definitions.Select(item => item.Id == small.Id ? small : item)]
        };
        Assert.True(ReadActors(state, catalogs: [smallCatalog]).Facts.DismantledOrLimberedIfPossible);
        foreach (var sourceState in new[] { PrintedState.NotPrinted, PrintedState.NotInSource })
        {
            var changed = definition with
            {
                Values = [.. definition.Values.Select(value => value == portage ? value with { State = sourceState } : value)]
            };
            var catalog = Catalog.Value with
            {
                Definitions = [.. Catalog.Value.Definitions.Select(item => item.Id == changed.Id ? changed : item)]
            };
            Assert.Null(ReadActors(state, catalogs: [catalog]).Facts.DismantledOrLimberedIfPossible);
        }
        var empty = definition with
        {
            Values = [.. definition.Values.Where(value => value != portage)]
        };
        var missing = Catalog.Value with
        {
            Definitions = [.. Catalog.Value.Definitions.Select(item => item.Id == empty.Id ? empty : item)]
        };
        Assert.Null(ReadActors(state, catalogs: [missing]).Facts.DismantledOrLimberedIfPossible);
    }

}
