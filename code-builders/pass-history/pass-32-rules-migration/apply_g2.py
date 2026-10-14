"""Pass 32.g commit 2: PlanAmbush, PlanCloseCombat, FirstRoundStacking, WithdrawalDestinations, WithdrawalTires, MustWithdraw call ScenarioA1CloseCombatRules."""
import pathlib

P = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL\LimboDancer.Domains.Asl.Play\GamePlanner.CloseCombat.cs")
C = "ScenarioA1CloseCombatRules"


def apply(p, edits):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:80])
        t = t.replace(old, new)
    p.write_bytes(t.encode("utf-8"))
    print("applied", p.name, len(edits))


apply(P, [
    # PlanAmbush
    (
        '        if (state.Phase != "ccph")\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.cc-phase: CC is resolved in the CCPh (A3.8, A11.1)");\n'
        '        }\n'
        '\n'
        '        if (state.CloseCombats.Any(item => item.Location == location || !item.Closed))\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.ambush-order: the Ambush drs come before any CC in the Location, with no other Location\'s CC open (A11.4, A11.12)");\n'
        '        }\n',
        f'        if ({C}.CcPhaseBar(state.Phase) is {{ }} phaseBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, phaseBar);\n'
        '        }\n'
        '\n'
        f'        if ({C}.AmbushOrderBar(state.CloseCombats.Any(item => item.Location == location || !item.Closed)) is {{ }} orderBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, orderBar);\n'
        '        }\n',
    ),
    (
        '        facts = facts with\n'
        '        {\n'
        '            DarkNight = state.Night && !Illuminated(state, location) ? true : null\n'
        '        };\n'
        '\n'
        '        var reference = CloseCombatReference.Value;\n'
        '        var first = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, reference);\n'
        '        if (first.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.cc.roll-missing:ambush:", StringComparison.Ordinal))\n',
        '        facts = facts with\n'
        '        {\n'
        f'            DarkNight = {C}.DarkNight(state.Night, Illuminated(state, location))\n'
        '        };\n'
        '\n'
        '        var reference = CloseCombatReference.Value;\n'
        '        var first = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, reference);\n'
        f'        if ({C}.AmbushRollSide(first.Reasons) is null)\n',
    ),
    (
        '                if (resolution.Reasons is not [{ } key] || !key.StartsWith("asl.a1.cc.roll-missing:ambush:", StringComparison.Ordinal))\n'
        '                {\n'
        '                    throw new InvalidOperationException("The Close Combat package left an Ambush it had accepted undecided: " + string.Join("; ", resolution.Reasons));\n'
        '                }\n'
        '\n'
        '                var side = key["asl.a1.cc.roll-missing:ambush:".Length..];\n',
        f'                if ({C}.AmbushRollSide(resolution.Reasons) is not {{ }} side)\n'
        '                {\n'
        '                    throw new InvalidOperationException("The Close Combat package left an Ambush it had accepted undecided: " + string.Join("; ", resolution.Reasons));\n'
        '                }\n'
        '\n',
    ),
    (
        '            foreach (var unit in resolution.Ambusher is { } ambusher ? facts.Units!.Where(unit => unit.Side != ambusher && unit.Concealed == true && unit.Captured != true) : [])\n'
        '            {\n'
        '                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.UnitId!, new Dictionary<string, ConditionState>\n'
        '                {\n'
        '                    [Conditions.Concealed] = ConditionState.False,\n'
        '                    [Conditions.Hidden] = ConditionState.False,\n'
        '                }), package, null, [recordId]));\n'
        '            }\n',
        f'            foreach (var unit in facts.Units!.Where(unit => {C}.AmbushedLosesConcealment(resolution.Ambusher, unit.Side!, unit.Concealed, unit.Captured)))\n'
        '            {\n'
        '                var lost = new Dictionary<string, ConditionState>();\n'
        f'                foreach (var (condition, value) in {C}.AmbushedConditions())\n'
        '                {\n'
        '                    lost[ConditionName(condition)] = value ? ConditionState.True : ConditionState.False;\n'
        '                }\n'
        '\n'
        '                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.UnitId!, lost), package, null, [recordId]));\n'
        '            }\n',
    ),
    (
        '            [$"play.ambush: each side makes an Ambush dr for {location}; one at least three below the other ambushes (A11.4)"])\n',
        f'            [{C}.AmbushSummary(location.ToString())])\n',
    ),
    # PlanCloseCombat
    (
        '        var overrun = state.Phase == "mph" && BerserkOverrunPending(state) == location;\n'
        '        if (state.Phase != "ccph" && !overrun)\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.cc-phase: CC is resolved in the CCPh, or in the MPh after a berserk Infantry OVR onto a lone SMC (A3.8, A11.1, A4.152)");\n'
        '        }\n'
        '\n'
        '        // A11.31 (ruling R11.16): CC in a Location holding a vehicle is sequential, one attack at a time, with the vehicle CC action.\n'
        '        if (state.At(location).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)) is { } present)\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.cc-vehicle: {present.Id} is in {location}, so its CC is sequential, one attack at a time (asl.game.vehicle-close-combat; A11.31)");\n'
        '        }\n',
        f'        var overrun = {C}.OverrunCc(state.Phase, state.Phase == "mph" && BerserkOverrunPending(state) == location);\n'
        f'        if ({C}.RoundPhaseBar(state.Phase, overrun) is {{ }} phaseBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, phaseBar);\n'
        '        }\n'
        '\n'
        '        // A11.31 (ruling R11.16): CC in a Location holding a vehicle is sequential, one attack at a time, with the vehicle CC action.\n'
        f'        if ({C}.VehiclePresentBar(state.At(location).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit))?.Id, location.ToString()) is {{ }} vehicleBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, vehicleBar);\n'
        '        }\n',
    ),
    (
        '            var phasing = attacks.Where(item => item.Attackers!.All(berserkHere.Contains)).ToArray();\n'
        '            if (phasing.Length != 1 || !phasing[0].Attackers!.ToHashSet(StringComparer.Ordinal).SetEquals(berserkHere) || attacks.Count > 2\n'
        '                || attacks.Any(item => item.Capture == true) || arguments.TryGetProperty("withdrawals", out _) || arguments.TryGetProperty("infiltrations", out _))\n'
        '            {\n'
        '                return Refused(scope, label, expected, "play.cc-overrun: in the OVR\'s CC every berserk unit attacks the SMC together, and the SMC may attack them; no capture, withdrawal, or Infiltration (A4.152, A15.432, A20.21)");\n'
        '            }\n',
        '            var phasing = attacks.Where(item => item.Attackers!.All(berserkHere.Contains)).ToArray();\n'
        f'            if ({C}.OverrunAttacksBar(phasing.Length, phasing.Length == 1 && phasing[0].Attackers!.ToHashSet(StringComparer.Ordinal).SetEquals(berserkHere), attacks.Count,\n'
        '                attacks.Any(item => item.Capture == true), arguments.TryGetProperty("withdrawals", out _), arguments.TryGetProperty("infiltrations", out _)) is { } overrunBar)\n'
        '            {\n'
        '                return Refused(scope, label, expected, overrunBar);\n'
        '            }\n',
    ),
    (
        '            if (state.Unit(unitId) is { Status: InstanceStatus.Active } laden && Laden(state, laden))\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.cc-withdrawal-portage: {unitId} carries more than its IPC and may not withdraw with it; dropping a SW is not built (A11.21, A4.43)");\n'
        '            }\n'
        '\n'
        '            if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || state.Location(unit.Id)?.Location != location || !Is(unit, Conditions.Melee)\n'
        '                || !BoardLocation.TryParse(destination, out var to) || !WithdrawalDestinations(state, unit, location).Contains(to))\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.cc-withdrawal: {unitId} withdraws only from Melee, to an ADJACENT Location it could advance into that holds no Known enemy unit (A11.2, A11.21)");\n'
        '            }\n'
        '        }\n'
        '\n'
        '        if (state.Units.FirstOrDefault(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id)?.Location == location && MustWithdraw(state, unit, location)\n'
        '            && !withdrawals.ContainsKey(unit.Id)) is { } broken)\n'
        '        {\n'
        '            return Refused(scope, label, expected, $"play.cc-withdraw-required: {broken.Id} is broken in Melee and must attempt to withdraw (A11.16)");\n'
        '        }\n',
        f'            if ({C}.WithdrawalPortageBar(unitId, state.Unit(unitId) is {{ Status: InstanceStatus.Active }} laden && Laden(state, laden)) is {{ }} portageBar)\n'
        '            {\n'
        '                return Refused(scope, label, expected, portageBar);\n'
        '            }\n'
        '\n'
        f'            if ({C}.WithdrawalBar(unitId, state.Unit(unitId) is {{ Status: InstanceStatus.Active }} unit && state.Location(unit.Id)?.Location == location && Is(unit, Conditions.Melee)\n'
        '                && BoardLocation.TryParse(destination, out var to) && WithdrawalDestinations(state, unit, location).Contains(to)) is { } withdrawalBar)\n'
        '            {\n'
        '                return Refused(scope, label, expected, withdrawalBar);\n'
        '            }\n'
        '        }\n'
        '\n'
        '        if (state.Units.FirstOrDefault(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id)?.Location == location && MustWithdraw(state, unit, location)\n'
        '            && !withdrawals.ContainsKey(unit.Id)) is { } broken)\n'
        '        {\n'
        f'            return Refused(scope, label, expected, {C}.WithdrawRequiredText(broken.Id));\n'
        '        }\n',
    ),
    (
        '            if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || state.Location(unit.Id)?.Location != location\n'
        '                || !BoardLocation.TryParse(destination, out var to) || !WithdrawalDestinations(state, unit, location).Contains(to))\n'
        '            {\n'
        '                return Refused(scope, label, expected, $"play.cc-infiltration: {unitId} may infiltrate only to an ADJACENT Location a withdrawal could reach (A11.22, A11.21)");\n'
        '            }\n',
        f'            if ({C}.InfiltrationBar(unitId, state.Unit(unitId) is {{ Status: InstanceStatus.Active }} unit && state.Location(unit.Id)?.Location == location\n'
        '                && BoardLocation.TryParse(destination, out var to) && WithdrawalDestinations(state, unit, location).Contains(to)) is { } infiltrationBar)\n'
        '            {\n'
        '                return Refused(scope, label, expected, infiltrationBar);\n'
        '            }\n',
    ),
    (
        '        if (handToHand && !state.SpecialRules.Contains(HandToHandRule, StringComparer.Ordinal))\n'
        '        {\n'
        '            return Refused(scope, label, expected, "play.cc-hand-to-hand: Hand-to-Hand CC is declared only where an SSR allows it (J2.31, G1.64)");\n'
        '        }\n',
        f'        if ({C}.HandToHandSsrBar(handToHand, state.SpecialRules.Contains(HandToHandRule, StringComparer.Ordinal)) is {{ }} ssrBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, ssrBar);\n'
        '        }\n',
    ),
    (
        '        if (handToHand && ((Text(arguments, "round", out var handRound) && handRound == CloseCombatFacts.PrisonersRound)\n'
        '            || begun?.Rounds.Any(item => item != CloseCombatResolved.PrisonersRound) == true || (begun?.Ambusher is { } ambushed && ambushed != state.PhasingSide)))\n'
        '        {\n'
        '            return Refused(scope, label, expected,\n'
        '                "play.cc-hand-to-hand: the ATTACKER declares Hand-to-Hand with the Location\'s first round, not in the prisoners\' round, and not after being ambushed (J2.31, A25.43)");\n'
        '        }\n',
        f'        if ({C}.HandToHandTimingBar(handToHand, Text(arguments, "round", out var handRound) && handRound == CloseCombatFacts.PrisonersRound,\n'
        '            begun?.Rounds.Any(item => item != CloseCombatResolved.PrisonersRound) == true, begun?.Ambusher is { } ambushed && ambushed != state.PhasingSide) is { } timingBar)\n'
        '        {\n'
        '            return Refused(scope, label, expected, timingBar);\n'
        '        }\n',
    ),
    (
        '                if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.cc.roll-missing:", StringComparison.Ordinal))\n'
        '                {\n'
        '                    throw new InvalidOperationException("The Close Combat package left a round it had accepted undecided: " + string.Join("; ", resolution.Reasons));\n'
        '                }\n'
        '\n'
        '                var key = missing["asl.a1.cc.roll-missing:".Length..];\n'
        '                var split = key.IndexOf(\':\', StringComparison.Ordinal);\n'
        '                var (kind, rest) = (key[..split], key[(split + 1)..]);\n'
        '                string[] selected = kind == "randomSelection" ? rest[(rest.IndexOf(\':\', StringComparison.Ordinal) + 1)..].Split(\',\') : [];\n'
        '                if (kind == "leaderStack")\n'
        '                {\n'
        '                    selected = rest[(rest.IndexOf(\':\', StringComparison.Ordinal) + 1)..].Split(\',\');\n'
        '                }\n'
        '\n'
        '                var (count, purpose) = kind switch\n'
        '                {\n'
        '                    "attack" => (2, "cc-attack"),\n'
        '                    "escapeNtc" => (2, "cc-escape-ntc"),\n'
        '                    "leaderStack" => (selected.Length, "cc-leader-stack"),\n'
        '                    "randomSelection" => (selected.Length, "cc-random-selection"),\n'
        '                    "woundSeverity" => (1, "cc-wound-severity"),\n'
        '                    "leaderCreation" => (1, "cc-leader-creation"),\n'
        '                    _ => (1, "cc-weapon-loss"),\n'
        '                };\n'
        '                var drawn = draw(new RollRequest(count, 6));\n',
        f'                var next = {C}.NextStep(resolution.Disposition, resolution.Reasons);\n'
        '                if (next.Undecided is { } undecided)\n'
        '                {\n'
        '                    throw new InvalidOperationException("The Close Combat package left a round it had accepted undecided: " + undecided);\n'
        '                }\n'
        '\n'
        '                var (kind, rest, selected, count, purpose) = (next.Kind!, next.Rest!, next.Selected, next.Count, next.Purpose!);\n'
        '                var key = kind + ":" + rest;\n'
        '                var drawn = draw(new RollRequest(count, 6));\n',
    ),
    (
        '                        RandomSelection = selected.Select((id, index) => (id, index)).Aggregate(rolls.RandomSelection,\n'
        '                            (map, pair) => With(map, rest[..rest.IndexOf(\':\', StringComparison.Ordinal)] + ":" + pair.id, drawn.Values[pair.index]))\n',
        '                        RandomSelection = selected.Select((id, index) => (id, index)).Aggregate(rolls.RandomSelection,\n'
        f'                            (map, pair) => With(map, {C}.SelectionKey(rest, pair.id), drawn.Values[pair.index]))\n',
    ),
    (
        '                        LeaderStack = selected.Select((id, index) => (id, index)).Aggregate(rolls.LeaderStack,\n'
        '                            (map, pair) => With(map, rest[..rest.IndexOf(\':\', StringComparison.Ordinal)] + ":" + pair.id, drawn.Values[pair.index]))\n',
        '                        LeaderStack = selected.Select((id, index) => (id, index)).Aggregate(rolls.LeaderStack,\n'
        f'                            (map, pair) => With(map, {C}.SelectionKey(rest, pair.id), drawn.Values[pair.index]))\n',
    ),
    (
        '                var left = after.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured)).ToArray();\n'
        '                if (left.Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count() > 1)\n',
        '                var left = after.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured)).ToArray();\n'
        f'                if ({C}.OverrunHoldsInMelee(overrun, left.Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count()))\n',
    ),
    (
        '        var described = attacks.Count == 0 ? "no attacks"\n'
        '            : string.Join("; ", attacks.Select(item => $"{string.Join(", ", item.Attackers!)} {(item.Capture == true ? "attempt to capture" : "attack")} {string.Join(", ", item.Defenders!)}"\n'
        '                + (item.Director is { } director ? $", directed by {director}" : string.Empty)));\n'
        '        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [$"play.cc: {facts.Round} round in {location}{(facts.HandToHand == true ? ", Hand-to-Hand" : string.Empty)}: {described} (A11.11, A11.12)"])\n',
        f'        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [{C}.RoundSummary(facts.Round, location.ToString(), facts.HandToHand, {C}.DescribedAttacks(attacks))])\n',
    ),
    # FirstRoundStacking, WithdrawalDestinations, WithdrawalTires, MustWithdraw
    (
        '        if (state.CloseCombats.FirstOrDefault(item => item.Location == location) is not { Rounds.Count: > 0 })\n'
        '        {\n'
        '            return null;\n'
        '        }\n',
        f'        if (!{C}.KeepsFirstRoundStacking(state.CloseCombats.FirstOrDefault(item => item.Location == location)?.Rounds.Count ?? 0))\n'
        '        {\n'
        '            return null;\n'
        '        }\n',
    ),
    (
        '    public IReadOnlyList<BoardLocation> WithdrawalDestinations(GameState state, UnitInstance unit, BoardLocation from) => Laden(state, unit) ? []\n'
        '        : [.. Neighbors(state, from).Where(to => PlayableBar(state, to) is null && InfantryStep(state, from, to).Entry is { AllMf: false } entry\n'
        '            && DifficultAdvance(state, unit, entry.HalfMf) is { } difficult && !(difficult && Is(unit, Conditions.Cx))\n'
        '            && !state.At(to).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && !Is(other, Conditions.Captured)))];\n',
        '    public IReadOnlyList<BoardLocation> WithdrawalDestinations(GameState state, UnitInstance unit, BoardLocation from) => Laden(state, unit) ? []\n'
        '        : [.. Neighbors(state, from).Where(to => PlayableBar(state, to) is null && InfantryStep(state, from, to).Entry is { } entry\n'
        f'            && {C}.WithdrawalDestination(true, !entry.AllMf, entry.AllMf ? null : DifficultAdvance(state, unit, entry.HalfMf), Is(unit, Conditions.Cx),\n'
        '                !entry.AllMf && state.At(to).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && !Is(other, Conditions.Captured))))];\n',
    ),
    (
        '    private bool WithdrawalTires(GameState state, UnitInstance unit, BoardLocation from, BoardLocation to) =>\n'
        '        InfantryStep(state, from, to).Entry is { AllMf: false } entry && DifficultAdvance(state, unit, entry.HalfMf) == true;\n',
        '    private bool WithdrawalTires(GameState state, UnitInstance unit, BoardLocation from, BoardLocation to) =>\n'
        f'        InfantryStep(state, from, to).Entry is {{ }} entry && {C}.WithdrawalTires(!entry.AllMf, entry.AllMf ? null : DifficultAdvance(state, unit, entry.HalfMf));\n',
    ),
    (
        '    private bool MustWithdraw(GameState state, UnitInstance unit, BoardLocation at) =>\n'
        '        Is(unit, Conditions.Broken) && Is(unit, Conditions.Melee) && !Is(unit, Conditions.Disrupted) && !Is(unit, Conditions.Captured)\n'
        '        && !IsGuard(state, unit) && WithdrawalDestinations(state, unit, at).Count > 0;\n',
        '    private bool MustWithdraw(GameState state, UnitInstance unit, BoardLocation at) =>\n'
        f'        {C}.MustWithdraw(Is(unit, Conditions.Broken), Is(unit, Conditions.Melee), Is(unit, Conditions.Disrupted), Is(unit, Conditions.Captured),\n'
        '            () => IsGuard(state, unit), () => WithdrawalDestinations(state, unit, at).Count > 0);\n',
    ),
])
