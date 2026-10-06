using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Hex-by-hex Infantry movement in the MPh (unit step 22): a stack enters an adjacent Location at its terrain's MF cost,
/// and after each MF expenditure the DEFENDER may fire at it (A8.1) or pass; the ATTACKER then continues or ends the move
/// (A8.11). A Location holding Residual FP attacks the stack as it enters, before the DEFENDER may fire (A8.22).
/// </summary>
public sealed partial class GamePlanner
{
    // MF to enter the admitted terrain, in half MF (A4.13, B12.4, B13.4, B14.4, B15.4, B23.4, B24.4), moved to Rules (pass 32.a).
    private static readonly IReadOnlyDictionary<string, int> EntryHalfMf = ScenarioA1ResultTables.EntryHalfMf;

    /// <summary>
    /// The MF a unit may spend this phase (A4.11, A4.42, A4.5, A4.52; rulings R5.1 and R5.4): its allotment (a MMC's four or three, a SMC's
    /// six or three, a berserk unit's eight), plus the MF Double Time adds, at most eight (seven for Conscripts), less one MF for each PP it
    /// carries beyond its IPC (three for a MMC, one for a SMC, none for a wounded SMC; one less while CX). A berserk unit counts only its 1PP SW,
    /// since it abandons the others before it charges (A15.431). Null when the catalog does not decide it.
    /// </summary>
    private int? MfAllotment(GameState state, UnitInstance unit, int doubleTimeMf, bool cx, int bonusMf = 0, int ipcBonus = 0) =>
        ScenarioA1MovementCalculator.MfAllotment(
            new MfAllotmentFacts(unit.Kind == UnitKinds.Dummy, DefinitionOf(unit)?.Class, vocabulary.IsA(unit.Kind, "asl:smc"), Is(unit, Conditions.Wounded), Is(unit, Conditions.Berserk)),
            doubleTimeMf, cx, bonusMf, ipcBonus, () => Experience.MoveAllowance(state, unit, catalogs, vocabulary), () => Portage(state, unit));

    /// <summary>One unit of a moving stack as Rules reads it for the leader bonus (pass 32.b).</summary>
    private MovingUnitFacts MovingUnit(UnitInstance unit) =>
        new(unit.Id, unit.Side, unit.Kind == UnitKinds.Dummy, vocabulary.IsA(unit.Kind, "asl:mmc"), vocabulary.IsA(unit.Kind, "asl:leader"),
            Is(unit, Conditions.Berserk), Is(unit, Conditions.Broken), Is(unit, Conditions.Wounded), Nationality(unit), unit.MfSpent, unit.HalfMfSpent, unit.MovedWith);

    /// <summary>The PP of each SW a unit possesses (A4.4), from the catalog; null when one is not recorded.</summary>
    private int[]? Portage(GameState state, UnitInstance unit)
    {
        var values = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)
            .Select(PortageOf).ToArray();
        return values.Any(value => value is null) ? null : [.. values.Select(value => value!.Value)];
    }

    /// <summary>The PP of one SW (A4.4), from the catalog; a dismantled weapon's are halved, FRU (A9.8; ruling R13.6). Null when none is recorded.</summary>
    public int? PortageOf(EquipmentInstance item) =>
        item is null ? throw new ArgumentNullException(nameof(item)) : item.Definition is { } reference
            && catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:portage")?.Value?.Number is { } value
            ? Is(item, Conditions.Dismantled) ? (value + 1) / 2 : value
            : null;

    /// <summary>
    /// The half MF Infantry spend to enter a terrain (B15.4, B15.6; ruling R5.19): grain costs 1½ MF from April to September and is Open Ground
    /// otherwise, and a game with no scenario month does not decide it; null when the terrain is not a reviewed entry.
    /// </summary>
    private static int? InfantryEntryHalfMf(GameState state, string terrain) => ScenarioA1TerrainCosts.InfantryEntryHalfMf(terrain, state.ScenarioMonth);

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

        // Pass 32.b: the planner reads the state, the map, and the catalog block by block, in the order the old body read them, and Rules decides each
        // block (ScenarioA1MovementCalculator); the arguments' parsing and the reads that belong to other slices (the entry edge, the playable area,
        // the charge route) stay here.
        var found = ids.Select(id => state.Unit(id)).ToArray();
        if (ScenarioA1MovementCalculator.MoveStart(state.Phase, [.. found.Select(unit => new MoveStartUnitFacts(unit is { Status: InstanceStatus.Active }, unit?.Side))], state.PhasingSide) is { } notStarted)
        {
            return Refused(scope, label, expected, notStarted);
        }

        var movers = found.Select(unit => unit!).ToArray();

        // Rulings R20.5, R25.3: a stack waiting off board enters along its entry edge as its first step, which is a step like any other; it never moves
        // with units on the map, and takes no action off board (A2.52).
        var offBoard = movers.Count(unit => unit.Position is OffMapPosition && state.Location(unit.Id) is null);
        HexsideDirection? entering = null;
        if (ScenarioA1MovementCalculator.OffBoardEntry(offBoard, movers.Length, Text(arguments, "smoke", out _) || Text(arguments, "placeDc", out _) || Text(arguments, "pushGun", out _)) is { } notEntering)
        {
            return Refused(scope, label, expected, notEntering);
        }

        if (offBoard > 0)
        {
            var (edge, crossing, barred) = EntryCheck(state, movers, to, false);
            if (edge is null)
            {
                return Refused(scope, label, expected, barred!);
            }

            entering = crossing;
        }

        // An entering stack has no Location yet: the steps below read its origin only for a stack on the map (ruling R25.3).
        var origins = movers.Select(unit => state.Location(unit.Id)?.Location).Distinct().ToArray();
        if (ScenarioA1MovementCalculator.Origin(entering is not null, origins.Length, origins is [{ }]) is { } noOrigin)
        {
            return Refused(scope, label, expected, noOrigin);
        }

        var from = entering is not null ? to : origins[0]!;

        // A2.1 (ruling R20.6): never out of the card's playable area.
        if (to != from && PlayableBar(state, to) is { } outside)
        {
            return Refused(scope, label, expected, outside);
        }

        var current = state.Movement;
        var pushGiven = Text(arguments, "pushGun", out var pushGunId);
        var pusher = movers.Length == 1 ? movers[0] : null;
        var gunToPush = pushGiven && pusher is not null && state.Find(pushGunId) is EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Manned } } gun ? gun : null;
        var bars = new MoveBarsFacts(
            pushGiven,
            pusher is not null,
            gunToPush is not null && gunToPush.Holding!.Holder == pusher!.Id,
            pusher is not null && (vocabulary.IsA(pusher.Kind, "asl:crew") || vocabulary.IsA(pusher.Kind, "asl:half-squad")),
            pusher is not null && Is(pusher, Conditions.Pinned),
            pusher is not null && Is(pusher, Conditions.Broken),
            gunToPush is not null && OrdnanceReference.Value.Guns.GetValueOrDefault(gunToPush.Definition?.Definition ?? string.Empty) is { Manhandling: not null },
            assault,
            pushGiven && current?.Members.Contains(movers[0].Id) == true,
            [.. movers.Select(unit => new MoveBarUnitFacts(unit.Id, state.NoMoveThisPlayerTurn.Contains(unit.Id), Is(unit, "asl:ti"), Is(unit, Conditions.BoundingFire), LiveFire.IsVehicle(unit),
                Is(unit, Conditions.Berserk), GameState.Condition(unit, Conditions.PrepFire) == ConditionState.True, MassacredInPrepFire(existing, unit.Id), Is(unit, Conditions.Melee),
                Is(unit, Conditions.Captured), unit.Kind == UnitKinds.Dummy, GameState.Condition(unit, Conditions.Broken) switch
                {
                    ConditionState.True => true,
                    ConditionState.False => false,
                    _ => null,
                },
                GameState.Condition(unit, Conditions.Pinned) == ConditionState.True, unit.MovementEnded, GameState.Condition(unit, Conditions.Hidden) == ConditionState.True))],
            current is not null,
            current?.Members ?? [],
            current?.Movers ?? [],
            current is { WindowOpen: true },
            EnemyVehicleAt(state, state.PhasingSide!, to)?.Id,
            doubleTime,
            to.ToString());
        if (ScenarioA1MovementCalculator.MoveBars(bars) is { } barredMove)
        {
            return Refused(scope, label, expected, barredMove);
        }

        var pushed = pushGiven ? gunToPush : null;

        // A15.43: at the start of the MPh every berserk unit charges before any other unit moves.
        var berserk = movers.Count(unit => Is(unit, Conditions.Berserk));
        BoardLocation? charge = null;
        if (ScenarioA1MovementCalculator.BerserkFirst(berserk, current is not null, () => MustCharge(state) is [{ } charging, ..] ? charging.Id : null) is { } notFirst)
        {
            return Refused(scope, label, expected, notFirst);
        }

        var abandoned = new List<EquipmentInstance>();
        if (berserk > 0)
        {
            var lane = arguments.TryGetProperty("bypass", out var laneList) && laneList.ValueKind == JsonValueKind.Array
                ? string.Join(",", Strings(arguments, "bypass").Select(item => item.ToLowerInvariant())) : null;
            var (allowed, target, reason) = BerserkStep(state, movers, from, to, current, assault, lane);
            if (allowed is null)
            {
                return Refused(scope, label, expected, reason!);
            }

            charge = target;

            // A15.431: before its charge a berserk unit abandons every SW of more than one PP; its 1PP SW beyond its IPC (A4.42: three for
            // a MMC, one for a SMC) are its own choice, which is not reviewed. The SW held and their printed portage are read here; Rules decides.
            var carriedBy = movers.ToDictionary(unit => unit.Id, unit => state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == unit.Id).ToArray(), StringComparer.Ordinal);
            var (noPortage, abandonedIds) = ScenarioA1MovementCalculator.BerserkAbandons(
                [.. movers.Select(unit => new BerserkAbandonFacts(unit.Id, unit.MfSpent == 0 && !unit.HalfMfSpent, vocabulary.IsA(unit.Kind, "asl:smc"),
                    [.. carriedBy[unit.Id].Select(item => new BerserkCarriedFacts(item.Id, item.Definition is { } reference
                        ? catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:portage")?.Value?.Number
                        : null))]))],
                [.. Strings(arguments, "keep")]);
            if (noPortage is not null)
            {
                return Refused(scope, label, expected, noPortage);
            }

            var items = carriedBy.Values.SelectMany(list => list).ToDictionary(item => item.Id, StringComparer.Ordinal);
            abandoned.AddRange(abandonedIds.Select(id => items[id]));
        }

        if (ScenarioA1MovementCalculator.MoveTiming(current is not null, current?.Assault == true, assault, doubleTime, state.Nvr,
            [.. movers.Select(unit => new MoveTimingUnitFacts(unit.Id, Is(unit, Conditions.Wounded), Is(unit, Conditions.Berserk), Is(unit, Conditions.Cx), state.NoDoubleTime.Contains(unit.Id),
                vocabulary.IsA(unit.Kind, "asl:smc"), Portage(state, unit)?.Sum()))]) is { } badTiming)
        {
            return Refused(scope, label, expected, badTiming);
        }

        var action = ScenarioA1MovementCalculator.SpecialAction(Text(arguments, "placeDc", out var chargeId), Text(arguments, "placeDcAt", out var placeAt),
            Text(arguments, "smoke", out var smokeAt), Text(arguments, "smokeBy", out var placerId), berserk, current is { Bypass.Count: > 0 }, to == from);
        if (action.Refusal is { } noAction)
        {
            return Refused(scope, label, expected, noAction);
        }

        if (action.PlaceDc)
        {
            return PlanPlaceDc(scope, attemptId, expected, label, state, movers, ids, from, chargeId, placeAt, assault, doubleTime);
        }

        if (action.Smoke)
        {
            return PlanSmoke(scope, attemptId, expected, label, actor, state, movers, ids, from, placerId, smokeAt, assault, doubleTime);
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

        var (entry, stepReason, bypassing, occupy) = entering is { } edgeSide
            ? EntryStep(state, movers, to, edgeSide, bypass)
            : MoveEntry(state, movers, from, to, current, bypass);
        if (entry is null)
        {
            return Refused(scope, label, expected, stepReason!);
        }

        var terrain = entry.Terrain;
        var (noCost, halfMf) = ScenarioA1MovementCalculator.EntryCost(
            new EntryCostFacts(entry, movers.Any(state.Encircled), pushed is not null, bypassing is not null, occupy, minimumMove, assault, berserk, current is not null, doubleTime,
                [.. movers.Select(unit => new EntryMoverFacts(unit.MfSpent != 0 || unit.HalfMfSpent, unit.DoubleTimeMf, Is(unit, Conditions.Cx)))]),
            (index, doubleTimeMf, cx) => MfAllotment(state, movers[index], doubleTimeMf, cx));
        if (noCost is not null)
        {
            return Refused(scope, label, expected, noCost);
        }

        // A4.14, A12.15 (rulings R10.11, R10.15): the units of the target Location are read here, and Rules decides who is revealed and who is barred.
        var there = state.At(to).OfType<UnitInstance>().ToArray();
        var enemies = ScenarioA1MovementCalculator.EnemiesAtTarget(
            [.. there.Select(unit => new UnitAtTargetFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, Is(unit, Conditions.Captured), KnownEnemy(unit), unit.Kind == UnitKinds.Dummy))],
            state.PhasingSide, charge is not null, occupy, bypassing is not null, movers.All(unit => unit.Kind == UnitKinds.Dummy), minimumMove, string.Join(", ", ids), to.ToString());
        if (enemies.Refusal is { } occupied)
        {
            return Refused(scope, label, expected, occupied);
        }

        if (enemies.RemoveDummies)
        {
            List<GameEvent> removed = [.. movers.Select((unit, index) => Event(scope, attemptId, index + 1, expected, "instance-eliminated", new InstanceEliminated(unit.Id),
                ScenarioA1FirePackage.Identity.ToString(), null))];
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, removed, [enemies.Summary!]);
        }

        var byId = there.ToDictionary(unit => unit.Id, StringComparer.Ordinal);
        UnitInstance[] enemiesThere = [.. enemies.EnemiesThere.Select(id => byId[id])];
        UnitInstance[] hiddenEnemies = [.. enemies.HiddenEnemies.Select(id => byId[id])];

        // A4.11, A4.42, A4.5, A4.12, B3.4, A4.61, A4.134, B16.4: the MF each mover has left, decided by Rules from each mover's facts and allotment.
        var leaderIpcTo = LeaderIpcRecipient(state, movers);
        if (ScenarioA1MovementCalculator.MfLeft(
            [.. movers.Select(unit => new MfLeftMoverFacts(unit.Id, unit.MfSpent == 0 && !unit.HalfMfSpent, unit.DoubleTimeMf, Is(unit, Conditions.Cx), unit.OffRoad, LeaderBonus(state, unit, movers),
                (unit.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0)))],
            minimumMove, entry.AllMf, doubleTime, entry.RoadRate, pushed is not null, state.Nvr, assault, halfMf, leaderIpcTo,
            (index, doubleTimeMf, cx, bonusMf, ipcBonus) => MfAllotment(state, movers[index], doubleTimeMf, cx, bonusMf, ipcBonus)) is { } noMf)
        {
            return Refused(scope, label, expected, noMf);
        }

        var step = (current?.Step ?? 0) + 1;
        var package = ScenarioA1FirePackage.Identity.ToString();

        // A12.15, A.9, A12.11, A2.51 (rulings R10.11, R10.15, R25.3, R27.3): who is revealed, whether the stack is forced back, the record's Road
        // Bonus, the summary, and the Random Selection's pool are decided by Rules from the facts read above; the events are built here.
        var verdict = ScenarioA1MovementCalculator.MoveStep(new MoveStepFacts(ids, from.ToString(), to.ToString(), entering is not null, charge?.ToString(), occupy, terrain,
            bypassing is null ? null : [.. bypassing.Select(side => side.ToString().ToLowerInvariant())], halfMf, assault, doubleTime, minimumMove,
            [.. abandoned.Select(item => item.Id)], entry.RoadRate, pushed is not null, state.Night, movers.Length > 0 && movers.All(unit => unit.Kind == UnitKinds.Dummy),
            [.. enemiesThere.Select(unit => new MoveRevealUnitFacts(unit.Id, unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Hidden)))],
            [.. hiddenEnemies.Select(unit => new MoveRevealUnitFacts(unit.Id, unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Hidden)))]));
        var forcedBack = verdict.ForcedBack;
        var moved = new MovementStepped(ids, forcedBack ? from : to, halfMf, assault, step)
        {
            Charge = charge,
            DoubleTime = doubleTime,
            Road = verdict.RoadBonus,
            MinimumMove = minimumMove,
            Attempted = forcedBack ? to : null,
            Bypass = bypassing,
        };
        var summary = verdict.Summary;
        var dummyWarning = verdict.DummyWarning;
        List<GameEvent> prefix = [.. abandoned.Select((item, index) => Event(scope, attemptId, index + 1, expected, "equipment-transferred",
            new EquipmentTransferred(item.Id, null, new MapPosition(from)), package, null))];
        var landed = forcedBack ? from : to;
        var offMap = verdict.OffMap;
        var residual = offMap ? null : state.ResidualFire.FirstOrDefault(item => item.Location == landed);

        // A9.22 (ruling R12.7): each Fire Lane with Residual FP in the Location attacks the stack after any other Residual FP.
        var lanes = offMap ? [] : state.FireLanes.SelectMany(lane => lane.Entries.Where(item => item.Location == landed).Select(item => (Lane: lane, Entry: item))).ToArray();
        var gate = ScenarioA1MovementCalculator.StepGate(verdict.NeedsSelection, residual is not null, lanes.Length, pushed is not null);
        if (gate.Refusal is { } noPush)
        {
            return Refused(scope, label, expected, noPush);
        }

        if (gate.Push)
        {
            return PushPlan(scope, attemptId, expected, label, actor, state, pushed!, moved, halfMf / 2 - (entry.RoadRate ? 2 : 0), verdict.PushSummary);
        }

        string[] pool = [.. verdict.Pool];
        var needsSelection = verdict.NeedsSelection;
        void Reveal(List<GameEvent> events, IReadOnlyList<int>? dice)
        {
            var (toConceal, shown, drawnDummies) = ScenarioA1MovementCalculator.RevealDraw(verdict, dice);
            foreach (var hidden in toConceal)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(hidden,
                    new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.False, [Conditions.Concealed] = ConditionState.True }), package, null));
            }

            events.AddRange(RevealEvents(scope, attemptId, expected, events.Count + 1, shown));
            foreach (var dummy in drawnDummies)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(dummy), package, null));
            }
        }

        // A12.14, A12.15, E1.31 (rulings R10.10, R16.4): the movers that lose their "?" are decided by Rules; the LOS scan and the illumination are read when it asks.
        void Unmask(List<GameEvent> events)
        {
            foreach (var unit in ScenarioA1MovementCalculator.Unmasked([.. movers.Select(mover => new MoverConcealmentFacts(mover.Id, mover.Kind == UnitKinds.Dummy, Is(mover, Conditions.Concealed)))],
                forcedBack, assault, state.Night, terrain, () => EnemyGoodOrderInLosWithin16(state, state.PhasingSide!, landed), () => Illuminated(state, landed)))
            {
                events.Add(unit.Dummy
                    ? Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(unit.Id), package, null)
                    : RevealEvents(scope, attemptId, expected, events.Count + 1, [unit.Id]).Single());
            }
        }

        List<GameEvent> Stepped(IReadOnlyList<int>? dice, Func<RollRequest, RollResult>? draw)
        {
            var events = new List<GameEvent>(prefix);
            if (needsSelection && draw is not null)
            {
                var roll = draw(new RollRequest(pool.Length, 6));
                dice = roll.Values;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled($"{attemptId}-reveal", "random-selection", roll.Request.Count, roll.Request.Sides, roll.Values, DiceRolled.SystemSource, actor), package, null));
            }

            if (verdict.RevealBeforeStep)
            {
                Reveal(events, dice);
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "movement-step", moved, package, null));
            if (verdict.RevealAfterStep)
            {
                Reveal(events, dice);
            }

            Unmask(events);
            return events;
        }

        if (gate.ReadyWithoutRoll)
        {
            var events = Stepped(null, null);

            // A12.2 Case H (ruling R6.7): the step may bring a concealed vehicle out of Concealment Terrain into the movers' LOS.
            if (!offMap && Replay([.. existing, .. events]).Current is { } stepped && VehicleConcealmentLost(stepped, null, false) is { Count: > 0 } lost)
            {
                events = [.. events, .. RevealEvents(scope, attemptId, expected, events.Count + 1, lost)];
                summary += $"; {string.Join(", ", lost)} loses its \"?\" (A12.2)";
            }

            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [summary, .. dummyWarning]);
        }

        // A8.22, A12.15: Residual FP attacks a unit entering its Location, or returned to it, first, alone, with any FFNAM and FFMO.
        FireAttack? residualFacts = null;
        if (residual is not null)
        {
            if (Replay([.. existing, .. Stepped([.. pool.Select(_ => 6)], null)]).Current is not { } entered
                || LiveFire.ResidualFromState(entered, landed, residual.Fp) is not ({ } residualAttack, null)
                || FireMapFacts(entered, residualAttack, landed) is not ({ } mapFacts, null))
            {
                return Refused(scope, label, expected, ScenarioA1MovementCalculator.ResidualUnreadable);
            }

            residualFacts = HeatOfBattleFacts(entered, mapFacts);
            if (ScenarioA1MovementCalculator.ResidualUndecided(ScenarioA1FireCalculator.Precheck(residualFacts, FireReference.Value)) is { } undecided)
            {
                return Refused(scope, label, expected, [.. undecided]);
            }
        }

        if (lanes.Length > 0)
        {
            if (Replay([.. existing, .. Stepped([.. pool.Select(_ => 6)], null)]).Current is not { } laneState
                || lanes.Any(item => FireLaneFacts(laneState, landed, item.Entry) is not { } laneFacts || ScenarioA1FireCalculator.Precheck(laneFacts, FireReference.Value).Count != 0))
            {
                return Refused(scope, label, expected, ScenarioA1MovementCalculator.FireLaneUndecided);
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

        var reasons = ScenarioA1MovementCalculator.StepReasons(summary, dummyWarning, residual?.Fp, landed.ToString(), [.. lanes.Select(item => new FireLaneAttackFacts(item.Lane.Weapon, item.Entry.Fp))]);
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [.. reasons])
        {
            Roll = new PlannedRoll(verdict.RollPurpose, Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// The first step of a stack waiting off board (A2.51, A2.6; ruling R25.3): into a ground-level hex of its entry edge across the edge's hexside, as if from
    /// the mirror-image hex beyond it at the same level: the hex's own cost, at the road rate across a road hexside, one MF more across a wall or hedge,
    /// or in Bypass along one or two hexsides starting at a vertex of the edge's hexside.
    /// </summary>
    private (InfantryEntry? Entry, string? Reason, IReadOnlyList<HexsideDirection>? Bypass, bool Occupy) EntryStep(GameState state, UnitInstance[] movers, BoardLocation to,
        HexsideDirection side, List<HexsideDirection>? bypass)
    {
        // Pass 32.b: the hex and the edge's hexside are read here, and Rules decides which step is read.
        var read = ReadLocation(state, to);
        var crossed = HexsideAt(state, to, side);
        var (entry, reason) = ScenarioA1TerrainCosts.EntryStep(bypass is not null, read is not null && crossed is not null, to.ToString(),
            () =>
            {
                var (lane, laneReason, _, _) = BypassStep(state, movers, to, side, read!.Hex.BaseLevel, WallOn(crossed), bypass!);
                return (lane, laneReason);
            },
            () => EntryGround(state, to, side));
        return (entry, reason, bypass is not null && entry is not null ? bypass : null, false);
    }

    /// <summary>
    /// The cost of crossing a map edge's hexside into a ground-level hex, or out of it into the mirror-image hex beyond (A2.51, A2.6; rulings R25.3, R25.5):
    /// the hex's own terrain at the same level, at the road rate across a road hexside (the A2.6 EX's 2Y1), one MF more across a wall or hedge.
    /// </summary>
    private (InfantryEntry? Entry, string? Reason) EntryGround(GameState state, BoardLocation at, HexsideDirection side)
    {
        // Pass 32.b: the hex and the edge's hexside are read here, and Rules decides.
        var read = ReadLocation(state, at);
        var crossed = HexsideAt(state, at, side);
        return ScenarioA1TerrainCosts.EntryGround(read is null ? null : TerrainKey(read), crossed is null ? null : CrossedFacts(crossed), at.ToString(), state.ScenarioMonth,
            () => BlazeEntryHalfMf(state, at), (terrain, road, rise) => InfantryWeatherHalfMf(state, crossed!, terrain, road, rise));
    }

    /// <summary>
    /// A berserk stack's step (A15.43, A15.431): every berserk unit of the Location that is not done moving, with the same wounded
    /// status, moves together and alone, never by Assault Movement, onto a shortest route to the nearest Known enemy unit in its
    /// LOS, in Bypass when the route takes it so (<paramref name="lane"/>, ruling R27.2); it enters that unit's Location. Once in a
    /// Location with a Known enemy unit it moves no farther. Returns the charged Location, or why the step is refused.
    /// </summary>
    private (bool? Allowed, BoardLocation? Target, string? Reason) BerserkStep(GameState state, UnitInstance[] movers, BoardLocation from, BoardLocation to,
        MovementState? current, bool assault, string? lane)
    {
        // Pass 32.b: the stack and the units of its Location are read here, the route search only when Rules asks, and Rules decides.
        var route = new Lazy<(IReadOnlyDictionary<BoardLocation, ChargeStep> Steps, BoardLocation? Target, string? Undecided)>(() => ChargeSteps(state, movers, from, current));
        var (allowed, _, reason) = ScenarioA1MovementCalculator.BerserkStep(
            [.. movers.Select(unit => new BerserkMoverFacts(unit.Id, unit.Side, Is(unit, Conditions.Berserk), Is(unit, Conditions.Wounded)))], assault, current is not null,
            [.. state.At(from).OfType<UnitInstance>().Select(unit => new BerserkNeighbourFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, Is(unit, Conditions.Berserk), unit.MovementEnded, Is(unit, Conditions.Wounded)))],
            to.ToString(), lane,
            () => new ChargeRouteFacts(route.Value.Steps.ToDictionary(item => item.Key.ToString(), item => new ChargeStepFacts(item.Value.Target.ToString(), item.Value.HalfMf, item.Value.Plain, item.Value.Lanes), StringComparer.Ordinal),
                route.Value.Undecided));
        return (allowed, allowed is true ? route.Value.Steps[to].Target : null, reason);
    }

    /// <summary>The DEFENDER passes on the moving stack's latest MF expenditure (A8.11).</summary>
    private GamePlan PlanPassFire(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        // Pass 32.b: the moving stack is read here, and Rules decides.
        var movement = state.Movement;
        var (refusal, gone, summary) = ScenarioA1MovementCalculator.PassFire(movement is null ? null
            : new PassFireFacts(movement.WindowOpen, movement.Members.Count, movement.Vehicle, movement.Ending, movement.Overrun is not null, movement.Movers,
                [.. movement.Movers.Where(id => state.Unit(id) is { Status: InstanceStatus.Active })], movement.Location.ToString()));
        if (refusal is not null)
        {
            return Refused(scope, label, expected, refusal);
        }

        List<GameEvent> events = [Event(scope, attemptId, 1, expected, "movement-window-closed", new MovementWindowClosed(movement!.Step), null, null)];
        if (gone)
        {
            events.Add(Event(scope, attemptId, 2, expected, "movement-ended", new MovementEnded([.. movement.Movers]), null, null));
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [summary]);
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

        // Pass 32.b: the moving stack and the units named are read here, a berserk unit's route only when Rules asks, and Rules decides.
        var movement = state.Movement;
        var named = Strings(arguments, "unitIds").ToArray();
        EndMoveBerserkFacts? Charge(string id)
        {
            if (state.Unit(id) is not { Status: InstanceStatus.Active } unit || !Is(unit, Conditions.Berserk) || state.Location(unit.Id) is not { } at)
            {
                return null;
            }

            var (steps, _, undecided) = ChargeSteps(state, [unit], at.Location, movement);
            var left = MfAllotment(state, unit, unit.DoubleTimeMf, Is(unit, Conditions.Cx)) is { } allowance ? (allowance * 2) - (unit.MfSpent * 2) - (unit.HalfMfSpent ? 1 : 0) : 0;
            return new EndMoveBerserkFacts([.. steps.Values.Select(step => step.HalfMf)], undecided, left);
        }

        var verdict = ScenarioA1MovementCalculator.EndMove(movement is null ? null
            : new EndMoveFacts(movement.WindowOpen, movement.Location.ToString(), movement.Members, movement.Movers,
                movement.Vehicle && state.Unit(movement.Members.Count > 0 ? movement.Members[0] : movement.Movers[0]) is not null, movement.Bypass is { Count: > 0 }, named), Charge);
        if (verdict.Refusal is { } refusal)
        {
            return Refused(scope, label, expected, refusal);
        }

        // D2.1, D2.4: a vehicle ends its move in Motion or stopped, spending its MP left in its hex first (rulings R5.14, R5.15).
        if (verdict.Vehicle)
        {
            return PlanEndVehicle(scope, arguments, existing, attemptId, expected, label, state, movement!, state.Unit(movement!.Members.Count > 0 ? movement.Members[0] : movement.Movers[0])!, actor);
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "movement-ended", new MovementEnded(verdict.Ending), null, null)],
            [verdict.Summary]);
    }
}
