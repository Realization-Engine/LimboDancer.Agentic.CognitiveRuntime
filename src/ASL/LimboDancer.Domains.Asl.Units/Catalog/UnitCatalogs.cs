using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Catalog;

/// <summary>
/// The catalogs in <c>src/ASL/units/catalog</c>, by name: <c>scenario-a1.synthetic</c>, whose values are illustrative
/// and never counter data, and <c>scenario-a1</c> once its transcription is reviewed and published.
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
