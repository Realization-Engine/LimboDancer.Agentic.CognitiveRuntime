using System.Diagnostics;
using System.Text;
using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The live game source chosen for D2 (Governed Writes Design, section 3): games set up and played in the Studio, kept
/// under <c>{boards folder}/units/live</c>, and changed only through the governed path. The Studio has one local user,
/// who holds the setup and play permissions and confirms each write. A test may supply the dice roller; the Studio uses
/// the default.
/// </summary>
public sealed class LivePlay
{
    /// <summary>The tenant of the Studio's local user.</summary>
    public static readonly Guid Tenant = Guid.Parse("5a7d1f00-0000-4000-8000-0000000057d0");

    private readonly ILogger<LivePlay>? logger;

    public LivePlay(UnitLibrary units, IBoardProvider boards, DiceRoller? roller = null, ILogger<LivePlay>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(units);
        this.logger = logger;
        Root = Path.Combine(units.UnitsRoot, "live");
        Store = new FileGameStore(Root);
        Catalogs = [.. UnitCatalogs.Names.Select(name => UnitCatalogs.Read(name, units.Vocabulary)?.Catalog).OfType<UnitCatalog>()];

        // Pass 22 (ruling R22.2): the user's cards are saved beside the saved maps, under the boards root.
        Cards = new ScenarioCardLibrary(Path.Combine(Path.GetDirectoryName(units.UnitsRoot)!, "cards"));
        Footprints = new CampaignPlayableFootprints(Path.GetDirectoryName(units.UnitsRoot)!);
        Planner = new GamePlanner(Store, new StudioBoardCatalog(boards, Footprints), units.Vocabulary, Catalogs, cardLibrary: Cards);
        Audit = new FileAuditSink(Path.Combine(Root, "audit.jsonl"));
        Play = new GamePlay(Planner, Store, Audit, roller: roller);
        Plans = new MovementPlans(this);
    }

    /// <summary>The movement plans being made, one per game (the Play streamlining plan, S6); kept as long as this service, never in a game.</summary>
    public MovementPlans Plans
    {
        get;
    }

    /// <summary>The first side's CC declarations of a simultaneous round, one per game, until the other side declares (A11.11); never in a game.</summary>
    public CloseCombatDeclarations Declarations
    {
        get;
    } = new();

    public CampaignPlayableFootprints Footprints
    {
        get;
    }

    public IReadOnlyList<UnitCatalog> Catalogs
    {
        get;
    }

    public string Root
    {
        get;
    }

    /// <summary>The scenario cards a game starts from: the built-in cards and the user's (ruling R22.2).</summary>
    public ScenarioCardLibrary Cards
    {
        get;
    }

    public IGameStore Store
    {
        get;
    }

    public GamePlanner Planner
    {
        get;
    }

    public GamePlay Play
    {
        get;
    }

    public FileAuditSink Audit
    {
        get;
    }

    public RuntimePrincipal Principal { get; } = GamePlay.Principal("studio-user", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    public IReadOnlyList<GameScope> Games() => Store.List(Tenant);

    /// <summary>
    /// The game-to-card index (pass 28, ruling R28.4): the live games whose <c>game-started</c> names the card, read from each game's record on request,
    /// with nothing stored beside them.
    /// </summary>
    public IReadOnlyList<string> GamesFrom(string card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return [.. Games().Where(scope => Store.Read(scope)?.Events.Select(item => item.Payload).OfType<GameStarted>().FirstOrDefault()?.Scenario?.Id == card)
            .Select(scope => scope.Game).Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// A new game that plays on from a moment of another (pass 31b, design D8): the other game's events through a revision, under a new name, with a
    /// label that says where it came from. The first game is only read. The new game's file is written whole by the store, which replays the events
    /// first and refuses a name that is taken or is not a lowercase slug; nothing is drawn, and the dice from there on are the new game's own.
    /// </summary>
    public AppendResult PlayOn(string source, long revision, string name, string origin)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(origin);
        if (Store.Read(new GameScope(Tenant, source)) is not { } record || revision < 1 || revision > record.Events.Count)
        {
            return new AppendResult(AppendStatus.Invalid, 0, [UnitDiagnostic.Error("PLAY-ON-001", $"There is no game '{source}' with a revision {revision}.")]);
        }

        var scope = new GameScope(Tenant, name);
        if (Store.Read(scope) is not null)
        {
            return new AppendResult(AppendStatus.Invalid, 0, [UnitDiagnostic.Error("PLAY-ON-002", $"A game named '{name}' exists already: give another name.")]);
        }

        GameEvent[] events = [.. record.Events.Take((int)revision).Select(item => new GameEvent(scope, item.EventId, item.Revision, item.Time, item.Source, item.Type,
            item.Payload, item.RulePackage, item.Causes, item.Visibility))];
        return Store.Append(scope, $"{record.Label} ({origin})", 0, events, Planner.Replay);
    }

    /// <summary>
    /// A new game that follows a recorded one (the SVG memory analysis, section 13): it starts with the record's first step, as play on does, and
    /// <see cref="FollowStep"/> adds the record's steps to it one at a time, so the Play page does on each exactly the work it does after a
    /// confirm. A development aid, offered where scripted dice are; the record is only read.
    /// </summary>
    public AppendResult StartFollowing(string source, string name)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(name);
        if (Store.Read(new GameScope(Tenant, source)) is not { Events.Count: > 0 } record)
        {
            return new AppendResult(AppendStatus.Invalid, 0, [UnitDiagnostic.Error("FOLLOW-001", $"There is no recorded game '{source}' to follow.")]);
        }

        return PlayOn(source, StepEnd(record.Events, 0), name, $"following {source}");
    }

    /// <summary>
    /// Adds the record's next step to a game that follows it: the events from the follower's revision that share one attempt, which is one step of
    /// the replay (<see cref="ReplaySteps.AttemptOf"/>). Refused when the follower is no longer the record's beginning (it was played on by hand) and
    /// when the record has no step left.
    /// </summary>
    public AppendResult FollowStep(string source, string follower)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(follower);
        var scope = new GameScope(Tenant, follower);
        if (Store.Read(new GameScope(Tenant, source)) is not { } record || Store.Read(scope) is not { } copy)
        {
            return new AppendResult(AppendStatus.Invalid, 0, [UnitDiagnostic.Error("FOLLOW-001", $"There is no game '{source}' followed by '{follower}'.")]);
        }

        var (all, have) = (record.Events, copy.Events);
        for (var index = 0; index < have.Count; index++)
        {
            if (index >= all.Count || have[index].EventId != all[index].EventId)
            {
                return new AppendResult(AppendStatus.Invalid, have.Count,
                    [UnitDiagnostic.Error("FOLLOW-002", $"'{follower}' left the record of '{source}' at revision {index + 1}: it is played on by hand now.")]);
            }
        }

        if (have.Count >= all.Count)
        {
            return new AppendResult(AppendStatus.Invalid, have.Count, [UnitDiagnostic.Error("FOLLOW-003", $"'{follower}' has every step of '{source}'.")]);
        }

        GameEvent[] events = [.. all.Skip(have.Count).Take(StepEnd(all, have.Count) - have.Count).Select(item => new GameEvent(scope, item.EventId,
            item.Revision, item.Time, item.Source, item.Type, item.Payload, item.RulePackage, item.Causes, item.Visibility))];
        return Store.Append(scope, copy.Label, have.Count, events, Planner.Replay);
    }

    /// <summary>The revision that ends the step starting at an index: the events from it that share its attempt.</summary>
    private static int StepEnd(IReadOnlyList<GameEvent> events, int from)
    {
        var attempt = ReplaySteps.AttemptOf(events[from].EventId);
        var end = from + 1;
        while (end < events.Count && ReplaySteps.AttemptOf(events[end].EventId) == attempt)
        {
            end++;
        }

        return end;
    }

    /// <summary>
    /// The names a view reads for a game's units (pass 31c, design D11), with the planner's view of each state: who sets up out of sight (ruling
    /// R23.3) and the stacks the viewer may not inspect (ruling R70.18).
    /// </summary>
    public UnitNames NamesOf(GameHistory history, Perspective viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var planner = Planner;
        return UnitNames.For(history, viewer, Catalogs, state => planner.ViewOf(state, viewer, []));
    }

    /// <summary>A live game's history, replayed against the exact boards in play; null when the game does not exist.</summary>
    public GameHistory? History(string game)
    {
        ArgumentNullException.ThrowIfNull(game);
        var timer = Stopwatch.StartNew();
        var record = Store.Read(new GameScope(Tenant, game));
        LoadMeasurements.Record(logger, game, "read", timer.Elapsed.TotalMilliseconds);
        timer.Restart();
        var history = record is not null ? Planner.Replay(record.Events) : null;
        LoadMeasurements.Record(logger, game, "replay including board load", timer.Elapsed.TotalMilliseconds);
        return history;
    }
}

/// <summary>An audit sink that appends each runtime audit event to a JSON lines file.</summary>
public sealed class FileAuditSink(string path) : IAuditSink
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };
    private readonly object gate = new();

    public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        var line = JsonSerializer.Serialize(new
        {
            type = auditEvent.EventType.ToString(),
            time = auditEvent.OccurredAt,
            correlation = auditEvent.CorrelationId.Value,
            principal = auditEvent.PrincipalId,
            action = auditEvent.ActionId?.Value,
            gate = auditEvent.ExecutionGateOutcome?.ToString(),
            outcome = auditEvent.OutcomeCode ?? auditEvent.ExecutionCode,
            reasons = auditEvent.ReasonCodes,
        }, Options);
        lock (gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, line + "\n", new UTF8Encoding(false));
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The last audit lines, newest last. Read back from the file's end, a block at a time, until the lines asked for are in hand (the Play Step
    /// Plan, task 5): the adjudicator's audit panel asks on every render, and the log only grows. Read from the file, not kept in memory, so the
    /// lines another Studio writes to the shared folder are read too. The lines are split as <see cref="File.ReadLines(string)"/> splits them.
    /// </summary>
    public IReadOnlyList<string> Tail(int count)
    {
        lock (gate)
        {
            if (count <= 0 || !File.Exists(path))
            {
                return [];
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var end = stream.Length;
            var from = TailStart(stream, end, count);
            stream.Position = from;
            var bytes = new byte[end - from];
            stream.ReadExactly(bytes);
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: from == 0);
            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
            {
                lines.Add(line);
            }

            return [.. lines.TakeLast(count)];
        }
    }

    // Where the last count lines begin: just after the newline that has count newlines after it, leaving out one that ends the file; or the
    // file's start. A newline byte is never inside a UTF-8 sequence, so the text after it decodes on its own.
    private static long TailStart(FileStream stream, long end, int count)
    {
        var buffer = new byte[8192];
        var start = end;
        var newlines = 0;
        while (start > 0)
        {
            var size = (int)Math.Min(buffer.Length, start);
            start -= size;
            stream.Position = start;
            stream.ReadExactly(buffer, 0, size);
            for (var index = size - 1; index >= 0; index--)
            {
                if (buffer[index] == (byte)'\n' && start + index != end - 1 && ++newlines > count)
                {
                    return start + index + 1;
                }
            }
        }

        return 0;
    }
}
