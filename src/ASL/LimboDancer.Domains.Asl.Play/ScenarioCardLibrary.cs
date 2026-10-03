using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Units.Catalog;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The scenario cards a game may start from (pass 22 of the Scenario Card Games Plan, rulings R22.1 and R22.2): the built-in cards embedded in the
/// assembly, and the user's cards saved in a directory beside the saved maps (<c>src/ASL/boards/cards</c> in the Studio). A user card never takes a
/// built-in card's name, so a name means one card; its text is read afresh when the file changes.
/// </summary>
public sealed partial class ScenarioCardLibrary(string? directory)
{
    private const string Suffix = ".scenario-card.json";

    private static readonly JsonSerializerOptions Writing = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private readonly Dictionary<string, (DateTime Written, long Length, string Text, string Sha256)> read = new(StringComparer.Ordinal);
    private readonly Lock gate = new();

    /// <summary>The built-in cards only (every game and test that names no directory).</summary>
    public static ScenarioCardLibrary Embedded { get; } = new(null);

    /// <summary>Where the user's cards are saved; null when the library holds only the built-in cards.</summary>
    public string? Directory { get; } = directory;

    /// <summary>The built-in cards' names and then the user's, each in ordinal order.</summary>
    public IReadOnlyList<string> Names => [.. ScenarioCards.Names, .. UserNames];

    /// <summary>The user's cards' names, in ordinal order.</summary>
    public IReadOnlyList<string> UserNames => Directory is { } root && System.IO.Directory.Exists(root)
        ? [.. System.IO.Directory.GetFiles(root, "*" + Suffix).Select(path => Path.GetFileName(path)[..^Suffix.Length])
            .Where(name => NamePattern().IsMatch(name) && !ScenarioCards.Names.Contains(name, StringComparer.Ordinal)).Order(StringComparer.Ordinal)]
        : [];

    /// <summary>Whether a name is one of the user's cards.</summary>
    public bool IsUser(string name) => UserNames.Contains(name, StringComparer.Ordinal);

    /// <summary>The file a user card is kept in; null for a built-in card or a name with no card.</summary>
    public string? UserFile(string name) => IsUser(name) && Directory is { } root ? Path.Combine(root, name + Suffix) : null;

    /// <summary>A card's text with LF line endings, built-in or the user's; null when none has that name.</summary>
    public string? Text(string name) => Current(name)?.Text;

    /// <summary>The SHA-256 of a card's text with LF line endings (ruling R18.2); null when none has that name.</summary>
    public string? Sha256(string name) => Current(name)?.Sha256;

    /// <summary>
    /// A card's text and its SHA-256, read together (referee, pass 22): a built-in card's once, a user card's again only when its file changes (its
    /// write time or length); null when none has that name.
    /// </summary>
    public (string Text, string Sha256)? Current(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (ScenarioCards.EmbeddedText(name) is { } embedded)
        {
            return (embedded, ScenarioCards.Sha256(name)!);
        }

        if (Directory is not { } root || !NamePattern().IsMatch(name))
        {
            return null;
        }

        var file = new FileInfo(Path.Combine(root, name + Suffix));
        if (!file.Exists)
        {
            return null;
        }

        lock (gate)
        {
            if (read.TryGetValue(name, out var cached) && cached.Written == file.LastWriteTimeUtc && cached.Length == file.Length)
            {
                return (cached.Text, cached.Sha256);
            }
        }

        var text = File.ReadAllText(file.FullName).ReplaceLineEndings("\n");
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        lock (gate)
        {
            read[name] = (file.LastWriteTimeUtc, file.Length, text, sha256);
        }

        return (text, sha256);
    }

    /// <summary>
    /// The text of a card's setups file (pass 30), with LF line endings: embedded for a built-in card, <c>&lt;name&gt;.setups.json</c> beside a user's card;
    /// null when the card has none. It is no part of the card's own text, so it never changes the card's SHA-256.
    /// </summary>
    public string? PlansText(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (ScenarioCards.Names.Contains(name, StringComparer.Ordinal))
        {
            return ScenarioSetupPlans.EmbeddedText(name);
        }

        if (!IsUser(name) || Directory is not { } root || new FileInfo(Path.Combine(root, name + ScenarioSetupPlans.Suffix)) is not { Exists: true } file)
        {
            return null;
        }

        try
        {
            return File.ReadAllText(file.FullName).ReplaceLineEndings("\n");
        }
        catch (IOException)
        {
            // The file is being written or is locked: the card offers no plan this time, and setup by hand goes on.
            return null;
        }
    }

    /// <summary>A card's setup plans (pass 30), read and checked for form; null when the card has no setups file or cannot itself be read.</summary>
    public SetupPlansRead? Plans(string name, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return PlansText(name) is { } text && Read(name, catalog)?.Card is { } card ? ScenarioSetupPlans.Parse(card, text, catalog) : null;
    }

    /// <summary>Whether a game that recorded this SHA-256 still plays the card of that name (rulings R22.2, R28.4).</summary>
    public bool Matches(string name, string sha256) => ScenarioCards.SameCard(name, sha256, Sha256(name));

    /// <summary>Reads and validates a card; null when none has that name.</summary>
    public ScenarioCardRead? Read(string name, UnitCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return Text(name) is { } text ? ScenarioCards.Parse(name, text, catalog) : null;
    }

    /// <summary>
    /// Saves a user's card (ruling R22.2): valid against the catalog, named by its id (lower-case letters, digits, and hyphens), never a built-in card's
    /// name, and replacing a user card of that id only when <paramref name="replace"/> says so (referee, pass 22). Returns why it is refused, empty when
    /// it was saved.
    /// </summary>
    public IReadOnlyList<string> Save(ScenarioCard card, UnitCatalog catalog, bool replace = false)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(catalog);
        if (Directory is not { } root)
        {
            return ["card.save: this library keeps no user cards"];
        }

        if (!NamePattern().IsMatch(card.Id) || ScenarioCards.Names.Contains(card.Id, StringComparer.Ordinal))
        {
            return [$"card.id: a user card's id is 1 to 64 lower-case letters, digits, and hyphens, and not a built-in card's ('{card.Id}') (ruling R22.2)"];
        }

        if (!replace && IsUser(card.Id))
        {
            return [$"card.id: you already have a card '{card.Id}'; choose another id, or edit that card (ruling R22.2)"];
        }

        var text = Serialize(card);
        if (ScenarioCards.Parse(card.Id, text, catalog) is { IsValid: false } refused)
        {
            return refused.Diagnostics;
        }

        // Referee, pass 22: the file is written whole, then moved into place, so no reader sees half of it.
        System.IO.Directory.CreateDirectory(root);
        var path = Path.Combine(root, card.Id + Suffix);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, text);
        File.Move(temporary, path, overwrite: true);
        return [];
    }

    /// <summary>Deletes a user's card; false when there is none by that name.</summary>
    public bool Delete(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Directory is not { } root || !IsUser(name))
        {
            return false;
        }

        File.Delete(Path.Combine(root, name + Suffix));
        return true;
    }

    /// <summary>A card's JSON as the library writes it: camel case, indented, LF line endings, with a final newline.</summary>
    public static string Serialize(ScenarioCard card) => JsonSerializer.Serialize(card, Writing).ReplaceLineEndings("\n") + "\n";

    /// <summary>Whether a name may be a user card's: lower-case letters, digits, and hyphens, never a built-in card's (ruling R22.2).</summary>
    public static bool ValidName(string name) => NamePattern().IsMatch(name) && !ScenarioCards.Names.Contains(name, StringComparer.Ordinal);

    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]{0,63}\z")]
    private static partial Regex NamePattern();
}
