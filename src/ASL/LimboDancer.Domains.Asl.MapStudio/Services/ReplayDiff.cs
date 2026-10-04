using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// One thing a view saw change over a step (pass 31b, design D4): its kind, a line in words, the Location it is marked at, and for a move the
/// Location it came from.
/// </summary>
public sealed record ReplayChange(string Kind, string Text, BoardLocation? At, BoardLocation? From = null)
{
    public const string Moved = "moved";
    public const string Gone = "gone";
    public const string Appeared = "appeared";
    public const string Condition = "condition";
}

/// <summary>
/// The difference between a view before a step and the same perspective's view after it (pass 31b, design D4; rulings R23.1 to R23.6): the units
/// that moved, changed condition, appeared, or went, the "?" that appeared, left, or moved, and the entities placed or removed. It reads the two
/// <see cref="GameView"/>s and the step's events the view is entitled to, never the full state, so it says nothing the view may not know: an enemy
/// unit under "?" has no id here, only a "?" at a Location.
/// </summary>
public static class ReplayDiff
{
    // The conditions a player follows on the map; the marks of who has fired or moved change with every phase and are left out.
    private static readonly (string Condition, string Gained, string? Lost)[] Followed =
    [
        (Conditions.Broken, "broke", "rallied"),
        (Conditions.Pinned, "was pinned", null),
        (Conditions.Berserk, "went berserk", "is no longer berserk"),
        (Conditions.Captured, "was captured", "is no longer a prisoner"),
        (Conditions.Melee, "is held in Melee", "is no longer in Melee"),
        (Conditions.Concealed, "gained \"?\"", "lost its \"?\""),
        (Conditions.Wounded, "was wounded", null),
    ];

    /// <summary>
    /// What <paramref name="after"/> shows that <paramref name="before"/> did not. <paramref name="readable"/> are the step's events the view is
    /// entitled to: they say why a unit went (eliminated, or become other units) where the views alone show only that it is gone.
    /// </summary>
    public static IReadOnlyList<ReplayChange> Of(GameView? before, GameView after, IReadOnlyList<GameEvent> readable)
    {
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(readable);
        var changes = new List<ReplayChange>();
        if (before is null)
        {
            return changes;
        }

        var eliminated = readable.Select(item => item.Payload).OfType<InstanceEliminated>().Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var lineage = readable.Select(item => item.Payload).OfType<LineageRecorded>().ToArray();
        var produced = lineage.SelectMany(item => item.Produced.Select(unit => unit.Id)).ToHashSet(StringComparer.Ordinal);
        var earlier = before.Units.ToDictionary(unit => unit.Id, StringComparer.Ordinal);
        var later = after.Units.ToDictionary(unit => unit.Id, StringComparer.Ordinal);

        foreach (var unit in before.Units.Where(unit => !later.ContainsKey(unit.Id)).OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            var at = Where(before, unit.Id);
            var text = eliminated.Contains(unit.Id) ? $"{unit.Id} was eliminated"
                : lineage.FirstOrDefault(item => item.Consumed.Contains(unit.Id, StringComparer.Ordinal)) is { } became
                    ? $"{unit.Id} became {string.Join(" and ", became.Produced.Select(item => item.Id + Held(later.GetValueOrDefault(item.Id))))}"
                : $"{unit.Id} is no longer in this view";
            changes.Add(new ReplayChange(ReplayChange.Gone, text + In(at), at));
        }

        foreach (var unit in after.Units.OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            var at = Where(after, unit.Id);
            if (!earlier.TryGetValue(unit.Id, out var was))
            {
                // A unit a lineage produced is said with the unit it came from; the hex is still marked.
                changes.Add(new ReplayChange(ReplayChange.Appeared, produced.Contains(unit.Id) ? string.Empty : $"{unit.Id} came into this view" + In(at), at));
                continue;
            }

            var from = Where(before, unit.Id);
            if (from != at)
            {
                changes.Add(new ReplayChange(ReplayChange.Moved, at is null ? $"{unit.Id} left the map from {Place(from)}"
                    : from is null ? $"{unit.Id} came onto the map at {Place(at)}" : $"{unit.Id} moved from {Place(from)} to {Place(at)}", at ?? from, at is null ? null : from));
            }

            foreach (var (condition, gained, lost) in Followed)
            {
                var (had, has) = (GameState.Condition(was, condition) == ConditionState.True, GameState.Condition(unit, condition) == ConditionState.True);
                if (has && !had)
                {
                    changes.Add(new ReplayChange(ReplayChange.Condition, $"{unit.Id} {gained}" + In(at), at));
                }
                else if (had && !has && lost is not null)
                {
                    changes.Add(new ReplayChange(ReplayChange.Condition, $"{unit.Id} {lost}" + In(at), at));
                }
            }

            if (was.Definition?.Definition != unit.Definition?.Definition && unit.Definition is { } now)
            {
                changes.Add(new ReplayChange(ReplayChange.Condition, $"{unit.Id} is now {now.Definition}" + In(at), at));
            }
        }

        // The "?" a side only knows of (A12.11): counted by side and Location, since a presence has no id of its own.
        var sealedBefore = before.Sealed.GroupBy(item => (item.Side, item.Location)).ToDictionary(group => group.Key, group => group.Count());
        var sealedAfter = after.Sealed.GroupBy(item => (item.Side, item.Location)).ToDictionary(group => group.Key, group => group.Count());
        var fewer = sealedBefore.Select(pair => (pair.Key, Count: pair.Value - sealedAfter.GetValueOrDefault(pair.Key))).Where(item => item.Count > 0).ToArray();
        var more = sealedAfter.Select(pair => (pair.Key, Count: pair.Value - sealedBefore.GetValueOrDefault(pair.Key))).Where(item => item.Count > 0).ToArray();
        foreach (var side in fewer.Select(item => item.Key.Side).Concat(more.Select(item => item.Key.Side)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            var (left, came) = (fewer.Where(item => item.Key.Side == side).ToArray(), more.Where(item => item.Key.Side == side).ToArray());
            if (left.Length == 1 && came.Length == 1 && left[0].Count == came[0].Count)
            {
                changes.Add(new ReplayChange(ReplayChange.Moved, $"{Presences(came[0].Count, side)} moved from {Place(left[0].Key.Location)} to {Place(came[0].Key.Location)}",
                    came[0].Key.Location, left[0].Key.Location));
                continue;
            }

            changes.AddRange(left.OrderBy(item => item.Key.Location.ToString(), StringComparer.Ordinal)
                .Select(item => new ReplayChange(ReplayChange.Gone, $"{Presences(item.Count, side)} left {Place(item.Key.Location)}", item.Key.Location)));
            changes.AddRange(came.OrderBy(item => item.Key.Location.ToString(), StringComparer.Ordinal)
                .Select(item => new ReplayChange(ReplayChange.Appeared, $"{Presences(item.Count, side)} appeared in {Place(item.Key.Location)}", item.Key.Location)));
        }

        var entitiesBefore = before.Entities.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var entitiesAfter = after.Entities.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        changes.AddRange(after.Entities.Where(item => !entitiesBefore.Contains(item.Id)).OrderBy(item => item.Id, StringComparer.Ordinal)
            .Select(item => new ReplayChange(ReplayChange.Appeared, $"{DisplayText.Kind(item.Kind)} placed" + In(Where(after, item.Id)), Where(after, item.Id))));
        changes.AddRange(before.Entities.Where(item => !entitiesAfter.Contains(item.Id)).OrderBy(item => item.Id, StringComparer.Ordinal)
            .Select(item => new ReplayChange(ReplayChange.Gone, $"{DisplayText.Kind(item.Kind)} removed" + In(Where(before, item.Id)), Where(before, item.Id))));
        return changes;
    }

    /// <summary>The lines of the changes, in order, without the changes that only mark a hex.</summary>
    public static IReadOnlyList<string> Lines(IReadOnlyList<ReplayChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var lines = new List<string>();

        // Many units coming into the view at once (a setup, or the other side's setup coming into sight) are counted, not listed.
        var arrived = changes.Count(change => change.Kind == ReplayChange.Appeared && change.Text.Length > 0);
        if (arrived > Listed)
        {
            lines.Add($"{arrived} units and \"?\" came into this view");
        }

        lines.AddRange(changes.Where(change => change.Text.Length > 0 && (arrived <= Listed || change.Kind != ReplayChange.Appeared)).Select(change => change.Text));
        return lines;
    }

    private const int Listed = 8;

    /// <summary>
    /// The marks of a step on the map (design D4), as the content of the <c>layer-marks</c> group: an arrow for each move, a line for each fire from
    /// the firers' Location to the target's, and an outline for each hex where something else changed. No mark takes a pointer event.
    /// </summary>
    public static string Marks(StudioBoard board, IReadOnlyList<ReplayChange> changes, IReadOnlyList<ReplayFire> fires)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(fires);
        var marks = new List<string>();
        var outlined = new HashSet<string>(StringComparer.Ordinal);
        foreach (var change in changes.Where(change => change.At is not null))
        {
            // A move inside one hex, between its levels, has no arrow to draw: its hex is outlined.
            if (change is { Kind: ReplayChange.Moved, From: { } from } && Centre(board, from) is { } start && Centre(board, change.At!) is { } end && start != end)
            {
                marks.Add(Arrow("replay-move", start, end, "#1d4ed8", dashed: false, change.At!.ToString(), from.ToString()));
            }
            else if (outlined.Add($"{change.At!.Board.Value}:{change.At.Hex}"))
            {
                marks.Add(GameMaps.HexMark(board, change.At!, "replay-change", "#f59e0b", "#b45309", 4, dashed: false, label: null));
            }
        }

        foreach (var fire in fires)
        {
            if (Centre(board, fire.From) is { } start && Centre(board, fire.To) is { } end)
            {
                marks.Add(start == end ? GameMaps.HexMark(board, fire.To, "replay-fire", "#fecaca", "#b91c1c", 4, dashed: true, label: null)
                    : Arrow("replay-fire", start, end, "#b91c1c", dashed: true, fire.To.ToString(), fire.From.ToString()));
            }
        }

        return string.Concat(marks);
    }

    /// <summary>The centre of the hex holding a Location, in board units; null when the map has no such hex.</summary>
    public static (double X, double Y)? Centre(StudioBoard board, BoardLocation at)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(at);
        if (UnitLibrary.TargetFor(board).Locate(at with { Level = 0 }) is not { } hex)
        {
            return null;
        }

        var vertices = board.Render.Grid.Geometry.Vertices(hex).ToArray();
        return (vertices.Average(point => point.X), vertices.Average(point => point.Y));
    }

    /// <summary>A line with a head at its end, over a white line that keeps it legible on any terrain; it stops short of the hex centres.</summary>
    private static string Arrow(string css, (double X, double Y) from, (double X, double Y) to, string color, bool dashed, string location, string origin)
    {
        var (dx, dy) = (to.X - from.X, to.Y - from.Y);
        var length = Math.Sqrt((dx * dx) + (dy * dy));
        var (ux, uy) = (dx / length, dy / length);
        var inset = Math.Min(14, length / 4);
        var (x1, y1, x2, y2) = (from.X + (ux * inset), from.Y + (uy * inset), to.X - (ux * inset), to.Y - (uy * inset));
        const double head = 13;
        var (bx, by) = (x2 - (ux * head), y2 - (uy * head));
        var points = string.Create(CultureInfo.InvariantCulture,
            $"{x2:0.##},{y2:0.##} {bx - (uy * head * 0.55):0.##},{by + (ux * head * 0.55):0.##} {bx + (uy * head * 0.55):0.##},{by - (ux * head * 0.55):0.##}");
        var line = string.Create(CultureInfo.InvariantCulture, $"x1=\"{x1:0.##}\" y1=\"{y1:0.##}\" x2=\"{bx:0.##}\" y2=\"{by:0.##}\"");
        return $"<g class=\"{css}\" data-location=\"{location}\" data-from=\"{origin}\" pointer-events=\"none\">"
            + $"<line {line} stroke=\"#ffffff\" stroke-width=\"8\" stroke-linecap=\"round\" stroke-opacity=\"0.85\"/>"
            + $"<line {line} stroke=\"{color}\" stroke-width=\"4\" stroke-linecap=\"round\"" + (dashed ? " stroke-dasharray=\"10 7\"" : string.Empty) + "/>"
            + $"<polygon points=\"{points}\" fill=\"{color}\" stroke=\"#ffffff\" stroke-width=\"1.5\"/></g>";
    }

    /// <summary>The followed conditions a unit a lineage produced holds, such as " (broken)": it has no earlier self to compare with.</summary>
    private static string Held(UnitInstance? unit) => unit is null ? string.Empty
        : Followed.Where(item => GameState.Condition(unit, item.Condition) == ConditionState.True).Select(item => DisplayText.Kind(item.Condition)).ToArray() is { Length: > 0 } held
            ? $" ({string.Join(", ", held)})" : string.Empty;

    private static BoardLocation? Where(GameView view, string id) => view.Locations.TryGetValue(id, out var position) ? position.Location : null;

    private static string Place(BoardLocation? at) => at is null ? "off the map" : DisplayText.Location(at);

    private static string In(BoardLocation? at) => at is null ? string.Empty : $" in {DisplayText.Location(at)}";

    private static string Presences(int count, string side) => count == 1 ? $"A {DisplayText.Side(side)} \"?\"" : $"{count} {DisplayText.Side(side)} \"?\"";
}
