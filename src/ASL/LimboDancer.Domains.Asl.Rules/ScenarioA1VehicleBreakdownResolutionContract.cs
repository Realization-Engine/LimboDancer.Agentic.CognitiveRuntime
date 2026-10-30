using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Supplemental owner-fact contract for a new D3.7 weapon use. Uninspected numbers stay unknown (A12.16, PDF p. 79).
/// No new Play fact source is implied; entered examples do not authorize a command.
/// </summary>
internal static class ScenarioA1VehicleBreakdownResolutionContract
{
    private static ContractFact Flag(string name, string description) =>
        new(name, ContractFactType.Flag, false, ContractDisclosure.Owner, description)
        {
            Concerns = "vehicleId"
        };

    private static ContractFact Number(string name, string description, bool optional = false) =>
        new(name, ContractFactType.Number, optional, ContractDisclosure.Owner, description)
        {
            Concerns = "vehicleId"
        };

    [RuleContract]
    private static RuleContract BreakdownUse() => new("d3.7-vehicle-breakdown-use", "Vehicle weapon breakdown for this use",
        [new("D3.7", 201), new("D3.71", 202), new("A.11", 43), new("A19.131", 86), new("E3.741", 231)],
        [
            new("vehicleId", ContractFactType.Unit, false, ContractDisclosure.Owner, "The vehicle that fires") { Concerns = "vehicleId" },
            Flag("numbersKnown", "Its printed numbers have been inspected; omitted numbers mean none printed only when this is true"),
            new("weapon", ContractFactType.Choice, false, ContractDisclosure.Owner, "The actual weapon")
            {
                Concerns = "vehicleId", Choices = Enum.GetNames<VehicleBreakdownWeapon>()
            },
            Number("printedB", "Printed B#, or none printed", true),
            Number("printedX", "Printed X#, or none printed", true),
            Flag("circled", "The printed B# is circled"),
            Flag("noteExtends", "A Vehicle Note extends the printed numbers to this weapon"),
            Flag("lowAmmo", "Low Ammo before this use"),
            Flag("ammunitionShortage", "This vehicle is under scenario Ammunition Shortage"),
            Flag("captured", "This vehicle is captured"),
            Flag("nonQualified", "Its actual user incurs the non-qualified-use penalty"),
            Number("winterReduction", "The E3.741 weather reduction, 0, 1 or 2"),
            Flag("intensiveFire", "This use is Intensive Fire"),
            new("use", ContractFactType.Choice, false, ContractDisclosure.Owner, "The actual weapon use, not a target follow-up")
            {
                Concerns = "vehicleId", Choices = Enum.GetNames<VehicleBreakdownUse>()
            },
            Number("originalDr", "The Original weapon DR"),
        ], ["none", "malfunction", "low-ammo", "disabled", ContractVerdict.NotEvaluated],
        [new("conditions-changed", "the actual weapon's returned condition")], new("PFPh", "2.22A"))
    {
        Decide = Verdict,
    };

    private static bool Whole(decimal? value, int low, int high) => value is { } number && number == decimal.Truncate(number)
        && number >= low && number <= high;

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        var b = facts.Number("printedB");
        var x = facts.Number("printedX");
        if (facts.Flag("numbersKnown") != true || !Whole(facts.Number("winterReduction"), 0, 2)
            || !Whole(facts.Number("originalDr"), 2, 12) || b is not null && !Whole(b, 2, 12) || x is not null && !Whole(x, 2, 12))
        {
            return new(ContractVerdict.NotEvaluated, [new("Inspected numbers and a whole valid weapon DR/weather reduction are required", "D3.7", 201)], []);
        }

        var result = ScenarioA1VehicleBreakdownResolutionCalculator.Resolve(new(
            Enum.Parse<VehicleBreakdownWeapon>(facts.Text("weapon")!), b is null ? null : (int)b.Value, x is null ? null : (int)x.Value,
            facts.Flag("circled")!.Value, facts.Flag("noteExtends")!.Value, facts.Flag("lowAmmo")!.Value,
            facts.Flag("ammunitionShortage")!.Value, facts.Flag("captured")!.Value, facts.Flag("nonQualified")!.Value,
            (int)facts.Number("winterReduction")!.Value, facts.Flag("intensiveFire")!.Value),
            Enum.Parse<VehicleBreakdownUse>(facts.Text("use")!), (int)facts.Number("originalDr")!.Value);
        var outcome = result.Outcome switch
        {
            VehicleBreakdownOutcome.LowAmmo => "low-ammo",
            VehicleBreakdownOutcome.Malfunction => "malfunction",
            VehicleBreakdownOutcome.Disabled => "disabled",
            _ => "none",
        };
        var reason = string.Format(CultureInfo.InvariantCulture, "B{0}, X{1}: {2}", result.Numbers.BNumber,
            result.Numbers.XNumber?.ToString(CultureInfo.InvariantCulture) ?? "none", outcome);
        return new(outcome, [new(reason, "D3.7", 201)], []);
    }
}
