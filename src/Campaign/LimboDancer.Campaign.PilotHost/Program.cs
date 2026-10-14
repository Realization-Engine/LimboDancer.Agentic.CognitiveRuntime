using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using LimboDancer.Campaign.PilotHost;
using LimboDancer.Campaign.Refinement;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
if (builder.Configuration["urls"] is null) builder.WebHost.UseUrls("http://127.0.0.1:8093");
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 4 * 1024 * 1024);
var directory = new DirectoryInfo(AppContext.BaseDirectory);
while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "prototypes", "europe-hex-map"))) directory = directory.Parent;
var prototype = Path.GetFullPath(builder.Configuration["Campaign:PrototypeRoot"] ??
    Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Configure Campaign:PrototypeRoot."), "prototypes", "europe-hex-map"));
var inputs = new PilotInputs(File.ReadAllText(Path.Combine(prototype, "sources", "situations", "panzer-leader-05.json")));
var packages = Path.GetFullPath(builder.Configuration["Campaign:PackagesRoot"] ?? Path.Combine(directory?.FullName ?? Path.GetDirectoryName(prototype)!, "artifacts", "campaign-pilot"));
builder.Services.AddSingleton(new PilotSessions(inputs, packages));
builder.Services.AddSingleton(new SituationSessions(inputs, Path.Combine(packages, "situations")));
var studio = new Uri(builder.Configuration["Campaign:AslStudioUrl"] ?? "http://127.0.0.1:5178");
if (!studio.IsLoopback || studio.Scheme != "http") throw new InvalidOperationException("ASL Studio must use a local HTTP address.");
builder.Services.AddHttpClient("asl-studio", client => { client.BaseAddress = studio; client.Timeout = TimeSpan.FromSeconds(60); });
builder.Services.AddHttpClient("asl-situation-studio", client => { client.BaseAddress = studio; client.Timeout = TimeSpan.FromMinutes(10); });
var app = builder.Build();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/asl-pilot") || context.Request.Path.StartsWithSegments("/api/asl-situation"))
    {
        // Local, same-origin API. JSON-only mutations prevent cross-site form submissions.
        var origin = context.Request.Headers.Origin.ToString();
        if (context.Connection.RemoteIpAddress is { } ip && !System.Net.IPAddress.IsLoopback(ip) ||
            origin.Length > 0 && origin != context.Request.Scheme + "://" + context.Request.Host ||
            context.Request.Method == "POST" && !context.Request.HasJsonContentType())
        { context.Response.StatusCode = 403; return; }
        context.Response.Headers.CacheControl = "no-store";
        if (context.Request.Path == "/api/asl-situation/import" &&
            context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
            limit.MaxRequestBodySize = 60 * 1024 * 1024;
        if (context.Request.Path == "/api/asl-pilot/open")
        {
            var watch = Stopwatch.StartNew();
            context.Items["pilotRequestWatch"] = watch;
            context.Response.OnStarting(() =>
            {
                var total = watch.Elapsed.TotalMilliseconds;
                var bound = (double)(context.Items["pilotBoundMs"] ?? 0d);
                var handled = (double)(context.Items["pilotHandledMs"] ?? bound);
                context.Response.Headers["Server-Timing"] = string.Create(CultureInfo.InvariantCulture,
                    $"binding;dur={bound:F2},service;dur={handled - bound:F2},responseHeaders;dur={total - handled:F2},requestHeaders;dur={total:F2}");
                return Task.CompletedTask;
            });
        }
    }
    try { await next(context); }
    catch (Exception e) when (e is InvalidDataException or ArgumentException or JsonException or KeyNotFoundException or FormatException or InvalidOperationException)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { error = e.Message });
    }
});
app.MapGet("/api/asl-pilot", () => new { version = PilotDeployment.Policy, source = "panzer-leader-05", admission = "bounded infantry setup only" });
app.MapPost("/api/asl-pilot/generate", (GenerateRequest request, PilotSessions service) => service.Start(request.Seed, request.Formation));
app.MapGet("/api/asl-pilot/generate/{seed}", (string seed, PilotSessions service) => service.Status(seed));
app.MapDelete("/api/asl-pilot/generate/{seed}", (string seed, PilotSessions service) => { service.Cancel(seed); return Results.NoContent(); });
app.MapPost("/api/asl-pilot/open", (OpenRequest request, PilotSessions service) => service.Open(request))
    .AddEndpointFilter(async (context, next) =>
    {
        var watch = (Stopwatch)context.HttpContext.Items["pilotRequestWatch"]!;
        context.HttpContext.Items["pilotBoundMs"] = watch.Elapsed.TotalMilliseconds;
        var response = await next(context);
        context.HttpContext.Items["pilotHandledMs"] = watch.Elapsed.TotalMilliseconds;
        return response;
    });
app.MapPost("/api/asl-pilot/edit", (EditRequest request, PilotSessions service) => service.Edit(request));
app.MapPost("/api/asl-pilot/handoff", (EditRequest request, PilotSessions service) => service.Handoff(request));
app.MapPost("/api/asl-pilot/launch", async (EditRequest request, PilotSessions service, IHttpClientFactory clients) =>
{
    var packet = await service.ExportHandoff(request);
    try
    {
        using var response = await clients.CreateClient("asl-studio").PostAsJsonAsync("/api/campaign/import", packet);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (!response.IsSuccessStatusCode) throw new InvalidDataException(result.TryGetProperty("error", out var error) ? error.GetString() : "ASL import failed.");
        var path = result.GetProperty("path").GetString()!;
        if (!path.StartsWith("/games/play?game=campaign-", StringComparison.Ordinal)) throw new InvalidDataException("Unexpected ASL game address.");
        return Results.Ok(new { url = new Uri(studio, path).AbsoluteUri, existing = result.GetProperty("existing").GetBoolean() });
    }
    catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
    { throw new InvalidDataException("ASL Studio is unavailable. Start it at " + studio + " and try again. Your campaign setup is unchanged.", e); }
});
app.MapPost("/api/asl-situation/start", (SituationStartRequest request, SituationSessions service) => new { job = service.Start(request) });
app.MapGet("/api/asl-situation/jobs/{id}", (string id, SituationSessions service) => service.Status(id));
app.MapDelete("/api/asl-situation/jobs/{id}", (string id, SituationSessions service) => { service.Cancel(id); return Results.NoContent(); });
app.MapGet("/api/asl-situation/saves/{id}", (string id, SituationSessions service) => service.Read(id));
app.MapGet("/api/asl-situation/saves/{id}/export", (string id, SituationSessions service, HttpContext context) => service.Export(id, context.RequestAborted));
app.MapPost("/api/asl-situation/import", (SituationPortableSave request, SituationSessions service) => new { job = service.Import(request) });
app.MapPost("/api/asl-situation/saves/{id}/prepare", (string id, SituationSessions service) => new { job = service.Prepare(id) });
app.MapGet("/api/asl-situation/handoffs/{id}", (string id, SituationSessions service) => service.ReadHandoff(id));
app.MapPost("/api/asl-situation/handoffs/{id}/launch", (string id, SituationSessions service, IHttpClientFactory clients) =>
    new { job = service.Launch(id, async (handoff, token) =>
    {
        var packet = new { board = handoff.Board, name = handoff.Name, version = handoff.BoardVersion,
            features = handoff.Features, gameRecord = handoff.GameRecord, campaignContext = SituationHandoffs.ContextJson(handoff) };
        try
        {
            using var response = await clients.CreateClient("asl-situation-studio").PostAsJsonAsync("/api/campaign/import", packet, token);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(token);
            if (!response.IsSuccessStatusCode) throw new InvalidDataException(result.TryGetProperty("error", out var error) ? error.GetString() : "ASL import failed.");
            var path = result.GetProperty("path").GetString()!;
            if (!path.StartsWith("/games/play?game=campaign-", StringComparison.Ordinal)) throw new InvalidDataException("Unexpected ASL game address.");
            return new Uri(studio, path).AbsoluteUri;
        }
        catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException && !token.IsCancellationRequested)
        { throw new InvalidDataException("ASL Studio could not complete the import. Your campaign save is unchanged; retrying preserves an existing ASL game.", e); }
    }) });
using var files = new PhysicalFileProvider(prototype);
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
app.Run();
