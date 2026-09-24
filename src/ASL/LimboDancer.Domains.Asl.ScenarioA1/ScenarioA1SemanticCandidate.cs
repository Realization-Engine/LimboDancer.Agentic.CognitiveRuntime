using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.ScenarioA1;

public sealed record ScenarioA1Predicate(string Key, string ExpectedValue);
public sealed record ScenarioA1SemanticCase(string Id, string ReviewStatus, string ExpectedDisposition,
    IReadOnlyList<string> SourceRules, IReadOnlyList<ScenarioA1Predicate> Predicates);
public sealed record ScenarioA1SemanticResult(string Disposition, IReadOnlyList<string> SourceRules);

/// <summary>
/// A digest-pinned candidate profile for exact reviewed cases. It is not a published domain package.
/// Deferred cases cannot return a definitive semantic result.
/// </summary>
public sealed class ScenarioA1SemanticCandidate
{
    private const string MatrixDigest = "0986e0657f9c5e39dbac991d1934664acc1044d18f143bc869d42db7868024ff";
    private const string ComparisonDigest = "c64fe3229d5a3541357fe6948ec037fcfbdbfde8810a599f21df81aed0a94bc5";
    private readonly IReadOnlyDictionary<string, ScenarioA1SemanticCase> _cases;

    public ScenarioA1SemanticCandidate()
    {
        var matrix = ReadResource("ScenarioA1.matrix.json", MatrixDigest);
        _ = ReadResource("ScenarioA1.occupied-comparison.json", ComparisonDigest);
        var manifest = ReadResource("ScenarioA1.candidate-manifest.json");
        RootSha256 = ValidateManifest(manifest, MatrixDigest, ComparisonDigest);
        using var document = JsonDocument.Parse(matrix);
        var root = document.RootElement;
        if (root.GetProperty("semanticProfile").GetString() != "bounded-exact-case-predicates-v1"
            || root.GetProperty("status").GetString() != "user-delegated-xunit-review-bounded")
        {
            throw new InvalidOperationException("The bounded semantic profile changed.");
        }

        var cases = root.GetProperty("cases").EnumerateArray().Select(value =>
        {
            var predicates = value.GetProperty("predicates").EnumerateArray()
                .Select(item => new ScenarioA1Predicate(item.GetProperty("key").GetString()!,
                    item.GetProperty("equals").GetString()!)).ToArray();
            var ruleIds = value.GetProperty("sourceRules").EnumerateArray()
                .Select(item => item.GetString()!).ToArray();
            var status = value.GetProperty("reviewStatus").GetString()!;
            var disposition = value.GetProperty("expectedDisposition").GetString()!;
            if (ruleIds.Length == 0 || ruleIds.Distinct(StringComparer.Ordinal).Count() != ruleIds.Length
                || (status == "reviewed-bounded" && predicates.Length == 0)
                || (status != "reviewed-bounded" && predicates.Length != 0)
                || predicates.Any(item => string.IsNullOrWhiteSpace(item.Key)
                    || string.IsNullOrWhiteSpace(item.ExpectedValue))
                || predicates.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count() != predicates.Length
                || (status == "reviewed-bounded" && disposition is not ("eligible-2mf" or "prohibited"))
                || (status == "deferred" && disposition != "abstained")
                || (status == "reviewed-nondefinitive" && disposition != "indeterminate"))
            {
                throw new InvalidOperationException("A case has invalid semantic review status or predicates.");
            }
            return new ScenarioA1SemanticCase(value.GetProperty("caseId").GetString()!,
                status, disposition, ruleIds, predicates);
        }).ToArray();
        if (cases.Length != 9 || cases.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != 9
            || cases.Count(item => item.ReviewStatus == "reviewed-bounded") != 3)
        {
            throw new InvalidOperationException("The candidate case inventory changed.");
        }
        _cases = cases.ToDictionary(item => item.Id, StringComparer.Ordinal);
    }

    public string RootSha256 { get; }
    public IReadOnlyCollection<ScenarioA1SemanticCase> Cases => _cases.Values.ToArray();

    public ScenarioA1SemanticResult Evaluate(string caseId, IReadOnlyDictionary<string, string> facts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentNullException.ThrowIfNull(facts);
        if (!_cases.TryGetValue(caseId, out var semanticCase))
        {
            return new ScenarioA1SemanticResult("abstained", []);
        }
        if (semanticCase.ReviewStatus == "deferred")
        {
            return new ScenarioA1SemanticResult("abstained", semanticCase.SourceRules);
        }
        if (semanticCase.ReviewStatus == "reviewed-nondefinitive")
        {
            return new ScenarioA1SemanticResult("indeterminate", semanticCase.SourceRules);
        }
        if (semanticCase.Predicates.Any(item => !facts.ContainsKey(item.Key)))
        {
            return new ScenarioA1SemanticResult("indeterminate", semanticCase.SourceRules);
        }
        if (facts.Count != semanticCase.Predicates.Count
            || semanticCase.Predicates.Any(item => facts[item.Key] != item.ExpectedValue))
        {
            return new ScenarioA1SemanticResult("abstained", semanticCase.SourceRules);
        }
        return new ScenarioA1SemanticResult(semanticCase.ExpectedDisposition, semanticCase.SourceRules);
    }

    public static string ValidateManifest(string content, string matrixSha256, string comparisonSha256)
    {
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;
        var fields = new[] { "schemaVersion", "candidateId", "sourceDecisionSha256",
            "caseMatrixSha256", "occupiedComparisonSha256" };
        var values = fields.Select(field => root.GetProperty(field).GetString()!).ToArray();
        if (root.GetProperty("status").GetString() != "candidate-semantic-profile-not-admitted"
            || values[0] != "1.0.0" || values[1] != "asl-scenario-a1-occupied-candidate"
            || values[2] != ScenarioA1Package.DecisionSha256
            || values[3] != matrixSha256 || values[4] != comparisonSha256
            || root.GetProperty("caseCount").GetInt32() != 9)
        {
            throw new InvalidOperationException("The candidate manifest inputs changed.");
        }
        var payload = string.Join('\n', values.Append("9"));
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        if (root.GetProperty("rootSha256").GetString() != digest)
        {
            throw new InvalidOperationException("The candidate manifest digest does not reproduce.");
        }
        return digest;
    }

    private static string ReadResource(string name, string? digest = null)
    {
        using var stream = typeof(ScenarioA1SemanticCandidate).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("A candidate artifact is missing: " + name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (digest is not null && Convert.ToHexStringLower(SHA256.HashData(bytes)) != digest)
        {
            throw new InvalidOperationException("A candidate artifact changed: " + name);
        }
        return Encoding.UTF8.GetString(bytes);
    }
}
