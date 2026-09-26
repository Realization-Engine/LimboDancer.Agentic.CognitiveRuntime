using LimboDancer.Dice;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.ScenarioA1;
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
public sealed class GamePlanner(IGameStore store, IBoardCatalog boards, UnitVocabulary vocabulary, IReadOnlyList<UnitCatalog> catalogs,
    TimeProvider? time = null)
{
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

    private static readonly HashSet<string> OrdinaryBuildings = new(
        from material in new[] { "Wooden", "Stone" }
        from suffix in new[] { string.Empty, ", 1 Level", ", 2 Level", ", 3 Level", ", 4 Level" }
        select $"{material} Building{suffix}",
        StringComparer.Ordinal);

    private readonly TimeProvider clock = time ?? TimeProvider.System;

    /// <summary>Replays a live game's events against the exact boards in play; refuses when a board cannot be read.</summary>
    public GameHistory Replay(IReadOnlyList<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return GameProjector.Project(events, vocabulary, catalogs, Chains(events), LiveGames.Sources);
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

        var plan = action.Id.Value switch
        {
            "asl.game.setup" => PlanSetup(scope, arguments, existing, attemptId, expected, ref label),
            "asl.game.advance-phase" => PlanAdvance(scope, existing, attemptId, expected, label),
            "asl.game.enter-empty-building" => await PlanEntryAsync(scope, arguments, existing, attemptId, expected, label, cancellationToken),
            "asl.game.declare-overrun" => await PlanDeclareAsync(scope, arguments, existing, attemptId, expected, label, cancellationToken),
            "asl.game.enter-building" => await PlanEnterBuildingAsync(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown", cancellationToken),
            _ => Refused(scope, label, expected, "play.unknown-action"),
        };

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

    private GamePlan PlanSetup(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, ref string label)
    {
        if (existing.Any(item => item.Payload is not (GameStarted or InstanceCreated)))
        {
            return Refused(scope, label, expected, "play.setup-closed: play has started, so no more units can be set up");
        }

        var events = new JsonArray();
        var sides = new Dictionary<string, string>(StringComparer.Ordinal);
        if (existing.Count == 0)
        {
            if (!arguments.TryGetProperty("start", out var start) || start.ValueKind != JsonValueKind.Object)
            {
                return Refused(scope, label, expected, "play.start-required: a new game needs its sides, boards, and catalog");
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
            var hidden = IsTrue(placement, Conditions.Hidden) || IsTrue(placement, Conditions.Concealed);
            var side = placement.TryGetProperty("side", out var sideElement) && sideElement.ValueKind == JsonValueKind.String ? sideElement.GetString() : null;
            events.Add(EventNode(events.Count + 1, "instance-created", new JsonObject { ["instance"] = node }, hidden && side is not null ? [side] : null));
        }

        if (events.Count == 0)
        {
            return Refused(scope, label, expected, "play.nothing-to-set-up");
        }

        var parsed = Parse(scope, events, attemptId, expected);
        return parsed.Events is { } list
            ? new GamePlan(GamePlanStatus.Ready, scope, label, expected, list, [$"play.setup: {list.Count} event(s)"])
            : Refused(scope, label, expected, [.. parsed.Reasons]);
    }

    private GamePlan PlanAdvance(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (state.Sides.Count != 2)
        {
            return Refused(scope, label, expected, "play.two-sides: the sequence of play alternates two sides");
        }

        if (state.OpenAttempts.Count > 0)
        {
            return Refused(scope, label, expected, $"play.declaration-pending: the entry attempt '{state.OpenAttempts[0].EventId}' by "
                + $"{state.OpenAttempts[0].Unit} awaits the attacker's OVR declaration (A12.15, p. 78), so the phase cannot advance");
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

        var payload = new PhaseChanged(turn, phase, phasing);
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "phase-changed", payload, rulePackage: null, visibility: null)],
            [$"play.advance: turn {turn}, {phase}, {phasing} phasing"]);
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
                    IReadOnlyList<GameEvent> Build(RollResult roll)
                    {
                        var events = Prefix();
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
                        Roll = new PlannedRoll("random-selection", new RollRequest(order.Length, 6), Build),
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
        long expected, string label, CancellationToken cancellationToken)
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
        if (choice == "elect")
        {
            return Refused(scope, label, expected, EntryDisclosure.CannotResolve
                + " (an elected Infantry OVR needs its NTC and what follows it, which step 10 reviews)");
        }

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
        var handle = state.Map.Board(target.Board) is { } placed ? boards.TryGetBoard(target.Board, placed.Version).Board : null;
        var targetRead = handle?.Resolve(target).Read;
        var fromRead = from is not null && handle is not null && from.Location.Board == target.Board ? handle.Resolve(from.Location).Read : null;
        var definitive = targetRead is { IsDefinitive: true } && fromRead is { IsDefinitive: true };
        var adjacent = definitive && handle!.Distance(from!.Location.Hex, target.Hex) == 1;

        bool? And(params bool?[] values) => values.Any(value => value == false) ? false : values.All(value => value == true) ? true : null;

        var known = Known(Conditions.Concealed) is { } concealed && Known(Conditions.Hidden) is { } hidden ? !concealed && !hidden : (bool?)null;
        var squad = vocabulary.IsA(unit.Kind, "asl:squad");
        var occupants = state.At(target).Where(item => item.Id != unit.Id).ToArray();
        HexsideFacts? crossed = null;
        if (adjacent)
        {
            var side = Enum.GetValues<HexsideDirection>().FirstOrDefault(direction => handle!.Neighbor(from!.Location.Hex, direction) == target.Hex);
            crossed = fromRead!.Hex.Hexsides.FirstOrDefault(item => item.Side == side);
        }

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

    private bool Start(JsonElement start, out JsonObject? payload, out string label, out string? reason)
    {
        payload = null;
        label = Text(start, "label", out var text) ? text : "Live game";
        reason = null;
        var catalogName = Text(start, "catalog", out var catalogText) ? catalogText : null;
        var catalog = catalogs.FirstOrDefault(item => $"{item.Identity.Catalog}@{item.Identity.Version}" == catalogName);
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

        var placed = new JsonArray();
        var versions = new List<string>();
        foreach (var item in boardList.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !BoardRef.TryParse(item.GetString(), out var board)
                || boards.TryGetBoard(board).Board is not { } handle)
            {
                reason = $"play.boards: the board '{item}' cannot be read";
                return false;
            }

            if (handle.Status is not (BoardReadStatus.Verified or BoardReadStatus.AuthoredValid))
            {
                reason = $"play.boards: {board} is {handle.Status}, and a live game is played only on verified boards (ASL-MAP-044)";
                return false;
            }

            placed.Add(new JsonObject { ["board"] = board.Value, ["version"] = handle.Version });
            versions.Add(handle.Version);
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
                ["reference"] = string.Join('+', placed.Select(item => (string)item!["board"]!)),
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
