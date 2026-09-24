using System.Net;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Rendering.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class AuthoringSessionTests : IDisposable
{
    private static readonly BoardGeometry Geometry = BoardGeometry.Standard(6, 4);
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "asl-authoring-" + Guid.NewGuid().ToString("N"));

    // Both roots are temporary, so no test writes into the repository's boards folder.
    private readonly StudioOptions options = new() { CacheRoot = Path.Combine(Root, "cache"), BoardsRoot = Path.Combine(Root, "boards") };

    [Fact]
    public void EditsBuildNewVersionsThatUndoAndRedoRestore()
    {
        var service = Service();
        var session = new AuthoringSession(service, SyntheticBoard.Catalog, Package(), draft: false);
        var original = session.Current.Version;

        var add = session.Execute(new AddFeature(new AreaTerrainFeature("woods", 0, Box(20, 20, 80, 70), 60)));
        Assert.True(add.Succeeded, add.Error);
        Assert.Equal(["woods"], add.Upserted.Select(feature => feature.Id));
        Assert.NotEqual(original, session.Current.Version);
        Assert.True(session.Dirty);
        Assert.Equal("add area terrain", session.UndoDescription);
        var edited = session.Current.Version;

        var undo = Assert.IsType<EditOutcome>(session.Undo());
        Assert.Equal([("styled-area", "f-woods")], undo.Removed);
        Assert.Equal(original, session.Current.Version);
        Assert.Equal("add area terrain", session.RedoDescription);

        Assert.IsType<EditOutcome>(session.Redo());
        Assert.Equal(edited, session.Current.Version);
        Assert.Null(session.RedoDescription);
    }

    [Fact]
    public void RefusedCommandsChangeNothing()
    {
        var session = new AuthoringSession(Service(), SyntheticBoard.Catalog, Package(), draft: false);
        var version = session.Current.Version;
        var outcome = session.Execute(new AddFeature(new AreaTerrainFeature("bad", 0, Box(0, 0, 10, 10), 42)));
        Assert.False(outcome.Succeeded);
        Assert.Contains("is not area terrain", outcome.Error, StringComparison.Ordinal);
        Assert.Equal(version, session.Current.Version);
        Assert.Null(session.UndoDescription);
        Assert.False(session.Dirty);
    }

    [Fact]
    public void SavedBoardsAndDraftsReloadAtTheSameVersion()
    {
        var service = Service();
        var session = new AuthoringSession(service, SyntheticBoard.Catalog, Package(), draft: false);
        session.Execute(new AddFeature(new BuildingFeature("house", 0, [BuildingKit.Centered(Geometry, new HexIndex(2, 1))], 42, "B1")));
        session.Save();
        Assert.False(session.Dirty);

        var draft = new AuthoringSession(service, SyntheticBoard.Catalog, Package("ab-derived"), draft: true);
        draft.Save();

        Assert.Equal([("ab-derived", true), ("ab-village", false)], service.List().Select(listing => (listing.Ref.Value, listing.IsDraft)));
        var reloaded = Assert.IsType<StudioBoard>(service.Load(BoardRef.Parse("ab-village")).Board);
        Assert.Equal(session.Current.Version, reloaded.Version);
        Assert.Equal(BoardStatus.AuthoredValid, reloaded.Status);
        Assert.True(Assert.IsType<StudioBoard>(service.Load(BoardRef.Parse("ab-derived")).Board).IsDraft);
    }

    [Fact]
    public void RecentVersionsStayLoadableForRenderUrls()
    {
        var service = Service();
        var session = new AuthoringSession(service, SyntheticBoard.Catalog, Package(), draft: false);
        var first = session.Current.Version;
        session.Execute(new SetStairway(new HexIndex(2, 1), true));
        Assert.NotNull(service.LoadVersion(session.Board, first));
        Assert.NotNull(service.LoadVersion(session.Board, session.Current.Version));
        Assert.Null(service.LoadVersion(session.Board, new string('0', 64)));
    }

    [Fact]
    public void HexsideClicksAddAndRemoveSpans()
    {
        var model = Package().Model;
        var side = new HexsideRef(new HexIndex(2, 1), HexsideDirection.South);
        var add = Assert.IsType<AddFeature>(EditorGestures.AddHexside(model, 72, side));
        var withWall = model with
        {
            Features = [add.Feature]
        };
        var other = new HexsideRef(new HexIndex(2, 1), HexsideDirection.North);
        var extend = Assert.IsType<ReplaceFeature>(EditorGestures.AddHexside(withWall, 72, other));
        Assert.Equal(2, ((HexsideTerrainFeature)extend.Feature).Spans.Count);
        Assert.IsType<RemoveFeature>(EditorGestures.AddHexside(withWall, 72, side));
    }

    [Fact]
    public void GesturesSnapAndBuildCenterlines()
    {
        var model = Package().Model;
        var points = EditorGestures.Snap(model, [(10.3, 10.6), (10.4, 10.6), (40.2, 12.9), (70.0, 30.1)], snap: false);
        Assert.Equal([FixedVector.FromPixels(10, 11), FixedVector.FromPixels(40, 13), FixedVector.FromPixels(70, 30)], points);
        var straight = EditorGestures.Centerline(points, curved: false);
        Assert.All(straight.Segments, segment => Assert.False(segment.IsCurve));
        var curved = EditorGestures.Centerline(points, curved: true);
        Assert.All(curved.Segments, segment => Assert.True(segment.IsCurve));
        Assert.Equal(points[^1], curved.Segments[^1].End);

        var moved = (AreaTerrainFeature)EditorGestures.Translate(new AreaTerrainFeature("a", 0, Box(0, 0, 10, 10), 60), 3, -2, Geometry);
        Assert.Equal(FixedVector.FromPixels(3, -2), moved.Shape.Rings[0][0]);
    }

    private AuthoredBoardService Service() => new(options, new FakeCatalogSource());

    private static BoardPackage Package(string board = "ab-village") =>
        new(BoardRef.Parse(board), "Village", FeatureModel.New(Geometry, FakeCatalogSource.Hash));

    private static FeatureShape Box(int x0, int y0, int x1, int y1) => FeatureShape.Rectangle(FixedVector.FromPixels(x0, y0), FixedVector.FromPixels(x1, y1));

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}

public sealed class AuthoringPageTests(StudioFactory factory) : IClassFixture<StudioFactory>
{
    [Fact]
    public async Task AuthoredBoardsRenderStyledLayersByVersion()
    {
        var service = factory.Services.GetRequiredService<AuthoredBoardService>();
        var geometry = BoardGeometry.Standard(6, 4);
        var package = new BoardPackage(BoardRef.Parse("ab-rendered"), "Rendered", FeatureModel.New(geometry, FakeCatalogSource.Hash) with
        {
            Features = [new AreaTerrainFeature("woods", 0, FeatureShape.Rectangle(FixedVector.FromPixels(10, 10), FixedVector.FromPixels(60, 60)), 60)],
        });
        service.Save(package, draft: false);
        var version = Assert.IsType<StudioBoard>(service.Load(package.Board).Board).Version;

        using var client = factory.CreateClient();
        var area = await client.GetStringAsync(new Uri($"/render/ab-rendered/{version}/styled/styled-area.svg", UriKind.Relative));
        Assert.Contains("id=\"f-woods\"", area, StringComparison.Ordinal);
        using var stale = await client.GetAsync(new Uri($"/render/ab-rendered/{new string('0', 64)}/styled/styled-area.svg", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, stale.StatusCode);

        var library = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        Assert.Contains("href=\"author/ab-rendered\"", library, StringComparison.Ordinal);

        var editor = await client.GetStringAsync(new Uri("/author/ab-rendered", UriKind.Relative));
        Assert.Contains("tool-palette", editor, StringComparison.Ordinal);
        Assert.Contains("value=\"Rendered\"", editor, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/author/new", "Start from")]
    [InlineData("/settings", "Boards folder")]
    [InlineData("/author/ab-nothing-here", "could not be opened")]
    [InlineData("/author/bd01", "not an authored board reference")]
    public async Task AuthoringPagesPrerender(string path, string expected)
    {
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(new Uri(path, UriKind.Relative));
        Assert.Contains(expected, html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Test Village", "test-village")]
    [InlineData("  Hill 621 / North  ", "hill-621-north")]
    [InlineData("---", "")]
    public void SlugsAreLowercaseWordsJoinedByHyphens(string name, string slug) =>
        Assert.Equal(slug, Components.Pages.NewBoard.Slug(name));
}
