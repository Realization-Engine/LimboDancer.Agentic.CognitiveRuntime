using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The actions of Passengers (pass 65, task 65.2; rulings R65.10 to R65.13): Mounted Fire with LOS from the vehicle's Location, the fire groups and
/// leaders of D6.64 and D6.65, a broken Passenger's rout beneath its vehicle, a SW passed aboard (D6.31), and the Passengers of an OVR. Who rides which
/// vehicle is read from the state's index once per state (ruling R65.25); Rules decides each rule.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The vehicle a unit rides, read from the state's index once per state (ruling R65.25); null for a unit on foot. The page's lists read it.</summary>
    public string? VehicleRidden(GameState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        return VehicleOf(state, unitId);
    }

    /// <summary>Whether a vehicle moved this phase as D6.65 and C.8 read it: it entered a new hex or hexside in this MPh, or is in Motion.</summary>
    private static bool MovedThisPhase(GameState state, UnitInstance vehicle) =>
        (state.Phase == "mph" && state.MovedVehicles.Contains(vehicle.Id, StringComparer.Ordinal)) || Is(vehicle, Conditions.Motion);

    /// <summary>Whether an armored vehicle's Passengers are CE with its crew (D6.62, D6.84): a Carrier's always; a truck's are always exposed.</summary>
    private static bool PassengersExposed(UnitInstance vehicle, FireDefinition definition) =>
        definition.Unarmored == true || definition.VehicleClass == "carrier" || LiveFire.CrewExposed(vehicle);

    /// <summary>
    /// An attack with each Passenger firer's Mounted Fire (D6.1, D6.62, D6.63; ruling R65.10) and the makeup of its group and leaders (D6.64, D6.65;
    /// ruling R65.12), or why it may not be made. An attack with nobody aboard is returned as it is.
    /// </summary>
    private (FireAttack? Attack, string? Refusal) WithMounted(GameState state, FireAttack attack)
    {
        var firers = attack.Firers ?? [];
        string?[] directors = [attack.Director?.UnitId, .. (attack.OtherDirectors ?? []).Select(item => item.UnitId)];
        if (firers.All(firer => VehicleOf(state, firer.UnitId ?? string.Empty) is null) && directors.All(id => id is null || VehicleOf(state, id) is null))
        {
            return (attack, null);
        }

        var members = new List<FireGroupMemberFacts>();
        var mounted = new List<FireFirer>();
        foreach (var firer in firers)
        {
            if (VehicleOf(state, firer.UnitId!) is not { } vehicleId || state.Unit(vehicleId) is not { } vehicle || VehicleDefinition(vehicle) is not { } definition
                || state.Unit(firer.UnitId!) is not { } unit)
            {
                members.Add(new FireGroupMemberFacts(firer.UnitId!, null, null, false));
                mounted.Add(firer);
                continue;
            }

            // D5.8 (pass 65, ruling R65.20): a unit crewing the vehicle from inside fires its armament, not its own FP.
            if (unit.Position is ContainedPosition { Role: ContainmentRole.Crew })
            {
                return (null, ScenarioA1CrewActsRules.CrewInsideFireBar(unit.Id, vehicle.Id));
            }

            // D6.22 (pass 65, ruling R65.17): a Rider is always exposed, and its FP is quartered once its AFV has moved this Player Turn.
            var rider = unit.Position is ContainedPosition { Role: ContainmentRole.Rider };
            var exposed = rider || PassengersExposed(vehicle, definition);
            var moved = rider ? state.MovedVehicles.Contains(vehicle.Id, StringComparer.Ordinal) : MovedThisPhase(state, vehicle);
            var verdict = ScenarioA1PassengerActionRules.MountedFire(new MountedFireFacts(unit.Id, vehicle.Id, definition, exposed,
                !Is(unit, Conditions.Pinned) && !Is(unit, Conditions.Broken), moved, Is(vehicle, Conditions.Motion), vehicle.Straddling is not null, attack.Phase,
                [.. (firer.Weapons ?? []).Select(weapon => new MountedWeaponFacts(weapon.EquipmentId ?? string.Empty,
                    weapon.DefinitionId is { } held ? FireReference.Value.Definitions.GetValueOrDefault(held) : null))])
            {
                Rider = rider
            });
            if (verdict.Refusal is { } refusal)
            {
                return (null, refusal);
            }

            var role = rider ? FirePrc.Rider : FirePrc.Passenger;
            members.Add(new FireGroupMemberFacts(unit.Id, vehicle.Id, role, !rider && definition.Unarmored != true && exposed));
            mounted.Add(firer with
            {
                Mounted = new FireMounted(vehicle.Id, vehicle.Definition!.Definition)
                {
                    Moved = moved ? true : null,
                    Motion = Is(vehicle, Conditions.Motion) ? true : null,
                    Rider = rider ? true : null,
                },
            });
        }

        var leaders = new List<FireGroupLeaderFacts>();
        foreach (var id in directors.OfType<string>())
        {
            if (VehicleOf(state, id) is { } vehicleId && state.Unit(vehicleId) is { } vehicle && VehicleDefinition(vehicle) is { } definition && state.Unit(id) is { } leader)
            {
                leaders.Add(new FireGroupLeaderFacts(id, vehicleId, PassengersExposed(vehicle, definition) && !Is(leader, Conditions.Pinned), definition.Unarmored != true,
                    MovedThisPhase(state, vehicle), false));
            }
            else
            {
                leaders.Add(new FireGroupLeaderFacts(id, null, true, false, false, false));
            }
        }

        string[] vehiclesFiring = attack.VehicleFire?.VehicleId is { } firing ? [firing] : [];
        if (ScenarioA1PassengerActionRules.FireGroupBar(members, leaders, vehiclesFiring) is { } groupBar)
        {
            return (null, groupBar);
        }

        return (attack with
        {
            Firers = mounted
        }, null);
    }

    /// <summary>
    /// What a proposal says of each Passenger's Mounted Fire (D6.1, D6.63; ruling R65.10): "; de-s fires from de-truck as Mounted Fire, its FP times 1/4
    /// (D6.1)"; empty when no Passenger fires.
    /// </summary>
    private static string MountedPreview(FireAttack facts) =>
        string.Concat((facts.Firers ?? []).Where(firer => firer.Mounted is { DefinitionId: not null }).Select(firer =>
        {
            var mounted = firer.Mounted!;
            var multipliers = ScenarioA1PassengerActionRules.Multipliers(FireReference.Value.Definitions.GetValueOrDefault(mounted.DefinitionId!)?.Unarmored == true,
                mounted.Moved == true, mounted.Motion == true, facts.Phase, mounted.Rider == true);
            var times = multipliers.Aggregate(1m, (value, item) => value * item.Value);
            return $"; {firer.UnitId} fires from {mounted.VehicleId} as Mounted Fire"
                + (multipliers.Count == 0 ? ", its FP not halved for it (D6.63)"
                    : $", its FP times 1/{(int)(1 / times)} ({string.Join(", ", multipliers.Select(item => item.Rule).Distinct(StringComparer.Ordinal))})");
        }));

    /// <summary>
    /// D.3 (p. 192; ruling R65.10): a vehicle whose Passengers Prep Fire may neither move in the MPh nor fire then or in the AFPh, as after its own Prep
    /// Fire, so it is marked as having Prep Fired.
    /// </summary>
    private void AddMountedPrepFire(GameScope scope, string attemptId, long expected, GameState state, FireAttack facts, List<GameEvent> events)
    {
        if (state.Phase != "pfph")
        {
            return;
        }

        foreach (var vehicleId in (facts.Firers ?? []).Select(firer => firer.Mounted?.VehicleId).OfType<string>().Distinct(StringComparer.Ordinal))
        {
            if (state.Unit(vehicleId) is { } vehicle && !Is(vehicle, Conditions.PrepFire))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                    new ConditionsChanged(vehicleId, new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.PrepFire] = ConditionState.True }), null, null));
            }
        }
    }

    /// <summary>
    /// D6.1 (ruling R65.11): a broken Passenger's vehicle and what it may do in the RtPh: stay aboard, rout beneath, or rout beneath because its vehicle's
    /// inherent crew is gone (Abandoned) or broken. D5.311 (ruling R65.31): a crew broken inside its AFV is in the Crew role, and must rout beneath it
    /// (<c>Crew</c> true). Null for a unit on foot.
    /// </summary>
    private (string Vehicle, string Verdict, bool Crew)? PassengerRout(GameState state, UnitInstance unit)
    {
        if (VehicleOf(state, unit.Id) is not { } vehicleId || state.Unit(vehicleId) is not { } vehicle)
        {
            return null;
        }

        // An inherent crew is eliminated with its vehicle, which then carries nobody (D5.6); the crew is gone when it has Abandoned the vehicle, and broken
        // when its counter is broken inside it (D5.311), read from the state's index (ruling R65.25).
        var crewBroken = MemoOf(state).Objects(state).CrewOf(vehicleId) is { } crew && Is(crew, Conditions.Broken);
        return (vehicleId, ScenarioA1RoutCalculator.RoutBeneath(Is(unit, Conditions.Broken), !Is(vehicle, Conditions.Motion), crewEliminated: false, crewBroken,
            Is(vehicle, Conditions.Abandoned)), unit.Position is ContainedPosition { Role: ContainmentRole.Crew });
    }

    /// <summary>
    /// D6.1, D5.311 (ruling R65.11): a broken Passenger routs beneath its vehicle, spending all its RtPh MF: one rout step into the vehicle's Location,
    /// on foot. D5.311 (ruling R65.31): a broken crew inside its AFV routs beneath it the same way, and the vehicle is Abandoned first.
    /// </summary>
    private GamePlan PlanRoutBeneath(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, GameState state, UnitInstance unit,
        BoardLocation at, (string Vehicle, string Verdict, bool Crew) aboard, int routeSteps, bool lowCrawl)
    {
        if (ScenarioA1RoutCalculator.RoutBeneathBar(unit.Id, aboard.Vehicle, aboard.Verdict, routeSteps, lowCrawl) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        var events = new List<GameEvent>();
        if (aboard.Crew)
        {
            events.Add(Event(scope, attemptId, 1, expected, "conditions-changed",
                new ConditionsChanged(aboard.Vehicle, ConditionChanges(ScenarioA1OrdnanceEventRules.AbandonedConditions)), null, null));
        }

        events.Add(Event(scope, attemptId, events.Count + 1, expected, "rout-stepped", new RoutStepped(unit.Id, at, RoutHalfMf(unit), false), null, null));
        AddAdjacentDm(scope, attemptId, expected, Replay([.. existing, .. events]).Current!, events);
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
            [aboard.Crew
                ? $"play.rout: {unit.Id}, the broken crew of {aboard.Vehicle}, routs beneath it in {at}, spending all its MF; {aboard.Vehicle} is Abandoned (D5.311)"
                : $"play.rout: {unit.Id} routs beneath {aboard.Vehicle} in {at}, spending all its MF"
                    + (aboard.Verdict == ScenarioA1RoutCalculator.MustRoutBeneath ? ", since its vehicle has lost its inherent crew" : string.Empty) + " (D6.1, D5.311)"]);
    }

    /// <summary>
    /// The Passengers who add their FP to a vehicle's OVR as it is declared (D7.11, D6.62, D6.5; ruling R65.13): those named, or every Passenger that may
    /// fire from it (Good Order, unpinned, CE in an armored vehicle) when none is named. Null when none adds, so the record keeps its older shape.
    /// </summary>
    private string[]? OverrunPassengerIds(GameState state, UnitInstance vehicle, IReadOnlyList<string>? named)
    {
        if (VehicleDefinition(vehicle) is not { } definition
            || ScenarioA1PassengerActionRules.OverrunShare(definition.Unarmored == true, PassengersExposed(vehicle, definition), definition.VehicleClass) == 0)
        {
            return null;
        }

        string[] able = [.. Riding(state, vehicle.Id).Where(unit => unit.Position is ContainedPosition { Role: ContainmentRole.Passenger } && !Is(unit, Conditions.Broken)
            && !Is(unit, Conditions.Pinned) && !Is(unit, Conditions.BoundingFire) && !LiveFire.Fired(unit) && (named is null || named.Contains(unit.Id, StringComparer.Ordinal)))
            .Select(unit => unit.Id)];
        return able.Length == 0 ? null : able;
    }

    /// <summary>The Passengers the declaration of a vehicle's current OVR named (D7.11; ruling R65.13), from the last overrunning step this phase.</summary>
    private static IReadOnlyList<string>? DeclaredOverrunPassengers(IReadOnlyList<GameEvent> events, string vehicleId) =>
        ThisPhase(events).Select(item => item.Payload).OfType<VehicleStepped>().LastOrDefault(step => step.Vehicle == vehicleId && step.Overrunning)?.Units;

    /// <summary>What an OVR's declaration says of the Passengers adding their FP (D7.11; ruling R65.13); empty when none does.</summary>
    private static string OverrunPassengerPreview(IReadOnlyList<string>? adding) =>
        adding is { Count: > 0 } ? $"; {string.Join(", ", adding)} add{(adding.Count == 1 ? "s" : string.Empty)} {(adding.Count == 1 ? "its" : "their")} FP to it and may not unload this MPh (D7.11, D6.5)" : string.Empty;

    /// <summary>
    /// An OVR's facts with the Passengers its declaration named (D7.11, D7.16; ruling R65.13), each with its definition, and with the PRC of each enemy
    /// vehicle in the Location, which it may attack where Vulnerable (D7.12, ruling R65.5).
    /// </summary>
    private FireAttack WithOverrunPassengers(GameState state, FireAttack attack, IReadOnlyList<string>? declared) =>
        WithPrc(state, attack.Overrun is { } overrun && declared is { Count: > 0 } ? attack with
        {
            Overrun = overrun with
            {
                Passengers = [.. declared.Select(id => state.Unit(id) is { } unit
                    ? new FireOverrunPassenger(unit.Id, unit.Definition?.Definition) { Wounded = Is(unit, Conditions.Wounded) ? true : null }
                    : new FireOverrunPassenger(id, null))],
            },
        } : attack);

    /// <summary>D6.5 (ruling R65.10): whether a Passenger added its FP to its vehicle's OVR this phase.</summary>
    private static bool OverranThisPhase(IReadOnlyList<GameEvent> existing, string unitId) =>
        ThisPhase(existing).Select(item => item.Payload).OfType<VehicleStepped>().Any(step => step.Overrunning && step.Units?.Contains(unitId, StringComparer.Ordinal) == true);
}
