using System.Globalization;
using LimboDancer.Domains.Asl.Authoring;

namespace LimboDancer.Domains.Asl.Authoring.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = ParseArguments(args);
            var manifests = AslAuthoringManifestGenerator.Generate(
                options.RepositoryRoot,
                options.SourceCommit);
            ManifestJson.WriteFile(
                options.RegistryOutput,
                ManifestJson.SerializeRegistry(manifests.Registry));
            ManifestJson.WriteFile(
                options.VerificationOutput,
                ManifestJson.SerializeVerificationSample(
                    manifests.Registry.RegistryId,
                    manifests.VerificationSample));
            if (options.ScenarioA1Output is not null)
            {
                ManifestJson.WriteFile(
                    options.ScenarioA1Output,
                    AslScenarioA1SourceInventory.Extract(manifests).Serialize());
            }
            if (options.TirOutput is not null && options.TirCreatedAt is not null)
            {
                var tir = TirStructuralExtractor.Extract(
                    manifests.Registry,
                    manifests.Fragments,
                    new TirExtractionOptions(
                        new TirPackageCandidate(
                            "asl",
                            "easlrb-3.10-a-e",
                            "0.0.0-candidate.1"),
                        options.TirCreatedAt.Value,
                        "operator-supplied-reproducible-build-metadata"));
                var sample = TirStructuralExtractor.SelectRepresentativeSample(tir);
                ManifestJson.WriteFile(options.TirOutput, TirCanonicalJson.Serialize(sample));
                var sectionCount = tir.Artifacts.Count(static artifact => artifact is TirSectionArtifact);
                var ruleCount = tir.Artifacts.Count(static artifact => artifact is TirRuleArtifact);
                var crossReferenceCount = tir.Artifacts.Count(static artifact => artifact is TirCrossReferenceArtifact);
                var exampleCount = tir.Artifacts.Count(static artifact => artifact is TirExampleArtifact);
                var tableCount = tir.Artifacts.Count(static artifact => artifact is TirTableArtifact);
                var missingParentCount = tir.Diagnostics.Count(
                    static diagnostic => diagnostic.Code == "TIR-MISSING-PARENT");
                Console.WriteLine(FormattableString.Invariant(
                    $"TIR extraction summary: sections={sectionCount}, rules={ruleCount}, crossReferences={crossReferenceCount}, examples={exampleCount}, tables={tableCount}, diagnostics={tir.Diagnostics.Count}, missingParents={missingParentCount}."));
            }

            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException
            or IOException
            or InvalidOperationException
            or SourceRegistryException)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
    }

    private static Options ParseArguments(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw UsageException();
            }

            if (!values.TryAdd(args[index], args[index + 1]))
            {
                throw new ArgumentException($"Duplicate option: {args[index]}");
            }
        }

        var supported = new HashSet<string>(
            [
                "--repository-root",
                "--source-commit",
                "--registry-output",
                "--verification-output",
                "--tir-output",
                "--tir-created-at",
                "--a1-output",
            ],
            StringComparer.Ordinal);
        var unknown = values.Keys.Where(key => !supported.Contains(key)).ToArray();
        if (unknown.Length > 0)
        {
            throw new ArgumentException($"Unknown option: {string.Join(", ", unknown)}");
        }

        var tirOutput = values.GetValueOrDefault("--tir-output");
        var tirCreatedAtValue = values.GetValueOrDefault("--tir-created-at");
        if ((tirOutput is null) != (tirCreatedAtValue is null))
        {
            throw new ArgumentException("--tir-output and --tir-created-at must be supplied together.");
        }

        DateTimeOffset? tirCreatedAt = tirCreatedAtValue is null
            ? null
            : DateTimeOffset.Parse(
                tirCreatedAtValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        return new Options(
            values.GetValueOrDefault("--repository-root", Directory.GetCurrentDirectory()),
            Required(values, "--source-commit"),
            Required(values, "--registry-output"),
            Required(values, "--verification-output"),
            tirOutput,
            tirCreatedAt,
            values.GetValueOrDefault("--a1-output"));
    }

    private static string Required(Dictionary<string, string> values, string name)
    {
        return values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required option: {name}\n{Usage()}");
    }

    private static ArgumentException UsageException()
    {
        return new ArgumentException(Usage());
    }

    private static string Usage()
    {
        return "Usage: dotnet run --project src/ASL/LimboDancer.Domains.Asl.Authoring.Cli -- "
            + "[--repository-root PATH] --source-commit SHA --registry-output PATH "
            + "--verification-output PATH [--tir-output PATH --tir-created-at UTC_TIMESTAMP] "
            + "[--a1-output PATH]";
    }

    private sealed record Options(
        string RepositoryRoot,
        string SourceCommit,
        string RegistryOutput,
        string VerificationOutput,
        string? TirOutput,
        DateTimeOffset? TirCreatedAt,
        string? ScenarioA1Output);
}
