using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// One committed change to a game (ASL-UNIT-040): the envelope of scope, event id, revision, time, source, type, rule
/// package, causes, and visibility, and a typed payload. <see cref="Visibility"/> lists the perspectives entitled to the
/// event; null means every perspective. The adjudicator is entitled to every event.
/// </summary>
public sealed record GameEvent(
    GameScope Scope,
    string EventId,
    long Revision,
    DateTimeOffset Time,
    string Source,
    string Type,
    EventPayload Payload,
    string? RulePackage,
    IReadOnlyList<string> Causes,
    IReadOnlyList<string>? Visibility)
{
    public bool IsVisibleTo(Perspective perspective)
    {
        ArgumentNullException.ThrowIfNull(perspective);
        return perspective.IsAdjudicator || Visibility is null || Visibility.Contains(perspective.Name, StringComparer.Ordinal);
    }
}

/// <summary>What an event says happened. Each payload has one event type name.</summary>
public abstract record EventPayload;

/// <summary>An instance to create: from a catalog definition for units, by kind alone for equipment and entities.</summary>
public sealed record NewInstance(
    string Id,
    string Kind,
    string? Definition,
    string? Side,
    Position? Position,
    Holding? Holding,
    IReadOnlyDictionary<string, ConditionState> Conditions);

/// <summary>
/// <c>game-started</c>: the sides, the map in play, the catalog, and the opening turn and phase. A game that is not
/// synthetic must come from an accepted live source (ASL-UNIT-050, D2). <see cref="SpecialRules"/> names the scenario's
/// SSRs in force; an empty list means none.
/// </summary>
public sealed record GameStarted(
    IReadOnlyList<SideState> Sides,
    MapInPlay Map,
    string Catalog,
    int Turn,
    string Phase,
    string PhasingSide,
    bool Synthetic) : EventPayload
{
    public IReadOnlyList<string> SpecialRules { get; init; } = [];
}

/// <summary><c>phase-changed</c>.</summary>
public sealed record PhaseChanged(int Turn, string Phase, string PhasingSide) : EventPayload;

/// <summary><c>instance-created</c>: a unit, piece of equipment, or entity enters the game.</summary>
public sealed record InstanceCreated(NewInstance Instance) : EventPayload;

/// <summary><c>instance-moved</c>: a unit or entity takes a new position, spending <paramref name="Mf"/> MF when given.</summary>
public sealed record InstanceMoved(string Id, Position Position, int? Mf = null) : EventPayload;

/// <summary><c>equipment-transferred</c>: equipment changes holder, or is left at a map position with none.</summary>
public sealed record EquipmentTransferred(string Id, Holding? Holding, Position? Position) : EventPayload;

/// <summary><c>conditions-changed</c>: one or more condition dimensions take new values.</summary>
public sealed record ConditionsChanged(string Id, IReadOnlyDictionary<string, ConditionState> Conditions) : EventPayload;

/// <summary>What a lineage event records (ASL-UNIT-021).</summary>
public enum LineageAction
{
    /// <summary>A squad becomes a half-squad.</summary>
    Reduced,

    /// <summary>A squad splits into two half-squads (A1.31, p. 45).</summary>
    Deployed,

    /// <summary>Two half-squads become a squad (A1.32, p. 45).</summary>
    Recombined,

    /// <summary>A unit is Replaced by a lesser one (A19.13, p. 86).</summary>
    Replaced,
}

/// <summary><c>lineage</c>: instances consumed and the instances produced from them.</summary>
public sealed record LineageRecorded(LineageAction Action, IReadOnlyList<string> Consumed, IReadOnlyList<NewInstance> Produced) : EventPayload;

/// <summary><c>instance-eliminated</c>.</summary>
public sealed record InstanceEliminated(string Id) : EventPayload;

/// <summary>
/// <c>entry-attempted</c>: a unit attempts, in its MPh, to enter <paramref name="Target"/> at a cost of <paramref name="Mf"/> MF
/// (Occupied and Concealed Entry Design, section 8). It records the cost only: the unit does not move and spends
/// nothing until the attempt is resolved.
/// </summary>
public sealed record EntryAttempted(string Id, BoardLocation Target, int Mf) : EventPayload;

/// <summary>
/// <c>entry-forced-back</c>: the unit of the attempt <paramref name="Attempt"/> is forced back to the location it tried to
/// leave, the attempt's MF count as spent there, and its MPh ends (A12.15, p. 78). <paramref name="FollowOnFireResolved"/>
/// records whether fire on the return was resolved; no action resolves it yet.
/// </summary>
public sealed record EntryForcedBack(string Id, string Attempt, BoardLocation ReturnedTo, int Mf, bool FollowOnFireResolved) : EventPayload;

/// <summary>
/// <c>dice-rolled</c>: a roll drawn by the system inside a game commit and recorded, never drawn again
/// (DICE-09, DICE-12; Random Selection and Declined OVR Design, section 4). <paramref name="Values"/> are in generation
/// order. The roll changes no state; the events that follow it apply its result.
/// </summary>
public sealed record DiceRolled(string Roll, string Purpose, int Count, int Sides, IReadOnlyList<int> Values, string Source, string Actor) : EventPayload
{
    /// <summary>The only source of a recorded roll: the system, not a player.</summary>
    public const string SystemSource = "system";
}

/// <summary>
/// <c>random-selection</c>: which unit each die of a Random Selection roll belongs to (A.9, p. 43), for an open entry
/// attempt (A12.15, p. 78). <paramref name="Subjects"/> are in die order. The units with the highest value are the ones
/// the attempt reveals. It is visible only to the side whose units are selected, since it names concealed units.
/// </summary>
public sealed record RandomSelection(string Roll, string Attempt, IReadOnlyList<string> Subjects) : EventPayload;

/// <summary>
/// <c>overrun-declared</c>: the attacker's choice, after an entry reveals a lone SMC, whether to attempt an Infantry OVR
/// (A12.15, p. 78; A4.15, p. 49). <paramref name="Choice"/> is <see cref="Declined"/> or <see cref="Elected"/>.
/// </summary>
public sealed record OverrunDeclared(string Id, string Attempt, string Choice) : EventPayload
{
    public const string Declined = "declined";
    public const string Elected = "elected";
}

/// <summary>One DRM of a Task Check, with the rule it comes from.</summary>
public sealed record TaskCheckModifier(string Source, int Value);

/// <summary>
/// <c>task-check</c>: a Task Check taken by <paramref name="Id"/> with the recorded roll <paramref name="Roll"/>
/// (A10.1, p. 65; the NTC entry, p. 30; Infantry OVR Design, section 4). It records its arithmetic, which replay
/// recomputes: the final DR is the dice plus every modifier, and the check passes at or below the Morale Level.
/// </summary>
public sealed record TaskCheck(string Id, string Roll, string Purpose, int MoraleLevel, IReadOnlyList<TaskCheckModifier> Modifiers, int FinalDr, bool Passed)
    : EventPayload
{
    /// <summary>The NTC an Infantry OVR takes (A4.15, p. 49).</summary>
    public const string OvrNtc = "ovr-ntc";
}

/// <summary><c>instance-captured</c>: a unit becomes a prisoner in the custody of an enemy unit (A20.2, A20.5).</summary>
public sealed record InstanceCaptured(string Id, string Custodian) : EventPayload;
