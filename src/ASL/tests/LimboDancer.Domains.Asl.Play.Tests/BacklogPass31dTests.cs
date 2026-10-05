using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 31d of the Card Play and Map Studio Redesign Plan: the leftovers of pass 31c. The first tests hold the two ways of reading a game equal
/// (design D1): a game whose events are applied a few at a time to what was kept is, state for state, the game replayed whole. They replay The Guards
/// Counterattack as it was played (613 revisions, the fixture <c>guards-dl-01.game.json</c>).
/// </summary>
public sealed class BacklogPass31dTests : IDisposable
{
    private const string Played = "guards-dl-01";

    // The Tractor Works as it was played on 2026-10-04 (pass 31c's third play test).
    private const string Tractor = "p31c-tw";
    private static readonly Guid Tenant = Guid.Parse("5a7d1f00-0000-4000-8000-0000000057d0");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();

    private static readonly UnitCatalog[] Catalogs =
        [.. UnitCatalogs.ReplayNames.Select(name => UnitCatalogs.Read(name, Vocabulary)?.Catalog).OfType<UnitCatalog>()];

    private static readonly JsonSerializerOptions Compact = new()
    {
        WriteIndented = false
    };

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p31d-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public BacklogPass31dTests() => store = new FileGameStore(Path.Combine(root, "games"));

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // The test board carries no LOS data: clear LOS at the board's true range, with no Hindrance, as in the tests of pass 13.
    private sealed class ClearLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, Catalogs, fireLos: new ClearLos());

    private string FileOf(string name) => Path.Combine(root, "games", Tenant.ToString("N"), name + ".game.json");

    /// <summary>The played game cut back to a revision, saved under a name of its own.</summary>
    private GameScope Cut(int revision, string name, string played = Played)
    {
        var game = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", played + ".game.json")))!;
        game["game"] = name;
        var events = game["events"]!.AsArray();
        while (events.Count > revision)
        {
            events.RemoveAt(events.Count - 1);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(FileOf(name))!);
        File.WriteAllText(FileOf(name), game.ToJsonString(Compact));
        return new GameScope(Tenant, name);
    }

    private static string Text(GameState state) => JsonSerializer.Serialize(state, Compact);

    [Fact]
    public void AGameContinuedOneEventAtATimeIsTheGameReplayedWhole()
    {
        var events = store.Read(Cut(613, "p31d-whole"))!.Events;
        var whole = Planner().Replay(events);
        Assert.False(whole.HasErrors);
        Assert.Equal(613, whole.States.Count);

        // Another planner has kept nothing: each list one event longer is a continuation of the one before.
        var planner = Planner();
        GameHistory? stepped = null;
        for (var count = 1; count <= events.Count; count++)
        {
            stepped = planner.Replay([.. events.Take(count)]);
            Assert.Equal(count, stepped.States.Count);
        }

        Assert.Equal(whole.Diagnostics.Select(item => item.ToString()), stepped!.Diagnostics.Select(item => item.ToString()));
        for (var index = 0; index < whole.States.Count; index++)
        {
            Assert.Equal(Text(whole.States[index]), Text(stepped.States[index]));
        }
    }

    [Fact]
    public void TheTractorWorksContinuedPhaseByPhaseIsTheGameReplayedWhole()
    {
        // The larger game (716 revisions, 112 counters at its start, Dummies, prisoners, and surrenders): each phase's events applied to what
        // was kept give, at every phase change, the state the whole replay has there.
        var events = store.Read(Cut(716, "p31d-tractor", Tractor))!.Events;
        var whole = Planner().Replay(events);
        Assert.False(whole.HasErrors);
        Assert.Equal(716, whole.States.Count);

        var planner = Planner();
        var changes = events.Where(item => item.Payload is PhaseChanged).Select(item => (int)item.Revision).Append(events.Count).Distinct().ToArray();
        Assert.True(changes.Length > 100);
        foreach (var count in changes)
        {
            var stepped = planner.Replay([.. events.Take(count)]);
            Assert.False(stepped.HasErrors);
            Assert.Equal(Text(whole.States[count - 1]), Text(stepped.Current!));
        }
    }

    [Fact]
    public async Task EveryWayOfferedForARoutInTheTractorWorksIsARouteTheGameTakes()
    {
        // Design D3: at the start of each Rout Phase of the played game, each route the advice offers a broken unit that may rout is planned as its
        // rout. The plan may wait on another unit's rout or on the unit's surrender; it is never refused for its route.
        var events = store.Read(Cut(716, "p31d-routs", Tractor))!.Events;
        var history = Planner().Replay(events);
        var offered = 0;
        foreach (var start in events.Where(item => item.Payload is PhaseChanged { Phase: "rtph" }).Select(item => (int)item.Revision))
        {
            var state = history.States[start - 1];
            var planner = Planner();
            GameScope? scope = null;
            foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && GameState.Condition(unit, Conditions.Broken) == ConditionState.True
                && planner.MayRout(state, unit)))
            {
                foreach (var (target, route) in planner.RoutAdvice(state, unit).Routes)
                {
                    scope ??= Cut(start, $"p31d-rout-{start}", Tractor);
                    offered++;
                    Assert.Equal(target, route[^1]);
                    var plan = await planner.PlanAsync(GameActions.Rout, JsonSerializer.SerializeToElement(new
                    {
                        gameId = scope.Game,
                        attemptId = $"rout-{start}-{offered}",
                        expectedRevision = start,
                        unitId = unit.Id,
                        route = route.Select(step => step.ToString()).ToArray(),
                        lowCrawl = false,
                    }), Tenant, "tester");
                    Assert.True(plan.Status == GamePlanStatus.Ready || plan.Reasons.Any(reason => reason.StartsWith("play.rout-order", StringComparison.Ordinal)
                            || reason.StartsWith("play.rout-surrender", StringComparison.Ordinal)),
                        $"Revision {start}, {unit.Id} to {target}: {string.Join("; ", plan.Reasons)}");
                }
            }
        }

        Assert.True(offered >= 5, $"Only {offered} routes were offered in the whole game.");
    }

    [Fact]
    public void AListReplayedAgainIsNotReplayedAndACallersListIsNotKept()
    {
        var events = store.Read(Cut(300, "p31d-again"))!.Events;
        var planner = Planner();
        var first = planner.Replay(events);
        Assert.Same(first, planner.Replay(events));
        Assert.Same(first, planner.Replay([.. events]));

        // A caller that goes on adding to its own list is not handed the shorter history for the longer list.
        var all = store.Read(Cut(310, "p31d-longer"))!.Events;
        List<GameEvent> growing = [.. all.Take(300)];
        var before = planner.Replay(growing);
        Assert.Equal(300, before.States.Count);
        growing.AddRange(all.Skip(300));
        Assert.Equal(310, planner.Replay(growing).States.Count);
        Assert.Equal(300, before.States.Count);
        Assert.Equal(300, before.Events.Count);
    }

    [Fact]
    public void ACandidatesEventsLeaveWhatWasKeptAsItWasAndAnErrorIsNotBuiltOn()
    {
        var events = store.Read(Cut(613, "p31d-candidate"))!.Events;
        var planner = Planner();
        var kept = planner.Replay([.. events.Take(400)]);
        var text = Text(kept.Current!);

        // The events out of their order break the envelope: the continuation stops with its error, and the kept history has none.
        var broken = planner.Replay([.. events.Take(400), events[401]]);
        Assert.True(broken.HasErrors);
        Assert.Equal(400, broken.States.Count);
        Assert.False(kept.HasErrors);
        Assert.Equal(text, Text(kept.Current!));

        // The right events after the broken try are applied to the 400, and give the state the whole game has there.
        var right = planner.Replay([.. events.Take(402)]);
        Assert.False(right.HasErrors);
        Assert.Equal(Text(Planner().Replay(events).States[401]), Text(right.Current!));

        // Nothing follows an error: the list with more events after the broken one is replayed whole and stops at the same place.
        var after = planner.Replay([.. events.Take(400), events[401], events[402]]);
        Assert.True(after.HasErrors);
        Assert.Equal(400, after.States.Count);
    }

    [Fact]
    public void TheStoreGivesTheSameEventsForTheSameBytesAndNewOnesForAFileChanged()
    {
        var scope = Cut(200, "p31d-store");
        var first = store.Read(scope)!;
        Assert.Same(first, store.Read(scope));

        // The file written again with other bytes (here, indented) is parsed anew, and says the same.
        var node = JsonNode.Parse(File.ReadAllText(FileOf(scope.Game)))!;
        File.WriteAllText(FileOf(scope.Game), node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var second = store.Read(scope)!;
        Assert.NotSame(first, second);
        Assert.Equal(first.Events.Select(item => item.EventId), second.Events.Select(item => item.EventId));
        Assert.NotSame(first.Events[0], second.Events[0]);
    }

    [Fact]
    public void ACommitKeepsTheEventsAlreadyReadAndWhatItWroteReadsBackTheSame()
    {
        // The game one event short of a phase's end; the event is appended as the store appends any batch.
        var source = store.Read(Cut(613, "p31d-source"))!.Events;
        var scope = Cut(250, "p31d-commit");
        var planner = Planner();
        var before = store.Read(scope)!.Events;
        var next = source[250];
        GameEvent[] batch = [new GameEvent(scope, next.EventId, next.Revision, next.Time, next.Source, next.Type, next.Payload, next.RulePackage, next.Causes, next.Visibility)];
        var appended = store.Append(scope, "label", 250, batch, planner.Replay);
        Assert.Equal(AppendStatus.Committed, appended.Status);

        var after = store.Read(scope)!.Events;
        Assert.Equal(251, after.Count);
        for (var index = 0; index < before.Count; index++)
        {
            Assert.Same(before[index], after[index]);
        }

        // The referee, pass 31d: the kept record pairs the events read before with the new ones as they read back. Written again it is the
        // file, byte for byte, so the events read before are what a new parse of the file gives.
        Assert.Equal(File.ReadAllText(FileOf(scope.Game)), GameEventWriter.Write(scope, store.Read(scope)!));

        // What the store keeps is what the file says: a store that has kept nothing of this file parses the same game, state for state.
        var kept = planner.Replay(after);
        File.WriteAllText(FileOf("p31d-copy"), File.ReadAllText(FileOf(scope.Game)).Replace("\"p31d-commit\"", "\"p31d-copy\"", StringComparison.Ordinal));
        var parsed = Planner().Replay(store.Read(new GameScope(Tenant, "p31d-copy"))!.Events);
        Assert.False(kept.HasErrors);
        Assert.False(parsed.HasErrors);
        Assert.Equal(251, parsed.States.Count);
        Assert.Equal(Text(parsed.Current!).Replace("p31d-copy", "p31d-commit", StringComparison.Ordinal), Text(kept.Current!));
    }
}
