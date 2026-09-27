using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Rally and Repair in the RPh (unit steps 19 and 23): a broken unit rallies through the reviewed Rally package, and a
/// Good Order unit repairs a malfunctioned MG it possesses (A9.72). Each is refused before any roll unless every outcome
/// the dice can reach is decided.
/// </summary>
public sealed partial class GamePlanner
{
    private static readonly Lazy<ScenarioA1RallyReference> RallyReference = new(() => new ScenarioA1RallyPackage().Reference);

    private GamePlan PlanRally(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Rally attempt names the broken unit");
        }

        string? leaderId = Text(arguments, "leader", out var named) ? named : null;
        if (state.Unit(unitId) is not { } unit || state.Location(unit.Id)?.Location is not { } at || ReadLocation(state, at) is not { } read)
        {
            return Refused(scope, label, expected, $"play.rally-unit: '{unitId}' is not a unit on a Location the map reads");
        }

        // A12.141: the attempt costs a concealed unit or rallying leader its "?" in the LOS of a Good Order enemy within 16 hexes.
        var concealed = GameState.Condition(unit, Conditions.Concealed) == ConditionState.True
            || (leaderId is not null && state.Unit(leaderId) is { } leaderUnit && GameState.Condition(leaderUnit, Conditions.Concealed) == ConditionState.True);
        var (attempt, reason) = LiveRally.FromState(state, unitId, leaderId, TerrainKey(read) ?? read.Level.Terrain?.Name ?? "unknown",
            concealed ? EnemyGoodOrderInLosWithin16(state, unit.Side, at) : null);
        if (attempt is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        var reference = RallyReference.Value;
        var precheck = ScenarioA1RallyCalculator.Precheck(attempt, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, ["play.rally-refused: the Rally package does not decide every outcome of this attempt", .. precheck]);
        }

        var package = ScenarioA1RallyPackage.Identity.ToString();

        // A rally by a concealed unit that stays concealed is its side's own business (ruling R19.8).
        var withheld = concealed && attempt.EnemyGoodOrderInLosWithin16 != true ? new[] { unit.Side } : null;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            var rollIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var rolls = new RallyRolls(null, null);
            RallyResolution resolution;
            while (true)
            {
                resolution = ScenarioA1RallyCalculator.Resolve(attempt with
                {
                    Rolls = rolls
                }, reference);
                if (resolution.Disposition == RallyResolution.Resolved)
                {
                    break;
                }

                if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.rally.roll-missing:", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The Rally package left an attempt it had accepted undecided: " + string.Join("; ", resolution.Reasons));
                }

                var wound = missing.Contains("woundSeverity", StringComparison.Ordinal);
                var drawn = draw(new RollRequest(wound ? 1 : 2, 6));
                var rollId = $"{attemptId}-roll-{(rollIds.Count + 1).ToString(CultureInfo.InvariantCulture)}";
                rollIds[wound ? "woundSeverity" : "rally"] = rollId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, wound ? "rally-wound-severity" : "rally", wound ? 1 : 2, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
                rolls = wound ? rolls with
                {
                    WoundSeverity = drawn.Values[0]
                } : rolls with
                {
                    Rally = drawn.Values
                };
            }

            var rallyId = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "rally-attempted",
                new RallyAttempted(unit.Id, leaderId, rollIds, JsonSerializer.SerializeToElement(attempt, LiveFire.Json),
                    JsonSerializer.SerializeToElement(resolution, LiveFire.Json)), package, withheld));
            foreach (var (type, payload) in RallyEffects(state, unit, resolution.Effect!, attemptId))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, withheld, [rallyId]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.rally: {unit.Id} attempts to rally in the RPh" + (leaderId is null ? " by Self-Rally" : $", rallied by {leaderId}")])
        {
            Roll = new PlannedRoll("rally", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>The events a Rally attempt's effect records: rallied, Reduced by Fate, wounded, eliminated, and "?" lost.</summary>
    private static IEnumerable<(string Type, EventPayload Payload)> RallyEffects(GameState state, UnitInstance unit, RallyEffect effect, string attemptId)
    {
        foreach (var id in effect.ConcealmentLost.Where(id => id != unit.Id))
        {
            yield return ("conditions-changed", new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False }));
        }

        if (effect.Eliminated)
        {
            yield return ("instance-eliminated", new InstanceEliminated(unit.Id));
            yield break;
        }

        if (effect.FinalDefinitionId != effect.DefinitionId)
        {
            // A10.64, A7.302: Fate Reduces a squad to its HS, broken like it.
            var reference = FireReference.Value.Definitions[effect.FinalDefinitionId];
            var produced = unit.Conditions.Where(item => item.Key != Conditions.Concealed && item.Key != Conditions.Hidden)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            produced[Conditions.Concealed] = ConditionState.False;
            produced[Conditions.Hidden] = ConditionState.False;
            yield return ("lineage", new LineageRecorded(LineageAction.Reduced, [unit.Id],
                [new NewInstance($"{attemptId}-{unit.Id}", reference.Kind, reference.Id, unit.Side, unit.Position, null, produced)]));
            yield break;
        }

        var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        if (effect.Rallied)
        {
            conditions[Conditions.Broken] = ConditionState.False;
        }

        if (effect.Wounded && GameState.Condition(unit, Conditions.Wounded) != ConditionState.True)
        {
            conditions[Conditions.Wounded] = ConditionState.True;
        }

        if (effect.ConcealmentLost.Contains(unit.Id))
        {
            conditions[Conditions.Concealed] = ConditionState.False;
        }

        if (conditions.Count > 0)
        {
            yield return ("conditions-changed", new ConditionsChanged(unit.Id, conditions));
        }
    }

    /// <summary>
    /// Repair in the RPh (A9.72, p. 65): a Good Order unit that has not attempted to rally makes a dr for a malfunctioned MG
    /// it possesses; at most its Repair Number repairs it, a 6 eliminates it. Every outcome is decided, so only the facts
    /// can refuse it.
    /// </summary>
    private GamePlan PlanRepair(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "equipmentId", out var equipmentId))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Repair names the unit and the SW");
        }

        if (state.Phase != "rph")
        {
            return Refused(scope, label, expected, "play.repair-phase: a SW is repaired in the RPh (A9.72, p. 65)");
        }

        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || GameState.Condition(unit, Conditions.Broken) != ConditionState.False)
        {
            return Refused(scope, label, expected, $"play.repair-unit: '{unitId}' is not a Good Order unit (A9.72)");
        }

        if (state.RallyAttemptsThisPlayerTurn.Contains(unit.Id))
        {
            return Refused(scope, label, expected, $"play.repair-unit: '{unitId}' attempted to rally this RPh (A3.1, p. 47)");
        }

        if (state.Find(equipmentId) is not EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding } equipment
            || holding.Holder != unit.Id || GameState.Condition(equipment, Conditions.Malfunctioned) != ConditionState.True)
        {
            return Refused(scope, label, expected, $"play.repair-weapon: '{equipmentId}' is not a malfunctioned SW '{unitId}' possesses");
        }

        if (equipment.Definition is null || FireReference.Value.Definitions.GetValueOrDefault(equipment.Definition.Definition)?.Repair is not { } repairNumber)
        {
            return Refused(scope, label, expected, $"play.repair-weapon: '{equipmentId}' has no Repair Number in the catalog");
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        var withheld = GameState.Condition(unit, Conditions.Concealed) == ConditionState.True || GameState.Condition(unit, Conditions.Hidden) == ConditionState.True
            ? new[] { unit.Side } : null;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            var dr = drawn.Values[0];
            var result = dr == 6 ? RepairAttempted.Eliminated : dr <= repairNumber ? RepairAttempted.Repaired : RepairAttempted.NoChange;
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "repair", 1, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null),
                Event(scope, attemptId, 2, expected, "repair-attempted", new RepairAttempted(unit.Id, equipment.Id, rollId, repairNumber, result), package, withheld),
            };
            if (result == RepairAttempted.Repaired)
            {
                events.Add(Event(scope, attemptId, 3, expected, "conditions-changed",
                    new ConditionsChanged(equipment.Id, new Dictionary<string, ConditionState> { [Conditions.Malfunctioned] = ConditionState.False }),
                    package, withheld, [EventId(attemptId, 2)]));
            }
            else if (result == RepairAttempted.Eliminated)
            {
                events.Add(Event(scope, attemptId, 3, expected, "instance-eliminated", new InstanceEliminated(equipment.Id), package, withheld,
                    [EventId(attemptId, 2)]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.repair: {unit.Id} attempts to repair {equipment.Id} (R{repairNumber}; a 6 eliminates it)"])
        {
            Roll = new PlannedRoll("repair", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
