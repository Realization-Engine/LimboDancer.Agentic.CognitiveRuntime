using System.Collections.ObjectModel;
using System.Text.Json;

namespace LimboDancer.Abstractions.Runtime;

public sealed class TerminalReason
{
    public TerminalReason(
        string code,
        string? message = null,
        IEnumerable<KeyValuePair<string, JsonElement>>? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (message is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
        }

        var copiedDetails = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var detail in details ?? [])
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(detail.Key);
            if (!copiedDetails.TryAdd(detail.Key, detail.Value.Clone()))
            {
                throw new ArgumentException($"Terminal reason detail '{detail.Key}' is duplicated.", nameof(details));
            }
        }

        Code = code;
        Message = message;
        Details = new ReadOnlyDictionary<string, JsonElement>(copiedDetails);
    }

    public string Code
    {
        get;
    }

    public string? Message
    {
        get;
    }

    public IReadOnlyDictionary<string, JsonElement> Details
    {
        get;
    }
}
