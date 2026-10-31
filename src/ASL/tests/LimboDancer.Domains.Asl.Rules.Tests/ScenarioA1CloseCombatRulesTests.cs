using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 32.g: the Close Combat, advance, charge, prisoner, Mopping Up, and vehicle CC decisions moved into Rules, read directly: the advance's bars and
/// arithmetic, the round's order and what the package asks for, the charge's search over a fact reader of a small row, the ordered condition lists, the
/// captors, Mopping Up's drm, the vehicle CC's sides, the fact builders' round, and the projector's verdicts.
/// </summary>
public sealed class ScenarioA1CloseCombatRulesTests
{
    /// <summary>A row of five ground-level hexes, 0 to 4, each ADJACENT to the next, every step costing one MF, no Bypass, every LOS clear.</summary>
    private sealed class Row : IChargeFactReader
    {
        private readonly HashSet<int> unplayable = [];

        public Row Unplayable(int location)
        {
            unplayable.Add(location);
            return this;
        }

        public string Name(int location) => "bd01:" + "ABCDE"[location] + "1:0";

        public int Level(int location) => 0;

        public IEnumerable<int> Neighbors(int location)
        {
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

        public ChargeEntryFacts EntryCost(int fromLocation, int toLocation) => new(2, null, null);

        public int? Across(int location, int side) => null;

        public int? SideToward(int fromLocation, int toLocation) => null;

        public bool Reads(int location) => false;

        public int? BypassHalfMf(int obstacle, int side, IReadOnlyList<int> lane, int fromLocation) => null;

        public string LaneKey(IReadOnlyList<int> lane) => string.Join(",", lane);

        public IReadOnlyList<int> Lane(string key) => [.. key.Split(',').Select(int.Parse)];

        public bool LosClear(int fromLocation, int toLocation) => true;

        public int? Distance(int one, int two) => Math.Abs(one - two);
    }

    [Fact]
    public void TheAdvanceBarsComeInTheirOrder()
    {
        Assert.NotNull(ScenarioA1AdvanceCalculator.AdvancePhaseBar("mph"));
        Assert.Null(ScenarioA1AdvanceCalculator.AdvancePhaseBar("aph"));
        Assert.Equal("broken", ScenarioA1AdvanceCalculator.AdvanceBarringCondition(true, true, false, false, false, false, false));
        Assert.Equal("ti", ScenarioA1AdvanceCalculator.AdvanceBarringCondition(false, false, false, false, false, false, true));
        Assert.Null(ScenarioA1AdvanceCalculator.AdvanceBarringCondition(false, false, false, false, false, false, false));
        Assert.NotNull(ScenarioA1AdvanceCalculator.MarshBar(true));
        Assert.Null(ScenarioA1AdvanceCalculator.CxDifficultBar("g1", false, true));
        Assert.NotNull(ScenarioA1AdvanceCalculator.CxDifficultBar("g1", true, true));
    }

    [Theory]
    [InlineData(3, 0, 0, 0)]
    [InlineData(4, 0, 0, 1)]
    [InlineData(3, 1, 0, 1)]
    [InlineData(2, 2, 5, 1)]
    [InlineData(0, 0, 5, 0)]
    public void OverstackingCountsSquadEquivalentsAboveThree(int squads, int halfSquads, int smc, int excess) =>
        Assert.Equal(excess, new StackCount(squads, halfSquads, 0, smc, 0).PersonnelExcess);

    [Fact]
    public void TheAdvanceMfDoublesWhenEncircledAndAddsTwoPerExcess()
    {
        Assert.Equal(2, ScenarioA1AdvanceCalculator.AdvanceHalfMf(2, false, 0));
        Assert.Equal(4, ScenarioA1AdvanceCalculator.AdvanceHalfMf(2, true, 0));
        Assert.Equal(6, ScenarioA1AdvanceCalculator.AdvanceHalfMf(2, false, 2));
        Assert.True(ScenarioA1AdvanceCalculator.DifficultAdvance(4, 8));
        Assert.False(ScenarioA1AdvanceCalculator.DifficultAdvance(4, 6));
        Assert.Null(ScenarioA1AdvanceCalculator.DifficultAdvance(null, 6));
        Assert.Null(ScenarioA1AdvanceCalculator.DifficultAdvance(0, 6));
    }

    [Fact]
    public void ALeaderAidsTheMmcOfHisNationalityAndTakesOneLadenMmcsPortage()
    {
        var (aided, ipcTo) = ScenarioA1AdvanceCalculator.AdvanceAid(
        [
            new AdvancingUnitFacts("g1", true, false, "german", false, false, true),
            new AdvancingUnitFacts("g2", true, false, "german", false, false, false),
            new AdvancingUnitFacts("r1", true, false, "russian", false, false, false),
            new AdvancingUnitFacts("l1", false, true, "german", false, false, false),
        ]);
        Assert.Equal(["g1", "g2"], aided.Order(StringComparer.Ordinal));
        Assert.Equal("g1", ipcTo);
    }

    [Fact]
    public void TheAdvancingSideReadsKnownUnitsByIdAndOthersAsOneStack()
    {
        Assert.Equal("r1, r2", ScenarioA1AdvanceCalculator.Opposing([new OpposingCounterFacts("r1", true, false), new OpposingCounterFacts("r2", true, false)]));
        Assert.Equal("r1 and a concealed stack", ScenarioA1AdvanceCalculator.Opposing([new OpposingCounterFacts("r1", true, false), new OpposingCounterFacts("r2", false, false)]));
        Assert.Equal(string.Empty, ScenarioA1AdvanceCalculator.Opposing([new OpposingCounterFacts("r1", false, true)]));
    }

    [Fact]
    public void TheClosestKnownEnemyInLosIsCharged()
    {
        var row = new Row();
        var (steps, target, undecided) = ScenarioA1ChargeCalculator.Steps(row, 0, [3], null, () => new ChargeNodeFacts(0, null, null));
        Assert.Null(undecided);
        Assert.Equal(3, target);
        var step = Assert.Single(steps);
        Assert.Equal(1, step.Key);
        Assert.Equal(new ChargeSearchStep(3, 2, true, []), step.Value);
    }

    [Fact]
    public void ANearerEnemyIsPreferredAndEnemiesElsewhereAreNotPassedThrough()
    {
        var row = new Row();
        var (steps, target, _) = ScenarioA1ChargeCalculator.Steps(row, 0, [2, 4], null, () => new ChargeNodeFacts(0, null, null));
        Assert.Equal(2, target);
        Assert.Equal([1], steps.Keys);

        // From 3, the enemy at 2 is the target; the enemy at 0 is behind it and never on the route.
        var (back, backTarget, _) = ScenarioA1ChargeCalculator.Steps(row, 3, [0, 2], null, () => new ChargeNodeFacts(3, null, null));
        Assert.Equal(2, backTarget);
        Assert.Equal([2], back.Keys);
    }

    [Fact]
    public void AChargeWithNoRouteIsUndecidedAndNamesItsLocations()
    {
        var row = new Row().Unplayable(1);
        var (steps, target, undecided) = ScenarioA1ChargeCalculator.Steps(row, 0, [3], null, () => new ChargeNodeFacts(0, null, null));
        Assert.Empty(steps);
        Assert.Equal(3, target);
        Assert.Equal("play.charge-no-route: the game allows a route to no Known enemy unit in its LOS (the nearest bd01:D1:0), so the charge ends in place (ruling R30.5)", undecided);
    }

    [Fact]
    public void AChargeKeepsToItsLocationUntilACloserEnemyAppears()
    {
        var row = new Row();
        var (steps, target, _) = ScenarioA1ChargeCalculator.Steps(row, 0, [4], 2, () => new ChargeNodeFacts(0, null, null));
        Assert.Equal(2, target);
        Assert.Equal([1], steps.Keys);
        var (here, hereTarget, hereUndecided) = ScenarioA1ChargeCalculator.Steps(row, 0, [0, 4], null, () => new ChargeNodeFacts(0, null, null));
        Assert.Empty(here);
        Assert.Equal(0, hereTarget);
        Assert.Null(hereUndecided);
    }

    [Fact]
    public void TheHalfMfLeftAndTheAffordableStep()
    {
        Assert.Equal(7, ScenarioA1ChargeCalculator.HalfMfLeft(4, 0, true));
        Assert.Equal(0, ScenarioA1ChargeCalculator.HalfMfLeft(null, 0, false));
        Assert.True(ScenarioA1ChargeCalculator.Affordable(4, 3, false, 2));
        Assert.False(ScenarioA1ChargeCalculator.Affordable(4, 3, true, 2));
        Assert.True(ScenarioA1ChargeCalculator.ChargeTarget(true, true, false));
        Assert.False(ScenarioA1ChargeCalculator.ChargeTarget(true, true, true));
    }

    [Fact]
    public void ThePackagesNextRollIsReadWithItsDiceAndPurpose()
    {
        var attack = ScenarioA1CloseCombatRules.NextStep("undecided", ["asl.a1.cc.roll-missing:attack:1"]);
        Assert.Equal(("attack", "1", 2, "cc-attack"), (attack.Kind, attack.Rest, attack.Count, attack.Purpose));
        var selection = ScenarioA1CloseCombatRules.NextStep("undecided", ["asl.a1.cc.roll-missing:randomSelection:1:g1,g2"]);
        Assert.Equal(["g1", "g2"], selection.Selected);
        Assert.Equal(2, selection.Count);
        Assert.True(ScenarioA1CloseCombatRules.NextStep(CloseCombatResolution.Resolved, []).Resolved);
        Assert.Equal("a; b", ScenarioA1CloseCombatRules.NextStep("undecided", ["a", "b"]).Undecided);
        Assert.Equal("german", ScenarioA1CloseCombatRules.AmbushRollSide(["asl.a1.cc.roll-missing:ambush:german"]));
        Assert.Null(ScenarioA1CloseCombatRules.AmbushRollSide(["asl.a1.cc.roll-missing:attack:1"]));
    }

    [Fact]
    public void TheSidesThatMayDeclareFollowTheAmbush()
    {
        string[] sides = ["german", "russian"];
        Assert.Equal(sides, ScenarioA1CloseCombatRules.DeclaringSides(sides, null, 0, false));
        Assert.Equal(["russian"], ScenarioA1CloseCombatRules.DeclaringSides(sides, "russian", 0, false));
        Assert.Equal(["german"], ScenarioA1CloseCombatRules.DeclaringSides(sides, "russian", 1, false));
        Assert.Equal(["russian"], ScenarioA1CloseCombatRules.DeclaringSides(sides, "russian", 1, true));
    }

    [Fact]
    public void TheRequiredCcUnitIsTheBerserkOneThenTheReinforcingOne()
    {
        RequiredCcUnitFacts[] here =
        [
            new("g1", "german", false, true, false, true, true, false),
            new("r1", "russian", true, true, false, true, false, false),
        ];
        Assert.Equal(("r1", true), ScenarioA1CloseCombatRules.RequiredUnit(here)!.Value);
        Assert.Equal(("g1", false), ScenarioA1CloseCombatRules.RequiredUnit([here[0], here[1] with { Berserk = false }])!.Value);
        Assert.Null(ScenarioA1CloseCombatRules.RequiredUnit([here[0], here[1] with { Berserk = false, Vehicle = true }]));
        Assert.Null(ScenarioA1CloseCombatRules.RequiredUnit([here[0] with { Melee = false }, here[1] with { Berserk = false, Melee = false }]));
    }

    [Fact]
    public void TheDueLinesAndNothingLeft()
    {
        // Pass 72 (ruling R72.5): a charge into a Location holding an enemy vehicle and enemy Infantry is no longer barred, so ChargeBarred is gone.
        Assert.Equal("bd01:A1:0: the german side attacks or passes (A11.31)", ScenarioA1CloseCombatRules.DueOpenLine("bd01:A1:0", "german", null));
        Assert.Equal("bd01:A1:0: its round after the Ambush drs (A11.12)", ScenarioA1CloseCombatRules.DueOpenLine("bd01:A1:0", null, null));
        Assert.Equal("bd01:A1:0: the Ambush drs (A11.4)", ScenarioA1CloseCombatRules.DueAmbushLine("bd01:A1:0", null));
        Assert.True(ScenarioA1CloseCombatRules.NothingLeft(true, 1, false));
        Assert.False(ScenarioA1CloseCombatRules.NothingLeft(true, 0, false));
    }

    [Fact]
    public void AKnownEnemyInLosIsReadOverTheEnemiesLocationsInOrder()
    {
        var reads = new List<int>();
        bool? Los(int location)
        {
            reads.Add(location);
            return location == 2 ? true : location == 1 ? false : null;
        }

        Assert.True(ScenarioA1PrisonerCalculator.KnownEnemyInLos([1, 2, 3], 0, Los));
        Assert.Equal([1, 2], reads);
        Assert.Null(ScenarioA1PrisonerCalculator.KnownEnemyInLos([1, 3], 0, Los));
        Assert.False(ScenarioA1PrisonerCalculator.KnownEnemyInLos([1], 0, Los));
        Assert.True(ScenarioA1PrisonerCalculator.KnownEnemyInLos([1], 1, location => throw new InvalidOperationException("The LOS was read.")));
    }

    [Fact]
    public void TheCaptorsWithGuardCapacityComeFirstByIdThenEveryCaptor()
    {
        CaptorFacts[] adjacent = [new("r2", true, false), new("r1", true, true), new("r3", false, true)];
        Assert.Equal(["r1"], ScenarioA1PrisonerCalculator.Captors(adjacent));
        Assert.Equal(["r1", "r2"], ScenarioA1PrisonerCalculator.Captors([adjacent[0], adjacent[1] with { CanGuard = false }, adjacent[2]]));
        Assert.True(ScenarioA1PrisonerCalculator.CanGuard(12, 3, 3));
        Assert.False(ScenarioA1PrisonerCalculator.CanGuard(13, 3, 3));
    }

    [Fact]
    public void TheRoundsConditionsKeepTheRecordsOrder()
    {
        Assert.Equal([(UnitCondition.Unarmed, false), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)], ScenarioA1PrisonerCalculator.RoundConditions(true, true));
        Assert.Equal([(UnitCondition.Wounded, true), (UnitCondition.Berserk, false)], ScenarioA1PrisonerCalculator.AfterRoundConditions(true, false, true));
        Assert.Empty(ScenarioA1PrisonerCalculator.AfterRoundConditions(true, true, false));
        Assert.Equal([(UnitCondition.Unarmed, false), (UnitCondition.Captured, false)], ScenarioA1PrisonerCalculator.RearmedConditions(true));
        Assert.Equal(("cc-1-g1-a", "cc-1-g1-b"), ScenarioA1PrisonerCalculator.CapturedHalfIds("cc-1", "g1"));
        Assert.Equal("bd01:B1:0", ScenarioA1PrisonerCalculator.CreatedLeaderPlacedAt("bd01:B1:0", "bd01:A1:0"));
        Assert.Null(ScenarioA1PrisonerCalculator.MovesAfterRound("bd01:B1:0", null, "g1-c"));
    }

    [Fact]
    public void TheSidesWithVehicleCcAttacksLeftAndTheirOrder()
    {
        VehicleCcUnitFacts[] here =
        [
            new("g1", "german", false, false, true, true, false, false, true),
            new("t1", "russian", true, true, false, false, false, false, true),
        ];
        Assert.Equal(["german", "russian"], ScenarioA1VehicleCloseCombatRules.SidesWithAttacks(here, [], []).Order(StringComparer.Ordinal));
        Assert.Equal(["russian"], ScenarioA1VehicleCloseCombatRules.SidesWithAttacks(here, ["g1"], []));
        Assert.Equal(["german"], ScenarioA1VehicleCloseCombatRules.SidesWithAttacks(here, [], ["russian"]));
        Assert.Equal("german", ScenarioA1VehicleCloseCombatRules.FirstCcSide([("german", false), ("russian", true)], "russian"));
        Assert.Equal("russian", ScenarioA1VehicleCloseCombatRules.FirstCcSide([("german", true), ("russian", true)], "russian"));
        Assert.Equal(("russian", false), ScenarioA1VehicleCloseCombatRules.NextCcSide(["german", "russian"], "german"));
        Assert.Equal(("german", false), ScenarioA1VehicleCloseCombatRules.NextCcSide(["german"], "german"));
        Assert.Equal(((string?)null, true), ScenarioA1VehicleCloseCombatRules.NextCcSide([], "german"));
        Assert.Equal(("g1", true, "the enemy vehicle"), ScenarioA1VehicleCloseCombatRules.OwingAttack([here[0] with { Berserk = true }, here[1]], null, [], null));
        Assert.Null(ScenarioA1VehicleCloseCombatRules.OwingAttack([here[0] with { Berserk = true }, here[1]], null, ["g1"], null));
    }

    [Fact]
    public void TheVehicleRollsAndTheReactionFireMarks()
    {
        Assert.Equal(("attack:1", "attack", "1"), ScenarioA1VehicleCloseCombatRules.VehicleRollMissing(["asl.a1.cc-vehicle.roll-missing:attack:1"]));
        Assert.Equal(("unlikelyKill", "unlikelyKill", string.Empty), ScenarioA1VehicleCloseCombatRules.VehicleRollMissing(["asl.a1.cc-vehicle.roll-missing:unlikelyKill"]));
        Assert.Null(ScenarioA1VehicleCloseCombatRules.VehicleRollMissing(["asl.a1.cc-vehicle.refused"]));
        Assert.Equal([(UnitCondition.FinalFire, true), (UnitCondition.FirstFire, false), (UnitCondition.CcReaction, true)], ScenarioA1VehicleCloseCombatRules.ReactionFireConditions(true, false));
        Assert.Equal([(UnitCondition.FirstFire, true)], ScenarioA1VehicleCloseCombatRules.ReactionFireConditions(false, true));
        Assert.True(ScenarioA1VehicleCloseCombatRules.Destroyed(VehicleCloseCombatResolution.BurningWreck));
        Assert.False(ScenarioA1VehicleCloseCombatRules.Destroyed(VehicleCloseCombatResolution.Immobilized));
    }

    [Fact]
    public void MoppingUpsCasualtyDrmAndItsNotes()
    {
        var (drm, notes) = ScenarioA1MoppingUp.CasualtyDrm(
        [
            new MopUpDefenderFacts(false, 2, null, false) { Stealth = StealthStatus.Normal },
            new MopUpDefenderFacts(true, 0, -1, true) { Stealth = StealthStatus.Normal },
            new MopUpDefenderFacts(false, 0, null, false) { Stealth = StealthStatus.Stealthy },
            new MopUpDefenderFacts(false, 0, null, false) { Stealth = StealthStatus.Lax },
        ]);
        Assert.Equal(-1, drm);
        Assert.Equal(["-1 Stealthy", "-1 for 2 HS-equivalents", "+1 Lax"], notes);
        var (ledDrm, ledNotes) = ScenarioA1MoppingUp.CasualtyDrm([new MopUpDefenderFacts(false, 2, null, false) { Stealth = StealthStatus.Normal },
            new MopUpDefenderFacts(true, 0, -1, false) { Stealth = StealthStatus.Normal }]);
        Assert.Equal(-2, ledDrm);
        Assert.Equal(["-1 for 2 HS-equivalents", "-1 leadership"], ledNotes);
        Assert.Equal("is not armed", ScenarioA1MoppingUp.UnitWhy(false, false, false, true, false, false, false, false));
        Assert.Null(ScenarioA1MoppingUp.UnitWhy(false, false, false, false, false, false, false, false));
        Assert.NotNull(ScenarioA1MoppingUp.RangeBar([3, 4], "D5", "german"));
        Assert.Null(ScenarioA1MoppingUp.RangeBar([3, 2], "D5", "german"));
        Assert.StartsWith("play.mop-up-range: the distance to D5 is not decided", ScenarioA1MoppingUp.RangeBar([null, 2], "D5", "german"), StringComparison.Ordinal);
        Assert.True(ScenarioA1MoppingUp.CasualtyReduces(2, -1));
        Assert.False(ScenarioA1MoppingUp.CasualtyReduces(2, 0));
        Assert.Equal(["g1", "g3"], ScenarioA1MoppingUp.Struck(["g1", "g2", "g3"], [6, 2, 6]));
    }

    [Fact]
    public void TheNextRoundFollowsTheLocationsCcSoFar()
    {
        Assert.Equal((CloseCombatFacts.Simultaneous, 0), ScenarioA1CloseCombatFactBuilders.NextRound(null, [], null, false));
        Assert.Equal((CloseCombatFacts.PrisonersRound, 0), ScenarioA1CloseCombatFactBuilders.NextRound(CloseCombatFacts.PrisonersRound, [], null, false));
        Assert.Equal((CloseCombatFacts.AmbusherRound, 0), ScenarioA1CloseCombatFactBuilders.NextRound(null, [], "german", true));
        Assert.Equal((CloseCombatFacts.AmbushedRound, 1), ScenarioA1CloseCombatFactBuilders.NextRound(null, [CloseCombatFacts.AmbusherRound], "german", true));
        Assert.Equal((CloseCombatFacts.AmbusherRound, 1), ScenarioA1CloseCombatFactBuilders.NextRound(CloseCombatFacts.AmbusherRound, [CloseCombatFacts.AmbusherRound], "german", true));
        Assert.Equal((CloseCombatFacts.AmbushedRound, 0), ScenarioA1CloseCombatFactBuilders.NextRound(null, [CloseCombatFacts.PrisonersRound], "german", false));
        Assert.True(ScenarioA1CloseCombatFactBuilders.HandToHand(0, false, CloseCombatFacts.Simultaneous, true));
        Assert.False(ScenarioA1CloseCombatFactBuilders.HandToHand(0, false, CloseCombatFacts.PrisonersRound, true));
        Assert.True(ScenarioA1CloseCombatFactBuilders.HandToHand(1, true, CloseCombatFacts.AmbushedRound, false));
    }

    [Fact]
    public void TheProjectorsRoundsAndMelee()
    {
        Assert.Equal([CloseCombatFacts.Simultaneous, CloseCombatFacts.PrisonersRound], ScenarioA1CloseCombatProjection.AllowedRounds(null, false, 0, 0));
        Assert.Equal([CloseCombatFacts.AmbusherRound], ScenarioA1CloseCombatProjection.AllowedRounds("german", true, 0, 1));
        Assert.Equal([CloseCombatFacts.AmbusherRound, CloseCombatFacts.AmbushedRound], ScenarioA1CloseCombatProjection.AllowedRounds("german", true, 1, 1));
        Assert.Equal([CloseCombatFacts.AmbushedRound], ScenarioA1CloseCombatProjection.AllowedRounds("german", false, 1, 1));
        Assert.NotNull(ScenarioA1CloseCombatProjection.RoundRecordRefusal([CloseCombatFacts.Simultaneous], CloseCombatFacts.AmbusherRound, false, false, "bd01:A1:0"));
        Assert.True(ScenarioA1CloseCombatProjection.Closes(CloseCombatFacts.Simultaneous));
        Assert.False(ScenarioA1CloseCombatProjection.Closes(CloseCombatFacts.AmbusherRound));
        Assert.Null(ScenarioA1CloseCombatProjection.MeleeChange(true, true));
        Assert.False(ScenarioA1CloseCombatProjection.MeleeChange(false, true));
        MeleeUnitFacts[] group =
        [
            new("g1", "german", false, true, false, false),
            new("r1", "russian", false, true, false, false),
            new("t1", "russian", true, true, false, false),
        ];
        Assert.Equal(["g1", "r1"], ScenarioA1CloseCombatProjection.InMelee(group));
        Assert.Equal(["g1"], ScenarioA1CloseCombatProjection.InMelee([group[0], group[2]]));
        Assert.Empty(ScenarioA1CloseCombatProjection.InMelee([group[0] with { Concealed = true }, group[2]]));
        Assert.True(ScenarioA1CloseCombatProjection.GuardLeft(false, false));
        Assert.True(ScenarioA1CloseCombatProjection.GuardLeft(true, true));
        Assert.False(ScenarioA1CloseCombatProjection.GuardLeft(true, false));
    }

    // A11.12 (found in dl-tractor-works-01, 2026-10-21): while another Location's CC is open, the plan refuses this one, as the record does.
    [Fact]
    public void AnotherLocationsOpenCcIsFinishedFirst()
    {
        Assert.Null(ScenarioA1CloseCombatRules.OpenElsewhereBar(null));
        Assert.Equal("play.cc-order: CC is resolved one Location at a time: finish the CC in bd01:X4:0 first (A11.12)",
            ScenarioA1CloseCombatRules.OpenElsewhereBar("bd01:X4:0"));
    }

    [Fact]
    public void ASimultaneousRoundWaitsForEverySideThatMayDeclare()
    {
        // A11.11 (the user, 2026-10-22): both sides declare, the ATTACKER first; a side may declare no attack; a caller that names no declared sides
        // declares them all in one place.
        string[] both = ["german", "russian"];
        Assert.Equal("play.cc-declare: the russian side has not declared its CC attacks in bd01:X3:0; CC is simultaneous, so both sides declare before the round is resolved, the ATTACKER first (A11.11)",
            ScenarioA1CloseCombatRules.UndeclaredSideBar(both, ["german"], "bd01:X3:0"));
        Assert.StartsWith("play.cc-declare: the german side", ScenarioA1CloseCombatRules.UndeclaredSideBar(both, [], "bd01:X3:0"), StringComparison.Ordinal);
        Assert.Null(ScenarioA1CloseCombatRules.UndeclaredSideBar(both, ["russian", "german"], "bd01:X3:0"));
        Assert.Null(ScenarioA1CloseCombatRules.UndeclaredSideBar(both, null, "bd01:X3:0"));
        Assert.Null(ScenarioA1CloseCombatRules.UndeclaredSideBar(["german"], ["german"], "bd01:X3:0"));
    }
}
