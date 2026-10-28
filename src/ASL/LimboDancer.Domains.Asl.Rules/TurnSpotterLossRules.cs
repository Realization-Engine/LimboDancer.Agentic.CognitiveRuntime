namespace LimboDancer.Domains.Asl.Rules;

/// <summary>C9.3 loss facts for an observed assignment. Consumption by lineage alone is not elimination.</summary>
public static class TurnSpotterLossRules
{
    public static string? FireBar(bool recordedLoss) => recordedLoss
        ? "play.ordnance-spotter-lost: the recorded assignment was lost; a governed replacement is required (C9.3). Replacement execution remains pending."
        : null;

    public static bool RetainsReducedSquad(bool reducedSquad, bool activeHalfSquad, bool sameSide, bool goodOrder, bool recordedLoss) =>
        reducedSquad && activeHalfSquad && sameSide && goodOrder && !recordedLoss;

    public static TurnSpotterChange ObservedLoss(bool previouslyActive, bool eliminated, bool broken, bool captured) =>
        !previouslyActive ? TurnSpotterChange.None
        : eliminated ? TurnSpotterChange.Eliminated
        : captured ? TurnSpotterChange.Captured
        : broken ? TurnSpotterChange.Broken : TurnSpotterChange.None;
}
