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
    // D5.341, D5.342 (the D5.34 claim): a crew's Recall, as the Fire path's vehicle results set it.
    private static readonly FireVehicleEffect RecallEffect = new(string.Empty, string.Empty, FireVehicleEffect.None, null, [], 0, null, null, FireVehicleEffect.Recalled);

    private static UnitCondition Unit(VehicleCondition condition) => condition switch
    {
        VehicleCondition.Immobilized => UnitCondition.Immobilized,
        VehicleCondition.Motion => UnitCondition.Motion,
        VehicleCondition.Stunned => UnitCondition.Stunned,
        VehicleCondition.StunRecovery => UnitCondition.StunRecovery,
        VehicleCondition.Recalled => UnitCondition.Recalled,
        VehicleCondition.ButtonedUp => UnitCondition.ButtonedUp,
        _ => UnitCondition.Pinned,
    };

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
        // D3.3 (pass 50): a Bounding Fire counter once a Bounding First Firer's ROF is spent.
        "MPh" => fireCounter is "first-fire" or "intensive-fire" ? UnitCondition.FirstFire : fireCounter == "bounding-fire" ? UnitCondition.BoundingFire : null,
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
        ArgumentNullException.ThrowIfNull(kill);
        return KillConditions(kill.Result, kill.Shocked == true, concealedOrHidden);
    }

    /// <summary>
    /// <see cref="KillConditions(OrdnanceKill, bool)"/> with the AFV's Stun counter (the D5.34 claim): the To Kill result read as
    /// <see cref="KillConditions(string, bool, bool, bool, bool)"/> reads it, so a MG's Stun of an AFV already Stunned (or Recalled) this Player Turn or
    /// under a "+1" counter is a Recall (D5.342, p. 204).
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> KillConditions(OrdnanceKill kill, bool concealedOrHidden, bool stunned, bool stunRecovery)
    {
        ArgumentNullException.ThrowIfNull(kill);
        return KillConditions(kill.Result, kill.Shocked == true, concealedOrHidden, stunned, stunRecovery);
    }

    /// <summary>
    /// The conditions of a result in the To Kill vocabulary (pass 60): a To Kill DR's, or a mortar's IFT attack on an AFV (C1.55), whose
    /// <c>shock</c> always Shocks it. A MG's Stun is a Stun whatever the AFV's state, where D5.342 (p. 204) Recalls an AFV already Stunned or under a "+1"
    /// counter: the overload with the AFV's <c>stunned</c> and <c>stunRecovery</c> replaces this one (the D5.34 claim, ruling RD5.16), which forwards to it
    /// with both false and keeps its answer.
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> KillConditions(string result, bool shocked, bool concealedOrHidden) =>
        KillConditions(result, shocked, concealedOrHidden, false, false);

    /// <summary>
    /// <see cref="KillConditions(string, bool, bool)"/> with the AFV's Stun counter (the D5.34 claim, ruling RD5.16): a MG's Stun of an AFV already Stunned
    /// or under a "+1" counter is a second Stun (<see cref="ScenarioA1StunCalculator.SecondStun"/>), which D5.342 (p. 204) makes a Recall, with the
    /// conditions <see cref="ScenarioA1ResultTables.VehicleConditions"/> gives a Recall. <paramref name="stunned"/> includes the front of a Recall (D5.341,
    /// p. 204: "Recall is treated the same as a Stun"): the caller gives Stunned or Recalled this Player Turn.
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> KillConditions(string result, bool shocked, bool concealedOrHidden, bool stunned, bool stunRecovery)
    {
        var changed = new List<(UnitCondition, bool)>();
        if (concealedOrHidden)
        {
            changed.Add((UnitCondition.Concealed, false));
            changed.Add((UnitCondition.Hidden, false));
        }

        if (result == OrdnanceKill.Immobilized)
        {
            changed.Add((UnitCondition.Immobilized, true));
            changed.Add((UnitCondition.Motion, false));
        }

        // D5.342 (p. 204; the D5.34 claim): a MG's Stun of an AFV already Stunned or under a "+1" counter is a Recall.
        if (result == OrdnanceKill.Stun && ScenarioA1StunCalculator.SecondStun(stunned, stunRecovery))
        {
            foreach (var (condition, value) in ScenarioA1ResultTables.VehicleConditions(RecallEffect))
            {
                changed.Add((Unit(condition), value));
            }
        }
        // D5.34 (p. 203; pass 60): a MG's Stun; the AFV becomes BU and Stops.
        else if (result == OrdnanceKill.Stun)
        {
            changed.Add((UnitCondition.Stunned, true));
            changed.Add((UnitCondition.ButtonedUp, true));
            changed.Add((UnitCondition.Motion, false));
        }

        if (shocked)
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
