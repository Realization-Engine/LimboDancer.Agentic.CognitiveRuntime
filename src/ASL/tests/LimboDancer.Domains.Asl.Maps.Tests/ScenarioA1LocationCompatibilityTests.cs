using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Tests;

/// <summary>
/// ASL-MAP-022: every board location string the Scenario A1 providers, packages, and execution tests use
/// must parse to a canonical <see cref="BoardLocation"/> and format back to the identical string.
/// </summary>
public sealed partial class ScenarioA1LocationCompatibilityTests
{
    private static readonly string[] ScannedDirectories =
    [
        Path.Combine("src", "ASL", "LimboDancer.Domains.Asl.ScenarioA1"),
        Path.Combine("src", "ASL", "LimboDancer.Domains.Asl.Execution"),
        Path.Combine("src", "ASL", "tests", "LimboDancer.Domains.Asl.ScenarioA1.Tests"),
        Path.Combine("docs", "ASL", "SourceRegistry"),
    ];

    [Fact]
    public void EveryScenarioA1LocationStringRoundTrips()
    {
        var root = RepositoryRoot.Find();
        var locations = ScannedDirectories
            .SelectMany(directory => Directory.EnumerateFiles(Path.Combine(root, directory), "*.*", SearchOption.AllDirectories))
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".json", StringComparison.Ordinal))
            .Where(path => !IsBuildOutput(path))
            .SelectMany(path => LocationPattern().Matches(File.ReadAllText(path)).Select(match => (Path: path, Text: match.Value)))
            .ToArray();

        // The return path, its journal, and the board observation tests all use locations such as bd01:D4:0.
        Assert.Contains(locations, location => location.Text == "bd01:D4:0");
        Assert.Contains(locations, location => location.Text == "bd01:E4:0");

        foreach (var (path, text) in locations)
        {
            Assert.True(BoardLocation.TryParse(text, out var parsed), $"{text} in {path} does not parse.");
            Assert.Equal(text, parsed.ToString());
            Assert.Equal(BoardRefKind.Vasl, parsed.Board.Kind);
            Assert.True(
                BoardGeometry.StandardGeomorphic.TryGetIndex(parsed.Hex, out _),
                $"{text} in {path} names a hex that is not on a standard geomorphic board.");
        }
    }

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    [GeneratedRegex(@"(?<![A-Za-z0-9])bd[0-9A-Za-z]+:[A-Z]+[0-9]+:-?[0-9]+(/[0-5])?(?![0-9])")]
    private static partial Regex LocationPattern();
}
