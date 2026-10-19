using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace LimboDancer.Domains.Asl.Units.Rendering;

/// <summary>
/// What the counters of a layer share, collected as a hex of the layer is drawn (the Counter Layer Design, decision L1 and section 4): each
/// figure outline once, named by its content, <c>fig-{24 hex}</c>, so every counter that draws it refers to the same definition and the layer
/// writes it once. A counter drawn without a context (a preview, the Unit Lab, a golden) writes its own definitions as before.
/// </summary>
internal sealed class UnitLayerContext(UnitFaceMode faceMode = UnitFaceMode.InPlace)
{
    // The built-in figure art has a few dozen outlines; each is hashed once in the process.
    private static readonly ConcurrentDictionary<string, string> Ids = new(StringComparer.Ordinal);

    private readonly List<(string Id, string Path)> outlines = [];
    private readonly HashSet<string> held = new(StringComparer.Ordinal);
    private readonly List<(string Id, string Markup)> definitions = [];
    private readonly HashSet<string> heldFaces = new(StringComparer.Ordinal);

    /// <summary>How the counters' faces are written (option Q).</summary>
    public UnitFaceMode Faces => faceMode;

    /// <summary>The outlines referred to, each once, in the order first drawn.</summary>
    public IReadOnlyList<(string Id, string Path)> Outlines => outlines;

    /// <summary>The face definitions referred to (option Q), each once, in the order first drawn.</summary>
    public IReadOnlyList<(string Id, string Markup)> FaceDefinitions => definitions;

    /// <summary>
    /// The id of a face's definition: <c>face-{24 hex}</c> of its markup as drawn at the origin, for one tier (decision L3; section 8). The
    /// definition is a <c>&lt;g data-face-def&gt;</c>, never a symbol, which would clip a covered arc or a badge reaching outside the face.
    /// </summary>
    public string Face(string tier, string content)
    {
        var id = "face-" + Hash96(tier + "|" + content);
        if (heldFaces.Add(id))
        {
            definitions.Add((id, $"<g id=\"{id}\" data-face-def=\"{tier}\">{content}</g>"));
        }

        return id;
    }

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

/// <summary>How a counter layer writes its counters' faces (the Counter Layer Design, section 8).</summary>
internal enum UnitFaceMode
{
    /// <summary>Each face drawn where it stands, as a standalone counter's is.</summary>
    InPlace,

    /// <summary>Each face drawn at the origin and moved into place: the layer of option Q with every reference written out, the oracle's form.</summary>
    AtOrigin,

    /// <summary>Each distinct face defined once at the origin and each counter's three faces referred to by position (option Q).</summary>
    Shared,
}
