using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Infantry movement and fire over the terrain of backlog pass 10 (rulings R10.1 to R10.7): the cost of one step between Locations, levels in
/// buildings and on hills, walls and hedges on entry and as TEM at the target, Wall Advantage, and Height Advantage.
/// </summary>
public sealed partial class GamePlanner
{
    // One Infantry step's cost and what it enters: the InfantryEntry record moved to Rules (pass 32.b).

    /// <summary>The angle between two bearings in degrees, 0 to 180.</summary>
    private static double AngleBetween(double one, double two) => ScenarioA1Geometry.AngleBetween(one, two);

    private static bool IsBuildingTerrain(string? key) => ScenarioA1Definitions.IsBuildingTerrain(key);

    private static bool IsRubbleTerrain(string? key) => ScenarioA1Definitions.IsRubbleTerrain(key);

    /// <summary>The wall or hedge on a hexside, as <c>wall</c> or <c>hedge</c>; null for none; <c>other</c> for hexside terrain the pass does not review.</summary>
    private static string? WallOn(HexsideFacts? side) => ScenarioA1TerrainCosts.WallOn(side?.HexsideTerrain?.Name);

    /// <summary>The hexside an Infantry step crosses, as Rules reads it (pass 32.b).</summary>
    private static CrossedHexsideFacts CrossedFacts(HexsideFacts crossed) =>
        new(crossed.Cliff, crossed.Slope, crossed.HexsideTerrain?.Name, crossed.Terrain?.IsRoad == true, crossed.Terrain?.Name);

    /// <summary>One Location of an Infantry step as Rules reads it (pass 32.b); null when the map cannot read it.</summary>
    private static StepLocationFacts? StepLocation(LocationRead? read) =>
        read is null ? null : new(TerrainKey(read), (read.Level.Terrain ?? read.Hex.Center.Terrain)?.Name, read.Hex.BaseLevel, read.Hex.Stairway);

    /// <summary>
    /// The cost of an Infantry step from one Location to another (rulings R10.1 to R10.3, R10.5): up or down one level in a stairwell hex
    /// (B23.4, 1 MF); into an ADJACENT hex of the same building at an upper level (B23.421, 2 MF); or into an adjacent hex at ground level at
    /// its terrain's cost (A4.13), a road hexside's rate (A4.132), doubled one level up (A4.133, B10.4), with Abrupt Elevation Changes
    /// (B10.51), one MF more across a wall or hedge (B9.4), and one more into SMOKE or a burning wreck (A24.7, B25.141). Null with the reason
    /// when the step is not allowed or not reviewed.
    /// </summary>
    private (InfantryEntry? Entry, string? Reason) InfantryStep(GameState state, BoardLocation from, BoardLocation to)
    {
        // Pass 32.b: the map is read here as the step read it (both Locations within one hex, the step between two), and Rules decides.
        var hexsides = from.Side is not null || to.Side is not null;
        var sameHex = from.Board == to.Board && from.Hex == to.Hex;
        var (fromRead, toRead, adjacent, crossed) = hexsides ? (null, null, false, null)
            : sameHex ? (ReadLocation(state, from), ReadLocation(state, to), false, null) : Step(state, from, to);
        var facts = new InfantryStepFacts(from.Side is not null, to.Side is not null, sameHex, from.Level, to.Level, from.ToString(), to.ToString(), from.Hex.ToString(),
            StepLocation(fromRead), StepLocation(toRead), adjacent, crossed is null ? null : CrossedFacts(crossed), state.Night, state.ScenarioMonth);
        return ScenarioA1TerrainCosts.InfantryStep(facts, () => BlazeEntryHalfMf(state, to), (terrain, road, rise) => InfantryWeatherHalfMf(state, crossed!, terrain, road, rise));
    }

    /// <summary>
    /// The cost of an Infantry step into a ground-level Location across a hexside (rulings R10.1, R10.3): the step of <see cref="InfantryStep"/> from an
    /// adjacent hex <paramref name="rise"/> levels lower, or the entry from off board across the map edge's hexside, from the mirror-image hex at the same
    /// level (A2.51, A2.6; ruling R25.3).
    /// </summary>
    private static (InfantryEntry? Entry, string? Reason) GroundStep(GameState state, BoardLocation to, Maps.Derivation.HexsideFacts crossed, string terrain, int rise) =>
        ScenarioA1TerrainCosts.GroundStep(CrossedFacts(crossed), terrain, rise, state.ScenarioMonth, () => BlazeEntryHalfMf(state, to),
            (entered, road, climbed) => InfantryWeatherHalfMf(state, crossed, entered, road, climbed));

    /// <summary>
    /// The facts of a Location's hexside named in the map's frame (table player, pass 10): on a reversed board of a placed map the board's own
    /// hexside is the opposite one, as the composed map read turns it.
    /// </summary>
    private HexsideFacts? HexsideAt(GameState state, BoardLocation at, HexsideDirection mapSide)
    {
        var side = state.Map.IsPlaced && state.Map.Board(at.Board) is { Slot.Reversed: true } ? (HexsideDirection)(((int)mapSide + 3) % 6) : mapSide;
        return ReadLocation(state, at)?.Hex.Hexsides.FirstOrDefault(item => item.Side == side);
    }

    /// <summary>The Location across a hexside of a Location's hex, at ground level; null off the map.</summary>
    private BoardLocation? Across(GameState state, BoardLocation at, HexsideDirection side)
    {
        if (Composed(state) is { } composed)
        {
            return composed.Neighbor(at.Board, at.Hex, side) is { } other ? new BoardLocation(other.Board, other.Hex, 0) : null;
        }

        return state.Map.Board(at.Board) is { } placed && boards.TryGetBoard(at.Board, placed.Version).Board is { } handle
            && handle.Neighbor(at.Hex, side) is { } hex ? new BoardLocation(at.Board, hex, 0) : null;
    }

    /// <summary>The hexside of a Location's hex facing an adjacent hex; null when they are not adjacent.</summary>
    private HexsideDirection? SideToward(GameState state, BoardLocation at, BoardLocation other) =>
        Enum.GetValues<HexsideDirection>().Cast<HexsideDirection?>().FirstOrDefault(side => Across(state, at, side!.Value) is { } across
            && across.Board == other.Board && across.Hex == other.Hex);

    /// <summary>
    /// The hexsides of a target hex that an LOS from a firer's hex crosses into it: one, or the two that meet at the vertex it passes through,
    /// read from the bearing of each neighbor so a reversed board or a seam keeps one frame. Null when the bearings cannot be read.
    /// </summary>
    private IReadOnlyList<HexsideDirection>? LosEntrySides(GameState state, BoardLocation target, BoardLocation firer)
    {
        if (Bearing(state, target, firer) is not { } toward)
        {
            return null;
        }

        var sides = new List<(HexsideDirection Side, double Off)>();
        foreach (var side in Enum.GetValues<HexsideDirection>())
        {
            if (Across(state, target, side) is { } neighbor && Bearing(state, target, neighbor) is { } bearing)
            {
                sides.Add((side, AngleBetween(toward, bearing)));
            }
        }

        var within = sides.Where(item => item.Off <= 30 + 1e-6).OrderBy(item => item.Off).ToArray();
        return within.Length == 0 ? null
            : within[0].Off < 30 - 1e-6 ? [within[0].Side]
            : [.. within.Where(item => Math.Abs(item.Off - 30) <= 1e-6).Select(item => item.Side)];
    }

    /// <summary>
    /// The wall or hedge TEM a target claims against a group's LOS (B9.3, B9.31, B9.33, B9.35; rulings R10.5, R10.6): from the wall or hedge on
    /// the hexside each firer's LOS crosses into the target hex, or at a vertex on either hexside there or the hexspine leading away; reduced
    /// by one for each full level a firer's height above the target hex exceeds the range; none against a firer that holds Wall Advantage
    /// over that hexside. Every firer Location must give the same TEM. Null TEM with no reason when none applies.
    /// </summary>
    private (FireHexsideTem? Tem, string? Reason) HexsideTemAt(GameState state, BoardLocation target, IReadOnlyList<(BoardLocation From, int Range)> firers,
        string firingSide, IReadOnlyList<GameEvent>? history)
    {
        // Pass 32.b: the target hex, each firer's LOS entry hexsides, and the hexes beyond them are read here, and Rules decides.
        var targetRead = ReadLocation(state, target);
        WallTemFirerFacts Firer(BoardLocation from, int range)
        {
            var sides = LosEntrySides(state, target, from);
            IReadOnlyList<LosEntrySideFacts>? entry = sides is null ? null : [.. sides.Select(side =>
            {
                var beyond = Across(state, target, side);
                return new LosEntrySideFacts((int)side, WallOn(HexsideAt(state, target, side)), HexsideAt(state, target, side)?.Terrain?.IsRoad == true,
                    beyond is null ? null : ReadLocation(state, beyond)?.Hex.BaseLevel, beyond is { } across && across.Board == from.Board && across.Hex == from.Hex);
            })];
            var spine = sides is { Count: 2 } && Across(state, target, sides[0]) is { } one && Across(state, target, sides[1]) is { } two
                && Step(state, one, two).Crossed is { } crossed ? WallOn(crossed) : null;
            return new WallTemFirerFacts(from == target, from.Level, range, entry, spine, ReadLocation(state, from)?.Hex.BaseLevel,
                () => WallAdvantageHolder(state, target, from, history));
        }

        var facts = new WallTemFacts(targetRead is not null, target.Level, targetRead?.Hex.BaseLevel ?? 0,
            targetRead is null ? [] : [.. targetRead.Hex.Hexsides.Select(side => side.HexsideTerrain?.Name)],
            [.. firers.Select(item => Firer(item.From, item.Range))], firingSide, state.Phase == "mph" && state.Movement?.Location == target);
        return ScenarioA1TerrainCosts.HexsideTemAt(facts);
    }

    /// <summary>
    /// Which side holds Wall Advantage over the hexside two ADJACENT Locations share (B9.32, B9.321, B9.41; ruling R10.6): of the Good Order
    /// units there, the one that entered its Location first; units there since setup give it to the Scenario Defender when named. Null holder
    /// with no reason when neither Location holds a unit that may claim it.
    /// </summary>
    private (string? Side, string? Reason) WallAdvantageHolder(GameState state, BoardLocation one, BoardLocation two, IReadOnlyList<GameEvent>? history)
    {
        // Pass 32.b: both Locations and the units in them are read here, the arrivals only when Rules asks, and Rules decides.
        WallAdvantageLocationFacts Location(BoardLocation at) => new(at.Level, ReadLocation(state, at) is { } read ? TerrainKey(read) : null,
            [.. state.At(at).OfType<UnitInstance>().Select(unit => new WallAdvantageUnitFacts(unit.Id, unit.Side, unit.Status == InstanceStatus.Active,
                unit.Kind == UnitKinds.Dummy, LiveFire.IsVehicle(unit), Is(unit, Conditions.Captured), Is(unit, Conditions.Broken)))]);
        var arrivals = new Lazy<Dictionary<string, long>>(() => Arrivals(history!));
        return ScenarioA1TerrainCosts.WallAdvantageHolder(new WallAdvantageFacts(Location(one), Location(two), history is not null,
            id => arrivals.Value.GetValueOrDefault(id), state.ScenarioDefender));
    }

    /// <summary>
    /// The revision at which each unit last entered its Location: setup is 0, and a movement step, advance, or entry that changed its Location
    /// sets it; a unit produced by lineage keeps its predecessors' (ruling R10.6).
    /// </summary>
    private static Dictionary<string, long> Arrivals(IReadOnlyList<GameEvent> history)
    {
        var at = new Dictionary<string, BoardLocation>(StringComparer.Ordinal);
        var arrived = new Dictionary<string, long>(StringComparer.Ordinal);
        void Place(string id, BoardLocation location, long revision)
        {
            if (!at.TryGetValue(id, out var previous) || previous != location)
            {
                arrived[id] = at.ContainsKey(id) ? revision : 0;
                at[id] = location;
            }
        }

        foreach (var item in history)
        {
            switch (item.Payload)
            {
                case InstanceCreated { Instance.Position: MapPosition created } placed:
                    at[placed.Instance.Id] = created.Location;
                    arrived[placed.Instance.Id] = 0;
                    break;
                case MovementStepped moving:
                    foreach (var id in moving.Movers)
                    {
                        Place(id, moving.To, item.Revision);
                    }

                    break;
                case AdvanceMoved advanced:
                    foreach (var id in advanced.Units)
                    {
                        Place(id, advanced.To, item.Revision);
                    }

                    break;
                case InstanceMoved { Position: MapPosition moved } entered:
                    Place(entered.Id, moved.Location, item.Revision);
                    break;
                case LineageRecorded lineage:
                    var kept = lineage.Consumed.Select(id => arrived.GetValueOrDefault(id)).DefaultIfEmpty(0).Min();
                    foreach (var produced in lineage.Produced)
                    {
                        if (produced.Position is MapPosition position)
                        {
                            at[produced.Id] = position.Location;
                            arrived[produced.Id] = kept;
                        }
                    }

                    break;
            }
        }

        return arrived;
    }

    /// <summary>
    /// Whether a target at a higher elevation than every firer may claim Height Advantage (B10.31; ruling R10.4): not a mover under Defensive
    /// First Fire whose step came from a lower hex across the hexside a firer's LOS crosses into its hex.
    /// </summary>
    private bool HeightAdvantageAt(GameState state, BoardLocation target, LocationRead targetRead, IReadOnlyList<(BoardLocation From, int Level)> firers, bool snapShot)
    {
        // Pass 32.b: the moving stack's origin and the LOS entry hexsides are read here, the latter only when Rules asks, and Rules decides.
        var movement = state.Movement;
        var left = movement?.From;
        var facts = new HeightAdvantageFacts(targetRead.Hex.BaseLevel, target.Level, [.. firers.Select(item => item.Level)], state.Phase == "mph",
            left is not null && movement!.Location == target, left is null ? null : ReadLocation(state, left)?.Hex.BaseLevel,
            left is null ? null : SideToward(state, target, left) is { } climbed ? (int)climbed : null,
            index => LosEntrySides(state, target, firers[index].From) is { } sides ? [.. sides.Select(side => (int)side)] : null, snapShot);
        return ScenarioA1TerrainCosts.HeightAdvantageAt(facts);
    }

    /// <summary>
    /// A moving stack's step (rulings R10.1 to R10.3, R10.7): an ordinary step, a step into a woods or building hex in Bypass along one or two of its
    /// hexsides, or, from Bypass, a step out through the far vertex or into the obstacle itself. Returns the step's cost, the Bypass hexsides of a
    /// Bypass step, and whether the stack occupies the obstacle it Bypassed; or the reason it is refused.
    /// </summary>
    private (InfantryEntry? Entry, string? Reason, IReadOnlyList<HexsideDirection>? Bypass, bool Occupy) MoveEntry(GameState state, UnitInstance[] movers,
        BoardLocation from, BoardLocation to, MovementState? current, List<HexsideDirection>? bypass)
    {
        // Pass 32.b: the state and the map are read here, and Rules decides which step the stack takes and reads it.
        var lane = current?.Bypass ?? [];
        var fromRead = ReadLocation(state, from);
        var sideTowardFrom = SideToward(state, to, from);
        var facts = new MoveEntryFacts(
            lane.Count > 0,
            [.. lane.Select(item => (int)item)],
            current is not null && current.Location == from,
            current is not null && movers.All(unit => current.Movers.Contains(unit.Id, StringComparer.Ordinal)),
            current is not null && current.Movers.Any(id => current.Members.Contains(id, StringComparer.Ordinal) && !movers.Any(unit => unit.Id == id)),
            to == from,
            movers.Length > 0 && state.At(from).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != movers[0].Side && !Is(unit, Conditions.Captured)),
            bypass is not null,
            fromRead is null ? null : TerrainKey(fromRead),
            from.Level,
            fromRead is not null,
            to.Level == 0,
            sideTowardFrom is { } toward ? (int)toward : null,
            SideToward(state, from, to) is { } facing ? WallOn(HexsideAt(state, from, facing)) : null,
            to.ToString());
        var verdict = ScenarioA1TerrainCosts.MoveEntry(facts,
            () => BypassEntered(state, from, current!.From, lane) is { } entered ? (int)entered : null,
            side => Across(state, from, (HexsideDirection)side) is { } open && open.Board == to.Board && open.Hex == to.Hex,
            () => BlazeEntryHalfMf(state, from));
        if (verdict.Refusal is { } refusal)
        {
            return (null, refusal, null, false);
        }

        if (verdict.Occupy is { } occupy)
        {
            return (occupy, null, null, true);
        }

        if (verdict.OrdinaryStep)
        {
            var (exit, exitReason) = InfantryStep(state, from, to);
            return (exit, exitReason, null, false);
        }

        return BypassStep(state, movers, to, sideTowardFrom!.Value, fromRead!.Hex.BaseLevel, facts.EntryWall, bypass!);
    }

    /// <summary>
    /// The hexside a stack in Bypass entered its hex by (ruling R10.7): toward the Location it came from, or, for a stack that entered the map in Bypass,
    /// the map edge's hexside beside its first Bypass hexside (ruling R25.3).
    /// </summary>
    private HexsideDirection? BypassEntered(GameState state, BoardLocation at, BoardLocation? came, IReadOnlyList<HexsideDirection> lane) =>
        came is { } previous ? SideToward(state, at, previous)
            : EdgeSides(state, at).Select(item => (HexsideDirection?)item.Side).FirstOrDefault(side => ((int)lane[0] - (int)side!.Value + 6) % 6 is 1 or 5);

    /// <summary>
    /// A Bypass step into a woods or building hex (A4.3, A4.31; ruling R10.7) across its hexside <paramref name="side"/>: from an adjacent hex at
    /// <paramref name="fromBaseLevel"/>, or from off board across the map edge (ruling R25.3), with the wall or hedge on the hexside crossed.
    /// </summary>
    private (InfantryEntry? Entry, string? Reason, IReadOnlyList<HexsideDirection>? Bypass, bool Occupy) BypassStep(GameState state, UnitInstance[] movers,
        BoardLocation to, HexsideDirection side, int fromBaseLevel, string? entryWall, List<HexsideDirection> bypass)
    {
        // Pass 32.b: the target hex, its hexsides, and the units in it are read here, and Rules decides.
        var targetRead = ReadLocation(state, to);
        var there = state.At(to).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).ToArray();
        var facts = new BypassStepFacts(
            [.. bypass.Select(bypassed => HexsideAt(state, to, bypassed) is { } read
                ? new BypassedHexsideFacts((int)bypassed, bypassed.ToString().ToLowerInvariant(), true, read.Terrain?.Name, read.HexsideTerrain?.Name, read.Terrain?.IsRoad == true)
                : new BypassedHexsideFacts((int)bypassed, bypassed.ToString().ToLowerInvariant(), false, null, null, false))],
            (int)side,
            to.Level,
            to.ToString(),
            targetRead is not null,
            targetRead is null ? null : TerrainKey(targetRead),
            targetRead?.Hex.BaseLevel ?? 0,
            targetRead is null ? [] : [.. targetRead.Hex.Hexsides.Select(item => item.HexsideTerrain?.Name)],
            movers.Length > 0 && there.Any(unit => unit.Side == movers[0].Side),
            movers.Length > 0 && there.Any(unit => unit.Side != movers[0].Side && KnownEnemy(unit) && !Is(unit, Conditions.Disrupted)),
            fromBaseLevel,
            entryWall,
            state.ScenarioMonth);
        var entered = HexsideAt(state, to, side);
        var (entry, reason) = ScenarioA1TerrainCosts.BypassStep(facts, () => BlazeEntryHalfMf(state, to),
            entered is null ? null : (terrain, road, climbed) => InfantryWeatherHalfMf(state, entered, terrain, road, climbed));
        return (entry, reason, entry is null ? null : bypass, false);
    }

    /// <summary>
    /// Whether a Good Order MMC moving with a Good Order leader of its nationality, who began the MPh with it and has moved with it at every step, has
    /// the leader's two MF bonus (A4.12; ruling R10.8). A berserk unit's MF are never increased but by the Road Bonus (A15.431).
    /// </summary>
    private bool LeaderBonus(GameState state, UnitInstance unit, IReadOnlyList<UnitInstance> movers) =>
        state is null ? throw new ArgumentNullException(nameof(state)) : ScenarioA1MovementCalculator.LeaderBonus(MovingUnit(unit), [.. movers.Select(MovingUnit)]);

    /// <summary>
    /// The MMC a leader lends his IPC to (A4.42; ruling R10.8): the only MMC of the stack with his leader bonus that carries more than its own IPC; the
    /// leader's own IPC is then spent. Nulls when there is none, or more than one.
    /// </summary>
    private (string? Recipient, string? Leader) LeaderIpcRecipient(GameState state, UnitInstance[] movers) =>
        ScenarioA1MovementCalculator.LeaderIpcRecipient([.. movers.Select(MovingUnit)], index => Laden(state, movers[index]));

    /// <summary>A unit's nationality from the reviewed catalog; null when the catalog does not name it.</summary>
    private static string? Nationality(UnitInstance unit) =>
        unit.Definition is { } reference ? FireReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.Nationality : null;
}
