using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Catalog;

/// <summary>
/// The catalogs in <c>src/ASL/units/catalog</c>, by name: <c>scenario-a1.synthetic</c>, whose values are illustrative
/// and never counter data, and <c>scenario-a1</c>, built from the reviewed counter transcription and published.
/// </summary>
public static class UnitCatalogs
{
    public const string ScenarioA1 = "scenario-a1";
    public const string ScenarioA1Synthetic = "scenario-a1.synthetic";

    private const string Prefix = "Catalogs.";
    private const string Suffix = ".catalog.json";

    /// <summary>The embedded catalog names, in ordinal order.</summary>
    public static IReadOnlyList<string> Names => [.. typeof(UnitCatalogs).Assembly.GetManifestResourceNames()
        .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal) && name.EndsWith(Suffix, StringComparison.Ordinal))
        .Select(name => name[Prefix.Length..^Suffix.Length])
        .Order(StringComparer.Ordinal)];

    /// <summary>
    /// The catalog a game or a card reads, from the name it records (<c>asl-scenario-a1@1.9.0</c>): the catalog of that name, whatever version is
    /// loaded (the user, 2026-10-05). The version in a record says which version the game was set up under and locks nothing: a catalog's
    /// definitions are added to and corrected, never removed, so a game set up under an earlier version reads the current one. Null when no
    /// catalog of the name is loaded.
    /// </summary>
    public static UnitCatalog? For(IEnumerable<UnitCatalog> catalogs, string? recorded)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        if (recorded is null)
        {
            return null;
        }

        var at = recorded.IndexOf('@', StringComparison.Ordinal);
        var name = at < 0 ? recorded : recorded[..at];
        return catalogs.FirstOrDefault(catalog => catalog.Identity.Catalog == name);
    }

    /// <summary>Reads an embedded catalog; null when no catalog of that name is embedded.</summary>
    public static UnitCatalogResult? Read(string name, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(vocabulary);
        using var stream = typeof(UnitCatalogs).Assembly.GetManifestResourceStream(Prefix + name + Suffix);
        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return UnitCatalogReader.Read(buffer.ToArray(), vocabulary);
    }
}
