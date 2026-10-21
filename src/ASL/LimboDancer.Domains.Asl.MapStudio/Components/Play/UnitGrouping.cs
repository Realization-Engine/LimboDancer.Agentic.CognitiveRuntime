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
