using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// What a Play step asks the rules again and again about one state, kept with that state (the Play Step Plan, V5; the analysis's ruling P4). A
/// <see cref="GameState"/> is an immutable snapshot of one revision, so an answer read from it stays true for as long as the state lives; the memo
/// lives exactly as long, in a <see cref="ConditionalWeakTable{TKey, TValue}"/> keyed by the state object. The planner is shared by every circuit,
/// so the stores are concurrent; two circuits that fill the same entry at once compute the same answer, and either may be kept.
/// </summary>
public sealed partial class GamePlanner
{
    private readonly ConditionalWeakTable<GameState, PlayStepMemo> playStepMemos = [];

    private PlayStepMemo MemoOf(GameState state) => playStepMemos.GetValue(state, static _ => new PlayStepMemo());

    /// <summary>The answers kept for one state.</summary>
    private sealed class PlayStepMemo
    {
        /// <summary>
        /// A7.72's answer per unit, keyed by the unit object itself: the answer reads the unit's own fields (its side, status, kind, and conditions)
        /// as well as the state, so only the state's own instance of a unit may share it. It reads no view (the rule counts unconcealed enemy units
        /// only) and no enemy list, so it is kept at once.
        /// </summary>
        public ConcurrentDictionary<UnitInstance, bool> UpperLevelEncircled { get; } = new(ReferenceEqualityComparer.Instance);

        // 1 once the state's SurrenderedBeforeRouts set is complete; written after the set is filled, read with a volatile read.
        private int routSettled;

        /// <summary>
        /// Whether the state's <c>SurrenderedBeforeRouts</c> set is complete (the plan's V6, the surrender window). While it is being filled, the
        /// rout's answers read an enemy list with no prisoner in it yet, which is right only for the read that decides the surrenders: so the rout
        /// stores below are read and written only for a settled state, and a caller checks this before it computes, never after.
        /// </summary>
        public bool RoutSettled => Volatile.Read(ref routSettled) == 1;

        public void SettleRout() => Volatile.Write(ref routSettled, 1);

        /// <summary>
        /// The Known enemy units of a side as the rout reads them, each with its Location; each scan numbers the Locations in its own table, so the
        /// facts are kept with no Location index, and a scan gives each its own in the list's order.
        /// </summary>
        public ConcurrentDictionary<string, (BoardLocation At, RoutEnemyFacts Facts)[]> RoutEnemies { get; } = new(StringComparer.Ordinal);

        /// <summary><c>MustRout</c>'s answer per unit, keyed by the unit object as <see cref="UpperLevelEncircled"/> is.</summary>
        public ConcurrentDictionary<UnitInstance, string?> MustRout { get; } = new(ReferenceEqualityComparer.Instance);

        /// <summary><c>RoutSurrender</c>'s answer per unit, with the Location it was asked from.</summary>
        public ConcurrentDictionary<UnitInstance, (BoardLocation Start, (string Cause, IReadOnlyList<string> Captors)? Surrender)> RoutSurrender { get; } = new(ReferenceEqualityComparer.Instance);

        /// <summary><c>RoutAdvice</c>'s answer per unit.</summary>
        public ConcurrentDictionary<UnitInstance, (IReadOnlyList<BoardLocation> Targets, bool CanRout, IReadOnlyDictionary<BoardLocation, IReadOnlyList<BoardLocation>> Routes)> RoutAdvice { get; } = new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// The map context (the A3 plan, W3): each board's handle at the version the state pins, looked up once; null when the board is not on the
        /// map or cannot be read. A footprint published while the state lives is read from the next state on (W4; the user, 2026-10-19).
        /// </summary>
        public ConcurrentDictionary<BoardRef, BoardHandle?> Handles { get; } = new();

        /// <summary>
        /// Each Location's read, resolved once a state (the A3 plan, task 3b; the user's ruling on Q2): a Rout Phase step resolved each Location
        /// about two hundred times. A read depends only on the board's handle, which the state keeps.
        /// </summary>
        public ConcurrentDictionary<BoardLocation, LocationRead?> Reads { get; } = new();

        /// <summary>
        /// Whether the DEFENDER could fire at the moving stack's step (the Play streamlining plan, S6), keyed by the step's Location and the Location
        /// the stack came from; the state fixes the sides, the units, and the markers. It counts the DEFENDER's concealed and hidden units, so it is
        /// read only for his view and for passing for him, never shown to the moving side.
        /// </summary>
        public ConcurrentDictionary<(BoardLocation At, BoardLocation? From), bool> DefenderMayFire { get; } = new();

        /// <summary>
        /// A berserk stack's charge (the Berserk Charge Plan, T1): the search's answer by stack, start, and whether the stack is in the state's move.
        /// It reads the state only (a Known enemy unit is read from the unit's own conditions, never from a view), so one answer serves every view.
        /// Kept only for the state's own unit objects and its own move (<see cref="ChargeKeyOf"/>). The kept answer is read, never written.
        /// </summary>
        public ConcurrentDictionary<ChargeKey, (IReadOnlyDictionary<BoardLocation, ChargeStep> Steps, BoardLocation? Target, string? Undecided)> Charges { get; } = new();

        /// <summary>The map edges a Location lies on (the Play streamlining plan, S7): the map's geometry, read once a state and Location.</summary>
        public ConcurrentDictionary<BoardLocation, string[]> ExitEdges { get; } = new();

        // The index of the state's objects (the Berserk Charge Plan, T2), built at its first use; two circuits may each build it, and either is kept.
        private ObjectIndex? objects;

        /// <summary>
        /// The state's objects by id and by Location (C2 of the Play Step Analysis; the Berserk Charge Plan, T2, ruling C1): built once from the state's
        /// own <see cref="GameState.Location"/> and <see cref="GameState.Aboard"/>, so it answers exactly as <see cref="GameState.At"/> and
        /// <see cref="GameState.Location"/> do, without their scans of every object. View-independent.
        /// </summary>
        public ObjectIndex Objects(GameState state) => Volatile.Read(ref objects) ?? (objects = ObjectIndex.Of(state));

        // The ids of the units that guard an active prisoner, read once for the state; two circuits may each build it, and either is kept.
        private IReadOnlySet<string>? custodians;

        /// <summary>The units guarding an active prisoner (A20.5): the Deploy form asks it for each squad it offers. View-independent.</summary>
        public IReadOnlySet<string> Custodians(GameState state) => Volatile.Read(ref custodians)
            ?? (custodians = state.Units.Where(prisoner => prisoner.Status == InstanceStatus.Active && prisoner.Custodian is not null)
                .Select(prisoner => prisoner.Custodian!).ToHashSet(StringComparer.Ordinal));

        // 1 once the ATTACKER's first unit owing a rout is kept (null kept too); written after the answer, read with a volatile read.
        private int attackerRoutKept;
        private UnitInstance? attackerRout;

        /// <summary>
        /// The ATTACKER's first unit that still owes a rout (A10.5), read once for a settled state: the gate asks it for each DEFENDER's rout and the page
        /// for each render of the RtPh. Two circuits may each read it; they read the same answer.
        /// </summary>
        public UnitInstance? AttackerRoutOwed(Func<UnitInstance?> read)
        {
            if (Volatile.Read(ref attackerRoutKept) == 1)
            {
                return attackerRout;
            }

            var found = read();
            attackerRout = found;
            Volatile.Write(ref attackerRoutKept, 1);
            return found;
        }

        // 1 once the placed map's composed read is kept; written after the read, read with a volatile read.
        private int composedKept;
        private ComposedMapRead? composed;

        /// <summary>A placed map's composed read, built at its first use from the kept handles and then kept.</summary>
        public ComposedMapRead? Composed(GamePlanner planner, GameState state)
        {
            if (Volatile.Read(ref composedKept) == 1)
            {
                return composed;
            }

            var made = planner.ComposedOf(state);
            composed = made;
            Volatile.Write(ref composedKept, 1);
            return made;
        }
    }

    /// <summary>
    /// A state's objects by id and by Location (the Berserk Charge Plan, T2). <see cref="At"/> holds what <see cref="GameState.At"/> returns, in the
    /// same order (the state's objects in order: units, equipment, entities); <see cref="Location"/> what <see cref="GameState.Location"/> returns.
    /// Read only, never written after it is built.
    /// </summary>
    internal sealed class ObjectIndex
    {
        private readonly Dictionary<string, MapPosition?> locations;
        private readonly Dictionary<BoardLocation, IGameObject[]> at;

        private ObjectIndex(Dictionary<string, MapPosition?> locations, Dictionary<BoardLocation, IGameObject[]> at)
        {
            this.locations = locations;
            this.at = at;
        }

        public static ObjectIndex Of(GameState state)
        {
            var locations = new Dictionary<string, MapPosition?>(StringComparer.Ordinal);
            var stacks = new Dictionary<BoardLocation, List<IGameObject>>();
            foreach (var item in state.Objects)
            {
                // GameState.Location and Aboard read an id's first object, as Find does; At still lists every object, so a repeated id is too.
                if (!locations.TryGetValue(item.Id, out var position))
                {
                    position = state.Location(item.Id);
                    locations[item.Id] = position;
                }

                if (item.Status == InstanceStatus.Active && position is not null && state.Aboard(item.Id) is null)
                {
                    if (!stacks.TryGetValue(position.Location, out var stack))
                    {
                        stacks[position.Location] = stack = [];
                    }

                    stack.Add(item);
                }
            }

            return new ObjectIndex(locations, stacks.ToDictionary(item => item.Key, item => item.Value.ToArray()));
        }

        /// <summary>The active objects in a Location, as <see cref="GameState.At"/> reads them.</summary>
        public IReadOnlyList<IGameObject> At(BoardLocation location) => at.TryGetValue(location, out var stack) ? stack : [];

        /// <summary>Where an object is, as <see cref="GameState.Location"/> reads it; null for an id the state does not hold.</summary>
        public MapPosition? Location(string id) => locations.GetValueOrDefault(id);
    }
}
