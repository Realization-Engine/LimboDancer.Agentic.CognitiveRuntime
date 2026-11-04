namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The building TEM component of B23.31 (p. 136), composed with the existing B23.741 Factory calculator. Does not decide total DRM,
/// FFMO, FT, Snap Shots, CH reversal, or Set DC's no-TEM resolution (A23.71, p. 91). Existing lane callers keep their recorded reading.
/// </summary>
public static class ScenarioA1BuildingFireCalculator
{
    /// <summary>Return the building contribution, without deciding an attack's eligibility to use that contribution.</summary>
    public static BuildingFireVerdict BuildingTem(BuildingFireFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (Invalid(facts.Building) || Invalid(facts.Location) || Invalid(facts.Profile) || Invalid(facts.Attack)
            || Invalid(facts.Firer) || Invalid(facts.ResidualEntry))
        {
            return Unknown("A supplied building fact has an unsupported value.");
        }

        var factory = facts.Building is BuildingFireKind.WoodFactory or BuildingFireKind.StoneFactory;
        var bypass = facts.Bypassing;
        var entry = facts.ResidualEntry;
        if (factory && facts.Attack == BuildingFireAttack.Residual)
        {
            if (entry is { } knownEntry)
            {
                var enteredBypass = knownEntry == FactoryResidualEntry.Bypass;
                if (bypass is { } knownBypass && knownBypass != enteredBypass)
                {
                    return Unknown("Factory Residual entry and Bypass facts contradict one another.");
                }

                bypass = enteredBypass;
            }
            else if (bypass == true)
            {
                entry = FactoryResidualEntry.Bypass;
            }
        }

        if (facts.Building == BuildingFireKind.None)
        {
            return new(BuildingFireDisposition.NotApplicable, null, "There is no building contribution; other terrain protection is separate.", "B23.31", 136);
        }

        if (facts.Building is null || facts.Location is null)
        {
            return Unknown("The target building and Location kind must be known.");
        }

        if (facts.Location == BuildingFireLocation.UnsupportedSpecial)
        {
            return Unknown("This special building Location needs its own rule; no ordinary building answer is assumed.");
        }

        if (bypass == true && facts.Attack is BuildingFireAttack.Direct or BuildingFireAttack.Indirect)
        {
            return new(BuildingFireDisposition.Evaluated, 0, "Bypass removes the building contribution against non-Residual FP only.", "B23.31", 136);
        }

        if (bypass != false && facts.Attack is null or BuildingFireAttack.Residual && facts.Profile != BuildingFireProfile.Standard)
        {
            return Unknown("Possible Deluxe Bypass Residual requires J2.23's separate hexside reading.", "J2.23", 602);
        }

        if (!factory && (bypass == false || facts.Attack == BuildingFireAttack.Residual && facts.Profile == BuildingFireProfile.Standard))
        {
            var tem = ScenarioA1FactoryCalculator.BuildingTem(facts.Building == BuildingFireKind.Stone);
            return new(BuildingFireDisposition.Evaluated, tem, "The target's building determines TEM, including same-building fire and standard-ASL Residual FP.", "B23.31", 136);
        }

        if (facts.Attack is null || facts.Attack != BuildingFireAttack.Residual && bypass is null)
        {
            return Unknown("Bypass or attack classification is materially unknown.");
        }

        if (!factory)
        {
            return Unknown("The remaining building facts do not establish a contribution.");
        }

        if (facts.Attack == BuildingFireAttack.Indirect && facts.Roofless is null)
        {
            return Unknown("Roofless status is required for indirect fire at a Factory.", "B23.741", 139);
        }

        var result = ScenarioA1FactoryCalculator.Tem(new(facts.Building == BuildingFireKind.StoneFactory, facts.Firer,
            facts.TracedWithinDepiction, facts.OverAdjacentRubbleEnteringAcrossDepiction, facts.Attack == BuildingFireAttack.Indirect,
            facts.Roofless == true, facts.Attack == BuildingFireAttack.Residual, entry));
        return new(result.Tem is null ? BuildingFireDisposition.NotEvaluated : BuildingFireDisposition.Evaluated,
            result.Tem, result.Reason, "B23.741", 139);
    }

    private static bool Invalid<T>(T? value) where T : struct, Enum => value is { } known && !Enum.IsDefined(known);

    private static BuildingFireVerdict Unknown(string reason, string rule = "B23.31", int page = 136) =>
        new(BuildingFireDisposition.NotEvaluated, null, reason, rule, page);
}
