using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Catalog;

/// <summary>
/// Writes a catalog in its one canonical form: two-space indentation, LF line endings, fields in a fixed order,
/// definitions and values in their recorded order, and attribute names in their short form where unambiguous. The
/// catalog's identity hash is taken over this form, so reading and writing a catalog never changes its identity.
/// </summary>
public static class UnitCatalogJson
{
    private static readonly JsonWriterOptions Options = new()
    {
        Indented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Write(UnitCatalog catalog, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(vocabulary);
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, Options))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", UnitCatalogReader.SchemaVersion);
            writer.WriteString("catalog", catalog.Identity.Catalog);
            writer.WriteString("version", catalog.Identity.Version);
            writer.WriteString("label", catalog.Label);
            writer.WriteString("publication", UnitCatalogReader.Name(catalog.Publication));
            Strings(writer, "vocabulary", catalog.Vocabulary);
            writer.WriteStartArray("sources");
            foreach (var source in catalog.Sources)
            {
                writer.WriteStartObject();
                writer.WriteString("id", source.Id);
                writer.WriteString("status", UnitCatalogReader.Name(source.Status));
                Optional(writer, "record", source.Record);
                Optional(writer, "recordSha256", source.RecordSha256);
                Optional(writer, "transcription", source.Transcription);
                Optional(writer, "transcriptionSha256", source.TranscriptionSha256);
                Optional(writer, "transcriber", source.Transcriber);
                Optional(writer, "reviewer", source.Reviewer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("slots");
            foreach (var slot in catalog.Slots)
            {
                writer.WriteStartObject();
                writer.WriteString("id", slot.Id);
                writer.WriteString("kind", slot.Kind);
                writer.WriteString("label", slot.Label);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("definitions");
            foreach (var definition in catalog.Definitions)
            {
                WriteDefinition(writer, definition, vocabulary);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray()) + "\n";
    }

    private static void WriteDefinition(Utf8JsonWriter writer, UnitDefinition definition, UnitVocabulary vocabulary)
    {
        writer.WriteStartObject();
        writer.WriteString("id", definition.Id);
        writer.WriteString("kind", definition.Kind);
        writer.WriteNumber("kindRow", definition.KindRow);
        writer.WriteString("nationality", definition.Nationality);
        writer.WriteNumber("nationalityRow", definition.NationalityRow);
        Optional(writer, "class", definition.Class);
        writer.WriteStartObject("counter");
        writer.WriteString("source", definition.Counter.Source);
        writer.WriteString("sheet", definition.Counter.Sheet);
        writer.WriteString("counter", definition.Counter.Counter);
        writer.WriteEndObject();
        var applicability = definition.Applicability;
        writer.WriteStartObject("applicability");
        writer.WriteString("status", UnitCatalogReader.Name(applicability.Status));
        Optional(writer, "from", applicability.From);
        Optional(writer, "to", applicability.To);
        Strings(writer, "modules", applicability.Modules);
        Strings(writer, "conditions", applicability.Conditions);
        Optional(writer, "basis", applicability.Basis);
        writer.WriteEndObject();
        Strings(writer, "slots", definition.Slots);
        writer.WriteStartArray("values");
        foreach (var value in definition.Values)
        {
            writer.WriteStartObject();
            writer.WriteString("face", value.Face);
            writer.WriteString(value.IsTrait ? "trait" : "attribute", value.IsTrait ? value.Name : vocabulary.ShortName(definition.Kind, value.Name));
            if (value.NotInSource)
            {
                writer.WriteBoolean("recorded", false);
            }
            else if (value.NotPrinted)
            {
                writer.WriteBoolean("printed", false);
            }
            else if (value.IsTrait)
            {
                writer.WriteBoolean("present", value.TraitPresent!.Value);
            }
            else
            {
                WriteValue(writer, "value", value.Value!);
            }

            writer.WriteNumber("row", value.Source.Row);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <summary>Writes a typed value as the property <paramref name="name"/>, in the JSON type its attribute reads.</summary>
    internal static void WriteValue(Utf8JsonWriter writer, string name, Documents.UnitValue value)
    {
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

    private static void Optional(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
        {
            writer.WriteString(name, value);
        }
    }

    private static void Strings(Utf8JsonWriter writer, string name, IReadOnlyList<string> values)
    {
        writer.WriteStartArray(name);
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }
}
