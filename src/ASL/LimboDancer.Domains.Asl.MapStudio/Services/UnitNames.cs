using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The names a view reads for a game's units (pass 31c, design D11): a unit's printed values, its kind, and a tag that never changes, such as
/// "4-6-7 squad G4" and "9-2 leader G2". A side's own units are numbered in the order they entered the game, and the adjudicator reads every unit by
/// its owner's tag. A unit that takes another's place keeps its tag; a Deployed HS adds "a" or "b". The user's ruling of 2026-10-22 (option 2): the
/// other side's units carry no tag in a side's view; one the view holds reads by its counter and its Location at that moment ("4-4-7 squad in
/// [W4]"), so a unit has one name in every view and nothing follows it through concealment. A unit the view does not hold reads "a concealed unit", or
/// "an uninspected unit" beneath the top of a stack the view may not inspect.
/// Play, Replay, and the records read the one instance kept for a game and a view; it grows with the game and what it has given is never changed.
/// </summary>
public sealed partial class UnitNames
{
    /// <summary>What a view reads for a unit it may not name (rulings R23.1 and R19.8).</summary>
    public const string Unnamed = "a concealed unit";

    /// <summary>
    /// What a view reads for a counter beneath the top of an enemy stack it may not inspect, under no "?" (A2.9, A12.16; rulings R23.3, R70.18; the
    /// referee's finding at the reviews of pass 70).
    /// </summary>
    public const string UninspectedUnit = "an uninspected unit";

    /// <summary>Whether a name is one a view reads for a unit it may not name: <see cref="Unnamed"/> or <see cref="UninspectedUnit"/>.</summary>
    public static bool IsUnnamed(string name) => name is Unnamed or UninspectedUnit;

    /// <summary>What a view reads for a weapon it does not hold.</summary>
    public const string UnseenWeapon = "a weapon not in view";

    private static readonly ConcurrentDictionary<(string First, string Viewer), UnitNames> Kept = new();

    private readonly Perspective viewer;
    private readonly IReadOnlyDictionary<string, UnitDefinition> definitions;
    private readonly Func<GameState, GameView>? viewOf;
    private readonly Lock gate = new();

    // What every view may be told of an id: its side, its kind, the revision that made it, and the units it came from.
    private readonly Dictionary<string, (string? Side, string Kind, bool Unit)> known = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> born = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (LineageAction Action, IReadOnlyList<string> From, int Index)> lineage = new(StringComparer.Ordinal);

    // The tags by entry, which a unit's own side and the adjudicator read; and the other side's units and weapons this view has held by name.
    private readonly Dictionary<string, string> entered = new(StringComparer.Ordinal);
    private readonly HashSet<string> seen = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Side, string Group), int> enteredCount = [];

    // The other side's units and weapons this view held at each revision, and the units it counted beneath an uninspected stack's top (null for none).
    private readonly List<HashSet<string>> held = [];
    private readonly List<HashSet<string>?> uninspected = [];
    private GameHistory history;
    private long through;
    private string lastEvent = string.Empty;
    private Regex? pattern;
    private int patternIds;

    private UnitNames(GameHistory history, Perspective viewer, IReadOnlyDictionary<string, UnitDefinition> definitions, Func<GameState, GameView>? viewOf) =>
        (this.history, this.viewer, this.definitions, this.viewOf) = (history, viewer, definitions, viewOf);

    /// <summary>
    /// The names of <paramref name="history"/> for <paramref name="viewer"/>. <paramref name="viewOf"/> builds the viewer's view of a state, with no
    /// events (the planner's one entry: the OB groups a side may not see while they set up, ruling R23.3, and the stacks it may not inspect, ruling
    /// R70.18); without it the view is the state's alone. A counter the view counts but does not show reads as any unit the view may not name. The same
    /// game read again, longer, keeps every tag it has given.
    /// </summary>
    public static UnitNames For(GameHistory history, Perspective viewer, IReadOnlyList<UnitCatalog> catalogs, Func<GameState, GameView>? viewOf = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(catalogs);
        var first = history.Events.Count > 0 ? $"{history.Events[0].Scope.Game}/{history.Events[0].EventId}" : string.Empty;
        var key = (first, viewer.Name);
        if (Kept.TryGetValue(key, out var kept))
        {
            lock (kept.gate)
            {
                var count = Math.Min(history.Events.Count, history.States.Count);
                var same = kept.through == 0 || (kept.through <= count && history.Events[(int)kept.through - 1].EventId == kept.lastEvent);
                if (same)
                {
                    kept.Advance(history);
                    return kept;
                }

                // A shorter reading of the same game (another tab that has not read the latest commit) is served by the names already made.
                if (kept.through > count && count > 0 && kept.history.Events.Count >= count && kept.history.Events[count - 1].EventId == history.Events[count - 1].EventId)
                {
                    return kept;
                }
            }
        }

        // A new game, or another game under the same first event (a copy cut back and played on differently): made whole before any reader has it
        // (the UI review, pass 31c). The cache is small: the games a Studio has open, not every game it ever opened.
        var fresh = new UnitNames(history, viewer, DefinitionsOf(catalogs), viewOf);
        lock (fresh.gate)
        {
            fresh.Advance(history);
        }

        if (Kept.Count >= 24)
        {
            Kept.Clear();
        }

        Kept[key] = fresh;
        return fresh;
    }

    private static Dictionary<string, UnitDefinition> DefinitionsOf(IReadOnlyList<UnitCatalog> catalogs)
    {
        var all = new Dictionary<string, UnitDefinition>(StringComparer.Ordinal);
        foreach (var definition in catalogs.SelectMany(catalog => catalog.Definitions))
        {
            all.TryAdd(definition.Id, definition);
        }

        return all;
    }

    public Perspective Viewer => viewer;

    /// <summary>The tag this view reads for a unit, such as "G4": its own side's units, or any unit for the adjudicator; null for the other side's.</summary>
    public string? Tag(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        lock (gate)
        {
            return TagOf(id);
        }
    }

    /// <summary>
    /// A unit the caller holds from this view, by name, with the values it has now. A unit the view has never held by name reads
    /// <see cref="Unnamed"/>, so nothing is told by mistake.
    /// </summary>
    public string Of(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        lock (gate)
        {
            return Named(id, through, check: false);
        }
    }

    /// <summary>A unit of this view, by name, with the values it has in <paramref name="unit"/>.</summary>
    public string Of(UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        lock (gate)
        {
            return Name(unit, through);
        }
    }

    /// <summary>
    /// A unit, a Gun, or a weapon in a record of the event at the revision after <paramref name="before"/> (plan section 15.9): its name when the
    /// view could name it just before the event, and <see cref="Unnamed"/> otherwise.
    /// </summary>
    public string Of(string id, long before)
    {
        ArgumentNullException.ThrowIfNull(id);
        lock (gate)
        {
            return Named(id, before, check: true);
        }
    }

    /// <summary>
    /// A text made elsewhere (a planner's reason, a record) with names and words in place of its identifiers, for this view: each unit, Gun, and
    /// weapon id as <see cref="Of(string, long)"/> gives it, each definition id as its counter reads, and each Location in words.
    /// </summary>
    public string InText(string text, long before) => InText(text, before, check: true);

    /// <summary>
    /// A text about units the caller holds from this view at <paramref name="revision"/> (a view's difference, its units table), in the view's
    /// words. A unit the view has never held by name still reads <see cref="Unnamed"/>.
    /// </summary>
    public string Held(string text, long revision) => InText(text, revision, check: false);

    private string InText(string text, long before, bool check)
    {
        ArgumentNullException.ThrowIfNull(text);
        lock (gate)
        {
            if (pattern is null || patternIds != known.Count)
            {
                // Longest first, so "g-squad-10" is never read as "g-squad-1"; an id is taken only when no letter, digit, or hyphen touches it.
                var ids = known.Keys.Concat(definitions.Keys).Where(id => id.Length > 1).Distinct(StringComparer.Ordinal).OrderByDescending(id => id.Length).Select(Regex.Escape);
                pattern = new Regex($"(?<![A-Za-z0-9-])(?:{string.Join('|', ids)})(?![A-Za-z0-9-])", RegexOptions.CultureInvariant);
                patternIds = known.Count;
            }

            var named = known.Count + definitions.Count == 0 ? text : pattern.Replace(text, match =>
            {
                var word = known.ContainsKey(match.Value) ? Named(match.Value, before, check) : Counter(definitions[match.Value]);
                // "its g-mmg-1" reads "its MMG", not "its the MMG".
                var lead = text[Math.Max(0, match.Index - 4)..match.Index];
                return word.StartsWith("the ", StringComparison.Ordinal) && (lead is "its " or "the " or "The " || lead.EndsWith(" a ", StringComparison.Ordinal)) ? word[4..] : word;
            });
            // A concealed firer's weapons are not said: "with its a weapon not in view" is no sentence, and it would count them.
            named = named.Replace($" and its {UnseenWeapon}", string.Empty, StringComparison.Ordinal).Replace($" with its {UnseenWeapon}", string.Empty, StringComparison.Ordinal);

            // A leader a Close Combat attack creates joins it before it has an id of its own (A18.12).
            named = CreatedLeader().Replace(named, "the leader created");

            // The user, 2026-10-21: "a concealed unit, a concealed unit, ..." reads as their count, "9 concealed units"; the pattern runs only on a
            // text that holds such a run, since every text the page says passes here.
            if (named.Contains(UnnamedPair, StringComparison.Ordinal))
            {
                named = UnnamedRun().Replace(named, match => $"{match.Value.Split(", ").Length} concealed units");
            }

            if (named.Contains(UninspectedPair, StringComparison.Ordinal))
            {
                named = UninspectedRun().Replace(named, match => $"{match.Value.Split(", ").Length} uninspected units");
            }
            var boards = history.States.Count > 0 ? history.States[0].Map.Boards.Count : 1;
            named = LocationPattern().Replace(named, match => DisplayText.Place(boards, match.Value));

            // The other side's unit is named by where it stands (the user's ruling of 2026-10-22); a text that says that hex again in the same clause
            // says it once: "4-4-7 squad moves from [B3] to [B4]", "4-4-7 squad, 4-4-7 squad in [B3] fire at [B4]".
            if (named.Contains(" in [", StringComparison.Ordinal))
            {
                named = PlaceSaidTwice().Replace(named, " ${verb} from ${at}");
                named = PlaceSaidAgain().Replace(named, string.Empty);
            }

            // The second play test: a side reads as its counters name it ("the russian side" is "the Russian side").
            if (history.States.Count > 0)
            {
                foreach (var side in history.States[0].Sides.Select(side => side.Id).Where(id => id.Length > 2 && char.IsLower(id[0])))
                {
                    named = Regex.Replace(named, $@"(?<![A-Za-z0-9-]){Regex.Escape(side)}(?![A-Za-z0-9-])", DisplayText.Side(side), RegexOptions.CultureInvariant);
                }
            }

            return named;
        }
    }

    /// <summary>A counter as a player names it, from its definition: "4-6-7 squad", "9-2 leader", "MMG".</summary>
    public static string Counter(UnitDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        int? Value(string name) => definition.Printed("front", name)?.Value?.Number;
        var kind = DisplayText.Kind(definition.Kind);
        return definition.Kind switch
        {
            "asl:leader" when Value("asl:morale") is { } morale && Value("asl:leadership") is { } leadership =>
                $"{morale}{(leadership > 0 ? "+" : "-")}{Math.Abs(leadership)} {(definition.Id.Contains("commissar", StringComparison.Ordinal) ? "Commissar" : "leader")}",
            "asl:mg" => (definition.Printed("front", "asl:size")?.Value?.Text) switch
            {
                "light" => "LMG",
                "medium" => "MMG",
                "heavy" => "HMG",
                _ => "MG",
            },
            "asl:ft" => "FT",
            "asl:dc" => "DC",
            "asl:light-mortar" or "asl:latw" => ScenarioCards.Describe(definition),
            _ when Value("asl:firepower") is { } firepower && Value("asl:range") is { } range && Value("asl:morale") is { } level => $"{firepower}-{range}-{level} {kind}",
            _ => kind,
        };
    }

    [GeneratedRegex(@"\b(?:bd\w+|ab-[a-z0-9-]+):[A-Z]{1,2}\d{1,2}:-?\d\b")]
    private static partial Regex LocationPattern();

    [GeneratedRegex(@"\bcreated-leader:\d+")]
    private static partial Regex CreatedLeader();

    // " in [B3] moves from [B3]": a unit named by its hex, then said to leave that same hex.
    [GeneratedRegex(@" in (?<at>\[[A-Z]{1,2}\d{1,2}\](?:, level -?\d+)?) (?<verb>moves|routs|advances|withdraws) from \k<at>(?!\d|, level)")]
    private static partial Regex PlaceSaidTwice();

    // " in [B3]" that the same clause (up to a ";", ":", or ".") says again: "4-4-7 squad in [B3], 4-4-7 squad in [B3] in [B3] fire at".
    // The same Location only: "[B3]" is not "[B3], level 1", and "level 1" is not "level 10".
    [GeneratedRegex(@" in (?<at>\[[A-Z]{1,2}\d{1,2}\](?:, level -?\d+)?)(?=[^;:.]*? in \k<at>(?!\d|, level))")]
    private static partial Regex PlaceSaidAgain();

    private const string UnnamedPair = Unnamed + ", " + Unnamed;

    // Two or more "a concealed unit" in a row, separated by commas.
    [GeneratedRegex(@"\ba concealed unit(?:, a concealed unit)+\b")]
    private static partial Regex UnnamedRun();

    private const string UninspectedPair = UninspectedUnit + ", " + UninspectedUnit;

    // Two or more "an uninspected unit" in a row, separated by commas.
    [GeneratedRegex(@"\ban uninspected unit(?:, an uninspected unit)+\b")]
    private static partial Regex UninspectedRun();

    private void Advance(GameHistory longer)
    {
        history = longer;
        var count = Math.Min(longer.Events.Count, longer.States.Count);
        for (var revision = through + 1; revision <= count; revision++)
        {
            var item = longer.Events[(int)revision - 1];
            var state = longer.States[(int)revision - 1];
            if (item.Payload is LineageRecorded recorded)
            {
                for (var index = 0; index < recorded.Produced.Count; index++)
                {
                    lineage[recorded.Produced[index].Id] = (recorded.Action, recorded.Consumed, index);
                }
            }

            // Every unit and Gun the state holds that has no tag by entry yet takes one, in the state's own order.
            foreach (var unit in state.Units)
            {
                Enter(unit.Id, unit.Side, unit.Kind, true, revision);
            }

            foreach (var weapon in state.Equipment)
            {
                Enter(weapon.Id, weapon.Side ?? (weapon.Holding is { } holding ? state.Unit(holding.Holder)?.Side : null), weapon.Kind, false, revision);
            }

            var now = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string>? counted = null;
            if (!viewer.IsAdjudicator)
            {
                // The view of this revision alone says which of the other side's units it holds by name; its events are not needed for that.
                var view = viewOf?.Invoke(state) ?? GameView.Of(state, viewer, []);
                foreach (var unit in view.Units.Where(unit => unit.Side != viewer.Name))
                {
                    now.Add(unit.Id);
                    See(unit.Id);
                }

                foreach (var weapon in view.Equipment)
                {
                    now.Add(weapon.Id);
                    if (known.TryGetValue(weapon.Id, out var what) && what.Side != viewer.Name)
                    {
                        See(weapon.Id);
                    }
                }

                counted = UninspectedOf(state, view, now);
            }

            held.Add(now);
            uninspected.Add(counted);
            (through, lastEvent) = (revision, item.EventId);
        }
    }

    /// <summary>
    /// The other side's units a view counts beneath the top of a stack it may not inspect, at one revision (A2.9, A12.16; rulings R23.3, R70.18): under no
    /// "?", not hidden, not a Dummy, not held by name, in a Location where the view seals an uninspected counter of their side. Null when it seals none.
    /// </summary>
    private HashSet<string>? UninspectedOf(GameState state, GameView view, HashSet<string> now)
    {
        if (!view.Sealed.Any(item => item.Uninspected))
        {
            return null;
        }

        var stacks = view.Sealed.Where(item => item.Uninspected).Select(item => (item.Side, item.Location)).ToHashSet();
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in state.Units)
        {
            if (unit.Status == InstanceStatus.Active && unit.Side is { } side && side != viewer.Name && !now.Contains(unit.Id) && unit.Kind != UnitKinds.Dummy
                && GameState.Condition(unit, Conditions.Concealed) != ConditionState.True && GameState.Condition(unit, Conditions.Hidden) != ConditionState.True
                && state.Location(unit.Id) is { } place && stacks.Contains((side, place.Location)))
            {
                found.Add(unit.Id);
            }
        }

        return found;
    }

    // A unit the view may not name at a revision: an uninspected stack's counter, or any other.
    private string UnnamedAt(string id, long revision) =>
        revision >= 1 && revision <= uninspected.Count && uninspected[(int)revision - 1]?.Contains(id) == true ? UninspectedUnit : Unnamed;

    private void Enter(string id, string? side, string kind, bool unit, long revision)
    {
        if (known.ContainsKey(id))
        {
            return;
        }

        known[id] = (side, kind, unit);
        born[id] = revision;
        if (GroupOf(kind, unit) is { } group)
        {
            entered[id] = Inherited(id, entered, _ => true) ?? Next(enteredCount, side, group);
        }
    }

    // The other side's unit or weapon, held by name at least once: it is named, by counter and Location, from then on where it is not checked.
    private void See(string id) => seen.Add(id);

    /// <summary>
    /// The tag a unit takes from the unit it came from, when that unit has one among <paramref name="tags"/> and <paramref name="mayInherit"/>
    /// allows it. A tag is never given twice: two HS of one squad give its tag back; two of different squads make a squad with a new number.
    /// </summary>
    private string? Inherited(string id, Dictionary<string, string> tags, Func<string, bool> mayInherit)
    {
        if (!lineage.TryGetValue(id, out var from) || from.From.Count == 0 || !tags.TryGetValue(from.From[0], out var parent) || !mayInherit(from.From[0]))
        {
            return null;
        }

        static string Stem(string tag) => tag.Length > 2 && tag[^1] is 'a' or 'b' && char.IsDigit(tag[^2]) ? tag[..^1] : tag;
        return from.Action switch
        {
            LineageAction.Deployed => parent + (from.Index == 0 ? "a" : "b"),
            LineageAction.Recombined => from.From.Count > 1 && tags.TryGetValue(from.From[1], out var other) && mayInherit(from.From[1])
                && Stem(parent) == Stem(other) && Stem(parent) != parent ? Stem(parent) : null,
            _ => parent,
        };
    }

    private static string Next(Dictionary<(string, string), int> counts, string? side, string group)
    {
        var key = (side ?? string.Empty, group);
        var number = counts[key] = counts.GetValueOrDefault(key) + 1;
        return string.Create(CultureInfo.InvariantCulture, $"{Letter(side)}{number}");
    }

    private static string Letter(string? side) => string.IsNullOrEmpty(side) ? "N" : char.ToUpperInvariant(side[0]).ToString();

    /// <summary>
    /// The group a kind is numbered in, so a tag is not given twice: squads, half-squads, and crews together, since one becomes another; leaders
    /// and heroes together; vehicles; Guns. A SW has no tag (the user's answer 3): it reads with its holder or its Location.
    /// </summary>
    private static string? GroupOf(string kind, bool unit) => kind switch
    {
        "asl:squad" or "asl:half-squad" or "asl:crew" or "asl:vehicle-crew" => "mmc",
        "asl:leader" or "asl:hero" => "smc",
        "asl:gun" => "gun",
        _ => unit ? kind : null,
    };

    // A tag is read only for the view's own units, or by the adjudicator (the user's ruling of 2026-10-22).
    private string? TagOf(string id) => known.TryGetValue(id, out var what) && Mine(what.Side) ? entered.GetValueOrDefault(id) : null;

    private bool Mine(string? side) => viewer.IsAdjudicator || side == viewer.Name;

    /// <summary>Where a unit or weapon stands at a revision, in words ("[W4]", "[F5], level 1"); null off board, aboard, or not in that state.</summary>
    private string? PlaceAt(string id, long revision)
    {
        if (revision < 1 || revision > history.States.Count)
        {
            return null;
        }

        var state = history.States[(int)revision - 1];
        return state.Location(id)?.Location is { } at ? DisplayText.Place(state, at) : null;
    }

    private string Named(string id, long before, bool check)
    {
        if (!known.TryGetValue(id, out var what))
        {
            return id;
        }

        // A unit the event itself made is read in the state that first holds it.
        var revision = Math.Clamp(Math.Max(before, born[id]), 1, Math.Max(through, 1));
        var mine = Mine(what.Side);
        var inView = mine || (check ? revision <= held.Count && held[(int)revision - 1].Contains(id) : seen.Contains(id));
        if (what.Unit)
        {
            return inView && Instance(id, revision) is { } found ? Name(found.Unit, found.Revision) : UnnamedAt(id, revision);
        }

        if (!inView)
        {
            return UnseenWeapon;
        }

        var weapon = history.States[(int)revision - 1].Equipment.FirstOrDefault(item => item.Id == id);
        var word = weapon?.Definition is { } reference && definitions.TryGetValue(reference.Definition, out var definition) ? Counter(definition)
            : what.Kind == "asl:gun" ? "Gun" : DisplayText.Kind(what.Kind).ToUpperInvariant();

        // The other side's Gun is told by its Location, as its units are; a SW reads with its holder or its Location in the text around it.
        return TagOf(id) is { } tag ? $"{word} {tag}"
            : what.Kind == "asl:gun" && PlaceAt(id, revision) is { } gunAt ? $"the {word} in {gunAt}"
            : $"the {word}";
    }

    // A unit the state at the revision no longer lists is read as it was when it entered, with the revision it is read at.
    private (UnitInstance Unit, long Revision)? Instance(string id, long revision)
    {
        if (revision >= 1 && revision <= history.States.Count && history.States[(int)revision - 1].Units.FirstOrDefault(unit => unit.Id == id) is { } now)
        {
            return (now, revision);
        }

        return born.TryGetValue(id, out var first) && first != revision && first >= 1 && first <= history.States.Count
            && history.States[(int)first - 1].Units.FirstOrDefault(unit => unit.Id == id) is { } then ? (then, first) : null;
    }

    private string Name(UnitInstance unit, long revision)
    {
        var counter = unit.Kind == UnitKinds.Dummy ? "Dummy"
            : unit.Definition is { } reference && definitions.TryGetValue(reference.Definition, out var definition) ? Counter(definition)
            : DisplayText.Kind(unit.Kind);
        if (TagOf(unit.Id) is { } tag)
        {
            return $"{counter} {tag}";
        }

        // The user's ruling of 2026-10-22: the other side's unit by its counter and where it stood.
        if (!Mine(unit.Side) && seen.Contains(unit.Id))
        {
            return PlaceAt(unit.Id, revision) is { } at ? $"{counter} in {at}" : counter;
        }

        return UnnamedAt(unit.Id, revision);
    }
}
