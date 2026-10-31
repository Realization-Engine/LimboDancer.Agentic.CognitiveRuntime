namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A24.4 (PDF p. 92): counter height and column intersection. Its Blaze-height clause is decided by ScenarioA1BlazeSmokeCalculator.Height;
/// both heights feed Intersect. Hindrance strength remains ScenarioA1SmokeStrengthCalculator's A24.5 decision.</summary>
public static class ScenarioA1SmokeHeightCalculator
{
    /// <summary>A24.2/.8: an exact source Location proves occupancy without another LOS walk. An end elsewhere in the same hex still needs height facts.</summary>
    public static SmokeLosVerdict? ExactLocation(bool sourceLocation, bool targetLocation, bool sourceHex, bool targetHex) =>
        sourceLocation && targetLocation || sourceLocation && !targetHex || targetLocation && !sourceHex
            ? new(true, sourceLocation, targetLocation) : null;

    /// <summary>Conventional SMOKE rises two levels, WP four. Unsupported source or chemical facts remain unknown.</summary>
    public static int? Height(TurnSmokeSource source, TurnSmokeChemical chemical)
    {
        if (!Enum.IsDefined(source) || !Enum.IsDefined(chemical))
        {
            throw new ArgumentException("Unknown SMOKE source or chemical.");
        }

        if (source is TurnSmokeSource.Unknown or TurnSmokeSource.Blaze or TurnSmokeSource.Cave or TurnSmokeSource.Napalm
            || chemical == TurnSmokeChemical.Unknown)
        {
            return null;
        }
        return chemical == TurnSmokeChemical.WhitePhosphorus ? 4 : 2;
    }

    /// <summary>A24.4/A6.4-.43: vertical intersection of a planar LOS with one SMOKE source. Null leaves an unreviewed endpoint case undecided.</summary>
    public static SmokeLosVerdict? Intersect(SmokeLosFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(facts.Source);
        ArgumentNullException.ThrowIfNull(facts.Target);
        ArgumentOutOfRangeException.ThrowIfNegative(facts.Height);
        ArgumentOutOfRangeException.ThrowIfNegative(facts.RangeFromSource);
        ArgumentOutOfRangeException.ThrowIfNegative(facts.RangeToTarget);
        if (facts.Height == 0 || !facts.Crossed && !facts.SourceHex && !facts.TargetHex)
        {
            return new(false, false, false);
        }

        var bottom = facts.SmokeBase;
        var top = bottom + facts.Height;
        var source = facts.Source.Elevation + facts.Source.RooftopAdjustment;
        var target = facts.Target.Elevation + facts.Target.RooftopAdjustment;
        var sourceWithin = facts.SourceHex && source >= bottom && source < top;
        var targetWithin = facts.TargetHex && target >= bottom && target < top;
        if (sourceWithin || targetWithin)
        {
            return new(true, sourceWithin, targetWithin);
        }
        if (Math.Max(source, target) < bottom || Math.Min(source, target) >= top)
        {
            return new(false, false, false);
        }
        if (facts.Source.EmbankmentAdjustment != 0 || facts.Target.EmbankmentAdjustment != 0)
        {
            return null;
        }

        if (facts.SourceHex || facts.TargetHex)
        {
            // A24.4 example and Perry Sez, combined Q&A p.73: also above the top, regardless of range.
            if (facts.SourceHex && source >= top && target < top || facts.TargetHex && target >= top && source < top)
            {
                return new(true, false, false);
            }
            // A24.4/O6.3, Journal 1 Q&A: a cellar LOS through its ground-level SMOKE, without the outgoing +1.
            if (facts.SmokeBase == facts.GroundLevel && (facts.SourceHex && facts.Source.Cellar && source + 1 == bottom && target >= bottom
                || facts.TargetHex && facts.Target.Cellar && target + 1 == bottom && source >= bottom))
            {
                return new(true, false, false);
            }
            return null;
        }

        var higher = Math.Max(source, target);
        var lower = Math.Min(source, target);
        if (bottom > facts.GroundLevel && lower < bottom)
        {
            return null;
        }
        if (higher < top)
        {
            return new(true, false, false);
        }

        var highEnd = source >= target ? facts.Source : facts.Target;
        var rangeFromHigher = source >= target ? facts.RangeFromSource : facts.RangeToTarget;
        var rangeToLower = source >= target ? facts.RangeToTarget : facts.RangeFromSource;
        if ((highEnd.Hillock || highEnd.Upslope) && highEnd.Elevation == top)
        {
            // No reviewed rule establishes an extra level for this endpoint geometry.
            return null;
        }

        // A6.42 (p. 53): only full levels count. Apply its minimum before A6.43 adjusts for the hex behind.
        // The extra hex at an endpoint exactly at the top follows the A24.4 example (ruling RA24H.1).
        var advantage = decimal.Floor(higher) - top;
        var blind = Math.Max(facts.Height + rangeFromHigher / 5 - advantage + 1, 1)
            + bottom - decimal.Floor(lower);
        return new(rangeToLower <= blind, false, false);
    }
}
