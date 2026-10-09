namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Two players who wish to play the same side (A26.4): the side, the players in the order the start names them, and the other side.</summary>
public sealed record BalanceRollFacts(string Wanted, string First, string Second, string Other);

/// <summary>The Balance as the start's words give it (A26.4; ruling R20.3): the side that takes it, the players who wish different sides, the dr two players wanting one side need, or the refusal.</summary>
public sealed record BalanceVerdict(string? Balance, IReadOnlyList<(string Name, string Side)> Players, BalanceRollFacts? Roll, string? Reason);

/// <summary>
/// The game's start and its setup (pass 32.i, S2): the setup's steps, a start from a card (rulings R18.1 to R18.3), the Balance and the first-move drs
/// (rulings R20.2, R20.3), and Bore Sighting at setup (C6.41, C6.42; ruling R8.8). Play parses the request and reads the card library, the state, and the
/// map, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1GameStart
{
    /// <summary>Play has started, so no more units can be set up.</summary>
    public static string? SetupClosedBar(bool playStarted) => playStarted ? "play.setup-closed: play has started, so no more units can be set up" : null;

    /// <summary>A setup places at least one unit.</summary>
    public static string? NothingToSetUpBar(int eventCount) => eventCount == 0 ? "play.nothing-to-set-up" : null;

    /// <summary>Ruling R18.3 (table player, pass 20): a unit names an OB group of its own side.</summary>
    public static bool GroupOfOtherSide(bool namesGroup, bool ownSideKnown, bool ownSideHasGroup) => namesGroup && ownSideKnown && !ownSideHasGroup;

    /// <summary>Ruling R18.3: the refusal, naming whose group it is, or that it is no group of the game.</summary>
    public static string SetupGroupText(string instanceId, string owner, string group, string? holder) =>
        $"play.setup-group: {instanceId} is {owner}'s, and the OB group '{group}' is "
            + (holder is null ? "not a group of this game" : $"{holder}'s") + "; a unit sets up in an OB group of its own side (ruling R18.3)";

    /// <summary>
    /// Ruling R18.3: where a side's OB groups differ in ELR, each of its units names its group, since no side ELR stands in for it; a Dummy, a sniper, a hero,
    /// a crew, and a Commissar are exempt.
    /// </summary>
    public static bool NeedsGroup(bool namesGroup, string kind, bool dummy, bool commissar, bool sideHasGroups, bool sideElrKnown) =>
        !namesGroup && !dummy && kind is not ("asl:sniper" or "asl:hero" or "asl:crew") && !commissar && sideHasGroups && !sideElrKnown;

    /// <summary>Ruling R18.3, in words.</summary>
    public static string GroupText(string unitId, string? side) => $"play.group: {unitId} names no OB group, and the groups of {side} differ in ELR (A19.1; ruling R18.3)";

    /// <summary>C6.41, C6.42: the Scenario Defender's manned Gun Bore Sights a Location outside its hex, in its LOS, within 16 hexes; the LOS is read last, only when the rest holds.</summary>
    public static string? BoreSightBar(bool mannedGunOnMap, bool scenarioDefenders, bool ownLocation, Func<bool> losClearWithin16, string gun)
    {
        ArgumentNullException.ThrowIfNull(losClearWithin16);
        return !mannedGunOnMap || !scenarioDefenders || ownLocation || !losClearWithin16()
            ? $"play.bore-sight: {gun} may Bore Sight only as the Scenario Defender's manned Gun, a Location outside its hex, in its LOS, within 16 hexes (C6.41, C6.42)"
            : null;
    }

    /// <summary>The setup in words.</summary>
    public static string SetupText(int eventCount) => $"play.setup: {eventCount} event(s)";

    /// <summary>Pass 20 (rulings R20.2, R20.3): the start's drs, for the first move and for the Balance, are drawn as the game is written.</summary>
    public static bool StartRollsDue(bool firstMove, bool balance) => firstMove || balance;

    /// <summary>A3.9 (ruling R20.2), in words.</summary>
    public static string FirstMoveText() => "play.first-move: a dr for each side decides which moves first (A3.9; ruling R20.2)";

    /// <summary>A26.4 (ruling R20.3), in words.</summary>
    public static string BalanceRollText() => "play.balance: both players wish to play the same side, so a dr decides it; the other side takes its Balance (A26.4; ruling R20.3)";

    /// <summary>
    /// Rulings R20.2, R20.3: two dice, rerolled while they tie, up to 20 times; the index of the higher (0 the first, 1 the second); after 20 ties the first.
    /// <paramref name="rollAttempt"/> draws and records the attempt it is given, 1 to 20, and returns the two dice.
    /// </summary>
    public static int Higher(Func<int, IReadOnlyList<int>> rollAttempt)
    {
        ArgumentNullException.ThrowIfNull(rollAttempt);
        for (var attempt = 1; attempt <= 20; attempt++)
        {
            var values = rollAttempt(attempt);
            if (values[0] != values[1])
            {
                return values[0] > values[1] ? 0 : 1;
            }
        }

        return 0;
    }

    /// <summary>A3.9 (ruling R20.2): the higher dr moves first.</summary>
    public static string FirstMover(int higher, string one, string two) => higher == 0 ? one : two;

    /// <summary>A26.4 (ruling R20.3): the higher of two players wanting one side plays it; the other plays the other side with its Balance.</summary>
    public static IReadOnlyList<(string Name, string Side)> BalancePlayers(bool firstWins, BalanceRollFacts roll)
    {
        ArgumentNullException.ThrowIfNull(roll);
        return [(firstWins ? roll.First : roll.Second, roll.Wanted), (firstWins ? roll.Second : roll.First, roll.Other)];
    }

    /// <summary>
    /// A start naming a scenario card (backlog pass 18, rulings R18.1, R18.2): the card must be embedded, unchanged since the request read it, valid against
    /// the catalog, and, when it leaves the first move to a die roll, the request names no side. The library is read in that order, each read only when the
    /// one before passes. Null when the start may go on.
    /// </summary>
    public static string? CardStartBar(string id, string sha256, string? catalogName, Func<string?> currentSha256, Func<bool> validAgainstCatalog, Func<bool> movesFirstUndecided,
        bool firstSideNamed, Func<string?> movesFirstNote)
    {
        ArgumentNullException.ThrowIfNull(currentSha256);
        ArgumentNullException.ThrowIfNull(validAgainstCatalog);
        ArgumentNullException.ThrowIfNull(movesFirstUndecided);
        ArgumentNullException.ThrowIfNull(movesFirstNote);
        if (currentSha256() is not { } current)
        {
            return $"play.scenario: '{id}' is not a scenario card of the game (ruling R18.1)";
        }

        if (current != sha256)
        {
            return $"play.scenario: the card '{id}' has changed since it was read; read it again (ruling R18.2)";
        }

        if (!validAgainstCatalog())
        {
            return $"play.scenario: the card '{id}' is not valid against the catalog '{catalogName}' (ruling R18.1)";
        }

        // Pass 20 (ruling R20.2): the die roll the card leaves the first move to is drawn at the first setup; the start names no winner.
        if (movesFirstUndecided() && firstSideNamed)
        {
            var note = movesFirstNote();
            return $"play.scenario: {note} The game rolls it as it starts, so the start names no winner (A3.9; ruling R20.2)";
        }

        return null;
    }

    /// <summary>A26.4 (ruling R20.3): a player's name, or "the first player" and "the second player" when the start names none.</summary>
    public static string PlayerName(string? name, int index) => name is { Length: > 0 } ? name : index == 0 ? "the first player" : "the second player";

    /// <summary>
    /// The start's Balance (A26.4; ruling R20.3): <paramref name="agreed"/> names the side that takes it by agreement; <paramref name="players"/> names two
    /// players and the side each wishes to play: the same side for both is decided by a dr, the other side then taking its Balance; different sides, none.
    /// Null players means the start names none.
    /// </summary>
    public static BalanceVerdict Balance(IReadOnlyList<string> sides, string? agreed, IReadOnlyList<(string Name, string? Wants)>? players)
    {
        ArgumentNullException.ThrowIfNull(sides);
        var balance = agreed;
        IReadOnlyList<(string Name, string Side)> playing = [];
        BalanceRollFacts? wanted = null;
        if (players is { } named)
        {
            if (named.Count != 2 || named.Any(item => !sides.Contains(item.Wants, StringComparer.Ordinal)) || named[0].Name == named[1].Name
                || (balance is not null && named[0].Wants == named[1].Wants))
            {
                return new BalanceVerdict(null, [], null,
                    "play.balance: the Balance names two players by different names, each with a side of the card they wish to play, and, when they wish different sides, the side that takes it by agreement (A26.4; ruling R20.3)");
            }

            if (named[0].Wants == named[1].Wants)
            {
                var other = sides.First(item => item != named[0].Wants);
                wanted = new BalanceRollFacts(named[0].Wants!, named[0].Name, named[1].Name, other);
                balance = other;
            }
            else
            {
                playing = [.. named.Select(item => (item.Name, item.Wants!))];
            }
        }

        if (balance is not null && !sides.Contains(balance, StringComparer.Ordinal))
        {
            return new BalanceVerdict(null, [], null, $"play.balance: '{balance}' is not a side of the card (A26.4; ruling R20.3)");
        }

        return new BalanceVerdict(balance, playing, wanted, null);
    }
}
