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
/// before Random Selection, and which target Random Selection gave takes the dr; an older record's picks are its recorded events. Since increment 4e (ruling
/// R71.13) every record of a planned step that carries a TH, MC, TC, or IFT DR is read, ordnance's included, and a Sniper's own LLMC calls the next. The rules are
/// <see cref="ScenarioA1Sniper"/>'s (pass 32.h); this file reads the records, the state, and the map, draws the dice, and writes the events.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// The trigger reader (A14.1, p. 82; pass 71, ruling R71.13, which supersedes R55.30): every TH, MC, TC, or IFT Original DR of the step's records that equals
    /// the enemy's SAN, in the PFPh, MPh, DFPh, or AFPh, calls for that enemy's Sniper attack, resolved after the step's own effects, in the order the DRs were
    /// made. The records are the fire records (with a vehicle crew's checks), the ordnance records (To Hit, Canister, the HE hit's IFT and checks, the crew
    /// checks), <c>morale-check-resolved</c>, the Bail Out, PAATC, and both OVR NTCs; Rules names each roll's maker. A Sniper attack's own LLMC DRs call the
    /// next attacks, which the attack resolves before its Sniper Check and any later trigger (A14.3; ruling R71.25). The planner runs it once per planned step,
    /// after Fire's own calls: a trigger already answered by a
    /// <c>sniper-attacked</c> record, or whose Sniper is gone or pinned, is not called again. A pending choice of the step stops it (its resume runs it); one
    /// of a Sniper attack keeps the triggers still to resolve in its resume.
    /// </summary>
    private void AddSniperAttacks(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, List<GameEvent> events,
        Func<RollRequest, RollResult> draw)
    {
        // Design section 7: a step with no active Sniper counter on the map returns at once, read from the state's memo.
        if (Replay(existing).Current is not { } before || MemoOf(before).Snipers(before) is not { Count: > 0 } snipers)
        {
            return;
        }

        var walk = new SniperTriggerWalk(before, snipers);
        var from = 0;
        while (true)
        {
            ReadTriggers(before, existing, events, from, walk);
            from = events.Count;
            var pending = events.FindLastIndex(item => item.Payload is ChoicePending);
            if (pending >= 0)
            {
                // Ruling R71.12: a Sniper attack that waits for a choice keeps in its resume the triggers its own LLMC made (A14.1, A14.3).
                if (events[pending].Payload is ChoicePending { Kind: ChoicePending.Sniper or ChoicePending.SniperCheck } choice)
                {
                    KeepTriggers(events, pending, choice, walk.Unresolved());
                }

                return;
            }

            if (walk.Unresolved() is not { Length: > 0 } triggers)
            {
                return;
            }

            SnipeInOrder(scope, attemptId, expected, actor, existing, events, draw, [.. triggers.Select(item => new SniperQueued(item.Roll, item.Sniper))]);
            walk.Answered.UnionWith(triggers.Select(item => item.Roll));
        }
    }

    /// <summary>A planned step's events with the Sniper attacks its DRs call for (ruling R71.13), drawn after the step's own dice.</summary>
    private List<GameEvent> WithSniperAttacks(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing,
        IReadOnlyList<GameEvent> built, Func<RollRequest, RollResult> draw)
    {
        var events = built as List<GameEvent> ?? [.. built];
        AddSniperAttacks(scope, attemptId, expected, actor, existing, events, draw);
        return events;
    }

    /// <summary>
    /// What is still to resolve after a Sniper attack (A14.1, A14.3, A14.4): the attack its <see cref="Roll"/> calls for, or, with <see cref="Check"/>, the
    /// Sniper Check the attack that roll called offers in that Location, owed once the attacks its own LLMC DRs called are resolved (pass 71, ruling R71.25).
    /// </summary>
    private readonly record struct SniperQueued(string Roll, string Sniper, BoardLocation? Check = null);

    /// <summary>A queue as a pending Sniper choice's resume keeps it, "rest": each item its roll and Sniper, and an owed Sniper Check its Location.</summary>
    private static JsonArray QueueJson(IEnumerable<SniperQueued> queue) => new([.. queue.Select(item => (JsonNode)(item.Check is { } at
        ? new JsonObject { ["roll"] = item.Roll, ["sniper"] = item.Sniper, ["check"] = at.ToString() }
        : new JsonObject { ["roll"] = item.Roll, ["sniper"] = item.Sniper }))]);

    /// <summary>The queue a pending Sniper choice's resume keeps; an item with no "check", as every one recorded before ruling R71.25, is an attack.</summary>
    private static List<SniperQueued> QueueOf(JsonElement resume) => [.. resume.GetProperty("rest").EnumerateArray().Select(item => new SniperQueued(item.GetProperty("roll").GetString()!,
        item.GetProperty("sniper").GetString()!, item.TryGetProperty("check", out var check) ? BoardLocation.Parse(check.GetString()!) : null))];

    /// <summary>A pending Sniper choice's resume with the triggers still to resolve added after its own, those it already keeps or answers left out.</summary>
    private static void KeepTriggers(List<GameEvent> events, int at, ChoicePending choice, (string Roll, string Sniper)[] triggers)
    {
        var resume = JsonNode.Parse(choice.Resume.GetRawText())!.AsObject();
        var rest = resume["rest"]!.AsArray();
        var kept = rest.Select(item => item!["roll"]!.GetValue<string>()).Append(resume["trigger"]?.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
        var added = false;
        foreach (var (roll, sniper) in triggers.Where(item => !kept.Contains(item.Roll)))
        {
            rest.Add(new JsonObject { ["roll"] = roll, ["sniper"] = sniper });
            added = true;
        }

        if (added)
        {
            events[at] = events[at] with
            {
                Payload = choice with
                {
                    Resume = JsonSerializer.SerializeToElement(resume)
                }
            };
        }
    }

    /// <summary>
    /// What the trigger reader knows as it walks a step's events once (ruling R71.13): the phase and phasing side (a step may change the phase), each side's
    /// SAN reductions so far, the Sniper counters gone or pinned, the dice by roll, the triggers found in DR order, and those a <c>sniper-attacked</c> record
    /// has answered.
    /// </summary>
    private sealed class SniperTriggerWalk(GameState before, Dictionary<string, EntityInstance> snipers)
    {
        public string? Phase { get; set; } = before.Phase;

        public string? PhasingSide { get; set; } = before.PhasingSide;

        public Dictionary<string, int> Reductions { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, bool> Pinned { get; } = snipers.Values.ToDictionary(item => item.Id, item => Is(item, Conditions.Pinned), StringComparer.Ordinal);

        public HashSet<string> Gone { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, (DiceRolled Roll, int Order)> Dice { get; } = new(StringComparer.Ordinal);

        public List<(string Roll, string Sniper, int Order)> Found { get; } = [];

        public HashSet<string> Answered { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, EntityInstance> Snipers { get; } = snipers;

        /// <summary>The triggers no record has answered whose Sniper is still active and unpinned, once each, in the order their DRs were made.</summary>
        public (string Roll, string Sniper)[] Unresolved() =>
            [.. Found.Where(item => !Answered.Contains(item.Roll) && !Gone.Contains(item.Sniper) && !Pinned.GetValueOrDefault(item.Sniper)).DistinctBy(item => item.Roll)
                .OrderBy(item => item.Order).Select(item => (item.Roll, item.Sniper))];
    }

    /// <summary>
    /// Walks the step's events from <paramref name="from"/> (ruling R71.13): each record that carries a roll gives Rules the roll's kind and the reads its maker
    /// needs, and a two-dice roll its maker made equal to the enemy's actual SAN, in a trigger phase, is a trigger of the enemy's Sniper counter.
    /// </summary>
    private void ReadTriggers(GameState before, IReadOnlyList<GameEvent> existing, List<GameEvent> events, int from, SniperTriggerWalk walk)
    {
        var index = MemoOf(before).Objects(before);
        (string? Side, bool Captured)? Checker(string unitId) => index.Unit(unitId) is { } unit ? (unit.Side, Is(unit, Conditions.Captured)) : null;
        string? SideOf(string? unitId) => unitId is null ? null : index.Unit(unitId)?.Side;

        // Table player, pass 27: a record resumed after an owner's choice uses dice drawn in an earlier commit, which come before this step's.
        (DiceRolled Roll, int Order)? Dice(string rollId)
        {
            if (walk.Dice.TryGetValue(rollId, out var found))
            {
                return found;
            }

            for (var position = existing.Count - 1; position >= 0; position--)
            {
                if (existing[position].Payload is DiceRolled earlier && earlier.Roll == rollId)
                {
                    return (earlier, position - existing.Count);
                }
            }

            return null;
        }

        void Consider(string rollId, string kind, string unitId, string? firingSide, Func<string?> vehicleSide)
        {
            if (!ScenarioA1Sniper.TriggerPhase(walk.Phase) || Dice(rollId) is not { } roll)
            {
                return;
            }

            // A14.1: whose DR it is, never a prisoner's, and only a DR of two dice calls (Rules decides both). E1.76 (ruling R16.7), A14.4 (ruling R71.9): the
            // enemy's SAN is its actual one, less the reductions so far, never below the printed SAN at night.
            var maker = ScenarioA1Sniper.RollMaker(kind, unitId.Length > 0, firingSide, () => Checker(unitId), vehicleSide);
            if (maker is not null && before.Sides.FirstOrDefault(item => item.Id != maker) is { San: { } printed } enemy && walk.Snipers.TryGetValue(enemy.Id, out var sniper)
                && ScenarioA1Sniper.Triggers(roll.Roll.Values, printed, before.Night, enemy.SanReductions + walk.Reductions.GetValueOrDefault(enemy.Id)))
            {
                walk.Found.Add((rollId, sniper.Id, roll.Order));
            }
        }

        void Keyed(IReadOnlyDictionary<string, string> rolls, string? firingSide, Func<string?> vehicleSide)
        {
            foreach (var (key, rollId) in rolls)
            {
                var (kind, unitId) = ScenarioA1Sniper.RollKey(key);
                Consider(rollId, kind, unitId, firingSide, vehicleSide);
            }
        }

        static string? VehicleIn(JsonElement facts, string name) =>
            facts.ValueKind == JsonValueKind.Object && facts.TryGetProperty(name, out var vehicle) && vehicle.ValueKind == JsonValueKind.Object
                && vehicle.TryGetProperty("vehicleId", out var id) ? id.GetString() : null;

        for (var position = from; position < events.Count; position++)
        {
            switch (events[position].Payload)
            {
                case DiceRolled rolled:
                    walk.Dice[rolled.Roll] = (rolled, position);
                    break;
                case PhaseChanged phase:
                    (walk.Phase, walk.PhasingSide) = (phase.Phase, phase.PhasingSide);
                    break;
                case SanReduced reduced:
                    walk.Reductions[reduced.Side] = walk.Reductions.GetValueOrDefault(reduced.Side) + 1;
                    break;
                case InstanceEliminated gone when walk.Pinned.ContainsKey(gone.Id):
                    walk.Gone.Add(gone.Id);
                    break;
                case ConditionsChanged changed when walk.Pinned.ContainsKey(changed.Id) && changed.Conditions.TryGetValue(Conditions.Pinned, out var pin):
                    walk.Pinned[changed.Id] = pin == ConditionState.True;
                    break;
                case SniperAttacked attacked:
                    walk.Answered.Add(attacked.Trigger);
                    break;

                // A14.1: an IFT DR is its firing side's; a MC or TC its unit's; a vehicle crew's check that names no unit the target vehicle's side's.
                case FireResolved fire:
                    Keyed(fire.Rolls, FiringSideOf(before, index, fire, walk.PhasingSide), () => SideOf(VehicleIn(fire.Facts, "targetVehicle")));
                    break;
                case OrdnanceFired shot:
                    Keyed(shot.Rolls, SideOf(shot.Crew) ?? SideOf(shot.Gun), () => SideOf(VehicleIn(shot.Facts, "vehicleTarget")));
                    break;
                case MoraleCheckResolved check:
                    Keyed(check.Rolls, null, static () => null);
                    break;
                case RiderBailedOut bailed:
                    Consider(bailed.Roll, ScenarioA1Sniper.BailOutRoll, bailed.Unit, null, static () => null);
                    break;
                case PaatcTaken paatc when paatc.Units.Count > 0:
                    Consider(paatc.Roll, ScenarioA1Sniper.PaatcRoll, paatc.Units[0], null, static () => null);
                    break;
                case TaskCheck task:
                    Consider(task.Roll, ScenarioA1Sniper.NtcRoll, task.Id, null, static () => null);
                    break;
                case InfantryOverrunNtc ntc:
                    Consider(ntc.Roll, ScenarioA1Sniper.NtcRoll, ntc.Taker, null, static () => null);
                    break;
            }
        }
    }

    /// <summary>
    /// The Sniper attacks called for, in the order their DRs were made (A14.1), until one stops at a choice, the Sniper player's (A14.2; pass 71, ruling
    /// R71.12) or a Sniper Check (A14.4; ruling R71.10): the attacks after it are kept in the choice and resolve after the answer, in the same order. Ruling
    /// R71.25: an attack resolved puts the attacks its own LLMC DRs call, then its Sniper Check, at the head of what is still to resolve (A14.3: an attack "is
    /// considered resolved only after the full resolution of all further Sniper attacks generated by ... all LLMC/LLTC"; A14.4: the check comes after it).
    /// </summary>
    private void SnipeInOrder(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, List<GameEvent> events,
        Func<RollRequest, RollResult> draw, IReadOnlyList<SniperQueued> queue)
    {
        var rest = new List<SniperQueued>(queue);
        while (rest.Count > 0)
        {
            var next = rest[0];
            rest.RemoveAt(0);
            if (next.Check is { } at ? OfferSniperCheck(scope, attemptId, expected, existing, events, next, at, rest)
                : Snipe(scope, attemptId, expected, actor, existing, events, draw, next.Sniper, next.Roll, rest))
            {
                return;
            }
        }
    }

    /// <summary>
    /// The side that made a fire record's IFT DR: its firers', a DC's user's, a Canister shot's Gun's (its vehicle's, or the Gun's crew's), or the DEFENDER's
    /// for Residual FP (A8.2), the side not phasing when the record was made; the record's reads are made as Rules asks, from the state's index.
    /// </summary>
    private static string? FiringSideOf(GameState state, ObjectIndex index, FireResolved fire, string? phasingSide) =>
        ScenarioA1Sniper.FiringSide(fire.Firers.Count > 0, () => index.Unit(fire.Firers[0])?.Side,
            () => fire.Facts.TryGetProperty("demolitionCharge", out var charge) && charge.TryGetProperty("userId", out var user) && user.GetString() is { } userId
                ? index.Unit(userId)?.Side : null,
            () => fire.Facts.TryGetProperty("canister", out var canister) && canister.ValueKind == JsonValueKind.Object && canister.TryGetProperty("gunId", out var gunId)
                && gunId.GetString() is { } gun
                ? index.Unit(gun)?.Side ?? (state.Find(gun) is EquipmentInstance equipment ? equipment.Side ?? (equipment.Holding?.Holder is { } crew ? index.Unit(crew)?.Side : null) : null)
                : null,
            () => fire.Facts.TryGetProperty("fireKind", out var kind) && kind.GetString() == ScenarioA1FireCalculator.ResidualFire,
            () => state.Sides.FirstOrDefault(item => item.Id != phasingSide)?.Id);

    /// <summary>
    /// A Sniper target (A14.2 EXC, A14.22, A14.31; pass 71, ruling R71.3): a unit (on foot or a Vulnerable PRC), a vehicle's Inherent crew or the unarmored
    /// vehicle itself (the vehicle as the item, by <see cref="Role"/>), or the enemy Sniper counter, with the Location it is attacked in. <see cref="Stacked"/>:
    /// part of the Location's concealed stack, which Random Selection counts as one (A14.23): a concealed unit or a Dummy, and the PRC, crew, and vehicle of
    /// a concealed vehicle (A12.2; ruling R71.20), as Rules decides.
    /// </summary>
    private readonly record struct SniperTarget(IGameObject Item, string Role, BoardLocation At, bool Stacked);

    /// <summary>
    /// What a Sniper attack resumed after its Sniper player's answer reads again (A14.2; pass 71, ruling R71.12): the counter's Location when the attack began
    /// (it may have moved to the Location attacked since), the rolls already recorded, whether the counter has moved, and the answers so far by step. Since
    /// R1's finding 4: the Random Selection roll inside a concealed stack, and the new Sniper drs of the targets the outer Random Selection tied, in order.
    /// </summary>
    private sealed record SniperResume(BoardLocation From, string SniperRoll, string? LocationRoll, string? SelectionRoll, bool Moved, IReadOnlyDictionary<string, string> Answers)
    {
        public string? StackRoll
        {
            get; init;
        }

        public IReadOnlyList<string> AgainRolls
        {
            get; init;
        } = [];
    }

    /// <summary>
    /// One Sniper attack (A14.2, A14.3); true when it stops at a choice whose resume keeps <paramref name="rest"/>, the attacks still to resolve: the Sniper
    /// player's (A14.2, A14.21; pass 71, ruling R71.12), or the attacked side's Sniper Check (A14.4; ruling R71.10). <paramref name="resumed"/> continues
    /// an attack from the Sniper player's answers and the rolls it recorded. Resolved without a stop, it puts what it owes at the head of
    /// <paramref name="rest"/> (ruling R71.25).
    /// </summary>
    private bool Snipe(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, List<GameEvent> events,
        Func<RollRequest, RollResult> draw, string sniperId, string trigger, List<SniperQueued> rest, SniperResume? resumed = null)
    {
        // Pass 71 (design section 5, item 4): the state after the commit's events so far is taken once, and every Location is read from its index; the
        // replay is not kept (R5's finding 13).
        if (ReplayInCommit([.. existing, .. events]).Current is not { } state || state.Find(sniperId) is not EntityInstance { Status: InstanceStatus.Active } sniper
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
        void Record(string rollId, int dr, BoardLocation? at, string? unit, string result, IReadOnlyList<string>? owner) =>
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "sniper-attacked", new SniperAttacked(sniper.Id, trigger, rollId, dr, at, unit, result), package, owner));

        var answers = resumed?.Answers ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var (locationRoll, selectionRoll, stackRoll, moved) = (resumed?.LocationRoll, resumed?.SelectionRoll, resumed?.StackRoll, resumed?.Moved ?? false);
        var againRolls = new List<string>();
        var sniperRoll = resumed?.SniperRoll ?? Roll(1, "sniper");
        var dr = Values(sniperRoll)[0];
        if (!ScenarioA1Sniper.Effective(dr))
        {
            Record(sniperRoll, dr, null, null, ScenarioA1Sniper.NoEffect, null);
            return false;
        }

        // Ruling R71.12: the attack stops at the Sniper player's choice, public as the dr is; its resume keeps what the attack has recorded, and the attacks
        // still to resolve. Ruling R71.19: the reposition hexes are the Sniper side's alone (its own hidden units shape them, A12.3).
        bool Pause(string step, IReadOnlyList<string> options, BoardLocation? place, IReadOnlyList<string>? visibility = null)
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

            // R1's finding 4: the stop inside a concealed stack keeps its Random Selection roll and the new drs already drawn.
            if (stackRoll is not null)
            {
                rolls["stack"] = stackRoll;
            }

            if (againRolls.Count > 0)
            {
                rolls["again"] = new JsonArray([.. againRolls.Select(roll => (JsonNode)roll)]);
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
                ["rest"] = QueueJson(rest),
            };
            if (place is not null)
            {
                resume["place"] = place.ToString();
            }

            // R5's finding 1: the key names the trigger, so two attacks of one Sniper in a commit, or after an answer with no fire record between, never share
            // one (the projector refuses a key already answered).
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "choice-pending", new ChoicePending($"{ChoicePending.Sniper}:{sniper.Id}:{trigger}:{step}",
                ChoicePending.Sniper, sniperSide, options, JsonSerializer.SerializeToElement(resume)), null, visibility));
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
                // Ruling R71.19: the choice of hex is recorded for the Sniper side only; the other side reads the public answer to forfeit and reposition,
                // the description of the step on the page, and then the counter's new hex.
                if (to is null)
                {
                    return Pause(ScenarioA1Sniper.RepositionStep, [.. elsewhere.Select(at => at.ToString())], null, [sniperSide]);
                }

                Record(sniperRoll, dr, to, null, ScenarioA1Sniper.Forfeited, null);
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
            Record(sniperRoll, dr, null, null, ScenarioA1Sniper.NoTarget, null);
            return false;
        }

        // A14.21 (ruling R71.7): the target hex, or the closest hex holding an eligible target or the enemy Sniper counter; ties go to the lowest TEM with the
        // Location's SMOKE (Rules orders them), and the equidistant hexes still tied are the Sniper player's choice (ruling R71.12). R1's finding 5: SMOKE is
        // counted in its own Location ("in-hex TEM/SMOKE DRM applicable to any eligible target"), not in every Location of its hex.
        var smoke = SmokeSources(state).GroupBy(place => place).ToDictionary(group => group.Key, group => group.Count());
        int Tem(BoardLocation location) => ScenarioA1Sniper.TargetTem(ReadLocation(state, location) is { } read ? TerrainKey(read) : null, smoke.GetValueOrDefault(location));
        var hexes = eligible.GroupBy(target => (target.At.Board, target.At.Hex))
            .Select((group, position) => (At: new BoardLocation(group.Key.Board, group.Key.Hex, 0), Facts: new SniperTargetHex(position, Distance(state, board, hex, group.Key.Board, group.Key.Hex),
                group.Min(target => Tem(target.At)), group.Key.Hex.ToString()))).ToArray();
        SniperTargetHex[] hexFacts = [.. hexes.Select(item => item.Facts)];
        BoardLocation[] equidistant = [.. ScenarioA1Sniper.TargetHexes(hexFacts).Select(position => hexes[position].At)];
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
        // is alone there, and joins the Location chosen otherwise. A14.21 (R1's finding 5): where the TEM compared equidistant hexes, "the Location with the
        // lowest TEM is the target", so only the hex's Locations of that TEM remain.
        SniperTarget[] inChosen = [.. eligible.Where(target => target.At.Board == chosen.Board && target.At.Hex == chosen.Hex)];
        SniperTarget[] snipers = [.. inChosen.Where(target => target.Role == ScenarioA1Sniper.SniperTarget)];
        SniperTarget[] placed = [.. inChosen.Where(target => target.Role != ScenarioA1Sniper.SniperTarget)];
        BoardLocation[] occupied = [.. (placed.Length > 0 ? placed : snipers).Select(target => target.At).Distinct().OrderBy(at => at.Level)];
        BoardLocation[] locations = [.. ScenarioA1Sniper.TargetLocations([.. occupied.Select(Tem)], ScenarioA1Sniper.TemDecidesLocation(hexFacts)).Select(position => occupied[position])];
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
        bool[] concealedOrDummy = [.. here.Select(target => target.Stacked)];
        var candidates = ScenarioA1Sniper.Candidates(concealedOrDummy).Select(group => (IReadOnlyList<SniperTarget>)[.. group.Select(position => here[position])]).ToList();
        static bool Concealed(IReadOnlyList<SniperTarget> stack) => stack.Count > 1 || stack[0].Stacked;
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
            // A stop inside a concealed stack below keeps these drs, and its resume reads them again (R1's finding 4).
            targets.Add((tied[0], sniperRoll, dr));
            foreach (var (other, position) in tied.Skip(1).Select((item, order) => (item, order)))
            {
                var again = resumed is { } earlier && position < earlier.AgainRolls.Count ? earlier.AgainRolls[position] : Roll(1, "sniper");
                againRolls.Add(again);
                targets.Add((other, again, Values(again)[0]));
            }
        }

        // A14.23 (p. 82): a concealed stack chosen with an effective dr: "the sniper player's opponent must declare the number (only) of eligible possible
        // targets that stack contains. If it contains none (i.e., is a Dummy stack), it is automatically eliminated. If it contains one, that unit is selected.
        // If it contains two or more, the player rolls for Random Selection accordingly": a tie there is the Sniper player's choice, unseen, and each other tied
        // target takes a new dr (A14.2; R1's finding 4). Decided before any effect is written, so a stop here leaves nothing written twice. A null target is
        // the stack's own result: a Dummy stack, or (ruling R71.20) a stack of real units with no eligible target, such as a concealed BU AFV.
        var attacks = new List<(IReadOnlyList<SniperTarget> Stack, SniperTarget? Target, string Roll, int Dr)>();
        foreach (var (stack, rollId, value) in targets)
        {
            if (!ScenarioA1Sniper.Effective(value) || !Concealed(stack))
            {
                attacks.Add((stack, stack[0], rollId, value));
                continue;
            }

            var (inStack, _) = ScenarioA1Sniper.StackTargets([.. stack.Select(item => (item.Role, item.Item is UnitInstance { Kind: UnitKinds.Dummy }))]);
            if (inStack.Count < 2)
            {
                attacks.Add((stack, inStack.Count == 1 ? stack[inStack[0]] : null, rollId, value));
                continue;
            }

            stackRoll ??= Roll(inStack.Count, "sniper-random-selection");
            var tiedInStack = ScenarioA1Sniper.Tied(Values(stackRoll)).Select(position => stack[inStack[position]]).ToList();
            if (ScenarioA1Sniper.ChoiceOffered(tiedInStack.Count))
            {
                if (!answers.TryGetValue(ScenarioA1Sniper.StackFirstStep, out var picked))
                {
                    return Pause(ScenarioA1Sniper.StackFirstStep, [.. tiedInStack.Select((_, position) => ScenarioA1Sniper.StackMemberOption(position))], location);
                }

                var head = tiedInStack[ScenarioA1Sniper.StackMemberOf(picked) ?? 0];
                tiedInStack.Remove(head);
                tiedInStack.Insert(0, head);
            }

            attacks.Add((stack, tiedInStack[0], rollId, value));
            foreach (var other in tiedInStack.Skip(1))
            {
                var again = Roll(1, "sniper");
                attacks.Add((stack, other, again, Values(again)[0]));
            }
        }

        // A14.3 (ruling R71.26): the SMC Temporary crew of an unarmored vehicle or Partially Armored AFV, attacked as the SMC he is; null for any other crew.
        UnitInstance? SmcCrew(SniperTarget target) => target.Item is UnitInstance vehicle && ScenarioA1Sniper.CrewAttackedAsSmc(
            vehicle.TemporaryCrew?.Manning == ScenarioA1CapturedVehicleRules.Smc, VehicleDefinition(vehicle) is not ({ Unarmored: true } or { PartiallyArmored: true }))
            ? index.CrewOf(vehicle.Id) : null;

        // A12.14 (rulings R71.8, R71.21): whether an unconcealed Good Order enemy ground unit has a LOS to a target's Location, read only when a concealed one
        // keeps its "?" by it; where every enemy seer is itself concealed, the "?" stays (A12.14's momentary forfeiture is not offered here: a deviation).
        bool Seen(SniperTarget target) => target.Item.Side is { } side && NearestUnconcealedLossSeer(state, side, target.At) is not null;
        // A12.2 (ruling R71.20): what else shares a target's "?", still concealed or hidden, which loses it with the target.
        IReadOnlyList<string> Sharing(string id) => [.. ScenarioA1ConcealmentChoices.SharesConcealment(id, linked => ConcealmentLinks(state, index, linked))
            .Where(other => other != id && state.Find(other) is { Status: InstanceStatus.Active } item && (Is(item, Conditions.Concealed) || Is(item, Conditions.Hidden)))];
        var lostLeaders = new List<(string Id, bool Eliminated)>();
        var replaced = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (stack, selected, rollId, value) in attacks)
        {
            if (!ScenarioA1Sniper.Effective(value))
            {
                Record(rollId, value, location, null, ScenarioA1Sniper.NoEffect, null);
                continue;
            }

            if (selected is not { } target)
            {
                // A14.23: a Dummy stack is eliminated. Ruling R71.20: a stack of real units with no eligible target is attacked to no effect and keeps its "?",
                // in public as every kept "?" is (ruling R71.22).
                if (ScenarioA1Sniper.StackTargets([.. stack.Select(item => (item.Role, item.Item is UnitInstance { Kind: UnitKinds.Dummy }))]).DummyStack)
                {
                    Record(rollId, value, location, null, ScenarioA1Sniper.DummyStackEliminated, null);
                    foreach (var dummy in stack)
                    {
                        events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(dummy.Item.Id), package, null));
                    }

                    continue;
                }

                var holder = stack.First(item => item.Role == ScenarioA1Sniper.ConcealedVehicle).Item;
                Record(rollId, value, location, null, ScenarioA1Sniper.ConcealedKept, null);
                Record(rollId, value, location, holder.Id, ScenarioA1Sniper.NoEffect, [attacked]);
                continue;
            }

            switch (target.Role)
            {
                case ScenarioA1Sniper.CrewTarget when SmcCrew(target) is { } smcCrew:
                    var crewResult = SnipeUnit(scope, attemptId, expected, events, smcCrew, target.Stacked, state.Phase, rollId, value, location, package, Roll, Values, Record,
                        () => Seen(target), Sharing);
                    if (crewResult == ScenarioA1Sniper.Eliminated)
                    {
                        // D5.311 (pass 65, rulings R65.26, R65.31): the vehicle he crewed is then unmanned, so Abandoned, as an eliminated Unprotected crew leaves it;
                        // a leader so lost causes the LLMC of his Location (A10.2).
                        replaced[smcCrew.Id] = null;
                        events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                            new ConditionsChanged(target.Item.Id, ConditionChanges(ScenarioA1OrdnanceEventRules.AbandonedConditions)), package, null));
                        if (smcCrew.Kind == "asl:leader")
                        {
                            lostLeaders.Add((smcCrew.Id, true));
                        }
                    }

                    break;
                case ScenarioA1Sniper.CrewTarget or ScenarioA1Sniper.VehicleTarget:
                    SnipeVehicle(scope, attemptId, expected, actor, events, draw, state, target, rollId, value, location, package, Record, () => Seen(target), Sharing);
                    break;
                case ScenarioA1Sniper.SniperTarget:
                    SnipeSniper(scope, attemptId, expected, events, state, (EntityInstance)target.Item, rollId, value, location, package, Record);
                    break;
                default:
                    var unit = (UnitInstance)target.Item;
                    var result = SnipeUnit(scope, attemptId, expected, events, unit, target.Stacked, state.Phase, rollId, value, location, package, Roll, Values, Record,
                        () => Seen(target), Sharing);
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
        // Ruling R71.25 (A14.1, A14.3): the Sniper attacks the LLMC DRs call are read by the one trigger reader from the check's own events, and resolve before
        // this attack's Sniper Check and any later trigger.
        GameState? after = null;
        (string Roll, string Sniper)[] called = [];
        if (lostLeaders.Count > 0 && ReplayInCommit([.. existing, .. events]).Current is { } sniped)
        {
            var checks = events.Count;
            AddLeaderLossChecks(scope, attemptId, expected, actor, events, draw, state, sniped, lostLeaders, ScenarioA1MoraleCheck.Sniper, null);
            after = ReplayInCommit([.. existing, .. events]).Current;
            var walk = new SniperTriggerWalk(sniped, MemoOf(sniped).Snipers(sniped));
            ReadTriggers(sniped, existing, events, checks, walk);
            called = walk.Unresolved();
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

        // A14.4 (pass 71, ruling R71.10): the attack resolved, it owes a Sniper Check when the target Location held a unit of the attacked side before it.
        // Ruling R71.25: the attacks its own LLMC DRs called come first, then the check, then what was still to resolve.
        var owed = called.Select(item => new SniperQueued(item.Roll, item.Sniper)).ToList();
        if (index.At(location).OfType<UnitInstance>().Any(unit => unit.Side == attacked))
        {
            owed.Add(new SniperQueued(trigger, sniper.Id, location));
        }

        rest.InsertRange(0, owed);
        return false;
    }

    /// <summary>
    /// The Sniper Check an effective Sniper attack owes (A14.4, p. 83; pass 71, rulings R71.10, R71.18, R71.25), offered once the attacks its own LLMC DRs called
    /// are resolved, from the state of that moment: when a unit of the attacked side in the target Location may take it, and whenever the Location still
    /// holds a unit of that side under "?", so the public offer tells nothing of the stack (A14.23, A12.141). True when it stops at the attacked side's
    /// choice, whose resume keeps <paramref name="rest"/>.
    /// </summary>
    private bool OfferSniperCheck(GameScope scope, string attemptId, long expected, IReadOnlyList<GameEvent> existing, List<GameEvent> events, SniperQueued owed,
        BoardLocation location, List<SniperQueued> rest)
    {
        if (events.Any(item => item.Payload is ChoicePending) || ReplayInCommit([.. existing, .. events]).Current is not { } state
            || state.Find(owed.Sniper) is not EntityInstance { Status: InstanceStatus.Active, Side: { } sniperSide } sniper
            || state.Sides.FirstOrDefault(item => item.Id != sniperSide)?.Id is not { } attacked
            || !ScenarioA1Sniper.SniperCheckOffered(SniperCheckers(state, location, attacked).Count, MemoOf(state).Objects(state).At(location).OfType<UnitInstance>()
                .Any(unit => unit.Status == InstanceStatus.Active && unit.Side == attacked && !Is(unit, Conditions.Hidden) && (Is(unit, Conditions.Concealed) || unit.Kind == UnitKinds.Dummy))))
        {
            return false;
        }

        var resume = new JsonObject
        {
            ["record"] = ChoicePending.SniperCheck,
            ["sniper"] = sniper.Id,
            ["sniperSide"] = sniperSide,
            ["trigger"] = owed.Roll,
            ["location"] = location.ToString(),
            ["rest"] = QueueJson(rest),
        };

        // R5's finding 1: the key names the attack's trigger, so the checks of two attacks of one Sniper on one Location never share one.
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "choice-pending", new ChoicePending($"{ChoicePending.SniperCheck}:{sniper.Id}:{owed.Roll}",
            ChoicePending.SniperCheck, attacked, [ChoicePending.Take, ChoicePending.Decline], JsonSerializer.SerializeToElement(resume)), null, null));
        return true;
    }

    /// <summary>
    /// The units of the attacked side in a Sniper attack's target Location that may take the Sniper Check (A14.4; pass 71, ruling R71.10), read from the
    /// state's index, in id order: Personnel on foot, in Good Order, unpinned, not TI, armed, not hidden, and neither fired nor moved this Player Turn, which
    /// Rules decides from the unit's fire markers, its MF, this phase's firers, the Gun crews that fired, and the state's list of the units that moved or fired
    /// in an earlier phase (R5's finding 6).
    /// </summary>
    private List<UnitInstance> SniperCheckers(GameState state, BoardLocation location, string attacked)
    {
        var index = MemoOf(state).Objects(state);
        SniperCheckActivity Activity(UnitInstance unit) => new(Is(unit, Conditions.PrepFire), Is(unit, Conditions.FirstFire), Is(unit, Conditions.FinalFire),
            Is(unit, Conditions.BoundingFire), unit.MfSpent, unit.HalfMfSpent, state.PhaseFirers.Any(item => item.Unit == unit.Id),
            state.GunCrewsFired.Contains(unit.Id, StringComparer.Ordinal), state.FiredOrMovedThisPlayerTurn.Contains(unit.Id, StringComparer.Ordinal));
        return [.. index.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == attacked
                && ScenarioA1Sniper.MayTakeSniperCheck(new SniperCheckCandidate(vocabulary.IsA(unit.Kind, "asl:personnel") && unit.Kind != UnitKinds.Dummy && index.VehicleOf(unit.Id) is null,
                    GameState.GoodOrder(unit, vocabulary) != ConditionState.False, Is(unit, Conditions.Pinned), Is(unit, "asl:ti"), Is(unit, Conditions.Unarmed), Is(unit, Conditions.Hidden),
                    Activity(unit))))
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
        var stillToResolve = QueueOf(resume).Count;
        var answered = state with
        {
            Choice = null
        };
        var sniper = answered.Find(sniperId) as EntityInstance;
        List<UnitInstance> units = option == ChoicePending.Take && sniper is { Status: InstanceStatus.Active } ? SniperCheckers(answered, at, pending.Side) : [];

        // Ruling R71.18: a check taken where no unit may take it commits nothing of its own, and says so in the answering side's proposal only.
        string[] reasons = option == ChoicePending.Take && units.Count == 0 ? [summary, ScenarioA1Sniper.NoSniperCheckerText($"[{at.Hex}]")] : [summary];
        if (units.Count == 0 && stillToResolve == 0)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [made], reasons);
        }

        var (drm, causes) = ScenarioA1Sniper.SniperCheckDrm([.. units.Select(unit => new SniperCheckParticipant(unit.Kind,
            DefinitionOf(unit) is { IsLeader: true, Leadership: { } leadership } ? leadership : null, Is(unit, Conditions.Heroic))
        {
            Wounded = Is(unit, Conditions.Wounded)
        })]);
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent> { made };
            if (units.Count > 0 && sniper is not null)
            {
                var package = ScenarioA1FirePackage.Identity.ToString();

                // A12.141: the participants lose their "?" in the LOS of a Good Order enemy ground unit within 16 hexes, an unconcealed one (ruling R71.21);
                // those that keep it are not named to the Sniper's side, nor is the record that names them, nor its DR (ruling R71.22).
                var concealed = units.Any(unit => Is(unit, Conditions.Concealed));
                var exposed = concealed && ScenarioA1Sight.SeenWithin(NearestUnconcealedLossSeer(answered, pending.Side, at), 16);
                IReadOnlyList<string>? owner = concealed && !exposed ? [pending.Side] : null;
                var rollId = $"{attemptId}-roll-1";
                var dice = draw(new RollRequest(2, 6));
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled", new DiceRolled(rollId, "sniper-check", 2, 6, dice.Values, DiceRolled.SystemSource, actor),
                    package, owner));
                var result = ScenarioA1Sniper.SniperCheckResult(dice.Values.Sum() + drm, Is(sniper, Conditions.Pinned));
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

            // Each build reads the queue afresh, as a plan may be built more than once.
            SnipeInOrder(scope, attemptId, expected, actor, existing, events, draw, QueueOf(resume));
            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [.. reasons, units.Count == 0 ? "play.sniper-check: the Sniper attacks still to resolve follow (A14.1)"
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
            resume.GetProperty("moved").GetBoolean(), answers)
        {
            StackRoll = RollOf("stack"),
            AgainRolls = rolls.TryGetProperty("again", out var again) ? [.. again.EnumerateArray().Select(roll => roll.GetString()!)] : [],
        };
        var sniperId = resume.GetProperty("sniper").GetString()!;
        var trigger = resume.GetProperty("trigger").GetString()!;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            // Each build reads the queue afresh, as a plan may be built more than once; the attack resumed puts what it owes at its head (ruling R71.25).
            var events = new List<GameEvent> { made };
            var rest = QueueOf(resume);
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
                found.Add(new SniperTarget(unit, ScenarioA1Sniper.UnitTarget, at, ScenarioA1Sniper.InConcealedStack(Is(unit, Conditions.Concealed) || unit.Kind == UnitKinds.Dummy, false)));
                continue;
            }

            // A14.22 (ruling R71.3): the PRC's exposure as a Collateral Attack reads it (D.8, D6.61, D6.62); a Stunned, Recalled, or Shocked AFV's Passengers spared.
            var definition = VehicleDefinition(unit);
            var crewExposed = LiveFire.CrewExposed(unit);
            var impaired = ScenarioA1Sniper.CrewImpaired(Is(unit, Conditions.Stunned), Is(unit, Conditions.Recalled), Is(unit, Conditions.Shocked));
            UnitInstance[] aboard = [.. index.Aboard(unit.Id).Where(prc => !Is(prc, Conditions.Hidden) && !Is(prc, Conditions.Captured))];
            string[] exposures = definition is null ? [] : [.. aboard.Select(prc => ScenarioA1PrcRules.Exposure(new FirePrc(prc.Id, prc.Definition?.Definition,
                prc.Position is ContainedPosition { Role: ContainmentRole.Rider } ? FirePrc.Rider : FirePrc.Passenger, Is(prc, Conditions.Broken) ? true : null,
                Is(prc, Conditions.Pinned) ? true : null), definition, crewExposed, impaired))];
            var manned = ScenarioA1Sniper.Manned(Is(unit, Conditions.Abandoned), index.CrewOf(unit.Id) is { } crew && Is(crew, Conditions.Broken));
            var targets = ScenarioA1Sniper.VehicleTargets(new SniperVehicleFacts(definition?.Unarmored == true, definition?.PartiallyArmored == true, crewExposed, manned, exposures));

            // A12.2, A14.23 (ruling R71.20): the PRC, crew, and vehicle of a concealed vehicle are its concealed stack, and the vehicle is one "?" candidate
            // whatever its state.
            var concealed = Is(unit, Conditions.Concealed);
            found.AddRange(targets.Prc.Select(position => new SniperTarget(aboard[position], ScenarioA1Sniper.UnitTarget, at,
                ScenarioA1Sniper.InConcealedStack(Is(aboard[position], Conditions.Concealed), concealed))));
            var ofVehicle = ScenarioA1Sniper.InConcealedStack(false, concealed);
            if (targets.Crew)
            {
                found.Add(new SniperTarget(unit, ScenarioA1Sniper.CrewTarget, at, ofVehicle));
            }

            if (targets.Vehicle)
            {
                found.Add(new SniperTarget(unit, ScenarioA1Sniper.VehicleTarget, at, ofVehicle));
            }

            if (ScenarioA1Sniper.CountsAsConcealedVehicle(concealed, targets))
            {
                found.Add(new SniperTarget(unit, ScenarioA1Sniper.ConcealedVehicle, at, true));
            }
        }

        // A14.21, A14.31 (ruling R71.6): the enemy Sniper counter is a target too.
        foreach (var entity in state.Entities)
        {
            if (entity.Kind == "asl:sniper" && entity.Side == attacked && entity.Status == InstanceStatus.Active && entity.Position is MapPosition position)
            {
                found.Add(new SniperTarget(entity, ScenarioA1Sniper.SniperTarget, position.Location, false));
            }
        }

        return found;
    }

    /// <summary>
    /// A14.3 (ruling R15.5): dr 1 eliminates a SMC and breaks a MMC, Casualty Reducing one that cannot break (broken or berserk); dr 2 wounds a SMC (its Wound
    /// Severity dr, A17.11) and pins a MMC not immune to Pin results. A concealed unit keeps its "?" out of every Good Order enemy LOS unless it breaks, is
    /// wounded, or is Reduced (A12.14; pass 71, ruling R71.8), and the record then names no unit. Rules decides the effect (pass 32.h). A SMC wounded in the MPh
    /// after spending more than 3 MF is pinned too (A17.2; ruling R71.14), unless he is broken (A7.8; ruling R71.27). Since ruling R71.26 a SMC Temporary crew
    /// of a vehicle not fully armored comes here too. The result recorded. Since the hidden-information fixes (rulings R71.20 to
    /// R71.22): <paramref name="concealed"/> is the target's place in the concealed stack (a PRC of a concealed vehicle too), a "?" lost is lost by what
    /// shares it (<paramref name="sharing"/>), and a target that keeps it has a neutral public record, its real one and its effects for its owner alone.
    /// </summary>
    private string SnipeUnit(GameScope scope, string attemptId, long expected, List<GameEvent> events, UnitInstance unit, bool concealed, string? phase, string rollId, int dr,
        BoardLocation location, string package, Func<int, string, string> roll, Func<string, IReadOnlyList<int>> values,
        Action<string, int, BoardLocation?, string?, string, IReadOnlyList<string>?> record, Func<bool> seen, Func<string, IReadOnlyList<string>> sharing)
    {
        IReadOnlyList<string>? owner = null;
        void Change(IReadOnlyList<(UnitCondition Condition, bool Value)> conditions) =>
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.Id, ConditionChanges(conditions)), package, owner));
        void Eliminate() => events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(unit.Id), package, null));

        var smc = vocabulary.IsA(unit.Kind, "asl:smc");
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
        var keeps = ScenarioA1Sniper.KeepsConcealment(concealed, result, seen);
        RecordAttack(record, rollId, dr, location, unit.Id, unit.Side, result, keeps);
        owner = keeps ? [unit.Side] : null;
        switch (effect)
        {
            case SniperEffect.Eliminated:
                Eliminate();
                break;
            case SniperEffect.WoundSeverity when mortal:
                Eliminate();
                break;
            case SniperEffect.WoundSeverity:
                Change(ScenarioA1Sniper.WoundedConditions(ScenarioA1Wounds.PinnedByWound(smc, Is(unit, Conditions.Broken), phase, unit.MfSpent, unit.HalfMfSpent)));
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

        // A12.2 (ruling R71.20): "... so does its manning Infantry or PRC, and vice versa": what shares the lost "?" loses it too.
        if (concealed && !keeps)
        {
            AddUnconcealed(scope, attemptId, expected, events, package, sharing(unit.Id));
        }

        return result;
    }

    /// <summary>
    /// The record of a Sniper attack on a target (A14.3): public, or, for a target that keeps its "?", the one neutral public result and its owner's twin with
    /// the unit and the real result (A12.14, A14.23; ruling R71.22).
    /// </summary>
    private static void RecordAttack(Action<string, int, BoardLocation?, string?, string, IReadOnlyList<string>?> record, string rollId, int dr, BoardLocation location,
        string unit, string side, string result, bool keeps)
    {
        record(rollId, dr, location, keeps ? null : unit, ScenarioA1Sniper.PublicResult(keeps, result), null);
        if (keeps)
        {
            record(rollId, dr, location, unit, result, [side]);
        }
    }

    /// <summary>The "?" and hidden status lost by each of <paramref name="ids"/>, in public (A12.2, A12.14).</summary>
    private void AddUnconcealed(GameScope scope, string attemptId, long expected, List<GameEvent> events, string package, IReadOnlyList<string> ids)
    {
        foreach (var id in ids)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(id, Unconcealed()), package, null));
        }
    }

    /// <summary>
    /// A14.3, A14.33, D5.34, D5.341, D5.342 (ruling R71.5): a Sniper attack on a vehicle's Inherent crew or on an unarmored vehicle itself. A Stun, a Recall, an
    /// Immobilization, or a crew's pin is the vehicle's <c>conditions-changed</c>, as the Fire path writes one, so the projector's Stun, Recall, BU, and SMC
    /// crew handling apply; a broken Inherent crew is placed broken inside and routs beneath it, as an Unprotected crew is (D5.311, whose "[EXC: Sniper attack]"
    /// leaves the break to A14.3). A concealed vehicle keeps its "?" as a unit does (A12.14; ruling R71.8). A SMC Temporary crew's Wound Severity dr comes before
    /// a Stun (D5.343, A17.11; ruling R71.15). A vehicle that keeps its "?" has a neutral public record, and its real one, its conditions, and a crew's
    /// Severity dr are its owner's (ruling R71.22); one that loses it loses it with its PRC and crew (A12.2; ruling R71.20).
    /// </summary>
    private void SnipeVehicle(GameScope scope, string attemptId, long expected, string actor, List<GameEvent> events, Func<RollRequest, RollResult> draw, GameState state,
        SniperTarget target, string rollId, int dr, BoardLocation location, string package, Action<string, int, BoardLocation?, string?, string, IReadOnlyList<string>?> record,
        Func<bool> seen, Func<string, IReadOnlyList<string>> sharing)
    {
        var vehicle = (UnitInstance)target.Item;
        var definition = VehicleDefinition(vehicle);
        var result = target.Role == ScenarioA1Sniper.CrewTarget
            ? ScenarioA1Sniper.CrewResult(dr, definition is not ({ Unarmored: true } or { PartiallyArmored: true }), Is(vehicle, Conditions.StunRecovery), Is(vehicle, Conditions.Pinned))
            : ScenarioA1Sniper.VehicleResult(dr, Is(vehicle, Conditions.Immobilized));
        var concealed = target.Stacked;
        var keeps = ScenarioA1Sniper.KeepsConcealment(concealed, result, seen);
        RecordAttack(record, rollId, dr, location, vehicle.Id, vehicle.Side, result, keeps);
        IReadOnlyList<string>? owner = keeps ? [vehicle.Side] : null;
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
            var change = new ConditionsChanged(vehicle.Id, changed);
            AddSmcCrewSeverity(scope, attemptId, expected, actor, package, events, draw, state, change, owner);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", change, package, owner));
        }

        if (concealed && !keeps)
        {
            AddUnconcealed(scope, attemptId, expected, events, package, sharing(vehicle.Id));
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
        BoardLocation location, string package, Action<string, int, BoardLocation?, string?, string, IReadOnlyList<string>?> record)
    {
        var result = ScenarioA1Sniper.SniperResult(dr, Is(enemy, Conditions.Pinned));
        record(rollId, dr, location, enemy.Id, result, null);
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
