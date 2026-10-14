"""Pass 40 task 40.4, Infantry OVR (A4.15 to A4.152, A20.54): the wiring outside the two new files.

Exact blocks with asserted counts, all checked before any file is written.
"""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
PENDING = {}


def apply(path, edits):
    text = PENDING.get(path) or open(ROOT + path, encoding="utf-8", newline="").read()
    for edit in edits:
        old, new = edit[0], edit[1]
        count = edit[2] if len(edit) > 2 else 1
        assert text.count(old) == count, (path, old[:90], text.count(old))
        text = text.replace(old, new)
    PENDING[path] = text


R = "LimboDancer.Domains.Asl.Rules/"
P = "LimboDancer.Domains.Asl.Play/"
U = "LimboDancer.Domains.Asl.Units/State/"
M = "LimboDancer.Domains.Asl.MapStudio/"

# ---- Units: the records, the writer and reader, the state, the projector -------------------------------------------------------
apply(U + "LiveRecords.cs", [
    ("public sealed record PaatcTaken(IReadOnlyList<string> Units, string Vehicle, string Roll, int Morale, int Drm, bool Passed) : EventPayload;\n",
     "public sealed record PaatcTaken(IReadOnlyList<string> Units, string Vehicle, string Roll, int Morale, int Drm, bool Passed) : EventPayload;\n\n"
     "/// <summary>\n"
     "/// <c>infantry-overrun-ntc</c>: the NTC of an Infantry OVR (A4.15, p. 49; pass 40): the stack it is taken for, the unit that takes it (the stack's\n"
     "/// leader, or the one MMC), the Location the stack would enter, the roll, the Morale Level, the DRM (the TEM there), and the result.\n"
     "/// </summary>\n"
     "public sealed record InfantryOverrunNtc(IReadOnlyList<string> Units, string Taker, BoardLocation Target, string Roll, int Morale, int Drm, bool Passed) : EventPayload;\n\n"
     "/// <summary>A NTC of an Infantry OVR taken in this phase, as the state keeps it: the stack, the Location, and the result (A4.15; pass 40).</summary>\n"
     "public sealed record InfantryOverrunNtcTaken(IReadOnlyList<string> Units, string Target, bool Passed);\n"),
    ("    public string? IpcTo\n    {\n        get; init;\n    }\n\n    /// <summary>\n    /// Whether the step is a Minimum Move (A4.134; ruling R10.9): the movers' only hex this MPh;",
     "    public string? IpcTo\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>\n"
     "    /// Whether the step enters a Location of enemy units in the MPh as an Infantry OVR onto a lone Known SMC, or onto Unarmed units to recapture\n"
     "    /// them (A4.15, A20.54; pass 40): its CC follows at once when the DEFENDER's window closes.\n"
     "    /// </summary>\n"
     "    public bool InfantryOverrun\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>\n    /// Whether the step is a Minimum Move (A4.134; ruling R10.9): the movers' only hex this MPh;"),
    ("    public bool ChangedLocation\n    {\n        get; init;\n    }\n",
     "    public bool ChangedLocation\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>The Location the stack's latest step entered as an Infantry OVR or to recapture Unarmed units (A4.15, A20.54; pass 40); null otherwise.</summary>\n"
     "    public BoardLocation? InfantryOverrun\n    {\n        get; init;\n    }\n"),
])
apply(U + "ChoiceRecords.cs", [
    ("    public const string Paatc = \"paatc\";\n",
     "    public const string Paatc = \"paatc\";\n\n"
     "    /// <summary>\n"
     "    /// A4.151 (pass 40): a SMC moves away from an Infantry OVR into an adjacent Location \"of the ATTACKER's choice\"; the options are the Locations.\n"
     "    /// </summary>\n"
     "    public const string OverrunEvade = \"overrun-evade\";\n"),
])
apply(U + "GameEventWriter.cs", [
    ("                writer.WriteBoolean(\"passed\", paatc.Passed);\n                break;\n",
     "                writer.WriteBoolean(\"passed\", paatc.Passed);\n                break;\n"
     "            case InfantryOverrunNtc ntc:\n"
     "                Strings(writer, \"units\", ntc.Units);\n"
     "                writer.WriteString(\"taker\", ntc.Taker);\n"
     "                writer.WriteString(\"target\", ntc.Target.ToString());\n"
     "                writer.WriteString(\"roll\", ntc.Roll);\n"
     "                writer.WriteNumber(\"morale\", ntc.Morale);\n"
     "                writer.WriteNumber(\"drm\", ntc.Drm);\n"
     "                writer.WriteBoolean(\"passed\", ntc.Passed);\n"
     "                break;\n"),
    ("                if (moving.IpcTo is { } ipcTo)\n                {\n                    writer.WriteString(\"ipcTo\", ipcTo);\n                }\n",
     "                if (moving.IpcTo is { } ipcTo)\n                {\n                    writer.WriteString(\"ipcTo\", ipcTo);\n                }\n\n"
     "                if (moving.InfantryOverrun)\n                {\n                    writer.WriteBoolean(\"infantryOverrun\", true);\n                }\n"),
])
apply(U + "GameEventReader.cs", [
    ("            case \"movement-window-closed\":\n",
     "            case \"infantry-overrun-ntc\":\n"
     "                var ntcTaker = fields.RequiredString(payload, \"taker\", path);\n"
     "                var ntcTarget = ReadLocation(payload, \"target\", path, fields, diagnostics);\n"
     "                var ntcRoll = fields.RequiredString(payload, \"roll\", path);\n"
     "                var ntcMorale = fields.OptionalInteger(payload, \"morale\", path);\n"
     "                var ntcDrm = fields.OptionalInteger(payload, \"drm\", path);\n"
     "                return ntcTaker is null || ntcTarget is null || ntcRoll is null || ntcMorale is null || ntcDrm is null\n"
     "                    ? Missing(diagnostics, \"An Infantry OVR's NTC names its units, taker, target, roll, Morale Level, and DRM.\", path)\n"
     "                    : new InfantryOverrunNtc(fields.StringList(payload, \"units\", path), ntcTaker, ntcTarget, ntcRoll, ntcMorale.Value, ntcDrm.Value,\n"
     "                        fields.OptionalBoolean(payload, \"passed\", path));\n"
     "            case \"movement-window-closed\":\n"),
    ("                        IpcTo = fields.OptionalString(payload, \"ipcTo\", path),\n",
     "                        IpcTo = fields.OptionalString(payload, \"ipcTo\", path),\n"
     "                        InfantryOverrun = fields.OptionalBoolean(payload, \"infantryOverrun\", path),\n"),
])
apply(U + "GameState.cs", [
    ("    public IReadOnlyList<(string Unit, string Weapon)> PortagedThisPhase { get; init; } = [];\n",
     "    public IReadOnlyList<(string Unit, string Weapon)> PortagedThisPhase { get; init; } = [];\n\n"
     "    /// <summary>The NTCs of Infantry OVRs taken in this phase (A4.15; pass 40): a stack enters as an OVR only on a passed one, and takes one a Location.</summary>\n"
     "    public IReadOnlyList<InfantryOverrunNtcTaken> InfantryOverrunNtcs { get; init; } = [];\n"),
])
apply(U + "GameProjector.cs", [
    ("                PaatcTaken paatc => TakePaatc(previous, paatc),\n",
     "                PaatcTaken paatc => TakePaatc(previous, paatc),\n                InfantryOverrunNtc ntc => TakeOverrunNtc(previous, ntc),\n"),
    ("        private GameState? CloseWindow(GameState state, MovementWindowClosed closed)\n",
     "        /// <summary>\n"
     "        /// The NTC of an Infantry OVR (A4.15; pass 40): the record against its roll; the state keeps it for the phase, and a leader's failure ends\n"
     "        /// his stack's MPh.\n"
     "        /// </summary>\n"
     "        private GameState? TakeOverrunNtc(GameState state, InfantryOverrunNtc ntc)\n"
     "        {\n"
     "            var taker = Active(state, ntc.Taker) as UnitInstance;\n"
     "            if (ntc.Units.Count == 0 || ntc.Units.Any(id => Active(state, id) is not UnitInstance) || !ntc.Units.Contains(ntc.Taker, StringComparer.Ordinal) || taker is null)\n"
     "            {\n"
     "                return Fail<GameState>(\"UNIT-STATE-039\", \"An Infantry OVR's NTC names active units and one of them as its taker (A4.15).\");\n"
     "            }\n\n"
     "            if (!rolls.TryGetValue(ntc.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6\n"
     "                || Rules.ScenarioA1InfantryOverrun.NtcPassed(roll.Values[0], roll.Values[1], ntc.Drm, ntc.Morale) != ntc.Passed)\n"
     "            {\n"
     "                return Fail<GameState>(\"UNIT-STATE-039\", \"The NTC's result disagrees with its DR (A4.15).\");\n"
     "            }\n\n"
     "            GameState? next = state with\n"
     "            {\n"
     "                InfantryOverrunNtcs = [.. state.InfantryOverrunNtcs, new InfantryOverrunNtcTaken(ntc.Units, ntc.Target.ToString(), ntc.Passed)]\n"
     "            };\n"
     "            if (Rules.ScenarioA1InfantryOverrun.EndsStackMove(vocabulary.IsA(taker.Kind, \"asl:leader\"), ntc.Passed))\n"
     "            {\n"
     "                foreach (var id in ntc.Units)\n"
     "                {\n"
     "                    next = next is not null && Active(next, id) is UnitInstance unit ? Replace(next, unit with { MovementEnded = true }) : next;\n"
     "                }\n"
     "            }\n\n"
     "            return next;\n"
     "        }\n\n"
     "        private GameState? CloseWindow(GameState state, MovementWindowClosed closed)\n"),
    ("pending.Kind is ChoicePending.LeaderCreation or ChoicePending.BattleHardening or ChoicePending.UnlikelyKill or ChoicePending.Acquisition or ChoicePending.Paatc,",
     "pending.Kind is ChoicePending.LeaderCreation or ChoicePending.BattleHardening or ChoicePending.UnlikelyKill or ChoicePending.Acquisition or ChoicePending.Paatc\n"
     "                    or ChoicePending.OverrunEvade,"),
    ("                    IpcTo = moving.IpcTo ?? current?.IpcTo,\n",
     "                    IpcTo = moving.IpcTo ?? current?.IpcTo,\n                    InfantryOverrun = moving.InfantryOverrun ? moving.To : null,\n"),
    ("                    PortagedThisPhase = [],\n", "                    PortagedThisPhase = [],\n                    InfantryOverrunNtcs = [],\n"),
])

# ---- Rules ----------------------------------------------------------------------------------------------------------------------
apply(R + "ScenarioA1MovementCalculator.cs", [
    ("    public static MoveEnemiesVerdict EnemiesAtTarget(IReadOnlyList<UnitAtTargetFacts> there, string? phasingSide, bool charge, bool occupy, bool bypassing, bool allDummies, bool minimumMove,\n"
     "        string idsText, string toText)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(there);\n"
     "        string[] enemiesThere = charge || occupy || bypassing ? []\n"
     "            : [.. there.Where(unit => unit.Active && unit.Side != phasingSide && !unit.Captured).Select(unit => unit.Id)];\n",
     "    public static MoveEnemiesVerdict EnemiesAtTarget(IReadOnlyList<UnitAtTargetFacts> there, string? phasingSide, bool charge, bool occupy, bool bypassing, bool allDummies, bool minimumMove,\n"
     "        string idsText, string toText, bool knownEntered = false)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(there);\n\n"
     "        // A4.15, A20.54 (pass 40): an Infantry OVR enters the Location of its lone Known SMC, and any Infantry that of Unarmed units; the Known\n"
     "        // units are then no bar and are not drawn for a reveal, while concealed units there still are (A12.15).\n"
     "        string[] enemiesThere = charge || occupy || bypassing ? []\n"
     "            : [.. there.Where(unit => unit.Active && unit.Side != phasingSide && !unit.Captured && (!knownEntered || !unit.Known)).Select(unit => unit.Id)];\n"),
    ("            + (facts.DashText is { } dashed && !forcedBack ? ScenarioA1Dash.DeclaredText(dashed) : string.Empty)\n",
     "            + (facts.DashText is { } dashed && !forcedBack ? ScenarioA1Dash.DeclaredText(dashed) : string.Empty)\n"
     "            + ((facts.InfantryOverrun || facts.Recapture) && !forcedBack ? ScenarioA1InfantryOverrun.EntryText(facts.Recapture) : string.Empty)\n"),
])
apply(R + "ScenarioA1MovementModels.cs", [
    ("    bool OnRoad = false,\n    string? DashText = null);", "    bool OnRoad = false,\n    string? DashText = null,\n    bool InfantryOverrun = false,\n    bool Recapture = false);"),
])
apply(R + "ScenarioA1SequenceCalculator.cs", [
    ("            ? $\"play.cc-overrun-first: the berserk Infantry OVR in {at} has its CC at once, before anything else happens (A4.152, A15.432)\"",
     "            ? ScenarioA1InfantryOverrun.CloseCombatFirstText(at)"),
])
apply(R + "ScenarioA1VehicleCloseCombatRules.cs", [
    ("    /// <summary>Ruling R11.16: a vehicle's CC Location waits for a side in the CCPh,",
     "    /// <summary>\n"
     "    /// The attacks of an Infantry OVR's CC that is not a berserk charge's (A4.152, A20.54; pass 40), as the page first offers them: the entering\n"
     "    /// units attack the enemy units there together, and each of those that may attack in CC attacks them back. The ATTACKER may declare others.\n"
     "    /// </summary>\n"
     "    public static IReadOnlyList<CloseCombatDeclaration> EntryAttacks(IReadOnlyList<string> attackers, IReadOnlyList<string> defenders, IReadOnlyList<string> defendersThatAttack)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(defendersThatAttack);\n"
     "        return [new CloseCombatDeclaration(attackers, defenders), .. defendersThatAttack.Select(id => new CloseCombatDeclaration([id], attackers))];\n"
     "    }\n\n"
     "    /// <summary>Ruling R11.16: a vehicle's CC Location waits for a side in the CCPh,"),
])

# ---- Play -----------------------------------------------------------------------------------------------------------------------
apply(P + "GamePlanner.Movement.cs", [
    ("        // A4.63 (pass 40): the Location a Dash is declared to, with the stack's first step.\n",
     "        // A4.15 (pass 40): the step is declared an Infantry OVR onto a lone Known SMC.\n"
     "        var overrunDeclared = arguments.TryGetProperty(\"overrun\", out var overrunning) && overrunning.ValueKind == JsonValueKind.True;\n\n"
     "        // A4.63 (pass 40): the Location a Dash is declared to, with the stack's first step.\n"),
    ("        // A4.63 (pass 40): a Dash declared with this step, or pending from the last; Rules decides from the step's facts and the map's.\n",
     "        // A4.15, A20.54 (pass 40): an Infantry OVR onto a lone Known SMC needs its passed NTC; Unarmed units are entered with none. Rules decides\n"
     "        // from the Location's enemy units as the moving side knows them and from the NTCs the state keeps.\n"
     "        var defenders = OverrunDefenders(state, to, movers[0].Side);\n"
     "        var recapture = !overrunDeclared && berserk == 0 && entering is null && bypassing is null && !occupy && !minimumMove && to != from && ScenarioA1InfantryOverrun.OnlyUnarmed(defenders);\n"
     "        if (overrunDeclared)\n"
     "        {\n"
     "            string[] mmcIds = [.. movers.Where(unit => vocabulary.IsA(unit.Kind, \"asl:mmc\")).Select(unit => unit.Id)];\n"
     "            var taken = state.InfantryOverrunNtcs.Where(ntc => ntc.Target == to.ToString()).ToArray();\n"
     "            if ((ScenarioA1InfantryOverrun.TargetBar(defenders, to.ToString())\n"
     "                ?? ScenarioA1InfantryOverrun.EntryBar(mmcIds.Length > 0 && taken.Any(ntc => ntc.Passed && mmcIds.All(id => ntc.Units.Contains(id, StringComparer.Ordinal))),\n"
     "                    taken.Any(ntc => !ntc.Passed && ntc.Units.Any(id => ids.Contains(id, StringComparer.Ordinal))), minimumMove, bypassing is not null, entering is not null, berserk > 0,\n"
     "                    to.ToString())) is { } overrunBar)\n"
     "            {\n"
     "                return Refused(scope, label, expected, overrunBar);\n"
     "            }\n"
     "        }\n\n"
     "        // A4.63 (pass 40): a Dash declared with this step, or pending from the last; Rules decides from the step's facts and the map's.\n"),
    ("        if (noCost is not null)\n        {\n            return Refused(scope, label, expected, noCost);\n        }\n",
     "        if (noCost is not null)\n        {\n            return Refused(scope, label, expected, noCost);\n        }\n\n"
     "        // A4.15 (pass 40): an Infantry OVR enters \"at double the total MF cost of entry\".\n"
     "        if (overrunDeclared)\n        {\n            halfMf = ScenarioA1InfantryOverrun.EntryHalfMf(halfMf);\n        }\n"),
    ("            state.PhasingSide, charge is not null, occupy, bypassing is not null, movers.All(unit => unit.Kind == UnitKinds.Dummy), minimumMove, string.Join(\", \", ids), to.ToString());",
     "            state.PhasingSide, charge is not null, occupy, bypassing is not null, movers.All(unit => unit.Kind == UnitKinds.Dummy), minimumMove, string.Join(\", \", ids), to.ToString(),\n"
     "            overrunDeclared || recapture);"),
    ("overstack, entry.OnRoad, dashTo?.ToString()));", "overstack, entry.OnRoad, dashTo?.ToString(), overrunDeclared, recapture));"),
    ("            IpcTo = ipcChosen,\n", "            IpcTo = ipcChosen,\n            InfantryOverrun = (overrunDeclared || recapture) && !forcedBack,\n"),
])
apply(P + "GamePlanner.VehicleCloseCombat.cs", [
    ("        var movement = state.Movement;\n"
     "        if (movement?.Charge is not { } charged || !ScenarioA1VehicleCloseCombatRules.OverrunPendingPossible(state.Phase, movement.WindowOpen, true, movement.Location == charged,\n"
     "            state.CloseCombats.Any(item => item.Location == charged)))\n"
     "        {\n            return null;\n        }\n",
     "        // A4.152, A20.54 (pass 40): the same holds for the entry of a declared Infantry OVR and of a recapture of Unarmed units.\n"
     "        var movement = state.Movement;\n"
     "        if ((movement?.Charge ?? movement?.InfantryOverrun) is not { } charged || !ScenarioA1VehicleCloseCombatRules.OverrunPendingPossible(state.Phase, movement!.WindowOpen, true,\n"
     "            movement.Location == charged, state.CloseCombats.Any(item => item.Location == charged)))\n"
     "        {\n            return null;\n        }\n"),
    ("        if (smc is null || !ScenarioA1VehicleCloseCombatRules.OverrunSmc(here.Any(unit => unit.Side == state.PhasingSide && movement.Members.Contains(unit.Id, StringComparer.Ordinal) && Is(unit, Conditions.Berserk)\n"
     "            && vocabulary.IsA(unit.Kind, \"asl:mmc\")), enemies.Length, vocabulary.IsA(smc.Kind, \"asl:smc\"), KnownEnemy(smc), Is(smc, Conditions.Melee)))\n",
     "        var owed = smc is not null && (movement.Charge is not null\n"
     "            ? ScenarioA1VehicleCloseCombatRules.OverrunSmc(here.Any(unit => unit.Side == state.PhasingSide && movement.Members.Contains(unit.Id, StringComparer.Ordinal) && Is(unit, Conditions.Berserk)\n"
     "                && vocabulary.IsA(unit.Kind, \"asl:mmc\")), enemies.Length, vocabulary.IsA(smc.Kind, \"asl:smc\"), KnownEnemy(smc), Is(smc, Conditions.Melee))\n"
     "            : ScenarioA1InfantryOverrun.CloseCombatOwed(here.Any(unit => unit.Side == state.PhasingSide && movement.Members.Contains(unit.Id, StringComparer.Ordinal)\n"
     "                && vocabulary.IsA(unit.Kind, \"asl:mmc\")), OverrunDefenders(state, charged, state.PhasingSide!)));\n"
     "        if (smc is null || !owed)\n"),
    ("        string[] berserk = [.. state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide\n"
     "            && Is(unit, Conditions.Berserk)).Select(unit => unit.Id).Order(StringComparer.Ordinal)];\n",
     "        // A4.152, A20.54 (pass 40): for a declared OVR or a recapture, the stack's units there attack the enemy units there.\n"
     "        if (state.Movement is { Charge: null, InfantryOverrun: not null } entered)\n"
     "        {\n"
     "            UnitInstance[] present = [.. state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured))];\n"
     "            return ScenarioA1VehicleCloseCombatRules.EntryAttacks(\n"
     "                [.. present.Where(unit => unit.Side == state.PhasingSide && entered.Members.Contains(unit.Id, StringComparer.Ordinal)).Select(unit => unit.Id).Order(StringComparer.Ordinal)],\n"
     "                [.. present.Where(unit => unit.Side != state.PhasingSide).Select(unit => unit.Id).Order(StringComparer.Ordinal)],\n"
     "                [.. present.Where(unit => unit.Side != state.PhasingSide && CcAttacker(unit)).Select(unit => unit.Id).Order(StringComparer.Ordinal)]);\n"
     "        }\n\n"
     "        string[] berserk = [.. state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide\n"
     "            && Is(unit, Conditions.Berserk)).Select(unit => unit.Id).Order(StringComparer.Ordinal)];\n"),
])
apply(P + "GamePlanner.CloseCombat.cs", [
    ("        // A15.432 (ruling R27.3): every berserk unit of the OVR attacks the SMC; the SMC may attack them back; nothing else happens in that CC.\n        if (overrun)\n",
     "        // A15.432 (ruling R27.3): every berserk unit of the OVR attacks the SMC; the SMC may attack them back; nothing else happens in that CC. A\n"
     "        // declared Infantry OVR's CC is \"normal CC\" (A4.152; pass 40): its attacks are the ATTACKER's to declare.\n"
     "        if (overrun && state.Movement?.Charge is not null)\n"),
])
apply(P + "GamePlanner.cs", [
    ("            \"asl.game.choose\" => PlanChoose(scope, arguments, existing, attemptId, expected, label, actor ?? \"unknown\"),\n",
     "            \"asl.game.choose\" => PlanChoose(scope, arguments, existing, attemptId, expected, label, actor ?? \"unknown\"),\n"
     "            \"asl.game.infantry-overrun-ntc\" => PlanInfantryOverrunNtc(scope, arguments, existing, attemptId, expected, label, actor ?? \"unknown\"),\n"
     "            \"asl.game.overrun-evade\" => PlanOverrunEvade(scope, arguments, existing, attemptId, expected, label),\n"),
])
apply(P + "GamePlanner.Choices.cs", [
    ("        var subject = choice.Key[(choice.Key.IndexOf(':', StringComparison.Ordinal) + 1)..];\n",
     "        var subject = choice.Key[(choice.Key.IndexOf(':', StringComparison.Ordinal) + 1)..];\n"
     "        if (choice.Kind == ChoicePending.OverrunEvade)\n"
     "        {\n"
     "            return ScenarioA1InfantryOverrun.EvadeChoiceText(subject.Split(':')[0]);\n"
     "        }\n\n"),
    ("        // A12.41 (ruling R11.12): the concealed units a vehicle entered are revealed or take their combined PAATC.\n        if (pending.Kind == ChoicePending.Paatc)\n",
     "        // A4.151 (pass 40): the SMC that moves away from an Infantry OVR enters the Location the ATTACKER names.\n"
     "        if (pending.Kind == ChoicePending.OverrunEvade)\n"
     "        {\n"
     "            var evaded = PlanOverrunEvadeAnswer(scope, attemptId, expected, label, state, pending, made, option);\n"
     "            return evaded with\n"
     "            {\n"
     "                Reasons = [summary, .. evaded.Reasons]\n"
     "            };\n"
     "        }\n\n"
     "        // A12.41 (ruling R11.12): the concealed units a vehicle entered are revealed or take their combined PAATC.\n        if (pending.Kind == ChoicePending.Paatc)\n"),
])
apply(P + "GameActions.cs", [
    ("    public static readonly ActionDescriptor EndMove = Descriptor(\"asl.game.end-move\", \"End the move\",",
     "    public static readonly ActionDescriptor InfantryOverrunNtc = Descriptor(\"asl.game.infantry-overrun-ntc\", \"Take an Infantry OVR's NTC\",\n"
     "        \"The ATTACKER's stack takes the NTC that lets it enter the Location of a lone Known enemy SMC in its MPh: by its leader for the stack, or by its one MMC, against the TEM there (A4.15).\",\n"
     "        PlayPermission, \"asl.game.sequence-v1\", \"\"\"\n"
     "        {\n"
     "          \"type\": \"object\", \"additionalProperties\": false,\n"
     "          \"required\": [\"gameId\", \"attemptId\", \"expectedRevision\", \"unitIds\", \"to\"],\n"
     "          \"properties\": {\n"
     "            \"gameId\": { \"type\": \"string\" }, \"attemptId\": { \"type\": \"string\" },\n"
     "            \"expectedRevision\": { \"type\": \"integer\", \"minimum\": 0 },\n"
     "            \"unitIds\": { \"type\": \"array\", \"items\": { \"type\": \"string\" }, \"minItems\": 1 },\n"
     "            \"to\": { \"type\": \"string\" }\n"
     "          }\n"
     "        }\n"
     "        \"\"\");\n\n"
     "    public static readonly ActionDescriptor OverrunEvade = Descriptor(\"asl.game.overrun-evade\", \"Move away from an Infantry OVR\",\n"
     "        \"The DEFENDER's SMC, before any Defensive First Fire at the units that entered its Location by an Infantry OVR, moves away into an adjacent Location of the ATTACKER's choice (A4.151).\",\n"
     "        PlayPermission, \"asl.game.sequence-v1\", \"\"\"\n"
     "        {\n"
     "          \"type\": \"object\", \"additionalProperties\": false,\n"
     "          \"required\": [\"gameId\", \"attemptId\", \"expectedRevision\", \"unitId\"],\n"
     "          \"properties\": {\n"
     "            \"gameId\": { \"type\": \"string\" }, \"attemptId\": { \"type\": \"string\" },\n"
     "            \"expectedRevision\": { \"type\": \"integer\", \"minimum\": 0 },\n"
     "            \"unitId\": { \"type\": \"string\" }\n"
     "          }\n"
     "        }\n"
     "        \"\"\");\n\n"
     "    public static readonly ActionDescriptor EndMove = Descriptor(\"asl.game.end-move\", \"End the move\","),
    ("Rout, Deploy, Recombine, Transfer, Drop, Recover, Dismantle, AmbushWithdraw, GuardPrisoners, ThrowDc, DetonateDc, FireStarshell, PlaceHidden, MopUp];",
     "Rout, Deploy, Recombine, Transfer, Drop, Recover, Dismantle, AmbushWithdraw, GuardPrisoners, ThrowDc, DetonateDc, FireStarshell, PlaceHidden, MopUp,\n"
     "            InfantryOverrunNtc, OverrunEvade];"),
    ("\"dash\": { \"type\": \"string\" }, \"ipcTo\": { \"type\": \"string\" },", "\"dash\": { \"type\": \"string\" }, \"ipcTo\": { \"type\": \"string\" }, \"overrun\": { \"type\": \"boolean\" },"),
])

# ---- The page -------------------------------------------------------------------------------------------------------------------
apply(M + "Components/Play/InfantryMovementAction.razor", [
    ('<label><input id="move-assault" type="checkbox"',
     '<label title="A4.15: a MMC enters the Location of a lone Known enemy SMC at double the MF of entry, once the stack has passed its NTC (its leader\'s for all of it, or the one MMC\'s). Take the NTC first; then check this and propose the move.">'
     '<input id="move-overrun" type="checkbox" checked="@Draft.Overrun" @onchange="@(args => Changed(Draft with { Overrun = args.Value is true }))" /> Infantry OVR</label>\n'
     '<button type="button" id="propose-overrun-ntc" disabled="@(Selected.Count == 0 || Draft.To.Trim().Length == 0)" title="A4.15: the NTC for an Infantry OVR of the Location typed, by the units checked." @onclick="@(() => OnOverrunNtc.InvokeAsync())">Propose: the OVR\'s NTC</button>\n'
     '<label><input id="move-assault" type="checkbox"'),
    ("bool TerrainRate = false, string Dash = \"\", string IpcTo = \"\");", "bool TerrainRate = false, string Dash = \"\", string IpcTo = \"\", bool Overrun = false);"),
    ("    [Parameter]\n    public EventCallback OnPropose { get; set; }\n",
     "    [Parameter]\n    public EventCallback OnPropose { get; set; }\n\n"
     "    /// <summary>Proposes the NTC of an Infantry OVR for the units checked and the Location typed (A4.15).</summary>\n"
     "    [Parameter]\n    public EventCallback OnOverrunNtc { get; set; }\n"),
])
apply(M + "Components/Pages/Play.razor", [
    ("OnToggle=\"@ToggleMover\" OnDraftChanged=\"@SetMoveDraft\" OnPropose=\"@ProposeMove\" />",
     "OnToggle=\"@ToggleMover\" OnDraftChanged=\"@SetMoveDraft\" OnPropose=\"@ProposeMove\" OnOverrunNtc=\"@ProposeOverrunNtc\" />"),
    ("    private string moveIpcTo = string.Empty;\n", "    private string moveIpcTo = string.Empty;\n    private bool moveOverrun;\n"),
    ("moveTerrainRate, moveDash, moveIpcTo);", "moveTerrainRate, moveDash, moveIpcTo, moveOverrun);"),
    ("        (moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush, moveTerrainRate, moveDash, moveIpcTo) =\n"
     "            (draft.To, draft.Minimum, draft.Bypass, draft.Assault, draft.DoubleTime, draft.Push, draft.TerrainRate, draft.Dash, draft.IpcTo);",
     "        (moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush, moveTerrainRate, moveDash, moveIpcTo, moveOverrun) =\n"
     "            (draft.To, draft.Minimum, draft.Bypass, draft.Assault, draft.DoubleTime, draft.Push, draft.TerrainRate, draft.Dash, draft.IpcTo, draft.Overrun);"),
    ("        // A4.42 (pass 40): the laden MMC named for the leader's IPC.\n",
     "        // A4.15 (pass 40): the step is an Infantry OVR.\n"
     "        if (moveOverrun)\n        {\n            arguments[\"overrun\"] = true;\n        }\n\n"
     "        // A4.42 (pass 40): the laden MMC named for the leader's IPC.\n"),
    ("    /// <summary>An action that names only the game: the DEFENDER's pass, or the end of the moving stack's move.</summary>\n",
     "    /// <summary>The NTC of an Infantry OVR (A4.15; pass 40), for the movers checked and the Location typed.</summary>\n"
     "    private Task ProposeOverrunNtc() => Propose(GameActions.InfantryOverrunNtc, JsonSerializer.SerializeToElement(new Dictionary<string, object>\n"
     "    {\n"
     "        [\"gameId\"] = gameId, [\"attemptId\"] = Attempt(\"ovr-ntc\"), [\"expectedRevision\"] = (long)history!.Events.Count,\n"
     "        [\"unitIds\"] = moveUnits.Order(StringComparer.Ordinal).ToArray(), [\"to\"] = moveTo.Trim(),\n"
     "    }));\n\n"
     "    /// <summary>The DEFENDER's SMC moves away from an Infantry OVR (A4.151; pass 40); the ATTACKER is then asked where.</summary>\n"
     "    private Task ProposeOverrunEvade(string unitId) => Propose(GameActions.OverrunEvade, JsonSerializer.SerializeToElement(new Dictionary<string, object>\n"
     "    {\n"
     "        [\"gameId\"] = gameId, [\"attemptId\"] = Attempt(\"ovr-evade\"), [\"expectedRevision\"] = (long)history!.Events.Count, [\"unitId\"] = unitId,\n"
     "    }));\n\n"
     "    /// <summary>An action that names only the game: the DEFENDER's pass, or the end of the moving stack's move.</summary>\n"),
    ("                            <button id=\"propose-pass\" @onclick='@(() => ProposeSimple(GameActions.PassFire, \"pass\"))'>Propose: the DEFENDER passes</button>\n",
     "                            <button id=\"propose-pass\" @onclick='@(() => ProposeSimple(GameActions.PassFire, \"pass\"))'>Propose: the DEFENDER passes</button>\n"
     "                            @if (Live.Planner.OverrunEvader(state, history!.Events) is { } evader)\n"
     "                            {\n"
     "                                <button id=\"propose-overrun-evade\" title=\"A4.151: before any Defensive First Fire at the units that entered, the SMC may move away into an adjacent Location of the ATTACKER's choice, free of all fire.\" @onclick=\"@(() => ProposeOverrunEvade(evader))\">Propose: the SMC moves away from the OVR</button>\n"
     "                            }\n"),
    ("\n        moveDash = string.Empty;\n", "\n        moveDash = string.Empty;\n        moveOverrun = false;\n", 2),
    ("\n                moveDash = string.Empty;\n", "\n                moveDash = string.Empty;\n                moveOverrun = false;\n", 2),
])

for path, text in PENDING.items():
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)
print("ok", len(PENDING))
