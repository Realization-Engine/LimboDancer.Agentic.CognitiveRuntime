using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnDecisionWindowTests
{
    private static TurnPlacedDcWindowFacts Facts => new(new("entry", 12, 1, "a", "afph"), "dc", "origin", 1, "a", true,
        new("dc", "origin", "entry", "a", "A23.4; opening reviewed", new(TurnOpeningEvidence.Resolved,
            TurnOpeningEvidence.NotApplicable, TurnOpeningEvidence.Resolved, TurnOpeningEvidence.NotApplicable)));

    [Fact]
    public void CompleteEvidenceIsScopedTimingOnlyAndHistoricalReadsRetainIt()
    {
        var result = TurnDecisionWindows.Describe(Facts, false);
        Assert.Equal(TurnDecisionTiming.ReadyForCommandAssessment, result.Timing);
        Assert.Equal("dc", result.SubjectId);
        Assert.Equal("afph:asop:5.22A", result.WindowId);
        Assert.False(result.Historical);
        Assert.Equal(Facts.Review!.Opening, result.Opening);
        Assert.Equal(6, result.SourceTiming.Length);
        Assert.Equal(4, result.Pending.Length);
        var past = TurnDecisionWindows.Describe(Facts, true);
        Assert.True(past.Historical);
        Assert.Equal(result.Timing, past.Timing);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(past with
        {
            Historical = false
        }));
        var restored = JsonSerializer.Deserialize<TurnPlacedDcWindowFacts>(JsonSerializer.Serialize(Facts))!;
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(TurnDecisionWindows.Describe(restored, false)));
    }

    [Fact]
    public void MissingAndMismatchedReviewsNeverInventOpeningCompletion()
    {
        var absent = TurnDecisionWindows.Describe(Facts with
        {
            Review = null
        }, false);
        Assert.Null(absent.Opening);
        Assert.Equal(TurnDecisionTiming.NotEvaluated, absent.Timing);
        var stale = TurnDecisionWindows.Describe(Facts with
        {
            Occurrence = Facts.Occurrence! with
            {
                EventId = "other-afph"
            }
        }, false);
        Assert.Null(stale.Opening);
        Assert.Equal(TurnDecisionTiming.NotEvaluated, stale.Timing);
        var unknown = Facts with
        {
            Review = Facts.Review! with
            {
                Opening = Facts.Review!.Opening with
                {
                    Drift = TurnOpeningEvidence.Unknown
                }
            }
        };
        Assert.Equal(TurnDecisionTiming.NotEvaluated, TurnDecisionWindows.Describe(unknown, false).Timing);
        Assert.Equal(TurnOpeningEvidence.Unknown, TurnDecisionWindows.Describe(unknown, false).Opening!.Drift);
        Assert.Equal(TurnDecisionTiming.OutsideWindow, TurnDecisionWindows.Describe(Facts with
        {
            Operable = false
        }, false).Timing);
    }
}
