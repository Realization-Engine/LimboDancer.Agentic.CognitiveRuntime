using System.Text.Json;

namespace LimboDancer.Adapters.Mcp;

internal static class JsonSchemaSubsetValidator
{
    public static bool IsValid(JsonElement value, JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (schema.TryGetProperty("anyOf", out var alternatives)
            && alternatives.ValueKind == JsonValueKind.Array
            && !alternatives.EnumerateArray().Any(alternative => IsValid(value, alternative)))
        {
            return false;
        }

        if (schema.TryGetProperty("type", out var type)
            && type.ValueKind == JsonValueKind.String
            && !MatchesType(value, type.GetString()))
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.Object && !ValidateObject(value, schema))
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.Array
            && schema.TryGetProperty("items", out var items)
            && value.EnumerateArray().Any(item => !IsValid(item, items)))
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.Number && !ValidateNumber(value, schema))
        {
            return false;
        }

        return value.ValueKind != JsonValueKind.String || ValidateString(value, schema);
    }

    private static bool ValidateObject(JsonElement value, JsonElement schema)
    {
        if (schema.TryGetProperty("required", out var required)
            && required.ValueKind == JsonValueKind.Array
            && required.EnumerateArray().Any(item => !value.TryGetProperty(item.GetString()!, out _)))
        {
            return false;
        }

        if (!schema.TryGetProperty("properties", out var properties)
            || properties.ValueKind != JsonValueKind.Object)
        {
            return true;
        }

        var rejectUnknown = schema.TryGetProperty("additionalProperties", out var additional)
            && additional.ValueKind == JsonValueKind.False;
        foreach (var property in value.EnumerateObject())
        {
            if (!properties.TryGetProperty(property.Name, out var propertySchema))
            {
                if (rejectUnknown)
                {
                    return false;
                }

                continue;
            }

            if (!IsValid(property.Value, propertySchema))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateNumber(JsonElement value, JsonElement schema)
    {
        if (!value.TryGetDecimal(out var number))
        {
            return false;
        }

        if (schema.TryGetProperty("minimum", out var minimum)
            && minimum.TryGetDecimal(out var minimumValue)
            && number < minimumValue)
        {
            return false;
        }

        return !schema.TryGetProperty("maximum", out var maximum)
            || !maximum.TryGetDecimal(out var maximumValue)
            || number <= maximumValue;
    }

    private static bool ValidateString(JsonElement value, JsonElement schema)
    {
        var text = value.GetString();
        if (schema.TryGetProperty("enum", out var allowed)
            && allowed.ValueKind == JsonValueKind.Array
            && !allowed.EnumerateArray().Any(item => string.Equals(item.GetString(), text, StringComparison.Ordinal)))
        {
            return false;
        }

        return !schema.TryGetProperty("format", out var format)
            || !string.Equals(format.GetString(), "date-time", StringComparison.Ordinal)
            || value.TryGetDateTimeOffset(out _);
    }

    private static bool MatchesType(JsonElement value, string? type) => type switch
    {
        "object" => value.ValueKind == JsonValueKind.Object,
        "array" => value.ValueKind == JsonValueKind.Array,
        "string" => value.ValueKind == JsonValueKind.String,
        "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
        "number" => value.ValueKind == JsonValueKind.Number,
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "null" => value.ValueKind == JsonValueKind.Null,
        _ => true,
    };
}
