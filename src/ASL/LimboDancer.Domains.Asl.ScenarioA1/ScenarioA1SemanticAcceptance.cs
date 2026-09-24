using System.Text.Json;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>An affirmative, content-addressed review of one exact semantic profile.</summary>
public static class ScenarioA1SemanticAcceptance
{
    public const string Sha256 = "7c9157b26fb5d0c23ac1b39f7181127b4d9530af9db6f9a90b05b27e6b8c1b66";
    public const string DeclaredUse = "Scenario A1 occupied-building entry eligibility for exact synthetic facts only";

    public static void Validate(ScenarioA1SemanticCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        using var stream = typeof(ScenarioA1SemanticAcceptance).Assembly
            .GetManifestResourceStream("ScenarioA1.semantic-acceptance.json")
            ?? throw new InvalidOperationException("The semantic acceptance record is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes)) != Sha256)
            throw new InvalidOperationException("The semantic acceptance record changed.");
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        if (root.GetProperty("schemaVersion").GetString() != "1.0.0"
            || root.GetProperty("status").GetString() != "accepted-exact-case-semantic-profile"
            || root.GetProperty("declaredUse").GetString() != DeclaredUse
            || root.GetProperty("authority").GetString() != "user-directed-affirmative-xunit-review-2026-09-24"
            || root.GetProperty("candidateManifestRootSha256").GetString() != candidate.RootSha256
            || root.GetProperty("boundedAdmissionSha256").GetString() != ScenarioA1BoundedAdmission.Sha256
            || root.GetProperty("precedence").GetString() != "unique-exact-case-id-and-complete-predicate-match"
            || root.GetProperty("adjudication").GetString() !=
                "no-conflicting-exact-case-match; unresolved or missing evidence remains nondefinitive"
            || root.GetProperty("occupiedComparisonSha256").GetString() !=
                "c64fe3229d5a3541357fe6948ec037fcfbdbfde8810a599f21df81aed0a94bc5")
        {
            throw new InvalidOperationException("The semantic acceptance inputs or policy changed.");
        }
        var accepted = Ids(root, "acceptedCaseIds");
        var nonDefinitive = Ids(root, "nonDefinitiveCaseIds");
        var cases = candidate.Cases.ToDictionary(item => item.Id, StringComparer.Ordinal);
        if (accepted.Length != 7 || nonDefinitive.Length != 2
            || accepted.Concat(nonDefinitive).Distinct(StringComparer.Ordinal).Count() != 9
            || !cases.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(accepted.Concat(nonDefinitive))
            || root.GetProperty("exclusions").GetArrayLength() != 4)
            throw new InvalidOperationException("The declared use has incomplete case or exclusion coverage.");

        foreach (var id in accepted)
        {
            var semanticCase = cases[id];
            if (semanticCase.ReviewStatus != "reviewed-bounded" || semanticCase.Predicates.Count == 0)
                throw new InvalidOperationException("An accepted case has no reviewed predicate contract.");
            var facts = semanticCase.Predicates.ToDictionary(item => item.Key, item => item.ExpectedValue);
            if (candidate.Evaluate(id, facts).Disposition != semanticCase.ExpectedDisposition)
                throw new InvalidOperationException("The accepted case does not reproduce.");
            facts.Remove(semanticCase.Predicates[0].Key);
            if (candidate.Evaluate(id, facts).Disposition != "indeterminate")
                throw new InvalidOperationException("Missing evidence was accepted.");
            facts[semanticCase.Predicates[0].Key] = "contrary";
            if (candidate.Evaluate(id, facts).Disposition != "abstained")
                throw new InvalidOperationException("Contrary evidence was accepted.");
        }
        foreach (var id in nonDefinitive)
        {
            if (cases[id].ReviewStatus != "reviewed-nondefinitive"
                || candidate.Evaluate(id, new Dictionary<string, string>()).Disposition != "indeterminate")
                throw new InvalidOperationException("An unresolved case acquired a definitive result.");
        }
    }

    private static string[] Ids(JsonElement root, string name) => root.GetProperty(name)
        .EnumerateArray().Select(item => item.GetString()!).ToArray();
}
