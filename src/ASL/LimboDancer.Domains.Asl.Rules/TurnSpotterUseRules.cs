namespace LimboDancer.Domains.Asl.Rules;

/// <summary>One observed mortar use by the same Spotter in this Player Turn. Location includes level.</summary>
public sealed record TurnSpotterUse(string Gun, string? Location, string Target);

public static class TurnSpotterUseRules
{
    /// <summary>C9.31: sharing more than one mortar requires the same Location and target throughout the Player Turn.</summary>
    public static bool Compatible(IReadOnlyList<TurnSpotterUse> previous, string gun, string? location, string target)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentException.ThrowIfNullOrWhiteSpace(gun);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        return previous.All(item => item.Gun == gun)
            || location is not null && previous.All(item => item.Location == location && item.Target == target);
    }
}
