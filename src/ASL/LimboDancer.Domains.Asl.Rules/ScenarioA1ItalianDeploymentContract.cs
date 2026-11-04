namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Italian deployment nationality restriction: a facts-only adapter, RN1, with no caller or serialized-game changes.</summary>
internal static class ScenarioA1ItalianDeploymentContract
{
    [RuleContract]
    private static RuleContract Contract() => new("a25.61-italian-deployment", "Italian deployment nationality restriction",
        [new("A25.61", 97), new("A25.66", 97), new("A20.5", 88), new("A21.22", 88)],
        [
            new("subjectId", ContractFactType.Unit, false, ContractDisclosure.Owner, "The subject, selected by its owning side") { Concerns = "subjectId" },
            new("nationality", ContractFactType.Text, true, ContractDisclosure.Owner, "Actual nationality, including eritrean; never a derived Italian flag") { Concerns = "subjectId" },
            new("kind", ContractFactType.Choice, true, ContractDisclosure.Owner, "kind") { Concerns = "subjectId", Choices = ["squad", "half-squad", "crew", "leader", "other"] },
            new("elite", ContractFactType.Flag, true, ContractDisclosure.Owner, "elite") { Concerns = "subjectId" },
            new("context", ContractFactType.Choice, false, ContractDisclosure.All, "context") { Choices = ["setup", "ordinary", "guard", "unarmed", "temporary-crew"] },
            new("guard", ContractFactType.Flag, true, ContractDisclosure.Owner, "guard") { Concerns = "subjectId" },
            new("unarmed", ContractFactType.Flag, true, ContractDisclosure.Owner, "unarmed") { Concerns = "subjectId" },
            new("broken", ContractFactType.Flag, true, ContractDisclosure.Owner, "broken") { Concerns = "subjectId" },
        ],
        ["unrestricted", "restricted", "not-applicable", "not-evaluated"],
        [new("unit-deployed", "If the remaining rules allow: deployment roll and resulting unit lineage; no new event shape")], new("Any", "none"))
    {
        Decide = Evaluate,
    };

    private static ContractVerdict Evaluate(ContractFacts facts)
    {
        var verdict = ScenarioA1ItalianDeploymentCalculator.Evaluate(new ItalianDeploymentFacts(facts.Text("nationality"), facts.Text("kind"),
            facts.Flag("elite"), facts.Text("context")!, facts.Flag("guard"), facts.Flag("unarmed"), facts.Flag("broken")));
        return new(verdict.Outcome, [new(verdict.Reason, "A25.61", 97)], []);
    }
}
