namespace LimboDancer.Domains.Asl.MapStudio.Services;

public enum AslDieRole
{
    Unspecified, White, Colored, Subsequent, Independent,
}
public enum AslRollState
{
    Recorded, Pending, Queued,
}
public sealed record AslDieView(int? Value, int Sides, AslDieRole Role, string Label);

/// <summary>Already disclosed dice. Roles are supplied by the producer, never inferred from the number of values.</summary>
public sealed record AslRollView(string Key, string Purpose, IReadOnlyList<AslDieView> Dice)
{
    public AslRollState State
    {
        get; init;
    } = AslRollState.Recorded;

    public static AslRollView Values(string key, string purpose, IReadOnlyList<int> values, int sides = 6, bool coloredFirst = false) =>
        new(key, purpose, values.Select((value, index) => new AslDieView(value, sides,
            coloredFirst && index < 2 ? (index == 0 ? AslDieRole.Colored : AslDieRole.White) : AslDieRole.Unspecified,
            coloredFirst && index < 2 ? (index == 0 ? "Colored" : "White") : $"Die {index + 1}")).ToArray());

    /// <summary>Explicit recorded-purpose mappings. Independent collections are never treated as paired DRs by their length.</summary>
    public static AslRollView Recorded(string key, string purpose, IReadOnlyList<int> values, int sides = 6, IReadOnlyList<string>? subjects = null)
    {
        var paired = purpose is "fire-ift" or "ordnance-canister" or "ordnance-to-hit" or "ordnance-to-kill" or "cc-attack"
            or "fire-check" or "fire-heat-of-battle" or "fire-leader-loss" or "fire-dc-position" or "fire-dc-second"
            or "fire-crew-survival" or "fire-task-check" or "fire-collateral-check" or "ordnance-shock-check"
            or "ordnance-crew-check" or "ordnance-crew-survival" or "ordnance-collateral-check" or "rally" or "cc-vehicle-attack"
            or "paatc" or "deployment" or "wind-change" or "rider-bail-out" or "sniper-check" or "sniper-location";
        var display = Values(key, "", values, sides, paired);
        if (subjects is not null && subjects.Count == values.Count)
        {
            return display with
            {
                Dice = display.Dice.Select((die, index) => die with { Role = AslDieRole.Independent, Label = subjects[index] }).ToArray()
            };
        }
        return purpose == "ordnance-subsequent"
            ? display with
            {
                Dice = display.Dice.Select(die => die with { Role = AslDieRole.Subsequent, Label = "Subsequent" }).ToArray()
            }
            : display;
    }
}
