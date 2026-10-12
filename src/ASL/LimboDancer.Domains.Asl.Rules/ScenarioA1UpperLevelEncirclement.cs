namespace LimboDancer.Domains.Asl.Rules;

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

    /// <summary>Whether a Location holds "an unbroken, armed, unconcealed enemy unit" of the unit searched for.</summary>
    public bool Held(int location);
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

    /// <summary>
    /// Whether the unit is Encircled by its cut-off path. <paramref name="upperLevelOfBuilding"/> is its own Location's; <paramref name="start"/>
    /// is that Location in the reader's numbering. A search that reaches its limit has found no path.
    /// </summary>
    public static bool Encircled(bool upperLevelOfBuilding, bool berserk, bool heroic, int start, IUpperLevelPathReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (!upperLevelOfBuilding || berserk || heroic)
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
                if (seen.Contains(next) || reader.Held(next))
                {
                    continue;
                }

                if (reader.GroundLevel(next))
                {
                    return false;
                }

                if (seen.Count < SearchLimit)
                {
                    seen.Add(next);
                    open.Enqueue(next);
                }
            }
        }

        return true;
    }
}
