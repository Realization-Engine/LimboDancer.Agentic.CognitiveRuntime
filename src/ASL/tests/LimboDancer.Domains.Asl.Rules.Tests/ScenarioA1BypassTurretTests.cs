using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1BypassTurretTests
{
    [Fact]
    public void LegacyTurretStepRefusalPreservesTextAndPriority()
    {
        Assert.Equal("play.move-vehicle-turret: v is in Bypass, where the TCA has its own four positions; that is not built (D2.32, D2.321)",
            ScenarioA1VehicleMovementCalculator.TurretStepBar("v", "stop", true, true, true, true));
        Assert.Equal("play.move-vehicle-turret:  is in Bypass, where the TCA has its own four positions; that is not built (D2.32, D2.321)",
            ScenarioA1VehicleMovementCalculator.TurretStepBar(null!, "stop", true, true, false, true));
        Assert.Equal("play.move-vehicle-turret: v has no turret to turn (D3.12)",
            ScenarioA1VehicleMovementCalculator.TurretStepBar("v", "start", false, false, true, true));
        Assert.Equal("play.move-vehicle-turret: name the hexspine the turret turns to, such as north-east or west (D3.12)",
            ScenarioA1VehicleMovementCalculator.TurretStepBar("v", "start", true, false, true, true));
        Assert.Equal("play.move-vehicle-turret: v's turret turns with a Delay MP, a stop, a VCA change, or an entry (D3.12)",
            ScenarioA1VehicleMovementCalculator.TurretStepBar("v", null!, true, true, true, true));
        Assert.Equal("play.move-vehicle-turret: v is in woods, a building, or rubble, where a TCA change doubles the MP expenditure; that is not built (D3.12, C5.11)",
            ScenarioA1VehicleMovementCalculator.TurretStepBar("v", "stop", true, true, true, false));
        Assert.Null(ScenarioA1VehicleMovementCalculator.TurretStepBar("v", "stop", true, true, false, false));
    }

    [Fact]
    public void LegacyMotionChangeRefusalPreservesTextAndPriority()
    {
        const string expected = "play.motion-attempt-bypass: v is in Bypass, where a CA has its own positions; that is not built (D2.32, D2.33). Attempt Motion without a change of VCA or TCA";
        Assert.Equal(expected, ScenarioA1MotionAttempts.ChangeBar("v", true, true, false, false, false, true));
        Assert.Equal(expected, ScenarioA1MotionAttempts.ChangeBar("v", false, false, true, true, true, true));
        Assert.Equal(expected, ScenarioA1MotionAttempts.ChangeBar("v", true, true, true, true, true, true));
        Assert.Equal("play.motion-attempt: name the hexspine the VCA or the TCA turns to, such as north-east or west (D2.401)",
            ScenarioA1MotionAttempts.ChangeBar("v", true, false, true, true, false, true));
        Assert.Equal("play.motion-attempt: name the hexspine the VCA or the TCA turns to, such as north-east or west (D2.401)",
            ScenarioA1MotionAttempts.ChangeBar("v", false, false, true, false, false, true));
        Assert.Equal("play.motion-attempt: v has no turret to turn (D3.12); name a VCA alone",
            ScenarioA1MotionAttempts.ChangeBar("v", true, true, true, true, false, true));
        Assert.Null(ScenarioA1MotionAttempts.ChangeBar("v", false, false, false, false, false, true));
        Assert.Null(ScenarioA1MotionAttempts.ChangeBar("v", true, true, true, true, true, false));
    }

    private static readonly BypassFrameFacts Lane = new(new(0, 0), new(0, 1), new(1, 0));
    private static readonly BypassTurretFacts SideShot = new(true, Lane, new(0, 2), BypassTurretPosition.Front,
        BypassTurretPosition.Left, null, BypassTraverse.St, BypassWeaponKind.Ordnance, BypassFireMode.Ordinary,
        false, null, null, null, null);

    [Fact]
    public void PrintedBoard23ExamplePlacesEveryNamedHexInItsPublishedFacing()
    {
        // D2.321 p. 197 diagram. Columns A=0, Z=25, AA=26. The board's stagger maps to
        // q=column, r=-row-ceiling(column/2). Fixed values below are independent of the classifier.
        var bb3 = new BypassHex(27, -17);
        var cc4 = new BypassHex(28, -18);
        var bb4 = new BypassHex(27, -18);
        var frame = new BypassFrameFacts(bb3, cc4, bb4);
        (string Label, BypassHex Hex)[] front = [("BB4", bb4), ("AA5", new(26, -18)), ("BB5", new(27, -19)),
            ("Z5", new(25, -18)), ("AA6", new(26, -19)), ("BB6", new(27, -20))];
        (string Label, BypassHex Hex)[] rear = [("CC3", new(28, -17)), ("CC2", new(28, -16)), ("DD2", new(29, -17)),
            ("CC1", new(28, -15)), ("DD1", new(29, -16)), ("EE2", new(30, -17))];
        foreach (var (label, hex) in front)
        {
            Assert.True(ScenarioA1BypassTurretCalculator.Sector(frame, hex).Sector == BypassTurretPosition.Front, $"[{label}] must be front.");
        }
        foreach (var (label, hex) in rear)
        {
            Assert.True(ScenarioA1BypassTurretCalculator.Sector(frame, hex).Sector == BypassTurretPosition.Rear, $"[{label}] must be rear.");
        }
        Assert.Equal(new BypassHex(28, -17), ScenarioA1BypassTurretCalculator.Frame(frame).Frame!.Rear);
        var aa4 = new BypassHex(26, -17);
        var bb2 = new BypassHex(27, -16);
        Assert.Equal(BypassTurretPosition.Right, ScenarioA1BypassTurretCalculator.Sector(frame, aa4).Sector);
        Assert.Equal(BypassTurretPosition.Right, ScenarioA1BypassTurretCalculator.Sector(frame, bb2).Sector);
        var first = SideShot with
        {
            Frame = frame,
            Target = aa4,
            Proposed = BypassTurretPosition.Right
        };
        Components(first, 1, 2, 1);
        Components(first with
        {
            Target = bb2,
            Current = BypassTurretPosition.Right,
            ApplicableAcquisition = false
        }, 0, 2, 0);
        Components(first with
        {
            Current = BypassTurretPosition.Right,
            ApplicableAcquisition = true
        }, 0, 0, 0);
    }

    [Fact]
    public void PrintedSlowTurretExamplesKeepCaseAAndSideChangeSeparate()
    {
        Components(SideShot, 1, 2, 1);
        Components(SideShot with
        {
            Proposed = BypassTurretPosition.Rear,
            Target = new(-1, 1),
            Direction = BypassTurnDirection.Clockwise
        }, 2, 3, 1);
        Components(SideShot with
        {
            Current = BypassTurretPosition.Left,
            ApplicableAcquisition = false
        }, 0, 2, 0);
        Components(SideShot with
        {
            Current = BypassTurretPosition.Left,
            ApplicableAcquisition = true
        }, 0, 0, 0);
        Components(SideShot with
        {
            Current = BypassTurretPosition.Left,
            Proposed = BypassTurretPosition.Front,
            Target = new(1, 0)
        }, 1, 2, 0);
        Components(SideShot with
        {
            Current = BypassTurretPosition.Front,
            Proposed = BypassTurretPosition.Front,
            Target = new(1, 0)
        }, 0, 0, 0);
        Components(SideShot with
        {
            Traverse = BypassTraverse.T
        }, 1, 1, 1);
        Components(SideShot with
        {
            Traverse = BypassTraverse.Rst
        }, 1, 2, 1);
        Components(SideShot with
        {
            Traverse = BypassTraverse.OneMan
        }, 1, 2, 1);
    }

    [Theory]
    [InlineData(BypassWeaponKind.Cmg)]
    [InlineData(BypassWeaponKind.Ife)]
    [InlineData(BypassWeaponKind.Canister)]
    public void RepeatedTargetCategoriesCannotUseAcquisitionInstead(BypassWeaponKind weapon)
    {
        var shot = SideShot with
        {
            Current = BypassTurretPosition.Left,
            Weapon = weapon,
            SameKnownTarget = true,
            SameTargetFacing = true
        };
        Components(shot, 0, 0, 0);
        Components(shot with
        {
            SameKnownTarget = false,
            ApplicableAcquisition = true
        }, 0, 2, 0);
        Components(shot with
        {
            SameTargetFacing = false,
            ApplicableAcquisition = null
        }, 0, 2, 0);
        Unknown(shot with
        {
            SameKnownTarget = null
        });
        Unknown(shot with
        {
            SameTargetFacing = null,
            ApplicableAcquisition = true
        });
        Components(shot with
        {
            SameTargetFacing = false,
            SameKnownTarget = null
        }, 0, 2, 0);
        Unknown(SideShot with
        {
            Current = BypassTurretPosition.Left,
            SameKnownTarget = true,
            SameTargetFacing = true
        });
    }

    [Fact]
    public void EveryTurretPathCountsTargetFacingsAndOnlyEnteredSidePositions()
    {
        // Expected side-entry matrix for clockwise paths; the diagonals mean unchanged, not a full revolution.
        int[,] steps = { { 0, 1, 2, 3 }, { 3, 0, 1, 2 }, { 2, 3, 0, 1 }, { 1, 2, 3, 0 } };
        bool[,] sides = { { false, true, true, true }, { true, false, false, true }, { true, true, false, true }, { false, true, true, false } };
        for (var from = 0; from < 4; from++)
        {
            for (var to = 0; to < 4; to++)
            {
                var clockwise = ScenarioA1BypassTurretCalculator.TurretChange((BypassTurretPosition)from, (BypassTurretPosition)to,
                    from == to ? null : BypassTurnDirection.Clockwise);
                Assert.Equal(BypassDisposition.Evaluated, clockwise.Disposition);
                Assert.Equal(steps[from, to], clockwise.Steps);
                Assert.Equal(sides[from, to], clockwise.EntersSide);
                // Mirror Front/Rear unchanged, Right/Left interchanged. A mirrored clockwise path is counterclockwise.
                var counter = ScenarioA1BypassTurretCalculator.TurretChange((BypassTurretPosition)((4 - from) % 4), (BypassTurretPosition)((4 - to) % 4),
                    from == to ? null : BypassTurnDirection.Counterclockwise);
                Assert.Equal((clockwise.Steps, clockwise.EntersSide), (counter.Steps, counter.EntersSide));
            }
        }
        Assert.Equal(1, ScenarioA1BypassTurretCalculator.TurretChange(BypassTurretPosition.Front, BypassTurretPosition.Left, null).Steps);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassTurretCalculator.TurretChange(BypassTurretPosition.Front, BypassTurretPosition.Rear, null).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassTurretCalculator.TurretChange(BypassTurretPosition.Front, BypassTurretPosition.Front, BypassTurnDirection.Clockwise).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassTurretCalculator.TurretChange((BypassTurretPosition)4, BypassTurretPosition.Front, null).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassTurretCalculator.TurretChange(null, BypassTurretPosition.Front, null).Disposition);
    }

    [Fact]
    public void OccupiedHexSectorsRotateAndReflectAcrossBothObstacleSides()
    {
        (BypassHex Hex, BypassTurretPosition Sector)[] examples = [
            (new(1, 0), BypassTurretPosition.Front), (new(3, 0), BypassTurretPosition.Front), (new(3, -2), BypassTurretPosition.Front),
            (new(-1, 1), BypassTurretPosition.Rear), (new(-3, 1), BypassTurretPosition.Rear), (new(-3, 3), BypassTurretPosition.Rear),
            (new(0, 2), BypassTurretPosition.Left), (new(3, 1), BypassTurretPosition.Left), (new(-3, 4), BypassTurretPosition.Left),
            (new(0, -1), BypassTurretPosition.Right), (new(3, -3), BypassTurretPosition.Right), (new(-3, 0), BypassTurretPosition.Right)];
        for (var rotation = 0; rotation < 6; rotation++)
        {
            foreach (var reflected in new[] { false, true })
            {
                BypassHex Transform(BypassHex h) => Map(h, rotation, reflected);
                var lane = new BypassFrameFacts(Transform(new(0, 0)), Transform(new(0, 1)), Transform(new(1, 0)));
                foreach (var (hex, expected) in examples)
                {
                    var sector = reflected ? expected switch
                    {
                        BypassTurretPosition.Left => BypassTurretPosition.Right,
                        BypassTurretPosition.Right => BypassTurretPosition.Left,
                        _ => expected
                    } : expected;
                    Assert.Equal(sector, ScenarioA1BypassTurretCalculator.Sector(lane, Transform(hex)).Sector);
                    // The other incident hex can be the obstacle without changing the physical Target Facing.
                    Assert.Equal(sector, ScenarioA1BypassTurretCalculator.Sector(lane with
                    {
                        Obstacle = lane.Across,
                        Across = lane.Obstacle
                    }, Transform(hex)).Sector);
                }
                var frame = ScenarioA1BypassTurretCalculator.Frame(lane).Frame!;
                Assert.Equal(Transform(new(-1, 1)), frame.Rear);
                Assert.Equal(((reflected ? 3 : 0) + rotation) % 6, frame.Facing);
            }
        }
    }

    [Fact]
    public void BoundingFirstFireDoesNotGuessEarlierPaidMpHistory()
    {
        var shot = SideShot with
        {
            Mode = BypassFireMode.BoundingFirstFire
        };
        Assert.Equal(BypassDisposition.Refused, Decide(shot).Disposition);
        shot = shot with
        {
            Current = BypassTurretPosition.Left
        };
        Unknown(shot);
        Unknown(shot with
        {
            EarlierMpTcaChange = true
        });
        Components(shot with
        {
            EarlierMpTcaChange = false
        }, 0, 0, 0);
        Assert.Equal("C5.13", Decide(shot with
        {
            EarlierMpTcaChange = false
        }).Rule);
    }

    [Fact]
    public void UnknownInvalidAndUnsupportedFactsNeverBecomePermission()
    {
        BypassTurretFacts[] unknowns = [SideShot with { InBypass = null }, SideShot with { Frame = null }, SideShot with { Target = null },
            SideShot with { Current = null }, SideShot with { Proposed = null }, SideShot with { Traverse = null }, SideShot with { Weapon = null },
            SideShot with { Mode = null }, SideShot with { SpecialTerrain = null }, SideShot with { SpecialTerrain = true },
            SideShot with { Current = BypassTurretPosition.Left }, SideShot with { Target = new(0, 0) },
            SideShot with { Frame = new(new(0, 0), new(5, 0), new(1, 0)), InBypass = false },
            SideShot with { Frame = new(new(0, 0), new(5, 0), null), InBypass = false },
            SideShot with { Target = new(int.MaxValue, int.MinValue), InBypass = false },
            SideShot with { Current = (BypassTurretPosition)99, InBypass = false }, SideShot with { Direction = (BypassTurnDirection)99 },
            SideShot with { Traverse = (BypassTraverse)99 }, SideShot with { Weapon = (BypassWeaponKind)99 }, SideShot with { Mode = (BypassFireMode)99 }];
        foreach (var facts in unknowns)
        {
            Unknown(facts);
        }
        Assert.Equal(BypassDisposition.NotApplicable, Decide(SideShot with
        {
            InBypass = false,
            Frame = null
        }).Disposition);
        Assert.Equal(BypassDisposition.Refused, Decide(SideShot with
        {
            Weapon = BypassWeaponKind.Bow
        }).Disposition);
        Assert.Equal(BypassDisposition.Refused, Decide(SideShot with
        {
            Traverse = BypassTraverse.Nt
        }).Disposition);
        Assert.Equal(BypassDisposition.Refused, Decide(SideShot with
        {
            Proposed = BypassTurretPosition.Right
        }).Disposition);
        Assert.Throws<ArgumentNullException>(() => Decide(null!));
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassTurretCalculator.Frame(new(new(1000000, 0), new(1000000, 1), new(999999, 1))).Disposition);
        Assert.Equal(BypassDisposition.Evaluated, ScenarioA1BypassTurretCalculator.Frame(new(new(-999999, 0), new(-999999, 1), new(-1000000, 1))).Disposition);
    }

    [Fact]
    public void ContractValidatesNumbersAndMasksUnanchoredPrivateFacts()
    {
        var contract = ScenarioA1RuleContracts.Find("d2.321-bypass-tca")!;
        Assert.Empty(contract.Events);
        var anchor = Assert.Single(contract.Facts, fact => fact.Name == "vehicleId");
        Assert.Equal((ContractDisclosure.Owner, "vehicleId"), (anchor.Disclosure, anchor.Concerns));
        var facts = JsonNode.Parse("""{"inBypass":true,"vehicleId":"tank","frameObstacleQ":0,"frameObstacleR":0,"frameAcrossQ":0,"frameAcrossR":1,"frameFrontQ":1,"frameFrontR":0,"targetQ":0,"targetR":2,"current":"Front","proposed":"Left","traverse":"St","weapon":"Ordnance","mode":"Ordinary","specialTerrain":false}""")!.AsObject();
        Assert.Equal("evaluated", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        foreach (var number in new[] { 0.5m, 1000001m, decimal.MaxValue })
        {
            var bad = facts.DeepClone().AsObject();
            bad["frameObstacleQ"] = number;
            Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, bad).Outcome);
        }
        var invalidChoice = facts.DeepClone().AsObject();
        invalidChoice["traverse"] = "invalid";
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, invalidChoice).Outcome);
        facts["vehicleId"] = null;
        var hidden = ScenarioA1RuleContracts.Evaluate(contract, facts);
        Assert.Equal(ContractVerdict.NotEvaluated, hidden.Outcome);
        Assert.DoesNotContain(hidden.Reasons, reason => reason.Text.Contains("St", StringComparison.Ordinal) || reason.Text.Contains("Left", StringComparison.Ordinal));
    }

    private static BypassHex Map(BypassHex h, int rotation, bool reflected)
    {
        if (reflected)
        {
            h = new(-h.Q, h.Q + h.R);
        }
        for (var i = 0; i < rotation; i++)
        {
            h = new(-h.R, h.Q + h.R);
        }
        return new(h.Q + 17, h.R - 29);
    }
    private static BypassTurretVerdict Decide(BypassTurretFacts facts) => ScenarioA1BypassTurretCalculator.FireAdjustment(facts);
    private static void Unknown(BypassTurretFacts facts) => Assert.Equal(BypassDisposition.NotEvaluated, Decide(facts).Disposition);
    private static void Components(BypassTurretFacts facts, int steps, int caseA, int side)
    {
        var verdict = Decide(facts);
        Assert.Equal(BypassDisposition.Evaluated, verdict.Disposition);
        Assert.Equal((steps, caseA, side), (verdict.Steps, verdict.CaseA, verdict.SideChangeToHitDrm));
    }
}
