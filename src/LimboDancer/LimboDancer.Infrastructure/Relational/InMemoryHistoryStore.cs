using System.Collections.Concurrent;
using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.History;

namespace LimboDancer.Infrastructure.Relational;

public sealed class InMemoryHistoryStore : IHistoryReader, IHistoryWriter
{
    private readonly ConcurrentDictionary<(Guid TenantId, string SessionId), HistoryStream> streams = new();

    public ValueTask<HistoryEntry> AppendAsync(
        TenantScope tenant,
        HistoryAppendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        tenant.ThrowIfInvalid();
        cancellationToken.ThrowIfCancellationRequested();

        var entry = new HistoryEntry(
            Guid.NewGuid().ToString("D"),
            request.SessionId,
            request.Sender,
            request.Text,
            request.Timestamp,
            request.Metadata);
        var stream = streams.GetOrAdd((tenant.TenantId, request.SessionId), static _ => new HistoryStream());

        lock (stream.SyncRoot)
        {
            stream.Entries.Add(entry);
        }

        return ValueTask.FromResult(entry);
    }

    public ValueTask<IReadOnlyList<HistoryEntry>> ListAsync(
        TenantScope tenant,
        string sessionId,
        int limit,
        DateTimeOffset? before = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        tenant.ThrowIfInvalid();
        if (limit is < 1 or > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be between 1 and 200.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!streams.TryGetValue((tenant.TenantId, sessionId), out var stream))
        {
            return ValueTask.FromResult<IReadOnlyList<HistoryEntry>>([]);
        }

        HistoryEntry[] result;
        lock (stream.SyncRoot)
        {
            result = stream.Entries
                .Where(entry => before is null || entry.Timestamp < before.Value)
                .OrderByDescending(static entry => entry.Timestamp)
                .ThenByDescending(static entry => entry.MessageId, StringComparer.Ordinal)
                .Take(limit)
                .OrderBy(static entry => entry.Timestamp)
                .ThenBy(static entry => entry.MessageId, StringComparer.Ordinal)
                .ToArray();
        }

        return ValueTask.FromResult<IReadOnlyList<HistoryEntry>>(Array.AsReadOnly(result));
    }

    private sealed class HistoryStream
    {
        public object SyncRoot
        {
            get;
        } = new();

        public List<HistoryEntry> Entries
        {
            get;
        } = [];
    }
}
