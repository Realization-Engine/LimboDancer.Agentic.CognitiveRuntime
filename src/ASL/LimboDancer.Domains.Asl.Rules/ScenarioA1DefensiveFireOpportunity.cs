namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// One DEFENDER unit as the opportunity reads it at a moving stack's step: whether it is a Dummy, broken, or barred from fire; its LOS to the step's
/// Location and to the Location the stack came from (a Snap Shot's hexside, A8.15), each null when the map cannot say; the range to the step and the
/// unit's Normal Range, null when not known; and whether it is marked First Fire or Final Fire.
/// </summary>
public sealed record DefensiveFirerFacts(string Id, bool Dummy, bool Broken, string? FireBar, bool? LosToStep, bool? LosToOrigin, int? Range, int? NormalRange,
    bool FirstFire, bool FinalFire);

/// <summary>
/// Whether the DEFENDER could fire at a moving stack's step at all (the Play streamlining plan, S6; ruling Q8): when none of his units could, the
/// step passes without asking him. A8.1: he may fire whenever the stack expends MF in the LOS of one of his units. The answer errs one way only: a
/// fact not known counts as "may fire", so he is asked; a wrong "may not" would take a shot from him.
/// </summary>
public static class ScenarioA1DefensiveFireOpportunity
{
    /// <summary>
    /// Whether any of the DEFENDER's units could fire at the step, or he owes Mandatory Fire there (A8.312), which only he resolves.
    /// </summary>
    public static bool MayFire(IReadOnlyList<DefensiveFirerFacts> firers, bool mandatoryFireOwed)
    {
        ArgumentNullException.ThrowIfNull(firers);
        if (mandatoryFireOwed)
        {
            return true;
        }

        foreach (var firer in firers)
        {
            if (MayFire(firer))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether one unit could fire at the step: not a Dummy, not broken (A10.4), not barred (the fire bar); in LOS of the Location, or of the
    /// hexside crossed into it, which the Location left stands for (A8.15); and within reach: Final Fire only at an ADJACENT or same-hex stack
    /// (FPF, A8.31), Subsequent First Fire only within Normal Range (A8.3), and otherwise up to Long Range, twice the Normal Range (A7.22).
    /// </summary>
    public static bool MayFire(DefensiveFirerFacts firer)
    {
        ArgumentNullException.ThrowIfNull(firer);
        if (firer.Dummy || firer.Broken || firer.FireBar is not null)
        {
            return false;
        }

        if (firer.LosToStep == false && firer.LosToOrigin == false)
        {
            return false;
        }

        if (firer.Range is not { } range)
        {
            return true;
        }

        if (firer.FinalFire)
        {
            return range <= 1;
        }

        if (firer.NormalRange is not { } normal)
        {
            return true;
        }

        return firer.FirstFire ? range <= normal : range <= 2 * normal;
    }
}
