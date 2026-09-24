using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Durable, per-aggregate append-only CAS journal for the bounded return state.</summary>
public sealed class ScenarioA1JournalReturnStore : IScenarioA1ReturnStateStore
{
    private readonly string directory;

    public ScenarioA1JournalReturnStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        this.directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(this.directory);
    }

    public async ValueTask SeedAsync(ScenarioA1ReturnState initial,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(initial);
        var name = Name(initial.TenantId, initial.GameId, initial.UnitId);
        await using var guard = await LockAsync(name, cancellationToken);
        var path = Journal(name);
        if (File.Exists(path))
            throw new InvalidOperationException("The game aggregate is already present.");
        await AppendAsync(path, initial, cancellationToken);
    }

    public async ValueTask<ScenarioA1ReturnState?> ReadAsync(Guid tenantId,
        string gameId, string unitId, CancellationToken cancellationToken = default)
    {
        var name = Name(tenantId, gameId, unitId);
        await using var guard = await LockAsync(name, cancellationToken);
        return ReadJournal(Journal(name), tenantId, gameId, unitId);
    }

    public async ValueTask<bool> TryCommitAsync(ScenarioA1ReturnState expected,
        ScenarioA1ReturnAttempt attempt, ScenarioA1ReturnState updated,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(updated);
        var candidate = ScenarioA1SecondDefenderReturnTransition.Evaluate(expected, attempt);
        if (candidate.Status != ScenarioA1ReturnTransitionStatus.Applied
            || candidate.State != updated)
            return false;
        var name = Name(expected.TenantId, expected.GameId, expected.UnitId);
        await using var guard = await LockAsync(name, cancellationToken);
        var path = Journal(name);
        var current = ReadJournal(path, expected.TenantId, expected.GameId, expected.UnitId);
        if (current != expected)
            return false;
        await AppendAsync(path, updated, cancellationToken);
        return true;
    }

    private string Journal(string name) => Path.Combine(directory, name + ".jsonl");

    private static string Name(Guid tenantId, string gameId, string unitId)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(gameId)
            || string.IsNullOrWhiteSpace(unitId)
            || gameId.Contains('\0') || unitId.Contains('\0'))
            throw new ArgumentException("An aggregate needs valid tenant, game and unit identifiers.");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            tenantId.ToString("N") + "\0" + gameId + "\0" + unitId)));
    }

    private async ValueTask<FileStream> LockAsync(string name, CancellationToken cancellationToken)
    {
        var path = Path.Combine(directory, name + ".lock");
        for (var attempt = 0; attempt < 100; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException) when (attempt < 99)
            {
                await Task.Delay(20, cancellationToken);
            }
        }
        throw new IOException("Could not lock the game aggregate.");
    }

    private static ScenarioA1ReturnState? ReadJournal(string path, Guid tenantId,
        string gameId, string unitId)
    {
        if (!File.Exists(path))
            return null;
        ScenarioA1ReturnState? latest = null;
        foreach (var line in File.ReadLines(path))
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                var envelope = document.RootElement;
                var payload = envelope.GetProperty("state").GetRawText();
                var checksum = envelope.GetProperty("sha256").GetString();
                if (checksum != Convert.ToHexStringLower(SHA256.HashData(
                        Encoding.UTF8.GetBytes(payload))))
                    throw new InvalidDataException("Game aggregate journal checksum differs.");
                var state = JsonSerializer.Deserialize<ScenarioA1ReturnState>(payload)
                    ?? throw new InvalidDataException("Game aggregate journal state is missing.");
                if (state.TenantId != tenantId || state.GameId != gameId || state.UnitId != unitId
                    || state.Version < 0 || (latest is not null
                        && (latest.Version == long.MaxValue
                            || state.Version != latest.Version + 1)))
                    throw new InvalidDataException("Game aggregate journal identity or version changed.");
                latest = state;
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("Game aggregate journal is incomplete or corrupt.",
                    exception);
            }
        }
        return latest ?? throw new InvalidDataException("Game aggregate journal is empty.");
    }

    private static async ValueTask AppendAsync(string path, ScenarioA1ReturnState state,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(state);
        var envelope = JsonSerializer.SerializeToUtf8Bytes(new
        {
            sha256 = Convert.ToHexStringLower(SHA256.HashData(payload)),
            state,
        });
        await using var stream = new FileStream(path, FileMode.Append, FileAccess.Write,
            FileShare.Read, bufferSize: 4096, useAsync: true);
        await stream.WriteAsync(envelope, cancellationToken);
        await stream.WriteAsync("\n"u8.ToArray(), cancellationToken);
        stream.Flush(flushToDisk: true);
    }
}
