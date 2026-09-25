using LimboDancer.Dice;
using System.Collections.Concurrent;
using System.Text;
using LimboDancer.Domains.Asl.Units;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>The live game source chosen for D2 on 2026-09-26: games set up and played in Map Studio.</summary>
public static class LiveGames
{
    /// <summary>The event source every live event carries, and the projector's accepted live source.</summary>
    public const string Source = "map-studio";

    public static IReadOnlyCollection<string> Sources { get; } = [Source];
}

public enum AppendStatus
{
    /// <summary>The events were validated and written atomically.</summary>
    Committed,

    /// <summary>The events were already written by the same attempt; nothing changed.</summary>
    Replay,

    /// <summary>The game is not at the expected revision; nothing changed.</summary>
    Stale,

    /// <summary>The events would break a rule of the model; nothing changed.</summary>
    Invalid,
}

public sealed record AppendResult(AppendStatus Status, long Revision, IReadOnlyList<UnitDiagnostic> Diagnostics);

/// <summary>
/// A roll an action needs (Random Selection and Declined OVR Design, section 3): what it is for, what to draw, and a pure
/// function from the drawn result to the complete batch of events. The store draws it inside its commit, never before.
/// </summary>
public sealed record PlannedRoll(string Purpose, RollRequest Request, Func<RollResult, IReadOnlyList<GameEvent>> Build);

/// <summary>
/// Where live games are kept (ASL-UNIT-040, 042): one ordered event log per game. Appends are atomic, happen only at
/// the expected revision, and only when the whole log still replays without error.
/// </summary>
public interface IGameStore
{
    public GameRecord? Read(GameScope scope);

    public IReadOnlyList<GameScope> List(Guid tenant);

    public AppendResult Append(GameScope scope, string label, long expectedRevision, IReadOnlyList<GameEvent> events,
        Func<IReadOnlyList<GameEvent>, GameHistory> replay);

    /// <summary>
    /// Draws a planned roll and appends the events built from it, atomically (DICE-08, DICE-10, DICE-11): a committed
    /// attempt is a Replay and draws nothing, a stale revision draws nothing, and a draw whose events are not written
    /// leaves no trace.
    /// </summary>
    public AppendResult AppendRolled(GameScope scope, string label, long expectedRevision, string firstEventId, PlannedRoll roll, DiceRoller roller,
        Func<IReadOnlyList<GameEvent>, GameHistory> replay);
}

/// <summary>
/// A game store on disk: <c>{root}/{tenant}/{game}.game.json</c>, written in the game record format. A write replaces
/// the file through a temporary file, so a reader never sees half a log.
/// </summary>
public sealed class FileGameStore(string root) : IGameStore
{
    private const string Suffix = ".game.json";
    private static readonly ConcurrentDictionary<string, object> Locks = new(StringComparer.OrdinalIgnoreCase);

    public GameRecord? Read(GameScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var path = PathOf(scope);
        return File.Exists(path) ? GameEventReader.Read(File.ReadAllBytes(path)).Record : null;
    }

    public IReadOnlyList<GameScope> List(Guid tenant)
    {
        var folder = Path.Combine(root, tenant.ToString("N"));
        return Directory.Exists(folder)
            ? [.. Directory.EnumerateFiles(folder, "*" + Suffix).Select(path => new GameScope(tenant, Path.GetFileName(path)[..^Suffix.Length]))
                .OrderBy(scope => scope.Game, StringComparer.Ordinal)]
            : [];
    }

    public AppendResult Append(GameScope scope, string label, long expectedRevision, IReadOnlyList<GameEvent> events,
        Func<IReadOnlyList<GameEvent>, GameHistory> replay)
    {
        ArgumentNullException.ThrowIfNull(events);
        return Commit(scope, label, expectedRevision, events.Count > 0 ? events[0].EventId : null, () => events, replay);
    }

    public AppendResult AppendRolled(GameScope scope, string label, long expectedRevision, string firstEventId, PlannedRoll roll, DiceRoller roller,
        Func<IReadOnlyList<GameEvent>, GameHistory> replay)
    {
        ArgumentNullException.ThrowIfNull(firstEventId);
        ArgumentNullException.ThrowIfNull(roll);
        ArgumentNullException.ThrowIfNull(roller);

        // The draw happens here, inside the lock and after the attempt and revision checks, and nowhere else.
        return Commit(scope, label, expectedRevision, firstEventId, () => roll.Build(roller.Roll(roll.Request)), replay);
    }

    private AppendResult Commit(GameScope scope, string label, long expectedRevision, string? firstEventId, Func<IReadOnlyList<GameEvent>> events,
        Func<IReadOnlyList<GameEvent>, GameHistory> replay)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(replay);
        if (!VocabularyNamesSlug(scope.Game))
        {
            return new AppendResult(AppendStatus.Invalid, 0, [UnitDiagnostic.Error("PLAY-001", $"The game id '{scope.Game}' must be a lowercase slug.")]);
        }

        var path = PathOf(scope);
        lock (Locks.GetOrAdd(path, _ => new object()))
        {
            var current = File.Exists(path) ? GameEventReader.Read(File.ReadAllBytes(path)).Record : null;
            var existing = current?.Events ?? [];
            if (firstEventId is not null && existing.Any(item => item.EventId == firstEventId))
            {
                return new AppendResult(AppendStatus.Replay, existing.Count, []);
            }

            if (existing.Count != expectedRevision)
            {
                return new AppendResult(AppendStatus.Stale, existing.Count,
                    [UnitDiagnostic.Error("PLAY-002", $"The game is at revision {existing.Count}, not the expected {expectedRevision}.")]);
            }

            var batch = events();
            if (firstEventId is not null && (batch.Count == 0 || batch[0].EventId != firstEventId))
            {
                return new AppendResult(AppendStatus.Invalid, existing.Count,
                    [UnitDiagnostic.Error("PLAY-003", $"The events built for the attempt must start with '{firstEventId}'.")]);
            }

            GameEvent[] all = [.. existing, .. batch];
            var history = replay(all);
            if (history.HasErrors)
            {
                return new AppendResult(AppendStatus.Invalid, existing.Count, history.Diagnostics);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, GameEventWriter.Write(scope, new GameRecord(current?.Label ?? label, Synthetic: false, all)), new UTF8Encoding(false));
            File.Move(temporary, path, overwrite: true);
            return new AppendResult(AppendStatus.Committed, all.Length, []);
        }
    }

    private string PathOf(GameScope scope) => Path.Combine(root, scope.Tenant.ToString("N"), scope.Game + Suffix);

    private static bool VocabularyNamesSlug(string text) => Units.Vocabulary.VocabularyNames.IsSlug(text);
}
