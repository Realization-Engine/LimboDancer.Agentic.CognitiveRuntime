namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// What the Play page shares with its typed Locations so a hex clicked on the map can fill them (pass 31c, design D13): the field that is armed,
/// the hex last picked and a count that rises with each pick, the boards of the game's map, and the levels a hex has. A field arms itself and
/// reads the next pick; the page drops the armed field with the view's other drafts, at a hand-over, another game, and another phase.
/// </summary>
public sealed class LocationPicking
{
    /// <summary>The id of the field the next pick fills; null when none is armed.</summary>
    public string? Armed
    {
        get; set;
    }

    /// <summary>The armed field's label, for the page's line that says what the next click does.</summary>
    public string? ArmedLabel
    {
        get; set;
    }

    /// <summary>The hex last picked on the map, as a ground-level Location; null when none is.</summary>
    public string? Picked
    {
        get; set;
    }

    /// <summary>Rises with every pick, so a field tells a new pick from the one it has already taken.</summary>
    public long Serial
    {
        get; set;
    }

    /// <summary>The boards of the game's map, by id, for a Location typed without its board.</summary>
    public Func<IReadOnlyList<string>> Boards { get; set; } = static () => [];

    /// <summary>The levels a counter may stand on in the hex of a Location: the ground alone when the map does not say.</summary>
    public Func<string, IReadOnlyList<int>> Levels { get; set; } = static _ => [0];

    /// <summary>Arms a field by its id and label, or disarms with nulls; the page renders again.</summary>
    public Action<string?, string?> Arm { get; set; } = static (_, _) => { };
}
