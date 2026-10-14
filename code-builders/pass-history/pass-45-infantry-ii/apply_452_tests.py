"""Pass 45, tasks 45.2 and 45.3: the Rules tests of Cowering's marks and of the Residual FP an attack leaves."""
import sys

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass45RulesTests.cs"
EDITS = [
    ("""                "firerSelection" => rolls with { FirerSelection = Add(rolls.FirerSelection, 1) },
""",
     """                "firerSelection" => rolls with { FirerSelection = Add(rolls.FirerSelection, 1) },
                "cowerSelection" => rolls with { CowerSelection = Add(rolls.CowerSelection, 1) },
"""),
    ("""    /// <summary>A building for the A7.72 search""",
     """    private static FireFirer Gunner(string id, string weapon = "defender-mmg") => Firer(id) with
    {
        Weapons = [Mg(id + "-mg", weapon)],
        UsesSupportWeapon = true,
    };

    [Fact]
    public void AUnitThatCowersIsMarkedFinalFireWithItsMg()
    {
        // A7.9 (p. 58): "any unit that cowers (as well as all of its SW regardless of whether it was using its inherent FP) is automatically marked
        // with a Final or Prep Fire counter as appropriate." A squad and its MMG (ROF 2) roll doubles of 2 in Defensive First Fire: without the
        // Cowering the MMG would keep its ROF and the squad be marked First Fire.
        var cowers = Complete(Moving(Attack([2, 2], firers: [Gunner("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.True(cowers.Arithmetic!.Cowered);
        Assert.Equal(["ru-1"], cowers.CoweredUnitIds);
        Assert.Equal("first-fire", cowers.FireCounter);
        Assert.Equal("final-fire", cowers.FirerCounters!["ru-1"]);
        var mg = Assert.Single(cowers.WeaponEffects!);
        Assert.False(mg.RateOfFireRetained);
        Assert.Equal("final-fire", mg.FireCounter);

        // The same DR's colored dr with no doubles keeps the ROF.
        var steady = Complete(Moving(Attack([2, 3], firers: [Gunner("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.Null(steady.CoweredUnitIds);
        Assert.True(Assert.Single(steady.WeaponEffects!).RateOfFireRetained);

        // In Prep Fire the counter is the Prep Fire one the attack places anyway; the MG still loses its ROF.
        var prep = Complete(Attack([2, 2], firers: [Gunner("ru-1")]));
        Assert.Equal(["ru-1"], prep.CoweredUnitIds);
        Assert.Null(prep.FirerCounters);
        Assert.False(Assert.Single(prep.WeaponEffects!).RateOfFireRetained);

        // A squad that fired only its MG is marked too, though the MG's fire alone would leave it unmarked (A7.351).
        var alone = Complete(Moving(Attack([2, 2], firers: [Gunner("ru-1") with { UsesInherentFp = false }]), ScenarioA1FireCalculator.FirstFire));
        Assert.Contains("ru-1", alone.FireCounterUnitIds);
    }

    [Fact]
    public void RandomSelectionMarksTheCoweringUnitOfAFireGroup()
    {
        // A7.9: "If a FG cowers, Random Selection is used to determine the unit(s) (and its SW) that becomes marked with a Prep or Final Fire counter."
        var group = Moving(Attack([4, 4]), ScenarioA1FireCalculator.FirstFire);
        Assert.Contains("asl.a1.fire.roll-missing:cowerSelection:ru-1,ru-2", ScenarioA1FireCalculator.Resolve(group, Reference).Reasons);
        FireResolution With(int one, int two) => Complete(group with
        {
            Rolls = group.Rolls! with { CowerSelection = new Dictionary<string, int> { ["ru-1"] = one, ["ru-2"] = two } },
        });
        var second = With(2, 5);
        Assert.Equal(["ru-2"], second.CoweredUnitIds);
        Assert.Equal("final-fire", Assert.Single(second.FirerCounters!).Value);
        Assert.Equal(["ru-1", "ru-2"], With(3, 3).CoweredUnitIds);
    }

    [Fact]
    public void ABerserkFirerNeverCowersAndOnlyInexperiencedPersonnelShiftTwo()
    {
        // A7.9: "Cowering affects all fire except that from a SMC, berserk or Fanatic unit".
        Assert.False(Complete(Attack([3, 3], firers: [Firer("ru-1") with { Berserk = true }])).Arithmetic!.Cowered);

        // "doubled ... for an attack by Inexperienced (19.33) Personnel": a Conscript always; a Green unit stacked with a leader is not (A19.3).
        Assert.Equal(2, Complete(Attack([3, 3], firers: [Firer("ru-1", "defender-conscript-squad")])).Arithmetic!.ColumnShift);
        Assert.Equal(1, Complete(Attack([3, 3], firers: [Firer("us-1", "american-green-squad") with { Inexperienced = false }])).Arithmetic!.ColumnShift);
        Assert.Equal(2, Complete(Attack([3, 3], firers: [Firer("us-1", "american-green-squad") with { Inexperienced = true }])).Arithmetic!.ColumnShift);
    }

    [Fact]
    public void ResidualFpIsHeldToTwelveAfterItsReduction()
    {
        // A8.2 (p. 60): "half (up to a maximum of 12 after adjustment as per 8.26; FRD) of the highest IFT FP column used". Five 4-4-7 at PBF are
        // 40 FP on the 36 column: half is 18, the 16 column; one +1 of Hindrance lowers it to 12. It used to be 8.
        FireFirer[] five = [.. Enumerable.Range(1, 5).Select(index => Firer("ru-" + index))];
        var clear = Complete(Moving(Attack([6, 5], firers: five), ScenarioA1FireCalculator.FirstFire));
        Assert.Equal((36, 12), (clear.Arithmetic!.ColumnFp, clear.Arithmetic.ResidualFp));
        var hindered = Complete(Moving(Attack([6, 5], firers: five), ScenarioA1FireCalculator.FirstFire) with
        {
            Los = new FireLos(false, 1, true, false),
        });
        Assert.Equal(12, hindered.Arithmetic!.ResidualFp);

        // Lower columns are as they were: 16 FP leaves 8, and 6 with one +1.
        var two = Complete(Moving(Attack([6, 5]), ScenarioA1FireCalculator.FirstFire) with
        {
            Los = new FireLos(false, 1, true, false),
        });
        Assert.Equal(6, two.Arithmetic!.ResidualFp);
    }

    [Fact]
    public void ACxFirerLowersTheResidualFp()
    {
        // A8.26 (p. 60): "each positive DRM caused solely by conditions outside (including CX, BU, Stun, leadership modifier ...) the target hex".
        // The page's example: "If the 4-6-7 were CX the First Fire Attack would have a +1 DRM, reducing the squad's Residual FP".
        var fresh = Complete(Moving(Attack([6, 5], firers: [Firer("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.Equal(4, fresh.Arithmetic!.ResidualFp);
        var tired = Complete(Moving(Attack([6, 5], firers: [Firer("ru-1") with { Cx = true }]), ScenarioA1FireCalculator.FirstFire));
        Assert.Contains(tired.Arithmetic!.Drm, item => item.Name == "cx:ru-1");
        Assert.Equal(2, tired.Arithmetic.ResidualFp);
    }

    [Fact]
    public void AMgThatKeepsItsRofLeavesNoResidualFpOfItsOwn()
    {
        // A8.23 (p. 60), the page's example: "A 4-6-7 squad and a MMG make a 9 FP Defensive First Fire attack ... To retain his Multiple ROF for
        // the MMG, the German player opts to leave no Residual FP with the MG, but the squad still leaves two Residual FP". Here a 4-4-7 and a 4 FP
        // MMG at PBF: 16 FP. The colored dr of 1 keeps the ROF: the squad's 8 FP leave 4, and giving the ROF up would leave 8.
        var kept = Complete(Moving(Attack([1, 5], firers: [Gunner("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.True(Assert.Single(kept.WeaponEffects!).RateOfFireRetained);
        Assert.Equal((4, 8), (kept.Arithmetic!.ResidualFp, kept.Arithmetic.ResidualFpIfForfeited));

        // With the ROF lost the whole column counts, and there is nothing to give up.
        var lost = Complete(Moving(Attack([6, 2], firers: [Gunner("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.Equal((8, null), (lost.Arithmetic!.ResidualFp, lost.Arithmetic.ResidualFpIfForfeited));

        // A8.221: "Residual FP is not gained for an attack by a malfunctioning weapon". The MMG's B11 is rolled: the squad's FP alone.
        var broken = Complete(Moving(Attack([6, 5], firers: [Gunner("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.True(Assert.Single(broken.WeaponEffects!).Malfunctioned);
        Assert.Equal((4, null), (broken.Arithmetic!.ResidualFp, broken.Arithmetic.ResidualFpIfForfeited));
    }

    [Fact]
    public void TheRofIsGivenUpOnlyAtOnceAfterTheAttack()
    {
        // A8.23: "The decision ... must be made immediately after resolving its attack."
        Assert.True(ScenarioA1FireFollowUps.RofForfeitOpen(true, true, 8, 1));
        Assert.False(ScenarioA1FireFollowUps.RofForfeitOpen(false, true, 8, 1));
        Assert.False(ScenarioA1FireFollowUps.RofForfeitOpen(true, false, 8, 1));
        Assert.False(ScenarioA1FireFollowUps.RofForfeitOpen(true, true, null, 1));
        Assert.False(ScenarioA1FireFollowUps.RofForfeitOpen(true, true, 8, 0));

        // The record's Residual FP is its own value, or the value with the ROF given up.
        Assert.Null(ScenarioA1FireFollowUps.VerifyResidualFp("mph", true, true, 4, 8, "fire", 4, 8));
        Assert.NotNull(ScenarioA1FireFollowUps.VerifyResidualFp("mph", true, true, 4, 8, "fire", 4));
        Assert.NotNull(ScenarioA1FireFollowUps.VerifyResidualFp("mph", true, true, 4, 6, "fire", 4, 8));

        // A cowering unit's other SW: its counter, a Final Fire counter replacing a First Fire one.
        Assert.Equal([(UnitCondition.FinalFire, true), (UnitCondition.FirstFire, false)], ScenarioA1FireFollowUps.CoweredWeaponConditions("final-fire", true));
        Assert.Equal([(UnitCondition.PrepFire, true)], ScenarioA1FireFollowUps.CoweredWeaponConditions("prep-fire", false));
    }

    /// <summary>A building for the A7.72 search"""),
]
with open(PATH, encoding="utf-8", newline="") as f:
    text = f.read()
for old, new in EDITS:
    if text.count(old) != 1:
        sys.exit(f"anchor found {text.count(old)} times: {old[:80]!r}")
for old, new in EDITS:
    text = text.replace(old, new)
with open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("ok")
