using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contract of A21.11, A21.12, and A21.13 (p. 88; the A21.13 claim's design, section 1.4): may this unit fire this crew-served ordnance
/// weapon, and under what penalty, as <see cref="ScenarioA1NonQualifiedUseCalculator.Use"/> decides it. The Gun and its manning unit may be hidden or
/// concealed, so who mans it, whether it is captured, its caliber, and whether it fired this Game Turn are its owner's: the opponent's view leaves them
/// unknown and the verdict not evaluated. The shot itself is public once fired. The phase is public. Asked at every ordnance shot, from the PFPh
/// (2.22A) on, the MPh's and the DFPh's included (the design's review, finding 6).
/// </summary>
internal static class ScenarioA1NonQualifiedUseContract
{
    private const string Firer = "firerId";

    private static readonly string[] Users = ["crew", "mmc", "smc-group", "lone-smc"];

    [RuleContract]
    private static RuleContract Use() => new("a21.13-crew-served-use", "Crew-served weapon use",
        [new("A21.13", 88), new("A21.11", 88), new("A21.12", 88), new("A.11", 43), new("C5.8", 173), new("A15.23", 83), new("C9.2", 179)],
        [
            new(Firer, ContractFactType.Unit, false, ContractDisclosure.Owner, "The unit that mans and fires the weapon") { Concerns = Firer },
            new("user", ContractFactType.Choice, false, ContractDisclosure.Owner,
                "Who fires it: a crew (infantry or vehicle crew), a squad or HS (mmc), two or more SMC (smc-group), or one SMC (lone-smc)")
            {
                Choices = Users,
                Concerns = Firer
            },
            Owners("captured", "The weapon is possessed from an enemy side (A21.1)"),
            Owners("hero", "The lone SMC is a hero (A15.23)"),
            Owners("mortar", "The weapon is a mortar (C9.2)"),
            new("caliber", ContractFactType.Number, false, ContractDisclosure.Owner, "Its caliber, in mm") { Concerns = Firer },
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase of the shot") { Choices = ScenarioA1RuleContracts.Phases },
            Owners("firedThisGameTurn", "The weapon has already fired this Game Turn (A15.23)"),
        ],
        ["no-penalty", "penalized", "penalized-doubled", "fires-as-light", "refused", ContractVerdict.NotEvaluated],
        [new("ordnance-fired", "the shot, with its B#, X#, Multiple ROF, To Hit color, and Case H")],
        new("PFPh", "2.22A"))
    {
        Decide = Verdict,
    };

    private static ContractFact Owners(string name, string description) =>
        ScenarioA1RuleContracts.Flag(name, description, disclosure: ContractDisclosure.Owner) with
        {
            Concerns = Firer
        };

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        // A caliber that is not a whole number of mm an int holds leaves the question open (the review of Wiring Prompt 001, C932).
        if (facts.Number("caliber") is not { } given || given < 0 || given > int.MaxValue || given != decimal.Truncate(given))
        {
            return new ContractVerdict(ContractVerdict.NotEvaluated, [new ContractReason("the fact 'caliber' is not a whole number this contract reads", "A21.13", 88)], []);
        }

        var user = facts.Text("user") switch
        {
            "crew" => CrewServedUser.Crew,
            "mmc" => CrewServedUser.Mmc,
            "smc-group" => CrewServedUser.SmcGroup,
            _ => CrewServedUser.LoneSmc,
        };
        var verdict = ScenarioA1NonQualifiedUseCalculator.Use(new NonQualifiedUseFacts(user, facts.Flag("captured"), facts.Flag("hero"), facts.Flag("mortar"), (int)given)
        {
            Phase = facts.Text("phase"),
            FiredThisGameTurn = facts.Flag("firedThisGameTurn"),
        });
        if (verdict.Refusal is { } refusal)
        {
            var rule = verdict.RefusalRule!;
            return new ContractVerdict("refused", [new ContractReason(refusal, rule, Page(rule))], []);
        }

        var multiple = verdict.Multiple;
        if (verdict.FiresAsLight)
        {
            ContractReason light = new("It fires as a light mortar, with no Non-Qualified Use penalty", "C9.2", 179);
            return new ContractVerdict("fires-as-light", multiple > 0 ? [light, Penalties(multiple)] : [light], []);
        }

        if (multiple == 0)
        {
            return new ContractVerdict("no-penalty", [new ContractReason("A qualified crew fires its own side's weapon without penalty", "C2.1", 167)], []);
        }

        List<ContractReason> reasons = [Penalties(multiple)];
        if (!verdict.HeroDrmApplies)
        {
            reasons.Add(new ContractReason("A hero fires it as if captured and fired by an enemy crew; his heroic DRM does not apply", "A15.23", 83));
        }

        if (verdict.HeroGunLimit)
        {
            reasons.Add(new ContractReason("A Gun over 82mm: only once per Game Turn and only in the MPh or DFPh", "A15.23", 83));
        }

        return new ContractVerdict(multiple > 1 ? "penalized-doubled" : "penalized", reasons, []);
    }

    private static ContractReason Penalties(int multiple) => new(string.Create(CultureInfo.InvariantCulture,
        $"B# {ScenarioA1NonQualifiedUseCalculator.BreakdownLowering(multiple)} lower and the Original B# its X# (A.11); Multiple ROF {ScenarioA1NonQualifiedUseCalculator.RofLowering(multiple)} lower; the red To Hit Numbers; Case H +{ScenarioA1NonQualifiedUseCalculator.CaseHDrm(multiple)} (C5.8)"),
        multiple > 1 ? "A21.13" : "A21.12", 88);

    private static int Page(string rule) => rule switch
    {
        "A15.23" => 83,
        "C9.2" => 179,
        _ => 88,
    };
}
