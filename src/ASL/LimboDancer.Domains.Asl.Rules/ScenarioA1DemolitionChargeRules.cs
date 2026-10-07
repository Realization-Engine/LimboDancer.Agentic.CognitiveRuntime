namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions of a Demolition Charge (S7; rulings R15.2, R15.3): its Placement's MF and Area Fire (A23.1, A23.3, A4.61), and who Throws one
/// and when (A23.6, A23.63, A7.351). The caller reads the state and the map and writes the events.
/// </summary>
public static class ScenarioA1DemolitionChargeRules
{
    /// <summary>A4.61: the Double Time MF a unit adds: two before it has spent any, else one; otherwise those it has already.</summary>
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
}
