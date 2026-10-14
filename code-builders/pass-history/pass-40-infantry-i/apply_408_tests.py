"""Pass 40 task 40.8: the Play test and the Rules tests of Hazardous Movement in every fire phase. Exact anchors, asserted once each."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/"

PLAY_TEST = '''    [Fact]
    public async Task ACrewThatPushedItsGunIsUnderHazardousMovementInTheDfphToo()
    {
        // Pass 40, task 40.8 (A4.62, p. 51; ruling R40.10): "regardless of fire phase until they are pinned". The crew pushed in its MPh; the
        // DEFENDER's Final Fire at it takes the -2. A crew that did not push takes none.
        await SetupDefended("german", Crew("de-crew", "bd01:A8:0", "german"), Gun("de-gun", "attacker-inf-gun", "bd01:A8:0", "east", "de-crew", "german"),
            Crew("de-c2", "bd01:A6:0", "german"), Squad("r1", "bd01:D8:0", "russian"), Squad("r2", "bd01:D6:0", "russian"));
        await Advance(2);
        Committed(await Push("de-crew", "bd01:B8:0", "de-gun", 1, 1));
        Assert.Equal(CrewIds, Current.HazardousMovers);
        await Pass();
        await EndMove();
        await Advance();
        Assert.Equal("dfph", Current.Phase);
        var before = Revision;
        Committed(await Fire(R1, "bd01:B8:0", 6, 6, 6, 6, 6, 6));
        Assert.True(HasDrm(LastFire(before), "hazardous-movement", -2));
        before = Revision;
        Committed(await Fire(R2, "bd01:A6:0", 6, 6, 6, 6, 6, 6));
        Assert.False(HasDrm(LastFire(before), "hazardous-movement", -2));

        // The list is the Player Turn's: it is empty once the next one begins.
        await Advance(5);
        Assert.Empty(Current.HazardousMovers);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AHookedUpTruckIsTiAndAnAbandonedGunCanBeHookedUp()'''

path = "LimboDancer.Domains.Asl.Play.Tests/BacklogPass8Tests.cs"
text = open(ROOT + path, encoding="utf-8", newline="").read()
anchor = "    [Fact]\n    public async Task AHookedUpTruckIsTiAndAnAbandonedGunCanBeHookedUp()"
assert text.count(anchor) == 1
open(ROOT + path, "w", encoding="utf-8", newline="").write(text.replace(anchor, PLAY_TEST))

FIRE_TEST = '''    [Fact]
    public void HazardousMovementIsReadInEveryFirePhaseForTheUnitsThatPushed()
    {
        // A4.62 (p. 51; pass 40, task 40.8, ruling R40.10): "regardless of fire phase until they are pinned".
        var prep = Attack([5, 5]);
        Assert.True(ScenarioA1FireMapRules.HazardousMovement(["de-s"], prep));
        Assert.Null(ScenarioA1FireMapRules.HazardousMovement([], prep));
        Assert.Null(ScenarioA1FireMapRules.HazardousMovement(["other"], prep));
        Assert.Null(ScenarioA1FireMapRules.HazardousMovement(["de-s"], prep with
        {
            Targets = [Target("de-s") with { Broken = true }],
        }));
        var fired = Complete(prep with
        {
            HazardousMovement = true,
        });
        Assert.Contains(("hazardous-movement", -2m), Drm(fired));
        Assert.DoesNotContain(fired.Reasons ?? [], reason => reason.Contains("terrain-fact-outside", StringComparison.Ordinal));
        Assert.Equal(["a", "b"], ScenarioA1MovementCalculator.HazardousMovers(["a"], true, ["b"]));
        Assert.Equal(["a"], ScenarioA1MovementCalculator.HazardousMovers(["a"], false, ["b"]));
    }

    [Fact]
    public void MarshHasNoTemButNoFfmoAndRubbleHasItsBuildingsTem()'''
path = "LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass10Tests.cs"
text = open(ROOT + path, encoding="utf-8", newline="").read()
anchor = "    [Fact]\n    public void MarshHasNoTemButNoFfmoAndRubbleHasItsBuildingsTem()"
assert text.count(anchor) == 1
open(ROOT + path, "w", encoding="utf-8", newline="").write(text.replace(anchor, FIRE_TEST))
print("ok")
