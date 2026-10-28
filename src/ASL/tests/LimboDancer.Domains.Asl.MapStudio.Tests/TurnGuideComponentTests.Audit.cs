using ASL.Turns;
using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Turns;
using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed partial class TurnGuideComponentTests
{
    [Fact]
    public void AuditRetainsEveryDependencyLinkAndNavigatesAcrossPhases()
    {
        string? requested = null;
        var audit = context.Render<TurnDependencyAudit>(p => p.Add(c => c.TopicRequested, id => requested = id));
        Assert.Equal(38, audit.FindAll("option").Count);
        var links = 0;
        foreach (var dependency in Catalog.Dependencies)
        {
            audit.Find("select").Change(dependency.Id);
            Assert.Equal(dependency.TopicIds.Length, audit.FindAll("[data-related-topic]").Count);
            links += audit.FindAll("[data-related-topic]").Count;
            Assert.Contains(dependency.IntegrationNote, audit.Find(".turn-audit").TextContent, StringComparison.Ordinal);
        }
        Assert.Equal(191, links);
        audit.Find("[data-related-topic]").Click();
        Assert.NotNull(Catalog.FindTopic(requested!));

        var page = context.Render<LimboDancer.Domains.Asl.MapStudio.Components.Pages.TurnGuide>();
        var crossPhase = Catalog.Dependencies.First(item => item.TopicIds.Any(id => id.StartsWith("afph:", StringComparison.Ordinal)));
        var target = crossPhase.TopicIds.First(id => id.StartsWith("afph:", StringComparison.Ordinal));
        page.Find(".turn-audit select").Change(crossPhase.Id);
        page.Find($".turn-audit [data-related-topic='{target}']").Click();
        Assert.Equal("true", page.Find($"[data-topic='{target}']").GetAttribute("aria-pressed"));
        Assert.Contains(Catalog.FindTopic(target)!.Title, page.Find("article h3").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void PhaseSelectionAndRemindersSurviveBrowsingButDoNotLeakAcrossPhases()
    {
        var page = context.Render<LimboDancer.Domains.Asl.MapStudio.Components.Pages.TurnGuide>();
        page.Find("[data-topic='rph:packStart']").Click();
        page.Find(".turn-considered").Change(true);
        page.Find("[data-phase=afph]").Click();
        Assert.Contains("0 private reminders in this phase", page.Markup, StringComparison.Ordinal);
        page.Find("[data-topic='afph:placed']").Click();
        page.Find("[data-phase=rph]").Click();
        Assert.Equal("true", page.Find("[data-topic='rph:packStart']").GetAttribute("aria-pressed"));
        Assert.Contains("1 private reminders in this phase", page.Markup, StringComparison.Ordinal);
        page.Find("[data-phase=afph]").Click();
        Assert.Equal("true", page.Find("[data-topic='afph:placed']").GetAttribute("aria-pressed"));
        Assert.Equal(Catalog.Phases.Single(item => item.Id == "afph").Glossary.Count, page.FindAll(".turn-glossary dt").Count);
    }

    [Theory]
    [InlineData("subject")]
    [InlineData("profile")]
    [InlineData("action")]
    [InlineData("other-phase")]
    public void InconsistentActionIdentityCannotProduceAnIntent(string fault)
    {
        var guide = Guide();
        var action = guide.Actions[0];
        guide = fault switch
        {
            "subject" => guide with { Actions = [action with { SubjectId = "another-charge" }] },
            "profile" => guide with { Actions = [action with { Profile = "existing-engine" }] },
            "action" => guide with { Actions = [action with { ActionId = "other-command" }] },
            _ => guide with { Guide = guide.Guide with { ActualPhaseId = "mph" } },
        };
        var cut = context.Render<TurnGuidePanel>(p => p.Add(c => c.Host, guide).Add(c => c.ActionRequested, _ => throw new InvalidOperationException("Must not execute")));
        cut.Find("[data-topic='afph:placed']").Click();
        Assert.Empty(cut.FindAll(".turn-action"));
        Assert.Contains("Required consideration", cut.Find("[data-topic='afph:placed']").TextContent, StringComparison.Ordinal);
        Assert.Contains("Pending rules:", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExplicitRefreshHidesOldGuidanceAndRejectsLateSameScopeReads()
    {
        var second = new TaskCompletionSource<LimboDancer.Domains.Asl.Play.TurnHostGuide>();
        var count = 0;
        var cut = context.Render<LiveTurnGuide>(p => p.Add(c => c.ScopeKey, "game/german").Add(c => c.ActualPhase, "afph")
            .Add(c => c.ReadGuide, _ => ++count == 2 ? second.Task : Task.FromResult(Guide())));
        var refresh = cut.Find(".turn-refresh").ClickAsync(new());
        cut.WaitForAssertion(() => Assert.True(cut.Find(".turn-refresh").HasAttribute("disabled")));
        Assert.True(cut.Find(".turn-guide").ParentElement!.HasAttribute("hidden"));
        cut.Render(p => p.Add(c => c.Revision, 2));
        await cut.InvokeAsync(() => second.SetResult(Guide("obsolete")));
        await refresh;
        Assert.Equal("german", cut.FindComponent<TurnGuidePanel>().Instance.Host!.Guide.ViewerId);
        Assert.Equal(3, count);
    }

    [Fact]
    public void EvidenceShowsUnknownsOpeningPrerequisitesAndWorkLifecycleWithoutPromotingRules()
    {
        var guide = Guide() with
        {
            Obligations = [new("placedDc", "dc", TurnObligationState.Due, new(TurnRuleDeadlineKind.PlacedDcAttack, 3, "german"), TurnSupport.IntegratedSubset)],
            DecisionWindows = [new("dc", TurnDecisionTiming.NotEvaluated, false, "afph:asop:5.22A",
                new(TurnOpeningEvidence.Resolved, TurnOpeningEvidence.Unknown, TurnOpeningEvidence.NotApplicable, TurnOpeningEvidence.Resolved), [], ["WP remains pending."])],
            FireHistory = new([new(TurnFireKind.Ordnance, 1, "german", "pfph", ["crew"], "gun", "spotter", null, false)],
                [new("gun", "spotter", 1, "pfph", TurnSpotterState.NotEvaluated, null)], ["Spotter execution pending."]),
            Atmosphere = new([new("squad", "german", 1, "mph", "smoke", TurnSmokeState.Active, true)], [], [], []),
            Work = [new("work", "gun", TurnWorkKind.AnimalPacking, 2, 2, false, false, true, false, true, TurnSupport.Pending)
                { EffectKind = TurnWorkEffectKind.PackGun, EffectStatus = TurnWorkEffectStatus.Cancelled }],
        };
        var cut = context.Render<TurnGuideEvidence>(p => p.Add(c => c.Host, guide));
        var text = cut.Find("section").TextContent;
        Assert.Contains("Earliest Player Turn: 3", text, StringComparison.Ordinal);
        Assert.All(guide.Profile.Bounds.SelectMany(bound => bound.Pending), gap => Assert.Contains(gap, text, StringComparison.Ordinal));
        Assert.Contains("WP: Unknown", text, StringComparison.Ordinal);
        Assert.Contains("ROF retained: Unknown", text, StringComparison.Ordinal);
        Assert.Contains("Spotted this phase: Unknown", text, StringComparison.Ordinal);
        Assert.Contains("Attempt spent this phase: Yes", text, StringComparison.Ordinal);
        Assert.Contains("Cancelled: Yes", text, StringComparison.Ordinal);
        Assert.Contains("Completion due: No", text, StringComparison.Ordinal);
        Assert.Contains("Supply depleted: Yes", text, StringComparison.Ordinal);
        Assert.Contains("Completion effect: PackGun", text, StringComparison.Ordinal);
        Assert.Contains("Rule automation: Pending", text, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("button"));
    }
}
