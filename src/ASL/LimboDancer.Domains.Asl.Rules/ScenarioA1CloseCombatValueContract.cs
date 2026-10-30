using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contract of A11.5 (p. 74; D7.213, p. 208; the A11.5 claim's design, section 5.4): what does this Final DR do to the vehicle, with these
/// attackers, as <see cref="ScenarioA1CloseCombatValueCalculator"/> decides it. In CC every concealed unit reveals its Strength Factor (A11.19), so the
/// attacker's kind, class, pin, fire counter, and state are All; whether it is an Assault Engineer and its national case are a Roster entry or a
/// designation, its owner's, concerning the attacker. Asked in the CCPh and in D7.21's CC Reaction Fire in the MPh.
/// </summary>
internal static class ScenarioA1CloseCombatValueContract
{
    private static readonly string[] Kinds = ["squad", "crew", "half-squad", "smc"];

    private static readonly string[] Nationals = ["none", "ethiopian", "t-h-hero", "tacp"];

    [RuleContract]
    private static RuleContract CcvVsVehicle() => new("a11.5-ccv-vs-vehicle", "CC vs a vehicle: the CCV",
        [new("A11.5", 74), new("A11.51", 74), new("D7.213", 208), new("A20.5", 87), new("E6.2", 235)],
        [
            new("attackerId", ContractFactType.Unit, false, ContractDisclosure.All, "The main attacking unit") { Concerns = "attackerId" },
            new("attackerKind", ContractFactType.Choice, false, ContractDisclosure.All, "Its kind, revealed in CC (A11.19)") { Choices = Kinds },
            ScenarioA1RuleContracts.Flag("inexperienced", "It is Inexperienced (A19.34)"),
            ScenarioA1RuleContracts.Flag("smcCombining", "A SMC combines with it in the attack"),
            ScenarioA1RuleContracts.Flag("pinned", "An attacker is pinned (A7.8)"),
            ScenarioA1RuleContracts.Flag("reactionFireMarked", "The attack is CC Reaction Fire by a unit marked First or Final Fire (D7.213)"),
            ScenarioA1RuleContracts.Flag("vehicleConcealed", "The vehicle is concealed (A11.19)"),
            ScenarioA1RuleContracts.Flag("guard", "An attacker is a Guard attacking non-prisoners, regardless of US# (A20.52)", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("guardOutnumbered", "Legacy input: an attacker is an outnumbered Guard", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("csRed", "The vehicle's CS# is printed red (D5.7)"),
            new("finalDr", ContractFactType.Number, false, ContractDisclosure.All, "The Final CC DR"),
            ScenarioA1RuleContracts.Flag("unarmed", "An attacker is Unarmed (A20.5)"),
            ScenarioA1RuleContracts.Flag("berserk", "The main attacking unit is berserk (A15.42)"),
            ScenarioA1RuleContracts.Flag("swimming", "An attacker is swimming (E6.2)"),
            ScenarioA1RuleContracts.Flag("assaultEngineer", "It is an Assault Engineer (A25.01, H1.22)", mayBeUnknown: true, disclosure: ContractDisclosure.Owner) with
            {
                Concerns = "attackerId"
            },
            new("nationalCcv", ContractFactType.Choice, true, ContractDisclosure.Owner, "Its national CCV case (A25.934, G1.4231, KW 9.11)")
            {
                Choices = Nationals,
                Concerns = "attackerId"
            },
        ],
        ["burning-wreck", "eliminated", "immobilized", "no-effect", "refused", ContractVerdict.NotEvaluated],
        [new("close-combat-resolved", "the attack on the vehicle, its CCV and its result, and the PRC eliminated with a destroyed vehicle")],
        new("CCPh", "8.21B"))
    {
        Decide = Verdict,
    };

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        // A refusal the public facts make is certain, whatever the owner's facts are.
        if (ScenarioA1CloseCombatValueCalculator.AttackerRefusal(true, true, false, false, facts.Flag("unarmed")!.Value, facts.Flag("swimming")!.Value) is { } refusal)
        {
            return new("refused", [new(refusal, facts.Flag("unarmed")!.Value ? "A20.5" : "E6.2", facts.Flag("unarmed")!.Value ? 87 : 235)], []);
        }

        if (facts.Flag("assaultEngineer") is not { } engineer || facts.Text("nationalCcv") is not { } national)
        {
            return new(ContractVerdict.NotEvaluated, [new("whether the attacker is an Assault Engineer, and its national case, are its owner's", "A11.5", 74)], []);
        }

        if (facts.Number("finalDr") is not { } dr || dr != decimal.Truncate(dr) || dr is < -20 or > 30)
        {
            return new(ContractVerdict.NotEvaluated, [new("the Final DR is a whole number", "A11.5", 74)], []);
        }

        var guard = facts.Flag("guard");
        var legacyGuard = facts.Flag("guardOutnumbered");
        if (guard is null && legacyGuard is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("whether an attacker is a Guard is not known", "A20.52", 87)], []);
        }

        var kind = facts.Text("attackerKind") switch
        {
            "squad" => "asl:squad",
            "crew" => "asl:crew",
            "half-squad" => "asl:half-squad",
            _ => "asl:leader",
        };
        var ccv = ScenarioA1CloseCombatValueCalculator.AttackCcv(new CcvAttackFacts(facts.Text("attackerId")!, kind, engineer, facts.Flag("inexperienced")!.Value,
            facts.Flag("smcCombining")!.Value ? "smc" : null, null, facts.Flag("pinned")!.Value, facts.Flag("reactionFireMarked")!.Value,
            facts.Flag("vehicleConcealed")!.Value, legacyGuard == true, NationalOf(national))
        {
            Guard = guard == true
        });
        var result = ScenarioA1CloseCombatValueCalculator.VehicleResult((int)dr, ccv.Ccv, facts.Flag("csRed")!.Value);
        var reasons = new List<ContractReason>
        {
            new(string.Create(CultureInfo.InvariantCulture, $"CCV {ccv.Ccv}: base {ccv.Base}")
                + string.Concat(ccv.Modifiers.Select(item => string.Create(CultureInfo.InvariantCulture, $", {item.Name} {item.Value:+0;-0}"))), "A11.5", 74),
        };
        if (ScenarioA1CloseCombatValueCalculator.PrcEliminated(result))
        {
            reasons.Add(new("The vehicle's PRC are eliminated with it", "A11.5", 74));
        }

        if (facts.Flag("smcCombining")!.Value && !ScenarioA1CloseCombatValueCalculator.LeaderDrmApplies(true, facts.Flag("berserk")!.Value))
        {
            reasons.Add(new("A berserk unit receives no leader's DRM, so the Final DR holds none (A15.42)", "A11.51", 74));
        }

        return new(result == VehicleCloseCombatResolution.None ? "no-effect" : result, reasons, []);
    }

    private static NationalCcv NationalOf(string national) => national switch
    {
        "ethiopian" => NationalCcv.Ethiopian,
        "t-h-hero" => NationalCcv.TankHunterHero,
        "tacp" => NationalCcv.Tacp,
        _ => NationalCcv.None,
    };
}
