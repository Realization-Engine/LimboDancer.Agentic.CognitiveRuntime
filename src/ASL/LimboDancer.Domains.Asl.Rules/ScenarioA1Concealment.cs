namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A unit as the non-OB "?" scan reads it (A12.12; ruling R23.6): whether it is active, an enemy, a Dummy, broken, hidden, captured, and its Location as an index into the caller's table (null off the map).</summary>
public sealed record NonObUnitFacts(bool Active, bool Enemy, bool Dummy, bool Broken, bool Hidden, bool Captured, int? Location);

/// <summary>A LOS as the non-OB "?" scan reads it: blocked, clear, and its range.</summary>
public sealed record NonObLosFacts(bool Blocked, bool Clear, int Range);

/// <summary>
/// The reads of the non-OB "?" scan (the design's D4): every unit of the game, the hex distance from a Location to the stack's, and the LOS from a Location to
/// the stack's (null when the map cannot give it), each made only when Rules asks.
/// </summary>
public interface INonObConcealmentReader
{
    /// <summary>Every unit of the game, in the state's order.</summary>
    public IReadOnlyList<NonObUnitFacts> Units
    {
        get;
    }

    /// <summary>The hex distance from a Location to the stack's Location, or null when the map cannot give it.</summary>
    public int? Distance(int location);

    /// <summary>The LOS from a Location to the stack's Location, or null when the map cannot give it.</summary>
    public NonObLosFacts? Los(int location);
}

/// <summary>What a non-OB "?" placed at the end of setup does (A12.12; ruling R23.6): the conditions it sets, and that the unit is recorded as non-OB concealed.</summary>
public sealed record SetupConcealVerdict(IReadOnlyList<(UnitCondition Condition, bool Value)> Conditions, bool RecordNonOb);

/// <summary>
/// Concealment at setup (pass 32.j, S10; A12.12, A12.3, A12.32; rulings R23.5, R23.6): the non-OB "?" each side places once both have set up, hidden units
/// placed beneath a "?" in play, how a setup plan looks to the other side, and the projector's record of a non-OB "?". Play reads the state and the map,
/// hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1Concealment
{
    /// <summary>Why no non-OB "?" may be placed now (ruling R23.6): play has started, or a card's setup order is still open; null when they may.</summary>
    public static string? NonObConcealmentBar(bool setupClosed, Func<bool> setupOrderOpen)
    {
        ArgumentNullException.ThrowIfNull(setupOrderOpen);
        if (setupClosed)
        {
            return "play.non-ob-concealment: play has started; a non-OB \"?\" is placed only after both sides set up and before play (A12.12; ruling R23.6)";
        }

        return setupOrderOpen()
            ? "play.non-ob-concealment: a non-OB \"?\" is placed only after both sides have set up (A12.12; ruling R23.6)"
            : null;
    }

    /// <summary>
    /// The Locations where a side may place a non-OB "?" now (A12.12; ruling R23.6), as texts in ordinal order: none while the bar holds; else each of the
    /// side's Locations on the map that <paramref name="locationBarred"/> does not refuse, read in the order given.
    /// </summary>
    public static IReadOnlyList<string> NonObConcealment(Func<bool> barred, Func<IReadOnlyList<string>> ownLocations, Func<int, bool> locationBarred)
    {
        ArgumentNullException.ThrowIfNull(barred);
        ArgumentNullException.ThrowIfNull(ownLocations);
        ArgumentNullException.ThrowIfNull(locationBarred);
        if (barred())
        {
            return [];
        }

        var locations = ownLocations();
        return [.. Enumerable.Range(0, locations.Count).Where(at => !locationBarred(at)).Select(at => locations[at]).OrderBy(at => at, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Why a side may not place a non-OB "?" on its stack at a Location (A12.12; ruling R23.6); null when it may: it has units there, none broken, berserk,
    /// under "?", hidden, or a Dummy, and the Location is out of the LOS of every unbroken enemy ground unit within 16 hexes, or at 17 hexes or more from all
    /// of them (hidden enemy units are not on the board, A12.3). Each enemy Location is read in order; the LOS only within 16 hexes, only until one sees.
    /// </summary>
    public static string? NonObLocationBar(IReadOnlyList<(bool Broken, bool Berserk, bool Dummy, bool Concealed, bool Hidden)> mine, string side, string at, INonObConcealmentReader reader)
    {
        ArgumentNullException.ThrowIfNull(mine);
        ArgumentNullException.ThrowIfNull(reader);
        if (mine.Count == 0)
        {
            return $"play.non-ob-concealment: {side} has no unit at {at} (A12.12; ruling R23.6)";
        }

        if (mine.Any(unit => unit.Broken || unit.Berserk))
        {
            return $"play.non-ob-concealment: a unit of {side} at {at} is broken or berserk, and such a unit never gains \"?\" (A12.12; ruling R23.6)";
        }

        if (mine.Any(unit => unit.Dummy || unit.Concealed || unit.Hidden))
        {
            return $"play.non-ob-concealment: {at} already holds a \"?\" or a hidden unit of {side}; one non-OB \"?\" per stack, never on top of another \"?\" (A12.12; ruling R23.6)";
        }

        foreach (var enemyAt in reader.Units.Where(unit => unit.Active && unit.Enemy && !unit.Dummy && !unit.Broken && !unit.Hidden && !unit.Captured)
            .Select(unit => unit.Location).OfType<int>().Distinct())
        {
            // 17 hexes or more from the enemy unit needs no LOS read.
            if (reader.Distance(enemyAt) is >= 17)
            {
                continue;
            }

            var los = reader.Los(enemyAt);
            if (los is null || (!los.Blocked && los.Range <= 16))
            {
                return los is { Clear: true }
                    ? $"play.non-ob-concealment: {at} is in the LOS of an unbroken enemy ground unit within 16 hexes (A12.12; ruling R23.6)"
                    : $"play.non-ob-concealment: the LOS of an enemy unit within 16 hexes to {at} is not decided (A12.12; ruling R23.6)";
            }
        }

        return null;
    }

    /// <summary>A12.12 (ruling R23.6): a non-OB "?" goes on a game already set up.</summary>
    public static string? NonObGameBar(bool anyEvents, bool hasState) =>
        !anyEvents || !hasState ? "play.non-ob-concealment: a non-OB \"?\" goes on a game already set up (A12.12; ruling R23.6)" : null;

    /// <summary>The non-OB "?" placed, in words: one setup event a unit.</summary>
    public static string NonObConcealmentText(string side, IReadOnlyList<string> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return $"play.non-ob-concealment: the {side} side places {events.Count} {(events.Count == 1 ? "unit" : "units")} under \"?\" (A12.12; ruling R23.6)";
    }

    /// <summary>A12.32 (ruling R23.5): during setup a unit is set up hidden or not; placing it beneath "?" comes with play.</summary>
    public static string? PlaceHiddenSetupBar(bool setupOnly) =>
        setupOnly ? "play.place-hidden: during setup a unit is set up hidden or not; placing it beneath \"?\" comes with play (A12.32; ruling R23.5)" : null;

    /// <summary>A12.32 (ruling R23.5): the units placed beneath "?" are hidden units of one side, on the map.</summary>
    public static string? PlaceHiddenUnitsBar(int ids, bool anyNotActiveHiddenOnMap, int sides) =>
        ids == 0 || anyNotActiveHiddenOnMap || sides != 1 ? "play.place-hidden: the units are hidden units of one side, on the map (A12.32; ruling R23.5)" : null;

    /// <summary>A12.32: a hidden unit placed beneath a "?" is hidden no more and concealed, in that order.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> PlaceHiddenConditions() => [(UnitCondition.Hidden, false), (UnitCondition.Concealed, true)];

    /// <summary>The hidden units placed, in words.</summary>
    public static string PlaceHiddenText(IReadOnlyList<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        return $"play.place-hidden: {string.Join(", ", ids)} placed beneath \"?\" (A12.32; ruling R23.5)";
    }

    /// <summary>
    /// How a plan looks to the other side before play, counter for counter (A2.9, A12.11; ruling R23.3): for each Location, how many "?" stand there, and
    /// for the counters not under "?" the top one, with its facing, and how many lie beneath it. Two plans of one side with the same look cannot be told
    /// apart by the stacks: they differ only in what the other side does not see.
    /// </summary>
    public static string PlanLook(IEnumerable<(string At, bool Concealed, bool Dummy, string? Definition, string? Facing)> standing)
    {
        ArgumentNullException.ThrowIfNull(standing);
        return string.Join(';', standing.GroupBy(item => item.At, StringComparer.Ordinal).OrderBy(stack => stack.Key, StringComparer.Ordinal).Select(stack =>
        {
            var open = stack.Where(item => !item.Concealed && !item.Dummy).ToArray();
            var top = open.Length > 0 ? $"{open[0].Definition}/{open[0].Facing}+{open.Length - 1}" : string.Empty;
            return $"{stack.Key}={stack.Count() - open.Length}?{top}";
        }));
    }

    /// <summary>A non-OB "?" placed at the end of setup (A12.12; ruling R23.6): the unit is concealed, and recorded as such.</summary>
    public static SetupConcealVerdict SetupConceal() => new([(UnitCondition.Concealed, true)], true);
}
