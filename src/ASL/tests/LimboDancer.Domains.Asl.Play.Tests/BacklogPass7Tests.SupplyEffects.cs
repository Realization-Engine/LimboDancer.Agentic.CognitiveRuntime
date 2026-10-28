using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class BacklogPass7Tests
{
    private static TurnWorkDeclaration SupplyDeclaration(bool offload) => new("ammo", "german", "source",
        offload ? ["source"] : ["source", "destination"], TurnWorkKind.AmmoTransfer, null, null, "E10.4; E10.11 qualification adjudicated")
    {
        CompletionEffects = new(offload ? TurnWorkEffectKind.OffloadSupply : TurnWorkEffectKind.TransferSupply,
            SupplyId: "supply", SupplyVehicle: "source", CreateSupply: true,
            DestinationVehicle: offload ? null : "destination", DumpLocation: offload ? "bd01:B10:0" : null),
    };

    private Task<GamePlan> SupplyPlan(string operation, TurnWorkDeclaration? declaration = null) =>
        Planner().PlanAsync(GameActions.RecordTurnWork, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "probe",
            expectedRevision = Revision,
            workId = "ammo",
            operation,
            declaration,
            ruleBasis = "E10.4 adjudication",
        }), Tenant);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SupplyEffectsMoveOneCounterAndApplySourceAndCapacityEffects(bool offload, bool immobilized)
    {
        await WorkTanks(1942, "north-east", "east",
            Vehicle("source", "attacker-truck", "bd01:B10:0", "german", "east", immobilized ? Conditions.Immobilized : Conditions.Bogged),
            Vehicle("destination", "attacker-truck", "bd01:B10:0", "german"));
        var ordinary = GamePlanner.PassengerCapacity(Current, Current.Unit("source")!);
        Committed(await RecordWorkEffect("start", "start", SupplyDeclaration(offload)));
        var started = Revision;
        Assert.Equal(0, GamePlanner.PassengerCapacity(Current, Current.Unit("source")!));
        Assert.Equal(ordinary, GamePlanner.PassengerCapacity(Current, Current.Unit("destination")!));
        await Advance(15);
        Assert.Equal(GamePlanStatus.Refused, (await SupplyPlan("complete")).Status);
        Assert.Equal("source", Assert.Single(Current.TurnAmmoSupplies).Vehicle);
        await Advance(48);
        Assert.Equal(GamePlanStatus.Refused, (await SupplyPlan("complete", SupplyDeclaration(offload))).Status);
        var before = Revision;
        Committed(await RecordWorkEffect("complete", "complete")); // No dice source is available.
        Assert.Single(Since(before));
        Assert.IsType<TurnWorkRecorded>(Since(before)[0].Payload);
        var supply = Assert.Single(Current.TurnAmmoSupplies);
        Assert.Equal("supply", supply.Id);
        Assert.True(supply.Available);
        Assert.Equal(offload ? null : "destination", supply.Vehicle);
        Assert.Equal(offload ? "bd01:B10:0" : null, supply.DumpLocation);
        Assert.Equal(immobilized ? InstanceStatus.Wrecked : InstanceStatus.Active, Current.Unit("source")!.Status);
        Assert.Equal(ordinary, GamePlanner.PassengerCapacity(Current, Current.Unit("source")!));
        Assert.Equal(offload ? ordinary : 0, GamePlanner.PassengerCapacity(Current, Current.Unit("destination")!));
        Assert.Equal(ConditionState.False, GameState.Condition(Current.Unit("source")!, "asl:ti"));
        Assert.Empty(Current.LowAmmoVehicles);
        Assert.Empty(Current.DepletedAmmunition);
        var history = Planner().Replay(store.Read(Scope)!.Events);
        Assert.False(history.HasErrors);
        Assert.Equal(supply, Assert.Single(history.Current!.TurnAmmoSupplies));
        Assert.Equal("source", Assert.Single(history.At(started)!.TurnAmmoSupplies).Vehicle);
        Assert.Equal(GamePlanStatus.Refused, (await SupplyPlan("complete")).Status);
        var guide = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "ccph", "existing-engine-work-v1");
        var disclosure = Assert.Single(guide.Work);
        Assert.Equal(TurnWorkEffectStatus.Applied, disclosure.EffectStatus);
        Assert.Equal(TurnSupport.Pending, disclosure.RuleAutomation);
        await Advance();
        var replenishment = new TurnWorkDeclaration("replenish", "german", "de-tank", ["de-tank", "destination"],
            TurnWorkKind.AmmoReplenishment, 9, null, "E10.3 qualification adjudicated")
        {
            CompletionEffects = new(TurnWorkEffectKind.Replenish, SupplyId: "supply", SupplyVehicle: "destination", RecordLowAmmo: true),
        };
        var reuse = await Planner().PlanAsync(GameActions.RecordTurnWork, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "reuse",
            expectedRevision = Revision,
            workId = "replenish",
            operation = "start",
            declaration = replenishment,
            ruleBasis = "E10.3",
        }), Tenant);
        Assert.Equal(offload ? GamePlanStatus.Refused : GamePlanStatus.Ready, reuse.Status);
        Assert.Empty(Current.LowAmmoVehicles); // The read-only probe records no facts.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupplyEffectsRejectStaleSourceAtomicallyAndCancellationLeavesSupplyInPlace(bool offload)
    {
        await WorkTanks(1942, "north-east", "east",
            Vehicle("source", "attacker-truck", "bd01:B10:0", "german", "east", Conditions.Immobilized),
            Vehicle("destination", "attacker-truck", "bd01:B10:0", "german"));
        Committed(await RecordWorkEffect("start", "start", SupplyDeclaration(offload)));
        await Advance(63);
        var events = store.Read(Scope)!.Events;
        var changed = events[^1] with
        {
            EventId = "mobile-source",
            Revision = events.Count + 1,
            Type = "conditions-changed",
            Payload = new ConditionsChanged("source", new Dictionary<string, ConditionState> { [Conditions.Immobilized] = ConditionState.False }),
        };
        var complete = changed with
        {
            EventId = "stale-completion",
            Revision = changed.Revision + 1,
            Type = TurnWorkRecorded.AuditedType,
            Payload = new TurnWorkRecorded("ammo", "complete", null, null, "E10.4"),
        };
        Assert.False(Planner().Replay([.. events, changed]).HasErrors);
        Assert.Equal(AppendStatus.Invalid, store.Append(Scope, "work", events.Count, [changed, complete], Planner().Replay).Status);
        Assert.Equal(events.Count, Revision);
        Assert.Equal("source", Assert.Single(Current.TurnAmmoSupplies).Vehicle);
        Assert.False(Assert.Single(Current.TurnWork).EffectsApplied);
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("source")!, "asl:ti"));
        Committed(await RecordWorkEffect("cancel", "cancel"));
        Assert.Equal("source", Assert.Single(Current.TurnAmmoSupplies).Vehicle);
        Assert.Equal(InstanceStatus.Active, Current.Unit("source")!.Status);
        Assert.Equal(ConditionState.False, GameState.Condition(Current.Unit("source")!, "asl:ti"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SupplyEffectsRejectInvalidSourcesAndDestinationsWithoutRecordingFacts(bool offload, bool mobile)
    {
        await WorkTanks(1942, "north-east", "east",
            Vehicle("source", "attacker-truck", "bd01:B10:0", "german", "east", mobile ? [] : [Conditions.Immobilized]),
            Vehicle("destination", "attacker-truck", "bd01:B9:0", "german"));
        var declaration = SupplyDeclaration(offload);
        if (offload && !mobile)
        {
            declaration = declaration with
            {
                CompletionEffects = declaration.CompletionEffects! with
                {
                    DumpLocation = "bd01:B9:0"
                }
            };
        }
        Assert.Equal(GamePlanStatus.Refused, (await SupplyPlan("start", declaration)).Status);
        Assert.Empty(Current.TurnAmmoSupplies);
        Assert.Equal(GamePlanStatus.Refused, (await SupplyPlan("start", declaration with
        {
            CompletionEffects = declaration.CompletionEffects! with
            {
                CreateSupply = false
            },
        })).Status);
        Assert.Empty(Current.TurnWork);
    }
}
