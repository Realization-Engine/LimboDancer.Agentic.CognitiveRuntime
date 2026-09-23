using System.Text;

namespace LimboDancer.Domains.Asl.Authoring;

public static class AslAuthoringManifestGenerator
{
    public const string VerificationPurpose =
        "Representative fragments awaiting comparison with the authoritative ASL 3.01 edition.";

    public const string VerificationSelectionPolicy =
        "First structural examples plus chapter coverage and selected chapter-local identifiers.";

    public static GeneratedManifests Generate(string repositoryRoot, string sourceCommit)
    {
        var root = Path.GetFullPath(repositoryRoot);
        var registry = AslSourceRegistryBuilder.Build(root, sourceCommit);
        var fragments = new List<SourceFragment>();

        foreach (var artifact in registry.Artifacts.Where(static artifact => artifact.Kind == SourceArtifactKind.Markdown))
        {
            var path = Path.Combine(root, artifact.Path.Replace('/', Path.DirectorySeparatorChar));
            var content = File.ReadAllText(path, Encoding.UTF8);
            fragments.AddRange(MarkdownFragmentLocator.Locate(
                artifact.SourceId,
                artifact.Path,
                artifact.Sha256,
                artifact.Chapter,
                content));
        }

        ValidateFragmentDependencies(fragments, registry.Artifacts.Select(static artifact => artifact.Path));
        return new GeneratedManifests(registry, fragments, SelectVerificationSample(fragments));
    }

    public static IReadOnlyList<SourceFragment> SelectVerificationSample(IReadOnlyList<SourceFragment> fragments)
    {
        var selected = new List<SourceFragment>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(SourceFragment? fragment)
        {
            if (fragment is not null && seen.Add(fragment.FragmentId))
            {
                selected.Add(fragment);
            }
        }

        foreach (var kind in Enum.GetValues<SourceFragmentKind>())
        {
            Add(fragments.FirstOrDefault(fragment => fragment.Kind == kind));
        }

        foreach (var chapter in "ABCDE")
        {
            Add(fragments.FirstOrDefault(fragment =>
                fragment.Kind == SourceFragmentKind.RuleText
                && fragment.Locator.NormalizedElementId is not null
                && fragment.Locator.NormalizedElementId.StartsWith(chapter)));
        }

        foreach (var identifier in new[] { "A1.1", "B1.13", "C1.2" })
        {
            Add(fragments.FirstOrDefault(fragment =>
                string.Equals(fragment.Locator.NormalizedElementId, identifier, StringComparison.Ordinal)));
        }

        Add(fragments.FirstOrDefault(static fragment => fragment.HasFootnoteMarkers));
        return selected;
    }

    public static void ValidateFragmentDependencies(
        IReadOnlyList<SourceFragment> fragments,
        IEnumerable<string> registeredPaths)
    {
        var registered = registeredPaths.ToHashSet(StringComparer.Ordinal);
        var unresolved = new List<string>();
        foreach (var fragment in fragments)
        {
            var sourceDirectory = GetPosixDirectoryName(fragment.SourcePath);
            foreach (var dependency in fragment.Dependencies)
            {
                var resolved = NormalizePosixPath($"{sourceDirectory}/{dependency}");
                if (!registered.Contains(resolved))
                {
                    unresolved.Add($"{fragment.FragmentId} -> {resolved}");
                }
            }
        }

        if (unresolved.Count > 0)
        {
            throw new InvalidOperationException($"Unregistered fragment dependencies: {string.Join(", ", unresolved)}");
        }
    }

    private static string GetPosixDirectoryName(string path)
    {
        var index = path.LastIndexOf('/');
        return index < 0 ? string.Empty : path[..index];
    }

    private static string NormalizePosixPath(string path)
    {
        var segments = new List<string>();
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    throw new InvalidOperationException($"Dependency escapes the repository root: {path}");
                }

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        return string.Join('/', segments);
    }
}

public sealed record GeneratedManifests(
    SourceRegistryManifest Registry,
    IReadOnlyList<SourceFragment> Fragments,
    IReadOnlyList<SourceFragment> VerificationSample);
