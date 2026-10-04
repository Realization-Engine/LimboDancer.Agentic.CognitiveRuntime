using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// One thing a view saw change over a step (pass 31b, design D4): its kind, a line in words, the Location it is marked at, for a move the
/// Location it came from, and how many counters it speaks of.
/// </summary>
public sealed record ReplayChange(string Kind, string Text, BoardLocation? At, BoardLocation? From = null, int Count = 1)
{
    public const string Moved = "moved";
    public const string Gone = "gone";
    public const string Appeared = "appeared";
    public const string Condition = "condition";
}

/// <summary>
/// The difference between a view before a step and the same perspective's view after it (pass 31b, design D4; rulings R23.1 to R23.6): the units
/// that moved, changed condition, appeared, or went, the "?" that appeared, were removed, or moved, the weapons that changed hands or condition, and
/// the entities placed or removed. It reads the two <see cref="GameView"/>s and the step's events the view is entitled to, never the full state, so
/// it says nothing the view may not know: an enemy unit under "?" has no id here, only a "?" at a Location, and what it holds is absent.
/// </summary>
public static partial class ReplayDiff
{
    // The conditions a player follows on the map; the marks of who has fired or moved change with every phase and are left out, and so is the DM
    // that every Rally Phase's end removes.
    private static readonly (string Condition, string Gained, string? Lost)[] Followed =
    [
        (Conditions.Broken, "broke", "rallied"),
        (Conditions.DesperationMorale, "came under DM", null),
        (Conditions.Pinned, "was pinned", null),
        (Conditions.Berserk, "went berserk", "is no longer berserk"),
        (Conditions.Fanatic, "became Fanatic", null),
        (Conditions.Disrupted, "was Disrupted", "is no longer Disrupted"),
        (Conditions.Captured, "was captured", "is no longer a prisoner"),
        (Conditions.Melee, "is held in Melee", "is no longer in Melee"),
        (Conditions.Concealed, "gained \"?\"", "lost its \"?\""),
        (Conditions.Wounded, "was wounded", null),
    ];

    private const int Listed = 8;

    /// <summary>
    /// What <paramref name="after"/> shows that <paramref name="before"/> did not. <paramref name="readable"/> are the step's events the view is
    /// entitled to: they say why a unit went (eliminated, or become other units) where the views alone show only that it is gone.
    /// </summary>
    public static IReadOnlyList<ReplayChange> Of(GameView? before, GameView after, IReadOnlyList<GameEvent> readable) => Of(before, after, readable, null);

    /// <summary>
    /// The same, said with the view's names (pass 31c, design D11): every unit here is one the view holds before or after the step, so each reads
    /// by its name and tag; a Location reads as the game writes it (design D12).
    /// </summary>
    public static IReadOnlyList<ReplayChange> Of(GameView? before, GameView after, IReadOnlyList<GameEvent> readable, UnitNames? names)
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

        // The "?" a side only knows of (A12.11), counted by side and Location, since a presence has no id of its own. What is left of each
        // difference once the units below have claimed their share is said as "?" appearing, being removed, or moving.
        var sealedBefore = before.Sealed.GroupBy(item => (item.Side, item.Location)).ToDictionary(group => group.Key, group => group.Count());
        var sealedAfter = after.Sealed.GroupBy(item => (item.Side, item.Location)).ToDictionary(group => group.Key, group => group.Count());
        var more = sealedAfter.Select(pair => (pair.Key, Count: pair.Value - sealedBefore.GetValueOrDefault(pair.Key))).Where(item => item.Count > 0)
            .ToDictionary(item => item.Key, item => item.Count);
        var fewer = sealedBefore.Select(pair => (pair.Key, Count: pair.Value - sealedAfter.GetValueOrDefault(pair.Key))).Where(item => item.Count > 0)
            .ToDictionary(item => item.Key, item => item.Count);

        foreach (var unit in before.Units.Where(unit => !later.ContainsKey(unit.Id)).OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            var at = Where(before, unit.Id);
            string text;
            if (eliminated.Contains(unit.Id))
            {
                text = $"{unit.Id} was eliminated" + In(at);
            }
            else if (lineage.FirstOrDefault(item => item.Consumed.Contains(unit.Id, StringComparer.Ordinal)) is { } became)
            {
                // Only the units the view holds after the step are named.
                var named = became.Produced.Where(item => later.ContainsKey(item.Id)).Select(item => item.Id + Held(later[item.Id])).ToArray();
                text = named.Length == 0 ? $"{unit.Id} is no longer in this view" + In(at) : $"{unit.Id} {LineageVerb(became.Action)} {string.Join(" and ", named)}" + In(at);
            }
            else if (Claim(more, unit.Side, at))
            {
                // The unit is still there, under a "?" the view cannot see beneath (A12.11).
                text = $"{unit.Id} went under \"?\"" + In(at);
            }
            else
            {
                text = $"{unit.Id} is no longer in this view" + In(at);
            }

            changes.Add(new ReplayChange(ReplayChange.Gone, text, at));
        }

        foreach (var unit in after.Units.OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            var at = Where(after, unit.Id);
            if (!earlier.TryGetValue(unit.Id, out var was))
            {
                // A unit a lineage produced is said with the unit it came from; the hex is still marked. A unit where a "?" stood was revealed.
                changes.Add(new ReplayChange(ReplayChange.Appeared, produced.Contains(unit.Id) ? string.Empty
                    : Claim(fewer, unit.Side, at) ? $"A {DisplayText.Side(unit.Side)} \"?\"{In(at)} was revealed: {unit.Id}" : $"{unit.Id} came into this view" + In(at), at));
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

            if (was.Status != unit.Status && unit.Status == InstanceStatus.Wrecked)
            {
                changes.Add(new ReplayChange(ReplayChange.Condition, $"{unit.Id} was wrecked" + In(at), at));
            }

            if (was.Definition?.Definition != unit.Definition?.Definition && unit.Definition is { } now)
            {
                changes.Add(new ReplayChange(ReplayChange.Condition, $"{unit.Id} is now {now.Definition}" + In(at), at));
            }
        }

        foreach (var side in fewer.Keys.Select(key => key.Side).Concat(more.Keys.Select(key => key.Side)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray())
        {
            var (left, came) = (fewer.Where(item => item.Key.Side == side).ToArray(), more.Where(item => item.Key.Side == side).ToArray());
            if (left.Length == 1 && came.Length == 1 && left[0].Value == came[0].Value)
            {
                changes.Add(new ReplayChange(ReplayChange.Moved, $"{Presences(came[0].Value, side)} moved from {Place(left[0].Key.Location)} to {Place(came[0].Key.Location)}",
                    came[0].Key.Location, left[0].Key.Location, came[0].Value));
                continue;
            }

            changes.AddRange(left.OrderBy(item => item.Key.Location.ToString(), StringComparer.Ordinal).Select(item => new ReplayChange(ReplayChange.Gone,
                $"{Presences(item.Value, side)} {(item.Value == 1 ? "is" : "are")} gone from {Place(item.Key.Location)}", item.Key.Location, null, item.Value)));
            changes.AddRange(came.OrderBy(item => item.Key.Location.ToString(), StringComparer.Ordinal).Select(item => new ReplayChange(ReplayChange.Appeared,
                $"{Presences(item.Value, side)} appeared in {Place(item.Key.Location)}", item.Key.Location, null, item.Value)));
        }

        // The weapons the view holds (ruling R23.1: none held by a unit it may not see): eliminated, passed, left, taken up, malfunctioned, repaired.
        var weaponsBefore = before.Equipment.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var weaponsAfter = after.Equipment.ToDictionary(item => item.Id, StringComparer.Ordinal);
        foreach (var weapon in before.Equipment.Where(item => !weaponsAfter.ContainsKey(item.Id) && eliminated.Contains(item.Id)).OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            changes.Add(new ReplayChange(ReplayChange.Gone, $"{weapon.Id} was eliminated" + In(Where(before, weapon.Id)), Where(before, weapon.Id)));
        }

        foreach (var weapon in after.Equipment.Where(item => weaponsBefore.ContainsKey(item.Id)).OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            var was = weaponsBefore[weapon.Id];
            var at = Where(after, weapon.Id);
            var (held, holds) = (was.Holding?.Holder, weapon.Holding?.Holder);
            if (held != holds)
            {
                changes.Add(new ReplayChange(ReplayChange.Condition, holds is null ? $"{weapon.Id} was left by {held}" + In(at)
                    : held is null ? $"{weapon.Id} was taken up by {holds}" + In(at) : $"{weapon.Id} passed from {held} to {holds}" + In(at), at));
            }

            var (broken, breaks) = (GameState.Condition(was, Conditions.Malfunctioned) == ConditionState.True, GameState.Condition(weapon, Conditions.Malfunctioned) == ConditionState.True);
            if (broken != breaks)
            {
                changes.Add(new ReplayChange(ReplayChange.Condition, $"{weapon.Id} {(breaks ? "malfunctioned" : "was repaired")}" + In(at), at));
            }
        }

        var entitiesBefore = before.Entities.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var entitiesAfter = after.Entities.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        changes.AddRange(after.Entities.Where(item => !entitiesBefore.Contains(item.Id)).OrderBy(item => item.Id, StringComparer.Ordinal)
            .Select(item => new ReplayChange(ReplayChange.Appeared, $"{DisplayText.Kind(item.Kind)} placed" + In(Where(after, item.Id)), Where(after, item.Id))));
        changes.AddRange(before.Entities.Where(item => !entitiesAfter.Contains(item.Id)).OrderBy(item => item.Id, StringComparer.Ordinal)
            .Select(item => new ReplayChange(ReplayChange.Gone, $"{DisplayText.Kind(item.Kind)} removed" + In(Where(before, item.Id)), Where(before, item.Id))));

        // The lines are built with identifiers and said once here: a Location in the game's words, and a unit by the name the view gives it.
        var boards = after.Map.Boards.Count;
        var revision = after.Stamp.Revision;
        string Said(string text) => text.Length == 0 ? text
            : names is not null ? names.Held(text, revision) : LocationId().Replace(text, match => DisplayText.Place(boards, match.Value));
        for (var index = 0; index < changes.Count; index++)
        {
            changes[index] = changes[index] with
            {
                Text = Said(changes[index].Text),
            };
        }

        return changes;
    }

    /// <summary>The lines of the changes, in order, without the changes that only mark a hex.</summary>
    public static IReadOnlyList<string> Lines(IReadOnlyList<ReplayChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var lines = new List<string>();

        // Many counters coming into the view at once (a setup, or the other side's setup coming into sight) are counted, not listed.
        var arrivals = changes.Where(change => change.Kind == ReplayChange.Appeared && change.Text.Length > 0).ToArray();
        if (arrivals.Length > Listed)
        {
            lines.Add($"{arrivals.Sum(change => change.Count)} counters came into this view");
        }

        lines.AddRange(changes.Where(change => change.Text.Length > 0 && (arrivals.Length <= Listed || change.Kind != ReplayChange.Appeared)).Select(change => change.Text));
        return lines;
    }

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
        var drawn = new HashSet<string>(StringComparer.Ordinal);
        foreach (var change in changes.Where(change => change.At is not null))
        {
            // A move inside one hex, between its levels, has no arrow to draw: its hex is outlined. Units moving together share one arrow.
            if (change is { Kind: ReplayChange.Moved, From: { } from } && Centre(board, from) is { } start && Centre(board, change.At!) is { } end && start != end)
            {
                if (drawn.Add($"move {from.Board.Value}:{from.Hex}>{change.At!.Board.Value}:{change.At.Hex}"))
                {
                    marks.Add(Arrow("replay-move", start, end, "#1d4ed8", dashed: false, change.At!.ToString(), from.ToString()));
                }
            }
            else if (drawn.Add($"hex {change.At!.Board.Value}:{change.At.Hex}"))
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

    /// <summary>Takes one from the count of a side's "?" at a Location, when there is one to take.</summary>
    private static bool Claim(Dictionary<(string Side, BoardLocation Location), int> counts, string side, BoardLocation? at)
    {
        if (at is null || !counts.TryGetValue((side, at), out var count))
        {
            return false;
        }

        if (count == 1)
        {
            counts.Remove((side, at));
        }
        else
        {
            counts[(side, at)] = count - 1;
        }

        return true;
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

    /// <summary>What a lineage did to a unit, in a player's words (A7.302, A1.31, A1.32, A19.13).</summary>
    private static string LineageVerb(LineageAction action) => action switch
    {
        LineageAction.Reduced => "was Reduced to",
        LineageAction.Deployed => "Deployed into",
        LineageAction.Recombined => "Recombined into",
        LineageAction.Replaced => "was Replaced by",
        _ => "became",
    };

    /// <summary>The followed conditions a unit a lineage produced holds, such as " (broken)": it has no earlier self to compare with.</summary>
    private static string Held(UnitInstance unit) =>
        Followed.Where(item => GameState.Condition(unit, item.Condition) == ConditionState.True).Select(item => DisplayText.Kind(item.Condition)).ToArray() is { Length: > 0 } held
            ? $" ({string.Join(", ", held)})" : string.Empty;

    private static BoardLocation? Where(GameView view, string id) => view.Locations.TryGetValue(id, out var position) ? position.Location : null;

    private static string Place(BoardLocation? at) => at is null ? "off the map" : at.ToString();

    private static string In(BoardLocation? at) => at is null ? string.Empty : $" in {at}";

    [System.Text.RegularExpressions.GeneratedRegex(@"\bbd\w+:[A-Z]{1,2}\d{1,2}:-?\d\b")]
    private static partial System.Text.RegularExpressions.Regex LocationId();

    private static string Presences(int count, string side) => count == 1 ? $"A {DisplayText.Side(side)} \"?\"" : $"{count} {DisplayText.Side(side)} \"?\"";
}
