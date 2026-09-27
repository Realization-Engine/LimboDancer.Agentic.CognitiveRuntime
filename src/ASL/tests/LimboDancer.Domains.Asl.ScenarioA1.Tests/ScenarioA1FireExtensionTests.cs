using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The Fire package as revised for unit steps 19 to 23 and 27 and 28: Advancing Fire, fire groups across Locations, hidden
/// and Dummy targets, Defensive First Fire with FFNAM and FFMO, Subsequent First Fire, FPF and its NMC, Residual FP, Final
/// Fire by First-Fire-marked units, MGs, Heat of Battle, heroes, Battle Hardening, and Fanatic units.
/// </summary>
public sealed class ScenarioA1FireExtensionTests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    private static FireFirer Firer(string id, string definition = "defender-squad", string location = From) =>
        new(id, definition, location, false, false, false, false, false);

    private static FireTarget Target(string id, string definition = "attacker-squad") => new(id, definition, At, false, false, false, false, false, false, false);

    private static readonly FireDirector Leader = new("ru-l", "defender-leader", From, false, false, false, false, false);

    private static FireAttack Attack(FireRolls rolls, string phase = "PFPh", string side = "phasing", string terrain = "open-ground",
        FireDirector? director = null, int range = 1, FireFirer[]? firers = null, FireTarget[]? targets = null) =>
        new(phase, side, true, From, At, firers ?? [Firer("ru-1"), Firer("ru-2")], director, range, true, new FireLos(false, 0, true, false), null,
            terrain, targets ?? [Target("de-s")], 3, rolls);

    private static FireAttack Moving(FireAttack attack, string kind, bool assault = false) => attack with
    {
        Phase = "MPh",
        FiringSide = "non-phasing",
        FireKind = kind,
        TargetMovement = new FireMovement(assault),
    };

    private static FireRolls Rolls(int[] attack, Dictionary<string, IReadOnlyList<int>>? checks = null) => new(attack, null, checks, null);

    private static IEnumerable<(string Name, decimal Value)> Drm(FireResolution result) => result.Arithmetic!.Drm.Select(item => (item.Name, item.Value));

    /// <summary>
    /// Resolves an attack, supplying each further roll the package asks for with a fixed value (checks 2 and 2, drs of 1),
    /// or stops at the first request <paramref name="stop"/> matches.
    /// </summary>
    private static FireResolution Complete(FireAttack attack, Func<string, bool>? stop = null, int[]? checks = null)
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
            if (stop?.Invoke(key) == true)
            {
                return result;
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
                "firerSelection" => rolls with { FirerSelection = Add(rolls.FirerSelection, 1) },
                "heatOfBattle" => rolls with { HeatOfBattle = Add(rolls.HeatOfBattle, dice) },
                _ => throw new InvalidOperationException(key),
            };
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    [Fact]
    public void AdvancingFireIsHalvedAndMarkedPrepFire()
    {
        // Two 4-4-7 at PBF in the AFPh: 4 x 2 / 2 = 4 each, the 8 column (A7.24).
        var result = Complete(Attack(Rolls([6, 5]), phase: "AFPh"));
        Assert.Equal((8m, "prep-fire"), (result.Arithmetic!.TotalFirepower, result.FireCounter));
        Assert.Contains(result.Arithmetic.Firers[0].Multipliers, item => item.Name == "advancing-fire" && item.Rule == "A7.24");

        // A unit that already fired this Player Turn cannot fire again (A7.1).
        var fired = Complete(Attack(Rolls([6, 5]), phase: "AFPh", firers: [Firer("ru-1") with { FiredThisPlayerTurn = true }]));
        Assert.Contains("asl.a1.fire.firer-already-fired", fired.Reasons);
    }

    [Fact]
    public void AGroupAcrossLocationsUsesEachFirersRangeAndTheWorstHindrance()
    {
        var firers = new[]
        {
            Firer("ru-1") with { Range = 1, SameLevel = true, Los = new FireLos(false, 0, true, false) },
            Firer("ru-2", location: "bd01:F6:0") with { Range = 2, SameLevel = true, Los = new FireLos(false, 1, true, false) },
        };
        var attack = Attack(Rolls([6, 5]), firers: firers) with
        {
            FirerLocationsAdjacent = true
        };
        var result = Complete(attack);
        Assert.Equal(12m, result.Arithmetic!.TotalFirepower);
        Assert.Contains(("los-hindrance", 1m), Drm(result));

        // A7.5: the Locations must be ADJACENT; A7.531: direction needs a leader in every Location.
        Assert.Contains("asl.a1.fire.firer-outside",
            Complete(attack with
            {
                FirerLocationsAdjacent = false
            }).Reasons);
        Assert.Contains("asl.a1.fire.director-outside", Complete(attack with
        {
            Director = Leader
        }).Reasons);
        Assert.Contains("asl.a1.fire.los-blocked", Complete(attack with
        {
            Firers = [firers[0], firers[1] with { Los = new FireLos(true, 0, true, false) }],
        }).Reasons);
    }

    [Fact]
    public void HiddenUnitsAreAttackedAsConcealedAndDummiesAreRemoved()
    {
        var targets = new[]
        {
            Target("de-s") with { Hidden = true },
            new FireTarget("de-dummy", null, At, false, false, true, false, true, false, false),
        };

        // 1+2 = 3 on the 8 column (16 halved): an effect, which removes the Dummy and costs the hidden squad its cover.
        var hit = Complete(Attack(Rolls([1, 2]), targets: targets));
        Assert.Equal(FireResolution.Resolved, hit.Disposition);
        Assert.Equal(8m, hit.Arithmetic!.TotalFirepower);
        Assert.True(hit.Effects.Single(item => item.UnitId == "de-dummy").Eliminated);
        Assert.Contains("dummy-removed", hit.Effects.Single(item => item.UnitId == "de-dummy").Events);
        Assert.True(hit.Effects.Single(item => item.UnitId == "de-s").ConcealmentLost);

        // No effect leaves both as they were.
        var miss = Complete(Attack(Rolls([6, 6]), targets: targets));
        Assert.Equal("none", miss.Arithmetic!.Result);
        Assert.All(miss.Effects, effect => Assert.False(effect.Eliminated || effect.ConcealmentLost));
    }

    [Fact]
    public void DefensiveFirstFireAppliesFfnamAndFfmoAndLeavesResidualFp()
    {
        // 16 FP (PBF) at a squad moving in Open Ground: -1 FFNAM, -1 FFMO; Residual FP is half the 16 column: 8.
        var result = Complete(Moving(Attack(Rolls([6, 5])), ScenarioA1FireCalculator.FirstFire));
        Assert.Equal([("ffnam", -1m), ("ffmo", -1m)], Drm(result));
        Assert.Equal((9, 8, "first-fire"), (result.Arithmetic!.FinalDr, result.Arithmetic.ResidualFp!.Value, result.FireCounter));

        // Assault Movement avoids FFNAM but not FFMO (A4.61); a Hindrance cancels FFMO and lowers Residual FP a counter (A8.26).
        var assault = Complete(Moving(Attack(Rolls([6, 5])), ScenarioA1FireCalculator.FirstFire, assault: true));
        Assert.Equal([("ffmo", -1m)], Drm(assault));
        var hindered = Complete(Moving(Attack(Rolls([6, 5])) with
        {
            Los = new FireLos(false, 1, true, false)
        },
            ScenarioA1FireCalculator.FirstFire));
        Assert.Equal([("los-hindrance", 1m), ("ffnam", -1m)], Drm(hindered));
        Assert.Equal(6, hindered.Arithmetic!.ResidualFp);

        // A pinned mover takes neither (A7.83).
        var pinned = Complete(Moving(Attack(Rolls([6, 5]), targets: [Target("de-s") with { Pinned = true }]),
            ScenarioA1FireCalculator.FirstFire));
        Assert.Empty(Drm(pinned));
    }

    [Fact]
    public void AResidualFpAttackIsAloneAndNeverCowers()
    {
        var attack = Moving(Attack(Rolls([3, 3])) with
        {
            Firers = null,
            Director = null,
            FireGroupComplete = null,
            FirerLocationId = null,
            Range = null,
            SameLevel = null,
            Los = null,
            ResidualFp = 4,
        }, ScenarioA1FireCalculator.ResidualFire);
        var result = Complete(attack);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Equal((4, false, (string?)null), (result.Arithmetic!.ColumnFp!.Value, result.Arithmetic.Cowered, result.FireCounter));
        Assert.Null(result.Arithmetic.ResidualFp);
        Assert.Contains("asl.a1.fire.residual-outside", Complete(attack with
        {
            ResidualFp = 3
        }).Reasons);
    }

    [Fact]
    public void SubsequentFirstFireIsAreaFireWithinRange()
    {
        var firers = new[] { Firer("ru-1") with { FirstFireMarked = true }, Firer("ru-2") with { FirstFireMarked = true } };
        var attack = Moving(Attack(Rolls([6, 5]), firers: firers) with
        {
            WithinSubsequentFirstFireRange = true
        }, ScenarioA1FireCalculator.SubsequentFirstFire);
        var result = Complete(attack);
        Assert.Equal((8m, "final-fire"), (result.Arithmetic!.TotalFirepower, result.FireCounter));
        Assert.Contains("asl.a1.fire.subsequent-first-fire-outside",
            Complete(attack with
            {
                WithinSubsequentFirstFireRange = false
            }).Reasons);
        Assert.Contains("asl.a1.fire.firer-already-fired",
            Complete(attack with
            {
                Firers = [Firer("ru-1")]
            }).Reasons);
    }

    [Fact]
    public void FinalProtectiveFireChecksItsFirersOnTheOriginalDr()
    {
        var firers = new[] { Firer("ru-1") with { FinalFireMarked = true }, Firer("ru-2") with { FinalFireMarked = true } };
        var attack = Moving(Attack(Rolls([4, 4]), firers: firers) with
        {
            FiringSideElr = 2
        },
            ScenarioA1FireCalculator.FinalProtectiveFire);

        // Area Fire and PBF: 8 FP. The Original 8 is a NMC for each 4-4-7: failed, broken within ELR 2.
        var result = Complete(attack);
        Assert.Equal(8m, result.Arithmetic!.TotalFirepower);
        Assert.All(result.FirerEffects!, effect => Assert.True(effect.Broken));

        // An Original 12 is a Casualty MC for one of the two, chosen by Random Selection (A8.31).
        var twelve = Complete(attack with
        {
            Rolls = Rolls([6, 6])
        }, key => key.StartsWith("firerSelection:", StringComparison.Ordinal));
        Assert.Equal(["asl.a1.fire.roll-missing:firerSelection:ru-1,ru-2"], twelve.Reasons);

        // Undirected only, and an ELR for the firing side before any roll.
        Assert.Contains("asl.a1.fire.fpf-outside", Complete(attack with
        {
            Director = Leader
        }).Reasons);
        Assert.Contains("asl.a1.fire.elr-undecided:firing-side-elr-undeclared",
            ScenarioA1FireCalculator.Precheck(attack with
            {
                FiringSideElr = null,
                Rolls = null
            }, Reference));
    }

    [Fact]
    public void AFirstFireMarkedUnitFinalFiresOnlyAdjacentAsAreaFire()
    {
        var firers = new[] { Firer("ru-1") with { FirstFireMarked = true }, Firer("ru-2") };
        var result = Complete(Attack(Rolls([6, 5]), phase: "DFPh", side: "non-phasing", firers: firers));
        Assert.Equal(12m, result.Arithmetic!.TotalFirepower);
        Assert.Contains(result.Arithmetic.Firers[0].Multipliers, item => item.Name == "area-fire" && item.Rule == "A8.4");
        Assert.Contains("asl.a1.fire.final-fire-outside",
            Complete(Attack(Rolls([6, 5]), phase: "DFPh", side: "non-phasing", firers: firers, range: 2)).Reasons);
    }

    [Fact]
    public void MachineGunsAddTheirFirepowerMalfunctionAndKeepTheirRof()
    {
        var mmg = new FireWeapon("ru-mmg", "defender-mmg", false, false, false);
        var squad = Firer("ru-1") with
        {
            Pinned = true,
            Weapons = [mmg]
        };

        // A pinned squad's inherent 4 x 2 is halved (A7.8) but its MMG's 4 x 2 is not: 12 FP. A colored 1 keeps ROF 2.
        var kept = Complete(Attack(Rolls([1, 5]), firers: [squad]));
        Assert.Equal(12m, kept.Arithmetic!.TotalFirepower);
        var weapon = Assert.Single(kept.WeaponEffects!);
        Assert.Equal(("ru-mmg", false, true, (string?)null), (weapon.EquipmentId, weapon.Malfunctioned, weapon.RateOfFireRetained, weapon.FireCounter));

        // An Original 11 reaches its B11: malfunction, and no ROF.
        var broken = Complete(Attack(Rolls([5, 6]), firers: [squad])).WeaponEffects!.Single();
        Assert.Equal((true, false, "prep-fire"), (broken.Malfunctioned, broken.RateOfFireRetained, broken.FireCounter));

        // A HS firing a MG loses its inherent FP (A7.352).
        var half = Complete(Attack(Rolls([6, 5]), firers: [Firer("ru-h", "defender-half-squad") with { Weapons = [mmg] }]));
        Assert.Equal(["ru-mmg"], half.Arithmetic!.Firers.Select(item => item.UnitId));

        // Two MGs whose B# is reached: Random Selection (A9.71).
        var two = new[]
        {
            Firer("ru-1") with { Weapons = [mmg] },
            Firer("ru-2") with { Weapons = [new FireWeapon("ru-lmg", "defender-lmg", false, false, false)] },
        };
        Assert.Equal(["asl.a1.fire.roll-missing:weaponSelection:ru-mmg,ru-lmg"],
            Complete(Attack(Rolls([6, 6]), firers: two), key => key.StartsWith("weaponSelection:", StringComparison.Ordinal)).Reasons);
    }

    /// <summary>A 1MC on a German 1st Line squad in a wooden building (16 FP, 3+4 = 7, +2) whose MC rolls an Original 2.</summary>
    private static FireResolution HeatOfBattle(int[] heat, FireTarget? target = null) =>
        ScenarioA1FireCalculator.Resolve(Attack(new FireRolls([3, 4], null, new Dictionary<string, IReadOnlyList<int>> { ["de-s"] = [1, 1] }, null)
        {
            HeatOfBattle = new Dictionary<string, IReadOnlyList<int>> { ["de-s"] = heat },
        }, terrain: "wooden-building", targets: [target ?? Target("de-s") with { KnownEnemyInLos = true, Captors = [] }]), Reference);

    [Fact]
    public void AnOriginalTwoOnAnMcCallsForHeatOfBattle()
    {
        // A15.1: the Original 2 passes the 1MC, then the package asks for the Heat of Battle DR.
        var asked = Complete(Attack(Rolls([3, 4]), terrain: "wooden-building"), key => key.StartsWith("heatOfBattle", StringComparison.Ordinal), checks: [1, 1]);
        Assert.Equal(["asl.a1.fire.roll-missing:heatOfBattle:de-s"], asked.Reasons);

        // No DRM for a German 1st Line squad in Good Order: 1+1 = 2 creates a German hero (A15.21), the squad unchanged.
        var hero = HeatOfBattle([1, 1]).Effects.Single();
        Assert.Equal((HeatOfBattleOutcome.HeroCreation, "attacker-hero", "attacker-squad"),
            (hero.HeatOfBattle!.Result, hero.HeatOfBattle.HeroDefinitionId, hero.FinalDefinitionId));
        Assert.Contains("hero-created:attacker-hero", hero.Events);

        // 6: a hero and Battle Hardening into the squared-E 4-6-8 (A15.3); 8: Battle Hardening only.
        var both = HeatOfBattle([3, 3]).Effects.Single();
        Assert.Equal((HeatOfBattleOutcome.HeroAndBattleHardening, "attacker-elite-squad"), (both.HeatOfBattle!.Result, both.FinalDefinitionId));
        var hardened = HeatOfBattle([4, 4]).Effects.Single();
        Assert.Equal((null, "attacker-elite-squad"), (hardened.HeatOfBattle!.HeroDefinitionId, hardened.FinalDefinitionId));

        // 9 to 11 is Berserk (A15.4): the squad is berserk and unbroken; 12 Surrender (A15.5): broken and Disrupted, with no
        // ADJACENT captor to surrender to.
        var berserk = HeatOfBattle([5, 5]).Effects.Single();
        Assert.Equal((HeatOfBattleOutcome.Berserk, true, false), (berserk.HeatOfBattle!.Result, berserk.Berserk, berserk.Broken));
        var surrender = HeatOfBattle([6, 6]).Effects.Single();
        Assert.Equal((HeatOfBattleOutcome.Surrender, true, true), (surrender.HeatOfBattle!.Result, surrender.Broken, surrender.Disrupted));

        // An elite squad already of the highest quality becomes Fanatic instead (A15.3, A10.8): 4+4 = 8, -1 elite: 7.
        var fanatic = HeatOfBattle([4, 4], Target("de-s", "attacker-elite-squad")).Effects.Single();
        Assert.Equal((true, "attacker-elite-squad"), (fanatic.Fanatic, fanatic.FinalDefinitionId));
    }

    [Fact]
    public void AConscriptIsAlwaysInexperienced()
    {
        // A15.1: +1 for an Inexperienced unit; a Conscript always is (A19.3), so no fact is needed, and the +1 applies.
        var attack = Attack(Rolls([3, 4]), terrain: "wooden-building",
            targets: [Target("de-c", "attacker-conscript-squad") with { KnownEnemyInLos = true, Captors = [] }]);
        Assert.Empty(ScenarioA1FireCalculator.Precheck(attack, Reference));
        var heat = ScenarioA1HeatOfBattle.Resolve(Reference.Definitions["attacker-conscript-squad"], false, null, false, [3, 3], Reference.Definitions);
        Assert.Contains(("inexperienced", 1m), heat.Outcome!.Drm.Select(item => (item.Name, item.Value)));
    }

    [Fact]
    public void AHeroFiresWithTheHeroicDrmAndIsWoundedNotBroken()
    {
        // A15.24: a hero adds 1 FP (2 at PBF) and -1 to the DR; alone he is not subject to Cowering (A15.2).
        var alone = Complete(Attack(Rolls([3, 3]), firers: [Firer("ru-h", "defender-hero")]));
        Assert.Contains(("heroic:ru-h", -1m), Drm(alone));
        Assert.False(alone.Arithmetic!.Cowered);
        Assert.Equal(2m, alone.Arithmetic.TotalFirepower);

        // In a group with a squad, the group still Cowers on doubles (A15.24).
        var group = Complete(Attack(Rolls([3, 3]), firers: [Firer("ru-h", "defender-hero"), Firer("ru-1")]));
        Assert.True(group.Arithmetic!.Cowered);

        // A15.2: a hero who fails a MC is wounded, not broken (16 FP, 3+4 +2 in a wooden building is a 1MC; 6+5 +1 fails his 9); a
        // wounded hero who fails is eliminated.
        var hit = Complete(Attack(Rolls([3, 4]), terrain: "wooden-building", targets: [Target("de-h", "attacker-hero")]), checks: [6, 5]).Effects.Single();
        Assert.Equal((true, false, "wounded", 9), (hit.Wounded, hit.Broken, hit.Checks[0].Consequence, hit.Checks[0].MoraleLevel));
        var again = Complete(Attack(Rolls([3, 4]), terrain: "wooden-building", targets: [Target("de-h", "attacker-hero") with { Wounded = true }]),
            checks: [6, 5]).Effects.Single();
        Assert.Equal((true, "eliminated", 8), (again.Eliminated, again.Checks[0].Consequence, again.Checks[0].MoraleLevel));

        // A7.302: Casualty Reduction wounds a hero, as it does any SMC (16 FP, 1+2: a K result).
        var reduced = Complete(Attack(Rolls([1, 2]), targets: [Target("de-h", "attacker-hero")])).Effects.Single();
        Assert.True(reduced.Wounded || reduced.Eliminated);
    }

    /// <summary>The attack dice whose IFT result is <paramref name="result"/> in the attack <paramref name="attack"/> builds from them.</summary>
    private static int[] DiceFor(string result, Func<int[], FireAttack> attack) =>
        Enumerable.Range(1, 6).SelectMany(a => Enumerable.Range(1, 6).Select(b => new[] { a, b }))
            .First(dice => Complete(attack(dice)).Arithmetic?.Result == result);

    [Fact]
    public void AHeroIsNeverPinnedOrBrokenAndACasualtyMcWoundsHimAsIfWounded()
    {
        // A15.2: a hero is not subject to enforced Pin results, so a PTC asks him for nothing and leaves him unpinned.
        FireTarget[] hero = [Target("de-h", "attacker-hero")];
        var ptc = Complete(Attack(Rolls(DiceFor("PTC", dice => Attack(Rolls(dice), targets: hero))), targets: hero), checks: [6, 6]).Effects.Single();
        Assert.Equal((false, 0), (ptc.Pinned, ptc.Checks.Count));

        // A7.301: a unit that cannot break suffers Casualty Reduction instead; a 1KIA eliminates the squad and wounds the hero.
        FireTarget[] pair = [Target("de-s"), Target("de-h", "attacker-hero")];
        var selection = new Dictionary<string, int> { ["de-s"] = 6, ["de-h"] = 1 };
        FireAttack Kia(int[] dice) => Attack(new FireRolls(dice, selection, null, null), targets: pair);
        var kia = Complete(Kia(DiceFor("1KIA", Kia))).Effects.Single(item => item.UnitId == "de-h");
        Assert.Equal((false, true, false), (kia.Broken, kia.Wounded, kia.Eliminated));

        // A10.31 EXC: an Original 12 on his MC wounds him with +1 to the Wound Severity dr, so a dr of 4 is mortal.
        var casualty = Complete(Attack(new FireRolls([3, 4], null, null, null, new Dictionary<string, int> { ["de-h"] = 4 }), terrain: "wooden-building",
            targets: hero), checks: [6, 6]).Effects.Single();
        Assert.True(casualty.Eliminated);

        // A15.2: a heroic leader's Morale Level never exceeds 10, even when Fanatic.
        var heroic = Complete(Attack(Rolls([3, 4]), terrain: "wooden-building",
            targets: [Target("de-l", "attacker-leader-10-2") with { Heroic = true, Fanatic = true }]), checks: [2, 3]).Effects.Single();
        Assert.Equal(10, heroic.Checks[0].MoraleLevel);
    }

    [Fact]
    public void ABattleHardenedUnitIsUnbrokenAndNoLongerDisrupted()
    {
        // A15.3: a broken, Disrupted Russian Conscript that Battle Hardens is exchanged for an unbroken NKVD squad (A25.25):
        // 1+2 = 3, +2 Russian, +1 broken, +1 Inexperienced: 7.
        FireTarget[] russian = [Target("ru-c", "defender-conscript-squad") with { Broken = true, Disrupted = true }];
        FireFirer[] germans = [Firer("de-1", "attacker-squad"), Firer("de-2", "attacker-squad")];
        var conscript = Complete(Attack(Rolls(DiceFor("1MC", dice => Attack(Rolls(dice), firers: germans, targets: russian))), firers: germans, targets: russian),
            checks: [1, 1]).Effects.Single();
        Assert.Equal(("defender-nkvd-squad", false, false), (conscript.FinalDefinitionId, conscript.Broken, conscript.Disrupted));

        // Ruling R28.7: a broken elite squad with no better class becomes Fanatic and is unbroken too: 4+4 = 8, -1 elite, +1 broken.
        var elite = HeatOfBattle([4, 4], Target("de-s", "attacker-elite-squad") with { Broken = true }).Effects.Single();
        Assert.Equal((true, false), (elite.Fanatic, elite.Broken));
    }

    [Fact]
    public void AFanaticUnitHasAHigherMoraleAndIsNeverDisrupted()
    {
        // A10.8: a Fanatic 4-6-8 checks against 9; 4+4 = 8 +1 (1MC) = 9 passes (and pins), where 8 would fail.
        var fanatic = Complete(Attack(Rolls([3, 4]), terrain: "wooden-building", targets: [Target("de-s", "attacker-elite-squad") with { Fanatic = true }]),
            checks: [4, 4]).Effects.Single();
        Assert.Equal((9, true), (fanatic.Checks[0].MoraleLevel, fanatic.Checks[0].Passed));
    }

    [Fact]
    public void AnNkvdUnitTakesTheExtraHeatOfBattleDrm()
    {
        var heat = ScenarioA1HeatOfBattle.Resolve(Reference.Definitions["defender-nkvd-squad"], false, null, false, [3, 3], Reference.Definitions);
        Assert.Equal([("nkvd", -1m), ("nationality:russian", 2m)], heat.Outcome!.Drm.Select(item => (item.Name, item.Value)));

        // A25.25: an NKVD MMC that Battle Hardens becomes Fanatic; a Russian Conscript Battle Hardens into NKVD.
        Assert.True(heat.Outcome.Fanatic);

        // The table's note: a Fanatic unit's 12 is Berserk, not Surrender.
        var fanatic = ScenarioA1HeatOfBattle.Resolve(Reference.Definitions["defender-nkvd-squad"], true, null, true, [6, 6], Reference.Definitions, true, []);
        Assert.Equal(HeatOfBattleOutcome.Berserk, fanatic.Outcome!.Result);
        var conscript = ScenarioA1HeatOfBattle.Resolve(Reference.Definitions["defender-conscript-squad"], false, false, false, [2, 3], Reference.Definitions);
        Assert.Equal("defender-nkvd-squad", conscript.Outcome!.HardenedDefinitionId);
    }
}
