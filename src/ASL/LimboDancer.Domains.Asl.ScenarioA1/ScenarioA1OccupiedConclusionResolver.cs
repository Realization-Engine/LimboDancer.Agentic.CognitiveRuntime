using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Read-only adjudication of exact, versioned synthetic case observations.</summary>
public sealed class ScenarioA1OccupiedConclusionResolver : IDomainConclusionResolver
{
    public const string QuestionKind = "scenario-a1-occupied-entry";
    private readonly ScenarioA1SemanticCandidate _candidate = new();
    private readonly ScenarioA1OccupiedPackage _package = new();

    public async ValueTask<DomainConclusion> ConcludeAsync(DomainConclusionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        var question = context.Question;
        if (question.Package != ScenarioA1OccupiedPackage.Identity)
            throw new ArgumentException("An exact occupied-case package is required.", nameof(context));
        var pinned = (await _package.ResolveAsync(question.Package, cancellationToken)).Package!;
        if (!context.Package.CanonicalSources.SequenceEqual(pinned.CanonicalSources))
            throw new ArgumentException("The occupied-case canonical source set changed.", nameof(context));

        var ambiguities = new List<string>();
        var outside = new List<string>();
        if (question.Kind.Value != QuestionKind)
            outside.Add("question.kind-outside-admitted-use");
        if (question.Parameters.EnumerateObject().Count() != 4)
            outside.Add("question.unreviewed-parameter");
        if (context.CalculationEvidence.Count != 0)
            outside.Add("calculation.outside-declared-case");
        var unitId = Parameter(question.Parameters, "unitId");
        var locationId = Parameter(question.Parameters, "locationId");
        var caseId = Parameter(question.Parameters, "caseId");
        var expectedVersion = Parameter(question.Parameters, "observationVersion");
        if (unitId is null || locationId is null || caseId is null || unitId == locationId)
            ambiguities.Add("question.subjects-or-case-missing");
        if (expectedVersion is null)
            ambiguities.Add("question.observation-version-missing");
        if (context.EntityResolutions.Count != 2 || unitId is null || locationId is null
            || new[] { unitId, locationId }.Any(id => context.EntityResolutions.Count(result =>
                result.Query.Reference == id && result.Outcome == DomainEntityResolutionOutcome.Resolved) != 1))
            ambiguities.Add("entity.resolution-incomplete-or-ambiguous");

        var observation = context.Observations.Count == 1 ? context.Observations[0] : null;
        if (observation is null || observation.Version is null
            || observation.ResourceId != locationId || observation.ObservedAt > question.AskedAt)
            ambiguities.Add("observation.exact-version-and-location-required");
        else if (observation.Version != expectedVersion)
            ambiguities.Add("observation.version-changed-during-adjudication");
        var facts = new Dictionary<string, string>(StringComparer.Ordinal);
        if (observation is not null)
        {
            if (observation.Data.ValueKind != JsonValueKind.Object)
                ambiguities.Add("observation.facts-object-required");
            else
                foreach (var property in observation.Data.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.String || property.Value.GetString() is null)
                        ambiguities.Add("fact.unknown:" + property.Name);
                    else if (!facts.TryAdd(property.Name, property.Value.GetString()!))
                        ambiguities.Add("fact.duplicate:" + property.Name);
                }
        }
        var semantic = caseId is null
            ? new ScenarioA1SemanticResult("indeterminate", [])
            : _candidate.Evaluate(caseId, facts);
        var disposition = ambiguities.Count != 0 || semantic.Disposition == "indeterminate"
            ? ConclusionDisposition.Indeterminate
            : outside.Count != 0 || semantic.Disposition == "abstained"
                ? ConclusionDisposition.Abstained
                : semantic.Disposition.StartsWith("qualified-", StringComparison.Ordinal)
                    ? ConclusionDisposition.Qualified : ConclusionDisposition.Definitive;
        if (disposition == ConclusionDisposition.Indeterminate && ambiguities.Count == 0)
            ambiguities.Add("fact.required-case-evidence-missing-or-unresolved");

        var evidence = new List<EvidenceReference>
        {
            new("asl-a1-occupied-acceptance", EvidenceKind.CanonicalSource, question.TenantId,
                question.Package, "asl-scenario-a1.semantic-acceptance.json",
                ScenarioA1SemanticAcceptance.Sha256, "user-directed-affirmative-xunit-review-2026-09-24"),
        };
        evidence.AddRange(context.Observations.Select(item => new EvidenceReference(
            "asl-a1-observation:" + item.ObservationId, EvidenceKind.Observation, question.TenantId,
            question.Package, item.ResourceId ?? item.ObservationId, item.Version,
            item.Provenance ?? item.Source.SourceId)));
        var references = disposition is ConclusionDisposition.Definitive or ConclusionDisposition.Qualified
            ? semantic.SourceRules.Select(id => pinned.CanonicalSources.Single(rule => rule.ElementId == id)).ToArray()
            : [];
        var exceptions = references.Where(item =>
            caseId == "A1-fortified-breached-entry" && item.ElementId == "B23.9221"
            || caseId == "A1-single-known-enemy-smc-overrun" && item.ElementId == "A4.15"
            || caseId == "A1-advance-phase-entry" && item.ElementId == "A4.14").ToArray();
        var value = disposition is ConclusionDisposition.Definitive or ConclusionDisposition.Qualified
            ? JsonSerializer.SerializeToElement(new
            {
                caseId,
                eligible = semantic.Disposition != "prohibited",
                attemptOnly = disposition == ConclusionDisposition.Qualified,
                entryMf = semantic.Disposition switch
                {
                    "eligible-2mf" => (int?)2,
                    "eligible-3mf-with-overstack-penalty" => 3,
                    "qualified-overrun-entry-attempt-4mf" => 4,
                    _ => null,
                },
            }) : (JsonElement?)null;
        var reasons = disposition switch
        {
            ConclusionDisposition.Indeterminate => ambiguities,
            ConclusionDisposition.Abstained => outside.Append("asl.a1.outside-exact-case").ToList(),
            _ => ["asl.a1." + semantic.Disposition],
        };
        return new DomainConclusion("asl-a1-occupied:" + question.QuestionId, question,
            disposition, value, evidence, references, exceptions,
            disposition is ConclusionDisposition.Definitive or ConclusionDisposition.Qualified
                ? ["The versioned observation declares the complete exact case facts."] : [],
            ambiguities, reasons,
            disposition is ConclusionDisposition.Definitive or ConclusionDisposition.Qualified
                ? "The admitted exact case contract yields a read-only entry eligibility conclusion."
                : "The observation cannot inherit an admitted exact case outcome.",
            "asl-scenario-a1.occupied-package.json#sha256:" + ScenarioA1OccupiedPackage.ManifestSha256,
            question.AskedAt);
    }

    private static string? Parameter(JsonElement parameters, string key) =>
        parameters.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;
}
