using System.Globalization;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Snipers (backlog pass 15, ruling R15.5): after a fire record, each IFT, MC, or TC Original DR a side made in the PFPh, MPh, DFPh, or AFPh that equals the
/// other side's SAN calls for that side's Sniper attack, in the order the DRs were made, resolved in the same commit (A14.1). The Sniper dr decides it (1 or 2;
/// A14.3); a Random Location DR from the Sniper counter's hex finds the target hex, or the closest hex holding an eligible target, ties to the lowest TEM and then
/// the first hex by name (A14.2, A14.21); the Location there with the most targets is attacked, and Random Selection picks among them, a concealed stack
/// counting as one (A14.23). The Sniper player never forfeits an attack, so never repositions (backlog). The rules are <see cref="ScenarioA1Sniper"/>'s
/// (pass 32.h); this file reads the records, the state, and the map, draws the dice, and writes the events.
/// </summary>
public sealed partial class GamePlanner
{
    private void AddSniperAttacks(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, List<GameEvent> events,
        Func<RollRequest, RollResult> draw)
    {
        if (events.Any(item => item.Payload is ChoicePending) || Replay(existing).Current is not { } before || !ScenarioA1Sniper.TriggerPhase(before.Phase))
        {
            return;
        }

        // Table player, pass 27: an attack resumed after an owner's choice drew its dice in an earlier commit; its record is in this one.
        var dice = existing.Concat(events).Select(item => item.Payload).OfType<DiceRolled>().ToDictionary(item => item.Roll, StringComparer.Ordinal);
        var order = existing.Concat(events).Select(item => item.Payload).OfType<DiceRolled>().Select(item => item.Roll).ToList();
        var triggers = new List<(string Roll, string Sniper)>();
        foreach (var fire in events.Select(item => item.Payload).OfType<FireResolved>())
        {
            var firingSide = FiringSideOf(before, fire);
            foreach (var (key, rollId) in fire.Rolls)
            {
                if (!dice.TryGetValue(rollId, out var roll) || roll.Count != 2)
                {
                    continue;
                }

                var (kind, unitId) = ScenarioA1Sniper.RollKey(key);

                // A14.1: an IFT DR is its firing side's; a MC or TC DR its unit's, never a prisoner's (Rules decides, pass 32.h).
                var maker = ScenarioA1Sniper.RollMaker(kind, firingSide,
                    () => before.Unit(unitId) is { } checker ? (checker.Side, Is(checker, Conditions.Captured)) : null);
                // E1.76 (backlog pass 16, ruling R16.7): at night each side's SAN is two higher, to at most 7.
                if (maker is null || before.Sides.FirstOrDefault(item => item.Id != maker) is not { San: { } printed } enemy
                    || !ScenarioA1Sniper.Triggers(roll.Values.Sum(), printed, before.Night))
                {
                    continue;
                }

                if (before.Entities.FirstOrDefault(item => item.Kind == "asl:sniper" && item.Side == enemy.Id && item.Status == InstanceStatus.Active) is { } sniper)
                {
                    triggers.Add((rollId, sniper.Id));
                }
            }
        }

        foreach (var (trigger, sniperId) in triggers.OrderBy(item => order.IndexOf(item.Roll)))
        {
            Snipe(scope, attemptId, expected, actor, existing, events, draw, sniperId, trigger);
        }
    }

    /// <summary>The side that made a fire record's IFT DR: its firers', a DC's user's, or the DEFENDER's for Residual FP (A8.2); the record's reads are made as Rules asks.</summary>
    private static string? FiringSideOf(GameState state, FireResolved fire) =>
        ScenarioA1Sniper.FiringSide(fire.Firers.Count > 0, () => state.Unit(fire.Firers[0])?.Side,
            () => fire.Facts.TryGetProperty("demolitionCharge", out var charge) && charge.TryGetProperty("userId", out var user) && user.GetString() is { } userId
                ? state.Unit(userId)?.Side : null,
            () => fire.Facts.TryGetProperty("fireKind", out var kind) && kind.GetString() == ScenarioA1FireCalculator.ResidualFire,
            () => state.Sides.FirstOrDefault(item => item.Id != state.PhasingSide)?.Id);

    private void Snipe(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, List<GameEvent> events,
        Func<RollRequest, RollResult> draw, string sniperId, string trigger)
    {
        if (Replay([.. existing, .. events]).Current is not { } state || state.Find(sniperId) is not EntityInstance { Status: InstanceStatus.Active } sniper
            || state.Location(sniper.Id) is not { } sniperAt || sniper.Side is not { } sniperSide
            || state.Sides.FirstOrDefault(item => item.Id != sniperSide)?.Id is not { } attacked)
        {
            return;
        }

        // A14.31: a pinned Sniper makes no further attack this Player Turn.
        if (!ScenarioA1Sniper.Attacks(Is(sniper, Conditions.Pinned)))
        {
            return;
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        string Roll(int count, string purpose)
        {
            var drawn = draw(new RollRequest(count, 6));
            var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled", new DiceRolled(rollId, purpose, count, 6, drawn.Values, DiceRolled.SystemSource, actor),
                package, null));
            return rollId;
        }

        IReadOnlyList<int> Values(string rollId) => events.Select(item => item.Payload).OfType<DiceRolled>().First(item => item.Roll == rollId).Values;
        void Record(string rollId, int dr, BoardLocation? at, string? unit, string result) =>
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "sniper-attacked", new SniperAttacked(sniper.Id, trigger, rollId, dr, at, unit, result), package, null));

        var sniperRoll = Roll(1, "sniper");
        var dr = Values(sniperRoll)[0];
        if (!ScenarioA1Sniper.Effective(dr))
        {
            Record(sniperRoll, dr, null, null, ScenarioA1Sniper.NoEffect);
            return;
        }

        // A14.2 (ruling R15.5): the Random Location DR: the colored dr a hexside direction, the white dr the hexes along it, stopping at the last hex on the map.
        var locationRoll = Roll(2, "sniper-location");
        var (direction, extent) = ScenarioA1Sniper.RandomLocation(Values(locationRoll)[0], Values(locationRoll)[1]);
        var (board, hex) = (sniperAt.Location.Board, sniperAt.Location.Hex);
        for (var step = 0; step < extent && Next(state, board, hex, (HexsideDirection)direction) is { } next; step++)
        {
            (board, hex) = next;
        }

        // A14.22 (ruling R15.5): the eligible targets are the attacked side's Personnel and Dummies, not hidden, not prisoners.
        UnitInstance[] eligible = [.. state.Units.Where(unit => ScenarioA1Sniper.Eligible(unit.Status == InstanceStatus.Active, unit.Side == attacked, LiveFire.IsVehicle(unit),
            Is(unit, Conditions.Hidden), Is(unit, Conditions.Captured), state.Location(unit.Id) is not null))];
        if (eligible.Length == 0)
        {
            Record(sniperRoll, dr, null, null, ScenarioA1Sniper.NoTarget);
            return;
        }

        // A14.21: the target hex, or the closest hex holding an eligible target; ties go to the lowest TEM, then the first hex by name (Rules orders them).
        int Tem(BoardLocation location) => ScenarioA1Sniper.TargetTem(ReadLocation(state, location) is { } read ? TerrainKey(read) : null);
        var hexes = eligible.GroupBy(unit => (state.Location(unit.Id)!.Location.Board, state.Location(unit.Id)!.Location.Hex))
            .Select((group, index) => (group.Key.Board, group.Key.Hex, Facts: new SniperTargetHex(index, Distance(state, board, hex, group.Key.Board, group.Key.Hex),
                group.Min(unit => Tem(state.Location(unit.Id)!.Location)), group.Key.Hex.ToString()))).ToArray();
        var chosen = hexes[ScenarioA1Sniper.TargetHex([.. hexes.Select(item => item.Facts)])];

        // Ruling R15.5: of several Locations in the hex, the one holding the most eligible targets, then the lowest.
        var inHex = eligible.Select(unit => state.Location(unit.Id)!.Location).Where(item => item.Board == chosen.Board && item.Hex == chosen.Hex)
            .GroupBy(item => item).Select((group, index) => (group.Key, Facts: new SniperTargetLocation(index, group.Count(), group.Key.Level))).ToArray();
        var location = inHex[ScenarioA1Sniper.TargetLocation([.. inHex.Select(item => item.Facts)])].Key;
        UnitInstance[] here = [.. eligible.Where(unit => state.Location(unit.Id)!.Location == location).OrderBy(unit => unit.Id, StringComparer.Ordinal)];

        // Ruling R15.5: the Sniper counter moves to the Location attacked.
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-moved", new InstanceMoved(sniper.Id, new MapPosition(location)), package, null));

        // A14.23: a concealed stack is one possible target; the others one each.
        bool[] concealedOrDummy = [.. here.Select(unit => Is(unit, Conditions.Concealed) || unit.Kind == UnitKinds.Dummy)];
        var candidates = ScenarioA1Sniper.Candidates(concealedOrDummy).Select(group => (IReadOnlyList<UnitInstance>)[.. group.Select(index => here[index])]).ToList();
        var targets = new List<(IReadOnlyList<UnitInstance> Stack, string Roll, int Dr)>();
        if (candidates.Count == 1)
        {
            targets.Add((candidates[0], sniperRoll, dr));
        }
        else
        {
            var selection = Roll(candidates.Count, "sniper-random-selection");
            var tied = ScenarioA1Sniper.Tied(Values(selection)).Select(index => candidates[index]).ToArray();

            // A14.2: the first selected takes the Sniper dr; each other one takes a new Sniper dr of its own.
            targets.Add((tied[0], sniperRoll, dr));
            foreach (var other in tied.Skip(1))
            {
                var again = Roll(1, "sniper");
                targets.Add((other, again, Values(again)[0]));
            }
        }

        foreach (var (stack, rollId, value) in targets)
        {
            if (!ScenarioA1Sniper.Effective(value))
            {
                Record(rollId, value, location, null, ScenarioA1Sniper.NoEffect);
                continue;
            }

            var unit = stack[0];
            if (stack.Count > 1 || concealedOrDummy[Array.IndexOf(here, stack[0])])
            {
                // A14.23: a concealed stack reveals how many real units it holds: none is a Dummy stack, eliminated; one is the target; more are chosen by
                // Random Selection.
                UnitInstance[] real = [.. stack.Where(item => item.Kind != UnitKinds.Dummy)];
                if (real.Length == 0)
                {
                    Record(rollId, value, location, null, ScenarioA1Sniper.DummyStackEliminated);
                    foreach (var dummy in stack)
                    {
                        events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(dummy.Id), package, null));
                    }

                    continue;
                }

                if (real.Length == 1)
                {
                    unit = real[0];
                }
                else
                {
                    var among = Roll(real.Length, "sniper-random-selection");
                    unit = real[ScenarioA1Sniper.Picked(Values(among))];
                }
            }

            SnipeUnit(scope, attemptId, expected, events, unit, rollId, value, location, package, Roll, Values, Record);
        }

        // A14.3: an effective Sniper attack puts every broken unit of the attacked side in the Location under DM.
        if (Replay([.. existing, .. events]).Current is { } after)
        {
            foreach (var broken in after.At(location).OfType<UnitInstance>().Where(item => ScenarioA1Sniper.ComesUnderDm(item.Status == InstanceStatus.Active, item.Side == attacked,
                Is(item, Conditions.Broken), Is(item, Conditions.DesperationMorale))))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                    new ConditionsChanged(broken.Id, new Dictionary<string, ConditionState> { [Conditions.DesperationMorale] = ConditionState.True }), package, null));
            }
        }
    }

    /// <summary>
    /// A14.3 (ruling R15.5): dr 1 eliminates a SMC and breaks a MMC, Casualty Reducing one that cannot break (broken or berserk); dr 2 wounds a SMC (its Wound
    /// Severity dr, A17.11) and pins a MMC not immune to Pin results. A unit the attack affects loses its "?" (A12.14). Rules decides the effect (pass 32.h).
    /// </summary>
    private void SnipeUnit(GameScope scope, string attemptId, long expected, List<GameEvent> events, UnitInstance unit, string rollId, int dr,
        BoardLocation location, string package, Func<int, string, string> roll, Func<string, IReadOnlyList<int>> values,
        Action<string, int, BoardLocation?, string?, string> record)
    {
        void Change(IReadOnlyList<(UnitCondition Condition, bool Value)> conditions) =>
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.Id, ConditionChanges(conditions)), package, null));
        void Eliminate() => events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(unit.Id), package, null));

        var smc = unit.Kind is "asl:leader" or "asl:hero";
        switch (ScenarioA1Sniper.UnitEffect(smc, dr, Is(unit, Conditions.Broken), Is(unit, Conditions.Berserk), Is(unit, Conditions.Pinned)))
        {
            case SniperEffect.Eliminated:
                record(rollId, dr, location, unit.Id, ScenarioA1Sniper.Eliminated);
                Eliminate();
                break;
            case SniperEffect.WoundSeverity:
                var severity = roll(1, "sniper-wound-severity");
                var mortal = ScenarioA1Sniper.Mortal(values(severity)[0], Is(unit, Conditions.Wounded));
                record(rollId, dr, location, unit.Id, mortal ? ScenarioA1Sniper.Eliminated : ScenarioA1Sniper.Wounded);
                if (mortal)
                {
                    Eliminate();
                }
                else
                {
                    Change(ScenarioA1Sniper.WoundedConditions());
                }

                break;
            case SniperEffect.Broken:
                record(rollId, dr, location, unit.Id, ScenarioA1Sniper.Broken);
                Change(ScenarioA1Sniper.BrokenConditions());
                break;
            case SniperEffect.CasualtyReduction:
                // A7.302: a squad is Reduced to its HS, a HS eliminated.
                var half = unit.Kind == "asl:squad" && unit.Definition is { } reference && ScenarioA1FireReference.HalfSquadOf(reference.Definition) is { } halfId
                    && FireReference.Value.Definitions.TryGetValue(halfId, out var halfDefinition) ? halfDefinition : null;
                record(rollId, dr, location, unit.Id, ScenarioA1Sniper.ReductionResult(half is not null));
                if (half is not null)
                {
                    var produced = unit.Conditions.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
                    produced[Conditions.Concealed] = ConditionState.False;
                    produced[Conditions.Hidden] = ConditionState.False;
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "lineage", new LineageRecorded(LineageAction.Reduced, [unit.Id],
                        [new NewInstance($"{attemptId}-{unit.Id}-sniped", half.Kind, half.Id, unit.Side, unit.Position, null, produced)]), package, null));
                }
                else
                {
                    Eliminate();
                }

                break;
            case SniperEffect.Pinned:
                record(rollId, dr, location, unit.Id, ScenarioA1Sniper.Pinned);
                Change(ScenarioA1Sniper.PinnedConditions());
                break;
            default:
                record(rollId, dr, location, unit.Id, ScenarioA1Sniper.NoEffect);
                break;
        }
    }

    /// <summary>The hex across a hexside, on one board or across a seam of a placed map; null at the edge of the map.</summary>
    private (BoardRef Board, HexName Hex)? Next(GameState state, BoardRef board, HexName hex, HexsideDirection side) =>
        Composed(state) is { } composed ? composed.Neighbor(board, hex, side)
            : state.Map.Board(board) is { } placed && boards.TryGetBoard(board, placed.Version).Board is { } handle && handle.Neighbor(hex, side) is { } next
                ? (board, next) : null;

    /// <summary>The distance in hexes between two hexes of the map; null when it cannot be read.</summary>
    private int? Distance(GameState state, BoardRef fromBoard, HexName from, BoardRef toBoard, HexName to) =>
        Composed(state) is { } composed ? composed.Distance(fromBoard, from, toBoard, to)
            : fromBoard == toBoard && state.Map.Board(fromBoard) is { } placed && boards.TryGetBoard(fromBoard, placed.Version).Board is { } handle ? handle.Distance(from, to) : null;
}
