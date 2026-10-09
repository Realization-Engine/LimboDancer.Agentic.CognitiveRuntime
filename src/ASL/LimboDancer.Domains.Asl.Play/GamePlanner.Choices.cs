using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>The rolls a resolution drew before it stopped at a choice (ruling R5.8): each key's roll id and recorded values, in draw order.</summary>
internal sealed record ResumedRolls(IReadOnlyDictionary<string, string> RollIds, IReadOnlyList<(string Key, IReadOnlyList<int> Values)> Values);

/// <summary>
/// The owners' options during a resolution (ruling R5.8), the captor's choice at a surrender (A20.3, ruling R5.6), and Massacre (A20.4,
/// ruling R5.7). A resolution that reaches an option records the rolls it drew and a pending choice, and stops; the choosing side's answer
/// continues it from those rolls with the answer as a declared fact. Nothing else may happen while a choice is pending. The rules are
/// <see cref="ScenarioA1SequenceCalculator"/>'s (pass 32.i); this file reads the request and the state and writes the events.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The roll ids of a resolution, for a pending choice's resume data.</summary>
    private static JsonObject RollNode(IReadOnlyDictionary<string, string> rollIds)
    {
        var node = new JsonObject();
        foreach (var (key, id) in rollIds.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            node[key] = id;
        }

        return node;
    }

    /// <summary>The pending choice for an option a package asks for (ruling R5.8); Rules decides whose it is (pass 32.i).</summary>
    private static ChoicePending Pending(GameState state, string key, JsonObject resume)
    {
        var (kind, unit) = ScenarioA1SequenceCalculator.OptionKey(key);
        var owner = state.Unit(unit)?.Side;
        var (choiceKind, side) = ScenarioA1SequenceCalculator.PendingChoice(kind, owner, state.Sides.FirstOrDefault(item => item.Id != (owner ?? state.PhasingSide))?.Id, state.PhasingSide);
        return new ChoicePending(key, ChoiceKindName(choiceKind), side, [ChoicePending.Take, ChoicePending.Decline], JsonSerializer.SerializeToElement(resume));
    }

    /// <summary>A choice's kind under Units' name.</summary>
    private static string ChoiceKindName(ChoiceKind kind) => kind switch
    {
        ChoiceKind.BattleHardening => ChoicePending.BattleHardening,
        ChoiceKind.LeaderCreation => ChoicePending.LeaderCreation,
        ChoiceKind.UnlikelyKill => ChoicePending.UnlikelyKill,
        ChoiceKind.Paatc => ChoicePending.Paatc,
        _ => ChoicePending.Acquisition,
    };

    /// <summary>A choice's kind as Rules names it.</summary>
    private static ChoiceKind ChoiceKindOf(string kind) => kind switch
    {
        ChoicePending.BattleHardening => ChoiceKind.BattleHardening,
        ChoicePending.LeaderCreation => ChoiceKind.LeaderCreation,
        ChoicePending.UnlikelyKill => ChoiceKind.UnlikelyKill,
        ChoicePending.Paatc => ChoiceKind.Paatc,
        _ => ChoiceKind.Acquisition,
    };

    /// <summary>What a pending choice asks, in words, for the Play page and the plan's reasons.</summary>
    public static string DescribeChoice(PendingChoice choice)
    {
        ArgumentNullException.ThrowIfNull(choice);
        var subject = choice.Key[(choice.Key.IndexOf(':', StringComparison.Ordinal) + 1)..];
        return ScenarioA1SequenceCalculator.DescribeChoice(ChoiceKindOf(choice.Kind), subject, choice.Options);
    }

    /// <summary>
    /// The choosing side's answer to the pending choice (ruling R5.8): it is recorded, and a stopped resolution continues from the rolls it
    /// drew, with the answer among its declared choices; it may stop again at a later option.
    /// </summary>
    private GamePlan PlanChoose(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "key", out var key) || !Text(arguments, "option", out var option))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: an answer names the choice and the option chosen");
        }

        if (state.Choice is not { } pending || pending.Key != key)
        {
            return Refused(scope, label, expected, ScenarioA1SequenceCalculator.NoChoiceText(key));
        }

        if (ScenarioA1SequenceCalculator.OptionBar(pending.Options, option) is { } optionBar)
        {
            return Refused(scope, label, expected, optionBar);
        }

        var made = Event(scope, attemptId, 1, expected, "choice-made", new ChoiceMade(key, option), null, null, [pending.Event]);
        var summary = ScenarioA1SequenceCalculator.ChoiceSummary(pending.Side, option, DescribeChoice(pending));
        if (pending.Kind == ChoicePending.Acquisition)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [made], [summary]);
        }

        // A12.41 (ruling R11.12): the concealed units a vehicle entered are revealed or take their combined PAATC.
        if (pending.Kind == ChoicePending.Paatc)
        {
            var answer = PlanPaatcAnswer(scope, existing, attemptId, expected, label, actor, state, pending, made, option);
            return answer with
            {
                Reasons = [summary, .. answer.Reasons]
            };
        }

        var resume = pending.Resume;
        var rollIds = resume.TryGetProperty("rolls", out var rollMap) && rollMap.ValueKind == JsonValueKind.Object
            ? rollMap.EnumerateObject().ToDictionary(item => item.Name, item => item.Value.GetString()!, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        var dice = existing.Select(item => item.Payload).OfType<DiceRolled>().ToDictionary(item => item.Roll, StringComparer.Ordinal);
        var ordered = existing.Select(item => item.Payload).OfType<DiceRolled>().Select(item => item.Roll).ToList();
        if (ScenarioA1SequenceCalculator.ResumeBar(rollIds.Values.All(dice.ContainsKey)) is { } resumeBar)
        {
            return Refused(scope, label, expected, resumeBar);
        }

        var resumed = new ResumedRolls(rollIds,
            [.. rollIds.OrderBy(item => ordered.IndexOf(item.Value)).Select(item => (item.Key, (IReadOnlyList<int>)dice[item.Value].Values))]);
        var record = resume.TryGetProperty("record", out var recordName) ? recordName.GetString() : null;
        var answered = state with
        {
            Choice = null
        };
        Dictionary<string, string> With(IReadOnlyDictionary<string, string>? choices) =>
            new(choices ?? new Dictionary<string, string>(), StringComparer.Ordinal)
            {
                [key] = option
            };

        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent> { made };
            switch (record)
            {
                case "fire":
                    var fire = resume.GetProperty("facts").Deserialize<FireAttack>(LiveFire.Json)!;
                    int? step = resume.TryGetProperty("step", out var stepValue) ? stepValue.GetInt32() : null;
                    var followUps = resume.TryGetProperty("followUps", out var followNode) ? followNode.Deserialize<FireFollowUps>(LiveFire.Json) : null;
                    AddFireEvents(scope, attemptId, expected, actor, answered, fire with
                    {
                        Choices = With(fire.Choices)
                    }, resume.GetProperty("targetSide").GetString()!, step,
                        events, draw, resumed, followUps);
                    AddOverrunResolved(scope, attemptId, expected, answered, fire, events);

                    // Ruling R27.4: what the attack was to do next is done once its options are answered, then the Sniper checks its DRs call for (a
                    // DC's follow-ups make them with its removal; table player, pass 27).
                    if (followUps is not null)
                    {
                        AddFireFollowUps(scope, attemptId, expected, actor, existing, answered, resume.GetProperty("targetSide").GetString()!, step, events, draw, followUps);
                    }

                    if (followUps?.DcCharge is null)
                    {
                        AddSniperAttacks(scope, attemptId, expected, actor, existing, events, draw);
                    }

                    break;
                case "rally":
                    var rally = resume.GetProperty("facts").Deserialize<RallyAttempt>(LiveFire.Json)!;
                    var unit = answered.Unit(resume.GetProperty("unit").GetString()!)!;
                    string? leader = resume.TryGetProperty("leader", out var leaderValue) ? leaderValue.GetString() : null;
                    AddRallyEvents(scope, attemptId, expected, actor, answered, unit, leader, rally with
                    {
                        Choices = With(rally.Choices)
                    },
                        resume.TryGetProperty("withheld", out var withheld) && withheld.GetBoolean(), events, draw, resumed);
                    break;
                default:
                    var shot = resume.GetProperty("facts").Deserialize<OrdnanceShot>(LiveFire.Json)!;
                    UnitFacing? facing = resume.TryGetProperty("facing", out var facingValue) && UnitFacings.TryParse(facingValue.GetString(), out var parsed) ? parsed : null;
                    AddOrdnanceEvents(scope, attemptId, expected, actor, answered, shot with
                    {
                        Hit = shot.Hit! with
                        {
                            Choices = With(shot.Hit.Choices)
                        }
                    }, facing, events, draw,
                        resumed);
                    break;
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [summary])
        {
            Roll = new PlannedRoll("choice", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// A20.4 (ruling R5.7): in its own fire phase, a Russian or berserk Infantry unit not in Melee, that has not fired this phase, eliminates
    /// a prisoner in its Location as its attack, and is marked as having fired. Rules decides each bar (pass 32.i).
    /// </summary>
    private GamePlan PlanMassacre(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "prisonerId", out var prisonerId))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Massacre names the unit and the prisoner");
        }

        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || state.Unit(prisonerId) is not { Status: InstanceStatus.Active } prisoner
            || state.Location(unit.Id)?.Location is not { } at
            || !ScenarioA1SequenceCalculator.MassacreTarget(Is(prisoner, Conditions.Captured), prisoner.Side == unit.Side, state.Location(prisoner.Id)?.Location == at))
        {
            return Refused(scope, label, expected, ScenarioA1SequenceCalculator.MassacreTargetText(prisonerId, unitId));
        }

        if (ScenarioA1SequenceCalculator.MassacrePhaseBar(state.Phase, state.PhasingSide == unit.Side) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        var russian = unit.Definition is { } reference && FireReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.Nationality == "russian";
        if (ScenarioA1SequenceCalculator.MassacreUnitBar(vocabulary.IsA(unit.Kind, "asl:personnel"), Is(unit, Conditions.Melee), Is(unit, Conditions.Captured), russian,
            Is(unit, Conditions.Berserk), unit.Id) is { } unitBar)
        {
            return Refused(scope, label, expected, unitBar);
        }

        // A20.4 (ruling R5.7): a Massacre is made "as if using a SW", once per phase: a MMC keeps its inherent FP (A7.351), and a SMC forfeits
        // its own (A7.352), so it may not massacre after firing and is marked as having fired.
        var start = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Type == "phase-changed").index;
        if (ScenarioA1SequenceCalculator.MassacreOnceBar(existing.Skip(start).Any(item => item.Payload is PrisonersMassacred { Berserk: false } done && done.Units.Contains(unit.Id)),
            unit.Id) is { } onceBar)
        {
            return Refused(scope, label, expected, onceBar);
        }

        var smc = vocabulary.IsA(unit.Kind, "asl:smc");
        if (ScenarioA1SequenceCalculator.MassacreFiredBar(smc, LiveFire.Fired(unit), state.Phase, Is(unit, Conditions.FirstFire), unit.Id) is { } firedBar)
        {
            return Refused(scope, label, expected, firedBar);
        }

        var marker = ConditionName(ScenarioA1SequenceCalculator.MassacreMarker(state.Phase));
        var massacre = EventId(attemptId, 1);
        List<GameEvent> events = [Event(scope, attemptId, 1, expected, "prisoners-massacred", new PrisonersMassacred([unit.Id], [prisoner.Id], false), null, null)];
        if (smc)
        {
            events.Add(Event(scope, attemptId, 2, expected, "conditions-changed", new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [marker] = ConditionState.True }),
                null, null, [massacre]));
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [ScenarioA1SequenceCalculator.MassacreSummary(unit.Id, prisoner.Id, prisoner.Side)]);
    }

    /// <summary>Whether a unit massacred a prisoner in the PFPh of this Player Turn (A20.4, A7.351; ruling R5.7).</summary>
    private static bool MassacredInPrepFire(IReadOnlyList<GameEvent> existing, string unitId)
    {
        var start = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged { Phase: "pfph" }).index;
        return start > 0 && existing.Skip(start).Any(item => item.Payload is PrisonersMassacred { Berserk: false } done && done.Units.Contains(unitId));
    }

    /// <summary>
    /// A20.4 (ruling R5.7): at the start of a side's AFPh or DFPh (a berserk unit never fires in the PFPh, A15.432), each of its berserk units not in Melee that shares a Location with enemy prisoners
    /// eliminates them and returns to normal. The events follow the phase change. Rules names the side and the words (pass 32.i).
    /// </summary>
    private static IEnumerable<(PrisonersMassacred Massacre, string Reason)> BerserkMassacres(GameState state, string phase, string phasing)
    {
        var side = ScenarioA1SequenceCalculator.BerserkMassacringSide(phase, phasing, state.Sides.FirstOrDefault(item => item.Id != phasing)?.Id);
        if (side is null)
        {
            yield break;
        }

        foreach (var group in state.Units.Where(unit => ScenarioA1SequenceCalculator.BerserkMassacres(unit.Status == InstanceStatus.Active, unit.Side == side, Is(unit, Conditions.Berserk),
            Is(unit, Conditions.Melee), state.Location(unit.Id) is not null)).GroupBy(unit => state.Location(unit.Id)!.Location).OrderBy(group => group.Key.ToString(), StringComparer.Ordinal))
        {
            string[] prisoners = [.. state.At(group.Key).OfType<UnitInstance>()
                .Where(unit => ScenarioA1SequenceCalculator.MassacredPrisoner(unit.Status == InstanceStatus.Active, unit.Side != side, Is(unit, Conditions.Captured)))
                .Select(unit => unit.Id).Order(StringComparer.Ordinal)];
            if (prisoners.Length > 0)
            {
                string[] units = [.. group.Select(unit => unit.Id).Order(StringComparer.Ordinal)];
                yield return (new PrisonersMassacred(units, prisoners, true), ScenarioA1SequenceCalculator.BerserkMassacreText(units, prisoners, group.Key.ToString()));
            }
        }
    }
}
