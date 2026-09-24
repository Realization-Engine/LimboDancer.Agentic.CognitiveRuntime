using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Rendering;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

public sealed record DemoUnitPlacement(string PlacementId, string Location, string SideId, string Label, string FaceKey, int StackOrder);

public sealed record DemoUnitFixture(int SchemaVersion, string BoardRef, string FixtureId,
    IReadOnlyList<DemoUnitPlacement> Placements, string? ExpectedBoardVersion = null);

public sealed record DisplayedUnit(DemoUnitPlacement Placement, BoardLocation Location);

public sealed record DemoUnitOverlay(string Svg, string FixtureId, string BoardVersion, string ContentHash,
    IReadOnlyList<DisplayedUnit> Units, IReadOnlyList<string> Diagnostics);

/// <summary>Read-only, explicitly synthetic counters. Their positions are never game-state observations.</summary>
public static class DemoUnitOverlayBuilder
{
    private static readonly JsonSerializerOptions FixtureJsonOptions = new(JsonSerializerDefaults.Web);

    public static DemoUnitOverlay? Load(StudioBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.Ref.Value != "bd01")
        {
            return null;
        }

        using var stream = typeof(DemoUnitOverlayBuilder).Assembly.GetManifestResourceStream(
            "LimboDancer.Domains.Asl.MapStudio.DemoPlacements.bd01.json")
            ?? throw new InvalidOperationException("The bd01 demo fixture is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        var fixture = JsonSerializer.Deserialize<DemoUnitFixture>(bytes, FixtureJsonOptions)
            ?? throw new InvalidDataException("The bd01 demo fixture is empty.");
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        return Build(board.Ref, board.Version, board.Render.Grid.Geometry, fixture, hash);
    }

    public static DemoUnitOverlay Build(BoardRef board, string version, BoardGeometry geometry, DemoUnitFixture fixture, string contentHash)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(fixture.Placements);
        var diagnostics = new List<string>();
        var units = new List<DisplayedUnit>();
        if (fixture.SchemaVersion != 1 || fixture.BoardRef != board.Value ||
            (fixture.ExpectedBoardVersion is not null && fixture.ExpectedBoardVersion != version))
        {
            diagnostics.Add("The demo fixture schema, board, or pinned board version does not match the displayed board.");
            return new DemoUnitOverlay(EmptySvg(), fixture.FixtureId, version, contentHash, units, diagnostics);
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var placement in fixture.Placements)
        {
            if (placement is null)
            {
                diagnostics.Add("A demo placement is missing.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(placement.PlacementId) || !ids.Add(placement.PlacementId))
            {
                diagnostics.Add($"Duplicate or missing placement ID: {placement.PlacementId}.");
                continue;
            }

            if (!BoardLocation.TryParse(placement.Location, out var location) ||
                location.Board != board || location.Side is not null ||
                !geometry.TryGetIndex(location.Hex, out _) ||
                placement.SideId is not ("blue" or "red") || placement.FaceKey != "front" ||
                string.IsNullOrWhiteSpace(placement.Label) || placement.Label.Length > 12)
            {
                diagnostics.Add($"Invalid demo placement {placement.PlacementId}: location, side, face, or label.");
                continue;
            }

            units.Add(new DisplayedUnit(placement, location));
        }

        units = [.. units.OrderBy(x => x.Location.Hex.ToString(), StringComparer.Ordinal)
            .ThenBy(x => x.Placement.StackOrder).ThenBy(x => x.Placement.PlacementId, StringComparer.Ordinal)];
        return new DemoUnitOverlay(Render(geometry, units), fixture.FixtureId, version, contentHash, units, diagnostics);
    }

    private static string EmptySvg() => "<g id=\"layer-units\"></g>";

    private static string Render(BoardGeometry geometry, IReadOnlyList<DisplayedUnit> units)
    {
        var svg = new SvgWriter();
        svg.Start("g", ("id", "layer-units"));
        foreach (var stack in units.GroupBy(unit => unit.Location.Hex))
        {
            var center = geometry.CenterDot(geometry.IndexOf(stack.Key));
            var members = stack.ToArray();
            for (var i = 0; i < members.Length; i++)
            {
                var unit = members[i];
                var offsetX = members.Length == 1 ? 0 : members.Length == 2 ? (i * 20) - 10 : ((i % 3) - 1) * 19;
                var offsetY = members.Length < 4 ? 0 : (i / 3) * 19;
                var x = center.X + offsetX - 9;
                var y = center.Y + offsetY - 9;
                var label = $"Demo unit {unit.Placement.Label}, {unit.Placement.SideId}, {unit.Location}, {unit.Placement.FaceKey}";
                svg.Start("g", ("data-placement-id", unit.Placement.PlacementId), ("tabindex", "0"),
                    ("role", "button"), ("aria-label", label));
                svg.Start("title");
                svg.Text(label);
                svg.End();
                svg.Empty("rect", ("x", SvgWriter.Number(x)), ("y", SvgWriter.Number(y)),
                    ("width", "18"), ("height", "18"), ("rx", "2"),
                    ("fill", unit.Placement.SideId == "blue" ? "#d6e4fa" : "#f5d1c9"),
                    ("stroke", "#27313a"), ("stroke-width", "1"));
                svg.Start("text", ("x", SvgWriter.Number(x + 9)), ("y", SvgWriter.Number(y + 12)),
                    ("text-anchor", "middle"), ("font-size", "10"), ("font-family", "sans-serif"),
                    ("fill", "#17212b"), ("pointer-events", "none"));
                svg.Text(unit.Placement.Label);
                svg.End();
                svg.End();
            }
        }

        svg.End();
        return svg.ToString();
    }
}
