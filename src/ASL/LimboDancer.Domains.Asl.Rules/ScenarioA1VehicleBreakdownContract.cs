using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contract of D3.7 and D3.71 (pp. 201 and 202; the D3.7 claim's design, ruling RD3.7): what does this Original DR do to this vehicle weapon,
/// as <see cref="ScenarioA1VehicleBreakdownCalculator"/> decides it. Every fact is disclosed to All: the Malfunction, Disabled, and Low Ammo counters are
/// on the vehicle, the DR is public, an unconcealed stack may be inspected (A12.16), and a vehicle that fires reveals itself; a concealed vehicle's facts
/// are gathered as unknown for its opponent. A printed number left unknown is none printed.
/// </summary>
internal static class ScenarioA1VehicleBreakdownContract
{
    private static readonly string[] Weapons = ["MA", "BMG", "CMG", "AAMG", "SA"];

    private static readonly string[] DrKinds = [nameof(VehicleBreakdownDr.ToHit), nameof(VehicleBreakdownDr.Ift), nameof(VehicleBreakdownDr.CloseCombat)];

    [RuleContract]
    private static RuleContract Breakdown() => new("d3.7-vehicle-breakdown", "Vehicle weapon breakdown",
        [new("D3.7", 201), new("D3.71", 201), new("A.11", 42), new("A19.131", 86)],
        [
            new("vehicleId", ContractFactType.Unit, false, ContractDisclosure.All, "The vehicle that fires"),
            new("weapon", ContractFactType.Choice, false, ContractDisclosure.All, "The weapon that fires") { Choices = Weapons },
            new("printedB", ContractFactType.Number, true, ContractDisclosure.All, "Its printed B#, if one is printed"),
            new("printedX", ContractFactType.Number, true, ContractDisclosure.All, "Its printed X#, if one is printed"),
            ScenarioA1RuleContracts.Flag("circled", "The printed B# is circled (D3.71)"),
            ScenarioA1RuleContracts.Flag("noteExtends", "A Vehicle Note extends the printed numbers beyond the MA"),
            ScenarioA1RuleContracts.Flag("lowAmmo", "The vehicle is under Low Ammo (its counter)"),
            ScenarioA1RuleContracts.Flag("ammunitionShortage", "The scenario gives this side an Ammunition Shortage (A19.131)"),
            new("lowersB", ContractFactType.Number, false, ContractDisclosure.All, "How much this use lowers the B# only (C5.62)"),
            new("lowersBX", ContractFactType.Number, false, ContractDisclosure.All, "How much this use lowers the B# and X# (A21.11, E3.741, A19.32)"),
            new("drKind", ContractFactType.Choice, false, ContractDisclosure.All, "The DR: To Hit, IFT, or CC") { Choices = DrKinds },
            new("originalDr", ContractFactType.Number, false, ContractDisclosure.All, "The Original DR"),
        ],
        ["none", "malfunction", "low-ammo", "disabled", ContractVerdict.NotEvaluated],
        [new("conditions-changed", "the Malfunction, Disabled, or Low Ammo counter")],
        new("PFPh", "2.22A"))
    {
        Decide = Verdict,
    };

    private static int? Whole(decimal? given) => given is { } value && value == decimal.Truncate(value) && value is >= 0 and <= 99 ? (int)value : null;

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        var printedB = facts.Number("printedB");
        var printedX = facts.Number("printedX");
        if (Whole(facts.Number("lowersB")) is not { } lowersB || Whole(facts.Number("lowersBX")) is not { } lowersBX
            || Whole(facts.Number("originalDr")) is not (>= 2 and <= 12) || printedB is not null && Whole(printedB) is null
            || printedX is not null && Whole(printedX) is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("a number is not a whole number this contract reads, or the DR is not 2 to 12", "D3.7", 201)], []);
        }

        var weapon = facts.Text("weapon") switch
        {
            "BMG" => VehicleBreakdownWeapon.BowMg,
            "CMG" => VehicleBreakdownWeapon.CoaxialMg,
            "AAMG" => VehicleBreakdownWeapon.AntiAircraftMg,
            "SA" => VehicleBreakdownWeapon.SecondaryArmament,
            _ => VehicleBreakdownWeapon.MainArmament,
        };
        var numbers = ScenarioA1VehicleBreakdownCalculator.Numbers(new(weapon, Whole(printedB), Whole(printedX), facts.Flag("circled")!.Value,
            facts.Flag("noteExtends")!.Value, facts.Flag("lowAmmo")!.Value, facts.Flag("ammunitionShortage")!.Value, lowersB, lowersBX));
        var outcome = ScenarioA1VehicleBreakdownCalculator.Outcome(Whole(facts.Number("originalDr"))!.Value, numbers,
            Enum.Parse<VehicleBreakdownDr>(facts.Text("drKind")!));
        var x = numbers.XNumber is { } value ? string.Format(CultureInfo.InvariantCulture, ", X{0}", value) : string.Empty;
        var reason = string.Format(CultureInfo.InvariantCulture, "B{0}{1}{2}{3}", numbers.BNumber, numbers.Circled ? " circled" : string.Empty, x,
            numbers.LowAmmo ? ", under Low Ammo" : string.Empty);
        return outcome switch
        {
            VehicleBreakdownOutcome.Disabled => new("disabled", [new($"{reason}: the X# is reached and the weapon is Disabled", "A.11", 42)], []),
            VehicleBreakdownOutcome.LowAmmo => new("low-ammo", [new($"{reason}: Low Ammo, the B# becomes the X# and a new B# one lower", "D3.71", 201)], []),
            VehicleBreakdownOutcome.Malfunction => new("malfunction", [new($"{reason}: the weapon malfunctions", "D3.7", 201)], []),
            _ => new("none", [new($"{reason}: no breakdown", "D3.7", 201)], []),
        };
    }
}
