using System.Globalization;
using System.Text.Json;
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
                string[] said = [.. (ofLast.Any(line => !line.Minor) ? ofLast.Where(line => !line.Minor) : ofLast).Select(line => Sentence(line.Text))];
                // The result of the attempt first, then what followed from it; a long attempt is cut, and the records have the rest.
                latest = (true, said.Length == 0 ? null : $"Latest: {WhenOf(from[^1].EventId)}: {string.Join(" ", said.Take(4))}{(said.Length > 4 ? " More is in the records." : string.Empty)}");
            }

            return latest.Text;
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
            var text = item.Payload switch
            {
                MovementStepped step => Many(step.Movers, "moves", "move") + $"{From(before, step.Movers.Count > 0 ? step.Movers[0] : null)} to {step.To}{(step.Assault ? " by Assault Movement" : string.Empty)}",
                VehicleStepped { Kind: not VehicleStepped.Turn } drive => $"{drive.Vehicle} moves{From(before, drive.Vehicle)} to {drive.At}",
                AdvanceMoved advance => Many(advance.Units, "advances", "advance") + (advance.Exit is { } edge ? $" off the {edge} edge from {advance.To}" : $"{From(before, advance.Units.Count > 0 ? advance.Units[0] : null)} to {advance.To}"),
                MovementWindowClosed => $"The {DisplayText.Side(before?.Sides.FirstOrDefault(side => side.Id != before.PhasingSide)?.Id)} side, the DEFENDER, declines First Fire",
                MovementEnded ended => Many(ended.Movers, "ends its move", "end their move") + In(before, ended.Movers.Count > 0 ? ended.Movers[0] : null),
                OpportunityFireDeclared held => string.Join(", ", held.Units.Select(id => id + In(before, id))) + (held.Units.Count == 1 ? " is" : " are") + " held for Opportunity Fire (A7.25)",
                ChoiceMade => "A choice the game waited for was answered",
                SurrenderPending surrender => $"{surrender.Unit}{In(before, surrender.Unit)} surrenders, and its captor is to be chosen (A20.21)",
                SurrenderRejected refused => $"The surrender of {refused.Unit} is refused (A20.3)",
                InstanceCaptured taken => $"{taken.Id}{In(before, taken.Id)} is captured, guarded by {taken.Custodian} (A20.2)",
                PrisonerFreed freed => $"{freed.Unit}{In(before, freed.Unit)} is no longer guarded (A20.5)",
                BuildingMoppedUp mopped => DisplayText.Hexes($"The {DisplayText.Side(mopped.Side)} side Mops Up building {mopped.Building} (A12.153)", history.States.Count > 0 ? history.States[0].Map.Boards.Count : 1),
                PhaseChanged phase => $"The {DisplayText.Side(phase.PhasingSide)} {GameText.PhaseLabel(phase.Phase)} of Turn {phase.Turn} begins",
                GameEnded => "The game ends",
                _ => null,
            };
            if (text is not null)
            {
                Add(item.EventId, Say(item.EventId, text), item.Payload is PhaseChanged or ChoiceMade);
            }
        }

        // The order it happened in; lines of one event keep the order they were read in.
        return [.. read.Select((line, index) => (line, index)).OrderBy(pair => pair.line.Revision).ThenBy(pair => pair.index).Select(pair => pair.line)];
    }

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

        // Pass 31d (design D7; A6.11, p. 53): a blocked LOS is said as what it was, and not as an attack that missed.
        return fire.LosBlocked
            ? $"{fire.Group} {(fire.Several ? "fire" : "fires")} at {fire.Target}: the LOS is blocked, so the attack has no effect and its firers have fired (A6.11)"
            : $"{fire.Group} {(fire.Several ? "fire" : "fires")} at {fire.Target}: {ResultText(fire.Arithmetic.Result)}" + (changed.Length > 0 ? "; " + string.Join("; ", changed) : string.Empty);
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
                if (item.Payload is OrdnanceFired { } checking && checking.Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json) is { ToHit: null, PanzerfaustCheck: { } failed })
                {
                    // C13.31 (ruling R9.7): a PF Check that gave no shot.
                    records.Add((item.EventId, $"{checking.Crew}{In(history.At(item.Revision - 1), checking.Crew)} makes a PF Check: dr {failed.Dr}{ModifierText(failed.Drm)} = {failed.FinalDr}: "
                        + (failed.Outcome == OrdnancePanzerfaustCheck.NoShot ? "no shot (C13.31)" : $"no PF in position, and {failed.Outcome} (C13.31)")));
                    continue;
                }

                if (item.Payload is not OrdnanceFired fired || fired.Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json) is not { ToHit: { } hit, Gun: { } gun } resolution)
                {
                    continue;
                }

                var dice = string.Join(", ", hit.Dice.Select((die, index) => index == 0 ? $"{die} (colored)" : $"{die}"));
                var modifications = string.Concat(hit.Modifications.Select(item => $" {(item.Value < 0 ? "-" : "+")} {Number(Math.Abs(item.Value))} ({DisplayText.Modifier(item.Name)}, {item.Rule})"));
                var outcome = hit.CriticalHit ? "Critical Hit" : hit.Hit ? "hit" : "miss";
                var shot = fired.Facts.Deserialize<OrdnanceShot>(LiveFire.Json);
                var aim = shot?.VehicleTarget is { } aimed ? $"{(shot.Ammunition ?? "ap").ToUpperInvariant()} at {aimed.VehicleId} in {fired.Target} (hull {aimed.HullFacing}"
                    + (aimed.TurretFacing is { } turretFacing ? $", turret {turretFacing}" : "") + " toward the firer)"
                    : shot?.TargetType == OrdnanceTargetTypes.Area ? $"HE at {fired.Target} on the Area Target Type" + (shot.Spotter is { } spotter ? $", spotted by {spotter.UnitId}" : "")
                    : $"HE at {fired.Target}";
                var check = resolution.PanzerfaustCheck is { } pf ? $"PF Check dr {pf.Dr}{ModifierText(pf.Drm)} = {pf.FinalDr}: a shot; " : "";
                var parts = new List<string>
                {
                    check + $"{fired.Gun}{In(history.At(item.Revision - 1), fired.Gun)} fires {aim}"
                        + (fired.Facing is { } facing ? $", turning to {UnitFacings.Name(facing)}" : "")
                        + $": Basic TH# {hit.BasicToHit} ({hit.Color}){modifications} = {hit.ModifiedToHit}; DR {dice} = {hit.OriginalDr}{ModifierText(hit.Drm)} = Final DR {hit.FinalDr}"
                        + (hit.SubsequentDr is { } dr ? $", subsequent dr {dr}" : "") + (hit.Improbable ? " (Improbable Hit, C3.6)" : "") + $": {outcome}",
                };
                if (resolution.AreaTargets is { } judged)
                {
                    parts.Add("each unit: " + string.Join(", ", judged.Select(unit => $"{unit.UnitId} Final DR {unit.FinalDr}{ModifierText(unit.Drm)} {(unit.Hit ? "hit" : "missed")}")));
                }

                if (resolution.FirerEffect is { } firerEffect)
                {
                    parts.Add($"{fired.Crew}: {firerEffect} (C13.36)");
                }

                foreach (var (label, attack) in new[] { ("Critical Hit", resolution.CriticalHit), ("hit", resolution.Hit) })
                {
                    if (attack?.Arithmetic is { } arithmetic)
                    {
                        parts.Add($"{label} on {string.Join(", ", attack.Effects.Select(effect => effect.UnitId))}: {arithmetic.ColumnFp} FP, IFT DR {string.Join(", ", arithmetic.Dice)} = {arithmetic.OriginalDr}"
                            + $"{ModifierText(arithmetic.Drm)} = {arithmetic.FinalDr}: {arithmetic.Result}");
                    }
                }

                if (resolution.AmmunitionUse == "none")
                {
                    parts.Add($"the Original DR is above the Depletion Number: it had no {shot?.Ammunition?.ToUpperInvariant()}, has not fired, and has none for the rest of the scenario (C8.9)");
                }
                else if (resolution.AmmunitionUse == "depleted")
                {
                    parts.Add($"the Original DR equals the Depletion Number: its last {shot?.Ammunition?.ToUpperInvariant()} (C8.9)");
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
                        _ => "no effect",
                    };
                    var checks = (kill.ShockCheck is { } ntc ? $"; NTC DR {ntc.FinalDr} against {ntc.MoraleLevel}: {(ntc.Passed ? "passed" : "Shocked")}" : "")
                        + (kill.CrewCheck is { } tc ? $"; crew TC DR {tc.FinalDr} against {tc.MoraleLevel}: {(tc.Passed ? "passed" : "Abandoned")}" : "")
                        + (kill.CrewSurvival is { } cs ? $"; Crew Survival DR {cs.FinalDr} against CS# {cs.CrewSurvival}: {(cs.Survived ? "the crew survives" : "no survivors")}" : "");
                    parts.Add($"{kill.HitLocation} hit, {kill.TargetFacing} Target Facing, {kill.Ammunition.ToUpperInvariant()}: {tk}; TK DR {string.Join(", ", kill.Dice)} = {kill.OriginalDr}: {fate}{checks}");
                }

                parts.Add((gun.Malfunctioned ? "the Gun malfunctions (C2.28)" : gun.RateOfFireKept ? "it may fire again this phase (C2.24)" : "no more fire this phase")
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
                    var before = fire.Facts.Deserialize<FireAttack>(LiveFire.Json)?.Targets ?? [];
                    views.Add(new FireView(item.EventId, Say(item.EventId, GroupOf(fire)), Say(item.EventId, fire.TargetLocation), null, arithmetic, resolution.Effects)
                    {
                        Say = text => Say(item.EventId, text),
                        Several = fire.Firers.Count > 1,
                        LosBlocked = resolution.LosBlocked == true,
                        Weapons = resolution.WeaponEffects,
                        Vehicles = resolution.VehicleEffects,
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
            return $"{byVehicle.VehicleId}'s AAMG in {byVehicle.LocationId}";
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
                else if (item.Payload is RoutStepped routed)
                {
                    // Pass 31d (design D11): the steps of one rout are one sentence, "routs from [Y5] by [Z5] to [AA5] for 3 MF", and name the unit once.
                    var key = (AttemptOf(item.EventId), routed.Unit);
                    if (routs.TryGetValue(key, out var earlier))
                    {
                        routs[key] = earlier = (earlier.Index, earlier.From, [.. earlier.Steps, routed.To.ToString()], earlier.HalfMf + routed.HalfMf);
                        var total = (earlier.HalfMf / 2.0).ToString("0.#", CultureInfo.InvariantCulture);
                        records[earlier.Index] = (records[earlier.Index].Item1, "rout",
                            $"{routed.Unit} routs{earlier.From} by {string.Join(" then ", earlier.Steps.Take(earlier.Steps.Count - 1))} to {earlier.Steps[^1]} for {total} MF (A10.5)");
                    }
                    else
                    {
                        var mf = (routed.HalfMf / 2.0).ToString("0.#", CultureInfo.InvariantCulture);
                        routs[key] = (records.Count, From(before, routed.Unit), [routed.To.ToString()], routed.HalfMf);
                        records.Add((item.EventId, "rout", $"{routed.Unit} routs{From(before, routed.Unit)} to {routed.To} for {mf} MF{(routed.LowCrawl ? " by Low Crawl (A10.52)" : " (A10.5)")}"));
                    }
                }
                else if (item.Payload is DiceRolled { Purpose: ScenarioA1Wounds.SeverityPurpose } severity)
                {
                    // Pass 35 (task 35.1; A17.11): the Wound Severity dr of a SMC's Casualty Reduction, said where it is made.
                    records.Add((item.EventId, "wound", ScenarioA1Wounds.SeverityText(severity.Values[0])));
                }
                else if (item.Payload is RoutInterdicted interdicted)
                {
                    var dr = rolls.TryGetValue(interdicted.Roll, out var roll) ? roll.Values.Sum().ToString(CultureInfo.InvariantCulture) : "?";
                    records.Add((item.EventId, "interdiction",
                        $"Interdiction of {interdicted.Unit} entering {interdicted.At}{(interdicted.Interdictor is { } by ? $" by {by}" : "")}: NMC DR {dr} against broken morale {interdicted.Morale}: {interdicted.Result} (A10.53)"));
                }
                else if (item.Payload is LineageRecorded { Action: LineageAction.Deployed or LineageAction.Recombined } lineage && lineage.Consumed.All(id => Open(before, id)))
                {
                    records.Add((item.EventId, "lineage", lineage.Action == LineageAction.Deployed
                        ? $"{lineage.Consumed[0]}{In(before, lineage.Consumed[0])} becomes the HS {string.Join(" and ", lineage.Produced.Select(unit => unit.Id))} (A1.31){DeploySplit(attempts[attempt], lineage)}"
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
                    && attempts[attempt].Count(other => !IsDmOnly(other.Payload)) == 1
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
                else if (item.Payload is RepairAttempted repair)
                {
                    var dr = rolls.TryGetValue(repair.Roll, out var roll) ? roll.Values[0].ToString(CultureInfo.InvariantCulture) : "?";
                    var result = repair.Result switch
                    {
                        RepairAttempted.Repaired => "repaired",
                        RepairAttempted.Eliminated => "eliminated (a 6)",
                        _ => "still malfunctioned",
                    };
                    records.Add((item.EventId, "repair", $"{repair.Unit}{In(before, repair.Unit)} repairs {repair.Equipment}: dr {dr} against R{repair.RepairNumber}: {result}"));
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
                    var withdrew = resolution.Effects.Where(effect => effect.WithdrewTo is not null).Select(effect => $"{effect.UnitId} withdraws to {effect.WithdrewTo} (A11.2)")
                        .Concat(resolution.Effects.Where(effect => effect.InfiltratedTo is not null).Select(effect => $"{effect.UnitId} infiltrates to {effect.InfiltratedTo} (A11.22)"))
                        .Concat(resolution.Effects.Where(effect => effect.Captured == true).Select(effect => effect.GuardId is { } guard
                            ? $"{effect.UnitId}{(effect.CapturedHalf == true ? " gives up one HS, which" : "")} is captured and guarded by {guard} (A20.22)"
                            : $"{effect.UnitId} is captured but no one can guard it, so it is freed as Unarmed (A20.5)"))
                        .Concat((resolution.EscapeChecks ?? []).Select(check => $"{check.UnitId}'s escape NTC {string.Join("+", check.Dice)} against {check.Morale}: {(check.Passed ? "passed" : "failed")} (A20.55)"))
                        .Concat(resolution.Effects.Where(effect => effect.Escaped == true).Select(effect => $"{effect.UnitId} is no longer guarded (A20.55)"))
                        .Concat(resolution.Effects.Where(effect => effect.RearmedAs is not null).Select(effect => $"{effect.UnitId} is rearmed as {effect.RearmedAs} (A20.551)"))
                        .Concat(resolution.Effects.Where(effect => effect.ConcealmentLost == true).Select(effect => $"{effect.UnitId} loses its concealment (A11.19)"));
                    var left = UnpossessedAfter(item, resolution).Select(weapon => $"{weapon} is left unpossessed in {combat.Location} (A11.13)");
                    var text = resolution.Attacks.Count == 0 ? "no attacks" : string.Join(" | ", attacks);
                    var hand = combat.Facts.TryGetProperty("handToHand", out var handToHand) && handToHand.ValueKind == JsonValueKind.True ? ", Hand-to-Hand (red Kill Numbers)" : "";
                    // Shown with the first CC record after the leader went berserk (UI review, pass 27).
                    var companions = combat.Attackers.Concat(combat.Defenders).Distinct(StringComparer.Ordinal)
                        .Select(unit => companionChecks.Remove(unit, out var checks) ? checks : null).OfType<string>().ToArray();
                    records.Add((item.EventId, $"CC in {combat.Location}, {combat.Round} round{hand}: {string.Join("; ", new[] { text }.Concat(lost).Concat(ended).Concat(withdrew).Concat(left).Concat(companions))}"));
                }
            }

            records.Reverse();
            return records;
        }
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
            : before.Equipment.Where(weapon => weapon.Status == InstanceStatus.Active && weapon.Holding is { Role: HoldingRole.Possessed } holding
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

    /// <summary>The Sniper attacks the viewer may see (A14; ruling R15.5), latest first.</summary>
    private IReadOnlyList<(string EventId, string Text)> SniperRecords => [.. events.Where(item => item.IsVisibleTo(viewer) && item.Payload is SniperAttacked).Reverse().Select(item =>
        {
            var sniper = (SniperAttacked)item.Payload;
            var side = state?.Find(sniper.Sniper)?.Side ?? "a";
            var text = $"The {side} Sniper ({sniper.Sniper}) attacks after the SAN roll {sniper.Trigger}: dr {sniper.Dr}"
                + (sniper.Target is { } at ? $", at {at}" : string.Empty)
                + (sniper.Unit is { } unit ? $": {unit} is {sniper.Result} (A14.3)" : sniper.Result == "none" ? ", no effect (A14.3)" : $": {sniper.Result} (A14.21, A14.23)");
            return (item.EventId, text);
        })];

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
