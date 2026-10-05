using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 31 of the Card Play and Map Studio Redesign Plan: the play-test UI, part I. The tests replay a real game, The Guards Counterattack played to
/// its end through the Play page on 2026-10-04 (613 revisions, the fixture <c>guards-dl-01.game.json</c>), and propose at the revisions where the
/// play test met each problem: a move stopped by fire, the Defensive First Fire limit, fire at a Location with two leaders, the Rout Phase's end,
/// the game's end and its result, a Russian Deployment, and a freed SMC. Rulings R31.1 to R31.8.
/// </summary>
public sealed class BacklogPass31Tests : IDisposable
{
    private const string Played = "guards-dl-01";
    private static readonly Guid Tenant = Guid.Parse("5a7d1f00-0000-4000-8000-0000000057d0");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();

    private static readonly UnitCatalog[] Catalogs =
        [.. UnitCatalogs.Names.Select(name => UnitCatalogs.Read(name, Vocabulary)?.Catalog).OfType<UnitCatalog>()];

    private static readonly JsonSerializerOptions Compact = new()
    {
        WriteIndented = false
    };

    private static readonly string[] LoneFirer = ["g-squad-4"];
    private static readonly string[] GuardsStack = ["r-squad-16", "r-squad-17", "r-squad-18"];

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p31-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public BacklogPass31Tests() => store = new FileGameStore(Path.Combine(root, "games"));

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    // The test board carries no LOS data: clear LOS at the board's true range, with no Hindrance, as in the tests of pass 13.
    private sealed class ClearLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, Catalogs, fireLos: new ClearLos());

    /// <summary>The played game cut back to a revision, saved under a name of its own; the scope to propose in.</summary>
    private GameScope Cut(int revision, string name)
    {
        var game = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", Played + ".game.json")))!;
        game["game"] = name;
        var events = game["events"]!.AsArray();
        while (events.Count > revision)
        {
            events.RemoveAt(events.Count - 1);
        }

        var folder = Path.Combine(root, "games", Tenant.ToString("N"));
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, name + ".game.json"), game.ToJsonString(Compact));
        return new GameScope(Tenant, name);
    }

    private static JsonElement Arguments(string game, long revision, string attempt, string? proposedBy, object more)
    {
        var node = JsonSerializer.SerializeToNode(more)!.AsObject();
        node["gameId"] = game;
        node["attemptId"] = attempt;
        node["expectedRevision"] = revision;
        if (proposedBy is not null)
        {
            node[GamePlanner.ProposedByArgument] = proposedBy;
        }

        return JsonSerializer.SerializeToElement(node);
    }

    private Task<GamePlan> Plan(LimboDancer.Abstractions.Actions.ActionDescriptor action, GameScope scope, int revision, string? proposedBy, object more) =>
        Planner().PlanAsync(action, Arguments(scope.Game, revision, "p31-" + Guid.NewGuid().ToString("N")[..12], proposedBy, more), Tenant, "tester");

    [Fact]
    public void ThePlayedGameReplaysToItsEndWithItsResultAndAnAccountOfEveryCondition()
    {
        var scope = Cut(613, "p31-whole");
        var planner = Planner();
        var history = planner.Replay(store.Read(scope)!.Events);

        Assert.False(history.HasErrors);
        var ended = Assert.IsType<GameEnded>(history.Current!.Ended);
        Assert.Equal("german", ended.Result!.Winner);

        // Play test P-08: every condition of the card is read with whether it holds and why.
        var report = planner.Victory(history, ended: true)!;
        Assert.Equal(2, report.Conditions.Count);
        Assert.All(report.Conditions, line => Assert.False(line.Holds));
        Assert.All(report.Conditions, line => Assert.Equal("russian", line.Winner));
        Assert.Contains("a margin of 0, where 2 is needed", report.Conditions[0].Text, StringComparison.Ordinal);
        Assert.Contains("14 unbroken squad-equivalents against 8", report.Conditions[1].Text, StringComparison.Ordinal);
        Assert.Equal("german", report.Otherwise);
    }

    [Fact]
    public async Task TheDefendersPassEndsAMoveThatFireStopped()
    {
        // Play test P-01 (A8.1, A4.2; ruling R25.7): at revision 441 the German squad that moved into H5 is broken, the window is open, and no member is left.
        var scope = Cut(441, "p31-stopped");
        var state = Planner().Replay(store.Read(scope)!.Events).Current!;
        Assert.True(state.Movement is { WindowOpen: true, Members.Count: 0 });

        var pass = await Plan(GameActions.PassFire, scope, 441, "russian", new
        {
        });
        Assert.Equal(GamePlanStatus.Ready, pass.Status);
        Assert.Equal(["movement-window-closed", "movement-ended"], pass.Events.Select(item => item.Type));

        // Ruling R31.6: the pass is the DEFENDER's.
        var notTheirs = await Plan(GameActions.PassFire, scope, 441, "german", new
        {
        });
        Assert.Equal(GamePlanStatus.Refused, notTheirs.Status);
        Assert.Contains(notTheirs.Reasons, reason => reason.StartsWith("play.not-your-action:", StringComparison.Ordinal));

        // A caller that names no view, and the adjudicator, may propose anything, as before.
        Assert.Equal(GamePlanStatus.Ready, (await Plan(GameActions.PassFire, scope, 441, null, new
        {
        })).Status);
        Assert.Equal(GamePlanStatus.Ready, (await Plan(GameActions.PassFire, scope, 441, GamePlanner.AdjudicatorView, new
        {
        })).Status);
    }

    [Fact]
    public async Task ASidesViewActsOnlyForItsOwnUnitsAndEndsOnlyItsOwnPhases()
    {
        // Ruling R31.6 (play test P-07). Revision 485: Turn 5, the Russian RPh.
        var rally = Cut(485, "p31-sides");
        var forTheOther = await Plan(GameActions.Rally, rally, 485, "german", new
        {
            unitId = "r-squad-2"
        });
        Assert.Equal(GamePlanStatus.Refused, forTheOther.Status);
        Assert.Contains(forTheOther.Reasons, reason => reason.StartsWith("play.not-your-unit: r-squad-2 belongs to the russian side", StringComparison.Ordinal));
        Assert.Equal(GamePlanStatus.Ready, (await Plan(GameActions.Rally, rally, 485, "russian", new
        {
            unitId = "r-squad-2"
        })).Status);

        // Either side ends the RPh, where both act.
        Assert.Equal(GamePlanStatus.Ready, (await Plan(GameActions.AdvancePhase, rally, 485, "german", new
        {
        })).Status);

        // Revision 59: Turn 1, the Russian PFPh, which the ATTACKER ends.
        var prep = Cut(59, "p31-prep");
        var notYours = await Plan(GameActions.AdvancePhase, prep, 59, "german", new
        {
        });
        Assert.Contains(notYours.Reasons, reason => reason.StartsWith("play.not-your-action: the russian side ends this phase", StringComparison.Ordinal));
        Assert.Equal(GamePlanStatus.Ready, (await Plan(GameActions.AdvancePhase, prep, 59, "russian", new
        {
        })).Status);

        var state = Planner().Replay(store.Read(prep)!.Events).Current!;
        Assert.Equal("russian", GamePlanner.PhaseEndedBy(state, "german"));
    }

    [Fact]
    public async Task TheRoutPhaseIsNotEndedWhileTheOtherSideMustStillRoutAndItsEndNamesWhatIsLost()
    {
        // Revision 381: Turn 4, the Russian RtPh opens with two Russian squads ADJACENT to a German squad.
        var scope = Cut(381, "p31-rout");

        // Table player, pass 31 (A10.5): the German view may not end the phase on them.
        var german = await Plan(GameActions.AdvancePhase, scope, 381, "german", new
        {
        });
        Assert.True(german.Status == GamePlanStatus.Refused, string.Join(" | ", german.Reasons));
        Assert.Contains(german.Reasons, reason => reason.Contains("still has a unit that must rout", StringComparison.Ordinal));

        // Play test P-06: their own side's end of the phase passes, and says what it costs.
        var russian = await Plan(GameActions.AdvancePhase, scope, 381, "russian", new
        {
        });
        Assert.Equal(GamePlanStatus.Ready, russian.Status);
        PlanConsequence[] lost = [.. russian.Reasons.Select(GamePlanner.ConsequenceOf).OfType<PlanConsequence>()];
        Assert.Equal(2, lost.Length);
        Assert.All(lost, item => Assert.Equal(ConsequenceKind.Loss, item.Kind));
        Assert.Contains(lost, item => item.Text.StartsWith("r-squad-20 is eliminated for Failure to Rout", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheLastPhasesEndIsAConsequenceAndIsSaidOnce()
    {
        // Play test P-08: revision 612 is the last phase of the last turn.
        var scope = Cut(612, "p31-last");
        var end = await Plan(GameActions.AdvancePhase, scope, 612, "german", new
        {
        });

        Assert.Equal(GamePlanStatus.Ready, end.Status);
        Assert.IsType<GameEnded>(end.Events[^1].Payload);
        Assert.Contains(end.Reasons, reason => GamePlanner.ConsequenceOf(reason) is { Kind: ConsequenceKind.End });
        Assert.Single(end.Reasons, reason => reason.StartsWith("play.result:", StringComparison.Ordinal));
    }

    [Fact]
    public void AReasonIsAConsequenceOnlyWhenThePlannerNamesItOne()
    {
        Assert.Null(GamePlanner.ConsequenceOf("play.advance: turn 4, aph, russian phasing"));
        Assert.Equal(ConsequenceKind.Loss, GamePlanner.ConsequenceOf("play.melee-eliminated: x is broken in Melee")!.Kind);
        Assert.Equal(ConsequenceKind.Waste, GamePlanner.ConsequenceOf("play.fire-los-blocked: no firer has a LOS")!.Kind);
        Assert.Equal(ConsequenceKind.OwnUnits, GamePlanner.ConsequenceOf("play.fire-own-units: a of the firing side is in b")!.Kind);
        Assert.Equal("x surrenders", GamePlanner.ConsequenceOf("play.failure-to-rout-surrender: x surrenders")!.Text);
    }

    [Fact]
    public async Task TheFirstFireLimitCountsThisStacksMoveOnly()
    {
        // Play test P-05 (A8.3, A9.2; ruling R31.1). Revision 332: Turn 4, a Russian leader has entered I3 for 1 MF. g-squad-4 fired at a first step
        // in Turn 1, which the old count read as an attack on this stack.
        var scope = Cut(332, "p31-limit");
        var fire = await Plan(GameActions.Fire, scope, 332, "german", new
        {
            firers = LoneFirer,
            target = "bd01:I3:0"
        });

        Assert.DoesNotContain(fire.Reasons, reason => reason.StartsWith("play.fire-mf-limit", StringComparison.Ordinal));
        Assert.True(fire.Status == GamePlanStatus.Ready, string.Join(" | ", fire.Reasons));
    }

    [Fact]
    public async Task FireAtALocationWithTwoLeadersIsDecided()
    {
        // Play test P-02 (A10.2, A10.21, A10.22; ruling R31.5). Revision 494: Turn 5, the Russian PFPh; H4 holds a German squad and two leaders.
        var scope = Cut(494, "p31-leaders");
        var fire = await Plan(GameActions.Fire, scope, 494, "russian", new
        {
            firers = GuardsStack,
            target = "bd01:H4:0"
        });

        Assert.DoesNotContain(fire.Reasons, reason => reason.Contains("leaders-interact", StringComparison.Ordinal));
        Assert.True(fire.Status == GamePlanStatus.Ready, string.Join(" | ", fire.Reasons));

        // Ruling R31.3: the attack records how a pinned unit's MG fires; H4 is not in Melee yet, so ruling R31.2's fact is absent.
        Assert.True(fire.Fire!.Attack.PinnedMgAreaFire);
        Assert.Null(fire.Fire.Attack.TargetsInMelee);

        // Play test P-13 (A11.15; ruling R31.2). Revision 301: Turn 4, the Russian PFPh; H4 holds a Melee of r-squad-19 and g-squad-7. Fire from
        // outside attacks the Russian squad too, the proposal says so, and the attack records the Melee, so it makes no Leader Loss check.
        var melee = Cut(301, "p31-melee");
        var into = await Plan(GameActions.Fire, melee, 301, "russian", new
        {
            firers = GuardsStack,
            target = "bd01:H4:0"
        });
        Assert.True(into.Status == GamePlanStatus.Ready, string.Join(" | ", into.Reasons));
        Assert.Contains(into.Reasons, reason => GamePlanner.ConsequenceOf(reason) is { Kind: ConsequenceKind.OwnUnits, Text: var text } && text.Contains("r-squad-19", StringComparison.Ordinal));
        Assert.True(into.Fire!.Attack.TargetsInMelee);
    }

    [Fact]
    public async Task RussianSquadsMayNotDeploy()
    {
        // Play test R-01 (A25.2; ruling R31.4, correcting R13.4). Revision 485: the Russian RPh; r-squad-10 is a Guards squad.
        var scope = Cut(485, "p31-deploy");
        var deploy = await Plan(GameActions.Deploy, scope, 485, "russian", new
        {
            squadId = "r-squad-10"
        });

        Assert.Equal(GamePlanStatus.Refused, deploy.Status);
        Assert.Contains(deploy.Reasons, reason => reason.StartsWith("play.deploy-russian:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AFreedSmcIsArmedByTheNextAction()
    {
        // A20.551 (ruling R31.8, the user's of 2026-10-04). At revision 485 the German 9-2, freed in Turn 4 when his Guard was eliminated, is free and
        // Unarmed, as the game was played; the next action arms him.
        var scope = Cut(485, "p31-armed");
        var before = Planner().Replay(store.Read(scope)!.Events).Current!;
        var leader = before.Unit("g-leader-9-2-1")!;
        Assert.Equal(ConditionState.True, GameState.Condition(leader, Conditions.Unarmed));
        Assert.Null(leader.Custodian);

        var end = await Plan(GameActions.AdvancePhase, scope, 485, "russian", new
        {
        });
        Assert.Equal(GamePlanStatus.Ready, end.Status);
        var armed = Assert.Single(end.Events.Select(item => item.Payload).OfType<ConditionsChanged>(), change => change.Id == "g-leader-9-2-1");
        Assert.Equal(ConditionState.False, armed.Conditions[Conditions.Unarmed]);
        Assert.Contains(end.Reasons, reason => reason.StartsWith("play.smc-armed:", StringComparison.Ordinal));
    }

    [Fact]
    public void AnAdvanceFromAnUpperLevelIsOfferedNoGroundLevelLocation()
    {
        // Play test P-03 (B23.4, B23.421, B23.422). Revision 384: the Russian APh. The page offered the squads at G4 level 1 six ground-level
        // Locations, each of which the gate refused. The list now holds only what the movement rules accept from there, so no ground-level Location;
        // on the real board it is F3 and G3 at level 1 (checked in the Studio; the test board does not join the levels of its buildings).
        var scope = Cut(384, "p31-advance");
        var planner = Planner();
        var state = planner.Replay(store.Read(scope)!.Events).Current!;
        var upper = state.Location("r-squad-16")!.Location;
        Assert.Equal(1, upper.Level);
        Assert.DoesNotContain(planner.AdvanceLocations(state, upper), to => to.Level == 0);

        // From ground level the ADJACENT ground-level Locations are offered, as before.
        var ground = state.Location("r-leader-9-1-1")!.Location;
        Assert.Equal(0, ground.Level);
        Assert.NotEmpty(planner.AdvanceLocations(state, ground));
        Assert.All(planner.AdvanceLocations(state, ground), to => Assert.Equal(0, to.Level));
    }
}
