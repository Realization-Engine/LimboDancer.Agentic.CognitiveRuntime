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
    private static FireResolution Resolved(FireAttack attack, IReadOnlyList<int> checks)
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
}
