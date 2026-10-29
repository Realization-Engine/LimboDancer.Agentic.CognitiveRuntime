using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The replacement of a lost mortar Spotter at the owner's MPh opening (C9.3; ASOP 3.11A, p. 679), as <see cref="ScenarioA1SpotterCalculator.Replacement"/>
/// decides it, the opening's steps, the spotted fire's marks, and the contract <c>c9.3-spotter-replacement</c> (the replacement design, RC93.11 to RC93.16).
/// </summary>
public sealed class ScenarioA1SpotterReplacementTests
{
    private static readonly TurnRulePosition Opening = new(1, 2, "a", "mph", TurnCheckpoint.DuringPhase);
    private static readonly TurnRulePosition Earlier = new(1, 1, "b", "ccph", TurnCheckpoint.DuringPhase);
    private static readonly SpotterCandidateFacts Good = new(true, true, true, true, false, false, false);

    private static SpotterReplacementFacts Facts(SpotterChange change = SpotterChange.Broken) =>
        new(new SpotterTimingFacts("a", Opening, true, change, Earlier), [], true, Good, 1, true, false, false);

    private static string Refusal(SpotterReplacementFacts facts)
    {
        var verdict = ScenarioA1SpotterCalculator.Replacement(facts);
        Assert.Equal(SpotterReplacementState.Refused, verdict.State);
        return verdict.Refusal!;
    }

    private static RuleContract Contract => ScenarioA1RuleContracts.Find("c9.3-spotter-replacement")!;

    private static JsonObject ContractFacts() => new()
    {
        ["phase"] = "MPh",
        ["ownerPhasing"] = true,
        ["holderId"] = "g-crew",
        ["mortar"] = true,
        ["hasDesignation"] = true,
        ["lossCause"] = "broken",
        ["lossBeforeThisPhase"] = true,
        ["openingClosed"] = false,
        ["hexRange"] = 1,
        ["sharesLocation"] = true,
        ["spotterId"] = "g-squad",
        ["spotterActive"] = true,
        ["spotterSameSide"] = true,
        ["spotterPersonnel"] = true,
        ["spotterGoodOrder"] = true,
        ["spotterCaptured"] = false,
        ["spotterMelee"] = false,
        ["spotterIsMortarCrew"] = false,
        ["night"] = false,
        ["pendingDecision"] = false,
    };

    [Fact]
    public void OnlyReplacementsKeepTheOpeningOpen()
    {
        Assert.True(ScenarioA1SpotterCalculator.OpeningOpen([]));
        Assert.True(ScenarioA1SpotterCalculator.OpeningOpen([MovementOpeningStep.SpotterReplacement, MovementOpeningStep.SpotterReplacement]));
        foreach (var step in Enum.GetValues<MovementOpeningStep>().Where(step => step != MovementOpeningStep.SpotterReplacement))
        {
            Assert.False(ScenarioA1SpotterCalculator.OpeningOpen([MovementOpeningStep.SpotterReplacement, step]));
            Assert.Equal(TurnCheckpoint.UnitMovement, ScenarioA1SpotterCalculator.OpeningCheckpoint([step]));
        }

        Assert.Equal(TurnCheckpoint.MovementStart, ScenarioA1SpotterCalculator.OpeningCheckpoint([]));
    }

    [Theory]
    [InlineData(MovementOpeningStep.SpotterReplacement, "mortar-spotter-designated")]
    [InlineData(MovementOpeningStep.SpotterReplacement, "mortar-spotter-designated", "dice-rolled", "conditions-changed")]
    [InlineData(MovementOpeningStep.Starshell, "starshell-fired", "dice-rolled")]
    [InlineData(MovementOpeningStep.UnitMovement, "movement-step")]
    [InlineData(MovementOpeningStep.UnitMovement, "vehicle-step")]
    [InlineData(MovementOpeningStep.UnitMovement, "instance-moved")]
    [InlineData(MovementOpeningStep.UnitMovement, "movement-ended")]
    [InlineData(MovementOpeningStep.UnitMovement, "motion-attempted")]
    [InlineData(MovementOpeningStep.UnitMovement, "entry-attempted")]
    [InlineData(MovementOpeningStep.UnitMovement, "overrun-declared")]
    [InlineData(MovementOpeningStep.UnitMovement, "gun-hooked")]
    [InlineData(MovementOpeningStep.UnitMovement, "manhandling-rolled")]
    [InlineData(MovementOpeningStep.UnitMovement, "units-searched")]
    [InlineData(MovementOpeningStep.UnitMovement, "turn-work-recorded")]
    [InlineData(MovementOpeningStep.UnitMovement, "mortar-spotter-designated", "movement-step")]
    [InlineData(MovementOpeningStep.Other, "mortar-spotter-designated", "ordnance-fired")]
    [InlineData(MovementOpeningStep.Other, "something-new")]
    [InlineData(MovementOpeningStep.Other)]
    public void EachCommandMapsToItsStepByTheEventsItRecorded(MovementOpeningStep expected, params string[] events) =>
        Assert.Equal(expected, ScenarioA1SpotterCalculator.OpeningStepOf(events));

    [Theory]
    [InlineData(SpotterChange.Eliminated)]
    [InlineData(SpotterChange.Broken)]
    [InlineData(SpotterChange.Captured)]
    public void ALostSpotterIsReplacedAtTheNextOwnersMovementOpeningWithNoFireCounter(SpotterChange change)
    {
        var verdict = ScenarioA1SpotterCalculator.Replacement(Facts(change));
        Assert.Equal((SpotterReplacementState.MayReplace, true, (string?)null), (verdict.State, verdict.EstablishesAssignment, verdict.Refusal));
        Assert.Contains("no fire counter", verdict.Reason, StringComparison.Ordinal);

        // Another replacement already taken keeps the opening open (RC93.11).
        Assert.Equal(SpotterReplacementState.MayReplace, ScenarioA1SpotterCalculator.Replacement(Facts(change) with
        {
            StepsTaken = [MovementOpeningStep.SpotterReplacement]
        }).State);
    }

    [Fact]
    public void TheChecksRefuseInTheirOrder()
    {
        var bad = new SpotterCandidateFacts(true, true, true, false, false, false, false);
        var all = Facts() with
        {
            PendingDecision = true,
            Mortar = false,
            Candidate = bad,
            Night = true,
            StepsTaken = [MovementOpeningStep.UnitMovement],
            HexRange = 2,
            SharesLocation = false
        };
        Assert.StartsWith("Complete the outstanding", Refusal(all), StringComparison.Ordinal);
        Assert.Contains("active mortar", Refusal(all with
        {
            PendingDecision = false
        }), StringComparison.Ordinal);
        Assert.Contains("Good Order", Refusal(all with
        {
            PendingDecision = false,
            Mortar = true
        }), StringComparison.Ordinal);
        Assert.Contains("night", Refusal(all with
        {
            PendingDecision = false,
            Mortar = true,
            Candidate = Good
        }), StringComparison.Ordinal);
        Assert.Contains("ASOP 3.11A", Refusal(all with
        {
            PendingDecision = false,
            Mortar = true,
            Candidate = Good,
            Night = false
        }), StringComparison.Ordinal);
        // The timing before the range and the sharing: a loss in this same MPh, then an unknown loss with an unknown range.
        Assert.Contains("Wait for an owner's MPh opening", Refusal(all with
        {
            PendingDecision = false,
            Mortar = true,
            Candidate = Good,
            Night = false,
            StepsTaken = [],
            Timing = Facts().Timing with
            {
                LossAt = Opening
            }
        }), StringComparison.Ordinal);
        var unknownTiming = ScenarioA1SpotterCalculator.Replacement(Facts(SpotterChange.Unknown) with
        {
            HexRange = null
        });
        Assert.Equal(SpotterReplacementState.NotEvaluated, unknownTiming.State);
        Assert.Contains("loss history is unknown", unknownTiming.Reason, StringComparison.Ordinal);
        Assert.Contains("2 hexes away", Refusal(all with
        {
            PendingDecision = false,
            Mortar = true,
            Candidate = Good,
            Night = false,
            StepsTaken = []
        }), StringComparison.Ordinal);
        Assert.Equal(Refusal(Facts() with
        {
            HexRange = 2
        }), ScenarioA1SpotterCalculator.ReplacementRefusal(Facts() with
        {
            HexRange = 2
        }));
        Assert.Contains("one Location", Refusal(Facts() with
        {
            SharesLocation = false
        }), StringComparison.Ordinal);

        // The candidate's own refusals: the mortar's crew, a Captured unit, one in Melee.
        Assert.Contains("own crew", Refusal(Facts() with
        {
            Candidate = Good with
            {
                MortarCrew = true
            }
        }), StringComparison.Ordinal);
        Assert.Contains("Captured", Refusal(Facts() with
        {
            Candidate = Good with
            {
                Captured = true
            }
        }), StringComparison.Ordinal);
        Assert.Contains("Melee", Refusal(Facts() with
        {
            Candidate = Good with
            {
                Melee = true
            }
        }), StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownTimingRangeOrSharingIsNotEvaluated()
    {
        Assert.Equal(SpotterReplacementState.NotEvaluated, ScenarioA1SpotterCalculator.Replacement(Facts(SpotterChange.Unknown)).State);
        Assert.Equal(SpotterReplacementState.NotEvaluated, ScenarioA1SpotterCalculator.Replacement(Facts() with
        {
            Timing = Facts().Timing with
            {
                HasDesignation = null
            }
        }).State);
        Assert.Equal(SpotterReplacementState.NotEvaluated, ScenarioA1SpotterCalculator.Replacement(Facts() with
        {
            HexRange = null
        }).State);
        Assert.Equal(SpotterReplacementState.NotEvaluated, ScenarioA1SpotterCalculator.Replacement(Facts() with
        {
            SharesLocation = null
        }).State);
        Assert.Equal(SpotterReplacementState.NotEvaluated, ScenarioA1SpotterCalculator.Replacement(Facts() with
        {
            Timing = Facts().Timing with
            {
                Change = SpotterChange.SquadReduced,
                GoodOrderReductionSurvivor = null
            }
        }).State);
    }

    [Fact]
    public void NoWindowWithoutALossOrWhileAGoodOrderHalfSquadSpots()
    {
        // RC93.14: while the assignment is not lost, a second replacement of the same mortar is refused.
        Assert.Equal(SpotterReplacementState.Refused, ScenarioA1SpotterCalculator.Replacement(Facts() with
        {
            Timing = new SpotterTimingFacts("a", Opening, true, SpotterChange.None)
        }).State);
        Assert.Equal(SpotterReplacementState.Refused, ScenarioA1SpotterCalculator.Replacement(Facts() with
        {
            Timing = Facts().Timing with
            {
                Change = SpotterChange.SquadReduced,
                GoodOrderReductionSurvivor = true
            }
        }).State);

        // A mortar with no Spotter yet designates its first in the PFPh or DFPh (ASOP 2.22A, 4.21D), not here.
        Assert.Equal(SpotterReplacementState.Refused, ScenarioA1SpotterCalculator.Replacement(Facts() with
        {
            Timing = new SpotterTimingFacts("a", Opening, false, SpotterChange.None)
        }).State);
    }

    [Fact]
    public void RefusedInTheOpponentsMovementInThePrepFireAndInTheMovementOfTheLoss()
    {
        Assert.Equal(SpotterReplacementState.Refused, ScenarioA1SpotterCalculator.Replacement(Facts() with
        {
            Timing = Facts().Timing with
            {
                Now = Opening with
                {
                    PhasingSide = "b"
                }
            }
        }).State);
        Assert.Equal(SpotterReplacementState.Refused, ScenarioA1SpotterCalculator.Replacement(Facts() with
        {
            Timing = Facts().Timing with
            {
                Now = Opening with
                {
                    Phase = "pfph"
                }
            }
        }).State);
        Assert.Equal(SpotterReplacementState.Refused, ScenarioA1SpotterCalculator.Replacement(Facts() with
        {
            Timing = Facts().Timing with
            {
                LossAt = Opening
            }
        }).State);
    }

    [Fact]
    public void AReplacementTakesNoTemporaryImmobilizationFactAndAFirstDesignationIsNotOne()
    {
        // The design's review, finding 2: a replacement uses nothing, so there is no TI check (RC93.12) and its facts carry no TI member.
        Assert.DoesNotContain(typeof(SpotterReplacementFacts).GetProperties(), property => property.Name.Contains("Immobilized", StringComparison.Ordinal));

        // A mortar with no Spotter, asked in its owner's PFPh, is refused with its own reason, not Timing's permission (the build's review, finding 1).
        Assert.Contains("not by replacement", Refusal(Facts() with
        {
            Timing = new SpotterTimingFacts("a", Opening with
            {
                Phase = "pfph"
            }, false, SpotterChange.None)
        }), StringComparison.Ordinal);
    }

    [Fact]
    public void SpottingMarksItsSpotterAsTheUseOfASupportWeapon()
    {
        Assert.Equal(new SpottingMarksVerdict(UnitCondition.PrepFire, true), ScenarioA1SpotterCalculator.SpottingMarks("pfph", false));
        Assert.Equal(new SpottingMarksVerdict(UnitCondition.FinalFire, false), ScenarioA1SpotterCalculator.SpottingMarks("dfph", false));
        Assert.Equal(new SpottingMarksVerdict(null, true), ScenarioA1SpotterCalculator.SpottingMarks("pfph", true));
        Assert.Equal(new SpottingMarksVerdict(null, false), ScenarioA1SpotterCalculator.SpottingMarks("dfph", true));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.SpottingMarks("mph", false));
    }

    [Fact]
    public void TheContractDecidesAReplacementFromTheOwnersFacts()
    {
        Assert.Equal("may-replace", ScenarioA1RuleContracts.Evaluate(Contract, ContractFacts()).Outcome);

        var moved = ContractFacts();
        moved["openingClosed"] = true;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(Contract, moved).Outcome);

        var same = ContractFacts();
        same["lossBeforeThisPhase"] = false;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(Contract, same).Outcome);

        var reduced = ContractFacts();
        reduced["lossCause"] = "reduced";
        reduced["reducedSurvivorGoodOrder"] = true;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(Contract, reduced).Outcome);

        // A Reduction is decided by its survivor alone, so its moment is not needed (the build's review, finding 2).
        reduced["lossBeforeThisPhase"] = null;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(Contract, reduced).Outcome);

        var unknownCause = ContractFacts();
        unknownCause["lossCause"] = "unknown";
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Contract, unknownCause).Outcome);

        // With no loss the "before this phase" fact is not needed, and the replacement is refused.
        var kept = ContractFacts();
        kept["lossCause"] = "none";
        kept["lossBeforeThisPhase"] = null;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(Contract, kept).Outcome);
    }

    [Fact]
    public void TheMortarsFactsAreItsHoldersSoAnotherViewerLearnsNoLoss()
    {
        // RC93.16: the mortar's and the loss's facts concern holderId; the candidate's concern spotterId.
        foreach (var name in new[] { "mortar", "hasDesignation", "lossCause", "reducedSurvivorGoodOrder", "lossBeforeThisPhase", "openingClosed", "hexRange", "sharesLocation" })
        {
            var fact = Contract.Facts.Single(fact => fact.Name == name);
            Assert.Equal((ContractDisclosure.Owner, "holderId"), (fact.Disclosure, fact.Concerns));
        }

        Assert.All(Contract.Facts.Where(fact => fact.Name.StartsWith("spotter", StringComparison.Ordinal) && fact.Name != "spotterId"),
            fact => Assert.Equal((ContractDisclosure.Owner, "spotterId"), (fact.Disclosure, fact.Concerns)));

        // The viewer who owns the candidate but not the mortar knows none of the holder's facts: not evaluated, whatever the candidate.
        var view = ContractFacts();
        foreach (var name in new[] { "mortar", "hasDesignation", "lossCause", "lossBeforeThisPhase", "openingClosed", "hexRange", "sharesLocation" })
        {
            view[name] = null;
        }

        var verdict = ScenarioA1RuleContracts.Evaluate(Contract, view);
        Assert.Equal(ContractVerdict.NotEvaluated, verdict.Outcome);
        Assert.DoesNotContain(verdict.Reasons, reason => reason.Text.Contains("broken", StringComparison.OrdinalIgnoreCase));
    }
}
