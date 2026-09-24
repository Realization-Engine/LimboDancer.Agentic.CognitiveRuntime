using System.Collections.ObjectModel;

namespace LimboDancer.Abstractions.State.History;

public sealed class HistoryEntry
{
    public HistoryEntry(
        string messageId,
        string sessionId,
        string sender,
        string text,
        DateTimeOffset timestamp,
        IReadOnlyDictionary<string, string?>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sender);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        MessageId = messageId;
        SessionId = sessionId;
        Sender = sender;
        Text = text;
        Timestamp = timestamp;
        Metadata = new ReadOnlyDictionary<string, string?>(
            new Dictionary<string, string?>(metadata ?? new Dictionary<string, string?>(), StringComparer.Ordinal));
    }

    public string MessageId
    {
        get;
    }

    public string SessionId
    {
        get;
    }

    public string Sender
    {
        get;
    }

    public string Text
    {
        get;
    }

    public DateTimeOffset Timestamp
    {
        get;
    }

    public IReadOnlyDictionary<string, string?> Metadata
    {
        get;
    }
}
