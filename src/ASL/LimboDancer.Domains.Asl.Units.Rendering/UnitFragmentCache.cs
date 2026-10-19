using System.Globalization;
using System.Text;
using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.Units.Rendering;

/// <summary>
/// The counter layer's hexes as drawn before (option E of the SVG memory analysis; the Counter Layer Design, sections 7 and 7.1): a hex whose
/// drawing inputs are those of an earlier build is not drawn again, its markup and warnings are reused. The key holds every input a hex's drawing
/// reads, so a hit is the hex as it would be drawn now. Bounded by characters (ruling U5): least recently used entries leave when the total passes
/// the bound. One cache serves every renderer and every caller at once.
/// </summary>
public sealed class UnitFragmentCache
{
    /// <summary>Ruling U5 of the Counter Layer Design: 4 million characters, about 8 MB.</summary>
    public const long DefaultBound = 4_000_000;

    private readonly object gate = new();
    private readonly Dictionary<string, LinkedListNode<Entry>> entries = new(StringComparer.Ordinal);
    private readonly LinkedList<Entry> recent = new();
    private long characters;
    private long hits;
    private long misses;

    public UnitFragmentCache(long bound = DefaultBound)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bound);
        Bound = bound;
    }

    public long Bound
    {
        get;
    }

    /// <summary>The characters held, keys and markup together.</summary>
    public long Characters
    {
        get
        {
            lock (gate)
            {
                return characters;
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return entries.Count;
            }
        }
    }

    public long Hits => Interlocked.Read(ref hits);

    public long Misses => Interlocked.Read(ref misses);

    internal bool TryGet(string key, out Fragment fragment)
    {
        lock (gate)
        {
            if (entries.TryGetValue(key, out var node))
            {
                recent.Remove(node);
                recent.AddFirst(node);
                fragment = node.Value.Fragment;
                Interlocked.Increment(ref hits);
                return true;
            }
        }

        Interlocked.Increment(ref misses);
        fragment = null!;
        return false;
    }

    internal void Add(string key, Fragment fragment)
    {
        var size = (long)key.Length + fragment.Markup.Length;
        if (size > Bound)
        {
            return;
        }

        lock (gate)
        {
            if (entries.TryGetValue(key, out var existing))
            {
                // Two callers drew the same hex at once: the first kept is as good as the second.
                recent.Remove(existing);
                recent.AddFirst(existing);
                return;
            }

            entries[key] = recent.AddFirst(new Entry(key, fragment, size));
            characters += size;
            while (characters > Bound && recent.Last is { } oldest)
            {
                recent.RemoveLast();
                entries.Remove(oldest.Value.Key);
                characters -= oldest.Value.Size;
            }
        }
    }

    /// <summary>A hex as drawn: its markup, the warnings its counters raised, and the figure outlines it refers to, which the layer holds (C1).</summary>
    internal sealed record Fragment(string Markup, RenderWarning[] Warnings, (string Id, string Path)[] Outlines);

    private sealed record Entry(string Key, Fragment Fragment, long Size);
}

/// <summary>
/// The fragment cache's key for a hex: the renderer, the hex and its center, the hex height, and each level's members in drawing order, each as
/// every field of its document. Every field is written, never a chosen few, so no state a counter can show is left out; a test holds the list
/// of the documents' properties, so a new one is added here when it is added there. Text is written with its length, so no text can be read as
/// another field.
/// </summary>
internal static class UnitFragmentKey
{
    public static void Hex(StringBuilder key, UnitRenderer renderer, int column, int row, double centerX, double centerY, double hexHeight)
    {
        key.Append(renderer.Identity).Append('|').Append(column).Append(',').Append(row).Append('|')
            .Append(centerX.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(centerY.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(hexHeight.ToString("R", CultureInfo.InvariantCulture));
    }

    public static void Level(StringBuilder key, int level, int members) => key.Append("|L").Append(level).Append('#').Append(members);

    /// <summary>The text before a counter's name (decision L6), which its accessible name and title begin with.</summary>
    public static void Lead(StringBuilder key, string? lead)
    {
        key.Append('l');
        Text(key, lead);
    }

    public static void Document(StringBuilder key, UnitDocument document)
    {
        key.Append('{');
        List(key, document.Vocabulary);
        Text(key, document.Id);
        Text(key, document.Kind);
        Text(key, document.Side);
        Text(key, document.Location);
        key.Append('F').Append(document.Faces.Count);
        foreach (var face in document.Faces)
        {
            Text(key, face.Name);
            Values(key, face.Values);
            List(key, face.Traits);
        }

        Values(key, document.Unit);
        List(key, document.States);
        key.Append('A').Append(document.Attached.Count);
        foreach (var attached in document.Attached)
        {
            Document(key, attached);
        }

        key.Append('o').Append(document.StackOrder)
            .Append(document.Concealed ? "c" : "-")
            .Append('s').Append(document.SizeClass?.ToString(CultureInfo.InvariantCulture) ?? "-")
            .Append('f').Append(document.Facing is { } facing ? ((int)facing).ToString(CultureInfo.InvariantCulture) : "-")
            .Append('t').Append(document.TurretFacing is { } turret ? ((int)turret).ToString(CultureInfo.InvariantCulture) : "-")
            .Append('h').Append(document.Hexside is { } hexside ? ((int)hexside).ToString(CultureInfo.InvariantCulture) : "-")
            .Append(document.Uninspected ? "u" : "-")
            .Append(document.Dummy ? "d" : "-")
            .Append('}');
    }

    private static void Values(StringBuilder key, IReadOnlyList<UnitValue> values)
    {
        key.Append('V').Append(values.Count);
        foreach (var value in values)
        {
            Text(key, value.Attribute.Name);
            key.Append((int)value.Attribute.Type).Append('n').Append(value.Number?.ToString(CultureInfo.InvariantCulture) ?? "-");
            Text(key, value.Text);
            List(key, value.Items);
        }
    }

    private static void List(StringBuilder key, IReadOnlyList<string> items)
    {
        key.Append('[').Append(items.Count);
        foreach (var item in items)
        {
            Text(key, item);
        }
    }

    private static void Text(StringBuilder key, string? text)
    {
        if (text is null)
        {
            key.Append('~');
            return;
        }

        key.Append(text.Length).Append(':').Append(text);
    }
}
