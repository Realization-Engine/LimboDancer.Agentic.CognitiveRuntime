using System.Text.Json;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Catalog;

public sealed record UnitCatalogResult(UnitCatalog? Catalog, IReadOnlyList<UnitDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error);
}

/// <summary>
/// Reads and validates a definition catalog against a vocabulary (Scenario A1 Catalog Design, sections 4 to 7). Any
/// error refuses the whole catalog: a catalog is published or used as one versioned unit (ASL-UNIT-080).
/// </summary>
public static class UnitCatalogReader
{
    public const int SchemaVersion = 1;

    private static readonly HashSet<string> CatalogFields = new(StringComparer.Ordinal)
    {
        "schemaVersion", "catalog", "version", "label", "publication", "vocabulary", "sources", "slots", "definitions", "note",
    };

    private static readonly HashSet<string> DefinitionFields = new(StringComparer.Ordinal)
    {
        "id", "kind", "kindRow", "nationality", "nationalityRow", "class", "counter", "applicability", "slots", "values", "note",
    };

    public static UnitCatalogResult Read(ReadOnlySpan<byte> json, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        var diagnostics = new List<UnitDiagnostic>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json.ToArray());
        }
        catch (JsonException exception)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-001",
                $"Not valid JSON: line {exception.LineNumber + 1}, position {exception.BytePositionInLine + 1}."));
            return new UnitCatalogResult(null, diagnostics);
        }

        using (document)
        {
            var catalog = ReadCatalog(document.RootElement, vocabulary, diagnostics);
            if (catalog is null || diagnostics.Any(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error))
            {
                return new UnitCatalogResult(null, diagnostics);
            }

            // The identity hashes the canonical form, so layout does not change it and content always does.
            var identity = catalog.Identity with
            {
                Hash = UnitCatalog.Hash(UnitCatalogJson.Write(catalog, vocabulary))
            };
            return new UnitCatalogResult(catalog with
            {
                Identity = identity
            }, diagnostics);
        }
    }

    public static UnitCatalogResult Read(string json, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Read(System.Text.Encoding.UTF8.GetBytes(json), vocabulary);
    }

    /// <summary>Whether text is a month such as <c>1944-06</c>.</summary>
    public static bool IsYearMonth(string text) =>
        text is { Length: 7 } && text[4] == '-' && text[..4].All(char.IsAsciiDigit) && text[5..].All(char.IsAsciiDigit)
        && text[5..] is var month && string.CompareOrdinal(month, "01") >= 0 && string.CompareOrdinal(month, "12") <= 0;

    internal static string Name(CatalogPublication publication) => publication switch
    {
        CatalogPublication.Synthetic => "synthetic",
        CatalogPublication.Draft => "draft",
        _ => "published",
    };

    internal static string Name(CatalogSourceStatus status) => status switch
    {
        CatalogSourceStatus.Synthetic => "synthetic",
        CatalogSourceStatus.AwaitingTranscription => "awaiting-transcription",
        CatalogSourceStatus.TranscribedUnreviewed => "transcribed-unreviewed",
        _ => "reviewed",
    };

    internal static string Name(ApplicabilityStatus status) => status == ApplicabilityStatus.Reviewed ? "reviewed" : "unreviewed";

    private static UnitCatalog? ReadCatalog(JsonElement root, UnitVocabulary vocabulary, List<UnitDiagnostic> diagnostics)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-001", "A catalog must be a JSON object.", "$"));
            return null;
        }

        var fields = new JsonFields(diagnostics, "UNIT-CAT-001");
        WarnUnknown(root, CatalogFields, "$", diagnostics);
        var schema = fields.OptionalInteger(root, "schemaVersion", "$");
        if (schema != SchemaVersion)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-002", $"The schema version must be {SchemaVersion}.", "$"));
        }

        var id = fields.RequiredString(root, "catalog", "$");
        if (id is not null && !VocabularyNames.IsSlug(id))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-003", $"The catalog id '{id}' must be a lowercase slug.", "$"));
        }

        var version = fields.RequiredString(root, "version", "$");
        if (version is not null && !VocabularyPackReader.IsVersion(version))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-003", $"The version '{version}' must be major.minor.patch.", "$"));
        }

        var label = fields.RequiredString(root, "label", "$") ?? string.Empty;
        var publication = fields.RequiredString(root, "publication", "$") switch
        {
            "synthetic" => CatalogPublication.Synthetic,
            "draft" => CatalogPublication.Draft,
            "published" => CatalogPublication.Published,
            null => (CatalogPublication?)null,
            var other => Invalid<CatalogPublication>(diagnostics, "UNIT-CAT-003", $"'{other}' is not synthetic, draft, or published.", "$"),
        };

        var packs = fields.StringList(root, "vocabulary", "$");
        if (packs.Count == 0)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-004", "The catalog records no vocabulary version.", "$"));
        }

        foreach (var reference in packs.Where(reference => !vocabulary.Serves(reference)))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-004", $"The catalog was written against '{reference}', which is not loaded ({vocabulary.Identity}).", "$"));
        }

        var sources = ReadSources(root, fields, diagnostics);
        var slots = ReadSlots(root, vocabulary, fields, diagnostics);
        var definitions = new List<UnitDefinition>();
        foreach (var (item, path) in fields.Objects(root, "definitions", "$"))
        {
            if (ReadDefinition(item, path, vocabulary, sources, slots, fields, diagnostics) is { } definition)
            {
                definitions.Add(definition);
            }
        }

        CheckDefinitions(definitions, diagnostics);
        if (publication is { } value)
        {
            CheckPublication(value, sources, slots, definitions, diagnostics);
        }

        if (id is null || version is null || publication is null)
        {
            return null;
        }

        return new UnitCatalog(new CatalogIdentity(id, version, string.Empty), label, publication.Value, packs, sources, slots, definitions);
    }

    private static List<CatalogSource> ReadSources(JsonElement root, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        var sources = new List<CatalogSource>();
        foreach (var (item, path) in fields.Objects(root, "sources", "$"))
        {
            var id = fields.RequiredString(item, "id", path);
            var status = fields.RequiredString(item, "status", path) switch
            {
                "synthetic" => CatalogSourceStatus.Synthetic,
                "awaiting-transcription" => CatalogSourceStatus.AwaitingTranscription,
                "transcribed-unreviewed" => CatalogSourceStatus.TranscribedUnreviewed,
                "reviewed" => CatalogSourceStatus.Reviewed,
                null => (CatalogSourceStatus?)null,
                var other => Invalid<CatalogSourceStatus>(diagnostics, "UNIT-CAT-005",
                    $"'{other}' is not synthetic, awaiting-transcription, transcribed-unreviewed, or reviewed.", path),
            };

            var source = new CatalogSource(id ?? string.Empty, status ?? CatalogSourceStatus.Synthetic,
                fields.OptionalString(item, "record", path), fields.OptionalString(item, "recordSha256", path),
                fields.OptionalString(item, "transcription", path), fields.OptionalString(item, "transcriptionSha256", path),
                fields.OptionalString(item, "transcriber", path), fields.OptionalString(item, "reviewer", path));
            if (id is null || status is null)
            {
                continue;
            }

            if (sources.Any(existing => existing.Id == id))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-005", $"The source '{id}' is declared twice.", path));
                continue;
            }

            if (status != CatalogSourceStatus.Synthetic)
            {
                foreach (var (name, value) in new[] { ("record", source.Record), ("transcription", source.Transcription), ("transcriber", source.Transcriber) })
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-005", $"A registered source needs its '{name}'.", path));
                    }
                }

                foreach (var (name, value) in new[] { ("recordSha256", source.RecordSha256), ("transcriptionSha256", source.TranscriptionSha256) })
                {
                    if (value is null || value.Length != 64 || !value.All(character => char.IsAsciiHexDigitLower(character) || char.IsAsciiDigit(character)))
                    {
                        diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-005", $"'{name}' must be a lowercase SHA-256.", path));
                    }
                }
            }

            if (status == CatalogSourceStatus.Reviewed && string.IsNullOrWhiteSpace(source.Reviewer))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-005", "A reviewed source names its reviewer.", path));
            }

            sources.Add(source);
        }

        return sources;
    }

    private static List<CatalogSlot> ReadSlots(JsonElement root, UnitVocabulary vocabulary, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        var slots = new List<CatalogSlot>();
        foreach (var (item, path) in fields.Objects(root, "slots", "$"))
        {
            var id = fields.RequiredString(item, "id", path);
            var kind = fields.RequiredString(item, "kind", path);
            var label = fields.RequiredString(item, "label", path) ?? string.Empty;
            if (id is null || kind is null)
            {
                continue;
            }

            if (!VocabularyNames.IsSlug(id) || slots.Any(slot => slot.Id == id))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-006", $"The slot id '{id}' must be a lowercase slug used once.", path));
                continue;
            }

            if (!vocabulary.HasKind(kind))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-006", $"The slot's kind '{kind}' is not declared.", path));
                continue;
            }

            slots.Add(new CatalogSlot(id, kind, label));
        }

        return slots;
    }

    private static UnitDefinition? ReadDefinition(JsonElement item, string path, UnitVocabulary vocabulary, List<CatalogSource> sources,
        List<CatalogSlot> slots, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        var errors = Errors(diagnostics);
        WarnUnknown(item, DefinitionFields, path, diagnostics);
        var id = fields.RequiredString(item, "id", path);
        if (id is not null && !VocabularyNames.IsSlug(id))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-007", $"The definition id '{id}' must be a lowercase slug.", path));
        }

        var kind = fields.RequiredString(item, "kind", path);
        if (kind is not null && !vocabulary.HasKind(kind))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-008", $"The kind '{kind}' is not declared.", path));
            kind = null;
        }

        var nationality = fields.RequiredString(item, "nationality", path);
        if (nationality is not null && !vocabulary.TryGetSide(nationality, out _))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-008", $"The nationality '{nationality}' is not a declared side.", path));
        }

        var kindRow = Row(fields.OptionalInteger(item, "kindRow", path), "kindRow", path, diagnostics);
        var nationalityRow = Row(fields.OptionalInteger(item, "nationalityRow", path), "nationalityRow", path, diagnostics);
        var @class = fields.OptionalString(item, "class", path);
        if (@class is not null && (!vocabulary.TryGetAttribute("asl:class", out var classAttribute) || classAttribute.Member(@class) is null))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-009", $"The class '{@class}' is not a member of asl:class.", path));
        }

        CounterReference? counter = null;
        if (!item.TryGetProperty("counter", out var counterElement) || counterElement.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-001", "'counter' must be an object with source, sheet, and counter.", path));
        }
        else
        {
            var counterPath = path + ".counter";
            var source = fields.RequiredString(counterElement, "source", counterPath);
            var sheet = fields.RequiredString(counterElement, "sheet", counterPath);
            var counterId = fields.RequiredString(counterElement, "counter", counterPath);
            if (source is not null && sources.All(declared => declared.Id != source))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-010", $"The source '{source}' is not declared.", counterPath));
            }

            if (source is not null && sheet is not null && counterId is not null)
            {
                counter = new CounterReference(source, sheet, counterId);
            }
        }

        var applicability = ReadApplicability(item, path, fields, diagnostics);
        var slotNames = fields.StringList(item, "slots", path);
        foreach (var slotName in slotNames)
        {
            if (slots.FirstOrDefault(slot => slot.Id == slotName) is not { } slot)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-013", $"The slot '{slotName}' is not declared.", path));
            }
            else if (kind is not null && !vocabulary.IsA(kind, slot.Kind))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-013", $"A {kind} cannot fill the slot '{slotName}', which needs a {slot.Kind}.", path));
            }
        }

        var values = kind is null || counter is null ? [] : ReadValues(item, path, kind, counter, vocabulary, fields, diagnostics);
        var printedClass = values.FirstOrDefault(value => value.Name == "asl:class");
        if (printedClass is not null && printedClass.Value?.Text != @class)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-009",
                $"The key's class '{@class ?? "none"}' differs from the printed class '{printedClass.Value?.Text ?? "not printed"}'.", path));
        }
        else if (printedClass is null && @class is not null)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-009", $"The key's class '{@class}' has no printed class value behind it.", path));
        }

        if (Errors(diagnostics) > errors || id is null || kind is null || nationality is null || counter is null || applicability is null
            || kindRow is null || nationalityRow is null)
        {
            return null;
        }

        return new UnitDefinition(id, kind, nationality, @class, counter, kindRow.Value, nationalityRow.Value, applicability, slotNames, values);
    }

    private static Applicability? ReadApplicability(JsonElement item, string path, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        if (!item.TryGetProperty("applicability", out var element) || element.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-015", "'applicability' is required; write status 'unreviewed' when it is not yet known.", path));
            return null;
        }

        var applicabilityPath = path + ".applicability";
        var status = fields.RequiredString(element, "status", applicabilityPath) switch
        {
            "reviewed" => ApplicabilityStatus.Reviewed,
            "unreviewed" => ApplicabilityStatus.Unreviewed,
            null => (ApplicabilityStatus?)null,
            var other => Invalid<ApplicabilityStatus>(diagnostics, "UNIT-CAT-015", $"'{other}' is not reviewed or unreviewed.", applicabilityPath),
        };

        var from = fields.OptionalString(element, "from", applicabilityPath);
        var to = fields.OptionalString(element, "to", applicabilityPath);
        var basis = fields.OptionalString(element, "basis", applicabilityPath);
        foreach (var date in new[] { from, to }.OfType<string>().Where(date => !IsYearMonth(date)))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-015", $"'{date}' is not a date such as 1944-06.", applicabilityPath));
        }

        if (from is not null && to is not null && string.CompareOrdinal(from, to) > 0)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-015", $"The applicability starts ({from}) after it ends ({to}).", applicabilityPath));
        }

        if (status == ApplicabilityStatus.Reviewed && string.IsNullOrWhiteSpace(basis))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-015", "Reviewed applicability names its basis: a rule and page, or a registered source.", applicabilityPath));
        }

        if (status == ApplicabilityStatus.Unreviewed && (from is not null || to is not null))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-015", "Unreviewed applicability carries no dates.", applicabilityPath));
        }

        return status is null
            ? null
            : new Applicability(status.Value, from, to, fields.StringList(element, "modules", applicabilityPath),
                fields.StringList(element, "conditions", applicabilityPath), basis);
    }

    private static List<PrintedValue> ReadValues(JsonElement item, string path, string kind, CounterReference counter, UnitVocabulary vocabulary,
        JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        var values = new List<PrintedValue>();
        var faces = vocabulary.Faces(kind);
        foreach (var (entry, entryPath) in fields.Objects(item, "values", path))
        {
            var face = fields.RequiredString(entry, "face", entryPath);
            var row = Row(fields.OptionalInteger(entry, "row", entryPath), "row", entryPath, diagnostics);
            if (face is not null && !faces.Contains(face))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-011", $"The kind '{kind}' has no '{face}' face.", entryPath));
                continue;
            }

            var attributeName = fields.OptionalString(entry, "attribute", entryPath);
            var traitName = fields.OptionalString(entry, "trait", entryPath);
            if ((attributeName is null) == (traitName is null))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-011", "A value names exactly one 'attribute' or 'trait'.", entryPath));
                continue;
            }

            if (face is null || row is null)
            {
                continue;
            }

            var source = new ValueSource(counter, face, row.Value);
            var value = traitName is not null
                ? ReadTrait(entry, entryPath, kind, face, traitName, source, vocabulary, diagnostics)
                : ReadAttribute(entry, entryPath, kind, face, attributeName!, source, vocabulary, fields, diagnostics);
            if (value is null)
            {
                continue;
            }

            if (values.Any(existing => existing.Face == value.Face && existing.Name == value.Name))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-011", $"'{value.Name}' is recorded twice on the {face} face.", entryPath));
                continue;
            }

            values.Add(value);
        }

        return values;
    }

    private static PrintedValue? ReadTrait(JsonElement entry, string path, string kind, string face, string name, ValueSource source,
        UnitVocabulary vocabulary, List<UnitDiagnostic> diagnostics)
    {
        if (!vocabulary.TryGetTrait(name, out var trait) || !vocabulary.AcceptedTraits(kind).Contains(name))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-011", $"The trait '{name}' is not declared for '{kind}'.", path));
            return null;
        }

        if (trait.Faces.Count > 0 && !trait.Faces.Contains(face))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-011", $"The trait '{name}' belongs on the {string.Join(" or ", trait.Faces)} face.", path));
            return null;
        }

        if (!entry.TryGetProperty("present", out var present) || present.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-011", "A trait records 'present' as true or false.", path));
            return null;
        }

        return new PrintedValue(face, name, null, present.GetBoolean(), source);
    }

    private static PrintedValue? ReadAttribute(JsonElement entry, string path, string kind, string face, string name, ValueSource source,
        UnitVocabulary vocabulary, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        if (!vocabulary.TryResolveAttribute(kind, name, out var attribute, out var ambiguous))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-011", ambiguous
                ? $"'{name}' names more than one attribute of '{kind}'; write the pack prefix."
                : $"The attribute '{name}' is not declared for '{kind}'.", path));
            return null;
        }

        if (attribute.Faces.Count > 0 && !attribute.Faces.Contains(face))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-011", $"'{name}' belongs on the {string.Join(" or ", attribute.Faces)} face.", path));
            return null;
        }

        var hasValue = entry.TryGetProperty("value", out var element);
        var notPrinted = entry.TryGetProperty("printed", out var printed) && printed.ValueKind == JsonValueKind.False;
        if (hasValue == notPrinted)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-011", "An attribute records either a 'value' or \"printed\": false.", path));
            return null;
        }

        if (notPrinted)
        {
            return new PrintedValue(face, attribute.Name, null, null, source);
        }

        var converted = new List<UnitDiagnostic>();
        var value = UnitDocumentReader.ConvertValue(attribute, element, name, path, converted);
        diagnostics.AddRange(converted.Select(diagnostic => diagnostic with { Code = "UNIT-CAT-011" }));
        return value is null ? null : new PrintedValue(face, attribute.Name, value, null, source);
    }

    private static void CheckDefinitions(List<UnitDefinition> definitions, List<UnitDiagnostic> diagnostics)
    {
        foreach (var group in definitions.GroupBy(definition => definition.Id, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-007", $"The definition id '{group.Key}' is used twice."));
        }

        foreach (var group in definitions.GroupBy(definition => definition.Key).Where(group => group.Count() > 1))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-014",
                $"The definitions {string.Join(" and ", group.Select(definition => definition.Id))} have the same key."));
        }

        // Each transcription row is one printed value, so no row may back two values.
        var rows = definitions.SelectMany(definition => definition.Values.Select(value => value.Source.Row)
            .Append(definition.KindRow)
            .Append(definition.NationalityRow)
            .Select(row => (definition.Counter.Source, Row: row, definition.Id)));
        foreach (var group in rows.GroupBy(row => (row.Source, row.Row)).Where(group => group.Count() > 1))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-011",
                $"Row {group.Key.Row} of '{group.Key.Source}' backs more than one value ({string.Join(", ", group.Select(row => row.Id).Distinct())})."));
        }
    }

    private static void CheckPublication(CatalogPublication publication, List<CatalogSource> sources, List<CatalogSlot> slots,
        List<UnitDefinition> definitions, List<UnitDiagnostic> diagnostics)
    {
        foreach (var source in sources)
        {
            var allowed = publication switch
            {
                CatalogPublication.Synthetic => source.Status == CatalogSourceStatus.Synthetic,
                CatalogPublication.Draft => source.Status is CatalogSourceStatus.TranscribedUnreviewed or CatalogSourceStatus.Reviewed,
                _ => source.Status == CatalogSourceStatus.Reviewed,
            };

            if (!allowed)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-016",
                    $"A {Name(publication)} catalog cannot use the {Name(source.Status)} source '{source.Id}'."));
            }

            if (publication == CatalogPublication.Published && source.Reviewer is not null
                && string.Equals(source.Reviewer.Trim(), source.Transcriber?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-016",
                    $"The source '{source.Id}' was reviewed by its transcriber; publication needs a second person (D1)."));
            }
        }

        if (publication != CatalogPublication.Synthetic)
        {
            foreach (var slot in slots.Where(slot => !definitions.Any(definition => definition.Slots.Contains(slot.Id, StringComparer.Ordinal))))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-016", $"No definition fills the slot '{slot.Id}'."));
            }
        }
    }

    private static int? Row(int? row, string name, string path, List<UnitDiagnostic> diagnostics)
    {
        if (row is null or < 1)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-011", $"'{name}' must be a transcription row number of 1 or more.", path));
            return null;
        }

        return row;
    }

    private static T? Invalid<T>(List<UnitDiagnostic> diagnostics, string code, string message, string path)
        where T : struct
    {
        diagnostics.Add(UnitDiagnostic.Error(code, message, path));
        return null;
    }

    private static void WarnUnknown(JsonElement element, HashSet<string> known, string path, List<UnitDiagnostic> diagnostics)
    {
        foreach (var property in element.EnumerateObject().Where(property => !known.Contains(property.Name)))
        {
            diagnostics.Add(UnitDiagnostic.Warning("UNIT-CAT-017", $"'{property.Name}' is not a catalog field and is ignored.", path));
        }
    }

    private static int Errors(List<UnitDiagnostic> diagnostics) =>
        diagnostics.Count(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error);
}
