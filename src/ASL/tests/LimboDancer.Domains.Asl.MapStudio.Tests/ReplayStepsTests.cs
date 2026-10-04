using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 31b (the Replay page): a game's events grouped into the steps of its replay (<see cref="ReplaySteps"/>), what a view saw change over a step
/// (<see cref="ReplayDiff"/>), and the records read through a revision (<see cref="PlayRecords"/>), on the game of The Guards Counterattack played to
/// its end through the Play page on 2026-10-04 (613 revisions, the Play tests' fixture <c>guards-dl-01.game.json</c>). Rulings R31b.1 and R31b.2: a
/// view's steps, their count, their titles, and their changes read that view, never the full state.
/// </summary>
public sealed partial class ReplayStepsTests : IDisposable
{
    private const string Played = "guards-dl-01";
    private static readonly string[] Sides = ["german", "russian"];
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-replay-" + Guid.NewGuid().ToString("N"));
    private readonly GameLibrary games;
    private readonly MapService maps;
    private readonly Lazy<GameHistory> history;

    public ReplayStepsTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        maps = new MapService(options, new FakeVaslMapSource());
        var library = new UnitLibrary(options);
        var boards = new SyntheticBoards(maps);
        games = new GameLibrary(library, boards, new LivePlay(library, boards));

        // The played game is read as the live game it is. The test boards have no bd01, so its positions are not checked, and it replays all the same.
        var folder = Path.Combine(library.UnitsRoot, "live", LivePlay.Tenant.ToString("N"));
        Directory.CreateDirectory(folder);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", Played + ".game.json"), Path.Combine(folder, Played + ".game.json"));
        history = new Lazy<GameHistory>(() => games.Load(GameLibrary.LivePrefix + Played).History!);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private GameHistory History => history.Value;

    private ReplayTimeline Timeline(string view, GameHistory? of = null)
    {
        var (game, perspective) = (of ?? History, new Perspective(view));
        return ReplaySteps.Read(game, games.ViewOf(game, game.States.Count, perspective), revision => games.ViewOf(game, revision, perspective));
    }

    private IReadOnlyList<ReplayChange> Changes(string view, ReplayStep step)
    {
        var perspective = new Perspective(view);
        var after = games.ViewOf(History, step.Last, perspective);
        return ReplayDiff.Of(step.First > 1 ? games.ViewOf(History, step.First - 1, perspective) : null, after, [.. after.Events.Where(item => item.Revision >= step.First)]);
    }

    private IReadOnlyList<string> Lines(string view, ReplayStep step) => ReplayDiff.Lines(Changes(view, step));

    private ReplayStep StepOf(ReplayTimeline timeline, Func<EventPayload, bool> holds) =>
        timeline.Steps.First(step => History.Events.Any(item => item.Revision >= step.First && item.Revision <= step.Last && holds(item.Payload)));

    [Fact]
    public void ThePlayedGameGivesOneStepForEachAttempt()
    {
        Assert.False(History.HasErrors, string.Join("; ", History.Diagnostics.Where(item => item.Severity == LimboDancer.Domains.Asl.Units.UnitDiagnosticSeverity.Error).Take(3)));
        Assert.Equal(613, History.States.Count);
        var timeline = Timeline(Perspective.AdjudicatorName);
        Assert.Equal(History.Events.Select(item => ReplaySteps.AttemptOf(item.EventId)).Distinct(StringComparer.Ordinal).Count(), timeline.Steps.Count);
        Assert.Equal(221, timeline.Steps.Count);

        // The steps cover every revision once, in order, and are numbered from 1.
        Assert.Equal(1, timeline.Steps[0].First);
        Assert.Equal(613, timeline.Steps[^1].Last);
        Assert.All(timeline.Steps.Zip(timeline.Steps.Skip(1)), pair => Assert.Equal(pair.First.Last + 1, pair.Second.First));
        Assert.Equal(Enumerable.Range(1, 221), timeline.Steps.Select(step => step.Number));
        Assert.All(timeline.Steps, step => Assert.Equal(ReplayRead.Full, step.Read));

        // The same game gives the same steps each time.
        Assert.Equal(timeline.Steps.Select(step => (step.First, step.Last, step.Title)), Timeline(Perspective.AdjudicatorName).Steps.Select(step => (step.First, step.Last, step.Title)));
    }

    [Fact]
    public void TheStepsFollowTheGamesTurnsAndPhases()
    {
        var timeline = Timeline(Perspective.AdjudicatorName);
        Assert.Equal(81, timeline.Phases.Count);
        Assert.Equal(timeline.Steps.Count, timeline.Phases.Sum(phase => phase.Count));

        // Setup comes first: the start, each side's setup, and the non-OB "?"; play then runs from Turn 1 to the end in Turn 5.
        Assert.Equal((ReplayStep.SetupPhase, 1, 4), (timeline.Phases[0].Phase, timeline.Phases[0].FirstStep, timeline.Phases[0].Count));
        Assert.Equal("Setup", timeline.Phases[0].Label);
        Assert.Equal((1, "rph", "Russian Player Turn: Rally Phase"), (timeline.Phases[1].Turn, timeline.Phases[1].Phase, timeline.Phases[1].Label));
        var turns = timeline.Steps.Where(step => !step.IsSetup).Select(step => step.Turn).ToArray();
        Assert.Equal(turns.Order(), turns);
        Assert.Equal(5, turns[^1]);
        Assert.True(timeline.Steps[^1].EndsGame);
        Assert.Equal("end", timeline.Steps[^1].Kind);
        Assert.Single(timeline.Steps, step => step.EndsGame);
    }

    [Fact]
    public void TitlesAreInAPlayersWordsAndNameNoUnit()
    {
        var timeline = Timeline(Perspective.AdjudicatorName);
        Assert.Equal("The game starts: The Guards Counterattack", timeline.Steps[0].Title);
        Assert.Equal("The German side sets up: 26 counters", timeline.Steps[1].Title);
        Assert.Equal("The Russian side places non-OB \"?\" on 4 units", timeline.Steps[3].Title);
        Assert.Equal("Russian Prep Fire: [F3], level 1 at [F5], level 1: 2MC", timeline.Steps[6].Title);
        Assert.Equal("German Defensive First Fire: [J4], level 1 at [H3]: 1MC", timeline.Steps[10].Title);
        Assert.Equal("The German side, the DEFENDER, declines First Fire", timeline.Steps[11].Title);
        Assert.Equal("The Russian Close Combat Phase ends; the German Player Turn begins", timeline.Steps[31].Title);
        Assert.Equal("The German Close Combat Phase ends; Game Turn 2 begins", timeline.Steps[48].Title);
        Assert.Equal("The game ends: German win", timeline.Steps[^1].Title);
        Assert.Contains(timeline.Steps, step => step.Title.EndsWith("by Assault Movement", StringComparison.Ordinal));
        Assert.Contains(timeline.Steps, step => step.Title.StartsWith("A German Self-Rally attempt in ", StringComparison.Ordinal));
        Assert.Contains(timeline.Steps, step => step.Title.EndsWith(": no effect", StringComparison.Ordinal));

        // A fire's step carries its two Locations for the map; no title of any view holds a unit's id.
        Assert.Equal(("bd01:F3:1", "bd01:F5:1"), (timeline.Steps[6].Fires[0].From.ToString(), timeline.Steps[6].Fires[0].To.ToString()));
        foreach (var view in Sides.Append(Perspective.AdjudicatorName))
        {
            Assert.All(Timeline(view).Steps, step => Assert.DoesNotMatch(UnitId(), step.Title));
        }
    }

    [Fact]
    public void ASidesViewHasItsOwnSteps()
    {
        var adjudicator = Timeline(Perspective.AdjudicatorName);
        var german = Timeline("german");
        var russian = Timeline("russian");
        var nonOb = adjudicator.Steps[3];

        // The Russian non-OB "?" is the Russian side's to read. The German side reads none of it, and sees "?" placed on the map (A12.12), so its
        // step says only that (ruling R31b.2).
        Assert.Equal(nonOb.Title, russian.Steps.Single(step => step.Attempt == nonOb.Attempt).Title);
        var seen = german.Steps.Single(step => step.Attempt == nonOb.Attempt);
        Assert.Equal((ReplayRead.None, "unread", "The other side acts out of this view's sight, and the map shows a change"), (seen.Read, seen.Kind, seen.Title));
        Assert.Empty(seen.Fires);
        Assert.Equal(["r-squad-19 went under \"?\" in [G3]", "r-squad-9 went under \"?\" in [N2]"], Lines("german", seen));

        // The other side's setup is said without its count of counters, and is not marked as read in part: the mark is fire's alone.
        Assert.Equal("The Russian side sets up", german.Steps[2].Title);
        Assert.Equal("The German side sets up", russian.Steps[1].Title);
        Assert.Equal("The German side sets up: 26 counters", german.Steps[1].Title);
        Assert.All(german.Steps.Concat(russian.Steps), step => Assert.True(step.Read != ReplayRead.Part || step.Kind == "fire"));

        // Each view counts its own steps from 1.
        Assert.Equal(Enumerable.Range(1, german.Steps.Count), german.Steps.Select(step => step.Number));
    }

    [Fact]
    public void AMinorStepIsASidesStepOnlyWhenItsViewSeesAChange()
    {
        // Ruling R31b.2 (the referee's review): a weapon changing hands with nothing for a side to see, as between two units under "?", is no step
        // of that side, so it learns neither that it was done nor when. Here a DEFENDER's pass, which changes nothing any view sees, is made such an event.
        var pass = Timeline(Perspective.AdjudicatorName).Steps.First(step => step.Kind == "pass" && step.First == step.Last);
        var weapon = History.At(pass.First)!.Equipment.First(item => item.Holding is not null);
        GameEvent[] events = [.. History.Events.Select(item => item.Revision != pass.First ? item
            : new GameEvent(item.Scope, item.EventId, item.Revision, item.Time, item.Source, "equipment-transferred", new EquipmentTransferred(weapon.Id, weapon.Holding, null), null, [], null))];
        var changed = new GameHistory(events, History.States, History.Diagnostics);

        var adjudicator = Timeline(Perspective.AdjudicatorName, changed);
        var step = adjudicator.Steps.Single(item => item.Attempt == pass.Attempt);
        Assert.EndsWith("weapon changes hands", step.Title, StringComparison.Ordinal);
        Assert.Equal(221, adjudicator.Steps.Count);
        foreach (var side in Sides)
        {
            Assert.DoesNotContain(Timeline(side, changed).Steps, item => item.Attempt == pass.Attempt);
            Assert.Equal(Timeline(side).Steps.Count - 1, Timeline(side, changed).Steps.Count);
        }
    }

    [Fact]
    public void TheJumpsGoByPhaseAndByGameTurn()
    {
        var timeline = Timeline(Perspective.AdjudicatorName);

        // Steps 7 to 9 are the Turn 1 Russian Prep Fire Phase; step 10 starts its Movement Phase; step 5 starts Turn 1 and step 50 Turn 2.
        Assert.Equal(10, timeline.NextPhase(7));
        Assert.Equal(10, timeline.NextPhase(9));
        Assert.Equal(7, timeline.PreviousPhase(9));
        Assert.Equal(7, timeline.PreviousPhase(10));
        Assert.Equal(5, timeline.PreviousPhase(7));
        Assert.Equal(50, timeline.NextTurn(10));
        Assert.Equal(5, timeline.NextTurn(2));
        Assert.Equal(5, timeline.PreviousTurn(49));
        Assert.Equal(5, timeline.PreviousTurn(50));
        Assert.Equal(1, timeline.PreviousTurn(5));

        // At the ends the jumps stay inside the steps.
        Assert.Equal(1, timeline.PreviousPhase(1));
        Assert.Equal(221, timeline.NextPhase(221));
        Assert.Equal(221, timeline.NextTurn(220));
        Assert.Equal(11, timeline.StepAt(84));
        Assert.Equal(10, timeline.StepAt(79));
        Assert.Equal(1, timeline.StepAt(0));
        Assert.Equal(221, timeline.Step(500)!.Number);
        Assert.Null(ReplayTimeline.Empty.Step(1));
    }

    [Fact]
    public void WhatChangedIsSaidFromTheViewAlone()
    {
        var adjudicator = Timeline(Perspective.AdjudicatorName);

        // A move that loses "?": the adjudicator and the unit's own side read the unit moving; the other side reads a "?" gone and a unit come.
        var move = adjudicator.Steps[9];
        Assert.Equal(["r-squad-19 moved from [G3] to [H3]", "r-squad-19 lost its \"?\" in [H3]"], Lines(Perspective.AdjudicatorName, move));
        var moved = Changes(Perspective.AdjudicatorName, move)[0];
        Assert.Equal((ReplayChange.Moved, "bd01:G3:0", "bd01:H3:0"), (moved.Kind, moved.From!.ToString(), moved.At!.ToString()));
        var german = Timeline("german");
        Assert.Equal(["r-squad-19 came into this view in [H3]", "A Russian \"?\" is gone from [G3]"], Lines("german", german.Steps.Single(step => step.Attempt == move.Attempt)));

        // A fire that breaks a squad and pins a leader: the Replacement says why, with what the new unit holds, and the SW follows it.
        Assert.Equal(
            [
                "g-squad-1 was Replaced by fire-a672887feaab-g-squad-1 (broken, dm) in [F5], level 1",
                "g-leader-9-1-1 was pinned in [F5], level 1",
                "g-lmg-1 passed from g-squad-1 to fire-a672887feaab-g-squad-1 in [F5], level 1",
            ],
            Lines(Perspective.AdjudicatorName, adjudicator.Steps[6]));

        // A capture, and an elimination.
        Assert.Contains("g-leader-9-2-1 was captured in [H4]", Lines(Perspective.AdjudicatorName, StepOf(adjudicator, payload => payload is InstanceCaptured)));
        Assert.Contains(Lines(Perspective.AdjudicatorName, StepOf(adjudicator, payload => payload is InstanceEliminated)), line => line.Contains(" was eliminated", StringComparison.Ordinal));

        // A setup's many counters are counted, not listed; the start of the game changes nothing.
        Assert.Matches("^[0-9]+ counters came into this view$", Assert.Single(Lines(Perspective.AdjudicatorName, adjudicator.Steps[2])));
        Assert.Empty(Lines(Perspective.AdjudicatorName, adjudicator.Steps[0]));

        // No line of a side's view names a unit that view does not hold just before or just after the step.
        foreach (var side in Sides)
        {
            var perspective = new Perspective(side);
            foreach (var step in Timeline(side).Steps.Where(step => step.First > 1))
            {
                var held = games.ViewOf(History, step.First - 1, perspective).Units.Concat(games.ViewOf(History, step.Last, perspective).Units).Select(unit => unit.Id).ToHashSet(StringComparer.Ordinal);
                var enemy = History.At(step.Last)!.Units.Where(unit => unit.Side != side && !held.Contains(unit.Id)).Select(unit => unit.Id).ToArray();
                Assert.All(Lines(side, step), line => Assert.DoesNotContain(enemy, id => Names(line, id)));
            }
        }
    }

    /// <summary>Whether a line holds an id as a whole word, so "r-squad-1" is not found inside "r-squad-19".</summary>
    private static bool Names(string line, string id)
    {
        static bool Part(char letter) => char.IsLetterOrDigit(letter) || letter == '-';
        for (var at = line.IndexOf(id, StringComparison.Ordinal); at >= 0; at = line.IndexOf(id, at + 1, StringComparison.Ordinal))
        {
            var end = at + id.Length;
            if ((at == 0 || !Part(line[at - 1])) && (end == line.Length || !Part(line[end])))
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void TheMarksAreAnArrowForAMoveALineForFireAndAnOutlineForTheRest()
    {
        var board = FakeBoardProvider.Board;
        BoardLocation[] hexes = [.. board.Render.Grid.Geometry.Hexes().Select(index => GameMaps.LocationOf(board, index)).OfType<BoardLocation>().Take(3)];
        Assert.Equal(3, hexes.Length);
        var marks = ReplayDiff.Marks(board,
            [
                new ReplayChange(ReplayChange.Moved, "a move", hexes[1], hexes[0]),
                new ReplayChange(ReplayChange.Moved, "the same move by another unit", hexes[1], hexes[0]),
                new ReplayChange(ReplayChange.Moved, "a move inside a hex", hexes[2] with { Level = 1 }, hexes[2]),
                new ReplayChange(ReplayChange.Condition, "a change", hexes[2]),
            ],
            [new ReplayFire(hexes[0], hexes[2]), new ReplayFire(hexes[1], hexes[1])]);
        var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument($"<body><svg>{marks}</svg></body>");

        // Units moving together share one arrow; a move between the levels of one hex is an outline, and so is a change; a fire inside one hex too.
        Assert.Single(document.QuerySelectorAll("g.replay-move"));
        Assert.Equal((hexes[0].ToString(), hexes[1].ToString()), (document.QuerySelector("g.replay-move")!.GetAttribute("data-from"), document.QuerySelector("g.replay-move")!.GetAttribute("data-location")));
        Assert.Single(document.QuerySelectorAll("g.replay-change"));
        Assert.Equal(2, document.QuerySelectorAll("g.replay-fire").Length);
        Assert.Single(document.QuerySelectorAll("g.replay-fire polygon[stroke-dasharray]"));
        Assert.All(document.QuerySelectorAll("svg > g"), mark => Assert.Equal("none", mark.GetAttribute("pointer-events")));
        Assert.Equal(string.Empty, ReplayDiff.Marks(board, [new ReplayChange(ReplayChange.Condition, "off this map", new BoardLocation(BoardRef.Parse("bd99"), hexes[0].Hex, 0))], []));
    }

    [Fact]
    public void TheRecordsThroughARevisionAreTheGamesOwnUpToIt()
    {
        // Design D7: Play reads the records at the game's last revision and Replay shows a step's; a record reads the same wherever it is read from.
        var whole = new PlayRecords(History, Perspective.Adjudicator, 613);
        var part = new PlayRecords(History, Perspective.Adjudicator, 200);
        var revisions = History.Events.ToDictionary(item => item.EventId, item => item.Revision, StringComparer.Ordinal);
        Assert.Equal(49, whole.Fire.Count);
        Assert.Equal(whole.Fire.Where(fire => revisions[fire.EventId] <= 200).Select(fire => (fire.EventId, fire.Group, fire.Target, fire.Arithmetic.Result)),
            part.Fire.Select(fire => (fire.EventId, fire.Group, fire.Target, fire.Arithmetic.Result)));
        Assert.Equal(whole.RallyAndRepair.Where(item => revisions[item.EventId] <= 200).Select(item => item.Text).Order(StringComparer.Ordinal),
            part.RallyAndRepair.Select(item => item.Text).Order(StringComparer.Ordinal));
        Assert.Equal(whole.CloseCombat.Where(item => revisions[item.EventId] <= 200), part.CloseCombat);
        Assert.Equal(whole.WhenOf(whole.Fire[^1].EventId), part.WhenOf(whole.Fire[^1].EventId));
        Assert.Equal("Turn 1, Russian Prep Fire Phase", whole.WhenOf(whole.Fire[^1].EventId));

        // Pass 31c (design D17): the latest line is the last attempt that did something, under a heading that names the side; the game's end
        // is one. Read with no names, a record keeps its identifiers, which the page puts in words.
        Assert.Equal("Latest: Turn 5, German Close Combat Phase: The game ends.", whole.Latest);
        Assert.StartsWith("Latest: Turn 5, German Close Combat Phase: CC in bd01:H4:0", new PlayRecords(History, Perspective.Adjudicator, 612).Latest, StringComparison.Ordinal);

        // Every fire record belongs to one fire step, and a step's rolls are those of its own revisions.
        var timeline = Timeline(Perspective.AdjudicatorName);
        Assert.All(whole.Fire, fire => Assert.Equal("fire", timeline.Steps.Single(step => step.Attempt == ReplaySteps.AttemptOf(fire.EventId)).Kind));
        Assert.Equal(3, whole.RollsIn(timeline.Steps[6].First, timeline.Steps[6].Last).Count);
        Assert.Empty(whole.RollsIn(timeline.Steps[8].First, timeline.Steps[8].Last));
        Assert.True(whole.RollRows.Count <= 6);
    }

    [GeneratedRegex(@"\b[gr]-(squad|leader|lmg|mmg|hmg|hs|crew)\b|[a-z]+-[0-9a-f]{12}")]
    private static partial Regex UnitId();
}
