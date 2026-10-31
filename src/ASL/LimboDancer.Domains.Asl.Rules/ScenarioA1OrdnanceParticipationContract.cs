namespace LimboDancer.Domains.Asl.Rules;

/// <summary>The owner-only contract for a real second SMC firing a Gun or mortar, distinct from its director and Spotter.</summary>
internal static class ScenarioA1OrdnanceParticipationContract
{
    [RuleContract]
    private static RuleContract Participation() => new("c9-ordnance-participation", "Two SMC firing a Gun or mortar",
        [new("C9.2", 179), new("A21.13", 88), new("A7.35", 56), new("C9.3", 179), new("A7.25", 55)],
        [
            new("phase", ContractFactType.Text, false, ContractDisclosure.All, "The current phase"),
            new("firerId", ContractFactType.Unit, false, ContractDisclosure.Owner, "The primary firer") { Concerns = "firerId" },
            new("secondId", ContractFactType.Unit, false, ContractDisclosure.Owner, "The additional firer") { Concerns = "secondId" },
            new("primaryKind", ContractFactType.Text, false, ContractDisclosure.Owner, "The primary firer's kind") { Concerns = "firerId" },
            new("secondKind", ContractFactType.Text, false, ContractDisclosure.Owner, "The additional firer's kind") { Concerns = "secondId" },
            Flag("active", "Active, with a known definition"),
            Flag("sameSide", "Of the primary firer's playing side"),
            Flag("sameLocation", "In the primary firer's complete Location, including level"),
            Flag("broken", "Broken"), Flag("berserk", "Berserk"), Flag("pinned", "Pinned"),
            Flag("firedOther", "Already fired apart from this weapon"),
            Flag("melee", "Held in Melee"), Flag("captured", "Captured"), Flag("ti", "Temporarily Immobilized"),
            Flag("usedOther", "Used another weapon this fire phase"),
            Flag("directed", "Directed fire this phase, or is this shot's director"),
            Flag("spotted", "Spotted this phase, or is this shot's Spotter"),
            Flag("opportunityFire", "Declared Opportunity Fire"),
        ],
        ["may-participate", "refused", ContractVerdict.NotEvaluated],
        [new("ordnance-fired", "Both actual firers are recorded and receive the firing-use effects")],
        new("PFPh", "2.22A"))
    {
        Decide = Verdict,
    };

    private static ContractFact Flag(string name, string description) =>
        ScenarioA1RuleContracts.Flag(name, description, disclosure: ContractDisclosure.Owner) with
        {
            Concerns = "secondId"
        };

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        var crew = new OrdnanceCrew(facts.Text("secondId"), "selected-definition", facts.Flag("broken"), facts.Flag("pinned"), facts.Flag("berserk"), false,
            facts.Flag("firedOther"));
        var second = new OrdnanceAdditionalFirer(crew, facts.Flag("active"), facts.Flag("sameSide"), facts.Flag("sameLocation"),
            facts.Flag("melee"), facts.Flag("captured"), facts.Flag("ti"), facts.Flag("usedOther"), facts.Flag("directed"), facts.Flag("spotted"))
        {
            OpportunityFireDeclared = facts.Flag("opportunityFire"),
        };
        var refusal = ScenarioA1OrdnanceParticipationCalculator.CandidateRefusal(facts.Text("firerId")!, facts.Text("primaryKind"), facts.Text("secondKind"), second,
            phase: facts.Text("phase"));
        return refusal is null
            ? new("may-participate", [new("The two distinct SMC may participate; the weapon's normal firing authorization still applies", "A21.13", 88)], [])
            : new("refused", [new(refusal, "A21.13", 88)], []);
    }
}
