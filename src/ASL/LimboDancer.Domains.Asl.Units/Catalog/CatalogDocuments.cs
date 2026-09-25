using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Catalog;

/// <summary>A unit document made from a definition, with the definition and catalog version it came from.</summary>
public sealed record CatalogUnitDocument(UnitDocument Document, DefinitionReference Definition);

public sealed record CatalogDocumentResult(CatalogUnitDocument? Result, IReadOnlyList<UnitDiagnostic> Diagnostics);

/// <summary>
/// Makes a unit document from a definition (Scenario A1 Catalog Design, section 8): the nationality becomes the side,
/// printed attribute values go on the face they are printed on (or on the unit, for attributes the vocabulary scopes
/// to the whole unit), and present traits become the face's traits. Values the counter does not print and absent
/// traits are left out. The document is read back through <see cref="UnitDocumentReader"/>, so it is always valid
/// against the vocabulary or refused with diagnostics. It shows printed values only, never effective ones.
/// </summary>
public static class CatalogDocuments
{
    private static readonly JsonWriterOptions Options = new() { Indented = false };

    public static CatalogDocumentResult ToDocument(UnitCatalog catalog, UnitDefinition definition, UnitVocabulary vocabulary, string documentId,
        string? location = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(vocabulary);
        ArgumentNullException.ThrowIfNull(documentId);
        if (catalog.Definition(definition.Id) != definition)
        {
            throw new ArgumentException($"The definition '{definition.Id}' is not in {catalog.Identity}.", nameof(definition));
        }

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, Options))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("vocabulary");
            foreach (var pack in catalog.Vocabulary)
            {
                writer.WriteStringValue(pack);
            }

            writer.WriteEndArray();
            writer.WriteString("id", documentId);
            writer.WriteString("kind", definition.Kind);
            writer.WriteString("side", definition.Nationality);
            if (location is not null)
            {
                writer.WriteString("location", location);
            }

            var faceValues = definition.Values.Where(value => value.Value is not null && value.Value.Attribute.Scope == AttributeScope.Face).ToArray();
            var traits = definition.Values.Where(value => value.TraitPresent == true).ToArray();
            writer.WriteStartObject("faces");
            foreach (var face in vocabulary.Faces(definition.Kind))
            {
                var values = faceValues.Where(value => value.Face == face).ToArray();
                var faceTraits = traits.Where(value => value.Face == face).ToArray();
                if (values.Length == 0 && faceTraits.Length == 0)
                {
                    continue;
                }

                writer.WriteStartObject(face);
                foreach (var value in values)
                {
                    UnitCatalogJson.WriteValue(writer, value.Name, value.Value!);
                }

                if (faceTraits.Length > 0)
                {
                    writer.WriteStartArray("traits");
                    foreach (var trait in faceTraits)
                    {
                        writer.WriteStringValue(trait.Name);
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            var unitValues = definition.Values.Where(value => value.Value?.Attribute.Scope == AttributeScope.Unit).ToArray();
            if (unitValues.Length > 0)
            {
                writer.WriteStartObject("unit");
                foreach (var value in unitValues)
                {
                    UnitCatalogJson.WriteValue(writer, value.Name, value.Value!);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        var read = UnitDocumentReader.Read(buffer.ToArray(), vocabulary);
        return read.HasErrors || read.Documents.Count != 1
            ? new CatalogDocumentResult(null, read.Diagnostics)
            : new CatalogDocumentResult(new CatalogUnitDocument(read.Documents[0], catalog.Reference(definition)), read.Diagnostics);
    }
}
