using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 31d (designs D5, D11, and D12): the records as each view reads them on The Tractor Works played to its end (the fixture
/// <c>p31c-tw.game.json</c>: 716 revisions, Dummies and "?" on both sides, prisoners, routs of two hexes). A Dummy stack's removal is said to both
/// sides (A12.11, A12.14, A11.19; ruling R31d.3); a result that changed nothing names who passed; a rout is one sentence; and what a view missed
/// ends where the view came back.
/// </summary>
public sealed partial class PlayRecordsPass31dTests : IDisposable
{
    private const string Played = "p31c-tw";
    private static readonly string[] Sides = ["german", "russian"];
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-records-31d-" + Guid.NewGuid().ToString("N"));
    private readonly GameLibrary games;
    private readonly Lazy<GameHistory> history;

    public PlayRecordsPass31dTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var maps = new MapService(options, new FakeVaslMapSource());
        var library = new UnitLibrary(options);
        var boards = new SyntheticBoards(maps);
        games = new GameLibrary(library, boards, new LivePlay(library, boards));
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

    private PlayRecords Records(string view, long last) => new(History, new Perspective(view), last, games.NamesOf(History, new Perspective(view)));

    [GeneratedRegex(@"= (\d+) = Final DR \1\b")]
    private static partial Regex SameDrTwice();

    [GeneratedRegex(@": (PTC|NMC|\dMC)\.?$")]
    private static partial Regex ResultAlone();

    [Fact]
    public void ThePlayedGameReplaysWholeWithItsRevisions()
    {
        Assert.False(History.HasErrors);
        Assert.Equal(716, History.States.Count);
        Assert.NotNull(History.Current!.Ended);
    }

    [Fact]
    public void ADummyStackRemovedByItsMoveIsSaidOnceToBothSides()
    {
        // Revisions 188 to 191: three German Dummies move without Assault Movement in Russian sight, and are removed (A12.11).
        foreach (var side in Sides)
        {
            var lines = Records(side, 191).Lines.Where(line => line.Revision is >= 188 and <= 191).Select(line => line.Text).ToArray();
            var removed = Assert.Single(lines, line => line.Contains("Dummy stack is removed", StringComparison.Ordinal));
            Assert.StartsWith("A German Dummy stack is removed in [S5]", removed, StringComparison.Ordinal);
            Assert.Contains("(A12.11)", removed, StringComparison.Ordinal);
            Assert.DoesNotContain("g-dummy", removed, StringComparison.Ordinal);
        }

        // The other side reads no count of the counters and no id; the owner reads its own Dummies in the move's line.
        Assert.DoesNotContain(Records("russian", 191).Lines.Where(line => line.Revision is >= 188 and <= 191), line => line.Text.Contains("Dummy G", StringComparison.Ordinal));
    }

    [Fact]
    public void DummiesRemovedByFireAreSaidInTheFiresLine()
    {
        // Revision 144: a German group fires at [W4], where a Russian Dummy lay with two concealed squads; the result removes it (A12.14).
        foreach (var side in Sides)
        {
            var fire = Assert.Single(Records(side, 160).Lines, line => line.Revision == 144 && line.Text.Contains(" at [W4]", StringComparison.Ordinal));
            Assert.Contains("the Dummies there are removed (A12.14)", fire.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("r-dummy", fire.Text, StringComparison.Ordinal);
            Assert.DoesNotContain(": eliminated", fire.Text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DummiesInALocationEnteredForCloseCombatAreSaidAtThePhasesStart()
    {
        // A11.19: the game removes them as it applies the phase change and records no event, so the line is read from the two states.
        var changes = History.Events.Where(item => item.Payload is PhaseChanged { Phase: "ccph" }).ToArray();
        var removedAt = changes.Where(item => History.At(item.Revision - 1)!.Units.Any(unit => unit.Kind == UnitKinds.Dummy && unit.Status == InstanceStatus.Active
            && History.At(item.Revision)!.Unit(unit.Id) is { Status: InstanceStatus.Eliminated })).Select(item => item.Revision).ToArray();
        Assert.NotEmpty(removedAt);
        foreach (var side in Sides)
        {
            var records = Records(side, History.States.Count);
            foreach (var revision in removedAt)
            {
                Assert.Contains(records.Lines, line => line.Revision == revision && line.Text.Contains("Dummies in [", StringComparison.Ordinal)
                    && line.Text.EndsWith("are removed before Close Combat (A11.19)", StringComparison.Ordinal));
            }

            // And at no other phase change.
            Assert.All(records.Lines.Where(line => line.Text.Contains("before Close Combat (A11.19)", StringComparison.Ordinal)), line => Assert.Contains(line.Revision, removedAt));
        }
    }

    [Fact]
    public void NoRecordSaysADrTwiceOrAResultAlone()
    {
        foreach (var view in new[] { "german", "russian", Perspective.AdjudicatorName })
        {
            var lines = Records(view, History.States.Count).Lines;
            Assert.True(lines.Count > 100, $"{view}: {lines.Count} lines");

            // "Heat of Battle DR 4, 4 = 8 = Final DR 8" said the DR twice when no DRM applied.
            Assert.DoesNotContain(lines, line => SameDrTwice().IsMatch(line.Text));

            // "...: PTC." stood alone when every unit passed; it now names who passed, for the units the view may name.
            Assert.DoesNotContain(lines, line => ResultAlone().IsMatch(line.Text));
            Assert.Contains(lines, line => line.Text.Contains(": passed", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ARoutOfSeveralHexesIsOneSentenceThatNamesItsUnitOnce()
    {
        var records = Records(Perspective.AdjudicatorName, History.States.Count);
        var routs = History.Events.Where(item => item.Payload is RoutStepped).GroupBy(item => (ReplaySteps.AttemptOf(item.EventId), ((RoutStepped)item.Payload).Unit)).ToArray();
        Assert.Contains(routs, rout => rout.Count() > 1);
        foreach (var rout in routs)
        {
            var said = records.Lines.Where(line => rout.Any(step => step.EventId == line.EventId) && line.Text.Contains(" routs ", StringComparison.Ordinal)).ToArray();
            var line = Assert.Single(said);
            if (rout.Count() > 1)
            {
                Assert.Contains(" by [", line.Text, StringComparison.Ordinal);
                Assert.Single(Regex.Matches(line.Text, " routs "));
            }
        }
    }

    [Fact]
    public void WhatAViewMissedEndsAtTheRevisionItCameBackAt()
    {
        // The German view leaves at revision 227 and comes back at 250, then acts: what it missed is 228 to 250, and none of what follows.
        var later = Records("german", 300);
        var missed = later.Since(227, 250);
        Assert.NotEmpty(missed);
        Assert.All(missed, line => Assert.InRange(line.Revision, 228, 250));
        Assert.Equal(Records("german", 250).Since(227).Select(line => line.Text), missed.Select(line => line.Text));
        Assert.True(later.Since(227).Count > missed.Count);
    }
}
