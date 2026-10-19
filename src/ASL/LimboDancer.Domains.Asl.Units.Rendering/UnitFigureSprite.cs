using System.Runtime.CompilerServices;
using LimboDancer.Domains.Asl.Maps.Rendering;

namespace LimboDancer.Domains.Asl.Units.Rendering;

/// <summary>
/// The viewport's figure sprite (option C2; the Counter Layer Design, section 4, ruling U1): every outline of a figure art, each once, named by its
/// content as the counter layer refers to it, in <c>&lt;g id="unit-figures"&gt;&lt;defs&gt;</c>. The viewport holds it beside the board's layers from
/// a board load on, so a counter layer, whole or patched, carries no outline the sprite holds. Made once per figure art.
/// </summary>
public static class UnitFigureSprite
{
    public const string Id = "unit-figures";

    private static readonly ConditionalWeakTable<FigureArt, Sprite> Made = [];

    /// <summary>The sprite's markup for a figure art.</summary>
    public static string Markup(FigureArt art)
    {
        ArgumentNullException.ThrowIfNull(art);
        return For(art).Markup;
    }

    internal static Sprite For(FigureArt art) => Made.GetValue(art, Make);

    private static Sprite Make(FigureArt art)
    {
        var outlines = art.Outlines.Select(path => (Id: UnitLayerContext.ContentId(path), Path: path)).OrderBy(outline => outline.Id, StringComparer.Ordinal).ToArray();
        var svg = new SvgWriter();
        svg.Start("g", ("id", Id));
        svg.Start("defs");
        foreach (var (id, path) in outlines)
        {
            svg.Empty("path", ("id", id), ("d", SvgValue.Trusted(path)));
        }

        svg.End();
        svg.End();
        return new Sprite(svg.ToString(), outlines.Select(outline => outline.Id).ToHashSet(StringComparer.Ordinal));
    }

    /// <summary>The sprite's markup and the ids of the outlines it holds.</summary>
    internal sealed record Sprite(string Markup, IReadOnlySet<string> Ids);
}
