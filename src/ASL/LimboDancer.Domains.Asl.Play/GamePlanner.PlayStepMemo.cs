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

        // The state's units as seers (pass 70, ruling R70.16), built at its first use from the index; two circuits may each build it, and either is kept.
        private SightTable? sight;

        /// <summary>
        /// Every unit's facts as a seer and the table of their Locations (ruling R70.16), built once from <see cref="Objects"/> in one pass over the units.
        /// View-independent: it holds every side's units, hidden ones too, and is read only by the sight reads below, never shown.
        /// </summary>
        public SightTable Sight(Func<SightTable> build) => Volatile.Read(ref sight) ?? (sight = build());

        /// <summary>
        /// The distinct seer Locations by watched side and purpose (ruling R70.16), indexes into <see cref="Sight"/>'s table: the units of every side but
        /// the watched one that see for the purpose (an Inspection key holds the viewer's own seers). The fill reads the table only.
        /// </summary>
        public ConcurrentDictionary<(string Side, SightPurpose Purpose), int[]> SeerLocations { get; } = new();

        /// <summary>
        /// The range to the nearest seer with a LOS (ruling R70.16), by watched side, purpose, LOS reading, and Location; null when none has one. It reads the
        /// seeing side's units as the state holds them, so it is read for decisions and proposals, never shown to the watched side. The fill reads the
        /// planner's LOS cache and has no side effect.
        /// </summary>
        public ConcurrentDictionary<(string Side, SightPurpose Purpose, SightLosReading Reading, BoardLocation At), int?> NearestSeers { get; } = new();

        /// <summary>
        /// The Locations of the enemy stacks a viewer may not inspect (A12.16; ruling R70.18), by the viewer's name; null before setup closes and for the
        /// adjudicator. Keyed by viewer, since it reads the viewer's own seers (hidden ones too): it is that viewer's to know.
        /// </summary>
        public ConcurrentDictionary<string, IReadOnlySet<BoardLocation>?> Uninspected { get; } = new(StringComparer.Ordinal);

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
        private readonly Dictionary<string, string> vehicles;
        private readonly Dictionary<string, UnitInstance[]> aboard;
        private readonly Dictionary<string, EquipmentInstance[]> stowed;
        private readonly Dictionary<string, UnitInstance> crews;
        private readonly Dictionary<string, UnitInstance> units;
        private readonly Dictionary<BoardLocation, UnitInstance[]> wrecks;
        private readonly HashSet<string> entities;

        private ObjectIndex(Dictionary<string, MapPosition?> locations, Dictionary<BoardLocation, IGameObject[]> at, Dictionary<string, string> vehicles,
            Dictionary<string, UnitInstance[]> aboard, Dictionary<string, EquipmentInstance[]> stowed, Dictionary<string, UnitInstance> crews,
            Dictionary<string, UnitInstance> units, Dictionary<BoardLocation, UnitInstance[]> wrecks, HashSet<string> entities)
        {
            this.locations = locations;
            this.at = at;
            this.vehicles = vehicles;
            this.aboard = aboard;
            this.stowed = stowed;
            this.crews = crews;
            this.units = units;
            this.wrecks = wrecks;
            this.entities = entities;
        }

        public static ObjectIndex Of(GameState state)
        {
            var locations = new Dictionary<string, MapPosition?>(StringComparer.Ordinal);
            var vehicles = new Dictionary<string, string>(StringComparer.Ordinal);
            var stacks = new Dictionary<BoardLocation, List<IGameObject>>();
            var cargo = new Dictionary<string, List<UnitInstance>>(StringComparer.Ordinal);
            var stowed = new Dictionary<string, List<EquipmentInstance>>(StringComparer.Ordinal);
            var crews = new Dictionary<string, UnitInstance>(StringComparer.Ordinal);
            var units = new Dictionary<string, UnitInstance>(StringComparer.Ordinal);
            var wrecks = new Dictionary<BoardLocation, List<UnitInstance>>();
            var entities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in state.Objects)
            {
                // GameState.Location and Aboard read an id's first object, as Find does; At still lists every object, so a repeated id is too.
                string? vehicle;
                if (!locations.TryGetValue(item.Id, out var position))
                {
                    position = state.Location(item.Id);
                    locations[item.Id] = position;
                    vehicle = state.Aboard(item.Id);
                    if (vehicle is not null)
                    {
                        vehicles[item.Id] = vehicle;
                    }
                }
                else
                {
                    vehicle = vehicles.GetValueOrDefault(item.Id);
                }

                if (item.Status == InstanceStatus.Active && position is not null && vehicle is null)
                {
                    if (!stacks.TryGetValue(position.Location, out var stack))
                    {
                        stacks[position.Location] = stack = [];
                    }

                    stack.Add(item);
                }

                // Pass 65, ruling R65.25: the PRC of each vehicle, as GameState.Passengers reads them (active units in the Passenger or Rider role).
                if (item is UnitInstance { Status: InstanceStatus.Active, Position: ContainedPosition { Role: ContainmentRole.Passenger or ContainmentRole.Rider } contained } unit)
                {
                    if (!cargo.TryGetValue(contained.Container, out var riding))
                    {
                        cargo[contained.Container] = riding = [];
                    }

                    riding.Add(unit);
                }

                // D5.42 (pass 65, ruling R65.20): the unit that crews a vehicle from inside, no Passenger; GameState.Aboard reads it in the vehicle too.
                if (item is UnitInstance { Status: InstanceStatus.Active, Position: ContainedPosition { Role: ContainmentRole.Crew } crewing } crew)
                {
                    crews.TryAdd(crewing.Container, crew);
                }

                // D6.4 (pass 65, ruling R65.15): a SW left aboard a halftrack or Carrier, held by no unit.
                if (item is EquipmentInstance { Status: InstanceStatus.Active, Holding: null, Position: ContainedPosition { Role: ContainmentRole.Passenger } kept } weapon)
                {
                    if (!stowed.TryGetValue(kept.Container, out var held))
                    {
                        stowed[kept.Container] = held = [];
                    }

                    held.Add(weapon);
                }

                // Pass 65 (the UI review): units by id as GameState.Unit reads them (the first), the wrecks by Location as WrecksAt reads them, and the
                // active entities by id, for a Blaze on a wreck (B25.14), so the Play page reads none of them by a scan per item.
                if (item is UnitInstance found)
                {
                    units.TryAdd(found.Id, found);
                    if (found.Status == InstanceStatus.Wrecked && position is not null)
                    {
                        if (!wrecks.TryGetValue(position.Location, out var here))
                        {
                            wrecks[position.Location] = here = [];
                        }

                        here.Add(found);
                    }
                }

                if (item is EntityInstance { Status: InstanceStatus.Active } entity)
                {
                    entities.Add(entity.Id);
                }
            }

            return new ObjectIndex(locations, stacks.ToDictionary(item => item.Key, item => item.Value.ToArray()), vehicles,
                cargo.ToDictionary(item => item.Key, item => item.Value.ToArray(), StringComparer.Ordinal),
                stowed.ToDictionary(item => item.Key, item => item.Value.ToArray(), StringComparer.Ordinal), crews, units,
                wrecks.ToDictionary(item => item.Key, item => item.Value.ToArray()), entities);
        }

        /// <summary>A unit by id, as <see cref="GameState.Unit"/> reads it; null for an id the state holds no unit by.</summary>
        public UnitInstance? Unit(string id) => units.GetValueOrDefault(id);

        /// <summary>The wrecks at a Location, as <see cref="WrecksAt"/> reads them (D10.1).</summary>
        public IReadOnlyList<UnitInstance> Wrecks(BoardLocation location) => wrecks.TryGetValue(location, out var here) ? here : [];

        /// <summary>Whether an active entity has this id: a Blaze by its name (B25.14).</summary>
        public bool ActiveEntity(string id) => entities.Contains(id);

        /// <summary>The unit that crews a vehicle from inside after a re-crewing (D5.42; pass 65, ruling R65.20); null for an inherent crew or none.</summary>
        public UnitInstance? CrewOf(string vehicle) => crews.GetValueOrDefault(vehicle);

        /// <summary>The SW left aboard a vehicle, held by no unit (D6.4; pass 65, ruling R65.15).</summary>
        public IReadOnlyList<EquipmentInstance> Stowed(string vehicle) => stowed.TryGetValue(vehicle, out var held) ? held : [];

        /// <summary>The active units riding a vehicle as Passengers or Riders, as <see cref="GameState.Passengers"/> reads them (D6.1, D6.2; ruling R65.25).</summary>
        public IReadOnlyList<UnitInstance> Aboard(string vehicle) => aboard.TryGetValue(vehicle, out var riding) ? riding : [];

        /// <summary>The units riding a vehicle in one role (D6.1 Passengers, D6.2 Riders).</summary>
        public IEnumerable<UnitInstance> Aboard(string vehicle, ContainmentRole role) =>
            Aboard(vehicle).Where(unit => unit.Position is ContainedPosition contained && contained.Role == role);

        /// <summary>The vehicle an id rides, or whose Passenger or Rider holds it, as <see cref="GameState.Aboard"/> reads it; null when it is on foot.</summary>
        public string? VehicleOf(string id) => vehicles.GetValueOrDefault(id);

        /// <summary>The active objects in a Location, as <see cref="GameState.At"/> reads them.</summary>
        public IReadOnlyList<IGameObject> At(BoardLocation location) => at.TryGetValue(location, out var stack) ? stack : [];

        /// <summary>
        /// The occupied Locations and their active objects, as <see cref="At"/> holds them (pass 70, ruling R70.25): built with the index, so Searching and
        /// Mopping Up read the stacks of a set of hexes in one pass over them, never with a lookup per Location.
        /// </summary>
        public IEnumerable<KeyValuePair<BoardLocation, IGameObject[]>> Occupied => at;

        /// <summary>Where an object is, as <see cref="GameState.Location"/> reads it; null for an id the state does not hold.</summary>
        public MapPosition? Location(string id) => locations.GetValueOrDefault(id);
    }
}
