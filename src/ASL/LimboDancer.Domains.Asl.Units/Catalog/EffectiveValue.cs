namespace LimboDancer.Domains.Asl.Units.Catalog;

/// <summary>One change a rule makes to a value, with the rule and page that make it.</summary>
public sealed record ValueAdjustment(string Rule, string Reason, int Delta);

/// <summary>
/// A value after terrain, leadership, condition, date, SSR, captured use, or attack mode have applied (ASL-UNIT-013).
/// It keeps the printed value it started from and every adjustment, so the printed value is never overwritten and the
/// result can be explained. No rules produce adjustments yet; they arrive with the reviewed transitions of later steps.
/// </summary>
public sealed record EffectiveValue(PrintedValue Printed, int Value, IReadOnlyList<ValueAdjustment> Adjustments)
{
    /// <summary>The effective value before any rule applies: the printed number.</summary>
    public static EffectiveValue From(PrintedValue printed)
    {
        ArgumentNullException.ThrowIfNull(printed);
        if (printed.Value?.Number is not { } number)
        {
            throw new ArgumentException($"'{printed.Name}' on the {printed.Face} face has no printed number.", nameof(printed));
        }

        return new EffectiveValue(printed, number, []);
    }

    public EffectiveValue Apply(ValueAdjustment adjustment)
    {
        ArgumentNullException.ThrowIfNull(adjustment);
        return this with
        {
            Value = Value + adjustment.Delta,
            Adjustments = [.. Adjustments, adjustment]
        };
    }
}
