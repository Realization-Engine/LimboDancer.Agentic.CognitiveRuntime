namespace LimboDancer.Domains.Asl.Rules;

/// <summary>What a #KIA does to a unit it does not eliminate (A7.301, p. 55; A20.54, p. 88; ruling RA20.2).</summary>
public enum KiaSurvivorEffect
{
    /// <summary>The unit breaks.</summary>
    Broken,

    /// <summary>The unit cannot be broken (berserk, heroic, broken, unarmed) and suffers Casualty Reduction instead (A7.302).</summary>
    CasualtyReduction,
}

/// <summary>
/// An enemy prisoner's standing as its captor's CVP reads it (A26.222, p. 100; A20.1, p. 86; A20.55, p. 88; ruling RA20.5): still held (in play, or
/// recaptured), taken off the map by its Guard, eliminated by an attack of its own side, eliminated otherwise (the captor's attack, Massacre, its Guard in
/// an escape attempt, or an attack whose side is not known), or escaped.
/// </summary>
public enum PrisonerCvpStatus
{
    Held,
    ExitedUnderGuard,
    EliminatedByOwnSide,
    EliminatedOtherwise,
    Escaped,
}

/// <summary>
/// The facts of one prisoner's escape attempt in a CCPh (A20.55, p. 88; A25.63, p. 97; A25.82, p. 98; G1.621, p. 280; rulings RA20.6, RA20.7): the
/// prisoner's id and nationality, its captor's nationality, whether it is Infantry, whether an Axis Minor prisoner is within its own national borders (the
/// card's fact; null when the card does not say, which never allows), whether its Guard is broken, and whether a Melee exists in the Location.
/// </summary>
public sealed record PrisonerEscapeFacts(string PrisonerId, string? Nationality, string? CaptorNationality, bool Infantry, bool? AxisMinorAtHome, bool GuardBroken,
    bool MeleeInLocation);

/// <summary>Which bar refuses a prisoner's escape attempt (A20.55, p. 88; A25.63, A25.82, G1.621).</summary>
public enum PrisonerEscapeBar
{
    /// <summary>The prisoner is not Infantry: only Infantry may escape (A20.55).</summary>
    NotInfantry,

    /// <summary>Its nationality does not attempt escape, or is not known (A20.55, A25.63, A25.82, G1.621).</summary>
    Nationality,

    /// <summary>Its Guard is neither broken nor in a Melee (A20.55).</summary>
    Guard,
}

/// <summary>
/// The verdict on one prisoner's escape attempt (A20.55): the refusal and the rule it rests on (null when it may attack its Guard), and whether it must
/// pass a NTC first.
/// </summary>
public sealed record PrisonerEscapeVerdict(string? Refusal, string? RefusalRule, bool NtcRequired)
{
    /// <summary>Whether the prisoner may attack its Guard this CCPh.</summary>
    public bool MayAttack => Refusal is null;

    /// <summary>Which bar refused it; null when it may attack.</summary>
    public PrisonerEscapeBar? Bar
    {
        get; init;
    }
}
