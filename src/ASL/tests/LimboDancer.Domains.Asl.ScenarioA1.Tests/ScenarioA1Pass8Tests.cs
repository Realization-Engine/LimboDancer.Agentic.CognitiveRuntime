using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The backlog pass 8 in the Ordnance and Fire packages (rulings R8.1 to R8.10): Defensive First Fire by a Gun with Cases J to J4 and the
/// C6.17 limit, the First Fire counter and Intensive Fire (C2.241, C5.6), Cases E, H, and M, overstacking (A5.12, A5.131), Guns and their
/// crews as targets (C11), and a crew's own fire (A7.352). The German 7.5cm leIG 18 and the Russian 45mm PTP obr. 32 with their crews.
/// </summary>
public sealed class ScenarioA1Pass8Tests
{
    private static readonly ScenarioA1OrdnanceReference Reference = new ScenarioA1OrdnancePackage().Reference;
    private const string At = "bd01:G5:0";

    private static FireTarget Target(string id, string definition = "defender-squad") =>
        new(id, definition, At, false, false, false, false, false, false, false)
        {
            KnownEnemyInLos = true,
            Captors = [],
        };

    private static FireAttack Hit(string phase, string side, string terrain, FireTarget[] targets) =>
        new(phase, side, null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, terrain, targets, 2, null);

    private static OrdnanceShot German(string phase = "PFPh", int range = 5, string terrain = "open-ground", FireTarget[]? targets = null, bool woods = false,
        string crew = "attacker-crew")
    {
        var side = phase is "DFPh" or "MPh" ? "non-phasing" : "phasing";
        return new OrdnanceShot(phase, side, "german", new OrdnanceGun("de-gun", "attacker-inf-gun", false, 0, false, false),
            new OrdnanceCrew("de-crew", crew, false, false, false, false, false), At, range, 0, woods, true, 0,
            Hit(phase, side, terrain, targets ?? [Target("ru-s")]), new OrdnanceRolls(null, null, null, null, null));
    }

    private static OrdnanceShot FirstFire(OrdnanceShot shot, bool nonAssault = true, bool openGround = true, int spent = 1, int shots = 0) => shot with
    {
        FireKind = "first-fire",
        Movement = new OrdnanceMovement(null, nonAssault, openGround, spent, shots),
    };

    private static OrdnanceResolution Resolve(OrdnanceShot shot, int[] toHit, int[]? ift = null, int? subsequent = null)
    {
        var rolls = shot.Rolls! with { ToHit = toHit, Subsequent = subsequent };
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
    public void DefensiveFirstFireTakesCasesJ3AndJ4AndLeavesAFirstFireCounter()
    {
        // C6.13, C6.14 (R8.1): -1 each against Infantry using non-Assault Movement into Open Ground; a colored 5 above ROF 2: First Fire.
        var result = Resolve(FirstFire(German("MPh")), [5, 6]);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal((-1m, -1m), (Drm(result, "case-j3"), Drm(result, "case-j4")));
        Assert.Equal(("first-fire", false), (result.Gun!.FireCounter, result.Gun.RateOfFireKept));
        Assert.Null(Drm(Resolve(FirstFire(German("MPh"), nonAssault: false, openGround: false), [5, 6]), "case-j3"));
    }

    [Fact]
    public void DefensiveFirstFireAtATargetIsLimitedByTheMfItSpentThere()
    {
        // C6.17: no more shots at a target in a Location than the MF it spent there, a minimum of one.
        Assert.Contains("asl.a1.ordnance.first-fire-limit", Resolve(FirstFire(German("MPh"), spent: 1, shots: 1), [5, 6]).Reasons);
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(FirstFire(German("MPh"), spent: 2, shots: 1) with { Gun = German().Gun! with { ShotsThisPhase = 1, RateOfFireKept = true } }, [5, 6]).Disposition);
        Assert.Contains("asl.a1.ordnance.phase-outside", Resolve(FirstFire(German("PFPh")), [5, 6]).Reasons);
    }

    [Fact]
    public void DefensiveFirstFireAtAVehicleTakesCaseJ2J1OrJ()
    {
        // C6.11, C6.12: at most one MP in the firer's continuous LOS is Case J2 (+4), at most three Case J1 (+3), more Case J (+2).
        var shot = new OrdnanceShot("MPh", "non-phasing", "russian", new OrdnanceGun("ru-gun", "defender-at-gun", false, 0, false, false),
            new OrdnanceCrew("ru-crew", "defender-crew", false, false, false, false, false), At, 3, 0, false, true, 0, Hit("MPh", "non-phasing", "open-ground", []),
            new OrdnanceRolls(null, null, null, null, null))
        {
            FireKind = "first-fire",
            VehicleTarget = new OrdnanceVehicleTarget("de-tank", "attacker-tank", "side", "side", true, false, false, false, true),
            Ammunition = "ap",
            ScenarioYear = 1942,
        };
        decimal? CaseJ(int mp, string name) => ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Movement = new OrdnanceMovement(mp, null, null, 1, 0),
            Rolls = new OrdnanceRolls([6, 6], null, null, null, null),
        }, Reference).ToHit!.Drm.FirstOrDefault(item => item.Name == name)?.Value;
        Assert.Equal((4m, 3m, 2m), (CaseJ(1, "case-j2"), CaseJ(3, "case-j1"), CaseJ(5, "case-j")));
    }

    [Fact]
    public void AGunMarkedFirstFireFiresOnceMoreOnlyAsIntensiveFire()
    {
        // C2.241, C5.6, C5.61, C5.62 (R8.2): refused normally; as Intensive Fire it adds Case F +2, its B# is 10, and it is marked
        // Intensive Fire.
        var marked = German("DFPh") with { Gun = German().Gun! with { FirstFire = true } };
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Resolve(marked, [5, 6]).Reasons);
        var intensive = Resolve(marked with { IntensiveFire = true }, [5, 5]);
        Assert.Equal((2m, 10, true, "intensive-fire"), (Drm(intensive, "case-f"), intensive.Gun!.BreakdownNumber, intensive.Gun.Malfunctioned, intensive.Gun.FireCounter));
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Resolve(marked with { IntensiveFire = true, Gun = marked.Gun! with { IntensiveFired = true } }, [5, 6]).Reasons);
    }

    [Fact]
    public void IntensiveFireNeedsTheNormalRofUsedAndNotTheAfph()
    {
        // C5.6: only after its normal ROF is used, and never in the AFPh.
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Resolve(German() with { IntensiveFire = true }, [5, 6]).Reasons);
        var fired = German() with { Gun = German().Gun! with { FiredThisPlayerTurn = true } };
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(fired with { IntensiveFire = true }, [5, 6]).Disposition);
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Resolve(German("AFPh") with { Gun = fired.Gun, IntensiveFire = true }, [5, 6]).Reasons);
    }

    [Fact]
    public void CasesEHAndMAndOverstackingChangeTheToHitDr()
    {
        // C5.5 Case E: +2 in the Gun's own Location, +4 in woods, without Case L; C5.8 Case H: +2 for a squad manning it; C6.4 Case M: -2
        // instead of the Acquisition; A5.12 and A5.131: +1 per squad equivalent over for the firer, -1 for the target.
        var own = Resolve(German(range: 0) with { SameHex = true }, [5, 6]);
        Assert.Equal((2m, (decimal?)null), (Drm(own, "case-e"), Drm(own, "case-l")));
        Assert.Equal(4m, Drm(Resolve(German(range: 0, woods: true) with { SameHex = true }, [5, 6]), "case-e"));
        Assert.Contains("asl.a1.ordnance.out-of-range", Resolve(German(range: 0), [5, 6]).Reasons);
        Assert.Equal(2m, Drm(Resolve(German(crew: "attacker-squad") with { NonQualified = true }, [5, 6]), "case-h"));
        Assert.Contains("asl.a1.ordnance.crew-outside", Resolve(German(crew: "attacker-squad"), [5, 6]).Reasons);
        var sighted = Resolve(German() with { BoreSighted = true, Acquisition = -1 }, [5, 6]);
        Assert.Equal((-2m, (decimal?)null), (Drm(sighted, "case-m"), Drm(sighted, "case-n")));
        var crowded = Resolve(German() with { FirerOverstack = 1, TargetOverstack = 2 }, [5, 6]);
        Assert.Equal((1m, -2m), (Drm(crowded, "overstack-firer"), Drm(crowded, "overstack-target")));
    }

    private static OrdnanceShot AtTheGun(bool emplaced = true, bool gunshield = true)
    {
        var shot = German(targets: [Target("ru-crew", "defender-crew")]);
        return shot with { Hit = shot.Hit! with { GunTarget = new FireGunTarget("ru-gun", "defender-at-gun", "ru-crew", emplaced, gunshield) } };
    }

    [Fact]
    public void AnEmplacedGunAddsItsTargetSizeAndEmplacementToTheToHitDr()
    {
        // C11.2 (R8.3): +1 for the Small Target and +2 Emplacement TEM in Open Ground; not Emplaced, the size alone.
        var emplaced = Resolve(AtTheGun(), [5, 6]);
        Assert.Equal((1m, 2m), (Drm(emplaced, "case-p:small"), Drm(emplaced, "case-q:emplacement")));
        Assert.Null(Drm(Resolve(AtTheGun(emplaced: false), [5, 6]), "case-q:emplacement"));
    }

    [Fact]
    public void AnHeHitDestroysTheGunOnAKiaAndItsGunshieldProtectsTheCrewFromANearMiss()
    {
        // C11.4, C11.6: DR 2 and 3 + 3 = 8 hits at TH# 8; an IFT DR of 2 on the 12 column is a KIA: the Gun is destroyed with its crew.
        var direct = Resolve(AtTheGun(), [2, 3], ift: [1, 1]);
        Assert.Equal(("destroyed", true), (direct.GunTargetFate, direct.Hit!.Effects.Single().Eliminated));

        // A Near Miss: the gunshield adds +2 to the crew's IFT DR, and the Gun is untouched.
        var near = Resolve(AtTheGun(), [2, 3], ift: [5, 6]);
        Assert.Null(near.GunTargetFate);
        Assert.Contains(near.Hit!.Arithmetic!.Drm, item => item.Name == "gunshield:ru-gun" && item.Value == 2m);

        // A Critical Hit destroys the Gun and its crew.
        Assert.Equal("destroyed", Resolve(AtTheGun(), [1, 1], ift: [5, 6], subsequent: 1).GunTargetFate);
    }

    [Fact]
    public void InfantryFireAtAGunCrewTakesItsGunshieldOrEmplacementAndACrewFiresItsOwnFp()
    {
        // C11.5 (R8.3): the crew alone in Open Ground takes +2; with another unit the attack is not reviewed.
        var fire = new ScenarioA1FirePackage().Reference;
        FireAttack Attack(FireTarget[] targets, FireFirer firer) =>
            new("PFPh", "phasing", true, "bd01:G7:0", At, [firer], null, 2, true, new FireLos(false, 0, true, false), 7, "open-ground", targets, 2,
                new FireRolls([5, 6], null, null, null))
            {
                GunTarget = new FireGunTarget("ru-gun", "defender-at-gun", "ru-crew", false, true),
            };
        var squad = new FireFirer("g1", "attacker-squad", "bd01:G7:0", false, false, false, false, false);
        var alone = ScenarioA1FireCalculator.Resolve(Attack([Target("ru-crew", "defender-crew")], squad), fire);
        Assert.Contains(alone.Arithmetic!.Drm, item => item.Name == "gunshield:ru-gun" && item.Value == 2m);
        Assert.Contains("asl.a1.fire.crew-target-unreviewed",
            ScenarioA1FireCalculator.Resolve(Attack([Target("ru-crew", "defender-crew"), Target("ru-s")], squad), fire).Reasons);

        // A7.352 (R8.4): a crew fires its inherent FP, unless it fired its Gun this Player Turn.
        var crew = new FireFirer("de-crew", "attacker-crew", "bd01:G7:0", false, false, false, false, false);
        var target = Target("ru-s");
        var ok = ScenarioA1FireCalculator.Resolve(Attack([target], crew) with { GunTarget = null }, fire);
        Assert.DoesNotContain("asl.a1.fire.firer-outside", ok.Reasons);
        Assert.Contains("asl.a1.fire.firer-outside", ScenarioA1FireCalculator.Resolve(Attack([target], crew with { GunFired = true }) with { GunTarget = null }, fire).Reasons);
    }

    [Fact]
    public void IntensiveFireIsBarredAfterFinalFireAndWithAPinnedCrew()
    {
        // C5.6 (referee, pass 8): a Final Fire counter or a pinned crew bars Intensive Fire.
        var final = German("DFPh") with { Gun = German().Gun! with { FiredThisPlayerTurn = true, FinalFire = true }, IntensiveFire = true };
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Resolve(final, [5, 6]).Reasons);
        var pinned = German() with { Gun = German().Gun! with { FiredThisPlayerTurn = true }, Crew = German().Crew! with { Pinned = true }, IntensiveFire = true };
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Resolve(pinned, [5, 6]).Reasons);
    }

    [Fact]
    public void MpClaimedByAnEarlierShotCountNeitherForCaseJ1NorForTheLimit()
    {
        // C6.17 and its EX (referee, pass 8): with 2 MP seen and 1 claimed, the shot takes Case J2 for the 1 MP left; with every MP spent in
        // the Location claimed, no shot remains.
        var shot = new OrdnanceShot("MPh", "non-phasing", "russian", new OrdnanceGun("ru-gun", "defender-at-gun", false, 1, true, false),
            new OrdnanceCrew("ru-crew", "defender-crew", false, false, false, false, false), At, 3, 0, false, true, 0, Hit("MPh", "non-phasing", "open-ground", []),
            new OrdnanceRolls([6, 6], null, null, null, null))
        {
            FireKind = "first-fire",
            VehicleTarget = new OrdnanceVehicleTarget("de-tank", "attacker-tank", "side", "side", true, false, false, false, true),
            Ammunition = "ap",
            ScenarioYear = 1942,
        };
        var claimed = ScenarioA1OrdnanceCalculator.Resolve(shot with { Movement = new OrdnanceMovement(2, null, null, 2, 1) { MpClaimed = 1 } }, Reference);
        Assert.Contains(claimed.ToHit!.Drm, item => item.Name == "case-j2");
        Assert.Contains("asl.a1.ordnance.first-fire-limit",
            ScenarioA1OrdnanceCalculator.Resolve(shot with { Movement = new OrdnanceMovement(3, null, null, 2, 1) { MpClaimed = 2 } }, Reference).Reasons);
    }

    [Fact]
    public void ACriticalHitOfDefensiveFirstFireKeepsFfnamAndFfmo()
    {
        // C3.71 (referee, pass 8): the Critical Hit's Effects DR keeps FFNAM and FFMO.
        var shot = FirstFire(German("MPh"));
        shot = shot with { Hit = shot.Hit! with { TargetMovement = new FireMovement(false) } };
        var critical = Resolve(shot, [1, 1], ift: [5, 6], subsequent: 1);
        Assert.True(critical.ToHit!.CriticalHit);
        Assert.Contains(critical.CriticalHit!.Arithmetic!.Drm, item => item.Name == "ffnam");
        Assert.Contains(critical.CriticalHit.Arithmetic.Drm, item => item.Name == "ffmo");
    }
}
