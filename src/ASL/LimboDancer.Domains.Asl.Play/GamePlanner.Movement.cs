using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Hex-by-hex Infantry movement in the MPh (unit step 22): a stack enters an adjacent Location at its terrain's MF cost,
/// and after each MF expenditure the DEFENDER may fire at it (A8.1) or pass; the ATTACKER then continues or ends the move
/// (A8.11). A Location holding Residual FP attacks the stack as it enters, before the DEFENDER may fire (A8.22).
/// </summary>
public sealed partial class GamePlanner
{
    // MF to enter the admitted terrain, in half MF: Open Ground and orchard 1 (A4.13; B14.4), brush and woods 2 (B12.4,
    // B13.4), grain 1½ (B15.4), a building 2 (B23.4); entry across a road hexside 1 (A4.132, B3.4).
    private static readonly Dictionary<string, int> EntryHalfMf = new(StringComparer.Ordinal)
    {
        ["open-ground"] = 2,
        ["orchard"] = 2,
        ["brush"] = 4,
        ["woods"] = 4,
        ["grain"] = 3,
        ["wooden-building"] = 4,
        ["stone-building"] = 4,
    };

    private GamePlan PlanMove(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        var ids = Strings(arguments, "unitIds").ToArray();
        if (ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length || !Text(arguments, "to", out var toText)
            || !BoardLocation.TryParse(toText, out var to))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a move names its stack and the Location it enters");
        }

        var assault = arguments.TryGetProperty("assault", out var flag) && flag.ValueKind == JsonValueKind.True;
        if (state.Phase != "mph")
        {
            return Refused(scope, label, expected, "play.move-phase: units move in their side's MPh (A3.3, p. 47)");
        }

        var movers = ids.Select(id => state.Unit(id)).ToArray();
        if (movers.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side != state.PhasingSide))
        {
            return Refused(scope, label, expected, "play.move-stack: every mover is an active unit of the phasing side");
        }

        if (movers.Select(unit => state.Location(unit!.Id)?.Location).Distinct().ToArray() is not [{ } from])
        {
            return Refused(scope, label, expected, "play.move-stack: the stack moves from one Location (A4.2)");
        }

        // A4.1, A7.83: broken, pinned, or already-ended units do not move; A12.14: concealed movement is not reviewed.
        if (movers.Any(unit => GameState.Condition(unit!, Conditions.Broken) != ConditionState.False || GameState.Condition(unit!, Conditions.Pinned) == ConditionState.True
            || unit!.MovementEnded || unit.Kind == UnitKinds.Dummy
            || GameState.Condition(unit, Conditions.Concealed) == ConditionState.True || GameState.Condition(unit, Conditions.Hidden) == ConditionState.True))
        {
            return Refused(scope, label, expected, "play.move-unit: every mover is Good Order, unpinned, unconcealed, and not done moving (A4.1, A7.83)");
        }

        var current = state.Movement;
        if (current is not null && !current.Movers.Order(StringComparer.Ordinal).SequenceEqual(ids.Order(StringComparer.Ordinal)))
        {
            return Refused(scope, label, expected, $"play.move-order: {string.Join(", ", current.Movers)} are moving; end their move first (A8.11)");
        }

        if (current is { WindowOpen: true })
        {
            return Refused(scope, label, expected, "play.move-window: the DEFENDER may still fire at the stack's last MF expenditure (A8.1, A8.11)");
        }

        // A4.61: Assault Movement is declared before the stack moves, and moves it no more than one Location.
        if (current is not null && (current.Assault || assault))
        {
            return Refused(scope, label, expected, "play.move-assault: Assault Movement is declared at the start of the move and enters one Location (A4.61)");
        }

        var (fromRead, toRead, adjacent, crossed) = Step(state, from, to);
        if (fromRead is null || toRead is null || !adjacent || crossed is null)
        {
            return Refused(scope, label, expected, $"play.move-step: {to} is not an adjacent Location the map reads");
        }

        if (fromRead.Hex.BaseLevel + fromRead.Level.Level != toRead.Hex.BaseLevel + toRead.Level.Level || to.Level != 0
            || crossed.HexsideTerrain is not null || crossed.Cliff || crossed.Slope)
        {
            return Refused(scope, label, expected, "play.move-terrain: level changes and hexside terrain are not reviewed (ruling R22.3)");
        }

        if (TerrainKey(toRead) is not { } terrain || !EntryHalfMf.TryGetValue(terrain, out var halfMf))
        {
            return Refused(scope, label, expected, $"play.move-terrain: {toRead.Level.Terrain?.Name ?? "the terrain"} is not a reviewed entry (ruling R22.3)");
        }

        if (crossed.Terrain?.IsRoad == true)
        {
            halfMf = 2;
        }

        if (state.At(to).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide))
        {
            return Refused(scope, label, expected, $"play.move-occupied: entry into an enemy-occupied Location is the building entry's action, or not reviewed (ruling R22.6)");
        }

        // A4.11: the MF each mover has left; A4.61: Assault Movement may not use all of it.
        foreach (var unit in movers)
        {
            if (Experience.MfAllowance(state, unit!, catalogs, vocabulary) is not { } allowance)
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit!.Id} has no MF allowance the catalog decides");
            }

            var left = (allowance * 2) - (unit!.MfSpent * 2) - (unit.HalfMfSpent ? 1 : 0);
            if (left < halfMf || (assault && left <= halfMf))
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit.Id} has {left / 2m} MF left, and the entry costs {halfMf / 2m} (A4.11, A4.61)");
            }
        }

        var step = (current?.Step ?? 0) + 1;
        var moved = new MovementStepped(ids, to, halfMf, assault, step);
        var package = ScenarioA1FirePackage.Identity.ToString();
        var summary = $"play.move: {string.Join(", ", ids)} enter {to} ({terrain}) for {halfMf / 2m} MF" + (assault ? ", by Assault Movement" : string.Empty);
        var residual = state.ResidualFire.FirstOrDefault(item => item.Location == to);
        if (residual is null)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
                [Event(scope, attemptId, 1, expected, "movement-step", moved, package, null)], [summary]);
        }

        // A8.22: Residual FP attacks a unit entering its Location first, alone, with any FFNAM and FFMO.
        var stepEvent = Event(scope, attemptId, 1, expected, "movement-step", moved, package, null);
        if (Replay([.. existing, stepEvent]).Current is not { } entered
            || LiveFire.ResidualFromState(entered, to, residual.Fp) is not ({ } residualAttack, null)
            || FireMapFacts(entered, residualAttack, to) is not ({ } facts, null))
        {
            return Refused(scope, label, expected, "play.move-residual: the Residual FP attack on the entering stack cannot be read");
        }

        var precheck = ScenarioA1FireCalculator.Precheck(facts, FireReference.Value);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, ["play.move-residual: the Fire package does not decide the Residual FP attack this entry would suffer", .. precheck]);
        }

        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent> { stepEvent };
            AddFireEvents(scope, attemptId, expected, actor, entered, facts, state.PhasingSide, step, events, draw);
            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [summary, $"play.move: {residual.Fp} Residual FP in {to} attacks the stack first (A8.22)"])
        {
            Roll = new PlannedRoll("residual", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>The DEFENDER passes on the moving stack's latest MF expenditure (A8.11).</summary>
    private GamePlan PlanPassFire(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        return state.Movement is { WindowOpen: true } movement
            ? new GamePlan(GamePlanStatus.Ready, scope, label, expected,
                [Event(scope, attemptId, 1, expected, "movement-window-closed", new MovementWindowClosed(movement.Step), null, null)],
                [$"play.pass: the DEFENDER does not fire at {string.Join(", ", movement.Movers)} in {movement.Location}"])
            : Refused(scope, label, expected, "play.pass-window: no moving stack awaits the DEFENDER");
    }

    /// <summary>The ATTACKER ends the moving stack's move (A8.11): its units may not move again this MPh.</summary>
    private GamePlan PlanEndMove(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (state.Movement is not { } movement)
        {
            return Refused(scope, label, expected, "play.end-move: no stack is moving");
        }

        if (movement.WindowOpen)
        {
            return Refused(scope, label, expected, "play.end-move: the DEFENDER may still fire at the stack's last MF expenditure (A8.11)");
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "movement-ended", new MovementEnded(movement.Movers), null, null)],
            [$"play.end-move: {string.Join(", ", movement.Movers)} end their move in {movement.Location}"]);
    }
}
