using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// Read-only conclusions for an elected Infantry OVR after a lone concealed SMC reveal (unit step 10): a failed NTC, and
/// a passed NTC followed by a second reveal that denies the OVR, both return the mover with the ordinary attempted-entry
/// MF; fewer than four MF leave no election; a passed NTC against a lone SMC and unknown occupancy stay Indeterminate.
/// </summary>
public sealed class ScenarioA1OvrNtcConclusionResolver : IDomainConclusionResolver
{
    public const string QuestionKind = "scenario-a1-ovr-ntc";
    private const string ForcedBack = "forced-back-attempted-entry-mf-in-previous-location";
    private readonly ScenarioA1OvrNtcPackage package = new();
    private readonly IReadOnlyDictionary<string, string> common;
    private readonly IReadOnlyDictionary<string, ReviewedCase> cases;

    public ScenarioA1OvrNtcConclusionResolver()
    {
        using var matrix = ScenarioA1OvrNtcPackage.Read("ScenarioA1.ovr-ntc-matrix.json", ScenarioA1OvrNtcPackage.MatrixSha256);
        common = ReadFacts(matrix.RootElement.GetProperty("commonFacts"));
        cases = matrix.RootElement.GetProperty("cases").EnumerateArray()
            .Select(item => new ReviewedCase(item.GetProperty("caseId").GetString()!, item.GetProperty("expectedDisposition").GetString()!,
                ReadFacts(item.GetProperty("facts"))))
            .ToDictionary(item => item.Id, StringComparer.Ordinal);
        if (cases.Count != 5 || cases.Values.Count(item => item.Disposition == ForcedBack) != 2)
        {
            throw new InvalidOperationException("The OVR NTC cases changed.");
        }
    }

    public async ValueTask<DomainConclusion> ConcludeAsync(DomainConclusionContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        var question = context.Question;
        if (question.Package != ScenarioA1OvrNtcPackage.Identity)
        {
            throw new ArgumentException("The exact OVR NTC package is required.", nameof(context));
        }

        var pinned = (await package.ResolveAsync(question.Package, cancellationToken)).Package!;
        if (!context.Package.CanonicalSources.SequenceEqual(pinned.CanonicalSources))
        {
            throw new ArgumentException("The OVR NTC source set changed.", nameof(context));
        }

        var ambiguities = new List<string>();
        var outside = new List<string>();
        if (question.Kind.Value != QuestionKind || question.Parameters.ValueKind != JsonValueKind.Object
            || question.Parameters.EnumerateObject().Count() != 5 || context.CalculationEvidence.Count != 0)
        {
            outside.Add("asl.a1.ovr-ntc.question-outside-exact-contract");
        }

        var unitId = Parameter(question.Parameters, "unitId");
        var locationId = Parameter(question.Parameters, "locationId");
        var previousId = Parameter(question.Parameters, "previousLocationId");
        var version = Parameter(question.Parameters, "observationVersion");
        var caseId = Parameter(question.Parameters, "caseId");
        if (unitId is null || locationId is null || previousId is null || version is null || caseId is null
            || new[] { unitId, locationId, previousId }.Distinct().Count() != 3)
        {
            ambiguities.Add("question.subject-previous-location-case-or-version-missing");
        }

        if (context.EntityResolutions.Count != 3 || unitId is null || locationId is null || previousId is null
            || new[] { unitId, locationId, previousId }.Any(id => context.EntityResolutions.Count(item => item.Query.Reference == id
                && item.Outcome == DomainEntityResolutionOutcome.Resolved) != 1))
        {
            ambiguities.Add("entity.resolution-incomplete-or-ambiguous");
        }

        var observation = context.Observations.Count == 1 ? context.Observations[0] : null;
        if (observation is null || observation.Version != version || observation.TenantId != question.TenantId
            || observation.DomainPackage != question.Package || observation.ResourceId != locationId || observation.ObservedAt > question.AskedAt)
        {
            ambiguities.Add("observation.exact-package-version-and-location-required");
        }

        var facts = new Dictionary<string, string>(StringComparer.Ordinal);
        if (observation is not null)
        {
            if (observation.Data.ValueKind != JsonValueKind.Object)
            {
                ambiguities.Add("observation.facts-object-required");
            }
            else
            {
                foreach (var item in observation.Data.EnumerateObject())
                {
                    if (item.Value.ValueKind != JsonValueKind.String || !facts.TryAdd(item.Name, item.Value.GetString()!))
                    {
                        ambiguities.Add("observation.duplicate-or-nonstring-fact");
                    }
                }
            }
        }

        if (!facts.Remove("previousLocationId", out var suppliedPrevious) || string.IsNullOrWhiteSpace(suppliedPrevious) || suppliedPrevious != previousId)
        {
            ambiguities.Add("observation.previous-location-does-not-match-question");
        }

        ReviewedCase? reviewed = null;
        if (caseId is null || !cases.TryGetValue(caseId, out reviewed))
        {
            outside.Add("asl.a1.ovr-ntc.unknown-case");
        }
        else
        {
            var expected = common.Concat(reviewed.Facts).ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            if (facts.Keys.Except(expected.Keys, StringComparer.Ordinal).Any()
                || facts.Any(item => expected.TryGetValue(item.Key, out var value) && item.Value != value))
            {
                outside.Add("asl.a1.ovr-ntc.extra-or-contrary-fact");
            }

            if (expected.Keys.Except(facts.Keys, StringComparer.Ordinal).Any())
            {
                ambiguities.Add("asl.a1.ovr-ntc.required-fact-missing");
            }
        }

        var disposition = outside.Count != 0 ? ConclusionDisposition.Abstained
            : ambiguities.Count != 0 ? ConclusionDisposition.Indeterminate
            : reviewed!.Disposition switch
            {
                ForcedBack => ConclusionDisposition.Definitive,
                "election-unavailable" => ConclusionDisposition.Abstained,
                _ => ConclusionDisposition.Indeterminate,
            };
        if (disposition == ConclusionDisposition.Indeterminate && ambiguities.Count == 0)
        {
            ambiguities.Add("asl.a1.ovr-ntc.reviewed-unresolved-branch");
        }

        if (disposition == ConclusionDisposition.Abstained && outside.Count == 0)
        {
            outside.Add("asl.a1.ovr-ntc.election-unavailable");
        }

        var evidence = new List<EvidenceReference>
        {
            new("asl-a1-ovr-ntc-package", EvidenceKind.CanonicalSource, question.TenantId, question.Package,
                "asl-scenario-a1.ovr-ntc-package.json", ScenarioA1OvrNtcPackage.ManifestSha256, "user-directed-affirmative-xunit-review"),
            new("asl-a1-ovr-ntc-matrix", EvidenceKind.CanonicalSource, question.TenantId, question.Package,
                "asl-scenario-a1.ovr-ntc-case-matrix.json", ScenarioA1OvrNtcPackage.MatrixSha256, "user-directed-affirmative-xunit-review"),
        };
        if (observation is not null)
        {
            evidence.Add(new EvidenceReference("asl-a1-ovr-ntc:" + observation.ObservationId, EvidenceKind.Observation, question.TenantId,
                question.Package, observation.ResourceId ?? observation.ObservationId, observation.Version,
                observation.Provenance ?? observation.Source.SourceId));
        }

        var value = disposition == ConclusionDisposition.Definitive
            ? JsonSerializer.SerializeToElement(new
            {
                caseId,
                returnToLocationId = suppliedPrevious,
                attemptedEntryMf = 2,
                mfExpenditureLocationId = suppliedPrevious,
                movementPhaseEnds = true,
                doubledOvrMfCharged = false,
                followOnFireResolved = false,
                executed = false,
            })
            : (JsonElement?)null;
        return new DomainConclusion("asl-a1-ovr-ntc:" + question.QuestionId, question, disposition, value, evidence,
            disposition == ConclusionDisposition.Definitive ? pinned.CanonicalSources.ToArray() : [], [],
            disposition == ConclusionDisposition.Definitive
                ? ["A12.15 returns the mover to its previous Location with the ordinary attempted-entry MF expended there; the OVR does not enter."]
                : [],
            ambiguities, disposition == ConclusionDisposition.Abstained ? outside
                : disposition == ConclusionDisposition.Indeterminate ? ambiguities
                : ["asl.a1.ovr-ntc." + reviewed!.Disposition],
            "Read-only conclusion; no roll, return, or MF charge is executed, and no attack or CC is resolved.",
            "asl-scenario-a1.ovr-ntc-package.json#sha256:" + ScenarioA1OvrNtcPackage.ManifestSha256, question.AskedAt);
    }

    private static string? Parameter(JsonElement parameters, string name) =>
        parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;

    private static Dictionary<string, string> ReadFacts(JsonElement element) =>
        element.EnumerateObject().ToDictionary(item => item.Name, item => item.Value.GetString()!, StringComparer.Ordinal);

    private sealed record ReviewedCase(string Id, string Disposition, IReadOnlyDictionary<string, string> Facts);
}
