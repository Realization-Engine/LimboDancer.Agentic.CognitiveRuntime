using System.Collections.Concurrent;
using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.Memory;

namespace LimboDancer.Infrastructure.Vector;

public sealed class InMemoryMemorySearch : IMemorySearch
{
    private readonly ConcurrentDictionary<(Guid TenantId, string DocumentId), MemoryDocument> documents = new();

    public ValueTask UpsertAsync(
        TenantScope tenant,
        MemoryDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        tenant.ThrowIfInvalid();
        cancellationToken.ThrowIfCancellationRequested();
        documents[(tenant.TenantId, document.Id)] = document;
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<MemorySearchItem>> SearchAsync(
        TenantScope tenant,
        MemorySearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        tenant.ThrowIfInvalid();
        cancellationToken.ThrowIfCancellationRequested();

        var results = documents
            .Where(pair => pair.Key.TenantId == tenant.TenantId)
            .Select(static pair => pair.Value)
            .Where(document => MatchesFilters(document, query))
            .Select(document => new
            {
                Document = document,
                Score = Score(document, query),
            })
            .Where(static match => match.Score > 0)
            .OrderByDescending(static match => match.Score)
            .ThenBy(static match => match.Document.Id, StringComparer.Ordinal)
            .Take(query.Limit)
            .Select(static match => new MemorySearchItem(
                match.Document.Id,
                match.Document.Content,
                match.Score,
                match.Document.Title,
                match.Document.Source,
                match.Document.OntologyClass,
                match.Document.Uri,
                match.Document.Tags))
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<MemorySearchItem>>(Array.AsReadOnly(results));
    }

    private static bool MatchesFilters(MemoryDocument document, MemorySearchQuery query)
    {
        if (query.OntologyClass is not null
            && !string.Equals(document.OntologyClass, query.OntologyClass, StringComparison.Ordinal))
        {
            return false;
        }

        if (query.Uri is not null && !string.Equals(document.Uri, query.Uri, StringComparison.Ordinal))
        {
            return false;
        }

        return query.TagsAny.Count == 0
            || query.TagsAny.Any(tag => document.Tags.Contains(tag, StringComparer.Ordinal));
    }

    private static double Score(MemoryDocument document, MemorySearchQuery query)
    {
        var scores = new List<double>(capacity: 2);
        var queryText = query.QueryText;
        if (!string.IsNullOrWhiteSpace(queryText))
        {
            var terms = queryText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var searchable = string.Join(' ', new[] { document.Title, document.Content, document.Source });
            scores.Add(terms.Count(term => searchable.Contains(term, StringComparison.OrdinalIgnoreCase)) / (double)terms.Length);
        }

        if (query.QueryVector.Count > 0)
        {
            scores.Add(CosineSimilarity(query.QueryVector, document.Vector));
        }

        return scores.Count == 0 ? 0 : scores.Average();
    }

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        if (left.Count == 0 || left.Count != right.Count)
        {
            return 0;
        }

        double dotProduct = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;
        for (var index = 0; index < left.Count; index++)
        {
            dotProduct += left[index] * right[index];
            leftMagnitude += left[index] * left[index];
            rightMagnitude += right[index] * right[index];
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return dotProduct / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}
