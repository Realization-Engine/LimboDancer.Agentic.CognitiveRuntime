namespace LimboDancer.Domains.Asl.Rules;

/// <summary>An ADJACENT enemy unit as the surrender reads it (A15.5, A20.21, A20.51): whether it may be a captor, and whether it has Guard capacity for the unit.</summary>
public sealed record CaptorFacts(string Id, bool Captor, bool CanGuard);

/// <summary>
/// The Close Combat effects, capture, and prisoners (A11.11 to A11.22, A15.5, A20.2 to A20.55; pass 32.g, S6): the events a round's resolution orders as
/// ordered condition lists, the captured unit's effects, the surrender's captor, Known enemies and the captors, the Ambush Withdrawal, and a Guard's
/// transfer or abandonment. Play reads the state, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1PrisonerCalculator
{
    /// <summary>A Known enemy unit (A10.51, A15.44, A20.4): not a Dummy, not concealed, not hidden, and not a prisoner.</summary>
    public static bool KnownEnemy(bool dummy, bool concealed, bool hidden, bool captured) => !dummy && !concealed && !hidden && !captured;

    /// <summary>
    /// A15.44: whether a Known enemy unit is in a Location or has a clear LOS to it, over the Locations of the Known enemy units: true at once when one is the
    /// Location; else the LOS from each in turn (true clear, false blocked, null when the map cannot say), true at the first clear one; null when any LOS
    /// was unknown and none clear; else false.
    /// </summary>
    public static bool? KnownEnemyInLos(IReadOnlyList<int> enemyLocations, int at, Func<int, bool?> los)
    {
        ArgumentNullException.ThrowIfNull(enemyLocations);
        ArgumentNullException.ThrowIfNull(los);
        if (enemyLocations.Contains(at))
        {
            return true;
        }

        var unknown = false;
        foreach (var location in enemyLocations)
        {
            switch (los(location))
            {
                case true:
                    return true;
                case false:
                    break;
                default:
                    unknown = true;
                    break;
            }
        }

        return unknown ? null : false;
    }

    /// <summary>A1.6 (p. 45), A20.51: a unit's US#: a squad 3, a HS or crew 2, a SMC 1.</summary>
    public static int UnitSize(bool squad, bool halfSquadOrCrew) => squad ? 3 : halfSquadOrCrew ? 2 : 1;

    /// <summary>A20.51: a Guard may hold prisoners whose US# total at most five times its own.</summary>
    public static bool CanGuard(int load, int prisonerSize, int captorSize) => load + prisonerSize <= 5 * captorSize;

    /// <summary>
    /// A15.5, A20.21, A.8: a unit may surrender to an ADJACENT enemy unit that is active, from the catalog, Known (or revealed and neither a Dummy nor a
    /// prisoner), unbroken, not berserk, not in Melee, armed, and Infantry (MMC or SMC).
    /// </summary>
    public static bool Captor(bool active, bool enemy, bool fromCatalog, bool known, bool revealedUnit, bool broken, bool berserk, bool melee, bool unarmed, bool infantry) =>
        active && enemy && fromCatalog && (known || revealedUnit) && !broken && !berserk && !melee && !unarmed && infantry;

    /// <summary>A20.21, A20.51 (ruling R14.5): the captors with Guard capacity, by id; with none, every captor, and the captor's side frees the unit as Unarmed.</summary>
    public static IReadOnlyList<string> Captors(IReadOnlyList<CaptorFacts> adjacent)
    {
        ArgumentNullException.ThrowIfNull(adjacent);
        string[] guards = [.. adjacent.Where(other => other.Captor && other.CanGuard).Select(other => other.Id).Order(StringComparer.Ordinal)];
        return guards.Length > 0 ? guards : [.. adjacent.Where(other => other.Captor).Select(other => other.Id).Order(StringComparer.Ordinal)];
    }

    /// <summary>A18.12: a leader created by a CC attack, named for its attack.</summary>
    public static string CreatedLeaderId(string attemptId, int attack) => $"{attemptId}-leader-{attack.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    /// <summary>
    /// A18.12 (pass 31, play test P-26): a created leader's conditions, in the record's order: Good Order, unconcealed, neither berserk nor a prisoner nor in
    /// Melee, and Fanatic when his MMC is (A10.8).
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> CreatedCcLeaderConditions(bool mmcFanatic)
    {
        List<(UnitCondition, bool)> conditions = [(UnitCondition.Broken, false), (UnitCondition.Pinned, false), (UnitCondition.Wounded, false), (UnitCondition.Concealed, false),
            (UnitCondition.Hidden, false), (UnitCondition.Berserk, false), (UnitCondition.Captured, false), (UnitCondition.Melee, false)];
        if (mmcFanatic)
        {
            conditions.Add((UnitCondition.Fanatic, true));
        }

        return conditions;
    }

    /// <summary>A11.22, A18.12 (ruling R14.8): a leader created by an attack whose MMC infiltrates goes with it; else he is placed in the CC Location.</summary>
    public static string CreatedLeaderPlacedAt(string? mmcInfiltratedTo, string location) => mmcInfiltratedTo ?? location;

    /// <summary>A20.551, A11.19, A12.14 (rulings R14.2, R14.6): the conditions a surviving unit takes first after a round, in order: Armed again, its "?" and hidden status lost.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> RoundConditions(bool armed, bool concealmentLost)
    {
        var conditions = new List<(UnitCondition, bool)>();
        if (armed)
        {
            conditions.Add((UnitCondition.Unarmed, false));
        }

        if (concealmentLost)
        {
            conditions.Add((UnitCondition.Concealed, false));
            conditions.Add((UnitCondition.Hidden, false));
        }

        return conditions;
    }

    /// <summary>A11.11, A15.46: the conditions that follow for a unit not rearmed: wounded (when not already), and berserk ended.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> AfterRoundConditions(bool wounded, bool unitWounded, bool berserkEnded)
    {
        var conditions = new List<(UnitCondition, bool)>();
        if (wounded && !unitWounded)
        {
            conditions.Add((UnitCondition.Wounded, true));
        }

        if (berserkEnded)
        {
            conditions.Add((UnitCondition.Berserk, false));
        }

        return conditions;
    }

    /// <summary>A20.551 (ruling R14.6): a rearmed unit's extra conditions over the round's: Unarmed no longer, and no longer captured when it escaped.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> RearmedConditions(bool escaped)
    {
        List<(UnitCondition, bool)> conditions = [(UnitCondition.Unarmed, false)];
        if (escaped)
        {
            conditions.Add((UnitCondition.Captured, false));
        }

        return conditions;
    }

    /// <summary>A11.11, A7.302: a unit whose final definition differs is Casualty Reduced to its HS, with the same status.</summary>
    public static bool Reduced(string definitionId, string finalDefinitionId) => finalDefinitionId != definitionId;

    /// <summary>A11.2, A11.21, A11.22 (rulings R5.5, R14.8): where a surviving unit moves after the round, unless it was rearmed in place; null when it stays.</summary>
    public static string? MovesAfterRound(string? withdrewTo, string? infiltratedTo, string? rearmedAs) => rearmedAs is null ? withdrewTo ?? infiltratedTo : null;

    /// <summary>A20.24: a captured unit abandons its SW, unless it was a squad exchanged for two HS, whose SW the free HS keeps (referee, pass 14).</summary>
    public static bool CaptiveDropsWeapons(bool? capturedHalf) => capturedHalf != true;

    /// <summary>A20.22 (ruling R14.4): the ids of the two HS a squad captured at the Kill Number becomes, the first captured.</summary>
    public static (string Captive, string Free) CapturedHalfIds(string attemptId, string unitId) => ($"{attemptId}-{unitId}-a", $"{attemptId}-{unitId}-b");

    /// <summary>A20.5, A20.54 (ruling R14.4): a prisoner is never broken, Disrupted, pinned, under DM, concealed, hidden, or in Melee, and is Unarmed.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> CapturedConditions() =>
        [(UnitCondition.Broken, false), (UnitCondition.Disrupted, false), (UnitCondition.Pinned, false), (UnitCondition.DesperationMorale, false), (UnitCondition.Concealed, false),
            (UnitCondition.Hidden, false), (UnitCondition.Melee, false), (UnitCondition.Unarmed, true)];

    /// <summary>A15.5, A20.5, A20.54: a surrendering unit taken prisoner is no longer broken, Disrupted, pinned, or under DM.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> TakenPrisonerConditions() =>
        [(UnitCondition.Broken, false), (UnitCondition.Disrupted, false), (UnitCondition.Pinned, false), (UnitCondition.DesperationMorale, false)];

    /// <summary>The surrendering unit has not surrendered.</summary>
    public static string NoSurrenderText(string unitId) => $"play.no-surrender: {unitId} has not surrendered";

    /// <summary>
    /// A20.21, A20.51 (ruling R14.5): the unit is freed, or sent to an able captor, when the owner asks to free it, or names a captor without capacity while
    /// another has it.
    /// </summary>
    public static bool FreesOrRedirects(bool free, bool reject, bool anyAble, bool namedAble, bool namedIsCaptor) => free || (!reject && anyAble && !namedAble && namedIsCaptor);

    /// <summary>A20.21, A20.51: a captor with capacity takes the unit.</summary>
    public static string CaptorCapacityText(IReadOnlyList<string> able, string unitId)
    {
        ArgumentNullException.ThrowIfNull(able);
        return $"play.captor-capacity: {string.Join(", ", able)} can guard {unitId}, so it is taken by one of them (A20.21, A20.51)";
    }

    /// <summary>A20.21, A20.51: with no captor able to guard it, the unit abandons its SW and is freed as Unarmed.</summary>
    public static string FreedUnarmedText(string unitId) => $"play.freed-unarmed: no captor can guard {unitId}, so it abandons its SW and is freed as an Unarmed unit (A20.21, A20.51)";

    /// <summary>A20.3 (ruling R5.6), A15.5: the surrender is rejected and the unit's side faced with No Quarter.</summary>
    public static string NoQuarterText(string unitId, string? side) =>
        $"play.no-quarter: the surrender of {unitId} is rejected and it is eliminated; the {side} side is faced with No Quarter from now on: its units never surrender (A20.3, A15.5)";

    /// <summary>A15.5: the captor must be one the surrender named, active and placed, and the prisoner placed.</summary>
    public static string CaptorText(string unitId, IReadOnlyList<string> captors)
    {
        ArgumentNullException.ThrowIfNull(captors);
        return $"play.captor: {unitId} surrenders to one of {string.Join(", ", captors)} (A15.5)";
    }

    /// <summary>A15.5, A20.5: the capture's words.</summary>
    public static string CaptureSummary(string prisonerId, string captorId, string locationText) => $"play.capture: {prisonerId} surrenders to {captorId} and is its prisoner in {locationText} (A15.5, A20.5)";

    /// <summary>A11.41 (ruling R14.9): an Ambush Withdrawal is made in the CCPh by the side that ambushed, before the Location's first round or once its CC is over.</summary>
    public static string? AmbushWithdrawalPhaseBar(string? phase, bool ambushed, bool roundsBegunAndOpen) =>
        phase != "ccph" || !ambushed || roundsBegunAndOpen
            ? "play.ambush-withdraw-phase: an Ambush Withdrawal is made in the CCPh by the side that ambushed, before the Location's first round or once its CC is over (A11.41)"
            : null;

    /// <summary>A11.41: a withdrawing unit is Infantry of the ambushing side in the Location, not pinned, berserk, Disrupted, or captured.</summary>
    public static bool AmbushWithdrawer(bool active, bool ofAmbusher, bool here, bool pinned, bool berserk, bool disrupted, bool captured, bool vehicle) =>
        active && ofAmbusher && here && !pinned && !berserk && !disrupted && !captured && !vehicle;

    /// <summary>A11.41: the refusal of a unit that is not one.</summary>
    public static string AmbushWithdrawerText(string unitId, string locationText) => $"play.ambush-withdraw-unit: {unitId} is not Infantry of the ambushing side in {locationText} that is free to withdraw (A11.41)";

    /// <summary>A11.41, A11.21: the destination is one a withdrawal could reach.</summary>
    public static string? AmbushWithdrawalToBar(string unitId, bool reachable) =>
        reachable ? null : $"play.ambush-withdraw-to: {unitId} may withdraw only to an ADJACENT Location a withdrawal could reach (A11.41, A11.21)";

    /// <summary>A11.41, A4.72: the Ambush Withdrawal's words.</summary>
    public static string AmbushWithdrawalSummary(IReadOnlyList<string> ids, string locationText, string toText, IReadOnlyList<string> tiring)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(tiring);
        return $"play.ambush-withdraw: {string.Join(", ", ids)} withdraw from {locationText} to {toText} (A11.41)" + (tiring.Count > 0 ? $"; {string.Join(", ", tiring)} become CX (A4.72)" : string.Empty);
    }

    /// <summary>A20.5: a Guard with no prisoners.</summary>
    public static string GuardNoneText(string guardId) => $"play.guard-none: {guardId} guards no prisoners (A20.5)";

    /// <summary>A20.5 (ruling R14.7): a Guard hands over or abandons its prisoners in its own side's RPh or APh, when not held in Melee.</summary>
    public static string? GuardPhaseBar(string? phase, bool phasing, bool melee) =>
        phase is not ("rph" or "aph") || !phasing || melee ? "play.guard-phase: a Guard hands over or abandons its prisoners in its own side's RPh or APh, when not held in Melee (A20.5)" : null;

    /// <summary>A20.5, A20.53: the abandonment's words.</summary>
    public static string PrisonersAbandonedText(string guardId, IReadOnlyList<string> prisoners)
    {
        ArgumentNullException.ThrowIfNull(prisoners);
        return $"play.prisoners-abandoned: {guardId} abandons {string.Join(", ", prisoners)}, who are Unarmed units of their own side (A20.5, A20.53)";
    }

    /// <summary>A20.5: the heir is another armed Personnel unit of the Guard's side in its Location, free of Melee and not a prisoner.</summary>
    public static bool Heir(bool active, bool self, bool sameSide, bool sameLocation, bool captured, bool unarmed, bool melee, bool infantry) =>
        active && !self && sameSide && sameLocation && !captured && !unarmed && !melee && infantry;

    /// <summary>A20.5: the refusal of an heir that is not one.</summary>
    public static string HeirText(string? toId, string side, string guardId) => $"play.guard-heir: {toId} is not an armed Personnel unit of {side} in {guardId}'s Location, free of Melee (A20.5)";

    /// <summary>A20.51: the heir's capacity.</summary>
    public static string? GuardCapacityBar(string heirId, bool canGuard) => canGuard ? null : $"play.guard-capacity: {heirId} can guard prisoners of at most five times its US# (A20.51)";

    /// <summary>A20.5, A4.431: the transfer's words.</summary>
    public static string PrisonersTransferredText(string guardId, IReadOnlyList<string> prisoners, string heirId)
    {
        ArgumentNullException.ThrowIfNull(prisoners);
        return $"play.prisoners-transferred: {guardId} hands {string.Join(", ", prisoners)} to {heirId} (A20.5, A4.431)";
    }
}
