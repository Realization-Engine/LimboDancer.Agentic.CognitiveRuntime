using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;

// Pass 32.a proof tools (design section 8, P2 and P3). Read-only over the repository: the proof store is a copy in the scratchpad.
//   prepare <storeRoot> <liveFolder> <fixturesFolder> <syntheticFile>
//   replay  <out> <storeRoot>     one line a state: game, revision, state digest; then one line a game with its diagnostics
//   sweep   <out> <storeRoot>     the planner's answers at the last state of each game
var mode = args[0];
// ASL_REPO and ASL_VASL_ROOT (code-builders/paths.sh) say where the repository and the VASL checkout are; the defaults are this machine's.
var repository = (Environment.GetEnvironmentVariable("ASL_REPO") is { Length: > 0 } given ? given : "E:/Archive/GitHub/dlandi/LimboDancer.MCP").Replace('\\', '/').TrimEnd('/');
var boardsRoot = repository + "/src/ASL/boards";
var tenant = LivePlay.Tenant;

if (mode == "prepare")
{
    var storeRoot = args[1];
    var folder = Path.Combine(storeRoot, tenant.ToString("N"));
    Directory.CreateDirectory(folder);
    foreach (var file in Directory.GetFiles(args[2], "*.game.json"))
    {
        File.Copy(file, Path.Combine(folder, Path.GetFileName(file)), overwrite: true);
    }

    File.Copy(args[4], Path.Combine(folder, "a1-village-synthetic.game.json"), overwrite: true);
    var count = 0;
    foreach (var name in new[] { "guards-dl-01", "p31c-tw" })
    {
        var bytes = File.ReadAllBytes(Path.Combine(args[3], name + ".game.json"));
        var record = GameEventReader.Read(bytes).Record ?? throw new InvalidOperationException(name);
        File.WriteAllText(Path.Combine(folder, $"fx-{name}.game.json"), GameEventWriter.Write(new GameScope(tenant, $"fx-{name}"), record));
        count++;
        for (var index = 0; index < record.Events.Count; index++)
        {
            if (record.Events[index].Payload is PhaseChanged)
            {
                var scope = new GameScope(tenant, $"fx-{name}-r{index + 1}");
                File.WriteAllText(Path.Combine(folder, scope.Game + ".game.json"),
                    GameEventWriter.Write(scope, record with { Events = [.. record.Events.Take(index + 1)] }));
                count++;
            }
        }
    }

    Console.WriteLine($"{Directory.GetFiles(folder).Length} games in the proof store ({count} fixture files)");
    return;
}

var output = args[1];
var services = new ServiceCollection();
// The Studio's own settings: the VASL checkout beside the repository, and the oracle fixtures in the tests (the tool's own folder is not under the repository).
var options = new StudioOptions
{
    BoardsRoot = boardsRoot,
    VaslRoot = Environment.GetEnvironmentVariable("ASL_VASL_ROOT") is { Length: > 0 } vasl ? vasl : "E:/Archive/GitHub/dlandi/vasl",
    OracleFixtures = repository + "/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle",
};
services.AddSingleton(options);
services.AddSingleton<VaslBoardProvider>();
services.AddSingleton<ICatalogSource>(provider => provider.GetRequiredService<VaslBoardProvider>());
services.AddSingleton<IVaslMapSource>(provider => provider.GetRequiredService<VaslBoardProvider>());
services.AddSingleton<AuthoredBoardService>();
services.AddSingleton<MapService>();
services.AddSingleton<IBoardProvider, StudioBoardProvider>();
services.AddSingleton<UnitLibrary>();
using var provider = services.BuildServiceProvider();
var units = provider.GetRequiredService<UnitLibrary>();
var boards = provider.GetRequiredService<IBoardProvider>();
var store = new FileGameStore(args[2]);
IReadOnlyList<UnitCatalog> catalogs = [.. UnitCatalogs.Names.Select(name => UnitCatalogs.Read(name, units.Vocabulary)?.Catalog).OfType<UnitCatalog>()];
var cards = new ScenarioCardLibrary(Path.Combine(boardsRoot, "cards"));
var planner = new GamePlanner(store, new StudioBoardCatalog(boards), units.Vocabulary, catalogs, cardLibrary: cards);
// Pass 70 (ruling R70.19): the planner's view entry, read by reflection since the tool compiles against the baseline too, which lacks it.
var viewOf = typeof(GamePlanner).GetMethod("ViewOf", [typeof(GameHistory), typeof(long), typeof(Perspective)])?.CreateDelegate<Func<GameHistory, long, Perspective, GameView>>(planner);
// Pass 70's boundary review (finding 19): say on the log which view the sweep builds, so a renamed entry cannot fall back unseen.
Console.Error.WriteLine(viewOf is not null ? "sweep view: GamePlanner.ViewOf" : "sweep view: GameView.Of (no planner entry in this build)");
var vocabulary = units.Vocabulary;
var lines = new List<string>();
// The user (2026-10-05): the sweep runs over one game of each implemented scenario card, named on the command line.
var named = args.Length > 3 ? args[3].Split(',') : [];
IReadOnlyList<GameScope> games = named.Length == 0 ? store.List(tenant)
    : [.. store.List(tenant).Where(scope => named.Any(name => name.StartsWith('=') ? scope.Game == name[1..] : scope.Game == name || scope.Game.StartsWith(name + "-r", StringComparison.Ordinal)))];
// PROOF_EXCLUDE (after pass 55's gate): games left out by name, so that a baseline of the games made before a pass can be kept and reused.
if (Environment.GetEnvironmentVariable("PROOF_EXCLUDE") is { Length: > 0 } excluded)
{
    var gone = excluded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal);
    games = [.. games.Where(scope => !gone.Contains(scope.Game))];
}

if (mode == "cut")
{
    // cut <out> <storeRoot> <games> <every>: a copy of each named game at every Nth phase change, as <game>-r<revision>.
    // <every> is a number (every Nth phase change) or a phase name (every change into that phase).
    var every = int.TryParse(args[4], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var nth) ? nth : 0;
    var phase = every == 0 ? args[4] : null;
    var folder = Path.Combine(args[2], tenant.ToString("N"));
    var count = 0;
    foreach (var scope in games.Where(scope => named.Contains(scope.Game)))
    {
        var record = store.Read(scope)!;
        var changes = 0;
        for (var index = 0; index < record.Events.Count; index++)
        {
            if (record.Events[index].Payload is PhaseChanged changed && (phase is null ? changes++ % every == 0 : changed.Phase == phase))
            {
                var cut = new GameScope(tenant, $"{scope.Game}-r{index + 1}");
                File.WriteAllText(Path.Combine(folder, cut.Game + ".game.json"), GameEventWriter.Write(cut, record with { Events = [.. record.Events.Take(index + 1)] }));
                count++;
            }
        }
    }

    Console.WriteLine($"{count} cuts written");
    return;
}
var started = DateTime.Now;
var timings = new Dictionary<string, (long Ms, int Count)>(StringComparer.Ordinal);

// After pass 55's gate: the output is written a game at a time, so a run that stops loses one game; and a game that throws is one ERROR
// line, which the gate's trial and proofs refuse.
using var writer = new StreamWriter(output, false, new UTF8Encoding(false));
var flushed = 0;
async Task Guarded(GameScope scope, Func<Task> body)
{
    var mark = lines.Count;
    try
    {
        await body();
    }
    catch (Exception exception)
    {
        lines.RemoveRange(mark, lines.Count - mark);
        lines.Add($"{scope.Game}\tERROR\t{exception.GetType().Name}: {exception.Message.ReplaceLineEndings(" ")}");
    }

    for (; flushed < lines.Count; flushed++)
    {
        writer.WriteLine(lines[flushed]);
    }

    writer.Flush();
}

if (mode == "replay")
{
    // A cut is the first events of its game, and the game's own replay hashes every state the cut has: the replay passes over cuts
    // (73,175 of 93,401 lines at pass 55's gate). PROOF_REPLAY_CUTS=1 replays them as before.
    var replayCuts = Environment.GetEnvironmentVariable("PROOF_REPLAY_CUTS") == "1";
    foreach (var entry in games.Where(item => replayCuts || !System.Text.RegularExpressions.Regex.IsMatch(item.Game, "-r[0-9]+$")))
    {
        await Guarded(entry, () =>
        {
            ReplayOne(entry);
            return Task.CompletedTask;
        });
    }

    void ReplayOne(GameScope scope)
    {
        var record = store.Read(scope);
        if (record is null)
        {
            lines.Add($"{scope.Game}\tUNREADABLE");
            return;
        }

        var history = planner.Replay(record.Events);
        for (var index = 0; index < history.States.Count; index++)
        {
            lines.Add($"{scope.Game}\t{index + 1}\t{Dump.Hash(history.States[index])}");
            // PROOF_DUMP_STATE (pass 55): "<file>|<game>|<state number>" writes that one state in full, to name what differs when every hash does.
            if (Environment.GetEnvironmentVariable("PROOF_DUMP_STATE") is { Length: > 0 } dumpState && dumpState.Split('|') is [var dumpFile, var dumpGame, var dumpIndex]
                && dumpGame == scope.Game && dumpIndex == (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture))
            {
                File.WriteAllText(dumpFile, Dump.Full(history.States[index]));
            }
        }

        lines.Add($"{scope.Game}\tD\t{record.Events.Count}\t{history.States.Count}\t{string.Join(" || ", history.Diagnostics.Select(item => item.ToString()))}");
    }
}
else if (mode == "sweep")
{
    foreach (var entry in games)
    {
        await Guarded(entry, () => SweepOne(entry));
    }

    async Task SweepOne(GameScope scope)
    {
        // A game with an event type this side cannot read (pass 55's gate: the baseline's sweep ended in a null reference after 53 minutes).
        var record = store.Read(scope);
        if (record is null)
        {
            lines.Add($"{scope.Game}\tUNREADABLE");
            return;
        }

        var history = planner.Replay(record.Events);
        if (history.Current is not { } state)
        {
            lines.Add($"{scope.Game}\tNOSTATE");
            return;
        }

        var game = scope.Game;
        var expected = record.Events.Count;
        var onMap = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Position is MapPosition).ToArray();
        var at = onMap.ToDictionary(unit => unit.Id, unit => state.Location(unit.Id)!.Location);
        var neighbors = new Dictionary<BoardLocation, IReadOnlyList<BoardLocation>>();
        foreach (var location in at.Values.Distinct())
        {
            neighbors[location] = Read(game, "AdjacentLocations", location.ToString(), () => planner.AdjacentLocations(state, location)) as IReadOnlyList<BoardLocation> ?? [];
        }

        var sides = state.Sides.Select(side => side.Id).ToArray();
        var enemyLocations = onMap.GroupBy(unit => unit.Side).ToDictionary(group => group.Key, group => group.Select(unit => at[unit.Id]).Distinct().ToArray());

        // Per state.
        Read(game, "CloseCombatDue", "", () => planner.CloseCombatDue(state));
        Read(game, "BerserkOverrunPending", "", () => planner.BerserkOverrunPending(state));
        if (!game.Contains("-r", StringComparison.Ordinal))
        {
            Read(game, "Victory", "", () => planner.Victory(history));
        }
        Read(game, "CardSetup", "", () => planner.CardSetup(state, new HashSet<string>()));
        foreach (var edge in new[] { "top", "bottom", "left", "right" })
        {
            Read(game, "EntryHexes", edge, () => planner.EntryHexes(state, edge));
        }

        foreach (var perspective in state.Perspectives)
        {
            var outOfSight = Read(game, "OutOfSight", perspective.Name, () => planner.OutOfSight(state, perspective)) as IReadOnlySet<string>;
            // Pass 70 (ruling R70.19): the view through the planner's one entry where the build has it, so a view line that differs is 70.10's (the
            // Right of Inspection); a baseline without the entry builds the view as before. Bound once, as a delegate, so an exception is its own.
            Read(game, "GameView", perspective.Name, () => viewOf is not null ? viewOf(history, history.States.Count, perspective) : GameView.Of(history, history.States.Count, perspective, outOfSight));
            Read(game, "PhaseEndedBy", perspective.Name, () => GamePlanner.PhaseEndedBy(state, perspective.Name));
        }

        foreach (var side in sides)
        {
            Read(game, "NonObConcealment", side, () => planner.NonObConcealment(state, side));
            foreach (var location in neighbors.Keys)
            {
                Read(game, "KnownEnemyInLos", $"{side} {location}", () => planner.KnownEnemyInLos(state, side, location));
                Read(game, "CoverAt", $"{side} {location}", () => GamePlanner.CoverAt(state, location, side));
            }
        }

        foreach (var location in neighbors.Keys)
        {
            Read(game, "AdvanceLocations", location.ToString(), () => planner.AdvanceLocations(state, location));
            Read(game, "AmbushDue", location.ToString(), () => planner.AmbushDue(state, location));
            Read(game, "DeclaringSides", location.ToString(), () => planner.DeclaringSides(state, location, false));
            Read(game, "WrecksAt", location.ToString(), () => GamePlanner.WrecksAt(state, location));
            foreach (var next in neighbors[location])
            {
                Read(game, "Adjacent", $"{location} {next}", () => planner.Adjacent(state, location, next));
                Read(game, "Range", $"{location} {next}", () => planner.Range(state, location, next));
            }
        }

        // Per unit.
        foreach (var unit in onMap)
        {
            var id = unit.Id;
            var from = at[id];
            Read(game, "GoodOrder", id, () => GameState.GoodOrder(unit, vocabulary));
            Read(game, "Encircled", id, () => state.Encircled(unit));
            Read(game, "Inexperienced", id, () => Experience.Inexperienced(state, unit, catalogs, vocabulary));
            Read(game, "MoveAllowance", id, () => Experience.MoveAllowance(state, unit, catalogs, vocabulary));
            Read(game, "MustRout", id, () => planner.MustRout(state, unit));
            Read(game, "MayRout", id, () => planner.MayRout(state, unit));
            Read(game, "FireBar", id, () => GamePlanner.FireBar(state, unit));
            Read(game, "VictoryPoints", id, () => planner.VictoryPoints(state, unit));
            Read(game, "Captors", id, () => planner.Captors(state, unit));
            Read(game, "MustWithdraw", id, () => planner.MustWithdraw(state, unit));
            Read(game, "WithdrawalDestinations", id, () => planner.WithdrawalDestinations(state, unit, from));
            Read(game, "SmokeExponent", id, () => planner.SmokeExponent(unit));
            Read(game, "RoutLoadOf", id, () => planner.RoutLoadOf(state, unit));
            Read(game, "MayRetainDm", id, () => planner.MayRetainDm(state, id));
            Read(game, "IsArmoredVehicle", id, () => GamePlanner.IsArmoredVehicle(unit));
            Read(game, "HasVehicleMg", id, () => GamePlanner.HasVehicleMg(unit));
            Read(game, "IsClosedTopped", id, () => GamePlanner.IsClosedTopped(unit));
            Read(game, "IsTracked", id, () => GamePlanner.IsTracked(unit));
            Read(game, "MustLeave", id, () => GamePlanner.MustLeave(unit));
            Read(game, "MayStartVehicleMove", id, () => GamePlanner.MayStartVehicleMove(unit));
            Read(game, "MayChangeExposure", id, () => GamePlanner.MayChangeExposure(unit));
            Read(game, "VehicleExits", id, () => planner.VehicleExits(state, unit));
            Read(game, "VehicleVcaHexes", id, () => planner.VehicleVcaHexes(state, unit));
            Read(game, "PassengerCapacity", id, () => GamePlanner.PassengerCapacity(state, unit));
            Read(game, "OpenEntryHexes", id, () => planner.OpenEntryHexes(state, unit));
            if (neighbors[from].Count > 0)
            {
                var next = neighbors[from][0];
                Read(game, "EntryFacts", $"{id} {next}", () => planner.EntryFacts(state, unit, next));
            }
        }

        foreach (var item in state.Equipment.Where(item => item.Status == InstanceStatus.Active))
        {
            Read(game, "PortageOf", item.Id, () => planner.PortageOf(item));
            Read(game, "MayDismantle", item.Id, () => planner.MayDismantle(state, item.Id));
            if (neighbors.Keys.FirstOrDefault() is { } location)
            {
                Read(game, "GunTargetStatus", $"{item.Id} {location}", () => planner.GunTargetStatus(state, item.Id, location));
            }
        }

        // Requests. A truncated fixture (a phase change) takes one request of each kind per unit; a whole game takes every neighbor and target.
        // The user (2026-10-05): one request of each kind per unit at every state; the per-neighbor fan-out is off.
        var full = false;
        await Act(game, GameActions.AdvancePhase, expected, new());
        await Act(game, GameActions.EndMove, expected, new());
        await Act(game, GameActions.PassFire, expected, new());
        await Act(game, GameActions.Overrun, expected, new());
        foreach (var unit in onMap)
        {
            var id = unit.Id;
            var from = at[id];
            await Act(game, GameActions.Rally, expected, new() { ["unitId"] = id });
            await Act(game, GameActions.OpportunityFire, expected, new() { ["unitIds"] = new[] { id } });
            await Act(game, GameActions.Move, expected, new() { ["unitIds"] = new[] { id }, ["to"] = from.ToString(), ["smoke"] = from.ToString(), ["smokeBy"] = id });
            await Act(game, GameActions.RecoverShock, expected, new() { ["vehicleId"] = id });
            await Act(game, GameActions.ButtonUp, expected, new() { ["vehicleId"] = id, ["buttonedUp"] = true });
            await Act(game, GameActions.MoveVehicle, expected, new() { ["vehicleId"] = id, ["kind"] = "start" });
            await Act(game, GameActions.MoveVehicle, expected, new() { ["vehicleId"] = id, ["kind"] = "stop" });
            foreach (var next in full ? neighbors[from] : neighbors[from].Take(1))
            {
                await Act(game, GameActions.Move, expected, new() { ["unitIds"] = new[] { id }, ["to"] = next.ToString() });
                await Act(game, GameActions.Move, expected, new() { ["unitIds"] = new[] { id }, ["to"] = next.ToString(), ["assault"] = true });
                await Act(game, GameActions.Move, expected, new() { ["unitIds"] = new[] { id }, ["to"] = from.ToString(), ["smoke"] = next.ToString(), ["smokeBy"] = id });
                await Act(game, GameActions.Advance, expected, new() { ["unitIds"] = new[] { id }, ["to"] = next.ToString() });
                await Act(game, GameActions.Rout, expected, new() { ["unitId"] = id, ["route"] = new[] { next.ToString() } });
                await Act(game, GameActions.MoveVehicle, expected, new() { ["vehicleId"] = id, ["kind"] = "enter", ["to"] = next.ToString() });
            }

            foreach (var side in sides.Where(side => side != unit.Side))
            {
                foreach (var target in full ? enemyLocations.GetValueOrDefault(side, []) : enemyLocations.GetValueOrDefault(side, []).Take(1))
                {
                    await Act(game, GameActions.Fire, expected, new() { ["firers"] = new[] { id }, ["target"] = target.ToString() });
                    await Act(game, GameActions.FireOrdnance, expected, new() { ["gunId"] = id, ["target"] = target.ToString() });
                }
            }
        }

        foreach (var location in neighbors.Keys)
        {
            await Act(game, GameActions.Ambush, expected, new() { ["location"] = location.ToString() });
            await Act(game, GameActions.VehicleCloseCombat, expected, new() { ["location"] = location.ToString(), ["pass"] = true });
        }

        foreach (var item in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { } holding))
        {
            await Act(game, GameActions.Repair, expected, new() { ["unitId"] = item.Holding!.Holder, ["equipmentId"] = item.Id });
            await Act(game, GameActions.Drop, expected, new() { ["unitId"] = item.Holding!.Holder, ["equipmentId"] = item.Id });
            await Act(game, GameActions.Dismantle, expected, new() { ["unitId"] = item.Holding!.Holder, ["equipmentId"] = item.Id });
            foreach (var target in full ? neighbors.Keys : neighbors.Keys.Take(1))
            {
                await Act(game, GameActions.FireOrdnance, expected, new() { ["gunId"] = item.Id, ["target"] = target.ToString() });
            }
        }

        Console.WriteLine($"{game}: {lines.Count} lines so far, {(DateTime.Now - started).TotalSeconds:F0} s");
    }
}

writer.Flush();
Console.WriteLine($"{lines.Count} lines -> {output} in {(DateTime.Now - started).TotalSeconds:F0} s");
foreach (var (name, (ms, count)) in timings.OrderByDescending(item => item.Value.Ms).Take(25))
{
    Console.WriteLine($"  {ms,8} ms {count,6} x  {name}");
}

void Time(string name, long ms)
{
    var (total, count) = timings.GetValueOrDefault(name);
    timings[name] = (total + ms, count + 1);
}

object? Read(string game, string name, string key, Func<object?> read)
{
    var watch = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        return ReadInner(game, name, key, read);
    }
    finally
    {
        Time("READ " + name, watch.ElapsedMilliseconds);
    }
}

object? ReadInner(string game, string name, string key, Func<object?> read)
{
    object? value;
    string text;
    try
    {
        value = read();
        text = Dump.Text(value);
    }
    catch (Exception exception)
    {
        value = null;
        text = $"THROWS {exception.GetType().Name}: {exception.Message.ReplaceLineEndings(" ")}";
    }

    lines.Add($"{game}\tREAD\t{name}\t{key}\t{text}");
    return value;
}

async Task Act(string game, ActionDescriptor action, long expected, Dictionary<string, object?> arguments)
{
    var watch = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        await ActInner(game, action, expected, arguments);
    }
    finally
    {
        Time("ACT " + action.Id.Value, watch.ElapsedMilliseconds);
    }
}

async Task ActInner(string game, ActionDescriptor action, long expected, Dictionary<string, object?> arguments)
{
    arguments["gameId"] = game;
    arguments["attemptId"] = "sweep-1";
    arguments["expectedRevision"] = expected;
    var key = string.Join(" ", arguments.Where(item => item.Key is not ("gameId" or "attemptId" or "expectedRevision")).Select(item => $"{item.Key}={Dump.Text(item.Value)}"));
    string text;
    try
    {
        var plan = await planner.PlanAsync(action, JsonSerializer.SerializeToElement(arguments), tenant);
        text = $"{plan.Status}\t{string.Join(" || ", plan.Reasons)}\t{EventsDigest(plan.Events)}\t{Dump.Hash(plan.Fire)}/{Dump.Hash(plan.Disclosure)}/{Dump.Hash(plan.Entry)}";
        if (Environment.GetEnvironmentVariable("PROOF_DUMP_FIRE") is { Length: > 0 } dumpFire && plan.Fire is not null) { File.AppendAllText(dumpFire, game + " " + key + Environment.NewLine + Dump.Full(plan.Fire) + Environment.NewLine); }
        // PROOF_DUMP_EVENTS (pass 55's gate): a plan's events in full, to name what differs when a plan's text is the same and its hash is not.
        if (Environment.GetEnvironmentVariable("PROOF_DUMP_EVENTS") is { Length: > 0 } dumpEvents && plan.Events.Count > 0) { File.AppendAllText(dumpEvents, "KEY " + game + " " + key + Environment.NewLine + string.Join(Environment.NewLine, plan.Events.Select(item => Dump.Full(item))) + Environment.NewLine); }
        if (plan.Roll is { } roll)
        {
            var drawn = 0;
            RollResult Draw(RollRequest request)
            {
                var values = Enumerable.Range(0, request.Count).Select(_ => (drawn++ % request.Sides) + 1).ToArray();
                return (RollResult)Activator.CreateInstance(typeof(RollResult), BindingFlags.NonPublic | BindingFlags.Instance, null, [request, values], null)!;
            }

            string rolled;
            try
            {
                rolled = EventsDigest(roll.Build(Draw));
            }
            catch (Exception exception)
            {
                rolled = $"THROWS {exception.GetType().Name}: {exception.Message.ReplaceLineEndings(" ")}";
            }

            text += $"\t{roll.Purpose}\t{rolled}\t{drawn}";
        }
    }
    catch (Exception exception)
    {
        text = $"THROWS {exception.GetType().Name}: {exception.Message.ReplaceLineEndings(" ")}";
    }

    lines.Add($"{game}\tACT\t{action.Id.Value}\t{key}\t{text}");
}

static string EventsDigest(IReadOnlyList<GameEvent> events)
{
    var text = new StringBuilder();
    foreach (var item in events)
    {
        text.Append(item.Type).Append(' ').Append(item.EventId).Append(' ').Append(item.Revision).Append(' ').Append(Dump.Skips("GameEvent.RulePackage") ? "-" : item.RulePackage?.ToString()).Append(' ')
            .Append(string.Join(",", item.Causes)).Append(' ').Append(item.Visibility is null ? "-" : string.Join(",", item.Visibility)).Append(' ').Append(Dump.Text(item.Payload)).Append('\n');
    }

    return events.Count + ":" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())))[..16];
}

internal static class Dump
{
    private static readonly HashSet<string> Skipped = [.. (Environment.GetEnvironmentVariable("PROOF_SKIP") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    // Pass 55's gate: a planned event's rule package reference is written outside the property walk; this lets PROOF_SKIP name it.
    public static bool Skips(string name) => Skipped.Contains(name);

    public static string Hash(object? value)
    {
        var text = new StringBuilder();
        Write(value, text, 0);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())))[..12];
    }

    public static string Full(object? value)
    {
        var text = new StringBuilder();
        Write(value, text, 0);
        return text.ToString();
    }

    public static string Text(object? value)
    {
        var text = new StringBuilder();
        Write(value, text, 0);
        var result = text.ToString().ReplaceLineEndings(" ");
        return result.Length <= 2000 ? result : result[..1900] + " #" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(result)))[..16];
    }

    private static void Write(object? value, StringBuilder text, int depth)
    {
        if (value is null)
        {
            text.Append("null");
            return;
        }

        if (depth > 24)
        {
            text.Append("<deep>");
            return;
        }

        var type = value.GetType();
        if (value is string || type.IsPrimitive || type.IsEnum || value is decimal || value is Guid || value is DateTimeOffset || value is DateTime)
        {
            text.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        if (value is JsonElement json)
        {
            // PROOF_SKIP_JSON (pass 45): keys a pass added to a record kept as JSON, left out of the raw text with their flat values.
            var raw = json.GetRawText();
            foreach (var key in (Environment.GetEnvironmentVariable("PROOF_SKIP_JSON") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                // Pass 50: "key{}" leaves out only a flat object under the key, "key[{}]" only an array of one or more flat objects, so that an
                // older key of the same name (a fire request's "weapons", an array of strings) stays in the text.
                const string FlatObject = "\\{[^\\[\\]{}]*\\}";
                var (skipKey, shape) = key.EndsWith("[{}]", StringComparison.Ordinal) ? (key[..^4], "\\[" + FlatObject + "(?:," + FlatObject + ")*\\]")
                    : key.EndsWith("{}", StringComparison.Ordinal) ? (key[..^2], FlatObject)
                    : (key, "true|false|null|-?[0-9.]+|\"[^\"]*\"|\\[[^\\[\\]{}]*\\]|" + FlatObject);
                var flat = "\"" + System.Text.RegularExpressions.Regex.Escape(skipKey) + "\":(?:" + shape + ")";
                raw = System.Text.RegularExpressions.Regex.Replace(raw, "," + flat, string.Empty);
                raw = System.Text.RegularExpressions.Regex.Replace(raw, flat + ",?", string.Empty);
            }

            text.Append(raw);
            return;
        }

        if (value is Delegate)
        {
            text.Append("<delegate>");
            return;
        }

        if (value is IDictionary dictionary)
        {
            var entries = new List<string>();
            foreach (DictionaryEntry entry in dictionary)
            {
                var item = new StringBuilder();
                Write(entry.Key, item, depth + 1);
                item.Append('=');
                Write(entry.Value, item, depth + 1);
                entries.Add(item.ToString());
            }

            entries.Sort(StringComparer.Ordinal);
            text.Append('{').AppendJoin(',', entries).Append('}');
            return;
        }

        if (value is IEnumerable sequence)
        {
            text.Append('[');
            foreach (var item in sequence)
            {
                Write(item, text, depth + 1);
                text.Append(',');
            }

            text.Append(']');
            return;
        }

        if (type.Namespace is { } name && !name.StartsWith("LimboDancer", StringComparison.Ordinal))
        {
            text.Append(value);
            return;
        }

        text.Append(type.Name).Append('(');
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(item => item.GetIndexParameters().Length == 0 && item.Name != "EqualityContract" && !Skipped.Contains(type.Name + "." + item.Name))
            .OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            text.Append(property.Name).Append(':');
            object? inner;
            try
            {
                inner = property.GetValue(value);
            }
            catch (TargetInvocationException)
            {
                inner = "<throws>";
            }

            Write(inner, text, depth + 1);
            text.Append(';');
        }

        text.Append(')');
    }
}
