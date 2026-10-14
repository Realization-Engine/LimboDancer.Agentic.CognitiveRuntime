"""Pass 50, tasks 50.1 and 50.2: Play hands the facts over and writes the events."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
PLAY = "LimboDancer.Domains.Asl.Play/"
LF = PLAY + "LiveFire.cs"
GF = PLAY + "GamePlanner.Fire.cs"
GR = PLAY + "GamePlanner.Rally.cs"

# --- LiveFire: the vehicle's named weapons ---------------------------------------------------------------------------
e.edit(LF, """        if (ScenarioA1FireEligibility.VehicleFiresAlone(vehicleFirers.Length, firerIds.Count, directorIds.Count, weapons is { Count: > 0 }) is { } grouped)
""", """        // D3.5 (pass 50): the weapons named under the vehicle's own id are its BMG and CMG, not a SW of its group.
        if (ScenarioA1FireEligibility.VehicleFiresAlone(vehicleFirers.Length, firerIds.Count, directorIds.Count,
            weapons?.Keys.Any(key => vehicleFirers.All(vehicle => vehicle.Id != key)) == true) is { } grouped)
""")
e.edit(LF, """vehicleFirers.Length > 0 ? VehicleFire(state, vehicleFirers[0], firers[0].At) : null,
""", """vehicleFirers.Length > 0
                ? VehicleFire(state, vehicleFirers[0], firers[0].At, weapons?.GetValueOrDefault(vehicleFirers[0].Id)) : null,
""")
e.edit(LF, """    private static FireVehicleFire VehicleFire(GameState state, UnitInstance vehicle, BoardLocation at) =>
        ScenarioA1FireEligibility.VehicleFirer(new VehicleFirerFacts(vehicle.Id, vehicle.Definition?.Definition, at.ToString(), CrewExposed(vehicle),
""", """    private static FireVehicleFire VehicleFire(GameState state, UnitInstance vehicle, BoardLocation at, IReadOnlyList<string>? named = null)
    {
        var fire = MainArmamentFire(state, vehicle, at);

        // D3.5, D3.51, D3.7 (pass 50): the BMG and CMG the attack names, against the vehicle's shots of this Player Turn.
        return named is not { Count: > 0 } ? fire : ScenarioA1FireEligibility.WithVehicleWeapons(fire, new VehicleWeaponsFacts(named,
            Is(vehicle, Conditions.BmgMalfunctioned), Is(vehicle, Conditions.CmgMalfunctioned),
            ScenarioA1FireEligibility.FirePhase(state.Phase, vehicle.Side == state.PhasingSide), VehicleShots(state, vehicle.Id),
            vehicle.Definition is { } definition ? CatalogReference.Value.Definitions.GetValueOrDefault(definition.Definition)?.MaType : null));
    }

    /// <summary>A vehicle's weapon shots of this Player Turn, as Rules reads them (D3.5, D3.51; pass 50).</summary>
    internal static IReadOnlyList<VehicleWeaponShotFacts> VehicleShots(GameState state, string vehicleId) =>
        [.. state.VehicleWeaponShots.Where(item => item.Vehicle == vehicleId).Select(item => new VehicleWeaponShotFacts(item.Weapon, item.FirePhase, item.Turn, item.Doubled))];

    private static FireVehicleFire MainArmamentFire(GameState state, UnitInstance vehicle, BoardLocation at) =>
        ScenarioA1FireEligibility.VehicleFirer(new VehicleFirerFacts(vehicle.Id, vehicle.Definition?.Definition, at.ToString(), CrewExposed(vehicle),
""")

# --- LiveFire: the record's check ------------------------------------------------------------------------------------
e.edit(LF, """            var mol = recorded.Firers?.FirstOrDefault(item => item.Mol == true)?.UnitId;
            (expected, reason) = LiveFire.FromState(state, fire.Firers, directors, target, weapons, alone, partners is { Count: > 0 } ? partners : null, mol);
""", """            var mol = recorded.Firers?.FirstOrDefault(item => item.Mol == true)?.UnitId;

            // D3.5 (pass 50): the BMG and CMG a vehicle's record names.
            if (recorded.VehicleFire is { VehicleId: { } armed, Weapons: { Count: > 0 } vehicleWeapons })
            {
                weapons = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) { [armed] = [.. vehicleWeapons.Select(item => item.Weapon ?? string.Empty)] };
            }

            (expected, reason) = LiveFire.FromState(state, fire.Firers, directors, target, weapons, alone, partners is { Count: > 0 } ? partners : null, mol);
""")
e.edit(LF, """            FirerLocationsAdjacent = recorded.FirerLocationsAdjacent,
            WithinSubsequentFirstFireRange = recorded.WithinSubsequentFirstFireRange,
""", """            // D3.52, C5.11 (pass 50): the hexspines a vehicle's MG must turn its CA, and its hex's terrain, are map reads recorded with the attack.
            VehicleFire = expected.VehicleFire is { Weapons: { } stateWeapons } armedVehicle && recorded.VehicleFire is { Weapons: { } recordedWeapons } recordedVehicle
                && stateWeapons.Count == recordedWeapons.Count
                ? armedVehicle with
                {
                    InWoodsOrBuilding = recordedVehicle.InWoodsOrBuilding,
                    Weapons = [.. stateWeapons.Zip(recordedWeapons, (fact, record) => fact with { Turn = record.Turn })],
                }
                : expected.VehicleFire,
            FirerLocationsAdjacent = recorded.FirerLocationsAdjacent,
            WithinSubsequentFirstFireRange = recorded.WithinSubsequentFirstFireRange,
""")
e.edit(LF, """        if (LiveFire.Rolls(fire, recorded, rolls) is not { } dice)
        {
            return "The fire record names a roll of the wrong shape.";
        }
""", """        // D3.52, D3.12 (pass 50): the record turns the turret exactly the hexspines its CMG's facts say, and no other record turns one.
        var cmgTurn = recorded.VehicleFire?.Weapons?.FirstOrDefault(item => item.Weapon == FireVehicleWeapon.CoaxialMg)?.Turn ?? 0;
        var tcaNow = recorded.VehicleFire?.VehicleId is { } turning && state.Unit(turning) is { } turningVehicle ? LiveOrdnance.TurretFacing(state, turningVehicle) : null;
        if ((cmgTurn > 0) != fire.TurretFacing.HasValue || ScenarioA1OrdnanceEligibility.HexspinesTurned((int?)fire.TurretFacing, (int?)tcaNow) != cmgTurn)
        {
            return "The fire record turns the turret exactly the hexspines its CMG's facts say the shot changes its TCA by.";
        }

        if (LiveFire.Rolls(fire, recorded, rolls) is not { } dice)
        {
            return "The fire record names a roll of the wrong shape.";
        }
""")

# --- GamePlanner.Fire: the arc facts, the turret's facing, the condition names --------------------------------------
e.edit(GF, """        attack = WithSeen(state, attack);

        // A22.611 (ruling R15.4): no MOL through a woods or orchard hexside""", """        attack = WithSeen(state, attack);

        // D1.81, D1.82, D3.52, C5.11, C5.13 (pass 50): the hexspines each of a vehicle's MGs must turn its CA to cover the target.
        if (attack.VehicleFire is { Weapons.Count: > 0 })
        {
            var (armed, arcBar) = VehicleArcFacts(state, attack, target);
            if (armed is null)
            {
                return Refused(scope, label, expected, arcBar!);
            }

            attack = armed;
        }

        // A22.611 (ruling R15.4): no MOL through a woods or orchard hexside""")
e.edit(GF, """            {
                MovementStep = step,
            }, package, hidesIdentity ? [targetSide] : null));
""", """            {
                MovementStep = step,
                TurretFacing = VehicleTurretTurn(state, facts),
            }, package, hidesIdentity ? [targetSide] : null));
""")
e.edit(GF, """    private static string ConditionName(UnitCondition condition) => condition switch
    {
""", """    /// <summary>
    /// The map's part of a vehicle's MG attack (D1.81, D1.82, D3.52, C5.11, C5.13; pass 50): the hexspines the VCA would turn for a BMG and the TCA
    /// for a CMG, and whether the vehicle is in woods, a building, or rubble. Rules says which turn is allowed.
    /// </summary>
    private (FireAttack? Attack, string? Reason) VehicleArcFacts(GameState state, FireAttack attack, BoardLocation target)
    {
        var fire = attack.VehicleFire!;
        if (state.Unit(fire.VehicleId!) is not { Position: MapPosition { Facing: { } hull } } vehicle || !BoardLocation.TryParse(fire.LocationId, out var from)
            || Bearing(state, from!, target) is not { } bearing || ReadLocation(state, from!) is not { } read || TerrainKey(read) is not { } terrain)
        {
            return (null, $"play.fire-vehicle-arc: the Covered Arcs of '{fire.VehicleId}' cannot be read toward this target on this map (D3.1)");
        }

        var turret = LiveOrdnance.TurretFacing(state, vehicle) ?? hull;
        var weapons = new List<FireVehicleWeapon>();
        foreach (var weapon in fire.Weapons!)
        {
            var turn = ScenarioA1OrdnanceMapRules.CoveredArcTurn((int)(weapon.Weapon == FireVehicleWeapon.CoaxialMg ? turret : hull), bearing).Steps;
            if (ScenarioA1FireEligibility.VehicleWeaponArcBar(vehicle.Id, weapon.Weapon ?? string.Empty, turn, attack.FireKind == ScenarioA1FireCalculator.BoundingFirstFire) is { } bar)
            {
                return (null, bar);
            }

            weapons.Add(weapon with { Turn = turn });
        }

        return (attack with
        {
            VehicleFire = fire with
            {
                Weapons = weapons,
                InWoodsOrBuilding = WoodsOrBuilding.Contains(terrain) ? true : null,
            },
        }, null);
    }

    /// <summary>The TCA a vehicle's CMG turns its turret to for its attack (D3.52, D3.12; pass 50); null when it fires within the TCA.</summary>
    private UnitFacing? VehicleTurretTurn(GameState state, FireAttack facts)
    {
        if (facts.VehicleFire is not { Weapons: { } weapons } fire || !weapons.Any(item => item.Weapon == FireVehicleWeapon.CoaxialMg && item.Turn > 0)
            || state.Unit(fire.VehicleId!) is not { Position: MapPosition { Facing: { } hull } } vehicle || !BoardLocation.TryParse(fire.LocationId, out var from)
            || !BoardLocation.TryParse(facts.TargetLocationId, out var target) || Bearing(state, from!, target!) is not { } bearing)
        {
            return null;
        }

        return (UnitFacing)ScenarioA1OrdnanceMapRules.CoveredArcTurn((int)(LiveOrdnance.TurretFacing(state, vehicle) ?? hull), bearing).Facing;
    }

    private static string ConditionName(UnitCondition condition) => condition switch
    {
""")
e.edit(GF, """        UnitCondition.CcReaction => Conditions.CcReaction,
        _ => Conditions.CmgMalfunctioned,
    };
""", """        UnitCondition.CcReaction => Conditions.CcReaction,
        UnitCondition.Recalled => Conditions.Recalled,
        UnitCondition.Stunned => Conditions.Stunned,
        UnitCondition.StunRecovery => Conditions.StunRecovery,
        UnitCondition.BmgDisabled => Conditions.BmgDisabled,
        UnitCondition.CmgDisabled => Conditions.CmgDisabled,
        _ => Conditions.CmgMalfunctioned,
    };
""")

# --- GamePlanner.Rally: the repair of a vehicle's weapon by name ----------------------------------------------------
e.edit(GR, """        if (unitId == equipmentId && state.Unit(unitId) is { } named && LiveFire.IsVehicle(named))
        {
            return PlanVehicleRepair(scope, attemptId, expected, label, actor, state, named);
        }
""", """        if (unitId == equipmentId && state.Unit(unitId) is { } named && LiveFire.IsVehicle(named))
        {
            // D3.7 (pass 50): a Gun MA, a BMG, or a CMG is repaired by its name; with none, the vehicle's MA AAMG as before.
            return Text(arguments, "weapon", out var weaponName) || LiveOrdnance.IsTank(named)
                ? PlanVehicleWeaponRepair(scope, attemptId, expected, label, actor, state, named, weaponName ?? "ma")
                : PlanVehicleRepair(scope, attemptId, expected, label, actor, state, named);
        }
""")
e.edit(GR, """    /// <summary>A Repair outcome of Rules as the record names it.</summary>
""", """    /// <summary>
    /// D3.7 (p. 201; pass 50): in the RPh a crew that is not shocked or stunned tries once, by a dr of its own, to repair its vehicle's Gun MA, BMG,
    /// or CMG: a 1 repairs it, a 6 disables it, and a vehicle with no Passenger or Towing capability whose MA is disabled is Recalled (D5.341).
    /// </summary>
    private GamePlan PlanVehicleWeaponRepair(GameScope scope, string attemptId, long expected, string label, string actor, GameState state, UnitInstance vehicle, string weapon)
    {
        if (ScenarioA1RallyRules.VehicleRepairPhaseBar(state.Phase, vehicle.Status == InstanceStatus.Active) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        var definition = vehicle.Definition is { } named ? FireReference.Value.Definitions.GetValueOrDefault(named.Definition) : null;
        if (!ScenarioA1RallyRules.IsVehicleWeapon(weapon) || definition is null
            || (weapon == "ma" ? !LiveOrdnance.IsTank(vehicle) : ScenarioA1FireCalculator.VehicleWeaponFp(definition, weapon) is null))
        {
            return Refused(scope, label, expected, $"play.repair-weapon: {vehicle.Id} has no such weapon to repair (D3.7)");
        }

        if (ScenarioA1RallyRules.VehicleWeaponRepairBar(vehicle.Id, weapon, Is(vehicle, ConditionName(ScenarioA1RallyRules.VehicleWeaponMalfunction(weapon))),
            Is(vehicle, ConditionName(ScenarioA1RallyRules.VehicleWeaponDisabled(weapon))),
            ScenarioA1RallyRules.VehicleCrewImpaired(Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Shocked), Is(vehicle, Conditions.UnconfirmedKill),
                Is(vehicle, Conditions.Recalled), Is(vehicle, Conditions.StunRecovery)),
            state.RepairsThisPhase.Contains(vehicle.Id + ":" + weapon)) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        var carries = VehicleDefinition(vehicle) is { } printed && (printed.PassengerCapacity is > 0 || printed.Towing is > 0);
        var package = ScenarioA1FirePackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            var outcome = ScenarioA1RallyRules.VehicleMgRepairResult(drawn.Values[0]);
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "repair", 1, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null),
                Event(scope, attemptId, 2, expected, "repair-attempted", new RepairAttempted(vehicle.Id, vehicle.Id, rollId, 1, RepairResult(outcome)) { Weapon = weapon },
                    package, null),
            };
            var changes = ScenarioA1RallyRules.VehicleWeaponRepairChanges(weapon, outcome, carries);
            if (changes.Count > 0)
            {
                events.Add(Event(scope, attemptId, 3, expected, "conditions-changed", new ConditionsChanged(vehicle.Id,
                    changes.ToDictionary(item => ConditionName(item.Condition), item => item.Value ? ConditionState.True : ConditionState.False, StringComparer.Ordinal)),
                    package, null, [EventId(attemptId, 2)]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1RallyRules.VehicleWeaponRepairSummary(vehicle.Id, weapon, weapon == "ma" && !carries)])
        {
            Roll = new PlannedRoll("repair", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>A Repair outcome of Rules as the record names it.</summary>
""")

e.apply()
