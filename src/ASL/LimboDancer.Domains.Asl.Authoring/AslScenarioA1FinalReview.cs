using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

public sealed record AslScenarioA1FinalReview(
    string Status,
    string ReviewerAuthority,
    string PdfComparisonSha256,
    string SourceAttestationSha256,
    string ChartReviewSha256,
    string TirDocumentSha256,
    int NewlyVerifiedFragmentCount,
    string[] RequiredRuleIds,
    string[] ExcludedBranchRuleIds,
    string DependencyDecision,
    string CaseDecision,
    IReadOnlyList<TirSourceVerificationRecord> NewlyVerifiedRecords,
    AslScenarioA1CaseAssessment Assessment);

/// <summary>
/// User-delegated executable review of one explicitly declared Scenario A1 case.
/// Its source attestations are separate from the earlier comparison history.
/// </summary>
public static class AslScenarioA1FinalReviewer
{
    public const string Authority = "user-directed-xunit-review-2026-09-24";
    public const string DependencyDecision =
        "A5.5 and A4.12 do not need new source subjects because squad identity, remaining MF and below-limit stacking are declared inputs; hidden occupancy, fortification, road, Bypass, elevation, SSR and other modifiers are explicitly excluded. Unknown inputs are outside this decision.";
    public const string CaseDecision =
        "For the declared Good Order Infantry squad during its MPh, entry into an adjacent empty ground-level ordinary wooden or stone building is permitted at 2 MF, provided the declared movement capability, available MF, stacking and absence of special modifiers are true.";
    private static readonly DateTimeOffset ReviewedAt = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    public static AslScenarioA1FinalReview Review(
        string repositoryRoot, GeneratedManifests manifests,
        AslScenarioA1SourceAttestation originalAttestation,
        JsonDocument comparison, AslScenarioA1ChartReviewDecision chartDecision,
        AslScenarioA1CaseFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var directory = Path.Combine(repositoryRoot, "docs", "ASL", "SourceRegistry");
        var comparisonPath = Path.Combine(directory, "asl-scenario-a1.first-case-pdf-comparison.json");
        using var comparisonOnDisk = JsonDocument.Parse(File.ReadAllText(comparisonPath));
        if (!JsonElement.DeepEquals(comparison.RootElement, comparisonOnDisk.RootElement))
        {
            throw new InvalidOperationException("The delegated comparison differs from the pinned evidence.");
        }

        var initial = AslScenarioA1VerificationBatchBuilder.Build(manifests, originalAttestation);
        var pending = AslScenarioA1ComparisonBatch.Build(manifests, originalAttestation, comparison);
        if (initial.Records.Count != 11 || pending.Records.Count != 10
            || initial.Records.Any(record => record.Disposition != TirSourceVerificationDisposition.Verified)
            || pending.Records.Any(record => record.Disposition != TirSourceVerificationDisposition.Indeterminate)
            || TirCanonicalJson.ComputePayloadSha256(initial.SourceDocument)
                != TirCanonicalJson.ComputePayloadSha256(pending.SourceDocument))
        {
            throw new InvalidOperationException("The 11 attested and 10 compared subjects must bind to one full TIR.");
        }

        var fragments = manifests.Fragments.ToDictionary(fragment => fragment.FragmentId, StringComparer.Ordinal);
        var verifiedRecords = pending.Records.Select(record =>
        {
            var fragment = fragments[record.SourceFragment.FragmentId];
            return TirSourceVerificationService.CreateRecord(
                pending.SourceDocument, record.Subject.ArtifactId, manifests.Registry, fragment,
                $"User-delegated xUnit fidelity review of exact comparison record; PDF SHA-256 "
                    + $"{AslScenarioA1SourceInventory.PdfDigest}; comparison SHA-256 "
                    + Hashing.Sha256File(comparisonPath) + ".",
                TirSourceVerificationDisposition.Verified, null, [], ReviewedAt,
                Authority, "source-provider:delegated-xunit-review");
        }).ToArray();
        var verifiedIds = initial.Records.Concat(verifiedRecords)
            .Select(record => record.SourceFragment.FragmentId).ToHashSet(StringComparer.Ordinal);
        var chartPath = Path.Combine(directory, "asl-scenario-a1.chart-review-decision.json");
        var chartOnDisk = JsonSerializer.Deserialize<AslScenarioA1ChartReviewDecision>(
            File.ReadAllText(chartPath), WebJsonOptions)
            ?? throw new InvalidOperationException("Chart decision is missing.");
        if (JsonSerializer.Serialize(chartDecision) != JsonSerializer.Serialize(chartOnDisk))
        {
            throw new InvalidOperationException("Chart decision differs from the pinned decision.");
        }

        var assessed = AslScenarioA1CaseAssessor.AssessWithReviewedChart(
            facts, manifests.Fragments, verifiedIds, repositoryRoot, chartDecision);
        var hasOutsideScope = assessed.Blockers.Any(blocker =>
            blocker.Kind == AslScenarioA1CaseBlockerKind.FactOutsideDeclaredScope);
        if (assessed.Blockers.Any(blocker => blocker.Kind is
            AslScenarioA1CaseBlockerKind.SourceFragmentUnverified
            or AslScenarioA1CaseBlockerKind.SourceRuleNotLocated
            or AslScenarioA1CaseBlockerKind.SourceBoundaryUnresolved))
        {
            throw new InvalidOperationException("Required source evidence is incomplete.");
        }

        var completed = hasOutsideScope ? assessed : assessed with
        {
            Blockers = assessed.Blockers.Where(blocker =>
                blocker.Kind != AslScenarioA1CaseBlockerKind.DependencyAndSemanticReviewPending).ToArray(),
        };
        return new AslScenarioA1FinalReview(
            hasOutsideScope ? "outside-declared-case" : "accepted-declared-case",
            Authority, Hashing.Sha256File(comparisonPath),
            Hashing.Sha256File(Path.Combine(directory, "asl-scenario-a1.source-attestation.json")),
            Hashing.Sha256File(chartPath),
            TirCanonicalJson.ComputePayloadSha256(pending.SourceDocument),
            verifiedRecords.Length, assessed.RequiredRuleIds.ToArray(),
            assessed.ExcludedBranchRuleIds.ToArray(), DependencyDecision, CaseDecision,
            verifiedRecords, completed);
    }
}
