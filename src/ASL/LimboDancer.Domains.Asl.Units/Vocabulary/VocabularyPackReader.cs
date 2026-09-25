using System.Text.Json;

namespace LimboDancer.Domains.Asl.Units.Vocabulary;

public sealed record VocabularyPackResult(VocabularyPack? Pack, IReadOnlyList<UnitDiagnostic> Diagnostics);

/// <summary>
/// Reads one vocabulary pack file (Unit Display Design, sections 3.1 and 8). It checks the pack on its own: names are
/// in the pack's namespace, declarations are unique, and types are known. Cross-pack references are checked when the
/// packs are combined in <see cref="UnitVocabulary"/>.
/// </summary>
public static class VocabularyPackReader
{
    private const string Code = "UNIT-VOC-001";

    public static VocabularyPackResult Read(ReadOnlySpan<byte> bytes)
    {
        var diagnostics = new List<UnitDiagnostic>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
        }
        catch (JsonException exception)
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, $"The pack is not valid JSON: line {exception.LineNumber + 1}, position {exception.BytePositionInLine + 1}."));
            return new VocabularyPackResult(null, diagnostics);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(UnitDiagnostic.Error(Code, "The pack must be a JSON object."));
                return new VocabularyPackResult(null, diagnostics);
            }

            var pack = Read(root, JsonFields.ContentHash(bytes), diagnostics);
            return new VocabularyPackResult(diagnostics.Any(d => d.Severity == UnitDiagnosticSeverity.Error) ? null : pack, diagnostics);
        }
    }

    public static VocabularyPack ReadEmbedded(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        using var stream = typeof(VocabularyPackReader).Assembly.GetManifestResourceStream($"Vocabulary.{name}.vocab.json")
            ?? throw new InvalidOperationException($"The {name} vocabulary pack is not embedded.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var result = Read(buffer.ToArray());
        return result.Pack ?? throw new InvalidDataException($"The {name} vocabulary pack is invalid: {string.Join("; ", result.Diagnostics)}");
    }

    /// <summary>The built-in <c>asl</c> pack: Personnel and support weapons.</summary>
    public static VocabularyPack Asl() => ReadEmbedded("asl");

    private static VocabularyPack Read(JsonElement root, string hash, List<UnitDiagnostic> diagnostics)
    {
        var fields = new JsonFields(diagnostics, Code);
        var name = fields.RequiredString(root, "pack", "pack") ?? string.Empty;
        var version = fields.RequiredString(root, "version", "version") ?? string.Empty;
        if (name.Length > 0 && !VocabularyNames.IsSlug(name))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-003", $"The pack name '{name}' must be a lowercase slug.", "pack"));
        }

        if (version.Length > 0 && !IsVersion(version))
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, $"The version '{version}' must be major.minor.patch.", "version"));
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        void Declare(string declared, string path)
        {
            if (!VocabularyNames.IsQualified(declared, name))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-003", $"'{declared}' must be named in the pack's namespace, as {name}:name.", path));
            }

            if (!names.Add(declared))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-004", $"'{declared}' is declared twice.", path));
            }
        }

        var sides = new List<SideDefinition>();
        foreach (var (item, path) in fields.Objects(root, "sides", "pack"))
        {
            var sideName = fields.RequiredString(item, "name", path) ?? string.Empty;
            if (!VocabularyNames.IsSlug(sideName))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-003", $"The side '{sideName}' must be a lowercase slug.", path));
            }

            sides.Add(new SideDefinition(sideName, fields.OptionalString(item, "label", path) ?? sideName));
        }

        var faces = new List<FaceDefinition>();
        foreach (var (item, path) in fields.Objects(root, "faces", "pack"))
        {
            var faceName = fields.RequiredString(item, "name", path) ?? string.Empty;
            if (!VocabularyNames.IsSlug(faceName))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-003", $"The face '{faceName}' must be a lowercase slug.", path));
            }

            faces.Add(new FaceDefinition(faceName, fields.OptionalString(item, "label", path) ?? faceName));
        }

        var attributes = new List<AttributeDefinition>();
        foreach (var (item, path) in fields.Objects(root, "attributes", "pack"))
        {
            var attributeName = fields.RequiredString(item, "name", path) ?? string.Empty;
            Declare(attributeName, path);
            var type = ParseType(fields.RequiredString(item, "type", path), path, diagnostics) ?? AttributeType.Text;
            var scopeText = fields.OptionalString(item, "scope", path) ?? "face";
            var scope = scopeText switch
            {
                "face" => AttributeScope.Face,
                "unit" => AttributeScope.Unit,
                _ => Invalid(AttributeScope.Face, $"The scope '{scopeText}' must be face or unit.", path, diagnostics),
            };
            var members = new List<EnumerationMember>();
            foreach (var (member, memberPath) in fields.Objects(item, "members", path))
            {
                var memberName = fields.RequiredString(member, "name", memberPath) ?? string.Empty;
                var label = fields.OptionalString(member, "label", memberPath) ?? memberName;
                members.Add(new EnumerationMember(memberName, label, fields.OptionalString(member, "short", memberPath) ?? label));
            }

            if (type == AttributeType.Enumeration && members.Count == 0)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-006", $"The enumeration '{attributeName}' declares no members.", path));
            }

            if (members.Select(member => member.Name).Distinct(StringComparer.Ordinal).Count() != members.Count)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-004", $"The enumeration '{attributeName}' declares a member twice.", path));
            }

            var itemType = AttributeType.Text;
            if (type == AttributeType.List)
            {
                itemType = ParseType(fields.OptionalString(item, "items", path) ?? "text", path, diagnostics) ?? AttributeType.Text;
                if (itemType is not (AttributeType.Number or AttributeType.Text))
                {
                    diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-006", $"The list '{attributeName}' must hold integer or text items.", path));
                }
            }

            attributes.Add(new AttributeDefinition(attributeName, fields.OptionalString(item, "label", path) ?? VocabularyNames.Local(attributeName),
                type, scope, fields.OptionalString(item, "unit", path), members, itemType, fields.StringList(item, "faces", path),
                fields.OptionalString(item, "rule", path))
            {
                Separator = fields.OptionalString(item, "separator", path) ?? "/",
            });
        }

        var traits = new List<TraitDefinition>();
        foreach (var (item, path) in fields.Objects(root, "traits", "pack"))
        {
            var traitName = fields.RequiredString(item, "name", path) ?? string.Empty;
            Declare(traitName, path);
            traits.Add(new TraitDefinition(traitName, fields.OptionalString(item, "label", path) ?? VocabularyNames.Local(traitName),
                fields.OptionalString(item, "short", path), fields.StringList(item, "faces", path), fields.OptionalString(item, "rule", path)));
        }

        var states = new List<StateDefinition>();
        foreach (var (item, path) in fields.Objects(root, "states", "pack"))
        {
            var stateName = fields.RequiredString(item, "name", path) ?? string.Empty;
            Declare(stateName, path);
            states.Add(new StateDefinition(stateName, fields.OptionalString(item, "label", path) ?? VocabularyNames.Local(stateName),
                fields.OptionalString(item, "short", path), fields.OptionalString(item, "face", path), fields.OptionalString(item, "group", path),
                fields.OptionalString(item, "rule", path)));
        }

        var kinds = new List<KindDefinition>();
        foreach (var (item, path) in fields.Objects(root, "kinds", "pack"))
        {
            var kindName = fields.RequiredString(item, "name", path) ?? string.Empty;
            Declare(kindName, path);
            var sizeClass = fields.OptionalInteger(item, "sizeClass", path);
            if (sizeClass is < 1 or > 3)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-006", "The size class must be 1, 2, or 3.", path));
            }

            kinds.Add(new KindDefinition(kindName, fields.OptionalString(item, "label", path) ?? VocabularyNames.Local(kindName),
                fields.OptionalString(item, "extends", path), fields.StringList(item, "faces", path), fields.StringList(item, "attributes", path),
                fields.StringList(item, "traits", path), sizeClass, fields.Templates(item, "accessibleName", path),
                fields.Templates(item, "attachedName", path), fields.OptionalString(item, "rule", path))
            {
                Facing = item.TryGetProperty("facing", out _) ? fields.OptionalBoolean(item, "facing", path) : null,
                Turret = item.TryGetProperty("turret", out _) ? fields.OptionalBoolean(item, "turret", path) : null,
            });
        }

        var augments = new List<KindAugment>();
        foreach (var (item, path) in fields.Objects(root, "augments", "pack"))
        {
            augments.Add(new KindAugment(fields.RequiredString(item, "kind", path) ?? string.Empty,
                fields.StringList(item, "attributes", path), fields.StringList(item, "traits", path)));
        }

        foreach (var side in sides.GroupBy(side => side.Name, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-004", $"The side '{side.Key}' is declared twice.", "sides"));
        }

        foreach (var face in faces.GroupBy(face => face.Name, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-004", $"The face '{face.Key}' is declared twice.", "faces"));
        }

        return new VocabularyPack(name, version, fields.OptionalString(root, "label", "pack") ?? name, fields.StringList(root, "extends", "pack"),
            sides, faces, kinds, attributes, traits, states, augments, hash);
    }

    internal static bool IsVersion(string text)
    {
        var parts = text.Split('.');
        return parts.Length == 3 && parts.All(part => part.Length > 0 && part.All(char.IsAsciiDigit));
    }

    private static AttributeType? ParseType(string? text, string path, List<UnitDiagnostic> diagnostics)
    {
        switch (text)
        {
            case null:
                return null;
            case "integer":
                return AttributeType.Number;
            case "text":
                return AttributeType.Text;
            case "enumeration":
                return AttributeType.Enumeration;
            case "rating":
                return AttributeType.Rating;
            case "list":
                return AttributeType.List;
            default:
                diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-006", $"The type '{text}' is not integer, text, enumeration, rating, or list.", path));
                return null;
        }
    }

    private static T Invalid<T>(T fallback, string message, string path, List<UnitDiagnostic> diagnostics)
    {
        diagnostics.Add(UnitDiagnostic.Error("UNIT-VOC-006", message, path));
        return fallback;
    }
}
