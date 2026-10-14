"""Pass 32.g commit 5: the charge search into Rules over IChargeFactReader; Play keeps the reader, the map reads, and the callers."""
import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
CC = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.CloseCombat.cs"
MV = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.Movement.cs"
C = "ScenarioA1ChargeCalculator"

t = CC.read_bytes().decode("utf-8")
assert "\r\n" not in t
start = t.index("    /// <summary>A node of a charge's route (ruling R27.2)")
end = t.index("    /// <summary>\n    /// The phasing side's berserk units that are to charge in this MPh")
old = t[start:end]
assert "private HashSet<ChargeMove>? ChargeFirstMoves" in old and "private static string LaneKey" in old and "private IEnumerable<BoardLocation> ChargeNeighbors" in old

# What stays of the old block: LaneKey, Lane, and ChargeNeighbors (map reads).
keep_start = old.index("    /// <summary>A Bypass lane's key: its hexsides, lowercase, comma-separated.</summary>")
keep_end = old.index("    /// <summary>\n    /// The moves out of a node of a charge's route")
kept = old[keep_start:keep_end]

new = '''    /// <summary>
    /// The charge search's fact reader (pass 32.g, S6): Locations as indexes into a table built as the search asks for them, hexsides as 0 to 5, the map
    /// reads, the entry and Bypass costs (for the movers), and the LOS and distances, all Play's; the search is Rules'.
    /// </summary>
    private sealed class ChargeScan : IChargeFactReader
    {
        private readonly GamePlanner planner;
        private readonly GameState state;
        private readonly UnitInstance[] movers;
        private readonly List<BoardLocation> locations = [];
        private readonly Dictionary<BoardLocation, int> indexes = [];

        public ChargeScan(GamePlanner planner, GameState state, UnitInstance[] movers)
        {
            this.planner = planner;
            this.state = state;
            this.movers = movers;
        }

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

        /// <summary>The Location at an index of the table.</summary>
        public BoardLocation At(int index) => locations[index];

        public string Name(int location) => locations[location].ToString();

        public int Level(int location) => locations[location].Level;

        public IEnumerable<int> Neighbors(int location) => planner.ChargeNeighbors(state, locations[location]).Select(Index);

        public bool Playable(int location) => planner.PlayableBar(state, locations[location]) is null;

        public int? EntryCost(int fromLocation, int toLocation) => planner.EntryCost(state, locations[fromLocation], locations[toLocation]);

        public int? Across(int location, int side) => planner.Across(state, locations[location], (HexsideDirection)side) is { } exit ? Index(exit) : null;

        public int? SideToward(int fromLocation, int toLocation) => planner.SideToward(state, locations[fromLocation], locations[toLocation]) is { } side ? (int)side : null;

        public bool Reads(int location) => planner.ReadLocation(state, locations[location]) is not null;

        /// <summary>A Bypass of an obstacle hex from a Location the map reads (A4.3, A4.31): the wall on the hexside entered counts, and the movers' entry decides.</summary>
        public int? BypassHalfMf(int obstacle, int side, IReadOnlyList<int> lane, int fromLocation)
        {
            var from = locations[fromLocation];
            var wall = planner.SideToward(state, from, locations[obstacle]) is { } toward ? WallOn(planner.HexsideAt(state, from, toward)) : null;
            return planner.BypassStep(state, movers, locations[obstacle], (HexsideDirection)side, planner.ReadLocation(state, from)!.Hex.BaseLevel, wall,
                [.. lane.Select(item => (HexsideDirection)item)]).Entry?.HalfMf;
        }

        public string LaneKey(IReadOnlyList<int> lane) => GamePlanner.LaneKey(lane.Select(item => (HexsideDirection)item));

        public IReadOnlyList<int> Lane(string key) => [.. GamePlanner.Lane(key).Select(item => (int)item)];

        public bool LosClear(int fromLocation, int toLocation) => planner.Los(state, locations[fromLocation], locations[toLocation]) is { Status: LosStatus.Clear };

        public int? Distance(int one, int two) => planner.HexDistance(state, locations[one], locations[two]);

        public ChargeOccupantFacts Occupants(int location)
        {
            var enemies = state.At(locations[location]).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != movers[0].Side).ToArray();
            return new ChargeOccupantFacts(enemies.Any(unit => planner.vocabulary.IsA(unit.Kind, "asl:crew")), enemies.Any(LiveFire.IsVehicle), enemies.Any(unit => !LiveFire.IsVehicle(unit)));
        }
    }

    /// <summary>
    /// The steps a berserk stack may take toward the nearest Known enemy unit in its LOS (A15.43, A15.431; ruling R27.2): the Known enemy units' Locations
    /// and the stack's start (its Location, or its Bypass lane, A4.32) are read here, Rules searches through a <see cref="ChargeScan"/>, and the steps come
    /// back by Location. <paramref name="current"/> is the stack's move this MPh, when the movers are in it.
    /// </summary>
    private (IReadOnlyDictionary<BoardLocation, ChargeStep> Steps, BoardLocation? Target, string? Undecided) ChargeSteps(GameState state,
        UnitInstance[] movers, BoardLocation from, MovementState? current)
    {
        var side = movers[0].Side;
        var scan = new ChargeScan(this, state, movers);

        // Table player, pass 27: an Abandoned vehicle is no unit to charge.
        int[] enemies = [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && ScenarioA1ChargeCalculator.ChargeTarget(KnownEnemy(unit), LiveFire.IsVehicle(unit), Is(unit, Conditions.Abandoned)))
            .Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>().Distinct().Select(scan.Index)];
        ChargeNodeFacts Start()
        {
            var at = scan.Index(from);
            return current is { Bypass: { Count: > 0 } lane } && current.Location == from && movers.All(unit => current.Movers.Contains(unit.Id, StringComparer.Ordinal))
                && BypassEntered(state, from, current.From, lane) is { } entered
                ? new ChargeNodeFacts(at, (int)entered, LaneKey(lane))
                : new ChargeNodeFacts(at, null, null);
        }

        var (steps, target, undecided) = ScenarioA1ChargeCalculator.Steps(scan, scan.Index(from), enemies, current?.Charge is { } previous ? scan.Index(previous) : null, Start);
        return (steps.ToDictionary(item => scan.At(item.Key), item => new ChargeStep(scan.At(item.Value.Target), item.Value.HalfMf, item.Value.Plain, item.Value.Lanes)),
            target is { } found ? scan.At(found) : null, undecided);
    }

'''
step_record = old[:keep_start]
assert step_record.startswith("    /// <summary>A node of a charge's route")
# The ChargeStep record (callers use it) sits above the node record in the old block: keep it, drop ChargeNode and ChargeMove.
head = t[:start]
record_start = head.rindex("    /// <summary>A step of a berserk charge (A15.431; ruling R27.2)")
assert "private sealed record ChargeStep(" in head[record_start:]
t = head + kept + new + t[end:]

edits = [
    (
        "            && charge.Steps.Values.Any(step => (allowance * 2) - (unit.MfSpent * 2) - (unit.HalfMfSpent ? 1 : 0) >= step.HalfMf))];\n",
        f"            && charge.Steps.Values.Any(step => {C}.Affordable(allowance, unit.MfSpent, unit.HalfMfSpent, step.HalfMf)))];\n",
    ),
    (
        "    private static string LaneKey(IEnumerable<HexsideDirection> lane)",
        "    internal static string LaneKey(IEnumerable<HexsideDirection> lane)",
    ),
    (
        "    private static List<HexsideDirection> Lane(string key)",
        "    internal static List<HexsideDirection> Lane(string key)",
    ),
]
for o, n in edits:
    assert t.count(o) == 1, o[:80]
    t = t.replace(o, n)
CC.write_bytes(t.encode("utf-8"))
print("applied", CC.name)

m = MV.read_bytes().decode("utf-8")
o = "            var left = MfAllotment(state, unit, unit.DoubleTimeMf, Is(unit, Conditions.Cx)) is { } allowance ? (allowance * 2) - (unit.MfSpent * 2) - (unit.HalfMfSpent ? 1 : 0) : 0;\n"
n = f"            var left = {C}.HalfMfLeft(MfAllotment(state, unit, unit.DoubleTimeMf, Is(unit, Conditions.Cx)), unit.MfSpent, unit.HalfMfSpent);\n"
assert m.count(o) == 1
MV.write_bytes(m.replace(o, n).encode("utf-8"))
print("applied", MV.name)
