namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The fire counter a unit or weapon carries (A8.4, A8.41, p. 61): none, First Fire, Final Fire, Intensive Fire (C5.6), or No Fire (C5.64; ruling
/// RA8.4: a marker value no caller gives yet). Each weapon carries its own, and a unit's own counter is for its inherent FP (ruling RA8.2).
/// </summary>
public enum FireMarker
{
    None, FirstFire, FinalFire, IntensiveFire, NoFire
}

/// <summary>
/// What fires in Final Fire (A8.4, A8.41, A9.3, C2.241, C2.29): a unit's inherent FP; a MG; a Gun firing as ordnance; an ordnance SW (a light mortar,
/// an ATR, a LATW); a Gun firing IFE; or another SW that can use neither Sustained nor Intensive Fire (a FT, a DC; ruling RA8.8).
/// </summary>
public enum FinalFireWeapon
{
    Inherent, Mg, Gun, OrdnanceSw, Ife, OtherSw
}

/// <summary>
/// What Final Fire allows (A8.4, A8.41): fire as it would at any target, its Multiple ROF kept (Normal); one more attack, at an ADJACENT or same-hex
/// target only (OnceMore); or none.
/// </summary>
public enum FinalFireUse
{
    Normal, OnceMore, None
}

/// <summary>
/// The facts of one unit's or weapon's Final Fire (the A8.41 claim's design, section 3): what fires, its own counter, the range to the target (null
/// when not known), and the facts A9.3 and C5.6 read: a MG fired by a lone SMC, a vehicular MG and whether it is the MA, a MG firing as ordnance, a
/// Gun that may not Intensive Fire ("No IF", C5.63), and a Gun's crew neither pinned, shocked, nor stunned (C5.6).
/// </summary>
public sealed record FinalFireFacts(FinalFireWeapon Weapon, FireMarker Marker, int? Range, bool LoneSmc, bool Vehicular, bool MainArmament, bool AsOrdnance,
    bool NoIntensiveFire, bool CrewFit);

/// <summary>
/// The verdict of one Final Fire (ruling RA8.1): the use; whether it is Area Fire (every OnceMore attack, A8.4); Sustained Fire (a MG or IFE,
/// A9.3, C2.29); Intensive Fire (a Gun, C2.241); the B# lowering of Sustained Fire (A9.3; a Gun's C5.62 lowering stays the ordnance calculator's);
/// and the refusal, null when it may fire.
/// </summary>
public sealed record FinalFireVerdict(FinalFireUse Use, bool AreaFire, bool Sustained, bool Intensive, int BreakdownReduction, string? Refusal);
