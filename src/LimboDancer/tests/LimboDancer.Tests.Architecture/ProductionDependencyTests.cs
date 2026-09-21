using System.Xml.Linq;

namespace LimboDancer.Tests.Architecture;

public sealed class ProductionDependencyTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedProjectReferences =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["LimboDancer.Abstractions"] = new HashSet<string>(StringComparer.Ordinal),
            ["LimboDancer.Runtime"] = new HashSet<string>(["LimboDancer.Abstractions"], StringComparer.Ordinal),
            ["LimboDancer.Infrastructure"] = new HashSet<string>(["LimboDancer.Abstractions"], StringComparer.Ordinal),
            ["LimboDancer.Adapters.Mcp"] = new HashSet<string>(["LimboDancer.Abstractions", "LimboDancer.Runtime"], StringComparer.Ordinal),
            ["LimboDancer.Host"] = new HashSet<string>(
                [
                    "LimboDancer.Abstractions",
                    "LimboDancer.Runtime",
                    "LimboDancer.Infrastructure",
                    "LimboDancer.Adapters.Mcp",
                ],
                StringComparer.Ordinal),
        };

    [Fact]
    public void ProductionProjectsDoNotReferenceLegacyProjects()
    {
        var violations = ReadProductionProjectReferences()
            .Where(reference =>
                reference.ReferencedProject.StartsWith("LimboDancer.MCP.", StringComparison.Ordinal)
                || IsBelowLegacyRoot(reference.ResolvedPath))
            .Select(reference => $"{reference.SourceProject} -> {reference.ResolvedPath}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"New production projects must not reference src/Legacy or LimboDancer.MCP.*:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void ProductionProjectReferencesFollowTheApprovedDependencyGraph()
    {
        var violations = ReadProductionProjectReferences()
            .Where(reference =>
                !AllowedProjectReferences.TryGetValue(reference.SourceProject, out var allowed)
                || !allowed.Contains(reference.ReferencedProject))
            .Select(reference => $"{reference.SourceProject} -> {reference.ReferencedProject}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Production project reference is outside the approved dependency graph:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void SolutionContainsExactlyTheApprovedProductionProjects()
    {
        var actualProjects = GetProductionProjectFiles()
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(
            actualProjects.SetEquals(AllowedProjectReferences.Keys),
            $"Expected: {string.Join(", ", AllowedProjectReferences.Keys)}{Environment.NewLine}Actual: {string.Join(", ", actualProjects)}");
    }

    private static IEnumerable<ProjectReference> ReadProductionProjectReferences()
    {
        foreach (var projectFile in GetProductionProjectFiles())
        {
            var sourceProject = Path.GetFileNameWithoutExtension(projectFile);
            var projectDirectory = Path.GetDirectoryName(projectFile)!;
            var document = XDocument.Load(projectFile);

            foreach (var element in document.Descendants("ProjectReference"))
            {
                var include = element.Attribute("Include")?.Value;
                Assert.False(string.IsNullOrWhiteSpace(include), $"ProjectReference in {projectFile} has no Include attribute.");

                var normalizedInclude = include!
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar);
                var resolvedPath = Path.GetFullPath(normalizedInclude, projectDirectory);
                yield return new ProjectReference(
                    sourceProject,
                    Path.GetFileNameWithoutExtension(resolvedPath),
                    resolvedPath);
            }
        }
    }

    private static string[] GetProductionProjectFiles()
    {
        var solutionRoot = FindSolutionRoot();

        return Directory.GetDirectories(solutionRoot, "LimboDancer.*", SearchOption.TopDirectoryOnly)
            .SelectMany(directory => Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string FindSolutionRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LimboDancer.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the directory containing LimboDancer.sln.");
    }

    private static bool IsBelowLegacyRoot(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        return normalizedPath.Contains("/src/Legacy/", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ProjectReference(
        string SourceProject,
        string ReferencedProject,
        string ResolvedPath);
}
