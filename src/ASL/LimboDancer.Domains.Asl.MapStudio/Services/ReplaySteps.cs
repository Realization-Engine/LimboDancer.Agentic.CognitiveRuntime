using System.Globalization;
using System.Text.Json;
using LimboDancer.Domains.Asl.MapStudio.Components.Games;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>How much of a step a view reads (pass 31b, D6): every event of it, or only some.</summary>
public enum ReplayRead
{
    Full,
    Part,

    /// <summary>The view reads none of the step's events, and sees something change on the map: a "?" placed where a hidden unit stood (A12.32).</summary>
    None,
}

/// <summary>A fire a step made, as the two Locations both sides know (A6.1): where it came from and where it went.</summary>
public sealed record ReplayFire(BoardLocation From, BoardLocation To);

/// <summary>
/// One step of a game's replay (pass 31b, D2): every event of one confirmed proposal. <see cref="Number"/> counts the steps of the view it was read
/// for, from 1; <see cref="First"/> and <see cref="Last"/> are the revisions it covers; <see cref="Turn"/>, <see cref="Phase"/>, and
/// <see cref="PhasingSide"/> are those the step was taken in; <see cref="Title"/> says it in words that name no unit.
/// </summary>
public sealed record ReplayStep(int Number, long First, long Last, string Attempt, string Kind, int Turn, string Phase, string PhasingSide, string? ActingSide,
    string Title, ReplayRead Read)
{
    /// <summary>The phase of a step taken before play starts.</summary>
    public const string SetupPhase = "setup";

    /// <summary>The fire the step made, for the map's lines.</summary>
    public IReadOnlyList<ReplayFire> Fires { get; init; } = [];

    /// <summary>The Locations the step's readable events name, for the map to bring into view.</summary>
    public IReadOnlyList<BoardLocation> Locations { get; init; } = [];

    /// <summary>Whether the step ends the game.</summary>
    public bool EndsGame
    {
        get; init;
    }

    public bool IsSetup => Phase == SetupPhase;
}

/// <summary>A run of steps taken in one phase of one Player Turn, or before play: its first step's number and how many steps it holds.</summary>
public sealed record ReplayPhase(int Turn, string PhasingSide, string Phase, int FirstStep, int Count)
{
    public string Label => Phase == ReplayStep.SetupPhase ? "Setup" : $"{DisplayText.Side(PhasingSide)} {GameText.PhaseLabel(Phase)}";
}

/// <summary>A view's steps of a game, in order, with their phases; the jumps of the transport are read from it (pass 31b, D5).</summary>
public sealed record ReplayTimeline(IReadOnlyList<ReplayStep> Steps, IReadOnlyList<ReplayPhase> Phases)
{
    public static ReplayTimeline Empty { get; } = new([], []);

    /// <summary>The step with a number, clamped to the steps there are; null when there is none.</summary>
    public ReplayStep? Step(int number) => Steps.Count == 0 ? null : Steps[Math.Clamp(number, 1, Steps.Count) - 1];

    /// <summary>The number of the last step at or before a revision; 1 when the revision is before the first.</summary>
    public int StepAt(long revision) => Math.Max(1, Steps.Count(step => step.First <= revision));

    public ReplayPhase? PhaseOf(int number) => Phases.LastOrDefault(phase => phase.FirstStep <= number);

    /// <summary>The first step of the next phase; the last step when there is no later phase.</summary>
    public int NextPhase(int number) => Phases.FirstOrDefault(phase => phase.FirstStep > number)?.FirstStep ?? Steps.Count;

    /// <summary>The first step of this phase, or of the phase before when the step is already its phase's first.</summary>
    public int PreviousPhase(int number) => Phases.LastOrDefault(phase => phase.FirstStep < number)?.FirstStep ?? Math.Min(1, Steps.Count);

    /// <summary>The first step of the next Game Turn (setup counts as the turn before the first); the last step when there is none.</summary>
    public int NextTurn(int number)
    {
        var now = TurnOf(Step(number));
        return Steps.FirstOrDefault(step => step.Number > number && TurnOf(step) > now)?.Number ?? Steps.Count;
    }

    /// <summary>The first step of this Game Turn, or of the turn before when the step is already its turn's first.</summary>
    public int PreviousTurn(int number)
    {
        var earlier = Steps.Where(step => step.Number < number).ToArray();
        if (earlier.Length == 0)
        {
            return Math.Min(1, Steps.Count);
        }

        var turn = TurnOf(earlier[^1]);
        return earlier.First(step => TurnOf(step) == turn).Number;
    }

    private static int TurnOf(ReplayStep? step) => step is null || step.IsSetup ? 0 : step.Turn;
}

/// <summary>
/// A game's events grouped into the steps of its replay (pass 31b, D2 and D6; ruling R31b.1). A step is one attempt: the events whose ids are the
/// attempt's id and a number, which is one confirmed proposal with everything the gate added to it. A view's steps are read only from the events that
/// view is entitled to: a step with none of them is left out, so a side's count and titles say nothing of what the other side did out of its sight, and
/// a step read in part is worded from its readable events alone. Titles name sides and Locations, never a unit.
/// </summary>
public static class ReplaySteps
{
    /// <summary>The attempt an event belongs to: its id without the event's number.</summary>
    public static string AttemptOf(string eventId)
    {
        ArgumentNullException.ThrowIfNull(eventId);
        return eventId.LastIndexOf('-') is > 0 and var at ? eventId[..at] : eventId;
    }

    /// <summary>
    /// The steps <paramref name="view"/> may read of the game through its own revision. The view is the perspective's at the last revision shown, so
    /// its events are what the perspective is entitled to now (A12.12: a setup still out of its sight has no step).
    /// </summary>
    /// <remarks>
    /// With <paramref name="viewAt"/>, the perspective's view at a revision, an attempt the view reads nothing of is still a step when the view sees
    /// the map change over it: at the table the other side would see the "?" placed, though not what it is. An attempt that changes nothing the view
    /// sees stays out, so the count says nothing of it.
    /// </remarks>
    public static ReplayTimeline Read(GameHistory history, GameView view, Func<long, GameView>? viewAt = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(view);
        var entitled = view.Events.Select(item => item.EventId).ToHashSet(StringComparer.Ordinal);
        var steps = new List<ReplayStep>();
        var index = 0;
        var events = history.Events;
        while (index < events.Count && events[index].Revision <= view.Stamp.Revision)
        {
            var attempt = AttemptOf(events[index].EventId);
            var start = index;
            while (index < events.Count && events[index].Revision <= view.Stamp.Revision && AttemptOf(events[index].EventId) == attempt)
            {
                index++;
            }

            GameEvent[] all = [.. events.Skip(start).Take(index - start)];
            GameEvent[] readable = [.. all.Where(item => entitled.Contains(item.EventId))];
            // The phase the step was proposed in: the state before it, or the start's own state.
            var before = history.At(all[0].Revision - 1);
            if ((before ?? history.At(all[0].Revision)) is not { } at)
            {
                continue;
            }

            if (readable.Length == 0)
            {
                if (viewAt is not null && all[0].Revision > 1 && ReplayDiff.Of(viewAt(all[0].Revision - 1), viewAt(all[^1].Revision), []) is { Count: > 0 } seen)
                {
                    steps.Add(new ReplayStep(steps.Count + 1, all[0].Revision, all[^1].Revision, attempt, "unread", at.Turn,
                        all.All(item => GameState.IsSetupEvent(item.Payload)) ? ReplayStep.SetupPhase : at.Phase, at.PhasingSide, null,
                        "The other side acts where this view sees only the map change", ReplayRead.None)
                    {
                        Locations = [.. seen.Select(change => change.At).OfType<BoardLocation>().Distinct()],
                    });
                }

                continue;
            }

            var setup = all.All(item => GameState.IsSetupEvent(item.Payload));
            var (kind, acting, title) = Describe(readable, at, view.Perspective);
            steps.Add(new ReplayStep(steps.Count + 1, all[0].Revision, all[^1].Revision, attempt, kind, at.Turn, setup ? ReplayStep.SetupPhase : at.Phase, at.PhasingSide,
                acting, title, readable.Length == all.Length ? ReplayRead.Full : ReplayRead.Part)
            {
                Fires = [.. FiresOf(readable)],
                Locations = [.. LocationsOf(readable, at).Distinct()],
                EndsGame = readable.Any(item => item.Payload is GameEnded),
            });
        }

        var phases = new List<ReplayPhase>();
        foreach (var step in steps)
        {
            if (phases.Count > 0 && phases[^1] is var last && last.Phase == step.Phase && (step.IsSetup || (last.Turn == step.Turn && last.PhasingSide == step.PhasingSide)))
            {
                phases[^1] = new ReplayPhase(last.Turn, last.PhasingSide, last.Phase, last.FirstStep, last.Count + 1);
            }
            else
            {
                phases.Add(new ReplayPhase(step.Turn, step.PhasingSide, step.Phase, step.Number, 1));
            }
        }

        return new ReplayTimeline(steps, phases);
    }

    /// <summary>A Location in a title: its hex and level, with its board only when the map has several.</summary>
    public static string Place(GameState state, BoardLocation at)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(at);
        if (state.Map.Boards.Count != 1)
        {
            return DisplayText.Location(at);
        }

        return at.Level switch
        {
            0 => at.Hex.ToString(),
            -1 => $"{at.Hex}, cellar",
            var level => string.Create(CultureInfo.InvariantCulture, $"{at.Hex}, level {level}"),
        };
    }

    private static string Place(GameState state, string location) => BoardLocation.TryParse(location, out var at) ? Place(state, at) : location;

    /// <summary>A step's kind, the side that took it when the events say, and its title, from the events the view may read.</summary>
    private static (string Kind, string? Acting, string Title) Describe(GameEvent[] readable, GameState before, Perspective viewer)
    {
        string? SideOf(string? id) => id is null ? null : before.Find(id)?.Side;
        string Named(string? side) => side is null ? "A" : $"A {DisplayText.Side(side)}";
        var defender = before.Sides.FirstOrDefault(side => side.Id != before.PhasingSide)?.Id;
        var payloads = readable.Select(item => item.Payload).ToArray();
        var ended = payloads.OfType<GameEnded>().FirstOrDefault();
        var end = ended is null ? string.Empty : $"; the game ends{(ended.Result is { } won ? $": {ResultText(won)}" : string.Empty)}";
        static string? Head(IReadOnlyList<string> ids) => ids.Count > 0 ? ids[0] : null;

        if (First<GameStarted>(payloads) is { } started)
        {
            return ("start", null, started.Scenario is { } card ? $"The game starts: {card.Title}" : "The game starts");
        }

        if (First<FireResolved>(payloads) is { } fire)
        {
            var acting = SideOf(Head(fire.Firers)) ?? (fire.MovementStep is not null || before.Phase is "mph" or "dfph" ? defender : before.PhasingSide);
            var result = ResultWords(fire.Resolution.Deserialize<FireResolution>(LiveFire.Json)?.Arithmetic?.Result);
            return ("fire", acting, $"{FireName(fire, before, acting, DisplayText.Side(acting))}: {Place(before, fire.FirerLocation)} at {Place(before, fire.TargetLocation)}"
                + (result is null ? string.Empty : $": {result}") + end);
        }

        if (First<FireReported>(payloads) is { } report)
        {
            // The record is the target side's (A12.14); the firing side reads the public report of its own attack.
            var acting = viewer.IsAdjudicator ? null : viewer.Name;
            var result = ResultWords(report.Arithmetic.Deserialize<FireArithmetic>(LiveFire.Json)?.Result);
            return ("fire", acting, $"Fire from {Place(before, report.FirerLocation)} at {Place(before, report.TargetLocation)}" + (result is null ? string.Empty : $": {result}") + end);
        }

        if (First<OrdnanceFired>(payloads) is { } ordnance)
        {
            var hit = ordnance.Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json)?.ToHit;
            var outcome = hit is null ? string.Empty : hit.CriticalHit ? ": Critical Hit" : hit.Hit ? ": hit" : ": miss";
            return ("fire", SideOf(ordnance.Crew), $"{DisplayText.Side(SideOf(ordnance.Crew))} ordnance fires at {Place(before, ordnance.Target)}{outcome}{end}");
        }

        if (First<CloseCombatResolved>(payloads) is { } combat)
        {
            return ("cc", null, $"Close Combat in {Place(before, combat.Location)}{end}");
        }

        if (First<AmbushRolled>(payloads) is { } ambush)
        {
            return ("cc", null, $"Ambush dr in {Place(before, ambush.Location)}: {(ambush.Ambusher is { } side ? $"the {DisplayText.Side(side)} side ambushes" : "no Ambush")}");
        }

        if (First<MovementStepped>(payloads) is { } moved)
        {
            var side = SideOf(Head(moved.Movers));
            return ("move", side, $"{Named(side)} stack moves to {Place(before, moved.To)}{end}");
        }

        if (First<VehicleStepped>(payloads) is { } drove)
        {
            var side = SideOf(drove.Vehicle);
            return ("move", side, $"{Named(side)} vehicle moves in {Place(before, drove.At)}{end}");
        }

        if (First<AdvanceMoved>(payloads) is { } advanced)
        {
            var side = SideOf(Head(advanced.Units));
            return ("advance", side, $"{DisplayText.Side(side)} units advance into {Place(before, advanced.To)}{end}");
        }

        if (First<RoutStepped>(payloads) is { } routed)
        {
            var side = SideOf(routed.Unit);
            return ("rout", side, $"{Named(side)} broken unit routs to {Place(before, routed.To)}{end}");
        }

        if (First<RallyAttempted>(payloads) is { } rally)
        {
            var side = SideOf(rally.Unit);
            var rallied = rally.Resolution.Deserialize<RallyResolution>(LiveFire.Json)?.Arithmetic?.Rallied;
            var where = before.Location(rally.Unit)?.Location is { } at ? $" in {Place(before, at)}" : string.Empty;
            return ("rally", side, $"{DisplayText.Side(side)} Rally attempt{where}" + (rallied is null ? string.Empty : rallied == true ? ": rallied" : ": not rallied"));
        }

        if (First<DeploymentAttempted>(payloads) is { } deployment)
        {
            var side = SideOf(deployment.Squad);
            return ("rally", side, $"{Named(side)} squad tries to Deploy: {(deployment.Passed ? "two HS" : "it stays a squad")}");
        }

        if (First<LineageRecorded>(payloads) is { Action: LineageAction.Deployed or LineageAction.Recombined } lineage)
        {
            var side = SideOf(Head(lineage.Consumed));
            return ("rally", side, lineage.Action == LineageAction.Deployed ? $"{Named(side)} squad Deploys" : $"Two {DisplayText.Side(side)} HS Recombine");
        }

        if (First<RepairAttempted>(payloads) is { } repair)
        {
            return ("rally", SideOf(repair.Unit), $"{Named(SideOf(repair.Unit))} unit tries to repair a weapon");
        }

        if (First<RecoveryAttempted>(payloads) is { } recovery)
        {
            return ("rally", SideOf(recovery.Unit), $"{Named(SideOf(recovery.Unit))} unit tries to Recover a weapon: {(recovery.Recovered ? "recovered" : "not recovered")}");
        }

        if (First<MovementWindowClosed>(payloads) is not null)
        {
            return ("pass", defender, $"The DEFENDER passes{end}");
        }

        if (First<MovementEnded>(payloads) is { } stopped)
        {
            var side = SideOf(Head(stopped.Movers)) ?? before.PhasingSide;
            return ("move-end", side, $"The {DisplayText.Side(side)} stack ends its move{end}");
        }

        if (First<OpportunityFireDeclared>(payloads) is { } held)
        {
            var side = SideOf(Head(held.Units)) ?? before.PhasingSide;
            return ("fire", side, $"{DisplayText.Side(side)} units are held for Opportunity Fire");
        }

        if (First<BuildingMoppedUp>(payloads) is { } mopped)
        {
            return ("other", mopped.Side, $"The {DisplayText.Side(mopped.Side)} side Mops Up a building{end}");
        }

        if (First<InstanceCaptured>(payloads) is { } captured)
        {
            var side = SideOf(captured.Id);
            return ("other", SideOf(captured.Custodian), $"{Named(side)} unit is taken prisoner{end}");
        }

        if (First<SurrenderRejected>(payloads) is { } rejected)
        {
            return ("other", null, $"{Named(SideOf(rejected.Unit))} unit's surrender is refused{end}");
        }

        if (First<SurrenderPending>(payloads) is { } surrender)
        {
            return ("other", null, $"{Named(SideOf(surrender.Unit))} unit offers to surrender{end}");
        }

        if (First<ChoicePending>(payloads) is { } waiting)
        {
            return ("other", null, $"A choice waits for the {DisplayText.Side(waiting.Side)} side");
        }

        if (First<ChoiceMade>(payloads) is not null)
        {
            return ("other", null, $"A choice is answered{end}");
        }

        if (readable.FirstOrDefault(item => item.Type == "hidden-placed") is { Payload: ConditionsChanged placed })
        {
            // A12.32 (ruling R23.5): the event is its own side's; the other side sees only that a "?" is there.
            var side = SideOf(placed.Id);
            return ("other", side, $"The {DisplayText.Side(side)} side places hidden units beneath \"?\"");
        }

        if (First<SetupConcealed>(payloads) is { } concealed)
        {
            var side = SideOf(concealed.Id);
            var count = payloads.OfType<SetupConcealed>().Count();
            return ("setup", side, $"The {DisplayText.Side(side)} side places non-OB \"?\" on {count} {(count == 1 ? "unit" : "units")}");
        }

        if (First<InstanceCreated>(payloads) is { } created && payloads.All(GameState.IsSetupEvent))
        {
            // Only the side's own view and the adjudicator's read every counter of a setup, so only they are given the count.
            var side = created.Instance.Side;
            var own = viewer.IsAdjudicator || viewer.Name == side;
            var count = payloads.OfType<InstanceCreated>().Count();
            return ("setup", side, $"The {DisplayText.Side(side)} side sets up" + (own ? $": {count} {(count == 1 ? "counter" : "counters")}" : string.Empty));
        }

        if (First<PhaseChanged>(payloads) is { } phase)
        {
            var next = phase.Turn != before.Turn ? $"Turn {phase.Turn} begins"
                : phase.PhasingSide != before.PhasingSide ? $"the {DisplayText.Side(phase.PhasingSide)} Player Turn begins" : null;
            return (ended is null ? "phase" : "end", null, $"The {DisplayText.Side(before.PhasingSide)} {GameText.PhaseLabel(before.Phase)} ends" + (next is null ? string.Empty : $"; {next}") + end);
        }

        if (ended is not null)
        {
            return ("end", null, "The game ends" + (ended.Result is { } final ? $": {ResultText(final)}" : string.Empty));
        }

        // Anything else is named by its first event's type, in words: "smoke-placed" reads "Smoke placed".
        var type = readable.Select(item => item.Type).FirstOrDefault(name => name != "dice-rolled" && name != "conditions-changed") ?? readable[0].Type;
        var words = type.Replace('-', ' ');
        return ("other", null, char.ToUpperInvariant(words[0]) + words[1..]);
    }

    private static T? First<T>(EventPayload[] payloads)
        where T : EventPayload => payloads.OfType<T>().FirstOrDefault();

    /// <summary>An IFT result in a title: the table's own word, with "none" said as "no effect".</summary>
    private static string? ResultWords(string? result) => result is null ? null : result.Equals("none", StringComparison.OrdinalIgnoreCase) ? "no effect" : result;

    private static string ResultText(GameResult result) => result.Winner is { } winner ? $"{DisplayText.Side(winner)} win" : "no winner";

    /// <summary>What a fire is called in its phase (A7.2, A8.1, A8.4, A7.24): Residual FP has no firers (A8.22).</summary>
    private static string FireName(FireResolved fire, GameState before, string? acting, string side)
    {
        if (fire.Firers.Count == 0)
        {
            return "Residual FP attacks";
        }

        var name = fire.MovementStep is not null || (before.Phase == "mph" && acting != before.PhasingSide) ? "Defensive First Fire"
            : before.Phase switch
            {
                "pfph" => "Prep Fire",
                "dfph" => "Final Fire",
                "afph" => "Advancing Fire",
                _ => "fire",
            };
        return $"{side} {name}";
    }

    private static IEnumerable<ReplayFire> FiresOf(GameEvent[] readable)
    {
        foreach (var payload in readable.Select(item => item.Payload))
        {
            var (from, to) = payload switch
            {
                FireResolved fire => (fire.FirerLocation, fire.TargetLocation),
                FireReported report when !readable.Any(item => item.EventId == report.Fire) => (report.FirerLocation, report.TargetLocation),
                _ => (null, null),
            };
            if (from is not null && to is not null && BoardLocation.TryParse(from, out var source) && BoardLocation.TryParse(to, out var target))
            {
                yield return new ReplayFire(source, target);
            }
        }
    }

    private static IEnumerable<BoardLocation> LocationsOf(GameEvent[] readable, GameState before)
    {
        foreach (var fire in FiresOf(readable))
        {
            yield return fire.From;
            yield return fire.To;
        }

        foreach (var payload in readable.Select(item => item.Payload))
        {
            var at = payload switch
            {
                MovementStepped moved => moved.To,
                VehicleStepped drove => drove.At,
                AdvanceMoved advanced => advanced.To,
                RoutStepped routed => routed.To,
                CloseCombatResolved combat => combat.Location,
                AmbushRolled ambush => ambush.Location,
                OrdnanceFired ordnance => ordnance.Target,
                RallyAttempted rally => before.Location(rally.Unit)?.Location,
                _ => null,
            };
            if (at is not null)
            {
                yield return at;
            }
        }
    }
}
