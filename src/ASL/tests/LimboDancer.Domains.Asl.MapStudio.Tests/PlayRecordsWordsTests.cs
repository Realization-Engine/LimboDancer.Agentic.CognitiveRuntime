using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 31c (design D17; design section 9, "Records"): the records as a view reads them (<see cref="PlayRecords"/> with the view's
/// <see cref="UnitNames"/>), on the game of The Guards Counterattack played to its end (the fixture <c>guards-dl-01.game.json</c>). A record's
/// heading names the side whose phase it is; "Latest" is the last attempt that did something and gives a fire's result; and what a view missed
/// ("Since you last looked") is read from that view's own records, so it tells the view nothing its records do not.
/// </summary>
public sealed partial class PlayRecordsWordsTests : IDisposable
{
    private const string Played = "guards-dl-01";
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-records-" + Guid.NewGuid().ToString("N"));
    private readonly GameLibrary games;
    private readonly Lazy<GameHistory> history;

    public PlayRecordsWordsTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var maps = new MapService(options, new FakeVaslMapSource());
        var library = new UnitLibrary(options);
        var boards = new SyntheticBoards(maps);
        games = new GameLibrary(library, boards, new LivePlay(library, boards));

        // The played game is read as the live game it is, as ReplayStepsTests reads it.
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

    /// <summary>The records a view reads through a revision, in that view's words.</summary>
    private PlayRecords Records(string view, long last) => new(History, new Perspective(view), last, games.NamesOf(History, new Perspective(view)));

    [Fact]
    public void ARecordsHeadingNamesTheSideWhosePhaseItIs()
    {
        var records = Records("german", History.States.Count);
        Assert.True(records.Lines.Count > 200);

        // Every line of play sits under a turn, a side, and a phase; the side is the one phasing when the event was proposed.
        Assert.All(records.Lines, line =>
        {
            var when = records.WhenOf(line.EventId);
            Assert.Matches(Heading(), when);
            var before = History.At(line.Revision - 1)!;
            Assert.StartsWith($"Turn {before.Turn}, {DisplayText.Side(before.PhasingSide)} ", when, StringComparison.Ordinal);
        });
        Assert.Equal("Turn 1, Russian Prep Fire Phase", records.WhenOf(records.Lines.Single(line => line.Revision == 63).EventId));
        Assert.Equal("Turn 1, German Prep Fire Phase", records.WhenOf(records.Lines.Single(line => line.Revision == 128).EventId));
        Assert.Equal("Turn 2, Russian Rally Phase", records.WhenOf(records.Lines.Single(line => line.Revision == 152).EventId));

        // The heading is the game's, the same in every view.
        var fire = records.Fire[^1].EventId;
        Assert.Equal("Turn 1, Russian Prep Fire Phase", records.WhenOf(fire));
        Assert.Equal(records.WhenOf(fire), Records("russian", History.States.Count).WhenOf(fire));
        Assert.Equal(records.WhenOf(fire), Records(Perspective.AdjudicatorName, History.States.Count).WhenOf(fire));
    }

    [Fact]
    public void LatestGivesAFiresResultInTheViewsWords()
    {
        // The Russian Prep Fire of Turn 1 from [F3], level 1: a 2MC that pins the leader and breaks the squad, which is Replaced (A19.13). Each
        // side reads the firers by the tags its own view gives them.
        const string result = " in [F3], level 1, directed by 10-2 leader R3 fire at [F5], level 1: 2MC; 9-1 leader G1: pinned; 4-6-7 squad G1: now 4-4-7 squad, broken.";
        Assert.Equal("Latest: Turn 1, Russian Prep Fire Phase: 6-2-8 squad R7, 6-2-8 squad R18, 6-2-8 squad R19" + result, Records("german", 66).Latest);
        Assert.Equal("Latest: Turn 1, Russian Prep Fire Phase: 6-2-8 squad R13, 6-2-8 squad R14, 6-2-8 squad R15" + result, Records("russian", 66).Latest);

        // The start of the next phase does not take the fire's place: the latest line is the last attempt that did something.
        var movement = Records("german", 77);
        Assert.Equal(77, movement.Lines[^1].Revision);
        Assert.True(movement.Lines[^1].Minor);
        Assert.StartsWith("Latest: Turn 1, Russian Prep Fire Phase: 6-2-8 squad R8, 6-2-8 squad R20, 6-2-8 squad R21 in [G4], level 1 fire at [H5], level 1: NMC;", movement.Latest, StringComparison.Ordinal);

        // A move is a latest line too, and a view that does not hold the mover by name reads a concealed unit.
        Assert.Equal("Latest: Turn 1, Russian Movement Phase: A concealed unit moves from [G3] to [H3].", Records("german", 80).Latest);
        Assert.Equal("Latest: Turn 1, Russian Movement Phase: 6-2-8 squad R19 moves from [G3] to [H3].", Records("russian", 80).Latest);

        // Before play there is nothing to say.
        Assert.Null(Records("german", 52).Latest);
    }

    [Fact]
    public void WhatAViewMissedIsReadFromItsOwnRecordsAlone()
    {
        var (german, russian) = (Records("german", 95), Records("russian", 95));

        // The lines after a revision, in order, each a line of the view's own records.
        var missed = german.Since(77);
        Assert.NotEmpty(missed);
        Assert.All(missed, line => Assert.True(line.Revision > 77));
        Assert.Equal(german.Lines.Where(line => line.Revision > 77), missed);
        Assert.Equal(missed.Select(line => line.Revision).Order(), missed.Select(line => line.Revision));
        Assert.Empty(german.Since(95));

        // The same move, as each view may read it: the German side did not hold the mover by name, so its line names no unit.
        Assert.Equal("a concealed unit moves from [G3] to [H3]", missed[0].Text);
        Assert.Equal("6-2-8 squad R19 moves from [G3] to [H3]", russian.Since(77)[0].Text);
        Assert.Equal(missed.Select(line => line.Revision), russian.Since(77).Select(line => line.Revision));

        // What only one side may read is in that side's list alone: the Russian squad's Deployment attempt of its own Rally Phase.
        Assert.Contains(russian.Since(52), line => line.Revision == 58 && line.Text.StartsWith("6-2-8 squad R21 in [G3] tries to Deploy", StringComparison.Ordinal));
        Assert.DoesNotContain(german.Since(52), line => line.Revision == 58);
        Assert.DoesNotContain(german.Since(52), line => line.Text.Contains("Deploy", StringComparison.Ordinal));

        // The German list is no longer than the Russian one, and every revision in it is one the Russian side reads too over these phases.
        Assert.True(german.Since(52).Count < russian.Since(52).Count);
        Assert.Subset(russian.Since(52).Select(line => line.Revision).ToHashSet(), german.Since(52).Select(line => line.Revision).ToHashSet());
    }

    [GeneratedRegex(@"^Turn [1-5], (German|Russian) (Rally|Prep Fire|Movement|Defensive Fire|Advancing Fire|Rout|Advance|Close Combat) Phase$")]
    private static partial Regex Heading();
}
