using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Read-only, exact-case conclusions after a supplied A12.15 SMC reveal.</summary>
public sealed class ScenarioA1ConcealedSmcOverrunConclusionResolver : IDomainConclusionResolver
{
    public const string QuestionKind = "scenario-a1-concealed-smc-overrun";
    private readonly ScenarioA1ConcealedSmcOverrunPackage package = new();
    private readonly IReadOnlyDictionary<string, string> common;
    private readonly IReadOnlyDictionary<string, ReviewedCase> cases;

    public ScenarioA1ConcealedSmcOverrunConclusionResolver()
    {
        using var matrix = ScenarioA1ConcealedSmcOverrunPackage.Read(
            "ScenarioA1.concealed-smc-overrun-matrix.json",
            ScenarioA1ConcealedSmcOverrunPackage.MatrixSha256);
        common = Facts(matrix.RootElement.GetProperty("commonFacts"));
        cases = matrix.RootElement.GetProperty("cases").EnumerateArray()
            .Select(item => new ReviewedCase(item.GetProperty("caseId").GetString()!,
                item.GetProperty("expectedDisposition").GetString()!,
                item.GetProperty("sourceRules").EnumerateArray().Select(rule => rule.GetString()!).ToArray(),
                Facts(item.GetProperty("facts"))))
            .ToDictionary(item => item.Id, StringComparer.Ordinal);
        if (cases.Count != 10 || cases.Values.Count(item => item.Disposition == "qualified") != 1)
            throw new InvalidOperationException("The reviewed case inventory changed.");
    }

    public async ValueTask<DomainConclusion> ConcludeAsync(DomainConclusionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        var question = context.Question;
        if (question.Package != ScenarioA1ConcealedSmcOverrunPackage.Identity)
            throw new ArgumentException("The exact concealed-SMC OVR package is required.", nameof(context));
        var pinned = (await package.ResolveAsync(question.Package, cancellationToken)).Package!;
        if (!context.Package.CanonicalSources.SequenceEqual(pinned.CanonicalSources))
            throw new ArgumentException("The concealed-SMC OVR source set changed.", nameof(context));

        var ambiguities = new List<string>();
        var outside = new List<string>();
        if (question.Kind.Value != QuestionKind || question.Parameters.ValueKind != JsonValueKind.Object
            || question.Parameters.EnumerateObject().Count() != 4 || context.CalculationEvidence.Count != 0)
            outside.Add("asl.a1.ovr.question-outside-exact-contract");
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
        if (caseId is null || !cases.TryGetValue(caseId, out var reviewed))
            outside.Add("asl.a1.ovr.unknown-case");
        else
        {
            var expected = common.Concat(reviewed.Facts).ToDictionary(item => item.Key,
                item => item.Value, StringComparer.Ordinal);
            if (facts.Keys.Except(expected.Keys, StringComparer.Ordinal).Any()
                || facts.Any(item => expected.TryGetValue(item.Key, out var value) && item.Value != value))
                outside.Add("asl.a1.ovr.extra-or-contrary-fact");
            if (expected.Keys.Except(facts.Keys, StringComparer.Ordinal).Any())
                ambiguities.Add("asl.a1.ovr.required-fact-missing");
        }
        var disposition = outside.Count != 0 ? ConclusionDisposition.Abstained
            : ambiguities.Count != 0 ? ConclusionDisposition.Indeterminate
            : reviewed!.Disposition switch
            {
                "qualified" => ConclusionDisposition.Qualified,
                "abstained" or "delegated" => ConclusionDisposition.Abstained,
                _ => ConclusionDisposition.Indeterminate,
            };
        if (disposition == ConclusionDisposition.Indeterminate && ambiguities.Count == 0)
            ambiguities.Add("asl.a1.ovr.reviewed-unresolved-branch");
        if (disposition == ConclusionDisposition.Abstained && outside.Count == 0)
            outside.Add(reviewed!.Disposition == "delegated"
                ? "asl.a1.ovr.declined-use-exact-post-reveal-package"
                : "asl.a1.ovr.outside-qualified-continuation");

        var evidence = new List<EvidenceReference>
        {
            new("asl-a1-concealed-smc-package", EvidenceKind.CanonicalSource,
                question.TenantId, question.Package,
                "asl-scenario-a1.concealed-smc-overrun-package.json",
                ScenarioA1ConcealedSmcOverrunPackage.ManifestSha256,
                "user-directed-affirmative-xunit-review"),
            new("asl-a1-concealed-smc-matrix", EvidenceKind.CanonicalSource,
                question.TenantId, question.Package,
                "asl-scenario-a1.concealed-smc-overrun-case-matrix.json",
                ScenarioA1ConcealedSmcOverrunPackage.MatrixSha256,
                "user-directed-affirmative-xunit-review"),
        };
        if (observation is not null)
            evidence.Add(new EvidenceReference("asl-a1-ovr:" + observation.ObservationId,
                EvidenceKind.Observation, question.TenantId, question.Package,
                observation.ResourceId ?? observation.ObservationId, observation.Version,
                observation.Provenance ?? observation.Source.SourceId));
        CanonicalReference[] applicable = disposition == ConclusionDisposition.Qualified
            ? reviewed!.SourceRules.Select(rule => pinned.CanonicalSources.Single(item =>
                item.ElementId == rule)).ToArray() : [];
        var value = disposition == ConclusionDisposition.Qualified
            ? JsonSerializer.SerializeToElement(new
            {
                caseId,
                revealedOccupant = "enemySmc",
                revealProvenance = "A12.15-immediate-defender-reveal",
                overrunElected = true,
                ntc = "passed",
                entryMfRequired = 4,
                additionalDefender = "none-verified-by-supplied-state",
                nextResolution = "defenderResponseOrImmediateCloseCombat",
                attemptOnly = true,
            }) : (JsonElement?)null;
        return new DomainConclusion("asl-a1-concealed-smc-ovr:" + question.QuestionId, question,
            disposition, value, evidence, applicable,
            disposition == ConclusionDisposition.Qualified
                ? pinned.CanonicalSources.Where(item => item.ElementId == "A4.15").ToArray() : [],
            disposition == ConclusionDisposition.Qualified
                ? ["Exact supplied A12.15 reveal, sole occupancy, passed NTC, and at least 4 MF."] : [],
            ambiguities, disposition == ConclusionDisposition.Abstained ? outside
                : disposition == ConclusionDisposition.Indeterminate ? ambiguities
                : ["asl.a1.ovr.qualified-attempt-response-unresolved"],
            "A read-only attempt conclusion; MF, movement, defender response, fire and CC remain unexecuted.",
            "asl-scenario-a1.concealed-smc-overrun-package.json#sha256:"
                + ScenarioA1ConcealedSmcOverrunPackage.ManifestSha256, question.AskedAt);
    }

    private static string? Parameter(JsonElement parameters, string name) =>
        parameters.ValueKind == JsonValueKind.Object
        && parameters.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;

    private static Dictionary<string, string> Facts(JsonElement element) =>
        element.EnumerateObject().ToDictionary(item => item.Name,
            item => item.Value.GetString()!, StringComparer.Ordinal);

    private sealed record ReviewedCase(string Id, string Disposition, string[] SourceRules,
        IReadOnlyDictionary<string, string> Facts);
}
