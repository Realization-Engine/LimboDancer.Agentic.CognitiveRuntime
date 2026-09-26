using System.Text.Json;
using System.Text.Json.Serialization;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// Read-only conclusions for a declared fire attack (unit step 17): Definitive with the attack's arithmetic and each
/// target unit's effect when the Fire package resolves it, Abstained outside the reviewed scope, and Indeterminate when
/// the review leaves it undecided or a roll is missing. The observation holds the declared attack.
/// </summary>
public sealed class ScenarioA1FireConclusionResolver : IDomainConclusionResolver
{
    public const string QuestionKind = "scenario-a1-fire";

    private static readonly JsonSerializerOptions Strict = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private static readonly JsonSerializerOptions Output = new(JsonSerializerDefaults.Web);

    private readonly ScenarioA1FirePackage package = new();

    public async ValueTask<DomainConclusion> ConcludeAsync(DomainConclusionContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        var question = context.Question;
        if (question.Package != ScenarioA1FirePackage.Identity)
        {
            throw new ArgumentException("The exact Fire package is required.", nameof(context));
        }

        var pinned = (await package.ResolveAsync(question.Package, cancellationToken)).Package!;
        if (!context.Package.CanonicalSources.SequenceEqual(pinned.CanonicalSources))
        {
            throw new ArgumentException("The Fire source set changed.", nameof(context));
        }

        var ambiguities = new List<string>();
        var outside = new List<string>();
        if (question.Kind.Value != QuestionKind || question.Parameters.ValueKind != JsonValueKind.Object
            || question.Parameters.EnumerateObject().Count() != 3 || context.CalculationEvidence.Count != 0)
        {
            outside.Add("asl.a1.fire.question-outside-exact-contract");
        }

        var firerLocationId = Parameter(question.Parameters, "firerLocationId");
        var targetLocationId = Parameter(question.Parameters, "targetLocationId");
        var version = Parameter(question.Parameters, "observationVersion");
        if (firerLocationId is null || targetLocationId is null || version is null || firerLocationId == targetLocationId)
        {
            ambiguities.Add("question.locations-or-version-missing");
        }

        if (context.EntityResolutions.Count != 2 || firerLocationId is null || targetLocationId is null
            || new[] { firerLocationId, targetLocationId }.Any(id => context.EntityResolutions.Count(item => item.Query.Reference == id
                && item.Outcome == DomainEntityResolutionOutcome.Resolved) != 1))
        {
            ambiguities.Add("entity.resolution-incomplete-or-ambiguous");
        }

        var observation = context.Observations.Count == 1 ? context.Observations[0] : null;
        if (observation is null || observation.Version != version || observation.TenantId != question.TenantId
            || observation.DomainPackage != question.Package || observation.ResourceId != targetLocationId || observation.ObservedAt > question.AskedAt)
        {
            ambiguities.Add("observation.exact-package-version-and-location-required");
        }

        FireAttack? attack = null;
        if (observation is not null)
        {
            try
            {
                attack = observation.Data.Deserialize<FireAttack>(Strict);
            }
            catch (JsonException)
            {
                outside.Add("asl.a1.fire.extra-or-malformed-fact");
            }

            if (attack is not null && (attack.FirerLocationId != firerLocationId || attack.TargetLocationId != targetLocationId))
            {
                ambiguities.Add("observation.locations-do-not-match-question");
            }
        }

        FireResolution? resolution = null;
        if (outside.Count == 0 && ambiguities.Count == 0 && attack is not null)
        {
            resolution = ScenarioA1FireCalculator.Resolve(attack, package.Reference);
            (resolution.Disposition == FireResolution.Abstained ? outside : ambiguities).AddRange(resolution.Reasons);
        }

        var disposition = outside.Count != 0 ? ConclusionDisposition.Abstained
            : ambiguities.Count != 0 || resolution is null ? ConclusionDisposition.Indeterminate
            : ConclusionDisposition.Definitive;
        var evidence = new List<EvidenceReference>
        {
            new("asl-a1-fire-package", EvidenceKind.CanonicalSource, question.TenantId, question.Package,
                "asl-scenario-a1.fire-package.json", ScenarioA1FirePackage.ManifestSha256, "user-directed-affirmative-xunit-review"),
            new("asl-a1-fire-matrix", EvidenceKind.CanonicalSource, question.TenantId, question.Package,
                "asl-scenario-a1.fire-case-matrix.json", ScenarioA1FirePackage.MatrixSha256, "user-directed-affirmative-xunit-review"),
        };
        if (observation is not null)
        {
            evidence.Add(new EvidenceReference("asl-a1-fire:" + observation.ObservationId, EvidenceKind.Observation, question.TenantId,
                question.Package, observation.ResourceId ?? observation.ObservationId, observation.Version,
                observation.Provenance ?? observation.Source.SourceId));
        }

        var definitive = disposition == ConclusionDisposition.Definitive;
        return new DomainConclusion("asl-a1-fire:" + question.QuestionId, question, disposition,
            definitive ? JsonSerializer.SerializeToElement(new { resolution = resolution!, executed = false }, Output) : null,
            evidence, definitive ? pinned.CanonicalSources.ToArray() : [], [],
            definitive ? ["The IFT result of the declared attack and its effect on each target unit, under the reviewed Fire rulings."] : [],
            ambiguities, disposition == ConclusionDisposition.Abstained ? outside
                : disposition == ConclusionDisposition.Indeterminate ? ambiguities
                : ["asl.a1.fire.resolved"],
            "Read-only conclusion; no roll is drawn, no counter is placed, and no unit is changed.",
            "asl-scenario-a1.fire-package.json#sha256:" + ScenarioA1FirePackage.ManifestSha256, question.AskedAt);
    }

    private static string? Parameter(JsonElement parameters, string name) =>
        parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;
}
