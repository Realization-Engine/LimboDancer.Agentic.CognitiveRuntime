using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Documents;

public sealed record UnitReadResult(IReadOnlyList<UnitDocument> Documents, IReadOnlyList<UnitDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error);
}

/// <summary>
/// Reads unit documents, one or a list, against a vocabulary (Unit Display Design, sections 3.2 and 9). A document
/// using an undeclared kind, face, attribute, trait, or state, or an attribute of the wrong type, is refused with a
/// diagnostic naming it; the other documents in a list are still read.
/// </summary>
public static class UnitDocumentReader
{
    private static readonly HashSet<string> DocumentFields = new(StringComparer.Ordinal)
    {
        "vocabulary", "id", "kind", "side", "location", "facing", "faces", "unit", "states", "attached", "stackOrder", "concealed", "sizeClass", "note",
    };

    private static readonly HashSet<string> PlaceholderFields = new(StringComparer.Ordinal)
    {
        "vocabulary", "id", "kind", "side", "location", "stackOrder", "concealed", "sizeClass", "note",
    };

    public static UnitReadResult Read(ReadOnlySpan<byte> json, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        var diagnostics = new List<UnitDiagnostic>();
        if (!TryParse(json, diagnostics, out var document))
        {
            return new UnitReadResult([], diagnostics);
        }

        using (document)
        {
            var documents = new List<UnitDocument>();
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var item in root.EnumerateArray())
                {
                    if (ReadElement(item, vocabulary, $"[{index++}]", null, attached: false, diagnostics) is { } unit)
                    {
                        documents.Add(unit);
                    }
                }
            }
            else if (ReadElement(root, vocabulary, "$", null, attached: false, diagnostics) is { } unit)
            {
                documents.Add(unit);
            }

            return new UnitReadResult(documents, diagnostics);
        }
    }

    public static UnitReadResult Read(string json, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Read(System.Text.Encoding.UTF8.GetBytes(json), vocabulary);
    }

    internal static bool TryParse(ReadOnlySpan<byte> json, List<UnitDiagnostic> diagnostics, out JsonDocument document)
    {
        try
        {
            document = JsonDocument.Parse(json.ToArray());
            return true;
        }
        catch (JsonException exception)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-001",
                $"Not valid JSON: line {exception.LineNumber + 1}, position {exception.BytePositionInLine + 1}."));
            document = null!;
            return false;
        }
    }

    /// <summary>Reads one document; returns null, with errors recorded, when it is refused.</summary>
    internal static UnitDocument? ReadElement(JsonElement element, UnitVocabulary vocabulary, string path, IReadOnlyList<string>? inheritedVocabulary,
        bool attached, List<UnitDiagnostic> diagnostics)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-001", "A unit document must be a JSON object.", path));
            return null;
        }

        var errors = diagnostics.Count(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error);
        var fields = new JsonFields(diagnostics, "UNIT-DOC-001");
        var concealed = fields.OptionalBoolean(element, "concealed", path);
        foreach (var property in element.EnumerateObject())
        {
            if (concealed && !PlaceholderFields.Contains(property.Name) && DocumentFields.Contains(property.Name))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-017",
                    $"A concealed placeholder carries no '{property.Name}': the source sends only what the viewer may know.", path));
            }
            else if (!DocumentFields.Contains(property.Name))
            {
                diagnostics.Add(UnitDiagnostic.Warning("UNIT-DOC-016", $"'{property.Name}' is not a unit document field and is ignored.", path));
            }
        }

        var packs = element.TryGetProperty("vocabulary", out _) ? fields.StringList(element, "vocabulary", path) : inheritedVocabulary;
        if (packs is null || packs.Count == 0)
        {
            if (!attached)
            {
                diagnostics.Add(UnitDiagnostic.Warning("UNIT-DOC-011", "The document records no vocabulary version.", path));
            }

            packs = [];
        }
        else
        {
            foreach (var reference in packs.Where(reference => !vocabulary.Serves(reference)))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-011", $"The document was written against '{reference}', which is not loaded ({vocabulary.Identity}).", path));
            }
        }

        var id = fields.RequiredString(element, "id", path);
        if (id is not null && (id.Length == 0 || id.Length > 64 || id.Any(char.IsWhiteSpace)))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-009", $"The id '{id}' must be 1 to 64 characters without spaces.", path));
        }

        var kind = fields.RequiredString(element, "kind", path);
        if (kind is not null && !vocabulary.HasKind(kind))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-002", $"The kind '{kind}' is not declared.", path));
            kind = null;
        }

        var side = fields.OptionalString(element, "side", path);
        if (side is not null && !vocabulary.TryGetSide(side, out _))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-008", $"The side '{side}' is not declared.", path));
        }

        var location = fields.OptionalString(element, "location", path);
        if (location is not null)
        {
            if (attached)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-013", "Attached equipment has no location of its own; it is where its owner is.", path));
            }
            else if (!BoardLocation.TryParse(location, out var parsed) || parsed.Side is not null)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-013", $"'{location}' is not a hex location such as bd01:E4:0.", path));
            }
        }

        var sizeClass = fields.OptionalInteger(element, "sizeClass", path);
        if (sizeClass is < 1 or > 3)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-004", "The size class must be 1, 2, or 3.", path));
        }

        var stackOrder = fields.OptionalInteger(element, "stackOrder", path) ?? 0;
        UnitFacing? facing = null;
        if (fields.OptionalString(element, "facing", path) is { } facingText && !concealed)
        {
            if (!UnitFacings.TryParse(facingText, out var parsedFacing))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-018", $"'{facingText}' is not a hexspine; use {string.Join(", ", UnitFacings.All)}.", path));
            }
            else if (attached || kind is null || !vocabulary.HasFacing(kind))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-018",
                    attached ? "Attached equipment has no facing of its own." : $"The kind '{kind}' has no facing.", path));
            }
            else
            {
                facing = parsedFacing;
            }
        }
        var faces = new List<UnitFace>();
        var unit = new List<UnitValue>();
        var states = new List<string>();
        var attachments = new List<UnitDocument>();
        if (kind is not null && !concealed)
        {
            faces = ReadFaces(element, vocabulary, kind, path, diagnostics);
            unit = ReadUnitValues(element, vocabulary, kind, path, diagnostics);
            states = ReadStates(element, vocabulary, path, fields, diagnostics);
            if (element.TryGetProperty("attached", out var list) && list.ValueKind != JsonValueKind.Null)
            {
                if (attached)
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-015", "Attached equipment cannot carry equipment of its own.", path));
                }
                else if (list.ValueKind != JsonValueKind.Array)
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-001", "'attached' must be a list of unit documents.", path));
                }
                else
                {
                    var index = 0;
                    var ids = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var item in list.EnumerateArray())
                    {
                        var itemPath = $"{path}.attached[{index++}]";
                        if (ReadElement(item, vocabulary, itemPath, packs, attached: true, diagnostics) is { } equipment)
                        {
                            if (!ids.Add(equipment.Id) || equipment.Id == id)
                            {
                                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-010", $"The id '{equipment.Id}' is used twice in this document.", itemPath));
                                continue;
                            }

                            attachments.Add(equipment);
                        }
                    }
                }
            }
        }

        if (diagnostics.Count(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error) > errors || id is null || kind is null)
        {
            return null;
        }

        return new UnitDocument(packs, id, kind, side, location, faces, unit, states, attachments, stackOrder, concealed, sizeClass, facing);
    }

    private static List<UnitFace> ReadFaces(JsonElement element, UnitVocabulary vocabulary, string kind, string path, List<UnitDiagnostic> diagnostics)
    {
        var faces = new List<UnitFace>();
        if (!element.TryGetProperty("faces", out var map) || map.ValueKind == JsonValueKind.Null)
        {
            return faces;
        }

        if (map.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-001", "'faces' must be an object of faces by name.", path));
            return faces;
        }

        var declared = vocabulary.Faces(kind);
        var read = new Dictionary<string, UnitFace>(StringComparer.Ordinal);
        foreach (var face in map.EnumerateObject())
        {
            var facePath = $"{path}.faces.{face.Name}";
            if (!declared.Contains(face.Name))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-007", $"The kind '{kind}' has no '{face.Name}' face.", facePath));
                continue;
            }

            if (face.Value.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-001", "A face must be an object of attribute values.", facePath));
                continue;
            }

            var values = new List<UnitValue>();
            var traits = new List<string>();
            foreach (var property in face.Value.EnumerateObject())
            {
                if (property.Name == "traits")
                {
                    traits = ReadTraits(property.Value, vocabulary, kind, face.Name, facePath, diagnostics);
                    continue;
                }

                if (ReadValue(property, vocabulary, kind, AttributeScope.Face, face.Name, facePath, diagnostics) is { } value)
                {
                    values.Add(value);
                }
            }

            read[face.Name] = new UnitFace(face.Name, Ordered(values, vocabulary, kind), traits);
        }

        // Faces keep the kind's declared order, so equal documents write and render the same.
        faces.AddRange(declared.Where(read.ContainsKey).Select(name => read[name]));
        return faces;
    }

    private static List<UnitValue> ReadUnitValues(JsonElement element, UnitVocabulary vocabulary, string kind, string path, List<UnitDiagnostic> diagnostics)
    {
        var values = new List<UnitValue>();
        if (!element.TryGetProperty("unit", out var map) || map.ValueKind == JsonValueKind.Null)
        {
            return values;
        }

        if (map.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-001", "'unit' must be an object of attribute values.", path));
            return values;
        }

        foreach (var property in map.EnumerateObject())
        {
            if (ReadValue(property, vocabulary, kind, AttributeScope.Unit, null, path + ".unit", diagnostics) is { } value)
            {
                values.Add(value);
            }
        }

        return Ordered(values, vocabulary, kind);
    }

    private static List<UnitValue> Ordered(List<UnitValue> values, UnitVocabulary vocabulary, string kind)
    {
        var order = vocabulary.Ancestors(kind).Reverse()
            .SelectMany(name => vocabulary.Kind(name).Attributes)
            .Concat(vocabulary.AcceptedAttributes(kind).Order(StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Select((name, index) => (name, index))
            .ToDictionary(item => item.name, item => item.index, StringComparer.Ordinal);
        return [.. values.GroupBy(value => value.Attribute.Name, StringComparer.Ordinal).Select(group => group.Last())
            .OrderBy(value => order.GetValueOrDefault(value.Attribute.Name, int.MaxValue))];
    }

    private static UnitValue? ReadValue(JsonProperty property, UnitVocabulary vocabulary, string kind, AttributeScope scope, string? face, string path,
        List<UnitDiagnostic> diagnostics)
    {
        var attributePath = $"{path}.{property.Name}";
        if (!vocabulary.TryResolveAttribute(kind, property.Name, out var attribute, out var ambiguous))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-003", ambiguous
                ? $"'{property.Name}' names more than one attribute of '{kind}'; write the pack prefix."
                : $"The attribute '{property.Name}' is not declared for '{kind}'.", attributePath));
            return null;
        }

        if (attribute.Scope != scope)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-014", scope == AttributeScope.Face
                ? $"'{property.Name}' belongs to the whole unit; put it under 'unit'."
                : $"'{property.Name}' belongs to a face; put it under 'faces'.", attributePath));
            return null;
        }

        if (face is not null && attribute.Faces.Count > 0 && !attribute.Faces.Contains(face))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-014", $"'{property.Name}' belongs on the {string.Join(" or ", attribute.Faces)} face.", attributePath));
            return null;
        }

        var value = property.Value;
        switch (attribute.Type)
        {
            case AttributeType.Number when value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number):
                return UnitValue.Of(attribute, number);
            case AttributeType.Rating when value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var rating):
                return UnitValue.Of(attribute, rating);
            case AttributeType.Rating when value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(),
                System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out var signed):
                return UnitValue.Of(attribute, signed);
            case AttributeType.Text when value.ValueKind == JsonValueKind.String:
                return UnitValue.Of(attribute, value.GetString()!);
            case AttributeType.Enumeration when value.ValueKind == JsonValueKind.String:
                if (attribute.Member(value.GetString()!) is null)
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-004",
                        $"'{value.GetString()}' is not one of {string.Join(", ", attribute.Members.Select(member => member.Name))}.", attributePath));
                    return null;
                }

                return UnitValue.Of(attribute, value.GetString()!);
            case AttributeType.List when value.ValueKind == JsonValueKind.Array:
                var items = new List<string>();
                foreach (var item in value.EnumerateArray())
                {
                    if (attribute.ItemType == AttributeType.Number && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var itemNumber))
                    {
                        items.Add(itemNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else if (attribute.ItemType == AttributeType.Text && item.ValueKind == JsonValueKind.String)
                    {
                        items.Add(item.GetString()!);
                    }
                    else
                    {
                        diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-004",
                            $"'{property.Name}' must be a list of {(attribute.ItemType == AttributeType.Number ? "whole numbers" : "strings")}.", attributePath));
                        return null;
                    }
                }

                return UnitValue.Of(attribute, items);
            default:
                var expected = attribute.Type switch
                {
                    AttributeType.Number => "a whole number",
                    AttributeType.Rating => "a whole number with an optional sign",
                    AttributeType.List => "a list",
                    _ => "a string",
                };
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-004", $"'{property.Name}' must be {expected}.", attributePath));
                return null;
        }
    }

    private static List<string> ReadTraits(JsonElement value, UnitVocabulary vocabulary, string kind, string face, string path, List<UnitDiagnostic> diagnostics)
    {
        var traits = new List<string>();
        if (value.ValueKind != JsonValueKind.Array || value.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-001", "'traits' must be a list of trait names.", path + ".traits"));
            return traits;
        }

        foreach (var name in value.EnumerateArray().Select(item => item.GetString()!))
        {
            if (!vocabulary.TryGetTrait(name, out var trait))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-005", $"The trait '{name}' is not declared.", path + ".traits"));
            }
            else if (!vocabulary.AcceptedTraits(kind).Contains(name))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-005", $"The trait '{name}' is not declared for '{kind}'.", path + ".traits"));
            }
            else if (trait.Faces.Count > 0 && !trait.Faces.Contains(face))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-014", $"The trait '{name}' belongs on the {string.Join(" or ", trait.Faces)} face.", path + ".traits"));
            }
            else if (!traits.Contains(name))
            {
                traits.Add(name);
            }
        }

        return traits;
    }

    private static List<string> ReadStates(JsonElement element, UnitVocabulary vocabulary, string path, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        var states = new List<string>();
        foreach (var name in fields.StringList(element, "states", path))
        {
            if (!vocabulary.TryGetState(name, out _))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-006", $"The state '{name}' is not declared.", path + ".states"));
            }
            else if (!states.Contains(name))
            {
                states.Add(name);
            }
        }

        foreach (var group in states.Select(name => vocabulary.States.First(state => state.Name == name))
            .Where(state => state.Group is not null).GroupBy(state => state.Group, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-012",
                $"The states {string.Join(" and ", group.Select(state => state.Name))} exclude each other ({group.Key}).", path + ".states"));
        }

        // States keep the vocabulary's order.
        return [.. vocabulary.States.Select(state => state.Name).Where(states.Contains)];
    }
}
