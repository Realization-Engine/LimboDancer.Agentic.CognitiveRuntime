using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The Fire package as revised for backlog pass 12 (rulings R12.1 to R12.11): Opportunity Fire, fire at a blocked LOS, FPF variants and pinned
/// movers, a leader's MG, split fire, Spraying Fire, Encirclement, and targets in a Melee or held as prisoners.
/// </summary>
public sealed class ScenarioA1Pass12Tests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    private static FireFirer Firer(string id, string definition = "defender-squad") => new(id, definition, From, false, false, false, false, false);

    private static FireTarget Target(string id, string definition = "attacker-squad") => new(id, definition, At, false, false, false, false, false, false, false);

    private static FireWeapon Mg(string id, string definition = "defender-lmg") => new(id, definition, false, false, false);

    private static FireAttack Attack(int[] dice, string phase = "PFPh", string side = "phasing", FireFirer[]? firers = null, FireTarget[]? targets = null,
        FireDirector? director = null, bool blocked = false) =>
        new(phase, side, true, From, At, firers ?? [Firer("ru-1"), Firer("ru-2")], director, 1, true, new FireLos(blocked, 0, true, false), null,
            "open-ground", targets ?? [Target("de-s")], 3, new FireRolls(dice, null, null, null));

    private static FireAttack Moving(FireAttack attack, string kind) => attack with
    {
        Phase = "MPh",
        FiringSide = "non-phasing",
        FireKind = kind,
        TargetMovement = new FireMovement(false),
    };

    /// <summary>Resolves an attack, supplying each further roll with a fixed value: checks as given (2 and 2 by default), drs of 1.</summary>
    private static FireResolution Complete(FireAttack attack, int[]? checks = null)
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
    public void OpportunityFireIsNotHalvedInTheAfph()
    {
        // A7.24, A7.25 (ruling R12.1): two 4-4-7 at PBF in the AFPh, 4 each halved; as Opportunity Fire, 8 each.
        var halved = Complete(Attack([6, 5], phase: "AFPh"));
        Assert.Equal(8m, halved.Arithmetic!.TotalFirepower);
        var opportunity = Complete(Attack([6, 5], phase: "AFPh", firers: [Firer("ru-1") with { OpportunityFire = true }, Firer("ru-2") with { OpportunityFire = true }]));
        Assert.Equal(16m, opportunity.Arithmetic!.TotalFirepower);
        Assert.DoesNotContain(opportunity.Arithmetic.Firers.SelectMany(item => item.Multipliers), item => item.Name == "advancing-fire");

        // Opportunity Fire is made in the AFPh only.
        var early = Complete(Attack([6, 5], firers: [Firer("ru-1") with { OpportunityFire = true }]));
        Assert.Contains("asl.a1.fire.phase-outside", early.Reasons);
    }

    [Fact]
    public void FireAtABlockedLosAffectsNothingButMarksTheFirers()
    {
        // A6.11 (ruling R12.2): the DR is made, the firers are marked Prep Fire, and the target is untouched.
        var result = Complete(Attack([1, 1], blocked: true));
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.True(result.LosBlocked);
        Assert.Equal(["ru-1", "ru-2"], result.FireCounterUnitIds);
        Assert.Equal("prep-fire", result.FireCounter);
        Assert.All(result.Effects, effect => Assert.False(effect.Broken || effect.Pinned || effect.Eliminated));
    }

    [Fact]
    public void PinnedMoversOfAMixedStackTakeTheirOwnFinalDr()
    {
        // A7.83 (ruling R12.3): FFNAM and FFMO, -2, apply to the unpinned mover only; the pinned one's Final DR is two higher.
        var result = Complete(Moving(Attack([4, 3], targets: [Target("de-a"), Target("de-b") with { Pinned = true }]), ScenarioA1FireCalculator.FirstFire));
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Equal(result.Arithmetic!.FinalDr + 2, result.Arithmetic.PinnedFinalDr);
        Assert.NotNull(result.Arithmetic.PinnedResult);
    }

    [Fact]
    public void FpfMayBeDirectedAndMixedWithFirstFire()
    {
        // A8.31 (ruling R12.3): ru-1 uses FPF, ru-2 Subsequent First Fire, both Area Fire; only ru-1 and the directing leader take the NMC.
        var leader = new FireDirector("ru-l", "defender-leader", From, false, false, false, false, false);
        var attack = Moving(Attack([6, 5], side: "non-phasing", firers: [Firer("ru-1") with { FinalFireMarked = true, FiredThisPlayerTurn = true },
            Firer("ru-2") with { FirstFireMarked = true }], director: leader), ScenarioA1FireCalculator.FinalProtectiveFire) with
        {
            FiringSideElr = 3,
            WithinSubsequentFirstFireRange = true,
        };
        var result = Complete(attack);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Contains(result.Arithmetic!.Firers.Single(item => item.UnitId == "ru-1").Multipliers, item => item.Name == "area-fire" && item.Rule == "A8.31");
        Assert.Contains(result.Arithmetic.Firers.Single(item => item.UnitId == "ru-2").Multipliers, item => item.Name == "area-fire" && item.Rule == "A8.3");

        // Referee, pass 12: an unmarked unit does not join FPF, since its First Fire counter would differ.
        var unmarked = Complete(attack with
        {
            Firers = [attack.Firers![0], Firer("ru-2")]
        });
        Assert.Contains("asl.a1.fire.firer-already-fired", unmarked.Reasons);
        Assert.Equal(["ru-1", "ru-l"], result.FirerEffects!.Select(item => item.UnitId).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ALeaderFiresAMgAsAreaFireAloneAndAtFullFpWithAPartner()
    {
        // A9.12 (ruling R12.4): the leader's LMG, 2 FP at PBF 4, halved alone; with a stacked SMC as his partner it keeps 4, and both are marked.
        var lone = Firer("ru-l", "defender-leader") with
        {
            UsesInherentFp = false,
            Weapons = [Mg("ru-mg")]
        };
        var alone = Complete(Attack([6, 5], firers: [lone]));
        Assert.Equal(2m, alone.Arithmetic!.TotalFirepower);
        var together = Complete(Attack([6, 5], firers: [lone with { Partner = "ru-l2" }]));
        Assert.Equal(4m, together.Arithmetic!.TotalFirepower);
        Assert.Equal(["ru-l", "ru-l2"], together.FireCounterUnitIds);
    }

    [Fact]
    public void ASquadFiringItsMgAloneKeepsItsInherentFp()
    {
        // A7.351 (ruling R12.4): the squad fires only its LMG, so it is not marked and keeps its inherent FP for another attack.
        var result = Complete(Attack([6, 5], firers: [Firer("ru-1") with { UsesInherentFp = false, Weapons = [Mg("ru-mg")] }]));
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Empty(result.FireCounterUnitIds);
    }

    [Fact]
    public void SprayingFireIsAreaFireAndItsSecondRecordMarksNothing()
    {
        // A9.5 (ruling R12.6): the squad with its LMG sprays; each Location is attacked at half FP; the second record leaves the markers to the first.
        var sprayer = Firer("ru-1") with
        {
            Weapons = [Mg("ru-mg")]
        };
        var first = Complete(Attack([6, 5], firers: [sprayer]) with
        {
            SprayingFire = true
        });
        Assert.All(first.Arithmetic!.Firers, item => Assert.Contains(item.Multipliers, multiplier => multiplier.Name == "spraying-fire"));
        Assert.Equal(["ru-1"], first.FireCounterUnitIds);
        var second = Complete(Attack([6, 5], firers: [sprayer]) with
        {
            SprayingFire = true,
            SprayShare = true
        });
        Assert.Empty(second.FireCounterUnitIds);
        Assert.Null(second.WeaponEffects);

        // A9.5: every firer of a spraying group fires a MG.
        var bare = Complete(Attack([6, 5]) with
        {
            SprayingFire = true
        });
        Assert.Contains("asl.a1.fire.spraying-fire-outside", bare.Reasons);
    }

    [Fact]
    public void EncirclementLowersTheTargetsMoraleAndAddsOneToItsOwnFire()
    {
        // A7.7 (ruling R12.11): 16 FP, 6 + 4 = 10: a NMC against Morale Level 6 instead of 7.
        var result = Complete(Attack([6, 4], targets: [Target("de-s") with { Encircled = true }]));
        Assert.Equal(6, result.Effects.Single().Checks.Single().MoraleLevel);
        var firing = Complete(Attack([6, 4], firers: [Firer("ru-1") with { Encircled = true }, Firer("ru-2")]));
        Assert.Contains(firing.Arithmetic!.Drm, item => item.Name == "encircled:ru-1" && item.Value == 1m);
    }

    [Fact]
    public void FireIntoAMeleeAttacksBothSidesEachAgainstItsOwnElr()
    {
        // A11.15 (ruling R12.8): the Russian squad in the Melee is attacked with the Germans; it needs its own side's ELR.
        var targets = new[] { Target("de-s"), Target("ru-m", "defender-squad") with { Friendly = true } };
        var undeclared = ScenarioA1FireCalculator.Precheck(Attack([6, 4], targets: targets) with
        {
            Rolls = null
        }, Reference);
        Assert.Contains("asl.a1.fire.elr-undecided:firing-side-elr-undeclared", undeclared);
        var result = Complete(Attack([6, 4], targets: targets) with
        {
            FiringSideElr = 3
        });
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Equal(2, result.Effects.Count);
    }

    [Fact]
    public void APrisonerIsReducedNotBroken()
    {
        // A20.54 (ruling R12.9): the captured Russian squad fails its NMC (6 + 5 = 11 against 7) and becomes a HS; the German Guard breaks.
        var targets = new[] { Target("de-s"), Target("ru-p", "defender-squad") with { Friendly = true, GuardId = "de-s" } };
        var result = Complete(Attack([6, 4], targets: targets) with
        {
            FiringSideElr = 3
        }, checks: [6, 5]);
        var prisoner = result.Effects.Single(item => item.UnitId == "ru-p");
        Assert.False(prisoner.Broken);
        Assert.NotEqual(prisoner.DefinitionId, prisoner.FinalDefinitionId);
        Assert.True(result.Effects.Single(item => item.UnitId == "de-s").Broken);
    }
}
