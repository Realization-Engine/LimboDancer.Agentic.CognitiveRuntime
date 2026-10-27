using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnWorkDisclosureTests
{
    [Fact]
    public void AdjudicatedCompletionDoesNotPromotePendingRulesOrRevealEvidence()
    {
        var declaration = new TurnWorkDeclaration("work", "side", "gun", ["gun", "crew"], TurnWorkKind.AnimalPacking, null, 1, "private adjudication");
        var live = TurnWorkLifecycle.Start(declaration, 1, true);
        live = TurnWorkLifecycle.NextGameTurn(live, 1, 2);
        var due = TurnWorkDisclosure.From(live, true);
        Assert.True(due.CompletionDue);
        Assert.Equal(1, due.CompletedGameTurns);
        Assert.Equal(1, due.RequiredGameTurns);
        Assert.Equal("work", due.Id);
        Assert.Equal("gun", due.Subject);
        Assert.Equal(TurnWorkKind.AnimalPacking, due.Kind);
        Assert.True(due.Working);
        Assert.False(due.Completed);
        Assert.False(due.Cancelled);
        Assert.False(due.SupplyDepleted);
        Assert.Equal(TurnSupport.Pending, due.RuleAutomation);
        Assert.DoesNotContain("private adjudication", System.Text.Json.JsonSerializer.Serialize(due), StringComparison.Ordinal);
        var done = TurnWorkDisclosure.From(TurnWorkLifecycle.Change(live, "complete", 2, TurnWorkWindow.FirstRally), false);
        Assert.True(done.Completed);
        Assert.False(done.Working);
        Assert.False(done.CompletionDue);
        Assert.Throws<ArgumentNullException>(() => TurnWorkDisclosure.From(null!, false));
    }

    [Fact]
    public void EffectsAreDistinguishedFromLegacyLifecycleCompletion()
    {
        var declaration = new TurnWorkDeclaration("work", "side", "gun", ["gun", "crew"], TurnWorkKind.AnimalPacking, null, 1, "ruling");
        var legacy = TurnWorkLifecycle.Start(declaration, 1, true);
        Assert.Equal(TurnWorkEffectStatus.NotSpecified, TurnWorkDisclosure.From(legacy, false).EffectStatus);
        Assert.Null(TurnWorkDisclosure.From(legacy, false).EffectKind);
        var bound = TurnWorkLifecycle.Start(declaration with
        {
            CompletionEffects = new(TurnWorkEffectKind.PackGun, Crew: "crew")
        }, 1, true);
        Assert.Equal(TurnWorkEffectKind.PackGun, TurnWorkDisclosure.From(bound, false).EffectKind);
        Assert.Equal(TurnWorkEffectStatus.AwaitingCompletion, TurnWorkDisclosure.From(bound, false).EffectStatus);
        var cancelled = TurnWorkLifecycle.Change(bound, "cancel", 1, TurnWorkWindow.FirstRally);
        Assert.Equal(TurnWorkEffectStatus.Cancelled, TurnWorkDisclosure.From(cancelled, false).EffectStatus);
        var completed = TurnWorkLifecycle.Change(TurnWorkLifecycle.NextGameTurn(bound, 1, 2), "complete", 2, TurnWorkWindow.FirstRally);
        var view = TurnWorkDisclosure.From(completed with
        {
            EffectsApplied = true
        }, false);
        Assert.Equal(TurnWorkEffectStatus.Applied, view.EffectStatus);
        Assert.Equal(TurnSupport.Pending, view.RuleAutomation);
    }

    [Theory]
    [InlineData(TurnWorkEffectKind.TransferSupply)]
    [InlineData(TurnWorkEffectKind.OffloadSupply)]
    public void RelocationDisclosureKeepsEligibilityPendingAndDestinationsPrivate(TurnWorkEffectKind kind)
    {
        var offload = kind == TurnWorkEffectKind.OffloadSupply;
        var declaration = new TurnWorkDeclaration("work", "side", "source", offload ? ["source"] : ["source", "private-target"],
            TurnWorkKind.AmmoTransfer, null, null, "private-basis")
        {
            CompletionEffects = new(kind, SupplyId: "private-supply", SupplyVehicle: "source",
                DestinationVehicle: offload ? null : "private-target", DumpLocation: offload ? "private-location" : null),
        };
        var work = TurnWorkLifecycle.Start(declaration, 1, true);
        var view = TurnWorkDisclosure.From(work, false);
        Assert.Equal(kind, view.EffectKind);
        Assert.Equal(TurnWorkEffectStatus.AwaitingCompletion, view.EffectStatus);
        Assert.Equal(4, view.RequiredGameTurns);
        for (var turn = 1; turn < 4; turn++)
        {
            work = TurnWorkLifecycle.NextGameTurn(work, turn, turn + 1);
        }
        work = TurnWorkLifecycle.Change(work, "complete", 4, TurnWorkWindow.SecondCloseCombat) with
        {
            EffectsApplied = true
        };
        view = TurnWorkDisclosure.From(work, false);
        Assert.Equal(TurnWorkEffectStatus.Applied, view.EffectStatus);
        Assert.Equal(TurnSupport.Pending, view.RuleAutomation);
        Assert.DoesNotContain("private-", System.Text.Json.JsonSerializer.Serialize(view), StringComparison.Ordinal);
    }
}
