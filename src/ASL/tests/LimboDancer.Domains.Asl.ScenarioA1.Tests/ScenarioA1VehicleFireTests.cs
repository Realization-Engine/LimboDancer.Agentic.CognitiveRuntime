using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The Fire package as revised for unit step 25 (rulings R25.4 to R25.7): vehicles in the target Location on the IFT Vehicle line
/// (A7.308, A7.309), an open-topped AFV's CE crew in a General Collateral Attack (A7.307, D.8B, D5.31, D5.34, D5.341, A7.82), and
/// the SPW 251/1's MA AAMG firing on the IFT (D1.83, D3.5, D3.53, D2.42, D3.7).
/// </summary>
public sealed class ScenarioA1VehicleFireTests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    private static readonly FireDirector Leader = new("ru-l", "defender-leader", From, false, false, false, false, false);

    private static FireFirer Firer(string id, string definition = "defender-squad") => new(id, definition, From, false, false, false, false, false);

    private static FireTarget Target(string id, string definition) => new(id, definition, At, false, false, false, false, false, false, false);

    private static FireVehicle Vehicle(string id, string definition, bool crewExposed = true, bool stunned = false, bool recovering = false) =>
        new(id, definition, At, crewExposed, stunned, recovering, false);

    private static FireAttack Attack(int[] dice, FireVehicle[] vehicles, FireTarget[]? targets = null, int range = 2, int hindrance = 0,
        FireDirector? director = null, string terrain = "open-ground", FireFirer[]? firers = null) =>
        new FireAttack("PFPh", "phasing", true, From, At, firers ?? [Firer("ru-1"), Firer("ru-2")], director, range, true,
            new FireLos(false, hindrance, true, false), null, terrain, targets ?? [], 3, new FireRolls(dice, null, null, null))
        {
            Vehicles = vehicles,
        };

    private static FireVehicleFire HalftrackFire(bool crewExposed = true, bool motion = false, bool pinned = false, bool recovering = false,
        bool fired = false, bool rofShot = false) =>
        new("de-ht", "attacker-halftrack", From, crewExposed, motion, pinned, false, recovering, false, fired, rofShot);

    private static FireAttack HalftrackAttack(int[] dice, FireVehicleFire? vehicle = null, int range = 3, string phase = "PFPh", FireTarget[]? targets = null) =>
        new FireAttack(phase, "phasing", true, From, At, [], null, range, true, new FireLos(false, 0, true, false), null, "open-ground",
            targets ?? [Target("ru-s", "defender-squad")], 3, new FireRolls(dice, null, null, null))
        {
            VehicleFire = vehicle ?? HalftrackFire(),
        };

    private static FireVehicleEffect Only(FireResolution result) => Assert.Single(result.VehicleEffects!);

    [Theory]
    [InlineData(5, 6, FireVehicleEffect.None)]
    [InlineData(3, 4, FireVehicleEffect.Immobilized)]
    [InlineData(2, 4, FireVehicleEffect.Eliminated)]
    [InlineData(1, 2, FireVehicleEffect.BurningWreck)]
    public void AnUnarmoredTruckIsResolvedOnTheVehicleLineOfTheAttacksColumn(int colored, int white, string expected)
    {
        // Two 4-4-7 at two hexes: 8 FP, the 8 column's Kill Number 7 (A7.308): below 7 eliminates, 7 immobilizes, 3 (at most 3.5) burns.
        var result = ScenarioA1FireCalculator.Resolve(Attack([colored, white], [Vehicle("de-t", "attacker-truck")], director: Leader), Reference);

        Assert.Equal(FireResolution.Resolved, result.Disposition);
        var effect = Only(result);
        Assert.Equal((expected, 7, colored + white), (effect.Result, effect.KillNumber, effect.FinalDr));
    }

    [Fact]
    public void TheVehicleLineTakesTheAttacksHindranceButNeverThePersonnelTargetsTem()
    {
        // In woods (+1 TEM) with a +1 Hindrance: the squad's Final DR is 8, the truck's 7: immobilized (A7.308 EX).
        var result = ScenarioA1FireCalculator.Resolve(Attack([2, 4], [Vehicle("de-t", "attacker-truck")], [Target("de-s", "attacker-squad")], hindrance: 1,
            director: Leader, terrain: "woods") with
        {
            Rolls = new FireRolls([2, 4], null, new Dictionary<string, IReadOnlyList<int>> { ["de-s"] = [3, 3] }, null),
        }, Reference);

        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Equal(8, result.Arithmetic!.FinalDr);
        var effect = Only(result);
        Assert.Equal((FireVehicleEffect.Immobilized, 7), (effect.Result, effect.FinalDr));
        Assert.DoesNotContain(effect.Drm, item => item.Name.StartsWith("tem:", StringComparison.Ordinal));
    }

    [Fact]
    public void AnOriginalTwoThatDoesNotHarmTheTruckRollsTheUnlikelyKill()
    {
        // One 4-4-7 at two hexes: 4 FP, Kill Number 5; with a +4 Hindrance the Original 2 becomes 6: no effect, so a dr follows (A7.309).
        var attack = Attack([1, 1], [Vehicle("de-t", "attacker-truck")], hindrance: 4, director: Leader, firers: [Firer("ru-1")]);
        var asked = ScenarioA1FireCalculator.Resolve(attack, Reference);
        Assert.Equal(["asl.a1.fire.roll-missing:unlikelyKill:de-t"], asked.Reasons);

        var result = ScenarioA1FireCalculator.Resolve(attack with
        {
            Rolls = attack.Rolls! with
            {
                UnlikelyKill = new Dictionary<string, int> { ["de-t"] = 2 }
            },
        }, Reference);
        Assert.Equal((FireVehicleEffect.Eliminated, 2), (Only(result).Result, Only(result).UnlikelyKillDr));
    }

    [Fact]
    public void AnOpenToppedAfvIsUnharmedAndItsCeCrewTakesACollateralAttackWithTheCeDrm()
    {
        // 8 FP; the crew's Final DR is the Original 5 + 2 (CE, D5.31) = 7 on the 8 column.
        var attack = Attack([2, 3], [Vehicle("de-ht", "attacker-halftrack")], director: Leader);
        var column = Array.IndexOf(ScenarioA1FireReference.ColumnFp, 8);
        var outcome = Reference.Result(7, column);
        var asked = ScenarioA1FireCalculator.Resolve(attack, Reference);
        if (outcome is "NMC" or "PTC" || outcome.EndsWith("MC", StringComparison.Ordinal))
        {
            Assert.Equal(["asl.a1.fire.roll-missing:crewCheck:de-ht"], asked.Reasons);
        }

        // A failed MC Stuns the crew (D5.34); a passed one leaves it.
        var failed = ScenarioA1FireCalculator.Resolve(attack with
        {
            Rolls = attack.Rolls! with
            {
                CrewChecks = new Dictionary<string, IReadOnlyList<int>> { ["de-ht"] = [6, 5] }
            },
        }, Reference);
        var effect = Only(failed);
        Assert.Equal((FireVehicleEffect.None, 7), (effect.Result, effect.FinalDr));
        Assert.Contains(effect.Drm, item => item is { Name: "crew-exposed", Value: 2m, Rule: "D5.31" });
        Assert.Equal(outcome == "PTC" ? FireVehicleEffect.Pinned : FireVehicleEffect.Stunned, effect.CrewResult);
        Assert.Equal(8, effect.CrewCheck!.MoraleLevel);
    }

    [Fact]
    public void AKiaOrKResultRecallsTheCrewAndABuCrewIsNotVulnerable()
    {
        // 16 FP (two squads at one hex) and an Original 2: the crew's 4 on the 16 column (A7.301 to A7.302 results Recall, D5.341).
        var attack = Attack([1, 1], [Vehicle("de-ht", "attacker-halftrack")], range: 1, director: Leader);
        var outcome = Reference.Result(4, Array.IndexOf(ScenarioA1FireReference.ColumnFp, 16));
        Assert.Matches("KIA|^K/", outcome);
        Assert.Equal(FireVehicleEffect.Recalled, Only(ScenarioA1FireCalculator.Resolve(attack, Reference)).CrewResult);

        var buttoned = ScenarioA1FireCalculator.Resolve(Attack([1, 1], [Vehicle("de-ht", "attacker-halftrack", crewExposed: false)], range: 1, director: Leader), Reference);
        Assert.Equal((FireVehicleEffect.None, FireVehicleEffect.NotVulnerable), (Only(buttoned).Result, Only(buttoned).CrewResult));
    }

    [Fact]
    public void ACrewUnderStunPlusOneThatFailsItsMcIsRecalled()
    {
        var attack = Attack([2, 3], [Vehicle("de-ht", "attacker-halftrack", recovering: true)], director: Leader) with
        {
            Rolls = new FireRolls([2, 3], null, null, null) { CrewChecks = new Dictionary<string, IReadOnlyList<int>> { ["de-ht"] = [4, 4] } },
        };
        var outcome = Reference.Result(7, Array.IndexOf(ScenarioA1FireReference.ColumnFp, 8));
        var effect = Only(ScenarioA1FireCalculator.Resolve(attack, Reference));
        if (outcome.EndsWith("MC", StringComparison.Ordinal))
        {
            // 4 + 4 + the MC's number + 1 (Stun +1, D5.34) is above 8: a second Stun is a Recall (D5.342).
            Assert.Equal(FireVehicleEffect.Recalled, effect.CrewResult);
            Assert.Contains(effect.CrewCheck!.Drm, item => item.Name == "stun-recovery");
        }
    }

    [Fact]
    public void TheHalftracksAamgFiresOnTheIftWithoutCoweringAndKeepsItsRofOnAColoredOne()
    {
        // 3 FP at three hexes, the 2 column; doubles do not Cower (A7.9); a colored 1 keeps the ROF of 1 (C2.24).
        var doubles = ScenarioA1FireCalculator.Resolve(HalftrackAttack([6, 6]) with
        {
            Rolls = new FireRolls([6, 6], null, null, null),
        }, Reference);
        Assert.Equal(FireResolution.Resolved, doubles.Disposition);
        Assert.False(doubles.Arithmetic!.Cowered);
        Assert.Equal((3m, 2), (doubles.Arithmetic.TotalFirepower, doubles.Arithmetic.ColumnFp));
        var weapon = Assert.Single(doubles.WeaponEffects!);
        Assert.Equal(("de-ht", true), (weapon.EquipmentId, weapon.Malfunctioned));
        Assert.Equal(["de-ht"], doubles.FireCounterUnitIds);

        var kept = ScenarioA1FireCalculator.Resolve(HalftrackAttack([1, 6]), Reference);
        Assert.True(Assert.Single(kept.WeaponEffects!).RateOfFireRetained);
    }

    [Fact]
    public void TheAamgIsHalvedInTheAfphInMotionAndWhenPinnedAndLosesItsRof()
    {
        var afph = ScenarioA1FireCalculator.Resolve(HalftrackAttack([1, 6], phase: "AFPh"), Reference);
        Assert.Equal(1.5m, afph.Arithmetic!.TotalFirepower);
        Assert.False(Assert.Single(afph.WeaponEffects!).RateOfFireRetained);

        // D2.4: a vehicle in Motion does not Prep Fire, so the Motion halving is shown in the DFPh.
        var moving = ScenarioA1FireCalculator.Resolve(HalftrackAttack([1, 6], HalftrackFire(motion: true, pinned: true), phase: "DFPh") with
        {
            FiringSide = "non-phasing"
        },
            Reference);
        Assert.Equal(0.75m, moving.Arithmetic!.TotalFirepower);
        Assert.Contains(moving.Arithmetic.Firers[0].Multipliers, item => item.Rule == "D2.42");
        Assert.False(Assert.Single(moving.WeaponEffects!).RateOfFireRetained);

        var recovering = ScenarioA1FireCalculator.Resolve(HalftrackAttack([2, 6], HalftrackFire(recovering: true)), Reference);
        Assert.Contains(recovering.Arithmetic!.Drm, item => item.Rule == "D5.34" && item.Value == 1m);
    }

    [Fact]
    public void TheAamgNeedsACeCrewOneShotPerPlayerTurnAndItsRangeAndNoInfantryJoinsIt()
    {
        Assert.Contains("asl.a1.fire.vehicle-fire-outside", ScenarioA1FireCalculator.Resolve(HalftrackAttack([2, 3], HalftrackFire(crewExposed: false)), Reference).Reasons);
        Assert.Contains("asl.a1.fire.firer-already-fired", ScenarioA1FireCalculator.Resolve(HalftrackAttack([2, 3], HalftrackFire(fired: true)), Reference).Reasons);
        Assert.DoesNotContain("asl.a1.fire.firer-already-fired",
            ScenarioA1FireCalculator.Resolve(HalftrackAttack([2, 3], HalftrackFire(fired: true, rofShot: true)), Reference).Reasons);
        Assert.Contains("asl.a1.fire.out-of-range", ScenarioA1FireCalculator.Resolve(HalftrackAttack([2, 3], range: 17), Reference).Reasons);
        Assert.Contains("asl.a1.fire.vehicle-fire-outside",
            ScenarioA1FireCalculator.Resolve(HalftrackAttack([2, 3]) with
            {
                Firers = [Firer("de-1", "attacker-squad")]
            }, Reference).Reasons);
        Assert.Contains("asl.a1.fire.target-outside",
            ScenarioA1FireCalculator.Resolve(HalftrackAttack([2, 3], targets: [Target("de-s", "attacker-squad")]), Reference).Reasons);
    }

    [Fact]
    public void TwoVehiclesOrAVehicleUnderAnOrdnanceHitAreOutside()
    {
        Assert.Contains("asl.a1.fire.vehicle-outside",
            ScenarioA1FireCalculator.Resolve(Attack([3, 4], [Vehicle("de-t", "attacker-truck"), Vehicle("de-ht", "attacker-halftrack")]), Reference).Reasons);
        var hit = Attack([3, 4], [Vehicle("de-t", "attacker-truck")], firers: []) with
        {
            OrdnanceHit = new FireOrdnanceHit("ru-gun", 4, false),
        };
        Assert.Contains("asl.a1.fire.vehicle-outside", ScenarioA1FireCalculator.Resolve(hit, Reference).Reasons);
    }

    [Fact]
    public void ACasualtyMcRecallsTheCrew()
    {
        // A10.31, D5.341 (referee D1): an Original 12 on the crew's MC is a Casualty MC, which Recalls it rather than Stunning it.
        var column = Array.IndexOf(ScenarioA1FireReference.ColumnFp, 8);
        var (colored, white) = Enumerable.Range(1, 6).SelectMany(one => Enumerable.Range(1, 6).Select(two => (one, two)))
            .First(pair => Reference.Result(pair.one + pair.two + 2, column) is var outcome && (outcome == "NMC" || outcome.EndsWith("MC", StringComparison.Ordinal)));
        FireResolution Check(int[] crewDice) => ScenarioA1FireCalculator.Resolve(Attack([colored, white], [Vehicle("de-ht", "attacker-halftrack")], director: Leader) with
        {
            Rolls = new FireRolls([colored, white], null, null, null) { CrewChecks = new Dictionary<string, IReadOnlyList<int>> { ["de-ht"] = crewDice } },
        }, Reference);

        Assert.Equal(FireVehicleEffect.Recalled, Only(Check([6, 6])).CrewResult);
        Assert.Equal(FireVehicleEffect.Stunned, Only(Check([6, 5])).CrewResult);
    }

    [Fact]
    public void AVehicleInMotionMayNotPrepFire()
    {
        // D2.4 (referee D3): a vehicle that starts its Player Turn in Motion may not Prep Fire; in the AFPh it fires halved (D2.42).
        Assert.Contains("asl.a1.fire.vehicle-fire-outside", ScenarioA1FireCalculator.Resolve(HalftrackAttack([2, 3], HalftrackFire(motion: true)), Reference).Reasons);
        Assert.DoesNotContain("asl.a1.fire.vehicle-fire-outside",
            ScenarioA1FireCalculator.Resolve(HalftrackAttack([2, 3], HalftrackFire(motion: true), phase: "AFPh"), Reference).Reasons);
    }

    [Fact]
    public void InfantryWithAnAfvAndAnAfvInTerrainWithATemAreOutside()
    {
        // D9.3 (referee D4): Infantry sharing a Location with an AFV; D5.31 (referee D9): the CE DRM is not cumulative with a positive TEM.
        Assert.Contains("asl.a1.fire.vehicle-outside",
            ScenarioA1FireCalculator.Resolve(Attack([3, 4], [Vehicle("de-ht", "attacker-halftrack")], [Target("de-s", "attacker-squad")]), Reference).Reasons);
        Assert.Contains("asl.a1.fire.vehicle-outside",
            ScenarioA1FireCalculator.Resolve(Attack([3, 4], [Vehicle("de-ht", "attacker-halftrack")], terrain: "woods"), Reference).Reasons);

        // An unarmored truck gives Infantry no cover, and the Vehicle line ignores the TEM (A7.308 EX).
        Assert.DoesNotContain("asl.a1.fire.vehicle-outside",
            ScenarioA1FireCalculator.Resolve(Attack([3, 4], [Vehicle("de-t", "attacker-truck")], [Target("de-s", "attacker-squad")], terrain: "woods"), Reference).Reasons);
    }
}
