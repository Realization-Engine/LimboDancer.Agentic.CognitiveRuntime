namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Victory Conditions (pass 32.j, S10; A26; passes 21 and 24, rulings R21.1 to R21.5, R24.1 to R24.6): the levels of a hex that count for Control, a
/// unit's and a vehicle's VP, the inherent crew, and the game's end at once on an immediate condition. Play reads the state, the catalog, and the map,
/// hands the facts over, and writes the events; the reads a decision makes only on some path cross as delegates.
/// </summary>
public static class ScenarioA1VictoryCalculator
{
    /// <summary>
    /// The levels of a hex's Locations that count for Control (A26.14, B23.41; ruling R24.1): ground 0 and the upper levels, rooftops and cellars left out
    /// (cellars have no other use in the game; table player, pass 24); ground level when the hex is unread.
    /// </summary>
    public static IReadOnlyList<int> HexLevels(IEnumerable<(string? Terrain, int Level)>? locations) =>
        locations is null ? [0] : [.. locations.Where(item => item.Terrain is not "Rooftop" && item.Level >= 0).Select(item => item.Level).Distinct().Order()];

    /// <summary>
    /// A unit's VP (A26.211, A26.212; rulings R21.2, R24.3): a squad or crew two, a HS one, a leader one plus one for each negative leadership modifier (read
    /// only for a leader), a Hero none, a vehicle its own count (read only for a vehicle), and others none.
    /// </summary>
    public static int VictoryPoints(string kind, Func<int?> leadership, Func<int> vehicleVictoryPoints)
    {
        ArgumentNullException.ThrowIfNull(leadership);
        ArgumentNullException.ThrowIfNull(vehicleVictoryPoints);
        return kind switch
        {
            "asl:squad" or "asl:crew" => 2,
            "asl:half-squad" => 1,
            "asl:leader" => 1 + Math.Max(0, -(leadership() ?? 0)),
            "asl:vehicle" => vehicleVictoryPoints(),
            _ => 0,
        };
    }

    /// <summary>
    /// A26.212 (ruling R24.3): one VP, one for a MA not malfunctioned (a MG MA also loses its point to Disabled, a vehicle MG's condition), one per multiple
    /// of five AF of the vehicle's single strongest AF, rounded up (a 0 AF one; an unarmored vehicle none), and the inherent crew's two (A26.211) unless it
    /// has left the vehicle, read last.
    /// </summary>
    public static int VehicleVictoryPoints(bool hasMainArmament, bool mainArmamentIsGun, bool malfunctioned, bool disabled, bool armored, IReadOnlyList<int?> armorFactors, Func<bool> inherentCrew)
    {
        ArgumentNullException.ThrowIfNull(armorFactors);
        ArgumentNullException.ThrowIfNull(inherentCrew);
        var value = 1;
        if (hasMainArmament && !malfunctioned && !(!mainArmamentIsGun && disabled))
        {
            value++;
        }

        if (armored)
        {
            var strongest = armorFactors.Max() ?? 0;
            value += strongest == 0 ? 1 : (strongest + 4) / 5;
        }

        if (inherentCrew())
        {
            value += 2;
        }

        return value;
    }

    /// <summary>A26.211, D5.1 (rulings R24.3, R24.5): an armed vehicle (a MA or any MG) has an inherent crew; an unarmed vehicle only an Inherent Driver.</summary>
    public static bool VehicleArmed(bool mainArmamentIsGun, bool mainArmament, bool antiAircraftMg, bool bowMg, bool coaxialMg) =>
        mainArmamentIsGun || mainArmament || antiAircraftMg || bowMg || coaxialMg;

    /// <summary>A26.211 (rulings R24.3, R24.5): the inherent crew is gone once the vehicle is Abandoned or its crew is a counter of its own (read last).</summary>
    public static bool HasInherentCrew(bool armed, bool abandoned, Func<bool> crewCounterExists)
    {
        ArgumentNullException.ThrowIfNull(crewCounterExists);
        return armed && !abandoned && !crewCounterExists();
    }

    /// <summary>
    /// Ruling R21.4: whether a plan's events are read for an immediate Victory: there are events, the last does not already end the game, the game started from
    /// a card, and that card has an immediate outcome (only such a card is read after every action: Gambit's Exit VP; the card is read only then).
    /// </summary>
    public static bool ImmediateVictoryRead(bool anyEvents, bool lastEndsGame, bool startedFromCard, Func<bool> cardHasImmediateOutcome)
    {
        ArgumentNullException.ThrowIfNull(cardHasImmediateOutcome);
        return anyEvents && !lastEndsGame && startedFromCard && cardHasImmediateOutcome();
    }

    /// <summary>A26.222 (referee, pass 21; UNIT-STATE-045): the game ends at once only with play started and nothing left open: no entry attempt, choice, surrender, or CC.</summary>
    public static bool NothingLeftOpen(bool setupClosed, int openAttempts, bool choicePending, int pendingSurrenders, bool anyOpenCloseCombat) =>
        setupClosed && openAttempts == 0 && !choicePending && pendingSurrenders == 0 && !anyOpenCloseCombat;

    // The Victory Conditions over a game's history (A26, A26.11 to A26.15, A26.211, A26.212, A26.22, A26.221, A26.222, A26.23, A26.3, A12.153, A16, A20.53, A11.15; rulings R21.1 to R21.4, R23.4, R24.1 to R24.7).

    /// <summary>
    /// Evaluates a card's Victory Conditions over the states of a game, the last being the present (A26; rulings R21.1 to R21.4, R24.1 to R24.6). Null when
    /// the card has no structured Victory Conditions or play has not started. <paramref name="knownTo"/> reads them as that side may know them (A26.15;
    /// ruling R23.4). <paramref name="fold"/> carries the cached Control fold and where to store it.
    /// </summary>
    public static VictoryVerdict? Evaluate(VictoryCardFacts card, IVictoryStateReader states, bool ended, string? knownTo, VictoryFoldFacts fold)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(states);
        ArgumentNullException.ThrowIfNull(fold);
        if (card.Outcomes is not { } outcomes || card.Otherwise is not { } otherwise || !Enumerable.Range(0, states.Count).Any(states.SetupClosed))
        {
            return null;
        }

        var conditions = outcomes.SelectMany(outcome => outcome.Any).ToArray();
        var buildings = conditions.SelectMany(condition => (condition.Buildings ?? []).Concat(condition.Versus ?? []).Concat(condition.Building is { } one ? [one] : []))
            .Distinct(StringComparer.Ordinal).ToArray();
        var counted = conditions.Where(condition => condition.Type == "control-count").Select(condition => condition.Building!).Distinct(StringComparer.Ordinal).ToArray();
        var (control, holders) = Control(card, states, knownTo, fold, buildings, counted);
        var present = states.Count - 1;
        var presentUnits = states.Units(present);

        // Table player, pass 21: a hex a hex count treats as neither's while it is in Melee is shown so.
        if (conditions.Any(condition => condition.Type == "control-count" && condition.MeleeUncontrolled == true))
        {
            foreach (var (id, item) in control.Where(pair => pair.Value.Kind == "hex").ToArray())
            {
                control[id] = item with
                {
                    InMelee = InMelee(presentUnits, item.Hexes[0])
                };
            }
        }

        var sides = card.Sides;
        var standing = sides.Select(side =>
        {
            var (cvp, equipment) = Cvp(card, states.Standing(present), states.Guns(present), side, ended, holders);
            return new VictorySideVerdict(side, cvp, ExitVp(card, states.Exits(present), side, ended, holders), UnbrokenSquads(presentUnits, side, knownTo), equipment);
        }).ToArray();

        VictoryResultVerdict? Decide(bool immediateOnly)
        {
            foreach (var outcome in outcomes.Where(outcome => !immediateOnly || outcome.Immediate))
            {
                foreach (var condition in outcome.Any)
                {
                    if (Holds(card, condition, control, standing, presentUnits) is { } reason)
                    {
                        return new VictoryResultVerdict(outcome.Winner == "draw" ? null : outcome.Winner, reason, Facts(control, standing));
                    }
                }
            }

            return null;
        }

        var atEnd = Decide(false) ?? new VictoryResultVerdict(otherwise == "draw" ? null : otherwise,
            otherwise == "draw" ? "no Victory Condition holds" : "no Victory Condition of the other side holds (A26.3)", Facts(control, standing));
        VictoryConditionVerdict[] lines = [.. outcomes.SelectMany(outcome => outcome.Any.Select(condition =>
        {
            var reason = Holds(card, condition, control, standing, presentUnits);
            return new VictoryConditionVerdict(outcome.Winner, outcome.Immediate, reason is not null, reason ?? NotMet(card, condition, control, standing, presentUnits));
        }))];
        return new VictoryVerdict([.. control.Values], standing, Decide(true), atEnd, lines, otherwise == "draw" ? null : otherwise);
    }

    /// <summary>
    /// The Control of each Location of a building of the card as its states leave it (A26.11, A12.153; ruling R24.1), with no view: what Mopping Up reads
    /// for the ground-level Locations its side does not Control. Each Location is its hex id and level.
    /// </summary>
    public static IReadOnlyDictionary<(string HexId, int Level), string?> LocationControl(VictoryCardFacts card, IVictoryStateReader states, string building)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(states);
        var (control, _) = Control(card, states, null, new VictoryFoldFacts(null, null, 0, 0, null), [building], []);
        return control.Values.Where(item => item.Kind == "location").ToDictionary(item => (item.Hexes[0], item.Level!.Value), item => item.Side);
    }

    /// <summary>
    /// The Control of each named building, of each Location of its hexes, and of each hex of a building a hex count names (rulings R21.1, R24.1): at
    /// scenario start the side whose setup area holds it alone, or the only side setting up on its board (A26.11; ruling R24.4); then, state by state, a
    /// building secured by Mopping Up (A12.153), and the side whose armed Good Order Infantry MMC occupies it with no armed enemy ground unit there (A26.11:
    /// the Location for a Location, ground level for a hex, A26.13, any level for a building, A26.14), not in Bypass; Dummies neither gain nor prevent it
    /// (A26.15). A vehicle's temporary Control is read from the present (A26.12; ruling R24.5). The side last holding each Gun is kept beside it (A26.222).
    /// Ruling R24.6: a fold cached for the same events continues from where it stopped, and is stored where the caller says.
    /// </summary>
    private static (Dictionary<string, ControlVerdict> Control, Dictionary<string, string> GunHolders) Control(VictoryCardFacts card, IVictoryStateReader states,
        string? knownTo, VictoryFoldFacts fold, IReadOnlyList<string> buildings, IReadOnlyList<string> counted)
    {
        IReadOnlyList<int> Levels(string hex) => card.Levels?.Invoke(hex) is { Count: > 0 } levels ? levels : [0];
        var tracked = new Dictionary<string, ControlVerdict>(StringComparer.Ordinal);
        foreach (var id in buildings)
        {
            var hexes = card.BuildingHexes(id)!;
            string[] ids = [.. hexes.Select(hex => hex.Id)];
            tracked[id] = new ControlVerdict(id, "building", ids, StartSide(card, hexes));
            foreach (var hex in hexes)
            {
                foreach (var level in Levels(hex.Id))
                {
                    var location = $"{hex.Board}:{hex.Hex}:{level}";
                    tracked[location] = new ControlVerdict(location, "location", [hex.Id], StartSide(card, [hex])) { Level = level, Building = id };
                }
            }
        }

        foreach (var id in counted)
        {
            foreach (var hex in card.BuildingHexes(id)!)
            {
                tracked[hex.Id] = new ControlVerdict(hex.Id, "hex", [hex.Id], StartSide(card, [hex])) { Building = id };
            }
        }

        var holders = new Dictionary<string, string>(StringComparer.Ordinal);
        var start = 0;
        if (fold.Control is { } cached && fold.GunHolders is { } cachedHolders)
        {
            tracked = new Dictionary<string, ControlVerdict>(cached, StringComparer.Ordinal);
            holders = new Dictionary<string, string>(cachedHolders, StringComparer.Ordinal);
            start = fold.Start;
        }

        for (var index = start; index < states.Count; index++)
        {
            foreach (var (gun, side) in states.GunHolders(index))
            {
                holders[gun] = side;
            }

            if (states.SetupClosed(index))
            {
                // A12.153 (ruling R24.2): a building secured by Mopping Up gives its side the building and every Location the record lists.
                foreach (var secured in states.NewlySecured(index))
                {
                    foreach (var (id, item) in tracked.ToArray())
                    {
                        var held = item.Kind switch
                        {
                            "building" => item.Id == secured.Building,
                            "location" => secured.Locations.Contains($"{item.Hexes[0]}:{item.Level!.Value}", StringComparer.Ordinal),
                            _ => secured.Locations.Contains($"{item.Hexes[0]}:0", StringComparer.Ordinal),
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

                var units = states.Units(index);
                foreach (var (id, item) in tracked.ToArray())
                {
                    if (Gainer(units, item, knownTo) is { } side && side != item.Side)
                    {
                        tracked[id] = item with
                        {
                            Side = side
                        };
                    }
                }
            }

            if (fold.Store is { } store && index + 1 == fold.StoreAt && index + 1 > start)
            {
                store(index + 1, new Dictionary<string, ControlVerdict>(tracked, StringComparer.Ordinal), new Dictionary<string, string>(holders, StringComparer.Ordinal));
            }
        }

        // A26.12 (ruling R24.5): an armed vehicle not in Bypass holds its Location while no armed enemy unit is there, and the hex when that is its only Location.
        var present = states.Units(states.Count - 1);
        foreach (var (id, item) in tracked.Where(pair => pair.Value.Kind != "building").ToArray())
        {
            var level = item.Level ?? 0;
            if (item.Kind == "hex" && Levels(item.Hexes[0]) is not [0])
            {
                continue;
            }

            var there = Inside(present, item.Hexes, knownTo).Where(unit => unit.Level == level).ToArray();
            string[] holding = [.. there.Where(unit => unit.Vehicle && Armed(unit) && !unit.Straddling).Select(unit => unit.Side).Distinct(StringComparer.Ordinal)];
            if (holding is [var vehicleSide] && !there.Any(unit => unit.Side != vehicleSide && Armed(unit)) && vehicleSide != item.Side)
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

    /// <summary>
    /// A26.11 (rulings R21.1, R24.4): each hex's side is the side whose setup areas alone hold it, or, when no area holds it, the only side setting up on
    /// its board; the hexes have that side when every hex has the same one.
    /// </summary>
    public static string? StartSide(VictoryCardFacts card, IReadOnlyList<VictoryHexFacts> hexes)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(hexes);
        string? Of(VictoryHexFacts hex)
        {
            string[] holding = [.. card.SetupAreas.Where(side => side.Areas.Any(area => area.Kind != "entry"
                && ScenarioA1SetupCalculator.Within(area.Kind, area.Hexes, area.Board, area.From, area.To, card.BoardCount, card.FirstBoard, hex.Board, hex.Hex, hex.RowNumber))).Select(side => side.Side)];
            if (holding.Length > 0)
            {
                return holding is [var alone] ? alone : null;
            }

            string[] setting = [.. card.SetupAreas.Where(side => side.Areas.Any(area => area.Kind != "entry"
                && (area.Board ?? (card.BoardCount == 1 ? card.FirstBoard : null)) == hex.Board)).Select(side => side.Side)];
            return setting is [var only] ? only : null;
        }

        var sides = hexes.Select(Of).Distinct(StringComparer.Ordinal).ToArray();
        return sides is [{ } side] ? side : null;
    }

    /// <summary>
    /// A.7, A26.11 (ruling R24.5): an armed ground unit, which prevents the enemy from gaining Control: any unit but a Dummy, a prisoner, or an Unarmed
    /// unit; a vehicle only with an inherent crew and not Abandoned (the Index's Armed), and never a captured one (a deviation: captured vehicles are not
    /// used in play yet).
    /// </summary>
    public static bool Armed(VictoryUnitFacts unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return !unit.Dummy && !unit.Captured && !unit.Unarmed && !(unit.Vehicle && (unit.Abandoned || unit.InherentCrew == false));
    }

    /// <summary>The active units in the hexes, but those a side does not know (ruling R23.4) and Passengers (ruling R26.2).</summary>
    private static VictoryUnitFacts[] Inside(IReadOnlyList<VictoryUnitFacts> units, IReadOnlyList<string> hexes, string? knownTo) =>
        [.. units.Where(unit => !unit.Aboard && hexes.Contains(unit.HexId, StringComparer.Ordinal) && !Undeclared(unit, knownTo))];

    /// <summary>
    /// A.7, A26.11, A26.13, A26.14 (rulings R21.1, R24.1, R24.5): an armed Good Order Infantry MMC, not in Bypass, gains Control (in the Location for a
    /// Location, at ground level for a hex, at any level for a building); any armed enemy ground unit in the same Location, hex, or building prevents it,
    /// but a vehicle in Bypass does not prevent a building's.
    /// </summary>
    private static string? Gainer(IReadOnlyList<VictoryUnitFacts> units, ControlVerdict item, string? knownTo)
    {
        var inside = Inside(units, item.Hexes, knownTo);
        if (item.Kind == "location")
        {
            inside = [.. inside.Where(unit => unit.Level == item.Level)];
        }

        var gainers = inside.Where(unit => ScenarioA1Definitions.MmcKinds.Contains(unit.Kind) && !unit.Unarmed && !unit.Broken && !unit.Berserk
            && !unit.Captured && !unit.Melee
            && (item.Kind != "hex" || unit.Level == 0)
            && !unit.InBypass).Select(unit => unit.Side).Distinct(StringComparer.Ordinal).ToArray();
        return gainers is [var side] && !inside.Any(unit => unit.Side != side && Armed(unit) && !(item.Kind == "building" && unit.Vehicle && unit.Straddling))
            ? side : null;
    }

    /// <summary>An enemy unit a side does not know as such (A26.15; ruling R23.4): concealed or hidden, when the Victory Conditions are read for that side.</summary>
    private static bool Undeclared(VictoryUnitFacts unit, string? knownTo) =>
        knownTo is not null && unit.Side != knownTo && (unit.Concealed || unit.Hidden);

    /// <summary>
    /// A side's unbroken squad-equivalents on the map (A16): a squad one, a HS or crew half; not broken, not a prisoner. Read for <paramref name="knownTo"/>,
    /// another side's concealed and hidden units are not counted (ruling R23.4). <paramref name="units"/> are the active units with a Location.
    /// </summary>
    public static double UnbrokenSquads(IReadOnlyList<VictoryUnitFacts> units, string side, string? knownTo)
    {
        ArgumentNullException.ThrowIfNull(units);
        return units.Where(unit => unit.Side == side && !unit.Broken && !unit.Captured && !Undeclared(unit, knownTo)).Sum(unit => unit.Kind switch
        {
            "asl:squad" => 1.0,
            "asl:half-squad" or "asl:crew" => 0.5,
            _ => 0.0,
        });
    }

    /// <summary>Whether an exit counts for one of its side's Exit VP conditions (A26.23): off the condition's edge, from a hex on or adjacent to one it names.</summary>
    private static bool Qualifies(VictoryCardFacts card, VictoryExitFacts exit, string side) =>
        ScenarioA1EntryCalculator.ExitMeetsCondition(card.Outcomes!.SelectMany(outcome => outcome.Any), side, exit.Edge, exit.FromOnOrAdjacent);

    /// <summary>
    /// Whether a Guard escorting prisoners off an edge is not eliminated for CVP (A20.53, A26.221; referee, pass 25): the side's Friendly Board Edge as the card
    /// names it, or the edge of one of its exit conditions.
    /// </summary>
    public static bool EscortEdge(string? friendlyEdge, IEnumerable<CardConditionFacts>? conditions, string side, string edge) =>
        friendlyEdge == edge || conditions?.Any(condition => condition.Type == "exit-vp" && condition.Side == side && condition.Edge == edge) == true;

    /// <summary>
    /// A26.23: the VP a side has exited through its exit conditions' areas, none for broken Personnel; the enemy units its Guards took off with them count
    /// their normal VP during play and double once the game has ended (A26.222; ruling R25.5). A vehicle counts its VP with its Passengers', and a Gun
    /// towed or pushed off the map its two (A26.212; ruling R26.4).
    /// </summary>
    private static int ExitVp(VictoryCardFacts card, IReadOnlyList<VictoryExitFacts> exits, string side, bool ended, Dictionary<string, string> holders) =>
        exits.Where(exit => !exit.Broken && !exit.Recalled && exit.ExitedUnit is { } unit && (exit.CapturedBy ?? unit.Side) == side && Qualifies(card, exit, side))
            .Sum(exit => exit.ExitedUnit!.Value.Vp() * (exit.CapturedBy is not null && ended ? 2 : 1))
        + (exits.Count(exit => exit.ExitedGun is { } gun && ExitSide(gun.Id, gun.Side, holders) == side && Qualifies(card, exit, side)) * ScenarioA1Definitions.GunVp);

    /// <summary>The side a Gun belongs to: its own, else the side last holding it (A26.222).</summary>
    private static string? GunSide(string gun, string? side, Dictionary<string, string> holders) => side ?? holders.GetValueOrDefault(gun);

    /// <summary>The side that took a Gun off the map: the side last holding it (referee, pass 26: a captor's exit is the captor's).</summary>
    private static string? ExitSide(string gun, string? side, Dictionary<string, string> holders) => holders.GetValueOrDefault(gun) ?? side;

    /// <summary>
    /// A26.22, A26.221, A26.222 (rulings R21.2, R24.3): a side's CVP: the VP of the enemy units eliminated or wrecked, or that left the map other than by
    /// their own exit conditions, of the enemy units and vehicles it holds captured, and of the enemy Guns eliminated or last held by it; captured ones
    /// double once the game has ended. The second value is the part from Guns and vehicles.
    /// </summary>
    private static (int Cvp, int GunsAndVehicles) Cvp(VictoryCardFacts card, IReadOnlyList<VictoryStandingUnitFacts> units, IReadOnlyList<VictoryGunFacts> guns, string side, bool ended,
        Dictionary<string, string> holders)
    {
        var total = 0;
        var equipment = 0;
        foreach (var unit in units.Where(unit => unit.Side != side && !unit.Dummy))
        {
            var value = unit.Status switch
            {
                VictoryStatus.Eliminated or VictoryStatus.Wrecked => unit.Vp(),
                // A20.53, A26.222 (ruling R25.5): a prisoner its Guard took off the map is still held; A26.221: the escorting Guard is not eliminated.
                VictoryStatus.Exited when unit.LastExit is { CapturedBy: not null } => unit.Vp() * (ended ? 2 : 1),
                // A26.23, A26.221 (ruling R26.4): a vehicle leaving under Recall counts no CVP.
                VictoryStatus.Exited when unit.LastExit is { Recalled: true } => 0,
                VictoryStatus.Exited when unit.LastExit is not { } exit
                    || (!(exit.Escort && EscortEdge(card.FriendlyEdge(unit.Side), card.Outcomes?.SelectMany(outcome => outcome.Any), unit.Side, exit.Edge)) && !Qualifies(card, exit, unit.Side)) => unit.Vp(),
                VictoryStatus.Active when unit.Captured => unit.Vp() * (ended ? 2 : 1),
                _ => 0,
            };
            total += value;
            equipment += unit.Vehicle ? value : 0;
        }

        // A26.212, A26.222 (ruling R24.3): a Gun is captured while the side last to hold it is the enemy's; A26.221 (ruling R26.4): one that left the map other
        // than by its side's exit conditions counts as eliminated.
        foreach (var gun in guns.Where(item => GunSide(item.Id, item.Side, holders) is { } owner && owner != side))
        {
            var value = gun.Status switch
            {
                VictoryStatus.Eliminated => ScenarioA1Definitions.GunVp,
                VictoryStatus.Active when holders.TryGetValue(gun.Id, out var holder) && holder == side => ScenarioA1Definitions.GunVp * (ended ? 2 : 1),
                // A26.222: a Gun its captor took off the map is still the captor's.
                VictoryStatus.Exited when ExitSide(gun.Id, gun.Side, holders) == side => ScenarioA1Definitions.GunVp * (ended ? 2 : 1),
                VictoryStatus.Exited when gun.LastExit is not { } gunExit || !Qualifies(card, gunExit, GunSide(gun.Id, gun.Side, holders)!) => ScenarioA1Definitions.GunVp,
                _ => 0,
            };
            total += value;
            equipment += value;
        }

        return (total, equipment);
    }

    /// <summary>Why a condition holds (its reason for the result), or null when it does not.</summary>
    private static string? Holds(VictoryCardFacts card, CardConditionFacts condition, Dictionary<string, ControlVerdict> control, IReadOnlyList<VictorySideVerdict> standing,
        IReadOnlyList<VictoryUnitFacts> present)
    {
        var other = card.Sides.First(side => side != condition.Side);
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
                var hexes = card.BuildingHexes(condition.Building!)!;
                var melee = condition.MeleeUncontrolled == true ? hexes.Where(hex => InMelee(present, hex.Id)).Select(hex => hex.Id).ToHashSet(StringComparer.Ordinal) : [];
                var count = hexes.Count(hex => !melee.Contains(hex.Id) && control[hex.Id].Side == condition.Side);
                return count >= condition.AtLeast
                    ? $"{condition.Side} Controls {count} hexes of building {condition.Building}, at least {condition.AtLeast} (A26.13)"
                    : null;
            case "squad-ratio":
                return mine.UnbrokenSquads > 0 && mine.UnbrokenSquads >= condition.Ratio * theirs.UnbrokenSquads
                    ? $"{condition.Side} has {Number(mine.UnbrokenSquads)} unbroken squad-equivalents against {Number(theirs.UnbrokenSquads)}, at least {Number(condition.Ratio!.Value)} times as many"
                    : null;
            case "sole-unbroken":
                var building = card.BuildingHexes(condition.Building!)!;
                string[] there = [.. present.Where(unit => !unit.Dummy && !unit.Broken && !unit.Captured && building.Any(hex => hex.Id == unit.HexId))
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
    private static string NotMet(VictoryCardFacts card, CardConditionFacts condition, Dictionary<string, ControlVerdict> control, IReadOnlyList<VictorySideVerdict> standing,
        IReadOnlyList<VictoryUnitFacts> present)
    {
        var other = card.Sides.First(side => side != condition.Side);
        var mine = standing.Single(item => item.Side == condition.Side);
        var theirs = standing.Single(item => item.Side == other);
        switch (condition.Type)
        {
            case "control-margin":
                var held = condition.Buildings!.Count(id => control[id].Side == condition.Side);
                var lost = condition.Versus!.Count(id => control[id].Side == other);
                return $"{condition.Side} Controls {held} of {string.Join(", ", condition.Buildings!)}, and {other} Controls {lost} of {string.Join(", ", condition.Versus!)}: a margin of {held - lost}, where {condition.Margin} is needed (A26.14)";
            case "control-count":
                var hexes = card.BuildingHexes(condition.Building!)!;
                var melee = condition.MeleeUncontrolled == true ? hexes.Where(hex => InMelee(present, hex.Id)).Select(hex => hex.Id).ToHashSet(StringComparer.Ordinal) : [];
                var count = hexes.Count(hex => !melee.Contains(hex.Id) && control[hex.Id].Side == condition.Side);
                return $"{condition.Side} Controls {count} hexes of building {condition.Building}, where {condition.AtLeast} are needed (A26.13)";
            case "squad-ratio":
                return $"{condition.Side} has {Number(mine.UnbrokenSquads)} unbroken squad-equivalents against {Number(theirs.UnbrokenSquads)}, where {Number(condition.Ratio!.Value)} times as many are needed";
            case "sole-unbroken":
                return $"{condition.Side} is not the only side with an unbroken unit in building {condition.Building}";
            case "exit-vp":
                return $"{condition.Side} has exited {mine.ExitVp} Exit VP off the {condition.Edge} edge near {string.Join(", ", condition.Near!)}, where {condition.AtLeast} are needed (A26.23)";
            case "cvp":
                return $"{condition.Side} has {mine.Cvp} CVP, where {condition.AtLeast} are needed (A26.22)";
            default:
                return $"a condition of a kind the game does not read ({condition.Type})";
        }
    }

    /// <summary>Whether units of both sides are in Melee in a hex (A11.15).</summary>
    private static bool InMelee(IReadOnlyList<VictoryUnitFacts> units, string hexId) =>
        units.Where(unit => unit.Melee && unit.HexId == hexId).Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count() > 1;

    private static string Number(double value) => value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// The Locations whose Control differs from their building's (ruling R24.7): the only Location rows the standing and the facts show, since the rest
    /// follow their building.
    /// </summary>
    public static IEnumerable<ControlVerdict> DifferingLocations(IReadOnlyList<ControlVerdict> control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return control.Where(item => item.Kind == "location"
            && control.FirstOrDefault(building => building.Kind == "building" && building.Id == item.Building) is { } building && building.Side != item.Side);
    }

    /// <summary>A Location as the standing names it, such as "bd01:M9 level 1" (ruling R24.7).</summary>
    public static string LocationName(string hexId, int? level) => $"{hexId} {LevelName(level)}";

    private static string LevelName(int? level) => level switch
    {
        -1 => "cellar",
        0 => "ground level",
        var other => $"level {other}",
    };

    private static List<string> Facts(Dictionary<string, ControlVerdict> control, IReadOnlyList<VictorySideVerdict> standing) =>
    [
        .. control.Values.Where(item => item.Kind == "building").Select(item => $"building {item.Id}: {item.Side ?? "not Controlled"}"),
        .. control.Values.Where(item => item.Kind == "hex").GroupBy(item => item.InMelee ? "in Melee, neither's for the card" : item.Side ?? "not Controlled")
            .Select(group => $"hexes {string.Join(", ", group.Select(item => item.Id))}: {group.Key}"),
        .. DifferingLocations([.. control.Values]).Select(item => $"Location {LocationName(item.Hexes[0], item.Level)} of building {item.Building}: {item.Side ?? "not Controlled"}{(item.ByVehicle ? " (a vehicle, for now)" : string.Empty)}"),
        .. standing.Select(side => $"{side.Side}: {side.Cvp} CVP, {side.ExitVp} Exit VP, {Number(side.UnbrokenSquads)} unbroken squad-equivalents"),
    ];
}

/// <summary>The status of a unit or a Gun as the Victory Conditions read it.</summary>
public enum VictoryStatus
{
    Active,
    Eliminated,
    Wrecked,
    Exited,
    Other,
}

/// <summary>A hex of a card's building as facts: its id ("board:hex"), its board, its hex name, and the hex's row number.</summary>
public sealed record VictoryHexFacts(string Id, string Board, string Hex, int RowNumber);

/// <summary>
/// A card as the Victory Conditions read it: its sides in order, the structured outcomes and the result when none holds, each side's Friendly Board Edge,
/// each side's setup areas (for Control at scenario start), the boards, a building's hexes by its id (null for none), and the levels of a hex by its id
/// (null for ground level only).
/// </summary>
public sealed record VictoryCardFacts(IReadOnlyList<string> Sides, IReadOnlyList<(string Winner, bool Immediate, IReadOnlyList<CardConditionFacts> Any)>? Outcomes, string? Otherwise,
    Func<string, string?> FriendlyEdge, IReadOnlyList<(string Side, IReadOnlyList<SetupAreaFacts> Areas)> SetupAreas, int BoardCount, string? FirstBoard,
    Func<string, IReadOnlyList<VictoryHexFacts>?> BuildingHexes, Func<string, IReadOnlyList<int>>? Levels);

/// <summary>
/// An active unit with a Location as the Control fold reads it: its id, side, kind, its hex ("board:hex") and level, whether it rides a vehicle, its conditions,
/// whether it is a vehicle and has its inherent crew (null when unread), whether it straddles in Bypass, whether it is among the moving stack's Bypass movers, and
/// whether it is concealed or hidden.
/// </summary>
public sealed record VictoryUnitFacts(string Id, string Side, string Kind, bool Dummy, string HexId, int Level, bool Aboard, bool Unarmed, bool Broken, bool Berserk, bool Captured, bool Melee,
    bool Abandoned, bool Vehicle, bool? InherentCrew, bool Straddling, bool InBypass, bool Concealed, bool Hidden);

/// <summary>An exit record as facts: who left, broken, under Recall, captured by, as an escort, off which edge, and whether a named hex is the exit hex or adjacent to it; the exited unit's side and VP, or the exited Gun's id and side.</summary>
public sealed record VictoryExitFacts(string Unit, bool Broken, bool Recalled, string? CapturedBy, bool Escort, string Edge, Func<string, bool> FromOnOrAdjacent, (string Side, Func<int> Vp)? ExitedUnit,
    (string Id, string? Side)? ExitedGun);

/// <summary>A unit as the standing reads it: its side, whether a Dummy, its status, whether captured, whether a vehicle, its VP (read only when counted), and its last exit record.</summary>
public sealed record VictoryStandingUnitFacts(string Id, string Side, bool Dummy, VictoryStatus Status, bool Captured, bool Vehicle, Func<int> Vp, VictoryExitFacts? LastExit);

/// <summary>A Gun as the standing reads it: its id, its own side, its status, and its last exit record.</summary>
public sealed record VictoryGunFacts(string Id, string? Side, VictoryStatus Status, VictoryExitFacts? LastExit);

/// <summary>
/// The states of a game as the Control fold reads them, by index (the design's D4: the fold is a search over the states): whether play has started, the Guns'
/// holders, the buildings newly secured, the active units with a Location, and, for the present, the standing's units, Guns, and exits.
/// </summary>
public interface IVictoryStateReader
{
    /// <summary>How many states the history holds.</summary>
    public int Count
    {
        get;
    }

    /// <summary>Whether play has started in a state.</summary>
    public bool SetupClosed(int state);

    /// <summary>Each Gun with a holder in a state, with the holder's side.</summary>
    public IEnumerable<(string Gun, string HolderSide)> GunHolders(int state);

    /// <summary>The buildings a state secures beyond the state before it, each with its side and its Locations as "board:hex:level".</summary>
    public IEnumerable<(string Building, string Side, IReadOnlyList<string> Locations)> NewlySecured(int state);

    /// <summary>The active units with a Location in a state.</summary>
    public IReadOnlyList<VictoryUnitFacts> Units(int state);

    /// <summary>Every unit of a state as the standing reads it.</summary>
    public IReadOnlyList<VictoryStandingUnitFacts> Standing(int state);

    /// <summary>Every Gun of a state as the standing reads it.</summary>
    public IReadOnlyList<VictoryGunFacts> Guns(int state);

    /// <summary>The exit records of a state.</summary>
    public IReadOnlyList<VictoryExitFacts> Exits(int state);
}

/// <summary>
/// What a side Controls that the Victory Conditions name (rulings R21.1, R24.1): a building of the card, a hex of one ("board:hex"), or a Location of one
/// ("board:hex:level"); the hexes by id; null when neither side does.
/// </summary>
public sealed record ControlVerdict(string Id, string Kind, IReadOnlyList<string> Hexes, string? Side)
{
    /// <summary>Whether a hex count of the card treats this hex, in Melee now, as Controlled by neither (ruling R21.3).</summary>
    public bool InMelee
    {
        get; init;
    }

    /// <summary>A Location's level; null for a building or a hex (ruling R24.1).</summary>
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

/// <summary>The cached Control fold a reading continues from (ruling R24.6), and where the fold is stored again: after the state whose index plus one is <paramref name="StoreAt"/>.</summary>
public sealed record VictoryFoldFacts(IReadOnlyDictionary<string, ControlVerdict>? Control, IReadOnlyDictionary<string, string>? GunHolders, int Start, int StoreAt,
    Action<int, Dictionary<string, ControlVerdict>, Dictionary<string, string>>? Store);

/// <summary>A side's standing (rulings R21.2, R21.3, R24.3): its CVP, Exit VP, unbroken squad-equivalents, and the CVP from Guns and vehicles.</summary>
public sealed record VictorySideVerdict(string Side, int Cvp, int ExitVp, double UnbrokenSquads, int GunAndVehicleCvp);

/// <summary>A result of the Victory Conditions: the winner (null for a draw), the reason, and the facts.</summary>
public sealed record VictoryResultVerdict(string? Winner, string Reason, IReadOnlyList<string> Facts);

/// <summary>One Victory condition read against a game: the side it wins for ("draw" for a draw), whether it ends the game at once, whether it holds, and why.</summary>
public sealed record VictoryConditionVerdict(string Winner, bool Immediate, bool Holds, string Text);

/// <summary>The Victory Conditions read against a game: the Control, each side's standing, the result now, the result at the end, every condition, and the winner when none holds.</summary>
public sealed record VictoryVerdict(IReadOnlyList<ControlVerdict> Control, IReadOnlyList<VictorySideVerdict> Sides, VictoryResultVerdict? Immediate, VictoryResultVerdict AtEnd,
    IReadOnlyList<VictoryConditionVerdict> Conditions, string? Otherwise);
