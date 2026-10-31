using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;

// Arguments: repository, read-only proof store, output JSON, source commit label.
var startedUtc = DateTimeOffset.UtcNow;
var repository = args[0];
var options = new StudioOptions
{
    BoardsRoot = repository + "/src/ASL/boards",
    VaslRoot = Environment.GetEnvironmentVariable("ASL_VASL_ROOT") ?? "E:/Archive/GitHub/dlandi/vasl",
    OracleFixtures = repository + "/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle",
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
var boards = new StudioBoardCatalog(provider.GetRequiredService<IBoardProvider>());
var fromState = typeof(LiveOrdnance).GetMethod("FromState", BindingFlags.Public | BindingFlags.Static)!;
var advice = typeof(GamePlanner).GetMethods().Single(method => method.Name == "GunTargetStatus");
var supportsPair = fromState.GetParameters().Any(parameter => parameter.Name == "secondFirer");
var results = new List<object>();
var inputs = new List<object>();
foreach (var workload in new[] { "gun", "canister", "mortar-solo", "mortar-pair" })
{
    var mortar = workload.StartsWith("mortar", StringComparison.Ordinal);
    var pair = workload == "mortar-pair";
    var game = mortar ? "wire-c9-light-review" : "wire-c9-gun-review";
    var scope = new GameScope(LivePlay.Tenant, game);
    var record = store.Read(scope) ?? throw new InvalidOperationException("Missing " + game);
    var firstShot = record.Events.Select(item => item.Payload).OfType<OrdnanceFired>().FirstOrDefault();
    var cut = firstShot is null ? record.Events.ToArray()
        : record.Events.TakeWhile(item => item.Payload is not DiceRolled die || !firstShot.Rolls.Values.Contains(die.Roll)).ToArray();
    var planner = new GamePlanner(new CutStore(scope, record with { Events = cut }), boards, units.Vocabulary, catalogs,
        cardLibrary: new ScenarioCardLibrary(options.BoardsRoot + "/cards"));
    var history = planner.Replay(cut);
    if (history.HasErrors || history.Current is null) throw new InvalidOperationException("Invalid cut " + game);
    var state = history.Current;
    var gun = mortar ? "mortar" : "gun";
    var target = BoardLocation.Parse(mortar ? "bd03:B6:0" : "bd01:D4:0");
    var second = pair && supportsPair ? "g-second" : null;
    var ammunition = workload == "canister" ? "canister" : "he";
    inputs.Add(new
    {
        workload, game, revision = cut.Length, state.Map, secondFirer = second,
        sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(args[1], LivePlay.Tenant.ToString("N"), game + ".game.json")))),
    });
    object?[] Arguments(MethodInfo method)
    {
        var values = method.GetParameters().Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : null).ToArray();
        values[0] = state; values[1] = gun; values[2] = target;
        foreach (var parameter in method.GetParameters())
        {
            if (parameter.Name == "secondFirer") values[parameter.Position] = second;
            if (parameter.Name == "ammunition") values[parameter.Position] = ammunition;
        }
        return values;
    }
    var reads = Arguments(fromState);
    var adviceReads = Arguments(advice);
    var factField = fromState.ReturnType.GetField("Item1")!;
    var reasonField = fromState.ReturnType.GetField("Item2")!;
    object Read(bool cold)
    {
        reads[0] = cold ? state with { } : state;
        var result = fromState.Invoke(null, reads)!;
        if (reasonField.GetValue(result) is { } reason) throw new InvalidOperationException(reason.ToString());
        return factField.GetValue(result) ?? throw new InvalidOperationException("No shot");
    }
    object? Advise(bool cold)
    {
        adviceReads[0] = cold ? state with { } : state;
        return advice.Invoke(planner, adviceReads);
    }
    string Summary(object? value)
    {
        if (value is not OrdnanceShot shot) return value?.ToString() ?? "null";
        var participation = shot.GetType().GetProperty("Participation")?.GetValue(shot);
        var hasSecond = participation?.GetType().GetProperty("Second")?.GetValue(participation) is not null;
        return $"type={shot.TargetType};use={shot.UseMultiple?.ToString() ?? "null"};participants={(hasSecond ? 2 : 1)}";
    }
    foreach (var cold in new[] { false, true })
    foreach (var operation in new[] { "from-state", "target-advice" })
    {
        Func<object?> action = operation == "from-state" ? () => Read(cold) : () => Advise(cold);
        var expected = Summary(action());
        for (var index = 0; index < 40; index++) _ = action();
        var count = cold ? 40 : 400;
        var samples = new List<Sample>();
        for (var batch = 0; batch < 9; batch++)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            object? last = null;
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            for (var index = 0; index < count; index++) last = action();
            var elapsed = Stopwatch.GetElapsedTime(start).TotalMicroseconds;
            var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
            if (Summary(last) != expected) throw new InvalidOperationException("Unstable helper result");
            samples.Add(new(bytes / (double)count, elapsed / count));
        }
        var row = new { workload, game, operation, cold, answer = expected, iterations = count,
            bytes = samples.Select(sample => sample.Bytes).Order().ElementAt(4),
            medianUs = samples.Select(sample => sample.Microseconds).Order().ElementAt(4),
            p90Us = samples.Select(sample => sample.Microseconds).Order().ElementAt(8), samples };
        results.Add(row); Console.WriteLine(JsonSerializer.Serialize(row));
    }
    var command = new Dictionary<string, object?> { ["gameId"] = game, ["attemptId"] = "allocation-probe", ["expectedRevision"] = cut.Length,
        ["gunId"] = gun, ["ammunition"] = ammunition };
    if (workload == "canister") command["aim"] = 4;
    else command["target"] = target.ToString();
    if (second is not null) command["secondFirer"] = second;
    var json = JsonSerializer.SerializeToElement(command);
    IReadOnlyList<GameEvent> lastEvents = [];
    int Run()
    {
        var plan = planner.PlanAsync(GameActions.FireOrdnance, json, LivePlay.Tenant).GetAwaiter().GetResult();
        if (plan.Status != GamePlanStatus.Ready || plan.Roll is null) throw new InvalidOperationException(string.Join("; ", plan.Reasons));
        RollResult Draw(RollRequest request) => (RollResult)Activator.CreateInstance(typeof(RollResult), BindingFlags.NonPublic | BindingFlags.Instance,
            null, [request, Enumerable.Repeat(3, request.Count).ToArray()], null)!;
        lastEvents = plan.Roll.Build(Draw);
        return plan.Events.Count + lastEvents.Count;
    }
    for (var index = 0; index < 20; index++) _ = Run();
    var eventCount = Run();
    var commandSamples = new List<Sample>();
    for (var batch = 0; batch < 9; batch++)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var allocated = GC.GetTotalAllocatedBytes(true);
        var start = Stopwatch.GetTimestamp();
        for (var index = 0; index < 20; index++) if (Run() != eventCount) throw new InvalidOperationException("Unstable event count");
        var elapsed = Stopwatch.GetElapsedTime(start).TotalMicroseconds;
        commandSamples.Add(new((GC.GetTotalAllocatedBytes(true) - allocated) / 20d, elapsed / 20));
    }
    var fired = lastEvents.Select(item => item.Payload).OfType<OrdnanceFired>().Single();
    var paired = fired.Facts.TryGetProperty("participation", out var evidence)
        && evidence.TryGetProperty("second", out var additional) && additional.ValueKind == JsonValueKind.Object;
    int? useMultiple = fired.Facts.TryGetProperty("useMultiple", out var recordedUse) ? recordedUse.GetInt32() : null;
    var commandRow = new { workload, game, operation = "fire-command", cold = false,
        answer = new { events = eventCount, participants = paired ? 2 : 1, useMultiple, fired.RateOfFireKept, fired.Acquisition },
        eventTypes = lastEvents.Select(item => item.Payload.GetType().Name).ToArray(),
        iterations = 20, bytes = commandSamples.Select(sample => sample.Bytes).Order().ElementAt(4),
        medianUs = commandSamples.Select(sample => sample.Microseconds).Order().ElementAt(4),
        p90Us = commandSamples.Select(sample => sample.Microseconds).Order().ElementAt(8), samples = commandSamples };
    results.Add(commandRow); Console.WriteLine(JsonSerializer.Serialize(commandRow));
}
File.WriteAllText(args[2], JsonSerializer.Serialize(new
{
    sourceCommit = args[3], startedUtc, build = "Debug", runtime = Environment.Version.ToString(),
    serverGc = System.Runtime.GCSettings.IsServerGC, processorCount = Environment.ProcessorCount, supportsPair,
    playVersion = typeof(GamePlanner).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
    playAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(GamePlanner).Assembly.Location))),
    inputs, results,
}, new JsonSerializerOptions { WriteIndented = true }));

sealed record Sample(double Bytes, double Microseconds);
sealed class CutStore(GameScope scope, GameRecord record) : IGameStore
{
    public GameRecord? Read(GameScope requested) => requested == scope ? record : null;
    public IReadOnlyList<GameScope> List(Guid tenant) => [scope];
    public AppendResult Append(GameScope scope, string label, long expectedRevision, IReadOnlyList<GameEvent> events,
        Func<IReadOnlyList<GameEvent>, GameHistory> replay) => throw new InvalidOperationException("Read-only allocation probe");
    public AppendResult AppendRolled(GameScope scope, string label, long expectedRevision, string firstEventId, PlannedRoll roll, DiceRoller roller,
        Func<IReadOnlyList<GameEvent>, GameHistory> replay) => throw new InvalidOperationException("Read-only allocation probe");
}
