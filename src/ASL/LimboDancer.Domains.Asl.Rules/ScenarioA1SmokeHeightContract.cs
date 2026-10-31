namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A24.4's public physical source classification. This contract supplies no placement permission or hidden unit information.</summary>
internal static class ScenarioA1SmokeHeightContract
{
    [RuleContract]
    private static RuleContract Height() => new("a24.4-smoke-height", "SMOKE height",
        [new("A24.4", 92)],
        [
            new("source", ContractFactType.Choice, false, ContractDisclosure.All, "The recorded SMOKE source")
            {
                Choices = Enum.GetNames<TurnSmokeSource>()
            },
            new("chemical", ContractFactType.Choice, false, ContractDisclosure.All, "The recorded chemical")
            {
                Choices = Enum.GetNames<TurnSmokeChemical>()
            },
        ],
        ["two-levels", "four-levels", ContractVerdict.NotEvaluated], [], new("Any", "none"))
    {
        Decide = facts => ScenarioA1SmokeHeightCalculator.Height(Enum.Parse<TurnSmokeSource>(facts.Text("source")!),
            Enum.Parse<TurnSmokeChemical>(facts.Text("chemical")!)) is { } height
            ? new(height == 2 ? "two-levels" : "four-levels", [new($"The source rises {height} levels; its top boundary is not within SMOKE.", "A24.4", 92)], [])
            : new(ContractVerdict.NotEvaluated, [new("This source needs its own height rule or known chemical.", "A24.4", 92)], []),
    };
}
