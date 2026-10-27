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
}
