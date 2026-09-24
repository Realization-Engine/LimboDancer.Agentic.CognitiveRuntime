namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Computes the A5.1/A5.11/A5.5 cost for the one admitted friendly stack.</summary>
public static class ScenarioA1StackingCost
{
    public static bool IsReviewedThreeMfEntry(IReadOnlyDictionary<string, string> facts)
    {
        if (!int.TryParse(facts["friendlySquads"], out var squads)
            || !int.TryParse(facts["friendlyUnmannedCrewsOrHalfSquads"], out var halfSquads)
            || !int.TryParse(facts["friendlySmc"], out var smc)
            || !int.TryParse(facts["incomingSquads"], out var incoming)
            || squads != 2 || halfSquads != 1 || smc != 0 || incoming != 1)
        {
            return false;
        }

        // Half-squad/crew equivalents count as halves; A5.11 rounds up the
        // squad equivalent exceeding the three-squad normal limit.
        var excessHalfEquivalents = Math.Max(0, 2 * (squads + incoming) + halfSquads - 6);
        var penaltyMf = (excessHalfEquivalents + 1) / 2;
        return 2 + penaltyMf == 3;
    }
}
