using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.MapStudio.Services.Acronyms;
using Microsoft.VisualBasic.FileIO;

namespace AslAcronyms;

public static partial class CatalogBuilder
{
    private static readonly string[] Columns = ["entry_id", "acronym", "short_forms_searched", "expanded_or_context_searches",
        "short_form_pdf_pages", "expanded_or_context_pdf_pages", "definition", "document_reference_trail", "definition_source",
        "expanded_query_page_map", "remaining_review_status", "remaining_review_basis", "remaining_review_changes", "review_date"];

    public static string Normalize(string text) => text.TrimStart('\uFEFF').Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    public static string Generate(string scan, string ledger) => Normalize(JsonSerializer.Serialize(Build(scan, ledger), AcronymCatalog.JsonOptions)) + "\n";

    public static AcronymDocument Build(string scan, string ledger)
    {
        scan = Normalize(scan);
        ledger = Normalize(ledger);
        var pdf = PdfPattern().Match(scan);
        var hash = HashPattern().Match(scan);
        if (!pdf.Success || !hash.Success) throw new InvalidDataException("Scan: missing PDF edition/page count/hash.");
        int pageCount = int.Parse(pdf.Groups[2].Value, CultureInfo.InvariantCulture);
        var entries = ReadLedger(ledger, pageCount);
        var used = new HashSet<string>(StringComparer.Ordinal);
        string category = "", heading = "";
        var guidance = ImmutableArray.CreateBuilder<AcronymGuidance>();
        var prose = new StringBuilder();
        foreach (var line in scan.Split('\n'))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (category.Length == 0 && prose.Length > 0 && heading.Length > 0)
                    guidance.Add(new(heading, Normalize(prose.ToString()).Trim()));
                prose.Clear();
                heading = line[3..];
                category = heading switch
                {
                    "Alphabetical definitions and rule references" => "main",
                    "Equipment names and historical or publication abbreviations" => "equipment",
                    _ => ""
                };
                continue;
            }
            if (category.Length == 0) { prose.AppendLine(line); continue; }
            if (!line.StartsWith('|')) continue;
            var cells = SplitTableRow(line);
            int expected = category == "main" ? 4 : 3;
            if (cells.Length != expected) throw new InvalidDataException($"Scan {heading}: expected {expected} columns: {line}");
            if (cells[0].StartsWith("Acronym or", StringComparison.Ordinal) || cells.All(c => c.All(ch => ch is '-' or ':' or ' '))) continue;
            if (!cells[0].StartsWith((char)96) || !cells[0].EndsWith((char)96)) throw new InvalidDataException($"Scan {heading}: unsupported form: {cells[0]}");
            var form = cells[0][1..^1];
            var candidates = entries.Where(e => e.Category == category && e.Form == form).ToArray();
            var matched = candidates.Where(e => Plain(e.Definition) == Plain(cells[1])
                && Plain(e.References) == Plain(cells[2]) && Plain(e.Source) == Plain(cells[expected - 1])).ToArray();
            if (matched.Length != 1 || !used.Add(matched[0].Id))
                throw new InvalidDataException($"Scan {heading}/{form}: missing, ambiguous, duplicate, or mismatched definition/reference/source; candidates: {string.Join(", ", candidates.Select(e => e.Id))}");
        }
        if (category.Length == 0 && prose.Length > 0) guidance.Add(new(heading, Normalize(prose.ToString()).Trim()));
        if (used.Count != entries.Length) throw new InvalidDataException("Ledger entries absent from scan: " + string.Join(", ", entries.Where(e => !used.Contains(e.Id)).Select(e => e.Id)));
        var document = new AcronymDocument(1, TextHash(scan), TextHash(ledger), pdf.Groups[1].Value, hash.Groups[1].Value,
            pageCount, guidance.ToImmutable(), entries);
        AcronymCatalog.Validate(document);
        return document;
    }

    private static ImmutableArray<AcronymEntry> ReadLedger(string ledger, int pageCount)
    {
        using var reader = new TextFieldParser(new StringReader(ledger)) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
        reader.SetDelimiters(",");
        var headers = reader.ReadFields() ?? throw new InvalidDataException("Ledger header missing.");
        if (headers.Length != Columns.Length || !headers.Order(StringComparer.Ordinal).SequenceEqual(Columns.Order(StringComparer.Ordinal)))
            throw new InvalidDataException("Ledger must contain the 14 expected column names exactly once.");
        var rows = ImmutableArray.CreateBuilder<AcronymEntry>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        while (!reader.EndOfData)
        {
            long row = reader.LineNumber;
            try
            {
                var fields = reader.ReadFields()!;
                if (fields.Length != headers.Length) throw new InvalidDataException("Column count differs from header.");
                string Get(string name) => fields[Array.IndexOf(headers, name)];
                string id = Get("entry_id");
                if (!ids.Add(id)) throw new InvalidDataException($"Duplicate entry ID: {id}");
                using var map = JsonDocument.Parse(Get("expanded_query_page_map"));
                var queries = ImmutableArray.CreateBuilder<AcronymQuery>();
                var keys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in map.RootElement.EnumerateObject())
                {
                    if (!keys.Add(property.Name)) throw new InvalidDataException($"Duplicate query: {property.Name}");
                    var pages = property.Value.EnumerateArray().Select(p => p.GetInt32()).ToArray();
                    if (pages.Any(p => p < 1 || p > pageCount)) throw new InvalidDataException("Query page outside PDF.");
                    queries.Add(new(property.Name, pages.Distinct().Order().ToImmutableArray()));
                }
                rows.Add(new(id, id.Split(':')[0], Get("acronym"), Get("definition"), Get("document_reference_trail"), Get("definition_source"),
                    Get("short_forms_searched").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToImmutableArray(),
                    Get("expanded_or_context_searches"), Get("short_form_pdf_pages"), Get("expanded_or_context_pdf_pages"),
                    Pages(Get("short_form_pdf_pages"), pageCount), Pages(Get("expanded_or_context_pdf_pages"), pageCount),
                    queries.OrderBy(q => q.Text, StringComparer.Ordinal).ToImmutableArray(),
                    new(Get("remaining_review_status"), Get("remaining_review_basis"), Get("remaining_review_changes"), Get("review_date"))));
            }
            catch (Exception ex) when (ex is MalformedLineException or JsonException or InvalidOperationException or FormatException or OverflowException or InvalidDataException)
            {
                throw new InvalidDataException($"Ledger row {row}: {ex.Message}", ex);
            }
        }
        return rows.OrderBy(e => e.Id, StringComparer.Ordinal).ToImmutableArray();
    }

    public static ImmutableArray<int> Pages(string text, int maximum)
    {
        var pages = new SortedSet<int>();
        if (string.IsNullOrWhiteSpace(text)) return [];
        foreach (var part in text.Split(','))
        {
            var range = part.Trim().Split('-');
            if (range.Length is < 1 or > 2 || !int.TryParse(range[0], out int first)
                || !int.TryParse(range[^1], out int last) || first < 1 || last < first || last > maximum)
                throw new InvalidDataException($"Invalid PDF page range: {part}");
            for (int p = first; p <= last; p++) pages.Add(p);
        }
        return pages.ToImmutableArray();
    }

    public static string[] SplitTableRow(string line)
    {
        // The source uses inline code and escaped pipes; a pipe inside code is content.
        var cells = new List<string>();
        var cell = new StringBuilder();
        bool code = false;
        for (int i = 1; i < line.Length - 1; i++)
        {
            char c = line[i];
            if (c == '\\' && i + 1 < line.Length - 1 && line[i + 1] == '|') { cell.Append('|'); i++; }
            else if (c == (char)96) { code = !code; cell.Append(c); }
            else if (c == '|' && !code) { cells.Add(cell.ToString().Trim()); cell.Clear(); }
            else cell.Append(c);
        }
        if (!line.EndsWith('|') || code) throw new InvalidDataException("Malformed Markdown table row.");
        cells.Add(cell.ToString().Trim());
        return cells.ToArray();
    }

    private static string Plain(string text) => text.Trim().Replace(((char)96).ToString(), "", StringComparison.Ordinal);
    private static string TextHash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    [GeneratedRegex("Source: [\u0060]([^\u0060]+)[\u0060],[^\\n]*?([0-9]+) physical PDF pages")]
    private static partial Regex PdfPattern();
    [GeneratedRegex("Verified SHA-256: [\u0060]([a-f0-9]{64})[\u0060]")]
    private static partial Regex HashPattern();
}
