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
            "open-ground", targets ?? [Target("de-s")], 3, new FireRolls(dice, null, null, null))
        {
            // As the planner records with every attack from pass 45 on (ruling R45.4).
            CoweringMarksAndWeaponResidual = true,
        };

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
                "cowerSelection" => rolls with { CowerSelection = Add(rolls.CowerSelection, 1) },
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
        var good = new MgSightingFacts(true, true, true, true, false);
        Assert.True(ScenarioA1FireEligibility.MgBoreSighted([(true, [good])]));
        Assert.True(ScenarioA1FireEligibility.MgBoreSighted([(true, [good]), (true, [good])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(false, [good])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [good]), (true, [good with { Sighted = false }])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([]));

        // C6.43: not by another unit, not from another Location, not at another Location, and not while "is dm".
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [good with { ByItsPossessor = false }])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [good with { AtItsSetupLocation = false }])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [good with { OnTheTarget = false }])]));
        Assert.False(ScenarioA1FireEligibility.MgBoreSighted([(true, [good with { Dismantled = true }])]));

        // C6.41, C6.42: a MMG or HMG of the Scenario Defender, outside its hex, in its LOS, within its Normal Range and 16 hexes.
        Assert.Null(ScenarioA1GameStart.MgBoreSightBar("medium", true, false, 10, () => (true, 10), "mg"));
        Assert.Null(ScenarioA1GameStart.MgBoreSightBar("heavy", true, false, 20, () => (true, 16), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("heavy", true, false, 20, () => (true, 17), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, false, 10, () => (true, 11), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("light", true, false, 6, () => (true, 2), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", false, false, 10, () => (true, 2), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, true, 10, () => (true, 2), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, false, 10, () => (false, 2), "mg"));
        Assert.StartsWith("play.bore-sight", ScenarioA1GameStart.MgBoreSightBar("medium", true, false, 10, () => null, "mg"));
    }

    [Fact]
    public void AnotherActOfTheDefenderWaitsForTheAttackOwed()
    {
        // A8.312 (p. 61; ruling R45.2): the DEFENDER "must ... then immediately attack".
        string[] owing = ["r4", "r5"];
        var bar = ScenarioA1MandatoryFire.OtherActBar(true, true, () => (owing, "bd01:A1:0"), "Gun's shot");
        Assert.StartsWith("play.act-mandatory: r4, r5 must first attack", bar);
        Assert.Contains("Gun's shot", bar);

        // Never for the ATTACKER's proposal, outside the MPh, or with nothing owed; the attack owed is not even read for the first two.
        Assert.Null(ScenarioA1MandatoryFire.OtherActBar(true, false, () => throw new InvalidOperationException(), "Gun's shot"));
        Assert.Null(ScenarioA1MandatoryFire.OtherActBar(false, true, () => throw new InvalidOperationException(), "Gun's shot"));
        Assert.Null(ScenarioA1MandatoryFire.OtherActBar(true, true, () => null, "Gun's shot"));
    }

    [Fact]
    public void FireAtTheNextLevelOfTheFirersHexIsPointBlankFire()
    {
        // A7.21, B23.26 (pp. 54, 136): "doubled while ... ADJACENT to its target"; "Attacks up or down a stairwell hex ... are limited to the next
        // higher or lower level." Two 4-4-7 at level 1 fire at ground level of their hex: 8 FP each, not the 12 of TPBF.
        const string upstairs = "bd01:G5:1";
        FireFirer Up(string id) => new(id, "defender-squad", upstairs, false, false, false, false, false);
        var down = Attack([6, 5], firers: [Up("ru-1"), Up("ru-2")]) with
        {
            FirerLocationId = upstairs,
            Range = 0,
            SameLevel = false,
            TargetLevelAbove = -1,
        };
        var result = Complete(down);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Equal(16m, result.Arithmetic!.TotalFirepower);
        Assert.All(result.Arithmetic.Firers, firer => Assert.Contains(firer.Multipliers, item => item.Name == "point-blank-fire"));

        // Two levels apart there is no attack.
        Assert.Contains("asl.a1.fire.out-of-range", ScenarioA1FireCalculator.Resolve(down with { TargetLevelAbove = -2 }, Reference).Reasons);

        // The planner's read: the next level, and a stairwell between them.
        Assert.Null(ScenarioA1FireMapRules.SameHexLevelBar("a", "b", 1, () => true));
        Assert.StartsWith("play.fire-level", ScenarioA1FireMapRules.SameHexLevelBar("a", "b", 1, () => false));
        Assert.StartsWith("play.fire-level", ScenarioA1FireMapRules.SameHexLevelBar("a", "b", 2, () => true));
    }

    [Fact]
    public void FireFromAboveAndFromBelowEncircles()
    {
        // A7.7 (p. 57): "An Encirclement can also be created by a LOF from both the Location directly above and below it in a building hex."
        Assert.True(ScenarioA1Geometry.Encircles([ScenarioA1FireMapRules.FromAbove, ScenarioA1FireMapRules.FromBelow]));
        Assert.False(ScenarioA1Geometry.Encircles([ScenarioA1FireMapRules.FromAbove]));
        Assert.False(ScenarioA1Geometry.Encircles([ScenarioA1FireMapRules.FromAbove, 1]));
        Assert.False(ScenarioA1Geometry.Encircles([ScenarioA1FireMapRules.FromBelow, 7]));

        // The hexside cases stand as they were: opposite hexsides, and opposite hexspines.
        Assert.True(ScenarioA1Geometry.Encircles([1, 7]));
        Assert.True(ScenarioA1Geometry.Encircles([0, 6]));
    }

    private static FireFirer Gunner(string id, string weapon = "defender-mmg") => Firer(id) with
    {
        Weapons = [Mg(id + "-mg", weapon)],
        UsesSupportWeapon = true,
    };

    [Fact]
    public void AUnitThatCowersIsMarkedFinalFireWithItsMg()
    {
        // A7.9 (p. 58): "any unit that cowers (as well as all of its SW regardless of whether it was using its inherent FP) is automatically marked
        // with a Final or Prep Fire counter as appropriate." A squad and its MMG (ROF 2) roll doubles of 2 in Defensive First Fire: without the
        // Cowering the MMG would keep its ROF and the squad be marked First Fire.
        var cowers = Complete(Moving(Attack([2, 2], firers: [Gunner("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.True(cowers.Arithmetic!.Cowered);
        Assert.Equal(["ru-1"], cowers.CoweredUnitIds);
        Assert.Equal("first-fire", cowers.FireCounter);
        Assert.Equal("final-fire", cowers.FirerCounters!["ru-1"]);
        var mg = Assert.Single(cowers.WeaponEffects!);
        Assert.False(mg.RateOfFireRetained);
        Assert.Equal("final-fire", mg.FireCounter);

        // The same DR's colored dr with no doubles keeps the ROF.
        var steady = Complete(Moving(Attack([2, 3], firers: [Gunner("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.Null(steady.CoweredUnitIds);
        Assert.True(Assert.Single(steady.WeaponEffects!).RateOfFireRetained);

        // In Prep Fire the counter is the Prep Fire one the attack places anyway; the MG still loses its ROF.
        var prep = Complete(Attack([2, 2], firers: [Gunner("ru-1")]));
        Assert.Equal(["ru-1"], prep.CoweredUnitIds);
        Assert.Null(prep.FirerCounters);
        Assert.False(Assert.Single(prep.WeaponEffects!).RateOfFireRetained);

        // A squad that fired only its MG is marked too, though the MG's fire alone would leave it unmarked (A7.351).
        var alone = Complete(Moving(Attack([2, 2], firers: [Gunner("ru-1") with { UsesInherentFp = false }]), ScenarioA1FireCalculator.FirstFire));
        Assert.Contains("ru-1", alone.FireCounterUnitIds);
    }

    [Fact]
    public void RandomSelectionMarksTheCoweringUnitOfAFireGroup()
    {
        // A7.9: "If a FG cowers, Random Selection is used to determine the unit(s) (and its SW) that becomes marked with a Prep or Final Fire counter."
        var group = Moving(Attack([4, 4]), ScenarioA1FireCalculator.FirstFire);

        // The selection is asked for as a roll of its own, after the targets' checks; the helper answers each dr with 1, a tie.
        Assert.Equal(["ru-1", "ru-2"], Complete(group).CoweredUnitIds);
        FireResolution With(int one, int two) => Complete(group with
        {
            Rolls = group.Rolls! with { CowerSelection = new Dictionary<string, int> { ["ru-1"] = one, ["ru-2"] = two } },
        });
        var second = With(2, 5);
        Assert.Equal(["ru-2"], second.CoweredUnitIds);
        Assert.Equal("final-fire", Assert.Single(second.FirerCounters!).Value);
        Assert.Equal(["ru-1", "ru-2"], With(3, 3).CoweredUnitIds);
    }

    [Fact]
    public void ABerserkFirerNeverCowersAndOnlyInexperiencedPersonnelShiftTwo()
    {
        // A7.9: "Cowering affects all fire except that from a SMC, berserk or Fanatic unit".
        Assert.False(Complete(Attack([3, 3], firers: [Firer("ru-1") with { Berserk = true }])).Arithmetic!.Cowered);

        // "doubled ... for an attack by Inexperienced (19.33) Personnel": a Conscript always; a Green unit stacked with a leader is not (A19.3).
        Assert.Equal(2, Complete(Attack([3, 3], firers: [Firer("ru-1", "defender-conscript-squad")])).Arithmetic!.ColumnShift);
        Assert.Equal(1, Complete(Attack([3, 3], firers: [Firer("us-1", "american-green-squad") with { Inexperienced = false }])).Arithmetic!.ColumnShift);
        Assert.Equal(2, Complete(Attack([3, 3], firers: [Firer("us-1", "american-green-squad") with { Inexperienced = true }])).Arithmetic!.ColumnShift);
    }

    [Fact]
    public void ResidualFpIsHeldToTwelveAfterItsReduction()
    {
        // A8.2 (p. 60): "half (up to a maximum of 12 after adjustment as per 8.26; FRD) of the highest IFT FP column used". Five 4-4-7 at PBF are
        // 40 FP on the 36 column: half is 18, the 16 column; one +1 of Hindrance lowers it to 12. It used to be 8.
        FireFirer[] five = [.. Enumerable.Range(1, 5).Select(index => Firer("ru-" + index))];
        var clear = Complete(Moving(Attack([6, 5], firers: five), ScenarioA1FireCalculator.FirstFire));
        Assert.Equal((36, 12), (clear.Arithmetic!.ColumnFp, clear.Arithmetic.ResidualFp));
        var hindered = Complete(Moving(Attack([6, 5], firers: five), ScenarioA1FireCalculator.FirstFire) with
        {
            Los = new FireLos(false, 1, true, false),
        });
        Assert.Equal(12, hindered.Arithmetic!.ResidualFp);

        // Lower columns are as they were: 16 FP leaves 8, and 6 with one +1.
        var two = Complete(Moving(Attack([6, 5]), ScenarioA1FireCalculator.FirstFire) with
        {
            Los = new FireLos(false, 1, true, false),
        });
        Assert.Equal(6, two.Arithmetic!.ResidualFp);
    }

    [Fact]
    public void ACxFirerLowersTheResidualFp()
    {
        // A8.26 (p. 60): "each positive DRM caused solely by conditions outside (including CX, BU, Stun, leadership modifier ...) the target hex".
        // The page's example: "If the 4-6-7 were CX the First Fire Attack would have a +1 DRM, reducing the squad's Residual FP".
        var fresh = Complete(Moving(Attack([6, 5], firers: [Firer("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.Equal(4, fresh.Arithmetic!.ResidualFp);
        var tired = Complete(Moving(Attack([6, 5], firers: [Firer("ru-1") with { Cx = true }]), ScenarioA1FireCalculator.FirstFire));
        Assert.Contains(tired.Arithmetic!.Drm, item => item.Name == "cx:ru-1");
        Assert.Equal(2, tired.Arithmetic.ResidualFp);
    }

    [Fact]
    public void AMgThatKeepsItsRofLeavesNoResidualFpOfItsOwn()
    {
        // A8.23 (p. 60), the page's example: "A 4-6-7 squad and a MMG make a 9 FP Defensive First Fire attack ... To retain his Multiple ROF for
        // the MMG, the German player opts to leave no Residual FP with the MG, but the squad still leaves two Residual FP". Here a 4-4-7 and a 4 FP
        // MMG at PBF: 16 FP. The colored dr of 1 keeps the ROF: the squad's 8 FP leave 4, and giving the ROF up would leave 8.
        var kept = Complete(Moving(Attack([1, 5], firers: [Gunner("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.True(Assert.Single(kept.WeaponEffects!).RateOfFireRetained);
        Assert.Equal((4, 8), (kept.Arithmetic!.ResidualFp, kept.Arithmetic.ResidualFpIfForfeited));

        // With the ROF lost the whole column counts, and there is nothing to give up.
        var lost = Complete(Moving(Attack([6, 2], firers: [Gunner("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.Equal((8, null), (lost.Arithmetic!.ResidualFp, lost.Arithmetic.ResidualFpIfForfeited));

        // A8.221: "Residual FP is not gained for an attack by a malfunctioning weapon". The MMG's B11 is rolled: the squad's FP alone.
        var broken = Complete(Moving(Attack([6, 5], firers: [Gunner("ru-1")]), ScenarioA1FireCalculator.FirstFire));
        Assert.True(Assert.Single(broken.WeaponEffects!).Malfunctioned);
        Assert.Equal((4, null), (broken.Arithmetic!.ResidualFp, broken.Arithmetic.ResidualFpIfForfeited));
    }

    [Fact]
    public void TheRofIsGivenUpOnlyAtOnceAfterTheAttack()
    {
        // A8.23: "The decision ... must be made immediately after resolving its attack."
        Assert.True(ScenarioA1FireFollowUps.RofForfeitOpen(true, true, 8, 1));
        Assert.False(ScenarioA1FireFollowUps.RofForfeitOpen(false, true, 8, 1));
        Assert.False(ScenarioA1FireFollowUps.RofForfeitOpen(true, false, 8, 1));
        Assert.False(ScenarioA1FireFollowUps.RofForfeitOpen(true, true, null, 1));
        Assert.False(ScenarioA1FireFollowUps.RofForfeitOpen(true, true, 8, 0));

        // The record's Residual FP is its own value, or the value with the ROF given up.
        Assert.Null(ScenarioA1FireFollowUps.VerifyResidualFp("mph", true, true, 4, 8, "fire", 4, 8));
        Assert.NotNull(ScenarioA1FireFollowUps.VerifyResidualFp("mph", true, true, 4, 8, "fire", 4));
        Assert.NotNull(ScenarioA1FireFollowUps.VerifyResidualFp("mph", true, true, 4, 6, "fire", 4, 8));

        // A cowering unit's other SW: its counter, a Final Fire counter replacing a First Fire one.
        Assert.Equal([(UnitCondition.FinalFire, true), (UnitCondition.FirstFire, false)], ScenarioA1FireFollowUps.CoweredWeaponConditions("final-fire", true));
        Assert.Equal([(UnitCondition.PrepFire, true)], ScenarioA1FireFollowUps.CoweredWeaponConditions("prep-fire", false));
    }

    [Fact]
    public void ASquadWithTheOptionSpraysToThreeHexesAndHalvesAgainAtLongRange()
    {
        // A7.34 (p. 56), the page's example: "The 6-2-8 squad ... attacks both hexes with Area Fire. Note, however, that C5 is attacked with three FP
        // while C4 is at Long Range so the FP must be halved again and attacks with only 1 1/2 FP".
        FireAttack Spray(string definition, int range) => Attack([6, 5], firers: [Firer("ru-1", definition)]) with
        {
            SprayingFire = true,
            Range = range,
        };
        Assert.Equal(3m, Complete(Spray("defender-guards-squad", 2)).Arithmetic!.TotalFirepower);
        var far = Complete(Spray("defender-guards-squad", 3)).Arithmetic!;
        Assert.Equal(1.5m, far.TotalFirepower);
        Assert.Contains(far.Firers.Single().Multipliers, item => item.Name == "long-range-fire");

        // "limited to a maximum range of three hexes"; and a squad with no underscore has no Spraying Fire of its own.
        Assert.Contains("asl.a1.fire.spraying-fire-outside", ScenarioA1FireCalculator.Resolve(Spray("defender-guards-squad", 4), Reference).Reasons);
        Assert.Contains("asl.a1.fire.spraying-fire-outside", ScenarioA1FireCalculator.Resolve(Spray("defender-squad", 2), Reference).Reasons);
    }

    [Fact]
    public void AGroupSpraysOnlyWhenEveryMemberCan()
    {
        // A9.52 (p. 64): "Spraying Fire cannot be used in a FG attack unless all members of the FG are capable of Spraying Fire". A 4-4-7 has no
        // option: with its MG it may not add its own FP (ruling R45.5, replacing R12.6's leave), but its MG sprays alone.
        FireAttack Spray(params FireFirer[] firers) => Attack([6, 5], firers: firers) with
        {
            SprayingFire = true,
            Range = 2,
        };
        Assert.Contains("asl.a1.fire.spraying-fire-outside", ScenarioA1FireCalculator.Resolve(Spray(Gunner("ru-1")), Reference).Reasons);
        Assert.Equal(FireResolution.Resolved, Complete(Spray(Gunner("ru-1") with { UsesInherentFp = false })).Disposition);

        // A squad with the option adds its FP to its MG's, and fires beside a MG fired alone.
        var guards = Firer("ru-2", "defender-guards-squad") with
        {
            Weapons = [Mg("ru-2-mg", "defender-lmg")],
            UsesSupportWeapon = true,
        };
        Assert.Equal(FireResolution.Resolved, Complete(Spray(guards, Gunner("ru-1") with { UsesInherentFp = false })).Disposition);

        // An attack recorded before the pass keeps ruling R12.6: the MG's holder adds its FP.
        Assert.Equal(FireResolution.Resolved, Complete(Spray(Gunner("ru-1")) with { CoweringMarksAndWeaponResidual = null }).Disposition);

        // A9.5: two Locations that share a hexside at one level, or two levels of one hex one level apart.
        Assert.Null(ScenarioA1FireEligibility.SprayTargetBar(true, false, true, true));
        Assert.Null(ScenarioA1FireEligibility.SprayTargetBar(true, false, false, false, true));
        Assert.NotNull(ScenarioA1FireEligibility.SprayTargetBar(true, false, false, true));
        Assert.NotNull(ScenarioA1FireEligibility.SprayTargetBar(true, true, true, true));
    }

    [Fact]
    public void AMmgInWoodsFiresAgainOnlyInsideTheCaOfItsFirstShot()
    {
        // A9.21 (p. 62): "if a SW MMG/HMG in a woods/rubble/building hex fires and is entitled to another shot, it may continue to fire during that
        // phase only inside the CA of the prior shot (which ... includes the MG's own Location ...)".
        Assert.Null(ScenarioA1FireEligibility.FieldOfFireBar("medium", "woods", [0], 20, "mmg"));
        Assert.StartsWith("play.fire-field", ScenarioA1FireEligibility.FieldOfFireBar("medium", "woods", [0], 90, "mmg"));
        Assert.StartsWith("play.fire-field", ScenarioA1FireEligibility.FieldOfFireBar("heavy", "stone-building", [10, 25], 310, "hmg"));
        Assert.Null(ScenarioA1FireEligibility.FieldOfFireBar("heavy", "stone-building", [10, 25], 30, "hmg"));

        // Not a LMG, not in other terrain, not a first shot, and always its own hex.
        Assert.Null(ScenarioA1FireEligibility.FieldOfFireBar("light", "woods", [0], 90, "lmg"));
        Assert.Null(ScenarioA1FireEligibility.FieldOfFireBar("medium", "open-ground", [0], 90, "mmg"));
        Assert.Null(ScenarioA1FireEligibility.FieldOfFireBar("medium", "woods", [], 90, "mmg"));
        Assert.Null(ScenarioA1FireEligibility.FieldOfFireBar("medium", "woods", [0], null, "mmg"));
    }

    [Fact]
    public void AMgFiresBeyondSixteenHexesOnlyWhenALeaderDirectsIt()
    {
        // A9.4 (p. 64): "MG fire is limited to a 16 hex maximum range unless an Infantry leader ... is directing that fire; moreover, MG attacks vs
        // unconcealed Infantry at a range >= 17 hexes treat those Infantry as concealed". The German HMG (7-16) at 20 hexes.
        var gunner = Firer("de-1", "attacker-squad") with
        {
            UsesInherentFp = false,
            Weapons = [Mg("de-hmg", "attacker-hmg")],
            UsesSupportWeapon = true,
        };
        var far = Attack([6, 5], firers: [gunner], targets: [Target("ru-s", "defender-squad")]) with
        {
            Range = 20,
        };
        Assert.Contains("asl.a1.fire.mg-direction-outside", ScenarioA1FireCalculator.Resolve(far, Reference).Reasons);
        var directed = Complete(far with
        {
            Director = new FireDirector("de-l", "attacker-leader-9-1", From, false, false, false, false, false),
        });
        Assert.Equal(FireResolution.Resolved, directed.Disposition);
        Assert.Contains(MultipliersOf(directed, "de-hmg"), item => item.Name == "mandatory-fire-direction" && item.Value == 0.5m);

        // At 16 hexes it needs no leader and is not halved.
        var near = Complete(far with { Range = 16 });
        Assert.DoesNotContain(MultipliersOf(near, "de-hmg"), item => item.Name == "mandatory-fire-direction");
    }

    [Fact]
    public void ALeaderDirectsTheLaterShotsOfAMgHeDirected()
    {
        // A7.53 (p. 57): "a leader can direct the fire of a MG as many times as the MG can fire, even if he also directed other units as part of a
        // FG in the MG's previous attack."
        var leader = new FireDirector("ru-l", "defender-leader", From, false, false, false, true, false);
        var mgAlone = Gunner("ru-1") with { UsesInherentFp = false };
        Assert.Contains("asl.a1.fire.director-outside", ScenarioA1FireCalculator.Resolve(Attack([6, 5], firers: [mgAlone], director: leader), Reference).Reasons);
        var again = leader with { DirectedTheseMgs = true };
        Assert.Equal(FireResolution.Resolved, Complete(Attack([6, 5], firers: [mgAlone], director: again)).Disposition);

        // Not when the squad's own FP fires with it: that is a second unit directed in the phase.
        Assert.Contains("asl.a1.fire.director-outside", ScenarioA1FireCalculator.Resolve(Attack([6, 5], firers: [Gunner("ru-1")], director: again), Reference).Reasons);
    }

    [Fact]
    public void AnAttackAtItsOwnLocationCancelsTheMgsFireLane()
    {
        // A9.223 (p. 63): the lane is cancelled "only if a TPBF ... situation occurs (8.312 ...) in which case the Fire Lane must be cancelled".
        var withLane = Moving(Attack([6, 5], firers: [Gunner("ru-1")]), ScenarioA1FireCalculator.SubsequentFirstFire);
        (string Weapon, string Operator)[] lanes = [("ru-1-mg", "ru-1")];
        Assert.StartsWith("play.fire-lane-mg", ScenarioA1FireEligibility.FireLaneInUseBar(lanes, withLane));
        Assert.Null(ScenarioA1FireEligibility.FireLaneInUseBar(lanes, withLane with { TargetLocationId = From }));
        Assert.True(ScenarioA1FireEligibility.FireLaneCancelledBy(true, true));
        Assert.False(ScenarioA1FireEligibility.FireLaneCancelledBy(false, true));
        Assert.False(ScenarioA1FireEligibility.FireLaneCancelledBy(true, false));
    }

    [Fact]
    public void ALoweredBreakdownNumberMakesThePrintedOneAnXNumber()
    {
        // A.11 (p. 43), the page's example: "A German LMG with a malfunction rating of B12 uses Sustained Fire. During that attack it has a
        // malfunction rating of B10 and X12. If its Original IFT DR is 10 or 11 the weapon malfunctions; if it is a 12 the weapon is permanently
        // removed." The Russian LMG is B11: in Sustained Fire B9 and X11.
        var squad = Firer("ru-1") with
        {
            Weapons = [Mg("ru-mg") with { FirstFireMarked = true }],
            UsesSupportWeapon = true,
        };
        FireWeaponEffect On(int one, int two) => Assert.Single(Complete(Moving(Attack([one, two], firers: [squad]), ScenarioA1FireCalculator.SubsequentFirstFire)).WeaponEffects!);
        Assert.Equal((true, null), (On(6, 4).Malfunctioned, On(6, 4).Removed));
        Assert.Equal((true, true), (On(6, 5).Malfunctioned, On(6, 5).Removed));
        Assert.Equal((false, null), (On(5, 3).Malfunctioned, On(5, 3).Removed));

        // Fired normally its B# is not lowered, so a DR of 11 malfunctions it and removes nothing.
        var plain = Assert.Single(Complete(Moving(Attack([6, 5], firers: [Gunner("ru-1", "defender-lmg")]), ScenarioA1FireCalculator.FirstFire)).WeaponEffects!);
        Assert.Equal((true, null), (plain.Malfunctioned, plain.Removed));
    }

    [Fact]
    public void ALoneSmcsMgUsesNoSustainedFire()
    {
        // A9.3 (p. 63): "Sustained Fire cannot be used ... by a MG fired by a lone SMC."
        var leader = Firer("ru-l", "defender-leader") with
        {
            UsesInherentFp = false,
            Weapons = [Mg("ru-mg") with { FirstFireMarked = true }],
            UsesSupportWeapon = true,
        };
        var alone = ScenarioA1FireCalculator.Resolve(Moving(Attack([6, 4], firers: [leader]), ScenarioA1FireCalculator.SubsequentFirstFire), Reference);
        Assert.Contains("asl.a1.fire.weapon-outside", alone.Reasons);
    }

    [Fact]
    public void TheWeaponsTheRuleNamesAreDismantledAndAGermanOneFiresAsALmg()
    {
        // A9.8 (p. 65): "Any light mortar of >= four PP, 76-82mm mortar, or non-Russian HMG/MMG ... may have its PP halved (FRU) if in a dismantled
        // state"; "A dm SW may not be fired until reassembled [EXC: a German dm HMG/MMG may be fired as a German LMG]".
        Assert.True(ScenarioA1FireEligibility.Dismantlable("asl:mg", "german", "medium", 4));
        Assert.True(ScenarioA1FireEligibility.Dismantlable("asl:mg", "german", "heavy", 5));
        Assert.False(ScenarioA1FireEligibility.Dismantlable("asl:mg", "russian", "heavy", 5));
        Assert.False(ScenarioA1FireEligibility.Dismantlable("asl:mg", "german", "light", 1));
        Assert.True(ScenarioA1FireEligibility.Dismantlable("asl:light-mortar", "russian", null, 4));
        Assert.False(ScenarioA1FireEligibility.Dismantlable("asl:light-mortar", "german", null, 3));
        Assert.True(ScenarioA1FireEligibility.DismantledFiresAsLmg(true, "german", "heavy"));
        Assert.False(ScenarioA1FireEligibility.DismantledFiresAsLmg(false, "german", "heavy"));
        Assert.False(ScenarioA1FireEligibility.DismantledFiresAsLmg(true, "british", "medium"));

        // A9.72: "A captured SW/Gun may never be repaired unless recaptured by its original side."
        Assert.StartsWith("play.repair-weapon", ScenarioA1RallyRules.SwRepairCapturedBar("mg", true));
        Assert.Null(ScenarioA1RallyRules.SwRepairCapturedBar("mg", false));
    }

    [Fact]
    public void ACxLeaderLendsNoIpcAndATiUnitActsWhereNoMfIsSpent()
    {
        // A4.52 (p. 51): "CX Infantry have an IPC one < normal." A leader's IPC is one, so in Double Time he has none to lend (A4.42).
        MovingUnitFacts[] stack =
        [
            new("s1", "russian", false, true, false, false, false, false, "russian", 0, false, null),
            new("l", "russian", false, false, true, false, false, false, "russian", 0, false, null),
        ];
        Assert.Equal(("s1", "l"), ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, index => index == 0));
        Assert.Equal((null, null), ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, index => index == 0, doubleTime: true));

        // A.7, A4.8 (ruling R45.6): a TI unit is in Good Order; it "may not move, advance, fire, or perform other labor tasks".
        Assert.True(ScenarioA1Definitions.FreeToAct(true, true, spendsMf: false));
        Assert.False(ScenarioA1Definitions.FreeToAct(true, true, spendsMf: true));
        Assert.True(ScenarioA1Definitions.FreeToAct(true, false, spendsMf: true));
        Assert.False(ScenarioA1Definitions.FreeToAct(false, false, spendsMf: false));
    }

    [Fact]
    public void TheReviewsFixesOfTheMixedGroup()
    {
        // The example of p. 62: a squad marked First Fire fires its own FP in one group with a crew that fires its LMG alone. The attack is
        // Subsequent First Fire, though a firer fires a MG alone (the first review's blocker).
        IReadOnlyList<(bool FinalFire, bool FirstFire)> marks = [(false, true), (false, false)];
        Assert.Equal(ScenarioA1FireCalculator.SubsequentFirstFire,
            ScenarioA1FireEligibility.MphFireKind("mph", new FireKindFacts(false, false, () => false, () => marks, false, true, EveryFirstFireMarkedFiresAlone: false)).Kind);

        // A MG fired again alone on its ROF by the marked unit itself is First Fire still (A9.2).
        IReadOnlyList<(bool FinalFire, bool FirstFire)> alone = [(false, true)];
        Assert.Equal(ScenarioA1FireCalculator.FirstFire,
            ScenarioA1FireEligibility.MphFireKind("mph", new FireKindFacts(false, false, () => false, () => alone, false, true, EveryFirstFireMarkedFiresAlone: true)).Kind);

        // A8.3 binds "a unit [that] uses Subsequent First Fire" to every MG it possesses; a unit with no counter beside it is free.
        var marked = Firer("ru-1") with { FirstFireMarked = true, FiredThisPlayerTurn = true };
        var mixed = Moving(Attack([6, 5], firers: [marked, Firer("ru-2")]), ScenarioA1FireCalculator.SubsequentFirstFire);
        Assert.Null(ScenarioA1FireEligibility.EveryMgBar(mixed, id => id == "ru-2" ? ["ru-2-mg"] : []));
        Assert.StartsWith("play.fire-weapons", ScenarioA1FireEligibility.EveryMgBar(mixed, id => id == "ru-1" ? ["ru-1-mg"] : []));

        // A7.53: a leader who has directed does not direct a group that holds a unit with no counter (the example: "leader direction is NA").
        var leader = new FireDirector("ru-l", "defender-leader", From, false, false, false, true, false);
        Assert.Contains("asl.a1.fire.director-outside", ScenarioA1FireCalculator.Resolve(mixed with { Director = leader }, Reference).Reasons);
        var allMarked = Moving(Attack([6, 5], firers: [marked, Firer("ru-2") with { FirstFireMarked = true, FiredThisPlayerTurn = true }], director: leader),
            ScenarioA1FireCalculator.SubsequentFirstFire);
        Assert.Equal(FireResolution.Resolved, Complete(allMarked).Disposition);

        // A7.353: a crew with no counter whose Gun fired has no FP in the group's First Fire; marked Final Fire it uses FPF.
        var crew = Firer("ru-c", "defender-crew") with { GunFired = true };
        var squad = Firer("ru-1") with { FinalFireMarked = true, FiredThisPlayerTurn = true };
        Assert.Contains("asl.a1.fire.firer-outside",
            ScenarioA1FireCalculator.Resolve(Moving(Attack([3, 4], firers: [squad, crew]), ScenarioA1FireCalculator.FinalProtectiveFire), Reference).Reasons);
        Assert.Equal(FireResolution.Resolved, Complete(Moving(Attack([3, 4], firers: [squad, crew with { FinalFireMarked = true, FiredThisPlayerTurn = true }]),
            ScenarioA1FireCalculator.FinalProtectiveFire)).Disposition);
    }

    [Fact]
    public void TheReviewsFixesOfTheWeapons()
    {
        // A.11 lists the causes that make an X#; Extreme Winter lowers the B# and the X# alike (E3.741) and makes none.
        var winter = Assert.Single(Complete(Moving(Attack([6, 6], firers: [Gunner("ru-1", "defender-lmg"), Firer("ru-2") with { Fanatic = true }]),
            ScenarioA1FireCalculator.FirstFire) with { BreakdownReduction = 1 }).WeaponEffects!);
        Assert.Equal((true, null), (winter.Malfunctioned, winter.Removed));

        // A9.3: a lone hero, as a lone leader, uses no Sustained Fire.
        var hero = Firer("ru-h", "defender-hero") with
        {
            UsesInherentFp = false,
            Weapons = [Mg("ru-mg") with { FirstFireMarked = true }],
            UsesSupportWeapon = true,
            FirstFireMarked = true,
        };
        Assert.Contains("asl.a1.fire.weapon-outside",
            ScenarioA1FireCalculator.Resolve(Moving(Attack([6, 4], firers: [hero]), ScenarioA1FireCalculator.SubsequentFirstFire), Reference).Reasons);

        // A9.52, A7.352: a HS that fires a MG adds no FP of its own, so it sprays with the MG as it did before the pass.
        var halfSquad = Firer("ru-hs", "defender-half-squad") with
        {
            Weapons = [Mg("ru-mg")],
            UsesSupportWeapon = true,
        };
        Assert.Equal(FireResolution.Resolved, Complete(Attack([6, 5], firers: [halfSquad]) with { SprayingFire = true, Range = 2 }).Disposition);

        // C6.44: a "?" stack of Dummies is Infantry to the firer, so Prep Fire at it takes no Bore Sighting DRM either.
        var gunner = Gunner("ru-1") with { UsesInherentFp = false };
        var dummies = Complete(Attack([6, 5], firers: [gunner], targets: [new FireTarget("de-q", null, At, false, false, true, false, true, false, false)]) with { MgBoreSighted = true });
        Assert.DoesNotContain(dummies.Arithmetic!.Drm, item => item.Name == "bore-sighting");

        // The ROF is given up only for a larger counter than the one that stands there (A8.21), and a Sniper Check written with the attack leaves
        // the choice open.
        Assert.False(ScenarioA1FireFollowUps.RofForfeitOpen(true, true, 4, 1, standingFp: 4));
        Assert.True(ScenarioA1FireFollowUps.RofForfeitOpen(true, true, 4, 1, standingFp: 2));
        Assert.True(ScenarioA1FireFollowUps.PartOfTheAttack(false, true));
        Assert.False(ScenarioA1FireFollowUps.PartOfTheAttack(false, false));
        Assert.True(ScenarioA1FireFollowUps.StillKeepsRof(true, false, false, false));
        Assert.False(ScenarioA1FireFollowUps.StillKeepsRof(true, true, false, false));

        // A8.22 is per unit: a stack one of whose units was not yet attacked there is attacked.
        IReadOnlyList<(int Fp, int Drm)>[] byUnit = [[(4, 0)], []];
        Assert.True(ScenarioA1FireFollowUps.ResidualAttacksAgain(4, 0, byUnit));
        Assert.False(ScenarioA1FireFollowUps.ResidualAttacksAgain(4, 0, new IReadOnlyList<(int Fp, int Drm)>[] { [(4, 0)], [(4, 0)] }));

        // A FT, never raised for PBF, fires at the next level of its hex at its own FP; A9.8: a dismantled mortar is not fired.
        Assert.Equal(FireRangeBand.Normal, FireRange.Band(0, sameLocation: false, 1, levelAbove: 1, noPointBlank: true).Band);
        Assert.StartsWith("play.ordnance-dismantled", ScenarioA1FireEligibility.DismantledShotBar(true, "mortar"));
        Assert.Null(ScenarioA1FireEligibility.DismantledShotBar(false, "mortar"));

        // A4.52: a fresh leader of the stack lends his IPC though another leader is CX.
        MovingUnitFacts[] stack =
        [
            new("s1", "russian", false, true, false, false, false, false, "russian", 0, false, null),
            new("l1", "russian", false, false, true, false, false, false, "russian", 0, false, null, Cx: true),
            new("l2", "russian", false, false, true, false, false, false, "russian", 0, false, null),
        ];
        Assert.Equal(("s1", "l2"), ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, index => index == 0));
    }

    [Fact]
    public void ResidualFpAttacksAUnitOnceALocationUnlessItGrewOrTheDrmFell()
    {
        // A8.22 (p. 60): "A unit can be attacked by Residual FP only once per Location [EXC: if, since that first Residual FP attack, the Residual
        // FP has increased in strength or the unit is subject to more-negative-DRM/less-positive-DRM ...]".
        Assert.True(ScenarioA1FireFollowUps.ResidualAttacksAgain(4, 0, Array.Empty<(int Fp, int Drm)>()));
        Assert.False(ScenarioA1FireFollowUps.ResidualAttacksAgain(4, 0, [(4, 0)]));
        Assert.False(ScenarioA1FireFollowUps.ResidualAttacksAgain(4, 1, [(4, 0)]));
        Assert.True(ScenarioA1FireFollowUps.ResidualAttacksAgain(6, 0, [(4, 0)]));
        Assert.True(ScenarioA1FireFollowUps.ResidualAttacksAgain(4, -1, [(4, 0)]));
        Assert.False(ScenarioA1FireFollowUps.ResidualAttacksAgain(6, 0, [(4, 0), (6, 0)]));
    }

    /// <summary>A building for the A7.72 search: steps between numbered Locations, which of them are at ground level, and which the enemy holds.</summary>
    private sealed class Building(Dictionary<int, int[]> steps, int[] ground, int[] held, PathUnitFacts? holder = null) : IUpperLevelPathReader
    {
        public IReadOnlyList<int> StepsFrom(int location) => steps.GetValueOrDefault(location, []);

        public bool GroundLevel(int location) => ground.Contains(location);

        public IReadOnlyList<PathUnitFacts> UnitsAt(int location) => held.Contains(location) ? [holder ?? Enemy] : [];
    }

    private static readonly PathUnitFacts Enemy = new(true, true, false, false, false, false, false, false);

    private static readonly UpperLevelUnitFacts Upstairs = new(true, false, false, false, 2, true, false, false);

    [Fact]
    public void AUnitUpstairsWithNoPathToGroundLevelIsEncircled()
    {
        // A7.72 (p. 58): a unit in an upper level that "cannot trace a path free of an unbroken, armed, unconcealed enemy unit ... to ground level".
        // Location 0 is level 2, 1 is level 1, 2 is ground level, all of one stairwell hex; 3 is level 1 of the next hex and 4 its ground level.
        var steps = new Dictionary<int, int[]> { [0] = [1], [1] = [0, 2, 3], [3] = [1, 4] };
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs, 0, new Building(steps, [2, 4], [])));

        // The enemy at level 1 cuts the unit at level 2 off.
        Assert.True(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs, 0, new Building(steps, [2, 4], [1])));

        // From level 1, the enemy at ground level of its own hex leaves the way through the next hex; held too, there is none.
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Level = 1 }, 1, new Building(steps, [2, 4], [2])));
        Assert.True(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Level = 1 }, 1, new Building(steps, [2, 4], [2, 4])));

        // Never a berserk or heroic unit, a vehicle, a unit aboard one or in Bypass, or a unit that is not upstairs in a building.
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Berserk = true }, 0, new Building(steps, [2, 4], [1])));
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Heroic = true }, 0, new Building(steps, [2, 4], [1])));
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Building = false }, 0, new Building(steps, [2, 4], [1])));
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Level = 0 }, 0, new Building(steps, [2, 4], [1])));
        Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs with { Aboard = true }, 0, new Building(steps, [2, 4], [1])));

        // Only "an unbroken, armed, unconcealed enemy unit" holds a Location.
        Assert.True(ScenarioA1UpperLevelEncirclement.Holds(Enemy));
        foreach (var free in new[] { Enemy with { Broken = true }, Enemy with { Unarmed = true }, Enemy with { Concealed = true }, Enemy with { Hidden = true },
            Enemy with { Dummy = true }, Enemy with { Captured = true }, Enemy with { Enemy = false }, Enemy with { Active = false } })
        {
            Assert.False(ScenarioA1UpperLevelEncirclement.Holds(free));
            Assert.False(ScenarioA1UpperLevelEncirclement.Encircled(Upstairs, 0, new Building(steps, [2, 4], [1], free)));
        }
    }

    [Fact]
    public void CasualtyReductionEliminatesACrew()
    {
        // A7.302 (p. 55): "Casualty Reduction eliminates any HS or crew". Two 4-4-7 at PBF, 16 FP; DR 4 is a K/3, and the crew is alone at its Gun
        // (ruling R8.3 reviews a crew as a target only so).
        var result = Complete(Attack([1, 3], targets: [Target("de-c", "attacker-crew")]) with
        {
            GunTarget = new FireGunTarget("de-gun", "attacker-inf-gun", "de-c", false, false),
        });
        Assert.True(result.Disposition == FireResolution.Resolved, string.Join(" | ", result.Reasons));
        Assert.Equal((FireResolution.Resolved, "K/3"), (result.Disposition, result.Arithmetic!.Result));
        Assert.True(Assert.Single(result.Effects).Eliminated);
    }

    [Fact]
    public void OrdnanceIsHeldToItsOwnLocationByAKnownEnemyThere()
    {
        // A7.212 (p. 55): "A unit does not have the freedom to attack units in other Locations while its own Location is occupied by a Known enemy
        // unit". A Gun's, a mortar's, or a LATW's shot is read as IFT fire is.
        FirerLocationFacts Facts(bool direct, bool ordnance, bool isTarget, bool enemy) =>
            new("bd01:F5:0", true, "open-ground", direct, isTarget, false, "PFPh", () => enemy, () => false, null, ordnance);
        Assert.StartsWith("play.fire-target-limit", ScenarioA1FireMapRules.FirerLocation(Facts(false, true, false, true)).Refusal);
        Assert.StartsWith("play.fire-target-limit", ScenarioA1FireMapRules.FirerLocation(Facts(true, false, false, true)).Refusal);
        Assert.Null(ScenarioA1FireMapRules.FirerLocation(Facts(false, true, false, false)).Refusal);
        Assert.DoesNotContain("fire-target-limit", ScenarioA1FireMapRules.FirerLocation(Facts(false, true, true, true)).Refusal ?? string.Empty, StringComparison.Ordinal);

        // Residual FP has no firer's Location to hold it.
        Assert.Null(ScenarioA1FireMapRules.FirerLocation(Facts(false, false, false, true)).Refusal);
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
