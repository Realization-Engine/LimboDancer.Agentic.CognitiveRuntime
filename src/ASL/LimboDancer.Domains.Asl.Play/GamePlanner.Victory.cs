using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Rules;
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
        return Victory(history, ended, knownTo, history.Events.Count);
    }

    /// <summary>The Victory Conditions read with the game's Control fold cached up to <paramref name="stored"/> events, those already in its log (ruling R24.6).</summary>
    private VictoryReport? Victory(GameHistory history, bool? ended, string? knownTo, int stored)
    {
        if (history.Current is not { Scenario: { } scenario } state || !CardLibrary.Matches(scenario.Id, scenario.Sha256) || CardOf(state) is not { } card || !Valid(state, card))
        {
            return null;
        }

        var first = history.Events[0];
        var reading = new VictoryReading
        {
            Levels = hex => HexLevels(state, hex),
            ArmedVehicle = vehicle => HasInherentCrew(state, vehicle),
            EventIds = [.. history.Events.Select(item => $"{item.EventId}@{item.Time.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture)}")],
            CacheUpTo = stored,
            Cache = VictoryCaches.GetOrAdd($"{first.Scope}|{first.EventId}|{first.Time.UtcTicks}|{scenario.Sha256}", _ => new VictoryCache()),
        };
        return ScenarioVictory.Evaluate(card, history.States, unit => VictoryPoints(state, unit), at => Neighbors(state, at), ended ?? state.Ended is not null, knownTo, reading);
    }

    // Ruling R24.6 (the referee, pass 21): each game's Control fold is kept, so a reading folds only the states added since the last.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, VictoryCache> VictoryCaches = new(StringComparer.Ordinal);

    /// <summary>
    /// The levels of a hex's Locations (ground 0 and upper levels), rooftops (A26.14) and cellars left out: cellars have no other use in the game (B23.41;
    /// table player, pass 24; ruling R24.1); ground level when unread.
    /// </summary>
    private IReadOnlyList<int> HexLevels(GameState state, BoardLocation hex) =>
        ScenarioA1VictoryCalculator.HexLevels(ReadLocation(state, hex with
        {
            Level = 0
        })?.Hex.Locations.Select(item => (item.Terrain?.Name, item.Level)));

    // Referee, pass 21: a card that no longer validates decides nothing; each card's validity is read once.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string Card, string Sha256, Units.Catalog.CatalogIdentity Catalog), bool> Validity = new();

    private bool Valid(GameState state, ScenarioCard card) => catalogs.FirstOrDefault(catalog => catalog.Identity == state.Catalog) is { } catalog
        && state.Scenario is { } scenario && Validity.GetOrAdd((card.Id, scenario.Sha256, catalog.Identity), _ => ScenarioCards.Validate(card, catalog).Count == 0);

    /// <summary>
    /// A unit's VP (A26.211, A26.212; rulings R21.2, R24.3): a squad or crew two, a HS one, a leader one plus one for each negative leadership modifier; a
    /// Hero none; a vehicle one, plus one for a MA not malfunctioned, one per five AF of its strongest (FRU, a 0 AF one; none unarmored), and two for its
    /// inherent crew while it has one. A Gun's two VP are <see cref="ScenarioVictory.GunVp"/>.
    /// </summary>
    public int VictoryPoints(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        return ScenarioA1VictoryCalculator.VictoryPoints(unit.Kind,
            () => unit.Definition is { } reference
                ? catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:leadership")?.Value?.Number ?? 0
                : 0,
            () => VehicleVictoryPoints(state, unit));
    }

    /// <summary>
    /// A26.212 (ruling R24.3): one VP, one for a MA not malfunctioned, one per multiple of five AF of the vehicle's single strongest AF, rounded up (a 0 AF
    /// one; an unarmored vehicle none), and the inherent crew's two (A26.211) unless it has left the vehicle, Abandoned or as a crew counter of its own. An
    /// unarmored vehicle with no armament has no inherent crew (A26.212 EX: an unarmed truck is worth one).
    /// </summary>
    private static int VehicleVictoryPoints(GameState state, UnitInstance vehicle)
    {
        var definition = VehicleDefinition(vehicle);
        var armor = vehicle.Definition is { } reference ? OrdnanceReference.Value.Armor.Vehicles.GetValueOrDefault(reference.Definition) : null;

        // The MA is a Gun (its type, such as "t") or a MG the catalog names as the MA (a SPW 251/1's AAMG); not malfunctioned, nor disabled (a vehicle MG's
        // condition, so only a MG MA loses its point to it).
        return ScenarioA1VictoryCalculator.VehicleVictoryPoints(armor?.MaType is not null || definition?.MainArmament is not null, armor?.MaType is not null,
            Is(vehicle, Conditions.Malfunctioned), Is(vehicle, Conditions.Disabled), armor is { Unarmored: false },
            armor is null ? [] : [armor.FrontAf, armor.SideAf, Rules.ScenarioA1ArmorReference.ArmorFactor(armor, "turret", "front"), Rules.ScenarioA1ArmorReference.ArmorFactor(armor, "turret", "side")],
            () => HasInherentCrew(state, vehicle));
    }

    /// <summary>
    /// Whether a vehicle has its inherent crew (A26.211, D5.1; rulings R24.3, R24.5): an armed vehicle (a MA or any MG) has one, an unarmed vehicle only an
    /// Inherent Driver; it is gone once the vehicle is Abandoned or its crew is a counter of its own.
    /// </summary>
    private static bool HasInherentCrew(GameState state, UnitInstance vehicle)
    {
        var definition = VehicleDefinition(vehicle);
        var armor = vehicle.Definition is { } reference ? OrdnanceReference.Value.Armor.Vehicles.GetValueOrDefault(reference.Definition) : null;
        return ScenarioA1VictoryCalculator.HasInherentCrew(
            ScenarioA1VictoryCalculator.VehicleArmed(armor?.MaType is not null, definition?.MainArmament is not null, definition?.AntiAircraftMg is not null, definition?.BowMg is not null,
                definition?.CoaxialMg is not null),
            Is(vehicle, Conditions.Abandoned), () => state.Units.Any(unit => unit.Kind == "asl:crew" && unit.Id.EndsWith($"-{vehicle.Id}-crew", StringComparison.Ordinal)));
    }

    /// <summary>
    /// A game ended at once (ruling R21.4): the events of a plan, with <c>game-ended</c> added when an immediate Victory Condition holds after them; the events
    /// unchanged otherwise, or when they already end the game.
    /// </summary>
    private IReadOnlyList<GameEvent> WithImmediateVictory(GameScope scope, string attemptId, long expected, IReadOnlyList<GameEvent> existing, IReadOnlyList<GameEvent> events)
    {
        var started = existing.Count > 0 ? existing[0].Payload as GameStarted : null;
        // Only a card with an immediate condition is read after every action (Gambit's Exit VP).
        if (!ScenarioA1VictoryCalculator.ImmediateVictoryRead(events.Count > 0, events.Count > 0 && events[^1].Payload is GameEnded, started?.Scenario is not null,
            () => Units.Catalog.UnitCatalogs.For(catalogs, started!.Catalog) is { } catalog
                && CachedCard(started.Scenario!.Id, catalog) is { Card.VictoryConditions.Outcomes: { } outcomes } && outcomes.Any(outcome => outcome.Immediate)))
        {
            return events;
        }

        var history = Replay([.. existing, .. events]);
        // A26.222 (referee, pass 21): during play captured units count their normal VP; and the game ends only with nothing left open (UNIT-STATE-045).
        return history.Current is { } after
            && ScenarioA1VictoryCalculator.NothingLeftOpen(after.SetupClosed, after.OpenAttempts.Count, after.Choice is not null, after.PendingSurrenders.Count, after.CloseCombats.Any(item => !item.Closed))
            && Victory(history, false, null, existing.Count) is { Immediate: { } result }
            ? [.. events, Event(scope, attemptId, events.Count + 1, expected, "game-ended", new GameEnded(after.Turn, "victory") { Result = result }, rulePackage: null, visibility: null)]
            : events;
    }

    /// <summary>
    /// What an exit counts for (A26.23, A26.221; table player, pass 25): toward the side's Exit VP when it meets an exit condition of the side's from this hex,
    /// otherwise as elimination for the enemy's CVP, but an escorting Guard; empty for a card whose Victory Conditions name no outcome.
    /// </summary>
    private string ExitScoring(GameState state, string side, BoardLocation from, string edge, bool escorting)
    {
        var card = CardOf(state);
        var outcomes = card?.VictoryConditions.Outcomes is { Count: > 0 } list ? list : null;
        // Referee, pass 25: a Guard may leave by any edge with its prisoners, which stay captured; only off a Friendly Board Edge or its exit condition's edge is
        // it not eliminated for CVP (A20.53, A26.221).
        return ScenarioA1EntryCalculator.ExitScoring(outcomes is not null,
            outcomes is not null && ScenarioA1EntryCalculator.ExitMeetsCondition(outcomes.SelectMany(outcome => outcome.Any).Select(ScenarioCards.ConditionFacts), side, edge,
                hex => BoardLocation.Parse(hex + ":0") is var near && ((near.Board == from.Board && near.Hex == from.Hex)
                    || Neighbors(state, near).Any(next => next.Board == from.Board && next.Hex == from.Hex))),
            escorting, () => ScenarioVictory.EscortEdge(card!, side, edge));
    }

    /// <summary>
    /// Infantry leaving the map (A2.6; rulings R21.5, R25.5): a Good Order stack of the phasing side, in its MPh (the whole moving stack when one moves) or
    /// by advance in its APh, from a ground-level edge hex within the playable area, across that edge, as if entering the mirror-image hex beyond: its own
    /// hex's cost across the edge hexside, at the road rate across a road hexside; from Bypass, one MF of Open Ground when the far vertex of its last
    /// hexside lies on the edge. A Guard takes its prisoners, only off its side's Friendly Board Edge or the edge of its exit condition (A20.53, A26.23).
    /// The units are Exited and may not return.
    /// </summary>
    private GamePlan PlanExit(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string edge, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        var ids = Strings(arguments, "unitIds").ToArray();
        var advancing = state.Phase == "aph";
        if (ScenarioA1EntryCalculator.ExitPhaseBar(state.Phase) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        var movers = ids.Select(state.Unit).ToArray();
        if (ScenarioA1EntryCalculator.ExitStackBar(ids.Length, ids.Distinct(StringComparer.Ordinal).Count(),
            movers.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side != state.PhasingSide || LiveFire.IsVehicle(unit))) is { } stackBar)
        {
            return Refused(scope, label, expected, stackBar);
        }

        var locations = movers.Select(unit => state.Location(unit!.Id)?.Location).Distinct().ToArray();
        if (ScenarioA1EntryCalculator.ExitFromBar(locations.Length, locations.Any(at => at is null)) is { } fromBar)
        {
            return Refused(scope, label, expected, fromBar);
        }

        var from = locations[0]!;

        // A3.3: a unit that fired in the PFPh does not move in the MPh, though it may advance (A4.7). C10.3 (referee, pass 26): a crew still pushing its Gun
        // may push it on, off the map, though its last push made it TI.
        var pushingOn = ScenarioA1EntryCalculator.PushingOn(advancing, Text(arguments, "pushGun", out _), state.Movement is not null,
            state.Movement is { } pushing && ids.All(id => pushing.Members.Contains(id, StringComparer.Ordinal)));
        if (ScenarioA1EntryCalculator.ExitUnableBar([.. movers.Select(unit => (unit!.Id, Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned), Is(unit, Conditions.Berserk),
            Is(unit, Conditions.Melee), Is(unit, Conditions.Captured), Is(unit, Conditions.Hidden), Is(unit, "asl:ti"), unit.MovementEnded, Is(unit, Conditions.PrepFire)))],
            advancing, pushingOn) is { } unableBar)
        {
            return Refused(scope, label, expected, unableBar);
        }

        // C10.3 (ruling R26.4): a crew or HS manning a Gun leaves the map only pushing it, alone, in its MPh, with a Manhandling DR as for any push.
        EquipmentInstance? pushed = null;
        if (movers.FirstOrDefault(unit => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Manned } holding && holding.Holder == unit!.Id)) is { } gunner)
        {
            var manned = state.Equipment.First(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Manned } holding && holding.Holder == gunner.Id);
            if (ScenarioA1EntryCalculator.ExitGunBar(gunner.Id, manned.Id, Text(arguments, "pushGun", out var pushGunId) ? pushGunId : null, advancing,
                [.. movers.Where(unit => unit!.Id != gunner.Id).Select(unit => unit!.Id)], vocabulary.IsA(gunner.Kind, "asl:crew") || vocabulary.IsA(gunner.Kind, "asl:half-squad"),
                OrdnanceReference.Value.Guns.GetValueOrDefault(manned.Definition?.Definition ?? string.Empty) is { Manhandling: not null },
                state.Movement is { Bypass.Count: > 0 } && state.Movement.Members.Contains(gunner.Id, StringComparer.Ordinal),
                () => manned.Definition is { } gunType && FireReference.Value.Definitions.GetValueOrDefault(gunType.Definition)?.QuickSetUp == true) is { } gunBar)
            {
                return Refused(scope, label, expected, gunBar);
            }

            pushed = manned;
        }

        var current = advancing ? null : state.Movement;
        // Table player, pass 25: a fresh stack is told another one is moving.
        if (ScenarioA1EntryCalculator.ExitMovementBar(current is not null, current?.WindowOpen == true, current is not null && current.Members.ToHashSet(StringComparer.Ordinal).SetEquals(ids),
            current is not null && ids.Any(id => current.Members.Contains(id, StringComparer.Ordinal)), current?.Movers ?? []) is { } movementBar)
        {
            return Refused(scope, label, expected, movementBar);
        }

        var sides = EdgeSides(state, from).Where(item => item.Edge == edge).ToArray();
        // Table player, pass 21: from a hex near the edge, the refusal names the edge hexes next to it.
        if (ScenarioA1EntryCalculator.ExitEdgeBar(from.Level, sides.Length > 0, PlayableBar(state, from) is null, from.ToString(), edge,
            () => [.. Neighbors(state, from).Where(next => next.Level == 0 && EdgeSides(state, next).Any(item => item.Edge == edge) && PlayableBar(state, next) is null)
                .Select(next => next.ToString()).Order(StringComparer.Ordinal)]) is { } edgeBar)
        {
            return Refused(scope, label, expected, edgeBar);
        }

        int halfMf;
        string terrain;
        var road = false;
        if (current is { Bypass: { Count: > 0 } lane })
        {
            // A2.6 (ruling R25.5): from Bypass, through the far vertex of the last hexside, with one MF beyond the Bypass, as Open Ground.
            var bypass = ScenarioA1EntryCalculator.BypassExit(BypassEntered(state, from, current.From, lane) is { } entered ? (int)entered : null, (int)lane[0], (int)lane[^1],
                [.. sides.Select(item => (int)item.Side)], edge);
            if (bypass.Refusal is { } bypassBar)
            {
                return Refused(scope, label, expected, bypassBar);
            }

            (halfMf, terrain) = (bypass.HalfMf, bypass.Terrain);
        }
        else
        {
            // The cheapest edge hexside of the hex: a half hex may lie on the edge across two.
            var cost = ScenarioA1EntryCalculator.ExitCost([.. sides.Select(item => EntryGround(state, from, item.Side).Entry).OfType<InfantryEntry>()
                .Select(item => (item.HalfMf, item.Terrain, item.RoadRate, item.AllMf))], from.ToString());
            if (cost.Refusal is { } costBar)
            {
                return Refused(scope, label, expected, costBar);
            }

            (halfMf, terrain, road) = (cost.HalfMf, cost.Terrain, cost.Road);
        }

        // C10.3 (rulings R8.6, R26.4): a Gun is pushed only across Open Ground or grain, at double the MF.
        if (pushed is not null)
        {
            var push = ScenarioA1EntryCalculator.PushedExit(terrain, halfMf, from.ToString());
            if (push.Refusal is { } pushTerrainBar)
            {
                return Refused(scope, label, expected, pushTerrainBar);
            }

            halfMf = push.HalfMf;
        }

        var how = ScenarioA1EntryCalculator.ExitHow(halfMf, road, current is { Bypass.Count: > 0 });
        var escorted = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Custodian is { } holder && ids.Contains(holder, StringComparer.Ordinal))
            .Select(unit => unit.Id).Order(StringComparer.Ordinal).ToArray();
        var with = ScenarioA1EntryCalculator.ExitWith(escorted);
        var scoring = ExitScoring(state, movers[0]!.Side, from, edge, escorted.Length > 0);
        var package = Rules.ScenarioA1FirePackage.Identity.ToString();
        if (advancing)
        {
            // A4.72 (ruling R25.5): an advance off the map into Difficult Terrain makes the unit CX; a CX unit does not make it. Referee, pass 25: with a leader's
            // two MF and IPC, as any advance (A4.72 EX).
            var (aided, ipcTo) = AdvanceAid(state, [.. movers.Select(unit => unit!)]);
            var (advanceBar, tiring) = ScenarioA1EntryCalculator.AdvanceExit([.. movers.Select(unit => (unit!.Id, Is(unit, Conditions.Cx)))],
                index => DifficultAdvance(state, movers[index]!, halfMf, aided.Contains(movers[index]!.Id), movers[index]!.Id == ipcTo));
            if (advanceBar is not null)
            {
                return Refused(scope, label, expected, advanceBar);
            }

            List<GameEvent> events = [.. tiring.Select((id, index) => Event(scope, attemptId, index + 1, expected, "conditions-changed",
                new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.Cx] = ConditionState.True }), package, null))];
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "advanced", new AdvanceMoved(ids, from) { Exit = edge }, package, null));
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
                [ScenarioA1EntryCalculator.AdvanceExitText(ids, from.ToString(), edge, terrain, with, tiring, scoring)]);
        }

        var doubleTime = Flag(arguments, "doubleTime");
        if (ScenarioA1EntryCalculator.ExitMfBar([.. movers.Select(unit => (unit!.Id, unit.MfSpent, unit.HalfMfSpent))],
            index => MfAllotment(state, movers[index]!, ScenarioA1EntryCalculator.DoubleTimeExtraMf(doubleTime, movers[index]!.MfSpent, movers[index]!.HalfMfSpent, movers[index]!.DoubleTimeMf),
                doubleTime || Is(movers[index]!, Conditions.Cx)), halfMf, from.ToString()) is { } mfBar)
        {
            return Refused(scope, label, expected, mfBar);
        }

        var step = new MovementStepped(ids, from, halfMf, false, (state.Movement?.Step ?? 0) + 1) { Exit = edge, DoubleTime = doubleTime, Road = road && pushed is null };
        if (pushed is not null)
        {
            // C10.3 (ruling R26.4): the Manhandling DR takes the MF spent, -2 across a road hexside; a DR above the M# leaves the Gun and crew in place.
            return PushPlan(scope, attemptId, expected, label, actor, state, pushed, step, ScenarioA1EntryCalculator.PushOffDrm(halfMf, road),
                ScenarioA1EntryCalculator.PushExitText(ids, from.ToString(), edge, how, scoring));
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "movement-step", step, package, null)],
            [ScenarioA1EntryCalculator.MoveExitText(ids, from.ToString(), edge, how, with, scoring)]);
    }
}
