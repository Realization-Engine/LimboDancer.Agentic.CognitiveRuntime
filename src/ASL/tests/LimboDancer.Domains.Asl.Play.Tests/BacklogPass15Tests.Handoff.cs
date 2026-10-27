using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class BacklogPass15Tests
{
    // Executable U1 host example. Setup/review are fixture adjudication, never inferred by a renderer.
    [Fact]
    public async Task HeadlessConsumerBrowsesEveryPhaseAndConfirmsAnOfferedDecision()
    {
        await PlacedDcRouteGame();
        const string profile = "existing-engine-work-v1";
        var viewer = Perspective.Side("german");
        var planner = Planner();
        var catalog = TurnGuideCatalog.Load();
        var beforeReads = JsonSerializer.Serialize(store.Read(Scope));
        var discovered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var phase in catalog.Phases)
        {
            var response = await planner.ReadTurnGuideProfileAsync(Scope, viewer, phase.Id, profile);
            TurnGuideAcceptanceCases.AssertProfile(response, profile);
            Assert.Equal("afph", response.Guide.ActualPhaseId);
            Assert.Equal(phase.Id, response.Guide.BrowsedPhaseId);
            Assert.Empty(response.Actions); // Opening facts are still missing, even in live AFPh.
            Assert.NotEmpty(response.Guide.Options);
            foreach (var option in response.Guide.Options)
            {
                Assert.True(discovered.Add(option.Topic.Id));
                Assert.NotEmpty(option.Topic.References);
                Assert.NotEmpty(option.Decisions);
            }
            Assert.NotEmpty(response.PhaseClose.EvaluationGaps);
            Assert.Equal(TurnCloseDisposition.GovernedCheckRequired, response.PhaseClose.Disposition);
        }
        Assert.Equal(538, discovered.Count);
        Assert.Equal(beforeReads, JsonSerializer.Serialize(store.Read(Scope)));

        // A separate authorized adjudicator supplies the durable missing facts.
        await ReviewDcRoute();
        var due = await planner.ReadTurnGuideProfileAsync(Scope, viewer, "afph", profile);
        var route = Assert.Single(due.Actions);
        var decision = Assert.Single(due.Guide.Options.Single(option => option.Topic.Id == "afph:placed").Decisions);
        Assert.Equal(route.Token, decision.ActionRoute);
        Assert.Contains("governed command check", decision.Reason, StringComparison.Ordinal);
        Assert.Equal(TurnObligationState.Due, Assert.Single(due.Obligations).State);
        Assert.Equal(TurnCloseDisposition.GovernedCheckRequired, due.PhaseClose.Disposition);

        // Trusted server uses the authenticated side and retains the exact prepared arguments.
        var args = (await planner.PreparePlacedDcDecisionAsync(Scope, viewer, route, "u1-example"))!.Value;
        var play = Play(Then(3));
        var revisionBeforeConfirmation = Revision;
        var proposed = await play.ProposeAsync(GameActions.DetonateDc, args, Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        Assert.Equal(TurnGuideAcceptanceCases.Json(due), TurnGuideAcceptanceCases.Json(
            await planner.ReadTurnGuideProfileAsync(Scope, viewer, "afph", profile)));
        Assert.Equal(revisionBeforeConfirmation, Revision);
        Committed(await play.ConfirmAsync(GameActions.DetonateDc, args, Player, proposed.Correlation));

        var resolved = await planner.ReadTurnGuideProfileAsync(Scope, viewer, "afph", profile);
        TurnGuideAcceptanceCases.AssertProfile(resolved, profile);
        Assert.Empty(resolved.Actions);
        Assert.Equal(TurnObligationState.Resolved, Assert.Single(resolved.Obligations).State);
        Assert.Null(await planner.PreparePlacedDcDecisionAsync(Scope, viewer, route, "expired-example"));
        var after = Revision;
        Assert.Equal(PlayOutcome.Replay, (await Commit(Play(), GameActions.DetonateDc, args)).Outcome);
        Assert.Equal(after, Revision);
        var historical = await planner.ReadTurnGuideProfileAsync(Scope, viewer, "afph", profile, revisionBeforeConfirmation);
        TurnGuideAcceptanceCases.AssertProfile(historical, profile);
        Assert.Equal(TurnObligationState.Due, Assert.Single(historical.Obligations).State);
        Assert.Empty(historical.Actions);
    }
}
