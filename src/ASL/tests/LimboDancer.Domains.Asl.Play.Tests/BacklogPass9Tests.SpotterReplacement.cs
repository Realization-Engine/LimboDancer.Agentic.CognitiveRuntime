using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class BacklogPass9Tests
{
    private static Dictionary<string, object> ReadySpotter(string id)
    {
        var unit = Squad(id, "bd01:B9:0", "german");
        var conditions = (Dictionary<string, bool>)unit["conditions"];
        conditions[Conditions.Berserk] = false;
        conditions[Conditions.Captured] = false;
        conditions[Conditions.Melee] = false;
        return unit;
    }

    private void RecordSpotterCondition(string id, string condition, bool value, string eventId)
    {
        var item = store.Read(Scope)!.Events[^1] with
        {
            EventId = eventId,
            Revision = Revision + 1,
            Type = "conditions-changed",
            Payload = new ConditionsChanged(id, new Dictionary<string, ConditionState>
            {
                [condition] = value ? ConditionState.True : ConditionState.False
            }),
            RulePackage = null,
            Causes = [],
        };
        Assert.Equal(AppendStatus.Committed, store.Append(Scope, eventId, Revision, [item], Planner().Replay).Status);
    }

    private async Task PrepareReplacement()
    {
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"),
            Mortar("m1", "attacker-light-mortar", "g1", "german"), ReadySpotter("g2"), ReadySpotter("g3"), Squad("r1", "bd01:B6:0", "russian"));
        await Advance();
        Committed(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g2"
        }));
        RecordSpotterCondition("g2", Conditions.Broken, true, "loss-1");
    }

    [Fact]
    public async Task ReplacementUsesTheRecordedOpeningAndWritesNoFireOrMovementMark()
    {
        await PrepareReplacement();
        Refused(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g3"
        }), "play.spotter");
        await Advance();
        Assert.Equal("mph", Current.Phase);
        Assert.Empty(Current.SpotterOpening.Steps);
        var before = Revision;
        var request = JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "replacement",
            expectedRevision = before,
            gunId = "m1",
            spotter = "g3",
            proposedBy = "german"
        });
        Assert.Equal(GamePlanStatus.Ready, (await Planner().PlanAsync(GameActions.DesignateSpotter, request, Tenant)).Status);
        Assert.Equal(before, Revision);
        Committed(await Commit(Play(), GameActions.DesignateSpotter, request));
        Assert.Equal("g3", Assert.Single(Current.MortarSpotters).Spotter);
        Assert.Equal(2, Current.MortarSpotterDesignations.Count);
        Assert.DoesNotContain(Current.SpottedThisPhase, item => item.Spotter == "g3");
        Assert.DoesNotContain(Current.SupportWeaponUses, item => item.Unit == "g3");
        Assert.DoesNotContain("g3", Current.NoMoveThisPlayerTurn);
        Assert.False(Is(Current.Unit("g3")!, Conditions.PrepFire));
        Assert.Equal(MovementOpeningStep.SpotterReplacement, Assert.Single(Current.SpotterOpening.Steps));
        var history = (await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "mph", "existing-engine-fire-v1")).FireHistory!;
        Assert.False(history.Declarations[0].Replacement);
        Assert.True(history.Declarations[1].Replacement);
        Refused(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g3"
        }), "play.spotter");
        var serialized = GameEventWriter.Write(Scope, store.Read(Scope)!);
        var roundTrip = GameEventReader.Read(Encoding.UTF8.GetBytes(serialized)).Record!;
        Assert.Equal(serialized, GameEventWriter.Write(Scope, roundTrip));
        var replayed = Planner().Replay(roundTrip.Events).Current!;
        Assert.Equal(Current.MortarSpotterLosses[0].PlayerTurnOrdinal, replayed.MortarSpotterLosses[0].PlayerTurnOrdinal);
        Assert.Equal(Current.SpotterOpening.Steps, replayed.SpotterOpening.Steps);
    }

    [Fact]
    public async Task AnUnknownCommittedCommandClosesReplacementEvenWhenThePhaseStillSaysMovement()
    {
        await PrepareReplacement();
        await Advance();
        RecordSpotterCondition("g3", Conditions.Concealed, false, "other-command-1");
        Assert.Equal(MovementOpeningStep.Other, Assert.Single(Current.SpotterOpening.Steps));
        Refused(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g3"
        }), "play.spotter");
        var forged = store.Read(Scope)!.Events[^1] with
        {
            EventId = "forged-replacement-1",
            Revision = Revision + 1,
            Type = "mortar-spotter-designated",
            Payload = new MortarSpotterDesignated("m1", "g3"),
            RulePackage = "asl.spotter.v2"
        };
        Assert.True(Planner().Replay([.. store.Read(Scope)!.Events, forged]).HasErrors);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task OriginalDesignationReadsMortarAndCandidateTiSeparately(bool mortarTi, bool candidateTi)
    {
        var mortar = Mortar("m1", "attacker-light-mortar", "g1", "german");
        ((Dictionary<string, bool>)mortar["conditions"])["asl:ti"] = mortarTi;
        var spotter = ReadySpotter("g2");
        ((Dictionary<string, bool>)spotter["conditions"])["asl:ti"] = candidateTi;
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"), mortar, spotter, Squad("r1", "bd01:B6:0", "russian"));
        await Advance();
        var facts = Planner().ReadSpotterDesignation(Current, "m1", "g2")!;
        Assert.Equal(mortarTi, facts.TemporarilyImmobilized);
        Assert.Equal(candidateTi, facts.SpotterTemporarilyImmobilized);
        Refused(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g2"
        }), "play.spotter");
    }

    [Fact]
    public async Task ANewAssignmentOccurrenceDoesNotInheritTheOriginalSpottersOldLoss()
    {
        await PrepareReplacement();
        await Advance();
        Committed(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g3"
        }));
        var state = Current;
        // A later declaration of the recovered original starts a distinct occurrence. The projection's loss journal remains historical.
        var later = state with
        {
            MortarSpotters = [new("m1", "g2")],
            MortarSpotterDesignations = [.. state.MortarSpotterDesignations,
                new("m1", "g2", "german", "new-original-1", state.Revision + 10, state.TurnPhaseOccurrence!)],
        };
        Assert.Null(later.ActiveSpotterLoss("m1", "g2"));
        Assert.Single(later.MortarSpotterLosses);
        var nextLoss = later.MortarSpotterLosses[0] with
        {
            EventId = "new-loss-1",
            Revision = state.Revision + 11
        };
        later = later with
        {
            MortarSpotterLosses = [.. later.MortarSpotterLosses, nextLoss]
        };
        Assert.Same(nextLoss, later.ActiveSpotterLoss("m1", "g2"));
    }

    [Fact]
    public async Task ReplacementClassifiesTheWholeCommandAndIncrementalReplayRechecksAnExtendedBatch()
    {
        await PrepareReplacement();
        await Advance();
        var events = store.Read(Scope)!.Events;
        var basis = events[^1];
        var dice = basis with
        {
            EventId = "batched-replacement-1",
            Revision = Revision + 1,
            Type = "dice-rolled",
            Payload = new DiceRolled("batch-roll", "test", 1, 6, [1], DiceRolled.SystemSource, "player"),
            RulePackage = null,
            Causes = []
        };
        var designation = basis with
        {
            EventId = "batched-replacement-2",
            Revision = Revision + 2,
            Type = "mortar-spotter-designated",
            Payload = new MortarSpotterDesignated("m1", "g3"),
            RulePackage = "asl.spotter.v2",
            Causes = []
        };
        var whole = Planner().Replay([.. events, dice, designation]);
        Assert.False(whole.HasErrors, string.Join("; ", whole.Diagnostics.Select(item => item.Message)));
        Assert.Equal("g3", Assert.Single(whole.Current!.MortarSpotters).Spotter);
        Assert.Equal(MovementOpeningStep.SpotterReplacement, Assert.Single(whole.Current.SpotterOpening.Steps));
        var incremental = Planner();
        Assert.False(incremental.Replay([.. events, dice]).HasErrors);
        var continued = incremental.Replay([.. events, dice, designation]);
        Assert.False(continued.HasErrors, string.Join("; ", continued.Diagnostics.Select(item => item.Message)));

        var early = designation with
        {
            EventId = "mixed-replacement-1",
            Revision = Revision + 1
        };
        var later = basis with
        {
            EventId = "mixed-replacement-2",
            Revision = Revision + 2,
            Type = "movement-ended",
            Payload = new MovementEnded(["g1"]),
            Causes = [],
            RulePackage = null
        };
        Assert.True(Planner().Replay([.. events, early, later]).HasErrors);
        var acceptedPrefix = Planner();
        Assert.False(acceptedPrefix.Replay([.. events, early]).HasErrors);
        Assert.True(acceptedPrefix.Replay([.. events, early, later]).HasErrors);
    }

    [Fact]
    public async Task TwoDifferentMortarsCanReplaceAtTheSameOpening()
    {
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"),
            Mortar("m1", "attacker-light-mortar", "g1", "german"), Mortar("m2", "attacker-light-mortar", "g1", "german"),
            ReadySpotter("g2"), ReadySpotter("g3"), Squad("r1", "bd01:B6:0", "russian"));
        await Advance();
        Committed(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g2"
        }));
        Committed(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m2",
            spotter = "g2"
        }));
        RecordSpotterCondition("g2", Conditions.Broken, true, "loss-both-1");
        await Advance();
        Committed(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g3"
        }));
        Committed(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m2",
            spotter = "g3"
        }));
        Assert.Equal(2, Current.MortarSpotters.Count);
        Assert.All(Current.MortarSpotters, item => Assert.Equal("g3", item.Spotter));
        Assert.Equal(2, Current.SpotterOpening.Steps.Count);
        Assert.All(Current.SpotterOpening.Steps, step => Assert.Equal(MovementOpeningStep.SpotterReplacement, step));
    }

    [Fact]
    public async Task AGloballyNumberedLegacyJournalCannotHideMovementPhaseActivityAsHousekeeping()
    {
        await PrepareReplacement();
        await Advance();
        RecordSpotterCondition("g3", Conditions.Concealed, false, "ordinary-action-1");
        var events = store.Read(Scope)!.Events;
        var ids = events.Select((item, index) => (item.EventId, NewId: $"legacy-journal-{index + 1}"))
            .ToDictionary(item => item.EventId, item => item.NewId, StringComparer.Ordinal);
        var numbered = events.Select(item => item with
        {
            EventId = ids[item.EventId],
            Causes = [.. item.Causes.Select(cause => ids[cause])]
        }).ToArray();
        var read = Planner().Replay(numbered);
        Assert.False(read.HasErrors, string.Join("; ", read.Diagnostics.Select(item => item.Message)));
        Assert.False(ScenarioA1SpotterCalculator.OpeningOpen(read.Current!.SpotterOpening.Steps));
        var replacement = numbered[^1] with
        {
            EventId = "new-replacement-1",
            Revision = read.Current.Revision + 1,
            Type = "mortar-spotter-designated",
            Payload = new MortarSpotterDesignated("m1", "g3"),
            RulePackage = "asl.spotter.v2",
            Causes = []
        };
        Assert.True(Planner().Replay([.. numbered, replacement]).HasErrors);
    }

    [Fact]
    public async Task CandidateAssignmentReadsShareOneSnapshotAndRejectAStaleContext()
    {
        await PrepareReplacement();
        var state = Current;
        var context = MortarSpotterFacts.ReadAssignments(state);
        var planner = Planner();
        Assert.Equal(planner.ReadSpotterDesignation(state, "m1", "g3"), planner.ReadSpotterDesignation(state, "m1", "g3", context));
        var changed = state with
        {
            MortarSpotterLosses = []
        };
        var fresh = planner.ReadSpotterDesignation(changed, "m1", "g3");
        Assert.Equal(SpotterChange.None, fresh!.Timing.Change);
        Assert.Equal(fresh, planner.ReadSpotterDesignation(changed, "m1", "g3", context));
    }
}
