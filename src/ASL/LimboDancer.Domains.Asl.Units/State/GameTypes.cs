using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// Who a read is for (ASL-UNIT-030, D3): one of the game's sides, or the adjudicator, who sees everything. The set is
/// closed per game: its sides plus <see cref="Adjudicator"/>. Perspectives are names, so adding one needs no model change.
/// </summary>
public sealed record Perspective(string Name)
{
    public const string AdjudicatorName = "adjudicator";

    public static Perspective Adjudicator { get; } = new(AdjudicatorName);

    public bool IsAdjudicator => Name == AdjudicatorName;

    public static Perspective Side(string side) => new(side);

    public override string ToString() => Name;
}

/// <summary>The tenant and game every event and read is scoped to (ASL-UNIT-020).</summary>
public sealed record GameScope(Guid Tenant, string Game)
{
    public override string ToString() => $"{Tenant:D}/{Game}";
}

/// <summary>
/// A side (ASL-UNIT-020): its id, which is also its perspective name, its nationality, and the ELR (A19.1, p. 86) and SAN
/// (A14.1, p. 82) its scenario OB gives it. ELR and SAN are the side's, not a unit's.
/// </summary>
public sealed record SideState(string Id, string Nationality, int? Elr, int? San);

/// <summary>A board placed in the map in play, with the exact board version positions are checked against.</summary>
public sealed record PlacedBoard(BoardRef Board, string Version);

/// <summary>
/// The board or composed map in play (ASL-UNIT-020, 024): its reference and version, and the boards placed in it.
/// Positions keep the placed board's reference, never map coordinates (ASL-MAP-024).
/// </summary>
public sealed record MapInPlay(string Reference, string Version, IReadOnlyList<PlacedBoard> Boards)
{
    public PlacedBoard? Board(BoardRef board) => Boards.FirstOrDefault(placed => placed.Board == board);
}

/// <summary>The phases of a Player Turn (A3.1 to A3.8, p. 47).</summary>
public static class Phases
{
    public static IReadOnlyList<string> All { get; } = ["rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph"];
}

/// <summary>What is known about one condition of an instance (ASL-UNIT-023).</summary>
public enum ConditionState
{
    /// <summary>Nothing is recorded; the default for every condition.</summary>
    Unknown,
    False,
    True,

    /// <summary>The value exists but this perspective may not know it.</summary>
    Withheld,

    /// <summary>The condition cannot apply to this instance.</summary>
    Inapplicable,
}

/// <summary>
/// The condition dimensions an instance can have (ASL-UNIT-023): each a separate dimension, never one state enumeration.
/// They are the vocabulary's states, which the display draws, plus three the display does not draw.
/// </summary>
public static class Conditions
{
    public const string Broken = "asl:broken";
    public const string Berserk = "asl:berserk";
    public const string Concealed = "asl:concealed";

    /// <summary>Hidden Initial Placement (A12.3, p. 80): the opponent is told nothing, not even presence.</summary>
    public const string Hidden = "asl:hidden";

    /// <summary>Captured (A20.2, p. 86), held by a captor (A20.5, p. 87).</summary>
    public const string Captured = "asl:captured";

    /// <summary>Held in Melee (A11.15, p. 72).</summary>
    public const string Melee = "asl:melee";

    /// <summary>Conditions the state model adds to the vocabulary's states; they have no drawn form.</summary>
    public static IReadOnlyList<string> Undrawn { get; } = [Hidden, Captured, Melee];

    public static bool IsDeclared(string name, Vocabulary.UnitVocabulary vocabulary) =>
        Undrawn.Contains(name, StringComparer.Ordinal) || vocabulary.TryGetState(name, out _);

    public static ConditionState Parse(string text) => text switch
    {
        "true" => ConditionState.True,
        "false" => ConditionState.False,
        "unknown" => ConditionState.Unknown,
        "withheld" => ConditionState.Withheld,
        "inapplicable" => ConditionState.Inapplicable,
        _ => throw new FormatException($"'{text}' is not true, false, unknown, withheld, or inapplicable."),
    };

    public static string Name(ConditionState state) => state.ToString().ToLowerInvariant();
}

/// <summary>Where an instance is (ASL-UNIT-024).</summary>
public abstract record Position;

/// <summary>
/// A map location: the placed board, hex, and one location in the hex's derived location chain (the level, -1 for a
/// cellar), or its bridge; a hexside where the placement needs one; and a facing for kinds that face.
/// </summary>
public sealed record MapPosition(BoardLocation Location, bool OnBridge = false, UnitFacing? Facing = null) : Position
{
    public override string ToString() => Location + (OnBridge ? " bridge" : string.Empty);
}

/// <summary>How an instance is inside another.</summary>
public enum ContainmentRole
{
    /// <summary>A Passenger inside a vehicle.</summary>
    Passenger,

    /// <summary>A Rider on a vehicle.</summary>
    Rider,

    /// <summary>In a fortification such as a foxhole or pillbox.</summary>
    InFortification,
}

/// <summary>Inside another instance; its location is the container's (ASL-UNIT-025).</summary>
public sealed record ContainedPosition(string Container, ContainmentRole Role) : Position;

/// <summary>Off the playing area.</summary>
public sealed record OffMapPosition : Position
{
    public static OffMapPosition Instance { get; } = new();
}

/// <summary>Not yet entered play.</summary>
public sealed record NotEnteredPosition : Position
{
    public static NotEnteredPosition Instance { get; } = new();
}

/// <summary>
/// How equipment is held (ASL-UNIT-022): possessed and carried (A4.43, p. 50), manned as a Gun in its own Location, or
/// towed. Each piece of equipment has at most one holder.
/// </summary>
public enum HoldingRole
{
    Possessed,
    Manned,
    Towed,
}

public sealed record Holding(string Holder, HoldingRole Role);

public enum InstanceStatus
{
    Active,

    /// <summary>Eliminated (ASL-UNIT-021); it cannot act.</summary>
    Eliminated,

    /// <summary>Consumed by reduction, deployment, recombination, or replacement; its successors carry on.</summary>
    Consumed,
}

/// <summary>What the state model knows about any object in play: units, equipment, and entities alike.</summary>
public interface IGameObject
{
    public string Id
    {
        get;
    }

    public string Kind
    {
        get;
    }

    public string? Side
    {
        get;
    }

    public Position Position
    {
        get;
    }

    public IReadOnlyDictionary<string, ConditionState> Conditions
    {
        get;
    }

    public InstanceStatus Status
    {
        get;
    }
}

/// <summary>
/// A unit instance (ASL-UNIT-021): stable id within its game, the definition it was created from, its owning side, its
/// conditions, and its position. <see cref="From"/> names the instances it was produced from, if any, and
/// <see cref="MfSpent"/> the MF its moves have spent in the current phase.
/// </summary>
public sealed record UnitInstance(
    string Id,
    string Kind,
    DefinitionReference? Definition,
    string Side,
    Position Position,
    IReadOnlyDictionary<string, ConditionState> Conditions,
    InstanceStatus Status,
    IReadOnlyList<string> From,
    string? Custodian = null,
    int MfSpent = 0) : IGameObject
{
    string? IGameObject.Side => Side;
}

/// <summary>A support weapon or Gun instance (ASL-UNIT-022): its own identity and condition, and at most one holder.</summary>
public sealed record EquipmentInstance(
    string Id,
    string Kind,
    string? Side,
    Position Position,
    Holding? Holding,
    IReadOnlyDictionary<string, ConditionState> Conditions,
    InstanceStatus Status) : IGameObject;

/// <summary>A Sniper, fortification, or marker (ASL-UNIT-026): its own entity, not a unit with a flag.</summary>
public sealed record EntityInstance(
    string Id,
    string Kind,
    string? Side,
    Position Position,
    IReadOnlyDictionary<string, ConditionState> Conditions,
    InstanceStatus Status) : IGameObject;

/// <summary>What a conclusion records about the state it read (ASL-UNIT-041).</summary>
public sealed record StateStamp(GameScope Scope, long Revision, string MapVersion);
