using System.Collections.Immutable;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.MapStudio.Services.Acronyms;

public sealed record AcronymReview(string Status, string Basis, string Changes, string Date);
public sealed record AcronymQuery(string Text, ImmutableArray<int> Pages);
public sealed record AcronymEntry(
    string Id, string Category, string Form, string Definition, string References, string Source,
    ImmutableArray<string> ShortForms, string ExpandedSearchText,
    string ShortPagesText, string ExpandedPagesText, ImmutableArray<int> ShortPages,
    ImmutableArray<int> ExpandedPages, ImmutableArray<AcronymQuery> Queries, AcronymReview Review);
public sealed record AcronymGuidance(string Heading, string Text);
public sealed record AcronymDocument(int Schema, string ScanTextHash, string LedgerTextHash,
    string PdfName, string PdfHash, int PdfPages, ImmutableArray<AcronymGuidance> Guidance,
    ImmutableArray<AcronymEntry> Entries);

/// <summary>Immutable packaged data. Load failure is distinct from an empty search.</summary>
public sealed partial class AcronymCatalog
{
    public const string ResourceName = "ASL.Acronyms.Catalog";
    public static JsonSerializerOptions JsonOptions { get; } = new() { WriteIndented = true };
    private readonly ImmutableDictionary<string, AcronymEntry> byId;
    public AcronymDocument? Document
    {
        get;
    }
    public string? Error
    {
        get;
    }
    public ImmutableArray<AcronymEntry> Entries => Document?.Entries ?? [];

    public AcronymCatalog(ILogger<AcronymCatalog> logger)
    {
        try
        {
            using var stream = typeof(AcronymCatalog).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidDataException("The acronym catalog resource is missing.");
            Document = Read(stream);
            byId = Document.Entries.ToImmutableDictionary(e => e.Id, StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or ArgumentException)
        {
            Error = "Acronym Search is unavailable because its reference data could not be loaded.";
            byId = ImmutableDictionary<string, AcronymEntry>.Empty;
            LoadFailed(logger, ex);
        }
    }

    public AcronymCatalog(AcronymDocument document)
    {
        Validate(document);
        Document = document;
        byId = document.Entries.ToImmutableDictionary(e => e.Id, StringComparer.Ordinal);
    }

    public AcronymEntry? Find(string? id) => id is not null && byId.TryGetValue(id, out var entry) ? entry : null;

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not load the acronym catalog")]
    private static partial void LoadFailed(ILogger logger, Exception exception);

    public static AcronymDocument Read(Stream stream)
    {
        var document = JsonSerializer.Deserialize<AcronymDocument>(stream, JsonOptions)
            ?? throw new InvalidDataException("The acronym catalog is empty.");
        Validate(document);
        return document;
    }

    public static void Validate(AcronymDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Schema != 1 || document.PdfPages <= 0 || document.Entries.IsDefaultOrEmpty
            || document.Guidance.IsDefault || string.IsNullOrWhiteSpace(document.PdfHash)
            || string.IsNullOrWhiteSpace(document.ScanTextHash) || string.IsNullOrWhiteSpace(document.LedgerTextHash))
        {
            throw new InvalidDataException("Unsupported or incomplete acronym catalog.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in document.Entries)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Id) || !ids.Add(entry.Id)
                || entry.Category is not ("main" or "equipment") || !entry.Id.StartsWith(entry.Category + ":", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(entry.Form) || string.IsNullOrWhiteSpace(entry.Definition)
                || string.IsNullOrWhiteSpace(entry.Source) || entry.References is null || entry.Review is null
                || entry.ShortForms.IsDefaultOrEmpty || entry.Queries.IsDefault || entry.ShortPages.IsDefault || entry.ExpandedPages.IsDefault)
            {
                throw new InvalidDataException($"Invalid or duplicate acronym entry: {entry?.Id}");
            }

            if (entry.Queries.Any(q => q is null || q.Pages.IsDefault || string.IsNullOrWhiteSpace(q.Text))
                || entry.ShortForms.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidDataException($"Invalid query/form: {entry.Id}");
            }

            if (entry.ShortPages.Concat(entry.ExpandedPages).Concat(entry.Queries.SelectMany(q => q.Pages))
                .Any(page => page < 1 || page > document.PdfPages))
            {
                throw new InvalidDataException($"Invalid physical page: {entry.Id}");
            }
        }
    }
}
