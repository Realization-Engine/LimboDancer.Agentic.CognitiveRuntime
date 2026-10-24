using LimboDancer.Domains.Asl.Authoring;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>
/// The rulebook's manifests at the pinned source commit, generated once for the whole test run. Fifty tests read them; generating them
/// again in each test, and so extracting and hashing the full TIR again for each, made this project the slowest in CI.
/// </summary>
internal static class SharedManifests
{
    public const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";

    private static readonly Lazy<GeneratedManifests> Manifests = new(() => AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit));

    public static GeneratedManifests AtSourceCommit => Manifests.Value;
}
