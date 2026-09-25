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

/// <summary><c>game-started</c>: the sides, the map in play, the catalog, and the opening turn and phase.</summary>
public sealed record GameStarted(
    IReadOnlyList<SideState> Sides,
    MapInPlay Map,
    string Catalog,
    int Turn,
    string Phase,
    string PhasingSide,
    bool Synthetic) : EventPayload;

/// <summary><c>phase-changed</c>.</summary>
public sealed record PhaseChanged(int Turn, string Phase, string PhasingSide) : EventPayload;

/// <summary><c>instance-created</c>: a unit, piece of equipment, or entity enters the game.</summary>
public sealed record InstanceCreated(NewInstance Instance) : EventPayload;

/// <summary><c>instance-moved</c>: a unit or entity takes a new position.</summary>
public sealed record InstanceMoved(string Id, Position Position) : EventPayload;

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

/// <summary><c>instance-captured</c>: a unit becomes a prisoner in the custody of an enemy unit (A20.2, A20.5).</summary>
public sealed record InstanceCaptured(string Id, string Custodian) : EventPayload;
