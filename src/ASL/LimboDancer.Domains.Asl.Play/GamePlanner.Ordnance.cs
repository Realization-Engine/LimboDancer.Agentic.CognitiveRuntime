using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// A Gun's HE shot at Infantry (unit step 24): the state's facts, the planner's map reads (range, LOS, terrain, and the hexspines
/// the Gun must turn to bring the target into its Covered Arc, C3.2), the Ordnance package's pre-check, and the rolls it asks for.
/// Backlog pass 7: a tank's MA shot, turning its turret (D3.12), and a shot at one named vehicle on the Vehicle Target Type with its
/// Target Facings (D3.2), its To Kill DR, and the vehicle's fate (C7, D5.5, D5.6; rulings R7.2 to R7.10).
/// </summary>
public sealed partial class GamePlanner
{
    private static readonly Lazy<ScenarioA1OrdnanceReference> OrdnanceReference = new(() => new ScenarioA1OrdnancePackage().Reference);

    private static readonly IReadOnlyList<string> WoodsOrBuilding = ScenarioA1Definitions.WoodsOrBuilding;

    private GamePlan PlanFireOrdnance(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "gunId", out var gunId) || !Text(arguments, "target", out var targetText) || !BoardLocation.TryParse(targetText, out var target))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Gun's shot names the Gun and a target Location");
        }

        string? targetVehicle = Text(arguments, "targetVehicle", out var named) ? named : null;
        string? ammunition = Text(arguments, "ammunition", out var declared) ? declared : null;
        var intensive = arguments.TryGetProperty("intensive", out var intensiveArgument) && intensiveArgument.ValueKind == JsonValueKind.True;
        string? spotter = Text(arguments, "spotter", out var spotterId) ? spotterId : null;
        string? director = Text(arguments, "director", out var directorId) ? directorId : null;
        var (shot, reason) = LiveOrdnance.FromState(state, gunId, target, targetVehicle, ammunition, intensive, spotter, director);
        if (shot is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        // A7.25, A15.432 (table player, pass 12): an Opportunity Firer or a berserk unit fires no ordnance or LATW in its PFPh.
        var shooter = gunId.EndsWith(":pf", StringComparison.Ordinal) ? state.Unit(gunId[..^":pf".Length])
            : state.Find(gunId) is EquipmentInstance { Holding: { } holding } ? state.Unit(holding.Holder) : null;
        if (state.Phase == "pfph" && shooter is not null && !LiveFire.IsVehicle(shooter) && (Is(shooter, Conditions.BoundingFire) || Is(shooter, Conditions.Berserk)))
        {
            return Refused(scope, label, expected, $"play.fire-barred: {shooter.Id} {GamePlanner.FireBar(state, shooter)}");
        }

        // C3.33, C3.332 (ruling R9.3): a mortar's Area Target Type shot at a hex holding a vehicle, or units in another Location of the hex, is not built.
        if (shot.TargetType == OrdnanceTargetTypes.Area)
        {
            if (state.Units.FirstOrDefault(unit => unit.Status is InstanceStatus.Active && LiveFire.IsVehicle(unit) && state.Location(unit.Id)?.Location is { } at
                && at.Board == target.Board && at.Hex == target.Hex) is { } inHex)
            {
                return Refused(scope, label, expected, $"play.ordnance-area-vehicle: {inHex.Id} is in the target hex; a mortar's hit on a vehicle is not built (C3.332, C1.55; ruling R9.3)");
            }

            if (state.Units.Any(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id)?.Location is { } other && other.Board == target.Board
                && other.Hex == target.Hex && other != target))
            {
                return Refused(scope, label, expected, "play.ordnance-area-levels: the target hex holds units in more than one Location, which the Area Target Type is not built for (C3.33; ruling R9.3)");
            }
        }

        // Ruling R25.10: the hit's IFT attack on a vehicle in the target Location is not reviewed; C3.31 (ruling R7.2): name the vehicle to
        // fire at it on the Vehicle Target Type.
        if (targetVehicle is null && shot.TargetType is null
            && state.At(target).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)
                && (state.Phase != "mph" || state.Movement?.Movers.Contains(unit.Id) == true)) is { } vehicle)
        {
            return Refused(scope, label, expected, $"play.ordnance-vehicle: {vehicle.Id} is in {target}; fire at it on the Vehicle Target Type by naming it, "
                + "since a hit's attack on the Location's Infantry and vehicles together is not reviewed (C3.31; rulings R25.10, R7.2)");
        }

        if (state.At(target).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && (Is(unit, Conditions.Melee) || Is(unit, Conditions.Captured))))
        {
            return Refused(scope, label, expected, "play.fire-melee: fire at a Location holding units in Melee or prisoners is not reviewed (A11.15, A20.54)");
        }

        var (map, mapReason) = OrdnanceMapFacts(state, shot, target);
        if (map is null)
        {
            return Refused(scope, label, expected, mapReason!);
        }

        // Backlog pass 16 (rulings R16.2, R16.3, R16.11 to R16.14): ordnance at a Gunflash beyond NVR is not built; the Low Visibility DRM is a Hindrance
        // of its own on the TH DR (Case R, C6.9), never cancelling FFMO or the Open Ground cases; Mud and Deep Snow cushion HE at Infantry in Open Ground;
        // Extreme Winter lowers the Gun's B#.
        if (map.Value.Shot.Hit is { BeyondNvr: true })
        {
            return Refused(scope, label, expected, "play.night-ordnance: ordnance fire at a Gunflash beyond the firer's NVR is not built (E1.81; ruling R16.2)");
        }

        if (map.Value.Shot.Hit is { } mapped)
        {
            var cushioned = (state.Weather("mud") || state.Weather("deep-snow")) && map.Value.Shot.VehicleTarget is null ? true : (bool?)null;
            map = (map.Value.Shot with
            {
                Hit = mapped with
                {
                    LowVisibilityDrm = null,
                    CushionedOpenGround = cushioned,
                },
                CushionedOpenGround = cushioned,
                LowVisibilityDrm = mapped.LowVisibilityDrm,
                BreakdownReduction = ExtremeWinterReduction(state, state.Unit(shot.Crew?.UnitId ?? string.Empty)?.Side),
            }, map.Value.Facing);
        }

        shot = PassEightFacts(state, map.Value.Shot, target);
        var reference = OrdnanceReference.Value;
        var precheck = ScenarioA1OrdnanceCalculator.Precheck(shot, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.ordnance-refused", "Ordnance", "shot", precheck));
        }

        var facts = shot;
        var facing = map.Value.Facing;

        // The To Hit number and DRM before any roll, read from the package with dice that decide nothing else (a miss).
        var preview = ScenarioA1OrdnanceCalculator.Resolve(facts with
        {
            Rolls = new OrdnanceRolls([6, 6], null, null, null, null) { PanzerfaustCheck = 1 }
        }, reference).ToHit;
        var toHit = preview is null ? string.Empty
            : $"; Modified TH# {preview.ModifiedToHit} ({preview.Color}), DRM {(preview.Drm.Count == 0 ? "none" : string.Join(", ", preview.Drm.Select(item => $"{item.Name} {item.Value:+0;-0}")))}";
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            AddOrdnanceEvents(scope, attemptId, expected, actor, state, facts, facing, events, draw);
            return events;
        }

        var aim = facts.Gun!.DefinitionId == OrdnanceTargetTypes.Panzerfaust && facts.VehicleTarget is { } faust
            ? $"a PF (after a PF Check) at {faust.VehicleId} in {facts.TargetLocationId} (hull {faust.HullFacing} facing the firer)"
            : facts.TargetType == OrdnanceTargetTypes.Area
            ? $"HE at {facts.TargetLocationId} (Area Target Type" + (facts.Spotter is { } spotted ? $", spotted by {spotted.UnitId}" : string.Empty) + ")"
            : facts.VehicleTarget is { } aimed
            ? $"{(facts.Ammunition ?? "ap").ToUpperInvariant()} at {aimed.VehicleId} in {facts.TargetLocationId} (Vehicle Target Type; hull {aimed.HullFacing}"
                + (aimed.TurretFacing is { } turretFacing ? $", turret {turretFacing}" : string.Empty) + " facing the firer)"
            : $"HE at {facts.TargetLocationId}";
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.ordnance: {facts.Gun!.GunId} fires {aim} in the {facts.Phase}; range {facts.Range}, {facts.Hit!.TargetTerrain}"
                + (facts.HexspinesToTurn > 0 ? $", turning {(facts.Vehicle is null ? string.Empty : "its turret ")}{facts.HexspinesToTurn} hexspine(s) to {facing!.Value.Name()}" : string.Empty)
                + toHit])
        {
            Roll = new PlannedRoll("ordnance", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// The map reads of a shot: range, levels, LOS and Hindrance, and the target terrain as for Infantry fire; the Gun's terrain (C5.11);
    /// and the Covered Arc (C3.2): the hexspines the Gun turns, fewest first, to have the target within 30 degrees of its barrel, the
    /// boundary rows included. The Covered Arc is read in the map's frame across boards and on a reversed board (rulings R8.7, R26.6).
    /// </summary>
    private ((OrdnanceShot Shot, UnitFacing? Facing)? Facts, string? Reason) OrdnanceMapFacts(GameState state, OrdnanceShot shot, BoardLocation target)
    {
        var from = FirerLocation(state, shot.Gun!.GunId!);
        if (shot.TargetType == OrdnanceTargetTypes.Area || shot.Gun.DefinitionId == OrdnanceTargetTypes.Panzerfaust
            || OrdnanceReference.Value.Guns.GetValueOrDefault(shot.Gun.DefinitionId ?? string.Empty)?.GunType == "latw")
        {
            return SupportWeaponMapFacts(state, shot, from, target);
        }

        // C3.2, D3.12: a Gun's barrel, or a turreted AFV's TCA (ruling R7.10); a non-turreted MA would pivot the vehicle, which is not built.
        var firer = state.Find(shot.Gun!.GunId!)!;

        // C5.5 (ruling R8.8): a shot at enemy Infantry in the Gun's own Location, at range 0, without a CA change, the LOS clear.
        if (target == from)
        {
            if (firer is not EquipmentInstance || shot.VehicleTarget is not null || ReadLocation(state, target) is not { } ownRead || TerrainKey(ownRead) is not { } ownTerrain)
            {
                return (null, "play.ordnance-own-hex: only a Gun fires within its own Location, at Infantry (C5.5)");
            }

            return ((shot with
            {
                Range = 0,
                HexspinesToTurn = 0,
                FirerInWoodsOrBuilding = WoodsOrBuilding.Contains(ownTerrain),
                ElevationAllowed = true,
                SameHex = true,
                Hit = shot.Hit! with
                {
                    SameLevel = true,
                    Los = new FireLos(false, 0, true, false),
                    TargetTerrain = ownTerrain,
                    Targets = [.. shot.Hit.Targets!.Select(item => item with { KnownEnemyInLos = true, Captors = item.Captors ?? [] })],
                },
            }, null), null);
        }

        var tank = firer as UnitInstance;
        var turreted = tank is not null && OrdnanceReference.Value.Guns.GetValueOrDefault(shot.Gun.DefinitionId!)?.MaType is "t" or "st" or "rst" or "1mt";
        if ((tank is null ? ((MapPosition)((EquipmentInstance)firer).Position).Facing : LiveOrdnance.TurretFacing(state, tank)) is not { } facing)
        {
            return (null, $"play.ordnance-facing: '{firer.Id}' has no facing, so its Covered Arc is unknown (C3.2)");
        }

        var probe = shot.Hit! with
        {
            Firers = [new FireFirer(shot.Crew!.UnitId, shot.Crew.DefinitionId, from.ToString(), false, false, false, false, false)],
            FirerLocationId = from.ToString(),
        };
        var (read, reason) = FireMapFacts(state, probe, target);
        if (read is null)
        {
            return (null, reason);
        }

        if (ReadLocation(state, from) is not { } gunRead || TerrainKey(gunRead) is not { } gunTerrain)
        {
            return (null, "play.ordnance-map: the Gun's Location cannot be read");
        }

        if (Bearing(state, from, target) is not { } bearing)
        {
            return (null, "play.ordnance-arc: the Covered Arc cannot be read between these Locations on this map (C3.2)");
        }

        // C3.2: the Covered Arc is the 60-degree wedge on the barrel's hexspine; C5.1: the fewest hexspines turned brings the target in.
        var turn = ScenarioA1OrdnanceMapRules.CoveredArcTurn((int)facing, bearing);
        var turns = (Facing: (UnitFacing)turn.Facing, turn.Steps);
        if (ScenarioA1OrdnanceMapRules.VcaRefusal(tank?.Id, turreted, turns.Steps) is { } vca)
        {
            return (null, vca);
        }

        // D3.2 (ruling R7.4): the Target Facing is read from the hexside of the target hex the firer's LOS crosses: within 60 degrees of the
        // VCA (or TCA) the front, beyond 120 degrees the rear, otherwise the side; along a hexspine the facing less favorable to the firer.
        OrdnanceVehicleTarget? aimed = null;
        if (shot.VehicleTarget is { } vehicleTarget && state.Unit(vehicleTarget.VehicleId!) is { } targetVehicle)
        {
            if (Bearing(state, target, from) is not { } back || (targetVehicle.Position as MapPosition)?.Facing is not { } hull)
            {
                return (null, $"play.ordnance-facing: {targetVehicle.Id}'s Target Facing is read only on one board not reversed, from a vehicle with a VCA (D3.2)");
            }

            var targetTurreted = OrdnanceReference.Value.Armor.Vehicles.GetValueOrDefault(vehicleTarget.DefinitionId!)?.Turreted == true;
            // D2.32 (ruling R11.2): a vehicle in Bypass has its Target Facing read from the firer's hex, for its turret too.
            var bypassFacing = BypassTargetFacing(state, targetVehicle, from);
            var (hullFacing, turretFacing) = ScenarioA1OrdnanceMapRules.VehicleTargetFacings(back, (int)hull, targetTurreted, bypassFacing,
                () => (int?)LiveOrdnance.TurretFacing(state, targetVehicle));
            aimed = vehicleTarget with
            {
                HullFacing = hullFacing,
                TurretFacing = turretFacing,
            };
        }

        // A12.14, A15.44: a concealed crew
        var revealing = HeatOfBattleFacts(state, read);
        var hit = shot.Hit! with
        {
            Range = null,
            SameLevel = read.SameLevel,
            Los = read.Los,
            LowVisibilityDrm = read.LowVisibilityDrm,
            BeyondNvr = read.BeyondNvr,
            TargetTerrain = read.TargetTerrain,
            Targets = revealing.Targets,
        };
        var withMap = shot with
        {
            Range = read.Range,
            HexspinesToTurn = turns.Steps,
            FirerInWoodsOrBuilding = WoodsOrBuilding.Contains(gunTerrain),
            // C2.6 (ruling R8.7): a Gun fires at another level only if the range is at least the elevation difference; other levels are then
            // undecided in the package until levels come to fire.
            ElevationAllowed = ReadLocation(state, target) is not { } targetRead
                || ScenarioA1OrdnanceMapRules.ElevationAllowed(Math.Abs(gunRead.Hex.BaseLevel + gunRead.Level.Level - (targetRead.Hex.BaseLevel + targetRead.Level.Level)), read.Range),
            Hit = hit,
            VehicleTarget = aimed ?? shot.VehicleTarget,
        };
        return ((withMap, turns.Steps > 0 ? turns.Facing : null), null);
    }

    /// <summary>
    /// The map reads of a SW's shot (rulings R9.2 to R9.4, R9.8): range from the firer; LOS, Hindrance, levels, and terrain from the firer or, for a
    /// Spotted mortar shot, from its Spotter in the mortar's hex or an adjacent one (C9.3); no CA; the firer's terrain for Case B, and for a PF
    /// whether it fires from a ground-level building (Case C3) or above it (refused: Desperation is not built, C13.8). A PF's Target Facing is
    /// read as a Gun's.
    /// </summary>
    private ((OrdnanceShot Shot, UnitFacing? Facing)? Facts, string? Reason) SupportWeaponMapFacts(GameState state, OrdnanceShot shot, BoardLocation from, BoardLocation target)
    {
        if (target == from)
        {
            return (null, "play.ordnance-own-hex: a SW does not fire within its own Location (C3.33, C13.3)");
        }

        if (ReadLocation(state, from) is not { } firerRead || TerrainKey(firerRead) is not { } firerTerrain)
        {
            return (null, "play.ordnance-map: the firer's Location cannot be read");
        }

        var sees = from;
        if (shot.Spotter is { UnitId: { } spotterId } && state.Location(spotterId)?.Location is { } spotterAt)
        {
            // C9.3: a Spotter in the mortar's hex or an adjacent one, whatever its level and LOS to the mortar.
            if (!(spotterAt.Board == from.Board && spotterAt.Hex == from.Hex) && !Step(state, from, spotterAt).Adjacent && Los(state, from, spotterAt)?.Range != 1)
            {
                return (null, $"play.ordnance-spotter: {spotterId} spots only from the mortar's hex or an adjacent one (C9.3)");
            }

            sees = spotterAt;
        }

        if (Los(state, from, target) is not { } ranged)
        {
            return (null, "play.ordnance-map: the range cannot be read");
        }

        var probe = shot.Hit! with
        {
            Firers = [new FireFirer(shot.Spotter?.UnitId ?? shot.Crew!.UnitId, shot.Spotter?.DefinitionId ?? shot.Crew!.DefinitionId, sees.ToString(), false, false, false, false, false)],
            FirerLocationId = sees.ToString(),
        };
        var (read, reason) = FireMapFacts(state, probe, target);
        if (read is null)
        {
            return (null, reason);
        }

        var latwType = OrdnanceReference.Value.Guns.GetValueOrDefault(shot.Gun!.DefinitionId ?? string.Empty)?.LatwType;
        var building = ScenarioA1OrdnanceMapRules.IsBuilding(firerTerrain);

        // B23.423 (referee, pass 9): no mortar fires from a non-rooftop building Location.
        if (ScenarioA1OrdnanceMapRules.SupportWeaponLocationRefusal(latwType, building, firerRead.Level.Level) is { } placed)
        {
            return (null, placed);
        }

        OrdnanceVehicleTarget? aimed = null;
        if (shot.VehicleTarget is { } vehicleTarget && state.Unit(vehicleTarget.VehicleId!) is { } targetVehicle)
        {
            if (Bearing(state, target, from) is not { } back || (targetVehicle.Position as MapPosition)?.Facing is not { } hull)
            {
                return (null, $"play.ordnance-facing: {targetVehicle.Id}'s Target Facing is read only on one board not reversed, from a vehicle with a VCA (D3.2)");
            }

            var targetTurreted = OrdnanceReference.Value.Armor.Vehicles.GetValueOrDefault(vehicleTarget.DefinitionId!)?.Turreted == true;
            // D2.32 (ruling R11.2): a vehicle in Bypass has its Target Facing read from the firer's hex, for its turret too.
            var bypassFacing = BypassTargetFacing(state, targetVehicle, from);
            var (hullFacing, turretFacing) = ScenarioA1OrdnanceMapRules.VehicleTargetFacings(back, (int)hull, targetTurreted, bypassFacing,
                () => (int?)LiveOrdnance.TurretFacing(state, targetVehicle));
            aimed = vehicleTarget with
            {
                HullFacing = hullFacing,
                TurretFacing = turretFacing,
            };
        }

        var revealing = HeatOfBattleFacts(state, read);
        return ((shot with
        {
            Range = ranged.Range,
            HexspinesToTurn = 0,
            FirerInWoodsOrBuilding = WoodsOrBuilding.Contains(firerTerrain),
            ElevationAllowed = true,
            Hit = shot.Hit! with
            {
                Range = null,
                SameLevel = read.SameLevel,
                Los = read.Los,
                LowVisibilityDrm = read.LowVisibilityDrm,
                BeyondNvr = read.BeyondNvr,
                TargetTerrain = read.TargetTerrain,
                Targets = revealing.Targets,
            },
            VehicleTarget = aimed ?? shot.VehicleTarget,
            Panzerfaust = shot.Panzerfaust is null ? null : shot.Panzerfaust with
            {
                FromBuilding = building
            },
        }, null), null);
    }

    /// <summary>
    /// The map and state reads of the backlog pass 8 (rulings R8.1, R8.3, R8.5, R8.8, R8.10): the moving vehicle's MP in the firer's continuous
    /// LOS and a move into Open Ground (C6.11, C6.14), the Gun and its crew in the target Location with its Emplacement and gunshield (C11.2,
    /// C11.5), whether a Good Order enemy sees a concealed crew fire (A12.14), and the overstacking of both Locations (A5.12, A5.131).
    /// </summary>
    private OrdnanceShot PassEightFacts(GameState state, OrdnanceShot shot, BoardLocation target)
    {
        var from = FirerLocation(state, shot.Gun!.GunId!);
        var side = (state.Find(shot.Gun.GunId!) as UnitInstance)?.Side ?? state.Unit(shot.Crew!.UnitId!)!.Side;
        if (shot.Movement is { } movement)
        {
            shot = shot with
            {
                Movement = shot.VehicleTarget is { VehicleId: { } moving }
                    ? movement with
                    {
                        MpInLos = MpInLos(state, from, moving)
                    }
                    : movement with
                    {
                        OpenGround = ScenarioA1OrdnanceMapRules.OpenGround(shot.Hit!.TargetTerrain, shot.Hit.Los?.HindranceDrm)
                    },
            };
        }

        if (shot.VehicleTarget is null && shot.TargetType is null && GunAt(state, target, side, [from]) is { } gunTarget)
        {
            shot = shot with
            {
                Hit = shot.Hit! with
                {
                    GunTarget = gunTarget
                }
            };
        }

        // C6.43 (ruling R8.8): the Bore Sighting counts while its original crew fires the Gun from its setup Location.
        var boreSighted = LiveOrdnance.BoreSighted(state, shot.Gun.GunId!, shot.Crew!.UnitId!, from, target);
        return shot with
        {
            BoreSighted = boreSighted ? true : null,
            CrewSeen = shot.Crew!.Concealed == true && shot.Vehicle is null ? EnemyGoodOrderInLosWithin16(state, side, from) : null,
            FirerOverstack = LiveOrdnance.Excess(state, from, side) is var over and > 0 ? over : (int?)null,
            TargetOverstack = shot.VehicleTarget is null && state.Sides.FirstOrDefault(item => item.Id != side)?.Id is { } enemy && LiveOrdnance.Excess(state, target, enemy) is var crowded and > 0
                ? crowded : (int?)null,
        };
    }

    /// <summary>
    /// The MP a moving vehicle has spent in a firer's continuous LOS this MPh (C6.11, C6.12, C6.15), counted back from its latest expenditure
    /// to the last Location the firer could not see; a vehicle seen since its MPh began takes Case J alone, read as 99.
    /// </summary>
    private int MpInLos(GameState state, BoardLocation firer, string vehicle)
    {
        var history = store.Read(state.Scope)?.Events ?? [];
        var start = history.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged).index;
        var steps = history.Skip(start).Select(item => item.Payload).OfType<VehicleStepped>().Where(step => step.Vehicle == vehicle).ToArray();
        // C6.15 (table player, pass 8): a vehicle that began its MPh out of the firer's LOS has spent all its MP since in that LOS.
        return ScenarioA1OrdnanceMapRules.MpInLos(
            [.. steps.Select(step => (step.HalfMp, (Func<bool>)(() => Los(state, firer, step.At) is { Status: LosStatus.Clear })))],
            () => Replay([.. history.Take(start + 1)]).Current?.Location(vehicle)?.Location is { } origin && Los(state, firer, origin) is not { Status: LosStatus.Clear });
    }

    /// <summary>
    /// The enemy Gun whose crew is alone in the target Location (C11.2, C11.3, C11.5): Emplaced when manned by a crew and never moved or hooked
    /// up; its gunshield faces a firer within its CA and outside its hex when it is an AT or INF Gun.
    /// </summary>
    internal FireGunTarget? GunAt(GameState state, BoardLocation target, string firingSide, IReadOnlyList<BoardLocation> firers)
    {
        var gun = state.Equipment.FirstOrDefault(item => item.Status == InstanceStatus.Active && item.Kind == "asl:gun" && item.Position is MapPosition at && at.Location == target
            && item.Holding is { Role: HoldingRole.Manned } && state.Unit(item.Holding.Holder)?.Side != firingSide);
        if (gun?.Holding is not { } manning || state.Unit(manning.Holder) is not { } crew)
        {
            return null;
        }

        var definition = OrdnanceReference.Value.Guns.GetValueOrDefault(gun.Definition?.Definition ?? string.Empty);
        var facing = (gun.Position as MapPosition)?.Facing;
        var withinCa = firers.Count > 0 && facing is { } barrel && firers.Any(from => from != target && Bearing(state, target, from) is { } bearing
            && ScenarioA1OrdnanceMapRules.WithinArc(bearing, (int)barrel));
        // C11.5 (referee, pass 8): the gunshield protects only a Good Order crew, never one moving or pushing the Gun under Defensive First Fire.
        var crewKind = crew.Kind == "asl:crew";
        var emplaced = LiveOrdnance.Emplaced(state, gun);
        var moving = state.Phase == "mph" && state.Movement?.Movers.Contains(crew.Id) == true;
        return new FireGunTarget(gun.Id, gun.Definition?.Definition, crew.Id, emplaced, ScenarioA1OrdnanceMapRules.Gunshield(definition?.GunType, withinCa, crewKind, moving));
    }

    /// <summary>
    /// A target Location as a Gun's crew reads it (C3.2, C2.25; ruling R8.9): its own Location (Case E), no LOS, beyond the Gun's range, within its
    /// CA, or the hexspines it must turn; null when the Gun or the map cannot be read.
    /// </summary>
    public string? GunTargetStatus(GameState state, string gunId, BoardLocation target)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Find(gunId) is not EquipmentInstance { Position: MapPosition { Facing: { } facing } at } gun
            || OrdnanceReference.Value.Guns.GetValueOrDefault(gun.Definition?.Definition ?? string.Empty) is not { } definition)
        {
            return null;
        }

        if (target == at.Location)
        {
            return "its own Location (Case E, C5.5)";
        }

        if (Los(state, at.Location, target) is not { Status: LosStatus.Clear } los)
        {
            return "no LOS";
        }

        return ScenarioA1OrdnanceMapRules.GunTargetStatus(los.Range, definition.RangeMaximum,
            () => ReadLocation(state, at.Location) is { } gunRead && ReadLocation(state, target) is { } targetRead
                ? Math.Abs(gunRead.Hex.BaseLevel + gunRead.Level.Level - (targetRead.Hex.BaseLevel + targetRead.Level.Level)) : null,
            () => Bearing(state, at.Location, target), (int)facing);
    }

    /// <summary>Where a Gun, a tank, a SW's possessor, or a PF's firer is.</summary>
    private static BoardLocation FirerLocation(GameState state, string id) =>
        state.Location(id)?.Location ?? (id.EndsWith(":pf", StringComparison.Ordinal) ? state.Location(id[..^":pf".Length])?.Location : null)
            ?? throw new InvalidOperationException($"'{id}' is not on the map.");

    /// <summary>The bearing from one Location to another in degrees counterclockwise from east, on one board or across the composed map; null when unread.</summary>
    private double? Bearing(GameState state, BoardLocation from, BoardLocation to)
    {
        Maps.Geometry.HexIndex one, two;
        if (from.Board == to.Board && state.Map.Board(from.Board) is { } placed && placed.Slot is not { Reversed: true }
            && boards.TryGetBoard(from.Board, placed.Version).Board is { } handle
            && handle.Geometry.TryGetIndex(from.Hex, out var first) && handle.Geometry.TryGetIndex(to.Hex, out var second))
        {
            (one, two) = (first, second);
        }
        else if (state.Map.IsPlaced && Composed(state)?.Layout is { } layout && layout.Locate(from.Board, from.Hex) is { } a && layout.Locate(to.Board, to.Hex) is { } b)
        {
            // C3.2 (ruling R8.7): across boards and on a reversed board, the composed map's hex grid gives the bearing.
            (one, two) = (a, b);
        }
        else
        {
            return null;
        }

        // Flat-topped hexes in columns, odd columns half a hex higher (BoardGeometry.CenterDot); y grows downward on the board.
        static (double X, double Y) Center(Maps.Geometry.HexIndex hex) => (1.5 * hex.Column, Math.Sqrt(3) * (hex.Row - (0.5 * (hex.Column % 2))));
        var (x1, y1) = Center(one);
        var (x2, y2) = Center(two);
        return Math.Atan2(-(y2 - y1), x2 - x1) * 180 / Math.PI;
    }

    /// <summary>
    /// Draws the rolls the package asks for, one at a time, and adds the shot's events: the dice, the ordnance record, the hit's effects
    /// on the target units, the Gun's malfunction, the fire markers of the Gun and its crew, and any surrender that follows.
    /// </summary>
    private void AddOrdnanceEvents(GameScope scope, string attemptId, long expected, string actor, GameState state, OrdnanceShot facts, UnitFacing? facing,
        List<GameEvent> events, Func<RollRequest, RollResult> draw, ResumedRolls? resumed = null)
    {
        var reference = OrdnanceReference.Value;
        var package = ScenarioA1OrdnancePackage.Identity.ToString();

        // Ruling R5.8: the owners' options in the hit's IFT attack are asked for as it reaches them; a resumed shot starts from its rolls.
        facts = facts with
        {
            Hit = facts.Hit! with
            {
                Choices = facts.Hit.Choices ?? new Dictionary<string, string>(StringComparer.Ordinal)
            }
        };
        var rollIds = new Dictionary<string, string>(resumed?.RollIds ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        var rolls = new OrdnanceRolls(null, null, null, null, null);
        foreach (var (key, values) in resumed?.Values ?? [])
        {
            rolls = ApplyOrdnanceRoll(rolls, key, values);
        }

        OrdnanceResolution resolution;
        while ((resolution = ScenarioA1OrdnanceCalculator.Resolve(facts with
        {
            Rolls = rolls
        }, reference)).Disposition != OrdnanceResolution.Resolved)
        {
            if (resolution.Reasons is [{ } option] && option.StartsWith("asl.a1.ordnance.choice-missing:", StringComparison.Ordinal))
            {
                var resume = new JsonObject
                {
                    ["record"] = "ordnance",
                    ["facts"] = JsonNode.Parse(JsonSerializer.Serialize(facts, LiveFire.Json)),
                    ["rolls"] = RollNode(rollIds),
                };
                if (facing is { } turned)
                {
                    resume["facing"] = UnitFacings.Name(turned);
                }

                events.Add(Event(scope, attemptId, events.Count + 1, expected, "choice-pending", Pending(state, option["asl.a1.ordnance.choice-missing:".Length..], resume),
                    package, null));
                return;
            }

            if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.ordnance.roll-missing:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Ordnance package left a shot it had accepted undecided: " + string.Join("; ", resolution.Reasons));
            }

            var key = missing["asl.a1.ordnance.roll-missing:".Length..];
            string[] selected = key.StartsWith("criticalSelection:", StringComparison.Ordinal) ? key["criticalSelection:".Length..].Split(',') : [];
            var nested = key.StartsWith("hit:", StringComparison.Ordinal) ? "hit:" : key.StartsWith("critical-hit:", StringComparison.Ordinal) ? "critical-hit:" : null;
            var inner = nested is null ? null : key[nested.Length..];
            var (count, purpose) = key switch
            {
                "toHit" => (2, "ordnance-to-hit"),
                "panzerfaustCheck" => (1, "panzerfaust-check"),
                "subsequent" => (1, "ordnance-subsequent"),
                "toKill" => (2, "ordnance-to-kill"),
                "shockCheck" => (2, "ordnance-shock-check"),
                "crewCheck" => (2, "ordnance-crew-check"),
                "crewSurvival" => (2, "ordnance-crew-survival"),
                _ when selected.Length > 0 => (selected.Length, "ordnance-random-selection"),
                _ => FireRollShape(inner!),
            };
            var drawn = draw(new RollRequest(count, 6));
            var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
            rollIds[key] = rollId;
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                new DiceRolled(rollId, purpose, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
            rolls = ApplyOrdnanceRoll(rolls, key, drawn.Values);
        }

        // C13.31 (ruling R9.7): a PF Check that gave no shot is recorded with no To Hit DR; the check is the unit's SW use.
        var gunResult = resolution.Gun ?? new OrdnanceGunEffect(0, false, 0, false, facts.Phase == "MPh" ? "first-fire" : null, 0, null);
        var recordId = EventId(attemptId, events.Count + 1);
        BoardLocation? acquired = gunResult.AcquiredLocationId is { } acquiredAt ? BoardLocation.Parse(acquiredAt) : null;
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "ordnance-fired",
            new OrdnanceFired(facts.Gun!.GunId!, facts.Crew!.UnitId!, BoardLocation.Parse(facts.TargetLocationId!), facing, gunResult.RateOfFireKept,
                gunResult.Acquisition, acquired, rollIds, JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json)),
            package, null));

        // C13.31, C13.36 (ruling R9.7): a PF Check's Original 6 pins or breaks its firer, or gives Casualty Reduction; so does an Original 12 To Hit DR.
        if (resolution.FirerEffect is { } firerEffect && state.Unit(facts.Crew.UnitId!) is { } shooter)
        {
            var phaseMarker = ConditionName(ScenarioA1OrdnanceEventRules.FirerPhaseMarker(facts.Phase));
            foreach (var (type, payload) in FirerEffectEvents(shooter, firerEffect, attemptId, phaseMarker))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, [recordId]));
            }
        }

        // C8.9: Special Ammunition the Gun turned out not to have was never fired, unless the Gun malfunctioned: no marker, no Acquisition.
        if (resolution.AmmunitionUse == "none" && !gunResult.Malfunctioned)
        {
            return;
        }

        // C7.7, D5.5, D5.6: a vehicle target's fate.
        if (resolution.Kill is { } kill && state.Unit(facts.VehicleTarget!.VehicleId!) is { } struck)
        {
            foreach (var (type, payload) in KillEvents(state, struck, kill, attemptId))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, [recordId]));
            }
        }

        // C11.4, C11.6 (ruling R8.3): a Direct Hit destroys the Gun in the target Location, a K malfunctions it.
        if (resolution.GunTargetFate is { } fate && facts.Hit!.GunTarget?.GunId is { } struckGun)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, fate == "destroyed" ? "instance-eliminated" : "conditions-changed",
                fate == "destroyed" ? new InstanceEliminated(struckGun) : new ConditionsChanged(struckGun,
                    new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.Malfunctioned] = ConditionState.True }), package, null, [recordId]));
        }

        // The hit's effects: the Critical Hit's and the normal hit's units, with DM for a broken unit an attack could have given a NMC (A10.62).
        var effects = new List<FireUnitEffect>();
        foreach (var attack in new[] { resolution.CriticalHit, resolution.Hit }.OfType<FireResolution>())
        {
            var nestedFacts = facts.Hit! with
            {
                OrdnanceHit = new FireOrdnanceHit(facts.Gun.GunId, 0, false),
                Targets = [.. facts.Hit.Targets!.Where(item => attack.Effects.Any(effect => effect.UnitId == item.UnitId))]
            };
            var desperate = CouldCauseNmc(nestedFacts, attack.Arithmetic!);
            foreach (var effect in attack.Effects)
            {
                var target = facts.Hit.Targets!.First(item => item.UnitId == effect.UnitId);
                var attackedBroken = target.Broken == true && desperate(target.Concealed == true || target.Hidden == true || target.Dummy == true);
                foreach (var payload in EffectEvents(state, effect, attemptId, attackedBroken))
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, payload.Type, payload.Payload, package, null, [recordId]));
                }
            }

            foreach (var effect in attack.CompanionEffects ?? [])
            {
                if (EffectEvent(state, effect, attemptId, false) is { } companion)
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, companion.Type, companion.Payload, package, null, [recordId]));
                }
            }

            effects.AddRange(attack.Effects);
        }

        // C2.28: the malfunction; A7.1, C2.24: the Gun and its crew carry the fire phase's marker from their first shot; A8.1, C2.241 (ruling R8.1):
        // in the MPh a First Fire counter only once its ROF is spent; C5.6 (ruling R8.2): an Intensive Fire counter beside it.
        string? marker = ScenarioA1OrdnanceEventRules.ShotMarker(facts.Phase, gunResult.FireCounter) is { } shotMarker ? ConditionName(shotMarker) : null;
        var gunConditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);

        // C13.47 (ruling R9.11): a PSK is removed on its X#.
        var removed = gunResult.Malfunctioned && OrdnanceReference.Value.Guns.GetValueOrDefault(facts.Gun.DefinitionId ?? string.Empty)?.LatwType == "psk";
        if (removed)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(facts.Gun.GunId!), package, null, [recordId]));
        }
        else if (gunResult.Malfunctioned)
        {
            gunConditions[Conditions.Malfunctioned] = ConditionState.True;
        }

        if (state.Find(facts.Gun.GunId!) is { } gun && marker is not null && GameState.Condition(gun, marker) != ConditionState.True)
        {
            gunConditions[marker] = ConditionState.True;
        }

        if (gunResult.FireCounter == "intensive-fire")
        {
            gunConditions[Conditions.IntensiveFire] = ConditionState.True;
        }

        // A12.34 (ruling R26.5): an Emplaced hidden or concealed Gun that fires is revealed, with its crew, when the colored dr of its Original To Hit DR is 5 or
        // more and the nearest Good Order enemy ground unit with a LOS to it is within 16 hexes, or the dr is a 6 and that unit 17 hexes or more away;
        // otherwise both are placed (or stay) beneath "?"; they keep HIP when no such unit has a LOS to the Gun.
        Dictionary<string, ConditionState>? emplacedReveal = null;
        if (facts.Vehicle is null && state.Find(facts.Gun.GunId!) is EquipmentInstance firingGun && LiveOrdnance.Emplaced(state, firingGun) && state.Unit(facts.Crew.UnitId!) is { } gunCrew
            && new[] { (IGameObject)firingGun, gunCrew }.Any(item => GameState.Condition(item, Conditions.Concealed) == ConditionState.True || GameState.Condition(item, Conditions.Hidden) == ConditionState.True)
            && rolls.ToHit is [var colored, ..] && state.Location(firingGun.Id) is { } firingAt)
        {
            var nearest = NearestGoodOrderEnemyInLos(state, gunCrew.Side, firingAt.Location);
            emplacedReveal = ConditionChanges(ScenarioA1OrdnanceEventRules.EmplacedReveal(nearest, colored));
            foreach (var (name, value) in emplacedReveal)
            {
                if (GameState.Condition(firingGun, name) != value)
                {
                    gunConditions[name] = value;
                }
            }
        }

        // A12.14 (ruling R8.5): a concealed Gun loses its "?" with its crew.
        if (emplacedReveal is null && resolution.CrewConcealmentLost == true && state.Find(facts.Gun.GunId!) is EquipmentInstance concealedGun
            && (GameState.Condition(concealedGun, Conditions.Concealed) == ConditionState.True || GameState.Condition(concealedGun, Conditions.Hidden) == ConditionState.True))
        {
            gunConditions[Conditions.Concealed] = ConditionState.False;
            gunConditions[Conditions.Hidden] = ConditionState.False;
        }

        // A12.14: a concealed tank that fires loses its "?"; it is its own crew.
        if (facts.Crew.UnitId == facts.Gun.GunId && resolution.CrewConcealmentLost == true)
        {
            gunConditions[Conditions.Concealed] = ConditionState.False;
            gunConditions[Conditions.Hidden] = ConditionState.False;
        }

        if (gunConditions.Count > 0 && !removed)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(facts.Gun.GunId!, gunConditions), package, null, [recordId]));
        }

        // A12.14: a concealed crew that fires loses its "?"; a tank is its own crew and carries the marker once.
        if (facts.Crew.UnitId != facts.Gun.GunId && state.Unit(facts.Crew.UnitId!) is { } crew)
        {
            var crewConditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
            if (marker is not null && GameState.Condition(crew, marker) != ConditionState.True)
            {
                crewConditions[marker] = ConditionState.True;
            }

            if (emplacedReveal is not null)
            {
                foreach (var (name, value) in emplacedReveal.Where(pair => GameState.Condition(crew, pair.Key) != pair.Value))
                {
                    crewConditions[name] = value;
                }
            }
            else if (resolution.CrewConcealmentLost == true)
            {
                crewConditions[Conditions.Concealed] = ConditionState.False;
                crewConditions[Conditions.Hidden] = ConditionState.False;
            }

            if (crewConditions.Count > 0 && resolution.FirerEffect is not (OrdnancePanzerfaustCheck.CasualtyReduction or OrdnancePanzerfaustCheck.Broken))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(crew.Id, crewConditions), package, null, [recordId]));
            }
        }

        // C9.3, A7.531 (rulings R9.2, R9.4): the Spotter has used a SW and the directing leader has directed: both carry the phase's marker.
        foreach (var supporter in new[] { facts.Spotter?.UnitId, facts.Director?.UnitId }.OfType<string>())
        {
            if (marker is not null && state.Unit(supporter) is { } supporting && GameState.Condition(supporting, marker) != ConditionState.True)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                    new ConditionsChanged(supporting.Id, new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [marker] = ConditionState.True }), package, null, [recordId]));
            }
        }

        // C6.5, C6.51 (ruling R5.13): the Acquisition is on the Known units the shot leaves in its target Location; C9.2: a mortar's Area Target
        // Acquisition stays on its hex.
        if (acquired is { } acquiredLocation && facts.TargetType is null && AcquiredUnits(facts, effects, attemptId) is { Count: > 0 } units)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "acquisition-changed", new AcquisitionChanged(facts.Gun.GunId!, acquiredLocation, units), package, null,
                [recordId]));
        }

        // A15.5: a unit that surrendered waits for the captor's choice, last.
        foreach (var effect in effects)
        {
            if (!effect.Eliminated && (effect.SecondHeatOfBattle ?? effect.HeatOfBattle) is { Result: HeatOfBattleOutcome.Surrender, Captors.Count: > 0 } surrender)
            {
                var id = effect.FinalDefinitionId != effect.DefinitionId ? $"{attemptId}-{effect.UnitId}" : effect.UnitId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "surrender-pending", new SurrenderPending(id, surrender.Captors!), package, null, [recordId]));
            }
        }
    }

    /// <summary>
    /// The events of a To Kill result (rulings R7.7 to R7.9): a burning wreck with its Blaze (D10.1, B25.14), or a wreck whose crew may survive
    /// beneath it (D5.6); an immobilized vehicle, Abandoned by a crew that fails its TC (D5.5); a Shocked AFV, BU and stopped (C7.42), which
    /// a second Shock returns from an Unconfirmed Kill. A concealed vehicle hit loses its "?" (A12.2).
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> KillEvents(GameState state, UnitInstance vehicle, OrdnanceKill kill, string attemptId)
    {
        var at = state.Location(vehicle.Id)?.Location;
        if (ScenarioA1OrdnanceEventRules.Wrecks(kill.Result))
        {
            var burning = kill.Result == OrdnanceKill.Burn;
            yield return ("vehicle-wrecked", new VehicleWrecked(vehicle.Id, burning));
            if (burning && at is { } burningAt)
            {
                yield return ("instance-created", new InstanceCreated(new NewInstance(BlazeId(vehicle.Id), "asl:fire", null, null, new MapPosition(burningAt), null,
                    new Dictionary<string, ConditionState>())));
            }
            else if (kill.CrewSurvival is { Survived: true } && CrewCounter(vehicle, attemptId) is { } survivors)
            {
                yield return ("instance-created", survivors);
            }

            yield break;
        }

        var changed = ConditionChanges(ScenarioA1OrdnanceEventRules.KillConditions(kill, Is(vehicle, Conditions.Concealed) || Is(vehicle, Conditions.Hidden)));
        if (changed.Count > 0)
        {
            yield return ("conditions-changed", new ConditionsChanged(vehicle.Id, changed));
        }

        if (kill.Abandoned == true)
        {
            yield return ("conditions-changed", new ConditionsChanged(vehicle.Id, ConditionChanges(ScenarioA1OrdnanceEventRules.AbandonedConditions)));
            if (CrewCounter(vehicle, attemptId) is { } crew)
            {
                yield return ("instance-created", crew);
            }
        }
    }

    /// <summary>
    /// What a PF does to its own firer (C13.31, C13.36; ruling R9.7): pinned; broken; or Casualty Reduction: a squad becomes its HS, a HS or crew
    /// is eliminated, a SMC is wounded, or eliminated if already wounded (A7.302, A17.2).
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> FirerEffectEvents(UnitInstance unit, string effect, string attemptId, string marker)
    {
        if (effect == OrdnancePanzerfaustCheck.Pinned)
        {
            yield return ("conditions-changed", new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.Pinned] = ConditionState.True }));
            yield break;
        }

        if (effect == OrdnancePanzerfaustCheck.Broken)
        {
            yield return ("conditions-changed", new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState>(StringComparer.Ordinal)
            {
                [Conditions.Broken] = ConditionState.True,
                [Conditions.Pinned] = ConditionState.False,
                [marker] = ConditionState.True,
            }));
            yield break;
        }

        string? half = null;
        var casualty = ScenarioA1OrdnanceEventRules.Casualty(unit.Kind, () => unit.Definition is { } squad && (half = ScenarioA1FireReference.HalfSquadOf(squad.Definition)) is not null,
            GameState.Condition(unit, Conditions.Wounded) == ConditionState.True);
        if (casualty == FirerCasualty.HalfSquad)
        {
            // Table player, pass 9: the HS has fired, as its squad had.
            yield return ("lineage", new LineageRecorded(LineageAction.Reduced, [unit.Id],
                [new NewInstance($"{attemptId}-{unit.Id}", "asl:half-squad", half, unit.Side, unit.Position, null,
                    new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { [marker] = ConditionState.True })]));
        }
        else if (casualty == FirerCasualty.Wounded)
        {
            yield return ("conditions-changed", new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState>(StringComparer.Ordinal)
            {
                [Conditions.Wounded] = ConditionState.True,
                [marker] = ConditionState.True,
            }));
        }
        else
        {
            yield return ("instance-eliminated", new InstanceEliminated(unit.Id));
        }
    }

    /// <summary>A vehicle's crew placed beneath it as a crew counter of its nationality (D5.5, D5.6; ruling R7.9).</summary>
    private static InstanceCreated? CrewCounter(UnitInstance vehicle, string attemptId)
    {
        var nationality = VehicleDefinition(vehicle)?.Nationality;
        return FireReference.Value.Definitions.Values.Where(item => item.Kind == "asl:crew" && item.Nationality == nationality).Select(item => item.Id)
            .Order(StringComparer.Ordinal).FirstOrDefault() is { } crew && vehicle.Position is MapPosition position
            ? new InstanceCreated(new NewInstance($"{attemptId}-{vehicle.Id}-crew", "asl:crew", crew, vehicle.Side, new MapPosition(position.Location), null,
                new Dictionary<string, ConditionState>(StringComparer.Ordinal)
                {
                    [Conditions.Broken] = ConditionState.False,
                    [Conditions.Pinned] = ConditionState.False,
                    [Conditions.Concealed] = ConditionState.False,
                    [Conditions.Hidden] = ConditionState.False,
                }))
            : null;
    }

    /// <summary>An Ordnance package roll added to its rolls under its key.</summary>
    private static OrdnanceRolls ApplyOrdnanceRoll(OrdnanceRolls rolls, string key, IReadOnlyList<int> values)
    {
        string[] selected = key.StartsWith("criticalSelection:", StringComparison.Ordinal) ? key["criticalSelection:".Length..].Split(',') : [];
        return key switch
        {
            "toHit" => rolls with { ToHit = values },
            "panzerfaustCheck" => rolls with { PanzerfaustCheck = values[0] },
            "subsequent" => rolls with { Subsequent = values[0] },
            "toKill" => rolls with { ToKill = values },
            "shockCheck" => rolls with { ShockCheck = values },
            "crewCheck" => rolls with { CrewCheck = values },
            "crewSurvival" => rolls with { CrewSurvival = values },
            _ when selected.Length > 0 => rolls with
            {
                CriticalSelection = selected.Select((id, index) => (id, index)).ToDictionary(item => item.id, item => values[item.index], StringComparer.Ordinal)
            },
            _ when key.StartsWith("hit:", StringComparison.Ordinal) => rolls with { Hit = ApplyFireRoll(rolls.Hit ?? new FireRolls(null, null, null, null), key["hit:".Length..], values) },
            _ => rolls with { CriticalHit = ApplyFireRoll(rolls.CriticalHit ?? new FireRolls(null, null, null, null), key["critical-hit:".Length..], values) },
        };
    }

    /// <summary>The dice count and purpose of a Fire package roll key (as the Fire planner draws them).</summary>
    private static (int Count, string Purpose) FireRollShape(string key)
    {
        var split = key.IndexOf(':', StringComparison.Ordinal);
        var (kind, unit) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
        return kind switch
        {
            "attack" => (2, "fire-ift"),
            "randomSelection" => (unit.Split(',').Length, "fire-random-selection"),
            "weaponSelection" => (unit.Split(',').Length, "fire-weapon-selection"),
            "firerSelection" => (unit.Split(',').Length, "fire-firer-selection"),
            "checks" => (2, "fire-check"),
            "leaderLoss" => (2, "fire-leader-loss"),
            "heatOfBattle" => (2, "fire-heat-of-battle"),
            "berserkCheck" => (2, "fire-check"),
            _ => (1, "fire-wound-severity"),
        };
    }

    /// <summary>A Fire package roll added to its rolls under its key.</summary>
    private static FireRolls ApplyFireRoll(FireRolls rolls, string key, IReadOnlyList<int> values)
    {
        var split = key.IndexOf(':', StringComparison.Ordinal);
        var (kind, unit) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
        Dictionary<string, int> Selection(IReadOnlyDictionary<string, int>? existing)
        {
            var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
            foreach (var (id, index) in unit.Split(',').Select((id, index) => (id, index)))
            {
                next[id] = values[index];
            }

            return next;
        }

        return kind switch
        {
            "attack" => rolls with { Attack = values },
            "randomSelection" => rolls with { RandomSelection = Selection(rolls.RandomSelection) },
            "weaponSelection" => rolls with { WeaponSelection = Selection(rolls.WeaponSelection) },
            "firerSelection" => rolls with { FirerSelection = Selection(rolls.FirerSelection) },
            "checks" => rolls with { Checks = Add(rolls.Checks, unit, values) },
            "leaderLoss" => rolls with { LeaderLoss = Add(rolls.LeaderLoss, unit, values) },
            "heatOfBattle" => rolls with { HeatOfBattle = Add(rolls.HeatOfBattle, unit, values) },
            "berserkCheck" => rolls with { BerserkChecks = Add(rolls.BerserkChecks, unit, values) },
            "crewCheck" => rolls with { CrewChecks = Add(rolls.CrewChecks, unit, values) },
            "unlikelyKill" => rolls with { UnlikelyKill = Add(rolls.UnlikelyKill, unit, values[0]) },
            "molCheck" => rolls with { MolCheck = values[0] },
            _ => rolls with { WoundSeverity = Add(rolls.WoundSeverity, unit, values[0]) },
        };
    }
}
