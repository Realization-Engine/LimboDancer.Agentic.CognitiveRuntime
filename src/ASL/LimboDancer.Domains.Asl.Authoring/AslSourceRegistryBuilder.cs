using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Authoring;

public sealed class SourceRegistryException : Exception
{
    public SourceRegistryException()
    {
    }

    public SourceRegistryException(string message)
        : base(message)
    {
    }

    public SourceRegistryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public static partial class AslSourceRegistryBuilder
{
    public const string SchemaVersion = "1.0.0";
    public const string RegistryId = "asl-easlrb-3.10-a-e";
    public const string Edition = "3.01";
    public const string SourceRoot = "docs/ASL/Rulebook_Markdown";
    public const string ConversionToolPath = "utils/pdf_to_markdown.py";

    private static readonly IReadOnlyDictionary<string, SourceDefinition> ExpectedMarkdown =
        new ReadOnlyDictionary<string, SourceDefinition>(
            new SortedDictionary<string, SourceDefinition>(StringComparer.Ordinal)
            {
                ["00 - Table of Contents.md"] = new("asl-easlrb-3.10:contents", null),
                ["01 - Index and Glossary.md"] = new("asl-easlrb-3.10:index-glossary", null),
                ["02 - Chapter A - Infantry and Basic Game Rules.md"] = new("asl-easlrb-3.10:chapter-a", "A"),
                ["03 - Chapter B - Terrain.md"] = new("asl-easlrb-3.10:chapter-b", "B"),
                ["04 - Chapter C - Ordnance and Offboard Artillery.md"] = new("asl-easlrb-3.10:chapter-c", "C"),
                ["05 - Chapter D - Vehicles.md"] = new("asl-easlrb-3.10:chapter-d", "D"),
                ["06 - Chapter E - Miscellaneous.md"] = new("asl-easlrb-3.10:chapter-e", "E"),
            });

    public static SourceRegistryManifest Build(string repositoryRoot, string sourceCommit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var root = Path.GetFullPath(repositoryRoot);
        var sourceRoot = Path.Combine(root, SourceRoot.Replace('/', Path.DirectorySeparatorChar));
        var conversionTool = Path.Combine(root, ConversionToolPath.Replace('/', Path.DirectorySeparatorChar));
        ValidateInputs(sourceRoot, conversionTool, sourceCommit);

        var artifacts = new List<SourceArtifact>();
        foreach (var item in ExpectedMarkdown)
        {
            var path = Path.Combine(sourceRoot, item.Key);
            artifacts.Add(CreateArtifact(root, path, item.Value.SourceId, SourceArtifactKind.Markdown, item.Value.Chapter));
        }

        var imagesRoot = Path.Combine(sourceRoot, "images");
        var imagePaths = Directory.Exists(imagesRoot)
            ? Directory.EnumerateFiles(imagesRoot, "*", SearchOption.AllDirectories)
                .OrderBy(static path => path, StringComparer.Ordinal)
                .ToArray()
            : [];
        if (imagePaths.Length == 0)
        {
            throw new SourceRegistryException("The ASL rulebook image set is empty.");
        }

        foreach (var path in imagePaths)
        {
            var relativeImage = ToRepositoryPath(Path.GetRelativePath(imagesRoot, path));
            var sourceId = $"asl-easlrb-3.10:image/{relativeImage}";
            artifacts.Add(CreateArtifact(root, path, sourceId, SourceArtifactKind.Image, null));
        }

        artifacts.Sort((left, right) => StringComparer.Ordinal.Compare(left.Path, right.Path));
        return new SourceRegistryManifest(
            SchemaVersion,
            RegistryId,
            Edition,
            sourceCommit,
            SourceRoot,
            new ConversionTool(ConversionToolPath, Hashing.Sha256File(conversionTool)),
            artifacts);
    }

    private static void ValidateInputs(string sourceRoot, string conversionTool, string sourceCommit)
    {
        if (string.IsNullOrWhiteSpace(sourceCommit))
        {
            throw new SourceRegistryException("A non-empty source commit is required.");
        }

        if (!Directory.Exists(sourceRoot))
        {
            throw new SourceRegistryException($"Source root does not exist: {sourceRoot}");
        }

        if (!File.Exists(conversionTool))
        {
            throw new SourceRegistryException($"Conversion tool does not exist: {conversionTool}");
        }

        var actualMarkdown = Directory.EnumerateFiles(sourceRoot, "*.md", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(static name => name is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
        var missing = ExpectedMarkdown.Keys.Except(actualMarkdown, StringComparer.Ordinal)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();
        var unexpected = actualMarkdown.Except(ExpectedMarkdown.Keys, StringComparer.Ordinal)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();
        if (missing.Length > 0 || unexpected.Length > 0)
        {
            throw new SourceRegistryException(
                $"Unexpected ASL Markdown source set; missing=[{string.Join(", ", missing)}], unexpected=[{string.Join(", ", unexpected)}].");
        }
    }

    private static SourceArtifact CreateArtifact(
        string repositoryRoot,
        string path,
        string sourceId,
        SourceArtifactKind kind,
        string? chapter)
    {
        var (startPage, endPage) = GetPageRange(path, kind);
        return new SourceArtifact(
            sourceId,
            ToRepositoryPath(Path.GetRelativePath(repositoryRoot, path)),
            kind,
            Hashing.Sha256File(path),
            new FileInfo(path).Length,
            chapter,
            startPage,
            endPage);
    }

    private static (int? StartPage, int? EndPage) GetPageRange(string path, SourceArtifactKind kind)
    {
        int[] pages;
        if (kind == SourceArtifactKind.Markdown)
        {
            var content = File.ReadAllText(path, Encoding.UTF8);
            pages = PageMarkerRegex().Matches(content)
                .Cast<Match>()
                .Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
                .ToArray();
        }
        else
        {
            var match = ImagePageRegex().Match(Path.GetFileName(path));
            pages = match.Success
                ? [int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)]
                : [];
        }

        return pages.Length == 0 ? (null, null) : (pages.Min(), pages.Max());
    }

    private static string ToRepositoryPath(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/');
    }

    [GeneratedRegex(@"<!--\s*page\s+(\d+)\s*-->", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PageMarkerRegex();

    [GeneratedRegex(@"-p(\d+)(?:-|\.)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ImagePageRegex();

    private sealed record SourceDefinition(string SourceId, string? Chapter);
}
