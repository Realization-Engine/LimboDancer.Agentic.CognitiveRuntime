namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The sequence of play (pass 32.i, S2): who may propose what (pass 31, ruling R31.6), the owners' options and the Massacre (rulings R5.7, R5.8), and, as the
/// later commits add them, the bars every request meets, the phase's end and what it removes and requires, and the game's start. Play reads the request and
/// the state, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1SequenceCalculator
{
    // Who may propose what (pass 31, ruling R31.6).

    /// <summary>The arguments that name the units an action is taken by; an action not listed is taken by no single side's units.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ActorArguments
    {
        get;
    } = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
    {
        ["asl.game.rout"] = ["unitId"],
        ["asl.game.deploy"] = ["squadId"],
        ["asl.game.recombine"] = ["halfSquads"],
        ["asl.game.transfer"] = ["unitId"],
        ["asl.game.drop"] = ["unitId"],
        ["asl.game.recover"] = ["unitId"],
        ["asl.game.dismantle"] = ["unitId"],
        ["asl.game.enter-empty-building"] = ["unitId"],
        ["asl.game.enter-building"] = ["unitId"],
        ["asl.game.declare-overrun"] = ["unitId"],
        ["asl.game.fire"] = ["firers"],
        ["asl.game.opportunity-fire"] = ["unitIds"],
        ["asl.game.rally"] = ["unitId"],
        ["asl.game.repair"] = ["unitId", "equipmentId"],
        ["asl.game.move"] = ["unitIds"],
        ["asl.game.end-move"] = ["unitIds"],
        ["asl.game.advance"] = ["unitIds"],
        ["asl.game.ambush-withdraw"] = ["unitIds"],
        ["asl.game.guard-prisoners"] = ["guardId"],
        ["asl.game.fire-ordnance"] = ["gunId"],
        ["asl.game.recover-shock"] = ["vehicleId"],
        ["asl.game.turn-gun"] = ["gunId"],
        ["asl.game.hook-gun"] = ["vehicleId"],
        ["asl.game.move-vehicle"] = ["vehicleId"],
        ["asl.game.overrun"] = ["vehicleId"],
        ["asl.game.button-up"] = ["vehicleId"],
        ["asl.game.massacre"] = ["unitId"],
        ["asl.game.throw-dc"] = ["unitId"],
        ["asl.game.fire-starshell"] = ["unitId"],
        ["asl.game.place-hidden"] = ["unitIds"],
        ["asl.game.mop-up"] = ["unitIds"],
    };

    /// <summary>The side that ends the present phase: either side in the RPh, RtPh, and CCPh, where both act; the DEFENDER in the DFPh; else the ATTACKER.</summary>
    public static string? PhaseEndedBy(string? phase, string proposer, string? defender, string? phasing) =>
        phase is "rph" or "rtph" or "ccph" ? proposer
            : phase == "dfph" ? defender ?? phasing
            : phasing;

    /// <summary>A view that is not one of the game's sides is not checked: the game has no side to hold it to.</summary>
    public static bool ProposerChecked(bool proposerIsSide) => proposerIsSide;

    /// <summary>
    /// Whose action a proposal is (ruling R31.6): the end of a phase is the side's that ends it, the pass the DEFENDER's, a choice the side's it was put to, a
    /// surrender the captors' side's; any other action is no one side's here. Each side is read only for its action.
    /// </summary>
    public static ProposalOwner Owner(string action, Func<string?> phaseEndedBy, string? defender, Func<string?> choiceSide, Func<string?> captorSide)
    {
        ArgumentNullException.ThrowIfNull(phaseEndedBy);
        ArgumentNullException.ThrowIfNull(choiceSide);
        ArgumentNullException.ThrowIfNull(captorSide);
        return action switch
        {
            "asl.game.advance-phase" => new ProposalOwner(phaseEndedBy(), "ends this phase"),
            "asl.game.pass-fire" => new ProposalOwner(defender, "is the DEFENDER and passes"),
            "asl.game.choose" => new ProposalOwner(choiceSide(), "answers this choice"),
            "asl.game.take-prisoner" => new ProposalOwner(captorSide(), "answers this surrender"),
            _ => new ProposalOwner(null, string.Empty),
        };
    }

    /// <summary>Ruling R31.6: the action belongs to another side's view.</summary>
    public static string? NotYourActionBar(ProposalOwner owner, string proposer)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return owner.Side is not null && owner.Side != proposer
            ? $"play.not-your-action: the {owner.Side} side {owner.What}, not the {proposer} side (ruling R31.6)"
            : null;
    }

    /// <summary>
    /// Table player, pass 31: either side ends the Rout Phase, but not while the other side still has a unit that must rout: the end would eliminate it, or
    /// make it surrender, on its opponent's word (A10.5, A20.21). <paramref name="owedSide"/> reads the side of such a unit, only for the RtPh's end.
    /// </summary>
    public static string? RoutPhaseEndBar(string action, string? phase, Func<string?> owedSide)
    {
        ArgumentNullException.ThrowIfNull(owedSide);
        return action == "asl.game.advance-phase" && phase == "rtph" && owedSide() is { } owed
            ? $"play.not-your-action: the {owed} side still has a unit that must rout, and ending the Rout Phase now would eliminate it or make it surrender; hand over to the {owed} side first (A10.5; ruling R31.6)"
            : null;
    }

    /// <summary>Ruling R31.6: a side's view does not act for another side's unit.</summary>
    public static string? NotYourUnitBar(string id, string? side, string proposer) =>
        side is not null && side != proposer ? $"play.not-your-unit: {id} belongs to the {side} side; the {proposer} side's view does not act for it (ruling R31.6)" : null;

    // The owners' options (ruling R5.8) and the Massacre (A20.4; ruling R5.7).

    /// <summary>The unit a resolution's option key names: the key is kind:subject, and the subject starts with the unit.</summary>
    public static (string Kind, string Unit) OptionKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var split = key.IndexOf(':', StringComparison.Ordinal);
        var (kind, subject) = (key[..split], key[(split + 1)..]);
        return (kind, subject.Split(':')[0]);
    }

    /// <summary>
    /// The pending choice for an option a package asks for (ruling R5.8): Battle Hardening and the Leader Creation dr are the unit's owner's, the Unlikely Kill dr
    /// the firing side's, the side other than the vehicle's; a unit no longer in the game is the phasing side's.
    /// </summary>
    public static (ChoiceKind Kind, string Side) PendingChoice(string kind, string? unitSide, string? otherSide, string phasing)
    {
        var owner = unitSide ?? phasing;
        return kind switch
        {
            "battleHardening" => (ChoiceKind.BattleHardening, owner),
            "leaderCreation" => (ChoiceKind.LeaderCreation, owner),
            _ => (ChoiceKind.UnlikelyKill, otherSide ?? phasing),
        };
    }

    /// <summary>What a pending choice asks, in words, for the Play page and the plan's reasons; <paramref name="subject"/> is the key after its kind.</summary>
    public static string DescribeChoice(ChoiceKind kind, string subject, IReadOnlyList<string> options)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(options);
        return kind switch
        {
            ChoiceKind.BattleHardening => $"{subject.Split(':')[0]} may be Battle Hardened; its owner takes it or refuses it (A15.3)",
            ChoiceKind.LeaderCreation => $"{subject} rolled an Original 2 on its Self-Rally; its side may make the Leader Creation dr or decline it (A18.11)",
            ChoiceKind.UnlikelyKill => $"an Original 2 on the Vehicle line: the firer may make the Unlikely Kill dr against {subject} or decline it (A7.309)",
            ChoiceKind.Paatc => $"a vehicle entered concealed units' Location ({subject}): their owner reveals them or takes one combined PAATC (A12.41)",
            _ => $"the acquired units are in {string.Join(" and ", options)}; the Gun's side chooses which Location keeps the Acquisition (C6.51)",
        };
    }

    /// <summary>Ruling R5.8: an answer names the pending choice; this one did not.</summary>
    public static string NoChoiceText(string key) => $"play.no-choice: '{key}' is not the pending choice";

    /// <summary>Ruling R5.8: an answer is one of the choice's options.</summary>
    public static string? OptionBar(IReadOnlyList<string> options, string option)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Contains(option, StringComparer.Ordinal) ? null : $"play.choice-option: the options are {string.Join(", ", options)}";
    }

    /// <summary>The answer in words (ruling R5.8).</summary>
    public static string ChoiceSummary(string side, string option, string described) => $"play.choice: the {side} side answers '{option}' to {described}";

    /// <summary>Ruling R5.8: a stopped resolution continues from the rolls it drew, which must all be recorded.</summary>
    public static string? ResumeBar(bool everyRollRecorded) => everyRollRecorded ? null : "play.choice-resume: a roll the resolution drew is not recorded";

    /// <summary>A20.4: the prisoner is a captured unit of the other side in the unit's Location.</summary>
    public static bool MassacreTarget(bool captured, bool sameSide, bool sameLocation) => captured && !sameSide && sameLocation;

    /// <summary>A20.4: the unit named is no such prisoner.</summary>
    public static string MassacreTargetText(string prisonerId, string unitId) => $"play.massacre-target: {prisonerId} is not a prisoner in {unitId}'s Location (A20.4)";

    /// <summary>A20.4: a Massacre is made in the unit's own fire phase: the PFPh or AFPh of its side, or the DFPh of the other.</summary>
    public static string? MassacrePhaseBar(string? phase, bool unitIsPhasing) =>
        (phase is "pfph" or "afph" ? unitIsPhasing : phase == "dfph" && !unitIsPhasing) ? null : "play.massacre-phase: a Massacre is made in the unit's own fire phase (A20.4)";

    /// <summary>A20.4: only Russian or berserk Infantry not in Melee, and not itself a prisoner, massacre prisoners.</summary>
    public static string? MassacreUnitBar(bool personnel, bool melee, bool captured, bool russian, bool berserk, string unitId) =>
        !personnel || melee || captured || !(russian || berserk)
            ? $"play.massacre-unit: only Russian or berserk Infantry not in Melee massacre prisoners (A20.4); {unitId} is not one"
            : null;

    /// <summary>A20.4 (ruling R5.7): a Massacre is made "as if using a SW", once per phase.</summary>
    public static string? MassacreOnceBar(bool massacredThisPhase, string unitId) =>
        massacredThisPhase ? $"play.massacre-once: {unitId} has already massacred a prisoner this phase; a Massacre is its SW use (A20.4, A7.351)" : null;

    /// <summary>A20.4, A7.352: a SMC forfeits its inherent FP, so it may not massacre after firing, unless it is a DFPh First Fire it keeps.</summary>
    public static string? MassacreFiredBar(bool smc, bool fired, string? phase, bool firstFire, string unitId) =>
        smc && fired && !(phase == "dfph" && firstFire)
            ? $"play.massacre-fired: {unitId} is a SMC that has already fired this phase; a Massacre forfeits its inherent FP (A20.4, A7.352)"
            : null;

    /// <summary>A20.4, A7.352: the fire marker a SMC's Massacre earns it: Final Fire in the DFPh, Prep Fire otherwise.</summary>
    public static UnitCondition MassacreMarker(string? phase) => phase == "dfph" ? UnitCondition.FinalFire : UnitCondition.PrepFire;

    /// <summary>The Massacre in words (A20.4, A20.3).</summary>
    public static string MassacreSummary(string unitId, string prisonerId, string? prisonerSide) =>
        $"play.massacre: {unitId} eliminates the prisoner {prisonerId}; the {prisonerSide} side's ELR rises by one (once) and it is faced with No Quarter (A20.4, A20.3)";

    /// <summary>A20.4 (ruling R5.7): the side whose berserk units massacre as a phase begins: the phasing side's AFPh, the other side's DFPh; no other phase.</summary>
    public static string? BerserkMassacringSide(string phase, string phasing, string? otherSide) =>
        phase == "afph" ? phasing : phase == "dfph" ? otherSide : null;

    /// <summary>A20.4 (ruling R5.7): a berserk unit not in Melee in a Location with enemy prisoners massacres them as its side's fire phase begins.</summary>
    public static bool BerserkMassacres(bool active, bool ofSide, bool berserk, bool melee, bool onMap) => active && ofSide && berserk && !melee && onMap;

    /// <summary>A20.4: a prisoner the berserk units massacre: an active captured enemy unit in their Location.</summary>
    public static bool MassacredPrisoner(bool active, bool enemy, bool captured) => active && enemy && captured;

    /// <summary>The berserk Massacre in words (A20.4).</summary>
    public static string BerserkMassacreText(IReadOnlyList<string> units, IReadOnlyList<string> prisoners, string location)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(prisoners);
        return $"play.berserk-massacre: {string.Join(", ", units)} massacre the prisoners {string.Join(", ", prisoners)} in {location} and return to normal (A20.4)";
    }
}
