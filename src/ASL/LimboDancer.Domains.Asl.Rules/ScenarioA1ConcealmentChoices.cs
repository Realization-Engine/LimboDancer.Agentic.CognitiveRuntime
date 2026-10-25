namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The owner's and the viewer's choices on a "?", and concealment for vehicles and an Emplaced Gun at setup (pass 70, tasks 70.4 and 70.5; rulings R70.20
/// to R70.22): the voluntary removal of a "?" (A12.14), the viewer's option not to force a loss (A12.14), a vehicle's Concealment Terrain at setup (A12.2),
/// and an Emplaced Gun's HIP out of Concealment Terrain (A12.34). Play reads the state and the map, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1ConcealmentChoices
{
    /// <summary>The kind of the viewer's pending choice (A12.14; ruling R70.21).</summary>
    public const string ConcealmentLossChoice = "concealment-loss";

    /// <summary>
    /// Why a voluntary removal of "?" is refused (A12.14, p. 78: "The owning player can voluntarily remove any concealment at any time during his or his
    /// opponent's Player Turn"; ruling R70.20); null when it may be made: once play has begun, of active units of one side on the map, each under a "?"
    /// or hidden, or a Dummy. The rule's exception, between a Sniper's Random Location DR and the Sniper's relocation (A14.2), is not built: the game
    /// keeps no state between the two.
    /// </summary>
    public static string? RemovalBar(bool setupOnly, int ids, bool anyNotActiveOnMapUnderQuestionMark, int sides)
    {
        if (setupOnly)
        {
            return "play.remove-concealment: a \"?\" is removed voluntarily once play has begun, in either side's Player Turn (A12.14; ruling R70.20)";
        }

        return ids == 0 || anyNotActiveOnMapUnderQuestionMark || sides != 1
            ? "play.remove-concealment: the units are units of one side on the map, each under a \"?\", hidden, or a Dummy (A12.14; ruling R70.20)"
            : null;
    }

    /// <summary>
    /// What a voluntary removal does to one unit (A12.14, A12.11, A12.31; ruling R70.20): a Dummy is removed, being nothing beneath its "?"; a concealed
    /// unit loses its "?"; a hidden unit is placed and revealed in full ("A revealed hidden unit is totally discovered", A12.31).
    /// </summary>
    public static bool RemovalEliminates(bool dummy) => dummy;

    /// <summary>A voluntary removal of "?", in words: the units by their names, each with its hex.</summary>
    public static string RemovalText(IReadOnlyList<string> named, int dummies)
    {
        ArgumentNullException.ThrowIfNull(named);
        var shown = named.Count == 0 ? null
            : $"{string.Join(", ", named)} {(named.Count == 1 ? "gives up its" : "give up their")} \"?\" voluntarily, revealed in full (A12.14, A12.31)";
        var removed = dummies == 0 ? null : $"{dummies} {(dummies == 1 ? "Dummy is" : "Dummies are")} removed (A12.11)";
        return "play.remove-concealment: " + string.Join("; ", new[] { shown, removed }.OfType<string>()) + " (ruling R70.20)";
    }

    /// <summary>
    /// A12.2 (p. 79): "A vehicle that sets up in a woods-road (or orchard-/brush-/grain- road) hex is considered to be in Concealment Terrain for the
    /// purposes of placing OB-designated '?' (12.12) and of using SSR-allowed HIP" (ruling R70.22): a hex with a road at its center and woods, orchard,
    /// brush, or grain (June to September, B15.6) on a hexside Location, read as the map names a woods-road hex (B13.31). Setup only: thereafter the
    /// vehicle is on the road.
    /// </summary>
    public static bool RoadConcealmentHex(bool centerIsRoad, IEnumerable<string?> hexsideLocationTerrains, int? month)
    {
        ArgumentNullException.ThrowIfNull(hexsideLocationTerrains);
        return centerIsRoad && hexsideLocationTerrains.Any(name => name is "Woods" or "Orchard" or "Brush" || (name == "Grain" && month is >= 6 and <= 9));
    }

    /// <summary>
    /// Whether a counter is in Concealment Terrain for setting up under an OB "?" or hidden (A12.12, A12.2, A12.3; ruling R70.22): the terrain's own
    /// list, and for a vehicle also a woods-, orchard-, brush-, or grain-road hex.
    /// </summary>
    public static bool SetupConcealmentTerrain(string? terrain, int? month, bool vehicle, Func<bool> roadConcealmentHex)
    {
        ArgumentNullException.ThrowIfNull(roadConcealmentHex);
        return (terrain is not null && ScenarioA1Definitions.IsConcealmentTerrain(terrain, month)) || (vehicle && roadConcealmentHex());
    }

    /// <summary>
    /// A12.34 (p. 80; ruling R70.22): a hidden Emplaced Gun set up out of Concealment Terrain "must be placed onboard under a '?' as soon as a Good Order
    /// enemy ground unit has a LOS to it regardless of range". In Concealment Terrain it keeps HIP even in an enemy's LOS. The terrain is read only for a
    /// hidden Emplaced Gun, the LOS only out of Concealment Terrain.
    /// </summary>
    public static bool EmplacedHipLost(bool hidden, bool emplaced, Func<bool> concealmentTerrain, Func<bool> seenByGoodOrderEnemy)
    {
        ArgumentNullException.ThrowIfNull(concealmentTerrain);
        ArgumentNullException.ThrowIfNull(seenByGoodOrderEnemy);
        return hidden && emplaced && !concealmentTerrain() && seenByGoodOrderEnemy();
    }

    /// <summary>What the viewer's pending choice asks, in words (A12.14; ruling R70.21): the stack's hex, and what forcing the loss costs.</summary>
    public static string LossChoiceText(string at) =>
        $"A concealed enemy stack in {at} has done something that loses its \"?\" in the LOS of your concealed units only. Take: one of them forfeits its \"?\" "
        + "for a moment, shown to the enemy, and the stack loses its \"?\". Decline: the stack keeps its \"?\" and nothing is shown (A12.14; ruling R70.21).";

    /// <summary>The answer's line (A12.14; ruling R70.21).</summary>
    public static string LossAnswerText(bool forced, string at, IReadOnlyList<string> lost) => forced
        ? $"play.concealment: the viewing side forces the loss; {string.Join(", ", lost)} in {at} {(lost.Count == 1 ? "loses its" : "lose their")} \"?\" (A12.14; ruling R70.21)"
        : $"play.concealment: the viewing side declines to force the loss; the stack in {at} keeps its \"?\" (A12.14; ruling R70.21)";

    /// <summary>A12.14 (ruling R70.21): the projector's check of a momentary forfeiture of "?": active units of one side, each concealed (they stay so).</summary>
    public static bool VerifiedAllowed(IEnumerable<(string? Side, bool Concealed)> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        var read = units.ToArray();
        return read.Length > 0 && read.All(unit => unit.Side is not null && unit.Concealed) && read.Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count() == 1;
    }

    /// <summary>A12.14 (UNIT-STATE-050): the refusal of a momentary forfeiture of "?".</summary>
    public static RecordRefusal VerifiedRefusal() =>
        new("UNIT-STATE-050", "A momentary forfeiture of \"?\" names active concealed units of one side, which stay concealed (A12.14; ruling R70.21).");

    /// <summary>The line of a viewer's momentary forfeiture of "?", shown to the moving side only (A12.14: "to prove that it is not a Dummy"; ruling R70.21).</summary>
    public static string VerifiedText(IReadOnlyList<string> named)
    {
        ArgumentNullException.ThrowIfNull(named);
        return $"{string.Join(", ", named)} {(named.Count == 1 ? "shows itself" : "show themselves")} for a moment to force the loss of the \"?\", then {(named.Count == 1 ? "is" : "are")} "
            + "concealed again (A12.14)";
    }
}
