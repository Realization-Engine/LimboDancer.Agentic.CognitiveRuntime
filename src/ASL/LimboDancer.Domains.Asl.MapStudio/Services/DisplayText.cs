using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Vasl;
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

    /// <summary>A condition that is true, false, or not known, as Yes, No, Unknown, or "Does not apply".</summary>
    public static string YesNo(ConditionState state) => state switch
    {
        ConditionState.True => "Yes",
        ConditionState.False => "No",
        ConditionState.Inapplicable => "Does not apply",
        _ => "Unknown",
    };

    /// <summary>One condition: its name alone when it holds, or its name and state ("broken unknown").</summary>
    public static string Condition(string key, ConditionState state) =>
        state == ConditionState.True ? Kind(key) : $"{Kind(key)} {Conditions.Name(state)}";

    /// <summary>The conditions an object holds or may hold, leaving out those known to be false; "none" when it holds none.</summary>
    public static string ConditionsOf(IGameObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var shown = item.Conditions.Where(pair => pair.Value is not ConditionState.False).Select(pair => Condition(pair.Key, pair.Value)).ToArray();
        return shown.Length == 0 ? "none" : string.Join(", ", shown);
    }

    /// <summary>A fidelity batch outcome; Ingested reads as "Not verified", the name the board's own status uses.</summary>
    public static string Outcome(BatchOutcome outcome) => outcome switch
    {
        BatchOutcome.Verified => "Verified",
        BatchOutcome.Ingested => "Not verified",
        BatchOutcome.Failed => "Failed",
        BatchOutcome.OutOfScope => "Out of scope",
        _ => outcome.ToString(),
    };

    /// <summary>F1, whether VASL's terrain data is read exactly: decoded, encoded again, and compared byte for byte.</summary>
    public static string TerrainData(F1Status? status) => status switch
    {
        null => "Not checked",
        F1Status.Pass => "Pass",
        F1Status.FramingOnly => "Pass, framing differs",
        F1Status.Fail => "Fail",
        _ => status.Value.ToString(),
    };

    /// <summary>F2, whether the hex facts match VASL's own answers for the board.</summary>
    public static string HexFacts(string status) => status switch
    {
        BatchF2.Pass => "Pass",
        BatchF2.Fail => "Fail",
        BatchF2.NoFixture => "No VASL answers to compare",
        _ => status,
    };

    /// <summary>A named fidelity check in words; F3's two checks test the Styled drawing.</summary>
    public static string Check(string name) => name switch
    {
        "exact-outlines" => "Terrain outlines",
        "elevation-outlines" => "Elevation outlines",
        "f3-hexfacts" => "Styled drawing: hex facts",
        "f3-pixels" => "Styled drawing: pixels",
        "hexfacts-svg" => "Hex facts drawing",
        _ when name.EndsWith("-svg", StringComparison.Ordinal) => $"{Side(name[..^4])} drawing",
        _ => name,
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

    /// <summary>
    /// A Location as a player reads it: "F6 on board 01", "F6 on board 01, cellar", or "F6 on board 01, level 1"; the identifier as it was when it
    /// is not a Location.
    /// </summary>
    public static string Location(string location) =>
        BoardLocation.TryParse(location, out var at) ? Location(at) : location;

    public static string Location(BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        var board = location.Board.Value is ['b', 'd', .. var number] && number.Length > 0 ? $"board {number}" : location.Board.Value;
        var level = location.Level switch
        {
            0 => string.Empty,
            -1 => ", cellar",
            var other => $", level {other}",
        };
        return $"{location.Hex} on {board}{level}";
    }
}
