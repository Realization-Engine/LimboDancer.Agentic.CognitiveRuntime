namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions of a vehicle's concealment and of the cover and Hindrance of AFVs and wrecks (S8; backlog pass 6, rulings R6.1 to R6.7, R9.6):
/// Concealment Terrain and who sees (A12.2), the "?" lost, standing (D2.41), cover (D9.3, D10.3), Hindrance (D9.4, A6.7), and SMOKE (A24.2, A24.5,
/// A24.8). The caller reads the state, the map, and the LOS and writes the events.
/// </summary>
public static class ScenarioA1VehicleSightRules
{
    /// <summary>Whether a Location is Concealment Terrain for a vehicle (A12.2; ruling R6.7): grain in season, June to September (B15.6).</summary>
    public static bool ConcealmentTerrain(int? month, Func<string?> terrain) => month is >= 6 and <= 9 && terrain() == "grain";

    /// <summary>
    /// Whether a concealed or hidden vehicle loses its "?" (A12.2; ruling R6.7): the moving vehicle moved within 16 hexes and in the LOS of a Good
    /// Order enemy ground unit, or it is not in Concealment Terrain and in the LOS of one (Case H).
    /// </summary>
    public static bool ConcealmentLost(bool movedNow, Func<int?, bool> seen, Func<bool> concealmentTerrain) =>
        (movedNow && seen(16)) || (!concealmentTerrain() && seen(null));

    /// <summary>
    /// Whether an AFV or wreck gives cover and Hindrance now (D2.41, D9.3, D9.4; ruling R6.1): not in Motion, and not, in the MPh, DFPh, or
    /// AFPh, one that spent MP in this Player Turn's MPh (the Case J clause).
    /// </summary>
    public static bool Standing(bool motion, string? phase, bool movedThisTurn) =>
        !motion && !(phase is "mph" or "dfph" or "afph" && movedThisTurn);

    /// <summary>
    /// D9.3, D10.3 (ruling R6.1): the wreck or AFV whose +1 TEM Infantry claim: the first standing non-burning wreck, else the first standing AFV that is
    /// friendly or abandoned; null when there is none. Each candidate's facts are read in order, only until one gives cover.
    /// </summary>
    public static string? Cover(IEnumerable<(string Id, Func<bool> Burning, Func<bool> Standing)> wrecks,
        IEnumerable<(string Id, Func<bool> Afv, Func<bool> FriendlyOrAbandoned, Func<bool> Standing)> units)
    {
        ArgumentNullException.ThrowIfNull(wrecks);
        ArgumentNullException.ThrowIfNull(units);
        return wrecks.FirstOrDefault(wreck => !wreck.Burning() && wreck.Standing()).Id
            ?? units.FirstOrDefault(unit => unit.Afv() && unit.FriendlyOrAbandoned() && unit.Standing()).Id;
    }

    /// <summary>A6.7: a vehicle in a brush or in-season grain hex adds its +1 to that hex's own Hindrance (rulings R6.2, R6.3).</summary>
    public static bool AddsToOwnHindrance(string? terrain, int? month) => terrain == "brush" || (terrain == "grain" && month is >= 6 and <= 9);

    /// <summary>A6.7 (rulings R6.2, R6.3): a vehicle's or wreck's +1 counts at a range unless the map counts a Hindrance there it does not add to.</summary>
    public static bool HindersAtRange(bool mapHindranceAtRange, string? ownTerrain, int? month) => !mapHindranceAtRange || AddsToOwnHindrance(ownTerrain, month);

    /// <summary>
    /// A24.2, A24.5, A24.8 (ruling R9.6): a Location's SMOKE counted at +2 a source, by the by-value overload with
    /// <see cref="ScenarioA1SmokeStrengthCalculator.CountedSources"/> (the A24.5 claim, RA24.5): the same answer for every count.
    /// </summary>
    public static int SmokeDrm(int sources, bool firerThere, bool targetThere, Func<bool> crossed) =>
        SmokeDrm(ScenarioA1SmokeStrengthCalculator.CountedSources(sources), firerThere, targetThere, crossed);

    /// <summary>
    /// A24.2, A24.8 (the A24.5 claim, RA24.5): a Location's SMOKE, the sum of its sources' Hindrance (<see cref="ScenarioA1SmokeStrengthCalculator.Hindrance"/>)
    /// and at most +3, with +1 more for fire traced out of or within it; none when the LOS does not enter it, which is read only when neither end is there.
    /// </summary>
    public static int SmokeDrm(IReadOnlyList<int> hindrances, bool firerThere, bool targetThere, Func<bool> crossed)
    {
        ArgumentNullException.ThrowIfNull(hindrances);
        var drm = Math.Min(3, hindrances.Sum());
        return firerThere ? drm + 1
            : targetThere ? drm
            : crossed() ? drm
            : 0;
    }

    /// <summary>
    /// D9.4 (p. 210; pass 35, task 35.13 h): a wreck or AFV in Bypass is no Hindrance to a LOS that does not touch the hexside it Bypasses. The LOS
    /// is read as touching it when it also enters the hex across that hexside, or starts or ends there.
    /// </summary>
    public static bool BypassHinders(bool inBypass, Func<bool> losEntersHexAcrossHexside)
    {
        ArgumentNullException.ThrowIfNull(losEntersHexAcrossHexside);
        return !inBypass || losEntersHexAcrossHexside();
    }

    /// <summary>
    /// C5.5 (p. 172; pass 35, task 35.10): the Hindrance of a Gun's shot within its own hex, Case R: the hex's SMOKE as for fire within it (A24.2,
    /// A24.8), and +1 for a wreck or AFV there that is neither firer nor target (D9.4).
    /// </summary>
    public static int OwnHexHindrance(int smokeSources, bool wreckOrAfv) =>
        OwnHexHindrance(smokeSources > 0 ? ScenarioA1SmokeStrengthCalculator.CountedSources(smokeSources) : [], wreckOrAfv);

    /// <summary>C5.5 Case R by value (the A24.5 claim, RA24.5): the hex's SMOKE sources' Hindrance as for fire within it, and +1 for a wreck or AFV.</summary>
    public static int OwnHexHindrance(IReadOnlyList<int> smokeHindrances, bool wreckOrAfv)
    {
        ArgumentNullException.ThrowIfNull(smokeHindrances);
        return (smokeHindrances.Count > 0 ? SmokeDrm(smokeHindrances, true, true, static () => false) : 0) + (wreckOrAfv ? 1 : 0);
    }
}
