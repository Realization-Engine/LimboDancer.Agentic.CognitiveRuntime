namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contracts of D5.34 and D5.342 (pp. 203 and 204; the D5.34 claim's design, section 1.4): what a result does to an AFV's crew, and whether
/// the AFV adds the Stun +1 to a DR, as <see cref="ScenarioA1StunCalculator"/> decides them. The attack, its rolls, the AFV's facing and armor, and its
/// counters on the map are public; under a concealed or hidden AFV's "?" its Stun counters, and whether a SMC is its Temporary crew, are its owner's, so
/// the opponent's view leaves them unknown and the verdict not evaluated.
/// </summary>
internal static class ScenarioA1StunContract
{
    private const string Vehicle = "vehicleId";

    private static readonly string[] Causes = ["failed-mc", "sniper-dr", "mg-to-kill", "falling-rubble"];

    private static readonly string[] Drs = Enum.GetNames<StunDr>();

    [RuleContract]
    private static RuleContract Stun() => new("d5.34-stun", "Stun", [new("D5.34", 203), new("D5.342", 204), new("A14.3", 82), new("A9.61", 64),
        new("D5.311", 203), new("B24.121", 141)],
        [
            new(Vehicle, ContractFactType.Unit, false, ContractDisclosure.All, "The AFV whose crew the result reaches") { Concerns = Vehicle },
            new("cause", ContractFactType.Choice, false, ContractDisclosure.All,
                "The result: a failed MC of the CE crew, a Sniper dr on it, a MG To Kill DR, or Falling Rubble")
            {
                Choices = Causes
            },
            ScenarioA1RuleContracts.Flag("crewExposed", "Its crew is CE", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("qualifiesForCeDrm", "The crew qualifies for its normal CE DRM vs this attack (not Unprotected, D5.311)", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("fullyArmored", "The AFV is fully armored (A14.3)", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("closedTopped", "The AFV is closed-topped (B24.121)", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("mcFailed", "The crew failed the MC", mayBeUnknown: true),
            new("sniperDr", ContractFactType.Number, true, ContractDisclosure.All, "The Sniper dr"),
            new("finalDr", ContractFactType.Number, true, ContractDisclosure.All, "The MG's Final TK DR"),
            new("finalTk", ContractFactType.Number, true, ContractDisclosure.All, "The MG's Final TK#"),
            ScenarioA1RuleContracts.Flag("oneManTurret", "The AFV is a 1MT AFV (D1.322), whose Stun D5.341 makes a Recall"),
            Owners("stunned", "The AFV is already Stunned this Player Turn"),
            Owners("stunRecovery", "The AFV is already under a \"+1\" counter (Stun +1, or Recall; +1)"),
            Owners("smcTemporaryCrew", "Its crew is a SMC acting as a Temporary crew (A21.22), whom D5.343 Wounds"),
        ],
        [StunVerdict.None, StunVerdict.Stunned, StunVerdict.Recalled, ContractVerdict.NotEvaluated],
        [new("conditions-changed", "the AFV's Stun, or its Recall, with BU and Motion off")],
        new("PFPh", "2.22A"))
    {
        Decide = StunVerdictOf,
    };

    [RuleContract]
    private static RuleContract PlusOne() => new("d5.34-stun-plus-one", "Stun +1", [new("D5.34", 203)],
        [
            new(Vehicle, ContractFactType.Unit, false, ContractDisclosure.All, "The AFV that makes the DR") { Concerns = Vehicle },
            new("dr", ContractFactType.Choice, false, ContractDisclosure.All, "The DR or dr it makes") { Choices = Drs },
            Owners("stunRecovery", "The AFV is under a \"+1\" counter (Stun +1, or Recall; +1)"),
        ],
        ["plus-one", "none", ContractVerdict.NotEvaluated],
        [],
        new("PFPh", "2.22A"))
    {
        Decide = PlusOneVerdict,
    };

    private static ContractFact Owners(string name, string description) =>
        ScenarioA1RuleContracts.Flag(name, description, disclosure: ContractDisclosure.Owner) with
        {
            Concerns = Vehicle
        };

    private static ContractVerdict StunVerdictOf(ContractFacts facts)
    {
        if (!Whole(facts.Number("sniperDr"), out var sniperDr) || !Whole(facts.Number("finalDr"), out var finalDr) || !Whole(facts.Number("finalTk"), out var finalTk))
        {
            return new(ContractVerdict.NotEvaluated, [new("the Sniper dr, the Final TK DR, and the Final TK# are whole numbers", "D5.34", 203)], []);
        }

        var cause = facts.Text("cause") switch
        {
            "failed-mc" => StunCause.FailedMc,
            "sniper-dr" => StunCause.SniperDr,
            "mg-to-kill" => StunCause.MachineGunToKill,
            _ => StunCause.FallingRubble,
        };
        var verdict = ScenarioA1StunCalculator.Result(new StunFacts(cause, facts.Flag("crewExposed"), facts.Flag("qualifiesForCeDrm"), facts.Flag("fullyArmored"),
            facts.Flag("closedTopped"), facts.Flag("mcFailed"), sniperDr, finalDr, finalTk, facts.Flag("stunned")!.Value, facts.Flag("stunRecovery")!.Value)
        {
            OneManTurret = facts.Flag("oneManTurret"),
            SmcTemporaryCrew = facts.Flag("smcTemporaryCrew"),
        });
        return new(verdict.Outcome, [new(verdict.Reason, verdict.Rule, verdict.Page)], []);
    }

    private static ContractVerdict PlusOneVerdict(ContractFacts facts)
    {
        var dr = Enum.Parse<StunDr>(facts.Text("dr")!);
        return ScenarioA1StunCalculator.Drm(dr, facts.Flag("stunRecovery")!.Value) > 0
            ? new("plus-one", [new("Under the \"+1\" counter the AFV adds one to its " + facts.Text("dr") + " DR", "D5.34", 203)], [])
            : new("none", [new(facts.Flag("stunRecovery")!.Value
                ? "The Stun +1 modifies only a TH, MG/IFE/FT IFT, CC, TC/MC, Crew Survival, or OVR DR"
                : "The AFV is not under a \"+1\" counter", "D5.34", 203)], []);
    }

    // A number the facts leave out is unknown (null); one given must be a whole number an int holds.
    private static bool Whole(decimal? value, out int? number)
    {
        number = null;
        if (value is not { } given)
        {
            return true;
        }

        if (given != decimal.Truncate(given) || given < int.MinValue || given > int.MaxValue)
        {
            return false;
        }

        number = (int)given;
        return true;
    }
}
