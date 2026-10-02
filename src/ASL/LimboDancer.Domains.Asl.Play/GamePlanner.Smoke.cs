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

        // A24.1: a squad of the moving stack with a Smoke Placement Exponent, once per MPh, not berserk (A15.43).
        if (movers.FirstOrDefault(unit => unit.Id == placerId) is not { } placer || placer.Kind != "asl:squad" || Is(placer, Conditions.Berserk)
            || SmokeExponent(placer) is not { } exponent)
        {
            return Refused(scope, label, expected, $"play.smoke-placer: {placerId} is not a squad of the moving stack with a Smoke Placement Exponent (A24.1)");
        }

        if (state.SmokeAttempts.Contains(placer.Id, StringComparer.Ordinal))
        {
            return Refused(scope, label, expected, $"play.smoke-once: {placer.Id} has already attempted to place SMOKE this MPh (A24.1)");
        }

        if (state.ResidualFire.Any(item => item.Location == from))
        {
            return Refused(scope, label, expected, "play.smoke-residual: spending MF in a Residual FP Location to place SMOKE is not reviewed (A8.2)");
        }

        // E3.53, E3.734 (referee, pass 16): in rain, Mud, or Deep Snow the only SMOKE is a Blaze's or SMOKE placed inside a building.
        if ((state.Precipitation is "rain" or "heavy-rain" || state.Weather("mud") || state.Weather("deep-snow"))
            && !(ReadLocation(state, target) is { } smokeRead && TerrainKey(smokeRead) is "wooden-building" or "stone-building"))
        {
            return Refused(scope, label, expected, "play.smoke-weather: in rain, Mud, or Deep Snow no SMOKE is placed but inside a building (E3.53, E3.734)");
        }

        // A24.1 (ruling R9.5): the own Location for 1 MF, or an ADJACENT Location at its level for 2 MF; no other level, water, or marsh.
        int halfMf;
        if (target == from)
        {
            halfMf = 2;
        }
        else
        {
            var (fromRead, toRead, adjacent, crossed) = Step(state, from, target);
            if (fromRead is null || toRead is null || !adjacent || crossed is null || fromRead.Hex.BaseLevel + fromRead.Level.Level != toRead.Hex.BaseLevel + toRead.Level.Level
                || target.Level != from.Level || crossed.Cliff || TerrainKey(toRead) is not { } terrain || !EntryHalfMf.ContainsKey(terrain))
            {
                return Refused(scope, label, expected, $"play.smoke-target: SMOKE grenades go in the squad's Location or an ADJACENT reviewed Location at its level, not {target} (A24.1; ruling R9.5)");
            }

            halfMf = 4;
        }

        foreach (var unit in movers)
        {
            var extra = doubleTime ? (unit.MfSpent == 0 && !unit.HalfMfSpent ? 2 : 1) : unit.DoubleTimeMf;
            var exhausted = doubleTime || Is(unit, Conditions.Cx);
            if (MfAllotment(state, unit, extra, exhausted) is not { } allowance || MfAllotment(state, unit, 0, exhausted) is not { } plain)
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit.Id} has no MF allowance the catalog decides");
            }

            var spent = (unit.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0);
            var left = (allowance * 2) - spent;
            if (left < halfMf || (assault && (plain * 2) - spent <= halfMf))
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit.Id} has {left / 2m} MF left, and the SMOKE attempt costs {halfMf / 2m} (A24.1, A4.61)");
            }
        }

        var step = (current?.Step ?? 0) + 1;
        var package = ScenarioA1FirePackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            events.Add(Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "smoke-placement", 1, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
            // A4.51 (table player, pass 9): +1 to the dr of a CX squad, or one Double Timing with this step.
            var attempt = new SmokeAttempt(placer.Id, target, rollId, drawn.Values[0], exponent) { Cx = doubleTime || Is(placer, Conditions.Cx) };
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
            [$"play.smoke: {placer.Id} attempts to place SMOKE grenades in {target} for {halfMf / 2m} MF of the stack; a dr of {exponent} or less{(doubleTime || Is(placer, Conditions.Cx) ? " (+1 while CX)" : string.Empty)} places a +2 SMOKE counter until the end of this MPh, a 6 ends {placer.Id}'s MPh (A24.1, A24.11)"])
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
