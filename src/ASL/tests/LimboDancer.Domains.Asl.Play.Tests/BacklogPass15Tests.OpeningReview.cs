using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class BacklogPass15Tests
{
    [Fact]
    public async Task OpeningReviewOffersAreAdjudicatorOnlyBoundToStateAndStillRequirePermission()
    {
        await PlacedDcRouteGame();
        var planner = Planner();
        var offer = Assert.Single((await DcGuide(Perspective.Adjudicator)).OpeningReviews);
        Assert.Empty((await DcGuide()).OpeningReviews);
        Assert.Empty((await DcGuide(Perspective.Side("russian"))).OpeningReviews);
        Assert.Empty((await DcGuide(Perspective.Adjudicator, revision: Revision)).OpeningReviews);
        Assert.Empty((await DcGuide(Perspective.Adjudicator, phase: "rph")).OpeningReviews);
        Assert.Empty((await DcGuide(Perspective.Adjudicator, profile: "existing-engine")).OpeningReviews);
        var request = new TurnOpeningReviewRequest(offer, "ASOP reviewed", new(TurnOpeningEvidence.Resolved, TurnOpeningEvidence.Unknown,
            TurnOpeningEvidence.NotApplicable, TurnOpeningEvidence.Resolved));
        Assert.Null(planner.PrepareOpeningReview(Scope, Perspective.Side("german"), request, "side"));
        Assert.Null(planner.PrepareOpeningReview(Scope, Perspective.Adjudicator, request with
        {
            RuleBasis = " "
        }, "blank"));
        Assert.Null(planner.PrepareOpeningReview(Scope, Perspective.Adjudicator, request with
        {
            Opening = request.Opening with
            {
                Wp = (TurnOpeningEvidence)42
            }
        }, "invalid"));
        foreach (var wrong in new[] { offer with { Token = "forged" }, offer with { SubjectId = "other" }, offer with { OwnerSide = "russian" } })
        {
            Assert.Null(planner.PrepareOpeningReview(Scope, Perspective.Adjudicator, request with
            {
                Offer = wrong
            }, "forged"));
        }
        var before = Revision;
        var args = planner.PrepareOpeningReview(Scope, Perspective.Adjudicator, request, "review-from-offer")!.Value;
        Assert.Equal(before, Revision);
        Assert.Equal(PlayOutcome.Denied, (await Play().ProposeAsync(GameActions.ReviewPlacedDcWindow, args, Player)).Outcome);
        var referee = GamePlay.Principal("referee", Tenant, GameActions.WindowAdjudicationPermission);
        var play = Play();
        var proposal = await play.ProposeAsync(GameActions.ReviewPlacedDcWindow, args, referee);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposal.Outcome);
        Committed(await play.ConfirmAsync(GameActions.ReviewPlacedDcWindow, args, referee, proposal.Correlation));
        Assert.Empty((await DcGuide()).Actions);
        Assert.Null(planner.PrepareOpeningReview(Scope, Perspective.Adjudicator, request, "expired"));
        await ReviewDcRoute();
        Assert.Single((await DcGuide()).Actions);
    }
}
