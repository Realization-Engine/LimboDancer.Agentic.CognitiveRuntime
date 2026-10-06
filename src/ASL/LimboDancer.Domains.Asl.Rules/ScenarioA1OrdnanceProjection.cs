namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions the projector makes as it replays a Gun's shot and the Gun and RPh records (S7): the fire phases and the Multiple ROF
/// (C2.24, C3.3), Special Ammunition depletion (C8.9, ruling R7.6), the PF usage count (C13.31, ruling R9.7), a Gun's change of CA (C3.22,
/// ruling R8.9), hooking up (C10.11, C10.12), and the Deployment and Recovery checks (A1.31, A4.44). The projector reads the state and writes it.
/// </summary>
public static class ScenarioA1OrdnanceProjection
{
    /// <summary>C3.3: a Gun fires in the PFPh, AFPh, DFPh, or MPh.</summary>
    public static bool FirePhase(string? phase) => phase is "pfph" or "afph" or "dfph" or "mph";

    /// <summary>C13.3 (ruling R9.7): a PF is named "&lt;unit&gt;:pf" for the unit making the check.</summary>
    public static bool IsPanzerfaust(string gun, string crew) => gun == crew + ":pf";

    /// <summary>C2.24, C5.6: a Gun that fired this phase fires again only on a kept Multiple ROF, as a PF, or by Intensive Fire; the last is read lazily.</summary>
    public static bool FiresAgain(bool firedBefore, bool rateOfFireKept, bool panzerfaust, Func<bool> intensiveFire) =>
        !(firedBefore && !rateOfFireKept && !panzerfaust && !intensiveFire());

    /// <summary>C8.9 (ruling R7.6): Special Ammunition used at or above its Depletion Number is used up.</summary>
    public static bool AmmunitionDepleted(string? use) => use is "depleted" or "none";

    /// <summary>C8.9: a shot with Special Ammunition the Gun did not have was never fired, unless the Gun malfunctioned.</summary>
    public static bool NeverFired(string? use, bool malfunctioned) => use == "none" && !malfunctioned;

    /// <summary>A7.352: a crew firing its Gun loses its inherent FP; an AFV's MA and a PF have no such crew.</summary>
    public static bool CrewLosesInherentFp(bool tank, bool panzerfaust, bool recorded) => !(tank || panzerfaust || recorded);

    /// <summary>C13.31 (ruling R9.7): the half-squad equivalents of a unit in its side's OB.</summary>
    public static int HalfSquadEquivalents(string kind) => kind == "asl:squad" ? 2 : kind is "asl:half-squad" or "asl:crew" ? 1 : 0;

    /// <summary>
    /// C3.22 (ruling R8.9): a Gun changes its CA in its side's friendly fire phase while its Good Order, unpinned crew could still fire it; the
    /// crew's and the Gun's conditions are read lazily, in that order.
    /// </summary>
    public static bool TurnGunAllowed(string? phase, bool crewPhasing, Func<bool> crewBrokenOrPinned, Func<bool> gunMarked, bool firedBefore, bool rateOfFireKept) =>
        !(phase is not ("pfph" or "afph" or "dfph") || crewPhasing != (phase != "dfph")
            || crewBrokenOrPinned()
            || (gunMarked() && !rateOfFireKept)
            || (firedBefore && !rateOfFireKept));

    /// <summary>C3.22: in the PFPh neither the turned Gun nor its crew moves that Player Turn.</summary>
    public static bool TurnFreezesMovement(string? phase) => phase == "pfph";

    /// <summary>C10.11, C10.12 (ruling R8.6): the vehicle's MP after hooking up or unhooking, counted in halves.</summary>
    public static (int MfSpent, bool HalfMfSpent) HookMp(int mfSpent, bool halfMfSpent, int mp)
    {
        var halves = (mfSpent * 2) + (halfMfSpent ? 1 : 0) + (mp * 2);
        return (halves / 2, halves % 2 == 1);
    }

    /// <summary>A1.31 (ruling R13.4): a Deployment NTC passes when its Final DR does not exceed the Morale.</summary>
    public static bool DeploymentPassed(int first, int second, int drm, int morale) => first + second + drm <= morale;

    /// <summary>A4.44 (ruling R13.5): a Recovery dr succeeds below 6.</summary>
    public static bool Recovered(int dr, int drm) => dr + drm < 6;

    /// <summary>A1.32, A4.431 (rulings R13.4, R13.5): a Recombination or Transfer is made in the RPh, or a Transfer at the start of the APh.</summary>
    public static bool RallyPhaseActionPhase(string? phase) => phase is "rph" or "aph";
}
