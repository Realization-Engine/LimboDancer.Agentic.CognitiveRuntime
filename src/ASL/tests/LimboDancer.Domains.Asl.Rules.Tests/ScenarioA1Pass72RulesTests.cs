using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 72, Infantry V: Heat of Battle and Unit Substitution. Increment 5a: the berserk charge (A15.43 to A15.45, A15.44, A15.46, E1.533; rulings R72.3 to
/// R72.6), read in Rules through its public members: one search for every target, the next nearest past a rule bar, A15.45's comparison, A15.44's verdict,
/// a surviving vehicle in A15.46, and the mid-move MF.
/// </summary>
public sealed class ScenarioA1Pass72RulesTests
{
    private const string Cliff = "play.move-cliff: a cliff hexside is crossed only by Climbing, which is not built (B11)";
    private const string Water = "play.move-terrain: Water is not a reviewed entry (ruling R10.1)";
    private const string Unreviewed = "play.move-terrain: Shallow Stream is not a reviewed entry (ruling R10.1)";
    private static readonly ScenarioA1CloseCombatReference CloseCombat = new ScenarioA1CloseCombatPackage().Reference;
    private static readonly string[] Russian = ["russian"];

    /// <summary>
    /// A graph of ground-level Locations: the charging unit in 0, each step both ways at one MF unless a step is refused one way, the range in hexes from 0 as a
    /// test gives it (1 for any other Location), every other pair of different Locations one hex apart, and every LOS clear.
    /// </summary>
    private sealed class Graph : IChargeFactReader
    {
        private readonly Dictionary<int, Dictionary<int, ChargeEntryFacts>> edges = [];
        private readonly Dictionary<int, int> ranges = [];

        public Graph Step(int one, int two)
        {
            Add(one, two, new ChargeEntryFacts(2, null, null));
            Add(two, one, new ChargeEntryFacts(2, null, null));
            return this;
        }

        /// <summary>A chain of steps through the Locations given, in order.</summary>
        public Graph Path(params int[] locations)
        {
            for (var index = 1; index < locations.Length; index++)
            {
                Step(locations[index - 1], locations[index]);
            }

            return this;
        }

        public Graph Refuse(int from, int to, string refusal, string? terrain = null)
        {
            Add(from, to, new ChargeEntryFacts(null, refusal, terrain));
            return this;
        }

        public Graph Range(int location, int hexes)
        {
            ranges[location] = hexes;
            return this;
        }

        private void Add(int from, int to, ChargeEntryFacts entry)
        {
            if (!edges.TryGetValue(from, out var leaving))
            {
                edges[from] = leaving = [];
            }

            leaving[to] = entry;
        }

        public string Name(int location) => "n" + location.ToString(System.Globalization.CultureInfo.InvariantCulture);

        public int Level(int location) => 0;

        public IEnumerable<int> Neighbors(int location) => edges.TryGetValue(location, out var leaving) ? leaving.Keys : [];

        public bool Playable(int location) => true;

        public ChargeEntryFacts EntryCost(int fromLocation, int toLocation) => edges[fromLocation][toLocation];

        public int? Across(int location, int side) => null;

        public int? SideToward(int fromLocation, int toLocation) => null;

        public bool Reads(int location) => false;

        public int? BypassHalfMf(int obstacle, int side, IReadOnlyList<int> lane, int fromLocation) => null;

        public string LaneKey(IReadOnlyList<int> lane) => string.Join(",", lane);

        public IReadOnlyList<int> Lane(string key) => [.. key.Split(',').Select(int.Parse)];

        public bool LosClear(int fromLocation, int toLocation) => true;

        public int? Distance(int one, int two) => one == two ? 0 : one == 0 ? ranges.GetValueOrDefault(two, 1) : two == 0 ? ranges.GetValueOrDefault(one, 1) : 1;
    }

    private static (IReadOnlyDictionary<int, ChargeSearchStep> Steps, int? Target, string? Undecided) Charge(Graph graph, params int[] enemies) =>
        ScenarioA1ChargeCalculator.Steps(graph, 0, enemies, null, () => new ChargeNodeFacts(0, null, null));

    [Fact]
    public void TheSingleSearchChargesTheNearestAndEveryEquidistantTargetAsBefore()
    {
        // A15.43 (p. 84; ruling R72.3): the nearest in hexes, by the shortest route in MF; equidistant targets are the ATTACKER's choice, so both first steps.
        var graph = new Graph().Path(0, 1, 2).Path(0, 3, 4).Path(0, 5, 6, 7).Range(2, 2).Range(4, 2).Range(7, 3);
        var (steps, target, undecided) = Charge(graph, 2, 4, 7);
        Assert.Null(undecided);
        Assert.Null(target);
        Assert.Equal([1, 3], steps.Keys.Order());
        Assert.Equal((2, 2, true), (steps[1].Target, steps[1].HalfMf, steps[1].Plain));
        Assert.Equal(4, steps[3].Target);

        // One nearest: its own first step only, and the farther units are never charged.
        var (one, oneTarget, _) = Charge(graph, 2, 7);
        Assert.Equal(2, oneTarget);
        Assert.Equal([1], one.Keys);
    }

    [Theory]
    [InlineData(Cliff, null)]
    [InlineData(Water, "Water")]
    [InlineData("play.move-terrain: River is not a reviewed entry (ruling R10.1)", "River")]
    public void PastACliffOrAWaterObstacleTheChargeTakesTheNextNearest(string refusal, string? terrain)
    {
        // A15.45 (p. 84): "it will charge the next nearest Known enemy unit instead"; the nearest, one hex off, is behind the bar with no other route.
        var graph = new Graph().Refuse(0, 1, refusal, terrain).Range(1, 1).Path(0, 2, 3).Range(3, 2);
        var (steps, target, undecided) = Charge(graph, 1, 3);
        Assert.Null(undecided);
        Assert.Equal(3, target);
        Assert.Equal([2], steps.Keys);
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    public void PastABarAnAlternateRouteStaysOnTheNearestWhileNoLongerThanTheNextNearestsRange(int nextRange, int charged)
    {
        // A15.45 (ruling R72.3): the nearest (n1, one hex off) is behind a cliff; round it the route steps three hexes. The next nearest (n4) is nextRange hexes
        // off: at 2 the alternate route is longer and the charge moves on; at 3 (a tie) and 4 it stays.
        var graph = new Graph().Refuse(0, 1, Cliff).Range(1, 1).Path(0, 2, 3, 1).Range(4, nextRange);
        int[] chain = [0, .. Enumerable.Range(10, nextRange - 1), 4];
        graph.Path(chain);
        var (steps, target, undecided) = Charge(graph, 1, 4);
        Assert.Null(undecided);
        Assert.Equal(charged, target);
        Assert.Equal([charged == 1 ? 2 : 10], steps.Keys);
    }

    [Fact]
    public void WithOnlyRuleBarsBetweenItAndEveryTargetTheChargeIsUnreachableAndHardeningIsDue()
    {
        // A15.45, A15.44, E1.533 (pp. 84, 224; ruling R72.4): no Known enemy unit can be reached but across a cliff and a Water Obstacle.
        var graph = new Graph().Refuse(0, 1, Cliff).Range(1, 1).Path(0, 2).Refuse(2, 3, Water, "Water").Range(3, 2);
        var (steps, target, undecided) = Charge(graph, 1, 3);
        Assert.Empty(steps);
        Assert.Equal(1, target);
        Assert.StartsWith(ScenarioA1ChargeCalculator.UnreachableCode + ": from n0 no route reaches a Known enemy unit in LOS but across a cliff or a Water Obstacle", undecided,
            StringComparison.Ordinal);
        Assert.True(ScenarioA1ChargeCalculator.NoneReachable(undecided));
        Assert.True(ScenarioA1ChargeCalculator.HardeningDue("mph", true, true, true, false, true, () => undecided));
        Assert.False(ScenarioA1ChargeCalculator.HardeningDue("mph", true, true, true, true, true, () => undecided));
        Assert.False(ScenarioA1ChargeCalculator.HardeningDue("dfph", true, true, true, false, true, () => undecided));
    }

    [Fact]
    public void AGapOfTheModelOnTheFrontierKeepsRulingR305()
    {
        // Ruling R72.4: a refusal the model does not review (ruling R10.1) may hide a route, so the charge ends in place and the unit stays berserk.
        var graph = new Graph().Refuse(0, 1, Cliff).Range(1, 1).Path(0, 2).Refuse(2, 3, Unreviewed, "Shallow Stream").Range(3, 2);
        var (steps, target, undecided) = Charge(graph, 1, 3);
        Assert.Empty(steps);
        Assert.Equal(1, target);
        Assert.Equal("play.charge-no-route: no route the game allows leads from n0 to n1; the charge ends in place (A15.431)", undecided);
        Assert.False(ScenarioA1ChargeCalculator.NoneReachable(undecided));
        Assert.False(ScenarioA1ChargeCalculator.HardeningDue("mph", true, true, true, false, true, () => undecided));
    }

    [Fact]
    public void TheRuleBarsAreACliffAWaterObstacleAndUnconnectedLevels()
    {
        // A15.45, B21.1 (pp. 84, 134), B23.4: a stream is no Water Obstacle (B20), and terrain "not reviewed" is a gap.
        Assert.True(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(null, Cliff, "Open Ground")));
        Assert.True(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(null, Water, "Water")));
        Assert.True(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(null, "play.move-terrain: Canal is not a reviewed entry (ruling R10.1)", "Canal")));
        Assert.True(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(null, "play.move-stairwell: bd01:D5 has no stairwell, so its levels do not connect (B23.23, B23.4)", null)));
        Assert.False(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(null, Unreviewed, "Shallow Stream")));
        Assert.False(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(null, null, null)));
        Assert.False(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(2, null, "Water")));
    }

    [Fact]
    public void ASingleSearchFallsBackWithoutSearchingAgain()
    {
        // Ruling R72.3: the fall back reads the one search; every Location is entered by the search at most once from each neighbour.
        var counting = new CountingGraph(new Graph().Refuse(0, 1, Cliff).Range(1, 1).Path(0, 2, 3).Range(3, 2));
        ScenarioA1ChargeCalculator.Steps(counting, 0, [1, 3], null, () => new ChargeNodeFacts(0, null, null));
        Assert.Equal(1, counting.FromStart);
    }

    /// <summary>A graph that counts how often the search leaves the charging unit's Location.</summary>
    private sealed class CountingGraph(Graph inner) : IChargeFactReader
    {
        public int FromStart
        {
            get; private set;
        }

        public string Name(int location) => inner.Name(location);

        public int Level(int location) => inner.Level(location);

        public IEnumerable<int> Neighbors(int location)
        {
            if (location == 0)
            {
                FromStart++;
            }

            return inner.Neighbors(location);
        }

        public bool Playable(int location) => inner.Playable(location);

        public ChargeEntryFacts EntryCost(int fromLocation, int toLocation) => inner.EntryCost(fromLocation, toLocation);

        public int? Across(int location, int side) => inner.Across(location, side);

        public int? SideToward(int fromLocation, int toLocation) => inner.SideToward(fromLocation, toLocation);

        public bool Reads(int location) => inner.Reads(location);

        public int? BypassHalfMf(int obstacle, int side, IReadOnlyList<int> lane, int fromLocation) => inner.BypassHalfMf(obstacle, side, lane, fromLocation);

        public string LaneKey(IReadOnlyList<int> lane) => inner.LaneKey(lane);

        public IReadOnlyList<int> Lane(string key) => inner.Lane(key);

        public bool LosClear(int fromLocation, int toLocation) => inner.LosClear(fromLocation, toLocation);

        public int? Distance(int one, int two) => inner.Distance(one, two);
    }

    [Fact]
    public void BattleHardeningMakesTheNextHigherQualityOrAnEliteUnitFanatic()
    {
        // A15.3 (p. 83): the next higher quality; an already elite MMC becomes Fanatic, once.
        Assert.Equal(("attacker-elite-squad", false), ScenarioA1ChargeCalculator.Hardening("attacker-squad", false));
        Assert.Equal(((string?)null, true), ScenarioA1ChargeCalculator.Hardening("attacker-elite-squad", false));
        Assert.Equal(((string?)null, false), ScenarioA1ChargeCalculator.Hardening("attacker-elite-squad", true));
        Assert.StartsWith("play.berserk-hardening: g1 loses berserk status and its owner refuses Battle Hardening",
            ScenarioA1ChargeCalculator.HardeningText("g1", false, false, false), StringComparison.Ordinal);
    }

    private static CloseCombatUnit Unit(string id, string definition, string side) =>
        new(id, definition, side, false, false, false, false, false, false, false, false, false, false, false);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ABerserkUnitStaysBerserkWhileAKnownEnemyVehicleSurvivesInItsLocation(bool vehicle)
    {
        // A15.46 (p. 84; ruling R72.5): "eliminates all ... Known enemy units in its Location": the German squad kills the Russian HS beside the Russian tank,
        // which the sequential round's units leave out; the squad stays berserk while the tank is there.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { Berserk = true }, Unit("r1", "defender-half-squad", "russian")];
        var facts = new CloseCombatFacts("CCPh", "bd01:G5:0", "open-ground", "german", CloseCombatFacts.Simultaneous, null, units, [], [],
            [new CloseCombatDeclaration(["g1"], ["r1"])], new CloseCombatRolls(new Dictionary<string, IReadOnlyList<int>> { ["0"] = [2, 3] }))
        {
            VehicleSides = vehicle ? Russian : null,
        };
        var result = ScenarioA1CloseCombatCalculator.Resolve(facts, CloseCombat);
        Assert.True(result.Effects.Single(item => item.UnitId == "r1").Eliminated);
        Assert.Equal(vehicle ? (bool?)null : true, result.Effects.Single(item => item.UnitId == "g1").BerserkEnded);
    }

    [Fact]
    public void AUnitMadeBerserkInMidMoveHasEightMfLessThoseSpentAndAbandonsBeyondItsIpc()
    {
        // A15.431 (p. 84; ruling R72.6): with four 1PP SW a squad has three MF (A4.42); berserk, it has eight, since the fourth is abandoned at once.
        var squad = new MfAllotmentFacts(false, null, false, false, false);
        int[] fourLight = [1, 1, 1, 1];
        Assert.Equal(3, ScenarioA1MovementCalculator.MfAllotment(squad, 0, false, 0, 0, () => 4, () => fourLight));
        Assert.Equal(8, ScenarioA1MovementCalculator.MfAllotment(squad with
        {
            Berserk = true
        }, 0, false, 0, 0, () => 8, () => fourLight));
        Assert.Equal(14, ScenarioA1ChargeCalculator.HalfMfLeft(8, 1, false));

        // The abandonment is read at every step now, so a squad made berserk in mid move drops its MMG and the 1PP SW beyond its IPC with its next step.
        var (refusal, abandoned) = ScenarioA1MovementCalculator.BerserkAbandons(
            [new BerserkAbandonFacts("g1", false, [new("m1", 1), new("m2", 1), new("m3", 1), new("m4", 1), new("mmg", 4)])], []);
        Assert.Null(refusal);
        Assert.Equal(["mmg", "m4"], abandoned);
    }
}
