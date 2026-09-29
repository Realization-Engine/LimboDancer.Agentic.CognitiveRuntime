using LimboDancer.Domains.Asl.Maps.Composition;
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
public sealed record SideState(string Id, string Nationality, int? Elr, int? San)
{
    /// <summary>
    /// The side's Friendly Board Edge (A20.53): <c>top</c>, <c>bottom</c>, <c>left</c>, or <c>right</c> of the map in play, named at the
    /// start of the game, since no registered source gives a scenario's (ruling R5.16); null when none is named.
    /// </summary>
    public string? FriendlyEdge
    {
        get; init;
    }

    /// <summary>The map edges a side's Friendly Board Edge may be.</summary>
    public static IReadOnlyList<string> Edges { get; } = ["top", "bottom", "left", "right"];
}

/// <summary>Where a board sits in a composed map: its slot, as in <see cref="BoardPlacement"/>, and whether it is reversed.</summary>
public sealed record BoardSlot(int Column, int Row, bool Reversed);

/// <summary>
/// A board placed in the map in play, with the exact board version positions are checked against, and its slot when
/// the map is composed (Composed Maps Design, section 5).
/// </summary>
public sealed record PlacedBoard(BoardRef Board, string Version, BoardSlot? Slot = null);

/// <summary>
/// The board or composed map in play (ASL-UNIT-020, 024): its reference and version, and the boards placed in it.
/// Positions keep the placed board's reference, never map coordinates (ASL-MAP-024).
/// </summary>
public sealed record MapInPlay(string Reference, string Version, IReadOnlyList<PlacedBoard> Boards)
{
    public PlacedBoard? Board(BoardRef board) => Boards.FirstOrDefault(placed => placed.Board == board);

    /// <summary>Whether every board has a slot, so the map is laid out and reads cross its seams.</summary>
    public bool IsPlaced => Boards.Count > 0 && Boards.All(board => board.Slot is not null);

    /// <summary>The boards as map placements, or an empty list when the map is not placed.</summary>
    public IReadOnlyList<BoardPlacement> Placements() => IsPlaced
        ? [.. Boards.Select(board => new BoardPlacement(board.Board, board.Slot!.Column, board.Slot.Row, board.Slot.Reversed, []))]
        : [];
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
/// They are the vocabulary's states, which the display draws, plus two the display does not draw.
/// </summary>
public static class Conditions
{
    public const string Broken = "asl:broken";
    public const string Berserk = "asl:berserk";
    public const string Concealed = "asl:concealed";

    /// <summary>
    /// Hidden Initial Placement (A12.3, p. 80): the opponent is told nothing, not even presence. A vocabulary state since
    /// <c>asl@1.4.0</c>, so its owner sees it drawn.
    /// </summary>
    public const string Hidden = "asl:hidden";

    /// <summary>Pinned (A7.8, p. 58): inherent FP halved; removed in the CCPh (A3.8, p. 47).</summary>
    public const string Pinned = "asl:pinned";

    /// <summary>Wounded (A17, p. 85): a leader's Morale Level one lower and leadership modifier one worse.</summary>
    public const string Wounded = "asl:wounded";

    /// <summary>Disrupted (A19.12, p. 86).</summary>
    public const string Disrupted = "asl:disrupted";

    /// <summary>
    /// Prep Fire (A3.2, p. 47): the unit fired, or directed fire, in the PFPh; the marker is removed at the end of the AFPh
    /// (A3.5). A vocabulary state since <c>asl@1.5.0</c>.
    /// </summary>
    public const string PrepFire = "asl:prep-fire";

    /// <summary>Final Fire (A3.4, p. 47): the unit fired, or directed fire, in the DFPh; removed at the end of the DFPh.</summary>
    public const string FinalFire = "asl:final-fire";

    /// <summary>
    /// First Fire (A8.1, p. 59): the unit or weapon Defensive First Fired this MPh; removed at the end of the DFPh (A3.4). A
    /// vocabulary state since <c>asl@1.6.0</c>.
    /// </summary>
    public const string FirstFire = "asl:first-fire";

    /// <summary>Desperation Morale (A10.62, p. 68): +4 to a Rally attempt; removed at the end of every RPh.</summary>
    public const string DesperationMorale = "asl:dm";

    /// <summary>Fanatic (A10.8): both Morale Levels one higher, never Disrupted (asl@1.2.0).</summary>
    public const string Fanatic = "asl:fanatic";

    /// <summary>A heroic leader (A15.21): wounded rather than broken by a failed MC (asl@1.7.0).</summary>
    public const string Heroic = "asl:heroic";

    /// <summary>A malfunctioned SW (A9.7, p. 65): it cannot fire until repaired (A9.72).</summary>
    public const string Malfunctioned = "asl:malfunctioned";

    /// <summary>Captured (A20.2, p. 86), held by a captor (A20.5, p. 87).</summary>
    public const string Captured = "asl:captured";

    /// <summary>Held in Melee (A11.15, p. 72).</summary>
    public const string Melee = "asl:melee";

    /// <summary>A vehicle in Motion (D2.4, p. 198): it ended its MPh without stopping; removed when it next starts to move.</summary>
    public const string Motion = "asl:motion";

    /// <summary>An AFV crew Buttoned Up (D5.2, p. 203): not Vulnerable to Collateral Attacks; an OT AFV is CE unless BU (D5.3).</summary>
    public const string ButtonedUp = "asl:bu";

    /// <summary>An immobilized vehicle (A7.308, p. 56): it may not move (asl@1.8.0).</summary>
    public const string Immobilized = "asl:immobilized";

    /// <summary>A Stunned AFV crew (D5.34, p. 203): BU, and the vehicle may not fire or move for the rest of that Player Turn (asl@1.8.0).</summary>
    public const string Stunned = "asl:stunned";

    /// <summary>Stun +1 (D5.34): after its Stun, the vehicle adds one to its IFT, MC, and TC DR (asl@1.8.0).</summary>
    public const string StunRecovery = "asl:stun-recovery";

    /// <summary>A Recalled AFV (D5.341, pp. 203 to 204): Stunned, then leaving play at the end of that Player Turn (asl@1.8.0).</summary>
    public const string Recalled = "asl:recalled";

    /// <summary>
    /// Counter Exhaustion (A4.51, p. 51): placed by Double Time (A4.5), an advance into Difficult Terrain (A4.72), or a withdrawal from
    /// Melee that needs it (A11.21); removed when the unit breaks and at the start of its side's next MPh (ruling R5.3).
    /// </summary>
    public const string Cx = "asl:cx";

    /// <summary>An Abandoned vehicle (D5.41, p. 204): its crew has left it, and it may not move or fire (ruling R5.18).</summary>
    public const string Abandoned = "asl:abandoned";

    /// <summary>A wreck (D10.1): the vehicle counter on its wreck face (asl@1.1.0).</summary>
    public const string Wrecked = "asl:wrecked";

    /// <summary>Bounding Fire (D3.3, p. 199): the vehicle fired in its MPh; it may not fire in the AFPh, and the counter leaves at its end (asl@1.10.0).</summary>
    public const string BoundingFire = "asl:bounding-fire";

    /// <summary>Intensive Fire (C5.6, p. 173): the Gun fired once beyond its ROF and fires no more this Player Turn; it is kept beside its fire counter (asl@1.12.0).</summary>
    public const string IntensiveFire = "asl:intensive-fire";

    /// <summary>A Shocked AFV (C7.41, C7.42, p. 177): it may not move, fire, or change its CA until it recovers (asl@1.11.0).</summary>
    public const string Shocked = "asl:shocked";

    /// <summary>An Unconfirmed Kill (C7.42): still Shocked; a recovery dr of 4 to 6 at the end of the next RPh wrecks it (asl@1.11.0).</summary>
    public const string UnconfirmedKill = "asl:unconfirmed-kill";

    /// <summary>A vehicle MG disabled for good by a repair dr of 6 (D3.7, p. 201; asl@1.10.0).</summary>
    public const string Disabled = "asl:disabled";

    /// <summary>A bogged vehicle (D8.2, p. 208): Immobile until freed by a Bog Removal dr (D8.3; ruling R11.10).</summary>
    public const string Bogged = "asl:bogged";

    /// <summary>A Mired vehicle (D8.31): still bogged, with +1 to its Bog Removal colored dr (ruling R11.10).</summary>
    public const string Mired = "asl:mired";

    /// <summary>A malfunctioned BMG (D1.8; ruling R11.11): it adds no FP to an OVR.</summary>
    public const string BmgMalfunctioned = "asl:bmg-malfunctioned";

    /// <summary>A malfunctioned CMG (D1.8; ruling R11.11): it adds no FP to an OVR and makes no CC attack.</summary>
    public const string CmgMalfunctioned = "asl:cmg-malfunctioned";

    /// <summary>
    /// The CC counter of a unit that made a CC Reaction Fire attack on a vehicle that survived (D7.21; ruling R11.13): no Non-CC Reaction Fire
    /// at it; removed at the end of the phase.
    /// </summary>
    public const string CcReaction = "asl:cc-reaction";

    /// <summary>A SW in its dismantled state (A9.8; backlog pass 13, ruling R13.6): not fired, and portaged at half its PP.</summary>
    public const string Dismantled = "asl:dismantled";

    /// <summary>
    /// Unarmed (A20.5; backlog pass 14, ruling R14.5): a captured unit, and one freed from capture, with a CC FP of one; it never fires, uses a SW, or
    /// enters a Known enemy unit's Location, and is never broken.
    /// </summary>
    public const string Unarmed = "asl:unarmed";

    /// <summary>Conditions the state model adds to the vocabulary's states; they have no drawn form.</summary>
    public static IReadOnlyList<string> Undrawn { get; } = [Captured, Melee, Abandoned, Bogged, Mired, BmgMalfunctioned, CmgMalfunctioned, CcReaction, Dismantled, Unarmed];

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

    /// <summary>Exited the playing area (A2.6, D5.341): it cannot act or return, and it is not eliminated.</summary>
    Exited,

    /// <summary>
    /// A destroyed vehicle's wreck (D10.1; ruling R6.5): it stays at its Location on its wreck face, keeps its VCA, and no longer acts as a
    /// unit; it gives cover and Hindrance (D9.3, D9.4) and raises vehicle entry costs (D2.14).
    /// </summary>
    Wrecked,
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
/// <see cref="MfSpent"/> the MF its moves have spent in the current phase. <see cref="MovementEnded"/> is set when the
/// unit may not move again in the phase, as after being forced back (A12.15, p. 78), and is cleared at every phase change.
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
    int MfSpent = 0,
    bool MovementEnded = false) : IGameObject
{
    string? IGameObject.Side => Side;

    /// <summary>Half an MF spent beyond <see cref="MfSpent"/>, as grain's 1½ MF leaves (B15.4, p. 129).</summary>
    public bool HalfMfSpent
    {
        get; init;
    }

    /// <summary>
    /// The MF Double Time adds to the unit's allotment this MPh (A4.5, ruling R5.1): two when declared before it spent any MF, one after;
    /// zero when it does not Double Time. Cleared at every phase change.
    /// </summary>
    public int DoubleTimeMf
    {
        get; init;
    }

    /// <summary>
    /// Whether the unit crossed a hexside other than at the road rate this MPh, or into SMOKE, a burning wreck, or rubble (B3.4; ruling R10.8), which
    /// denies it the Road Bonus. Cleared at every phase change.
    /// </summary>
    public bool OffRoad
    {
        get; init;
    }

    /// <summary>
    /// The leaders the unit has moved with at every step of this MPh, for the leader bonus (A4.12; ruling R10.8): null until its first step. Cleared
    /// at every phase change.
    /// </summary>
    public IReadOnlyList<string>? MovedWith
    {
        get; init;
    }

    /// <summary>
    /// A vehicle in Bypass (D2.3, D2.34; ruling R11.2): the ground-level Location across the hexside it straddles, beside the obstacle hex it
    /// occupies; its CAFP is the vertex of that hexside its VCA faces. Null when it is at its hex center. Kept across phases (Stationary Bypass).
    /// </summary>
    public Maps.Coordinates.BoardLocation? Straddling
    {
        get; init;
    }

    /// <summary>The MP a successful ESB DR added to the vehicle's allotment this MPh (D2.5; ruling R11.3). Cleared at every phase change.</summary>
    public int EsbMp
    {
        get; init;
    }
}

/// <summary>Unit kinds the state model treats specially.</summary>
public static class UnitKinds
{
    /// <summary>A Dummy (A12.11, p. 76): a concealment counter with no unit beneath, and no catalog definition.</summary>
    public const string Dummy = "asl:dummy";
}

/// <summary>
/// An entry attempt not yet resolved (Random Selection and Declined OVR Design, section 4): its first event, its unit,
/// target, and MF, and the units a Random Selection requires it to reveal. While it is open the phase may not change
/// and its unit may not move.
/// </summary>
public sealed record OpenAttempt(string EventId, string Unit, BoardLocation Target, int Mf)
{
    /// <summary>The units a Random Selection roll selected for the reveal; empty when no roll was made.</summary>
    public IReadOnlyList<string> Revealing { get; init; } = [];

    /// <summary>The attacker's OVR choice once declared, or null while none is made.</summary>
    public string? Declaration
    {
        get; init;
    }

    /// <summary>Whether the OVR NTC after an election passed; null until it is taken.</summary>
    public bool? TaskCheckPassed
    {
        get; init;
    }

    /// <summary>Whether a Random Selection followed the passed OVR NTC, revealing the second defender.</summary>
    public bool SecondSelection
    {
        get; init;
    }
}

/// <summary>A support weapon or Gun instance (ASL-UNIT-022): its own identity and condition, and at most one holder.</summary>
public sealed record EquipmentInstance(
    string Id,
    string Kind,
    string? Side,
    Position Position,
    Holding? Holding,
    IReadOnlyDictionary<string, ConditionState> Conditions,
    InstanceStatus Status) : IGameObject
{
    /// <summary>The catalog definition of a SW or Gun that has one (catalog 1.2.0 on); null for equipment created by kind alone.</summary>
    public DefinitionReference? Definition
    {
        get; init;
    }
}

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
