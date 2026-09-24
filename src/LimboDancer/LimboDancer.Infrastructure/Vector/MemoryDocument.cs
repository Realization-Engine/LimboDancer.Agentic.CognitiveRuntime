namespace LimboDancer.Infrastructure.Vector;

public sealed class MemoryDocument
{
    public MemoryDocument(
        string id,
        string content,
        string? title = null,
        string? source = null,
        string? ontologyClass = null,
        string? uri = null,
        IEnumerable<string>? tags = null,
        IEnumerable<float>? vector = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Id = id;
        Content = content;
        Title = title;
        Source = source;
        OntologyClass = ontologyClass;
        Uri = uri;
        Tags = Array.AsReadOnly((tags ?? []).ToArray());
        Vector = Array.AsReadOnly((vector ?? []).ToArray());
    }

    public string Id
    {
        get;
    }

    public string Content
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

    public IReadOnlyList<float> Vector
    {
        get;
    }
}
