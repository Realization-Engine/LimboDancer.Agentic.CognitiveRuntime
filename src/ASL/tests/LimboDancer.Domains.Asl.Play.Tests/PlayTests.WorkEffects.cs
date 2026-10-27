using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ASL.Turns;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class PlayTests
{
    private JsonElement EffectsArgs(string id, string attempt, string operation, TurnWorkDeclaration? declaration = null)
    {
        var node = JsonNode.Parse(WorkArgs(attempt, operation, declaration).GetRawText())!;
        node["workId"] = id;
        return JsonSerializer.SerializeToElement(node);
    }

    private async Task<GamePlay> PackingEffectsGame()
    {
        var play = Play();
        var gun = new
        {
            id = "gun",
            kind = "asl:gun",
            definition = "attacker-inf-gun",
            side = "german",
            position = new
            {
                at = "bd01:D4:0",
                facing = "east"
            },
            holding = new
            {
                holder = "g1",
                role = "manned"
            },
            conditions = new Dictionary<string, bool> { [Conditions.Malfunctioned] = false },
        };
        await Commit(play, GameActions.Setup, SetupArgs("setup-packing", 0,
            [Unit("g1", "attacker-squad", "german", "bd01:D4:0"), Unit("g2", "attacker-squad", "german", "bd01:D4:0"), gun]));
        return play;
    }

    [Fact]
    public async Task WorkEffectsPackAndUnpackChangeAssemblyAndManningExactlyOnce()
    {
        var play = await PackingEffectsGame();
        var pack = new TurnWorkDeclaration("pack", "german", "gun", ["gun", "g1"], TurnWorkKind.AnimalPacking, null, 1, "adjudicated G10 final assembly")
        {
            CompletionEffects = new(TurnWorkEffectKind.PackGun, Crew: "g1")
        };
        await WorkCommit(play, EffectsArgs("pack", "start-pack", "start", pack));
        Assert.Empty(Current().PackedGuns);
        await WorkPhases(play, 16);
        var completion = EffectsArgs("pack", "finish-pack", "complete");
        await WorkCommit(play, completion);
        Assert.Contains("gun", Current().PackedGuns);
        Assert.Null(((EquipmentInstance)Current().Find("gun")!).Holding);
        Assert.Equal("bd01:D4:0", Current().Location("gun")!.Location.ToString());
        Assert.Equal(PlayOutcome.Replay, (await WorkCommit(play, completion)).Outcome);
        Assert.Single(Current().PackedGuns);
        Assert.Contains("packed-gun", LiveOrdnance.FromState(Current(), "gun", BoardLocation.Parse("bd01:E4:0")).Reason!, StringComparison.Ordinal);
        var rearm = await Planner().PlanAsync(GameActions.TurnGun, Args(new
        {
            gameId = "village",
            attemptId = "turn-packed",
            expectedRevision = Current().Revision,
            gunId = "gun",
            facing = "east"
        }), Tenant);
        Assert.Equal(GamePlanStatus.Refused, rearm.Status);
        var guide = await Planner().ReadTurnGuideProfileAsync(Current().Scope, Perspective.Side("german"), "rph", "existing-engine-work-v1");
        Assert.Equal(TurnWorkEffectStatus.Applied, Assert.Single(guide.Work).EffectStatus);
        Assert.Equal(TurnSupport.Pending, guide.Work[0].RuleAutomation);
        var record = store.Read(Current().Scope)!;
        var workEvent = record.Events.First(e => e.Payload is TurnWorkRecorded);
        Assert.Equal(TurnWorkRecorded.AuditedType, workEvent.Type);
        var downgraded = record.Events.Select(e => e.EventId == workEvent.EventId ? e with { Type = TurnWorkRecorded.LifecycleType } : e).ToArray();
        Assert.True(Planner().Replay(downgraded).HasErrors);
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(GameEventWriter.Write(Current().Scope, record)));
        Assert.False(read.HasErrors);
        Assert.Contains("gun", Planner().Replay(read.Record!.Events).Current!.PackedGuns);
        var unpack = pack with
        {
            Id = "unpack",
            CompletionEffects = new(TurnWorkEffectKind.UnpackGun, Crew: "g1")
        };
        await WorkCommit(play, EffectsArgs("unpack", "start-unpack", "start", unpack));
        await WorkPhases(play, 16);
        await WorkCommit(play, EffectsArgs("unpack", "finish-unpack", "complete"));
        Assert.Empty(Current().PackedGuns);
        Assert.Equal(new Holding("g1", HoldingRole.Manned), ((EquipmentInstance)Current().Find("gun")!).Holding);
        Assert.All(Current().TurnWork, work => Assert.True(work.EffectsApplied));
    }

    [Fact]
    public async Task WorkEffectsCannotBeRetargetedAndStaleCrewFactsAbortTheWholeCompletion()
    {
        var play = await PackingEffectsGame();
        var pack = new TurnWorkDeclaration("pack", "german", "gun", ["gun", "g1", "g2"], TurnWorkKind.AnimalPacking, null, 1, "G10 ruling")
        {
            CompletionEffects = new(TurnWorkEffectKind.PackGun, Crew: "g1")
        };
        await WorkCommit(play, EffectsArgs("pack", "start", "start", pack));
        await WorkPhases(play, 16);
        var plan = await Planner().PlanAsync(GameActions.RecordTurnWork, EffectsArgs("pack", "retarget", "complete",
            pack with
            {
                CompletionEffects = new(TurnWorkEffectKind.PackGun, Crew: "g2")
            }), Tenant);
        Assert.Equal(GamePlanStatus.Refused, plan.Status);
        var events = store.Read(Current().Scope)!.Events;
        var changed = events[^1] with
        {
            EventId = "crew-changed",
            Revision = events.Count + 1,
            Type = "equipment-transferred",
            Payload = new EquipmentTransferred("gun", new Holding("g2", HoldingRole.Manned), ((EquipmentInstance)Current().Find("gun")!).Position),
        };
        var complete = changed with
        {
            EventId = "bad-completion",
            Revision = changed.Revision + 1,
            Type = TurnWorkRecorded.AuditedType,
            Payload = new TurnWorkRecorded("pack", "complete", null, null, "attempt"),
        };
        var history = Planner().Replay([.. events, changed, complete]);
        Assert.True(history.HasErrors);
        var prefix = Planner().Replay([.. events, changed]);
        Assert.False(prefix.HasErrors);
        var lastValid = prefix.Current!;
        Assert.NotNull(lastValid);
        Assert.Empty(lastValid.PackedGuns);
        Assert.False(lastValid.TurnWork[0].Completed);
        Assert.False(lastValid.TurnWork[0].Working);
        Assert.Equal(ConditionState.False, GameState.Condition(lastValid.Unit("g1")!, "asl:ti"));
        var append = store.Append(Current().Scope, "work", events.Count, [changed, complete], Planner().Replay);
        Assert.Equal(AppendStatus.Invalid, append.Status);
        Assert.Equal(events.Count, Current().Revision);
        Assert.Equal(new Holding("g1", HoldingRole.Manned), ((EquipmentInstance)Current().Find("gun")!).Holding);
    }
}
