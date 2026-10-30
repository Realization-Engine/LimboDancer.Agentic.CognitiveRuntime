namespace LimboDancer.Domains.Asl.Rules;

/// <summary>The result of a Final Manhandling DR (C10.3 p. 181): below the M# the Gun enters, at it the Gun enters and stops, above it nothing moves.</summary>
public enum ManhandlingResult
{
    Enter, EnterStop, Stay
}

/// <summary>
/// The facts of a push (C10.3 p. 181; C10.2, C10.25, C10.26 p. 180; the C10.3 claim's design, rulings RC10.1, RC10.4, RC10.5): the Gun's mode and
/// state, how it moves, the last Manhandling result of this MPh (null when none), the doubled cost of the hex in half MF, and the MF left, in half MF,
/// of every Infantry that has pushed during this Player Turn, this push's pushers included (null for one not known).
/// </summary>
public sealed record GunPushFacts(bool Qsu, bool Limbered, bool Nm, bool Rfnm, bool UnhookedThisPlayerTurn, bool Bypass, bool Assault, bool MinimumMove,
    bool CarryingPp, bool BeneathWireOrPanji, bool IntoDeepStream, bool IntoBamboo, bool ViaTb, ManhandlingResult? LastResultThisMph, int EntryHalfMf,
    IReadOnlyList<int?> PushersHalfMfLeft);

/// <summary>
/// The facts of the Manhandling DRM (C10.3 p. 181 and its chart; E3.61 p. 230; E3.7332 p. 231; B9.3; B24.8 p. 143; D3.71; rulings RC10.2, RC10.3,
/// RC10.6, RC10.10): the MF the pusher expends for the hex (+Y, the doubled cost); the positive TEM of the hex entered and of the hexside crossed (+X,
/// without SMOKE, Hindrance, Height Advantage, or FFMO, which the caller leaves out); the weather; whether a road hexside is crossed and the road is paved
/// or plowed; the extra pushers; a Low Ammo counter; the Labor status (0, 1, 2). A struct, so the old member that forwards to it allocates nothing.
/// </summary>
public readonly record struct ManhandlingDrmFacts(int MfSpent, int HexTem, int HexsideTem, bool Mud, bool DeepSnow, bool RoadHexsideCrossed, bool PavedRoad,
    bool PlowedRoad, int ExtraCrewsOrHalfSquads, int ExtraSquads, bool OnlyPusherIsSquad, bool LowAmmoPlaced, int Labor);

/// <summary>
/// What follows a Manhandling result (C10.3 p. 181; B24.8 p. 143): whether the Gun and its pushers enter the hex, whether they may be pushed on or the
/// Gun hooked up this MPh (C10.31), whether the pushers gain Labor, and that they become TI once their pushing or hooking up ends.
/// </summary>
public sealed record PushOutcome(bool GunEnters, bool MayPushOn, bool MayHookUp, bool LaborGained, bool TiWhenEnded);

/// <summary>
/// The facts of a hook-up after a push (C10.31 p. 181; C10.11 p. 180; rulings RC10.4, RC10.7): whether the Gun was pushed this MPh, its last
/// Manhandling result, the MF left, in half MF, of every Infantry that has pushed during this Player Turn (null for one not known), the weather, whether
/// a paved (Mud) or plowed (Deep Snow) road hexside leaves the Gun's hex, and whether the vehicle has not exceeded the MP it needs to hook up (C10.11).
/// </summary>
public sealed record HookAfterPushFacts(bool PushedThisMph, ManhandlingResult? LastResult, IReadOnlyList<int?> PushersHalfMfLeft, bool Mud, bool DeepSnow,
    bool RoadExit, bool VehicleHasHookUpMp);
