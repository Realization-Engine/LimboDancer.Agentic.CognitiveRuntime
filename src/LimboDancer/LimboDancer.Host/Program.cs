namespace LimboDancer.Host;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var application = HostApplication.Build(args);
        await application.RunAsync().ConfigureAwait(false);
    }
}
