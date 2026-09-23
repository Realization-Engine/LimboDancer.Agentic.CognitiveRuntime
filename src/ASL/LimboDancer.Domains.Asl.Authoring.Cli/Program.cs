using LimboDancer.Domains.Asl.Authoring;

namespace LimboDancer.Domains.Asl.Authoring.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = ParseArguments(args);
            var manifests = new AslAuthoringManifestGenerator().Generate(
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
            ["--repository-root", "--source-commit", "--registry-output", "--verification-output"],
            StringComparer.Ordinal);
        var unknown = values.Keys.Where(key => !supported.Contains(key)).ToArray();
        if (unknown.Length > 0)
        {
            throw new ArgumentException($"Unknown option: {string.Join(", ", unknown)}");
        }

        return new Options(
            values.GetValueOrDefault("--repository-root", Directory.GetCurrentDirectory()),
            Required(values, "--source-commit"),
            Required(values, "--registry-output"),
            Required(values, "--verification-output"));
    }

    private static string Required(IReadOnlyDictionary<string, string> values, string name)
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
            + "[--repository-root PATH] --source-commit SHA --registry-output PATH --verification-output PATH";
    }

    private sealed record Options(
        string RepositoryRoot,
        string SourceCommit,
        string RegistryOutput,
        string VerificationOutput);
}
