using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.ScenarioA1;
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
    // The Terrain Chart's MP Entrance Cost (p. 698) in half MP, by movement type: Fully Tracked, Halftrack, Truck (ruling R11.7).
    private static readonly Dictionary<(string MovementType, string Terrain), int> VehicleTerrainHalfMp = new()
    {
        [("fully-tracked", "open-ground")] = 2,
        [("half-tracked", "open-ground")] = 2,
        [("truck", "open-ground")] = 8,
        [("fully-tracked", "grain")] = 2,
        [("half-tracked", "grain")] = 2,
        [("truck", "grain")] = 10,
        [("fully-tracked", "brush")] = 4,
        [("half-tracked", "brush")] = 4,
        [("truck", "brush")] = 12,
    };

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

    private static bool Tracked(UnitInstance vehicle) => MovementTypeOf(vehicle) is "fully-tracked" or "half-tracked";

    /// <summary>Whether a vehicle is tracked, and so may attempt ESB (D2.5).</summary>
    public static bool IsTracked(UnitInstance vehicle) => Tracked(vehicle);

    /// <summary>D2.21 (ruling R11.1): Reverse costs four times the entry for a tracked vehicle, three times for a truck.</summary>
    private static int ReverseMultiplier(UnitInstance vehicle) => MovementTypeOf(vehicle) == "truck" ? 3 : 4;

    /// <summary>
    /// The Bog DRM of a vehicle (D8.21; ruling R11.9) that belong to it, whatever the terrain: +1 Normal Ground Pressure (none printed), +2 High, 0 Low;
    /// +1 towing a Gun; +1 not fully-tracked; +1 truck-type MP.
    /// </summary>
    private static List<(string Cause, int Drm)> VehicleBogDrm(GameState state, UnitInstance vehicle)
    {
        var definition = VehicleDefinition(vehicle);
        var drm = new List<(string, int)>();
        switch (definition?.GroundPressure)
        {
            case "low":
                break;
            case "high":
                drm.Add(("high-ground-pressure", 2));
                break;
            default:
                drm.Add(("normal-ground-pressure", 1));
                break;
        }

        if (state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id))
        {
            drm.Add(("towing", 1));
        }

        if (definition?.MovementType != "fully-tracked")
        {
            drm.Add(("not-fully-tracked", 1));
        }

        if (definition?.MovementType == "truck")
        {
            drm.Add(("truck-type-mp", 1));
        }

        return drm;
    }

    /// <summary>
    /// A vehicle's outright entry of an ADJACENT ground-level Location (rulings R11.6 to R11.9): the Terrain Chart cost of its movement type, a road
    /// hexside's rate (D2.16), ALL or half the allotment with a Bog DR in woods (B13.41, B13.42; <paramref name="allMp"/> chooses ALL for a
    /// fully-tracked vehicle), half the allotment with a Bog DR in rubble (B24.4), a wall or hedge (B9.4), 4 MP per level up (2 by road), the wreck
    /// and vehicle penalty (D2.14, doubled in woods, B13.41), SMOKE, and towing (C10.1). Null with the reason when it may not.
    /// </summary>
    private (VehicleEntry? Entry, string? Reason) VehicleOutright(GameState state, UnitInstance vehicle, BoardLocation from, BoardLocation to, bool reverse, bool allMp)
    {
        var (fromRead, toRead, adjacent, crossed) = Step(state, from, to);
        if (fromRead is null || toRead is null || !adjacent || crossed is null || MovementTypeOf(vehicle) is not { } type)
        {
            return (null, $"{to} is not an adjacent Location the map reads");
        }

        if (to.Level != 0 || from.Level != 0)
        {
            return (null, "vehicles stay at ground level (B23.4)");
        }

        if (crossed.Cliff || crossed.Slope)
        {
            return (null, "cliffs and Continuous Slopes are not reviewed for vehicles (ruling R11.7)");
        }

        if (TerrainKey(toRead) is not { } terrain)
        {
            return (null, $"{(toRead.Level.Terrain ?? toRead.Hex.Center.Terrain)?.Name ?? "that terrain"} is not a reviewed entry for vehicles (ruling R11.7)");
        }

        var wall = WallOn(crossed);
        if (wall == "other")
        {
            return (null, $"{crossed.HexsideTerrain!.Name} hexsides are not reviewed (ruling R11.7)");
        }

        var road = crossed.Terrain?.IsRoad == true && !IsRubbleTerrain(terrain);
        var rise = toRead.Hex.BaseLevel - fromRead.Hex.BaseLevel;

        // B10.52 (ruling R11.7): on a board whose elevations are hills, an Abrupt Elevation Change crosses two hillside Crest Lines, a Double-Crest
        // hexside, which a vehicle crosses only by road.
        if (Math.Abs(rise) >= 2 && !road)
        {
            return (null, "an Abrupt Elevation Change between hill levels is a Double-Crest hexside, crossed by a vehicle only along a road (B10.52; ruling R11.7)");
        }

        // B15.6: grain is Open Ground outside its season, April to September for MP.
        if (terrain == "grain")
        {
            if (state.ScenarioMonth is not { } month)
            {
                return (null, "grain's MP cost depends on the season, and the game names no scenario month (B15.6)");
            }

            terrain = month is >= 4 and <= 9 ? "grain" : "open-ground";
        }

        // E3.6 (backlog pass 16, ruling R16.12; referee, pass 16): in Mud a vehicle using an unpaved road pays the Open Ground COT, whatever the hex holds.
        var paved = road && crossed.Terrain?.Name == "Paved Road";
        var plowed = road && state.SpecialRules.Contains("plowed-roads", StringComparer.Ordinal);
        if (road && state.Weather("mud") && !paved)
        {
            road = false;
            terrain = "open-ground";
        }

        // B13.41, B13.42, B24.4: ALL and half the allotment are of the printed allotment (D1.1), whatever ESB added.
        var allotment = PrintedHalfMp(vehicle);
        var bog = new List<(string Cause, int Drm)>();
        var all = false;
        int cost;
        var woods = terrain == "woods";
        if (road)
        {
            // E3.724, E3.7331 (referee, pass 16): in Ground or Deep Snow a road entry costs at least one MP.
            cost = IsAfv(vehicle) && Is(vehicle, Conditions.ButtonedUp) || state.Weather("ground-snow") || state.Weather("deep-snow") ? 2 : 1;
        }
        else if (woods)
        {
            // B13.41, B13.42: ALL with a Bog DR, or for a fully-tracked vehicle half its allotment with a Bog DR at +3.
            if (type == "fully-tracked" && !allMp)
            {
                cost = allotment / 2;
                bog.Add(("woods-at-half-allotment", 3));
            }
            else
            {
                all = true;
                cost = allotment;
            }

            bog.Add(("woods", 0));
            if (rise > 0)
            {
                bog.Add(("gaining-elevation-into-woods", 1));
            }
        }
        else if (IsRubbleTerrain(terrain))
        {
            // B24.4: a fully-tracked vehicle only, at half its allotment, with a Bog DR at +3.
            if (type != "fully-tracked")
            {
                return (null, "only a fully-tracked vehicle enters rubble (B24.4)");
            }

            cost = allotment / 2;
            bog.Add(("rubble-at-half-allotment", 3));
        }
        else if (IsBuildingTerrain(terrain))
        {
            return (null, "a vehicle enters a building hex only by VBM here; the B23.41 entry of a fully-tracked AFV is not built (ruling R11.7)");
        }
        else if (!VehicleTerrainHalfMp.TryGetValue((type, terrain), out cost))
        {
            return (null, $"{terrain} is not allowed to a {type} vehicle (Terrain Chart; ruling R11.7)");
        }

        // Terrain Chart note H, B10.4: 4 MP more for the level climbed, 2 by a road hexside; B10.51 (referee, pass 11): across an Abrupt Elevation Change
        // each intermediate level climbed costs 4 MP more and each descended 2 MP.
        if (rise > 0 && !all)
        {
            cost += (road ? 4 : 8) + ((rise - 1) * 8);
        }
        else if (rise <= -2 && !all)
        {
            cost += (-rise - 1) * 4;
        }

        // B9.4: a wall or hedge hexside, not crossed by the road through a gap in it: fully-tracked 1 + COT; a halftrack a hedge only, 2 + COT with a
        // Bog DR in the hex it leaves; a truck neither.
        var bogInLeft = false;
        if (wall is not null && !road)
        {
            switch (type, wall)
            {
                case ("fully-tracked", _):
                    cost += all ? 0 : 2;
                    break;
                case ("half-tracked", "hedge"):
                    cost += all ? 0 : 4;
                    bogInLeft = true;
                    break;
                default:
                    return (null, $"a {type} vehicle may not cross a {wall} (Terrain Chart; B9.4)");
            }
        }

        // D2.14, B13.41, R6.4: one more MP per wreck or vehicle there, two by a road hexside at the road rate, doubled in woods off the road; SMOKE
        // and towing add one MP each (A24.7, C10.1).
        var penalty = WreckEntryHalfMp(state, to, road);
        if (woods && !road)
        {
            penalty += WreckEntryHalfMp(state, to, false) - (HasSmoke(state, to) ? 2 : 0);
        }

        var towing = state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id) ? 2 : 0;
        cost = all ? cost : cost + penalty + towing + VehicleWeatherHalfMp(state, type, terrain, road, paved, plowed, rise);
        if (reverse && !all)
        {
            cost *= ReverseMultiplier(vehicle);
        }

        int? bogDrm = null;
        var causes = new List<string>();
        var own = VehicleBogDrm(state, vehicle);

        // D8.21 notes 2 and 3 (backlog pass 16, rulings R16.12, R16.13; referee, pass 16): +1 on mud or snow-covered ground, +1 more with Deep Snow, not in
        // a building (a Bog DR is never made by road).
        if (terrain is not ("wooden-building" or "stone-building") && (state.Weather("mud") || state.Weather("ground-snow") || state.Weather("deep-snow")))
        {
            own.Add((state.Weather("mud") ? "mud" : "snow", 1));
            if (state.Weather("deep-snow"))
            {
                own.Add(("deep-snow", 1));
            }
        }
        if (bog.Count > 0 && !road)
        {
            var total = bog.Concat(own).ToArray();
            bogDrm = total.Sum(item => item.Drm);
            causes.AddRange(total.Select(item => item.Cause));
        }

        // B9.4 (referee, pass 11): a halftrack crossing a hedge into a Bog hex takes the hedge's Bog DR in the hex it leaves, then the hex's own.
        int? hedgeDrm = bogInLeft ? own.Sum(item => item.Drm) : null;
        if (bogInLeft)
        {
            causes.Insert(0, "hedge");
        }

        return (new VehicleEntry(cost, all, bogDrm, causes, to, null, terrain, road, reverse, hedgeDrm), null);
    }

    /// <summary>The two ADJACENT hexes both of two ADJACENT hexes border (the hexes at the ends of their shared hexside), ground level.</summary>
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

        var smoke = HasSmoke(state, obstacle) ? 2 : 0;
        var cost = 2 * (open + (rise > 0 ? rise * 8 : 0) + smoke);
        if (state.Location(vehicle.Id)?.Location is { } now && now != obstacle)
        {
            cost += WreckEntryHalfMp(state, obstacle, false) - smoke;
        }

        if (state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id))
        {
            cost += 2;
        }

        if (reverse)
        {
            cost *= ReverseMultiplier(vehicle);
        }

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
        var options = new List<(VehicleEntry?, BoardLocation, BoardLocation?, string?)>();
        void Outright(BoardLocation to, BoardLocation from)
        {
            var (entry, reason) = VehicleOutright(state, vehicle, from, to, reverse, allMp);
            options.Add((entry is null ? null : entry with
            {
                To = to
            }, to, null, reason));
        }

        void Lane(BoardLocation one, BoardLocation two)
        {
            foreach (var (obstacle, other) in new[] { (one, two), (two, one) })
            {
                if (Bypassable(state, obstacle))
                {
                    var (entry, reason) = VehicleBypass(state, vehicle, here, obstacle, other, reverse);
                    options.Add((entry, obstacle, other, reason));
                }
            }
        }

        var direction = reverse ? (UnitFacing)(((int)facing + 3) % 6) : facing;
        if (vehicle.Straddling is { } lane)
        {
            if (TurnedAtCafp(state, vehicle))
            {
                // D2.33: after a VCA change at its CAFP the vehicle goes on in Bypass along the hexside it now faces along.
                if (!reverse)
                {
                    foreach (var (one, two) in new[] { (here, lane) }.SelectMany(pair => SharedNeighbors(state, pair.Item1, pair.Item2)
                        .SelectMany(third => new[] { (pair.Item1, third), (pair.Item2, third) })))
                    {
                        if (LaneEnds(state, one, two, facing) is ({ } _, { } behind) && (behind == here || behind == lane))
                        {
                            Lane(one, two);
                        }
                    }
                }

                return options;
            }

            var (front, rear) = LaneEnds(state, here, lane, facing);
            if ((reverse ? rear : front) is { } beyond)
            {
                Outright(beyond, lane);
            }

            return options;
        }

        foreach (var to in VcaHexes(state, here, direction))
        {
            Outright(to, here);
        }

        if (VcaHexes(state, here, direction) is [{ } first, { } second])
        {
            Lane(first, second);
        }

        return options;
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

        var obstacle = terrain == "woods" || IsBuildingTerrain(terrain) || IsRubbleTerrain(terrain);
        if (!obstacle)
        {
            return (2, null, []);
        }

        var drm = VehicleBogDrm(state, vehicle);
        return terrain is "woods" || IsRubbleTerrain(terrain)
            ? (4, drm.Sum(item => item.Drm), [terrain, .. drm.Select(item => item.Cause)])
            : (4, null, []);
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
        static string Facing(double off) => off <= 60 + 1e-6 ? "front" : off <= 120 + 1e-6 ? "side" : "rear";
        var hullFacing = "side";
        var turretFacing = "side";
        if (came is { } from && state.Location(afv.Id)?.Location is { } at && Bearing(state, at, from) is { } back && afv.Position is MapPosition { Facing: { } hull })
        {
            hullFacing = Facing(AngleBetween(back, FacingDegrees(hull)));
            turretFacing = LiveOrdnance.TurretFacing(state, afv) is { } tca ? Facing(AngleBetween(back, FacingDegrees(tca))) : hullFacing;
        }

        var hullAf = ScenarioA1ArmorReference.ArmorFactor(armor, "hull", hullFacing);
        var turretAf = armor.Turreted ? ScenarioA1ArmorReference.ArmorFactor(armor, "turret", turretFacing) : null;
        return (hullAf is { } h && basic + caseD + (hullFacing == "rear" ? 1 : 0) - h > 5)
            || (turretAf is { } t && basic + caseD + (turretFacing == "rear" ? 1 : 0) - t >= 5);
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
