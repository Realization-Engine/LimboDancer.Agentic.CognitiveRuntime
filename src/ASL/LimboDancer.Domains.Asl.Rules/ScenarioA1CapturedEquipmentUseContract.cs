namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Finnish captured equipment use: a facts-only adapter, RN1, with no caller or serialized-game changes.</summary>
internal static class ScenarioA1CapturedEquipmentUseContract
{
    [RuleContract]
    private static RuleContract Contract() => new("a25.75-equipment-use", "Finnish captured equipment use",
        [new("A25.75", 97), new("A25.77", 97), new("E1.76", 225)],
        [
            new("subjectId", ContractFactType.Unit, false, ContractDisclosure.Owner, "The subject, selected by its owning side") { Concerns = "subjectId" },
            new("userNationality", ContractFactType.Text, true, ContractDisclosure.Owner, "userNationality") { Concerns = "subjectId" },
            new("personnel", ContractFactType.Flag, true, ContractDisclosure.Owner, "personnel") { Concerns = "subjectId" },
            new("printedClass", ContractFactType.Choice, true, ContractDisclosure.Owner, "printedClass") { Concerns = "subjectId", Choices = ["elite", "first-line", "second-line", "green", "conscript", "other"] },
            new("weaponNationality", ContractFactType.Text, true, ContractDisclosure.Owner, "weaponNationality") { Concerns = "subjectId" },
            new("weaponKind", ContractFactType.Choice, true, ContractDisclosure.Owner, "weaponKind") { Concerns = "subjectId", Choices = ["lmg", "mmg", "hmg", "hmg50", "psk", "other"] },
            new("originMark", ContractFactType.Choice, true, ContractDisclosure.Owner, "originMark") { Concerns = "subjectId", Choices = ["r", "none", "not-applicable"] },
            new("scenarioDate", ContractFactType.Text, false, ContractDisclosure.All, "YYYY-MM, or not-applicable only for a date-independent branch"),
            new("physicallyCaptured", ContractFactType.Flag, true, ContractDisclosure.Owner, "physicallyCaptured") { Concerns = "subjectId" },
        ],
        ["captured-use", "exempt", "refused-use", "not-applicable", "not-evaluated"],
        [new("weapon-use", "Captured Use and night MG treatment; no change to ownership, repair, non-qualified use or ammunition")], new("Any", "none"))
    {
        Decide = Evaluate,
    };

    private static ContractVerdict Evaluate(ContractFacts facts)
    {
        var verdict = ScenarioA1CapturedEquipmentUseCalculator.Evaluate(new CapturedEquipmentUseFacts(facts.Text("userNationality"), facts.Flag("personnel"),
            facts.Text("printedClass"), facts.Text("weaponNationality"), facts.Text("weaponKind"), facts.Text("originMark"), facts.Text("scenarioDate")!, facts.Flag("physicallyCaptured")));
        return new(verdict.Outcome, [new(verdict.Reason, verdict.UseRefusal is null ? "A25.75" : "A25.77", 97)], []);
    }
}
