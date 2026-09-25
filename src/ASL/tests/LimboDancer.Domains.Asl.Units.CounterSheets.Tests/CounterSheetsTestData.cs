using System.Security.Cryptography;
using System.Text;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.CounterSheets.Tests;

/// <summary>
/// The committed catalog manifest, worksheet, and source record, and synthetic fills of the worksheet. A synthetic
/// fill writes illustrative values into every worksheet row; it is never counter data.
/// </summary>
internal static class CounterSheetsTestData
{
    public const string ManifestPath = "src/ASL/units/catalog/scenario-a1.catalog-manifest.json";
    public const string SyntheticCatalogPath = "src/ASL/units/catalog/scenario-a1.synthetic.catalog.json";
    public const string PublishedCatalogPath = "src/ASL/units/catalog/scenario-a1.catalog.json";
    public const string RecordPath = "docs/ASL/SourceRegistry/CounterSheets/asl-counter-sheets.scenario-a1.source-record.json";
    public const string WorksheetPath = "docs/ASL/SourceRegistry/CounterSheets/scenario-a1.counter-worksheet.csv";
    public const string TranscriptionPath = "docs/ASL/SourceRegistry/CounterSheets/scenario-a1.counter-transcription.csv";

    public static readonly Lazy<UnitVocabulary> AslVocabulary = new(UnitVocabulary.Asl);

    public static string Root { get; } = FindRoot();

    public static byte[] Bytes(string path) => File.ReadAllBytes(Full(path));

    public static string Full(string path) => Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar));

    public static string Hash(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ReplaceLineEndings("\n"))));

    /// <summary>The worksheet with every row filled with a synthetic value, by the transcriber and, when given, the reviewer.</summary>
    public static string SyntheticTranscription(string transcriber = "Synthetic Transcriber", string reviewer = "", string sheet = "SYN")
    {
        var lines = File.ReadAllText(Full(WorksheetPath)).ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
        var rows = CounterTranscription.Read(Bytes(WorksheetPath)).Rows;
        var output = new StringBuilder(lines[0]).Append('\n');
        foreach (var row in rows)
        {
            output.Append(string.Join(',', sheet, row.Counter, row.Face, row.Attribute, SyntheticValue(row), transcriber, reviewer, "synthetic"))
                .Append('\n');
        }

        return output.ToString();
    }

    public static string SyntheticValue(TranscriptionRow row) => (row.Face, row.Attribute) switch
    {
        ("counter", "kind") => row.Note.Replace("expected ", string.Empty, StringComparison.Ordinal),
        ("counter", "nationality") => row.Counter.StartsWith("attacker", StringComparison.Ordinal) ? "american" : "italian",
        (_, "identity" or "class-variant") => CounterTranscription.NotPrinted,
        (_, "class") => "green",
        (_, "leadership") => "0",
        (_, var name) when name.Contains(':', StringComparison.Ordinal) => "no",
        _ => "1",
    };

    /// <summary>A source record for a transcription, with the given status and people.</summary>
    public static string Record(string transcription, string status = "transcribed-unreviewed", string transcriber = "Synthetic Transcriber",
        string? reviewer = null, string sheet = "SYN") => $$"""
        {
          "schemaVersion": 1,
          "sourceId": "asl-counter-sheets:scenario-a1",
          "status": "{{status}}",
          "sheets": [ { "id": "{{sheet}}", "title": "Synthetic sheet" } ],
          "transcription": {
            "format": "asl-counter-transcription/1",
            "path": "{{TranscriptionPath}}",
            "sha256": "{{Hash(transcription)}}",
            "transcriber": "{{transcriber}}",
            "transcribedOn": "2026-09-25"
          },
          "review": { "reviewer": {{(reviewer is null ? "null" : $"\"{reviewer}\"")}} }
        }
        """;

    public static CounterCatalogBuild Build(string transcription, string record) =>
        CounterSheetCatalogBuilder.Build(Bytes(ManifestPath), Encoding.UTF8.GetBytes(record), Encoding.UTF8.GetBytes(transcription),
            Bytes(WorksheetPath), AslVocabulary.Value);

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "ASL", "LimboDancer.Domains.Asl.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from " + AppContext.BaseDirectory);
    }
}
