using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
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

        // Pass 32.g: the card, the state, and the map are read here, and Rules decides each bar in its order.
        var card = CardOf(state);
        if (ScenarioA1MoppingUp.CardBar(card is not null) is { } cardBar)
        {
            return Refused(scope, label, expected, cardBar);
        }

        var hexes = Text(arguments, "building", out var building) ? ScenarioCards.BuildingHexes(card!, building) : null;
        if (ScenarioA1MoppingUp.BuildingBar(building, hexes is { Count: > 0 }) is { } buildingBar)
        {
            return Refused(scope, label, expected, buildingBar);
        }

        if (ScenarioA1MoppingUp.PhaseBar(state.Phase) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        var side = state.PhasingSide;
        bool InBuilding(BoardLocation at) => ScenarioA1MoppingUp.InBuilding(at.Level, hexes!.Any(hex => hex.Board == at.Board && hex.Hex == at.Hex));
        if (ScenarioA1MoppingUp.SingleHexBar(hexes!.Count, hexes.Count != 1 || HexLevels(state, hexes[0]).Any(level => level > 0), building!) is { } singleBar)
        {
            return Refused(scope, label, expected, singleBar);
        }

        if (ScenarioA1MoppingUp.OnceBar(state.MoppedUpThisPlayerTurn.Contains(building!, StringComparer.Ordinal), building!) is { } onceBar)
        {
            return Refused(scope, label, expected, onceBar);
        }

        if (ScenarioA1MoppingUp.NoQuarterBar(card!.Sides.Any(item => item.Side != side && state.NoQuarter.Contains(item.Side, StringComparer.Ordinal)), side!) is { } quarterBar)
        {
            return Refused(scope, label, expected, quarterBar);
        }

        var ids = Strings(arguments, "unitIds").Distinct(StringComparer.Ordinal).ToArray();
        var units = ids.Select(state.Unit).ToArray();
        if (ScenarioA1MoppingUp.UnitsBar(ids.Length, units.Any(unit => !ScenarioA1MoppingUp.MopUpUnit(unit is { Status: InstanceStatus.Active }, unit?.Side == side,
            unit?.Kind is "asl:squad" or "asl:half-squad" or "asl:crew", unit is not null && state.Location(unit.Id)?.Location is { } at && InBuilding(at))), side!, building!) is { } unitsBar)
        {
            return Refused(scope, label, expected, unitsBar);
        }

        // Table player, pass 24: the refusal says why.
        foreach (var unit in units)
        {
            if (ScenarioA1MoppingUp.UnitWhy(Is(unit!, Conditions.Broken), Is(unit!, Conditions.Berserk), Is(unit!, Conditions.Pinned), Is(unit!, Conditions.Captured), Is(unit!, Conditions.Unarmed),
                Is(unit!, Conditions.Melee), Is(unit!, "asl:ti"), Is(unit!, Conditions.PrepFire)) is { } why)
            {
                return Refused(scope, label, expected, ScenarioA1MoppingUp.UnitText(unit!.Id, why));
            }
        }

        // Referee and table player, pass 24: an enemy vehicle in Bypass is not inside the building (A26.11); it keeps its ground level out of the secured set.
        var enemies = state.Units.Where(unit => ScenarioA1MoppingUp.EnemyInside(unit.Status == InstanceStatus.Active, unit.Side != side, state.Location(unit.Id)?.Location is { } at && InBuilding(at),
            LiveFire.IsVehicle(unit), unit.Straddling is not null)).OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        if (enemies.Any(unit => ScenarioA1MoppingUp.UnconcealedUnbroken(unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), Is(unit, Conditions.Broken), Is(unit, Conditions.Captured))))
        {
            return Refused(scope, label, expected, ScenarioA1MoppingUp.EnemyText(building!));
        }

        // A12.153: within two hexes of every ground-level Location the side does not Control (A26.11; ruling R24.1).
        var control = ScenarioVictory.LocationControl(card, history.States, building, hex => HexLevels(state, hex));
        foreach (var hex in hexes.Where(hex => control.GetValueOrDefault(hex with { Level = 0 }) != side))
        {
            if (ScenarioA1MoppingUp.RangeBar([.. units.Select(unit => HexDistance(state, state.Location(unit!.Id)!.Location, hex))], hex.Hex.ToString(), side!) is { } rangeBar)
            {
                return Refused(scope, label, expected, rangeBar);
            }
        }

        // The guard the broken enemy surrender to, of the side's choice inside the building (A12.153): any armed Personnel unit, even a broken SMC, never a
        // berserk one (A20.5, A20.4; referee, pass 24). One guard takes them all (ruling R24.2).
        var guardId = Text(arguments, "guard", out var named) ? named : ids[0];
        var guard = state.Unit(guardId);
        if (guard is null || state.Location(guard.Id)?.Location is not { } guardAt || !ScenarioA1MoppingUp.Guard(guard.Status == InstanceStatus.Active, guard.Side == side, guard.Kind == UnitKinds.Dummy,
            LiveFire.IsVehicle(guard), Is(guard, Conditions.Broken), guard.Kind is "asl:leader" or "asl:hero", Is(guard, Conditions.Berserk), Is(guard, Conditions.Unarmed),
            Is(guard, Conditions.Captured), InBuilding(guardAt)))
        {
            return Refused(scope, label, expected, ScenarioA1MoppingUp.GuardText(guardId, side!, building!));
        }

        var events = new List<GameEvent>();
        void Add(string type, EventPayload payload, IReadOnlyList<string>? visibility = null) =>
            events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, null, visibility));

        foreach (var hidden in enemies.Where(unit => Is(unit, Conditions.Hidden)))
        {
            Add("hidden-placed", new ConditionsChanged(hidden.Id, ConditionChanges(ScenarioA1MoppingUp.HiddenPlacedConditions())), [hidden.Side]);
        }

        foreach (var dummy in enemies.Where(unit => unit.Kind == UnitKinds.Dummy))
        {
            Add("instance-eliminated", new InstanceEliminated(dummy.Id));
        }

        var concealed = enemies.Where(unit => ScenarioA1MoppingUp.Concealed(unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden))).ToArray();
        var reasons = new List<string>();
        var found = enemies.Count(unit => Is(unit, Conditions.Hidden));
        var dummies = enemies.Count(unit => unit.Kind == UnitKinds.Dummy);

        // Table player, pass 24: what the Mopping Up found is said.
        var phrase = ScenarioA1MoppingUp.Phrase(ids, building!, found, dummies);
        if (concealed.Length == 0)
        {
            // A26.11: every Location of the building, but a ground level holding an armed enemy vehicle in Bypass.
            var bypassed = state.Units.Where(unit => ScenarioA1MoppingUp.BypassedVehicle(unit.Status == InstanceStatus.Active, unit.Side != side, LiveFire.IsVehicle(unit), unit.Straddling is not null,
                Is(unit, Conditions.Abandoned), Is(unit, Conditions.Captured))).Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>()
                .Select(at => at with { Level = 0, Side = null }).ToHashSet();
            BoardLocation[] secured = [.. hexes.SelectMany(hex => HexLevels(state, hex).Select(level => hex with { Level = level })).Where(location => !bypassed.Contains(location))];
            Add("building-mopped-up", new BuildingMoppedUp(building!, side!, ids, secured));
            reasons.Add(ScenarioA1MoppingUp.SecuredSummary(phrase, side!));
            foreach (var broken in enemies.Where(unit => ScenarioA1MoppingUp.Surrenders(unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Broken), Is(unit, Conditions.Captured))))
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

                Add("conditions-changed", new ConditionsChanged(broken.Id, ConditionChanges(ScenarioA1MoppingUp.SurrenderConditions())));
                Add("instance-captured", new InstanceCaptured(broken.Id, guard.Id));
                reasons.Add(ScenarioA1MoppingUp.SurrenderText(broken.Id, guard.Id));
            }

            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, reasons);
        }

        Add("building-mopped-up", new BuildingMoppedUp(building!, side!, ids, null));
        var defenders = concealed.Where(unit => ScenarioA1MoppingUp.Defender(Is(unit, Conditions.Broken), Is(unit, Conditions.Unarmed), Is(unit, Conditions.Captured))).ToArray();
        var summary = ScenarioA1MoppingUp.NotSecuredSummary(phrase);
        if (defenders.Length == 0)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [summary]);
        }

        // A12.154: -1 per Stealthy unit, -1 per HS-equivalent beyond one, the best leader's leadership unless alone, +1 per Lax unit; the catalog and the
        // Inexperience are read here, and Rules counts (pass 32.g).
        var (drm, notes) = ScenarioA1MoppingUp.CasualtyDrm([.. defenders.Select(unit => new MopUpDefenderFacts(unit.Kind == "asl:hero", unit.Kind == "asl:leader", Is(unit, Conditions.Heroic),
            ScenarioA1MoppingUp.HalfSquads(unit.Kind == "asl:squad", unit.Kind is "asl:half-squad" or "asl:crew"), DefinitionOf(unit)?.Leadership, Is(unit, Conditions.Wounded),
            Experience.Inexperienced(state, unit, catalogs, vocabulary) == ConditionState.True))]);

        var before = events.ToArray();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var built = new List<GameEvent>(before);
            void Push(string type, EventPayload payload) => built.Add(Event(scope, attemptId, built.Count + 1, expected, type, payload, null, null));
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            Push("dice-rolled", new DiceRolled(rollId, "search-casualty", 1, 6, drawn.Values, DiceRolled.SystemSource, actor));
            if (!ScenarioA1MoppingUp.CasualtyReduces(drawn.Values[0], drm))
            {
                return built;
            }

            var struck = ids;
            if (ids.Length > 1)
            {
                var selection = draw(new RollRequest(ids.Length, 6));
                Push("dice-rolled", new DiceRolled($"{attemptId}-roll-2", "random-selection", ids.Length, 6, selection.Values, DiceRolled.SystemSource, actor));
                struck = [.. ScenarioA1MoppingUp.Struck(ids, selection.Values)];
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
            [ScenarioA1MoppingUp.CasualtyText(summary, notes)])
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
                .Where(item => ScenarioA1MoppingUp.MopUpBuilding(item.Item2.Count, item.Item2.Any(hex => HexLevels(state, hex).Any(level => level > 0))))]
            : [];
    }
}
