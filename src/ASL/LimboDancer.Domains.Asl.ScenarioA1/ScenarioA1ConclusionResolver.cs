using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Adjudicates only the reviewed synthetic first case; it never mutates state.</summary>
public sealed class ScenarioA1ConclusionResolver : IDomainConclusionResolver
{
    public const string QuestionKind = "scenario-a1-building-entry";
    private static readonly string[] FactNames =
    [
        "isKnownGoodOrderInfantrySquad", "isAttackerMovementPhase", "canMoveThisPhase",
        "isAdjacentGroundLevelOrdinaryBuilding", "isDestinationKnownEmpty",
        "hasNoRoadBypassElevationOrAdditionalTerrain", "hasEnoughMovementFactors",
        "isBelowStackingLimit", "hasNoSpecialRuleOrOtherModifier",
    ];

    public ValueTask<DomainConclusion> ConcludeAsync(DomainConclusionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        var question = context.Question;
        if (question.Package != ScenarioA1Package.Identity)
            throw new ArgumentException("The Scenario A1 resolver requires the exact reviewed package.", nameof(context));
        var pinned = new ScenarioA1Package().ResolveAsync(ScenarioA1Package.Identity,
            cancellationToken).Result.Package!;
        if (!context.Package.CanonicalSources.SequenceEqual(pinned.CanonicalSources))
            throw new ArgumentException("The resolved source set differs from the reviewed case.", nameof(context));

        var evidence = new List<EvidenceReference>
        {
            new("asl-a1-review", EvidenceKind.CanonicalSource, question.TenantId, question.Package,
                "asl-scenario-a1.first-case-review-decision.json", ScenarioA1Package.DecisionSha256,
                "user-directed-xunit-review-2026-09-24"),
        };
        evidence.AddRange(context.Observations.Select(observation => new EvidenceReference(
            "asl-a1-observation:" + observation.ObservationId, EvidenceKind.Observation,
            question.TenantId, question.Package, observation.ResourceId ?? observation.ObservationId,
            observation.Version, observation.Provenance ?? observation.Source.SourceId)));

        var ambiguities = new List<string>();
        var outside = new List<string>();
        if (question.Kind.Value != QuestionKind || question.Kind.DomainId != question.Package.DomainId)
            outside.Add("question.kind-outside-reviewed-case");
        var parameters = question.Parameters;
        var unitId = StringParameter(parameters, "unitId");
        var locationId = StringParameter(parameters, "locationId");
        if (unitId is null || locationId is null || unitId == locationId)
            ambiguities.Add("question.subjects-missing");
        foreach (var id in new[] { unitId, locationId }.Where(id => id is not null).Distinct(StringComparer.Ordinal))
        {
            var matches = context.EntityResolutions.Where(result => result.Query.Reference == id).ToArray();
            if (matches.Length != 1 || matches[0].Outcome != DomainEntityResolutionOutcome.Resolved)
                ambiguities.Add("entity.unresolved-or-ambiguous:" + id);
        }
        if (context.EntityResolutions.Count != 2)
            ambiguities.Add("entity.resolution-set-incomplete");

        if (context.Observations.Count != 1)
            ambiguities.Add("observation.exactly-one-required");
        else
        {
            var observation = context.Observations[0];
            if (observation.Version is null || observation.ResourceId != locationId
                || observation.ObservedAt > question.AskedAt)
                ambiguities.Add("observation.version-or-location-unusable");
            foreach (var name in FactNames)
            {
                if (observation.Data.ValueKind != JsonValueKind.Object
                    || !observation.Data.TryGetProperty(name, out var value)
                    || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    ambiguities.Add("fact.unknown:" + name);
                else if (value.ValueKind == JsonValueKind.False)
                    outside.Add("fact.outside-reviewed-case:" + name);
            }
        }
        var disposition = ambiguities.Count > 0 ? ConclusionDisposition.Indeterminate
            : outside.Count > 0 ? ConclusionDisposition.Abstained : ConclusionDisposition.Definitive;
        var reasons = disposition switch
        {
            ConclusionDisposition.Indeterminate => ambiguities,
            ConclusionDisposition.Abstained => outside,
            _ => ["asl.a1.declared-entry-eligible-2mf"],
        };
        var conclusion = new DomainConclusion(
            "asl-a1:" + question.QuestionId, question, disposition,
            disposition == ConclusionDisposition.Definitive
                ? JsonSerializer.SerializeToElement(new { eligible = true, entryMf = 2 }) : null,
            evidence,
            disposition == ConclusionDisposition.Definitive ? context.Package.CanonicalSources : [],
            [],
            disposition == ConclusionDisposition.Definitive
                ? ["Nine supplied facts are declared and true for the reviewed first case."] : [],
            ambiguities, reasons,
            disposition == ConclusionDisposition.Definitive
                ? "The declared first case permits entry into an empty ordinary ground-level building for 2 MF."
                : "The supplied case cannot inherit the bounded entry ruling.",
            "asl-scenario-a1.first-case-review-decision.json#sha256:" + ScenarioA1Package.DecisionSha256,
            question.AskedAt);
        return ValueTask.FromResult(conclusion);
    }

    private static string? StringParameter(JsonElement data, string name) =>
        data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;
}
