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

        // C8.4 (pass 55, ruling R55.9): Canister is fired at an aim of the Covered Arc, not at a Location: its shot names no target, and one it
        // names is not read. Named with a vehicle it stays a Vehicle Target Type shot, which that Target Type refuses.
        if (Text(arguments, "gunId", out var canisterGun) && Text(arguments, "ammunition", out var canisterNamed) && canisterNamed == ScenarioA1CanisterCalculator.Ammunition
            && !Text(arguments, "targetVehicle", out _))
        {
            return PlanFireCanister(scope, arguments, existing, state, canisterGun, attemptId, expected, label, actor);
        }

        var (planned, refusal) = OrdnanceShotFacts(state, arguments);
        if (planned is null)
        {
            return Refused(scope, label, expected, refusal!);
        }

        var (facts, facing) = planned.Value;

        // C2.2401 (pass 55, ruling R55.19): a Defensive First Fire shot at the moving vehicle that it may answer with a Gun Duel is declared, not
        // rolled: the commit is the pending choice, and the shot resolves with the ATTACKER's answer.
        if (GunDuelDeclaration(scope, state, arguments, facts, facing, attemptId, expected, label) is { } declaration)
        {
            return declaration;
        }

        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            AddOrdnanceEvents(scope, attemptId, expected, actor, state, facts, facing, events, draw);
            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [OrdnanceSummary(facts, facing)])
        {
            Roll = new PlannedRoll("ordnance", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// The facts of an ordnance shot its request names, accepted by every check made before a roll, with the facing its Gun turns to; or the reason
    /// it is refused. The state's facts, the planner's map reads, and the Ordnance package's pre-check, in that order. <paramref name="gunDuel"/>
    /// plans the moving vehicle's MA shot as a Gun Duel's first shot (C2.2401; pass 55, ruling R55.19), which waives the closed window and nothing else.
    /// </summary>
    private ((OrdnanceShot Facts, UnitFacing? Facing)? Shot, string[]? Reasons) OrdnanceShotFacts(GameState state, JsonElement arguments, bool gunDuel = false)
    {
        if (!Text(arguments, "gunId", out var gunId) || !Text(arguments, "target", out var targetText) || !BoardLocation.TryParse(targetText, out var target))
        {
            return (null, ["play.invalid-arguments: a Gun's shot names the Gun and a target Location"]);
        }

        string? targetVehicle = Text(arguments, "targetVehicle", out var named) ? named : null;
        string? ammunition = Text(arguments, "ammunition", out var declared) ? declared : null;
        var intensive = arguments.TryGetProperty("intensive", out var intensiveArgument) && intensiveArgument.ValueKind == JsonValueKind.True;
        string? spotter = Text(arguments, "spotter", out var spotterId) ? spotterId : null;
        string? director = Text(arguments, "director", out var directorId) ? directorId : null;
        var (shot, reason) = LiveOrdnance.FromState(state, gunId, target, targetVehicle, ammunition, intensive, spotter, director, gunDuel);
        if (shot is null)
        {
            return (null, [reason!]);
        }

        // A7.25, A15.432 (table player, pass 12): an Opportunity Firer or a berserk unit fires no ordnance or LATW in its PFPh.
        var shooter = gunId.EndsWith(":pf", StringComparison.Ordinal) ? state.Unit(gunId[..^":pf".Length])
            : state.Find(gunId) is EquipmentInstance { Holding: { } holding } ? state.Unit(holding.Holder) : null;
        if (state.Phase == "pfph" && shooter is not null && !LiveFire.IsVehicle(shooter) && (Is(shooter, Conditions.BoundingFire) || Is(shooter, Conditions.Berserk)))
        {
            return (null, [$"play.fire-barred: {shooter.Id} {GamePlanner.FireBar(state, shooter)}"]);
        }

        // C3.332, C1.55, B23.32 (pass 60, rulings R60.10, R60.11): a mortar's Area Target Type shot attacks the vehicles and the Locations of its hex;
        // Rules says where a vehicle there is still refused.
        if (shot.TargetType == OrdnanceTargetTypes.Area && state.Units.Where(unit => unit.Status is InstanceStatus.Active && LiveFire.IsVehicle(unit)
                && state.Location(unit.Id)?.Location is { } at && at.Board == target.Board && at.Hex == target.Hex)
            .Select(unit => ScenarioA1OrdnanceEligibility.AreaVehicleRefusal(unit.Id, unit.Straddling is not null, state.Phase == "mph")).FirstOrDefault(item => item is not null) is { } areaVehicle)
        {
            return (null, [areaVehicle]);
        }

        // C3.32, C3.41 (pass 60, ruling R60.8): an Infantry Target Type shot at a Location holding a vehicle attacks its Infantry and unarmored vehicles
        // and a Vulnerable AFV crew; Rules says where it is still refused. C3.31 (ruling R7.2): name a vehicle to fire at it on the Vehicle Target Type.
        if (targetVehicle is null && shot.TargetType is null
            && state.At(target).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)
                && (state.Phase != "mph" || state.Movement?.Movers.Contains(unit.Id) == true)) is { } vehicle
            && ScenarioA1OrdnanceEligibility.LocationVehicleRefusal(vehicle.Id, target.ToString(), state.Phase == "mph",
                shot.Gun!.DefinitionId == OrdnanceTargetTypes.Panzerfaust || LiveOrdnance.LatwType(shot.Gun.DefinitionId ?? string.Empty) == "atr",
                shot.Hit!.Targets is { Count: > 0 }, shot.Hit.Vehicles?.Any(item => FireReference.Value.Definitions.GetValueOrDefault(item.DefinitionId ?? string.Empty)?.Unarmored == true) == true) is { } vehicleRefusal)
        {
            return (null, [vehicleRefusal]);
        }

        if (state.At(target).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && (Is(unit, Conditions.Melee) || Is(unit, Conditions.Captured))))
        {
            return (null, ["play.fire-melee: fire at a Location holding units in Melee or prisoners is not reviewed (A11.15, A20.54)"]);
        }

        var (map, mapReason) = OrdnanceMapFacts(state, shot, target);
        if (map is null)
        {
            return (null, [mapReason!]);
        }

        // Backlog pass 16 (rulings R16.2, R16.3, R16.11 to R16.14): ordnance at a Gunflash beyond NVR is not built; the Low Visibility DRM is a Hindrance
        // of its own on the TH DR (Case R, C6.9), never cancelling FFMO or the Open Ground cases; Mud and Deep Snow cushion HE at Infantry in Open Ground;
        // Extreme Winter lowers the Gun's B#.
        if (map.Value.Shot.Hit is { BeyondNvr: true })
        {
            return (null, ["play.night-ordnance: ordnance fire at a Gunflash beyond the firer's NVR is not built (E1.81; ruling R16.2)"]);
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
            return (null, RefusalReasons.Refusal("play.ordnance-refused", "Ordnance", "shot", precheck));
        }

        return ((shot, map.Value.Facing), null);
    }

    /// <summary>A shot's proposal in words: what fires at what, the range and terrain, any turn, and the To Hit number and DRM before any roll.</summary>
    private static string OrdnanceSummary(OrdnanceShot facts, UnitFacing? facing)
    {
        // The To Hit number and DRM before any roll, read from the package with dice that decide nothing else (a miss).
        var previewed = ScenarioA1OrdnanceCalculator.Resolve(facts with
        {
            Rolls = new OrdnanceRolls([6, 6], null, null, null, null) { PanzerfaustCheck = 1 }
        }, OrdnanceReference.Value);
        var preview = previewed.ToHit;

        // C3.32, C3.4 (pass 60, rulings R60.8, R60.9): a Location hit target by target says which units and vehicles each Original DR hits; a
        // concealed one is named only as such, and an AFV for its CE crew.
        var each = previewed.TargetHits is { } judged && preview is not null
            ? "; hit target by target (C3.32, C3.4): " + string.Join(", ", judged.Select(item =>
            {
                var need = preview.ModifiedToHit - (item.FinalDr - 12 - (int)preview.Drm.Where(drm => drm.Name == "case-c4:lower-dr").Sum(drm => drm.Value));
                var unit = facts.Hit!.Targets!.FirstOrDefault(target => target.UnitId == item.UnitId);
                var vehicle = facts.Hit.Vehicles?.FirstOrDefault(target => target.VehicleId == item.UnitId);
                var name = unit is not null ? (unit.Concealed == true || unit.Hidden == true || unit.Dummy == true ? "a concealed unit" : item.UnitId)
                    : vehicle?.Concealed == true ? "a concealed vehicle"
                    : FireReference.Value.Definitions.GetValueOrDefault(vehicle?.DefinitionId ?? string.Empty)?.Unarmored == true ? item.UnitId
                    : $"the CE crew of {item.UnitId} (D.8B)";
                return need >= 2 ? $"{name} on an Original DR of {need} or less" : $"{name} only on an Original 2 (C3.6)";
            }))
            : string.Empty;
        var toHit = preview is null ? string.Empty
            : $"; Modified TH# {preview.ModifiedToHit} ({preview.Color}), DRM {(preview.Drm.Count == 0 ? "none" : string.Join(", ", preview.Drm.Select(item =>
                // C5.35 (pass 50): the lower dr is not known before the roll.
                item.Name == "case-c4:lower-dr" ? "Case C4: the lower dr of the roll doubled (C5.35)"
                // D3.44, D3.45 (pass 55): the Armor Leader's line, or the Inexperienced Crew's, with its rule.
                : ScenarioA1ArmorLeaders.IsModifier(item.Name) ? ScenarioA1ArmorLeaders.ModifierText((int)item.Value)
                : $"{ScenarioA1OrdnanceEligibility.CaseWords(item.Name)} {item.Value:+0;-0}")))}";
        var aim = facts.Gun!.DefinitionId == OrdnanceTargetTypes.Panzerfaust && facts.VehicleTarget is { } faust
            ? $"a PF (after a PF Check) at {faust.VehicleId} in {facts.TargetLocationId} (hull {faust.HullFacing} facing the firer)"
            : facts.TargetType == OrdnanceTargetTypes.Area
            ? $"HE at {facts.TargetLocationId} (Area Target Type" + (facts.Spotter is { } spotted ? $", spotted by {spotted.UnitId}" : string.Empty) + ")"
            : facts.VehicleTarget is { } aimed
            ? $"{(facts.Ammunition ?? "ap").ToUpperInvariant()} at {aimed.VehicleId} in {facts.TargetLocationId} (Vehicle Target Type; "
                + (facts.SameHex == true ? "in its own hex, so the colored dr gives the Target Facing: 1-2 rear, 3-4 side, 5-6 front, D3.2)"
                    : $"hull {aimed.HullFacing}" + (aimed.TurretFacing is { } turretFacing ? $", turret {turretFacing}" : string.Empty) + " facing the firer)")
            // C8.31 (pass 60): AP, APCR, APDS, HEAT, and a LATW's round at Infantry or a Gun, by their HE Equivalency.
            : ScenarioA1OrdnanceCalculator.HeEquivalency(facts, OrdnanceReference.Value) is { } equivalency
            ? (facts.Gun!.DefinitionId == OrdnanceTargetTypes.Panzerfaust ? "a PF (after a PF Check)" : ScenarioA1Definitions.AmmunitionName(facts.Ammunition!))
                + $" at {facts.TargetLocationId} ({equivalency} FP by HE Equivalency, C8.31)"
            : $"HE at {facts.TargetLocationId}";

        // C8.9 (pass 55, ruling R55.7): the Depletion Number the declared ammunition is fired under, and what an Original DR above it means.
        var depletion = ScenarioA1Definitions.DepletionWords(facts) is { } depletionWords
            ? $"; {facts.Ammunition!.ToUpperInvariant()} by its {depletionWords}: on an Original To Hit DR above it the Gun had none, has not fired, and has none for the rest of the scenario (C8.9)"
            : string.Empty;
        return $"play.ordnance: {facts.Gun!.GunId} fires {aim} in the {facts.Phase}; range {facts.Range}, {facts.Hit!.TargetTerrain}"
            + (facts.HexspinesToTurn > 0 ? $", turning {(facts.Vehicle is null ? string.Empty : "its turret ")}{facts.HexspinesToTurn} hexspine(s) to {facing!.Value.Name()}" : string.Empty)
            + toHit + each + depletion;
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
            // D3.2, C5.5 (pass 50): a Gun, or a tank's MA, also fires at a vehicle in its own Location; Infantry there is still a Gun's target alone.
            if ((firer is not EquipmentInstance && shot.VehicleTarget is null) || ReadLocation(state, target) is not { } ownRead || TerrainKey(ownRead) is not { } ownTerrain)
            {
                return (null, "play.ordnance-own-hex: within its own Location a Gun fires at Infantry or a vehicle, and a tank's MA at a vehicle (C5.5, D3.2)");
            }

            if (ScenarioA1OrdnanceMapRules.SameHexBypassRefusal(firer is UnitInstance { Straddling: not null },
                shot.VehicleTarget is { VehicleId: { } aimedAt } && state.Unit(aimedAt) is { Straddling: not null }) is { } bypassBar)
            {
                return (null, bypassBar);
            }

            // C5.5: a wreck, SMOKE, or an AFV in the hex adds its Hindrance (Case R), "other than the target or firer".
            var ownHindrance = ScenarioA1VehicleSightRules.OwnHexHindrance(
                SmokeSources(state).Count(place => place.Board == target.Board && place.Hex == target.Hex),
                state.Units.Any(unit => unit.Id != shot.VehicleTarget?.VehicleId && unit.Id != firer.Id
                    && (unit.Status == InstanceStatus.Active ? IsAfv(unit) : unit.Status == InstanceStatus.Wrecked && !IsBurning(state, unit))
                    && state.Location(unit.Id)?.Location == target && Standing(state, unit)));
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
                    Los = new FireLos(false, ownHindrance, true, false),
                    TargetTerrain = ownTerrain,
                    Targets = [.. shot.Hit.Targets!.Select(item => item with { KnownEnemyInLos = true, Captors = item.Captors ?? [] })],
                },
            }, null), null);
        }

        // C2.6 (pass 55): a Gun, or a vehicle's MA, fires at no other level of its own hex.
        if (ScenarioA1OrdnanceMapRules.OwnHexLevelRefusal(firer.Id, target.Board == from.Board && target.Hex == from.Hex, false) is { } ownHexLevel)
        {
            return (null, ownHexLevel);
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

        // C5.13 (pass 50): a Bounding First Firer fires within its current CA; its turret turns with an MP expenditure (D3.12).
        if (ScenarioA1OrdnanceMapRules.BoundingArcRefusal(tank?.Id, shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire, turns.Steps) is { } boundingArc)
        {
            return (null, boundingArc);
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

        // C2.6, B11.31, B11.32 (pass 55): the target's elevation less the Gun's, for the range a shot at another level needs, and the cliff
        // hexside no ordnance fires through to an adjacent Location.
        var definition = OrdnanceReference.Value.Guns.GetValueOrDefault(shot.Gun.DefinitionId!);
        var targetAbove = ReadLocation(state, target) is { } targetRead ? targetRead.Hex.BaseLevel + targetRead.Level.Level - (gunRead.Hex.BaseLevel + gunRead.Level.Level) : 0;
        if (ScenarioA1OrdnanceMapRules.CliffRefusal(CliffBetween(state, from, target), read.Range, targetAbove, definition?.GunType, definition?.LatwType,
            definition?.RangeMinimum) is { } cliff)
        {
            return (null, cliff);
        }

        // C2.6 (the table player's review of pass 55): the refusal says the levels and the range, which the map alone knows.
        if (ScenarioA1OrdnanceMapRules.ElevationRefusal(shot.Gun.GunId ?? "the Gun", targetAbove, read.Range, definition?.GunType) is { } elevationBar)
        {
            return (null, elevationBar);
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
            // C2.6 (ruling R8.7; pass 55): a Gun fires at another level, up or down, only if the range is at least the elevation difference.
            ElevationAllowed = ScenarioA1OrdnanceMapRules.ElevationAllowed(targetAbove, read.Range, definition?.GunType),

            // B10.31 (pass 55): the Height Advantage the probe read, for Case Q. C3.71 (pass 60, ruling R60.14): a Critical Hit reverses it, for a shot
            // made since the pass.
            HeightAdvantage = ScenarioA1OrdnanceMapRules.HeightAdvantage(read.HeightAdvantage, read.HexsideTem?.Tem),
            ReversesHeightAdvantage = ScenarioA1OrdnanceMapRules.HeightAdvantage(read.HeightAdvantage, read.HexsideTem?.Tem) == true ? true : null,

            // C8.31 (pass 60, ruling R60.4): a wall's TEM lets HEAT be fired at Infantry; read for a HEAT shot with no vehicle named.
            WallTem = ScenarioA1OrdnanceMapRules.HeatWallTem(shot.VehicleTarget is not null, shot.Ammunition, read.HexsideTem?.Terrain),
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

        // C2.6, B11.31, B11.32 (pass 55): a LATW is free of the elevation limit and a mortar is held to it firing down; Rules says which, and
        // which of them fires through a cliff hexside to an adjacent Location.
        var definition = OrdnanceReference.Value.Guns.GetValueOrDefault(shot.Gun!.DefinitionId ?? string.Empty);
        var targetAbove = ReadLocation(state, target) is { } targetRead ? targetRead.Hex.BaseLevel + targetRead.Level.Level - (firerRead.Hex.BaseLevel + firerRead.Level.Level) : 0;
        if (ScenarioA1OrdnanceMapRules.CliffRefusal(CliffBetween(state, from, target), ranged.Range, targetAbove, definition?.GunType, definition?.LatwType,
            definition?.RangeMinimum) is { } cliff)
        {
            return (null, cliff);
        }

        // C13.8 (p. 185): a PF or PSK fires at no target two or more levels higher in an adjacent hex, nor directly above it in its own hex.
        if (ScenarioA1OrdnanceMapRules.BackblastElevationRefusal(shot.Crew?.UnitId ?? shot.Gun.GunId ?? "the firer", latwType, ranged.Range,
            target.Board == from.Board && target.Hex == from.Hex, targetAbove) is { } backblast)
        {
            return (null, backblast);
        }

        if (ScenarioA1OrdnanceMapRules.ElevationRefusal(shot.Gun.GunId ?? "the weapon", targetAbove, ranged.Range, definition?.GunType) is { } elevationBar)
        {
            return (null, elevationBar);
        }

        var revealing = HeatOfBattleFacts(state, read);
        var area = shot.TargetType == OrdnanceTargetTypes.Area;
        var (levels, levelsReason) = area ? AreaLocations(state, shot, target, probe, read) : (null, null);
        if (levelsReason is not null)
        {
            return (null, levelsReason);
        }

        var heightAdvantage = ScenarioA1OrdnanceMapRules.HeightAdvantage(read.HeightAdvantage, read.HexsideTem?.Tem, area);
        return ((shot with
        {
            Range = ranged.Range,
            HexspinesToTurn = 0,
            FirerInWoodsOrBuilding = WoodsOrBuilding.Contains(firerTerrain),
            ElevationAllowed = ScenarioA1OrdnanceMapRules.ElevationAllowed(targetAbove, ranged.Range, definition?.GunType),

            // B10.31, C6.8 (pass 55): Height Advantage is Case Q on a LATW's To Hit DR; the Area Target Type takes no Case Q. C3.71 (pass 60, ruling
            // R60.14): a Critical Hit reverses it, for a shot made since the pass.
            HeightAdvantage = heightAdvantage,
            ReversesHeightAdvantage = heightAdvantage == true ? true : null,

            // B23.32 (pass 60, ruling R60.11): the Locations of a mortar's building hex.
            AreaLocations = levels,

            // C8.31 (pass 60, ruling R60.5): a wall's TEM lets a PF's or PSK's HEAT be fired at Infantry.
            WallTem = ScenarioA1OrdnanceMapRules.HeatWallTem(shot.VehicleTarget is not null, shot.Ammunition, read.HexsideTem?.Terrain),
            Hit = shot.Hit! with
            {
                Range = null,
                SameLevel = read.SameLevel,
                Los = read.Los,
                LowVisibilityDrm = read.LowVisibilityDrm,
                BeyondNvr = read.BeyondNvr,
                TargetTerrain = read.TargetTerrain,
                Targets = revealing.Targets,

                // D9.3, C9.5 EX (pass 60, ruling R60.10): a mortar's target Infantry keep the +1 of a friendly AFV or wreck beside them.
                AfvCover = area ? read.AfvCover : null,
            },
            VehicleTarget = aimed ?? shot.VehicleTarget,
            Panzerfaust = shot.Panzerfaust is null ? null : shot.Panzerfaust with
            {
                FromBuilding = ScenarioA1OrdnanceMapRules.IsBackblastLocation(firerTerrain)
            },
        }, null), null);
    }

    /// <summary>
    /// The Locations of a mortar's target hex in a building of more than one level (B23.32, C3.4; pass 60, ruling R60.11): the named one and each
    /// other that holds a unit or vehicle, lowest first, with its terrain and LOS read from the firer or Spotter and the building levels above it.
    /// Null for any other hex. The board data does not tell an Interior Building Hex, and has no rooftop: every building hex is taken as not Interior.
    /// </summary>
    private (IReadOnlyList<OrdnanceAreaLocation>? Locations, string? Reason) AreaLocations(GameState state, OrdnanceShot shot, BoardLocation target, FireAttack probe,
        FireAttack read)
    {
        if (!ScenarioA1Definitions.IsBuildingTerrain(read.TargetTerrain) || ReadLocation(state, target) is not { Hex.Locations.Count: > 1 } targetRead)
        {
            return (null, null);
        }

        int LevelsAbove(int level) => targetRead.Hex.Locations.Count(item => item.Level > level
            && ReadLocation(state, target with { Level = item.Level }) is { } above && IsBuildingTerrain(TerrainKey(above)));
        BoardLocation[] places = [.. shot.Hit!.Targets!.Select(item => item.LocationId).Concat((shot.Hit.Vehicles ?? []).Select(item => item.LocationId))
            .OfType<string>().Select(BoardLocation.Parse).Append(target).Distinct().OrderBy(item => item.Level)];
        var locations = new List<OrdnanceAreaLocation>();
        foreach (var place in places)
        {
            var placeRead = read;
            if (place != target)
            {
                var (other, reason) = FireMapFacts(state, probe, place);
                if (other is null)
                {
                    return (null, reason);
                }

                placeRead = other;
            }

            var above = LevelsAbove(place.Level);
            locations.Add(new OrdnanceAreaLocation(place.ToString(), placeRead.TargetTerrain, placeRead.Los, above > 0 ? above : null));
        }

        return (locations, null);
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
                        MpInLos = MpInLos(state, from, moving),
                        InBogHex = ScenarioA1OrdnanceMapRules.InBogHex([.. ThisPhase(store.Read(state.Scope)?.Events ?? []).Select(item => item.Payload).OfType<VehicleStepped>()
                            .Where(step => step.Vehicle == moving).Select(step => (step.Kind, step.BogRemoval))],
                            state.Find(moving) is UnitInstance bogged && Is(bogged, Conditions.Bogged)),
                    }
                    : movement with
                    {
                        OpenGround = ScenarioA1OrdnanceMapRules.OpenGround(shot.Hit!.TargetTerrain, shot.Hit.Los?.HindranceDrm),
                        // C6.6, A4.62 (rulings R10.8, R40.10): a crew that pushed its Gun this Player Turn is under Hazardous Movement.
                        Hazardous = ScenarioA1FireMapRules.HazardousMovement(state.HazardousMovers, shot.Hit),

                        // A4.63, C6.1 (pass 40): a Dashing stack in its road Location, to this firer.
                        Dashing = DashAgainst(state, [from], target),
                    },
            };
        }
        else if (shot.VehicleTarget is null && shot.Hit is { } aimed && ScenarioA1FireMapRules.HazardousMovement(state.HazardousMovers, aimed) == true)
        {
            // C6.6, A4.62 (pass 40, ruling R40.10): Case O outside the MPh too, "regardless of fire phase".
            shot = shot with
            {
                HazardousTarget = true,
            };
        }

        // C11.4, C11.5 (pass 60, ruling R60.14): a mortar's hit on a Gun's crew alone, its gunshield facing the mortar's own Location.
        if (shot.VehicleTarget is null && GunAt(state, target, side, [from]) is { } gunTarget)
        {
            shot = shot with
            {
                Hit = shot.Hit! with
                {
                    GunTarget = gunTarget
                }
            };
        }

        // C5.31 to C5.33 (pass 50): the MP a Bounding First Firer has spent in a continuous LOS to its target; a LOS is the same both ways.
        if (shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire && shot.Vehicle is { } boundingFirer)
        {
            shot = shot with
            {
                Vehicle = boundingFirer with
                {
                    MpInLos = MpInLos(state, target, shot.Gun.GunId!)
                },
            };
        }

        // C6.43 (ruling R8.8): the Bore Sighting counts while its original crew fires the Gun from its setup Location.
        var boreSighted = LiveOrdnance.BoreSighted(state, shot.Gun.GunId!, shot.Crew!.UnitId!, from, target);
        return shot with
        {
            BoreSighted = boreSighted ? true : null,
            CrewSeen = shot.Crew!.Concealed == true && shot.Vehicle is null ? EnemyGoodOrderInLosWithin16(state, side, from) : null,
            FirerOverstack = LiveStacking.Of(state, from, side).AttackPenalty is var over and > 0 ? over : (int?)null,
            TargetOverstack = shot.VehicleTarget is null && state.Sides.FirstOrDefault(item => item.Id != side)?.Id is { } enemy && LiveStacking.Of(state, target, enemy, knownOnly: true).DefensePenalty is var crowded and > 0
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

    /// <summary>Whether the hexside two adjacent hexes share is a cliff (B11.1), read from either hex; false when they are not adjacent.</summary>
    private bool CliffBetween(GameState state, BoardLocation one, BoardLocation two) =>
        (SideToward(state, one, two) is { } side && HexsideAt(state, one, side)?.Cliff == true)
        || (SideToward(state, two, one) is { } back && HexsideAt(state, two, back)?.Cliff == true);

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
    /// <paramref name="owedShot"/> is the declared shot a Gun Duel's first shot owes its DEFENDER (C2.2401; pass 55): a shot that stops at a
    /// choice carries it in its resume data, so the answer resolves the declared shot after it.
    /// </summary>
    private void AddOrdnanceEvents(GameScope scope, string attemptId, long expected, string actor, GameState state, OrdnanceShot facts, UnitFacing? facing,
        List<GameEvent> events, Func<RollRequest, RollResult> draw, ResumedRolls? resumed = null, JsonElement? owedShot = null)
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
                    // D3.41 (pass 55, the referee's review): no Morale Level of a crew whose Armor Leader is not yet revealed; the answer reads it again.
                    ["facts"] = JsonNode.Parse(JsonSerializer.Serialize(ScenarioA1ArmorLeaders.Pending(facts, UnrevealedLeaders(state)), LiveFire.Json)),
                    ["rolls"] = RollNode(rollIds),
                };
                if (facing is { } turned)
                {
                    resume["facing"] = UnitFacings.Name(turned);
                }

                if (owedShot is { } owed)
                {
                    resume[GunDuelOwed] = JsonNode.Parse(owed.GetRawText());
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

                // D.8A (pass 60): the check a Specific Collateral Attack calls on the crew to take.
                "collateralCheck" => (2, "ordnance-collateral-check"),

                // C8.4 (pass 55): a Canister shot's one DR, on the IFT.
                "canister" => (2, "ordnance-canister"),

                // C1.55, D5.6 (pass 60): the Crew Survival DR of an AFV a mortar's hit destroyed.
                _ when key.StartsWith("areaSurvival:", StringComparison.Ordinal) => (2, "ordnance-crew-survival"),
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
        // D3.41 (pass 55, ruling R55.15): the record keeps a target crew's Morale Level only when the shot made that crew check against it.
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "ordnance-fired",
            new OrdnanceFired(facts.Gun!.GunId!, facts.Crew!.UnitId!, BoardLocation.Parse(facts.TargetLocationId!), facing, gunResult.RateOfFireKept,
                gunResult.Acquisition, acquired, rollIds, JsonSerializer.SerializeToElement(ScenarioA1ArmorLeaders.Recorded(facts, resolution), LiveFire.Json),
                JsonSerializer.SerializeToElement(resolution, LiveFire.Json)),
            package, null));

        // C13.31, C13.36 (ruling R9.7): a PF Check's Original 6 pins or breaks its firer, or gives Casualty Reduction; so does an Original 12 To Hit DR.
        if (resolution.FirerEffect is { } firerEffect && state.Unit(facts.Crew.UnitId!) is { } shooter)
        {
            var phaseMarker = ConditionName(ScenarioA1OrdnanceEventRules.FirerPhaseMarker(facts.Phase));
            // A17.11 (pass 35, task 35.1): a SMC firer's Casualty Reduction is a wound, and its Wound Severity dr is made at once.
            int? severity = null;
            if (firerEffect != OrdnancePanzerfaustCheck.Pinned && firerEffect != OrdnancePanzerfaustCheck.Broken && WoundSeverityDue(shooter))
            {
                var wound = draw(new RollRequest(1, 6));
                severity = wound.Values[0];
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled($"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}", ScenarioA1Wounds.SeverityPurpose, 1, 6,
                        wound.Values, DiceRolled.SystemSource, actor), package, null, [recordId]));
            }

            foreach (var (type, payload) in FirerEffectEvents(shooter, firerEffect, attemptId, phaseMarker, severity))
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
        // C1.55, C9.1 (pass 60, ruling R60.10): what a mortar's hit did to each AFV it hit, as a To Kill result is written, its Shock automatic.
        foreach (var afv in resolution.AreaVehicles ?? [])
        {
            if (state.Unit(afv.VehicleId) is { } armored)
            {
                foreach (var (type, payload) in AreaVehicleEvents(state, armored, afv, attemptId))
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, [recordId]));
                }
            }
        }

        // B23.32 (pass 60, ruling R60.11): the attacks on the other Locations of a mortar's building hex.
        var effects = new List<FireUnitEffect>();
        foreach (var attack in new[] { resolution.CriticalHit, resolution.Hit }.OfType<FireResolution>().Concat(resolution.LevelHits ?? []))
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

            // C3.32, A7.308, D.8B (pass 60, ruling R60.8): the unarmored vehicles the hit struck, on the Vehicle line, and the Vulnerable crews of the
            // AFVs whose TH# it reached, as an IFT attack's are.
            foreach (var vehicle in attack.VehicleEffects ?? [])
            {
                VehicleEffectsAfterHit(state, vehicle, nestedFacts, scope, attemptId, expected, package, recordId, events);
            }

            effects.AddRange(attack.Effects);
        }

        // D.8A (pass 60, ruling R60.7): the Specific Collateral Attack on the crew of the AFV the hit left standing.
        if (resolution.SpecificCollateral is { } collateral)
        {
            VehicleEffectsAfterHit(state, collateral.Effect, facts.Hit!, scope, attemptId, expected, package, recordId, events);
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
            // C8.4 (pass 55): a Canister shot's IFT DR stands in for the To Hit DR it does not make.
            && (rolls.ToHit ?? rolls.Canister) is [var colored, ..] && state.Location(firingGun.Id) is { } firingAt)
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
        // C6.51 (pass 35, task 35.11): a vehicle fired at on the Vehicle Target Type carries the counter as Infantry does.
        var acquiredVehicle = ScenarioA1ResultTables.AcquiredVehicle(facts.VehicleTarget?.VehicleId, facts.VehicleTarget?.Concealed == true, resolution.ToHit?.Hit == true,
            resolution.Kill is { } killed && ScenarioA1OrdnanceEventRules.Wrecks(killed.Result));
        if (acquired is { } acquiredLocation && facts.TargetType is null
            && (acquiredVehicle is null ? AcquiredUnits(facts, effects, attemptId) : [acquiredVehicle]) is { Count: > 0 } units)
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
    /// The events of a mortar's hit on an AFV (C1.55; pass 60, ruling R60.10), written as a To Kill result's are: a burning wreck, a wreck its crew may
    /// survive, an Immobilization, or a Shock, which is never a possible one. A result of none writes nothing, and the AFV keeps any "?".
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> AreaVehicleEvents(GameState state, UnitInstance vehicle, OrdnanceAreaVehicle hit, string attemptId) =>
        hit.Result == OrdnanceKill.None ? []
            : KillEvents(state, vehicle, new OrdnanceKill("he", hit.HitLocation, "none", null, hit.Drm, null, null, hit.KillDr, hit.Dice, hit.FinalDr, hit.Result)
            {
                Shocked = hit.Result == OrdnanceKill.Shock ? true : null,
                CrewSurvival = hit.CrewSurvival,
            }, attemptId);

    /// <summary>
    /// The events of what an ordnance hit did to a vehicle or its crew (pass 60, rulings R60.7, R60.8), as an IFT attack's are written: the Vehicle
    /// line's result or the crew's, and a Recalled AFV on its way off the map that is immobilized Abandoned by its crew (D5.341, D5.41).
    /// </summary>
    private void VehicleEffectsAfterHit(GameState state, FireVehicleEffect vehicle, FireAttack facts, GameScope scope, string attemptId, long expected, string package,
        string recordId, List<GameEvent> events)
    {
        foreach (var payload in VehicleEffectEvents(state, vehicle, facts))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, payload.Type, payload.Payload, package, null, [recordId]));
        }

        if (state.Unit(vehicle.VehicleId) is { } stuck && ScenarioA1FireFollowUps.AbandonsWhenImmobilized(vehicle, MustLeave(stuck)))
        {
            foreach (var (type, abandon) in AbandonEvents(stuck, attemptId))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, abandon, package, null, [recordId]));
            }
        }
    }

    /// <summary>
    /// What a PF does to its own firer (C13.31, C13.36; ruling R9.7): pinned; broken; or Casualty Reduction: a squad becomes its HS, a HS or crew
    /// is eliminated, a SMC is wounded, or eliminated if already wounded (A7.302, A17.2).
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> FirerEffectEvents(UnitInstance unit, string effect, string attemptId, string marker, int? severityDr)
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
            GameState.Condition(unit, Conditions.Wounded) == ConditionState.True, severityDr);
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
            "collateralCheck" => rolls with { CollateralCheck = values },
            "canister" => rolls with { Canister = values },

            // C3.74 (pass 60): a mortar's Critical Hit picks a Location of its building hex, then a unit there; C1.55, D5.6: an AFV's Crew Survival DR.
            _ when selected.Length > 0 => rolls with
            {
                CriticalSelection = new Dictionary<string, int>(selected.Select((id, index) => KeyValuePair.Create(id, values[index]))
                    .Concat((rolls.CriticalSelection ?? new Dictionary<string, int>()).Where(item => !selected.Contains(item.Key))), StringComparer.Ordinal)
            },
            _ when key.StartsWith("areaSurvival:", StringComparison.Ordinal) => rolls with
            {
                AreaSurvival = new Dictionary<string, IReadOnlyList<int>>(rolls.AreaSurvival ?? new Dictionary<string, IReadOnlyList<int>>(), StringComparer.Ordinal)
                {
                    [key["areaSurvival:".Length..]] = values
                }
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
            "cowerSelection" => (unit.Split(',').Length, "fire-cower-selection"),
            "checks" => (2, "fire-check"),
            "leaderLoss" => (2, "fire-leader-loss"),
            "heatOfBattle" => (2, "fire-heat-of-battle"),
            "berserkCheck" => (2, "fire-check"),

            // Pass 60 (ruling R60.8): a vehicle crew's check and the Unlikely Kill dr of a hit on a Location holding vehicles.
            "crewCheck" => (2, "fire-crew-check"),
            "unlikelyKill" => (1, "fire-unlikely-kill"),
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
            "cowerSelection" => rolls with { CowerSelection = Selection(rolls.CowerSelection) },
            "checks" => rolls with { Checks = Add(rolls.Checks, unit, values) },
            "leaderLoss" => rolls with { LeaderLoss = Add(rolls.LeaderLoss, unit, values) },
            "heatOfBattle" => rolls with { HeatOfBattle = Add(rolls.HeatOfBattle, unit, values) },
            "berserkCheck" => rolls with { BerserkChecks = Add(rolls.BerserkChecks, unit, values) },
            "crewCheck" => rolls with { CrewChecks = Add(rolls.CrewChecks, unit, values) },
            "unlikelyKill" => rolls with { UnlikelyKill = Add(rolls.UnlikelyKill, unit, values[0]) },
            "molCheck" => rolls with { MolCheck = values[0] },

            // Pass 60 (rulings R60.15, R60.16): a DC's Position DR and the checks of a named AFV's crew.
            "position" => rolls with { Position = values },
            "crewSurvival" => rolls with { CrewSurvival = values },
            "taskCheck" => rolls with { TaskCheck = values },
            "collateralCheck" => rolls with { CollateralCheck = values },
            _ => rolls with { WoundSeverity = Add(rolls.WoundSeverity, unit, values[0]) },
        };
    }
}
