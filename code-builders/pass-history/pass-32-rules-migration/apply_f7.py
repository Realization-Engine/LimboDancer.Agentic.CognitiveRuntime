"""Pass 32.f commit 7: the projector's Rally, Repair, ShockRecovery, Rout, RoutHalfMf, Interdict, Surrender, RejectSurrender, and Massacre call Rules."""
import pathlib

P = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL\LimboDancer.Domains.Asl.Units\State\GameProjector.cs")
PROJ = "Rules.ScenarioA1RoutRallyProjection"
RULES = "Rules.ScenarioA1RallyRules"


def apply(p, edits):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:80])
        t = t.replace(old, new)
    p.write_bytes(t.encode("utf-8"))
    print("applied", p.name, len(edits))


apply(P, [
    # Rally
    (
        '            if (state.Phase != "rph" || Active(state, rally.Unit) is not UnitInstance unit)\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-027", "A Rally attempt is made in the RPh by an active unit (A10.6).");\n'
        '            }\n'
        '\n'
        '            if (state.RallyAttemptsThisPlayerTurn.Contains(unit.Id) || state.RepairsThisPhase.Contains(unit.Id))\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-027", $"\'{unit.Id}\' already attempted to rally this Player Turn, or to repair this RPh (A10.6, A3.1).");\n'
        '            }\n',
        f'            if (!{PROJ}.RallyPhase(state.Phase) || Active(state, rally.Unit) is not UnitInstance unit)\n'
        '            {\n'
        f'                var refused = {PROJ}.RallyRefusal();\n'
        '                return Fail<GameState>(refused.Code, refused.Text);\n'
        '            }\n'
        '\n'
        f'            if ({PROJ}.VerifyRallyOnce(unit.Id, state.RallyAttemptsThisPlayerTurn.Contains(unit.Id), state.RepairsThisPhase.Contains(unit.Id)) is {{ }} once)\n'
        '            {\n'
        '                return Fail<GameState>(once.Code, once.Text);\n'
        '            }\n',
    ),
    (
        '            var mmc = vocabulary.IsA(unit.Kind, "asl:mmc") && unit.Side == state.PhasingSide && !state.FirstMmcRallyTaken.Contains(unit.Side);\n',
        f'            var mmc = {PROJ}.TakesFirstMmcRally(vocabulary.IsA(unit.Kind, "asl:mmc"), unit.Side == state.PhasingSide, state.FirstMmcRallyTaken.Contains(unit.Side));\n',
    ),
    # ShockRecovery
    (
        '            if (state.Phase != "rph" || Active(state, shock.Vehicle) is not UnitInstance vehicle || !vocabulary.IsA(vehicle.Kind, "asl:vehicle")\n'
        '                || state.ShockRollsThisPhase.Contains(vehicle.Id)\n'
        '                || (GameState.Condition(vehicle, Conditions.Shocked) != ConditionState.True && GameState.Condition(vehicle, Conditions.UnconfirmedKill) != ConditionState.True))\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-038", "A Shocked AFV or an Unconfirmed Kill makes one dr in the RPh (C7.42).");\n'
        '            }\n',
        f'            if (!{RULES}.ShockRollPhase(state.Phase) || Active(state, shock.Vehicle) is not UnitInstance vehicle\n'
        f'                || !{PROJ}.ShockRecoveryAllowed(vocabulary.IsA(vehicle.Kind, "asl:vehicle"), state.ShockRollsThisPhase.Contains(vehicle.Id),\n'
        '                    GameState.Condition(vehicle, Conditions.Shocked) == ConditionState.True, GameState.Condition(vehicle, Conditions.UnconfirmedKill) == ConditionState.True))\n'
        '            {\n'
        f'                var refused = {PROJ}.ShockRecoveryRefusal();\n'
        '                return Fail<GameState>(refused.Code, refused.Text);\n'
        '            }\n',
    ),
    (
        '            if (shock.Result != ShockRecoveryRolled.For(unconfirmed, roll.Values[0]))\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-038", "The Shock recovery record disagrees with its dr (C7.42).");\n'
        '            }\n',
        f'            if (!{PROJ}.ShockRecoveryAgrees(shock.Result, unconfirmed, roll.Values[0]))\n'
        '            {\n'
        f'                var disagrees = {PROJ}.ShockRecoveryDisagrees();\n'
        '                return Fail<GameState>(disagrees.Code, disagrees.Text);\n'
        '            }\n',
    ),
    # Repair, vehicle
    (
        '                if (state.Phase != "rph" || GameState.Condition(vehicle, Conditions.Malfunctioned) != ConditionState.True\n'
        '                    || GameState.Condition(vehicle, Conditions.Disabled) == ConditionState.True || GameState.Condition(vehicle, Conditions.ButtonedUp) == ConditionState.True\n'
        '                    || GameState.Condition(vehicle, Conditions.Stunned) == ConditionState.True || GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True\n'
        '                    || GameState.Condition(vehicle, Conditions.Shocked) == ConditionState.True || GameState.Condition(vehicle, Conditions.UnconfirmedKill) == ConditionState.True\n'
        '                    || state.RepairsThisPhase.Contains(vehicle.Id))\n'
        '                {\n'
        '                    return Fail<GameState>("UNIT-STATE-028", "A vehicle\'s malfunctioned MG is repaired once per RPh by its CE crew that is not Stunned or Recalled (D3.7).");\n'
        '                }\n',
        f'                if (!{PROJ}.VehicleRepairAllowedAsRecorded(state.Phase, GameState.Condition(vehicle, Conditions.Malfunctioned) == ConditionState.True,\n'
        '                    GameState.Condition(vehicle, Conditions.Disabled) == ConditionState.True, GameState.Condition(vehicle, Conditions.ButtonedUp) == ConditionState.True,\n'
        '                    GameState.Condition(vehicle, Conditions.Stunned) == ConditionState.True, GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True,\n'
        '                    GameState.Condition(vehicle, Conditions.Shocked) == ConditionState.True, GameState.Condition(vehicle, Conditions.UnconfirmedKill) == ConditionState.True,\n'
        '                    state.RepairsThisPhase.Contains(vehicle.Id)))\n'
        '                {\n'
        f'                    var refused = {PROJ}.VehicleRepairRefusal();\n'
        '                    return Fail<GameState>(refused.Code, refused.Text);\n'
        '                }\n',
    ),
    (
        '                var vehicleResult = vehicleDr == 6 ? RepairAttempted.Eliminated : vehicleDr == 1 ? RepairAttempted.Repaired : RepairAttempted.NoChange;\n'
        '                if (repair.RepairNumber != 1 || repair.Result != vehicleResult)\n'
        '                {\n'
        '                    return Fail<GameState>("UNIT-STATE-028", "The Repair record disagrees with the vehicle MG\'s dr (D3.7).");\n'
        '                }\n',
        f'                var vehicleResult = RepairResult({RULES}.VehicleMgRepairResult(vehicleDr));\n'
        f'                if (!{PROJ}.VehicleRepairRecordAgrees(repair.RepairNumber, repair.Result == vehicleResult))\n'
        '                {\n'
        f'                    var disagrees = {PROJ}.VehicleRepairDisagrees();\n'
        '                    return Fail<GameState>(disagrees.Code, disagrees.Text);\n'
        '                }\n',
    ),
    # Repair, SW
    (
        '            if (state.Phase != "rph" || Active(state, repair.Unit) is not UnitInstance unit\n'
        '                || Active(state, repair.Equipment) is not EquipmentInstance { Holding: { Role: HoldingRole.Possessed } holding } equipment\n'
        '                || holding.Holder != unit.Id || GameState.Condition(equipment, Conditions.Malfunctioned) != ConditionState.True)\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-028", "A Repair is attempted in the RPh on a malfunctioned SW its unit possesses (A9.72).");\n'
        '            }\n'
        '\n'
        '            // A3.1: a unit takes one kind of action in the RPh, so one that attempted to rally does not also repair.\n'
        '            if (state.RallyAttemptsThisPlayerTurn.Contains(unit.Id))\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-028", $"\'{unit.Id}\' attempted to rally this RPh, so it may not also repair (A3.1).");\n'
        '            }\n',
        f'            if (!{PROJ}.SwRepairPhase(state.Phase) || Active(state, repair.Unit) is not UnitInstance unit\n'
        '                || Active(state, repair.Equipment) is not EquipmentInstance { Holding: { Role: HoldingRole.Possessed } holding } equipment\n'
        f'                || !{RULES}.SwRepairWeaponAllowed(holding.Holder == unit.Id, GameState.Condition(equipment, Conditions.Malfunctioned) == ConditionState.True))\n'
        '            {\n'
        f'                var refused = {PROJ}.SwRepairRefusal();\n'
        '                return Fail<GameState>(refused.Code, refused.Text);\n'
        '            }\n'
        '\n'
        '            // A3.1: a unit takes one kind of action in the RPh, so one that attempted to rally does not also repair.\n'
        f'            if ({PROJ}.VerifySwRepairOnce(unit.Id, state.RallyAttemptsThisPlayerTurn.Contains(unit.Id)) is {{ }} once)\n'
        '            {\n'
        '                return Fail<GameState>(once.Code, once.Text);\n'
        '            }\n',
    ),
    (
        '            var expected = dr == 6 ? RepairAttempted.Eliminated : dr <= repair.RepairNumber ? RepairAttempted.Repaired : RepairAttempted.NoChange;\n'
        '            if (printed != repair.RepairNumber || repair.Result != expected)\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-028", "The Repair record disagrees with the SW\'s Repair Number or its dr (A9.72).");\n'
        '            }\n',
        f'            var expected = RepairResult({RULES}.SwRepairResult(dr, repair.RepairNumber));\n'
        f'            if (!{PROJ}.SwRepairRecordAgrees(printed, repair.RepairNumber, repair.Result == expected))\n'
        '            {\n'
        f'                var disagrees = {PROJ}.SwRepairDisagrees();\n'
        '                return Fail<GameState>(disagrees.Code, disagrees.Text);\n'
        '            }\n',
    ),
    # Rout
    (
        '            if (state.Phase != "rtph" || Active(state, routed.Unit) is not UnitInstance unit || GameState.Condition(unit, Conditions.Broken) != ConditionState.True\n'
        '                || GameState.Condition(unit, Conditions.Melee) == ConditionState.True || GameState.Condition(unit, Conditions.Pinned) == ConditionState.True\n'
        '                || routed.HalfMf < 0 || (!routed.LowCrawl && (unit.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0) + routed.HalfMf > RoutHalfMf(unit))\n'
        '                || (routed.LowCrawl && state.RoutedThisPhase.Contains(unit.Id, StringComparer.Ordinal)))\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-041", "A rout step moves a broken unit not in Melee or pinned in the RtPh, within its MF (A10.5, A10.52).");\n'
        '            }\n'
        '\n'
        '            var spent = unit.MfSpent * 2 + (unit.HalfMfSpent ? 1 : 0) + routed.HalfMf;\n'
        '            return Replace(state, unit with\n'
        '            {\n'
        '                Position = new MapPosition(routed.To),\n'
        '                MfSpent = spent / 2,\n'
        '                HalfMfSpent = spent % 2 == 1,\n'
        '            })',
        f'            if (!{PROJ}.RoutPhase(state.Phase) || Active(state, routed.Unit) is not UnitInstance unit\n'
        f'                || !{PROJ}.RoutStepAllowed(GameState.Condition(unit, Conditions.Broken) == ConditionState.True, GameState.Condition(unit, Conditions.Melee) == ConditionState.True,\n'
        '                    GameState.Condition(unit, Conditions.Pinned) == ConditionState.True, routed.HalfMf, routed.LowCrawl, unit.MfSpent, unit.HalfMfSpent, RoutHalfMf(unit),\n'
        '                    state.RoutedThisPhase.Contains(unit.Id, StringComparer.Ordinal)))\n'
        '            {\n'
        f'                var refused = {PROJ}.RoutStepRefusal();\n'
        '                return Fail<GameState>(refused.Code, refused.Text);\n'
        '            }\n'
        '\n'
        f'            var (mfSpent, halfMfSpent) = {PROJ}.RoutMfSpent(unit.MfSpent, unit.HalfMfSpent, routed.HalfMf);\n'
        '            return Replace(state, unit with\n'
        '            {\n'
        '                Position = new MapPosition(routed.To),\n'
        '                MfSpent = mfSpent,\n'
        '                HalfMfSpent = halfMfSpent,\n'
        '            })',
    ),
    (
        '        private static int RoutHalfMf(UnitInstance unit) =>\n'
        '            unit.Kind is "asl:leader" or "asl:hero" && GameState.Condition(unit, Conditions.Wounded) == ConditionState.True ? 6 : 12;\n',
        '        private static int RoutHalfMf(UnitInstance unit) =>\n'
        f'            {PROJ}.RoutHalfMfAsRecorded(unit.Kind is "asl:leader" or "asl:hero", GameState.Condition(unit, Conditions.Wounded) == ConditionState.True);\n'
        '\n'
        '        /// <summary>A Repair outcome of Rules as the record names it.</summary>\n'
        '        private static string RepairResult(Rules.RepairOutcome outcome) => outcome switch\n'
        '        {\n'
        '            Rules.RepairOutcome.Eliminated => RepairAttempted.Eliminated,\n'
        '            Rules.RepairOutcome.Repaired => RepairAttempted.Repaired,\n'
        '            _ => RepairAttempted.NoChange,\n'
        '        };\n',
    ),
    # Interdict
    (
        '            if (state.Phase != "rtph" || Active(state, interdicted.Unit) is not UnitInstance || !state.RoutedThisPhase.Contains(interdicted.Unit, StringComparer.Ordinal)\n'
        '                || !rolls.TryGetValue(interdicted.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6\n'
        '                || RoutInterdicted.For(roll.Values[0] + roll.Values[1], roll.Values[0] + roll.Values[1] + interdicted.Drm, interdicted.Morale) != interdicted.Result)\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-041", "An Interdiction NMC is taken by a routing unit, and its result agrees with its DR (A10.53).");\n'
        '            }\n',
        f'            if (!{PROJ}.RoutPhase(state.Phase) || Active(state, interdicted.Unit) is not UnitInstance\n'
        f'                || !{PROJ}.InterdictionByRoutingUnit(state.RoutedThisPhase.Contains(interdicted.Unit, StringComparer.Ordinal))\n'
        '                || !rolls.TryGetValue(interdicted.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6\n'
        f'                || !{PROJ}.InterdictionAgrees(roll.Values[0], roll.Values[1], interdicted.Drm, interdicted.Morale, interdicted.Result))\n'
        '            {\n'
        f'                var refused = {PROJ}.InterdictionRefusal();\n'
        '                return Fail<GameState>(refused.Code, refused.Text);\n'
        '            }\n',
    ),
    # Surrender
    (
        '            if (Active(state, surrender.Unit) is not UnitInstance unit || GameState.Condition(unit, Conditions.Broken) != ConditionState.True\n'
        '                || GameState.Condition(unit, Conditions.Captured) == ConditionState.True || surrender.Captors.Count == 0\n'
        '                || surrender.Captors.Any(id => state.Unit(id) is not { Status: InstanceStatus.Active } captor || captor.Side == unit.Side)\n'
        '                || state.PendingSurrenders.Any(item => item.Unit == unit.Id))\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-031", "A surrender names a broken unit and the enemy units it may surrender to (A15.5).");\n'
        '            }\n',
        f'            if (Active(state, surrender.Unit) is not UnitInstance unit || !{PROJ}.SurrenderAllowed(GameState.Condition(unit, Conditions.Broken) == ConditionState.True,\n'
        '                GameState.Condition(unit, Conditions.Captured) == ConditionState.True, surrender.Captors.Count,\n'
        '                surrender.Captors.Select(id => state.Unit(id) is { Status: InstanceStatus.Active } captor ? (true, captor.Side != unit.Side) : (false, false)),\n'
        '                () => state.PendingSurrenders.Any(item => item.Unit == unit.Id)))\n'
        '            {\n'
        f'                var refused = {PROJ}.SurrenderRefusal();\n'
        '                return Fail<GameState>(refused.Code, refused.Text);\n'
        '            }\n',
    ),
    # RejectSurrender
    (
        '                return Fail<GameState>("UNIT-STATE-031", $"\'{rejected.Unit}\' has no pending surrender to reject (A20.3).");\n',
        f'                var refused = {PROJ}.RejectSurrenderRefusal(rejected.Unit);\n'
        '                return Fail<GameState>(refused.Code, refused.Text);\n',
    ),
    (
        '                NoQuarter = next.NoQuarter.Contains(unit.Side, StringComparer.Ordinal) ? next.NoQuarter : [.. next.NoQuarter, unit.Side],\n',
        f'                NoQuarter = {PROJ}.NoQuarterAfter(next.NoQuarter, unit.Side),\n',
    ),
    # Massacre
    (
        '            if (units.Length == 0 || prisoners.Length == 0 || units.Any(unit => unit is not { Status: InstanceStatus.Active })\n'
        '                || prisoners.Any(unit => unit is not { Status: InstanceStatus.Active }))\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-036", "A Massacre names active units and the active prisoners they eliminate (A20.4).");\n'
        '            }\n',
        f'            if (!{PROJ}.MassacreNamesActive(units.Length, prisoners.Length, units.All(unit => unit is {{ Status: InstanceStatus.Active }}),\n'
        '                prisoners.All(unit => unit is { Status: InstanceStatus.Active })))\n'
        '            {\n'
        f'                var refused = {PROJ}.MassacreNamesRefusal();\n'
        '                return Fail<GameState>(refused.Code, refused.Text);\n'
        '            }\n',
    ),
    (
        '            var firePhase = state.Phase is "pfph" or "afph" ? state.PhasingSide == side : state.Phase == "dfph" && state.PhasingSide != side;\n',
        f'            var firePhase = {PROJ}.MassacreFirePhase(state.Phase, state.PhasingSide == side);\n',
    ),
    (
        '            if (!firePhase || at is null\n'
        '                || units.Any(unit => unit!.Side != side || state.Location(unit.Id)?.Location != at || !vocabulary.IsA(unit.Kind, "asl:personnel")\n'
        '                    || GameState.Condition(unit, Conditions.Melee) == ConditionState.True || GameState.Condition(unit, Conditions.Captured) == ConditionState.True\n'
        '                    || (massacre.Berserk ? GameState.Condition(unit, Conditions.Berserk) != ConditionState.True\n'
        '                        : GameState.Condition(unit, Conditions.Berserk) != ConditionState.True && Nationality(unit) != "russian"))\n'
        '                || prisoners.Any(unit => unit!.Side == side || GameState.Condition(unit, Conditions.Captured) != ConditionState.True || state.Location(unit.Id)?.Location != at))\n'
        '            {\n'
        '                return Fail<GameState>("UNIT-STATE-036",\n'
        '                    "Only Russian or berserk Infantry not in Melee massacre the prisoners in their Location, in a fire phase of their own side (A20.4).");\n'
        '            }\n',
        '            if (!firePhase || at is null\n'
        f'                || units.Any(unit => !{PROJ}.MassacreUnitAllowed(unit!.Side == side, state.Location(unit.Id)?.Location == at, vocabulary.IsA(unit.Kind, "asl:personnel"),\n'
        '                    GameState.Condition(unit, Conditions.Melee) == ConditionState.True, GameState.Condition(unit, Conditions.Captured) == ConditionState.True, massacre.Berserk,\n'
        '                    GameState.Condition(unit, Conditions.Berserk) == ConditionState.True, () => Nationality(unit)))\n'
        f'                || prisoners.Any(unit => !{PROJ}.MassacrePrisonerAllowed(unit!.Side == side, GameState.Condition(unit, Conditions.Captured) == ConditionState.True,\n'
        '                    state.Location(unit.Id)?.Location == at)))\n'
        '            {\n'
        f'                var refused = {PROJ}.MassacreRefusal();\n'
        '                return Fail<GameState>(refused.Code, refused.Text);\n'
        '            }\n',
    ),
    (
        '            if (massacre.Berserk)\n'
        '            {\n'
        '                foreach (var unit in units)\n',
        f'            if ({PROJ}.MassacreEndsBerserk(massacre.Berserk))\n'
        '            {\n'
        '                foreach (var unit in units)\n',
    ),
    (
        '            var raise = !next!.MassacreElrRaised.Contains(victims, StringComparer.Ordinal);\n'
        '            return next with\n'
        '            {\n'
        '                NoQuarter = next.NoQuarter.Contains(victims, StringComparer.Ordinal) ? next.NoQuarter : [.. next.NoQuarter, victims],\n',
        f'            var raise = {PROJ}.MassacreRaisesElr(next!.MassacreElrRaised.Contains(victims, StringComparer.Ordinal));\n'
        '            return next with\n'
        '            {\n'
        f'                NoQuarter = {PROJ}.NoQuarterAfter(next.NoQuarter, victims),\n',
    ),
    (
        '                    Elr = item.Elr is { } elr ? Math.Min(elr + 1, 6) : null,\n'
        '                    Groups = [.. item.Groups.Select(group => group.Elr is { } groupElr ? group with { Elr = Math.Min(groupElr + 1, 6) } : group)],\n',
        f'                    Elr = {PROJ}.MassacreRaisedElr(item.Elr),\n'
        f'                    Groups = [.. item.Groups.Select(group => group.Elr is {{ }} groupElr ? group with {{ Elr = {PROJ}.MassacreRaisedElr(groupElr) }} : group)],\n',
    ),
])
