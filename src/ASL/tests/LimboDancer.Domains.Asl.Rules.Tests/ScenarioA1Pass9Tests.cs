using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The backlog pass 9 in the Ordnance and Fire packages (rulings R9.1 to R9.8): light mortars on the Area Target Type (C9, C3.33, C3.331),
/// spotting (C9.3, C9.31), a leader directing a SW (A7.531), Air Bursts (B13.3), and the Panzerfaust (C13.3 to C13.36). The German 5cm leGrW 36
/// and the Russian 50mm RM obr. 40.
/// </summary>
public sealed class ScenarioA1Pass9Tests
{
    private static readonly ScenarioA1OrdnanceReference Reference = new ScenarioA1OrdnancePackage().Reference;
    private const string At = "bd01:G5:0";

    private static FireTarget Target(string id, string definition = "defender-squad", bool concealed = false) =>
        new(id, definition, At, false, false, concealed, false, false, false, false)
        {
            KnownEnemyInLos = true,
            Captors = [],
        };

    private static FireAttack Hit(string phase, string side, string terrain, FireTarget[] targets, int month = 7) =>
        new(phase, side, null, null, At, [], null, null, true, new FireLos(false, 0, true, false), month, terrain, targets, 2, null);

    private static OrdnanceShot Mortar(string phase = "PFPh", int range = 5, string terrain = "open-ground", FireTarget[]? targets = null,
        string crew = "attacker-squad")
    {
        var side = phase is "DFPh" or "MPh" ? "non-phasing" : "phasing";
        return new OrdnanceShot(phase, side, "german", new OrdnanceGun("de-mtr", "attacker-light-mortar", false, 0, false, false),
            new OrdnanceCrew("de-s", crew, false, false, false, false, false), At, range, 0, false, true, 0,
            Hit(phase, side, terrain, targets ?? [Target("ru-s")]), new OrdnanceRolls(null, null, null, null, null))
        {
            TargetType = OrdnanceTargetTypes.Area,
        };
    }

    private static OrdnanceShot Panzerfaust(string phase = "PFPh", int range = 2, int year = 1944, int month = 7, string crew = "attacker-squad",
        string target = "defender-tank")
    {
        var side = phase is "DFPh" or "MPh" ? "non-phasing" : "phasing";
        return new OrdnanceShot(phase, side, "german", new OrdnanceGun("de-s:pf", OrdnanceTargetTypes.Panzerfaust, false, 0, false, false),
            new OrdnanceCrew("de-s", crew, false, false, false, false, false), At, range, 0, false, true, 0, Hit(phase, side, "open-ground", [], month),
            new OrdnanceRolls(null, null, null, null, null))
        {
            VehicleTarget = new OrdnanceVehicleTarget("ru-tank", target, "front", "front", false, false, false, false, true),
            Ammunition = "heat",
            ScenarioYear = year,
            Panzerfaust = new OrdnancePanzerfaust(0, 4, false, false),
        };
    }

    private static OrdnanceResolution Resolve(OrdnanceShot shot, int[] toHit, int[]? ift = null, int? subsequent = null, IReadOnlyDictionary<string, int>? selection = null,
        int? pfCheck = null, int[]? toKill = null)
    {
        var rolls = shot.Rolls! with { ToHit = toHit, Subsequent = subsequent, CriticalSelection = selection, PanzerfaustCheck = pfCheck, ToKill = toKill };
        var last = string.Empty;
        for (var step = 0; step < 40; step++)
        {
            var result = ScenarioA1OrdnanceCalculator.Resolve(shot with { Rolls = rolls }, Reference);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.ordnance.roll-missing:", StringComparison.Ordinal))
            {
                return result;
            }

            var key = reason["asl.a1.ordnance.roll-missing:".Length..];
            last = key;
            if (key is "crewSurvival" or "crewCheck" or "shockCheck")
            {
                rolls = key == "crewSurvival" ? rolls with { CrewSurvival = [3, 4] } : key == "crewCheck" ? rolls with { CrewCheck = [3, 4] } : rolls with { ShockCheck = [3, 4] };
                continue;
            }

            var critical = key.StartsWith("critical-hit:", StringComparison.Ordinal);
            var inner = key[(key.IndexOf(':', StringComparison.Ordinal) + 1)..];
            var fire = (critical ? rolls.CriticalHit : rolls.Hit) ?? new FireRolls(null, null, null, null);
            var unit = inner.Contains(':', StringComparison.Ordinal) ? inner[(inner.IndexOf(':', StringComparison.Ordinal) + 1)..] : string.Empty;
            fire = inner.StartsWith("attack", StringComparison.Ordinal) ? fire with { Attack = ift ?? [3, 4] }
                : inner.StartsWith("randomSelection:", StringComparison.Ordinal) ? fire with { RandomSelection = unit.Split(',').ToDictionary(id => id, _ => 3) }
                : inner.StartsWith("checks:", StringComparison.Ordinal) ? fire with { Checks = new Dictionary<string, IReadOnlyList<int>>(fire.Checks ?? new Dictionary<string, IReadOnlyList<int>>()) { [unit] = [3, 4] } }
                : fire with { WoundSeverity = new Dictionary<string, int> { [unit] = 2 } };
            rolls = critical ? rolls with { CriticalHit = fire } : rolls with { Hit = fire };
        }

        throw new InvalidOperationException("The package kept asking for rolls: " + last);
    }

    private static decimal? Drm(OrdnanceResolution result, string name) => result.ToHit!.Drm.FirstOrDefault(item => item.Name == name)?.Value;

    [Fact]
    public void ALightMortarFiresAtTheRedAreaRowWithItsBarrelAndCaliberModifications()
    {
        // C3.33, C4.11, C4.2 (R9.2): 7 at 5 hexes; at 13, 8 less one for * and one for 57mm or less.
        var near = Resolve(Mortar(), [5, 6]);
        Assert.Equal(OrdnanceResolution.Resolved, near.Disposition);
        Assert.Equal(("red", 7, 7), (near.ToHit!.Color, near.ToHit.BasicToHit, near.ToHit.ModifiedToHit));
        var far = Resolve(Mortar(range: 13), [5, 6]);
        Assert.Equal((8, 6), (far.ToHit!.BasicToHit, far.ToHit.ModifiedToHit));

        // C9.4: never nearer than its minimum range (2; the Russian 3) nor beyond its maximum (13), nor on the Infantry Target Type.
        Assert.Contains("asl.a1.ordnance.out-of-range", Resolve(Mortar(range: 1), [5, 6]).Reasons);
        Assert.Contains("asl.a1.ordnance.out-of-range", Resolve(Mortar(range: 14), [5, 6]).Reasons);
        var russian = Mortar("DFPh", range: 2) with
        {
            FiringNationality = "russian",
            Gun = new OrdnanceGun("ru-mtr", "defender-light-mortar", false, 0, false, false),
            Crew = new OrdnanceCrew("ru-s", "defender-squad", false, false, false, false, false),
            Hit = Hit("DFPh", "non-phasing", "open-ground", [Target("de-s", "attacker-squad")]),
        };
        Assert.Contains("asl.a1.ordnance.out-of-range", Resolve(russian, [5, 6]).Reasons);
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(russian with { Range = 3 }, [5, 6]).Disposition);
        Assert.Contains("asl.a1.ordnance.target-type-outside", Resolve(Mortar() with { TargetType = null }, [5, 6]).Reasons);
    }

    [Fact]
    public void TheAreaTargetTypeJudgesEachUnitAndAttacksThoseHitAtHalfTheHeFp()
    {
        // C3.331 (R9.3): the concealed squad takes Case K +2; a DR of 6 hits the Known squad (6 <= 7) and misses the concealed one (8).
        var result = Resolve(Mortar(targets: [Target("ru-a"), Target("ru-b", concealed: true)]), [3, 3]);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal([("ru-a", 6, true), ("ru-b", 8, false)], result.AreaTargets!.Select(item => (item.UnitId, item.FinalDr, item.Hit)));
        Assert.Equal(2m, result.AreaTargets![1].Drm.Single(item => item.Name == "case-k").Value);

        // C3.33, C.6: the 50mm HE FP of 6, halved, attacks on the 2 column; no Case L at 1 or 2 hexes and no Case Q TEM on the To Hit DR.
        Assert.Equal(2, result.Hit!.Arithmetic!.ColumnFp);
        Assert.Equal(["ru-a"], result.Hit.Effects.Select(item => item.UnitId));
        var close = Resolve(Mortar(range: 2, terrain: "stone-building"), [5, 6]);
        Assert.Null(Drm(close, "case-l"));
        Assert.DoesNotContain(close.ToHit!.Drm, item => item.Name.StartsWith("case-q", StringComparison.Ordinal));

        // C3.331: the TEM applies to the Effects DR instead.
        var building = Resolve(Mortar(terrain: "stone-building"), [2, 3]);
        Assert.Contains(building.Hit!.Arithmetic!.Drm, item => item.Name == "tem:stone-building" && item.Value == 3);
    }

    [Fact]
    public void AMortarsCriticalHitNeedsAnOriginal2AndDoublesTheFullFp()
    {
        // C9.5, C3.7: a Final DR below half the TH# is not a Critical Hit on the Area Target Type; an Original 2 is, at 12 FP (C3.71).
        var low = Resolve(Mortar() with { Acquisition = -2 }, [1, 2]);
        Assert.Equal((true, false), (low.ToHit!.Hit, low.ToHit.CriticalHit));
        var critical = Resolve(Mortar(terrain: "wooden-building"), [1, 1]);
        Assert.True(critical.ToHit!.CriticalHit);
        Assert.Equal(12, critical.CriticalHit!.Arithmetic!.ColumnFp);
        Assert.Contains(critical.CriticalHit.Arithmetic.Drm, item => item.Name == "critical-hit-tem:wooden-building" && item.Value == -2);
    }

    [Fact]
    public void AMortarHitInWoodsTakesAirBurstsInsteadOfTheWoodsTem()
    {
        // B13.3 (R9.3): -1 instead of +1, also on a Critical Hit, where it is not reversed.
        var hit = Resolve(Mortar(terrain: "woods"), [2, 3]);
        Assert.Contains(hit.Hit!.Arithmetic!.Drm, item => item.Name == "air-burst" && item.Value == -1);
        Assert.DoesNotContain(hit.Hit.Arithmetic.Drm, item => item.Name == "tem:woods");
        var critical = Resolve(Mortar(terrain: "woods"), [1, 1]);
        Assert.Contains(critical.CriticalHit!.Arithmetic!.Drm, item => item.Name == "air-burst" && item.Value == -1);
    }

    [Fact]
    public void SpottedFireAddsTwoAndLowersTheRofAndAPinnedSpotterAddsCaseD()
    {
        // C9.31 (R9.4): +2, ROF 3 becomes 2; a pinned Spotter pins the mortar's firer for the shot (Case D, no Multiple ROF).
        var spotted = Mortar() with { Spotter = new OrdnanceSpotter("de-hs", "attacker-half-squad", false, false) };
        var result = Resolve(spotted, [2, 3]);
        Assert.Equal((2m, 2), (Drm(result, "spotted"), result.Gun!.RateOfFire));
        var pinned = Resolve(spotted with { Spotter = spotted.Spotter! with { Pinned = true } }, [2, 3]);
        Assert.Equal((2m, 0), (Drm(pinned, "case-d"), pinned.Gun!.RateOfFire));
        Assert.Contains("asl.a1.ordnance.spotter-outside", Resolve(spotted with { Spotter = spotted.Spotter! with { Broken = true } }, [2, 3]).Reasons);
        Assert.Contains("asl.a1.ordnance.target-type-outside", Resolve(spotted with { Phase = "MPh", FiringSide = "non-phasing", FireKind = "first-fire", Movement = new OrdnanceMovement(null, true, true, 1, 0) }, [2, 3]).Reasons);
    }

    [Fact]
    public void ALeaderDirectsAMortarAndALoneSmcFiresItWithoutMultipleRof()
    {
        // A7.531, C9.2 (R9.2): the 9-1's -1; a leader who directed this phase may not; a SMC firing it keeps no Multiple ROF.
        var leader = new FireDirector("de-9-1", "attacker-leader-9-1", At, false, false, false, false, false);
        var directed = Resolve(Mortar() with { Director = leader }, [2, 3]);
        Assert.Equal(-1m, Drm(directed, "leadership:de-9-1"));
        Assert.Contains("asl.a1.ordnance.director-outside", Resolve(Mortar() with { Director = leader with { DirectedThisPlayerTurn = true } }, [2, 3]).Reasons);
        Assert.Equal(0, Resolve(Mortar(crew: "attacker-leader-8-0"), [2, 3]).Gun!.RateOfFire);
        Assert.Equal(3, Resolve(Mortar(crew: "attacker-half-squad"), [2, 3]).Gun!.RateOfFire);
    }

    [Fact]
    public void AnAreaShotAtAHexWithFriendlyUnitsIsRefused()
    {
        // C3.33 (R9.3): friendly units would be hit too, on their own side's ELR, which the shot does not carry.
        Assert.Contains("asl.a1.ordnance.area-friendly-units", Resolve(Mortar(targets: [Target("ru-s"), Target("de-x", "attacker-squad")]), [2, 3]).Reasons);
    }

    [Fact]
    public void APanzerfaustCheckOf1To3GivesAShotAtTh10LessTwoPerHexAndHeatTk31()
    {
        // C13.31, C13.33, C13.34 (R9.7, R9.8): at 2 hexes the Modified TH# is 6; a hull hit on the T-34's front (AF 11) meets TK# 31: a 7 burns it.
        var result = Resolve(Panzerfaust(), [3, 2], pfCheck: 3, toKill: [3, 4]);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal((OrdnancePanzerfaustCheck.Shot, 10, 6), (result.PanzerfaustCheck!.Outcome, result.ToHit!.BasicToHit, result.ToHit.ModifiedToHit));
        Assert.Null(Drm(result, "case-l"));
        Assert.Equal((31, 20, OrdnanceKill.Burn), (result.Kill!.BasicTk, result.Kill.FinalTk, result.Kill.Result));
        Assert.Equal(0, result.Gun!.Acquisition);
    }

    [Fact]
    public void APanzerfaustCheckMayGiveNoShotOrPinOrBreakItsFirer()
    {
        // C13.31 (R9.7): +1 for a HS makes a 3 a 4, no shot; an Original 6 pins, or breaks a pinned unit, or reduces a berserk one.
        Assert.Equal(OrdnancePanzerfaustCheck.NoShot, Resolve(Panzerfaust(crew: "attacker-half-squad"), [2, 3], pfCheck: 3).PanzerfaustCheck!.Outcome);
        var pinned = Resolve(Panzerfaust(), [2, 3], pfCheck: 6);
        Assert.Equal((OrdnancePanzerfaustCheck.Pinned, "pinned"), (pinned.PanzerfaustCheck!.Outcome, pinned.FirerEffect));
        Assert.Null(pinned.ToHit);
        var alreadyPinned = Panzerfaust() with { Crew = Panzerfaust().Crew! with { Pinned = true } };
        Assert.Equal("broken", Resolve(alreadyPinned, [2, 3], pfCheck: 6).FirerEffect);
        Assert.Equal("casualty-reduction", Resolve(alreadyPinned with { Crew = alreadyPinned.Crew! with { Berserk = true } }, [2, 3], pfCheck: 6).FirerEffect);

        // -1 in 1945 makes a 4 a 3.
        Assert.Equal(OrdnancePanzerfaustCheck.Shot, Resolve(Panzerfaust(year: 1945, range: 3), [2, 3], pfCheck: 4, toKill: [3, 4]).PanzerfaustCheck!.Outcome);
    }

    [Fact]
    public void APanzerfaustIsBoundByDateRangeTargetAndUsage()
    {
        // C13.3, C13.32 (R9.7, R9.8): not before October 1943; one hex before June 1944; only at an AFV; not past the usage limit.
        Assert.Contains("asl.a1.ordnance.panzerfaust-date-outside", Resolve(Panzerfaust(range: 1, year: 1943, month: 9), [2, 3], pfCheck: 1).Reasons);
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(Panzerfaust(range: 1, year: 1943, month: 10), [2, 3], pfCheck: 1, toKill: [3, 4]).Disposition);
        Assert.Contains("asl.a1.ordnance.out-of-range", Resolve(Panzerfaust(range: 2, year: 1944, month: 5), [2, 3], pfCheck: 1).Reasons);
        Assert.Contains("asl.a1.ordnance.ammunition-outside", Resolve(Panzerfaust(target: "defender-truck"), [2, 3], pfCheck: 1).Reasons);
        Assert.Contains("asl.a1.ordnance.panzerfaust-exhausted", Resolve(Panzerfaust() with { Panzerfaust = new OrdnancePanzerfaust(4, 4, false, false) }, [2, 3], pfCheck: 1).Reasons);

        // A7.351: a squad checks twice in a phase, a HS once.
        var twice = Panzerfaust() with { Gun = Panzerfaust().Gun! with { ShotsThisPhase = 1 } };
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(twice, [2, 3], pfCheck: 1, toKill: [3, 4]).Disposition);
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Resolve(twice with { Crew = twice.Crew! with { DefinitionId = "attacker-half-squad" } }, [2, 3], pfCheck: 1).Reasons);
    }

    [Fact]
    public void APanzerfaustTakesCaseC3AndAnOriginal12ReducesItsFirer()
    {
        // C13.1, C13.8 (R9.8): +2 in the AFPh and +2 more from a ground-level building; C13.36: an Original 12 misses with Casualty Reduction.
        var afph = Resolve(Panzerfaust("AFPh") with { Panzerfaust = new OrdnancePanzerfaust(0, 4, true, false) }, [2, 3], pfCheck: 1);
        Assert.Equal((2m, 2m), (Drm(afph, "case-c3:afph"), Drm(afph, "case-c3:backblast")));
        Assert.Null(Drm(afph, "case-b"));
        var twelve = Resolve(Panzerfaust(), [6, 6], pfCheck: 1);
        Assert.Equal((false, "casualty-reduction", false), (twelve.ToHit!.Hit, twelve.FirerEffect, twelve.Gun!.Malfunctioned));
    }

    [Fact]
    public void TheAreaTargetAcquisitionAppliesToConcealedUnitsAndAlwaysSteps()
    {
        // C6.521, C6.57 (referee, pass 9): Case N applies to the concealed squad too, and a shot at concealed units alone acquires the hex, one
        // step more when it is already acquired.
        var result = Resolve(Mortar(targets: [Target("ru-b", concealed: true)]) with { Acquisition = -1 }, [5, 6]);
        Assert.Contains(result.AreaTargets![0].Drm, item => item.Name == "case-n" && item.Value == -1);
        Assert.Equal((-2, At), (result.Gun!.Acquisition, result.Gun.AcquiredLocationId));
    }

    [Fact]
    public void ACriticalHitIsJudgedForEachUnit()
    {
        // C3.7, C3.331 (referee, pass 9): at TH# 7 with a +3 Hindrance, the Known squad (lowest Final DR 5) takes a CH on an Original 2; the
        // concealed one (+2 more, lowest Final DR 7) only on a subsequent dr of 1. With a subsequent 2 both are hit and the Known squad alone
        // takes the CH.
        var shot = Mortar(targets: [Target("ru-a"), Target("ru-b", concealed: true)]) with { Hit = Mortar().Hit! with { Los = new FireLos(false, 3, true, false), Targets = [Target("ru-a"), Target("ru-b", concealed: true)] } };
        var result = Resolve(shot, [1, 1], subsequent: 2);
        Assert.Equal((true, true), (result.AreaTargets![0].Hit, result.AreaTargets[1].Hit));
        Assert.True(result.ToHit!.CriticalHit);
        Assert.Equal(["ru-a"], result.CriticalHit!.Effects.Select(item => item.UnitId));
        Assert.Equal(["ru-b"], result.Hit!.Effects.Select(item => item.UnitId));
    }

    [Fact]
    public void SpottedFireIsDesignatedInThePfphOrDfphAndAPinnedPanzerfaustFirerInABuildingMayNotFire()
    {
        // C9.3 (referee, pass 9): no Spotter in the AFPh; C13.8: from a ground-level building only an unpinned unit fires a PF.
        var spotted = Mortar("AFPh") with { Spotter = new OrdnanceSpotter("de-hs", "attacker-half-squad", false, false) };
        Assert.Contains("asl.a1.ordnance.target-type-outside", Resolve(spotted, [2, 3]).Reasons);
        var pinned = Panzerfaust() with { Crew = Panzerfaust().Crew! with { Pinned = true }, Panzerfaust = new OrdnancePanzerfaust(0, 4, true, false) };
        Assert.Contains("asl.a1.ordnance.panzerfaust-backblast", Resolve(pinned, [2, 3], pfCheck: 1).Reasons);
    }

    [Fact]
    public void AnInexperiencedPanzerfaustFirerIsReducedOnAnOriginal11()
    {
        // C13.36, A19.32 (referee, pass 9): 11 or 12 for Inexperienced Infantry.
        var conscript = Resolve(Panzerfaust(crew: "attacker-conscript-squad"), [5, 6], pfCheck: 1);
        Assert.Equal("casualty-reduction", conscript.FirerEffect);
        Assert.Null(Resolve(Panzerfaust(), [5, 6], pfCheck: 1, toKill: [3, 4]).FirerEffect);
    }
}
