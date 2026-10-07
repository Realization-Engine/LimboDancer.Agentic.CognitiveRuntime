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

    /// <summary>Whether a unit is a Good Order enemy ground unit that can see (A12.2): Good Order Personnel, or a vehicle whose crew is not Stunned or Recalled.</summary>
    public static bool Watching(bool active, bool vehicle, bool stunned, bool shocked, bool unconfirmedKill, bool recalled, bool abandoned, Func<bool> personnel,
        bool broken, bool berserk, bool captured, bool melee) => active
        && (vehicle ? !stunned && !shocked && !unconfirmedKill && !recalled && !abandoned
            : personnel() && !broken && !berserk && !captured && !melee);

    /// <summary>A12.2: whether a watcher is near enough to count, within the range given when one is; the range is read only when a limit is given.</summary>
    public static bool WithinRange(int? within, Func<int?> range) => within is not { } limit || (range() is { } read && read <= limit);

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
    /// A24.2, A24.5, A24.8 (ruling R9.6): a Location's SMOKE, +2 per source and at most +3, with +1 more for fire traced out of or within it; none when
    /// the LOS does not enter it.
    /// </summary>
    public static int SmokeDrm(int sources, bool firerThere, bool targetThere, Func<bool> crossed)
    {
        var drm = Math.Min(3, 2 * sources);
        return firerThere ? drm + 1
            : targetThere ? drm
            : crossed() ? drm
            : 0;
    }
}
