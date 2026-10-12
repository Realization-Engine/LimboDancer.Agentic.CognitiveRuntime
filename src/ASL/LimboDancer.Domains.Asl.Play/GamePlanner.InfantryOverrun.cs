using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Infantry OVR in live play (A4.15 to A4.152, A20.54; pass 40, task 40.4): the stack's NTC, taken as an action of its own before the entry; the
/// facts of the entry, which the move reads; and the SMC's move away to a Location the ATTACKER chooses. The rules are
/// <see cref="ScenarioA1InfantryOverrun"/>'s; this file reads the request, the state, and the map, and writes the events.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>A unit of a stack that would make an Infantry OVR, as Rules reads it.</summary>
    private InfantryOverrunMoverFacts OverrunMover(UnitInstance unit) =>
        new(unit.Id, vocabulary.IsA(unit.Kind, "asl:mmc"), vocabulary.IsA(unit.Kind, "asl:leader"), GameState.GoodOrder(unit, vocabulary) != ConditionState.False,
            Is(unit, Conditions.Berserk), Is(unit, Conditions.Pinned), unit.MovementEnded);

    /// <summary>The enemy units of a Location as the side entering it knows them, for an Infantry OVR.</summary>
    private InfantryOverrunDefenderFacts[] OverrunDefenders(GameState state, BoardLocation at, string side) =>
        [.. state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && !Is(unit, Conditions.Captured))
            .Select(unit => new InfantryOverrunDefenderFacts(unit.Id, KnownEnemy(unit), vocabulary.IsA(unit.Kind, "asl:smc"), Is(unit, Conditions.Melee), Is(unit, Conditions.Unarmed),
                LiveFire.IsVehicle(unit)))];

    /// <summary>The NTC of an Infantry OVR (A4.15): by the stack's leader for all of it, or by the one MMC, against the TEM of the Location it would enter.</summary>
    private GamePlan PlanInfantryOverrunNtc(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        var ids = Strings(arguments, "unitIds").ToArray();
        if (ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length || !Text(arguments, "to", out var toText) || !BoardLocation.TryParse(toText, out var to))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: an Infantry OVR's NTC names the stack and the Location it would enter");
        }

        var found = ids.Select(id => state.Unit(id)).ToArray();
        if (state.Phase != "mph" || found.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side != state.PhasingSide || LiveFire.IsVehicle(unit)))
        {
            return Refused(scope, label, expected, "play.overrun-ntc: the NTC is taken in the MPh by Infantry of the phasing side (A4.15)");
        }

        UnitInstance[] movers = [.. found.Select(unit => unit!)];
        var origins = movers.Select(unit => state.Location(unit.Id)?.Location).Distinct().ToArray();
        if (origins is not [{ } from] || (state.Movement is { } moving && !ids.All(id => moving.Members.Contains(id, StringComparer.Ordinal))))
        {
            return Refused(scope, label, expected, "play.overrun-ntc: the stack stands in one Location, and no other stack's move is open (A4.15, A4.2)");
        }

        if (InfantryStep(state, from, to!).Entry is null)
        {
            return Refused(scope, label, expected, $"play.overrun-ntc: {to} is not a Location the stack could enter from {from} (A4.15)");
        }

        if (ScenarioA1InfantryOverrun.TargetBar(OverrunDefenders(state, to!, state.PhasingSide!), to!.ToString()) is { } targetBar)
        {
            return Refused(scope, label, expected, targetBar);
        }

        var (takerId, takerRefusal) = ScenarioA1InfantryOverrun.NtcTaker([.. movers.Select(OverrunMover)]);
        if (takerRefusal is not null || takerId is null)
        {
            return Refused(scope, label, expected, takerRefusal!);
        }

        if (ScenarioA1InfantryOverrun.AlreadyTakenBar(state.InfantryOverrunNtcs.Any(taken => taken.Target == to.ToString() && taken.Units.Any(id => ids.Contains(id, StringComparer.Ordinal))),
            to.ToString()) is { } takenBar)
        {
            return Refused(scope, label, expected, takenBar);
        }

        var taker = movers.First(unit => unit.Id == takerId);
        var morale = taker.Definition is { } reference
            ? catalogs.FirstOrDefault(item => item.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:morale")?.Value?.Number
            : null;
        var terrain = ReadLocation(state, to) is { } read ? TerrainKey(read) : null;
        if (morale is not { } moraleLevel || terrain is null || !ScenarioA1FireReference.Tem.TryGetValue(terrain, out var tem))
        {
            return Refused(scope, label, expected, $"play.overrun-ntc: the Morale Level of {takerId} or the TEM of {to} is not decided by the catalog (A4.15)");
        }

        // "Hexside TEM apply only if the present LOS crosses that hexside": the wall or hedge between the stack and the SMC.
        var (hexside, _) = HexsideTemAt(state, to, [(from, 1)], state.PhasingSide!, existing);
        var drm = ScenarioA1InfantryOverrun.NtcDrm(tem, hexside?.Tem ?? 0);
        var leader = vocabulary.IsA(taker.Kind, "asl:leader");

        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var dice = draw(new RollRequest(2, 6));
            var rollId = $"{attemptId}-roll-1";
            var passed = ScenarioA1InfantryOverrun.NtcPassed(dice.Values[0], dice.Values[1], drm, moraleLevel);
            return
            [
                Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, ScenarioA1InfantryOverrun.NtcPurpose, 2, 6, dice.Values, DiceRolled.SystemSource, actor), null, null),
                Event(scope, attemptId, 2, expected, "infantry-overrun-ntc", new InfantryOverrunNtc(ids, takerId, to, rollId, moraleLevel, drm, passed), null, null, [EventId(attemptId, 1)]),
            ];
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1InfantryOverrun.NtcText(takerId, leader, string.Join(", ", ids), to.ToString(), moraleLevel, drm)])
        {
            Roll = new PlannedRoll(ScenarioA1InfantryOverrun.NtcPurpose, Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// The SMC of a Location an Infantry OVR has just entered that may still move away (A4.151), for the Play page; null when there is none.
    /// </summary>
    public string? OverrunEvader(GameState state, IReadOnlyList<GameEvent> existing)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(existing);
        if (state.Phase != "mph" || state.Choice is not null || state.Movement is not { InfantryOverrun: { } at, WindowOpen: true })
        {
            return null;
        }

        var smc = state.At(at).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide && !Is(unit, Conditions.Captured)
            && vocabulary.IsA(unit.Kind, "asl:smc"));
        return smc is not null && EvadeBarOf(state, existing, smc) is null ? smc.Id : null;
    }

    /// <summary>Why a unit may not move away from an Infantry OVR (A4.151); null when it may.</summary>
    private string? EvadeBarOf(GameState state, IReadOnlyList<GameEvent> existing, UnitInstance unit)
    {
        var movement = state.Movement;
        var here = state.Location(unit.Id)?.Location;
        var lastStep = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is MovementStepped);
        var firedSince = lastStep.item is not null && existing.Skip(lastStep.index + 1).Any(item => item.Payload is FireResolved or OrdnanceFired);
        return ScenarioA1InfantryOverrun.EvadeBar(unit.Id, movement?.InfantryOverrun is { } at && at == here && unit.Side != state.PhasingSide, movement?.WindowOpen == true, firedSince,
            vocabulary.IsA(unit.Kind, "asl:smc"), GameState.GoodOrder(unit, vocabulary) != ConditionState.False, Is(unit, Conditions.Pinned), Is(unit, Conditions.Melee), Is(unit, "asl:ti"),
            here is null ? 0 : EvadeDestinations(state, here, unit.Side).Count);
    }

    /// <summary>
    /// The adjacent Locations a SMC may be sent to (A4.151): ground-level hexes it could enter by an ordinary step, in the playable area, that hold
    /// no unit of the other side; Rules leaves Open Ground out when anything else is open.
    /// </summary>
    private IReadOnlyList<string> EvadeDestinations(GameState state, BoardLocation from, string side) =>
        ScenarioA1InfantryOverrun.EvadeDestinations([.. Neighbors(state, from)
            .Where(near => InfantryStep(state, from, near).Entry is not null && PlayableBar(state, near) is null
                && !state.At(near).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != side && !Is(other, Conditions.Captured)))
            .Select(near => (near.ToString(), ReadLocation(state, near) is { } read && TerrainKey(read) == "open-ground"))]);

    /// <summary>The DEFENDER's declaration that its SMC moves away from an Infantry OVR (A4.151): the ATTACKER is then asked where.</summary>
    private GamePlan PlanOverrunEvade(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId) || state.Unit(unitId) is not { Status: InstanceStatus.Active } unit)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a move away from an Infantry OVR names the SMC");
        }

        if (EvadeBarOf(state, existing, unit) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        var options = EvadeDestinations(state, state.Location(unit.Id)!.Location, unit.Side);
        var key = $"{ChoicePending.OverrunEvade}:{unit.Id}:{expected.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "choice-pending", new ChoicePending(key, ChoicePending.OverrunEvade, state.PhasingSide!, options,
                JsonSerializer.SerializeToElement(new JsonObject { ["unit"] = unit.Id })), null, null)],
            [ScenarioA1InfantryOverrun.EvadeText(unit.Id, options)]);
    }

    /// <summary>The ATTACKER's answer (A4.151): the SMC enters the Location named, free of all fire.</summary>
    private GamePlan PlanOverrunEvadeAnswer(GameScope scope, string attemptId, long expected, string label, GameState state, PendingChoice pending, GameEvent made, string option)
    {
        var unitId = pending.Resume.TryGetProperty("unit", out var named) ? named.GetString() : null;
        if (unitId is null || state.Unit(unitId) is not { Status: InstanceStatus.Active } || !BoardLocation.TryParse(option, out var to))
        {
            return Refused(scope, label, expected, "play.overrun-evade: the SMC or the Location chosen is not in the game any more (A4.151)");
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [made, Event(scope, attemptId, 2, expected, "instance-moved", new InstanceMoved(unitId, new MapPosition(to!)), null, null, [made.EventId])],
            [ScenarioA1InfantryOverrun.EvadedText(unitId, to!.ToString())]);
    }
}
