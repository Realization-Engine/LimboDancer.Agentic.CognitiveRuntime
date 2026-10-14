"""Pass 40 task 40.5: when the units that owe the attack cannot fire as one group, the page offers each unit's attack and says so."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("LimboDancer.Domains.Asl.Rules/ScenarioA1MandatoryFire.cs", [
    ("    /// <summary>The page's note for the DEFENDER.</summary>\n"
     "    public static string Note(IReadOnlyList<string> owed, string place)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(owed);\n"
     "        return $\"Enemy Infantry has entered {place}: {string.Join(\", \", owed)} must attack it now as one Mandatory FG, before a pass (A8.312).\";\n"
     "    }\n",
     "    /// <summary>The page's note for the DEFENDER; the units fire each alone when their counters keep them out of one group.</summary>\n"
     "    public static string Note(IReadOnlyList<string> owed, bool groupPossible, string place)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(owed);\n"
     "        return $\"Enemy Infantry has entered {place}: {string.Join(\", \", owed)} must attack it now, \"\n"
     "            + (groupPossible ? \"as one Mandatory FG, \" : \"each alone since their fire counters differ, \") + \"before a pass (A8.312).\";\n"
     "    }\n"),
])
apply("LimboDancer.Domains.Asl.Play/GamePlanner.MandatoryFire.cs", [
    ("    /// <summary>The arguments of the plainest Mandatory FG attack, for the Play page's button.</summary>\n"
     "    public Dictionary<string, object> MandatoryFireArguments(GameState state, MandatoryFireOwed owed)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(state);\n"
     "        ArgumentNullException.ThrowIfNull(owed);\n"
     "        return MandatoryArguments(state, owed.Firers, owed.At);\n"
     "    }\n",
     "    /// <summary>The arguments of the plainest Mandatory FG attack, or of one unit's part of it, for the Play page's buttons.</summary>\n"
     "    public Dictionary<string, object> MandatoryFireArguments(GameState state, MandatoryFireOwed owed, string? only = null)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(state);\n"
     "        ArgumentNullException.ThrowIfNull(owed);\n"
     "        return MandatoryArguments(state, only is null ? owed.Firers : [only], owed.At);\n"
     "    }\n"),
])
apply("LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor", [
    ("@ScenarioA1MandatoryFire.Note([.. mandatory.Firers.Select(id => Words(id))], PlaceOf(mandatory.At))",
     "@ScenarioA1MandatoryFire.Note([.. mandatory.Firers.Select(id => Words(id))], mandatory.Group, PlaceOf(mandatory.At))"),
    ("                                <button id=\"propose-mandatory-fire\" title=\"Each unit fires its inherent FP, and a unit with none its MG. To add a MG or a leader's direction, name the same units in the fire panel below.\" @onclick=\"@(() => ProposeMandatoryFire(mandatory))\">Propose: the Mandatory FG attacks</button>\n",
     "                                @if (mandatory.Group)\n"
     "                                {\n"
     "                                    <button id=\"propose-mandatory-fire\" title=\"Each unit fires its inherent FP, and a unit with none its MG. To add a MG or a leader's direction, name the same units in the fire panel below.\" @onclick=\"@(() => ProposeMandatoryFire(mandatory))\">Propose: the Mandatory FG attacks</button>\n"
     "                                }\n"
     "                                else\n"
     "                                {\n"
     "                                    @foreach (var owing in mandatory.Firers)\n"
     "                                    {\n"
     "                                        <button class=\"propose-mandatory-fire-one\" data-unit=\"@owing\" @onclick=\"@(() => ProposeMandatoryFire(mandatory, owing))\">Propose: @Words(owing) attacks</button>\n"
     "                                    }\n"
     "                                }\n"),
    ("    private Task ProposeMandatoryFire(GamePlanner.MandatoryFireOwed owed)\n    {\n        var arguments = Live.Planner.MandatoryFireArguments(state!, owed);",
     "    private Task ProposeMandatoryFire(GamePlanner.MandatoryFireOwed owed, string? only = null)\n    {\n        var arguments = Live.Planner.MandatoryFireArguments(state!, owed, only);"),
])
print("ok")
