using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Runtime.Actions;

internal static class ActionExecutorJson
{
    public static bool IsExpectedAction(AuthorizedAction action, ActionId expectedActionId) =>
        action.Selected.Candidate.Descriptor.Id == expectedActionId;

    public static bool TryGetRequiredString(JsonElement arguments, string name, out string value)
    {
        value = string.Empty;
        if (arguments.ValueKind != JsonValueKind.Object
            || !arguments.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var candidate = property.GetString();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        value = candidate;
        return true;
    }

    public static bool TryGetOptionalString(JsonElement arguments, string name, out string? value)
    {
        value = null;
        if (!arguments.TryGetProperty(name, out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return true;
    }

    public static bool TryGetOptionalInt32(
        JsonElement arguments,
        string name,
        int defaultValue,
        int minimum,
        int maximum,
        out int value)
    {
        value = defaultValue;
        if (!arguments.TryGetProperty(name, out var property))
        {
            return true;
        }

        return property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out value)
            && value >= minimum
            && value <= maximum;
    }

    public static bool TryGetStringArray(
        JsonElement arguments,
        string name,
        out IReadOnlyList<string> values)
    {
        values = [];
        if (!arguments.TryGetProperty(name, out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var result = new List<string>();
        foreach (var element in property.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(element.GetString()))
            {
                return false;
            }

            result.Add(element.GetString()!);
        }

        values = result.AsReadOnly();
        return true;
    }

    public static object? ToScalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
        _ => value.Clone(),
    };

    public static JsonElement Serialize<T>(T value) => JsonSerializer.SerializeToElement(value);
}
