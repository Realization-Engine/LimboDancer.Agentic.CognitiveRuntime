using System.Text;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>Shared inputs: the built-in vocabulary, the repository's unit folder, and a small extension pack.</summary>
internal static class UnitsTestData
{
    public static readonly Lazy<UnitVocabulary> Asl = new(UnitVocabulary.Asl);

    /// <summary>The design's Squad Leader Apocalypse example (section 5), as a test pack.</summary>
    public const string SlaPack = """
        {
          "pack": "sla", "version": "0.1.0", "extends": ["asl@1.0.0"],
          "kinds": [
            { "name": "sla:scavenger-band", "extends": "asl:mmc", "label": "Scavenger band", "faces": ["front", "broken"] },
            { "name": "sla:drone", "extends": "asl:equipment", "label": "Recon drone", "faces": ["front", "downed"], "attributes": ["sla:endurance"] },
            { "name": "sla:mutant", "label": "Mutant", "attributes": ["sla:endurance"] }
          ],
          "faces": [ { "name": "downed", "label": "downed" } ],
          "attributes": [
            { "name": "sla:supply", "type": "integer", "label": "Supply", "scope": "unit" },
            { "name": "sla:endurance", "type": "integer", "label": "Endurance", "scope": "face" }
          ],
          "traits": [ { "name": "sla:scrounger", "label": "Scrounger" } ],
          "states": [ { "name": "sla:irradiated", "label": "Irradiated" } ],
          "augments": [ { "kind": "asl:mmc", "attributes": ["sla:supply"], "traits": ["sla:scrounger"] } ]
        }
        """;

    public static UnitVocabulary WithSla()
    {
        var pack = VocabularyPackReader.Read(Encoding.UTF8.GetBytes(SlaPack));
        Assert.NotNull(pack.Pack);
        var result = UnitVocabulary.Create([VocabularyPackReader.Asl(), pack.Pack]);
        Assert.Empty(result.Diagnostics);
        return result.Vocabulary!;
    }

    public static UnitDocument One(string json, UnitVocabulary? vocabulary = null)
    {
        var result = UnitDocumentReader.Read(json, vocabulary ?? Asl.Value);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == Units.UnitDiagnosticSeverity.Error);
        return Assert.Single(result.Documents);
    }

    public static string UnitsDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "ASL", "LimboDancer.Domains.Asl.sln")))
            {
                return Path.Combine(directory.FullName, "src", "ASL", "units");
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from " + AppContext.BaseDirectory);
    }
}
