using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The fire extensions of backlog pass 12 (rulings R12.1 to R12.11): Opportunity Fire, fire at a blocked LOS, Spraying Fire, Fire Lanes,
/// Encirclement, and the concealment a unit gains at the end of its Player Turn.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// Opportunity Fire (A7.25; ruling R12.1): in their PFPh, Good Order Infantry of the phasing side that have not fired or directed fire this
    /// Player Turn are marked with a Bounding Fire counter; one in the LOS of a Good Order enemy ground unit within 16 hexes loses its "?" (Case D).
    /// Rules decides each check (pass 32.c).
    /// </summary>
    private GamePlan PlanOpportunityFire(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (ScenarioA1FireEligibility.OpportunityFirePhaseBar(state.Phase) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        string[] ids = [.. Strings(arguments, "unitIds")];
        if (ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: Opportunity Fire names its units once each");
        }

        foreach (var id in ids)
        {
            var unit = state.Unit(id);
            if (ScenarioA1FireEligibility.OpportunityFirerBar(new OpportunityFirerFacts(id, unit is { Status: InstanceStatus.Active } && state.Location(id) is not null,
                unit?.Side == state.PhasingSide, unit is not null && LiveFire.IsVehicle(unit), unit is not null && vocabulary.IsA(unit.Kind, "asl:personnel"),
                unit?.Kind == UnitKinds.Dummy, unit is not null && Is(unit, Conditions.Broken), unit is not null && Is(unit, Conditions.Berserk),
                unit is not null && Is(unit, Conditions.Melee), unit is not null && Is(unit, Conditions.Captured), unit is not null && Is(unit, "asl:ti"),
                unit is not null && LiveFire.Fired(unit), unit is not null && Is(unit, Conditions.BoundingFire), state.PhaseFirers.Any(item => item.Unit == id),
                state.SupportWeaponUses.Any(item => item.Unit == id), state.SupportWeaponDirectors.Any(item => item.Leader == id))) is { } unitBar)
            {
                return Refused(scope, label, expected, unitBar);
            }
        }

        var events = new List<GameEvent> { Event(scope, attemptId, 1, expected, "opportunity-fire-declared", new OpportunityFireDeclared(ids), null, null) };
        var revealed = ScenarioA1FireEligibility.OpportunityFireReveals(ids.Select(id => state.Unit(id)!).Select(unit => (unit.Id,
            Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden), (Func<bool>)(() => SeenByEnemy(state, unit.Side, state.Location(unit.Id)!.Location, 16)))));
        foreach (var id in revealed)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "concealment-lost",
                new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False, [Conditions.Hidden] = ConditionState.False }),
                null, null));
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [ScenarioA1FireEligibility.OpportunityFireSummary(ids, revealed)]);
    }

    /// <summary>
    /// Whether LOS entries Encircle a hex (A7.7; ruling R12.11): two opposite hexspines, two opposite hexsides (exactly three vertices between them both
    /// ways), or three hexsides no two of which are adjacent.
    /// </summary>
    internal static bool Encircles(IReadOnlyCollection<int> positions) => ScenarioA1Geometry.Encircles(positions);

    /// <summary>An attack's share of an Encirclement (A7.7; ruling R12.11), as Rules decides it from each firer's LOS entry (pass 32.c).</summary>
    private (int Units, List<int> Entries) EncirclementShare(GameState state, FireAttack facts, FireArithmetic? arithmetic) =>
        ScenarioA1FireMapRules.EncirclementShare(facts, arithmetic, BoardLocation.TryParse(facts.TargetLocationId, out var target), FireReference.Value,
            EncirclementFirers(state, facts, target));

    /// <summary>Each firer of an attack as the Encirclement share reads it, in the attack's order: whether its Location is known, and its LOS entry sides, read when asked.</summary>
    private IReadOnlyList<EncirclementFirerFacts> EncirclementFirers(GameState state, FireAttack facts, BoardLocation? target) =>
        [.. (facts.Firers ?? []).Select(firer => BoardLocation.TryParse(firer.LocationId, out var from)
            ? new EncirclementFirerFacts(true, () => target is null ? null : LosEntrySides(state, target, from)?.Select(side => (int)side).ToArray())
            : new EncirclementFirerFacts(false, () => null))];

    /// <summary>
    /// The side this attack Encircles in its target Location, or null (A7.7; ruling R12.11), as Rules decides it from the phase's earlier attacks, read
    /// latest first as the scan reaches them (pass 32.c).
    /// </summary>
    private string? EncirclementSeal(GameState state, IReadOnlyList<GameEvent> existing, FireAttack facts, string firingSide)
    {
        var targetKnown = BoardLocation.TryParse(facts.TargetLocationId, out var target);
        return ScenarioA1FireMapRules.EncirclementSeal(state.Phase, facts, targetKnown, id => state.Unit(id)?.Side,
            side => state.Encirclements.Any(item => item.Location == target && item.Side == side), FireReference.Value, EncirclementFirers(state, facts, target),
            ThisPhase(existing).Select(item => item.Payload).Where(item => item is FireResolved or OrdnanceFired).Reverse().Select(payload => payload is OrdnanceFired shot
                ? new EarlierAttackFacts(true, state.Unit(shot.Crew)?.Side, shot.Target == target, () => null)
                : EarlierFire(state, (FireResolved)payload, facts, target)),
            firingSide);
    }

    /// <summary>A fire record of the phase as the Encirclement scan reads it: its first firer's side, whether it was at the target, and its facts when asked.</summary>
    private EarlierAttackFacts EarlierFire(GameState state, FireResolved record, FireAttack facts, BoardLocation? target) =>
        new(false, record.Firers.Count == 0 ? null : state.Unit(record.Firers[0])?.Side, record.TargetLocation == facts.TargetLocationId, () =>
        {
            var recorded = record.Facts.Deserialize<FireAttack>(LiveFire.Json);
            var blocked = record.Resolution.TryGetProperty("losBlocked", out var flag) && flag.ValueKind == JsonValueKind.True;
            var arithmetic = record.Resolution.TryGetProperty("arithmetic", out var value) && value.ValueKind == JsonValueKind.Object
                ? value.Deserialize<FireArithmetic>(LiveFire.Json) : null;
            return recorded is null ? null : new RecordedAttackFacts(recorded, blocked, arithmetic, EncirclementFirers(state, recorded, target));
        });

    /// <summary>
    /// The Locations of a Fire Lane (A9.22; ruling R12.7), as Rules searches them along the Hex Grain through a fact reader over the map (the pass 32
    /// design, D4; pass 32.c); null with the reason when the lane is not on a Hex Grain.
    /// </summary>
    private (IReadOnlyList<FireLaneEntry>? Entries, string? Reason) FireLaneEntries(GameState state, BoardLocation from, BoardLocation target, BoardLocation to,
        int normalRange, int firepower)
    {
        var map = new FireLaneMap(this, state);
        var (entries, reason) = ScenarioA1FireMapRules.FireLaneEntries(map.Index(from), from.Level, map.Index(target), map.Index(to), target.ToString(), to.ToString(),
            normalRange, firepower, map);
        return entries is null ? (null, reason) : ([.. entries.Select(entry => new FireLaneEntry(map.LocationOf(entry.Location), entry.Fp, entry.HindranceDrm))], null);
    }

    /// <summary>The map as the Fire Lane's search reads it: a table of Locations by index, the Location across a hexside, a hex's identity, a base level, and a LOS.</summary>
    private sealed class FireLaneMap(GamePlanner planner, GameState state) : IFireLaneFactReader
    {
        private readonly List<BoardLocation> locations = [];
        private readonly Dictionary<BoardLocation, int> indexes = [];

        /// <summary>The index of a Location in the table, added when it is new.</summary>
        public int Index(BoardLocation location)
        {
            if (!indexes.TryGetValue(location, out var index))
            {
                index = locations.Count;
                locations.Add(location);
                indexes[location] = index;
            }

            return index;
        }

        public BoardLocation LocationOf(int index) => locations[index];

        public int? Across(int location, int side) => planner.Across(state, locations[location], (HexsideDirection)side) is { } next ? Index(next) : null;

        public int HexOf(int location) => locations.FindIndex(item => item.Board == locations[location].Board && item.Hex == locations[location].Hex);

        public int? BaseLevel(int location) => planner.ReadLocation(state, locations[location])?.Hex.BaseLevel;

        public (bool Blocked, int Hindrance)? Los(int from, int target) =>
            planner.Los(state, locations[from], locations[target]) is { } los ? (los.IsBlocked != false, los.Hindrance) : null;
    }

    /// <summary>
    /// A Fire Lane's attack on the moving stack in one of its Locations (A9.22, A9.222; ruling R12.7): Residual FP, never reduced, with no CX, leader, or
    /// hero DRM and no Cowering, taking the Hindrance of the LOS from its MG; null when it cannot be read.
    /// </summary>
    private FireAttack? FireLaneFacts(GameState state, BoardLocation at, FireLaneEntry entry)
    {
        if (LiveFire.ResidualFromState(state, at, entry.Fp) is not ({ } attack, null) || FireMapFacts(state, attack, at) is not ({ } map, null))
        {
            return null;
        }

        return ScenarioA1FireFollowUps.FireLaneAttack(HeatOfBattleFacts(state, map), entry.HindranceDrm);
    }

    /// <summary>
    /// A Fire Lane's attack as the moving stack enters one of its Locations (A9.22, A9.223): once the other attacks there are made, on the stack still
    /// there, while the lane is in place; an Original DR at least its MG's Breakdown Number malfunctions the MG, which ends the lane. Rules decides both
    /// (pass 32.c).
    /// </summary>
    private void AddFireLaneAttack(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, List<GameEvent> events,
        FireLane lane, FireLaneEntry entry, BoardLocation at, int? step, Func<RollRequest, RollResult> draw)
    {
        if (events.Any(item => item.Payload is ChoicePending) || Replay([.. existing, .. events]).Current is not { } state
            || !ScenarioA1FireFollowUps.FireLaneAttacks(state.FireLanes.Any(item => item.Weapon == lane.Weapon), state.Movement is { } movement && movement.Location == at,
                state.Movement?.Movers.Count ?? 0)
            || FireLaneFacts(state, at, entry) is not { } facts || ScenarioA1FireCalculator.Precheck(facts, FireReference.Value).Count != 0)
        {
            return;
        }

        var start = events.Count;
        AddFireEvents(scope, attemptId, expected, actor, state, facts, state.PhasingSide, step, events, draw);
        if (events.Skip(start).FirstOrDefault(item => item.Payload is FireResolved)?.Payload is FireResolved record && record.Resolution.TryGetProperty("arithmetic", out var arithmetic)
            && arithmetic.TryGetProperty("originalDr", out var original) && state.Find(lane.Weapon) is EquipmentInstance { Definition: { } weapon }
            && ScenarioA1FireFollowUps.FireLaneMalfunctions(original.GetInt32(), FireReference.Value.Definitions.GetValueOrDefault(weapon.Definition)?.Breakdown,
                state.Unit(lane.Operator) is { } manning && LiveFire.CapturedBy(weapon.Definition, manning) == true))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                new ConditionsChanged(lane.Weapon, new Dictionary<string, ConditionState> { [Conditions.Malfunctioned] = ConditionState.True }), null, null));
        }
    }

    /// <summary>
    /// The units that gain "?" as their Player Turn ends (A12.12, A12.121, A12.122, the Concealment Table; ruling R12.5): each with the Final
    /// Concealment dr modifier it needs, or null when it gains "?" with no dr. Rules decides it (pass 32.c) as a search through a fact reader over the
    /// map (the design, D4); the units cross as facts, their costlier reads made when asked.
    /// </summary>
    private List<(UnitInstance Unit, int? Drm)> ConcealmentGains(GameState state)
    {
        var map = new ConcealmentMap(this, state);
        UnitInstance[] units = [.. state.Units];
        int? LocationIndex(UnitInstance unit) => state.Location(unit.Id) is { } at ? map.Index(at.Location) : null;
        var gains = ScenarioA1FireFollowUps.ConcealmentGains(
            units.Select(unit => new ConcealmentCandidateFacts(unit.Id, unit.Kind, unit.Status == InstanceStatus.Active, unit.Side == state.PhasingSide, LiveFire.IsVehicle(unit),
                vocabulary.IsA(unit.Kind, "asl:personnel"), unit.Kind == UnitKinds.Dummy, unit.Definition is not null, vocabulary.IsA(unit.Kind, "asl:squad"),
                vocabulary.IsA(unit.Kind, "asl:half-squad"), LocationIndex(unit), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), Is(unit, Conditions.Broken),
                Is(unit, Conditions.Berserk), Is(unit, Conditions.Melee), Is(unit, Conditions.Captured),
                () => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Kind == "asl:gun" && item.Holding is { } holding && holding.Holder == unit.Id),
                () => state.At(state.Location(unit.Id)!.Location).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && !Is(other, Conditions.Captured)),
                () => ReadLocation(state, state.Location(unit.Id)!.Location) is { } read ? TerrainKey(read) : null,
                () => SmokeSources(state).Any(place => place.Board == state.Location(unit.Id)!.Location.Board && place.Hex == state.Location(unit.Id)!.Location.Hex),
                () => state.At(state.Location(unit.Id)!.Location).OfType<UnitInstance>().Where(other => other != unit && other.Status == InstanceStatus.Active
                        && other.Side == unit.Side && other.Kind == "asl:leader" && !Is(other, Conditions.Broken) && !Is(other, Conditions.Pinned)
                        && other.Definition is { } definition && FireReference.Value.Definitions.GetValueOrDefault(definition.Definition)?.Leadership is not null)
                    .Select(other => FireReference.Value.Definitions[other.Definition!.Definition].Leadership!.Value).DefaultIfEmpty(0).Min())),
            units.Select((unit, index) => new WatchingEnemyFacts(index, unit.Status == InstanceStatus.Active, unit.Side != state.PhasingSide, Is(unit, Conditions.Broken),
                Is(unit, Conditions.Captured), LocationIndex(unit))),
            state.Night, state.ScenarioMonth, map);
        return [.. gains.Select(gain => (units.First(unit => unit.Id == gain.Id), gain.Drm))];
    }

    /// <summary>The map and the night as the concealment gain's search reads them: a table of Locations by index, the hex distance and LOS between two, a unit's NVR, and Illumination.</summary>
    private sealed class ConcealmentMap(GamePlanner planner, GameState state) : IConcealmentFactReader
    {
        private readonly List<BoardLocation> locations = [];
        private readonly Dictionary<BoardLocation, int> indexes = [];

        /// <summary>The index of a Location in the table, added when it is new.</summary>
        public int Index(BoardLocation location)
        {
            if (!indexes.TryGetValue(location, out var index))
            {
                index = locations.Count;
                locations.Add(location);
                indexes[location] = index;
            }

            return index;
        }

        public int? Range(int from, int target) => planner.HexDistance(state, locations[from], locations[target]);

        public bool LosOpen(int from, int target) => planner.Los(state, locations[from], locations[target]) is { IsBlocked: false };

        public int? Nvr(int unit) => NvrOf(state, state.Units.ElementAt(unit));

        public bool Illuminated(int location) => planner.Illuminated(state, locations[location]);
    }

    /// <summary>The events of the concealment gained as a Player Turn ends (ruling R12.5): each dr needed, then each unit's "?"; Rules reads each dr (pass 32.c).</summary>
    private void AddConcealmentGains(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<(UnitInstance Unit, int? Drm)> gains,
        List<GameEvent> events, Func<RollRequest, RollResult>? draw, List<string> reasons)
    {
        foreach (var (unit, drm) in gains)
        {
            if (drm is { } modifier)
            {
                if (draw is null)
                {
                    continue;
                }

                var dice = draw(new RollRequest(1, 6));
                var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, "concealment", 1, 6, dice.Values, DiceRolled.SystemSource, actor), null, null));
                var (gained, reason) = ScenarioA1FireFollowUps.ConcealmentDr(unit.Id, dice.Values[0], modifier);
                reasons.Add(reason);
                if (!gained)
                {
                    continue;
                }
            }
            else
            {
                reasons.Add(ScenarioA1FireFollowUps.ConcealmentWithoutDr(unit.Id));
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "concealment-gained",
                new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.True }), null, null));
        }
    }
}
