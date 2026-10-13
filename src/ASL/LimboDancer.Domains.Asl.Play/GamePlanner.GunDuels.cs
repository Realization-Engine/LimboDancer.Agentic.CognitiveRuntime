using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Gun Duels (C2.2401, pp. 167 and 168; pass 55, rulings R55.19 and R55.20). The DEFENDER's Defensive First Fire ordnance shot at the moving
/// vehicle is a declaration when that vehicle may fire its MA at the firer first: the commit is a pending choice for the ATTACKER that holds the
/// declared shot's arguments and no roll. On <c>decline</c> the declared shot resolves; on <c>duel</c> one commit holds the vehicle's shot, then
/// the declared shot planned again against the state the first left, or the record that it is void. <see cref="ScenarioA1GunDuels"/> decides
/// whether a duel may be declared and words it; this file reads the state, plans both shots through the ordnance shot's own facts, and writes the
/// events.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The key a shot's resume data carries the declared shot under, when the shot is a Gun Duel's first and stopped at a choice.</summary>
    private const string GunDuelOwed = "gunDuelOwed";

    /// <summary>
    /// The request of the moving vehicle's answer (C2.2401): its MA at the firer's Location, on the Vehicle Target Type when the firer is a
    /// vehicle, with the default ammunition (ruling R55.20). Null when the firer is not on the map.
    /// </summary>
    private static JsonElement? GunDuelAnswerArguments(GameState state, string vehicle, string firer)
    {
        if (state.Location(firer)?.Location is not { } firerAt)
        {
            return null;
        }

        var arguments = new JsonObject { ["gunId"] = vehicle, ["target"] = firerAt.ToString() };
        if (state.Find(firer) is UnitInstance)
        {
            arguments["targetVehicle"] = firer;
        }

        return JsonSerializer.SerializeToElement(arguments);
    }

    /// <summary>
    /// The declaration of a shot the moving vehicle may answer with a Gun Duel (C2.2401; ruling R55.19), or null when none may be declared and
    /// the shot resolves as it always did. The facts are the state's; Rules decides, from the declared shot and the vehicle's potential shot,
    /// which is planned as the shot itself would be.
    /// </summary>
    private GamePlan? GunDuelDeclaration(GameScope scope, GameState state, JsonElement arguments, OrdnanceShot declared, UnitFacing? facing, string attemptId, long expected,
        string label)
    {
        if (declared.FireKind != "first-fire" || declared.VehicleTarget?.VehicleId is not { } vehicleId || declared.Gun?.GunId is not { } firerId
            || state.Movement is not { WindowOpen: true } movement || state.Find(firerId) is not { } firer || state.Location(firerId)?.Location is not { } firerAt)
        {
            return null;
        }

        var facts = new GunDuelFacts(movement.Vehicle && movement.Movers.Contains(vehicleId),
            Is(firer, Conditions.Concealed) || Is(firer, Conditions.Hidden),
            // D7.2: Reaction Fire is the fire of the units in the Location an OVR was resolved in, in the window that follows it.
            movement.Reaction && firerAt == movement.Location,
            movement.Overrun is not null,
            // C5.3: the MA's shots of this MPh are kept with the movement step each followed; one at the step now is a duel's.
            state.VehicleWeaponShots.Any(item => item.Vehicle == vehicleId && item.Weapon == "ma" && item.FirePhase == "mph" && item.Step == movement.Step))
        {
            // D3.41 (ruling R55.29): an Armor Leader no record has revealed is not counted at the DEFENDER's declaration.
            UnrevealedArmorLeader = state.Unit(vehicleId)?.ArmorLeader is { Revealed: false },
        };
        var verdict = ScenarioA1GunDuels.Verdict(facts, declared,
            () => GunDuelAnswerArguments(state, vehicleId, firerId) is { } answer ? OrdnanceShotFacts(state, answer, gunDuel: true).Shot?.Facts : null, OrdnanceReference.Value);
        if (!verdict.Offered)
        {
            return null;
        }

        var resume = new JsonObject
        {
            ["record"] = ChoicePending.GunDuel,
            ["shot"] = JsonNode.Parse(arguments.GetRawText()),
            ["vehicle"] = vehicleId,
            ["firer"] = firerId,
            ["firerAt"] = firerAt.ToString(),
            ["step"] = movement.Step,
            ["attacker"] = JsonSerializer.SerializeToNode(verdict.Attacker, LiveFire.Json),
            ["defender"] = JsonSerializer.SerializeToNode(verdict.Defender, LiveFire.Json),
        };

        // The request's own fields are no part of the declared shot.
        var shot = resume["shot"]!.AsObject();
        foreach (var name in new[] { "gameId", "attemptId", "expectedRevision" })
        {
            shot.Remove(name);
        }

        var key = $"{ChoicePending.GunDuel}:{firerId}:{vehicleId}:{expected.ToString(CultureInfo.InvariantCulture)}";
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "choice-pending", new ChoicePending(key, ChoicePending.GunDuel, state.PhasingSide!, [ChoicePending.Duel, ChoicePending.Decline],
                JsonSerializer.SerializeToElement(resume)), null, null)],
            [OrdnanceSummary(declared, facing), ScenarioA1GunDuels.ProposalText(vehicleId, firerId, verdict.Attacker!, verdict.Defender!)]);
    }

    /// <summary>What a pending Gun Duel asks, from its resume data.</summary>
    private static string DescribeGunDuel(JsonElement resume)
    {
        if (!Text(resume, "vehicle", out var vehicle) || !Text(resume, "firer", out var firer) || !Text(resume, "firerAt", out var firerAt)
            || !resume.TryGetProperty("attacker", out var attacker) || attacker.Deserialize<GunDuelDrm>(LiveFire.Json) is not { } attackerDrm
            || !resume.TryGetProperty("defender", out var defender) || defender.Deserialize<GunDuelDrm>(LiveFire.Json) is not { } defenderDrm)
        {
            return "a shot was declared at a moving vehicle, which may fire its MA first (C2.2401)";
        }

        return ScenarioA1GunDuels.ChoiceText(firer, firerAt, vehicle, attackerDrm, defenderDrm);
    }

    /// <summary>
    /// The ATTACKER's answer to a declared shot (C2.2401; ruling R55.19). <c>decline</c>: the declared shot is planned from its arguments and
    /// resolves. <c>duel</c>: the vehicle's MA fires first as Bounding First Fire, and the declared shot is planned again against the state that
    /// shot left: resolved, or recorded as void when the checks every shot passes refuse it.
    /// </summary>
    private GamePlan PlanGunDuelAnswer(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor, PendingChoice pending,
        GameEvent made, string option, string summary)
    {
        var resume = pending.Resume;
        if (!resume.TryGetProperty("shot", out var declared) || !Text(resume, "vehicle", out var vehicle) || !Text(resume, "firer", out var firer)
            || Replay([.. existing, made]).Current is not { } answered)
        {
            return Refused(scope, label, expected, "play.gun-duel: the pending Gun Duel cannot be read");
        }

        var answer = "play.gun-duel: " + ScenarioA1GunDuels.AnswerText(option, vehicle, firer);
        var (returned, returnRefusal) = OrdnanceShotFacts(answered, declared);
        if (option != ChoicePending.Duel)
        {
            // Nothing happens between the declaration and its answer, so the declared shot is accepted now as it was then; should it not be, the
            // answer alone is recorded, and the game goes on.
            if (returned is not { } shot)
            {
                return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [made],
                    [summary, answer, $"play.gun-duel: the declared shot is not made ({string.Join("; ", returnRefusal ?? [])})"]);
            }

            IReadOnlyList<GameEvent> Resolve(Func<RollRequest, RollResult> draw)
            {
                var events = new List<GameEvent> { made };
                AddOrdnanceEvents(scope, attemptId, expected, actor, answered, shot.Facts, shot.Facing, events, draw);
                return events;
            }

            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [summary, answer, OrdnanceSummary(shot.Facts, shot.Facing)])
            {
                Roll = new PlannedRoll("choice", Resolve),
                FirstEventId = EventId(attemptId, 1),
            };
        }

        if (GunDuelAnswerArguments(answered, vehicle, firer) is not { } attackArguments)
        {
            return Refused(scope, label, expected, ScenarioA1GunDuels.NoLongerText(vehicle, $"{firer} is not on the map"));
        }

        var (first, firstRefusal) = OrdnanceShotFacts(answered, attackArguments, gunDuel: true);
        if (first is not { } attack)
        {
            return Refused(scope, label, expected, ScenarioA1GunDuels.NoLongerText(vehicle, string.Join("; ", firstRefusal ?? [])));
        }

        var owed = JsonSerializer.SerializeToElement(new JsonObject { ["shot"] = JsonNode.Parse(declared.GetRawText()), ["vehicle"] = vehicle, ["firer"] = firer });
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent> { made };
            AddOrdnanceEvents(scope, attemptId, expected, actor, answered, attack.Facts, attack.Facing, events, draw, owedShot: owed);
            AddGunDuelReturnShot(scope, attemptId, expected, actor, existing, events, draw, owed);
            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [summary, answer, OrdnanceSummary(attack.Facts, attack.Facing), .. returned is { } after ? new[] { OrdnanceSummary(after.Facts, after.Facing) } : Array.Empty<string>()])
        {
            Roll = new PlannedRoll("choice", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// The declared shot after a Gun Duel's first shot (C2.2401): unless that shot stopped at a choice, which then carries it, the declared shot
    /// is planned again from its arguments against the state the events so far leave. "The lower ... fires first, and voids the opponent's return
    /// shot by eliminating, breaking, stunning or shocking it": a firer the checks of every shot now refuse makes none, and the record says why.
    /// </summary>
    private void AddGunDuelReturnShot(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, List<GameEvent> events,
        Func<RollRequest, RollResult> draw, JsonElement owed)
    {
        if (events[^1].Payload is ChoicePending || !owed.TryGetProperty("shot", out var declared) || !Text(owed, "vehicle", out var vehicle) || !Text(owed, "firer", out var firer)
            || Replay([.. existing, .. events]).Current is not { } after)
        {
            return;
        }

        if (OrdnanceShotFacts(after, declared).Shot is { } shot)
        {
            AddOrdnanceEvents(scope, attemptId, expected, actor, after, shot.Facts, shot.Facing, events, draw);
            return;
        }

        var gun = after.Find(firer);
        var crew = gun switch
        {
            UnitInstance tank => tank,
            EquipmentInstance { Holding: { } holding } => after.Unit(holding.Holder),
            _ => null,
        };
        var cause = ScenarioA1GunDuels.VoidCause(crew is not { Status: InstanceStatus.Active }, crew is not null && Is(crew, Conditions.Broken),
            crew is not null && Is(crew, Conditions.Stunned), crew is not null && (Is(crew, Conditions.Shocked) || Is(crew, Conditions.UnconfirmedKill)),
            gun is not { Status: InstanceStatus.Active } || Is(gun, Conditions.Malfunctioned));
        IReadOnlyList<string>? causes = events.LastOrDefault(item => item.Payload is OrdnanceFired) is { } firstShot ? [firstShot.EventId] : null;
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "gun-duel-voided", new GunDuelVoided(firer, vehicle, cause), null, null, causes));
    }
}
