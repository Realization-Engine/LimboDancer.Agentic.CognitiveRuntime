"""Pass 50, task 50.3: the tests, added to the pass's two test classes."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
R = "tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass50RulesTests.cs"
P = "tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass50Tests.cs"

e.edit(R, """/// disabled (D3.7, D5.341), and the MA beside the vehicle's other weapons.
/// </summary>""", """/// disabled (D3.7, D5.341), and the MA beside the vehicle's other weapons. Task 50.3: the MA while moving: Bounding First Fire with Cases C, C1, C2,
/// and C4 (C5.3 to C5.35, D3.3), the AFPh after it (D3.32), a Delay MP (D2.17), and the turret turned with an MP expenditure (D3.12).
/// </summary>""")
e.edit(R, """        Assert.Contains("Recalls", ScenarioA1RallyRules.VehicleWeaponRepairSummary("de-tank", "ma", true), StringComparison.Ordinal);
    }
}
""", """        Assert.Contains("Recalls", ScenarioA1RallyRules.VehicleWeaponRepairSummary("de-tank", "ma", true), StringComparison.Ordinal);
    }

    // A tank's MA shot at an enemy tank's front: the German PzKpfw IIIH (a T turret, ROF 2) or the Russian T-34 (a RST turret).
    private static OrdnanceShot MaShot(string phase = "MPh", string? kind = ScenarioA1FireCalculator.BoundingFirstFire, int? mpInLos = 99, bool inMotion = false, bool moved = false,
        bool boundingFirer = false, int turn = 0, string side = "phasing", bool russian = false, int[]? dice = null)
    {
        var id = russian ? "defender-tank" : "attacker-tank";
        return new OrdnanceShot(phase, side, russian ? "russian" : "german", new OrdnanceGun(id + "-1", id, false, 0, false, false),
            new OrdnanceCrew(id + "-1", id, false, false, false, false, false), At, 3, turn, false, true, 0,
            new FireAttack(phase, side, null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 2, null),
            new OrdnanceRolls(dice ?? [5, 6], null, null, null, null))
        {
            Vehicle = new OrdnanceVehicleFirer(true, inMotion, false, false, false)
            {
                MpInLos = kind is null ? null : mpInLos,
                Moved = moved ? true : null,
                BoundingFirer = boundingFirer ? true : null,
            },
            VehicleTarget = new OrdnanceVehicleTarget("target-1", russian ? "attacker-tank" : "defender-tank", "front", "front", false, false, false, false, true),
            Ammunition = "ap",
            ScenarioYear = 1942,
            FireKind = kind,
        };
    }

    private static OrdnanceResolution Shoot(OrdnanceShot shot) => ScenarioA1OrdnanceCalculator.Resolve(shot with { Rolls = shot.Rolls! with { ToKill = [6, 6] } }, Ordnance);

    private static (string Name, decimal Value) CaseOf(OrdnanceResolution result, string prefix = "case-c") =>
        Assert.Single(result.ToHit!.Drm.Where(item => item.Name.StartsWith(prefix, StringComparison.Ordinal) && !item.Name.StartsWith("case-c4", StringComparison.Ordinal))
            .Select(item => (item.Name, item.Value)));

    [Fact]
    public void ABoundingFirstFirersMaTakesCaseCAndItsSubcasesByTheMpInLos()
    {
        // C5.3: "it must add the +2 DRM of Case B to the applicable Gun Type DRM of Case C"; the EX: "a T or ST Gun Type must add a +4 DRM".
        Assert.Equal(("case-c", 4m), CaseOf(Shoot(MaShot())));
        Assert.Equal(("case-c", 4m), CaseOf(Shoot(MaShot(russian: true))));

        // C5.33: more than three MP in a continuous LOS is Case C; C5.31: two and a half to three is C1; C5.32: at most two is C2. The MP are FRU.
        Assert.Equal(("case-c", 4m), CaseOf(Shoot(MaShot(mpInLos: 4))));
        Assert.Equal(("case-c1", 5m), CaseOf(Shoot(MaShot(mpInLos: 3))));
        Assert.Equal(("case-c2", 6m), CaseOf(Shoot(MaShot(mpInLos: 2))));
        Assert.Equal(("case-c2", 6m), CaseOf(Shoot(MaShot(mpInLos: 0))));

        // Case B is part of Case C and is not added again; C5.13: Case A never applies, and a shot that would turn the turret is outside.
        Assert.DoesNotContain(Shoot(MaShot()).ToHit!.Drm, item => item.Name == "case-b");
        Assert.Contains("asl.a1.ordnance.covered-arc-fixed", Shoot(MaShot(turn: 1)).Reasons);

        // D3.3: the phasing side's vehicle, in the MPh; a record that names no MP in LOS is incomplete.
        Assert.Contains("asl.a1.ordnance.phase-outside", Shoot(MaShot(side: "non-phasing")).Reasons);
        Assert.Contains("asl.a1.ordnance.phase-outside", Shoot(MaShot(phase: "PFPh")).Reasons);
        Assert.NotEqual(OrdnanceResolution.Resolved, Shoot(MaShot(mpInLos: null)).Disposition);

        // D3.3: "is marked with a Bounding Fire counter [EXC: if the only weapon fired did not exhaust its Multiple ROF]".
        Assert.Equal((false, "bounding-fire"), (Shoot(MaShot(dice: [5, 6])).Gun!.RateOfFireKept, Shoot(MaShot(dice: [5, 6])).Gun!.FireCounter));
        Assert.Equal((true, (string?)null), (Shoot(MaShot(dice: [2, 6])).Gun!.RateOfFireKept, Shoot(MaShot(dice: [2, 6])).Gun!.FireCounter));
        Assert.Equal(UnitCondition.BoundingFire, ScenarioA1OrdnanceEventRules.ShotMarker("MPh", "bounding-fire"));
        Assert.Equal(UnitCondition.FirstFire, ScenarioA1OrdnanceEventRules.ShotMarker("MPh", "first-fire"));
        Assert.Null(ScenarioA1OrdnanceEventRules.ShotMarker("MPh", null));
    }

    [Fact]
    public void AMotionFirerDoublesTheLowerDrOfItsToHitDr()
    {
        // C5.35 and its EX: Case C, C1, or C2 as applicable, and the lower dr doubled: a DR of 1 and 4 adds 1, of 2 and 2 adds 2.
        var moving = Shoot(MaShot(mpInLos: 2, inMotion: true, dice: [1, 4]));
        Assert.Equal(("case-c2", 6m), CaseOf(moving));
        Assert.Equal(1m, Assert.Single(moving.ToHit!.Drm, item => item.Name == "case-c4:lower-dr").Value);
        Assert.Equal(5 + 6 + 1 + 1, moving.ToHit.FinalDr);
        Assert.Equal(2m, Assert.Single(Shoot(MaShot(inMotion: true, dice: [2, 2])).ToHit!.Drm, item => item.Name == "case-c4:lower-dr").Value);
        Assert.DoesNotContain(Shoot(MaShot(dice: [1, 4])).ToHit!.Drm, item => item.Name == "case-c4:lower-dr");

        // D2.42: in its AFPh too, with Case C; D2.4: never in the PFPh; the DEFENDER's vehicle in Motion is not built.
        var afph = Shoot(MaShot(phase: "AFPh", kind: null, inMotion: true, dice: [3, 4]));
        Assert.Equal(("case-c", 4m), CaseOf(afph));
        Assert.Equal(3m, Assert.Single(afph.ToHit!.Drm, item => item.Name == "case-c4:lower-dr").Value);
        Assert.Contains("asl.a1.ordnance.vehicle-fire-outside", Shoot(MaShot(phase: "PFPh", kind: null, inMotion: true)).Reasons);
        Assert.Contains("asl.a1.ordnance.vehicle-fire-outside", Shoot(MaShot(phase: "DFPh", kind: null, inMotion: true, side: "non-phasing")).Reasons);
    }

    [Fact]
    public void InTheAfphAVehicleThatMovedOrFiredInItsMphUsesCaseC()
    {
        // C5.3: "a vehicle ... which has entered a new hex/hexside during its MPh but does not fire until its AFPh is using Bounding Fire and always
        // uses Case C"; C5.2: one that did not takes Case B; D3.32: the shot on the ROF its Bounding First Fire kept uses Case C.
        Assert.Equal(("case-c", 4m), CaseOf(Shoot(MaShot(phase: "AFPh", kind: null, moved: true))));
        Assert.DoesNotContain(Shoot(MaShot(phase: "AFPh", kind: null, moved: true)).ToHit!.Drm, item => item.Name == "case-b");
        Assert.Equal(("case-c", 4m), CaseOf(Shoot(MaShot(phase: "AFPh", kind: null, boundingFirer: true))));
        Assert.Equal(("case-b", 2m), CaseOf(Shoot(MaShot(phase: "AFPh", kind: null)), "case-b"));
        Assert.DoesNotContain(Shoot(MaShot(phase: "AFPh", kind: null)).ToHit!.Drm, item => item.Name.StartsWith("case-c", StringComparison.Ordinal));

        // D3.32: "if the vehicle did not exhaust its Multiple ROF during its MPh, and did not fire any other weapons ... it may fire that Multiple
        // ROF weapon (only) again once during its AFPh". A Bounding Fire counter says the ROF is spent.
        VehicleWeaponShotFacts[] ma = [new("ma", "mph", 0, false, 0)];
        Assert.False(ScenarioA1OrdnanceEligibility.VehicleWeapons(ma, "afph", "t").OtherPhase);
        Assert.True(ScenarioA1OrdnanceEligibility.VehicleWeapons(ma, "afph", "t", boundingFireMarked: true).OtherPhase);
        Assert.True(ScenarioA1OrdnanceEligibility.VehicleWeapons([.. ma, new(FireVehicleWeapon.CoaxialMg, "mph", 0, false)], "afph", "t").OtherPhase);
        Assert.True(ScenarioA1OrdnanceEligibility.VehicleWeapons([new("ma", "pfph", 0, false)], "afph", "t").OtherPhase);
    }

    [Fact]
    public void TheMphGivesAVehicleItsShotsADelayMpAndATurretTurnedWithAnMp()
    {
        // D3.3: the phasing side's vehicle fires as Bounding First Fire when it may; the other side at the moving stack while its window is open.
        Assert.Equal((ScenarioA1FireCalculator.BoundingFirstFire, (string?)null), ScenarioA1OrdnanceEligibility.MphShotKind(true, true, () => true, false, () => false));
        Assert.StartsWith("play.ordnance-bounding", ScenarioA1OrdnanceEligibility.MphShotKind(true, true, () => false, false, () => false).Refusal, StringComparison.Ordinal);
        Assert.Equal(ScenarioA1OrdnanceEligibility.MphWindowRefusal, ScenarioA1OrdnanceEligibility.MphShotKind(true, false, () => true, false, () => false).Refusal);
        Assert.Equal(("first-fire", (string?)null), ScenarioA1OrdnanceEligibility.MphShotKind(false, true, () => false, true, () => true));
        Assert.Equal(ScenarioA1OrdnanceEligibility.MphWindowRefusal, ScenarioA1OrdnanceEligibility.MphShotKind(false, false, () => false, false, () => true).Refusal);

        // C5.3: "may not fire again until it has expended another MP (even if only a Delay MP)".
        Assert.Null(ScenarioA1OrdnanceEligibility.BoundingShotBar("de-tank", null, 0));
        Assert.StartsWith("play.ordnance-bounding-mp", ScenarioA1OrdnanceEligibility.BoundingShotBar("de-tank", 0, 0), StringComparison.Ordinal);
        Assert.Null(ScenarioA1OrdnanceEligibility.BoundingShotBar("de-tank", 0, 1));
        Assert.Null(ScenarioA1OrdnanceEligibility.BoundingShotBar("de-tank", 0, 0, rofSpent: true));
        Assert.StartsWith("play.ordnance-bounding-arc", ScenarioA1OrdnanceMapRules.BoundingArcRefusal("de-tank", true, 1), StringComparison.Ordinal);
        Assert.Null(ScenarioA1OrdnanceMapRules.BoundingArcRefusal("de-tank", true, 0));
        Assert.Null(ScenarioA1OrdnanceMapRules.BoundingArcRefusal("de-tank", false, 2));

        // D2.17, D3.3: a Delay MP while stopped or before starting, for one MP of those left (in half MP).
        Assert.Null(ScenarioA1VehicleMovementCalculator.DelayBar("de-tank", false, 0, 26));
        Assert.StartsWith("play.move-vehicle-delay", ScenarioA1VehicleMovementCalculator.DelayBar("de-tank", true, 0, 26), StringComparison.Ordinal);
        Assert.StartsWith("play.move-vehicle-mp", ScenarioA1VehicleMovementCalculator.DelayBar("de-tank", false, 25, 26), StringComparison.Ordinal);

        // D3.12: the turret turns with a Delay, a stop, a VCA change, or an entry; a vehicle with a turret; not in woods, a building, or rubble.
        Assert.Null(ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "delay", true, true, false));
        Assert.Null(ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "enter", true, true, false));
        Assert.Contains("no turret", ScenarioA1VehicleMovementCalculator.TurretStepBar("de-ht", "stop", false, true, false), StringComparison.Ordinal);
        Assert.Contains("hexspine", ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "stop", true, false, false), StringComparison.Ordinal);
        Assert.Contains("Delay MP, a stop", ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "start", true, true, false), StringComparison.Ordinal);
        Assert.Contains("doubles", ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "stop", true, true, true), StringComparison.Ordinal);
    }
}
""")

e.edit(P, """    private Task<PlayResult> Repair(string weapon, int dr) =>""", """    private Task<PlayResult> Step(string kind, string? turret = null)
    {
        var arguments = new Dictionary<string, string> { ["vehicleId"] = "de-tank", ["kind"] = kind };
        if (turret is not null)
        {
            arguments["turret"] = turret;
        }

        return Do(GameActions.MoveVehicle, NoRoll(), arguments);
    }

    private async Task Pass() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private OrdnanceFired LastShot(long before) => Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single();

    private Task<PlayResult> Repair(string weapon, int dr) =>""")
e.edit(P, """        Assert.True(Is(tank, Conditions.Disabled));
        Assert.True(Is(tank, Conditions.Malfunctioned));
        Assert.True(Is(tank, Conditions.Recalled));
    }
}
""", """        Assert.True(Is(tank, Conditions.Disabled));
        Assert.True(Is(tank, Conditions.Malfunctioned));
        Assert.True(Is(tank, Conditions.Recalled));
    }

    [Fact]
    public async Task ATanksMaFiresAtTheOutsetOfItsMphSpendsADelayMpAndFiresAgain()
    {
        // D3.3, C5.33: at the outset of its MPh the MA fires with Case C (+4 for a T turret). A colored dr of 2 keeps the ROF of 2: no counter.
        await Setup(Tank("north-east"), Squad("r1", Target));
        await Advance();
        await Advance();
        Assert.Equal("mph", Current.Phase);
        var before = Revision;
        Committed(await FireMa(2, 6));
        var first = LastShot(before);
        Assert.Equal(4, DrmOf(first.Resolution.GetProperty("toHit").GetProperty("drm"), "case-c"));
        Assert.Equal("bounding-first-fire", first.Facts.GetProperty("fireKind").GetString());
        var state = Current;
        Assert.False(Is(state.Unit("de-tank")!, Conditions.BoundingFire));
        Assert.Equal(0, Assert.Single(state.VehicleWeaponShots).Step);

        // C5.3: another MP before the next shot; D2.17: a Delay MP will do, and the DEFENDER may answer it.
        Refused(await FireMa(2, 6), "play.ordnance-bounding-mp");
        Committed(await Step("delay"));
        Refused(await FireMa(2, 6), "play.ordnance-bounding");
        await Pass();

        // The second shot loses the ROF on a colored 5: a Bounding Fire counter (D3.3), and no shot after it.
        Committed(await FireMa(5, 6));
        state = Current;
        Assert.True(Is(state.Unit("de-tank")!, Conditions.BoundingFire));
        Assert.NotEqual(PlayOutcome.Committed, (await FireMa(2, 6)).Outcome);

        // A tank that only spent Delay MP ends its move stopped, not in Motion (D2.4), and does not fire in the AFPh (D3.3).
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        if (Current.Movement is { WindowOpen: true })
        {
            await Pass();
        }

        Assert.False(Is(Current.Unit("de-tank")!, Conditions.Motion));
        await Advance();
        await Advance();
        Assert.Equal("afph", Current.Phase);
        Assert.NotEqual(PlayOutcome.Committed, (await FireMa(2, 6)).Outcome);
    }

    [Fact]
    public async Task TheMaThatKeptItsRofInTheMphFiresOnceInTheAfphWithCaseC()
    {
        // D3.32: the MA kept its ROF in Bounding First Fire and no other weapon fired: one shot in the AFPh, with Case C and not Case B.
        await Setup(Tank("north-east"), Squad("r1", Target));
        await Advance();
        await Advance();
        Committed(await FireMa(2, 6));
        await Advance();
        await Advance();
        Assert.Equal("afph", Current.Phase);
        var before = Revision;
        Committed(await FireMa(2, 6));
        var drm = LastShot(before).Resolution.GetProperty("toHit").GetProperty("drm");
        Assert.Equal(4, DrmOf(drm, "case-c"));
        Assert.Equal(0, DrmOf(drm, "case-b"));
        Assert.NotEqual(PlayOutcome.Committed, (await FireMa(2, 6)).Outcome);
    }

    [Fact]
    public async Task TheTurretTurnsWithADelayMpAndNotWithAStart()
    {
        // D3.12: "the TCA may also change freely with each MP expended during the MPh".
        await Setup(Tank("north-east"), Squad("r1", Target));
        await Advance();
        await Advance();
        Refused(await Step("start", "east"), "play.move-vehicle-turret");
        Committed(await Step("delay", "east"));
        var state = Current;
        Assert.Equal(UnitFacing.East, state.TurretFacings.Single(item => item.Vehicle == "de-tank").Facing);
        Assert.Equal(UnitFacing.NorthEast, ((MapPosition)state.Unit("de-tank")!.Position!).Facing);
        Assert.True(state.Movement is { WindowOpen: true, Stopped: true, Started: false });

        // C5.13: the MA now fires only within the TCA it was turned to: the squad in B8 lies outside it.
        await Pass();
        Refused(await FireMa(2, 6), "play.ordnance-bounding-arc");
    }
}
""")
e.apply()
