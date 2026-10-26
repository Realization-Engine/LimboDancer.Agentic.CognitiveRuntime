using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Who sees (pass 70, task 70.3, ruling R70.16): one read of the nearest seer, by side, purpose, and Location, kept per state; the actions that cost a
/// "?" (A12.141; ruling R70.17); and the Right of Inspection (A12.16; task 70.10, rulings R70.18 and R70.19) with the one entry that builds every view.
/// Rules decides who is a seer and what a LOS read counts as; the planner reads the index and the map and hands the facts over.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// The state's units as seers: each unit's facts in the state's order, and the distinct Locations they stand in, by index (ruling R70.16). Built
    /// once a state, read only.
    /// </summary>
    internal sealed class SightTable(SightSeerFacts[] units, BoardLocation[] locations, Dictionary<BoardLocation, int> indexes)
    {
        /// <summary>Every unit's facts, in the order of <see cref="GameState.Units"/>.</summary>
        public SightSeerFacts[] Units { get; } = units;

        /// <summary>The distinct Locations of the units, by index.</summary>
        public BoardLocation[] Locations { get; } = locations;

        /// <summary>A Location's index in the table, or the index one past the table for a Location no unit stands in.</summary>
        public int IndexOf(BoardLocation location) => indexes.TryGetValue(location, out var index) ? index : Locations.Length;
    }

    /// <summary>The table of the state's seers, built at its first use in one pass over the units, from the state's index (ruling R70.16).</summary>
    private SightTable SightOf(GameState state) => MemoOf(state).Sight(() =>
    {
        var index = MemoOf(state).Objects(state);
        var locations = new List<BoardLocation>();
        var indexes = new Dictionary<BoardLocation, int>();
        var units = new SightSeerFacts[state.Units.Count];
        var position = 0;
        foreach (var unit in state.Units)
        {
            int? at = null;
            if (index.Location(unit.Id)?.Location is { } location)
            {
                if (!indexes.TryGetValue(location, out var found))
                {
                    found = locations.Count;
                    locations.Add(location);
                    indexes[location] = found;
                }

                at = found;
            }

            units[position++] = SeerFactsOf(index, unit, at);
        }

        return new SightTable(units, [.. locations], indexes);
    });

    /// <summary>
    /// One unit's facts as a seer (ruling R70.16), from the state's index. An empty vehicle is one Abandoned (its inherent crew gone, D5.4), with no crew
    /// inside and no Passenger or Rider: "broken" for "?" gain and loss (A12.1, p. 76).
    /// </summary>
    private static SightSeerFacts SeerFactsOf(ObjectIndex index, UnitInstance unit, int? at) =>
        new(unit.Status == InstanceStatus.Active, unit.Side, unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Hidden), index.VehicleOf(unit.Id) is not null, GoodOrder(unit),
            !Is(unit, Conditions.Broken), Is(unit, Conditions.Captured),
            LiveFire.IsVehicle(unit) && Is(unit, Conditions.Abandoned) && index.CrewOf(unit.Id) is null && index.Aboard(unit.Id).Count == 0, at)
        {
            // Pass 70 (A12.14; ruling R70.21): a concealed Loss seer forces a loss only by forfeiting its "?" for a moment.
            Concealed = Is(unit, Conditions.Concealed),
        };

    /// <summary>The LOS between two Locations of the sight table, the index past its end being the Location asked about, as the sight reads take it.</summary>
    private sealed class SightLos(GamePlanner planner, GameState state, SightTable table, BoardLocation at) : ISightLosReader
    {
        private BoardLocation LocationOf(int index) => index < table.Locations.Length ? table.Locations[index] : at;

        public SightLosFacts? Los(int fromLocation, int toLocation) => planner.Los(state, LocationOf(fromLocation), LocationOf(toLocation)) is { } result
            ? new SightLosFacts(result.Status == LosStatus.Clear, result.IsBlocked, result.Range)
            : null;
    }

    /// <summary>The distinct seer Locations of a purpose that may see a unit of the watched side (ruling R70.16), kept per state.</summary>
    private int[] SeerLocations(GameState state, string watchedSide, SightPurpose purpose) =>
        MemoOf(state).SeerLocations.GetOrAdd((watchedSide, purpose), static (key, table) => ScenarioA1Sight.SeerLocations(table.Units, key.Side, key.Purpose), SightOf(state));

    /// <summary>
    /// The range to the nearest seer of a purpose with a LOS to a Location, as the reading takes the LOS; null when none has one (A12.121; ruling
    /// R70.16). The one read of who sees a unit of <paramref name="watchedSide"/>, kept per state.
    /// </summary>
    private int? NearestSeer(GameState state, string watchedSide, BoardLocation at, SightPurpose purpose, SightLosReading reading) =>
        MemoOf(state).NearestSeers.GetOrAdd((watchedSide, purpose, reading, at), static (key, read) =>
        {
            var table = read.Planner.SightOf(read.State);
            return ScenarioA1Sight.NearestSeer(read.Planner.SeerLocations(read.State, key.Side, key.Purpose), table.IndexOf(key.At),
                new SightLos(read.Planner, read.State, table, key.At), key.Reading);
        }, (Planner: this, State: state));

    /// <summary>The range to the nearest Good Order enemy ground unit with a clear LOS to a Location (A12.34); null when none has one (ruling R70.16's Loss seers).</summary>
    private int? NearestGoodOrderEnemyInLos(GameState state, string side, BoardLocation at) => NearestSeer(state, side, at, SightPurpose.Loss, SightLosReading.Clear);

    /// <summary>Whether any Good Order enemy ground unit within 16 hexes has a clear LOS to a Location (A12.14, A12.141; ruling R70.16's Loss seers).</summary>
    private bool EnemyGoodOrderInLosWithin16(GameState state, string side, BoardLocation at) => ScenarioA1Sight.SeenWithin(NearestGoodOrderEnemyInLos(state, side, at), 16);

    /// <summary>
    /// Whether a Good Order enemy ground unit has a LOS to a Location, within a range when one is given (A12.2; ruling R70.16's Loss seers). A vehicle's
    /// concealment takes a LOS that is not blocked, definitive or not, as it did before the pass.
    /// </summary>
    private bool SeenByEnemy(GameState state, string side, BoardLocation at, int? within) =>
        ScenarioA1Sight.SeenWithin(NearestSeer(state, side, at, SightPurpose.Loss, SightLosReading.NotBlocked), within);

    /// <summary>Whether a unit is itself a Loss seer (A12.14, A12.41; ruling R70.16): a vehicle whose entry may force a "?" to be shown, for one.</summary>
    private bool SeesForLoss(GameState state, UnitInstance unit)
    {
        var index = MemoOf(state).Objects(state);
        return ScenarioA1Sight.Sees(SeerFactsOf(index, unit, index.Location(unit.Id) is null ? null : 0), SightPurpose.Loss);
    }

    /// <summary>
    /// An attack with, for each concealed unit that fires or directs, whether a Good Order enemy ground unit within 16 hexes has a LOS to it (A12.14,
    /// read in the PDF, p. 77; pass 31d, ruling R31d.2; pass 70, ruling R70.16's Loss seers). The Fire package sees the target Location alone, and
    /// refused fire by concealed units at a Location with no Good Order unit as undecided; a refusal marks no firer, so a side could try a "?" stack and
    /// read from the refusal that it held Dummies. With this read the attack is made, and the firer's "?" is lost or kept as the rule has it. A hidden
    /// unit, which would have to show itself to force the loss, does not force it. At night the read is not given (E1.31), and the package decides as before.
    /// </summary>
    private FireAttack WithSeen(GameState state, FireAttack attack)
    {
        var index = MemoOf(state).Objects(state);
        return ScenarioA1MovementCalculator.WithSeen(attack, state.Night, unitId => index.Unit(unitId) is { } unit && index.Location(unit.Id)?.Location is { } at
            ? EnemyGoodOrderInLosWithin16(state, unit.Side, at)
            : null);
    }

    /// <summary>
    /// The units of an action that lose their "?" by it (A12.141, p. 78; ruling R70.17): each concealed or hidden one in the LOS of a Good Order enemy
    /// ground unit within 16 hexes, once each, in the order given.
    /// </summary>
    private string[] ActionLosesConcealment(GameState state, IEnumerable<UnitInstance?> actors)
    {
        var index = MemoOf(state).Objects(state);
        return [.. actors.OfType<UnitInstance>().DistinctBy(unit => unit.Id).Where(unit => ScenarioA1Sight.ActionLosesConcealment(Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden),
            () => index.Location(unit.Id)?.Location is { } at ? NearestGoodOrderEnemyInLos(state, unit.Side, at) : null)).Select(unit => unit.Id)];
    }

    /// <summary>The <c>concealment-lost</c> events of an action's units that lose their "?" by it (A12.141; ruling R70.17), added to its events; the ids lost.</summary>
    private string[] AddActionConcealmentLoss(GameScope scope, string attemptId, long expected, List<GameEvent> events, GameState state, params UnitInstance?[] actors)
    {
        var lost = ActionLosesConcealment(state, actors);
        events.AddRange(RevealEvents(scope, attemptId, expected, events.Count + 1, lost));
        return lost;
    }

    /// <summary>The proposal's line for the units an action strips of their "?" (A12.141; ruling R70.17); none when no unit loses one.</summary>
    private static IEnumerable<string> ConcealmentLossNote(string[] lost) => lost.Length == 0 ? []
        : [$"play.concealment: {string.Join(", ", lost)} {(lost.Length == 1 ? "loses its" : "lose their")} \"?\" by this action, in the LOS of a Good Order enemy ground unit within 16 hexes (A12.141)"];

    /// <summary>A new unit's conditions, copied from the unit it came from, with its "?" gone when that unit lost it by the action (A12.141; ruling R70.17).</summary>
    private static Dictionary<string, ConditionState> RevealedIf(IReadOnlyDictionary<string, ConditionState> conditions, bool lost)
    {
        var copy = new Dictionary<string, ConditionState>(conditions, StringComparer.Ordinal);
        if (lost)
        {
            copy[Conditions.Concealed] = ConditionState.False;
            copy[Conditions.Hidden] = ConditionState.False;
        }

        return copy;
    }

    /// <summary>
    /// The Locations of the enemy stacks a viewer may not inspect (A12.16, p. 79; ruling R70.18): after setup closes, each Location of an enemy stack
    /// with a unit not concealed to the viewer, out of the LOS of every Good Order ground unit of the viewer, his hidden ones included and Dummies not.
    /// A LOS the map cannot give, or does not read as blocked, counts as in LOS. Null for the adjudicator and before setup closes (the view shows a
    /// stack's top before play itself, A2.9). Kept per state and viewer; each stack's read stops at the first seer in LOS. A Location where the viewer has
    /// any unit of its own, not a prisoner or a Dummy, is never uninspected (ruling R70.40).
    /// </summary>
    public IReadOnlySet<BoardLocation>? Uninspected(GameState state, Perspective viewer)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(viewer);
        if (viewer.IsAdjudicator || !state.SetupClosed)
        {
            return null;
        }

        return MemoOf(state).Uninspected.GetOrAdd(viewer.Name, static (name, read) => read.Planner.UninspectedOf(read.State, name), (Planner: this, State: state));
    }

    /// <summary>The fill of <see cref="Uninspected"/> for one viewer: reads the sight table and the LOS cache only.</summary>
    private HashSet<BoardLocation> UninspectedOf(GameState state, string name)
    {
        var table = SightOf(state);
        var seers = ScenarioA1Sight.OwnSeerLocations(table.Units, name, SightPurpose.Inspection);
        var index = MemoOf(state).Objects(state);
        var stacks = new HashSet<BoardLocation>();
        foreach (var unit in state.Units)
        {
            // A stack is its units on the map, not aboard a vehicle (the vehicle stands for them); a hidden or concealed one is no stack to inspect.
            if (unit.Status == InstanceStatus.Active && unit.Side != name && !Is(unit, Conditions.Hidden)
                && !ScenarioA1Visibility.ConcealedToEnemy(Is(unit, Conditions.Concealed), unit.Kind == UnitKinds.Dummy)
                && index.VehicleOf(unit.Id) is null && index.Location(unit.Id)?.Location is { } at)
            {
                stacks.Add(at);
            }
        }

        var uninspected = new HashSet<BoardLocation>();
        foreach (var at in stacks)
        {
            // Ruling R70.40: any unit of the viewer's own in the Location inspects it, whatever its state, but a prisoner and a Dummy.
            if (!ScenarioA1Sight.Inspectable(state.SetupClosed, index.At(at).Any(item => item is UnitInstance { Status: InstanceStatus.Active } own && own.Side == name
                    && ScenarioA1Sight.InspectsByPresence(own.Kind == UnitKinds.Dummy, Is(own, Conditions.Captured))),
                () => ScenarioA1Sight.AnySeerInLos(seers, table.IndexOf(at), new SightLos(this, state, table, at), SightLosReading.NotShownBlocked)))
            {
                uninspected.Add(at);
            }
        }

        return uninspected;
    }

    /// <summary>
    /// A perspective's view at a revision (ruling R70.18): the one entry every view is built through, with the OB groups setting up out of its sight
    /// (A12.12; ruling R23.3) and the enemy stacks it may not inspect (A12.16).
    /// </summary>
    public GameView ViewOf(GameHistory history, long revision, Perspective perspective)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(perspective);
        var state = history.At(revision) ?? throw new ArgumentOutOfRangeException(nameof(revision), revision, "The history has no state at that revision.");
        return GameView.Of(history, revision, perspective, OutOfSight(state, perspective), Uninspected(state, perspective));
    }

    /// <summary>A perspective's view of a state with the events given (ruling R70.18), as <see cref="ViewOf(GameHistory, long, Perspective)"/> builds it.</summary>
    public GameView ViewOf(GameState state, Perspective perspective, IReadOnlyList<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(perspective);
        return GameView.Of(state, perspective, events, OutOfSight(state, perspective), Uninspected(state, perspective));
    }
}
