using System.Globalization;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.MapStudio.Components.Games;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;
using static LimboDancer.Domains.Asl.MapStudio.Services.FireText;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// A game's records in a player's words, as one view may read them through a revision (pass 31b, task 31b.2; plan sections 15.4 and 15.9): the rolls,
/// the ordnance, fire, Close Combat, night, and Sniper records, and the Rally, Rout, and other actions. The Play page reads them at the game's last
/// revision and the Replay page at a step's, so no record is worded twice. A list holds only events the view is entitled to; a record names a unit to
/// a side only when the side's view could name it just before the event. Each list is read once and kept.
/// </summary>
public sealed class PlayRecords
{
    private readonly GameHistory history;
    private readonly Perspective viewer;
    private readonly IReadOnlyList<GameEvent> events;
    private readonly GameState? state;
    private readonly UnitNames? names;
    private Dictionary<string, long>? revisions;
    private Dictionary<string, long>? rollRevisions;
    private IReadOnlyList<RollRow>? rollRows;
    private IReadOnlyList<(string EventId, string Text)>? ordnance;
    private IReadOnlyList<FireView>? fire;
    private IReadOnlyList<(string EventId, string Text)>? closeCombat;
    private IReadOnlyList<(string EventId, string Text)>? night;
    private IReadOnlyList<(string EventId, string Text)>? snipers;
    private IReadOnlyList<(string EventId, string Kind, string Text)>? rallyAndRepair;
    private Dictionary<string, string>? when;
    private (bool Read, string? Text) latest;

    /// <summary>
    /// The records of <paramref name="history"/> through the revision <paramref name="last"/>, as <paramref name="viewer"/> may read them. With
    /// <paramref name="names"/> (pass 31c, design D11) every unit, weapon, counter, and Location in a record is said in the view's words, and a
    /// unit the view could not name just before the event reads "a concealed unit"; without them a record keeps its identifiers.
    /// </summary>
    public PlayRecords(GameHistory history, Perspective viewer, long last, UnitNames? names = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(viewer);
        (this.history, this.viewer, Last, this.names) = (history, viewer, last, names);
        events = last >= history.Events.Count ? history.Events : [.. history.Events.Where(item => item.Revision <= last)];
        state = history.At(Math.Min(last, history.States.Count));
    }

    public GameHistory History => history;

    public Perspective Viewer => viewer;

    /// <summary>The last revision the records are read through.</summary>
    public long Last
    {
        get;
    }

    /// <summary>The latest six rolls the view may see, latest first.</summary>
    public IReadOnlyList<RollRow> RollRows => rollRows ??= RowsOf(Rolls, RollText);

    /// <summary>The view's names, when the records were read with them.</summary>
    public UnitNames? Names => names;

    /// <summary>
    /// Where a unit or a weapon was just before an event, as " in bd01:G4:1" (the user, 2026-10-04: a record says the hex of the units that act
    /// and of their targets); empty when it was not on the map. The view's words turn the identifier into "G4, level 1".
    /// </summary>
    private static string In(GameState? before, string? id) => id is not null && before?.Location(id)?.Location is { } at ? $" in {at}" : string.Empty;

    private static string From(GameState? before, string? id) => id is not null && before?.Location(id)?.Location is { } at ? $" from {at}" : string.Empty;

    /// <summary>
    /// D6.5, D6.24 (pass 70, ruling R70.32): units that unload or Bail Out where a Known enemy unit stands are in an enemy Location and not held in Melee; the
    /// CC counter is no stored condition, so the line says it. Only a Known enemy unit is read, which every view may know, by the planner's one member that
    /// the proposals read too (fix round C); one stack read per such line.
    /// </summary>
    private static string EnemyLocation(GameState? before, BoardLocation at, string ownId, string rule) =>
        before?.Unit(ownId)?.Side is { } side && GamePlanner.EnemyLocation(before.At(at), side)
            ? ScenarioA1PassengerActionRules.EnemyLocationText(at.ToString(), rule) : string.Empty;

    /// <summary>
    /// A Search's line (A12.152; pass 70, ruling R70.24), public to both sides: what the same attempt revealed after it, a "?" placed over hidden units said
    /// by its Location alone, since its record is the owner's.
    /// </summary>
    private string SearchText(string eventId, UnitsSearched search, GameState? before, int? dr)
    {
        var attempt = AttemptOf(eventId);
        GameEvent[] later = [.. events.Where(other => AttemptOf(other.EventId) == attempt).SkipWhile(other => other.EventId != eventId).Skip(1)];
        string[] placedIn = [.. later.Where(other => other.Type == "hidden-placed" && other.Payload is ConditionsChanged)
            .Select(other => before?.Location(((ConditionsChanged)other.Payload).Id)?.Location.ToString()).OfType<string>().Distinct(StringComparer.Ordinal)];
        return ScenarioA1Searching.RecordText(search.Units, search.Location.ToString(), dr, search.Drm, [.. search.Searched.Select(hex => hex.ToString())],
            later.Count(other => other.Type == "concealment-lost"), placedIn,
            later.Count(other => other.Payload is InstanceEliminated gone && before?.Unit(gone.Id)?.Kind == UnitKinds.Dummy))
            + CasualtyText(later, search.Units, search.CasualtyDrm, before);
    }

    // The events an action that costs a "?" holds besides its losses (A12.141; ruling R70.17): a transfer, a Recovery, a dismantling, a Deployment or a
    // Recombination, a Gun hooked up, a Scrounging. A move, a fire, or a CC, which lose a "?" by other rules, holds others and is not said here.
    private static readonly HashSet<string> LossActionTypes = new(StringComparer.Ordinal)
    {
        "equipment-transferred", "recovery-attempted", "conditions-changed", "dice-rolled", "rph-action-taken", "lineage", "deployment-attempted", "gun-hooked",
        "instance-created", "vehicle-scrounged",
    };

    /// <summary>
    /// The line of a "?" given up or lost by an action (pass 70's fixes; table player's finding 2), for the attempt of <paramref name="item"/>, its first loss:
    /// the voluntary removal, an attempt of reveals and Dummies alone (A12.14, A12.31; ruling R70.20), or the units an action strips of their "?" (A12.141;
    /// ruling R70.17), a Search's or a Mopping Up's searchers before it. Said in the words of the revision after the last loss, since the loss shows the units.
    /// Null for any other attempt, whose own line or rule says it.
    /// </summary>
    private string? ConcealmentLossLine(GameEvent item, GameState? before)
    {
        var attempt = AttemptOf(item.EventId);
        GameEvent[] ofAttempt = [.. events.Where(other => AttemptOf(other.EventId) == attempt)];
        static bool Loss(GameEvent other) => other.Type == "concealment-lost" && other.Payload is ConditionsChanged;
        string Said(GameEvent[] losses, string text) => names is null ? text : names.InText(text, losses[^1].Revision);
        string Units(GameEvent[] losses) => string.Join(", ", losses.Select(loss => ((ConditionsChanged)loss.Payload).Id).Distinct(StringComparer.Ordinal).Select(id => id + In(before, id)));

        if (ofAttempt.All(other => Loss(other) || (other.Payload is InstanceEliminated gone && before?.Unit(gone.Id)?.Kind == UnitKinds.Dummy)))
        {
            GameEvent[] given = [.. ofAttempt.Where(Loss)];
            var hidden = given.Any(loss => before?.Find(((ConditionsChanged)loss.Payload).Id) is { } thing && thing.Conditions.TryGetValue(Conditions.Hidden, out var was) && was == ConditionState.True);
            return Said(given, $"{Units(given)} {(given.Length == 1 ? "gives up its" : "give up their")} \"?\" (A12.14{(hidden ? ", A12.31" : string.Empty)})");
        }

        var anchor = Array.FindIndex(ofAttempt, other => other.Payload is UnitsSearched or BuildingMoppedUp);
        GameEvent[] lost = anchor >= 0 ? [.. ofAttempt.Take(anchor).Where(Loss)]
            : ofAttempt.All(other => Loss(other) || LossActionTypes.Contains(other.Type)) ? [.. ofAttempt.Where(Loss)] : [];
        return lost.Length == 0 ? null
            : Said(lost, $"{Units(lost)} {(lost.Length == 1 ? "loses its" : "lose their")} \"?\" by this action, in the LOS of a Good Order enemy ground unit within 16 hexes (A12.141)");
    }

    /// <summary>
    /// A Mopping Up's line (A12.153, A12.154), public to both sides: the building, where hidden units were placed beneath "?" (by Location alone, since that
    /// record is the owner's; pass 70's fixes), and the DEFENDER's Casualty dr. The hidden-placed records come before it in the attempt, the dr after it.
    /// </summary>
    private string MopUpText(string eventId, BuildingMoppedUp mopped, GameState? before)
    {
        var attempt = AttemptOf(eventId);
        GameEvent[] ofAttempt = [.. events.Where(other => AttemptOf(other.EventId) == attempt)];
        string[] placedIn = [.. ofAttempt.TakeWhile(other => other.EventId != eventId).Where(other => other.Type == "hidden-placed" && other.Payload is ConditionsChanged)
            .Select(other => before?.Location(((ConditionsChanged)other.Payload).Id)?.Location.ToString()).OfType<string>().Distinct(StringComparer.Ordinal)];
        return DisplayText.Hexes($"The {DisplayText.Side(mopped.Side)} side Mops Up building {mopped.Building} (A12.153)", history.States.Count > 0 ? history.States[0].Map.Boards.Count : 1)
            + (placedIn.Length > 0 ? $"; hidden enemy units are placed beneath \"?\" in {string.Join(", ", placedIn)} (A12.153)" : string.Empty)
            + CasualtyText([.. ofAttempt.SkipWhile(other => other.EventId != eventId).Skip(1)], mopped.Units, mopped.CasualtyDrm, before);
    }

    /// <summary>
    /// The DEFENDER's Casualty dr after a Search or a Mopping Up, as "; the DEFENDER's Casualty dr 3 - 2 = 1: ... (A12.154)" (pass 70's fixes); empty when the
    /// attempt made none. Its drm, read from the record (none on one made before the fixes), is said only to the DEFENDER and the adjudicator: it is made of
    /// what his "?" hide. The units Reduced are the searchers the attempt's later events Reduce, wound, or eliminate.
    /// </summary>
    private string CasualtyText(IReadOnlyList<GameEvent> later, IReadOnlyList<string> units, int? drm, GameState? before)
    {
        if (later.Select(other => other.Payload).OfType<DiceRolled>().FirstOrDefault(roll => roll.Purpose == "search-casualty") is not { Values.Count: > 0 } casualty)
        {
            return string.Empty;
        }

        var searching = units.Count > 0 ? before?.Unit(units[0])?.Side : null;
        var mayKnow = viewer.IsAdjudicator || (searching is not null && viewer.Name != searching);
        string[] reduced = [.. units.Where(id => later.Any(other => other.Payload switch
        {
            LineageRecorded { Action: LineageAction.Reduced } lineage => lineage.Consumed.Contains(id, StringComparer.Ordinal),
            ConditionsChanged changed => changed.Id == id && changed.Conditions.TryGetValue(Conditions.Wounded, out var wounded) && wounded == ConditionState.True,
            InstanceEliminated gone => gone.Id == id,
            _ => false,
        }))];
        return "; " + ScenarioA1MoppingUp.CasualtyRecordText(casualty.Values[0], mayKnow ? drm : null, reduced);
    }

    /// <summary>
    /// A record's text in the view's words (design D11): the one place a record's identifiers become names, so no record is worded twice and none
    /// misses the view check (plan section 15.9).
    /// </summary>
    private string Say(string eventId, string text)
    {
        if (names is null)
        {
            return text;
        }

        revisions ??= events.ToDictionary(item => item.EventId, item => item.Revision, StringComparer.Ordinal);
        return names.InText(text, revisions.TryGetValue(eventId, out var revision) ? revision - 1 : Last);
    }

    private string SayRoll(string roll, string text)
    {
        if (names is null)
        {
            return text;
        }

        rollRevisions ??= events.Where(item => item.Payload is DiceRolled).GroupBy(item => ((DiceRolled)item.Payload).Roll, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Revision, StringComparer.Ordinal);
        return names.InText(text, rollRevisions.TryGetValue(roll, out var revision) ? revision - 1 : Last);
    }

    /// <summary>Every roll the view may see among the events of the revisions <paramref name="first"/> to <paramref name="last"/>, in order.</summary>
    public IReadOnlyList<RollRow> RollsIn(long first, long last)
    {
        GameEvent[] seen = [.. events.Where(item => item.IsVisibleTo(viewer))];
        return RowsOf(seen.Where(item => item.Revision >= first && item.Revision <= last).Select(item => item.Payload).OfType<DiceRolled>()
            .Select(roll => (roll, seen.Select(item => item.Payload).OfType<RandomSelection>().FirstOrDefault(selection => selection.Roll == roll.Roll)?.Subjects)), RollText);
    }

    public IReadOnlyList<(string EventId, string Text)> Ordnance => ordnance ??= Said(OrdnanceRecords);

    public IReadOnlyList<FireView> Fire => fire ??= FireRecords;

    public IReadOnlyList<(string EventId, string Text)> CloseCombat => closeCombat ??= Said(CloseCombatRecords);

    public IReadOnlyList<(string EventId, string Text)> Night => night ??= Said(NightRecords);

    public IReadOnlyList<(string EventId, string Text)> Snipers => snipers ??= Said(SniperRecords);

    public IReadOnlyList<(string EventId, string Kind, string Text)> RallyAndRepair =>
        rallyAndRepair ??= names is null ? RallyAndRepairRecords : [.. RallyAndRepairRecords.Select(record => (record.EventId, record.Kind, Say(record.EventId, record.Text)))];

    private IReadOnlyList<(string EventId, string Text)> Said(IReadOnlyList<(string EventId, string Text)> records) =>
        names is null ? records : [.. records.Select(record => (record.EventId, Say(record.EventId, record.Text)))];

    /// <summary>The turn and phase an event happened in, such as "Turn 2, Rally Phase"; null for an event the records do not hold.</summary>
    public string? WhenOf(string eventId)
    {
        ArgumentNullException.ThrowIfNull(eventId);
        if (when is null)
        {
            when = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in events)
            {
                // The phase the event was proposed in: the state before it, or the start's own state.
                if ((history.At(item.Revision - 1) ?? history.At(item.Revision)) is { } before)
                {
                    // Pass 31c (design D17): the heading names the side whose phase it is.
                    when[item.EventId] = $"Turn {before.Turn}, {DisplayText.Side(before.PhasingSide)} {GameText.PhaseLabel(before.Phase)}";
                }
            }
        }

        return when.GetValueOrDefault(eventId);
    }

    /// <summary>
    /// The latest thing the view may read, with its turn and phase, for the activity strip (pass 31c, design D17; play test P-15): the lines of the
    /// last attempt that has any, so a fire reads with its result and what it did, and a move, a pass, a choice, a surrender, a capture, and a
    /// declaration are said too.
    /// </summary>
    public string? Latest
    {
        get
        {
            if (!latest.Read)
            {
                // The last attempt that did something; a phase's start and an answered choice alone are passed over (the table player, pass 31c), so
                // a fire's result is not hidden by the phase that began after it.
                var told = Lines.Where(line => !line.Minor).ToArray();
                var from = told.Length > 0 ? told : [.. Lines];
                var last = from.Length == 0 ? null : AttemptOf(from[^1].EventId);
                var ofLast = last is null ? [] : Lines.Where(line => AttemptOf(line.EventId) == last).ToArray();
                var shown = (ofLast.Any(line => !line.Minor) ? ofLast.Where(line => !line.Minor) : ofLast).ToArray();
                string[] said = [.. shown.Select(line => Sentence(line.Text))];
                // The result of the attempt first, then what followed from it; a long attempt is cut, and the records have the rest.
                latest = (true, said.Length == 0 ? null : $"Latest: {WhenOf(from[^1].EventId)}: {string.Join(" ", said.Take(4))}{(said.Length > 4 ? " More is in the records." : string.Empty)}");

                // The user, 2026-10-21: a CC round in the latest attempt is shown as its table, the other lines of the attempt as text.
                latestTables = [.. shown.Select(line => CombatTable(line.EventId)).OfType<CloseCombatTable>()];

                // A fire in the latest attempt is shown as its card (the Records tab's), under its line (the user, 2026-10-21).
                var fires = Fire.ToDictionary(view => view.EventId, StringComparer.Ordinal);
                latestFires = [.. shown.Where(line => fires.ContainsKey(line.EventId)).Select(line => (fires[line.EventId], Sentence(line.Text)))];
                latestOthers = [.. shown.Where(line => CombatTable(line.EventId) is null && !fires.ContainsKey(line.EventId)).Select(line => Sentence(line.Text))];
                latestWhen = from.Length == 0 ? null : WhenOf(from[^1].EventId);
            }

            return latest.Text;
        }
    }

    private IReadOnlyList<(FireView View, string Line)> latestFires = [];

    /// <summary>The fire attacks of the latest attempt, each with its line, to show as their cards (the user, 2026-10-21); empty when it holds none.</summary>
    public IReadOnlyList<(FireView View, string Line)> LatestFires
    {
        get
        {
            _ = Latest;
            return latestFires;
        }
    }

    private IReadOnlyList<CloseCombatTable> latestTables = [];
    private IReadOnlyList<string> latestOthers = [];
    private string? latestWhen;

    /// <summary>The CC rounds of the latest attempt as tables (the user, 2026-10-21); empty when it holds none.</summary>
    public IReadOnlyList<CloseCombatTable> LatestTables
    {
        get
        {
            _ = Latest;
            return latestTables;
        }
    }

    /// <summary>The latest attempt's lines that are no CC round, beside its tables.</summary>
    public IReadOnlyList<string> LatestOthers
    {
        get
        {
            _ = Latest;
            return latestOthers;
        }
    }

    /// <summary>The turn and phase of the latest attempt.</summary>
    public string? LatestWhen
    {
        get
        {
            _ = Latest;
            return latestWhen;
        }
    }

    private IReadOnlyList<(long Revision, string EventId, string Text, bool Minor)>? lines;

    /// <summary>
    /// Everything the view may read of the game, in the order it happened, one line for each thing (design D17): the records, a fire with its
    /// result and what it changed, and the events the records leave out: a move, an advance, the DEFENDER's pass, a move's end, a choice, a
    /// surrender, a capture, a declaration, a phase's start. Each line is said in the view's words, with the view check of a name.
    /// </summary>
    public IReadOnlyList<(long Revision, string EventId, string Text, bool Minor)> Lines => lines ??= ReadLines();

    /// <summary>The lines of what happened after a revision: what the view missed while another had the screen ("Since you last looked").</summary>
    public IReadOnlyList<(long Revision, string EventId, string Text, bool Minor)> Since(long revision, long through = long.MaxValue) =>
        [.. Lines.Where(line => line.Revision > revision && line.Revision <= through)];

    private List<(long Revision, string EventId, string Text, bool Minor)> ReadLines()
    {
        var revisionOf = events.ToDictionary(item => item.EventId, item => item.Revision, StringComparer.Ordinal);
        var read = new List<(long Revision, string EventId, string Text, bool Minor)>();
        void Add(string eventId, string text, bool minor = false)
        {
            if (revisionOf.TryGetValue(eventId, out var revision))
            {
                read.Add((revision, eventId, text, minor));
            }
        }

        foreach (var record in Ordnance.Concat(CloseCombat).Concat(Night).Concat(Snipers).Concat(RallyAndRepair.Select(item => (item.EventId, item.Text))))
        {
            Add(record.EventId, record.Text);
        }

        foreach (var fire in Fire)
        {
            Add(fire.EventId, FireLine(fire));
        }

        // Pass 31d (design D5; ruling R31d.3): a Dummy stack's removal is said to both sides, who both see the counters leave the map (A12.11, A12.15,
        // A11.19). One line for a stack, whatever its count of counters; removal by fire is said in the fire's own line.
        var dummiesSaid = new HashSet<(string Attempt, string At, string? Side)>();

        // Pass 65 (the table player): the dice of each roll a record names by its id, and the vehicles each attempt marked Abandoned (D6.83), read once.
        var dice = events.Select(item => item.Payload).OfType<DiceRolled>().GroupBy(roll => roll.Roll, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Values, StringComparer.Ordinal);
        var abandoning = events.Where(item => item.Payload is ConditionsChanged changed && changed.Conditions.TryGetValue(Conditions.Abandoned, out var abandonedNow)
                && abandonedNow == ConditionState.True)
            .Select(item => (AttemptOf(item.EventId), ((ConditionsChanged)item.Payload).Id)).ToHashSet();
        var lossesSaid = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in events.Where(item => item.IsVisibleTo(viewer)))
        {
            var before = history.At(item.Revision - 1);
            static string Many(IReadOnlyList<string> ids, string one, string several) => $"{string.Join(", ", ids)} {(ids.Count == 1 ? one : several)}";
            if (item.Payload is InstanceEliminated removed && before?.Unit(removed.Id) is { Kind: UnitKinds.Dummy } dummy && before.Location(dummy.Id)?.Location is { } dummyAt)
            {
                var attempt = AttemptOf(item.EventId);
                var ofAttempt = events.Where(other => AttemptOf(other.EventId) == attempt).Select(other => other.Payload).ToArray();
                // The referee, pass 31d: a fire's own line says it only where the view reads that fire's effects and this Dummy is among them; a
                // fire withheld from the view, and a Residual FP attack in the attempt of a move that removed the stack, leave the line to be said.
                var inFireLine = Fire.Any(view => AttemptOf(view.EventId) == attempt && view.Effects?.Any(effect => effect.UnitId == dummy.Id && effect.Events.Contains("dummy-removed", StringComparer.Ordinal)) == true);
                if (!inFireLine && dummiesSaid.Add((attempt, dummyAt.ToString(), dummy.Side)))
                {
                    Add(item.EventId, Say(item.EventId, $"{DisplayText.ASide(dummy.Side)} Dummy stack is removed in {dummyAt}"
                        + (ofAttempt.OfType<MovementStepped>().Any(step => step.Movers.Contains(dummy.Id, StringComparer.Ordinal))
                            ? ": it moved without Assault Movement, or into Open Ground, in the LOS of a Good Order enemy unit (A12.11)"
                            : ofAttempt.Any(payload => payload is MovementStepped or EntryAttempted or EntryForcedBack) ? ": a unit tried to enter its Location, and it held no real unit (A12.15)"
                            : ofAttempt.Any(payload => payload is FireResolved or FireReported or OrdnanceFired) ? ": an attack reached it (A12.14)"
                            : string.Empty)));
                }

                continue;
            }

            // A11.19: the Dummies in a Location that holds units of both sides are removed as the CCPh begins. The game records no event for it,
            // so the line is read from the states on each side of the phase change.
            if (item.Payload is PhaseChanged { Phase: "ccph" } && before is not null && history.At(item.Revision) is { } begun)
            {
                foreach (var gone in before.Units.Where(unit => unit.Kind == UnitKinds.Dummy && unit.Status == InstanceStatus.Active && begun.Unit(unit.Id) is { Status: InstanceStatus.Eliminated }
                        && before.Location(unit.Id) is not null)
                    .GroupBy(unit => (At: before.Location(unit.Id)!.Location, unit.Side)).OrderBy(group => group.Key.At.ToString(), StringComparer.Ordinal))
                {
                    Add(item.EventId, Say(item.EventId, $"The {DisplayText.Side(gone.Key.Side)} Dummies in {gone.Key.At} are removed before Close Combat (A11.19)"));
                }
            }

            // Pass 70's fixes (A12.14, A12.141; rulings R70.17, R70.20): a "?" given up, or lost by an action, has a line, one an attempt.
            if (item.Type == "concealment-lost" && item.Payload is ConditionsChanged && lossesSaid.Add(AttemptOf(item.EventId)) && ConcealmentLossLine(item, before) is { } loss)
            {
                Add(item.EventId, loss);
                continue;
            }

            var text = item.Payload switch
            {
                // A12.15 (pass 70's fixes): a step forced back stays where it was, and says the Location it tried to enter, not "moves from [F5] to [F5]".
                MovementStepped { Attempted: { } tried } forced => (forced.Movers.Count == 1 ? $"{forced.Movers[0]} tries" : $"{string.Join(", ", forced.Movers)} try")
                    + $" to enter {tried}{From(before, forced.Movers.Count > 0 ? forced.Movers[0] : null)} and {(forced.Movers.Count == 1 ? "is" : "are")} forced back (A12.15)",
                MovementStepped step => Many(step.Movers, "moves", "move") + $"{From(before, step.Movers.Count > 0 ? step.Movers[0] : null)} to {step.To}{(step.Assault ? " by Assault Movement" : string.Empty)}",
                // D2.17, D3.12 (pass 50): a Delay MP, and a turret turned with an MP expenditure.
                VehicleStepped { Kind: VehicleStepped.Turn, Turret: { } turnTurret } turnedBoth => $"{turnedBoth.Vehicle} turns its turret to {UnitFacings.Name(turnTurret)} with its VCA change (D3.12)",
                VehicleStepped { Kind: VehicleStepped.Delay } delay => $"{delay.Vehicle} spends a Delay MP in {delay.At} (D2.17)"
                    + (delay.Turret is { } delayTurret ? $", turning its turret to {UnitFacings.Name(delayTurret)} (D3.12)" : string.Empty),
                // D6.4, D6.5 (pass 65, ruling R65.15): a load and an unload name who boards or gets off.
                VehicleStepped { Kind: VehicleStepped.Load, Units: { Count: > 0 } boarders } boarding => $"{boarding.At}: " + Many(boarders, "boards", "board") + $" {boarding.Vehicle}"
                    + (boarding.Riders ? " as Riders (D6.2, D6.4)" : " (D6.4)"),
                // D6.21, D6.23, D6.24 (pass 65, ruling R65.17): a Rider's Bail Out says why, its NMC, and what became of its SW.
                // The table player: the NMC's dice and total, and a Casualty Reduction said as such (D6.24, A10.31).
                RiderBailedOut bailed => $"{bailed.At}: {bailed.Unit} Bails Out of {bailed.Vehicle}, since " + ScenarioA1RiderRules.CauseText(bailed.Cause)
                    + "; NMC" + (dice.TryGetValue(bailed.Roll, out var bailDice) ? $" DR {string.Join(", ", bailDice)} = {bailDice.Sum()}" : string.Empty)
                    // Pass 70 (ruling R70.9): a Rider leader's DRM, said when there is one.
                    + (bailed.Drm is { } bailDrm && bailDrm != 0 && bailDice is not null ? $" {(bailDrm < 0 ? "-" : "+")} {Math.Abs(bailDrm)} (leadership, A10.7) = {bailDice.Sum() + bailDrm}" : string.Empty)
                    + $" against {bailed.Morale}: {BailOutResult(bailed.Result)}"
                    + string.Concat((bailed.Weapons ?? []).Select(weapon => $"; {weapon.Weapon} {(weapon.Fate == ScenarioA1RiderRules.Kept ? "is kept" : $"is {weapon.Fate}")}"
                        + (weapon.Roll is { } weaponRoll && dice.TryGetValue(weaponRoll, out var weaponDice) ? $" on a dr of {weaponDice[0]}" : string.Empty)))
                    + " (D6.24)" + EnemyLocation(before, bailed.At, bailed.Unit, "D6.24, D6.5"),
                // D5.41, D5.411, D6.631, D5.42 (pass 65, ruling R65.20): the crew's own acts, with their rules in one parenthesis (the table player).
                // Pass 70 (rulings R70.3, R70.4): a morale check made outside an attack is among the Rally, rout, and other actions above.
                VehicleAbandoned abandoned => $"{abandoned.At}: {abandoned.Crew} Abandons {abandoned.Vehicle}"
                    + (abandoned.Destroyed == ScenarioA1CrewActsRules.Wreck ? ", leaving it a wreck"
                        : abandoned.Destroyed == ScenarioA1CrewActsRules.Malfunction ? ", malfunctioning its weapons" : string.Empty)
                    + (abandoned.Removed is { } removedMg ? $", taking its MG as {removedMg}" : string.Empty)
                    + $" (D5.41{(abandoned.Destroyed is not null ? ", D5.411" : string.Empty)}{(abandoned.Removed is not null ? ", D6.631" : string.Empty)})",
                VehicleRecrewed recrewed => $"{recrewed.At}: {recrewed.Unit} enters {recrewed.Vehicle} and crews it" + (recrewed.Temporary ? " as a Temporary crew" : string.Empty)
                    + (recrewed.Returned is { } returnedMg ? $", returning {returnedMg}" : string.Empty)
                    + $" (D5.42{(recrewed.Temporary ? ", A21.22" : string.Empty)}{(recrewed.Returned is not null ? ", D6.631" : string.Empty)})",
                // D10.5, D10.52 (pass 65, ruling R65.23): a Scrounging, its dr, and what it salvaged.
                VehicleScrounged scrounged => $"{scrounged.At}: {scrounged.Unit} scrounges {scrounged.Vehicle} on a dr of {scrounged.Dr}: "
                    + (scrounged.Taken is { Count: > 0 } found ? "it salvages " + string.Join(" and ", found.Select(item => $"{item.Weapon}, its {ScenarioA1WreckRules.MountText(item.Mount)}"))
                        : "it salvages nothing")
                    + $"; {scrounged.Vehicle} is Scrounged, and {scrounged.Unit} is TI (D10.5)",
                // A11.52, A21.2, A21.21 (pass 65, ruling R65.22): a capture names the captor's side.
                // The table player: the hex said once, after the vehicle, so the other side's vehicle named by its hex does not say it twice.
                VehicleCaptured captured => $"{captured.Vehicle} in {captured.At} is captured by the {DisplayText.Side(captured.Side)} side"
                    + (captured.Cause == VehicleCaptured.Passengers ? " with all its Passengers, and is unmanned until a captor enters it (A11.52, D5.42)"
                        : captured.Cause != VehicleCaptured.Unarmed ? " (A21.2)"
                        : captured.Driver is { } driver ? $", {driver} removed into it as its Temporary Driver (A11.52, A21.21)" : ", a Temporary Driver running it for its captor (A11.52, A21.21)"),
                // D6.83 (pass 65, the table player): a Carrier whose inherent HS unloads with the step is Abandoned by it.
                VehicleStepped { Kind: VehicleStepped.Unload, Units: { Count: > 0 } leavers } leaving => $"{leaving.At}: " + Many(leavers, "unloads", "unload") + $" from {leaving.Vehicle} (D6.5)"
                    + (abandoning.Contains((AttemptOf(item.EventId), leaving.Vehicle)) ? $"; its inherent HS has left it, so {leaving.Vehicle} is Abandoned (D6.83)" : string.Empty)
                    + EnemyLocation(before, leaving.At, leaving.Vehicle, "D6.5"),
                // D5.43 (pass 65, the table player): a loader the fire its boarding drew pinned or broke, or whose vehicle it destroyed, is back on foot.
                InstanceMoved { Ended: false, Mf: null, Position: MapPosition } back when before?.Unit(back.Id) is { Position: ContainedPosition { Role: ContainmentRole.Passenger or ContainmentRole.Rider } boarded } loader =>
                    $"{back.Id} does not board {boarded.Container}: "
                    + (GameState.Condition(loader, Conditions.Broken) == ConditionState.True ? "broken by the fire its boarding drew"
                        : GameState.Condition(loader, Conditions.Pinned) == ConditionState.True ? "pinned by the fire its boarding drew"
                        : $"the fire its boarding drew destroys {boarded.Container}") + ", so it is back on foot (D5.43)",
                // A21.22 (pass 65, the table player): a Temporary crew's start dr.
                VehicleCheckRolled { Check: VehicleCheckRolled.TemporaryStart } start => $"{start.Vehicle}'s Temporary crew makes its start dr"
                    + (dice.TryGetValue(start.Roll, out var startDice) ? $" {startDice[0]}" + (start.Drm != 0 ? $" + {start.Drm} = {startDice[0] + start.Drm}" : string.Empty) : string.Empty)
                    + ": " + (start.Result switch
                    {
                        VehicleCheckRolled.Passed => "it starts",
                        VehicleCheckRolled.NoMove => "no movement this phase",
                        _ => "it Bogs",
                    }) + " (A21.22)",
                // A11.5, A11.51, A11.62, A21.2 (pass 65, the table player): a CC attack with a vehicle, a capture attempt's among them, with its arithmetic.
                VehicleCloseCombatResolved combat => VehicleCloseCombatText(combat),
                VehicleStepped { Kind: not VehicleStepped.Turn } drive => $"{drive.Vehicle} moves{From(before, drive.Vehicle)} to {drive.At}"
                    + (drive.Turret is { } driveTurret ? $", turning its turret to {UnitFacings.Name(driveTurret)} (D3.12)" : string.Empty),
                AdvanceMoved advance => Many(advance.Units, "advances", "advance") + (advance.Exit is { } edge ? $" off the {edge} edge from {advance.To}" : $"{From(before, advance.Units.Count > 0 ? advance.Units[0] : null)} to {advance.To}"),
                MovementWindowClosed => $"The {DisplayText.Side(before?.Sides.FirstOrDefault(side => side.Id != before.PhasingSide)?.Id)} side, the DEFENDER, declines First Fire",
                MovementEnded ended => Many(ended.Movers, "ends its move", "end their move") + In(before, ended.Movers.Count > 0 ? ended.Movers[0] : null),
                OpportunityFireDeclared held => string.Join(", ", held.Units.Select(id => id + In(before, id))) + (held.Units.Count == 1 ? " is" : " are") + " held for Opportunity Fire (A7.25)",
                // C2.2401 (pass 55): the ATTACKER's answer to a declared shot; the declaration and a void return shot are among the ordnance records.
                ChoiceMade answered when before?.Choice is { Kind: ChoicePending.GunDuel } duel && GunDuelParties(duel.Resume) is (var answeredFirer, var answeredVehicle) =>
                    ScenarioA1GunDuels.AnswerText(answered.Option, answeredVehicle, answeredFirer),
                ChoiceMade => "A choice the game waited for was answered",
                SurrenderPending surrender => $"{surrender.Unit}{In(before, surrender.Unit)} surrenders, and its captor is to be chosen (A20.21)",
                SurrenderRejected refused => $"The surrender of {refused.Unit} is refused (A20.3)",
                InstanceCaptured taken => $"{taken.Id}{In(before, taken.Id)} is captured, guarded by {taken.Custodian} (A20.2)",
                PrisonerFreed freed => $"{freed.Unit}{In(before, freed.Unit)} is no longer guarded (A20.5)",
                // A12.152 (pass 70, ruling R70.24): a Search, its dr, the hexes searched, and what it found, said to both sides.
                UnitsSearched search => SearchText(item.EventId, search, before, dice.TryGetValue(search.Roll, out var searchDice) ? (int?)searchDice[0] : null),
                BuildingMoppedUp mopped => MopUpText(item.EventId, mopped, before),
                PhaseChanged phase => $"The {DisplayText.Side(phase.PhasingSide)} {GameText.PhaseLabel(phase.Phase)} of Turn {phase.Turn} begins",
                GameEnded => "The game ends",
                _ => null,
            };
            if (text is not null)
            {
                Add(item.EventId, Say(item.EventId, text), item.Payload is PhaseChanged or ChoiceMade);
            }
        }

        // Pass 70's fixes (A12.32, A12.34): hidden units placed beneath "?" outside a Search or a Mopping Up, whose lines say it. The record is the owner's;
        // the line, public, says only where a "?" appears, as the map shows it: one a Location and attempt.
        foreach (var group in events.Where(item => item.Type == "hidden-placed" && item.Payload is ConditionsChanged)
            .GroupBy(item => AttemptOf(item.EventId), StringComparer.Ordinal))
        {
            if (events.Any(other => AttemptOf(other.EventId) == group.Key && other.Payload is UnitsSearched or BuildingMoppedUp))
            {
                continue;
            }

            foreach (var placed in group.Select(item => (Event: item, Before: history.At(item.Revision - 1), Id: ((ConditionsChanged)item.Payload).Id))
                .Select(item => (item.Event, Thing: item.Before?.Find(item.Id), At: item.Before?.Location(item.Id)?.Location))
                .Where(item => item.Thing is not null && item.At is not null).GroupBy(item => item.At!.ToString(), StringComparer.Ordinal))
            {
                var (first, _, at) = placed.First();
                var gun = placed.Any(item => item.Thing is EquipmentInstance { Kind: "asl:gun" });
                var side = placed.Select(item => item.Thing!.Side).FirstOrDefault(found => found is not null);
                Add(first.EventId, Say(first.EventId, $"{DisplayText.ASide(side)} \"?\" is placed in {at} ({(gun ? "A12.34" : "A12.32")})"));
            }
        }

        // The order it happened in; lines of one event keep the order they were read in.
        return [.. read.Select((line, index) => (line, index)).OrderBy(pair => pair.line.Revision).ThenBy(pair => pair.index).Select(pair => pair.line)];
    }

    /// <summary>The firer and the moving vehicle of a pending Gun Duel (C2.2401; pass 55), from its resume data; null when it names neither.</summary>
    private static (string Firer, string Vehicle)? GunDuelParties(JsonElement resume) =>
        resume.ValueKind == JsonValueKind.Object && resume.TryGetProperty("firer", out var firer) && firer.GetString() is { } firerId
            && resume.TryGetProperty("vehicle", out var vehicle) && vehicle.GetString() is { } vehicleId ? (firerId, vehicleId) : null;

    /// <summary>A fire in one line (play test P-15): who fired at what, the result, and what it did to each unit the view may read of.</summary>
    private static string FireLine(FireView fire)
    {
        var changed = fire.Effects is null ? []
            : fire.Effects.Select(effect => (effect.UnitId, Text: EffectText(effect, fire.BrokenBefore.Contains(effect.UnitId), fire.PinnedBefore.Contains(effect.UnitId))))
                .Where(effect => effect.Text is not ("unaffected" or "already broken" or "already pinned")).Select(effect => fire.Say($"{effect.UnitId}: {effect.Text}")).ToArray();

        // Pass 31d (design D11): a result that asked a check of a unit and changed nothing says who passed, so "PTC." does not stand alone.
        if (changed.Length == 0 && fire.Effects is not null)
        {
            changed = [.. fire.Effects.Where(effect => effect.Checks.Count > 0 && effect.Checks.All(check => check.Passed)).Select(effect => fire.Say($"{effect.UnitId}: passed"))];
        }

        // Pass 31d (design D5): Dummies removed by the attack are said once, as what they were (A12.14).
        if (fire.Effects is not null && fire.Effects.Any(effect => effect.Events.Contains("dummy-removed", StringComparer.Ordinal)))
        {
            changed = [.. fire.Effects.Where(effect => !effect.Events.Contains("dummy-removed", StringComparer.Ordinal))
                    .Select(effect => (effect.UnitId, Text: EffectText(effect, fire.BrokenBefore.Contains(effect.UnitId), fire.PinnedBefore.Contains(effect.UnitId))))
                    .Where(effect => effect.Text is not ("unaffected" or "already broken" or "already pinned")).Select(effect => fire.Say($"{effect.UnitId}: {effect.Text}")),
                "the Dummies there are removed (A12.14)"];
        }

        // D.8B, D6.66, D6.9 (pass 65, the table player): what the attack did to the PRC of the vehicles it struck, as the ordnance records say it.
        if (fire.Effects is not null && fire.Vehicles?.SelectMany(vehicle => PrcLines(vehicle)).ToArray() is { Length: > 0 } prc)
        {
            changed = [.. changed, .. prc.Select(line => fire.Say(line))];
        }

        // The table player's review of pass 55: a Canister attack on a Location where the view knows of no unit.
        if (fire.NoKnownTarget)
        {
            return $"{fire.Group} {(fire.Several ? "fire" : "fires")} at {fire.Target}: {FireText.NoKnownTargetText(fire.Target)}";
        }

        // Pass 31d (design D7; A6.11, p. 53): a blocked LOS is said as what it was, and not as an attack that missed.
        return fire.LosBlocked
            ? $"{fire.Group} {(fire.Several ? "fire" : "fires")} at {fire.Target}: the LOS is blocked, so the attack has no effect and its firers have fired (A6.11)"
            : $"{fire.Group} {(fire.Several ? "fire" : "fires")} at {fire.Target}: {ResultText(fire.Arithmetic.Result)}" + (changed.Length > 0 ? "; " + string.Join("; ", changed) : string.Empty);
    }

    /// <summary>A Bail Out NMC's result in words (D6.24, A10.31; pass 65, the table player): a Casualty Reduction is said with the break it brings.</summary>
    private static string BailOutResult(string result) => result == ScenarioA1RiderRules.Reduced ? "Casualty Reduced and broken"
        : result == ScenarioA1RiderRules.Replaced ? "failed by more than its ELR, Replaced by a broken unit of lesser quality (A19.13)" : result;

    /// <summary>
    /// A morale check made outside an attack in words (pass 70, rulings R70.3, R70.4): "bd03:F8:0: de-s takes a LLMC after the loss of de-l to Interdiction
    /// (A10.53): LLMC 3, 3 + 1 (leadership reversed, de-l) = 7 against 7: passed, unaffected (A10.2)"; the Heat of Battle of an Interdiction NMC's Original 2
    /// says that, with what it did. Null when the record cannot be read.
    /// </summary>
    private static string? MoraleCheckText(MoraleCheckResolved check)
    {
        if (check.Resolution.Deserialize<FireResolution>(LiveFire.Json) is not { } resolution || check.Facts.Deserialize<MoraleCheckFacts>(LiveFire.Json) is not { } facts)
        {
            return null;
        }

        string[] lost = [.. (facts.LostLeaders ?? []).Concat(facts.BrokenLeaders ?? []).Select(item => item.UnitId ?? string.Empty)];
        var cause = ScenarioA1MoraleCheck.CauseText(check.Cause);
        var parts = new List<string>();
        foreach (var effect in resolution.Effects.Where(item => item.Checks.Count > 0 || item.HeatOfBattle is not null))
        {
            var leaderLoss = effect.Checks.Where(item => item.Kind is "LLMC" or "LLTC").ToArray();
            parts.Add(leaderLoss.Length > 0
                // Pass 70 (ruling R70.11): a leader who broke and was not lost is said to have broken.
                ? $"{effect.UnitId} takes {string.Join(" and ", leaderLoss.Select(item => item.Kind))} after the {(facts.LostLeaders is { Count: > 0 } ? "loss" : "break")} of {string.Join(", ", lost)} to {cause}: "
                    + string.Join(", ", leaderLoss.Select(FireText.CheckText)) + $", {FireText.EffectText(effect)} (A10.2)"
                // The Heat of Battle words end with their own rule (A15.1), so the clause before them does not cite it again (pass 70's fixes).
                : $"{effect.UnitId}'s {(check.Cause == ScenarioA1MoraleCheck.Interdiction ? "Interdiction NMC" : "NMC")} is an Original 2, which calls for Heat of Battle: {FireText.EffectText(effect)}");
        }

        foreach (var effect in resolution.CompanionEffects ?? [])
        {
            parts.Add($"{effect.UnitId}: {FireText.EffectText(effect)} (A15.41)");
        }

        return parts.Count == 0 ? null : $"{check.Location}: {string.Join("; ", parts)}";
    }

    /// <summary>
    /// A CC attack with a vehicle in one line (A11.5, A11.51, A11.62, A21.2; pass 65, the table player): Infantry attacking the vehicle, a capture attempt
    /// among them, with the Kill Number, the DR with each DRM and its rule, and the result; or the vehicle attacking Infantry, each defender's Final DR and
    /// result. Null for a record that resolved nothing.
    /// </summary>
    private static string? VehicleCloseCombatText(VehicleCloseCombatResolved combat)
    {
        if (combat.Resolution.ValueKind != JsonValueKind.Object || combat.Resolution.Deserialize<VehicleCloseCombatResolution>(LiveFire.Json) is not { Dice.Count: > 0 } resolution)
        {
            return null;
        }

        if (combat.ByVehicle)
        {
            return $"{combat.Vehicle} attacks {string.Join(", ", combat.Defenders)} in CC" + (resolution.Odds is { } odds ? $" at {odds} odds" : string.Empty) + ": "
                + string.Join("; ", (resolution.Defending ?? []).Select(defender => $"{defender.UnitId} Final DR {defender.FinalDr}{ModifierText(defender.Drm)}: {defender.Result}"))
                + " (A11.62)";
        }

        var capture = combat.Facts.ValueKind == JsonValueKind.Object && combat.Facts.TryGetProperty("capture", out var declared) && declared.ValueKind == JsonValueKind.True;
        var result = resolution.VehicleResult switch
        {
            VehicleCloseCombatResolution.Captured => "it is captured",
            VehicleCloseCombatResolution.BurningWreck => "it burns (a burning wreck)",
            VehicleCloseCombatResolution.Eliminated => "it is eliminated (a wreck)",
            VehicleCloseCombatResolution.Immobilized => "it is immobilized",
            _ => "no effect",
        };
        return $"{string.Join(" and ", combat.Attackers)} {(combat.Attackers.Count == 1 ? "attacks" : "attack")} {combat.Vehicle} in CC{(capture ? " to capture it" : string.Empty)}: "
            + $"Kill Number {resolution.KillNumber}{(resolution.KillNumberModifiers.Count > 0 ? ", with" + ModifierText(resolution.KillNumberModifiers) : string.Empty)}; "
            + $"DR {string.Join(", ", resolution.Dice)} = {resolution.OriginalDr}{ModifierText(resolution.Drm)} = {resolution.FinalDr}"
            + (resolution.UnlikelyKillDr is { } unlikely ? $", Unlikely Kill dr {unlikely}" : string.Empty)
            + $": {result} (A11.5{(capture ? ", A21.2" : string.Empty)})"
            // A18.12 (pass 70, ruling R70.34): the Leader Creation dr after a MMC's Original 2, and the leader it created, by his definition.
            + (resolution.LeaderCreation is { } creation ? $"; the Original 2 draws a Leader Creation dr {creation.Dr}, Final {creation.FinalDr}: "
                + (creation.LeaderDefinitionId is { } leader ? $"a {leader} is created and joins the attack (A18.12)" : "no leader is created (A18.12)") : string.Empty);
    }

    private static string Sentence(string text) => DisplayText.Sentence(text.TrimEnd()) + (text.TrimEnd().EndsWith('.') ? string.Empty : ".");

    /// <summary>
    /// The rolls the viewer may see, latest first, with the unit each die belongs to when the viewer may see that too:
    /// the adjudicator, or the side whose units were selected (Random Selection and Declined OVR Design, section 7).
    /// </summary>
    private IReadOnlyList<(DiceRolled Roll, IReadOnlyList<string>? Subjects)> Rolls => [.. events.Where(item => item.IsVisibleTo(viewer)).Select(item => item.Payload).OfType<DiceRolled>().Reverse().Take(6)
            .Select(roll => (roll, events.Where(item => item.IsVisibleTo(viewer)).Select(item => item.Payload).OfType<RandomSelection>()
                .FirstOrDefault(selection => selection.Roll == roll.Roll)?.Subjects))];

    /// <summary>
    /// A roll with its purpose; an NTC also shows its arithmetic from the task check that used it (Infantry OVR Design,
    /// section 7), for example "OVR NTC: 3, 5 + 3 (TEM) = 11 against morale 7: failed".
    /// </summary>
    private string RollText(DiceRolled roll)
    {
        var check = events.Select(item => item.Payload).OfType<TaskCheck>().FirstOrDefault(item => item.Roll == roll.Roll);
        if (check is null)
        {
            return $"{DisplayText.RollPurpose(roll.Purpose)}: {string.Join(", ", roll.Values)}";
        }

        var modifiers = string.Concat(check.Modifiers.Select(item => $" {(item.Value < 0 ? "-" : "+")} {Math.Abs(item.Value)} ({(item.Source == "B23.3" ? "TEM" : item.Source)})"));
        return $"{(check.Purpose == TaskCheck.OvrNtc ? "OVR NTC" : DisplayText.RollPurpose(check.Purpose))}: {string.Join(", ", roll.Values)}{modifiers} = {check.FinalDr} "
            + $"against morale {check.MoraleLevel}: {(check.Passed ? "passed" : "failed")}";
    }

    private IReadOnlyList<RollRow> RowsOf(IEnumerable<(DiceRolled Roll, IReadOnlyList<string>? Subjects)> rolls, Func<DiceRolled, string> RollText) => [.. rolls.Select(item => new RollRow(item.Roll.Roll,
        SayRoll(item.Roll.Roll, RollText(item.Roll) + (item.Subjects is null ? "" : $" for {string.Join(", ", item.Subjects)}")),
        $"{item.Roll.Count} d{item.Roll.Sides}, drawn by the {item.Roll.Source} for {item.Roll.Actor}"))];

    /// <summary>
    /// The ordnance records the viewer may see, latest first, in words: the Basic and Modified TH# (C3.3, C4), the To Hit DR with its colored die
    /// and DRM (C5, C6), whether it hit or made a Critical Hit (C3.7), the Gun's ROF, malfunction, and Acquisition, and the IFT result of each hit.
    /// </summary>
    private IReadOnlyList<(string EventId, string Text)> OrdnanceRecords
    {
        get
        {
            var records = new List<(string, string)>();
            foreach (var item in events.Where(item => item.IsVisibleTo(viewer)))
            {
                // C2.2401 (pass 55): a shot declared at a moving vehicle that may fire its MA first, and a declared shot that first shot voided.
                if (item.Payload is ChoicePending { Kind: ChoicePending.GunDuel } declared && GunDuelParties(declared.Resume) is (var duelFirer, var duelVehicle))
                {
                    records.Add((item.EventId, ScenarioA1GunDuels.DeclarationText(duelFirer, In(history.At(item.Revision - 1), duelFirer), duelVehicle)));
                    continue;
                }

                if (item.Payload is GunDuelVoided voided)
                {
                    records.Add((item.EventId, ScenarioA1GunDuels.VoidText(voided.Firer, voided.Cause, voided.Reason)));
                    continue;
                }

                if (item.Payload is OrdnanceFired { } checking && checking.Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json) is { ToHit: null, PanzerfaustCheck: { } failed })
                {
                    // C13.31 (ruling R9.7): a PF Check that gave no shot.
                    records.Add((item.EventId, $"{checking.Crew}{In(history.At(item.Revision - 1), checking.Crew)} makes a PF Check: dr {failed.Dr}{ModifierText(failed.Drm)} = {failed.FinalDr}: "
                        + (failed.Outcome == OrdnancePanzerfaustCheck.NoShot ? "no shot (C13.31)" : $"no PF in position, and {failed.Outcome} (C13.31)")));
                    continue;
                }

                // C8.4 (pass 55, ruling R55.9): a Canister shot makes no To Hit DR; its IFT DR decides the round, the Gun, and each Location's attack,
                // which is a fire record of its own.
                if (item.Payload is OrdnanceFired canisterShot
                    && canisterShot.Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json) is { Canister: { } canister, Gun: { } canisterGun } canisterResolution)
                {
                    // The UI review of pass 55: the number is the record's own, never the catalog's of a later day.
                    var fact = canisterShot.Facts.Deserialize<OrdnanceShot>(LiveFire.Json);
                    var number = ScenarioA1Definitions.DepletionNumberWords(canister.DepletionNumber, fact?.Elite == true);
                    var round = canisterResolution.AmmunitionUse switch
                    {
                        "none" => $"the DR is above the {number}: it had no Canister, attacks nothing, "
                            + (canisterGun.Malfunctioned ? "and has none for the rest of the scenario (C8.9)" : "has not fired, and has none for the rest of the scenario (C8.9)"),
                        "depleted" => $"the DR equals the {number}: its last Canister (C8.9)",
                        _ => $"the DR is below the {number}: Canister fired (C8.9)",
                    };
                    var attacked = canister.Attacks.Count == 0 ? null : "it attacks " + string.Join(", ", canister.Attacks.Select(attack => $"{attack.LocationId} with {Number(attack.Firepower)} FP"
                        + (attack.Multipliers.Count > 0 ? $" ({string.Join(", ", attack.Multipliers.Select(multiplier => ScenarioA1CanisterCalculator.HalvingWords(multiplier.Name)))})" : "")))
                        + ", each with its own TEM and Hindrance, and at half FP again against concealed units (C8.4)";
                    string?[] canisterParts =
                    [
                        $"{canisterShot.Gun}{In(history.At(item.Revision - 1), canisterShot.Gun)} fires Canister at {ScenarioA1CanisterCalculator.AimWords(canister.Aim, canister.Facing)}"
                            + (canisterShot.Facing is { } turned ? $", turning to {UnitFacings.Name(turned)}: Case A +{canister.CaseA} on the IFT DR (D3.52)" : "")
                            + $": no To Hit DR; IFT DR {string.Join(", ", canister.Dice.Select((die, index) => index == 0 ? $"{die} (colored)" : $"{die}"))} = {canister.OriginalDr}, "
                            + "which is also its Depletion, malfunction, and ROF check (C8.4)",
                        round,
                        attacked,
                        canisterGun.Malfunctioned ? $"the Gun malfunctions on {canisterGun.BreakdownNumber} or more (C2.28)"
                            : canisterGun.RateOfFireKept ? "it may fire again this phase (C2.24)" : "no more fire this phase",
                        canisterResolution.AmmunitionUse == "none" && !canisterGun.Malfunctioned ? null : "it holds no Acquisition (C6.5)",
                    ];
                    records.Add((item.EventId, string.Join("; ", canisterParts.OfType<string>())));
                    continue;
                }

                if (item.Payload is not OrdnanceFired fired || fired.Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json) is not { ToHit: { } hit, Gun: { } gun } resolution)
                {
                    continue;
                }

                var dice = string.Join(", ", hit.Dice.Select((die, index) => index == 0 ? $"{die} (colored)" : $"{die}"));
                var modifications = string.Concat(hit.Modifications.Select(item => $" {(item.Value < 0 ? "-" : "+")} {Number(Math.Abs(item.Value))} ({DisplayText.Modifier(item.Name)}, {item.Rule})"));
                var shot = fired.Facts.Deserialize<OrdnanceShot>(LiveFire.Json);
                var machineGun = LiveOrdnance.IsMachineGun(shot?.Gun?.DefinitionId);

                // C8.9 (the table player's and the UI review of pass 55): a record whose resolution names its Depletion Number, or whose
                // ammunition has had one only since the pass (APDS, HE or AP by number), is worded as the rule has it: a round that was not
                // there is no miss, and the Gun "is considered not to have fired yet for any purpose". An APCR or HEAT record made before
                // the pass names no number and keeps the words it was shown with.
                var worded = resolution.AmmunitionUse is not null && (resolution.DepletionNumber is not null || shot?.Ammunition is "apds" or "he" or "ap");
                var roundName = ScenarioA1Definitions.AmmunitionName(shot?.Ammunition ?? string.Empty);
                var noRound = worded && resolution.AmmunitionUse == "none";
                var outcome = noRound ? $"no shot: it had no {roundName}" : hit.CriticalHit ? "Critical Hit" : hit.Hit ? "hit" : "miss";
                // D3.2 (pass 50): from inside the target's hex the colored dr gives the Target Facing.
                // A9.61 (pass 60): a MG's To Kill attack, fired as ordnance.
                var aim = shot?.VehicleTarget is { } aimed ? $"{(machineGun ? "a MG To Kill attack (A9.61)" : (shot.Ammunition ?? "ap").ToUpperInvariant())} at {aimed.VehicleId} in {fired.Target} ("
                    + (shot.SameHex == true ? "in its own hex: the Target Facing by the colored dr, D3.2)"
                        : $"hull {aimed.HullFacing}" + (aimed.TurretFacing is { } turretFacing ? $", turret {turretFacing}" : "") + " toward the firer)")
                    : shot?.TargetType == OrdnanceTargetTypes.Area ? $"HE at {fired.Target} on the Area Target Type" + (shot.Spotter is { } spotter ? $", spotted by {spotter.UnitId}" : "")
                    // C8.31 (pass 60): the round and the FP its HE Equivalency attacks with, as the record names it.
                    : resolution.HeEquivalency is { } equivalency
                    ? $"{(shot?.Gun?.DefinitionId == OrdnanceTargetTypes.Panzerfaust ? "a PF" : ScenarioA1Definitions.AmmunitionName(shot?.Ammunition ?? string.Empty))} at {fired.Target} ({equivalency} FP by HE Equivalency, C8.31)"
                    : $"HE at {fired.Target}";
                var check = resolution.PanzerfaustCheck is { } pf ? $"PF Check dr {pf.Dr}{ModifierText(pf.Drm)} = {pf.FinalDr}: a shot; " : "";
                var parts = new List<string>
                {
                    check + $"{fired.Gun}{In(history.At(item.Revision - 1), fired.Gun)} fires {aim}" + (shot?.FireKind == ScenarioA1FireCalculator.BoundingFirstFire ? " as Bounding First Fire (D3.3)" : "")
                        + (shot?.GunDuel == true ? ", first in a Gun Duel (C2.2401)" : "")
                        + (fired.Facing is { } facing ? $", turning to {UnitFacings.Name(facing)}" : "")
                        + $": Basic TH# {hit.BasicToHit} ({hit.Color}){modifications} = {hit.ModifiedToHit}; DR {dice} = {hit.OriginalDr}{ModifierText(hit.Drm)} = Final DR {hit.FinalDr}"
                        + (hit.SubsequentDr is { } dr ? $", subsequent dr {dr}" : "") + (hit.Improbable ? " (Improbable Hit, C3.6)" : "") + $": {outcome}",
                };
                // A12.3, ruling R21.1 (pass 60, the table player): a hidden unit the shot attacked is never named nor counted to a view that may not
                // know it; its own side and the adjudicator read it.
                var before = history.At(item.Revision - 1);
                bool Shown(string id) => viewer.IsAdjudicator || shot?.Hit?.Targets?.FirstOrDefault(target => target.UnitId == id) is not { Hidden: true }
                    || before?.Unit(id)?.Side == viewer.Name;

                // A concealed vehicle's line gives no DRM or Final DR to such a view: its Case P would tell its size. A concealed unit's tells only its "?".
                bool Masked(string id) => !viewer.IsAdjudicator && before?.Unit(id)?.Side != viewer.Name
                    && shot?.Hit?.Vehicles?.FirstOrDefault(vehicle => vehicle.VehicleId == id)?.Concealed == true;
                string Judged(OrdnanceAreaTarget unit) => Masked(unit.UnitId) ? $"{unit.UnitId} {(unit.Hit ? "hit" : "missed")}"
                    : $"{unit.UnitId} Final DR {unit.FinalDr}{ModifierText(unit.Drm)} {(unit.Hit ? "hit" : "missed")}";
                if (resolution.AreaTargets is { } judged)
                {
                    parts.Add("each unit: " + string.Join(", ", judged.Where(unit => Shown(unit.UnitId)).Select(Judged)));
                }

                // C3.32, C3.4 (pass 60): a Location hit target by target, each unit and vehicle on its own Final DR.
                if (resolution.TargetHits is { } each)
                {
                    parts.Add("each target (C3.32, C3.4): " + string.Join(", ", each.Where(unit => Shown(unit.UnitId)).Select(Judged)));
                }

                // C13.31 (p. 183; pass 60, the table player): a PF's hit affects the unit the Random Selection dr picks, the highest and every one tied.
                if (shot?.Gun?.DefinitionId == OrdnanceTargetTypes.Panzerfaust && resolution.CriticalHit is null
                    && fired.Rolls.FirstOrDefault(pair => pair.Key.StartsWith("criticalSelection:", StringComparison.Ordinal)) is { Key: { } selectionKey } selection
                    && events.Select(other => other.Payload).OfType<DiceRolled>().FirstOrDefault(roll => roll.Roll == selection.Value) is { } selectionRoll)
                {
                    var drawn = selectionKey["criticalSelection:".Length..].Split(',').Zip(selectionRoll.Values, (id, dr) => (Id: id, Dr: dr)).Where(pair => Shown(pair.Id)).ToArray();
                    var top = drawn.Length == 0 ? 0 : drawn.Max(pair => pair.Dr);
                    var picked = drawn.Where(pair => pair.Dr == top).Select(pair => pair.Id).ToArray();
                    parts.Add($"Random Selection dr: {string.Join(", ", drawn.Select(pair => $"{pair.Id} {pair.Dr}"))}; {string.Join(", ", picked)} {(picked.Length == 1 ? "is" : "are")} hit (C13.31)");
                }

                if (resolution.FirerEffect is { } firerEffect)
                {
                    parts.Add($"{fired.Crew}: {firerEffect} (C13.36)");
                }

                // B23.32 (pass 60): a mortar's hit on each other Location of a building hex, on the same DR.
                foreach (var (label, attack) in new[] { ("Critical Hit", resolution.CriticalHit), ("hit", resolution.Hit) }
                    .Concat((resolution.LevelHits ?? []).Select(level => ("hit", (FireResolution?)level))))
                {
                    if (attack?.Arithmetic is { } arithmetic)
                    {
                        // C3.32, A7.308, D.8B (pass 60): the vehicles it struck, on the Vehicle line, and the CE crews of the AFVs beside them.
                        var struck = (attack.VehicleEffects ?? []).Select(vehicle => vehicle.VehicleId);
                        string[] hitOn = [.. attack.Effects.Select(effect => effect.UnitId).Where(Shown).Concat(struck)];

                        // C3.41, C11.4 (pass 60, the table player): a hit on a Location whose only target is an unattended Gun names the Gun.
                        var onWhat = hitOn.Length > 0 ? string.Join(", ", hitOn)
                            : shot?.Hit?.GunTarget is { CrewUnitId: null, GunId: { } loneGun } ? $"the unattended Gun {loneGun}" : "the Location";
                        parts.Add($"{label} on {onWhat}: {arithmetic.ColumnFp} FP, IFT DR {string.Join(", ", arithmetic.Dice)} = {arithmetic.OriginalDr}"
                            + $"{ModifierText(arithmetic.Drm)} = {arithmetic.FinalDr}: {arithmetic.Result}");
                        foreach (var vehicle in attack.VehicleEffects ?? [])
                        {
                            parts.Add($"{vehicle.VehicleId}: " + (vehicle.KillNumber is not null ? FireText.VehicleLineText(vehicle) : FireText.CrewText(vehicle)));

                            // D.8B, D6.9 (pass 65): what it did to the vehicle's PRC.
                            parts.AddRange(FireText.PrcLines(vehicle));
                        }

                        // A9.74, C8.42 (pass 60, ruling R60.17): what the hit's KIA did to its victims' weapons.
                        parts.AddRange(FireText.WeaponDestructionLines(attack.WeaponDestruction?.Where(fate => Shown(fate.HolderId)).ToArray()));
                    }
                }

                // C11.4, C11.6 (pass 60, ruling R60.18): the fate of the Gun the shot was at, a crewed or an unattended one.
                if (resolution.GunTargetFate is { } gunFate && shot?.Hit?.GunTarget?.GunId is { } struckGun)
                {
                    parts.Add($"{struckGun}: a Direct Hit, {(gunFate == ScenarioA1GunDestruction.Destroyed ? "destroyed" : "malfunctioned")} (C11.4)");
                }

                // C8.9, C8.2 (pass 55): the number the shot was fired under, with the one an Elite force adds, as the record itself names it.
                var depletion = resolution.DepletionNumber is { } recordedNumber ? ScenarioA1Definitions.DepletionNumberWords(recordedNumber, shot?.Elite == true)
                    : "Depletion Number";
                if (resolution.AmmunitionUse == "none")
                {
                    parts.Add(worded ? $"the Original DR is above the {depletion}: it has none for the rest of the scenario (C8.9)"
                        : $"the Original DR is above the Depletion Number: it had no {shot?.Ammunition?.ToUpperInvariant()}, has not fired, and has none for the rest of the scenario (C8.9)");
                }
                else if (resolution.AmmunitionUse == "depleted")
                {
                    parts.Add($"the Original DR equals the {depletion}: its last {(worded ? roundName : shot?.Ammunition?.ToUpperInvariant())} (C8.9)");
                }
                else if (resolution.AmmunitionUse == "used" && worded)
                {
                    parts.Add($"the Original DR is below the {depletion}: {roundName} fired (C8.9)");
                }

                if (resolution.Kill is { } kill)
                {
                    var tk = kill.BasicTk is { } basic
                        ? $"Basic TK# {basic}{string.Concat(kill.Modifications.Select(item => $" {(item.Value < 0 ? "-" : "+")} {Number(Math.Abs(item.Value))} ({DisplayText.Modifier(item.Name)}, {item.Rule})"))} - AF {kill.ArmorFactor} = Final TK# {kill.FinalTk}"
                        : $"unarmored Final TK# {kill.FinalTk}";
                    var fate = kill.Result switch
                    {
                        OrdnanceKill.Burn => "burns (a burning wreck)",
                        OrdnanceKill.Eliminated => "eliminated (a wreck)",
                        OrdnanceKill.Immobilized => "immobilized",
                        OrdnanceKill.Shock => "Shocked",
                        OrdnanceKill.PossibleShock => "possible Shock",
                        OrdnanceKill.Dud => "a dud (Original 12)",

                        // A9.61, D5.34 (pass 60): a MG's To Kill DR equal to the Final TK#.
                        OrdnanceKill.Stun => "Stunned, BU (D5.34)",
                        _ => "no effect",
                    };
                    // D3.42, D3.45 (pass 55): a check at an Armor Leader's Morale Level, or an Inexperienced Crew's, says whose it is.
                    // The table player's review: "against crew morale 10, its Armor Leader's (D3.42)", with no parenthesis inside another.
                    static string Against(FireCheck made) => FireText.MoraleWords(made);
                    var checks = (kill.ShockCheck is { } ntc ? $"; NTC DR {ntc.FinalDr} against {Against(ntc)}: {(ntc.Passed ? "passed" : "Shocked")}" : "")
                        + (kill.CrewCheck is { } tc ? $"; {(kill.SecondCause == true ? FireText.SecondCauseWords + ", " : "")}crew TC DR {tc.FinalDr} against {Against(tc)}: {(tc.Passed ? "passed" : "Abandoned")}" : "")
                        + (kill.CrewSurvival is { } cs ? $"; Crew Survival DR {cs.FinalDr} against CS# {cs.CrewSurvival}: {(cs.Survived ? "the crew survives" : "no survivors")}" : "");
                    // A9.61 (pass 60, the table player): a MG's To Kill attack is the MG's, on the AP To Kill Table, not a Gun's AP round.
                    var round = machineGun ? "the MG, on the AP To Kill Table" : kill.Ammunition.ToUpperInvariant();
                    parts.Add($"{kill.HitLocation} hit, {kill.TargetFacing} Target Facing, {round}: {tk}; TK DR {string.Join(", ", kill.Dice)} = {kill.OriginalDr}: {fate}{checks}");

                    // D6.9 (pass 65): the Survival DR of each PRC of the vehicle.
                    if (shot?.VehicleTarget?.VehicleId is { } killedId)
                    {
                        parts.AddRange(FireText.PrcSurvivalLines(killedId, kill.PrcSurvival));
                    }

                    // D5.34 (pass 60, ruling R60.25): a Stun buttons the crew up at once, so no Specific Collateral Attack follows it.
                    if (kill.Result == OrdnanceKill.Stun && shot?.SpecificCollateral == true && shot.VehicleTarget?.CrewExposed == true)
                    {
                        parts.Add("no Collateral Attack: the Stun buttons it up (D5.34)");
                    }
                }

                // C1.55, C9.1 (pass 60): a mortar's hit on each AFV, on the IFT.
                foreach (var afv in resolution.AreaVehicles ?? [])
                {
                    var fate = afv.Result switch
                    {
                        OrdnanceKill.Burn => "burns (a burning wreck)",
                        OrdnanceKill.Eliminated => "eliminated (a wreck)",
                        OrdnanceKill.Immobilized => "immobilized",
                        OrdnanceKill.Shock => "Shocked",
                        _ => "no effect",
                    };
                    parts.Add($"{afv.VehicleId}: {(afv.Critical == true ? "a Critical Hit, " : "")}{afv.HitLocation} hit on the IFT (C1.55): {afv.Firepower} FP, DR "
                        + $"{string.Join(", ", afv.Dice)}{ModifierText(afv.Drm)} = {afv.FinalDr} against the K/# DR {afv.KillDr}: {fate}"
                        + (afv.CrewCheck is { } areaTc ? $"; crew TC DR {areaTc.FinalDr} against {FireText.MoraleWords(areaTc)}: {(areaTc.Passed ? "passed" : "Abandoned")} (D5.5)" : "")
                        + (afv.CrewSurvival is { } survival ? $"; Crew Survival DR {survival.FinalDr} against CS# {survival.CrewSurvival}: {(survival.Survived ? "the crew survives" : "no survivors")}" : ""));

                    // D6.9, D9.2 (pass 65): the Survival DR of each PRC of the AFV.
                    parts.AddRange(FireText.PrcSurvivalLines(afv.VehicleId, afv.PrcSurvival));
                }

                // D.8A (pass 60): the Specific Collateral Attack on the CE crew of the AFV the hit left standing.
                if (resolution.SpecificCollateral is { } collateral)
                {
                    // Pass 65 (ruling R65.7): the attack may reach the PRC alone; the crew's part is written only when it took it.
                    var crew = collateral.Effect;
                    if (crew.CrewResult != FireVehicleEffect.NotVulnerable)
                    {
                        parts.Add($"its CE crew takes a Specific Collateral Attack of {collateral.Firepower} FP on the same DR{ModifierText(crew.Drm)} (D.8A): Final DR {crew.FinalDr}, "
                            + (collateral.Result == "none" ? "no effect" : collateral.Result)
                            + (crew.CrewCheck is { } made ? $"; {FireText.CheckText(made)}" : "")
                            + (crew.CrewResult == FireVehicleEffect.None ? "" : $": {crew.CrewResult}"));
                    }

                    parts.AddRange(FireText.PrcLines(crew));
                }

                var boundingShot = shot?.FireKind == ScenarioA1FireCalculator.BoundingFirstFire;
                parts.Add((gun.Malfunctioned ? machineGun ? "the MG malfunctions (A9.7)" : "the Gun malfunctions (C2.28)"
                    : noRound ? "it has not fired and may fire other ammunition (C8.9)"
                    : gun.RateOfFireKept ? boundingShot ? "it may fire again after another MP, and a Delay MP will do (C2.24, C5.3)" : "it may fire again this phase (C2.24)"
                    : boundingShot ? "a Bounding Fire counter: no more fire this Player Turn (D3.3)" : "no more fire this phase")
                    + (gun.Acquisition < 0 ? $"; Acquisition {gun.Acquisition} on {gun.AcquiredLocationId} (C6.5)" : ""));
                records.Add((item.EventId, string.Join("; ", parts)));
            }

            records.Reverse();
            return records;
        }
    }

    /// <summary>
    /// The fire attacks the viewer may see, latest first: each record it is entitled to, and for a record withheld from it,
    /// the public report of its arithmetic (Fire in Live Play, part 5).
    /// </summary>
    private IReadOnlyList<FireView> FireRecords
    {
        get
        {
            var views = new List<FireView>();
            foreach (var item in events.Where(item => item.IsVisibleTo(viewer)))
            {
                if (item.Payload is FireResolved fire && fire.Resolution.Deserialize<FireResolution>(LiveFire.Json) is { Arithmetic: { } arithmetic } resolution)
                {
                    // Pass 31 (play test P-16): what the targets already were, read from the record's own facts.
                    var facts = fire.Facts.Deserialize<FireAttack>(LiveFire.Json);
                    var before = facts?.Targets ?? [];

                    // C8.4, D3.52 (the table player's review of pass 55): a Canister shot's Case A is its CA change, not its Gun's name.
                    if (facts?.Canister is { GunId: { } canisterGun })
                    {
                        arithmetic = arithmetic with
                        {
                            Drm = [.. arithmetic.Drm.Select(modifier => modifier.Name == "case-a:" + canisterGun ? modifier with { Name = DisplayText.CanisterCaseA } : modifier)]
                        };
                    }

                    views.Add(new FireView(item.EventId, Say(item.EventId, GroupOf(fire)), Say(item.EventId, fire.TargetLocation), null, arithmetic, resolution.Effects)
                    {
                        // A Canister attack on a Location where the view knows of no unit reads as that, and not as fire at a concealed target.
                        NoKnownTarget = facts is { Canister: not null, Targets: null or { Count: 0 }, Vehicles: null or { Count: 0 } },
                        Say = text => Say(item.EventId, text),
                        Several = fire.Firers.Count > 1,
                        LosBlocked = resolution.LosBlocked == true,
                        Weapons = resolution.WeaponEffects,
                        Vehicles = resolution.VehicleEffects,

                        // Pass 60 (rulings R60.15, R60.16): the AFV a FT, MOL, or DC named.
                        NamedVehicle = facts?.TargetVehicle?.VehicleId is { } namedId ? FireText.NamedVehicleLines(namedId, resolution, facts.TargetVehicle.CrewExposed != false) : null,

                        // Pass 60 (ruling R60.17): what the KIA did to its victims' weapons.
                        WeaponDestruction = resolution.WeaponDestruction is { Count: > 0 } ? FireText.WeaponDestructionLines(resolution.WeaponDestruction) : null,
                        BrokenBefore = before.Where(target => target.Broken == true && target.UnitId is not null).Select(target => target.UnitId!).ToHashSet(StringComparer.Ordinal),
                        PinnedBefore = before.Where(target => target.Pinned == true && target.UnitId is not null).Select(target => target.UnitId!).ToHashSet(StringComparer.Ordinal),
                    });
                }
                else if (item.Payload is FireReported report && events.FirstOrDefault(other => other.EventId == report.Fire) is { } withheld
                    && !withheld.IsVisibleTo(viewer) && report.Arithmetic.Deserialize<FireArithmetic>(LiveFire.Json) is { } reported)
                {
                    views.Add(new FireView(report.Fire, Say(item.EventId, withheld.Payload is FireResolved hidden ? GroupOf(hidden) : $"The fire group in {report.FirerLocation}"),
                        Say(item.EventId, report.TargetLocation), withheld.Visibility is [{ } only] ? only : null, reported, null)
                    {
                        Say = text => Say(item.EventId, text),

                        // The firing side reads a Canister attack that left units it does not see unaffected as it reads one on an empty Location.
                        NoKnownTarget = withheld.Payload is FireResolved canisterAttack && canisterAttack.Facts.ValueKind == JsonValueKind.Object
                            && canisterAttack.Facts.TryGetProperty("canister", out var canisterFact) && canisterFact.ValueKind == JsonValueKind.Object
                            && LimboDancer.Domains.Asl.Maps.Coordinates.BoardLocation.TryParse(report.TargetLocation, out var canisterAt) && history.At(item.Revision - 1) is { } then
                            && !then.At(canisterAt).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != viewer.Name && Open(then, unit.Id)),
                        Several = withheld.Payload is FireResolved { Firers.Count: > 1 },

                        // The LOS is the map's, and both sides see that the shot went nowhere (A6.11).
                        LosBlocked = withheld.Payload is FireResolved unseen && unseen.Resolution.Deserialize<FireResolution>(LiveFire.Json)?.LosBlocked == true,
                    });
                }
            }

            views.Reverse();
            return views;
        }
    }

    /// <summary>
    /// A record's fire group in words. The firers, their Locations, their MGs, and the directors are the firing side's own
    /// and public at the table, so a record withheld for its targets still names them; the targets never appear here.
    /// </summary>
    private string GroupOf(FireResolved fire) => fire.Facts.Deserialize<FireAttack>(LiveFire.Json) is { } attack
        ? FireGroupText(events, attack)
        : $"The fire group in {fire.FirerLocation}";

    /// <summary>
    /// The fire group of an attack: each Location's firers with the MGs they use (an MG firing without its holder's inherent FP
    /// is named alone), then every directing leader (A7.5, A7.531, A9.2), for example "r1 with its mg1, r2 in bd01:D4:0, directed
    /// by rl", which a view then reads as "4-4-7 squad R1 with its MMG, 4-4-7 squad R2 in D4, directed by 9-1 leader R1". Residual FP has no
    /// firers (A8.22).
    /// </summary>
    public static string FireGroupText(IReadOnlyList<GameEvent> events, FireAttack attack)
    {
        if (attack.VehicleFire is { } byVehicle)
        {
            // D3.5 (pass 50): a tank's BMG and CMG by name; a vehicle whose MA is its MG as before.
            return byVehicle.Weapons is { Count: > 0 } mgs
                ? $"{byVehicle.VehicleId}'s {string.Join(" and ", mgs.Select(item => (item.Weapon ?? string.Empty).ToUpperInvariant()))} in {byVehicle.LocationId}"
                : $"{byVehicle.VehicleId}'s AAMG in {byVehicle.LocationId}";
        }

        // D7.1 (visual check, pass 11): an OVR's firer is its vehicle.
        if (attack.Overrun is { } overrun)
        {
            return $"{overrun.VehicleId}'s OVR in {overrun.LocationId}";
        }

        // A23 (visual check, pass 15): a DC's attack names the DC and its user.
        if (attack.DemolitionCharge is { } charge)
        {
            return charge.Mode switch
            {
                FireDemolitionCharge.Placed => $"The DC Placed by {charge.UserId}",
                FireDemolitionCharge.Thrown => $"The DC Thrown by {charge.UserId}",
                _ => $"The DC Thrown by {charge.UserId} at its own Location",
            };
        }

        // C8.4 (pass 55): a Canister shot's attack on one of its Locations names the Gun.
        if (attack.Canister is { } canister)
        {
            return $"{canister.GunId}'s Canister in {attack.FirerLocationId}";
        }

        if (attack.Firers is not { Count: > 0 } firers)
        {
            // A9.22 (backlog pass 12): a Fire Lane's attack names the lane's MG and its manning Infantry.
            if (attack.FireLane == true)
            {
                var laid = events.Select(item => item.Payload).OfType<FireLanePlaced>()
                    .LastOrDefault(lane => lane.Entries.Any(entry => entry.Location.ToString() == attack.TargetLocationId && entry.Fp == attack.ResidualFp));
                return laid is null ? $"{attack.ResidualFp} Fire Lane Residual FP (A9.22)"
                    : $"{attack.ResidualFp} Fire Lane Residual FP of {laid.Weapon} ({laid.Operator}) (A9.22)";
            }

            return attack.FireKind == ScenarioA1FireCalculator.ResidualFire ? $"{attack.ResidualFp} Residual FP" : "no firers";
        }

        static string Firer(FireFirer firer)
        {
            var weapons = (firer.Weapons ?? []).Select(item => item.EquipmentId).OfType<string>().ToArray();
            // Pass 31c (play test P-17): the headline names the weapons, and says when a MG fires without its holder's own FP.
            return firer.UsesInherentFp == false ? $"{string.Join(", ", weapons)} of {firer.UnitId}, alone"
                : weapons.Length > 0 ? $"{firer.UnitId} with its {string.Join(" and its ", weapons)}" : firer.UnitId ?? "?";
        }

        var groups = firers.GroupBy(item => item.LocationId ?? attack.FirerLocationId).Select(group => $"{string.Join(", ", group.Select(Firer))} in {group.Key}");
        var directors = new[] { attack.Director?.UnitId }.Concat((attack.OtherDirectors ?? []).Select(item => item.UnitId)).OfType<string>().ToArray();
        return string.Join("; ", groups) + (directors.Length > 0 ? $", directed by {string.Join(", ", directors)}" : "");
    }

    /// <summary>The range: the attack's, or each firer's when the group spans Locations (A7.52).</summary>
    public static string RangeText(FireAttack attack) => attack.Range is { } range ? range.ToString(CultureInfo.InvariantCulture)
        : attack.Firers?.Where(item => item.Range is not null).ToArray() is { Length: > 0 } ranged
            ? string.Join(", ", ranged.Select(item => $"{item.UnitId} {item.Range!.Value.ToString(CultureInfo.InvariantCulture)}"))
            : "not read";

    /// <summary>
    /// The Rally and Repair attempts the viewer may see, latest attempt first, each with its arithmetic: for a rally the DR, each DRM,
    /// the Final DR against the Morale Level, and the result (A10.6 to A10.64); for a repair the dr against the Repair Number
    /// (A9.72). A rally by a concealed unit that stays concealed is its own side's (ruling R19.8).
    /// </summary>
    private IReadOnlyList<(string EventId, string Kind, string Text)> RallyAndRepairRecords
    {
        get
        {
            var rolls = events.Select(item => item.Payload).OfType<DiceRolled>().ToDictionary(item => item.Roll, StringComparer.Ordinal);
            var routs = new Dictionary<(string Attempt, string Unit), (int Index, string From, IReadOnlyList<string> Steps, int HalfMf)>();
            var records = new List<(string, string, string)>();
            // Backlog section 23 (pass 28b): each attempt's events, to tell a Failure to Rout and a lone SW transfer from the same events elsewhere.
            var attempts = events.GroupBy(item => AttemptOf(item.EventId), StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            var starts = events.Select((item, index) => (Attempt: AttemptOf(item.EventId), index)).GroupBy(pair => pair.Attempt, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().index, StringComparer.Ordinal);
            foreach (var (item, index) in events.Select((item, index) => (item, index)).Where(pair => pair.item.IsVisibleTo(viewer)))
            {
                var before = index > 0 ? history.States.ElementAtOrDefault(index - 1) : null;
                var attempt = AttemptOf(item.EventId);
                var start = starts[attempt] > 0 ? history.States.ElementAtOrDefault(starts[attempt] - 1) : null;
                if (item.Payload is RallyAttempted rally && rally.Resolution.Deserialize<RallyResolution>(LiveFire.Json) is { Arithmetic: { } arithmetic } resolution)
                {
                    records.Add((item.EventId, "rally", RallyText(rally, arithmetic, resolution.Effect, In(before, rally.Unit))));
                }
                else if (item.Payload is RoutStepped { Attempted: { } repulsedFrom } repulsed)
                {
                    // Pass 35 (task 35.4; A10.533): a repulsed step is a sentence of its own, after whatever steps the rout made before it.
                    records.Add((item.EventId, "rout", $"{repulsed.Unit} is repulsed from {repulsedFrom} by a concealed unit there and ends its rout in {repulsed.To} (A10.533)"));
                }
                else if (item.Payload is RoutStepped beneath && before?.Unit(beneath.Unit) is { Position: ContainedPosition aboard })
                {
                    // D6.1, D5.311 (pass 65, the table player): a Passenger, or a broken crew inside its AFV, routs beneath its vehicle, spending all its MF.
                    records.Add((item.EventId, "rout", $"{beneath.Unit} routs beneath {aboard.Container} in {beneath.To}, spending all its MF"
                        + (aboard.Role == ContainmentRole.Crew ? $", and {aboard.Container} is Abandoned (D5.311)" : " (D6.1)")));
                }
                else if (item.Payload is RoutStepped routed)
                {
                    // Pass 31d (design D11): the steps of one rout are one sentence, "routs from [Y5] by [Z5] to [AA5] for 3 MF", and name the unit once.
                    // Pass 70 (ruling R70.12): a Voluntary Rout leader's steps are his own sentence, "routs with ... by Voluntary Rout".
                    var key = (AttemptOf(item.EventId), routed.Unit);
                    var how = routed.With is { } companion ? $" with {companion} by Voluntary Rout" : string.Empty;
                    var rule = routed.With is not null ? " (A10.711)" : routed.LowCrawl ? " by Low Crawl (A10.52)" : " (A10.5)";
                    if (routs.TryGetValue(key, out var earlier))
                    {
                        routs[key] = earlier = (earlier.Index, earlier.From, [.. earlier.Steps, routed.To.ToString()], earlier.HalfMf + routed.HalfMf);
                        var total = (earlier.HalfMf / 2.0).ToString("0.#", CultureInfo.InvariantCulture);
                        records[earlier.Index] = (records[earlier.Index].Item1, "rout",
                            $"{routed.Unit} routs{how}{earlier.From} by {string.Join(" then ", earlier.Steps.Take(earlier.Steps.Count - 1))} to {earlier.Steps[^1]} for {total} MF{(routed.With is not null ? " (A10.711)" : " (A10.5)")}");
                    }
                    else
                    {
                        var mf = (routed.HalfMf / 2.0).ToString("0.#", CultureInfo.InvariantCulture);
                        routs[key] = (records.Count, From(before, routed.Unit), [routed.To.ToString()], routed.HalfMf);
                        records.Add((item.EventId, "rout", $"{routed.Unit} routs{how}{From(before, routed.Unit)} to {routed.To} for {mf} MF{rule}"));
                    }
                }
                else if (item.Payload is MoraleCheckResolved moraleCheck && MoraleCheckText(moraleCheck) is { } checkText)
                {
                    // Pass 70 (rulings R70.3, R70.4; the fixes): a morale check made outside an attack, a LLMC among them, in the Records tab's list too.
                    records.Add((item.EventId, "morale-check", checkText));
                }
                else if (item.Payload is VoluntaryBreak voluntary)
                {
                    // Pass 70 (ruling R70.11): the units a side breaks at the start of the RtPh so as to rout.
                    records.Add((item.EventId, "voluntary-break", string.Join(", ", voluntary.Units.Select(id => id + In(before, id)))
                        + (voluntary.Units.Count == 1 ? " breaks" : " break") + " voluntarily at the start of the RtPh, under DM, so as to rout (A10.41, A10.62)"));
                }
                else if (item.Payload is ConcealmentVerified verified)
                {
                    // Pass 70 (ruling R70.21): the concealed units that forced a "?" loss, shown for a moment; the record is the moving side's alone.
                    records.Add((item.EventId, "concealment-verified",
                        ScenarioA1ConcealmentChoices.VerifiedText([.. verified.Units.Select(id => id + In(before, id))])));
                }
                else if (item.Payload is DiceRolled { Purpose: ScenarioA1Wounds.SeverityPurpose } severity)
                {
                    // Pass 35 (task 35.1; A17.11): the Wound Severity dr of a SMC's Casualty Reduction, said where it is made.
                    // The table player, pass 35: the man and the outcome are read from what the same action did next, a wound or an elimination.
                    var later = attempts[attempt].SkipWhile(other => other.EventId != item.EventId).Skip(1).Select(other => other.Payload);
                    var man = later.Select(payload => payload switch
                    {
                        InstanceEliminated gone => gone.Id,
                        ConditionsChanged changed when changed.Conditions.ContainsKey(Conditions.Wounded) => changed.Id,
                        _ => null,
                    }).FirstOrDefault(id => id is not null);
                    records.Add((item.EventId, "wound", ScenarioA1Wounds.SeverityText(severity.Values[0], man,
                        man is not null && before?.Unit(man) is { } hurt && GameState.Condition(hurt, Conditions.Wounded) == ConditionState.True)));
                }
                else if (item.Payload is RoutInterdicted interdicted)
                {
                    var dr = rolls.TryGetValue(interdicted.Roll, out var roll) ? roll.Values.Sum().ToString(CultureInfo.InvariantCulture) : "?";
                    // Pass 70 (ruling R70.12): the Voluntary Rout leader's DRM, and his fate when the NMC fails.
                    var led = interdicted.Leader is { } guide && roll is not null
                        ? $" {(interdicted.Drm < 0 ? "-" : "+")} {Math.Abs(interdicted.Drm).ToString(CultureInfo.InvariantCulture)} (leadership, {guide}, A10.711) = {(roll.Values.Sum() + interdicted.Drm).ToString(CultureInfo.InvariantCulture)}"
                        : string.Empty;
                    var fate = interdicted.Leader is { } routedWith && interdicted.Result is RoutInterdicted.Reduced or RoutInterdicted.Eliminated
                        ? $"; {routedWith}, routing with it, is eliminated (A10.711)" : string.Empty;
                    records.Add((item.EventId, "interdiction",
                        $"Interdiction of {interdicted.Unit} entering {interdicted.At}{(interdicted.Interdictor is { } by ? $" by {by}" : "")}: NMC DR {dr}{led} against broken morale {interdicted.Morale}: {interdicted.Result} (A10.53){fate}"));
                }
                else if (item.Payload is LineageRecorded { Action: LineageAction.Deployed or LineageAction.Recombined } lineage && lineage.Consumed.All(id => Open(before, id)))
                {
                    records.Add((item.EventId, "lineage", lineage.Action == LineageAction.Deployed
                        ? $"{lineage.Consumed[0]}{In(before, lineage.Consumed[0])} becomes the HS {string.Join(" and ", lineage.Produced.Select(unit => unit.Id))} "
                            + (attempts[attempt].Any(other => other.Payload is DeploymentAttempted) ? "(A1.31)" : "with no NTC, as a Guard of prisoners (A20.5)")
                            + DeploySplit(attempts[attempt], lineage)
                        : $"{string.Join(" and ", lineage.Consumed)}{In(before, lineage.Consumed[0])} Recombine into {lineage.Produced[0].Id} (A1.32)"));
                }
                else if (item.Payload is ConditionsChanged dismantled && dismantled.Conditions.TryGetValue(Conditions.Dismantled, out var taken)
                    && (before?.Find(dismantled.Id) is not EquipmentInstance { Holding: { } heldBy } || Open(before, heldBy.Holder)))
                {
                    records.Add((item.EventId, "dismantle", $"{dismantled.Id}{In(before, dismantled.Id)} is {(taken == ConditionState.True ? "dismantled" : "assembled")} (A9.8)"));
                }
                else if (item.Payload is ConditionsChanged marked && marked.Conditions.TryGetValue(Conditions.DesperationMorale, out var gained)
                    && gained == ConditionState.True && Open(before, marked.Id))
                {
                    // Referee, pass 28b: the RPh's end clears DM and sets it again for a unit that keeps it (A10.62 EXC; E1.54 at night), which is not
                    // DM gained.
                    if (start?.Unit(marked.Id) is not { } earlier || GameState.Condition(earlier, Conditions.DesperationMorale) != ConditionState.True)
                    {
                        records.Add((item.EventId, "dm", $"{marked.Id}{In(before, marked.Id)} comes under DM (A10.62)"));
                    }
                    else if (start.Phase == "rph")
                    {
                        records.Add((item.EventId, "dm", $"{marked.Id}{In(before, marked.Id)} keeps DM as the RPh ends ({(start.Night ? "E1.54" : "A10.62")})"));
                    }
                }
                else if (item.Payload is InstanceEliminated eliminated && item.RulePackage is null && before?.Phase == "rtph"
                    && attempts[attempt].Any(other => other.Payload is PhaseChanged) && Open(before, eliminated.Id))
                {
                    records.Add((item.EventId, "failure-to-rout", $"{eliminated.Id}{In(before, eliminated.Id)} is eliminated for Failure to Rout as the RtPh ends (A10.5)"));
                }
                else if (item.Payload is EquipmentTransferred { Holding: null, Position: MapPosition left } dropped)
                {
                    records.Add((item.EventId, "drop", $"{dropped.Id} is left in {left.Location} (A4.43)"));
                }
                else if (item.Payload is EquipmentTransferred { Holding: { Role: HoldingRole.Possessed } receiver } passed
                    // Pass 70's fixes: the "?" the transfer costs (A12.141) has its own line and does not hide this one.
                    && attempts[attempt].Count(other => !IsDmOnly(other.Payload) && other.Type != "concealment-lost") == 1
                    && before?.Find(passed.Id) is EquipmentInstance { Holding: { Role: HoldingRole.Possessed } held } && held.Holder != receiver.Holder
                    && Open(before, held.Holder) && Open(before, receiver.Holder))
                {
                    records.Add((item.EventId, "transfer", $"{held.Holder} passes {passed.Id} to {receiver.Holder}{In(before, receiver.Holder)} (A4.431)"));
                }
                else if (item.Payload is DeploymentAttempted deployment && Open(before, deployment.Squad))
                {
                    var dr = rolls.TryGetValue(deployment.Roll, out var roll) ? roll.Values.Sum().ToString(CultureInfo.InvariantCulture) : "?";
                    records.Add((item.EventId, "deployment", $"{deployment.Squad}{In(before, deployment.Squad)} tries to Deploy{(deployment.Leader is { } leader ? $" with {leader}" : "")}: NTC DR {dr} "
                        + $"{deployment.Drm:+0;-0;+0} against morale {deployment.Morale}: {(deployment.Passed ? "two HS" : "stays a squad")} (A1.31)"));
                }
                else if (item.Payload is RecoveryAttempted recovery && Open(before, recovery.Unit))
                {
                    var dr = rolls.TryGetValue(recovery.Roll, out var roll) ? roll.Values[0].ToString(CultureInfo.InvariantCulture) : "?";
                    records.Add((item.EventId, "recovery", $"{recovery.Unit}{In(before, recovery.Unit)} tries to Recover {recovery.Weapon}: dr {dr} {recovery.Drm:+0;-0;+0}, needing below 6: "
                        + $"{(recovery.Recovered ? "recovered" : "not recovered")} (A4.44)"));
                }
                else if (item.Payload is InfantryOverrunNtc ntc && Open(before, ntc.Taker))
                {
                    // Pass 40 (A4.15): the NTC of an Infantry OVR, with its DR and result.
                    var dr = rolls.TryGetValue(ntc.Roll, out var roll) ? roll.Values.Sum().ToString(CultureInfo.InvariantCulture) : "?";
                    string[] others = [.. ntc.Units.Where(id => id != ntc.Taker)];
                    records.Add((item.EventId, "overrun-ntc", $"{ntc.Taker}{In(before, ntc.Taker)} takes the NTC{(others.Length > 0 ? $" for {string.Join(", ", others)}" : "")} "
                        + $"for an Infantry OVR into {ntc.Target}: DR {dr} {ntc.Drm:+0;-0;+0} against morale {ntc.Morale}: "
                        + (ntc.Passed ? "passed" : others.Length > 0 ? "failed; none of them moves or acts again this MPh" : "failed; it makes no OVR there") + " (A4.15)"));
                }
                else if (item.Payload is RepairAttempted repair)
                {
                    var dr = rolls.TryGetValue(repair.Roll, out var roll) ? roll.Values[0].ToString(CultureInfo.InvariantCulture) : "?";
                    var result = repair.Result switch
                    {
                        RepairAttempted.Repaired => "repaired",
                        RepairAttempted.Eliminated => "eliminated (a 6)",
                        _ => "still malfunctioned",
                    };
                    // D3.7 (pass 50): a vehicle's weapon by its name; a 6 disables it, and a tank whose MA is disabled is Recalled.
                    records.Add((item.EventId, "repair", repair.Weapon is { } repairedWeapon
                        ? $"{repair.Unit}{In(before, repair.Unit)} tries to repair its {repairedWeapon.ToUpperInvariant()}: dr {dr}: "
                            + (repair.Result == RepairAttempted.Eliminated
                                ? "disabled (a 6; D3.7)" + (attempts[attempt].Any(other => other.Payload is ConditionsChanged changed
                                    && changed.Conditions.TryGetValue(Conditions.Recalled, out var recalled) && recalled == ConditionState.True)
                                    ? ", and the vehicle is Recalled: buttoned up and treated as stunned until the Player Turn ends, then it must leave (D3.7, D5.341)" : "")
                                : result)
                        : $"{repair.Unit}{In(before, repair.Unit)} repairs {repair.Equipment}: dr {dr} against R{repair.RepairNumber}: {result}"));
                }
                else if (item.Payload is ManhandlingRolled push)
                {
                    var dr = rolls.TryGetValue(push.Roll, out var roll) ? roll.Values.Sum().ToString(CultureInfo.InvariantCulture) : "?";
                    records.Add((item.EventId, "manhandling", $"Manhandling DR for {push.Gun}{In(before, push.Gun)}: {dr} {push.Drm:+0;-0;+0} against M{push.Manhandling}: {push.Result} (C10.3)"));
                }
                else if (item.Payload is GunTurned turned)
                {
                    records.Add((item.EventId, "turn", $"{turned.Gun}{In(before, turned.Gun)} turns to {UnitFacings.Name(turned.Facing)} without firing (C3.22)"));
                }
                else if (item.Payload is GunHooked hooked)
                {
                    records.Add((item.EventId, "hook", $"{hooked.Vehicle} {(hooked.Hooked ? "hooks up" : "unhooks")} {hooked.Gun} for {hooked.Mp} MP (C10.11, C10.12)"));
                }
                else if (item.Payload is ShockRecoveryRolled shock)
                {
                    var dr = rolls.TryGetValue(shock.Roll, out var roll) ? roll.Values[0].ToString(CultureInfo.InvariantCulture) : "?";
                    var result = shock.Result switch
                    {
                        ShockRecoveryRolled.Recovered => "recovers",
                        ShockRecoveryRolled.UnconfirmedKill => "becomes an Unconfirmed Kill",
                        _ => "is wrecked",
                    };
                    records.Add((item.EventId, "shock", $"{shock.Vehicle}'s Shock or Unconfirmed Kill dr {dr}: it {result} (C7.42)"));
                }
                else if (item.Payload is MotionAttempted motion)
                {
                    // D2.401 (pass 55): the MF or MP the mover spent in the vehicle's LOS, the dr, and what it brought.
                    var dr = rolls.TryGetValue(motion.Roll, out var roll) ? roll.Values[0].ToString(CultureInfo.InvariantCulture) : "?";
                    var vehicleMover = motion.Mover is { } moverId && before?.Unit(moverId) is { } moving && LiveFire.IsVehicle(moving);
                    // The UI review: the vehicle's hex only where the view may know it; the table player's: who spent the MF or MP.
                    var where = Open(before, motion.Vehicle) ? In(before, motion.Vehicle) : string.Empty;
                    var spent = ScenarioA1MotionAttempts.Points(motion.HalfPoints, vehicleMover);
                    records.Add((item.EventId, "motion", (motion.Mover is { } spender
                            ? $"{motion.Vehicle}{where} attempts Motion as {spender} moves: {spender} has spent {spent} in its LOS, dr {dr}: "
                            : $"{motion.Vehicle}{where} attempts Motion: {spent} spent in its LOS, dr {dr}: ")
                        + ScenarioA1MotionAttempts.ResultText(motion.Result, motion.Facing is { } vca ? UnitFacings.Name(vca) : null, motion.Turret is { } tca ? UnitFacings.Name(tca) : null)
                        + " (D2.401)"));
                }
            }

            // Table player, pass 28b: the latest attempt first, and each attempt's records in the order they happened, so a cause reads before its effect.
            return [.. records.GroupBy(record => AttemptOf(record.Item1), StringComparer.Ordinal).Reverse().SelectMany(group => group)];
        }
    }

    /// <summary>A rally in words, for example "rl rallies r1: DR 5, 1 = 6 - 1 (terrain, A10.61) = Final DR 5 against 7: rallied".</summary>
    private static string RallyText(RallyAttempted rally, RallyArithmetic arithmetic, RallyEffect? effect, string where)
    {
        var who = rally.Leader is { } leader ? $"{leader} rallies {rally.Unit}{where}" : $"{rally.Unit}{where} Self-Rallies";
        var drm = string.Concat(arithmetic.Drm.Select(item => $" {(item.Value < 0 ? "-" : "+")} {Number(Math.Abs(item.Value))} ({DisplayText.Modifier(item.Name)}, {item.Rule})"));
        List<string> results = [arithmetic.Rallied ? "rallied" : "not rallied"];
        if (arithmetic.Fate)
        {
            results.Add("Fate");
        }

        if (effect is { Eliminated: true })
        {
            results.Add("eliminated");
        }
        else if (effect is { } changed && changed.FinalDefinitionId != changed.DefinitionId && arithmetic.HeatOfBattle?.HardenedDefinitionId != changed.FinalDefinitionId)
        {
            results.Add($"now {changed.FinalDefinitionId}");
        }

        if (effect is { Wounded: true })
        {
            results.Add("wounded");
        }

        if (arithmetic.HeatOfBattle is { } heat)
        {
            results.Add(HeatOfBattleText(heat));
        }

        foreach (var check in arithmetic.BerserkChecks ?? [])
        {
            results.Add($"{check.UnitId} Berserk TC {string.Join(", ", check.Dice)}{ModifierText(check.Drm)} = {check.FinalDr} against {check.MoraleLevel}: "
                + $"{(check.Passed ? "goes berserk" : "no change")} (A15.41)");
        }

        if (arithmetic.LeaderCreation is { } creation)
        {
            var created = creation.LeaderDefinitionId is { } leader2 ? $"creates {leader2}" : "creates no leader";
            results.Add($"Leader Creation dr {creation.Dr}{ModifierText(creation.Drm)} = Final dr {creation.FinalDr}: {created} (A18.2)");
        }

        return $"{who}: DR {string.Join(", ", arithmetic.Dice)} = {arithmetic.OriginalDr}{drm} = Final DR {arithmetic.FinalDr} against {arithmetic.MoraleLevel}: {string.Join(", ", results)}";
    }

    /// <summary>
    /// The CC and Ambush records the viewer may see, latest first, in words: each attack's FP, odds, Kill Number, DR with its DRM,
    /// and each defender's result (A11.11), the leaders an Original 2 created (A18.12), and the SW lost (A11.13).
    /// </summary>
    private IReadOnlyList<(string EventId, string Text)> CloseCombatRecords
    {
        get
        {
            var records = new List<(string, string)>();

            // A15.41 (ruling R27.3): the TCs a berserk leader's companions took when it went berserk, by leader, from the fire and Rally records the viewer
            // may see; a CC round names them for each berserk leader in it, since the CC itself has no Heat of Battle.
            var dice = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);
            var companionChecks = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in events.Where(item => item.IsVisibleTo(viewer)))
            {
                if (item.Payload is DiceRolled rolledDice)
                {
                    dice[rolledDice.Roll] = rolledDice.Values;
                }

                // Fire (UI and table player review, pass 27): each Berserk TC names its leader in its DRM, on an attacked unit and a companion alike.
                if (item.Payload is FireResolved fire && fire.Rolls.Keys.Any(key => key.StartsWith(BerserkCheckKey, StringComparison.Ordinal))
                    && fire.Resolution.Deserialize<FireResolution>(LiveFire.Json) is { } fired)
                {
                    var byLeader = fired.Effects.Concat(fired.CompanionEffects ?? [])
                        .SelectMany(effect => effect.Checks.Where(check => check.Kind == "Berserk TC").Select(check => (effect.UnitId, Check: check)))
                        .GroupBy(entry => entry.Check.Drm.Select(drm => drm.Name).FirstOrDefault(name => name.StartsWith(BerserkLeaderDrm, StringComparison.Ordinal))?[BerserkLeaderDrm.Length..]);
                    foreach (var group in byLeader.Where(group => group.Key is not null))
                    {
                        companionChecks[group.Key!] = CompanionText(group.Key!, group.Select(entry => (entry.UnitId, entry.Check.Dice, entry.Check.Passed)));
                    }
                }
                else if (item.Payload is RallyAttempted rally && rally.Rolls.Keys.Any(key => key.StartsWith(BerserkCheckKey, StringComparison.Ordinal))
                    && rally.Resolution.Deserialize<RallyResolution>(LiveFire.Json)?.Effect is { Berserk: true } effect)
                {
                    companionChecks[rally.Unit] = CompanionText(rally.Unit, rally.Rolls.Where(roll => roll.Key.StartsWith(BerserkCheckKey, StringComparison.Ordinal))
                        .OrderBy(roll => roll.Key, StringComparer.Ordinal).Select(roll => (roll.Key[BerserkCheckKey.Length..], dice.GetValueOrDefault(roll.Value) ?? [],
                            (effect.BerserkCompanions ?? []).Contains(roll.Key[BerserkCheckKey.Length..]))));
                }

                if (item.Payload is AmbushRolled ambush && ambush.Resolution.Deserialize<AmbushResolution>(LiveFire.Json) is { } rolled)
                {
                    var sides = string.Join("; ", rolled.Sides.Select(side => $"{side.Side} dr {side.Dr}{ModifierText(side.Drm)} = {side.FinalDr}"));
                    records.Add((item.EventId, $"Ambush in {ambush.Location}: {sides}: {(rolled.Ambusher is { } side ? $"the {side} side ambushes" : "no Ambush")} (A11.4)"));
                }
                else if (item.Payload is CloseCombatResolved combat && combat.Resolution.Deserialize<CloseCombatResolution>(LiveFire.Json) is { } resolution)
                {
                    var attacks = resolution.Attacks.Select(attack =>
                    {
                        // Each defender's own DRM follow the attack's modified DR, then its Final DR (A11.11).
                        var defenders = string.Join(", ", attack.Defending.Select(defender =>
                            $"{defender.UnitId}: {Number(defender.FinalDr - defender.Drm.Sum(item => item.Value))}{ModifierText(defender.Drm)} = Final DR {defender.FinalDr}: {DefenderText(defender)}"));
                        var firepower = attack.FirepowerModifiers.Count == 0 ? "" : $" [{string.Join(", ", attack.FirepowerModifiers.Select(item => $"{DisplayText.Modifier(item.Name)} x{Number(item.Value)}, {item.Rule}"))}]";
                        var creation = attack.LeaderCreation is { } created
                            ? $"; Leader Creation dr {created.Dr}{ModifierText(created.Drm)} = {created.FinalDr}: {(created.LeaderDefinitionId is { } leader ? $"creates {leader}" : "no leader")} (A18.12)"
                            : "";
                        var dice = string.Join(", ", attack.Dice.Select((die, index) => index == 0 ? $"{die} (colored)" : $"{die}"));
                        return $"{string.Join(", ", attack.Attackers)} ({Number(attack.AttackFirepower)} FP{firepower}) attack {string.Join(", ", attack.Defenders)} ({Number(attack.DefenseFirepower)} FP) "
                            + $"at {attack.Odds}, Kill Number {attack.KillNumber}: DR {dice} = {attack.OriginalDr}{ModifierText(attack.Drm)}; {defenders}{creation}"
                            // Pass 31 (play test R-07; A11.22; ruling R14.8): an Original 12 lets the units attacked withdraw, if they declared it with the round.
                            + (attack.OriginalDr == 12 && !resolution.Effects.Any(effect => effect.InfiltratedTo is not null) ? "; an Original 12: no unit attacked had declared a withdrawal (A11.22)" : string.Empty);
                    });
                    var lost = resolution.WeaponEffects.Where(weapon => weapon.Eliminated).Select(weapon => $"{weapon.EquipmentId} lost (dr {weapon.Dr}, A11.13)");
                    var ended = resolution.Effects.Where(effect => effect.BerserkEnded == true).Select(effect => $"{effect.UnitId} is no longer berserk (A15.46)");
                    // A11.21 (pass 70, ruling R70.26): a unit that withdrew into a concealed or hidden enemy's Location was eliminated by the round's own events.
                    var enteredAndLost = resolution.Effects.Any(effect => effect.WithdrewTo is not null || effect.InfiltratedTo is not null)
                        ? events.Where(other => other.Payload is InstanceEliminated && other.Causes.Contains(item.EventId)).Select(other => ((InstanceEliminated)other.Payload).Id)
                            .ToHashSet(StringComparer.Ordinal)
                        : [];
                    string Went(string unitId, string went, string to, string rule) => enteredAndLost.Contains(unitId)
                        ? $"{unitId} {went} to {to}, which holds a concealed or hidden enemy unit, and is eliminated (A11.21)"
                        : $"{unitId} {went} to {to} ({rule})";
                    var withdrew = resolution.Effects.Where(effect => effect.WithdrewTo is not null).Select(effect => Went(effect.UnitId, "withdraws", effect.WithdrewTo!, "A11.2"))
                        .Concat(resolution.Effects.Where(effect => effect.InfiltratedTo is not null).Select(effect => Went(effect.UnitId, "infiltrates", effect.InfiltratedTo!, "A11.22")))
                        .Concat(resolution.Effects.Where(effect => effect.Captured == true).Select(effect => effect.GuardId is { } guard
                            ? $"{effect.UnitId}{(effect.CapturedHalf == true ? " gives up one HS, which" : "")} is captured and guarded by {guard} (A20.22)"
                            : $"{effect.UnitId} is captured but no one can guard it, so it is freed as Unarmed (A20.5)"))
                        .Concat((resolution.EscapeChecks ?? []).Select(check => $"{check.UnitId}'s escape NTC {string.Join("+", check.Dice)} against {check.Morale}: {(check.Passed ? "passed" : "failed")} (A20.55)"))
                        .Concat(resolution.Effects.Where(effect => effect.Escaped == true).Select(effect => $"{effect.UnitId} is no longer guarded (A20.55)"))
                        .Concat(resolution.Effects.Where(effect => effect.RearmedAs is not null).Select(effect => $"{effect.UnitId} is rearmed as {effect.RearmedAs} (A20.551)"))
                        .Concat(resolution.Effects.Where(effect => effect.ConcealmentLost == true).Select(effect => $"{effect.UnitId} loses its concealment (A11.19)"));
                    var left = UnpossessedAfter(item, resolution).Select(weapon => $"{weapon} is left unpossessed in {combat.Location} (A11.13)");

                    // The user, 2026-10-21: the same round as a table, for the activity strip, which reads the record as text no player could follow. Its
                    // inputs are kept and the table is built only when it is asked for (the latest round), not for every round at each revision.
                    string[] notes = [.. lost.Concat(ended).Concat(withdrew).Concat(left)];
                    combatInputs[item.EventId] = (combat, resolution, notes);
                    var text = resolution.Attacks.Count == 0 ? "no attacks" : string.Join(" | ", attacks);
                    var hand = combat.Facts.TryGetProperty("handToHand", out var handToHand) && handToHand.ValueKind == JsonValueKind.True ? ", Hand-to-Hand (red Kill Numbers)" : "";
                    // Shown with the first CC record after the leader went berserk (UI review, pass 27).
                    var companions = combat.Attackers.Concat(combat.Defenders).Distinct(StringComparer.Ordinal)
                        .Select(unit => companionChecks.Remove(unit, out var checks) ? checks : null).OfType<string>().ToArray();
                    records.Add((item.EventId, $"CC in {combat.Location}, {combat.Round} round{hand}: {string.Join("; ", new[] { text }.Concat(notes).Concat(companions))}"));
                }
            }

            records.Reverse();
            return records;
        }
    }

    // The CC rounds the view may read, by event, with what their tables are built from; a table is built when first asked for and then kept, for
    // this record set's life (one revision and view).
    private readonly Dictionary<string, (CloseCombatResolved Combat, CloseCombatResolution Resolution, string[] Notes)> combatInputs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CloseCombatTable> combatTables = new(StringComparer.Ordinal);

    /// <summary>A CC round as a table (the user, 2026-10-21), in the view's words as of the round; null for an event that is no CC round the view may read.</summary>
    public CloseCombatTable? CombatTable(string eventId)
    {
        ArgumentNullException.ThrowIfNull(eventId);
        _ = CloseCombat;
        if (combatTables.TryGetValue(eventId, out var table))
        {
            return table;
        }

        if (!combatInputs.TryGetValue(eventId, out var inputs))
        {
            return null;
        }

        table = CombatTableOf(eventId, inputs.Combat, inputs.Resolution, inputs.Notes);
        combatTables[eventId] = table;
        return table;
    }

    /// <summary>
    /// One CC round as a table (the user, 2026-10-21): per attack its sides, FP, odds, Kill Number, dice, and DRM, the DRM every defender shares said
    /// once, and a row per defender with its own DRM, Final DR, and result, the exceptions first; the round's other effects as notes.
    /// </summary>
    private CloseCombatTable CombatTableOf(string eventId, CloseCombatResolved combat, CloseCombatResolution resolution, IReadOnlyList<string> notes)
    {
        string Said(string text) => Say(eventId, text);
        string Modifier(FireModifier item) => Said(ModifierText([item]).Trim());
        var attacks = resolution.Attacks.Select(attack =>
        {
            // A DRM every defender has (same name and value) is said once for the attack.
            var shared = attack.Defending.Count == 0 ? []
                : attack.Defending[0].Drm.Where(item => attack.Defending.All(defender => defender.Drm.Any(other => other.Name == item.Name && other.Value == item.Value))).ToArray();
            var modified = attack.OriginalDr + attack.Drm.Sum(item => item.Value);
            var rows = attack.Defending.Select(defender => new CloseCombatTableRow(
                    Said(defender.UnitId),
                    [.. defender.Drm.Where(item => !shared.Any(other => other.Name == item.Name && other.Value == item.Value)).Select(Modifier)],
                    defender.FinalDr,
                    DefenderText(defender),
                    defender.Result == CloseCombatDefenderResult.Eliminated))
                .OrderBy(row => row.Eliminated ? 1 : 0).ToArray();
            var eliminated = rows.Count(row => row.Eliminated);
            var outcome = string.Join(", ", new[]
            {
                eliminated > 0 ? $"eliminated {eliminated}" : null,
                rows.Count(row => row.Result.StartsWith("Casualty", StringComparison.Ordinal)) is > 0 and var reduced ? $"reduced {reduced}" : null,
                rows.Count(row => row.Result.StartsWith("spared", StringComparison.Ordinal)) is > 0 and var spared ? $"spared {spared}" : null,
                rows.Count(row => row.Result == "captured") is > 0 and var captured ? $"captured {captured}" : null,
                rows.Count(row => row.Result == "no effect") is > 0 and var untouched && untouched < rows.Length ? $"no effect on {untouched}" : null,
            }.OfType<string>());
            return new CloseCombatTableAttack(
                DisplayText.Side(attack.Side),
                [.. attack.Attackers.Select(Said)],
                [.. attack.Defenders.Select(Said)],
                $"{Number(attack.AttackFirepower)} FP"
                    + (attack.FirepowerModifiers.Count == 0 ? string.Empty : $" ({string.Join(", ", attack.FirepowerModifiers.Select(item => $"{DisplayText.Modifier(item.Name)} x{Number(item.Value)}, {item.Rule}"))})"),
                $"{Number(attack.DefenseFirepower)} FP",
                attack.Odds,
                attack.KillNumber,
                string.Join(" + ", attack.Dice.Select((die, index) => index == 0 ? $"{die} (colored)" : $"{die}")) + $" = {attack.OriginalDr}",
                [.. attack.Drm.Select(Modifier)],
                modified,
                [.. shared.Select(Modifier)],
                rows,
                outcome.Length == 0 ? "no effect" : outcome,
                attack.LeaderCreation is { } created
                    ? Said($"Leader Creation dr {created.Dr}{ModifierText(created.Drm)} = {created.FinalDr}: {(created.LeaderDefinitionId is { } leader ? $"creates {leader}" : "no leader")} (A18.12)")
                    : null);
        }).ToArray();
        var hand = combat.Facts.TryGetProperty("handToHand", out var handToHand) && handToHand.ValueKind == JsonValueKind.True ? ", Hand-to-Hand (red Kill Numbers)" : "";
        return new CloseCombatTable(Said(combat.Location.ToString()), $"{combat.Round} round{hand}", attacks, [.. notes.Select(Said)]);
    }

    /// <summary>The roll key of a berserk leader's companion's TC in fire and Rally records (A15.41).</summary>
    private const string BerserkCheckKey = "berserkCheck:";

    /// <summary>The DRM a Berserk TC in a fire record takes from its leader, which names him (A15.41).</summary>
    private const string BerserkLeaderDrm = "berserk-leader:";

    /// <summary>A berserk leader's companions' TCs in words (A15.41; ruling R27.3).</summary>
    private static string CompanionText(string leader, IEnumerable<(string Unit, IReadOnlyList<int> Dice, bool Berserk)> checks) =>
        $"{leader} went berserk before this CC; its companions' TCs then: {string.Join(", ", checks.Select(check => $"{check.Unit} {string.Join("+", check.Dice)}: {(check.Berserk ? "goes berserk with it" : "stays")}"))} (A15.41)";

    /// <summary>The SW the units a CC round eliminated possessed before it, less those it eliminated: they stay in the Location unpossessed.</summary>
    private IEnumerable<string> UnpossessedAfter(GameEvent combat, CloseCombatResolution resolution)
    {
        var eliminated = resolution.Effects.Where(effect => effect.Eliminated).Select(effect => effect.UnitId).ToHashSet(StringComparer.Ordinal);
        if (eliminated.Count == 0)
        {
            return [];
        }

        // The state before the round: the history holds it, so it is not replayed again.
        var before = history.At(combat.Revision - 1);
        var lost = resolution.WeaponEffects.Where(weapon => weapon.Eliminated).Select(weapon => weapon.EquipmentId).ToHashSet(StringComparer.Ordinal);
        return before is null ? []
            // Pass 70 (ruling R70.27): a Gun its eliminated crew manned is left too, unless A11.13 eliminated it.
            : before.Equipment.Where(weapon => weapon.Status == InstanceStatus.Active && weapon.Holding is { Role: HoldingRole.Possessed or HoldingRole.Manned } holding
                && eliminated.Contains(holding.Holder) && !lost.Contains(weapon.Id)).Select(weapon => weapon.Id).Order(StringComparer.Ordinal);
    }

    private static string DefenderText(CloseCombatDefenderResult defender) => defender.Result switch
    {
        CloseCombatDefenderResult.Eliminated => "eliminated",
        CloseCombatDefenderResult.Captured => "captured",
        CloseCombatDefenderResult.PartialKill => defender.CasualtyReduced == true
            ? "Casualty Reduction" + (defender.RandomSelectionDr is { } dr ? $" (Random Selection dr {dr})" : "")
            : $"spared (Random Selection dr {defender.RandomSelectionDr})",
        _ => "no effect",
    };

    /// <summary>The Wind Change DRs and Starshell attempts the viewer may see (rulings R16.8, R16.10), latest first.</summary>
    private IReadOnlyList<(string EventId, string Text)> NightRecords => [.. events.Where(item => item.IsVisibleTo(viewer) && item.Payload is WindChanged or StarshellFired).Reverse().Select(item => (item.EventId, item.Payload switch
        {
            WindChanged wind => $"Wind Change DR ({wind.Roll})"
                + (wind.Nvr is { } nvr ? $": the Base NVR is {nvr}" : string.Empty)
                + (wind.Precipitation is { } falling ? $"; {falling.Replace('-', ' ')} falls" : string.Empty)
                + (wind.Gust ? "; a Gust blows" : string.Empty) + " (B25.65, E1.12, E3.51)",
            StarshellFired starshell => $"{starshell.Unit}{In(history.At(item.Revision - 1), starshell.Unit)} fires a Starshell ({starshell.Method}): "
                + (!starshell.Passed ? "its Usage dr fails (E1.921)"
                    : starshell.At is { } at ? $"it lands in {at} and Illuminates three hexes around it (E1.922, E1.923)" : "it lands off the map"),
            _ => string.Empty,
        }))];

    /// <summary>The Sniper attacks, Sniper Checks, and SAN reductions the viewer may see (A14; ruling R15.5; pass 71, rulings R71.9, R71.10), latest first.</summary>
    private IReadOnlyList<(string EventId, string Text)> SniperRecords => [.. events.Where(item => item.IsVisibleTo(viewer) && item.Payload is SniperAttacked or SniperChecked or SanReduced)
        .Reverse().Select(item => (item.EventId, item.Payload switch
        {
            SniperChecked check => SniperCheckText(check),
            SanReduced reduced => $"The {DisplayText.Side(reduced.Side)} Sniper is eliminated by {(reduced.Cause == ScenarioA1Sniper.SniperCheckCause ? "a Sniper Check" : "the enemy Sniper")}: "
                + (ActualSanAfter(item, reduced.Side) is { } san
                    ? $"the {DisplayText.Side(reduced.Side)} SAN is now {san.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                        + (ScenarioA1Sniper.CounterRemoved(san) ? ", below 2, so its Sniper counter is removed (A14.1, A14.4)" : " (A14.4)")
                    : $"the {DisplayText.Side(reduced.Side)} SAN is one lower (A14.4)"),
            _ => SniperAttackText((SniperAttacked)item.Payload),
        }))];

    private string SniperAttackText(SniperAttacked sniper)
    {
        var side = state?.Find(sniper.Sniper)?.Side ?? "a";
        return $"The {side} Sniper ({sniper.Sniper}) attacks after the SAN roll {sniper.Trigger}: dr {sniper.Dr}"
            + (sniper.Target is { } at ? $"; {at}: {SniperResultText(sniper.Unit, sniper.Result)}" : sniper.Result == ScenarioA1Sniper.NoTarget
                ? ", no target in reach (A14.21, A14.22)" : ", no effect (A14.3)");
    }

    /// <summary>A Sniper Check (A14.4; pass 71, ruling R71.10): its units, its DR and DRM, and its result.</summary>
    private string SniperCheckText(SniperChecked check)
    {
        var rolled = events.Select(item => item.Payload).OfType<DiceRolled>().FirstOrDefault(roll => roll.Roll == check.Roll)?.Values;
        return $"{string.Join(", ", check.Units)} in {check.Location} make a Sniper Check against the {DisplayText.Side(state?.Find(check.Sniper)?.Side)} Sniper ({check.Sniper})"
            + (rolled is { Count: > 0 } ? $": DR {string.Join(", ", rolled)} = {rolled.Sum()}" + (check.Drm != 0 ? $" {(check.Drm < 0 ? "-" : "+")} {Math.Abs(check.Drm)} = {rolled.Sum() + check.Drm}" : string.Empty)
                : string.Empty)
            + check.Result switch
            {
                ScenarioA1Sniper.SniperEliminated => ", the Sniper is eliminated",
                ScenarioA1Sniper.SniperPinned => ", the Sniper is pinned and makes no further attack this Player Turn (A14.31)",
                _ => ", no effect",
            } + "; they are TI (A14.4)";
    }

    /// <summary>A side's actual SAN after a reduction (A14.4, E1.76; ruling R71.9), as Rules gives it from the state after the event.</summary>
    private int? ActualSanAfter(GameEvent item, string side) => history.At(item.Revision) is { } after && after.Side(side) is { San: { } printed } reduced
        ? ScenarioA1Sniper.ActualSan(printed, after.Night, reduced.SanReductions)
        : null;

    /// <summary>
    /// What a Sniper attack did in its Location (A14.3; pass 71, the record lines): the unit by its id, which the view's names make "4-6-7 squad G1", or a
    /// concealed unit that keeps its "?" (A12.14; ruling R71.8), which the record does not name.
    /// </summary>
    private static string SniperResultText(string? unit, string result) => (unit, result) switch
    {
        (_, ScenarioA1Sniper.DummyStackEliminated) => "a Dummy stack is eliminated (A14.23)",
        (null, ScenarioA1Sniper.NoEffect) => "no effect (A14.3)",

        // A12.14 (ruling R71.8): a target that keeps its "?" is not named, nor is what it is.
        (null, _) => "a concealed unit is attacked and keeps its \"?\", out of every Good Order enemy LOS (A12.14, A14.3)",
        (_, ScenarioA1Sniper.Stunned) => $"the crew of {unit} is Stunned (A14.3, D5.34)",
        (_, ScenarioA1Sniper.Recalled) => $"the crew of {unit} is Recalled (A14.3, D5.341)",
        (_, ScenarioA1Sniper.CrewBroken) => $"the Inherent crew of {unit} breaks (A14.3)",
        (_, ScenarioA1Sniper.CrewPinned) => $"the Inherent crew of {unit} is pinned (A14.3)",
        (_, ScenarioA1Sniper.Immobilized) => $"{unit} is immobilized (A14.33)",
        (_, ScenarioA1Sniper.SniperEliminated) => $"the enemy Sniper ({unit}) is eliminated (A14.3, A14.31)",
        (_, ScenarioA1Sniper.SniperPinned) => $"the enemy Sniper counter ({unit}) is pinned and makes no further attack this Player Turn (A14.31)",
        (_, ScenarioA1Sniper.NoEffect) => $"{unit} is unaffected (A14.3)",
        (_, ScenarioA1Sniper.CasualtyReduced) => $"{unit} is Casualty Reduced (A14.3)",
        _ => $"{unit} is {result} (A14.3)",
    };

    /// <summary>A change of DM alone, which the gate adds to any plan that leaves a broken unit ADJACENT to its enemy (A10.62; ruling R13.1).</summary>
    private static bool IsDmOnly(EventPayload payload) => payload is ConditionsChanged { Conditions: var changed } && changed.Keys.All(key => key == Conditions.DesperationMorale);

    /// <summary>The attempt an event belongs to: its id without the event's number.</summary>
    private static string AttemptOf(string eventId) => ReplaySteps.AttemptOf(eventId);

    /// <summary>Who takes which SW when a squad Deploys (A1.31), such as "; r1-2 takes r-lmg".</summary>
    private static string DeploySplit(IEnumerable<GameEvent> attempt, LineageRecorded lineage) => string.Concat(lineage.Produced.Select(half =>
        attempt.Select(other => other.Payload).OfType<EquipmentTransferred>().Where(moved => moved.Holding?.Holder == half.Id).Select(moved => moved.Id).ToArray()
            is { Length: > 0 } taken ? $"; {half.Id} takes {string.Join(", ", taken)}" : ""));

    /// <summary>
    /// Whether a record may name a unit to the view (pass 28b, section 15.4): the adjudicator, the unit's own side, or a unit then neither concealed
    /// nor hidden.
    /// </summary>
    private bool Open(GameState? before, string id) => viewer.IsAdjudicator || (before?.Unit(id) is { } unit && (unit.Side == viewer.Name
        || (GameState.Condition(unit, Conditions.Concealed) != ConditionState.True && GameState.Condition(unit, Conditions.Hidden) != ConditionState.True)));
}
