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
        private readonly HashSet<int> hidden = [];

        /// <summary>A Location the charging unit has no LOS to.</summary>
        public Graph Hidden(int location)
        {
            hidden.Add(location);
            return this;
        }

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

        public bool LosClear(int fromLocation, int toLocation) => !hidden.Contains(toLocation);

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

    /// <summary>The nearest (n1, one hex off) behind <paramref name="refusal"/>; round it the route steps three hexes (n2, n3). The next nearest (n4) is
    /// <paramref name="nextRange"/> hexes off by a chain of steps from n10.</summary>
    private static Graph RoundTheNearest(string refusal, int nextRange)
    {
        var graph = new Graph().Refuse(0, 1, refusal).Range(1, 1).Path(0, 2, 3, 1).Range(4, nextRange);
        int[] chain = [0, .. Enumerable.Range(10, nextRange - 1), 4];
        return graph.Path(chain);
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(4, 1)]
    public void PastABarAnAlternateRouteStaysOnTheNearestWhileStillCloserThanTheNextNearest(int nextRange, int charged)
    {
        // A15.45 (rulings R72.3, R72.17): the cliff lies across n1's route (ignoring it, n1 is one MF off, not three). The next nearest (n4) is nextRange hexes
        // off: at 2 the alternate route is longer and the charge moves on; at 4 it is still closer and stays.
        var (steps, target, undecided) = Charge(RoundTheNearest(Cliff, nextRange), 1, 4);
        Assert.Null(undecided);
        Assert.Equal(charged, target);
        Assert.Equal([charged == 1 ? 2 : 10], steps.Keys);
    }

    [Fact]
    public void AtATieTheAttackerChoosesAmongBothGroupsFromOneSearch()
    {
        // A15.45, A15.43 (ruling R72.17): the alternate route to n1 steps three hexes and n4 is three off: "still closer" fails, the targets are equidistant, so
        // the ATTACKER may take the first step toward either. The search that ignores bars reads the moves the search read: the start is left once.
        var counting = new CountingGraph(RoundTheNearest(Cliff, 3));
        var (steps, target, undecided) = ScenarioA1ChargeCalculator.Steps(counting, 0, [1, 4], null, () => new ChargeNodeFacts(0, null, null));
        Assert.Null(undecided);
        Assert.Null(target);
        Assert.Equal([2, 10], steps.Keys.Order());
        Assert.Equal((1, 4), (steps[2].Target, steps[10].Target));
        Assert.Equal(1, counting.FromStart);
    }

    [Fact]
    public void AnOrdinaryChargePastAMultiLevelBuildingKeepsTheNearest()
    {
        // Ruling R72.17: Locations a building does not connect are ordinary movement, no bar; the shortest route round them is A15.43's, whatever the next
        // nearest's range (two hexes against the route's three).
        var (steps, target, undecided) = Charge(RoundTheNearest("play.move-stairwell: bd01:D5 has no stairwell, so its levels do not connect (B23.23, B23.4)", 2), 1, 4);
        Assert.Null(undecided);
        Assert.Equal(1, target);
        Assert.Equal([2], steps.Keys);
    }

    [Fact]
    public void WithTheNextNearestUnreachableTheChargeKeepsTheNearest()
    {
        // Ruling R72.17: the comparison is with the next group that has a reachable target; n4 (two hexes off) is across Water, so the charge stays on n1.
        var graph = new Graph().Refuse(0, 1, Cliff).Range(1, 1).Path(0, 2, 3, 1).Refuse(0, 4, Water, "Water").Range(4, 2);
        var (steps, target, undecided) = Charge(graph, 1, 4);
        Assert.Null(undecided);
        Assert.Equal(1, target);
        Assert.Equal([2], steps.Keys);
    }

    [Fact]
    public void ATargetCutOffByAnotherEnemyLocationKeepsRulingR305()
    {
        // Ruling R72.17: n1 (in LOS, behind a cliff) is reached only through n3, the Location of a Known enemy unit out of LOS, which the charge enters and
        // never passes; a Known enemy Location is reached, so no Battle Hardening.
        var graph = new Graph().Refuse(0, 1, Cliff).Range(1, 1).Path(0, 2, 3, 1).Hidden(3);
        var (steps, target, undecided) = Charge(graph, 1, 3);
        Assert.Empty(steps);
        Assert.Equal(1, target);
        Assert.Equal("play.charge-no-route: the game allows a route to no Known enemy unit in its LOS (the nearest n1), so the charge ends in place (ruling R30.5)", undecided);
        Assert.False(ScenarioA1ChargeCalculator.NoneReachable(undecided));
    }

    [Fact]
    public void WithNoKnownEnemyInLosTheChargeIsNeverHardened()
    {
        // A15.431, A15.44 (ruling R72.17): the charge kept to n1, now out of LOS and across Water; the unit returns to normal, so no Battle Hardening is due.
        var graph = new Graph().Refuse(0, 1, Water, "Water").Range(1, 1).Path(0, 2).Hidden(1);
        var (steps, target, undecided) = ScenarioA1ChargeCalculator.Steps(graph, 0, [1], 1, () => new ChargeNodeFacts(0, null, null));
        Assert.Empty(steps);
        Assert.Equal(1, target);
        Assert.False(ScenarioA1ChargeCalculator.NoneReachable(undecided));
        Assert.False(ScenarioA1ChargeCalculator.HardeningDue("mph", true, true, true, false, true, () => undecided));
    }

    [Fact]
    public void WithOnlyRuleBarsBetweenItAndEveryTargetTheChargeIsUnreachableAndHardeningIsDue()
    {
        // A15.45, A15.44, E1.533 (pp. 84, 224; ruling R72.4): no Known enemy unit can be reached but across a cliff and a Water Obstacle.
        var graph = new Graph().Refuse(0, 1, Cliff).Range(1, 1).Path(0, 2).Refuse(2, 3, Water, "Water").Range(3, 2);
        var (steps, target, undecided) = Charge(graph, 1, 3);
        Assert.Empty(steps);
        Assert.Equal(1, target);
        Assert.StartsWith(ScenarioA1ChargeCalculator.UnreachableCode + ": no route reaches a Known enemy unit in its LOS except across a cliff or a Water Obstacle;", undecided,
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
        Assert.Equal("play.charge-no-route: the game allows a route to no Known enemy unit in its LOS (the nearest n1), so the charge ends in place (ruling R30.5)", undecided);
        Assert.False(ScenarioA1ChargeCalculator.NoneReachable(undecided));
        Assert.False(ScenarioA1ChargeCalculator.HardeningDue("mph", true, true, true, false, true, () => undecided));
    }

    [Fact]
    public void TheRuleBarsAreACliffAndAnUnbridgedWaterObstacle()
    {
        // A15.45, B21.1 (pp. 84, 134; ruling R72.17): a stream is no Water Obstacle (B20), terrain "not reviewed" is a gap, a building connection is ordinary
        // movement (B23.4), and a bridged hex is not barred.
        Assert.True(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(null, Cliff, "Open Ground")));
        Assert.True(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(null, Water, "Water")));
        Assert.True(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(null, "play.move-terrain: Canal is not a reviewed entry (ruling R10.1)", "Canal")));
        Assert.False(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(null, Water, "Water") { Bridged = true }));
        Assert.False(ScenarioA1ChargeCalculator.RuleBar(new ChargeEntryFacts(null, "play.move-stairwell: bd01:D5 has no stairwell, so its levels do not connect (B23.23, B23.4)", null)));
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
        Assert.StartsWith("play.berserk-hardening: g1 in bd01:A8:0 loses berserk status and its owner refuses Battle Hardening",
            ScenarioA1ChargeCalculator.HardeningText("g1", "bd01:A8:0", false, null, false), StringComparison.Ordinal);
        Assert.StartsWith("play.berserk-hardening: g1 in bd01:A8:0 loses berserk status and is Battle Hardened: it is Replaced by an unbroken attacker-elite-squad",
            ScenarioA1ChargeCalculator.HardeningText("g1", "bd01:A8:0", true, "attacker-elite-squad", false), StringComparison.Ordinal);
        Assert.Contains("it becomes Fanatic", ScenarioA1ChargeCalculator.HardeningText("g1", "bd01:A8:0", true, null, true), StringComparison.Ordinal);
        Assert.StartsWith("[A8]: g1 is berserk and can reach no Known enemy unit in its LOS", ScenarioA1ChargeCalculator.HardeningChoiceText("[A8]", "g1"), StringComparison.Ordinal);
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

    private static readonly string[] BerserkPair = ["g1", "g2"];

    // Increment 5b: the return to normal after fire in the AFPh and after a vehicle destroyed in CC (A15.46; ruling R72.7).

    [Theory]
    // A15.46 (p. 84): the group's halved TPBF eliminates the one Known enemy unit in its Location: both berserk firers return to normal.
    [InlineData("afph", true, false, 1, 0, true)]
    // One Known enemy unit is left: still berserk.
    [InlineData("afph", true, false, 2, 1, false)]
    // A FT at an ADJACENT hex fires from another Location: no return. A FT at its own Location (A22.1: "never increased for PBF/TPBF") is the first row's case.
    [InlineData("afph", false, false, 1, 0, false)]
    // A concealed enemy unit is not Known: Play counts only the Known, so one left is no count at all (the Play test of a concealed HS left).
    // "but at least one": a Location with no Known enemy unit to eliminate gives no return.
    [InlineData("afph", true, false, 0, 0, false)]
    // Not the AFPh: TPBF in the DFPh, or the PFPh, gives no return.
    [InlineData("dfph", true, false, 1, 0, false)]
    // A CC attack that destroys the enemy vehicle there, no other Known enemy unit left: the berserk attackers return to normal; Infantry left, not.
    [InlineData("ccph", false, true, 1, 0, true)]
    [InlineData("ccph", false, true, 2, 1, false)]
    public void ABerserkGroupReturnsToNormalWhenItsAttackClearsItsLocation(string phase, bool ownLocation, bool vehicleDestroyed, int before, int after, bool returns)
    {
        var facts = new BerserkAttackFacts(phase, ownLocation, vehicleDestroyed, BerserkPair);
        Assert.Equal(returns ? BerserkPair : [], ScenarioA1SequenceCalculator.BerserkReturnsAfterAttack(facts, () => new KnownEnemyCount(before, after)));
    }

    [Fact]
    public void TheLocationIsReadOnlyForAnAttackThatCanReturnABerserkUnit()
    {
        // A15.46: no berserk attacker, fire outside the AFPh, or fire from another Location: the Known enemy units are never read.
        static KnownEnemyCount Unread() => throw new InvalidOperationException("The Location was read.");
        Assert.Empty(ScenarioA1SequenceCalculator.BerserkReturnsAfterAttack(new BerserkAttackFacts("afph", true, false, []), Unread));
        Assert.Empty(ScenarioA1SequenceCalculator.BerserkReturnsAfterAttack(new BerserkAttackFacts("pfph", true, false, BerserkPair), Unread));
        Assert.Empty(ScenarioA1SequenceCalculator.BerserkReturnsAfterAttack(new BerserkAttackFacts("afph", false, false, BerserkPair), Unread));
        Assert.Equal("4-4-7 squad R1 in [G5] eliminated every Known enemy unit in its Location by destroying the enemy vehicle in CC, so it is no longer berserk and "
            + "returns to Good Order (A15.46)", ScenarioA1SequenceCalculator.BerserkClearedText("4-4-7 squad R1 in [G5]", true));
    }

    // Increment 5c: the SSR tokens, A19.132's SSR ELR for underscored units, and A19.32's MG B# in Inexperienced hands (rulings R72.9, R72.10, R72.13, R72.14).

    private static readonly ScenarioA1FireReference Fire = new ScenarioA1FirePackage().Reference;
    private static readonly string[] Shortage = ["ammo-shortage:german"];
    private static readonly string[] SideElr = ["underscored-elr:german:3"];
    private static readonly string[] GroupElr = ["underscored-elr:german:2", "underscored-elr:german:german-2:1"];
    private static readonly string[] MalformedElr = ["underscored-elr:german:5"];
    private static readonly string[] MalformedShortage = ["ammo-shortage:"];
    private static readonly string[] TokenStatus = ["token"];
    private const string Engineer = "attacker-elite-squad-8-3-8";
    private const string FireFrom = "bd01:F5:0";
    private const string FireAt = "bd01:G5:0";

    [Fact]
    public void TheUnitSubstitutionTokensAreParsedAndAMalformedOneIsRefused()
    {
        // A19.131, A19.132 (p. 86; ruling R72.10): ammo-shortage:<side>, and underscored-elr:<side>[:<group>]:<n> with n from 0 to 4 ("an ELR ≤ 4").
        Assert.Equal("german", ScenarioA1UnitSubstitution.AmmunitionShortageToken("ammo-shortage:german"));
        Assert.Null(ScenarioA1UnitSubstitution.AmmunitionShortageToken("ammo-shortage:"));
        Assert.Null(ScenarioA1UnitSubstitution.AmmunitionShortageToken("ammo-shortage:german:x"));
        Assert.Equal<(string, string?, int)?>(("german", null, 3), ScenarioA1UnitSubstitution.UnderscoredElrToken("underscored-elr:german:3"));
        Assert.Equal<(string, string?, int)?>(("russian", "russian-2", 0), ScenarioA1UnitSubstitution.UnderscoredElrToken("underscored-elr:russian:russian-2:0"));
        Assert.Null(ScenarioA1UnitSubstitution.UnderscoredElrToken("underscored-elr:german:5"));
        Assert.Null(ScenarioA1UnitSubstitution.UnderscoredElrToken("underscored-elr:german"));
        Assert.Null(ScenarioA1UnitSubstitution.UnderscoredElrToken("underscored-elr:german:-1"));

        // The readers: Ammunition Shortage by side; the SSR ELR only for a MMC with an underscored Morale Factor, its group's token before its side's.
        Assert.True(ScenarioA1UnitSubstitution.AmmunitionShortage(Shortage, "german"));
        Assert.False(ScenarioA1UnitSubstitution.AmmunitionShortage(Shortage, "russian"));
        Assert.Equal(3, ScenarioA1UnitSubstitution.UnderscoredElr(SideElr, "german", null, Fire.Definitions[Engineer]));
        Assert.Null(ScenarioA1UnitSubstitution.UnderscoredElr(SideElr, "german", null, Fire.Definitions["attacker-squad"]));
        Assert.Null(ScenarioA1UnitSubstitution.UnderscoredElr(SideElr, "russian", null, Fire.Definitions["defender-nkvd-squad"]));
        Assert.Equal(1, ScenarioA1UnitSubstitution.UnderscoredElr(GroupElr, "german", "german-2", Fire.Definitions[Engineer]));
        Assert.Equal(2, ScenarioA1UnitSubstitution.UnderscoredElr(GroupElr, "german", "german-1", Fire.Definitions[Engineer]));
        Assert.Null(ScenarioA1UnitSubstitution.UnderscoredElr([], "german", null, Fire.Definitions[Engineer]));

        // A new game refuses a malformed token; a card's names a side of the card (and one of its OB groups).
        Assert.StartsWith("play.unit-substitution-rule: 'underscored-elr:german:5'", ScenarioA1NightAndWeather.RulesBar(MalformedElr, (_, _) => true, true, true),
            StringComparison.Ordinal);
        Assert.StartsWith("play.unit-substitution-rule: 'ammo-shortage:'", ScenarioA1NightAndWeather.RulesBar(MalformedShortage, (_, _) => true, true, true),
            StringComparison.Ordinal);
        Assert.Null(ScenarioA1NightAndWeather.RulesBar(GroupElr, (_, _) => true, true, true));
        Assert.Null(ScenarioA1NightAndWeather.RulesBar(Shortage, (_, _) => true, true, true));
        var rule = new CardRuleFacts(1, "token", 1, null, "The German OB has an ELR of 3 for its underscored units.", [], [])
        {
            SubstitutionTokens = [("underscored-elr:soviet:3", false), ("ammo-shortage:german", true)]
        };
        Assert.Contains("card.ssr: SSR 1's 'underscored-elr:soviet:3' is not", Assert.Single(ScenarioA1SetupCalculator.RuleDiagnostics([rule], TokenStatus, [])),
            StringComparison.Ordinal);
    }

    // A 4-4-7 squad of the other side: a German one at a Russian target, a Russian one at any other.
    private static FireFirer Rifle(string id, string target) =>
        new(id, Fire.Definitions[target].Nationality == "russian" ? "attacker-2nd-line-squad" : "defender-squad", FireFrom, false, false, false, false, false);

    // Two 4-4-7 squads at the underscored unit in the open, Point Blank: 16 FP; the IFT DR 3 + 4 is a 2MC.
    private static FireAttack AtTheEngineers(string definition, int? underscoredElr) =>
        new FireAttack("PFPh", "phasing", true, FireFrom, FireAt, [Rifle("f-1", definition), Rifle("f-2", definition)], null, 1, true, new FireLos(false, 0, true, false), null,
            "open-ground",
            [new FireTarget("de-t", definition, FireAt, false, false, false, false, false, false, false) { UnderscoredElr = underscoredElr }], 3,
            new FireRolls([3, 4], null, null, null));

    /// <summary>Resolves an attack, answering each check with the dice given and every other roll with the lowest it takes.</summary>
    private static FireResolution Resolved(FireAttack attack, IReadOnlyList<int> checks, IReadOnlyDictionary<string, int>? shortage = null)
    {
        var rolls = attack.Rolls!;
        for (var step = 0; step < 20; step++)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack with
            {
                Rolls = rolls
            }, Fire);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal))
            {
                Assert.True(result.Disposition == FireResolution.Resolved, string.Join("; ", result.Reasons));
                return result;
            }

            var key = reason["asl.a1.fire.roll-missing:".Length..];
            var split = key.IndexOf(':', StringComparison.Ordinal);
            var (kind, ids) = (key[..split], key[(split + 1)..].Split(','));
            Dictionary<string, T> Add<T>(IReadOnlyDictionary<string, T>? existing, T value)
            {
                var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
                foreach (var id in ids)
                {
                    next[id] = value;
                }

                return next;
            }

            rolls = kind switch
            {
                "randomSelection" => rolls with { RandomSelection = Add(rolls.RandomSelection, 1) },
                "weaponSelection" => rolls with { WeaponSelection = Add(rolls.WeaponSelection, 1) },
                "cowerSelection" => rolls with { CowerSelection = Add(rolls.CowerSelection, 1) },
                "shortageSelection" => rolls with { ShortageSelection = shortage ?? throw new InvalidOperationException(key) },
                "checks" => rolls with { Checks = Add(rolls.Checks, checks) },
                _ => throw new InvalidOperationException(key),
            };
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    [Fact]
    public void AnSsrElrReplacesTheUnderscoredEngineerSquadByTheConscriptWhereR159OnlyBreaksIt()
    {
        // A19.132 (p. 86; ruling R72.13): the 2MC on 5 + 5 (12) fails the 8-3-8's Morale Level 8 by four. With the SSR ELR of 3 that is beyond its ELR, and it is
        // Replaced normally, by a broken 4-3-6 Conscript.
        var replaced = Assert.Single(Resolved(AtTheEngineers(Engineer, 3), [5, 5]).Effects);
        Assert.Equal(("attacker-conscript-squad", true), (replaced.FinalDefinitionId, replaced.Broken));
        Assert.Contains("replaced-elr", replaced.Events);
        Assert.DoesNotContain("replaced-by-two-half-squads", replaced.Events);

        // Without the SSR, R15.9's ELR of 5 holds: failed by four, the squad only breaks.
        var broken = Assert.Single(Resolved(AtTheEngineers(Engineer, null), [5, 5]).Effects);
        Assert.Equal((Engineer, true), (broken.FinalDefinitionId, broken.Broken));
        Assert.DoesNotContain("replaced-elr", broken.Events);

        // Failed by six (6 + 6 is an Original 12, a Casualty MC; so 6 + 5 Encircled), beyond 5: its two broken 3-3-8, as before the pass.
        var split = Assert.Single(Resolved(AtTheEngineers(Engineer, null) with
        {
            Targets = [new FireTarget("de-t", Engineer, FireAt, false, false, false, false, false, false, false) { Encircled = true }]
        }, [6, 5]).Effects);
        Assert.Equal(("attacker-elite-half-squad-3-3-8", true), (split.FinalDefinitionId, split.Broken));
        Assert.Contains("replaced-by-two-half-squads", split.Events);
    }

    [Theory]
    // A19.132 (ruling R72.13): the 3-3-8 by the 2-3-7 ruled for it (ruling R19.7), the elite 2-4-8 HS by the 2-4-7, the NKVD 6-2-8 and 3-2-8 by the Conscript.
    [InlineData("attacker-elite-half-squad-3-3-8", "attacker-2nd-line-half-squad", false)]
    [InlineData("attacker-elite-half-squad", "attacker-half-squad", false)]
    [InlineData("defender-nkvd-squad", "defender-conscript-squad", false)]
    [InlineData("defender-nkvd-half-squad", "defender-conscript-half-squad", false)]
    // The Finnish 8-3-8 and 3-3-8 are in neither of A25.72's progressions (ruling R15.13): never Replaced, so Disrupted (A19.12).
    [InlineData("finnish-elite-squad", "finnish-elite-squad", true)]
    [InlineData("finnish-elite-half-squad", "finnish-elite-half-squad", true)]
    public void EveryUnderscoredMmcUnderAnSsrElrIsReplacedNormally(string definition, string replacement, bool disrupted)
    {
        Assert.True(Fire.Definitions[definition] is { IsMmc: true, UnderscoredMorale: true }, definition);
        var morale = Fire.Definitions[definition].Morale!.Value;

        // A check four over the Morale Level (the 2MC's +2 included), not an Original 12.
        var total = morale + 4 - 2;
        int[] checks = [Math.Min(6, total - 1), total - Math.Min(6, total - 1)];
        var effect = Assert.Single(Resolved(AtTheEngineers(definition, 0), checks).Effects);
        Assert.Equal((replacement, true, disrupted), (effect.FinalDefinitionId, effect.Broken, effect.Disrupted));
    }

    private static FireFirer Gunner(string definition, string weapon, bool? inexperienced) => new FireFirer("ru-1", definition, FireFrom, false, false, false, false, true)
    {
        Weapons = [new FireWeapon("mg", weapon, false, false, false)],
        Inexperienced = inexperienced,
    };

    // A squad and its LMG at a German squad 2 hexes off in the open; the IFT DR is given.
    private static FireWeaponEffect Lmg(FireFirer gunner, int[] dice, bool stamped) =>
        Assert.Single(Resolved(new FireAttack("PFPh", "phasing", true, FireFrom, FireAt, [gunner], null, 2, true, new FireLos(false, 0, true, false), null, "open-ground",
            [new FireTarget("de-t", "attacker-squad", FireAt, false, false, false, false, false, false, false)], 3, new FireRolls(dice, null, null, null)) with
        {
            UnitSubstitution = stamped ? true : null,
            CoweringMarksAndWeaponResidual = stamped ? true : null,
        }, [2, 3]).WeaponEffects!);

    /// <summary>An IFT DR of a total, not doubles (no Cowering).</summary>
    private static int[] Dr(int total) => [total - Math.Min(6, total - 1), Math.Min(6, total - 1)];

    [Fact]
    public void AConscriptsMgBreaksDownOneLowerWithThePassStampAndAsRecordedWithout()
    {
        // A19.32 (p. 86; ruling R72.9, R9.10 lifted): the Russian LMG's B# one lower in a Conscript's hands; an Original DR of B# - 1 malfunctions it, and as
        // A.11 has it the lowered B# makes the printed one its X#, which this DR does not reach.
        var printed = Fire.Definitions["defender-lmg"].Breakdown!.Value;
        var dice = Dr(printed - 1);
        var conscript = Gunner("defender-conscript-squad", "defender-lmg", null);
        var stamped = Lmg(conscript, dice, stamped: true);
        Assert.Equal((printed - 1, true, (bool?)null), (stamped.BreakdownNumber, stamped.Malfunctioned, stamped.Removed));

        // Without the stamp, as dl-tractor-works-01 revision 1267 was recorded: B11 and an Original DR of 10, no malfunction.
        var recorded = Lmg(conscript, dice, stamped: false);
        Assert.Equal((printed, false), (recorded.BreakdownNumber, recorded.Malfunctioned));
    }

    [Fact]
    public void AGreenSquadStackedWithALeaderKeepsItsMgsBreakdownNumber()
    {
        // A19.3 (p. 86): "A Green MMC stacked with an unbroken leader is exempt"; its Inexperienced fact is false, so its B# stays. Without the leader it is
        // Inexperienced and its B# is one lower.
        var printed = Fire.Definitions["british-lmg"].Breakdown!.Value;
        var dice = Dr(printed - 1);
        Assert.Equal((printed, false), Pair(Lmg(Gunner("british-green-squad", "british-lmg", false), dice, stamped: true)));
        Assert.Equal((printed - 1, true), Pair(Lmg(Gunner("british-green-squad", "british-lmg", true), dice, stamped: true)));

        static (int, bool) Pair(FireWeaponEffect effect) => (effect.BreakdownNumber, effect.Malfunctioned);
    }

    // Increment 5d: Ammunition Shortage (A19.131; rulings R72.11, R72.12).

    private static readonly ScenarioA1OrdnanceReference Ordnance = new ScenarioA1OrdnancePackage().Reference;

    private static FireFirer Squad(string id, string definition = "defender-squad") => new(id, definition, FireFrom, false, false, false, false, false);

    // A fire group at a German squad (a Russian one for a German group) two hexes off in the open, on the IFT DR given, the firing side short of ammunition.
    private static FireAttack Short(int[] dice, bool shortage, params FireFirer[] firers) =>
        new FireAttack("PFPh", "phasing", true, FireFrom, FireAt, firers, null, 2, true, new FireLos(false, 0, true, false), null, "open-ground",
            [new FireTarget("de-t", Fire.Definitions[firers[0].DefinitionId!].Nationality == "russian" ? "attacker-squad" : "defender-squad", FireAt, false, false, false,
                false, false, false, false)], 3, new FireRolls(dice, null, null, null)) with
        {
            AmmunitionShortage = shortage ? true : null,
            UnitSubstitution = true,
            CoweringMarksAndWeaponResidual = true,
        };

    private static readonly Dictionary<string, int> SecondPicked = new(StringComparer.Ordinal) { ["ru-1"] = 2, ["ru-2"] = 5 };
    private static readonly Dictionary<string, int> Tied = new(StringComparer.Ordinal) { ["ru-1"] = 4, ["ru-2"] = 4 };
    private static readonly string[] BothSquads = ["ru-1", "ru-2"];

    [Fact]
    public void AnOriginal12WithInherentFpReplacesTheMmcTheRandomSelectionPicks()
    {
        // A19.131 (p. 86; ruling R72.11): two Russian 4-4-7 fire with their Inherent FP; the Original 12 under Ammunition Shortage Replaces the one with the higher
        // Random Selection dr (A.9) by the next lower quality unit, the 4-2-6 Conscript, which keeps its unbroken status.
        var replaced = Assert.Single(Resolved(Short([6, 6], true, Squad("ru-1"), Squad("ru-2")), [], SecondPicked).ShortageEffects!);
        Assert.Equal(("ru-2", "defender-conscript-squad", false, (int?)5), (replaced.UnitId, replaced.FinalDefinitionId, replaced.Broken, replaced.RandomSelectionDr));
        Assert.Contains("ammunition-shortage-replaced", replaced.Events);

        // A.9: "all of the applicable units which rolled that number are affected equally".
        Assert.Equal(BothSquads, Resolved(Short([6, 6], true, Squad("ru-1"), Squad("ru-2")), [], Tied).ShortageEffects!.Select(item => item.UnitId));

        // No Ammunition Shortage, or no Original 12: no Replacement.
        Assert.Null(Resolved(Short([6, 6], false, Squad("ru-1"), Squad("ru-2")), []).ShortageEffects);
        Assert.Null(Resolved(Short([5, 6], true, Squad("ru-1"), Squad("ru-2")), []).ShortageEffects);
    }

    private static FireFirer WithLmg(FireFirer firer, bool inherent, bool captured = false) => firer with
    {
        Weapons = [new FireWeapon("mg", "defender-lmg", false, false, false) { Captured = captured ? true : null }],
        UsesInherentFp = inherent ? null : false,
        UsesSupportWeapon = true,
    };

    [Fact]
    public void ASquadFiringOnlyItsLmgIsNotReplacedAndItsLoweredBreakdownNumberIsItsX()
    {
        // A19.131: only a MMC "using its Inherent FP" suffers Unit Replacement; a squad firing its LMG alone does not, though the 12 malfunctions the LMG.
        var alone = Resolved(Short([6, 6], true, WithLmg(Squad("ru-1"), inherent: false)), []);
        Assert.Null(alone.ShortageEffects);
        Assert.True(Assert.Single(alone.WeaponEffects!).Malfunctioned);

        // A19.131, A.11 (p. 43; ruling R72.18): the LMG's B# is one lower and its Original B# becomes its X# (a B11 LMG is B10 and X11), so an Original DR
        // of the lowered B# malfunctions it and one of the printed B# removes it. Without the Shortage the lower DR does nothing to it.
        var printed = Fire.Definitions["defender-lmg"].Breakdown!.Value;
        var lowered = Assert.Single(Resolved(Short(Dr(printed - 1), true, WithLmg(Squad("ru-1"), inherent: false)), []).WeaponEffects!);
        Assert.Equal((printed - 1, true, (bool?)null), (lowered.BreakdownNumber, lowered.Malfunctioned, lowered.Removed));
        var reached = Assert.Single(Resolved(Short(Dr(printed), true, WithLmg(Squad("ru-1"), inherent: false)), []).WeaponEffects!);
        Assert.Equal((printed - 1, true, (bool?)true), (reached.BreakdownNumber, reached.Malfunctioned, reached.Removed));
        var normal = Assert.Single(Resolved(Short(Dr(printed - 1), false, WithLmg(Squad("ru-1"), inherent: false)), []).WeaponEffects!);
        Assert.Equal((printed, false), (normal.BreakdownNumber, normal.Malfunctioned));
    }

    [Fact]
    public void ACapturedMgKeepsItsBreakdownNumberUnderAmmunitionShortage()
    {
        // A21.11 (p. 88): "Ammunition Shortage (19.131) penalties do not apply to captured Gun/SW": a German squad's captured Russian LMG is two lower, not three.
        var printed = Fire.Definitions["defender-lmg"].Breakdown!.Value;
        var captured = Assert.Single(Resolved(Short(Dr(3), true, WithLmg(Squad("de-1", "attacker-squad"), inherent: false, captured: true)), [2, 3]).WeaponEffects!);
        Assert.Equal(printed - 2, captured.BreakdownNumber);
    }

    [Fact]
    public void AnEliteUnderscoredSquadIsReplacedByItsTwoHalfSquads()
    {
        // Ruling R72.11 (A19.13): the German 8-3-8, firing alone (no Random Selection), becomes its two 3-3-8 HS, unbroken as it was.
        var split = Assert.Single(Resolved(Short([6, 6], true, Squad("de-1", Engineer)), []).ShortageEffects!);
        Assert.Equal(("attacker-elite-half-squad-3-3-8", (bool?)true, false, (int?)null), (split.FinalDefinitionId, split.SplitIntoHalfSquads, split.Broken, split.RandomSelectionDr));
    }

    [Fact]
    public void AConscriptIncapableOfQualityLossIsBrokenUnlessBerserk()
    {
        // A19.131: "Any MMC subject to, but incapable of, further quality loss is broken after resolution of the attack, unless berserk."
        var broken = Assert.Single(Resolved(Short([6, 6], true, Squad("ru-1", "defender-conscript-squad")), []).ShortageEffects!);
        Assert.Equal(("defender-conscript-squad", true), (broken.FinalDefinitionId, broken.Broken));
        Assert.Contains("ammunition-shortage-broken", broken.Events);
        var berserk = Assert.Single(Resolved(Short([6, 6], true, Squad("ru-1", "defender-conscript-squad") with
        {
            Berserk = true
        }), []).ShortageEffects!);
        Assert.Equal(("defender-conscript-squad", false), (berserk.FinalDefinitionId, berserk.Broken));
        Assert.Contains("ammunition-shortage-berserk", berserk.Events);
    }

    [Fact]
    public void ADcKeepsItsBreakdownNumberUnderAmmunitionShortage()
    {
        // A19.131: "all SW Original B#/X# [EXC: DC]": a 1st Line owner's Placed DC still malfunctions on 10 (A23.4), not on 9.
        var charge = new FireDemolitionCharge("ru-dc", "defender-dc", FireDemolitionCharge.Placed, "ru-1", "defender-squad", false, false, false, null);
        FireAttack Placed(int[] dice) => new FireAttack("AFPh", "phasing", null, FireFrom, FireAt, null, null, 1, true, null, null, "stone-building",
            [new FireTarget("de-s", "attacker-squad", FireAt, false, false, false, false, false, false, false)], 3, new FireRolls(dice, null, null, null))
        {
            DemolitionCharge = charge,
            AmmunitionShortage = true,
            UnitSubstitution = true,
        };
        Assert.NotEqual(true, Resolved(Placed([4, 5]), [3, 3]).DemolitionChargeMalfunctioned);
        Assert.True(Resolved(Placed([4, 6]), [3, 3]).DemolitionChargeMalfunctioned);
    }

    [Fact]
    public void DepletionNumbersAreOneLowerUnderAmmunitionShortage()
    {
        // A19.131 (ruling R72.12): "Ammunition Depletion Numbers are decreased by one"; the 6-Pounder's HE8 of 1944 is 7, and Elite's one more makes it 8 again.
        var sixPounder = Ordnance.Guns["british-at-gun"];
        Assert.Equal((8, 7, 8), (ScenarioA1Definitions.DepletionNumber(sixPounder, "he", 1944, 6), ScenarioA1Definitions.DepletionNumber(sixPounder, "he", 1944, 6, shortage: true),
            ScenarioA1Definitions.DepletionNumber(sixPounder, "he", 1944, 6, elite: true, shortage: true)));
        Assert.Equal("HE (Depletion Number 7: 8, less 1 for Ammunition Shortage, A19.131)",
            ScenarioA1Definitions.AmmunitionChoices("british-at-gun", false, 1944, 6, false, [], shortage: true)[0].Label);
        Assert.Equal("Depletion Number 7 (8, less 1 for Ammunition Shortage, A19.131)", ScenarioA1Definitions.DepletionNumberWords(7, false, true));
    }

    [Fact]
    public void AFireLaneIsRefusedUnderAmmunitionShortage()
    {
        // A19.131: Ammunition Shortages "do prevent placement of Fire Lanes" (A9.22's Good Order MG is "not ... restricted by Ammunition Shortage"); a captured
        // MG takes no Ammunition Shortage penalty (A21.11).
        var gunner = WithLmg(Squad("ru-1"), inherent: true);
        var attack = Short([3, 4], true, gunner) with
        {
            Phase = "MPh",
            FiringSide = "non-phasing",
            FireKind = ScenarioA1FireCalculator.FirstFire,
        };
        var mg = gunner.Weapons![0];
        Assert.Equal(ScenarioA1FireEligibility.AmmunitionShortageFireLaneRefusal,
            ScenarioA1FireEligibility.FireLaneDeclarationBar(new FireLaneDeclarationFacts("mph", attack, gunner, mg, true, 6, 2, true)));
        Assert.NotEqual(ScenarioA1FireEligibility.AmmunitionShortageFireLaneRefusal,
            ScenarioA1FireEligibility.FireLaneDeclarationBar(new FireLaneDeclarationFacts("mph", attack, gunner, mg with
            {
                Captured = true
            }, true, 6, 2, true)));
        Assert.NotEqual(ScenarioA1FireEligibility.AmmunitionShortageFireLaneRefusal,
            ScenarioA1FireEligibility.FireLaneDeclarationBar(new FireLaneDeclarationFacts("mph", attack with
            {
                AmmunitionShortage = null
            }, gunner, mg, true, 6, 2, true)));
    }

    // Increment 5e: the hero's weapons (A15.23, A15.24, C13.25, C13.45; ruling R72.8).

    private static FireAttack OrdnanceHit(string phase, string side, FireTarget[] targets) =>
        new(phase, side, null, null, FireAt, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", targets, 2, null);

    private static OrdnanceShot Stamped(OrdnanceShot shot, bool stamped) => shot with
    {
        HeroWeapons = stamped ? true : null,
        Rolls = new OrdnanceRolls([2, 3], null, null, new FireRolls([6, 5], null, null, null), null)
        {
            ToKill = [6, 6],
            PanzerfaustCheck = 1,
            CrewSurvival = [6, 6],
            CrewCheck = [3, 4],
            ShockCheck = [3, 4],
        },
    };

    // A LATW (or a PF) of the firer's side at the enemy tank, from a unit of the given definition, in the PFPh.
    private static OrdnanceShot AtTheTank(string nationality, string weapon, string firer, string tank, string ammunition, int range, bool stamped) =>
        Stamped(new OrdnanceShot("PFPh", "phasing", nationality, new OrdnanceGun("sw", weapon, false, 0, false, false),
            new OrdnanceCrew("hero", firer, false, false, false, false, false), FireAt, range, 0, false, true, 0, OrdnanceHit("PFPh", "phasing", []), null)
        {
            VehicleTarget = new OrdnanceVehicleTarget("tank", tank, "side", "side", false, false, false, false, true),
            Ammunition = ammunition,
            ScenarioYear = 1944,
            Panzerfaust = weapon == OrdnanceTargetTypes.Panzerfaust ? new OrdnancePanzerfaust(0, 4, false, false) : null,
        }, stamped);

    private static FireModifier[] HeroDrm(IEnumerable<FireModifier> drm) => [.. drm.Where(item => item.Name.StartsWith("hero", StringComparison.Ordinal))];

    [Fact]
    public void AHerosLightMortarTakesItsMinusOneOnTheToHitDrOnly()
    {
        // A15.23 (p. 83; ruling R72.8): the German hero's 50mm mortar at the Russian squad 5 hexes off takes -1 on its To Hit DR; the hit's IFT DR takes neither
        // it nor A15.24's heroic DRM. C9.2 (p. 179): a lone SMC firing a light mortar loses its Multiple ROF.
        var target = new FireTarget("ru-s", "defender-squad", FireAt, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] };
        var mortar = new OrdnanceShot("PFPh", "phasing", "german", new OrdnanceGun("de-mtr", "attacker-light-mortar", false, 0, false, false),
            new OrdnanceCrew("de-h", "attacker-hero", false, false, false, false, false), FireAt, 5, 0, false, true, 0, OrdnanceHit("PFPh", "phasing", [target]), null)
        {
            TargetType = OrdnanceTargetTypes.Area,
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(Stamped(mortar, true), Ordnance);
        Assert.True(result.Disposition == OrdnanceResolution.Resolved, string.Join("; ", result.Reasons));
        var hero = Assert.Single(HeroDrm(result.ToHit!.Drm));
        Assert.Equal(("hero:de-h", -1m, "A15.23"), (hero.Name, hero.Value, hero.Rule));
        Assert.True(result.ToHit.Hit);
        Assert.Empty(HeroDrm(result.Hit!.Arithmetic!.Drm));
        Assert.Equal(0, result.Gun!.RateOfFire);

        // A record made before the pass carries no fact and takes no -1, as it was resolved.
        Assert.Empty(HeroDrm(ScenarioA1OrdnanceCalculator.Resolve(Stamped(mortar, false), Ordnance).ToHit!.Drm));
    }

    [Fact]
    public void AHerosPfAndAtrAtAVehicleTakeTheMinusOneOnTheToHitDrAndNeverOnTheToKill()
    {
        // A15.23, C13.25 (p. 183; ruling R72.8): "Leadership/Hero modifiers do not apply to ATR To Kill DR, but do modify any To Hit DR"; so a PF's.
        foreach (var shot in new[]
        {
            AtTheTank("german", OrdnanceTargetTypes.Panzerfaust, "attacker-hero", "defender-tank", "heat", 2, true),
            AtTheTank("russian", "defender-atr", "defender-hero", "attacker-tank", "ap", 3, true),
        })
        {
            var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Ordnance);
            Assert.True(result.Disposition == OrdnanceResolution.Resolved, string.Join("; ", result.Reasons));
            Assert.Equal(["hero:hero"], HeroDrm(result.ToHit!.Drm).Select(item => item.Name));
            Assert.Empty(HeroDrm(result.Kill!.Modifications));
        }
    }

    [Fact]
    public void ALoneHeroFiresAPskWithTheTwoManPlusOneNegatedAndALoneLeaderStaysRefused()
    {
        // C13.45, C13.48 (p. 184), A15.23 (ruling R72.8): "A BAZ can be used by one SMC only if that SMC is a Hero": +1 for one man firing a SW that needs two,
        // negated by his -1, both named in the arithmetic.
        var hero = ScenarioA1OrdnanceCalculator.Resolve(AtTheTank("german", "attacker-psk", "attacker-hero", "defender-tank", "heat", 2, true), Ordnance);
        Assert.True(hero.Disposition == OrdnanceResolution.Resolved, string.Join("; ", hero.Reasons));
        Assert.Equal([("hero:hero", -1m), ("hero-two-man:hero", 1m)], HeroDrm(hero.ToHit!.Drm).Select(item => (item.Name, item.Value)));
        Assert.Equal(0m, HeroDrm(hero.ToHit.Drm).Sum(item => item.Value));

        // Ruling R9.11: a lone leader fires no PSK; nor a hero in a shot without the pass's fact, which no record before the pass could hold.
        Assert.Contains("asl.a1.ordnance.crew-outside",
            ScenarioA1OrdnanceCalculator.Resolve(AtTheTank("german", "attacker-psk", "attacker-leader-8-1", "defender-tank", "heat", 2, true), Ordnance).Reasons);
        Assert.Contains("asl.a1.ordnance.crew-outside",
            ScenarioA1OrdnanceCalculator.Resolve(AtTheTank("german", "attacker-psk", "attacker-hero", "defender-tank", "heat", 2, false), Ordnance).Reasons);
    }

    [Fact]
    public void AHerosAtrAsSmallArmsTakesTheHeroicDrmAndNoHeroMgDrm()
    {
        // C13.24, C13.25 (p. 183), A15.23, A15.24 (ruling R72.8): the Russian hero fires his ATR as Small Arms at the German squad 2 hexes off, within its Normal
        // Range of 12: A15.24's -1, no hero-mg +1 (it is no MG), no second -1, and no inherent FP of his own.
        var hero = new FireFirer("ru-h", "defender-hero", FireFrom, false, false, false, false, true)
        {
            Weapons = [new FireWeapon("ru-atr", "defender-atr", false, false, false)],
            UsesSupportWeapon = true,
        };
        var result = Resolved(new FireAttack("PFPh", "phasing", true, FireFrom, FireAt, [hero], null, 2, true, new FireLos(false, 0, true, false), null, "open-ground",
            [new FireTarget("de-t", "attacker-squad", FireAt, false, false, false, false, false, false, false)], 3, new FireRolls([5, 4], null, null, null)) with
        {
            UnitSubstitution = true,
            CoweringMarksAndWeaponResidual = true,
        }, [3, 4]);
        Assert.Equal([("heroic:ru-h", -1m)], HeroDrm(result.Arithmetic!.Drm).Select(item => (item.Name, item.Value)));
        Assert.DoesNotContain(result.Arithmetic.Firers, item => item.UnitId == "ru-h");
    }

    // The reviews' fixes, Round B (rulings R72.18 to R72.22).

    [Fact]
    public void AnUnderscoredSquadUnderBothSsrsIsStillReplacedByItsTwoHalfSquadsOnAShortage()
    {
        // Ruling R72.22 (A19.132, A19.13, p. 86): A19.132's SSR ELR reaches ELR failures only; the Shortage's Unit Replacement keeps A19.13's EXC, so the German
        // 8-3-8 under an SSR ELR of 3 still becomes its two 3-3-8 HS.
        var engineers = Squad("de-1", Engineer) with
        {
            UnderscoredElr = 3
        };
        var split = Assert.Single(Resolved(Short([6, 6], true, engineers), []).ShortageEffects!);
        Assert.Equal(("attacker-elite-half-squad-3-3-8", (bool?)true, false), (split.FinalDefinitionId, split.SplitIntoHalfSquads, split.Broken));
    }

    [Fact]
    public void ALoweredBreakdownNumberIsSaidWithItsPrintedNumberAndItsCause()
    {
        // A19.131, A.11 (ruling R72.18; the reviews R3.3): the LMG's B# one lower for the Shortage, and its X# the printed B# when the DR reaches it.
        var printed = Fire.Definitions["defender-lmg"].Breakdown!.Value;
        var malfunctioned = Short(Dr(printed - 1), true, WithLmg(Squad("ru-1"), inherent: false));
        Assert.Equal($"B{printed - 1} (B{printed}, less 1 for Ammunition Shortage, A19.131)",
            ScenarioA1FireCalculator.BreakdownWords(malfunctioned, Assert.Single(Resolved(malfunctioned, []).WeaponEffects!), Fire));
        var removed = Short(Dr(printed), true, WithLmg(Squad("ru-1"), inherent: false));
        Assert.Equal($"X{printed} (B{printed}, lowered to B{printed - 1}: less 1 for Ammunition Shortage, A19.131)",
            ScenarioA1FireCalculator.BreakdownWords(removed, Assert.Single(Resolved(removed, []).WeaponEffects!), Fire));

        // A B# as printed says nothing more.
        var normal = Short(Dr(printed), false, WithLmg(Squad("ru-1"), inherent: false));
        Assert.Null(ScenarioA1FireCalculator.BreakdownWords(normal, Assert.Single(Resolved(normal, []).WeaponEffects!), Fire));
    }

    [Fact]
    public void ACapturedGunsDepletionNumberIsNotLoweredUnderAmmunitionShortage()
    {
        // A21.11 (p. 88; ruling R72.18): "Ammunition Shortage (19.131) penalties do not apply to captured Gun/SW", so its Depletion Number either.
        var shot = new OrdnanceShot("PFPh", "phasing", "british", new OrdnanceGun("gun", "british-at-gun", false, 0, false, false),
            new OrdnanceCrew("crew", "defender-crew", false, false, false, false, false), FireAt, 3, 0, false, true, 0, null, null)
        {
            Ammunition = "he",
            ScenarioYear = 1944,
            AmmunitionShortage = true,
        };
        Assert.True(ScenarioA1Definitions.DepletionShortage(shot));
        Assert.False(ScenarioA1Definitions.DepletionShortage(shot with
        {
            CapturedPenalty = 1
        }));
        Assert.False(ScenarioA1Definitions.DepletionShortage(shot with
        {
            AmmunitionShortage = null
        }));
    }

    [Fact]
    public void AFirerReplacedUnderAmmunitionShortageDoesNotReturnToNormal()
    {
        // A19.131, A15.46 (the reviews R3.11): Rules leaves out the berserk firer the attack Replaced; the other returns to normal.
        var facts = new BerserkAttackFacts("afph", true, false, BerserkPair) { Replaced = ["g1"] };
        Assert.Equal(["g2"], ScenarioA1SequenceCalculator.BerserkReturnsAfterAttack(facts, () => new KnownEnemyCount(1, 0)));
        Assert.Equal("g1 eliminated every Known enemy unit in its Location by its FT in the AFPh, so it is no longer berserk and returns to Good Order (A15.46)",
            ScenarioA1SequenceCalculator.BerserkClearedText("g1", false, tpbf: false, flamethrower: true));
        Assert.Contains("by its TPBF or FT in the AFPh", ScenarioA1SequenceCalculator.BerserkClearedText("g1", false, tpbf: false), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AConcealedEnemyLeftInCcKeepsNoBerserkUnitBerserkFromThePassOn(bool stamped)
    {
        // A15.46 (p. 84; ruling R72.20): "all ... Known enemy units in its Location": the German squad kills the Russian HS, and a concealed Russian HS is left;
        // a round recorded from the pass on counts Known units only, one recorded before counts every enemy unit as it did.
        CloseCombatUnit[] units = [Unit("g1", "attacker-squad", "german") with { Berserk = true }, Unit("r1", "defender-half-squad", "russian"),
            Unit("r2", "defender-half-squad", "russian") with { Concealed = true }];
        var facts = new CloseCombatFacts("CCPh", "bd01:G5:0", "open-ground", "german", CloseCombatFacts.Simultaneous, null, units, [], [],
            [new CloseCombatDeclaration(["g1"], ["r1"])], new CloseCombatRolls(new Dictionary<string, IReadOnlyList<int>> { ["0"] = [2, 3] }))
        {
            KnownEnemiesOnly = stamped ? true : null,
        };
        var result = ScenarioA1CloseCombatCalculator.Resolve(facts, CloseCombat);
        Assert.True(result.Effects.Single(item => item.UnitId == "r1").Eliminated);
        Assert.Equal(stamped ? true : (bool?)null, result.Effects.Single(item => item.UnitId == "g1").BerserkEnded);
    }

    [Fact]
    public void ATpbfIsSelectedOnlyWhenAFirerStandsInTheTargetLocation()
    {
        // A7.21, A7.211 (rulings R72.7, R72.21): Play hands the fact that a firer, Infantry or a vehicle, is in the target Location; Rules attacks the other
        // side's units there only, and never in the MPh's moving stack.
        TargetUnitStateFacts[] here = [new("g1", true, "german", false, false, true, null), new("r1", true, "russian", false, false, true, null)];
        Assert.Equal(["r1"], ScenarioA1FireEligibility.SelectTargets(here, false, "german", firerInTarget: true).Targets);
        Assert.Equal(["g1", "r1"], ScenarioA1FireEligibility.SelectTargets(here, false, "german").Targets);
    }

    [Fact]
    public void TheHerosDrmReadsInTheProposalAsTheRecordSaysIt()
    {
        // A15.23 (the reviews' known items): "hero's SW, gh", not "hero:gh"; the PSK's +1 is the hero rule's own.
        Assert.Equal(("hero's SW, gh", "hero alone with a two-man SW, gh"),
            (ScenarioA1OrdnanceEligibility.CaseWords("hero:gh"), ScenarioA1OrdnanceEligibility.CaseWords("hero-two-man:gh")));
        var hero = ScenarioA1OrdnanceCalculator.Resolve(AtTheTank("german", "attacker-psk", "attacker-hero", "defender-tank", "heat", 2, true), Ordnance);
        Assert.Equal("A15.23", HeroDrm(hero.ToHit!.Drm).Single(item => item.Name == "hero-two-man:hero").Rule);
    }

    [Fact]
    public void TheFireLaneIsOfferedUnderAmmunitionShortageOnlyWithACapturedWeapon()
    {
        // A19.131, A9.22, A21.11 (the reviews R4.13): the captured read is asked only under the Shortage.
        Assert.True(ScenarioA1FireEligibility.FireLaneOffered(false, () => throw new InvalidOperationException("The weapons were read.")));
        Assert.False(ScenarioA1FireEligibility.FireLaneOffered(true, () => false));
        Assert.True(ScenarioA1FireEligibility.FireLaneOffered(true, () => true));
    }
}
