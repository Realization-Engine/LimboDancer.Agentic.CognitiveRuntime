using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contracts of the A20.54 claim (the Five Claims B design, section 3.4): may this prisoner attack its Guard to escape, and after a NTC
/// (A20.55); is a #KIA survivor broken or Casualty Reduced (A7.301, A20.54); and what CVP the captor scores for an enemy prisoner (A26.222), as
/// <see cref="ScenarioA1PrisonerCalculator"/> decides them. A prisoner is never concealed or hidden, so its identity and nationality are public; the
/// Guard's state, the attack and its side, and the card's facts are public too. Every fact is All. The CVP contract has no ASOP step: it applies at any
/// Victory Conditions check, immediate ones included, and at the scenario's end.
/// </summary>
internal static class ScenarioA1PrisonerContract
{
    private static readonly string[] Nationalities = ["german", "russian", "american", "british", "french", "finnish", "italian", "japanese", "chinese", "axis-minor",
        "romanian", "hungarian", "slovakian", "croatian", "bulgarian", "other"];

    private static readonly string[] CvpStatuses = ["held", "exited-under-guard", "eliminated-by-own-side", "eliminated-otherwise", "escaped"];

    [RuleContract]
    private static RuleContract Escape() => new("a20.55-prisoner-escape", "Prisoner escape",
        [new("A20.55", 88), new("A25.63", 97), new("A25.82", 98), new("G1.621", 280)],
        [
            new("prisonerId", ContractFactType.Unit, false, ContractDisclosure.All, "The prisoner that would attack its Guard"),
            new("nationality", ContractFactType.Choice, false, ContractDisclosure.All, "The prisoner's nationality") { Choices = Nationalities },
            new("captorNationality", ContractFactType.Choice, false, ContractDisclosure.All, "Its Guard's nationality") { Choices = Nationalities },
            ScenarioA1RuleContracts.Flag("infantry", "The prisoner is Infantry"),
            ScenarioA1RuleContracts.Flag("axisMinorAtHome", "The card puts an Axis Minor prisoner within its own national borders (unknown: it is not)", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("guardBroken", "Its Guard is broken"),
            ScenarioA1RuleContracts.Flag("meleeInLocation", "A Melee exists in the Location"),
        ],
        ["escape-after-ntc", "escape-no-ntc", "refused-nationality", "refused-not-infantry", "refused-guard", ContractVerdict.NotEvaluated],
        [new("close-combat-resolved", "the prisoners' round: each escape NTC, the escape attacks, and each prisoner no longer guarded")],
        new("CCPh", "8.14B"))
    {
        Decide = EscapeVerdict,
    };

    [RuleContract]
    private static RuleContract KiaSurvivor() => new("a7.301-kia-survivor", "#KIA survivor", [new("A7.301", 55), new("A20.54", 88)],
        [
            ScenarioA1RuleContracts.Flag("broken", "The unit is broken"),
            ScenarioA1RuleContracts.Flag("heroic", "The unit is a hero or a heroic leader (A15.2)"),
            ScenarioA1RuleContracts.Flag("berserk", "The unit is berserk"),
            ScenarioA1RuleContracts.Flag("unarmed", "The unit is a prisoner or an Unarmed unit (A20.5)"),
        ],
        ["broken", "casualty-reduction", ContractVerdict.NotEvaluated],
        [new("fire-resolved", "the attack, and for each unit the #KIA did not eliminate, broken or Casualty Reduced")],
        new("PFPh", "2.22A"))
    {
        Decide = KiaVerdict,
    };

    [RuleContract]
    private static RuleContract Cvp() => new("a26.222-prisoner-cvp", "Prisoner CVP",
        [new("A26.222", 100), new("A20.1", 86), new("A20.54", 88), new("A20.55", 88)],
        [
            new("vp", ContractFactType.Number, false, ContractDisclosure.All, "The prisoner's normal VP (A26.211)"),
            new("status", ContractFactType.Choice, false, ContractDisclosure.All,
                "The prisoner: still held (or recaptured), taken off the map by its Guard, eliminated by an attack of its own side, eliminated otherwise (any attack not of its own side, or of a side not known), or escaped")
            {
                Choices = CvpStatuses
            },
            ScenarioA1RuleContracts.Flag("ended", "The scenario has ended"),
        ],
        ["none", "normal", "double", ContractVerdict.NotEvaluated],
        [new("instance-eliminated", "the prisoner's elimination, with the side whose attack eliminated it")],
        new("Any", "none"))
    {
        Decide = CvpVerdict,
    };

    private static ContractVerdict EscapeVerdict(ContractFacts facts)
    {
        var verdict = ScenarioA1PrisonerCalculator.Escape(new PrisonerEscapeFacts(facts.Text("prisonerId")!, facts.Text("nationality"), facts.Text("captorNationality"),
            facts.Flag("infantry")!.Value, facts.Flag("axisMinorAtHome"), facts.Flag("guardBroken")!.Value, facts.Flag("meleeInLocation")!.Value));
        if (verdict.Refusal is { } refusal)
        {
            var rule = verdict.RefusalRule!;
            var outcome = verdict.Bar switch
            {
                PrisonerEscapeBar.NotInfantry => "refused-not-infantry",
                PrisonerEscapeBar.Guard => "refused-guard",
                _ => "refused-nationality",
            };
            return new ContractVerdict(outcome, [new ContractReason(refusal, rule, Page(rule))], []);
        }

        return verdict.NtcRequired
            ? new ContractVerdict("escape-after-ntc", [new ContractReason("With no Melee in the Location, it must pass a NTC before it attacks its broken Guard", "A20.55", 88)],
                [new ContractRoll("ntc", null, "Escape NTC: the dice at most the prisoner's Morale")])
            : new ContractVerdict("escape-no-ntc", [new ContractReason("A Melee exists in the Location: it attacks its Guard with no NTC, even an unbroken one", "A20.55", 88)], []);
    }

    private static ContractVerdict KiaVerdict(ContractFacts facts) =>
        ScenarioA1PrisonerCalculator.KiaSurvivor(facts.Flag("broken")!.Value, facts.Flag("heroic")!.Value, facts.Flag("berserk")!.Value, facts.Flag("unarmed")!.Value) == KiaSurvivorEffect.Broken
            ? new ContractVerdict("broken", [new ContractReason("A unit the #KIA does not eliminate breaks", "A7.301", 55)], [])
            : new ContractVerdict("casualty-reduction", [new ContractReason("It cannot be broken (berserk, heroic, broken, or unarmed): it suffers Casualty Reduction instead", "A7.301", 55)], []);

    private static ContractVerdict CvpVerdict(ContractFacts facts)
    {
        if (facts.Number("vp") is not { } given || given < 0 || given > int.MaxValue / 2 || given != decimal.Truncate(given))
        {
            return new ContractVerdict(ContractVerdict.NotEvaluated, [new ContractReason("the fact 'vp' is not a whole number this contract reads", "A26.222", 100)], []);
        }

        var vp = (int)given;
        PrisonerCvpStatus? read = facts.Text("status") switch
        {
            "held" => PrisonerCvpStatus.Held,
            "exited-under-guard" => PrisonerCvpStatus.ExitedUnderGuard,
            "eliminated-by-own-side" => PrisonerCvpStatus.EliminatedByOwnSide,
            "eliminated-otherwise" => PrisonerCvpStatus.EliminatedOtherwise,
            "escaped" => PrisonerCvpStatus.Escaped,
            _ => null,
        };
        if (read is not { } status)
        {
            return new ContractVerdict(ContractVerdict.NotEvaluated, [new ContractReason("the fact 'status' is not a standing this contract reads", "A26.222", 100)], []);
        }

        var ended = facts.Flag("ended")!.Value;
        var cvp = ScenarioA1PrisonerCalculator.PrisonerCvp(vp, status, ended);
        var worth = string.Create(CultureInfo.InvariantCulture, $"; the captor scores {cvp} CVP");
        return status switch
        {
            PrisonerCvpStatus.Escaped => new ContractVerdict("none", [new ContractReason("An escaped prisoner counts nothing toward Victory Conditions" + worth, "A20.55", 88)], []),
            PrisonerCvpStatus.EliminatedByOwnSide => new ContractVerdict("double",
                [new ContractReason("A prisoner eliminated by an attack of its own side: the captor receives double CVP at once" + worth, "A26.222", 100)], []),
            PrisonerCvpStatus.EliminatedOtherwise => new ContractVerdict("normal",
                [new ContractReason("An eliminated prisoner grants its captor normal CVP, an escape attempt's included (A20.55)" + worth, "A26.222", 100)], []),
            _ => ended
                ? new ContractVerdict("double", [new ContractReason("Once the scenario has ended, a captured unit is worth double its normal VP" + worth, "A26.222", 100)], [])
                : new ContractVerdict("normal", [new ContractReason("During play a captured unit is worth its normal VP" + worth, "A26.222", 100)], []),
        };
    }

    private static int Page(string rule) => rule switch
    {
        "A25.63" => 97,
        "A25.82" => 98,
        "G1.621" => 280,
        _ => 88,
    };
}
