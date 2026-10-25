namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// What a seer is read for (pass 70, ruling R70.16): "Good Order for '?' loss; unbroken (and Dummies) for denial of '?' gain" (A12.1, p. 76), and the
/// Good Order ground units in whose LOS a stack may be inspected (A12.16, p. 79).
/// </summary>
public enum SightPurpose
{
    /// <summary>A12.14 (p. 77): the loss of a "?", forced only by a Good Order enemy ground unit.</summary>
    Loss,

    /// <summary>A12.12, A12.121, A12.122 (p. 77): the denial of a "?" gain, by any unbroken enemy ground unit, Dummies included.</summary>
    GainDenial,

    /// <summary>A12.16 (p. 79): the Right of Inspection, by the viewing side's Good Order ground units.</summary>
    Inspection,
}

/// <summary>
/// How a caller takes a LOS read (pass 70, ruling R70.16: each as it took it before the pass). <see cref="Clear"/>: only a definitive clear LOS (the
/// move's, the advance's, the Rally's, fire's, and A12.34's reads); <see cref="NotBlocked"/>: any read that is not blocked, definitive or not (a
/// vehicle's concealment, A12.2, and the gain); <see cref="NotShownBlocked"/>: anything but a blocked read, a missing or unsupported one counting as in
/// LOS (the Right of Inspection, ruling R70.18, so a map with no LOS shows a stack as before the pass).
/// </summary>
public enum SightLosReading
{
    Clear,
    NotBlocked,
    NotShownBlocked,
}

/// <summary>
/// One unit as a seer (pass 70, ruling R70.16): active, its side, a Dummy, hidden, aboard a vehicle as a Passenger or Rider, Good Order as the planner
/// reads it (A.7), unbroken, a prisoner, an empty vehicle (one with neither an inherent crew nor Passengers nor Riders, "broken" for "?" gain and loss,
/// A12.1, p. 76), and its Location as an index into the caller's table (null off the map).
/// </summary>
public sealed record SightSeerFacts(bool Active, string? Side, bool Dummy, bool Hidden, bool Aboard, bool GoodOrder, bool Unbroken, bool Captured, bool EmptyVehicle,
    int? Location);

/// <summary>A LOS read as the map gives it: whether it is definitively clear, whether it is blocked (null when the map does not answer), and its range.</summary>
public sealed record SightLosFacts(bool Clear, bool? Blocked, int Range);

/// <summary>A fact reader for the sight reads: the LOS between two Locations of the caller's table, or null when the map cannot give it.</summary>
public interface ISightLosReader
{
    public SightLosFacts? Los(int fromLocation, int toLocation);
}

/// <summary>
/// Who sees (A12.1, A12.12, A12.14, A12.141, A12.16, A12.34; pass 70, rulings R70.16 to R70.18): one member for every read of a seeing enemy, and of the
/// viewer's own seers for the Right of Inspection. Play hands over each unit's facts and a LOS reader over the map, and keeps the answers per state.
/// </summary>
public static class ScenarioA1Sight
{
    /// <summary>
    /// Whether a unit is a seer for a purpose (ruling R70.16). Loss: Good Order (A12.14: "a Good Order (not just unbroken) enemy ground unit"), not a
    /// Dummy (not Good Order, A12.16), not hidden (it would have to show itself to force the loss; R35.5), not a Passenger or Rider (its vehicle is
    /// counted; R26.2), not an empty vehicle (A12.1). GainDenial: unbroken and not a prisoner, Dummies included (A12.1, A12.121's example), not hidden
    /// (a hidden unit is not placed to deny a gain), not an empty vehicle (A12.1: it can "neither force loss of '?' nor block placement of '?'").
    /// Inspection: Good Order, hidden units included (the viewer's own), not a Dummy ("Dummy stacks (not being Good order)", A12.16), not an empty vehicle.
    /// </summary>
    public static bool Sees(SightSeerFacts seer, SightPurpose purpose)
    {
        ArgumentNullException.ThrowIfNull(seer);
        if (!seer.Active || seer.Location is null || seer.EmptyVehicle)
        {
            return false;
        }

        return purpose switch
        {
            SightPurpose.Loss => seer.GoodOrder && !seer.Dummy && !seer.Hidden && !seer.Aboard,
            SightPurpose.GainDenial => seer.Unbroken && !seer.Captured && !seer.Hidden,
            _ => seer.GoodOrder && !seer.Dummy,
        };
    }

    /// <summary>
    /// The distinct Locations of the seers of a purpose that may see a unit of <paramref name="watchedSide"/>: the units of every other side, in the
    /// order given.
    /// </summary>
    public static int[] SeerLocations(IEnumerable<SightSeerFacts> units, string watchedSide, SightPurpose purpose)
    {
        ArgumentNullException.ThrowIfNull(units);
        return [.. units.Where(unit => unit.Side != watchedSide && Sees(unit, purpose)).Select(unit => unit.Location!.Value).Distinct()];
    }

    /// <summary>The distinct Locations of a side's own seers of a purpose, in the order given: the viewer's, for the Right of Inspection (A12.16).</summary>
    public static int[] OwnSeerLocations(IEnumerable<SightSeerFacts> units, string seeingSide, SightPurpose purpose)
    {
        ArgumentNullException.ThrowIfNull(units);
        return [.. units.Where(unit => unit.Side == seeingSide && Sees(unit, purpose)).Select(unit => unit.Location!.Value).Distinct()];
    }

    /// <summary>Whether a LOS read counts as in LOS for a reading (ruling R70.16; each caller's reading as it was before the pass).</summary>
    public static bool InLos(SightLosFacts? los, SightLosReading reading) => reading switch
    {
        SightLosReading.Clear => los is { Clear: true },
        SightLosReading.NotBlocked => los is { Blocked: false },
        _ => los is not { Blocked: true },
    };

    /// <summary>
    /// The range to the nearest seer with a LOS to a Location, or null when none has one (A12.121: "the range from the nearest such enemy having a LOS").
    /// A seer in the Location itself is at range 0, with no read. A read the map does not give is no LOS here, whatever the reading. "Within 16 hexes
    /// and in LOS" (A12.14, A12.141) is a nearest seer at 16 or less.
    /// </summary>
    public static int? NearestSeer(IReadOnlyList<int> seers, int at, ISightLosReader los, SightLosReading reading)
    {
        ArgumentNullException.ThrowIfNull(seers);
        ArgumentNullException.ThrowIfNull(los);
        int? nearest = null;
        foreach (var seer in seers)
        {
            if (seer == at)
            {
                return 0;
            }

            if (los.Los(seer, at) is { } read && InLos(read, reading) && (nearest is null || read.Range < nearest))
            {
                nearest = read.Range;
            }
        }

        return nearest;
    }

    /// <summary>Whether any seer has a LOS to a Location, read until the first that has one (the Right of Inspection's early exit, R70.18).</summary>
    public static bool AnySeerInLos(IReadOnlyList<int> seers, int at, ISightLosReader los, SightLosReading reading)
    {
        ArgumentNullException.ThrowIfNull(seers);
        ArgumentNullException.ThrowIfNull(los);
        return seers.Any(seer => seer == at || InLos(los.Los(seer, at), reading));
    }

    /// <summary>A12.14, A12.141: a seer is near enough when the nearest is within the range given; with no range given, any seer in LOS is.</summary>
    public static bool SeenWithin(int? nearest, int? within) => nearest is { } range && (within is not { } limit || range <= limit);

    /// <summary>
    /// A12.141 (p. 78; ruling R70.17): a concealed or hidden unit that engages in an activity loses its "?" when a Good Order enemy ground unit within
    /// 16 hexes has a LOS to it. The nearest Loss seer is read only for a concealed unit.
    /// </summary>
    public static bool ActionLosesConcealment(bool concealed, Func<int?> nearestLossSeer)
    {
        ArgumentNullException.ThrowIfNull(nearestLossSeer);
        return concealed && SeenWithin(nearestLossSeer(), 16);
    }

    /// <summary>
    /// A12.16 (p. 79; ruling R70.18): after play begins an enemy stack not concealed may be inspected, unless it is out of the LOS of every Good Order
    /// ground unit of the viewing side. Before play none is (A2.9; the view reads that itself). The seers are read only after setup closes.
    /// </summary>
    public static bool Inspectable(bool setupClosed, Func<bool> anySeerInLos)
    {
        ArgumentNullException.ThrowIfNull(anySeerInLos);
        return setupClosed && anySeerInLos();
    }
}
