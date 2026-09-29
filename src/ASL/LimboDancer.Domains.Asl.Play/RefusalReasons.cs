namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// A reviewed package's refusal codes as a player reads them. Each code keeps its name and detail, for tests and the audit, and
/// gains a sentence with its rule, for example <c>asl.a1.fire.firer-already-fired: a firer has already fired this phase or
/// Player Turn (A7.1, A8.1, A8.4)</c>. The headline says whether the package refuses the action as proposed or only cannot
/// decide every outcome the dice can reach.
/// </summary>
public static class RefusalReasons
{
    // Codes whose name says the package cannot decide an outcome, rather than that the action is outside its reviewed cases.
    private static readonly string[] UndecidedMarks =
        ["undecided", "unreviewed", "missing", "incomplete", "unattributed", "unrecorded", "differs", "interact", "levels-differ", "concealment-mixed"];

    private static readonly Dictionary<string, string> Sentences = new(StringComparer.Ordinal)
    {
        // Fire (A7, A8, A9).
        ["fire.phase-outside"] = "this side may not fire this kind of attack in this phase (A3.2 to A3.5)",
        ["fire.residual-outside"] = "Residual FP attacks alone, at a printed counter value (A8.2, A8.22)",
        ["fire.target-outside"] = "the target Location holds no enemy unit the package reviews, is the firers' own Location, or has terrain it does not review",
        ["fire.ordnance-hit-outside"] = "an ordnance hit attacks on its Gun's HE FP column, with no firers of its own (C.6, C3.32)",
        ["fire.crew-target-unreviewed"] = "crews and the Guns they man as targets are not reviewed (C11; ruling R24.3)",
        ["fire.unit-listed-twice"] = "a unit or weapon is named twice in the attack",
        ["fire.firer-outside"] = "a firer is broken, is not a squad, half-squad, or hero, or the group's Locations are not each ADJACENT to another (A7.5)",
        ["fire.firers-of-two-sides"] = "the firers belong to two sides",
        ["fire.weapon-outside"] = "a MG, ATR, or FT is malfunctioned, not its firer's side's, already fired, or more than the firer may use; a hero fires one MG or FT (A7.35, A9.1, A15.23)",
        ["fire.flamethrower-outside"] = "a FT attacks alone, with no other firer, weapon, inherent FP, or leader, from an unpinned unit; at Long Range only through a LOS with no Hindrance, and never at a target more than two levels away (A22.31 to A22.33; ruling R15.1)",
        ["fire.mol-outside"] = "one unpinned MMC or hero per attack makes a MOL Check, in a PBF or TPBF attack that is not Subsequent First Fire or FPF, with no vehicle in the target Location; a squad adds its inherent FP and fires no SW (A22.611; ruling R15.4)",
        ["fire.demolition-charge-outside"] = "a DC is Placed and detonated in its side's AFPh, or Thrown into an ADJACENT Location in a friendly fire phase or as Defensive First Fire, by its Personnel user alone (A23.3, A23.6; rulings R15.2, R15.3)",
        ["fire.firer-already-fired"] = "a firer has already fired this phase or Player Turn (A7.1, A8.1, A8.4)",
        ["fire.fpf-outside"] = "Final Protective Fire is reviewed only undirected, at an ADJACENT or same-hex moving unit (A8.31)",
        ["fire.subsequent-first-fire-outside"] = "Subsequent First Fire is limited to Normal Range and the closest Known enemy unit (A8.3)",
        ["fire.final-fire-outside"] = "a unit marked First Fire fires again in the DFPh only at an ADJACENT or same-hex target (A8.4)",
        ["fire.director-outside"] = "the director is not a Good Order, unpinned leader of the group in its Location, or has directed already (A7.53, A7.531)",
        ["fire.out-of-range"] = "a firer or MG is beyond twice its Normal Range, or fires into its own hex",
        ["fire.los-blocked"] = "a firer has no LOS to the target (A6)",
        ["fire.levels-differ"] = "firer and target at different levels are not reviewed",
        ["fire.hindrance-unattributed"] = "the LOS Hindrance is not attributed to brush or in-season grain, which the package reviews",
        ["fire.leaders-interact"] = "several leaders among the targets, or a broken leader's Morale Level, are not reviewed",
        ["fire.elr-undecided"] = "a side's ELR is not declared, or a MMC's underscored Morale Factor is not recorded (A19.1, A19.13)",
        ["fire.movement-drm-differs"] = "a moving stack of pinned and unpinned units would need two DRM (A7.83)",
        ["fire.concealment-unreviewed"] = "fire by concealed units beyond 16 hexes, or at no Good Order target, is not reviewed",
        ["fire.reduction-counter-missing"] = "the catalog has no half-squad for a squad's Casualty Reduction (A7.302)",
        ["fire.definition-incomplete"] = "a unit's catalog definition lacks a printed value the attack needs",

        // Close Combat (A11).
        ["cc.phase-outside"] = "Close Combat is resolved only in the CCPh (A11.1)",
        ["cc.ambush-not-possible"] = "no Ambush is possible here: no Infantry advanced into woods or a building, or with or against a concealed unit, and no hidden unit was placed (A11.4)",
        ["cc.capture-outside"] = "a capture attempt is made by and against units none of which is berserk; its Guard is one of its armed attackers, and the defender's order names only its defenders (A20.2, A20.22)",
        ["cc.capture-choice-undecided"] = "a capture attempt on several units needs the defender's order of choice for the unit captured at the Kill Number (A20.22)",
        ["cc.escape-outside"] = "prisoners attack only in their own round, only a Guard that is broken or held in Melee, and every attack takes in the Guard (A20.55)",
        ["cc.prisoner-outside"] = "prisoners take no part in CC but in their escape round, and are never attacked (A20.54, A20.55)",
        ["cc.prisoner-guard-outside"] = "a prisoner's Guard is not an enemy unit in its Location (A20.5)",
        ["cc.infiltration-outside"] = "a berserk, Disrupted, or withdrawing unit does not infiltrate (A11.2, A11.22)",
        ["cc.field-promotion-bpv-missing"] = "a MMC of the attack has no BPV recorded, which Leader Creation needs (A18.2)",
        ["cc.rearm-counter-missing"] = "the catalog has no Conscript MMC to rearm an Unarmed unit as (A20.551)",
        ["cc.unit-listed-twice"] = "a unit is named twice",
        ["cc.unit-outside"] = "a named unit is not an active unit in this Location, or is not reviewed in CC",
        ["cc.concealment-unreviewed"] = "concealment in CC is not reviewed (A11.19)",
        ["cc.prisoners-unreviewed"] = "prisoners in CC are not reviewed (A20.55)",
        ["cc.overstacked-unreviewed"] = "an overstacked Location in CC is not reviewed (A5.12)",
        ["cc.round-outside"] = "after an Ambush the ambusher's attacks come first, and only then the ambushed side's round (A11.3, A11.32)",
        ["cc.stacking-outside"] = "a SMC must be stacked with an MMC of its side in the Location (A11.14)",
        ["cc.attack-outside"] = "an attack must name attackers of one side and defenders of the other",
        ["cc.broken-attacker"] = "a broken unit never attacks in CC (A11.16)",
        ["cc.attacked-twice"] = "no unit attacks or is attacked more than once per CCPh (A11.12)",
        ["cc.withdrawing-attacker"] = "a withdrawing unit makes no CC attack (A11.2)",
        ["cc.director-outside"] = "the director is not a Good Order, unpinned, unberserk leader among the attackers (A11.141)",
        ["cc.withdrawal-outside"] = "the withdrawal is not to an ADJACENT Location the unit could advance into with no enemy unit, or the unit may not withdraw (A11.2)",
        ["cc.reinforcement-must-attack"] = "units that advanced into an existing Melee must attack",
        ["cc.berserk-must-attack"] = "a berserk unit attacks in its side's round (A15.43; ruling R29.15)",
        ["cc.definition-incomplete"] = "a unit's catalog definition lacks a printed value CC needs",
        ["cc.reduction-counter-missing"] = "the catalog has no half-squad for a squad's Casualty Reduction (A7.302, A11.11)",
        ["cc.field-promotion-base-undecided"] = "which MMC a Field Promotion would come from is not declared (A18.12)",
        ["cc.field-promotion-commissar-unreviewed"] = "a Field Promotion that may create a Commissar is not reviewed (A25.25)",
        ["cc.field-promotion-stacking-undecided"] = "which MMC a created leader would defend with is not declared (A18.12)",
        ["cc.leader-creation-not-applicable"] = "no leader can be created here (A18.12)",
        ["cc.odds-above-10-to-1-undecided"] = "odds above 10-1 are not reviewed (A11.11)",

        // Ordnance (C).
        ["ordnance.phase-outside"] = "a Gun fires only in its side's PFPh, AFPh, or DFPh here (ruling R24.2)",
        ["ordnance.gun-outside"] = "the Gun is not one the package reviews",
        ["ordnance.gun-malfunctioned"] = "the Gun is malfunctioned (C2.28)",
        ["ordnance.crew-outside"] = "the Gun is not manned by a crew of its side able to fire it (A21.13)",
        ["ordnance.gun-already-fired"] = "the Gun has fired and did not keep its Multiple ROF (C2.24)",
        ["ordnance.out-of-range"] = "the target is beyond the Gun's range or in its own hex (C2.25, C5.5)",
        ["ordnance.fact-outside"] = "a fact of the shot is outside the reviewed cases",
        ["ordnance.covered-arc-fixed"] = "the Gun may not turn to bring the target into its Covered Arc (C5.11)",
        ["ordnance.los-blocked"] = "the Gun has no LOS to the target (A6)",
        ["ordnance.target-outside"] = "the target Location holds no in-LOS enemy unit on the Infantry Target Type (C3.32)",
        ["ordnance.levels-differ"] = "Gun and target at different levels are not reviewed (C2.6)",
        ["ordnance.hindrance-unattributed"] = "the LOS Hindrance is not attributed to terrain the package reviews",
        ["ordnance.concealment-mixed"] = "concealed and unconcealed targets together are not reviewed",
        ["ordnance.concealment-unreviewed"] = "fire by or at concealed units is decided only when a Good Order unit of the firing side is in range",

        // Rally (A10).
        ["rally.phase-outside"] = "a unit rallies only in its side's RPh (A10.6)",
        ["rally.unit-outside"] = "only a broken unit of the phasing side rallies (A10.6)",
        ["rally.already-attempted"] = "each unit attempts to rally once per Player Turn (A10.6)",
        ["rally.other-action"] = "the unit has already taken another RPh action (A3.1)",
        ["rally.terrain-outside"] = "the unit's terrain is not one the package reviews (A10.61)",
        ["rally.leader-outside"] = "the leader is not a leader of the unit's side in its Location (A10.7)",
        ["rally.self-rally-with-leader-present"] = "a unit may not Self-Rally with a Good Order friendly leader present (A10.63)",
        ["rally.disrupted-self-rally"] = "a Disrupted unit may not Self-Rally (A19.12)",
        ["rally.self-rally-not-capable"] = "this unit may not Self-Rally (A10.63)",
        ["rally.self-rally-capability-unrecorded"] = "the catalog does not record whether this unit may Self-Rally",
        ["rally.field-promotion-commissar-unreviewed"] = "a Field Promotion that may create a Commissar is not reviewed (A25.25)",
        ["rally.reduction-counter-missing"] = "the catalog has no counter this result needs",
        ["rally.definition-incomplete"] = "a unit's catalog definition lacks a printed value the rally needs",

        // Heat of Battle (A15).
        ["hob.nationality-unreviewed"] = "this nationality's Heat of Battle DRM is not reviewed (A15.1)",
        ["hob.inexperience-undecided"] = "whether the unit is Inexperienced is not declared (A15.1, A19.2)",
        ["hob.known-enemy-in-los-undecided"] = "whether a Known enemy unit is in the unit's LOS is not read (A15.44)",
        ["hob.captors-undecided"] = "which ADJACENT enemy units could take a surrender is not read (A15.5)",
        ["hob.hero-counter-missing"] = "the catalog has no hero for this side (A15.2)",
        ["hob.hardening-counter-missing"] = "the catalog has no Battle Hardened counter for this unit (A15.3)",
        ["hob.hardening-unreviewed"] = "Battle Hardening of this unit is not reviewed (A15.3)",
    };

    /// <summary>
    /// The refusal for a package's pre-check: a headline naming the package and the action, then each code with its sentence.
    /// </summary>
    public static string[] Refusal(string reason, string package, string action, IReadOnlyList<string> codes)
    {
        ArgumentNullException.ThrowIfNull(codes);
        var headline = codes.All(Undecided)
            ? $"{reason}: the {package} package does not decide every outcome of this {action}"
            : $"{reason}: the {package} package refuses this {action} as proposed";
        return [headline, .. codes.Select(Explain)];
    }

    /// <summary>A code with its sentence, or the code alone when it has none (a fact or roll the planner failed to supply).</summary>
    public static string Explain(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var name = Name(code);
        if (Sentences.TryGetValue(name, out var sentence))
        {
            return $"{code}: {sentence}";
        }

        return name.EndsWith(".fact-missing", StringComparison.Ordinal) ? $"{code}: the game did not supply a fact the package needs"
            : name.EndsWith(".roll-missing", StringComparison.Ordinal) || name.EndsWith(".roll-malformed", StringComparison.Ordinal)
                || name.EndsWith(".extra-roll", StringComparison.Ordinal) ? $"{code}: the rolls do not match what the package asked for"
            : code;
    }

    /// <summary>Whether a code says only that an outcome is undecided, not that the action is outside the reviewed cases.</summary>
    public static bool Undecided(string code) => Name(code) is var name && UndecidedMarks.Any(mark => name.Contains(mark, StringComparison.Ordinal));

    // "asl.a1.fire.firer-already-fired:detail" -> "fire.firer-already-fired".
    private static string Name(string code)
    {
        var bare = code.Split(':', 2)[0];
        return bare.StartsWith("asl.a1.", StringComparison.Ordinal) ? bare["asl.a1.".Length..] : bare;
    }
}
