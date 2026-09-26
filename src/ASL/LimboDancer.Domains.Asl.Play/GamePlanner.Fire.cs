using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Reads LOS for a fire attack. The planner's own reader builds the LOS map from the boards' LOS data; a host or a test
/// may supply another, such as one over a map it has already built.
/// </summary>
public interface IFireLosReader
{
    /// <summary>The LOS result from one Location to another on the game's map, or null when it cannot be read.</summary>
    public LosResult? Read(GameState state, BoardLocation from, BoardLocation target);
}

/// <summary>
/// Fire in live play (unit step 18): a PFPh or DFPh attack by a fire group in one Location, resolved by the reviewed
/// Fire package. Every fact comes from the game and the map read; the attack is refused before any roll unless every
/// outcome the dice can reach is decided (<see cref="ScenarioA1FireCalculator.Precheck"/>), and its rolls are drawn one at
/// a time as the package asks for them.
/// </summary>
public sealed partial class GamePlanner
{
    private static readonly Lazy<ScenarioA1FireReference> FireReference = new(() => new ScenarioA1FirePackage().Reference);

    // The VASL terrain names the Fire package's TEM admits (Terrain Chart p. 698; B1.1, B12, B13, B14, B15, B23).
    private static readonly IReadOnlyDictionary<string, string> FireTerrain = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Open Ground"] = "open-ground",
        ["Brush"] = "brush",
        ["Woods"] = "woods",
        ["Orchard"] = "orchard",
        ["Grain"] = "grain",
    };

    private GamePlan PlanFire(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!arguments.TryGetProperty("firers", out var firerList) || firerList.ValueKind != JsonValueKind.Array
            || firerList.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String)
            || !Text(arguments, "target", out var targetText) || !BoardLocation.TryParse(targetText, out var target))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: fire names its firers and a target Location");
        }

        string? directorId = Text(arguments, "director", out var named) ? named : null;
        var (attack, reason) = LiveFire.FromState(state, [.. firerList.EnumerateArray().Select(item => item.GetString()!)], directorId, target);
        if (attack is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        // A7.55: the units of a Location that fire at a target in a phase form one fire group, so it fires once.
        if (state.FiresThisPhase.Any(record => record.FirerLocation == attack.FirerLocationId && record.TargetLocation == attack.TargetLocationId))
        {
            return Refused(scope, label, expected,
                $"play.fire-group: {attack.FirerLocationId} has already fired at {attack.TargetLocationId} this phase, and its units fire as one fire group (A7.55, p. 57)");
        }

        var (map, mapReason) = MapFacts(state, BoardLocation.Parse(attack.FirerLocationId!), target);
        if (map is null)
        {
            return Refused(scope, label, expected, mapReason!);
        }

        attack = attack with
        {
            Range = map.Range,
            SameLevel = map.SameLevel,
            Los = map.Los,
            TargetTerrain = map.Terrain
        };
        var reference = FireReference.Value;
        var precheck = ScenarioA1FireCalculator.Precheck(attack, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, ["play.fire-refused: the Fire package does not decide every outcome of this attack", .. precheck]);
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        var facts = attack;
        var targetSide = state.Unit(facts.Targets![0].UnitId!)!.Side;
        var marker = facts.Phase == "PFPh" ? Conditions.PrepFire : Conditions.FinalFire;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            var rollIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var rolls = new FireRolls(null, null, null, null, null);
            FireResolution resolution;
            while (true)
            {
                resolution = ScenarioA1FireCalculator.Resolve(facts with
                {
                    Rolls = rolls
                }, reference);
                if (resolution.Disposition == FireResolution.Resolved)
                {
                    break;
                }

                // The pre-check leaves only missing rolls, asked for one at a time.
                if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The Fire package left an attack it had accepted undecided: " + string.Join("; ", resolution.Reasons));
                }

                var key = missing["asl.a1.fire.roll-missing:".Length..];
                var split = key.IndexOf(':', StringComparison.Ordinal);
                var (kind, unit) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
                var (count, purpose) = kind switch
                {
                    "attack" => (2, "fire-ift"),
                    "randomSelection" => (facts.Targets.Count, "fire-random-selection"),
                    "checks" => (2, "fire-check"),
                    "leaderLoss" => (2, "fire-leader-loss"),
                    _ => (1, "fire-wound-severity"),
                };
                var drawn = draw(new RollRequest(count, 6));
                var rollId = $"{attemptId}-roll-{(rollIds.Count + 1).ToString(CultureInfo.InvariantCulture)}";
                rollIds[kind == "randomSelection" ? kind : key] = rollId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, purpose, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
                rolls = kind switch
                {
                    "attack" => rolls with { Attack = drawn.Values },
                    "randomSelection" => rolls with
                    {
                        RandomSelection = facts.Targets.Select((item, index) => (item.UnitId!, drawn.Values[index]))
                            .ToDictionary(pair => pair.Item1, pair => pair.Item2, StringComparer.Ordinal),
                    },
                    "checks" => rolls with { Checks = Add(rolls.Checks, unit, drawn.Values) },
                    "leaderLoss" => rolls with { LeaderLoss = Add(rolls.LeaderLoss, unit, drawn.Values) },
                    _ => rolls with { WoundSeverity = Add(rolls.WoundSeverity, unit, drawn.Values[0]) },
                };
            }

            // A concealed unit that the attack leaves concealed is not identified to the firing side (A12.14).
            var hidesIdentity = resolution.Arithmetic!.Result == "none" && facts.Targets.Any(item => item.Concealed == true);
            var fireId = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "fire-resolved",
                new FireResolved([.. facts.Firers!.Select(item => item.UnitId!)], facts.Director?.UnitId, facts.FirerLocationId!, facts.TargetLocationId!,
                    rollIds, JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json)),
                package, hidesIdentity ? [targetSide] : null));
            foreach (var effect in resolution.Effects)
            {
                if (EffectEvent(state, effect, attemptId) is { } payload)
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, payload.Type, payload.Payload, package, null, [fireId]));
                }
            }

            foreach (var id in resolution.FireCounterUnitIds)
            {
                var conditions = new Dictionary<string, ConditionState> { [marker] = ConditionState.True };
                if (resolution.FirerConcealmentLost.Contains(id))
                {
                    conditions[Conditions.Concealed] = ConditionState.False;
                }

                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(id, conditions), package, null,
                    [fireId]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.fire: {string.Join(", ", facts.Firers!.Select(item => item.UnitId))} fire at {facts.TargetLocationId} in the {facts.Phase}"
                + (facts.Director is { } director ? $", directed by {director.UnitId}" : string.Empty)
                + $"; range {facts.Range}, {facts.TargetTerrain}, Hindrance {facts.Los!.HindranceDrm}"])
        {
            Roll = new PlannedRoll("fire", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    private static Dictionary<string, T> Add<T>(IReadOnlyDictionary<string, T>? existing, string id, T value)
    {
        var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new Dictionary<string, T>(StringComparer.Ordinal);
        next[id] = value;
        return next;
    }

    /// <summary>The event that records one target unit's effect: an elimination, a Reduction or Replacement, or new conditions.</summary>
    private static (string Type, EventPayload Payload)? EffectEvent(GameState state, FireUnitEffect effect, string attemptId)
    {
        var unit = state.Unit(effect.UnitId)!;
        if (effect.Eliminated)
        {
            return ("instance-eliminated", new InstanceEliminated(unit.Id));
        }

        var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        void Set(string name, bool value)
        {
            if ((GameState.Condition(unit, name) == ConditionState.True) != value)
            {
                conditions[name] = value ? ConditionState.True : ConditionState.False;
            }
        }

        Set(Conditions.Broken, effect.Broken);
        Set(Conditions.Pinned, effect.Pinned);
        Set(Conditions.Wounded, effect.Wounded);
        Set(Conditions.Disrupted, effect.Disrupted);
        if (effect.ConcealmentLost)
        {
            conditions[Conditions.Concealed] = ConditionState.False;
            conditions[Conditions.Hidden] = ConditionState.False;
        }

        if (effect.FinalDefinitionId != effect.DefinitionId)
        {
            // A7.302: Casualty Reduction makes a HS of the same broken status; A19.13: Replacement by a lesser unit.
            var reference = FireReference.Value.Definitions[effect.FinalDefinitionId];
            var reduced = unit.Kind == "asl:squad" && reference.Kind == "asl:half-squad";
            var produced = unit.Conditions.Where(item => item.Key != Conditions.Concealed && item.Key != Conditions.Hidden)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            foreach (var (name, value) in conditions)
            {
                produced[name] = value;
            }

            produced[Conditions.Concealed] = ConditionState.False;
            produced[Conditions.Hidden] = ConditionState.False;
            return ("lineage", new LineageRecorded(reduced ? LineageAction.Reduced : LineageAction.Replaced, [unit.Id],
                [new NewInstance($"{attemptId}-{unit.Id}", reference.Kind, reference.Id, unit.Side, unit.Position, null, produced)]));
        }

        return conditions.Count == 0 ? null : ("conditions-changed", new ConditionsChanged(unit.Id, conditions));
    }

    /// <summary>The map's facts of an attack: range, levels, LOS and its attributed Hindrance, and the target's terrain.</summary>
    private (FireMapFacts? Facts, string? Reason) MapFacts(GameState state, BoardLocation from, BoardLocation target)
    {
        LosMap? map;
        Func<BoardLocation, LocationReadResult> resolve;
        if (state.Map.IsPlaced)
        {
            if (Composed(state) is not { } composed)
            {
                return (null, "play.fire-map: a board of the placed map cannot be read");
            }

            map = fireLos is null ? LosMap.ForPlacedMap(composed).Map : null;
            resolve = composed.Resolve;
        }
        else
        {
            if (state.Map.Boards is not [{ } placed] || boards.TryGetBoard(placed.Board, placed.Version).Board is not { } handle)
            {
                return (null, "play.fire-map: the board in play cannot be read");
            }

            map = fireLos is null ? LosMap.ForBoard(handle).Map : null;
            resolve = handle.Resolve;
        }

        if (resolve(from).Read is not { } firerRead || resolve(target).Read is not { } targetRead)
        {
            return (null, "play.fire-map: the Locations cannot be read");
        }

        if ((fireLos is not null ? fireLos.Read(state, from, target) : map is null ? null : LosCalculator.Check(map, from, target)) is not { } los)
        {
            return (null, "play.fire-los: the board has no LOS data to read");
        }

        if (los.Status is not (LosStatus.Clear or LosStatus.Blocked))
        {
            return (null, $"play.fire-los: the LOS read gives no definitive answer ({los.Status}: {los.Reason})");
        }

        var terrainName = (targetRead.Level.Terrain ?? targetRead.Hex.Center.Terrain)?.Name;
        var terrain = terrainName is null ? null
            : FireTerrain.GetValueOrDefault(terrainName)
                ?? (OrdinaryBuildings.Contains(terrainName) ? terrainName.StartsWith("Stone", StringComparison.Ordinal) ? "stone-building" : "wooden-building" : null);
        if (terrain is null)
        {
            return (null, $"play.fire-terrain: the target's terrain ({terrainName ?? "unknown"}) has no TEM in the Fire package");
        }

        if (targetRead.Hex.Hexsides.Any(side => side.HexsideTerrain is not null || side.Cliff))
        {
            return (null, "play.fire-terrain: hexside terrain at the target is not reviewed");
        }

        // A6.7: the largest Hindrance at each range counts; brush always, grain June to September (B15.2).
        var month = state.ScenarioMonth;
        var inSeason = month is >= 6 and <= 9;
        var attributed = los.Hindrances.All(entry => entry.Terrains.Count > 0 && entry.Terrains.All(name => name is "Brush" or "Grain"));
        var drm = los.Hindrances.Count(entry => entry.Terrains.Contains("Brush") || (inSeason && entry.Terrains.Contains("Grain")));
        var grain = los.Hindrances.Any(entry => entry.Terrains.Contains("Grain"));
        var sameLevel = firerRead.Hex.BaseLevel + firerRead.Level.Level == targetRead.Hex.BaseLevel + targetRead.Level.Level;
        return (new FireMapFacts(los.Range, sameLevel, new FireLos(los.IsBlocked == true, drm, attributed, grain), terrain), null);
    }

    private sealed record FireMapFacts(int Range, bool SameLevel, FireLos Los, string Terrain);
}
