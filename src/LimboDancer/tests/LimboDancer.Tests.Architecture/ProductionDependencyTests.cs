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
            ["LimboDancer.AppHost"] = new HashSet<string>(["LimboDancer.Host"], StringComparer.Ordinal),
        };

    [Fact]
    public void SolutionProjectsDoNotReferenceLegacyProjects()
    {
        var violations = ReadSolutionProjectReferences()
            .Where(reference =>
                reference.ReferencedProject.StartsWith("LimboDancer.MCP.", StringComparison.Ordinal)
                || IsBelowLegacyRoot(reference.ResolvedPath))
            .Select(reference => $"{reference.SourceProject} -> {reference.ResolvedPath}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"New solution projects must not reference src/_Legacy or LimboDancer.MCP.*:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void RuntimeProjectsReferenceNoAslProject()
    {
        // ASL-UNIT-002: ASL depends on the runtime, never the reverse.
        var violations = ReadSolutionProjectReferences()
            .Where(reference => reference.ReferencedProject.StartsWith("LimboDancer.Domains.Asl.", StringComparison.Ordinal))
            .Select(reference => $"{reference.SourceProject} -> {reference.ResolvedPath}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Runtime projects must not reference ASL projects:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void SolutionProjectReferencesFollowTheApprovedDependencyGraph()
    {
        var violations = ReadSolutionProjectReferences()
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
    public void SolutionContainsExactlyTheApprovedTopLevelProjects()
    {
        var actualProjects = GetSolutionProjectFiles()
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(
            actualProjects.SetEquals(AllowedProjectReferences.Keys),
            $"Expected: {string.Join(", ", AllowedProjectReferences.Keys)}{Environment.NewLine}Actual: {string.Join(", ", actualProjects)}");
    }

    [Fact]
    public void RuntimeAuthorityProjectsDoNotDependOnAspire()
    {
        var protectedProjects = new[]
        {
            "LimboDancer.Abstractions",
            "LimboDancer.Runtime",
        };

        var violations = GetSolutionProjectFiles()
            .Where(project => protectedProjects.Contains(Path.GetFileNameWithoutExtension(project), StringComparer.Ordinal))
            .Where(project => File.ReadAllText(project).Contains("Aspire.", StringComparison.Ordinal))
            .Select(project => Path.GetRelativePath(FindSolutionRoot(), project))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Runtime authority projects must not depend on Aspire:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void EveryProjectTreatsWarningsAsErrors()
    {
        var solutionRoot = FindSolutionRoot();
        var sharedProperties = XDocument.Load(Path.Combine(solutionRoot, "Directory.Build.props"));
        var sharedSetting = sharedProperties
            .Descendants("TreatWarningsAsErrors")
            .Select(element => element.Value.Trim())
            .LastOrDefault();

        Assert.True(
            string.Equals("true", sharedSetting, StringComparison.OrdinalIgnoreCase),
            "Directory.Build.props must set TreatWarningsAsErrors to true.");

        var overrides = Directory.GetFiles(solutionRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(project => XDocument.Load(project)
                .Descendants("TreatWarningsAsErrors")
                .Any(element => !string.Equals(element.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase)))
            .Select(project => Path.GetRelativePath(solutionRoot, project))
            .ToArray();

        Assert.True(
            overrides.Length == 0,
            $"Projects must not disable TreatWarningsAsErrors:{Environment.NewLine}{string.Join(Environment.NewLine, overrides)}");
    }

    private static IEnumerable<ProjectReference> ReadSolutionProjectReferences()
    {
        foreach (var projectFile in GetSolutionProjectFiles())
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

    private static string[] GetSolutionProjectFiles()
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
        return normalizedPath.Contains("/src/_Legacy/", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ProjectReference(
        string SourceProject,
        string ReferencedProject,
        string ResolvedPath);
}
