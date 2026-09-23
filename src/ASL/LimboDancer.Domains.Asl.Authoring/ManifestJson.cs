using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

public static class ManifestJson
{
    public const string GeneratorName = "LimboDancer.Domains.Asl.Authoring";
    public const string GeneratorVersion = "1.0.0";

    public static string SerializeRegistry(SourceRegistryManifest manifest)
    {
        return Write(writer => WriteRegistry(writer, manifest));
    }

    public static string SerializeVerificationSample(
        string registryId,
        IReadOnlyList<SourceFragment> fragments)
    {
        return Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("schemaVersion", AslSourceRegistryBuilder.SchemaVersion);
            writer.WriteString("registryId", registryId);
            writer.WriteString("purpose", AslAuthoringManifestGenerator.VerificationPurpose);
            writer.WriteString("selectionPolicy", AslAuthoringManifestGenerator.VerificationSelectionPolicy);
            writer.WriteBoolean("requiresAuthoritativeEditionComparison", true);
            writer.WritePropertyName("fragments");
            writer.WriteStartArray();
            foreach (var fragment in fragments)
            {
                WriteFragment(writer, fragment);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        });
    }

    public static void WriteFile(string path, string content)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private static string Write(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(
            stream,
            new JsonWriterOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Indented = true,
            }))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    private static void WriteRegistry(Utf8JsonWriter writer, SourceRegistryManifest manifest)
    {
        writer.WriteStartObject();
        writer.WriteString("schemaVersion", manifest.SchemaVersion);
        writer.WriteString("registryId", manifest.RegistryId);
        writer.WriteString("edition", manifest.Edition);
        writer.WriteString("sourceCommit", manifest.SourceCommit);
        writer.WriteString("sourceRoot", manifest.SourceRoot);
        writer.WritePropertyName("generator");
        writer.WriteStartObject();
        writer.WriteString("name", GeneratorName);
        writer.WriteString("version", GeneratorVersion);
        writer.WriteEndObject();
        writer.WritePropertyName("distribution");
        writer.WriteStartObject();
        writer.WriteBoolean("containsCopyrightedMaterial", true);
        writer.WriteString("access", "repository-controlled");
        writer.WriteString("redistribution", "source-license-governed");
        writer.WriteEndObject();
        writer.WritePropertyName("conversionTool");
        writer.WriteStartObject();
        writer.WriteString("path", manifest.ConversionTool.Path);
        writer.WriteString("sha256", manifest.ConversionTool.Sha256);
        writer.WriteEndObject();
        writer.WritePropertyName("artifacts");
        writer.WriteStartArray();
        foreach (var artifact in manifest.Artifacts)
        {
            WriteArtifact(writer, artifact);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteArtifact(Utf8JsonWriter writer, SourceArtifact artifact)
    {
        writer.WriteStartObject();
        writer.WriteString("sourceId", artifact.SourceId);
        writer.WriteString("path", artifact.Path);
        writer.WriteString("kind", ArtifactKindValue(artifact.Kind));
        writer.WriteString("sha256", artifact.Sha256);
        writer.WriteNumber("sizeBytes", artifact.SizeBytes);
        if (artifact.Chapter is not null)
        {
            writer.WriteString("chapter", artifact.Chapter);
        }

        if (artifact.StartPage is not null)
        {
            writer.WriteNumber("startPage", artifact.StartPage.Value);
        }

        if (artifact.EndPage is not null)
        {
            writer.WriteNumber("endPage", artifact.EndPage.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteFragment(Utf8JsonWriter writer, SourceFragment fragment)
    {
        writer.WriteStartObject();
        writer.WriteString("fragmentId", fragment.FragmentId);
        writer.WriteString("sourceId", fragment.SourceId);
        writer.WriteString("sourcePath", fragment.SourcePath);
        writer.WriteString("sourceSha256", fragment.SourceSha256);
        writer.WriteString("kind", FragmentKindValue(fragment.Kind));
        writer.WriteString("contentSha256", fragment.ContentSha256);
        writer.WritePropertyName("locator");
        writer.WriteStartObject();
        writer.WriteNumber("startLine", fragment.Locator.StartLine);
        writer.WriteNumber("endLine", fragment.Locator.EndLine);
        if (fragment.Locator.StartPage is not null)
        {
            writer.WriteNumber("startPage", fragment.Locator.StartPage.Value);
        }

        if (fragment.Locator.EndPage is not null)
        {
            writer.WriteNumber("endPage", fragment.Locator.EndPage.Value);
        }

        writer.WritePropertyName("headingPath");
        writer.WriteStartArray();
        foreach (var heading in fragment.Locator.HeadingPath)
        {
            writer.WriteStringValue(heading);
        }

        writer.WriteEndArray();
        if (fragment.Locator.PublishedElementId is not null)
        {
            writer.WriteString("publishedElementId", fragment.Locator.PublishedElementId);
        }

        if (fragment.Locator.NormalizedElementId is not null)
        {
            writer.WriteString("normalizedElementId", fragment.Locator.NormalizedElementId);
        }

        writer.WriteEndObject();
        writer.WritePropertyName("dependencies");
        writer.WriteStartArray();
        foreach (var dependency in fragment.Dependencies)
        {
            writer.WriteStringValue(dependency);
        }

        writer.WriteEndArray();
        writer.WriteBoolean("hasFootnoteMarkers", fragment.HasFootnoteMarkers);
        writer.WriteString("verificationStatus", VerificationStatusValue(fragment.VerificationStatus));
        writer.WriteEndObject();
    }

    private static string ArtifactKindValue(SourceArtifactKind value) => value switch
    {
        SourceArtifactKind.Markdown => "markdown",
        SourceArtifactKind.Image => "image",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string FragmentKindValue(SourceFragmentKind value) => value switch
    {
        SourceFragmentKind.Heading => "heading",
        SourceFragmentKind.RuleText => "ruleText",
        SourceFragmentKind.RuleContinuation => "ruleContinuation",
        SourceFragmentKind.FigureReference => "figureReference",
        SourceFragmentKind.StructuredText => "structuredText",
        SourceFragmentKind.Paragraph => "paragraph",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string VerificationStatusValue(VerificationStatus value) => value switch
    {
        VerificationStatus.Unverified => "unverified",
        VerificationStatus.Verified => "verified",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
