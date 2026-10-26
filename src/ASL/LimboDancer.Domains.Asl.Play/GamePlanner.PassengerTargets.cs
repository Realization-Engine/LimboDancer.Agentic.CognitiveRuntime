using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Passengers and Riders as targets (pass 65, task 65.1; rulings R65.5 to R65.9): the attacks and shots planned since the pass carry each vehicle's PRC,
/// read from the state's index once per state (ruling R65.25); Rules decides what reaches them, and the events here write what it decided.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// A vehicle's PRC as the Fire and Ordnance packages read them (ruling R65.5): each unit, its role, and the states its Vulnerability and checks
    /// read; empty when nobody is aboard. Null for a concealed or hidden vehicle, whose stack its attacker does not see (A12.1): its attack reads as
    /// before the pass.
    /// </summary>
    internal IReadOnlyList<FirePrc>? PrcOf(GameState state, UnitInstance vehicle) =>
        Is(vehicle, Conditions.Concealed) || Is(vehicle, Conditions.Hidden) ? null
            : [.. Riding(state, vehicle.Id).Select(unit => (Unit: unit, Aboard: AboardFacts(state, unit))).Select(item => new FirePrc(item.Unit.Id, item.Unit.Definition?.Definition,
                item.Unit.Position is ContainedPosition { Role: ContainmentRole.Rider } ? FirePrc.Rider : FirePrc.Passenger, Is(item.Unit, Conditions.Broken) ? true : null,
                Is(item.Unit, Conditions.Pinned) ? true : null)
            {
                Wounded = Is(item.Unit, Conditions.Wounded) ? true : null,
                Fanatic = Is(item.Unit, Conditions.Fanatic) ? true : null,
                Elr = item.Unit.Group is not null ? state.ElrOf(item.Unit) : state.Side(item.Unit.Side)?.Elr,

                // Pass 70 (ruling R70.8): whether the vehicle moved this phase, and a Passenger CE in an unmoved armored halftrack, for D6.651's reach.
                VehicleMoving = item.Aboard?.VehicleMoving ?? VehicleMoved(state, vehicle, false),
                CeInUnmovedHalftrack = item.Aboard?.CeInUnmovedHalftrack,
            })];

    /// <summary>
    /// An attack with the PRC of each vehicle in its target Location, and of the AFV it names (ruling R65.5). The units boarding with the expenditure the
    /// attack answers are attacked on foot, not as PRC (D5.43; ruling R65.14). Pass 70 (ruling R70.7): each Unarmed target says so.
    /// </summary>
    internal FireAttack WithPrc(GameState state, FireAttack attack) => attack with
    {
        Vehicles = attack.Vehicles is null ? null
            : [.. attack.Vehicles.Select(vehicle => state.Unit(vehicle.VehicleId ?? string.Empty) is { } unit
                ? WithCrewState(state, vehicle with { Prc = OnFoot(PrcOf(state, unit), attack.Embarking?.Units) }, unit)
                : vehicle)],
        TargetVehicle = attack.TargetVehicle is { VehicleId: { } named } aimed && state.Unit(named) is { } target ? aimed with
        {
            Prc = PrcOf(state, target)
        } : attack.TargetVehicle,
        Targets = attack.Targets is null ? null
            : [.. attack.Targets.Select(target => target.UnitId is { } id && MemoOf(state).Objects(state).Unit(id) is { } unit && Is(unit, Conditions.Unarmed) ? target with { Unarmed = true } : target)],
    };

    /// <summary>
    /// A vehicle's Shock and Abandonment, written with its PRC for its crew's TC and Crew Survival (D5.5, D5.6; pass 65, ruling R65.21): nothing when the
    /// vehicle carries no PRC fact, so that its attack reads as before the pass. Pass 70 (ruling R70.10): so is a crew inside already broken (D5.311).
    /// </summary>
    private FireVehicle WithCrewState(GameState state, FireVehicle vehicle, UnitInstance unit) => vehicle.Prc is null ? vehicle : vehicle with
    {
        Shocked = Is(unit, Conditions.Shocked) || Is(unit, Conditions.UnconfirmedKill) ? true : null,
        Abandoned = Is(unit, Conditions.Abandoned) ? true : null,
        CrewBroken = MemoOf(state).Objects(state).CrewOf(unit.Id) is { } crew && Is(crew, Conditions.Broken) ? true : null,
    };

    private static IReadOnlyList<FirePrc>? OnFoot(IReadOnlyList<FirePrc>? prc, IReadOnlyList<string>? loaders) =>
        prc is null || loaders is null ? prc : [.. prc.Where(item => item.UnitId is not { } id || !loaders.Contains(id, StringComparer.Ordinal))];

    /// <summary>A shot with the PRC of the vehicle it fires at and of each vehicle its hit attacks (ruling R65.5).</summary>
    internal OrdnanceShot WithPrc(GameState state, OrdnanceShot shot) => shot with
    {
        VehicleTarget = shot.VehicleTarget is { VehicleId: { } named } aimed && state.Unit(named) is { } target && PrcOf(state, target) is var prc ? aimed with
        {
            Prc = prc,

            // D5.5, D8.11, D8.22 (pass 65, ruling R65.21): whether the target is already bogged or immobilized, for the TC's second cause.
            Immobile = prc is not null && (Is(target, Conditions.Immobilized) || Is(target, Conditions.Bogged)) ? true : null,
        } : shot.VehicleTarget,
        Hit = shot.Hit is null ? null : WithPrc(state, shot.Hit),
    };

    /// <summary>
    /// What a proposal says of the PRC an attack or a shot reaches (D.8; pass 65): "; it reaches de-s aboard de-ht where Vulnerable (D.8B)" for each
    /// vehicle that carries any; empty when none does.
    /// </summary>
    private static string PrcPreview(IEnumerable<(string? VehicleId, IReadOnlyList<FirePrc>? Prc)> vehicles, string rule) =>
        string.Concat(vehicles.Where(item => item.Prc is { Count: > 0 })
            .Select(item => $"; it reaches {string.Join(", ", item.Prc!.Select(unit => unit.UnitId))} aboard {item.VehicleId} where Vulnerable ({rule})"));

    /// <summary>
    /// The events of what a Collateral Attack did to a vehicle's PRC (D.8, D6.62, D6.66, A10.62; rulings R65.7, R65.9), as Rules decides it: each unit's
    /// result as a target unit's is written, a broken one under DM where the attack could have caused a NMC; a broken BU Passenger put under DM alone.
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> PrcEffectEvents(GameState state, FireVehicleEffect effect, string attemptId)
    {
        var desperate = effect.PrcDesperation ?? [];
        var attacked = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in (effect.PrcAttacks ?? []).SelectMany(item => item.Effects))
        {
            attacked.Add(unit.UnitId);
            if (state.Unit(unit.UnitId) is not { Status: InstanceStatus.Active })
            {
                continue;
            }

            foreach (var payload in EffectEvents(state, unit, attemptId, desperate.Contains(unit.UnitId)))
            {
                yield return payload;
            }
        }

        foreach (var id in desperate.Where(id => !attacked.Contains(id)))
        {
            if (state.Unit(id) is { Status: InstanceStatus.Active } unit && !Is(unit, Conditions.DesperationMorale))
            {
                yield return ("conditions-changed", new ConditionsChanged(id, new Dictionary<string, ConditionState>(StringComparer.Ordinal)
                {
                    [Conditions.DesperationMorale] = ConditionState.True
                }));
            }
        }
    }

    /// <summary>
    /// The PRC that survive their vehicle's destruction, placed beneath the wreck on foot before it is written (D6.9, D5.6; ruling R65.9), so that the
    /// projector eliminates only the others with it; the SW a survivor holds goes with it. Each is then under Hazardous Movement for the phase (ruling R65.21).
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> SurvivorEvents(GameState state, string vehicleId, IReadOnlyList<PrcSurvival>? survival)
    {
        if (survival is not { Count: > 0 } || state.Location(vehicleId)?.Location is not BoardLocation at)
        {
            yield break;
        }

        foreach (var survivor in survival.Where(item => item.Survived))
        {
            if (state.Unit(survivor.UnitId) is { Status: InstanceStatus.Active })
            {
                // D6.9, D5.6 (the referee's review): placed "expending all remaining MF".
                yield return ("instance-moved", new InstanceMoved(survivor.UnitId, new MapPosition(at)) { Ended = true });

                // D5.6 (pass 65, ruling R65.21): under Hazardous Movement for the rest of the phase.
                yield return Hazardous(survivor.UnitId);
            }
        }
    }
}
