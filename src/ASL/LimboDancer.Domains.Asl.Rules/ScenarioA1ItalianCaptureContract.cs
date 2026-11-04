namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Per-defender capture term: a facts-only adapter, RN1, with no caller or serialized-game changes.</summary>
internal static class ScenarioA1ItalianCaptureContract
{
    [RuleContract]
    private static RuleContract Contract() => new("a25.63-capture-modifier", "Per-defender capture term",
        [new("A25.63", 97), new("A25.66", 97), new("A19.35", 86), new("A20.22", 87), new("A11.19", 73)],
        [
            new("subjectId", ContractFactType.Unit, false, ContractDisclosure.All, "The defender selected for CC attack declaration; supply only facts public at that step") { Concerns = "subjectId" },
            new("nationality", ContractFactType.Text, true, ContractDisclosure.All, "Actual nationality, including eritrean; never a derived Italian flag") { Concerns = "subjectId" },
            new("elite", ContractFactType.Flag, true, ContractDisclosure.All, "elite") { Concerns = "subjectId" },
            new("inexperienced", ContractFactType.Flag, true, ContractDisclosure.All, "inexperienced") { Concerns = "subjectId" },
        ],
        ["minus-one", "zero", "plus-one", "not-evaluated"],
        [new("close-combat-resolved", "This defender ordinary capture term, independently combined with Passenger, leadership and other modifiers")], new("CCPh", "8.21B"))
    {
        Decide = Evaluate,
    };

    private static ContractVerdict Evaluate(ContractFacts facts)
    {
        var verdict = ScenarioA1ItalianCaptureCalculator.CaptureModifier(new ItalianCaptureFacts(facts.Text("nationality"), facts.Flag("elite"), facts.Flag("inexperienced")));
        var outcome = verdict.CaptureDrm switch
        {
            -1 => "minus-one",
            0 => "zero",
            1 => "plus-one",
            _ => ContractVerdict.NotEvaluated
        };
        var rule = verdict.CaptureDrm switch
        {
            -1 => "A19.35",
            1 => "A20.22",
            _ => "A25.63"
        };
        var page = verdict.CaptureDrm switch
        {
            -1 => 86,
            1 => 87,
            _ => 97
        };
        return new(outcome, [new(verdict.Reason, rule, page)], []);
    }
}
