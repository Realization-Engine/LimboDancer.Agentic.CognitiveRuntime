namespace LimboDancer.Domains.Asl.Rules;

public enum TurnWorkKind
{
    AnimalPacking, AmmoReplenishment, AmmoTransfer
}
public enum TurnWorkWindow
{
    Unknown, FirstRally, SecondCloseCombat
}
public enum TurnWorkState
{
    NotEvaluated, InProgress, Interrupted, CompletionPending
}
public sealed record TurnWorkVerdict(int? RequiredGameTurns, TurnWorkState State);
public enum TurnWorkIntervalOutcome
{
    Interrupted, Complete, Incomplete
}

/// <summary>
/// What happened to a work participant at one event, as recorded facts; <see cref="TurnWorkRules.Interrupted"/> decides.
/// <see cref="PackingCrewLostPackTi"/> is <see cref="TurnPackingRules.CrewRetainsPackTi"/>'s verdict on an Animal-Packing crew (G10.11).
/// </summary>
public sealed record TurnWorkActivity(bool AuditedEligibilityLost, bool ParticipantGoneOrMoved, bool TiCleared, bool SubjectButtonedUp,
    bool Moved, bool Fired, bool OrdnanceFired, bool CloseCombat, bool VehicleStepped, bool PackingCrewLostPackTi);

/// <summary>E10.3-.4 complete Game Turns. This evaluates progress, never clears TI, rolls depletion or moves supply.</summary>
public static class TurnWorkRules
{
    public static TurnWorkVerdict Evaluate(TurnWorkKind kind, int? originalBNumber, int completedGameTurns,
        bool? eligible, bool originKnown, TurnWorkWindow window)
    {
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(window))
        {
            throw new ArgumentException("Unknown work kind or window.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(completedGameTurns);
        int? required = kind switch
        {
            TurnWorkKind.AmmoTransfer => 4,
            TurnWorkKind.AmmoReplenishment when originalBNumber is > 0 and <= 9 => 1,
            TurnWorkKind.AmmoReplenishment when originalBNumber == 10 => 2,
            TurnWorkKind.AmmoReplenishment when originalBNumber == 11 => 3,
            _ => null,
        };
        var state = !originKnown || eligible is null ? TurnWorkState.NotEvaluated
            : eligible == false ? TurnWorkState.Interrupted
            : required is null ? TurnWorkState.NotEvaluated
            : completedGameTurns >= required && window == TurnWorkWindow.SecondCloseCombat
                ? TurnWorkState.CompletionPending : TurnWorkState.InProgress;
        return new(required, state);
    }

    /// <summary>
    /// The work release windows (E10.3-.4; G10): a complete Game Turn runs from the first player's RPh to the second player's CCPh, so work
    /// starts and resumes in the first player's RPh and completes in the second player's CCPh.
    /// </summary>
    public static TurnWorkWindow Window(string phase, bool phasingIsFirst) =>
        phase == "rph" && phasingIsFirst ? TurnWorkWindow.FirstRally
        : phase == "ccph" && !phasingIsFirst ? TurnWorkWindow.SecondCloseCombat
        : TurnWorkWindow.Unknown;

    /// <summary>
    /// Whether an event interrupts work in progress (E10.3; G10.11): the TI is not owned by phase cleanup, so an explicit TI or BU change,
    /// a participant leaving play or its Location, movement, fire, ordnance fire, Close Combat or a vehicle step ends the current interval.
    /// An Animal-Packing crew that becomes pinned or not in Good Order loses Pack-TI at once, and the Game Turn no longer counts (G10.11, G10.3).
    /// </summary>
    public static bool Interrupted(TurnWorkActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return activity.AuditedEligibilityLost || activity.ParticipantGoneOrMoved || activity.TiCleared || activity.SubjectButtonedUp
            || activity.Moved || activity.Fired || activity.OrdnanceFired || activity.CloseCombat || activity.VehicleStepped
            || activity.PackingCrewLostPackTi;
    }

    /// <summary>
    /// One Game Turn interval of work (E10.3): an interrupted interval does not count; an uninterrupted one counts as a complete Game Turn
    /// only with both its first-RPh and second-CCPh boundaries recorded.
    /// </summary>
    public static TurnWorkIntervalOutcome Interval(bool interrupted, bool firstRallyRecorded, bool secondCloseCombatRecorded) =>
        interrupted ? TurnWorkIntervalOutcome.Interrupted
        : firstRallyRecorded && secondCloseCombatRecorded ? TurnWorkIntervalOutcome.Complete
        : TurnWorkIntervalOutcome.Incomplete;
}
