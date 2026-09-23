namespace LimboDancer.Domains.Asl.Authoring;

public enum SourceArtifactKind
{
    Markdown,
    Image,
}

public enum SourceFragmentKind
{
    Heading,
    RuleText,
    RuleContinuation,
    FigureReference,
    StructuredText,
    Paragraph,
}

public enum VerificationStatus
{
    Unverified,
    Verified,
}

public sealed record SourceArtifact(
    string SourceId,
    string Path,
    SourceArtifactKind Kind,
    string Sha256,
    long SizeBytes,
    string? Chapter = null,
    int? StartPage = null,
    int? EndPage = null);

public sealed record ConversionTool(string Path, string Sha256);

public sealed record SourceRegistryManifest(
    string SchemaVersion,
    string RegistryId,
    string Edition,
    string SourceCommit,
    string SourceRoot,
    ConversionTool ConversionTool,
    IReadOnlyList<SourceArtifact> Artifacts);

public sealed record SourceLocator(
    int StartLine,
    int EndLine,
    int? StartPage,
    int? EndPage,
    IReadOnlyList<string> HeadingPath,
    string? PublishedElementId = null,
    string? NormalizedElementId = null);

public sealed record SourceFragment(
    string FragmentId,
    string SourceId,
    string SourcePath,
    string SourceSha256,
    SourceFragmentKind Kind,
    string ContentSha256,
    string Content,
    SourceLocator Locator,
    IReadOnlyList<string> Dependencies,
    bool HasFootnoteMarkers,
    VerificationStatus VerificationStatus = VerificationStatus.Unverified);
