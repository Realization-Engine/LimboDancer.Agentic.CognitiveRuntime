using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Wrecks, and the cover and Hindrance of AFVs and wrecks (backlog pass 6, rulings R6.1 to R6.5): a destroyed vehicle becomes a wreck,
/// burning when the attack burned it (D10.1, B25.14); Infantry with a wreck or a friendly AFV take its +1 TEM (D9.3); an AFV or wreck
/// between firer and target is a +1 Hindrance (D9.4); a burning wreck's smoke is a +2 Hindrance (B25.2, A24.2, A24.8); and a wreck
/// raises the MP a vehicle pays to enter its hex (D2.14, B25.141).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The id of the Blaze on a burning wreck (B25.14).</summary>
    public static string BlazeId(string vehicle) => vehicle + "-blaze";

    /// <summary>The wrecks at a Location (D10.1).</summary>
    public static IReadOnlyList<UnitInstance> WrecksAt(GameState state, BoardLocation at)
    {
        ArgumentNullException.ThrowIfNull(state);
        return [.. state.Units.Where(unit => unit.Status == InstanceStatus.Wrecked && state.Location(unit.Id)?.Location == at)];
    }

    /// <summary>Whether a wreck carries a Blaze (B25.14).</summary>
    public static bool IsBurning(GameState state, UnitInstance wreck)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(wreck);
        return state.Entities.Any(entity => entity.Status == InstanceStatus.Active && entity.Id == BlazeId(wreck.Id));
    }

    /// <summary>
    /// Whether an AFV or wreck gives cover and Hindrance now (D2.41, D9.3, D9.4; ruling R6.1): not in Motion, and not, in the MPh, DFPh, or
    /// AFPh, one that spent MP in this Player Turn's MPh (the Case J clause).
    /// </summary>
    private static bool Standing(GameState state, UnitInstance vehicle) =>
        ScenarioA1VehicleSightRules.Standing(Is(vehicle, Conditions.Motion), state.Phase, state.MovedVehicles.Contains(vehicle.Id, StringComparer.Ordinal));

    /// <summary>
    /// The wreck or AFV whose +1 TEM Infantry of a side claim at a Location (D9.3, D10.3; ruling R6.1): a non-burning wreck of either
    /// side, a friendly AFV, or an abandoned enemy AFV, standing; null when there is none. An unarmored vehicle gives none.
    /// </summary>
    public static string? CoverAt(GameState state, BoardLocation at, string? infantrySide)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (infantrySide is null)
        {
            return null;
        }

        return ScenarioA1VehicleSightRules.Cover(
            WrecksAt(state, at).Select(wreck => (wreck.Id, (Func<bool>)(() => IsBurning(state, wreck)), (Func<bool>)(() => Standing(state, wreck)))),
            state.At(at).OfType<UnitInstance>().Select(unit => (unit.Id, (Func<bool>)(() => IsAfv(unit)),
                (Func<bool>)(() => unit.Side == infantrySide || Is(unit, Conditions.Abandoned)), (Func<bool>)(() => Standing(state, unit)))));
    }

    /// <summary>
    /// The Hindrance an LOS takes from vehicles and wrecks (rulings R6.2, R6.3), added to the map's: +1 at each range the LOS passes
    /// through a hex holding a standing AFV or non-burning wreck at the LOS's level, seen from both ends: at a range where the map counts a
    /// Hindrance in another hex only the highest counts, but a vehicle in a Hindrance hex adds its +1 to that hex's (A6.7 and its example);
    /// and +2 for each burning wreck's Location it is traced into or through, +3 out of or within (B25.2, A24.2, A24.8). Null with a reason
    /// when an LOS to a vehicle's hex cannot be read.
    /// </summary>
    private (int Drm, string? Reason) VehicleHindrance(GameState state, BoardLocation from, BoardLocation target, LosResult los, bool sameLevel,
        HashSet<int> mapRanges)
    {
        var ranges = new HashSet<int>();
        var fromElevation = ReadLocation(state, from) is { } fromRead ? fromRead.Hex.BaseLevel + fromRead.Level.Level : (int?)null;
        foreach (var crossed in los.Crossed)
        {
            if (!sameLevel || ranges.Contains(crossed.Range) || crossed.Board is not { } board || crossed.Hex is not { } hex)
            {
                continue;
            }

            var hindering = state.Units.Where(unit => (unit.Status == InstanceStatus.Active ? IsAfv(unit)
                    : unit.Status == InstanceStatus.Wrecked && !IsBurning(state, unit))
                && state.Location(unit.Id)?.Location is { } at && at.Board == board && at.Hex == hex && Standing(state, unit)
                && ReadLocation(state, at) is { } atRead && atRead.Hex.BaseLevel + atRead.Level.Level == fromElevation)
                .Select(unit => state.Location(unit.Id)!.Location).FirstOrDefault();
            if (hindering is null)
            {
                continue;
            }

            // A6.7: a map Hindrance at this range in another hex is the higher; one in the vehicle's own hex is added to.
            var ownTerrain = ReadLocation(state, hindering) is { } hinderingRead ? TerrainKey(hinderingRead) : null;
            if (!ScenarioA1VehicleSightRules.HindersAtRange(mapRanges.Contains(crossed.Range), ownTerrain, state.ScenarioMonth))
            {
                continue;
            }

            // D9.4: not when the vehicle or wreck is out of the LOS of the firer or the target.
            if (Los(state, from, hindering) is not { } toFirer || Los(state, target, hindering) is not { } toTarget)
            {
                return (0, "play.fire-los: the LOS to a vehicle's or wreck's hex cannot be read (D9.4)");
            }

            if (toFirer.IsBlocked == false && toTarget.IsBlocked == false)
            {
                ranges.Add(crossed.Range);
            }
        }

        // A24.2, A24.5, A24.8 (ruling R9.6): each SMOKE source of a Location, a burning wreck's or a grenade's, is +2, a Location's SMOKE at most +3,
        // and +1 more for fire traced out of or within it.
        var smoke = 0;
        foreach (var hex in SmokeSources(state).GroupBy(place => (place.Board, place.Hex)))
        {
            var sameHex = (BoardLocation place) => place.Board == hex.Key.Board && place.Hex == hex.Key.Hex;
            smoke += ScenarioA1VehicleSightRules.SmokeDrm(hex.Count(), sameHex(from), sameHex(target),
                () => los.Crossed.Any(item => item.Board == hex.Key.Board && item.Hex == hex.Key.Hex));
        }

        return (ranges.Count + smoke, null);
    }

    /// <summary>The Locations of every SMOKE source (A24.2; rulings R6.3, R9.5): a burning wreck's Blaze and each SMOKE grenade counter.</summary>
    private static IEnumerable<BoardLocation> SmokeSources(GameState state) =>
        state.Units.Where(unit => unit.Status == InstanceStatus.Wrecked && IsBurning(state, unit)).Select(unit => state.Location(unit.Id)!.Location)
            .Concat(state.Entities.Where(entity => entity.Status == InstanceStatus.Active && entity.Kind == "asl:smoke" && entity.Position is MapPosition)
                .Select(entity => ((MapPosition)entity.Position).Location));

    /// <summary>Whether a Location holds SMOKE (A24.7; rulings R6.3, R9.6).</summary>
    private static bool HasSmoke(GameState state, BoardLocation at) => SmokeSources(state).Contains(at);

    /// <summary>The extra MP a vehicle pays to enter a hex for its wrecks and vehicles, doubled by a road entry, and a Blaze's smoke (D2.14, B25.141).</summary>
    private static int WreckEntryHalfMp(GameState state, BoardLocation to, bool road)
    {
        var wrecks = WrecksAt(state, to);
        var vehicles = state.At(to).OfType<UnitInstance>().Count(LiveFire.IsVehicle);
        return ScenarioA1VehicleTerrainCosts.WreckEntryHalfMp(wrecks.Count, vehicles, road, HasSmoke(state, to));
    }

    /// <summary>The extra half MF Infantry pay to enter a SMOKE Location: a burning wreck's or a grenade's (B25.141, A24.7; ruling R9.6).</summary>
    private static int BlazeEntryHalfMf(GameState state, BoardLocation to) => ScenarioA1VehicleTerrainCosts.BlazeEntryHalfMf(HasSmoke(state, to));
}
