using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The D5.34 claim's Encirclement increment (A7.7 p. 57, A7.71 and A7.72 p. 58, A10.532 p. 67, B30.32 p. 150; the claim's design, rulings RD5.7 to
/// RD5.13, RD5.15, RD5.16): the Encirclement calculator's checks, the forwards of the old members (each equal to a copy of its old expression over its
/// inputs), the readings the claim's fields turn on (null as before; set as the rule), the members that keep their answers, and the two contracts.
/// </summary>
public sealed class ScenarioA1EncirclementCalculatorTests
{
    private static readonly ScenarioA1OrdnanceReference Ordnance = new ScenarioA1OrdnancePackage().Reference;
    private static readonly ScenarioA1FireReference Fire = new ScenarioA1FirePackage().Reference;
    private const string At = "bd01:G5:0";
    private const string From = "bd01:F5:0";
    private const string Bren = "campaign-british-bren-carrier";
    private static readonly bool[] Both = [false, true];
    private static readonly bool?[] Three = [null, false, true];
    private static readonly RuleState[] States = [RuleState.Unknown, RuleState.False, RuleState.True];
    private static readonly string[] Kinds = ["asl:squad", "asl:leader", "asl:hero", "asl:vehicle", "asl:crew"];

    // The geometry (A7.7 a, b, c; above and below; A7.71).

    [Fact]
    public void EachGeometryCaseEncirclesAndItsNeighboursDoNot()
    {
        // a) opposite hexspines: the hexspine after hexside i is 2i + 2.
        Assert.True(ScenarioA1EncirclementCalculator.Encircles([2, 8]));
        Assert.True(ScenarioA1EncirclementCalculator.Encircles([4, 10]));
        Assert.False(ScenarioA1EncirclementCalculator.Encircles([2, 6]));

        // b) three vertices between them both ways: opposite hexsides.
        Assert.True(ScenarioA1EncirclementCalculator.Encircles([1, 7]));
        Assert.False(ScenarioA1EncirclementCalculator.Encircles([1, 5]));

        // c) three non-contiguous hexsides; three contiguous ones do not.
        Assert.True(ScenarioA1EncirclementCalculator.Encircles([1, 5, 9]));
        Assert.True(ScenarioA1EncirclementCalculator.Encircles([3, 7, 11]));
        Assert.False(ScenarioA1EncirclementCalculator.Encircles([1, 3, 5]));

        // A LOF from directly above and directly below in a building; either alone does not.
        Assert.True(ScenarioA1EncirclementCalculator.Encircles([ScenarioA1EncirclementCalculator.FromAbove, ScenarioA1EncirclementCalculator.FromBelow]));
        Assert.False(ScenarioA1EncirclementCalculator.Encircles([ScenarioA1EncirclementCalculator.FromAbove, 1]));
        Assert.False(ScenarioA1EncirclementCalculator.Encircles([]));
    }

    [Fact]
    public void TheA77ExamplesEncircleT3FromU2AndS5()
    {
        // A7.7 EX (p. 58), board 3: a unit in [U2] fires on [T3], its LOS entering along the hexspine between the N and NE hexsides (sides 0 and 1);
        // the next attack from [S5] enters along the [S4]-[T4] hexspine (sides 3 and 4), "through both the S4-T3 and T4-T3 hexsides": Encircled. Through
        // the [S4]-[T3] hexside alone (side 4): Not Encircled.
        var fromU2 = ScenarioA1FireMapRules.PerimeterPosition([0, 1])!.Value;
        var fromS5 = ScenarioA1FireMapRules.PerimeterPosition([3, 4])!.Value;
        var s4Side = ScenarioA1FireMapRules.PerimeterPosition([4])!.Value;
        Assert.True(ScenarioA1EncirclementCalculator.Seals(2, [fromU2, fromS5]));
        Assert.False(ScenarioA1EncirclementCalculator.Seals(2, [fromU2, s4Side]));
    }

    [Fact]
    public void OneFireGroupMayEncircleAloneAndOneUnitNever()
    {
        // A7.71 EX: the four-hex FG in [T3]-[U4]-[U5]-[T5] fires on [T4] through four hexsides: two pairs of opposite hexsides.
        int[] fg = [ScenarioA1FireMapRules.PerimeterPosition([0])!.Value, ScenarioA1FireMapRules.PerimeterPosition([1])!.Value,
            ScenarioA1FireMapRules.PerimeterPosition([2])!.Value, ScenarioA1FireMapRules.PerimeterPosition([3])!.Value];
        Assert.True(ScenarioA1EncirclementCalculator.Seals(4, fg));

        // "two or more non-Aerial units": one unit's entries never seal, whatever they are.
        Assert.False(ScenarioA1EncirclementCalculator.Seals(1, [2, 8]));
        Assert.False(ScenarioA1EncirclementCalculator.Seals(0, [1, 5, 9]));
    }

    [Fact]
    public void GeometryEncirclesForwardsWithItsOldAnswerOnEverySetOfEntries()
    {
        for (var mask = 0; mask < 1 << 14; mask++)
        {
            var positions = Enumerable.Range(0, 14).Where(bit => (mask & (1 << bit)) != 0).ToArray();
            Assert.Equal(OldEncircles(positions), ScenarioA1Geometry.Encircles(positions));
            Assert.Equal(OldEncircles(positions), ScenarioA1EncirclementCalculator.Encircles(positions));
            Assert.Equal(positions.Length >= 2 && OldEncircles(positions), ScenarioA1EncirclementCalculator.Seals(positions.Length, positions));
        }
    }

    // A copy of ScenarioA1Geometry.Encircles as it was before the claim.
    private static bool OldEncircles(IReadOnlyCollection<int> positions)
    {
        var set = positions.ToHashSet();
        if (set.Contains(12) && set.Contains(13))
        {
            return true;
        }

        if (set.Any(position => position < 12 && set.Contains((position + 6) % 12)))
        {
            return true;
        }

        var sides = set.Where(position => position < 12 && position % 2 == 1).Select(position => (position - 1) / 2).ToHashSet();
        return sides.Any(side => sides.Contains((side + 2) % 6) && sides.Contains((side + 4) % 6));
    }

    // Who may be Encircled (A7.7, B30.32).

    [Fact]
    public void TheSubjectIsNonBerserkNonHeroicInfantryOutsideAPillboxOrTheVulnerablePrcOfAnImmobileVehicle()
    {
        Assert.True(ScenarioA1EncirclementCalculator.Subject(new EncirclementSubjectFacts("asl:squad", false, false, false, false, false, null)));
        Assert.False(ScenarioA1EncirclementCalculator.Subject(new EncirclementSubjectFacts("asl:squad", false, true, false, false, false, null)));
        Assert.False(ScenarioA1EncirclementCalculator.Subject(new EncirclementSubjectFacts("asl:leader", false, false, true, false, false, null)));
        Assert.False(ScenarioA1EncirclementCalculator.Subject(new EncirclementSubjectFacts("asl:hero", false, false, false, false, false, null)));
        Assert.False(ScenarioA1EncirclementCalculator.Subject(new EncirclementSubjectFacts("asl:vehicle", false, false, false, false, false, null)));

        // B30.32: "Neither a pillbox nor its contents can be Encircled".
        Assert.False(ScenarioA1EncirclementCalculator.Subject(new EncirclementSubjectFacts("asl:squad", false, false, false, true, false, null)));

        // A7.7: aboard a vehicle, only the Vulnerable PRC of an Immobile vehicle.
        Assert.True(ScenarioA1EncirclementCalculator.Subject(new EncirclementSubjectFacts("asl:squad", false, false, false, false, true, true)));
        Assert.False(ScenarioA1EncirclementCalculator.Subject(new EncirclementSubjectFacts("asl:squad", false, false, false, false, true, false)));
        Assert.False(ScenarioA1EncirclementCalculator.Subject(new EncirclementSubjectFacts("asl:squad", false, false, false, false, true, null)));

        // A7.7: "any non-Aerial Infantry"; null Aerial reads as before.
        var infantry = new EncirclementSubjectFacts("asl:squad", false, false, false, false, false, null);
        Assert.True(ScenarioA1EncirclementCalculator.Subject(infantry with
        {
            Aerial = false
        }));
        Assert.False(ScenarioA1EncirclementCalculator.Subject(infantry with
        {
            Aerial = true
        }));

        // Under the marker of its side, or in Melee under any marker.
        var squad = new EncirclementSubjectFacts("asl:squad", false, false, false, null, null, null);
        Assert.True(ScenarioA1EncirclementCalculator.Encircled(true, true, true, squad));
        Assert.False(ScenarioA1EncirclementCalculator.Encircled(true, false, true, squad));
        Assert.True(ScenarioA1EncirclementCalculator.Encircled(true, false, true, squad with
        {
            Melee = true
        }));
        Assert.False(ScenarioA1EncirclementCalculator.Encircled(false, true, true, squad));
        Assert.False(ScenarioA1EncirclementCalculator.Encircled(true, true, true, squad with
        {
            InPillbox = true
        }));
    }

    [Fact]
    public void DefinitionsEncircledForwardsWithItsOldAnswerOnEveryInput()
    {
        foreach (var onMap in Both)
        {
            foreach (var side in Both)
            {
                foreach (var any in Both)
                {
                    foreach (var kind in Kinds)
                    {
                        foreach (var melee in States)
                        {
                            foreach (var berserk in States)
                            {
                                foreach (var heroic in States)
                                {
                                    // A copy of ScenarioA1Definitions.Encircled as it was before the claim.
                                    var old = onMap && (side || (any && melee == RuleState.True))
                                        && berserk != RuleState.True && heroic != RuleState.True && kind != "asl:hero" && kind != "asl:vehicle";
                                    Assert.Equal(old, ScenarioA1Definitions.Encircled(onMap, side, any, kind, melee, berserk, heroic));
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Valid fire (A7.7, A10.532; rulings RD5.8, RD5.9, RD5.15).

    private static EncirclementFireFacts Fired(EncirclementFireKind kind, int? range = 4, int? normal = 6, bool? hit = null, bool? nmc = true, string phase = "PFPh") =>
        new(phase, kind, range, normal, hit, nmc, false);

    [Fact]
    public void ValidFireIsInherentFpASwOrdnanceOrVehicularArmamentAtNormalRange()
    {
        Assert.True(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp)));
        Assert.True(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.SupportWeapon)));
        Assert.True(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.VehicularArmament, normal: 12)));
        Assert.True(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.Ordnance, normal: 12, hit: true, nmc: null)));

        // Ordnance must secure a hit; other fire must be able to inflict a NMC.
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.Ordnance, normal: 12, hit: false)));
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.Ordnance, normal: 12, hit: null)));
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, nmc: false)));
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, nmc: null)));

        // Range 0, beyond Normal Range, and beyond 16 hexes (A10.532); an unknown range or Normal Range.
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, range: 0)));
        Assert.True(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, range: 6)));
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, range: 7)));
        Assert.True(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.Ordnance, range: 16, normal: 24, hit: true)));
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.Ordnance, range: 17, normal: 24, hit: true)));
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, range: null)));
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, normal: null)));

        // A squad firing inherent FP and a LMG at 5 hexes counts at the LMG's Normal Range (ruling RD5.9): the caller gives the longest.
        Assert.True(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.SupportWeapon, range: 5, normal: Math.Max(4, 6))));

        // Not the MPh; Residual FP and other fire never; an Aerial unit never.
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, phase: "MPh")));
        Assert.True(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, phase: "dfph")));
        Assert.True(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, phase: "AFPh")));
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.Residual)));
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.Other)));
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp) with
        {
            Aerial = true
        }));

        // A LOF from directly above or below, at range 0.
        Assert.True(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, range: 0) with
        {
            AboveOrBelow = true
        }));
        Assert.False(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.InherentFp, range: 1) with
        {
            AboveOrBelow = true
        }));
    }

    [Fact]
    public void TheSealIsRefusedOutsideAFirePhaseAfterABrokenSequenceAndInAPillbox()
    {
        Assert.Null(ScenarioA1EncirclementCalculator.SealRefusal("PFPh", true, false));
        Assert.Contains("(A7.7)", ScenarioA1EncirclementCalculator.SealRefusal("MPh", true, false), StringComparison.Ordinal);
        Assert.Contains("consecutive", ScenarioA1EncirclementCalculator.SealRefusal("DFPh", false, false), StringComparison.Ordinal);
        Assert.Contains("(B30.32)", ScenarioA1EncirclementCalculator.SealRefusal("AFPh", true, true), StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1EncirclementCalculator.SealRefusal("AFPh", null, false));
        Assert.Equal("Whether the target is in a pillbox is not known (B30.32)", ScenarioA1EncirclementCalculator.SealRefusal("AFPh", true, null));
    }

    // The effects (A7.7) and the old members that forward to them.

    [Fact]
    public void TheMoraleDropForwardsWithTheOldAnswersOfInitialMoraleAndMoraleLevel()
    {
        Assert.Equal(1, ScenarioA1EncirclementCalculator.MoraleDrop(true, false, false));
        Assert.Equal(0, ScenarioA1EncirclementCalculator.MoraleDrop(true, true, false));
        Assert.Equal(0, ScenarioA1EncirclementCalculator.MoraleDrop(true, false, true));
        Assert.Equal(0, ScenarioA1EncirclementCalculator.MoraleDrop(false, false, false));
        foreach (var encircled in Three)
        {
            foreach (var targetBerserk in Three)
            {
                foreach (var targetHeroic in Three)
                {
                    // Copies of TargetState's old private Encircles, as InitialMorale and MoraleLevel read it.
                    var old = encircled == true && targetBerserk != true && targetHeroic != true;
                    Assert.Equal(old ? 1 : 0, ScenarioA1EncirclementCalculator.MoraleDrop(encircled == true, targetBerserk == true, targetHeroic == true));
                    foreach (var berserk in Both)
                    {
                        foreach (var heroic in Both)
                        {
                            Assert.Equal(old && !berserk && !heroic ? 1 : 0,
                                ScenarioA1EncirclementCalculator.MoraleDrop(encircled == true, targetBerserk == true || berserk, targetHeroic == true || heroic));
                        }
                    }
                }
            }
        }
    }

    private static FireFirer Firer(string id) => new(id, "defender-squad", From, false, false, false, false, false);

    private static FireTarget Target(string id) => new(id, "attacker-squad", At, false, false, false, false, false, false, false);

    private static FireAttack Attack(int[] dice, FireFirer[]? firers = null, FireTarget[]? targets = null) =>
        new("PFPh", "phasing", true, From, At, firers ?? [Firer("ru-1"), Firer("ru-2")], null, 1, true, new FireLos(false, 0, true, false), null,
            "open-ground", targets ?? [Target("de-s")], 3, new FireRolls(dice, null, null, null));

    [Fact]
    public void TheIftPlusOneAndTheTargetsMoraleDropKeepTheirAnswers()
    {
        // The pass 12 case (ruling R12.11): "encircled:" + the first Encircled firer, 1, once however many are.
        var firing = ScenarioA1FireCalculator.Preview(Attack([6, 4], firers: [Firer("ru-1") with { Encircled = true }, Firer("ru-2") with { Encircled = true }]), Fire);
        Assert.Single(firing!.Drm, item => item.Name.StartsWith("encircled:", StringComparison.Ordinal));
        Assert.Contains(firing.Drm, item => item is { Name: "encircled:ru-1", Value: 1m, Rule: "A7.7" });
        var plain = ScenarioA1FireCalculator.Preview(Attack([6, 4]), Fire);
        Assert.DoesNotContain(plain!.Drm, item => item.Name.StartsWith("encircled:", StringComparison.Ordinal));

        // 16 FP, 6 + 4 = 10: a NMC against Morale Level 6 instead of 7.
        static FireAttack Checked(FireTarget target) => Attack([6, 4], targets: [target]) with
        {
            Rolls = new FireRolls([6, 4], null, new Dictionary<string, IReadOnlyList<int>> { ["de-s"] = [3, 3] }, null)
        };
        var level = ScenarioA1FireCalculator.Resolve(Checked(Target("de-s") with
        {
            Encircled = true
        }), Fire);
        Assert.True(level.Effects.Count == 1, string.Join("; ", level.Reasons));
        Assert.Equal(6, level.Effects.Single().Checks.Single().MoraleLevel);
        Assert.Equal(7, ScenarioA1FireCalculator.Resolve(Checked(Target("de-s")), Fire).Effects.Single().Checks.Single().MoraleLevel);
    }

    [Fact]
    public void TheEntryMultiplierForwardsWithTheOldDoublings()
    {
        foreach (var encircled in Both)
        {
            Assert.Equal(encircled ? 2 : 1, ScenarioA1EncirclementCalculator.EntryMultiplier(encircled));
            for (var half = 0; half <= 12; half++)
            {
                for (var excess = 0; excess <= 3; excess++)
                {
                    // Copies of AdvanceHalfMf and EntryCost's first line as they were before the claim.
                    Assert.Equal((half * (encircled ? 2 : 1)) + (2 * excess), ScenarioA1AdvanceCalculator.AdvanceHalfMf(half, encircled, excess));
                    var entry = new InfantryEntry(half, "stone-building", false, false, false, false);
                    var facts = new EntryCostFacts(entry, encircled, false, false, false, false, false, 0, false, false, [new EntryMoverFacts(false, 0, false)], excess);
                    Assert.Equal((null, (half * (encircled ? 2 : 1)) + (2 * excess)), ScenarioA1MovementCalculator.EntryCost(facts, (_, _, _) => 4));
                }

                var reader = new OneStep(new InfantryEntry(half, "open-ground", false, false, false, false));
                Assert.Equal(half * (encircled ? 2 : 1), ScenarioA1RoutCalculator.RoutEntry(reader, 0, 1, encircled).HalfMf);
            }
        }
    }

    private sealed class OneStep(InfantryEntry entry) : IRoutFactReader
    {
        public string Name(int location) => "L" + location.ToString(System.Globalization.CultureInfo.InvariantCulture);

        public IEnumerable<int> Neighbors(int location) => [1 - location];

        public bool Playable(int location) => true;

        public RoutLocationFacts? Location(int location) => null;

        public RoutLosFacts? Los(int fromLocation, int toLocation) => null;

        public int? Distance(int one, int two) => one == two ? 0 : 1;

        public bool Adjacent(int one, int two) => one != two;

        public (InfantryEntry? Entry, string? Reason) Entry(int fromLocation, int toLocation) => (entry, null);
    }

    [Fact]
    public void StandsForwardsWithTheOldAnswerOfEncirclementStands()
    {
        var units = new List<EncircledLocationUnitFacts>();
        for (var mask = 0; mask < 16; mask++)
        {
            units.Add(new EncircledLocationUnitFacts((mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0));
        }

        foreach (var one in units)
        {
            foreach (var two in units)
            {
                EncircledLocationUnitFacts[] there = [one, two];

                // A copy of EncirclementStands as it was before the claim.
                var old = there.Any(unit => unit.Active && !unit.Dummy && !unit.Captured && unit.Encircled);
                Assert.Equal(old, ScenarioA1FireFollowUps.EncirclementStands(there));
                Assert.Equal(old, ScenarioA1EncirclementCalculator.Stands(there));
            }
        }

        Assert.False(ScenarioA1EncirclementCalculator.Stands([]));
    }

    [Fact]
    public void TheFirerDrmIsOneOnceAndAGunDuelReadsEncircledAsFirerBased()
    {
        Assert.Equal(1, ScenarioA1EncirclementCalculator.FirerDrm(true));
        Assert.Equal(0, ScenarioA1EncirclementCalculator.FirerDrm(false));

        // C5.9's note (p. 173): Encircled is a Firer-Based DRM (ruling RD5.13).
        Assert.Equal(ToHitDrmClass.FirerBased, ScenarioA1GunDuels.Classify("encircled"));
        Assert.Equal(ToHitDrmClass.FirerBased, ScenarioA1GunDuels.Classify("stun"));
    }

    // The ordnance paths under OrdnanceShot.StunEncircledReading and OrdnanceCrew.Encircled.

    private static OrdnanceShot TankAtInfantry(bool? encircled, bool? reading) =>
        new OrdnanceShot("PFPh", "phasing", "russian", new OrdnanceGun("defender-tank-1", "defender-tank", false, 0, false, false),
            new OrdnanceCrew("defender-tank-1", "defender-tank", false, false, false, false, false) { Encircled = encircled }, At, 3, 0, false, true, 0,
            new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground",
                [new FireTarget("de-s", "attacker-squad", At, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], 2, null),
            new OrdnanceRolls([6, 5], null, null, null, null))
        {
            Vehicle = new OrdnanceVehicleFirer(true, false, false, false, false),
            Ammunition = "he",
            ScenarioYear = 1942,
            StunEncircledReading = reading,
        };

    [Fact]
    public void TheInfantryTargetTypeTakesTheEncircledPlusOneOnlyUnderTheSwitch()
    {
        var before = ScenarioA1OrdnanceCalculator.Resolve(TankAtInfantry(true, null), Ordnance);
        Assert.True(before.ToHit is not null, string.Join("; ", before.Reasons));
        Assert.DoesNotContain(before.ToHit.Drm, item => item.Name == "encircled");

        var after = ScenarioA1OrdnanceCalculator.Resolve(TankAtInfantry(true, true), Ordnance);
        Assert.Contains(after.ToHit!.Drm, item => item is { Name: "encircled", Value: 1m, Rule: "A7.7" });
        Assert.Equal(before.ToHit.FinalDr + 1, after.ToHit.FinalDr);

        // A crew not Encircled, or not known to be, adds nothing under the switch.
        Assert.DoesNotContain(ScenarioA1OrdnanceCalculator.Resolve(TankAtInfantry(false, true), Ordnance).ToHit!.Drm, item => item.Name == "encircled");
        Assert.DoesNotContain(ScenarioA1OrdnanceCalculator.Resolve(TankAtInfantry(null, true), Ordnance).ToHit!.Drm, item => item.Name == "encircled");

        // The shot's Gun Duel DRM reads it as Firer-Based (ruling RD5.13).
        Assert.NotNull(ScenarioA1GunDuels.Drm(TankAtInfantry(true, true), Ordnance));
    }

    private static OrdnanceShot TankAtBren(bool? encircled, bool? reading) =>
        new OrdnanceShot("PFPh", "phasing", "german", new OrdnanceGun("firer", "attacker-tank", false, 0, false, false),
            new OrdnanceCrew("firer", "attacker-tank", false, false, false, false, false) { Encircled = encircled }, At, 3, 0, false, true, 0,
            new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 3, null),
            new OrdnanceRolls([2, 1], null, null, null, null) { ToKill = [6, 6], CollateralCheck = [6, 5], CrewSurvival = [6, 6] })
        {
            Vehicle = new OrdnanceVehicleFirer(true, false, false, false, false),
            VehicleTarget = new OrdnanceVehicleTarget("uk-bren", Bren, "front", null, false, false, false, false, false)
            {
                CrewExposed = true,
                Prc = [],
            },
            Ammunition = "he",
            ScenarioYear = 1942,
            StunEncircledReading = reading,
        };

    [Fact]
    public void TheVehicleTargetTypeTakesTheEncircledPlusOneOnlyUnderTheSwitch()
    {
        var before = ScenarioA1OrdnanceCalculator.Resolve(TankAtBren(true, null), Ordnance);
        Assert.True(before.ToHit is not null, string.Join("; ", before.Reasons));
        Assert.DoesNotContain(before.ToHit.Drm, item => item.Name == "encircled");
        var after = ScenarioA1OrdnanceCalculator.Resolve(TankAtBren(true, true), Ordnance);
        Assert.Contains(after.ToHit!.Drm, item => item is { Name: "encircled", Value: 1m, Rule: "A7.7" });
        Assert.DoesNotContain(ScenarioA1OrdnanceCalculator.Resolve(TankAtBren(false, true), Ordnance).ToHit!.Drm, item => item.Name == "encircled");
    }

    private static OrdnanceShot Mortar(bool? encircled, bool? reading) =>
        new OrdnanceShot("PFPh", "phasing", "german", new OrdnanceGun("de-mtr", "attacker-light-mortar", false, 0, false, false),
            new OrdnanceCrew("de-s", "attacker-squad", false, false, false, false, false) { Encircled = encircled }, At, 5, 0, false, true, 0,
            new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground",
                [new FireTarget("ru-s", "defender-squad", At, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], 2, null),
            new OrdnanceRolls([6, 5], null, null, null, null))
        {
            TargetType = OrdnanceTargetTypes.Area,
            StunEncircledReading = reading,
        };

    // The Area calculator is internal to Rules, so it is asked directly, as the Stun increment's tests ask it.
    private static OrdnanceResolution AreaRun(OrdnanceShot shot) =>
        (OrdnanceResolution)typeof(ScenarioA1OrdnanceCalculator).Assembly.GetType("LimboDancer.Domains.Asl.Rules.ScenarioA1AreaCalculator")!
            .GetMethod("Run", [typeof(OrdnanceShot), typeof(GunDefinition), typeof(ScenarioA1OrdnanceReference)])!
            .Invoke(null, [shot, Ordnance.Guns["attacker-light-mortar"], Ordnance])!;

    [Fact]
    public void TheAreaTargetTypeTakesTheEncircledPlusOneOnlyUnderTheSwitch()
    {
        var before = AreaRun(Mortar(true, null));
        Assert.True(before.ToHit is not null, string.Join("; ", before.Reasons));
        Assert.DoesNotContain(before.ToHit.Drm, item => item.Name == "encircled");
        var after = AreaRun(Mortar(true, true));
        Assert.Contains(after.ToHit!.Drm, item => item is { Name: "encircled", Value: 1m, Rule: "A7.7" });
        Assert.DoesNotContain(AreaRun(Mortar(false, true)).ToHit!.Drm, item => item.Name == "encircled");
    }

    private static FireAttack CanisterAttack(FireCanister canister) =>
        new FireAttack("PFPh", "phasing", null, From, At, [], null, 2, true, new FireLos(false, 0, true, false), 7, "open-ground",
            [new FireTarget("de-s", "attacker-squad", At, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], 3,
            new FireRolls([5, 6], null, null, null))
        {
            Canister = canister,
        };

    [Fact]
    public void ACanisterOfAnEncircledCrewTakesThePlusOneAndNoOtherFirerTerm()
    {
        var before = ScenarioA1FireCalculator.Resolve(CanisterAttack(new FireCanister("gun", 12)), Fire).Arithmetic!;
        Assert.DoesNotContain(before.Drm, item => item.Name.StartsWith("encircled", StringComparison.Ordinal));
        Assert.Equal(before.Drm, ScenarioA1FireCalculator.Resolve(CanisterAttack(new FireCanister("gun", 12) { Encircled = false }), Fire).Arithmetic!.Drm);

        var after = ScenarioA1FireCalculator.Resolve(CanisterAttack(new FireCanister("gun", 12) { Encircled = true }), Fire).Arithmetic!;
        Assert.Contains(after.Drm, item => item is { Name: "encircled:gun", Value: 1m, Rule: "A7.7" });

        // Nothing else changes: the firers stay empty, so no Inexperience, CX, or leadership term turns on.
        Assert.Equal(before.Drm, after.Drm.Where(item => item.Name != "encircled:gun"));
        Assert.Equal(before.TotalFirepower, after.TotalFirepower);
    }

    [Fact]
    public void ACanisterBesideAnEncircledFirerIsNeverResolved()
    {
        // A7.7 with A7.52: one +1 per attack. A Canister shot with firers is outside (C8.4), so the Canister's term, read only when no Encircled
        // firer's is, never stands beside one.
        var attack = CanisterAttack(new FireCanister("gun", 12) { Encircled = true }) with
        {
            Firers = [Firer("de-f") with { Encircled = true }]
        };
        Assert.Null(ScenarioA1FireCalculator.Resolve(attack, Fire).Arithmetic);
    }

    // The members that keep their answers (ruling RD5.16).

    [Fact]
    public void EncirclementShareStillCountsNothingForAnOrdnanceHitOrVehicularArmament()
    {
        EncirclementFirerFacts[] entries = [new(true, () => [4]), new(true, () => [1])];
        var attack = Attack([6, 4]);
        var (units, seen) = ScenarioA1FireMapRules.EncirclementShare(attack, ScenarioA1FireCalculator.Preview(attack, Fire), true, Fire, entries);
        Assert.Equal(2, units);
        Assert.Equal(2, seen.Count);

        // A7.7 counts "ordnance/vehicular-armament"; the old member does not, and ValidFire does.
        var hit = attack with
        {
            OrdnanceHit = new FireOrdnanceHit("gun", 6, false)
        };
        Assert.Equal((0, 0), Count(ScenarioA1FireMapRules.EncirclementShare(hit, ScenarioA1FireCalculator.Preview(attack, Fire), true, Fire, entries)));
        Assert.True(ScenarioA1EncirclementCalculator.ValidFire(Fired(EncirclementFireKind.Ordnance, range: 1, normal: 12, hit: true)));
    }

    private static (int Units, int Entries) Count((int Units, List<int> Entries) share) => (share.Units, share.Entries.Count);

    // The contracts.

    private static ContractVerdict Evaluate(string contract, string json) =>
        ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find(contract)!, JsonNode.Parse(json)!.AsObject());

    private static string Seal(string phase = "PFPh", bool consecutive = true, int units = 2, string pattern = "opposite-hexspines", string pillbox = "false") =>
        "{\"targetId\": \"r-squad\", \"phase\": \"" + phase + "\", \"consecutive\": " + (consecutive ? "true" : "false") + ", \"countingUnits\": "
        + units.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", \"pattern\": \"" + pattern + "\", \"inPillbox\": " + pillbox + "}";

    [Fact]
    public void TheSealContractAnswersEachOutcome()
    {
        Assert.Equal("seals", Evaluate("a7.7-encirclement-seal", Seal()).Outcome);
        Assert.Equal("seals", Evaluate("a7.7-encirclement-seal", Seal(pattern: "three-vertices")).Outcome);
        Assert.Equal("seals", Evaluate("a7.7-encirclement-seal", Seal(phase: "AFPh", units: 4, pattern: "three-hexsides")).Outcome);
        Assert.Equal("seals", Evaluate("a7.7-encirclement-seal", Seal(phase: "DFPh", pattern: "above-and-below")).Outcome);
        Assert.Equal("no-seal", Evaluate("a7.7-encirclement-seal", Seal(pattern: "none")).Outcome);
        Assert.Equal("no-seal", Evaluate("a7.7-encirclement-seal", Seal(units: 1)).Outcome);
        Assert.Equal("no-seal", Evaluate("a7.7-encirclement-seal", Seal(phase: "MPh")).Outcome);
        Assert.Equal("no-seal", Evaluate("a7.7-encirclement-seal", Seal(consecutive: false)).Outcome);
        var pillbox = Evaluate("a7.7-encirclement-seal", Seal(pillbox: "true"));
        Assert.Equal(("no-seal", "B30.32"), (pillbox.Outcome, pillbox.Reasons[0].Rule));

        // A count that is not a whole number, and a pillbox the viewer does not know.
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate("a7.7-encirclement-seal", Seal().Replace("\"countingUnits\": 2", "\"countingUnits\": 2.5", StringComparison.Ordinal)).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate("a7.7-encirclement-seal", Seal(pillbox: "null")).Outcome);
        Assert.All(ScenarioA1RuleContracts.Find("a7.7-encirclement-seal")!.Facts.Where(fact => fact.Name == "inPillbox"),
            fact => Assert.Equal((ContractDisclosure.Owner, "targetId"), (fact.Disclosure, fact.Concerns)));
    }

    private static string Unit(string kind = "infantry", bool marker = true, bool melee = false, bool berserk = false, bool heroic = false, bool cutOff = false,
        bool pillbox = false) =>
        "{\"unitId\": \"r-squad\", \"marker\": " + Json(marker) + ", \"melee\": " + Json(melee) + ", \"berserk\": " + Json(berserk) + ", \"heroic\": " + Json(heroic)
        + ", \"upperLevelCutOff\": " + Json(cutOff) + ", \"kind\": \"" + kind + "\", \"inPillbox\": " + Json(pillbox) + "}";

    private static string Json(bool value) => value ? "true" : "false";

    [Fact]
    public void TheEncircledContractAnswersEachOutcome()
    {
        var encircled = Evaluate("a7.7-encircled", Unit());
        Assert.Equal("encircled", encircled.Outcome);
        Assert.Contains("Morale Level", encircled.Reasons[0].Text, StringComparison.Ordinal);
        Assert.Contains("+1", encircled.Reasons[0].Text, StringComparison.Ordinal);
        Assert.Contains("double MF", encircled.Reasons[0].Text, StringComparison.Ordinal);

        Assert.Equal("encircled", Evaluate("a7.7-encircled", Unit(marker: false, melee: true)).Outcome);
        Assert.Equal("encircled", Evaluate("a7.7-encircled", Unit(kind: "vulnerable-prc")).Outcome);
        var upper = Evaluate("a7.7-encircled", Unit(marker: false, cutOff: true));
        Assert.Equal(("encircled", "A7.72"), (upper.Outcome, upper.Reasons[0].Rule));

        Assert.Equal("not-encircled", Evaluate("a7.7-encircled", Unit(marker: false)).Outcome);
        Assert.Equal("not-encircled", Evaluate("a7.7-encircled", Unit(berserk: true)).Outcome);
        Assert.Equal("not-encircled", Evaluate("a7.7-encircled", Unit(heroic: true, cutOff: true)).Outcome);
        Assert.Equal("not-encircled", Evaluate("a7.7-encircled", Unit(kind: "hero")).Outcome);
        Assert.Equal("not-encircled", Evaluate("a7.7-encircled", Unit(kind: "vehicle")).Outcome);
        Assert.Equal("not-encircled", Evaluate("a7.7-encircled", Unit(kind: "prc")).Outcome);

        // B30.32: "Neither a pillbox nor its contents can be Encircled", under a marker or cut off; the pillbox is its owner's.
        var pillbox = Evaluate("a7.7-encircled", Unit(pillbox: true));
        Assert.Equal(("not-encircled", "B30.32"), (pillbox.Outcome, pillbox.Reasons[0].Rule));
        Assert.Equal("not-encircled", Evaluate("a7.7-encircled", Unit(marker: false, cutOff: true, pillbox: true)).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate("a7.7-encircled", Unit().Replace("\"inPillbox\": false", "\"inPillbox\": null", StringComparison.Ordinal)).Outcome);
        Assert.All(ScenarioA1RuleContracts.Find("a7.7-encircled")!.Facts.Where(fact => fact.Name == "inPillbox"),
            fact => Assert.Equal((ContractDisclosure.Owner, "unitId"), (fact.Disclosure, fact.Concerns)));
    }

    [Fact]
    public void TheOpponentOfAConcealedUnitCannotEvaluateItsEncirclement()
    {
        // A concealed unit's kind is its owner's: the other side's viewer gives it as unknown.
        var verdict = Evaluate("a7.7-encircled", "{\"unitId\": \"r-squad\", \"marker\": true, \"melee\": false, \"berserk\": false, \"heroic\": false, "
            + "\"upperLevelCutOff\": false, \"kind\": null, \"inPillbox\": null}");
        Assert.Equal(ContractVerdict.NotEvaluated, verdict.Outcome);
        Assert.All(ScenarioA1RuleContracts.Find("a7.7-encircled")!.Facts.Where(fact => fact.Name == "kind"),
            fact => Assert.Equal((ContractDisclosure.Owner, "unitId"), (fact.Disclosure, fact.Concerns)));
    }
}
