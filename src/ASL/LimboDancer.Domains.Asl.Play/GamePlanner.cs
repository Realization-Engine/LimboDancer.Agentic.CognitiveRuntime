using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play;

public enum GamePlanStatus
{
    /// <summary>The events are ready to commit at the expected revision.</summary>
    Ready,

    /// <summary>The same attempt already committed; there is nothing more to do.</summary>
    Replay,

    /// <summary>The game is not at the expected revision.</summary>
    Stale,

    /// <summary>The action is not allowed in the current state; the reasons say why.</summary>
    Refused,
}

/// <summary>
/// The reviewed first case as the entry action read it: each of the nine facts (true, false, or null when the state
/// cannot establish it), and the reviewed resolver's conclusion from them.
/// </summary>
public sealed record EntryReview(IReadOnlyDictionary<string, bool?> Facts, DomainConclusion Conclusion);

/// <summary>Which reviewed case an entry was routed to (Occupied and Concealed Entry Design, section 6).</summary>
public enum EntryRoute
{
    /// <summary>The target is empty: the reviewed first case.</summary>
    Empty,

    /// <summary>Every occupant is a known, unconcealed enemy MMC: the Occupied package's A4.14 case.</summary>
    KnownEnemy,

    /// <summary>One concealed or hidden enemy MMC: the PostReveal forced back.</summary>
    Concealed,

    /// <summary>One concealed SMC: revealed, and the attempt waits for the attacker's OVR declaration.</summary>
    LoneSmc,

    /// <summary>Several concealed or hidden enemy units: a Random Selection roll decides the reveal.</summary>
    RandomSelection,

    /// <summary>No reviewed case covers the target.</summary>
    Outside,
}

/// <summary>
/// What the moving side may be told about an entry (Occupied and Concealed Entry Design, section 7). An entry into a
/// location the side sees as empty, or as a sealed presence, is <see cref="Withheld"/>: its outcome is disclosed only by
/// committing it. <see cref="MoverReasons"/> are the refusals that depend only on the mover and the terrain, which the
/// side may always see.
/// </summary>
public sealed record EntryDisclosure(string Mover, EntryRoute Route, bool Withheld, IReadOnlyList<string> MoverReasons)
{
    public const string ResolvedOnConfirmation = "play.resolved-on-confirmation: the entry is declared, and its outcome is resolved when it is confirmed";

    public const string CannotResolve = "play.adjudicator-cannot-resolve: no reviewed case covers this entry, and nothing was committed";

    /// <summary>The reasons the mover's side may see for a plan, before or after confirmation.</summary>
    public IReadOnlyList<string> ReasonsForMover(GamePlan plan, bool confirmed)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (!Withheld)
        {
            return plan.Reasons;
        }

        if (MoverReasons.Count > 0)
        {
            return MoverReasons;
        }

        return !confirmed ? [ResolvedOnConfirmation] : plan.CanCommit ? ["play.committed"] : [CannotResolve];
    }
}

/// <summary>What an action would commit, or why it would not.</summary>
public sealed record GamePlan(
    GamePlanStatus Status,
    GameScope Scope,
    string Label,
    long ExpectedRevision,
    IReadOnlyList<GameEvent> Events,
    IReadOnlyList<string> Reasons,
    EntryReview? Entry = null)
{
    public bool CanCommit => Status is GamePlanStatus.Ready or GamePlanStatus.Replay;

    /// <summary>For an entry: its route and what the moving side may be told.</summary>
    public EntryDisclosure? Disclosure
    {
        get; init;
    }

    /// <summary>For a fire attack: its facts and what the firing side may be told.</summary>
    public FireProposal? Fire
    {
        get; init;
    }

    /// <summary>
    /// A roll the action needs. A plan with a roll is Ready with no events: the store draws the roll inside its commit
    /// and builds the events from it (Random Selection and Declined OVR Design, section 3).
    /// </summary>
    public PlannedRoll? Roll
    {
        get; init;
    }

    /// <summary>The id the plan's first event has, known before any roll: a committed attempt is found by it.</summary>
    public string? FirstEventId
    {
        get; init;
    }
}

/// <summary>
/// Plans the governed game actions (Governed Writes Design, sections 6 to 8) from current, server-owned state: it
/// re-reads the game, the boards, and the catalog every time it is asked, so the gate and the executor each plan
/// afresh. It never writes.
/// </summary>
public sealed partial class GamePlanner(IGameStore store, IBoardCatalog boards, UnitVocabulary vocabulary, IReadOnlyList<UnitCatalog> catalogs,
    TimeProvider? time = null, IFireLosReader? fireLos = null, ScenarioCardLibrary? cardLibrary = null)
{
    /// <summary>The scenario cards a game may start from (ruling R22.2): the built-in cards, and the user's when the planner is given them.</summary>
    public ScenarioCardLibrary CardLibrary { get; } = cardLibrary ?? ScenarioCardLibrary.Embedded;

    /// <summary>
    /// The terrain of an ordinary wooden or stone building (B23; the reviewed B. Terrain Chart supplement): the reviewed
    /// case covers its ground level whatever the building's height, so the multi-level names VASL boards use count too.
    /// </summary>
    /// <summary>
    /// The facts of the first case that depend only on the mover and the terrain; the others read the target's occupants.
    /// A known-enemy or concealed entry needs every one of them true.
    /// </summary>
    private static readonly string[] MoverFacts =
    [
        "isKnownGoodOrderInfantrySquad", "isAttackerMovementPhase", "canMoveThisPhase", "isAdjacentGroundLevelOrdinaryBuilding",
        "hasNoRoadBypassElevationOrAdditionalTerrain", "hasEnoughMovementFactors", "hasNoSpecialRuleOrOtherModifier",
    ];

    private static readonly IReadOnlySet<string> OrdinaryBuildings = ScenarioA1Definitions.OrdinaryBuildings;

    // Ruling R10.12 (table player, pass 10): move options the reviewed entry cases do not take.
    private static readonly string[] MoveOptions = ["assault", "doubleTime", "minimumMove"];

    private readonly TimeProvider clock = time ?? TimeProvider.System;

    // Pass 31d (design D1): the projections last asked for, the most recent first. A request replayed the whole game at each of its guards, some
    // twenty times; a list that begins with a kept projection's events, object for object, now has only its later events applied.
    private const int KeptProjections = 8;
    private readonly List<ReplayedGame> projections = [];

    /// <summary>
    /// Replays a live game's events against the exact boards in play; refuses when a board cannot be read. The events of a list already replayed
    /// are not replayed again (pass 31d): a list that begins with them, as the same objects, has the rest applied to what was kept. Every event is
    /// still applied and verified once, by the same code.
    /// </summary>
    public GameHistory Replay(IReadOnlyList<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        ReplayedGame? kept = null;
        lock (projections)
        {
            foreach (var candidate in projections)
            {
                if ((kept is null || candidate.History.Events.Count > kept.History.Events.Count) && candidate.Begins(events))
                {
                    kept = candidate;
                }
            }
        }

        if (kept is not null && kept.History.Events.Count == events.Count)
        {
            Keep(kept);
            return kept.History;
        }

        if (kept is { Complete: true })
        {
            Keep(kept);
            var continued = kept.Continue(events);
            Keep(continued);
            return continued.History;
        }

        // A board that cannot be read is asked for again at the next request, so a replay without its chains is not kept.
        var chains = Chains(events);
        var projection = GameProjector.Begin(events, vocabulary, catalogs, chains, LiveGames.Sources, FireRecordVerifier.Shared, RallyRecordVerifier.Shared,
            CloseCombatRecordVerifier.Shared, OrdnanceRecordVerifier.Shared);
        if (chains is not null && events.Count > 0)
        {
            Keep(projection);
        }

        return projection.History;
    }

    private void Keep(ReplayedGame projection)
    {
        lock (projections)
        {
            projections.Remove(projection);
            projections.Insert(0, projection);
            if (projections.Count > KeptProjections)
            {
                projections.RemoveRange(KeptProjections, projections.Count - KeptProjections);
            }
        }
    }

    public async Task<GamePlan> PlanAsync(ActionDescriptor action, JsonElement arguments, Guid tenant, string? actor = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!Common(arguments, out var gameId, out var attemptId, out var expected))
        {
            return Refused(new GameScope(tenant, "unknown"), "", 0, "play.invalid-arguments");
        }

        var scope = new GameScope(tenant, gameId);
        var record = store.Read(scope);
        var existing = record?.Events ?? [];
        var label = record?.Label ?? gameId;
        if (existing.FirstOrDefault(item => item.EventId == EventId(attemptId, 1)) is { } committed)
        {
            // DICE-10: the same attempt with different inputs is not a replay.
            return ReusedWithOtherInputs(action, arguments, committed)
                ? Refused(scope, label, expected, $"play.attempt-reused: the attempt '{attemptId}' was committed with different inputs")
                : new GamePlan(GamePlanStatus.Replay, scope, label, expected, [], ["play.replay"]);
        }

        if (existing.Count != expected)
        {
            return new GamePlan(GamePlanStatus.Stale, scope, label, expected, [], [$"play.stale: the game is at revision {existing.Count}"]);
        }

        // Pass 20 (ruling R20.1): nothing happens in a game that has ended.
        if (existing.Count > 0 && existing[^1].Payload is GameEnded over)
        {
            return Refused(scope, label, expected, $"play.game-over: the game ended after Game Turn {over.Turn} (A3.9; ruling R20.1)");
        }

        // Pass 19 (ruling R19.2; referee, pass 19): in a game from a card nothing but setup happens until every group has set up.
        if (action.Id.Value != "asl.game.setup" && existing.Count > 0 && existing.All(item => GameState.IsSetupEvent(item.Payload))
            && Replay(existing).Current is { Scenario: not null } setupState && CardSetupIncomplete(setupState, existing) is { } unfinished)
        {
            return Refused(scope, label, expected, unfinished);
        }

        // Pass 31 (ruling R31.6): a side's view proposes only what its side may do; setup is guarded by its own checks of each group's side.
        if (action.Id.Value != "asl.game.setup" && ProposedBy(arguments) is { } proposer && existing.Count > 0 && Replay(existing).Current is { } proposerState
            && ProposerBar(proposerState, action.Id.Value, arguments, proposer, existing) is { } notYours)
        {
            return Refused(scope, label, expected, notYours);
        }

        // Ruling R26.2: a Passenger acts only with its vehicle until it unloads.
        if (action.Id.Value is not ("asl.game.setup" or "asl.game.move-vehicle" or "asl.game.hook-gun" or "asl.game.advance-phase" or "asl.game.choose" or "asl.game.pass-fire"
            or "asl.game.end-move" or "asl.game.button-up") && existing.Count > 0 && Replay(existing).Current is { } boardState && AboardBar(boardState, arguments) is { } aboard)
        {
            return Refused(scope, label, expected, aboard);
        }

        // A4.152 (ruling R27.3): once the DEFENDER's window on a berserk OVR's entry closes, its CC comes first.
        if (action.Id.Value is not ("asl.game.close-combat" or "asl.game.choose" or "asl.game.take-prisoner") && existing.Count > 0 && Replay(existing).Current is { } overrunState
            && BerserkOverrunPending(overrunState) is { } overrunAt)
        {
            return Refused(scope, label, expected, $"play.cc-overrun-first: the berserk Infantry OVR in {overrunAt} has its CC at once, before anything else happens (A4.152, A15.432)");
        }

        var plan = action.Id.Value switch
        {
            "asl.game.setup" => PlanSetup(scope, arguments, existing, attemptId, expected, ref label, actor ?? "unknown"),
            "asl.game.advance-phase" => PlanAdvance(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.enter-empty-building" => await PlanEntryAsync(scope, arguments, existing, attemptId, expected, label, cancellationToken),
            "asl.game.declare-overrun" => await PlanDeclareAsync(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown", cancellationToken),
            "asl.game.enter-building" => await PlanEnterBuildingAsync(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown", cancellationToken),
            "asl.game.fire" => PlanFire(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.rally" => PlanRally(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.repair" => PlanRepair(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.recover-shock" => PlanRecoverShock(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.turn-gun" => PlanTurnGun(scope, arguments, existing, attemptId, expected, label),
            "asl.game.hook-gun" => PlanHookGun(scope, arguments, existing, attemptId, expected, label),
            "asl.game.move" => await PlanMoveOrEntryAsync(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown", cancellationToken),
            "asl.game.pass-fire" => PlanPassFire(scope, existing, attemptId, expected, label),
            "asl.game.end-move" => PlanEndMove(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.advance" => PlanAdvanceUnits(scope, arguments, existing, attemptId, expected, label),
            "asl.game.ambush" => PlanAmbush(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.close-combat" => PlanCloseCombat(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.fire-ordnance" => PlanFireOrdnance(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.take-prisoner" => PlanTakePrisoner(scope, arguments, existing, attemptId, expected, label),
            "asl.game.move-vehicle" => PlanMoveVehicle(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.overrun" => PlanOverrun(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.vehicle-close-combat" => PlanVehicleCloseCombat(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.button-up" => PlanButtonUp(scope, arguments, existing, attemptId, expected, label),
            "asl.game.choose" => PlanChoose(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.massacre" => PlanMassacre(scope, arguments, existing, attemptId, expected, label),
            "asl.game.opportunity-fire" => PlanOpportunityFire(scope, arguments, existing, attemptId, expected, label),
            "asl.game.rout" => PlanRout(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.deploy" => PlanDeploy(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.recombine" => PlanRecombine(scope, arguments, existing, attemptId, expected, label),
            "asl.game.transfer" => PlanTransfer(scope, arguments, existing, attemptId, expected, label),
            "asl.game.drop" => PlanDrop(scope, arguments, existing, attemptId, expected, label),
            "asl.game.recover" => PlanRecover(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.dismantle" => PlanDismantle(scope, arguments, existing, attemptId, expected, label),
            "asl.game.ambush-withdraw" => PlanAmbushWithdrawal(scope, arguments, existing, attemptId, expected, label),
            "asl.game.guard-prisoners" => PlanGuardPrisoners(scope, arguments, existing, attemptId, expected, label),
            "asl.game.throw-dc" => PlanThrowDc(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.fire-starshell" => PlanStarshell(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.detonate-dc" => PlanDetonateDc(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.place-hidden" => PlanPlaceHidden(scope, arguments, existing, attemptId, expected, label),
            "asl.game.mop-up" => PlanMopUp(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            _ => Refused(scope, label, expected, "play.unknown-action"),
        };

        // Ruling R5.8: a pending choice is answered before anything else happens in the game.
        if (plan.Status == GamePlanStatus.Ready && action.Id.Value is not ("asl.game.choose" or "asl.game.setup")
            && Replay(existing).Current is { Choice: { } choice })
        {
            return Refused(scope, label, expected, $"play.choice-pending: the {choice.Side} side answers first: {DescribeChoice(choice)}");
        }

        // A15.5: a surrender waits for its captor before anything else happens in the game.
        if (plan.Status == GamePlanStatus.Ready && action.Id.Value is not ("asl.game.take-prisoner" or "asl.game.setup")
            && Replay(existing).Current is { PendingSurrenders: [{ } pending, ..] })
        {
            return Refused(scope, label, expected, $"play.surrender-pending: {pending.Unit} has surrendered; its captor's side chooses the Guard or rejects it first (A15.5, A20.3)");
        }

        // C6.5, C6.51 (ruling R5.13): an Acquisition follows the units it is on.
        plan = WithAcquisitions(plan, scope, existing, attemptId, expected);

        // A10.62 (ruling R13.1): a broken unit comes under DM when a Known armed enemy unit is ADJACENT to it.
        if (action.Id.Value != "asl.game.setup")
        {
            plan = WithAdjacentDm(plan, scope, existing, attemptId, expected);
        }

        // A20.551 (ruling R31.8): a SMC the action leaves free and Unarmed is Armed again.
        if (action.Id.Value != "asl.game.setup")
        {
            plan = WithArmedSmc(plan, scope, existing, attemptId, expected);
        }

        // Pass 21 (ruling R21.4): an immediate Victory Condition met by the action ends the game after it.
        if (plan.Status == GamePlanStatus.Ready && action.Id.Value != "asl.game.setup")
        {
            plan = plan.Roll is { } rolled
                ? plan with
                {
                    Roll = rolled with
                    {
                        Build = draw => WithImmediateVictory(scope, attemptId, expected, existing, rolled.Build(draw))
                    }
                }
                : plan with
                {
                    Events = WithImmediateVictory(scope, attemptId, expected, existing, plan.Events)
                };
            if (plan.Roll is null && plan.Events.Count > 0 && plan.Events[^1].Payload is GameEnded { Reason: "victory", Result: { } won } && won.Reason.Length > 0)
            {
                plan = plan with
                {
                    Reasons = [.. plan.Reasons, $"play.result: {(won.Winner is { } winner ? $"{winner} wins at once" : "a draw")}: {won.Reason}"]
                };
            }
        }

        // A plan with a roll has no events until the store draws it; its outcomes are checked when they are built.
        if (plan.Status != GamePlanStatus.Ready || plan.Roll is not null)
        {
            return plan;
        }

        // The whole log must still replay, against the exact boards in play, with the new events.
        var history = Replay([.. existing, .. plan.Events]);
        return history.HasErrors
            ? plan with
            {
                Status = GamePlanStatus.Refused,
                Events = [],
                Reasons = [.. history.Diagnostics.Where(item => item.Severity == Units.UnitDiagnosticSeverity.Error).Select(item => item.ToString())]
            }
            : plan;
    }

    private GamePlan PlanSetup(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, ref string label,
        string actor)
    {
        if (existing.Any(item => !GameState.IsSetupEvent(item.Payload)))
        {
            return Refused(scope, label, expected, "play.setup-closed: play has started, so no more units can be set up");
        }

        // Ruling R23.6 (A12.12): the non-OB "?" each side places once both have set up.
        if (arguments.TryGetProperty("conceal", out var conceal))
        {
            return PlanNonObConcealment(scope, conceal, existing, attemptId, expected, label);
        }

        var events = new JsonArray();
        var sides = new Dictionary<string, string>(StringComparer.Ordinal);
        var boreSights = new List<(string Gun, BoardLocation At)>();
        StartRolls? startRolls = null;
        if (existing.Count == 0)
        {
            if (!arguments.TryGetProperty("start", out var start) || start.ValueKind != JsonValueKind.Object)
            {
                return Refused(scope, label, expected, "play.start-required: a new game needs its sides, boards, and catalog");
            }

            // Backlog pass 18 (rulings R18.1, R18.2): a game from a scenario card starts as the card says.
            if (CardStart(start, out var fromCard, out var cardReason, out startRolls) is false)
            {
                return Refused(scope, label, expected, cardReason!);
            }

            using var cardStart = fromCard is null ? null : JsonDocument.Parse(fromCard.ToJsonString());
            if (cardStart is not null)
            {
                start = cardStart.RootElement.Clone();
            }

            if (Start(start, out var payload, out var startLabel, out var reason) is false)
            {
                return Refused(scope, label, expected, reason!);
            }

            label = startLabel;
            events.Add(EventNode(1, "game-started", payload!, visibility: null));
        }

        if (!arguments.TryGetProperty("placements", out var placements) || placements.ValueKind != JsonValueKind.Array)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: placements must be a list");
        }

        foreach (var placement in placements.EnumerateArray())
        {
            if (placement.ValueKind != JsonValueKind.Object)
            {
                return Refused(scope, label, expected, "play.invalid-arguments: each placement is an object");
            }

            var node = JsonNode.Parse(placement.GetRawText())!.AsObject();

            // C6.42 (ruling R8.8): a Gun's Bore Sighted Location is recorded after the setup's placements.
            if (node["boreSighted"] is JsonValue boreNode && boreNode.TryGetValue<string>(out var boreText))
            {
                if (!BoardLocation.TryParse(boreText, out var boreAt) || node["id"]?.GetValue<string>() is not { } boreGun)
                {
                    return Refused(scope, label, expected, "play.bore-sight: a Bore Sighted Location is a Location of the map");
                }

                boreSights.Add((boreGun, boreAt));
                node.Remove("boreSighted");
            }

            var hidden = IsTrue(placement, Conditions.Hidden) || IsTrue(placement, Conditions.Concealed);
            var side = placement.TryGetProperty("side", out var sideElement) && sideElement.ValueKind == JsonValueKind.String ? sideElement.GetString() : null;
            events.Add(EventNode(events.Count + 1, "instance-created", new JsonObject { ["instance"] = node }, hidden && side is not null ? [side] : null));
        }

        if (events.Count == 0)
        {
            return Refused(scope, label, expected, "play.nothing-to-set-up");
        }

        var parsed = Parse(scope, events, attemptId, expected);

        // Table player, pass 20 (pass 25): a unit naming another side's OB group is refused here, with a plain message, not by the projector.
        if (parsed.Events is { Count: > 0 } created && (existing.Count > 0 ? Replay(existing).Current : Replay([created[0]]).Current) is { } groupsState)
        {
            foreach (var instance in created.Select(item => item.Payload).OfType<InstanceCreated>().Select(item => item.Instance))
            {
                if (instance.Group is { } group && instance.Side is { } owner && groupsState.Side(owner) is { } ownSide && ownSide.Groups.All(item => item.Id != group))
                {
                    var holder = groupsState.Sides.FirstOrDefault(item => item.Groups.Any(other => other.Id == group))?.Id;
                    return Refused(scope, label, expected, $"play.setup-group: {instance.Id} is {owner}'s, and the OB group '{group}' is "
                        + (holder is null ? "not a group of this game" : $"{holder}'s") + "; a unit sets up in an OB group of its own side (ruling R18.3)");
                }
            }
        }

        if (parsed.Events is { } placed && Replay([.. existing, .. placed]).Current is { } after && VehicleSetupBar(after) is { } vehicleBar)
        {
            return Refused(scope, label, expected, vehicleBar);
        }

        // Pass 19 (rulings R19.1 to R19.6): a game from a card sets up its OB, in its areas and order.
        if (parsed.Events is { } carded && Replay([.. existing, .. carded]).Current is { } cardState && cardState.Scenario is { } playing)
        {
            if (!CardLibrary.Matches(playing.Id, playing.Sha256))
            {
                return Refused(scope, label, expected, $"play.scenario: the card '{playing.Id}' {Gone(playing.Id)}, so its OB cannot be checked (ruling R19.1)");
            }

            var placedNow = carded.Select(item => item.Payload).OfType<InstanceCreated>().Select(item => item.Instance.Id).ToHashSet(StringComparer.Ordinal);
            if (CardSetup(cardState, placedNow) is { Reasons.Count: > 0 } setupReport)
            {
                return Refused(scope, label, expected, [.. setupReport.Reasons]);
            }
        }

        // Ruling R18.3: where a side's OB groups differ in ELR, each of its units names its group, since no side ELR stands in for it.
        if (parsed.Events is { } grouped && Replay([.. existing, .. grouped]).Current is { } groupedState
            && groupedState.Units.FirstOrDefault(unit => unit.Group is null && unit.Kind is not (UnitKinds.Dummy or "asl:sniper" or "asl:hero" or "asl:crew")
                && unit.Definition?.Definition.Contains("commissar", StringComparison.Ordinal) != true
                && groupedState.Side(unit.Side) is { Groups.Count: > 0, Elr: null }) is { } ungrouped)
        {
            return Refused(scope, label, expected,
                $"play.group: {ungrouped.Id} names no OB group, and the groups of {ungrouped.Side} differ in ELR (A19.1; ruling R18.3)");
        }

        IReadOnlyList<GameEvent>? withSights = null;
        if (boreSights.Count > 0 && parsed.Events is { } setUp && Replay([.. existing, .. setUp]).Current is { } sightState)
        {
            // C6.41, C6.42: the Scenario Defender's manned Gun, a Location outside its hex, in its LOS, within 16 hexes.
            var sightEvents = new List<GameEvent>();
            foreach (var (boreGun, boreAt) in boreSights)
            {
                if (sightState.Find(boreGun) is not EquipmentInstance { Holding: { Role: HoldingRole.Manned } manning, Position: MapPosition gunAt }
                    || sightState.Unit(manning.Holder)?.Side != sightState.ScenarioDefender || boreAt == gunAt.Location
                    || Los(sightState, gunAt.Location, boreAt) is not { Status: LosStatus.Clear, Range: <= 16 })
                {
                    return Refused(scope, label, expected,
                        $"play.bore-sight: {boreGun} may Bore Sight only as the Scenario Defender's manned Gun, a Location outside its hex, in its LOS, within 16 hexes (C6.41, C6.42)");
                }

                sightEvents.Add(Event(scope, attemptId, setUp.Count + sightEvents.Count + 1, expected, "bore-sighted",
                    new BoreSighted(boreGun, boreAt, manning.Holder, gunAt.Location), ScenarioA1OrdnancePackage.Identity.ToString(), [sightState.ScenarioDefender!]));
            }

            withSights = [.. setUp, .. sightEvents];
        }

        if ((withSights ?? parsed.Events) is not { } list)
        {
            return Refused(scope, label, expected, [.. parsed.Reasons]);
        }

        // Pass 20 (rulings R20.2, R20.3): the start's drs, for the first move and for the Balance, are drawn as the game is written.
        return startRolls is { } rolls && (rolls.FirstMove is not null || rolls.Balance is not null)
            ? new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [$"play.setup: {list.Count} event(s)",
                .. rolls.FirstMove is not null ? ["play.first-move: a dr for each side decides which moves first (A3.9; ruling R20.2)"] : Array.Empty<string>(),
                .. rolls.Balance is not null ? ["play.balance: both players wish to play the same side, so a dr decides it; the other side takes its Balance (A26.4; ruling R20.3)"] : Array.Empty<string>()])
            {
                Roll = new PlannedRoll("start", draw => StartRolled(scope, attemptId, expected, actor, list, rolls, draw)),
                FirstEventId = EventId(attemptId, 1),
            }
            : new GamePlan(GamePlanStatus.Ready, scope, label, expected, list, [$"play.setup: {list.Count} event(s)"]);
    }

    private GamePlan PlanAdvance(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (state.Sides.Count != 2)
        {
            return Refused(scope, label, expected, "play.two-sides: the sequence of play alternates two sides");
        }

        // Pass 19 (ruling R19.2): play starts when every group of a card that sets up on board has finished.
        if (CardSetupIncomplete(state, existing) is { } incomplete)
        {
            return Refused(scope, label, expected, incomplete);
        }

        if (state.OpenAttempts.Count > 0)
        {
            return Refused(scope, label, expected, $"play.declaration-pending: the entry attempt '{state.OpenAttempts[0].EventId}' by "
                + $"{state.OpenAttempts[0].Unit} awaits the attacker's OVR declaration (A12.15, p. 78), so the phase cannot advance");
        }

        // C7.42 (ruling R7.8): the RPh does not end while a Shocked AFV or an Unconfirmed Kill owes its dr.
        if (ShockRollsOwed(state).FirstOrDefault() is { } shocked)
        {
            return Refused(scope, label, expected, $"play.shock-recovery-pending: {shocked.Id} makes its Shock or Unconfirmed Kill dr before the RPh ends (C7.42)");
        }

        // A25.222 (backlog pass 15, ruling R15.6): a Commissar must attempt to rally every broken unit of his Location; the RPh does not end while one has made
        // no attempt that the Rally package would decide.
        if (state.Phase == "rph")
        {
            foreach (var broken in state.Units.Where(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Broken) && !Is(unit, Conditions.Captured)
                && !state.RallyAttemptsThisPlayerTurn.Contains(unit.Id) && !state.RallyPhaseActions.Contains(unit.Id)).OrderBy(unit => unit.Id, StringComparer.Ordinal))
            {
                if (LiveRally.Commissar(state, broken) is { } commissar && !state.RallyPhaseActions.Contains(commissar.Id)
                    && PlanRally(scope, JsonSerializer.SerializeToElement(new
                    {
                        unitId = broken.Id,
                        leader = commissar.Id
                    }), existing, attemptId + "-commissar", expected, label,
                        actor).Status == GamePlanStatus.Ready)
                {
                    return Refused(scope, label, expected, $"play.commissar-rally: {commissar.Id} must attempt to rally {broken.Id} before the RPh ends (A25.222)");
                }
            }
        }

        // A3.1 to A3.8 (p. 47): the eight phases in order; after the CCPh the other side's Player Turn begins, and a new
        // Game Turn begins when the side that moved first is phasing again.
        var index = Phases.All.ToList().IndexOf(state.Phase);
        var (turn, phase, phasing) = index < Phases.All.Count - 1
            ? (state.Turn, Phases.All[index + 1], state.PhasingSide)
            : (state.Turn, Phases.All[0], state.Sides.First(side => side.Id != state.PhasingSide).Id);
        if (index == Phases.All.Count - 1 && phasing == state.FirstSide)
        {
            turn++;
        }

        // A23.4 (backlog pass 15, ruling R15.2): a DC operably Placed detonates before the AFPh ends, unless the Fire package refuses its attack; it then stays
        // in its Location.
        if (state.Phase == "afph" && state.PlacedCharges.Where(item => item.Operable).FirstOrDefault(item => PlanDetonateDc(scope,
            JsonSerializer.SerializeToElement(new
            {
                equipmentId = item.Charge
            }), existing, attemptId + "-dc", expected, label, actor).Status == GamePlanStatus.Ready) is { } unexploded)
        {
            return Refused(scope, label, expected, $"play.dc-detonate-pending: {unexploded.Charge} was Placed in {unexploded.Target} and detonates before the AFPh ends (A23.4)");
        }

        // A2.5 (rulings R20.5, R25.1): the APh does not end while a unit whose entry turn has come waits off board and may still enter by advance.
        if (state.Phase == "aph" && EntryDue(state) is { } due)
        {
            return Refused(scope, label, expected, due);
        }

        // A2.5 (ruling R26.1): vehicles cannot advance, so the MPh does not end while a vehicle whose entry turn has come waits off board and may enter.
        if (state.Phase == "mph" && VehicleEntryDue(state) is { } vehicleDue)
        {
            return Refused(scope, label, expected, vehicleDue);
        }

        // A3.9 (ruling R20.1): the game from a card ends after its last Game Turn, or after the first side's Player Turn of a half turn.
        var ending = index == Phases.All.Count - 1 && CardOf(state) is { } endCard && ScenarioCards.EndsAfter(endCard.Turns, state.Turn, state.PhasingSide == state.FirstSide);

        // A15.43: the MPh does not end while a berserk unit must still charge.
        var reasons = new List<string>();
        var events = new List<GameEvent>();

        // A10.62 (ruling R13.1): as the RPh ends, a broken unit under DM outside woods and buildings may keep it.
        string[] retained = [.. Strings(arguments, "retainDm")];
        if (retained.Length > 0 && state.Phase != "rph")
        {
            return Refused(scope, label, expected, "play.retain-dm: DM is retained as the RPh ends (A10.62)");
        }

        if (retained.Select(id => RetainDmBar(state, id)).FirstOrDefault(bar => bar is not null) is { } retainBar)
        {
            return Refused(scope, label, expected, retainBar);
        }

        // A10.5, A20.21 (ruling R13.3): as the RtPh ends, a broken unit that failed to rout is eliminated, or surrenders to its captors first.
        if (state.Phase == "rtph")
        {
            var failed = FailureToRout(state, existing);
            var surrendering = failed.Where(item => item.Captors is not null).ToArray();
            if (surrendering.Length > 0)
            {
                foreach (var (unit, why, captors) in surrendering)
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "surrender-pending", new SurrenderPending(unit.Id, captors!), null, null));
                    reasons.Add($"play.failure-to-rout-surrender: {unit.Id} would be eliminated for Failure to Rout ({why}), so it surrenders to {string.Join(" or ", captors!)} (A20.21); advance the phase again once its captor's side has chosen");
                }

                return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [.. reasons]);
            }

            foreach (var (unit, why, _) in failed)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(unit.Id), null, null));
                reasons.Add($"play.failure-to-rout: {unit.Id} is eliminated for Failure to Rout: {why} (A10.5)");
            }
        }
        if (state.Phase == "mph" && MustCharge(state) is [{ } charging, ..])
        {
            return Refused(scope, label, expected, $"play.berserk-charge: {charging.Id} is berserk and must charge before the MPh ends (A15.43)");
        }

        // D2.4: a vehicle under a Motion counter must expend at least one MP in its MPh.
        if (state.Phase == "mph" && state.Units.FirstOrDefault(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && LiveFire.IsVehicle(unit)
            && Is(unit, Conditions.Motion) && unit is { MfSpent: 0, HalfMfSpent: false, MovementEnded: false }) is { } idle)
        {
            return Refused(scope, label, expected, $"play.vehicle-motion: {idle.Id} is in Motion and must expend at least one MP this MPh (D2.4)");
        }

        // D5.341 (ruling R5.17): a Recalled AFV must move toward its Friendly Board Edge in its MPh, when its route is decided.
        if (state.Phase == "mph" && state.Units.FirstOrDefault(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && MustLeave(unit)
            && !unit.MovementEnded && RecallRoute(state, unit) is { Undecided: null, Moves.Count: > 0 }) is { } recalled)
        {
            return Refused(scope, label, expected, $"play.recall-move: {recalled.Id} is Recalled and must move off by its side's Friendly Board Edge this MPh (D5.341)");
        }

        // A15.431, A15.46: at the end of its MPh a berserk unit with no Known enemy unit in its LOS returns to normal.
        if (state.Phase == "mph")
        {
            foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && Is(unit, Conditions.Berserk)
                && !Is(unit, Conditions.Melee) && state.Location(unit.Id) is { } at && KnownEnemyInLos(state, unit.Side, at.Location) == false))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                    new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.Berserk] = ConditionState.False }), null, null));
                reasons.Add($"play.berserk-ends: {unit.Id} sees no Known enemy unit and returns to normal (A15.431, A15.46)");
            }
        }

        // A11.16, A19.12: a broken or Disrupted unit held in Melee is eliminated at the end of the CCPh unless it withdrew (ruling R29.11);
        // one that could withdraw attempts it in its Location's CC first.
        if (state.Phase == "ccph" && state.Units.FirstOrDefault(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id) is { } held
            && !state.CloseCombats.Any(item => item.Location == held.Location) && MustWithdraw(state, unit, held.Location)) is { } withdrawing)
        {
            return Refused(scope, label, expected, $"play.cc-withdraw-required: {withdrawing.Id} is broken in Melee and must attempt to withdraw in its Location's CC first (A11.16)");
        }

        // A15.43, A11.15: a berserk unit with a Known enemy unit in its Location, or a unit that advanced into a Melee, must attack, so a
        // Location that holds one and whose CC the package can resolve has its round before the CCPh ends.
        if (state.Phase == "ccph" && CloseCombatRequired(state) is { } required)
        {
            return Refused(scope, label, expected, required);
        }

        // A11.16: a broken Guard is not eliminated in Melee.
        if (state.Phase == "ccph")
        {
            foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Melee) && !Is(unit, Conditions.Captured) && !IsGuard(state, unit)
                && (Is(unit, Conditions.Broken) || Is(unit, Conditions.Disrupted))).OrderBy(unit => unit.Id, StringComparer.Ordinal))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(unit.Id), null, null));
                reasons.Add($"play.melee-eliminated: {unit.Id} is broken or Disrupted in Melee and cannot withdraw, so it is eliminated (A11.16, A19.12)");
            }
        }

        // Pass 31d (design D9; A11.15, read in the PDF, p. 72; ruling R31d.6): a Location that holds units of both sides, prisoners apart, in which no
        // round was fought this phase is said before the phase ends. It is a consequence and not a refusal: where the Close Combat package does
        // not decide the Location, a refusal would leave the phase with no way to end (ruling R31c.5).
        if (state.Phase == "ccph")
        {
            foreach (var unfought in state.Units.Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured) && unit.Kind != UnitKinds.Dummy && state.Location(unit.Id) is not null)
                .GroupBy(unit => state.Location(unit.Id)!.Location).Where(group => group.Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count() > 1
                    && !state.CloseCombats.Any(item => item.Location == group.Key && item.Rounds.Count > 0))
                .Select(group => group.Key).OrderBy(location => location.ToString(), StringComparer.Ordinal))
            {
                reasons.Add($"play.cc-unfought: no Close Combat was fought in {unfought} this phase; the units of both sides stay there, held in Melee unless they keep their \"?\" (A11.15)");
            }
        }

        // A12.12, A12.122 (ruling R12.5): as a Player Turn ends, the phasing side's Good Order Infantry may gain "?", some on a Final Concealment dr.
        var gains = !ending && state.Phase == "ccph" && phasing != state.PhasingSide ? ConcealmentGains(state) : [];
        // B25.65 (backlog pass 16, rulings R16.1, R16.10): the Wind Change DR at the start of a RPh.
        var wind = !ending && WindChangeDue(state, phase, turn, phasing);
        if (gains.Any(item => item.Drm is not null) || wind)
        {
            var prefix = events.ToList();
            IReadOnlyList<GameEvent> Rolled(Func<RollRequest, RollResult> draw)
            {
                var built = new List<GameEvent>(prefix);
                AddConcealmentGains(scope, attemptId, expected, actor, gains, built, draw, []);
                Finish(built, [], draw);
                return built;
            }

            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
                [$"play.advance: turn {turn}, {phase}, {phasing} phasing", .. reasons,
                    .. gains.Any(item => item.Drm is not null)
                        ? [$"play.concealment: {string.Join(", ", gains.Where(item => item.Drm is not null).Select(item => item.Unit.Id))} make a Final Concealment dr (A12.122)"]
                        : Array.Empty<string>(),
                    .. wind ? ["play.wind-change: the Wind Change DR is made as the RPh begins (B25.65)"] : Array.Empty<string>()])
            {
                Roll = new PlannedRoll(wind ? "wind-change" : "concealment", Rolled),
                FirstEventId = EventId(attemptId, 1),
            };
        }

        AddConcealmentGains(scope, attemptId, expected, actor, gains, events, null, reasons);
        Finish(events, reasons);
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [ending
            ? $"play.game-ended: Game Turn {state.Turn} was the card's last{(turn > state.Turn ? string.Empty : ", a half turn")}, so the game ends (A3.9; ruling R20.1)"
            : $"play.advance: turn {turn}, {phase}, {phasing} phasing", .. reasons]);

        void Finish(List<GameEvent> events, List<string> reasons, Func<RollRequest, RollResult>? draw = null)
        {
            if (ending)
            {
                // Pass 21 (ruling R21.4): the card's Victory Conditions decide the result as the game ends.
                var result = Victory(Replay([.. existing, .. events]), ended: true)?.AtEnd;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "game-ended",
                    new GameEnded(state.Turn, turn > state.Turn ? "last-game-turn" : "half-turn") { Result = result }, rulePackage: null, visibility: null));
                if (result is not null)
                {
                    reasons.Add($"play.result: {(result.Winner is { } winner ? $"{winner} wins" : "a draw")}: {result.Reason}");
                }

                return;
            }

            var payload = new PhaseChanged(turn, phase, phasing);
            var changed = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "phase-changed", payload, rulePackage: null, visibility: null));
            if (wind && draw is not null)
            {
                AddWindChange(scope, attemptId, expected, actor, state, events, reasons, draw, changed);
            }

            // E1.54 (backlog pass 16, ruling R16.6): at night a DM unit keeps DM until a Rally Original DR at most its printed morale.
            if (state.Phase == "rph" && state.Night)
            {
                var start = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged).index;
                var shed = existing.Skip(start).Select(item => item.Payload).OfType<RallyAttempted>()
                    .Where(item => item.Resolution.TryGetProperty("arithmetic", out var arithmetic) && arithmetic.ValueKind == JsonValueKind.Object
                        && arithmetic.TryGetProperty("originalDr", out var original) && state.Unit(item.Unit)?.Definition is { } definition
                        && FireReference.Value.Definitions.GetValueOrDefault(definition.Definition)?.BrokenMorale is { } printed && original.GetInt32() <= printed)
                    .Select(item => item.Unit).ToHashSet(StringComparer.Ordinal);
                foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Broken) && Is(unit, Conditions.DesperationMorale)
                    && !shed.Contains(unit.Id) && !retained.Contains(unit.Id)).OrderBy(unit => unit.Id, StringComparer.Ordinal))
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                        new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.DesperationMorale] = ConditionState.True }), null, null, [changed]));
                    reasons.Add($"play.night-dm: {unit.Id} keeps its DM: at night DM leaves only with a Rally Original DR at most the printed morale (E1.54)");
                }
            }

            // A10.62 (ruling R13.1): the DM its owner keeps as the RPh ends, and the DM of the start of the RtPh.
            foreach (var id in retained)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                    new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.DesperationMorale] = ConditionState.True }), null, null, [changed]));
                reasons.Add($"play.retain-dm: {id} keeps its DM (A10.62)");
            }

            if (phase == "rtph")
            {
                foreach (var (unit, why) in RoutPhaseDm(state))
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                        new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.DesperationMorale] = ConditionState.True }), null, null, [changed]));
                    reasons.Add($"play.dm: {unit.Id} comes under DM {why} as the RtPh begins (A10.62)");
                }
            }

            // A11.52 (ruling R11.16): an unarmed vehicle alone with enemy Infantry is captured as the CCPh begins.
            if (phase == "ccph")
            {
                foreach (var vehicle in CapturedVehicles(state))
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(vehicle.Id,
                        new Dictionary<string, ConditionState> { [Conditions.Captured] = ConditionState.True, [Conditions.Abandoned] = ConditionState.True }), null, null, [changed]));
                    reasons.Add($"play.cc-vehicle-capture: {vehicle.Id} is unarmed and alone with enemy Infantry, so it is captured; the use of captured vehicles is not built (A11.52, A21.2)");
                }
            }

            // A20.4 (ruling R5.7): at the start of their side's fire phase, berserk units massacre the enemy prisoners in their Location.
            foreach (var (massacre, why) in BerserkMassacres(state, phase, phasing))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "prisoners-massacred", massacre, null, null, [changed]));
                reasons.Add(why);
            }

            // D5.341, D5.41 (ruling R5.18): at the end of the Player Turn of its Recall, an immobilized Recalled AFV is Abandoned.
            if (phasing != state.PhasingSide)
            {
                foreach (var vehicle in state.Units.Where(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit) && Is(unit, Conditions.Recalled)
                    && Is(unit, Conditions.Immobilized) && !Is(unit, Conditions.Abandoned)).OrderBy(unit => unit.Id, StringComparer.Ordinal))
                {
                    foreach (var (type, abandon) in AbandonEvents(vehicle, attemptId))
                    {
                        events.Add(Event(scope, attemptId, events.Count + 1, expected, type, abandon, null, null, [changed]));
                    }

                    reasons.Add($"play.recall-abandoned: {vehicle.Id} is Recalled and immobilized, so its crew Abandons it (D5.341, D5.41)");
                }
            }
        }
    }

    private async Task<GamePlan> PlanEntryAsync(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected,
        string label, CancellationToken cancellationToken)
    {
        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "location", out var locationText)
            || !BoardLocation.TryParse(locationText, out var target))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: unitId and a location such as bd01:E4:0 are required");
        }

        if (Replay(existing).Current is not { } state || state.Unit(unitId) is not { Status: InstanceStatus.Active } unit)
        {
            return Refused(scope, label, expected, $"play.unit-unavailable: no active unit '{unitId}'");
        }

        var review = await ReviewEntryAsync(scope, state, unit, target, cancellationToken);
        var conclusion = review.Conclusion;
        if (conclusion.Disposition != ConclusionDisposition.Definitive)
        {
            return new GamePlan(GamePlanStatus.Refused, scope, label, expected, [],
                [$"play.not-definitive: the reviewed first case is {conclusion.Disposition.ToString().ToLowerInvariant()}", .. conclusion.ReasonCodes], review);
        }

        var move = new InstanceMoved(unit.Id, new MapPosition(target), Mf: 2);
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "instance-moved", move, ScenarioA1Package.Identity.ToString(), visibility: null)],
            [$"play.enter: {unit.Id} into {target} for 2 MF ({conclusion.ConclusionId})"], review);
    }

    /// <summary>
    /// The move action (ruling R10.12): a move of one squad, as a stack's first step, into a ground-level building override Location of board 01 that
    /// holds enemy units goes to the reviewed entry cases of unit steps 7 to 11, with their records; every other move is planned as a movement step.
    /// </summary>
    private async Task<GamePlan> PlanMoveOrEntryAsync(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected,
        string label, string actor, CancellationToken cancellationToken)
    {
        if (Strings(arguments, "unitIds").ToArray() is [{ } unitId] && Text(arguments, "to", out var toText) && BoardLocation.TryParse(toText, out var to)
            && !arguments.TryGetProperty("smoke", out _) && !arguments.TryGetProperty("bypass", out _)
            && !MoveOptions.Any(name => arguments.TryGetProperty(name, out var option) && option.ValueKind == JsonValueKind.True)
            && Replay(existing).Current is { Phase: "mph", Movement: null } state && state.Unit(unitId) is { Status: InstanceStatus.Active } unit
            && vocabulary.IsA(unit.Kind, "asl:squad") && !Is(unit, Conditions.Berserk)
            && state.At(to).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && !Is(other, Conditions.Captured))
            && state.Map.Board(to.Board) is { } placed && boards.TryGetBoard(to.Board, placed.Version).Board is { } handle
            && new BoardCatalogTerrainEvidence(boards).Bind(handle, to) is not null)
        {
            var entry = JsonSerializer.SerializeToElement(new Dictionary<string, object>
            {
                ["gameId"] = scope.Game,
                ["attemptId"] = attemptId,
                ["expectedRevision"] = expected,
                ["unitId"] = unitId,
                ["location"] = to.ToString(),
            });
            return await PlanEnterBuildingAsync(scope, entry, existing, attemptId, expected, label, actor, cancellationToken);
        }

        // Pass 21 (ruling R21.5): a move naming an edge leaves the map.
        if (Text(arguments, "exit", out var edge) && !arguments.TryGetProperty("to", out _))
        {
            return PlanExit(scope, arguments, existing, attemptId, expected, label, edge, actor);
        }

        return PlanMove(scope, arguments, existing, attemptId, expected, label, actor);
    }

    /// <summary>
    /// One entry action for every target (Occupied and Concealed Entry Design, section 6): the planner reads the whole
    /// game and routes the entry to the reviewed case that covers the target, or refuses it.
    /// </summary>
    private async Task<GamePlan> PlanEnterBuildingAsync(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId,
        long expected, string label, string actor, CancellationToken cancellationToken)
    {
        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "location", out var locationText)
            || !BoardLocation.TryParse(locationText, out var target))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: unitId and a location such as bd01:E4:0 are required");
        }

        if (Replay(existing).Current is not { } state || state.Unit(unitId) is not { Status: InstanceStatus.Active } unit)
        {
            return Refused(scope, label, expected, $"play.unit-unavailable: no active unit '{unitId}'");
        }

        var occupants = state.At(target).Where(item => item.Id != unit.Id).ToArray();
        var withheld = occupants.Length == 0 || occupants.Any(item => !VisibleTo(item, unit.Side));
        var facts = EntryFacts(state, unit, target);
        string[] moverReasons = [.. MoverFacts.Where(name => facts[name] != true)
            .Select(name => facts[name] is null ? $"play.fact-unknown: {name}" : $"play.fact-false: {name}")];

        if (occupants.Length == 0)
        {
            var plan = await PlanEntryAsync(scope, arguments, existing, attemptId, expected, label, cancellationToken);
            return plan with
            {
                Disclosure = new EntryDisclosure(unit.Side, EntryRoute.Empty, withheld, moverReasons)
            };
        }

        GamePlan Refuse(EntryRoute route, EntryReview? review, params string[] reasons) =>
            new(GamePlanStatus.Refused, scope, label, expected, [], reasons, review)
            {
                Disclosure = new EntryDisclosure(unit.Side, route, withheld, moverReasons)
            };

        var units = occupants.OfType<UnitInstance>().ToArray();
        var enemies = units.Length == occupants.Length && units.All(item => item.Side != unit.Side);
        bool Mmc(UnitInstance item) => vocabulary.IsA(item.Kind, "asl:mmc");
        bool Smc(UnitInstance item) => vocabulary.IsA(item.Kind, "asl:smc");
        var knownEnemy = enemies && units.All(Mmc) && units.All(item => VisibleTo(item, unit.Side));
        var concealedAll = enemies && units.All(item => Mmc(item) || Smc(item)) && units.All(item => Is(item, Conditions.Concealed) || Is(item, Conditions.Hidden));
        var route = knownEnemy ? EntryRoute.KnownEnemy
            : !concealedAll ? EntryRoute.Outside
            : units.Length == 1 && Mmc(units[0]) ? EntryRoute.Concealed

            // The reviewed decline covers only an SMC that was concealed, not hidden, when the attempt began.
            : units.Length == 1 && Is(units[0], Conditions.Concealed) && !Is(units[0], Conditions.Hidden) ? EntryRoute.LoneSmc
            : units.Length > 1 && !units.Any(item => Smc(item) && Is(item, Conditions.Hidden)) ? EntryRoute.RandomSelection
            : EntryRoute.Outside;
        if (route == EntryRoute.Outside)
        {
            return Refuse(route, null, "play.outside-reviewed-cases: the target holds units no reviewed case covers "
                + "(a hidden SMC, a friendly unit, known and concealed units together, or an entity)");
        }

        if (moverReasons.Length > 0)
        {
            return Refuse(route, null, ["play.mover-cannot-attempt", .. moverReasons]);
        }

        if (A414Exception(unit) is not false)
        {
            return Refuse(route, null, "play.a414-exception: the mover may be Berserk, Disrupted, or captured, so A4.14 (p. 49) may not apply");
        }

        if (state.Map.Board(target.Board) is not { } placed || boards.TryGetBoard(target.Board, placed.Version).Board is not { } handle
            || new BoardCatalogTerrainEvidence(boards).Bind(handle, target) is not { } binding)
        {
            return Refuse(route, null, "play.outside-reviewed-board: the reviewed cases cover only the explicit building overrides of VASL board 01");
        }

        return route == EntryRoute.KnownEnemy
            ? Refuse(route, null, await KnownEnemyReasonsAsync(scope, state, unit, target, facts, binding, cancellationToken))
            : await PlanConcealedEntryAsync(scope, existing, state, unit, units, target, facts, binding, attemptId, expected, label, withheld, route, actor,
                cancellationToken);
    }

    /// <summary>The Occupied package's conclusion for an entry into known enemy MMC: a Definitive A4.14 prohibition (U8).</summary>
    private async Task<string[]> KnownEnemyReasonsAsync(GameScope scope, GameState state, UnitInstance unit, BoardLocation target,
        IReadOnlyDictionary<string, bool?> facts, ScenarioA1TerrainBinding binding, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var version = $"r{state.Revision}";
        var locationId = target.ToString();
        var snapshot = new ScenarioA1BoardSnapshot(scope.Tenant, ScenarioA1OccupiedPackage.Identity, unit.Id, locationId, version, now, LiveGames.Source,
            ScenarioA1BoardOccupancy.KnownUnconcealedEnemyMmc, facts["isAttackerMovementPhase"], facts["isKnownGoodOrderInfantrySquad"],
            facts["canMoveThisPhase"], null, true, null, null, null, facts["hasNoSpecialRuleOrOtherModifier"], true, binding);
        var provider = new ScenarioA1BoardObservationProvider(
            new Board01ValidatedSnapshotSource(new LiveBoardSnapshotSource(snapshot), new BoardCatalogTerrainEvidence(boards)));
        var package = (await new ScenarioA1OccupiedPackage().ResolveAsync(ScenarioA1OccupiedPackage.Identity, cancellationToken)).Package!;
        var observed = await provider.ObserveAsync(Query(scope, package.Identity, ScenarioA1BoardObservationProvider.QueryKind, unit.Id, locationId, version),
            cancellationToken);
        if (observed.Observations.Count != 1)
        {
            return ["play.not-definitive: the Occupied package observed no reviewed case", .. observed.ReasonCodes];
        }

        const string caseId = "A1-known-enemy-mmc-mph";
        var conclusion = await new ScenarioA1OccupiedConclusionResolver().ConcludeAsync(Context(scope, state, package, unit.Id, locationId,
            ScenarioA1OccupiedConclusionResolver.QuestionKind, new
            {
                unitId = unit.Id,
                locationId,
                caseId,
                observationVersion = version
            }, observed.Observations[0], now),
            cancellationToken);
        return conclusion.Disposition == ConclusionDisposition.Definitive
            ? [$"play.prohibited: A4.14 (p. 49) prohibits entering a location with a known enemy unit in the MPh ({conclusion.ConclusionId})", .. conclusion.ReasonCodes]
            : [$"play.not-definitive: the Occupied case is {conclusion.Disposition.ToString().ToLowerInvariant()}", .. conclusion.ReasonCodes];
    }

    /// <summary>
    /// An entry into concealed or hidden enemy units (A12.15, p. 78): one MMC is revealed and forces the mover back (step 8);
    /// a lone concealed SMC is revealed and the attempt waits for the attacker's OVR declaration; several units are
    /// resolved by a Random Selection roll drawn inside the commit (A.9, p. 43). Every branch is decided here, before any
    /// roll, and the forced back is committed only on the Definitive PostReveal conclusion (Random Selection reveal review).
    /// </summary>
    private async Task<GamePlan> PlanConcealedEntryAsync(GameScope scope, IReadOnlyList<GameEvent> existing, GameState state, UnitInstance unit,
        UnitInstance[] defenders, BoardLocation target, IReadOnlyDictionary<string, bool?> facts, ScenarioA1TerrainBinding binding, string attemptId,
        long expected, string label, bool withheld, EntryRoute route, string actor, CancellationToken cancellationToken)
    {
        GamePlan Refuse(params string[] reasons) => new(GamePlanStatus.Refused, scope, label, expected, [], reasons)
        {
            Disclosure = new EntryDisclosure(unit.Side, route, withheld, [])
        };

        var from = state.Location(unit.Id)!.Location;
        var hazards = state.At(from).Where(item => vocabulary.IsA(item.Kind, "asl:fortification") || vocabulary.IsA(item.Kind, "asl:residual")
            || vocabulary.IsA(item.Kind, "asl:fire")).ToArray();
        if (hazards.Length > 0)
        {
            return Refuse($"play.return-hazard: {string.Join(", ", hazards.Select(item => item.Kind))} at {from}; the forced back covers only a clear return");
        }

        var package = ScenarioA1PostRevealPackage.Identity.ToString();
        var attempt = EventId(attemptId, 1);
        GameEvent[] subjects = [];
        List<GameEvent> Prefix()
        {
            var events = new List<GameEvent> { Event(scope, attemptId, 1, expected, "entry-attempted", new EntryAttempted(unit.Id, target, 2), package, null) };

            // A12.15: hidden units are first placed beneath a "?".
            foreach (var hidden in defenders.Where(item => Is(item, Conditions.Hidden)))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(hidden.Id,
                    new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.False, [Conditions.Concealed] = ConditionState.True }),
                    package, null, [attempt]));
            }

            return events;
        }

        void Reveal(List<GameEvent> events, IEnumerable<string> ids)
        {
            foreach (var id in ids)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(id,
                    new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.False, [Conditions.Concealed] = ConditionState.False }),
                    package, null, [attempt]));
            }
        }

        void ForceBack(List<GameEvent> events) =>
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "entry-forced-back", new EntryForcedBack(unit.Id, attempt, from, 2, false),
                package, null, [attempt]));

        // The forced back is checked once, before any roll: the PostReveal facts do not depend on which non-Dummy unit is
        // revealed, so a candidate that reveals one stands for every branch that forces the mover back. A lone SMC's
        // branch ends in the same forced back once the OVR is declined, so it is checked the same way.
        var sample = defenders.FirstOrDefault(item => vocabulary.IsA(item.Kind, "asl:mmc")) is { } mmc
            ? [mmc.Id]
            : defenders.Select(item => item.Id).Take(defenders.Length > 1 ? 2 : 1).ToArray();
        var candidateEvents = Prefix();
        Reveal(candidateEvents, sample);
        ForceBack(candidateEvents);
        var candidate = Replay([.. existing, .. candidateEvents]);
        if (candidate.HasErrors || candidate.Current is not { } after)
        {
            return Refuse([.. candidate.Diagnostics.Where(item => item.Severity == Units.UnitDiagnosticSeverity.Error).Select(item => item.ToString())]);
        }

        var conclusion = await PostRevealAsync(scope, after, unit, target, from, facts, binding, cancellationToken);
        if (conclusion.Disposition != ConclusionDisposition.Definitive)
        {
            return Refuse([$"play.not-definitive: the PostReveal case is {conclusion.Disposition.ToString().ToLowerInvariant()}", .. conclusion.Reasons]);
        }

        GamePlan Ready(IReadOnlyList<GameEvent> events, string reason) => new(GamePlanStatus.Ready, scope, label, expected, events, [reason])
        {
            Disclosure = new EntryDisclosure(unit.Side, route, withheld, [])
        };

        switch (route)
        {
            case EntryRoute.Concealed:
                {
                    var events = Prefix();
                    Reveal(events, [defenders[0].Id]);
                    ForceBack(events);
                    return Ready(events,
                        $"play.forced-back: {unit.Id} attempts {target}, {defenders[0].Id} is revealed, and {unit.Id} returns to {from} with 2 MF spent and its MPh ended ({conclusion.ConclusionId})");
                }

            case EntryRoute.LoneSmc:
                {
                    var events = Prefix();
                    Reveal(events, [defenders[0].Id]);
                    return Ready(events,
                        $"play.declaration-pending: {unit.Id} attempts {target} and {defenders[0].Id} is revealed; the attacker may decline or elect an Infantry OVR (A12.15, p. 78)");
                }

            default:
                {
                    // Die order is fixed, by unit id, so replay can recompute the reveal from the recorded values.
                    string[] order = [.. defenders.Select(item => item.Id).Order(StringComparer.Ordinal)];
                    var defenderSide = defenders[0].Side;
                    IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
                    {
                        var events = Prefix();
                        var roll = draw(new RollRequest(order.Length, 6));
                        var rollId = $"{attemptId}-roll-1";
                        events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                            new DiceRolled(rollId, "random-selection", roll.Request.Count, roll.Request.Sides, roll.Values, DiceRolled.SystemSource, actor), package, null,
                            [attempt]));
                        events.Add(Event(scope, attemptId, events.Count + 1, expected, "random-selection", new RandomSelection(rollId, attempt, order), package,
                            [defenderSide], [attempt]));
                        var highest = roll.Values.Max();
                        string[] revealing = [.. order.Where((_, index) => roll.Values[index] == highest)];
                        Reveal(events, revealing);

                        // A12.15 and A4.15 (p. 49): a revealed MMC, or more than one revealed SMC, forces the mover back; a lone
                        // revealed SMC leaves the attempt open for the attacker's OVR declaration.
                        var loneSmc = revealing.Length == 1 && defenders.First(item => item.Id == revealing[0]) is var only && vocabulary.IsA(only.Kind, "asl:smc");
                        if (!loneSmc)
                        {
                            ForceBack(events);
                        }

                        return events;
                    }

                    return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
                        [$"play.random-selection: one dr for each of the {order.Length} units at {target} decides the reveal (A.9, p. 43); a revealed MMC, or more than one revealed SMC, forces {unit.Id} back ({conclusion.ConclusionId})"])
                    {
                        Disclosure = new EntryDisclosure(unit.Side, route, withheld, []),
                        Roll = new PlannedRoll("random-selection", Build),
                        FirstEventId = attempt,
                    };
                }
        }
    }

    /// <summary>The PostReveal conclusion over a candidate state in which a non-Dummy unit at the target is revealed.</summary>
    private async Task<(ConclusionDisposition Disposition, string? ConclusionId, IReadOnlyList<string> Reasons)> PostRevealAsync(GameScope scope,
        GameState after, UnitInstance unit, BoardLocation target, BoardLocation from, IReadOnlyDictionary<string, bool?> facts,
        ScenarioA1TerrainBinding binding, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var version = $"r{after.Revision}";
        var locationId = target.ToString();

        // No OVR is elected in any branch this checks: a revealed MMC or several SMC give no option, and a declined
        // election means none was made (Random Selection reveal review).
        var snapshot = new ScenarioA1PostRevealSnapshot(scope.Tenant, ScenarioA1PostRevealPackage.Identity, unit.Id, locationId, from.ToString(), version, now,
            LiveGames.Source, binding, ScenarioA1DefenderReveal.NonDummy,
            facts["isAttackerMovementPhase"], vocabulary.IsA(unit.Kind, "asl:mmc"), facts["isKnownGoodOrderInfantrySquad"], true,
            A414Exception(unit) is false, true, facts["hasNoSpecialRuleOrOtherModifier"]);
        var provider = new ScenarioA1PostRevealObservationProvider(new LivePostRevealSnapshotSource(snapshot), new BoardCatalogTerrainEvidence(boards));
        var descriptor = (await new ScenarioA1PostRevealPackage().ResolveAsync(ScenarioA1PostRevealPackage.Identity, cancellationToken)).Package!;
        var observed = await provider.ObserveAsync(Query(scope, descriptor.Identity, ScenarioA1PostRevealObservationProvider.QueryKind, unit.Id, locationId, version),
            cancellationToken);
        if (observed.Observations.Count != 1)
        {
            return (ConclusionDisposition.Abstained, null, ["play.not-definitive: the PostReveal package observed no reviewed case", .. observed.ReasonCodes]);
        }

        var conclusion = await new ScenarioA1PostRevealConclusionResolver().ConcludeAsync(Context(scope, after, descriptor, unit.Id, locationId,
            ScenarioA1PostRevealConclusionResolver.QuestionKind, new
            {
                unitId = unit.Id,
                locationId,
                observationVersion = version
            }, observed.Observations[0], now),
            cancellationToken);
        return (conclusion.Disposition, conclusion.ConclusionId, conclusion.ReasonCodes);
    }

    /// <summary>
    /// The attacker's Infantry OVR declaration for an open attempt that revealed a lone SMC (Random Selection and
    /// Declined OVR Design, section 6). A decline is resolved through the reviewed delegation: ConcealedSmcOverrun hands
    /// the declined case to PostReveal, whose Definitive forced back is committed. An election is refused with a generic
    /// reason until step 10 reviews the NTC and what follows it; since every election is refused, the refusal discloses nothing.
    /// </summary>
    private async Task<GamePlan> PlanDeclareAsync(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId,
        long expected, string label, string actor, CancellationToken cancellationToken)
    {
        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "choice", out var choice) || choice is not ("decline" or "elect"))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: unitId and a choice of decline or elect are required");
        }

        if (Replay(existing).Current is not { } state || state.Unit(unitId) is not { Status: InstanceStatus.Active } unit)
        {
            return Refused(scope, label, expected, $"play.unit-unavailable: no active unit '{unitId}'");
        }

        if (state.OpenAttempts.FirstOrDefault(open => open.Unit == unit.Id && open.Declaration is null) is not { } open)
        {
            return Refused(scope, label, expected, $"play.no-pending-declaration: {unit.Id} has no entry attempt awaiting an OVR declaration");
        }

        // The declaration exists only when the attempt revealed exactly one SMC and nothing else (A12.15, p. 78).
        var revealed = state.At(open.Target).OfType<UnitInstance>()
            .Where(item => item.Side != unit.Side && GameState.Condition(item, Conditions.Concealed) == ConditionState.False
                && GameState.Condition(item, Conditions.Hidden) != ConditionState.True)
            .ToArray();
        if (revealed.Length != 1 || !vocabulary.IsA(revealed[0].Kind, "asl:smc") || (open.Revealing.Count > 0 && !open.Revealing.SequenceEqual([revealed[0].Id])))
        {
            return Refused(scope, label, expected, "play.no-pending-declaration: the attempt did not reveal exactly one SMC");
        }

        var smc = revealed[0];

        var from = state.Location(unit.Id)!.Location;
        var hazards = state.At(from).Where(item => vocabulary.IsA(item.Kind, "asl:fortification") || vocabulary.IsA(item.Kind, "asl:residual")
            || vocabulary.IsA(item.Kind, "asl:fire")).ToArray();
        if (hazards.Length > 0)
        {
            return Refused(scope, label, expected,
                $"play.return-hazard: {string.Join(", ", hazards.Select(item => item.Kind))} at {from}; the forced back covers only a clear return");
        }

        if (state.Map.Board(open.Target.Board) is not { } placed || boards.TryGetBoard(open.Target.Board, placed.Version).Board is not { } handle
            || new BoardCatalogTerrainEvidence(boards).Bind(handle, open.Target) is not { } binding)
        {
            return Refused(scope, label, expected, "play.outside-reviewed-board: the reviewed cases cover only the explicit building overrides of VASL board 01");
        }

        // The facts of the attempt as it was made: the unit's movement is held by the open attempt, which is not a reason
        // it could not have attempted.
        var facts = EntryFacts(state with
        {
            OpenAttempts = [.. state.OpenAttempts.Where(item => item.EventId != open.EventId)]
        }, unit, open.Target);
        var attemptEvents = existing.SkipWhile(item => item.EventId != open.EventId).ToArray();
        var placedBeneathQuestionMark = attemptEvents.Any(item => item.Payload is ConditionsChanged changed && changed.Id == smc.Id
            && changed.Conditions.TryGetValue(Conditions.Concealed, out var concealed) && concealed == ConditionState.True);
        var revealedByAttempt = attemptEvents.Any(item => item.Causes.Contains(open.EventId) && item.Payload is ConditionsChanged changed && changed.Id == smc.Id
            && changed.Conditions.TryGetValue(Conditions.Concealed, out var concealed) && concealed == ConditionState.False);
        if (choice == "elect")
        {
            return await PlanElectAsync(scope, state, unit, open, smc, from, facts, binding, !placedBeneathQuestionMark, revealedByAttempt, attemptId, expected,
                label, actor, cancellationToken);
        }

        var now = clock.GetUtcNow();
        var version = $"r{state.Revision}";
        var locationId = open.Target.ToString();
        var snapshot = new ScenarioA1ConcealedSmcOverrunSnapshot(scope.Tenant, ScenarioA1ConcealedSmcOverrunPackage.Identity, unit.Id, locationId, version, now,
            LiveGames.Source, binding,
            placedBeneathQuestionMark ? ScenarioA1InitialConcealedOccupancy.Hidden : ScenarioA1InitialConcealedOccupancy.Concealed,
            revealedByAttempt, ScenarioA1RevealedOccupant.EnemySmc, smc.Position is MapPosition,
            facts["isAttackerMovementPhase"], facts["isKnownGoodOrderInfantrySquad"], facts["isAdjacentGroundLevelOrdinaryBuilding"], true,
            A414Exception(unit) is false, And(facts["hasNoRoadBypassElevationOrAdditionalTerrain"], facts["hasNoSpecialRuleOrOtherModifier"]),

            // The unit moves alone, so no leader exempts it from anything.
            true,
            ScenarioA1OverrunElection.Declined, null, null, null, null, null, null, null);
        const string caseId = "A1-concealed-smc-declined";
        var package = (await new ScenarioA1ConcealedSmcOverrunPackage().ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity, cancellationToken)).Package!;
        var provider = new ScenarioA1ConcealedSmcOverrunObservationProvider(new LiveConcealedSmcOverrunSnapshotSource(snapshot),
            new BoardCatalogTerrainEvidence(boards));
        var observed = await provider.ObserveAsync(new ObservationQuery($"{scope.Game}-{version}-{unit.Id}-ovr", scope.Tenant, package.Identity,
            new SemanticIdentifier(package.Identity.DomainId, ScenarioA1ConcealedSmcOverrunObservationProvider.QueryKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = unit.Id,
                locationId,
                observationVersion = version,
                caseId
            }), 1), cancellationToken);
        if (observed.Observations.Count != 1)
        {
            return Refused(scope, label, expected, ["play.not-definitive: the concealed-SMC OVR package observed no reviewed case", .. observed.ReasonCodes]);
        }

        var delegation = await new ScenarioA1ConcealedSmcOverrunConclusionResolver().ConcludeAsync(Context(scope, state, package, unit.Id, locationId,
            ScenarioA1ConcealedSmcOverrunConclusionResolver.QuestionKind, new
            {
                unitId = unit.Id,
                locationId,
                observationVersion = version,
                caseId
            }, observed.Observations[0], now),
            cancellationToken);
        if (!delegation.ReasonCodes.Contains(DeclinedDelegation))
        {
            return Refused(scope, label, expected, ["play.not-delegated: the concealed-SMC OVR package did not delegate the declined case", .. delegation.ReasonCodes]);
        }

        var declaration = EventId(attemptId, 1);
        GameEvent[] events =
        [
            Event(scope, attemptId, 1, expected, "overrun-declared", new OverrunDeclared(unit.Id, open.EventId, OverrunDeclared.Declined),
                ScenarioA1ConcealedSmcOverrunPackage.Identity.ToString(), null, [open.EventId]),
            Event(scope, attemptId, 2, expected, "entry-forced-back", new EntryForcedBack(unit.Id, open.EventId, from, open.Mf, false),
                ScenarioA1PostRevealPackage.Identity.ToString(), null, [open.EventId, declaration]),
        ];
        var candidate = Replay([.. existing, .. events]);
        if (candidate.HasErrors || candidate.Current is not { } after)
        {
            return Refused(scope, label, expected, [.. candidate.Diagnostics.Where(item => item.Severity == Units.UnitDiagnosticSeverity.Error).Select(item => item.ToString())]);
        }

        var conclusion = await PostRevealAsync(scope, after, unit, open.Target, from, facts, binding, cancellationToken);
        return conclusion.Disposition != ConclusionDisposition.Definitive
            ? Refused(scope, label, expected, [$"play.not-definitive: the PostReveal case is {conclusion.Disposition.ToString().ToLowerInvariant()}", .. conclusion.Reasons])
            : new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
                [$"play.declined: {unit.Id} declines the OVR against {smc.Id}; the reviewed matrix delegates the case ({delegation.ConclusionId}), "
                    + $"and {unit.Id} returns to {from} with {open.Mf} MF spent and its MPh ended ({conclusion.ConclusionId})"]);
    }

    /// <summary>
    /// An elected Infantry OVR after a lone SMC reveal (Infantry OVR Design, section 5): the NTC is rolled first; a failure
    /// forces the mover back, and a pass is followed by a Random Selection among the other concealed units, their reveal,
    /// and the forced back. Both branches are checked Definitive against the OVR NTC package before any roll.
    /// </summary>
    private async Task<GamePlan> PlanElectAsync(GameScope scope, GameState state, UnitInstance unit, OpenAttempt open, UnitInstance smc,
        BoardLocation from, IReadOnlyDictionary<string, bool?> facts, ScenarioA1TerrainBinding binding, bool initiallyConcealed, bool revealedByAttempt,
        string attemptId, long expected, string label, string actor, CancellationToken cancellationToken)
    {
        // A12.15 offers the OVR only if possible, and A4.15 doubles the MF of entry: four MF must be left. The attempt's own
        // MF are not yet spent.
        var remaining = Experience.MfAllowance(state, unit, catalogs, vocabulary) is { } allowance ? allowance - unit.MfSpent : (int?)null;
        if (remaining is null)
        {
            return Refused(scope, label, expected, "play.election-unavailable: the mover's MF allowance is unknown");
        }

        var observed = await OvrNtcAsync(scope, state, unit, open, from, facts, binding, initiallyConcealed, revealedByAttempt,
            remaining >= 4 ? ScenarioA1OvrElection.Elected : ScenarioA1OvrElection.Requested,
            remaining >= 4 ? ScenarioA1OvrRemainingMf.AtLeastFour : ScenarioA1OvrRemainingMf.BelowFour,
            remaining >= 4 ? ScenarioA1OvrNtcResult.Failed : null, null, null, remaining >= 4 ? "A1-ovr-ntc-failed" : "A1-ovr-ntc-mf-insufficient",
            cancellationToken);
        if (remaining < 4)
        {
            return Refused(scope, label, expected,
                $"play.election-unavailable: {unit.Id} has {remaining} MF left, and an Infantry OVR needs four (A4.15, p. 49) ({observed.ConclusionId})");
        }

        // Another concealed non-Dummy unit must be present; against a lone SMC a passed NTC leads to its options and CC,
        // which are unreviewed. The refusal says only that the adjudicator cannot resolve the election (Infantry OVR
        // Design, section 6).
        UnitInstance[] others = [.. state.At(open.Target).OfType<UnitInstance>()
            .Where(item => item.Id != smc.Id && item.Side != unit.Side && GameState.Condition(item, Conditions.Concealed) == ConditionState.True
                && (vocabulary.IsA(item.Kind, "asl:mmc") || vocabulary.IsA(item.Kind, "asl:smc")))];
        if (others.Length == 0)
        {
            return Refused(scope, label, expected, EntryDisclosure.CannotResolve + " (the election could lead to outcomes that are not yet reviewed)");
        }

        if (observed.Disposition != ConclusionDisposition.Definitive)
        {
            return Refused(scope, label, expected, ["play.not-definitive: the OVR NTC failed case is not Definitive", .. observed.Reasons]);
        }

        var passedBranch = await OvrNtcAsync(scope, state, unit, open, from, facts, binding, initiallyConcealed, revealedByAttempt,
            ScenarioA1OvrElection.Elected, ScenarioA1OvrRemainingMf.AtLeastFour, ScenarioA1OvrNtcResult.Passed, ScenarioA1OtherConcealedNonDummy.Present, true,
            "A1-ovr-ntc-passed-second-defender-revealed", cancellationToken);
        if (passedBranch.Disposition != ConclusionDisposition.Definitive)
        {
            return Refused(scope, label, expected, ["play.not-definitive: the OVR NTC passed case is not Definitive", .. passedBranch.Reasons]);
        }

        // The NTC: two dice against the Morale Level, with the building TEM as DRM (A10.1, p. 65; B23.3, p. 136).
        var morale = unit.Definition is { } reference
            ? catalogs.FirstOrDefault(item => item.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:morale")?.Value?.Number
            : null;
        if (morale is not { } moraleLevel || !new Board01TerrainCatalog().TryGetBuilding(binding.Hex, out var building))
        {
            return Refused(scope, label, expected, "play.ntc-unavailable: the mover's printed morale or the building's construction is unknown");
        }

        var tem = new TaskCheckModifier("B23.3", building!.Material == "stone" ? 3 : 2);
        var package = ScenarioA1OvrNtcPackage.Identity.ToString();
        var declaration = EventId(attemptId, 1);
        string[] order = [.. others.Select(item => item.Id).Order(StringComparer.Ordinal)];
        var defenderSide = smc.Side;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            List<GameEvent> events =
            [
                Event(scope, attemptId, 1, expected, "overrun-declared", new OverrunDeclared(unit.Id, open.EventId, OverrunDeclared.Elected), package, null,
                    [open.EventId]),
            ];
            var ntc = draw(new RollRequest(2, 6));
            var ntcRoll = $"{attemptId}-roll-1";
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                new DiceRolled(ntcRoll, TaskCheck.OvrNtc, 2, 6, ntc.Values, DiceRolled.SystemSource, actor), package, null, [open.EventId, declaration]));
            var finalDr = ntc.Values.Sum() + tem.Value;
            var passed = finalDr <= moraleLevel;
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "task-check",
                new TaskCheck(unit.Id, ntcRoll, TaskCheck.OvrNtc, moraleLevel, [tem], finalDr, passed), package, null, [open.EventId, declaration]));
            if (passed)
            {
                // A passed NTC makes the MMC capable of OVR; the DEFENDER must then reveal another non-Dummy unit, chosen by
                // Random Selection (A12.15, p. 78; A.9, p. 43).
                var selection = draw(new RollRequest(order.Length, 6));
                var selectionRoll = $"{attemptId}-roll-2";
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(selectionRoll, "random-selection", order.Length, 6, selection.Values, DiceRolled.SystemSource, actor), package, null,
                    [open.EventId, declaration]));
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "random-selection", new RandomSelection(selectionRoll, open.EventId, order),
                    package, [defenderSide], [open.EventId, declaration]));
                var highest = selection.Values.Max();
                foreach (var id in order.Where((_, index) => selection.Values[index] == highest))
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(id,
                        new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.False, [Conditions.Concealed] = ConditionState.False }),
                        package, null, [open.EventId, declaration]));
                }
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "entry-forced-back", new EntryForcedBack(unit.Id, open.EventId, from, open.Mf, false),
                package, null, [open.EventId, declaration]));
            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.elected: {unit.Id} attempts an Infantry OVR against {smc.Id}. The NTC is two dice + {tem.Value} (B23.3) against morale {moraleLevel}; "
                + $"a failure forces {unit.Id} back ({observed.ConclusionId}), and a pass reveals another unit by Random Selection and forces it back ({passedBranch.ConclusionId})"])
        {
            Roll = new PlannedRoll(TaskCheck.OvrNtc, Build),
            FirstEventId = declaration,
        };
    }

    /// <summary>The OVR NTC package's conclusion for one branch of an election, over a live snapshot of that branch.</summary>
    private async Task<(ConclusionDisposition Disposition, string? ConclusionId, IReadOnlyList<string> Reasons)> OvrNtcAsync(GameScope scope, GameState state,
        UnitInstance unit, OpenAttempt open, BoardLocation from, IReadOnlyDictionary<string, bool?> facts, ScenarioA1TerrainBinding binding, bool initiallyConcealed,
        bool revealedByAttempt, ScenarioA1OvrElection election, ScenarioA1OvrRemainingMf remainingMf, ScenarioA1OvrNtcResult? ntc,
        ScenarioA1OtherConcealedNonDummy? other, bool? secondReveal, string caseId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var version = $"r{state.Revision}";
        var locationId = open.Target.ToString();
        var snapshot = new ScenarioA1OvrNtcSnapshot(scope.Tenant, ScenarioA1OvrNtcPackage.Identity, unit.Id, locationId, from.ToString(), version, now,
            LiveGames.Source, binding, facts["isAttackerMovementPhase"], facts["isKnownGoodOrderInfantrySquad"], initiallyConcealed, revealedByAttempt, true,
            A414Exception(unit) is false, facts["hasNoSpecialRuleOrOtherModifier"], true, election, remainingMf, ntc, other, secondReveal);
        var provider = new ScenarioA1OvrNtcObservationProvider(new LiveOvrNtcSnapshotSource(snapshot), new BoardCatalogTerrainEvidence(boards));
        var descriptor = (await new ScenarioA1OvrNtcPackage().ResolveAsync(ScenarioA1OvrNtcPackage.Identity, cancellationToken)).Package!;
        var observed = await provider.ObserveAsync(Query(scope, descriptor.Identity, ScenarioA1OvrNtcObservationProvider.QueryKind, unit.Id, locationId, version),
            cancellationToken);
        if (observed.Observations.Count != 1)
        {
            return (ConclusionDisposition.Abstained, null, ["play.not-definitive: the OVR NTC package observed no reviewed case", .. observed.ReasonCodes]);
        }

        var question = new DomainQuestion($"{ScenarioA1OvrNtcConclusionResolver.QuestionKind}-{scope.Game}-r{state.Revision}-{unit.Id}-{caseId}", scope.Tenant,
            descriptor.Identity, new SemanticIdentifier(descriptor.Identity.DomainId, ScenarioA1OvrNtcConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = unit.Id,
                locationId,
                previousLocationId = from.ToString(),
                observationVersion = version,
                caseId
            }), now);
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("query-" + id, scope.Tenant, descriptor.Identity, new SemanticIdentifier(descriptor.Identity.DomainId, "unit-or-location"), id),
            DomainEntityResolutionOutcome.Resolved,
            [new DomainEntityCandidate(new SemanticIdentifier(descriptor.Identity.DomainId, id), descriptor.CanonicalSources[0],
                new EvidenceReference("entity:" + id, EvidenceKind.Observation, scope.Tenant, descriptor.Identity, id,
                    state.Revision.ToString(CultureInfo.InvariantCulture), LiveGames.Source))],
            ["asl.live-game.resolution"]);
        var conclusion = await new ScenarioA1OvrNtcConclusionResolver().ConcludeAsync(
            new DomainConclusionContext(question, descriptor, [Entity(unit.Id), Entity(locationId), Entity(from.ToString())], [observed.Observations[0]]),
            cancellationToken);
        return (conclusion.Disposition, conclusion.ConclusionId, conclusion.ReasonCodes);
    }

    /// <summary>The reason the concealed-SMC OVR resolver gives when it delegates a declined election to the PostReveal package.</summary>
    private const string DeclinedDelegation = "asl.a1.ovr.declined-use-exact-post-reveal-package";

    private static bool? And(bool? left, bool? right) => left == false || right == false ? false : left == true && right == true ? true : null;

    /// <summary>
    /// Whether a committed attempt was for other inputs than these: an entry's first event names its unit and target, so
    /// an entry attempt reused for another unit or location is not a replay (DICE-10).
    /// </summary>
    private static bool ReusedWithOtherInputs(ActionDescriptor action, JsonElement arguments, GameEvent committed)
    {
        if (action.Id.Value == "asl.game.declare-overrun")
        {
            var declared = Text(arguments, "unitId", out var declaringUnit) ? declaringUnit : null;
            var chosen = Text(arguments, "choice", out var choiceText) ? choiceText : null;
            return committed.Payload is not OverrunDeclared declaration || declaration.Id != declared
                || declaration.Choice != (chosen == "decline" ? OverrunDeclared.Declined : OverrunDeclared.Elected);
        }

        if (action.Id.Value is not ("asl.game.enter-building" or "asl.game.enter-empty-building"))
        {
            return false;
        }

        var unitId = Text(arguments, "unitId", out var unitText) ? unitText : null;
        var target = Text(arguments, "location", out var locationText) && BoardLocation.TryParse(locationText, out var parsed) ? parsed : null;
        return committed.Payload switch
        {
            EntryAttempted attempted => attempted.Id != unitId || attempted.Target != target,
            InstanceMoved moved => moved.Id != unitId || moved.Position is not MapPosition { Location: var at } || at != target,
            _ => true,
        };
    }

    /// <summary>Whether a unit may be an A4.14 exception (p. 49): Berserk, Disrupted, or captured (and so Unarmed). Null when unknown.</summary>
    private static bool? A414Exception(UnitInstance unit)
    {
        ConditionState[] states = [GameState.Condition(unit, Conditions.Berserk), GameState.Condition(unit, "asl:disrupted"), GameState.Condition(unit, Conditions.Captured)];
        return states.Contains(ConditionState.True) ? true : states.All(item => item == ConditionState.False) ? false : null;
    }

    /// <summary>Whether a side can see an object: its own, or an enemy's that is neither concealed nor hidden.</summary>
    private static bool VisibleTo(IGameObject item, string side) =>
        item.Side == side || (GameState.Condition(item, Conditions.Concealed) == ConditionState.False && GameState.Condition(item, Conditions.Hidden) != ConditionState.True);

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private static ObservationQuery Query(GameScope scope, DomainPackageRef package, string kind, string unitId, string locationId, string version) =>
        new($"{scope.Game}-{version}-{unitId}", scope.Tenant, package, new SemanticIdentifier(package.DomainId, kind),
            JsonSerializer.SerializeToElement(new
            {
                unitId,
                locationId,
                observationVersion = version
            }), 1);

    private static DomainConclusionContext Context(GameScope scope, GameState state, DomainPackageDescriptor package, string unitId, string locationId,
        string questionKind, object parameters, Observation observation, DateTimeOffset now)
    {
        var question = new DomainQuestion($"{questionKind}-{scope.Game}-r{state.Revision}-{unitId}", scope.Tenant, package.Identity,
            new SemanticIdentifier(package.Identity.DomainId, questionKind), JsonSerializer.SerializeToElement(parameters), now);
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("query-" + id, scope.Tenant, package.Identity, new SemanticIdentifier(package.Identity.DomainId, "unit-or-location"), id),
            DomainEntityResolutionOutcome.Resolved,
            [new DomainEntityCandidate(new SemanticIdentifier(package.Identity.DomainId, id), package.CanonicalSources[0],
                new EvidenceReference("entity:" + id, EvidenceKind.Observation, scope.Tenant, package.Identity, id,
                    state.Revision.ToString(CultureInfo.InvariantCulture), LiveGames.Source))],
            ["asl.live-game.resolution"]);
        return new DomainConclusionContext(question, package, [Entity(unitId), Entity(locationId)], [observation]);
    }

    /// <summary>The nine facts of an entry and the reviewed first case's conclusion from them, for one state of a game.</summary>
    public async Task<EntryReview> ReviewEntryAsync(GameScope scope, GameState state, UnitInstance unit, BoardLocation target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(target);
        var facts = EntryFacts(state, unit, target);
        return new EntryReview(facts, await ConcludeAsync(scope, state, unit, target, facts, cancellationToken));
    }

    /// <summary>
    /// The nine facts of the reviewed first case, derived from live state and the map read API (Governed Writes Design,
    /// section 8). A fact the state cannot establish is null, never guessed, so the reviewed resolver stays indeterminate.
    /// </summary>
    public IReadOnlyDictionary<string, bool?> EntryFacts(GameState state, UnitInstance unit, BoardLocation target)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(target);
        bool? Known(string condition) => GameState.Condition(unit, condition) switch
        {
            ConditionState.True => true,
            ConditionState.False => false,
            _ => null,
        };

        var goodOrder = GameState.GoodOrder(unit, vocabulary);
        var from = state.Location(unit.Id);

        // A placed map reads across its seams (Composed Maps Design, section 7); otherwise the target's own board is read.
        LocationRead? targetRead;
        LocationRead? fromRead;
        bool adjacent;
        HexsideFacts? crossed = null;
        if (Composed(state) is { } composed)
        {
            targetRead = composed.Resolve(target).Read;
            fromRead = from is not null ? composed.Resolve(from.Location).Read : null;
            adjacent = targetRead is { IsDefinitive: true } && fromRead is { IsDefinitive: true }
                && composed.Distance(from!.Location.Board, from.Location.Hex, target.Board, target.Hex) == 1;
            crossed = adjacent ? composed.Crossed(from!.Location.Board, from.Location.Hex, target.Board, target.Hex)?.Agreed : null;
        }
        else
        {
            var handle = state.Map.Board(target.Board) is { } placed ? boards.TryGetBoard(target.Board, placed.Version).Board : null;
            targetRead = handle?.Resolve(target).Read;
            fromRead = from is not null && handle is not null && from.Location.Board == target.Board ? handle.Resolve(from.Location).Read : null;
            adjacent = targetRead is { IsDefinitive: true } && fromRead is { IsDefinitive: true } && handle!.Distance(from!.Location.Hex, target.Hex) == 1;
            if (adjacent)
            {
                var side = Enum.GetValues<HexsideDirection>().FirstOrDefault(direction => handle!.Neighbor(from!.Location.Hex, direction) == target.Hex);
                crossed = fromRead!.Hex.Hexsides.FirstOrDefault(item => item.Side == side);
            }
        }

        var definitive = targetRead is { IsDefinitive: true } && fromRead is { IsDefinitive: true };

        bool? And(params bool?[] values) => values.Any(value => value == false) ? false : values.All(value => value == true) ? true : null;

        var known = Known(Conditions.Concealed) is { } concealed && Known(Conditions.Hidden) is { } hidden ? !concealed && !hidden : (bool?)null;
        var squad = vocabulary.IsA(unit.Kind, "asl:squad");
        var occupants = state.At(target).Where(item => item.Id != unit.Id).ToArray();

        return new Dictionary<string, bool?>(StringComparer.Ordinal)
        {
            ["isKnownGoodOrderInfantrySquad"] = And(squad, goodOrder switch { ConditionState.True => true, ConditionState.False => false, _ => null }, known),
            ["isAttackerMovementPhase"] = state.Phase == "mph" && state.PhasingSide == unit.Side,

            // A4.1 (p. 48): a unit that is broken, TI, or held in Melee cannot move. Fire and Opportunity Fire are not
            // actions of the live source yet, so no live unit has fired.
            // A unit forced back has ended its MPh (A12.15, p. 78) and cannot move again in it.
            ["canMoveThisPhase"] = And(Known(Conditions.Broken) is { } broken ? !broken : null, Known("asl:ti") is { } ti ? !ti : null,
                Known(Conditions.Melee) is { } melee ? !melee : null, !unit.MovementEnded, !state.OpenAttempts.Any(open => open.Unit == unit.Id)),
            ["isAdjacentGroundLevelOrdinaryBuilding"] = !definitive ? null
                : adjacent && target.Level == 0 && target.Side is null && OrdinaryBuildings.Contains(targetRead!.Level.Terrain?.Name ?? string.Empty),
            ["isDestinationKnownEmpty"] = occupants.Length == 0,
            ["hasNoRoadBypassElevationOrAdditionalTerrain"] = !definitive || crossed is null ? (adjacent ? (bool?)null : false)
                : fromRead!.Hex.BaseLevel == targetRead!.Hex.BaseLevel && crossed.HexsideTerrain is null && crossed.Terrain?.IsRoad != true
                    && !crossed.Cliff && !crossed.Slope && !crossed.RailroadEmbankment && crossed.DepressionTerrain is null
                    && fromRead.Level.DepressionTerrain is null && targetRead.Level.DepressionTerrain is null,

            // A4.11 (p. 48) and A19.31 (p. 86): a Good Order MMC has four MF, three if Inexperienced. When the status is
            // unknown, two MF remain under either allotment after one MF spent, and under neither after three.
            ["hasEnoughMovementFactors"] = Experience.MfAllowance(state, unit, catalogs, vocabulary) is { } allowance
                ? allowance - unit.MfSpent >= 2
                : unit.MfSpent <= 1 ? true : unit.MfSpent >= 3 ? false : null,
            ["isBelowStackingLimit"] = occupants.Length == 0,
            ["hasNoSpecialRuleOrOtherModifier"] = state.SpecialRules.Count == 0,
        };
    }

    private async Task<DomainConclusion> ConcludeAsync(GameScope scope, GameState state, UnitInstance unit, BoardLocation target,
        IReadOnlyDictionary<string, bool?> facts, CancellationToken cancellationToken)
    {
        var package = (await new ScenarioA1Package().ResolveAsync(ScenarioA1Package.Identity, cancellationToken)).Package!;
        var now = clock.GetUtcNow();
        var locationId = target.ToString();
        var question = new DomainQuestion($"entry-{scope.Game}-r{state.Revision}-{unit.Id}", scope.Tenant, package.Identity,
            new SemanticIdentifier(package.Identity.DomainId, ScenarioA1ConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = unit.Id,
                locationId
            }), now);
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("query-" + id, scope.Tenant, package.Identity, new SemanticIdentifier(package.Identity.DomainId, "unit-or-location"), id),
            DomainEntityResolutionOutcome.Resolved,
            [new DomainEntityCandidate(new SemanticIdentifier(package.Identity.DomainId, id), package.CanonicalSources[0],
                new EvidenceReference("entity:" + id, EvidenceKind.Observation, scope.Tenant, package.Identity, id,
                    state.Revision.ToString(CultureInfo.InvariantCulture), LiveGames.Source))],
            ["asl.live-game.resolution"]);

        // Only the facts the state establishes are observed; an unknown fact is left out and stays unknown.
        var data = new JsonObject();
        foreach (var (name, value) in facts)
        {
            if (value is { } known)
            {
                data[name] = known;
            }
        }

        var observation = new Observation($"{scope.Game}-r{state.Revision}", new ObservationSource(LiveGames.Source), scope.Tenant, now,
            JsonSerializer.SerializeToElement(data), locationId, $"r{state.Revision}", "asl-unit-state", package.Identity);
        return await new ScenarioA1ConclusionResolver().ConcludeAsync(
            new DomainConclusionContext(question, package, [Entity(unit.Id), Entity(locationId)], [observation]), cancellationToken);
    }

    /// <summary>
    /// A start naming a scenario card (backlog pass 18, rulings R18.1, R18.2): the card must be embedded, unchanged since the request read it,
    /// valid against the catalog, and, when it leaves the first move to a die roll, the request names one of its sides. The start is then
    /// the card's; only the label and that side come from the request. Null when the start names no card.
    /// </summary>
    private bool CardStart(JsonElement start, out JsonObject? fromCard, out string? reason, out StartRolls? rolls)
    {
        fromCard = null;
        reason = null;
        rolls = null;
        if (!start.TryGetProperty("scenario", out var scenario) || scenario.ValueKind != JsonValueKind.Object)
        {
            return true;
        }

        var id = Text(scenario, "id", out var named) ? named : string.Empty;
        var sha256 = Text(scenario, "sha256", out var hash) ? hash : string.Empty;
        var catalogName = Text(start, "catalog", out var catalogText) ? catalogText : null;
        var catalog = UnitCatalogs.For(catalogs, catalogName);
        if (CardLibrary.Sha256(id) is not { } current)
        {
            reason = $"play.scenario: '{id}' is not a scenario card of the game (ruling R18.1)";
            return false;
        }

        if (current != sha256)
        {
            reason = $"play.scenario: the card '{id}' has changed since it was read; read it again (ruling R18.2)";
            return false;
        }

        if (catalog is null || CardLibrary.Read(id, catalog) is not { IsValid: true, Card: { } card })
        {
            reason = $"play.scenario: the card '{id}' is not valid against the catalog '{catalogName}' (ruling R18.1)";
            return false;
        }

        // Pass 20 (ruling R20.2): the die roll the card leaves the first move to is drawn at the first setup; the start names no winner.
        var first = Text(start, "firstSide", out var side) && side.Length > 0 ? side : null;
        if (card.Turns.MovesFirst is null && first is not null)
        {
            reason = $"play.scenario: {card.Turns.MovesFirstNote} The game rolls it as it starts, so the start names no winner (A3.9; ruling R20.2)";
            return false;
        }

        // A26.4 (ruling R20.3): the Balance, by agreement or by a dr when both players wish to play the same side.
        if (Balance(card, start, out var balance, out var players, out var wanted, out reason) is false)
        {
            return false;
        }

        fromCard = ScenarioCards.Start(card, current, catalogName!, Text(start, "label", out var cardLabel) ? cardLabel : null, card.Turns.MovesFirst ?? card.Turns.SetsUpFirst);
        if (balance is not null)
        {
            fromCard["scenario"]!["balance"] = balance;
        }

        if (players.Count > 0)
        {
            fromCard["scenario"]!["players"] = new JsonArray([.. players.Select(player => (JsonNode)new JsonObject { ["name"] = player.Name, ["side"] = player.Side })]);
        }

        rolls = new StartRolls(card.Turns.MovesFirst is null ? [.. card.Sides.Select(item => item.Side)] : null, wanted);
        return true;
    }

    /// <summary>The drs a start from a card draws (rulings R20.2, R20.3): the sides that roll for the first move, and the Balance roll's players.</summary>
    private sealed record StartRolls(IReadOnlyList<string>? FirstMove, BalanceRoll? Balance);

    /// <summary>Two players who wish to play the same side (A26.4): the side, the players in the order the start names them, and the other side.</summary>
    private sealed record BalanceRoll(string Wanted, string First, string Second, string Other);

    /// <summary>
    /// The start's Balance (A26.4; ruling R20.3): <c>balance.side</c> names the side that takes it by agreement; <c>balance.players</c> names two players
    /// and the side each wishes to play: the same side for both is decided by a dr, the other side then taking its Balance; different sides, none.
    /// </summary>
    private static bool Balance(ScenarioCard card, JsonElement start, out string? balance, out List<ScenarioPlayer> players, out BalanceRoll? wanted, out string? reason)
    {
        balance = null;
        players = [];
        wanted = null;
        reason = null;
        if (!start.TryGetProperty("balance", out var node) || node.ValueKind != JsonValueKind.Object)
        {
            return true;
        }

        var sides = card.Sides.Select(item => item.Side).ToArray();
        if (Text(node, "side", out var agreed))
        {
            balance = agreed;
        }

        if (node.TryGetProperty("players", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            var named = list.EnumerateArray().Select((item, index) => (Name: Text(item, "name", out var name) && name.Length > 0 ? name : index == 0 ? "the first player" : "the second player",
                Wants: Text(item, "wants", out var wants) ? wants : null)).ToArray();
            if (named.Length != 2 || named.Any(item => !sides.Contains(item.Wants, StringComparer.Ordinal)) || named[0].Name == named[1].Name
                || (balance is not null && named[0].Wants == named[1].Wants))
            {
                reason = "play.balance: the Balance names two players by different names, each with a side of the card they wish to play, and, when they wish different sides, the side that takes it by agreement (A26.4; ruling R20.3)";
                return false;
            }

            if (named[0].Wants == named[1].Wants)
            {
                var other = sides.First(item => item != named[0].Wants);
                wanted = new BalanceRoll(named[0].Wants!, named[0].Name, named[1].Name, other);
                balance = other;
            }
            else
            {
                players = [.. named.Select(item => new ScenarioPlayer(item.Name, item.Wants!))];
            }
        }

        if (balance is not null && !sides.Contains(balance, StringComparer.Ordinal))
        {
            reason = $"play.balance: '{balance}' is not a side of the card (A26.4; ruling R20.3)";
            return false;
        }

        return true;
    }

    /// <summary>
    /// The start's events with its drs drawn (rulings R20.2, R20.3): a dr for each side, in the card's order and rerolled while they tie, gives the first
    /// move to the higher; a dr for each player who wishes the same side gives it to the higher, the other playing the other side with its Balance.
    /// </summary>
    private List<GameEvent> StartRolled(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> placed, StartRolls rolls,
        Func<RollRequest, RollResult> draw)
    {
        var events = new List<GameEvent>(placed);
        var started = (GameStarted)events[0].Payload;
        var scenario = started.Scenario!;
        int Higher(string purpose)
        {
            for (var attempt = 1; attempt <= 20; attempt++)
            {
                var roll = draw(new RollRequest(2, 6));
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled($"{attemptId}-{purpose}-{attempt}", purpose, 2, 6, roll.Values, DiceRolled.SystemSource, actor), null, null));
                if (roll.Values[0] != roll.Values[1])
                {
                    return roll.Values[0] > roll.Values[1] ? 0 : 1;
                }
            }

            return 0;
        }

        if (rolls.FirstMove is [var one, var two])
        {
            started = started with
            {
                PhasingSide = Higher("first-move") == 0 ? one : two
            };
        }

        if (rolls.Balance is { } balance)
        {
            var firstWins = Higher("balance") == 0;
            scenario = scenario with
            {
                Players = [new ScenarioPlayer(firstWins ? balance.First : balance.Second, balance.Wanted), new ScenarioPlayer(firstWins ? balance.Second : balance.First, balance.Other)],
            };
        }

        events[0] = events[0] with
        {
            Payload = started with
            {
                Scenario = scenario
            }
        };
        return events;
    }

    private bool Start(JsonElement start, out JsonObject? payload, out string label, out string? reason)
    {
        payload = null;
        label = Text(start, "label", out var text) ? text : "Live game";
        reason = null;
        var catalogName = Text(start, "catalog", out var catalogText) ? catalogText : null;
        var catalog = UnitCatalogs.For(catalogs, catalogName);
        if (catalog is not { Publication: CatalogPublication.Published })
        {
            reason = $"play.catalog: a live game uses a published catalog, and '{catalogName}' is not one";
            return false;
        }

        if (!start.TryGetProperty("boards", out var boardList) || boardList.ValueKind != JsonValueKind.Array || boardList.GetArrayLength() == 0)
        {
            reason = "play.boards: a live game names the boards in play";
            return false;
        }

        // Each board is a reference, or an object that places it in a composed map (Composed Maps Design, section 6).
        var placed = new JsonArray();
        var versions = new List<string>();
        var placements = new List<(BoardPlacement Placement, BoardHandle Board)>();
        var objects = boardList.EnumerateArray().Count(item => item.ValueKind == JsonValueKind.Object);
        if (objects != 0 && objects != boardList.GetArrayLength())
        {
            reason = "play.boards: either every board is placed with its slot or none is";
            return false;
        }

        foreach (var item in boardList.EnumerateArray())
        {
            var boardText = item.ValueKind == JsonValueKind.Object ? (Text(item, "board", out var named) ? named : null) : item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (!BoardRef.TryParse(boardText, out var board) || boards.TryGetBoard(board).Board is not { } handle)
            {
                reason = $"play.boards: the board '{boardText ?? item.ToString()}' cannot be read";
                return false;
            }

            if (handle.Status is not (BoardReadStatus.Verified or BoardReadStatus.AuthoredValid))
            {
                reason = $"play.boards: {board} is {handle.Status}, and a live game is played only on verified boards (ASL-MAP-044)";
                return false;
            }

            var node = new JsonObject { ["board"] = board.Value, ["version"] = handle.Version };
            if (item.ValueKind == JsonValueKind.Object)
            {
                if (!item.TryGetProperty("column", out var column) || !column.TryGetInt32(out var slotColumn)
                    || !item.TryGetProperty("row", out var row) || !row.TryGetInt32(out var slotRow))
                {
                    reason = $"play.boards: {board} needs a whole-number column and row";
                    return false;
                }

                var reversed = item.TryGetProperty("reversed", out var flag) && flag.ValueKind == JsonValueKind.True;
                node["column"] = slotColumn;
                node["row"] = slotRow;
                if (reversed)
                {
                    node["reversed"] = true;
                }

                placements.Add((new BoardPlacement(board, slotColumn, slotRow, reversed, []), handle));
            }

            placed.Add(node);
            versions.Add(handle.Version);
        }

        string reference;
        if (placements.Count > 0)
        {
            var composed = ComposedMapRead.Create(placements);
            if (composed.Read is null)
            {
                reason = $"play.boards: the boards cannot be laid out: {string.Join("; ", composed.Diagnostics.Select(item => $"{item.Code} {item.Message}"))}";
                return false;
            }

            reference = Text(start, "map", out var savedMap) ? savedMap
                : string.Join(' ', placements.Select(item => $"{item.Placement.Board.Value}@{item.Placement.Column},{item.Placement.Row}{(item.Placement.Reversed ? "/r" : string.Empty)}"));
        }
        else
        {
            reference = string.Join('+', placed.Select(item => (string)item!["board"]!));
        }

        if (!start.TryGetProperty("sides", out var sides) || !Text(start, "firstSide", out var firstSide))
        {
            reason = "play.sides: a live game names its sides and the side that moves first";
            return false;
        }

        payload = new JsonObject
        {
            ["sides"] = JsonNode.Parse(sides.GetRawText()),
            ["map"] = new JsonObject
            {
                ["reference"] = reference,
                ["version"] = string.Join('+', versions),
                ["boards"] = placed,
            },
            ["catalog"] = catalogName,
            ["turn"] = 1,
            ["phase"] = Phases.All[0],
            ["phasingSide"] = firstSide,
            ["synthetic"] = false,
            ["specialRules"] = start.TryGetProperty("specialRules", out var rules) ? JsonNode.Parse(rules.GetRawText()) : new JsonArray(),
        };
        if (start.TryGetProperty("scenarioMonth", out var month))
        {
            // B15: grain is a Hindrance June to September, so fire through grain needs the month.
            payload["scenarioMonth"] = JsonNode.Parse(month.GetRawText());
        }

        if (start.TryGetProperty("scenarioDefender", out var defender) && defender.ValueKind == JsonValueKind.String)
        {
            // C6.41 (ruling R8.8): the Scenario Defender may Bore Sight.
            payload["scenarioDefender"] = defender.GetString();
        }

        if (start.TryGetProperty("scenarioYear", out var year))
        {
            // C8.1, C8.3 (ruling R7.6): Special Ammunition is available by year.
            payload["scenarioYear"] = JsonNode.Parse(year.GetRawText());
        }

        // Backlog pass 18 (ruling R18.2): the scenario card the game starts from.
        if (start.TryGetProperty("scenario", out var scenario) && scenario.ValueKind == JsonValueKind.Object)
        {
            payload["scenario"] = JsonNode.Parse(scenario.GetRawText());
        }

        // E1.1, E3 (backlog pass 16, rulings R16.1, R16.9): the night and weather SSRs.
        if (NightAndWeatherRulesBar(start) is { } weatherBar)
        {
            reason = weatherBar;
            return false;
        }

        return true;
    }

    /// <summary>Reads proposed events through the game record reader, so they meet exactly the rules a stored log meets.</summary>
    private (IReadOnlyList<GameEvent>? Events, IReadOnlyList<string> Reasons) Parse(GameScope scope, JsonArray events, string attemptId, long expected)
    {
        var index = 0;
        foreach (var item in events)
        {
            index++;
            item!["eventId"] = EventId(attemptId, index);
            item["revision"] = expected + index;
            item["time"] = clock.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
            item["source"] = LiveGames.Source;
        }

        var document = new JsonObject
        {
            ["schemaVersion"] = GameEventReader.SchemaVersion,
            ["tenant"] = scope.Tenant.ToString("D"),
            ["game"] = scope.Game,
            ["label"] = scope.Game,
            ["synthetic"] = false,
            ["events"] = events,
        };
        var read = GameEventReader.Read(System.Text.Encoding.UTF8.GetBytes(document.ToJsonString()));
        return read.Record is { } record ? (record.Events, []) : (null, [.. read.Diagnostics.Select(item => item.ToString())]);
    }

    private GameEvent Event(GameScope scope, string attemptId, int index, long expected, string type, EventPayload payload, string? rulePackage,
        IReadOnlyList<string>? visibility, IReadOnlyList<string>? causes = null) =>
        new(scope, EventId(attemptId, index), expected + index, clock.GetUtcNow(), LiveGames.Source, type, payload, rulePackage, causes ?? [], visibility);

    private static JsonObject EventNode(int index, string type, JsonObject payload, IReadOnlyList<string>? visibility)
    {
        var node = new JsonObject { ["type"] = type, ["payload"] = payload };
        node["visibility"] = visibility is null ? "all" : new JsonArray([.. visibility.Select(side => (JsonNode)side)]);
        return node;
    }

    /// <summary>The composed read of a placed map at the versions in play, or null when the map is not placed or a board cannot be read.</summary>
    private ComposedMapRead? Composed(GameState state)
    {
        if (!state.Map.IsPlaced)
        {
            return null;
        }

        var placed = new List<(BoardPlacement, BoardHandle)>();
        foreach (var placement in state.Map.Placements())
        {
            if (boards.TryGetBoard(placement.Board, state.Map.Board(placement.Board)!.Version).Board is not { } handle)
            {
                return null;
            }

            placed.Add((placement, handle));
        }

        return ComposedMapRead.Create(placed).Read;
    }

    private List<(BoardRef, string, HexFactSet)>? BoardsOf(IReadOnlyList<GameEvent> events)
    {
        if (events.Count == 0 || events[0].Payload is not GameStarted started)
        {
            return null;
        }

        var loaded = new List<(BoardRef, string, HexFactSet)>();
        foreach (var placed in started.Map.Boards)
        {
            if (boards.TryGetBoard(placed.Board, placed.Version).Board is not { } handle)
            {
                return null;
            }

            loaded.Add((handle.Ref, handle.Version, handle.Facts));
        }

        return loaded;
    }

    private HexFactLocationChains? Chains(IReadOnlyList<GameEvent> events) => BoardsOf(events) is { } loaded ? new HexFactLocationChains(loaded) : null;

    private static bool IsTrue(JsonElement placement, string condition) =>
        placement.TryGetProperty("conditions", out var conditions) && conditions.ValueKind == JsonValueKind.Object
        && conditions.TryGetProperty(condition, out var value) && value.ValueKind == JsonValueKind.True;

    private static string EventId(string attemptId, int index) => $"{attemptId}-{index.ToString(CultureInfo.InvariantCulture)}";

    private static bool Common(JsonElement arguments, out string gameId, out string attemptId, out long expected)
    {
        expected = 0;
        attemptId = string.Empty;
        return Text(arguments, "gameId", out gameId) && Text(arguments, "attemptId", out attemptId)
            && arguments.TryGetProperty("expectedRevision", out var revision) && revision.ValueKind == JsonValueKind.Number
            && revision.TryGetInt64(out expected) && expected >= 0;
    }

    private static bool Text(JsonElement value, string name, out string text)
    {
        text = string.Empty;
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            return false;
        }

        text = property.GetString()!;
        return true;
    }

    private static GamePlan Refused(GameScope scope, string label, long expected, params string[] reasons) =>
        new(GamePlanStatus.Refused, scope, label, expected, [], reasons);
}
