namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Where a range falls for a firer or a weapon (A7.21, A7.22).</summary>
public enum FireRangeBand
{
    /// <summary>The firer's own Location: TPBF (A7.21).</summary>
    TriplePointBlank,

    /// <summary>Another level of the firer's own hex, which the Fire package does not resolve.</summary>
    SameHexOtherLevel,

    /// <summary>An adjacent target at most one level above: PBF (A7.21).</summary>
    PointBlank,

    Normal,

    /// <summary>Beyond Normal Range, up to twice it (A7.22).</summary>
    LongRange,

    Out,
}

/// <summary>A range read against a Normal Range: its band, and the farthest range the firer or weapon fires to.</summary>
public sealed record FireRangeReading(FireRangeBand Band, int Range, int NormalRange, int Limit)
{
    /// <summary>The FP multiplier the band gives, or null where there is no attack.</summary>
    public decimal? Multiplier => Band switch
    {
        FireRangeBand.TriplePointBlank => 3m,
        FireRangeBand.PointBlank => 2m,
        FireRangeBand.Normal => 1m,
        FireRangeBand.LongRange => 0.5m,
        _ => null,
    };
}

/// <summary>
/// Range read before an attack (pass 31c, design section 14): the band a range falls in for a Normal Range, in the words a
/// player uses. It resolves nothing; <see cref="ScenarioA1FireCalculator"/> decides an attack's FP and its refusals, and the
/// tests hold the two together.
/// </summary>
public static class FireRange
{
    /// <summary>
    /// The band of a range. <paramref name="levelAbove"/> is the target's level less the firer's; a weapon with
    /// <paramref name="noLongRange"/> fires to its Normal Range only (an ATR, C13.24), and one with
    /// <paramref name="noPointBlank"/> is never raised for PBF or TPBF (a FT, A22.1).
    /// </summary>
    public static FireRangeReading Band(int range, bool sameLocation, int normalRange, int levelAbove = 0, bool noLongRange = false, bool noPointBlank = false)
    {
        var limit = noLongRange ? normalRange : 2 * normalRange;
        var band = sameLocation ? noPointBlank ? FireRangeBand.Normal : FireRangeBand.TriplePointBlank
            : range < 1 ? FireRangeBand.SameHexOtherLevel
            : range > limit ? FireRangeBand.Out
            : range > normalRange ? FireRangeBand.LongRange
            : range == 1 && levelAbove <= 1 && !noPointBlank ? FireRangeBand.PointBlank
            : FireRangeBand.Normal;
        return new FireRangeReading(band, sameLocation ? 0 : range, normalRange, limit);
    }

    /// <summary>The band of a firer's own FP or of a weapon it fires, from its reviewed definition; null when it prints no range.</summary>
    public static FireRangeReading? Band(FireDefinition definition, int range, bool sameLocation, int levelAbove = 0, bool wounded = false)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var normal = wounded && definition.WoundedRange is { } woundedRange ? woundedRange : definition.Range;
        return normal is { } printed ? Band(range, sameLocation, printed, levelAbove, definition.IsAtr, definition.IsFt) : null;
    }

    /// <summary>A reading in words: "Long Range, FP x1/2 (A7.22)", "out of range: it fires to 12".</summary>
    public static string Words(FireRangeReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return reading.Band switch
        {
            FireRangeBand.TriplePointBlank => "TPBF, FP x3 (A7.21)",
            FireRangeBand.SameHexOtherLevel => "the same hex, another level: not built",
            FireRangeBand.PointBlank => "PBF, FP x2 (A7.21)",
            FireRangeBand.Normal => "Normal Range",
            FireRangeBand.LongRange => "Long Range, FP x1/2 (A7.22)",
            _ => $"out of range: it fires to {reading.Limit}",
        };
    }
}
