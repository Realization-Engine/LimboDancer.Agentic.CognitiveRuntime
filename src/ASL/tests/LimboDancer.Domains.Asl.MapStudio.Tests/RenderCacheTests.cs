using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Rendering;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The render cache keeps one version of each board, as UTF-8 bytes with a gzip copy (option J of the SVG memory analysis).</summary>
public sealed class RenderCacheTests
{
    private static readonly StudioBoard Board = FakeBoardProvider.Board;

    [Fact]
    public void ANewVersionDropsTheOldOnes()
    {
        var cache = new RenderCache();
        cache.Get(Board, BoardView.Exact, "grid", trace: false);
        cache.Get(Board, BoardView.Exact, "document", trace: false);
        Assert.Equal(2, cache.Count);

        cache.Get(Board with { Version = new string('1', 40) }, BoardView.Exact, "grid", trace: false);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void EachBoardKeepsItsOwnVersion()
    {
        var cache = new RenderCache();
        cache.Get(Board, BoardView.Exact, "grid", trace: false);
        cache.Get(Board with { Ref = BoardRef.Parse("ab-other") }, BoardView.Exact, "grid", trace: false);
        cache.Get(Board, BoardView.Exact, "legend", trace: false);
        Assert.Equal(3, cache.Count);
    }

    [Fact]
    public void TheEntryIsTheRenderAsUtf8WithItsHashAndAGzipCopy()
    {
        var rendered = new RenderCache().Get(Board, BoardView.Exact, "exact-terrain", trace: false)!;
        var expected = Encoding.UTF8.GetBytes(BoardRenderer.Fragment(Board.Render, BoardView.Exact, "exact-terrain"));
        Assert.Equal(expected, rendered.Svg);
        Assert.Equal("\"" + Convert.ToHexStringLower(SHA256.HashData(expected)) + "\"", rendered.ETag);

        using var unzip = new GZipStream(new MemoryStream(rendered.Gzip), CompressionMode.Decompress);
        using var plain = new MemoryStream();
        unzip.CopyTo(plain);
        Assert.Equal(expected, plain.ToArray());
    }
}
