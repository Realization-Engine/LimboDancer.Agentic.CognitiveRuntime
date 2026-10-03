using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// A counter a setup plan places: its id; its catalog definition, or a Dummy; its OB group; and where it goes: a Location, a holder (a SW's possessor, a
/// Gun's crew or towing vehicle, a Passenger's vehicle), or off board with its entry area. A Gun or a vehicle on the map has a facing; a unit may be
/// under "?" or hidden, and a Gun may name its Bore Sighted Location.
/// </summary>
public sealed record SetupPlanPlacement(string Id, string? Definition = null, string? Group = null, string? At = null, string? Holder = null, string? Facing = null,
    bool Dummy = false, bool Concealed = false, bool Hidden = false, bool OffBoard = false, string? Entry = null, string? BoreSighted = null);

/// <summary>
/// A prepared setup for the side that sets up first (pass 30 of the Card Play and Map Studio Redesign Plan): its name, its idea, what it gives up, the
/// terrain facts it rests on, and its placements; and the SHA-256 of the card text it was made for. It places the card's fixed OB and never changes it.
/// </summary>
public sealed record SetupPlan(string Id, string CardSha256, string Side, string Name, string Idea, string GivesUp, IReadOnlyList<string> Terrain,
    IReadOnlyList<SetupPlanPlacement> Placements);

/// <summary>A card's setup plans as their file holds them: the format, the card's id, and the plans.</summary>
public sealed record SetupPlanFile(string Format, string Card, IReadOnlyList<SetupPlan> Plans);

/// <summary>A card's setup plans and why their file is refused, if it is; a refused file gives no plans.</summary>
public sealed record SetupPlansRead(IReadOnlyList<SetupPlan> Plans, IReadOnlyList<string> Diagnostics);

/// <summary>
/// The setup plans of the scenario cards (pass 30). They are kept in a file beside the card, <c>&lt;card id&gt;.setups.json</c>, embedded for a built-in
/// card, so adding plans never changes a card's text, its SHA-256, or the games started from it. Reading checks only the file's form; whether a plan's
/// placements are a legal setup is the gate's question, asked when the plan is proposed like any setup.
/// </summary>
public static partial class ScenarioSetupPlans
{
    public const string Format = "asl-setup-plans/1";

    public const string Suffix = ".setups.json";

    /// <summary>The most plans a card offers.</summary>
    public const int MostPlans = 3;

    private const string Prefix = "Scenarios.";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string?> Embedded = new(StringComparer.Ordinal);

    /// <summary>A built-in card's embedded setups file, with LF line endings; null when the card has none.</summary>
    public static string? EmbeddedText(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Embedded.GetOrAdd(name, key =>
        {
            using var stream = typeof(ScenarioSetupPlans).Assembly.GetManifestResourceStream(Prefix + key + Suffix);
            if (stream is null)
            {
                return null;
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().ReplaceLineEndings("\n");
        });
    }

    /// <summary>Reads a card's setups file and checks its form against the card and the catalog.</summary>
    public static SetupPlansRead Parse(ScenarioCard card, string json, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(catalog);
        SetupPlanFile? file;
        try
        {
            file = JsonSerializer.Deserialize<SetupPlanFile>(json, Options);
        }
        catch (JsonException error)
        {
            return new SetupPlansRead([], [$"setups.json: {error.Message}"]);
        }

        if (file is null)
        {
            return new SetupPlansRead([], ["setups.json: the file is empty"]);
        }

        var found = Validate(card, file, catalog);
        return found.Count > 0 ? new SetupPlansRead([], found) : new SetupPlansRead(file.Plans, []);
    }

    /// <summary>Why a setups file is refused; empty when its form is valid.</summary>
    public static IReadOnlyList<string> Validate(ScenarioCard card, SetupPlanFile file, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(catalog);
        var found = new List<string>();
        void Check(bool valid, string diagnostic)
        {
            if (!valid)
            {
                found.Add(diagnostic);
            }
        }

        Check(file.Format == Format, $"setups.format: a setups file is '{Format}', not '{file.Format}'");
        Check(file.Card == card.Id, $"setups.card: the file is for the card '{file.Card}', not '{card.Id}'");
        var plans = file.Plans ?? [];
        Check(plans.Count <= MostPlans, $"setups.plans: a card offers at most {MostPlans} plans, and the file has {plans.Count}");
        Check(plans.Select(plan => plan.Id).Distinct(StringComparer.Ordinal).Count() == plans.Count, "setups.plans: two plans share an id");
        var side = card.Sides.FirstOrDefault(item => item.Side == card.Turns.SetsUpFirst);
        var groups = side is null ? [] : side.Groups.Select((_, index) => ScenarioCards.GroupId(side.Side, index)).ToArray();
        foreach (var plan in plans)
        {
            var name = string.IsNullOrWhiteSpace(plan.Id) ? "a plan" : $"'{plan.Id}'";
            Check(plan.Id is not null && IdPattern().IsMatch(plan.Id), $"setups.plan: {name} has an id of lower-case letters, digits, and hyphens");
            Check(plan.Side == card.Turns.SetsUpFirst, $"setups.plan: {name} is for {plan.Side}, and {card.Turns.SetsUpFirst} sets up first");
            Check(!string.IsNullOrWhiteSpace(plan.Name) && !string.IsNullOrWhiteSpace(plan.Idea) && !string.IsNullOrWhiteSpace(plan.GivesUp),
                $"setups.plan: {name} has a name, its idea, and what it gives up");
            Check(plan.CardSha256 is not null && ShaPattern().IsMatch(plan.CardSha256), $"setups.plan: {name} names the SHA-256 of the card text it was made for");
            Check(plan.Terrain is not null, $"setups.plan: {name} lists its terrain facts, or none");
            var placements = plan.Placements ?? [];
            Check(placements.Count > 0, $"setups.plan: {name} places at least one counter");
            Check(placements.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == placements.Count, $"setups.plan: {name} gives two counters one id");
            foreach (var item in placements)
            {
                var counter = $"{name}, counter '{item.Id}'";
                Check(!string.IsNullOrWhiteSpace(item.Id), $"setups.placement: {name} has a counter with no id");
                Check(item.Dummy ? item.Definition is null : item.Definition is { } definition && catalog.Definition(definition)?.Nationality == plan.Side,
                    $"setups.placement: {counter} is a Dummy, or a {plan.Side} definition of the catalog");
                Check(item.Group is { } group && groups.Contains(group, StringComparer.Ordinal), $"setups.placement: {counter} names an OB group of {plan.Side}");
                Check(new[] { item.At is not null && item.Holder is null, item.Holder is not null && item.At is null, item.OffBoard }.Count(way => way) == 1
                    || (item is { At: not null, Holder: not null, OffBoard: false } && catalog.Definition(item.Definition ?? string.Empty)?.Kind == "asl:gun"),
                    $"setups.placement: {counter} has a Location, a holder, or waits off board, one of the three; only a manned Gun has a Location and a holder");
                Check(item.At is null || (BoardLocation.TryParse(item.At, out var at) && card.Boards.Any(board => board.Board == at.Board.Value)),
                    $"setups.placement: {counter} is at '{item.At}', not a Location of the card's boards");
                Check(item.Holder is null || placements.Any(other => other.Id == item.Holder && other.Id != item.Id), $"setups.placement: {counter} is held by '{item.Holder}', not a counter of the plan");
                Check(item.Entry is null || item.OffBoard, $"setups.placement: {counter} names an entry area and does not wait off board");
            }
        }

        return found;
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]{0,63}\z")]
    private static partial Regex IdPattern();

    [GeneratedRegex(@"^[0-9a-f]{64}\z")]
    private static partial Regex ShaPattern();
}
