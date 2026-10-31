using LimboDancer.Dice;
using System.Collections.Concurrent;
using System.Security.Cryptography;
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
/// The rolls an action needs (Infantry OVR Design, section 3): what they are for, and a pure function that builds the
/// complete batch of events, asking the draw function for each roll only when the outcome so far needs it. The store
/// calls it inside its commit, never before, so every roll is drawn there and nothing is drawn for a branch not taken.
/// </summary>
public sealed record PlannedRoll(string Purpose, Func<Func<RollRequest, RollResult>, IReadOnlyList<GameEvent>> Build);

/// <summary>A rule could not resolve facts learned while building an atomic rolled batch; none of that batch may be written.</summary>
internal sealed class GameBuildRefusedException(string reason) : Exception(reason);

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

    /// <summary>
    /// The minigame bundle a game was started from, or null (the Rules-first plan, section 7.1; ruling RPS.10): what the lists ask of every game to leave
    /// minigames out. A store may answer from the whole record; the file store reads only the file's header.
    /// </summary>
    public string? MinigameOf(GameScope scope) => Read(scope)?.Minigame;
}

/// <summary>
/// A game store on disk: <c>{root}/{tenant}/{game}.game.json</c>, written in the game record format. A write replaces
/// the file through a temporary file, so a reader never sees half a log.
/// </summary>
public sealed class FileGameStore(string root) : IGameStore
{
    private const string Suffix = ".game.json";
    private static readonly ConcurrentDictionary<string, object> Locks = new(StringComparer.OrdinalIgnoreCase);

    // Pass 31d (design D1): the record last parsed from each file, with the SHA-256 of the bytes it was parsed from. While a file's bytes are the
    // same, a read gives the same event objects, which is how the planner knows the events it has replayed already.
    private const int KeptRecords = 32;
    private static readonly ConcurrentDictionary<string, (byte[] Hash, GameRecord? Record)> Parsed = new(StringComparer.OrdinalIgnoreCase);

    public GameRecord? Read(GameScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return Parse(PathOf(scope));
    }

    // The proving slice (ruling RPS.10; the memory and performance guidelines): each file's "minigame" read from its header, kept while the file's time
    // and length are those it was read at, so a list of games opens no whole record.
    private static readonly ConcurrentDictionary<string, (DateTime Time, long Length, string? Minigame)> Headers = new(StringComparer.OrdinalIgnoreCase);

    public string? MinigameOf(GameScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var file = new FileInfo(PathOf(scope));
        if (!file.Exists)
        {
            return null;
        }

        if (Headers.TryGetValue(file.FullName, out var kept) && kept.Time == file.LastWriteTimeUtc && kept.Length == file.Length)
        {
            return kept.Minigame;
        }

        var minigame = HeaderMinigame(file.FullName);
        Headers[file.FullName] = (file.LastWriteTimeUtc, file.Length, minigame);
        return minigame;
    }

    /// <summary>The "minigame" of a game file's header: read up to the "events" array and no further; null when there is none before it.</summary>
    private static string? HeaderMinigame(string path)
    {
        var buffer = new byte[4096];
        int read;
        using (var stream = File.OpenRead(path))
        {
            read = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        }

        var reader = new System.Text.Json.Utf8JsonReader(buffer.AsSpan(0, read), isFinalBlock: read < buffer.Length, state: default);
        try
        {
            while (reader.Read())
            {
                if (reader.TokenType != System.Text.Json.JsonTokenType.PropertyName || reader.CurrentDepth != 1)
                {
                    continue;
                }

                if (reader.ValueTextEquals("events"u8))
                {
                    return null;
                }

                if (reader.ValueTextEquals("minigame"u8) && reader.Read() && reader.TokenType == System.Text.Json.JsonTokenType.String)
                {
                    return reader.GetString();
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }

        return null;
    }

    /// <summary>The record in a file: the one parsed last when the file's bytes are those it was parsed from, and a new parse otherwise.</summary>
    private static GameRecord? Parse(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(path);
        var hash = SHA256.HashData(bytes);
        if (Parsed.TryGetValue(path, out var kept) && kept.Hash.AsSpan().SequenceEqual(hash))
        {
            return kept.Record;
        }

        var record = GameEventReader.Read(bytes).Record;
        Remember(path, hash, record);
        return record;
    }

    private static void Remember(string path, byte[] hash, GameRecord? record)
    {
        if (Parsed.Count >= KeptRecords && !Parsed.ContainsKey(path))
        {
            Parsed.Clear();
        }

        Parsed[path] = (hash, record);
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
        return Commit(scope, label, expectedRevision, firstEventId, () => roll.Build(roller.Roll), replay);
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
            var current = Parse(path);
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

            IReadOnlyList<GameEvent> batch;
            try
            {
                batch = events();
            }
            catch (GameBuildRefusedException refusal)
            {
                return new AppendResult(AppendStatus.Invalid, existing.Count, [UnitDiagnostic.Error("PLAY-UNDECIDED", refusal.Message)]);
            }
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

            // The log is written only if it reads back: a batch built in memory must meet the rules a stored log meets.
            // The proving slice (ruling RPS.6): a game takes its minigame from the card its game-started names, at its first commit, and keeps it.
            var minigame = current is not null ? current.Minigame
                : Minigames.BundleOfCard(all.Select(item => item.Payload).OfType<GameStarted>().FirstOrDefault()?.Scenario?.Id);
            var text = GameEventWriter.Write(scope, new GameRecord(current?.Label ?? label, Synthetic: false, all)
            {
                Minigame = minigame,
            });
            var written = new UTF8Encoding(false).GetBytes(text);
            var reread = GameEventReader.Read(written);
            if (reread.HasErrors)
            {
                return new AppendResult(AppendStatus.Invalid, existing.Count, reread.Diagnostics);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            File.WriteAllBytes(temporary, written);
            File.Move(temporary, path, overwrite: true);

            // Pass 31d: the file now holds the events it held, written again as they were read, and the new ones. The record kept for it is the
            // events already read, as the objects they were, followed by the new events as they read back from the text just written.
            if (reread.Record is { } stored && stored.Events.Count == all.Length)
            {
                Remember(path, SHA256.HashData(written), stored with
                {
                    Events = [.. existing, .. stored.Events.Skip(existing.Count)]
                });
            }
            return new AppendResult(AppendStatus.Committed, all.Length, []);
        }
    }

    private string PathOf(GameScope scope) => Path.Combine(root, scope.Tenant.ToString("N"), scope.Game + Suffix);

    private static bool VocabularyNamesSlug(string text) => Units.Vocabulary.VocabularyNames.IsSlug(text);
}
