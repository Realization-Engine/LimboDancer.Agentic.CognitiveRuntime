"""Pass 40: the page's fixes after the five reviews and the Studio check. Exact anchors, asserted."""
import re

ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/"
path = ROOT + "Pages/Play.razor"
text = open(path, encoding="utf-8", newline="").read()


def once(old, new):
    global text
    assert text.count(old) == 1, (old[:90], text.count(old))
    text = text.replace(old, new)


# UI 1: the leader's IPC name is cleared with the other move drafts, and is sent only when a stack moves.
text, count = re.subn(r"(\r?\n)([ \t]+)moveOverrun = false;", lambda m: f"{m.group(1)}{m.group(2)}moveOverrun = false;{m.group(1)}{m.group(2)}moveIpcTo = string.Empty;", text)
assert count == 4, count
once("        if (moveIpcTo.Length > 0 && moveUnits.Contains(moveIpcTo))", "        if (moveIpcTo.Length > 0 && moveUnits.Count > 1 && moveUnits.Contains(moveIpcTo))")
once("        Toggle(moveUnits, toggle.Id, toggle.On);\n        PruneMoveDrafts();\n    }\n\n    private void ToggleAdvancer",
     "        Toggle(moveUnits, toggle.Id, toggle.On);\n        if (!moveUnits.Contains(moveIpcTo))\n        {\n            moveIpcTo = string.Empty;\n        }\n\n        PruneMoveDrafts();\n    }\n\n    private void ToggleAdvancer")

# UI 2 to 4: the attack owed is read once a render; the adjudicator has its note and buttons and no pass; the stopped stack's note does not offer a pass.
once("                            <MovementWindowActions WindowOpen=\"@state.Movement?.WindowOpen\" EndingSome=\"@EndingSome\" MayPass=\"@Viewer.IsAdjudicator\"",
     "                            <MovementWindowActions WindowOpen=\"@state.Movement?.WindowOpen\" EndingSome=\"@EndingSome\" MayPass=\"@(Viewer.IsAdjudicator && MandatoryNow is null)\"")
start = text.index("                    @* Pass 28c (A8.1, A8.11): after each step the DEFENDER, from its own view, sees where the stack is and fires at it or passes. *@")
end = text.index("                        </ActionSection>\n                    }\n", start) + len("                        </ActionSection>\n                    }\n")
block = text[start:end]
assert "id=\"dff-window\"" in block and "propose-mandatory-fire-one" in block
note_start = block.index("<span class=\"toolbar-note\" id=\"dff-note\">No member of the moving stack")
stopped_line = block[note_start:block.index("\n", note_start)]
moving_line = "<span class=\"toolbar-note\" id=\"dff-note\">The moving stack is at @PlaceOf(movingAt): fire at it below, or pass (A8.1).</span>"
assert moving_line in block
NEW = (
    "                    @* Pass 28c (A8.1, A8.11): after each step the DEFENDER, from its own view, sees where the stack is and fires at it or passes.\n"
    "                       Pass 40 (A8.312): the attack owed in the DEFENDER's own Location comes before a pass; the adjudicator has the same block. *@\n"
    "                    @if (state.Phase == \"mph\" && MovingAt is { } movingAt && (Viewer.IsAdjudicator ? MandatoryNow is not null : viewAs != state.PhasingSide))\n"
    "                    {\n"
    "                        var mandatory = MandatoryNow;\n"
    "                        <ActionSection Title=\"Defensive First Fire\" Key=\"defensive-first-fire\">\n"
    "                        <div class=\"toolbar\" id=\"dff-window\">\n"
    "                            @if (mandatory is not null)\n"
    "                            {\n"
    "                                <span class=\"toolbar-note\" id=\"mandatory-fire-note\">@ScenarioA1MandatoryFire.Note([.. mandatory.Firers.Select(id => Words(id))], mandatory.Group, PlaceOf(mandatory.At))</span>\n"
    "                            }\n"
    "                            else if (MoveStopped)\n"
    "                            {\n"
    "                                " + stopped_line + "\n"
    "                            }\n"
    "                            else\n"
    "                            {\n"
    "                                " + moving_line + "\n"
    "                            }\n"
    "                            <button type=\"button\" id=\"dff-show\" @onclick=\"@(() => LocateHex(movingAt))\">Show it on the map</button>\n"
    "                            @if (mandatory is not null && mandatory.Group)\n"
    "                            {\n"
    "                                <button id=\"propose-mandatory-fire\" title=\"Each unit fires its inherent FP; one already marked fires its MG with it, and a unit with no FP of its own its MG. To add a MG or a leader's direction, name the same units in the fire panel below.\" @onclick=\"@(() => ProposeMandatoryFire(mandatory))\">Propose: the Mandatory FG attacks</button>\n"
    "                            }\n"
    "                            else if (mandatory is not null)\n"
    "                            {\n"
    "                                @foreach (var owing in mandatory.Firers)\n"
    "                                {\n"
    "                                    <button class=\"propose-mandatory-fire-one\" data-unit=\"@owing\" @onclick=\"@(() => ProposeMandatoryFire(mandatory, owing))\">Propose: @Words(owing) attacks</button>\n"
    "                                }\n"
    "                            }\n"
    "                            else if (!Viewer.IsAdjudicator)\n"
    "                            {\n"
    "                                <button id=\"propose-pass\" @onclick='@(() => ProposeSimple(GameActions.PassFire, \"pass\"))'>Propose: the DEFENDER passes</button>\n"
    "                            }\n"
    "                        </div>\n"
    "                        </ActionSection>\n"
    "                    }\n")
text = text[:start] + NEW + text[end:]

# The attack owed, read once for a revision and a view.
once("    private Task ProposeMandatoryFire(GamePlanner.MandatoryFireOwed owed, string? only = null)\n",
     "    private (long Revision, GamePlanner.MandatoryFireOwed? Owed)? mandatoryCache;\n\n"
     "    /// <summary>The attack the DEFENDER owes now (A8.312; pass 40), read once for a revision: the planner asks the fire plan of each unit.</summary>\n"
     "    private GamePlanner.MandatoryFireOwed? MandatoryNow\n"
     "    {\n"
     "        get\n"
     "        {\n"
     "            if (state is null || history is null)\n"
     "            {\n"
     "                return null;\n"
     "            }\n\n"
     "            if (mandatoryCache is not { } kept || kept.Revision != history.Events.Count)\n"
     "            {\n"
     "                mandatoryCache = (history.Events.Count, Live.Planner.MandatoryFire(state, history.Events));\n"
     "            }\n\n"
     "            return mandatoryCache.Value.Owed;\n"
     "        }\n"
     "    }\n\n"
     "    private Task ProposeMandatoryFire(GamePlanner.MandatoryFireOwed owed, string? only = null)\n")

# The Studio check: the hand-over prompt says what is owed.
once("The {DisplayText.Side(handOver)} side, as the DEFENDER, may fire at it or pass (A8.1).\"",
     "The {DisplayText.Side(handOver)} side, as the DEFENDER, {(MandatoryNow is null ? \"may fire at it or pass (A8.1)\" : \"must attack it from the Location it entered (A8.312)\")}.\"")
once("                    ? [$\"Fire at the moving stack in {PlaceOf(moving.Location)} with your units that see it, or pass.\", \"Hand the screen back once you have passed.\"]",
     "                    ? [MandatoryNow is null ? $\"Fire at the moving stack in {PlaceOf(moving.Location)} with your units that see it, or pass.\" : ScenarioA1MandatoryFire.HandOverText(PlaceOf(moving.Location)),\n"
     "                        \"Hand the screen back once you have passed.\"]")

# Table player 9: a lone leader has no FP to fire with.
old_note = text[text.index("<span class=\"toolbar-note\">An Infantry OVR has entered the SMC's Location: it fires, or moves away"):]
old_note = old_note[:old_note.index("</span>") + len("</span>")]
once(old_note, "<span class=\"toolbar-note\">An Infantry OVR has entered the SMC's Location: it may attack if it has FP (a leader with no SW has none), move away before any fire into an adjacent Location the ATTACKER names, or stay for the CC (A4.151, A4.152).</span>")

# UI 7 and boundary 7: the Guard's block says where the SW go, and the squads come from the planner's reading of A20.5.
once("with no leader and no NTC (A20.5).</span>", "with no leader and no NTC; its SW stay with the first HS (A20.5).</span>")
gs = text.index("    private IReadOnlyList<string> GuardSquads => state is null ? []")
ge = text.index("            .Select(unit => unit.Id)];\n", gs) + len("            .Select(unit => unit.Id)];\n")
text = text[:gs] + ("    private IReadOnlyList<string> GuardSquads => state is null ? []\n"
                    "        : [.. Live.Planner.GuardSquadsAtWill(state, Viewer.IsAdjudicator ? null : viewAs).Where(id => ActingUnits.Any(unit => unit.Id == id))];\n") + text[ge:]
open(path, "w", encoding="utf-8", newline="").write(text)

path = ROOT + "Play/InfantryMovementAction.razor"
panel = open(path, encoding="utf-8", newline="").read()
old = "In the road Location the DEFENDER's small arms fire at it is Area Fire for firers to whom both sides of the road are not Open Ground."
assert panel.count(old) == 1
panel = panel.replace(old, "In the road Location the DEFENDER's non-ordnance Defensive First Fire at it is Area Fire for a firer to whom neither side of the road is Open Ground.")
open(path, "w", encoding="utf-8", newline="").write(panel)
print("ok")
