namespace LimboDancer.Domains.Asl.Rules;

/// <summary>One unit in a Bypassed Location as its loss of concealment reads it (A12.151, A12.42): active, its side, captured, concealed, hidden, a Dummy.</summary>
public sealed record BypassLocationUnitFacts(string Id, bool Active, string? Side, bool Captured, bool Concealed, bool Hidden, bool Dummy);

/// <summary>The units of a Bypassed Location that lose their "?": the real units revealed and the Dummies removed, each in the order given.</summary>
public sealed record BypassRevealVerdict(IReadOnlyList<string> Revealed, IReadOnlyList<string> DummiesRemoved)
{
    /// <summary>Whether any unit loses its "?".</summary>
    public bool Any => Revealed.Count > 0 || DummiesRemoved.Count > 0;

    /// <summary>No unit loses its "?".</summary>
    public static BypassRevealVerdict None { get; } = new([], []);
}

/// <summary>How an attempt to occupy a Bypassed obstacle ends (A12.15, A12.151): it occupies; its Dummies are removed; or it is forced back into its lane.</summary>
public enum ObstacleAttempt
{
    /// <summary>No concealed enemy unit holds the obstacle (or only Dummies, which are removed): the stack occupies it.</summary>
    Occupies,

    /// <summary>The moving stack holds no real unit to show, so its Dummies are removed (A12.15).</summary>
    MoversRemoved,

    /// <summary>A real concealed or hidden enemy unit holds it: every "?" there is lost and the stack stays in its lane, the MF spent, its MPh over.</summary>
    ForcedBack,
}

/// <summary>The verdict on an attempt to occupy a Bypassed obstacle: how it ends, and the units of the Location that lose their "?" with it.</summary>
public sealed record ObstacleVerdict(ObstacleAttempt Attempt, BypassRevealVerdict Reveal);

/// <summary>
/// Bypass and detection (pass 70, task 70.8; ruling R70.23): A12.15's Minimum Move, A12.151's forced back into the lane, a Bypass member broken or pinned,
/// the occupation of an obstacle holding concealed units, and A12.42's vehicle ending its MPh in Bypass. The planner reads the Location's units and hands
/// them over; the reveals are written as <c>concealment-lost</c> events, public as every reveal.
/// </summary>
public static class ScenarioA1BypassDetection
{
    /// <summary>
    /// A12.151 (p. 79): "should Defensive Fire, the presence of other unknown units in the planned exit hex, or other unforeseen circumstances force a
    /// Bypass Infantry unit ... to end its MPh prematurely in the Bypass Location": a stack forced back from its exit is returned to its lane, not to the
    /// obstacle (A12.15, "the last Location occupied").
    /// </summary>
    public static bool KeepsLane(bool forcedBack, bool inBypass) => forcedBack && inBypass;

    /// <summary>
    /// A12.151: a member of a Bypassing Infantry stack broken or pinned by the DEFENDER's fire ends its MPh in the Bypass Location (A4.2, A7.8). Read as the
    /// DEFENDER's window closes; a vehicle's move has its own rule (A12.42).
    /// </summary>
    public static bool EndsInBypass(bool inBypass, bool vehicle, IEnumerable<(bool Active, bool Broken, bool Pinned)> movers)
    {
        ArgumentNullException.ThrowIfNull(movers);
        return inBypass && !vehicle && movers.Any(mover => mover.Active && (mover.Broken || mover.Pinned));
    }

    /// <summary>
    /// A12.151 (p. 79): with the Bypassing stack ended in the Bypass Location "with a concealed enemy unit, all concealment in that Location is lost": every
    /// "?" of both sides there, a Dummy's by its removal. A hidden unit is not concealed and stays hidden, unless the attempt placed it beneath a "?" first
    /// (<paramref name="hiddenPlaced"/>: A12.15, "all hidden Units in the Location must be placed on board beneath a '?'"). The units in
    /// <paramref name="already"/> lost theirs with the step itself (the forced back's movers, A12.15) and are left out.
    /// </summary>
    public static BypassRevealVerdict AllConcealmentLost(IReadOnlyList<BypassLocationUnitFacts> there, string bypassingSide, bool hiddenPlaced, IReadOnlyCollection<string> already)
    {
        ArgumentNullException.ThrowIfNull(there);
        ArgumentNullException.ThrowIfNull(already);
        // "With a concealed enemy unit": a Dummy is no unit (A12.11), so enemy Dummies alone lose nothing (the referee's finding at the reviews); once a
        // real one is there, the Dummies of the Location are removed with the rest.
        bool UnderQuestionMark(BypassLocationUnitFacts unit) => unit.Active && !unit.Captured && (unit.Hidden ? hiddenPlaced : unit.Concealed || unit.Dummy);
        if (!there.Any(unit => unit.Side != bypassingSide && !unit.Dummy && UnderQuestionMark(unit)))
        {
            return BypassRevealVerdict.None;
        }

        BypassLocationUnitFacts[] losing = [.. there.Where(unit => UnderQuestionMark(unit) && !already.Contains(unit.Id))];
        return new BypassRevealVerdict([.. losing.Where(unit => !unit.Dummy).Select(unit => unit.Id)], [.. losing.Where(unit => unit.Dummy).Select(unit => unit.Id)]);
    }

    /// <summary>
    /// An attempt from Bypass to occupy the obstacle (A12.151, p. 78: "The opposing Infantry unit must actually attempt to occupy the concealed unit's
    /// obstacle and Location (not using Bypass) to cause loss of concealment"), read with A12.15 (p. 78): a moving stack of Dummies alone that cannot show a
    /// real unit is removed; an obstacle holding a real concealed or hidden enemy unit forces the stack back into its lane, where it ends its MPh with a
    /// concealed enemy unit, so every "?" of the Location is lost, hidden units placed first (the <paramref name="movers"/> lose theirs with the forced
    /// back); one holding only Dummies loses them and is occupied ("all of his '?' in the Location are removed and the moving unit may continue its move").
    /// A Known enemy unit there is the caller's refusal (A4.14), made before.
    /// </summary>
    public static ObstacleVerdict Occupy(IReadOnlyList<BypassLocationUnitFacts> location, string movingSide, IReadOnlyCollection<string> movers, bool moversAllDummies)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(movers);
        BypassLocationUnitFacts[] enemies = [.. location.Where(unit => unit.Active && !unit.Captured && unit.Side != movingSide && (unit.Concealed || unit.Hidden || unit.Dummy))];
        return enemies.Length == 0 ? new ObstacleVerdict(ObstacleAttempt.Occupies, BypassRevealVerdict.None)
            : moversAllDummies ? new ObstacleVerdict(ObstacleAttempt.MoversRemoved, BypassRevealVerdict.None)
            : enemies.Any(unit => !unit.Dummy) ? new ObstacleVerdict(ObstacleAttempt.ForcedBack, AllConcealmentLost(location, movingSide, true, movers))
            : new ObstacleVerdict(ObstacleAttempt.Occupies, new BypassRevealVerdict([], [.. enemies.Select(unit => unit.Id)]));
    }

    /// <summary>
    /// A12.42 (p. 80): "Concealed unit(s) in the Location are revealed due to the vehicle's presence only voluntarily, or if the vehicle/its-Passenger(s)/
    /// Rider(s) end its MPh in that Location [EXC: a 'broken' vehicle cannot cause concealment loss; 12.1]"; A12.1 (p. 76): a vehicle with "neither an
    /// inherent crew, nor Passenger(s), nor Rider(s), is considered 'broken'". Whether a vehicle in Bypass ending its MPh reveals; the units are then the
    /// concealed enemy units of the Location (<see cref="VehicleReveals"/>). <paramref name="inherentCrew"/> is read only when the rule asks.
    /// </summary>
    public static bool VehicleEndRevealsInBypass(bool inBypass, bool prc, Func<bool> inherentCrew)
    {
        ArgumentNullException.ThrowIfNull(inherentCrew);
        return inBypass && (prc || inherentCrew());
    }

    /// <summary>
    /// A12.42: the concealed enemy units of the vehicle's Location that lose their "?", a Dummy by its removal; hidden units stay hidden (the rule names
    /// concealed ones, and a Bypassing vehicle is not "even ... informed of any hidden unit(s) therein").
    /// </summary>
    public static BypassRevealVerdict VehicleReveals(IReadOnlyList<BypassLocationUnitFacts> there, string vehicleSide)
    {
        ArgumentNullException.ThrowIfNull(there);
        BypassLocationUnitFacts[] losing = [.. there.Where(unit => unit.Active && !unit.Captured && unit.Side != vehicleSide && !unit.Hidden && (unit.Concealed || unit.Dummy))];
        return new BypassRevealVerdict([.. losing.Where(unit => !unit.Dummy).Select(unit => unit.Id)], [.. losing.Where(unit => unit.Dummy).Select(unit => unit.Id)]);
    }

    /// <summary>The sentence a forced back from a Bypass exit adds (A12.151): the stack stays in its lane, and every "?" there is lost when a concealed enemy unit shares it.</summary>
    public static string LaneKeptText(string locationText, bool reveals) => $"; it stays in Bypass in {locationText}, in its lane's terrain (A12.151)"
        + (reveals ? ", and a concealed enemy unit shares that Location, so every \"?\" there is lost" : string.Empty);

    /// <summary>The sentence the DEFENDER's pass adds when a Bypass member broken or pinned ends its MPh with a concealed enemy unit (A12.151).</summary>
    public static string LaneRevealText(string locationText) =>
        $"; a broken or pinned member of the Bypassing stack ends its MPh in {locationText} with a concealed enemy unit, so every \"?\" there is lost (A12.151)";

    /// <summary>The summary when a stack of Dummies alone attempts an obstacle holding concealed enemy units (A12.15).</summary>
    public static string MoversRemovedText(string idsText, string locationText) =>
        $"play.move: {idsText} attempt to occupy the obstacle of {locationText}, holding concealed enemy units; the moving stack shows no real unit, so its Dummies are removed (A12.15)";

    /// <summary>The sentence an occupation adds when only Dummies held the obstacle (A12.15).</summary>
    public static string ObstacleDummiesText(string locationText) => $"; only Dummies held the obstacle of {locationText}, and they are removed (A12.15)";

    /// <summary>The sentence a vehicle's end of move adds when it reveals the concealed units of its Bypass Location (A12.42).</summary>
    public static string VehicleRevealText(string vehicleId, string locationText) =>
        $"; {vehicleId} ends its MPh in Bypass of {locationText}, so the concealed enemy units there lose their \"?\" (A12.42)";
}
