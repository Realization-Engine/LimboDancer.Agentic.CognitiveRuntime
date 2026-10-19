using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Units.Rendering;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>A fragment to put in the browser's counter layer: in place of the one with its id, or else before <see cref="Before"/> (or at the end).</summary>
public sealed record UnitLayerUpsert(string Id, string Markup, string? Before);

/// <summary>
/// What changed in the counter layer since the version the browser holds (the Counter Layer Design, section 6): the fragments gone, the
/// fragments new or changed in paint order, and the layer's attributes. The browser applies it only to the layer of version <see cref="Base"/>.
/// </summary>
public sealed record UnitLayerPatch(int Base, int Version, IReadOnlyList<string> Removals, IReadOnlyList<UnitLayerUpsert> Upserts, string FaceSize, string UnitSet)
{
    /// <summary>The characters of markup the patch carries.</summary>
    public int Size => Upserts.Sum(upsert => upsert.Markup.Length);
}

public enum UnitLayerSendKind
{
    /// <summary>The browser holds this layer already.</summary>
    Nothing,

    /// <summary>The whole layer (or none, when <see cref="UnitLayerSend.Svg"/> is null), at <see cref="UnitLayerSend.Version"/>.</summary>
    Whole,

    /// <summary>A patch from the version the browser holds.</summary>
    Patch,
}

/// <summary>What to send the browser for a counter layer.</summary>
public sealed record UnitLayerSend(UnitLayerSendKind Kind, int Version, string? Svg, UnitLayerPatch? Patch);

/// <summary>
/// What the browser's counter layer holds, kept on the server for one viewport (the Counter Layer Design, section 6, and decision L5): its version,
/// its set, its attributes, and its fragments' ids and hashes in order. For each new layer it says what to send: nothing when nothing changed,
/// a patch when some fragments changed, and the whole layer when the browser's layer is not known (nothing sent yet, a board load, a patch the
/// browser refused), when the set changed, or when a patch would carry more than half the layer.
/// </summary>
public sealed class UnitLayerSync
{
    private int version;
    private bool known;
    private Held? held;

    /// <summary>The version of the layer last sent.</summary>
    public int Version => version;

    /// <summary>What to send for <paramref name="overlay"/>, recorded as sent.</summary>
    public UnitLayerSend Next(UnitOverlay? overlay)
    {
        if (overlay is null)
        {
            if (known && held is null)
            {
                return new UnitLayerSend(UnitLayerSendKind.Nothing, version, null, null);
            }

            (known, held) = (true, null);
            return new UnitLayerSend(UnitLayerSendKind.Whole, ++version, null, null);
        }

        var now = Held.Of(overlay);
        if (!known || held is not { } before || before.UnitSet != now.UnitSet)
        {
            return Whole(overlay, now);
        }

        if (before.FaceSize == now.FaceSize && before.Fragments.SequenceEqual(now.Fragments))
        {
            return new UnitLayerSend(UnitLayerSendKind.Nothing, version, null, null);
        }

        var patch = Diff(before, overlay, version, version + 1);
        if (2L * patch.Size > overlay.Fragments.Sum(fragment => (long)fragment.Markup.Length))
        {
            return Whole(overlay, now);
        }

        (version, held) = (patch.Version, now);
        return new UnitLayerSend(UnitLayerSendKind.Patch, version, null, patch);
    }

    /// <summary>The browser's layer is gone (a board load replaces everything): the next layer is sent whole.</summary>
    public void Reset() => (known, held) = (false, null);

    /// <summary>The browser refused a patch (its layer was not the version the patch was made from): the next layer is sent whole.</summary>
    public void Refused() => Reset();

    private UnitLayerSend Whole(UnitOverlay overlay, Held now)
    {
        (known, held) = (true, now);
        return new UnitLayerSend(UnitLayerSendKind.Whole, ++version, overlay.Svg, null);
    }

    /// <summary>
    /// The patch from the layer the browser holds to <paramref name="overlay"/>. A new fragment goes before the next fragment in paint order that the
    /// browser already holds, so the upserts, applied in order, leave the layer in the order a whole send would (section 9).
    /// </summary>
    internal static UnitLayerPatch Diff(Held before, UnitOverlay overlay, int baseVersion, int newVersion)
    {
        var old = before.Fragments.ToDictionary(fragment => fragment.Id, fragment => fragment.Hash, StringComparer.Ordinal);
        var ids = overlay.Fragments.Select(fragment => fragment.Id).ToHashSet(StringComparer.Ordinal);
        var removals = before.Fragments.Where(fragment => !ids.Contains(fragment.Id)).Select(fragment => fragment.Id).ToArray();
        var upserts = new List<UnitLayerUpsert>();
        for (var index = 0; index < overlay.Fragments.Count; index++)
        {
            var fragment = overlay.Fragments[index];
            if (old.TryGetValue(fragment.Id, out var hash) && hash == fragment.Hash)
            {
                continue;
            }

            var next = overlay.Fragments.Skip(index + 1).FirstOrDefault(later => old.ContainsKey(later.Id))?.Id;
            upserts.Add(new UnitLayerUpsert(fragment.Id, fragment.Markup, old.ContainsKey(fragment.Id) ? null : next));
        }

        return new UnitLayerPatch(baseVersion, newVersion, removals, upserts, SvgWriter.Number(overlay.FaceSize), overlay.SetId);
    }

    /// <summary>What a layer sent to the browser holds.</summary>
    internal sealed record Held(string UnitSet, string FaceSize, IReadOnlyList<(string Id, string Hash)> Fragments)
    {
        public static Held Of(UnitOverlay overlay) =>
            new(overlay.SetId, SvgWriter.Number(overlay.FaceSize), [.. overlay.Fragments.Select(fragment => (fragment.Id, fragment.Hash))]);
    }
}
