namespace LimboDancer.Domains.Asl.Rules;

public enum BypassDisposition
{
    Evaluated, Refused, NotApplicable, NotEvaluated, ForcedEnd
}
public enum BypassTurretPosition
{
    Front, Right, Rear, Left
}
public enum BypassTurnDirection
{
    Clockwise, Counterclockwise
}
public enum BypassFireMode
{
    Ordinary, BoundingFirstFire
}
public enum BypassWeaponKind
{
    Ordnance, Cmg, Ife, Canister, Bow, Unsupported
}
public enum BypassTraverse
{
    T, St, Rst, OneMan, Nt, Unsupported
}

/// <summary>Axial coordinates: q points NE (30 degrees), r north (90 degrees). Never screen coordinates.</summary>
public readonly record struct BypassHex(int Q, int R);
public sealed record BypassFrameFacts(BypassHex? Obstacle, BypassHex? Across, BypassHex? Front);
public sealed record BypassFrame(BypassHex Obstacle, BypassHex Across, BypassHex Front, BypassHex Rear, int Facing);
public sealed record BypassFrameVerdict(BypassDisposition Disposition, BypassFrame? Frame, string Reason);
public sealed record BypassSectorVerdict(BypassDisposition Disposition, BypassTurretPosition? Sector, string Reason);
public sealed record BypassTurretChangeVerdict(BypassDisposition Disposition, int? Steps, bool? EntersSide, string Reason);
public sealed record BypassTurretFacts(bool? InBypass, BypassFrameFacts? Frame, BypassHex? Target,
    BypassTurretPosition? Current, BypassTurretPosition? Proposed, BypassTurnDirection? Direction,
    BypassTraverse? Traverse, BypassWeaponKind? Weapon, BypassFireMode? Mode, bool? SpecialTerrain,
    bool? ApplicableAcquisition, bool? SameKnownTarget, bool? SameTargetFacing, bool? EarlierMpTcaChange);
/// <summary>Local components only. SideChangeToHitDrm is not an IFT modifier; no D3.51 composition is implied.</summary>
public sealed record BypassTurretVerdict(BypassDisposition Disposition, BypassTurretPosition? Sector,
    int? Steps, int? CaseA, int? SideChangeToHitDrm, string Reason, string Rule = "D2.321", int Page = 197);
