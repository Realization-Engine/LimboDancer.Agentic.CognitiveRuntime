"""Pass 32.f commit 6: Rally effects as ordered condition lists, the SW and vehicle repairs, and Shock recovery call Rules."""
import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
RALLY = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.Rally.cs"
SHOCK = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.Shock.cs"
FIRE = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs"
RULES = ROOT / "LimboDancer.Domains.Asl.Rules/ScenarioA1RallyRules.cs"
MODELS = ROOT / "LimboDancer.Domains.Asl.Rules/ScenarioA1RallyRuleModels.cs"
FIREMODELS = ROOT / "LimboDancer.Domains.Asl.Rules/ScenarioA1FireRuleModels.cs"
DRAFT = pathlib.Path(r"E:\Archive\GitHub\dlandi\pass32-tools\draft_f6_rules.cs")


def read(p):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    return t


def apply(p, edits):
    t = read(p)
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:70])
        t = t.replace(old, new)
    p.write_bytes(t.encode("utf-8"))
    print("applied", p.name, len(edits))


rules = read(RULES)
assert rules.endswith("}\n") and rules.count("\n}\n") == 1
draft = read(DRAFT)
RULES.write_bytes((rules[: -len("}\n")] + draft).encode("utf-8"))
print("rules appended")

apply(MODELS, [(
    "/// <summary>Which Self-Rally rule bars a unit",
    "/// <summary>What a Repair dr does (A9.72, D3.7): repairs the weapon, changes nothing, or eliminates or disables it.</summary>\n"
    "public enum RepairOutcome\n{\n    Repaired,\n    NoChange,\n    Eliminated,\n}\n\n"
    "/// <summary>Which Self-Rally rule bars a unit",
)])

apply(FIREMODELS, [(
    "    ButtonedUp,\n    Abandoned,\n}\n",
    "    ButtonedUp,\n    Abandoned,\n    Disabled,\n}\n",
)])

apply(FIRE, [(
    "        UnitCondition.Abandoned => Conditions.Abandoned,\n        _ => Conditions.CmgMalfunctioned,\n",
    "        UnitCondition.Abandoned => Conditions.Abandoned,\n        UnitCondition.Disabled => Conditions.Disabled,\n        _ => Conditions.CmgMalfunctioned,\n",
)])

apply(RALLY, [
    (
        "            yield return (\"instance-created\", new InstanceCreated(HeroOf(unit, hero, attemptId, concealed: !effect.ConcealmentLost.Contains(unit.Id) && !effect.Eliminated)));\n",
        "            yield return (\"instance-created\", new InstanceCreated(HeroOf(unit, hero, attemptId, concealed: ScenarioA1RallyRules.HeroConcealed(effect.ConcealmentLost.Contains(unit.Id), effect.Eliminated))));\n",
    ),
    (
        "            yield return (\"conditions-changed\", new ConditionsChanged(id, new Dictionary<string, ConditionState>(StringComparer.Ordinal)\n"
        "            {\n"
        "                [Conditions.Berserk] = ConditionState.True,\n"
        "                [Conditions.Broken] = ConditionState.False,\n"
        "                [Conditions.Pinned] = ConditionState.False,\n"
        "                [Conditions.Disrupted] = ConditionState.False,\n"
        "                [Conditions.DesperationMorale] = ConditionState.False,\n"
        "                [Conditions.Concealed] = ConditionState.False,\n"
        "            }));\n",
        "            yield return (\"conditions-changed\", new ConditionsChanged(id, ConditionChanges(ScenarioA1RallyRules.BerserkCompanionConditions())));\n",
    ),
    (
        "            var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal)\n"
        "            {\n"
        "                [Conditions.Broken] = ConditionState.False,\n"
        "                [Conditions.Pinned] = ConditionState.False,\n"
        "                [Conditions.Wounded] = ConditionState.False,\n"
        "                [Conditions.Concealed] = ConditionState.False,\n"
        "                [Conditions.Hidden] = ConditionState.False,\n"
        "            };\n"
        "            if (GameState.Condition(unit, Conditions.Fanatic) == ConditionState.True)\n"
        "            {\n"
        "                conditions[Conditions.Fanatic] = ConditionState.True;\n"
        "            }\n"
        "\n",
        "            var conditions = ConditionChanges(ScenarioA1RallyRules.CreatedLeaderConditions(GameState.Condition(unit, Conditions.Fanatic) == ConditionState.True));\n",
    ),
    (
        "            var hardened = effect.Events.Contains(\"battle-hardened\");\n",
        "            var hardened = ScenarioA1RallyRules.BattleHardened(effect.Events);\n",
    ),
    (
        "            produced[Conditions.Concealed] = effect.ConcealmentLost.Contains(unit.Id) || !hardened ? ConditionState.False\n"
        "                : GameState.Condition(unit, Conditions.Concealed);\n"
        "            produced[Conditions.Hidden] = ConditionState.False;\n"
        "            if (hardened)\n"
        "            {\n"
        "                produced[Conditions.Broken] = ConditionState.False;\n"
        "                produced[Conditions.Pinned] = ConditionState.False;\n"
        "                produced[Conditions.Disrupted] = ConditionState.False;\n"
        "                produced[Conditions.DesperationMorale] = ConditionState.False;\n"
        "                if (effect.Fanatic == true)\n"
        "                {\n"
        "                    produced[Conditions.Fanatic] = ConditionState.True;\n"
        "                }\n"
        "\n"
        "                // A15.1, A15.21: a Final DR of 5 or 6 makes a leader heroic and Battle Hardens him.\n"
        "                if (effect.Heroic == true)\n"
        "                {\n"
        "                    produced[Conditions.Heroic] = ConditionState.True;\n"
        "                }\n"
        "            }\n"
        "\n"
        "            // A25.222 (ruling R15.6): a Commissar's failed rally Replaces the unit by one of its size.\n"
        "            var replaced = hardened || (effect.ReplacedByCommissar == true && reference.Kind == unit.Kind);\n",
        "            // Rules gives the conditions in the order the record writes them; null keeps the unit's own value (its \"?\" when Battle Hardened).\n"
        "            foreach (var (condition, value) in ScenarioA1RallyRules.ReplacedUnitConditions(hardened, effect.ConcealmentLost.Contains(unit.Id), effect.Fanatic, effect.Heroic))\n"
        "            {\n"
        "                produced[ConditionName(condition)] = value is { } set ? (set ? ConditionState.True : ConditionState.False) : GameState.Condition(unit, ConditionName(condition));\n"
        "            }\n"
        "\n"
        "            // A25.222 (ruling R15.6): a Commissar's failed rally Replaces the unit by one of its size.\n"
        "            var replaced = ScenarioA1RallyRules.ReplacedNotReduced(hardened, effect.ReplacedByCommissar, reference.Kind == unit.Kind);\n",
    ),
    (
        "        var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);\n"
        "        if (effect.Rallied)\n"
        "        {\n"
        "            // A19.12: a Disrupted unit rallied is no longer Disrupted.\n"
        "            conditions[Conditions.Broken] = ConditionState.False;\n"
        "            if (GameState.Condition(unit, Conditions.Disrupted) == ConditionState.True)\n"
        "            {\n"
        "                conditions[Conditions.Disrupted] = ConditionState.False;\n"
        "            }\n"
        "        }\n"
        "\n"
        "        // A10.8, A15.3: Fanaticism; A15.21: a heroic leader.\n"
        "        if (effect.Fanatic == true && GameState.Condition(unit, Conditions.Fanatic) != ConditionState.True)\n"
        "        {\n"
        "            conditions[Conditions.Fanatic] = ConditionState.True;\n"
        "        }\n"
        "\n"
        "        if (effect.Heroic == true)\n"
        "        {\n"
        "            conditions[Conditions.Heroic] = ConditionState.True;\n"
        "        }\n"
        "\n"
        "        // A15.4, A15.42: a berserk unit, rallied, loses DM and \"?\"; A15.5: a surrendering one is Disrupted.\n"
        "        if (effect.Berserk == true)\n"
        "        {\n"
        "            conditions[Conditions.Berserk] = ConditionState.True;\n"
        "            conditions[Conditions.DesperationMorale] = ConditionState.False;\n"
        "            conditions[Conditions.Concealed] = ConditionState.False;\n"
        "        }\n"
        "\n"
        "        if (effect.Disrupted == true)\n"
        "        {\n"
        "            conditions[Conditions.Disrupted] = ConditionState.True;\n"
        "        }\n"
        "\n"
        "        if (effect.Wounded && GameState.Condition(unit, Conditions.Wounded) != ConditionState.True)\n"
        "        {\n"
        "            conditions[Conditions.Wounded] = ConditionState.True;\n"
        "        }\n"
        "\n"
        "        if (effect.ConcealmentLost.Contains(unit.Id))\n"
        "        {\n"
        "            conditions[Conditions.Concealed] = ConditionState.False;\n"
        "        }\n"
        "\n",
        "        // A19.12, A10.8, A15.3, A15.21, A15.4, A15.42, A15.5: the rallied unit's conditions, in the order the record writes them (pass 32.f).\n"
        "        var conditions = ConditionChanges(ScenarioA1RallyRules.RalliedUnitConditions(effect.Rallied, GameState.Condition(unit, Conditions.Disrupted) == ConditionState.True,\n"
        "            effect.Fanatic, GameState.Condition(unit, Conditions.Fanatic) == ConditionState.True, effect.Heroic, effect.Berserk, effect.Disrupted, effect.Wounded,\n"
        "            GameState.Condition(unit, Conditions.Wounded) == ConditionState.True, effect.ConcealmentLost.Contains(unit.Id)));\n",
    ),
    (
        "        if (state.Phase != \"rph\")\n"
        "        {\n"
        "            return Refused(scope, label, expected, \"play.repair-phase: a SW is repaired in the RPh (A9.72, p. 65)\");\n"
        "        }\n"
        "\n"
        "        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || GameState.Condition(unit, Conditions.Broken) != ConditionState.False)\n"
        "        {\n"
        "            return Refused(scope, label, expected, $\"play.repair-unit: '{unitId}' is not a Good Order unit (A9.72)\");\n"
        "        }\n"
        "\n"
        "        if (state.RallyAttemptsThisPlayerTurn.Contains(unit.Id))\n"
        "        {\n"
        "            return Refused(scope, label, expected, $\"play.repair-unit: '{unitId}' attempted to rally this RPh (A3.1, p. 47)\");\n"
        "        }\n"
        "\n"
        "        if (state.RallyPhaseActions.Contains(unit.Id))\n"
        "        {\n"
        "            return Refused(scope, label, expected, $\"play.rph-action: {unitId} has taken its RPh action (a Deployment, Recombination, Recovery, or Transfer) (A1.31; ruling R13.4)\");\n"
        "        }\n"
        "\n"
        "        if (state.Find(equipmentId) is not EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding } equipment\n"
        "            || holding.Holder != unit.Id || GameState.Condition(equipment, Conditions.Malfunctioned) != ConditionState.True)\n"
        "        {\n"
        "            return Refused(scope, label, expected, $\"play.repair-weapon: '{equipmentId}' is not a malfunctioned SW '{unitId}' possesses\");\n"
        "        }\n"
        "\n"
        "        if (equipment.Definition is null || FireReference.Value.Definitions.GetValueOrDefault(equipment.Definition.Definition)?.Repair is not { } repairNumber)\n"
        "        {\n"
        "            return Refused(scope, label, expected, $\"play.repair-weapon: '{equipmentId}' has no Repair Number in the catalog\");\n"
        "        }\n"
        "\n"
        "        var package = ScenarioA1FirePackage.Identity.ToString();\n"
        "        var withheld = GameState.Condition(unit, Conditions.Concealed) == ConditionState.True || GameState.Condition(unit, Conditions.Hidden) == ConditionState.True\n"
        "            ? new[] { unit.Side } : null;\n",
        "        if (ScenarioA1RallyRules.SwRepairPhaseBar(state.Phase) is { } phaseBar)\n"
        "        {\n"
        "            return Refused(scope, label, expected, phaseBar);\n"
        "        }\n"
        "\n"
        "        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || !ScenarioA1RallyRules.SwRepairGoodOrderAsPlanned(RuleBool(GameState.Condition(unit, Conditions.Broken))))\n"
        "        {\n"
        "            return Refused(scope, label, expected, ScenarioA1RallyRules.SwRepairUnitText(unitId));\n"
        "        }\n"
        "\n"
        "        if (ScenarioA1RallyRules.SwRepairRalliedBar(unitId, state.RallyAttemptsThisPlayerTurn.Contains(unit.Id)) is { } ralliedBar)\n"
        "        {\n"
        "            return Refused(scope, label, expected, ralliedBar);\n"
        "        }\n"
        "\n"
        "        if (ScenarioA1RallyRules.SwRepairActionBar(unitId, state.RallyPhaseActions.Contains(unit.Id)) is { } actionBar)\n"
        "        {\n"
        "            return Refused(scope, label, expected, actionBar);\n"
        "        }\n"
        "\n"
        "        if (state.Find(equipmentId) is not EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding } equipment\n"
        "            || !ScenarioA1RallyRules.SwRepairWeaponAllowed(holding.Holder == unit.Id, GameState.Condition(equipment, Conditions.Malfunctioned) == ConditionState.True))\n"
        "        {\n"
        "            return Refused(scope, label, expected, ScenarioA1RallyRules.SwRepairWeaponText(equipmentId, unitId));\n"
        "        }\n"
        "\n"
        "        var printed = equipment.Definition is null ? null : FireReference.Value.Definitions.GetValueOrDefault(equipment.Definition.Definition)?.Repair;\n"
        "        if (ScenarioA1RallyRules.SwRepairNumberBar(equipmentId, printed) is { } numberBar)\n"
        "        {\n"
        "            return Refused(scope, label, expected, numberBar);\n"
        "        }\n"
        "\n"
        "        var repairNumber = printed!.Value;\n"
        "        var package = ScenarioA1FirePackage.Identity.ToString();\n"
        "        var withheld = ScenarioA1RallyRules.RepairWithheld(GameState.Condition(unit, Conditions.Concealed) == ConditionState.True, GameState.Condition(unit, Conditions.Hidden) == ConditionState.True)\n"
        "            ? new[] { unit.Side } : null;\n",
    ),
    (
        "            var result = dr == 6 ? RepairAttempted.Eliminated : dr <= repairNumber ? RepairAttempted.Repaired : RepairAttempted.NoChange;\n",
        "            var result = RepairResult(ScenarioA1RallyRules.SwRepairResult(dr, repairNumber));\n",
    ),
    (
        "            [$\"play.repair: {unit.Id} attempts to repair {equipment.Id} (R{repairNumber}; a 6 eliminates it)\"])\n",
        "            [ScenarioA1RallyRules.SwRepairSummary(unit.Id, equipment.Id, repairNumber)])\n",
    ),
    (
        "        if (state.Phase != \"rph\" || vehicle.Status != InstanceStatus.Active)\n"
        "        {\n"
        "            return Refused(scope, label, expected, \"play.repair-phase: a vehicle's MG is repaired in the RPh (D3.7)\");\n"
        "        }\n"
        "\n"
        "        if (!Is(vehicle, Conditions.Malfunctioned) || Is(vehicle, Conditions.Disabled))\n"
        "        {\n"
        "            return Refused(scope, label, expected, $\"play.repair-weapon: {vehicle.Id}'s MG is not malfunctioned, or is disabled (D3.7)\");\n"
        "        }\n"
        "\n"
        "        if (!LiveFire.CrewExposed(vehicle) || state.RepairsThisPhase.Contains(vehicle.Id))\n"
        "        {\n"
        "            return Refused(scope, label, expected, $\"play.repair-unit: {vehicle.Id}'s AAMG is repaired once per RPh by a CE crew that is not Stunned or Recalled (D3.7)\");\n"
        "        }\n",
        "        if (ScenarioA1RallyRules.VehicleRepairPhaseBar(state.Phase, vehicle.Status == InstanceStatus.Active) is { } phaseBar)\n"
        "        {\n"
        "            return Refused(scope, label, expected, phaseBar);\n"
        "        }\n"
        "\n"
        "        if (ScenarioA1RallyRules.VehicleRepairWeaponBar(vehicle.Id, Is(vehicle, Conditions.Malfunctioned), Is(vehicle, Conditions.Disabled)) is { } weaponBar)\n"
        "        {\n"
        "            return Refused(scope, label, expected, weaponBar);\n"
        "        }\n"
        "\n"
        "        if (ScenarioA1RallyRules.VehicleRepairCrewBarAsPlanned(vehicle.Id, LiveFire.CrewExposed(vehicle), state.RepairsThisPhase.Contains(vehicle.Id)) is { } crewBar)\n"
        "        {\n"
        "            return Refused(scope, label, expected, crewBar);\n"
        "        }\n",
    ),
    (
        "            var result = dr == 6 ? RepairAttempted.Eliminated : dr == 1 ? RepairAttempted.Repaired : RepairAttempted.NoChange;\n",
        "            var outcome = ScenarioA1RallyRules.VehicleMgRepairResult(dr);\n"
        "            var result = RepairResult(outcome);\n",
    ),
    (
        "            if (result != RepairAttempted.NoChange)\n"
        "            {\n"
        "                var changed = result == RepairAttempted.Repaired\n"
        "                    ? new Dictionary<string, ConditionState> { [Conditions.Malfunctioned] = ConditionState.False }\n"
        "                    : new Dictionary<string, ConditionState> { [Conditions.Disabled] = ConditionState.True };\n",
        "            if (outcome != RepairOutcome.NoChange)\n"
        "            {\n"
        "                var (condition, value) = ScenarioA1RallyRules.VehicleRepairChange(outcome);\n"
        "                var changed = new Dictionary<string, ConditionState> { [ConditionName(condition)] = value ? ConditionState.True : ConditionState.False };\n",
    ),
    (
        "            [$\"play.repair: {vehicle.Id}'s crew attempts to repair its AAMG (a dr of 1 repairs it, a 6 disables it; D3.7)\"])\n"
        "        {\n"
        "            Roll = new PlannedRoll(\"repair\", Build),\n"
        "            FirstEventId = EventId(attemptId, 1),\n"
        "        };\n"
        "    }\n"
        "}\n",
        "            [ScenarioA1RallyRules.VehicleRepairSummary(vehicle.Id)])\n"
        "        {\n"
        "            Roll = new PlannedRoll(\"repair\", Build),\n"
        "            FirstEventId = EventId(attemptId, 1),\n"
        "        };\n"
        "    }\n"
        "\n"
        "    /// <summary>A Repair outcome of Rules as the record names it.</summary>\n"
        "    private static string RepairResult(RepairOutcome outcome) => outcome switch\n"
        "    {\n"
        "        RepairOutcome.Eliminated => RepairAttempted.Eliminated,\n"
        "        RepairOutcome.Repaired => RepairAttempted.Repaired,\n"
        "        _ => RepairAttempted.NoChange,\n"
        "    };\n"
        "\n"
        "    /// <summary>A condition's state as a three-valued fact for Rules.</summary>\n"
        "    private static bool? RuleBool(ConditionState state) => state switch\n"
        "    {\n"
        "        ConditionState.True => true,\n"
        "        ConditionState.False => false,\n"
        "        _ => null,\n"
        "    };\n"
        "}\n",
    ),
])

apply(SHOCK, [
    (
        "    private static IEnumerable<UnitInstance> ShockRollsOwed(GameState state) => state.Phase != \"rph\" ? [] : state.Units\n"
        "        .Where(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit) && (Is(unit, Conditions.Shocked) || Is(unit, Conditions.UnconfirmedKill))\n"
        "            && !state.ShockRollsThisPhase.Contains(unit.Id, StringComparer.Ordinal))\n",
        "    private static IEnumerable<UnitInstance> ShockRollsOwed(GameState state) => !ScenarioA1RallyRules.ShockRollPhase(state.Phase) ? [] : state.Units\n"
        "        .Where(unit => ScenarioA1RallyRules.OwesShockRoll(unit.Status == InstanceStatus.Active, LiveFire.IsVehicle(unit), Is(unit, Conditions.Shocked), Is(unit, Conditions.UnconfirmedKill),\n"
        "            state.ShockRollsThisPhase.Contains(unit.Id, StringComparer.Ordinal)))\n",
    ),
    (
        "        if (state.Phase != \"rph\")\n"
        "        {\n"
        "            return Refused(scope, label, expected, \"play.shock-phase: a Shocked AFV or an Unconfirmed Kill makes its dr in the RPh (C7.42)\");\n"
        "        }\n"
        "\n"
        "        if (state.Unit(vehicleId) is not { Status: InstanceStatus.Active } vehicle || !ShockRollsOwed(state).Any(item => item.Id == vehicle.Id))\n"
        "        {\n"
        "            return Refused(scope, label, expected, $\"play.shock-vehicle: '{vehicleId}' is not a Shocked AFV or an Unconfirmed Kill that still owes its dr this RPh (C7.42)\");\n"
        "        }\n",
        "        if (ScenarioA1RallyRules.ShockPhaseBar(state.Phase) is { } phaseBar)\n"
        "        {\n"
        "            return Refused(scope, label, expected, phaseBar);\n"
        "        }\n"
        "\n"
        "        if (state.Unit(vehicleId) is not { Status: InstanceStatus.Active } vehicle || !ShockRollsOwed(state).Any(item => item.Id == vehicle.Id))\n"
        "        {\n"
        "            return Refused(scope, label, expected, ScenarioA1RallyRules.ShockVehicleText(vehicleId));\n"
        "        }\n",
    ),
    (
        "            if (result == ShockRecoveryRolled.Wrecked)\n",
        "            if (ScenarioA1RallyRules.ShockRecoveryWrecks(result))\n",
    ),
    (
        "                events.Add(Event(scope, attemptId, 3, expected, \"conditions-changed\", new ConditionsChanged(vehicle.Id, new Dictionary<string, ConditionState>(StringComparer.Ordinal)\n"
        "                {\n"
        "                    [Conditions.Shocked] = ConditionState.False,\n"
        "                    [Conditions.UnconfirmedKill] = result == ShockRecoveryRolled.UnconfirmedKill ? ConditionState.True : ConditionState.False,\n"
        "                }), package, null, [record]));\n",
        "                events.Add(Event(scope, attemptId, 3, expected, \"conditions-changed\", new ConditionsChanged(vehicle.Id, ConditionChanges(ScenarioA1RallyRules.ShockRecoveryConditions(result))),\n"
        "                    package, null, [record]));\n",
    ),
    (
        "            [unconfirmed\n"
        "                ? $\"play.shock-recovery: {vehicle.Id} is an Unconfirmed Kill: a dr of 1 to 3 removes it, 4 to 6 wrecks the AFV (C7.42)\"\n"
        "                : $\"play.shock-recovery: {vehicle.Id} is Shocked: a dr of 1 or 2 removes the Shock, 3 to 6 makes it an Unconfirmed Kill (C7.42)\"])\n",
        "            [ScenarioA1RallyRules.ShockRecoverySummary(vehicle.Id, unconfirmed)])\n",
    ),
])
