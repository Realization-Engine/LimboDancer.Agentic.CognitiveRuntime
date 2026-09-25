using System.Text;
using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The live game source chosen for D2 (Governed Writes Design, section 3): games set up and played in the Studio, kept
/// under <c>{boards folder}/units/live</c>, and changed only through the governed path. The Studio has one local user,
/// who holds the setup and play permissions and confirms each write.
/// </summary>
public sealed class LivePlay
{
    /// <summary>The tenant of the Studio's local user.</summary>
    public static readonly Guid Tenant = Guid.Parse("5a7d1f00-0000-4000-8000-0000000057d0");

    public LivePlay(UnitLibrary units, IBoardProvider boards)
    {
        ArgumentNullException.ThrowIfNull(units);
        Root = Path.Combine(units.UnitsRoot, "live");
        Store = new FileGameStore(Root);
        Catalogs = [.. UnitCatalogs.Names.Select(name => UnitCatalogs.Read(name, units.Vocabulary)?.Catalog).OfType<UnitCatalog>()];
        Planner = new GamePlanner(Store, new StudioBoardCatalog(boards), units.Vocabulary, Catalogs);
        Audit = new FileAuditSink(Path.Combine(Root, "audit.jsonl"));
        Play = new GamePlay(Planner, Store, Audit);
    }

    public IReadOnlyList<UnitCatalog> Catalogs
    {
        get;
    }

    public string Root
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

    /// <summary>A live game's history, replayed against the exact boards in play; null when the game does not exist.</summary>
    public GameHistory? History(string game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return Store.Read(new GameScope(Tenant, game)) is { } record ? Planner.Replay(record.Events) : null;
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

    /// <summary>The last audit lines, newest last.</summary>
    public IReadOnlyList<string> Tail(int count)
    {
        lock (gate)
        {
            return File.Exists(path) ? [.. File.ReadLines(path).TakeLast(count)] : [];
        }
    }
}
