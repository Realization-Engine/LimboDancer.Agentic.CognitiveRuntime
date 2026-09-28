using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        // A15.44, A15.5: a leader's rally can reach Heat of Battle, so the planner reads the unit's LOS to a Known enemy and its captors. A
        // unit that goes berserk with prisoners in its Location massacres them at the start of its next fire phase (A20.4, ruling R5.7).
        var heat = leaderId is not null;

        var (attempt, reason) = LiveRally.FromState(state, unitId, leaderId, TerrainKey(read) ?? read.Level.Terrain?.Name ?? "unknown",
            concealed ? EnemyGoodOrderInLosWithin16(state, unit.Side, at) : null, heat ? KnownEnemyInLos(state, unit.Side, at) : null, heat ? Captors(state, unit) : null);
        if (attempt is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        var reference = RallyReference.Value;
        var precheck = ScenarioA1RallyCalculator.Precheck(attempt, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.rally-refused", "Rally", "attempt", precheck));
        }

        var package = ScenarioA1RallyPackage.Identity.ToString();

        // A rally by a concealed unit that stays concealed is its side's own business (ruling R19.8).
        var withheld = concealed && attempt.EnemyGoodOrderInLosWithin16 != true;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            AddRallyEvents(scope, attemptId, expected, actor, state, unit, leaderId, attempt, withheld, events, draw);
            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.rally: {unit.Id} attempts to rally in the RPh" + (leaderId is null ? " by Self-Rally" : $", rallied by {leaderId}")])
        {
            Roll = new PlannedRoll("rally", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// Draws the rolls the Rally package asks for, one at a time, and adds the attempt's events: the dice, the record (withheld from the enemy
    /// when a concealed unit stays concealed), its effects, and a pending surrender. An option the attempt reaches, the Leader Creation dr or
    /// Battle Hardening, stops it with a pending choice for the rallying side (ruling R5.8); its answer resumes it from the rolls it drew.
    /// </summary>
    private void AddRallyEvents(GameScope scope, string attemptId, long expected, string actor, GameState state, UnitInstance unit, string? leaderId,
        RallyAttempt attempt, bool withheld, List<GameEvent> events, Func<RollRequest, RollResult> draw, ResumedRolls? resumed = null)
    {
        var reference = RallyReference.Value;
        var package = ScenarioA1RallyPackage.Identity.ToString();
        attempt = attempt with
        {
            Choices = attempt.Choices ?? new Dictionary<string, string>(StringComparer.Ordinal)
        };
        var visibility = withheld ? new[] { unit.Side } : null;
        var rollIds = new Dictionary<string, string>(resumed?.RollIds ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        var rolls = new RallyRolls(null, null);
        foreach (var (key, values) in resumed?.Values ?? [])
        {
            rolls = ApplyRallyRoll(rolls, key, values);
        }

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

            // An option the attempt reaches stops it until its owner answers (ruling R5.8).
            if (resolution.Reasons is [{ } option] && option.StartsWith("asl.a1.rally.choice-missing:", StringComparison.Ordinal))
            {
                var resume = new JsonObject
                {
                    ["record"] = "rally",
                    ["unit"] = unit.Id,
                    ["facts"] = JsonNode.Parse(JsonSerializer.Serialize(attempt, LiveFire.Json)),
                    ["rolls"] = RollNode(rollIds),
                    ["withheld"] = withheld,
                };
                if (leaderId is not null)
                {
                    resume["leader"] = leaderId;
                }

                events.Add(Event(scope, attemptId, events.Count + 1, expected, "choice-pending",
                    Pending(state, option["asl.a1.rally.choice-missing:".Length..], resume), package, visibility));
                return;
            }

            if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.rally.roll-missing:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Rally package left an attempt it had accepted undecided: " + string.Join("; ", resolution.Reasons));
            }

            // The Rally DR, a leader's Wound Severity dr (A17.11), the Heat of Battle DR (A15.1), the Leader Creation dr (A18.2), or
            // the NTC of a companion a berserk leader tries to take with him (A15.41).
            const string BerserkCheck = "asl.a1.rally.roll-missing:berserkCheck:";
            var companion = missing.StartsWith(BerserkCheck, StringComparison.Ordinal) ? missing[BerserkCheck.Length..] : null;
            var rollKey = companion is not null ? "berserkCheck:" + companion
                : missing.Contains("woundSeverity", StringComparison.Ordinal) ? "woundSeverity"
                : missing.EndsWith(":heatOfBattle", StringComparison.Ordinal) ? "heatOfBattle"
                : missing.EndsWith(":leaderCreation", StringComparison.Ordinal) ? "leaderCreation"
                : "rally";
            var (count, purpose) = rollKey switch
            {
                "woundSeverity" => (1, "rally-wound-severity"),
                "heatOfBattle" => (2, "rally-heat-of-battle"),
                "leaderCreation" => (1, "rally-leader-creation"),
                _ when companion is not null => (2, "rally-berserk-check"),
                _ => (2, "rally"),
            };
            var drawn = draw(new RollRequest(count, 6));
            var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
            rollIds[rollKey] = rollId;
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                new DiceRolled(rollId, purpose, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
            rolls = ApplyRallyRoll(rolls, rollKey, drawn.Values);
        }

        var rallyId = EventId(attemptId, events.Count + 1);
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "rally-attempted",
            new RallyAttempted(unit.Id, leaderId, rollIds, JsonSerializer.SerializeToElement(attempt, LiveFire.Json),
                JsonSerializer.SerializeToElement(resolution, LiveFire.Json)), package, visibility));
        foreach (var (type, payload) in RallyEffects(state, unit, resolution.Effect!, attemptId))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, visibility, [rallyId]));
        }

        // A15.5: a unit that surrendered to ADJACENT captors waits for the captor's choice, last.
        if (resolution.Arithmetic!.HeatOfBattle is { Result: HeatOfBattleOutcome.Surrender, Captors.Count: > 0 } surrender && !resolution.Effect!.Eliminated)
        {
            var id = resolution.Effect.FinalDefinitionId != resolution.Effect.DefinitionId ? $"{attemptId}-{unit.Id}" : unit.Id;
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "surrender-pending", new SurrenderPending(id, surrender.Captors!), package, null, [rallyId]));
        }
    }

    /// <summary>A Rally package roll added to its rolls under its key.</summary>
    private static RallyRolls ApplyRallyRoll(RallyRolls rolls, string key, IReadOnlyList<int> values) => key switch
    {
        "woundSeverity" => rolls with { WoundSeverity = values[0] },
        "heatOfBattle" => rolls with { HeatOfBattle = values },
        "leaderCreation" => rolls with { LeaderCreation = values[0] },
        _ when key.StartsWith("berserkCheck:", StringComparison.Ordinal) => rolls with { BerserkChecks = Add(rolls.BerserkChecks, key["berserkCheck:".Length..], values) },
        _ => rolls with { Rally = values },
    };

    /// <summary>
    /// The events a Rally attempt's effect records: rallied, Reduced by Fate, wounded, eliminated, and "?" lost; after an
    /// Original 2, Battle Hardening, Fanaticism, a heroic leader, a hero, or a created leader (A15.21, A15.3, A18.11).
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> RallyEffects(GameState state, UnitInstance unit, RallyEffect effect, string attemptId)
    {
        foreach (var item in RallyUnitEffects(state, unit, effect, attemptId))
        {
            yield return item;
        }

        if (effect.HeroDefinitionId is { } hero)
        {
            yield return ("instance-created", new InstanceCreated(HeroOf(unit, hero, attemptId)));
        }

        // A15.41: the companions who went berserk with a berserk leader, rallied if broken.
        foreach (var id in effect.BerserkCompanions ?? [])
        {
            yield return ("conditions-changed", new ConditionsChanged(id, new Dictionary<string, ConditionState>(StringComparer.Ordinal)
            {
                [Conditions.Berserk] = ConditionState.True,
                [Conditions.Broken] = ConditionState.False,
                [Conditions.Pinned] = ConditionState.False,
                [Conditions.Disrupted] = ConditionState.False,
                [Conditions.DesperationMorale] = ConditionState.False,
                [Conditions.Concealed] = ConditionState.False,
            }));
        }

        // A18.11: the created leader, Good Order, in the rallied unit's Location; one from a Fanatic unit is Fanatic (A10.8).
        if (effect.CreatedLeaderDefinitionId is { } leader)
        {
            var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal)
            {
                [Conditions.Broken] = ConditionState.False,
                [Conditions.Pinned] = ConditionState.False,
                [Conditions.Wounded] = ConditionState.False,
                [Conditions.Concealed] = ConditionState.False,
                [Conditions.Hidden] = ConditionState.False,
            };
            if (GameState.Condition(unit, Conditions.Fanatic) == ConditionState.True)
            {
                conditions[Conditions.Fanatic] = ConditionState.True;
            }

            yield return ("instance-created", new InstanceCreated(new NewInstance($"{attemptId}-{unit.Id}-leader", "asl:leader", leader, unit.Side,
                unit.Position, null, conditions)));
        }
    }

    private static IEnumerable<(string Type, EventPayload Payload)> RallyUnitEffects(GameState state, UnitInstance unit, RallyEffect effect, string attemptId)
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
            // A10.64, A7.302: Fate Reduces a squad to its HS, broken like it; A15.3: Battle Hardening Replaces the rallied unit
            // with an unbroken unit of the next higher quality.
            var reference = FireReference.Value.Definitions[effect.FinalDefinitionId];
            var hardened = effect.Events.Contains("battle-hardened");
            var produced = unit.Conditions.Where(item => item.Key != Conditions.Concealed && item.Key != Conditions.Hidden)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            produced[Conditions.Concealed] = effect.ConcealmentLost.Contains(unit.Id) || !hardened ? ConditionState.False
                : GameState.Condition(unit, Conditions.Concealed);
            produced[Conditions.Hidden] = ConditionState.False;
            if (hardened)
            {
                produced[Conditions.Broken] = ConditionState.False;
                produced[Conditions.Pinned] = ConditionState.False;
                produced[Conditions.Disrupted] = ConditionState.False;
                produced[Conditions.DesperationMorale] = ConditionState.False;
                if (effect.Fanatic == true)
                {
                    produced[Conditions.Fanatic] = ConditionState.True;
                }

                // A15.1, A15.21: a Final DR of 5 or 6 makes a leader heroic and Battle Hardens him.
                if (effect.Heroic == true)
                {
                    produced[Conditions.Heroic] = ConditionState.True;
                }
            }

            yield return ("lineage", new LineageRecorded(hardened ? LineageAction.Replaced : LineageAction.Reduced, [unit.Id],
                [new NewInstance($"{attemptId}-{unit.Id}", reference.Kind, reference.Id, unit.Side, unit.Position, null, produced)]));
            yield break;
        }

        var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        if (effect.Rallied)
        {
            // A19.12: a Disrupted unit rallied is no longer Disrupted.
            conditions[Conditions.Broken] = ConditionState.False;
            if (GameState.Condition(unit, Conditions.Disrupted) == ConditionState.True)
            {
                conditions[Conditions.Disrupted] = ConditionState.False;
            }
        }

        // A10.8, A15.3: Fanaticism; A15.21: a heroic leader.
        if (effect.Fanatic == true && GameState.Condition(unit, Conditions.Fanatic) != ConditionState.True)
        {
            conditions[Conditions.Fanatic] = ConditionState.True;
        }

        if (effect.Heroic == true)
        {
            conditions[Conditions.Heroic] = ConditionState.True;
        }

        // A15.4, A15.42: a berserk unit, rallied, loses DM and "?"; A15.5: a surrendering one is Disrupted.
        if (effect.Berserk == true)
        {
            conditions[Conditions.Berserk] = ConditionState.True;
            conditions[Conditions.DesperationMorale] = ConditionState.False;
            conditions[Conditions.Concealed] = ConditionState.False;
        }

        if (effect.Disrupted == true)
        {
            conditions[Conditions.Disrupted] = ConditionState.True;
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

        if (unitId == equipmentId && state.Unit(unitId) is { } named && LiveFire.IsVehicle(named))
        {
            return PlanVehicleRepair(scope, attemptId, expected, label, actor, state, named);
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

    /// <summary>
    /// D3.7 (ruling R6.10): in the RPh the CE crew of a vehicle that is not Stunned or Recalled may try once to repair its malfunctioned MG:
    /// a dr of 1 repairs it, a 6 disables it for good. The SPW 251/1 can carry Passengers, so a disabled MG does not Recall it.
    /// </summary>
    private GamePlan PlanVehicleRepair(GameScope scope, string attemptId, long expected, string label, string actor, GameState state, UnitInstance vehicle)
    {
        if (state.Phase != "rph" || vehicle.Status != InstanceStatus.Active)
        {
            return Refused(scope, label, expected, "play.repair-phase: a vehicle's MG is repaired in the RPh (D3.7)");
        }

        if (!Is(vehicle, Conditions.Malfunctioned) || Is(vehicle, Conditions.Disabled))
        {
            return Refused(scope, label, expected, $"play.repair-weapon: {vehicle.Id}'s MG is not malfunctioned, or is disabled (D3.7)");
        }

        if (!LiveFire.CrewExposed(vehicle) || state.RepairsThisPhase.Contains(vehicle.Id))
        {
            return Refused(scope, label, expected, $"play.repair-unit: {vehicle.Id}'s AAMG is repaired once per RPh by a CE crew that is not Stunned or Recalled (D3.7)");
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            var dr = drawn.Values[0];
            var result = dr == 6 ? RepairAttempted.Eliminated : dr == 1 ? RepairAttempted.Repaired : RepairAttempted.NoChange;
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "repair", 1, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null),
                Event(scope, attemptId, 2, expected, "repair-attempted", new RepairAttempted(vehicle.Id, vehicle.Id, rollId, 1, result), package, null),
            };
            if (result != RepairAttempted.NoChange)
            {
                var changed = result == RepairAttempted.Repaired
                    ? new Dictionary<string, ConditionState> { [Conditions.Malfunctioned] = ConditionState.False }
                    : new Dictionary<string, ConditionState> { [Conditions.Disabled] = ConditionState.True };
                events.Add(Event(scope, attemptId, 3, expected, "conditions-changed", new ConditionsChanged(vehicle.Id, changed), package, null, [EventId(attemptId, 2)]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.repair: {vehicle.Id}'s crew attempts to repair its AAMG (a dr of 1 repairs it, a 6 disables it; D3.7)"])
        {
            Roll = new PlannedRoll("repair", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
