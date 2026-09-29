using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.ScenarioA1;
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
    /// </summary>
    private GamePlan PlanOpportunityFire(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (state.Phase != "pfph")
        {
            return Refused(scope, label, expected, "play.opportunity-fire-phase: Opportunity Fire is declared in the PFPh (A7.25)");
        }

        string[] ids = [.. Strings(arguments, "unitIds")];
        if (ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: Opportunity Fire names its units once each");
        }

        foreach (var id in ids)
        {
            if (state.Unit(id) is not { Status: InstanceStatus.Active } unit || state.Location(id) is null || unit.Side != state.PhasingSide
                || LiveFire.IsVehicle(unit) || !vocabulary.IsA(unit.Kind, "asl:personnel") || unit.Kind == UnitKinds.Dummy)
            {
                return Refused(scope, label, expected, $"play.opportunity-fire: '{id}' is not Infantry of the phasing side on the map (A7.25)");
            }

            if (new[] { Conditions.Broken, Conditions.Berserk, Conditions.Melee, Conditions.Captured, "asl:ti" }.Any(name => Is(unit, name)))
            {
                return Refused(scope, label, expected, $"play.opportunity-fire: {id} is not Good Order, or is berserk, TI, in Melee, or a prisoner (A7.25, A15.432)");
            }

            if (LiveFire.Fired(unit) || Is(unit, Conditions.BoundingFire) || state.PhaseFirers.Any(item => item.Unit == id)
                || state.SupportWeaponUses.Any(item => item.Unit == id) || state.SupportWeaponDirectors.Any(item => item.Leader == id))
            {
                return Refused(scope, label, expected, $"play.opportunity-fire: {id} has already fired or directed fire this Player Turn (A7.25)");
            }
        }

        var events = new List<GameEvent> { Event(scope, attemptId, 1, expected, "opportunity-fire-declared", new OpportunityFireDeclared(ids), null, null) };
        var revealed = new List<string>();
        foreach (var unit in ids.Select(id => state.Unit(id)!).Where(unit => Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden)))
        {
            if (SeenByEnemy(state, unit.Side, state.Location(unit.Id)!.Location, 16))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "concealment-lost",
                    new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False, [Conditions.Hidden] = ConditionState.False }),
                    null, null));
                revealed.Add(unit.Id);
            }
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
            [$"play.opportunity-fire: {string.Join(", ", ids)} hold their fire for the AFPh under a Bounding Fire counter, and do not move in the MPh (A7.25)"
                + (revealed.Count > 0 ? $"; {string.Join(", ", revealed)} loses its \"?\" (Case D)" : string.Empty)]);
    }

    /// <summary>
    /// Where an LOS enters a target hex, as a position on its perimeter (A7.7; ruling R12.11): hexside i is 2i + 1, the hexspine between hexsides i and
    /// i + 1 is 2i + 2, counting 12 positions clockwise from the north vertex of the north hexside; null when the entry cannot be read.
    /// </summary>
    private static int? PerimeterPosition(IReadOnlyList<HexsideDirection> sides) => sides switch
    {
        [var one] => (2 * (int)one) + 1,
        [var a, var b] when ((int)a + 1) % 6 == (int)b => ((2 * (int)a) + 2) % 12,
        [var a, var b] when ((int)b + 1) % 6 == (int)a => ((2 * (int)b) + 2) % 12,
        _ => null,
    };

    /// <summary>
    /// Whether LOS entries Encircle a hex (A7.7; ruling R12.11): two opposite hexspines, two opposite hexsides (exactly three vertices between them both
    /// ways), or three hexsides no two of which are adjacent.
    /// </summary>
    internal static bool Encircles(IReadOnlyCollection<int> positions)
    {
        var set = positions.ToHashSet();
        if (set.Any(position => set.Contains((position + 6) % 12)))
        {
            return true;
        }

        var sides = set.Where(position => position % 2 == 1).Select(position => (position - 1) / 2).ToHashSet();
        return sides.Any(side => sides.Contains((side + 2) % 6) && sides.Contains((side + 4) % 6));
    }

    /// <summary>
    /// An attack's share of an Encirclement (A7.7; ruling R12.11): the units firing inherent FP or a SW at no more than Normal Range, when its FP could
    /// inflict at least a NMC allowing for Cowering, with the positions where their LOS enters the target hex; none for other fire.
    /// </summary>
    private (int Units, List<int> Entries) EncirclementShare(GameState state, FireAttack facts, FireArithmetic? arithmetic)
    {
        if (facts.VehicleFire is not null || facts.Overrun is not null || facts.OrdnanceHit is not null || facts.FireKind == ScenarioA1FireCalculator.ResidualFire
            || facts.Firers is not { Count: > 0 } firers || arithmetic is null || !BoardLocation.TryParse(facts.TargetLocationId, out var target))
        {
            return (0, []);
        }

        var could = CouldCauseNmc(facts, arithmetic);
        if (!could(false) && !could(true))
        {
            return (0, []);
        }

        var reference = FireReference.Value;
        var units = 0;
        var entries = new List<int>();
        foreach (var firer in firers)
        {
            var definition = reference.Definitions.GetValueOrDefault(firer.DefinitionId ?? string.Empty);
            var weaponRanges = (firer.Weapons ?? []).Select(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.Range).OfType<int>();
            int? normal = firer.UsesInherentFp == false ? weaponRanges.DefaultIfEmpty(0).Max() : definition?.Range;
            var range = firer.Range ?? facts.Range;
            if (normal is not { } limit || range is not { } distance || distance < 1 || distance > limit || !BoardLocation.TryParse(firer.LocationId, out var from))
            {
                continue;
            }

            units++;
            if (LosEntrySides(state, target, from) is { } sides && PerimeterPosition(sides) is { } position)
            {
                entries.Add(position);
            }
        }

        return (units, entries);
    }

    /// <summary>
    /// The side this attack Encircles in its target Location, or null (A7.7; ruling R12.11): with the side's earlier attacks this phase on that Location,
    /// made consecutively (none of its attacks at another Location between), by at least two counting units whose LOS entries Encircle it.
    /// </summary>
    private string? EncirclementSeal(GameState state, IReadOnlyList<GameEvent> existing, FireAttack facts, string firingSide)
    {
        if (state.Phase is not ("pfph" or "dfph" or "afph") || facts.SprayingFire == true || !BoardLocation.TryParse(facts.TargetLocationId, out var target))
        {
            return null;
        }

        var targetSide = (facts.Targets ?? []).Where(item => item.Friendly != true && item.Dummy != true && item.GuardId is null)
            .Select(item => state.Unit(item.UnitId!)?.Side).FirstOrDefault(side => side is not null);
        if (targetSide is null || state.Encirclements.Any(item => item.Location == target && item.Side == targetSide))
        {
            return null;
        }

        var (units, entries) = EncirclementShare(state, facts, ScenarioA1FireCalculator.Preview(facts, FireReference.Value));
        if (units == 0)
        {
            return null;
        }

        foreach (var payload in ThisPhase(existing).Select(item => item.Payload).Where(item => item is FireResolved or OrdnanceFired).Reverse())
        {
            // Table player, pass 12: a Gun's shot at another Location breaks the sequence too.
            if (payload is OrdnanceFired shot)
            {
                if (state.Unit(shot.Crew)?.Side == firingSide && shot.Target != target)
                {
                    break;
                }

                continue;
            }

            var record = (FireResolved)payload;
            if (record.Firers.Count == 0 || state.Unit(record.Firers[0])?.Side != firingSide)
            {
                continue;
            }

            if (record.TargetLocation != facts.TargetLocationId)
            {
                break;
            }

            var recorded = record.Facts.Deserialize<FireAttack>(LiveFire.Json);
            var blocked = record.Resolution.TryGetProperty("losBlocked", out var flag) && flag.ValueKind == JsonValueKind.True;
            var arithmetic = record.Resolution.TryGetProperty("arithmetic", out var value) && value.ValueKind == JsonValueKind.Object
                ? value.Deserialize<FireArithmetic>(LiveFire.Json) : null;
            if (recorded is null || blocked || recorded.SprayingFire == true)
            {
                continue;
            }

            var (more, seen) = EncirclementShare(state, recorded, arithmetic);
            units += more;
            entries.AddRange(seen);
        }

        return units >= 2 && Encircles(entries) ? targetSide : null;
    }

    /// <summary>
    /// The Locations of a Fire Lane (A9.22; ruling R12.7): along the Hex Grain from the MG's hex through the target hex to the counter's hex, each at the
    /// MG's level, within its Normal Range and in its manning Infantry's LOS, with its Fire Lane Residual FP (the IFT column left of the MG's FP, doubled
    /// ADJACENT) and the LOS Hindrance DRM from the MG; null with the reason when the lane is not on a Hex Grain.
    /// </summary>
    private (IReadOnlyList<FireLaneEntry>? Entries, string? Reason) FireLaneEntries(GameState state, BoardLocation from, BoardLocation target, BoardLocation to,
        int normalRange, int firepower)
    {
        var column = Array.FindLastIndex(ScenarioA1FireReference.ColumnFp, fp => fp <= firepower) - 1;
        if (column < 0)
        {
            return (null, "play.fire-lane: the MG's FP has no IFT column to its left, so its Fire Lane has no Residual FP (A9.22)");
        }

        var laneFp = ScenarioA1FireReference.ColumnFp[column];
        var fromLevel = ReadLocation(state, from) is { } fromRead ? fromRead.Hex.BaseLevel + from.Level : (int?)null;
        foreach (var direction in Enum.GetValues<HexsideDirection>())
        {
            var path = new List<BoardLocation>();
            var at = from;
            for (var step = 1; step <= normalRange && Across(state, at, direction) is { } next; step++)
            {
                at = next;
                path.Add(at);
            }

            var targetIndex = path.FindIndex(item => item.Board == target.Board && item.Hex == target.Hex);
            var toIndex = path.FindIndex(item => item.Board == to.Board && item.Hex == to.Hex);
            if (targetIndex < 0 || toIndex < targetIndex)
            {
                continue;
            }

            var entries = new List<FireLaneEntry>();
            foreach (var (location, index) in path.Take(toIndex + 1).Select((item, index) => (item, index)))
            {
                if (ReadLocation(state, location) is not { } read || read.Hex.BaseLevel != fromLevel || Los(state, from, location) is not { IsBlocked: false } los)
                {
                    continue;
                }

                entries.Add(new FireLaneEntry(location, index == 0 ? 2 * laneFp : laneFp, los.Hindrance));
            }

            return entries.Count == 0 ? (null, "play.fire-lane: no Location of the lane is at the MG's level and in its LOS (A9.22)") : (entries, null);
        }

        return (null, $"play.fire-lane: {to} is not on the Hex Grain through the MG's hex and {target}, within the MG's Normal Range (A9.22)");
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

        return HeatOfBattleFacts(state, map) with
        {
            FireLane = true,
            Los = (map.Los ?? new FireLos(false, 0, true, false)) with
            {
                HindranceDrm = (map.Los?.HindranceDrm ?? 0) + entry.HindranceDrm
            },
        };
    }

    /// <summary>
    /// A Fire Lane's attack as the moving stack enters one of its Locations (A9.22, A9.223): once the other attacks there are made, on the stack still
    /// there, while the lane is in place; an Original DR at least its MG's Breakdown Number malfunctions the MG, which ends the lane.
    /// </summary>
    private void AddFireLaneAttack(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, List<GameEvent> events,
        FireLane lane, FireLaneEntry entry, BoardLocation at, int? step, Func<RollRequest, RollResult> draw)
    {
        if (events.Any(item => item.Payload is ChoicePending) || Replay([.. existing, .. events]).Current is not { } state || !state.FireLanes.Any(item => item.Weapon == lane.Weapon)
            || state.Movement is not { } movement || movement.Location != at || movement.Movers.Count == 0 || FireLaneFacts(state, at, entry) is not { } facts
            || ScenarioA1FireCalculator.Precheck(facts, FireReference.Value).Count != 0)
        {
            return;
        }

        var start = events.Count;
        AddFireEvents(scope, attemptId, expected, actor, state, facts, state.PhasingSide, step, events, draw);
        if (events.Skip(start).FirstOrDefault(item => item.Payload is FireResolved)?.Payload is FireResolved record && record.Resolution.TryGetProperty("arithmetic", out var arithmetic)
            && arithmetic.TryGetProperty("originalDr", out var original) && state.Find(lane.Weapon) is EquipmentInstance { Definition: { } weapon }
            && original.GetInt32() >= (FireReference.Value.Definitions.GetValueOrDefault(weapon.Definition)?.Breakdown ?? 12)
                - (state.Unit(lane.Operator) is { } manning && LiveFire.CapturedBy(weapon.Definition, manning) == true ? 2 : 0))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                new ConditionsChanged(lane.Weapon, new Dictionary<string, ConditionState> { [Conditions.Malfunctioned] = ConditionState.True }), null, null));
        }
    }

    /// <summary>
    /// The units that gain "?" as their Player Turn ends (A12.12, A12.121, A12.122, the Concealment Table; ruling R12.5): each with the Final
    /// Concealment dr modifier it needs, or null when it gains "?" with no dr.
    /// </summary>
    private List<(UnitInstance Unit, int? Drm)> ConcealmentGains(GameState state)
    {
        var gains = new List<(UnitInstance, int?)>();
        var enemies = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide && !Is(unit, Conditions.Broken)
            && !Is(unit, Conditions.Captured) && state.Location(unit.Id) is not null).ToArray();
        foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && !LiveFire.IsVehicle(unit)
            && vocabulary.IsA(unit.Kind, "asl:personnel") && unit.Kind != UnitKinds.Dummy && unit.Definition is not null)
            .OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            if (state.Location(unit.Id) is not { } at || new[] { Conditions.Concealed, Conditions.Hidden, Conditions.Broken, Conditions.Berserk, Conditions.Melee,
                Conditions.Captured }.Any(name => Is(unit, name))
                || state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Kind == "asl:gun" && item.Holding is { } holding && holding.Holder == unit.Id)
                || state.At(at.Location).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && !Is(other, Conditions.Captured)))
            {
                // A unit sharing its Location with enemy units is about to be held in Melee (A11.15), and gains no "?" (a reading).
                continue;
            }

            var inLosNear = false;
            var inLosFar = false;
            var within16 = false;
            foreach (var enemy in enemies)
            {
                var from = state.Location(enemy.Id)!.Location;
                var range = HexDistance(state, from, at.Location);
                within16 |= range <= 16;
                if (Los(state, from, at.Location) is { IsBlocked: false })
                {
                    inLosNear |= range <= 16;
                    inLosFar |= range > 16;
                }
            }

            var read = ReadLocation(state, at.Location);
            var terrain = read is null ? null : TerrainKey(read);
            var inSeason = state.ScenarioMonth is >= 6 and <= 9;
            var concealmentTerrain = terrain is "brush" or "woods" or "orchard" or "marsh" or "wooden-building" or "stone-building" or "wooden-rubble" or "stone-rubble"
                || (terrain == "grain" && inSeason);
            if (inLosNear || (inLosFar && !concealmentTerrain))
            {
                continue;
            }

            var needsDr = inLosFar || (!concealmentTerrain && within16);
            if (!needsDr)
            {
                gains.Add((unit, null));
                continue;
            }

            // A12.122: +US#, + the best Good Order leader's Leadership in the Location unless alone, - the Location's TEM and in-hex Hindrance.
            var size = vocabulary.IsA(unit.Kind, "asl:squad") ? 3 : vocabulary.IsA(unit.Kind, "asl:half-squad") || unit.Kind == "asl:crew" ? 2 : 1;
            var leadership = state.At(at.Location).OfType<UnitInstance>().Where(other => other != unit && other.Status == InstanceStatus.Active
                    && other.Side == unit.Side && other.Kind == "asl:leader" && !Is(other, Conditions.Broken) && !Is(other, Conditions.Pinned)
                    && other.Definition is { } definition && FireReference.Value.Definitions.GetValueOrDefault(definition.Definition)?.Leadership is not null)
                .Select(other => FireReference.Value.Definitions[other.Definition!.Definition].Leadership!.Value).DefaultIfEmpty(0).Min();
            var tem = terrain is not null && ScenarioA1FireReference.Tem.TryGetValue(terrain, out var value) ? value : 0;
            // A6.7 (referee, pass 12): only SMOKE in the Location hinders its own units; brush, grain, orchard, and marsh do not.
            var smoke = SmokeSources(state).Any(place => place.Board == at.Location.Board && place.Hex == at.Location.Hex) ? 2 : 0;
            gains.Add((unit, size + leadership - tem - smoke));
        }

        return gains;
    }

    /// <summary>The events of the concealment gained as a Player Turn ends (ruling R12.5): each dr needed, then each unit's "?".</summary>
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
                var final = dice.Values[0] + modifier;
                reasons.Add($"play.concealment: {unit.Id}'s Final Concealment dr is {dice.Values[0]}{modifier:+0;-0;+0} = {final}: {(final <= 5 ? "gains" : "no")} \"?\" (A12.122)");
                if (final > 5)
                {
                    continue;
                }
            }
            else
            {
                reasons.Add($"play.concealment: {unit.Id} gains \"?\" (A12.12, the Concealment Table)");
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "concealment-gained",
                new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.True }), null, null));
        }
    }
}
