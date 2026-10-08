using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Rally and Repair in the RPh (unit steps 19 and 23): a broken unit rallies through the reviewed Rally package, and a
/// Good Order unit repairs a malfunctioned MG it possesses (A9.72). Each is refused before any roll unless every outcome
/// the dice can reach is decided.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The MMC that made a side's first MMC Rally attempt of the RPh in progress (A18.11), for a refusal's words; null when none is recorded.</summary>
    private static string? FirstMmcRallier(IReadOnlyList<GameEvent> events, GameState state, string side) =>
        Enumerable.Reverse(events).TakeWhile(item => item.Payload is not PhaseChanged).Select(item => item.Payload).OfType<RallyAttempted>()
            .LastOrDefault(item => state.Unit(item.Unit) is { } rallied && ScenarioA1RallyRules.IsMmcRallier(rallied.Side == side, rallied.Kind))?.Unit;

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
        if (ScenarioA1RallyRules.LeaderRallyPhaseActionBar(leaderId, leaderId is not null && state.RallyPhaseActions.Contains(leaderId)) is { } actionBar)
        {
            return Refused(scope, label, expected, actionBar);
        }
        if (state.Unit(unitId) is not { } unit || state.Location(unit.Id)?.Location is not { } at || ReadLocation(state, at) is not { } read)
        {
            return Refused(scope, label, expected, ScenarioA1RallyRules.RallyUnitUnreadText(unitId));
        }

        // A10.6: the rallying leader is of the unit's side; one of another nationality leads it as Allied Troops (A10.7; ruling R15.8).
        if (ScenarioA1RallyRules.LeaderSideBar(leaderId, unitId, leaderId is not null && state.Unit(leaderId) is { } rallying && rallying.Side != unit.Side) is { } sideBar)
        {
            return Refused(scope, label, expected, sideBar);
        }

        // A12.141: the attempt costs a concealed unit or rallying leader its "?" in the LOS of a Good Order enemy within 16 hexes.
        var concealed = ScenarioA1RallyRules.ConcealedAttempt(GameState.Condition(unit, Conditions.Concealed) == ConditionState.True,
            leaderId is not null && state.Unit(leaderId) is { } leaderUnit && GameState.Condition(leaderUnit, Conditions.Concealed) == ConditionState.True);
        // A15.44, A15.5: a leader's rally can reach Heat of Battle, so the planner reads the unit's LOS to a Known enemy and its captors. A
        // unit that goes berserk with prisoners in its Location massacres them at the start of its next fire phase (A20.4, ruling R5.7).
        var heat = ScenarioA1RallyRules.ReachesHeatOfBattle(leaderId);

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
            // Pass 31 (play test R-04; A10.63, A18.11, A10.71): which of the Self-Rally rules bars this unit is said, not only that one does.
            string[] why = ScenarioA1RallyRules.SelfRallyRefusal(precheck, unit.Side == state.PhasingSide, state.FirstMmcRallyTaken.Contains(unit.Side)) switch
            {
                SelfRallyBar.NotOwnPhase =>
                    [$"play.rally-self: {unit.Id} has no Self-Rally capability, and only its own side's RPh gives one MMC a Self-Rally without it (A10.63, A18.11); it needs an unbroken leader in its Location"],
                // Pass 31c (backlog section 48): the refusal names the unit that used the attempt.
                SelfRallyBar.Used =>
                    [$"play.rally-self: {unit.Id} cannot Self-Rally: its side's one MMC Self-Rally of this RPh was used{(FirstMmcRallier(existing, state, unit.Side) is { } first ? $" by {first}" : string.Empty)} (A18.11). It needs an unbroken leader in its Location"],
                SelfRallyBar.BrokenLeader =>
                    [$"play.rally-self: {unit.Id} has no Self-Rally capability, and a broken leader is in its Location, so its side's first MMC Rally attempt is not open to it (A10.71, A18.11); rally the leader first"],
                _ => [],
            };
            return Refused(scope, label, expected, [.. RefusalReasons.Refusal("play.rally-refused", "Rally", "attempt", precheck), .. why]);
        }

        var package = ScenarioA1RallyPackage.Identity.ToString();

        // A rally by a concealed unit that stays concealed is its side's own business (ruling R19.8).
        var withheld = ScenarioA1RallyRules.Withheld(concealed, attempt.EnemyGoodOrderInLosWithin16);
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            AddRallyEvents(scope, attemptId, expected, actor, state, unit, leaderId, attempt, withheld, events, draw);
            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1RallyRules.RallySummary(unit.Id, leaderId)])
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
            var next = ScenarioA1RallyRules.NextStep(resolution.Disposition, resolution.Reasons);
            if (next.Resolved)
            {
                break;
            }

            // An option the attempt reaches stops it until its owner answers (ruling R5.8).
            if (next.ChoiceKey is { } choiceKey)
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
                    Pending(state, choiceKey, resume), package, visibility));
                return;
            }

            if (next.Undecided is { } undecided)
            {
                throw new InvalidOperationException("The Rally package left an attempt it had accepted undecided: " + undecided);
            }

            // The Rally DR, a leader's Wound Severity dr (A17.11), the Heat of Battle DR (A15.1), the Leader Creation dr (A18.2), or
            // the NTC of a companion a berserk leader tries to take with him (A15.41), as Rules names them.
            var rollKey = next.RollKey!;
            var (count, purpose) = (next.Count, next.Purpose!);
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
        var heatOfBattle = resolution.Arithmetic!.HeatOfBattle;
        if (ScenarioA1RallyRules.SurrendersAfterRally(heatOfBattle?.Result == HeatOfBattleOutcome.Surrender, heatOfBattle?.Captors is { Count: > 0 }, resolution.Effect!.Eliminated))
        {
            var id = ScenarioA1RallyRules.SurrenderingId(attemptId, unit.Id, resolution.Effect.FinalDefinitionId != resolution.Effect.DefinitionId);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "surrender-pending", new SurrenderPending(id, heatOfBattle!.Captors!), package, null, [rallyId]));
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
            yield return ("instance-created", new InstanceCreated(HeroOf(unit, hero, attemptId, concealed: ScenarioA1RallyRules.HeroConcealed(effect.ConcealmentLost.Contains(unit.Id), effect.Eliminated))));
        }

        // A15.41: the companions who went berserk with a berserk leader, rallied if broken.
        foreach (var id in effect.BerserkCompanions ?? [])
        {
            yield return ("conditions-changed", new ConditionsChanged(id, ConditionChanges(ScenarioA1RallyRules.BerserkCompanionConditions())));
        }

        // A18.11: the created leader, Good Order, in the rallied unit's Location; one from a Fanatic unit is Fanatic (A10.8).
        if (effect.CreatedLeaderDefinitionId is { } leader)
        {
            var conditions = ConditionChanges(ScenarioA1RallyRules.CreatedLeaderConditions(GameState.Condition(unit, Conditions.Fanatic) == ConditionState.True));
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
            var hardened = ScenarioA1RallyRules.BattleHardened(effect.Events);
            var produced = unit.Conditions.Where(item => item.Key != Conditions.Concealed && item.Key != Conditions.Hidden)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            // Rules gives the conditions in the order the record writes them; null keeps the unit's own value (its "?" when Battle Hardened).
            foreach (var (condition, value) in ScenarioA1RallyRules.ReplacedUnitConditions(hardened, effect.ConcealmentLost.Contains(unit.Id), effect.Fanatic, effect.Heroic))
            {
                produced[ConditionName(condition)] = value is { } set ? (set ? ConditionState.True : ConditionState.False) : GameState.Condition(unit, ConditionName(condition));
            }

            // A25.222 (ruling R15.6): a Commissar's failed rally Replaces the unit by one of its size.
            var replaced = ScenarioA1RallyRules.ReplacedNotReduced(hardened, effect.ReplacedByCommissar, reference.Kind == unit.Kind);
            yield return ("lineage", new LineageRecorded(replaced ? LineageAction.Replaced : LineageAction.Reduced, [unit.Id],
                [new NewInstance($"{attemptId}-{unit.Id}", reference.Kind, reference.Id, unit.Side, unit.Position, null, produced)]));
            yield break;
        }

        // A19.12, A10.8, A15.3, A15.21, A15.4, A15.42, A15.5: the rallied unit's conditions, in the order the record writes them (pass 32.f).
        var conditions = ConditionChanges(ScenarioA1RallyRules.RalliedUnitConditions(effect.Rallied, GameState.Condition(unit, Conditions.Disrupted) == ConditionState.True,
            effect.Fanatic, GameState.Condition(unit, Conditions.Fanatic) == ConditionState.True, effect.Heroic, effect.Berserk, effect.Disrupted, effect.Wounded,
            GameState.Condition(unit, Conditions.Wounded) == ConditionState.True, effect.ConcealmentLost.Contains(unit.Id)));
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

        if (ScenarioA1RallyRules.SwRepairPhaseBar(state.Phase) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || !ScenarioA1RallyRules.SwRepairGoodOrderAsPlanned(RuleBool(GameState.Condition(unit, Conditions.Broken))))
        {
            return Refused(scope, label, expected, ScenarioA1RallyRules.SwRepairUnitText(unitId));
        }

        if (ScenarioA1RallyRules.SwRepairRalliedBar(unitId, state.RallyAttemptsThisPlayerTurn.Contains(unit.Id)) is { } ralliedBar)
        {
            return Refused(scope, label, expected, ralliedBar);
        }

        if (ScenarioA1RallyRules.SwRepairActionBar(unitId, state.RallyPhaseActions.Contains(unit.Id)) is { } actionBar)
        {
            return Refused(scope, label, expected, actionBar);
        }

        if (state.Find(equipmentId) is not EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding } equipment
            || !ScenarioA1RallyRules.SwRepairWeaponAllowed(holding.Holder == unit.Id, GameState.Condition(equipment, Conditions.Malfunctioned) == ConditionState.True))
        {
            return Refused(scope, label, expected, ScenarioA1RallyRules.SwRepairWeaponText(equipmentId, unitId));
        }

        var printed = equipment.Definition is null ? null : FireReference.Value.Definitions.GetValueOrDefault(equipment.Definition.Definition)?.Repair;
        if (ScenarioA1RallyRules.SwRepairNumberBar(equipmentId, printed) is { } numberBar)
        {
            return Refused(scope, label, expected, numberBar);
        }

        var repairNumber = printed!.Value;
        var package = ScenarioA1FirePackage.Identity.ToString();
        var withheld = ScenarioA1RallyRules.RepairWithheld(GameState.Condition(unit, Conditions.Concealed) == ConditionState.True, GameState.Condition(unit, Conditions.Hidden) == ConditionState.True)
            ? new[] { unit.Side } : null;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            var dr = drawn.Values[0];
            var result = RepairResult(ScenarioA1RallyRules.SwRepairResult(dr, repairNumber));
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
            [ScenarioA1RallyRules.SwRepairSummary(unit.Id, equipment.Id, repairNumber)])
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
        if (ScenarioA1RallyRules.VehicleRepairPhaseBar(state.Phase, vehicle.Status == InstanceStatus.Active) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        if (ScenarioA1RallyRules.VehicleRepairWeaponBar(vehicle.Id, Is(vehicle, Conditions.Malfunctioned), Is(vehicle, Conditions.Disabled)) is { } weaponBar)
        {
            return Refused(scope, label, expected, weaponBar);
        }

        if (ScenarioA1RallyRules.VehicleRepairCrewBarAsPlanned(vehicle.Id, LiveFire.CrewExposed(vehicle), state.RepairsThisPhase.Contains(vehicle.Id)) is { } crewBar)
        {
            return Refused(scope, label, expected, crewBar);
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            var dr = drawn.Values[0];
            var outcome = ScenarioA1RallyRules.VehicleMgRepairResult(dr);
            var result = RepairResult(outcome);
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "repair", 1, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null),
                Event(scope, attemptId, 2, expected, "repair-attempted", new RepairAttempted(vehicle.Id, vehicle.Id, rollId, 1, result), package, null),
            };
            if (outcome != RepairOutcome.NoChange)
            {
                var (condition, value) = ScenarioA1RallyRules.VehicleRepairChange(outcome);
                var changed = new Dictionary<string, ConditionState> { [ConditionName(condition)] = value ? ConditionState.True : ConditionState.False };
                events.Add(Event(scope, attemptId, 3, expected, "conditions-changed", new ConditionsChanged(vehicle.Id, changed), package, null, [EventId(attemptId, 2)]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1RallyRules.VehicleRepairSummary(vehicle.Id)])
        {
            Roll = new PlannedRoll("repair", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>A Repair outcome of Rules as the record names it.</summary>
    private static string RepairResult(RepairOutcome outcome) => outcome switch
    {
        RepairOutcome.Eliminated => RepairAttempted.Eliminated,
        RepairOutcome.Repaired => RepairAttempted.Repaired,
        _ => RepairAttempted.NoChange,
    };

    /// <summary>A condition's state as a three-valued fact for Rules.</summary>
    private static bool? RuleBool(ConditionState state) => state switch
    {
        ConditionState.True => true,
        ConditionState.False => false,
        _ => null,
    };
}
