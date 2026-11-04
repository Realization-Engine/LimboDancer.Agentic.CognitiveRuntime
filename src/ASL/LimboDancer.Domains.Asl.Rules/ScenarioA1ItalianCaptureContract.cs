namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Per-defender capture term: a facts-only adapter, RN1, with no caller or serialized-game changes.</summary>
internal static class ScenarioA1ItalianCaptureContract
{
    [RuleContract]
    private static RuleContract Contract() => new("a25.63-capture-modifier", "Per-defender capture term",
        [new("A25.63", 97), new("A25.66", 97), new("A19.35", 86), new("A20.22", 87)],
        [
            new("subjectId", ContractFactType.Unit, false, ContractDisclosure.Owner, "The subject, selected by its owning side") { Concerns = "subjectId" },
            new("nationality", ContractFactType.Text, true, ContractDisclosure.Owner, "Actual nationality, including eritrean; never a derived Italian flag") { Concerns = "subjectId" },
            new("elite", ContractFactType.Flag, true, ContractDisclosure.Owner, "elite") { Concerns = "subjectId" },
            new("inexperienced", ContractFactType.Flag, true, ContractDisclosure.Owner, "inexperienced") { Concerns = "subjectId" },
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

    private static readonly string[] Nationalities = ["german", "russian", "american", "british", "french", "finnish", "italian", "eritrean", "japanese", "chinese", "axis-minor",
        "romanian", "hungarian", "slovakian", "croatian", "bulgarian", "other"];

    [RuleContract]
    private static RuleContract Escape() => new("a25.63-prisoner-escape", "Prisoner escape",
        [new("A20.55", 88), new("A25.63", 97), new("A25.66", 97), new("A25.82", 98), new("G1.621", 280)],
        [
            new("prisonerId", ContractFactType.Unit, false, ContractDisclosure.All, "The prisoner that would attack its Guard"),
            new("nationality", ContractFactType.Choice, false, ContractDisclosure.All, "The prisoner's nationality") { Choices = Nationalities },
            new("captorNationality", ContractFactType.Choice, false, ContractDisclosure.All, "Its Guard's nationality") { Choices = Nationalities },
            ScenarioA1RuleContracts.Flag("infantry", "The prisoner is Infantry"),
            ScenarioA1RuleContracts.Flag("axisMinorAtHome", "The card puts an Axis Minor prisoner within its own national borders (unknown: it is not)", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("guardBroken", "Its Guard is broken"),
            ScenarioA1RuleContracts.Flag("meleeInLocation", "A Melee exists in the Location"),
        ],
        ["escape-after-ntc", "escape-no-ntc", "refused-nationality", "refused-not-infantry", "refused-guard", ContractVerdict.NotEvaluated],
        [new("close-combat-resolved", "the prisoners' round: each escape NTC, the escape attacks, and each prisoner no longer guarded")],
        new("CCPh", "8.14B"))
    {
        Decide = EscapeVerdict,
    };

    private static ContractVerdict EscapeVerdict(ContractFacts facts)
    {
        var verdict = ScenarioA1PrisonerCalculator.Escape(new PrisonerEscapeFacts(facts.Text("prisonerId")!, facts.Text("nationality"), facts.Text("captorNationality"),
            facts.Flag("infantry")!.Value, facts.Flag("axisMinorAtHome"), facts.Flag("guardBroken")!.Value, facts.Flag("meleeInLocation")!.Value), ItalianEscapeContext.WithEritreanInheritance);
        if (verdict.Refusal is { } refusal)
        {
            var rule = verdict.RefusalRule!;
            var outcome = verdict.Bar switch
            {
                PrisonerEscapeBar.NotInfantry => "refused-not-infantry",
                PrisonerEscapeBar.Guard => "refused-guard",
                _ => "refused-nationality",
            };
            return new ContractVerdict(outcome, [new ContractReason(refusal, rule, Page(rule))], []);
        }

        return verdict.NtcRequired
            ? new ContractVerdict("escape-after-ntc", [new ContractReason("With no Melee in the Location, it must pass a NTC before it attacks its broken Guard", "A20.55", 88)],
                [new ContractRoll("ntc", null, "Escape NTC: the dice at most the prisoner's Morale")])
            : new ContractVerdict("escape-no-ntc", [new ContractReason("A Melee exists in the Location: it attacks its Guard with no NTC, even an unbroken one", "A20.55", 88)], []);
    }

    private static int Page(string rule) => rule switch
    {
        "A25.63" or "A25.66" => 97,
        "A25.82" => 98,
        "G1.621" => 280,
        _ => 88,
    };
}
