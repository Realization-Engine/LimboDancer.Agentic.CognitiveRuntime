using System.Text.Json;

namespace LimboDancer.Abstractions.Actions;

public sealed class SemanticActionIntent
{
    public SemanticActionIntent(string value, JsonElement arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (arguments.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Semantic action arguments must be a JSON object.", nameof(arguments));
        }

        Value = value;
        Arguments = arguments.Clone();
    }

    public string Value
    {
        get;
    }

    public JsonElement Arguments
    {
        get;
    }
}
