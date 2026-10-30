using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Searching (A12.152, A12.154, p. 79; pass 70, task 70.9, ruling R70.24): members of the moving stack, as it ends its move, spend one more MF to reveal the
/// concealed enemy units of their Accessible hexes. The planner reads the stack, the map, and the occupied Locations; Rules decides who may Search, the drm,
/// how many hexes are searched, what each unit there becomes, and the DEFENDER's Casualty dr.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// The hexes a stack in a Location may Search besides its own (A12.152; ruling R70.24, until the Index's "Accessible" is read): the ADJACENT hexes it
    /// could enter from the ground level of its hex, enemy units ignored, in the map's order. The page offers them, and the Search checks the hexes named.
    /// </summary>
    public IReadOnlyList<BoardLocation> SearchHexes(GameState state, BoardLocation at)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(at);
        var ground = new BoardLocation(at.Board, at.Hex, 0);
        return [.. Neighbors(state, ground).Where(next => InfantryStep(state, ground, next).Entry is not null)
            .OrderBy(next => next.Board.Value, StringComparer.Ordinal).ThenBy(next => next.Hex.Column).ThenBy(next => next.Hex.RowNumber)];
    }

    /// <summary>
    /// A12.152 (ruling R70.24): the moving stack's members named, as it ends its move, Search: each spends one more MF in its
    /// hex and becomes TI for the rest of the Player Turn; the Search dr leaves its Final dr of the stack's Accessible hexes other than its own unsearched: those
    /// the ATTACKER named are searched first in his order, then the others in map order (ruling R70.42). Every Location above ground of a searched hex shows its
    /// contents: concealed enemy units lose "?", hidden ones are placed beneath "?", and Dummies are removed. A Searched Location with an armed Good Order
    /// DEFENDER unit draws his Casualty dr at once (A12.154; ruling R24.2), which Reduces a searcher, by Random Selection among several, on a Final dr of 1
    /// or less. A leadership modifier applied to the Search costs the searchers' "?" (A12.141; ruling R70.17). The DEFENDER's window then opens on that MF
    /// (A8.1), and the searchers' move ends when it closes.
    /// </summary>
    private GamePlan PlanSearch(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (ScenarioA1Searching.PhaseBar(state.Phase) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        var movement = state.Movement;
        if (ScenarioA1Searching.MoveBar(movement is { Members.Count: > 0 }, movement?.Vehicle == true, movement?.WindowOpen == true, movement?.Assault == true) is { } moveBar)
        {
            return Refused(scope, label, expected, moveBar);
        }

        string[] ids = [.. Strings(arguments, "unitIds")];
        if (ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Search names its units once each");
        }

        // Pass 70 (ruling R70.24): the searchers' facts, read from the state's index; Rules decides each bar.
        var index = MemoOf(state).Objects(state);
        var at = movement!.Location;
        SearcherFacts Facts(string id)
        {
            var unit = index.Unit(id);
            var definition = unit is null ? null : DefinitionOf(unit);
            return new SearcherFacts(id, unit is { Status: InstanceStatus.Active }, unit?.Side == state.PhasingSide,
                movement.Members.Contains(id, StringComparer.Ordinal) && index.Location(id)?.Location == at,
                unit is not null && unit.Kind != UnitKinds.Dummy && vocabulary.IsA(unit.Kind, "asl:personnel") && index.VehicleOf(id) is null,
                unit is not null && vocabulary.IsA(unit.Kind, "asl:mmc"), definition?.IsLeader == true, unit is not null && Is(unit, Conditions.Broken),
                unit is not null && Is(unit, Conditions.Berserk), unit is not null && Is(unit, Conditions.Pinned), unit is not null && Is(unit, Conditions.Captured),
                unit is not null && Is(unit, Conditions.Unarmed), unit?.Kind == "asl:hero", unit is not null && Is(unit, Conditions.Heroic),
                unit is null ? 0 : ScenarioA1MoppingUp.HalfSquads(unit.Kind == "asl:squad", unit.Kind == "asl:half-squad" || ScenarioA1Definitions.IsCrew(unit.Kind)),
                definition?.Leadership, unit is not null && Is(unit, Conditions.Wounded),
                unit is not null && Experience.Inexperienced(state, unit, catalogs, vocabulary) == ConditionState.True, unit is not null && Is(unit, Conditions.Cx))
            {
                Stealth = unit is null ? null : StealthStatusOf(state, unit, StealthPurpose.Search),
            };
        }

        SearcherFacts[] facts = [.. ids.Select(Facts)];
        foreach (var fact in facts)
        {
            if (ScenarioA1Searching.UnitBar(fact) is { } unitBar)
            {
                return Refused(scope, label, expected, unitBar);
            }
        }

        if (ScenarioA1Searching.MmcBar(facts) is { } mmcBar)
        {
            return Refused(scope, label, expected, mmcBar);
        }

        if (ScenarioA1StealthCalculator.FactsRefusal(facts.Select(fact => fact.Stealth)) is { } stealthBar)
        {
            return Refused(scope, label, expected, stealthBar);
        }

        // A12.152: one more MF in its present hex, read as a step of 1 MF is for each searcher (A4.11, A4.42; the leader bonus of the stack it moved with).
        UnitInstance[] searchers = [.. ids.Select(id => index.Unit(id)!)];
        UnitInstance[] stack = [.. movement.Members.Select(index.Unit).OfType<UnitInstance>()];
        var leaderIpcTo = LeaderIpcRecipient(state, searchers, movement.IpcTo);
        foreach (var searcher in searchers)
        {
            MfLeftMoverFacts[] mover = [new(searcher.Id, searcher.MfSpent == 0 && !searcher.HalfMfSpent, searcher.DoubleTimeMf, Is(searcher, Conditions.Cx), searcher.OffRoad,
                LeaderBonus(state, searcher, stack), (searcher.MfSpent * 2) + (searcher.HalfMfSpent ? 1 : 0))];
            if (ScenarioA1MovementCalculator.MfCheck(mover, false, false, false, false, false, state.Nvr, false, 2, leaderIpcTo,
                (_, doubleTimeMf, cx, bonusMf, ipcBonus) => MfAllotment(state, searcher, doubleTimeMf, cx, bonusMf, ipcBonus)).Refusal is not null)
            {
                return Refused(scope, label, expected, ScenarioA1Searching.MfText(searcher.Id));
            }
        }

        // A12.152 (ruling R70.24): the hexes named, in the ATTACKER's order of preference, each an Accessible hex other than the stack's own.
        var own = new BoardLocation(at.Board, at.Hex, 0);
        var accessible = SearchHexes(state, at);
        var named = new List<BoardLocation>();
        foreach (var text in Strings(arguments, "hexes"))
        {
            if (!BoardLocation.TryParse(text, out var hex) && !BoardLocation.TryParse(text + ":0", out hex))
            {
                return Refused(scope, label, expected, "play.invalid-arguments: a Search names its hexes as Locations, such as bd01:E4:0");
            }

            named.Add(new BoardLocation(hex!.Board, hex.Hex, 0));
        }

        if (ScenarioA1Searching.HexesBar([.. named.Select(hex => hex.ToString())], text => accessible.Any(hex => hex.ToString() == text)) is { } hexesBar)
        {
            return Refused(scope, label, expected, hexesBar);
        }

        var (drm, notes) = ScenarioA1Searching.Drm(facts);

        // A12.141 (p. 78; ruling R70.17): a leadership modifier applied to the Search costs the searchers' "?" in the LOS of a Good Order enemy within 16 hexes.
        var prefix = new List<GameEvent>();
        string[] lost = ScenarioA1Searching.LeadershipApplied(facts) ? AddActionConcealmentLoss(scope, attemptId, expected, prefix, state, searchers) : [];
        var side = state.PhasingSide!;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var built = new List<GameEvent>(prefix);
            void Push(string type, EventPayload payload, IReadOnlyList<string>? visibility = null) =>
                built.Add(Event(scope, attemptId, built.Count + 1, expected, type, payload, null, visibility));
            var rolled = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            Push("dice-rolled", new DiceRolled(rollId, ScenarioA1Searching.Purpose, 1, 6, rolled.Values, DiceRolled.SystemSource, actor));
            // Ruling R70.42: the hexes named in the ATTACKER's order, then the other Accessible hexes in map order, as many as the Final dr leaves.
            BoardLocation[] searched = [own, .. ScenarioA1Searching.Searched(named, accessible, rolled.Values[0] + drm)];

            // Ruling R70.25: the enemy units of the Searched Locations, read in one pass over the occupied stacks of their hexes.
            (UnitInstance Unit, BoardLocation At)[] enemies = [.. UnitsInHexes(index, searched).Where(item => item.Unit.Side != side).OrderBy(item => item.Unit.Id, StringComparer.Ordinal)];

            // A12.154: an armed Good Order DEFENDER unit, or Residual FP, in a Searched Location draws one Casualty dr, with the drm of the concealed units as
            // the Search found them. Pass 70's fixes: the Search's record keeps that drm for the lines of the views that may know it.
            UnitInstance[] defenders = [.. enemies.Select(item => item.Unit).Where(unit => ScenarioA1Searching.CasualtyTrigger(unit.Kind == UnitKinds.Dummy, LiveFire.IsVehicle(unit),
                Is(unit, Conditions.Broken), Is(unit, Conditions.Unarmed), Is(unit, Conditions.Captured)))];
            int? casualtyDrm = null;
            if (ScenarioA1Searching.CasualtyDue(defenders.Length > 0,
                state.ResidualFire.Any(residual => residual.Location.Level >= 0 && searched.Any(hex => hex.Board == residual.Location.Board && hex.Hex == residual.Location.Hex))))
            {
                MopUpDefenderFacts[] casualtyFacts = [.. defenders.Where(unit => Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden))
                    .Select(unit => new MopUpDefenderFacts(unit.Kind == "asl:hero", unit.Kind == "asl:leader", Is(unit, Conditions.Heroic),
                        ScenarioA1MoppingUp.HalfSquads(unit.Kind == "asl:squad", unit.Kind == "asl:half-squad" || ScenarioA1Definitions.IsCrew(unit.Kind)), DefinitionOf(unit)?.Leadership,
                        Is(unit, Conditions.Wounded), Experience.Inexperienced(state, unit, catalogs, vocabulary) == ConditionState.True)
                    {
                        Stealth = StealthStatusOf(state, unit, StealthPurpose.SearchCasualty),
                    })];
                if (ScenarioA1StealthCalculator.FactsRefusal(casualtyFacts.Select(fact => fact.Stealth)) is { } casualtyBar)
                {
                    throw new GameBuildRefusedException(casualtyBar);
                }

                casualtyDrm = ScenarioA1MoppingUp.CasualtyDrm(casualtyFacts).Drm;
            }
            Push("units-searched", new UnitsSearched(ids, at, rollId, drm, accessible.Count, searched) { CasualtyDrm = casualtyDrm });
            foreach (var (enemy, _) in enemies)
            {
                switch (ScenarioA1Searching.Reveal(enemy.Kind == UnitKinds.Dummy, Is(enemy, Conditions.Concealed), Is(enemy, Conditions.Hidden)))
                {
                    case "removed":
                        Push("instance-eliminated", new InstanceEliminated(enemy.Id));
                        break;
                    case "placed":
                        Push("hidden-placed", new ConditionsChanged(enemy.Id, ConditionChanges(ScenarioA1MoppingUp.HiddenPlacedConditions())), [enemy.Side]);
                        break;
                    case "revealed":
                        built.AddRange(RevealEvents(scope, attemptId, expected, built.Count + 1, [enemy.Id]));
                        break;
                }
            }

            if (casualtyDrm is not { } casualtyModifier)
            {
                return built;
            }

            var casualty = draw(new RollRequest(1, 6));
            Push("dice-rolled", new DiceRolled($"{attemptId}-roll-2", "search-casualty", 1, 6, casualty.Values, DiceRolled.SystemSource, actor));
            if (!ScenarioA1MoppingUp.CasualtyReduces(casualty.Values[0], casualtyModifier))
            {
                return built;
            }

            var struck = ids;
            if (ids.Length > 1)
            {
                var selection = draw(new RollRequest(ids.Length, 6));
                Push("dice-rolled", new DiceRolled($"{attemptId}-roll-3", "random-selection", ids.Length, 6, selection.Values, DiceRolled.SystemSource, actor));
                struck = [.. ScenarioA1MoppingUp.Struck(ids, selection.Values)];
            }

            foreach (var id in struck)
            {
                var unit = index.Unit(id)!;

                // A17.11 (pass 35, task 35.1): a SMC's Casualty Reduction is a wound, and its Wound Severity dr is made at once.
                int? severity = null;
                if (WoundSeverityDue(unit))
                {
                    var wound = draw(new RollRequest(1, 6));
                    severity = wound.Values[0];
                    Push("dice-rolled", new DiceRolled($"{attemptId}-wound-{id}", ScenarioA1Wounds.SeverityPurpose, 1, 6, wound.Values, DiceRolled.SystemSource, actor));
                }

                // A17.2 (pass 71, ruling R71.14): a SMC wounded after spending more than 3 MF, the Search's own MF among them, is pinned (Rules adds them up).
                var (type, payload) = CasualtyReduction(unit with
                {
                    Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { ["asl:ti"] = ConditionState.True }
                }, attemptId, severity, ScenarioA1Wounds.PinnedByWound(vocabulary.IsA(unit.Kind, "asl:smc"), Is(unit, Conditions.Broken), state.Phase, unit.MfSpent,
                    unit.HalfMfSpent, ScenarioA1Searching.HalfMfCost));
                Push(type, payload);
            }

            // A10.2 (ruling R70.3): a leader the Casualty dr eliminates causes a LLMC of the units in his Location.
            (string, bool)[] lostLeaders = [.. struck.Where(id => built.Any(item => item.Payload is InstanceEliminated gone && gone.Id == id)).Select(id => (id, true))];
            if (lostLeaders.Length > 0)
            {
                AddLeaderLossChecks(scope, attemptId, expected, actor, built, draw, state, Replay([.. existing, .. built]).Current!, lostLeaders, ScenarioA1MoraleCheck.Searching, null);
            }

            return built;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1Searching.ProposalText(ids, at.ToString(), drm, notes, accessible.Count, [.. named.Select(hex => hex.ToString())]), .. ConcealmentLossNote(lost)])
        {
            Roll = new PlannedRoll(ScenarioA1Searching.Purpose, Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
