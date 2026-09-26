using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The Fire package as revised for unit steps 19 to 23: Advancing Fire, fire groups across Locations, hidden and Dummy
/// targets, Defensive First Fire with FFNAM and FFMO, Subsequent First Fire, FPF and its NMC, Residual FP, Final Fire by
/// First-Fire-marked units, MGs, and the Heat of Battle deviation of ruling R0.2.
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
        Phase = "MPh", FiringSide = "non-phasing", FireKind = kind, TargetMovement = new FireMovement(assault),
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
            var result = ScenarioA1FireCalculator.Resolve(attack with { Rolls = rolls }, Reference);
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
        var attack = Attack(Rolls([6, 5]), firers: firers) with { FirerLocationsAdjacent = true };
        var result = Complete(attack);
        Assert.Equal(12m, result.Arithmetic!.TotalFirepower);
        Assert.Contains(("los-hindrance", 1m), Drm(result));

        // A7.5: the Locations must be ADJACENT; A7.531: direction needs a leader in every Location.
        Assert.Contains("asl.a1.fire.firer-outside",
            Complete(attack with { FirerLocationsAdjacent = false }).Reasons);
        Assert.Contains("asl.a1.fire.director-outside", Complete(attack with { Director = Leader }).Reasons);
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
        var hindered = Complete(Moving(Attack(Rolls([6, 5])) with { Los = new FireLos(false, 1, true, false) },
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
            Firers = null, Director = null, FireGroupComplete = null, FirerLocationId = null, Range = null, SameLevel = null, Los = null, ResidualFp = 4,
        }, ScenarioA1FireCalculator.ResidualFire);
        var result = Complete(attack);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Equal((4, false, (string?)null), (result.Arithmetic!.ColumnFp!.Value, result.Arithmetic.Cowered, result.FireCounter));
        Assert.Null(result.Arithmetic.ResidualFp);
        Assert.Contains("asl.a1.fire.residual-outside", Complete(attack with { ResidualFp = 3 }).Reasons);
    }

    [Fact]
    public void SubsequentFirstFireIsAreaFireWithinRange()
    {
        var firers = new[] { Firer("ru-1") with { FirstFireMarked = true }, Firer("ru-2") with { FirstFireMarked = true } };
        var attack = Moving(Attack(Rolls([6, 5]), firers: firers) with { WithinSubsequentFirstFireRange = true }, ScenarioA1FireCalculator.SubsequentFirstFire);
        var result = Complete(attack);
        Assert.Equal((8m, "final-fire"), (result.Arithmetic!.TotalFirepower, result.FireCounter));
        Assert.Contains("asl.a1.fire.subsequent-first-fire-outside",
            Complete(attack with { WithinSubsequentFirstFireRange = false }).Reasons);
        Assert.Contains("asl.a1.fire.firer-already-fired",
            Complete(attack with { Firers = [Firer("ru-1")] }).Reasons);
    }

    [Fact]
    public void FinalProtectiveFireChecksItsFirersOnTheOriginalDr()
    {
        var firers = new[] { Firer("ru-1") with { FinalFireMarked = true }, Firer("ru-2") with { FinalFireMarked = true } };
        var attack = Moving(Attack(Rolls([4, 4]), firers: firers) with { FiringSideElr = 2 },
            ScenarioA1FireCalculator.FinalProtectiveFire);

        // Area Fire and PBF: 8 FP. The Original 8 is a NMC for each 4-4-7: failed, broken within ELR 2.
        var result = Complete(attack);
        Assert.Equal(8m, result.Arithmetic!.TotalFirepower);
        Assert.All(result.FirerEffects!, effect => Assert.True(effect.Broken));

        // An Original 12 is a Casualty MC for one of the two, chosen by Random Selection (A8.31).
        var twelve = Complete(attack with { Rolls = Rolls([6, 6]) }, key => key.StartsWith("firerSelection:", StringComparison.Ordinal));
        Assert.Equal(["asl.a1.fire.roll-missing:firerSelection:ru-1,ru-2"], twelve.Reasons);

        // Undirected only, and an ELR for the firing side before any roll.
        Assert.Contains("asl.a1.fire.fpf-outside", Complete(attack with { Director = Leader }).Reasons);
        Assert.Contains("asl.a1.fire.elr-undecided:firing-side-elr-undeclared",
            ScenarioA1FireCalculator.Precheck(attack with { FiringSideElr = null, Rolls = null }, Reference));
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
        var squad = Firer("ru-1") with { Pinned = true, Weapons = [mmg] };

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

    [Fact]
    public void AnOriginalTwoOnAnMcRecordsThatHeatOfBattleWasNotTaken()
    {
        // 16 FP: 3+4 = 7 with the building's +2 is a 1MC; the squad's Original 2 passes and records the deviation of R0.2.
        var result = Complete(Attack(Rolls([3, 4]), terrain: "wooden-building"), checks: [1, 1]);
        Assert.Equal("1MC", result.Arithmetic!.Result);
        Assert.Equal(2, result.Effects.Single().Checks.Single().OriginalDr);
        Assert.Contains("heat-of-battle-not-taken", result.Effects.Single().Events);
    }
}
