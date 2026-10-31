using System.Reflection;
using System.Text.Json;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>D3.7/D3.71 (PDF pp. 201-202): opt-in shot composition and the unchanged recorded reading.</summary>
public sealed class ScenarioA1OrdnanceBreakdownAdapterTests
{
    private const string At = "bd01:G5:0";
    private static readonly ScenarioA1OrdnanceReference Original = new ScenarioA1OrdnancePackage().Reference;

    // Synthetic numbers and ROF make the outcome boundaries visible; they are not assertions about the historical tank.
    private static readonly GunDefinition Gun = Original.Guns["attacker-tank"] with { Breakdown = 11, RateOfFire = 6 };
    private static readonly ScenarioA1OrdnanceReference Reference = Original.WithGun(Gun);
    private static readonly VehicleBreakdownUseFacts Circled = new(VehicleBreakdownWeapon.MainArmament, 11, null, true, false, false, false);

    private static OrdnanceShot Shot(bool vehicleTarget = false, int[]? dice = null) => new("PFPh", "phasing", "german",
        new OrdnanceGun("tank", Gun.Id, false, 0, false, false),
        new OrdnanceCrew("tank", Gun.Id, false, false, false, false, false), At, 3, 0, false, true, 0,
        new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7,
            "open-ground", vehicleTarget ? [] : [new FireTarget("squad", "defender-squad", At, false, false, false, false, false, false, false)
            { KnownEnemyInLos = true, Captors = [] }], 2, null),
        new OrdnanceRolls(dice ?? [5, 6], null, null, null, null) { ToKill = [6, 6] })
    {
        Vehicle = new OrdnanceVehicleFirer(true, false, false, false, false),
        VehicleTarget = vehicleTarget ? new OrdnanceVehicleTarget("target", "defender-tank", "front", "front", false, false, false, false, true) : null,
        Ammunition = vehicleTarget ? "ap" : null,
        ScenarioYear = 1942,
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecordedVehicleFactsChooseTheCurrentReadingAndNullKeepsTheLegacy(bool vehicleTarget)
    {
        var legacy = Shot(vehicleTarget);
        Assert.False(JsonSerializer.SerializeToElement(legacy).TryGetProperty(nameof(OrdnanceShot.VehicleBreakdown), out _));
        var current = legacy with
        {
            VehicleBreakdown = Circled
        };
        var roundTrip = JsonSerializer.Deserialize<OrdnanceShot>(JsonSerializer.Serialize(current))!;
        Assert.Equal(Circled, roundTrip.VehicleBreakdown);
        Assert.Equal(JsonSerializer.Serialize(ScenarioA1OrdnanceCalculator.Resolve(current, Reference, Circled)),
            JsonSerializer.Serialize(ScenarioA1OrdnanceCalculator.Resolve(roundTrip, Reference)));
        Assert.Equal(ScenarioA1OrdnanceCalculator.Precheck(current, Reference, Circled),
            ScenarioA1OrdnanceCalculator.Precheck(roundTrip, Reference));
        Assert.Null(ScenarioA1OrdnanceCalculator.Resolve(legacy, Reference).Gun!.VehicleBreakdown);
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, ScenarioA1OrdnanceCalculator.Resolve(roundTrip, Reference).Gun!.VehicleBreakdown!.Outcome);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LowAmmoKeepsRofAndAcquisitionButDisabledDoesNot(bool vehicleTarget)
    {
        var shot = Shot(vehicleTarget);
        var low = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference, Circled);
        Assert.Equal(OrdnanceResolution.Resolved, low.Disposition);
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, low.Gun!.VehicleBreakdown!.Outcome);
        Assert.False(low.Gun.Malfunctioned);
        Assert.True(low.Gun.RateOfFireKept);
        Assert.Equal(-1, low.Gun.Acquisition);

        var disabled = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference, Circled with
        {
            LowAmmo = true
        });
        Assert.Equal(OrdnanceResolution.Resolved, disabled.Disposition);
        Assert.Equal(VehicleBreakdownOutcome.Disabled, disabled.Gun!.VehicleBreakdown!.Outcome);
        Assert.False(disabled.Gun.Malfunctioned);
        Assert.False(disabled.Gun.RateOfFireKept);
        Assert.Equal(0, disabled.Gun.Acquisition);

        var malfunction = ScenarioA1OrdnanceCalculator.Resolve(Shot(vehicleTarget, [4, 6]), Reference, Circled with
        {
            LowAmmo = true
        });
        Assert.Equal(VehicleBreakdownOutcome.Malfunction, malfunction.Gun!.VehicleBreakdown!.Outcome);
        Assert.True(malfunction.Gun.Malfunctioned);
        Assert.False(malfunction.Gun.RateOfFireKept);

        var legacy = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference);
        Assert.True(legacy.Gun!.Malfunctioned);
        Assert.Null(legacy.Gun.VehicleBreakdown);
        Assert.DoesNotContain("VehicleBreakdown", JsonSerializer.Serialize(legacy), StringComparison.Ordinal);
        Assert.Equal(JsonSerializer.Serialize(legacy), JsonSerializer.Serialize(ScenarioA1OrdnanceCalculator.Resolve(shot, Reference)));
    }

    [Fact]
    public void CurrentVehicleGunMayIntensiveFinalFireWithItsOwnBreakdownFacts()
    {
        var plain = Shot();
        var shot = plain with
        {
            Phase = "DFPh",
            FiringSide = "non-phasing",
            Range = 1,
            FinalFireVersion = 1,
            IntensiveFire = true,
            Gun = plain.Gun! with
            {
                FirstFire = true,
                FiredThisPlayerTurn = true
            },
            Hit = plain.Hit! with
            {
                Phase = "DFPh",
                FiringSide = "non-phasing"
            },
        };
        var facts = Circled with
        {
            Circled = false,
            IntensiveFire = true
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference, facts);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal(9, result.Gun!.BreakdownNumber);
        Assert.Equal(VehicleBreakdownOutcome.Disabled, result.Gun.VehicleBreakdown!.Outcome);
        Assert.False(result.Gun.RateOfFireKept);
        Assert.Contains("asl.a1.ordnance.gun-already-fired", ScenarioA1OrdnanceCalculator.Precheck(shot with
        {
            FinalFireVersion = null
        }, Reference));
        Assert.Contains(ScenarioA1OrdnanceCalculator.Precheck(shot with
        {
            Range = 3
        }, Reference),
            reason => reason.StartsWith("asl.a1.ordnance.final-fire:", StringComparison.Ordinal));
        Assert.Contains("asl.a1.ordnance.gun-already-fired", ScenarioA1OrdnanceCalculator.Precheck(shot with
        {
            Vehicle = shot.Vehicle! with
            {
                Shocked = true
            }
        }, Reference));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AbsentSpecialAmmoStillRecordsLowAmmoAndTheFailureException(bool vehicleTarget)
    {
        var reference = Original.WithGun(Gun with
        {
            RateOfFire = 2,
            SpecialAmmo = [vehicleTarget ? "AP7" : "HE7"]
        });
        var shot = Shot(vehicleTarget) with
        {
            Ammunition = vehicleTarget ? "ap" : "he"
        };
        var low = ScenarioA1OrdnanceCalculator.Resolve(shot, reference, Circled);
        Assert.Equal(OrdnanceResolution.Resolved, low.Disposition);
        Assert.Equal("none", low.AmmunitionUse);
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, low.Gun!.VehicleBreakdown!.Outcome);
        Assert.False(low.Gun.Malfunctioned);
        Assert.False(low.Gun.RateOfFireKept);
        Assert.Equal("prep-fire", low.Gun.FireCounter);
        Assert.Equal(-1, low.Gun.Acquisition);
        Assert.False(low.ToHit!.Hit);
        Assert.Null(low.Hit);
        Assert.Null(low.Kill);

        var unused = ScenarioA1OrdnanceCalculator.Resolve(shot, reference, Circled with
        {
            PrintedB = 12,
            Circled = false
        });
        Assert.Equal(OrdnanceResolution.Resolved, unused.Disposition);
        Assert.Equal("none", unused.AmmunitionUse);
        Assert.Null(unused.Gun!.VehicleBreakdown);
        Assert.True(unused.Gun.RateOfFireKept);
        Assert.Null(unused.Gun.FireCounter);
        Assert.False(unused.ToHit!.Hit);

        var failed = ScenarioA1OrdnanceCalculator.Resolve(shot, reference, Circled with
        {
            LowAmmo = true
        });
        Assert.Equal(OrdnanceResolution.Resolved, failed.Disposition);
        Assert.Equal("none", failed.AmmunitionUse);
        Assert.Equal(VehicleBreakdownOutcome.Disabled, failed.Gun!.VehicleBreakdown!.Outcome);
        Assert.False(failed.Gun.RateOfFireKept);
        Assert.Null(failed.Hit);
        Assert.Null(failed.Kill);
    }

    [Fact]
    public void CanisterUsesItsIftRollAndRetainsLowAmmoWhenNoRoundWasAvailable()
    {
        var gun = Gun with
        {
            Caliber = 75,
            SpecialAmmo = ["C12"]
        };
        var reference = Original.WithGun(gun);
        var basis = Shot();
        var shot = basis with
        {
            Range = 2,
            Ammunition = "canister",
            // Canister declares its affected Locations separately, not an Infantry Target Type target list.
            Hit = basis.Hit! with
            {
                Targets = []
            },
            Canister = new OrdnanceCanister(4, "west", [new("bd01:F5:0"), new("bd01:F6:0"), new("bd01:G5:0"), new("bd01:E5:0") { AdjacentHexes = 1 }]),
            Rolls = new OrdnanceRolls(null, null, null, null, null) { Canister = [5, 6] },
        };
        var low = ScenarioA1OrdnanceCalculator.Resolve(shot, reference, Circled);
        Assert.Equal(OrdnanceResolution.Resolved, low.Disposition);
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, low.Gun!.VehicleBreakdown!.Outcome);
        Assert.True(low.Gun.RateOfFireKept);
        Assert.NotEmpty(low.Canister!.Attacks);

        var disabled = ScenarioA1OrdnanceCalculator.Resolve(shot, reference, Circled with
        {
            LowAmmo = true
        });
        Assert.Equal(VehicleBreakdownOutcome.Disabled, disabled.Gun!.VehicleBreakdown!.Outcome);
        Assert.False(disabled.Gun.Malfunctioned);
        Assert.False(disabled.Gun.RateOfFireKept);

        var absent = ScenarioA1OrdnanceCalculator.Resolve(shot, Original.WithGun(gun with
        {
            SpecialAmmo = ["C7"]
        }), Circled);
        Assert.Equal("none", absent.AmmunitionUse);
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, absent.Gun!.VehicleBreakdown!.Outcome);
        Assert.False(absent.Gun.Malfunctioned);
        Assert.Empty(absent.Canister!.Attacks);
    }

    [Fact]
    public void AreaUsesOneActualSharedEffectsRollAndIgnoresEffectsOnAMiss()
    {
        var shot = Shot(dice: [1, 2]) with
        {
            TargetType = OrdnanceTargetTypes.Area,
            Rolls = new OrdnanceRolls([1, 2], null, null, new FireRolls([5, 6], null, null, null), null),
        };
        var low = Area(shot, Circled);
        Assert.Equal(OrdnanceResolution.Resolved, low.Disposition);
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, low.Gun!.VehicleBreakdown!.Outcome);
        Assert.True(low.Gun.RateOfFireKept);
        Assert.Equal(-1, low.Gun.Acquisition);

        var disabled = Area(shot, Circled with
        {
            LowAmmo = true
        });
        Assert.Equal(VehicleBreakdownOutcome.Disabled, disabled.Gun!.VehicleBreakdown!.Outcome);
        Assert.False(disabled.Gun.RateOfFireKept);
        Assert.Equal(0, disabled.Gun.Acquisition);

        var both = Area(shot with
        {
            Rolls = shot.Rolls! with
            {
                Hit = new FireRolls([6, 6], null, null, null)
            }
        }, Circled with
        {
            PrintedB = 3
        });
        Assert.Equal(VehicleBreakdownOutcome.Malfunction, both.Gun!.VehicleBreakdown!.Outcome);
        Assert.True(both.Gun.VehicleBreakdown.LowAmmoGained);
        Assert.False(both.Gun.RateOfFireKept);

        var miss = Area(shot with
        {
            Rolls = shot.Rolls! with
            {
                ToHit = [5, 6],
                Hit = new FireRolls([6, 6], null, null, null)
            }
        }, Circled);
        Assert.False(miss.ToHit!.Hit);
        Assert.Equal(VehicleBreakdownOutcome.LowAmmo, miss.Gun!.VehicleBreakdown!.Outcome);

        var pending = Area(shot with
        {
            Rolls = shot.Rolls! with
            {
                Hit = null
            }
        }, Circled);
        Assert.Equal(OrdnanceResolution.Indeterminate, pending.Disposition);
        Assert.Contains("asl.a1.ordnance.roll-missing:hit:attack", pending.Reasons);
        Assert.Null(pending.Gun);

        foreach (var malformed in new int[][] { [6], [0, 6], [1, 7], [1, 2, 3] })
        {
            var refused = Area(shot with
            {
                Rolls = shot.Rolls! with
                {
                    Hit = new FireRolls(malformed, null, null, null)
                }
            }, Circled);
            Assert.Equal(OrdnanceResolution.Abstained, refused.Disposition);
            Assert.Equal(["asl.a1.ordnance.roll-malformed"], refused.Reasons);
            Assert.Null(refused.Gun);
        }

        // The adapter does not broaden the existing public admission of vehicle Area Target Type shots.
        Assert.Contains("asl.a1.ordnance.target-type-outside", ScenarioA1OrdnanceCalculator.Resolve(shot, Reference, Circled).Reasons);
    }

    private static OrdnanceResolution Area(OrdnanceShot shot, VehicleBreakdownUseFacts facts)
    {
        // Exercise the future internal Area adapter without publishing unsupported vehicle ATT admission or a new production API.
        var type = typeof(ScenarioA1OrdnanceCalculator).Assembly.GetType("LimboDancer.Domains.Asl.Rules.ScenarioA1AreaCalculator")!;
        var method = type.GetMethod("Run", BindingFlags.Public | BindingFlags.Static, null,
            [typeof(OrdnanceShot), typeof(GunDefinition), typeof(ScenarioA1OrdnanceReference), typeof(VehicleBreakdownUseFacts)], null)!;
        return (OrdnanceResolution)method.Invoke(null, [shot, Gun, Reference, facts])!;
    }

    [Fact]
    public void SupplementalFactsCannotChangeTheWeaponOrOverrideTheRecordedUse()
    {
        var shot = Shot();
        var wrongWeapon = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference, Circled with
        {
            Weapon = VehicleBreakdownWeapon.BowMg
        });
        Assert.Equal(OrdnanceResolution.Abstained, wrongWeapon.Disposition);
        Assert.Equal(["asl.a1.ordnance.vehicle-breakdown-weapon"], wrongWeapon.Reasons);
        var infantryGun = shot with
        {
            Vehicle = null,
            Gun = shot.Gun! with
            {
                DefinitionId = "attacker-inf-gun"
            },
            Crew = shot.Crew! with
            {
                DefinitionId = "attacker-crew"
            },
        };
        Assert.Equal(["asl.a1.ordnance.vehicle-breakdown-weapon"], ScenarioA1OrdnanceCalculator.Resolve(infantryGun, Reference, Circled).Reasons);
        var missingRoll = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Rolls = shot.Rolls! with
            {
                ToHit = null
            }
        }, Reference, Circled);
        Assert.Equal(OrdnanceResolution.Indeterminate, missingRoll.Disposition);
        Assert.Equal(["asl.a1.ordnance.roll-missing:toHit"], missingRoll.Reasons);
        Assert.Null(missingRoll.Gun);
        foreach (var invalid in new[] { Circled with { PrintedB = 13 }, Circled with { PrintedX = 1 }, Circled with { WinterReduction = 3 } })
        {
            var refused = ScenarioA1OrdnanceCalculator.Resolve(shot with
            {
                Rolls = shot.Rolls! with
                {
                    ToHit = null
                }
            }, Reference, invalid);
            Assert.Equal(OrdnanceResolution.Abstained, refused.Disposition);
            Assert.StartsWith("asl.a1.ordnance.vehicle-breakdown-facts-invalid:", Assert.Single(refused.Reasons), StringComparison.Ordinal);
            Assert.Equal(refused.Reasons, ScenarioA1OrdnanceCalculator.Precheck(shot, Reference, invalid));
        }

        // Mounted Temporary crews record the multiplier, while NonQualified is an equipment-only legacy fact.
        var temporary = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            CapturedPenalty = 1
        }, Reference, Circled with
        {
            NonQualified = true
        });
        Assert.Equal(OrdnanceResolution.Resolved, temporary.Disposition);
        Assert.Equal(9, temporary.Gun!.VehicleBreakdown!.Numbers.BNumber);
        foreach (var conflicting in new[]
        {
            Circled with { Captured = true }, Circled with { NonQualified = true }, Circled with { WinterReduction = 1 },
            Circled with { IntensiveFire = true }, Circled with { AmmunitionShortage = true },
        })
        {
            var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference, conflicting);
            Assert.Equal(OrdnanceResolution.Abstained, result.Disposition);
            Assert.Equal(["asl.a1.ordnance.vehicle-breakdown-facts-conflict"], result.Reasons);
            Assert.Equal(result.Reasons, ScenarioA1OrdnanceCalculator.Precheck(shot, Reference, conflicting));
        }
    }
}
