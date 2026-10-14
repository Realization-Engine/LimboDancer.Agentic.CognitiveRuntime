"""Pass 32.g commit 7: Mopping Up calls Rules (ScenarioA1MoppingUp)."""
import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
MU = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.MoppingUp.cs"
C = "ScenarioA1MoppingUp"


def apply(p, edits):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:90])
        t = t.replace(old, new)
    p.write_bytes(t.encode("utf-8"))
    print("applied", p.name, len(edits))


apply(MU, [
    (
        "using LimboDancer.Domains.Asl.Maps.Coordinates;\nusing LimboDancer.Domains.Asl.Units.State;\n",
        "using LimboDancer.Domains.Asl.Maps.Coordinates;\nusing LimboDancer.Domains.Asl.Rules;\nusing LimboDancer.Domains.Asl.Units.State;\n",
    ),
    (
        '        if (CardOf(state) is not { } card)\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.mop-up-card: Mopping Up secures a building of the game\'s card; this game has none (A12.153; ruling R24.2)");\n'
        '        }\n'
        '\n'
        '        if (!Text(arguments, "building", out var building) || ScenarioCards.BuildingHexes(card, building) is not { Count: > 0 } hexes)\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.mop-up-building: \'{building}\' is not a building of the card (A12.153; ruling R24.2)");\n'
        '        }\n'
        '\n'
        '        if (state.Phase != "pfph")\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.mop-up-phase: Mopping Up is declared in its side\'s PFPh (A12.153)");\n'
        '        }\n'
        '\n'
        '        var side = state.PhasingSide;\n'
        '        bool InBuilding(BoardLocation at) => at.Level >= 0 && hexes.Any(hex => hex.Board == at.Board && hex.Hex == at.Hex);\n'
        '        if (hexes.Count == 1 && !HexLevels(state, hexes[0]).Any(level => level > 0))\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.mop-up-building: building {building} has one hex and one level; Mopping Up is of a multi-hex or multi-level building (A12.153)");\n'
        '        }\n'
        '\n'
        '        if (state.MoppedUpThisPlayerTurn.Contains(building, StringComparer.Ordinal))\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.mop-up-once: building {building} was Mopped Up this Player Turn; it may be once per Player Turn (A12.153)");\n'
        '        }\n'
        '\n'
        '        if (card.Sides.Any(item => item.Side != side && state.NoQuarter.Contains(item.Side, StringComparer.Ordinal)))\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.mop-up-no-quarter: the {side} side has employed No Quarter or Massacre and may no longer Mop Up (A12.153, A20.3, A20.4)");\n'
        '        }\n'
        '\n'
        '        var ids = Strings(arguments, "unitIds").Distinct(StringComparer.Ordinal).ToArray();\n'
        '        var units = ids.Select(state.Unit).ToArray();\n'
        '        if (ids.Length == 0 || units.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side != side\n'
        '            || unit.Kind is not ("asl:squad" or "asl:half-squad" or "asl:crew") || state.Location(unit.Id)?.Location is not { } at || !InBuilding(at)))\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.mop-up-units: each unit Mopping Up is an active Infantry MMC of the {side} side in building {building} (A12.153)");\n'
        '        }\n'
        '\n'
        '        // Table player, pass 24: the refusal says why.\n'
        '        foreach (var unit in units)\n'
        '        {\n'
        '            if ((Is(unit!, Conditions.Broken) ? "is broken" : Is(unit!, Conditions.Berserk) ? "is berserk" : Is(unit!, Conditions.Pinned) ? "is pinned"\n'
        '                : Is(unit!, Conditions.Captured) || Is(unit!, Conditions.Unarmed) ? "is not armed" : Is(unit!, Conditions.Melee) ? "is in Melee"\n'
        '                : Is(unit!, "asl:ti") ? "is already TI" : Is(unit!, Conditions.PrepFire) ? "fired in this PFPh" : null) is { } why)\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.mop-up-unit: {unit!.Id} {why}; Mopping Up takes an armed, unpinned, Good Order MMC free to become TI (A12.153)");\n'
        '            }\n'
        '        }\n'
        '\n'
        '        // Referee and table player, pass 24: an enemy vehicle in Bypass is not inside the building (A26.11); it keeps its ground level out of the secured set.\n'
        '        var enemies = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && state.Location(unit.Id)?.Location is { } at && InBuilding(at)\n'
        '            && !(LiveFire.IsVehicle(unit) && unit.Straddling is not null)).OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();\n'
        '        if (enemies.FirstOrDefault(unit => unit.Kind != UnitKinds.Dummy && !Is(unit, Conditions.Concealed) && !Is(unit, Conditions.Hidden) && !Is(unit, Conditions.Broken)\n'
        '            && !Is(unit, Conditions.Captured)) is not null)\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.mop-up-enemy: building {building} holds an unconcealed unbroken enemy unit (A12.153)");\n'
        '        }\n',
        '        // Pass 32.g: the card, the state, and the map are read here, and Rules decides each bar in its order.\n'
        '        var card = CardOf(state);\n'
        f'        if ({C}.CardBar(card is not null) is {{ }} cardBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, cardBar);\n'
        '        }\n'
        '\n'
        '        var hexes = Text(arguments, "building", out var building) ? ScenarioCards.BuildingHexes(card!, building) : null;\n'
        f'        if ({C}.BuildingBar(building, hexes is {{ Count: > 0 }}) is {{ }} buildingBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, buildingBar);\n'
        '        }\n'
        '\n'
        f'        if ({C}.PhaseBar(state.Phase) is {{ }} phaseBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, phaseBar);\n'
        '        }\n'
        '\n'
        '        var side = state.PhasingSide;\n'
        f'        bool InBuilding(BoardLocation at) => {C}.InBuilding(at.Level, hexes!.Any(hex => hex.Board == at.Board && hex.Hex == at.Hex));\n'
        f'        if ({C}.SingleHexBar(hexes!.Count, hexes.Count != 1 || HexLevels(state, hexes[0]).Any(level => level > 0), building!) is {{ }} singleBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, singleBar);\n'
        '        }\n'
        '\n'
        f'        if ({C}.OnceBar(state.MoppedUpThisPlayerTurn.Contains(building!, StringComparer.Ordinal), building!) is {{ }} onceBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, onceBar);\n'
        '        }\n'
        '\n'
        f'        if ({C}.NoQuarterBar(card!.Sides.Any(item => item.Side != side && state.NoQuarter.Contains(item.Side, StringComparer.Ordinal)), side!) is {{ }} quarterBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, quarterBar);\n'
        '        }\n'
        '\n'
        '        var ids = Strings(arguments, "unitIds").Distinct(StringComparer.Ordinal).ToArray();\n'
        '        var units = ids.Select(state.Unit).ToArray();\n'
        f'        if ({C}.UnitsBar(ids.Length, units.Any(unit => !{C}.MopUpUnit(unit is {{ Status: InstanceStatus.Active }}, unit?.Side == side,\n'
        '            unit?.Kind is "asl:squad" or "asl:half-squad" or "asl:crew", unit is not null && state.Location(unit.Id)?.Location is { } at && InBuilding(at))), side!, building!) is { } unitsBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, unitsBar);\n'
        '        }\n'
        '\n'
        '        // Table player, pass 24: the refusal says why.\n'
        '        foreach (var unit in units)\n'
        '        {\n'
        f'            if ({C}.UnitWhy(Is(unit!, Conditions.Broken), Is(unit!, Conditions.Berserk), Is(unit!, Conditions.Pinned), Is(unit!, Conditions.Captured), Is(unit!, Conditions.Unarmed),\n'
        '                Is(unit!, Conditions.Melee), Is(unit!, "asl:ti"), Is(unit!, Conditions.PrepFire)) is { } why)\n'
        '            {\n'
        f'                return Refused(scope, label, expected, {C}.UnitText(unit!.Id, why));\n'
        '            }\n'
        '        }\n'
        '\n'
        '        // Referee and table player, pass 24: an enemy vehicle in Bypass is not inside the building (A26.11); it keeps its ground level out of the secured set.\n'
        f'        var enemies = state.Units.Where(unit => {C}.EnemyInside(unit.Status == InstanceStatus.Active, unit.Side != side, state.Location(unit.Id)?.Location is {{ }} at && InBuilding(at),\n'
        '            LiveFire.IsVehicle(unit), unit.Straddling is not null)).OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();\n'
        f'        if (enemies.Any(unit => {C}.UnconcealedUnbroken(unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), Is(unit, Conditions.Broken), Is(unit, Conditions.Captured))))\n'
        '        {\n'
        f'            return Refused(scope, label, expected, {C}.EnemyText(building!));\n'
        '        }\n',
    ),
    (
        '            var near = units.Select(unit => HexDistance(state, state.Location(unit!.Id)!.Location, hex)).ToArray();\n'
        '            if (near.Any(distance => distance is null))\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.mop-up-range: the distance to {hex.Hex} is not decided on this map (A12.153)");\n'
        '            }\n'
        '\n'
        '            if (!near.Any(distance => distance <= 2))\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.mop-up-range: no unit Mopping Up is within two hexes of {hex.Hex}, a ground-level Location the {side} side does not Control (A12.153, A26.11)");\n'
        '            }\n',
        f'            if ({C}.RangeBar([.. units.Select(unit => HexDistance(state, state.Location(unit!.Id)!.Location, hex))], hex.Hex, side!) is {{ }} rangeBar)\n'
        '            {\n'
        '                return Refused(scope, label, expected, rangeBar);\n'
        '            }\n',
    ),
    (
        '        if (state.Unit(guardId) is not { Status: InstanceStatus.Active } guard || guard.Side != side || guard.Kind == UnitKinds.Dummy || LiveFire.IsVehicle(guard)\n'
        '            || (Is(guard, Conditions.Broken) && guard.Kind is not ("asl:leader" or "asl:hero")) || Is(guard, Conditions.Berserk) || Is(guard, Conditions.Unarmed)\n'
        '            || Is(guard, Conditions.Captured) || state.Location(guard.Id)?.Location is not { } guardAt || !InBuilding(guardAt))\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.mop-up-guard: {guardId} is not an armed Personnel unit of the {side} side inside building {building} that may guard prisoners (A12.153, A20.5)");\n'
        '        }\n',
        '        var guard = state.Unit(guardId);\n'
        f'        if (guard is null || state.Location(guard.Id)?.Location is not {{ }} guardAt || !{C}.Guard(guard.Status == InstanceStatus.Active, guard.Side == side, guard.Kind == UnitKinds.Dummy,\n'
        '            LiveFire.IsVehicle(guard), Is(guard, Conditions.Broken), guard.Kind is "asl:leader" or "asl:hero", Is(guard, Conditions.Berserk), Is(guard, Conditions.Unarmed),\n'
        '            Is(guard, Conditions.Captured), InBuilding(guardAt)))\n'
        '        {\n'
        f'            return Refused(scope, label, expected, {C}.GuardText(guardId, side!, building!));\n'
        '        }\n',
    ),
    (
        '            Add("hidden-placed", new ConditionsChanged(hidden.Id, new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.False, [Conditions.Concealed] = ConditionState.True }),\n'
        '                [hidden.Side]);\n',
        f'            Add("hidden-placed", new ConditionsChanged(hidden.Id, ConditionChanges({C}.HiddenPlacedConditions())), [hidden.Side]);\n',
    ),
    (
        '        var concealed = enemies.Where(unit => unit.Kind != UnitKinds.Dummy && (Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden))).ToArray();\n',
        f'        var concealed = enemies.Where(unit => {C}.Concealed(unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden))).ToArray();\n',
    ),
    (
        '        var phrase = (ids.Length == 1\n'
        '            ? $"{ids[0]} Mops Up building {building} and becomes TI (A12.153; ruling R24.2)"\n'
        '            : $"{string.Join(", ", ids)} Mop Up building {building} and become TI (A12.153; ruling R24.2)")\n'
        '            + (found > 0 ? $"; {found} hidden enemy unit{(found == 1 ? " is" : "s are")} placed beneath \\"?\\"" : string.Empty)\n'
        '            + (dummies > 0 ? $"; {dummies} enemy Dumm{(dummies == 1 ? "y is" : "ies are")} removed" : string.Empty);\n',
        f'        var phrase = {C}.Phrase(ids, building!, found, dummies);\n',
    ),
    (
        '            var bypassed = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && LiveFire.IsVehicle(unit) && unit.Straddling is not null\n'
        '                && !Is(unit, Conditions.Abandoned) && !Is(unit, Conditions.Captured)).Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>()\n',
        f'            var bypassed = state.Units.Where(unit => {C}.BypassedVehicle(unit.Status == InstanceStatus.Active, unit.Side != side, LiveFire.IsVehicle(unit), unit.Straddling is not null,\n'
        '                Is(unit, Conditions.Abandoned), Is(unit, Conditions.Captured))).Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>()\n',
    ),
    (
        '            Add("building-mopped-up", new BuildingMoppedUp(building, side, ids, secured));\n'
        '            reasons.Add($"play.mop-up: {phrase}; no concealed enemy unit is left, so the {side} side Controls the building and its Locations (A26.11)");\n'
        '            foreach (var broken in enemies.Where(unit => unit.Kind != UnitKinds.Dummy && Is(unit, Conditions.Broken) && !Is(unit, Conditions.Captured)))\n',
        '            Add("building-mopped-up", new BuildingMoppedUp(building!, side!, ids, secured));\n'
        f'            reasons.Add({C}.SecuredSummary(phrase, side!));\n'
        f'            foreach (var broken in enemies.Where(unit => {C}.Surrenders(unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Broken), Is(unit, Conditions.Captured))))\n',
    ),
    (
        '                Add("conditions-changed", new ConditionsChanged(broken.Id, new Dictionary<string, ConditionState>\n'
        '                {\n'
        '                    [Conditions.Broken] = ConditionState.False,\n'
        '                    [Conditions.Disrupted] = ConditionState.False,\n'
        '                    [Conditions.Pinned] = ConditionState.False,\n'
        '                    [Conditions.DesperationMorale] = ConditionState.False,\n'
        '                }));\n'
        '                Add("instance-captured", new InstanceCaptured(broken.Id, guard.Id));\n'
        '                reasons.Add($"play.mop-up-surrender: {broken.Id} surrenders to {guard.Id} (A12.153, A20.5)");\n',
        f'                Add("conditions-changed", new ConditionsChanged(broken.Id, ConditionChanges({C}.SurrenderConditions())));\n'
        '                Add("instance-captured", new InstanceCaptured(broken.Id, guard.Id));\n'
        f'                reasons.Add({C}.SurrenderText(broken.Id, guard.Id));\n',
    ),
    (
        '        Add("building-mopped-up", new BuildingMoppedUp(building, side, ids, null));\n'
        '        var defenders = concealed.Where(unit => !Is(unit, Conditions.Broken) && !Is(unit, Conditions.Unarmed) && !Is(unit, Conditions.Captured)).ToArray();\n'
        '        var summary = $"play.mop-up: {phrase}; concealed enemy units remain, so the building is not secured";\n',
        '        Add("building-mopped-up", new BuildingMoppedUp(building!, side!, ids, null));\n'
        f'        var defenders = concealed.Where(unit => {C}.Defender(Is(unit, Conditions.Broken), Is(unit, Conditions.Unarmed), Is(unit, Conditions.Captured))).ToArray();\n'
        f'        var summary = {C}.NotSecuredSummary(phrase);\n',
    ),
    (
        '        // A12.154: -1 per Stealthy unit, -1 per HS-equivalent beyond one, the best leader\'s leadership unless alone, +1 per Lax unit.\n'
        '        var drm = 0;\n'
        '        var notes = new List<string>();\n'
        '        var stealthy = defenders.Count(unit => unit.Kind == "asl:hero" || (unit.Kind == "asl:leader" && Is(unit, Conditions.Heroic)));\n'
        '        if (stealthy > 0)\n'
        '        {\n'
        '            drm -= stealthy;\n'
        '            notes.Add($"-{stealthy} Stealthy");\n'
        '        }\n'
        '\n'
        '        var halfSquads = defenders.Sum(unit => unit.Kind switch { "asl:squad" => 2, "asl:half-squad" or "asl:crew" => 1, _ => 0 });\n'
        '        if (halfSquads > 1)\n'
        '        {\n'
        '            drm -= halfSquads - 1;\n'
        '            notes.Add($"-{halfSquads - 1} for {halfSquads} HS-equivalents");\n'
        '        }\n'
        '\n'
        '        if (defenders.Length > 1 && defenders.Where(unit => unit.Kind == "asl:leader" && DefinitionOf(unit)?.Leadership is not null)\n'
        '            .Select(unit => DefinitionOf(unit)!.Leadership!.Value + (Is(unit, Conditions.Wounded) ? 1 : 0)).DefaultIfEmpty(0).Min() is var leadership and not 0)\n'
        '        {\n'
        '            drm += leadership;\n'
        '            notes.Add($"{leadership:+0;-0} leadership");\n'
        '        }\n'
        '\n'
        '        var lax = defenders.Count(unit => Experience.Inexperienced(state, unit, catalogs, vocabulary) == ConditionState.True);\n'
        '        if (lax > 0)\n'
        '        {\n'
        '            drm += lax;\n'
        '            notes.Add($"+{lax} Lax");\n'
        '        }\n',
        '        // A12.154: -1 per Stealthy unit, -1 per HS-equivalent beyond one, the best leader\'s leadership unless alone, +1 per Lax unit; the catalog and the\n'
        '        // Inexperience are read here, and Rules counts (pass 32.g).\n'
        f'        var (drm, notes) = {C}.CasualtyDrm([.. defenders.Select(unit => new MopUpDefenderFacts(unit.Kind == "asl:hero", unit.Kind == "asl:leader", Is(unit, Conditions.Heroic),\n'
        f'            {C}.HalfSquads(unit.Kind == "asl:squad", unit.Kind is "asl:half-squad" or "asl:crew"), DefinitionOf(unit)?.Leadership, Is(unit, Conditions.Wounded),\n'
        '            Experience.Inexperienced(state, unit, catalogs, vocabulary) == ConditionState.True))]);\n',
    ),
    (
        '            if (drawn.Values[0] + drm > 1)\n'
        '            {\n'
        '                return built;\n'
        '            }\n',
        f'            if (!{C}.CasualtyReduces(drawn.Values[0], drm))\n'
        '            {\n'
        '                return built;\n'
        '            }\n',
    ),
    (
        '                struck = [.. ids.Where((_, index) => selection.Values[index] == selection.Values.Max())];\n',
        f'                struck = [.. {C}.Struck(ids, selection.Values)];\n',
    ),
    (
        '            [$"{summary}; the DEFENDER\'s Casualty dr ({(notes.Count == 0 ? "no drm" : string.Join(", ", notes))}) Reduces a Mopping-Up unit on a Final dr of 1 or less (A12.154; ruling R24.2)"])\n',
        f'            [{C}.CasualtyText(summary, notes)])\n',
    ),
    (
        '                .Where(item => item.Item2.Count > 1 || item.Item2.Any(hex => HexLevels(state, hex).Any(level => level > 0)))]\n',
        f'                .Where(item => {C}.MopUpBuilding(item.Item2.Count, item.Item2.Any(hex => HexLevels(state, hex).Any(level => level > 0))))]\n',
    ),
])
