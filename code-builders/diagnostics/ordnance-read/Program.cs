using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;

// Read-only synchronous FromState probe. Startup, replay, warm-up and JSON
// rendering are outside the measured interval. Reflection is included equally
// on both builds to accommodate the appended optional participation parameters.
// args: repository, boards root, proof store, game, iterations (default 10000).
var repository = args[0];
var services = new ServiceCollection();
services.AddSingleton(new StudioOptions
{
    BoardsRoot = args[1],
    VaslRoot = Environment.GetEnvironmentVariable("ASL_VASL_ROOT") ?? "E:/Archive/GitHub/dlandi/vasl",
    OracleFixtures = repository + "/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle",
});
services.AddSingleton<VaslBoardProvider>();
services.AddSingleton<ICatalogSource>(provider => provider.GetRequiredService<VaslBoardProvider>());
services.AddSingleton<IVaslMapSource>(provider => provider.GetRequiredService<VaslBoardProvider>());
services.AddSingleton<AuthoredBoardService>();
services.AddSingleton<MapService>();
services.AddSingleton<IBoardProvider, StudioBoardProvider>();
services.AddSingleton<UnitLibrary>();
using var provider = services.BuildServiceProvider();
var units = provider.GetRequiredService<UnitLibrary>();
var store = new FileGameStore(args[2]);
IReadOnlyList<UnitCatalog> catalogs = [.. UnitCatalogs.Names.Select(name => UnitCatalogs.Read(name, units.Vocabulary)?.Catalog).OfType<UnitCatalog>()];
var planner = new GamePlanner(store, new StudioBoardCatalog(provider.GetRequiredService<IBoardProvider>()), units.Vocabulary, catalogs);
var record = store.Read(new GameScope(LivePlay.Tenant, args[3])) ?? throw new InvalidOperationException("Missing fixture");
var shotIndex = record.Events.ToList().FindIndex(item => item.Payload is OrdnanceFired);
if (shotIndex < 1) throw new InvalidOperationException("Fixture has no shot");
var shot = (OrdnanceFired)record.Events[shotIndex].Payload;
var history = planner.Replay([.. record.Events.Take(shotIndex)]);
if (history.States.Count != shotIndex) throw new InvalidOperationException("Fixture failed to replay to the shot");
var state = history.States[^1];
var method = typeof(LiveOrdnance).GetMethod("FromState", BindingFlags.Public | BindingFlags.Static)!;
var inputs = method.GetParameters().Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : null).ToArray();
inputs[0] = state;
inputs[1] = shot.Gun;
inputs[2] = shot.Target;
object Read() => method.Invoke(null, inputs)!;
var sample = Read();
if (sample.GetType().GetField("Item2")!.GetValue(sample) is { } reason) throw new InvalidOperationException(reason.ToString());
for (var index = 0; index < 100; index++) _ = Read();
var iterations = args.Length > 4 ? int.Parse(args[4]) : 10000;
using var process = Process.GetCurrentProcess();
Console.WriteLine($"game={args[3]} revision={shotIndex} gun={shot.Gun} target={shot.Target} iterations={iterations} warmup=100 reflection=true");
for (var trial = 0; trial < 5; trial++)
{
    var cpu = process.TotalProcessorTime;
    var allocated = GC.GetAllocatedBytesForCurrentThread();
    var start = Stopwatch.GetTimestamp();
    object? last = null;
    for (var index = 0; index < iterations; index++) last = Read();
    var elapsed = Stopwatch.GetElapsedTime(start);
    var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
    var cpuMs = (process.TotalProcessorTime - cpu).TotalMilliseconds;
    GC.KeepAlive(last);
    Console.WriteLine(JsonSerializer.Serialize(new { trial, elapsedMs = elapsed.TotalMilliseconds, cpuMs, bytes, bytesPerRead = (double)bytes / iterations }));
}
