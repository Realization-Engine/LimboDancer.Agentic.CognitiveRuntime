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

    /// <summary>A15.21 (rulings R5.10, R5.11): the hero a Rally creates may be concealed when the unit kept its "?" and was not eliminated.</summary>
    public static bool HeroConcealed(bool unitLostConcealment, bool eliminated) => !unitLostConcealment && !eliminated;

    /// <summary>A15.41: the conditions a companion who went berserk with a berserk leader takes, rallied if broken, in the order the record writes them.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> BerserkCompanionConditions() =>
        [(UnitCondition.Berserk, true), (UnitCondition.Broken, false), (UnitCondition.Pinned, false), (UnitCondition.Disrupted, false), (UnitCondition.DesperationMorale, false), (UnitCondition.Concealed, false)];

    /// <summary>A18.11: the created leader is Good Order, in the rallied unit's Location; one from a Fanatic unit is Fanatic (A10.8).</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> CreatedLeaderConditions(bool unitFanatic)
    {
        List<(UnitCondition, bool)> conditions = [(UnitCondition.Broken, false), (UnitCondition.Pinned, false), (UnitCondition.Wounded, false), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)];
        if (unitFanatic)
        {
            conditions.Add((UnitCondition.Fanatic, true));
        }

        return conditions;
    }

    /// <summary>A15.3: Battle Hardening is among the effect's events.</summary>
    public static bool BattleHardened(IEnumerable<string> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return events.Contains("battle-hardened");
    }

    /// <summary>
    /// A10.64, A7.302, A15.3, A15.1, A15.21: the conditions the replacing unit takes over a copy of the unit's own, in order: its "?" kept only when Battle
    /// Hardened and not lost (null keeps the unit's own value), never hidden, and when hardened unbroken, unpinned, not Disrupted, not under DM, Fanatic
    /// and heroic as the effect says.
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool? Value)> ReplacedUnitConditions(bool hardened, bool concealmentLost, bool? effectFanatic, bool? effectHeroic)
    {
        List<(UnitCondition, bool?)> produced = [(UnitCondition.Concealed, concealmentLost || !hardened ? false : null), (UnitCondition.Hidden, false)];
        if (hardened)
        {
            produced.Add((UnitCondition.Broken, false));
            produced.Add((UnitCondition.Pinned, false));
            produced.Add((UnitCondition.Disrupted, false));
            produced.Add((UnitCondition.DesperationMorale, false));
            if (effectFanatic == true)
            {
                produced.Add((UnitCondition.Fanatic, true));
            }

            // A15.1, A15.21: a Final DR of 5 or 6 makes a leader heroic and Battle Hardens him.
            if (effectHeroic == true)
            {
                produced.Add((UnitCondition.Heroic, true));
            }
        }

        return produced;
    }

    /// <summary>A15.3, A25.222 (ruling R15.6): the lineage is Replaced when Battle Hardened, or when a Commissar's failed rally replaces the unit by one of its kind; else Reduced.</summary>
    public static bool ReplacedNotReduced(bool hardened, bool? replacedByCommissar, bool sameKind) => hardened || (replacedByCommissar == true && sameKind);

    /// <summary>
    /// A19.12, A10.8, A15.3, A15.21, A15.4, A15.42, A15.5: the conditions a rallied, Fanatic, heroic, berserk, surrendering, or wounded unit takes, in
    /// order: a rally ends Disruption, a berserk unit loses DM and "?", a surrendering one is Disrupted, and its lost "?" last.
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> RalliedUnitConditions(bool rallied, bool unitDisrupted, bool? effectFanatic, bool unitFanatic, bool? effectHeroic,
        bool? effectBerserk, bool? effectDisrupted, bool effectWounded, bool unitWounded, bool concealmentLost)
    {
        var conditions = new List<(UnitCondition, bool)>();
        if (rallied)
        {
            // A19.12: a Disrupted unit rallied is no longer Disrupted.
            conditions.Add((UnitCondition.Broken, false));
            if (unitDisrupted)
            {
                conditions.Add((UnitCondition.Disrupted, false));
            }
        }

        // A10.8, A15.3: Fanaticism; A15.21: a heroic leader.
        if (effectFanatic == true && !unitFanatic)
        {
            conditions.Add((UnitCondition.Fanatic, true));
        }

        if (effectHeroic == true)
        {
            conditions.Add((UnitCondition.Heroic, true));
        }

        // A15.4, A15.42: a berserk unit, rallied, loses DM and "?"; A15.5: a surrendering one is Disrupted.
        if (effectBerserk == true)
        {
            conditions.Add((UnitCondition.Berserk, true));
            conditions.Add((UnitCondition.DesperationMorale, false));
            conditions.Add((UnitCondition.Concealed, false));
        }

        if (effectDisrupted == true)
        {
            conditions.Add((UnitCondition.Disrupted, true));
        }

        if (effectWounded && !unitWounded)
        {
            conditions.Add((UnitCondition.Wounded, true));
        }

        if (concealmentLost)
        {
            conditions.Add((UnitCondition.Concealed, false));
        }

        return conditions;
    }

    /// <summary>A9.72 (p. 65): a SW is repaired in the RPh.</summary>
    public static string? SwRepairPhaseBar(string? phase) => phase != "rph" ? "play.repair-phase: a SW is repaired in the RPh (A9.72, p. 65)" : null;

    /// <summary>A9.72, as the planner has it: the repairing unit is Good Order when its Broken condition is known false; the projector's test differs (section 12).</summary>
    public static bool SwRepairGoodOrderAsPlanned(bool? broken) => broken == false;

    /// <summary>A9.72: the unit is not a Good Order unit.</summary>
    public static string SwRepairUnitText(string unitId) => $"play.repair-unit: '{unitId}' is not a Good Order unit (A9.72)";

    /// <summary>A3.1 (p. 47): a unit that attempted to rally this RPh does not repair.</summary>
    public static string? SwRepairRalliedBar(string unitId, bool attemptedThisPlayerTurn) =>
        attemptedThisPlayerTurn ? $"play.repair-unit: '{unitId}' attempted to rally this RPh (A3.1, p. 47)" : null;

    /// <summary>A1.31 (ruling R13.4): a unit that took its RPh action does not repair.</summary>
    public static string? SwRepairActionBar(string unitId, bool tookRallyPhaseAction) =>
        tookRallyPhaseAction ? $"play.rph-action: {unitId} has taken its RPh action (a Deployment, Recombination, Recovery, or Transfer) (A1.31; ruling R13.4)" : null;

    /// <summary>A9.72: the SW is a malfunctioned one the unit possesses.</summary>
    public static bool SwRepairWeaponAllowed(bool possessedByUnit, bool malfunctioned) => possessedByUnit && malfunctioned;

    /// <summary>A9.72: the SW is not a malfunctioned one the unit possesses.</summary>
    public static string SwRepairWeaponText(string equipmentId, string unitId) => $"play.repair-weapon: '{equipmentId}' is not a malfunctioned SW '{unitId}' possesses";

    /// <summary>A9.72: the SW has a Repair Number in the catalog.</summary>
    public static string? SwRepairNumberBar(string equipmentId, int? repairNumber) =>
        repairNumber is null ? $"play.repair-weapon: '{equipmentId}' has no Repair Number in the catalog" : null;

    /// <summary>A9.72: a dr of 6 eliminates the SW, at most the Repair Number repairs it, anything else changes nothing; one function for the planner and the projector (D6).</summary>
    public static RepairOutcome SwRepairResult(int dr, int repairNumber) => dr == 6 ? RepairOutcome.Eliminated : dr <= repairNumber ? RepairOutcome.Repaired : RepairOutcome.NoChange;

    /// <summary>A concealed or hidden unit's Repair attempt is withheld from the enemy.</summary>
    public static bool RepairWithheld(bool concealed, bool hidden) => concealed || hidden;

    /// <summary>The plan's words for a SW repair.</summary>
    public static string SwRepairSummary(string unitId, string equipmentId, int repairNumber) => $"play.repair: {unitId} attempts to repair {equipmentId} (R{repairNumber}; a 6 eliminates it)";

    /// <summary>D3.7: a vehicle's MG is repaired in the RPh by an active vehicle.</summary>
    public static string? VehicleRepairPhaseBar(string? phase, bool active) => phase != "rph" || !active ? "play.repair-phase: a vehicle's MG is repaired in the RPh (D3.7)" : null;

    /// <summary>D3.7: the MG is malfunctioned and not disabled for good.</summary>
    public static string? VehicleRepairWeaponBar(string vehicleId, bool malfunctioned, bool disabled) =>
        !malfunctioned || disabled ? $"play.repair-weapon: {vehicleId}'s MG is not malfunctioned, or is disabled (D3.7)" : null;

    /// <summary>D3.7 (ruling R6.10), as the planner has it: a CE crew repairs once per RPh; the projector's test differs (section 12) and stays its own.</summary>
    public static string? VehicleRepairCrewBarAsPlanned(string vehicleId, bool crewExposed, bool repairedThisPhase) =>
        !crewExposed || repairedThisPhase ? $"play.repair-unit: {vehicleId}'s AAMG is repaired once per RPh by a CE crew that is not Stunned or Recalled (D3.7)" : null;

    /// <summary>D3.7: a dr of 1 repairs the MG, a 6 disables it for good; one function for the planner and the projector (D6).</summary>
    public static RepairOutcome VehicleMgRepairResult(int dr) => dr == 6 ? RepairOutcome.Eliminated : dr == 1 ? RepairOutcome.Repaired : RepairOutcome.NoChange;

    /// <summary>D3.7: a repaired MG is no longer malfunctioned; a 6 disables it.</summary>
    public static (UnitCondition Condition, bool Value) VehicleRepairChange(RepairOutcome outcome) =>
        outcome == RepairOutcome.Repaired ? (UnitCondition.Malfunctioned, false) : (UnitCondition.Disabled, true);

    /// <summary>The plan's words for a vehicle MG repair.</summary>
    public static string VehicleRepairSummary(string vehicleId) => $"play.repair: {vehicleId}'s crew attempts to repair its AAMG (a dr of 1 repairs it, a 6 disables it; D3.7)";

    /// <summary>C7.42 (ruling R7.8): the Shock recovery dr is made in the RPh.</summary>
    public static bool ShockRollPhase(string? phase) => phase == "rph";

    /// <summary>C7.42: an active Shocked AFV or Unconfirmed Kill owes one dr this RPh until it has made it.</summary>
    public static bool OwesShockRoll(bool active, bool vehicle, bool shocked, bool unconfirmedKill, bool rolledThisPhase) =>
        active && vehicle && (shocked || unconfirmedKill) && !rolledThisPhase;

    /// <summary>C7.42: the dr is made in the RPh.</summary>
    public static string? ShockPhaseBar(string? phase) => phase != "rph" ? "play.shock-phase: a Shocked AFV or an Unconfirmed Kill makes its dr in the RPh (C7.42)" : null;

    /// <summary>C7.42: the vehicle is not an active one that still owes its dr.</summary>
    public static string ShockVehicleText(string vehicleId) => $"play.shock-vehicle: '{vehicleId}' is not a Shocked AFV or an Unconfirmed Kill that still owes its dr this RPh (C7.42)";

    /// <summary>C7.42: an Unconfirmed Kill's dr of 4 to 6 eliminates it as a wreck, with no Crew Survival.</summary>
    public static bool ShockRecoveryWrecks(string result) => result == ScenarioA1ResultTables.ShockWrecked;

    /// <summary>C7.42: otherwise the Shock is removed and the Unconfirmed Kill set or cleared, in that order.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> ShockRecoveryConditions(string result) =>
        [(UnitCondition.Shocked, false), (UnitCondition.UnconfirmedKill, result == ScenarioA1ResultTables.ShockUnconfirmedKill)];

    /// <summary>The plan's words for a Shock recovery.</summary>
    public static string ShockRecoverySummary(string vehicleId, bool unconfirmedKill) => unconfirmedKill
        ? $"play.shock-recovery: {vehicleId} is an Unconfirmed Kill: a dr of 1 to 3 removes it, 4 to 6 wrecks the AFV (C7.42)"
        : $"play.shock-recovery: {vehicleId} is Shocked: a dr of 1 or 2 removes the Shock, 3 to 6 makes it an Unconfirmed Kill (C7.42)";
}
