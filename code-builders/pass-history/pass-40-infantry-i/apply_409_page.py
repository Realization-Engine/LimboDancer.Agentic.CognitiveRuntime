"""Pass 40 task 40.9: the page's button for a Guard squad's Deployment at will (A20.5), outside the RPh, and the help of the Deploy panel."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("Pages/Play.razor", [
    ("                @if (state.Phase == \"rph\")\n                {\n                    <ActionSection Title=\"Rally\" Key=\"rally\">\n",
     "                @* Pass 40 (A20.5, p. 88): an unbroken Guard squad Deploys at will, in any phase; in the RPh the Deploy panel takes it. *@\n"
     "                @if (state.Phase != \"rph\" && GuardSquads is { Count: > 0 } guardSquads)\n"
     "                {\n"
     "                    <ActionSection Title=\"A Guard's Deployment\" Key=\"guard-deploy\">\n"
     "                    <div class=\"toolbar\" id=\"guard-deploy\">\n"
     "                        <span class=\"toolbar-note\">An unbroken Guard squad Deploys into two HS at any time, with no leader and no NTC (A20.5).</span>\n"
     "                        @foreach (var guard in guardSquads)\n"
     "                        {\n"
     "                            <button class=\"propose-guard-deploy\" data-unit=\"@guard\" @onclick=\"@(() => ProposeGuardDeploy(guard))\">Propose: @WordsAt(guard) Deploys</button>\n"
     "                        }\n"
     "                    </div>\n"
     "                    </ActionSection>\n"
     "                }\n"
     "                @if (state.Phase == \"rph\")\n                {\n                    <ActionSection Title=\"Rally\" Key=\"rally\">\n"),
    ("    private Task ProposeDeploy()\n    {\n",
     "    /// <summary>The viewer's unbroken squads that guard prisoners (A20.5; pass 40): each may Deploy at will.</summary>\n"
     "    private IReadOnlyList<string> GuardSquads => state is null ? []\n"
     "        : [.. ActingUnits.Where(unit => unit.Status == InstanceStatus.Active && unit.Kind == \"asl:squad\" && state.Location(unit.Id) is not null\n"
     "                && (Viewer.IsAdjudicator || unit.Side == viewAs) && GameState.Condition(unit, Conditions.Broken) != ConditionState.True\n"
     "                && state.Units.Any(prisoner => prisoner.Status == InstanceStatus.Active && prisoner.Custodian == unit.Id))\n"
     "            .Select(unit => unit.Id)];\n\n"
     "    private Task ProposeGuardDeploy(string squad) => Propose(GameActions.Deploy, JsonSerializer.SerializeToElement(new Dictionary<string, object>\n"
     "    {\n"
     "        [\"gameId\"] = gameId, [\"attemptId\"] = Attempt(\"deploy\"), [\"expectedRevision\"] = (long)history!.Events.Count, [\"squadId\"] = squad,\n"
     "    }));\n\n"
     "    private Task ProposeDeploy()\n    {\n"),
])
apply("Play/DeployActionPanel.razor", [
    ("    A squad waiting off board may attempt to Deploy from its entry turn's RPh with a leader waiting to enter along the same edge (A2.52; ruling R25.4).\n",
     "    A squad waiting off board may attempt to Deploy from its entry turn's RPh with a leader waiting to enter along the same edge (A2.52; ruling R25.4).\n"
     "    A Guard of prisoners and an Unarmed unit need no leader: leave the leader unchosen. HS Recombine at the start of the RPh, before their side's other RPh actions (A1.32).\n"),
])
print("ok")
