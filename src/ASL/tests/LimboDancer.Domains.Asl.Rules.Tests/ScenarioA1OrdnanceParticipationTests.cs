using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1OrdnanceParticipationTests
{
    private static readonly ScenarioA1OrdnanceReference Reference = new ScenarioA1OrdnancePackage().Reference;
    private const string At = "bd01:G5:0";

    [Fact]
    public void DirectorCandidateUsesTheRecordedShotRefusal()
    {
        var director = new FireDirector("director", "attacker-leader-8-0", At, false, false, false, false, false);
        var nationality = Reference.Fire.Definitions[director.DefinitionId!].Nationality;
        Assert.Null(ScenarioA1OrdnanceCalculator.DirectorRefusal(director, "firer", nationality, Reference.Fire));
        Assert.NotNull(ScenarioA1OrdnanceCalculator.DirectorRefusal(director with
        {
            DirectedThisPlayerTurn = true
        }, "firer", nationality, Reference.Fire));
        Assert.NotNull(ScenarioA1OrdnanceCalculator.DirectorRefusal(director with
        {
            Broken = true
        }, "firer", nationality, Reference.Fire));
        Assert.NotNull(ScenarioA1OrdnanceCalculator.DirectorRefusal(director with
        {
            Pinned = true
        }, "firer", nationality, Reference.Fire));
        Assert.NotNull(ScenarioA1OrdnanceCalculator.DirectorRefusal(director, "director", nationality, Reference.Fire));
    }

    [Fact]
    public void ThePrimarySmcCannotReuseParticipationAsPermissionForAnotherWeapon()
    {
        Assert.NotNull(ScenarioA1OrdnanceParticipationCalculator.PrimaryUseRefusal("asl:leader", true, false, false));
        Assert.NotNull(ScenarioA1OrdnanceParticipationCalculator.PrimaryUseRefusal("asl:hero", false, true, false));
        Assert.NotNull(ScenarioA1OrdnanceParticipationCalculator.PrimaryUseRefusal("asl:leader", false, false, true));
        Assert.NotNull(ScenarioA1OrdnanceParticipationCalculator.PrimaryUseRefusal("asl:leader", null, false, false));
        Assert.Null(ScenarioA1OrdnanceParticipationCalculator.PrimaryUseRefusal("asl:leader", false, false, false));
        Assert.Null(ScenarioA1OrdnanceParticipationCalculator.PrimaryUseRefusal("asl:squad", true, false, false));
    }

    private static OrdnanceAdditionalFirer Second => new(new("second", "leader-definition", false, false, false, false, false),
        true, true, true, false, false, false, false, false, false);

    [Theory]
    [InlineData("asl:leader", "asl:leader")]
    [InlineData("asl:leader", "asl:hero")]
    [InlineData("asl:hero", "asl:leader")]
    public void TwoActualSmcAreNonQualifiedUsersAndAreNotALoneMortarFirer(string primary, string secondKind)
    {
        Assert.Null(ScenarioA1OrdnanceParticipationCalculator.CandidateRefusal("first", primary, secondKind, Second));
        var count = ScenarioA1OrdnanceParticipationCalculator.SmcCount(primary, new(1, Second));
        Assert.Equal(2, count);
        Assert.Equal(CrewServedUser.SmcGroup, ScenarioA1NonQualifiedUseCalculator.User(primary, count));
        Assert.True(ScenarioA1MortarCalculator.MultipleRofKept(count == 1));
        Assert.False(ScenarioA1MortarCalculator.FiresAsLight(81, count == 1));
        Assert.Equal(1, ScenarioA1OrdnanceParticipationCalculator.SmcCount(primary, new(1, null)));
        Assert.Equal(1, ScenarioA1OrdnanceParticipationCalculator.SmcCount(primary, null));
    }

    [Fact]
    public void DuplicateEnemyDifferentLevelAndUnknownParticipantsCannotSupplyTheSecondSmc()
    {
        Assert.NotNull(Refusal(Second with
        {
            Crew = Second.Crew! with
            {
                UnitId = "first"
            }
        }));
        Assert.NotNull(Refusal(Second with
        {
            SameSide = false
        }));
        Assert.NotNull(Refusal(Second with
        {
            AtFirer = false
        }));
        Assert.NotNull(Refusal(Second with
        {
            ActiveWithDefinition = false
        }));
        Assert.NotNull(Refusal(Second with
        {
            ActiveWithDefinition = null
        }));
        Assert.NotNull(Refusal(Second with
        {
            SameSide = null
        }));
        Assert.NotNull(Refusal(Second with
        {
            Crew = null
        }));
        Assert.NotNull(ScenarioA1OrdnanceParticipationCalculator.CandidateRefusal("first", "asl:squad", "asl:leader", Second));
        Assert.NotNull(ScenarioA1OrdnanceParticipationCalculator.CandidateRefusal("first", "asl:leader", "asl:squad", Second));
    }

    [Fact]
    public void DirectionSpottingAndAnotherWeaponsUseDoNotCountAsParticipation()
    {
        Assert.NotNull(Refusal(Second with
        {
            DirectedFire = true
        }));
        Assert.NotNull(Refusal(Second with
        {
            Spotted = true
        }));
        Assert.NotNull(Refusal(Second with
        {
            UsedOtherWeapon = true
        }));
        Assert.NotNull(Refusal(Second with
        {
            Crew = Second.Crew! with
            {
                FiredInherentFp = true
            }
        }));
        Assert.NotNull(ScenarioA1OrdnanceParticipationCalculator.CandidateRefusal("first", "asl:leader", "asl:leader", Second, "second"));
        Assert.NotNull(ScenarioA1OrdnanceParticipationCalculator.CandidateRefusal("first", "asl:leader", "asl:leader", Second, spotterId: "second"));
        Assert.NotNull(Refusal(Second with
        {
            DirectedFire = null
        }));
    }

    [Fact]
    public void PinnedParticipantCanFireButBrokenBerserkCapturedMeleeAndTiCannot()
    {
        Assert.Null(Refusal(Second with
        {
            Crew = Second.Crew! with
            {
                Pinned = true
            }
        }));
        Assert.NotNull(Refusal(Second with
        {
            Crew = Second.Crew! with
            {
                Broken = true
            }
        }));
        Assert.NotNull(Refusal(Second with
        {
            Crew = Second.Crew! with
            {
                Berserk = true
            }
        }));
        Assert.NotNull(Refusal(Second with
        {
            Captured = true
        }));
        Assert.NotNull(Refusal(Second with
        {
            Melee = true
        }));
        Assert.NotNull(Refusal(Second with
        {
            Ti = true
        }));
        Assert.NotNull(Refusal(Second with
        {
            Crew = Second.Crew! with
            {
                Pinned = null
            }
        }));
    }

    [Fact]
    public void AbsentParticipationSerializesAsBeforeAndTheSecondFirersConditionsArePreserved()
    {
        var shot = new OrdnanceShot(null, null, null, null, new("first", "leader-definition", false, false, false, false, false),
            null, null, null, null, null, null, null, null);
        Assert.DoesNotContain("Participation", JsonSerializer.Serialize(shot), StringComparison.Ordinal);
        Assert.False(ScenarioA1OrdnanceParticipationCalculator.Pinned(shot));
        var paired = shot with
        {
            Participation = new(1, Second with
            {
                Crew = Second.Crew! with
                {
                    Pinned = true,
                    Cx = true
                }
            })
        };
        var restored = JsonSerializer.Deserialize<OrdnanceShot>(JsonSerializer.Serialize(paired))!;
        Assert.True(ScenarioA1OrdnanceParticipationCalculator.Pinned(restored));
        Assert.True(ScenarioA1OrdnanceParticipationCalculator.Cx(restored));
        Assert.Equal("second", restored.Participation!.Second!.Crew!.UnitId);
    }

    private static string? Refusal(OrdnanceAdditionalFirer second) =>
        ScenarioA1OrdnanceParticipationCalculator.CandidateRefusal("first", "asl:leader", "asl:leader", second);

    [Fact]
    public void AreaResolutionUsesBothSmcAndPreservesTheLegacyLoneReading()
    {
        var solo = Shot(false, false);
        var legacy = ScenarioA1OrdnanceCalculator.Resolve(solo with
        {
            Participation = null
        }, Reference);
        var one = ScenarioA1OrdnanceCalculator.Resolve(solo, Reference);
        var two = ScenarioA1OrdnanceCalculator.Resolve(Shot(false, true), Reference);
        Assert.Equal(OrdnanceResolution.Resolved, legacy.Disposition);
        Assert.Equal(OrdnanceResolution.Resolved, one.Disposition);
        Assert.Equal(OrdnanceResolution.Resolved, two.Disposition);
        Assert.Equal(0, legacy.Gun!.RateOfFire);
        Assert.Equal(0, one.Gun!.RateOfFire);
        Assert.Equal(Reference.Guns["attacker-light-mortar"].RateOfFire, two.Gun!.RateOfFire);
        Assert.True(two.Gun.RateOfFireKept);
    }

    [Fact]
    public void TheCatalog81mmGunKeepsItsGunCaAndNonQualifiedPenaltyForTwoSmc()
    {
        var pair = Shot(true, true);
        var straight = ScenarioA1OrdnanceCalculator.Resolve(pair, Reference);
        Assert.True(straight.Disposition == OrdnanceResolution.Resolved, string.Join("; ", straight.Reasons));
        Assert.Equal(1, straight.Gun!.RateOfFire);
        Assert.Contains(straight.ToHit!.Drm, item => item.Name == "case-h" && item.Value == 2);
        var turned = ScenarioA1OrdnanceCalculator.Resolve(pair with
        {
            HexspinesToTurn = 1
        }, Reference);
        Assert.Equal(OrdnanceResolution.Resolved, turned.Disposition);
        Assert.Equal(1, turned.Gun!.RateOfFire);
        Assert.Contains(turned.ToHit!.Drm, item => item.Name == "case-a:1" && item.Value == 1);
        var solo = ScenarioA1OrdnanceCalculator.Resolve(Shot(true, false), Reference);
        Assert.Equal(OrdnanceResolution.Resolved, solo.Disposition);
        Assert.Equal(0, solo.Gun!.RateOfFire);
        Assert.DoesNotContain(solo.ToHit!.Drm, item => item.Name == "case-h");
    }

    [Fact]
    public void ASecondFirersPinAndCxAffectTheWholeShot()
    {
        var shot = Shot(false, true);
        var second = shot.Participation!.Second!;
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Participation = new(1, second with
            {
                Crew = second.Crew! with
                {
                    Pinned = true,
                    Cx = true
                }
            })
        }, Reference);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal(0, result.Gun!.RateOfFire);
        Assert.Contains(result.ToHit!.Drm, item => item.Name == "case-d" && item.Value == 2);
        Assert.Contains(result.ToHit.Drm, item => item.Name == "cx" && item.Value == 1);
    }

    [Fact]
    public void UnknownMortarMinimumAndUnsupportedParticipationReadingRefuseBeforeResolution()
    {
        var shot = Shot(true, true);
        var unknown = Reference.WithGun(Reference.Guns[shot.Gun!.DefinitionId!] with
        {
            RangeMinimum = null
        });
        Assert.NotEqual(OrdnanceResolution.Resolved, ScenarioA1OrdnanceCalculator.Resolve(shot, unknown).Disposition);
        Assert.NotEqual(OrdnanceResolution.Resolved, ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Range = 2
        }, Reference).Disposition);
        Assert.NotEqual(OrdnanceResolution.Resolved, ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Participation = new(2, shot.Participation!.Second)
        }, Reference).Disposition);
    }

    [Fact]
    public void HeroicParticipationAddsTheSwModifierButNotAGunModifier()
    {
        var light = Shot(false, true);
        var second = light.Participation!.Second! with
        {
            Heroic = true
        };
        var lightResult = ScenarioA1OrdnanceCalculator.Resolve(light with
        {
            Participation = new(1, second)
        }, Reference);
        Assert.Equal(OrdnanceResolution.Resolved, lightResult.Disposition);
        Assert.Contains(lightResult.ToHit!.Drm, item => item.Name == "hero:second" && item.Value == -1);
        var heavyResult = ScenarioA1OrdnanceCalculator.Resolve(Shot(true, true) with
        {
            Participation = new(1, second)
        }, Reference);
        Assert.Equal(OrdnanceResolution.Resolved, heavyResult.Disposition);
        Assert.DoesNotContain(heavyResult.ToHit!.Drm, item => item.Name.StartsWith("hero:", StringComparison.Ordinal));
    }

    [Fact]
    public void AHeroAbove82HasNoRofAndCannotTurnAnNtGunBelowManhandlingNine()
    {
        var original = Shot(true, false);
        var shot = original with
        {
            Phase = "DFPh",
            FiringSide = "non-phasing",
            Hit = original.Hit! with
            {
                Phase = "DFPh",
                FiringSide = "non-phasing"
            },
            HeroWeapons = true,
            UseMultiple = 1,
            HexspinesToTurn = 1
        };
        // Synthetic 120mm NT facts isolate the M# boundary; the catalog's actual 81mm mortar remains unchanged.
        var gun = Reference.Guns[shot.Gun!.DefinitionId!] with
        {
            Caliber = 120,
            Mount360 = false,
            Manhandling = 8
        };
        var refused = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference.WithGun(gun));
        Assert.NotEqual(OrdnanceResolution.Resolved, refused.Disposition);
        Assert.Contains(refused.Reasons, reason => reason.Contains("A15.23", StringComparison.Ordinal));
        var allowed = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference.WithGun(gun with
        {
            Manhandling = 9
        }));
        Assert.Equal(OrdnanceResolution.Resolved, allowed.Disposition);
        Assert.Equal(0, allowed.Gun!.RateOfFire);
        Assert.DoesNotContain(allowed.ToHit!.Drm, item => item.Name.StartsWith("hero:", StringComparison.Ordinal));
    }

    [Fact]
    public void TheNt81mmExceptionKeepsRofAndAPinnedPairCannotTurn()
    {
        var shot = Shot(true, true) with
        {
            HexspinesToTurn = 1
        };
        var reference = Reference.WithGun(Reference.Guns[shot.Gun!.DefinitionId!] with
        {
            Mount360 = false
        });
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot, reference);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal(1, result.Gun!.RateOfFire);
        Assert.Contains(result.ToHit!.Drm, item => item.Name == "case-a:1" && item.Value == 3);
        var second = shot.Participation!.Second!;
        var pinned = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Participation = new(1, second with
            {
                Crew = second.Crew! with
                {
                    Pinned = true
                }
            })
        }, reference);
        Assert.NotEqual(OrdnanceResolution.Resolved, pinned.Disposition);
        Assert.Contains("asl.a1.ordnance.covered-arc-fixed", pinned.Reasons);
    }

    [Fact]
    public void TheParticipationContractRequiresBothOwnersFactsAndNeverTreatsAnUnknownAsPermission()
    {
        var contract = ScenarioA1RuleContracts.Find("c9-ordnance-participation")!;
        var facts = JsonNode.Parse("""
            {"firerId":"first","secondId":"second","primaryKind":"asl:leader","secondKind":"asl:leader",
             "active":true,"sameSide":true,"sameLocation":true,"broken":false,"berserk":false,"pinned":false,
             "firedOther":false,"melee":false,"captured":false,"ti":false,"usedOther":false,"directed":false,"spotted":false,
             "phase":"PFPh","opportunityFire":false}
            """)!.AsObject();
        Assert.Equal("may-participate", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        foreach (var fact in contract.Facts)
        {
            Assert.Equal(fact.Name == "phase" ? ContractDisclosure.All : ContractDisclosure.Owner, fact.Disclosure);
            Assert.Equal(fact.Name == "phase" ? null : fact.Name is "firerId" or "primaryKind" ? "firerId" : "secondId", fact.Concerns);
            var unknown = facts.DeepClone().AsObject();
            unknown[fact.Name] = null;
            Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, unknown).Outcome);
        }
        facts["opportunityFire"] = true;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["phase"] = "AFPh";
        Assert.Equal("may-participate", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["secondId"] = "first";
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
    }

    [Fact]
    public void TheMortarTargetReadUsesItsMinimumAndEffectiveModeWithoutChangingTheOldOverload()
    {
        Assert.Contains("minimum range", ScenarioA1OrdnanceMapRules.GunTargetStatus(2, 60, () => 0, () => 120, 0, "mtr", 81, 3, false));
        Assert.Contains("not known", ScenarioA1OrdnanceMapRules.GunTargetStatus(3, 60, () => 0, () => 120, 0, "mtr", 81, null, false));
        Assert.Contains("no CA", ScenarioA1OrdnanceMapRules.GunTargetStatus(3, 60, () => 6, () => 120, 0, "mtr", 81, 3, true));
        Assert.Contains("Case A", ScenarioA1OrdnanceMapRules.GunTargetStatus(3, 60, () => 6, () => 120, 0, "mtr", 81, 3, false));
        Assert.Contains("C2.6", ScenarioA1OrdnanceMapRules.GunTargetStatus(3, 60, () => -6, () => 120, 0, "mtr", 81, 3, false));
        Assert.Contains("C2.6", ScenarioA1OrdnanceMapRules.GunTargetStatus(3, 60, () => 6, () => 120, 0));
    }

    [Fact]
    public void BothParticipantsMustQualifyForOpportunityFireAndBothMayLoseConcealment()
    {
        var shot = Shot(false, true);
        var second = shot.Participation!.Second!;
        var hidden = second with
        {
            Crew = second.Crew! with
            {
                Concealed = true
            },
            OpportunityFireDeclared = true
        };
        Assert.Contains("A7.25", ScenarioA1OrdnanceParticipationCalculator.CandidateRefusal("first", "asl:leader", "asl:leader", hidden, phase: "PFPh"));
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Participation = new(1, hidden with
            {
                OpportunityFireDeclared = false
            })
        }, Reference);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.True(result.CrewConcealmentLost);
        var opportunity = shot with
        {
            Phase = "AFPh",
            Crew = shot.Crew! with
            {
                OpportunityFire = true
            }
        };
        Assert.False(ScenarioA1OrdnanceCalculator.OpportunityFire(opportunity));
        Assert.True(ScenarioA1OrdnanceCalculator.OpportunityFire(opportunity with
        {
            Participation = new(1, second with
            {
                OpportunityFireDeclared = true
            }),
        }));
        Assert.False(ScenarioA1OrdnanceCalculator.OpportunityFire(opportunity with
        {
            Participation = new(1, second with
            {
                OpportunityFireDeclared = false,
                Crew = second.Crew! with
                {
                    OpportunityFire = true
                },
            }),
        }));
        Assert.True(ScenarioA1OrdnanceCalculator.OpportunityFire(opportunity with
        {
            Participation = null
        }));
        Assert.True(ScenarioA1OrdnanceCalculator.OpportunityFire(opportunity with
        {
            Participation = new(1, second with
            {
                Crew = second.Crew! with
                {
                    OpportunityFire = true
                }
            })
        }));
        Assert.NotNull(ScenarioA1OrdnanceParticipationCalculator.WeaponRefusal("asl:latw"));
        Assert.Null(ScenarioA1OrdnanceParticipationCalculator.WeaponRefusal("asl:gun"));
    }

    [Fact]
    public void TheSecondGunFirerAlsoLosesConcealmentOnTheVehicleTargetType()
    {
        var original = Shot(false, true);
        var second = original.Participation!.Second!;
        var shot = original with
        {
            Gun = original.Gun! with
            {
                DefinitionId = "defender-at-gun"
            },
            TargetType = null,
            VehicleTarget = new OrdnanceVehicleTarget("tank", "defender-tank", "front", "front", false, false, false, false, true),
            Ammunition = "ap",
            ScenarioYear = 1942,
            UseMultiple = 2,
            Hit = original.Hit! with
            {
                Targets = []
            },
            Participation = new(1, second with
            {
                Crew = second.Crew! with
                {
                    Concealed = true
                }
            }),
            Rolls = new OrdnanceRolls([3, 4], null, null, null, null)
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.True(result.CrewConcealmentLost);
        var unseen = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            CrewSeen = false
        }, Reference);
        Assert.Null(unseen.CrewConcealmentLost);
    }

    [Fact]
    public void BoreSightingAppliesOnlyToTheSelectedLocationOfAnAreaShot()
    {
        const string upper = "bd01:G5:1";
        var original = Shot(true, true);
        var shot = original with
        {
            BoreSighted = true,
            Acquisition = -1,
            AreaLocations =
            [
                new(At) { TargetTerrain = "stone-building", Los = original.Hit!.Los, LevelsAbove = 1 },
                new(upper) { TargetTerrain = "stone-building", Los = original.Hit.Los, LevelsAbove = 0 },
            ],
            Hit = original.Hit with
            {
                Targets = [original.Hit.Targets![0], original.Hit.Targets[0] with { UnitId = "upper", LocationId = upper }],
            },
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        var ground = Assert.Single(result.AreaTargets!, target => target.UnitId == "target");
        Assert.Contains(ground.Drm, item => item.Name == "case-m" && item.Value == -2);
        Assert.DoesNotContain(ground.Drm, item => item.Name == "case-n");
        var above = Assert.Single(result.AreaTargets!, target => target.UnitId == "upper");
        Assert.DoesNotContain(above.Drm, item => item.Name == "case-m");
        Assert.Contains(above.Drm, item => item.Name == "case-n" && item.Value == -1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoSmcCannotForgeTheLoneSmcsZeroUseMultiple(bool history)
    {
        var shot = Shot(true, true);
        shot = shot with
        {
            Participation = shot.Participation! with
            {
                UseHistory = history ? new(false, false) : null
            }
        };
        var bad = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            UseMultiple = 0
        }, Reference);
        Assert.NotEqual(OrdnanceResolution.Resolved, bad.Disposition);
        Assert.Contains(bad.Reasons, reason => reason.Contains("participation-use", StringComparison.Ordinal));
        Assert.Equal(OrdnanceResolution.Resolved, ScenarioA1OrdnanceCalculator.Resolve(shot, Reference).Disposition);
    }

    [Fact]
    public void RecordedCaptureAndPriorUseAreValidatedByTheUseCalculator()
    {
        var pair = Shot(true, true);
        pair = pair with
        {
            Participation = pair.Participation! with
            {
                UseHistory = new(true, false)
            }
        };
        Assert.NotEqual(OrdnanceResolution.Resolved, ScenarioA1OrdnanceCalculator.Resolve(pair, Reference).Disposition);
        Assert.Equal(OrdnanceResolution.Resolved, ScenarioA1OrdnanceCalculator.Resolve(pair with
        {
            UseMultiple = 2
        }, Reference).Disposition);
        var solo = Shot(true, false);
        solo = solo with
        {
            Phase = "DFPh",
            FiringSide = "non-phasing",
            HeroWeapons = true,
            UseMultiple = 1,
            Hit = solo.Hit! with
            {
                Phase = "DFPh",
                FiringSide = "non-phasing"
            },
            Participation = solo.Participation! with
            {
                UseHistory = new(false, true)
            },
        };
        var reference = Reference.WithGun(Reference.Guns[solo.Gun!.DefinitionId!] with
        {
            Caliber = 120
        });
        var bad = ScenarioA1OrdnanceCalculator.Resolve(solo, reference);
        Assert.NotEqual(OrdnanceResolution.Resolved, bad.Disposition);
        Assert.Contains(bad.Reasons, reason => reason.Contains("once per Game Turn", StringComparison.Ordinal));
        Assert.DoesNotContain("UseHistory", JsonSerializer.Serialize(new OrdnanceParticipation(1, null)));
    }

    [Theory]
    [InlineData("mtr")]
    [InlineData("mortar")]
    public void InexperiencedAndShortageSwPenaltiesUseTheEffectiveMortarMode(string gunType)
    {
        var green = Reference.Fire.Definitions.Values.First(item => item.Kind == "asl:squad" && item.Class == "green");
        var original = Shot(true, false);
        var shot = original with
        {
            Crew = original.Crew! with
            {
                DefinitionId = green.Id
            },
            UseMultiple = 1,
            AmmunitionShortage = true,
        };
        var gun = Reference.Guns[shot.Gun!.DefinitionId!] with
        {
            GunType = gunType
        };
        var heavy = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference.WithGun(gun));
        Assert.Equal(OrdnanceResolution.Resolved, heavy.Disposition);
        Assert.Equal(gun.Breakdown - 2, heavy.Gun!.BreakdownNumber);
        var light = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            UseMultiple = 0
        }, Reference.WithGun(gun with
        {
            Caliber = 50
        }));
        Assert.Equal(OrdnanceResolution.Resolved, light.Disposition);
        Assert.Equal(gun.Breakdown - 2, light.Gun!.BreakdownNumber);
        var captured = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Participation = shot.Participation! with
            {
                UseHistory = new(true, false)
            },
        }, Reference.WithGun(gun with
        {
            Caliber = 50
        }));
        Assert.Equal(OrdnanceResolution.Resolved, captured.Disposition);
        Assert.Equal(gun.Breakdown - 3, captured.Gun!.BreakdownNumber);
    }

    [Theory]
    [InlineData(3, 1, null)]
    [InlineData(1, 0, "intensive-fire")]
    public void NonExemptNtMortarLowersRofOrLeavesAnIntensiveFireCounter(int printedRof, int expectedRof, string? counter)
    {
        var shot = Shot(true, true) with
        {
            HexspinesToTurn = 1
        };
        var gun = Reference.Guns[shot.Gun!.DefinitionId!] with
        {
            Caliber = 120,
            Mount360 = false,
            RateOfFire = printedRof
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference.WithGun(gun));
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal(expectedRof, result.Gun!.RateOfFire);
        Assert.Equal(counter, result.Gun.FireCounter);
    }

    [Fact]
    public void MortarMayTurnAgainInWoodsAndIntensiveFireUsesCaseF()
    {
        var original = Shot(true, true);
        var shot = original with
        {
            FirerInWoodsOrBuilding = true,
            HexspinesToTurn = 1,
            Gun = original.Gun! with
            {
                ShotsThisPhase = 1,
                RateOfFireKept = true,
                FiredThisPlayerTurn = true
            },
        };
        Assert.Equal(OrdnanceResolution.Resolved, ScenarioA1OrdnanceCalculator.Resolve(shot, Reference).Disposition);
        var legacy = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Participation = null
        }, Reference);
        Assert.Contains("asl.a1.ordnance.gun-outside", legacy.Reasons);
        var intensive = ScenarioA1OrdnanceCalculator.Resolve(original with
        {
            IntensiveFire = true,
            Gun = original.Gun! with
            {
                ShotsThisPhase = 1,
                FiredThisPlayerTurn = true
            },
        }, Reference);
        Assert.Equal(OrdnanceResolution.Resolved, intensive.Disposition);
        Assert.Contains(intensive.ToHit!.Drm, item => item.Name == "case-f" && item.Value == 2);
    }

    [Fact]
    public void SecondaryEncirclementAndHeroApplyWithoutStackingTwoHeroToHitModifiers()
    {
        var shot = Shot(false, true);
        var second = shot.Participation!.Second!;
        shot = shot with
        {
            StunEncircledReading = true,
            Participation = shot.Participation with
            {
                Second = second with
                {
                    Heroic = true,
                    Crew = second.Crew! with
                    {
                        Encircled = true
                    }
                },
            },
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Contains(result.ToHit!.Drm, item => item.Name == "hero:second" && item.Value == -1);
        Assert.Contains(result.ToHit.Drm, item => item.Name == "encircled" && item.Value == 1);
        var both = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            HeroWeapons = true
        }, Reference);
        Assert.Equal(OrdnanceResolution.Resolved, both.Disposition);
        Assert.Equal(-1, Assert.Single(both.ToHit!.Drm, item => item.Name.StartsWith("hero:", StringComparison.Ordinal)).Value);
    }

    [Theory]
    [InlineData(false, false, 1, 0, "intensive-fire")]
    [InlineData(true, false, 1, 0, "intensive-fire")]
    [InlineData(false, true, 3, 2, null)]
    [InlineData(true, true, 3, 2, null)]
    public void DirectAndCanisterUseTheSameNtOnlyConditionalRof(bool canister, bool turreted, int printedRof, int expectedRof, string? counter)
    {
        var original = Shot(false, true);
        var shot = original with
        {
            Gun = original.Gun! with
            {
                DefinitionId = "american-at-gun"
            },
            TargetType = null,
            UseMultiple = 1,
            HexspinesToTurn = 1,
            Range = 2,
            ScenarioYear = 1943,
            Ammunition = canister ? "canister" : "he",
            Canister = canister ? new OrdnanceCanister(4, "west",
                [new("bd01:D4:0"), new("bd01:D5:0"), new("bd01:C5:0"), new("bd01:E5:0") { AdjacentHexes = 1 }]) : null,
            Rolls = canister ? new OrdnanceRolls(null, null, null, null, null) { Canister = [1, 3] } : original.Rolls,
            Hit = canister ? original.Hit! with
            {
                Targets = []
            } : original.Hit,
        };
        var gun = Reference.Guns[shot.Gun.DefinitionId!] with
        {
            MaType = turreted ? "t" : null,
            RateOfFire = printedRof,
            SpecialAmmo = ["C12"]
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference.WithGun(gun));
        Assert.True(result.Disposition == OrdnanceResolution.Resolved, string.Join(", ", result.Reasons));
        Assert.Equal(expectedRof, result.Gun!.RateOfFire);
        Assert.Equal(counter, result.Gun.FireCounter);
        if (turreted)
        {
            var legacy = ScenarioA1OrdnanceCalculator.Resolve(shot with
            {
                Participation = null
            }, Reference.WithGun(gun));
            Assert.Equal(OrdnanceResolution.Resolved, legacy.Disposition);
            Assert.Equal(expectedRof - 1, legacy.Gun!.RateOfFire);
        }
    }

    private static OrdnanceShot Shot(bool heavy, bool pair)
    {
        var second = Second with
        {
            Crew = Second.Crew! with
            {
                DefinitionId = "attacker-leader-8-0"
            }
        };
        return new("PFPh", "phasing", "german",
            new OrdnanceGun("mortar", heavy ? "campaign-german-81mm-mortar" : "attacker-light-mortar", false, 0, false, false),
            new OrdnanceCrew("first", "attacker-leader-8-0", false, false, false, false, false), At, 5, 0, false, true, 0,
            new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground",
                [new FireTarget("target", "defender-squad", At, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], 2, null),
            new OrdnanceRolls([1, 6], null, null, new FireRolls([6, 6], null, null, null), null))
        {
            TargetType = OrdnanceTargetTypes.Area,
            UseMultiple = heavy && pair ? 1 : 0,
            Participation = new(1, pair ? second : null)
            {
                UseHistory = new(false, false)
            },
        };
    }
}
