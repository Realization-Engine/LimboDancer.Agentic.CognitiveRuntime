namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// What a lab has drawn, for the workflow components it shares with the other lab (the brief editor, the prompt composer, the batch list):
/// the Icon Lab's icons, or the Unit Lab's figure parts. A key here is a drawing the lab has; a brief whose key is here is drawn.
/// </summary>
public interface IArtShelf
{
    /// <summary>The keys of the drawings the lab has.</summary>
    public IEnumerable<string> Keys
    {
        get;
    }

    /// <summary>What a drawing is called in the lab's words, such as "icon" or "figure part".</summary>
    public string Noun
    {
        get;
    }

    public bool Has(string key);

    /// <summary>A drawing's markup at a size in CSS pixels, or null when the lab has none for the key.</summary>
    public string? Thumbnail(string key, int pixels);
}
