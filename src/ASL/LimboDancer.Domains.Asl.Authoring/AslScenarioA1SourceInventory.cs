using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

public sealed record AslScenarioA1FragmentEvidence(
    string FragmentId,
    string ContentSha256,
    SourceFragmentKind Kind,
    int StartLine,
    int EndLine,
    int? ConversionStartPage,
    int? ConversionEndPage,
    bool HasFootnoteMarkers,
    IReadOnlyList<string> Dependencies);

public sealed record AslScenarioA1RuleEvidence(
    string NormalizedRuleId,
    string SourceId,
    string SourceSha256,
    int PdfStartPage,
    int PdfEndPage,
    IReadOnlyList<AslScenarioA1FragmentEvidence> Fragments);

public sealed record AslScenarioA1SourceInventory(
    string RegistryId,
    string Edition,
    string PdfSha256,
    IReadOnlyList<AslScenarioA1RuleEvidence> Rules)
{
    // These physical PDF page locations were visually checked against the supplied file.
    // The source hash prevents applying the correction to a different conversion.
    public const string PdfDigest = "957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247";
    private const string ChapterASha256 = "6347534e64f739cacc57dd75c3d604997723fd0b4988565609deeff007a8843d";
    private const string ChapterBSha256 = "836b1ce5f845efd1c21250a9e6a39e4054ea9398f370305bc3771b6e7f826982";

    private static readonly (string Id, string SourceSha256, int StartPage, int EndPage)[] Selection =
    [
        ("A2.8", ChapterASha256, 47, 47),
        ("A4.14", ChapterASha256, 49, 49),
        ("A4.15", ChapterASha256, 49, 49),
        ("A4.7", ChapterASha256, 52, 52),
        ("A12.15", ChapterASha256, 78, 78),
        ("B23.4", ChapterBSha256, 136, 136),
        ("B23.922", ChapterBSha256, 140, 141),
        ("B23.9221", ChapterBSha256, 141, 141),
    ];

    public static AslScenarioA1SourceInventory Extract(GeneratedManifests manifests)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        if (manifests.Registry.Edition != AslSourceRegistryBuilder.Edition
            || manifests.Registry.RegistryId != AslSourceRegistryBuilder.RegistryId)
        {
            throw new InvalidOperationException("Scenario A1 source registry identity or edition differs from the pinned baseline.");
        }

        var rules = new List<AslScenarioA1RuleEvidence>();
        foreach (var (id, sourceSha256, pdfStartPage, pdfEndPage) in Selection)
        {
            var start = manifests.Fragments.Where(fragment =>
                    fragment.SourceSha256 == sourceSha256
                    && fragment.Locator.NormalizedElementId == id
                    && fragment.Kind == SourceFragmentKind.RuleText)
                .ToArray();
            if (start.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one pinned Scenario A1 rule declaration: {id}.");
            }

            var head = start[0];
            var fragments = manifests.Fragments.Where(fragment =>
                    fragment.SourceId == head.SourceId
                    && fragment.Locator.NormalizedElementId == id
                    && (fragment.Kind == SourceFragmentKind.RuleText
                        || fragment.Kind == SourceFragmentKind.RuleContinuation
                        || fragment.Kind == SourceFragmentKind.FigureReference)
                    && fragment.Locator.StartLine >= head.Locator.StartLine)
                .OrderBy(fragment => fragment.Locator.StartLine)
                .ToArray();
            if (fragments.Length == 0 || fragments[0].FragmentId != head.FragmentId
                || fragments.Skip(1).Any(fragment => fragment.Kind is not
                    (SourceFragmentKind.RuleContinuation or SourceFragmentKind.FigureReference)))
            {
                throw new InvalidOperationException($"Scenario A1 rule evidence is incomplete: {id}.");
            }

            // A source rule may cross a conversion page marker: B23.922 is two exact
            // fragments, with no rewritten or synthetic combined source fragment.
            if (id == "B23.922"
                && (fragments.Length != 2
                    || fragments[0].Locator.StartPage != 140
                    || fragments[1].Locator.StartPage != 141))
            {
                throw new InvalidOperationException("B23.922 page-crossing evidence changed; review the conversion again.");
            }

            rules.Add(new AslScenarioA1RuleEvidence(
                id,
                head.SourceId,
                sourceSha256,
                pdfStartPage,
                pdfEndPage,
                fragments.Select(fragment => new AslScenarioA1FragmentEvidence(
                    fragment.FragmentId,
                    fragment.ContentSha256,
                    fragment.Kind,
                    fragment.Locator.StartLine,
                    fragment.Locator.EndLine,
                    fragment.Locator.StartPage,
                    fragment.Locator.EndPage,
                    fragment.HasFootnoteMarkers,
                    fragment.Dependencies)).ToArray()));
        }

        return new AslScenarioA1SourceInventory(
            manifests.Registry.RegistryId,
            manifests.Registry.Edition,
            PdfDigest,
            rules);
    }

    public string Serialize()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = true,
        }))
        {
            writer.WriteStartObject();
            writer.WriteString("schemaVersion", "1.0.0");
            writer.WriteString("status", "candidate-unverified");
            writer.WriteString("registryId", RegistryId);
            writer.WriteString("edition", Edition);
            writer.WriteString("pdfSha256", PdfSha256);
            writer.WritePropertyName("rules");
            writer.WriteStartArray();
            foreach (var rule in Rules)
            {
                writer.WriteStartObject();
                writer.WriteString("normalizedRuleId", rule.NormalizedRuleId);
                writer.WriteString("sourceId", rule.SourceId);
                writer.WriteString("sourceSha256", rule.SourceSha256);
                writer.WriteNumber("pdfStartPage", rule.PdfStartPage);
                writer.WriteNumber("pdfEndPage", rule.PdfEndPage);
                writer.WritePropertyName("fragments");
                writer.WriteStartArray();
                foreach (var fragment in rule.Fragments)
                {
                    writer.WriteStartObject();
                    writer.WriteString("fragmentId", fragment.FragmentId);
                    writer.WriteString("contentSha256", fragment.ContentSha256);
                    writer.WriteString("kind", fragment.Kind.ToString());
                    writer.WriteNumber("startLine", fragment.StartLine);
                    writer.WriteNumber("endLine", fragment.EndLine);
                    if (fragment.ConversionStartPage is int startPage)
                    {
                        writer.WriteNumber("conversionStartPage", startPage);
                    }
                    if (fragment.ConversionEndPage is int endPage)
                    {
                        writer.WriteNumber("conversionEndPage", endPage);
                    }
                    writer.WriteBoolean("hasFootnoteMarkers", fragment.HasFootnoteMarkers);
                    writer.WritePropertyName("dependencies");
                    writer.WriteStartArray();
                    foreach (var dependency in fragment.Dependencies)
                    {
                        writer.WriteStringValue(dependency);
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }
}
