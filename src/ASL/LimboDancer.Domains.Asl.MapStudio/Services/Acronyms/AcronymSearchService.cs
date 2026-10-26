using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace LimboDancer.Domains.Asl.MapStudio.Services.Acronyms;

public sealed record AcronymSearchState(string Query = "", string Category = "all", bool MatchCase = false,
    bool Audit = false, int Page = 1, string? Entry = null)
{
    public static AcronymSearchState FromUri(string uri)
    {
        var query = QueryHelpers.ParseQuery(new Uri(uri).Query);
        string Get(string key) => query.TryGetValue(key, out var value) ? value.ToString() : "";
        return new(Get("q"), Get("category") is "main" or "equipment" ? Get("category") : "all",
            bool.TryParse(Get("case"), out var matchCase) && matchCase,
            bool.TryParse(Get("audit"), out var audit) && audit,
            int.TryParse(Get("page"), out var page) && page > 0 ? page : 1,
            string.IsNullOrEmpty(Get("entry")) ? null : Get("entry"));
    }

    public string ToUri(string baseUri)
    {
        var values = new Dictionary<string, string?>();
        if (!string.IsNullOrEmpty(Query))
        {
            values["q"] = Query;
        }

        if (Category is "main" or "equipment")
        {
            values["category"] = Category;
        }

        if (MatchCase)
        {
            values["case"] = "true";
        }

        if (Audit)
        {
            values["audit"] = "true";
        }

        if (Page > 1)
        {
            values["page"] = Page.ToString(CultureInfo.InvariantCulture);
        }

        if (Entry is not null)
        {
            values["entry"] = Entry;
        }

        return QueryHelpers.AddQueryString(baseUri.Split('?', '#')[0], values);
    }
}

public sealed record AcronymHit(AcronymEntry Entry, int Rank, string Reason);
public sealed record AcronymSearchResult(int Total, int Page, int Pages, ImmutableArray<AcronymHit> Hits);

/// <summary>A direct search over a small immutable catalog, with explicit ranking and case-sensitive identity.</summary>
public sealed partial class AcronymSearchService(AcronymCatalog catalog)
{
    public const int PageSize = 25;

    public AcronymSearchResult Search(AcronymSearchState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var all = Matches(state).OrderBy(h => h.Rank)
            .ThenBy(h => h.Entry.Form, StringComparer.OrdinalIgnoreCase)
            .ThenBy(h => h.Entry.Id, StringComparer.Ordinal).ToArray();
        int pages = Math.Max(1, (all.Length + PageSize - 1) / PageSize);
        int page = Math.Clamp(state.Page, 1, pages);
        return new(all.Length, page, pages, all.Skip((page - 1) * PageSize).Take(PageSize).ToImmutableArray());
    }

    public bool Includes(AcronymEntry entry, AcronymSearchState state) => Matches(state).Any(h => h.Entry.Id == entry.Id);

    private IEnumerable<AcronymHit> Matches(AcronymSearchState state)
    {
        string query = state.Query.Trim().Normalize();
        string proseQuery = Prose(query);
        var words = Words().Matches(proseQuery).Select(m => m.Value).ToArray();
        foreach (var entry in catalog.Entries)
        {
            if (state.Category is "main" or "equipment" && state.Category != entry.Category)
            {
                continue;
            }

            var hit = Rank(entry, query, proseQuery, words, state);
            if (hit is not null)
            {
                yield return hit;
            }
        }
    }

    private static AcronymHit? Rank(AcronymEntry entry, string query, string proseQuery, string[] words, AcronymSearchState state)
    {
        if (query.Length == 0)
        {
            return new(entry, 0, "Browse");
        }

        var forms = entry.ShortForms.Prepend(entry.Form);
        var comparison = state.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        if (forms.Any(f => f.Normalize().Equals(query, StringComparison.Ordinal)))
        {
            return new(entry, 0, "Exact acronym");
        }

        if (!state.MatchCase && forms.Any(f => f.Normalize().Equals(query, StringComparison.OrdinalIgnoreCase)))
        {
            return new(entry, 10, "Acronym");
        }

        if (forms.Any(f => f.Normalize().StartsWith(query, StringComparison.Ordinal)))
        {
            return new(entry, 20, "Acronym prefix");
        }

        if (!state.MatchCase && forms.Any(f => f.Normalize().StartsWith(query, StringComparison.OrdinalIgnoreCase)))
        {
            return new(entry, 21, "Acronym prefix");
        }

        string references = entry.References + " " + entry.Source;
        if (CitationQuery().IsMatch(query) && Regex.IsMatch(references,
            @"(?<![\p{L}\p{N}.])" + Regex.Escape(query) + @"(?![\p{L}\p{N}]|\.[0-9])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
        {
            return new(entry, 30, "Reference");
        }

        if (proseQuery.Length > 0 && Prose(entry.Definition).Contains(proseQuery, StringComparison.Ordinal))
        {
            return new(entry, 30, "Definition");
        }

        string prose = Prose(entry.Definition + " " + references);
        if (words.Length > 0 && words.All(w => prose.Contains(w, StringComparison.Ordinal)))
        {
            return new(entry, 40, Prose(entry.Definition).Contains(proseQuery, StringComparison.Ordinal) ? "Definition" : "Definition or reference");
        }

        if (forms.Any(f => f.Normalize().Contains(query, comparison)))
        {
            return new(entry, 50, "Partial acronym");
        }

        if (state.Audit && words.Length > 0 && entry.Queries.Any(q => words.All(w => Prose(q.Text).Contains(w, StringComparison.Ordinal))))
        {
            return new(entry, 60, "Audit search term");
        }

        return null;
    }

    private static string Prose(string text)
    {
        var result = new StringBuilder();
        foreach (char c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            result.Append(c is '-' or '\u2010' or '\u2011' ? ' ' : char.ToUpperInvariant(c));
        }
        return Whitespace().Replace(result.ToString(), " ").Trim();
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:\.[\p{N}]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex Words();
    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
    [GeneratedRegex(@"^[A-Za-z](?:[0-9]+(?:\.[0-9]+)*|\.[0-9]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex CitationQuery();
}
