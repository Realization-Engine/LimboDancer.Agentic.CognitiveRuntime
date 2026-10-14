"""Pass 45: the tests of the reviews' fixes, and my earlier tests moved to the members' new shapes."""
import sys

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass45RulesTests.cs"
EDITS = [
    ("""        Assert.True(ScenarioA1FireEligibility.MgBoreSighted([(true, [true])]));
        Assert.True(ScenarioA1FireEligibility.MgBoreSighted([(true, [true]), (true, [true])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(false, [true])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [true]), (true, [false])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([]));

        // C6.41, C6.42: a MMG or HMG of the Scenario Defender, outside its hex, in its LOS and Normal Range.
        Assert.Null(ScenarioA1GameStart.MgBoreSightBar("medium", true, false, () => true, "mg"));
        Assert.Null(ScenarioA1GameStart.MgBoreSightBar("heavy", true, false, () => true, "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("light", true, false, () => true, "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", false, false, () => true, "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, true, () => true, "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, false, () => false, "mg"));
""",
     """        var good = new MgSightingFacts(true, true, true, true, false);
        Assert.True(ScenarioA1FireEligibility.MgBoreSighted([(true, [good])]));
        Assert.True(ScenarioA1FireEligibility.MgBoreSighted([(true, [good]), (true, [good])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(false, [good])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [good]), (true, [good with { Sighted = false }])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([]));

        // C6.43: not by another unit, not from another Location, not at another Location, and not while "is dm".
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [good with { ByItsPossessor = false }])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [good with { AtItsSetupLocation = false }])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [good with { OnTheTarget = false }])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [good with { Dismantled = true }])]));

        // C6.41, C6.42: a MMG or HMG of the Scenario Defender, outside its hex, in its LOS, within its Normal Range and 16 hexes.
        Assert.Null(ScenarioA1GameStart.MgBoreSightBar("medium", true, false, 10, () => (true, 10), "mg"));
        Assert.Null(ScenarioA1GameStart.MgBoreSightBar("heavy", true, false, 20, () => (true, 16), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("heavy", true, false, 20, () => (true, 17), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, false, 10, () => (true, 11), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("light", true, false, 6, () => (true, 2), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", false, false, 10, () => (true, 2), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, true, 10, () => (true, 2), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, false, 10, () => (false, 2), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, false, 10, () => null, "mg"));
"""),
    ("""        var bar = ScenarioA1MandatoryFire.OtherActBar(["r4", "r5"], "bd01:A1:0", "Gun's shot");
        Assert.StartsWith("play.act-mandatory: r4, r5 must first attack", bar);
        Assert.Contains("Gun's shot", bar);
""",
     """        string[] owing = ["r4", "r5"];
        var bar = ScenarioA1MandatoryFire.OtherActBar(true, true, () => (owing, "bd01:A1:0"), "Gun's shot");
        Assert.StartsWith("play.act-mandatory: r4, r5 must first attack", bar);
        Assert.Contains("Gun's shot", bar);

        // Never for the ATTACKER's proposal, outside the MPh, or with nothing owed; the attack owed is not even read for the first two.
        Assert.Null(ScenarioA1MandatoryFire.OtherActBar(true, false, () => throw new InvalidOperationException(), "Gun's shot"));
        Assert.Null(ScenarioA1MandatoryFire.OtherActBar(false, true, () => throw new InvalidOperationException(), "Gun's shot"));
        Assert.Null(ScenarioA1MandatoryFire.OtherActBar(true, true, () => null, "Gun's shot"));
"""),
    ("""    /// <summary>A building for the A7.72 search: steps between numbered Locations, which of them are at ground level, and which the enemy holds.</summary>
    private sealed class Building(Dictionary<int, int[]> steps, int[] ground, int[] held) : IUpperLevelPathReader
    {
        public IReadOnlyList<int> StepsFrom(int location) => steps.GetValueOrDefault(location, []);

        public bool GroundLevel(int location) => ground.Contains(location);

        public bool Held(int location) => held.Contains(location);
    }
""",
     """    /// <summary>A building for the A7.72 search: steps between numbered Locations, which of them are at ground level, and which the enemy holds.</summary>
    private sealed class Building(Dictionary<int, int[]> steps, int[] ground, int[] held, PathUnitFacts? holder = null) : IUpperLevelPathReader
    {
        public IReadOnlyList<int> StepsFrom(int location) => steps.GetValueOrDefault(location, []);

        public bool GroundLevel(int location) => ground.Contains(location);

        public IReadOnlyList<PathUnitFacts> UnitsAt(int location) => held.Contains(location) ? [holder ?? Enemy] : [];
    }

    private static readonly PathUnitFacts Enemy = new(true, true, false, false, false, false, false, false);

    private static readonly UpperLevelUnitFacts Upstairs = new(true, false, false, false, 2, true, false, false);
"""),
    ("""        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(true, false, false, 0, new Building(steps, [2, 4], [])));

        // The enemy at level 1 cuts the unit at level 2 off.
        Assert.True(ScenarioA1UpperLevelEncirclement.Encircled(true, false, false, 0, new Building(steps, [2, 4], [1])));

        // From level 1, the enemy at ground level of its own hex leaves the way through the next hex; held too, there is none.
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(true, false, false, 1, new Building(steps, [2, 4], [2])));
        Assert.True(ScenarioA1UpperLevelEncirclement.Encircled(true, false, false, 1, new Building(steps, [2, 4], [2, 4])));

        // Never a berserk or heroic unit, and never a unit that is not upstairs in a building.
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(true, true, false, 0, new Building(steps, [2, 4], [1])));
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(true, false, true, 0, new Building(steps, [2, 4], [1])));
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(false, false, false, 0, new Building(steps, [2, 4], [1])));
""",
     """        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs, 0, new Building(steps, [2, 4], [])));

        // The enemy at level 1 cuts the unit at level 2 off.
        Assert.True(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs, 0, new Building(steps, [2, 4], [1])));

        // From level 1, the enemy at ground level of its own hex leaves the way through the next hex; held too, there is none.
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Level = 1 }, 1, new Building(steps, [2, 4], [2])));
        Assert.True(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Level = 1 }, 1, new Building(steps, [2, 4], [2, 4])));

        // Never a berserk or heroic unit, a vehicle, a unit aboard one or in Bypass, or a unit that is not upstairs in a building.
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Berserk = true }, 0, new Building(steps, [2, 4], [1])));
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Heroic = true }, 0, new Building(steps, [2, 4], [1])));
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Building = false }, 0, new Building(steps, [2, 4], [1])));
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Level = 0 }, 0, new Building(steps, [2, 4], [1])));
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Aboard = true }, 0, new Building(steps, [2, 4], [1])));

        // Only "an unbroken, armed, unconcealed enemy unit" holds a Location.
        Assert.True(ScenarioA1UpperLevelEncirclement.Holds(Enemy));
        foreach (var free in new[] { Enemy with { Broken = true }, Enemy with { Unarmed = true }, Enemy with { Concealed = true }, Enemy with { Hidden = true },
            Enemy with { Dummy = true }, Enemy with { Captured = true }, Enemy with { Enemy = false }, Enemy with { Active = false } })
        {
            Assert.False(ScenarioA1UpperLevelEncirclement.Holds(free));
            Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs, 0, new Building(steps, [2, 4], [1], free)));
        }
"""),
    ("""    [Fact]
    public void ResidualFpAttacksAUnitOnceALocationUnlessItGrewOrTheDrmFell()
""",
     """    [Fact]
    public void TheReviewsFixesOfTheMixedGroup()
    {
        // The example of p. 62: a squad marked First Fire fires its own FP in one group with a crew that fires its LMG alone. The attack is
        // Subsequent First Fire, though a firer fires a MG alone (the first review's blocker).
        IReadOnlyList<(bool FinalFire, bool FirstFire)> marks = [(false, true), (false, false)];
        Assert.Equal(ScenarioA1FireCalculator.SubsequentFirstFire,
            ScenarioA1FireEligibility.MphFireKind("mph", new FireKindFacts(false, false, () => false, () => marks, false, true, EveryFirstFireMarkedFiresAlone: false)).Kind);

        // A MG fired again alone on its ROF by the marked unit itself is First Fire still (A9.2).
        IReadOnlyList<(bool FinalFire, bool FirstFire)> alone = [(false, true)];
        Assert.Equal(ScenarioA1FireCalculator.FirstFire,
            ScenarioA1FireEligibility.MphFireKind("mph", new FireKindFacts(false, false, () => false, () => alone, false, true, EveryFirstFireMarkedFiresAlone: true)).Kind);

        // A8.3 binds "a unit [that] uses Subsequent First Fire" to every MG it possesses; a unit with no counter beside it is free.
        var marked = Firer("ru-1") with { FirstFireMarked = true, FiredThisPlayerTurn = true };
        var mixed = Moving(Attack([6, 5], firers: [marked, Firer("ru-2")]), ScenarioA1FireCalculator.SubsequentFirstFire);
        Assert.Null(ScenarioA1FireEligibility.EveryMgBar(mixed, id => id == "ru-2" ? ["ru-2-mg"] : []));
        Assert.StartsWith("play.fire-weapons", ScenarioA1FireEligibility.EveryMgBar(mixed, id => id == "ru-1" ? ["ru-1-mg"] : []));

        // A7.53: a leader who has directed does not direct a group that holds a unit with no counter (the example: "leader direction is NA").
        var leader = new FireDirector("ru-l", "defender-leader", From, false, false, false, true, false);
        Assert.Contains("asl.a1.fire.director-outside", ScenarioA1FireCalculator.Resolve(mixed with { Director = leader }, Reference).Reasons);
        var allMarked = Moving(Attack([6, 5], firers: [marked, Firer("ru-2") with { FirstFireMarked = true, FiredThisPlayerTurn = true }], director: leader),
            ScenarioA1FireCalculator.SubsequentFirstFire);
        Assert.Equal(FireResolution.Resolved, Complete(allMarked).Disposition);

        // A7.353: a crew with no counter whose Gun fired has no FP in the group's First Fire; marked Final Fire it uses FPF.
        var crew = Firer("ru-c", "defender-crew") with { GunFired = true };
        var squad = Firer("ru-1") with { FinalFireMarked = true, FiredThisPlayerTurn = true };
        Assert.Contains("asl.a1.fire.firer-outside",
            ScenarioA1FireCalculator.Resolve(Moving(Attack([3, 4], firers: [squad, crew]), ScenarioA1FireCalculator.FinalProtectiveFire), Reference).Reasons);
        Assert.Equal(FireResolution.Resolved, Complete(Moving(Attack([3, 4], firers: [squad, crew with { FinalFireMarked = true, FiredThisPlayerTurn = true }]),
            ScenarioA1FireCalculator.FinalProtectiveFire)).Disposition);
    }

    [Fact]
    public void TheReviewsFixesOfTheWeapons()
    {
        // A.11 lists the causes that make an X#; Extreme Winter lowers the B# and the X# alike (E3.741) and makes none.
        var winter = Assert.Single(Complete(Moving(Attack([6, 6], firers: [Gunner("ru-1", "defender-lmg"), Firer("ru-2") with { Fanatic = true }]),
            ScenarioA1FireCalculator.FirstFire) with { BreakdownReduction = 1 }).WeaponEffects!);
        Assert.Equal((true, null), (winter.Malfunctioned, winter.Removed));

        // A9.3: a lone hero, as a lone leader, uses no Sustained Fire.
        var hero = Firer("ru-h", "defender-hero") with
        {
            UsesInherentFp = false,
            Weapons = [Mg("ru-mg") with { FirstFireMarked = true }],
            UsesSupportWeapon = true,
            FirstFireMarked = true,
        };
        Assert.Contains("asl.a1.fire.weapon-outside",
            ScenarioA1FireCalculator.Resolve(Moving(Attack([6, 4], firers: [hero]), ScenarioA1FireCalculator.SubsequentFirstFire), Reference).Reasons);

        // A9.52, A7.352: a HS that fires a MG adds no FP of its own, so it sprays with the MG as it did before the pass.
        var halfSquad = Firer("ru-hs", "defender-half-squad") with
        {
            Weapons = [Mg("ru-mg")],
            UsesSupportWeapon = true,
        };
        Assert.Equal(FireResolution.Resolved, Complete(Attack([6, 5], firers: [halfSquad]) with { SprayingFire = true, Range = 2 }).Disposition);

        // C6.44: a "?" stack of Dummies is Infantry to the firer, so Prep Fire at it takes no Bore Sighting DRM either.
        var gunner = Gunner("ru-1") with { UsesInherentFp = false };
        var dummies = Complete(Attack([6, 5], firers: [gunner], targets: [new FireTarget("de-q", null, At, false, false, true, false, false, false, true)]) with { MgBoreSighted = true });
        Assert.DoesNotContain(dummies.Arithmetic!.Drm, item => item.Name == "bore-sighting");

        // The ROF is given up only for a larger counter than the one that stands there (A8.21), and a Sniper Check written with the attack leaves
        // the choice open.
        Assert.False(ScenarioA1FireFollowUps.RofForfeitOpen(true, true, 4, 1, standingFp: 4));
        Assert.True(ScenarioA1FireFollowUps.RofForfeitOpen(true, true, 4, 1, standingFp: 2));
        Assert.True(ScenarioA1FireFollowUps.PartOfTheAttack(false, true));
        Assert.False(ScenarioA1FireFollowUps.PartOfTheAttack(false, false));
        Assert.True(ScenarioA1FireFollowUps.StillKeepsRof(true, false, false, false));
        Assert.False(ScenarioA1FireFollowUps.StillKeepsRof(true, true, false, false));

        // A8.22 is per unit: a stack one of whose units was not yet attacked there is attacked.
        IReadOnlyList<(int Fp, int Drm)>[] byUnit = [[(4, 0)], []];
        Assert.True(ScenarioA1FireFollowUps.ResidualAttacksAgain(4, 0, byUnit));
        Assert.False(ScenarioA1FireFollowUps.ResidualAttacksAgain(4, 0, new IReadOnlyList<(int Fp, int Drm)>[] { [(4, 0)], [(4, 0)] }));

        // A FT, never raised for PBF, fires at the next level of its hex at its own FP; A9.8: a dismantled mortar is not fired.
        Assert.Equal(FireRangeBand.Normal, FireRange.Band(0, sameLocation: false, 1, levelAbove: 1, noPointBlank: true).Band);
        Assert.StartsWith("play.ordnance-dismantled", ScenarioA1FireEligibility.DismantledShotBar(true, "mortar"));
        Assert.Null(ScenarioA1FireEligibility.DismantledShotBar(false, "mortar"));

        // A4.52: a fresh leader of the stack lends his IPC though another leader is CX.
        MovingUnitFacts[] stack =
        [
            new("s1", "russian", false, true, false, false, false, false, "russian", 0, false, null),
            new("l1", "russian", false, false, true, false, false, false, "russian", 0, false, null, Cx: true),
            new("l2", "russian", false, false, true, false, false, false, "russian", 0, false, null),
        ];
        Assert.Equal(("s1", "l2"), ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, index => index == 0));
    }

    [Fact]
    public void ResidualFpAttacksAUnitOnceALocationUnlessItGrewOrTheDrmFell()
"""),
]
with open(PATH, encoding="utf-8", newline="") as f:
    text = f.read()
for old, new in EDITS:
    if text.count(old) != 1:
        sys.exit(f"anchor found {text.count(old)} times: {old[:90]!r}")
for old, new in EDITS:
    text = text.replace(old, new)
with open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("ok")
