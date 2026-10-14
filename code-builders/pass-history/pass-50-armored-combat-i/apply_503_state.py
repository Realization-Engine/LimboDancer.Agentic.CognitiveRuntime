"""Pass 50, task 50.3: the state and Play: Delay, the TCA changed with an MP expenditure, and the MA's Bounding First Fire."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
UNITS = "LimboDancer.Domains.Asl.Units/State/"
RULES = "LimboDancer.Domains.Asl.Rules/"
PLAY = "LimboDancer.Domains.Asl.Play/"
P = UNITS + "GameProjector.cs"

# --- Units: the records -----------------------------------------------------------------------------------------------
e.edit(UNITS + "OrdnanceRecords.cs", """public sealed record VehicleWeaponShot(string Vehicle, string Weapon, string FirePhase, int Turn, bool Doubled);
""", """public sealed record VehicleWeaponShot(string Vehicle, string Weapon, string FirePhase, int Turn, bool Doubled)
{
    /// <summary>The movement step a shot of the phasing side's MPh followed (C5.3; pass 50): its MA fires again only after another MP expenditure. Null outside it.</summary>
    public int? Step
    {
        get; init;
    }
}
""")
e.edit(UNITS + "LiveRecords.cs", """    /// <summary>Passengers disembark beneath the Stopped vehicle (D6.5; ruling R26.2): <see cref="Units"/> name them.</summary>
    public const string Unload = "unload";
""", """    /// <summary>Passengers disembark beneath the Stopped vehicle (D6.5; ruling R26.2): <see cref="Units"/> name them.</summary>
    public const string Unload = "unload";

    /// <summary>A Delay MP (D2.17, p. 196; pass 50): one MP spent without moving, by a vehicle that is stopped or has not yet started.</summary>
    public const string Delay = "delay";

    /// <summary>
    /// The TCA the turret is turned to with this MP expenditure (D3.12, p. 199; pass 50): "the TCA may also change freely with each MP expended
    /// during the MPh"; null when it does not change.
    /// </summary>
    public UnitFacing? Turret
    {
        get; init;
    }
""")
e.edit(UNITS + "GameEventWriter.cs", """                writer.WriteNumber("halfMp", vehicle.HalfMp);
                writer.WriteNumber("step", vehicle.Step);
                if (vehicle.Reverse)
""", """                writer.WriteNumber("halfMp", vehicle.HalfMp);
                writer.WriteNumber("step", vehicle.Step);
                if (vehicle.Turret is { } vehicleTurret)
                {
                    writer.WriteString("turret", Documents.UnitFacings.Name(vehicleTurret));
                }

                if (vehicle.Reverse)
""")
e.edit(UNITS + "GameEventReader.cs", """                        UnitMf = fields.OptionalInteger(payload, "unitMf", path) ?? 0,
                    };
""", """                        UnitMf = fields.OptionalInteger(payload, "unitMf", path) ?? 0,
                        Turret = fields.OptionalString(payload, "turret", path) is { } turretName && UnitFacings.TryParse(turretName, out var stepTurret) ? stepTurret : null,
                    };
""")

# --- Units: the projector --------------------------------------------------------------------------------------------
e.edit(P, """                VehicleStepped.Stop => moving && step.At == at.Location && step.Facing is null && step.HalfMp == 2,
""", """                VehicleStepped.Stop => moving && step.At == at.Location && step.Facing is null && step.HalfMp == 2,

                // D2.17 (pass 50): a Delay MP, one MP in place by a vehicle that is stopped or has not started.
                VehicleStepped.Delay => !moving && !bogged && step.At == at.Location && step.Facing is null && step.HalfMp == 2,
""")
e.edit(P, """            if (!valid)
            {
                return Fail<GameState>("UNIT-STATE-034",
                    "A vehicle starts when not moving; turns one hexspine, enters a Location, stops, or exits only while moving; turns, starts, and stops for one MP; and spends its MP left in its hex only to end its move (D2.1, D2.11 to D2.13).");
            }
""", """            if (!valid)
            {
                return Fail<GameState>("UNIT-STATE-034",
                    "A vehicle starts when not moving; turns one hexspine, enters a Location, stops, or exits only while moving; turns, starts, and stops for one MP; and spends its MP left in its hex only to end its move (D2.1, D2.11 to D2.13).");
            }

            // D3.12 (pass 50): the TCA changes with a Delay, a stop, a VCA change, or an entry, not with any other expenditure.
            if (step.Turret is not null && !Rules.ScenarioA1VehicleMovementCalculator.TurretTurnsWith(step.Kind))
            {
                return Fail<GameState>("UNIT-STATE-034", "A vehicle's turret turns with a Delay MP, a stop, a VCA change, or an entry (D3.12).");
            }
""")
e.edit(P, """                    : item)],
            };
            return next with
            {
                Movement = new MovementState([vehicle.Id], step.At, step.Kind == VehicleStepped.Enter ? step.HalfMp : (current?.HalfMfInLocation ?? 0) + step.HalfMp,
                    step.Step, false, WindowOpen: true)
                {
                    Vehicle = true,
                    Started = step.Kind == VehicleStepped.Remain ? current!.Started : step.Kind != VehicleStepped.Stop,
                    Stopped = step.Kind == VehicleStepped.Remain ? current!.Stopped : step.Kind == VehicleStepped.Stop,
""", """                    : item)],
            };
            if (step.Turret is { } turretTurned)
            {
                next = next with
                {
                    TurretFacings = [.. next.TurretFacings.Where(item => item.Vehicle != vehicle.Id), new TurretFacing(vehicle.Id, turretTurned)],
                };
            }

            // D2.17 (pass 50): a Delay MP leaves the vehicle as it was, stopped or not yet started.
            var keeps = step.Kind is VehicleStepped.Remain or VehicleStepped.Delay;
            return next with
            {
                Movement = new MovementState([vehicle.Id], step.At, step.Kind == VehicleStepped.Enter ? step.HalfMp : (current?.HalfMfInLocation ?? 0) + step.HalfMp,
                    step.Step, false, WindowOpen: true)
                {
                    Vehicle = true,
                    Started = keeps ? current?.Started ?? false : step.Kind != VehicleStepped.Stop,
                    Stopped = keeps ? current?.Stopped ?? false : step.Kind == VehicleStepped.Stop,
""")
e.edit(P, """                    ? [.. state.VehicleWeaponShots, new VehicleWeaponShot(fired.Gun, "ma", Rules.ScenarioA1FireEligibility.FirePhase(state.Phase, shooter.Side == state.PhasingSide),
                        fired.Facts.TryGetProperty("hexspinesToTurn", out var maTurn) && maTurn.ValueKind == JsonValueKind.Number ? maTurn.GetInt32() : 0,
                        fired.Facts.TryGetProperty("firerInWoodsOrBuilding", out var maWoods) && maWoods.ValueKind == JsonValueKind.True)]
""", """                    ? [.. state.VehicleWeaponShots, new VehicleWeaponShot(fired.Gun, "ma", Rules.ScenarioA1FireEligibility.FirePhase(state.Phase, shooter.Side == state.PhasingSide),
                        fired.Facts.TryGetProperty("hexspinesToTurn", out var maTurn) && maTurn.ValueKind == JsonValueKind.Number ? maTurn.GetInt32() : 0,
                        fired.Facts.TryGetProperty("firerInWoodsOrBuilding", out var maWoods) && maWoods.ValueKind == JsonValueKind.True)
                    {
                        // C5.3 (pass 50): the movement step a Bounding First Fire shot followed; none yet at the outset of the move.
                        Step = state.Phase == "mph" && shooter.Side == state.PhasingSide ? state.Movement?.Step ?? 0 : null,
                    }]
""")

# --- Rules: the movement calculator ----------------------------------------------------------------------------------
M = RULES + "ScenarioA1VehicleMovementCalculator.cs"
e.edit(M, """    /// <summary>D2.12, D2.7: why a vehicle may not spend a moving MP, or null: it must be moving, and after an ALL entry it may only Stop.</summary>
""", """    /// <summary>D3.12 (p. 199; pass 50): the expenditures a TCA change may coincide with: "movement, stop, start, or Delay"; with a Start it is not built.</summary>
    public static bool TurretTurnsWith(string? kind) => kind is "delay" or "stop" or "turn" or "enter";

    /// <summary>
    /// D2.17 (p. 196; pass 50): why a vehicle may not spend a Delay MP, or null: "the expenditure of MP without moving ... can be used only while
    /// the vehicle is stopped", which D3.3 has "including at the outset of its MPh prior to movement"; and it costs one MP of those left.
    /// </summary>
    public static string? DelayBar(string id, bool moving, int spent, int allotment) =>
        moving ? $"play.move-vehicle-delay: {id} spends a Delay MP only while it is stopped, or before it starts (D2.17, D3.3)"
        : spent + 2 > allotment ? $"play.move-vehicle-mp: {id} has {Mp(Math.Max(0, allotment - spent))} of its {Mp(allotment)} MP left, and a Delay MP costs 1 (D2.1, D2.17)"
        : null;

    /// <summary>
    /// D3.12 (p. 199; pass 50): why the turret may not turn with this MP expenditure, or null: the vehicle has a turret; the expenditure is a Delay,
    /// a stop, a VCA change, or an entry; and it is not in a woods, building, or rubble obstacle, where "the MP expenditure required for a TCA
    /// change ... is doubled", which is not built.
    /// </summary>
    public static string? TurretStepBar(string id, string kind, bool turreted, bool facingRead, bool inObstacle) =>
        !turreted ? $"play.move-vehicle-turret: {id} has no turret to turn (D3.12)"
        : !facingRead ? "play.move-vehicle-turret: the TCA is named by a hexspine (D3.12)"
        : !TurretTurnsWith(kind) ? $"play.move-vehicle-turret: {id}'s turret turns with a Delay MP, a stop, a VCA change, or an entry (D3.12)"
        : inObstacle ? $"play.move-vehicle-turret: {id} is in woods, a building, or rubble, where a TCA change doubles the MP expenditure; that is not built (D3.12, C5.11)"
        : null;

    /// <summary>D2.12, D2.7: why a vehicle may not spend a moving MP, or null: it must be moving, and after an ALL entry it may only Stop.</summary>
""")

# --- Play: the planner's Delay step and the turret turned with an MP ------------------------------------------------
V = PLAY + "GamePlanner.Vehicles.cs"
e.edit(V, """            case VehicleStepped.Unload:
                return PlanUnload(scope, arguments, existing, attemptId, expected, label, actor, state, vehicle, at.Location, step);
        }
""", """            case VehicleStepped.Unload:
                return PlanUnload(scope, arguments, existing, attemptId, expected, label, actor, state, vehicle, at.Location, step);
            case VehicleStepped.Delay:
                // D2.17, D3.3 (pass 50): a Delay MP, one at a time, while stopped or before starting.
                if (ScenarioA1VehicleMovementCalculator.DelayBar(id, moving, spent, allotment) is { } delayBar)
                {
                    return Refused(scope, label, expected, delayBar);
                }

                var (delayTurret, delayTurretBar) = TurretWithStep(state, vehicle, arguments, kind, at.Location, vehicle.Straddling is not null);
                if (delayTurretBar is not null)
                {
                    return Refused(scope, label, expected, delayTurretBar);
                }

                return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
                    [Event(scope, attemptId, 1, expected, "vehicle-step", new VehicleStepped(id, kind, at.Location, null, 2, step) { Turret = delayTurret }, null, null)],
                    [$"play.move-vehicle: {id} spends a Delay MP in {at.Location} (D2.17)" + (delayTurret is { } delayed ? $", turning its turret to {UnitFacings.Name(delayed)} (D3.12)" : string.Empty)
                        + $"; {Mp(Math.Max(0, allotment - spent - 2))} MP left"]);
        }
""")
e.edit(V, """        // D5.341 (ruling R5.17): a leaving AFV keeps to a shortest route in MP to its Friendly Board Edge.
        if (leaving && !ScenarioA1RecallCalculator.StopsToUnload(kind, state.Passengers(id).Count > 0))
""", """        // D3.12 (pass 50): the TCA may change with this MP expenditure.
        var (turret, turretBar) = TurretWithStep(state, vehicle, arguments, kind, to, entry?.Bypass ?? vehicle.Straddling is not null, entry?.Road == true);
        if (turretBar is not null)
        {
            return Refused(scope, label, expected, turretBar);
        }

        if (turret is { } turretTo)
        {
            summary += $", turning its turret to {UnitFacings.Name(turretTo)} (D3.12)";
        }

        // D5.341 (ruling R5.17): a leaving AFV keeps to a shortest route in MP to its Friendly Board Edge.
        if (leaving && !ScenarioA1RecallCalculator.StopsToUnload(kind, state.Passengers(id).Count > 0))
""")
e.edit(V, """                MinimumMove = minimumMove,
                All = entry?.All == true,
            };
""", """                MinimumMove = minimumMove,
                All = entry?.All == true,
                Turret = stepKind == kind ? turret : null,
            };
""")
e.edit(V, """    private GamePlan PlanMoveVehicle(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
""", """    /// <summary>
    /// The TCA a vehicle's step turns its turret to (D3.12, p. 199; pass 50), from the step's <c>turret</c> argument: null with no refusal when
    /// the step names none. Rules decides from the facts: a turret, the kind of expenditure, and a woods, building, or rubble obstacle the
    /// vehicle is in (not in Bypass, nor on a road).
    /// </summary>
    private (UnitFacing? Turret, string? Bar) TurretWithStep(GameState state, UnitInstance vehicle, JsonElement arguments, string kind, BoardLocation at, bool bypass, bool road = false)
    {
        if (!Text(arguments, "turret", out var named))
        {
            return (null, null);
        }

        var read = UnitFacings.TryParse(named, out var parsed);
        var terrain = ReadLocation(state, at) is { } here ? TerrainKey(here) : null;
        var bar = ScenarioA1VehicleMovementCalculator.TurretStepBar(vehicle.Id, kind, LiveFire.MaTypeOf(vehicle.Definition?.Definition) is "t" or "st" or "rst" or "1mt", read,
            terrain is not null && (WoodsOrBuilding.Contains(terrain) || IsRubbleTerrain(terrain)) && !bypass && !road);
        return bar is null ? (parsed, null) : (null, bar);
    }

    private GamePlan PlanMoveVehicle(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
""")

# --- Play: LiveFire's shots, LiveOrdnance's Bounding First Fire ------------------------------------------------------
e.edit(PLAY + "LiveFire.cs", """.Select(item => new VehicleWeaponShotFacts(item.Weapon, item.FirePhase, item.Turn, item.Doubled))];
""", """.Select(item => new VehicleWeaponShotFacts(item.Weapon, item.FirePhase, item.Turn, item.Doubled, item.Step))];
""")
L = PLAY + "LiveOrdnance.cs"
e.edit(L, """        IReadOnlyList<string>? movers = null;
        if (phase == "MPh")
        {
            if (ScenarioA1OrdnanceEligibility.OutsideMphWindow(side == state.PhasingSide, state.Movement is { WindowOpen: true }, () => state.Movement!.Location == target))
            {
                return (null, ScenarioA1OrdnanceEligibility.MphWindowRefusal);
            }

            movers = state.Movement!.Movers;
        }
""", """        IReadOnlyList<string>? movers = null;
        var bounding = false;
        if (phase == "MPh")
        {
            // D3.3, C5.3 (pass 50): the phasing side's vehicle fires its MA as Bounding First Fire; Rules says which kind the shot is.
            var (mphKind, mphRefusal) = ScenarioA1OrdnanceEligibility.MphShotKind(side == state.PhasingSide, gun is UnitInstance, () => LiveFire.MayBoundingFire(state, gun.Id),
                state.Movement is { WindowOpen: true }, () => state.Movement!.Location == target);
            if (mphRefusal is not null)
            {
                return (null, mphRefusal);
            }

            bounding = mphKind == ScenarioA1FireCalculator.BoundingFirstFire;
            if (bounding)
            {
                // C5.3: another MP between its shots.
                if (ScenarioA1OrdnanceEligibility.BoundingShotBar(gun.Id,
                    state.VehicleWeaponShots.LastOrDefault(item => item.Vehicle == gun.Id && item.Weapon == "ma" && item.FirePhase == "mph")?.Step, state.Movement?.Step ?? 0) is { } mpBar)
                {
                    return (null, mpBar);
                }
            }
            else
            {
                movers = state.Movement!.Movers;
            }
        }
""")
e.edit(L, """        if (phase == "MPh")
        {
            // C6.13, C6.16, C6.17: non-Assault Movement and the MF or MP spent in the Location (FRD); the Gun's shots there so far; the MP in the
""", """        if (bounding)
        {
            shot = shot with
            {
                FireKind = ScenarioA1FireCalculator.BoundingFirstFire,
            };
        }
        else if (phase == "MPh")
        {
            // C6.13, C6.16, C6.17: non-Assault Movement and the MF or MP spent in the Location (FRD); the Gun's shots there so far; the MP in the
""")
e.edit(L, """                    state.MovedVehicles.Contains(firer.Id, StringComparer.Ordinal)) with
                {
                    // D3.5, D3.51 (pass 50): the MA beside the vehicle's other weapons.
                    Weapons = ScenarioA1OrdnanceEligibility.VehicleWeapons(LiveFire.VehicleShots(state, firer.Id),
                        ScenarioA1FireEligibility.FirePhase(state.Phase, firer.Side == state.PhasingSide),
                        LiveFire.MaTypeOf(definition)),
                },
""", """                    state.MovedVehicles.Contains(firer.Id, StringComparer.Ordinal),
                    // D2.42, D3.32 (pass 50): the moving vehicle that has not stopped is a Motion firer; one that fired its MA in this MPh is a Bounding Firer.
                    state.Movement is { Vehicle: true, Started: true, Stopped: false } moving && moving.Movers.Contains(firer.Id),
                    state.VehicleWeaponShots.Any(item => item.Vehicle == firer.Id && item.Weapon == "ma" && item.FirePhase == "mph")) with
                {
                    // D3.5, D3.51 (pass 50): the MA beside the vehicle's other weapons.
                    Weapons = ScenarioA1OrdnanceEligibility.VehicleWeapons(LiveFire.VehicleShots(state, firer.Id),
                        ScenarioA1FireEligibility.FirePhase(state.Phase, firer.Side == state.PhasingSide),
                        LiveFire.MaTypeOf(definition), Is(firer, Conditions.BoundingFire)),
                },
""")
e.edit(L, """        // Pass 50 (ruling R45.4's rule for a new fact): a record made before the pass says nothing of the vehicle's other weapons, and is compared without.
        if (merged.Vehicle is { Weapons: not null } withWeapons && recorded.Vehicle is { Weapons: null })
        {
            merged = merged with
            {
                Vehicle = withWeapons with
                {
                    Weapons = null
                }
            };
        }
""", """        // Pass 50 (ruling R45.4's rule for a new fact): a record made before the pass says nothing of the vehicle's other weapons, and is compared without.
        if (merged.Vehicle is { Weapons: not null } withWeapons && recorded.Vehicle is { Weapons: null })
        {
            merged = merged with
            {
                Vehicle = withWeapons with
                {
                    Weapons = null,
                    BoundingFirer = null,
                }
            };
        }

        // C5.31 to C5.33 (pass 50): the MP in a continuous LOS are the planner's read of the map, recorded with a Bounding First Fire shot.
        if (merged.Vehicle is { } firingVehicle && recorded.Vehicle is { MpInLos: { } mpInLos })
        {
            merged = merged with
            {
                Vehicle = firingVehicle with { MpInLos = mpInLos }
            };
        }
""")

# --- Play: the planner's map facts -----------------------------------------------------------------------------------
G = PLAY + "GamePlanner.Ordnance.cs"
e.edit(G, """        if (ScenarioA1OrdnanceMapRules.VcaRefusal(tank?.Id, turreted, turns.Steps) is { } vca)
        {
            return (null, vca);
        }
""", """        if (ScenarioA1OrdnanceMapRules.VcaRefusal(tank?.Id, turreted, turns.Steps) is { } vca)
        {
            return (null, vca);
        }

        // C5.13 (pass 50): a Bounding First Firer fires within its current CA; its turret turns with an MP expenditure (D3.12).
        if (ScenarioA1OrdnanceMapRules.BoundingArcRefusal(tank?.Id, shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire, turns.Steps) is { } boundingArc)
        {
            return (null, boundingArc);
        }
""")
e.edit(G, """        // C6.43 (ruling R8.8): the Bore Sighting counts while its original crew fires the Gun from its setup Location.
        var boreSighted = LiveOrdnance.BoreSighted(state, shot.Gun.GunId!, shot.Crew!.UnitId!, from, target);
""", """        // C5.31 to C5.33 (pass 50): the MP a Bounding First Firer has spent in a continuous LOS to its target; a LOS is the same both ways.
        if (shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire && shot.Vehicle is { } boundingFirer)
        {
            shot = shot with
            {
                Vehicle = boundingFirer with { MpInLos = MpInLos(state, target, shot.Gun.GunId!) },
            };
        }

        // C6.43 (ruling R8.8): the Bore Sighting counts while its original crew fires the Gun from its setup Location.
        var boreSighted = LiveOrdnance.BoreSighted(state, shot.Gun.GunId!, shot.Crew!.UnitId!, from, target);
""")
e.edit(RULES + "ScenarioA1OrdnanceMapRules.cs", """    /// <summary>
    /// C6.11, C6.12, C6.15 (search): the MP spent in the firer's continuous LOS""", """    /// <summary>C5.13 (p. 172; pass 50): "a Bounding First Firer must always fire within its current CA; it may change that CA only via MP expenditure".</summary>
    public static string? BoundingArcRefusal(string? vehicleId, bool boundingFirstFire, int steps) =>
        boundingFirstFire && steps > 0
            ? $"play.ordnance-bounding-arc: {vehicleId} fires its MA within its current TCA in Bounding First Fire; turn the turret with an MP expenditure first (C5.13, D3.12)"
            : null;

    /// <summary>
    /// C6.11, C6.12, C6.15 (search): the MP spent in the firer's continuous LOS""")

e.apply()
