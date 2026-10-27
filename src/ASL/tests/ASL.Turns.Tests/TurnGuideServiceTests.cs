using System.Collections.Immutable;
using System.Text.Json;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnGuideServiceTests
{
    private static readonly TurnGuideCatalog Catalog = TurnGuideCatalog.Load();
    private static TurnEvaluationContext Context => new("game", "viewer", "side-a", "base", "rph", "rph:asop:1.23B", "internal-stamp");
    private static TurnCapability Capability => new("rally:test", "rph:assisted", "base", TurnSourceReview.Reconciled,
        TurnSupport.IntegratedSubset, TurnSupport.IntegratedSubset, ["test-proof"]);
    private static TurnGuideVerdict Verdict => new("visible-1", "viewer", "rally:test", "unit-1", "rph:asop:1.23B",
        "side-a", TurnApplicability.Required, "Authorized reason", "opaque-route");
    private static TurnGuideRequest Request => new(Context, "rph", false);
    private static TurnFacts Facts(params TurnGuideVerdict[] verdicts) => new(Context, [.. verdicts]);
    private static TurnDecision Decision(TurnGuideSnapshot result) => Assert.Single(Assert.Single(result.Options,
        item => item.Topic.Id == "rph:assisted").Decisions);

    [Fact]
    public void ReferenceReadsKeepEveryPhaseTopicPendingNotesAndDependencies()
    {
        var service = new TurnGuideService(Catalog, []);
        var count = 0;
        foreach (var phase in Catalog.Phases)
        {
            var result = service.Describe(Request with
            {
                BrowsedPhaseId = phase.Id
            }, Facts());
            Assert.Equal(phase.Topics.Length, result.Options.Length);
            Assert.Equal(phase.Id, result.BrowsedPhaseId);
            Assert.Equal("rph", result.ActualPhaseId);
            Assert.All(result.Options, option =>
            {
                var decision = Assert.Single(option.Decisions);
                Assert.Equal(TurnApplicability.NotEvaluated, decision.Applicability);
                Assert.Equal(TurnAttentionKind.EvaluationGap, decision.Attention);
                Assert.Null(decision.ActionRoute);
                Assert.Empty(option.Capabilities);
                Assert.Equal(option.Topic.Id, option.Timing.TopicId);
            });
            Assert.NotEmpty(result.Dependencies);
            Assert.Equal(phase.Source, result.Source);
            Assert.Equal(phase.Glossary, result.Glossary);
            Assert.All(result.Groups, group => Assert.Equal(phase.Id, group.PhaseId));
            Assert.All(result.Windows, window => Assert.Equal(phase.Id, window.PhaseId));
            count += result.Options.Length;
        }
        Assert.Equal(538, count);
        var cc = service.Describe(Request with
        {
            BrowsedPhaseId = "ccph"
        }, Facts());
        Assert.Contains("pending", cc.Options.Single(item => item.Topic.Id == "ccph:hiddenHero").Topic.IntegrationNote, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TurnApplicability.NotEvaluated, TurnAttentionKind.EvaluationGap, false)]
    [InlineData(TurnApplicability.Available, TurnAttentionKind.OptionalChoice, true)]
    [InlineData(TurnApplicability.Required, TurnAttentionKind.RequiredDecision, true)]
    [InlineData(TurnApplicability.Unavailable, TurnAttentionKind.Informational, false)]
    [InlineData(TurnApplicability.NotApplicable, TurnAttentionKind.Informational, false)]
    public void AuthorizedVerdictsKeepDistinctAttentionAndExecution(TurnApplicability applicability, TurnAttentionKind attention, bool route)
    {
        var service = new TurnGuideService(Catalog, [Capability]);
        var result = service.Describe(Request, Facts(Verdict with
        {
            Applicability = applicability
        }));
        var decision = Decision(result);
        Assert.Equal(applicability, decision.Applicability);
        Assert.Equal(attention, decision.Attention);
        Assert.Equal(route ? "opaque-route" : null, decision.ActionRoute);
        Assert.Equal("unit-1", decision.SubjectId);
        Assert.Equal("rally:test", decision.CapabilityId);
        Assert.Equal("Authorized reason", decision.Reason);
    }

    [Fact]
    public void MissingSubjectNeverBecomesRequiredOrExecutable()
    {
        var service = new TurnGuideService(Catalog, [Capability]);
        var decision = Decision(service.Describe(Request, Facts(Verdict with
        {
            SubjectId = null
        })));
        Assert.Equal(TurnApplicability.NotEvaluated, decision.Applicability);
        Assert.Equal(TurnAttentionKind.EvaluationGap, decision.Attention);
        Assert.Null(decision.SubjectId);
        Assert.Null(decision.ActionRoute);
    }

    [Theory]
    [InlineData("historical")]
    [InlineData("browsing")]
    [InlineData("other-side")]
    [InlineData("pending-command")]
    public void NonActionableContextsSuppressRoutes(string condition)
    {
        var capability = Capability;
        var request = Request;
        var verdict = Verdict;
        if (condition == "historical")
        {
            request = request with
            {
                Historical = true
            };
        }

        if (condition == "other-side")
        {
            verdict = verdict with
            {
                DecisionOwnerSideId = "side-b"
            };
        }

        if (condition == "pending-command")
        {
            capability = capability with
            {
                Command = TurnSupport.Pending
            };
        }

        if (condition == "browsing")
        {
            request = request with
            {
                BrowsedPhaseId = "ccph"
            };
            capability = capability with
            {
                TopicId = "ccph:hiddenHero"
            };
        }
        var service = new TurnGuideService(Catalog, [capability]);
        var result = service.Describe(request, Facts(verdict));
        var decision = Assert.Single(result.Options.Single(item => item.Topic.Id == capability.TopicId).Decisions);
        Assert.Equal(TurnApplicability.Required, decision.Applicability);
        Assert.Null(decision.ActionRoute);
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("pending-evaluator")]
    [InlineData("unregistered")]
    [InlineData("window")]
    public void UnverifiedOrUnrelatedVerdictsDoNotCreateLegality(string condition)
    {
        var capability = Capability;
        var verdict = Verdict;
        if (condition == "profile")
        {
            capability = capability with
            {
                Profile = "other-profile"
            };
        }

        if (condition == "pending-evaluator")
        {
            capability = capability with
            {
                Evaluator = TurnSupport.Pending
            };
        }

        if (condition == "unregistered")
        {
            verdict = verdict with
            {
                CapabilityId = "unknown"
            };
        }

        if (condition == "window")
        {
            verdict = verdict with
            {
                WindowId = "other-window"
            };
        }

        var decision = Decision(new TurnGuideService(Catalog, [capability]).Describe(Request, Facts(verdict)));
        Assert.Equal(TurnApplicability.NotEvaluated, decision.Applicability);
        Assert.Null(decision.Id);
        Assert.Null(decision.ActionRoute);
    }

    [Fact]
    public void ReadsAreDeterministicAndHiddenWorldDifferencesDoNotChangeOutput()
    {
        var service = new TurnGuideService(Catalog, [Capability with { Evaluator = TurnSupport.Partial, Command = TurnSupport.Partial }]);
        var facts = Facts(Verdict, Verdict with
        {
            Id = "visible-2",
            SubjectId = "unit-2"
        });
        var before = JsonSerializer.Serialize(facts);
        var first = JsonSerializer.Serialize(service.Describe(Request, facts));
        var reversed = facts with
        {
            Verdicts = [.. facts.Verdicts.Reverse()]
        };
        Assert.Equal(first, JsonSerializer.Serialize(service.Describe(Request, reversed)));
        var context = Context with
        {
            EvaluationStamp = "different-secret-revision"
        };
        var hidden = Verdict with
        {
            ViewerId = "other-viewer",
            Id = "secret",
            Reason = "secret",
            Applicability = (TurnApplicability)999
        };
        var alternate = facts with
        {
            Context = context,
            Verdicts = [.. facts.Verdicts, hidden, hidden]
        };
        Assert.Equal(first, JsonSerializer.Serialize(service.Describe(Request with
        {
            Context = context
        }, alternate)));
        Assert.Equal(before, JsonSerializer.Serialize(facts));
        Assert.DoesNotContain("internal-stamp", first, StringComparison.Ordinal);
        Assert.DoesNotContain("different-secret-revision", first, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("GameId")]
    [InlineData("ViewerId")]
    [InlineData("ViewerSideId")]
    [InlineData("Profile")]
    [InlineData("ActualPhaseId")]
    [InlineData("WindowId")]
    [InlineData("EvaluationStamp")]
    public void StaleOrWrongScopeIsRejected(string property)
    {
        var context = Context;
        context = property switch
        {
            "GameId" => context with { GameId = "other" },
            "ViewerId" => context with { ViewerId = "other" },
            "ViewerSideId" => context with { ViewerSideId = "other" },
            "Profile" => context with { Profile = "other" },
            "ActualPhaseId" => context with { ActualPhaseId = "other" },
            "WindowId" => context with { WindowId = "other" },
            _ => context with { EvaluationStamp = "other" },
        };
        Assert.Throws<InvalidOperationException>(() => new TurnGuideService(Catalog, []).Describe(Request, Facts() with { Context = context }));
    }

    [Fact]
    public void InvalidPublicInputsFailWithoutWritingState()
    {
        Assert.Throws<ArgumentNullException>(() => new TurnGuideService(null!, []));
        var service = new TurnGuideService(Catalog, []);
        Assert.Throws<ArgumentNullException>(() => service.Describe(null!, Facts()));
        Assert.Throws<ArgumentNullException>(() => service.Describe(Request, null!));
        Assert.Throws<ArgumentNullException>(() => service.Describe(Request with { Context = null! }, Facts() with { Context = null! }));
        var emptyContext = Context with
        {
            EvaluationStamp = " "
        };
        Assert.Throws<ArgumentException>(() => service.Describe(Request with { Context = emptyContext }, Facts() with { Context = emptyContext }));
        Assert.Throws<ArgumentException>(() => service.Describe(Request with { BrowsedPhaseId = "unknown" }, Facts()));
        var context = Context with
        {
            ActualPhaseId = "unknown"
        };
        Assert.Throws<ArgumentException>(() => service.Describe(Request with { Context = context }, Facts() with { Context = context }));
        context = Context with
        {
            WindowId = "pfph:asop:2.11A"
        };
        Assert.Throws<ArgumentException>(() => service.Describe(Request with { Context = context }, Facts() with { Context = context }));
        Assert.Throws<ArgumentException>(() => service.Describe(Request, new TurnFacts(Context, default)));
        Assert.Throws<InvalidDataException>(() => service.Describe(Request, Facts(Verdict, Verdict)));
        Assert.Throws<InvalidDataException>(() => service.Describe(Request, Facts(Verdict with { Applicability = (TurnApplicability)999 })));
    }
}
