using LimboDancer.Domains.Asl.MapStudio.Components;
using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        var options = builder.Configuration.GetSection("AslMaps").Get<StudioOptions>() ?? new StudioOptions();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<VaslBoardProvider>();
        builder.Services.AddSingleton<ICatalogSource, AuthoredCatalogSource>();
        builder.Services.AddSingleton<IVaslMapSource>(services => services.GetRequiredService<VaslBoardProvider>());
        builder.Services.AddSingleton<AuthoredBoardService>();
        builder.Services.AddSingleton<MapService>();
        builder.Services.AddSingleton<IBoardProvider, StudioBoardProvider>();
        builder.Services.AddSingleton<RenderCache>();
        builder.Services.AddSingleton<FidelityReportStore>();
        builder.Services.AddSingleton<IFidelityBatch, VaslFidelityBatch>();
        builder.Services.AddSingleton<FidelityJobRunner>();
        builder.Services.AddSingleton<UnitLibrary>();
        builder.Services.AddSingleton(services => new IconLibrary(services.GetRequiredService<IWebHostEnvironment>()));
        builder.Services.AddSingleton(services => new IconPlanStore(services.GetRequiredService<IWebHostEnvironment>()));
        builder.Services.AddSingleton(services => new FigureArtLab(services.GetRequiredService<IWebHostEnvironment>()));
        if (ScriptedDice.Enabled(builder.Environment, builder.Configuration))
        {
            // UI test runs only: the Play page queues the dice (ScriptedDice).
            builder.Services.AddSingleton<ScriptedDice>();
            builder.Services.AddSingleton(services => new LivePlay(services.GetRequiredService<UnitLibrary>(), services.GetRequiredService<IBoardProvider>(),
                services.GetRequiredService<ScriptedDice>().Roller));
        }
        else
        {
            builder.Services.AddSingleton<LivePlay>();
        }

        builder.Services.AddSingleton<CampaignImport>();
        builder.Services.AddSingleton<GameLibrary>();
        builder.Services.AddSingleton<GameMaps>();
        builder.Services.AddSingleton<StudioLos>();
        builder.Services.AddScoped<LibraryViewState>();

        var app = builder.Build();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRenderEndpoints();
        app.MapPost("/api/campaign/import", (HttpContext context, CampaignImportRequest request, CampaignImport service) =>
        {
            // Local server-to-server import only; browser-origin writes are refused.
            if (context.Connection.RemoteIpAddress is not { } ip || !System.Net.IPAddress.IsLoopback(ip) ||
                context.Request.Headers.Origin.Count > 0 || !context.Request.HasJsonContentType())
            {
                return Results.StatusCode(403);
            }

            try
            {
                return Results.Ok(service.Import(request, context.RequestAborted));
            }
            catch (Exception e) when (e is InvalidDataException or ArgumentException or System.Text.Json.JsonException or InvalidOperationException)
            {
                return Results.BadRequest(new
                {
                    error = e.Message
                });
            }
        });
        app.MapGet("/api/campaign/{game}/progress", (HttpContext context, string game, CampaignImport service) =>
        {
            // Full event history is for local campaign-file transfer, never a side's play view.
            if (context.Connection.RemoteIpAddress is not { } ip || !System.Net.IPAddress.IsLoopback(ip)
                || context.Request.Headers.Origin.Count > 0)
            {
                return Results.StatusCode(403);
            }
            context.Response.Headers.CacheControl = "no-store";
            try
            {
                return service.ExportProgress(game) is { } record
                    ? Results.Ok(new
                    {
                        gameRecord = record
                    }) : Results.NotFound();
            }
            catch (Exception e) when (e is InvalidDataException or ArgumentException)
            {
                return Results.BadRequest(new
                {
                    error = e.Message
                });
            }
        });
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.Run();
    }
}
