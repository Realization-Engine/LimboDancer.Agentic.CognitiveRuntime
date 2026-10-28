using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class BacklogPass15Tests
{
    [Fact]
    public async Task HorseSetupCannotAdmitLivePlayOrAReadyPhaseClose()
    {
        const string designation = "scenario-mule-designation";
        var horse = new Dictionary<string, object>
        {
            ["id"] = "mule",
            ["kind"] = "asl:horse",
            ["side"] = "german",
            ["position"] = new { at = L("E4") },
            ["conditions"] = new Dictionary<string, bool>(),
            ["horse"] = new TurnHorseIdentity(TurnHorseSize.Squad, true, designation)
        };
        await SetupAt(0, "german", [designation], Sides(), horse);
        var before = Revision;
        var plan = await Planner().PlanAsync(GameActions.AdvancePhase, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "horse-close",
            expectedRevision = before
        }), Tenant);
        Assert.Equal(GamePlanStatus.Refused, plan.Status);
        Assert.Contains(plan.Reasons, reason => reason.Contains("pending-profile", StringComparison.Ordinal));
        Assert.Empty(plan.Events);
        Assert.Null(plan.Roll);
        var guide = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "rph", "existing-engine-work-v1");
        Assert.NotEqual(TurnCloseDisposition.Ready, guide.PhaseClose.Disposition);
        Assert.Equal(before, Revision);
        Assert.Equal(9, TurnCapabilityInventory.Load(TurnGuideCatalog.Load()).Gaps.Length);
    }
}
