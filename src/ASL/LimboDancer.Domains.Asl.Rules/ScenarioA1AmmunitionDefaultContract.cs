namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Owner stock and explicitly hypothetical/known-public target protection; no hidden-counter lookup.</summary>
internal static class ScenarioA1AmmunitionDefaultContract
{
    [RuleContract]
    private static RuleContract Default() => new("c2.21-ammunition-default", "Default ammunition component",
        [new("C2.21", 167), new("C3.31", 169), new("C3.32", 169), new("C3.33", 169), new("C8.8", 179)],
        [
            ScenarioA1RuleContracts.Flag("declared", "A round was explicitly declared, not necessarily accepted", mayBeUnknown: true),
            Choice("targetType", "Declared Target Type", ["infantry", "vehicle", "area"]),
            Choice("targetProtection", "Known-public or labelled hypothetical target protection; never inspect a hidden definition", ["unarmored", "armored"]),
            new("firerId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The manning unit or vehicle MA owner") { Concerns = "firerId" },
            Choice("weaponKind", "The owner's weapon family", ["gun", "mortar", "unsupported"]) with { Disclosure = ContractDisclosure.Owner, Concerns = "firerId" },
            StockFlag("apProhibited", "Counter prohibits ordinary AP"),
            StockFlag("apListedDepletion", "The counter lists AP# at any date"),
            StockFlag("apExhausted", "AP has run out"),
            StockFlag("heProhibited", "Counter prohibits ordinary HE"),
            StockFlag("heListedDepletion", "The counter lists HE# at any date"),
            StockFlag("heExhausted", "HE has run out"),
        ], ["default-he", "default-ap", "declaration-required", "not-applicable", ContractVerdict.NotEvaluated], [], new("PFPh", "2.22A"))
    {
        Decide = Decide,
    };

    private static ContractFact Choice(string name, string description, string[] choices) =>
        new(name, ContractFactType.Choice, true, ContractDisclosure.All, description)
        {
            Choices = choices
        };

    private static ContractFact StockFlag(string name, string description) =>
        new(name, ContractFactType.Flag, true, ContractDisclosure.Owner, description)
        {
            Concerns = "firerId"
        };

    private static ContractVerdict Decide(ContractFacts facts)
    {
        // Workbench converts sealed handles to an unknown identity. Never use stale stock without that identity.
        var anchored = facts.Text("firerId") is not null;
        AmmunitionDefaultWeapon? weapon = !anchored ? null : facts.Text("weaponKind") switch
        {
            "gun" => AmmunitionDefaultWeapon.Gun,
            "mortar" => AmmunitionDefaultWeapon.Mortar,
            "unsupported" => AmmunitionDefaultWeapon.Unsupported,
            _ => null,
        };
        var result = ScenarioA1AmmunitionDefaultCalculator.SelectDefault(new(facts.Flag("declared"), weapon,
            facts.Text("targetProtection") switch
            {
                "unarmored" => AmmunitionTargetProtection.Unarmored,
                "armored" => AmmunitionTargetProtection.Armored,
                _ => (AmmunitionTargetProtection?)null,
            }, facts.Text("targetType") switch
            {
                "infantry" => AmmunitionDefaultTargetType.Infantry,
                "vehicle" => AmmunitionDefaultTargetType.Vehicle,
                "area" => AmmunitionDefaultTargetType.Area,
                _ => (AmmunitionDefaultTargetType?)null,
            }, anchored ? new(facts.Flag("apProhibited"), facts.Flag("apListedDepletion"), facts.Flag("apExhausted")) : null,
            anchored ? new(facts.Flag("heProhibited"), facts.Flag("heListedDepletion"), facts.Flag("heExhausted")) : null));
        var outcome = result.Disposition switch
        {
            AmmunitionDefaultDisposition.DefaultSelected => result.Ammunition == DefaultAmmunition.He ? "default-he" : "default-ap",
            AmmunitionDefaultDisposition.DeclarationRequired => "declaration-required",
            AmmunitionDefaultDisposition.NotApplicable => "not-applicable",
            _ => ContractVerdict.NotEvaluated,
        };
        return new(outcome, [new(result.Reason, result.Rule, result.Page)], []);
    }
}
