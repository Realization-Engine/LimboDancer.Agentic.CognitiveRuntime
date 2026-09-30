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
        builder.Services.AddSingleton<ICatalogSource>(services => services.GetRequiredService<VaslBoardProvider>());
        builder.Services.AddSingleton<IVaslMapSource>(services => services.GetRequiredService<VaslBoardProvider>());
        builder.Services.AddSingleton<AuthoredBoardService>();
        builder.Services.AddSingleton<MapService>();
        builder.Services.AddSingleton<IBoardProvider, StudioBoardProvider>();
        builder.Services.AddSingleton<RenderCache>();
        builder.Services.AddSingleton<FidelityReportStore>();
        builder.Services.AddSingleton<IFidelityBatch, VaslFidelityBatch>();
        builder.Services.AddSingleton<FidelityJobRunner>();
        builder.Services.AddSingleton<UnitLibrary>();
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

        builder.Services.AddSingleton<GameLibrary>();
        builder.Services.AddSingleton<GameMaps>();
        builder.Services.AddSingleton<StudioLos>();
        builder.Services.AddScoped<LibraryViewState>();

        var app = builder.Build();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRenderEndpoints();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.Run();
    }
}
