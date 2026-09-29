using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
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

        // B24.4 (ruling R10.1): rubble costs three MF.
        ["wooden-rubble"] = 6,
        ["stone-rubble"] = 6,
    };

    /// <summary>
    /// The MF a unit may spend this phase (A4.11, A4.42, A4.5, A4.52; rulings R5.1 and R5.4): its allotment (a MMC's four or three, a SMC's
    /// six or three, a berserk unit's eight), plus the MF Double Time adds, at most eight (seven for Conscripts), less one MF for each PP it
    /// carries beyond its IPC (three for a MMC, one for a SMC, none for a wounded SMC; one less while CX). A berserk unit counts only its 1PP SW,
    /// since it abandons the others before it charges (A15.431). Null when the catalog does not decide it.
    /// </summary>
    private int? MfAllotment(GameState state, UnitInstance unit, int doubleTimeMf, bool cx, int bonusMf = 0, int ipcBonus = 0)
    {
        // A12.11 (ruling R10.10): a Dummy stack moves as if it holds a real unit, with four MF.
        if ((unit.Kind == UnitKinds.Dummy ? 4 : Experience.MoveAllowance(state, unit, catalogs, vocabulary)) is not { } allotment || Portage(state, unit) is not { } carried)
        {
            return null;
        }

        if (doubleTimeMf > 0)
        {
            var conscript = unit.Definition is { } reference && FireReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.Class == "conscript";
            allotment = Math.Min(allotment + doubleTimeMf, conscript ? 7 : 8);
        }

        var smc = vocabulary.IsA(unit.Kind, "asl:smc");
        var ipc = (smc ? (Is(unit, Conditions.Wounded) ? 0 : 1) : 3) - (cx ? 1 : 0) + ipcBonus;
        var pp = Is(unit, Conditions.Berserk) ? carried.Where(item => item == 1).Sum() : carried.Sum();

        // B3.4, A4.12 (ruling R10.8): the Road Bonus and a leader's bonus add to the allotment.
        return allotment + bonusMf - Math.Max(0, pp - Math.Max(ipc, 0));
    }

    /// <summary>The PP of each SW a unit possesses (A4.4), from the catalog; null when one is not recorded.</summary>
    private int[]? Portage(GameState state, UnitInstance unit)
    {
        var values = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)
            .Select(item => item.Definition is { } reference
                ? catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:portage")?.Value?.Number
                : null).ToArray();
        if (values.Any(value => value is null))
        {
            return null;
        }

        // A9.8 (ruling R13.6): a dismantled weapon's PP are halved, FRU.
        var halved = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)
            .Select(item => Is(item, Conditions.Dismantled)).ToArray();
        return [.. values.Select((value, index) => halved[index] ? (value!.Value + 1) / 2 : value!.Value)];
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

        // C10.3, C10.111 (ruling R8.6): a crew pushes the Gun it mans, alone, Good Order and unpinned, a QSU Gun; a crew that moves otherwise
        // abandons its Gun. C3.22 (ruling R8.9): a Gun that changed its CA in the PFPh, and its crew, do not move; A4.8: nor a TI unit.
        EquipmentInstance? pushed = null;
        if (Text(arguments, "pushGun", out var pushGunId))
        {
            if (movers is not [{ } pusher] || state.Find(pushGunId) is not EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Manned } manning } gunToPush
                || manning.Holder != pusher.Id || !vocabulary.IsA(pusher.Kind, "asl:crew") && !vocabulary.IsA(pusher.Kind, "asl:half-squad")
                || Is(pusher, Conditions.Pinned) || Is(pusher, Conditions.Broken)
                || OrdnanceReference.Value.Guns.GetValueOrDefault(gunToPush.Definition?.Definition ?? string.Empty) is not { Manhandling: not null })
            {
                return Refused(scope, label, expected, "play.move-push: a Good Order, unpinned crew or HS alone pushes the Gun it mans (C10.3, C10.111)");
            }

            pushed = gunToPush;
        }

        // C10.3: a crew pushing its Gun in the moving stack may push on though its last push made it TI; A4.61: pushing is never Assault Movement.
        if (pushed is not null && assault)
        {
            return Refused(scope, label, expected, "play.move-push: pushing a Gun prevents Assault Movement (C10.3)");
        }

        var pushingOn = pushed is not null && state.Movement?.Members.Contains(movers[0]!.Id) == true;
        if (movers.FirstOrDefault(unit => state.NoMoveThisPlayerTurn.Contains(unit!.Id) || (Is(unit, "asl:ti") && !pushingOn)) is { } halted)
        {
            return Refused(scope, label, expected, Is(halted, Conditions.BoundingFire)
                ? $"play.move-halted: {halted.Id} is an Opportunity Firer and does not move this MPh (A7.25)"
                : $"play.move-halted: {halted.Id} is TI, fired a SW in the PFPh, or changed its Gun's CA there, and does not move this Player Turn (A4.8, A3.3, C3.22)");
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

        // A4.1, A7.83: broken, pinned, or already-ended units do not move; A12.11, A12.14 (ruling R10.10): concealed units and Dummies move, a
        // hidden unit does not.
        if (movers.Any(unit => (unit!.Kind != UnitKinds.Dummy && GameState.Condition(unit, Conditions.Broken) != ConditionState.False)
            || GameState.Condition(unit, Conditions.Pinned) == ConditionState.True || unit.MovementEnded
            || GameState.Condition(unit, Conditions.Hidden) == ConditionState.True))
        {
            return Refused(scope, label, expected, "play.move-unit: every mover is Good Order, unpinned, not hidden, and not done moving (A4.1, A7.83, A12.3)");
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

                // Backlog pass 15 (ruling R15.14): the owner names the 1PP SW it keeps within its IPC; with none named, those first in id order are kept.
                var light = carried.Where(item => portage[item] == 1).OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
                var capacity = vocabulary.IsA(unit!.Kind, "asl:smc") ? 1 : 3;
                if (light.Length > capacity)
                {
                    string[] keep = [.. Strings(arguments, "keep").Where(id => light.Any(item => item.Id == id))];
                    if (keep.Length > capacity)
                    {
                        return Refused(scope, label, expected, $"play.berserk-sw: {unit.Id} keeps at most {capacity} 1PP SW within its IPC (A15.431, A4.42)");
                    }

                    var kept = keep.Concat(light.Select(item => item.Id).Where(id => !keep.Contains(id))).Take(capacity).ToHashSet(StringComparer.Ordinal);
                    abandoned.AddRange(light.Where(item => !kept.Contains(item.Id)));
                }
            }
        }

        // A4.61: Assault Movement is declared before the stack moves, and moves it no more than one Location.
        if (current is not null && (current.Assault || assault))
        {
            return Refused(scope, label, expected, "play.move-assault: Assault Movement is declared at the start of the move and enters one Location (A4.61)");
        }

        // A4.5 (ruling R5.1): Double Time by Infantry neither broken, wounded, berserk, nor already CX, nor whose CX counter left at this MPh's start.
        // E1.51 (backlog pass 16, ruling R16.5): no Double Time for a unit whose NVR is 0.
        if (doubleTime && state.Nvr == 0)
        {
            return Refused(scope, label, expected, "play.night-double-time: with an NVR of 0 a unit may not Double Time (E1.51)");
        }

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

        // A23.3 (ruling R15.2): a DC Placement spends the stack's MF in its own Location.
        if (Text(arguments, "placeDc", out var chargeId))
        {
            return berserk > 0 || current is { Bypass.Count: > 0 } || !Text(arguments, "placeDcAt", out var placeAt) || to != from
                ? Refused(scope, label, expected, "play.dc-arguments: a DC Placement names its DC and Location, the stack stays where it is, not in Bypass; a berserk stack charges (A23.3, A15.431)")
                : PlanPlaceDc(scope, attemptId, expected, label, state, [.. movers.Select(unit => unit!)], ids, from, chargeId, placeAt, assault, doubleTime);
        }

        // A24.1 (ruling R9.5): a SMOKE grenade attempt spends the stack's MF in its own Location.
        if (Text(arguments, "smoke", out var smokeAt))
        {
            // Table player, pass 10: a SMOKE attempt in Bypass is not built.
            if (current is { Bypass.Count: > 0 })
            {
                return Refused(scope, label, expected, "play.smoke-bypass: a SMOKE attempt by a stack in Bypass is not built (A24.1, A4.3; ruling R10.7)");
            }

            return berserk > 0 || !Text(arguments, "smokeBy", out var placerId) || to != from
                ? Refused(scope, label, expected, "play.smoke-arguments: a SMOKE attempt names its squad and Location, and the stack stays where it is; a berserk stack charges (A24.1, A15.43)")
                : PlanSmoke(scope, attemptId, expected, label, actor, state, [.. movers.Select(unit => unit!)], ids, from, placerId, smokeAt, assault, doubleTime);
        }

        var minimumMove = arguments.TryGetProperty("minimumMove", out var minimal) && minimal.ValueKind == JsonValueKind.True;
        List<HexsideDirection>? bypass = null;
        if (arguments.TryGetProperty("bypass", out var bypassList))
        {
            bypass = [];
            foreach (var item in bypassList.ValueKind == JsonValueKind.Array ? bypassList.EnumerateArray() : Enumerable.Empty<JsonElement>())
            {
                if (item.ValueKind != JsonValueKind.String || !Enum.TryParse<HexsideDirection>(item.GetString(), ignoreCase: true, out var side))
                {
                    return Refused(scope, label, expected, "play.invalid-arguments: a Bypass names the hexsides moved along: north, northeast, southeast, south, southwest, northwest");
                }

                bypass.Add(side);
            }
        }

        var (entry, stepReason, bypassing, occupy) = MoveEntry(state, [.. movers.Select(unit => unit!)], from, to, current, bypass);
        if (entry is null)
        {
            return Refused(scope, label, expected, stepReason!);
        }

        var terrain = entry.Terrain;

        // A7.7 (ruling R12.11): the first Location an Encircled unit enters costs twice its MF.
        var halfMf = entry.HalfMf * (movers.Any(unit => state.Encircled(unit!)) ? 2 : 1);

        // C10.3 (ruling R8.6): a Gun is pushed only into Open Ground or grain (a road hex counted as its terrain), at double the MF, never up a
        // level, into Bypass, or by Minimum Move (A4.134).
        if (pushed is not null && (terrain is not ("open-ground" or "grain") || entry.LevelChange || bypassing is not null || occupy || minimumMove))
        {
            return Refused(scope, label, expected, $"play.move-push-terrain: a Gun is pushed only into Open Ground or grain at its own level in the review, not {terrain} (C10.3, A4.134; ruling R8.6)");
        }

        if (pushed is not null)
        {
            halfMf *= 2;
        }

        // A4.134 (ruling R10.9): a Minimum Move is the stack's first and only step, by units with at least one MF after portage.
        if (minimumMove && (current is not null || assault || berserk > 0 || occupy || bypassing is not null
            || movers.Any(unit => unit!.MfSpent != 0 || unit.HalfMfSpent || MfAllotment(state, unit, 0, Is(unit, Conditions.Cx)) is not >= 1)))
        {
            return Refused(scope, label, expected, "play.move-minimum: a Minimum Move is the stack's only step this MPh, by units left with at least one MF after portage, not by Assault Movement, Bypass, or a berserk charge (A4.134; ruling R10.9)");
        }

        if (entry.MinimumMoveOnly && !minimumMove)
        {
            return Refused(scope, label, expected, "play.move-marsh: marsh is entered from a lower elevation only by Minimum Move (B16.4)");
        }

        // B16.4 (ruling R10.1): marsh costs each mover its whole allotment, so only units that have spent no MF this MPh enter it.
        if (entry.AllMf)
        {
            // A4.61 (table player, pass 10): marsh takes all the MF, so it is never Assault Movement.
            if (assault)
            {
                return Refused(scope, label, expected, "play.move-assault: entering marsh uses all of a unit's MF, which Assault Movement may not (A4.61, B16.4)");
            }

            if (!minimumMove && movers.Any(unit => unit!.MfSpent != 0 || unit.HalfMfSpent))
            {
                return Refused(scope, label, expected, "play.move-marsh: entering marsh costs a unit's whole MF allotment, so only units that have spent no MF this MPh enter it (B16.4)");
            }

            // A4.134 EX (referee, pass 10): from a lower level it costs twice the allotment.
            halfMf = movers.Select(unit => MfAllotment(state, unit!, doubleTime ? 2 : unit!.DoubleTimeMf, doubleTime || Is(unit!, Conditions.Cx)) ?? 0).Max() * (entry.MinimumMoveOnly ? 4 : 2);
            if (halfMf <= 0)
            {
                return Refused(scope, label, expected, "play.move-mf: no mover has an MF allotment the catalog decides");
            }
        }

        // A4.14, A12.15 (rulings R10.11, R10.15): a Location with a Known enemy unit is not entered in the MPh, but by a berserk charge; one with only
        // concealed or hidden enemy units or Dummies reveals one, forcing the stack back unless it charges or they were all Dummies.
        UnitInstance[] enemiesThere = charge is not null || occupy || bypassing is not null ? []
            : [.. state.At(to).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide && !Is(unit, Conditions.Captured))];
        UnitInstance[] hiddenEnemies = charge is null || occupy ? []
            : [.. state.At(to).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide && !Is(unit, Conditions.Captured) && !KnownEnemy(unit))];
        if (enemiesThere.Any(KnownEnemy))
        {
            return Refused(scope, label, expected, "play.move-occupied: Infantry may not enter a Location holding a Known enemy unit in the MPh; Infantry OVR outside the reviewed building case is not built (A4.14, A4.15; ruling R10.11)");
        }

        // A12.15 (referee, pass 10): a moving stack of Dummies that tries to enter concealed enemy units is asked to show a real unit, and is removed.
        if (enemiesThere.Length > 0 && movers.All(unit => unit!.Kind == UnitKinds.Dummy))
        {
            List<GameEvent> removed = [.. movers.Select((unit, index) => Event(scope, attemptId, index + 1, expected, "instance-eliminated", new InstanceEliminated(unit!.Id),
                ScenarioA1FirePackage.Identity.ToString(), null))];
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, removed,
                [$"play.move: {string.Join(", ", ids)} attempt {to}, holding concealed enemy units; the moving stack shows no real unit, so its Dummies are removed (A12.15)"]);
        }

        if (enemiesThere.Length > 0 && minimumMove)
        {
            return Refused(scope, label, expected, "play.move-occupied: a Minimum Move into concealed enemy units is not reviewed (A4.134, A12.15)");
        }

        // A4.11, A4.42, A4.5, A4.12, B3.4: the MF each mover has left, with Double Time, portage, and the leader and Road bonuses; A4.61: Assault Movement
        // may not use all of the allotment without Double Time. A Minimum Move needs none (A4.134), and marsh takes it all (B16.4).
        var leaderIpcTo = LeaderIpcRecipient(state, [.. movers.Select(unit => unit!)]);
        foreach (var unit in movers)
        {
            if (minimumMove || entry.AllMf)
            {
                continue;
            }

            var extra = doubleTime ? (unit!.MfSpent == 0 && !unit.HalfMfSpent ? 2 : 1) : unit!.DoubleTimeMf;
            var exhausted = doubleTime || Is(unit, Conditions.Cx);
            // E1.51 (backlog pass 16, ruling R16.5): no road bonus with an NVR of 0.
            var bonus = (entry.RoadRate && !unit.OffRoad && pushed is null && state.Nvr != 0 ? 1 : 0) + (LeaderBonus(state, unit, [.. movers.Select(item => item!)]) ? 2 : 0);
            var ipc = unit.Id == leaderIpcTo.Recipient ? 1 : unit.Id == leaderIpcTo.Leader ? -1 : 0;
            if (MfAllotment(state, unit, extra, exhausted, bonus, ipc) is not { } allowance || MfAllotment(state, unit, 0, exhausted, bonus, ipc) is not { } plain)
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit.Id} has no MF allowance the catalog decides");
            }

            var spent = (unit.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0);
            var left = (allowance * 2) - spent;
            if (left < halfMf || (assault && (plain * 2) - spent <= halfMf))
            {
                return Refused(scope, label, expected, $"play.move-mf: {unit.Id} has {left / 2m} MF left, and the entry costs {halfMf / 2m} (A4.11, A4.42, A4.61; Minimum Move, A4.134)");
            }
        }

        var step = (current?.Step ?? 0) + 1;
        var package = ScenarioA1FirePackage.Identity.ToString();
        var revealing = enemiesThere.Length > 0 ? enemiesThere : hiddenEnemies;
        var realOnes = revealing.Where(unit => unit.Kind != UnitKinds.Dummy).OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        var forcedBack = enemiesThere.Length > 0 && realOnes.Length > 0;
        var moved = new MovementStepped(ids, forcedBack ? from : to, halfMf, assault, step)
        {
            Charge = charge,
            DoubleTime = doubleTime,
            Road = entry.RoadRate && pushed is null && !forcedBack,
            MinimumMove = minimumMove,
            Attempted = forcedBack ? to : null,
            Bypass = bypassing,
        };
        var summary = $"play.move: {string.Join(", ", ids)} " + (forcedBack ? $"attempt {to}" : occupy ? $"occupy the obstacle of {to}" : $"enter {to}")
            + $" ({terrain}{(bypassing is not null ? " in Bypass along " + string.Join(", ", bypassing.Select(side => side.ToString().ToLowerInvariant())) : string.Empty)}) for {halfMf / 2m} MF"
            + (assault ? ", by Assault Movement" : string.Empty)
            + (doubleTime ? ", Double Timing and now CX (A4.5)" : string.Empty)
            + (minimumMove ? ", a Minimum Move: pinned and CX once the DEFENDER's fire is done (A4.134)" : string.Empty)
            + (charge is not null ? $", charging {charge} (A15.43)" : string.Empty)
            + (abandoned.Count > 0 ? $"; {string.Join(", ", abandoned.Select(item => item.Id))} abandoned before the charge (A15.431)" : string.Empty);
        List<GameEvent> prefix = [.. abandoned.Select((item, index) => Event(scope, attemptId, index + 1, expected, "equipment-transferred",
            new EquipmentTransferred(item.Id, null, new MapPosition(from)), package, null))];
        var landed = forcedBack ? from : to;
        var residual = state.ResidualFire.FirstOrDefault(item => item.Location == landed);

        // A9.22 (ruling R12.7): each Fire Lane with Residual FP in the Location attacks the stack after any other Residual FP.
        var lanes = state.FireLanes.SelectMany(lane => lane.Entries.Where(item => item.Location == landed).Select(item => (Lane: lane, Entry: item))).ToArray();
        if (pushed is not null)
        {
            return residual is not null || lanes.Length > 0
                ? Refused(scope, label, expected, "play.move-push-residual: pushing a Gun into Residual FP is not reviewed (C10.3, A8.2)")
                : PushPlan(scope, attemptId, expected, label, actor, state, pushed, moved, halfMf / 2 - (entry.RoadRate ? 2 : 0), summary);
        }

        // A12.15 (rulings R10.11, R10.15): the reveal. Hidden units first go beneath a "?"; a Random Selection among several real units reveals the
        // highest dr (ties all); Dummies alone are removed and the stack enters.
        var needsSelection = revealing.Length > 0 && realOnes.Length > 1;
        void Reveal(List<GameEvent> events, IReadOnlyList<int>? dice)
        {
            foreach (var hidden in revealing.Where(unit => Is(unit, Conditions.Hidden)))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(hidden.Id,
                    new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.False, [Conditions.Concealed] = ConditionState.True }), package, null));
            }

            string[] shown = realOnes.Length == 0 ? [] : dice is null ? [realOnes[0].Id]
                : [.. realOnes.Where((_, index) => dice[index] == dice.Max()).Select(unit => unit.Id)];
            events.AddRange(RevealEvents(scope, attemptId, expected, events.Count + 1, shown));
            if (realOnes.Length == 0 || charge is not null)
            {
                foreach (var dummy in revealing.Where(unit => unit.Kind == UnitKinds.Dummy))
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(dummy.Id), package, null));
                }
            }
        }

        // A12.14 (ruling R10.10): a concealed mover or Dummy loses "?" when it moves without Assault Movement, or into Open Ground, in the LOS of a
        // Good Order enemy ground unit within 16 hexes; a forced back reveals the whole stack (A12.15).
        void Unmask(List<GameEvent> events)
        {
            // E1.31 (backlog pass 16, ruling R16.4): at night a mover loses "?" only by Non-Assault Movement in an Illuminated Location.
            var seen = forcedBack || (EnemyGoodOrderInLosWithin16(state, state.PhasingSide!, landed)
                && (state.Night ? !assault && Illuminated(state, landed) : !assault || terrain == "open-ground"));
            foreach (var unit in movers.Where(unit => seen && (unit!.Kind == UnitKinds.Dummy || Is(unit, Conditions.Concealed))))
            {
                events.Add(unit!.Kind == UnitKinds.Dummy
                    ? Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(unit.Id), package, null)
                    : RevealEvents(scope, attemptId, expected, events.Count + 1, [unit.Id]).Single());
            }
        }

        List<GameEvent> Stepped(IReadOnlyList<int>? dice, Func<RollRequest, RollResult>? draw)
        {
            var events = new List<GameEvent>(prefix);
            if (needsSelection && draw is not null)
            {
                var roll = draw(new RollRequest(realOnes.Length, 6));
                dice = roll.Values;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled($"{attemptId}-reveal", "random-selection", roll.Request.Count, roll.Request.Sides, roll.Values, DiceRolled.SystemSource, actor), package, null));
            }

            if (revealing.Length > 0 && enemiesThere.Length > 0)
            {
                Reveal(events, dice);
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "movement-step", moved, package, null));
            if (revealing.Length > 0 && enemiesThere.Length == 0)
            {
                Reveal(events, dice);
            }

            Unmask(events);
            return events;
        }

        if (revealing.Length > 0)
        {
            summary += forcedBack
                ? $"; a concealed unit at {to} is revealed and the stack stays in {from} with the MF spent, its move ending (A12.15)"
                : charge is not null ? $"; the concealed units at {to} are revealed as the charge enters (A15.431, A12.15)"
                : $"; only Dummies were at {to}, and they are removed (A12.15)";
        }

        if (!needsSelection && residual is null && lanes.Length == 0)
        {
            var events = Stepped(null, null);

            // A12.2 Case H (ruling R6.7): the step may bring a concealed vehicle out of Concealment Terrain into the movers' LOS.
            if (Replay([.. existing, .. events]).Current is { } stepped && VehicleConcealmentLost(stepped, null, false) is { Count: > 0 } lost)
            {
                events = [.. events, .. RevealEvents(scope, attemptId, expected, events.Count + 1, lost)];
                summary += $"; {string.Join(", ", lost)} loses its \"?\" (A12.2)";
            }

            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [summary]);
        }

        // A8.22, A12.15: Residual FP attacks a unit entering its Location, or returned to it, first, alone, with any FFNAM and FFMO.
        FireAttack? residualFacts = null;
        if (residual is not null)
        {
            if (Replay([.. existing, .. Stepped([.. realOnes.Select(_ => 6)], null)]).Current is not { } entered
                || LiveFire.ResidualFromState(entered, landed, residual.Fp) is not ({ } residualAttack, null)
                || FireMapFacts(entered, residualAttack, landed) is not ({ } mapFacts, null))
            {
                return Refused(scope, label, expected, "play.move-residual: the Residual FP attack on the entering stack cannot be read");
            }

            residualFacts = HeatOfBattleFacts(entered, mapFacts);
            var precheck = ScenarioA1FireCalculator.Precheck(residualFacts, FireReference.Value);
            if (precheck.Count != 0)
            {
                return Refused(scope, label, expected, ["play.move-residual: the Fire package does not decide the Residual FP attack this entry would suffer", .. precheck]);
            }
        }

        if (lanes.Length > 0)
        {
            if (Replay([.. existing, .. Stepped([.. realOnes.Select(_ => 6)], null)]).Current is not { } laneState
                || lanes.Any(item => FireLaneFacts(laneState, landed, item.Entry) is not { } laneFacts || ScenarioA1FireCalculator.Precheck(laneFacts, FireReference.Value).Count != 0))
            {
                return Refused(scope, label, expected, "play.move-fire-lane: the Fire package does not decide the Fire Lane attack this entry would suffer (A9.22)");
            }
        }

        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = Stepped(null, draw);
            if (residualFacts is not null && Replay([.. existing, .. events]).Current is { } after)
            {
                AddFireEvents(scope, attemptId, expected, actor, after, residualFacts, state.PhasingSide, step, events, draw);
            }

            foreach (var (lane, entry) in lanes)
            {
                AddFireLaneAttack(scope, attemptId, expected, actor, existing, events, lane, entry, landed, step, draw);
            }

            return events;
        }

        string[] reasons = [summary, .. residual is not null ? [$"play.move: {residual.Fp} Residual FP in {landed} attacks the stack first (A8.22)"] : Array.Empty<string>(),
            .. lanes.Select(item => $"play.move: the Fire Lane of {item.Lane.Weapon} attacks the stack in {landed} with {item.Entry.Fp} Residual FP (A9.22)")];
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], reasons)
        {
            Roll = new PlannedRoll(needsSelection ? "random-selection" : "residual", Build),
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
    private GamePlan PlanEndMove(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
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
            return PlanEndVehicle(scope, arguments, existing, attemptId, expected, label, state, movement, vehicle, actor);
        }

        // A4.32 (ruling R10.7): no unit ends its move in Bypass; it leaves or occupies the obstacle first (a unit that broke or was pinned there has left
        // the stack).
        if (movement.Bypass is { Count: > 0 } && ending.Any(id => movement.Members.Contains(id, StringComparer.Ordinal) && movement.Movers.Contains(id, StringComparer.Ordinal)))
        {
            return Refused(scope, label, expected, "play.end-move-bypass: Infantry may not end its move in Bypass; it leaves the hex or pays to occupy the obstacle (A4.32)");
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
