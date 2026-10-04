using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The names a view reads for a game's units (pass 31c, design D11): a unit's printed values, its kind, and a tag that never changes, such as
/// "4-6-7 squad G4" and "9-2 leader G2". A side's own units are numbered in the order they entered the game; the other side's units are numbered
/// in the order this view could first name them, so a tag counts only what the view has seen and never what the other side has. A unit that takes
/// another's place keeps its tag; a Deployed HS adds "a" or "b". A unit the view does not hold reads "a concealed unit". Play, Replay, and the
/// records read the one instance kept for a game and a view; it grows with the game and what it has given is never changed.
/// </summary>
public sealed partial class UnitNames
{
    /// <summary>What a view reads for a unit it may not name (rulings R23.1 and R19.8).</summary>
    public const string Unnamed = "a concealed unit";

    /// <summary>What a view reads for a weapon it does not hold.</summary>
    public const string UnseenWeapon = "a weapon not in view";

    private static readonly ConcurrentDictionary<(string First, string Viewer), UnitNames> Kept = new();

    private readonly Perspective viewer;
    private readonly IReadOnlyDictionary<string, UnitDefinition> definitions;
    private readonly Func<GameState, IReadOnlySet<string>?>? outOfSight;
    private readonly Lock gate = new();

    // What every view may be told of an id: its side, its kind, the revision that made it, and the units it came from.
    private readonly Dictionary<string, (string? Side, string Kind, bool Unit)> known = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> born = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (LineageAction Action, IReadOnlyList<string> From, int Index)> lineage = new(StringComparer.Ordinal);

    // The tags by entry, which a unit's own side and the adjudicator read, and the tags this view gave the other side's units.
    private readonly Dictionary<string, string> entered = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> seen = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Side, string Group), int> enteredCount = [];
    private readonly Dictionary<(string Side, string Group), int> seenCount = [];

    // The other side's units and weapons this view held at each revision.
    private readonly List<HashSet<string>> held = [];
    private GameHistory history;
    private long through;
    private string lastEvent = string.Empty;
    private Regex? pattern;
    private int patternIds;

    private UnitNames(GameHistory history, Perspective viewer, IReadOnlyDictionary<string, UnitDefinition> definitions, Func<GameState, IReadOnlySet<string>?>? outOfSight) =>
        (this.history, this.viewer, this.definitions, this.outOfSight) = (history, viewer, definitions, outOfSight);

    /// <summary>
    /// The names of <paramref name="history"/> for <paramref name="viewer"/>. <paramref name="outOfSight"/> gives the OB groups a side may not see
    /// while they set up (ruling R23.3). The same game read again, longer, keeps every tag it has given.
    /// </summary>
    public static UnitNames For(GameHistory history, Perspective viewer, IReadOnlyList<UnitCatalog> catalogs, Func<GameState, IReadOnlySet<string>?>? outOfSight = null)
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
        var fresh = new UnitNames(history, viewer, DefinitionsOf(catalogs), outOfSight);
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

    /// <summary>The tag this view reads for a unit, such as "G4"; null for a unit it has never held by name.</summary>
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
            return Name(unit);
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
            var boards = history.States.Count > 0 ? history.States[0].Map.Boards.Count : 1;
            return LocationPattern().Replace(named, match => DisplayText.Place(boards, match.Value));
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

    [GeneratedRegex(@"\bbd\w+:[A-Z]{1,2}\d{1,2}:-?\d\b")]
    private static partial Regex LocationPattern();

    [GeneratedRegex(@"\bcreated-leader:\d+")]
    private static partial Regex CreatedLeader();

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
            if (!viewer.IsAdjudicator)
            {
                // The view of this revision alone says which of the other side's units it holds by name; its events are not needed for that.
                var view = GameView.Of(state, viewer, [], outOfSight?.Invoke(state));
                foreach (var unit in view.Units.Where(unit => unit.Side != viewer.Name))
                {
                    now.Add(unit.Id);
                    See(unit.Id, revision);
                }

                foreach (var weapon in view.Equipment)
                {
                    now.Add(weapon.Id);
                    if (known.TryGetValue(weapon.Id, out var what) && what.Side != viewer.Name)
                    {
                        See(weapon.Id, revision);
                    }
                }
            }

            held.Add(now);
            (through, lastEvent) = (revision, item.EventId);
        }
    }

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

    private void See(string id, long revision)
    {
        if (!seen.ContainsKey(id) && known.TryGetValue(id, out var what) && GroupOf(what.Kind, what.Unit) is { } group)
        {
            // The referee, pass 31c: the other side's unit takes its parent's tag only when this view held the parent by name just before the
            // unit was made, so a tag never tells of a Deployment, a Recombination, or a Replacement that happened under "?".
            var before = born.TryGetValue(id, out var made) ? made - 1 : revision - 1;
            bool Held(string parent) => before >= 1 && before <= held.Count && held[(int)before - 1].Contains(parent);
            seen[id] = Inherited(id, seen, Held) ?? Next(seenCount, what.Side, group);
        }
    }

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
        "asl:squad" or "asl:half-squad" or "asl:crew" => "mmc",
        "asl:leader" or "asl:hero" => "smc",
        "asl:gun" => "gun",
        _ => unit ? kind : null,
    };

    private string? TagOf(string id) => known.TryGetValue(id, out var what) && (viewer.IsAdjudicator || what.Side == viewer.Name) ? entered.GetValueOrDefault(id) : seen.GetValueOrDefault(id);

    private string Named(string id, long before, bool check)
    {
        if (!known.TryGetValue(id, out var what))
        {
            return id;
        }

        // A unit the event itself made is read in the state that first holds it.
        var revision = Math.Clamp(Math.Max(before, born[id]), 1, Math.Max(through, 1));
        var mine = viewer.IsAdjudicator || what.Side == viewer.Name;
        var inView = mine || !check || (revision <= held.Count && held[(int)revision - 1].Contains(id));
        if (what.Unit)
        {
            return inView && Instance(id, revision) is { } unit ? Name(unit) : Unnamed;
        }

        if (!inView)
        {
            return UnseenWeapon;
        }

        var weapon = history.States[(int)revision - 1].Equipment.FirstOrDefault(item => item.Id == id);
        var word = weapon?.Definition is { } reference && definitions.TryGetValue(reference.Definition, out var definition) ? Counter(definition)
            : what.Kind == "asl:gun" ? "Gun" : DisplayText.Kind(what.Kind).ToUpperInvariant();
        return TagOf(id) is { } tag ? $"{word} {tag}" : $"the {word}";
    }

    // A unit the state at the revision no longer lists is read as it was when it entered.
    private UnitInstance? Instance(string id, long revision) =>
        (revision >= 1 && revision <= history.States.Count ? history.States[(int)revision - 1].Units.FirstOrDefault(unit => unit.Id == id) : null)
            ?? (born.TryGetValue(id, out var first) && first != revision && first <= history.States.Count ? history.States[(int)first - 1].Units.FirstOrDefault(unit => unit.Id == id) : null);

    private string Name(UnitInstance unit)
    {
        if (TagOf(unit.Id) is not { } tag)
        {
            return Unnamed;
        }

        var counter = unit.Kind == UnitKinds.Dummy ? "Dummy"
            : unit.Definition is { } reference && definitions.TryGetValue(reference.Definition, out var definition) ? Counter(definition)
            : DisplayText.Kind(unit.Kind);
        return $"{counter} {tag}";
    }
}
