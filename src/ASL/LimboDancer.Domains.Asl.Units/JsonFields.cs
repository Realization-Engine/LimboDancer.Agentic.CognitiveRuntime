using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Units;

/// <summary>Reads JSON fields and records a diagnostic for each missing or mistyped one, so readers never throw on input.</summary>
internal sealed class JsonFields(List<UnitDiagnostic> diagnostics, string code)
{
    public List<UnitDiagnostic> Diagnostics => diagnostics;

    public string? RequiredString(JsonElement element, string name, string path)
    {
        var value = OptionalString(element, name, path);
        if (value is null && !element.TryGetProperty(name, out _))
        {
            diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' is required.", path));
        }

        return value;
    }

    public string? OptionalString(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' must be a string.", path));
            return null;
        }

        return value.GetString();
    }

    public int? OptionalInteger(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
        {
            diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' must be a whole number.", path));
            return null;
        }

        return number;
    }

    public bool OptionalBoolean(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return false;
        }

        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' must be true or false.", path));
            return false;
        }

        return value.GetBoolean();
    }

    public IReadOnlyList<string> StringList(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' must be a list of strings.", path));
            return [];
        }

        var items = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' must be a list of strings.", path));
                return [];
            }

            items.Add(item.GetString()!);
        }

        return items;
    }

    public IEnumerable<(JsonElement Item, string Path)> Objects(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            yield break;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' must be a list.", path));
            yield break;
        }

        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            var itemPath = $"{path}.{name}[{index++}]";
            if (item.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(UnitDiagnostic.Error(code, "Each item must be an object.", itemPath));
                continue;
            }

            yield return (item, itemPath);
        }
    }

    /// <summary>A map of face names to templates, or a single template for every face.</summary>
    public IReadOnlyDictionary<string, string> Templates(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal) { ["default"] = value.GetString()! };
        }

        var templates = new Dictionary<string, string>(StringComparer.Ordinal);
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}.{property.Name}' must be a string.", path));
                    continue;
                }

                templates[property.Name] = property.Value.GetString()!;
            }

            return templates;
        }

        diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' must be a string or an object of strings by face.", path));
        return templates;
    }

    /// <summary>The SHA-256 of text bytes with CRLF normalized to LF, in lowercase hex.</summary>
    public static string ContentHash(ReadOnlySpan<byte> bytes)
    {
        var text = Encoding.UTF8.GetString(bytes).ReplaceLineEndings("\n");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }
}
