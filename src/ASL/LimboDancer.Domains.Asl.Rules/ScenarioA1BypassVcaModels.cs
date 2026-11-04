namespace LimboDancer.Domains.Asl.Rules;

public enum BypassTurnPurpose
{
    ContinueMovement, FireOnly
}
public enum BypassPositionKind
{
    Bypass, Ordinary
}
public enum BypassMovementStage
{
    AtOccupiedPosition, TurnPending, EntryCompleted
}
public enum BypassEndCause
{
    Voluntary, Immobilized, OtherForcedHalt
}
public enum BypassOriginKind
{
    HexCenter, Cafp
}
public sealed record BypassEdge(BypassHex? One, BypassHex? Two);
public sealed record BypassPositionFacts(BypassPositionKind? Kind, BypassFrameFacts? Frame, BypassHex? Hex, int? Facing);
public sealed record BypassVcaTurnFacts(bool? InBypass, BypassFrameFacts? Frame, bool? PendingTurn, BypassEdge? Edge, BypassTurnPurpose? Purpose);
public sealed record BypassContinuationFacts(bool? InBypass, BypassFrameFacts? Frame, bool? PendingTurn, BypassEdge? Edge,
    bool? Reverse, BypassPositionFacts? Destination);
public sealed record BypassEndFacts(bool? InBypass, bool? PendingTurn, bool? EntryCompleted, BypassEndCause? Cause, bool? Immobilized);
public sealed record BypassDefensiveFacts(bool? InBypass, BypassFrameFacts? OldFrame, BypassMovementStage? Stage,
    BypassPositionFacts? Completed, bool? Immobilized, BypassOriginKind? OriginKind, BypassHex? Origin);
/// <summary>D2.33 component verdict, never complete movement permission. Position is independent of proposed VCA.</summary>
public sealed record BypassVcaVerdict(BypassDisposition Disposition, int? HalfMp, int? ProposedFacing,
    bool? ContinuationRequired, BypassPositionFacts? Position, string? TargetFacing, string Reason,
    string Rule = "D2.33", int Page = 198);
