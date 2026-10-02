using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
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
        ReadLocation(state, hex with { Level = 0 }) is { } read
            ? [.. read.Hex.Locations.Where(item => item.Terrain?.Name is not "Rooftop" && item.Level >= 0).Select(item => item.Level).Distinct().Order()]
            : [0];

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
        return unit.Kind switch
        {
            "asl:squad" or "asl:crew" => 2,
            "asl:half-squad" => 1,
            "asl:leader" => 1 + Math.Max(0, -(unit.Definition is { } reference
                ? catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:leadership")?.Value?.Number ?? 0
                : 0)),
            "asl:vehicle" => VehicleVictoryPoints(state, unit),
            _ => 0,
        };
    }

    /// <summary>
    /// A26.212 (ruling R24.3): one VP, one for a MA not malfunctioned, one per multiple of five AF of the vehicle's single strongest AF, rounded up (a 0 AF
    /// one; an unarmored vehicle none), and the inherent crew's two (A26.211) unless it has left the vehicle, Abandoned or as a crew counter of its own. An
    /// unarmored vehicle with no armament has no inherent crew (A26.212 EX: an unarmed truck is worth one).
    /// </summary>
    private static int VehicleVictoryPoints(GameState state, UnitInstance vehicle)
    {
        var value = 1;
        var definition = VehicleDefinition(vehicle);
        var armor = vehicle.Definition is { } reference ? OrdnanceReference.Value.Armor.Vehicles.GetValueOrDefault(reference.Definition) : null;

        // The MA is a Gun (its type, such as "t") or a MG the catalog names as the MA (a SPW 251/1's AAMG); not malfunctioned, nor disabled (a vehicle MG's
        // condition, so only a MG MA loses its point to it).
        if ((armor?.MaType is not null || definition?.MainArmament is not null) && !Is(vehicle, Conditions.Malfunctioned)
            && !(armor?.MaType is null && Is(vehicle, Conditions.Disabled)))
        {
            value++;
        }

        if (armor is { Unarmored: false })
        {
            int?[] factors = [armor.FrontAf, armor.SideAf, ScenarioA1.ScenarioA1ArmorReference.ArmorFactor(armor, "turret", "front"),
                ScenarioA1.ScenarioA1ArmorReference.ArmorFactor(armor, "turret", "side")];
            var strongest = factors.Max() ?? 0;
            value += strongest == 0 ? 1 : (strongest + 4) / 5;
        }

        if (HasInherentCrew(state, vehicle))
        {
            value += 2;
        }

        return value;
    }

    /// <summary>
    /// Whether a vehicle has its inherent crew (A26.211, D5.1; rulings R24.3, R24.5): an armed vehicle (a MA or any MG) has one, an unarmed vehicle only an
    /// Inherent Driver; it is gone once the vehicle is Abandoned or its crew is a counter of its own.
    /// </summary>
    private static bool HasInherentCrew(GameState state, UnitInstance vehicle)
    {
        var definition = VehicleDefinition(vehicle);
        var armor = vehicle.Definition is { } reference ? OrdnanceReference.Value.Armor.Vehicles.GetValueOrDefault(reference.Definition) : null;
        var armed = armor?.MaType is not null || definition is { MainArmament: not null } or { AntiAircraftMg: not null } or { BowMg: not null } or { CoaxialMg: not null };
        return armed && !Is(vehicle, Conditions.Abandoned)
            && !state.Units.Any(unit => unit.Kind == "asl:crew" && unit.Id.EndsWith($"-{vehicle.Id}-crew", StringComparison.Ordinal));
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
        if (CardOf(state) is not { VictoryConditions.Outcomes: { Count: > 0 } outcomes })
        {
            return string.Empty;
        }

        var meets = outcomes.SelectMany(outcome => outcome.Any).Any(condition => condition.Type == "exit-vp" && condition.Side == side && condition.Edge == edge
            && condition.Near!.Select(hex => BoardLocation.Parse(hex + ":0")).Any(near => (near.Board == from.Board && near.Hex == from.Hex)
                || Neighbors(state, near).Any(next => next.Board == from.Board && next.Hex == from.Hex)));
        // Referee, pass 25: a Guard may leave by any edge with its prisoners, which stay captured; only off a Friendly Board Edge or its exit condition's edge is
        // it not eliminated for CVP (A20.53, A26.221).
        return meets ? "; it counts toward the side's Exit VP, none for broken Personnel (A26.23)"
            : escorting && ScenarioVictory.EscortEdge(CardOf(state)!, side, edge) ? "; it meets no exit condition, but a Guard escorting prisoners off its side's Friendly Board Edge is not eliminated for CVP; its prisoners stay captured (A20.53, A26.221)"
            : escorting ? "; it meets no exit condition and is not the side's Friendly Board Edge, so the Guard counts as eliminated for the enemy's CVP; its prisoners stay captured (A20.53, A26.221)"
            : "; it meets no exit condition of the side, so the units count as eliminated for the enemy's CVP (A26.221)";
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
        if (state.Phase != "mph" && !advancing)
        {
            return Refused(scope, label, expected, "play.exit-phase: units leave the map in their side's MPh or by advance in its APh, never in the RtPh (A2.6; rulings R21.5, R25.5)");
        }

        var movers = ids.Select(state.Unit).ToArray();
        if (ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length
            || movers.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side != state.PhasingSide || LiveFire.IsVehicle(unit)))
        {
            return Refused(scope, label, expected, "play.exit-stack: every unit leaving is an active Infantry unit of the phasing side; a vehicle leaves by its own move (A2.6)");
        }

        if (movers.Select(unit => state.Location(unit!.Id)?.Location).Distinct().ToArray() is not [{ } from])
        {
            return Refused(scope, label, expected, "play.exit-stack: the stack leaves from one Location on the map (A2.6)");
        }

        // A3.3: a unit that fired in the PFPh does not move in the MPh, though it may advance (A4.7). C10.3 (referee, pass 26): a crew still pushing its Gun
        // may push it on, off the map, though its last push made it TI.
        var pushingOn = !advancing && Text(arguments, "pushGun", out _) && state.Movement is { } pushing && ids.All(id => pushing.Members.Contains(id, StringComparer.Ordinal));
        if (movers.FirstOrDefault(unit => Is(unit!, Conditions.Broken) || Is(unit!, Conditions.Pinned) || Is(unit!, Conditions.Berserk) || Is(unit!, Conditions.Melee)
            || Is(unit!, Conditions.Captured) || Is(unit!, Conditions.Hidden) || (Is(unit!, "asl:ti") && !pushingOn) || unit!.MovementEnded || (!advancing && Is(unit!, Conditions.PrepFire))) is { } unable)
        {
            return Refused(scope, label, expected, advancing
                ? $"play.exit-unit: {unable.Id} is not free to advance this APh (A4.7)"
                : $"play.exit-unit: {unable.Id} is not free to move this MPh (A4.1, A3.3)");
        }

        // C10.3 (ruling R26.4): a crew or HS manning a Gun leaves the map only pushing it, alone, in its MPh, with a Manhandling DR as for any push.
        EquipmentInstance? pushed = null;
        if (movers.FirstOrDefault(unit => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Manned } holding && holding.Holder == unit!.Id)) is { } gunner)
        {
            var manned = state.Equipment.First(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Manned } holding && holding.Holder == gunner.Id);
            if (!Text(arguments, "pushGun", out var pushGunId) || pushGunId != manned.Id)
            {
                return Refused(scope, label, expected, $"play.exit-unit: {gunner.Id} mans {manned.Id}; it leaves the map only pushing it (check \"push {manned.Id}\" beside the move), since abandoning a Gun to leave is not built (C10.3; ruling R26.4)");
            }

            var pushBar = advancing ? "a Gun is pushed in the MPh, not by advance"
                : movers.Length != 1 ? $"{gunner.Id} pushes its Gun alone; leave {string.Join(", ", movers.Where(unit => unit!.Id != gunner.Id).Select(unit => unit!.Id))} out of the stack"
                : !vocabulary.IsA(gunner.Kind, "asl:crew") && !vocabulary.IsA(gunner.Kind, "asl:half-squad") ? $"{gunner.Id} is not a crew or HS"
                : OrdnanceReference.Value.Guns.GetValueOrDefault(manned.Definition?.Definition ?? string.Empty) is not { Manhandling: not null } ? $"{manned.Id} has no M# in the catalog"
                : state.Movement is { Bypass.Count: > 0 } && state.Movement.Members.Contains(gunner.Id, StringComparer.Ordinal) ? "a Gun is not pushed from Bypass"
                : null;
            if (pushBar is not null)
            {
                return Refused(scope, label, expected, $"play.move-push: {pushBar} (C10.3, C10.111; ruling R26.4)");
            }

            if (manned.Definition is not { } gunType || FireReference.Value.Definitions.GetValueOrDefault(gunType.Definition)?.QuickSetUp != true)
            {
                return Refused(scope, label, expected, $"play.move-push: {manned.Id} is not QSU, and limbering is not built, so it is not pushed (C10.2; ruling R26.1)");
            }

            pushed = manned;
        }


        var current = advancing ? null : state.Movement;
        // Table player, pass 25: a fresh stack is told another one is moving.
        if (current is not null && (current.WindowOpen || !current.Members.ToHashSet(StringComparer.Ordinal).SetEquals(ids)))
        {
            return Refused(scope, label, expected, current.WindowOpen
                ? "play.move-window: the DEFENDER may still fire at the stack's last MF expenditure (A8.1, A8.11)"
                : !ids.Any(id => current.Members.Contains(id, StringComparer.Ordinal))
                    ? $"play.move-order: {string.Join(", ", current.Movers)} moved last; end their move first (A4.2)"
                    : "play.exit-stack: the whole moving stack leaves together (A2.6, A4.2; ruling R21.5)");
        }

        var sides = EdgeSides(state, from).Where(item => item.Edge == edge).ToArray();
        if (from.Level != 0 || sides.Length == 0 || PlayableBar(state, from) is not null)
        {
            // Table player, pass 21: from a hex near the edge, the refusal names the edge hexes next to it.
            var near = from.Level == 0 && sides.Length == 0
                ? Neighbors(state, from).Where(next => next.Level == 0 && EdgeSides(state, next).Any(item => item.Edge == edge) && PlayableBar(state, next) is null)
                    .Select(next => next.ToString()).Order(StringComparer.Ordinal).ToArray()
                : [];
            return Refused(scope, label, expected, $"play.exit-edge: {from} is not a ground-level hex of the {edge} edge within the playable area (A2.6; rulings R20.6, R21.5)"
                + (near.Length > 0 ? $"; {string.Join(" and ", near)} " + (near.Length == 1 ? "is" : "are") + " next to it" : string.Empty));
        }

        int halfMf;
        string terrain;
        var road = false;
        if (current is { Bypass: { Count: > 0 } lane })
        {
            // A2.6 (ruling R25.5): from Bypass, through the far vertex of the last hexside, with one MF beyond the Bypass, as Open Ground.
            if (BypassEntered(state, from, current.From, lane) is not { } entered)
            {
                return Refused(scope, label, expected, "play.exit-bypass: the hexside the stack entered its Bypass by cannot be read");
            }

            var far = (HexsideDirection)(((int)lane[^1] + (((int)lane[0] - (int)entered + 6) % 6)) % 6);
            if (!sides.Any(item => item.Side == lane[^1] || item.Side == far))
            {
                return Refused(scope, label, expected, $"play.exit-bypass: from Bypass the stack leaves only through the far vertex of its last hexside, which is not on the {edge} edge (A2.6, A4.31; ruling R25.5)");
            }

            (halfMf, terrain) = (2, "open-ground");
        }
        else
        {
            // The cheapest edge hexside of the hex: a half hex may lie on the edge across two.
            var crossings = sides.Select(item => EntryGround(state, from, item.Side).Entry).OfType<InfantryEntry>().Where(item => !item.AllMf).ToArray();
            if (crossings.Length == 0)
            {
                return Refused(scope, label, expected, $"play.exit-terrain: the cost of leaving {from} is not decided (A2.6; ruling R21.5)");
            }

            var cheapest = crossings.MinBy(item => item.HalfMf)!;
            (halfMf, terrain, road) = (cheapest.HalfMf, cheapest.Terrain, cheapest.RoadRate);
        }

        // C10.3 (rulings R8.6, R26.4): a Gun is pushed only across Open Ground or grain, at double the MF.
        if (pushed is not null)
        {
            if (terrain is not ("open-ground" or "grain"))
            {
                return Refused(scope, label, expected, $"play.move-push-terrain: a Gun is pushed only into Open Ground or grain in the review, and leaving {from} crosses {terrain} (C10.3; rulings R8.6, R26.4)");
            }

            halfMf *= 2;
        }

        var how = $"for {halfMf / 2m} MF" + (road ? " at the road rate" : string.Empty) + (current is { Bypass.Count: > 0 } ? " from Bypass" : string.Empty);
        var escorted = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Custodian is { } holder && ids.Contains(holder, StringComparer.Ordinal))
            .Select(unit => unit.Id).Order(StringComparer.Ordinal).ToArray();
        var with = escorted.Length > 0 ? $", escorting {string.Join(", ", escorted)} (A20.53)," : string.Empty;
        var scoring = ExitScoring(state, movers[0]!.Side, from, edge, escorted.Length > 0);
        var package = ScenarioA1.ScenarioA1FirePackage.Identity.ToString();
        if (advancing)
        {
            // A4.72 (ruling R25.5): an advance off the map into Difficult Terrain makes the unit CX; a CX unit does not make it.
            var tiring = new List<string>();
            var (aided, ipcTo) = AdvanceAid(state, [.. movers.Select(unit => unit!)]);
            foreach (var unit in movers)
            {
                // Referee, pass 25: with a leader's two MF and IPC, as any advance (A4.72 EX).
                if (DifficultAdvance(state, unit!, halfMf, aided.Contains(unit!.Id), unit.Id == ipcTo) is not { } difficult)
                {
                    return Refused(scope, label, expected, $"play.advance-mf: {unit!.Id} has no MF allotment the catalog decides, or none left after portage (A4.7, A4.72)");
                }

                if (difficult && Is(unit!, Conditions.Cx))
                {
                    return Refused(scope, label, expected, $"play.advance-difficult-terrain: {unit!.Id} is CX and may not advance into Difficult Terrain (A4.72)");
                }

                if (difficult)
                {
                    tiring.Add(unit!.Id);
                }
            }

            List<GameEvent> events = [.. tiring.Select((id, index) => Event(scope, attemptId, index + 1, expected, "conditions-changed",
                new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.Cx] = ConditionState.True }), package, null))];
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "advanced", new AdvanceMoved(ids, from) { Exit = edge }, package, null));
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
                [$"play.exit: {string.Join(", ", ids)} advance off the map from {from} across the {edge} edge ({terrain}){with} and may not return"
                    + (tiring.Count > 0 ? $"; {string.Join(", ", tiring)} become CX advancing into Difficult Terrain (A4.72)" : string.Empty) + $" (A2.6; ruling R25.5){scoring}"]);
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

        var step = new MovementStepped(ids, from, halfMf, false, (state.Movement?.Step ?? 0) + 1) { Exit = edge, DoubleTime = doubleTime, Road = road && pushed is null };
        if (pushed is not null)
        {
            // C10.3 (ruling R26.4): the Manhandling DR takes the MF spent, -2 across a road hexside; a DR above the M# leaves the Gun and crew in place.
            return PushPlan(scope, attemptId, expected, label, actor, state, pushed, step, (halfMf / 2) - (road ? 2 : 0),
                $"play.exit: {string.Join(", ", ids)} leave{(ids.Length == 1 ? "s" : string.Empty)} the map from {from} across the {edge} edge {how} (A2.6; rulings R21.5, R26.4){scoring}");
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "movement-step", step, package, null)],
            [$"play.exit: {string.Join(", ", ids)} leave the map from {from} across the {edge} edge {how}{with} and may not return (A2.6; rulings R21.5, R25.5){scoring}"]);
    }
}
