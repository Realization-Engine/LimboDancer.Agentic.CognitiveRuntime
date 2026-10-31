using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;

// Read-only paired probe of the actual SMOKE helpers. Build this same source against each Studio.
// Arguments: repository, proof store, output JSON. DOTNET_TieredCompilation=0 for both processes.
var repository = args[0];
var options = new StudioOptions
{
    BoardsRoot = repository + "/src/ASL/boards",
    VaslRoot = Environment.GetEnvironmentVariable("ASL_VASL_ROOT") ?? "E:/Archive/GitHub/dlandi/vasl",
    OracleFixtures = repository + "/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle"
};
var services = new ServiceCollection();
services.AddSingleton(options);
services.AddSingleton<VaslBoardProvider>();
services.AddSingleton<ICatalogSource>(p => p.GetRequiredService<VaslBoardProvider>());
services.AddSingleton<IVaslMapSource>(p => p.GetRequiredService<VaslBoardProvider>());
services.AddSingleton<AuthoredBoardService>();
services.AddSingleton<MapService>();
services.AddSingleton<IBoardProvider, StudioBoardProvider>();
services.AddSingleton<UnitLibrary>();
using var provider = services.BuildServiceProvider();
var units = provider.GetRequiredService<UnitLibrary>();
var store = new FileGameStore(args[1]);
var catalogs = UnitCatalogs.Names.Select(name => UnitCatalogs.Read(name, units.Vocabulary)?.Catalog).OfType<UnitCatalog>().ToArray();
var planner = new GamePlanner(store, new StudioBoardCatalog(provider.GetRequiredService<IBoardProvider>()), units.Vocabulary, catalogs,
    cardLibrary: new ScenarioCardLibrary(options.BoardsRoot + "/cards"));
const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
var los = typeof(GamePlanner).GetMethod("Los", Private, [typeof(GameState), typeof(BoardLocation), typeof(BoardLocation)])!
    .CreateDelegate<Func<GameState, BoardLocation, BoardLocation, LosResult?>>(planner);
var fire = typeof(GamePlanner).GetMethod("VehicleHindrance", Private)!
    .CreateDelegate<Func<GameState, BoardLocation, BoardLocation, LosResult, bool, HashSet<int>, (int Drm, string? Reason)>>(planner);
var sources = typeof(GamePlanner).GetMethod("SmokeSources", Private)!.CreateDelegate<Func<GameState, IEnumerable<BoardLocation>>>();
var currentSmoke = typeof(GamePlanner).GetMethod("SmokeHindrancesAt", Private)?
    .CreateDelegate<Func<GameState, BoardLocation, (IReadOnlyList<int>? Hindrances, string? Reason)>>(planner);
var preflight = typeof(GamePlanner).GetMethod("SniperSmokeBar", Private)?.CreateDelegate<Func<GameState, string?>>(planner);
var oldOverrun = typeof(ScenarioA1OverrunCalculator).GetMethod("OverrunSmoke", [typeof(int)])?.CreateDelegate<Func<int, int>>();
var valueOverrun = typeof(ScenarioA1OverrunCalculator).GetMethod("OverrunSmoke", [typeof(IReadOnlyList<int>)])?.CreateDelegate<Func<IReadOnlyList<int>, int>>();
var oldSniper = typeof(ScenarioA1Sniper).GetMethod("TargetTem", [typeof(string), typeof(int)])?.CreateDelegate<Func<string?, int, int>>();
var valueSniper = typeof(ScenarioA1Sniper).GetMethod("TargetTem", [typeof(string), typeof(IReadOnlyList<int>)])?.CreateDelegate<Func<string?, IReadOnlyList<int>, int>>();
var results = new List<object>();

GameHistory History(string game)
{
    var record = store.Read(new GameScope(LivePlay.Tenant, game)) ?? throw new InvalidOperationException(game);
    var history = planner.Replay(record.Events);
    if (history.HasErrors || history.Current is null) throw new InvalidOperationException("Incomplete measurement fixture: " + game);
    return history;
}
var full = History("dl-tractor-works-02");
var small = History("wire-smoke-strength");
var smokeState = small.States.First(s => s.Entities.Any(e => e.Kind == "asl:smoke" && e.Status == InstanceStatus.Active));
var smokeAt = ((MapPosition)smokeState.Entities.First(e => e.Kind == "asl:smoke" && e.Status == InstanceStatus.Active).Position!).Location;
var withSniper = smokeState with
{
    Entities = [.. smokeState.Entities, new EntityInstance("measure-sniper", "asl:sniper", "german", new MapPosition(smokeAt),
        new Dictionary<string, ConditionState>(), InstanceStatus.Active)]
};
foreach (var (name, state) in new[] { ("reference", full.Current!), ("smoke", smokeState), ("smoke-with-sniper", withSniper) })
{
    var source = sources(state).FirstOrDefault();
    var locations = state.Units.Where(u => u.Status == InstanceStatus.Active && u.Position is MapPosition)
        .Select(u => ((MapPosition)u.Position).Location).Distinct().ToArray();
    var at = source ?? locations[0];
    var from = locations.FirstOrDefault(p => p != at) ?? at;
    var sight = los(state, from, at) ?? throw new InvalidOperationException("No measurement LOS");
    var ranges = new HashSet<int>();
    IReadOnlyList<int> Values(GameState s)
    {
        var result = currentSmoke!(s, at);
        return result.Reason is null && result.Hindrances is not null ? result.Hindrances : throw new InvalidOperationException(result.Reason);
    }
    int Fire(GameState s)
    {
        var answer = fire(s, from, at, sight, from.Level == at.Level, ranges);
        return answer.Reason is null ? answer.Drm : throw new InvalidOperationException(answer.Reason);
    }
    int Overrun(GameState s) => currentSmoke is null ? oldOverrun!(sources(s).Count(p => p == at)) : valueOverrun!(Values(s));
    int Sniper(GameState s) => currentSmoke is null ? oldSniper!("open-ground", sources(s).Count(p => p == at)) : valueSniper!("open-ground", Values(s));
    int Preflight(GameState s) => preflight?.Invoke(s) is null ? 0 : throw new InvalidOperationException("Sniper preflight refused");
    foreach (var (operation, run) in new (string, Func<GameState, int>)[] { ("fire-los", Fire), ("overrun-smoke", Overrun), ("sniper-tem", Sniper), ("sniper-preflight", Preflight) })
    {
        foreach (var cold in new[] { false, true })
        {
            for (var i = 0; i < 40; i++) _ = run(cold ? state with { } : state);
            var bytes = new List<double>();
            var micros = new List<double>();
            var answer = run(state);
            var iterations = cold ? 40 : 400;
            for (var batch = 0; batch < 9; batch++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                var allocated = GC.GetAllocatedBytesForCurrentThread();
                var start = Stopwatch.GetTimestamp();
                for (var i = 0; i < iterations; i++)
                {
                    if (run(cold ? state with { } : state) != answer) throw new InvalidOperationException("Unstable answer");
                }
                var elapsed = Stopwatch.GetElapsedTime(start).TotalMicroseconds;
                bytes.Add((GC.GetAllocatedBytesForCurrentThread() - allocated) / (double)iterations);
                micros.Add(elapsed / iterations);
            }
            bytes.Sort(); micros.Sort();
            var row = new { fixture = name, revision = state.Revision, units = state.Units.Count, smoke = sources(state).Count(),
                operation, cold, answer, iterations, bytes = bytes[4], medianUs = micros[4], p90Us = micros[8] };
            results.Add(row);
            Console.WriteLine(JsonSerializer.Serialize(row));
        }
    }
}
foreach (var game in new[] { "wire-smoke-fire", "wire-vehicle-repair" })
{
    var scope = new GameScope(LivePlay.Tenant, game);
    var record = store.Read(scope)!;
    var shot = record.Events.Select(e => e.Payload).OfType<FireResolved>().FirstOrDefault();
    var repair = record.Events.Select(e => e.Payload).OfType<RepairAttempted>().FirstOrDefault();
    string[] rollIds = shot is not null ? [.. shot.Rolls.Values] : [repair!.Roll];
    var cut = record.Events.TakeWhile(e => e.Payload is not DiceRolled die || !rollIds.Contains(die.Roll)).ToArray();
    var probePlanner = new GamePlanner(new CutStore(scope, record with { Events = cut }), new StudioBoardCatalog(provider.GetRequiredService<IBoardProvider>()),
        units.Vocabulary, catalogs, cardLibrary: new ScenarioCardLibrary(options.BoardsRoot + "/cards"));
    var arguments = new Dictionary<string, object?> { ["gameId"] = game, ["attemptId"] = "allocation-probe", ["expectedRevision"] = cut.Length };
    if (shot is not null) { arguments["firers"] = shot.Firers; arguments["target"] = shot.TargetLocation; arguments["director"] = shot.Director; }
    else { arguments["unitId"] = repair!.Unit; arguments["equipmentId"] = repair.Equipment; arguments["weapon"] = repair.Weapon; }
    var json = JsonSerializer.SerializeToElement(arguments);
    var action = shot is not null ? GameActions.Fire : GameActions.Repair;
    int Run()
    {
        var plan = probePlanner.PlanAsync(action, json, LivePlay.Tenant).GetAwaiter().GetResult();
        if (plan.Status != GamePlanStatus.Ready || plan.Roll is null) throw new InvalidOperationException(string.Join("; ", plan.Reasons));
        RollResult Draw(RollRequest request) => (RollResult)Activator.CreateInstance(typeof(RollResult), BindingFlags.NonPublic | BindingFlags.Instance,
            null, [request, Enumerable.Repeat(4, request.Count).ToArray()], null)!;
        return plan.Events.Count + plan.Roll.Build(Draw).Count;
    }
    for (var i = 0; i < 20; i++) _ = Run();
    var answer = Run();
    var bytes = new List<double>(); var micros = new List<double>();
    for (var batch = 0; batch < 9; batch++)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var allocated = GC.GetTotalAllocatedBytes(true);
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < 20; i++) if (Run() != answer) throw new InvalidOperationException("Unstable command result");
        var elapsed = Stopwatch.GetElapsedTime(start).TotalMicroseconds;
        bytes.Add((GC.GetTotalAllocatedBytes(true) - allocated) / 20d); micros.Add(elapsed / 20);
    }
    bytes.Sort(); micros.Sort();
    var row = new { fixture = game, revision = cut.Length, operation = shot is not null ? "fire-command" : "repair-command", cold = false,
        answer, iterations = 20, bytes = bytes[4], medianUs = micros[4], p90Us = micros[8] };
    results.Add(row); Console.WriteLine(JsonSerializer.Serialize(row));
}
File.WriteAllText(args[2], JsonSerializer.Serialize(new
{
    playVersion = typeof(GamePlanner).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
    build = "Debug", runtime = Environment.Version.ToString(), view = "Viewer-independent physical facts; commands use the recorded phasing side.",
    inputs = new[] { "dl-tractor-works-02", "wire-smoke-strength", "wire-smoke-fire", "wire-vehicle-repair" }.Select(game => new
    {
        game, sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(args[1], LivePlay.Tenant.ToString("N"), game + ".game.json"))))
    }),
    maps = new[] { full.Current!.Map, smokeState.Map },
    source = "Exact synchronous CLR allocation totals: per-thread for helper reads, process-wide for complete commands; nine batches, GC outside timed regions; clone included for cold state.", results
}, new JsonSerializerOptions { WriteIndented = true }));

sealed class CutStore(GameScope scope, GameRecord record) : IGameStore
{
    public GameRecord? Read(GameScope requested) => requested == scope ? record : null;
    public IReadOnlyList<GameScope> List(Guid tenant) => [scope];
    public AppendResult Append(GameScope scope, string label, long expectedRevision, IReadOnlyList<GameEvent> events,
        Func<IReadOnlyList<GameEvent>, GameHistory> replay) => throw new InvalidOperationException("Read-only allocation probe");
    public AppendResult AppendRolled(GameScope scope, string label, long expectedRevision, string firstEventId, PlannedRoll roll, DiceRoller roller,
        Func<IReadOnlyList<GameEvent>, GameHistory> replay) => throw new InvalidOperationException("Read-only allocation probe");
}
