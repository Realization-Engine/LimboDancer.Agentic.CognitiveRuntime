namespace LimboDancer.Domains.Asl.Rules;

/// <summary>C9.3 loss facts for an observed assignment. Consumption by lineage alone is not elimination.</summary>
public static class TurnSpotterLossRules
{
    public static string? FireBar(bool recordedLoss) => recordedLoss
        ? "play.ordnance-spotter-lost: the recorded assignment was lost; a governed replacement is required (C9.3). Replacement execution remains pending."
        : null;

    public static bool RetainsReducedSquad(bool reducedSquad, bool activeHalfSquad, bool sameSide, bool goodOrder, bool recordedLoss) =>
        reducedSquad && activeHalfSquad && sameSide && goodOrder && !recordedLoss;

    public static string? DeploymentChoiceRefusal(bool assigned, string? choice) =>
        choice is not null && choice is not ("first" or "second")
            ? "play.deploy-spotter: choose the first or second half-squad to retain spotting (A1.31; C9.3)."
        : assigned && choice is null
            ? "play.deploy-spotter: choose which half-squad retains the Spotter assignment (A1.31; C9.3)."
        : !assigned && choice is not null
            ? "play.deploy-spotter: there is no retained Spotter assignment to pass to a half-squad."
        : null;

    /// <summary>
    /// A Deployment's named Spotter successor (A1.31; C9.3): the squad still holds an unlost assignment and is Good Order, and the successor
    /// is the half the owner chose before the NTC. A record made before the choice was recorded is checked by the squad's custody instead.
    /// </summary>
    public static bool DeploymentSuccessorValid(bool assignmentHeld, bool squadGoodOrder, string? recordedChoice, string successor,
        IReadOnlyList<string> producedIds, bool legacyCustodyHeld)
    {
        ArgumentNullException.ThrowIfNull(producedIds);
        return assignmentHeld && squadGoodOrder
            && (recordedChoice is not null ? producedIds[recordedChoice == "first" ? 0 : 1] == successor : legacyCustodyHeld);
    }

    public static bool RetainsDeployedSquad(bool deployedSquad, bool chosenProducedHalf, bool sameSide, bool goodOrder, bool recordedLoss) =>
        deployedSquad && chosenProducedHalf && sameSide && goodOrder && !recordedLoss;

    public static TurnSpotterChange ObservedLoss(bool previouslyActive, bool eliminated, bool broken, bool captured) =>
        !previouslyActive ? TurnSpotterChange.None
        : eliminated ? TurnSpotterChange.Eliminated
        : captured ? TurnSpotterChange.Captured
        : broken ? TurnSpotterChange.Broken : TurnSpotterChange.None;
}
