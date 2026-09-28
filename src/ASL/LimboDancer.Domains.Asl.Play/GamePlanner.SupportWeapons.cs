using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Squads and support weapons outside fire (backlog pass 13, rulings R13.4 to R13.6): Deployment and Recombining in the RPh, SW transfer, drop,
/// and Recovery, and dismantling and assembling the German MMG.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>A10.6, A1.31: a Good Order unit: not broken, Disrupted, in Melee, a prisoner, or TI.</summary>
    private static bool GoodOrder(UnitInstance unit) => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Broken) && !Is(unit, Conditions.Disrupted)
        && !Is(unit, Conditions.Melee) && !Is(unit, Conditions.Captured) && !Is(unit, "asl:ti");

    private static FireDefinition? DefinitionOf(UnitInstance unit) =>
        unit.Definition is { } reference ? FireReference.Value.Definitions.GetValueOrDefault(reference.Definition) : null;

    /// <summary>A1.31, A1.32: Guards Deploy and Recombine without a leader.</summary>
    private static bool Guards(UnitInstance unit) => unit.Definition?.Definition.Contains("guards", StringComparison.Ordinal) == true;

    /// <summary>Why a unit may take no more RPh actions this RPh (A3.1, A1.31; ruling R13.4), or null.</summary>
    private static string? RallyPhaseActionBar(GameState state, string id) =>
        state.RallyPhaseActions.Contains(id) ? $"{id} has taken its RPh action (a Deployment, Recombination, Recovery, or Transfer)"
        : state.RallyAttemptsThisPlayerTurn.Contains(id) ? $"{id} attempted to rally this RPh"
        : state.RepairsThisPhase.Contains(id) ? $"{id} attempted a Repair this RPh"
        : null;

    /// <summary>The leaders who directed a Rally attempt this RPh (A1.31; referee, pass 13): their RPh action is spent.</summary>
    private static HashSet<string> RallyingLeaders(IReadOnlyList<GameEvent> existing)
    {
        var start = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged).index;
        return existing.Skip(start).Select(item => item.Payload).OfType<RallyAttempted>().Select(item => item.Leader).OfType<string>().ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// The Good Order leader of a unit's nationality in its Location who directs a Deployment or Recombination (A1.31, A1.32), or why not; a leader who
    /// has only directed Recombinations may direct another (<paramref name="recombining"/>).
    /// </summary>
    private (UnitInstance? Leader, string? Reason) DirectingLeader(GameState state, IReadOnlyList<GameEvent> existing, UnitInstance unit, BoardLocation at, string? leaderId,
        bool recombining = false)
    {
        if (leaderId is null)
        {
            return Guards(unit) ? (null, null) : (null, $"play.deploy-leader: {unit.Id} needs a Good Order leader of its nationality in its Location (A1.31, A1.32)");
        }

        if (state.Unit(leaderId) is not { } leader || !vocabulary.IsA(leader.Kind, "asl:leader") || !GoodOrder(leader) || leader.Side != unit.Side
            || state.Location(leader.Id)?.Location != at || DefinitionOf(leader)?.Nationality != DefinitionOf(unit)?.Nationality)
        {
            return (null, $"play.deploy-leader: '{leaderId}' is not a Good Order leader of {unit.Id}'s nationality in its Location (A1.31, A1.32)");
        }

        if (RallyingLeaders(existing).Contains(leader.Id))
        {
            return (null, $"play.rph-action: {leader.Id} directed a Rally attempt this RPh (A1.31)");
        }

        if (recombining && RecombiningOnly(existing, leader.Id))
        {
            return (leader, null);
        }

        return RallyPhaseActionBar(state, leader.Id) is { } bar ? (null, $"play.rph-action: {bar} (A1.31)") : (leader, null);
    }

    /// <summary>Whether a leader's only RPh actions this RPh are Recombinations he permitted (A1.32: any eligible HS in his Location).</summary>
    private static bool RecombiningOnly(IReadOnlyList<GameEvent> existing, string leader)
    {
        var start = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged).index;
        var actions = existing.Skip(start).Select(item => item.Payload).ToArray();
        return actions.OfType<RallyPhaseActionTaken>().Any(item => item.Action == "recombine" && item.Units.Contains(leader))
            && !actions.OfType<RallyPhaseActionTaken>().Any(item => item.Action != "recombine" && item.Units.Contains(leader))
            && !actions.OfType<DeploymentAttempted>().Any(item => item.Leader == leader);
    }

    /// <summary>The SW a unit possesses, in id order.</summary>
    private static EquipmentInstance[] Held(GameState state, string unitId) =>
        [.. state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unitId)
            .OrderBy(item => item.Id, StringComparer.Ordinal)];

    /// <summary>
    /// Deployment (A1.31; ruling R13.4): in its RPh a Good Order squad with a Good Order leader of its nationality in its Location takes a NTC modified
    /// by his leadership (Guards need no leader and no NTC); passed, it becomes two HS, the first keeping its SW unless some are named for the second.
    /// </summary>
    private GamePlan PlanDeploy(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "squadId", out var squadId))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Deployment names the squad");
        }

        if (state.Phase != "rph")
        {
            return Refused(scope, label, expected, "play.deploy-phase: squads Deploy in the RPh (A1.31)");
        }

        if (state.Unit(squadId) is not { } squad || !vocabulary.IsA(squad.Kind, "asl:squad") || !GoodOrder(squad) || state.Location(squad.Id)?.Location is not { } at)
        {
            return Refused(scope, label, expected, $"play.deploy-unit: '{squadId}' is not a Good Order squad on the map (A1.31)");
        }

        if (RallyPhaseActionBar(state, squad.Id) is { } squadBar)
        {
            return Refused(scope, label, expected, $"play.rph-action: {squadBar} (A1.31)");
        }

        if (DefinitionOf(squad) is not { Morale: { } morale } || ScenarioA1FireReference.HalfSquadOf(squad.Definition!.Definition) is not { } half)
        {
            return Refused(scope, label, expected, $"play.deploy-unit: the catalog has no Morale Level or HS for {squad.Id}");
        }

        var (leader, leaderReason) = DirectingLeader(state, existing, squad, at, Text(arguments, "leader", out var named) ? named : null);
        if (leaderReason is not null)
        {
            return Refused(scope, label, expected, leaderReason);
        }

        var held = Held(state, squad.Id);
        string[] second = [.. Strings(arguments, "secondWeapons")];
        if (second.FirstOrDefault(id => held.All(item => item.Id != id)) is { } stray)
        {
            return Refused(scope, label, expected, $"play.deploy-weapons: {squad.Id} does not possess '{stray}'");
        }

        // A1.31, A10.7: the leader's modifier, one worse when wounded (A17.3).
        var drm = leader is null ? 0 : (DefinitionOf(leader)?.Leadership ?? 0) + (Is(leader, Conditions.Wounded) ? 1 : 0);
        var package = ScenarioA1FirePackage.Identity.ToString();
        var first = $"{attemptId}-{squad.Id}-1";
        var other = $"{attemptId}-{squad.Id}-2";
        void AddDeployed(List<GameEvent> events, string? cause)
        {
            var conditions = new Dictionary<string, ConditionState>(squad.Conditions, StringComparer.Ordinal);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "lineage", new LineageRecorded(LineageAction.Deployed, [squad.Id],
                [new NewInstance(first, "asl:half-squad", half, squad.Side, null, null, conditions), new NewInstance(other, "asl:half-squad", half, squad.Side, null, null, conditions)]),
                package, null, cause is null ? null : [cause]));
            foreach (var weapon in held)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "equipment-transferred",
                    new EquipmentTransferred(weapon.Id, new Holding(second.Contains(weapon.Id) ? other : first, HoldingRole.Possessed), null), package, null));
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "rph-action-taken", new RallyPhaseActionTaken([first, other], "deployed"), package, null));
        }

        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var drawn = draw(new RollRequest(2, 6));
            var rollId = $"{attemptId}-roll-1";
            var passed = drawn.Values[0] + drawn.Values[1] + drm <= morale;
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "deployment", 2, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null),
                Event(scope, attemptId, 2, expected, "deployment-attempted", new DeploymentAttempted(squad.Id, leader?.Id, rollId, morale, drm, passed), package, null),
            };
            if (passed)
            {
                AddDeployed(events, EventId(attemptId, 2));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.deploy: {squad.Id} takes a NTC (Morale Level {morale}, DRM {drm:+0;-0;0}{(leader is null ? ", a Guards squad with no leader" : $" for {leader.Id}")}) to Deploy into two HS (A1.31)"])
        {
            Roll = new PlannedRoll("deployment", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// Recombining (A1.32; ruling R13.4): in their RPh two Good Order HS of one definition in a Location, with a Good Order leader of their nationality
    /// there (Guards need none), become their squad, which keeps their SW; it is their sole RPh action and the leader's. A Fanatic HS with an
    /// unfanatic one makes an unfanatic squad.
    /// </summary>
    private GamePlan PlanRecombine(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (Strings(arguments, "halfSquads").Distinct(StringComparer.Ordinal).ToArray() is not [{ } oneId, { } twoId])
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Recombination names two HS");
        }

        if (state.Phase != "rph")
        {
            return Refused(scope, label, expected, "play.recombine-phase: HS Recombine in the RPh (A1.32)");
        }

        if (state.Unit(oneId) is not { } one || state.Unit(twoId) is not { } two || !vocabulary.IsA(one.Kind, "asl:half-squad") || !vocabulary.IsA(two.Kind, "asl:half-squad")
            || !GoodOrder(one) || !GoodOrder(two) || one.Side != two.Side || one.Definition?.Definition != two.Definition?.Definition
            || state.Location(one.Id)?.Location is not { } at || state.Location(two.Id)?.Location != at)
        {
            return Refused(scope, label, expected, "play.recombine-unit: Recombining takes two Good Order HS of one definition in one Location (A1.32)");
        }

        if ((RallyPhaseActionBar(state, one.Id) ?? RallyPhaseActionBar(state, two.Id)) is { } bar)
        {
            return Refused(scope, label, expected, $"play.rph-action: {bar} (A1.32)");
        }

        if (ScenarioA1FireReference.SquadOf(one.Definition!.Definition) is not { } squad)
        {
            return Refused(scope, label, expected, $"play.recombine-unit: the catalog has no squad for {one.Definition.Definition}");
        }

        var (leader, leaderReason) = DirectingLeader(state, existing, one, at, Text(arguments, "leader", out var named) ? named : null, recombining: true);
        if (leaderReason is not null)
        {
            return Refused(scope, label, expected, leaderReason);
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        var produced = $"{attemptId}-{one.Id}";
        var conditions = new Dictionary<string, ConditionState>(one.Conditions, StringComparer.Ordinal)
        {
            [Conditions.Fanatic] = Is(one, Conditions.Fanatic) && Is(two, Conditions.Fanatic) ? ConditionState.True : ConditionState.False,
        };
        string[] acting = [one.Id, two.Id, .. leader is null || state.RallyPhaseActions.Contains(leader.Id) ? Array.Empty<string>() : [leader.Id]];
        var events = new List<GameEvent>
        {
            Event(scope, attemptId, 1, expected, "rph-action-taken", new RallyPhaseActionTaken(acting, "recombine"), package, null),
            Event(scope, attemptId, 2, expected, "lineage", new LineageRecorded(LineageAction.Recombined, [one.Id, two.Id],
                [new NewInstance(produced, "asl:squad", squad, one.Side, null, null, conditions)]), package, null),
        };
        foreach (var weapon in Held(state, one.Id).Concat(Held(state, two.Id)))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "equipment-transferred",
                new EquipmentTransferred(weapon.Id, new Holding(produced, HoldingRole.Possessed), null), package, null));
        }

        events.Add(Event(scope, attemptId, events.Count + 1, expected, "rph-action-taken", new RallyPhaseActionTaken([produced], "recombined"), package, null));

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
            [$"play.recombine: {one.Id} and {two.Id} Recombine into {produced}{(leader is null ? "" : $", directed by {leader.Id}")} (A1.32)"]);
    }

    /// <summary>
    /// A SW transfer (A4.431; ruling R13.5): between Good Order unpinned units of one side in one Location, in their RPh as an RPh action, or in their
    /// APh before either advances; never in the phase the SW was Recovered.
    /// </summary>
    private GamePlan PlanTransfer(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var fromId) || !Text(arguments, "equipmentId", out var equipmentId) || !Text(arguments, "toUnitId", out var toId))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a transfer names the possessing unit, the SW, and the unit that receives it");
        }

        if (state.Phase is not ("rph" or "aph"))
        {
            return Refused(scope, label, expected, "play.transfer-phase: a SW is transferred in the RPh or at the start of the APh (A4.431)");
        }

        if (state.Unit(fromId) is not { } giver || state.Unit(toId) is not { } taker || giver.Id == taker.Id || !GoodOrder(giver) || !GoodOrder(taker)
            || Is(giver, Conditions.Pinned) || Is(taker, Conditions.Pinned) || giver.Side != taker.Side || state.Location(giver.Id)?.Location is not { } at
            || state.Location(taker.Id)?.Location != at || LiveFire.IsVehicle(giver) || LiveFire.IsVehicle(taker))
        {
            return Refused(scope, label, expected, "play.transfer-unit: a SW passes between Good Order unpinned Infantry of one side in one Location (A4.431)");
        }

        if (Held(state, giver.Id).FirstOrDefault(item => item.Id == equipmentId) is not { } weapon)
        {
            return Refused(scope, label, expected, $"play.transfer-weapon: {giver.Id} does not possess '{equipmentId}'");
        }

        if (state.RecoveredThisPhase.Contains(weapon.Id))
        {
            return Refused(scope, label, expected, $"play.transfer-weapon: {weapon.Id} was Recovered this phase and is not transferred in it (A4.44)");
        }

        if (state.Phase == "rph" && new[] { giver.Id, taker.Id }.FirstOrDefault(state.RallyPhaseActions.Contains) is { } acted)
        {
            return Refused(scope, label, expected, $"play.rph-action: {acted} has Deployed, Recombined, or Recovered a SW this RPh, its sole RPh action (A1.31, A4.44; ruling R13.4)");
        }

        if (state.Phase == "aph" && (giver.Side != state.PhasingSide || state.Advances.Any(item => item.Unit == giver.Id || item.Unit == taker.Id)))
        {
            return Refused(scope, label, expected, "play.transfer-phase: in the APh the ATTACKER's units transfer a SW before either advances (A4.431)");
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        var events = new List<GameEvent>();
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "equipment-transferred",
            new EquipmentTransferred(weapon.Id, new Holding(taker.Id, HoldingRole.Possessed), null), package, null));
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [$"play.transfer: {giver.Id} passes {weapon.Id} to {taker.Id} (A4.431)"]);
    }

    /// <summary>
    /// A SW dropped in its Location (A4.43; ruling R13.5): by an unbroken unit in its MPh before it moves, its APh before it advances, or at the start
    /// of the CCPh; by a broken unit in the RtPh before it routs.
    /// </summary>
    private GamePlan PlanDrop(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "equipmentId", out var equipmentId))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a drop names the unit and the SW");
        }

        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || state.Location(unit.Id)?.Location is not { } at
            || Held(state, unit.Id).FirstOrDefault(item => item.Id == equipmentId) is not { } weapon)
        {
            return Refused(scope, label, expected, $"play.drop-weapon: '{unitId}' does not possess '{equipmentId}' on the map");
        }

        var broken = Is(unit, Conditions.Broken);
        var when = state.Phase switch
        {
            "mph" when !broken && unit.Side == state.PhasingSide && unit is { MfSpent: 0, HalfMfSpent: false, MovementEnded: false } => null,
            "aph" when !broken && unit.Side == state.PhasingSide && state.Advances.All(item => item.Unit != unit.Id) => null,
            "ccph" when !broken && state.CloseCombats.Count == 0 => null,
            "rtph" when broken && !state.RoutedThisPhase.Contains(unit.Id) && Laden(state, unit) => null,
            _ => "play.drop-phase: an unbroken unit drops a SW in its MPh before it moves, its APh before it advances, or at the start of the CCPh; a broken unit, before it routs, the SW beyond its IPC (A4.43, A10.4; ruling R13.5)",
        };
        if (when is not null)
        {
            return Refused(scope, label, expected, when);
        }

        var events = new List<GameEvent>
        {
            Event(scope, attemptId, 1, expected, "equipment-transferred", new EquipmentTransferred(weapon.Id, null, new MapPosition(at)), ScenarioA1FirePackage.Identity.ToString(), null),
        };
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [$"play.drop: {unit.Id} drops {weapon.Id} in {at} (A4.43)"]);
    }

    /// <summary>
    /// Recovery (A4.44; ruling R13.5): an unpinned Good Order unit Recovers an unpossessed SW in its Location, friendly or enemy, on a Final dr below 6
    /// (+1 CX), as its sole RPh action or in its MPh for one extra MF before it moves, with no armed Known enemy unit in the Location; a SMC Recovers a
    /// SW its friendly broken unit there possesses the same way, with no MF.
    /// </summary>
    private GamePlan PlanRecover(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "equipmentId", out var equipmentId))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Recovery names the unit and the SW");
        }

        if (state.Unit(unitId) is not { } unit || !GoodOrder(unit) || Is(unit, Conditions.Pinned) || LiveFire.IsVehicle(unit) || state.Location(unit.Id)?.Location is not { } at)
        {
            return Refused(scope, label, expected, $"play.recover-unit: '{unitId}' is not unpinned Good Order Infantry on the map (A4.44)");
        }

        string? phaseBar = state.Phase switch
        {
            "rph" => RallyPhaseActionBar(state, unit.Id) is { } bar ? $"play.rph-action: {bar} (A4.44)" : null,
            "mph" => unit.Side != state.PhasingSide || unit.HalfMfSpent || unit.MovementEnded
                || unit.MfSpent != state.RecoveryAttempts.Count(key => key.StartsWith(unit.Id + "|", StringComparison.Ordinal))
                ? "play.recover-phase: in the MPh a unit Recovers a SW before it moves (A4.44; ruling R13.5)" : null,
            _ => "play.recover-phase: a SW is Recovered in the RPh or the MPh (A4.44)",
        };
        if (phaseBar is not null)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        if (state.Find(equipmentId) is not EquipmentInstance { Status: InstanceStatus.Active } weapon || state.Location(weapon.Id)?.Location != at)
        {
            return Refused(scope, label, expected, $"play.recover-weapon: '{equipmentId}' is not a SW in {unit.Id}'s Location");
        }

        var fromBroken = weapon.Holding is { Role: HoldingRole.Possessed } holding && state.Unit(holding.Holder) is { } holder && holder.Side == unit.Side
            && Is(holder, Conditions.Broken) && vocabulary.IsA(unit.Kind, "asl:smc");
        if (weapon.Holding is not null && !fromBroken)
        {
            return Refused(scope, label, expected, $"play.recover-weapon: {weapon.Id} is possessed; only a SMC Recovers a SW from its friendly broken unit (A4.44)");
        }

        if (KnownEnemies(state, unit.Side).Any(item => item.At == at && Armed(item.Unit)))
        {
            return Refused(scope, label, expected, "play.recover-weapon: no SW is Recovered in a Location holding an armed Known enemy unit (A4.44)");
        }

        if (state.RecoveryAttempts.Contains(unit.Id + "|" + weapon.Id))
        {
            return Refused(scope, label, expected, $"play.recover-weapon: {unit.Id} has tried to Recover {weapon.Id} this phase (A4.44)");
        }

        var drm = Is(unit, Conditions.Cx) ? 1 : 0;
        var package = ScenarioA1FirePackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            var recovered = drawn.Values[0] + drm < 6;
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "recovery", 1, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null),
                Event(scope, attemptId, 2, expected, "recovery-attempted", new RecoveryAttempted(unit.Id, weapon.Id, rollId, drm, recovered), package, null),
            };
            if (recovered)
            {
                events.Add(Event(scope, attemptId, 3, expected, "equipment-transferred",
                    new EquipmentTransferred(weapon.Id, new Holding(unit.Id, HoldingRole.Possessed), null), package, null, [EventId(attemptId, 2)]));
            }

            return events;
        }

        var cost = state.Phase == "mph" && !fromBroken ? " for one MF" : "";
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.recover: {unit.Id} tries to Recover {weapon.Id}{cost}: a Final dr below 6 (DRM {drm:+0;-0;0}) Recovers it (A4.44)"])
        {
            Roll = new PlannedRoll("recovery", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>The German MMG, the one catalog weapon A9.8 lets its possessor dismantle and assemble (ruling R13.6).</summary>
    private static bool Dismantlable(EquipmentInstance weapon) =>
        weapon.Definition is { } reference && FireReference.Value.Definitions.GetValueOrDefault(reference.Definition) is { Nationality: "german" } definition
            && definition.Id.Contains("mmg", StringComparison.Ordinal);

    /// <summary>
    /// Dismantling or assembling the German MMG (A9.8; ruling R13.6): its possessor does it in a PFPh or DFPh of its side in which the MG has not fired;
    /// it counts as the MG's use, so the MG is marked as having fired.
    /// </summary>
    private GamePlan PlanDismantle(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "equipmentId", out var equipmentId))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: dismantling names the possessing unit and the SW");
        }

        if (state.Unit(unitId) is not { } unit || !GoodOrder(unit) || Held(state, unit.Id).FirstOrDefault(item => item.Id == equipmentId) is not { } weapon)
        {
            return Refused(scope, label, expected, $"play.dismantle-weapon: '{unitId}' is not a Good Order unit possessing '{equipmentId}' (A9.8)");
        }

        if (!Dismantlable(weapon))
        {
            return Refused(scope, label, expected, $"play.dismantle-weapon: {weapon.Id} is not a weapon the game dismantles; only the German MMG is (A9.8; ruling R13.6)");
        }

        var marker = state.Phase switch
        {
            "pfph" when unit.Side == state.PhasingSide => Conditions.PrepFire,
            "dfph" when unit.Side != state.PhasingSide => Conditions.FinalFire,
            _ => null,
        };
        if (marker is null)
        {
            return Refused(scope, label, expected, "play.dismantle-phase: a MG is dismantled or assembled in its side's PFPh or DFPh (A9.8)");
        }

        if (Is(weapon, marker) || state.SupportWeaponUses.Any(item => item.Weapon == weapon.Id))
        {
            return Refused(scope, label, expected, $"play.dismantle-weapon: {weapon.Id} has fired this phase (A9.8)");
        }

        var dismantled = !Is(weapon, Conditions.Dismantled);
        var events = new List<GameEvent>
        {
            Event(scope, attemptId, 1, expected, "conditions-changed", new ConditionsChanged(weapon.Id, new Dictionary<string, ConditionState>
            {
                [Conditions.Dismantled] = dismantled ? ConditionState.True : ConditionState.False,
                [marker] = ConditionState.True,
            }), ScenarioA1FirePackage.Identity.ToString(), null),
        };
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
            [$"play.dismantle: {unit.Id} {(dismantled ? "dismantles" : "assembles")} {weapon.Id}, which counts as its use this phase (A9.8)"]);
    }
}
