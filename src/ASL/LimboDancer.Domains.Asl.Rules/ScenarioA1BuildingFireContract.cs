using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Declared or labelled hypothetical building contributions. Private facts depend on self-owned unit anchors, not public geometry.</summary>
internal static class ScenarioA1BuildingFireContract
{
    [RuleContract]
    private static RuleContract BuildingTem() => new("b23.31-building-fire-tem", "Building TEM contribution",
        [new("B23.31", 136), new("B23.3", 136), new("B23.741", 139), new("B23.743", 140)],
        [
            Choice("buildingKind", "The target building's public material/SSR, or a labelled hypothetical", ["none", "wood", "stone", "wood-factory", "stone-factory"]),
            Choice("locationKind", "Ordinary interior/ground-Factory Location, or a separately ruled special Location", ["ordinary", "unsupported-special"]),
            Choice("profile", "The scenario uses standard or Deluxe ASL", ["standard", "deluxe"]),
            Choice("attackKind", "Declared or hypothetical effect; residual includes Fire Lane FP. Not an undeclared ammunition picker", ["direct", "indirect", "residual"]),
            ScenarioA1RuleContracts.Flag("roofless", "The target Location is Roofless"),
            new("targetId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The target, when its private Bypass/entry facts matter") { Concerns = "targetId" },
            PrivateFlag("bypassing", "The target is Bypassing", "targetId"),
            new("residualEntry", ContractFactType.Choice, true, ContractDisclosure.Owner, "Factory Residual entry; irrelevant to other branches")
            {
                Choices = ["building-hexside", "non-building-hexside", "bypass"],
                Concerns = "targetId",
            },
            new("firerId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The firer, only when direct Factory TEM needs its private facts") { Concerns = "firerId" },
            new("firerPlace", ContractFactType.Choice, true, ContractDisclosure.Owner, "The firer relative to the target Factory")
            {
                Choices = ["inside", "rubble", "rooftop", "outside"],
                Concerns = "firerId",
            },
            PrivateFlag("tracedWithinDepiction", "The LOS remains within the Factory depiction", "firerId"),
            PrivateFlag("overAdjacentRubbleEnteringAcrossDepiction", "Outside LOS crosses adjacent Factory rubble and enters across the depiction", "firerId"),
        ],
        ["building-tem", "no-building-tem", "not-applicable", ContractVerdict.NotEvaluated], [], new("PFPh", "2.22A"))
    {
        Decide = Decide,
    };

    private static ContractFact Choice(string name, string description, string[] choices) =>
        new(name, ContractFactType.Choice, false, ContractDisclosure.All, description)
        {
            Choices = choices
        };

    private static ContractFact PrivateFlag(string name, string description, string unit) =>
        new(name, ContractFactType.Flag, true, ContractDisclosure.Owner, description)
        {
            Concerns = unit
        };

    private static ContractVerdict Decide(ContractFacts facts)
    {
        var targetKnown = facts.Text("targetId") is not null;
        var firerKnown = facts.Text("firerId") is not null;
        BuildingFireKind? building = facts.Text("buildingKind") switch
        {
            "none" => BuildingFireKind.None,
            "wood" => BuildingFireKind.Wood,
            "stone" => BuildingFireKind.Stone,
            "wood-factory" => BuildingFireKind.WoodFactory,
            "stone-factory" => BuildingFireKind.StoneFactory,
            _ => null,
        };
        BuildingFireAttack? attack = facts.Text("attackKind") switch
        {
            "direct" => BuildingFireAttack.Direct,
            "indirect" => BuildingFireAttack.Indirect,
            "residual" => BuildingFireAttack.Residual,
            _ => null,
        };
        FactoryResidualEntry? entry = facts.Text("residualEntry") switch
        {
            "building-hexside" => FactoryResidualEntry.BuildingHexside,
            "non-building-hexside" => FactoryResidualEntry.NonBuildingHexside,
            "bypass" => FactoryResidualEntry.Bypass,
            _ => null,
        };
        FactoryFirerPlace? firer = facts.Text("firerPlace") switch
        {
            "inside" => FactoryFirerPlace.Inside,
            "rubble" => FactoryFirerPlace.Rubble,
            "rooftop" => FactoryFirerPlace.Rooftop,
            "outside" => FactoryFirerPlace.Outside,
            _ => null,
        };
        var result = ScenarioA1BuildingFireCalculator.BuildingTem(new(building,
            facts.Text("locationKind") == "ordinary" ? BuildingFireLocation.Ordinary : BuildingFireLocation.UnsupportedSpecial,
            facts.Text("profile") == "standard" ? BuildingFireProfile.Standard : BuildingFireProfile.Deluxe, attack,
            targetKnown ? facts.Flag("bypassing") : null, facts.Flag("roofless"), firerKnown ? firer : null,
            firerKnown ? facts.Flag("tracedWithinDepiction") : null, firerKnown ? facts.Flag("overAdjacentRubbleEnteringAcrossDepiction") : null,
            targetKnown ? entry : null));
        var outcome = result.Disposition switch
        {
            BuildingFireDisposition.NotApplicable => "not-applicable",
            BuildingFireDisposition.Evaluated => result.Tem == 0 ? "no-building-tem" : "building-tem",
            _ => ContractVerdict.NotEvaluated,
        };
        var text = result.Tem is { } tem ? string.Create(CultureInfo.InvariantCulture, $"Building TEM contribution: {tem}. {result.Reason}") : result.Reason;
        return new(outcome, [new(text + " Other attack DRM rules still apply; this also serves MPh/DFPh/AFPh resolution.", result.Rule, result.Page)], []);
    }
}
