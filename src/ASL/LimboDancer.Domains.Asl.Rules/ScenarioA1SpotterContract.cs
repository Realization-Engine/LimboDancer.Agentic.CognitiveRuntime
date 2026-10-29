namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contract of C9.3 (p. 179), the C9.3 claim's (ruling RC93.9): may this unit be designated the mortar's Spotter now, as
/// <see cref="ScenarioA1SpotterCalculator.DesignationRefusal"/> decides an original designation in the owner's PFPh (ASOP 2.22A) or DFPh (ASOP 4.21D).
/// A hidden Spotter is recorded as one (C9.3) and the mortar may be hidden too, so every fact about either is its owner's: the opponent's view leaves
/// them unknown and the verdict not evaluated (the claim's review, finding 3). Which unit is asked about is picked on the viewer's own map.
/// </summary>
internal static class ScenarioA1SpotterContract
{
    private static readonly string[] OwnersFacts =
    [
        "mortar", "mortarTemporarilyImmobilized", "hasDesignation", "spotterActive", "spotterSameSide", "spotterPersonnel", "spotterGoodOrder",
        "spotterCaptured", "spotterMelee", "spotterIsMortarCrew", "spotterSquad", "spotterFired", "spotterFirstFire", "spotterFiredThisPhase",
        "spotterUsedSupportWeapon", "spotterSpottedThisPhase", "sharesLocation", "spotterTemporarilyImmobilized",
    ];

    [RuleContract]
    private static RuleContract Designation() => new("c9.3-spotter-designation", "Mortar Spotter designation", [new("C9.3", 179), new("A7.352", 56)],
        [
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            ScenarioA1RuleContracts.Flag("ownerPhasing", "The mortar's owner is the phasing side"),
            Owners("mortar", "The weapon is an active mortar whose crew is on the map"),
            Owners("mortarTemporarilyImmobilized", "The mortar is Temporarily Immobilized"),
            Owners("hasDesignation", "An original designation is already recorded for this mortar"),
            // The unit is picked on the viewer's own map, where a hidden Spotter is not shown to its opponent; what is known of it is the owner's.
            new("spotterId", ContractFactType.Unit, false, ContractDisclosure.All, "The unit to be designated") { Concerns = "spotterId" },
            Owners("spotterActive", "It is active on the map"),
            Owners("spotterSameSide", "It is of the mortar's side"),
            Owners("spotterPersonnel", "It is a Personnel unit"),
            Owners("spotterGoodOrder", "It is Good Order (not broken or berserk)"),
            Owners("spotterCaptured", "It is Captured"),
            Owners("spotterMelee", "It is in Melee"),
            Owners("spotterIsMortarCrew", "It is the mortar's own crew"),
            Owners("spotterSquad", "It is a squad (A7.351)"),
            Owners("spotterFired", "It has a Prep Fire or Final Fire counter"),
            Owners("spotterFirstFire", "It is marked First Fire"),
            Owners("spotterFiredThisPhase", "It fired its inherent FP this phase, with no SW"),
            Owners("spotterUsedSupportWeapon", "It has used a SW this phase"),
            Owners("spotterSpottedThisPhase", "It has already spotted this phase"),
            Owners("spotterTemporarilyImmobilized", "It is TI (A4.8)"),
            new("hexRange", ContractFactType.Number, true, ContractDisclosure.Owner, "The hex range from the mortar to the unit") { Concerns = "spotterId" },
            Owners("sharesLocation", "Every other mortar it spots for is in this mortar's Location"),
            ScenarioA1RuleContracts.Flag("night", "The scenario is at night"),
            ScenarioA1RuleContracts.Flag("pendingDecision", "A decision or a move is still open"),
        ],
        ["may-designate", "refused", ContractVerdict.NotEvaluated],
        [
            new("mortar-spotter-designated", "the mortar and its Spotter"),
            new("conditions-changed", "the Spotter's Prep Fire counter in the PFPh, its Final Fire counter in the DFPh"),
        ],
        new("PFPh", "2.22A"))
    {
        Decide = Verdict,
    };

    private static ContractFact Owners(string name, string description, string concerns = "spotterId") =>
        ScenarioA1RuleContracts.Flag(name, description, mayBeUnknown: true, disclosure: ContractDisclosure.Owner) with
        {
            Concerns = concerns
        };

    // The replacement design, ruling RC93.16: the facts of the mortar and its lost assignment are the owner's of the unit that holds the mortar, so a
    // viewer who picks its own unit as the candidate for the enemy's mortar learns nothing of the enemy's loss (the design's review, finding 3).
    private const string Holder = "holderId";

    private static readonly string[] LossCauses = ["eliminated", "broken", "captured", "reduced", "none", "unknown"];

    private static readonly string[] ReplacementKnown =
    [
        "mortar", "hasDesignation", "openingClosed", "sharesLocation", "spotterActive", "spotterSameSide", "spotterPersonnel", "spotterGoodOrder",
        "spotterCaptured", "spotterMelee", "spotterIsMortarCrew",
    ];

    [RuleContract]
    private static RuleContract Replacement() => new("c9.3-spotter-replacement", "Mortar Spotter replacement", [new("C9.3", 179), new("ASOP 3.11A", 679)],
        [
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            ScenarioA1RuleContracts.Flag("ownerPhasing", "The mortar's owner is the phasing side"),
            // A mortar is equipment, which the map does not offer; the unit that holds it is picked instead (the C9 claim's lesson).
            new(Holder, ContractFactType.Unit, false, ContractDisclosure.All, "The unit that holds the mortar") { Concerns = Holder },
            Owners("mortar", "The weapon is an active mortar whose crew is on the map", Holder),
            Owners("hasDesignation", "A Spotter has been designated for this mortar", Holder),
            new("lossCause", ContractFactType.Choice, true, ContractDisclosure.Owner, "How its current Spotter's assignment was lost")
            {
                Choices = LossCauses, Concerns = Holder
            },
            Owners("reducedSurvivorGoodOrder", "A reduced Spotting squad left a Good Order half-squad, which keeps spotting", Holder),
            Owners("lossBeforeThisPhase", "The loss was recorded before this MPh began", Holder),
            Owners("openingClosed", "A step other than a Spotter replacement has been taken this MPh (ASOP 3.1)", Holder),
            new("hexRange", ContractFactType.Number, true, ContractDisclosure.Owner, "The hex range from the mortar to the unit") { Concerns = Holder },
            Owners("sharesLocation", "Every other mortar the unit spots for is in this mortar's Location", Holder),
            new("spotterId", ContractFactType.Unit, false, ContractDisclosure.All, "The unit to be designated") { Concerns = "spotterId" },
            Owners("spotterActive", "It is active on the map"),
            Owners("spotterSameSide", "It is of the mortar's side"),
            Owners("spotterPersonnel", "It is a Personnel unit"),
            Owners("spotterGoodOrder", "It is Good Order (not broken or berserk)"),
            Owners("spotterCaptured", "It is Captured"),
            Owners("spotterMelee", "It is in Melee"),
            Owners("spotterIsMortarCrew", "It is the mortar's own crew"),
            ScenarioA1RuleContracts.Flag("night", "The scenario is at night"),
            ScenarioA1RuleContracts.Flag("pendingDecision", "A decision or a move is still open"),
        ],
        ["may-replace", "refused", ContractVerdict.NotEvaluated],
        [new("mortar-spotter-designated", "the mortar and its new Spotter, a new assignment; no fire counter")],
        new("MPh", "3.11A"))
    {
        Decide = ReplacementVerdict,
    };

    private static ContractVerdict ReplacementVerdict(ContractFacts facts)
    {
        var cause = facts.Text("lossCause");
        // Timing reads when the loss came for the three losses of C9.3; a squad's Reduction is decided by its survivor alone.
        var lost = cause is "eliminated" or "broken" or "captured";
        var unknown = ReplacementKnown.FirstOrDefault(name => facts.Flag(name) is null)
            ?? (cause is null ? "lossCause" : null)
            ?? (lost && facts.Flag("lossBeforeThisPhase") is null ? "lossBeforeThisPhase" : null)
            ?? (facts.Number("hexRange") is not { } given || given < 0 || given > int.MaxValue || given != decimal.Truncate(given) ? "hexRange" : null);
        if (unknown is not null)
        {
            return new ContractVerdict(ContractVerdict.NotEvaluated,
                [new ContractReason($"the fact '{unknown}' is not known to this viewer, or not a whole number of hexes", "C9.3", 179)], []);
        }

        // The positions Timing reads: now is the second Player Turn, phased by the owner or its opponent; a loss before this MPh is in the first.
        var now = new TurnRulePosition(1, 2, facts.Flag("ownerPhasing")!.Value ? "owner" : "opponent", facts.Text("phase")!.ToLowerInvariant(), TurnCheckpoint.DuringPhase);
        TurnRulePosition? lossAt = !lost ? null : facts.Flag("lossBeforeThisPhase")!.Value ? new(1, 1, "opponent", "ccph", TurnCheckpoint.DuringPhase) : now;
        var change = cause switch
        {
            "eliminated" => SpotterChange.Eliminated,
            "broken" => SpotterChange.Broken,
            "captured" => SpotterChange.Captured,
            "reduced" => SpotterChange.SquadReduced,
            "none" => SpotterChange.None,
            _ => SpotterChange.Unknown,
        };
        var verdict = ScenarioA1SpotterCalculator.Replacement(new SpotterReplacementFacts(
            new SpotterTimingFacts("owner", now, facts.Flag("hasDesignation"), change, lossAt, cause == "reduced" ? facts.Flag("reducedSurvivorGoodOrder") : null),
            facts.Flag("openingClosed")!.Value ? [MovementOpeningStep.UnitMovement] : [],
            facts.Flag("mortar")!.Value,
            new SpotterCandidateFacts(facts.Flag("spotterActive")!.Value, facts.Flag("spotterSameSide")!.Value, facts.Flag("spotterPersonnel")!.Value,
                facts.Flag("spotterGoodOrder")!.Value, facts.Flag("spotterCaptured")!.Value, facts.Flag("spotterMelee")!.Value, facts.Flag("spotterIsMortarCrew")!.Value),
            (int)facts.Number("hexRange")!.Value, facts.Flag("sharesLocation"), facts.Flag("night")!.Value, facts.Flag("pendingDecision")!.Value));
        return verdict.State switch
        {
            SpotterReplacementState.MayReplace => new ContractVerdict("may-replace", [new ContractReason(verdict.Reason, "ASOP 3.11A", 679)], []),
            SpotterReplacementState.Refused => new ContractVerdict("refused", [new ContractReason(verdict.Reason, "C9.3", 179)], []),
            _ => new ContractVerdict(ContractVerdict.NotEvaluated, [new ContractReason(verdict.Reason, "C9.3", 179)], []),
        };
    }

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        // A fact this viewer may not know, or a range that is not a whole number of hexes an int holds, leaves the question open: the contract does
        // not refuse for want of a fact (the review of Wiring Prompt 001, C932: a larger whole number overflowed the cast below).
        var unknown = OwnersFacts.FirstOrDefault(name => facts.Flag(name) is null)
            ?? (facts.Number("hexRange") is not { } given || given < 0 || given > int.MaxValue || given != decimal.Truncate(given) ? "hexRange" : null);
        if (unknown is not null)
        {
            return new ContractVerdict(ContractVerdict.NotEvaluated,
                [new ContractReason($"the fact '{unknown}' is not known to this viewer, or not a whole number of hexes", "C9.3", 179)], []);
        }

        var phase = facts.Text("phase")!.ToLowerInvariant();
        var counter = facts.Flag("spotterFired")!.Value;
        var firstFire = facts.Flag("spotterFirstFire")!.Value;
        var squad = facts.Flag("spotterSquad")!.Value;
        var firedHere = facts.Flag("spotterFiredThisPhase")!.Value;

        // As the projector reads them (review finding 2): A7.352's "fired" is a fire counter or First Fire; the squad's SW use reads them apart.
        var designation = new SpotterDesignationFacts(
            new SpotterTimingFacts("owner", new TurnRulePosition(1, 1, facts.Flag("ownerPhasing")!.Value ? "owner" : "opponent", phase, TurnCheckpoint.DuringPhase),
                facts.Flag("hasDesignation"), SpotterChange.None),
            facts.Flag("mortar")!.Value,
            new SpotterCandidateFacts(facts.Flag("spotterActive")!.Value, facts.Flag("spotterSameSide")!.Value, facts.Flag("spotterPersonnel")!.Value,
                facts.Flag("spotterGoodOrder")!.Value, facts.Flag("spotterCaptured")!.Value, facts.Flag("spotterMelee")!.Value, facts.Flag("spotterIsMortarCrew")!.Value),
            (int)facts.Number("hexRange")!.Value, facts.Flag("sharesLocation"), facts.Flag("night")!.Value, facts.Flag("mortarTemporarilyImmobilized")!.Value,
            squad, counter || firstFire, facts.Flag("spotterSpottedThisPhase")!.Value,
            // "Fired this phase" is its inherent FP alone (weapon "0"): C9.31's example lets such a squad spot for one mortar.
            new SquadSupportFacts(facts.Text("spotterId")!, squad, firstFire, counter, firedHere, firedHere ? "0" : null, facts.Flag("spotterUsedSupportWeapon")!.Value),
            facts.Flag("pendingDecision")!.Value, facts.Flag("spotterTemporarilyImmobilized")!.Value);
        return ScenarioA1SpotterCalculator.DesignationRefusal(designation) is { } refusal
            ? new ContractVerdict("refused", [new ContractReason(refusal, "C9.3", 179)], [])
            : new ContractVerdict("may-designate",
                [new ContractReason($"It may be designated: a Good Order Personnel unit in reach, in its owner's {(phase == "pfph" ? "PFPh" : "DFPh")}", "C9.3", 179)], []);
    }
}
