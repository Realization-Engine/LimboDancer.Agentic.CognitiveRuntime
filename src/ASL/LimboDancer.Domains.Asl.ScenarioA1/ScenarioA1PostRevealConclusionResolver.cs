using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Read-only A12.15 consequences after a supplied defender resolution.</summary>
public sealed class ScenarioA1PostRevealConclusionResolver : IDomainConclusionResolver
{
    public const string QuestionKind = "scenario-a1-post-reveal-entry";
    private readonly ScenarioA1PostRevealPackage package = new();

    public async ValueTask<DomainConclusion> ConcludeAsync(DomainConclusionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        var question = context.Question;
        if (question.Package != ScenarioA1PostRevealPackage.Identity)
            throw new ArgumentException("The exact post-reveal package is required.", nameof(context));
        var pinned = (await package.ResolveAsync(question.Package, cancellationToken)).Package!;
        if (!context.Package.CanonicalSources.SequenceEqual(pinned.CanonicalSources))
            throw new ArgumentException("The post-reveal sources changed.", nameof(context));

        var ambiguities = new List<string>();
        var outside = new List<string>();
        if (question.Kind.Value != QuestionKind
            || question.Parameters.ValueKind != JsonValueKind.Object
            || question.Parameters.EnumerateObject().Count() != 3
            || context.CalculationEvidence.Count != 0)
            outside.Add("asl.a1.reveal.outside-declared-use");
        var unitId = Parameter(question.Parameters, "unitId");
        var locationId = Parameter(question.Parameters, "locationId");
        var version = Parameter(question.Parameters, "observationVersion");
        if (unitId is null || locationId is null || version is null || unitId == locationId)
            ambiguities.Add("question.subject-or-version-missing");
        if (context.EntityResolutions.Count != 2 || unitId is null || locationId is null
            || new[] { unitId, locationId }.Any(id => context.EntityResolutions.Count(item =>
                item.Query.Reference == id
                && item.Outcome == DomainEntityResolutionOutcome.Resolved) != 1))
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
        var required = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["a414Exception"] = "none",
            ["attacker"] = "unconcealedNonDummyOrdinaryInfantry",
            ["entryMode"] = "obstacleEntryNotBypass",
            ["overrunElection"] = "none",
            ["phase"] = "mph",
            ["specialModifier"] = "none",
        };
        if (required.Keys.Any(key => !facts.ContainsKey(key))
            || !facts.ContainsKey("defenderReveal") || !facts.ContainsKey("previousLocationId")
            || string.IsNullOrWhiteSpace(facts.GetValueOrDefault("previousLocationId"))
            || facts.GetValueOrDefault("previousLocationId") == locationId)
            ambiguities.Add("fact.post-reveal-evidence-incomplete");
        else if (facts.Count != 8 || required.Any(item => facts[item.Key] != item.Value))
            outside.Add("fact.outside-exact-case");

        var reveal = facts.GetValueOrDefault("defenderReveal");
        var forcedBack = reveal == "nonDummy";
        var dummiesOnly = reveal == "dummiesOnly";
        if (ambiguities.Count == 0 && !forcedBack && !dummiesOnly)
            ambiguities.Add("fact.defender-reveal-unresolved");
        var disposition = ambiguities.Count != 0 ? ConclusionDisposition.Indeterminate
            : outside.Count != 0 ? ConclusionDisposition.Abstained
            : forcedBack ? ConclusionDisposition.Definitive : ConclusionDisposition.Qualified;
        var evidence = new List<EvidenceReference>
        {
            new("asl-a1-post-reveal-manifest", EvidenceKind.CanonicalSource,
                question.TenantId, question.Package,
                "asl-scenario-a1.post-reveal-package.json",
                ScenarioA1PostRevealPackage.ManifestSha256,
                "user-directed-affirmative-xunit-review"),
        };
        if (observation is not null)
            evidence.Add(new EvidenceReference("asl-a1-reveal:" + observation.ObservationId,
                EvidenceKind.Observation, question.TenantId, question.Package,
                observation.ResourceId ?? observation.ObservationId, observation.Version,
                observation.Provenance ?? observation.Source.SourceId));
        CanonicalReference[] applicable = disposition is ConclusionDisposition.Definitive
            or ConclusionDisposition.Qualified
            ? forcedBack ? pinned.CanonicalSources.ToArray()
                : pinned.CanonicalSources.Where(item => item.ElementId == "A12.15").ToArray()
            : [];
        var caseId = forcedBack ? "A1-post-reveal-nondummy-forced-back"
            : "A1-post-reveal-dummies-only-continue";
        var value = disposition is ConclusionDisposition.Definitive or ConclusionDisposition.Qualified
            ? JsonSerializer.SerializeToElement(new
            {
                caseId,
                forcedBack,
                mayContinueFromAttemptedLocation = dummiesOnly,
                movementPhaseEnds = forcedBack,
                attemptedMfSpentInPreviousLocation = forcedBack,
                dummiesRemoved = dummiesOnly,
                previousLocationId = facts["previousLocationId"],
                followOnFireResolved = false,
            }) : (JsonElement?)null;
        return new DomainConclusion("asl-a1-post-reveal:" + question.QuestionId, question,
            disposition, value, evidence, applicable, [],
            disposition is ConclusionDisposition.Definitive or ConclusionDisposition.Qualified
                ? ["Ordinary Movement Phase obstacle entry; no bypass, entry exception, or overrun election."]
                : [],
            ambiguities,
            disposition switch
            {
                ConclusionDisposition.Indeterminate => ambiguities,
                ConclusionDisposition.Abstained => outside,
                _ => ["asl.a1.reveal." + (forcedBack ? "forced-back" : "dummies-only-continue")],
            },
            "A12.15 resolves the attempted entry only; this conclusion cannot move a unit or resolve follow-on fire.",
            "asl-scenario-a1.post-reveal-package.json#sha256:"
                + ScenarioA1PostRevealPackage.ManifestSha256,
            question.AskedAt);
    }

    private static string? Parameter(JsonElement parameters, string key) =>
        parameters.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;
}
