namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Gunflashes (E1.8, E1.82, E1.89 p. 226; E1.91 p. 226 for the first Starshell; ASOP 5.33B p. 680; the E1.8 claim's design, RE1.1 to RE1.7): which acts
/// mark a Gunflash in the Location they came from, which place a fire counter at night, how long a mark lasts, and which mark opens the scenario's first
/// Starshell. A Gunflash belongs to the Location, not to its units: it lasts until the end of the AFPh "even if any/all the units have in the meantime
/// left the Gunflash Location and others have entered it" (E1.8). Play gathers the marks from the fire records; this class decides.
/// </summary>
public static class ScenarioA1GunflashCalculator
{
    /// <summary>The counters that mark a Gunflash (E1.8; ruling R16.2): Prep, First, Final, Bounding, or Intensive Fire, or a Melee.</summary>
    public static IReadOnlyList<UnitCondition> Markers
    {
        get;
    } =
        [UnitCondition.PrepFire, UnitCondition.FirstFire, UnitCondition.FinalFire, UnitCondition.BoundingFire, UnitCondition.IntensiveFire, UnitCondition.Melee];

    /// <summary>Whether a counter is one that marks a Gunflash (E1.8).</summary>
    public static bool MarkerIsGunflash(UnitCondition marker) => Markers.Contains(marker);

    /// <summary>
    /// Whether an act marks a Gunflash in its Location (E1.8, E1.82, E1.89, E1.921; RE1.4): an attack, a Melee, and an Opportunity Fire declaration (E1.8
    /// lists the Opportunity Fire counter) do; dismantling or assembling a SW, Spotting, a radio or phone, an Ammo Vehicle's B# benefit, and a Starshell do not.
    /// </summary>
    public static bool PlacesGunflash(GunflashCause cause) => cause is GunflashCause.Attack or GunflashCause.Melee or GunflashCause.OpportunityFireDeclared;

    /// <summary>Whether the act is one of E1.89's: dismantling or assembling a SW, Spotting, a radio or phone, or an Ammo Vehicle's B# benefit.</summary>
    public static bool CountsAsUse(GunflashCause cause) =>
        cause is GunflashCause.DismantleOrAssemble or GunflashCause.Spotting or GunflashCause.RadioOrPhone or GunflashCause.AmmoVehicleBenefit;

    /// <summary>
    /// Whether a Prep, Bounding, First, Final, or Intensive Fire counter is placed for the act (E1.89; RE1.7): an attack and an Opportunity Fire declaration
    /// (A7.25) place one; E1.89's acts place one by day (A9.8's "counts as use") and none at night, though the use still counts (<see cref="CountsAsUse"/>);
    /// a Melee places its Melee counter, not a fire counter, and a Starshell none (E1.921: it has no effect on the firer's other actions).
    /// </summary>
    public static bool PlacesFireCounter(GunflashCause cause, bool night) =>
        cause is GunflashCause.Attack or GunflashCause.OpportunityFireDeclared || (CountsAsUse(cause) && !night);

    /// <summary>
    /// Whether a Location is marked by a Gunflash (E1.8, E1.82; RE1.2): a mark of an act that places one lasts there, whoever is there now and whether or not
    /// a counter was placed (a MG that kept its ROF) or the firer survives; or the Location holds a Melee. The marks are read until the first found, with no
    /// allocation per call (sight scans ask it for each target Location).
    /// </summary>
    public static bool Marked(string location, IEnumerable<GunflashMark> marks, bool meleeInLocation)
    {
        ArgumentNullException.ThrowIfNull(marks);
        if (meleeInLocation)
        {
            return true;
        }

        foreach (var mark in marks)
        {
            if (string.Equals(mark.Location, location, StringComparison.Ordinal) && PlacesGunflash(mark.Cause))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the ending phase removes every Gunflash mark (E1.8; ASOP 5.33B, p. 680): the end of the AFPh.</summary>
    public static bool MarksEnd(string endingPhase) => endingPhase == "afph";

    /// <summary>
    /// Whether First and Final Fire counters stay past the end of a phase at night (E1.8; ASOP 5.33B; ruling R16.2): at night they stay "until the end of
    /// the AFPh", so through every phase but the AFPh; by day this rule keeps none.
    /// </summary>
    public static bool FireCountersStayAtNight(string endingPhase, bool night) => night && endingPhase != "afph";

    /// <summary>
    /// Whether a mark opens the scenario's first Starshell (E1.91 event 3; RE1.3 as corrected 2026-10-30, by the Index's "Unit"; RE1.4): a Gunflash placed
    /// by an attack, a Melee among them, vs a unit enemy to its attacker. A Dummy stack is a unit (Index, "Unit": "Dummy stacks ... are all different types
    /// of units"; A12.1, p. 77), so an attack vs an enemy Dummy stack opens it as any other does, and nothing here tells the attacker whether the "?" stack
    /// it attacked was a Dummy. An Opportunity Fire declaration, which is no attack, and fire at a Location holding no enemy unit do not. An enemy FFE opens
    /// it too once OBA is built.
    /// </summary>
    public static bool OpensFirstStarshell(GunflashCause cause, bool vsEnemyUnit) => cause is GunflashCause.Attack or GunflashCause.Melee && vsEnemyUnit;

    /// <summary>Whether a mark opens the scenario's first Starshell: <see cref="OpensFirstStarshell(GunflashCause, bool)"/> of its cause and target.</summary>
    public static bool OpensFirstStarshell(GunflashMark mark)
    {
        ArgumentNullException.ThrowIfNull(mark);
        return OpensFirstStarshell(mark.Cause, mark.VsEnemyUnit);
    }
}
