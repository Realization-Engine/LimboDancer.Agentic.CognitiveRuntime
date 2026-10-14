
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
