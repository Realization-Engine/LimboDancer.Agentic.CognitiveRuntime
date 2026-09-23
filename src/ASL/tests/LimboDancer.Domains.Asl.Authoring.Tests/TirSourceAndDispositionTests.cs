namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirSourceAndDispositionTests
{
    private static readonly DateTimeOffset CreatedAt = new(
        2026,
        9,
        23,
        12,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public void VerifiedSourceRecordPinsRegistryFragmentPagesAndVisualDependencies()
    {
        var registry = Registry(includeImage: true);
        var fragment = Fragment(
            "**A.1:** See the figure.\n",
            ["images/example.png"]);
        var document = Extract(registry, fragment);
        var rule = Assert.Single(document.Artifacts.OfType<TirRuleArtifact>());

        var first = Verify(
            document,
            rule.Envelope.ArtifactId,
            registry,
            fragment,
            TirSourceVerificationDisposition.Verified,
            null,
            []);
        var repeated = Verify(
            document,
            rule.Envelope.ArtifactId,
            registry,
            fragment,
            TirSourceVerificationDisposition.Verified,
            null,
            []);
        var dependency = Assert.Single(first.Dependencies);

        Assert.Equal(registry.RegistryId, first.SourceEvidence.RegistryId);
        Assert.Equal(registry.Edition, first.SourceEvidence.Edition);
        Assert.Equal(fragment.SourcePath, first.SourceEvidence.SourcePath);
        Assert.Equal(43, first.SourceEvidence.StartPage);
        Assert.Equal(43, first.SourceEvidence.EndPage);
        Assert.Equal(TirDependencyKind.Figure, dependency.Kind);
        Assert.Equal("docs/ASL/Rulebook_Markdown/images/example.png", dependency.Target);
        Assert.Equal(Sha('b'), dependency.Sha256);
        Assert.Equal(
            TirReviewCanonicalJson.Serialize(first),
            TirReviewCanonicalJson.Serialize(repeated));
    }

    [Fact]
    public void TamperedFragmentCannotProduceSourceVerificationRecord()
    {
        var registry = Registry(includeImage: true);
        var fragment = Fragment("**A.1:** Source text.\n", []);
        var document = Extract(registry, fragment);
        var rule = Assert.Single(document.Artifacts.OfType<TirRuleArtifact>());
        var tampered = fragment with
        {
            Content = "changed source text\n",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => Verify(
                document,
                rule.Envelope.ArtifactId,
                registry,
                tampered,
                TirSourceVerificationDisposition.Verified,
                null,
                []));

        Assert.Contains("does not reproduce", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingVisualDependencyRequiresExplicitDependencyMissingDisposition()
    {
        var registry = Registry(includeImage: false);
        var fragment = Fragment(
            "**A.1:** See the figure.\n",
            ["images/missing.png"]);
        var document = Extract(registry, fragment);
        var rule = Assert.Single(document.Artifacts.OfType<TirRuleArtifact>());

        var verifiedException = Assert.Throws<InvalidOperationException>(
            () => Verify(
                document,
                rule.Envelope.ArtifactId,
                registry,
                fragment,
                TirSourceVerificationDisposition.Verified,
                null,
                []));
        var record = Verify(
            document,
            rule.Envelope.ArtifactId,
            registry,
            fragment,
            TirSourceVerificationDisposition.DependencyMissing,
            "The referenced figure is absent from the pinned registry.",
            []);

        Assert.Contains("dependency-missing", verifiedException.Message, StringComparison.Ordinal);
        Assert.Empty(record.Dependencies);
        Assert.Equal(TirSourceVerificationDisposition.DependencyMissing, record.Disposition);
    }

    [Fact]
    public void SourceMismatchRequiresDiscrepancyAndCorrectionProposal()
    {
        var registry = Registry(includeImage: false);
        var fragment = Fragment("**A.1:** Source text.\n", []);
        var document = Extract(registry, fragment);
        var rule = Assert.Single(document.Artifacts.OfType<TirRuleArtifact>());

        var exception = Assert.Throws<InvalidOperationException>(
            () => Verify(
                document,
                rule.Envelope.ArtifactId,
                registry,
                fragment,
                TirSourceVerificationDisposition.Mismatch,
                "The authoritative edition uses different punctuation.",
                []));
        var record = Verify(
            document,
            rule.Envelope.ArtifactId,
            registry,
            fragment,
            TirSourceVerificationDisposition.Mismatch,
            "The authoritative edition uses different punctuation.",
            ["proposal:correct-punctuation"]);

        Assert.Contains("correction proposal", exception.Message, StringComparison.Ordinal);
        Assert.Equal(TirSourceVerificationDisposition.Mismatch, record.Disposition);
        Assert.Contains("proposal:correct-punctuation", record.CorrectionProposalRefs);
    }

    [Fact]
    public void ExtractedDiagnosticIdentityAndDeferredDispositionRemainOpen()
    {
        var registry = Registry(includeImage: false);
        var fragment = Fragment("**A.1:** See B9.9.\n", []);
        var document = Extract(registry, fragment);
        var diagnostic = Assert.Single(
            document.Diagnostics,
            static diagnostic => diagnostic.Code == "TIR-MISSING-REFERENCE-TARGET");
        var artifactId = Assert.IsType<string>(diagnostic.ArtifactId);
        var subject = TirReviewSubjects.Create(document, artifactId);

        var firstId = TirDiagnosticIdentity.Create(subject, diagnostic);
        var repeatedId = TirDiagnosticIdentity.Create(subject, diagnostic);
        var changedId = TirDiagnosticIdentity.Create(
            subject,
            diagnostic with
            {
                Message = "Changed diagnostic message.",
            });
        var disposition = TirDiagnosticDispositionService.CreateForExtractedDiagnostic(
            document,
            artifactId,
            diagnostic,
            TirFindingDisposition.Deferred,
            "Outside the current evaluation closure.",
            ["scope:scenario-a1"],
            "tracking:ASL-32",
            CreatedAt,
            "test-fixture",
            "reviewer@example.invalid");

        Assert.Equal(firstId, repeatedId);
        Assert.NotEqual(firstId, changedId);
        Assert.Equal(firstId, disposition.Finding.FindingId);
        Assert.Equal(diagnostic.Severity, disposition.Finding.Severity);
        Assert.True(TirDiagnosticDispositionService.IsOpen(disposition));
        Assert.True(TirDiagnosticDispositionService.BlocksDefinitiveUse(disposition));
    }

    [Fact]
    public void ResolvedDispositionRequiresChangedEvidenceReference()
    {
        var registry = Registry(includeImage: false);
        var fragment = Fragment("**A.1:** See B9.9.\n", []);
        var document = Extract(registry, fragment);
        var diagnostic = Assert.Single(document.Diagnostics);
        var artifactId = Assert.IsType<string>(diagnostic.ArtifactId);

        var exception = Assert.Throws<InvalidOperationException>(
            () => TirDiagnosticDispositionService.CreateForExtractedDiagnostic(
                document,
                artifactId,
                diagnostic,
                TirFindingDisposition.Resolved,
                "The source was corrected.",
                ["evidence:corrected-source"],
                null,
                CreatedAt,
                "test-fixture",
                "reviewer@example.invalid"));

        Assert.Contains("replacement-subject reference", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationFindingDispositionPinsItsExactReport()
    {
        var registry = Registry(includeImage: false);
        var fragment = Fragment("**A.1:** See B9.9.\n", []);
        var document = Extract(registry, fragment);
        var crossReference = Assert.Single(document.Artifacts.OfType<TirCrossReferenceArtifact>());
        var report = TirStructuralValidator.CreateReport(
            document,
            crossReference.Envelope.ArtifactId,
            CreatedAt,
            "test-fixture",
            "validator@example.invalid");
        var finding = Assert.Single(
            report.Findings,
            static finding => finding.Code == "TIR-VAL-REFERENCE-UNRESOLVED");

        var disposition = TirDiagnosticDispositionService.CreateForValidationFinding(
            report,
            finding,
            TirFindingDisposition.NotApplicable,
            "The reference is outside the declared scenario scope.",
            ["scope:scenario-a1"],
            null,
            CreatedAt,
            "test-fixture",
            "reviewer@example.invalid");
        var reportRef = TirReviewCanonicalJson.CreateRecordId(report);

        Assert.Equal(reportRef, disposition.Finding.ReportRecordRef);
        Assert.Contains(reportRef, disposition.PrerequisiteRecordRefs);
        Assert.False(TirDiagnosticDispositionService.IsOpen(disposition));
        Assert.False(TirDiagnosticDispositionService.BlocksDefinitiveUse(disposition));
    }

    private static TirSourceVerificationRecord Verify(
        TirDocument document,
        string artifactId,
        SourceRegistryManifest registry,
        SourceFragment fragment,
        TirSourceVerificationDisposition disposition,
        string? discrepancy,
        IReadOnlyList<string> correctionProposalRefs)
    {
        return TirSourceVerificationService.CreateRecord(
            document,
            artifactId,
            registry,
            fragment,
            "operator comparison with authoritative ASL 3.10 edition",
            disposition,
            discrepancy,
            correctionProposalRefs,
            CreatedAt,
            "test-fixture",
            "source-verifier@example.invalid");
    }

    private static TirDocument Extract(
        SourceRegistryManifest registry,
        SourceFragment fragment)
    {
        return TirStructuralExtractor.Extract(
            registry,
            [fragment],
            new TirExtractionOptions(
                new TirPackageCandidate("asl", "easlrb-3.10-a-e", "0.0.0-candidate.1"),
                CreatedAt,
                "test-fixture"));
    }

    private static SourceRegistryManifest Registry(bool includeImage)
    {
        var artifacts = new List<SourceArtifact>
        {
            new(
                "asl-easlrb-3.10:chapter-a",
                "docs/ASL/Rulebook_Markdown/chapter-a.md",
                SourceArtifactKind.Markdown,
                Sha('a'),
                100,
                "A",
                1,
                97),
        };
        if (includeImage)
        {
            artifacts.Add(
                new SourceArtifact(
                    "asl-easlrb-3.10:image-example",
                    "docs/ASL/Rulebook_Markdown/images/example.png",
                    SourceArtifactKind.Image,
                    Sha('b'),
                    25));
        }

        return new SourceRegistryManifest(
            AslSourceRegistryBuilder.SchemaVersion,
            AslSourceRegistryBuilder.RegistryId,
            AslSourceRegistryBuilder.Edition,
            new string('c', 40),
            AslSourceRegistryBuilder.SourceRoot,
            new ConversionTool(AslSourceRegistryBuilder.ConversionToolPath, Sha('f')),
            artifacts);
    }

    private static SourceFragment Fragment(
        string content,
        IReadOnlyList<string> dependencies)
    {
        var contentSha256 = Hashing.Sha256Text(content);
        return new SourceFragment(
            $"asl-fragment:sha256:{Hashing.Sha256Text("fragment-source-verification")}",
            "asl-easlrb-3.10:chapter-a",
            "docs/ASL/Rulebook_Markdown/chapter-a.md",
            Sha('a'),
            SourceFragmentKind.RuleText,
            contentSha256,
            content,
            new SourceLocator(
                10,
                10,
                43,
                43,
                ["A. INFANTRY"],
                "A.1",
                "A.1"),
            dependencies,
            false);
    }

    private static string Sha(char value)
    {
        return new string(value, 64);
    }
}
