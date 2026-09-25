using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Units.CounterSheets;

/// <summary>Reads JSON fields and records a diagnostic for each missing or mistyped one, so readers never throw on input.</summary>
internal sealed class Json(List<UnitDiagnostic> diagnostics, string code)
{
    public static bool TryParse(ReadOnlySpan<byte> json, string code, List<UnitDiagnostic> diagnostics, out JsonDocument document)
    {
        try
        {
            document = JsonDocument.Parse(json.ToArray());
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return true;
            }

            document.Dispose();
            diagnostics.Add(UnitDiagnostic.Error(code, "The file must hold one JSON object."));
        }
        catch (JsonException exception)
        {
            diagnostics.Add(UnitDiagnostic.Error(code, $"Not valid JSON: line {exception.LineNumber + 1}, position {exception.BytePositionInLine + 1}."));
        }

        document = null!;
        return false;
    }

    public static bool Object(JsonElement element, string name, out JsonElement value) =>
        element.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Object;

    /// <summary>The SHA-256 of text bytes with CRLF normalized to LF, in lowercase hex, as the catalog pins sources.</summary>
    public static string ContentHash(ReadOnlySpan<byte> bytes)
    {
        var text = Encoding.UTF8.GetString(bytes).ReplaceLineEndings("\n");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    public string? String(JsonElement element, string name, string path, bool required = false)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            if (required)
            {
                diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' is required.", path));
            }

            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' must be a string.", path));
            return null;
        }

        var text = value.GetString()!;
        if (required && text.Trim().Length == 0)
        {
            diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' must not be empty.", path));
            return null;
        }

        return text.Trim().Length == 0 ? null : text;
    }

    public int? Integer(JsonElement element, string name, string path)
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

    public IReadOnlyList<string> Strings(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array || value.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String))
        {
            diagnostics.Add(UnitDiagnostic.Error(code, $"'{name}' must be a list of strings.", path));
            return [];
        }

        return [.. value.EnumerateArray().Select(item => item.GetString()!)];
    }

    public IEnumerable<(JsonElement Item, string Path)> Objects(JsonElement element, string name, string path)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
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
}
