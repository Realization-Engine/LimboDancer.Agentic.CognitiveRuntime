using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Read-only A12.15 return and attempted-entry MF location after a second reveal.</summary>
public sealed class ScenarioA1SecondDefenderConsequenceConclusionResolver : IDomainConclusionResolver
{
    public const string QuestionKind = "scenario-a1-second-defender-consequence";
    private readonly ScenarioA1SecondDefenderConsequencePackage package = new();
    private readonly IReadOnlyDictionary<string, string> common;
    private readonly IReadOnlyDictionary<string, ReviewedCase> cases;

    public ScenarioA1SecondDefenderConsequenceConclusionResolver()
    {
        using var matrix = ScenarioA1SecondDefenderConsequencePackage.Read(
            "ScenarioA1.second-defender-consequence-matrix.json",
            ScenarioA1SecondDefenderConsequencePackage.MatrixSha256);
        common = ReadFacts(matrix.RootElement.GetProperty("commonFacts"));
        cases = matrix.RootElement.GetProperty("cases").EnumerateArray()
            .Select(item => new ReviewedCase(item.GetProperty("caseId").GetString()!,
                item.GetProperty("expectedDisposition").GetString()!,
                ReadFacts(item.GetProperty("facts"))))
            .ToDictionary(item => item.Id, StringComparer.Ordinal);
        if (cases.Count != 7 || cases.Values.Count(item => item.Disposition ==
            "forced-back-attempted-entry-mf-in-previous-location") != 2)
            throw new InvalidOperationException("The consequence cases changed.");
    }

    public async ValueTask<DomainConclusion> ConcludeAsync(DomainConclusionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        var question = context.Question;
        if (question.Package != ScenarioA1SecondDefenderConsequencePackage.Identity)
            throw new ArgumentException("The exact consequence package is required.", nameof(context));
        var pinned = (await package.ResolveAsync(question.Package, cancellationToken)).Package!;
        if (!context.Package.CanonicalSources.SequenceEqual(pinned.CanonicalSources))
            throw new ArgumentException("The consequence source set changed.", nameof(context));

        var ambiguities = new List<string>();
        var outside = new List<string>();
        if (question.Kind.Value != QuestionKind || question.Parameters.ValueKind != JsonValueKind.Object
            || question.Parameters.EnumerateObject().Count() != 5 || context.CalculationEvidence.Count != 0)
            outside.Add("asl.a1.second-defender-consequence.question-outside-exact-contract");
        var unitId = Parameter(question.Parameters, "unitId");
        var locationId = Parameter(question.Parameters, "locationId");
        var previousId = Parameter(question.Parameters, "previousLocationId");
        var version = Parameter(question.Parameters, "observationVersion");
        var caseId = Parameter(question.Parameters, "caseId");
        if (unitId is null || locationId is null || previousId is null || version is null
            || caseId is null || new[] { unitId, locationId, previousId }.Distinct().Count() != 3)
            ambiguities.Add("question.subject-previous-location-case-or-version-missing");
        if (context.EntityResolutions.Count != 3 || unitId is null || locationId is null
            || previousId is null || new[] { unitId, locationId, previousId }.Any(id =>
                context.EntityResolutions.Count(item => item.Query.Reference == id
                    && item.Outcome == DomainEntityResolutionOutcome.Resolved) != 1))
            ambiguities.Add("entity.resolution-incomplete-or-ambiguous");
        var observation = context.Observations.Count == 1 ? context.Observations[0] : null;
        if (observation is null || observation.Version != version
            || observation.TenantId != question.TenantId
            || observation.DomainPackage != question.Package
            || observation.ResourceId != locationId || observation.ObservedAt > question.AskedAt)
            ambiguities.Add("observation.exact-package-version-and-location-required");
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
        if (!facts.Remove("previousLocationId", out var suppliedPrevious)
            || string.IsNullOrWhiteSpace(suppliedPrevious) || suppliedPrevious != previousId)
            ambiguities.Add("observation.previous-location-does-not-match-question");
        ReviewedCase? reviewed = null;
        if (caseId is null || !cases.TryGetValue(caseId, out reviewed))
            outside.Add("asl.a1.second-defender-consequence.unknown-case");
        else
        {
            var expected = common.Concat(reviewed.Facts).ToDictionary(item => item.Key,
                item => item.Value, StringComparer.Ordinal);
            if (facts.Keys.Except(expected.Keys, StringComparer.Ordinal).Any()
                || facts.Any(item => expected.TryGetValue(item.Key, out var value) && item.Value != value))
                outside.Add("asl.a1.second-defender-consequence.extra-or-contrary-fact");
            if (expected.Keys.Except(facts.Keys, StringComparer.Ordinal).Any())
                ambiguities.Add("asl.a1.second-defender-consequence.required-fact-missing");
        }
        var disposition = outside.Count != 0 ? ConclusionDisposition.Abstained
            : ambiguities.Count != 0 ? ConclusionDisposition.Indeterminate
            : reviewed!.Disposition switch
            {
                "forced-back-attempted-entry-mf-in-previous-location" => ConclusionDisposition.Definitive,
                "abstained" => ConclusionDisposition.Abstained,
                _ => ConclusionDisposition.Indeterminate,
            };
        if (disposition == ConclusionDisposition.Indeterminate && ambiguities.Count == 0)
            ambiguities.Add("asl.a1.second-defender-consequence.reviewed-unresolved-branch");
        if (disposition == ConclusionDisposition.Abstained && outside.Count == 0)
            outside.Add("asl.a1.second-defender-consequence.outside-capable-attacker-scope");

        var evidence = new List<EvidenceReference>
        {
            new("asl-a1-second-defender-consequence-package", EvidenceKind.CanonicalSource,
                question.TenantId, question.Package,
                "asl-scenario-a1.second-defender-consequence-package.json",
                ScenarioA1SecondDefenderConsequencePackage.ManifestSha256,
                "user-directed-affirmative-xunit-review"),
            new("asl-a1-second-defender-consequence-matrix", EvidenceKind.CanonicalSource,
                question.TenantId, question.Package,
                "asl-scenario-a1.second-defender-consequence-case-matrix.json",
                ScenarioA1SecondDefenderConsequencePackage.MatrixSha256,
                "user-directed-affirmative-xunit-review"),
        };
        if (observation is not null)
            evidence.Add(new EvidenceReference("asl-a1-second-defender-consequence:"
                + observation.ObservationId, EvidenceKind.Observation, question.TenantId,
                question.Package, observation.ResourceId ?? observation.ObservationId,
                observation.Version, observation.Provenance ?? observation.Source.SourceId));
        var value = disposition == ConclusionDisposition.Definitive
            ? JsonSerializer.SerializeToElement(new
            {
                caseId,
                returnToLocationId = suppliedPrevious,
                attemptedEntryMf = 2,
                mfExpenditureLocationId = suppliedPrevious,
                additionalOvrMfResolved = false,
                defensiveAttackResolved = false,
                responseOrCcResolved = false,
                executed = false,
            }) : (JsonElement?)null;
        return new DomainConclusion("asl-a1-second-defender-consequence:" + question.QuestionId,
            question, disposition, value, evidence,
            disposition == ConclusionDisposition.Definitive ? pinned.CanonicalSources.ToArray() : [],
            [], disposition == ConclusionDisposition.Definitive
                ? ["A12.15 places the return and attempted entry MF in the supplied previous Location."] : [],
            ambiguities, disposition == ConclusionDisposition.Abstained ? outside
                : disposition == ConclusionDisposition.Indeterminate ? ambiguities
                : ["asl.a1.second-defender-consequence." + reviewed!.Disposition],
            "Read-only consequence; no return or MF charge is executed, and no additional OVR MF or attack is resolved.",
            "asl-scenario-a1.second-defender-consequence-package.json#sha256:"
                + ScenarioA1SecondDefenderConsequencePackage.ManifestSha256, question.AskedAt);
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
