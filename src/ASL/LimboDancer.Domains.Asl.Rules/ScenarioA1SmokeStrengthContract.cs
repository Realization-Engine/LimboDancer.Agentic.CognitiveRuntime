namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contract of A24.5 (p. 92; the A24.5 claim's design, ruling RA24.8): which side up does this SMOKE placement take, and what is its
/// Hindrance, as <see cref="ScenarioA1SmokeStrengthCalculator"/> decides them. Every fact is disclosed to All: the counter is on the map, WP is announced,
/// and the PFPh's order of attacks is public. A grenade's counter has one side (A24.11), given as full; drifting SMOKE is Dispersed (A24.61).
/// </summary>
internal static class ScenarioA1SmokeStrengthContract
{
    private static readonly string[] Sources = [.. Enum.GetNames<TurnSmokeSource>().Where(name => name != nameof(TurnSmokeSource.Unknown))];

    private static readonly string[] Chemicals = [nameof(TurnSmokeChemical.Conventional), nameof(TurnSmokeChemical.WhitePhosphorus)];

    [RuleContract]
    private static RuleContract Placement() => new("a24.5-smoke-placement", "SMOKE strength",
        [new("A24.5", 92), new("A24.11", 91), new("A24.61", 92), new("D13.31", 212)],
        [
            new("source", ContractFactType.Choice, false, ContractDisclosure.All, "What places the SMOKE") { Choices = Sources },
            new("chemical", ContractFactType.Choice, false, ContractDisclosure.All, "Conventional smoke or WP") { Choices = Chemicals },
            ScenarioA1RuleContracts.Flag("ownerPrepFire", "It is placed in its owner's PFPh"),
            ScenarioA1RuleContracts.Flag("beforeNonSmokeFire", "No non-SMOKE attack has been resolved yet this PFPh", mayBeUnknown: true),
        ],
        ["full", "dispersed", ContractVerdict.NotEvaluated],
        [new("instance-created", "the SMOKE counter, its side up")],
        new("PFPh", "2.12A"))
    {
        Decide = Verdict,
    };

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        var source = Enum.Parse<TurnSmokeSource>(facts.Text("source")!);
        var chemical = Enum.Parse<TurnSmokeChemical>(facts.Text("chemical")!);
        if (source is TurnSmokeSource.InfantryGrenade or TurnSmokeSource.VehicleGrenade or TurnSmokeSource.Drift)
        {
            var drift = source == TurnSmokeSource.Drift;
            var value = ScenarioA1SmokeStrengthCalculator.Hindrance(source, chemical, drift ? TurnSmokeStrength.Dispersed : TurnSmokeStrength.Unknown)!.Value;
            return new(drift ? "dispersed" : "full",
                [new(drift ? $"Drifting SMOKE is Dispersed: +{value}" : $"A grenade's SMOKE is never Dispersed: +{value}", drift ? "A24.61" : "A24.11", drift ? 92 : 91)], []);
        }

        var placement = ScenarioA1SmokeStrengthCalculator.PlacementStrength(source, chemical, facts.Flag("ownerPrepFire")!.Value, facts.Flag("beforeNonSmokeFire"));
        if (placement.Strength == TurnSmokeStrength.Unknown)
        {
            return new(ContractVerdict.NotEvaluated, [new(placement.Reason, "A24.5", 92)], []);
        }

        var dispenser = ScenarioA1SmokeStrengthCalculator.Dispenser(source);
        return placement.Strength == TurnSmokeStrength.Full
            ? new("full", [new($"Placed by ordnance or OBA as the first attack of its PFPh: full strength, +{placement.Hindrance}", "A24.5", 92)], [])
            : new("dispersed", [dispenser
                ? new($"A dispenser places white Dispersed SMOKE: +{placement.Hindrance}", "D13.31", 212)
                : new($"Not the first attack of its owner's PFPh: the Dispersed side, +{placement.Hindrance}", "A24.5", 92)], []);
    }
}
