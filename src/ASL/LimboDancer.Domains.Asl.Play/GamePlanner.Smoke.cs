using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// SMOKE grenades (backlog pass 9, rulings R9.5 and R9.6): in its MPh a Good Order squad with a Smoke Placement Exponent, in the moving stack,
/// names its own Location (1 MF) or an ADJACENT one at its level (2 MF); a dr at most the exponent places a SMOKE counter there, which leaves
/// at the end of the MPh; a dr of 6 ends the squad's MPh (A24.1, A24.11). The MF are the stack's expenditure in its Location, which opens the
/// DEFENDER's window (A8.1).
/// </summary>
public sealed partial class GamePlanner
{
    private GamePlan PlanSmoke(GameScope scope, string attemptId, long expected, string label, string actor, GameState state, UnitInstance[] movers,
        IReadOnlyList<string> ids, BoardLocation from, string placerId, string targetText, bool assault, bool doubleTime)
    {
        var current = state.Movement;
        if (!BoardLocation.TryParse(targetText, out var target))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a SMOKE placement names its Location");
        }

        // Pass 32.a (the worked action): the planner reads the facts and ScenarioA1SmokeCalculator decides, in the order the checks had here.
        var placer = movers.FirstOrDefault(unit => unit.Id == placerId);
        var targetRead = ReadLocation(state, target);
        var (fromRead, toRead, adjacent, crossed) = target == from ? (null, null, false, null) : Step(state, from, target);
        var facts = new SmokeAttemptFacts(placerId, placer is not null, placer?.Kind == "asl:squad", placer is not null && Is(placer, Conditions.Berserk),
            placer is null ? null : SmokeExponent(placer), placer is not null && state.SmokeAttempts.Contains(placer.Id, StringComparer.Ordinal),
            state.ResidualFire.Any(item => item.Location == from), state.Precipitation, state.Weather("mud"), state.Weather("deep-snow"),
            targetRead is null ? null : TerrainKey(targetRead), toRead is null ? null : TerrainKey(toRead), target.ToString(), target == from,
            fromRead is not null && toRead is not null && adjacent && crossed is not null,
            fromRead is not null && toRead is not null && fromRead.Hex.BaseLevel + fromRead.Level.Level == toRead.Hex.BaseLevel + toRead.Level.Level,
            target.Level == from.Level, crossed?.Cliff == true, assault,
            [.. movers.Select(unit =>
            {
                var extra = doubleTime ? (unit.MfSpent == 0 && !unit.HalfMfSpent ? 2 : 1) : unit.DoubleTimeMf;
                var exhausted = doubleTime || Is(unit, Conditions.Cx);
                return new SmokeMoverFacts(unit.Id, MfAllotment(state, unit, extra, exhausted), MfAllotment(state, unit, 0, exhausted), (unit.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0));
            })]);
        var verdict = ScenarioA1SmokeCalculator.Plan(facts);
        if (verdict.Refusal is { } refusal)
        {
            return Refused(scope, label, expected, refusal);
        }

        // The verdict passed, so the placer is a squad of the stack with an exponent.
        var halfMf = verdict.HalfMf;
        var exponent = facts.Exponent!.Value;
        var squad = placer!;

        var step = (current?.Step ?? 0) + 1;
        var package = ScenarioA1FirePackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            events.Add(Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "smoke-placement", 1, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
            // A4.51 (table player, pass 9): +1 to the dr of a CX squad, or one Double Timing with this step.
            var attempt = new SmokeAttempt(squad.Id, target, rollId, drawn.Values[0], exponent) { Cx = doubleTime || Is(squad, Conditions.Cx) };
            events.Add(Event(scope, attemptId, 2, expected, "movement-step", new MovementStepped(ids, from, halfMf, current?.Assault ?? assault, step)
            {
                DoubleTime = doubleTime,
                Smoke = attempt,
            }, package, null));
            if (attempt.Placed)
            {
                events.Add(Event(scope, attemptId, 3, expected, "instance-created", new InstanceCreated(new NewInstance(attemptId + GameState.SmokeGrenadeSuffix, "asl:smoke", null, null,
                    new MapPosition(target), null, new Dictionary<string, ConditionState>(StringComparer.Ordinal))), package, null, [EventId(attemptId, 2)]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.smoke: {squad.Id} attempts to place SMOKE grenades in {target} for {halfMf / 2m} MF of the stack; a dr of {exponent} or less{(doubleTime || Is(squad, Conditions.Cx) ? " (+1 while CX)" : string.Empty)} places a +2 SMOKE counter until the end of this MPh, a 6 ends {squad.Id}'s MPh (A24.1, A24.11)"])
        {
            Roll = new PlannedRoll("smoke", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>A squad's Smoke Placement Exponent (A1.21, A24.1) as its catalog definition prints it; null when none is printed.</summary>
    public int? SmokeExponent(UnitInstance unit) => unit.Definition is { } reference
        ? catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:smoke-exponent")?.Value?.Number
        : null;
}
