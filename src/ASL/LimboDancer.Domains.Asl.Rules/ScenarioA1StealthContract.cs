namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contract of A11.17 and A11.18 (p. 73; the A11.17 claim's design, section 4.4): is this unit Stealthy, Lax, or Normal for this dr, and what
/// drm does it give, as <see cref="ScenarioA1StealthCalculator.Status"/> decides it. The dr, Good Order, berserk, a Banzai or Human Wave, and the card's
/// SSR are public; what the unit is (its kind, heroic, nationality, class, designation, and Inexperience) is its owner's, since a concealed unit hides it.
/// For the Ambush, A11.19 reveals each unit's Strength Factor before the dr (the p. 74 example), so the other side knows them too; a fact's disclosure is
/// one for every purpose in this framework, so the contract keeps them at Owner, the stricter reading, and the wiring may give them for an Ambush.
/// </summary>
internal static class ScenarioA1StealthContract
{
    private static readonly string[] Purposes = ["ambush", "concealment", "search", "search-casualty"];

    private static readonly string[] Kinds = ["asl:squad", "asl:half-squad", "asl:crew", "asl:vehicle-crew", "asl:leader", "asl:hero"];

    private static readonly string[] Classes = ["elite", "1st-line", "2nd-line", "green", "conscript", "none"];

    private static readonly string[] Designations = ["none", "anzac", "gurkha", "commando", "partisan", "raider"];

    private static readonly string[] DesignationNames = ["", "ANZAC", "Gurkha", "Commando", "Partisan", "Raider"];

    private static readonly string[] DrNames = ["Ambush status dr", "Concealment Growth dr", "Searching dr", "Search Casualties dr"];

    [RuleContract]
    private static RuleContract Stealth() => new("a11.17-stealth", "Stealth and Lax",
        [new("A11.17", 73), new("A11.18", 73), new("A19.2", 86), new("A25.7", 97), new("A25.64", 97)],
        [
            new("unitId", ContractFactType.Unit, false, ContractDisclosure.Owner, "The unit") { Concerns = "unitId" },
            new("purpose", ContractFactType.Choice, false, ContractDisclosure.All, "The dr: the Ambush (A11.4), Concealment Growth (A12.122), Searching (A12.152), or Search Casualties (A12.154)") { Choices = Purposes },
            ScenarioA1RuleContracts.Flag("goodOrder", "It is in Good Order (A.7)"),
            ScenarioA1RuleContracts.Flag("berserk", "It is berserk (A15.432: Lax in CC)"),
            ScenarioA1RuleContracts.Flag("banzaiOrHumanWave", "It made a Banzai Charge or a Human Wave this Player Turn (G1.6, A25.23)"),
            ScenarioA1RuleContracts.Flag("ssrStealthy", "An SSR makes its side's troops Stealthy (A11.17)"),
            ScenarioA1RuleContracts.Flag("ssrLax", "An SSR makes its side's units Lax (A11.18)"),
            Owners(new("kind", ContractFactType.Choice, false, ContractDisclosure.Owner, "Its kind in the catalog") { Choices = Kinds }),
            Owners(ScenarioA1RuleContracts.Flag("heroic", "It is heroic (A15.21)", disclosure: ContractDisclosure.Owner)),
            Owners(new("nationality", ContractFactType.Text, false, ContractDisclosure.Owner, "Its nationality in the catalog")),
            Owners(new("class", ContractFactType.Choice, false, ContractDisclosure.Owner, "Its class (A1.25), none for a SMC or a vehicle crew") { Choices = Classes }),
            Owners(new("designation", ContractFactType.Choice, false, ContractDisclosure.Owner, "Its force's designation: ANZAC, Gurkha, Commando, Partisan, Raider") { Choices = Designations }),
            Owners(ScenarioA1RuleContracts.Flag("inexperienced", "It is Inexperienced (A19.2, A19.3; Unarmed included); unknown for a Green MMC whose leader is not known",
                mayBeUnknown: true, disclosure: ContractDisclosure.Owner)),
        ],
        ["stealthy", "lax", "normal", ContractVerdict.NotEvaluated],
        [],
        new("CCPh", "8.12B"))
    {
        Decide = Verdict,
    };

    private static ContractFact Owners(ContractFact fact) => fact with
    {
        Concerns = "unitId"
    };

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        var purposeIndex = Array.IndexOf(Purposes, facts.Text("purpose"));
        var purpose = (StealthPurpose)purposeIndex;
        var designation = Enum.Parse<StealthDesignation>(facts.Text("designation")!, ignoreCase: true);
        var @class = facts.Text("class") is "none" ? null : facts.Text("class");
        var unit = new StealthFacts(facts.Text("kind"), facts.Flag("heroic")!.Value, facts.Text("nationality")!.ToLowerInvariant(), @class, designation,
            facts.Flag("ssrStealthy")!.Value, facts.Flag("ssrLax")!.Value, facts.Flag("inexperienced"), facts.Flag("berserk")!.Value, facts.Flag("goodOrder")!.Value,
            facts.Flag("banzaiOrHumanWave")!.Value);
        var dr = DrNames[purposeIndex];
        return ScenarioA1StealthCalculator.Status(unit, purpose) switch
        {
            StealthStatus.Lax => new("lax", [LaxReason(unit, purpose, dr)], []),
            StealthStatus.Stealthy => new("stealthy", [StealthyReason(unit, dr)], []),
            StealthStatus.Normal => new("normal", [NormalReason(unit, dr)], []),
            _ => new(ContractVerdict.NotEvaluated, [new("Whether it is Lax is not known: its Inexperience (A19.3) or an Italian MMC's class (A25.64) decides it", "A11.18", 73)], []),
        };
    }

    private static ContractReason LaxReason(StealthFacts unit, StealthPurpose purpose, string dr) =>
        unit.SsrLax ? new($"Lax by SSR: +1 to its {dr}", "A11.18", 73)
        : unit.BanzaiOrHumanWave ? new($"Lax for this Player Turn after a Banzai Charge or a Human Wave: +1 to its {dr}", "G1.6", 280)
        : unit.Berserk && purpose == StealthPurpose.Ambush ? new($"A berserk unit is Lax in CC: +1 to its {dr}", "A15.432", 84)
        : unit.Inexperienced == true || unit.Class == "conscript" ? new($"Inexperienced Personnel are Lax: +1 to its {dr}", "A19.36", 86)
        : new($"Non-elite Italian units are always Lax: +1 to its {dr}", "A25.64", 97);

    private static ContractReason StealthyReason(StealthFacts unit, string dr) =>
        unit.Kind == "asl:hero" ? new($"A hero is always Stealthy: -1 to its {dr}", "A15.24", 83)
        : unit.Kind == "asl:leader" && unit.Heroic ? new($"A heroic leader is Stealthy as a hero: -1 to its {dr}", "A15.21", 83)
        : unit.SsrStealthy ? new($"Stealthy by SSR: -1 to its {dr}", "A11.17", 73)
        : unit.Designation != StealthDesignation.None ? new($"{DesignationNames[(int)unit.Designation]} troops are always Stealthy unless Lax: -1 to its {dr}", "A11.17", 73)
        : unit.Nationality == "japanese" ? new($"Good Order Elite and 1st Line Japanese Infantry are Stealthy: -1 to its {dr}", "G1.6", 280)
        : new($"Elite and 1st Line Finns are Stealthy: -1 to its {dr}", "A25.7", 97);

    private static ContractReason NormalReason(StealthFacts unit, string dr) =>
        ScenarioA1StealthCalculator.StealthBarredByClass(unit.Class) ? new($"Green and Conscript troops never use Stealth: no drm to its {dr}", "A19.2", 86)
        : !unit.GoodOrder && ScenarioA1StealthCalculator.StealthyByNature(unit) == true ? new($"Only Good Order troops are Stealthy: no drm to its {dr}", "A11.17", 73)
        : new($"Neither Stealthy nor Lax: no drm to its {dr}", "A11.17", 73);
}
