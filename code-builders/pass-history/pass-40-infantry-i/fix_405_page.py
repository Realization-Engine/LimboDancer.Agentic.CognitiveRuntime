"""Pass 40 task 40.5: the page's words while the Mandatory FG's attack is owed: units by name, and no "or pass" in the window's note."""
path = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor"
text = open(path, encoding="utf-8", newline="").read()
edits = [
    ("@ScenarioA1MandatoryFire.Note(mandatory.Firers, PlaceOf(mandatory.At))",
     "@ScenarioA1MandatoryFire.Note([.. mandatory.Firers.Select(id => names?.Of(id) ?? id)], PlaceOf(mandatory.At))"),
    ("                            else\n                            {\n                                <span class=\"toolbar-note\" id=\"dff-note\">The moving stack is at @PlaceOf(movingAt): fire at it below, or pass (A8.1).</span>\n                            }\n",
     "                            else if (Live.Planner.MandatoryFire(state, history!.Events) is null)\n                            {\n                                <span class=\"toolbar-note\" id=\"dff-note\">The moving stack is at @PlaceOf(movingAt): fire at it below, or pass (A8.1).</span>\n                            }\n"),
]
for old, new in edits:
    assert text.count(old) == 1, (old[:60], text.count(old))
    text = text.replace(old, new)
open(path, "w", encoding="utf-8", newline="").write(text)
print("ok")
