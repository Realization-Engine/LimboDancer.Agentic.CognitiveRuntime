using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

public sealed partial class GamePlanner
{
    private TurnAtmosphereSnapshot ReadAtmosphere(GameHistory history, long revision, Perspective perspective)
    {
        var visible = ViewOf(history, revision, perspective);
        var events = history.Events.Where(item => item.Revision <= revision).ToArray();
        var lastBoundary = events.Where(item => item.Payload is PhaseChanged).Select(item => item.Revision).DefaultIfEmpty(0).Max();
        var grenades = new List<TurnGrenadeFacts>();
        var wind = new List<TurnWindHistory>();
        var classified = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in events.Where(item => item.IsVisibleTo(perspective)))
        {
            var at = history.At(item.Revision)!;
            if (item.Payload is WindChanged change)
            {
                wind.Add(new(at.Turn, at.PhasingSide, at.Phase, change.Gust));
            }
            if (item.Payload is not MovementStepped { Smoke: { } smoke })
            {
                continue;
            }
            var before = history.At(item.Revision - 1)!;
            var unit = before.Unit(smoke.Unit);
            var view = ViewOf(history, item.Revision - 1, perspective);
            if (unit is null || (!perspective.IsAdjudicator &&
                !view.Units.Any(candidate => candidate.Id == unit.Id && candidate.Side == perspective.Name)))
            {
                continue;
            }

            // Bind the counter to its actual causal attempt, never just its name or Location.
            var created = events.FirstOrDefault(candidate => candidate.Payload is InstanceCreated creation
                && candidate.Revision == item.Revision + 1 && candidate.Causes.Contains(item.EventId)
                && creation.Instance.Kind == "asl:smoke" && creation.Instance.Id.EndsWith(GameState.SmokeGrenadeSuffix, StringComparison.Ordinal)
                && creation.Instance.Position is MapPosition position && position.Location == smoke.Target);
            string? counter = null;
            bool? active = null;
            var cleaned = false;
            if (created is not null && created.IsVisibleTo(perspective))
            {
                var id = ((InstanceCreated)created.Payload).Instance.Id;
                if (ViewOf(history, created.Revision, perspective).Entities.Any(entity => entity.Id == id))
                {
                    counter = id;
                    classified.Add(id);
                    active = visible.Entities.Any(entity => entity.Id == id && entity.Status == InstanceStatus.Active);
                    cleaned = events.Any(boundary => boundary.Revision > created.Revision && boundary.Payload is PhaseChanged
                        && boundary.IsVisibleTo(perspective)
                        && ScenarioA1SequenceProjection.RemovesSmokeGrenades(history.At(boundary.Revision - 1)!.Phase)
                        && history.At(boundary.Revision - 1)!.Find(id)?.Status == InstanceStatus.Active
                        && history.At(boundary.Revision)!.Find(id)?.Status == InstanceStatus.Eliminated);
                }
            }
            grenades.Add(new(unit.Id, unit.Side, before.Turn, before.Phase, smoke.Placed, counter, active, cleaned, item.Revision > lastBoundary));
        }
        return TurnAtmosphere.Compose([.. grenades], [.. wind],
            [.. visible.Entities.Where(entity => entity.Kind == "asl:smoke" && !classified.Contains(entity.Id))
                .Select(entity => entity.Id).Order(StringComparer.Ordinal)]);
    }
}
