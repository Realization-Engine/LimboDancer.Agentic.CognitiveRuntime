using System.Text;
using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Play;
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

    public LivePlay(UnitLibrary units, IBoardProvider boards, DiceRoller? roller = null)
    {
        ArgumentNullException.ThrowIfNull(units);
        Root = Path.Combine(units.UnitsRoot, "live");
        Store = new FileGameStore(Root);
        Catalogs = [.. UnitCatalogs.Names.Select(name => UnitCatalogs.Read(name, units.Vocabulary)?.Catalog).OfType<UnitCatalog>()];

        // Pass 22 (ruling R22.2): the user's cards are saved beside the saved maps, under the boards root.
        Cards = new ScenarioCardLibrary(Path.Combine(Path.GetDirectoryName(units.UnitsRoot)!, "cards"));
        Planner = new GamePlanner(Store, new StudioBoardCatalog(boards), units.Vocabulary, Catalogs, cardLibrary: Cards);
        Audit = new FileAuditSink(Path.Combine(Root, "audit.jsonl"));
        Play = new GamePlay(Planner, Store, Audit, roller: roller);
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
