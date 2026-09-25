using System.Globalization;
using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.CounterSheets;

public sealed record CounterCatalogBuild(UnitCatalog? Catalog, string? Json, IReadOnlyList<UnitDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error);
}

/// <summary>
/// Builds a definition catalog from a counter-sheet transcription (Scenario A1 Catalog Design, section 7). The catalog
/// manifest names the catalog, its slots, and which transcribed counter fills which slot; every printed value comes
/// from the transcription, row by row, and the source record says who transcribed and reviewed it. A reviewed source
/// yields a published catalog, an unreviewed one a draft. The result is written in the catalog's canonical form.
/// </summary>
public static class CounterSheetCatalogBuilder
{
    public const int ManifestSchemaVersion = 1;

    public static CounterCatalogBuild Build(ReadOnlySpan<byte> manifest, ReadOnlySpan<byte> record, ReadOnlySpan<byte> transcription,
        ReadOnlySpan<byte> worksheet, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        var diagnostics = new List<UnitDiagnostic>();
        var source = CounterSourceRecordReader.Read(record);
        diagnostics.AddRange(source.Diagnostics);
        var rows = CounterTranscription.Read(transcription);
        diagnostics.AddRange(rows.Diagnostics);
        var template = CounterTranscription.Read(worksheet);
        diagnostics.AddRange(template.Diagnostics.Select(diagnostic => diagnostic with { Message = "Worksheet: " + diagnostic.Message }));
        if (!Json.TryParse(manifest, "UNIT-CS-020", diagnostics, out var document) || source.Record is null || rows.HasErrors || template.HasErrors)
        {
            return new CounterCatalogBuild(null, null, diagnostics);
        }

        using (document)
        {
            var transcriptionHash = Json.ContentHash(transcription);
            CheckRecord(source.Record, transcriptionHash, diagnostics);
            CheckRows(source.Record, rows.Rows, template.Rows, diagnostics);
            var json = Write(document.RootElement, source.Record, Json.ContentHash(record), transcriptionHash, rows.Rows, vocabulary, diagnostics);
            if (json is null || diagnostics.Any(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error))
            {
                return new CounterCatalogBuild(null, null, diagnostics);
            }

            var read = UnitCatalogReader.Read(json, vocabulary);
            diagnostics.AddRange(read.Diagnostics);
            return read.Catalog is null
                ? new CounterCatalogBuild(null, null, diagnostics)
                : new CounterCatalogBuild(read.Catalog, UnitCatalogJson.Write(read.Catalog, vocabulary), diagnostics);
        }
    }

    private static void CheckRecord(CounterSourceRecord record, string transcriptionHash, List<UnitDiagnostic> diagnostics)
    {
        if (record.Status is not (CatalogSourceStatus.TranscribedUnreviewed or CatalogSourceStatus.Reviewed))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-011", $"The source is {record.Status}; a catalog is built only from a transcribed source."));
        }

        if (record.TranscriptionSha256 != transcriptionHash)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-011",
                $"The transcription has SHA-256 {transcriptionHash}, but the source record registers {record.TranscriptionSha256 ?? "nothing"}."));
        }

        if (record.Transcriber is null)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-011", "The source record names no transcriber."));
        }

        if (record.Status == CatalogSourceStatus.Reviewed && record.Reviewer is null)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-011", "A reviewed source record names its reviewer."));
        }

        if (record.Sheets.Count == 0)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-011", "The source record lists no counter sheets."));
        }
    }

    private static void CheckRows(CounterSourceRecord record, IReadOnlyList<TranscriptionRow> rows, IReadOnlyList<TranscriptionRow> worksheet,
        List<UnitDiagnostic> diagnostics)
    {
        foreach (var expected in worksheet.Where(expected => !rows.Any(row =>
            row.Counter == expected.Counter && row.Face == expected.Face && row.Attribute == expected.Attribute)))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-003",
                $"The worksheet asks for '{expected.Attribute}' on the {expected.Face} face of '{expected.Counter}'; the transcription has no such row.",
                $"worksheet line {expected.Row}"));
        }

        foreach (var row in rows)
        {
            var path = $"line {row.Row}";
            if (row.Value.Length == 0)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-004", $"No value: write what is printed, {CounterTranscription.NotPrinted}, or {CounterTranscription.NotInSource}.", path));
            }

            if (record.Sheets.All(sheet => sheet.Id != row.Sheet))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-005", $"The sheet '{row.Sheet}' is not listed in the source record.", path));
            }

            if (row.Transcriber != record.Transcriber)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-006", $"The row's transcriber '{row.Transcriber}' is not the record's '{record.Transcriber}'.", path));
            }

            if (record.Status == CatalogSourceStatus.Reviewed ? row.Reviewer != record.Reviewer : row.Reviewer.Length > 0)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-006", record.Status == CatalogSourceStatus.Reviewed
                    ? $"The row's reviewer '{row.Reviewer}' is not the record's '{record.Reviewer}'."
                    : "The row names a reviewer, but the source record is not reviewed.", path));
            }
        }

        foreach (var counter in rows.GroupBy(row => row.Counter, StringComparer.Ordinal).Where(group => group.Select(row => row.Sheet).Distinct().Count() > 1))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-005", $"The counter '{counter.Key}' is given more than one sheet."));
        }
    }

    private static string? Write(JsonElement manifest, CounterSourceRecord record, string recordHash, string transcriptionHash,
        IReadOnlyList<TranscriptionRow> rows, UnitVocabulary vocabulary, List<UnitDiagnostic> diagnostics)
    {
        var fields = new Json(diagnostics, "UNIT-CS-020");
        if (fields.Integer(manifest, "schemaVersion", "$") != ManifestSchemaVersion)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-020", $"The manifest schema version must be {ManifestSchemaVersion}.", "$"));
        }

        if (!Json.Object(manifest, "source", out var sourceElement))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-020", "'source' must be an object.", "$"));
            return null;
        }

        var sourceId = fields.String(sourceElement, "id", "$.source", required: true);
        var recordPath = fields.String(sourceElement, "record", "$.source", required: true);
        var transcriptionPath = fields.String(sourceElement, "transcription", "$.source", required: true);
        if (sourceId is not null && sourceId != record.SourceId)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-020", $"The manifest names the source '{sourceId}', but the record registers '{record.SourceId}'.", "$.source"));
        }

        if (transcriptionPath is not null && transcriptionPath != record.TranscriptionPath)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-020", "The manifest and the source record name different transcriptions.", "$.source"));
        }

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", UnitCatalogReader.SchemaVersion);
            writer.WriteString("catalog", fields.String(manifest, "catalog", "$", required: true));
            writer.WriteString("version", fields.String(manifest, "version", "$", required: true));
            writer.WriteString("label", fields.String(manifest, "label", "$", required: true));
            writer.WriteString("publication", record.Status == CatalogSourceStatus.Reviewed ? "published" : "draft");
            writer.WriteStartArray("vocabulary");
            foreach (var pack in fields.Strings(manifest, "vocabulary", "$"))
            {
                writer.WriteStringValue(pack);
            }

            writer.WriteEndArray();
            writer.WriteStartArray("sources");
            writer.WriteStartObject();
            writer.WriteString("id", record.SourceId);
            writer.WriteString("status", record.Status == CatalogSourceStatus.Reviewed ? "reviewed" : "transcribed-unreviewed");
            writer.WriteString("record", recordPath);
            writer.WriteString("recordSha256", recordHash);
            writer.WriteString("transcription", record.TranscriptionPath);
            writer.WriteString("transcriptionSha256", transcriptionHash);
            writer.WriteString("transcriber", record.Transcriber);
            if (record.Reviewer is not null)
            {
                writer.WriteString("reviewer", record.Reviewer);
            }

            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteStartArray("slots");
            foreach (var (slot, _) in fields.Objects(manifest, "slots", "$"))
            {
                slot.WriteTo(writer);
            }

            writer.WriteEndArray();
            writer.WriteStartArray("definitions");
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (definition, path) in fields.Objects(manifest, "definitions", "$"))
            {
                WriteDefinition(writer, definition, path, record.SourceId, rows, vocabulary, fields, diagnostics, used);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
            foreach (var counter in rows.Select(row => row.Counter).Distinct(StringComparer.Ordinal).Where(counter => !used.Contains(counter)))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-021", $"The transcribed counter '{counter}' is not a definition in the manifest."));
            }
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteDefinition(Utf8JsonWriter writer, JsonElement definition, string path, string sourceId, IReadOnlyList<TranscriptionRow> rows,
        UnitVocabulary vocabulary, Json fields, List<UnitDiagnostic> diagnostics, HashSet<string> used)
    {
        var id = fields.String(definition, "id", path, required: true);
        var counter = fields.String(definition, "counter", path, required: true);
        if (id is null || counter is null)
        {
            return;
        }

        used.Add(counter);
        var counterRows = rows.Where(row => row.Counter == counter).ToArray();
        var kindRow = counterRows.FirstOrDefault(row => row.Face == CounterTranscription.CounterFace && row.Attribute == "kind");
        var nationalityRow = counterRows.FirstOrDefault(row => row.Face == CounterTranscription.CounterFace && row.Attribute == "nationality");
        if (kindRow is null || nationalityRow is null)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-021", $"The counter '{counter}' needs a kind row and a nationality row on the counter face.", path));
            return;
        }

        var kind = kindRow.Value;
        if (!vocabulary.HasKind(kind))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-022", $"The kind '{kind}' is not declared.", $"line {kindRow.Row}"));
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("id", id);
        writer.WriteString("kind", kind);
        writer.WriteNumber("kindRow", kindRow.Row);
        writer.WriteString("nationality", nationalityRow.Value);
        writer.WriteNumber("nationalityRow", nationalityRow.Row);
        var classRow = counterRows.FirstOrDefault(row => row.Face != CounterTranscription.CounterFace
            && vocabulary.TryResolveAttribute(kind, row.Attribute, out var attribute, out _) && attribute.Name == "asl:class");
        if (classRow is not null && classRow.Value is not (CounterTranscription.NotPrinted or CounterTranscription.NotInSource))
        {
            writer.WriteString("class", classRow.Value);
        }

        writer.WriteStartObject("counter");
        writer.WriteString("source", sourceId);
        writer.WriteString("sheet", kindRow.Sheet);
        writer.WriteString("counter", counter);
        writer.WriteEndObject();
        if (Json.Object(definition, "applicability", out var applicability))
        {
            writer.WritePropertyName("applicability");
            applicability.WriteTo(writer);
        }
        else
        {
            writer.WriteStartObject("applicability");
            writer.WriteString("status", "unreviewed");
            writer.WriteEndObject();
        }

        writer.WriteStartArray("slots");
        foreach (var slot in fields.Strings(definition, "slots", path))
        {
            writer.WriteStringValue(slot);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("values");
        foreach (var row in counterRows.Where(row => row.Face != CounterTranscription.CounterFace))
        {
            WriteValue(writer, row, kind, vocabulary, diagnostics);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        foreach (var row in counterRows.Where(row => row.Face == CounterTranscription.CounterFace && row.Attribute is not ("kind" or "nationality")))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-022", $"The counter face holds only kind and nationality, not '{row.Attribute}'.", $"line {row.Row}"));
        }
    }

    private static void WriteValue(Utf8JsonWriter writer, TranscriptionRow row, string kind, UnitVocabulary vocabulary, List<UnitDiagnostic> diagnostics)
    {
        var path = $"line {row.Row}";
        writer.WriteStartObject();
        writer.WriteString("face", row.Face);
        if (vocabulary.TryGetTrait(row.Attribute, out _))
        {
            writer.WriteString("trait", row.Attribute);
            if (row.Value == CounterTranscription.NotInSource)
            {
                writer.WriteBoolean("recorded", false);
            }
            else if (row.Value is not ("yes" or "no"))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-022", $"A trait is written yes or no, not '{row.Value}'.", path));
            }
            else
            {
                writer.WriteBoolean("present", row.Value == "yes");
            }
        }
        else
        {
            writer.WriteString("attribute", row.Attribute);
            if (row.Value == CounterTranscription.NotPrinted)
            {
                writer.WriteBoolean("printed", false);
            }
            else if (row.Value == CounterTranscription.NotInSource)
            {
                writer.WriteBoolean("recorded", false);
            }
            else if (!vocabulary.TryResolveAttribute(kind, row.Attribute, out var attribute, out _))
            {
                // The catalog reader reports the undeclared attribute with its reason.
                writer.WriteString("value", row.Value);
            }
            else
            {
                WriteTyped(writer, attribute, row.Value, path, diagnostics);
            }
        }

        writer.WriteNumber("row", row.Row);
        writer.WriteEndObject();
    }

    private static void WriteTyped(Utf8JsonWriter writer, AttributeDefinition attribute, string value, string path, List<UnitDiagnostic> diagnostics)
    {
        switch (attribute.Type)
        {
            case AttributeType.Number when int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number):
                writer.WriteNumber("value", number);
                break;
            case AttributeType.Rating when int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var rating):
                writer.WriteNumber("value", rating);
                break;
            case AttributeType.Text or AttributeType.Enumeration:
                writer.WriteString("value", value);
                break;
            case AttributeType.List:
                writer.WriteStartArray("value");
                foreach (var item in value.Split('/', StringSplitOptions.TrimEntries))
                {
                    if (attribute.ItemType == AttributeType.Number && int.TryParse(item, NumberStyles.None, CultureInfo.InvariantCulture, out var itemNumber))
                    {
                        writer.WriteNumberValue(itemNumber);
                    }
                    else
                    {
                        writer.WriteStringValue(item);
                    }
                }

                writer.WriteEndArray();
                break;
            default:
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-022",
                    $"'{value}' is not {(attribute.Type == AttributeType.Rating ? "a whole number with an optional sign" : "a whole number")}.", path));
                writer.WriteNumber("value", 0);
                break;
        }
    }
}
