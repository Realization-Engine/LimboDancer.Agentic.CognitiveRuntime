namespace LimboDancer.Domains.Asl.Rules;

public enum TurnHorseSize
{
    Unknown, Squad, HalfSquad, Smc
}

/// <summary>A13.32/G10.1 counter identity. Null Mule status is unknown, not an ordinary horse.</summary>
public sealed record TurnHorseIdentity(TurnHorseSize Size, bool? Mule, string? DesignationRule);

public static class TurnHorseRules
{
    public static string? PlayIntegrationRefusal(bool hasHorses) => hasHorses
        ? "play.pending-profile: This setup contains counter rules whose live integration remains pending (B5-D3)." : null;

    /// <summary>Bounded scenario setup facts only. DYO, Riders, movement, reduction and packing require separate integration.</summary>
    public static string? SetupRefusal(TurnHorseIdentity? identity, bool designationInScenario)
    {
        if (identity is null || !Enum.IsDefined(identity.Size))
        {
            return "A Horse counter needs typed size and Mule identity facts, with unknowns explicit.";
        }
        if (identity.Mule == true)
        {
            if (identity.Size == TurnHorseSize.Smc)
            {
                return "A Mule cannot be an SMC Horse counter (G10.1).";
            }
            if (string.IsNullOrWhiteSpace(identity.DesignationRule) || !designationInScenario)
            {
                return "A scenario-designated Mule needs its recorded scenario rule (G10.1); DYO admission remains pending.";
            }
        }
        else if (identity.DesignationRule is not null)
        {
            return "An unknown or ordinary Horse counter cannot claim a Mule designation rule.";
        }
        return null;
    }
}
