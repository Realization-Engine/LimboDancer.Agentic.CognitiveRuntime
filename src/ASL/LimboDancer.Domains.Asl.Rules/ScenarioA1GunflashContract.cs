namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contracts of E1.8 and E1.921 (p. 226; the E1.8 claim's design, section 4.4): does this act mark a Gunflash in the Location it came from, and
/// does it open the scenario's first Starshell, as <see cref="ScenarioA1GunflashCalculator"/> decides it; and may this unit try a Starshell now, and what
/// Usage dr fires it, as <see cref="ScenarioA1Starshells"/> decides it. A Gunflash and the attack that made it are public: an attack vs an enemy Dummy
/// stack is an attack vs an enemy unit (RE1.3 as corrected 2026-10-30, by the Index's "Unit"), so no fact or outcome tells the attacker whether the "?"
/// stack it attacked was a Dummy. Whether a firer is a leader, carries an Armor Leader, sees an enemy, or where it is, and whether its force has a
/// motorized vehicle on board, are its owner's until it tries. Each contract names one Place, the PFPh (2.22A); a Gunflash is also marked, and a
/// Starshell also tried, at Defensive First Fire (3.38D) and in the DFPh (4.11D, 4.13D), and a Gunflash in the AFPh, which one Place cannot carry.
/// </summary>
internal static class ScenarioA1GunflashContract
{
    private const string Firer = "firerId";

    private static readonly string[] Causes =
        ["attack", "melee", "opportunity-fire-declared", "dismantle-or-assemble", "spotting", "radio-or-phone", "ammo-vehicle-benefit", "starshell"];

    private static readonly string[] Firers = ["leader", "ce-afv-armor-leader", "ce-afv", "mmc", "other"];

    [RuleContract]
    private static RuleContract Gunflash() => new("e1.8-gunflash", "Gunflash",
        [new("E1.8", 226), new("E1.82", 226), new("E1.89", 226), new("E1.91", 226), new("A7.25", 55), new("ASOP 5.33B", 680)],
        [
            new("cause", ContractFactType.Choice, false, ContractDisclosure.All, "What the act is") { Choices = Causes },
            ScenarioA1RuleContracts.Flag("night", "The scenario is at night"),
            ScenarioA1RuleContracts.Flag("vsEnemyUnit",
                "The attack was made at a Location holding a unit enemy to its attacker, a Dummy stack included (Index, \"Unit\"), as both sides see on the board"),
            ScenarioA1RuleContracts.Flag("firerStillInLocation", "The unit that acted is still in the Location (the answer does not depend on it)"),
        ],
        ["gunflash-opens", "gunflash", "no-gunflash-no-counter", "no-gunflash", ContractVerdict.NotEvaluated],
        [new("fire-resolved", "the attack, whose firing Location the projector marks as a Gunflash until the end of the AFPh (ASOP 5.33B)")],
        new("PFPh", "2.22A"))
    {
        Decide = GunflashVerdict,
    };

    [RuleContract]
    private static RuleContract Usage() => new("e1.921-starshell-usage", "Starshell usage",
        [new("E1.921", 226), new("E1.9", 226), new("E1.91", 226), new("E1.92", 226)],
        [
            ScenarioA1RuleContracts.Flag("night", "The scenario is at night"),
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            ScenarioA1RuleContracts.Flag("firerSidePhasing", "The firer's side is the phasing side"),
            ScenarioA1RuleContracts.Flag("attemptedFromHex", "A Starshell attempt was already made from the firer's hex this phase"),
            new(Firer, ContractFactType.Unit, true, ContractDisclosure.Owner, "The unit that tries the Starshell") { Concerns = Firer },
            new("firer", ContractFactType.Choice, false, ContractDisclosure.Owner,
                "What it is: a leader, a CE AFV with an Armor Leader, a CE AFV with none, a MMC, or other (a BU AFV among them)")
            {
                Choices = Firers,
                Concerns = Firer
            },
            Owners("goodOrder", "It is in Good Order"),
            Owners("pinned", "It is pinned"),
            Owners("ti", "It is TI"),
            Owners("captured", "It is captured"),
            Owners("stunned", "It is Stunned (an AFV)"),
            Owners("shocked", "It is Shocked (an AFV)"),
            Owners("inPillbox", "It is in a pillbox"),
            Owners("aerial", "It is Aerial"),
            Owners("interiorOrSubterranean", "It is in an Interior Building Hex or a subterranean Location", mayBeUnknown: true),
            Owners("firerHasEnemyInLos", "It has a LOS to an enemy unit (seen within NVR or Illuminated)"),
            Owners("friendlyMotorizedOnboard", "Its side has a motorized vehicle on board"),
            Owners("enemyMotorizedMovedWithin16", "An enemy motorized vehicle changed its hex or VCA within 16 hexes of it", mayBeUnknown: true),
            ScenarioA1RuleContracts.Flag("starshellFiredBefore", "A Starshell has been fired in the scenario"),
            ScenarioA1RuleContracts.Flag("firstStarshellTurnPast", "The Player Turn of the scenario's first Starshell is past"),
            ScenarioA1RuleContracts.Flag("startOfPhaseBeforeActs", "It is the start of the PFPh or the enemy MPh, before the ATTACKER fires or moves"),
            ScenarioA1RuleContracts.Flag("qualifyingGunflashOccurred", "A Gunflash has been placed by an attack vs an enemy unit"),
        ],
        ["usage-4", "usage-2", "refused", ContractVerdict.NotEvaluated],
        [new("starshell-fired", "the attempt, its Usage dr, and the placement")],
        new("PFPh", "2.22A"))
    {
        Decide = UsageVerdict,
    };

    private static ContractFact Owners(string name, string description, bool mayBeUnknown = false) =>
        ScenarioA1RuleContracts.Flag(name, description, mayBeUnknown, ContractDisclosure.Owner) with
        {
            Concerns = Firer
        };

    private static GunflashCause CauseOf(string? cause) => cause switch
    {
        "attack" => GunflashCause.Attack,
        "melee" => GunflashCause.Melee,
        "opportunity-fire-declared" => GunflashCause.OpportunityFireDeclared,
        "dismantle-or-assemble" => GunflashCause.DismantleOrAssemble,
        "spotting" => GunflashCause.Spotting,
        "radio-or-phone" => GunflashCause.RadioOrPhone,
        "ammo-vehicle-benefit" => GunflashCause.AmmoVehicleBenefit,
        _ => GunflashCause.Starshell,
    };

    private static StarshellFirer FirerOf(string? firer) => firer switch
    {
        "leader" => StarshellFirer.Leader,
        "ce-afv-armor-leader" => StarshellFirer.CeAfvWithArmorLeader,
        "ce-afv" => StarshellFirer.CeAfv,
        "mmc" => StarshellFirer.Mmc,
        _ => StarshellFirer.Other,
    };

    private static ContractVerdict GunflashVerdict(ContractFacts facts)
    {
        var night = facts.Flag("night")!.Value;
        if (!night)
        {
            return new(ContractVerdict.NotEvaluated, [new("Gunflashes are a night rule: by day a fire counter marks only the unit's fire", "E1.8", 226)], []);
        }

        var cause = CauseOf(facts.Text("cause"));
        if (ScenarioA1GunflashCalculator.PlacesGunflash(cause))
        {
            ContractReason marked = new("The Location it came from is marked by a Gunflash until the end of the AFPh, whoever is there then", "E1.8", 226);
            return ScenarioA1GunflashCalculator.OpensFirstStarshell(cause, facts.Flag("vsEnemyUnit")!.Value)
                ? new("gunflash-opens", [marked, new("It was an attack vs an enemy unit, so it opens the scenario's first Starshell", "E1.91", 226)], [])
                : new("gunflash", [marked, new("It was no attack vs an enemy unit, so it does not open the first Starshell", "E1.91", 226)], []);
        }

        return ScenarioA1GunflashCalculator.CountsAsUse(cause) && !ScenarioA1GunflashCalculator.PlacesFireCounter(cause, night)
            ? new("no-gunflash-no-counter", [new("It causes no Gunflash, and at night no Prep or Final Fire counter is placed, though the use counts", "E1.89", 226)], [])
            : new("no-gunflash", [new("Firing a Starshell causes no Gunflash", "E1.921", 226)], []);
    }

    private static ContractVerdict UsageVerdict(ContractFacts facts)
    {
        var unitId = facts.Text(Firer) ?? "the firer";
        var firer = FirerOf(facts.Text("firer"));
        var interior = facts.Flag("interiorOrSubterranean");
        var moved = facts.Flag("enemyMotorizedMovedWithin16");
        var phase = facts.Text("phase")!.ToLowerInvariant();

        // The bars in Play's order: night, the firer, the phase, one attempt per hex, the first Starshell, the timing after it (E1.9, E1.92, E1.91, E1.921).
        // A refusal the known facts already make is certain; one only an unknown fact could make leaves the question open (the A23.3 contract's rule).
        string? Refusal(bool? inside, bool? vehicleMoved) =>
            ScenarioA1Starshells.DayBar(facts.Flag("night")!.Value)
            ?? ScenarioA1Starshells.FirerRefusal(unitId, firer, facts.Flag("stunned")!.Value, facts.Flag("shocked")!.Value, facts.Flag("goodOrder")!.Value,
                facts.Flag("pinned")!.Value, facts.Flag("ti")!.Value, facts.Flag("captured")!.Value, facts.Flag("aerial")!.Value, facts.Flag("inPillbox")!.Value, inside)
            ?? ScenarioA1Starshells.PhaseBar(phase, facts.Flag("firerSidePhasing")!.Value)
            ?? ScenarioA1Starshells.OnceBar(facts.Flag("attemptedFromHex")!.Value, "its hex")
            ?? ScenarioA1Starshells.FirstStarshellRefusal(facts.Flag("starshellFiredBefore")!.Value, facts.Flag("firerHasEnemyInLos")!.Value,
                facts.Flag("friendlyMotorizedOnboard")!.Value, vehicleMoved, facts.Flag("qualifyingGunflashOccurred")!.Value)
            ?? ScenarioA1Starshells.TimingBar(facts.Flag("starshellFiredBefore")!.Value, facts.Flag("firstStarshellTurnPast")!.Value ? "earlier" : "this", "this",
                ScenarioA1Starshells.FiresAsLeader(firer), phase, !facts.Flag("startOfPhaseBeforeActs")!.Value);

        if (Refusal(interior ?? false, moved ?? true) is { } certain)
        {
            return new("refused", [new(certain, Rule(certain), 226)], []);
        }

        if (Refusal(interior, moved) is { } open)
        {
            return new(ContractVerdict.NotEvaluated, [new(open, Rule(open), 226)], []);
        }

        var need = ScenarioA1Starshells.UsageNeed(firer);
        return new(need == 4 ? "usage-4" : "usage-2",
            [new(ScenarioA1Starshells.FiresAsLeader(firer) ? "A leader, a CE Armor Leader included, fires it on a Usage dr of 4 or less"
                : "A CE AFV with no Armor Leader, or a MMC, fires it on a Usage dr of 2 or less", "E1.921", 226)],
            [new("starshell-usage", need, $"Usage dr: {need} or less fires the Starshell")]);
    }

    private static string Rule(string refusal) =>
        refusal.StartsWith("play.starshell-first", StringComparison.Ordinal) ? "E1.91"
        : refusal.StartsWith("play.starshell-day", StringComparison.Ordinal) ? "E1.9"
        : refusal.StartsWith("play.starshell-phase", StringComparison.Ordinal) || refusal.StartsWith("play.starshell-once", StringComparison.Ordinal) ? "E1.92"
        : "E1.921";
}
