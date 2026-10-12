namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A unit standing in a Location of the path, as the state has it: Rules judges whether it holds the Location against the unit searched for.</summary>
public sealed record PathUnitFacts(bool Active, bool Enemy, bool Dummy, bool Broken, bool Unarmed, bool Captured, bool Concealed, bool Hidden);

/// <summary>The unit searched for, as the state and the map have it: Rules judges whether A7.72 applies to it.</summary>
public sealed record UpperLevelUnitFacts(bool Active, bool Vehicle, bool Aboard, bool InBypass, int Level, bool Building, bool Berserk, bool Heroic);

/// <summary>
/// The Locations around a unit as the A7.72 search reads them (pass 45, task 45.1b): Play numbers the Locations as the search asks for them and
/// answers from the map and the state; the search is Rules'.
/// </summary>
public interface IUpperLevelPathReader
{
    /// <summary>The Locations Infantry could step into from a Location, as the movement rules allow the step, whoever stands there.</summary>
    public IReadOnlyList<int> StepsFrom(int location);

    /// <summary>Whether a Location is at ground level.</summary>
    public bool GroundLevel(int location);

    /// <summary>The units in a Location.</summary>
    public IReadOnlyList<PathUnitFacts> UnitsAt(int location);
}

/// <summary>
/// Upper-level Encirclement (A7.72, p. 58; pass 45, task 45.1b): "Encirclement also applies to any non-heroic/non-berserk unit in an upper level
/// building Location that cannot trace a path free of an unbroken, armed, unconcealed enemy unit/Blaze to ground level through ... Locations it
/// could legally traverse if so inclined. This type of Encirclement is broken the instant such a path can be traced." It is a condition of the map
/// and the units, read when asked; no counter marks it. A terrain Blaze is not built, and Scaling (B23.424) is not: both are left out.
/// </summary>
public static class ScenarioA1UpperLevelEncirclement
{
    /// <summary>The most Locations the search visits; a building of the boards has far fewer above ground.</summary>
    public const int SearchLimit = 96;

    /// <summary>Whether the rule can apply to a unit: active Infantry on foot, not in Bypass, in an upper level of a building, neither berserk nor heroic.</summary>
    public static bool Applies(UpperLevelUnitFacts unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return unit.Active && !unit.Vehicle && !unit.Aboard && !unit.InBypass && unit.Level > 0 && unit.Building && !unit.Berserk && !unit.Heroic;
    }

    /// <summary>Whether a unit holds its Location against the path: "an unbroken, armed, unconcealed enemy unit".</summary>
    public static bool Holds(PathUnitFacts unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return unit.Active && unit.Enemy && !unit.Dummy && !unit.Broken && !unit.Unarmed && !unit.Captured && !unit.Concealed && !unit.Hidden;
    }

    /// <summary>
    /// Whether the unit is Encircled by its cut-off path. <paramref name="start"/> is its Location in the reader's numbering. A search that reaches
    /// its limit has not shown that no path exists, and calls no unit Encircled.
    /// </summary>
    public static bool Encircled(UpperLevelUnitFacts unit, int start, IUpperLevelPathReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (!Applies(unit))
        {
            return false;
        }

        var seen = new HashSet<int> { start };
        var open = new Queue<int>();
        open.Enqueue(start);
        while (open.TryDequeue(out var current))
        {
            foreach (var next in reader.StepsFrom(current))
            {
                if (seen.Contains(next) || reader.UnitsAt(next).Any(Holds))
                {
                    continue;
                }

                if (reader.GroundLevel(next))
                {
                    return false;
                }

                if (seen.Count >= SearchLimit)
                {
                    return false;
                }

                seen.Add(next);
                open.Enqueue(next);
            }
        }

        return true;
    }
}
