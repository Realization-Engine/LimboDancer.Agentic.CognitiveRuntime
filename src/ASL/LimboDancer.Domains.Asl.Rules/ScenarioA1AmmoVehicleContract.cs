namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contracts of E10 (p. 244; the E10 claim's design, ruling RE10.9): does this shot fire on its Ammo Vehicle's B12 (E10.2, E10.21), and may
/// this Replenishment start (E10.3, E10.31), as <see cref="ScenarioA1AmmoVehicleCalculator"/> decides them. Every fact is disclosed to All: an Ammo Supply
/// counter is on its vehicle, a Low Ammo vehicle is marked with a Low Ammo counter (D3.71), Depletion follows an announced use and a public To Hit DR
/// (C8.9), and a stack that is not concealed may be inspected (A12.16). A concealed vehicle's facts are gathered as unknown for its opponent, and the
/// verdict is then not evaluated. <c>e10.1-eligibility</c> stays in ScenarioA1RuleContracts.cs, answering as before.
/// </summary>
internal static class ScenarioA1AmmoVehicleContract
{
    private static readonly string[] Barrels = [.. Enum.GetNames<TurnAmmoBarrel>()];

    private static IEnumerable<ContractFact> SupplyFacts() =>
    [
        new("supplyId", ContractFactType.Unit, false, ContractDisclosure.All, "The Ammo Vehicle, or the vehicle the Ammo Dump is named by"),
        new("supplyOwner", ContractFactType.Text, false, ContractDisclosure.All, "Its side"),
        ScenarioA1RuleContracts.Flag("supplyActive", "It is active on the map"),
        ScenarioA1RuleContracts.Flag("supplyAuthorized", "Its ammunition is authorized for this vehicle by SSR or DYO (E10.1)", mayBeUnknown: true),
        ScenarioA1RuleContracts.Flag("supplyAvailable", "It still carries ammunition (its Ammo Supply counter)", mayBeUnknown: true),
        new("supplyCaliberMm", ContractFactType.Number, true, ContractDisclosure.All, "The caliber of its ammunition, in mm"),
        new("supplyBarrel", ContractFactType.Choice, false, ContractDisclosure.All, "The barrel its ammunition fits") { Choices = Barrels },
    ];

    [RuleContract]
    private static RuleContract BenefitContract() => new("e10.2-benefit", "Ammo Vehicle benefit", [new("E10.2", 244), new("E10.21", 244), new("D3.71", 201)],
        [
            .. SupplyFacts(),
            new("firerId", ContractFactType.Unit, false, ContractDisclosure.All, "The armed vehicle that fires"),
            new("firerOwner", ContractFactType.Text, false, ContractDisclosure.All, "Its side"),
            ScenarioA1RuleContracts.Flag("firerActive", "It is active on the map"),
            ScenarioA1RuleContracts.Flag("firerHasBNumber", "The weapon it fires has a B#"),
            ScenarioA1RuleContracts.Flag("firerCrewExposed", "Its crew is exposed (CE)", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("firerSameOrAccessible", "It is in the supply's same or an Accessible Location", mayBeUnknown: true),
            new("firerCaliberMm", ContractFactType.Number, true, ContractDisclosure.All, "The weapon's caliber, in mm"),
            new("firerBarrel", ContractFactType.Choice, false, ContractDisclosure.All, "The weapon's barrel") { Choices = Barrels },
            new("servedOthers", ContractFactType.Number, false, ContractDisclosure.All, "Other vehicles the supply serves now (this Player Turn, still in reach)"),
            ScenarioA1RuleContracts.Flag("boundingFirstFire", "The shot is Bounding (First) Fire", mayBeUnknown: true),
            new("bNumber", ContractFactType.Number, true, ContractDisclosure.All, "The weapon's B# now"),
            new("xNumber", ContractFactType.Number, true, ContractDisclosure.All, "The weapon's X#, after Low Ammo (D3.71)"),
        ],
        ["b12", "printed-b", ContractVerdict.NotEvaluated],
        [new("ordnance-fired", "the shot, its breakdown numbers B12 and no X# with the benefit"), new("conditions-changed", "the Ammo Vehicle's Prep Fire counter in the PFPh (E10.21)")],
        new("PFPh", "2.22A"))
    {
        Decide = BenefitVerdict,
    };

    [RuleContract]
    private static RuleContract ReplenishmentContract() => new("e10.3-replenishment", "Ammo Replenishment", [new("E10.3", 244), new("E10.31", 244)],
        [
            .. SupplyFacts(),
            new("recipientId", ContractFactType.Unit, false, ContractDisclosure.All, "The vehicle to be Replenished"),
            new("recipientOwner", ContractFactType.Text, false, ContractDisclosure.All, "Its side"),
            ScenarioA1RuleContracts.Flag("recipientActive", "It is active on the map"),
            ScenarioA1RuleContracts.Flag("recipientHasBNumber", "Its weapon has a B#"),
            ScenarioA1RuleContracts.Flag("recipientLowAmmo", "It is under Low Ammo (its Low Ammo counter)", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("recipientCrewExposed", "Its crew is exposed (CE)", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("recipientSameOrAccessible", "It is in the supply's same or an Accessible Location", mayBeUnknown: true),
            new("recipientCaliberMm", ContractFactType.Number, true, ContractDisclosure.All, "Its weapon's caliber, in mm"),
            new("recipientBarrel", ContractFactType.Choice, false, ContractDisclosure.All, "Its weapon's barrel") { Choices = Barrels },
            ScenarioA1RuleContracts.Flag("isDump", "The supply is an Ammo Dump (E10.6)"),
            new("otherRecipients", ContractFactType.Number, false, ContractDisclosure.All, "Other vehicles the supply already Replenishes, identically armed"),
            ScenarioA1RuleContracts.Flag("recipientTi", "It is TI for the Replenishment", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("supplyTi", "The Ammo Vehicle is TI for the Replenishment", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("supplyHasPassengers", "Passengers are aboard the Ammo Vehicle", mayBeUnknown: true),
        ],
        ["may-start", "refused", ContractVerdict.NotEvaluated],
        [new("turn-work-recorded", "the Replenishment's declaration, both vehicles TI")],
        new("RPh", "8.43B"))
    {
        Decide = ReplenishmentVerdict,
    };

    private static TurnAmmoSupplyFacts Supply(ContractFacts facts) =>
        new(facts.Text("supplyId"), facts.Text("supplyOwner"), facts.Flag("supplyActive"), facts.Flag("supplyAuthorized"), facts.Flag("supplyAvailable"),
            new TurnAmmoArmament(Caliber(facts.Number("supplyCaliberMm")), Enum.Parse<TurnAmmoBarrel>(facts.Text("supplyBarrel")!)));

    // A caliber that is not above zero is no caliber: it leaves the question open rather than throw (the calculator refuses it).
    private static decimal? Caliber(decimal? given) => given is > 0 ? given : null;

    private static int? Whole(decimal? given) => given is { } value && value == decimal.Truncate(value) && value is >= 0 and <= int.MaxValue ? (int)value : null;

    private static ContractVerdict BenefitVerdict(ContractFacts facts)
    {
        if (Whole(facts.Number("servedOthers")) is not { } others)
        {
            return new(ContractVerdict.NotEvaluated, [new("the fact 'servedOthers' is not a whole number", "E10.2", 244)], []);
        }

        var firer = new TurnAmmoRecipientFacts(facts.Text("firerId"), facts.Text("firerOwner"), facts.Flag("firerActive"), true, facts.Flag("firerHasBNumber"),
            null, facts.Flag("firerCrewExposed"), facts.Flag("firerSameOrAccessible"),
            new TurnAmmoArmament(Caliber(facts.Number("firerCaliberMm")), Enum.Parse<TurnAmmoBarrel>(facts.Text("firerBarrel")!)));
        // Stand-ins named from the firer, so none is mistaken for it (the build's review, finding 4); two are enough to refuse.
        var served = Enumerable.Range(0, Math.Min(others, 2)).Select(index => $"{firer.Id}:served-{index}").ToList();
        var verdict = ScenarioA1AmmoVehicleCalculator.Benefit(new(Supply(facts), firer, served, facts.Flag("boundingFirstFire")));
        var numbers = ScenarioA1AmmoVehicleCalculator.BreakdownNumber(Whole(facts.Number("bNumber")), Whole(facts.Number("xNumber")), verdict.State == TurnAmmoEligibility.Eligible);
        return verdict.State switch
        {
            TurnAmmoEligibility.Eligible => new("b12", [new("Its B# is treated as a normal B12, with no X# (E10.2; RE10.11)", "E10.2", 244)], []),
            TurnAmmoEligibility.Ineligible => new("printed-b", [new($"{verdict.Reason} It fires on B{numbers.BNumber?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "#"}.", "E10.2", 244)], []),
            _ => new(ContractVerdict.NotEvaluated, [new(verdict.Reason, "E10.2", 244)], []),
        };
    }

    private static ContractVerdict ReplenishmentVerdict(ContractFacts facts)
    {
        var supply = Supply(facts);
        var recipient = new TurnAmmoRecipientFacts(facts.Text("recipientId"), facts.Text("recipientOwner"), facts.Flag("recipientActive"), true,
            facts.Flag("recipientHasBNumber"), facts.Flag("recipientLowAmmo"), facts.Flag("recipientCrewExposed"), facts.Flag("recipientSameOrAccessible"),
            new TurnAmmoArmament(Caliber(facts.Number("recipientCaliberMm")), Enum.Parse<TurnAmmoBarrel>(facts.Text("recipientBarrel")!)));

        // The other recipients are identically armed and already qualified: they stand for the two-vehicle limit (the build's review, finding 3).
        if (Whole(facts.Number("otherRecipients")) is not { } others)
        {
            return new(ContractVerdict.NotEvaluated, [new("the fact 'otherRecipients' is not a whole number", "E10.3", 244)], []);
        }

        List<TurnAmmoRecipientFacts> recipients = [recipient, .. Enumerable.Range(0, Math.Min(others, 2)).Select(index => recipient with
        {
            Id = $"{recipient.Id}:other-{index}", LowAmmo = true, CrewExposed = true, SameOrAccessibleLocation = true, Active = true, HasLowAmmoBNumber = true
        })];
        var verdict = ScenarioA1AmmoVehicleCalculator.Replenishment(new(supply, recipients, true, facts.Flag("recipientTi"), facts.Flag("supplyTi"),
            facts.Flag("supplyHasPassengers"), facts.Flag("isDump")!.Value));
        return verdict.State switch
        {
            TurnAmmoEligibility.Eligible => new("may-start", [new("The Replenishment may start", "E10.3", 244)], []),
            TurnAmmoEligibility.Ineligible => new("refused", [new(verdict.Reason, "E10.3", 244)], []),
            _ => new(ContractVerdict.NotEvaluated, [new(verdict.Reason, "E10.3", 244)], []),
        };
    }
}
