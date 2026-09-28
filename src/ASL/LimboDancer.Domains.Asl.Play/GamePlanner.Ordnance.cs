using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;
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

    private static readonly string[] WoodsOrBuilding = ["woods", "wooden-building", "stone-building"];

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
        var (shot, reason) = LiveOrdnance.FromState(state, gunId, target, targetVehicle, ammunition);
        if (shot is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        // Ruling R25.10: the hit's IFT attack on a vehicle in the target Location is not reviewed; C3.31 (ruling R7.2): name the vehicle to
        // fire at it on the Vehicle Target Type.
        if (targetVehicle is null
            && state.At(target).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)) is { } vehicle)
        {
            return Refused(scope, label, expected, $"play.ordnance-vehicle: {vehicle.Id} is in {target}; fire at it on the Vehicle Target Type by naming it, "
                + "since a hit's attack on the Location's Infantry and vehicles together is not reviewed (C3.31; rulings R25.10, R7.2)");
        }

        if (state.At(target).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && (Is(unit, Conditions.Melee) || Is(unit, Conditions.Captured))))
        {
            return Refused(scope, label, expected, "play.fire-melee: fire at a Location holding units in Melee or prisoners is not reviewed (A11.15, A20.54)");
        }

        // A5.12, A5.131: the To Hit DRM of an overstacked firer or target are not reviewed; a crew counts as a HS (A5.1).
        if (new[] { FirerLocation(state, gunId), target }.Any(location => state.Sides.Any(side => Overstacked(
            state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side.Id && !Is(unit, Conditions.Captured))))))
        {
            return Refused(scope, label, expected, "play.ordnance-overstacked: the Gun's or the target's Location is overstacked, whose To Hit DRM are not reviewed (A5.12, A5.131)");
        }

        var (map, mapReason) = OrdnanceMapFacts(state, shot, target);
        if (map is null)
        {
            return Refused(scope, label, expected, mapReason!);
        }

        shot = map.Value.Shot;
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
            Rolls = new OrdnanceRolls([6, 6], null, null, null, null)
        }, reference).ToHit;
        var toHit = preview is null ? string.Empty
            : $"; Modified TH# {preview.ModifiedToHit} ({preview.Color}), DRM {(preview.Drm.Count == 0 ? "none" : string.Join(", ", preview.Drm.Select(item => $"{item.Name} {item.Value:+0;-0}")))}";
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            AddOrdnanceEvents(scope, attemptId, expected, actor, state, facts, facing, events, draw);
            return events;
        }

        var aim = facts.VehicleTarget is { } aimed
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
    /// boundary rows included. A Covered Arc is read on one unreversed board only.
    /// </summary>
    private ((OrdnanceShot Shot, UnitFacing? Facing)? Facts, string? Reason) OrdnanceMapFacts(GameState state, OrdnanceShot shot, BoardLocation target)
    {
        // C3.2, D3.12: a Gun's barrel, or a turreted AFV's TCA (ruling R7.10); a non-turreted MA would pivot the vehicle, which is not built.
        var firer = state.Find(shot.Gun!.GunId!)!;
        var from = FirerLocation(state, firer.Id);
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
            return (null, "play.ordnance-arc: the Covered Arc is read only on one board not reversed (C3.2)");
        }

        // C3.2: the Covered Arc is the 60-degree wedge on the barrel's hexspine; C5.1: the fewest hexspines turned brings the target in.
        static double Off(double one, double two) => Math.Abs(((one - two) % 360 + 540) % 360 - 180);
        var turns = Enumerable.Range(0, 6).Select(step => (Facing: (UnitFacing)(((int)facing + step) % 6), Steps: Math.Min(step, 6 - step)))
            .Where(item => Off(bearing, (int)item.Facing * 60) <= 30 + 1e-6).OrderBy(item => item.Steps).First();
        if (tank is not null && !turreted && turns.Steps > 0)
        {
            return (null, $"play.ordnance-vca: {tank.Id}'s MA is not in a turret, and pivoting the vehicle to fire is not reviewed (C5.11)");
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

            static string Facing(double off) => off <= 60 + 1e-6 ? "front" : off <= 120 + 1e-6 ? "side" : "rear";
            var targetTurreted = OrdnanceReference.Value.Armor.Vehicles.GetValueOrDefault(vehicleTarget.DefinitionId!)?.Turreted == true;
            aimed = vehicleTarget with
            {
                HullFacing = Facing(Off(back, (int)hull * 60)),
                TurretFacing = targetTurreted && LiveOrdnance.TurretFacing(state, targetVehicle) is { } tca ? Facing(Off(back, (int)tca * 60)) : null,
            };
        }

        // A12.14, A15.44: a concealed crew that firing reveals is Known when a target's Heat of Battle result is read.
        var revealing = HeatOfBattleFacts(state, read);
        var hit = shot.Hit! with
        {
            Range = null,
            SameLevel = read.SameLevel,
            Los = read.Los,
            TargetTerrain = read.TargetTerrain,
            Targets = revealing.Targets,
        };
        var withMap = shot with
        {
            Range = read.Range,
            HexspinesToTurn = turns.Steps,
            FirerInWoodsOrBuilding = WoodsOrBuilding.Contains(gunTerrain),
            // C2.6: at the same level the limit never applies; other levels are undecided in the package.
            ElevationAllowed = true,
            Hit = hit,
            VehicleTarget = aimed ?? shot.VehicleTarget,
        };
        return ((withMap, turns.Steps > 0 ? turns.Facing : null), null);
    }

    /// <summary>Where a Gun or a tank is.</summary>
    private static BoardLocation FirerLocation(GameState state, string id) => state.Find(id) switch
    {
        EquipmentInstance { Position: MapPosition gunAt } => gunAt.Location,
        UnitInstance { Position: MapPosition tankAt } => tankAt.Location,
        _ => throw new InvalidOperationException($"'{id}' is not on the map."),
    };

    /// <summary>The bearing from one Location to another in degrees counterclockwise from east, on one unreversed board; null otherwise.</summary>
    private double? Bearing(GameState state, BoardLocation from, BoardLocation to)
    {
        if (from.Board != to.Board || state.Map.Board(from.Board) is not { } placed || placed.Slot is { Reversed: true }
            || boards.TryGetBoard(from.Board, placed.Version).Board is not { } handle
            || !handle.Geometry.TryGetIndex(from.Hex, out var one) || !handle.Geometry.TryGetIndex(to.Hex, out var two))
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

        var gunResult = resolution.Gun!;
        var recordId = EventId(attemptId, events.Count + 1);
        BoardLocation? acquired = gunResult.AcquiredLocationId is { } acquiredAt ? BoardLocation.Parse(acquiredAt) : null;
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "ordnance-fired",
            new OrdnanceFired(facts.Gun!.GunId!, facts.Crew!.UnitId!, BoardLocation.Parse(facts.TargetLocationId!), facing, gunResult.RateOfFireKept,
                gunResult.Acquisition, acquired, rollIds, JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json)),
            package, null));

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

        // C2.28: the malfunction; A7.1, C2.24: the Gun and its crew carry the fire phase's marker from their first shot.
        var marker = facts.Phase == "DFPh" ? Conditions.FinalFire : Conditions.PrepFire;
        var gunConditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        if (gunResult.Malfunctioned)
        {
            gunConditions[Conditions.Malfunctioned] = ConditionState.True;
        }

        if (state.Find(facts.Gun.GunId!) is { } gun && GameState.Condition(gun, marker) != ConditionState.True)
        {
            gunConditions[marker] = ConditionState.True;
        }

        // A12.14: a concealed tank that fires loses its "?"; it is its own crew.
        if (facts.Crew.UnitId == facts.Gun.GunId && resolution.CrewConcealmentLost == true)
        {
            gunConditions[Conditions.Concealed] = ConditionState.False;
            gunConditions[Conditions.Hidden] = ConditionState.False;
        }

        if (gunConditions.Count > 0)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(facts.Gun.GunId!, gunConditions), package, null, [recordId]));
        }

        // A12.14: a concealed crew that fires loses its "?"; a tank is its own crew and carries the marker once.
        if (facts.Crew.UnitId != facts.Gun.GunId && state.Unit(facts.Crew.UnitId!) is { } crew)
        {
            var crewConditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
            if (GameState.Condition(crew, marker) != ConditionState.True)
            {
                crewConditions[marker] = ConditionState.True;
            }

            if (resolution.CrewConcealmentLost == true)
            {
                crewConditions[Conditions.Concealed] = ConditionState.False;
            }

            if (crewConditions.Count > 0)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(crew.Id, crewConditions), package, null, [recordId]));
            }
        }

        // C6.5, C6.51 (ruling R5.13): the Acquisition is on the Known units the shot leaves in its target Location.
        if (acquired is { } acquiredLocation && AcquiredUnits(facts, effects, attemptId) is { Count: > 0 } units)
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
        if (kill.Result is OrdnanceKill.Burn or OrdnanceKill.Eliminated)
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

        var changed = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        if (Is(vehicle, Conditions.Concealed) || Is(vehicle, Conditions.Hidden))
        {
            changed[Conditions.Concealed] = ConditionState.False;
            changed[Conditions.Hidden] = ConditionState.False;
        }

        if (kill.Result == OrdnanceKill.Immobilized)
        {
            changed[Conditions.Immobilized] = ConditionState.True;
            changed[Conditions.Motion] = ConditionState.False;
        }

        if (kill.Shocked == true)
        {
            changed[Conditions.Shocked] = ConditionState.True;
            changed[Conditions.UnconfirmedKill] = ConditionState.False;
            changed[Conditions.ButtonedUp] = ConditionState.True;
            changed[Conditions.Motion] = ConditionState.False;
        }

        if (changed.Count > 0)
        {
            yield return ("conditions-changed", new ConditionsChanged(vehicle.Id, changed));
        }

        if (kill.Abandoned == true)
        {
            yield return ("conditions-changed", new ConditionsChanged(vehicle.Id, new Dictionary<string, ConditionState>(StringComparer.Ordinal)
            {
                [Conditions.Abandoned] = ConditionState.True,
                [Conditions.Motion] = ConditionState.False,
            }));
            if (CrewCounter(vehicle, attemptId) is { } crew)
            {
                yield return ("instance-created", crew);
            }
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
            _ => rolls with { WoundSeverity = Add(rolls.WoundSeverity, unit, values[0]) },
        };
    }
}
