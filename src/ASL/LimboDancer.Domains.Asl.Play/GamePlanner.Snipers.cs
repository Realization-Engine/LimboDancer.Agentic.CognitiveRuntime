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
/// A14.3); a Random Location DR from the Sniper counter's hex finds the target hex, or the closest hex holding an eligible target or the enemy Sniper counter,
/// ties to the lowest TEM and SMOKE and then the first hex by name (A14.2, A14.21); the Location there with the most targets is attacked, and Random Selection
/// picks among them, a concealed stack counting as one (A14.23). Since pass 71 (rulings R71.3 to R71.8) the targets are the units on foot, the Vulnerable PRC,
/// Inherent crews, unarmored vehicles, and the enemy Sniper counter (A14.22, A14.31), each with its own results (A14.3, A14.33, D5.34, D5.341). The Sniper
/// player never forfeits an attack, so never repositions (backlog). The rules are <see cref="ScenarioA1Sniper"/>'s (pass 32.h); this file reads the records,
/// the state, and the map, draws the dice, and writes the events.
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

    /// <summary>
    /// A Sniper target (A14.2 EXC, A14.22, A14.31; pass 71, ruling R71.3): a unit (on foot or a Vulnerable PRC), a vehicle's Inherent crew or the unarmored
    /// vehicle itself (the vehicle as the item, by <see cref="Role"/>), or the enemy Sniper counter, with the Location it is attacked in.
    /// </summary>
    private readonly record struct SniperTarget(IGameObject Item, string Role, BoardLocation At)
    {
        /// <summary>A14.23: a concealed unit or a Dummy, which Random Selection counts as one stack; a crew or vehicle of a concealed vehicle too.</summary>
        public bool ConcealedOrDummy => Item is UnitInstance unit && (Is(unit, Conditions.Concealed) || unit.Kind == UnitKinds.Dummy);
    }

    private void Snipe(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, List<GameEvent> events,
        Func<RollRequest, RollResult> draw, string sniperId, string trigger)
    {
        // Pass 71 (design section 5, item 4): the state after the commit's events so far is taken once, and every Location is read from its index.
        if (Replay([.. existing, .. events]).Current is not { } state || state.Find(sniperId) is not EntityInstance { Status: InstanceStatus.Active } sniper
            || sniper.Side is not { } sniperSide || state.Sides.FirstOrDefault(item => item.Id != sniperSide)?.Id is not { } attacked)
        {
            return;
        }

        var index = MemoOf(state).Objects(state);
        if (index.Location(sniper.Id) is not { } sniperAt)
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

        // A14.22, A14.31 (rulings R71.3, R71.4, R71.6): the attacked side's eligible targets and its Sniper counter.
        var eligible = SniperTargets(state, index, attacked);
        if (eligible.Count == 0)
        {
            Record(sniperRoll, dr, null, null, ScenarioA1Sniper.NoTarget);
            return;
        }

        // A14.21 (ruling R71.7): the target hex, or the closest hex holding an eligible target or the enemy Sniper counter; ties go to the lowest TEM with the
        // hex's SMOKE, then the first hex by name (Rules orders them).
        var smoke = SmokeSources(state).GroupBy(place => (place.Board, place.Hex)).ToDictionary(group => group.Key, group => group.Count());
        int Tem(BoardLocation location) => ScenarioA1Sniper.TargetTem(ReadLocation(state, location) is { } read ? TerrainKey(read) : null,
            smoke.GetValueOrDefault((location.Board, location.Hex)));
        var hexes = eligible.GroupBy(target => (target.At.Board, target.At.Hex))
            .Select((group, position) => (group.Key.Board, group.Key.Hex, Facts: new SniperTargetHex(position, Distance(state, board, hex, group.Key.Board, group.Key.Hex),
                group.Min(target => Tem(target.At)), group.Key.Hex.ToString()))).ToArray();
        var chosen = hexes[ScenarioA1Sniper.TargetHex([.. hexes.Select(item => item.Facts)])];

        // Ruling R15.5: of several Locations in the hex, the one holding the most eligible targets, then the lowest; the enemy Sniper counter is part of every
        // Location of its hex (A14.31), so it is in each one's count, and its own Location is attacked when it is alone there.
        SniperTarget[] inChosen = [.. eligible.Where(target => target.At.Board == chosen.Board && target.At.Hex == chosen.Hex)];
        SniperTarget[] snipers = [.. inChosen.Where(target => target.Role == ScenarioA1Sniper.SniperTarget)];
        SniperTarget[] placed = [.. inChosen.Where(target => target.Role != ScenarioA1Sniper.SniperTarget)];
        var inHex = (placed.Length > 0 ? placed : snipers).GroupBy(target => target.At)
            .Select((group, position) => (group.Key, Facts: new SniperTargetLocation(position, group.Count() + (placed.Length > 0 ? snipers.Length : 0), group.Key.Level))).ToArray();
        var location = inHex[ScenarioA1Sniper.TargetLocation([.. inHex.Select(item => item.Facts)])].Key;
        SniperTarget[] here = [.. placed.Where(target => target.At == location).Concat(snipers).OrderBy(target => target.Item.Id, StringComparer.Ordinal)
            .ThenBy(target => target.Role, StringComparer.Ordinal)];

        // Ruling R15.5: the Sniper counter moves to the Location attacked.
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-moved", new InstanceMoved(sniper.Id, new MapPosition(location)), package, null));

        // A14.23: a concealed stack is one possible target; the others one each (pass 71: a Vulnerable Inherent Crew, an unarmored vehicle, and the enemy
        // Sniper counter simply join Random Selection; the Sniper player's choice of them is increment 4d's).
        bool[] concealedOrDummy = [.. here.Select(target => target.ConcealedOrDummy)];
        var candidates = ScenarioA1Sniper.Candidates(concealedOrDummy).Select(group => (IReadOnlyList<SniperTarget>)[.. group.Select(position => here[position])]).ToList();
        var targets = new List<(IReadOnlyList<SniperTarget> Stack, string Roll, int Dr)>();
        if (candidates.Count == 1)
        {
            targets.Add((candidates[0], sniperRoll, dr));
        }
        else
        {
            var selection = Roll(candidates.Count, "sniper-random-selection");
            var tied = ScenarioA1Sniper.Tied(Values(selection)).Select(position => candidates[position]).ToArray();

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
                    SnipeSniper(scope, attemptId, expected, events, (EntityInstance)target.Item, rollId, value, location, package, Record);
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
    /// A14.3, A14.31 (ruling R71.6): a Sniper attack on the enemy Sniper counter: dr 1 eliminates it, dr 2 pins it until the CCPh's end (the projector clears
    /// the pin with the others).
    /// </summary>
    private void SnipeSniper(GameScope scope, string attemptId, long expected, List<GameEvent> events, EntityInstance enemy, string rollId, int dr,
        BoardLocation location, string package, Action<string, int, BoardLocation?, string?, string> record)
    {
        var result = ScenarioA1Sniper.SniperResult(dr, Is(enemy, Conditions.Pinned));
        record(rollId, dr, location, enemy.Id, result);
        if (result == ScenarioA1Sniper.SniperEliminated)
        {
            // Pass 71, increment 4b: the SAN reduction of ruling R71.9 joins the elimination here.
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(enemy.Id), package, null));
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
