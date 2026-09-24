using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

public sealed record AslScenarioA1ChartReviewDecision(
    string Subject,
    string Status,
    string ReviewerAuthority,
    string RegistrySha256,
    string ComparisonSha256,
    string SourcePdfSha256,
    int PhysicalPdfPage,
    int WoodenBuildingInfantryMf,
    int StoneBuildingInfantryMf,
    string StoneRowNote,
    string NoteAppliesTo,
    string ChartRole,
    string CaseScope,
    string[] RuleBasis);

/// <summary>
/// Validates a user-delegated, executable review of the bounded chart supplement.
/// This decision does not verify unrelated rule fragments or admit a full case ruling.
/// </summary>
public static class AslScenarioA1ChartReview
{
    public const string CandidateId = "asl-supplement:b-terrain-chart-building-entry";
    public const string AcceptedStatus = "accepted-by-user-delegated-xunit-review";
    public const string Authority = "user-directed-xunit-review-2026-09-24";
    public const string Note = "Move assumes no road or VBM";
    public const string Scope = "Declared first case: ground-level ordinary wooden or stone building; no road, Bypass, elevation change, additional terrain, or SSR";

    public static AslScenarioA1ChartReviewDecision Evaluate(
        string repositoryRoot, JsonDocument supplement, JsonDocument comparison)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(supplement);
        ArgumentNullException.ThrowIfNull(comparison);
        var registryPath = Path.Combine(repositoryRoot, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.supplementary-source-registry.json");
        var comparisonPath = Path.Combine(repositoryRoot, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.backmatter-chart-pdf-comparison.json");
        var registered = supplement.RootElement;
        var compared = comparison.RootElement;

        Require(registered.GetProperty("status").GetString() == "registered-unverified-supplement"
            && registered.GetProperty("candidateId").GetString() == CandidateId
            && registered.GetProperty("physicalPdfPage").GetInt32() == 698
            && registered.GetProperty("sourcePdfSha256").GetString() == AslScenarioA1SourceInventory.PdfDigest,
            "Chart source registration changed.");
        Require(compared.GetProperty("status").GetString() ==
                "pdf-comparison-complete-source-verification-pending"
            && compared.GetProperty("sourceVerificationStatus").GetString() == "unverified"
            && compared.GetProperty("subject").GetString() == CandidateId
            && compared.GetProperty("physicalPdfPage").GetInt32() == 698
            && compared.GetProperty("sourcePdfSha256").GetString() == registered.GetProperty("sourcePdfSha256").GetString()
            && compared.GetProperty("extractedPageTextSha256").GetString() ==
                registered.GetProperty("extractedPageTextSha256").GetString(),
            "Chart PDF comparison changed.");
        Require(compared.GetProperty("transcription").GetString() ==
                registered.GetProperty("transcriptionPath").GetString()
            && compared.GetProperty("transcriptionSha256").GetString() ==
                registered.GetProperty("transcriptionSha256").GetString(),
            "Comparison and registration do not name the same transcription.");

        var source = Path.Combine(repositoryRoot,
            registered.GetProperty("transcriptionPath").GetString()!.Replace('/', Path.DirectorySeparatorChar));
        Require(Hashing.Sha256File(source) == registered.GetProperty("transcriptionSha256").GetString(),
            "The reviewed chart transcription changed.");
        using var registeredOnDisk = JsonDocument.Parse(File.ReadAllText(registryPath));
        using var comparedOnDisk = JsonDocument.Parse(File.ReadAllText(comparisonPath));
        Require(JsonElement.DeepEquals(registered, registeredOnDisk.RootElement)
            && JsonElement.DeepEquals(compared, comparedOnDisk.RootElement),
            "Review inputs differ from the pinned repository evidence.");

        var rows = registered.GetProperty("rows").EnumerateArray().ToArray();
        Require(rows.Length == 2
            && rows[0].GetProperty("rowLabel").GetString() == "23. Wooden Building"
            && rows[1].GetProperty("rowLabel").GetString() == "23. Stone Building"
            && rows.All(row => row.GetProperty("infantryEntryMf").GetInt32() == 2),
            "Building row identity or cost changed.");

        var observations = compared.GetProperty("observations").EnumerateArray().ToArray();
        var labels = new[] { "MF entrance costs, Infantry column", "23. Wooden Building",
            "23. Stone Building", "COT legend" };
        var digests = new[] { registered.GetProperty("infantryMfColumnHeaderLineSha256").GetString(),
            rows[0].GetProperty("extractedLineSha256").GetString(),
            rows[1].GetProperty("extractedLineSha256").GetString(),
            registered.GetProperty("cotLegendLineSha256").GetString() };
        Require(observations.Length == 4 && observations.Select((item, index) =>
            item.GetProperty("item").GetString() == labels[index]
            && item.GetProperty("matchesTranscription").GetBoolean()
            && item.GetProperty("extractedLineSha256").GetString() == digests[index]).All(valid => valid),
            "Header, rows, note or legend PDF comparison changed.");
        Require(!observations[1].GetProperty("finding").GetString()!.Contains(Note, StringComparison.Ordinal)
            && observations[2].GetProperty("finding").GetString()!.Contains(Note, StringComparison.Ordinal)
            && observations[3].GetProperty("finding").GetString()!.Contains("Cost of Terrain", StringComparison.Ordinal)
            && File.ReadAllText(source).Contains(Note, StringComparison.Ordinal),
            "The stone-row note is missing from the review evidence.");

        return new AslScenarioA1ChartReviewDecision(
            CandidateId, AcceptedStatus, Authority,
            Hashing.Sha256File(registryPath), Hashing.Sha256File(comparisonPath),
            AslScenarioA1SourceInventory.PdfDigest, 698, 2, 2, Note,
            "Stone building row only; the wooden row has no such printed note. The declared case excludes road and VBM.",
            "Controlling Infantry MF entrance-cost table cited by A4.13; B23.4 agrees at 2 MF for ordinary building entry.",
            Scope, ["A4.13", "B23.4"]);
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
