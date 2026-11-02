using Microsoft.Net.Http.Headers;

namespace LimboDancer.Domains.Asl.MapStudio.Services.MapLab;

/// <summary>
/// Serves a Map Lab map's files at <c>lab-render/{key}/exact/{file}</c>, the shape of a board's render address, so the Studio's viewport loads it
/// as it loads a board's Exact view. The key is the whole map (<see cref="LabMapSpec.Key"/>), so every response is immutable.
/// </summary>
public static class LabMapEndpoints
{
    /// <summary>The source name the Lab's tiles are held under in the shared tile cache; one map at a time.</summary>
    public const string TileSource = "map-lab";

    public static void MapLabMapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/lab-render/{key}/exact/{file}", Render);
    }

    /// <summary>The address the viewport loads a map from, relative to the Studio's base.</summary>
    public static string BaseUrl(LabMapSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return "lab-render/" + spec.Key + "/exact";
    }

    private static IResult Render(string key, string file, HttpContext context, LabMapCache maps, TileCache tiles)
    {
        if (!LabMapSpec.TryReadKey(key, out var spec))
        {
            return Results.NotFound();
        }

        if (file.StartsWith("tile-", StringComparison.Ordinal))
        {
            if (!spec.Tiled || !TileCache.TryReadFileName(file, out var variant, out var level, out var column, out var row))
            {
                return Results.NotFound();
            }

            var map = maps.Map(spec);
            if (!LabMapRenderer.HasTile(map, level, column, row))
            {
                return Results.NotFound();
            }

            // Keyed by the whole spec: the two tiled techniques paint different tiles of one map.
            var tile = tiles.Get(TileSource, spec.Key, variant, level, column, row, () => LabMapRenderer.TilePng(map, level, column, row));
            return Immutable(context, tile.ETag, () => Results.Bytes(tile.Png, "image/png"));
        }

        if (maps.File(spec, file) is not { } rendered)
        {
            return Results.NotFound();
        }

        var contentType = file.EndsWith(".json", StringComparison.Ordinal) ? "application/json; charset=utf-8" : "image/svg+xml; charset=utf-8";
        context.Response.Headers.Vary = "Accept-Encoding";
        return Immutable(context, rendered.ETag, () =>
        {
            if (StringWithQualityHeaderValue.TryParseList(context.Request.Headers.AcceptEncoding, out var codings)
                && codings.Any(coding => coding.Value.Equals("gzip", StringComparison.OrdinalIgnoreCase) && coding.Quality is not 0))
            {
                context.Response.Headers.ContentEncoding = "gzip";
                return Results.Bytes(rendered.Gzip, contentType);
            }

            return Results.Bytes(rendered.Svg, contentType);
        });
    }

    private static IResult Immutable(HttpContext context, string etag, Func<IResult> body)
    {
        context.Response.Headers.ETag = etag;
        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return context.Request.Headers.IfNoneMatch.ToString() == etag ? Results.StatusCode(StatusCodes.Status304NotModified) : body();
    }
}
