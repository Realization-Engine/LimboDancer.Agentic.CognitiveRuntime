using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Demolition Charges (backlog pass 15, rulings R15.2 and R15.3): a DC Placed with a move step in the MPh and detonated in the AFPh, and a DC Thrown in a
/// friendly fire phase or as Defensive First Fire, attacking its target Location and then its thrower's. Each attack is a fire record of the Fire package,
/// with no firers; the DC is removed after it.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// A23.3 (ruling R15.2): a unit of the moving stack Places its DC in an ADJACENT Location in its LOS by spending, as the stack's expenditure in its own
    /// Location, the MF entering that Location would cost; not after firing in the PFPh, not pinned, not berserk (A15.431), and not where an enemy AFV is
    /// (the PAATC is not built).
    /// </summary>
    private GamePlan PlanPlaceDc(GameScope scope, string attemptId, long expected, string label, GameState state, UnitInstance[] movers,
        IReadOnlyList<string> ids, BoardLocation from, string chargeId, string targetText, bool assault, bool doubleTime)
    {
        var current = state.Movement;
        if (!BoardLocation.TryParse(targetText, out var target))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a DC Placement names its Location");
        }

        if (state.Find(chargeId) is not EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding } charge
            || FireReference.Value.Definitions.GetValueOrDefault(charge.Definition?.Definition ?? string.Empty)?.IsDc != true
            || movers.FirstOrDefault(unit => unit.Id == holding.Holder) is not { } placer)
        {
            return Refused(scope, label, expected, $"play.dc-placer: {chargeId} is not a DC possessed by a unit of the moving stack (A23.3)");
        }

        if (Is(placer, Conditions.Berserk) || Is(placer, Conditions.PrepFire) || Is(placer, Conditions.Unarmed))
        {
            return Refused(scope, label, expected, $"play.dc-placer: {placer.Id} fired in the PFPh, is berserk, or is Unarmed, and Places no DC (A23.3, A15.431, A20.5)");
        }

        if (state.AssaultWeaponUsers.Contains(placer.Id, StringComparer.Ordinal))
        {
            return Refused(scope, label, expected, $"play.dc-once: {placer.Id} has used a FT or DC this Player Turn and uses no other (A22.3)");
        }

        if (state.PlacedCharges.Any(item => item.Charge == chargeId))
        {
            return Refused(scope, label, expected, $"play.dc-placed: {chargeId} is already Placed (A23.3)");
        }

        if (target == from || !IsAdjacent(state, from, target))
        {
            return Refused(scope, label, expected, $"play.dc-target: a DC is Placed in an ADJACENT Location in the placer's LOS, not {target} (A23.3, A23.61)");
        }

        if (state.At(target).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)))
        {
            return Refused(scope, label, expected, "play.dc-vehicle: a DC is not Placed where a vehicle is: its attack on a vehicle and the PAATC are not built (A23.3, A23.5)");
        }

        if (ReadLocation(state, target) is not { } targetRead || TerrainKey(targetRead) is not { } targetTerrain || !ScenarioA1FireReference.Tem.ContainsKey(targetTerrain))
        {
            return Refused(scope, label, expected, $"play.dc-target: the TEM of {target} is not reviewed, so no DC is Placed there (A23.1)");
        }

        if (EntryCost(state, from, target) is not { } halfMf)
        {
            return Refused(scope, label, expected, $"play.dc-target: the MF to enter {target} are not reviewed, so no DC is Placed there (A23.3)");
        }

        foreach (var unit in movers)
        {
            var extra = doubleTime ? (unit.MfSpent == 0 && !unit.HalfMfSpent ? 2 : 1) : unit.DoubleTimeMf;
            var exhausted = doubleTime || Is(unit, Conditions.Cx);
            if (MfAllotment(state, unit, extra, exhausted) is not { } allowance || MfAllotment(state, unit, 0, exhausted) is not { } plain)
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit.Id} has no MF allowance the catalog decides");
            }

            var spent = (unit.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0);
            var left = (allowance * 2) - spent;
            if (left < halfMf || (assault && (plain * 2) - spent <= halfMf))
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit.Id} has {left / 2m} MF left, and Placing the DC costs {halfMf / 2m} (A23.3, A4.61)");
            }
        }

        // A23.1 (ruling R15.2): the DC's attack is Area Fire when every unit it will attack is concealed at its operable Placement.
        UnitInstance[] there = [.. state.At(target).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != placer.Side)];
        var concealed = there.Length > 0 && there.All(unit => Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden) || unit.Kind == UnitKinds.Dummy);
        var step = (current?.Step ?? 0) + 1;
        var package = ScenarioA1FirePackage.Identity.ToString();
        var placement = new DcPlacement(placer.Id, charge.Id, target, doubleTime || Is(placer, Conditions.Cx), concealed);
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "movement-step", new MovementStepped(ids, from, halfMf, current?.Assault ?? assault, step)
            {
                DoubleTime = doubleTime,
                DcPlacement = placement,
            }, package, null)],
            [$"play.place-dc: {placer.Id} Places {charge.Id} in {target} for {halfMf / 2m} MF of the stack; it is operably Placed once {placer.Id} leaves {from} or ends its move neither broken nor pinned, and detonates in the AFPh with 30 FP (A23.3, A23.4)"]);
    }

    /// <summary>A23.4 (ruling R15.2): in its side's AFPh, an operably Placed DC detonates on its target Location, and is removed.</summary>
    private GamePlan PlanDetonateDc(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "equipmentId", out var chargeId) || state.PlacedCharges.FirstOrDefault(item => item.Charge == chargeId && item.Operable) is not { } placed)
        {
            return Refused(scope, label, expected, "play.dc-detonate: name a DC operably Placed this Player Turn (A23.3)");
        }

        if (state.Phase != "afph")
        {
            return Refused(scope, label, expected, "play.dc-detonate: a Placed DC detonates in its side's AFPh (A23.4)");
        }

        var (facts, reason) = LiveFire.DemolitionChargeFromState(state, chargeId, FireDemolitionCharge.Placed, placed.Target);
        if (facts is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        return ChargePlan(scope, existing, attemptId, expected, label, actor, state, facts with
        {
            Range = 1,
            SameLevel = placed.From.Level == placed.Target.Level,
            TargetLevelAbove = placed.From.Level == placed.Target.Level ? null : placed.Target.Level - placed.From.Level,
            TargetTerrain = ReadLocation(state, placed.Target) is { } read ? TerrainKey(read) : null,
        }, null, chargeId, $"play.detonate-dc: {chargeId} detonates in {placed.Target} with 30 FP{(placed.TargetsConcealed ? ", halved for its concealed targets" : string.Empty)}, and is removed (A23.1, A23.4)");
    }

    /// <summary>
    /// A23.6 to A23.63 (ruling R15.3): a Good Order (or berserk) unpinned unit Throws its DC into an ADJACENT Location in its LOS at its level, in a friendly
    /// fire phase or as Defensive First Fire at the moving stack; a DEFENDING unit marked First Fire throws none, nor may one in Subsequent First Fire or FPF.
    /// The target Location is attacked, then the thrower's; the DC is removed.
    /// </summary>
    private GamePlan PlanThrowDc(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "equipmentId", out var chargeId) || !Text(arguments, "target", out var targetText)
            || !BoardLocation.TryParse(targetText, out var target))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Thrown DC names its thrower, the DC, and the target Location");
        }

        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } thrower || state.Location(thrower.Id)?.Location is not { } from
            || state.Find(chargeId) is not EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding }
            || holding.Holder != thrower.Id || FireReference.Value.Definitions.GetValueOrDefault((state.Find(chargeId) as EquipmentInstance)?.Definition?.Definition ?? string.Empty)?.IsDc != true)
        {
            return Refused(scope, label, expected, $"play.dc-thrower: {unitId} does not possess the DC {chargeId} (A23.6)");
        }

        if (!vocabulary.IsA(thrower.Kind, "asl:personnel") || Is(thrower, Conditions.Pinned) || Is(thrower, Conditions.Captured) || Is(thrower, Conditions.Unarmed)
            || Is(thrower, Conditions.Melee) || (Is(thrower, Conditions.Broken) && !Is(thrower, Conditions.Berserk)))
        {
            return Refused(scope, label, expected, $"play.dc-thrower: {unitId} is not an unpinned Good Order or berserk Personnel unit able to use a SW (A23.6)");
        }

        var phasing = thrower.Side == state.PhasingSide;
        var firePhase = state.Phase switch
        {
            "pfph" or "afph" => phasing,
            "dfph" => !phasing && !Is(thrower, Conditions.FirstFire),
            "mph" => !phasing && state.Movement is { WindowOpen: true } window && window.Location == target && !Is(thrower, Conditions.FirstFire)
                && !Is(thrower, Conditions.FinalFire),
            _ => false,
        };
        if (!firePhase)
        {
            return Refused(scope, label, expected, "play.dc-phase: a DC is Thrown in a friendly fire phase or as Defensive First Fire at the moving stack, never by a unit marked First Fire (A23.6, A23.63)");
        }

        // A7.351: a squad that fired its inherent FP alone this phase may still use one SW; any other firer has spent its fire.
        if (LiveFire.Fired(thrower) && state.Phase != "mph" && !(thrower.Kind == "asl:squad" && state.PhaseFirers.Any(item => item.Unit == thrower.Id && item.Weapon == "0")))
        {
            return Refused(scope, label, expected, $"play.dc-thrower: {unitId} has fired this phase and Throws no DC (A7.351, A23.6)");
        }

        if (state.AssaultWeaponUsers.Contains(thrower.Id, StringComparer.Ordinal))
        {
            return Refused(scope, label, expected, $"play.dc-once: {unitId} has used a FT or DC this Player Turn and uses no other (A22.3)");
        }

        if (target == from || !IsAdjacent(state, from, target))
        {
            return Refused(scope, label, expected, $"play.dc-target: a DC is Thrown into an ADJACENT Location in the thrower's LOS at its level, not {target} (A23.6; ruling R15.3)");
        }

        var (facts, reason) = LiveFire.DemolitionChargeFromState(state, chargeId, FireDemolitionCharge.Thrown, target);
        if (facts is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        return ChargePlan(scope, existing, attemptId, expected, label, actor, state, facts with
        {
            Range = 1,
            SameLevel = true,
            TargetTerrain = ReadLocation(state, target) is { } read ? TerrainKey(read) : null,
        }, from, chargeId, $"play.throw-dc: {unitId} Throws {chargeId} into {target}: 30 FP with +2 there, then its own Location with +3 on a DR of its own{(state.Phase == "afph" ? ", each +1 in the AFPh" : string.Empty)} (A23.6, A23.62)");
    }

    /// <summary>The DC's attack (and, for a Thrown one, its thrower's Location next), its removal, and any Sniper attack the DRs call for.</summary>
    private GamePlan ChargePlan(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor, GameState state,
        FireAttack facts, BoardLocation? thrower, string chargeId, string summary)
    {
        // A15.44, A15.5: the Heat of Battle reads every target needs.
        facts = HeatOfBattleFacts(state, facts);
        var check = ScenarioA1FireCalculator.Precheck(facts, FireReference.Value);
        if (check.Count != 0)
        {
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.fire-refused", "Fire", "attack", check));
        }

        var targetSide = state.Sides.First(item => item.Id != state.Unit(facts.DemolitionCharge!.UserId!)!.Side).Id;
        var package = ScenarioA1FirePackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();

            // Ruling R27.4: the DC's owners' options are asked for like any attack's; its thrower's Location and its removal follow the answers.
            var followUps = new FireFollowUps(facts.TargetLocationId!)
            {
                DcCharge = chargeId,
                DcThrower = thrower?.ToString(),
            };
            AddFireEvents(scope, attemptId, expected, actor, state, facts, targetSide, null, events, draw, followUps: followUps);
            AddFireFollowUps(scope, attemptId, expected, actor, existing, state, targetSide, null, events, draw, followUps);
            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [summary])
        {
            Roll = new PlannedRoll("dc", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
