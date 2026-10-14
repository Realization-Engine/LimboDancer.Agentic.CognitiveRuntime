"""Pass 50, the reviews' fixes: their tests."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
R = "tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass50RulesTests.cs"
e.edit(R, """        Assert.Equal(["is disabled", "has malfunctioned", "has fired", "the vehicle fired in another phase (D3.5)", null],""",
       """        Assert.Equal(["is disabled", "has malfunctioned", "has fired this Player Turn (D3.5)", "the vehicle fired in another phase (D3.5)", null],""")
e.edit(R, """    [Theory]
    [InlineData(1, "rear")]""", """    [Fact]
    public void TheReviewsFixes()
    {
        // C6.55 (p. 175): "only a Stabilized Gun (D11.1) may claim/retain its Acquisition DRM for Motion/Bounding-First Fire TH attempts".
        OrdnanceShot Acquired(OrdnanceShot shot) => shot with { Acquisition = -2 };
        Assert.Contains(Shoot(Acquired(MaShot(phase: "PFPh", kind: null))).ToHit!.Drm, item => item is { Name: "case-n", Value: -2 });
        Assert.DoesNotContain(Shoot(Acquired(MaShot())).ToHit!.Drm, item => item.Name == "case-n");
        Assert.DoesNotContain(Shoot(Acquired(MaShot(phase: "AFPh", kind: null, inMotion: true))).ToHit!.Drm, item => item.Name == "case-n");
        Assert.Equal(-1, Shoot(Acquired(MaShot(dice: [2, 3]))).Gun!.Acquisition);
        Assert.Equal(-2, Shoot(Acquired(MaShot(phase: "PFPh", kind: null, dice: [2, 3]))).Gun!.Acquisition);

        // D3.3, D7.1: a Bounding Fire counter beside a shot of the MA in the MPh, an OVR among them, is the MA's: it fires again only on a kept ROF.
        VehicleWeaponShotFacts[] ovr = [new("ma", "mph", 0, false, 3), new("bmg", "mph", 0, false, 3), new("cmg", "mph", 0, false, 3)];
        Assert.True(ScenarioA1OrdnanceEligibility.VehicleWeapons(ovr, "mph", "t", boundingFireMarked: true).BoundingFireSpent);
        Assert.Null(ScenarioA1OrdnanceEligibility.VehicleWeapons([new(FireVehicleWeapon.CoaxialMg, "mph", 0, false)], "mph", "t", boundingFireMarked: true).BoundingFireSpent);
        var spent = MaShot() with { Vehicle = MaShot().Vehicle! with { Weapons = new OrdnanceVehicleWeapons(false, false, null) { BoundingFireSpent = true } } };
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Shoot(spent).Reasons);
        Assert.DoesNotContain("asl.a1.ordnance.gun-already-fired", Shoot(spent with { Gun = spent.Gun! with { ShotsThisPhase = 1, RateOfFireKept = true } }).Reasons);

        // D3.51: the MA pays the CMG's Case A on its first shot after the CMG turned the turret, though it fired before the turn.
        VehicleWeaponShotFacts[] maThenCmg = [new("ma", "pfph", 0, false), new(FireVehicleWeapon.CoaxialMg, "pfph", 2, false)];
        Assert.Equal(2, ScenarioA1OrdnanceEligibility.VehicleWeapons(maThenCmg, "pfph", "t").MaintainedCaseA);
        Assert.Null(ScenarioA1OrdnanceEligibility.VehicleWeapons([.. maThenCmg, new("ma", "pfph", 0, false)], "pfph", "t").MaintainedCaseA);

        // D3.31, D3.3: the MGs fire in the AFPh when only the MA fired in the MPh and left no counter; not after a MG fired there.
        Assert.False(ScenarioA1FireEligibility.OtherFirePhase([new("ma", "mph", 0, false, 0)], "afph", false));
        Assert.True(ScenarioA1FireEligibility.OtherFirePhase([new("ma", "mph", 0, false, 0)], "afph", true));
        Assert.True(ScenarioA1FireEligibility.OtherFirePhase([new(FireVehicleWeapon.BowMg, "mph", 0, false)], "afph", false));

        // D5.41: an Abandoned vehicle fires and repairs nothing; D2.32: nothing of Bypass is read for a turret or a same-hex shot.
        Assert.StartsWith("play.fire-vehicle-abandoned", ScenarioA1FireEligibility.AbandonedVehicleBar("de-tank", true), StringComparison.Ordinal);
        Assert.Null(ScenarioA1FireEligibility.AbandonedVehicleBar("de-tank", false));
        Assert.True(ScenarioA1RallyRules.VehicleCrewImpaired(false, false, false, false, false, abandoned: true));
        Assert.Contains("Bypass", ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "stop", true, true, false, bypass: true), StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1OrdnanceMapRules.SameHexBypassRefusal(false, true));
        Assert.Null(ScenarioA1OrdnanceMapRules.SameHexBypassRefusal(false, false));

        // The repair's bars each say their own cause, and an unknown weapon name is refused.
        Assert.Contains("no weapon named", ScenarioA1RallyRules.VehicleWeaponRepairBar("de-ht", "aamg", true, false, false, false), StringComparison.Ordinal);
        Assert.Contains("has had its repair attempt", ScenarioA1RallyRules.VehicleWeaponRepairBar("de-tank", "cmg", true, false, false, true), StringComparison.Ordinal);
        Assert.Contains("Recalled", ScenarioA1RallyRules.VehicleWeaponRepairBar("de-tank", "cmg", true, false, true, false), StringComparison.Ordinal);

        // A proposal's words for the To Hit cases.
        Assert.Equal(("Case C2", "Case Q (woods)", "Case A (the CA change its turret already made)", "cx"), (ScenarioA1OrdnanceEligibility.CaseWords("case-c2"),
            ScenarioA1OrdnanceEligibility.CaseWords("case-q:woods"), ScenarioA1OrdnanceEligibility.CaseWords("case-a:maintained"), ScenarioA1OrdnanceEligibility.CaseWords("cx")));
    }

    [Theory]
    [InlineData(1, "rear")]""")
e.apply()
