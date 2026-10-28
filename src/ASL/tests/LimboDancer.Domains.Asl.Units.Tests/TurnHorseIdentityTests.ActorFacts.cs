using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

public sealed partial class TurnHorseIdentityTests
{
    private static readonly TurnPackingActors PackingActors = new("gun", "mule", "crew");
    private static GameState PackingState(string definition = "attacker-inf-gun", string kind = "asl:gun")
    {
        var events = Beginning();
        var clear = new Dictionary<string, ConditionState>
        {
            [Conditions.Broken] = ConditionState.False,
            [Conditions.Berserk] = ConditionState.False,
            [Conditions.Captured] = ConditionState.False,
            [Conditions.Melee] = ConditionState.False,
            [Conditions.Pinned] = ConditionState.False
        };
        Add(events, new InstanceCreated(new("crew", "asl:crew", "attacker-crew", "german", new MapPosition(BoardLocation.Parse("bd01:E4:0")), null, clear)));
        Add(events, new InstanceCreated(new("gun", kind, definition, "german", kind == "asl:gun" ? new MapPosition(BoardLocation.Parse("bd01:E4:0")) : null,
            new("crew", kind == "asl:gun" ? HoldingRole.Manned : HoldingRole.Possessed), new Dictionary<string, ConditionState>())));
        Add(events, new InstanceCreated(Horse()));
        var history = Project(events);
        Assert.False(history.HasErrors);
        return history.Current!;
    }
    private static TurnPackingActorRead ReadActors(GameState state, TurnPackingActors? actors = null, Perspective? perspective = null, IReadOnlyList<UnitCatalog>? catalogs = null) =>
        TurnPackingActorReader.Read(state, perspective ?? Perspective.Side("german"), actors ?? PackingActors, UnitsTestData.Asl.Value, catalogs ?? [Catalog.Value]);

    [Fact]
    public void ActorReaderBindsRealCountersAndCatalogFactsWithoutInventingAnOpeningTerrainOrLoad()
    {
        var state = PackingState();
        var before = JsonSerializer.Serialize(state);
        var read = ReadActors(state);
        Assert.Equal(state.Stamp, read.Stamp);
        Assert.Equal("german", read.Owner);
        Assert.True(read.ActorsBound);
        Assert.Equal(TurnPackingWeapon.Gun, read.Weapon);
        Assert.Equal(10, read.ManhandlingNumber);
        Assert.Equal(3, read.RequiredGameTurns);
        Assert.True(read.Facts.GunAllowedByNoteOrDyo);
        Assert.True(read.Facts.MuleDesignated);
        Assert.True(read.Facts.SquadMule);
        Assert.True(read.Facts.CrewCounter);
        Assert.True(read.Facts.SameNationality);
        Assert.True(read.Facts.GoodOrder);
        Assert.True(read.Facts.Unpinned);
        Assert.True(read.Facts.NonPrc);
        Assert.Null(read.Facts.FirstRallyWorkWindow);
        Assert.Null(read.Facts.SameLocationAndTerrain);
        Assert.True(read.Facts.MuleAvailableForSubject);
        Assert.Null(read.Facts.DismantledOrLimberedIfPossible);
        Assert.Equal(TurnPackingEligibility.NotEvaluated, read.Verdict.State);
        Assert.Equal(before, JsonSerializer.Serialize(state));
        Assert.False(ReadActors(state with
        {
            Phase = "pfph"
        }).Facts.FirstRallyWorkWindow);
        Assert.Equal(6, ReadActors(state with
        {
            Nvr = 3
        }).RequiredGameTurns);
    }

    [Theory]
    [InlineData("attacker-light-mortar", "asl:light-mortar", TurnPackingWeapon.LightMortar)]
    [InlineData("attacker-mmg", "asl:mg", TurnPackingWeapon.MediumMachineGun)]
    [InlineData("attacker-hmg", "asl:mg", TurnPackingWeapon.HeavyMachineGun)]
    public void SameNationalityMmcQualifiesForListedSupportWeaponsWithoutGrantingPackingPermission(string definition, string kind, TurnPackingWeapon expected)
    {
        var read = ReadActors(PackingState(definition, kind));
        Assert.Equal(expected, read.Weapon);
        Assert.Equal(1, read.RequiredGameTurns);
        Assert.True(read.Facts.QualifiedMmcForSw);
        Assert.Equal(TurnPackingEligibility.NotEvaluated, read.Verdict.State);
    }

    [Theory]
    [InlineData("attacker-squad", "asl:squad", true)]
    [InlineData("attacker-half-squad", "asl:half-squad", true)]
    [InlineData("attacker-crew", "asl:crew", true)]
    [InlineData("attacker-leader-8-0", "asl:leader", false)]
    public void SwPackingQualificationUsesActualVisibleMmcRoles(string definition, string kind, bool expected)
    {
        var state = PackingState("attacker-mmg", "asl:mg");
        state = state with
        {
            Units = [state.Units[0] with { Kind = kind, Definition = Catalog.Value.Reference(Catalog.Value.Definition(definition)!) }]
        };
        var read = ReadActors(state);
        Assert.Equal(expected, read.Facts.QualifiedMmcForSw);
        Assert.Equal(expected ? TurnPackingEligibility.NotEvaluated : TurnPackingEligibility.Ineligible, read.Verdict.State);
        Assert.Null(read.Facts.FirstRallyWorkWindow);
        Assert.Null(read.Facts.SameLocationAndTerrain);
    }

    [Fact]
    public void SwQualificationKeepsForeignMissingAndHiddenEvidenceUnknown()
    {
        var state = PackingState("attacker-light-mortar", "asl:light-mortar");
        var foreign = state with
        {
            Units = [state.Units[0] with { Definition = Catalog.Value.Reference(Catalog.Value.Definition("defender-crew")!) }]
        };
        Assert.False(ReadActors(foreign).Facts.SameNationality);
        Assert.Null(ReadActors(foreign).Facts.QualifiedMmcForSw);
        Assert.Null(ReadActors(state, catalogs: []).Facts.QualifiedMmcForSw);
        var hidden = state with
        {
            Units = [state.Units[0] with { Conditions = new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.True } }]
        };
        var disclosed = ReadActors(hidden, perspective: Perspective.Side("russian"));
        var absent = ReadActors(hidden with
        {
            Units = [],
            Equipment = [] // Remove the absent holder's SW too; a dangling holding relationship is not an equivalent valid state.
        }, perspective: Perspective.Side("russian"));
        Assert.Null(disclosed.Facts.QualifiedMmcForSw);
        Assert.Equal(absent.Facts, disclosed.Facts);
        Assert.True(ReadActors(hidden).Facts.QualifiedMmcForSw);
    }

    [Fact]
    public void MissingOrUnreviewedCatalogsNeverSupplyAWeaponPermissionOrPeriod()
    {
        var state = PackingState();
        var missing = ReadActors(state, catalogs: []);
        Assert.Equal(TurnPackingWeapon.Unknown, missing.Weapon);
        Assert.Null(missing.ManhandlingNumber);
        Assert.Null(missing.RequiredGameTurns);
        Assert.Null(missing.Facts.SameNationality);
        Assert.Null(missing.Facts.GunAllowedByNoteOrDyo);
        var draft = ReadActors(state, catalogs: [Catalog.Value with { Publication = CatalogPublication.Draft }]);
        Assert.Equal(missing.Facts, draft.Facts);
        Assert.Equal(TurnPackingEligibility.NotEvaluated, draft.Verdict.State);
        var unclassified = ReadActors(PackingState("attacker-lmg", "asl:mg"));
        Assert.Equal(TurnPackingWeapon.Unknown, unclassified.Weapon);
        Assert.Null(unclassified.RequiredGameTurns);
    }

    [Fact]
    public void VisibleWrongRolesOwnershipNationalityAndLocationsAreRefusals()
    {
        var state = PackingState();
        Assert.Equal(TurnPackingEligibility.Ineligible, ReadActors(state, new("gun", "gun", "crew")).Verdict.State);
        Assert.Equal(TurnPackingEligibility.Ineligible, ReadActors(state, new("mule", "gun", "crew")).Verdict.State);
        var foreign = state with
        {
            Units = [state.Units[0] with { Definition = Catalog.Value.Reference(Catalog.Value.Definition("defender-crew")!) }]
        };
        Assert.False(ReadActors(foreign).Facts.SameNationality);
        Assert.Equal(TurnPackingEligibility.Ineligible, ReadActors(foreign).Verdict.State);
        var elsewhere = state with
        {
            Entities = [state.Entities[0] with { Position = new MapPosition(BoardLocation.Parse("bd01:E5:0")) }]
        };
        Assert.False(ReadActors(elsewhere).Facts.SameLocationAndTerrain);
        Assert.Equal(TurnPackingEligibility.Ineligible, ReadActors(elsewhere).Verdict.State);
        var enemy = state with
        {
            Entities = [state.Entities[0] with { Side = "russian" }]
        };
        Assert.False(ReadActors(enemy).ActorsBound);
        var half = state with
        {
            Entities = [state.Entities[0] with { Horse = new(TurnHorseSize.HalfSquad, true, Designation) }]
        };
        Assert.False(ReadActors(half).Facts.SquadMule);
        Assert.Equal(TurnPackingEligibility.Ineligible, ReadActors(half).Verdict.State);
        var ordinary = state with
        {
            Entities = [state.Entities[0] with { Horse = new(TurnHorseSize.Squad, false, null) }]
        };
        Assert.False(ReadActors(ordinary).Facts.MuleDesignated);
        Assert.Equal(TurnPackingEligibility.Ineligible, ReadActors(ordinary).Verdict.State);
    }

    [Fact]
    public void LegacyPackingAndUnknownCrewStatusCannotBecomeVerifiedActorEvidence()
    {
        var state = PackingState();
        var legacy = ReadActors(state with
        {
            PackedGuns = ["gun"],
            Equipment = [state.Equipment[0] with { Holding = null }]
        });
        Assert.Null(legacy.ActorsBound);
        Assert.Null(legacy.Facts.MuleAvailableForSubject);
        Assert.Equal(TurnPackingEligibility.NotEvaluated, legacy.Verdict.State);
        var unknown = ReadActors(state with
        {
            Units = [state.Units[0] with { Conditions = new Dictionary<string, ConditionState>() }]
        });
        Assert.Null(unknown.Facts.GoodOrder);
        Assert.Null(unknown.Facts.Unpinned);
        var unpossessed = ReadActors(state with
        {
            Equipment = [state.Equipment[0] with { Holding = null }]
        });
        Assert.Null(unpossessed.ActorsBound);
        Assert.True(unpossessed.Facts.MuleAvailableForSubject);
        Assert.Equal(legacy.Facts with
        {
            MuleAvailableForSubject = true
        }, unpossessed.Facts);
        Assert.Equal(legacy.Verdict, unpossessed.Verdict);
        var otherHolder = state with
        {
            Units = [.. state.Units, state.Units[0] with { Id = "other-crew" }],
            Equipment = [state.Equipment[0] with { Holding = new("other-crew", HoldingRole.Manned) }]
        };
        Assert.False(ReadActors(otherHolder).ActorsBound);
        Assert.Equal(TurnPackingEligibility.Ineligible, ReadActors(otherHolder).Verdict.State);
    }

    [Fact]
    public void WithheldActorsAreIndistinguishableFromMissingOnesToTheOpponent()
    {
        var state = PackingState();
        var hidden = state with
        {
            Entities = [state.Entities[0] with { Conditions = new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.True } }]
        };
        var hiddenRead = ReadActors(hidden, perspective: Perspective.Side("russian"));
        var missingRead = ReadActors(hidden with
        {
            Entities = []
        }, perspective: Perspective.Side("russian"));
        Assert.Null(hiddenRead.ActorsBound);
        Assert.Equal(missingRead.Facts, hiddenRead.Facts);
        Assert.Equal(missingRead.Verdict, hiddenRead.Verdict);
        Assert.DoesNotContain(Designation, JsonSerializer.Serialize(hiddenRead), StringComparison.Ordinal);
        Assert.True(ReadActors(hidden, perspective: Perspective.Adjudicator).ActorsBound);
        Assert.True(ReadActors(hidden).ActorsBound);
    }
}
