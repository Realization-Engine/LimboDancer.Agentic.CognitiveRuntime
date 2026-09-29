using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The Ordnance package (unit step 24): a Gun's HE shot at Infantry on the Infantry Target Type. The German 7.5cm leIG 18 (75*,
/// ROF 2, black TH#) and the Russian 45mm PTP obr. 32 (45L, ROF 3, red TH#), each manned by its crew, fire at enemy units; the
/// To Hit arithmetic, Critical Hits, Improbable Hits, ROF, breakdown, Acquisition, and the IFT effects of a hit through the Fire
/// package, and every roll sequence the package asks for ends Resolved.
/// </summary>
public sealed class ScenarioA1OrdnanceTests
{
    private static readonly ScenarioA1OrdnanceReference Reference = new ScenarioA1OrdnancePackage().Reference;
    private const string At = "bd01:G5:0";

    private static FireTarget Target(string id, string definition = "defender-squad", string location = At, bool concealed = false) =>
        new(id, definition, location, false, false, concealed, false, false, false, false)
        {
            KnownEnemyInLos = true,
            Captors = [],
        };

    private static FireAttack Hit(string phase, string side, string terrain, FireTarget[] targets, int hindrance = 0) =>
        new(phase, side, null, null, At, [], null, null, true, new FireLos(false, hindrance, true, false), 7, terrain, targets, 2, null);

    private static OrdnanceShot German(int range = 5, string phase = "PFPh", string terrain = "open-ground", FireTarget[]? targets = null, int turn = 0,
        bool woods = false, int acquisition = 0, bool pinned = false, int shots = 0, bool kept = false, OrdnanceRolls? rolls = null)
    {
        var side = phase == "DFPh" ? "non-phasing" : "phasing";
        return new OrdnanceShot(phase, side, "german", new OrdnanceGun("de-gun", "attacker-inf-gun", false, shots, kept, shots > 0),
            new OrdnanceCrew("de-crew", "attacker-crew", false, pinned, false, false, false), At, range, turn, woods, true, acquisition,
            Hit(phase, side, terrain, targets ?? [Target("ru-s")]), rolls ?? new OrdnanceRolls(null, null, null, null, null));
    }

    private static OrdnanceShot Russian(int range = 5, string phase = "PFPh", int turn = 0, bool woods = false) =>
        new(phase, "phasing", "russian", new OrdnanceGun("ru-gun", "defender-at-gun", false, 0, false, false),
            new OrdnanceCrew("ru-crew", "defender-crew", false, false, false, false, false), At, range, turn, woods, true, 0,
            Hit(phase, "phasing", "open-ground", [Target("de-s", "attacker-squad")]) with { TargetSideElr = 3 }, new OrdnanceRolls(null, null, null, null, null));

    private static OrdnanceShot With(OrdnanceShot shot, int[] toHit, int? subsequent = null) => shot with
    {
        Rolls = shot.Rolls! with { ToHit = toHit, Subsequent = subsequent }
    };

    /// <summary>Resolves a shot, answering each further roll with a fixed value: IFT and MC DR 3 and 4, drs of 2.</summary>
    private static OrdnanceResolution Complete(OrdnanceShot shot, int[]? iftAttack = null)
    {
        var rolls = shot.Rolls!;
        for (var step = 0; step < 60; step++)
        {
            var result = ScenarioA1OrdnanceCalculator.Resolve(shot with { Rolls = rolls }, Reference);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.ordnance.roll-missing:", StringComparison.Ordinal))
            {
                return result;
            }

            rolls = Answer(rolls, reason["asl.a1.ordnance.roll-missing:".Length..], iftAttack ?? [3, 4], [3, 4], 2);
        }

        throw new InvalidOperationException("The package kept asking for rolls.");
    }

    private static OrdnanceRolls Answer(OrdnanceRolls rolls, string key, int[] iftAttack, int[] twoDice, int dr)
    {
        if (key == "subsequent")
        {
            return rolls with { Subsequent = dr };
        }

        if (key.StartsWith("criticalSelection:", StringComparison.Ordinal))
        {
            var units = key["criticalSelection:".Length..].Split(',');
            return rolls with { CriticalSelection = units.Select((id, index) => (id, index)).ToDictionary(item => item.id, item => item.index == 0 ? 6 : 1) };
        }

        var critical = key.StartsWith("critical-hit:", StringComparison.Ordinal);
        var inner = key[(key.IndexOf(':', StringComparison.Ordinal) + 1)..];
        var fire = (critical ? rolls.CriticalHit : rolls.Hit) ?? new FireRolls(null, null, null, null);
        fire = FireAnswer(fire, inner, iftAttack, twoDice, dr);
        return critical ? rolls with { CriticalHit = fire } : rolls with { Hit = fire };
    }

    private static FireRolls FireAnswer(FireRolls rolls, string key, int[] iftAttack, int[] twoDice, int dr)
    {
        var split = key.IndexOf(':', StringComparison.Ordinal);
        var (kind, unit) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
        Dictionary<string, TValue> Add<TValue>(IReadOnlyDictionary<string, TValue>? existing, string id, TValue value)
        {
            var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
            next[id] = value;
            return next;
        }

        Dictionary<string, int> Selection(IReadOnlyDictionary<string, int>? existing) =>
            unit.Split(',').Aggregate(existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new(StringComparer.Ordinal),
                (map, id) => { map[id] = dr; return map; });

        return kind switch
        {
            "attack" => rolls with { Attack = iftAttack },
            "randomSelection" => rolls with { RandomSelection = Selection(rolls.RandomSelection) },
            "checks" => rolls with { Checks = Add<IReadOnlyList<int>>(rolls.Checks, unit, twoDice) },
            "leaderLoss" => rolls with { LeaderLoss = Add<IReadOnlyList<int>>(rolls.LeaderLoss, unit, twoDice) },
            "heatOfBattle" => rolls with { HeatOfBattle = Add<IReadOnlyList<int>>(rolls.HeatOfBattle, unit, twoDice) },
            "berserkCheck" => rolls with { BerserkChecks = Add<IReadOnlyList<int>>(rolls.BerserkChecks, unit, twoDice) },
            _ => rolls with { WoundSeverity = Add(rolls.WoundSeverity, unit, dr) },
        };
    }

    [Fact]
    public void TheBasicAndModifiedToHitNumbersFollowTheTableTheColorAndTheGun()
    {
        // C3.3: the Infantry row, black for the Germans and red for the Russians (A25); C4.1, C4.2: the modifications beyond 12 hexes.
        Assert.Equal((8, 8), (Reference.BasicToHit("black", 5), Reference.BasicToHit("red", 5)));
        Assert.Equal((7, 6), (Reference.BasicToHit("black", 8), Reference.BasicToHit("red", 8)));
        var leig = Reference.Guns["attacker-inf-gun"];
        var pak = Reference.Guns["defender-at-gun"];
        Assert.Equal((75, "star", 2, 12), (leig.Caliber, leig.Suffix, leig.RateOfFire, leig.Breakdown));
        Assert.Equal((45, "l", 3, 12), (pak.Caliber, pak.Suffix, pak.RateOfFire, pak.Breakdown));
        Assert.Equal([("barrel:star", -1m)], Reference.Modifications(leig, 14).Select(item => (item.Name, item.Value)));
        Assert.Equal([("barrel:l", 1m), ("caliber-57mm", -1m)], Reference.Modifications(pak, 14).Select(item => (item.Name, item.Value)));
        Assert.Equal([("barrel:l", 1m), ("caliber-57mm", -2m)], Reference.Modifications(pak, 30).Select(item => (item.Name, item.Value)));
        Assert.Empty(Reference.Modifications(pak, 12));

        // C.6: 75mm uses the 12 FP column (70mm), 45mm the 4 FP column (37mm).
        Assert.Equal((12, 4), (Reference.HeFirepower(75), Reference.HeFirepower(45)));
    }

    [Fact]
    public void AHitAttacksTheLocationOnTheGunsHeColumnWithTheTemOnTheToHitDr()
    {
        // Range 5 in a wooden building: Basic TH# 8; Case Q +2; DR 2 and 3 = 5 + 2 = 7: a hit, not below half of 8.
        var result = Complete(With(German(terrain: "wooden-building"), [2, 3]));
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        var toHit = result.ToHit!;
        Assert.Equal(("black", 8, 8, 5, 7, true, false), (toHit.Color, toHit.BasicToHit, toHit.ModifiedToHit, toHit.OriginalDr, toHit.FinalDr, toHit.Hit, toHit.CriticalHit));
        Assert.Equal([("case-q:wooden-building", 2m)], toHit.Drm.Select(item => (item.Name, item.Value)));

        // C.6, C.3: the 12 FP column with no TEM on the Effects DR: 3 and 4 is a 1MC.
        var arithmetic = result.Hit!.Arithmetic!;
        Assert.Equal((12, 7, "1MC"), (arithmetic.ColumnFp!.Value, arithmetic.FinalDr, arithmetic.Result));
        Assert.Empty(arithmetic.Drm);
        Assert.Null(result.CriticalHit);
    }

    [Fact]
    public void AFinalDrBelowHalfTheModifiedToHitIsACriticalHitWithDoubledFpAndReversedTem()
    {
        // At one hex in a wooden building (-2, +2), DR 1 and 2: Final 3 < 4, a Critical Hit (C3.7); C3.71: 24 FP, and the wooden
        // building's +2 becomes -2. A single target takes it alone.
        var result = Complete(With(German(range: 1, terrain: "wooden-building"), [1, 2]));
        var toHit = result.ToHit!;
        Assert.Equal([("case-l", -2m), ("case-q:wooden-building", 2m)], toHit.Drm.Select(item => (item.Name, item.Value)));
        Assert.True(toHit.CriticalHit);
        var arithmetic = result.CriticalHit!.Arithmetic!;
        Assert.Equal((24, 5), (arithmetic.ColumnFp!.Value, arithmetic.FinalDr));
        Assert.Equal([("critical-hit-tem:wooden-building", -2m)], arithmetic.Drm.Select(item => (item.Name, item.Value)));
        Assert.Null(result.Hit);
    }

    [Fact]
    public void AtNightTheLowVisibilityDrmIsACaseRHindranceOfItsOwnAndMudCushionsHe()
    {
        // E1.7, E3.1, E3.62 (backlog pass 16; referee, pass 16): +1 Case R for Low Visibility, apart from the LOS Hindrance, and +1 TEM for Mud in Open Ground.
        var drm = ScenarioA1OrdnanceCalculator.Resolve(With(German() with { LowVisibilityDrm = 1, CushionedOpenGround = true }, [6, 6]), Reference).ToHit!.Drm;
        Assert.Contains(drm, item => item.Name == "case-r:lv" && item.Value == 1);
        Assert.Contains(drm, item => item.Name == "case-q:weather-cushion" && item.Value == 1);
        Assert.DoesNotContain(drm, item => item.Name == "case-r");
    }

    [Fact]
    public void AnOriginalTwoThatHitsCallsForASubsequentDrThatDecidesTheCriticalHit()
    {
        // Final 2 + 2 = 4 is not below half of 8, so an Original 2 asks for a subsequent dr: 4 (at most half) is a Critical Hit, 5 is not.
        var shot = German(terrain: "wooden-building");
        Assert.Equal(["asl.a1.ordnance.roll-missing:subsequent"], ScenarioA1OrdnanceCalculator.Resolve(With(shot, [1, 1]), Reference).Reasons);
        Assert.True(Complete(With(shot, [1, 1], 4)).ToHit!.CriticalHit);
        Assert.False(Complete(With(shot, [1, 1], 5)).ToHit!.CriticalHit);

        // Referee's reading A: when only the lowest Final DR hits (2 + 6 = 8), the Infantry Target Type's rule still applies: a dr of 2 (at
        // most half of 8) is a Critical Hit, a 5 is not; the bracketed exception belongs to the Area and Vehicle Target Types (R24.7).
        var hard = German(terrain: "stone-building", pinned: true, acquisition: 0) with { Hit = German().Hit! with { TargetTerrain = "stone-building", Los = new FireLos(false, 1, true, false) } };
        Assert.Equal(8, 2 + (int)ScenarioA1OrdnanceCalculator.Resolve(With(hard, [6, 6]), Reference).ToHit!.Drm.Sum(item => item.Value));
        Assert.True(Complete(With(hard, [1, 1], 2)).ToHit!.CriticalHit);
        Assert.False(Complete(With(hard, [1, 1], 5)).ToHit!.CriticalHit);
    }

    [Fact]
    public void WhenNoFinalDrCanHitAnOriginalTwoStillHitsOnASubsequentDrOfThreeOrLess()
    {
        // The Russian AT Gun at 13 hexes (red 5, L +1, 45mm -1: 5) turning three hexspines in woods: (3 + 2) x 2 = +10. C3.6.
        var shot = Russian(range: 13, turn: 3, woods: true);
        var drm = ScenarioA1OrdnanceCalculator.Resolve(With(shot, [4, 4]), Reference).ToHit!;
        Assert.Equal((5, true, false), (drm.ModifiedToHit, drm.Improbable, drm.Hit));
        Assert.Equal([("case-a:3", 10m)], drm.Drm.Select(item => (item.Name, item.Value)));
        Assert.True(Complete(With(shot, [1, 1], 3)).ToHit!.Hit);
        Assert.False(Complete(With(shot, [1, 1], 4)).ToHit!.Hit);
        Assert.True(Complete(With(shot, [1, 1], 1)).ToHit!.CriticalHit);
    }

    [Fact]
    public void TheColoredDrKeepsTheRofUnlessTheGunTurnedTheCrewIsPinnedOrItIsTheAfph()
    {
        // C2.24: the leIG's ROF 2 is kept on a colored 2, not on a 3; C2.5: turning lowers it to 1; C5.4, C5.2: pinned or AFPh, none.
        Assert.True(Complete(With(German(), [2, 6])).Gun!.RateOfFireKept);
        Assert.False(Complete(With(German(), [3, 5])).Gun!.RateOfFireKept);
        Assert.Equal("prep-fire", Complete(With(German(), [3, 5])).Gun!.FireCounter);
        Assert.False(Complete(With(German(turn: 1), [2, 5])).Gun!.RateOfFireKept);
        Assert.True(Complete(With(German(turn: 1), [1, 5])).Gun!.RateOfFireKept);
        Assert.False(Complete(With(German(pinned: true), [1, 5])).Gun!.RateOfFireKept);
        Assert.False(Complete(With(German(phase: "AFPh"), [1, 5])).Gun!.RateOfFireKept);
        Assert.Equal("final-fire", Complete(With(German(phase: "DFPh"), [4, 5])).Gun!.FireCounter);
    }

    [Fact]
    public void AnOriginalTwelveMalfunctionsTheGunAndLosesTheAcquisition()
    {
        var result = Complete(With(German(acquisition: -1), [6, 6]));
        Assert.Equal((true, false, 0, null), (result.Gun!.Malfunctioned, result.ToHit!.Hit, result.Gun.Acquisition, result.Gun.AcquiredLocationId));
    }

    [Fact]
    public void EachShotAcquiresTheLocationToMinusTwoAndAConcealedOneOnlyWhenItLosesConcealment()
    {
        Assert.Equal((-1, At), (Complete(With(German(), [5, 5])).Gun!.Acquisition, Complete(With(German(), [5, 5])).Gun!.AcquiredLocationId));
        Assert.Equal(-2, Complete(With(German(acquisition: -1), [5, 5])).Gun!.Acquisition);
        Assert.Equal(-2, Complete(With(German(acquisition: -2), [5, 5])).Gun!.Acquisition);

        // C6.2: +2 against a concealed Location; C6.57: a miss does not acquire it.
        var concealed = German(targets: [Target("ru-s", concealed: true)]);
        var miss = Complete(With(concealed, [5, 5]));
        Assert.Contains(("case-k", 2m), miss.ToHit!.Drm.Select(item => (item.Name, item.Value)));
        Assert.Equal(0, miss.Gun!.Acquisition);

        // A hit that breaks concealment (a PTC or worse, A12.14) acquires it.
        var hit = Complete(With(concealed, [1, 2]), iftAttack: [1, 2]);
        Assert.True(hit.Hit!.Effects.Single().ConcealmentLost);
        Assert.Equal(-1, hit.Gun!.Acquisition);

        // The acquisition lowers the next To Hit DR (Case N).
        Assert.Contains(("case-n", -2m), Complete(With(German(acquisition: -2), [5, 5])).ToHit!.Drm.Select(item => (item.Name, item.Value)));
    }

    [Fact]
    public void ACriticalHitAmongSeveralTargetsFallsOnTheUnitRandomSelectionPicks()
    {
        var shot = German(range: 1, targets: [Target("ru-s"), Target("ru-l", "defender-leader")]);
        Assert.Equal(["asl.a1.ordnance.roll-missing:criticalSelection:ru-s,ru-l"], ScenarioA1OrdnanceCalculator.Resolve(With(shot, [1, 2]), Reference).Reasons);
        var result = Complete(With(shot, [1, 2]));
        Assert.Equal("ru-s", result.CriticalTarget);
        Assert.Equal(["ru-s"], result.CriticalHit!.Effects.Select(item => item.UnitId));
        Assert.Equal(["ru-l"], result.Hit!.Effects.Select(item => item.UnitId));
        Assert.Equal(24, result.CriticalHit.Arithmetic!.ColumnFp);
    }

    [Fact]
    public void RefereeFindingsOnTheCoveredArcTargetsAndAcquisition()
    {
        // D1: A7.81, C5.11: a pinned crew cannot turn its Gun; D6: a Gun that fired from woods or a building fires again only inside its CA.
        Assert.Contains("asl.a1.ordnance.covered-arc-fixed", ScenarioA1OrdnanceCalculator.Resolve(With(German(pinned: true, turn: 1), [3, 4]), Reference).Reasons);
        Assert.Contains("asl.a1.ordnance.covered-arc-fixed",
            ScenarioA1OrdnanceCalculator.Resolve(With(German(woods: true, turn: 1, shots: 1, kept: true), [3, 4]), Reference).Reasons);
        Assert.Equal(OrdnanceResolution.Resolved, Complete(With(German(turn: 1, shots: 1, kept: true), [3, 4])).Disposition);

        // D3: C3.32: only enemy units are hit; a German squad in the target Location is not admitted as a target.
        Assert.Contains("asl.a1.ordnance.target-outside",
            ScenarioA1OrdnanceCalculator.Resolve(With(German(targets: [Target("de-s", "attacker-squad")]), [3, 4]), Reference).Reasons);

        // D2: C6.51, C6.57: no Case N against a concealed Location; a hit that costs it its concealment acquires it afresh at -1.
        var concealed = Complete(With(German(acquisition: -2, targets: [Target("ru-s", concealed: true)]), [1, 2]), iftAttack: [1, 2]);
        Assert.DoesNotContain(concealed.ToHit!.Drm, item => item.Name == "case-n");
        Assert.Equal(-1, concealed.Gun!.Acquisition);
    }

    [Fact]
    public void ACriticalHitAndTheNormalHitShareOneEffectsDr()
    {
        // Referee's reading C: C3.32: every unit hit is attacked with a single Effects DR; C3.74: the Critical Hit's unit takes it on the
        // doubled column, the others on the normal one.
        var shot = German(range: 1, targets: [Target("ru-s"), Target("ru-l", "defender-leader")]);
        var result = Complete(With(shot, [1, 2]), iftAttack: [2, 5]);
        Assert.Equal(result.CriticalHit!.Arithmetic!.Dice, result.Hit!.Arithmetic!.Dice);
        Assert.Equal((24, 12), (result.CriticalHit.Arithmetic.ColumnFp!.Value, result.Hit.Arithmetic.ColumnFp!.Value));
    }

    [Theory]
    [InlineData("phase", "asl.a1.ordnance.phase-outside")]
    [InlineData("crew-broken", "asl.a1.ordnance.crew-outside")]
    [InlineData("crew-foreign", "asl.a1.ordnance.crew-outside")]
    [InlineData("squad-mans", "asl.a1.ordnance.crew-outside")]
    [InlineData("malfunctioned", "asl.a1.ordnance.gun-malfunctioned")]
    [InlineData("no-rof", "asl.a1.ordnance.gun-already-fired")]
    [InlineData("afph-again", "asl.a1.ordnance.gun-already-fired")]
    [InlineData("own-hex", "asl.a1.ordnance.out-of-range")]
    [InlineData("elevation", "asl.a1.ordnance.out-of-range")]
    [InlineData("los", "asl.a1.ordnance.los-blocked")]
    [InlineData("empty", "asl.a1.ordnance.target-outside")]
    public void ShotsOutsideTheReviewAreAbstained(string name, string reason)
    {
        var shot = German();
        shot = name switch
        {
            "phase" => shot with { Phase = "MPh" },
            "crew-broken" => shot with { Crew = shot.Crew! with { Broken = true } },
            "crew-foreign" => shot with { Crew = shot.Crew! with { DefinitionId = "defender-crew" } },
            "squad-mans" => shot with { Crew = shot.Crew! with { DefinitionId = "attacker-squad" } },
            "malfunctioned" => shot with { Gun = shot.Gun! with { Malfunctioned = true } },
            "no-rof" => shot with { Gun = shot.Gun! with { ShotsThisPhase = 1, RateOfFireKept = false, FiredThisPlayerTurn = true } },
            "afph-again" => German(phase: "AFPh") with { Gun = shot.Gun! with { FiredThisPlayerTurn = true } },
            "own-hex" => shot with { Range = 0 },
            "elevation" => shot with { ElevationAllowed = false },
            "los" => shot with { Hit = shot.Hit! with { Los = new FireLos(true, 0, true, false) } },
            _ => shot with { Hit = shot.Hit! with { Targets = [] } },
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(With(shot, [3, 4]), Reference);
        Assert.Equal(OrdnanceResolution.Abstained, result.Disposition);
        Assert.Contains(reason, result.Reasons);
    }

    [Fact]
    public void AGunMayFireAgainOnAKeptRofAndMixedConcealmentIsUndecided()
    {
        Assert.Equal(OrdnanceResolution.Resolved, Complete(With(German(shots: 1, kept: true), [3, 4])).Disposition);
        var mixed = German(targets: [Target("ru-s"), Target("ru-h", "defender-half-squad", concealed: true)]);
        Assert.Contains("asl.a1.ordnance.concealment-mixed", ScenarioA1OrdnanceCalculator.Precheck(mixed, Reference));
    }

    [Fact]
    public void TheFirePackageRefusesACrewAsATarget()
    {
        // C11: a crew and its Gun as targets are not reviewed (ruling R24.3).
        var shot = German(targets: [Target("ru-crew", "defender-crew")]);
        Assert.Contains("asl.a1.ordnance.hit:asl.a1.fire.crew-target-unreviewed", ScenarioA1OrdnanceCalculator.Precheck(shot, Reference));
    }

    public static TheoryData<string> Accepted => ["german-open", "german-building-two-targets", "russian-long-range", "russian-improbable", "german-pinned-afph"];

    private static OrdnanceShot Scenario(string name) => name switch
    {
        "german-open" => German(),
        "german-building-two-targets" => German(range: 2, terrain: "stone-building", targets: [Target("ru-s"), Target("ru-l", "defender-leader")], acquisition: -1),
        "russian-long-range" => Russian(range: 14),
        "russian-improbable" => Russian(range: 13, turn: 3, woods: true),
        _ => German(phase: "AFPh", pinned: true, woods: true),
    };

    [Theory]
    [MemberData(nameof(Accepted))]
    public void EveryRollSequenceOfAnAcceptedShotEndsResolved(string name)
    {
        // The walk: every ordered To Hit DR, every subsequent dr, and one IFT DR per total; the other IFT rolls answered with 3 and 4.
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
                        int[] ift = total <= 7 ? [1, total - 1] : [total - 6, 6];
                        var result = Complete(With(shot, [colored, white], subsequent), ift);
                        Assert.True(result.Disposition == OrdnanceResolution.Resolved, $"{name} {colored},{white} {subsequent} {total}: {string.Join("; ", result.Reasons)}");
                        ends++;
                    }
                }
            }
        }

        Assert.Equal(36 * 6 * 11, ends);
    }

    [Fact]
    public void ACxCrewAddsOneToTheToHitDr()
    {
        // A4.51 (ruling R5.2): a CX crew adds one to its Gun's To Hit DR.
        var shot = German();
        var plain = ScenarioA1OrdnanceCalculator.Resolve(With(shot, [6, 5]), Reference).ToHit!;
        var tired = ScenarioA1OrdnanceCalculator.Resolve(With(shot with { Crew = shot.Crew! with { Cx = true } }, [6, 5]), Reference).ToHit!;
        Assert.DoesNotContain(plain.Drm, item => item.Name == "cx");
        Assert.Contains(tired.Drm, item => item.Name == "cx" && item.Value == 1 && item.Rule == "A4.51");
        Assert.Equal(plain.FinalDr + 1, tired.FinalDr);
    }

    [Fact]
    public void AWreckInTheTargetLocationAddsOneToTheToHitDr()
    {
        // D9.3, D10.3 (ruling R6.1): Infantry in Open Ground with a wreck take its +1 TEM as Case Q.
        var shot = German();
        var plain = ScenarioA1OrdnanceCalculator.Resolve(With(shot, [6, 5]), Reference).ToHit!;
        var covered = ScenarioA1OrdnanceCalculator.Resolve(With(shot with { Hit = shot.Hit! with { AfvCover = "de-wreck" } }, [6, 5]), Reference).ToHit!;
        Assert.Contains(covered.Drm, item => item.Name == "case-q:afv-cover:de-wreck" && item.Value == 1 && item.Rule == "D9.3");
        Assert.Equal(plain.FinalDr + 1, covered.FinalDr);
    }
}
