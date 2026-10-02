using LimboDancer.Domains.Asl.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Components.Cards;

/// <summary>The phrases a read card is shown in (K10 and K11, plan section 16.16): its boards, sides, edges, areas, setup order, and citations.</summary>
public static class CardText
{
    private static readonly string[] Ordinals = ["first", "second", "third", "fourth", "fifth", "sixth"];

    /// <summary>The boards as a player lays them out (A2.1): by row from the map's top, and by column when a row holds more than one.</summary>
    public static string Boards(ScenarioCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        var wide = card.Boards.Select(board => board.Column).Distinct().Count() > 1;
        string Slot(int index) => index < Ordinals.Length ? Ordinals[index] : $"{index + 1}th";
        return string.Join("; ", card.Boards.OrderBy(board => board.Row).ThenBy(board => board.Column).Select(board =>
            $"{board.Board}{(card.Boards.Count == 1 ? string.Empty : $" in the {Slot(board.Row)} row from the top")}"
            + $"{(wide ? $", {Slot(board.Column)} from the left" : string.Empty)}{(board.Reversed ? ", turned 180 degrees" : string.Empty)}"));
    }

    /// <summary>The rules a card cites, the project's rulings marked as such.</summary>
    public static string Cites(IEnumerable<string> rules) =>
        string.Join(", ", rules.Select(rule => rule.StartsWith('R') ? $"ruling {rule}" : rule));

    /// <summary>A group's place in a sequential setup: first, second, ..., or last.</summary>
    public static string Order(int order, ScenarioCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        var last = card.Sides.SelectMany(side => side.Groups).Max(group => group.SetupOrder ?? 0);
        return order == last ? "last" : order - 1 < Ordinals.Length ? Ordinals[order - 1] : $"{order}th";
    }

    /// <summary>What a Friendly Board Edge rests on (A20.53; ruling R17.7), as a phrase.</summary>
    public static string Basis(string basis) => basis switch
    {
        "setup" => "from its setup (A20.53)",
        "entry" => "from its entry (A20.53)",
        "ssr" => "by SSR",
        "none" => "not named (ruling R22.3)",
        _ => "manufactured under R0.3",
    };

    public static string Side(string side) => side.Length == 0 ? side : char.ToUpperInvariant(side[0]) + side[1..];

    public static string AreaText(ScenarioCard card, ScenarioCardSetup area)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(area);
        return area.Kind switch
        {
            "building" => $"building {area.Id}{(area.Hexes is { Count: > 1 } hexes ? $" ({string.Join(", ", hexes)})" : string.Empty)}{(area.Board is { } board ? $" on {board}" : string.Empty)}",
            "hex-numbers" => $"hexes numbered {area.From} to {area.To} on {area.Board}",
            "entry" => $"enters on Turn {area.Turn} along the {area.Edge} edge ({ScenarioCards.Compass(card, area.Edge ?? string.Empty)})",
            _ => area.Id,
        };
    }

    public static string Area(ScenarioCard card, ScenarioCardGroup group, ScenarioCardUnit unit)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(unit);
        return unit.Area is { } id && group.Areas.FirstOrDefault(area => area.Id == id) is { } area ? AreaText(card, area)
            : string.Join(", or ", group.Areas.Select(area => AreaText(card, area)));
    }
}
