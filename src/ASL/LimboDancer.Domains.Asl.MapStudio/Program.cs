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
        builder.Services.AddSingleton<IBoardProvider, VaslBoardProvider>();
        builder.Services.AddSingleton<RenderCache>();
        builder.Services.AddSingleton<FidelityReportStore>();
        builder.Services.AddSingleton<IFidelityBatch, VaslFidelityBatch>();
        builder.Services.AddSingleton<FidelityJobRunner>();

        var app = builder.Build();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRenderEndpoints();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.Run();
    }
}
