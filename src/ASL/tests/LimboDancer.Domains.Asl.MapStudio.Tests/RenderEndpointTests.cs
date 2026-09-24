using System.Net;
using LimboDancer.Domains.Asl.MapStudio.Components;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Rendering.Tests;
using LimboDancer.Domains.Asl.Maps.Vasl;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The synthetic catalog stands in for VASL's, so authored boards work without a checkout.</summary>
internal sealed class FakeCatalogSource : ICatalogSource
{
    public const string Hash = "synthetic-catalog";

    public (LimboDancer.Domains.Asl.Maps.Terrain.TerrainCatalog Catalog, string Hash)? Catalog() => (SyntheticBoard.Catalog, Hash);
}

/// <summary>
/// Serves the synthetic board, and routes other authored boards to the real authored board service, so the Studio
/// runs without a VASL checkout.
/// </summary>
internal sealed class FakeBoardProvider(AuthoredBoardService authored) : IBoardProvider
{
    public const string Version = "0123456789abcdef0123456789abcdef01234567";

    public static readonly StudioBoard Board = new(
        BoardRef.Parse("ab-synthetic"),
        Version,
        "Synthetic 3 by 2 board",
        BoardStatus.Verified,
        SyntheticBoard.Input(),
        SyntheticBoard.Catalog,
        new F1Result(F1Status.Pass, "Synthetic."),
        new F2Result([], []),
        Provenance: null,
        Diagnostics: []);

    public static readonly string Catalog = new('c', 40);
    public static readonly string Board02LosData = new('2', 40);
    public static readonly string Board02Metadata = new('3', 40);

    public string? SourceDescription => "Synthetic test source";

    public string? CatalogBlob => Catalog;

    public IReadOnlyList<BoardListing> List() =>
    [
        new BoardListing(Board.Ref, Board.Title),
        new BoardListing(BoardRef.Parse("bd02"), "VASL board 02", BoardScope.InScope, null, Board02LosData, Board02Metadata),
        new BoardListing(BoardRef.Parse("bd1a"), "VASL board 1a", BoardScope.OutOfScope, "bd1a: 17 by 10 hexes is not a standard geomorphic board."),
    ];

    public BoardLoadResult Load(BoardRef board) =>
        board == Board.Ref ? new BoardLoadResult(Board, []) : board.Kind == BoardRefKind.Authored ? authored.Load(board) : new BoardLoadResult(null, []);

    public BoardLoadResult? Cached(BoardRef board) => board == Board.Ref ? Load(board) : null;

    public StudioBoard? LoadVersion(BoardRef board, string version) =>
        board == Board.Ref ? (Board.Version == version ? Board : null) : board.Kind == BoardRefKind.Authored ? authored.LoadVersion(board, version) : null;
}

/// <summary>
/// The Studio with the fake provider, a fake batch, and a temporary cache seeded with one saved report, so no test
/// touches VASL or the user's real cache.
/// </summary>
public sealed class StudioFactory : WebApplicationFactory<App>
{
    public string CacheRoot { get; } = Path.Combine(Path.GetTempPath(), "asl-mapstudio-tests-" + Guid.NewGuid().ToString("N"));

    public string SeededReportId { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var options = new StudioOptions { CacheRoot = CacheRoot, BoardsRoot = Path.Combine(CacheRoot, "boards") };
        SeededReportId = new FidelityReportStore(options).Save(FidelityTestData.Report(new DateTimeOffset(2026, 9, 24, 8, 0, 0, TimeSpan.Zero)));
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<ICatalogSource, FakeCatalogSource>();
            services.AddSingleton<IBoardProvider, FakeBoardProvider>();
            services.AddSingleton<IFidelityBatch>(new FakeFidelityBatch());
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(CacheRoot))
        {
            Directory.Delete(CacheRoot, recursive: true);
        }
    }
}

public sealed class RenderEndpointTests(StudioFactory factory) : IClassFixture<StudioFactory>
{
    private const string Base = "/render/ab-synthetic/" + FakeBoardProvider.Version;

    [Theory]
    [InlineData("exact", "exact-terrain")]
    [InlineData("exact", "grid")]
    [InlineData("hexfacts", "hexfacts")]
    [InlineData("hexfacts", "legend")]
    public async Task LayersAreServedAsImmutableSvg(string view, string layer)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri($"{Base}/{view}/{layer}.svg", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType!.MediaType);
        Assert.NotNull(response.Headers.ETag);
        Assert.Contains("immutable", response.Headers.CacheControl!.ToString(), StringComparison.Ordinal);
        Assert.True(BoardRenderer.TryParseView(view, out var boardView));
        Assert.Equal(BoardRenderer.Fragment(FakeBoardProvider.Board.Render, boardView, layer), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DocumentIsServedWhole()
    {
        using var client = factory.CreateClient();
        var body = await client.GetStringAsync(new Uri($"{Base}/exact/document.svg", UriKind.Relative));
        Assert.Equal(BoardRenderer.Document(FakeBoardProvider.Board.Render, BoardView.Exact), body);
    }

    [Fact]
    public async Task MatchingEntityTagReturnsNotModified()
    {
        using var client = factory.CreateClient();
        var uri = new Uri($"{Base}/exact/exact-terrain.svg", UriKind.Relative);
        using var first = await client.GetAsync(uri);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.IfNoneMatch.Add(first.Headers.ETag!);
        using var second = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
    }

    [Fact]
    public async Task TraceModeChangesTheHexFactsLayer()
    {
        using var client = factory.CreateClient();
        var plain = await client.GetStringAsync(new Uri($"{Base}/hexfacts/hexfacts.svg", UriKind.Relative));
        var traced = await client.GetStringAsync(new Uri($"{Base}/hexfacts/hexfacts.svg?trace=true", UriKind.Relative));
        Assert.NotEqual(plain, traced);
        Assert.Equal(BoardRenderer.Fragment(FakeBoardProvider.Board.Render, BoardView.HexFacts, "hexfacts", traceMode: true), traced);
    }

    [Theory]
    [InlineData("/render/ab-synthetic/stale/exact/grid.svg")]
    [InlineData("/render/ab-missing/" + FakeBoardProvider.Version + "/exact/grid.svg")]
    [InlineData("/render/not a board/" + FakeBoardProvider.Version + "/exact/grid.svg")]
    [InlineData(Base + "/styled/grid.svg")]
    [InlineData(Base + "/exact/hexfacts.svg")]
    [InlineData(Base + "/exact/grid.png")]
    public async Task UnknownRequestsAreNotFound(string path)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LibraryListsTheProviderBoards()
    {
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        Assert.Contains("Synthetic test source", html, StringComparison.Ordinal);
        Assert.Contains("href=\"boards/ab-synthetic\"", html, StringComparison.Ordinal);
        Assert.Contains("2 boards in scope", html, StringComparison.Ordinal);
        Assert.Contains(">2 verified<", html, StringComparison.Ordinal);
        Assert.Contains("Verified (batch)", html, StringComparison.Ordinal);
        Assert.Contains("Show out-of-scope boards (1)", html, StringComparison.Ordinal);
        Assert.DoesNotContain("VASL board 1a", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ViewerPrerendersTheBoardAndItsProvenance()
    {
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(new Uri("/boards/ab-synthetic", UriKind.Relative));
        Assert.Contains("Synthetic 3 by 2 board", html, StringComparison.Ordinal);
        Assert.Contains("Verified", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<image", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ViewerReportsUnknownBoards()
    {
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(new Uri("/boards/ab-missing", UriKind.Relative));
        Assert.Contains("ab-missing could not be loaded", html, StringComparison.Ordinal);
    }
}
