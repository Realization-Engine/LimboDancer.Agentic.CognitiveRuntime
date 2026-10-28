using System.Text.Json;
using System.Text.Json.Nodes;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>What a check expects: the contract's outcome, and texts its reasons must contain, if any.</summary>
public sealed record MinigameExpectation(string Outcome, IReadOnlyList<string>? Reasons = null);

/// <summary>
/// A proof of a minigame (the Rules-first plan, section 7.1): a position (a setup, or a revision of the bundle's game), the viewer, the contract, the facts
/// entered by hand, and the verdict expected. In the proving slice every check's facts are entered by hand (ruling RPS.12).
/// </summary>
public sealed record MinigameCheck(string Name, string Position, string Viewer, string Contract, JsonObject Facts, MinigameExpectation Expect);

/// <summary>The rulebook or Training Manual example a minigame reproduces, with its physical page.</summary>
public sealed record MinigameExample(string Source, int Page);

/// <summary>
/// A minigame's manifest, <c>src/ASL/units/minigames/&lt;id&gt;/&lt;id&gt;.minigame.json</c> (ruling RPS.8): the bundle id, its card, the rules it proves, the
/// contracts it exercises, the example it reproduces, and its checks. The manifest is the one source of the link between a rule and its minigames.
/// </summary>
public sealed record MinigameManifest(string Id, string Title, string Card, IReadOnlyList<string> Rules, IReadOnlyList<string> Contracts,
    MinigameExample? Example, IReadOnlyList<MinigameCheck> Checks);

/// <summary>
/// The minigame registry (the plan's section 7.1; ruling RPS.9): the bundles embedded from <c>src/ASL/units/minigames</c>, read once, and the questions
/// the lists, the store, and the workbench ask of them.
/// </summary>
public static class Minigames
{
    private const string Prefix = "Minigames.";
    private const string Suffix = ".minigame.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    private static readonly Lazy<IReadOnlyList<MinigameManifest>> Bundles = new(() => Load());

    /// <summary>Every bundle, by id.</summary>
    public static IReadOnlyList<MinigameManifest> All => Bundles.Value;

    /// <summary>The bundle with an id, or null.</summary>
    public static MinigameManifest? Find(string id) => All.FirstOrDefault(bundle => bundle.Id == id);

    /// <summary>The bundle whose card has this id, or null: how a game started from a card takes its minigame (ruling RPS.6).</summary>
    public static string? BundleOfCard(string? cardId) => cardId is null ? null : All.FirstOrDefault(bundle => bundle.Card == cardId)?.Id;

    /// <summary>Whether a card is a minigame's, for the lists that leave minigames out (ruling RPS.10).</summary>
    public static bool IsMinigameCard(string cardId) => BundleOfCard(cardId) is not null;

    /// <summary>The bundles that exercise a contract.</summary>
    public static IReadOnlyList<MinigameManifest> ForContract(string contractId) => [.. All.Where(bundle => bundle.Contracts.Contains(contractId))];

    /// <summary>The bundles that prove a rule.</summary>
    public static IReadOnlyList<MinigameManifest> ForRule(string rule) => [.. All.Where(bundle => bundle.Rules.Contains(rule))];

    /// <summary>A manifest's text read as a manifest; throws on a text that is not one.</summary>
    public static MinigameManifest Parse(string text) =>
        JsonSerializer.Deserialize<MinigameManifest>(text, Options) ?? throw new JsonException("A minigame manifest may not be null.");

    private static List<MinigameManifest> Load()
    {
        var assembly = typeof(Minigames).Assembly;
        var bundles = new List<MinigameManifest>();
        foreach (var name in assembly.GetManifestResourceNames().Where(name => name.StartsWith(Prefix, StringComparison.Ordinal)
            && name.EndsWith(Suffix, StringComparison.Ordinal)).Order(StringComparer.Ordinal))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            bundles.Add(Parse(reader.ReadToEnd()));
        }

        return bundles;
    }
}
