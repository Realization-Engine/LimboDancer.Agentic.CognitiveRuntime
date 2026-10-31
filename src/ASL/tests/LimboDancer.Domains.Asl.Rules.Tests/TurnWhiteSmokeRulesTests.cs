using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnWhiteSmokeRulesTests
{
    private static readonly string[] OrderedCounters = ["older", "smoke", "unknown"];
    private static readonly TurnRulePosition Origin = new(1, 1, "a", "pfph", TurnCheckpoint.DuringPhase);
    private static readonly TurnRulePosition Opening = new(2, 3, "a", "pfph", TurnCheckpoint.PrepFireOpening);
    private static readonly TurnWhiteSmokeFacts Full = new("smoke", "a", TurnSmokeSource.Ordnance,
        TurnSmokeChemical.Conventional, TurnSmokeSize.FiveEighthsInch, TurnSmokeColor.White, TurnSmokeStrength.Full, Origin, true);

    [Theory]
    [InlineData(TurnSmokeSource.SmokeDischarger)]
    [InlineData(TurnSmokeSource.SmokeMortar)]
    [InlineData(TurnSmokeSource.SmokePots)]
    [InlineData(TurnSmokeSource.CloseDefenseWeapon)]
    public void DispensersAlwaysUseDispersedConventionalSmoke(TurnSmokeSource source)
    {
        Assert.Equal(TurnWhiteSmokeEffect.NotEvaluated, TurnWhiteSmokeRules.Evaluate(Full with
        {
            Source = source,
            Chemical = TurnSmokeChemical.WhitePhosphorus
        }, Opening).Effect);
    }

    [Fact]
    public void CleanupIsOwnerRelativeOrderedAndIdempotent()
    {
        var dispersed = Full with
        {
            Counter = "older",
            Strength = TurnSmokeStrength.Dispersed
        };
        var result = TurnWhiteSmokeRules.Opening([Full, dispersed, Full with { Counter = "unknown", Origin = null }], Opening);
        Assert.Equal(OrderedCounters, result.Select(item => item.Counter));
        Assert.Equal(TurnWhiteSmokeEffect.Remove, result[0].Effect);
        Assert.Equal(TurnWhiteSmokeEffect.Disperse, result[1].Effect);
        Assert.Equal(TurnWhiteSmokeEffect.NotEvaluated, result[2].Effect);
        Assert.Contains("A24.4", result[0].Reference, StringComparison.Ordinal);
        var beforeOpening = Full with
        {
            Origin = Opening with
            {
                Phase = "rph",
                Checkpoint = TurnCheckpoint.DuringPhase
            }
        };
        Assert.Equal(TurnWhiteSmokeEffect.Disperse, TurnWhiteSmokeRules.Evaluate(beforeOpening, Opening).Effect);
        Assert.Equal(TurnWhiteSmokeEffect.Unchanged, TurnWhiteSmokeRules.Evaluate(beforeOpening with
        {
            LastCleanup = Opening
        }, Opening).Effect);
        var processed = Full with
        {
            Strength = TurnSmokeStrength.Dispersed,
            LastCleanup = Opening
        };
        Assert.Equal(TurnWhiteSmokeEffect.Unchanged, TurnWhiteSmokeRules.Evaluate(processed, Opening).Effect);
        Assert.Equal(TurnWhiteSmokeEffect.Unchanged, TurnWhiteSmokeRules.Evaluate(processed,
            new(2, 4, "b", "pfph", TurnCheckpoint.PrepFireOpening)).Effect);
        Assert.Equal(TurnWhiteSmokeEffect.Remove, TurnWhiteSmokeRules.Evaluate(processed,
            new(3, 5, "a", "pfph", TurnCheckpoint.PrepFireOpening)).Effect);
        Assert.Equal(TurnWhiteSmokeEffect.Unchanged, TurnWhiteSmokeRules.Evaluate(Full with
        {
            Origin = Opening
        }, Opening).Effect);
        Assert.Equal(TurnWhiteSmokeEffect.Unchanged, TurnWhiteSmokeRules.Evaluate(Full, Opening with
        {
            Checkpoint = TurnCheckpoint.DuringPhase
        }).Effect);
        Assert.Equal(TurnWhiteSmokeEffect.Unchanged, TurnWhiteSmokeRules.Evaluate(Full, Opening with
        {
            Phase = "afph"
        }).Effect);
        Assert.Equal(TurnWhiteSmokeEffect.Unchanged, TurnWhiteSmokeRules.Evaluate(Full with
        {
            Active = false
        }, Opening).Effect);
    }

    [Theory]
    [InlineData(TurnSmokeSource.Unknown)]
    [InlineData(TurnSmokeSource.InfantryGrenade)]
    [InlineData(TurnSmokeSource.VehicleGrenade)]
    [InlineData(TurnSmokeSource.Drift)]
    [InlineData(TurnSmokeSource.Blaze)]
    [InlineData(TurnSmokeSource.Cave)]
    [InlineData(TurnSmokeSource.Napalm)]
    public void SeparateSourcesCannotBePromotedByAWhiteCounterName(TurnSmokeSource source)
    {
        Assert.Equal(TurnWhiteSmokeEffect.NotEvaluated, TurnWhiteSmokeRules.Evaluate(Full with
        {
            Source = source
        }, Opening).Effect);
    }

    [Fact]
    public void MissingProvenanceStaysUnknownAndMalformedHistoryIsRejected()
    {
        foreach (var missing in new[] { Full with { OwnerSide = null }, Full with { Chemical = TurnSmokeChemical.Unknown },
            Full with { Size = TurnSmokeSize.HalfInch }, Full with { Color = TurnSmokeColor.Gray },
            Full with { Strength = TurnSmokeStrength.Unknown }, Full with { Origin = null } })
        {
            Assert.Equal(TurnWhiteSmokeEffect.NotEvaluated, TurnWhiteSmokeRules.Evaluate(missing, Opening).Effect);
        }
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Evaluate(Full with { Origin = Opening with { PlayerTurn = 4 } }, Opening));
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Evaluate(Full with { Origin = Opening with { GameTurn = 3 } }, Opening));
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Evaluate(Full with { Origin = Opening with { PhasingSide = "b" } }, Opening));
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Evaluate(Full with { Origin = Opening with { Phase = "afph" } }, Opening));
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Evaluate(Full with { LastCleanup = Origin }, Opening));
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Evaluate(Full with { LastCleanup = Opening with { PhasingSide = "b" } }, Opening));
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Evaluate(Full with { Source = (TurnSmokeSource)99 }, Opening));
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Evaluate(Full with { Chemical = (TurnSmokeChemical)99 }, Opening));
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Evaluate(Full with { Size = (TurnSmokeSize)99 }, Opening));
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Evaluate(Full with { Color = (TurnSmokeColor)99 }, Opening));
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Evaluate(Full with { Strength = (TurnSmokeStrength)99 }, Opening));
        Assert.Throws<ArgumentNullException>(() => TurnWhiteSmokeRules.Evaluate(null!, Opening));
        Assert.Throws<ArgumentNullException>(() => TurnWhiteSmokeRules.Opening(null!, Opening));
        Assert.Throws<ArgumentException>(() => TurnWhiteSmokeRules.Opening([Full, Full], Opening));
        Assert.Empty(TurnWhiteSmokeRules.Opening([], Opening));
    }
}
