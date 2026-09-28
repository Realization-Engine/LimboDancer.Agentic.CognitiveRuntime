using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Hex-by-hex Infantry movement in the MPh (unit step 22): a stack enters an adjacent Location at its terrain's MF cost,
/// and after each MF expenditure the DEFENDER may fire at it (A8.1) or pass; the ATTACKER then continues or ends the move
/// (A8.11). A Location holding Residual FP attacks the stack as it enters, before the DEFENDER may fire (A8.22).
/// </summary>
public sealed partial class GamePlanner
{
    // MF to enter the admitted terrain, in half MF: Open Ground and orchard 1 (A4.13; B14.4), brush and woods 2 (B12.4,
    // B13.4), grain 1½ (B15.4), a building 2 (B23.4); entry across a road hexside 1 (A4.132, B3.4).
    private static readonly Dictionary<string, int> EntryHalfMf = new(StringComparer.Ordinal)
    {
        ["open-ground"] = 2,
        ["orchard"] = 2,
        ["brush"] = 4,
        ["woods"] = 4,
        ["grain"] = 3,
        ["wooden-building"] = 4,
        ["stone-building"] = 4,
    };

    /// <summary>
    /// The MF a unit may spend this phase (A4.11, A4.42, A4.5, A4.52; rulings R5.1 and R5.4): its allotment (a MMC's four or three, a SMC's
    /// six or three, a berserk unit's eight), plus the MF Double Time adds, at most eight (seven for Conscripts), less one MF for each PP it
    /// carries beyond its IPC (three for a MMC, one for a SMC, none for a wounded SMC; one less while CX). A berserk unit counts only its 1PP SW,
    /// since it abandons the others before it charges (A15.431). Null when the catalog does not decide it.
    /// </summary>
    private int? MfAllotment(GameState state, UnitInstance unit, int doubleTimeMf, bool cx)
    {
        if (Experience.MoveAllowance(state, unit, catalogs, vocabulary) is not { } allotment || Portage(state, unit) is not { } carried)
        {
            return null;
        }

        if (doubleTimeMf > 0)
        {
            var conscript = unit.Definition is { } reference && FireReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.Class == "conscript";
            allotment = Math.Min(allotment + doubleTimeMf, conscript ? 7 : 8);
        }

        var smc = vocabulary.IsA(unit.Kind, "asl:smc");
        var ipc = (smc ? (Is(unit, Conditions.Wounded) ? 0 : 1) : 3) - (cx ? 1 : 0);
        var pp = Is(unit, Conditions.Berserk) ? carried.Where(item => item == 1).Sum() : carried.Sum();
        return allotment - Math.Max(0, pp - Math.Max(ipc, 0));
    }

    /// <summary>The PP of each SW a unit possesses (A4.4), from the catalog; null when one is not recorded.</summary>
    private int[]? Portage(GameState state, UnitInstance unit)
    {
        var values = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)
            .Select(item => item.Definition is { } reference
                ? catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:portage")?.Value?.Number
                : null).ToArray();
        return values.Any(value => value is null) ? null : [.. values.Select(value => value!.Value)];
    }

    /// <summary>
    /// The half MF Infantry spend to enter a terrain (B15.4, B15.6; ruling R5.19): grain costs 1½ MF from April to September and is Open Ground
    /// otherwise, and a game with no scenario month does not decide it; null when the terrain is not a reviewed entry.
    /// </summary>
    private static int? InfantryEntryHalfMf(GameState state, string terrain)
    {
        if (terrain == "grain")
        {
            return state.ScenarioMonth is not { } month ? null : month is >= 4 and <= 9 ? EntryHalfMf["grain"] : EntryHalfMf["open-ground"];
        }

        return EntryHalfMf.TryGetValue(terrain, out var halfMf) ? halfMf : null;
    }

    private GamePlan PlanMove(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        var ids = Strings(arguments, "unitIds").ToArray();
        if (ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length || !Text(arguments, "to", out var toText)
            || !BoardLocation.TryParse(toText, out var to))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a move names its stack and the Location it enters");
        }

        var assault = arguments.TryGetProperty("assault", out var flag) && flag.ValueKind == JsonValueKind.True;
        var doubleTime = arguments.TryGetProperty("doubleTime", out var timed) && timed.ValueKind == JsonValueKind.True;
        if (state.Phase != "mph")
        {
            return Refused(scope, label, expected, "play.move-phase: units move in their side's MPh (A3.3, p. 47)");
        }

        var movers = ids.Select(id => state.Unit(id)).ToArray();
        if (movers.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side != state.PhasingSide))
        {
            return Refused(scope, label, expected, "play.move-stack: every mover is an active unit of the phasing side");
        }

        if (movers.Select(unit => state.Location(unit!.Id)?.Location).Distinct().ToArray() is not [{ } from])
        {
            return Refused(scope, label, expected, "play.move-stack: the stack moves from one Location (A4.2)");
        }

        // C10: a crew manning a Gun leaves it only by abandoning it, and Gun movement is not reviewed (ruling R24.4).
        if (movers.FirstOrDefault(unit => Mans(state, unit!)) is { } gunner)
        {
            return Refused(scope, label, expected, $"play.move-crew-mans-gun: {gunner.Id} mans a Gun; abandoning or moving a Gun is not reviewed (C10, A21.13)");
        }

        // D2.1 (ruling R25.3): a vehicle spends its MP one expenditure at a time, by its own action; Infantry may not enter an enemy vehicle's
        // Location, since OVR (D7) and CC against a vehicle (A11.5) are not reviewed.
        if (movers.FirstOrDefault(unit => LiveFire.IsVehicle(unit!)) is { } driven)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-kind: {driven.Id} is a vehicle and moves by its MP expenditures (D2.1)");
        }

        if (EnemyVehicleAt(state, state.PhasingSide!, to) is { } blocking)
        {
            return Refused(scope, label, expected, $"play.move-enemy-vehicle: the enemy vehicle {blocking.Id} is in {to}; OVR and CC against a vehicle are not reviewed (D7, A11.5; ruling R25.3)");
        }

        // A3.3 (p. 47): a unit that fired in the PFPh does not move in the MPh.
        if (movers.FirstOrDefault(unit => GameState.Condition(unit!, Conditions.PrepFire) == ConditionState.True) is { } fired)
        {
            return Refused(scope, label, expected, $"play.move-prep-fire: {fired.Id} fired in the PFPh, so it may not move this MPh (A3.3, p. 47)");
        }

        // A20.4, A7.351 (ruling R5.7): a Massacre in the PFPh is made as if using a SW, so the unit has Prep Fired.
        if (movers.FirstOrDefault(unit => MassacredInPrepFire(existing, unit!.Id)) is { } massacring)
        {
            return Refused(scope, label, expected, $"play.move-prep-fire: {massacring.Id} massacred a prisoner in the PFPh, as if using a SW, so it may not move this MPh (A20.4, A3.3)");
        }

        // A4.61: Assault Movement is not used where the move requires CX status.
        if (assault && doubleTime)
        {
            return Refused(scope, label, expected, "play.move-assault: Assault Movement may not be combined with Double Time (A4.61, A4.5)");
        }

        // A11.15: a unit held in Melee does not leave its Location; a prisoner moves only with its Guard (A20.53).
        if (movers.FirstOrDefault(unit => Is(unit!, Conditions.Melee) || Is(unit!, Conditions.Captured)) is { } held)
        {
            return Refused(scope, label, expected, $"play.move-melee: {held.Id} is held in Melee or captured, so it does not move (A11.15, A20.53)");
        }

        // A4.1, A7.83: broken, pinned, or already-ended units do not move; A12.14: concealed movement is not reviewed.
        if (movers.Any(unit => GameState.Condition(unit!, Conditions.Broken) != ConditionState.False || GameState.Condition(unit!, Conditions.Pinned) == ConditionState.True
            || unit!.MovementEnded || unit.Kind == UnitKinds.Dummy
            || GameState.Condition(unit, Conditions.Concealed) == ConditionState.True || GameState.Condition(unit, Conditions.Hidden) == ConditionState.True))
        {
            return Refused(scope, label, expected, "play.move-unit: every mover is Good Order, unpinned, unconcealed, and not done moving (A4.1, A7.83)");
        }

        // A4.2: once a stack moves, only its members move, together or apart, until the ATTACKER ends them all.
        var current = state.Movement;
        if (current is not null && ids.Any(id => !current.Members.Contains(id, StringComparer.Ordinal)))
        {
            return Refused(scope, label, expected, current.Members.Count == 0
                ? $"play.move-order: {string.Join(", ", current.Movers)} can move no farther; end their move first (A4.2, A8.11)"
                : $"play.move-order: only {string.Join(", ", current.Members)} of the moving stack may move until its move ends (A4.2)");
        }

        if (current is { WindowOpen: true })
        {
            return Refused(scope, label, expected, "play.move-window: the DEFENDER may still fire at the stack's last MF expenditure (A8.1, A8.11)");
        }

        // A15.43: at the start of the MPh every berserk unit charges before any other unit moves.
        var berserk = movers.Count(unit => Is(unit!, Conditions.Berserk));
        BoardLocation? charge = null;
        if (berserk == 0 && current is null && MustCharge(state) is [{ } charging, ..])
        {
            return Refused(scope, label, expected, $"play.berserk-first: {charging.Id} is berserk and charges before any other unit moves (A15.43)");
        }

        var abandoned = new List<EquipmentInstance>();
        if (berserk > 0)
        {
            var (allowed, target, reason) = BerserkStep(state, [.. movers.Select(unit => unit!)], from, to, current, assault);
            if (allowed is null)
            {
                return Refused(scope, label, expected, reason!);
            }

            charge = target;

            // A15.431: before its charge a berserk unit abandons every SW of more than one PP; its 1PP SW beyond its IPC (A4.42: three for
            // a MMC, one for a SMC) are its own choice, which is not reviewed.
            foreach (var unit in movers.Where(unit => unit!.MfSpent == 0 && !unit.HalfMfSpent))
            {
                var carried = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == unit!.Id).ToArray();
                var portage = carried.ToDictionary(item => item, item => item.Definition is { } reference
                    ? catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:portage")?.Value?.Number
                    : null);
                if (portage.Values.Any(value => value is null))
                {
                    return Refused(scope, label, expected, $"play.berserk-sw: the portage of a SW {unit!.Id} holds is not recorded (A15.431)");
                }

                abandoned.AddRange(carried.Where(item => portage[item] > 1));
                if (carried.Count(item => portage[item] == 1) > (vocabulary.IsA(unit!.Kind, "asl:smc") ? 1 : 3))
                {
                    return Refused(scope, label, expected, $"play.berserk-sw: {unit.Id}'s 1PP SW exceed its IPC, and which it abandons is not reviewed (A15.431)");
                }
            }
        }

        // A4.61: Assault Movement is declared before the stack moves, and moves it no more than one Location.
        if (current is not null && (current.Assault || assault))
        {
            return Refused(scope, label, expected, "play.move-assault: Assault Movement is declared at the start of the move and enters one Location (A4.61)");
        }

        // A4.5 (ruling R5.1): Double Time by Infantry neither broken, wounded, berserk, nor already CX, nor whose CX counter left at this MPh's start.
        if (doubleTime && movers.FirstOrDefault(unit => Is(unit!, Conditions.Wounded) || Is(unit!, Conditions.Berserk) || Is(unit!, Conditions.Cx)
            || state.NoDoubleTime.Contains(unit!.Id)) is { } tired)
        {
            return Refused(scope, label, expected, Is(tired, Conditions.Cx) || state.NoDoubleTime.Contains(tired.Id)
                ? $"play.move-double-time: {tired.Id} is CX, or its CX counter left at the start of this MPh, so it may not Double Time (A4.5, A4.51)"
                : $"play.move-double-time: {tired.Id} is wounded or berserk and may not Double Time (A4.5, A17.2, A15.431)");
        }

        // A4.42: a SMC never portages more than two PP.
        if (movers.FirstOrDefault(unit => vocabulary.IsA(unit!.Kind, "asl:smc") && Portage(state, unit) is { } carried && carried.Sum() > 2) is { } laden)
        {
            return Refused(scope, label, expected, $"play.move-portage: {laden.Id} carries more than two PP, which a SMC never portages (A4.42)");
        }

        var (fromRead, toRead, adjacent, crossed) = Step(state, from, to);
        if (fromRead is null || toRead is null || !adjacent || crossed is null)
        {
            return Refused(scope, label, expected, $"play.move-step: {to} is not an adjacent Location the map reads");
        }

        if (fromRead.Hex.BaseLevel + fromRead.Level.Level != toRead.Hex.BaseLevel + toRead.Level.Level || to.Level != 0
            || crossed.HexsideTerrain is not null || crossed.Cliff || crossed.Slope)
        {
            return Refused(scope, label, expected, "play.move-terrain: level changes and hexside terrain are not reviewed (ruling R22.3)");
        }

        if (TerrainKey(toRead) is not { } terrain || !EntryHalfMf.ContainsKey(terrain))
        {
            return Refused(scope, label, expected, $"play.move-terrain: {toRead.Level.Terrain?.Name ?? "the terrain"} is not a reviewed entry (ruling R22.3)");
        }

        if (InfantryEntryHalfMf(state, terrain) is not { } halfMf)
        {
            return Refused(scope, label, expected, "play.move-grain: grain's MF cost depends on the season, and the game names no scenario month (B15.6)");
        }

        if (crossed.Terrain?.IsRoad == true)
        {
            halfMf = 2;
        }

        if (charge is null && state.At(to).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide))
        {
            return Refused(scope, label, expected, $"play.move-occupied: entry into an enemy-occupied Location is the building entry's action, or not reviewed (ruling R22.6)");
        }

        // A4.11, A4.42, A4.5: the MF each mover has left, with Double Time and portage; A4.61: Assault Movement may not use all of the
        // allotment without Double Time.
        foreach (var unit in movers)
        {
            var extra = doubleTime ? (unit!.MfSpent == 0 && !unit.HalfMfSpent ? 2 : 1) : unit!.DoubleTimeMf;
            var exhausted = doubleTime || Is(unit, Conditions.Cx);
            if (MfAllotment(state, unit, extra, exhausted) is not { } allowance || MfAllotment(state, unit, 0, exhausted) is not { } plain)
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit.Id} has no MF allowance the catalog decides");
            }

            var spent = (unit.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0);
            var left = (allowance * 2) - spent;
            if (left < halfMf || (assault && (plain * 2) - spent <= halfMf))
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit.Id} has {left / 2m} MF left, and the entry costs {halfMf / 2m} (A4.11, A4.42, A4.61)");
            }
        }

        var step = (current?.Step ?? 0) + 1;
        var moved = new MovementStepped(ids, to, halfMf, assault, step)
        {
            Charge = charge,
            DoubleTime = doubleTime,
        };
        var package = ScenarioA1FirePackage.Identity.ToString();
        var summary = $"play.move: {string.Join(", ", ids)} enter {to} ({terrain}) for {halfMf / 2m} MF" + (assault ? ", by Assault Movement" : string.Empty)
            + (doubleTime ? ", Double Timing and now CX (A4.5)" : string.Empty)
            + (charge is not null ? $", charging {charge} (A15.43)" : string.Empty)
            + (abandoned.Count > 0 ? $"; {string.Join(", ", abandoned.Select(item => item.Id))} abandoned before the charge (A15.431)" : string.Empty);
        List<GameEvent> prefix = [.. abandoned.Select((item, index) => Event(scope, attemptId, index + 1, expected, "equipment-transferred",
            new EquipmentTransferred(item.Id, null, new MapPosition(from)), package, null))];
        var residual = state.ResidualFire.FirstOrDefault(item => item.Location == to);
        var stepEvent = Event(scope, attemptId, prefix.Count + 1, expected, "movement-step", moved, package, null);
        if (residual is null)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [.. prefix, stepEvent], [summary]);
        }

        // A8.22: Residual FP attacks a unit entering its Location first, alone, with any FFNAM and FFMO.
        if (Replay([.. existing, .. prefix, stepEvent]).Current is not { } entered
            || LiveFire.ResidualFromState(entered, to, residual.Fp) is not ({ } residualAttack, null)
            || FireMapFacts(entered, residualAttack, to) is not ({ } mapFacts, null))
        {
            return Refused(scope, label, expected, "play.move-residual: the Residual FP attack on the entering stack cannot be read");
        }

        var facts = HeatOfBattleFacts(entered, mapFacts);

        var precheck = ScenarioA1FireCalculator.Precheck(facts, FireReference.Value);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, ["play.move-residual: the Fire package does not decide the Residual FP attack this entry would suffer", .. precheck]);
        }

        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>([.. prefix, stepEvent]);
            AddFireEvents(scope, attemptId, expected, actor, entered, facts, state.PhasingSide, step, events, draw);
            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [summary, $"play.move: {residual.Fp} Residual FP in {to} attacks the stack first (A8.22)"])
        {
            Roll = new PlannedRoll("residual", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// A berserk stack's step (A15.43, A15.431): every berserk unit of the Location that is not done moving, with the same wounded
    /// status, moves together and alone, never by Assault Movement, onto a shortest route to the nearest Known enemy unit in its
    /// LOS; it enters that unit's Location, unless the only Known enemy unit there is a lone SMC (an Infantry OVR, A15.432, not
    /// reviewed). Once in a Location with a Known enemy unit it moves no farther. Returns the charged Location, or why the step is
    /// refused.
    /// </summary>
    private (bool? Allowed, BoardLocation? Target, string? Reason) BerserkStep(GameState state, UnitInstance[] movers, BoardLocation from, BoardLocation to,
        MovementState? current, bool assault)
    {
        if (movers.Any(unit => !Is(unit, Conditions.Berserk)))
        {
            return (null, null, "play.berserk-stack: berserk units charge apart from units that are not berserk (A15.43)");
        }

        if (assault)
        {
            return (null, null, "play.berserk-assault: a berserk unit never uses Assault Movement (A15.431)");
        }

        var wounded = Is(movers[0], Conditions.Wounded);
        if (current is null && state.At(from).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side == movers[0].Side
            && Is(unit, Conditions.Berserk) && !unit.MovementEnded && Is(unit, Conditions.Wounded) == wounded && !movers.Contains(unit)))
        {
            return (null, null, "play.berserk-stack: berserk units in one Location charge together unless one is wounded and one is not (A15.43)");
        }

        var (steps, _, undecided) = ChargeSteps(state, movers[0].Side, from, current?.Charge);
        if (steps.TryGetValue(to, out var step))
        {
            return (true, step.Target, null);
        }

        if (undecided is not null)
        {
            return (null, null, undecided);
        }

        return (null, null, steps.Count == 0
            ? $"play.berserk-charge: {string.Join(", ", movers.Select(unit => unit.Id))} has no Known enemy unit to charge, or is already in its Location (A15.43)"
            : $"play.berserk-charge: {string.Join(", ", movers.Select(unit => unit.Id))} charges the nearest Known enemy unit in its LOS by a shortest route: "
                + $"{string.Join(", ", steps.Keys.Select(item => item.ToString()).Order(StringComparer.Ordinal))}, not {to} (A15.43, A15.431)");
    }

    /// <summary>The DEFENDER passes on the moving stack's latest MF expenditure (A8.11).</summary>
    private GamePlan PlanPassFire(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        return state.Movement is { WindowOpen: true } movement
            ? new GamePlan(GamePlanStatus.Ready, scope, label, expected,
                [Event(scope, attemptId, 1, expected, "movement-window-closed", new MovementWindowClosed(movement.Step), null, null)],
                [$"play.pass: the DEFENDER does not fire at {string.Join(", ", movement.Movers)} in {movement.Location}"
                    + (movement.Ending ? $"; {string.Join(", ", movement.Movers)} ends its move (D2.1)" : string.Empty)])
            : Refused(scope, label, expected, "play.pass-window: no moving stack awaits the DEFENDER");
    }

    /// <summary>
    /// The ATTACKER ends the move of the moving stack's members (A4.2, A8.11): the named ones, or all of them. Those units may
    /// not move again this MPh; the stack's move is over when none is left. With no member left, because each broke or was
    /// pinned, the units of its latest step are ended.
    /// </summary>
    private GamePlan PlanEndMove(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (state.Movement is not { } movement)
        {
            return Refused(scope, label, expected, "play.end-move: no stack is moving");
        }

        if (movement.WindowOpen)
        {
            return Refused(scope, label, expected, "play.end-move: the DEFENDER may still fire at the stack's last MF expenditure (A8.11)");
        }

        var named = Strings(arguments, "unitIds").ToArray();
        var ending = named.Length > 0 ? named : movement.Members.Count > 0 ? [.. movement.Members] : movement.Movers.ToArray();
        if (ending.Distinct(StringComparer.Ordinal).Count() != ending.Length
            || ending.Any(id => !movement.Members.Contains(id, StringComparer.Ordinal) && !movement.Movers.Contains(id, StringComparer.Ordinal)))
        {
            return Refused(scope, label, expected, $"play.end-move: only the moving stack's members ({string.Join(", ", movement.Members)}) end their move (A4.2)");
        }

        // D2.1, D2.4: a vehicle ends its move in Motion or stopped, spending its MP left in its hex first (rulings R5.14, R5.15).
        if (movement.Vehicle && state.Unit(movement.Members.Count > 0 ? movement.Members[0] : movement.Movers[0]) is { } vehicle)
        {
            return PlanEndVehicle(scope, arguments, existing, attemptId, expected, label, state, movement, vehicle);
        }

        // A15.43, A15.431: a berserk unit keeps charging while it has the MF for a step on its route; when the model cannot decide the
        // route it may end its move, a recorded deviation (ruling R30.5).
        var note = string.Empty;
        foreach (var unit in ending.Select(state.Unit).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Berserk)))
        {
            if (state.Location(unit.Id) is not { } at)
            {
                continue;
            }

            var (steps, _, undecided) = ChargeSteps(state, unit.Side, at.Location, movement.Charge);
            var left = MfAllotment(state, unit, unit.DoubleTimeMf, Is(unit, Conditions.Cx)) is { } allowance ? (allowance * 2) - (unit.MfSpent * 2) - (unit.HalfMfSpent ? 1 : 0) : 0;
            if (steps.Values.Any(step => step.HalfMf <= left))
            {
                return Refused(scope, label, expected, $"play.berserk-charge: {unit.Id} still has the MF to charge on (A15.43, A15.431)");
            }

            if (undecided is not null)
            {
                note = $"; {unit.Id}'s charge is not decided by the model, so it ends in place (ruling R30.5: {undecided})";
            }
        }

        var remaining = movement.Members.Where(id => !ending.Contains(id, StringComparer.Ordinal)).ToArray();
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "movement-ended", new MovementEnded(ending), null, null)],
            [$"play.end-move: {string.Join(", ", ending)} end their move" + (remaining.Length > 0 ? $"; {string.Join(", ", remaining)} may move on (A4.2)" : string.Empty) + note]);
    }
}
