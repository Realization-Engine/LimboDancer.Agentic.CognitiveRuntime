namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contract of C9.1, C9.2, C9.4, and A21.13 (p. 179; the C9 claim's design, ruling RC9.7): may this mortar fire at this target, and does
/// it keep its Multiple ROF, as <see cref="ScenarioA1MortarCalculator.Shot"/> decides it. The mortar may sit under a "?" and its firer may be a
/// concealed stack's SMC, so every fact of the shot is its owner's: the opponent's view leaves them unknown and the verdict not evaluated. The
/// maximum range (C2.25) and the To Hit are the ordnance calculator's, not this contract's. Its facts are entered by hand on the workbench.
/// </summary>
internal static class ScenarioA1MortarContract
{
    private const string Mortar = "mortarId";

    private static readonly string[] Numbers = ["caliber", "minimumRange", "range"];

    [RuleContract]
    private static RuleContract Shot() => new("c9-mortar-shot", "Mortar shot", [new("C9.1", 179), new("C9.2", 179), new("C9.4", 179), new("A21.13", 88)],
        [
            new(Mortar, ContractFactType.Unit, true, ContractDisclosure.Owner, "The mortar") { Concerns = Mortar },
            new("weapon", ContractFactType.Text, false, ContractDisclosure.Owner, "Its kind (asl:light-mortar) or its gun type (mortar, mtr, at, ...)") { Concerns = Mortar },
            new("caliber", ContractFactType.Number, false, ContractDisclosure.Owner, "Its caliber, in mm") { Concerns = Mortar },
            new("minimumRange", ContractFactType.Number, false, ContractDisclosure.Owner, "Its minimum range, the first number of its \"[#-#]\"") { Concerns = Mortar },
            new("range", ContractFactType.Number, false, ContractDisclosure.Owner, "The hex range to the target") { Concerns = Mortar },
            Owners("areaTargetType", "The shot is on the Area Target Type (C3.33)"),
            Owners("namesVehicle", "The shot names a vehicle as a To Kill target"),
            Owners("loneSmc", "Its firer is one SMC with no other SMC (A7.5, A9.12)"),
        ],
        ["may-fire", "refused", ContractVerdict.NotEvaluated],
        [new("ordnance-fired", "the shot, its To Hit DR, and whether its Multiple ROF is kept")],
        new("PFPh", "2.22A"))
    {
        Decide = Verdict,
    };

    private static ContractFact Owners(string name, string description) =>
        ScenarioA1RuleContracts.Flag(name, description, disclosure: ContractDisclosure.Owner) with
        {
            Concerns = Mortar
        };

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        // A number that is not a whole number of hexes or mm an int holds leaves the question open (the review of Wiring Prompt 001, C932).
        if (Numbers.FirstOrDefault(name => facts.Number(name) is not { } given || given < 0 || given > int.MaxValue || given != decimal.Truncate(given)) is { } bad)
        {
            return new ContractVerdict(ContractVerdict.NotEvaluated, [new ContractReason($"the fact '{bad}' is not a whole number this contract reads", "C9.4", 179)], []);
        }

        var weapon = facts.Text("weapon")!;
        var caliber = (int)facts.Number("caliber")!.Value;
        var verdict = ScenarioA1MortarCalculator.Shot(new MortarShotFacts(ScenarioA1MortarCalculator.IsMortar(weapon, weapon), caliber,
            (int)facts.Number("minimumRange")!.Value, (int)facts.Number("range")!.Value, facts.Flag("areaTargetType")!.Value, facts.Flag("namesVehicle")!.Value,
            facts.Flag("loneSmc")!.Value));
        if (verdict.Refusal is { } refusal)
        {
            return new ContractVerdict("refused", [new ContractReason(refusal, "C9", 179)], []);
        }

        // C9.2 for a light mortar; A21.13 for a lone SMC firing a larger one as if it were light.
        var light = ScenarioA1MortarCalculator.IsLight(caliber);
        ContractReason rof = verdict.MultipleRofKept
            ? new("It keeps its Multiple ROF, unless another rule lowers it (C9.31, C5.4, C5.2)", "C2.24", 167)
            : new("A lone SMC fires it as a light mortar and loses its Multiple ROF", light ? "C9.2" : "A21.13", light ? 179 : 88);
        return new ContractVerdict("may-fire", [new ContractReason("It may fire: the Area Target Type, no vehicle named, at or beyond its minimum range", "C9.1", 179), rof], []);
    }
}
