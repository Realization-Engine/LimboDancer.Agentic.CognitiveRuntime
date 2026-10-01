using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The Victory Conditions of a game from a card (pass 21 of the Scenario Card Games Plan; rulings R21.1 to R21.5): the planner's reading of the game for
/// <see cref="ScenarioVictory"/>, the result recorded when the game ends or an immediate condition holds, and Infantry leaving the map (A2.6).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// The Victory Conditions read against a game's history (rulings R21.1 to R21.4), for <paramref name="knownTo"/> as that side may know them (ruling R23.4);
    /// null for a game with no structured ones.
    /// </summary>
    public VictoryReport? Victory(GameHistory history, bool? ended = null, string? knownTo = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        return history.Current is { Scenario: { } scenario } state && CardLibrary.Sha256(scenario.Id) == scenario.Sha256 && CardOf(state) is { } card && Valid(state, card)
            ? ScenarioVictory.Evaluate(card, history.States, unit => VictoryPoints(state, unit), at => Neighbors(state, at), ended ?? state.Ended is not null, knownTo)
            : null;
    }

    // Referee, pass 21: a card that no longer validates decides nothing; each card's validity is read once.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string Card, string Sha256, Units.Catalog.CatalogIdentity Catalog), bool> Validity = new();

    private bool Valid(GameState state, ScenarioCard card) => catalogs.FirstOrDefault(catalog => catalog.Identity == state.Catalog) is { } catalog
        && state.Scenario is { } scenario && Validity.GetOrAdd((card.Id, scenario.Sha256, catalog.Identity), _ => ScenarioCards.Validate(card, catalog).Count == 0);

    /// <summary>
    /// A unit's VP (A26.211; ruling R21.2): a squad or crew two, a HS one, a leader one plus one for each negative leadership modifier; a Hero none. Guns and
    /// vehicles (A26.212) are not counted yet (backlog).
    /// </summary>
    public int VictoryPoints(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        return unit.Kind switch
        {
            "asl:squad" or "asl:crew" => 2,
            "asl:half-squad" => 1,
            "asl:leader" => 1 + Math.Max(0, -(unit.Definition is { } reference
                ? catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:leadership")?.Value?.Number ?? 0
                : 0)),
            _ => 0,
        };
    }

    /// <summary>
    /// A game ended at once (ruling R21.4): the events of a plan, with <c>game-ended</c> added when an immediate Victory Condition holds after them; the events
    /// unchanged otherwise, or when they already end the game.
    /// </summary>
    private IReadOnlyList<GameEvent> WithImmediateVictory(GameScope scope, string attemptId, long expected, IReadOnlyList<GameEvent> existing, IReadOnlyList<GameEvent> events)
    {
        if (events.Count == 0 || events[^1].Payload is GameEnded || existing.Count == 0 || existing[0].Payload is not GameStarted { Scenario: { } scenario } started)
        {
            return events;
        }

        // Only a card with an immediate condition is read after every action (Gambit's Exit VP).
        if (catalogs.FirstOrDefault(catalog => $"{catalog.Identity.Catalog}@{catalog.Identity.Version}" == started.Catalog) is not { } catalog
            || CachedCard(scenario.Id, catalog) is not { Card.VictoryConditions.Outcomes: { } outcomes }
            || !outcomes.Any(outcome => outcome.Immediate))
        {
            return events;
        }

        var history = Replay([.. existing, .. events]);
        // A26.222 (referee, pass 21): during play captured units count their normal VP; and the game ends only with nothing left open (UNIT-STATE-045).
        return history.Current is { SetupClosed: true, OpenAttempts.Count: 0, Choice: null, PendingSurrenders.Count: 0 } after && !after.CloseCombats.Any(item => !item.Closed)
            && Victory(history, ended: false) is { Immediate: { } result }
            ? [.. events, Event(scope, attemptId, events.Count + 1, expected, "game-ended", new GameEnded(after.Turn, "victory") { Result = result }, rulePackage: null, visibility: null)]
            : events;
    }

    /// <summary>
    /// Infantry leaving the map (A2.6; ruling R21.5): a Good Order stack of the phasing side in its MPh, the whole moving stack when one moves, from a
    /// ground-level edge hex within the playable area, across that edge, as if entering the mirror-image hex beyond: the MF of its own hex's terrain. The
    /// units are Exited and may not return. Leaving by Bypass or in the APh is not built.
    /// </summary>
    private GamePlan PlanExit(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string edge)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        var ids = Strings(arguments, "unitIds").ToArray();
        if (state.Phase != "mph")
        {
            return Refused(scope, label, expected, "play.exit-phase: units leave the map in their side's MPh; leaving in the APh is not built (A2.6; ruling R21.5)");
        }

        var movers = ids.Select(state.Unit).ToArray();
        if (ids.Length == 0 || movers.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side != state.PhasingSide || LiveFire.IsVehicle(unit)))
        {
            return Refused(scope, label, expected, "play.exit-stack: every unit leaving is an active Infantry unit of the phasing side; a vehicle leaves by its own move (A2.6)");
        }

        if (movers.Select(unit => state.Location(unit!.Id)?.Location).Distinct().ToArray() is not [{ } from])
        {
            return Refused(scope, label, expected, "play.exit-stack: the stack leaves from one Location on the map (A2.6)");
        }

        if (movers.FirstOrDefault(unit => Is(unit!, Conditions.Broken) || Is(unit!, Conditions.Pinned) || Is(unit!, Conditions.Berserk) || Is(unit!, Conditions.Melee)
            || Is(unit!, Conditions.Captured) || Is(unit!, Conditions.Hidden) || Is(unit!, "asl:ti") || unit!.MovementEnded || Is(unit!, Conditions.PrepFire)) is { } unable)
        {
            return Refused(scope, label, expected, $"play.exit-unit: {unable.Id} is not free to move this MPh (A4.1, A3.3)");
        }

        // Referee, pass 21: a crew manning a Gun and a Guard with prisoners do not leave the map (C10.3, A20.53: not built).
        if (movers.FirstOrDefault(unit => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Manned } holding && holding.Holder == unit!.Id)
            || IsGuard(state, unit!)) is { } bound)
        {
            return Refused(scope, label, expected, $"play.exit-unit: {bound.Id} mans a Gun or guards prisoners; leaving the map with them is not built (C10.3, A20.53; ruling R21.5)");
        }

        if (state.Movement is { } current && (current.WindowOpen || current.Bypass is { Count: > 0 } || !current.Members.ToHashSet(StringComparer.Ordinal).SetEquals(ids)))
        {
            return Refused(scope, label, expected, current.WindowOpen
                ? "play.move-window: the DEFENDER may still fire at the stack's last MF expenditure (A8.1, A8.11)"
                : "play.exit-stack: the whole moving stack leaves together, never from Bypass (A2.6, A4.2; ruling R21.5)");
        }

        if (from.Level != 0 || !EdgeSides(state, from).Any(item => item.Edge == edge) || PlayableBar(state, from) is not null)
        {
            return Refused(scope, label, expected, $"play.exit-edge: {from} is not a ground-level hex of the {edge} edge within the playable area (A2.6; rulings R20.6, R21.5)");
        }

        if (ReadLocation(state, from) is not { } read || TerrainKey(read) is not { } terrain || InfantryEntryHalfMf(state, terrain) is not { } halfMf)
        {
            return Refused(scope, label, expected, $"play.exit-terrain: the cost of leaving {from} is not decided (A2.6; ruling R21.5)");
        }

        var doubleTime = Flag(arguments, "doubleTime");
        foreach (var unit in movers)
        {
            var extra = doubleTime ? (unit!.MfSpent == 0 && !unit.HalfMfSpent ? 2 : 1) : unit!.DoubleTimeMf;
            if (MfAllotment(state, unit, extra, doubleTime || Is(unit, Conditions.Cx)) is not { } allowance)
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit.Id} has no MF allowance the catalog decides");
            }

            var left = (allowance * 2) - ((unit.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0));
            if (left < halfMf)
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit.Id} has {left / 2m} MF left, and leaving the map from {from} costs {halfMf / 2m} (A2.6, A4.11)");
            }
        }

        var step = new MovementStepped(ids, from, halfMf, false, (state.Movement?.Step ?? 0) + 1) { Exit = edge, DoubleTime = doubleTime };
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "movement-step", step, ScenarioA1.ScenarioA1FirePackage.Identity.ToString(), null)],
            [$"play.exit: {string.Join(", ", ids)} leave the map from {from} across the {edge} edge for {halfMf / 2m} MF and may not return (A2.6; ruling R21.5)"]);
    }
}
