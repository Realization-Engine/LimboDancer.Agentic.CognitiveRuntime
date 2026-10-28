using System.Text.Json;
using ASL.Turns;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class BacklogPass7Tests
{
    private Task WorkTanks(int? year = 1942, string germanFacing = "north-east", string russianFacing = "east", params Dictionary<string, object>[] others)
    {
        var subject = Vehicle("de-tank", "attacker-tank", "bd01:B10:0", "german", germanFacing);
        ((Dictionary<string, bool>)subject["conditions"])[Conditions.ButtonedUp] = false;
        return Setup("german", 7, year, [subject, Vehicle("ru-tank", "defender-tank", "bd01:B8:0", "russian", russianFacing), .. others]);
    }

    private async Task<PlayResult> RecordWorkEffect(string attempt, string operation, TurnWorkDeclaration? declaration = null, DiceRoller? roller = null)
    {
        var actor = GamePlay.Principal("work-adjudicator", Tenant, GameActions.WorkAdjudicationPermission);
        var play = Play(roller);
        var args = JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = attempt,
            expectedRevision = Revision,
            workId = "ammo",
            operation,
            declaration,
            ruleBasis = "E10.3 adjudicated B#/supply eligibility"
        });
        var proposed = await play.ProposeAsync(GameActions.RecordTurnWork, args, actor);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        return await play.ConfirmAsync(GameActions.RecordTurnWork, args, actor, proposed.Correlation);
    }

    [Theory]
    [InlineData(1, 1, false)]
    [InlineData(6, 6, true)]
    public async Task WorkEffectsReplenishRealSpecialAmmoAndResolveSupplyCapacity(int first, int second, bool depleted)
    {
        await WorkTanks(1942, "north-east", "east", Vehicle("supply-truck", "attacker-truck", "bd01:B10:0", "german"),
            Vehicle("other-tank", "attacker-tank", "bd01:B10:0", "german", "north-east"));
        var ordinaryCapacity = GamePlanner.PassengerCapacity(Current, Current.Unit("supply-truck")!);
        Assert.True(ordinaryCapacity > 0);
        await Advance();
        Committed(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "apcr", 4, 2));
        Assert.Contains(Current.DepletedAmmunition, item => item.Gun == "de-tank" && item.Ammunition == "apcr");
        Committed(await FireAt("other-tank", "bd01:B8:0", "ru-tank", "apcr", 4, 2));
        Assert.Contains(Current.DepletedAmmunition, item => item.Gun == "other-tank");
        await Advance(15);
        var declaration = new TurnWorkDeclaration("ammo", "german", "de-tank", ["de-tank", "supply-truck"], TurnWorkKind.AmmoReplenishment, 9, null, "External E10 eligibility")
        {
            CompletionEffects = new(TurnWorkEffectKind.Replenish, SupplyId: "supply-1", SupplyVehicle: "supply-truck", CreateSupply: true, RecordLowAmmo: true),
        };
        Committed(await RecordWorkEffect("ammo-start", "start", declaration));
        Assert.Contains("de-tank", Current.LowAmmoVehicles);
        Assert.Equal(0, GamePlanner.PassengerCapacity(Current, Current.Unit("supply-truck")!));
        var startRevision = Revision;
        await Advance(15);
        Committed(await RecordWorkEffect("ammo-finish", "complete", roller: Once(first, second)));
        Assert.DoesNotContain("de-tank", Current.LowAmmoVehicles);
        Assert.DoesNotContain(Current.DepletedAmmunition, item => item.Gun == "de-tank");
        Assert.Contains(Current.DepletedAmmunition, item => item.Gun == "other-tank");
        Assert.Equal(!depleted, Assert.Single(Current.TurnAmmoSupplies).Available);
        Assert.Equal(depleted ? ordinaryCapacity : 0, GamePlanner.PassengerCapacity(Current, Current.Unit("supply-truck")!));
        Assert.True(Assert.Single(Current.TurnWork).EffectsApplied);
        var reloaded = Planner().Replay(store.Read(Scope)!.Events);
        Assert.False(reloaded.HasErrors);
        Assert.Equal(!depleted, reloaded.Current!.TurnAmmoSupplies[0].Available);
        Assert.Contains("de-tank", reloaded.At(startRevision)!.LowAmmoVehicles);
        var guide = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "ccph", "existing-engine-work-v1");
        Assert.Equal(TurnWorkEffectStatus.Applied, Assert.Single(guide.Work).EffectStatus);
        await Advance();
        var beforeProbe = Revision;
        var reuse = declaration with
        {
            Id = "reuse",
            CompletionEffects = declaration.CompletionEffects! with
            {
                CreateSupply = false
            }
        };
        var reusePlan = await Planner().PlanAsync(GameActions.RecordTurnWork, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "reuse-probe",
            expectedRevision = Revision,
            workId = "reuse",
            operation = "start",
            declaration = reuse,
            ruleBasis = "new depletion adjudication"
        }), Tenant);
        Assert.Equal(depleted ? GamePlanStatus.Refused : GamePlanStatus.Ready, reusePlan.Status);
        Assert.Equal(beforeProbe, Revision);
        Assert.DoesNotContain("de-tank", Current.LowAmmoVehicles); // Probing never records the proposed fact.
        var duplicate = reuse with
        {
            CompletionEffects = declaration.CompletionEffects
        };
        var duplicatePlan = await Planner().PlanAsync(GameActions.RecordTurnWork, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "duplicate-supply",
            expectedRevision = Revision,
            workId = "reuse",
            operation = "start",
            declaration = duplicate,
            ruleBasis = "duplicate"
        }), Tenant);
        Assert.Equal(GamePlanStatus.Refused, duplicatePlan.Status);
        await Advance(); // Next first-side PFPh: the actual ordnance reader accepts APCR again.
        Committed(await FireAt("de-tank", "bd01:B8:0", "ru-tank", "apcr", 4, 2));
        Assert.Contains(Current.DepletedAmmunition, item => item.Gun == "de-tank"); // The new attempt independently depletes again.
    }
}
