namespace LimboDancer.Domains.Asl.Rules;

/// <summary>C6.17 single-target allowance. Hand-entered history proves the Rules stage, not a gathered Play history.</summary>
internal static class ScenarioA1DefensiveFirstFireHistoryContract
{
    [RuleContract]
    private static RuleContract Allowance() => new("c6.17-personnel-history", "Defensive First Fire allowance", [new("C6.17", 173)],
        [
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            new("firerId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The firing unit") { Concerns = "firerId" },
            new("targetId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The moving target") { Concerns = "targetId" },
            new("weaponId", ContractFactType.Text, true, ContractDisclosure.Owner, "The exact weapon identity") { Concerns = "firerId" },
            new("location", ContractFactType.Hex, false, ContractDisclosure.All, "The target's complete Location"),
            new("halfMfSpent", ContractFactType.Number, true, ContractDisclosure.All, "This target's announced accumulated half-MF in this Location"),
            new("shotsHere", ContractFactType.Number, true, ContractDisclosure.All, "This weapon's resolved shots at this target here"),
            new("inCurrentWindow", ContractFactType.Flag, false, ContractDisclosure.All, "The target belongs to the current fire window"),
        ],
        ["allowed", "refused", ContractVerdict.NotEvaluated],
        [new("ordnance-fired", "a versioned shot with each target's allowance facts; preview only")], new("MPh", "3.36D"))
    {
        Decide = Verdict,
    };

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        if (facts.Text("phase") != "MPh")
        {
            return new("refused", [new("Defensive First Fire uses the MPh window.", "A8.1", 59)], []);
        }
        if (facts.Text("firerId") is null || facts.Text("weaponId") is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("The firing unit or weapon is not known to this viewer.", "C6.17", 173)], []);
        }
        var result = ScenarioA1DefensiveFirstFireHistoryCalculator.Allowance(
            Whole(facts.Number("halfMfSpent")), Whole(facts.Number("shotsHere")), facts.Flag("inCurrentWindow"));
        return new(!result.Evaluated ? ContractVerdict.NotEvaluated : result.Refusal is null ? "allowed" : "refused",
            [new(result.Refusal ?? $"{result.Remaining} of {result.Ceiling} shots remain for this weapon against this target here.", "C6.17", 173)], []);
    }

    private static int? Whole(decimal? value) => value is >= 0 and <= int.MaxValue && value == decimal.Truncate(value.Value) ? (int)value : null;
}
