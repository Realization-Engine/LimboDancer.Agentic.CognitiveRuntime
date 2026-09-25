using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Catalog;

namespace LimboDancer.Domains.Asl.Units.CounterSheets;

/// <summary>A published counter sheet, identified well enough for a reviewer to find the same sheet.</summary>
public sealed record CounterSheet(string Id, string Title, string Publisher, string Product, string Printing);

/// <summary>
/// The registration of a counter-sheet source under decision D1 (Scenario A1 Catalog Design, section 6): the sheets
/// used, the transcription and its hash, who transcribed it, and who reviewed it.
/// </summary>
public sealed record CounterSourceRecord(
    string SourceId,
    CatalogSourceStatus Status,
    IReadOnlyList<CounterSheet> Sheets,
    string TranscriptionPath,
    string? TranscriptionSha256,
    string? Transcriber,
    string? TranscribedOn,
    string? Reviewer,
    string? ReviewedOn);

public sealed record CounterSourceRecordResult(CounterSourceRecord? Record, IReadOnlyList<UnitDiagnostic> Diagnostics);

public static class CounterSourceRecordReader
{
    public const int SchemaVersion = 1;

    public static CounterSourceRecordResult Read(ReadOnlySpan<byte> json)
    {
        var diagnostics = new List<UnitDiagnostic>();
        if (!Json.TryParse(json, "UNIT-CS-010", diagnostics, out var document))
        {
            return new CounterSourceRecordResult(null, diagnostics);
        }

        using (document)
        {
            var root = document.RootElement;
            var fields = new Json(diagnostics, "UNIT-CS-010");
            if (fields.Integer(root, "schemaVersion", "$") != SchemaVersion)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-010", $"The schema version must be {SchemaVersion}.", "$"));
            }

            var sourceId = fields.String(root, "sourceId", "$", required: true);
            var status = fields.String(root, "status", "$", required: true) switch
            {
                "awaiting-transcription" => CatalogSourceStatus.AwaitingTranscription,
                "transcribed-unreviewed" => CatalogSourceStatus.TranscribedUnreviewed,
                "reviewed" => CatalogSourceStatus.Reviewed,
                "synthetic" => CatalogSourceStatus.Synthetic,
                null => (CatalogSourceStatus?)null,
                var other => Invalid(diagnostics, $"'{other}' is not awaiting-transcription, transcribed-unreviewed, reviewed, or synthetic."),
            };

            var sheets = new List<CounterSheet>();
            foreach (var (sheet, sheetPath) in fields.Objects(root, "sheets", "$"))
            {
                var id = fields.String(sheet, "id", sheetPath, required: true);
                if (id is not null)
                {
                    sheets.Add(new CounterSheet(id, fields.String(sheet, "title", sheetPath) ?? string.Empty,
                        fields.String(sheet, "publisher", sheetPath) ?? string.Empty, fields.String(sheet, "product", sheetPath) ?? string.Empty,
                        fields.String(sheet, "printing", sheetPath) ?? string.Empty));
                }
            }

            foreach (var group in sheets.GroupBy(sheet => sheet.Id, StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-010", $"The sheet '{group.Key}' is declared twice.", "$.sheets"));
            }

            if (!Json.Object(root, "transcription", out var transcription) || !Json.Object(root, "review", out var review))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-010", "'transcription' and 'review' must be objects.", "$"));
                return new CounterSourceRecordResult(null, diagnostics);
            }

            var format = fields.String(transcription, "format", "$.transcription", required: true);
            if (format is not null && format != CounterTranscription.Format)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-010", $"The transcription format must be {CounterTranscription.Format}.", "$.transcription"));
            }

            var path = fields.String(transcription, "path", "$.transcription", required: true);
            if (sourceId is null || status is null || path is null || diagnostics.Any(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error))
            {
                return new CounterSourceRecordResult(null, diagnostics);
            }

            return new CounterSourceRecordResult(new CounterSourceRecord(sourceId, status.Value, sheets, path,
                fields.String(transcription, "sha256", "$.transcription"), fields.String(transcription, "transcriber", "$.transcription"),
                fields.String(transcription, "transcribedOn", "$.transcription"), fields.String(review, "reviewer", "$.review"),
                fields.String(review, "reviewedOn", "$.review")), diagnostics);
        }
    }

    private static CatalogSourceStatus? Invalid(List<UnitDiagnostic> diagnostics, string message)
    {
        diagnostics.Add(UnitDiagnostic.Error("UNIT-CS-010", message, "$"));
        return null;
    }
}
