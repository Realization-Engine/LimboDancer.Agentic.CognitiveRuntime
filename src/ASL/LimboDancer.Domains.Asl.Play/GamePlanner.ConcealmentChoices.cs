using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The owner's and the viewer's choices on a "?", and concealment for vehicles and an Emplaced Gun (pass 70, tasks 70.4 and 70.5; rulings R70.20 to
/// R70.22): the voluntary removal of a "?" (A12.14), the viewer's option not to force a loss in a move (A12.14), a vehicle's road hex at setup (A12.2),
/// and an Emplaced Gun's HIP lost to an enemy's LOS (A12.34). Rules decides each; this file reads the state and the map and writes the events.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>Whether a hex is a woods-, orchard-, brush-, or grain-road hex (A12.2; ruling R70.22): the map's facts handed to Rules.</summary>
    private bool RoadConcealmentHex(GameState state, BoardLocation at) => ReadLocation(state, at) is { } read
        && ScenarioA1ConcealmentChoices.RoadConcealmentHex((read.Level.Terrain ?? read.Hex.Center.Terrain)?.IsRoad == true, read.Hex.Hexsides.Select(side => side.Terrain?.Name),
            state.ScenarioMonth);

    /// <summary>
    /// The voluntary removal of "?" (A12.14, A12.11, A12.31, A12.2; ruling R70.20): units of one side on the map, each concealed, hidden, or a Dummy, once
    /// play has begun, in either side's Player Turn. A Dummy is removed; a concealed or hidden unit is revealed in full, the Gun it mans and the PRC of a
    /// vehicle with it. The events are public: the removal shows the unit.
    /// </summary>
    private GamePlan PlanRemoveConcealment(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game");
        }

        var ids = Strings(arguments, "unitIds").Distinct(StringComparer.Ordinal).ToArray();
        var index = MemoOf(state).Objects(state);
        var units = ids.Select(index.Unit).ToArray();
        if (ScenarioA1ConcealmentChoices.RemovalBar(existing.All(item => GameState.IsSetupEvent(item.Payload)), ids.Length,
            units.Any(unit => unit is not { Status: InstanceStatus.Active } || index.Location(unit.Id) is null
                || !(unit.Kind == UnitKinds.Dummy || Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden))),
            ids.Length == 0 ? 0 : units.Select(unit => unit?.Side).Distinct().Count()) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        var events = new List<GameEvent>();
        var named = new List<string>();
        var dummies = 0;
        foreach (var unit in units.OfType<UnitInstance>())
        {
            if (ScenarioA1ConcealmentChoices.RemovalEliminates(unit.Kind == UnitKinds.Dummy))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(unit.Id), null, null));
                dummies++;
                continue;
            }

            // A12.2: "If a Gun, vehicle ... loses its hidden/concealed status ... so does its manning Infantry or PRC, and vice versa."
            IEnumerable<IGameObject> sharing = [unit, .. state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Kind == "asl:gun"
                && item.Holding is { Role: HoldingRole.Manned } manned && manned.Holder == unit.Id), .. index.Aboard(unit.Id)];
            events.AddRange(RevealEvents(scope, attemptId, expected, events.Count + 1, [.. sharing.Where(item => Is(item, Conditions.Concealed) || Is(item, Conditions.Hidden))
                .Select(item => item.Id).Distinct(StringComparer.Ordinal)]));
            named.Add(unit.Id);
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [ScenarioA1ConcealmentChoices.RemovalText(named, dummies)]);
    }

    /// <summary>
    /// The words of a pending choice in another action's refusal (ruling R5.8): the viewer's option is the viewing side's to know (A12.14; ruling R70.21),
    /// so its refusal says only that the DEFENDER answers at the moving stack's step.
    /// </summary>
    private static string RefusalWordsOf(PendingChoice choice) => choice.Kind == ChoicePending.ConcealmentLoss
        ? "the moving stack waits for the DEFENDER's answer at its step (A8.1)"
        : DescribeChoice(choice);

    /// <summary>
    /// The range to the nearest Loss seer not itself concealed with a clear LOS to a Location (A12.14; ruling R70.21), kept per state with the other seer
    /// reads; null when none has one.
    /// </summary>
    private int? NearestUnconcealedLossSeer(GameState state, string side, BoardLocation at) => NearestSeer(state, side, at, SightPurpose.LossInView, SightLosReading.Clear);

    /// <summary>
    /// The concealed Loss seers of the viewing side at the nearest Location within 16 hexes with a clear LOS to a Location (A12.14; ruling R70.21): the
    /// units that would forfeit their "?" for a moment to force the loss. Read once, when the option is offered.
    /// </summary>
    private string[] ConcealedLossSeers(GameState state, string watchedSide, BoardLocation at)
    {
        var table = SightOf(state);
        var los = new SightLos(this, state, table, at);
        var target = table.IndexOf(at);
        int? nearest = null;
        var found = new List<(int Range, string Id)>();
        var position = 0;
        foreach (var unit in state.Units)
        {
            var facts = table.Units[position++];
            if (facts.Side == watchedSide || !facts.Concealed || !ScenarioA1Sight.Sees(facts, SightPurpose.Loss))
            {
                continue;
            }

            var range = facts.Location == target ? 0 : los.Los(facts.Location!.Value, target) is { } read && ScenarioA1Sight.InLos(read, SightLosReading.Clear) ? read.Range : (int?)null;
            if (range is { } seen && seen <= 16)
            {
                found.Add((seen, unit.Id));
                nearest = nearest is null ? seen : Math.Min(nearest.Value, seen);
            }
        }

        return [.. found.Where(item => item.Range == nearest).Select(item => item.Id).Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// The viewer's pending choice (A12.14; ruling R70.21): the side whose concealed units alone see the stack forces its "?" loss or declines. The choice
    /// is recorded for that side only, so the moving side learns nothing of it unless the loss is forced.
    /// </summary>
    private GameEvent LossChoiceEvent(GameScope scope, string attemptId, int index, long expected, GameState state, string movingSide, BoardLocation at, IReadOnlyList<string> movers)
    {
        var viewing = state.Sides.First(side => side.Id != movingSide).Id;
        var resume = new JsonObject
        {
            ["record"] = ChoicePending.ConcealmentLoss,
            ["location"] = at.ToString(),
            ["movers"] = new JsonArray([.. movers.Select(id => (JsonNode)id)]),
            ["seers"] = new JsonArray([.. ConcealedLossSeers(state, movingSide, at).Select(id => (JsonNode)id)]),
        };
        return Event(scope, attemptId, index, expected, "choice-pending", new ChoicePending($"{ChoicePending.ConcealmentLoss}:{at}", ChoicePending.ConcealmentLoss, viewing,
            [ChoicePending.Take, ChoicePending.Decline], JsonSerializer.SerializeToElement(resume)), null, [viewing]);
    }

    /// <summary>
    /// The viewing side's answer (A12.14; ruling R70.21): to force the loss, its seers forfeit their "?" for a moment, shown to the moving side only, and
    /// the movers lose theirs (a Dummy is removed); to decline, nothing changes and nothing is shown to the moving side.
    /// </summary>
    private GamePlan PlanConcealmentLossAnswer(GameScope scope, string attemptId, long expected, string label, GameState state, PendingChoice pending, string option, string summary)
    {
        var resume = pending.Resume;
        var at = resume.GetProperty("location").GetString()!;
        var made = Event(scope, attemptId, 1, expected, "choice-made", new ChoiceMade(pending.Key, option), null, [pending.Side], [pending.Event]);
        if (option != ChoicePending.Take)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [made], [summary, ScenarioA1ConcealmentChoices.LossAnswerText(false, at, [])]);
        }

        UnitInstance[] movers = [.. resume.GetProperty("movers").EnumerateArray().Select(item => state.Unit(item.GetString()!)).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active)];
        string[] seers = [.. resume.GetProperty("seers").EnumerateArray().Select(item => item.GetString()!)
            .Where(id => state.Unit(id) is { Status: InstanceStatus.Active } seer && Is(seer, Conditions.Concealed))];
        var events = new List<GameEvent> { made };
        var movingSide = state.Sides.First(side => side.Id != pending.Side).Id;
        if (seers.Length > 0)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "concealment-verified", new ConcealmentVerified(seers), null, [movingSide], [made.EventId]));
        }

        foreach (var unit in movers)
        {
            events.Add(unit.Kind == UnitKinds.Dummy
                ? Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(unit.Id), null, null, [made.EventId])
                : RevealEvents(scope, attemptId, expected, events.Count + 1, [unit.Id]).Single());
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
            [summary, ScenarioA1ConcealmentChoices.LossAnswerText(true, at, [.. movers.Select(unit => unit.Id)])]);
    }

    /// <summary>
    /// The hidden Emplaced Guns, with their manning crews, that a Good Order enemy ground unit now has a LOS to out of Concealment Terrain (A12.34; ruling
    /// R70.22): each placed beneath "?". Read after each step and at each phase; the terrain and the LOS only for a hidden Emplaced Gun.
    /// </summary>
    private List<(string Id, string Side)> EmplacedHipLost(GameState state)
    {
        var lost = new List<(string Id, string Side)>();
        foreach (var gun in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Kind == "asl:gun" && Is(item, Conditions.Hidden)))
        {
            if (gun.Holding is not { Role: HoldingRole.Manned } manned || state.Location(gun.Id)?.Location is not { } at || state.Unit(manned.Holder) is not { } crew)
            {
                continue;
            }

            if (ScenarioA1ConcealmentChoices.EmplacedHipLost(true, LiveOrdnance.Emplaced(state, gun, nonCrewManning: true),
                () => ReadLocation(state, at) is { } read && TerrainKey(read) is { } key && ScenarioA1Definitions.IsConcealmentTerrain(key, state.ScenarioMonth),
                () => NearestGoodOrderEnemyInLos(state, crew.Side, at) is not null))
            {
                lost.Add((gun.Id, crew.Side));
                if (Is(crew, Conditions.Hidden))
                {
                    lost.Add((crew.Id, crew.Side));
                }
            }
        }

        return lost;
    }

    /// <summary>
    /// The <c>hidden-placed</c> events of the hidden Emplaced Guns an enemy's LOS now finds in the state after <paramref name="events"/> (A12.34; ruling
    /// R70.22), seen by their owner only: the enemy sees a "?" appear, and the proposal says nothing of it, since either side may propose the step or the
    /// phase. The state is replayed only when a hidden Gun is in play.
    /// </summary>
    private void AddEmplacedHipLoss(GameScope scope, string attemptId, long expected, IReadOnlyList<GameEvent> existing, List<GameEvent> events)
    {
        if (Replay(existing).Current is not { } before || !before.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Kind == "asl:gun" && Is(item, Conditions.Hidden))
            || Replay([.. existing, .. events]).Current is not { } after)
        {
            return;
        }

        foreach (var (id, side) in EmplacedHipLost(after))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "hidden-placed", new ConditionsChanged(id, ConditionChanges(ScenarioA1Concealment.PlaceHiddenConditions())),
                null, [side]));
        }
    }
}
