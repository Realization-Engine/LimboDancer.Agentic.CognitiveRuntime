using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// A Gun's HE shot at Infantry (unit step 24): the state's facts, the planner's map reads (range, LOS, terrain, and the hexspines
/// the Gun must turn to bring the target into its Covered Arc, C3.2), the Ordnance package's pre-check, and the rolls it asks for.
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

        var (shot, reason) = LiveOrdnance.FromState(state, gunId, target);
        if (shot is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        if (state.At(target).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && (Is(unit, Conditions.Melee) || Is(unit, Conditions.Captured))))
        {
            return Refused(scope, label, expected, "play.fire-melee: fire at a Location holding units in Melee or prisoners is not reviewed (A11.15, A20.54)");
        }

        // A5.12, A5.131: the To Hit DRM of an overstacked firer or target are not reviewed; a crew counts as a HS (A5.1).
        if (new[] { ((MapPosition)((EquipmentInstance)state.Find(gunId)!).Position).Location, target }.Any(location => state.Sides.Any(side => Overstacked(
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

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.ordnance: {facts.Gun!.GunId} fires HE at {facts.TargetLocationId} in the {facts.Phase}; range {facts.Range}, {facts.Hit!.TargetTerrain}"
                + (facts.HexspinesToTurn > 0 ? $", turning {facts.HexspinesToTurn} hexspine(s) to {facing!.Value.Name()}" : string.Empty)
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
        var gun = (EquipmentInstance)state.Find(shot.Gun!.GunId!)!;
        var position = (MapPosition)gun.Position;
        if (position.Facing is not { } facing)
        {
            return (null, $"play.ordnance-facing: '{gun.Id}' has no facing, so its Covered Arc is unknown (C3.2)");
        }

        var from = position.Location;
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
        };
        return ((withMap, turns.Steps > 0 ? turns.Facing : null), null);
    }

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
        List<GameEvent> events, Func<RollRequest, RollResult> draw)
    {
        var reference = OrdnanceReference.Value;
        var package = ScenarioA1OrdnancePackage.Identity.ToString();
        var rollIds = new Dictionary<string, string>(StringComparer.Ordinal);
        var rolls = new OrdnanceRolls(null, null, null, null, null);
        OrdnanceResolution resolution;
        while ((resolution = ScenarioA1OrdnanceCalculator.Resolve(facts with
        {
            Rolls = rolls
        }, reference)).Disposition != OrdnanceResolution.Resolved)
        {
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
                _ when selected.Length > 0 => (selected.Length, "ordnance-random-selection"),
                _ => FireRollShape(inner!),
            };
            var drawn = draw(new RollRequest(count, 6));
            var rollId = $"{attemptId}-roll-{(rollIds.Count + 1).ToString(CultureInfo.InvariantCulture)}";
            rollIds[key] = rollId;
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                new DiceRolled(rollId, purpose, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
            rolls = key switch
            {
                "toHit" => rolls with { ToHit = drawn.Values },
                "subsequent" => rolls with { Subsequent = drawn.Values[0] },
                _ when selected.Length > 0 => rolls with
                {
                    CriticalSelection = selected.Select((id, index) => (id, index)).ToDictionary(item => item.id, item => drawn.Values[item.index], StringComparer.Ordinal)
                },
                _ when nested == "hit:" => rolls with { Hit = ApplyFireRoll(rolls.Hit ?? new FireRolls(null, null, null, null), inner!, drawn.Values) },
                _ => rolls with { CriticalHit = ApplyFireRoll(rolls.CriticalHit ?? new FireRolls(null, null, null, null), inner!, drawn.Values) },
            };
        }

        var gunResult = resolution.Gun!;
        var recordId = EventId(attemptId, events.Count + 1);
        BoardLocation? acquired = gunResult.AcquiredLocationId is { } acquiredAt ? BoardLocation.Parse(acquiredAt) : null;
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "ordnance-fired",
            new OrdnanceFired(facts.Gun!.GunId!, facts.Crew!.UnitId!, BoardLocation.Parse(facts.TargetLocationId!), facing, gunResult.RateOfFireKept,
                gunResult.Acquisition, acquired, rollIds, JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json)),
            package, null));

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

        if (gunConditions.Count > 0)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(facts.Gun.GunId!, gunConditions), package, null, [recordId]));
        }

        // A12.14: a concealed crew that fires loses its "?".
        if (state.Unit(facts.Crew.UnitId!) is { } crew)
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

        // A15.5: a unit that surrendered waits for the captor's choice, last.
        foreach (var effect in effects)
        {
            if (!effect.Eliminated && effect.HeatOfBattle is { Result: HeatOfBattleOutcome.Surrender, Captors.Count: > 0 } surrender)
            {
                var id = effect.FinalDefinitionId != effect.DefinitionId ? $"{attemptId}-{effect.UnitId}" : effect.UnitId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "surrender-pending", new SurrenderPending(id, surrender.Captors!), package, null, [recordId]));
            }
        }
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
            _ => rolls with { WoundSeverity = Add(rolls.WoundSeverity, unit, values[0]) },
        };
    }
}
