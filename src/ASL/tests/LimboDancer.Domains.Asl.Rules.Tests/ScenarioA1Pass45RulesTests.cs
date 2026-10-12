using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The Fire package as pass 45 (Infantry II) leaves it: a fire kind for each firer of an attack (A8.3, A8.31, A8.312; ruling R45.1), the group as one
/// chain (A7.5), no berserk unit in a multi-Location group (A7.54), and a crew's inherent FP after its Gun fired (A7.353).
/// </summary>
public sealed class ScenarioA1Pass45RulesTests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    private static FireFirer Firer(string id, string definition = "defender-squad") => new(id, definition, From, false, false, false, false, false);

    private static FireTarget Target(string id, string definition = "attacker-squad") => new(id, definition, At, false, false, false, false, false, false, false);

    private static FireWeapon Mg(string id, string definition = "defender-lmg") => new(id, definition, false, false, false);

    private static FireAttack Attack(int[] dice, string phase = "PFPh", string side = "phasing", FireFirer[]? firers = null, FireTarget[]? targets = null,
        FireDirector? director = null) =>
        new(phase, side, true, From, At, firers ?? [Firer("ru-1"), Firer("ru-2")], director, 1, true, new FireLos(false, 0, true, false), null,
            "open-ground", targets ?? [Target("de-s")], 3, new FireRolls(dice, null, null, null));

    private static FireAttack Moving(FireAttack attack, string kind) => attack with
    {
        Phase = "MPh",
        FiringSide = "non-phasing",
        FireKind = kind,
        TargetMovement = new FireMovement(false),
        WithinSubsequentFirstFireRange = true,
        FiringSideElr = 3,
    };

    /// <summary>Resolves an attack, supplying each further roll with a fixed value: checks of 2 and 2, drs of 1.</summary>
    private static FireResolution Complete(FireAttack attack)
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

            IReadOnlyList<int> dice = [2, 2];
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

    private static IReadOnlyList<FireModifier> MultipliersOf(FireResolution result, string id) => result.Arithmetic!.Firers.Single(item => item.UnitId == id).Multipliers;

    [Fact]
    public void AUnitWithNoCounterFiresFirstFireInOneGroupWithFpf()
    {
        // A8.31 (p. 61; ruling R45.1): "A unit using FPF may form a FG with units not using FPF, but only those units using FPF are affected by its
        // adverse affects." ru-1 is marked Final Fire and uses FPF (4 x 2 PBF, halved: 4); ru-2 has no counter and fires First Fire (4 x 2: 8).
        var result = Complete(Moving(Attack([6, 5], firers: [Firer("ru-1") with { FinalFireMarked = true, FiredThisPlayerTurn = true }, Firer("ru-2")]),
            ScenarioA1FireCalculator.FinalProtectiveFire));
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Contains(MultipliersOf(result, "ru-1"), item => item.Name == "area-fire" && item.Rule == "A8.31");
        Assert.DoesNotContain(MultipliersOf(result, "ru-2"), item => item.Name == "area-fire");
        Assert.Equal(12m, result.Arithmetic!.TotalFirepower);

        // Only the FPF firer takes the NMC, and the unit with no counter is marked First Fire, not Final Fire.
        Assert.Equal(["ru-1"], result.FirerEffects!.Select(item => item.UnitId));
        Assert.Equal("final-fire", result.FireCounter);
        var own = Assert.Single(result.FirerCounters!);
        Assert.Equal(("ru-2", "first-fire"), (own.Key, own.Value));
    }

    [Fact]
    public void AUnitWithNoCounterFiresFirstFireInOneGroupWithSubsequentFirstFire()
    {
        // A8.3, A8.312 (ruling R45.1): ru-1 is marked First Fire and fires again as Area Fire (4); ru-2 fires First Fire (8).
        var result = Complete(Moving(Attack([6, 5], firers: [Firer("ru-1") with { FirstFireMarked = true, FiredThisPlayerTurn = true }, Firer("ru-2")]),
            ScenarioA1FireCalculator.SubsequentFirstFire));
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Contains(MultipliersOf(result, "ru-1"), item => item.Name == "area-fire" && item.Rule == "A8.3");
        Assert.DoesNotContain(MultipliersOf(result, "ru-2"), item => item.Name == "area-fire");
        Assert.Equal(12m, result.Arithmetic!.TotalFirepower);
        Assert.Null(result.FirerEffects);
        Assert.Equal("first-fire", result.FirerCounters!["ru-2"]);

        // An attack of Subsequent First Fire has a firer that uses it.
        var none = Complete(Moving(Attack([6, 5]), ScenarioA1FireCalculator.SubsequentFirstFire));
        Assert.Contains("asl.a1.fire.subsequent-first-fire-outside", none.Reasons);
    }

    [Fact]
    public void AMgMarkedFirstFireFiresSustainedBesideItsSquadsFirstFire()
    {
        // The example of pp. 61 to 62: "If the LMG (only) were already marked with a First Fire counter ... that attack would use 11 FP ({4 [4-6-7 FP]
        // + 1 1/2 [LMG Area Fire]} x 2 [PBF] = 11) since the LMG would be using Sustained Fire ... the 4-6-7 and LMG would then both be marked
        // with a Final Fire counter." Here a 4-4-7 and a 2 FP LMG: (4 + 1) x 2 = 10.
        var squad = Firer("ru-1") with
        {
            Weapons = [Mg("ru-mg") with { FirstFireMarked = true }],
            UsesSupportWeapon = true,
        };
        var result = Complete(Moving(Attack([6, 5], firers: [squad]), ScenarioA1FireCalculator.SubsequentFirstFire));
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Equal(10m, result.Arithmetic!.TotalFirepower);
        Assert.DoesNotContain(MultipliersOf(result, "ru-1"), item => item.Name == "area-fire");
        Assert.Contains(MultipliersOf(result, "ru-mg"), item => item.Name == "area-fire");
        var mg = Assert.Single(result.WeaponEffects!);
        Assert.True(mg.SustainedFire);
        Assert.Equal("final-fire", mg.FireCounter);
        Assert.Equal(9, mg.BreakdownNumber);

        // The squad used Subsequent First Fire with its MG, so it takes the attack's Final Fire counter.
        Assert.Null(result.FirerCounters);
        Assert.Contains("ru-1", result.FireCounterUnitIds);
    }

    [Fact]
    public void AMgOfAUnitWithNoCounterKeepsItsRateOfFireBesideFpf()
    {
        // The example of p. 62: a crew that "had held its fire" fires its LMG at full FP beside the squad's FPF, and "The crew or its LMG (now both
        // marked with a First Fire counter)". Here ru-2 fires its LMG with a colored dr of 1: not Sustained Fire, the ROF kept.
        var holder = Firer("ru-2") with
        {
            Weapons = [Mg("ru-mg")],
            UsesSupportWeapon = true,
        };
        var result = Complete(Moving(Attack([1, 5], firers: [Firer("ru-1") with { FinalFireMarked = true, FiredThisPlayerTurn = true }, holder]),
            ScenarioA1FireCalculator.FinalProtectiveFire));
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        var mg = Assert.Single(result.WeaponEffects!);
        Assert.False(mg.SustainedFire);
        Assert.True(mg.RateOfFireRetained);
        Assert.Null(mg.FireCounter);

        // With a colored dr of 6 the MG loses its ROF and is marked First Fire with its unit.
        var lost = Complete(Moving(Attack([6, 2], firers: [Firer("ru-1") with { FinalFireMarked = true, FiredThisPlayerTurn = true }, holder]),
            ScenarioA1FireCalculator.FinalProtectiveFire));
        Assert.Equal("first-fire", Assert.Single(lost.WeaponEffects!).FireCounter);
    }

    [Fact]
    public void ABerserkUnitJoinsNoMultiLocationGroup()
    {
        // A7.54 (p. 57): "A berserk unit may never be part of a multi-Location FG."
        var beside = Firer("ru-2") with
        {
            LocationId = "bd01:F6:0",
            Range = 1,
            SameLevel = true,
            Los = new FireLos(false, 0, true, false),
        };
        var group = Attack([6, 5], firers: [Firer("ru-1") with { Berserk = true, Range = 1, SameLevel = true, Los = new FireLos(false, 0, true, false) }, beside]) with
        {
            FirerLocationsAdjacent = true,
        };
        Assert.Contains("asl.a1.fire.berserk-group-outside", ScenarioA1FireCalculator.Resolve(group, Reference).Reasons);

        // In its own Location it fires with the others.
        var together = Complete(Attack([6, 5], firers: [Firer("ru-1") with { Berserk = true }, Firer("ru-2")]));
        Assert.Equal(FireResolution.Resolved, together.Disposition);
    }

    [Fact]
    public void AFireGroupIsOneChainOfAdjacentLocations()
    {
        // A7.5 (p. 57): "a virtually unlimited string of ADJACENT Locations". A-B and C-D, each pair ADJACENT and the pairs apart, are no one group;
        // A-B-C-D in a row are.
        var los = new FireLos(false, 0, true, false);
        string[] names = ["A", "B", "C", "D"];
        var firers = names.Select(name => new FireFirer("u" + name, "defender-squad", name, false, false, false, false, false)).ToArray();
        var attack = Attack([6, 5], firers: firers) with
        {
            FirerLocationId = "A",
        };
        var perLocation = names.ToDictionary(name => name, _ => (Range: 2, SameLevel: true, Los: los, Height: 0), StringComparer.Ordinal);
        bool Pairs(string one, string two) => (one, two) is ("A", "B") or ("B", "A") or ("C", "D") or ("D", "C");
        bool Row(string one, string two) => Math.Abs(one[0] - two[0]) == 1;
        Assert.False(ScenarioA1FireMapRules.GroupMapFacts(attack, "open-ground", 0, perLocation, Pairs).FirerLocationsAdjacent);
        Assert.True(ScenarioA1FireMapRules.GroupMapFacts(attack, "open-ground", 0, perLocation, Row).FirerLocationsAdjacent);
    }

    [Fact]
    public void AMgDeductsTwoAtItsBoreSightedLocationInDefensiveFirstFire()
    {
        // C6.44 (p. 174): "may deduct two from its IFT DR unless using Spraying Fire or taking a Snap Shot [EXC: vs Infantry, only such an attack
        // conducted as Defensive First Fire (A8.1) qualifies for this -2 DRM]".
        var gunner = Firer("ru-1") with
        {
            UsesInherentFp = false,
            Weapons = [Mg("ru-mg", "defender-mmg")],
            UsesSupportWeapon = true,
        };
        var sighted = Moving(Attack([6, 5], firers: [gunner]), ScenarioA1FireCalculator.FirstFire) with
        {
            MgBoreSighted = true,
        };
        Assert.Contains(Complete(sighted).Arithmetic!.Drm, item => item.Name == "bore-sighting" && item.Value == -2m && item.Rule == "C6.44");

        // Not in Prep Fire against Infantry, not as a Snap Shot, and not without the sighting.
        Assert.DoesNotContain(Complete(Attack([6, 5], firers: [gunner]) with { MgBoreSighted = true }).Arithmetic!.Drm, item => item.Name == "bore-sighting");
        Assert.DoesNotContain(Complete(sighted with { SnapShot = true }).Arithmetic!.Drm, item => item.Name == "bore-sighting");
        Assert.DoesNotContain(Complete(sighted with { MgBoreSighted = null }).Arithmetic!.Drm, item => item.Name == "bore-sighting");
    }

    [Fact]
    public void OnlyMgsFiredAloneAtTheirSightedLocationAreBoreSighted()
    {
        // C6.44: "only if all elements of that FG have Bore Sighted that same Location"; a squad's own FP has Bore Sighted nothing.
        Assert.True(ScenarioA1FireEligibility.MgBoreSighted([(true, [true])]));
        Assert.True(ScenarioA1FireEligibility.MgBoreSighted([(true, [true]), (true, [true])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(false, [true])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [true]), (true, [false])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([]));

        // C6.41, C6.42: a MMG or HMG of the Scenario Defender, outside its hex, in its LOS and Normal Range.
        Assert.Null(ScenarioA1GameStart.MgBoreSightBar("medium", true, false, () => true, "mg"));
        Assert.Null(ScenarioA1GameStart.MgBoreSightBar("heavy", true, false, () => true, "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("light", true, false, () => true, "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", false, false, () => true, "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, true, () => true, "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, false, () => false, "mg"));
    }

    [Fact]
    public void AnotherActOfTheDefenderWaitsForTheAttackOwed()
    {
        // A8.312 (p. 61; ruling R45.2): the DEFENDER "must ... then immediately attack".
        var bar = ScenarioA1MandatoryFire.OtherActBar(["r4", "r5"], "bd01:A1:0", "Gun's shot");
        Assert.StartsWith("play.act-mandatory: r4, r5 must first attack", bar);
        Assert.Contains("Gun's shot", bar);
    }

    [Fact]
    public void ACrewThatFiredItsGunKeepsItsFpInSubsequentFirstFireAndFinalFireAgain()
    {
        // A7.353 (p. 56): "Subsequent First Fire (8.3), FPF (8.31), and Final Fire (8.4) vs adjacent units retain halved inherent FP".
        var crew = Firer("ru-c", "defender-crew") with
        {
            GunFired = true,
            FirstFireMarked = true,
            FiredThisPlayerTurn = true,
        };
        var subsequent = Complete(Moving(Attack([6, 5], firers: [crew]), ScenarioA1FireCalculator.SubsequentFirstFire));
        Assert.Equal(FireResolution.Resolved, subsequent.Disposition);
        Assert.Contains(MultipliersOf(subsequent, "ru-c"), item => item.Name == "area-fire");

        var finalAgain = Complete(Attack([6, 5], phase: "DFPh", side: "non-phasing", firers: [crew]));
        Assert.Equal(FireResolution.Resolved, finalAgain.Disposition);

        // In its first fire of the Player Turn it has none (A7.352; ruling R8.4).
        var first = ScenarioA1FireCalculator.Resolve(Attack([6, 5], firers: [crew with { FirstFireMarked = null, FiredThisPlayerTurn = false }]), Reference);
        Assert.Contains("asl.a1.fire.firer-outside", first.Reasons);
    }
}
