namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void RuntimeProjectsDoNotReferenceAslAuthoring()
    {
        var runtimeRoot = Path.Combine(RepositoryPaths.Root, "src", "LimboDancer");
        var violations = Directory.EnumerateFiles(runtimeRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("LimboDancer.Domains.Asl", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(RepositoryPaths.Root, path))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void AslAuthoringAndRuntimeRemainCSharpDespiteDraftEditionTooling()
    {
        var unexpectedPython = Directory.EnumerateFiles(
                RepositoryPaths.Root,
                "*.py",
                SearchOption.AllDirectories)
            .Where(path =>
            {
                var relative = Path.GetRelativePath(RepositoryPaths.Root, path)
                    .Replace(Path.DirectorySeparatorChar, '/');
                return relative != AslSourceRegistryBuilder.ConversionToolPath
                    && relative != "utils/asl_curated_edition/build_curated_edition.py";
            })
            .ToArray();
        Assert.Empty(unexpectedPython);

        var workflow = File.ReadAllText(Path.Combine(
            RepositoryPaths.Root,
            ".github",
            "workflows",
            "asl-authoring-ci.yml"));
        Assert.DoesNotContain("setup-python", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("python -m", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("build_curated_edition.py", workflow, StringComparison.Ordinal);

        var authoringProjects = Directory.EnumerateFiles(
            Path.Combine(RepositoryPaths.Root, "src", "ASL"), "*.csproj", SearchOption.AllDirectories);
        Assert.All(authoringProjects, project =>
            Assert.DoesNotContain("CuratedEdition", File.ReadAllText(project), StringComparison.Ordinal));
    }
}
