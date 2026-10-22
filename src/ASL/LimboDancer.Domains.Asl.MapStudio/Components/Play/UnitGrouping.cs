namespace LimboDancer.Domains.Asl.MapStudio.Components.Play;

/// <summary>
/// Where a unit or weapon offered in a list stands: its hex in words ("[AA6]"), its Location in words with any level ("[F5], level 1"), which
/// prefixes its name wherever it is offered (the user, 2026-10-21), its hex's sort key, and whether it is a leader.
/// </summary>
public sealed record UnitPlace(string Hex, string Where, (int Width, string Column, int Row) Order, bool Leader);

/// <summary>
/// The user, 2026-10-21: every list that offers units groups them by hex, a leader first in his hex's group. The page cascades
/// <c>PlaceOfUnit</c>, which gives a unit's <see cref="UnitPlace"/> for its id (null for an id that is no unit on the map, such as a weapon or a
/// unit off board); the lists group with it. Hexes go in map order (column, then row); a group keeps the caller's order after its leaders.
/// </summary>
public static class UnitGrouping
{
    /// <summary>
    /// Whether a list may put a unit first as a leader (the Memory and Performance Guidelines: share a fact only when the view may know it): a
    /// leader the view may know, its own side's or one neither concealed nor hidden; the adjudicator knows all. A concealed enemy leader sorted
    /// first would tell the view what the "?" hides.
    /// </summary>
    public static bool KnownLeader(bool leader, bool adjudicator, bool ownSide, bool concealedOrHidden) => leader && (adjudicator || ownSide || !concealedOrHidden);

    /// <summary>
    /// A unit's name under its hex in a list: the other side's unit reads "4-4-7 squad in [W4]" (the user's ruling of 2026-10-22), and a list that
    /// already says "[W4]" does not say it twice.
    /// </summary>
    public static string WithoutPlace(string name, string? where) =>
        where is { Length: > 0 } && name.EndsWith($" in {where}", StringComparison.Ordinal) ? name[..^(where.Length + 4)] : name;

    /// <summary>One hex's units, or, with no hex, the items that stand on none, after the hexes.</summary>
    public sealed record Group<T>(string? Hex, IReadOnlyList<T> Items);

    public static IReadOnlyList<Group<T>> Groups<T>(IEnumerable<T> items, Func<T, string> id, Func<string, UnitPlace?>? place)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(id);
        var list = items as IReadOnlyList<T> ?? [.. items];
        if (place is null || list.Count < 2)
        {
            return [new Group<T>(null, list)];
        }

        var placed = list.Select((item, index) => (Item: item, Index: index, Place: place(id(item)))).ToArray();
        var groups = placed.Where(entry => entry.Place is not null).GroupBy(entry => entry.Place!.Hex, StringComparer.Ordinal)
            .OrderBy(group => group.First().Place!.Order.Width).ThenBy(group => group.First().Place!.Order.Column, StringComparer.Ordinal)
            .ThenBy(group => group.First().Place!.Order.Row)
            .Select(group => new Group<T>(group.Key, [.. group.OrderBy(entry => entry.Place!.Leader ? 0 : 1).ThenBy(entry => entry.Index).Select(entry => entry.Item)]))
            .ToList();
        if (placed.Where(entry => entry.Place is null).Select(entry => entry.Item).ToArray() is { Length: > 0 } elsewhere)
        {
            groups.Add(new Group<T>(null, elsewhere));
        }

        return groups;
    }
}
