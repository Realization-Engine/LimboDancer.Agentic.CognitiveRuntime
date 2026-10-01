using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Mopping Up (A12.153, A12.154; pass 24 of the Card Play and Map Studio Redesign Plan, ruling R24.2): Infantry securing a building of the card in its PFPh,
/// which gives its side the Control of every Location there (A26.11).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// A12.153 (ruling R24.2): in its side's PFPh, armed unpinned Good Order Infantry MMC in a multi-hex or multi-level building of the card, with no
    /// unconcealed unbroken enemy unit in it, each within two hexes of a ground-level Location its side does not Control (between them, of every one),
    /// become TI to Mop it Up, once per building per Player Turn and never after their side has employed No Quarter or Massacre. Hidden enemy units there
    /// are placed beneath "?" and enemy Dummies removed; with no concealed enemy unit left the building is secured: its side Controls every Location but
    /// a ground level holding an armed enemy vehicle in Bypass, and the broken enemy units there surrender to the unit chosen (the first declaring one when
    /// none is). Otherwise, with an armed Good Order concealed enemy unit there, the DEFENDER's Casualty dr (A12.154) is rolled at once, as it costs him
    /// nothing: a Final dr of 1 or less Reduces a Mopping-Up unit, chosen by Random Selection when there are several.
    /// </summary>
    private GamePlan PlanMopUp(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        var history = Replay(existing);
        if (history.Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game");
        }

        if (CardOf(state) is not { } card)
        {
            return Refused(scope, label, expected, "play.mop-up-card: Mopping Up secures a building of the game's card; this game has none (A12.153; ruling R24.2)");
        }

        if (!Text(arguments, "building", out var building) || ScenarioCards.BuildingHexes(card, building) is not { Count: > 0 } hexes)
        {
            return Refused(scope, label, expected, $"play.mop-up-building: '{building}' is not a building of the card (A12.153; ruling R24.2)");
        }

        if (state.Phase != "pfph")
        {
            return Refused(scope, label, expected, "play.mop-up-phase: Mopping Up is declared in its side's PFPh (A12.153)");
        }

        var side = state.PhasingSide;
        bool InBuilding(BoardLocation at) => at.Level >= 0 && hexes.Any(hex => hex.Board == at.Board && hex.Hex == at.Hex);
        if (hexes.Count == 1 && !HexLevels(state, hexes[0]).Any(level => level > 0))
        {
            return Refused(scope, label, expected, $"play.mop-up-building: building {building} has one hex and one level; Mopping Up is of a multi-hex or multi-level building (A12.153)");
        }

        if (state.MoppedUpThisPlayerTurn.Contains(building, StringComparer.Ordinal))
        {
            return Refused(scope, label, expected, $"play.mop-up-once: building {building} was Mopped Up this Player Turn; it may be once per Player Turn (A12.153)");
        }

        if (card.Sides.Any(item => item.Side != side && state.NoQuarter.Contains(item.Side, StringComparer.Ordinal)))
        {
            return Refused(scope, label, expected, $"play.mop-up-no-quarter: the {side} side has employed No Quarter or Massacre and may no longer Mop Up (A12.153, A20.3, A20.4)");
        }

        var ids = Strings(arguments, "unitIds").Distinct(StringComparer.Ordinal).ToArray();
        var units = ids.Select(state.Unit).ToArray();
        if (ids.Length == 0 || units.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side != side
            || unit.Kind is not ("asl:squad" or "asl:half-squad" or "asl:crew") || state.Location(unit.Id)?.Location is not { } at || !InBuilding(at)))
        {
            return Refused(scope, label, expected, $"play.mop-up-units: each unit Mopping Up is an active Infantry MMC of the {side} side in building {building} (A12.153)");
        }

        // Table player, pass 24: the refusal says why.
        foreach (var unit in units)
        {
            if ((Is(unit!, Conditions.Broken) ? "is broken" : Is(unit!, Conditions.Berserk) ? "is berserk" : Is(unit!, Conditions.Pinned) ? "is pinned"
                : Is(unit!, Conditions.Captured) || Is(unit!, Conditions.Unarmed) ? "is not armed" : Is(unit!, Conditions.Melee) ? "is in Melee"
                : Is(unit!, "asl:ti") ? "is already TI" : Is(unit!, Conditions.PrepFire) ? "fired in this PFPh" : null) is { } why)
            {
                return Refused(scope, label, expected, $"play.mop-up-unit: {unit!.Id} {why}; Mopping Up takes an armed, unpinned, Good Order MMC free to become TI (A12.153)");
            }
        }

        // Referee and table player, pass 24: an enemy vehicle in Bypass is not inside the building (A26.11); it keeps its ground level out of the secured set.
        var enemies = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && state.Location(unit.Id)?.Location is { } at && InBuilding(at)
            && !(LiveFire.IsVehicle(unit) && unit.Straddling is not null)).OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        if (enemies.FirstOrDefault(unit => unit.Kind != UnitKinds.Dummy && !Is(unit, Conditions.Concealed) && !Is(unit, Conditions.Hidden) && !Is(unit, Conditions.Broken)
            && !Is(unit, Conditions.Captured)) is not null)
        {
            return Refused(scope, label, expected, $"play.mop-up-enemy: building {building} holds an unconcealed unbroken enemy unit (A12.153)");
        }

        // A12.153: within two hexes of every ground-level Location the side does not Control (A26.11; ruling R24.1).
        var control = ScenarioVictory.LocationControl(card, history.States, building, hex => HexLevels(state, hex));
        foreach (var hex in hexes.Where(hex => control.GetValueOrDefault(hex with { Level = 0 }) != side))
        {
            var near = units.Select(unit => HexDistance(state, state.Location(unit!.Id)!.Location, hex)).ToArray();
            if (near.Any(distance => distance is null))
            {
                return Refused(scope, label, expected, $"play.mop-up-range: the distance to {hex.Hex} is not decided on this map (A12.153)");
            }

            if (!near.Any(distance => distance <= 2))
            {
                return Refused(scope, label, expected, $"play.mop-up-range: no unit Mopping Up is within two hexes of {hex.Hex}, a ground-level Location the {side} side does not Control (A12.153, A26.11)");
            }
        }

        // The guard the broken enemy surrender to, of the side's choice inside the building (A12.153): any armed Personnel unit, even a broken SMC, never a
        // berserk one (A20.5, A20.4; referee, pass 24). One guard takes them all (ruling R24.2).
        var guardId = Text(arguments, "guard", out var named) ? named : ids[0];
        if (state.Unit(guardId) is not { Status: InstanceStatus.Active } guard || guard.Side != side || guard.Kind == UnitKinds.Dummy || LiveFire.IsVehicle(guard)
            || (Is(guard, Conditions.Broken) && guard.Kind is not ("asl:leader" or "asl:hero")) || Is(guard, Conditions.Berserk) || Is(guard, Conditions.Unarmed)
            || Is(guard, Conditions.Captured) || state.Location(guard.Id)?.Location is not { } guardAt || !InBuilding(guardAt))
        {
            return Refused(scope, label, expected, $"play.mop-up-guard: {guardId} is not an armed Personnel unit of the {side} side inside building {building} that may guard prisoners (A12.153, A20.5)");
        }

        var events = new List<GameEvent>();
        void Add(string type, EventPayload payload, IReadOnlyList<string>? visibility = null) =>
            events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, null, visibility));

        foreach (var hidden in enemies.Where(unit => Is(unit, Conditions.Hidden)))
        {
            Add("hidden-placed", new ConditionsChanged(hidden.Id, new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.False, [Conditions.Concealed] = ConditionState.True }),
                [hidden.Side]);
        }

        foreach (var dummy in enemies.Where(unit => unit.Kind == UnitKinds.Dummy))
        {
            Add("instance-eliminated", new InstanceEliminated(dummy.Id));
        }

        var concealed = enemies.Where(unit => unit.Kind != UnitKinds.Dummy && (Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden))).ToArray();
        var reasons = new List<string>();
        var found = enemies.Count(unit => Is(unit, Conditions.Hidden));
        var dummies = enemies.Count(unit => unit.Kind == UnitKinds.Dummy);

        // Table player, pass 24: what the Mopping Up found is said.
        var phrase = (ids.Length == 1
            ? $"{ids[0]} Mops Up building {building} and becomes TI (A12.153; ruling R24.2)"
            : $"{string.Join(", ", ids)} Mop Up building {building} and become TI (A12.153; ruling R24.2)")
            + (found > 0 ? $"; {found} hidden enemy unit{(found == 1 ? " is" : "s are")} placed beneath \"?\"" : string.Empty)
            + (dummies > 0 ? $"; {dummies} enemy Dumm{(dummies == 1 ? "y is" : "ies are")} removed" : string.Empty);
        if (concealed.Length == 0)
        {
            // A26.11: every Location of the building, but a ground level holding an armed enemy vehicle in Bypass.
            var bypassed = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && LiveFire.IsVehicle(unit) && unit.Straddling is not null
                && !Is(unit, Conditions.Abandoned) && !Is(unit, Conditions.Captured)).Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>()
                .Select(at => at with { Level = 0, Side = null }).ToHashSet();
            BoardLocation[] secured = [.. hexes.SelectMany(hex => HexLevels(state, hex).Select(level => hex with { Level = level })).Where(location => !bypassed.Contains(location))];
            Add("building-mopped-up", new BuildingMoppedUp(building, side, ids, secured));
            reasons.Add($"play.mop-up: {phrase}; no concealed enemy unit is left, so the {side} side Controls the building and its Locations (A26.11)");
            foreach (var broken in enemies.Where(unit => unit.Kind != UnitKinds.Dummy && Is(unit, Conditions.Broken) && !Is(unit, Conditions.Captured)))
            {
                var at = state.Location(broken.Id)!.Location;
                foreach (var weapon in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == broken.Id).OrderBy(item => item.Id, StringComparer.Ordinal))
                {
                    Add("equipment-transferred", new EquipmentTransferred(weapon.Id, null, new MapPosition(at)));
                }

                if (at != guardAt)
                {
                    Add("instance-moved", new InstanceMoved(broken.Id, new MapPosition(guardAt)));
                }

                Add("conditions-changed", new ConditionsChanged(broken.Id, new Dictionary<string, ConditionState>
                {
                    [Conditions.Broken] = ConditionState.False,
                    [Conditions.Disrupted] = ConditionState.False,
                    [Conditions.Pinned] = ConditionState.False,
                    [Conditions.DesperationMorale] = ConditionState.False,
                }));
                Add("instance-captured", new InstanceCaptured(broken.Id, guard.Id));
                reasons.Add($"play.mop-up-surrender: {broken.Id} surrenders to {guard.Id} (A12.153, A20.5)");
            }

            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, reasons);
        }

        Add("building-mopped-up", new BuildingMoppedUp(building, side, ids, null));
        var defenders = concealed.Where(unit => !Is(unit, Conditions.Broken) && !Is(unit, Conditions.Unarmed) && !Is(unit, Conditions.Captured)).ToArray();
        var summary = $"play.mop-up: {phrase}; concealed enemy units remain, so the building is not secured";
        if (defenders.Length == 0)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [summary]);
        }

        // A12.154: -1 per Stealthy unit, -1 per HS-equivalent beyond one, the best leader's leadership unless alone, +1 per Lax unit.
        var drm = 0;
        var notes = new List<string>();
        var stealthy = defenders.Count(unit => unit.Kind == "asl:hero" || (unit.Kind == "asl:leader" && Is(unit, Conditions.Heroic)));
        if (stealthy > 0)
        {
            drm -= stealthy;
            notes.Add($"-{stealthy} Stealthy");
        }

        var halfSquads = defenders.Sum(unit => unit.Kind switch { "asl:squad" => 2, "asl:half-squad" or "asl:crew" => 1, _ => 0 });
        if (halfSquads > 1)
        {
            drm -= halfSquads - 1;
            notes.Add($"-{halfSquads - 1} for {halfSquads} HS-equivalents");
        }

        if (defenders.Length > 1 && defenders.Where(unit => unit.Kind == "asl:leader" && DefinitionOf(unit)?.Leadership is not null)
            .Select(unit => DefinitionOf(unit)!.Leadership!.Value + (Is(unit, Conditions.Wounded) ? 1 : 0)).DefaultIfEmpty(0).Min() is var leadership and not 0)
        {
            drm += leadership;
            notes.Add($"{leadership:+0;-0} leadership");
        }

        var lax = defenders.Count(unit => Experience.Inexperienced(state, unit, catalogs, vocabulary) == ConditionState.True);
        if (lax > 0)
        {
            drm += lax;
            notes.Add($"+{lax} Lax");
        }

        var before = events.ToArray();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var built = new List<GameEvent>(before);
            void Push(string type, EventPayload payload) => built.Add(Event(scope, attemptId, built.Count + 1, expected, type, payload, null, null));
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            Push("dice-rolled", new DiceRolled(rollId, "search-casualty", 1, 6, drawn.Values, DiceRolled.SystemSource, actor));
            if (drawn.Values[0] + drm > 1)
            {
                return built;
            }

            var struck = ids;
            if (ids.Length > 1)
            {
                var selection = draw(new RollRequest(ids.Length, 6));
                Push("dice-rolled", new DiceRolled($"{attemptId}-roll-2", "random-selection", ids.Length, 6, selection.Values, DiceRolled.SystemSource, actor));
                struck = [.. ids.Where((_, index) => selection.Values[index] == selection.Values.Max())];
            }

            foreach (var id in struck)
            {
                var unit = state.Unit(id)!;
                var (type, payload) = CasualtyReduction(unit with
                {
                    Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { ["asl:ti"] = ConditionState.True }
                }, attemptId);
                Push(type, payload);
            }

            return built;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"{summary}; the DEFENDER's Casualty dr ({(notes.Count == 0 ? "no drm" : string.Join(", ", notes))}) Reduces a Mopping-Up unit on a Final dr of 1 or less (A12.154; ruling R24.2)"])
        {
            Roll = new PlannedRoll("search-casualty", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// The buildings of a game's card that may be Mopped Up, by id, with their hexes (A12.153; ruling R24.2): those of more than one hex or level, for the
    /// Play page's Mopping Up.
    /// </summary>
    public IReadOnlyList<(string Id, IReadOnlyList<BoardLocation> Hexes)> MopUpBuildings(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return CardOf(state) is { } card
            ? [.. card.Sides.SelectMany(side => side.Groups).SelectMany(group => group.Areas).Where(area => area.Kind == "building").Select(area => area.Id)
                .Distinct(StringComparer.Ordinal).Select(id => (id, ScenarioCards.BuildingHexes(card, id)!))
                .Where(item => item.Item2.Count > 1 || item.Item2.Any(hex => HexLevels(state, hex).Any(level => level > 0)))]
            : [];
    }
}
