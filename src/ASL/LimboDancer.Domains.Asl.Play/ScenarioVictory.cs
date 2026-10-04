using System.Collections.Concurrent;
using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
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
/// the Card Play and Map Studio Redesign Plan, rulings R24.1 to R24.6): Control gained at scenario start from the setup areas (A26.11), during play by
/// armed Good Order Infantry MMC (A26.11, A26.13, A26.14) per building, hex, and Location, and by Mopping Up (A12.153), kept until the enemy gains it, with
/// a vehicle's temporary Control (A26.12); VP (A26.211, A26.212), CVP (A26.22, A26.222), and Exit VP (A26.23); and the card's outcomes in order, else
/// its Avoidance or draw (A26.3). A pure function of the card and the states, so the planner, the Play page, and tests read the same answer.
/// </summary>
public static class ScenarioVictory
{
    private static readonly string[] Mmc = ["asl:squad", "asl:half-squad", "asl:crew"];

    /// <summary>A non-vehicular Gun's VP, even dismantled (A26.212; ruling R24.3).</summary>
    public const int GunVp = 2;

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
        if (card.VictoryConditions.Outcomes is not { } outcomes || card.VictoryConditions.Otherwise is not { } otherwise
            || states.FirstOrDefault(state => state.SetupClosed) is null)
        {
            return null;
        }

        var conditions = outcomes.SelectMany(outcome => outcome.Any).ToArray();
        var buildings = conditions.SelectMany(condition => (condition.Buildings ?? []).Concat(condition.Versus ?? []).Concat(condition.Building is { } one ? [one] : []))
            .Distinct(StringComparer.Ordinal).ToArray();
        var counted = conditions.Where(condition => condition.Type == "control-count").Select(condition => condition.Building!).Distinct(StringComparer.Ordinal).ToArray();
        var (control, holders) = Control(card, states, knownTo, reading, buildings, counted);
        var present = states[^1];

        // Table player, pass 21: a hex a hex count treats as neither's while it is in Melee is shown so.
        if (conditions.Any(condition => condition.Type == "control-count" && condition.MeleeUncontrolled == true))
        {
            foreach (var (id, item) in control.Where(pair => pair.Value.Kind == "hex").ToArray())
            {
                control[id] = item with
                {
                    InMelee = InMelee(present, item.Hexes[0])
                };
            }
        }
        var sides = card.Sides.Select(side => side.Side).ToArray();
        var standing = sides.Select(side =>
        {
            var (cvp, equipment) = Cvp(card, present, side, vp, adjacent, ended, holders);
            return new VictorySide(side, cvp, ExitVp(card, present, side, vp, adjacent, ended, holders), UnbrokenSquads(present, side, knownTo)) { GunAndVehicleCvp = equipment };
        }).ToArray();

        GameResult? Decide(bool immediateOnly)
        {
            foreach (var outcome in outcomes.Where(outcome => !immediateOnly || outcome.Immediate))
            {
                foreach (var condition in outcome.Any)
                {
                    if (Holds(card, condition, control, standing, present) is { } reason)
                    {
                        return new GameResult(outcome.Winner == "draw" ? null : outcome.Winner, reason, Facts(control, standing));
                    }
                }
            }

            return null;
        }

        var atEnd = Decide(false) ?? new GameResult(otherwise == "draw" ? null : otherwise,
            otherwise == "draw" ? "no Victory Condition holds" : "no Victory Condition of the other side holds (A26.3)", Facts(control, standing));
        VictoryConditionLine[] lines = [.. outcomes.SelectMany(outcome => outcome.Any.Select(condition =>
        {
            var reason = Holds(card, condition, control, standing, present);
            return new VictoryConditionLine(outcome.Winner, outcome.Immediate, reason is not null, reason ?? NotMet(card, condition, control, standing, present));
        }))];
        return new VictoryReport([.. control.Values], standing, Decide(true), atEnd)
        {
            Conditions = lines,
            Otherwise = otherwise == "draw" ? null : otherwise,
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
        var (control, _) = Control(card, states, null, new VictoryReading { Levels = levels }, [building], []);
        return control.Values.Where(item => item.Kind == "location").ToDictionary(item => item.Hexes[0] with { Level = item.Level!.Value }, item => item.Side);
    }

    /// <summary>
    /// The Control of each named building, of each Location of its hexes, and of each hex of a building a hex count names (rulings R21.1, R24.1): at
    /// scenario start the side whose setup area holds it alone, or the only side setting up on its board (A26.11; ruling R24.4); then, state by state, a
    /// building secured by Mopping Up (A12.153), and the side whose armed Good Order Infantry MMC occupies it with no armed enemy ground unit there (A26.11:
    /// the Location for a Location, ground level for a hex, A26.13, any level for a building, A26.14), not in Bypass; Dummies neither gain nor prevent it
    /// (A26.15). A vehicle's temporary Control is read from the present (A26.12; ruling R24.5). The side last holding each Gun is kept beside it (A26.222).
    /// </summary>
    private static (Dictionary<string, VictoryControl> Control, Dictionary<string, string> GunHolders) Control(ScenarioCard card, IReadOnlyList<GameState> states,
        string? knownTo, VictoryReading? reading, IReadOnlyList<string> buildings, IReadOnlyList<string> counted)
    {
        IReadOnlyList<int> Levels(BoardLocation hex) => reading?.Levels?.Invoke(hex) is { Count: > 0 } levels ? levels : [0];
        bool IsArmed(UnitInstance unit) => Armed(unit, reading?.ArmedVehicle);
        var tracked = new Dictionary<string, VictoryControl>(StringComparer.Ordinal);
        foreach (var id in buildings)
        {
            var hexes = ScenarioCards.BuildingHexes(card, id)!;
            tracked[id] = new VictoryControl(id, "building", hexes, StartSide(card, hexes));
            foreach (var hex in hexes)
            {
                foreach (var level in Levels(hex))
                {
                    var location = hex with
                    {
                        Level = level
                    };
                    tracked[location.ToString()] = new VictoryControl(location.ToString(), "location", [hex], StartSide(card, [hex])) { Level = level, Building = id };
                }
            }
        }

        foreach (var id in counted)
        {
            foreach (var hex in ScenarioCards.BuildingHexes(card, id)!)
            {
                tracked[HexId(hex)] = new VictoryControl(HexId(hex), "hex", [hex], StartSide(card, [hex])) { Building = id };
            }
        }

        // Ruling R24.6: a fold cached for the same events continues from where it stopped.
        var key = $"{knownTo ?? "*"}|{string.Join(",", buildings)}|{string.Join(",", counted)}";
        var holders = new Dictionary<string, string>(StringComparer.Ordinal);
        var start = 0;
        var ids = reading?.EventIds;
        if (reading?.Cache is { } cache && ids is not null && cache.Folds.TryGetValue(key, out var fold) && fold.Count <= states.Count && fold.Count <= ids.Count
            && ids[fold.Count - 1] == fold.LastEventId)
        {
            tracked = new Dictionary<string, VictoryControl>(fold.Control, StringComparer.Ordinal);
            holders = new Dictionary<string, string>(fold.GunHolders, StringComparer.Ordinal);
            start = fold.Count;
        }

        for (var index = start; index < states.Count; index++)
        {
            var state = states[index];
            foreach (var gun in state.Equipment.Where(item => item.Kind == "asl:gun" && item.Holding is { } holding && state.Unit(holding.Holder) is not null))
            {
                holders[gun.Id] = state.Unit(gun.Holding!.Holder)!.Side;
            }

            if (state.SetupClosed)
            {
                // A12.153 (ruling R24.2): a building secured by Mopping Up gives its side the building and every Location the record lists.
                foreach (var secured in state.Secured.Skip(index > 0 ? states[index - 1].Secured.Count : 0))
                {
                    foreach (var (id, item) in tracked.ToArray())
                    {
                        var held = item.Kind switch
                        {
                            "building" => item.Id == secured.Building,
                            "location" => secured.Locations.Contains(item.Hexes[0] with { Level = item.Level!.Value }),
                            _ => secured.Locations.Contains(item.Hexes[0] with { Level = 0 }),
                        };
                        if (held)
                        {
                            tracked[id] = item with
                            {
                                Side = secured.Side
                            };
                        }
                    }
                }

                foreach (var (id, item) in tracked.ToArray())
                {
                    if (Gainer(state, item, knownTo, IsArmed) is { } side && side != item.Side)
                    {
                        tracked[id] = item with
                        {
                            Side = side
                        };
                    }
                }
            }

            if (reading?.Cache is { } store && ids is not null && index + 1 == Math.Min(reading.CacheUpTo, Math.Min(states.Count, ids.Count)) && index + 1 > start)
            {
                store.Folds[key] = new VictoryFold(index + 1, ids[index], new Dictionary<string, VictoryControl>(tracked, StringComparer.Ordinal),
                    new Dictionary<string, string>(holders, StringComparer.Ordinal));
            }
        }

        // A26.12 (ruling R24.5): an armed vehicle not in Bypass holds its Location while no armed enemy unit is there, and the hex when that is its only Location.
        var present = states[^1];
        foreach (var (id, item) in tracked.Where(pair => pair.Value.Kind != "building").ToArray())
        {
            var level = item.Level ?? 0;
            if (item.Kind == "hex" && Levels(item.Hexes[0]) is not [0])
            {
                continue;
            }

            var there = Inside(present, item.Hexes, knownTo).Where(unit => present.Location(unit.Id)!.Location.Level == level).ToArray();
            string[] holding = [.. there.Where(unit => LiveFire.IsVehicle(unit) && IsArmed(unit) && unit.Straddling is null).Select(unit => unit.Side).Distinct(StringComparer.Ordinal)];
            if (holding is [var vehicleSide] && !there.Any(unit => unit.Side != vehicleSide && IsArmed(unit)) && vehicleSide != item.Side)
            {
                tracked[id] = item with
                {
                    Side = vehicleSide,
                    ByVehicle = true
                };
            }
        }

        return (tracked, holders);
    }

    private static string HexId(BoardLocation hex) => $"{hex.Board.Value}:{hex.Hex}";

    /// <summary>
    /// A26.11 (rulings R21.1, R24.4): each hex's side is the side whose setup areas alone hold it, or, when no area holds it, the only side setting up on
    /// its board; the hexes have that side when every hex has the same one.
    /// </summary>
    private static string? StartSide(ScenarioCard card, IReadOnlyList<BoardLocation> hexes)
    {
        string? Of(BoardLocation hex)
        {
            string[] holding = [.. card.Sides.Where(side => side.Groups.SelectMany(group => group.Areas)
                .Any(area => area.Kind != "entry" && ScenarioSetup.Within(card, area, hex))).Select(side => side.Side)];
            if (holding.Length > 0)
            {
                return holding is [var alone] ? alone : null;
            }

            string[] setting = [.. card.Sides.Where(side => side.Groups.SelectMany(group => group.Areas).Any(area => area.Kind != "entry"
                && (area.Board ?? (card.Boards.Count == 1 ? card.Boards[0].Board : null)) == hex.Board.Value)).Select(side => side.Side)];
            return setting is [var only] ? only : null;
        }

        var sides = hexes.Select(Of).Distinct(StringComparer.Ordinal).ToArray();
        return sides is [{ } side] ? side : null;
    }

    private static bool Is(UnitInstance unit, string condition) => GameState.Condition(unit, condition) == ConditionState.True;

    /// <summary>
    /// A.7, A26.11 (ruling R24.5): an armed ground unit, which prevents the enemy from gaining Control: any unit but a Dummy, a prisoner, or an Unarmed
    /// unit; a vehicle only with an inherent crew and not Abandoned (the Index's Armed), and never a captured one (a deviation: captured vehicles are not
    /// used in play yet).
    /// </summary>
    private static bool Armed(UnitInstance unit, Func<UnitInstance, bool>? armedVehicle) => unit.Kind != UnitKinds.Dummy && !Is(unit, Conditions.Captured)
        && !Is(unit, Conditions.Unarmed) && !(LiveFire.IsVehicle(unit) && (Is(unit, Conditions.Abandoned) || armedVehicle?.Invoke(unit) == false));

    /// <summary>The active units in the hexes, but those a side does not know (ruling R23.4) and Passengers (ruling R26.2).</summary>
    private static UnitInstance[] Inside(GameState state, IReadOnlyList<BoardLocation> hexes, string? knownTo) =>
        [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && state.Aboard(unit.Id) is null && state.Location(unit.Id)?.Location is { } at
            && hexes.Any(hex => hex.Board == at.Board && hex.Hex == at.Hex) && !Undeclared(unit, knownTo))];

    /// <summary>
    /// A.7, A26.11, A26.13, A26.14 (rulings R21.1, R24.1, R24.5): an armed Good Order Infantry MMC, not in Bypass, gains Control (in the Location for a
    /// Location, at ground level for a hex, at any level for a building); any armed enemy ground unit in the same Location, hex, or building prevents it,
    /// but a vehicle in Bypass does not prevent a building's.
    /// </summary>
    private static string? Gainer(GameState state, VictoryControl item, string? knownTo, Func<UnitInstance, bool> armed)
    {
        var inside = Inside(state, item.Hexes, knownTo);
        if (item.Kind == "location")
        {
            inside = [.. inside.Where(unit => state.Location(unit.Id)!.Location.Level == item.Level)];
        }

        var gainers = inside.Where(unit => Mmc.Contains(unit.Kind) && !Is(unit, Conditions.Unarmed) && !Is(unit, Conditions.Broken) && !Is(unit, Conditions.Berserk)
            && !Is(unit, Conditions.Captured) && !Is(unit, Conditions.Melee)
            && (item.Kind != "hex" || state.Location(unit.Id)!.Location.Level == 0)
            && !(state.Movement is { Bypass.Count: > 0 } movement && movement.Movers.Contains(unit.Id, StringComparer.Ordinal))).Select(unit => unit.Side).Distinct(StringComparer.Ordinal).ToArray();
        return gainers is [var side] && !inside.Any(unit => unit.Side != side && armed(unit) && !(item.Kind == "building" && LiveFire.IsVehicle(unit) && unit.Straddling is not null))
            ? side : null;
    }

    /// <summary>An enemy unit a side does not know as such (A26.15; ruling R23.4): concealed or hidden, when the Victory Conditions are read for that side.</summary>
    private static bool Undeclared(UnitInstance unit, string? knownTo) =>
        knownTo is not null && unit.Side != knownTo && (Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden));

    /// <summary>
    /// A side's unbroken squad-equivalents on the map (A16): a squad one, a HS or crew half; not broken, not a prisoner. Read for <paramref name="knownTo"/>,
    /// another side's concealed and hidden units are not counted (ruling R23.4).
    /// </summary>
    public static double UnbrokenSquads(GameState state, string side, string? knownTo = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side && state.Location(unit.Id) is not null && !Is(unit, Conditions.Broken)
            && !Is(unit, Conditions.Captured) && !Undeclared(unit, knownTo)).Sum(unit => unit.Kind switch
            {
                "asl:squad" => 1.0,
                "asl:half-squad" or "asl:crew" => 0.5,
                _ => 0.0,
            });
    }

    /// <summary>Whether an exit counts for one of its side's Exit VP conditions (A26.23): off the condition's edge, from a hex on or adjacent to one it names.</summary>
    private static bool Qualifies(ScenarioCard card, UnitExit exit, string side, Func<BoardLocation, IEnumerable<BoardLocation>> adjacent) =>
        card.VictoryConditions.Outcomes!.SelectMany(outcome => outcome.Any).Any(condition => condition.Type == "exit-vp" && condition.Side == side && condition.Edge == exit.Edge
            && condition.Near!.Select(hex => BoardLocation.Parse(hex + ":0")).Any(near => (near.Board == exit.From.Board && near.Hex == exit.From.Hex)
                || adjacent(near).Any(next => next.Board == exit.From.Board && next.Hex == exit.From.Hex)));

    /// <summary>
    /// Whether a Guard escorting prisoners off an edge is not eliminated for CVP (A20.53, A26.221; referee, pass 25): the side's Friendly Board Edge as the card
    /// names it, or the edge of one of its exit conditions.
    /// </summary>
    public static bool EscortEdge(ScenarioCard card, string side, string edge)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.Sides.FirstOrDefault(item => item.Side == side)?.FriendlyEdge.Edge == edge
            || card.VictoryConditions.Outcomes?.SelectMany(outcome => outcome.Any).Any(condition => condition.Type == "exit-vp" && condition.Side == side && condition.Edge == edge) == true;
    }

    /// <summary>
    /// A26.23: the VP a side has exited through its exit conditions' areas, none for broken Personnel; the enemy units its Guards took off with them count
    /// their normal VP during play and double once the game has ended (A26.222; ruling R25.5). A vehicle counts its VP with its Passengers', and a Gun
    /// towed or pushed off the map its two (A26.212; ruling R26.4).
    /// </summary>
    private static int ExitVp(ScenarioCard card, GameState state, string side, Func<UnitInstance, int> vp, Func<BoardLocation, IEnumerable<BoardLocation>> adjacent,
        bool ended, Dictionary<string, string> holders) =>
        state.Exits.Where(exit => !exit.Broken && !exit.Recalled && state.Unit(exit.Unit) is { Status: InstanceStatus.Exited } unit && (exit.CapturedBy ?? unit.Side) == side
            && Qualifies(card, exit, side, adjacent))
            .Sum(exit => vp(state.Unit(exit.Unit)!) * (exit.CapturedBy is not null && ended ? 2 : 1))
        + (state.Exits.Count(exit => state.Find(exit.Unit) is EquipmentInstance { Kind: "asl:gun", Status: InstanceStatus.Exited } gun && ExitSide(gun, holders) == side
            && Qualifies(card, exit, side, adjacent)) * GunVp);

    /// <summary>The side a Gun belongs to: its own, else the side last holding it (A26.222).</summary>
    private static string? GunSide(EquipmentInstance gun, Dictionary<string, string> holders) => gun.Side ?? holders.GetValueOrDefault(gun.Id);

    /// <summary>The side that took a Gun off the map: the side last holding it (referee, pass 26: a captor's exit is the captor's).</summary>
    private static string? ExitSide(EquipmentInstance gun, Dictionary<string, string> holders) => holders.GetValueOrDefault(gun.Id) ?? gun.Side;

    /// <summary>
    /// A26.22, A26.221, A26.222 (rulings R21.2, R24.3): a side's CVP: the VP of the enemy units eliminated or wrecked, or that left the map other than by
    /// their own exit conditions, of the enemy units and vehicles it holds captured, and of the enemy Guns eliminated or last held by it; captured ones
    /// double once the game has ended. The second value is the part from Guns and vehicles.
    /// </summary>
    private static (int Cvp, int GunsAndVehicles) Cvp(ScenarioCard card, GameState state, string side, Func<UnitInstance, int> vp,
        Func<BoardLocation, IEnumerable<BoardLocation>> adjacent, bool ended, Dictionary<string, string> holders)
    {
        var total = 0;
        var equipment = 0;
        foreach (var unit in state.Units.Where(unit => unit.Side != side && unit.Kind != UnitKinds.Dummy))
        {
            var value = unit.Status switch
            {
                InstanceStatus.Eliminated or InstanceStatus.Wrecked => vp(unit),
                // A20.53, A26.222 (ruling R25.5): a prisoner its Guard took off the map is still held; A26.221: the escorting Guard is not eliminated.
                InstanceStatus.Exited when state.Exits.LastOrDefault(exit => exit.Unit == unit.Id) is { CapturedBy: not null } => vp(unit) * (ended ? 2 : 1),
                // A26.23, A26.221 (ruling R26.4): a vehicle leaving under Recall counts no CVP.
                InstanceStatus.Exited when state.Exits.LastOrDefault(exit => exit.Unit == unit.Id) is { Recalled: true } => 0,
                InstanceStatus.Exited when state.Exits.LastOrDefault(exit => exit.Unit == unit.Id) is not { } exit
                    || (!(exit.Escort && EscortEdge(card, unit.Side, exit.Edge)) && !Qualifies(card, exit, unit.Side, adjacent)) => vp(unit),
                InstanceStatus.Active when Is(unit, Conditions.Captured) => vp(unit) * (ended ? 2 : 1),
                _ => 0,
            };
            total += value;
            equipment += LiveFire.IsVehicle(unit) ? value : 0;
        }

        // A26.212, A26.222 (ruling R24.3): a Gun is captured while the side last to hold it is the enemy's; A26.221 (ruling R26.4): one that left the map other
        // than by its side's exit conditions counts as eliminated.
        foreach (var gun in state.Equipment.Where(item => item.Kind == "asl:gun" && GunSide(item, holders) is { } owner && owner != side))
        {
            var value = gun.Status switch
            {
                InstanceStatus.Eliminated => GunVp,
                InstanceStatus.Active when holders.TryGetValue(gun.Id, out var holder) && holder == side => GunVp * (ended ? 2 : 1),
                // A26.222: a Gun its captor took off the map is still the captor's.
                InstanceStatus.Exited when ExitSide(gun, holders) == side => GunVp * (ended ? 2 : 1),
                InstanceStatus.Exited when state.Exits.LastOrDefault(exit => exit.Unit == gun.Id) is not { } gunExit || !Qualifies(card, gunExit, GunSide(gun, holders)!, adjacent) => GunVp,
                _ => 0,
            };
            total += value;
            equipment += value;
        }

        return (total, equipment);
    }

    /// <summary>Why a condition holds (its reason for the result), or null when it does not.</summary>
    private static string? Holds(ScenarioCard card, ScenarioCardCondition condition, Dictionary<string, VictoryControl> control, IReadOnlyList<VictorySide> standing,
        GameState present)
    {
        var other = card.Sides.First(side => side.Side != condition.Side).Side;
        var mine = standing.Single(item => item.Side == condition.Side);
        var theirs = standing.Single(item => item.Side == other);
        switch (condition.Type)
        {
            case "control-margin":
                var held = condition.Buildings!.Count(id => control[id].Side == condition.Side);
                var lost = condition.Versus!.Count(id => control[id].Side == other);
                return held - lost >= condition.Margin
                    ? $"{condition.Side} Controls {held} of {string.Join(", ", condition.Buildings!)} and {other} {lost} of {string.Join(", ", condition.Versus!)}, a margin of at least {condition.Margin} (A26.14)"
                    : null;
            case "control-count":
                var hexes = ScenarioCards.BuildingHexes(card, condition.Building!)!;
                var melee = condition.MeleeUncontrolled == true ? hexes.Where(hex => InMelee(present, hex)).ToHashSet() : [];
                var count = hexes.Count(hex => !melee.Contains(hex) && control[HexId(hex)].Side == condition.Side);
                return count >= condition.AtLeast
                    ? $"{condition.Side} Controls {count} hexes of building {condition.Building}, at least {condition.AtLeast} (A26.13)"
                    : null;
            case "squad-ratio":
                return mine.UnbrokenSquads > 0 && mine.UnbrokenSquads >= condition.Ratio * theirs.UnbrokenSquads
                    ? $"{condition.Side} has {Number(mine.UnbrokenSquads)} unbroken squad-equivalents against {Number(theirs.UnbrokenSquads)}, at least {Number(condition.Ratio!.Value)} times as many"
                    : null;
            case "sole-unbroken":
                var building = ScenarioCards.BuildingHexes(card, condition.Building!)!;
                string[] there = [.. present.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Kind != UnitKinds.Dummy && !Is(unit, Conditions.Broken)
                    && !Is(unit, Conditions.Captured) && present.Location(unit.Id)?.Location is { } at && building.Any(hex => hex.Board == at.Board && hex.Hex == at.Hex))
                    .Select(unit => unit.Side).Distinct(StringComparer.Ordinal)];
                return there is [var alone] && alone == condition.Side
                    ? $"{condition.Side} alone has an unbroken unit in building {condition.Building}"
                    : null;
            case "exit-vp":
                return mine.ExitVp >= condition.AtLeast
                    ? $"{condition.Side} has exited {mine.ExitVp} Exit VP off the {condition.Edge} edge near {string.Join(", ", condition.Near!)}, at least {condition.AtLeast} (A26.23)"
                    : null;
            case "cvp":
                return mine.Cvp >= condition.AtLeast ? $"{condition.Side} has {mine.Cvp} CVP, at least {condition.AtLeast} (A26.22)" : null;
            default:
                return null;
        }
    }

    /// <summary>Pass 31 (play test P-08): why a condition does not hold, with the same numbers its reason would give.</summary>
    private static string NotMet(ScenarioCard card, ScenarioCardCondition condition, Dictionary<string, VictoryControl> control, IReadOnlyList<VictorySide> standing,
        GameState present)
    {
        var other = card.Sides.First(side => side.Side != condition.Side).Side;
        var mine = standing.Single(item => item.Side == condition.Side);
        var theirs = standing.Single(item => item.Side == other);
        switch (condition.Type)
        {
            case "control-margin":
                var held = condition.Buildings!.Count(id => control[id].Side == condition.Side);
                var lost = condition.Versus!.Count(id => control[id].Side == other);
                return $"{condition.Side} Controls {held} of {string.Join(", ", condition.Buildings!)} and {other} {lost} of {string.Join(", ", condition.Versus!)}, a margin of {held - lost}, short of {condition.Margin} (A26.14)";
            case "control-count":
                var hexes = ScenarioCards.BuildingHexes(card, condition.Building!)!;
                var melee = condition.MeleeUncontrolled == true ? hexes.Where(hex => InMelee(present, hex)).ToHashSet() : [];
                var count = hexes.Count(hex => !melee.Contains(hex) && control[HexId(hex)].Side == condition.Side);
                return $"{condition.Side} Controls {count} hexes of building {condition.Building}, short of {condition.AtLeast} (A26.13)";
            case "squad-ratio":
                return $"{condition.Side} has {Number(mine.UnbrokenSquads)} unbroken squad-equivalents against {Number(theirs.UnbrokenSquads)}, short of {Number(condition.Ratio!.Value)} times as many";
            case "sole-unbroken":
                return $"{condition.Side} is not the only side with an unbroken unit in building {condition.Building}";
            case "exit-vp":
                return $"{condition.Side} has exited {mine.ExitVp} Exit VP off the {condition.Edge} edge near {string.Join(", ", condition.Near!)}, short of {condition.AtLeast} (A26.23)";
            case "cvp":
                return $"{condition.Side} has {mine.Cvp} CVP, short of {condition.AtLeast} (A26.22)";
            default:
                return $"a condition of a kind the game does not read ({condition.Type})";
        }
    }

    /// <summary>Whether units of both sides are in Melee in a hex (A11.15).</summary>
    private static bool InMelee(GameState state, BoardLocation hex) =>
        state.Units.Where(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Melee) && state.Location(unit.Id)?.Location is { } at
            && at.Board == hex.Board && at.Hex == hex.Hex).Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count() > 1;

    private static string Number(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>
    /// The Locations whose Control differs from their building's (ruling R24.7): the only Location rows the standing and the facts show, since the rest
    /// follow their building.
    /// </summary>
    public static IEnumerable<VictoryControl> DifferingLocations(IReadOnlyList<VictoryControl> control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return control.Where(item => item.Kind == "location"
            && control.FirstOrDefault(building => building.Kind == "building" && building.Id == item.Building) is { } building && building.Side != item.Side);
    }

    /// <summary>A Location as the standing names it, such as "bd01:M9 level 1" (ruling R24.7).</summary>
    public static string LocationName(VictoryControl location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return $"{HexId(location.Hexes[0])} {location.Level switch { -1 => "cellar", 0 => "ground level", var level => $"level {level}" }}";
    }

    private static List<string> Facts(Dictionary<string, VictoryControl> control, IReadOnlyList<VictorySide> standing) =>
    [
        .. control.Values.Where(item => item.Kind == "building").Select(item => $"building {item.Id}: {item.Side ?? "not Controlled"}"),
        .. control.Values.Where(item => item.Kind == "hex").GroupBy(item => item.InMelee ? "in Melee, neither's for the card" : item.Side ?? "not Controlled")
            .Select(group => $"hexes {string.Join(", ", group.Select(item => item.Id))}: {group.Key}"),
        .. DifferingLocations([.. control.Values]).Select(item => $"Location {LocationName(item)} of building {item.Building}: {item.Side ?? "not Controlled"}{(item.ByVehicle ? " (a vehicle, for now)" : string.Empty)}"),
        .. standing.Select(side => $"{side.Side}: {side.Cvp} CVP, {side.ExitVp} Exit VP, {Number(side.UnbrokenSquads)} unbroken squad-equivalents"),
    ];
}
