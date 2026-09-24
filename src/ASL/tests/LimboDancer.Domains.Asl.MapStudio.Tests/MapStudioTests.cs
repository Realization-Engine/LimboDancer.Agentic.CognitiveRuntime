using System.Net;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class MapServiceTests : IDisposable
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "asl-maps-" + Guid.NewGuid().ToString("N"));
    private readonly MapService service = new(new StudioOptions { CacheRoot = Path.Combine(Root, "cache"), BoardsRoot = Path.Combine(Root, "boards") }, new FakeVaslMapSource());

    private static readonly BoardRef Board02 = BoardRef.Parse("bd02");
    private static readonly BoardRef Board03 = BoardRef.Parse("bd03");

    [Fact]
    public void SavedMapsBuildAsVaslDoesAndLocateEachBoardsHexes()
    {
        var outcome = service.Save("Two boards", [new BoardPlacement(Board02), new BoardPlacement(Board03, 1, 0, true, ["WoodsToOpenGround"])]);
        var map = Assert.IsType<BoardRef>(outcome.Ref);
        Assert.Equal("map-two-boards", map.Value);
        Assert.Equal("{\"formatVersion\":\"1\",\"name\":\"Two boards\",\"placements\":[\"02@0,0\",\"03@1,0/r[WoodsToOpenGround]\"]}",
            File.ReadAllText(Path.Combine(service.MapsRoot, "two-boards.json")));

        var definition = Assert.Single(service.List());
        Assert.Equal("02@0,0 03@1,0/r[WoodsToOpenGround]", definition.PlacementText);

        var board = Assert.IsType<StudioBoard>(service.Load(map).Board);
        Assert.Equal(BoardStatus.Ingested, board.Status);
        Assert.False(board.HasStyled);
        var composition = Assert.IsType<StudioMap>(board.Composition);
        Assert.Equal((5, 2), (composition.Map.Geometry.WidthInHexes, composition.Map.Geometry.HeightInHexes));

        // The shared column belongs to the board placed later; its location names that board.
        var shared = Assert.IsType<HexIndex>(composition.Map.Locate(Board02, HexName.Parse("C1")));
        Assert.StartsWith("bd03:", board.LocationOf(board.Facts[shared]), StringComparison.Ordinal);
        Assert.Equal("bd02:A1:0", board.LocationOf(board.Facts[new HexIndex(0, 0)]));

        // The version changes when the placements do (ASL-MAP-072), and not otherwise.
        Assert.Equal(board.Version, service.Load(map).Board!.Version);
        service.Save("Two boards", [new BoardPlacement(Board02), new BoardPlacement(Board03, 1, 0)]);
        Assert.NotEqual(board.Version, service.Load(map).Board!.Version);

        Assert.True(service.Delete(map));
        Assert.Empty(service.List());
    }

    [Fact]
    public void MapsVaslCannotBuildAreNotSaved()
    {
        var wide = Enumerable.Range(0, 4).Select(column => new BoardPlacement(Board02, column, 0)).ToArray();
        Assert.Equal("VASL-MAP-003", Assert.Single(service.Check(wide).Diagnostics).Code);
        Assert.Null(service.Save("Too wide", wide).Ref);
        Assert.Equal("VASL-MAP-005", Assert.Single(service.Check([new BoardPlacement(BoardRef.Parse("bd99"))]).Diagnostics).Code);
        Assert.Equal("STUDIO-MAP-001", Assert.Single(service.Save("---", [new BoardPlacement(Board02)]).Diagnostics).Code);
        Assert.Empty(service.List());
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}

public sealed class MapPageTests(StudioFactory factory) : IClassFixture<StudioFactory>
{
    [Fact]
    public async Task SavedMapsAreListedViewedAndRendered()
    {
        var service = factory.Services.GetRequiredService<MapService>();
        var map = service.Save("Viewed map", [new BoardPlacement(BoardRef.Parse("bd02")), new BoardPlacement(BoardRef.Parse("bd03"), 1, 0)]).Ref!;
        var version = service.Load(map).Board!.Version;

        using var client = factory.CreateClient();
        var library = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        Assert.Contains($"href=\"boards/{map.Value}\"", library, StringComparison.Ordinal);

        var viewer = await client.GetStringAsync(new Uri($"/boards/{map.Value}", UriKind.Relative));
        Assert.Contains("Viewed map", viewer, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"styled\"", viewer, StringComparison.Ordinal);
        Assert.Contains("Edit map", viewer, StringComparison.Ordinal);

        using var layer = await client.GetAsync(new Uri($"/render/{map.Value}/{version}/exact/exact-terrain.svg", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, layer.StatusCode);
        using var stale = await client.GetAsync(new Uri($"/render/{map.Value}/{new string('0', 64)}/exact/exact-terrain.svg", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, stale.StatusCode);
    }

    [Fact]
    public async Task TheComposerPrefillsFromPlacementText()
    {
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(new Uri("/maps?placements=" + Uri.EscapeDataString("02@0,0[NoWhiteHexIDs] 03@1,0/r[NoWhiteHexIDs]"), UriKind.Relative));
        Assert.Contains("Scenario-specific rules", html, StringComparison.Ordinal);
        Assert.Contains("02@0,0[NoWhiteHexIDs] 03@1,0/r[NoWhiteHexIDs]", html, StringComparison.Ordinal);
        Assert.Contains("no LOS effect", html, StringComparison.Ordinal);
    }
}
