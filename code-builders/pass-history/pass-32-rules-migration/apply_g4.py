"""Pass 32.g commit 4: the due reads (CloseCombatRequired, MandatoryAttackDecidable, DeclaringSides, AmbushDue, AmbushUndecided, CloseCombatDue, ChargeBarred) call Rules."""
import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
CC = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.CloseCombat.cs"
RULES = ROOT / "LimboDancer.Domains.Asl.Rules/ScenarioA1CloseCombatRules.cs"
DRAFT = pathlib.Path(r"E:\Archive\GitHub\dlandi\pass32-tools\draft_g4_rules.cs")
C = "ScenarioA1CloseCombatRules"


def apply(p, edits):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:80])
        t = t.replace(old, new)
    p.write_bytes(t.encode("utf-8"))
    print("applied", p.name, len(edits))


# The Rules file: the record goes before the class, the members at the class's end.
draft = DRAFT.read_text(encoding="utf-8")
record_start = draft.index("    /// <summary>A unit of a Location as the CC-required read sees it")
record_end = draft.index("\n", draft.index("public sealed record RequiredCcUnitFacts")) + 1
record = "".join(line[4:] + "\n" for line in draft[record_start:record_end].splitlines())
members = draft[record_end:].lstrip("\n")
assert members.rstrip().endswith("}")
members = members.rstrip()[:-1].rstrip("\n") + "\n"
rules = RULES.read_text(encoding="utf-8")
assert "\r\n" not in rules
anchor = "public static class ScenarioA1CloseCombatRules\n"
rules = rules.replace(anchor, record + "\n" + anchor)
assert rules.rstrip().endswith("}")
rules = rules.rstrip()[:-1].rstrip("\n") + "\n\n" + members + "}\n"
RULES.write_text(rules, encoding="utf-8", newline="\n")
print("appended", RULES.name)

apply(CC, [
    (
        '                return $"play.cc-required: {owing.Id} is berserk with a Known enemy vehicle in {location}, so it attacks the vehicle in CC before the CCPh ends (A15.43, A11.31)";\n',
        f'                return {C}.CcRequiredVehicleText(owing.Id, location.ToString());\n',
    ),
    (
        '            // CC with a crew (ruling R24.3) is not reviewed, so it cannot be required; a Location holding a vehicle has its sequential CC (A11.31).\n'
        '            if (here.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew") || LiveFire.IsVehicle(unit)))\n'
        '            {\n'
        '                continue;\n'
        '            }\n'
        '\n'
        '            var berserk = here.FirstOrDefault(unit => Is(unit, Conditions.Berserk) && here.Any(other => other.Side != unit.Side && KnownEnemy(other)));\n'
        '            var reinforcing = here.Any(unit => Is(unit, Conditions.Melee))\n'
        '                ? here.FirstOrDefault(unit => state.Advances.Any(item => item.Unit == unit.Id && item.To == location) && !Is(unit, Conditions.Broken))\n'
        '                : null;\n'
        '            if ((berserk ?? reinforcing) is not { } unit)\n'
        '            {\n'
        '                continue;\n'
        '            }\n'
        '\n'
        '            var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;\n'
        '            if (LiveCloseCombat.AmbushFromState(state, location, terrain).Facts is not null && MandatoryAttackDecidable(state, location, terrain, unit, here))\n'
        '            {\n'
        '                return berserk is not null\n'
        '                    ? $"play.cc-required: {unit.Id} is berserk with a Known enemy unit in {location}, so it attacks in CC before the CCPh ends (A15.43)"\n'
        '                    : $"play.cc-required: {unit.Id} advanced into the Melee in {location}, so it attacks in CC before the CCPh ends (A11.15)";\n'
        '            }\n',
        '            // CC with a crew (ruling R24.3) is not reviewed, so it cannot be required; a Location holding a vehicle has its sequential CC (A11.31). Rules\n'
        '            // picks the unit (pass 32.g).\n'
        f'            if ({C}.RequiredUnit([.. here.Select(unit => new RequiredCcUnitFacts(unit.Id, unit.Side, Is(unit, Conditions.Berserk), Is(unit, Conditions.Melee), Is(unit, Conditions.Broken),\n'
        '                KnownEnemy(unit), state.Advances.Any(item => item.Unit == unit.Id && item.To == location), vocabulary.IsA(unit.Kind, "asl:crew"), LiveFire.IsVehicle(unit)))])\n'
        '                is not { } required || here.First(item => item.Id == required.Id) is not { } unit)\n'
        '            {\n'
        '                continue;\n'
        '            }\n'
        '\n'
        '            var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;\n'
        '            if (LiveCloseCombat.AmbushFromState(state, location, terrain).Facts is not null && MandatoryAttackDecidable(state, location, terrain, unit, here))\n'
        '            {\n'
        f'                return {C}.CcRequiredText(unit.Id, location.ToString(), required.Berserk);\n'
        '            }\n',
    ),
    (
        '            // Referee, pass 14: the prisoners kept keep their Guards.\n'
        '            var kept = facts?.Units!.Where(item => item.UnitId == unit.Id || item.Side != unit.Side || item.Captured == true).ToList();\n'
        '            kept?.AddRange(facts!.Units!.Where(item => item.Side == unit.Side && item.UnitId != unit.Id && kept.Any(prisoner => prisoner.GuardId == item.UnitId)));\n'
        '            if (facts is not null && ScenarioA1CloseCombatCalculator.Precheck(facts with { Units = kept }, CloseCombatReference.Value) is { Count: 0 })\n',
        '            // Referee, pass 14: the prisoners kept keep their Guards.\n'
        f'            if (facts is not null && ScenarioA1CloseCombatCalculator.Precheck(facts with {{ Units = {C}.TrialUnits(facts.Units!, unit.Id, unit.Side) }}, CloseCombatReference.Value) is {{ Count: 0 }})\n',
    ),
    (
        '        return state.CloseCombats.FirstOrDefault(item => item.Location == location) is { Ambusher: { } ambusher } entry\n'
        '            ? entry.Rounds.Count == 0 || ambusherAgain ? [ambusher] : [.. sides.Where(side => side != ambusher)]\n'
        '            : sides;\n',
        '        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);\n'
        f'        return {C}.DeclaringSides(sides, entry?.Ambusher, entry?.Rounds.Count ?? 0, ambusherAgain);\n',
    ),
    (
        '        if (state.Phase != "ccph" || state.CloseCombats.Any(item => item.Location == location))\n'
        '        {\n'
        '            return false;\n'
        '        }\n'
        '\n'
        '        // Ruling R11.16: no Ambush dr is made in a Location holding a vehicle.\n'
        '        if (state.At(location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)))\n'
        '        {\n'
        '            return false;\n'
        '        }\n',
        '        // Ruling R11.16: no Ambush dr is made in a Location holding a vehicle.\n'
        f'        if (!{C}.AmbushDueCandidate(state.Phase, state.CloseCombats.Any(item => item.Location == location),\n'
        '            state.At(location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit))))\n'
        '        {\n'
        '            return false;\n'
        '        }\n',
    ),
    (
        '        return first.Reasons is [{ } missing] && missing.StartsWith("asl.a1.cc.roll-missing:ambush:", StringComparison.Ordinal) ? null\n'
        '            : string.Join("; ", first.Reasons.Select(reason => RefusalReasons.Explain(reason).Split(": ", 2) is [_, var sentence] ? sentence : reason));\n',
        f'        return {C}.AmbushRollSide(first.Reasons) is not null ? null : {C}.AmbushUndecidedText(first.Reasons, RefusalReasons.Explain);\n',
    ),
    (
        '        bool NothingLeft(CloseCombatLocation item) => item.Ambusher is { } ambusher && item.Rounds.Count > 0\n'
        '            && !state.Units.Any(unit => unit.Status == InstanceStatus.Active && unit.Side != ambusher && !Is(unit, Conditions.Captured) && state.Location(unit.Id)?.Location == item.Location);\n',
        f'        bool NothingLeft(CloseCombatLocation item) => {C}.NothingLeft(item.Ambusher is not null, item.Rounds.Count, item.Ambusher is {{ }} ambusher\n'
        '            && state.Units.Any(unit => unit.Status == InstanceStatus.Active && unit.Side != ambusher && !Is(unit, Conditions.Captured) && state.Location(unit.Id)?.Location == item.Location));\n',
    ),
    (
        '            due.Add(open.Next is { } next ? $"{open.Location}: the {next} side attacks or passes (A11.31)"\n'
        '                : open.Ambusher is null ? $"{open.Location}: its round after the Ambush drs (A11.12)"\n'
        '                : $"{open.Location}: more attacks by the {open.Ambusher} side, then the ambushed side\'s round, which may declare no attacks (A11.3, A11.32)");\n',
        f'            due.Add({C}.DueOpenLine(open.Location.ToString(), open.Next, open.Ambusher));\n',
    ),
    (
        '        due.AddRange(locations.Where(location => AmbushDue(state, location)).Select(location => AmbushUndecided(state, location) is { } why\n'
        '            ? $"{location}: the Close Combat package does not decide this Location, since {why}"\n'
        '            : $"{location}: the Ambush drs (A11.4)"));\n',
        f'        due.AddRange(locations.Where(location => AmbushDue(state, location)).Select(location => {C}.DueAmbushLine(location.ToString(), AmbushUndecided(state, location))));\n',
    ),
    (
        '            .Select(unit => $"{state.Location(unit.Id)!.Location}: {unit.Id} is broken in Melee and must attempt to withdraw (A11.16)"));\n',
        f'            .Select(unit => {C}.DueWithdrawLine(state.Location(unit.Id)!.Location.ToString(), unit.Id)));\n',
    ),
    (
        '        // Ruling R10.15: a charge into concealed units reveals them, one into prisoners enters, and one onto a lone SMC is an Infantry OVR; a Gun\'s\n'
        '        // crew in CC stays unreviewed (ruling R24.3).\n'
        '        if (enemies.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew")))\n'
        '        {\n'
        '            return $"play.berserk-crew: a charge into {to}, which holds a Gun\'s crew, is CC with a crew, which is not reviewed (C11, R24.3); the charge ends in place (ruling R30.5)";\n'
        '        }\n'
        '\n'
        '        // Table player, pass 27: CC between Infantry in a Location holding a vehicle is not built (R11.16), so a charge enters a vehicle\'s Location only\n'
        '        // when no enemy Infantry is there.\n'
        '        if (enemies.Any(LiveFire.IsVehicle) && enemies.Any(unit => !LiveFire.IsVehicle(unit)))\n'
        '        {\n'
        '            return $"play.berserk-vehicle: a charge into {to}, which holds an enemy vehicle and enemy Infantry, needs CC between Infantry beside a vehicle, which is not built (R11.16); the charge ends in place (ruling R30.5)";\n'
        '        }\n'
        '\n'
        '        return null;\n',
        '        // Ruling R10.15: a charge into concealed units reveals them, one into prisoners enters, and one onto a lone SMC is an Infantry OVR; a Gun\'s\n'
        '        // crew in CC stays unreviewed (ruling R24.3). Table player, pass 27: CC between Infantry in a Location holding a vehicle is not built (R11.16).\n'
        f'        return {C}.ChargeBarred(enemies.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew")), enemies.Any(LiveFire.IsVehicle), enemies.Any(unit => !LiveFire.IsVehicle(unit)), to.ToString());\n',
    ),
])
