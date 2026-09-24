using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Rendering;
using Microsoft.Net.Http.Headers;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>A rendered SVG and its content-hash entity tag.</summary>
public sealed record RenderedSvg(string Svg, string ETag);

/// <summary>Content-addressed cache of rendered documents and layer fragments.</summary>
public sealed class RenderCache
{
    private readonly ConcurrentDictionary<(string Board, string Version, BoardView View, string Layer, bool Trace), RenderedSvg> cache = new();

    /// <summary>The rendered layer or document, or null when the board has no input for the view.</summary>
    public RenderedSvg? Get(StudioBoard board, BoardView view, string layer, bool trace)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.InputFor(view) is not { } input)
        {
            return null;
        }

        return cache.GetOrAdd((board.Ref.Value, board.Version, view, layer, trace), key =>
        {
            var svg = key.Layer == "document"
                ? BoardRenderer.Document(input, view, trace)
                : BoardRenderer.Fragment(input, view, key.Layer, trace);
            var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(svg)));
            return new RenderedSvg(svg, "\"" + hash + "\"");
        });
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
        if (context.Request.Headers.IfNoneMatch.ToString() == rendered.ETag)
        {
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        return Results.Text(rendered.Svg, "image/svg+xml", Encoding.UTF8);
    }
}
