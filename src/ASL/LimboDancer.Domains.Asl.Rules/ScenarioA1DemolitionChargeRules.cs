namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions of a Demolition Charge (S7; rulings R15.2, R15.3): its Placement's MF and Area Fire (A23.1, A23.3, A4.61), and who Throws one
/// and when (A23.6, A23.63, A7.351). The caller reads the state and the map and writes the events.
/// </summary>
public static class ScenarioA1DemolitionChargeRules
{
    /// <summary>A4.5 (Double Time; the A23.3 claim corrected the citation): the MF a unit adds: two before it has spent any, else one; otherwise those it has already.</summary>
    public static int DoubleTimeMf(bool doubleTime, int mfSpent, bool halfMfSpent, int already) =>
        doubleTime ? (mfSpent == 0 && !halfMfSpent ? 2 : 1) : already;

    /// <summary>
    /// A23.3, A4.61: a unit that has too few MF left for the Placement, in half MF, or Assault Moving with no MF beyond it, Places no DC; returns the
    /// half MF left, or null when it has enough.
    /// </summary>
    public static int? PlacementShort(int allowance, int plain, int mfSpent, bool halfMfSpent, int halfMf, bool assault)
    {
        var spent = (mfSpent * 2) + (halfMfSpent ? 1 : 0);
        var left = (allowance * 2) - spent;
        return left < halfMf || (assault && (plain * 2) - spent <= halfMf) ? left : null;
    }

    /// <summary>A23.1 (ruling R15.2): the attack is Area Fire when every unit it will attack is concealed, hidden, or a Dummy.</summary>
    public static bool AllConcealed(IReadOnlyList<bool> concealed) => concealed.Count > 0 && concealed.All(item => item);

    /// <summary>
    /// A23.6, A23.63 (ruling R15.3): a DC is Thrown in a friendly fire phase, or as Defensive First Fire at the moving stack's Location, never by a
    /// unit marked First Fire (nor Final Fire in the MPh); the window and markers are read lazily.
    /// </summary>
    public static bool ThrowPhase(string? phase, bool phasing, Func<bool> firstFire, Func<bool> windowAtTarget, Func<bool> finalFire) => phase switch
    {
        "pfph" or "afph" => phasing,
        "dfph" => !phasing && !firstFire(),
        "mph" => !phasing && windowAtTarget() && !firstFire() && !finalFire(),
        _ => false,
    };

    /// <summary>A7.351: a unit that fired this phase Throws no DC, unless in the MPh, or a squad that fired its inherent FP alone (read lazily).</summary>
    public static bool FiredRefusal(bool fired, string? phase, string kind, Func<bool> inherentOnly) =>
        fired && phase != "mph" && !(kind == "asl:squad" && inherentOnly());

    // The A23.3 claim (its design, RA23.1 to RA23.10): A23.3 Placement and A23.4 Detonation, the Placed DC.

    /// <summary>
    /// A23.3 (p. 90; RA23.2): a Placement costs the MF the unit would need to enter the target Location directly during this MPh without Bypass,
    /// gathered as a fact; <see cref="PlacementShort"/> compares it with the MF left.
    /// </summary>
    public static int PlacementCost(int directEntryMf)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(directEntryMf);
        return directEntryMf;
    }

    /// <summary>
    /// A23.3 (RA23.3): why this unit may not Place its DC there now, or null. A carrying Infantry unit, in the MPh, at an ADJACENT target Location in its
    /// LOS (a Cave, G11.8331, is not built), not fired during the PFPh, not pinned before the Placement this MPh. An unknown fact is no permission. Other
    /// rules' bars (A15.431, A3.3, A7.25, A22.3, A23.61, B26.45, G9.211) are their own calculators'.
    /// </summary>
    public static string? PlacementRefusal(DcPlacementFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return !facts.CarryingDc || !facts.Infantry ? "play.dc-place: only a carrying Infantry unit Places a DC (A23.3)"
            : facts.Phase != "mph" ? "play.dc-place: a DC is Placed during the MPh (A23.3)"
            : facts.Adjacent != true ? "play.dc-place: a DC is Placed only on an ADJACENT Location (A23.3)"
            : facts.InLos != true ? "play.dc-place: a DC is not Placed to a Location out of the unit's LOS (A23.3, A7.21)"
            : facts.FiredInPrepFire != false ? "play.dc-place: a unit that fired during the PFPh Places no DC (A23.3)"
            : facts.PinnedThisMph != false ? "play.dc-place: a unit pinned before the Placement during this MPh Places no DC (A23.3)"
            : null;
    }

    /// <summary>
    /// A23.3, A11.6 (p. 75; RA23.4): a manned, unconcealed enemy AFV in the target Location needs a PAATC of a placer A11.6 does not exempt (an SMC, a
    /// Fanatic, a Berserk unit).
    /// </summary>
    public static bool PlacementNeedsPaatc(bool enemyAfvInTarget, bool placerExempt) => enemyAfvInTarget && !placerExempt;

    /// <summary>
    /// A23.3, A8.1 (RA23.5): whether the DC is operably Placed. Null until settled: the placer leaves the Placement Location, another unit begins movement,
    /// or the MPh ends (until then the placer stays open to Defensive First Fire, Subsequent First Fire, and FPF there). Then operable unless the placer was
    /// broken, pinned, or eliminated before it; a newly berserk placer is fine. Capture cannot come before (A20.2) and has no effect after.
    /// </summary>
    public static bool? PlacementOperable(bool settled, bool brokenPinnedOrEliminated) => settled ? !brokenPinnedOrEliminated : null;

    /// <summary>A23.3, A20.24 (RA23.5): a DC not operably Placed stays with its unit, or in the unit's Location if it was eliminated or captured.</summary>
    public static DcHeldBy UnplacedDcStays(bool eliminatedOrCaptured) => eliminatedOrCaptured ? DcHeldBy.Location : DcHeldBy.Unit;

    /// <summary>
    /// A23.4 (RA23.6): a CX Placing or Throwing unit adds +1 to the DC's attack DR, or to its Position DR (C7.346). CX at Placement is the placer's status at
    /// that moment, from any cause; not a Double Time declared after it, nor a CX from an earlier turn (A4.51).
    /// </summary>
    public static int DcCxDrm(bool cxAtUse) => cxAtUse ? 1 : 0;

    /// <summary>
    /// A23.4, ASOP 5.22A (RA23.7): an operably Placed DC detonates in its owner's AFPh of the Player Turn it was Placed; the detonation is mandatory, with
    /// no carry-over.
    /// </summary>
    public static bool DetonationWindow(string? phase, bool ownerPhasing, bool operable, bool placedThisPlayerTurn) =>
        phase == "afph" && ownerPhasing && operable && placedThisPlayerTurn;

    /// <summary>
    /// A23.4 (RA23.7): an Original DR at or above the DC's removal number (A23.2's, the fire calculator's AssaultWeaponRemoval) removes it without
    /// detonation; under it, it detonates. For a Placed or a Thrown DC; a Thrown DC's second DR is A23.6's.
    /// </summary>
    public static bool DcMalfunction(int originalDr, int removalNumber) => originalDr >= removalNumber;

    /// <summary>A23.4 (RA23.7): a DC is removed from play after its attack, whether it detonated or not.</summary>
    public static bool RemovedAfterAttack => true;
}
