using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Catalog;
using static LimboDancer.Domains.Asl.Units.CounterSheets.Tests.CounterSheetsTestData;

namespace LimboDancer.Domains.Asl.Units.CounterSheets.Tests;

/// <summary>
/// The committed counter-sheet source, worksheet, manifest, and catalogs agree with each other (ASL-UNIT-080, 081).
/// Until the transcription exists, the record says so and no catalog other than the synthetic one is committed.
/// </summary>
public sealed class CommittedSourceTests
{
    private static JsonElement Manifest() => JsonDocument.Parse(Bytes(ManifestPath)).RootElement;

    [Fact]
    public void TheWorksheetAsksForExactlyTheManifestCounters()
    {
        var worksheet = CounterTranscription.Read(Bytes(WorksheetPath));
        Assert.Empty(worksheet.Diagnostics);
        var counters = Manifest().GetProperty("definitions").EnumerateArray().Select(definition => definition.GetProperty("counter").GetString()).ToArray();
        Assert.Equal(counters, worksheet.Rows.Select(row => row.Counter).Distinct());

        // A worksheet carries no values: every printed value comes from the transcriber.
        Assert.All(worksheet.Rows, row =>
        {
            Assert.Equal(string.Empty, row.Sheet);
            Assert.Equal(string.Empty, row.Value);
            Assert.Equal(string.Empty, row.Transcriber);
            Assert.Equal(string.Empty, row.Reviewer);
        });
    }

    [Fact]
    public void EveryWorksheetRowNamesSomethingItsKindAccepts()
    {
        var vocabulary = AslVocabulary.Value;
        var rows = CounterTranscription.Read(Bytes(WorksheetPath)).Rows;
        foreach (var counter in rows.GroupBy(row => row.Counter))
        {
            var kind = SyntheticValue(counter.Single(row => row.Face == "counter" && row.Attribute == "kind"));
            Assert.True(vocabulary.HasKind(kind), kind);
            Assert.Contains(counter, row => row.Face == "counter" && row.Attribute == "nationality");
            foreach (var row in counter.Where(row => row.Face != "counter"))
            {
                Assert.Contains(row.Face, vocabulary.Faces(kind));
                Assert.True(vocabulary.TryGetTrait(row.Attribute, out _)
                    ? vocabulary.AcceptedTraits(kind).Contains(row.Attribute)
                    : vocabulary.TryResolveAttribute(kind, row.Attribute, out _, out _), $"{counter.Key} {row.Face} {row.Attribute}");
            }
        }
    }

    [Fact]
    public void TheSyntheticCatalogHasTheManifestSlots()
    {
        var synthetic = UnitCatalogs.Read(UnitCatalogs.ScenarioA1Synthetic, AslVocabulary.Value)!.Catalog!;
        var manifest = Manifest();
        var slots = manifest.GetProperty("slots").EnumerateArray()
            .Select(slot => new CatalogSlot(slot.GetProperty("id").GetString()!, slot.GetProperty("kind").GetString()!, slot.GetProperty("label").GetString()!));
        Assert.Equal(slots, synthetic.Slots);
        foreach (var definition in manifest.GetProperty("definitions").EnumerateArray())
        {
            var id = definition.GetProperty("id").GetString()!;
            Assert.Equal(definition.GetProperty("slots").EnumerateArray().Select(slot => slot.GetString()), synthetic.Definition(id)!.Slots);
        }
    }

    [Fact]
    public void TheSourceRecordMatchesWhatIsCommitted()
    {
        var record = CounterSourceRecordReader.Read(Bytes(RecordPath));
        Assert.Empty(record.Diagnostics);
        Assert.Equal(TranscriptionPath, record.Record!.TranscriptionPath);
        Assert.Equal(Manifest().GetProperty("source").GetProperty("id").GetString(), record.Record.SourceId);
        if (!File.Exists(Full(TranscriptionPath)))
        {
            // Awaiting transcription: nothing is registered as transcribed and no catalog is built.
            Assert.Equal(CatalogSourceStatus.AwaitingTranscription, record.Record.Status);
            Assert.Null(record.Record.TranscriptionSha256);
            Assert.Null(record.Record.Transcriber);
            Assert.False(File.Exists(Full(PublishedCatalogPath)));
            Assert.Null(UnitCatalogs.Read(UnitCatalogs.ScenarioA1, AslVocabulary.Value));
            return;
        }

        var build = CounterSheetCatalogBuilder.Build(Bytes(ManifestPath), Bytes(RecordPath), Bytes(TranscriptionPath), Bytes(WorksheetPath), AslVocabulary.Value);
        Assert.Empty(build.Diagnostics);
        var committed = File.Exists(Full(PublishedCatalogPath)) ? File.ReadAllText(Full(PublishedCatalogPath)).ReplaceLineEndings("\n") : null;
        if (committed != build.Json)
        {
            var output = Path.Combine(AppContext.BaseDirectory, "scenario-a1.catalog.json");
            File.WriteAllText(output, build.Json);
            Assert.Fail($"{PublishedCatalogPath} differs from the catalog built from its sources; the built catalog is at {output}.");
        }

        Assert.Empty(build.Catalog!.VerifySource(record.Record.SourceId, Bytes(RecordPath), Bytes(TranscriptionPath)));
        var embedded = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, AslVocabulary.Value);
        Assert.Equal(build.Catalog.Identity, embedded!.Catalog!.Identity);
    }
}
