namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A25.61 and A25.66 (p. 97), RN1.1 to RN1.4: the Italian squad deployment restriction.</summary>
public static class ScenarioA1ItalianDeploymentCalculator
{
    /// <summary>The three printed elite Italian MMC; not an Eritrean classification or an ELR table.</summary>
    public static bool EliteMmc(string? nationality, string kind, int firepower, int range, int morale) =>
        nationality == "italian" && (kind, firepower, range, morale) is ("squad", 4, 4, 7) or ("half-squad", 2, 4, 7) or ("crew", 2, 2, 7);

    public static ItalianDeploymentVerdict Evaluate(ItalianDeploymentFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Context is not ("setup" or "ordinary" or "guard" or "unarmed" or "temporary-crew"))
        {
            return Unknown();
        }

        if (facts.Nationality is null)
        {
            return Unknown();
        }

        if (!ScenarioA1ItalianNationalityRules.UsesItalianDeploymentAndCapture(facts.Nationality))
        {
            return new("not-applicable", "This nationality is outside the Italian restriction (A25.61, A25.66).");
        }

        if (facts.Kind is null)
        {
            return Unknown();
        }

        if (facts.Kind != "squad")
        {
            return new("not-applicable", "A25.61's deployment restriction concerns squads, not permission for other units to Deploy.");
        }

        if (facts.Elite == true)
        {
            return Unrestricted("Elite Italian-treatment squads pass the nationality restriction (A25.61, A25.66).");
        }

        if (facts.Context == "guard")
        {
            if (facts.Guard != true || facts.Broken is null)
            {
                return Unknown();
            }

            if (ScenarioA1SupportWeaponRules.GuardDeploysAtWill(true, true, facts.Broken.Value))
            {
                return Unrestricted("An unbroken Guard squad Deploys automatically regardless of nationality (A20.5).");
            }
        }

        if (facts.Context == "unarmed")
        {
            return facts.Unarmed == true ? Unrestricted("The Unarmed exchange is exempt (A20.5, A25.61).") : Unknown();
        }

        if (facts.Context == "temporary-crew")
        {
            return Unrestricted("A21.22's Temporary Crew exchange is exempt from this nationality restriction; its other requirements still apply.");
        }

        return facts.Elite switch
        {
            false => new("restricted", "Only elite Italian-treatment squads may Deploy outside the stated exceptions (A25.61, A25.66)."),
            _ => Unknown(),
        };
    }

    private static ItalianDeploymentVerdict Unknown() => new("not-evaluated", "A material deployment fact or context is unavailable (A25.61).");
    private static ItalianDeploymentVerdict Unrestricted(string reason) => new("unrestricted", reason);
}
