using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1BypassVcaTests
{
    private static readonly BypassFrameFacts Old = new(new(0, 0), new(0, 1), new(1, 0));
    private static readonly BypassEdge Lower = new(new(0, 0), new(1, 0));
    private static readonly BypassEdge Upper = new(new(0, 1), new(1, 0));
    private static readonly BypassFrameFacts Entered = new(new(0, 0), new(1, 0), new(1, -1));
    private static readonly BypassVcaTurnFacts Turn = new(true, Old, false, Lower, BypassTurnPurpose.ContinueMovement);

    [Fact]
    public void BothConnectingTurnsCostOneMpApartFromEntryAndRequireContinuation()
    {
        var lower = ScenarioA1BypassVcaCalculator.TurnAtCafp(Turn);
        Assert.Equal((BypassDisposition.Evaluated, 2, 5, true), (lower.Disposition, lower.HalfMp, lower.ProposedFacing, lower.ContinuationRequired));
        var upper = ScenarioA1BypassVcaCalculator.TurnAtCafp(Turn with
        {
            Edge = Upper
        });
        Assert.Equal((BypassDisposition.Evaluated, 2, 1, true), (upper.Disposition, upper.HalfMp, upper.ProposedFacing, upper.ContinuationRequired));
        Assert.Equal(lower, ScenarioA1BypassVcaCalculator.TurnAtCafp(Turn with
        {
            Edge = new(Lower.Two, Lower.One)
        }));
        Assert.Equal(BypassDisposition.Refused, ScenarioA1BypassVcaCalculator.TurnAtCafp(Turn with
        {
            PendingTurn = true
        }).Disposition);
        Assert.Equal(BypassDisposition.Refused, ScenarioA1BypassVcaCalculator.TurnAtCafp(Turn with
        {
            Purpose = BypassTurnPurpose.FireOnly
        }).Disposition);
        Assert.Equal(BypassDisposition.Refused, ScenarioA1BypassVcaCalculator.TurnAtCafp(Turn with
        {
            Edge = new(Old.Obstacle, Old.Across)
        }).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassVcaCalculator.TurnAtCafp(Turn with
        {
            Frame = new(new(0, -1000000), new(0, -999999), new(1, -1000000)),
            Edge = new(new(0, -1000000), new(1, -1000000))
        }).Disposition);
    }

    [Fact]
    public void ForwardEntryAndReverseUseTheMovementOrientationNotDefensiveFacing()
    {
        var forward = new BypassContinuationFacts(true, Old, true, Lower, false, new(BypassPositionKind.Bypass, Entered, null, null));
        var answer = ScenarioA1BypassVcaCalculator.Continuation(forward);
        Assert.Equal(BypassDisposition.Evaluated, answer.Disposition);
        Assert.False(answer.ContinuationRequired);
        Assert.Null(answer.HalfMp); // The 1 MP turn is not an entry-cost verdict or a Reverse multiplier.
        Assert.Equal(5, answer.ProposedFacing);
        Assert.Equal(forward.Destination, answer.Position);
        var reverse = forward with
        {
            Reverse = true,
            Destination = new(BypassPositionKind.Ordinary, null, new(0, 1), 5)
        };
        Assert.Equal(BypassDisposition.Evaluated, ScenarioA1BypassVcaCalculator.Continuation(reverse).Disposition);
        Assert.Equal(BypassDisposition.Refused, ScenarioA1BypassVcaCalculator.Continuation(reverse with
        {
            Destination = new(BypassPositionKind.Ordinary, null, new(-1, 1), 5)
        }).Disposition);
        Assert.Equal(BypassDisposition.Refused, ScenarioA1BypassVcaCalculator.Continuation(reverse with
        {
            Edge = Upper,
            Destination = new(BypassPositionKind.Ordinary, null, new(0, 0), 1)
        }).Disposition);
        Assert.Equal(BypassDisposition.Refused, ScenarioA1BypassVcaCalculator.Continuation(forward with
        {
            Reverse = true
        }).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassVcaCalculator.Continuation(forward with
        {
            Reverse = true,
            PendingTurn = false,
            Edge = null
        }).Disposition);
        Assert.Equal(BypassDisposition.Refused, ScenarioA1BypassVcaCalculator.Continuation(reverse with
        {
            Reverse = false
        }).Disposition);
        Assert.Equal(BypassDisposition.Refused, ScenarioA1BypassVcaCalculator.Continuation(forward with
        {
            PendingTurn = false
        }).Disposition);
        Assert.Equal(BypassDisposition.Refused, ScenarioA1BypassVcaCalculator.Continuation(forward with
        {
            Destination = new(BypassPositionKind.Bypass, Old, null, null)
        }).Disposition);
        // Outright exits without a pending turn retain the original movement orientation.
        Assert.Equal(BypassDisposition.Evaluated, ScenarioA1BypassVcaCalculator.Continuation(new(true, Old, false, null, false, new(BypassPositionKind.Ordinary, null, new(1, 0), 0))).Disposition);
        Assert.Equal(BypassDisposition.Evaluated, ScenarioA1BypassVcaCalculator.Continuation(new(true, Old, false, null, true, new(BypassPositionKind.Ordinary, null, new(-1, 1), 0))).Disposition);
    }

    [Fact]
    public void PendingTurnRetainsOldTargetFacingUntilEntryAtEveryRotationAndReflection()
    {
        for (var n = 0; n < 6; n++)
        {
            foreach (var reflected in new[] { false, true })
            {
                BypassHex Map(BypassHex h)
                {
                    if (reflected)
                    {
                        h = new(-h.Q, h.Q + h.R);
                    }
                    for (var i = 0; i < n; i++)
                    {
                        h = new(-h.R, h.Q + h.R);
                    }
                    return new(h.Q - 43, h.R + 17);
                }
                BypassFrameFacts Frame(BypassFrameFacts f) => new(Map(f.Obstacle!.Value), Map(f.Across!.Value), Map(f.Front!.Value));
                var old = Frame(Old);
                var entered = Frame(Entered);
                var pending = new BypassDefensiveFacts(true, old, BypassMovementStage.TurnPending, null, false, BypassOriginKind.HexCenter, Map(new(2, 0)));
                var before = ScenarioA1BypassVcaCalculator.DefensivePosition(pending);
                Assert.Equal("front", before.TargetFacing);
                Assert.Equal(old, before.Position!.Frame);
                Assert.True(before.ContinuationRequired);
                var halted = ScenarioA1BypassVcaCalculator.DefensivePosition(pending with
                {
                    Immobilized = true
                });
                Assert.Equal("front", halted.TargetFacing);
                Assert.Equal(old, halted.Position!.Frame);
                Assert.False(halted.ContinuationRequired);
                Assert.Null(ScenarioA1BypassVcaCalculator.DefensivePosition(pending with
                {
                    Immobilized = null
                }).ContinuationRequired);
                var completed = pending with
                {
                    InBypass = null,
                    OldFrame = null,
                    Stage = BypassMovementStage.EntryCompleted,
                    Completed = new(BypassPositionKind.Bypass, entered, null, null),
                    Immobilized = true
                };
                var after = ScenarioA1BypassVcaCalculator.DefensivePosition(completed);
                Assert.Equal("side", after.TargetFacing); // Wrongly using the proposed frame before entry would change front to side.
                Assert.Equal(entered, after.Position!.Frame);
                Assert.False(after.ContinuationRequired);
                var edge = new BypassEdge(Map(new(0, 0)), Map(new(1, 0)));
                var turn = ScenarioA1BypassVcaCalculator.TurnAtCafp(new(true, old, false, edge, BypassTurnPurpose.ContinueMovement));
                Assert.Equal(BypassDisposition.Evaluated, turn.Disposition);
                Assert.Equal(((reflected ? 4 : 5) + n) % 6, turn.ProposedFacing);
                Assert.Equal(BypassDisposition.Evaluated, ScenarioA1BypassVcaCalculator.Continuation(new(true, old, true, edge, false, completed.Completed)).Disposition);
                var reversePosition = new BypassPositionFacts(BypassPositionKind.Ordinary, null, Map(new(0, 1)), turn.ProposedFacing);
                Assert.Equal(BypassDisposition.Evaluated, ScenarioA1BypassVcaCalculator.Continuation(new(true, old, true, edge, true, reversePosition)).Disposition);
                var ordinary = ScenarioA1BypassVcaCalculator.DefensivePosition(new(null, null, BypassMovementStage.EntryCompleted, reversePosition, true, null, null));
                Assert.Equal(BypassDisposition.NotApplicable, ordinary.Disposition);
                Assert.Equal(reversePosition, ordinary.Position);
            }
        }
    }

    [Fact]
    public void EndNeedsNoDestinationButCannotVoluntarilyAbandonAPendingTurn()
    {
        Assert.Equal(BypassDisposition.Refused, ScenarioA1BypassVcaCalculator.EndMovement(new(true, true, false, BypassEndCause.Voluntary, false)).Disposition);
        Assert.Equal(BypassDisposition.Evaluated, ScenarioA1BypassVcaCalculator.EndMovement(new(true, false, true, BypassEndCause.Voluntary, false)).Disposition);
        Assert.Equal(BypassDisposition.ForcedEnd, ScenarioA1BypassVcaCalculator.EndMovement(new(true, null, null, BypassEndCause.Immobilized, true)).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassVcaCalculator.EndMovement(new(true, true, null, BypassEndCause.Immobilized, null)).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassVcaCalculator.EndMovement(new(true, true, null, BypassEndCause.OtherForcedHalt, true)).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassVcaCalculator.EndMovement(new(true, true, true, BypassEndCause.Voluntary, false)).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassVcaCalculator.EndMovement(new(true, false, true, BypassEndCause.Voluntary, true)).Disposition);
        // Stop is not EndMovement. This API leaves Stop/Start and entry affordability to their own existing calculators.
        Assert.Equal(BypassDisposition.Refused, ScenarioA1BypassVcaCalculator.EndMovement(new(true, true, null, BypassEndCause.Voluntary, null)).Disposition);
    }

    [Fact]
    public void UnsupportedOriginsAndContradictoryPositionsRemainUnknown()
    {
        var pending = new BypassDefensiveFacts(true, Old, BypassMovementStage.TurnPending, null, false, BypassOriginKind.HexCenter, new(2, 0));
        BypassDefensiveFacts[] unknowns = [pending with { OldFrame = null }, pending with { Stage = null }, pending with { OriginKind = null },
            pending with { OriginKind = BypassOriginKind.Cafp }, pending with { Origin = new(0, 0) }, pending with { Origin = null },
            pending with { Completed = new(BypassPositionKind.Bypass, Entered, null, null) }, pending with { Stage = (BypassMovementStage)99 },
            pending with { Origin = new(int.MaxValue, 0) }, pending with { Stage = BypassMovementStage.EntryCompleted, Completed = new(BypassPositionKind.Ordinary, Entered, new(1, 0), 0) }];
        foreach (var facts in unknowns)
        {
            Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassVcaCalculator.DefensivePosition(facts).Disposition);
        }
        Assert.Equal(BypassDisposition.NotApplicable, ScenarioA1BypassVcaCalculator.DefensivePosition(pending with
        {
            InBypass = false
        }).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassVcaCalculator.TurnAtCafp(Turn with
        {
            Edge = null
        }).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassVcaCalculator.TurnAtCafp(Turn with
        {
            PendingTurn = null
        }).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassVcaCalculator.TurnAtCafp(Turn with
        {
            Purpose = (BypassTurnPurpose)99,
            InBypass = false
        }).Disposition);
        Assert.Equal(BypassDisposition.NotApplicable, ScenarioA1BypassVcaCalculator.TurnAtCafp(Turn with
        {
            InBypass = false
        }).Disposition);
        Assert.Equal(BypassDisposition.NotEvaluated, ScenarioA1BypassVcaCalculator.Continuation(new(true, Old, true, Lower, false, new((BypassPositionKind)99, null, null, null))).Disposition);
        Assert.Throws<ArgumentNullException>(() => ScenarioA1BypassVcaCalculator.TurnAtCafp(null!));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1BypassVcaCalculator.Continuation(null!));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1BypassVcaCalculator.EndMovement(null!));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1BypassVcaCalculator.DefensivePosition(null!));
    }

    [Fact]
    public void ContractsRejectInvalidInactiveFactsAndAllowMissingIrrelevantFacts()
    {
        var contract = ScenarioA1RuleContracts.Find("d2.33-bypass-continuation")!;
        Assert.Empty(contract.Events);
        var end = JsonNode.Parse("""{"inBypass":true,"action":"EndMovement","pendingTurn":false,"endCause":"Voluntary"}""")!.AsObject();
        Assert.Equal("evaluated", ScenarioA1RuleContracts.Evaluate(contract, end).Outcome);
        JsonObject[] invalid = [new() { ["ordinaryFacing"] = 6 }, new() { ["ordinaryFacing"] = 0.5m },
            new() { ["positionKind"] = "Ordinary", ["positionObstacleQ"] = 0 },
            new() { ["positionKind"] = "Bypass", ["ordinaryQ"] = 0 },
            new() { ["frameObstacleQ"] = 0, ["frameObstacleR"] = 0, ["frameAcrossQ"] = 4, ["frameAcrossR"] = 0 },
            new() { ["edgeOneQ"] = 0, ["edgeOneR"] = 0, ["edgeTwoQ"] = 4, ["edgeTwoR"] = 0 },
            new() { ["pendingTurn"] = true, ["entryCompleted"] = true }, new() { ["positionKind"] = "nonsense" }];
        foreach (var extra in invalid)
        {
            var facts = end.DeepClone().AsObject();
            foreach (var pair in extra)
            {
                facts[pair.Key] = pair.Value?.DeepClone();
            }
            Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        }
        end["pendingTurn"] = true;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(contract, end).Outcome);
        end["endCause"] = "Immobilized";
        end["immobilized"] = true;
        Assert.Equal("forced-end", ScenarioA1RuleContracts.Evaluate(contract, end).Outcome);
        var defensive = ScenarioA1RuleContracts.Find("d2.33-bypass-defensive-position")!;
        var exit = JsonNode.Parse("""{"stage":"EntryCompleted","positionKind":"Ordinary","ordinaryQ":0,"ordinaryR":1,"ordinaryFacing":5}""")!.AsObject();
        Assert.Equal("not-applicable", ScenarioA1RuleContracts.Evaluate(defensive, exit).Outcome);
        var turn = ScenarioA1RuleContracts.Find("d2.33-bypass-turn")!;
        Assert.All(turn.Facts.Where(f => f.Name != "vehicleId"), f => Assert.Equal(ContractDisclosure.All, f.Disclosure));
    }

    [Fact]
    public void LegacyCallbacksAndFirstRefusalsKeepTheirHistoricalReading()
    {
        var calls = new List<string>();
        bool May()
        {
            calls.Add("may");
            return false;
        }
        bool Cafp()
        {
            calls.Add("cafp");
            return true;
        }
        string? Enemy()
        {
            calls.Add("enemy");
            return "enemy-first";
        }
        string? Motion()
        {
            calls.Add("motion");
            return "motion-last";
        }
        Assert.Null(ScenarioA1VehicleMovementCalculator.StopBar("v", false, Enemy, May, true, Cafp));
        Assert.Equal(["enemy", "may", "cafp", "may"], calls);
        calls.Clear();
        Assert.Equal("motion-last", ScenarioA1VehicleMovementCalculator.EndMoveBar("v", false, null, false, Enemy, May, true, Cafp, Motion));
        Assert.Equal(["enemy", "may", "cafp", "may", "motion"], calls);
        calls.Clear();
        Assert.Contains("D2.1", ScenarioA1VehicleMovementCalculator.EndMoveBar("v", true, "target", false, Enemy, May, true, Cafp, Motion));
        Assert.Empty(calls);
        Assert.Equal("motion-last", ScenarioA1VehicleMovementCalculator.EndMoveBar("v", false, null, true, Enemy, May, true, Cafp, Motion));
        Assert.Equal(["motion"], calls);
        Assert.Throws<ArgumentNullException>(() => ScenarioA1VehicleMovementCalculator.MayMoveOn(0, 0, null!, false, Cafp, () => 2));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1VehicleMovementCalculator.StopBar("v", true, null!, May, false, Cafp));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1VehicleMovementCalculator.EndMoveBar("v", true, null, true, Enemy, May, false, Cafp, null!));
        calls.Clear();
        Assert.True(ScenarioA1VehicleMovementCalculator.MayMoveOn(0, 4, () => { calls.Add("entries"); return [(2, false)]; }, true, Cafp, () => throw new InvalidOperationException()));
        Assert.Equal(["entries"], calls);
        Assert.Contains("one hexspine", ScenarioA1VehicleMovementCalculator.TurnBar("v", 2, 0, true, () => throw new InvalidOperationException()));
    }

    [Fact]
    public void LegacyMoveOptionsPreserveOrderingReverseAndShortCircuitReads()
    {
        var reader = new LegacyReader();
        Assert.Equal(new[] { new VehicleStepOption(0, 0, 2), new(2, 0, 0), new(1, 0, 2), new(2, 0, 1) },
            ScenarioA1VehicleMovementCalculator.MoveOptions(reader, 0, 1, 5, false, () => true));
        Assert.Equal(["shared:0:1", "ends:0:2", "bypass:0", "bypass:2", "ends:1:2", "bypass:1", "bypass:2"], reader.Calls);
        Assert.Empty(ScenarioA1VehicleMovementCalculator.MoveOptions(null!, 0, 1, 5, true, () => true));
        reader.Calls.Clear();
        Assert.Equal(new[] { new VehicleStepOption(3, 1, null) }, ScenarioA1VehicleMovementCalculator.MoveOptions(reader, 0, 1, 0, true, () => false));
        Assert.Equal(["ends:0:1"], reader.Calls);
    }

    private sealed class LegacyReader : IVehicleStepFactReader
    {
        public List<string> Calls { get; } = [];
        public IReadOnlyList<int> VcaHexes(int location, int facing) => throw new InvalidOperationException("Ordinary VCA read in a Bypass fixture.");
        public IReadOnlyList<int> SharedNeighbors(int one, int two)
        {
            Calls.Add($"shared:{one}:{two}");
            return [2];
        }
        public (int? Front, int? Rear) LaneEnds(int one, int two, int facing)
        {
            Calls.Add($"ends:{one}:{two}");
            return two == 1 ? (2, 3) : (3, 0);
        }
        public bool Bypassable(int location)
        {
            Calls.Add($"bypass:{location}");
            return true;
        }
    }
}
