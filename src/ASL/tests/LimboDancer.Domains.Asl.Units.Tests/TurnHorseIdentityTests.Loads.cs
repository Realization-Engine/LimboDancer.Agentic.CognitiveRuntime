using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

public sealed partial class TurnHorseIdentityTests
{
    private static NewInstance Load(string id = "gun", string kind = "asl:gun", string? definition = "attacker-inf-gun") =>
        new(id, kind, definition, "german", new ContainedPosition("mule", ContainmentRole.AnimalPack), null, new Dictionary<string, ConditionState>());

    private static List<GameEvent> LoadedSetup()
    {
        var events = Beginning();
        Add(events, new InstanceCreated(Horse()));
        Add(events, new InstanceCreated(Load()));
        Add(events, new InstanceCreated(new("crew", "asl:crew", "attacker-crew", "german",
            new MapPosition(BoardLocation.Parse("bd01:E4:0")), null, new Dictionary<string, ConditionState>())));
        return events;
    }

    [Fact]
    public void PhysicalLoadRoundTripsAndResolvesLocationWithoutInventingCrewPossessionOrWork()
    {
        var events = LoadedSetup();
        var json = GameEventWriter.Write(events[0].Scope, new("loaded Mule setup", true, events));
        Assert.Contains("animal-pack", json, StringComparison.Ordinal);
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(json));
        Assert.False(read.HasErrors);
        var history = Project(read.Record!.Events);
        Assert.False(history.HasErrors);
        var state = history.Current!;
        var weapon = Assert.Single(state.Equipment);
        Assert.Equal(new ContainedPosition("mule", ContainmentRole.AnimalPack), weapon.Position);
        Assert.Null(weapon.Holding);
        Assert.Equal(state.Location("mule"), state.Location("gun"));
        Assert.Empty(state.PackedGuns);
        var facts = ReadActors(state);
        Assert.Equal("mule", facts.LoadedOnMule);
        Assert.True(facts.Facts.MuleAvailableForSubject);
        Assert.Null(facts.ActorsBound);
        Assert.Null(facts.Facts.SameLocationAndTerrain);
        Assert.Null(facts.Facts.FirstRallyWorkWindow);
        Assert.Equal(TurnPackingEligibility.NotEvaluated, facts.Verdict.State);
        Assert.Equal(facts, ReadActors(state, perspective: Perspective.Adjudicator));
        Assert.True(GameEventReader.Read(Encoding.UTF8.GetBytes(json.Replace("animal-pack", "unknown-load", StringComparison.Ordinal))).HasErrors);
    }

    [Theory]
    [InlineData("asl:mg", "attacker-mmg")]
    [InlineData("asl:mg", "attacker-hmg")]
    [InlineData("asl:light-mortar", "attacker-light-mortar")]
    [InlineData("asl:gun", "campaign-german-81mm-mortar")]
    public void EligibleWeaponLoadsUseTheSamePhysicalRelationship(string kind, string definition)
    {
        var events = Beginning();
        Add(events, new InstanceCreated(Horse()));
        Add(events, new InstanceCreated(Load(kind: kind, definition: definition)));
        var history = Project(events);
        Assert.False(history.HasErrors);
        Assert.Equal("mule", ReadActors(history.Current!).LoadedOnMule);
    }

    [Fact]
    public void NonPackWeaponsUnknownDefinitionsPersonnelAndConflictingHoldingAreRefused()
    {
        NewInstance[] invalid =
        [
            Load(kind: "asl:mg", definition: "attacker-lmg"), Load(definition: null), Load(definition: "defender-at-gun"),
            Load(kind: "asl:crew", definition: "attacker-crew"),
            Load() with { Side = "russian" }, Load() with { Side = null },
            Load() with { Holding = new("crew", HoldingRole.Manned) }
        ];
        foreach (var instance in invalid)
        {
            var events = LoadedSetup();
            events.RemoveAt(2);
            // Preserve causal revisions while placing the crew before the proposed load.
            events[2] = events[2] with
            {
                Revision = 3,
                EventId = "horse-event-3"
            };
            Add(events, new InstanceCreated(instance));
            var history = Project(events);
            Assert.True(history.HasErrors);
            Assert.Contains(history.Diagnostics, item => item.Code == "UNIT-PACK-004");
        }
    }

    [Fact]
    public void LoadRequiresOneActiveDesignatedSquadMuleAndCannotCarryRiders()
    {
        TurnHorseIdentity[] identities =
        [new(TurnHorseSize.Squad, false, null), new(TurnHorseSize.Unknown, null, null), new(TurnHorseSize.HalfSquad, true, Designation)];
        foreach (var identity in identities)
        {
            var events = Beginning();
            Add(events, new InstanceCreated(Horse() with
            {
                Horse = identity
            }));
            Add(events, new InstanceCreated(Load()));
            Assert.Contains(Project(events).Diagnostics, item => item.Code == "UNIT-PACK-004");
        }
        var duplicate = LoadedSetup();
        Add(duplicate, new InstanceCreated(Load("second-gun")));
        Assert.Contains(Project(duplicate).Diagnostics, item => item.Code == "UNIT-PACK-004");
        var missing = Beginning();
        Add(missing, new InstanceCreated(Load()));
        Assert.Contains(Project(missing).Diagnostics, item => item.Code == "UNIT-STATE-011");
        var wrong = Beginning();
        Add(wrong, new InstanceCreated(new("mule", "asl:wire", null, null, new MapPosition(BoardLocation.Parse("bd01:E4:0")), null, new Dictionary<string, ConditionState>())));
        Add(wrong, new InstanceCreated(Load()));
        Assert.Contains(Project(wrong).Diagnostics, item => item.Code == "UNIT-STATE-011");
        var rider = Beginning();
        Add(rider, new InstanceCreated(Horse()));
        Add(rider, new InstanceCreated(new("crew", "asl:crew", "attacker-crew", "german",
            new ContainedPosition("mule", ContainmentRole.Rider), null, new Dictionary<string, ConditionState>())));
        Assert.Contains(Project(rider).Diagnostics, item => item.Code == "UNIT-STATE-011");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WithheldMuleAlsoWithholdsItsLoadLocationHistoryAndCapacity(bool hidden)
    {
        var events = LoadedSetup();
        var creation = (InstanceCreated)events[1].Payload;
        events[1] = events[1] with
        {
            Payload = new InstanceCreated(creation.Instance with
            {
                Conditions = new Dictionary<string, ConditionState> { [hidden ? Conditions.Hidden : Conditions.Concealed] = ConditionState.True }
            })
        };
        var history = Project(events);
        Assert.False(history.HasErrors);
        var view = GameView.Of(history, events.Count, Perspective.Side("russian"));
        Assert.Empty(view.Equipment);
        Assert.DoesNotContain("gun", view.Locations.Keys);
        Assert.DoesNotContain(view.Events, item => item.Payload is InstanceCreated { Instance.Id: "gun" });
        var disclosed = ReadActors(history.Current!, perspective: Perspective.Side("russian"));
        var absent = ReadActors(history.Current! with
        {
            Equipment = [],
            Entities = []
        }, perspective: Perspective.Side("russian"));
        Assert.Equal(absent, disclosed);
        Assert.DoesNotContain("animal-pack", JsonSerializer.Serialize(view), StringComparison.Ordinal);
        Assert.Equal("mule", ReadActors(history.Current!).LoadedOnMule);
    }

    [Fact]
    public void DisclosedOtherLoadsRefuseCapacityButAnUnobservedEmptyMuleStaysUnknown()
    {
        var state = Project(LoadedSetup()).Current!;
        var other = state with
        {
            Equipment = [state.Equipment[0] with { Id = "other-gun" }]
        };
        Assert.False(ReadActors(other).Facts.MuleAvailableForSubject);
        Assert.Equal(TurnPackingEligibility.Ineligible, ReadActors(other).Verdict.State);
        Assert.Null(ReadActors(state with
        {
            Equipment = []
        }).Facts.MuleAvailableForSubject);
        var twoMules = state with
        {
            Entities = [.. state.Entities, state.Entities[0] with { Id = "second-mule" }]
        };
        Assert.False(ReadActors(twoMules, new("gun", "second-mule", "crew")).ActorsBound);
        var hiddenLoad = other with
        {
            Equipment = [other.Equipment[0] with
        {
            Conditions = new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.True }
        }]
        };
        Assert.Null(ReadActors(hiddenLoad, perspective: Perspective.Side("russian")).Facts.MuleAvailableForSubject);
    }

    [Fact]
    public void SetupLoadDoesNotAdmitMovementOrPhaseClose()
    {
        EventPayload[] actions = [new InstanceMoved("gun", new MapPosition(BoardLocation.Parse("bd01:E5:0"))), new PhaseChanged(1, "pfph", "german")];
        foreach (var action in actions)
        {
            var events = LoadedSetup();
            Add(events, action, action is InstanceMoved ? "instance-moved" : "phase-changed");
            var history = Project(events);
            Assert.Contains(history.Diagnostics, item => item.Code == "UNIT-PACK-003");
            Assert.Equal(4, history.States.Count);
            Assert.Equal(new ContainedPosition("mule", ContainmentRole.AnimalPack), history.States[^1].Equipment[0].Position);
        }
    }
}
