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
var app = builder.Build();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/asl-pilot"))
    {
        // Local, same-origin API. JSON-only mutations prevent cross-site form submissions.
        var origin = context.Request.Headers.Origin.ToString();
        if (context.Connection.RemoteIpAddress is { } ip && !System.Net.IPAddress.IsLoopback(ip) ||
            origin.Length > 0 && origin != context.Request.Scheme + "://" + context.Request.Host ||
            context.Request.Method == "POST" && !context.Request.HasJsonContentType())
        { context.Response.StatusCode = 403; return; }
        context.Response.Headers.CacheControl = "no-store";
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
using var files = new PhysicalFileProvider(prototype);
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
app.Run();
