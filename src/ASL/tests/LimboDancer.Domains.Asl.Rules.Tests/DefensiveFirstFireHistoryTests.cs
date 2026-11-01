using System.Text.Json;
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
        Assert.Equal(1, C.Reading([DefensiveFirstFireRelationship.OrdinaryPersonnel]));
        foreach (var excluded in new[] { DefensiveFirstFireRelationship.Carried, DefensiveFirstFireRelationship.AdditionalCreatedUnit, DefensiveFirstFireRelationship.ComplexLineage })
        {
            Assert.Equal(0, C.Reading([DefensiveFirstFireRelationship.OrdinaryPersonnel, excluded]));
            var h = C.Classify(C.Classify(Start(), excluded), DefensiveFirstFireRelationship.OrdinaryPersonnel);
            Assert.Equal(excluded, h.Relationship);
            Assert.False(C.Allowance(Read(h)).Evaluated);
        }
        Assert.Null(C.Reading([DefensiveFirstFireRelationship.Carried, DefensiveFirstFireRelationship.Unknown]));
        Assert.Null(C.Reading([]));
        Assert.Null(C.Reading([(DefensiveFirstFireRelationship)999]));
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
