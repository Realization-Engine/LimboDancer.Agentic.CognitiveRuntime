namespace LimboDancer.Abstractions.State.Memory;

public sealed class MemorySearchQuery
{
    public MemorySearchQuery(
        string? queryText,
        int limit = 8,
        string? ontologyClass = null,
        string? uri = null,
        IEnumerable<string>? tagsAny = null,
        IEnumerable<float>? queryVector = null)
    {
        var vector = (queryVector ?? []).ToArray();
        if (string.IsNullOrWhiteSpace(queryText) && vector.Length == 0)
        {
            throw new ArgumentException("A text query or query vector is required.", nameof(queryText));
        }

        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be between 1 and 100.");
        }

        QueryText = queryText;
        Limit = limit;
        OntologyClass = ontologyClass;
        Uri = uri;
        TagsAny = Array.AsReadOnly((tagsAny ?? []).ToArray());
        QueryVector = Array.AsReadOnly(vector);
    }

    public string QueryText
    {
        get;
    }

    public int Limit
    {
        get;
    }

    public string? OntologyClass
    {
        get;
    }

    public string? Uri
    {
        get;
    }

    public IReadOnlyList<string> TagsAny
    {
        get;
    }

    public IReadOnlyList<float> QueryVector
    {
        get;
    }
}

public sealed class MemorySearchItem
{
    public MemorySearchItem(
        string id,
        string content,
        double score,
        string? title = null,
        string? source = null,
        string? ontologyClass = null,
        string? uri = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Id = id;
        Content = content;
        Score = score;
        Title = title;
        Source = source;
        OntologyClass = ontologyClass;
        Uri = uri;
        Tags = Array.AsReadOnly((tags ?? []).ToArray());
    }

    public string Id
    {
        get;
    }

    public string Content
    {
        get;
    }

    public double Score
    {
        get;
    }

    public string? Title
    {
        get;
    }

    public string? Source
    {
        get;
    }

    public string? OntologyClass
    {
        get;
    }

    public string? Uri
    {
        get;
    }

    public IReadOnlyList<string> Tags
    {
        get;
    }
}
