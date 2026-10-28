using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnWorkLifecycleTests
{
    private static TurnWorkDeclaration Ammo => new("work", "side", "vehicle", ["vehicle", "supply"], TurnWorkKind.AmmoReplenishment, 10, null, "E10.3 adjudication");

    [Theory]
    [InlineData(2, false)]
    [InlineData(12, true)]
    public void ReplenishmentRetainsPriorTurnsAndRequiresRecordedDrAtSecondCc(int dr, bool depleted)
    {
        var work = TurnWorkLifecycle.Start(Ammo, 1, true);
        Assert.False(TurnWorkLifecycle.CompletionDue(work, 1, TurnWorkWindow.SecondCloseCombat));
        work = TurnWorkLifecycle.NextGameTurn(work, 1, 2);
        work = TurnWorkLifecycle.Change(work, "interrupt", 2, TurnWorkWindow.Unknown);
        Assert.Equal(1, work.CompletedGameTurns);
        Assert.Same(work, TurnWorkLifecycle.NextGameTurn(work, 2, 3));
        work = TurnWorkLifecycle.Change(work, "resume", 3, TurnWorkWindow.FirstRally);
        Assert.False(TurnWorkLifecycle.CompletionDue(work, 3, TurnWorkWindow.FirstRally));
        Assert.True(TurnWorkLifecycle.CompletionDue(work, 3, TurnWorkWindow.SecondCloseCombat));
        Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Change(work, "complete", 3, TurnWorkWindow.SecondCloseCombat));
        var done = TurnWorkLifecycle.Change(work, "complete", 3, TurnWorkWindow.SecondCloseCombat, dr);
        Assert.True(done.Completed);
        Assert.Equal(2, done.CompletedGameTurns);
        Assert.Equal(depleted, done.SupplyDepleted);
        Assert.False(TurnWorkLifecycle.CompletionDue(done, 3, TurnWorkWindow.SecondCloseCombat));
        Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Change(done, "complete", 3, TurnWorkWindow.SecondCloseCombat, dr));
    }

    [Fact]
    public void PackingUsesExplicitAdjudicatedDurationAndFollowingFirstRally()
    {
        var declaration = Ammo with
        {
            Kind = TurnWorkKind.AnimalPacking,
            OriginalBNumber = null
        };
        Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Start(declaration, 1, true));
        var work = TurnWorkLifecycle.Start(declaration with
        {
            PackingGameTurns = 1
        }, 1, true);
        Assert.False(TurnWorkLifecycle.CompletionDue(work, 1, TurnWorkWindow.SecondCloseCombat));
        work = TurnWorkLifecycle.NextGameTurn(work, 1, 2);
        Assert.True(TurnWorkLifecycle.CompletionDue(work, 2, TurnWorkWindow.FirstRally));
        Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Change(work, "complete", 2, TurnWorkWindow.FirstRally, 2));
        Assert.True(TurnWorkLifecycle.Change(work, "complete", 2, TurnWorkWindow.FirstRally).Completed);
    }

    [Fact]
    public void TransferRequiresFourWholeTurnsAndCancellationIsTerminal()
    {
        var work = TurnWorkLifecycle.Start(Ammo with
        {
            Kind = TurnWorkKind.AmmoTransfer
        }, 1, true);
        for (var turn = 1; turn < 4; turn++)
        {
            Assert.False(TurnWorkLifecycle.CompletionDue(work, turn, TurnWorkWindow.SecondCloseCombat));
            work = TurnWorkLifecycle.NextGameTurn(work, turn, turn + 1);
        }
        Assert.True(TurnWorkLifecycle.Change(work, "complete", 4, TurnWorkWindow.SecondCloseCombat).Completed);
        var cancelled = TurnWorkLifecycle.Change(work, "cancel", 4, TurnWorkWindow.Unknown);
        Assert.True(cancelled.Cancelled);
        Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Change(cancelled, "resume", 5, TurnWorkWindow.FirstRally));
    }

    [Fact]
    public void InvalidDeclarationsAndTransitionsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => TurnWorkLifecycle.Start(null!, 1, true));
        Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Start(Ammo, 1, false));
        TurnWorkDeclaration[] bad = [Ammo with { Id = "" }, Ammo with { Owner = "" }, Ammo with { RuleBasis = "" },
            Ammo with { Subject = "missing" }, Ammo with { Participants = ["vehicle", "vehicle"] },
            Ammo with { Participants = [] }, Ammo with { PackingGameTurns = 4 }, Ammo with { OriginalBNumber = 12 }];
        foreach (var declaration in bad)
        {
            Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Start(declaration, 1, true));
        }
        var work = TurnWorkLifecycle.Start(Ammo, 1, true);
        Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Change(work, "resume", 1, TurnWorkWindow.FirstRally));
        Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.NextGameTurn(work, 1, 3));
        Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.NextGameTurn(work, 2, 3));
        Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Change(work, "complete", 1, TurnWorkWindow.FirstRally));
    }
}
