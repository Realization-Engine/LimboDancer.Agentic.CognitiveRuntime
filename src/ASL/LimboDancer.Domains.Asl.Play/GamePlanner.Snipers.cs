using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Snipers (backlog pass 15, ruling R15.5): after a fire record, each IFT, MC, or TC Original DR a side made in the PFPh, MPh, DFPh, or AFPh that equals the
/// other side's SAN calls for that side's Sniper attack, in the order the DRs were made, resolved in the same commit (A14.1). The Sniper dr decides it (1 or 2;
/// A14.3); a Random Location DR from the Sniper counter's hex finds the target hex, or the closest hex holding an eligible target or the enemy Sniper counter,
/// ties to the lowest TEM and SMOKE (A14.2, A14.21), and Random Selection picks among the targets of the Location attacked, a concealed stack counting as one
/// (A14.23). Since pass 71 (rulings R71.3 to R71.8) the targets are the units on foot, the Vulnerable PRC,
/// Inherent crews, unarmored vehicles, and the enemy Sniper counter (A14.22, A14.31), each with its own results (A14.3, A14.33, D5.34, D5.341). Since pass 71's
/// increment 4b (rulings R71.9, R71.10) a SAN falls when its Sniper is eliminated, and the attacked side's units in the target Location may take a Sniper
/// Check, a pending choice the attacks still to resolve wait for (A14.4). Since increment 4d (ruling R71.12, which replaces R15.5's picks) the Sniper player
/// decides where the rule lets him, by a pending choice of his own: to attack or to forfeit and reposition, the equidistant hex, the Location, the target
/// before Random Selection, and which target Random Selection gave takes the dr; an older record's picks are its recorded events. The rules are
/// <see cref="ScenarioA1Sniper"/>'s (pass 32.h); this file reads the records, the state, and the map, draws the dice, and writes the events.
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
                // E1.76 (backlog pass 16, ruling R16.7): at night each side's SAN is two higher, to at most 7; A14.4 (pass 71, ruling R71.9): less its
                // reductions, never below the printed SAN at night.
                if (maker is null || before.Sides.FirstOrDefault(item => item.Id != maker) is not { San: { } printed } enemy
                    || !ScenarioA1Sniper.Triggers(roll.Values.Sum(), printed, before.Night, enemy.SanReductions))
                {
                    continue;
                }

                if (before.Entities.FirstOrDefault(item => item.Kind == "asl:sniper" && item.Side == enemy.Id && item.Status == InstanceStatus.Active) is { } sniper)
                {
                    triggers.Add((rollId, sniper.Id));
                }
            }
        }

        SnipeInOrder(scope, attemptId, expected, actor, existing, events, draw, [.. triggers.OrderBy(item => order.IndexOf(item.Roll))]);
    }

    /// <summary>
    /// The Sniper attacks called for, in the order their DRs were made (A14.1), until one stops at a choice, the Sniper player's (A14.2; pass 71, ruling
    /// R71.12) or a Sniper Check (A14.4; ruling R71.10): the attacks after it are kept in the choice and resolve after the answer, in the same order.
    /// </summary>
    private void SnipeInOrder(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, List<GameEvent> events,
        Func<RollRequest, RollResult> draw, (string Roll, string Sniper)[] triggers)
    {
        for (var position = 0; position < triggers.Length; position++)
        {
            if (Snipe(scope, attemptId, expected, actor, existing, events, draw, triggers[position].Sniper, triggers[position].Roll, [.. triggers.Skip(position + 1)]))
            {
                return;
            }
        }
    }

    /// <summary>The side that made a fire record's IFT DR: its firers', a DC's user's, or the DEFENDER's for Residual FP (A8.2); the record's reads are made as Rules asks.</summary>
    private static string? FiringSideOf(GameState state, FireResolved fire) =>
        ScenarioA1Sniper.FiringSide(fire.Firers.Count > 0, () => state.Unit(fire.Firers[0])?.Side,
            () => fire.Facts.TryGetProperty("demolitionCharge", out var charge) && charge.TryGetProperty("userId", out var user) && user.GetString() is { } userId
                ? state.Unit(userId)?.Side : null,
            () => fire.Facts.TryGetProperty("fireKind", out var kind) && kind.GetString() == ScenarioA1FireCalculator.ResidualFire,
            () => state.Sides.FirstOrDefault(item => item.Id != state.PhasingSide)?.Id);

    /// <summary>
    /// A Sniper target (A14.2 EXC, A14.22, A14.31; pass 71, ruling R71.3): a unit (on foot or a Vulnerable PRC), a vehicle's Inherent crew or the unarmored
    /// vehicle itself (the vehicle as the item, by <see cref="Role"/>), or the enemy Sniper counter, with the Location it is attacked in.
    /// </summary>
    private readonly record struct SniperTarget(IGameObject Item, string Role, BoardLocation At)
    {
        /// <summary>A14.23: a concealed unit or a Dummy, which Random Selection counts as one stack; a crew or vehicle of a concealed vehicle too.</summary>
        public bool ConcealedOrDummy => Item is UnitInstance unit && (Is(unit, Conditions.Concealed) || unit.Kind == UnitKinds.Dummy);
    }

    /// <summary>
    /// What a Sniper attack resumed after its Sniper player's answer reads again (A14.2; pass 71, ruling R71.12): the counter's Location when the attack began
    /// (it may have moved to the Location attacked since), the rolls already recorded, whether the counter has moved, and the answers so far by step.
    /// </summary>
    private sealed record SniperResume(BoardLocation From, string SniperRoll, string? LocationRoll, string? SelectionRoll, bool Moved, IReadOnlyDictionary<string, string> Answers);

    /// <summary>
    /// One Sniper attack (A14.2, A14.3); true when it stops at a choice whose resume keeps <paramref name="rest"/>, the attacks still to resolve: the Sniper
    /// player's (A14.2, A14.21; pass 71, ruling R71.12), or the attacked side's Sniper Check (A14.4; ruling R71.10). <paramref name="resumed"/> continues
    /// an attack from the Sniper player's answers and the rolls it recorded.
    /// </summary>
    private bool Snipe(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, List<GameEvent> events,
        Func<RollRequest, RollResult> draw, string sniperId, string trigger, IReadOnlyList<(string Roll, string Sniper)> rest, SniperResume? resumed = null)
    {
        // Pass 71 (design section 5, item 4): the state after the commit's events so far is taken once, and every Location is read from its index.
        if (Replay([.. existing, .. events]).Current is not { } state || state.Find(sniperId) is not EntityInstance { Status: InstanceStatus.Active } sniper
            || sniper.Side is not { } sniperSide || state.Sides.FirstOrDefault(item => item.Id != sniperSide)?.Id is not { } attacked)
        {
            return false;
        }

        var index = MemoOf(state).Objects(state);
        if ((resumed?.From ?? index.Location(sniper.Id)?.Location) is not { } sniperAt)
        {
            return false;
        }

        // A14.31: a pinned Sniper makes no further attack, nor repositioning, this Player Turn.
        if (!ScenarioA1Sniper.Attacks(Is(sniper, Conditions.Pinned)))
        {
            return false;
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

        // A roll of this commit, or one a resumed attack recorded before its Sniper player's answer.
        IReadOnlyList<int> Values(string rollId) => (events.Select(item => item.Payload).OfType<DiceRolled>().FirstOrDefault(item => item.Roll == rollId)
            ?? existing.Select(item => item.Payload).OfType<DiceRolled>().Last(item => item.Roll == rollId)).Values;
        void Record(string rollId, int dr, BoardLocation? at, string? unit, string result) =>
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "sniper-attacked", new SniperAttacked(sniper.Id, trigger, rollId, dr, at, unit, result), package, null));

        var answers = resumed?.Answers ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var (locationRoll, selectionRoll, moved) = (resumed?.LocationRoll, resumed?.SelectionRoll, resumed?.Moved ?? false);
        var sniperRoll = resumed?.SniperRoll ?? Roll(1, "sniper");
        var dr = Values(sniperRoll)[0];
        if (!ScenarioA1Sniper.Effective(dr))
        {
            Record(sniperRoll, dr, null, null, ScenarioA1Sniper.NoEffect);
            return false;
        }

        // Ruling R71.12: the attack stops at the Sniper player's choice, public as the dr is; its resume keeps what the attack has recorded, and the attacks
        // still to resolve.
        bool Pause(string step, IReadOnlyList<string> options, BoardLocation? place)
        {
            var answered = new JsonObject();
            foreach (var (name, value) in answers)
            {
                answered[name] = value;
            }

            var rolls = new JsonObject { ["sniper"] = sniperRoll };
            if (locationRoll is not null)
            {
                rolls["location"] = locationRoll;
            }

            if (selectionRoll is not null)
            {
                rolls["selection"] = selectionRoll;
            }

            var resume = new JsonObject
            {
                ["record"] = ChoicePending.Sniper,
                ["step"] = step,
                ["sniper"] = sniper.Id,
                ["sniperSide"] = sniperSide,
                ["trigger"] = trigger,
                ["dr"] = dr,
                ["from"] = sniperAt.ToString(),
                ["moved"] = moved,
                ["rolls"] = rolls,
                ["answers"] = answered,
                ["rest"] = new JsonArray([.. rest.Select(item => (JsonNode)new JsonObject { ["roll"] = item.Roll, ["sniper"] = item.Sniper })]),
            };
            if (place is not null)
            {
                resume["place"] = place.ToString();
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "choice-pending", new ChoicePending($"{ChoicePending.Sniper}:{sniper.Id}:{step}", ChoicePending.Sniper,
                sniperSide, options, JsonSerializer.SerializeToElement(resume)), null, null));
            return true;
        }

        // A14.2 (p. 82; ruling R71.12): "it may be repositioned by the Sniper player (within the same placement requirements) if he forfeits an effective
        // Sniper attack dr ... prior to selecting the hex for that forfeited Sniper attack": asked only when the placement check of the moment (4c's) allows a
        // hex other than its own.
        var forfeit = answers.GetValueOrDefault(ScenarioA1Sniper.ForfeitStep);
        if (forfeit != ScenarioA1Sniper.Attack)
        {
            BoardLocation[] elsewhere = [.. SniperPlacements(state, sniperSide).Where(at => at.Board != sniperAt.Board || at.Hex != sniperAt.Hex)];
            if (forfeit is null && ScenarioA1Sniper.ChoiceOffered(elsewhere.Length + 1))
            {
                return Pause(ScenarioA1Sniper.ForfeitStep, [ScenarioA1Sniper.Attack, ScenarioA1Sniper.Reposition], null);
            }

            if (forfeit == ScenarioA1Sniper.Reposition)
            {
                var to = answers.TryGetValue(ScenarioA1Sniper.RepositionStep, out var picked) ? BoardLocation.Parse(picked) : elsewhere.Length == 1 ? elsewhere[0] : null;
                if (to is null)
                {
                    return Pause(ScenarioA1Sniper.RepositionStep, [.. elsewhere.Select(at => at.ToString())], null);
                }

                Record(sniperRoll, dr, to, null, ScenarioA1Sniper.Forfeited);
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-moved", new InstanceMoved(sniper.Id, new MapPosition(to)), package, null));
                return false;
            }
        }

        // A14.2 (ruling R15.5): the Random Location DR: the colored dr a hexside direction, the white dr the hexes along it, stopping at the last hex on the map.
        locationRoll ??= Roll(2, "sniper-location");
        var (direction, extent) = ScenarioA1Sniper.RandomLocation(Values(locationRoll)[0], Values(locationRoll)[1]);
        var (board, hex) = (sniperAt.Board, sniperAt.Hex);
        for (var step = 0; step < extent && Next(state, board, hex, (HexsideDirection)direction) is { } next; step++)
        {
            (board, hex) = next;
        }

        // A14.22, A14.31 (rulings R71.3, R71.4, R71.6): the attacked side's eligible targets and its Sniper counter.
        var eligible = SniperTargets(state, index, attacked);
        if (eligible.Count == 0)
        {
            Record(sniperRoll, dr, null, null, ScenarioA1Sniper.NoTarget);
            return false;
        }

        // A14.21 (ruling R71.7): the target hex, or the closest hex holding an eligible target or the enemy Sniper counter; ties go to the lowest TEM with the
        // hex's SMOKE (Rules orders them), and the equidistant hexes still tied are the Sniper player's choice (ruling R71.12).
        var smoke = SmokeSources(state).GroupBy(place => (place.Board, place.Hex)).ToDictionary(group => group.Key, group => group.Count());
        int Tem(BoardLocation location) => ScenarioA1Sniper.TargetTem(ReadLocation(state, location) is { } read ? TerrainKey(read) : null,
            smoke.GetValueOrDefault((location.Board, location.Hex)));
        var hexes = eligible.GroupBy(target => (target.At.Board, target.At.Hex))
            .Select((group, position) => (At: new BoardLocation(group.Key.Board, group.Key.Hex, 0), Facts: new SniperTargetHex(position, Distance(state, board, hex, group.Key.Board, group.Key.Hex),
                group.Min(target => Tem(target.At)), group.Key.Hex.ToString()))).ToArray();
        BoardLocation[] equidistant = [.. ScenarioA1Sniper.TargetHexes([.. hexes.Select(item => item.Facts)]).Select(position => hexes[position].At)];
        var chosen = equidistant[0];
        if (ScenarioA1Sniper.ChoiceOffered(equidistant.Length))
        {
            if (!answers.TryGetValue(ScenarioA1Sniper.HexStep, out var picked))
            {
                return Pause(ScenarioA1Sniper.HexStep, [.. equidistant.Select(at => at.ToString())], null);
            }

            chosen = equidistant.First(at => at.ToString() == picked);
        }

        // A14.2 (ruling R71.12): "If that new target hex Contains > one Location occupied by an eligible (14.22) enemy unit(s), the Sniper player selects which
        // of those Locations to attack"; the enemy Sniper counter is part of every Location of its hex (A14.31), so it is attacked in its own Location when it
        // is alone there, and joins the Location chosen otherwise.
        SniperTarget[] inChosen = [.. eligible.Where(target => target.At.Board == chosen.Board && target.At.Hex == chosen.Hex)];
        SniperTarget[] snipers = [.. inChosen.Where(target => target.Role == ScenarioA1Sniper.SniperTarget)];
        SniperTarget[] placed = [.. inChosen.Where(target => target.Role != ScenarioA1Sniper.SniperTarget)];
        BoardLocation[] locations = [.. (placed.Length > 0 ? placed : snipers).Select(target => target.At).Distinct().OrderBy(at => at.Level)];
        var location = locations[0];
        if (ScenarioA1Sniper.ChoiceOffered(locations.Length))
        {
            if (!answers.TryGetValue(ScenarioA1Sniper.LocationStep, out var picked))
            {
                return Pause(ScenarioA1Sniper.LocationStep, [.. locations.Select(at => at.ToString())], chosen);
            }

            location = locations.First(at => at.ToString() == picked);
        }

        SniperTarget[] here = [.. placed.Where(target => target.At == location).Concat(snipers).OrderBy(target => target.Item.Id, StringComparer.Ordinal)
            .ThenBy(target => target.Role, StringComparer.Ordinal)];

        // Ruling R15.5: the Sniper counter moves to the Location attacked, once (an attack resumed after a later choice has moved it).
        if (!moved)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-moved", new InstanceMoved(sniper.Id, new MapPosition(location)), package, null));
            moved = true;
        }

        // A14.23: a concealed stack is one possible target; the others one each. A14.2 EXC (ruling R71.12): before Random Selection the Sniper player may
        // choose the enemy Sniper counter, a Vulnerable Inherent Crew, or an unarmored vehicle; of several targets Random Selection gives, he chooses which
        // takes the Sniper dr, and each other takes a new one.
        bool[] concealedOrDummy = [.. here.Select(target => target.ConcealedOrDummy)];
        var candidates = ScenarioA1Sniper.Candidates(concealedOrDummy).Select(group => (IReadOnlyList<SniperTarget>)[.. group.Select(position => here[position])]).ToList();
        static bool Concealed(IReadOnlyList<SniperTarget> stack) => stack.Count > 1 || stack[0].ConcealedOrDummy;
        static string OptionOf(IReadOnlyList<SniperTarget> stack) => Concealed(stack) ? ScenarioA1Sniper.ConcealedStack : ScenarioA1Sniper.TargetOption(stack[0].Role, stack[0].Item.Id);
        var targets = new List<(IReadOnlyList<SniperTarget> Stack, string Roll, int Dr)>();
        int[] prior = [.. ScenarioA1Sniper.PriorTargets([.. candidates.Select(stack => new SniperCandidate(stack[0].Role, Concealed(stack)))])];
        string? first = null;
        if (prior.Length > 0)
        {
            if (!answers.TryGetValue(ScenarioA1Sniper.TargetStep, out var picked))
            {
                return Pause(ScenarioA1Sniper.TargetStep, [.. prior.Select(position => OptionOf(candidates[position])), ScenarioA1Sniper.RandomSelection], location);
            }

            first = picked == ScenarioA1Sniper.RandomSelection ? null : picked;
        }

        if (candidates.Count == 1 || first is not null)
        {
            targets.Add((first is null ? candidates[0] : candidates.First(stack => OptionOf(stack) == first), sniperRoll, dr));
        }
        else
        {
            selectionRoll ??= Roll(candidates.Count, "sniper-random-selection");
            var tied = ScenarioA1Sniper.Tied(Values(selectionRoll)).Select(position => candidates[position]).ToList();
            if (ScenarioA1Sniper.ChoiceOffered(tied.Count))
            {
                if (!answers.TryGetValue(ScenarioA1Sniper.FirstStep, out var picked))
                {
                    return Pause(ScenarioA1Sniper.FirstStep, [.. tied.Select(OptionOf)], location);
                }

                var head = tied.First(stack => OptionOf(stack) == picked);
                tied.Remove(head);
                tied.Insert(0, head);
            }

            // A14.2: the first selected takes the Sniper dr; each other one takes a new Sniper dr of its own (A14.33: so a vehicle and its PRC never share one).
            targets.Add((tied[0], sniperRoll, dr));
            foreach (var other in tied.Skip(1))
            {
                var again = Roll(1, "sniper");
                targets.Add((other, again, Values(again)[0]));
            }
        }

        // A12.14 (ruling R71.8): whether a Good Order enemy ground unit has a LOS to a target's Location, read only when a concealed one keeps its "?" by it.
        bool Seen(SniperTarget target) => target.Item.Side is { } side && NearestGoodOrderEnemyInLos(state, side, target.At) is not null;
        var lostLeaders = new List<(string Id, bool Eliminated)>();
        var replaced = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (stack, rollId, value) in targets)
        {
            if (!ScenarioA1Sniper.Effective(value))
            {
                Record(rollId, value, location, null, ScenarioA1Sniper.NoEffect);
                continue;
            }

            var target = stack[0];
            if (stack.Count > 1 || concealedOrDummy[Array.IndexOf(here, stack[0])])
            {
                // A14.23: a concealed stack reveals how many real units it holds: none is a Dummy stack, eliminated; one is the target; more are chosen by
                // Random Selection.
                SniperTarget[] real = [.. stack.Where(item => !(item.Item is UnitInstance unit && unit.Kind == UnitKinds.Dummy))];
                if (real.Length == 0)
                {
                    Record(rollId, value, location, null, ScenarioA1Sniper.DummyStackEliminated);
                    foreach (var dummy in stack)
                    {
                        events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(dummy.Item.Id), package, null));
                    }

                    continue;
                }

                if (real.Length == 1)
                {
                    target = real[0];
                }
                else
                {
                    var among = Roll(real.Length, "sniper-random-selection");
                    target = real[ScenarioA1Sniper.Picked(Values(among))];
                }
            }

            switch (target.Role)
            {
                case ScenarioA1Sniper.CrewTarget or ScenarioA1Sniper.VehicleTarget:
                    SnipeVehicle(scope, attemptId, expected, events, state, target, rollId, value, location, package, Record, () => Seen(target));
                    break;
                case ScenarioA1Sniper.SniperTarget:
                    SnipeSniper(scope, attemptId, expected, events, state, (EntityInstance)target.Item, rollId, value, location, package, Record);
                    break;
                default:
                    var unit = (UnitInstance)target.Item;
                    var result = SnipeUnit(scope, attemptId, expected, events, unit, rollId, value, location, package, Roll, Values, Record, () => Seen(target));
                    if (result == ScenarioA1Sniper.Eliminated)
                    {
                        replaced[unit.Id] = null;
                        if (unit.Kind == "asl:leader")
                        {
                            lostLeaders.Add((unit.Id, true));
                        }
                    }
                    else if (result == ScenarioA1Sniper.CasualtyReduced)
                    {
                        replaced[unit.Id] = ReducedBySniper(attemptId, unit.Id);
                    }

                    break;
            }
        }

        // A10.2 (pass 70, ruling R70.3): a leader the Sniper eliminates, by a dr of 1 or a mortal wound, causes a LLMC of the units in his Location. Only then is the
        // state replayed again (pass 71, design section 5, item 4): once for the check, and once for the DM below, which reads the units the check broke.
        GameState? after = null;
        if (lostLeaders.Count > 0 && Replay([.. existing, .. events]).Current is { } sniped)
        {
            AddLeaderLossChecks(scope, attemptId, expected, actor, events, draw, state, sniped, lostLeaders, ScenarioA1MoraleCheck.Sniper, null);
            after = Replay([.. existing, .. events]).Current;
        }

        // A14.3: an effective Sniper attack puts every broken unit of the attacked side in the Location under DM; with no LLMC, read from the state's index and
        // this attack's own results (an eliminated unit is gone, a Reduced one is its HS; one the attack broke is under DM already).
        IEnumerable<(string Id, UnitInstance Unit)> present = after is not null ? after.At(location).OfType<UnitInstance>().Select(unit => (unit.Id, unit))
            : index.At(location).OfType<UnitInstance>().Where(unit => !replaced.TryGetValue(unit.Id, out var now) || now is not null)
                .Select(unit => (replaced.GetValueOrDefault(unit.Id) ?? unit.Id, unit));
        foreach (var (brokenId, _) in present.Where(item => ScenarioA1Sniper.ComesUnderDm(item.Unit.Status == InstanceStatus.Active, item.Unit.Side == attacked,
            Is(item.Unit, Conditions.Broken), Is(item.Unit, Conditions.DesperationMorale))).ToArray())
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                new ConditionsChanged(brokenId, new Dictionary<string, ConditionState> { [Conditions.DesperationMorale] = ConditionState.True }), package, null));
        }

        // A14.4 (pass 71, ruling R71.10): the attack resolved, the attacked side's units in the target Location that may take a Sniper Check are offered it,
        // read from the state after the attack only when the Location held a unit of that side on foot before it.
        if (events.Any(item => item.Payload is ChoicePending)
            || !index.At(location).OfType<UnitInstance>().Any(unit => unit.Side == attacked && index.VehicleOf(unit.Id) is null && !LiveFire.IsVehicle(unit))
            || Replay([.. existing, .. events]).Current is not { } attackedState || SniperCheckers(attackedState, location, attacked).Count == 0)
        {
            return false;
        }

        var resume = new JsonObject
        {
            ["record"] = ChoicePending.SniperCheck,
            ["sniper"] = sniper.Id,
            ["sniperSide"] = sniperSide,
            ["location"] = location.ToString(),
            ["rest"] = new JsonArray([.. rest.Select(item => (JsonNode)new JsonObject { ["roll"] = item.Roll, ["sniper"] = item.Sniper })]),
        };
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "choice-pending", new ChoicePending($"{ChoicePending.SniperCheck}:{sniper.Id}:{location}",
            ChoicePending.SniperCheck, attacked, [ChoicePending.Take, ChoicePending.Decline], JsonSerializer.SerializeToElement(resume)), null, null));
        return true;
    }

    /// <summary>
    /// The units of the attacked side in a Sniper attack's target Location that may take the Sniper Check (A14.4; pass 71, ruling R71.10), read from the
    /// state's index, in id order: Personnel on foot, in Good Order, unpinned, not TI, armed, not hidden, and neither fired nor moved this Player Turn (a fire
    /// marker, a fire this phase, a Gun crew's shot, or the state's list of the units that moved or fired in an earlier phase).
    /// </summary>
    private List<UnitInstance> SniperCheckers(GameState state, BoardLocation location, string attacked)
    {
        var index = MemoOf(state).Objects(state);
        bool FiredOrMoved(UnitInstance unit) => Is(unit, Conditions.PrepFire) || Is(unit, Conditions.FirstFire) || Is(unit, Conditions.FinalFire) || Is(unit, Conditions.BoundingFire)
            || unit.MfSpent > 0 || unit.HalfMfSpent || state.PhaseFirers.Any(item => item.Unit == unit.Id) || state.GunCrewsFired.Contains(unit.Id, StringComparer.Ordinal)
            || state.FiredOrMovedThisPlayerTurn.Contains(unit.Id, StringComparer.Ordinal);
        return [.. index.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == attacked
                && ScenarioA1Sniper.MayTakeSniperCheck(new SniperCheckCandidate(vocabulary.IsA(unit.Kind, "asl:personnel") && unit.Kind != UnitKinds.Dummy && index.VehicleOf(unit.Id) is null,
                    GameState.GoodOrder(unit, vocabulary) != ConditionState.False, Is(unit, Conditions.Pinned), Is(unit, "asl:ti"), Is(unit, Conditions.Unarmed), Is(unit, Conditions.Hidden),
                    FiredOrMoved(unit))))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];
    }

    /// <summary>
    /// A Sniper eliminated (A14.4, p. 83; pass 71, ruling R71.9): "reduces that side's SAN by one for the duration of the scenario", and "If the SAN is reduced
    /// to 1, remove the Sniper Target Selection counter from the game"; Rules gives the actual SAN, never below the printed one at night (E1.76). A side with
    /// no SAN, which has no Sniper by A14.01, only loses the counter.
    /// </summary>
    private void ReduceSan(GameScope scope, string attemptId, long expected, List<GameEvent> events, GameState state, EntityInstance sniper, string cause, string? package)
    {
        if (state.Side(sniper.Side ?? string.Empty) is { San: { } printed } side)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "san-reduced", new SanReduced(side.Id, cause), package, null));
            if (!ScenarioA1Sniper.CounterRemoved(ScenarioA1Sniper.ActualSan(printed, state.Night, side.SanReductions + 1)))
            {
                return;
            }
        }

        events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(sniper.Id), package, null));
    }

    /// <summary>
    /// The attacked side's answer to its Sniper Check (A14.4, p. 83; pass 71, ruling R71.10). Taken, every unit in the target Location that may take it makes
    /// the one combined DR, with the DRM of most benefit (Rules decides it), and becomes TI, losing its "?" in the LOS of a Good Order enemy ground unit
    /// within 16 hexes (A12.141); a Final DR of 2 or less eliminates the Sniper (ruling R71.9), a 3 pins it. The DR calls no Sniper (A14.1 names no Sniper
    /// Check). Then the Sniper attacks still to resolve follow, in the order of their DRs. A record that names a unit kept beneath its "?" is its owner's.
    /// </summary>
    private GamePlan PlanSniperCheckAnswer(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, PendingChoice pending, GameEvent made, string option, string summary)
    {
        var resume = pending.Resume;
        var sniperId = resume.GetProperty("sniper").GetString()!;
        var at = BoardLocation.Parse(resume.GetProperty("location").GetString()!);
        (string Roll, string Sniper)[] rest = [.. resume.GetProperty("rest").EnumerateArray().Select(item => (item.GetProperty("roll").GetString()!, item.GetProperty("sniper").GetString()!))];
        var answered = state with
        {
            Choice = null
        };
        var sniper = answered.Find(sniperId) as EntityInstance;
        List<UnitInstance> units = option == ChoicePending.Take && sniper is { Status: InstanceStatus.Active } ? SniperCheckers(answered, at, pending.Side) : [];
        if (units.Count == 0 && rest.Length == 0)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [made], [summary]);
        }

        var (drm, causes) = ScenarioA1Sniper.SniperCheckDrm([.. units.Select(unit => new SniperCheckParticipant(unit.Kind,
            DefinitionOf(unit) is { IsLeader: true, Leadership: { } leadership } ? leadership + (Is(unit, Conditions.Wounded) ? 1 : 0) : null, Is(unit, Conditions.Heroic)))]);
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent> { made };
            if (units.Count > 0 && sniper is not null)
            {
                var package = ScenarioA1FirePackage.Identity.ToString();
                var rollId = $"{attemptId}-roll-1";
                var dice = draw(new RollRequest(2, 6));
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled", new DiceRolled(rollId, "sniper-check", 2, 6, dice.Values, DiceRolled.SystemSource, actor),
                    package, null));
                var result = ScenarioA1Sniper.SniperCheckResult(dice.Values.Sum() + drm, Is(sniper, Conditions.Pinned));

                // A12.141: the participants lose their "?" in the LOS of a Good Order enemy ground unit within 16 hexes; those that keep it are not named to the
                // Sniper's side, nor is the record that names them.
                var concealed = units.Any(unit => Is(unit, Conditions.Concealed));
                var exposed = concealed && EnemyGoodOrderInLosWithin16(answered, pending.Side, at);
                IReadOnlyList<string>? owner = concealed && !exposed ? [pending.Side] : null;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "sniper-checked", new SniperChecked(sniperId, at, [.. units.Select(unit => unit.Id)], rollId, drm, result),
                    package, owner));
                foreach (var unit in units)
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                        new ConditionsChanged(unit.Id, ConditionChanges(ScenarioA1Sniper.SniperCheckConditions(exposed && Is(unit, Conditions.Concealed)))), package,
                        Is(unit, Conditions.Concealed) && !exposed ? [pending.Side] : null));
                }

                if (result == ScenarioA1Sniper.SniperEliminated)
                {
                    ReduceSan(scope, attemptId, expected, events, answered, sniper, ScenarioA1Sniper.SniperCheckCause, package);
                }
                else if (result == ScenarioA1Sniper.SniperPinned)
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                        new ConditionsChanged(sniper.Id, new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.Pinned] = ConditionState.True }), package, null));
                }
            }

            SnipeInOrder(scope, attemptId, expected, actor, existing, events, draw, rest);
            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [summary, units.Count == 0 ? "play.sniper-check: the Sniper attacks still to resolve follow (A14.1)"
            : $"play.sniper-check: {string.Join(", ", units.Select(unit => unit.Id))} in {at} make one combined Sniper Check DR{(drm == 0 ? string.Empty : $", DRM {drm:+0;-0} ({string.Join(", ", causes)})")}: a Final DR of 2 or less eliminates the Sniper, a 3 pins it, and they become TI (A14.4)"])
        {
            Roll = new PlannedRoll("choice", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// The Sniper player's answer (A14.2, A14.21; pass 71, ruling R71.12): the attack resumes from the rolls it recorded and the answers so far, draws its next
    /// dice in this commit, and may stop at a later choice; then the Sniper attacks still to resolve follow, in the order of their DRs (A14.1).
    /// </summary>
    private GamePlan PlanSniperAnswer(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        PendingChoice pending, GameEvent made, string option, string summary)
    {
        var resume = pending.Resume;
        var answers = resume.GetProperty("answers").EnumerateObject().ToDictionary(item => item.Name, item => item.Value.GetString()!, StringComparer.Ordinal);
        answers[resume.GetProperty("step").GetString()!] = option;
        var rolls = resume.GetProperty("rolls");
        string? RollOf(string name) => rolls.TryGetProperty(name, out var roll) ? roll.GetString() : null;
        var resumed = new SniperResume(BoardLocation.Parse(resume.GetProperty("from").GetString()!), RollOf("sniper")!, RollOf("location"), RollOf("selection"),
            resume.GetProperty("moved").GetBoolean(), answers);
        var sniperId = resume.GetProperty("sniper").GetString()!;
        var trigger = resume.GetProperty("trigger").GetString()!;
        (string Roll, string Sniper)[] rest = [.. resume.GetProperty("rest").EnumerateArray().Select(item => (item.GetProperty("roll").GetString()!, item.GetProperty("sniper").GetString()!))];
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent> { made };
            if (!Snipe(scope, attemptId, expected, actor, existing, events, draw, sniperId, trigger, rest, resumed))
            {
                SnipeInOrder(scope, attemptId, expected, actor, existing, events, draw, rest);
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [summary])
        {
            Roll = new PlannedRoll("choice", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>The id of the HS a Sniper's Casualty Reduction leaves (A7.302).</summary>
    private static string ReducedBySniper(string attemptId, string unitId) => $"{attemptId}-{unitId}-sniped";

    /// <summary>
    /// The attacked side's Sniper targets (A14.22, A14.31; pass 71, rulings R71.3, R71.4, R71.6), read once from the state's index: the units on foot; for each
    /// vehicle, its Vulnerable PRC, its Inherent crew, and the unarmored vehicle itself, as Rules names them; and the side's Sniper counter. A unit inside a
    /// vehicle is read only with its vehicle, so a BU AFV's Passengers and crew are no targets.
    /// </summary>
    private static List<SniperTarget> SniperTargets(GameState state, ObjectIndex index, string attacked)
    {
        var found = new List<SniperTarget>();
        foreach (var unit in state.Units)
        {
            if (!ScenarioA1Sniper.Eligible(unit.Status == InstanceStatus.Active, unit.Side == attacked, Is(unit, Conditions.Hidden), Is(unit, Conditions.Captured),
                index.Location(unit.Id) is not null) || index.VehicleOf(unit.Id) is not null)
            {
                continue;
            }

            var at = index.Location(unit.Id)!.Location;
            if (!LiveFire.IsVehicle(unit))
            {
                found.Add(new SniperTarget(unit, ScenarioA1Sniper.UnitTarget, at));
                continue;
            }

            // A14.22 (ruling R71.3): the PRC's exposure as a Collateral Attack reads it (D.8, D6.61, D6.62); a Stunned, Recalled, or Shocked AFV's Passengers spared.
            var definition = VehicleDefinition(unit);
            var crewExposed = LiveFire.CrewExposed(unit);
            var impaired = Is(unit, Conditions.Stunned) || Is(unit, Conditions.Recalled) || Is(unit, Conditions.Shocked);
            UnitInstance[] aboard = [.. index.Aboard(unit.Id).Where(prc => !Is(prc, Conditions.Hidden) && !Is(prc, Conditions.Captured))];
            string[] exposures = definition is null ? [] : [.. aboard.Select(prc => ScenarioA1PrcRules.Exposure(new FirePrc(prc.Id, prc.Definition?.Definition,
                prc.Position is ContainedPosition { Role: ContainmentRole.Rider } ? FirePrc.Rider : FirePrc.Passenger, Is(prc, Conditions.Broken) ? true : null,
                Is(prc, Conditions.Pinned) ? true : null), definition, crewExposed, impaired))];
            var manned = !Is(unit, Conditions.Abandoned) && !(index.CrewOf(unit.Id) is { } crew && Is(crew, Conditions.Broken));
            var targets = ScenarioA1Sniper.VehicleTargets(new SniperVehicleFacts(definition?.Unarmored == true, definition?.PartiallyArmored == true, crewExposed, manned, exposures));
            found.AddRange(targets.Prc.Select(position => new SniperTarget(aboard[position], ScenarioA1Sniper.UnitTarget, at)));
            if (targets.Crew)
            {
                found.Add(new SniperTarget(unit, ScenarioA1Sniper.CrewTarget, at));
            }

            if (targets.Vehicle)
            {
                found.Add(new SniperTarget(unit, ScenarioA1Sniper.VehicleTarget, at));
            }
        }

        // A14.21, A14.31 (ruling R71.6): the enemy Sniper counter is a target too.
        foreach (var entity in state.Entities)
        {
            if (entity.Kind == "asl:sniper" && entity.Side == attacked && entity.Status == InstanceStatus.Active && entity.Position is MapPosition position)
            {
                found.Add(new SniperTarget(entity, ScenarioA1Sniper.SniperTarget, position.Location));
            }
        }

        return found;
    }

    /// <summary>
    /// A14.3 (ruling R15.5): dr 1 eliminates a SMC and breaks a MMC, Casualty Reducing one that cannot break (broken or berserk); dr 2 wounds a SMC (its Wound
    /// Severity dr, A17.11) and pins a MMC not immune to Pin results. A concealed unit keeps its "?" out of every Good Order enemy LOS unless it breaks, is
    /// wounded, or is Reduced (A12.14; pass 71, ruling R71.8), and the record then names no unit. Rules decides the effect (pass 32.h). The result recorded.
    /// </summary>
    private string SnipeUnit(GameScope scope, string attemptId, long expected, List<GameEvent> events, UnitInstance unit, string rollId, int dr,
        BoardLocation location, string package, Func<int, string, string> roll, Func<string, IReadOnlyList<int>> values,
        Action<string, int, BoardLocation?, string?, string> record, Func<bool> seen)
    {
        void Change(IReadOnlyList<(UnitCondition Condition, bool Value)> conditions) =>
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.Id, ConditionChanges(conditions)), package, null));
        void Eliminate() => events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(unit.Id), package, null));

        var smc = unit.Kind is "asl:leader" or "asl:hero";
        var effect = ScenarioA1Sniper.UnitEffect(smc, dr, Is(unit, Conditions.Broken), Is(unit, Conditions.Berserk), Is(unit, Conditions.Pinned));
        var mortal = effect == SniperEffect.WoundSeverity && ScenarioA1Sniper.Mortal(values(roll(1, "sniper-wound-severity"))[0], Is(unit, Conditions.Wounded));

        // A7.302: a squad is Reduced to its HS, a HS eliminated.
        var half = effect == SniperEffect.CasualtyReduction && unit.Kind == "asl:squad" && unit.Definition is { } reference
            && ScenarioA1FireReference.HalfSquadOf(reference.Definition) is { } halfId && FireReference.Value.Definitions.TryGetValue(halfId, out var halfDefinition)
            ? halfDefinition : null;
        var result = effect switch
        {
            SniperEffect.Eliminated => ScenarioA1Sniper.Eliminated,
            SniperEffect.WoundSeverity => mortal ? ScenarioA1Sniper.Eliminated : ScenarioA1Sniper.Wounded,
            SniperEffect.Broken => ScenarioA1Sniper.Broken,
            SniperEffect.CasualtyReduction => ScenarioA1Sniper.ReductionResult(half is not null),
            SniperEffect.Pinned => ScenarioA1Sniper.Pinned,
            _ => ScenarioA1Sniper.NoEffect,
        };
        var concealed = Is(unit, Conditions.Concealed);
        var keeps = ScenarioA1Sniper.KeepsConcealment(concealed, result, seen);
        record(rollId, dr, location, keeps ? null : unit.Id, result);
        switch (effect)
        {
            case SniperEffect.Eliminated:
                Eliminate();
                break;
            case SniperEffect.WoundSeverity when mortal:
                Eliminate();
                break;
            case SniperEffect.WoundSeverity:
                Change(ScenarioA1Sniper.WoundedConditions());
                break;
            case SniperEffect.Broken:
                Change(ScenarioA1Sniper.BrokenConditions());
                break;
            case SniperEffect.CasualtyReduction when half is not null:
                var produced = unit.Conditions.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
                produced[Conditions.Concealed] = ConditionState.False;
                produced[Conditions.Hidden] = ConditionState.False;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "lineage", new LineageRecorded(LineageAction.Reduced, [unit.Id],
                    [new NewInstance(ReducedBySniper(attemptId, unit.Id), half.Kind, half.Id, unit.Side, unit.Position, null, produced)]), package, null));
                break;
            case SniperEffect.CasualtyReduction:
                Eliminate();
                break;
            case SniperEffect.Pinned:
                Change(ScenarioA1Sniper.PinnedConditions(keeps));
                break;
            default:
                // A12.14 (ruling R71.8): an attack by a dr of 1 or 2 with no effect still costs the "?" in a Good Order enemy's LOS.
                if (concealed && !keeps)
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.Id, Unconcealed()), package, null));
                }

                break;
        }

        return result;
    }

    /// <summary>
    /// A14.3, A14.33, D5.34, D5.341, D5.342 (ruling R71.5): a Sniper attack on a vehicle's Inherent crew or on an unarmored vehicle itself. A Stun, a Recall, an
    /// Immobilization, or a crew's pin is the vehicle's <c>conditions-changed</c>, as the Fire path writes one, so the projector's Stun, Recall, BU, and SMC
    /// crew handling apply; a broken Inherent crew is placed broken inside and routs beneath it, as an Unprotected crew is (D5.311, whose "[EXC: Sniper attack]"
    /// leaves the break to A14.3). A concealed vehicle keeps its "?" as a unit does (A12.14; ruling R71.8).
    /// </summary>
    private void SnipeVehicle(GameScope scope, string attemptId, long expected, List<GameEvent> events, GameState state, SniperTarget target, string rollId, int dr,
        BoardLocation location, string package, Action<string, int, BoardLocation?, string?, string> record, Func<bool> seen)
    {
        var vehicle = (UnitInstance)target.Item;
        var definition = VehicleDefinition(vehicle);
        var result = target.Role == ScenarioA1Sniper.CrewTarget
            ? ScenarioA1Sniper.CrewResult(dr, definition is not ({ Unarmored: true } or { PartiallyArmored: true }), Is(vehicle, Conditions.StunRecovery), Is(vehicle, Conditions.Pinned))
            : ScenarioA1Sniper.VehicleResult(dr, Is(vehicle, Conditions.Immobilized));
        var concealed = Is(vehicle, Conditions.Concealed);
        var keeps = ScenarioA1Sniper.KeepsConcealment(concealed, result, seen);
        record(rollId, dr, location, keeps ? null : vehicle.Id, result);
        var changed = VehicleConditionNames(ScenarioA1Sniper.VehicleConditions(result)) ?? new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        if (concealed && !keeps)
        {
            foreach (var (name, value) in Unconcealed())
            {
                changed[name] = value;
            }
        }

        if (changed.Count > 0)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(vehicle.Id, changed), package, null));
        }

        if (result == ScenarioA1Sniper.CrewBroken)
        {
            foreach (var (type, payload) in UnprotectedCrewEvents(state, vehicle, attemptId, broken: true))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null));
            }
        }
    }

    /// <summary>
    /// A14.3, A14.31 (ruling R71.6): a Sniper attack on the enemy Sniper counter: dr 1 eliminates the Sniper, which reduces its side's SAN and removes the
    /// counter only below 2 (A14.4; ruling R71.9); dr 2 pins it until the CCPh's end (the projector clears the pin with the others).
    /// </summary>
    private void SnipeSniper(GameScope scope, string attemptId, long expected, List<GameEvent> events, GameState state, EntityInstance enemy, string rollId, int dr,
        BoardLocation location, string package, Action<string, int, BoardLocation?, string?, string> record)
    {
        var result = ScenarioA1Sniper.SniperResult(dr, Is(enemy, Conditions.Pinned));
        record(rollId, dr, location, enemy.Id, result);
        if (result == ScenarioA1Sniper.SniperEliminated)
        {
            ReduceSan(scope, attemptId, expected, events, state, enemy, ScenarioA1Sniper.SniperAttackCause, package);
        }
        else if (result == ScenarioA1Sniper.SniperPinned)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                new ConditionsChanged(enemy.Id, new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.Pinned] = ConditionState.True }), package, null));
        }
    }

    /// <summary>The conditions of a unit that loses its "?" and hidden status (A12.14).</summary>
    private static Dictionary<string, ConditionState> Unconcealed() =>
        new(StringComparer.Ordinal)
        {
            [Conditions.Concealed] = ConditionState.False,
            [Conditions.Hidden] = ConditionState.False
        };

    /// <summary>The hex across a hexside, on one board or across a seam of a placed map; null at the edge of the map.</summary>
    private (BoardRef Board, HexName Hex)? Next(GameState state, BoardRef board, HexName hex, HexsideDirection side) =>
        Composed(state) is { } composed ? composed.Neighbor(board, hex, side)
            : Handle(state, board) is { } handle && handle.Neighbor(hex, side) is { } next
                ? (board, next) : null;

    /// <summary>The distance in hexes between two hexes of the map; null when it cannot be read.</summary>
    private int? Distance(GameState state, BoardRef fromBoard, HexName from, BoardRef toBoard, HexName to) =>
        Composed(state) is { } composed ? composed.Distance(fromBoard, from, toBoard, to)
            : fromBoard == toBoard && Handle(state, fromBoard) is { } handle ? handle.Distance(from, to) : null;
}
