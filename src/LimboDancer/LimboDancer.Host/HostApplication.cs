using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.Host;

public static class HostApplication
{
    public static WebApplication Build(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddLimboDancerHost(builder.Configuration);

        var application = builder.Build();
        application.UseAuthentication();
        application.UseAuthorization();
        application.MapLimboDancerEndpoints();
        return application;
    }
}
