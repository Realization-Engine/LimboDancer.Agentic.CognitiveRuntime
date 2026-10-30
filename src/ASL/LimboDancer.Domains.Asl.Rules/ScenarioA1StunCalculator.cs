namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// D5.34 Stun (p. 203) and D5.342 (p. 204), one calculator (the D5.34 claim's design, rulings RD5.2 to RD5.6): who is Stunned, when a Stun is a second
/// Stun and so a Recall, what a Stun sets, the Stun +1 on the DR the AFV makes, and the flip at the end of the Player Turn. D5.341's 1MT Recall and
/// Recall's own flip, and D5.343's SMC Temporary crew, take its verdict as a fact. Facts in, verdict out; no state.
/// </summary>
public static class ScenarioA1StunCalculator
{
    private static readonly IReadOnlyList<(VehicleCondition Condition, bool Value)> StunnedChanges =
        [(VehicleCondition.Stunned, true), (VehicleCondition.ButtonedUp, true), (VehicleCondition.Motion, false)];

    private static readonly IReadOnlyList<(UnitCondition Condition, bool Value)> FlipChanges = [(UnitCondition.Stunned, false), (UnitCondition.StunRecovery, true)];

    private static readonly IReadOnlyList<(UnitCondition Condition, bool Value)> NoChanges = [];

    /// <summary>
    /// D5.34 (p. 203): "If a CE AFV crew that qualifies for its normal CE DRM fails a MC" it is Stunned. An Unprotected crew (D5.311, p. 203) receives no
    /// (or a reduced) CE DRM and "is not susceptible to Stun/Recall". An unknown fact is no Stun.
    /// </summary>
    public static bool FailedMcStuns(bool? crewExposed, bool? qualifiesForCeDrm, bool? mcFailed) =>
        crewExposed == true && qualifiesForCeDrm == true && mcFailed == true;

    /// <summary>D5.34, A14.3 (pp. 203, 82): "if any CE crew of a fully armored AFV is attacked by a '2' Sniper dr". An unknown fact is no Stun.</summary>
    public static bool SniperStuns(bool? crewExposed, bool? fullyArmored, int? sniperDr) => crewExposed == true && fullyArmored == true && sniperDr == 2;

    /// <summary>D5.34, A9.61 (pp. 203, 64): "if a MG Final TK DR vs any (even a BU) AFV equals the Final TK#". An Original 12 is a Dud first (C7.35), the caller's.</summary>
    public static bool MachineGunStuns(int finalDr, int finalTk) => finalDr == finalTk;

    /// <summary>D5.34, B24.121 (pp. 203, 141): "if Falling Rubble lands on a CE CT AFV". An unknown fact is no Stun.</summary>
    public static bool FallingRubbleStuns(bool? crewExposed, bool? closedTopped) => crewExposed == true && closedTopped == true;

    /// <summary>Whether the result Stuns the crew, by its cause (D5.34, p. 203). An unknown fact is no Stun.</summary>
    public static bool Stuns(StunFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.Cause switch
        {
            StunCause.FailedMc => FailedMcStuns(facts.CrewExposed, facts.QualifiesForCeDrm, facts.McFailed),
            StunCause.SniperDr => SniperStuns(facts.CrewExposed, facts.FullyArmored, facts.SniperDr),
            StunCause.MachineGunToKill => facts.FinalDr is { } dr && facts.FinalTk is { } tk && MachineGunStuns(dr, tk),
            StunCause.FallingRubble => FallingRubbleStuns(facts.CrewExposed, facts.ClosedTopped),
            _ => false,
        };
    }

    /// <summary>
    /// D5.342 (p. 204; ruling RD5.3): "An AFV crew that suffers a second Stun result (e.g., while already under the effects of a '+1' counter) suffers
    /// Recall instead." By the "e.g.", a crew still Stunned (the counter's front) suffers it too. A Recall; +1 crew reads <paramref name="stunRecovery"/> true.
    /// </summary>
    public static bool SecondStun(bool stunned, bool stunRecovery) => stunned || stunRecovery;

    /// <summary>
    /// What the result does to the crew (D5.34, D5.342; rulings RD5.2, RD5.6): <see cref="StunVerdict.None"/>, <see cref="StunVerdict.Stunned"/>, or
    /// <see cref="StunVerdict.Recalled"/> for a Stun that is a second Stun. <see cref="StunVerdict.NotEvaluated"/> when a fact its cause reads is unknown,
    /// and for a Stun of a 1MT AFV (D5.341 makes it a Recall) or of a SMC Temporary crew (D5.343 Wounds him), whose rules decide it.
    /// </summary>
    public static StunVerdict Result(StunFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (Unknown(facts) is { } unknown)
        {
            return new(StunVerdict.NotEvaluated, $"the fact '{unknown}' is not known", "D5.34", 203);
        }

        if (!Stuns(facts))
        {
            return NoStun(facts);
        }

        if (facts.OneManTurret is not { } oneManTurret)
        {
            return new(StunVerdict.NotEvaluated, "the fact 'oneManTurret' is not known", "D5.341", 203);
        }

        if (oneManTurret)
        {
            return new(StunVerdict.NotEvaluated, "A Stun of a 1MT AFV is a Recall: D5.341 decides", "D5.341", 203);
        }

        if (facts.SmcTemporaryCrew is not { } smc)
        {
            return new(StunVerdict.NotEvaluated, "the fact 'smcTemporaryCrew' is not known", "D5.343", 204);
        }

        if (smc)
        {
            return new(StunVerdict.NotEvaluated, "A SMC Temporary crew is Wounded by a Stun: D5.343 decides", "D5.343", 204);
        }

        return SecondStun(facts.Stunned, facts.StunRecovery)
            ? new(StunVerdict.Recalled, "A second Stun: the crew suffers Recall instead", "D5.342", 204)
            : new(StunVerdict.Stunned, "The Inherent crew is Stunned: the AFV becomes BU and Stops, and may not fire or move this Player Turn", "D5.34", 203);
    }

    /// <summary>
    /// What a Stun sets on the AFV (D5.34, p. 203): Stunned; "The Stunned AFV and its Passengers immediately become BU if CE"; and "it immediately Stops"
    /// (Motion off).
    /// </summary>
    public static IReadOnlyList<(VehicleCondition Condition, bool Value)> StunnedConditions() => StunnedChanges;

    /// <summary>
    /// The Stun +1 (D5.34, p. 203; rulings RD5.4, RD5.5): under the "+1" counter the AFV "must add one to any TH, MG/IFE/FT IFT, CC, TC/MC, Crew Survival,
    /// or OVR DR it makes"; 0 for any other DR or dr, and 0 when not under it. The front (Stunned) side adds nothing: the AFV may not fire or move then.
    /// </summary>
    public static int Drm(StunDr dr, bool stunRecovery) =>
        stunRecovery && dr is not (StunDr.ExcessiveSpeedBreakdown or StunDr.Bog or StunDr.Other) ? 1 : 0;

    /// <summary>
    /// D5.34 (p. 203): "At the end of that Player Turn, the STUN counter is flipped over to its '+1' side": Stunned off and Stun +1 on for an active unit
    /// still Stunned; nothing otherwise. Recall's own flip is D5.341's.
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> PlayerTurnEnd(bool active, bool stunned) => active && stunned ? FlipChanges : NoChanges;

    private static string? Unknown(StunFacts facts) => facts.Cause switch
    {
        StunCause.FailedMc => facts.CrewExposed is null ? "crewExposed" : facts.QualifiesForCeDrm is null ? "qualifiesForCeDrm" : facts.McFailed is null ? "mcFailed" : null,
        StunCause.SniperDr => facts.CrewExposed is null ? "crewExposed" : facts.FullyArmored is null ? "fullyArmored" : facts.SniperDr is null ? "sniperDr" : null,
        StunCause.MachineGunToKill => facts.FinalDr is null ? "finalDr" : facts.FinalTk is null ? "finalTk" : null,
        StunCause.FallingRubble => facts.CrewExposed is null ? "crewExposed" : facts.ClosedTopped is null ? "closedTopped" : null,
        _ => "cause",
    };

    private static StunVerdict NoStun(StunFacts facts) => facts.Cause switch
    {
        StunCause.FailedMc when facts.CrewExposed != true => new(StunVerdict.None, "A BU crew takes no MC from this attack", "D5.34", 203),
        StunCause.FailedMc when facts.QualifiesForCeDrm != true => new(StunVerdict.None, "An Unprotected crew is not susceptible to Stun/Recall", "D5.311", 203),
        StunCause.FailedMc => new(StunVerdict.None, "The crew passed its MC", "D5.34", 203),
        StunCause.SniperDr when facts.CrewExposed != true || facts.FullyArmored != true =>
            new(StunVerdict.None, "Only the CE crew of a fully armored AFV is Stunned by a Sniper dr 2", "A14.3", 82),
        StunCause.SniperDr => new(StunVerdict.None, "Only a Sniper dr 2 Stuns", "A14.3", 82),
        StunCause.MachineGunToKill => new(StunVerdict.None, "The Final TK DR does not equal the Final TK#", "A9.61", 64),
        _ => new(StunVerdict.None, "Falling Rubble Stuns only a CE CT AFV", "B24.121", 141),
    };
}
