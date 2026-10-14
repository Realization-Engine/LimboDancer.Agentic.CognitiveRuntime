"""The SMC's "moves away" button, for the DEFENDER's view and the adjudicator's (it sat in the DEFENDER-only section). Exact blocks, asserted once each."""
path = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor"
text = open(path, encoding="utf-8", newline="").read()
inner = ("                            @if (Live.Planner.OverrunEvader(state, history!.Events) is { } evader)\n"
         "                            {\n"
         "                                <button id=\"propose-overrun-evade\" title=\"A4.151: before any Defensive First Fire at the units that entered, the SMC may move away into an adjacent Location of the ATTACKER's choice, free of all fire.\" @onclick=\"@(() => ProposeOverrunEvade(evader))\">Propose: the SMC moves away from the OVR</button>\n"
         "                            }\n")
assert text.count(inner) == 1
text = text.replace(inner, "")
anchor = "                    @* Pass 28c (A8.1, A8.11): after each step the DEFENDER, from its own view, sees where the stack is and fires at it or passes. *@\n"
assert text.count(anchor) == 1
block = ("                    @* Pass 40 (A4.151): the SMC an Infantry OVR has just entered upon may move away before any fire; the DEFENDER's to declare. *@\n"
         "                    @if (state.Phase == \"mph\" && (Viewer.IsAdjudicator || viewAs != state.PhasingSide) && Live.Planner.OverrunEvader(state, history!.Events) is { } evader)\n"
         "                    {\n"
         "                        <div class=\"toolbar\" id=\"overrun-evade\">\n"
         "                            <span class=\"toolbar-note\">An Infantry OVR has entered the SMC's Location: it fires, or moves away before any fire into an adjacent Location the ATTACKER names (A4.151).</span>\n"
         "                            <button id=\"propose-overrun-evade\" @onclick=\"@(() => ProposeOverrunEvade(evader))\">Propose: the SMC moves away from the OVR</button>\n"
         "                        </div>\n"
         "                    }\n")
text = text.replace(anchor, block + anchor)
open(path, "w", encoding="utf-8", newline="").write(text)
print("ok")
