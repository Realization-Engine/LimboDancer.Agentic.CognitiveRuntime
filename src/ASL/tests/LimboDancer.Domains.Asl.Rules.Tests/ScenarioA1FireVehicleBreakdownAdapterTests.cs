using System.Text.Json;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1FireVehicleBreakdownAdapterTests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string At = "bd01:G5:0";

    private static VehicleBreakdownUseFacts Facts(VehicleBreakdownWeapon weapon, int? b = null, int? x = null, bool circled = false,
        bool lowAmmo = false, bool shortage = false) => new(weapon, b, x, circled, true, lowAmmo, shortage);

    private static FireAttack Shot(bool multiple = false) => new("PFPh", "phasing", true, "bd01:F5:0", At, [], null, 3, true,
        new FireLos(false, 0, true, false), null, "open-ground",
        [new FireTarget("ru-s", "defender-squad", At, false, false, false, false, false, false, false)], 3,
        new FireRolls([5, 6], null, null, null))
    {
        VehicleFire = new FireVehicleFire("de-v", multiple ? "attacker-tank" : "attacker-halftrack", "bd01:F5:0", true, false, false, false,
            false, false, false, false)
        {
            Weapons = multiple ?
            [
                new("bmg", false, false) { Turn = 0 },
                new("cmg", false, false) { Turn = 0 },
            ] : null,
        },
    };

    private static FireVehicleBreakdownFacts Supplement(params (string Name, VehicleBreakdownUseFacts Facts)[] weapons) =>
        new(weapons.ToDictionary(item => item.Name, item => item.Facts, StringComparer.Ordinal));

    private static FireResolution Complete(FireAttack attack, FireVehicleBreakdownFacts facts)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack, Reference, facts);
            var checks = result.Reasons.Where(reason => reason.StartsWith("asl.a1.fire.roll-missing:checks:", StringComparison.Ordinal))
                .SelectMany(reason => reason["asl.a1.fire.roll-missing:checks:".Length..].Split(',')).ToArray();
            if (checks.Length == 0)
            {
                return result;
            }
            attack = attack with
            {
                Rolls = attack.Rolls! with
                {
                    Checks = checks.ToDictionary(id => id, _ => (IReadOnlyList<int>)[2, 2]),
                },
            };
        }
        throw new InvalidOperationException("Unexpected repeated target checks.");
    }

    [Fact]
    public void CurrentFinalFireMaMgUsesSustainedNumbersAndForfeitsRof()
    {
        var plain = Shot();
        var attack = plain with
        {
            Phase = "DFPh",
            FiringSide = "non-phasing",
            Range = 1,
            FinalFireVersion = 1,
            VehicleFire = plain.VehicleFire! with
            {
                FiredThisPlayerTurn = true,
                FinalFireMarker = FireMarker.FirstFire,
            },
        };
        var facts = Supplement(("ma", Facts(VehicleBreakdownWeapon.MainArmament, 12)));
        Assert.Contains("asl.a1.fire.firer-already-fired", ScenarioA1FireCalculator.Precheck(attack with
        {
            FinalFireVersion = null
        }, Reference));
        Assert.Contains(ScenarioA1FireCalculator.Precheck(attack with
        {
            Range = 3
        }, Reference), reason => reason.StartsWith("asl.a1.fire.final-fire:", StringComparison.Ordinal));
        foreach (var dice in new int[][] { [1, 2], [5, 5], [6, 6] })
        {
            var result = Complete(attack with
            {
                Rolls = new FireRolls(dice, null, null, null)
            }, facts);
            Assert.Equal(FireResolution.Resolved, result.Disposition);
            var effect = Assert.Single(result.WeaponEffects!);
            Assert.True(effect.SustainedFire);
            Assert.False(effect.RateOfFireRetained);
            Assert.Equal("final-fire", effect.FireCounter);
            Assert.Equal(new VehicleBreakdownNumbers(10, 12, false, false), effect.VehicleBreakdown!.Numbers);
            Assert.Equal(dice.Sum() == 12 ? VehicleBreakdownOutcome.Disabled
                : dice.Sum() == 10 ? VehicleBreakdownOutcome.Malfunction : VehicleBreakdownOutcome.None, effect.VehicleBreakdown.Outcome);
            var normal = Complete(attack with
            {
                VehicleFire = attack.VehicleFire! with
                {
                    FiredThisPlayerTurn = false,
                    FinalFireMarker = FireMarker.None
                },
                Rolls = new FireRolls([5, 6], null, null, null)
            }, facts);
            Assert.True(normal.Disposition == FireResolution.Resolved, string.Join(", ", normal.Reasons));
            Assert.Equal(normal.Arithmetic!.TotalFirepower / 2, result.Arithmetic!.TotalFirepower);
        }
    }

    private static FireAttack FinalFireMaMg(FireMarker? marker, bool fired = true, bool rofShot = false)
    {
        var plain = Shot();
        return plain with
        {
            Phase = "DFPh",
            FiringSide = "non-phasing",
            Range = 1,
            FinalFireVersion = 1,
            VehicleFire = plain.VehicleFire! with
            {
                FiredThisPlayerTurn = fired,
                RateOfFireShot = rofShot,
                FinalFireMarker = marker,
            },
        };
    }

    [Fact]
    public void AnMaMgWithNoMultipleRofHasNoFinalFire()
    {
        // D3.5 (p. 200): a vehicle's MG fires once a Player Turn "unless it is the vehicle's MA with a specific Multiple ROF" (item 46).
        var attack = FinalFireMaMg(FireMarker.FirstFire);
        var printed = Reference.Definitions["attacker-halftrack"];
        Assert.NotNull(printed.RateOfFire);
        var none = printed with
        {
            RateOfFire = null
        };
        Assert.Equal(FinalFireUse.OnceMore, ScenarioA1FinalFireCalculator.Verdict(ScenarioA1FinalFireCalculator.VehicleFacts(attack, attack.VehicleFire!, printed)).Use);
        var refused = ScenarioA1FinalFireCalculator.Verdict(ScenarioA1FinalFireCalculator.VehicleFacts(attack, attack.VehicleFire!, none));
        Assert.Equal(FinalFireUse.None, refused.Use);
        Assert.Contains("neither", refused.Refusal, StringComparison.Ordinal);
        Assert.Equal(FinalFireUse.None, ScenarioA1FinalFireCalculator.Verdict(ScenarioA1FinalFireCalculator.VehicleFacts(attack, attack.VehicleFire!, null)).Use);
        var spent = new FireSpentFacts("dfph", true, false, true, () => false, () => false, () => false);
        Assert.False(ScenarioA1FireEligibility.FireSpent(spent, ScenarioA1FinalFireCalculator.VehicleFacts(attack.VehicleFire!, true, 1)));
        Assert.True(ScenarioA1FireEligibility.FireSpent(spent, ScenarioA1FinalFireCalculator.VehicleFacts(attack.VehicleFire!, false, 1)));
    }

    [Fact]
    public void ASustainedMaMgThroughTheLegacyResolveNeedsBreakdownFacts()
    {
        var result = ScenarioA1FireCalculator.Resolve(FinalFireMaMg(FireMarker.FirstFire), Reference);
        Assert.Equal(FireResolution.Indeterminate, result.Disposition);
        Assert.Contains("asl.a1.fire.vehicle-breakdown-facts-missing", result.Reasons);
    }

    [Fact]
    public void AnUnknownOrContradictoryMaMarkerUnderVersionOneIsRefused()
    {
        Assert.Contains("asl.a1.fire.final-fire-marker-unknown", ScenarioA1FireCalculator.Precheck(FinalFireMaMg(null), Reference));
        Assert.Contains("asl.a1.fire.final-fire-marker-unknown", ScenarioA1FireCalculator.Precheck(FinalFireMaMg((FireMarker)99), Reference));
        // Item 49: fired this Player Turn, no ROF kept, and no counter cannot all be true.
        Assert.Contains("asl.a1.fire.final-fire-marker-contradicts", ScenarioA1FireCalculator.Precheck(FinalFireMaMg(FireMarker.None), Reference));
        Assert.DoesNotContain("asl.a1.fire.final-fire-marker-contradicts", ScenarioA1FireCalculator.Precheck(FinalFireMaMg(FireMarker.None, rofShot: true), Reference));
        Assert.DoesNotContain("asl.a1.fire.final-fire-marker-contradicts", ScenarioA1FireCalculator.Precheck(FinalFireMaMg(FireMarker.None, fired: false), Reference));
    }

    [Theory]
    [InlineData("first-fire", FireMarker.FirstFire)]
    [InlineData("final-fire", FireMarker.FinalFire)]
    [InlineData("none", FireMarker.None)]
    [InlineData("something-else", FireMarker.NoFire)]
    public void VehicleMarkerReadsTheMaCounterAndAnUnknownOneBarsFire(string counter, FireMarker expected)
    {
        Assert.Equal(expected, ScenarioA1FinalFireCalculator.VehicleMarker(counter, true, false, false, false));
    }

    [Fact]
    public void FinalFireFactsAreOmittedFromTheRecordWhenNull()
    {
        var legacy = Shot();
        var element = JsonSerializer.SerializeToElement(legacy);
        Assert.False(element.TryGetProperty(nameof(FireAttack.FinalFireVersion), out _));
        Assert.False(element.GetProperty(nameof(FireAttack.VehicleFire)).TryGetProperty(nameof(FireVehicleFire.FinalFireMarker), out _));
        var current = JsonSerializer.SerializeToElement(FinalFireMaMg(FireMarker.FirstFire));
        Assert.Equal(1, current.GetProperty(nameof(FireAttack.FinalFireVersion)).GetInt32());
        Assert.True(current.GetProperty(nameof(FireAttack.VehicleFire)).TryGetProperty(nameof(FireVehicleFire.FinalFireMarker), out _));
    }

    [Fact]
    public void RecordedWeaponFactsRoundTripAndMissingWeaponFactsAreRefused()
    {
        var legacy = Shot();
        Assert.False(JsonSerializer.SerializeToElement(legacy).TryGetProperty(nameof(FireAttack.VehicleBreakdown), out _));
        var facts = Supplement(("ma", Facts(VehicleBreakdownWeapon.MainArmament, 11, circled: true)));
        var current = legacy with
        {
            VehicleBreakdown = facts
        };
        var roundTrip = JsonSerializer.Deserialize<FireAttack>(JsonSerializer.Serialize(current))!;
        Assert.Equal(JsonSerializer.Serialize(ScenarioA1FireCalculator.Resolve(current, Reference, facts)),
            JsonSerializer.Serialize(ScenarioA1FireCalculator.Resolve(roundTrip, Reference)));
        foreach (var incomplete in new[] { new FireVehicleBreakdownFacts(null!), Supplement(("ma", null!)) })
        {
            var refused = ScenarioA1FireCalculator.Resolve(legacy with
            {
                VehicleBreakdown = incomplete
            }, Reference);
            Assert.Equal(FireResolution.Indeterminate, refused.Disposition);
            Assert.Contains(refused.Reasons, reason => reason.Contains("vehicle-breakdown", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void NewLowAmmoReadingDoesNotChangeTheLegacyAttackOrResolution()
    {
        var attack = Shot();
        var recordedAttack = JsonSerializer.Serialize(attack);
        var legacy = JsonSerializer.Serialize(ScenarioA1FireCalculator.Resolve(attack, Reference));
        var result = Complete(attack, Supplement(("ma", Facts(VehicleBreakdownWeapon.MainArmament, 11, circled: true))));
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        var effect = Assert.Single(result.WeaponEffects!);
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, effect.VehicleBreakdown!.Outcome);
        Assert.False(effect.Malfunctioned);
        Assert.Null(effect.Removed);
        Assert.Equal(recordedAttack, JsonSerializer.Serialize(attack));
        Assert.Equal(legacy, JsonSerializer.Serialize(ScenarioA1FireCalculator.Resolve(attack, Reference)));
        Assert.DoesNotContain("VehicleBreakdown", legacy, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyWeaponsReachingTheirOwnThresholdEnterRandomSelection()
    {
        var result = Complete(Shot(true), Supplement(("bmg", Facts(VehicleBreakdownWeapon.BowMg, 10)),
            ("cmg", Facts(VehicleBreakdownWeapon.CoaxialMg))));
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.True(Assert.Single(result.WeaponEffects!, effect => effect.VehicleWeapon == "bmg").Malfunctioned);
        Assert.False(Assert.Single(result.WeaponEffects!, effect => effect.VehicleWeapon == "cmg").Malfunctioned);
        Assert.All(result.WeaponEffects!, effect => Assert.Null(effect.SelectionDr));
    }

    [Fact]
    public void RandomSelectionPreservesDisabledVersusMalfunctionAndTies()
    {
        var facts = Supplement(("bmg", Facts(VehicleBreakdownWeapon.BowMg, 10, 11)), ("cmg", Facts(VehicleBreakdownWeapon.CoaxialMg, 10)));
        var missing = Complete(Shot(true), facts);
        Assert.Equal(FireResolution.Indeterminate, missing.Disposition);
        Assert.Contains(missing.Reasons, reason => reason.Contains("weaponSelection", StringComparison.Ordinal));
        var attack = Shot(true);
        var result = Complete(attack with
        {
            Rolls = attack.Rolls! with
            {
                WeaponSelection = new Dictionary<string, int> { ["de-v:bmg"] = 6, ["de-v:cmg"] = 6 }
            },
        }, facts);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        var bow = Assert.Single(result.WeaponEffects!, effect => effect.VehicleWeapon == "bmg");
        Assert.Equal(VehicleBreakdownOutcome.Disabled, bow.VehicleBreakdown!.Outcome);
        Assert.False(bow.RateOfFireRetained);
        Assert.Contains((UnitCondition.BmgDisabled, true), ScenarioA1FireFollowUps.WeaponEffectConditions(bow, () => false));
        Assert.True(Assert.Single(result.WeaponEffects!, effect => effect.VehicleWeapon == "cmg").Malfunctioned);
    }

    [Fact]
    public void OverrunUsesItsOwnIftBreakdownBeforeTwelveAndPreservesItsAttack()
    {
        var attack = Shot() with
        {
            Phase = "MPh",
            FireKind = ScenarioA1FireCalculator.OverrunFire,
            FirerLocationId = At,
            Range = 0,
            FireGroupComplete = null,
            Firers = null,
            VehicleFire = null,
            ScenarioMonth = 7,
            Overrun = new FireOverrun("de-v", "attacker-tank", At, false, false, false, false, false),
        };
        var facts = Supplement(("ma", Facts(VehicleBreakdownWeapon.MainArmament, 10, 11)),
            ("bmg", Facts(VehicleBreakdownWeapon.BowMg)), ("cmg", Facts(VehicleBreakdownWeapon.CoaxialMg)));
        var result = Complete(attack, facts);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Equal(16m, result.Arithmetic!.TotalFirepower);
        Assert.Equal(VehicleBreakdownOutcome.Disabled, result.OverrunEffect!.VehicleBreakdowns!["ma"].Outcome);
        Assert.Contains((UnitCondition.Disabled, true), ScenarioA1FireFollowUps.OverrunEffectConditions(result.OverrunEffect));
        Assert.False(result.OverrunEffect.Immobilized);
        Assert.Equal(VehicleBreakdownOutcome.Malfunction,
            Complete(attack, facts with
            {
                OverrunVersion = 1
            }).OverrunEffect!.VehicleBreakdowns!["ma"].Outcome);
        var circled = Supplement(("ma", Facts(VehicleBreakdownWeapon.MainArmament, 11, circled: true)),
            ("bmg", Facts(VehicleBreakdownWeapon.BowMg)), ("cmg", Facts(VehicleBreakdownWeapon.CoaxialMg)));
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, Complete(attack, circled).OverrunEffect!.VehicleBreakdowns!["ma"].Outcome);
        Assert.False(JsonSerializer.SerializeToElement(circled).TryGetProperty(nameof(FireVehicleBreakdownFacts.OverrunVersion), out _));
        Assert.Equal(VehicleBreakdownOutcome.Malfunction,
            Complete(attack, circled with
            {
                OverrunVersion = 1
            }).OverrunEffect!.VehicleBreakdowns!["ma"].Outcome);
        Assert.Equal(FireResolution.Abstained, Complete(attack, circled with
        {
            OverrunVersion = 2
        }).Disposition);
    }

    [Theory]
    [InlineData("ma")]
    [InlineData("bmg")]
    [InlineData("cmg")]
    public void DisabledArmamentDoesNotParticipateInCurrentOverruns(string disabled)
    {
        var legacy = new FireOverrun("de-v", "attacker-tank", At, false, false, false, false, false);
        var current = legacy with
        {
            DisabledWeapons = [disabled]
        };
        var definition = Reference.Definitions["attacker-tank"];
        Assert.Contains(ScenarioA1FireCalculator.OverrunWeapons(legacy, definition), item => item.Weapon == disabled);
        Assert.DoesNotContain(ScenarioA1FireCalculator.OverrunWeapons(current, definition), item => item.Weapon == disabled);
        Assert.Equal(2, ScenarioA1FireCalculator.OverrunWeapons(current, definition).Count);
        Assert.False(JsonSerializer.SerializeToElement(legacy).TryGetProperty(nameof(FireOverrun.DisabledWeapons), out _));
    }

    [Fact]
    public void LowAmmoCanKeepRofButDisabledCannotKeepItOnTheSameColoredDie()
    {
        var attack = Shot() with
        {
            Rolls = new FireRolls([1, 2], null, null, null)
        };
        var low = Complete(attack, Supplement(("ma", Facts(VehicleBreakdownWeapon.MainArmament, 3, circled: true))));
        Assert.Equal(FireResolution.Resolved, low.Disposition);
        Assert.True(Assert.Single(low.WeaponEffects!).RateOfFireRetained);
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, Assert.Single(low.WeaponEffects!).VehicleBreakdown!.Outcome);
        var disabled = Complete(attack, Supplement(("ma", Facts(VehicleBreakdownWeapon.MainArmament, 12, 3))));
        Assert.Equal(FireResolution.Resolved, disabled.Disposition);
        Assert.False(Assert.Single(disabled.WeaponEffects!).RateOfFireRetained);
        Assert.Equal(VehicleBreakdownOutcome.Disabled, Assert.Single(disabled.WeaponEffects!).VehicleBreakdown!.Outcome);
    }

    [Fact]
    public void SupplementalFactsCannotOverrideRecordedLoweringsOrAllowIntensiveFire()
    {
        var ordinary = Facts(VehicleBreakdownWeapon.MainArmament);
        VehicleBreakdownUseFacts[] contradictions =
        [
            ordinary with { Captured = true },
            ordinary with { NonQualified = true },
            ordinary with { WinterReduction = 1 },
            ordinary with { AmmunitionShortage = true },
            ordinary with { IntensiveFire = true },
        ];
        foreach (var facts in contradictions)
        {
            var result = ScenarioA1FireCalculator.Resolve(Shot(), Reference, Supplement(("ma", facts)));
            Assert.Equal(FireResolution.Abstained, result.Disposition);
            Assert.Contains("asl.a1.fire.vehicle-breakdown-facts-conflict", result.Reasons);
        }
        var recorded = Shot() with
        {
            BreakdownReduction = 1,
            AmmunitionShortage = true,
            VehicleFire = Shot().VehicleFire! with
            {
                CapturedPenalty = 2
            },
        };
        var matching = ordinary with
        {
            Captured = true,
            NonQualified = true,
            WinterReduction = 1,
            AmmunitionShortage = true,
        };
        Assert.Equal(FireResolution.Resolved, Complete(recorded, Supplement(("ma", matching))).Disposition);
        Assert.Equal(FireResolution.Abstained, ScenarioA1FireCalculator.Resolve(recorded, Reference, Supplement(("ma", ordinary))).Disposition);
    }

    [Fact]
    public void MalformedSupplementalNumbersRefuseBeforeRequestingAnyDice()
    {
        var attack = Shot() with
        {
            Rolls = new FireRolls(null, null, null, null)
        };
        var ordinary = Facts(VehicleBreakdownWeapon.MainArmament);
        VehicleBreakdownUseFacts[] malformed =
        [
            ordinary with { PrintedB = 1 },
            ordinary with { PrintedX = 13 },
            ordinary with { WinterReduction = 3 },
        ];
        foreach (var facts in malformed)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack, Reference, Supplement(("ma", facts)));
            Assert.Equal(FireResolution.Abstained, result.Disposition);
            Assert.Contains(result.Reasons, reason => reason.Contains("vehicle-breakdown-facts-invalid", StringComparison.Ordinal));
            Assert.DoesNotContain(result.Reasons, reason => reason.Contains("roll-missing", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void SupplementalFactsMustNameExactlyTheParticipatingWeapons()
    {
        var wrong = Supplement(("ma", Facts(VehicleBreakdownWeapon.BowMg)));
        Assert.Equal(FireResolution.Indeterminate, ScenarioA1FireCalculator.Resolve(Shot(), Reference, wrong).Disposition);
        Assert.Equal(FireResolution.Indeterminate, ScenarioA1FireCalculator.Resolve(Shot(), Reference, Supplement()).Disposition);
    }

    [Fact]
    public void OverrunRejectsUnknownDisabledWeaponNamesBeforeDice()
    {
        var attack = Shot() with
        {
            Phase = "MPh",
            FireKind = ScenarioA1FireCalculator.OverrunFire,
            Firers = [],
            VehicleFire = null,
            FirerLocationId = At,
            Range = 0,
            Overrun = new FireOverrun("de-v", "attacker-tank", At, false, false, false, false, false) { DisabledWeapons = ["aamg"] },
            Rolls = new FireRolls(null, null, null, null)
        };
        var result = ScenarioA1FireCalculator.Resolve(attack, Reference);
        Assert.Equal(FireResolution.Abstained, result.Disposition);
        Assert.Contains("asl.a1.fire.overrun-disabled-weapon-unknown", result.Reasons);
    }
}
