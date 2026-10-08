namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Rally decisions the planner made before and after the Rally package (unit step 19; pass 32.f): what a live attempt declares from the
/// state, the planner's bars before the package runs, the wording of a Self-Rally refusal, what the package asks for next, and the pending
/// surrender after a Heat of Battle. The package itself, <see cref="ScenarioA1RallyCalculator"/>, does not grow.
/// </summary>
public static class ScenarioA1RallyRules
{
    /// <summary>A10.6, A10.63, A10.71: another friendly leader in the unit's Location counts, Good Order or broken.</summary>
    public static bool OtherFriendlyLeader(bool active, bool self, bool sameSide, bool leader) => active && !self && sameSide && leader;

    /// <summary>A15.41: another friendly unit from the catalog in the Location, not a prisoner, is a companion a berserk leader tries to take with him.</summary>
    public static bool BerserkCompanion(bool active, bool self, bool sameSide, bool fromCatalog, bool captured) => active && !self && sameSide && fromCatalog && !captured;

    /// <summary>
    /// A25.221, A25.222 (backlog pass 15, ruling R15.6): the Commissar who acts on a unit is of its side, in its Location, from the catalog as a Commissar,
    /// and not broken, pinned, berserk, or captured, and not the unit itself.
    /// </summary>
    public static bool Commissar(bool active, bool self, bool sameSide, bool commissar, bool broken, bool pinned, bool berserk, bool captured) =>
        active && !self && sameSide && commissar && !broken && !pinned && !berserk && !captured;

    /// <summary>
    /// What a live Rally attempt declares (A10.6, A15.41, A19.3, E3.742; rulings R15.6, R15.10, R16.14): the facts as the state has them, with the
    /// derived ones decided here: the phase's name, the rallying side, the leaders in the Location, the side's first MMC Rally while it is open, and
    /// Extreme Winter's Fate outside a building. The record keeps its shape (the pass 32 design, D5).
    /// </summary>
    public static RallyAttempt Attempt(RallyAttemptStateFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var unit = facts.Unit;
        var leader = facts.Leader;
        return new RallyAttempt(
            facts.Phase == "rph" ? "RPh" : facts.Phase,
            facts.Phasing ? "phasing" : "non-phasing",
            new RallyUnit(unit.Id, unit.DefinitionId, unit.Location, unit.Broken, unit.Disrupted, unit.Wounded, unit.DesperationMorale, unit.Concealed,
                unit.AttemptedThisPlayerTurn, unit.RepairedThisPhase || unit.TookRallyPhaseAction)
            {
                Fanatic = unit.Fanatic ? true : null,
                // A19.3 (ruling R15.10): a Green MMC's Inexperience, for its Heat of Battle DRM.
                Inexperienced = unit.Inexperienced,
            },
            leader is null ? null : new RallyLeader(leader.Id, leader.DefinitionId, leader.Location, leader.Broken, leader.Wounded, leader.Concealed),
            unit.Location,
            facts.Terrain,
            facts.OtherLeadersBroken.Any(broken => !broken),
            facts.OtherLeadersBroken.Any(broken => broken),
            facts.Phasing && !facts.FirstMmcRallyTaken,
            facts.EnemyGoodOrderInLosWithin16,
            null)
        {
            KnownEnemyInLos = facts.KnownEnemyInLos,
            Captors = facts.Captors,
            Companions = facts.Companions.Count > 0 ? facts.Companions : null,
            NoQuarter = facts.NoQuarter ? true : null,
            Commissar = facts.CommissarId,
            // E3.742 (backlog pass 16, ruling R16.14): Extreme Winter's Fate outside a building.
            ExtremeWinterFate = facts.ExtremeWinter && facts.Terrain is not ("wooden-building" or "stone-building") ? true : null,
        };
    }

    /// <summary>A18.11: the MMC (a squad, half-squad, or crew) of a side whose Rally attempt counts as the side's first MMC Rally.</summary>
    public static bool IsMmcRallier(bool sameSide, string kind) => sameSide && kind is "asl:squad" or "asl:half-squad" or "asl:crew";

    /// <summary>A1.31, A1.32: a leader who directed a Deployment or permitted a Recombination this RPh rallies no one.</summary>
    public static string? LeaderRallyPhaseActionBar(string? leaderId, bool tookRallyPhaseAction) =>
        leaderId is not null && tookRallyPhaseAction ? $"play.rph-action: {leaderId} directed a Deployment or permitted a Recombination this RPh, his sole RPh action (A1.31, A1.32)" : null;

    /// <summary>The unit must be on a Location the map reads.</summary>
    public static string RallyUnitUnreadText(string unitId) => $"play.rally-unit: '{unitId}' is not a unit on a Location the map reads";

    /// <summary>A10.6: the rallying leader is of the unit's side; one of another nationality leads it as Allied Troops (A10.7; ruling R15.8).</summary>
    public static string? LeaderSideBar(string? leaderId, string unitId, bool leaderOfOtherSide) =>
        leaderId is not null && leaderOfOtherSide ? $"play.rally-leader: {leaderId} is not of {unitId}'s side (A10.6)" : null;

    /// <summary>A12.141: the attempt costs a concealed unit or rallying leader its "?" in the LOS of a Good Order enemy within 16 hexes.</summary>
    public static bool ConcealedAttempt(bool unitConcealed, bool leaderConcealed) => unitConcealed || leaderConcealed;

    /// <summary>A15.44, A15.5: a leader's rally can reach Heat of Battle, so the planner reads the unit's LOS to a Known enemy and its captors.</summary>
    public static bool ReachesHeatOfBattle(string? leaderId) => leaderId is not null;

    /// <summary>
    /// Pass 31 (play test R-04; A10.63, A18.11, A10.71): which of the Self-Rally rules bars this unit is said, not only that one does. The texts stay
    /// in Play: the one that names the unit that used the attempt (pass 31c, backlog section 48) holds a hole the text list cuts at a nested quote.
    /// </summary>
    public static SelfRallyBar SelfRallyRefusal(IReadOnlyList<string> precheck, bool phasing, bool firstMmcRallyTaken)
    {
        ArgumentNullException.ThrowIfNull(precheck);
        return !precheck.Contains("asl.a1.rally.self-rally-not-capable") ? SelfRallyBar.None
            : !phasing ? SelfRallyBar.NotOwnPhase
            : firstMmcRallyTaken ? SelfRallyBar.Used
            : SelfRallyBar.BrokenLeader;
    }

    /// <summary>A rally by a concealed unit that stays concealed is its side's own business (ruling R19.8).</summary>
    public static bool Withheld(bool concealed, bool? enemyGoodOrderInLosWithin16) => concealed && enemyGoodOrderInLosWithin16 != true;

    /// <summary>The plan's words.</summary>
    public static string RallySummary(string unitId, string? leaderId) =>
        $"play.rally: {unitId} attempts to rally in the RPh" + (leaderId is null ? " by Self-Rally" : $", rallied by {leaderId}");

    /// <summary>
    /// What the Rally package asks for next (ruling R5.8): the Rally DR, a leader's Wound Severity dr (A17.11), the Heat of Battle DR (A15.1), the
    /// Leader Creation dr (A18.2), or the NTC of a companion a berserk leader tries to take with him (A15.41); or an option its owner answers; or
    /// the attempt is resolved or left undecided.
    /// </summary>
    public static RallyNextStep NextStep(string disposition, IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        if (disposition == RallyResolution.Resolved)
        {
            return new RallyNextStep(true, null, null, 0, null, null);
        }

        // An option the attempt reaches stops it until its owner answers (ruling R5.8).
        if (reasons is [{ } option] && option.StartsWith("asl.a1.rally.choice-missing:", StringComparison.Ordinal))
        {
            return new RallyNextStep(false, option["asl.a1.rally.choice-missing:".Length..], null, 0, null, null);
        }

        if (reasons is not [{ } missing] || !missing.StartsWith("asl.a1.rally.roll-missing:", StringComparison.Ordinal))
        {
            return new RallyNextStep(false, null, null, 0, null, string.Join("; ", reasons));
        }

        const string BerserkCheck = "asl.a1.rally.roll-missing:berserkCheck:";
        var companion = missing.StartsWith(BerserkCheck, StringComparison.Ordinal) ? missing[BerserkCheck.Length..] : null;
        var rollKey = companion is not null ? "berserkCheck:" + companion
            : missing.Contains("woundSeverity", StringComparison.Ordinal) ? "woundSeverity"
            : missing.EndsWith(":heatOfBattle", StringComparison.Ordinal) ? "heatOfBattle"
            : missing.EndsWith(":leaderCreation", StringComparison.Ordinal) ? "leaderCreation"
            : "rally";
        var (count, purpose) = rollKey switch
        {
            "woundSeverity" => (1, "rally-wound-severity"),
            "heatOfBattle" => (2, "rally-heat-of-battle"),
            "leaderCreation" => (1, "rally-leader-creation"),
            _ when companion is not null => (2, "rally-berserk-check"),
            _ => (2, "rally"),
        };
        return new RallyNextStep(false, null, rollKey, count, purpose, null);
    }

    /// <summary>A15.5: a unit that surrendered to ADJACENT captors and is not eliminated waits for the captor's choice.</summary>
    public static bool SurrendersAfterRally(bool heatOfBattleSurrender, bool captorsNamed, bool eliminated) => heatOfBattleSurrender && captorsNamed && !eliminated;

    /// <summary>A15.5: the surrendering unit's id, its new one when the attempt Replaced or Reduced it.</summary>
    public static string SurrenderingId(string attemptId, string unitId, bool replaced) => replaced ? $"{attemptId}-{unitId}" : unitId;
}
