using ASL.Turns;
using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Turns;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed partial class TurnGuideComponentTests
{
    [Fact]
    public void ReviewSummaryPreservesIndependentEvidenceAndPendingBoundary()
    {
        var review = new TurnPlacedDcReview("dc", "placement", "phase", "german", "ASOP: WP remains unverified.",
            new(TurnOpeningEvidence.Resolved, TurnOpeningEvidence.Unknown, TurnOpeningEvidence.NotApplicable, TurnOpeningEvidence.Resolved));
        var cut = context.Render<TurnOpeningReviewSummary>(p => p.Add(c => c.Review, review));
        var items = cut.FindAll("li").Select(item => item.TextContent).ToArray();
        Assert.Equal("Drifting SMOKE (5.11B): Resolved", items[0]);
        Assert.Equal("Opening Dispersed WP (5.12A): Unknown", items[1]);
        Assert.Equal("Gusts and SMOKE (5.13A): Not applicable", items[2]);
        Assert.Equal("Glider contents (5.21A): Resolved", items[3]);
        Assert.Contains(review.RuleBasis, cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Pending rules remain pending", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("separate detonation proposal", cut.Markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("permission")]
    [InlineData("tenant")]
    [InlineData("anonymous")]
    [InlineData("authorized")]
    public void ServerSuppliedReviewerIsNarrowAndDefaultAccessIsDenied(string kind)
    {
        var root = Path.Combine(Path.GetTempPath(), "asl-review-access-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = new LimboDancer.Domains.Asl.MapStudio.Services.StudioOptions { BoardsRoot = root, CacheRoot = Path.Combine(root, "cache") };
            var library = new LimboDancer.Domains.Asl.MapStudio.Services.UnitLibrary(options);
            var maps = new LimboDancer.Domains.Asl.MapStudio.Services.MapService(options, new FakeVaslMapSource());
            var reviewer = kind == "missing" ? null : new LimboDancer.Abstractions.Execution.RuntimePrincipal("reviewer",
                kind == "tenant" ? Guid.NewGuid() : LimboDancer.Domains.Asl.MapStudio.Services.LivePlay.Tenant, kind != "anonymous",
                kind == "permission" ? [] : [GameActions.WindowAdjudicationPermission]);
            var live = new LimboDancer.Domains.Asl.MapStudio.Services.LivePlay(library, new BuildingBoards(maps), openingReviewer: reviewer);
            Assert.Equal(kind == "authorized", live.CanReviewOpening);
            Assert.Same(live.Principal, live.PrincipalFor(GameActions.ReviewPlacedDcWindow, Perspective.Side("german")));
            Assert.Same(live.Principal, live.PrincipalFor(GameActions.DetonateDc, Perspective.Adjudicator));
            Assert.Same(kind == "authorized" ? reviewer : live.Principal, live.PrincipalFor(GameActions.ReviewPlacedDcWindow, Perspective.Adjudicator));
            Assert.DoesNotContain(GameActions.WindowAdjudicationPermission, live.Principal.Permissions);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void OpeningReviewRequiresReasonDefaultsUnknownAndResetsOnNewOffer()
    {
        TurnOpeningReviewRequest? received = null;
        var offer = new TurnOpeningReviewOffer("token", "dc", "german");
        var cut = context.Render<TurnOpeningReview>(p => p.Add(c => c.Offer, offer).Add(c => c.ReviewRequested, request => received = request));
        Assert.All(cut.FindAll("select"), select => Assert.Equal("Unknown", select.GetAttribute("value")));
        Assert.True(cut.Find("button").HasAttribute("disabled"));
        cut.Find("textarea").Input("ASOP opening: WP still unverified");
        cut.Find(".turn-review-drift").Change("Resolved");
        cut.Find(".turn-review-gliders").Change("NotApplicable");
        cut.Find("button").Click();
        Assert.Equal(new TurnAfphOpening(TurnOpeningEvidence.Resolved, TurnOpeningEvidence.Unknown, TurnOpeningEvidence.Unknown, TurnOpeningEvidence.NotApplicable), received!.Opening);
        Assert.Equal(offer, received.Offer);
        cut.Render(p => p.Add(c => c.Busy, true));
        Assert.True(cut.Find("button").HasAttribute("disabled"));
        cut.Render(p => p.Add(c => c.Busy, false).Add(c => c.Offer, offer with { Token = "new-revision" }));
        Assert.True(cut.Find("button").HasAttribute("disabled"));
        Assert.All(cut.FindAll("select"), select => Assert.Equal("Unknown", select.GetAttribute("value")));
        Assert.Contains("Pending rules remain pending", cut.Markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public void ReviewFormNeedsHostAuthorizationLiveContextAndAdjudicator(bool authorized, bool historical, bool side)
    {
        var guide = Guide(side ? "german" : Perspective.AdjudicatorName, historical) with
        {
            OpeningReviews = [new("token", "dc", "german")]
        };
        var cut = context.Render<TurnGuidePanel>(p => p.Add(c => c.Host, guide).Add(c => c.CanReviewOpening, authorized)
            .Add(c => c.ReviewRequested, _ => throw new InvalidOperationException("Must not submit")));
        cut.Find("[data-topic='afph:placed']").Click();
        Assert.Empty(cut.FindAll(".turn-opening-review"));
        Assert.Contains("Pending rules:", cut.Markup, StringComparison.Ordinal);
    }
}
