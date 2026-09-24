using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Read-only single-SMC OVR eligibility after a supplied second defender event.</summary>
public sealed class ScenarioA1SecondDefenderConclusionResolver : IDomainConclusionResolver
{
    public const string QuestionKind = "scenario-a1-second-defender-ovr-eligibility";
    private readonly ScenarioA1SecondDefenderPackage package = new();
    private readonly IReadOnlyDictionary<string, string> common;
    private readonly IReadOnlyDictionary<string, ReviewedCase> cases;

    public ScenarioA1SecondDefenderConclusionResolver()
    {
        using var matrix = ScenarioA1SecondDefenderPackage.Read("ScenarioA1.second-defender-matrix.json",
            ScenarioA1SecondDefenderPackage.MatrixSha256);
        common = ReadFacts(matrix.RootElement.GetProperty("commonFacts"));
        cases = matrix.RootElement.GetProperty("cases").EnumerateArray()
            .Select(item => new ReviewedCase(item.GetProperty("caseId").GetString()!,
                item.GetProperty("expectedDisposition").GetString()!,
                ReadFacts(item.GetProperty("facts"))))
            .ToDictionary(item => item.Id, StringComparer.Ordinal);
        if (cases.Count != 7 || cases.Values.Count(item => item.Disposition.StartsWith(
            "single-smc-ovr-", StringComparison.Ordinal)) != 2)
            throw new InvalidOperationException("The second-defender cases changed.");
    }

    public async ValueTask<DomainConclusion> ConcludeAsync(DomainConclusionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        var question = context.Question;
        if (question.Package != ScenarioA1SecondDefenderPackage.Identity)
            throw new ArgumentException("The exact second-defender package is required.", nameof(context));
        var pinned = (await package.ResolveAsync(question.Package, cancellationToken)).Package!;
        if (!context.Package.CanonicalSources.SequenceEqual(pinned.CanonicalSources))
            throw new ArgumentException("The second-defender source set changed.", nameof(context));

        var ambiguities = new List<string>();
        var outside = new List<string>();
        if (question.Kind.Value != QuestionKind || question.Parameters.ValueKind != JsonValueKind.Object
            || question.Parameters.EnumerateObject().Count() != 4 || context.CalculationEvidence.Count != 0)
            outside.Add("asl.a1.second-defender.question-outside-exact-contract");
        var unitId = Parameter(question.Parameters, "unitId");
        var locationId = Parameter(question.Parameters, "locationId");
        var version = Parameter(question.Parameters, "observationVersion");
        var caseId = Parameter(question.Parameters, "caseId");
        if (unitId is null || locationId is null || version is null || caseId is null
            || unitId == locationId)
            ambiguities.Add("question.subject-case-or-version-missing");
        if (context.EntityResolutions.Count != 2 || unitId is null || locationId is null
            || new[] { unitId, locationId }.Any(id => context.EntityResolutions.Count(item =>
                item.Query.Reference == id && item.Outcome == DomainEntityResolutionOutcome.Resolved) != 1))
            ambiguities.Add("entity.resolution-incomplete-or-ambiguous");
        var observation = context.Observations.Count == 1 ? context.Observations[0] : null;
        if (observation is null || observation.Version != version
            || observation.ResourceId != locationId || observation.ObservedAt > question.AskedAt)
            ambiguities.Add("observation.exact-version-and-location-required");
        var facts = new Dictionary<string, string>(StringComparer.Ordinal);
        if (observation is not null)
        {
            if (observation.Data.ValueKind != JsonValueKind.Object)
                ambiguities.Add("observation.facts-object-required");
            else
                foreach (var item in observation.Data.EnumerateObject())
                {
                    if (item.Value.ValueKind != JsonValueKind.String
                        || !facts.TryAdd(item.Name, item.Value.GetString()!))
                        ambiguities.Add("observation.duplicate-or-nonstring-fact");
                }
        }
        ReviewedCase? reviewed = null;
        if (caseId is null || !cases.TryGetValue(caseId, out reviewed))
            outside.Add("asl.a1.second-defender.unknown-case");
        else
        {
            var expected = common.Concat(reviewed.Facts).ToDictionary(item => item.Key,
                item => item.Value, StringComparer.Ordinal);
            if (facts.Keys.Except(expected.Keys, StringComparer.Ordinal).Any()
                || facts.Any(item => expected.TryGetValue(item.Key, out var value) && item.Value != value))
                outside.Add("asl.a1.second-defender.extra-or-contrary-fact");
            if (expected.Keys.Except(facts.Keys, StringComparer.Ordinal).Any())
                ambiguities.Add("asl.a1.second-defender.required-fact-missing");
        }
        var disposition = outside.Count != 0 ? ConclusionDisposition.Abstained
            : ambiguities.Count != 0 ? ConclusionDisposition.Indeterminate
            : reviewed!.Disposition switch
            {
                "single-smc-ovr-denied-by-two-revealed-smc" or
                    "single-smc-ovr-inapplicable-revealed-mmc" => ConclusionDisposition.Definitive,
                "abstained" => ConclusionDisposition.Abstained,
                _ => ConclusionDisposition.Indeterminate,
            };
        if (disposition == ConclusionDisposition.Indeterminate && ambiguities.Count == 0)
            ambiguities.Add("asl.a1.second-defender.reviewed-unresolved-branch");
        if (disposition == ConclusionDisposition.Abstained && outside.Count == 0)
            outside.Add("asl.a1.second-defender.outside-capable-attacker-scope");

        var evidence = new List<EvidenceReference>
        {
            new("asl-a1-second-defender-package", EvidenceKind.CanonicalSource,
                question.TenantId, question.Package,
                "asl-scenario-a1.second-defender-reveal-package.json",
                ScenarioA1SecondDefenderPackage.ManifestSha256,
                "user-directed-affirmative-xunit-review"),
            new("asl-a1-second-defender-matrix", EvidenceKind.CanonicalSource,
                question.TenantId, question.Package,
                "asl-scenario-a1.second-defender-reveal-case-matrix.json",
                ScenarioA1SecondDefenderPackage.MatrixSha256,
                "user-directed-affirmative-xunit-review"),
        };
        if (observation is not null)
            evidence.Add(new EvidenceReference("asl-a1-second-defender:" + observation.ObservationId,
                EvidenceKind.Observation, question.TenantId, question.Package,
                observation.ResourceId ?? observation.ObservationId, observation.Version,
                observation.Provenance ?? observation.Source.SourceId));
        var applicable = disposition == ConclusionDisposition.Definitive
            ? pinned.CanonicalSources.ToArray() : [];
        var value = disposition == ConclusionDisposition.Definitive
            ? JsonSerializer.SerializeToElement(new
            {
                caseId,
                singleSmcOverrunEligible = false,
                reason = reviewed!.Disposition,
                forcedBackResolved = false,
                mfSpendResolved = false,
                responseOrCcResolved = false,
            }) : (JsonElement?)null;
        return new DomainConclusion("asl-a1-second-defender:" + question.QuestionId, question,
            disposition, value, evidence, applicable, [],
            disposition == ConclusionDisposition.Definitive
                ? ["The versioned observation matches the exact second-reveal eligibility case."] : [],
            ambiguities, disposition == ConclusionDisposition.Abstained ? outside
                : disposition == ConclusionDisposition.Indeterminate ? ambiguities
                : ["asl.a1.second-defender." + reviewed!.Disposition],
            "Eligibility only; forced back, MF expenditure, defensive fire and CC are not resolved or executed.",
            "asl-scenario-a1.second-defender-reveal-package.json#sha256:"
                + ScenarioA1SecondDefenderPackage.ManifestSha256, question.AskedAt);
    }

    private static string? Parameter(JsonElement parameters, string name) =>
        parameters.ValueKind == JsonValueKind.Object
        && parameters.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;

    private static Dictionary<string, string> ReadFacts(JsonElement element) =>
        element.EnumerateObject().ToDictionary(item => item.Name,
            item => item.Value.GetString()!, StringComparer.Ordinal);

    private sealed record ReviewedCase(string Id, string Disposition,
        IReadOnlyDictionary<string, string> Facts);
}
