using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// How the Studio words values that the game and the boards keep as identifiers (pass 29, the UX analysis of 2026-10-02): a side as its counters
/// name it, a yes-or-no condition, a unit kind without its vocabulary prefix, a board's status, and a Location. The identifiers themselves are
/// unchanged; only what the page shows is worded.
/// </summary>
public static class DisplayText
{
    /// <summary>A side as its counters name it: "german" reads "German".</summary>
    public static string Side(string? side) =>
        string.IsNullOrEmpty(side) ? string.Empty : char.ToUpper(side[0], CultureInfo.InvariantCulture) + side[1..];

    /// <summary>A side in a sentence: "the German side", or "the adjudicator".</summary>
    public static string ViewName(string? name) =>
        name is null ? string.Empty : name == Perspective.AdjudicatorName ? "the adjudicator" : $"the {Side(name)} side";

    /// <summary>A condition that is true, false, or not known, as Yes, No, unknown, or "does not apply".</summary>
    public static string YesNo(ConditionState state) => state switch
    {
        ConditionState.True => "Yes",
        ConditionState.False => "No",
        ConditionState.Inapplicable => "does not apply",
        _ => "unknown",
    };

    /// <summary>A unit or entity kind without its vocabulary prefix: "asl:half-squad" reads "half-squad".</summary>
    public static string Kind(string? kind) => (kind ?? string.Empty).Replace("asl:", string.Empty, StringComparison.Ordinal);

    /// <summary>A board's status in words.</summary>
    public static string BoardStatus(BoardStatus status) => status switch
    {
        MapStudio.Services.BoardStatus.Verified => "Verified",
        MapStudio.Services.BoardStatus.Ingested => "Not verified",
        MapStudio.Services.BoardStatus.Authored => "Authored, with errors",
        MapStudio.Services.BoardStatus.AuthoredValid => "Authored, valid",
        _ => status.ToString(),
    };

    /// <summary>A Location as a player reads it: "F6 on bd01", with its level when it is not ground level; the identifier as it was otherwise.</summary>
    public static string Location(string location) =>
        BoardLocation.TryParse(location, out var at) ? Location(at) : location;

    public static string Location(BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return $"{location.Hex} on {location.Board.Value}" + (location.Level == 0 ? string.Empty : $", level {location.Level}");
    }
}
