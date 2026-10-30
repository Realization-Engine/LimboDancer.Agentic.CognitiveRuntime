namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Where a DC not operably Placed stays (A23.3, A20.24): with its unit, or in the unit's Location.</summary>
public enum DcHeldBy
{
    Unit, Location
}

/// <summary>
/// The facts of a DC's Placement (A23.3; RA23.3): whether the unit carries a DC and is Infantry; the phase; whether the target Location is ADJACENT and
/// in its LOS; whether it fired during the PFPh; whether it was pinned before the Placement this MPh. Null when not known.
/// </summary>
public sealed record DcPlacementFacts(bool CarryingDc, bool Infantry, string? Phase, bool? Adjacent, bool? InLos, bool? FiredInPrepFire, bool? PinnedThisMph);

/// <summary>
/// The wiring contracts of A23.3 and A23.4 (pp. 90 and 91; the A23.3 claim's design, RA23.10): may this unit Place its DC there now, and what does this
/// Original DR do to a Placed DC, as <see cref="ScenarioA1DemolitionChargeRules"/> decides them. The Placement is movement the opponent watches and the
/// attack is public; whether a concealed placer carries a DC, is Infantry, and is exempt from a PAATC (A11.6) are its owner's.
/// </summary>
internal static class ScenarioA1DemolitionChargeContract
{
    private static readonly string[] PlacerFacts = ["carryingDc", "infantry", "paatcExempt"];

    private static readonly string[] OpenFacts = ["adjacent", "inLos", "firedInPrepFire", "pinnedThisMph"];

    [RuleContract]
    private static RuleContract Placement() => new("a23.3-dc-placement", "DC Placement", [new("A23.3", 90), new("A11.6", 75)],
        [
            new("placerId", ContractFactType.Unit, false, ContractDisclosure.All, "The unit that Places the DC") { Concerns = "placerId" },
            Owners("carryingDc", "It carries a DC"),
            Owners("infantry", "It is Infantry"),
            Owners("paatcExempt", "A11.6 exempts it from a PAATC (an SMC, a Fanatic, a Berserk unit)"),
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            ScenarioA1RuleContracts.Flag("adjacent", "The target Location is ADJACENT to it", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("inLos", "The target Location is in its LOS", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("firedInPrepFire", "It fired during the PFPh", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("pinnedThisMph", "It was pinned before the Placement this MPh", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("enemyAfvInTarget", "A manned, unconcealed enemy AFV occupies the target Location (A11.6)"),
        ],
        ["may-place", "may-place-after-paatc", "refused", ContractVerdict.NotEvaluated],
        [new("movement-stepped", "the Placement, its MF, and the placer's CX")],
        new("MPh", "3.22A"))
    {
        Decide = PlacementVerdict,
    };

    [RuleContract]
    private static RuleContract Detonation() => new("a23.4-dc-detonation", "Placed DC detonation", [new("A23.4", 90), new("A23.2", 90)],
        [
            ScenarioA1RuleContracts.Flag("operable", "The DC is operably Placed"),
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            ScenarioA1RuleContracts.Flag("ownerPhasing", "The DC's owner is the phasing side"),
            ScenarioA1RuleContracts.Flag("placedThisPlayerTurn", "It was Placed in this Player Turn's MPh"),
            new("removalNumber", ContractFactType.Number, false, ContractDisclosure.All, "Its removal number (A23.2: 12, 10 non-elite, 8 non-elite and captured)"),
            new("originalDr", ContractFactType.Number, false, ContractDisclosure.All, "The Original DR of its attack"),
        ],
        ["detonates", "removed", "not-due", ContractVerdict.NotEvaluated],
        [new("fire-resolved", "the DC's attack, or its removal without detonation")],
        new("AFPh", "5.22A"))
    {
        Decide = DetonationVerdict,
    };

    private static ContractFact Owners(string name, string description) =>
        ScenarioA1RuleContracts.Flag(name, description, mayBeUnknown: true, disclosure: ContractDisclosure.Owner) with
        {
            Concerns = "placerId"
        };

    private static ContractVerdict PlacementVerdict(ContractFacts facts)
    {
        if (PlacerFacts.FirstOrDefault(name => facts.Flag(name) is null) is { } unknown)
        {
            return new(ContractVerdict.NotEvaluated, [new($"the fact '{unknown}' is not known to this viewer", "A23.3", 90)], []);
        }

        // A refusal the known facts already make is certain; one only an unknown fact could make leaves the question open (the build's review, finding 3).
        var placement = new DcPlacementFacts(facts.Flag("carryingDc")!.Value, facts.Flag("infantry")!.Value, facts.Text("phase")!.ToLowerInvariant(),
            facts.Flag("adjacent"), facts.Flag("inLos"), facts.Flag("firedInPrepFire"), facts.Flag("pinnedThisMph"));
        var certain = ScenarioA1DemolitionChargeRules.PlacementRefusal(placement with
        {
            Adjacent = placement.Adjacent ?? true,
            InLos = placement.InLos ?? true,
            FiredInPrepFire = placement.FiredInPrepFire ?? false,
            PinnedThisMph = placement.PinnedThisMph ?? false,
        });
        if (certain is not null)
        {
            return new("refused", [new(certain, "A23.3", 90)], []);
        }

        if (OpenFacts.Any(name => facts.Flag(name) is null))
        {
            return new(ContractVerdict.NotEvaluated, [new(ScenarioA1DemolitionChargeRules.PlacementRefusal(placement)!, "A23.3", 90)], []);
        }

        return ScenarioA1DemolitionChargeRules.PlacementNeedsPaatc(facts.Flag("enemyAfvInTarget")!.Value, facts.Flag("paatcExempt")!.Value)
            ? new("may-place-after-paatc", [new("It may Place the DC once it passes a PAATC for the enemy AFV there", "A11.6", 75)], [])
            : new("may-place", [new("It may Place the DC", "A23.3", 90)], []);
    }

    private static ContractVerdict DetonationVerdict(ContractFacts facts)
    {
        if (facts.Number("removalNumber") is not { } removal || removal != decimal.Truncate(removal) || removal is < 2 or > 12
            || facts.Number("originalDr") is not { } dr || dr != decimal.Truncate(dr) || dr is < 2 or > 12)
        {
            return new(ContractVerdict.NotEvaluated, [new("the removal number and the DR are whole numbers from 2 to 12", "A23.4", 90)], []);
        }

        if (!ScenarioA1DemolitionChargeRules.DetonationWindow(facts.Text("phase")!.ToLowerInvariant(), facts.Flag("ownerPhasing")!.Value, facts.Flag("operable")!.Value,
            facts.Flag("placedThisPlayerTurn")!.Value))
        {
            return new("not-due", [new("An operably Placed DC detonates in its owner's AFPh of the Player Turn it was Placed", "A23.4", 90)], []);
        }

        return ScenarioA1DemolitionChargeRules.DcMalfunction((int)dr, (int)removal)
            ? new("removed", [new("The Original DR reaches its removal number: removed without detonation", "A23.4", 90)], [])
            : new("detonates", [new("It detonates and is removed from play", "A23.4", 90)], []);
    }
}
