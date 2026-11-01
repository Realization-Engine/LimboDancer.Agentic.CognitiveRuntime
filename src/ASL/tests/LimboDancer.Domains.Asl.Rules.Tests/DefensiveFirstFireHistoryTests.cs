using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using C = LimboDancer.Domains.Asl.Rules.ScenarioA1DefensiveFirstFireHistoryCalculator;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>C6.17, pp. 173-174: per-personnel-target allowance, recovery and retained Location history.</summary>
public sealed class DefensiveFirstFireHistoryTests
{
    private const string Phase = "turn1-russian-MPh";
    private const string At = "bd01:G5:0";
    private static readonly ScenarioA1OrdnanceReference Reference = new ScenarioA1OrdnancePackage().Reference;
    private static DefensiveFirstFireHistory Start(string id = "r1", int total = 0) =>
        C.Start(Phase, id, At, total, DefensiveFirstFireRelationship.OrdinaryPersonnel);
    private static DefensiveFirstFireHistory Spend(DefensiveFirstFireHistory h, int total, string at = At) =>
        C.Observe(h, new(Phase, h.TargetId, h.ActiveLocation, at, h.LastTotal, total));
    private static DefensiveFirstFireTarget Read(DefensiveFirstFireHistory h, string gun = "gun1", bool window = true) =>
        C.Read(h, Phase, h.ActiveLocation, gun, h.LastTotal, window);
    private static DefensiveFirstFireHistory Shot(DefensiveFirstFireHistory h, string gun = "gun1") =>
        C.RecordShot(h, Phase, h.ActiveLocation, gun, true, [h.TargetId]);

    [Fact]
    public void RecoveryKeepsEachTargetsOwnExpenditureAndWeaponShots()
    {
        // A4.2 p. 49: A completes movement before B starts; A4.44 p. 50: Recovery spends 1 MF, including a failed attempt.
        var a = Shot(Spend(Start(), 2));
        var b = Spend(Start("r2"), 4);
        Assert.Equal(0, C.Allowance(Read(a)).Remaining);
        Assert.Equal(2, C.Allowance(Read(b)).Remaining);
        b = Shot(b);
        Assert.Equal(1, C.Allowance(Read(b)).Remaining);
        Assert.Equal(2, C.Allowance(Read(b, "gun2")).Remaining);
        Assert.NotNull(C.Allowance(Read(Shot(b))).Refusal);
        Assert.Equal(0, Read(a, "gun2").ShotsHere);
    }

    [Fact]
    public void JointShotsCountOnceForEachAttackedMemberButNonShotsDoNot()
    {
        var a = Spend(Start(), 4);
        var b = Spend(Start("r2"), 4);
        a = C.RecordShot(a, Phase, At, "gun1", true, ["r1", "r2"]);
        b = C.RecordShot(b, Phase, At, "gun1", true, ["r1", "r2"]);
        Assert.Equal(1, Read(a).ShotsHere);
        Assert.Equal(1, Read(b).ShotsHere);
        Assert.Same(a, C.RecordShot(a, Phase, At, "gun1", false, ["r1"]));
        Assert.Same(a, C.RecordShot(a, Phase, At, "gun1", true, ["r2"]));
        Assert.False(C.RecordShot(a, Phase, At, "gun1", null, ["r1"]).Known);
    }

    [Fact]
    public void ReturnRetainsFullLocationHistoryAndChargesEntryOnlyToDestination()
    {
        var h = Shot(Spend(Start(), 4));
        h = Spend(h, 6, "bd01:G6:0");
        Assert.Equal(2, Read(h).HalfMfSpent);
        Assert.Equal(0, Read(h).ShotsHere);
        h = Spend(h, 8);
        Assert.Equal(6, Read(h).HalfMfSpent);
        Assert.Equal(1, Read(h).ShotsHere);
        Assert.Equal(2, C.Allowance(Read(h)).Remaining);
        h = Spend(h, 10, "bd01:G5:1");
        Assert.Equal(2, Read(h).HalfMfSpent);
        h = Spend(h, 12, "bd02:G5:0");
        Assert.Equal(2, Read(h).HalfMfSpent);
        Assert.Equal(0, Read(h).ShotsHere);
    }

    [Fact]
    public void OneToOneReplacementInheritsShotAndRebasesItsCounter()
    {
        // A7.302 p. 55 and A19.13 p. 86: fold the causing shot before replacing the continuing target.
        var predecessor = Shot(Spend(Start(), 4));
        var successor = C.ContinueTarget(predecessor, "r1-hs", At, 0, true);
        Assert.Equal("r1-hs", Read(successor).TargetId);
        Assert.Equal(4, Read(successor).HalfMfSpent);
        Assert.Equal(1, Read(successor).ShotsHere);
        Assert.Equal(6, Read(Spend(successor, 2)).HalfMfSpent);
        Assert.False(C.ContinueTarget(predecessor, "ambiguous", At, 0, null).Known);
        Assert.False(C.ContinueTarget(predecessor, "elsewhere", "bd01:G6:0", 0, true).Known);
    }

    [Fact]
    public void HalfMfRoundsDownWithMinimumOneButHistoryCannotReopenWindow()
    {
        Assert.Equal(1, C.Allowance(Read(Start())).Ceiling);
        var h = Shot(Spend(Start(), 3));
        Assert.Equal(1, C.Allowance(Read(h)).Ceiling);
        Assert.NotNull(C.Allowance(Read(h)).Refusal);
        h = Spend(h, 4);
        Assert.Null(C.Allowance(Read(h)).Refusal);
        Assert.Contains("outside", C.Allowance(Read(h, window: false)).Refusal!);
        Assert.False(C.Retire(false, false)); // A8.14 p. 59: pinning, breaking and MovementEnded are not terminal removal.
        Assert.True(C.Retire(true, false));
        Assert.True(C.Retire(false, true));
    }

    [Fact]
    public void MissingOrUnobservedHistoryNeverBecomesAnAllowance()
    {
        var h = Spend(Start(), 2);
        Assert.False(C.Allowance(C.Read(h, Phase, At, "gun1", 4, true)).Evaluated);
        Assert.False(C.Allowance(C.Read(h, "turn2-russian-MPh", At, "gun1", 2, true)).Evaluated);
        Assert.False(C.Allowance(C.Read(h, Phase, "bd01:G6:0", "gun1", 2, true)).Evaluated);
        Assert.False(C.Allowance(Read(C.Observe(h, new(Phase, "r1", At, At, 3, 4)))).Evaluated);
        Assert.False(C.Allowance(Read(C.Observe(h, new(Phase, "r1", At, At, 2, 1)))).Evaluated);
        Assert.False(C.Allowance(Read(C.Start(Phase, "r1", At, null, DefensiveFirstFireRelationship.OrdinaryPersonnel))).Evaluated);
    }

    [Fact]
    public void ExplicitExclusionsRetainLegacyForWholeTargetSetButUnknownDoesNot()
    {
        var shot = new DefensiveFirstFireReadingFacts(ScenarioA1FireCalculator.FirstFire, false, false);
        Assert.Equal(DefensiveFirstFireReading.Version1, C.Reading(shot, [DefensiveFirstFireRelationship.OrdinaryPersonnel]));
        foreach (var excluded in new[] { DefensiveFirstFireRelationship.Carried, DefensiveFirstFireRelationship.AdditionalCreatedUnit, DefensiveFirstFireRelationship.ComplexLineage })
        {
            Assert.Equal(DefensiveFirstFireReading.Legacy, C.Reading(shot, [DefensiveFirstFireRelationship.OrdinaryPersonnel, excluded]));
            var h = C.Classify(C.Classify(Start(), excluded), DefensiveFirstFireRelationship.OrdinaryPersonnel);
            Assert.Equal(excluded, h.Relationship);
            Assert.False(C.Allowance(Read(h)).Evaluated);
            var unknown = C.Classify(h, DefensiveFirstFireRelationship.Unknown);
            Assert.Equal(excluded, unknown.Relationship);
            Assert.False(unknown.Known);
            Assert.Equal(DefensiveFirstFireReading.Legacy, C.Reading(shot, [unknown.Relationship]));
        }
        Assert.Equal(DefensiveFirstFireReading.Unknown, C.Reading(shot, [DefensiveFirstFireRelationship.Carried, DefensiveFirstFireRelationship.Unknown]));
        Assert.Equal(DefensiveFirstFireReading.Unknown, C.Reading(shot, []));
        Assert.Equal(DefensiveFirstFireReading.Unknown, C.Reading(shot, [(DefensiveFirstFireRelationship)999]));
    }

    [Fact]
    public void ShotKindSelectsLegacyForVehiclesCanisterAndOtherFire()
    {
        // C6.17 pp. 173-174; C8.4 p. 178: this single-Location personnel reading does not cover the Canister cluster.
        var shot = new DefensiveFirstFireReadingFacts(ScenarioA1FireCalculator.FirstFire, false, false);
        Assert.Equal(DefensiveFirstFireReading.Legacy, C.Reading(shot with
        {
            VehicleTarget = true
        }, []));
        Assert.Equal(DefensiveFirstFireReading.Legacy, C.Reading(shot with
        {
            Canister = true
        }, []));
        Assert.Equal(DefensiveFirstFireReading.Legacy, C.Reading(shot with
        {
            FireKind = null
        }, []));
        Assert.Equal(DefensiveFirstFireReading.Legacy, C.Reading(shot with
        {
            FireKind = ScenarioA1FireCalculator.BoundingFirstFire
        }, []));
        Assert.Equal(DefensiveFirstFireReading.Unknown, C.Reading(shot with
        {
            Canister = null
        }, [DefensiveFirstFireRelationship.OrdinaryPersonnel]));
        Assert.Equal(DefensiveFirstFireReading.Unknown, C.Reading(shot with
        {
            VehicleTarget = null
        }, [DefensiveFirstFireRelationship.OrdinaryPersonnel]));
    }

    [Fact]
    public void MalformedVersionedSetsCannotFallBackToScalar()
    {
        var facts = new DefensiveFirstFirePersonnelFacts(1, [Read(Spend(Start(), 4))]);
        Assert.Null(C.FactsRefusal(facts));
        Assert.NotNull(C.FactsRefusal(facts with
        {
            Version = 99
        }));
        Assert.NotNull(C.FactsRefusal(facts with
        {
            Targets = []
        }));
        Assert.NotNull(C.FactsRefusal(facts with
        {
            Targets = [facts.Targets[0], facts.Targets[0]]
        }));
        Assert.NotNull(C.FactsRefusal(facts with
        {
            Targets = [facts.Targets[0] with { HalfMfSpent = null }]
        }));
        var shot = ShotFacts(facts);
        Assert.DoesNotContain(ScenarioA1OrdnanceCalculator.Precheck(shot, Reference), x => x.Contains("movement.personnelHistory", StringComparison.Ordinal));
        Assert.Contains("asl.a1.ordnance.fact-missing:movement.personnelHistory", ScenarioA1OrdnanceCalculator.Precheck(shot with
        {
            Movement = shot.Movement! with
            {
                SpentHere = 2
            }
        }, Reference));
        Assert.Contains("asl.a1.ordnance.fact-missing:movement.personnelHistory", ScenarioA1OrdnanceCalculator.Precheck(shot with
        {
            Movement = shot.Movement! with
            {
                PersonnelHistory = facts with
                {
                    Version = 99
                }
            }
        }, Reference));
        Assert.Contains("asl.a1.ordnance.fact-missing:movement.personnelHistory", ScenarioA1OrdnanceCalculator.Precheck(shot with
        {
            Movement = shot.Movement! with
            {
                PersonnelHistory = facts with
                {
                    Targets = [facts.Targets[0] with { TargetId = "wrong" }]
                }
            }
        }, Reference));
    }

    [Fact]
    public void VersionedOrdnanceEnforcesRemainingShotsAndOldNullShapeIsUnchanged()
    {
        var facts = new DefensiveFirstFirePersonnelFacts(1, [Read(Shot(Spend(Start(), 4)))]);
        var shot = ShotFacts(facts);
        Assert.DoesNotContain("asl.a1.ordnance.first-fire-limit", ScenarioA1OrdnanceCalculator.Precheck(shot, Reference));
        Assert.Contains("asl.a1.ordnance.first-fire-limit", ScenarioA1OrdnanceCalculator.Precheck(ShotFacts(facts with
        {
            Targets = [facts.Targets[0] with { ShotsHere = 2 }]
        }), Reference));
        Assert.DoesNotContain("PersonnelHistory", JsonSerializer.Serialize(new OrdnanceMovement(null, true, true, 2, 1)), StringComparison.Ordinal);
        Assert.Contains("PersonnelHistory", JsonSerializer.Serialize(shot.Movement), StringComparison.Ordinal);
    }

    [Fact]
    public void AtrHistoryUsesTheSameCrewOnlySelectionAsResolution()
    {
        // C13.23/.24 p. 183: the non-20L ATR attacks the Gun crew, not every unit of its Location.
        var shot = ShotFacts(new(1, [Read(Spend(Start(), 4))]));
        shot = shot with
        {
            Gun = shot.Gun! with
            {
                DefinitionId = "defender-atr"
            },
            Hit = shot.Hit! with
            {
                Targets = [Target("r1"), Target("r2")],
                GunTarget = new("target-gun", "attacker-inf-gun", "r1", false, false)
            },
        };
        var targets = ScenarioA1OrdnanceCalculator.PersonnelHistoryTargets(shot, Reference);
        Assert.Equal("r1", Assert.Single(targets!).UnitId);
        Assert.Null(C.MovementFactsRefusal(shot, targets));
    }

    [Fact]
    public void PublicExpenditurePermitsFirersVerdictWithoutPrivateTargetIdentity()
    {
        // A8.11/.12 p. 59: the DEFENDER acts on announced expenditure, without private target identity.
        var contract = ScenarioA1RuleContracts.Find("c6.17-personnel-history")!;
        foreach (var name in new[] { "halfMfSpent", "shotsHere" })
        {
            var fact = Assert.Single(contract.Facts, x => x.Name == name);
            Assert.Equal(ContractDisclosure.All, fact.Disclosure);
            Assert.Null(fact.Concerns);
        }
        Assert.Equal(ContractDisclosure.Owner, Assert.Single(contract.Facts, x => x.Name == "targetId").Disclosure);
        var facts = JsonNode.Parse("""{"phase":"MPh","firerId":"g1","targetId":null,"weaponId":"gun1","location":"bd01:G5:0","halfMfSpent":4,"shotsHere":1,"inCurrentWindow":true}""")!.AsObject();
        Assert.Equal("allowed", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["shotsHere"] = 2;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["weaponId"] = null;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
    }

    [Fact]
    public void JointShotRequiresEveryTargetsAllowanceAndOrdinalIdentityOrder()
    {
        var a = Read(Spend(Start(), 4));
        var b = Read(Shot(Spend(Start("r2"), 2)));
        var facts = new DefensiveFirstFirePersonnelFacts(1, [a, b]);
        Assert.Null(C.FactsRefusal(facts));
        Assert.NotNull(C.ShotRefusal(facts));
        Assert.Null(C.ShotRefusal(facts with
        {
            Targets = [a, b with { ShotsHere = 0 }]
        }));
        Assert.NotNull(C.FactsRefusal(facts with
        {
            Targets = [b, a]
        }));
        var shot = ShotFacts(facts) with
        {
            Hit = ShotFacts(facts).Hit! with
            {
                Targets = [Target("r1"), Target("r2")]
            }
        };
        Assert.Contains("asl.a1.ordnance.first-fire-limit", ScenarioA1OrdnanceCalculator.Precheck(shot, Reference));
    }

    [Fact]
    public void VersionedBindingRejectsContradictoryShotFactsAndMissingTargets()
    {
        var shot = ShotFacts(new(1, [Read(Spend(Start(), 4))]));
        var targets = new[] { Target("r1") };
        Assert.Null(C.MovementFactsRefusal(shot, targets));
        var invalid = new[]
        {
            shot with { Phase = "DFPh" },
            shot with { FireKind = null },
            shot with { VehicleTarget = new("v1", "tank", "north", "north", true, false, false, false, true) },
            shot with { Canister = new(1, "north", []) },
            shot with { TargetLocationId = "bd01:G6:0" },
            shot with { Gun = shot.Gun! with { GunId = "gun2" } },
            shot with { Movement = shot.Movement! with { PersonnelHistory = new(1, [Read(Spend(Start(), 4), window: false)]) } },
        };
        Assert.All(invalid, x => Assert.NotNull(C.MovementFactsRefusal(x, targets)));
        Assert.NotNull(C.MovementFactsRefusal(shot, [Target("r1"), Target("r2")]));
        Assert.Contains("asl.a1.ordnance.fact-missing:movement.personnelHistory", ScenarioA1OrdnanceCalculator.Precheck(invalid[^1], Reference));
        Assert.DoesNotContain("asl.a1.ordnance.first-fire-limit", ScenarioA1OrdnanceCalculator.Precheck(invalid[^1], Reference));
    }

    [Fact]
    public void ObserveRejectsWrongTargetPreviousLocationAndPhase()
    {
        var h = Spend(Start(), 2);
        var next = new DefensiveFirstFireExpenditure(Phase, "r1", At, At, 2, 4);
        Assert.False(C.Observe(h, next with
        {
            TargetId = "r2"
        }).Known);
        Assert.False(C.Observe(h, next with
        {
            PreviousLocation = "bd01:G6:0"
        }).Known);
        Assert.False(C.Observe(h, next with
        {
            PhaseId = "turn2-russian-MPh"
        }).Known);
    }

    [Fact]
    public void VersionedPayloadRoundTripsItsOrderedFactsAndVerdict()
    {
        var facts = new DefensiveFirstFirePersonnelFacts(1, [Read(Spend(Start(), 4)), Read(Spend(Start("r2"), 2))]);
        var restored = JsonSerializer.Deserialize<OrdnanceMovement>(JsonSerializer.Serialize(ShotFacts(facts).Movement))!;
        Assert.Equal(1, restored.PersonnelHistory!.Version);
        Assert.Equal(facts.Targets, restored.PersonnelHistory.Targets);
        Assert.Null(C.ShotRefusal(restored.PersonnelHistory));
    }

    private static FireTarget Target(string id) => new(id, "defender-squad", At, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] };
    private static OrdnanceShot ShotFacts(DefensiveFirstFirePersonnelFacts facts) =>
        new("MPh", "non-phasing", "german", new("gun1", "attacker-inf-gun", false, 0, false, false),
            new("g1", "attacker-crew", false, false, false, false, false), At, 5, 0, false, true, 0,
            new("MPh", "non-phasing", null, null, At, [], null, null, true, new(false, 0, true, false), 7, "open-ground", [Target("r1")], 2, null),
            new(null, null, null, null, null))
        {
            FireKind = ScenarioA1FireCalculator.FirstFire,
            Movement = new(null, true, true, null, null)
            {
                PersonnelHistory = facts
            }
        };
}
