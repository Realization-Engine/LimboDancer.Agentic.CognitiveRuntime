using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The backlog pass 7 in the Ordnance package: the Vehicle Target Type (C3.31), hit location and armor (C3.9, D1.6), To Kill (C7), Special
/// Ammunition (C8.9), Shock and the crew's checks (C7.4, D5.5, D5.6), and tanks firing their MA (D1.3), under rulings R7.1 to R7.12. The
/// Russian T-34 M41 (76L, RST, AF 11/6, red TH#) and the German PzKpfw IIIH (50, T, AF 6/3, turret front inferior, black TH#) fire at each
/// other in 1942.
/// </summary>
public sealed class ScenarioA1Pass7Tests
{
    private static readonly ScenarioA1OrdnanceReference Reference = new ScenarioA1OrdnancePackage().Reference;
    private const string At = "bd01:G5:0";

    private static FireAttack Hit(string phase, string side, string terrain = "open-ground", FireTarget[]? targets = null) =>
        new(phase, side, null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, terrain, targets ?? [], 2, null);

    private static OrdnanceVehicleTarget Target(string definition, string hull = "front", string? turret = "front", bool moving = false, bool nonStopped = false,
        bool concealed = false) => new(definition + "-1", definition, hull, turret, moving, nonStopped, concealed, false, true);

    private static OrdnanceShot Tank(string nationality, string ammunition = "ap", int range = 3, string phase = "PFPh", OrdnanceVehicleTarget? target = null,
        int turn = 0, bool buttonedUp = true, bool inMotion = false, int? year = 1942, int[]? toHit = null)
    {
        var id = nationality == "russian" ? "defender-tank" : "attacker-tank";
        var side = phase == "DFPh" ? "non-phasing" : "phasing";
        return new OrdnanceShot(phase, side, nationality, new OrdnanceGun(id + "-1", id, false, 0, false, false),
            new OrdnanceCrew(id + "-1", id, false, false, false, false, false), At, range, turn, false, true, 0, Hit(phase, side),
            new OrdnanceRolls(toHit, null, null, null, null))
        {
            Vehicle = new OrdnanceVehicleFirer(buttonedUp, inMotion, false, false, false),
            VehicleTarget = target ?? Target(nationality == "russian" ? "attacker-tank" : "defender-tank"),
            Ammunition = ammunition,
            ScenarioYear = year,
        };
    }

    private static OrdnanceResolution Resolve(OrdnanceShot shot, int[]? toKill = null, int[]? shock = null, int[]? crew = null, int[]? survival = null) =>
        ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Rolls = shot.Rolls! with
            {
                ToKill = toKill,
                ShockCheck = shock,
                CrewCheck = crew,
                CrewSurvival = survival
            }
        }, Reference);

    [Fact]
    public void TheReviewedTanksAreGunsOfTheirCaliber()
    {
        // R7.1, D1.3: the MA is a Gun of the vehicle's caliber, with its MA type and special ammunition.
        var t34 = Reference.Guns["defender-tank"];
        Assert.Equal(("vehicle", 76, "l", "rst", 12), (t34.GunType, t34.Caliber, t34.Suffix, t34.MaType, t34.Breakdown));
        Assert.Equal(["A4^2", "A5^3", "A6^4"], t34.SpecialAmmo);
        var panzer = Reference.Guns["attacker-tank"];
        Assert.Equal(("vehicle", 50, "t", 2), (panzer.GunType, panzer.Caliber, panzer.MaType, panzer.RateOfFire));
        Assert.Equal(["A4^1", "A5^2"], panzer.SpecialAmmo);
        Assert.True(Reference.Armor.Vehicles["attacker-tank"].Turreted);
        Assert.Equal((11, 6), (Reference.Armor.Vehicles["defender-tank"].FrontAf, Reference.Armor.Vehicles["defender-tank"].SideAf));
    }

    [Fact]
    public void ATurretHitMeetsTheTurretArmorAndTheToKillDrEliminates()
    {
        // C3.31: the red Vehicle row at range 3 is 10; Case I +1 for the BU T-34. Colored 2 below white 4 strikes the turret (C3.9), whose
        // inferior front AF is one step below the hull's 6: 4 (D1.64). AP 76L Russian is 13 (C7.31), Case D 0 at 3 hexes: Final TK# 9.
        var result = Resolve(Tank("russian", toHit: [2, 4]), toKill: [3, 4], survival: [3, 3]);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal(10, result.ToHit!.ModifiedToHit);
        Assert.Contains(result.ToHit.Drm, item => item.Name == "case-i" && item.Value == 1);
        Assert.Equal(7, result.ToHit.FinalDr);
        Assert.True(result.ToHit.Hit);
        var kill = result.Kill!;
        Assert.Equal(("turret", "front", 13, 4, 9), (kill.HitLocation, kill.TargetFacing, kill.BasicTk, kill.ArmorFactor, kill.FinalTk));
        Assert.Equal(OrdnanceKill.Eliminated, kill.Result);

        // D5.6: the PzKpfw IIIH's CS# is 6; a Final DR of 6 places a crew beneath the wreck.
        Assert.Equal((6, 6, true), (kill.CrewSurvival!.FinalDr, kill.CrewSurvival.CrewSurvival, kill.CrewSurvival.Survived));
    }

    [Fact]
    public void TheToKillDrBurnsShocksAndDuds()
    {
        // C7.7: at most half the Final TK# of 9 burns; equal to it Shocks a turret hit; an Original 12 is a dud (C7.35).
        Assert.Equal(OrdnanceKill.Burn, Resolve(Tank("russian", toHit: [2, 4]), toKill: [1, 3]).Kill!.Result);
        var shock = Resolve(Tank("russian", toHit: [2, 4]), toKill: [4, 5]).Kill!;
        Assert.Equal((OrdnanceKill.Shock, true), (shock.Result, shock.Shocked));
        Assert.Equal(OrdnanceKill.Dud, Resolve(Tank("russian", toHit: [2, 4]), toKill: [6, 6]).Kill!.Result);
        Assert.Equal(OrdnanceKill.None, Resolve(Tank("russian", toHit: [2, 4]), toKill: [5, 6]).Kill!.Result);
    }

    [Fact]
    public void OneAboveTheTkGivesAPossibleShockNtc()
    {
        // C7.41: a non-HE hit one above the Final TK# gives a possible Shock: the crew's NTC decides.
        var asked = Resolve(Tank("russian", toHit: [2, 4]), toKill: [5, 5]);
        Assert.Equal(["asl.a1.ordnance.roll-missing:shockCheck"], asked.Reasons);
        var passed = Resolve(Tank("russian", toHit: [2, 4]), toKill: [5, 5], shock: [1, 1]).Kill!;
        Assert.Equal((OrdnanceKill.PossibleShock, (bool?)null), (passed.Result, passed.Shocked));
        var failed = Resolve(Tank("russian", toHit: [2, 4]), toKill: [5, 5], shock: [6, 6]).Kill!;
        Assert.Equal((OrdnanceKill.PossibleShock, (bool?)true, "NTC"), (failed.Result, failed.Shocked, failed.ShockCheck!.Kind));
    }

    [Fact]
    public void AHullHitEqualToTheTkImmobilizesAndTheCrewMayAbandon()
    {
        // C3.9: colored 4 above white 2 strikes the hull: AF 6, Final TK# 7. D5.5: the immobilized AFV's crew takes a TC; failure Abandons.
        var kept = Resolve(Tank("russian", toHit: [4, 2]), toKill: [3, 4], crew: [1, 1]).Kill!;
        Assert.Equal(("hull", 6, 7, OrdnanceKill.Immobilized), (kept.HitLocation, kept.ArmorFactor, kept.FinalTk, kept.Result));
        Assert.Null(kept.Abandoned);
        var abandoned = Resolve(Tank("russian", toHit: [4, 2]), toKill: [3, 4], crew: [6, 6]).Kill!;
        Assert.Equal((true, "TC"), (abandoned.Abandoned, abandoned.CrewCheck!.Kind));
    }

    [Fact]
    public void TheRearFacingAddsOneAndACriticalHitDoublesTheBasicTk()
    {
        // C7.21: +1 against the rear; C7.23: a Critical Hit doubles the Basic TK# first.
        var rear = Resolve(Tank("russian", target: Target("attacker-tank", "rear", "rear"), toHit: [4, 2]), toKill: [6, 6]).Kill!;
        Assert.Equal((14, 3, 11), (rear.ModifiedTk, rear.ArmorFactor, rear.FinalTk));
        var critical = Resolve(Tank("russian", toHit: [1, 1]), toKill: [6, 6]);
        Assert.True(critical.ToHit!.CriticalHit);
        Assert.Equal(26, critical.Kill!.ModifiedTk);
    }

    [Fact]
    public void ThePanzerMeetsTheT34sArmor()
    {
        // AP 50 German is 11 (C7.31), Case D 0 at 3 hexes; the T-34's front hull AF 11 leaves 0, its side 6 leaves 5. Black row 10.
        var front = Resolve(Tank("german", target: Target("defender-tank", "front", "front"), toHit: [4, 2]), toKill: [6, 6]);
        Assert.Equal(10, front.ToHit!.ModifiedToHit);
        Assert.Equal((11, 11, 0), (front.Kill!.BasicTk, front.Kill.ArmorFactor, front.Kill.FinalTk));
        var side = Resolve(Tank("german", target: Target("defender-tank", "side", "side"), toHit: [4, 2]), toKill: [6, 6]).Kill!;
        Assert.Equal(5, side.FinalTk);
    }

    [Fact]
    public void ApcrFollowsItsDepletionNumber()
    {
        // C8.9: the PzKpfw IIIH has APCR A5 from 1942 (A4^1 A5^2): below 5 it is used, at 5 used and run out, above 5 never there, and the
        // Gun has not fired. APCR 50 is 14 (C7.32), Case D +1 at 3 hexes; the C4.5 APCR To Hit change is 0 at 0 to 6 hexes.
        var target = Target("defender-tank", "side", "side");
        var used = Resolve(Tank("german", "apcr", target: target, toHit: [3, 1]), toKill: [6, 6]);
        Assert.Equal("used", used.AmmunitionUse);
        Assert.Equal((14, 15, 9), (used.Kill!.BasicTk, used.Kill.ModifiedTk, used.Kill.FinalTk));
        Assert.Equal("depleted", Resolve(Tank("german", "apcr", target: target, toHit: [3, 2]), toKill: [6, 6]).AmmunitionUse);
        var none = Resolve(Tank("german", "apcr", target: target, toHit: [4, 2]));
        Assert.Equal(("none", false, true), (none.AmmunitionUse, none.ToHit!.Hit, none.Gun!.RateOfFireKept));
        Assert.Null(none.Kill);
    }

    [Fact]
    public void SpecialAmmunitionNeedsItsYear()
    {
        // C8.3, R7.6: no APCR before its year, none without a scenario year, and none once depleted.
        Assert.Contains("asl.a1.ordnance.ammunition-unavailable", Resolve(Tank("german", "apcr", year: 1940, toHit: [3, 1])).Reasons);
        Assert.Contains("asl.a1.ordnance.ammunition-unavailable", Resolve(Tank("german", "apcr", year: null, toHit: [3, 1])).Reasons);
        var shot = Tank("german", "apcr", toHit: [3, 1]);
        Assert.Contains("asl.a1.ordnance.ammunition-depleted", Resolve(shot.WithDepleted("apcr")).Reasons);
        Assert.Contains("asl.a1.ordnance.ammunition-outside", Resolve(Tank("german", "canister", toHit: [3, 1])).Reasons);
    }

    [Fact]
    public void ATurretTurnCostsLessThanAGunAndKeepsTheRof()
    {
        // C5.1, R7.10: a T turret +1 per hexspine; an RST turret +2 then +1; a vehicle's ROF is not lowered by the turn.
        var panzer = Resolve(Tank("german", turn: 2, toHit: [1, 6]), toKill: [6, 6]);
        Assert.Contains(panzer.ToHit!.Drm, item => item.Name == "case-a:2" && item.Value == 2);
        Assert.Equal(2, panzer.Gun!.RateOfFire);
        Assert.True(panzer.Gun.RateOfFireKept);
        var t34 = Resolve(Tank("russian", turn: 2, toHit: [4, 2]), toKill: [6, 6]);
        Assert.Contains(t34.ToHit!.Drm, item => item.Name == "case-a:2" && item.Value == 3);
    }

    [Fact]
    public void MovingConcealedAndPointBlankTargetsChangeTheDr()
    {
        // C6.1 Case J +2 against a moving target; C6.2 Case K +2 against a concealed one; C6.3 Case L -2 at one hex, not against a
        // Non-Stopped target.
        var moving = Resolve(Tank("russian", target: Target("attacker-tank", moving: true, nonStopped: true), range: 1, toHit: [4, 2]), toKill: [6, 6]).ToHit!;
        Assert.Contains(moving.Drm, item => item.Name == "case-j" && item.Value == 2);
        Assert.DoesNotContain(moving.Drm, item => item.Name == "case-l");
        var close = Resolve(Tank("russian", range: 1, toHit: [4, 2]), toKill: [6, 6]).ToHit!;
        Assert.Contains(close.Drm, item => item.Name == "case-l" && item.Value == -2);
        var hidden = Resolve(Tank("russian", target: Target("attacker-tank", concealed: true), toHit: [4, 2]), toKill: [6, 6]).ToHit!;
        Assert.Contains(hidden.Drm, item => item.Name == "case-k" && item.Value == 2);
    }

    [Fact]
    public void AnAfvThatMayNotFireIsOutside()
    {
        // D1.321: an RST MA fires only BU; D2.4: not in Motion in the PFPh; C3.31: never at a friendly vehicle.
        Assert.Contains("asl.a1.ordnance.vehicle-fire-outside", Resolve(Tank("russian", buttonedUp: false, toHit: [4, 2])).Reasons);
        Assert.Contains("asl.a1.ordnance.vehicle-fire-outside", Resolve(Tank("russian", inMotion: true, toHit: [4, 2])).Reasons);
        Assert.DoesNotContain("asl.a1.ordnance.vehicle-fire-outside", Resolve(Tank("german", buttonedUp: false, toHit: [4, 2]), toKill: [6, 6]).Reasons);
        Assert.Contains("asl.a1.ordnance.vehicle-target-outside", Resolve(Tank("russian", target: Target("defender-truck", turret: null), toHit: [4, 2])).Reasons);
    }

    [Fact]
    public void AnUnarmoredVehicleTakesTheUnarmoredTk()
    {
        // C7.311: AP of 37 to 57mm is 8 against an unarmored vehicle, doubled on a Critical Hit; the Russian AT Gun hits the German truck.
        var shot = Tank("russian", target: Target("attacker-truck", "side", null), toHit: [4, 2]) with
        {
            Gun = new OrdnanceGun("ru-gun", "defender-at-gun", false, 0, false, false),
            Crew = new OrdnanceCrew("ru-crew", "defender-crew", false, false, false, false, false),
            Vehicle = null,
        };
        Assert.Equal(["asl.a1.ordnance.roll-missing:crewCheck"], Resolve(shot, toKill: [4, 4]).Reasons);
        var kill = Resolve(shot, toKill: [4, 4], crew: [3, 4]).Kill!;
        Assert.Equal((8, (int?)null, OrdnanceKill.Immobilized), (kill.FinalTk, kill.ArmorFactor, kill.Result));

        // D5.5, D5.1: an unarmored vehicle's crew takes the TC too, on the 1st Line morale of its nationality (the German 4-6-7: 7).
        Assert.Equal(("TC", 7), (kill.CrewCheck!.Kind, kill.CrewCheck.MoraleLevel));
        Assert.Null(kill.Abandoned);
        Assert.True(Resolve(shot, toKill: [4, 4], crew: [4, 4]).Kill!.Abandoned);
    }

    [Fact]
    public void AnImprobableHitsSubsequentDrPicksTheTurretOrTheHull()
    {
        // C3.6: at 25 hexes the red Vehicle row is 5, and Case I +1 with a moving, concealed target's Cases J and K (+2 each) leaves no Final
        // DR that hits: an Original 2 hits on a subsequent dr of 2 on the turret, of 3 on the hull.
        var shot = Tank("russian", range: 25, target: Target("attacker-tank", moving: true, concealed: true), toHit: [1, 1]);
        var turret = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Rolls = shot.Rolls! with
            {
                Subsequent = 2,
                ToKill = [6, 6]
            }
        }, Reference);
        Assert.True(turret.ToHit!.Improbable);
        Assert.Equal("turret", turret.Kill!.HitLocation);
        var hull = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Rolls = shot.Rolls! with
            {
                Subsequent = 3,
                ToKill = [6, 6]
            }
        }, Reference);
        Assert.Equal("hull", hull.Kill!.HitLocation);
    }

    [Fact]
    public void AStunRecoveryCounterAddsOneToTheFirersDrAndTheTargetsChecks()
    {
        // D5.34: "+1" after a Stun: +1 To Hit for the firer; +1 to the target crew's NTC, TC, and Crew Survival.
        var shot = Tank("russian", toHit: [4, 2]);
        var firer = Resolve(shot with
        {
            Vehicle = shot.Vehicle! with
            {
                StunRecovery = true
            }
        }, toKill: [6, 6]).ToHit!;
        Assert.Contains(firer.Drm, item => item.Name == "stun" && item.Value == 1 && item.Rule == "D5.34");
        var recovering = Tank("russian", target: Target("attacker-tank") with
        {
            StunRecovery = true
        }, toHit: [4, 2]);
        var tc = Resolve(recovering, toKill: [3, 4], crew: [4, 4]).Kill!;
        Assert.Equal((8, 9, 8, true), (tc.CrewCheck!.OriginalDr, tc.CrewCheck.FinalDr, tc.CrewCheck.MoraleLevel, tc.Abandoned == true));
        var survival = Resolve(Tank("russian", target: Target("attacker-tank") with
        {
            StunRecovery = true
        }, toHit: [2, 4]), toKill: [3, 4], survival: [3, 3]).Kill!;
        Assert.Equal((1, 7, false), (survival.CrewSurvival!.Drm, survival.CrewSurvival.FinalDr, survival.CrewSurvival.Survived));
    }

    [Fact]
    public void AnAbandonedOrShockedTargetsCrewTakesNoCheckAndNoSurvival()
    {
        // D5.5 (table player, item 7): no Immobilization TC for a Shocked, Stunned, or absent crew; D5.6: an Abandoned AFV has no crew to survive.
        var shocked = Resolve(Tank("russian", target: Target("attacker-tank") with
        {
            CrewMayTakeTc = false
        }, toHit: [4, 2]), toKill: [3, 4]).Kill!;
        Assert.Equal((OrdnanceKill.Immobilized, (FireCheck?)null), (shocked.Result, shocked.CrewCheck));
        var abandoned = Resolve(Tank("russian", target: Target("attacker-tank") with
        {
            CrewMayTakeTc = false,
            Abandoned = true
        }, toHit: [2, 4]), toKill: [3, 4]);
        Assert.Equal((OrdnanceResolution.Resolved, OrdnanceKill.Eliminated, (OrdnanceCrewSurvival?)null), (abandoned.Disposition, abandoned.Kill!.Result, abandoned.Kill.CrewSurvival));
    }

    [Fact]
    public void AMovingFirerIsOutside()
    {
        // D2.42, C5.35: Case C4 for a Motion firer is not built; C5.3: Case C for an AFPh shot after entering a new hex is not built.
        var shot = Tank("german", phase: "AFPh", toHit: [4, 2]);
        Assert.Contains("asl.a1.ordnance.vehicle-fire-outside", Resolve(shot with
        {
            Vehicle = shot.Vehicle! with
            {
                Moved = true
            }
        }).Reasons);
        Assert.Contains("asl.a1.ordnance.vehicle-fire-outside", Resolve(Tank("german", phase: "AFPh", inMotion: true, toHit: [4, 2])).Reasons);
        Assert.DoesNotContain("asl.a1.ordnance.vehicle-fire-outside", Resolve(shot, toKill: [6, 6]).Reasons);
    }

    [Fact]
    public void ATankFiresHeAtInfantryWithCaseI()
    {
        // D1.3, C5.9: a BU T-34 firing HE at Infantry adds Case I +1 on the Infantry Target Type.
        var shot = Tank("russian", "he", toHit: [6, 5]) with
        {
            VehicleTarget = null,
            Hit = Hit("PFPh", "phasing", targets: [new FireTarget("de-s", "attacker-squad", At, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }]),
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference);
        Assert.True(result.ToHit is not null, string.Join("; ", result.Reasons));
        Assert.Contains(result.ToHit.Drm, item => item.Name == "case-i" && item.Value == 1);
    }

    public static TheoryData<string> Accepted => ["t34-turret", "panzer-side-apcr", "t34-rear-moving"];

    private static OrdnanceShot Scenario(string name) => name switch
    {
        "t34-turret" => Tank("russian"),
        "panzer-side-apcr" => Tank("german", "apcr", target: Target("defender-tank", "side", "side")),
        _ => Tank("russian", target: Target("attacker-tank", "rear", "side", moving: true, nonStopped: true), range: 2),
    };

    [Theory]
    [MemberData(nameof(Accepted))]
    public void EveryRollSequenceOfAVehicleShotEndsResolved(string name)
    {
        // Every ordered To Hit DR, every subsequent dr, and every To Kill DR total; the NTC, TC, and Crew Survival answered with 7.
        var shot = Scenario(name);
        Assert.Empty(ScenarioA1OrdnanceCalculator.Precheck(shot, Reference));
        var ends = 0;
        for (var colored = 1; colored <= 6; colored++)
        {
            for (var white = 1; white <= 6; white++)
            {
                foreach (var subsequent in new int?[] { 1, 2, 3, 4, 5, 6 })
                {
                    for (var total = 2; total <= 12; total++)
                    {
                        int[] kill = total <= 7 ? [1, total - 1] : [total - 6, 6];
                        var result = ScenarioA1OrdnanceCalculator.Resolve(shot with
                        {
                            Rolls = new OrdnanceRolls([colored, white], subsequent, null, null, null)
                            {
                                ToKill = kill,
                                ShockCheck = [3, 4],
                                CrewCheck = [3, 4],
                                CrewSurvival = [3, 4],
                            },
                        }, Reference);
                        Assert.True(result.Disposition == OrdnanceResolution.Resolved, $"{name} {colored},{white} {subsequent} {total}: {string.Join("; ", result.Reasons)}");
                        ends++;
                    }
                }
            }
        }

        Assert.Equal(36 * 6 * 11, ends);
    }
}

internal static class Pass7ShotExtensions
{
    public static OrdnanceShot WithDepleted(this OrdnanceShot shot, string ammunition) => shot with
    {
        Gun = shot.Gun! with
        {
            Depleted = [ammunition]
        },
    };
}
