"""Pass 40 task 40.9, first part: Deployment and Recombining without a leader for a Guard and an Unarmed unit, a Guard squad's Deployment at
will (A20.5), and Recombining at the start of the RPh (A1.31, A1.32). Exact anchors, asserted once each."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("LimboDancer.Domains.Asl.Rules/ScenarioA1SupportWeaponRules.cs", [
    ("    /// <summary>A1.32: the squad is Fanatic only when both HS are.</summary>\n",
     "    /// <summary>\n"
     "    /// A1.31, A1.32 (p. 45; pass 40): \"Guards ... [and] unarmed units (20.5) ... do not require leader direction to Deploy\", nor \"leader presence to\n"
     "    /// Recombine\". The Guards are the Guards of prisoners (ruling R31.4). Finns, Carrier HS, and U.S.M.C. 7-6-8s are not in the catalog.\n"
     "    /// </summary>\n"
     "    public static bool NeedsNoLeader(bool guard, bool unarmed) => guard || unarmed;\n\n"
     "    /// <summary>\n"
     "    /// A20.5 (p. 88; pass 40): \"An unbroken Guard squad can Deploy into HS automatically at any time, regardless of nationality or leader\n"
     "    /// requirements\": in any phase, with no NTC.\n"
     "    /// </summary>\n"
     "    public static bool GuardDeploysAtWill(bool squad, bool guard, bool broken) => squad && guard && !broken;\n\n"
     "    /// <summary>\n"
     "    /// A1.32 (p. 45; pass 40): HS Recombine \"at the start of any RPh\", so not once a unit of their side has attempted a Rally, a Repair, a\n"
     "    /// Deployment, a Recovery, or a Transfer in it. Null when they may.\n"
     "    /// </summary>\n"
     "    public static string? RecombineStartBar(bool sideHasActed) => sideHasActed\n"
     "        ? \"play.recombine-start: HS Recombine at the start of the RPh, before any other RPh action of their side (A1.32)\" : null;\n\n"
     "    /// <summary>A1.32: the squad is Fanatic only when both HS are.</summary>\n"),
])

apply("LimboDancer.Domains.Asl.Play/GamePlanner.SupportWeapons.cs", [
    # The stale note above the leader's citation.
    ("    /// (A20.5), not the Russian Guards squads, so no squad Deploys or Recombines without a leader for being of a Guards formation. A Guard of\n"
     "    /// prisoners Deploying at will (A20.5) is not built.\n",
     "    /// (A20.5), not the Russian Guards squads, so no squad Deploys or Recombines without a leader for being of a Guards formation. Pass 40: a Guard\n"
     "    /// of prisoners and an Unarmed unit need no leader, and a Guard squad Deploys at will (A20.5).\n"),
    ("    /// <summary>A25.2 (ruling R31.4): Russian squads may not Deploy [EXC: a Guard of prisoners, A20.5, and a temporary crew, A21.22, neither built].</summary>\n",
     "    /// <summary>A25.2 (ruling R31.4): Russian squads may not Deploy [EXC: a Guard of prisoners, A20.5 (pass 40), and a temporary crew, A21.22, not built].</summary>\n"),
    ("    /// <summary>The SW a unit possesses, in id order.</summary>\n",
     "    /// <summary>Whether a unit guards prisoners (A20.5).</summary>\n"
     "    private static bool IsGuard(GameState state, UnitInstance unit) =>\n"
     "        state.Units.Any(prisoner => prisoner.Status == InstanceStatus.Active && prisoner.Custodian == unit.Id);\n\n"
     "    /// <summary>\n"
     "    /// Whether a unit of a side has taken a RPh action other than Recombining since the RPh began (A1.32, \"at the start of any RPh\"; pass 40):\n"
     "    /// a Rally, Repair, Deployment, or Recovery attempt, or a Transfer.\n"
     "    /// </summary>\n"
     "    private static bool SideActedThisRph(GameState state, IReadOnlyList<GameEvent> existing, string side)\n"
     "    {\n"
     "        var start = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged).index;\n"
     "        return existing.Skip(start).Select(item => item.Payload).SelectMany(payload => payload switch\n"
     "        {\n"
     "            RallyPhaseActionTaken taken when taken.Action is not (\"recombine\" or \"recombined\") => taken.Units,\n"
     "            DeploymentAttempted attempt => attempt.Leader is { } directing ? [attempt.Squad, directing] : [attempt.Squad],\n"
     "            RallyAttempted rally => [rally.Unit],\n"
     "            RepairAttempted repair => [repair.Unit],\n"
     "            RecoveryAttempted recovery => [recovery.Unit],\n"
     "            _ => (IEnumerable<string>)[],\n"
     "        }).Any(id => state.Unit(id)?.Side == side);\n"
     "    }\n\n"
     "    /// <summary>The SW a unit possesses, in id order.</summary>\n"),
    # Deployment.
    ("        if (state.Phase != \"rph\")\n"
     "        {\n"
     "            return Refused(scope, label, expected, \"play.deploy-phase: squads Deploy in the RPh (A1.31)\");\n"
     "        }\n\n"
     "        if (state.Unit(squadId) is not { } squad || !vocabulary.IsA(squad.Kind, \"asl:squad\") || !FreeToAct(squad)\n",
     "        // Pass 40 (A20.5, p. 88): an unbroken Guard squad Deploys at will: in any phase, of any nationality, with no leader and no NTC.\n"
     "        var atWill = state.Unit(squadId) is { Status: InstanceStatus.Active } named && state.Location(named.Id) is not null\n"
     "            && ScenarioA1SupportWeaponRules.GuardDeploysAtWill(vocabulary.IsA(named.Kind, \"asl:squad\"), IsGuard(state, named), Is(named, Conditions.Broken));\n"
     "        if (state.Phase != \"rph\" && !atWill)\n"
     "        {\n"
     "            return Refused(scope, label, expected, \"play.deploy-phase: squads Deploy in the RPh (A1.31)\");\n"
     "        }\n\n"
     "        if (state.Unit(squadId) is not { } squad || !vocabulary.IsA(squad.Kind, \"asl:squad\") || (!atWill && !FreeToAct(squad))\n"),
    ("        if (squad.Side != state.PhasingSide)\n        {\n            return Refused(scope, label, expected, $\"play.deploy-phase: {squad.Id} Deploys in its own side's RPh (A1.31)\");",
     "        if (squad.Side != state.PhasingSide && !atWill)\n        {\n            return Refused(scope, label, expected, $\"play.deploy-phase: {squad.Id} Deploys in its own side's RPh (A1.31)\");"),
    ("        if (MayNotDeploy(squad))\n", "        if (MayNotDeploy(squad) && !atWill)\n"),
    ("        if (RallyPhaseActionBar(state, squad.Id) is { } squadBar)\n", "        if (!atWill && RallyPhaseActionBar(state, squad.Id) is { } squadBar)\n"),
    ("        var (leader, leaderReason) = DirectingLeader(state, existing, squad, at, Text(arguments, \"leader\", out var named) ? named : null, edge: waiting?.Edge);\n",
     "        // Pass 40 (A1.31): a Guard and an Unarmed squad need no leader's direction; one named directs all the same.\n"
     "        var leaderId = Text(arguments, \"leader\", out var directing) ? directing : null;\n"
     "        var noLeader = atWill || (leaderId is null && ScenarioA1SupportWeaponRules.NeedsNoLeader(IsGuard(state, squad), Is(squad, Conditions.Unarmed)));\n"
     "        var (leader, leaderReason) = noLeader ? (null, null) : DirectingLeader(state, existing, squad, at, leaderId, edge: waiting?.Edge);\n"),
    ("        void AddDeployed(List<GameEvent> events, string? cause)\n", "        void AddDeployed(List<GameEvent> events, string? cause, bool rphAction = true)\n"),
    ("            events.Add(Event(scope, attemptId, events.Count + 1, expected, \"rph-action-taken\", new RallyPhaseActionTaken([first, other], \"deployed\"), package, null));\n        }\n",
     "            if (rphAction)\n"
     "            {\n"
     "                events.Add(Event(scope, attemptId, events.Count + 1, expected, \"rph-action-taken\", new RallyPhaseActionTaken([first, other], \"deployed\"), package, null));\n"
     "            }\n"
     "        }\n\n"
     "        if (atWill)\n"
     "        {\n"
     "            var deployed = new List<GameEvent>();\n"
     "            AddDeployed(deployed, null, rphAction: state.Phase == \"rph\");\n"
     "            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, deployed,\n"
     "                [$\"play.deploy: {squad.Id}, a Guard, Deploys into two HS, with no leader and no NTC (A20.5)\"]);\n"
     "        }\n"),
    ("{(leader is null ? \", a Guards squad with no leader\" : $\" for {leader.Id}\")}", "{(leader is null ? \", with no leader's direction\" : $\" for {leader.Id}\")}"),
    # Recombining.
    ("        var (leader, leaderReason) = DirectingLeader(state, existing, one, at, Text(arguments, \"leader\", out var named) ? named : null, recombining: true);\n",
     "        // Pass 40 (A1.32): \"at the start of any RPh\"; and Guards and Unarmed HS \"do not need leader presence\".\n"
     "        if (ScenarioA1SupportWeaponRules.RecombineStartBar(SideActedThisRph(state, existing, one.Side)) is { } startBar)\n"
     "        {\n"
     "            return Refused(scope, label, expected, startBar);\n"
     "        }\n\n"
     "        var leaderId = Text(arguments, \"leader\", out var directing) ? directing : null;\n"
     "        var noLeader = leaderId is null && ScenarioA1SupportWeaponRules.NeedsNoLeader(IsGuard(state, one), Is(one, Conditions.Unarmed))\n"
     "            && ScenarioA1SupportWeaponRules.NeedsNoLeader(IsGuard(state, two), Is(two, Conditions.Unarmed));\n"
     "        var (leader, leaderReason) = noLeader ? (null, null) : DirectingLeader(state, existing, one, at, leaderId, recombining: true);\n"),
])
print("ok")
