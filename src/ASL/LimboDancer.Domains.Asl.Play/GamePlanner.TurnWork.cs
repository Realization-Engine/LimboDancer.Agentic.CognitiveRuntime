using LimboDancer.Dice;
using System.Collections.Immutable;
using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

public sealed partial class GamePlanner
{
    private static readonly string[] PackedGunArguments = ["gunId", "equipmentId", "pushGun"];

    private static TurnWorkWindow TurnWorkWindowOf(GameState state) => TurnWorkRules.Window(state.Phase, state.PhasingSide == state.FirstSide);

    private GamePlan PlanTurnWork(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing,
        string attemptId, long expected, string label, string actor)
    {
        var state = Replay(existing).Current;
        if (state is null || !Text(arguments, "workId", out var id) || !Text(arguments, "operation", out var operation)
            || !Text(arguments, "ruleBasis", out var basis) || string.IsNullOrWhiteSpace(basis))
        {
            return Refused(scope, label, expected, "play.work: a work adjudication needs a game, identity, operation and rule basis");
        }
        TurnWorkDeclaration? declaration = null;
        try
        {
            if (arguments.TryGetProperty("declaration", out var node))
            {
                declaration = node.Deserialize<TurnWorkDeclaration>(JsonSerializerOptions.Web);
            }
        }
        catch (JsonException)
        {
            return Refused(scope, label, expected, "play.work: invalid declaration");
        }
        var held = state.TurnWork.SingleOrDefault(item => item.Declaration.Id == id);
        var owner = declaration?.Owner ?? held?.Declaration.Owner;
        if (owner is null || state.Side(owner) is null || ProposedBy(arguments) is { } side && side != owner)
        {
            return Refused(scope, label, expected, "play.work: the work belongs to another or unknown side");
        }
        var effects = declaration?.CompletionEffects is not null || held?.Declaration.CompletionEffects is not null;
        var audited = operation == "start" || held?.HostAuditVersion == 1;
        if (!audited && operation != "cancel")
        {
            return Refused(scope, label, expected, "play.work: legacy work is read/cancel only; cancel and redeclare under the audited contract without importing old progress");
        }
        var eventType = audited ? TurnWorkRecorded.AuditedType : effects ? TurnWorkRecorded.EffectsType : TurnWorkRecorded.LifecycleType;
        var package = audited ? "asl.turn-work.v2" : effects ? "asl.turn-work-effects.v1" : "asl.turn-work.v1";
        var needsRoll = operation == "complete" && held is not null && TurnWorkLifecycle.RequiresAmmunitionDr(held.Declaration.Kind);
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult>? draw)
        {
            var events = new List<GameEvent>();
            string? rollId = null;
            if (needsRoll)
            {
                rollId = attemptId + "-work-roll";
                var values = draw is null ? new[] { 1, 1 } : draw(new(2, 6)).Values;
                events.Add(Event(scope, attemptId, 1, expected, "dice-rolled",
                    new DiceRolled(rollId, "work-ammunition", 2, 6, values, DiceRolled.SystemSource, actor), package, [owner]));
            }
            events.Add(Event(scope, attemptId, events.Count + 1, expected, eventType,
                new TurnWorkRecorded(id, operation, declaration, rollId, basis), package, [owner]));
            return events;
        }
        var probe = Build(null);
        var projected = Replay([.. existing, .. probe]);
        if (projected.HasErrors || projected.Current?.Revision != expected + probe.Count)
        {
            return Refused(scope, label, expected, "play.work: invalid work transition or participant evidence");
        }
        return new(GamePlanStatus.Ready, scope, label, expected, needsRoll ? [] : probe,
            ["play.work: externally adjudicated work; packing/supply rule automation remains pending"])
        {
            FirstEventId = EventId(attemptId, 1),
            Roll = needsRoll ? new PlannedRoll("work-ammunition", draw => Build(draw)) : null,
        };
    }

    private ImmutableArray<TurnWorkDisclosure> ReadWork(GameHistory history, long revision, Perspective perspective)
    {
        var state = history.At(revision)!;
        var visible = ViewOf(history, revision, perspective);
        var ids = visible.Units.Select(item => item.Id).Concat(visible.Equipment.Select(item => item.Id)).ToHashSet(StringComparer.Ordinal);
        return [.. state.TurnWork.Where(item => perspective.IsAdjudicator || item.Declaration.Owner == perspective.Name
            && item.Declaration.Participants.All(ids.Contains)
            && history.Events.Where(e => e.Revision <= revision && e.Payload is TurnWorkRecorded record && record.WorkId == item.Declaration.Id)
                .All(e => e.IsVisibleTo(perspective) && (e.Payload is not TurnWorkRecorded { Roll: { } rollId }
                    || history.Events.Any(roll => roll.Revision <= revision && roll.Payload is DiceRolled dice && dice.Roll == rollId && roll.IsVisibleTo(perspective)))))
            .Select(item => TurnWorkDisclosure.From(item, TurnWorkLifecycle.CompletionDue(item, state.Turn, TurnWorkWindowOf(state))))];
    }
}
