using System.Text;
using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class BacklogPass7Tests
{
    private static TurnWorkDeclaration AuditedAmmo() => new("ammo", "german", "de-tank", ["de-tank", "supply-truck"],
        TurnWorkKind.AmmoReplenishment, 10, null, "E10.3 adjudicated qualification")
    {
        CompletionEffects = new(TurnWorkEffectKind.Replenish, SupplyId: "supply", SupplyVehicle: "supply-truck", CreateSupply: true, RecordLowAmmo: true),
    };
    private void AuditCondition(string unit, string condition, ConditionState value)
    {
        var events = store.Read(Scope)!.Events;
        var change = events[^1] with
        {
            EventId = "audit-condition-" + Revision,
            Revision = Revision + 1,
            Type = "conditions-changed",
            Payload = new ConditionsChanged(unit, new Dictionary<string, ConditionState> { [condition] = value }),
        };
        Assert.Equal(AppendStatus.Committed, store.Append(Scope, "audit", Revision, [change], Planner().Replay).Status);
    }

    [Theory]
    [InlineData(ConditionState.Unknown)]
    [InlineData(ConditionState.True)]
    [InlineData(ConditionState.Withheld)]
    [InlineData(ConditionState.Inapplicable)]
    public async Task WorkAuditRejectsUnprovenCeWithoutPartialSupplyFacts(ConditionState posture)
    {
        await Tanks(1942, "north-east", "east", Vehicle("supply-truck", "attacker-truck", "bd01:B10:0", "german"));
        AuditCondition("de-tank", Conditions.ButtonedUp, posture);
        Assert.Equal(GamePlanStatus.Refused, (await SupplyPlan("start", AuditedAmmo())).Status);
        Assert.Empty(Current.TurnWork);
        Assert.Empty(Current.TurnAmmoSupplies);
        Assert.Empty(Current.LowAmmoVehicles);
    }

    [Fact]
    public async Task WorkAuditRechecksResumeAndPreservesOnlyCompletePriorIntervals()
    {
        await WorkTanks(1942, "north-east", "east", Vehicle("supply-truck", "attacker-truck", "bd01:B10:0", "german"));
        Committed(await RecordWorkEffect("start", "start", AuditedAmmo()));
        Assert.Equal(TurnWorkRecorded.AuditedType, store.Read(Scope)!.Events[^1].Type);
        Assert.Equal(1, Current.TurnWork[0].HostAuditVersion);
        await Advance(16);
        AuditCondition("de-tank", Conditions.ButtonedUp, ConditionState.True);
        Assert.False(Current.TurnWork[0].Working);
        Assert.Equal(1, Current.TurnWork[0].CompletedGameTurns);
        Assert.Equal(ConditionState.False, GameState.Condition(Current.Unit("supply-truck")!, "asl:ti"));
        Assert.Equal(GamePlanStatus.Refused, (await SupplyPlan("resume")).Status);
        AuditCondition("de-tank", Conditions.ButtonedUp, ConditionState.False);
        Committed(await RecordWorkEffect("resume", "resume"));
        await Advance(15);
        Committed(await RecordWorkEffect("finish", "complete", roller: Once(1, 1)));
        Assert.True(Current.TurnWork[0].EffectsApplied);
        var parsed = GameEventReader.Read(Encoding.UTF8.GetBytes(GameEventWriter.Write(Scope, store.Read(Scope)!)));
        Assert.False(parsed.HasErrors);
        Assert.Equal(1, Planner().Replay(parsed.Record!.Events).Current!.TurnWork[0].HostAuditVersion);
        var legacyEvents = parsed.Record.Events.Select(item => item.Type == TurnWorkRecorded.AuditedType
            ? item with
            {
                Type = TurnWorkRecorded.EffectsType
            } : item).ToArray();
        var legacyReplay = Planner().Replay(legacyEvents);
        Assert.False(legacyReplay.HasErrors);
        Assert.Equal(0, Assert.Single(legacyReplay.Current!.TurnWork).HostAuditVersion);
        Assert.True(legacyReplay.Current.TurnWork[0].EffectsApplied);
        var guide = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "ccph", "existing-engine-work-v1");
        Assert.Equal(1, Assert.Single(guide.Work).HostAuditVersion);
        Assert.Equal(GameActions.WorkAdjudicationPermission, Assert.Single(guide.Profile.Bounds, item => item.Id == "work").Permission);
    }

    [Fact]
    public async Task WorkAuditCapturedParticipantInterruptsAndCannotResume()
    {
        await WorkTanks(1942, "north-east", "east", Vehicle("supply-truck", "attacker-truck", "bd01:B10:0", "german"));
        Committed(await RecordWorkEffect("start", "start", AuditedAmmo()));
        AuditCondition("supply-truck", Conditions.Captured, ConditionState.True);
        Assert.False(Current.TurnWork[0].Working);
        Assert.Equal(GamePlanStatus.Refused, (await SupplyPlan("resume")).Status);
        Assert.Equal(GamePlanStatus.Refused, (await SupplyPlan("complete")).Status);
        Assert.True(Assert.Single(Current.TurnAmmoSupplies).Available);
        Committed(await RecordWorkEffect("cancel", "cancel"));
        Assert.True(Current.TurnWork[0].Cancelled);
    }

    [Fact]
    public async Task WorkAuditPreservesLegacyReplayButAllowsOnlyCancellationOfOldOpenWork()
    {
        await Tanks(1942, "north-east", "east", Vehicle("supply-truck", "attacker-truck", "bd01:B10:0", "german"));
        var events = store.Read(Scope)!.Events;
        var start = events[^1] with
        {
            EventId = "legacy-work",
            Revision = Revision + 1,
            Type = TurnWorkRecorded.EffectsType,
            Payload = new TurnWorkRecorded("ammo", "start", AuditedAmmo(), null, "legacy adjudication"),
        };
        Assert.Equal(AppendStatus.Committed, store.Append(Scope, "legacy", Revision, [start], Planner().Replay).Status);
        Assert.Equal(0, Current.TurnWork[0].HostAuditVersion);
        Assert.True(Current.TurnWork[0].Working); // Legacy unknown posture is not retroactively reinterpreted.
        Assert.Equal(GamePlanStatus.Refused, (await SupplyPlan("resume")).Status);
        Assert.Equal(GamePlanStatus.Refused, (await SupplyPlan("complete")).Status);
        var upgrade = start with
        {
            EventId = "illegal-upgrade",
            Revision = Revision + 1,
            Type = TurnWorkRecorded.AuditedType,
            Payload = new TurnWorkRecorded("ammo", "cancel", null, null, "not a migration"),
        };
        Assert.True(Planner().Replay([.. store.Read(Scope)!.Events, upgrade]).HasErrors);
        Committed(await RecordWorkEffect("cancel", "cancel"));
        Assert.True(Current.TurnWork[0].Cancelled);
        Assert.True(Assert.Single(Current.TurnAmmoSupplies).Available);
    }
}
