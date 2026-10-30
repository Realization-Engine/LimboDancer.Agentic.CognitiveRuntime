using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contracts of B23.74 to B23.743 (pp. 139 and 140; the B23.74 claim, ruling RB23F.11): what TEM and Factory Hindrance an attack on a
/// ground-level Factory Location meets, and whether a unit may move here in a Factory and at what cost, as <see cref="ScenarioA1FactoryCalculator"/>
/// decides them. The Factory, its stairwells, road hexsides, rubble, and hex control are public; the firer's and the target's Locations, and so the LOS
/// and the way a moving unit entered, are their owners' while hidden or concealed, as are a mover's type and MP. Each unit id is its owner's, so the other
/// side names only its "?" stack; neither verdict reads an id beyond whether the firer is named. A rooftop Location (B23.8, by SSR) is out of the movement
/// contract's scope: it reads every level above ground as an upper level floor.
/// </summary>
internal static class ScenarioA1FactoryContract
{
    private static readonly string[] Materials = ["stone", "wooden"];

    private static readonly string[] FirerPlaces = ["inside", "rubble", "rooftop", "outside"];

    private static readonly string[] ResidualEntries = ["building-hexside", "non-building-hexside", "bypass"];

    private static readonly string[] MovementTypes = ["fully-tracked", "half-tracked", "truck"];

    private static readonly string[] Froms = ["factory", "factory-rubble", "outside"];

    private static readonly string[] Tos = ["factory", "outside"];

    private static readonly string[] MoverFacts = ["vehicle", "bypass"];

    [RuleContract]
    private static RuleContract FactoryTem() => new("b23.741-factory-tem", "Factory TEM and Hindrance",
        [new("B23.741", 139), new("B23.74", 139), new("B23.743", 140)],
        [
            new("material", ContractFactType.Choice, false, ContractDisclosure.All, "The Factory's material") { Choices = Materials },
            ScenarioA1RuleContracts.Flag("roofless", "The target Location is Roofless"),
            ScenarioA1RuleContracts.Flag("indirectFire", "The attack is Indirect Fire"),
            ScenarioA1RuleContracts.Flag("residual", "The attack is Residual FP"),
            new("targetId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The unit attacked in the Factory Location") { Concerns = "targetId" },
            new("residualEntry", ContractFactType.Choice, true, ContractDisclosure.Owner, "For Residual FP, how the moving unit entered the Location")
            {
                Choices = ResidualEntries,
                Concerns = "targetId",
            },
            new("firerId", ContractFactType.Unit, true, ContractDisclosure.Owner,"The firer, for an attack other than Residual FP") { Concerns = "firerId" },
            new("firerPlace", ContractFactType.Choice, true, ContractDisclosure.Owner, "Where the firer is: a ground Location of the same Factory, its rubble, its rooftop, or outside it")
            {
                Choices = FirerPlaces,
                Concerns = "firerId",
            },
            Firers("tracedWithinDepiction", "The LOS is traced entirely within the building depiction"),
            Firers("overAdjacentRubbleEnteringAcrossDepiction", "The LOS is over an adjacent rubble hex of the Factory and crosses the depiction while entering the target (B23.743)"),
            new("factoryHexesTraced", ContractFactType.Number, true, ContractDisclosure.Owner, "The Factory hexes the LOS is traced through within the depiction") { Concerns = "firerId" },
        ],
        ["factory-tem", "building-tem", ContractVerdict.NotEvaluated],
        [new("fire-resolved", "the attack, with the Factory's TEM and Hindrance among its DRM")],
        new("PFPh", "2.22A"))
    {
        Decide = TemVerdict,
    };

    [RuleContract]
    private static RuleContract FactoryMovement() => new("b23.742-factory-movement", "Factory movement", [new("B23.742", 139), new("B23.74", 139), new("B23.743", 140)],
        [
            new("moverId", ContractFactType.Unit, false, ContractDisclosure.Owner,"The moving unit") { Concerns = "moverId" },
            Movers("vehicle", "It is a vehicle"),
            Movers("afv", "It is an AFV"),
            new("movementType", ContractFactType.Choice, true, ContractDisclosure.Owner, "A vehicle's movement type") { Choices = MovementTypes, Concerns = "moverId" },
            new("mpAllotment", ContractFactType.Number, true, ContractDisclosure.Owner, "A vehicle's printed MP allotment") { Concerns = "moverId" },
            Movers("bypass", "It moves in Bypass"),
            new("material", ContractFactType.Choice, false, ContractDisclosure.All, "The Factory's material") { Choices = Materials },
            new("from", ContractFactType.Choice, false, ContractDisclosure.All, "Where the step starts: a Factory hex, a rubbled hex of that Factory, or outside it") { Choices = Froms },
            new("to", ContractFactType.Choice, false, ContractDisclosure.All, "Where the step ends: a hex of that Factory, or outside it") { Choices = Tos },
            ScenarioA1RuleContracts.Flag("acrossFactoryHexside", "The hexside crossed is a Factory hexside of that building"),
            ScenarioA1RuleContracts.Flag("vehicularEntrance", "The Factory Location entered from outside, or left, has a printed stairwell, or the step crosses a road hexside"),
            ScenarioA1RuleContracts.Flag("entranceRoofless", "That Location is Roofless"),
            ScenarioA1RuleContracts.Flag("entranceFriendlyControlled", "That Location's hex is controlled by the mover's side"),
            new("toLevel", ContractFactType.Number, false, ContractDisclosure.All, "The level of the Location entered (0 ground level)"),
        ],
        ["factory-cost", "normal-cost", "refused", ContractVerdict.NotEvaluated],
        [new("movement-stepped", "the step, its MF or MP, and any Bog Check")],
        new("MPh", "3.32A"))
    {
        Decide = MovementVerdict,
    };

    private static ContractFact Firers(string name, string description) =>
        ScenarioA1RuleContracts.Flag(name, description, mayBeUnknown: true, disclosure: ContractDisclosure.Owner) with
        {
            Concerns = "firerId"
        };

    private static ContractFact Movers(string name, string description) =>
        ScenarioA1RuleContracts.Flag(name, description, mayBeUnknown: true, disclosure: ContractDisclosure.Owner) with
        {
            Concerns = "moverId"
        };

    private static ContractVerdict TemVerdict(ContractFacts facts)
    {
        var stone = facts.Text("material") == "stone";
        var residual = facts.Flag("residual")!.Value;
        var indirect = facts.Flag("indirectFire")!.Value;
        FactoryFirerPlace? firer = facts.Text("firerPlace") switch
        {
            "inside" => FactoryFirerPlace.Inside,
            "rubble" => FactoryFirerPlace.Rubble,
            "rooftop" => FactoryFirerPlace.Rooftop,
            "outside" => FactoryFirerPlace.Outside,
            _ => null,
        };
        FactoryResidualEntry? entry = facts.Text("residualEntry") switch
        {
            "building-hexside" => FactoryResidualEntry.BuildingHexside,
            "non-building-hexside" => FactoryResidualEntry.NonBuildingHexside,
            "bypass" => FactoryResidualEntry.Bypass,
            _ => null,
        };
        if (!indirect && !residual && facts.Text("firerId") is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("the firer is not named", "B23.741", 139)], []);
        }

        var verdict = ScenarioA1FactoryCalculator.Tem(new FactoryTemFacts(stone, firer, facts.Flag("tracedWithinDepiction"),
            facts.Flag("overAdjacentRubbleEnteringAcrossDepiction"), indirect, facts.Flag("roofless")!.Value, residual, entry));
        if (verdict.Tem is null)
        {
            return new(ContractVerdict.NotEvaluated, [new(verdict.Reason, "B23.741", 139)], []);
        }

        if (!verdict.FactoryTem)
        {
            return new("building-tem", [new(verdict.Reason, "B23.741", 139)], []);
        }

        // B23.741: the Factory Hindrance, for direct fire traced within the depiction from inside or from the rubble; none from the rooftop or outside.
        var hindrance = 0;
        if (!indirect && !residual && firer is FactoryFirerPlace.Inside or FactoryFirerPlace.Rubble)
        {
            if (facts.Number("factoryHexesTraced") is not { } traced || traced != decimal.Truncate(traced) || traced is < 0 or > 99)
            {
                return new(ContractVerdict.NotEvaluated, [new("the Factory hexes the LOS is traced through are not known", "B23.741", 139)], []);
            }

            hindrance = ScenarioA1FactoryCalculator.Hindrance((int)traced, firer.Value);
        }

        var text = hindrance == 0 ? "no Factory Hindrance" : string.Create(CultureInfo.InvariantCulture, $"a +{hindrance} Factory Hindrance");
        return new("factory-tem", [new(verdict.Reason, "B23.741", 139), new(string.Create(CultureInfo.InvariantCulture, $"Hindrance: {text} (B23.741)"), "B23.741", 139)], []);
    }

    private static ContractVerdict MovementVerdict(ContractFacts facts)
    {
        if (MoverFacts.FirstOrDefault(name => facts.Flag(name) is null) is { } unknown)
        {
            return new(ContractVerdict.NotEvaluated, [new($"the fact '{unknown}' is not known to this viewer", "B23.742", 139)], []);
        }

        if (facts.Number("toLevel") is not { } level || level != decimal.Truncate(level) || level is < -1 or > 9)
        {
            return new(ContractVerdict.NotEvaluated, [new("the level entered is a whole number", "B23.742", 139)], []);
        }

        var from = facts.Text("from")!;
        var to = facts.Text("to")!;
        var step = new FactoryStepFacts(from == "factory", to == "factory", from != "outside" && to == "factory", from == "factory-rubble",
            facts.Flag("acrossFactoryHexside")!.Value, facts.Flag("vehicularEntrance")!.Value, facts.Flag("entranceRoofless")!.Value,
            facts.Flag("entranceFriendlyControlled")!.Value);
        if (to == "factory" && ScenarioA1FactoryCalculator.OccupationRefusal((int)level, false, false) is { } occupation)
        {
            return new("refused", [new(occupation, "B23.74", 139)], []);
        }

        if (facts.Flag("vehicle")!.Value)
        {
            if (facts.Flag("afv") is not { } afv || facts.Text("movementType") is not { } type || facts.Number("mpAllotment") is not { } mp
                || mp != decimal.Truncate(mp) || mp is < 1 or > 99)
            {
                return new(ContractVerdict.NotEvaluated, [new("a vehicle's AFV status, movement type, and MP allotment are known to its owner", "B23.742", 139)], []);
            }

            var vehicle = ScenarioA1FactoryCalculator.VehicleStep(new FactoryVehicleStepFacts(type, afv, (int)mp, facts.Text("material") == "stone", step,
                facts.Flag("bypass")!.Value));
            if (vehicle.Refusal is { } refusal)
            {
                return new("refused", [new(refusal, "B23.742", 139)], []);
            }

            if (vehicle.HalfMp is not { } halfMp)
            {
                return new("normal-cost", [new("Not the Factory's case: Bypass, or the normal entry of a building (B23.41)", "B23.742", 139)], []);
            }

            var cost = string.Create(CultureInfo.InvariantCulture, $"{halfMp / 2m:0.#} MP");
            var bog = vehicle.BogCheck ? string.Create(CultureInfo.InvariantCulture, $", with a Bog Check at +{vehicle.BogDrm}") : string.Empty;
            var rubble = vehicle.RubbleCheck ? " and a rubble check before the next hex (B23.41)" : string.Empty;
            return new("factory-cost", [new(cost + bog + rubble, "B23.742", 139)], []);
        }

        return ScenarioA1FactoryCalculator.InfantryHalfMf(step) is { } halfMf
            ? new("factory-cost", [new(string.Create(CultureInfo.InvariantCulture, $"{halfMf / 2m:0.#} MF"), "B23.742", 139)], [])
            : new("normal-cost", [new("The building's normal entry cost applies (B23.4)", "B23.742", 139)], []);
    }
}
