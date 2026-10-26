using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The Sniper counter at setup (A14.01, A14.2; pass 71, increment 4c, ruling R71.11): a side of a game from a card with a SAN of 2 or more places its counter
/// in the setup's Sniper step once both sides have set up, the DEFENDER first, and play does not start until it has; a game made without a card places it by
/// hand with the other counters. Either way the hex is checked by A14.2 from the placing side's view. A game whose setup is closed is never given a counter.
/// The rules are <see cref="ScenarioA1Sniper"/>'s; this file reads the map and the state and writes the counter's <c>instance-created</c>.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// The hexes a side's Sniper counter may be placed in now (A14.2; ruling R71.11): unoccupied, within six hexes of the most enemy-occupied hexes any
    /// unoccupied hex has, at most six, inside the card's playable area, counted from the side's view; at ground level, in the map's order. Read once a state
    /// and side.
    /// </summary>
    public IReadOnlyList<BoardLocation> SniperPlacements(GameState state, string side)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(side);
        return SniperPlacement(state, side).Hexes;
    }

    /// <summary>
    /// The sides of a game from a card still in setup that owe their Sniper counter before play starts (A14.01, A14.2; ruling R71.11), the DEFENDER first (the
    /// Scenario Defender, else the side that does not move first); empty for a game made without a card, a minimal card's, and once play has started.
    /// </summary>
    public IReadOnlyList<string> SniperPlacementsDue(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.SetupClosed || CardOf(state) is not { Minimal: false })
        {
            return [];
        }

        var sides = SniperSetupSides(state);
        return ScenarioA1Sniper.PlacementsDue(sides, ScenarioA1Sniper.PlacementDefender(state.ScenarioDefender, state.FirstSide, sides));
    }

    /// <summary>
    /// The sides with a SAN of 2 or more and no Sniper counter once play has started (A14.01; ruling R71.11), for the page to say once; empty before play
    /// starts. The SAN and the counter are public.
    /// </summary>
    public static IReadOnlyList<string> SniperCounterMissing(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.SetupClosed ? ScenarioA1Sniper.NoCounter(SniperSetupSides(state)) : [];
    }

    private static SniperSetupSide[] SniperSetupSides(GameState state) =>
        [.. state.Sides.Select(side => new SniperSetupSide(side.Id, side.San, state.Entities.Any(item => item.Kind == "asl:sniper" && item.Side == side.Id)))];

    private (IReadOnlyList<BoardLocation> Hexes, int Tier) SniperPlacement(GameState state, string side) =>
        MemoOf(state).SniperPlacements.GetOrAdd(side, static (key, read) => read.Planner.ReadSniperPlacement(read.State, key), (Planner: this, State: state));

    /// <summary>
    /// A14.2's placement read from the map and the index: the playable hexes at ground level are the candidates, then the hexes of the units on the map, each
    /// unit with whether it is an enemy and hidden from the placing side; Rules decides.
    /// </summary>
    private (IReadOnlyList<BoardLocation> Hexes, int Tier) ReadSniperPlacement(GameState state, string side)
    {
        // The card is read once (the review of pass 32.j: the library checks a user card's file on every read).
        var card = state.Scenario is not null ? CardOf(state) : null;
        var table = new List<BoardLocation>();
        var indexes = new Dictionary<(BoardRef Board, HexName Hex), int>();
        foreach (var placed in state.Map.Boards)
        {
            if (Handle(state, placed.Board) is not { } handle)
            {
                continue;
            }

            foreach (var hex in handle.Geometry.Hexes())
            {
                var at = new BoardLocation(placed.Board, handle.Geometry.NameOf(hex), 0);
                if ((card is null || ScenarioCards.Playable(card, at)) && indexes.TryAdd((at.Board, at.Hex), table.Count))
                {
                    table.Add(at);
                }
            }
        }

        var candidates = table.Count;
        var index = MemoOf(state).Objects(state);
        var units = new List<SniperPlacementUnit>();
        foreach (var unit in state.Units)
        {
            if (unit.Status != InstanceStatus.Active || index.Location(unit.Id) is not { } position)
            {
                continue;
            }

            var key = (position.Location.Board, position.Location.Hex);
            if (!indexes.TryGetValue(key, out var hex))
            {
                hex = table.Count;
                indexes[key] = hex;
                table.Add(new BoardLocation(key.Board, key.Hex, 0));
            }

            var enemy = unit.Side != side;
            units.Add(new SniperPlacementUnit(hex, enemy, enemy && Is(unit, Conditions.Hidden)));
        }

        var verdict = ScenarioA1Sniper.Placements(candidates, units, (one, two) => HexDistance(state, table[one], table[two]));
        return ([.. verdict.Legal.Select(hex => table[hex])], verdict.Tier);
    }

    /// <summary>A hex in a refusal's words: "[G4]" on a map of one board, its Location on a map of several.</summary>
    private static string SniperHexText(GameState state, BoardLocation at) => state.Map.Boards.Count == 1 ? $"[{at.Hex}]" : SniperHex(at).ToString();

    /// <summary>A hex of a Location, at ground level: the Sniper counter is placed in a hex (A14.2).</summary>
    private static BoardLocation SniperHex(BoardLocation at) => new(at.Board, at.Hex, 0);

    /// <summary>Why a side's Sniper counter may not be placed in a hex (A14.2; ruling R71.11); null when it may.</summary>
    private string? SniperPlacementBar(GameState state, string side, BoardLocation at)
    {
        var (hexes, tier) = SniperPlacement(state, side);
        var hex = SniperHex(at);
        return ScenarioA1Sniper.PlacementBar(hexes.Contains(hex), side, SniperHexText(state, hex), tier);
    }

    /// <summary>
    /// The Sniper counters a setup proposal places by hand (A14.2; ruling R71.11): refused in a game from a card, which places them in the Sniper step;
    /// in any other game each hex is checked against the state with the proposal's counters placed. Null when every one may stand.
    /// </summary>
    private string? HandSniperBar(GameState after, HashSet<string> placedNow)
    {
        foreach (var sniper in after.Entities.Where(item => item.Kind == "asl:sniper" && item.Status == InstanceStatus.Active && item.Side is not null && placedNow.Contains(item.Id)))
        {
            if (ScenarioA1Sniper.CardPlacementBar(CardOf(after) is { Minimal: false }) is { } cardBar)
            {
                return cardBar;
            }

            if (MemoOf(after).Objects(after).Location(sniper.Id) is { } position && SniperPlacementBar(after, sniper.Side!, position.Location) is { } bar)
            {
                return bar;
            }
        }

        return null;
    }

    /// <summary>
    /// The setup's Sniper step (A14.2; ruling R71.11): <c>sniper</c> names a side and a hex; once both sides of a card have set up, the side that owes its
    /// counter next (the DEFENDER first) places it in a legal hex. The counter is public, so its record is seen by both sides.
    /// </summary>
    private GamePlan PlanSniperPlacement(GameScope scope, JsonElement sniper, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (existing.Count == 0 || Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.sniper-placement: a Sniper counter goes on a game already set up (A14.2; ruling R71.11)");
        }

        if (sniper.ValueKind != JsonValueKind.Object || !sniper.TryGetProperty("side", out var sideElement) || sideElement.ValueKind != JsonValueKind.String
            || state.Side(sideElement.GetString()!) is null || !sniper.TryGetProperty("at", out var atElement) || atElement.ValueKind != JsonValueKind.String
            || !BoardLocation.TryParse(atElement.GetString()!, out var at))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: sniper names a side of the game and a hex of the map");
        }

        var side = sideElement.GetString()!;
        var cardGame = CardOf(state) is { Minimal: false };
        if (ScenarioA1Sniper.PlacementTurnBar(side, cardGame, cardGame && CardSetup(state, new HashSet<string>()) is { CurrentOrder: not null },
            cardGame ? SniperPlacementsDue(state) : []) is { } turnBar)
        {
            return Refused(scope, label, expected, turnBar);
        }

        var hex = SniperHex(at);
        if (SniperPlacementBar(state, side, hex) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        var instance = new JsonObject
        {
            ["id"] = $"sniper-{side}",
            ["kind"] = "asl:sniper",
            ["side"] = side,
            ["position"] = new JsonObject { ["at"] = hex.ToString() },
            ["conditions"] = new JsonObject(),
        };
        var parsed = Parse(scope, new JsonArray(EventNode(1, "instance-created", new JsonObject { ["instance"] = instance }, visibility: null)), attemptId, expected);
        return parsed.Events is not { } events
            ? Refused(scope, label, expected, [.. parsed.Reasons])
            : new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [ScenarioA1Sniper.PlacementText(side, SniperHexText(state, hex))]);
    }
}
