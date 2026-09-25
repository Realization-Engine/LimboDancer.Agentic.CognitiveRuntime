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

/// <summary>The <c>layer-units</c> group, the units it placed, and why any were left out.</summary>
public sealed record UnitOverlay(string Svg, string SetId, IReadOnlyList<PlacedUnit> Units, IReadOnlyList<string> Diagnostics,
    IReadOnlyList<RenderWarning> Warnings, double FaceSize)
{
    public const string EmptySvg = "<g id=\"layer-units\"></g>";

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

    public static UnitOverlay Build(UnitMapTarget target, string setId, IEnumerable<UnitDocument> units, UnitRenderer renderer)
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
        var svg = new SvgWriter();
        svg.Start("g", ("id", "layer-units"), ("data-face-size", SvgWriter.Number(reference)), ("data-unit-set", setId));
        var placed = new List<PlacedUnit>();
        foreach (var hexGroup in located.GroupBy(unit => unit.Hex).OrderBy(group => group.Key.Column).ThenBy(group => group.Key.Row))
        {
            var center = target.Geometry.CenterDot(hexGroup.Key);
            var levels = hexGroup.GroupBy(unit => unit.Location.Level).OrderBy(group => group.Key).ToArray();
            for (var levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                var level = levels[levelIndex].Key;
                var members = levels[levelIndex].OrderBy(unit => unit.Document.StackOrder).ThenBy(unit => unit.Document.Id, StringComparer.Ordinal).ToArray();
                var face = members.Max(member => renderer.FaceSize(member.Document, DetailTier.Near, hexHeight));
                var offsetY = (levelIndex - ((levels.Length - 1) / 2.0)) * face * 0.7;
                var shown = Math.Min(members.Length, MaxShown);
                var suffix = level == 0 ? null : $", level {level.ToString(CultureInfo.InvariantCulture)}";
                svg.Start("g", ("data-stack", $"{hexGroup.Key.Column},{hexGroup.Key.Row},{level.ToString(CultureInfo.InvariantCulture)}"));
                var (top, left) = (double.MaxValue, double.MaxValue);
                for (var index = 0; index < members.Length; index++)
                {
                    var member = members[index];
                    var isShown = index < shown;
                    placed.Add(new PlacedUnit(member.Document, member.Location, hexGroup.Key, isShown,
                        UnitLabels.AccessibleName(member.Document, renderer.Vocabulary) + suffix));
                    if (!isShown)
                    {
                        continue;
                    }

                    var (dx, dy) = Offset(index, shown, face);
                    var x = center.X + dx;
                    var y = center.Y + offsetY + dy;
                    top = Math.Min(top, y - (face / 2));
                    left = Math.Min(left, x - (face / 2));
                    renderer.WriteUnit(svg, member.Document, x, y, hexHeight, Tiers, null, suffix, warnings);
                }

                if (members.Length > MaxShown)
                {
                    var more = "+" + (members.Length - MaxShown).ToString(CultureInfo.InvariantCulture);
                    var size = face * 0.34;
                    var right = center.X + Offset(Math.Min(2, shown - 1), shown, face).Dx + (face / 2);
                    svg.Start("g", ("data-stack-count", more));
                    svg.Empty("rect", ("x", SvgWriter.Number(right - (size * 0.6))), ("y", SvgWriter.Number(top - (size * 0.6))), ("width", SvgWriter.Number(size * 1.4)),
                        ("height", SvgWriter.Number(size)), ("rx", SvgWriter.Number(size * 0.3)), ("fill", "#17212b"));
                    Text(svg, right + (size * 0.1), top - (size * 0.6) + (size * 0.72), size * 0.7, more, "#ffffff");
                    svg.End();
                }

                if (level != 0)
                {
                    var tab = face * 0.3;
                    var label = level < 0 ? "C" : "L" + level.ToString(CultureInfo.InvariantCulture);
                    svg.Start("g", ("data-level", level.ToString(CultureInfo.InvariantCulture)));
                    svg.Empty("rect", ("x", SvgWriter.Number(left)), ("y", SvgWriter.Number(top - tab)), ("width", SvgWriter.Number(tab * 1.6)),
                        ("height", SvgWriter.Number(tab)), ("fill", "#17212b"));
                    Text(svg, left + (tab * 0.8), top - (tab * 0.25), tab * 0.72, label, "#ffffff");
                    svg.End();
                }

                svg.End();
            }
        }

        svg.End();
        return new UnitOverlay(svg.ToString(), setId, placed, diagnostics, warnings, reference);
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
        svg.Start("text", ("x", SvgWriter.Number(x)), ("y", SvgWriter.Number(y)), ("font-size", SvgWriter.Number(size)), ("font-family", "sans-serif"),
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
        svg.Start("svg", ("xmlns", "http://www.w3.org/2000/svg"), ("viewBox", $"0 0 {SvgWriter.Number(extent)} {SvgWriter.Number(extent)}"),
            ("width", SvgWriter.Number(extent * scale)), ("height", SvgWriter.Number(extent * scale)));
        renderer.WriteUnit(svg, document, extent / 2, extent * 0.44, HexHeight, tiers, face, null, warnings);
        svg.End();
        return svg.ToString();
    }
}
