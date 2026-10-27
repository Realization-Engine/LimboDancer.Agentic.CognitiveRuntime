using System.Collections.Immutable;
using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

public sealed record TurnHostObligation(string DefinitionId, string SubjectId, TurnObligationState State,
    TurnRuleDeadline? Deadline, TurnSupport CommandSupport);
public sealed record TurnHostGuide(TurnGuideSnapshot Guide, ImmutableArray<TurnHostObligation> Obligations,
    TurnPhaseCloseAssessment PhaseClose)
{
    public TurnGuideProfile Profile { get; init; } = TurnGuideProfiles.Get("existing-engine");
    public ImmutableArray<string> WorkProgressGaps { get; init; } = [];
    public TurnAtmosphereSnapshot? Atmosphere
    {
        get; init;
    }
    public TurnFireHistorySnapshot? FireHistory
    {
        get; init;
    }
}

public sealed partial class GamePlanner
{
    private static readonly TurnGuideCatalog GuideCatalog = TurnGuideCatalog.Load();
    private static readonly TurnGuideService GuideReader = new(GuideCatalog, []);

    /// <summary>
    /// Server-side read. The caller must supply its authenticated perspective, as for ViewOf; never bind an untrusted
    /// client-selected adjudicator name. No raw history, revision, candidate IDs, plans or roll delegates leave this API.
    /// </summary>
    public async Task<TurnHostGuide> ReadTurnGuideAsync(GameScope scope, Perspective perspective, string browsedPhase,
        long? historicalRevision = null, CancellationToken cancellationToken = default)
        => await ReadTurnGuideProfileAsync(scope, perspective, browsedPhase, "existing-engine", historicalRevision, cancellationToken).ConfigureAwait(false);

    public async Task<TurnHostGuide> ReadTurnGuideProfileAsync(GameScope scope, Perspective perspective, string browsedPhase,
        string profileId, long? historicalRevision = null, CancellationToken cancellationToken = default)
    {
        var profile = TurnGuideProfiles.Get(profileId);
        var record = store.Read(scope) ?? throw new ArgumentException("Game not found.", nameof(scope));
        var history = Replay(record.Events);
        var revision = historicalRevision ?? record.Events.Count;
        var state = history.At(revision) ?? throw new ArgumentOutOfRangeException(nameof(historicalRevision));
        var view = ViewOf(history, revision, perspective);
        var historical = historicalRevision.HasValue;
        var window = state.Phase + ":phase"; // The host does not persist an ASOP substep; never invent one.
        var context = new TurnEvaluationContext(scope.ToString(), perspective.Name, perspective.Name,
            profile.Id, state.Phase, window, state.Stamp + ":" + profile.Version);
        var guide = GuideReader.Describe(new(context, browsedPhase, historical), new(context, []));
        var evidence = TurnHistoryAdapter.Read(history, state.Revision);
        var assessed = new TurnObligationService(GuideCatalog).Assess(evidence.Read, evidence.Candidates);
        var visible = view.Units.Where(unit => perspective.IsAdjudicator || unit.Side == perspective.Name)
            .Select(unit => unit.Id)
            .Concat(view.Equipment.Where(item => perspective.IsAdjudicator || item.Side == perspective.Name).Select(item => item.Id))
            .ToHashSet(StringComparer.Ordinal);
        // Only own visible counters (or adjudicator) and visible origin/outcome evidence are disclosed.
        var events = history.Events.Where(item => item.Revision <= revision).ToDictionary(item => item.EventId, StringComparer.Ordinal);
        var obligations = assessed.Obligations.Where(item => Disclosed(item)
            && events[item.Facts.Key.OriginEventId].IsVisibleTo(perspective)
            && (item.Facts.Outcome is null || events[item.Facts.Outcome.EventId].IsVisibleTo(perspective)))
            .Select(item => new TurnHostObligation(item.Facts.Key.DefinitionId, item.Facts.Key.SubjectId,
                item.Verdict.State, item.Verdict.Deadline, profile.CommandSupport(item.Facts.Key.DefinitionId, item.Facts.Origin is not null)))
            .OrderBy(item => item.SubjectId, StringComparer.Ordinal).ThenBy(item => item.DefinitionId, StringComparer.Ordinal).ToImmutableArray();
        bool Disclosed(TurnObligation item)
        {
            if (perspective.IsAdjudicator)
            {
                return true;
            }
            if (item.Facts.OwnerSideId != perspective.Name)
            {
                return false;
            }
            if (visible.Contains(item.Facts.Key.SubjectId))
            {
                return true;
            }
            // Retain a completed own DC whose identity was disclosed at placement, even after its counter is removed.
            return item.Facts.Key.DefinitionId == "placedDc" && item.Facts.Outcome is not null
                && ViewOf(history, item.Facts.OriginSequence, perspective).Equipment.Any(dc => dc.Id == item.Facts.Key.SubjectId);
        }
        var close = historical
            ? TurnPhaseClose.Describe(new(TurnClosePlanStatus.Ready, false, false, [], []), perspective.IsAdjudicator, true)
            : await AssessTurnPhaseCloseAsync(state.Stamp, perspective, [], cancellationToken).ConfigureAwait(false);
        return new(guide, obligations, close)
        {
            Profile = profile,
            // No durable work declarations exist yet. Return gaps, never fabricate work instances from TI markers.
            WorkProgressGaps = profile.Id == "existing-engine-work-v1"
                ? TurnWorkProgress.Assess(new(scope.ToString(), revision, state.Turn, TurnWorkWindow.Unknown), []).PendingRules : [],
            FireHistory = profile.Id is "existing-engine-fire-v1" or "existing-engine-atmosphere-v1" or "existing-engine-work-v1" ? ReadFireHistory(history, revision, perspective) : null,
            Atmosphere = profile.Id is "existing-engine-atmosphere-v1" or "existing-engine-work-v1" ? ReadAtmosphere(history, revision, perspective) : null,
        };
    }

    /// <summary>Assess the same full planning path used by the gate and executor. Never invokes PlannedRoll.Build or commits.</summary>
    public async Task<TurnPhaseCloseAssessment> AssessTurnPhaseCloseAsync(StateStamp expected, Perspective perspective,
        IReadOnlyList<string> retainDm, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(perspective);
        ArgumentNullException.ThrowIfNull(retainDm);
        var record = store.Read(expected.Scope) ?? throw new ArgumentException("Game not found.", nameof(expected));
        var state = Replay(record.Events).Current ?? throw new InvalidOperationException("Game has no projected state.");
        _ = ViewOf(state, perspective, []); // Existing perspective and disclosure policy, including setup and LOS.
        if (state.Stamp != expected)
        {
            return TurnPhaseClose.Describe(new(TurnClosePlanStatus.Stale, false, false, ["The evaluation is stale."], []), perspective.IsAdjudicator, false);
        }
        var args = JsonSerializer.SerializeToElement(new
        {
            gameId = expected.Scope.Game,
            attemptId = "turn-guide-assessment-" + Guid.NewGuid().ToString("N"),
            expectedRevision = expected.Revision,
            proposedBy = perspective.Name,
            retainDm,
        });
        var plan = await PlanCoreAsync(GameActions.AdvancePhase, args, expected.Scope.Tenant, perspective.Name,
            assessmentOnly: true, cancellationToken).ConfigureAwait(false);
        var status = plan.Status switch
        {
            GamePlanStatus.Ready => TurnClosePlanStatus.Ready,
            GamePlanStatus.Stale => TurnClosePlanStatus.Stale,
            _ => TurnClosePlanStatus.Refused,
        };
        var advances = plan.Roll is not null || plan.Events.Any(item => item.Payload is PhaseChanged or GameEnded);
        var effects = plan.Events.Select(item => item.Type).ToImmutableArray();
        // The projector still owns its implicit cleanup. The guide never constructs replacement cleanup events.
        return TurnPhaseClose.Describe(new(status, advances, plan.Roll is not null, [.. plan.Reasons], effects), perspective.IsAdjudicator, false);
    }
}

internal sealed record TurnHistoryEvidence(TurnObligationRead Read, ImmutableArray<TurnObligationCandidate> Candidates);

/// <summary>Derives Stun/CX and the admitted Placed DC history from actual events; other families remain pending.</summary>
internal static class TurnHistoryAdapter
{
    public static TurnHistoryEvidence Read(GameHistory history, long revision)
    {
        var current = history.At(revision) ?? throw new ArgumentOutOfRangeException(nameof(revision));
        var instances = new List<TurnObligationCandidate>();
        var open = new Dictionary<(string Unit, string Definition), int>();
        GameState? previous = null;
        var playerTurn = 1;
        var dcAttacks = new HashSet<string>(StringComparer.Ordinal);
        var events = history.Events.ToDictionary(item => item.Revision);
        TurnRulePosition position = new(current.Turn, playerTurn, current.PhasingSide, current.Phase, TurnCheckpoint.DuringPhase);
        foreach (var state in history.States.Where(item => item.Revision <= revision))
        {
            if (previous is not null && previous.PhasingSide != state.PhasingSide)
            {
                playerTurn++;
            }

            position = new(state.Turn, playerTurn, state.PhasingSide, state.Phase, TurnCheckpoint.DuringPhase);
            var origin = events[state.Revision];
            if (origin.Payload is MovementStepped { DcPlacement: { } dc })
            {
                var owner = dc.OwnerSide ?? state.Unit(dc.Unit)?.Side ?? "unknown";
                var key = (dc.Charge, "placedDc");
                open.Add(key, instances.Count);
                dcAttacks.Remove(dc.Charge);
                instances.Add(new(new(current.Scope.ToString(), origin.EventId, "placedDc", dc.Charge, 0), origin.Revision,
                    owner, owner, dc.OwnerSide is null ? null : position,
                    new(true, false, false, false, false, false, false, null), [], null, []));
            }
            if (origin.Payload is FireResolved fire && fire.Facts.TryGetProperty("demolitionCharge", out var attack)
                && attack.ValueKind == JsonValueKind.Object && attack.TryGetProperty("mode", out var mode)
                && mode.GetString() == "placed" && attack.TryGetProperty("equipmentId", out var equipment))
            {
                dcAttacks.Add(equipment.GetString()!);
            }
            foreach (var key in open.Keys.Where(key => key.Definition == "placedDc").ToArray())
            {
                var index = open[key];
                var item = instances[index];
                var charge = state.PlacedCharges.FirstOrDefault(charge => charge.Charge == key.Unit);
                if (charge is null)
                {
                    var outcome = origin.Payload is InstanceEliminated removed && removed.Id == key.Unit && dcAttacks.Contains(key.Unit)
                        ? TurnRecordedOutcome.Resolved : TurnRecordedOutcome.Interrupted;
                    item = item with
                    {
                        Outcome = new(outcome, origin.EventId, origin.Revision)
                    };
                    open.Remove(key);
                }
                else
                {
                    item = item with
                    {
                        Subject = item.Subject with
                        {
                            PlacedDcOperable = charge.Operable
                        }
                    };
                }
                instances[index] = item;
            }
            foreach (var unit in state.Units)
            {
                var subject = new TurnObligationSubject(unit.Status == InstanceStatus.Active, Has(unit, Conditions.Stunned),
                    Has(unit, Conditions.Recalled), Has(unit, Conditions.StunRecovery), Has(unit, Conditions.Cx),
                    Has(unit, Conditions.Broken), Has(unit, Conditions.Berserk), null);
                foreach (var definition in new[] { "stun", "cx" })
                {
                    var pending = subject.Active && (definition == "cx" ? subject.Cx :
                        ScenarioA1SequenceProjection.StunEnds(subject.Active, subject.Recalled, subject.StunRecovery, subject.Stunned).Conditions.Count > 0);
                    var key = (unit.Id, definition);
                    if (open.TryGetValue(key, out var index))
                    {
                        var item = instances[index] with
                        {
                            Subject = subject
                        };
                        if (!pending)
                        {
                            item = item with
                            {
                                Outcome = new(subject.Active ? TurnRecordedOutcome.Resolved : TurnRecordedOutcome.Cancelled, origin.EventId, origin.Revision)
                            };
                            open.Remove(key);
                        }
                        instances[index] = item;
                    }
                    else if (pending)
                    {
                        open.Add(key, instances.Count);
                        instances.Add(new(new(current.Scope.ToString(), origin.EventId, definition, unit.Id, 0), origin.Revision,
                            unit.Side, unit.Side, GameState.IsSetupEvent(origin.Payload) ? null : position, subject, [], null, []));
                    }
                }
            }
            previous = state;
        }
        return new(new(current.Scope.ToString(), revision, position), [.. instances]);
    }

    private static bool Has(UnitInstance unit, string condition) => GameState.Condition(unit, condition) == ConditionState.True;
}
