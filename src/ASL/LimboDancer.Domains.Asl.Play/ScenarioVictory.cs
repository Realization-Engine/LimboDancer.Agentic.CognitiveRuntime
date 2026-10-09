using System.Collections.Concurrent;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// What a side Controls that the Victory Conditions name (rulings R21.1, R24.1): a building of the card, a hex of one, or a Location of one; null when
/// neither side does. A hex in Melee that a card counts for neither says so (table player, pass 21).
/// </summary>
public sealed record VictoryControl(string Id, string Kind, IReadOnlyList<BoardLocation> Hexes, string? Side)
{
    /// <summary>Whether a hex count of the card treats this hex, in Melee now, as Controlled by neither (ruling R21.3).</summary>
    public bool InMelee
    {
        get; init;
    }

    /// <summary>A Location's level (a cellar -1, ground 0, upper levels above); null for a building or a hex (ruling R24.1).</summary>
    public int? Level
    {
        get; init;
    }

    /// <summary>The building of the card a hex or Location belongs to (ruling R24.1).</summary>
    public string? Building
    {
        get; init;
    }

    /// <summary>Held now by a vehicle's temporary Control (A26.12; ruling R24.5), which reverts when the vehicle leaves.</summary>
    public bool ByVehicle
    {
        get; init;
    }
}

/// <summary>
/// A side's standing for the Victory Conditions (rulings R21.2, R21.3, R24.3): its CVP, Exit VP, and unbroken squad-equivalents, and how many of its CVP
/// come from Guns and vehicles.
/// </summary>
public sealed record VictorySide(string Side, int Cvp, int ExitVp, double UnbrokenSquads)
{
    /// <summary>The CVP the side has for enemy Guns and vehicles eliminated or captured (A26.212, A26.22; ruling R24.3).</summary>
    public int GunAndVehicleCvp
    {
        get; init;
    }
}

/// <summary>
/// The Victory Conditions read against a game (rulings R21.1 to R21.4): the Control of what they name, each side's standing, the result an immediate
/// condition gives now (null when none holds), and the result the game has at its end.
/// </summary>
public sealed record VictoryReport(IReadOnlyList<VictoryControl> Control, IReadOnlyList<VictorySide> Sides, GameResult? Immediate, GameResult AtEnd)
{
    /// <summary>
    /// Pass 31 (play test P-08): every condition of the card with whether it holds and why, in the card's order, so a result can be read from its
    /// conditions and not only from the one that decided it.
    /// </summary>
    public IReadOnlyList<VictoryConditionLine> Conditions { get; init; } = [];

    /// <summary>The winner when no condition holds: a side, or null for a draw.</summary>
    public string? Otherwise
    {
        get; init;
    }
}

/// <summary>One Victory condition read against a game: the side it wins for ("draw" for a draw), whether it ends the game at once, whether it holds, and why.</summary>
public sealed record VictoryConditionLine(string Winner, bool Immediate, bool Holds, string Text);

/// <summary>
/// What the planner adds to a reading of the Victory Conditions (pass 24, rulings R24.1, R24.6): the levels of a hex's Locations, and the Control fold
/// already made for the game, with the ids of the events its states follow and how many of them are stored.
/// </summary>
public sealed class VictoryReading
{
    /// <summary>The levels of a hex's Locations (ruling R24.1); ground level only when not given.</summary>
    public Func<BoardLocation, IReadOnlyList<int>>? Levels
    {
        get; init;
    }

    /// <summary>
    /// Whether a vehicle is armed (the Index's Armed: a vehicle with an inherent crew, not Abandoned; referee, pass 24); a vehicle not Abandoned when not
    /// given.
    /// </summary>
    public Func<UnitInstance, bool>? ArmedVehicle
    {
        get; init;
    }

    /// <summary>The ids of the events the states follow, one per state, so a cached fold is used only for the same events.</summary>
    public IReadOnlyList<string>? EventIds
    {
        get; init;
    }

    /// <summary>How many of the states follow stored events, and so may be cached; the rest are a plan's.</summary>
    public int CacheUpTo
    {
        get; init;
    }

    /// <summary>The game's cache of the Control fold (ruling R24.6).</summary>
    public VictoryCache? Cache
    {
        get; init;
    }
}

/// <summary>
/// A game's Control fold (ruling R24.6): per view, the Control after the stored events it last read and the side that last held each Gun, so a later
/// reading folds only the states added since.
/// </summary>
public sealed class VictoryCache
{
    internal ConcurrentDictionary<string, VictoryFold> Folds { get; } = new(StringComparer.Ordinal);
}

internal sealed record VictoryFold(int Count, string LastEventId, Dictionary<string, VictoryControl> Control, Dictionary<string, string> GunHolders);

/// <summary>
/// The Victory Conditions of a card evaluated over a game's history (A26; pass 21 of the Scenario Card Games Plan, rulings R21.1 to R21.4; pass 24 of
/// the Card Play and Map Studio Redesign Plan, rulings R24.1 to R24.6): since pass 32.j the rules are <see cref="ScenarioA1VictoryCalculator"/>'s; this
/// class reads the card and the states, hands the facts over by state index (the Control fold reads them as it goes), keeps the fold's cache, and maps
/// the verdicts to the records the planner, the Play page, and tests read.
/// </summary>
public static class ScenarioVictory
{
    /// <summary>A non-vehicular Gun's VP, even dismantled (A26.212; ruling R24.3).</summary>
    public const int GunVp = Rules.ScenarioA1Definitions.GunVp;

    /// <summary>
    /// Evaluates a card's Victory Conditions over the states of a game, the last being the present. <paramref name="vp"/> gives a unit's VP (A26.211,
    /// A26.212), and <paramref name="adjacent"/> the hexes adjacent to a Location. <paramref name="ended"/> doubles captured units' VP (A26.222). Null
    /// when the card has no structured Victory Conditions or play has not started. <paramref name="knownTo"/> reads them as that side may know them
    /// (A26.15; pass 23, ruling R23.4): the enemy's concealed and hidden units neither gain nor prevent Control, since their Control need not be declared
    /// until game end, and count for none of its unbroken squad-equivalents. <paramref name="reading"/> gives the levels of a hex and the game's cache.
    /// </summary>
    public static VictoryReport? Evaluate(ScenarioCard card, IReadOnlyList<GameState> states, Func<UnitInstance, int> vp, Func<BoardLocation, IEnumerable<BoardLocation>> adjacent,
        bool ended, string? knownTo = null, VictoryReading? reading = null)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(states);
        ArgumentNullException.ThrowIfNull(vp);
        ArgumentNullException.ThrowIfNull(adjacent);
        if (card.VictoryConditions.Outcomes is not { } outcomes || card.VictoryConditions.Otherwise is null)
        {
            return null;
        }

        var hexes = new Dictionary<string, BoardLocation>(StringComparer.Ordinal);
        var facts = CardFacts(card, reading?.Levels, hexes);

        // Ruling R24.6: a fold cached for the same events continues from where it stopped.
        var conditions = outcomes.SelectMany(outcome => outcome.Any).ToArray();
        var buildings = conditions.SelectMany(condition => (condition.Buildings ?? []).Concat(condition.Versus ?? []).Concat(condition.Building is { } one ? [one] : []))
            .Distinct(StringComparer.Ordinal).ToArray();
        var counted = conditions.Where(condition => condition.Type == "control-count").Select(condition => condition.Building!).Distinct(StringComparer.Ordinal).ToArray();
        var key = $"{knownTo ?? "*"}|{string.Join(",", buildings)}|{string.Join(",", counted)}";
        var ids = reading?.EventIds;
        VictoryFoldFacts fold = new(null, null, 0, 0, null);
        if (reading?.Cache is { } cache)
        {
            var cached = ids is not null && cache.Folds.TryGetValue(key, out var found) && found.Count <= states.Count && found.Count <= ids.Count && ids[found.Count - 1] == found.LastEventId
                ? found
                : null;
            fold = new VictoryFoldFacts(cached?.Control.ToDictionary(pair => pair.Key, pair => Verdict(pair.Value), StringComparer.Ordinal), cached?.GunHolders, cached?.Count ?? 0,
                ids is null ? 0 : Math.Min(reading.CacheUpTo, Math.Min(states.Count, ids.Count)),
                ids is null ? null : (count, control, holders) => cache.Folds[key] = new VictoryFold(count, ids[count - 1],
                    control.ToDictionary(pair => pair.Key, pair => Control(pair.Value, hexes), StringComparer.Ordinal), holders));
        }

        var verdict = ScenarioA1VictoryCalculator.Evaluate(facts, new StateReader(states, vp, adjacent, reading?.ArmedVehicle), ended, knownTo, fold);
        return verdict is null ? null
            : new VictoryReport([.. verdict.Control.Select(item => Control(item, hexes))],
                [.. verdict.Sides.Select(side => new VictorySide(side.Side, side.Cvp, side.ExitVp, side.UnbrokenSquads) { GunAndVehicleCvp = side.GunAndVehicleCvp })],
                verdict.Immediate is { } now ? Result(now) : null, Result(verdict.AtEnd))
            {
                Conditions = [.. verdict.Conditions.Select(line => new VictoryConditionLine(line.Winner, line.Immediate, line.Holds, line.Text))],
                Otherwise = verdict.Otherwise,
            };
    }

    /// <summary>
    /// The Control of each Location of a building of the card as its states leave it (A26.11, A12.153; ruling R24.1), with no view: what Mopping Up reads
    /// for the ground-level Locations its side does not Control.
    /// </summary>
    public static IReadOnlyDictionary<BoardLocation, string?> LocationControl(ScenarioCard card, IReadOnlyList<GameState> states, string building,
        Func<BoardLocation, IReadOnlyList<int>> levels)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(states);
        var hexes = new Dictionary<string, BoardLocation>(StringComparer.Ordinal);
        return ScenarioA1VictoryCalculator.LocationControl(CardFacts(card, levels, hexes), new StateReader(states, _ => 0, _ => [], null), building)
            .ToDictionary(item => hexes[item.Key.HexId] with { Level = item.Key.Level }, item => item.Value);
    }

    /// <summary>
    /// A side's unbroken squad-equivalents on the map (A16): a squad one, a HS or crew half; not broken, not a prisoner. Read for <paramref name="knownTo"/>,
    /// another side's concealed and hidden units are not counted (ruling R23.4).
    /// </summary>
    public static double UnbrokenSquads(GameState state, string side, string? knownTo = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        return ScenarioA1VictoryCalculator.UnbrokenSquads(StateReader.UnitFacts(state, null), side, knownTo);
    }

    /// <summary>
    /// Whether a Guard escorting prisoners off an edge is not eliminated for CVP (A20.53, A26.221; referee, pass 25): the side's Friendly Board Edge as the card
    /// names it, or the edge of one of its exit conditions.
    /// </summary>
    public static bool EscortEdge(ScenarioCard card, string side, string edge)
    {
        ArgumentNullException.ThrowIfNull(card);
        return ScenarioA1VictoryCalculator.EscortEdge(card.Sides.FirstOrDefault(item => item.Side == side)?.FriendlyEdge.Edge,
            card.VictoryConditions.Outcomes?.SelectMany(outcome => outcome.Any).Select(ScenarioCards.ConditionFacts), side, edge);
    }

    /// <summary>
    /// The Locations whose Control differs from their building's (ruling R24.7): the only Location rows the standing and the facts show, since the rest
    /// follow their building.
    /// </summary>
    public static IEnumerable<VictoryControl> DifferingLocations(IReadOnlyList<VictoryControl> control)
    {
        ArgumentNullException.ThrowIfNull(control);
        var byId = control.ToDictionary(item => item.Id, StringComparer.Ordinal);
        return ScenarioA1VictoryCalculator.DifferingLocations([.. control.Select(Verdict)]).Select(item => byId[item.Id]);
    }

    /// <summary>A Location as the standing names it, such as "bd01:M9 level 1" (ruling R24.7).</summary>
    public static string LocationName(VictoryControl location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return ScenarioA1VictoryCalculator.LocationName(HexId(location.Hexes[0]), location.Level);
    }

    private static string HexId(BoardLocation hex) => $"{hex.Board.Value}:{hex.Hex}";

    private static GameResult Result(VictoryResultVerdict result) => new(result.Winner, result.Reason, result.Facts);

    private static ControlVerdict Verdict(VictoryControl item) => new(item.Id, item.Kind, [.. item.Hexes.Select(HexId)], item.Side)
    {
        InMelee = item.InMelee,
        Level = item.Level,
        Building = item.Building,
        ByVehicle = item.ByVehicle,
    };

    private static VictoryControl Control(ControlVerdict item, Dictionary<string, BoardLocation> hexes) => new(item.Id, item.Kind, [.. item.Hexes.Select(id => hexes[id])], item.Side)
    {
        InMelee = item.InMelee,
        Level = item.Level,
        Building = item.Building,
        ByVehicle = item.ByVehicle,
    };

    /// <summary>The card as Rules reads it; <paramref name="hexes"/> gathers every building hex by its id, for the verdicts' way back.</summary>
    private static VictoryCardFacts CardFacts(ScenarioCard card, Func<BoardLocation, IReadOnlyList<int>>? levels, Dictionary<string, BoardLocation> hexes)
    {
        IReadOnlyList<VictoryHexFacts>? BuildingHexes(string id)
        {
            if (ScenarioCards.BuildingHexes(card, id) is not { } found)
            {
                return null;
            }

            foreach (var hex in found)
            {
                hexes[HexId(hex)] = hex;
            }

            return [.. found.Select(hex => new VictoryHexFacts(HexId(hex), hex.Board.Value, hex.Hex.ToString(), hex.Hex.RowNumber))];
        }

        return new VictoryCardFacts([.. card.Sides.Select(side => side.Side)],
            card.VictoryConditions.Outcomes is { } outcomes
                ? [.. outcomes.Select(outcome => (outcome.Winner, outcome.Immediate, (IReadOnlyList<CardConditionFacts>)[.. outcome.Any.Select(ScenarioCards.ConditionFacts)]))]
                : null,
            card.VictoryConditions.Otherwise, side => card.Sides.FirstOrDefault(item => item.Side == side)?.FriendlyEdge.Edge,
            [.. card.Sides.Select(side => (side.Side, (IReadOnlyList<SetupAreaFacts>)[.. side.Groups.SelectMany(group => group.Areas)
                .Select(area => new SetupAreaFacts(area.Id, area.Kind, area.Hexes, area.Board, area.From, area.To, area.Turn, area.Edge, area.Counters, area.MinMmc, area.Concealed))]))],
            card.Boards.Count, card.Boards.Count == 1 ? card.Boards[0].Board : null, BuildingHexes,
            levels is null ? null : id => levels(hexes[id]));
    }

    private static bool Is(UnitInstance unit, string condition) => GameState.Condition(unit, condition) == ConditionState.True;

    /// <summary>The states as the Control fold reads them, by index (pass 32.j): every read is made when Rules asks, as the old body made it.</summary>
    private sealed class StateReader(IReadOnlyList<GameState> states, Func<UnitInstance, int> vp, Func<BoardLocation, IEnumerable<BoardLocation>> adjacent,
        Func<UnitInstance, bool>? armedVehicle) : IVictoryStateReader
    {
        public int Count => states.Count;

        public bool SetupClosed(int state) => states[state].SetupClosed;

        public IEnumerable<(string Gun, string HolderSide)> GunHolders(int state)
        {
            var current = states[state];
            return current.Equipment.Where(item => item.Kind == "asl:gun" && item.Holding is { } holding && current.Unit(holding.Holder) is not null)
                .Select(gun => (gun.Id, current.Unit(gun.Holding!.Holder)!.Side));
        }

        public IEnumerable<(string Building, string Side, IReadOnlyList<string> Locations)> NewlySecured(int state) =>
            states[state].Secured.Skip(state > 0 ? states[state - 1].Secured.Count : 0)
                .Select(secured => (secured.Building, secured.Side, (IReadOnlyList<string>)[.. secured.Locations.Select(at => at.ToString())]));

        public IReadOnlyList<VictoryUnitFacts> Units(int state) => UnitFacts(states[state], armedVehicle);

        public static IReadOnlyList<VictoryUnitFacts> UnitFacts(GameState state, Func<UnitInstance, bool>? armedVehicle) =>
            [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id)?.Location is not null).Select(unit =>
            {
                var at = state.Location(unit.Id)!.Location;
                var vehicle = LiveFire.IsVehicle(unit);
                return new VictoryUnitFacts(unit.Id, unit.Side, unit.Kind, unit.Kind == UnitKinds.Dummy, HexId(at), at.Level, state.Aboard(unit.Id) is not null, Is(unit, Conditions.Unarmed),
                    Is(unit, Conditions.Broken), Is(unit, Conditions.Berserk), Is(unit, Conditions.Captured), Is(unit, Conditions.Melee), Is(unit, Conditions.Abandoned), vehicle,
                    vehicle ? armedVehicle?.Invoke(unit) : null, unit.Straddling is not null,
                    state.Movement is { Bypass.Count: > 0 } movement && movement.Movers.Contains(unit.Id, StringComparer.Ordinal), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden));
            })];

        public IReadOnlyList<VictoryStandingUnitFacts> Standing(int state)
        {
            var current = states[state];
            return [.. current.Units.Select(unit => new VictoryStandingUnitFacts(unit.Id, unit.Side, unit.Kind == UnitKinds.Dummy, Status(unit.Status), Is(unit, Conditions.Captured),
                LiveFire.IsVehicle(unit), () => vp(unit), current.Exits.LastOrDefault(exit => exit.Unit == unit.Id) is { } exit ? Exit(current, exit) : null))];
        }

        public IReadOnlyList<VictoryGunFacts> Guns(int state)
        {
            var current = states[state];
            return [.. current.Equipment.Where(item => item.Kind == "asl:gun")
                .Select(gun => new VictoryGunFacts(gun.Id, gun.Side, Status(gun.Status), current.Exits.LastOrDefault(exit => exit.Unit == gun.Id) is { } exit ? Exit(current, exit) : null))];
        }

        public IReadOnlyList<VictoryExitFacts> Exits(int state)
        {
            var current = states[state];
            return [.. current.Exits.Select(exit => Exit(current, exit))];
        }

        private VictoryExitFacts Exit(GameState state, UnitExit exit) => new(exit.Unit, exit.Broken, exit.Recalled, exit.CapturedBy, exit.Escort, exit.Edge,
            hex => BoardLocation.Parse(hex + ":0") is var near && ((near.Board == exit.From.Board && near.Hex == exit.From.Hex)
                || adjacent(near).Any(next => next.Board == exit.From.Board && next.Hex == exit.From.Hex)),
            state.Unit(exit.Unit) is { Status: InstanceStatus.Exited } unit ? (unit.Side, () => vp(unit)) : null,
            state.Find(exit.Unit) is EquipmentInstance { Kind: "asl:gun", Status: InstanceStatus.Exited } gun ? (gun.Id, gun.Side) : null);

        private static VictoryStatus Status(InstanceStatus status) => status switch
        {
            InstanceStatus.Active => VictoryStatus.Active,
            InstanceStatus.Eliminated => VictoryStatus.Eliminated,
            InstanceStatus.Wrecked => VictoryStatus.Wrecked,
            InstanceStatus.Exited => VictoryStatus.Exited,
            _ => VictoryStatus.Other,
        };
    }
}
