using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 32.f: the rout, Rally, Repair, and Shock decisions moved into Rules, read directly: the rout's scans and search over a fact reader of a small
/// table, the planner's bars with their lazy reads, the Rally attempt's derived facts, the ordered condition lists, and the projector's verdicts.
/// </summary>
public sealed class ScenarioA1RoutRallyRulesTests
{
    private static Func<bool> Never(string what) => () => throw new InvalidOperationException(what + " was read.");

    /// <summary>
    /// A row of five ground-level hexes, 0 to 4, each ADJACENT to the next: 0 and 2 Open Ground, 1 grain, 3 woods, 4 a stone building; every LOS clear
    /// at the hex distance unless a pair is listed as blocked.
    /// </summary>
    private sealed class Row : IRoutFactReader
    {
        private static readonly string[] Terrain = ["open-ground", "grain", "open-ground", "woods", "stone-building"];
        private readonly HashSet<(int, int)> blocked = [];
        private readonly HashSet<int> unplayable = [];
        private readonly Dictionary<(int, int), RoutCoverFacts> covers = [];
        private readonly Dictionary<(int, int, int), RoutCoverFacts> stepCovers = [];
        private readonly List<string> reads = [];

        public IReadOnlyList<string> Reads => reads;

        public Row Block(int one, int two)
        {
            blocked.Add((one, two));
            blocked.Add((two, one));
            return this;
        }

        public Row Unplayable(int location)
        {
            unplayable.Add(location);
            return this;
        }

        public string Name(int location) => "bd01:" + "ABCDE"[location] + "1:0";

        public IEnumerable<int> Neighbors(int location)
        {
            reads.Add("neighbors " + location);
            if (location > 0)
            {
                yield return location - 1;
            }

            if (location < 4)
            {
                yield return location + 1;
            }
        }

        public bool Playable(int location) => !unplayable.Contains(location);

        public Row Covered(int enemyLocation, int location, RoutCoverFacts cover)
        {
            covers[(enemyLocation, location)] = cover;
            return this;
        }

        public Row CoveredFrom(int enemyLocation, int location, int steppedFrom, RoutCoverFacts cover)
        {
            stepCovers[(enemyLocation, location, steppedFrom)] = cover;
            return this;
        }

        public RoutCoverFacts Cover(int enemyLocation, int location, int? steppedFrom = null)
        {
            reads.Add($"cover {enemyLocation}-{location}");
            return steppedFrom is { } left && stepCovers.TryGetValue((enemyLocation, location, left), out var stepped) ? stepped
                : covers.GetValueOrDefault((enemyLocation, location), RoutCoverFacts.None);
        }

        public RoutLocationFacts? Location(int location) => new(Terrain[location], false);

        public RoutLosFacts? Los(int fromLocation, int toLocation)
        {
            reads.Add($"los {fromLocation}-{toLocation}");
            return new(!blocked.Contains((fromLocation, toLocation)), 0, Math.Abs(fromLocation - toLocation));
        }

        public int? Distance(int one, int two) => Math.Abs(one - two);

        public bool Adjacent(int one, int two) => Math.Abs(one - two) == 1;

        public (InfantryEntry? Entry, string? Reason) Entry(int fromLocation, int toLocation) =>
            (new InfantryEntry(Terrain[toLocation] is "woods" or "stone-building" ? 4 : 2, Terrain[toLocation], false, false, false, false), null);
    }

    private static RoutEnemyFacts Enemy(string id, int at, bool armed = true, bool broken = false, bool melee = false, bool vehicle = false, bool cx = false, bool pinned = false,
        bool encircled = false, int range = 8) => new(id, at, armed, broken, melee, vehicle, cx, pinned, encircled, range);

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void AnArmedEnemyIsPersonnelOrAVehicleNotAbandoned(bool vehicle, bool abandoned, bool armed) =>
        Assert.Equal(armed, ScenarioA1RoutCalculator.Armed(vehicle, abandoned));

    [Fact]
    public void TheNormalRangeIsTheLongestOfOwnAndWeaponsAtMostSixteen()
    {
        Assert.Equal(8, ScenarioA1RoutCalculator.NormalRange(ScenarioA1RoutCalculator.OwnRange(true, false, 8), []));
        Assert.Equal(12, ScenarioA1RoutCalculator.NormalRange(ScenarioA1RoutCalculator.OwnRange(true, false, 8), [12, 4]));
        Assert.Equal(16, ScenarioA1RoutCalculator.NormalRange(0, [40]));
        Assert.Equal(0, ScenarioA1RoutCalculator.OwnRange(true, true, 8));
        Assert.Equal(0, ScenarioA1RoutCalculator.OwnRange(false, false, 8));
    }

    [Theory]
    [InlineData("open-ground", false, 7, true)]
    [InlineData("open-ground", true, 7, false)]
    [InlineData("grain", false, 7, false)]
    [InlineData("grain", false, 5, true)]
    [InlineData("grain", false, 10, true)]
    [InlineData("grain", false, null, false)]
    [InlineData("woods", false, 7, false)]
    public void OpenGroundIsOpenGroundOrGrainOutOfSeasonWithoutSmoke(string terrain, bool smoke, int? month, bool open) =>
        Assert.Equal(open, ScenarioA1RoutCalculator.OpenGround(new RoutLocationFacts(terrain, smoke), month));

    [Fact]
    public void OpenGroundIsNotReadFromAnUnreadLocation() => Assert.False(ScenarioA1RoutCalculator.OpenGround(null, 7));

    [Fact]
    public void TheExposingEnemyIsTheFirstByIdWithAClearLosInNormalRange()
    {
        var row = new Row();
        RoutEnemyFacts[] enemies = [Enemy("z", 4, range: 2), Enemy("b", 3, broken: true), Enemy("a", 4)];
        Assert.Equal("a", ScenarioA1RoutCalculator.ExposedInOpenGround(row, enemies, 0, 7));
        Assert.Null(ScenarioA1RoutCalculator.ExposedInOpenGround(row, enemies, 3, 7));
        Assert.Null(ScenarioA1RoutCalculator.ExposedInOpenGround(row.Block(4, 2), [Enemy("a", 4)], 2, 7));
    }

    [Fact]
    public void TheInterdictorIsUnbrokenInfantryNotCxPinnedEncircledOrInMelee()
    {
        var row = new Row();
        Assert.Equal("c", ScenarioA1RoutCalculator.Interdictor(row, [Enemy("a", 4, cx: true), Enemy("b", 4, vehicle: true), Enemy("c", 4), Enemy("d", 3, pinned: true)], 2, 7));
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(row, [Enemy("a", 4, encircled: true), Enemy("b", 4, melee: true)], 2, 7));
    }

    [Fact]
    public void NoEnemyAppliesFfmoThroughAHindranceOrAgainstATemTheUnitCouldClaim()
    {
        // Pass 35, task 35.4 (A10.531, A10.53): each kind of cover alone frees the Location of that enemy, for Interdiction and for the must-rout test.
        RoutCoverFacts[] covered =
        [
            new(null, 1, false, false, false), new(1, 0, false, false, false), new(null, 0, true, false, false), new(null, 0, false, true, false),
            new(null, 0, false, false, true),
        ];
        foreach (var cover in covered)
        {
            var row = new Row().Covered(4, 2, cover);
            Assert.False(ScenarioA1RoutCalculator.CouldApplyFfmo(row, 4, 2, 6));
            Assert.Null(ScenarioA1RoutCalculator.Interdictor(row, [Enemy("a", 4)], 2, 7));
            Assert.Null(ScenarioA1RoutCalculator.ExposedInOpenGround(row, [Enemy("a", 4)], 2, 7));
            Assert.Equal("b", ScenarioA1RoutCalculator.Interdictor(row, [Enemy("a", 4), Enemy("b", 0)], 2, 7));
        }

        // Fire's count of the map Hindrance, where fire can attribute it, replaces the map's own total: none in fire's count means Open Ground.
        Assert.True(ScenarioA1RoutCalculator.CouldApplyFfmo(new Row().Covered(4, 2, new(0, 0, false, false, false)), 4, 2, 6));
        Assert.True(ScenarioA1RoutCalculator.CouldApplyFfmo(new Row(), 4, 2, 6));

        // B1.14 (p. 113), B10.31: Height Advantage keeps a unit standing on the hill from Interdiction, but not one that enters it across the Crest
        // Line hexside the enemy's LOS crosses. The reader is asked with the Location the step comes from, and the search reads each step so.
        var hill = new Row().Covered(4, 2, new(null, 0, false, true, false)).CoveredFrom(4, 2, 3, RoutCoverFacts.None);
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(hill, [Enemy("a", 4)], 2, 7));
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(hill, [Enemy("a", 4)], 2, 7, steppedFrom: 1));
        Assert.Equal("a", ScenarioA1RoutCalculator.Interdictor(hill, [Enemy("a", 4)], 2, 7, steppedFrom: 3));

        // The cover is read only for a clear LOS within range.
        var blocked = new Row().Block(4, 2);
        Assert.False(ScenarioA1RoutCalculator.CouldApplyFfmo(blocked, 4, 2, 6));
        var far = new Row();
        Assert.False(ScenarioA1RoutCalculator.CouldApplyFfmo(far, 4, 2, 1));
        Assert.DoesNotContain(blocked.Reads.Concat(far.Reads), read => read.StartsWith("cover", StringComparison.Ordinal));
    }

    [Fact]
    public void TheCoverFactsTakeFiresHindranceCountOnlyWhereFireCanAttributeIt()
    {
        Assert.Equal(new RoutCoverFacts(2, 1, true, false, true), ScenarioA1RoutCalculator.CoverFacts(true, true, 3, 1, true, false, true));
        Assert.Equal(new RoutCoverFacts(null, 1, false, true, false), ScenarioA1RoutCalculator.CoverFacts(true, false, 3, 1, false, true, false));
        Assert.Equal(new RoutCoverFacts(null, 0, false, false, false), ScenarioA1RoutCalculator.CoverFacts(false, true, 3, 1, false, false, false));
    }

    [Fact]
    public void ALoneSmcDoesNotInterdictWithASupportWeapon()
    {
        // A10.532: a leader alone with a MG has no range to Interdict at; with another SMC he has the MG's; a squad has the longest it holds.
        Assert.Equal(0, ScenarioA1RoutCalculator.InterdictionRange(true, false, 0, [12]));
        Assert.Equal(1, ScenarioA1RoutCalculator.InterdictionRange(true, false, 1, [12]));
        Assert.Equal(12, ScenarioA1RoutCalculator.InterdictionRange(true, true, 0, [12]));
        Assert.Equal(16, ScenarioA1RoutCalculator.InterdictionRange(false, false, 6, [20]));

        // A15.23 (p. 83): a hero "uses a MG (at full FP)" alone, so its FP is not halved and he Interdicts at the MG's range.
        Assert.Equal(12, ScenarioA1RoutCalculator.InterdictionRange(true, false, 4, [12], hero: true));
        Assert.Equal(4, ScenarioA1RoutCalculator.InterdictionRange(true, false, 4, [], hero: true));
        var row = new Row();
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(row, [Enemy("a", 4) with { InterdictionRange = 0 }], 2, 7));
        Assert.Equal("a", ScenarioA1RoutCalculator.ExposedInOpenGround(row, [Enemy("a", 4) with { InterdictionRange = 0 }], 2, 7));
    }

    [Fact]
    public void AUnitBoundToSurrenderOwesNoRout()
    {
        // Pass 35, task 35.17 (A10.5, A20.21): the facts are read in order, each only when the one before leaves the question open.
        Assert.True(ScenarioA1RoutCalculator.RoutStillOwed(false, false, () => true, () => true, () => false));
        Assert.False(ScenarioA1RoutCalculator.RoutStillOwed(false, false, () => true, () => true, () => true));
        Assert.False(ScenarioA1RoutCalculator.RoutStillOwed(true, false, Never("must rout"), Never("can rout"), Never("the surrender")));
        Assert.False(ScenarioA1RoutCalculator.RoutStillOwed(false, true, Never("must rout"), Never("can rout"), Never("the surrender")));
        Assert.False(ScenarioA1RoutCalculator.RoutStillOwed(false, false, () => true, () => false, Never("the surrender")));
        Assert.False(ScenarioA1RoutCalculator.AttackerMustRoutFirst(false, false, false, Never("must rout"), Never("can rout"), Never("the surrender")));
        Assert.True(ScenarioA1RoutCalculator.AttackerMustRoutFirst(true, false, false, () => true, () => true, () => false));
        Assert.False(ScenarioA1RoutCalculator.AttackerMustRoutFirst(true, false, false, () => true, () => true, () => true));
    }

    [Fact]
    public void ADisruptedUnitRoutsOnlyWhenItMustAndNeverByLowCrawl()
    {
        // Pass 35, task 35.2 (A19.12).
        Assert.True(ScenarioA1RoutCalculator.MayRout(null, true, true, true, false, false, false));
        Assert.False(ScenarioA1RoutCalculator.MayRout(null, true, true, true, false, false, true));
        Assert.True(ScenarioA1RoutCalculator.MayRout("it must", true, true, true, false, false, true));
        Assert.Equal("play.rout-low-crawl: u is Disrupted and may not use Low Crawl; it routs normally if it must rout, and otherwise stays (A19.12)", ScenarioA1RoutCalculator.DisruptedLowCrawlBar("u", true, true, false));
        Assert.Null(ScenarioA1RoutCalculator.DisruptedLowCrawlBar("u", true, true, true));
        Assert.Null(ScenarioA1RoutCalculator.DisruptedLowCrawlBar("u", true, false, false));
        Assert.Null(ScenarioA1RoutCalculator.DisruptedLowCrawlBar("u", false, true, false));
    }

    [Fact]
    public void ARoutIsRepulsedByARealConcealedUnitAndDummiesAloneAreRemoved()
    {
        // Pass 35, task 35.4 (A10.533, A.9).
        var one = ScenarioA1RoutCalculator.RoutRepulse([new("d", true, false), new("b", false, true), new("a", false, false)]);
        Assert.True(one.Repulsed);
        Assert.Equal(["b"], one.ToConceal);
        Assert.Equal(["a", "b"], one.Pool);
        Assert.True(one.NeedsSelection);
        Assert.Empty(one.Dummies);
        var single = ScenarioA1RoutCalculator.RoutRepulse([new("a", false, false), new("d", true, false)]);
        Assert.Equal((true, false), (single.Repulsed, single.NeedsSelection));
        var dummies = ScenarioA1RoutCalculator.RoutRepulse([new("d", true, false), new("e", true, false)]);
        Assert.False(dummies.Repulsed);
        Assert.Equal(["d", "e"], dummies.Dummies);
        Assert.Equal(["a"], ScenarioA1RoutCalculator.RoutRepulseShown(["a"], null));
        Assert.Equal(["b"], ScenarioA1RoutCalculator.RoutRepulseShown(["a", "b"], [2, 5]));
        Assert.Equal(["a", "b"], ScenarioA1RoutCalculator.RoutRepulseShown(["a", "b"], [4, 4]));
    }

    [Fact]
    public void AdjacentIsAnAdvanceInfantryCouldMakeAndThenALos()
    {
        // A.8 (p. 43; pass 35): the LOS is read only when a step exists, and the two levels a stairwell joins have theirs by it (B23.25).
        Assert.True(ScenarioA1MovementCalculator.IsAdjacent(false, () => true, () => true));
        Assert.False(ScenarioA1MovementCalculator.IsAdjacent(false, () => true, () => false));
        Assert.False(ScenarioA1MovementCalculator.IsAdjacent(false, () => false, Never("the LOS")));
        Assert.True(ScenarioA1MovementCalculator.IsAdjacent(true, () => true, Never("the LOS")));
        Assert.False(ScenarioA1MovementCalculator.IsAdjacent(true, () => false, Never("the LOS")));
    }

    [Fact]
    public void ARoutStepNeverEntersOrApproachesAKnownEnemy()
    {
        var row = new Row();
        Assert.Equal("play.rout-step: a routing unit never enters bd01:C1:0, which holds the Known enemy unit e (A10.51)",
            ScenarioA1RoutCalculator.RoutStepBar(row, [Enemy("e", 2)], 1, 2, []));
        Assert.Equal("play.rout-step: a routing unit never moves ADJACENT to the Known enemy unit e unless it is leaving its Location (A10.51)",
            ScenarioA1RoutCalculator.RoutStepBar(row, [Enemy("e", 3)], 1, 2, []));
        Assert.Null(ScenarioA1RoutCalculator.RoutStepBar(row, [Enemy("e", 3)], 3, 2, []));
        Assert.Equal("play.rout-step: a routing unit never moves closer to the Known armed enemy unit e, which has had it in its LOS (A10.51)",
            ScenarioA1RoutCalculator.RoutStepBar(row, [Enemy("e", 4)], 1, 2, ["e"]));
        Assert.Null(ScenarioA1RoutCalculator.RoutStepBar(row, [Enemy("e", 4)], 1, 2, []));
    }

    [Fact]
    public void TheEntryCostIsTheInfantryEntryDoubledForAnEncircledUnitsFirstStep()
    {
        var row = new Row();
        Assert.Equal(new RoutStepCost(2, false, null), ScenarioA1RoutCalculator.RoutEntry(row, 0, 1, false));
        Assert.Equal(new RoutStepCost(4, false, null), ScenarioA1RoutCalculator.RoutEntry(row, 0, 1, true));
        Assert.Equal(new RoutStepCost(4, false, null), ScenarioA1RoutCalculator.RoutEntry(row, 2, 3, false));
    }

    [Fact]
    public void TheSearchReachesTheRowInOrderAndTheNearestCoverIsTheTarget()
    {
        var row = new Row();
        var reach = ScenarioA1RoutCalculator.RoutReach(row, [], 0, [], 0, 12, false, 7);
        Assert.Equal([0, 1, 2, 3, 4], reach.Keys);
        Assert.Equal(0, reach[0]);
        Assert.Equal(2, reach[1]);
        Assert.Equal(4, reach[2]);
        Assert.Equal(8, reach[3]);
        Assert.Equal(12, reach[4]);
        Assert.Equal([3], ScenarioA1RoutCalculator.RoutTargets(row, [], 0, reach));
        Assert.True(ScenarioA1RoutCalculator.CanRout(reach.Count));
    }

    [Fact]
    public void TheSearchKeepsOneLeastCostRouteToEachLocation()
    {
        var routes = new Dictionary<int, int[]>();
        ScenarioA1RoutCalculator.RoutReach(new Row(), [], 0, [], 0, 12, false, 7, routes: routes);
        Assert.Equal([1, 2, 3], routes[3]);
        Assert.Equal([1, 2, 3, 4], routes[4]);
    }

    [Fact]
    public void AnEnemyThatSawTheStartBarsEveryStepTowardIt()
    {
        var row = new Row();
        var reach = ScenarioA1RoutCalculator.RoutReach(row, [Enemy("e", 4)], 2, ["e"], 0, 12, false, 7);
        Assert.Equal([2, 1, 0], reach.Keys);
        Assert.Empty(ScenarioA1RoutCalculator.RoutTargets(row, [Enemy("e", 4)], 2, reach));
    }

    [Fact]
    public void TheSearchStaysInsideThePlayableAreaAndWithinTheAllowance()
    {
        Assert.Equal([0, 1, 2], ScenarioA1RoutCalculator.RoutReach(new Row().Unplayable(3), [], 0, [], 0, 12, false, 7).Keys);
        Assert.Equal([0, 1], ScenarioA1RoutCalculator.RoutReach(new Row(), [], 0, [], 0, 3, false, 7).Keys);
    }

    [Fact]
    public void TheRoutAdviceOffersOnlyRoutesThatStayInCoverAndDoNotEndAdjacentToTheStartsEnemy()
    {
        var row = new Row();
        var found = new Dictionary<int, int[]>();
        var reach = ScenarioA1RoutCalculator.RoutReach(row, [], 0, [], 0, 12, false, 7, routes: found);
        var (targets, canRout, routes) = ScenarioA1RoutCalculator.RoutAdvice(row, [], 0, reach, found);
        Assert.Equal([3], targets);
        Assert.True(canRout);
        Assert.Equal([1, 2, 3], routes[3]);
    }

    [Fact]
    public void TheTrapIsNoReachedLocationClearOfAnUnbrokenArmedEnemy()
    {
        // e at the far end cannot see the row's start, so the unit may step away from it and reach a Location clear of e.
        var row = new Row().Block(4, 0).Block(4, 1).Block(4, 2);
        var reach = ScenarioA1RoutCalculator.RoutReach(row, [Enemy("e", 4)], 0, [], 0, 4, false, 7);
        Assert.Equal([0, 1, 2], reach.Keys);
        Assert.False(ScenarioA1RoutCalculator.TrappedByInterdiction(row, [Enemy("e", 4)], 0, reach));
        Assert.True(ScenarioA1RoutCalculator.TrappedByInterdiction(row, [Enemy("e", 2)], 0, new Dictionary<int, int> { [0] = 0, [1] = 2 }));
    }

    [Fact]
    public void TheRouteWalkCostsEachStepAndGrowsTheSeenSet()
    {
        var row = new Row();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var (refusal, costs, spent) = ScenarioA1RoutCalculator.RoutRouteWalk(row, [Enemy("e", 4)], "u", 0, [1], false, false, 12, false, seen);
        Assert.Null(refusal);
        Assert.Equal([2], costs);
        Assert.Equal(2, spent);
        Assert.Contains("e", seen);
        // Once e has seen the unit, the next step toward it is barred (A10.51).
        Assert.Equal("play.rout-step: a routing unit never moves closer to the Known armed enemy unit e, which has had it in its LOS (A10.51)",
            ScenarioA1RoutCalculator.RoutRouteWalk(row, [Enemy("e", 4)], "u", 0, [1, 2], false, false, 12, false, []).Refusal);
        var (clear, twoCosts, twoSpent) = ScenarioA1RoutCalculator.RoutRouteWalk(row, [], "u", 0, [1, 2], false, false, 12, false, []);
        Assert.Null(clear);
        Assert.Equal([2, 2], twoCosts);
        Assert.Equal(4, twoSpent);
        Assert.Equal("play.rout-route: bd01:D1:0 is not ADJACENT to bd01:A1:0", ScenarioA1RoutCalculator.RoutRouteWalk(row, [], "u", 0, [3], false, false, 12, false, []).Refusal);
        Assert.Equal("play.rout-mf: the route costs more than u's 1 MF in the RtPh (A10.5)", ScenarioA1RoutCalculator.RoutRouteWalk(row, [], "u", 0, [1, 2], false, false, 2, false, []).Refusal);
    }

    [Fact]
    public void TheDestinationMustBeTheNearestCoverAndTheRoutGoesOnOnlyIntoCover()
    {
        var row = new Row();
        Assert.Null(ScenarioA1RoutCalculator.RoutDestinationBar(row, [], "u", 0, [1, 2, 3], false, 12, false, 7));
        Assert.Equal("play.rout-destination: u must rout to the nearest woods or building Location, bd01:D1:0 (A10.51)",
            ScenarioA1RoutCalculator.RoutDestinationBar(row, [], "u", 0, [1, 2], false, 12, false, 7));
        Assert.Equal("play.rout-route: u began ADJACENT to e and may not end its rout ADJACENT to it (A10.51)",
            ScenarioA1RoutCalculator.RoutDestinationBar(row, [Enemy("e", 1)], "u", 0, [1, 2], false, 12, false, 7));
    }

    [Fact]
    public void TheBarsReadTheirLazyFactsOnlyWhenTheyMatter()
    {
        Assert.Null(ScenarioA1RoutCalculator.RoutOrderBar(false, Never("the DEFENDER's routs")));
        Assert.False(ScenarioA1RoutCalculator.AttackerMustRoutFirst(true, false, false, () => false, Never("can rout"), Never("the surrender")));
        Assert.Equal("is Disrupted", ScenarioA1RoutCalculator.SurrenderCause(true, false, Never("the trap"), ["c"]));
        Assert.Null(ScenarioA1RoutCalculator.LowCrawlOccupiedBar(false, false, Never("the occupants")));
        Assert.Null(ScenarioA1RoutCalculator.InterdictionDue(true, true, () => throw new InvalidOperationException("the Interdictor was read.")));
        Assert.Equal("play.retain-dm: 'u' is not a broken unit under DM (A10.62)", ScenarioA1RoutCalculator.RetainDmBar("u", true, false, true, Never("the cover")));
        Assert.True(ScenarioA1RoutRallyProjection.MassacreUnitAllowed(true, true, true, false, false, true, true, () => throw new InvalidOperationException("the nationality was read.")));
        Assert.False(ScenarioA1RoutRallyProjection.SurrenderAllowed(true, false, 0, [], Never("the pending surrenders")));
    }

    [Fact]
    public void TheRoutMfOfAWoundedSmcIsOneMemberForThePlannerAndTheProjector()
    {
        Assert.Equal(6, ScenarioA1RoutCalculator.RoutHalfMf(true, true));
        Assert.Equal(12, ScenarioA1RoutCalculator.RoutHalfMf(true, false));
        Assert.Equal(12, ScenarioA1RoutCalculator.RoutHalfMf(false, true));
    }

    [Fact]
    public void AUnitThatHasSurrenderedBeforeTheOtherSideRoutsIsNoEnemyToItsRoutes()
    {
        // A19.12 (p. 86) and the Comprehensive Rout Example (p. 69): a Disrupted unit surrenders at the start of the RtPh, and an ATTACKER's unit
        // among the ATTACKER's routs, so both are prisoners when the other side routs; a DEFENDER's unit that is not Disrupted still stands
        // while the ATTACKER routs. Whether the unit is bound to surrender is read last.
        Assert.True(ScenarioA1RoutCalculator.SurrenderedBeforeTheOtherSideRouts(disrupted: true, attacker: false, () => true));
        Assert.True(ScenarioA1RoutCalculator.SurrenderedBeforeTheOtherSideRouts(disrupted: false, attacker: true, () => true));
        Assert.False(ScenarioA1RoutCalculator.SurrenderedBeforeTheOtherSideRouts(disrupted: true, attacker: true, () => false));
        Assert.False(ScenarioA1RoutCalculator.SurrenderedBeforeTheOtherSideRouts(disrupted: false, attacker: false, () => throw new InvalidOperationException("not read")));
    }

    [Fact]
    public void CasualtyReductionAndTheBrokenMorale()
    {
        Assert.Equal(CasualtyOutcome.Reduced, ScenarioA1RoutCalculator.CasualtyReduction(true, false, false, null));
        Assert.Equal(CasualtyOutcome.Wounded, ScenarioA1RoutCalculator.CasualtyReduction(false, true, false, 4));
        Assert.Equal(CasualtyOutcome.Eliminated, ScenarioA1RoutCalculator.CasualtyReduction(false, true, false, 5));
        Assert.Equal(CasualtyOutcome.Wounded, ScenarioA1RoutCalculator.CasualtyReduction(false, true, true, 3));
        Assert.Equal(CasualtyOutcome.Eliminated, ScenarioA1RoutCalculator.CasualtyReduction(false, true, true, 4));
        Assert.Equal(CasualtyOutcome.Eliminated, ScenarioA1RoutCalculator.CasualtyReduction(false, false, false, null));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1RoutCalculator.CasualtyReduction(false, true, false, null));

        // Pass 35, task 35.1 (A17.11): the one wound procedure, and the PF firer's Casualty Reduction by it.
        Assert.False(ScenarioA1Wounds.Mortal(4, false));
        Assert.True(ScenarioA1Wounds.Mortal(5, false));
        Assert.True(ScenarioA1Wounds.Mortal(4, true));
        Assert.Equal(ScenarioA1Wounds.Mortal(4, true), ScenarioA1Sniper.Mortal(4, true));
        Assert.Equal(FirerCasualty.Wounded, ScenarioA1OrdnanceEventRules.Casualty("asl:leader", () => false, true, 3));
        Assert.Equal(FirerCasualty.Eliminated, ScenarioA1OrdnanceEventRules.Casualty("asl:leader", () => false, false, 6));
        Assert.Equal(FirerCasualty.HalfSquad, ScenarioA1OrdnanceEventRules.Casualty("asl:squad", () => true, false, null));
        Assert.Equal(FirerCasualty.Eliminated, ScenarioA1OrdnanceEventRules.Casualty("asl:half-squad", () => false, false, null));
        Assert.Equal(7, ScenarioA1RoutCalculator.BrokenMorale(true, 8, 7, true));
        Assert.Equal(7, ScenarioA1RoutCalculator.BrokenMorale(true, null, 7, false));
        Assert.Null(ScenarioA1RoutCalculator.BrokenMorale(false, 8, 7, false));

        // Pass 35, task 35.4 (A10.8, A.18): a Fanatic unit's broken Morale Level is one higher, never above 10.
        Assert.Equal(8, ScenarioA1RoutCalculator.BrokenMorale(true, 7, 7, false, fanatic: true));
        Assert.Equal(10, ScenarioA1RoutCalculator.BrokenMorale(true, 10, 10, false, fanatic: true));

        // A20.21, A25.22: a Commissar never surrenders by the RtPh method.
        Assert.True(ScenarioA1RoutCalculator.SurrenderCandidate(false, false, false));
        Assert.False(ScenarioA1RoutCalculator.SurrenderCandidate(false, false, false, commissar: true));
        Assert.True(ScenarioA1RoutCalculator.SurrendersInstead(false, false, false));
        Assert.False(ScenarioA1RoutCalculator.SurrendersInstead(false, false, false, commissar: true));

        // A10.533 (p. 68): a unit repulsed from a concealed unit's Location is eliminated, not taken prisoner.
        Assert.False(ScenarioA1RoutCalculator.SurrendersInstead(false, false, false, repulsed: true));
    }

    [Fact]
    public void TheRoutSummaryWordsTheStepsTheInterdictionAndWhatIsLeft()
    {
        var words = ScenarioA1RoutCalculator.RoutSummary("u", false, [("bd01:B1:0", 2), ("bd01:C1:0", 3)], [("bd01:C1:0", "e")], [("w1", 3)], [], "bd01:A1:0");
        Assert.Equal("play.rout: u routs to bd01:B1:0 (1 MF), bd01:C1:0 (1.5 MF); Interdicted as it enters bd01:C1:0 by e, a NMC each (A10.53)", words[0]);
        Assert.Equal("play.rout-leaves: u leaves w1 (3 PP) in bd01:A1:0, unpossessed, and routs with no SW (A10.4, A4.431)", words[1]);
        Assert.Equal("play.rout: u routs by Low Crawl to bd01:B1:0 (3 MF); Low Crawl is never Interdicted (A10.52)", ScenarioA1RoutCalculator.RoutSummary("u", true, [("bd01:B1:0", 6)], [], [], [], "x")[0]);
    }

    [Fact]
    public void TheRallyAttemptDeclaresTheDerivedFacts()
    {
        var unit = new RallyUnitStateFacts("u", "def", "bd01:A1:0", true, false, false, true, false, false, false, true, true, null);
        var attempt = ScenarioA1RallyRules.Attempt(new RallyAttemptStateFacts("rph", true, unit, null, "woods", [true, false], false, null, null, null, [], false, null, true));
        Assert.Equal("RPh", attempt.Phase);
        Assert.Equal("phasing", attempt.RallyingSide);
        Assert.True(attempt.GoodOrderLeaderInLocation);
        Assert.True(attempt.BrokenLeaderInLocation);
        Assert.True(attempt.FirstMmcRallyOfOwnPlayerTurn);
        Assert.True(attempt.Unit!.OtherActionThisPhase);
        Assert.True(attempt.Unit.Fanatic);
        Assert.Null(attempt.Companions);
        Assert.True(attempt.ExtremeWinterFate);
        Assert.Null(attempt.NoQuarter);
        var inBuilding = ScenarioA1RallyRules.Attempt(new RallyAttemptStateFacts("mph", false, unit with
        {
            Fanatic = false
        }, null, "stone-building", [], true, null, null, null, [], true, "k", true));
        Assert.Equal("mph", inBuilding.Phase);
        Assert.Equal("non-phasing", inBuilding.RallyingSide);
        Assert.False(inBuilding.FirstMmcRallyOfOwnPlayerTurn);
        Assert.Null(inBuilding.ExtremeWinterFate);
        Assert.Null(inBuilding.Unit!.Fanatic);
        Assert.True(inBuilding.NoQuarter);
        Assert.Equal("k", inBuilding.Commissar);
    }

    [Fact]
    public void TheRallyPackagesNextStepIsNamedFromItsReasons()
    {
        Assert.True(ScenarioA1RallyRules.NextStep(RallyResolution.Resolved, []).Resolved);
        Assert.Equal("battle-hardening", ScenarioA1RallyRules.NextStep("undecided", ["asl.a1.rally.choice-missing:battle-hardening"]).ChoiceKey);
        var rally = ScenarioA1RallyRules.NextStep("undecided", ["asl.a1.rally.roll-missing:rally"]);
        Assert.Equal(("rally", 2, "rally"), (rally.RollKey, rally.Count, rally.Purpose));
        var heat = ScenarioA1RallyRules.NextStep("undecided", ["asl.a1.rally.roll-missing:heatOfBattle"]);
        Assert.Equal(("heatOfBattle", 2, "rally-heat-of-battle"), (heat.RollKey, heat.Count, heat.Purpose));
        var severity = ScenarioA1RallyRules.NextStep("undecided", ["asl.a1.rally.roll-missing:woundSeverity"]);
        Assert.Equal(("woundSeverity", 1, "rally-wound-severity"), (severity.RollKey, severity.Count, severity.Purpose));
        var check = ScenarioA1RallyRules.NextStep("undecided", ["asl.a1.rally.roll-missing:berserkCheck:c1"]);
        Assert.Equal(("berserkCheck:c1", 2, "rally-berserk-check"), (check.RollKey, check.Count, check.Purpose));
        Assert.Equal("a; b", ScenarioA1RallyRules.NextStep("undecided", ["a", "b"]).Undecided);
    }

    [Fact]
    public void WhichSelfRallyRuleBarsTheUnit()
    {
        Assert.Equal(SelfRallyBar.None, ScenarioA1RallyRules.SelfRallyRefusal(["asl.a1.rally.other"], true, false));
        Assert.Equal(SelfRallyBar.NotOwnPhase, ScenarioA1RallyRules.SelfRallyRefusal(["asl.a1.rally.self-rally-not-capable"], false, false));
        Assert.Equal(SelfRallyBar.Used, ScenarioA1RallyRules.SelfRallyRefusal(["asl.a1.rally.self-rally-not-capable"], true, true));
        Assert.Equal(SelfRallyBar.BrokenLeader, ScenarioA1RallyRules.SelfRallyRefusal(["asl.a1.rally.self-rally-not-capable"], true, false));
    }

    [Fact]
    public void TheRalliedUnitsConditionsComeInTheRecordsOrder()
    {
        Assert.Equal([(UnitCondition.Broken, false), (UnitCondition.Disrupted, false)], ScenarioA1RallyRules.RalliedUnitConditions(true, true, null, false, null, null, null, false, false, false));
        Assert.Equal([(UnitCondition.Berserk, true), (UnitCondition.DesperationMorale, false), (UnitCondition.Concealed, false), (UnitCondition.Disrupted, true), (UnitCondition.Concealed, false)],
            ScenarioA1RallyRules.RalliedUnitConditions(false, false, null, false, null, true, true, false, false, true));
        Assert.Equal([(UnitCondition.Heroic, true), (UnitCondition.Wounded, true)], ScenarioA1RallyRules.RalliedUnitConditions(false, false, true, true, true, null, null, true, false, false));
        Assert.Empty(ScenarioA1RallyRules.RalliedUnitConditions(false, false, null, false, null, null, null, true, true, false));
    }

    [Fact]
    public void TheReplacingUnitKeepsItsConcealmentOnlyWhenBattleHardened()
    {
        Assert.Equal([(UnitCondition.Concealed, (bool?)false), (UnitCondition.Hidden, false)], ScenarioA1RallyRules.ReplacedUnitConditions(false, false, true, true));
        Assert.Equal([(UnitCondition.Concealed, (bool?)null), (UnitCondition.Hidden, false), (UnitCondition.Broken, false), (UnitCondition.Pinned, false), (UnitCondition.Disrupted, false),
            (UnitCondition.DesperationMorale, false), (UnitCondition.Fanatic, true), (UnitCondition.Heroic, true)], ScenarioA1RallyRules.ReplacedUnitConditions(true, false, true, true));
        Assert.Equal([(UnitCondition.Concealed, (bool?)false), (UnitCondition.Hidden, false), (UnitCondition.Broken, false), (UnitCondition.Pinned, false), (UnitCondition.Disrupted, false),
            (UnitCondition.DesperationMorale, false)], ScenarioA1RallyRules.ReplacedUnitConditions(true, true, null, null));
        Assert.True(ScenarioA1RallyRules.ReplacedNotReduced(false, true, true));
        Assert.False(ScenarioA1RallyRules.ReplacedNotReduced(false, true, false));
        Assert.True(ScenarioA1RallyRules.BattleHardened(["rallied", "battle-hardened"]));
    }

    [Theory]
    [InlineData(1, 3, RepairOutcome.Repaired)]
    [InlineData(3, 3, RepairOutcome.Repaired)]
    [InlineData(4, 3, RepairOutcome.NoChange)]
    [InlineData(6, 6, RepairOutcome.Eliminated)]
    public void ASwRepairDrAtMostTheRepairNumberRepairsAndASixEliminates(int dr, int repairNumber, RepairOutcome outcome) =>
        Assert.Equal(outcome, ScenarioA1RallyRules.SwRepairResult(dr, repairNumber));

    [Theory]
    [InlineData(1, RepairOutcome.Repaired)]
    [InlineData(2, RepairOutcome.NoChange)]
    [InlineData(6, RepairOutcome.Eliminated)]
    public void AVehicleMgRepairDrOfOneRepairsAndASixDisables(int dr, RepairOutcome outcome)
    {
        Assert.Equal(outcome, ScenarioA1RallyRules.VehicleMgRepairResult(dr));
        if (outcome != RepairOutcome.NoChange)
        {
            Assert.Equal(outcome == RepairOutcome.Repaired ? (UnitCondition.Malfunctioned, false) : (UnitCondition.Disabled, true), ScenarioA1RallyRules.VehicleRepairChange(outcome));
        }
    }

    [Fact]
    public void TheRepairBarsOfThePlannerAndTheProjectorStayTheirOwn()
    {
        Assert.Null(ScenarioA1RallyRules.VehicleRepairCrewBarAsPlanned("v", true, false));
        Assert.NotNull(ScenarioA1RallyRules.VehicleRepairCrewBarAsPlanned("v", false, false));
        Assert.True(ScenarioA1RoutRallyProjection.VehicleRepairAllowedAsRecorded("rph", true, false, false, false, false, false, false, false));
        Assert.False(ScenarioA1RoutRallyProjection.VehicleRepairAllowedAsRecorded("rph", true, false, true, false, false, false, false, false));
        Assert.True(ScenarioA1RallyRules.SwRepairGoodOrderAsPlanned(false));
        Assert.False(ScenarioA1RallyRules.SwRepairGoodOrderAsPlanned(null));
        Assert.True(ScenarioA1RoutRallyProjection.SwRepairRecordAgrees(3, 3, true));
        Assert.False(ScenarioA1RoutRallyProjection.SwRepairRecordAgrees(null, 3, true));
        Assert.True(ScenarioA1RoutRallyProjection.VehicleRepairRecordAgrees(1, true));
        Assert.False(ScenarioA1RoutRallyProjection.VehicleRepairRecordAgrees(2, true));
    }

    [Fact]
    public void ShockRecoveryOwesOneDrInTheRphAndItsRecordAgreesWithTheDr()
    {
        Assert.True(ScenarioA1RallyRules.OwesShockRoll(true, true, true, false, false));
        Assert.False(ScenarioA1RallyRules.OwesShockRoll(true, true, true, false, true));
        Assert.False(ScenarioA1RallyRules.OwesShockRoll(true, false, true, false, false));
        Assert.True(ScenarioA1RallyRules.ShockRecoveryWrecks(ScenarioA1ResultTables.ShockWrecked));
        Assert.Equal([(UnitCondition.Shocked, false), (UnitCondition.UnconfirmedKill, true)], ScenarioA1RallyRules.ShockRecoveryConditions(ScenarioA1ResultTables.ShockUnconfirmedKill));
        Assert.True(ScenarioA1RoutRallyProjection.ShockRecoveryAgrees(ScenarioA1ResultTables.ShockWrecked, true, 5));
        Assert.False(ScenarioA1RoutRallyProjection.ShockRecoveryAgrees(ScenarioA1ResultTables.ShockRecovered, true, 5));
        Assert.True(ScenarioA1RoutRallyProjection.ShockRecoveryAllowed(true, false, false, true));
    }

    [Theory]
    [InlineData(true, false, false, 2, false, 0, false, 12, false, true)]
    [InlineData(false, false, false, 2, false, 0, false, 12, false, false)]
    [InlineData(true, true, false, 2, false, 0, false, 12, false, false)]
    [InlineData(true, false, false, 2, false, 5, true, 12, false, false)]
    [InlineData(true, false, false, 12, true, 5, true, 12, false, true)]
    [InlineData(true, false, false, 12, true, 0, false, 12, true, false)]
    [InlineData(true, false, false, -1, false, 0, false, 12, false, false)]
    public void ARoutStepMovesABrokenUnitWithinItsMf(bool broken, bool melee, bool pinned, int halfMf, bool lowCrawl, int mfSpent, bool halfMfSpent, int routHalfMf, bool routed, bool allowed) =>
        Assert.Equal(allowed, ScenarioA1RoutRallyProjection.RoutStepAllowed(broken, melee, pinned, halfMf, lowCrawl, mfSpent, halfMfSpent, routHalfMf, routed));

    [Fact]
    public void TheProjectorsRoutAndMassacreArithmetic()
    {
        Assert.Equal((3, true), ScenarioA1RoutRallyProjection.RoutMfSpent(2, false, 3));
        Assert.True(ScenarioA1RoutRallyProjection.InterdictionAgrees(6, 6, 0, 7, ScenarioA1ResultTables.InterdictionEliminated));
        Assert.True(ScenarioA1RoutRallyProjection.InterdictionAgrees(3, 4, 1, 8, ScenarioA1ResultTables.InterdictionPinned));
        Assert.False(ScenarioA1RoutRallyProjection.InterdictionAgrees(3, 4, 1, 8, ScenarioA1ResultTables.InterdictionPassed));
        Assert.True(ScenarioA1RoutRallyProjection.MassacreFirePhase("pfph", true));
        Assert.False(ScenarioA1RoutRallyProjection.MassacreFirePhase("pfph", false));
        Assert.True(ScenarioA1RoutRallyProjection.MassacreFirePhase("dfph", false));
        Assert.False(ScenarioA1RoutRallyProjection.MassacreFirePhase("mph", true));
        Assert.Equal(["a", "b"], ScenarioA1RoutRallyProjection.NoQuarterAfter(["a"], "b"));
        Assert.Equal(["a"], ScenarioA1RoutRallyProjection.NoQuarterAfter(["a"], "a"));
        Assert.Equal(6, ScenarioA1RoutRallyProjection.MassacreRaisedElr(6));
        Assert.Equal(4, ScenarioA1RoutRallyProjection.MassacreRaisedElr(3));
        Assert.Null(ScenarioA1RoutRallyProjection.MassacreRaisedElr(null));
        Assert.False(ScenarioA1RoutRallyProjection.MassacreUnitAllowed(true, true, true, false, false, false, false, () => "german"));
        Assert.True(ScenarioA1RoutRallyProjection.MassacreUnitAllowed(true, true, true, false, false, false, false, () => "russian"));
        Assert.Equal("UNIT-STATE-036", ScenarioA1RoutRallyProjection.MassacreRefusal().Code);
    }

    [Fact]
    public void TheSurrenderNamesABrokenUnitAndActiveEnemyCaptors()
    {
        Assert.True(ScenarioA1RoutRallyProjection.SurrenderAllowed(true, false, 1, [(true, true)], () => false));
        Assert.False(ScenarioA1RoutRallyProjection.SurrenderAllowed(true, false, 2, [(true, true), (true, false)], () => false));
        Assert.False(ScenarioA1RoutRallyProjection.SurrenderAllowed(true, false, 1, [(true, true)], () => true));
        Assert.Equal("UNIT-STATE-031", ScenarioA1RoutRallyProjection.RejectSurrenderRefusal("u").Code);
    }

    [Fact]
    public void FailureToRoutAndTheFreedSmc()
    {
        var row = new Row();
        Assert.Equal("it is ADJACENT to or in the Location of the Known unbroken armed enemy unit e", ScenarioA1RoutCalculator.FailureToRoutWhy(row, [Enemy("e", 1)], 0, false, false, 7));
        Assert.Equal("it did not rout from Open Ground in the LOS and Normal Range of e", ScenarioA1RoutCalculator.FailureToRoutWhy(row, [Enemy("e", 4)], 0, false, false, 7));
        Assert.Null(ScenarioA1RoutCalculator.FailureToRoutWhy(row, [Enemy("e", 4)], 0, true, false, 7));
        Assert.True(ScenarioA1RoutCalculator.NoFailureToRout(true));
        Assert.True(ScenarioA1RoutCalculator.FreedUnarmedSmc(true, true, true, true, false));
        Assert.False(ScenarioA1RoutCalculator.FreedUnarmedSmc(true, false, true, true, false));
        Assert.True(ScenarioA1RoutCalculator.ArmsFreedSmc(2, false));
        Assert.False(ScenarioA1RoutCalculator.ArmsFreedSmc(2, true));
        Assert.False(ScenarioA1RoutCalculator.ArmsFreedSmc(0, false));
        Assert.Equal("play.smc-armed: a, b are free and so Armed again (A20.551; ruling R31.8)", ScenarioA1RoutCalculator.SmcArmedReason(["a", "b"]));
        Assert.Equal("play.smc-armed: a is free and so Armed again (A20.551; ruling R31.8)", ScenarioA1RoutCalculator.SmcArmedReason(["a"]));
    }
}
