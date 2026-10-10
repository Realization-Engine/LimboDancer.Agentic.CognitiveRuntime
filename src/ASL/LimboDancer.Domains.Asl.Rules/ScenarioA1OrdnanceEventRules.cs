namespace LimboDancer.Domains.Asl.Rules;

/// <summary>What a PF's Casualty Reduction does to its firer (A7.302, A17.2): a squad becomes its HS, a SMC is wounded, else the unit is eliminated.</summary>
public enum FirerCasualty
{
    HalfSquad,
    Wounded,
    Eliminated,
}

/// <summary>
/// The rule decisions of an ordnance shot's events (S7): the fire phase's marker (A7.1, A8.1, C2.24, C2.241), an Emplaced Gun's reveal (A12.34,
/// ruling R26.5), a To Kill result's conditions (C7.42, D5.5; rulings R7.7 to R7.9), and a PF's effect on its firer (C13.31, C13.36; ruling R9.7).
/// The caller reads the state and writes the events in their order; each condition list is in the order the record writes it.
/// </summary>
public static class ScenarioA1OrdnanceEventRules
{
    /// <summary>C13.31 (ruling R9.7): the marker a PF's firer carries with a broken result.</summary>
    public static UnitCondition FirerPhaseMarker(string? phase) => phase switch
    {
        "DFPh" => UnitCondition.FinalFire,
        "MPh" => UnitCondition.FirstFire,
        _ => UnitCondition.PrepFire,
    };

    /// <summary>A7.1, C2.24; A8.1, C2.241 (ruling R8.1): the shot's fire marker; in the MPh a First Fire counter only once its ROF is spent.</summary>
    public static UnitCondition? ShotMarker(string? phase, string? fireCounter) => phase switch
    {
        "DFPh" => UnitCondition.FinalFire,
        "MPh" => fireCounter is "first-fire" or "intensive-fire" ? UnitCondition.FirstFire : null,
        _ => UnitCondition.PrepFire,
    };

    /// <summary>
    /// A12.34 (ruling R26.5): an Emplaced hidden or concealed Gun's and crew's conditions from the colored dr and the nearest Good Order enemy in LOS;
    /// none when no such enemy sees it.
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> EmplacedReveal(int? nearest, int colored) =>
        nearest is null
            ? []
            : (colored >= 5 && nearest <= 16) || (colored == 6 && nearest >= 17)
                ? [(UnitCondition.Concealed, false), (UnitCondition.Hidden, false)]
                : [(UnitCondition.Concealed, true), (UnitCondition.Hidden, false)];

    /// <summary>Whether a To Kill result wrecks the vehicle (D5.6, D10.1).</summary>
    public static bool Wrecks(string result) => result is OrdnanceKill.Burn or OrdnanceKill.Eliminated;

    /// <summary>
    /// A12.2, D5.5, C7.42: a struck vehicle's conditions: its "?" lost, Immobilized, or Shocked BU and stopped. A later entry of the same condition
    /// replaces the earlier one's value in its place.
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> KillConditions(OrdnanceKill kill, bool concealedOrHidden)
    {
        var changed = new List<(UnitCondition, bool)>();
        if (concealedOrHidden)
        {
            changed.Add((UnitCondition.Concealed, false));
            changed.Add((UnitCondition.Hidden, false));
        }

        if (kill.Result == OrdnanceKill.Immobilized)
        {
            changed.Add((UnitCondition.Immobilized, true));
            changed.Add((UnitCondition.Motion, false));
        }

        if (kill.Shocked == true)
        {
            changed.Add((UnitCondition.Shocked, true));
            changed.Add((UnitCondition.UnconfirmedKill, false));
            changed.Add((UnitCondition.ButtonedUp, true));
            changed.Add((UnitCondition.Motion, false));
        }

        return changed;
    }

    /// <summary>D5.5: an Abandoned vehicle's conditions.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> AbandonedConditions { get; } = [(UnitCondition.Abandoned, true), (UnitCondition.Motion, false)];

    /// <summary>
    /// A7.302, A17.11 (pass 35, task 35.1): a Casualty Reduction's result; the HS is read lazily for a squad alone, and a SMC's Wound Severity dr says
    /// whether its wound is mortal.
    /// </summary>
    public static FirerCasualty Casualty(string kind, Func<bool> hasHalfSquad, bool wounded, int? severityDr) =>
        kind == "asl:squad" && hasHalfSquad() ? FirerCasualty.HalfSquad
            : !ScenarioA1Wounds.SeverityDue(kind is "asl:leader" or "asl:hero") ? FirerCasualty.Eliminated
            : severityDr is { } dr ? ScenarioA1Wounds.Mortal(dr, wounded) ? FirerCasualty.Eliminated : FirerCasualty.Wounded
            : throw new ArgumentNullException(nameof(severityDr), "A SMC's Casualty Reduction needs its Wound Severity dr (A17.11).");
}
