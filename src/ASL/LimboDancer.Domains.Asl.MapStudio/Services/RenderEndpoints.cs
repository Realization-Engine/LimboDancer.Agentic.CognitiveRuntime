using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Rendering;
using Microsoft.Net.Http.Headers;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>A rendered SVG as UTF-8 bytes, a gzip copy of them, and their content-hash entity tag.</summary>
public sealed record RenderedSvg(byte[] Svg, byte[] Gzip, string ETag);

/// <summary>
/// Content-addressed cache of rendered documents and layer fragments. It keeps one version of each board: the version last asked
/// for. An authoring session makes a new version with every edit, and without this the old ones stayed for the life of the
/// process (option J of the SVG memory analysis). Entries are UTF-8 bytes, half the size of a string, served without re-encoding.
/// </summary>
public sealed class RenderCache(ILogger<RenderCache>? logger = null)
{
    private readonly ConcurrentDictionary<(string Board, string Version, BoardView View, string Layer, bool Trace), RenderedSvg> cache = new();
    private readonly ConcurrentDictionary<string, string> versions = new(StringComparer.Ordinal);

    /// <summary>The number of rendered layers and documents held.</summary>
    public int Count => cache.Count;

    /// <summary>The rendered layer or document, or null when the board has no input for the view.</summary>
    public RenderedSvg? Get(StudioBoard board, BoardView view, string layer, bool trace)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.InputFor(view) is not { } input)
        {
            return null;
        }

        if (versions.TryGetValue(board.Ref.Value, out var held) && held != board.Version)
        {
            foreach (var key in cache.Keys.Where(key => key.Board == board.Ref.Value && key.Version != board.Version))
            {
                cache.TryRemove(key, out _);
            }
        }

        versions[board.Ref.Value] = board.Version;
        return cache.GetOrAdd((board.Ref.Value, board.Version, view, layer, trace), key =>
        {
            var timer = Stopwatch.StartNew();
            var svg = key.Layer == "document"
                ? BoardRenderer.Document(input, view, trace)
                : BoardRenderer.Fragment(input, view, key.Layer, trace);
            LoadMeasurements.Record(logger, board.Ref.Value, layer, timer.Elapsed.TotalMilliseconds);
            var bytes = Encoding.UTF8.GetBytes(svg);
            var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            return new RenderedSvg(bytes, Compressed(bytes), "\"" + hash + "\"");
        });
    }

    private static byte[] Compressed(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(bytes);
        }

        return output.ToArray();
    }
}

/// <summary>
/// Serves board layers and documents as SVG (Architecture and Rendering Design, section 4.4). URLs carry the board
/// version, so responses are immutable and cached by the browser.
/// </summary>
public static class RenderEndpoints
{
    public static void MapRenderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/render/{board}/{version}/{view}/{file}", Render);
        endpoints.MapGet("/fidelity/reports/{file}", Report);
    }

    /// <summary>A saved fidelity report as JSON, for download.</summary>
    private static IResult Report(string file, FidelityReportStore store)
    {
        var id = file.EndsWith(".json", StringComparison.Ordinal) ? file[..^5] : null;
        return id is not null && store.ReadJson(id) is { } json
            ? Results.Text(json, "application/json", Encoding.UTF8)
            : Results.NotFound();
    }

    private static IResult Render(string board, string version, string view, string file, bool? trace,
        HttpContext context, IBoardProvider provider, RenderCache cache)
    {
        if (!file.EndsWith(".svg", StringComparison.Ordinal) || !BoardRef.TryParse(board, out var boardRef)
            || !BoardRenderer.TryParseView(view, out var boardView))
        {
            return Results.NotFound();
        }

        var layer = file[..^4];
        if (layer != "document" && !BoardRenderer.Layers(boardView).Contains(layer))
        {
            return Results.NotFound();
        }

        if (provider.LoadVersion(boardRef, version) is not { } studioBoard || cache.Get(studioBoard, boardView, layer, trace ?? false) is not { } rendered)
        {
            return Results.NotFound();
        }

        context.Response.Headers.ETag = rendered.ETag;
        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        context.Response.Headers.Vary = "Accept-Encoding";
        if (context.Request.Headers.IfNoneMatch.ToString() == rendered.ETag)
        {
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        // The cache holds a gzip copy, so a compressed response costs nothing per request (option L of the SVG memory analysis).
        if (AcceptsGzip(context.Request))
        {
            context.Response.Headers.ContentEncoding = "gzip";
            return Results.Bytes(rendered.Gzip, "image/svg+xml; charset=utf-8");
        }

        return Results.Bytes(rendered.Svg, "image/svg+xml; charset=utf-8");
    }

    /// <summary>Whether the request accepts gzip: named, or <c>*</c>, with a quality above zero.</summary>
    private static bool AcceptsGzip(HttpRequest request) =>
        StringWithQualityHeaderValue.TryParseList(request.Headers.AcceptEncoding, out var codings)
        && codings.Any(coding => (coding.Value.Equals("gzip", StringComparison.OrdinalIgnoreCase) || coding.Value == "*") && coding.Quality is not 0);
}
