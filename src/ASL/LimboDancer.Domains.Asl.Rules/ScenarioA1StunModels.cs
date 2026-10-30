namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The four results that Stun an AFV's Inherent crew (D5.34, p. 203; the D5.34 claim's design, ruling RD5.2): a CE crew that qualifies for its normal CE
/// DRM fails a MC; the CE crew of a fully armored AFV is attacked by a Sniper dr 2 (A14.3, p. 82); a MG's Final TK DR equals the Final TK#, even against
/// a BU AFV (A9.61, p. 64); Falling Rubble lands on a CE CT AFV (B24.121, p. 141).
/// </summary>
public enum StunCause
{
    FailedMc,
    SniperDr,
    MachineGunToKill,
    FallingRubble,
}

/// <summary>
/// A DR or dr an AFV makes, as D5.34 (p. 203; ruling RD5.5) sorts them for the Stun +1: "any TH, MG/IFE/FT IFT, CC, TC/MC, Crew Survival, or OVR DR it
/// makes" takes it, and nothing else does (an ESB DR, a Bog DR, any dr).
/// </summary>
public enum StunDr
{
    ToHit,
    MachineGunIft,
    IfeIft,
    FlameThrowerIft,
    CloseCombat,
    TaskCheck,
    MoraleCheck,
    CrewSurvival,
    Overrun,
    ExcessiveSpeedBreakdown,
    Bog,
    Other,
}

/// <summary>
/// The facts of one result that may Stun an AFV's crew (D5.34, D5.342; ruling RD5.2). Each cause reads its own: <see cref="StunCause.FailedMc"/> the
/// crew's CE, its normal CE DRM (an Unprotected crew, D5.311, has none), and the failed MC; <see cref="StunCause.SniperDr"/> the CE, the fully armored
/// AFV, and the Sniper dr; <see cref="StunCause.MachineGunToKill"/> the Final TK DR and the Final TK#; <see cref="StunCause.FallingRubble"/> the CE and
/// the closed top. <see cref="Stunned"/> and <see cref="StunRecovery"/> are the counter the AFV already carries, its front or its "+1" side (a Recall;
/// +1 crew reads <see cref="StunRecovery"/> true, as the state keeps it). Null is unknown, and an unknown fact the cause reads never Stuns.
/// </summary>
public sealed record StunFacts(StunCause Cause, bool? CrewExposed, bool? QualifiesForCeDrm, bool? FullyArmored, bool? ClosedTopped, bool? McFailed,
    int? SniperDr, int? FinalDr, int? FinalTk, bool Stunned, bool StunRecovery)
{
    /// <summary>Whether the AFV is a 1MT AFV (D1.322), whose Stun D5.341 turns into a Recall (ruling RD5.6). Null is unknown.</summary>
    public bool? OneManTurret
    {
        get; init;
    }

    /// <summary>Whether its crew is a SMC acting as a Temporary crew (A21.22), whom D5.343 Wounds for a Stun (ruling RD5.6). Null is unknown.</summary>
    public bool? SmcTemporaryCrew
    {
        get; init;
    }
}

/// <summary>
/// What a result does to an AFV's crew under D5.34 and D5.342 (ruling RD5.2): <see cref="None"/>, <see cref="Stunned"/>, <see cref="Recalled"/> (a Stun
/// that is a second Stun), or <see cref="NotEvaluated"/> (a fact the cause reads is unknown, or D5.341 or D5.343 decides the result), with its reason and
/// the rule and page it rests on.
/// </summary>
public sealed record StunVerdict(string Outcome, string Reason, string Rule, int Page)
{
    public const string None = "none";
    public const string Stunned = "stunned";
    public const string Recalled = "recalled";
    public const string NotEvaluated = "not-evaluated";
}
