namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A19.35 (p. 86), A20.22 (p. 87), A25.63 and A25.66 (p. 97), RN1.5-.7.</summary>
public static class ScenarioA1ItalianCaptureCalculator
{
    public static ItalianCaptureVerdict CaptureModifier(ItalianCaptureFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Inexperienced == true)
        {
            return new(-1, "The Inexperienced defender's capture term is -1 (A19.35), replacing the ordinary +1.");
        }

        if (facts.Inexperienced is null || (facts.Nationality is null && facts.Elite != true))
        {
            return new(null, "A material defender fact is unavailable (A19.35, A25.63).");
        }

        if (!ScenarioA1ItalianNationalityRules.UsesItalianDeploymentAndCapture(facts.Nationality) || facts.Elite == true)
        {
            return new(1, "The ordinary capture-attempt term is +1 (A20.22).");
        }

        return facts.Elite == false
            ? new(0, "The ordinary +1 does not apply against this non-elite Italian-treatment defender (A25.63, A25.66).")
            : new(null, "A material defender fact is unavailable (A25.63).");
    }
}
