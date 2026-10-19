using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace LimboDancer.Domains.Asl.Units.Rendering;

/// <summary>
/// What the counters of a layer share, collected as a hex of the layer is drawn (the Counter Layer Design, decision L1 and section 4): each
/// figure outline once, named by its content, <c>fig-{24 hex}</c>, so every counter that draws it refers to the same definition and the layer
/// writes it once. A counter drawn without a context (a preview, the Unit Lab, a golden) writes its own definitions as before.
/// </summary>
internal sealed class UnitLayerContext
{
    // The built-in figure art has a few dozen outlines; each is hashed once in the process.
    private static readonly ConcurrentDictionary<string, string> Ids = new(StringComparer.Ordinal);

    private readonly List<(string Id, string Path)> outlines = [];
    private readonly HashSet<string> held = new(StringComparer.Ordinal);

    /// <summary>The outlines referred to, each once, in the order first drawn.</summary>
    public IReadOnlyList<(string Id, string Path)> Outlines => outlines;

    /// <summary>The id of an outline's definition, which the layer will hold.</summary>
    public string Outline(string path)
    {
        var id = Ids.GetOrAdd(path, ContentId);
        if (held.Add(id))
        {
            outlines.Add((id, path));
        }

        return id;
    }

    /// <summary>A definition's name by its content (decision L3): SHA-256 over the UTF-16 text, 96 bits of it.</summary>
    internal static string ContentId(string text) => "fig-" + Hash96(text);

    /// <summary>SHA-256 over the UTF-16 text, its first 96 bits as 24 lowercase hex digits (decision L3).</summary>
    internal static string Hash96(string text) => Convert.ToHexStringLower(SHA256.HashData(MemoryMarshal.AsBytes(text.AsSpan())), 0, 12);
}
