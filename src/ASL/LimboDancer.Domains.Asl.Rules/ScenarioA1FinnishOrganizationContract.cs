namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Finnish organization requirements: a facts-only adapter, RN1, with no caller or serialized-game changes.</summary>
internal static class ScenarioA1FinnishOrganizationContract
{
    [RuleContract]
    private static RuleContract Contract() => new("a25.71-organization", "Finnish organization requirements",
        [new("A25.71", 97), new("A1.31", 45), new("A1.32", 45), new("A20.5", 88)],
        [
            new("subjectId", ContractFactType.Unit, false, ContractDisclosure.Owner, "The subject, selected by its owning side") { Concerns = "subjectId" },
            new("nationality", ContractFactType.Text, true, ContractDisclosure.Owner, "Actual nationality, including eritrean; never a derived Italian flag") { Concerns = "subjectId" },
            new("kind", ContractFactType.Choice, true, ContractDisclosure.Owner, "kind") { Concerns = "subjectId", Choices = ["squad", "half-squad", "crew", "leader", "other"] },
            new("action", ContractFactType.Choice, false, ContractDisclosure.All, "action") { Choices = ["deploy", "recombine"] },
            new("mode", ContractFactType.Choice, false, ContractDisclosure.All, "mode") { Choices = ["directed", "leaderless", "not-applicable"] },
            new("guard", ContractFactType.Flag, true, ContractDisclosure.Owner, "guard") { Concerns = "subjectId" },
            new("unarmed", ContractFactType.Flag, true, ContractDisclosure.Owner, "unarmed") { Concerns = "subjectId" },
            new("broken", ContractFactType.Flag, true, ContractDisclosure.Owner, "broken") { Concerns = "subjectId" },
        ],
        ["directed-ntc", "leaderless-ntc", "leaderless-1tc", "automatic", "no-leader-required", "not-applicable", "not-evaluated"],
        [new("unit-organization", "If other rules allow: deployment roll and deployment or recombination lineage")], new("RPh", "1.13B / 1.21A"))
    {
        Decide = Evaluate,
    };

    private static ContractVerdict Evaluate(ContractFacts facts)
    {
        var verdict = ScenarioA1FinnishOrganizationCalculator.Evaluate(new FinnishOrganizationFacts(facts.Text("nationality"), facts.Text("kind"),
            facts.Text("action")!, facts.Text("mode")!, facts.Flag("guard"), facts.Flag("unarmed"), facts.Flag("broken")));
        IReadOnlyList<ContractRoll> rolls = verdict.BaseTcDrm is { } drm
            ? [new("tc", null, drm == 1 ? "1TC: add +1 to the deployment DR" : "NTC: add the directing leader's DRM only if directed")]
            : [];
        return new(verdict.Outcome, [new(verdict.Reason, "A25.71", 97)], rolls);
    }
}
