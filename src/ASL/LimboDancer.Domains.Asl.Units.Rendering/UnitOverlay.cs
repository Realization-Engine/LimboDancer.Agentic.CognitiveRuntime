using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering.Styles;

namespace LimboDancer.Domains.Asl.Units.Rendering;

/// <summary>
/// Where units can be placed: a single board, or a composed map that translates each placed board's hexes into map
/// hexes (ASL-UNIT-071). The board geometry supplies every anchor; no hex name is parsed into pixels here.
/// </summary>
public sealed class UnitMapTarget
{
    private readonly Func<BoardRef, HexName, HexIndex?> locate;

    private UnitMapTarget(BoardRef reference, BoardGeometry geometry, Func<BoardRef, HexName, HexIndex?> locate, IReadOnlyList<BoardRef> boards)
    {
        Ref = reference;
        Geometry = geometry;
        this.locate = locate;
        Boards = boards;
    }

    public BoardRef Ref
    {
        get;
    }

    public BoardGeometry Geometry
    {
        get;
    }

    /// <summary>The boards whose locations can be placed: the board itself, or the map and every placed board.</summary>
    public IReadOnlyList<BoardRef> Boards
    {
        get;
    }

    public static UnitMapTarget ForBoard(BoardRef board, BoardGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(geometry);
        return new UnitMapTarget(board, geometry, (reference, hex) => reference == board && geometry.TryGetIndex(hex, out var index) ? index : null, [board]);
    }

    /// <summary>A composed map: locations on any placed board go through <see cref="VaslMap.Locate"/>; the map's own names also work.</summary>
    public static UnitMapTarget ForMap(BoardRef reference, VaslMap map)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(map);
        return new UnitMapTarget(reference, map.Geometry,
            (board, hex) => board == reference ? map.Geometry.TryGetIndex(hex, out var index) ? index : null : map.Locate(board, hex),
            [reference, .. map.Boards.Select(board => board.Placement.Board).Distinct()]);
    }

    public HexIndex? Locate(BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return location.Side is null ? locate(location.Board, location.Hex) : null;
    }
}

/// <summary>A unit on the overlay. <see cref="Shown"/> is false for stack members beyond the six drawn; the inspector lists them.</summary>
public sealed record PlacedUnit(UnitDocument Document, BoardLocation Location, HexIndex Hex, bool Shown, string Name);

/// <summary>A part of the counter layer: a hex, or the layer's figure outlines; its markup and a 96-bit hash of it (the Counter Layer Design, section 3).</summary>
public sealed record UnitFragment(string Id, string Markup, string Hash);

/// <summary>
/// The <c>layer-units</c> group, the units it placed, and why any were left out. The layer is kept as its parts in paint order (decision L4):
/// <see cref="Open"/>, the root's start tag with the layer's attributes, then <see cref="Fragments"/>. A host that sends only what changed reads
/// the parts; <see cref="Svg"/>, the layer whole, is made from them the first time it is asked for.
/// </summary>
public sealed record UnitOverlay(string SetId, IReadOnlyList<PlacedUnit> Units, IReadOnlyList<string> Diagnostics,
    IReadOnlyList<RenderWarning> Warnings, double FaceSize, string Open, IReadOnlyList<UnitFragment> Fragments)
{
    public const string EmptySvg = "<g id=\"layer-units\"></g>";

    /// <summary>
    /// The figure sprite the layer's references find (option C2; <see cref="UnitFigureSprite"/>), which the viewport must hold before the layer;
    /// null for a layer of whole counters.
    /// </summary>
    public string? Figures
    {
        get; init;
    }

    // The layer whole, kept with the parts it was made from, so a copy made with other parts (`with`) makes its own.
    private (string Open, IReadOnlyList<UnitFragment> Of, string Svg)? whole;

    /// <summary>The layer whole, as one SVG group.</summary>
    public string Svg
    {
        get
        {
            if (whole is not { } made || !ReferenceEquals(made.Of, Fragments) || !ReferenceEquals(made.Open, Open))
            {
                made = (Open, Fragments, string.Concat([Open, .. Fragments.Select(fragment => fragment.Markup), "</g>"]));
                whole = made;
            }

            return made.Svg;
        }
    }

    public IEnumerable<PlacedUnit> At(HexIndex hex) => Units.Where(unit => unit.Hex == hex);
}

/// <summary>
/// Builds the unit overlay (Unit Display Design, sections 7.1 and 7.2): every tier is rendered once, so the viewport
/// switches tiers by zoom without asking the server again. Stacks are clusters of up to three faces side by side, then
/// a second row, with a count badge beyond six; order follows <c>stackOrder</c>, then id. A location that is not ground
/// level gets a level tab. The output never touches the board layers.
/// </summary>
public static class UnitOverlayBuilder
{
    public const int MaxShown = 6;

    private static readonly DetailTier[] Tiers = [DetailTier.Far, DetailTier.Mid, DetailTier.Near];

    /// <summary>
    /// The layer of the units. With a <paramref name="cache"/>, a hex drawn before with the same inputs is taken from it rather than drawn again
    /// (option E); the layer is the same either way. The counters share their figure outlines, which the layer writes once at its end (C1).
    /// <paramref name="lead"/> gives the text before each counter's name and title, such as its hex and the unit's own name (decision L6).
    /// </summary>
    public static UnitOverlay Build(UnitMapTarget target, string setId, IEnumerable<UnitDocument> units, UnitRenderer renderer, UnitFragmentCache? cache = null,
        Func<PlacedUnit, string>? lead = null) =>
        Build(target, setId, units, renderer, cache, shared: true, lead);

    /// <summary>
    /// The layer, with the counters sharing their definitions (<paramref name="shared"/>) or each drawn whole as a standalone counter is; the
    /// equivalence oracle compares the two (the Counter Layer Design, decision L1 and section 11).
    /// </summary>
    internal static UnitOverlay Build(UnitMapTarget target, string setId, IEnumerable<UnitDocument> units, UnitRenderer renderer, UnitFragmentCache? cache,
        bool shared, Func<PlacedUnit, string>? lead = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(setId);
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(renderer);
        var diagnostics = new List<string>();
        var warnings = new List<RenderWarning>();
        var located = new List<(UnitDocument Document, BoardLocation Location, HexIndex Hex)>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in units)
        {
            if (!ids.Add(unit.Id))
            {
                diagnostics.Add($"The id '{unit.Id}' is used twice; the second unit is not drawn.");
                continue;
            }

            if (!BoardLocation.TryParse(unit.Location, out var location))
            {
                diagnostics.Add($"The unit '{unit.Id}' has no hex location.");
                continue;
            }

            if (target.Locate(location) is not { } hex)
            {
                diagnostics.Add($"The unit '{unit.Id}' is at {location}, which is not on {target.Ref}.");
                continue;
            }

            located.Add((unit, location, hex));
        }

        var hexHeight = target.Geometry.HexHeight;
        var faceSizes = located.Select(unit => renderer.FaceSize(unit.Document, DetailTier.Near, hexHeight)).ToArray();
        var reference = faceSizes.Length == 0 ? hexHeight * UnitRenderer.DefaultFaceSize : faceSizes.Min();
        var root = new SvgWriter();
        root.Start("g", ("id", "layer-units"), ("data-face-size", SvgValue.Number(reference)), ("data-unit-set", setId));
        root.End();
        var open = root.ToString()[..^"</g>".Length];
        var fragments = new List<UnitFragment>();
        var placed = new List<PlacedUnit>();
        var outlines = new List<(string Id, string Path)>();
        var held = new HashSet<string>(StringComparer.Ordinal);
        foreach (var hexGroup in located.GroupBy(unit => unit.Hex).OrderBy(group => group.Key.Column).ThenBy(group => group.Key.Row))
        {
            // A hex is a fragment of the layer (the Counter Layer Design, section 7.1): what it draws depends on its own hex and members alone.
            var levels = hexGroup.GroupBy(unit => unit.Location.Level).OrderBy(group => group.Key)
                .Select(group => new StackLevel(group.Key, [.. group.OrderBy(unit => unit.Document.StackOrder).ThenBy(unit => unit.Document.Id, StringComparer.Ordinal)
                    .Select(unit => new StackMember(unit.Document, unit.Location, null))]))
                .ToArray();
            foreach (var level in levels)
            {
                var suffix = LevelSuffix(level.Level);
                for (var index = 0; index < level.Members.Length; index++)
                {
                    var member = level.Members[index];
                    var bare = new PlacedUnit(member.Document, member.Location, hexGroup.Key, index < MaxShown,
                        UnitLabels.AccessibleName(member.Document, renderer.Vocabulary) + suffix);
                    var said = lead?.Invoke(bare);
                    placed.Add(said is null ? bare : bare with { Name = said + bare.Name });
                    level.Members[index] = member with { Lead = said };
                }
            }

            var center = target.Geometry.CenterDot(hexGroup.Key);
            var key = cache is null ? null : Key(hexGroup.Key, center, hexHeight, levels, renderer, shared);
            if (key is null || !cache!.TryGet(key, out var fragment))
            {
                var hexSvg = new SvgWriter();
                var hexWarnings = new List<RenderWarning>();
                var context = shared ? new UnitLayerContext() : null;
                WriteHex(hexSvg, hexGroup.Key, center, hexHeight, levels, renderer, hexWarnings, context);
                var markup = hexSvg.ToString();
                fragment = new UnitFragmentCache.Fragment(markup, [.. hexWarnings], [.. context?.Outlines ?? []], UnitLayerContext.Hash96(markup));
                if (key is not null)
                {
                    cache!.Add(key, fragment);
                }
            }

            fragments.Add(new UnitFragment(HexFragmentId(hexGroup.Key), fragment.Markup, fragment.Hash));
            warnings.AddRange(fragment.Warnings);
            Hold(outlines, held, fragment.Outlines);
        }

        // The figure outlines the counters refer to are the viewport's sprite (option C2), which holds every outline of the renderer's figure art.
        // An outline it does not hold is written in the layer, each once (C1): a reference may find an element later in the document, so the
        // definitions come after the counters, and a hex drawn earlier contributes its outlines as a hex drawn now does.
        var sprite = shared ? UnitFigureSprite.For(renderer.Figures) : null;
        var missing = outlines.Where(outline => sprite?.Ids.Contains(outline.Id) != true).ToArray();
        if (missing.Length > 0)
        {
            var definitions = new SvgWriter();
            definitions.Start("defs", ("id", FiguresFragmentId), ("data-figures", "true"));
            foreach (var (id, path) in missing)
            {
                definitions.Empty("path", ("id", id), ("d", SvgValue.Trusted(path)));
            }

            definitions.End();
            var markup = definitions.ToString();
            fragments.Add(new UnitFragment(FiguresFragmentId, markup, UnitLayerContext.Hash96(markup)));
        }

        return new UnitOverlay(setId, placed, diagnostics, warnings, reference, open, fragments)
        {
            Figures = sprite?.Markup,
        };
    }

    /// <summary>The id of the fragment that holds the layer's figure outlines (option C1), after the hexes.</summary>
    public const string FiguresFragmentId = "units-figures";

    /// <summary>The id of a hex's fragment: <c>units-{column}-{row}</c> of the composed map or board (the Counter Layer Design, section 3).</summary>
    public static string HexFragmentId(HexIndex hex) => string.Create(CultureInfo.InvariantCulture, $"units-{hex.Column}-{hex.Row}");

    /// <summary>One level of a hex's stack, its members in drawing order.</summary>
    private sealed record StackLevel(int Level, StackMember[] Members);

    /// <summary>A member of a stack: its document, its location, and the text before its name (decision L6).</summary>
    private readonly record struct StackMember(UnitDocument Document, BoardLocation Location, string? Lead);

    private static void Hold(List<(string Id, string Path)> outlines, HashSet<string> held, IEnumerable<(string Id, string Path)> more)
    {
        foreach (var outline in more)
        {
            if (held.Add(outline.Id))
            {
                outlines.Add(outline);
            }
        }
    }

    /// <summary>Everything <see cref="WriteHex"/> reads for a hex, as the fragment cache's key.</summary>
    private static string Key(HexIndex hex, PixelPoint center, double hexHeight, StackLevel[] levels, UnitRenderer renderer, bool shared)
    {
        var key = new System.Text.StringBuilder(256 * levels.Sum(level => level.Members.Length));
        key.Append(shared ? 'S' : 'W');
        UnitFragmentKey.Hex(key, renderer, hex.Column, hex.Row, center.X, center.Y, hexHeight);
        foreach (var level in levels)
        {
            UnitFragmentKey.Level(key, level.Level, level.Members.Length);
            foreach (var member in level.Members)
            {
                UnitFragmentKey.Lead(key, member.Lead);
                UnitFragmentKey.Document(key, member.Document);
            }
        }

        return key.ToString();
    }

    private static string? LevelSuffix(int level) => level == 0 ? null : $", level {level.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>A hex's stacks, every level with its count badge and level tab: one fragment of the layer.</summary>
    private static void WriteHex(SvgWriter svg, HexIndex hex, PixelPoint center, double hexHeight, StackLevel[] levels, UnitRenderer renderer,
        List<RenderWarning> warnings, UnitLayerContext? layer)
    {
        // The hex's own group, by which a patch replaces or removes it (option D; the Counter Layer Design, section 6).
        svg.Start("g", ("id", HexFragmentId(hex)), ("data-hex", $"{hex.Column},{hex.Row}"));
        for (var levelIndex = 0; levelIndex < levels.Length; levelIndex++)
        {
            var (level, members) = (levels[levelIndex].Level, levels[levelIndex].Members);
            var face = members.Max(member => renderer.FaceSize(member.Document, DetailTier.Near, hexHeight));
            var offsetY = (levelIndex - ((levels.Length - 1) / 2.0)) * face * 0.7;
            var shown = Math.Min(members.Length, MaxShown);
            var suffix = LevelSuffix(level);
            svg.Start("g", ("data-stack", $"{hex.Column},{hex.Row},{level.ToString(CultureInfo.InvariantCulture)}"));
            var (top, left) = (double.MaxValue, double.MaxValue);
            for (var index = 0; index < shown; index++)
            {
                var (dx, dy) = Offset(index, shown, face);
                var x = center.X + dx;
                var y = center.Y + offsetY + dy;
                top = Math.Min(top, y - (face / 2));
                left = Math.Min(left, x - (face / 2));
                renderer.WriteUnit(svg, members[index].Document, x, y, hexHeight, Tiers, null, suffix, warnings, layer, members[index].Lead);
            }

            if (members.Length > MaxShown)
            {
                var more = "+" + (members.Length - MaxShown).ToString(CultureInfo.InvariantCulture);
                var size = face * 0.34;
                var right = center.X + Offset(Math.Min(2, shown - 1), shown, face).Dx + (face / 2);
                svg.Start("g", ("data-stack-count", more));
                svg.Empty("rect", ("x", SvgValue.Number(right - (size * 0.6))), ("y", SvgValue.Number(top - (size * 0.6))), ("width", SvgValue.Number(size * 1.4)),
                    ("height", SvgValue.Number(size)), ("rx", SvgValue.Number(size * 0.3)), ("fill", "#17212b"));
                Text(svg, right + (size * 0.1), top - (size * 0.6) + (size * 0.72), size * 0.7, more, "#ffffff");
                svg.End();
            }

            if (level != 0)
            {
                var tab = face * 0.3;
                var label = level < 0 ? "C" : "L" + level.ToString(CultureInfo.InvariantCulture);
                svg.Start("g", ("data-level", level.ToString(CultureInfo.InvariantCulture)));
                svg.Empty("rect", ("x", SvgValue.Number(left)), ("y", SvgValue.Number(top - tab)), ("width", SvgValue.Number(tab * 1.6)),
                    ("height", SvgValue.Number(tab)), ("fill", "#17212b"));
                Text(svg, left + (tab * 0.8), top - (tab * 0.25), tab * 0.72, label, "#ffffff");
                svg.End();
            }

            svg.End();
        }

        svg.End();
    }

    /// <summary>The offset of a stack member: overlapping columns of three, a second row below.</summary>
    internal static (double Dx, double Dy) Offset(int index, int count, double face)
    {
        var step = face * 0.62;
        var inRow = Math.Min(count, 3);
        var row = index / 3;
        var column = index % 3;
        var rowCount = row == 0 ? inRow : count - 3;
        var rows = count > 3 ? 2 : 1;
        var dx = (column - ((rowCount - 1) / 2.0)) * step;
        var dy = rows == 1 ? 0 : (row - 0.5) * step;
        return (dx, dy);
    }

    private static void Text(SvgWriter svg, double x, double y, double size, string text, string color)
    {
        svg.Start("text", ("x", SvgValue.Number(x)), ("y", SvgValue.Number(y)), ("font-size", SvgValue.Number(size)), ("font-family", "sans-serif"),
            ("font-weight", "bold"), ("text-anchor", "middle"), ("fill", color), ("pointer-events", "none"));
        svg.Text(text);
        svg.End();
    }
}

/// <summary>A standalone SVG document of one unit for the Unit Lab and golden tests.</summary>
public static class UnitPreview
{
    /// <summary>The standard board's hex height, so previews match the map.</summary>
    public const double HexHeight = 64.5;

    public static string Document(UnitRenderer renderer, UnitDocument document, IReadOnlyList<DetailTier> tiers, string? face, double scale,
        List<RenderWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(tiers);
        var size = tiers.Max(tier => renderer.FaceSize(document, tier, HexHeight));
        var extent = size * 2.2;
        var svg = new SvgWriter();
        svg.Start("svg", ("xmlns", "http://www.w3.org/2000/svg"), ("viewBox", $"0 0 {SvgValue.Number(extent)} {SvgValue.Number(extent)}"),
            ("width", SvgValue.Number(extent * scale)), ("height", SvgValue.Number(extent * scale)));
        renderer.WriteUnit(svg, document, extent / 2, extent * 0.44, HexHeight, tiers, face, null, warnings);
        svg.End();
        return svg.ToString();
    }
}
