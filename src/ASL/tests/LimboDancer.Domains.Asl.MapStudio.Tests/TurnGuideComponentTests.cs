using System.Collections.Immutable;
using ASL.Turns;
using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Turns;
using LimboDancer.Domains.Asl.MapStudio.Components.Layout;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed partial class TurnGuideComponentTests : IDisposable
{
    private readonly BunitContext context = new();
    private static readonly TurnGuideCatalog Catalog = TurnGuideCatalog.Load();
    public void Dispose() => context.Dispose();

    private static TurnHostGuide Guide(string viewer = "german", bool historical = false)
    {
        var scope = new TurnEvaluationContext("game", viewer, viewer, "existing-engine-work-v1", "afph", "afph:phase", "test");
        var snapshot = new TurnGuideService(Catalog, []).Describe(new(scope, "afph", historical), new(scope, []));
        var options = snapshot.Options.Select(option => option.Topic.Id == "afph:placed" ? option with
        {
            Decisions = [new("decision", "dc", "placed", viewer, TurnApplicability.Required, "Request the governed command check", "opaque", TurnAttentionKind.RequiredDecision)]
        } : option).ToImmutableArray();
        return new(snapshot with
        {
            Options = options
        }, [new("placedDc", "dc", TurnObligationState.Due, null, TurnSupport.IntegratedSubset)],
            new(historical ? TurnCloseDisposition.ReadOnly : TurnCloseDisposition.GovernedCheckRequired, ["Private decisions may remain."], [], false, ["Full rules remain pending."]))
        {
            Profile = TurnGuideProfiles.Get("existing-engine-work-v1"),
            Actions = [new("opaque", "dc", "existing-engine-work-v1", TurnPlacedDcGuidance.ActionId)],
            WorkProgressGaps = ["Packing rules pending."]
        };
    }

    [Fact]
    public void ReferencePageSwitchesPhaseThroughItsCallback()
    {
        var cut = context.Render<LimboDancer.Domains.Asl.MapStudio.Components.Pages.TurnGuide>();
        cut.Find("[data-phase=afph]").Click();
        Assert.Contains("Advancing Fire", cut.Find("h2").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(cut.FindAll("[data-topic^='afph:']"));
        Assert.Empty(cut.FindAll("[data-topic^='rph:']"));
    }

    [Fact]
    public void ReferenceShowsEveryPhaseAndPendingOptionWithPrivateConsideration()
    {
        var cut = context.Render<TurnGuidePanel>();
        Assert.Equal(8, cut.FindAll("[data-phase]").Count);
        Assert.Equal("true", cut.Find("[data-phase=rph]").GetAttribute("aria-pressed"));
        Assert.Single(cut.FindAll("[data-topic][aria-pressed=true]"));
        var count = 0;
        foreach (var phase in Catalog.Phases)
        {
            cut.Render(p => p.Add(c => c.Phase, phase.Id));
            Assert.Equal(phase.Topics.Length, cut.FindAll("[data-topic]").Count);
            count += cut.FindAll("[data-topic]").Count;
            Assert.Contains("Pending rules:", cut.Markup, StringComparison.Ordinal);
            Assert.Empty(cut.FindAll(".turn-action, .turn-close"));
        }
        Assert.Equal(538, count);
        cut.Find(".turn-considered").Change(true);
        Assert.Contains("1 private reminders", cut.Markup, StringComparison.Ordinal);
        cut.Find("input[type=search]").Input("no-such-option");
        Assert.Empty(cut.FindAll("[data-topic]"));
        cut.FindAll("button").Single(button => button.TextContent == "Show all options").Click();
        Assert.NotEmpty(cut.FindAll("[data-topic]"));
        Assert.Contains("Reference guide", cut.Markup, StringComparison.Ordinal);
        Assert.Equal("reference", StudioNavigation.GroupOf("turn-guide")!.Id);
    }

    [Fact]
    public void OnlyOfferedRouteRaisesIntentAndHistoryRemainsReadOnly()
    {
        TurnPlacedDcAction? chosen = null;
        var close = 0;
        var guide = Guide();
        var cut = context.Render<TurnGuidePanel>(p => p.Add(c => c.Host, guide)
            .Add(c => c.ActionRequested, value => chosen = value).Add(c => c.CloseRequested, () => close++));
        cut.Find("[data-topic='afph:placed']").Click();
        cut.Find(".turn-action").Click();
        Assert.Equal(guide.Actions[0], chosen);
        cut.Find(".turn-close").Click();
        Assert.Equal(1, close);
        Assert.Contains("Due", cut.Markup, StringComparison.Ordinal);
        cut.Find(".turn-considered").Change(true);
        cut.Render(p => p.Add(c => c.Host, Guide("russian")));
        Assert.DoesNotContain("1 private reminders", cut.Markup, StringComparison.Ordinal);
        cut.Render(p => p.Add(c => c.Host, Guide(historical: true)));
        cut.Find("[data-topic='afph:placed']").Click();
        Assert.Empty(cut.FindAll(".turn-action, .turn-close"));
        Assert.Contains("read-only", cut.Markup, StringComparison.Ordinal);
        cut.Render(p => p.Add(c => c.Host, guide with { Actions = [] }));
        cut.Find("[data-topic='afph:placed']").Click();
        Assert.Empty(cut.FindAll(".turn-action"));
    }

    [Fact]
    public void PendingSourceGapsAndDependenciesAreNotHiddenByReferenceMode()
    {
        var cut = context.Render<TurnGuidePanel>();
        cut.Find("[data-topic='rph:packStart']").Click();
        Assert.Contains("Source not yet available", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("B5-D3", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Cross-phase dependencies", cut.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(cut.FindAll("article a[href^='acronyms']"));
    }

    [Fact]
    public async Task LateReadCannotReplaceAnotherViewAndErrorsAreSanitized()
    {
        var first = new TaskCompletionSource<TurnHostGuide>();
        var cut = context.Render<LiveTurnGuide>(p => p.Add(c => c.ScopeKey, "game/german")
            .Add(c => c.ReadGuide, _ => first.Task));
        cut.Render(p => p.Add(c => c.ScopeKey, "game/russian").Add(c => c.ReadGuide, _ => Task.FromResult(Guide("russian"))));
        cut.WaitForAssertion(() => Assert.Equal("russian", cut.FindComponent<TurnGuidePanel>().Instance.Host!.Guide.ViewerId));
        await cut.InvokeAsync(() => first.SetResult(Guide()));
        Assert.Equal("russian", cut.FindComponent<TurnGuidePanel>().Instance.Host!.Guide.ViewerId);
        cut.Render(p => p.Add(c => c.Revision, 2).Add(c => c.ReadGuide, _ => Task.FromException<TurnHostGuide>(new InvalidOperationException("hidden enemy"))));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[role=alert]")));
        Assert.DoesNotContain("hidden enemy", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindComponents<TurnGuidePanel>());
        cut.Render(p => p.Add(c => c.ReadGuide, _ => Task.FromResult(Guide("russian"))));
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindComponents<TurnGuidePanel>()));
    }
}
