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
    /// <summary>The phase as a heading of the timeline: whose Player Turn it is, then the phase, so the DEFENDER's fire phase is not read as the phasing side's own.</summary>
    public string Label => Phase == ReplayStep.SetupPhase ? "Setup" : $"{DisplayText.Side(PhasingSide)} Player Turn: {GameText.PhaseLabel(Phase)}";
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
    /// <paramref name="viewAt"/> gives the perspective's view at a revision, and decides two cases for a side (the referee's review, pass 31b). An
    /// attempt the view reads nothing of is still a step when the view sees the map change over it: at the table the other side would see the "?"
    /// placed, though not what it is. And an attempt whose public events say only that something minor was done (a SW passed, a marker changed)
    /// is a step only when the view sees a change: a SW passed between units under "?" is not the other side's to know, nor when it was done.
    /// </remarks>
    public static ReplayTimeline Read(GameHistory history, GameView view, Func<long, GameView>? viewAt = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(view);
        var entitled = view.Events.Select(item => item.EventId).ToHashSet(StringComparer.Ordinal);
        var side = !view.Perspective.IsAdjudicator;
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

            // Setup is what happens before play starts; a unit created later (a hero, a leader, a reinforcement) is a step of its phase.
            var setup = before is not { SetupClosed: true } && all.All(item => GameState.IsSetupEvent(item.Payload));
            var phase = setup ? ReplayStep.SetupPhase : at.Phase;
            bool Seen() => viewAt is not null && all[0].Revision > 1 && ReplayDiff.Of(viewAt(all[0].Revision - 1), viewAt(all[^1].Revision), []).Count > 0;
            if (readable.Length == 0)
            {
                if (side && Seen())
                {
                    var other = setup ? all.Select(item => item.Payload).OfType<InstanceCreated>().Select(item => item.Instance.Side).FirstOrDefault(name => name is not null) : null;
                    steps.Add(new ReplayStep(steps.Count + 1, all[0].Revision, all[^1].Revision, attempt, "unread", at.Turn, phase, at.PhasingSide, other,
                        other is null ? "The other side acts out of this view's sight, and the map shows a change" : $"The {DisplayText.Side(other)} side sets up",
                        ReplayRead.None)
                    {
                        Locations = [.. ReplayDiff.Of(viewAt!(all[0].Revision - 1), viewAt(all[^1].Revision), []).Select(change => change.At).OfType<BoardLocation>().Distinct()],
                    });
                }

                continue;
            }

            var (kind, acting, title, minor) = Describe(readable, at, view.Perspective);
            if (minor && side && !Seen())
            {
                continue;
            }

            // Only a fire is said to be read in part: its public report is table knowledge (A12.14). Any other step with a withheld event reads as
            // whole, so the mark cannot point to a hidden unit or a Bore Sighted Location in a setup.
            steps.Add(new ReplayStep(steps.Count + 1, all[0].Revision, all[^1].Revision, attempt, kind, at.Turn, phase, at.PhasingSide, acting, title,
                kind == "fire" && readable.Length != all.Length ? ReplayRead.Part : ReplayRead.Full)
            {
                Fires = [.. FiresOf(readable)],
                Locations = [.. LocationsOf(readable, at, view.Perspective).Distinct()],
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
        return DisplayText.Place(state, at);
    }

    private static string Place(GameState state, string location) => BoardLocation.TryParse(location, out var at) ? Place(state, at) : location;

    /// <summary>
    /// A step's kind, the side that took it when the events say, its title, and whether it is minor, from the events the view may read. A minor step
    /// says only that something small was done, and a side's view keeps it only when that view sees a change.
    /// </summary>
    private static (string Kind, string? Acting, string Title, bool Minor) Describe(GameEvent[] readable, GameState before, Perspective viewer)
    {
        string? SideOf(string? id) => id is null ? null : before.Find(id)?.Side;
        static string A(string? side, string noun) => side is null ? $"A {noun}" : $"A {DisplayText.Side(side)} {noun}";
        static string The(string? side, string noun) => side is null ? $"The {noun}" : $"The {DisplayText.Side(side)} {noun}";
        static string? Head(IReadOnlyList<string> ids) => ids.Count > 0 ? ids[0] : null;
        var defender = before.Sides.FirstOrDefault(side => side.Id != before.PhasingSide)?.Id;
        var payloads = readable.Select(item => item.Payload).ToArray();
        var ended = payloads.OfType<GameEnded>().FirstOrDefault();
        var end = ended is null ? string.Empty : $"; the game ends{(ended.Result is { } won ? $": {ResultText(won)}" : string.Empty)}";

        if (First<GameStarted>(payloads) is { } started)
        {
            return ("start", null, started.Scenario is { } card ? $"The game starts: {card.Title}" : "The game starts", false);
        }

        // C8.4 (pass 55): a Canister shot is one ordnance record and a fire record a Location on its DR; the step is the shot's.
        if (First<OrdnanceFired>(payloads) is { } canisterShot && canisterShot.Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json) is { Canister: { } canister } canisterFired)
        {
            var owner = SideOf(canisterShot.Crew);
            var attacked = canister.Attacks.Count > 0 ? $" at {string.Join(", ", canister.Attacks.Select(item => Place(before, item.LocationId)))}"
                : canisterFired.Gun is { Malfunctioned: true } ? ": the Gun malfunctions" : ": it had none (C8.9)";
            return ("fire", owner, $"{(owner is null ? "Ordnance" : $"{DisplayText.Side(owner)} ordnance")} fires Canister{attacked}{end}", false);
        }

        if (First<FireResolved>(payloads) is { } fire)
        {
            var attack = fire.Facts.Deserialize<FireAttack>(LiveFire.Json);
            var acting = SideOf(Head(fire.Firers)) ?? (fire.MovementStep is not null || before.Phase is "mph" or "dfph" ? defender : before.PhasingSide);
            var result = ResultWords(fire.Resolution.Deserialize<FireResolution>(LiveFire.Json)?.Arithmetic?.Result);
            return ("fire", acting, $"{FireName(fire, attack, before, acting)}: {Place(before, fire.FirerLocation)} at {Place(before, fire.TargetLocation)}"
                + (result is null ? string.Empty : $": {result}") + end, false);
        }

        if (First<FireReported>(payloads) is { } report)
        {
            // The record is the target side's (A12.14); the firing side reads the public report of its own attack, which does not say its kind.
            var acting = viewer.IsAdjudicator ? null : viewer.Name;
            var result = ResultWords(report.Arithmetic.Deserialize<FireArithmetic>(LiveFire.Json)?.Result);
            var name = acting is null ? "Fire" : $"{DisplayText.Side(acting)} {PhaseFire(before, acting)}";
            return ("fire", acting, $"{name}: {Place(before, report.FirerLocation)} at {Place(before, report.TargetLocation)}" + (result is null ? string.Empty : $": {result}") + end, false);
        }

        if (First<OrdnanceFired>(payloads) is { } ordnance)
        {
            // C8.9 (the table player's review of pass 55): a round that was not there is no miss. An APCR or HEAT record made before the pass
            // keeps the word it had.
            static string Outcome(OrdnanceFired shot)
            {
                var resolution = shot.Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json);
                var ammunition = shot.Facts.ValueKind == JsonValueKind.Object && shot.Facts.TryGetProperty("ammunition", out var named) ? named.GetString() : null;
                var hit = resolution?.ToHit;
                return hit is null ? string.Empty
                    : resolution is { AmmunitionUse: "none" } && (resolution.DepletionNumber is not null || ammunition is "apds" or "he" or "ap")
                        ? resolution.Gun is { Malfunctioned: true } ? "it had none, and the Gun malfunctions (C8.9)" : "it had none (C8.9)"
                    : hit.CriticalHit ? "Critical Hit" : hit.Hit ? "hit" : "miss";
            }

            var first = Outcome(ordnance);
            var outcome = first.Length == 0 ? string.Empty : $": {first}";
            var owner = SideOf(ordnance.Crew);

            // C2.2401 (pass 55): a Gun Duel's first shot, and the declared shot it answered: its result when it was made in the same commit
            // (the UI review), or that the first shot voided it.
            var duel = ordnance.Facts.ValueKind == JsonValueKind.Object && ordnance.Facts.TryGetProperty("gunDuel", out var duelFact) && duelFact.ValueKind == JsonValueKind.True;
            var returned = duel && payloads.OfType<OrdnanceFired>().Skip(1).FirstOrDefault() is { } second && Outcome(second) is { Length: > 0 } result
                ? $"; the return shot: {result}" : string.Empty;
            var voided = First<GunDuelVoided>(payloads) is { } none ? none.Cause == ScenarioA1GunDuels.Other ? "; the return shot is not fired" : "; the return shot is void" : string.Empty;
            return ("fire", owner, $"{(owner is null ? "Ordnance" : $"{DisplayText.Side(owner)} ordnance")} fires{(duel ? " first in a Gun Duel" : string.Empty)} at {Place(before, ordnance.Target)}{outcome}{returned}{voided}{end}", false);
        }

        if (First<CloseCombatResolved>(payloads) is { } combat)
        {
            var round = combat.Round switch
            {
                CloseCombatResolved.AmbusherRound => ", the ambusher's round",
                CloseCombatResolved.AmbushedRound => ", the ambushed side's round",

                // A11.31 (pass 70, ruling R70.30): one Infantry attack on Infantry in a Location holding a vehicle.
                CloseCombatResolved.SequentialRound => ", one attack of the sequence with a vehicle there",
                _ => string.Empty,
            };
            return ("cc", null, $"Close Combat in {Place(before, combat.Location)}{round}{end}", false);
        }

        if (First<AmbushRolled>(payloads) is { } ambush)
        {
            return ("cc", null, $"Ambush dr in {Place(before, ambush.Location)}: {(ambush.Ambusher is { } side ? $"the {DisplayText.Side(side)} side ambushes" : "no Ambush")}", false);
        }

        if (First<MovementStepped>(payloads) is { } moved)
        {
            var side = SideOf(Head(moved.Movers));
            return ("move", side, $"{A(side, moved.Movers.Count == 1 ? "unit" : "stack")} moves to {Place(before, moved.To)}{(moved.Assault ? " by Assault Movement" : string.Empty)}{end}", false);
        }

        if (First<VehicleStepped>(payloads) is { } drove)
        {
            var side = SideOf(drove.Vehicle);

            // D6.4, D6.5 (pass 65, ruling R65.15): Infantry board, or Passengers unload, in place of a move.
            var what = drove.Kind switch
            {
                VehicleStepped.Delay => "spends a Delay MP",
                VehicleStepped.Load => drove.Riders ? $"takes {(drove.Units?.Count == 1 ? "a Rider" : "Riders")}" : $"takes {(drove.Units?.Count == 1 ? "a unit" : "units")} aboard",
                VehicleStepped.Unload => $"unloads {(drove.Units?.Count == 1 ? "a unit" : "units")}",
                _ => "moves",
            };

            // D6.21, D6.24 (pass 65, ruling R65.17): Riders that Bail Out as it moves.
            var bailing = payloads.OfType<RiderBailedOut>().Any() ? ", and its Riders Bail Out" : string.Empty;

            // D6.83 (pass 65, the table player): a Carrier whose inherent HS unloads with the step is Abandoned.
            var abandons = drove.Kind == VehicleStepped.Unload && payloads.OfType<ConditionsChanged>().Any(changed => changed.Id == drove.Vehicle
                && changed.Conditions.TryGetValue(Conditions.Abandoned, out var now) && now == ConditionState.True) ? ", and its inherent HS leaves it Abandoned" : string.Empty;
            return ("move", side, $"{A(side, "vehicle")} {what} in {Place(before, drove.At)}{bailing}{abandons}{end}", false);
        }

        // D5.41, D5.42 (pass 65, ruling R65.20): a crew leaves its vehicle, or a unit enters an Abandoned one to crew it.
        if (First<VehicleAbandoned>(payloads) is { } abandoned)
        {
            var side = SideOf(abandoned.Vehicle);
            return ("move", side, $"{A(side, "vehicle")}'s crew Abandons it in {Place(before, abandoned.At)}{end}", false);
        }

        if (First<VehicleRecrewed>(payloads) is { } recrewed)
        {
            var side = SideOf(recrewed.Vehicle);
            return ("move", side, $"{A(side, "unit")} crews an Abandoned vehicle in {Place(before, recrewed.At)}{end}", false);
        }

        if (First<MotionAttempted>(payloads) is { } motion)
        {
            // D2.401 (pass 55): the DEFENDER's vehicle attempts Motion status in the enemy MPh.
            var side = SideOf(motion.Vehicle);
            var outcome = motion.Result switch
            {
                MotionAttempted.Passed => "it gains Motion",
                MotionAttempted.Failed => "it fails",
                MotionAttempted.Immobilized => "it is immobilized",
                _ => "it bogs",
            };
            return ("move", side, $"{A(side, "vehicle")} attempts Motion: {outcome}{end}", false);
        }

        if (First<AdvanceMoved>(payloads) is { } advanced)
        {
            var side = SideOf(Head(advanced.Units));
            return ("advance", side, $"{A(side, advanced.Units.Count == 1 ? "unit advances" : "stack advances")} into {Place(before, advanced.To)}{end}", false);
        }

        // Pass 70 (ruling R70.11): units broken voluntarily at the start of the RtPh, so as to rout.
        if (First<VoluntaryBreak>(payloads) is { } voluntary)
        {
            var side = SideOf(Head(voluntary.Units));
            return ("rout", side, voluntary.Units.Count == 1 ? $"{A(side, "unit")} breaks voluntarily{end}" : $"{DisplayText.Side(side)} units break voluntarily{end}", false);
        }

        // The table player, pass 35: a rout repulsed after a step it made is titled by the repulse, not by its first step.
        if ((payloads.OfType<RoutStepped>().FirstOrDefault(step => step.Attempted is not null) ?? First<RoutStepped>(payloads)) is { } routed)
        {
            var side = SideOf(routed.Unit);
            return ("rout", side, routed.Attempted is { } repulsedFrom
                ? $"{A(side, "broken unit")} is repulsed from {Place(before, repulsedFrom)} by a concealed unit{end}"
                // D6.1, D5.311 (pass 65, the table player): a unit aboard routs beneath its vehicle.
                : before.Unit(routed.Unit) is { Position: ContainedPosition }
                    ? $"{A(side, "broken unit")} routs beneath its vehicle in {Place(before, routed.To)}{end}"
                : $"{A(side, "broken unit")} routs to {Place(before, routed.To)}{(routed.LowCrawl ? " by Low Crawl" : string.Empty)}{end}", false);
        }

        if (First<RallyAttempted>(payloads) is { } rally)
        {
            var side = SideOf(rally.Unit);
            var rallied = rally.Resolution.Deserialize<RallyResolution>(LiveFire.Json)?.Arithmetic?.Rallied;
            var where = before.Location(rally.Unit)?.Location is { } at ? $" in {Place(before, at)}" : string.Empty;
            return ("rally", side, $"{A(side, rally.Leader is null ? "Self-Rally attempt" : "Rally attempt")}{where}" + (rallied is null ? string.Empty : rallied == true ? ": rallied" : ": not rallied"), false);
        }

        if (First<DeploymentAttempted>(payloads) is { } deployment)
        {
            var side = SideOf(deployment.Squad);
            return ("rally", side, $"{A(side, "squad")} tries to Deploy: {(deployment.Passed ? "two HS" : "it stays a squad")}", false);
        }

        if (First<LineageRecorded>(payloads) is { Action: LineageAction.Deployed or LineageAction.Recombined } lineage)
        {
            var side = SideOf(Head(lineage.Consumed));
            return ("rally", side, lineage.Action == LineageAction.Deployed ? $"{A(side, "squad")} Deploys" : $"Two {DisplayText.Side(side)} HS Recombine".Replace("  ", " ", StringComparison.Ordinal), false);
        }

        if (First<RepairAttempted>(payloads) is { } repair)
        {
            var result = repair.Result switch
            {
                RepairAttempted.Repaired => "repaired",
                RepairAttempted.Eliminated => repair.Weapon is null ? "the weapon is eliminated" : "the weapon is disabled",
                _ => "still malfunctioned",
            };
            return ("rally", SideOf(repair.Unit), $"{A(SideOf(repair.Unit), "unit")} tries to repair a weapon: {result}", false);
        }

        if (First<InfantryOverrunNtc>(payloads) is { } ntc)
        {
            return ("move", SideOf(ntc.Taker), $"{A(SideOf(ntc.Taker), "unit")} takes the NTC for an Infantry OVR: {(ntc.Passed ? "passed" : "failed")}", false);
        }

        // D10.5 (pass 65, ruling R65.23): a unit scrounges a wreck or an Abandoned vehicle in the RPh.
        if (First<VehicleScrounged>(payloads) is { } scrounged)
        {
            var side = SideOf(scrounged.Unit);
            return ("rally", side, $"{A(side, "unit")} scrounges a vehicle in {Place(before, scrounged.At)}: "
                + (scrounged.Taken is { Count: > 0 } found ? $"{found.Count} SW salvaged" : "nothing salvaged"), false);
        }

        if (First<RecoveryAttempted>(payloads) is { } recovery)
        {
            return ("rally", SideOf(recovery.Unit), $"{A(SideOf(recovery.Unit), "unit")} tries to Recover a weapon: {(recovery.Recovered ? "recovered" : "not recovered")}", false);
        }

        if (First<MovementWindowClosed>(payloads) is not null)
        {
            return ("pass", defender, $"{The(defender, "side")}, the DEFENDER, declines First Fire{end}", false);
        }

        if (First<MovementEnded>(payloads) is { } stopped)
        {
            var side = SideOf(Head(stopped.Movers)) ?? before.PhasingSide;
            return ("move-end", side, $"{The(side, stopped.Movers.Count == 1 ? "unit" : "stack")} ends its move{end}", false);
        }

        if (First<OpportunityFireDeclared>(payloads) is { } held)
        {
            var side = SideOf(Head(held.Units)) ?? before.PhasingSide;
            return ("fire", side, $"{DisplayText.Side(side)} units are held for Opportunity Fire", false);
        }

        // Pass 70 (ruling R70.24): a Search as the stack ends its move.
        if (First<UnitsSearched>(payloads) is { } searched)
        {
            var side = SideOf(Head(searched.Units)) ?? before.PhasingSide;
            return ("move", side, $"{A(side, searched.Units.Count == 1 ? "unit" : "stack")} Searches from {Place(before, searched.Location)}{end}", false);
        }

        if (First<BuildingMoppedUp>(payloads) is { } mopped)
        {
            return ("other", mopped.Side, DisplayText.Hexes($"{The(mopped.Side, "side")} Mops Up building {mopped.Building}", before.Map.Boards.Count) + end, false);
        }

        if (First<PrisonersMassacred>(payloads) is not null)
        {
            return ("other", null, $"Prisoners are massacred{end}", false);
        }

        if (First<InstanceCaptured>(payloads) is { } captured)
        {
            return ("other", SideOf(captured.Custodian), $"{A(SideOf(captured.Id), "unit")} is taken prisoner{end}", false);
        }

        if (First<PrisonerFreed>(payloads) is { } freed)
        {
            return ("other", null, $"{A(SideOf(freed.Unit), "prisoner")} is freed{end}", false);
        }

        if (First<SurrenderRejected>(payloads) is { } rejected)
        {
            return ("other", null, $"{A(SideOf(rejected.Unit), "unit")}'s surrender is refused{end}", false);
        }

        if (First<SurrenderPending>(payloads) is { } surrender)
        {
            return ("other", null, $"{A(SideOf(surrender.Unit), "unit")} offers to surrender{end}", false);
        }

        // A14.4 (pass 71, rulings R71.9, R71.10): a Sniper Check, and a Sniper eliminated, which lowers its side's SAN.
        if (First<SniperChecked>(payloads) is { } check)
        {
            return ("other", SideOf(Head(check.Units)), $"A Sniper Check against {A(SideOf(check.Sniper), "Sniper")} in {Place(before, check.Location)}{end}", false);
        }

        if (First<SanReduced>(payloads) is { } reduced)
        {
            // E1.76 (R4's finding 4): at night the SAN never falls below the printed one.
            return ("other", reduced.Side, $"{The(reduced.Side, "Sniper")} is eliminated, and the {DisplayText.Side(reduced.Side)} SAN "
                + (PlayRecords.SanUnchanged(before, reduced.Side) ? "stays at the printed SAN at night" : "falls") + end, false);
        }

        if (First<ChoicePending>(payloads) is { } waiting)
        {
            // A14.4 (pass 71, ruling R71.10): the attacked side may take a Sniper Check.
            if (waiting.Kind == ChoicePending.SniperCheck)
            {
                return ("other", waiting.Side, $"A Sniper attacks, and the {DisplayText.Side(waiting.Side)} side may take a Sniper Check", false);
            }

            // A14.2 (pass 71, ruling R71.12): the Sniper player chooses during his Sniper's attack.
            if (waiting.Kind == ChoicePending.Sniper)
            {
                return ("other", waiting.Side, $"{The(waiting.Side, "Sniper")} attacks, and the {DisplayText.Side(waiting.Side)} side chooses how", false);
            }

            // C2.2401 (pass 55): the DEFENDER's shot is declared and not yet rolled.
            if (waiting.Kind == ChoicePending.GunDuel)
            {
                return ("fire", defender, $"A shot is declared, and the {DisplayText.Side(waiting.Side)} side may answer with a Gun Duel", false);
            }

            return ("other", null, $"A choice waits for the {DisplayText.Side(waiting.Side)} side", false);
        }

        if (First<ChoiceMade>(payloads) is not null)
        {
            return ("other", null, $"A choice is answered{end}", false);
        }

        // Pass 70 (A12.14; ruling R70.21): a concealed enemy unit shows itself for a moment to force a "?" loss; the record is the moving side's alone.
        if (First<ConcealmentVerified>(payloads) is { } verified)
        {
            var side = SideOf(Head(verified.Units));
            return ("other", side, $"{A(side, "concealed unit")} shows itself for a moment to force the loss of a \"?\"{end}", false);
        }

        if (First<SniperAttacked>(payloads) is { } sniper)
        {
            // A14.2 (pass 71, ruling R71.12): a forfeited attack's hex is the one its counter is repositioned to.
            if (sniper.Result == ScenarioA1Sniper.Forfeited)
            {
                return ("other", SideOf(sniper.Sniper), $"{A(SideOf(sniper.Sniper), "Sniper")} forfeits its attack and is repositioned" + (sniper.Target is { } to ? $" to {Place(before, to)}" : string.Empty) + end, false);
            }

            return ("other", SideOf(sniper.Sniper), "A Sniper attacks" + (sniper.Target is { } target ? $" in {Place(before, target)}" : string.Empty) + end, false);
        }

        if (First<WindChanged>(payloads) is not null)
        {
            return ("other", null, "A Wind Change DR", false);
        }

        if (readable.FirstOrDefault(item => item.Type == "hidden-placed") is { Payload: ConditionsChanged placed })
        {
            // A12.32 (ruling R23.5): the event is its own side's; the other side sees only that a "?" is there.
            var side = SideOf(placed.Id);
            return ("other", side, $"{The(side, "side")} places hidden units beneath \"?\"", false);
        }

        if (First<SetupConcealed>(payloads) is { } concealed)
        {
            var side = SideOf(concealed.Id);
            var count = payloads.OfType<SetupConcealed>().Count();
            return ("setup", side, $"{The(side, "side")} places non-OB \"?\" on {count} {(count == 1 ? "unit" : "units")}", false);
        }

        if (First<InstanceCreated>(payloads) is { } created && payloads.All(GameState.IsSetupEvent))
        {
            // Only the side's own view and the adjudicator's read every counter of a setup, so only they are given the count.
            var side = created.Instance.Side;
            var own = viewer.IsAdjudicator || viewer.Name == side;
            var count = payloads.OfType<InstanceCreated>().Count();
            return ("setup", side, $"{The(side, "side")} sets up" + (own ? $": {count} {(count == 1 ? "counter" : "counters")}" : string.Empty), false);
        }

        if (First<PhaseChanged>(payloads) is { } phase)
        {
            var next = phase.Turn != before.Turn ? $"Game Turn {phase.Turn} begins"
                : phase.PhasingSide != before.PhasingSide ? $"the {DisplayText.Side(phase.PhasingSide)} Player Turn begins" : null;
            var lost = payloads.OfType<InstanceEliminated>().Count();
            return (ended is null ? "phase" : "end", null, $"The {DisplayText.Side(before.PhasingSide)} {GameText.PhaseLabel(before.Phase)} ends"
                + (lost == 0 ? string.Empty : lost == 1 ? "; a unit is eliminated" : $"; {lost} units are eliminated") + (next is null ? string.Empty : $"; {next}") + end, false);
        }

        if (ended is not null)
        {
            return ("end", null, "The game ends" + (ended.Result is { } final ? $": {ResultText(final)}" : string.Empty), false);
        }

        // Pass 70's fixes (A12.14; ruling R70.20): a voluntary removal of "?" holds only its reveals and the Dummies it removes, and is titled by what the
        // side did, not by its first Dummy.
        if (readable.Any(item => item.Type == "concealment-lost") && readable.All(item => item.Type == "concealment-lost"
            || (item.Payload is InstanceEliminated dummy && before.Unit(dummy.Id) is { Kind: UnitKinds.Dummy })))
        {
            var side = readable[0].Payload is ConditionsChanged first ? SideOf(first.Id) : SideOf(((InstanceEliminated)readable[0].Payload).Id);
            return ("other", side, $"{The(side, "side")} gives up \"?\"", false);
        }

        if (First<InstanceEliminated>(payloads) is { } gone)
        {
            // Pass 31d (design D5): a Dummy is no unit; its stack is removed.
            return ("other", null, before.Unit(gone.Id) is { Kind: UnitKinds.Dummy } ? $"{A(SideOf(gone.Id), "Dummy stack")} is removed" : $"{A(SideOf(gone.Id), "unit")} is eliminated", false);
        }

        if (First<GunTurned>(payloads) is { } turned)
        {
            return ("other", SideOf(turned.Gun), $"{A(SideOf(turned.Gun), "Gun")} turns without firing", false);
        }

        // What follows says only that something small was done; a side's view keeps such a step only when it sees a change (the referee's review).
        if (First<EquipmentTransferred>(payloads) is { } passed)
        {
            var side = SideOf(passed.Holding?.Holder) ?? SideOf(passed.Id);
            return ("other", side, passed.Holding is null ? $"{A(side, "weapon")} is left in its Location" : $"{A(side, "weapon")} changes hands", true);
        }

        if (First<RallyPhaseActionTaken>(payloads) is { } acted)
        {
            return ("rally", SideOf(Head(acted.Units)), $"{A(SideOf(Head(acted.Units)), "unit")} acts in the Rally Phase", true);
        }

        if (First<ConditionsChanged>(payloads) is { } marked)
        {
            return ("other", SideOf(marked.Id), $"{A(SideOf(marked.Id), "unit")}'s markers change", true);
        }

        // Anything else is named by its first event's type, in words: "smoke-placed" reads "Smoke placed".
        var type = readable.Select(item => item.Type).FirstOrDefault(name => name != "dice-rolled" && name != "conditions-changed") ?? readable[0].Type;
        var words = type.Replace('-', ' ');
        return ("other", null, char.ToUpperInvariant(words[0]) + words[1..], true);
    }

    private static T? First<T>(EventPayload[] payloads)
        where T : EventPayload => payloads.OfType<T>().FirstOrDefault();

    /// <summary>An IFT result in a title: the table's own word, with "none" said as "no effect".</summary>
    private static string? ResultWords(string? result) => result is null ? null : result.Equals("none", StringComparison.OrdinalIgnoreCase) ? "no effect" : result;

    private static string ResultText(GameResult result) => result.Winner is { } winner ? $"{DisplayText.Side(winner)} win" : "no winner";

    /// <summary>
    /// What a fire is called (A7.2, A8.1, A8.3, A8.31, A8.4, A7.24, D7.1): the attack's own kind where its facts record one (First Fire, Subsequent
    /// First Fire, Final Protective Fire, Bounding First Fire, an Overrun, Residual FP, which has no firers, A8.22), otherwise its phase's fire.
    /// </summary>
    private static string FireName(FireResolved fire, FireAttack? attack, GameState before, string? acting)
    {
        var kind = attack?.FireKind;

        // C8.4 (pass 55): a Canister shot's attack has no firers either; it is the Gun's.
        if (attack?.Canister is not null)
        {
            return acting is null ? "Canister" : $"{DisplayText.Side(acting)} Canister";
        }

        if (fire.Firers.Count == 0 || kind == ScenarioA1FireCalculator.ResidualFire)
        {
            return attack?.FireLane == true ? "A Fire Lane's Residual FP attacks" : "Residual FP attacks";
        }

        var name = kind switch
        {
            ScenarioA1FireCalculator.FirstFire => "Defensive First Fire",
            ScenarioA1FireCalculator.SubsequentFirstFire => "Subsequent First Fire",
            ScenarioA1FireCalculator.FinalProtectiveFire => "Final Protective Fire",
            ScenarioA1FireCalculator.BoundingFirstFire => "Bounding First Fire",
            ScenarioA1FireCalculator.OverrunFire => "Overrun",
            _ => attack?.DemolitionCharge is not null ? "Demolition Charge" : fire.MovementStep is not null ? "Defensive First Fire" : PhaseFire(before, acting),
        };
        return acting is null ? name : $"{DisplayText.Side(acting)} {name}";
    }

    /// <summary>The fire of a phase when the attack's facts are not read: Prep Fire, the DEFENDER's fire in the MPh, Final Fire, Advancing Fire.</summary>
    private static string PhaseFire(GameState before, string? acting) => before.Phase switch
    {
        "pfph" => "Prep Fire",
        "mph" => acting == before.PhasingSide ? "fire in the Movement Phase" : "Defensive First Fire",
        "dfph" => "Final Fire",
        "afph" => "Advancing Fire",
        _ => "fire",
    };

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

    /// <summary>
    /// Whether a step may give a unit's hex to the view (the UI review of pass 55), as a record may name it: the adjudicator, the unit's own
    /// side, or a unit then neither concealed nor hidden.
    /// </summary>
    private static bool Open(GameState before, string id, Perspective viewer) => viewer.IsAdjudicator || (before.Unit(id) is { } unit && (unit.Side == viewer.Name
        || (GameState.Condition(unit, Conditions.Concealed) != ConditionState.True && GameState.Condition(unit, Conditions.Hidden) != ConditionState.True)));

    private static IEnumerable<BoardLocation> LocationsOf(GameEvent[] readable, GameState before, Perspective viewer)
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
                RiderBailedOut bailed => bailed.At,
                VehicleAbandoned abandoned => abandoned.At,
                VehicleRecrewed recrewed => recrewed.At,
                VehicleScrounged scrounged => scrounged.At,

                // A11.52, A21.2 (pass 65, ruling R65.22): a capture is seen in its Location; it falls within a CC or phase step, which titles it.
                VehicleCaptured captured => captured.At,
                MotionAttempted motion => Open(before, motion.Vehicle, viewer) ? before.Location(motion.Vehicle)?.Location : null,
                AdvanceMoved advanced => advanced.To,
                RoutStepped routed => routed.To,
                CloseCombatResolved combat => combat.Location,
                AmbushRolled ambush => ambush.Location,
                OrdnanceFired ordnance => ordnance.Target,
                RallyAttempted rally => before.Location(rally.Unit)?.Location,

                // Pass 70 (ruling R70.11): a Voluntary Break is seen where its first unit stands.
                VoluntaryBreak voluntary => voluntary.Units.Count > 0 ? before.Location(voluntary.Units[0])?.Location : null,

                // Pass 70 (ruling R70.21): a momentary forfeiture of "?" is seen where its first unit stands (shown to the moving side only).
                ConcealmentVerified verified => verified.Units.Count > 0 ? before.Location(verified.Units[0])?.Location : null,

                // Pass 70 (ruling R70.24): a Search is seen in the Location it is made from.
                UnitsSearched search => search.Location,

                // Pass 70 (ruling R70.4): a morale check made outside an attack is seen in its Location, within the step of what caused it.
                MoraleCheckResolved check => BoardLocation.TryParse(check.Location, out var checkedAt) ? checkedAt : null,

                // Pass 71 (ruling R71.10): a Sniper Check is seen in its Location.
                SniperChecked sniperCheck => sniperCheck.Location,
                _ => null,
            };
            if (at is not null)
            {
                yield return at;
            }
        }
    }
}
