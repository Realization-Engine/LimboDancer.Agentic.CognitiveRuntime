using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The A11.5 claim (the design, section 5): <see cref="ScenarioA1CloseCombatValueCalculator"/>, the CCV of Infantry against a vehicle (A11.5, p. 74), the
/// leader's DRM for a berserk attacker (A11.51), the defender's CCV against a vehicle's attack (A11.62, p. 76), the contract, and the forwards of
/// <see cref="ScenarioA1VehicleCloseCombat"/>, each equal to its old answer.
/// </summary>
public sealed class ScenarioA1CloseCombatValueTests
{
    private static readonly ScenarioA1CloseCombatReference CloseCombat = new ScenarioA1CloseCombatPackage().Reference;
    private const string At = "bd02:F4:0";

    private static readonly string[] Kinds = ["asl:squad", "asl:half-squad", "asl:crew", "asl:vehicle-crew", "asl:leader", "asl:hero"];

    private static CcvAttackFacts Attack(string kind = "asl:squad", bool engineer = false, bool inexperienced = false, string? smc = null, string? created = null,
        bool pinned = false, bool marked = false, bool concealed = false, bool guard = false, NationalCcv national = NationalCcv.None) =>
        new("u", kind, engineer, inexperienced, smc, created, pinned, marked, concealed, guard, national);

    [Fact]
    public void TheBaseCcvIsFiveFourThreeAndTwoByKind()
    {
        // A11.5 (p. 74): squad 5, Crew 4, HS 3, SMC 2.
        Assert.Equal([5, 3, 4, 4, 2, 2], Kinds.Select(ScenarioA1CloseCombatValueCalculator.BaseCcv));
    }

    [Theory]
    [InlineData("attacker-squad", "replaced-elr", true)]
    [InlineData("attacker-squad", "ammunition-shortage-replaced", true)]
    [InlineData("attacker-elite-squad-8-3-8", "replaced-by-two-half-squads", false)]
    [InlineData("attacker-elite-squad-8-3-8", "ammunition-shortage-half-squads", false)]
    [InlineData("attacker-elite-squad-8-3-8", "casualty-reduced-beyond-elr", true)]
    [InlineData("finnish-elite-squad", "casualty-reduced-beyond-elr", false)]
    [InlineData("attacker-squad", "battle-hardened", false)]
    public void AssaultEngineerLossDistinguishesLowerQualityFromOwnHalfSquads(string definition, string consequence, bool lost)
    {
        var effect = new FireUnitEffect("u", definition, definition, null, false, true, false, false, false, false, [consequence], []);
        Assert.Equal(lost, ScenarioA1CloseCombatValueCalculator.AssaultEngineerLostByFire(effect));
    }

    [Fact]
    public void AssaultEngineerLineagePreservesPermanentLossAndDoesNotTurnOrdinaryAncestryIntoLoss()
    {
        Assert.Equal((true, (string?)null), ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(false, [true], false));
        Assert.Equal((false, (string?)null), ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(true, [true], true));
        Assert.Equal((false, (string?)null), ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(false, [false], true));
        Assert.Equal(((bool?)null, (string?)null), ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(false, [null], false));
        Assert.Equal(((bool?)null, (string?)null), ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(true, [null], false));
        Assert.Equal(((bool?)null, (string?)null), ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(null, [true], true));
        Assert.Equal((true, (string?)null), ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(false, [true, true], false));
        Assert.NotNull(ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(false, [true, null], false).Reason);
    }

    [Fact]
    public void AssaultEngineerLineageRecordsTheLossOfAnUntouchedPrintedEngineer()
    {
        // A25.01 (p. 93): a printed AE Replaced by a lower-quality unit loses AE, "even if it subsequently Battle Hardens".
        Assert.Equal((false, (string?)null), ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(true, [null], false, [true]));
        Assert.Equal((true, (string?)null), ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(false, [null], false, [true]));
        Assert.Equal(((bool?)null, (string?)null), ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(true, [null], false, [false]));
        Assert.NotNull(ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(false, [null, null], false, [true, false]).Reason);
        Assert.Throws<ArgumentException>(() => ScenarioA1CloseCombatValueCalculator.AssaultEngineerLineage(true, [null], false, [true, true]));
    }

    [Fact]
    public void AssaultEngineerCasualtyLossReadsTheDefinitionAfterEarlierBattleHardening()
    {
        var effect = new FireUnitEffect("u", "finnish-green-squad", "finnish-green-half-squad", null, false, true, false, false, false, false,
            ["battle-hardened", "casualty-reduced-beyond-elr"], []);
        Assert.True(ScenarioA1CloseCombatValueCalculator.AssaultEngineerLostByFire(effect));
        Assert.False(ScenarioA1CloseCombatValueCalculator.AssaultEngineerLostByFire(effect with
        {
            Events = ["casualty-reduced-beyond-elr", "battle-hardened"],
        }));
    }

    [Fact]
    public void TheNationalCasesChangeTheBaseCcvOfTheKindsTheyName()
    {
        // A25.934 (p. 98): Ethiopian MMC one lower; G1.4231 (p. 279): a T-H Hero 5; KW 9.11 (p. 666): a TACP 3. A case for another kind changes nothing.
        Assert.Equal([4, 2, 3, 3, 2, 2], Kinds.Select(kind => ScenarioA1CloseCombatValueCalculator.BaseCcv(kind, NationalCcv.Ethiopian)));
        Assert.Equal([5, 3, 4, 4, 5, 5], Kinds.Select(kind => ScenarioA1CloseCombatValueCalculator.BaseCcv(kind, NationalCcv.TankHunterHero)));
        Assert.Equal([5, 3, 3, 3, 2, 2], Kinds.Select(kind => ScenarioA1CloseCombatValueCalculator.BaseCcv(kind, NationalCcv.Tacp)));
        Assert.Throws<ArgumentException>(() => ScenarioA1CloseCombatValueCalculator.BaseCcv("asl:squad", (NationalCcv)9));
        Assert.Equal(5, ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack("asl:hero", national: NationalCcv.TankHunterHero)).Ccv);
    }

    [Fact]
    public void AnAssaultEngineerAddsOneAloneWithALeaderAsAHalfSquadAndAsACrew()
    {
        // A11.5: +1 for an Assault Engineer: an 8-3-8 squad 6, with a combining 9-2 7, its HS 4, the AVRE's crew 5.
        Assert.Equal(6, ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack(engineer: true)).Ccv);
        var led = ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack(engineer: true, smc: "l"));
        Assert.Equal((5, 7), (led.Base, led.Ccv));
        Assert.Equal(["smc-combining:l", "assault-engineer:u"], led.Modifiers.Select(item => item.Name));
        Assert.Equal(4, ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack("asl:half-squad", engineer: true)).Ccv);
        Assert.Equal(5, ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack("asl:vehicle-crew", engineer: true)).Ccv);
    }

    [Fact]
    public void AnAssaultEngineerIsOneByItsCounterOrADesignationAndNoneOnceReplaced()
    {
        // A25.01 (p. 93), H1.22 (p. 329): a DC-symbol counter or a designation; "If an Assault Engineer is Replaced by a lower quality unit, it loses all
        // special capabilities". An unknown trait is not one.
        Assert.True(ScenarioA1CloseCombatValueCalculator.AssaultEngineer(true, false, false));
        Assert.True(ScenarioA1CloseCombatValueCalculator.AssaultEngineer(false, true, false));
        Assert.True(ScenarioA1CloseCombatValueCalculator.AssaultEngineer(null, true, false));
        Assert.False(ScenarioA1CloseCombatValueCalculator.AssaultEngineer(true, true, true));
        Assert.False(ScenarioA1CloseCombatValueCalculator.AssaultEngineer(null, false, false));
        Assert.False(ScenarioA1CloseCombatValueCalculator.AssaultEngineer(false, false, false));

        // The catalog carries the trait on the front face, present false on every entry today.
        var fire = new ScenarioA1FirePackage().Reference;
        Assert.False(fire.Definitions["attacker-elite-squad-8-3-8"].AssaultEngineer);
        Assert.Null(fire.Definitions["attacker-tank"].AssaultEngineer);
    }

    [Fact]
    public void EachHalvingPenaltyLowersTheCcvOnceAndTheCcvHasNoFloor()
    {
        // A11.5: -1 Inexperienced; -1 for each halving FP penalty: pinned (the p. 75 example, a pinned 5-4-8 is 4), a concealed vehicle (A11.19), an
        // outnumbered Guard (A20.52), D7.213's fire counter; the p. 208 example: pinned and marked Final Fire, 3.
        Assert.Equal(4, ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack(inexperienced: true)).Ccv);
        Assert.Equal(4, ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack(pinned: true)).Ccv);
        Assert.Equal(3, ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack(pinned: true, marked: true)).Ccv);
        Assert.Equal(4, ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack(concealed: true)).Ccv);
        Assert.Equal(4, ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack(guard: true)).Ccv);
        var all = ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack(engineer: true, inexperienced: true, smc: "l", pinned: true, marked: true, concealed: true, guard: true));
        Assert.Equal(["smc-combining:l", "assault-engineer:u", "inexperienced:u", "pinned", "vehicle-concealed", "guard-outnumbered", "reaction-fire-marked"],
            all.Modifiers.Select(item => item.Name));
        Assert.Equal(["A11.5", "A11.5", "A11.5", "A11.5", "A11.19", "A20.52", "D7.213"], all.Modifiers.Select(item => item.Rule));
        Assert.Equal(2, all.Ccv);

        // A SMC alone, pinned, against a concealed vehicle: 0, kept (A11.501's Unlikely Kill is all that is left).
        Assert.Equal(0, ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack("asl:leader", pinned: true, concealed: true)).Ccv);
        Assert.Equal(-1, ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack("asl:leader", pinned: true, concealed: true, guard: true)).Ccv);
    }

    [Fact]
    public void ACreatedLeaderCombinesOnlyWhereNoSmcDoes()
    {
        // A18.12: the created leader combines as a SMC, and A11.5's "No more than two units" leaves him out where a SMC combines.
        Assert.Equal(["created-leader-combining:c"], ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack(created: "c")).Modifiers.Select(item => item.Name));
        Assert.Equal(["smc-combining:l"], ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack(smc: "l", created: "c")).Modifiers.Select(item => item.Name));
    }

    [Fact]
    public void TheFinalDrBelowTheCcvEliminatesAtMostHalfBurnsAndEqualImmobilizes()
    {
        // The p. 76 example: CCV 6, an Original 7 immobilizes, 6 or less eliminates, 3 or less burns. The p. 74 example: CCV 6, Final DR 4, eliminated.
        Assert.Equal(VehicleCloseCombatResolution.None, ScenarioA1CloseCombatValueCalculator.VehicleResult(8, 6, false));
        Assert.Equal(VehicleCloseCombatResolution.Immobilized, ScenarioA1CloseCombatValueCalculator.VehicleResult(7, 7, false));
        Assert.Equal(VehicleCloseCombatResolution.Eliminated, ScenarioA1CloseCombatValueCalculator.VehicleResult(6, 7, false));
        Assert.Equal(VehicleCloseCombatResolution.Eliminated, ScenarioA1CloseCombatValueCalculator.VehicleResult(4, 6, false));
        Assert.Equal(VehicleCloseCombatResolution.BurningWreck, ScenarioA1CloseCombatValueCalculator.VehicleResult(3, 6, false));

        // D5.7: a red CS# burns on a Final DR one lower.
        Assert.Equal(VehicleCloseCombatResolution.Eliminated, ScenarioA1CloseCombatValueCalculator.VehicleResult(4, 7, false));
        Assert.Equal(VehicleCloseCombatResolution.BurningWreck, ScenarioA1CloseCombatValueCalculator.VehicleResult(4, 7, true));

        // The review's finding 7: a Final DR equal to a CCV of 0 or less immobilizes; above it, nothing.
        Assert.Equal(VehicleCloseCombatResolution.Immobilized, ScenarioA1CloseCombatValueCalculator.VehicleResult(0, 0, false));
        Assert.Equal(VehicleCloseCombatResolution.Immobilized, ScenarioA1CloseCombatValueCalculator.VehicleResult(-2, -2, true));
        Assert.Equal(VehicleCloseCombatResolution.None, ScenarioA1CloseCombatValueCalculator.VehicleResult(-1, -2, false));
        Assert.Equal(VehicleCloseCombatResolution.BurningWreck, ScenarioA1CloseCombatValueCalculator.VehicleResult(-1, 0, false));
    }

    [Fact]
    public void AtMostTwoUnitsCombineAndOneOfTwoIsASmc()
    {
        Assert.Null(ScenarioA1CloseCombatValueCalculator.CombinationRefusal(1, 0));
        Assert.Null(ScenarioA1CloseCombatValueCalculator.CombinationRefusal(2, 1));
        Assert.Contains("must be a SMC", ScenarioA1CloseCombatValueCalculator.CombinationRefusal(2, 0), StringComparison.Ordinal);
        Assert.Contains("no more than two", ScenarioA1CloseCombatValueCalculator.CombinationRefusal(3, 1), StringComparison.Ordinal);
        Assert.Contains("needs an attacker", ScenarioA1CloseCombatValueCalculator.CombinationRefusal(0, 0), StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyAnArmedDrySoundEnemyInfantryUnitAttacksAVehicleInThatOrder()
    {
        Assert.Null(ScenarioA1CloseCombatValueCalculator.AttackerRefusal(true, true, false, false, false, false));
        Assert.Contains("Infantry or Cavalry", ScenarioA1CloseCombatValueCalculator.AttackerRefusal(false, false, true, true, true, true), StringComparison.Ordinal);
        Assert.Contains("enemy", ScenarioA1CloseCombatValueCalculator.AttackerRefusal(true, false, true, true, true, true), StringComparison.Ordinal);
        Assert.Contains("broken", ScenarioA1CloseCombatValueCalculator.AttackerRefusal(true, true, true, true, true, true), StringComparison.Ordinal);
        Assert.Contains("captured", ScenarioA1CloseCombatValueCalculator.AttackerRefusal(true, true, false, true, true, true), StringComparison.Ordinal);
        Assert.Contains("(A20.5)", ScenarioA1CloseCombatValueCalculator.AttackerRefusal(true, true, false, false, true, true), StringComparison.Ordinal);
        Assert.Contains("(E6.2)", ScenarioA1CloseCombatValueCalculator.AttackerRefusal(true, true, false, false, false, true), StringComparison.Ordinal);
    }

    [Fact]
    public void ThePrcDieWithADestroyedVehicleOnly()
    {
        Assert.True(ScenarioA1CloseCombatValueCalculator.PrcEliminated(VehicleCloseCombatResolution.Eliminated));
        Assert.True(ScenarioA1CloseCombatValueCalculator.PrcEliminated(VehicleCloseCombatResolution.BurningWreck));
        Assert.False(ScenarioA1CloseCombatValueCalculator.PrcEliminated(VehicleCloseCombatResolution.Immobilized));
        Assert.False(ScenarioA1CloseCombatValueCalculator.PrcEliminated(VehicleCloseCombatResolution.Captured));
        Assert.False(ScenarioA1CloseCombatValueCalculator.PrcEliminated(VehicleCloseCombatResolution.None));
    }

    [Fact]
    public void ALeadersDrmAppliesOnlyWhenHeCombinesAndNeverToABerserkUnit()
    {
        // A11.51 (p. 74); A15.42 (p. 84): "a berserk unit never receives the leadership benefit of a friendly leader".
        Assert.True(ScenarioA1CloseCombatValueCalculator.LeaderDrmApplies(true, false));
        Assert.False(ScenarioA1CloseCombatValueCalculator.LeaderDrmApplies(true, true));
        Assert.False(ScenarioA1CloseCombatValueCalculator.LeaderDrmApplies(false, false));
    }

    [Fact]
    public void ADefendersCcvIsAtLeastOneAndTheExcessIsADrm()
    {
        // A11.62 (p. 76): the p. 75 example's pinned 2-3-8 HS has 2 (3 -1 pinned); an Assault Engineer HS 4 (ruling RA11C.3); an Ethiopian Inexperienced
        // pinned HS would be 0, so 1 and a -1 DRM; a SMC adds one with a MMC and has 2 alone (a T-H Hero 5).
        var pinnedHalf = new CcvDefenderFacts("asl:half-squad", false, false, false, true);
        Assert.Equal(2, ScenarioA1CloseCombatValueCalculator.DefenseCcv([pinnedHalf]));
        Assert.Equal(4, ScenarioA1CloseCombatValueCalculator.DefenseCcv([new CcvDefenderFacts("asl:half-squad", false, true, false, false)]));
        var ethiopian = new CcvDefenderFacts("asl:half-squad", false, false, true, true, NationalCcv.Ethiopian);
        Assert.Equal((0, 1, -1), (ScenarioA1CloseCombatValueCalculator.DefenderCcv(ethiopian), ScenarioA1CloseCombatValueCalculator.DefenseCcv([ethiopian]),
            ScenarioA1CloseCombatValueCalculator.DefenseCcvDrm(ethiopian)));
        Assert.Equal(0, ScenarioA1CloseCombatValueCalculator.DefenseCcvDrm(pinnedHalf));
        var leader = new CcvDefenderFacts("asl:leader", true, false, false, false);
        Assert.Equal(6, ScenarioA1CloseCombatValueCalculator.DefenseCcv([new CcvDefenderFacts("asl:squad", false, false, false, false), leader]));
        Assert.Equal(2, ScenarioA1CloseCombatValueCalculator.DefenseCcv([leader]));
        Assert.Equal(5, ScenarioA1CloseCombatValueCalculator.DefenseCcv([leader with { National = NationalCcv.TankHunterHero }]));
        Assert.Equal(0, ScenarioA1CloseCombatValueCalculator.DefenseCcvDrm(leader));
    }

    // The forwards: each old expression, copied here as it stood, against the calculator.

    private static int OldBaseCcv(string kind) => kind switch
    {
        "asl:squad" => 5,
        var crew when ScenarioA1Definitions.IsCrew(crew) => 4,
        "asl:half-squad" => 3,
        _ => 2,
    };

    private static (int Kill, List<FireModifier> Modifiers) OldCcvBlock(string kind, string mainId, string? smc, string? created, bool inexperienced, bool pinned,
        bool marked)
    {
        var ccv = OldBaseCcv(kind);
        var modifiers = new List<FireModifier>();
        if (smc is not null)
        {
            modifiers.Add(new FireModifier("smc-combining:" + smc, 1m, "A11.5"));
        }
        else if (created is not null)
        {
            modifiers.Add(new FireModifier("created-leader-combining:" + created, 1m, "A18.12"));
        }

        if (inexperienced)
        {
            modifiers.Add(new FireModifier("inexperienced:" + mainId, -1m, "A11.5"));
        }

        if (pinned)
        {
            modifiers.Add(new FireModifier("pinned", -1m, "A11.5"));
        }

        if (marked)
        {
            modifiers.Add(new FireModifier("reaction-fire-marked", -1m, "D7.213"));
        }

        return (ccv + (int)modifiers.Sum(item => item.Value), modifiers);
    }

    private static string OldResultBlock(int final, int kill, bool csRed) =>
        final * 2 <= kill || (final < kill && ScenarioA1CrewSurvivalRules.BurnsRed(csRed, final, lower => lower * 2 <= kill))
            ? VehicleCloseCombatResolution.BurningWreck
        : final < kill ? VehicleCloseCombatResolution.Eliminated
        : final == kill ? VehicleCloseCombatResolution.Immobilized
        : VehicleCloseCombatResolution.None;

    private static int OldDefenseCcv((string Kind, bool Smc, bool Inexperienced, bool Pinned)[] defenders)
    {
        var mmc = defenders.Where(item => !item.Smc).ToArray();
        var total = mmc.Sum(item => Math.Max(1, OldBaseCcv(item.Kind) - (item.Inexperienced ? 1 : 0) - (item.Pinned ? 1 : 0)));
        var smc = defenders.Length - mmc.Length;
        return total + (mmc.Length > 0 ? smc : smc * 2);
    }

    [Fact]
    public void TheBaseCcvAndTheCcvBlockForwardEquallyOverEveryCombination()
    {
        Assert.All(Kinds.Append("asl:other"), kind => Assert.Equal(OldBaseCcv(kind), ScenarioA1CloseCombatValueCalculator.BaseCcv(kind)));
        Assert.All(Kinds.Append("asl:other"), kind => Assert.Equal(OldBaseCcv(kind), ScenarioA1CloseCombatValueCalculator.BaseCcv(kind, NationalCcv.None)));
        string?[] smcs = [null, "l"];
        foreach (var kind in Kinds)
        {
            foreach (var smc in smcs)
            {
                foreach (var created in smcs)
                {
                    for (var flags = 0; flags < 8; flags++)
                    {
                        bool inexperienced = (flags & 1) != 0, pinned = (flags & 2) != 0, marked = (flags & 4) != 0;
                        var old = OldCcvBlock(kind, "u", smc, created is null ? null : "c", inexperienced, pinned, marked);
                        var now = ScenarioA1CloseCombatValueCalculator.AttackCcv(Attack(kind, false, inexperienced, smc, created is null ? null : "c", pinned, marked));
                        Assert.Equal(old.Kill, now.Ccv);
                        Assert.Equal(old.Modifiers, now.Modifiers);
                    }
                }
            }
        }
    }

    [Fact]
    public void TheResultBlockKeepsItsAnswerAndDiffersOnlyWhereADrEqualsOrExceedsACcvOfZeroOrLess()
    {
        // The review's finding 7: equal wherever the CCV is above 0; where it is 0 or less, only a Final DR from the CCV up to half of it differs, burning
        // in the old block and immobilized or nothing in the calculator.
        foreach (var csRed in new[] { false, true })
        {
            for (var kill = -4; kill <= 9; kill++)
            {
                for (var final = -8; final <= 16; final++)
                {
                    var old = OldResultBlock(final, kill, csRed);
                    var now = ScenarioA1CloseCombatValueCalculator.VehicleResult(final, kill, csRed);
                    if (kill > 0 || final < kill || final * 2 > kill)
                    {
                        Assert.Equal(old, now);
                    }
                    else
                    {
                        Assert.Equal(VehicleCloseCombatResolution.BurningWreck, old);
                        Assert.Equal(final == kill ? VehicleCloseCombatResolution.Immobilized : VehicleCloseCombatResolution.None, now);
                    }
                }
            }
        }
    }

    [Fact]
    public void TheDefenseCcvForwardsEquallyWithoutAssaultEngineersOrNationalCases()
    {
        (string Kind, bool Smc)[] kinds = [("asl:squad", false), ("asl:half-squad", false), ("asl:crew", false), ("asl:leader", true), ("asl:hero", true)];
        var singles = (from kind in kinds from flags in Enumerable.Range(0, 4) select (kind.Kind, kind.Smc, (flags & 1) != 0, (flags & 2) != 0)).ToArray();
        foreach (var first in singles)
        {
            foreach (var second in singles)
            {
                foreach (var defenders in new[] { new[] { first }, new[] { first, second } })
                {
                    Assert.Equal(OldDefenseCcv(defenders),
                        ScenarioA1CloseCombatValueCalculator.DefenseCcv([.. defenders.Select(item => new CcvDefenderFacts(item.Kind, item.Smc, false, item.Item3, item.Item4))]));
                }
            }
        }
    }

    // Through ScenarioA1VehicleCloseCombat.Resolve: the old overload's answer, and the context's.

    private static CloseCombatUnit Unit(string id, string definition, string side = "russian", bool pinned = false, bool inexperienced = false, bool berserk = false,
        bool unarmed = false) =>
        new(id, definition, side, false, pinned, false, false, berserk, false, false, false, false, false, false)
        {
            Inexperienced = inexperienced,
            Unarmed = unarmed ? true : null,
        };

    private static VehicleCloseCombatVehicle Vehicle(string definition, bool crewExposed = false, bool motion = false) =>
        new("de-v", definition, "german", crewExposed, motion, false, false, false, false, false, false, false);

    private static VehicleCloseCombatFacts Against(VehicleCloseCombatVehicle vehicle, CloseCombatUnit[] units, string[] attackers, int[] dice, bool reaction = false) =>
        new(reaction ? "MPh" : "CCPh", At, vehicle, units, attackers, [], false, reaction, new VehicleCloseCombatRolls(dice)
        {
            UnlikelyKill = 4,
            WoundSeverity = new Dictionary<string, int>(StringComparer.Ordinal) { ["ru-l"] = 3 },
        });

    private static readonly CloseCombatUnit[] Russians =
    [
        Unit("ru-s", "defender-squad"), Unit("ru-l", "defender-leader-8-1"), Unit("ru-h", "defender-half-squad"), Unit("ru-c", "defender-crew"),
        Unit("ru-g", "defender-conscript-squad", inexperienced: true), Unit("ru-p", "defender-squad", pinned: true),
    ];

    [Fact]
    public void CurrentVehicleDefenseRequiresKnownExperienceBeforeAnyRoll()
    {
        var defender = Unit("ru-g", "defender-conscript-squad") with
        {
            Inexperienced = null,
        };
        var facts = new VehicleCloseCombatFacts("CCPh", At, Vehicle("attacker-tank"), [defender], [], ["ru-g"], true, false,
            new VehicleCloseCombatRolls([3, 4]));
        Assert.Empty(ScenarioA1VehicleCloseCombat.Precheck(facts, CloseCombat));
        Assert.Equal(CloseCombatResolution.Resolved, ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat).Disposition);
        Assert.Equal(["asl.a1.cc-vehicle.fact-missing:inexperienced"], ScenarioA1VehicleCloseCombat.Precheck(facts, CloseCombat, new CcvContext()));
        Assert.Equal(CloseCombatResolution.Abstained, ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext()).Disposition);
        var known = defender with
        {
            Inexperienced = true,
        };
        Assert.Empty(ScenarioA1VehicleCloseCombat.Precheck(facts with
        {
            Units = [known],
        }, CloseCombat, new CcvContext()));
    }

    private static string Json(VehicleCloseCombatResolution resolution) => JsonSerializer.Serialize(resolution);

    [Fact]
    public void AContextResolverRefusesAVehicleAsAnInfantryAttacker()
    {
        var facts = Against(Vehicle("attacker-tank"), [Unit("ru-vehicle", "attacker-truck")], ["ru-vehicle"], [3, 4]);
        Assert.Empty(ScenarioA1VehicleCloseCombat.Precheck(facts, CloseCombat));
        var result = ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext());
        Assert.Equal(CloseCombatResolution.Abstained, result.Disposition);
        Assert.Contains(result.Reasons, reason => reason.Contains("only Infantry or Cavalry", StringComparison.Ordinal));
    }

    [Fact]
    public void TheNewOverloadWithAnEmptyContextGivesTheOldAnswerOverTheExistingCases()
    {
        string[][] attacks = [["ru-s"], ["ru-s", "ru-l"], ["ru-h"], ["ru-c"], ["ru-l"], ["ru-g"], ["ru-g", "ru-l"], ["ru-p"], ["ru-p", "ru-l"]];
        VehicleCloseCombatVehicle[] vehicles = [Vehicle("attacker-tank"), Vehicle("attacker-tank", motion: true), Vehicle("attacker-halftrack", crewExposed: true),
            Vehicle("attacker-truck")];
        var compared = 0;
        foreach (var vehicle in vehicles)
        {
            foreach (var attackers in attacks)
            {
                for (var first = 1; first <= 6; first++)
                {
                    for (var second = 1; second <= 6; second++)
                    {
                        var facts = Against(vehicle, Russians, attackers, [first, second]);
                        Assert.Equal(Json(ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat)), Json(ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext())));
                        var reaction = facts with
                        {
                            Phase = "MPh",
                            Reaction = true,
                            FireMarked = [attackers[0]]
                        };
                        Assert.Equal(Json(ScenarioA1VehicleCloseCombat.Resolve(reaction, CloseCombat)), Json(ScenarioA1VehicleCloseCombat.Resolve(reaction, CloseCombat, new CcvContext())));
                        compared++;
                    }
                }
            }
        }

        // The vehicle's attack on Infantry (A11.62).
        string[][] defenses = [["ru-s"], ["ru-s", "ru-l"], ["ru-l"], ["ru-h", "ru-p"], ["ru-g"]];
        foreach (var defenders in defenses)
        {
            var facts = new VehicleCloseCombatFacts("CCPh", At, Vehicle("attacker-tank"), Russians, [], defenders, true, false, new VehicleCloseCombatRolls([3, 4])
            {
                RandomSelection = Russians.ToDictionary(unit => unit.UnitId!, _ => 3, StringComparer.Ordinal),
                WoundSeverity = new Dictionary<string, int>(StringComparer.Ordinal) { ["ru-l"] = 3 },
            });
            Assert.Equal(Json(ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat)), Json(ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext())));
        }

        Assert.Equal(vehicles.Length * attacks.Length * 36, compared);
    }

    [Fact]
    public void AContextAddsTheAssaultEngineerTheConcealedVehicleAndTheOutnumberedGuardToTheKillNumber()
    {
        var facts = Against(Vehicle("attacker-tank"), Russians, ["ru-s", "ru-l"], [3, 4]);
        Assert.Equal(6, ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat).KillNumber);
        var engineer = ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext(AssaultEngineers: ["ru-s"]));
        Assert.Equal(7, engineer.KillNumber);
        Assert.Equal(["smc-combining:ru-l", "assault-engineer:ru-s"], engineer.KillNumberModifiers.Select(item => item.Name));
        Assert.Equal(5, ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext(VehicleConcealed: true)).KillNumber);
        Assert.Equal(5, ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext(OutnumberedGuards: ["ru-s"])).KillNumber);
        Assert.Equal(5, ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext(National: new Dictionary<string, NationalCcv>(StringComparer.Ordinal)
        {
            ["ru-s"] = NationalCcv.Ethiopian
        })).KillNumber);
    }

    [Fact]
    public void AGuardAttackingAVehicleLosesOneCcvWithoutBeingOutnumbered()
    {
        // A20.52 (p. 87): a squad guarding one HS still halves its attack against non-prisoners.
        var facts = Against(Vehicle("attacker-tank"), Russians, ["ru-s"], [3, 4]);
        var context = new CcvContext { Guards = ["ru-s"] };
        Assert.False(context.IsOutnumberedGuard("ru-s"));
        var result = ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, context);
        Assert.Equal(4, result.KillNumber);
        Assert.Contains(result.KillNumberModifiers, item => item.Name == "guard" && item.Value == -1m);
        Assert.Equal(5, ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat).KillNumber);
        Assert.DoesNotContain("\"Guards\":", JsonSerializer.Serialize(new CcvContext()), StringComparison.Ordinal);
        var both = ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext(OutnumberedGuards: ["ru-s"]) { Guards = ["ru-s"] });
        Assert.Equal(4, both.KillNumber);
        Assert.Single(both.KillNumberModifiers);
        var combined = Against(Vehicle("attacker-tank"), Russians, ["ru-s", "ru-l"], [3, 4]);
        Assert.Equal(5, ScenarioA1VehicleCloseCombat.Resolve(combined, CloseCombat, new CcvContext { Guards = ["ru-l"] }).KillNumber);
        Assert.Null(JsonSerializer.Deserialize<CcvContext>(JsonSerializer.Serialize(new CcvContext()))!.Guards);
    }

    [Fact]
    public void AContextRefusesAnUnarmedOrSwimmingAttackerThatTheOldOverloadAdmits()
    {
        var units = Russians.Append(Unit("ru-u", "defender-squad", unarmed: true)).ToArray();
        var facts = Against(Vehicle("attacker-tank"), units, ["ru-u"], [3, 4]);
        Assert.Equal(CloseCombatResolution.Resolved, ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat).Disposition);
        var refused = ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext());
        Assert.Equal(CloseCombatResolution.Abstained, refused.Disposition);
        Assert.Contains("(A20.5)", Assert.Single(refused.Reasons), StringComparison.Ordinal);
        var swimmer = ScenarioA1VehicleCloseCombat.Resolve(Against(Vehicle("attacker-tank"), Russians, ["ru-s"], [3, 4]), CloseCombat, new CcvContext(Swimming: ["ru-s"]));
        Assert.Contains("(E6.2)", Assert.Single(swimmer.Reasons), StringComparison.Ordinal);
    }

    [Fact]
    public void AContextDropsACombiningLeadersDrmForABerserkAttacker()
    {
        var units = Russians.Append(Unit("ru-b", "defender-squad", berserk: true)).ToArray();
        var facts = Against(Vehicle("attacker-tank"), units, ["ru-b", "ru-l"], [3, 4]);
        Assert.Contains(ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat).Drm, item => item.Name == "leadership:ru-l");
        var now = ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext());
        Assert.DoesNotContain(now.Drm, item => item.Name.StartsWith("leadership:", StringComparison.Ordinal));
        Assert.Equal(6, now.KillNumber);
        Assert.Contains(ScenarioA1VehicleCloseCombat.Resolve(Against(Vehicle("attacker-tank"), units, ["ru-s", "ru-l"], [3, 4]), CloseCombat, new CcvContext()).Drm,
            item => item.Name == "leadership:ru-l");
    }

    [Fact]
    public void AContextImmobilizesOnAFinalDrEqualToACcvOfZero()
    {
        // A pinned 8-1 alone (2 - 1) against a concealed truck (-1): CCV 0; an Original 4 with -3 unarmored and -1 no manned MG is 0: immobilized.
        var units = new[] { Unit("ru-l", "defender-leader-8-1", pinned: true) };
        var result = ScenarioA1VehicleCloseCombat.Resolve(Against(Vehicle("attacker-truck"), units, ["ru-l"], [2, 2]), CloseCombat, new CcvContext(VehicleConcealed: true));
        Assert.Equal((0, 0, VehicleCloseCombatResolution.Immobilized), (result.KillNumber, result.FinalDr, result.VehicleResult));
    }

    [Fact]
    public void AContextGivesTheDefendersAssaultEngineerAndTheExcessBelowOneAsADrm()
    {
        var units = Russians.Append(Unit("ru-e", "defender-conscript-half-squad", pinned: true, inexperienced: true)).ToArray();
        var facts = new VehicleCloseCombatFacts("CCPh", At, Vehicle("attacker-tank"), units, [], ["ru-e"], true, false, new VehicleCloseCombatRolls([3, 4]));
        var old = ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat);
        Assert.Equal(1m, old.DefenseValue);
        Assert.Empty(Assert.Single(old.Defending!).Drm);
        var ethiopian = ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat, new CcvContext(National: new Dictionary<string, NationalCcv>(StringComparer.Ordinal)
        {
            ["ru-e"] = NationalCcv.Ethiopian
        }));
        Assert.Equal(1m, ethiopian.DefenseValue);
        var defender = Assert.Single(ethiopian.Defending!);
        Assert.Equal([("ccv-below-one:ru-e", -1m)], defender.Drm.Select(item => (item.Name, item.Value)));
        Assert.Equal(6, defender.FinalDr);

        var squad = facts with
        {
            Defenders = ["ru-s"]
        };
        Assert.Equal(6m, ScenarioA1VehicleCloseCombat.Resolve(squad, CloseCombat, new CcvContext(AssaultEngineers: ["ru-s"])).DefenseValue);
        Assert.Equal(5m, ScenarioA1VehicleCloseCombat.Resolve(squad, CloseCombat).DefenseValue);
    }

    // The contract a11.5-ccv-vs-vehicle.

    private static JsonObject Facts(int finalDr, bool engineer = false, bool smc = false, bool pinned = false, bool marked = false, string kind = "squad",
        bool unarmed = false, bool swimming = false, bool berserk = false, bool owner = true)
    {
        var facts = new JsonObject
        {
            ["attackerId"] = "g-1",
            ["attackerKind"] = kind,
            ["inexperienced"] = false,
            ["smcCombining"] = smc,
            ["pinned"] = pinned,
            ["reactionFireMarked"] = marked,
            ["vehicleConcealed"] = false,
            ["guard"] = false,
            ["csRed"] = false,
            ["finalDr"] = finalDr,
            ["unarmed"] = unarmed,
            ["berserk"] = berserk,
            ["swimming"] = swimming,
        };
        if (owner)
        {
            facts["assaultEngineer"] = engineer;
            facts["nationalCcv"] = "none";
        }

        return facts;
    }

    private static ContractVerdict Evaluate(JsonObject facts) => ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find("a11.5-ccv-vs-vehicle")!, facts);

    [Fact]
    public void TheContractGivesEachResultOfTheFinalDr()
    {
        // The p. 74 example: the 4-6-7 with the 9-2, CCV 6, Final DR 4: eliminated, and the PRC with it.
        var eliminated = Evaluate(Facts(4, smc: true));
        Assert.Equal("eliminated", eliminated.Outcome);
        Assert.StartsWith("CCV 6: base 5, smc-combining:smc +1", eliminated.Reasons[0].Text, StringComparison.Ordinal);
        Assert.Contains(eliminated.Reasons, reason => reason.Text.Contains("PRC", StringComparison.Ordinal));
        Assert.Equal("immobilized", Evaluate(Facts(6, engineer: true)).Outcome);
        Assert.Equal("no-effect", Evaluate(Facts(6)).Outcome);
        Assert.Equal("burning-wreck", Evaluate(Facts(3, engineer: true, smc: true)).Outcome);
        Assert.Equal("immobilized", Evaluate(Facts(4, pinned: true)).Outcome);
        Assert.Equal("no-effect", Evaluate(Facts(4, pinned: true, marked: true)).Outcome);
    }

    [Fact]
    public void TheGuardContractAcceptsBothNewAndLegacyInputs()
    {
        var current = Facts(4);
        current["guard"] = true;
        Assert.Equal("immobilized", Evaluate(current).Outcome);
        Assert.Contains("guard -1", Evaluate(current).Reasons[0].Text, StringComparison.Ordinal);
        var legacy = Facts(4);
        legacy.Remove("guard");
        legacy["guardOutnumbered"] = true;
        Assert.Equal("immobilized", Evaluate(legacy).Outcome);
        Assert.Contains("guard-outnumbered -1", Evaluate(legacy).Reasons[0].Text, StringComparison.Ordinal);
        legacy.Remove("guardOutnumbered");
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(legacy).Outcome);
        // A20.52 (p. 87): "not outnumbered" does not show the attacker is no Guard, so an unknown guard fact is not evaluated (item 15).
        legacy["guardOutnumbered"] = false;
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(legacy).Outcome);
        legacy["guard"] = false;
        Assert.Equal("eliminated", Evaluate(legacy).Outcome);
    }

    [Fact]
    public void TheContractRefusesUnarmedAndSwimmingAttackersEvenForTheOpponent()
    {
        var unarmed = Evaluate(Facts(4, unarmed: true, owner: false));
        Assert.Equal(("refused", "A20.5"), (unarmed.Outcome, unarmed.Reasons[0].Rule));
        Assert.Equal(("refused", "E6.2"), (Evaluate(Facts(4, swimming: true)).Outcome, Evaluate(Facts(4, swimming: true)).Reasons[0].Rule));
    }

    [Fact]
    public void TheVehiclesSideDoesNotKnowTheAssaultEngineerAndANonWholeDrIsNotEvaluated()
    {
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(Facts(4, owner: false)).Outcome);
        var half = Facts(4);
        half["finalDr"] = 4.5m;
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(half).Outcome);
        var contract = ScenarioA1RuleContracts.Find("a11.5-ccv-vs-vehicle")!;
        Assert.All(contract.Facts.Where(fact => fact.Disclosure == ContractDisclosure.Owner), fact => Assert.Equal("attackerId", fact.Concerns));
        Assert.Equal(["assaultEngineer", "nationalCcv"], contract.Facts.Where(fact => fact.Disclosure != ContractDisclosure.All).Select(fact => fact.Name));
    }

    [Fact]
    public void TheContractSaysABerserkAttackerTakesNoLeadersDrm()
    {
        Assert.Contains(Evaluate(Facts(4, smc: true, berserk: true)).Reasons, reason => reason.Rule == "A11.51");
        Assert.DoesNotContain(Evaluate(Facts(4, smc: true)).Reasons, reason => reason.Rule == "A11.51");
    }
}
