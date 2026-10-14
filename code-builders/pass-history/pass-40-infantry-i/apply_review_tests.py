"""Pass 40: the tests after the review fixes. Exact anchors, asserted once each."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:90], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass40RulesTests.cs", [
    # The NTC's DRM: the larger of the two TEM, not their sum (B9.31).
    ("        Assert.Equal(3, ScenarioA1InfantryOverrun.NtcDrm(2, 1));\n",
     "        Assert.Equal(2, ScenarioA1InfantryOverrun.NtcDrm(2, 1));\n"
     "        Assert.Equal(2, ScenarioA1InfantryOverrun.NtcDrm(1, 2));\n"
     "        Assert.Equal(0, ScenarioA1InfantryOverrun.NtcDrm(-1, 0));\n\n"
     "        // Each MMC under a passed NTC whose taker enters with it: a leader left behind covers no one, and MMC that each passed their own go together.\n"
     "        (IReadOnlyList<string> Units, string? Taker, bool Passed)[] byLeader = [(new[] { \"s1\", \"s2\", \"l\" }, \"l\", true)];\n"
     "        Assert.True(ScenarioA1InfantryOverrun.NtcCovers([\"s1\", \"s2\"], [\"s1\", \"s2\", \"l\"], byLeader));\n"
     "        Assert.False(ScenarioA1InfantryOverrun.NtcCovers([\"s1\", \"s2\"], [\"s1\", \"s2\"], byLeader));\n"
     "        (IReadOnlyList<string> Units, string? Taker, bool Passed)[] each = [(new[] { \"s1\" }, \"s1\", true), (new[] { \"s2\" }, \"s2\", true), (new[] { \"s3\" }, \"s3\", false)];\n"
     "        Assert.True(ScenarioA1InfantryOverrun.NtcCovers([\"s1\", \"s2\"], [\"s1\", \"s2\"], each));\n"
     "        Assert.False(ScenarioA1InfantryOverrun.NtcCovers([\"s1\", \"s3\"], [\"s1\", \"s3\"], each));\n"
     "        Assert.False(ScenarioA1InfantryOverrun.NtcCovers([], [\"l\"], byLeader));\n"),
    # A concealed or hidden vehicle beside the SMC does not turn the NTC away.
    ("        Assert.StartsWith(\"play.overrun-target:\", ScenarioA1InfantryOverrun.TargetBar([], \"x\"), StringComparison.Ordinal);\n",
     "        Assert.StartsWith(\"play.overrun-target:\", ScenarioA1InfantryOverrun.TargetBar([], \"x\"), StringComparison.Ordinal);\n"
     "        Assert.Null(ScenarioA1InfantryOverrun.TargetBar([OvrDefender(\"l\"), OvrDefender(\"tank\", known: false, smc: false, vehicle: true)], \"x\"));\n"
     "        Assert.StartsWith(\"play.overrun-target:\", ScenarioA1InfantryOverrun.TargetBar([OvrDefender(\"l\", vehicle: true)], \"x\"), StringComparison.Ordinal);\n"),
    ("        Assert.False(ScenarioA1InfantryOverrun.OnlyUnarmed([]));\n",
     "        Assert.False(ScenarioA1InfantryOverrun.OnlyUnarmed([]));\n\n"
     "        // A20.54: an unarmed unit \"may not enter a Known enemy-occupied Location\", and Dummies enter none.\n"
     "        Assert.True(ScenarioA1InfantryOverrun.MayRecapture(false, false));\n"
     "        Assert.False(ScenarioA1InfantryOverrun.MayRecapture(true, false));\n"
     "        Assert.False(ScenarioA1InfantryOverrun.MayRecapture(false, true));\n"),
    ("        Assert.Equal([\"woods\"], ScenarioA1InfantryOverrun.EvadeDestinations([(\"open\", true), (\"woods\", false)]));\n"
     "        Assert.Equal([\"open\", \"open2\"], ScenarioA1InfantryOverrun.EvadeDestinations([(\"open\", true), (\"open2\", true)]));\n",
     "        Assert.Equal([\"woods\"], ScenarioA1InfantryOverrun.EvadeDestinations([(\"open\", true, true, false, true), (\"woods\", true, true, false, false)]));\n"
     "        Assert.Equal([\"open\", \"open2\"], ScenarioA1InfantryOverrun.EvadeDestinations([(\"open\", true, true, false, true), (\"open2\", true, true, false, true)]));\n\n"
     "        // \"Accessible ... not occupied by an enemy unit\": a Location it could not enter, one off the playable area, and one an enemy unit holds are left out.\n"
     "        Assert.Equal([\"open\"], ScenarioA1InfantryOverrun.EvadeDestinations([(\"open\", true, true, false, true), (\"cliff\", false, true, false, false),\n"
     "            (\"off\", true, false, false, false), (\"held\", true, true, true, false)]));\n"
     "        Assert.DoesNotContain(\"[\", ScenarioA1InfantryOverrun.EvadeText(\"l\", [\"[C8]\"]), StringComparison.Ordinal);\n"),
    # The attack owed.
    ("        bool melee = false, bool captured = false, bool marked = false, bool attacked = false, bool mayAttack = true) =>\n"
     "        new(id, infantry, smc, inherent, weapons, unarmed, broken, melee, captured, marked, attacked, mayAttack);",
     "        bool melee = false, bool captured = false, bool marked = false, bool attacked = false) =>\n"
     "        new(id, infantry, smc, inherent, weapons, unarmed, broken, melee, captured, marked, attacked);"),
    ("Owing(\"done\", attacked: true), Owing(\"cannot\", mayAttack: false), Owing(\"v\", infantry: false)];\n"
     "        Assert.Equal([\"s1\", \"s2\", \"lm\"], ScenarioA1MandatoryFire.Owed(true, true, true, true, there));\n",
     "Owing(\"done\", attacked: true), Owing(\"cannot\"), Owing(\"v\", infantry: false), Owing(\"sm\", weapons: 1, marked: true)];\n"
     "        var asked = new List<string>();\n"
     "        bool MayAttack(string id)\n"
     "        {\n"
     "            asked.Add(id);\n"
     "            return id != \"cannot\";\n"
     "        }\n\n"
     "        Assert.Equal([\"s1\", \"s2\", \"lm\", \"sm\"], ScenarioA1MandatoryFire.Owed(true, true, true, true, there, MayAttack));\n\n"
     "        // The fire plan is asked only of a unit that would otherwise owe the attack.\n"
     "        Assert.Equal([\"s1\", \"s2\", \"lm\", \"cannot\", \"sm\"], asked);\n\n"
     "        // A unit already marked fires every MG its size operates with its own FP (A8.3, A8.31); one with no FP of its own, one MG; else none.\n"
     "        Assert.Equal(2, ScenarioA1MandatoryFire.WeaponsFired(true, true, true, 3));\n"
     "        Assert.Equal(1, ScenarioA1MandatoryFire.WeaponsFired(false, true, true, 2));\n"
     "        Assert.Equal(0, ScenarioA1MandatoryFire.WeaponsFired(true, true, false, 2));\n"
     "        Assert.Equal(1, ScenarioA1MandatoryFire.WeaponsFired(false, false, false, 2));\n"
     "        Assert.Equal(0, ScenarioA1MandatoryFire.WeaponsFired(true, true, true, 0));\n"),
    ("        Assert.Empty(ScenarioA1MandatoryFire.Owed(false, true, true, true, there));\n"
     "        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, false, true, true, there));\n"
     "        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, true, false, true, there));\n"
     "        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, true, true, false, there));\n",
     "        Assert.Empty(ScenarioA1MandatoryFire.Owed(false, true, true, true, there, MayAttack));\n"
     "        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, false, true, true, there, MayAttack));\n"
     "        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, true, false, true, there, MayAttack));\n"
     "        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, true, true, false, there, MayAttack));\n"),
    ("        Assert.Null(ScenarioA1MandatoryFire.PassBar([], \"x\"));\n"
     "        Assert.StartsWith(\"play.pass-mandatory:\", ScenarioA1MandatoryFire.PassBar(owed, \"x\"), StringComparison.Ordinal);\n\n"
     "        Assert.Null(ScenarioA1MandatoryFire.FireBar([], true, [\"other\"], \"y\", \"x\"));\n"
     "        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, true, [\"s1\", \"s2\"], \"x\", \"x\"));\n"
     "        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, true, [\"s1\", \"s2\", \"leader\"], \"x\", \"x\"));\n"
     "        Assert.Contains(\"s2 must join\", ScenarioA1MandatoryFire.FireBar(owed, true, [\"s1\"], \"x\", \"x\"), StringComparison.Ordinal);\n"
     "        Assert.StartsWith(\"play.fire-mandatory:\", ScenarioA1MandatoryFire.FireBar(owed, true, [\"s1\", \"s2\"], \"y\", \"x\"), StringComparison.Ordinal);\n",
     "        Assert.Null(ScenarioA1MandatoryFire.PassBar([], true, \"x\"));\n"
     "        Assert.Contains(\"as one Mandatory FG\", ScenarioA1MandatoryFire.PassBar(owed, true, \"x\"), StringComparison.Ordinal);\n"
     "        Assert.Contains(\"ruling R40.17\", ScenarioA1MandatoryFire.PassBar(owed, false, \"x\"), StringComparison.Ordinal);\n\n"
     "        Assert.Null(ScenarioA1MandatoryFire.FireBar([], true, [\"other\"], \"y\", \"x\", []));\n"
     "        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, true, [\"s1\", \"s2\"], \"x\", \"x\", []));\n"
     "        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, true, [\"s1\", \"s2\", \"leader\"], \"x\", \"x\", []));\n"
     "        Assert.Contains(\"s2 must join\", ScenarioA1MandatoryFire.FireBar(owed, true, [\"s1\"], \"x\", \"x\", []), StringComparison.Ordinal);\n"
     "        Assert.StartsWith(\"play.fire-mandatory:\", ScenarioA1MandatoryFire.FireBar(owed, true, [\"s1\", \"s2\"], \"y\", \"x\", []), StringComparison.Ordinal);\n"),
    ("        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, false, [\"s1\"], \"x\", \"x\"));\n"
     "        Assert.StartsWith(\"play.fire-mandatory:\", ScenarioA1MandatoryFire.FireBar(owed, false, [\"other\"], \"x\", \"x\"), StringComparison.Ordinal);\n"
     "        Assert.StartsWith(\"play.fire-mandatory:\", ScenarioA1MandatoryFire.FireBar(owed, false, [\"s1\", \"other\"], \"x\", \"x\"), StringComparison.Ordinal);\n",
     "        Assert.Null(ScenarioA1MandatoryFire.FireBar(owed, false, [\"s1\"], \"x\", \"x\", []));\n"
     "        Assert.StartsWith(\"play.fire-mandatory:\", ScenarioA1MandatoryFire.FireBar(owed, false, [\"other\"], \"x\", \"x\", []), StringComparison.Ordinal);\n"
     "        Assert.StartsWith(\"play.fire-mandatory:\", ScenarioA1MandatoryFire.FireBar(owed, false, [\"s1\", \"other\"], \"x\", \"x\", []), StringComparison.Ordinal);\n\n"
     "        // Ruling R40.17: a unit that could have fired in one group with those named must join them.\n"
     "        Assert.Contains(\"s3 can fire in one group\", ScenarioA1MandatoryFire.FireBar([\"s1\", \"s2\", \"s3\"], false, [\"s1\"], \"x\", \"x\", [\"s3\"]), StringComparison.Ordinal);\n"),
    ("        Assert.Contains(\"one of s1, s2\", ScenarioA1MandatoryFire.Note(owed, false, \"[C9]\"), StringComparison.Ordinal);\n",
     "        Assert.Contains(\"ruling R40.17\", ScenarioA1MandatoryFire.Note(owed, false, \"[C9]\"), StringComparison.Ordinal);\n"
     "        Assert.Contains(\"(A8.312)\", ScenarioA1MandatoryFire.HandOverText(\"[C9]\"), StringComparison.Ordinal);\n\n"
     "        // A5.13 to A5.132 (p. 52): the target's overstacking penalty is the movers' in the MPh, and Personnel's.\n"
     "        Assert.True(ScenarioA1Stacking.TargetPenaltyApplies(false, false, true));\n"
     "        Assert.True(ScenarioA1Stacking.TargetPenaltyApplies(true, true, true));\n"
     "        Assert.False(ScenarioA1Stacking.TargetPenaltyApplies(true, false, true));\n"
     "        Assert.False(ScenarioA1Stacking.TargetPenaltyApplies(false, false, false));\n"),
])
print("ok")
