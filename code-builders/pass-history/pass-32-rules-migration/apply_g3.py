"""Pass 32.g commit 3: CC effects, capture, the surrender's captor, Known enemies, captors, the Ambush Withdrawal, and a Guard's prisoners call Rules."""
import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
CC = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.CloseCombat.cs"
PR = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.Prisoners.cs"
FIRE = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs"
MODELS = ROOT / "LimboDancer.Domains.Asl.Rules/ScenarioA1FireRuleModels.cs"
C = "ScenarioA1PrisonerCalculator"


def apply(p, edits):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:80])
        t = t.replace(old, new)
    p.write_bytes(t.encode("utf-8"))
    print("applied", p.name, len(edits))


apply(MODELS, [("    Abandoned,\n    Disabled,\n}\n", "    Abandoned,\n    Disabled,\n    Captured,\n    Melee,\n    Unarmed,\n}\n")])
apply(FIRE, [(
    "        UnitCondition.Disabled => Conditions.Disabled,\n",
    "        UnitCondition.Disabled => Conditions.Disabled,\n        UnitCondition.Captured => Conditions.Captured,\n        UnitCondition.Melee => Conditions.Melee,\n        UnitCondition.Unarmed => Conditions.Unarmed,\n",
)])

apply(CC, [
    (
        '            var id = $"{attemptId}-leader-{leader.Attack.ToString(CultureInfo.InvariantCulture)}";\n'
        '            var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal)\n'
        '            {\n'
        '                [Conditions.Broken] = ConditionState.False,\n'
        '                [Conditions.Pinned] = ConditionState.False,\n'
        '                [Conditions.Wounded] = ConditionState.False,\n'
        '                [Conditions.Concealed] = ConditionState.False,\n'
        '                [Conditions.Hidden] = ConditionState.False,\n'
        '\n'
        '                // Pass 31 (play test P-26): a created leader is neither berserk nor a prisoner, so his Good Order is known and he may direct fire; the\n'
        '                // Melee of his Location is recorded for him with its other units.\n'
        '                [Conditions.Berserk] = ConditionState.False,\n'
        '                [Conditions.Captured] = ConditionState.False,\n'
        '                [Conditions.Melee] = ConditionState.False,\n'
        '            };\n'
        '            if (state.Unit(leader.StackedWith) is { } mmc && GameState.Condition(mmc, Conditions.Fanatic) == ConditionState.True)\n'
        '            {\n'
        '                conditions[Conditions.Fanatic] = ConditionState.True;\n'
        '            }\n'
        '\n'
        '            // A11.22, A18.12 (ruling R14.8): a leader created by an attack whose MMC infiltrates goes with it.\n'
        '            var placedAt = resolution.Effects.FirstOrDefault(item => item.UnitId == leader.StackedWith)?.InfiltratedTo is { } gone ? BoardLocation.Parse(gone) : location;\n',
        f'            var id = {C}.CreatedLeaderId(attemptId, leader.Attack);\n'
        '            // Pass 31 (play test P-26): a created leader is neither berserk nor a prisoner, so his Good Order is known and he may direct fire; the\n'
        '            // Melee of his Location is recorded for him with its other units. Rules gives the conditions in the record\'s order (pass 32.g).\n'
        f'            var conditions = ConditionChanges({C}.CreatedCcLeaderConditions(state.Unit(leader.StackedWith) is {{ }} mmc && GameState.Condition(mmc, Conditions.Fanatic) == ConditionState.True));\n'
        '\n'
        '            // A11.22, A18.12 (ruling R14.8): a leader created by an attack whose MMC infiltrates goes with it.\n'
        f'            var placedAt = BoardLocation.Parse({C}.CreatedLeaderPlacedAt(resolution.Effects.FirstOrDefault(item => item.UnitId == leader.StackedWith)?.InfiltratedTo, location.ToString()));\n',
    ),
    (
        '            var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);\n'
        '            if (effect.Armed == true)\n'
        '            {\n'
        '                conditions[Conditions.Unarmed] = ConditionState.False;\n'
        '            }\n'
        '\n'
        '            // A11.19, A12.14 (ruling R14.2): the unit loses its "?".\n'
        '            if (effect.ConcealmentLost == true)\n'
        '            {\n'
        '                conditions[Conditions.Concealed] = ConditionState.False;\n'
        '                conditions[Conditions.Hidden] = ConditionState.False;\n'
        '            }\n'
        '\n',
        '            // A20.551, A11.19, A12.14 (rulings R14.2, R14.6): Armed again and the "?" lost, in the record\'s order (pass 32.g).\n'
        f'            var conditions = ConditionChanges({C}.RoundConditions(effect.Armed == true, effect.ConcealmentLost == true));\n'
        '\n',
    ),
    (
        '                armed[Conditions.Unarmed] = ConditionState.False;\n'
        '\n'
        '                // Table player, pass 14: a rearmed prisoner is no longer captured.\n'
        '                if (effect.Escaped == true)\n'
        '                {\n'
        '                    armed[Conditions.Captured] = ConditionState.False;\n'
        '                }\n'
        '                yield return ("lineage", new LineageRecorded(LineageAction.Replaced, [unit.Id],\n',
        '                // Table player, pass 14: a rearmed prisoner is no longer captured.\n'
        f'                foreach (var (condition, value) in {C}.RearmedConditions(effect.Escaped == true))\n'
        '                {\n'
        '                    armed[ConditionName(condition)] = value ? ConditionState.True : ConditionState.False;\n'
        '                }\n'
        '\n'
        '                yield return ("lineage", new LineageRecorded(LineageAction.Replaced, [unit.Id],\n',
    ),
    (
        '            if (effect.Wounded && GameState.Condition(unit, Conditions.Wounded) != ConditionState.True)\n'
        '            {\n'
        '                conditions[Conditions.Wounded] = ConditionState.True;\n'
        '            }\n'
        '\n'
        '            if (effect.BerserkEnded == true)\n'
        '            {\n'
        '                conditions[Conditions.Berserk] = ConditionState.False;\n'
        '            }\n'
        '\n'
        '            if (effect.FinalDefinitionId != effect.DefinitionId)\n',
        f'            foreach (var (condition, value) in {C}.AfterRoundConditions(effect.Wounded, GameState.Condition(unit, Conditions.Wounded) == ConditionState.True, effect.BerserkEnded == true))\n'
        '            {\n'
        '                conditions[ConditionName(condition)] = value ? ConditionState.True : ConditionState.False;\n'
        '            }\n'
        '\n'
        f'            if ({C}.Reduced(effect.DefinitionId, effect.FinalDefinitionId))\n',
    ),
    (
        '        foreach (var effect in resolution.Effects.Where(item => (item.WithdrewTo ?? item.InfiltratedTo) is not null && item.RearmedAs is null))\n'
        '        {\n'
        '            yield return ("instance-moved", new InstanceMoved(effect.UnitId, new MapPosition(BoardLocation.Parse((effect.WithdrewTo ?? effect.InfiltratedTo)!))));\n',
        f'        foreach (var (effect, moved) in resolution.Effects.Select(item => (item, {C}.MovesAfterRound(item.WithdrewTo, item.InfiltratedTo, item.RearmedAs))).Where(pair => pair.Item2 is not null))\n'
        '        {\n'
        '            yield return ("instance-moved", new InstanceMoved(effect.UnitId, new MapPosition(BoardLocation.Parse(moved!))));\n',
    ),
    # CaptureEffects
    (
        '        foreach (var weapon in effect.CapturedHalf == true ? Enumerable.Empty<EquipmentInstance>()\n'
        '            : state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == unit.Id).OrderBy(item => item.Id, StringComparer.Ordinal))\n',
        f'        foreach (var weapon in !{C}.CaptiveDropsWeapons(effect.CapturedHalf) ? Enumerable.Empty<EquipmentInstance>()\n'
        '            : state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == unit.Id).OrderBy(item => item.Id, StringComparer.Ordinal))\n',
    ),
    (
        '            captive = $"{attemptId}-{unit.Id}-a";\n'
        '            var free = $"{attemptId}-{unit.Id}-b";\n',
        f'            var (captiveHalf, free) = {C}.CapturedHalfIds(attemptId, unit.Id);\n'
        '            captive = captiveHalf;\n',
    ),
    (
        '        yield return ("conditions-changed", new ConditionsChanged(captive, new Dictionary<string, ConditionState>\n'
        '        {\n'
        '            [Conditions.Broken] = ConditionState.False,\n'
        '            [Conditions.Disrupted] = ConditionState.False,\n'
        '            [Conditions.Pinned] = ConditionState.False,\n'
        '            [Conditions.DesperationMorale] = ConditionState.False,\n'
        '            [Conditions.Concealed] = ConditionState.False,\n'
        '            [Conditions.Hidden] = ConditionState.False,\n'
        '            [Conditions.Melee] = ConditionState.False,\n'
        '            [Conditions.Unarmed] = ConditionState.True,\n'
        '        }));\n',
        '        var captured = new Dictionary<string, ConditionState>();\n'
        f'        foreach (var (condition, value) in {C}.CapturedConditions())\n'
        '        {\n'
        '            captured[ConditionName(condition)] = value ? ConditionState.True : ConditionState.False;\n'
        '        }\n'
        '\n'
        '        yield return ("conditions-changed", new ConditionsChanged(captive, captured));\n',
    ),
    # PlanTakePrisoner
    (
        '            return Refused(scope, label, expected, $"play.no-surrender: {unitId} has not surrendered");\n'
        '        }\n'
        '\n'
        '        // A20.21, A20.51 (ruling R14.5): the unit goes to a captor with Guard capacity; when none has any, it is freed as Unarmed.\n'
        '        var able = pending.Captors.Where(id => state.Unit(id) is { Status: InstanceStatus.Active } captor && state.Unit(unitId) is { } surrendered\n'
        '            && GuardLoad(state, captor) + UnitSize(surrendered) <= 5 * UnitSize(captor)).ToArray();\n'
        '        var free = arguments.TryGetProperty("free", out var freeing) && freeing.ValueKind == JsonValueKind.True;\n'
        '        if (free || (!reject && able.Length > 0 && !able.Contains(captorId, StringComparer.Ordinal) && pending.Captors.Contains(captorId, StringComparer.Ordinal)))\n'
        '        {\n'
        '            if (able.Length > 0)\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.captor-capacity: {string.Join(", ", able)} can guard {unitId}, so it is taken by one of them (A20.21, A20.51)");\n'
        '            }\n'
        '\n'
        '            if (state.Unit(unitId) is not { } loose || state.Location(loose.Id) is not { } looseAt)\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.no-surrender: {unitId} has not surrendered");\n'
        '            }\n',
        f'            return Refused(scope, label, expected, {C}.NoSurrenderText(unitId));\n'
        '        }\n'
        '\n'
        '        // A20.21, A20.51 (ruling R14.5): the unit goes to a captor with Guard capacity; when none has any, it is freed as Unarmed.\n'
        '        var able = pending.Captors.Where(id => state.Unit(id) is { Status: InstanceStatus.Active } captor && state.Unit(unitId) is { } surrendered\n'
        f'            && {C}.CanGuard(GuardLoad(state, captor), UnitSize(surrendered), UnitSize(captor))).ToArray();\n'
        '        var free = arguments.TryGetProperty("free", out var freeing) && freeing.ValueKind == JsonValueKind.True;\n'
        f'        if ({C}.FreesOrRedirects(free, reject, able.Length > 0, able.Contains(captorId, StringComparer.Ordinal), pending.Captors.Contains(captorId, StringComparer.Ordinal)))\n'
        '        {\n'
        '            if (able.Length > 0)\n'
        '            {\n'
        f'                return Refused(scope, label, expected, {C}.CaptorCapacityText(able, unitId));\n'
        '            }\n'
        '\n'
        '            if (state.Unit(unitId) is not { } loose || state.Location(loose.Id) is not { } looseAt)\n'
        '            {\n'
        f'                return Refused(scope, label, expected, {C}.NoSurrenderText(unitId));\n'
        '            }\n',
    ),
    (
        '                [$"play.freed-unarmed: no captor can guard {unitId}, so it abandons its SW and is freed as an Unarmed unit (A20.21, A20.51)"]);\n',
        f'                [{C}.FreedUnarmedText(unitId)]);\n',
    ),
    (
        '                [$"play.no-quarter: the surrender of {unitId} is rejected and it is eliminated; the {side} side is faced with No Quarter from now on: its units never surrender (A20.3, A15.5)"]);\n',
        f'                [{C}.NoQuarterText(unitId, side)]);\n',
    ),
    (
        '            return Refused(scope, label, expected, $"play.captor: {unitId} surrenders to one of {string.Join(", ", pending.Captors)} (A15.5)");\n',
        f'            return Refused(scope, label, expected, {C}.CaptorText(unitId, pending.Captors));\n',
    ),
    (
        '        events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(prisoner.Id, new Dictionary<string, ConditionState>\n'
        '        {\n'
        '            [Conditions.Broken] = ConditionState.False,\n'
        '            [Conditions.Disrupted] = ConditionState.False,\n'
        '            [Conditions.Pinned] = ConditionState.False,\n'
        '            [Conditions.DesperationMorale] = ConditionState.False,\n'
        '        }), package, null, [pending.Event]));\n'
        '        events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-captured", new InstanceCaptured(prisoner.Id, captor.Id), package, null, [pending.Event]));\n'
        '        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,\n'
        '            [$"play.capture: {prisoner.Id} surrenders to {captor.Id} and is its prisoner in {guardAt.Location} (A15.5, A20.5)"]);\n',
        '        var taken = new Dictionary<string, ConditionState>();\n'
        f'        foreach (var (condition, value) in {C}.TakenPrisonerConditions())\n'
        '        {\n'
        '            taken[ConditionName(condition)] = value ? ConditionState.True : ConditionState.False;\n'
        '        }\n'
        '\n'
        '        events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(prisoner.Id, taken), package, null, [pending.Event]));\n'
        '        events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-captured", new InstanceCaptured(prisoner.Id, captor.Id), package, null, [pending.Event]));\n'
        '        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,\n'
        f'            [{C}.CaptureSummary(prisoner.Id, captor.Id, guardAt.Location.ToString())]);\n',
    ),
    # KnownEnemyInLos, KnownEnemy, Captors, UnitSize
    (
        '        var locations = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && KnownEnemy(unit))\n'
        '            .Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>().Distinct().ToArray();\n'
        '        if (locations.Contains(at))\n'
        '        {\n'
        '            return true;\n'
        '        }\n'
        '\n'
        '        var unknown = false;\n'
        '        foreach (var location in locations)\n'
        '        {\n'
        '            switch (Los(state, location, at)?.Status)\n'
        '            {\n'
        '                case LosStatus.Clear:\n'
        '                    return true;\n'
        '                case LosStatus.Blocked:\n'
        '                    break;\n'
        '                default:\n'
        '                    unknown = true;\n'
        '                    break;\n'
        '            }\n'
        '        }\n'
        '\n'
        '        return unknown ? null : false;\n',
        '        // Pass 32.g: the Known enemies\' Locations cross as indexes into a table, the LOS read as Rules asks for it, in their order.\n'
        '        var locations = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && KnownEnemy(unit))\n'
        '            .Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>().Distinct().ToList();\n'
        '        var table = locations.Contains(at) ? locations : [.. locations, at];\n'
        f'        return {C}.KnownEnemyInLos([.. Enumerable.Range(0, locations.Count)], table.IndexOf(at), index => Los(state, table[index], at)?.Status switch\n'
        '        {\n'
        '            LosStatus.Clear => true,\n'
        '            LosStatus.Blocked => false,\n'
        '            _ => null,\n'
        '        });\n',
    ),
    (
        '    private static bool KnownEnemy(UnitInstance unit) => unit.Kind != UnitKinds.Dummy && !Is(unit, Conditions.Concealed) && !Is(unit, Conditions.Hidden)\n'
        '        && !Is(unit, Conditions.Captured);\n',
        f'    private static bool KnownEnemy(UnitInstance unit) => {C}.KnownEnemy(unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), Is(unit, Conditions.Captured));\n',
    ),
    (
        '        var adjacent = state.Units.Where(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && other.Definition is not null\n'
        '                && (KnownEnemy(other) || (revealed?.Contains(other.Id) == true && other.Kind != UnitKinds.Dummy && !Is(other, Conditions.Captured)))\n'
        '                && !Is(other, Conditions.Broken) && !Is(other, Conditions.Berserk) && !Is(other, Conditions.Melee) && !Is(other, Conditions.Unarmed)\n'
        '                && (vocabulary.IsA(other.Kind, "asl:mmc") || vocabulary.IsA(other.Kind, "asl:smc"))\n'
        '                && state.Location(other.Id)?.Location is { } there && IsAdjacent(state, there, at)).ToArray();\n'
        '\n'
        '        // A20.21, A20.51 (ruling R14.5): the captors with Guard capacity; with none, every captor, and the captor\'s side frees the unit as Unarmed.\n'
        '        string[] guards = [.. adjacent.Where(other => GuardLoad(state, other) + UnitSize(unit) <= 5 * UnitSize(other)).Select(other => other.Id).Order(StringComparer.Ordinal)];\n'
        '        return guards.Length > 0 ? guards : [.. adjacent.Select(other => other.Id).Order(StringComparer.Ordinal)];\n',
        '        // Pass 32.g: the captor test is Rules\'; the ADJACENCY (a LOS read) is read only for a unit that passes it, as before.\n'
        f'        var adjacent = state.Units.Where(other => {C}.Captor(other.Status == InstanceStatus.Active, other.Side != unit.Side, other.Definition is not null, KnownEnemy(other),\n'
        '                revealed?.Contains(other.Id) == true && other.Kind != UnitKinds.Dummy && !Is(other, Conditions.Captured), Is(other, Conditions.Broken), Is(other, Conditions.Berserk),\n'
        '                Is(other, Conditions.Melee), Is(other, Conditions.Unarmed), vocabulary.IsA(other.Kind, "asl:mmc") || vocabulary.IsA(other.Kind, "asl:smc"))\n'
        '                && state.Location(other.Id)?.Location is { } there && IsAdjacent(state, there, at)).ToArray();\n'
        '\n'
        '        // A20.21, A20.51 (ruling R14.5): the captors with Guard capacity; with none, every captor, and the captor\'s side frees the unit as Unarmed.\n'
        f'        return {C}.Captors([.. adjacent.Select(other => new CaptorFacts(other.Id, true, {C}.CanGuard(GuardLoad(state, other), UnitSize(unit), UnitSize(other))))]);\n',
    ),
    (
        '    private int UnitSize(UnitInstance unit) => vocabulary.IsA(unit.Kind, "asl:squad") ? 3 : vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew") ? 2 : 1;\n',
        f'    private int UnitSize(UnitInstance unit) => {C}.UnitSize(vocabulary.IsA(unit.Kind, "asl:squad"), vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew"));\n',
    ),
])

apply(PR, [
    (
        '        if (state.Phase != "ccph" || state.CloseCombats.FirstOrDefault(item => item.Location == location) is not { Ambusher: { } ambusher } entry\n'
        '            || (entry.Rounds.Count > 0 && !entry.Closed))\n'
        '        {\n'
        '            return Refused(scope, label, expected,\n'
        '                "play.ambush-withdraw-phase: an Ambush Withdrawal is made in the CCPh by the side that ambushed, before the Location\'s first round or once its CC is over (A11.41)");\n'
        '        }\n',
        '        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);\n'
        f'        if ({C}.AmbushWithdrawalPhaseBar(state.Phase, entry?.Ambusher is not null, entry is {{ }} begun && begun.Rounds.Count > 0 && !begun.Closed) is {{ }} phaseBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, phaseBar);\n'
        '        }\n'
        '\n'
        '        var ambusher = entry!.Ambusher!;\n',
    ),
    (
        '            if (state.Unit(id) is not { Status: InstanceStatus.Active } unit || unit.Side != ambusher || state.Location(id)?.Location != location\n'
        '                || new[] { Conditions.Pinned, Conditions.Berserk, Conditions.Disrupted, Conditions.Captured }.Any(condition => Is(unit, condition)) || LiveFire.IsVehicle(unit))\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.ambush-withdraw-unit: {id} is not Infantry of the ambushing side in {location} that is free to withdraw (A11.41)");\n'
        '            }\n'
        '\n'
        '            if (!WithdrawalDestinations(state, unit, location).Contains(to))\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.ambush-withdraw-to: {id} may withdraw only to an ADJACENT Location a withdrawal could reach (A11.41, A11.21)");\n'
        '            }\n',
        f'            if (state.Unit(id) is not {{ Status: InstanceStatus.Active }} unit || !{C}.AmbushWithdrawer(true, unit.Side == ambusher, state.Location(id)?.Location == location,\n'
        '                Is(unit, Conditions.Pinned), Is(unit, Conditions.Berserk), Is(unit, Conditions.Disrupted), Is(unit, Conditions.Captured), LiveFire.IsVehicle(unit)))\n'
        '            {\n'
        f'                return Refused(scope, label, expected, {C}.AmbushWithdrawerText(id, location.ToString()));\n'
        '            }\n'
        '\n'
        f'            if ({C}.AmbushWithdrawalToBar(id, WithdrawalDestinations(state, unit, location).Contains(to)) is {{ }} toBar)\n'
        '            {\n'
        '                return Refused(scope, label, expected, toBar);\n'
        '            }\n',
    ),
    (
        '            [$"play.ambush-withdraw: {string.Join(", ", ids)} withdraw from {location} to {to} (A11.41)" + (tiring.Count > 0 ? $"; {string.Join(", ", tiring)} become CX (A4.72)" : string.Empty)]);\n',
        f'            [{C}.AmbushWithdrawalSummary(ids, location.ToString(), to.ToString(), tiring)]);\n',
    ),
    (
        '            return Refused(scope, label, expected, $"play.guard-none: {guard.Id} guards no prisoners (A20.5)");\n'
        '        }\n'
        '\n'
        '        if (state.Phase is not ("rph" or "aph") || guard.Side != state.PhasingSide || Is(guard, Conditions.Melee))\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.guard-phase: a Guard hands over or abandons its prisoners in its own side\'s RPh or APh, when not held in Melee (A20.5)");\n'
        '        }\n',
        f'            return Refused(scope, label, expected, {C}.GuardNoneText(guard.Id));\n'
        '        }\n'
        '\n'
        f'        if ({C}.GuardPhaseBar(state.Phase, guard.Side == state.PhasingSide, Is(guard, Conditions.Melee)) is {{ }} phaseBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, phaseBar);\n'
        '        }\n',
    ),
    (
        '                [$"play.prisoners-abandoned: {guard.Id} abandons {string.Join(", ", prisoners.Select(unit => unit.Id))}, who are Unarmed units of their own side (A20.5, A20.53)"]);\n',
        f'                [{C}.PrisonersAbandonedText(guard.Id, [.. prisoners.Select(unit => unit.Id)])]);\n',
    ),
    (
        '        if (state.Unit(toId) is not { Status: InstanceStatus.Active } heir || heir.Id == guard.Id || heir.Side != guard.Side || state.Location(heir.Id)?.Location != state.Location(guard.Id)?.Location\n'
        '            || Is(heir, Conditions.Captured) || Is(heir, Conditions.Unarmed) || Is(heir, Conditions.Melee) || !(vocabulary.IsA(heir.Kind, "asl:mmc") || vocabulary.IsA(heir.Kind, "asl:smc")))\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.guard-heir: {toId} is not an armed Personnel unit of {guard.Side} in {guard.Id}\'s Location, free of Melee (A20.5)");\n'
        '        }\n'
        '\n'
        '        if (GuardLoad(state, heir) + prisoners.Sum(UnitSize) > 5 * UnitSize(heir))\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.guard-capacity: {heir.Id} can guard prisoners of at most five times its US# (A20.51)");\n'
        '        }\n',
        f'        if (state.Unit(toId) is not {{ Status: InstanceStatus.Active }} heir || !{C}.Heir(true, heir.Id == guard.Id, heir.Side == guard.Side,\n'
        '            state.Location(heir.Id)?.Location == state.Location(guard.Id)?.Location, Is(heir, Conditions.Captured), Is(heir, Conditions.Unarmed), Is(heir, Conditions.Melee),\n'
        '            vocabulary.IsA(heir.Kind, "asl:mmc") || vocabulary.IsA(heir.Kind, "asl:smc")))\n'
        '        {\n'
        f'            return Refused(scope, label, expected, {C}.HeirText(toId, guard.Side, guard.Id));\n'
        '        }\n'
        '\n'
        f'        if ({C}.GuardCapacityBar(heir.Id, {C}.CanGuard(GuardLoad(state, heir), prisoners.Sum(UnitSize), UnitSize(heir))) is {{ }} capacityBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, capacityBar);\n'
        '        }\n',
    ),
    (
        '            [$"play.prisoners-transferred: {guard.Id} hands {string.Join(", ", prisoners.Select(unit => unit.Id))} to {heir.Id} (A20.5, A4.431)"]);\n',
        f'            [{C}.PrisonersTransferredText(guard.Id, [.. prisoners.Select(unit => unit.Id)], heir.Id)]);\n',
    ),
])
