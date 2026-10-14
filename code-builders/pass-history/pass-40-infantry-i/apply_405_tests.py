"""Pass 40 task 40.5: the Rules tests of the attack owed under A8.312 and of a crew's FP in FPF (A8.311)."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/"

RULES_TESTS = '''
    private static MandatoryDefenderFacts Owing(string id, bool infantry = true, bool smc = false, bool inherent = true, int weapons = 0, bool unarmed = false, bool broken = false,
        bool melee = false, bool captured = false, bool marked = false, bool attacked = false, bool mayAttack = true) =>
        new(id, infantry, smc, inherent, weapons, unarmed, broken, melee, captured, marked, attacked, mayAttack);

    [Fact]
    public void AnArmedUnbrokenInfantryDefenderNotInMeleeOwesItsAttackOnAnMmcThatEntersItsLocation()
    {
        // A8.312 (p. 61).
        MandatoryDefenderFacts[] there = [Owing("s1"), Owing("s2", marked: true), Owing("b", broken: true), Owing("m", melee: true), Owing("u", unarmed: true),
            Owing("p", captured: true), Owing("l", smc: true, inherent: false), Owing("lm", smc: true, inherent: false, weapons: 1),
            Owing("lmf", smc: true, inherent: false, weapons: 1, marked: true), Owing("done", attacked: true), Owing("cannot", mayAttack: false), Owing("v", infantry: false)];
        Assert.Equal(["s1", "s2", "lm"], ScenarioA1MandatoryFire.Owed(true, true, true, true, there));

        // Only in the MPh, with the DEFENDER's window open, on the step that entered, and for an Infantry or Cavalry MMC.
        Assert.Empty(ScenarioA1MandatoryFire.Owed(false, true, true, true, there));
        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, false, true, true, there));
        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, true, false, true, there));
        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, true, true, false, there));
    }

    [Fact]
    public void WhileTheAttackIsOwedThePassIsRefusedAndTheDefendersFireIsTheMandatoryFgs()
    {
        // A8.312, A7.55 (pp. 61, 57).
        string[] owed = ["s1", "s2"];
        Assert.Null(ScenarioA1MandatoryFire.PassBar([], "x"));
        Assert.StartsWith("play.pass-mandatory:", ScenarioA1MandatoryFire.PassBar(owed, "x"), StringComparison.Ordinal);

        Assert.Null(ScenarioA1MandatoryFire.FireBar([], true, ["other"], "y", "x"));
        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, true, ["s1", "s2"], "x", "x"));
        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, true, ["s1", "s2", "leader"], "x", "x"));
        Assert.Contains("s2 must join", ScenarioA1MandatoryFire.FireBar(owed, true, ["s1"], "x", "x"), StringComparison.Ordinal);
        Assert.StartsWith("play.fire-mandatory:", ScenarioA1MandatoryFire.FireBar(owed, true, ["s1", "s2"], "y", "x"), StringComparison.Ordinal);

        // Where the fire rules form no group of them (a unit with no counter beside one marked First Fire), one of them attacks, and no one else with it.
        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, false, ["s1"], "x", "x"));
        Assert.StartsWith("play.fire-mandatory:", ScenarioA1MandatoryFire.FireBar(owed, false, ["other"], "x", "x"), StringComparison.Ordinal);
        Assert.StartsWith("play.fire-mandatory:", ScenarioA1MandatoryFire.FireBar(owed, false, ["s1", "other"], "x", "x"), StringComparison.Ordinal);

        Assert.Contains("as one Mandatory FG", ScenarioA1MandatoryFire.Note(owed, true, "[C9]"), StringComparison.Ordinal);
        Assert.Contains("one of s1, s2", ScenarioA1MandatoryFire.Note(owed, false, "[C9]"), StringComparison.Ordinal);
    }

    [Fact]
    public void ACrewThatFiredItsGunAddsItsInherentFpToFpfOnly()
    {
        // A8.311 (p. 61) beside A7.352 (p. 56; ruling R8.4).
        Assert.True(ScenarioA1MandatoryFire.CrewAddsInherentFp(false, false));
        Assert.False(ScenarioA1MandatoryFire.CrewAddsInherentFp(true, false));
        Assert.True(ScenarioA1MandatoryFire.CrewAddsInherentFp(true, true));
    }
}
'''
path = ROOT + "LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass40RulesTests.cs"
text = open(path, encoding="utf-8", newline="").read()
assert text.rstrip().endswith("}")
body = text.rstrip()[:-1].rstrip() + "\n" + RULES_TESTS
old_doc = "/// Pass 40, read directly in Rules. Task 40.4:"
assert body.count(old_doc) == 1
body = body.replace(old_doc, "/// Pass 40, read directly in Rules. Task 40.5: the attack owed in the DEFENDER's own Location, and a crew's FP in FPF (A8.311, A8.312; p. 61). Task 40.4:")
open(path, "w", encoding="utf-8", newline="").write(body)
print("ok")
