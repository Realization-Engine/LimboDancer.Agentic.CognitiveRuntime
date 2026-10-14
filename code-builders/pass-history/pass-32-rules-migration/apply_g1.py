"""Pass 32.g commit 1: PlanAdvanceUnits' blocks, Opposing, DifficultAdvance, OverstackExcess, AdvanceAid call ScenarioA1AdvanceCalculator."""
import pathlib

P = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL\LimboDancer.Domains.Asl.Play\GamePlanner.CloseCombat.cs")
C = "ScenarioA1AdvanceCalculator"


def apply(p, edits):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:80])
        t = t.replace(old, new)
    p.write_bytes(t.encode("utf-8"))
    print("applied", p.name, len(edits))


apply(P, [
    (
        '        if (state.Phase != "aph")\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.advance-phase: units advance in their side\'s APh (A3.7, p. 47)");\n'
        '        }\n',
        f'        if ({C}.AdvancePhaseBar(state.Phase) is {{ }} phaseBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, phaseBar);\n'
        '        }\n',
    ),
    (
        '        if (units.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side != state.PhasingSide || unit.Definition is null))\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.advance-unit: every unit is an active unit of the phasing side from the catalog");\n'
        '        }\n',
        f'        if (units.Any(unit => !{C}.AdvanceUnitAllowed(unit is {{ Status: InstanceStatus.Active }}, unit?.Side == state.PhasingSide, unit?.Definition is not null)))\n'
        '        {\n'
        f'            return Refused(scope, label, expected, {C}.AdvanceUnitText());\n'
        '        }\n',
    ),
    (
        '        if (offBoard > 0)\n'
        '        {\n'
        '            if (offBoard < units.Length)\n'
        '            {\n'
        '                return Refused(scope, label, expected, "play.entry-stack: units waiting off board enter apart from units on the map (A2.51; ruling R20.5)");\n'
        '            }\n'
        '\n',
        '        if (offBoard > 0)\n'
        '        {\n'
        f'            if ({C}.EntryStackBar(offBoard, units.Length) is {{ }} stackBar)\n'
        '            {\n'
        '                return Refused(scope, label, expected, stackBar);\n'
        '            }\n'
        '\n',
    ),
    (
        '            return Refused(scope, label, expected, "play.advance-unit: the units advance from one Location");\n',
        f'            return Refused(scope, label, expected, {C}.OneOriginText());\n',
    ),
    (
        '            if (new[] { Conditions.Broken, Conditions.Pinned, Conditions.Berserk, Conditions.Melee, Conditions.Captured, Conditions.Hidden, "asl:ti" }\n'
        '                .FirstOrDefault(condition => Is(unit!, condition)) is { } condition)\n'
        '            {\n'
        '                return Refused(scope, label, expected,\n'
        '                    $"play.advance-unit: {unit!.Id} is {condition.Replace("asl:", string.Empty, StringComparison.Ordinal)}, so it may not advance (A4.7, A15.431, A11.15, A12.3)"\n'
        '                    + (condition == Conditions.Hidden ? "; a hidden unit is first placed beneath \\"?\\" (A12.32; ruling R23.5)" : string.Empty));\n'
        '            }\n'
        '\n'
        '            if (unit!.MovementEnded)\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.advance-unit: {unit.Id} has advanced or moved as far as it may this phase (A4.7)");\n'
        '            }\n',
        f'            // Rules names the condition (pass 32.g); the text stays here, since its hole on a Play local is cut by the text list at a nested quote.\n'
        f'            if ({C}.AdvanceBarringCondition(Is(unit!, Conditions.Broken), Is(unit!, Conditions.Pinned), Is(unit!, Conditions.Berserk), Is(unit!, Conditions.Melee),\n'
        '                Is(unit!, Conditions.Captured), Is(unit!, Conditions.Hidden), Is(unit!, "asl:ti")) is { } condition)\n'
        '            {\n'
        '                return Refused(scope, label, expected,\n'
        '                    $"play.advance-unit: {unit!.Id} is {condition.Replace("asl:", string.Empty, StringComparison.Ordinal)}, so it may not advance (A4.7, A15.431, A11.15, A12.3)"\n'
        '                    + (condition == "hidden" ? "; a hidden unit is first placed beneath \\"?\\" (A12.32; ruling R23.5)" : string.Empty));\n'
        '            }\n'
        '\n'
        f'            if ({C}.MovementEndedBar(unit!.Id, unit.MovementEnded) is {{ }} endedBar)\n'
        '            {\n'
        '                return Refused(scope, label, expected, endedBar);\n'
        '            }\n',
    ),
    (
        '            return Refused(scope, label, expected, $"play.advance-crew-mans-gun: {gunner.Id} mans a Gun; abandoning or moving a Gun is not reviewed (C10, A21.13)");\n',
        f'            return Refused(scope, label, expected, {C}.GunnerText(gunner.Id));\n',
    ),
    (
        '            return Refused(scope, label, expected, $"play.advance-unit: {driven.Id} is a vehicle; only Infantry advance (A4.7)");\n',
        f'            return Refused(scope, label, expected, {C}.VehicleText(driven.Id));\n',
    ),
    (
        '        var afv = state.At(to).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide && IsAfv(unit)\n'
        '            && !Is(unit, Conditions.Abandoned) && !Is(unit, Conditions.Concealed) && !Is(unit, Conditions.Hidden));\n',
        f'        var afv = state.At(to).OfType<UnitInstance>().FirstOrDefault(unit => {C}.PaatcAfv(unit.Status == InstanceStatus.Active, unit.Side != state.PhasingSide, IsAfv(unit),\n'
        '            Is(unit, Conditions.Abandoned), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden)));\n',
    ),
    (
        '            return Refused(scope, label, expected, stepReason!.Replace("play.move-", "play.advance-", StringComparison.Ordinal));\n'
        '        }\n'
        '\n'
        '        if (entry.AllMf)\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.advance-marsh: a marsh hex cannot be entered in the APh (B16.4)");\n'
        '        }\n',
        f'            return Refused(scope, label, expected, {C}.AdvanceStepReason(stepReason!));\n'
        '        }\n'
        '\n'
        f'        if ({C}.MarshBar(entry.AllMf) is {{ }} marshBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, marshBar);\n'
        '        }\n',
    ),
    (
        '        var halfMf = (entry.HalfMf * (units.Any(unit => state.Encircled(unit!)) ? 2 : 1)) + (2 * excess);\n',
        f'        var halfMf = {C}.AdvanceHalfMf(entry.HalfMf, units.Any(unit => state.Encircled(unit!)), excess);\n',
    ),
    (
        '                return Refused(scope, label, expected, $"play.advance-mf: {unit!.Id} has no MF allotment the catalog decides, or none left after portage (A4.7, A4.72)");\n'
        '            }\n'
        '\n'
        '            if (difficult && Is(unit!, Conditions.Cx))\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.advance-difficult-terrain: {unit!.Id} is CX and may not advance into Difficult Terrain (A4.72)");\n'
        '            }\n',
        f'                return Refused(scope, label, expected, {C}.NoMfText(unit!.Id));\n'
        '            }\n'
        '\n'
        f'            if ({C}.CxDifficultBar(unit!.Id, difficult, Is(unit!, Conditions.Cx)) is {{ }} cxBar)\n'
        '            {\n'
        '                return Refused(scope, label, expected, cxBar);\n'
        '            }\n',
    ),
    (
        '        if (enemies.Any(KnownEnemy) && units.FirstOrDefault(unit => Is(unit!, Conditions.Unarmed)) is { } unarmed)\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.advance-unarmed: {unarmed.Id} is Unarmed and may not enter a Location of a Known enemy unit (A20.54)");\n'
        '        }\n'
        '\n'
        '        // A crew in CC, and the Gun it mans, are not reviewed (ruling R24.3).\n'
        '        if (enemies.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew")))\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.advance-crew: CC with a Gun\'s crew is not reviewed (C11, ruling R24.3)");\n'
        '        }\n',
        f'        if ({C}.UnarmedBar(enemies.Any(KnownEnemy), units.FirstOrDefault(unit => Is(unit!, Conditions.Unarmed))?.Id) is {{ }} unarmedBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, unarmedBar);\n'
        '        }\n'
        '\n'
        '        // A crew in CC, and the Gun it mans, are not reviewed (ruling R24.3).\n'
        f'        if ({C}.CrewBar(enemies.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew"))) is {{ }} crewBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, crewBar);\n'
        '        }\n',
    ),
    (
        '        var surrendering = enemies.Where(unit => !state.Night && Is(unit, Conditions.Disrupted) && !state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal))\n'
        '            .OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();\n'
        '        string[] takers = [.. unitList.Where(unit => !Is(unit, Conditions.Unarmed) && !Is(unit, Conditions.Concealed) && !Is(unit, Conditions.Hidden)\n'
        '            && (vocabulary.IsA(unit.Kind, "asl:mmc") || vocabulary.IsA(unit.Kind, "asl:smc"))).Select(unit => unit.Id).Order(StringComparer.Ordinal)];\n'
        '        var summary = $"play.advance: {string.Join(", ", ids)} {(ids.Length == 1 ? "advances" : "advance")} into {to} ({terrain})"\n'
        '            + (entering is not null ? " from off board, entering the map (A2.5; ruling R25.1)" : string.Empty) + (Opposing(enemies) is { Length: > 0 } opposing ? $", with {opposing}: CC follows (A3.7)" : string.Empty)\n'
        '            + (tiring.Count > 0 ? $"; {string.Join(", ", tiring)} {(tiring.Count == 1 ? "becomes" : "become")} CX advancing into Difficult Terrain (A4.72)" : string.Empty)\n'
        '            + (excess > 0 ? $"; the Location is overstacked by {excess} squad-equivalent(s), which costs {excess} more MF (A5.11)" : string.Empty)\n'
        '            + (surrendering.Length > 0 && takers.Length > 0 ? $"; {string.Join(", ", surrendering.Select(unit => unit.Id))} is Disrupted and surrenders (A19.12)" : string.Empty);\n',
        f'        var surrendering = enemies.Where(unit => {C}.Surrenders(state.Night, Is(unit, Conditions.Disrupted), state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal)))\n'
        '            .OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();\n'
        f'        string[] takers = [.. unitList.Where(unit => {C}.Taker(Is(unit, Conditions.Unarmed), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden),\n'
        '            vocabulary.IsA(unit.Kind, "asl:mmc"), vocabulary.IsA(unit.Kind, "asl:smc"))).Select(unit => unit.Id).Order(StringComparer.Ordinal)];\n'
        f'        var summary = {C}.AdvanceSummary(ids, to.ToString(), terrain, entering is not null, Opposing(enemies), tiring, excess, [.. surrendering.Select(unit => unit.Id)], takers.Length > 0);\n',
    ),
    (
        '        string[] dummiesWarning = unitList.Any(unit => unit.Kind == UnitKinds.Dummy) && enemies.Any(unit => !Is(unit, Conditions.Hidden))\n',
        f'        string[] dummiesWarning = {C}.DummiesWarned(unitList.Any(unit => unit.Kind == UnitKinds.Dummy), enemies.Any(unit => !Is(unit, Conditions.Hidden)))\n',
    ),
    (
        '                [summary, .. dummiesWarning, $"play.advance-paatc: {string.Join(", ", testing.Select(unit => unit.Id))} each pass a PAATC before advancing on {afv.Id}; a unit that fails is pinned and stays (A11.6)"])\n',
        f'                [summary, .. dummiesWarning, {C}.PaatcText([.. testing.Select(unit => unit.Id)], afv.Id)])\n',
    ),
    (
        '        if (!state.Night && terrain == "open-ground" && EnemyGoodOrderInLosWithin16(state, state.PhasingSide, to))\n',
        f'        if ({C}.LosesConcealment(state.Night, terrain, () => EnemyGoodOrderInLosWithin16(state, state.PhasingSide, to)))\n',
    ),
    (
        '    private static string Opposing(UnitInstance[] enemies)\n'
        '    {\n'
        '        string[] known = [.. enemies.Where(KnownEnemy).Select(unit => unit.Id)];\n'
        '        var stack = enemies.Any(unit => !KnownEnemy(unit) && !Is(unit, Conditions.Hidden));\n'
        '        return string.Join(", ", known) + (stack ? (known.Length > 0 ? " and " : string.Empty) + "a concealed stack" : string.Empty);\n'
        '    }\n',
        '    private static string Opposing(UnitInstance[] enemies) =>\n'
        f'        {C}.Opposing([.. enemies.Select(unit => new OpposingCounterFacts(unit.Id, KnownEnemy(unit), Is(unit, Conditions.Hidden)))]);\n',
    ),
    (
        '    private bool? DifficultAdvance(GameState state, UnitInstance unit, int halfMf, bool aided = false, bool lent = false) =>\n'
        '        MfAllotment(state, unit, 0, Is(unit, Conditions.Cx), aided ? 2 : 0, lent ? 1 : 0) is { } allotment && allotment > 0 ? halfMf >= 2 * Math.Min(4, allotment) : null;\n',
        '    private bool? DifficultAdvance(GameState state, UnitInstance unit, int halfMf, bool aided = false, bool lent = false) =>\n'
        f'        {C}.DifficultAdvance(MfAllotment(state, unit, 0, Is(unit, Conditions.Cx), aided ? 2 : 0, lent ? 1 : 0), halfMf);\n',
    ),
    (
        '    private int OverstackExcess(IEnumerable<UnitInstance> units)\n'
        '    {\n'
        '        var list = units.ToArray();\n'
        '        var smc = list.Count(unit => vocabulary.IsA(unit.Kind, "asl:smc"));\n'
        '        var squads = list.Count(unit => vocabulary.IsA(unit.Kind, "asl:squad"))\n'
        '            + (list.Count(unit => vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew")) / 2m) + (Math.Floor(smc / 5m) / 2m);\n'
        '        return squads > 3 ? (int)Math.Ceiling(squads - 3) : 0;\n'
        '    }\n',
        '    private int OverstackExcess(IEnumerable<UnitInstance> units)\n'
        '    {\n'
        '        var list = units.ToArray();\n'
        f'        return {C}.OverstackExcess(list.Count(unit => vocabulary.IsA(unit.Kind, "asl:squad")),\n'
        '            list.Count(unit => vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew")), list.Count(unit => vocabulary.IsA(unit.Kind, "asl:smc")));\n'
        '    }\n',
    ),
    (
        '    private (HashSet<string> Aided, string? IpcTo) AdvanceAid(GameState state, UnitInstance[] units)\n'
        '    {\n'
        '        var aided = units.Where(unit => vocabulary.IsA(unit.Kind, "asl:mmc") && Nationality(unit) is { } nationality && units.Any(leader => leader.Id != unit.Id\n'
        '            && vocabulary.IsA(leader.Kind, "asl:leader") && !Is(leader, Conditions.Broken) && Nationality(leader) == nationality)).Select(unit => unit.Id)\n'
        '            .ToHashSet(StringComparer.Ordinal);\n'
        '        var ipcTo = units.Where(unit => aided.Contains(unit.Id) && Laden(state, unit)).ToArray() is [{ } onlyLaden]\n'
        '            && units.Any(leader => vocabulary.IsA(leader.Kind, "asl:leader") && !Is(leader, Conditions.Broken) && !Is(leader, Conditions.Wounded)\n'
        '                && Nationality(leader) == Nationality(onlyLaden)) ? onlyLaden.Id : null;\n'
        '        return (aided, ipcTo);\n'
        '    }\n',
        '    private (HashSet<string> Aided, string? IpcTo) AdvanceAid(GameState state, UnitInstance[] units) =>\n'
        f'        {C}.AdvanceAid([.. units.Select(unit => new AdvancingUnitFacts(unit.Id, vocabulary.IsA(unit.Kind, "asl:mmc"), vocabulary.IsA(unit.Kind, "asl:leader"), Nationality(unit),\n'
        '            Is(unit, Conditions.Broken), Is(unit, Conditions.Wounded), Laden(state, unit)))]);\n',
    ),
])
