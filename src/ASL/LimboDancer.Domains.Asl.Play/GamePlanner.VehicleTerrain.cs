using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Vehicles over the terrain of backlog pass 10 (backlog pass 11, rulings R11.1 to R11.9): the Terrain Chart's MP costs by movement type
/// (p. 698, read from the rendered chart), ALL and half-allotment entries, walls, hedges, and hills, Bog Checks and their DRM
/// (D8.21), Reverse movement, and VBM with its CAFP.
/// </summary>
public sealed partial class GamePlanner
{
    // The Terrain Chart's MP Entrance Cost (p. 698) in half MP, by movement type (ruling R11.7), moved to Rules (pass 32.a).
    private static readonly IReadOnlyDictionary<(string MovementType, string Terrain), int> VehicleTerrainHalfMp = ScenarioA1ResultTables.VehicleTerrainHalfMp;

    /// <summary>
    /// One vehicle entry (rulings R11.1, R11.2, R11.5, R11.7, R11.9): its cost in half MP, whether it takes the whole printed allotment (ALL), the Bog DR
    /// the hex entered needs (null for none) with its DRM, the Location entered and the hexside straddled after a VBM, the terrain, whether it crossed a
    /// road hexside at the road rate, and the Bog DR of a hedge crossed, taken first in the hex left (B9.4; referee, pass 11).
    /// </summary>
    internal sealed record VehicleEntry(int HalfMp, bool All, int? BogDrm, IReadOnlyList<string> BogCauses, BoardLocation To, BoardLocation? Straddling,
        string Terrain, bool Road, bool Reverse, int? HedgeBogDrm)
    {
        public bool Bypass => Straddling is not null;

        public bool BogInLeftHex => HedgeBogDrm is not null;
    }

    private static string? MovementTypeOf(UnitInstance vehicle) => VehicleDefinition(vehicle)?.MovementType;

    private static bool Tracked(UnitInstance vehicle) => ScenarioA1VehicleTerrainCosts.Tracked(MovementTypeOf(vehicle));

    /// <summary>Whether a vehicle is tracked, and so may attempt ESB (D2.5).</summary>
    public static bool IsTracked(UnitInstance vehicle) => Tracked(vehicle);

    /// <summary>D2.21 (ruling R11.1): Reverse costs four times the entry for a tracked vehicle, three times for a truck.</summary>
    private static int ReverseMultiplier(UnitInstance vehicle) => ScenarioA1VehicleTerrainCosts.ReverseMultiplier(MovementTypeOf(vehicle));

    /// <summary>
    /// The Bog DRM of a vehicle (D8.21; ruling R11.9) that belong to it, whatever the terrain: +1 Normal Ground Pressure (none printed), +2 High, 0 Low;
    /// +1 towing a Gun; +1 not fully-tracked; +1 truck-type MP.
    /// </summary>
    private static List<(string Cause, int Drm)> VehicleBogDrm(GameState state, UnitInstance vehicle) =>
        ScenarioA1VehicleTerrainCosts.VehicleBogDrm(VehicleDefinition(vehicle)?.GroundPressure, Towing(state, vehicle), VehicleDefinition(vehicle)?.MovementType);

    /// <summary>
    /// Whether a vehicle is a BU AFV (D5.2, p. 203; ruling R7.11; pass 35, task 35.13 a): an AFV whose crew is not CE, as Rules decides it. A CT AFV
    /// is BU with no counter recorded, so the recorded condition alone does not say.
    /// </summary>
    private static bool ButtonedUpAfv(UnitInstance vehicle) => IsAfv(vehicle) && !LiveFire.CrewExposed(vehicle);

    private static bool Towing(GameState state, UnitInstance vehicle) =>
        state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id);

    /// <summary>
    /// A vehicle's outright entry of an ADJACENT ground-level Location (rulings R11.6 to R11.9): the Terrain Chart cost of its movement type, a road
    /// hexside's rate (D2.16), ALL or half the allotment with a Bog DR in woods (B13.41, B13.42; <paramref name="allMp"/> chooses ALL for a
    /// fully-tracked vehicle), half the allotment with a Bog DR in rubble (B24.4), a wall or hedge (B9.4), 4 MP per level up (2 by road), the wreck
    /// and vehicle penalty (D2.14, doubled in woods, B13.41), SMOKE, and towing (C10.1). Null with the reason when it may not.
    /// </summary>
    private (VehicleEntry? Entry, string? Reason) VehicleOutright(GameState state, UnitInstance vehicle, BoardLocation from, BoardLocation to, bool reverse, bool allMp)
    {
        var (fromRead, toRead, adjacent, crossed) = Step(state, from, to);
        if (fromRead is null || toRead is null || !adjacent || crossed is null)
        {
            return (null, $"{to} is not an adjacent Location the map reads");
        }

        return from.Level != 0 ? (null, "vehicles stay at ground level (B23.4)") : VehicleCost(state, vehicle, fromRead, toRead, crossed, to, reverse, allMp);
    }

    /// <summary>
    /// A vehicle's entry from off board across a map edge hexside (A2.51, A2.52; ruling R26.1): as an outright entry from the mirror-image hex beyond, at
    /// the hex's own level, across the edge hexside's road, wall, or hedge.
    /// </summary>
    private (VehicleEntry? Entry, string? Reason) VehicleEdgeEntry(GameState state, UnitInstance vehicle, BoardLocation to, HexsideDirection side, bool allMp) =>
        ReadLocation(state, to) is { } read && HexsideAt(state, to, side) is { } crossed
            ? VehicleCost(state, vehicle, read, read, crossed, to, false, allMp)
            : (null, $"the cost of crossing the map edge at {to} is not decided (ruling R26.1)");

    /// <summary>The cost of a vehicle's outright entry of a Location over the hexside it crosses (rulings R11.6 to R11.9), as <see cref="VehicleOutright"/> reads it.</summary>
    private (VehicleEntry? Entry, string? Reason) VehicleCost(GameState state, UnitInstance vehicle, LocationRead fromRead, LocationRead toRead, HexsideFacts crossed, BoardLocation to,
        bool reverse, bool allMp)
    {
        var type = MovementTypeOf(vehicle);
        var (entry, reason) = ScenarioA1VehicleTerrainCosts.EntryCost(type, to.ToString(), to.Level, crossed.Cliff || crossed.Slope, TerrainKey(toRead),
            (toRead.Level.Terrain ?? toRead.Hex.Center.Terrain)?.Name, WallOn(crossed), crossed.HexsideTerrain?.Name, crossed.Terrain?.IsRoad == true,
            toRead.Hex.BaseLevel - fromRead.Hex.BaseLevel, state.ScenarioMonth, state.Weather("mud"), crossed.Terrain?.Name == "Paved Road",
            state.SpecialRules.Contains("plowed-roads", StringComparer.Ordinal), PrintedHalfMp(vehicle), ButtonedUpAfv(vehicle),
            state.Weather("ground-snow"), state.Weather("deep-snow"), road => WreckEntryHalfMp(state, to, road), () => HasSmoke(state, to), Towing(state, vehicle),
            (terrain, road, paved, plowed) => VehicleWeatherHalfMp(state, type!, terrain, road, paved, plowed, toRead.Hex.BaseLevel - fromRead.Hex.BaseLevel),
            reverse, allMp, VehicleBogDrm(state, vehicle),
            toRead.Hex.BaseLevel <= 0 && Neighbors(state, to).Any(near => ReadLocation(state, near) is { } nearRead && TerrainKey(nearRead) == "marsh"));
        return entry is null
            ? (null, reason)
            : (new VehicleEntry(entry.HalfMp, entry.All, entry.BogDrm, entry.BogCauses, to, null, entry.Terrain, entry.Road, reverse, entry.HedgeBogDrm), null);
    }

    /// <summary>The two ADJACENT
    private BoardLocation[] SharedNeighbors(GameState state, BoardLocation one, BoardLocation two)
    {
        var second = Neighbors(state, two).Select(item => (item.Board, item.Hex)).ToHashSet();
        return [.. Neighbors(state, one).Where(item => second.Contains((item.Board, item.Hex)) && !(item.Board == two.Board && item.Hex == two.Hex))];
    }

    /// <summary>
    /// The hex at each end of the hexside two ADJACENT hexes share, for a vehicle facing along it (D2.32): the hex beyond the vertex it faces (the
    /// base of its Bypass VCA) and the hex beyond the vertex behind it. Nulls when the facing does not run along that hexside.
    /// </summary>
    private (BoardLocation? Front, BoardLocation? Rear) LaneEnds(GameState state, BoardLocation one, BoardLocation two, UnitFacing facing)
    {
        var ends = SharedNeighbors(state, one, two);
        if (ends.Length != 2)
        {
            return (null, null);
        }

        double? Mean(BoardLocation end) => Bearing(state, one, end) is { } a && Bearing(state, two, end) is { } b
            ? Math.Atan2(Math.Sin(a * Math.PI / 180) + Math.Sin(b * Math.PI / 180), Math.Cos(a * Math.PI / 180) + Math.Cos(b * Math.PI / 180)) * 180 / Math.PI
            : null;
        var face = FacingDegrees(facing);
        var ahead = ends.Where(end => Mean(end) is { } mean && AngleBetween(mean, face) < 1).ToArray();
        var behind = ends.Where(end => Mean(end) is { } mean && AngleBetween(mean, face + 180) < 1).ToArray();
        return ahead.Length == 1 && behind.Length == 1 ? (ahead[0], behind[0]) : (null, null);
    }

    /// <summary>Whether a hex is a woods or building obstacle a vehicle may Bypass (D2.3, D2.31): not rubble, and with no Blaze.</summary>
    private bool Bypassable(GameState state, BoardLocation hex) =>
        ReadLocation(state, hex) is { } read && TerrainKey(read) is { } key && (key == "woods" || IsBuildingTerrain(key))
        && !WrecksAt(state, hex).Any(wreck => IsBurning(state, wreck));

    /// <summary>
    /// A VBM step (D2.3, D2.31; ruling R11.2): along the hexside between two ADJACENT hexes, occupying <paramref name="obstacle"/>, one of them; the
    /// obstacle may not touch that hexside (the map read's hexside terrain is woods or a building), which may not carry a wall or hedge, nor already
    /// be Bypassed by another vehicle or wreck. The cost is twice the vehicle's Open Ground cost, with a level climbed doubled as well, the wreck
    /// and vehicle penalty when the obstacle is a new hex, and the Reverse multiplier.
    /// </summary>
    private (VehicleEntry? Entry, string? Reason) VehicleBypass(GameState state, UnitInstance vehicle, BoardLocation from, BoardLocation obstacle,
        BoardLocation other, bool reverse)
    {
        if (ScenarioA1VehicleTerrainCosts.TowingBypassBar(Towing(state, vehicle)) is { } towingBar)
        {
            return (null, towingBar);
        }

        if (!Bypassable(state, obstacle))
        {
            return (null, $"{obstacle} holds no woods or building a vehicle may Bypass, or it holds rubble or a Blaze (D2.3, D2.31)");
        }

        if (SideToward(state, obstacle, other) is not { } side || HexsideAt(state, obstacle, side) is not { } facts)
        {
            return (null, $"{obstacle} and {other} do not share a hexside the map reads");
        }

        var name = facts.Terrain?.Name;
        if (name is null || name == "Woods" || OrdinaryBuildings.Contains(name) || WallOn(facts) is not null)
        {
            return (null, $"the obstacle touches the hexside between {obstacle} and {other}, or a wall or hedge runs along it, so VBM may not use it (D2.3)");
        }

        if (state.Units.Any(unit => unit.Id != vehicle.Id && unit.Status is InstanceStatus.Active or InstanceStatus.Wrecked && LiveFire.IsVehicle(unit)
            && unit.Straddling is { } lane && state.Location(unit.Id)?.Location is { } at
            && ((at == obstacle && lane == other) || (at == other && lane == obstacle))))
        {
            return (null, "another vehicle or wreck already Bypasses along that hexside (D2.31)");
        }

        if (MovementTypeOf(vehicle) is not { } type || !VehicleTerrainHalfMp.TryGetValue((type, "open-ground"), out var open)
            || ReadLocation(state, from) is not { } fromRead || ReadLocation(state, obstacle) is not { } obstacleRead)
        {
            return (null, "the vehicle's Open Ground cost cannot be read");
        }

        var rise = obstacleRead.Hex.BaseLevel - fromRead.Hex.BaseLevel;
        if (Math.Abs(rise) >= 2)
        {
            return (null, "a VBM across an Abrupt Elevation Change is not reviewed (ruling R11.2)");
        }

        var cost = ScenarioA1VehicleTerrainCosts.BypassHalfMp(open, rise, HasSmoke(state, obstacle), state.Location(vehicle.Id)?.Location is { } now && now != obstacle,
            () => WreckEntryHalfMp(state, obstacle, false), reverse, MovementTypeOf(vehicle),
            VehicleWeatherHalfMp(state, type, "open-ground", false, false, false, rise));

        return (new VehicleEntry(cost, false, null, [], obstacle, other, "open-ground", false, reverse, null), null);
    }

    /// <summary>Whether a vehicle in Bypass has changed its VCA at its CAFP, so its facing runs along another hexside and it must move on (D2.33).</summary>
    private bool TurnedAtCafp(GameState state, UnitInstance vehicle) =>
        vehicle.Straddling is { } lane && state.Location(vehicle.Id)?.Location is { } at && vehicle.Position is MapPosition { Facing: { } facing }
        && Bearing(state, at, lane) is { } across && Math.Abs(AngleBetween(across, FacingDegrees(facing)) - 90) > 1;

    /// <summary>
    /// The entries a moving vehicle may make now (rulings R11.1, R11.2, R11.7): forward outright into a hex of its VCA, or from Bypass into the hex
    /// beyond its CAFP; forward in VBM along the hexside its VCA runs along (from its hex center, the hexside its VCA hexes share; after a VCA change at
    /// its CAFP, the hexside ahead); in Reverse outright into a rear hex, from Bypass the hex beyond its rear vertex; and in Reverse VBM from its hex
    /// center along the hexside its rear hexes share (D2.22). Each with its cost, or the reason it may not.
    /// </summary>
    internal IReadOnlyList<(VehicleEntry? Entry, BoardLocation To, BoardLocation? Straddling, string? Reason)> VehicleMoveOptions(GameState state, UnitInstance vehicle,
        bool reverse, bool allMp)
    {
        if (state.Location(vehicle.Id) is not { } at || vehicle.Position is not MapPosition { Facing: { } facing })
        {
            return [];
        }

        var here = at.Location;
        var map = new VehicleStepMap(this, state);
        var steps = ScenarioA1VehicleMovementCalculator.MoveOptions(map, map.Index(here), vehicle.Straddling is { } lane ? map.Index(lane) : null, (int)facing, reverse,
            () => TurnedAtCafp(state, vehicle));
        var options = new List<(VehicleEntry?, BoardLocation, BoardLocation?, string?)>();
        foreach (var step in steps)
        {
            var to = map.LocationOf(step.To);
            if (step.Other is { } other)
            {
                var (entry, reason) = VehicleBypass(state, vehicle, here, to, map.LocationOf(other), reverse);
                options.Add((entry, to, map.LocationOf(other), reason));
            }
            else
            {
                var (entry, reason) = VehicleOutright(state, vehicle, map.LocationOf(step.From), to, reverse, allMp);
                options.Add((entry is null ? null : entry with
                {
                    To = to
                }, to, null, reason));
            }
        }

        return options;
    }

    /// <summary>The map as the search of a vehicle's steps reads it: a table of Locations by index, the VCA hexes, shared neighbors, a hexside's ends, and Bypass obstacles.</summary>
    private sealed class VehicleStepMap(GamePlanner planner, GameState state) : IVehicleStepFactReader
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

        public IReadOnlyList<int> VcaHexes(int location, int facing) => [.. planner.VcaHexes(state, locations[location], (UnitFacing)facing).Select(Index)];

        public IReadOnlyList<int> SharedNeighbors(int one, int two) => [.. planner.SharedNeighbors(state, locations[one], locations[two]).Select(Index)];

        public (int? Front, int? Rear) LaneEnds(int one, int two, int facing)
        {
            var (front, rear) = planner.LaneEnds(state, locations[one], locations[two], (UnitFacing)facing);
            return (front is null ? null : Index(front), rear is null ? null : Index(rear));
        }

        public bool Bypassable(int location) => planner.Bypassable(state, locations[location]);
    }

    /// <summary>
    /// A VCA change's cost in half MP and its Bog DR (D2.11, B13.41; ruling R11.8): one MP per hexspine, two in (not Bypassing) a woods, building, or
    /// rubble hex off its road, with a Bog DR per hexspine where the terrain needs one to enter. In Bypass only at the CAFP, toward a hexside that
    /// meets there (D2.33).
    /// </summary>
    private (int HalfMp, int? BogDrm, IReadOnlyList<string> Causes) VehicleTurnCost(GameState state, UnitInstance vehicle, BoardLocation at)
    {
        if (vehicle.Straddling is not null || ReadLocation(state, at) is not { } read || TerrainKey(read) is not { } terrain
            || read.Hex.Hexsides.Any(side => side.Terrain?.IsRoad == true) || read.Hex.Center.Terrain?.IsRoad == true)
        {
            return (2, null, []);
        }

        return ScenarioA1VehicleTerrainCosts.TurnCost(terrain, () => VehicleBogDrm(state, vehicle));
    }

    /// <summary>
    /// A vehicle's Target Facing to a firer when it is in Bypass (D2.32; ruling R11.2): front when the firer is in the hex beyond its CAFP or that hex's
    /// VCA along the vehicle's facing, rear likewise from the hex beyond its rear vertex, otherwise side. Null when it is not in Bypass or cannot be read.
    /// </summary>
    internal string? BypassTargetFacing(GameState state, UnitInstance vehicle, BoardLocation firer)
    {
        if (vehicle.Straddling is not { } lane || state.Location(vehicle.Id)?.Location is not { } at || vehicle.Position is not MapPosition { Facing: { } facing }
            || LaneEnds(state, at, lane, facing) is not ({ } front, { } rear))
        {
            return null;
        }

        bool Within(BoardLocation apex, double direction) => (firer.Board == apex.Board && firer.Hex == apex.Hex)
            || (Bearing(state, apex, firer) is { } bearing && AngleBetween(bearing, direction) <= 30 + 1e-6);
        var face = FacingDegrees(facing);
        return Within(front, face) ? "front" : Within(rear, face + 180) ? "rear" : "side";
    }

    /// <summary>
    /// Whether a vehicle could destroy or Shock an enemy AFV in its Location with an Original TK DR of 5 (D2.6; ruling R11.6): its MA fires AP whose
    /// Final TK# at range 0 against the AFV's hull AF, from the Target Facing of the hexside the vehicle entered by (+1 at the rear), exceeds 5, or
    /// equals 5 against a turret.
    /// </summary>
    private bool CouldKillWithFive(GameState state, UnitInstance vehicle, UnitInstance afv, BoardLocation? came)
    {
        var ordnance = OrdnanceReference.Value;
        if (vehicle.Definition is not { } own || !ordnance.Guns.TryGetValue(own.Definition, out var gun) || gun.NoAp || Is(vehicle, Conditions.Malfunctioned)
            || afv.Definition is not { } target || !ordnance.Armor.Vehicles.TryGetValue(target.Definition, out var armor)
            || ordnance.Armor.BasicTk("ap", gun) is not { } basic || ordnance.Armor.CaseD("ap", gun, 0) is not { } caseD)
        {
            return false;
        }

        // The Target Facing of the hexside the vehicle entered by, from the AFV's VCA for its hull and its TCA for its turret (D3.2; referee, pass 11);
        // with no entry read, the side.
        static string Facing(double off) => ScenarioA1VehicleTerrainCosts.Facing(off);
        var hullFacing = "side";
        var turretFacing = "side";
        if (came is { } from && state.Location(afv.Id)?.Location is { } at && Bearing(state, at, from) is { } back && afv.Position is MapPosition { Facing: { } hull })
        {
            hullFacing = Facing(AngleBetween(back, FacingDegrees(hull)));
            turretFacing = LiveOrdnance.TurretFacing(state, afv) is { } tca ? Facing(AngleBetween(back, FacingDegrees(tca))) : hullFacing;
        }

        var hullAf = ScenarioA1ArmorReference.ArmorFactor(armor, "hull", hullFacing);
        var turretAf = armor.Turreted ? ScenarioA1ArmorReference.ArmorFactor(armor, "turret", turretFacing) : null;
        return ScenarioA1VehicleTerrainCosts.KillsWithFive(basic, caseD, hullAf, hullFacing, turretAf, turretFacing);
    }

    /// <summary>
    /// Why a vehicle may not Stop, end its move, or attempt ESB where it is (D2.6; ruling R11.6): an enemy AFV, Known or not, shares its Location and it
    /// could not destroy or Shock it there with an Original TK DR of 5. Null when it may.
    /// </summary>
    private string? EnemyAfvBar(GameState state, UnitInstance vehicle, IReadOnlyList<GameEvent>? history)
    {
        if (state.Location(vehicle.Id) is not { } at)
        {
            return null;
        }

        // Referee, pass 11: the hex the vehicle entered from this MPh, which the moving stack records.
        var came = state.Movement is { Vehicle: true } movement && movement.Members.Contains(vehicle.Id, StringComparer.Ordinal) ? movement.EnteredFrom : null;

        var blocking = state.At(at.Location).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side
            && IsAfv(unit) && !CouldKillWithFive(state, vehicle, unit, came));
        return blocking is null ? null
            : $"the enemy AFV {(VisibleTo(blocking, vehicle.Side) ? blocking.Id : "there")} shares {at.Location}, and {vehicle.Id} could not destroy or Shock it with an Original TK DR of 5, so it may not Stop, end its move, or attempt ESB there (D2.6)";
    }
}
