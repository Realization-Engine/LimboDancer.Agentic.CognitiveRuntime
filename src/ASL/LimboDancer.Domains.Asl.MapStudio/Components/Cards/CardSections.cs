using System.Text;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.MapStudio.Components.Cards;

/// <summary>
/// The card editor's sections (the Card editor redesign, 2026-10-31): their keys, their names, and the problems each owns. A problem is routed by
/// the field key it starts with ("card.boards: ..."); a side's problem goes to the side whose group, counter, or name it mentions.
/// </summary>
public static class CardSections
{
    public const string Card = "card";
    public const string Map = "map";
    public const string Turns = "turns";
    public const string Rules = "rules";
    public const string Victory = "victory";
    public const string Json = "json";

    public static string Side(int index) => $"side-{index}";

    /// <summary>The sections in the order the editor lists them.</summary>
    public static IReadOnlyList<string> Keys(CardDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        return [Card, Map, Turns, .. Enumerable.Range(0, draft.Sides.Length).Select(Side), Rules, Victory, Json];
    }

    /// <summary>The key itself when the editor has that section, otherwise the first section.</summary>
    public static string Known(string? key, CardDraft draft) => key is not null && Keys(draft).Contains(key, StringComparer.Ordinal) ? key : Card;

    /// <summary>The section's name in the list; a side is named by its nationality.</summary>
    public static string Label(string key, CardDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        return key switch
        {
            Card => "Card",
            Map => "Map",
            Turns => "Turns",
            Rules => "Special rules",
            Victory => "Victory",
            Json => "JSON",
            _ when SideIndex(key) is { } index && index < draft.Sides.Length => CardText.Side(draft.Sides[index].Side),
            _ => key,
        };
    }

    /// <summary>The side a section key names, or null.</summary>
    public static int? SideIndex(string key) =>
        key.StartsWith("side-", StringComparison.Ordinal) && int.TryParse(key.AsSpan(5), out var index) ? index : null;

    /// <summary>Whether the section's fields pick hexes or read the map, so the map is shown beside it.</summary>
    public static bool UsesMap(string key) => key is Map or Victory || SideIndex(key) is not null;

    /// <summary>The section a problem belongs to, or null when no section can be named for it.</summary>
    public static string? SectionOf(string problem, CardDraft draft)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(draft);
        var colon = problem.IndexOf(':', StringComparison.Ordinal);
        var field = problem.StartsWith("card.", StringComparison.Ordinal) && colon > 5 ? problem[5..colon] : string.Empty;
        return field switch
        {
            "id" or "title" or "date" or "source" or "format" or "catalog" or "json" or "save" => Card,
            "boards" or "north" or "playable" => Map,
            "turns" or "sides" or "defender" => Turns,
            "ssr" or "rules" => Rules,
            "victory" => Victory,

            // The sequential setup's check numbers the groups of both sides and names none of them.
            "setup" when !problem.Contains('\'', StringComparison.Ordinal) => Turns,
            "nation" => Array.FindIndex(draft.Sides, side => side.Side == "axis-minor") is var axis and >= 0 ? Side(axis) : null,
            "san" or "edge" or "elr" or "integrity" or "balance" or "ob" or "setup" or "stealth-designation" => SideOf(problem, draft) is { } index ? Side(index) : null,
            _ => null,
        };
    }

    /// <summary>The problems by section; those no section can be named for are under the empty key.</summary>
    public static Dictionary<string, List<string>> Route(IEnumerable<string> problems, CardDraft draft)
    {
        ArgumentNullException.ThrowIfNull(problems);
        var routed = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var problem in problems)
        {
            var key = SectionOf(problem, draft) ?? string.Empty;
            if (!routed.TryGetValue(key, out var list))
            {
                routed[key] = list = [];
            }

            list.Add(problem);
        }

        return routed;
    }

    /// <summary>Whether a problem names the group or one of its counters.</summary>
    public static bool Names(string problem, GroupDraft group)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(group);
        return problem.Contains($"'{group.Name}'", StringComparison.Ordinal)
            || group.Units.Any(unit => unit.Definition.Length > 0 && problem.Contains($"'{unit.Definition}'", StringComparison.Ordinal));
    }

    /// <summary>A problem as shown beside its field: the message without its field key.</summary>
    public static string Message(string problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        var colon = problem.IndexOf(": ", StringComparison.Ordinal);
        return problem.StartsWith("card.", StringComparison.Ordinal) && colon > 0 ? problem[(colon + 2)..] : problem;
    }

    /// <summary>A card id from a title: lower-case letters and digits, other characters as single hyphens, 64 at most (ruling R22.2).</summary>
    public static string Slug(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        var slug = new StringBuilder(title.Length);
        foreach (var letter in title.ToLowerInvariant())
        {
            if (letter is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                slug.Append(letter);
            }
            else if (slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        var text = slug.ToString().Trim('-');
        return text.Length > 64 ? text[..64].TrimEnd('-') : text;
    }

    private static int? SideOf(string problem, CardDraft draft)
    {
        for (var index = 0; index < draft.Sides.Length; index++)
        {
            var side = draft.Sides[index];
            if (side.Groups.Any(group => Names(problem, group))
                || (side.BalanceUnits ?? []).Any(unit => unit.Definition.Length > 0 && problem.Contains($"'{unit.Definition}'", StringComparison.Ordinal)))
            {
                return index;
            }
        }

        for (var index = 0; index < draft.Sides.Length; index++)
        {
            if (Regex.IsMatch(problem, $@"(?<![\w-]){Regex.Escape(draft.Sides[index].Side)}(?![\w-])", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50)))
            {
                return index;
            }
        }

        return null;
    }
}
