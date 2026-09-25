using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Documents;

/// <summary>
/// Writes unit documents and placement sets in one canonical form: two-space indentation, LF line endings, fields in a
/// fixed order, faces in the kind's order, and attribute names in their short form where unambiguous.
/// </summary>
public static class UnitDocumentJson
{
    private static readonly JsonWriterOptions Options = new()
    {
        Indented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Write(UnitDocument document, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(vocabulary);
        return Write(writer => WriteDocument(writer, document, vocabulary, writeVocabulary: true));
    }

    public static string Write(UnitPlacementSet set, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(vocabulary);
        return Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", UnitPlacementSet.SchemaVersion);
            writer.WriteString("setId", set.SetId);
            writer.WriteString("label", set.Label);
            writer.WriteBoolean("synthetic", set.Synthetic);
            writer.WriteStartArray("vocabulary");
            foreach (var pack in set.Vocabulary)
            {
                writer.WriteStringValue(pack);
            }

            writer.WriteEndArray();
            writer.WriteStartArray("units");
            foreach (var unit in set.Units)
            {
                WriteDocument(writer, unit, vocabulary, writeVocabulary: !unit.Vocabulary.SequenceEqual(set.Vocabulary));
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        });
    }

    private static string Write(Action<Utf8JsonWriter> write)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, Options))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(buffer.ToArray()) + "\n";
    }

    private static void WriteDocument(Utf8JsonWriter writer, UnitDocument document, UnitVocabulary vocabulary, bool writeVocabulary)
    {
        writer.WriteStartObject();
        if (writeVocabulary && document.Vocabulary.Count > 0)
        {
            writer.WriteStartArray("vocabulary");
            foreach (var pack in document.Vocabulary)
            {
                writer.WriteStringValue(pack);
            }

            writer.WriteEndArray();
        }

        writer.WriteString("id", document.Id);
        writer.WriteString("kind", document.Kind);
        if (document.Side is not null)
        {
            writer.WriteString("side", document.Side);
        }

        if (document.Location is not null)
        {
            writer.WriteString("location", document.Location);
        }

        if (document.Facing is { } facing)
        {
            writer.WriteString("facing", facing.Name());
        }

        if (document.TurretFacing is { } turretFacing)
        {
            writer.WriteString("turretFacing", turretFacing.Name());
        }

        if (document.Concealed)
        {
            writer.WriteBoolean("concealed", true);
        }

        if (document.SizeClass is { } sizeClass)
        {
            writer.WriteNumber("sizeClass", sizeClass);
        }

        if (document.Faces.Count > 0)
        {
            writer.WriteStartObject("faces");
            foreach (var face in document.Faces)
            {
                writer.WriteStartObject(face.Name);
                WriteValues(writer, face.Values, document.Kind, vocabulary);
                if (face.Traits.Count > 0)
                {
                    writer.WriteStartArray("traits");
                    foreach (var trait in face.Traits)
                    {
                        writer.WriteStringValue(trait);
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        if (document.Unit.Count > 0)
        {
            writer.WriteStartObject("unit");
            WriteValues(writer, document.Unit, document.Kind, vocabulary);
            writer.WriteEndObject();
        }

        if (document.States.Count > 0)
        {
            writer.WriteStartArray("states");
            foreach (var state in document.States)
            {
                writer.WriteStringValue(state);
            }

            writer.WriteEndArray();
        }

        if (document.Attached.Count > 0)
        {
            writer.WriteStartArray("attached");
            foreach (var attached in document.Attached)
            {
                WriteDocument(writer, attached, vocabulary, writeVocabulary: false);
            }

            writer.WriteEndArray();
        }

        if (document.StackOrder != 0)
        {
            writer.WriteNumber("stackOrder", document.StackOrder);
        }

        writer.WriteEndObject();
    }

    private static void WriteValues(Utf8JsonWriter writer, IEnumerable<UnitValue> values, string kind, UnitVocabulary vocabulary)
    {
        foreach (var value in values)
        {
            var name = vocabulary.ShortName(kind, value.Attribute.Name);
            switch (value.Attribute.Type)
            {
                case AttributeType.Number or AttributeType.Rating:
                    writer.WriteNumber(name, value.Number!.Value);
                    break;
                case AttributeType.List:
                    writer.WriteStartArray(name);
                    foreach (var item in value.Items)
                    {
                        if (value.Attribute.ItemType == AttributeType.Number)
                        {
                            writer.WriteNumberValue(int.Parse(item, System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            writer.WriteStringValue(item);
                        }
                    }

                    writer.WriteEndArray();
                    break;
                default:
                    writer.WriteString(name, value.Text);
                    break;
            }
        }
    }
}
