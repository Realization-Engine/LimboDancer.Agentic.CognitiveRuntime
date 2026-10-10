namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// One Infantry step's cost in half MF and what it enters (pass 32.b, from Play's planner): the terrain key, whether it crossed a road hexside at
/// the road rate into a hex with no SMOKE, burning wreck, or rubble (the Road Bonus, B3.4), whether it costs the unit's whole MF allotment (marsh,
/// B16.4), and whether only a Minimum Move may make it (marsh from a lower elevation, B16.4).
/// </summary>
public sealed record InfantryEntry(int HalfMf, string Terrain, bool RoadRate, bool AllMf, bool MinimumMoveOnly, bool LevelChange);

/// <summary>
/// The hexside an Infantry step crosses, as the map reads it: a cliff or Continuous Slope, the hexside terrain's name (a wall, a hedge, or another),
/// whether the hexside carries a road, and the name of the terrain on it (the building two hexes of one building share).
/// </summary>
public sealed record CrossedHexsideFacts(bool Cliff, bool Slope, string? HexsideTerrain, bool Road, string? Terrain);

/// <summary>One Location of an Infantry step as the map reads it: the terrain key the packages admit (null when none), the terrain's own name, the hex's base level, and its stairwell.</summary>
public sealed record StepLocationFacts(string? TerrainKey, string? TerrainName, int BaseLevel, bool Stairway);

/// <summary>
/// The facts of one Infantry step from one Location to another (rulings R10.1 to R10.3, R10.5). <paramref name="From"/> and <paramref name="To"/> are
/// null where the map cannot read the Location; <paramref name="Crossed"/> is the hexside between the two hexes, null when they are not adjacent or
/// the map cannot give it. The texts are the Locations as the planner names them in a refusal.
/// </summary>
public sealed record InfantryStepFacts(
    bool FromIsHexside,
    bool ToIsHexside,
    bool SameHex,
    int FromLevel,
    int ToLevel,
    string FromText,
    string ToText,
    string FromHexText,
    StepLocationFacts? From,
    StepLocationFacts? To,
    bool Adjacent,
    CrossedHexsideFacts? Crossed,
    bool Night,
    int? Month);

/// <summary>One hexside a Bypass runs along, as the map reads the target hex's hexside: its direction as an int and its lower-case name, whether it was read, and the names of its terrain and hexside terrain and whether it carries a road.</summary>
public sealed record BypassedHexsideFacts(int Side, string SideText, bool Read, string? Terrain, string? HexsideTerrain, bool Road);

/// <summary>
/// The facts of a Bypass step into a woods or building hex (A4.3, A4.31; ruling R10.7) across the target's hexside <paramref name="Crossed"/>:
/// the hexsides named, the target Location and hex as the map reads them, who is in the hex, the base level of the hex left (or the mirror hex
/// beyond the map edge), and the wall or hedge on the hexside crossed.
/// </summary>
public sealed record BypassStepFacts(
    IReadOnlyList<BypassedHexsideFacts> Bypass,
    int Crossed,
    int ToLevel,
    string ToText,
    bool TargetReadable,
    string? TargetTerrainKey,
    int TargetBaseLevel,
    IReadOnlyList<string?> TargetHexsideTerrains,
    bool FriendlyUnitsAtTarget,
    bool ArmedKnownEnemyAtTarget,
    int FromBaseLevel,
    string? EntryWall,
    int? Month);

/// <summary>
/// The facts of a moving stack's step (rulings R10.1 to R10.3, R10.7): the moving stack's Bypass as the state has it, the movers against it, the
/// Locations named, and the reads of the Location left. <paramref name="SideTowardFrom"/> is the target hex's hexside toward the origin, null when
/// they are not adjacent; <paramref name="EntryWall"/> the wall or hedge on the origin's hexside toward the target.
/// </summary>
public sealed record MoveEntryFacts(
    bool StackInBypass,
    IReadOnlyList<int> Lane,
    bool CurrentAtFrom,
    bool MoversAllInCurrent,
    bool CurrentMemberLeftBehind,
    bool ToIsFrom,
    bool EnemyAtFrom,
    bool BypassGiven,
    string? ObstacleTerrainKey,
    int FromLevel,
    bool FromReadable,
    bool ToIsGround,
    int? SideTowardFrom,
    string? EntryWall,
    string ToText);

/// <summary>
/// The verdict on a moving stack's step: the first refusal; the cost of occupying the Bypassed obstacle; or which step the caller reads next, the
/// ordinary Infantry step from the origin to the target or a Bypass step into the target.
/// </summary>
public sealed record MoveEntryVerdict(string? Refusal, InfantryEntry? Occupy, bool OrdinaryStep, bool BypassStep);

/// <summary>
/// One hexside an LOS crosses into the target hex, as the map reads it for one firer: the wall or hedge on it (<c>wall</c>, <c>hedge</c>, null, or
/// <c>other</c>), whether it carries a road, the base level of the hex beyond it (null when the map cannot read that hex), and whether the hex beyond
/// it is the firer's own hex.
/// </summary>
public sealed record LosEntrySideFacts(int Side, string? Wall, bool Road, int? BeyondBaseLevel, bool BeyondIsFirer);

/// <summary>
/// One firer's Location against a target's walls and hedges (B9.3, B9.31, B9.33, B9.35): whether it is the target Location itself, its level, its range,
/// the hexsides its LOS crosses into the target hex (null when the bearing cannot be read; read only for a base-level target), the wall or hedge on the
/// hexspine leading away from a vertex (null when there is none or the LOS crosses one hexside), its hex's base level (null when unread), and a read
/// of which side holds Wall Advantage over the hexside it shares with the target, made only when the rule asks.
/// </summary>
public sealed record WallTemFirerFacts(
    bool IsTarget,
    int Level,
    int Range,
    IReadOnlyList<LosEntrySideFacts>? EntrySides,
    string? SpineWall,
    int? BaseLevel,
    Func<(string? Side, string? Reason)> WallAdvantage);

/// <summary>
/// The facts of a target's wall or hedge TEM against a group (B9.3, B9.31, B9.33, B9.35; rulings R10.5, R10.6): the target Location's level, its hex's base
/// level and the hexside terrains of its hexsides (empty when the map cannot read it), each firer, the firing side, and whether the moving stack is in the
/// target Location in the MPh (B9.3, the road gap).
/// </summary>
public sealed record WallTemFacts(
    bool TargetReadable,
    int TargetLevel,
    int TargetBaseLevel,
    IReadOnlyList<string?> TargetHexsideTerrains,
    IReadOnlyList<WallTemFirerFacts> Firers,
    string FiringSide,
    bool MovingStackAtTarget);

/// <summary>One unit in a Location that might claim Wall Advantage: its side and the conditions the claim reads (B9.32, B9.321, B9.323).</summary>
public sealed record WallAdvantageUnitFacts(string Id, string? Side, bool Active, bool Dummy, bool Vehicle, bool Captured, bool Broken);

/// <summary>One of the two ADJACENT Locations that share a hexside: its level, its terrain key (null when unread or not admitted), and the units in it.</summary>
public sealed record WallAdvantageLocationFacts(int Level, string? TerrainKey, IReadOnlyList<WallAdvantageUnitFacts> Units);

/// <summary>
/// The facts of which side holds Wall Advantage (B9.32, B9.321, B9.41; ruling R10.6): the two Locations, whether the game's history is at hand, the
/// revision at which a unit last entered its Location (read only when both Locations hold claimants), and the Scenario Defender when named.
/// </summary>
public sealed record WallAdvantageFacts(WallAdvantageLocationFacts One, WallAdvantageLocationFacts Two, bool HistoryKnown, Func<string, long> ArrivalOf, string? ScenarioDefender);

/// <summary>
/// The facts of Height Advantage (B10.31; ruling R10.4): the target hex's base level and Location level, each firer's level, whether the moving stack is in
/// the target Location in the MPh (or a routing unit is entering it, B1.14) with the base level of the hex it left and the target's hexside toward it (null when unread or not adjacent), and the
/// hexsides each firer's LOS crosses into the target hex, read by index only when the rule asks.
/// </summary>
public sealed record HeightAdvantageFacts(
    int TargetBaseLevel,
    int TargetLevel,
    IReadOnlyList<int> FirerLevels,
    bool MovementPhase,
    bool MovingStackAtTarget,
    int? LeftBaseLevel,
    int? ClimbedSide,
    Func<int, IReadOnlyList<int>?> EntrySidesOf,
    bool SnapShot);

/// <summary>A unit whose MF allotment is read (A4.11, A4.42): a Dummy, the class its catalog definition prints (null when none), a SMC, wounded, berserk.</summary>
public sealed record MfAllotmentFacts(bool Dummy, string? Class, bool Smc, bool Wounded, bool Berserk);

/// <summary>
/// One unit of a moving stack as the leader bonus reads it (A4.12, A4.42; ruling R10.8): its kinds, its conditions, the nationality its catalog
/// definition names (null when none), the MF it has spent, and the leaders it has moved with (null before its first step).
/// </summary>
public sealed record MovingUnitFacts(
    string Id,
    string Side,
    bool Dummy,
    bool Mmc,
    bool Leader,
    bool Berserk,
    bool Broken,
    bool Wounded,
    string? Nationality,
    int MfSpent,
    bool HalfMfSpent,
    IReadOnlyList<string>? MovedWith);

/// <summary>
/// One unit of the game as the scans for a seeing enemy read it (A12.14, A12.34): active, its side, a Dummy, aboard a vehicle, Broken as a
/// three-valued fact, Hidden, Good Order as the planner reads it, and its Location as an index into the caller's table of Locations (null off
/// the map).
/// </summary>
public sealed record EnemyUnitFacts(bool Active, string? Side, bool Dummy, bool Aboard, bool? Broken, bool Hidden, bool GoodOrder, int? Location);

/// <summary>The LOS between two Locations as the map gives it: whether it is clear, and its range.</summary>
public sealed record LosFacts(bool Clear, int Range);

/// <summary>A fact reader for a scan of who sees a Location (the pass 32 design, D4): the LOS between two Locations of the caller's table, or null when the map cannot give it.</summary>
public interface ILosFactReader
{
    public LosFacts? Los(int fromLocation, int toLocation);
}

/// <summary>A firer or director of an attack as the "seen" read finds it in the state: its side and its Location's index (null when it has none).</summary>
public sealed record SeenSubjectFacts(string Side, int? Location);

/// <summary>One unit a move names, as the state finds it: whether it is active, and its side (null when not found).</summary>
public sealed record MoveStartUnitFacts(bool Active, string? Side);

/// <summary>
/// One mover against the bars of a move (A3.3, A4.1, A4.8, A7.25, A7.83, A11.15, A12.32, A20.4, A20.53, D2.1): its conditions as the state has them,
/// Broken as a three-valued fact, and whether it massacred a prisoner in the PFPh.
/// </summary>
public sealed record MoveBarUnitFacts(
    string Id,
    bool NoMoveThisPlayerTurn,
    bool Ti,
    bool BoundingFire,
    bool Vehicle,
    bool Berserk,
    bool PrepFire,
    bool MassacredInPrepFire,
    bool Melee,
    bool Captured,
    bool Dummy,
    bool? Broken,
    bool Pinned,
    bool MovementEnded,
    bool Hidden);

/// <summary>
/// The facts of the bars of a move (the planner's PlanMove, blocks 3 and 4): the Gun push named and what the state says of the pusher and the Gun,
/// the movers, the moving stack as the state has it, the enemy vehicle in the target Location (its id, null for none), and the move's flags.
/// </summary>
public sealed record MoveBarsFacts(
    bool PushGiven,
    bool SinglePusher,
    bool GunMannedByPusher,
    bool PusherCrewOrHalfSquad,
    bool PusherPinned,
    bool PusherBroken,
    bool GunManhandlingKnown,
    bool Assault,
    bool PushingOn,
    IReadOnlyList<MoveBarUnitFacts> Movers,
    bool CurrentExists,
    IReadOnlyList<string> CurrentMembers,
    IReadOnlyList<string> CurrentMovers,
    bool WindowOpen,
    string? EnemyVehicleId,
    bool DoubleTime,
    string ToText);

/// <summary>One step a berserk charge's shortest route may take (ruling R27.2): the Location it charges, its cost, whether it is taken in the open, and its Bypass lanes.</summary>
public sealed record ChargeStepFacts(string Target, int HalfMf, bool Plain, IReadOnlyList<string> Lanes);

/// <summary>A berserk charge's route as the planner's search gives it: the steps by the Location text they enter, and why the route is undecided (null when it is decided).</summary>
public sealed record ChargeRouteFacts(IReadOnlyDictionary<string, ChargeStepFacts> Steps, string? Undecided);

/// <summary>One mover of a berserk stack: its id and side, and whether it is berserk and wounded.</summary>
public sealed record BerserkMoverFacts(string Id, string Side, bool Berserk, bool Wounded);

/// <summary>One unit in the Location a berserk stack charges from (A15.43): whether it is active, its side, berserk, done moving, wounded.</summary>
public sealed record BerserkNeighbourFacts(string Id, bool Active, string? Side, bool Berserk, bool MovementEnded, bool Wounded);

/// <summary>One SW a berserk unit holds, with the portage its catalog definition prints (null when none).</summary>
public sealed record BerserkCarriedFacts(string ItemId, int? Portage);

/// <summary>One berserk mover as the abandonment of its SW reads it (A15.431): whether it has moved this MPh, a SMC, and what it holds in the state's order.</summary>
public sealed record BerserkAbandonFacts(string Id, bool FirstStep, bool Smc, IReadOnlyList<BerserkCarriedFacts> Carried);

/// <summary>One mover against Assault Movement and Double Time (A4.5, A4.42, A4.61, E1.51): its conditions, whether its CX counter left at this MPh's start, a SMC, and the PP it carries (null when one is not recorded).</summary>
public sealed record MoveTimingUnitFacts(string Id, bool Wounded, bool Berserk, bool Cx, bool NoDoubleTime, bool Smc, int? PortageSum);

/// <summary>The verdict on a DC Placement or SMOKE attempt named with a move: the refusal, or which action the planner reads next.</summary>
public sealed record MoveActionVerdict(string? Refusal, bool PlaceDc, bool Smoke);

/// <summary>One mover against the entry's cost: whether it has spent MF this MPh, its Double Time MF, and CX.</summary>
public sealed record EntryMoverFacts(bool Spent, int DoubleTimeMf, bool Cx);

/// <summary>
/// The facts of an entry's cost (A4.134, A7.7, B16.4, C10.3; rulings R8.6, R10.9, R12.11): the step as the terrain read gives it, the movers, and the
/// move's flags. A mover's MF allotment is read by index when the rule asks.
/// </summary>
public sealed record EntryCostFacts(
    InfantryEntry Entry,
    bool AnyEncircled,
    bool Pushed,
    bool Bypassing,
    bool Occupy,
    bool MinimumMove,
    bool Assault,
    int Berserk,
    bool CurrentExists,
    bool DoubleTime,
    IReadOnlyList<EntryMoverFacts> Movers);

/// <summary>One unit in the target Location as the entry reads it (A4.14, A12.15): active, its side, captured, Known, a Dummy.</summary>
public sealed record UnitAtTargetFacts(string Id, bool Active, string? Side, bool Captured, bool Known, bool Dummy);

/// <summary>The verdict on the enemy units in the target Location: the refusal; a stack of Dummies alone removed; the enemies there and the hidden enemies a charge enters, in the state's order.</summary>
public sealed record MoveEnemiesVerdict(string? Refusal, bool RemoveDummies, string? Summary, IReadOnlyList<string> EnemiesThere, IReadOnlyList<string> HiddenEnemies);

/// <summary>One mover against the MF it has left (A4.11, A4.42, A4.5, A4.12, B3.4): whether this is its first step, its Double Time MF, CX, off the road, and its leader bonus.</summary>
public sealed record MfLeftMoverFacts(string Id, bool FirstStep, int DoubleTimeMf, bool Cx, bool OffRoad, bool LeaderBonus, int HalfMfSpent);

/// <summary>A mover's MF allotment as the caller reads it (A4.11, A4.42; rulings R5.1, R5.4), by the mover's index, with the Double Time MF, CX, and the bonuses the rule names.</summary>
public delegate int? MfAllotmentRead(int mover, int doubleTimeMf, bool cx, int bonusMf, int ipcBonus);

/// <summary>The moving stack as the DEFENDER's pass reads it (A8.11): the window, its members, a vehicle, ending, an OVR pending, its movers and those still active, and its Location.</summary>
public sealed record PassFireFacts(bool WindowOpen, int Members, bool Vehicle, bool Ending, bool OverrunPending, IReadOnlyList<string> Movers, IReadOnlyList<string> Living, string LocationText);

/// <summary>The moving stack as the ATTACKER's end of the move reads it (A4.2, A8.11): the window, its Location, members and movers, a vehicle, in Bypass, and the units named.</summary>
public sealed record EndMoveFacts(bool WindowOpen, string LocationText, IReadOnlyList<string> Members, IReadOnlyList<string> Movers, bool Vehicle, bool InBypass, IReadOnlyList<string> Named);

/// <summary>A berserk unit that would end its move (A15.43, A15.431): the cost of each step its route offers, why the route is undecided, and the half MF it has left.</summary>
public sealed record EndMoveBerserkFacts(IReadOnlyList<int> StepHalfMfs, string? Undecided, int Left);

/// <summary>The verdict on the end of a move: the refusal; a vehicle's own plan; or the units ending, those that may move on, and the summary.</summary>
public sealed record EndMoveVerdict(string? Refusal, bool Vehicle, IReadOnlyList<string> Ending, IReadOnlyList<string> Remaining, string Summary);

/// <summary>One enemy unit in the Location a stack enters or attempts, as the reveal reads it (A12.15, A.9): a Dummy, hidden.</summary>
public sealed record MoveRevealUnitFacts(string Id, bool Dummy, bool Hidden);

/// <summary>
/// The facts of a step once its cost and the enemies there are known (the planner's PlanMove, blocks 11 to 16): the stack and the Locations as
/// the summary names them, the charge, the step's terrain and Bypass hexsides (lower-case names), its cost and flags, the SW abandoned before a charge,
/// the road rate, a pushed Gun, night, a stack of Dummies alone, and the enemies there and the hidden enemies a charge enters, in the state's order.
/// </summary>
public sealed record MoveStepFacts(
    IReadOnlyList<string> Ids,
    string FromText,
    string ToText,
    bool Entering,
    string? ChargeText,
    bool Occupy,
    string Terrain,
    IReadOnlyList<string>? BypassTexts,
    int HalfMf,
    bool Assault,
    bool DoubleTime,
    bool MinimumMove,
    IReadOnlyList<string> AbandonedIds,
    bool RoadRate,
    bool Pushed,
    bool Night,
    bool AllDummies,
    IReadOnlyList<MoveRevealUnitFacts> EnemiesThere,
    IReadOnlyList<MoveRevealUnitFacts> HiddenEnemies);

/// <summary>
/// The verdict on a step's reveal and record (A12.15, A.9, A12.11, A2.51): the units revealed, the real ones among them by id, whether the stack is
/// forced back (and off board), the Road Bonus the record carries, the summary and the Dummy warning, the Random Selection's pool and whether it is
/// needed, whether the reveal precedes or follows the step, and the planned roll's purpose.
/// </summary>
public sealed record MoveStepVerdict(
    IReadOnlyList<MoveRevealUnitFacts> Revealing,
    IReadOnlyList<string> RealOnes,
    bool ForcedBack,
    bool OffMap,
    bool RoadBonus,
    string Summary,
    string PushSummary,
    IReadOnlyList<string> DummyWarning,
    IReadOnlyList<string> Pool,
    bool NeedsSelection,
    bool RevealBeforeStep,
    bool RevealAfterStep,
    string RollPurpose);

/// <summary>One mover as the loss of concealment on a step reads it (A12.14, E1.31): a Dummy, concealed.</summary>
public sealed record MoverConcealmentFacts(string Id, bool Dummy, bool Concealed);

/// <summary>A Fire Lane with Residual FP in the Location a stack enters (A9.22): its weapon and the FP.</summary>
public sealed record FireLaneAttackFacts(string Weapon, int Fp);

/// <summary>The refusal of a recorded event by the projector: the diagnostic's code and its text.</summary>
public sealed record RecordRefusal(string Code, string Text);

/// <summary>One mover of a recorded movement step as the projector finds it (A3.3, A4.2, A4.5, A11.15, A20.53): found and active, its side, its Location's text (null off the map), its conditions, Personnel, and whether its CX counter left at this MPh's start.</summary>
public sealed record StepMoverFacts(
    string Id,
    bool Active,
    string? Side,
    string? LocationText,
    bool MovementEnded,
    bool PrepFire,
    bool Melee,
    bool Captured,
    bool Personnel,
    bool NoDoubleTime,
    bool Broken,
    bool Wounded,
    bool Berserk,
    bool Cx,
    bool OffMapWaiting,
    int MfSpent,
    bool HalfMfSpent);

/// <summary>
/// A recorded movement step against the state (the projector's StepMovement, blocks 1 to 4 and 6 to 8): the phase and phasing side, the movers,
/// the record's fields, and the moving stack as the state has it.
/// </summary>
public sealed record StepRecordFacts(
    string? Phase,
    string? PhasingSide,
    IReadOnlyList<StepMoverFacts> Movers,
    int HalfMf,
    bool Assault,
    int Step,
    bool DoubleTime,
    bool MinimumMove,
    string? AttemptedText,
    string ToText,
    int? BypassCount,
    bool Exit,
    bool CurrentExists,
    IReadOnlyList<string> CurrentMembers,
    bool CurrentWindowOpen,
    bool CurrentAssault,
    int CurrentStep);

/// <summary>The verdict on a recorded movement step: the refusal, or whether the stack forced back from off board stays there with its move over.</summary>
public sealed record StepRecordVerdict(RecordRefusal? Refusal, bool OffBoardForcedBack);

/// <summary>One mover's bookkeeping after a step (A4.5, B3.4, A4.12; ruling R10.8): its MF spent in halves, CX when Double Timing, its Double Time MF, off the road, and the leaders it has moved with.</summary>
public sealed record StepMoverUpdate(int MfSpent, bool HalfMfSpent, bool SetCx, int DoubleTimeMf, bool OffRoad, IReadOnlyList<string> MovedWith);

/// <summary>A SW possessed by a mover as the AFPh bar reads it (A4.41): its id, a light mortar, and whether it is already marked moved.</summary>
public sealed record CarriedWeaponFacts(string Id, bool LightMortar, bool AlreadyMoved);

/// <summary>The moving stack as the DEFENDER's window closes (A4.134, A24.1, D2.15, D7.1; rulings R5.15, R9.5, R10.9, R11.11): the record's step against the state's, its ending members, and its state.</summary>
public sealed record CloseWindowFacts(bool WindowOpen, int Step, int ClosedStep, IReadOnlyList<string> EndingMembers, bool OverrunPending, bool Ending, IReadOnlyList<string> Members, IReadOnlyList<string> Movers, bool MinimumMove, bool Vehicle);

/// <summary>The verdict on the window's closing: the refusal; the units whose move ends (none, every member or mover, or the named ones); and whether the Minimum Move's unbroken movers are pinned and CX.</summary>
public sealed record CloseWindowVerdict(RecordRefusal? Refusal, IReadOnlyList<string>? EndMove, bool PinMinimumMovers);

/// <summary>One member of the moving stack as the stack keeps or drops it (A4.2, A7.8, D5.34, D8.2; ruling R11.9): active and its conditions.</summary>
public sealed record StackMemberFacts(string Id, bool Active, bool Stunned, bool Shocked, bool UnconfirmedKill, bool Immobilized, bool Abandoned, bool Bogged, bool Recalled, bool StunRecovery, bool Broken, bool Pinned);

/// <summary>The verdict on the moving stack after an event: unchanged, or its members and movers and the members that leave it and end their MPh.</summary>
public sealed record KeepMovingStackVerdict(bool Unchanged, IReadOnlyList<string> Members, IReadOnlyList<string> Movers, IReadOnlyList<string> Leaving);

/// <summary>The night and weather half MF of an Infantry step and whether the road rate survives them (E1.51, E3.54, E3.6, E3.64, E3.723, E3.733), read by the caller for a terrain, a road crossing, and a rise.</summary>
public delegate (int HalfMf, bool RoadRate) InfantryWeatherRead(string terrain, bool road, int rise);
