using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class SourceRegistryTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";

    [Fact]
    public void RegisteredRepositorySourceSetIsCompleteAndReproducible()
    {
        var first = AslSourceRegistryBuilder.Build(RepositoryPaths.Root, SourceCommit);
        var second = AslSourceRegistryBuilder.Build(RepositoryPaths.Root, SourceCommit);

        Assert.Equal(ManifestJson.SerializeRegistry(first), ManifestJson.SerializeRegistry(second));
        Assert.Equal(7, first.Artifacts.Count(static artifact => artifact.Kind == SourceArtifactKind.Markdown));
        Assert.Equal(661, first.Artifacts.Count(static artifact => artifact.Kind == SourceArtifactKind.Image));
        Assert.All(first.Artifacts, static artifact => Assert.Equal(64, artifact.Sha256.Length));
        Assert.Equal(
            first.Artifacts.Select(static artifact => artifact.Path)
                .OrderBy(static path => path, StringComparer.Ordinal),
            first.Artifacts.Select(static artifact => artifact.Path));
        var chapterA = Assert.Single(first.Artifacts, static artifact => artifact.Chapter == "A");
        Assert.Equal(43, chapterA.StartPage);
        Assert.Equal(111, chapterA.EndPage);
    }

    [Fact]
    public void CommittedRegistryMatchesCurrentSourceBytes()
    {
        var expected = ManifestJson.SerializeRegistry(
            AslSourceRegistryBuilder.Build(RepositoryPaths.Root, SourceCommit));
        var manifestPath = Path.Combine(
            RepositoryPaths.Root,
            "docs",
            "ASL",
            "SourceRegistry",
            "asl-3.10-a-e.source-registry.json");

        Assert.Equal(expected, File.ReadAllText(manifestPath));
    }

    [Fact]
    public void MissingRegisteredMarkdownFailsClosed()
    {
        using var temporary = new TemporaryDirectory();
        var source = Path.Combine(
            temporary.Path,
            AslSourceRegistryBuilder.SourceRoot.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.Combine(source, "images"));
        var converter = Path.Combine(
            temporary.Path,
            AslSourceRegistryBuilder.ConversionToolPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(converter)!);
        File.WriteAllText(converter, "tool");

        var exception = Assert.Throws<SourceRegistryException>(
            () => AslSourceRegistryBuilder.Build(temporary.Path, SourceCommit));

        Assert.Contains("missing=[", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VerificationSampleIsMetadataOnlyAndUnverified()
    {
        var samplePath = Path.Combine(
            RepositoryPaths.Root,
            "docs",
            "ASL",
            "SourceRegistry",
            "asl-3.10-a-e.verification-sample.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(samplePath));
        var root = document.RootElement;
        var fragments = root.GetProperty("fragments").EnumerateArray().ToArray();

        Assert.Equal(14, fragments.Length);
        Assert.True(root.GetProperty("requiresAuthoritativeEditionComparison").GetBoolean());
        Assert.All(
            fragments,
            static fragment => Assert.Equal("unverified", fragment.GetProperty("verificationStatus").GetString()));
        Assert.All(fragments, static fragment => Assert.False(fragment.TryGetProperty("content", out _)));
    }

    [Fact]
    public void CommittedVerificationSampleMatchesCurrentSourceBytes()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var expected = ManifestJson.SerializeVerificationSample(
            manifests.Registry.RegistryId,
            manifests.VerificationSample);
        var samplePath = Path.Combine(
            RepositoryPaths.Root,
            "docs",
            "ASL",
            "SourceRegistry",
            "asl-3.10-a-e.verification-sample.json");

        Assert.Equal(expected, File.ReadAllText(samplePath));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"asl-authoring-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
