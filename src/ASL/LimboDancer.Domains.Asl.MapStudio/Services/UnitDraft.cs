using System.Globalization;
using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The Unit Lab's editable unit: form text by face and attribute, traits, states, and attached equipment. It becomes a
/// unit document by writing JSON and reading it back through <see cref="UnitDocumentReader"/>, so the Lab reports the
/// same diagnostics as any other document source.
/// </summary>
public sealed class UnitDraft
{
    public string Id { get; set; } = "lab-unit";

    public string Kind { get; set; } = "asl:squad";

    public string? Side { get; set; } = "german";

    public bool Concealed
    {
        get; set;
    }

    public int? SizeClass
    {
        get; set;
    }

    public int StackOrder
    {
        get; set;
    }

    /// <summary>Form text by face, then by qualified attribute name.</summary>
    public Dictionary<string, Dictionary<string, string>> Faces { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, HashSet<string>> Traits { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> Unit { get; } = new(StringComparer.Ordinal);

    public HashSet<string> States { get; } = new(StringComparer.Ordinal);

    public List<UnitDraft> Attached { get; } = [];

    public string Value(string face, string attribute) =>
        Faces.TryGetValue(face, out var values) && values.TryGetValue(attribute, out var text) ? text : string.Empty;

    public void SetValue(string face, string attribute, string? text)
    {
        ArgumentNullException.ThrowIfNull(face);
        ArgumentNullException.ThrowIfNull(attribute);
        if (!Faces.TryGetValue(face, out var values))
        {
            values = new Dictionary<string, string>(StringComparer.Ordinal);
            Faces[face] = values;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            values.Remove(attribute);
        }
        else
        {
            values[attribute] = text.Trim();
        }
    }

    public string UnitValue(string attribute) => Unit.TryGetValue(attribute, out var text) ? text : string.Empty;

    public void SetUnitValue(string attribute, string? text)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        if (string.IsNullOrWhiteSpace(text))
        {
            Unit.Remove(attribute);
        }
        else
        {
            Unit[attribute] = text.Trim();
        }
    }

    public bool HasTrait(string face, string trait) => Traits.TryGetValue(face, out var set) && set.Contains(trait);

    public void SetTrait(string face, string trait, bool present)
    {
        ArgumentNullException.ThrowIfNull(face);
        if (!Traits.TryGetValue(face, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            Traits[face] = set;
        }

        if (present)
        {
            set.Add(trait);
        }
        else
        {
            set.Remove(trait);
        }
    }

    public void SetState(string state, bool present)
    {
        if (present)
        {
            States.Add(state);
        }
        else
        {
            States.Remove(state);
        }
    }

    public static UnitDraft From(UnitDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var draft = new UnitDraft
        {
            Id = document.Id,
            Kind = document.Kind,
            Side = document.Side,
            Concealed = document.Concealed,
            SizeClass = document.SizeClass,
            StackOrder = document.StackOrder,
        };
        foreach (var face in document.Faces)
        {
            foreach (var value in face.Values)
            {
                draft.SetValue(face.Name, value.Attribute.Name, FormText(value));
            }

            foreach (var trait in face.Traits)
            {
                draft.SetTrait(face.Name, trait, present: true);
            }
        }

        foreach (var value in document.Unit)
        {
            draft.SetUnitValue(value.Attribute.Name, FormText(value));
        }

        draft.States.UnionWith(document.States);
        draft.Attached.AddRange(document.Attached.Select(From));
        return draft;
    }

    /// <summary>The draft as a unit document's JSON. Form text that does not fit an attribute's type is written as text, so the reader reports it.</summary>
    public string ToJson(UnitVocabulary vocabulary, string? location = null)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
        {
            Write(writer, vocabulary, location, attached: false);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private void Write(Utf8JsonWriter writer, UnitVocabulary vocabulary, string? location, bool attached)
    {
        writer.WriteStartObject();
        if (!attached)
        {
            writer.WriteStartArray("vocabulary");
            foreach (var pack in vocabulary.Packs)
            {
                writer.WriteStringValue(pack.Identity);
            }

            writer.WriteEndArray();
        }

        writer.WriteString("id", Id);
        writer.WriteString("kind", Concealed ? VocabularyNames.RootKind : Kind);
        if (!string.IsNullOrEmpty(Side))
        {
            writer.WriteString("side", Side);
        }

        if (location is not null)
        {
            writer.WriteString("location", location);
        }

        if (Concealed)
        {
            writer.WriteBoolean("concealed", true);
            if (SizeClass is { } size)
            {
                writer.WriteNumber("sizeClass", size);
            }

            if (StackOrder != 0)
            {
                writer.WriteNumber("stackOrder", StackOrder);
            }

            writer.WriteEndObject();
            return;
        }

        var faces = vocabulary.HasKind(Kind) ? vocabulary.Faces(Kind) : [];
        var used = faces.Where(face => (Faces.TryGetValue(face, out var values) && values.Count > 0) || (Traits.TryGetValue(face, out var traits) && traits.Count > 0))
            .Concat(Faces.Keys.Where(face => !faces.Contains(face) && Faces[face].Count > 0))
            .ToArray();
        if (used.Length > 0)
        {
            writer.WriteStartObject("faces");
            foreach (var face in used)
            {
                writer.WriteStartObject(face);
                foreach (var (attribute, text) in Faces.GetValueOrDefault(face) ?? [])
                {
                    WriteValue(writer, vocabulary, attribute, text);
                }

                if (Traits.TryGetValue(face, out var traits) && traits.Count > 0)
                {
                    writer.WriteStartArray("traits");
                    foreach (var trait in traits.Order(StringComparer.Ordinal))
                    {
                        writer.WriteStringValue(trait);
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        if (Unit.Count > 0)
        {
            writer.WriteStartObject("unit");
            foreach (var (attribute, text) in Unit)
            {
                WriteValue(writer, vocabulary, attribute, text);
            }

            writer.WriteEndObject();
        }

        if (States.Count > 0)
        {
            writer.WriteStartArray("states");
            foreach (var state in vocabulary.States.Select(state => state.Name).Where(States.Contains))
            {
                writer.WriteStringValue(state);
            }

            writer.WriteEndArray();
        }

        if (!attached && Attached.Count > 0)
        {
            writer.WriteStartArray("attached");
            foreach (var equipment in Attached)
            {
                equipment.Write(writer, vocabulary, null, attached: true);
            }

            writer.WriteEndArray();
        }

        if (StackOrder != 0)
        {
            writer.WriteNumber("stackOrder", StackOrder);
        }

        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, UnitVocabulary vocabulary, string attribute, string text)
    {
        var type = vocabulary.TryGetAttribute(attribute, out var definition) ? definition.Type : AttributeType.Text;
        switch (type)
        {
            case AttributeType.Number or AttributeType.Rating when int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number):
                writer.WriteNumber(attribute, number);
                break;
            case AttributeType.List:
                writer.WriteStartArray(attribute);
                // Number lists are written 6/7/8; text lists, such as dates that contain slashes, are separated by commas.
                char[] separators = definition.ItemType == AttributeType.Number ? ['/', ','] : [','];
                foreach (var item in text.Split(separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    if (definition.ItemType == AttributeType.Number && int.TryParse(item, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var itemNumber))
                    {
                        writer.WriteNumberValue(itemNumber);
                    }
                    else
                    {
                        writer.WriteStringValue(item);
                    }
                }

                writer.WriteEndArray();
                break;
            default:
                writer.WriteString(attribute, text);
                break;
        }
    }

    private static string FormText(UnitValue value) => value.Attribute.Type switch
    {
        AttributeType.Number or AttributeType.Rating => value.Number!.Value.ToString(CultureInfo.InvariantCulture),
        AttributeType.List => string.Join(value.Attribute.ItemType == AttributeType.Number ? "/" : ", ", value.Items),
        _ => value.Text ?? string.Empty,
    };
}
