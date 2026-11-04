namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A25.71 (p. 97), A1.31-.32 (p. 45), RN1.8 to RN1.11: Finnish organization requirements.</summary>
public static class ScenarioA1FinnishOrganizationCalculator
{
    public static FinnishOrganizationVerdict Evaluate(FinnishOrganizationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Action is not ("deploy" or "recombine") ||
            (facts.Action == "recombine" ? facts.Mode != "not-applicable" : facts.Mode is not ("directed" or "leaderless")))
        {
            return Unknown();
        }

        if (facts.Nationality is null || (facts.Nationality == "finnish" && facts.Kind is null))
        {
            return Unknown();
        }

        if (facts.Nationality != "finnish" || facts.Kind != (facts.Action == "deploy" ? "squad" : "half-squad"))
        {
            return new("not-applicable", null, null, "This is outside A25.71's Finnish squad/HS organization requirements.");
        }

        if (facts.Action == "recombine")
        {
            return new("no-leader-required", false, null, "Finnish HS require no leader to Recombine; all other A1.32 requirements remain.");
        }

        if ((facts.Guard is null && facts.Broken != true) || (facts.Guard == true && facts.Broken is null))
        {
            return Unknown();
        }

        if (ScenarioA1SupportWeaponRules.GuardDeploysAtWill(true, facts.Guard ?? false, facts.Broken ?? false))
        {
            return new("automatic", false, null, "An unbroken Guard squad Deploys automatically (A20.5).");
        }

        if (facts.Mode == "directed")
        {
            return new("directed-ntc", true, 0, "Normal deployment NTC: add the eligible directing leader's DRM separately (A1.31, A25.71).");
        }

        if (facts.Guard != true && facts.Unarmed != true && (facts.Guard is null || facts.Unarmed is null))
        {
            return Unknown();
        }

        if (ScenarioA1SupportWeaponRules.NeedsNoLeader(facts.Guard ?? false, facts.Unarmed ?? false))
        {
            return new("leaderless-ntc", false, 0, "The existing Guard/Unarmed exception needs no leader (A1.31, A20.5).");
        }

        return new("leaderless-1tc", false, 1, "The chosen Finnish leaderless deployment requires a 1TC (A25.71).");
    }

    private static FinnishOrganizationVerdict Unknown() => new("not-evaluated", null, null, "A material organization fact or mode is unavailable (A25.71).");
}
