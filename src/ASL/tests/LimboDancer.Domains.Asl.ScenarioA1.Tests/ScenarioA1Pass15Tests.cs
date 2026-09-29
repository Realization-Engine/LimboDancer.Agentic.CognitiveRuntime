using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The packages as revised for backlog pass 15 (rulings R15.1 to R15.13): FT, DC, MOL, Commissars, Allied Troops, underscored Morale Factors, a hero's
/// MG, NKVD Field Promotion, and the Heat of Battle, Replacement, and Battle Hardening of five more nationalities.
/// </summary>
public sealed class ScenarioA1Pass15Tests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    private static FireFirer Firer(string id, string definition = "defender-squad") => new(id, definition, From, false, false, false, false, false);

    private static FireTarget Target(string id, string definition = "attacker-squad") => new(id, definition, At, false, false, false, false, false, false, false);

    private static FireAttack Attack(int[] dice, FireFirer[]? firers = null, FireTarget[]? targets = null, FireDirector? director = null, int range = 1,
        string terrain = "stone-building", int hindrance = 0) =>
        new("PFPh", "phasing", true, From, At, firers ?? [Firer("ru-1")], director, range, true, new FireLos(false, hindrance, true, false), null,
            terrain, targets ?? [Target("de-s")], 3, new FireRolls(dice, null, null, null));

    private static FireFirer Flamethrower(string id = "ru-1", string definition = "defender-squad") =>
        Firer(id, definition) with { Weapons = [new FireWeapon("ru-ft", "defender-ft", false, false, false)], UsesInherentFp = false, UsesSupportWeapon = true };

    /// <summary>Resolves an attack, supplying each further roll with a fixed value: checks as given (2 and 2 by default), drs of 1.</summary>
    private static FireResolution Complete(FireAttack attack, int[]? checks = null, int molCheck = 1)
    {
        var rolls = attack.Rolls!;
        for (var step = 0; step < 50; step++)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack with
            {
                Rolls = rolls
            }, Reference);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal))
            {
                return result;
            }

            var key = reason["asl.a1.fire.roll-missing:".Length..];
            if (key == "molCheck")
            {
                rolls = rolls with { MolCheck = molCheck };
                continue;
            }

            var split = key.IndexOf(':', StringComparison.Ordinal);
            var (kind, ids) = (key[..split], key[(split + 1)..].Split(','));
            Dictionary<string, T> Add<T>(IReadOnlyDictionary<string, T>? existing, T value)
            {
                var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
                foreach (var id in ids)
                {
                    next[id] = value;
                }

                return next;
            }

            IReadOnlyList<int> dice = checks ?? [2, 2];
            rolls = kind switch
            {
                "randomSelection" => rolls with { RandomSelection = Add(rolls.RandomSelection, 1) },
                "checks" => rolls with { Checks = Add(rolls.Checks, dice) },
                "leaderLoss" => rolls with { LeaderLoss = Add(rolls.LeaderLoss, dice) },
                "woundSeverity" => rolls with { WoundSeverity = Add(rolls.WoundSeverity, 1) },
                "weaponSelection" => rolls with { WeaponSelection = Add(rolls.WeaponSelection, 1) },
                "heatOfBattle" => rolls with { HeatOfBattle = Add(rolls.HeatOfBattle, dice) },
                "berserkCheck" => rolls with { BerserkChecks = Add(rolls.BerserkChecks, dice) },
                _ => throw new InvalidOperationException(key),
            };
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    [Fact]
    public void AFlamethrowerIgnoresTemAndIsHalvedAtLongRangeAndTwoLevelsAway()
    {
        // A22.1, A22.2 (ruling R15.1): 24 FP in a stone building with no TEM; 12 at range 2; 12 at a target two levels up.
        var adjacent = Complete(Attack([5, 4], firers: [Flamethrower()]));
        Assert.Equal(24m, adjacent.Arithmetic!.TotalFirepower);
        Assert.DoesNotContain(adjacent.Arithmetic.Drm, item => item.Name.StartsWith("tem:", StringComparison.Ordinal));
        Assert.Equal(12m, Complete(Attack([5, 4], firers: [Flamethrower()], range: 2)).Arithmetic!.TotalFirepower);
        Assert.Equal(12m, Complete(Attack([5, 4], firers: [Flamethrower()]) with { SameLevel = false, TargetLevelAbove = 2 }).Arithmetic!.TotalFirepower);

        // A22.32: never at Long Range through a Hindrance, nor three levels away, nor with a leader or another firer (A22.31).
        Assert.Contains("asl.a1.fire.flamethrower-outside", ScenarioA1FireCalculator.Resolve(Attack([5, 4], firers: [Flamethrower()], range: 2, hindrance: 1), Reference).Reasons);
        Assert.Contains("asl.a1.fire.flamethrower-outside", ScenarioA1FireCalculator.Resolve(Attack([5, 4], firers: [Flamethrower(), Firer("ru-2")]), Reference).Reasons);
    }

    [Fact]
    public void AnEliteFlamethrowerKeepsItsFuelThroughAnOriginalNine()
    {
        // A22.3, A22.5 (ruling R15.1): an elite user's removal number is 10; a 1st Line user's is 8.
        Assert.Null(Complete(Attack([5, 4], firers: [Flamethrower(definition: "defender-elite-squad")])).FlamethrowerRemoved);
        Assert.Equal("ru-ft", Complete(Attack([5, 4], firers: [Flamethrower()])).FlamethrowerRemoved);
    }

    [Fact]
    public void AFinnishFirstLineUserIsEliteForItsFlamethrowerAndAnInexperiencedUserLosesItOneSooner()
    {
        // A25.74, A19.32 (referee, pass 15): with a captured FT (two lower), a Finnish 1st Line user removes it on 8 like an elite one, a Finnish 2nd Line
        // user on 6, and a Conscript user, Inexperienced, on 5; an Inexperienced elite user removes its own FT on 9.
        FireFirer Captured(string definition) => Flamethrower(definition: definition) with
        {
            Weapons = [new FireWeapon("ru-ft", "defender-ft", false, false, false) { Captured = true }],
        };
        FireResolution Finnish(int[] dice, string definition) => Complete(Attack(dice, firers: [Captured(definition)]) with { FiringNationalities = ["finnish"] });
        Assert.Null(Finnish([4, 3], "finnish-squad").FlamethrowerRemoved);
        Assert.Equal("ru-ft", Finnish([4, 4], "finnish-squad").FlamethrowerRemoved);
        Assert.Equal("ru-ft", Finnish([4, 2], "finnish-2nd-line-squad").FlamethrowerRemoved);
        Assert.Null(Finnish([2, 2], "finnish-conscript-squad").FlamethrowerRemoved);
        Assert.Equal("ru-ft", Finnish([3, 2], "finnish-conscript-squad").FlamethrowerRemoved);
        Assert.Equal("ru-ft", Complete(Attack([5, 4], firers: [Flamethrower(definition: "defender-elite-squad") with { Inexperienced = true }])).FlamethrowerRemoved);
        Assert.Null(Complete(Attack([4, 4], firers: [Flamethrower(definition: "defender-elite-squad") with { Inexperienced = true }])).FlamethrowerRemoved);
    }

    [Fact]
    public void BritishEliteAndFirstLineUnitsAndFinnsButConscriptsNeverCower()
    {
        // A25.45, A25.7 (referee, pass 15): doubles Cower a Russian squad and a Finnish Conscript squad, never a British elite or 1st Line squad or a
        // Finnish 1st Line squad.
        Assert.True(Complete(Attack([3, 3])).Arithmetic!.Cowered);
        Assert.True(Complete(Attack([3, 3], firers: [Firer("fi-c", "finnish-conscript-squad")])).Arithmetic!.Cowered);
        foreach (var definition in new[] { "british-elite-squad", "british-squad", "finnish-squad" })
        {
            Assert.False(Complete(Attack([3, 3], firers: [Firer("uk-1", definition)])).Arithmetic!.Cowered);
        }
    }

    [Fact]
    public void AThrownDcsSecondDrAtItsThrowerNeverMalfunctionsAndMayBeDefensiveFirstFire()
    {
        // A23.4 EXC, A23.6 (referee and table player, pass 15): an Original 12 at the thrower's Location attacks; so does one Thrown as Defensive First Fire.
        var charge = new FireDemolitionCharge("ru-dc", "defender-dc", FireDemolitionCharge.Thrower, "ru-1", "defender-squad", false, false, null, null);
        var back = Attack([6, 6]) with
        {
            Firers = null, FireGroupComplete = null, Los = null, Range = 0, TargetLocationId = From,
            Targets = [Target("ru-1", "defender-squad") with { LocationId = From, Friendly = true }],
            FiringSideElr = 2,
            DemolitionCharge = charge,
        };
        var twelve = Complete(back);
        Assert.NotEqual(true, twelve.DemolitionChargeMalfunctioned);
        Assert.Contains(twelve.Arithmetic!.Drm, item => item.Name == "thrower-location");
        var defensive = Complete(back with { Phase = "MPh", FiringSide = "non-phasing", Rolls = new FireRolls([3, 3], null, null, null) });
        Assert.DoesNotContain(defensive.Reasons ?? [], reason => reason.StartsWith("asl.a1.fire.phase-outside", StringComparison.Ordinal));
        Assert.NotNull(defensive.Arithmetic);
    }

    [Fact]
    public void AThrownDcIsPlusTwoAndItsThrowerPlusThreeAndNeverCowers()
    {
        // A23.1, A23.6, A7.9 (rulings R15.2, R15.3): doubles never Cower a DC; the target's TEM applies.
        var charge = new FireDemolitionCharge("ru-dc", "defender-dc", FireDemolitionCharge.Thrown, "ru-1", "defender-squad", false, false, null, null);
        var thrown = Complete(Attack([3, 3]) with { Firers = null, FireGroupComplete = null, Los = null, DemolitionCharge = charge });
        Assert.Equal(30m, thrown.Arithmetic!.TotalFirepower);
        Assert.False(thrown.Arithmetic.Cowered);
        Assert.Contains(thrown.Arithmetic.Drm, item => item.Name == "thrown-dc" && item.Value == 2m);
        Assert.Contains(thrown.Arithmetic.Drm, item => item.Name == "tem:stone-building");
        var back = Complete(Attack([3, 3]) with
        {
            Firers = null, FireGroupComplete = null, Los = null, Range = 0, TargetLocationId = From,
            Targets = [Target("ru-1", "defender-squad") with { LocationId = From, Friendly = true }],
            FiringSideElr = 2,
            DemolitionCharge = charge with { Mode = FireDemolitionCharge.Thrower },
        });
        Assert.Contains(back.Arithmetic!.Drm, item => item.Name == "thrower-location" && item.Value == 3m);
    }

    [Fact]
    public void APlacedDcMalfunctionsOnTenForANonEliteOwnerAndIsHalvedForTargetsConcealedAtPlacement()
    {
        // A23.2, A23.4 (ruling R15.2): an Original 10 removes a 1st Line owner's DC with no effect; a DC Placed at concealed units is Area Fire.
        var charge = new FireDemolitionCharge("ru-dc", "defender-dc", FireDemolitionCharge.Placed, "ru-1", "defender-squad", false, false, false, null);
        var placed = Attack([5, 5]) with { Phase = "AFPh", Firers = null, FireGroupComplete = null, Los = null, DemolitionCharge = charge };
        var dud = Complete(placed);
        Assert.True(dud.DemolitionChargeMalfunctioned);
        Assert.Equal("none", dud.Arithmetic!.Result);
        var halved = Complete(placed with { Rolls = new FireRolls([2, 3], null, null, null), DemolitionCharge = charge with { ConcealedWhenPlaced = true } });
        Assert.Equal(15m, halved.Arithmetic!.TotalFirepower);
        Assert.DoesNotContain(halved.Arithmetic.Drm, item => item.Name == "advancing-fire");
    }

    [Fact]
    public void AMolUserBrokenByAColoredSixAddsNothing()
    {
        // A22.6111 (ruling R15.4): a passed Check with a colored 6 voids the squad's FP and the MOL, and breaks it; a colored 5 adds four FP.
        var broken = Complete(Attack([6, 1], firers: [Firer("ru-1") with { Mol = true }, Firer("ru-2")]));
        Assert.True(broken.MolCheck!.UserBroken);
        Assert.Equal(8m, broken.Arithmetic!.TotalFirepower);
        var passed = Complete(Attack([5, 1], firers: [Firer("ru-1") with { Mol = true }, Firer("ru-2")]));
        Assert.Equal(20m, passed.Arithmetic!.TotalFirepower);

        // A22.611: a HS that fails its Check adds no FP: dr 3, +1 HS, +1 non-AFV, is 5.
        var failed = Complete(Attack([5, 1], firers: [Firer("ru-h", "defender-half-squad") with { Mol = true }, Firer("ru-2")]), molCheck: 3);
        Assert.False(failed.MolCheck!.Passed);
        Assert.Equal(8m, failed.Arithmetic!.TotalFirepower);
    }

    [Fact]
    public void ACommissarRaisesHisLocationsMoraleAndTakesNoLeaderLossCheck()
    {
        // A25.221 (ruling R15.6): a Russian squad with a 9-0 Commissar checks on 8, and the Commissar checks first.
        var targets = new[] { Target("ru-s", "defender-squad"), Target("ru-c", "defender-commissar-9-0") };
        var attack = Attack([4, 3], firers: [Firer("de-1", "attacker-squad"), Firer("de-2", "attacker-squad")], targets: targets) with { TargetSideElr = 2 };
        var result = Complete(attack, checks: [4, 4]);
        var squad = result.Effects.Single(item => item.UnitId == "ru-s");
        Assert.All(squad.Checks, check => Assert.Equal(8, check.MoraleLevel));
        Assert.All(result.Effects.Single(item => item.UnitId == "ru-c").Checks, check => Assert.Equal(9, check.MoraleLevel));
    }

    [Fact]
    public void AnAlliedLeaderDirectsOneWorse()
    {
        // A10.7 (ruling R15.8): a German 9-2 directing an Italian squad is -1.
        var attack = Attack([4, 3], firers: [Firer("de-1", "attacker-squad"), Firer("it-1", "italian-squad")],
            director: new FireDirector("de-l", "attacker-leader-9-2", From, false, false, false, false, false), targets: [Target("ru-s", "defender-squad")])
            with { FiringNationalities = ["german", "italian"] };
        Assert.Contains(Complete(attack).Arithmetic!.Drm, item => item.Name == "leadership:de-l" && item.Value == -1m);
    }

    [Fact]
    public void AnUnderscoredHalfSquadIsDisruptedRatherThanReplaced()
    {
        // A1.23, A19.13 (ruling R15.9; referee, pass 15): an underscored HS has an ELR of 5 whatever its side's. A 2MC on 11 fails by five and only breaks it;
        // Encircled (morale 7), it fails by six and is Disrupted.
        FireAttack Attack2Mc(bool encircled) => Attack([1, 2], firers: [Firer("de-1", "attacker-squad"), Firer("de-2", "attacker-squad")],
            targets: [Target("ru-h", "defender-nkvd-half-squad") with { Encircled = encircled ? true : null }]) with { TargetSideElr = 1 };
        var byFive = Complete(Attack2Mc(false), checks: [6, 5]).Effects.Single();
        Assert.True(byFive.Broken);
        Assert.False(byFive.Disrupted);
        var effect = Complete(Attack2Mc(true), checks: [6, 5]).Effects.Single();
        Assert.True(effect.Disrupted);
        Assert.Equal("defender-nkvd-half-squad", effect.FinalDefinitionId);
    }

    [Fact]
    public void ItaliansSurrenderOnTenAndTheJapaneseStayRefused()
    {
        // A15.1 (ruling R15.13): a non-elite Italian MMC surrenders on 10; an elite one is berserk there; the Japanese are refused.
        var squad = Reference.Definitions["italian-squad"];
        Assert.Equal(HeatOfBattleOutcome.Surrender, ScenarioA1HeatOfBattle.Resolve(squad, false, null, false, [5, 2], Reference.Definitions, true, []).Outcome!.Result);
        Assert.Equal(HeatOfBattleOutcome.Berserk,
            ScenarioA1HeatOfBattle.Resolve(Reference.Definitions["italian-elite-squad"], false, null, false, [6, 2], Reference.Definitions, true, []).Outcome!.Result);
        var japanese = Reference.Definitions["attacker-squad"] with { Nationality = "japanese" };
        Assert.Equal("asl.a1.hob.nationality-unreviewed:attacker-squad", ScenarioA1HeatOfBattle.Resolve(japanese, false, null, false, [3, 3], Reference.Definitions, true, []).Undecided);
    }

    [Fact]
    public void TheNewNationalitiesReplaceAndBattleHardenByTheirOwnClasses()
    {
        // A19.13, A15.3 (ruling R15.13): the British 1st Line falls to 2nd Line and hardens to elite; a Finnish 1st Line squad becomes Fanatic.
        Assert.Equal("british-2nd-line-squad", ScenarioA1FireReference.ReplacementOf("british-squad"));
        Assert.Equal("british-elite-squad", ScenarioA1FireReference.HardenedOf("british-squad"));
        Assert.Null(ScenarioA1FireReference.HardenedOf("finnish-squad"));
        Assert.True(ScenarioA1FireReference.IsHighestQuality("finnish-squad"));
        Assert.Equal("american-leader-8-1", ScenarioA1FireReference.HardenedOf("american-leader-8-0"));
        Assert.Null(ScenarioA1FireReference.ReplacementOf("defender-commissar-9-0"));
        var hardened = ScenarioA1HeatOfBattle.Resolve(Reference.Definitions["finnish-squad"], false, null, false, [3, 4], Reference.Definitions, true, []).Outcome!;
        Assert.True(hardened.Fanatic);
    }

    [Fact]
    public void AnNkvdFieldPromotionUsesTheCommissarTable()
    {
        // A25.25 (ruling R15.7): a dr of 1, +1 broken, is 2: a 9-0 Commissar; 6 or more creates none.
        var nkvd = Reference.Definitions["defender-nkvd-squad"];
        var broken = new[] { new FireModifier("broken", 1m, "A18.11") };
        Assert.Equal("defender-commissar-9-0", ScenarioA1FieldPromotion.Create(nkvd, 9, 1, broken, Reference.Definitions, "asl.a1.rally").Outcome!.LeaderDefinitionId);
        Assert.Equal("defender-commissar-10-0", ScenarioA1FieldPromotion.Create(nkvd, 9, 1, [], Reference.Definitions, "asl.a1.cc").Outcome!.LeaderDefinitionId);
        Assert.Null(ScenarioA1FieldPromotion.Create(nkvd, 9, 5, broken, Reference.Definitions, "asl.a1.rally").Outcome!.LeaderDefinitionId);
    }

    [Fact]
    public void AHeroFiresAMgWithItsTwoManDrmAndHisHeroicDrm()
    {
        // A15.23, A15.24 (ruling R15.11): full FP, +1 and -1, and no inherent FP of his own.
        var hero = Firer("ru-h", "defender-hero") with { Weapons = [new FireWeapon("ru-lmg", "defender-lmg", false, false, false)], UsesSupportWeapon = true };
        var result = Complete(Attack([4, 3], firers: [hero]));
        Assert.Contains(result.Arithmetic!.Drm, item => item.Name == "hero-mg:ru-h");
        Assert.Contains(result.Arithmetic.Drm, item => item.Name == "heroic:ru-h");
        Assert.DoesNotContain(result.Arithmetic.Firers, item => item.UnitId == "ru-h");
    }
}
