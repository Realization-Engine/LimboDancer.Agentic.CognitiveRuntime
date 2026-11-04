using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>B16.31 declared/hypothetical FP components. Public required context never defaults a missing production feature to false.</summary>
internal static class ScenarioA1MarshHeContract
{
    [RuleContract]
    private static RuleContract MarshHe() => new("b16.31-marsh-he", "Marsh HE firepower multiplier",
        [new("B16.31", 130), new("B16.6", 130), new("B16.7", 130), new("B16.8", 130)],
        [
            Choice("effectFamily", "The declared or labelled hypothetical effect, not an undeclared ammunition choice", ["he", "dc", "he-equivalency", "non-he", "residual", "unsupported-vt-ffe"]),
            Choice("terrainKind", "Public terrain before the marsh water/weather consequences", ["marsh", "mudflat", "other", "swamp"]),
            Choice("snowCover", "Scenario-defined snow cover; only its marsh HE consequence is evaluated", ["none", "snow", "deep-snow"]),
            ScenarioA1RuleContracts.Flag("subZero", "Scenario-defined sub-zero temperature"),
            ScenarioA1RuleContracts.Flag("connectedToFloodedWater", "This marsh is adjacent to, or connected by continuous marsh to, flooded stream/river"),
            ScenarioA1RuleContracts.Flag("bridgePresent", "A bridge is present"),
            Choice("targetKind", "An attack on a unit, the bridge itself, or the marsh ground Location", ["unit", "bridge", "ground-location"]),
            new("targetId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The unit when its private bridge position matters") { Concerns = "targetId" },
            new("onBridge", ContractFactType.Flag, true, ContractDisclosure.Owner, "That unit is on the bridge, not beneath/beside it") { Concerns = "targetId" },
        ], ["halved", "unchanged", ContractVerdict.NotEvaluated], [], new("PFPh", "2.22A"))
    {
        Decide = Decide,
    };

    private static ContractFact Choice(string name, string description, string[] choices) =>
        new(name, ContractFactType.Choice, false, ContractDisclosure.All, description)
        {
            Choices = choices
        };

    private static ContractVerdict Decide(ContractFacts facts)
    {
        MarshHeEffect? effect = facts.Text("effectFamily") switch
        {
            "he" => MarshHeEffect.He,
            "dc" => MarshHeEffect.DemolitionCharge,
            "he-equivalency" => MarshHeEffect.HeEquivalency,
            "non-he" => MarshHeEffect.NonHe,
            "residual" => MarshHeEffect.Residual,
            "unsupported-vt-ffe" => MarshHeEffect.UnsupportedVtFfe,
            _ => null,
        };
        MarshHeTerrain? terrain = facts.Text("terrainKind") switch
        {
            "marsh" => MarshHeTerrain.Marsh,
            "mudflat" => MarshHeTerrain.Mudflat,
            "other" => MarshHeTerrain.Other,
            "swamp" => MarshHeTerrain.Swamp,
            _ => null,
        };
        MarshHeSnow? snow = facts.Text("snowCover") switch
        {
            "none" => MarshHeSnow.None,
            "snow" => MarshHeSnow.Snow,
            "deep-snow" => MarshHeSnow.DeepSnow,
            _ => null,
        };
        MarshHeTarget? target = facts.Text("targetKind") switch
        {
            "unit" => MarshHeTarget.Unit,
            "bridge" => MarshHeTarget.Bridge,
            "ground-location" => MarshHeTarget.GroundLocation,
            _ => null,
        };
        var result = ScenarioA1MarshHeCalculator.Evaluate(new(effect, terrain, snow, facts.Flag("subZero"),
            facts.Flag("connectedToFloodedWater"), facts.Flag("bridgePresent"), target,
            facts.Text("targetId") is null ? null : facts.Flag("onBridge")));
        var outcome = result.Disposition == MarshHeDisposition.NotEvaluated ? ContractVerdict.NotEvaluated
            : result.Multiplier == 0.5m ? "halved" : "unchanged";
        var text = result.Multiplier is { } factor ? string.Create(CultureInfo.InvariantCulture, $"Marsh HE multiplier: {factor:0.#}. {result.Reason}") : result.Reason;
        return new(outcome, [new(text + " This component also serves MPh/DFPh/AFPh; it grants no weapon-use permission or new event.", result.Rule, result.Page)], []);
    }
}
