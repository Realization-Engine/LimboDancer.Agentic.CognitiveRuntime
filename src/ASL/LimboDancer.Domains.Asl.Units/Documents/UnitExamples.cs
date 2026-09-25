using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Documents;

/// <summary>
/// The synthetic examples in <c>src/ASL/units/examples</c>: the Unit Lab's starting documents and the built-in placement
/// sets. Their values are illustrative, not catalog entries, and they are never game state.
/// </summary>
public static class UnitExamples
{
    private const string Prefix = "Examples.";
    private const string CatalogName = "catalog.units.json";

    /// <summary>The example documents, one per kind.</summary>
    public static IReadOnlyList<UnitDocument> Catalog(UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        return UnitDocumentReader.Read(Read(Prefix + CatalogName), vocabulary).Documents;
    }

    /// <summary>Every example placement set, by resource name.</summary>
    public static IReadOnlyList<UnitPlacementSetResult> PlacementSets(UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        return [.. typeof(UnitExamples).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal) && name.EndsWith(".units.json", StringComparison.Ordinal) && name != Prefix + CatalogName)
            .Order(StringComparer.Ordinal)
            .Select(name => UnitPlacementSetReader.Read(Read(name), vocabulary))];
    }

    private static byte[] Read(string name)
    {
        using var stream = typeof(UnitExamples).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"The unit example {name} is not embedded.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
