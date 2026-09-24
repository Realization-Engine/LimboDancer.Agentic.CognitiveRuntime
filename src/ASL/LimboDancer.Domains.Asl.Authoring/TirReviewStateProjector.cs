namespace LimboDancer.Domains.Asl.Authoring;

public static class TirReviewStateProjector
{
    public static TirReviewProjection Project(
        TirDocument document,
        TirReviewSubjectReference subject,
        TirValidationPolicy policy,
        IReadOnlyList<TirReviewRecord> records,
        string declaredUse)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(records);
        ArgumentException.ThrowIfNullOrWhiteSpace(declaredUse);
        var exact = TirReviewSubjects.Create(document, subject.ArtifactId);
        if (exact != subject)
        {
            throw new InvalidOperationException("Review bundle subject does not match the exact TIR artifact snapshot.");
        }

        // TIR 1.3 cannot represent a curated proposal. No ledger record can change
        // the captured status of the extracted artifact itself.
        var artifact = document.Artifacts.Single(item => item.Envelope.ArtifactId == subject.ArtifactId);
        if (artifact.Envelope.Origin != TirArtifactOrigin.Extracted
            || artifact.Envelope.ReviewStatus != TirReviewStatus.Captured
            || artifact.Envelope.FormalizationStatus != TirFormalizationStatus.Unmodeled)
        {
            throw new InvalidOperationException("TIR 1.3 review projection requires captured extraction evidence.");
        }

        var byId = new Dictionary<string, TirReviewRecord>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            if (record.Subject != subject)
            {
                throw new InvalidOperationException("A review record targets a different exact subject.");
            }

            var id = TirReviewCanonicalJson.CreateRecordId(record);
            if (!byId.TryAdd(id, record))
            {
                throw new InvalidOperationException($"Duplicate review record: {id}.");
            }
        }

        foreach (var (id, record) in byId)
        {
            foreach (var prerequisite in record.PrerequisiteRecordRefs)
            {
                if (!byId.TryGetValue(prerequisite, out var predecessor)
                    || prerequisite == id
                    || predecessor.CreatedAt > record.CreatedAt)
                {
                    throw new InvalidOperationException($"Missing, self-referential, or later prerequisite: {prerequisite}.");
                }
            }

            if (record is TirReviewDecisionRecord or TirAdjudicationRecord)
            {
                throw new InvalidOperationException(
                    "Captured extraction cannot undergo review-state transitions or adjudication.");
            }

            if (record is TirDiagnosticDispositionRecord disposition
                && disposition.Finding.ReportRecordRef is not null
                && !record.PrerequisiteRecordRefs.Contains(
                    disposition.Finding.ReportRecordRef,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException("Disposition must include its report as a prerequisite.");
            }
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in byId.Keys)
        {
            Visit(id);
        }

        void Visit(string id)
        {
            if (visited.Contains(id))
            {
                return;
            }

            if (!visiting.Add(id))
            {
                throw new InvalidOperationException("Review record prerequisites contain a cycle.");
            }

            foreach (var prerequisite in byId[id].PrerequisiteRecordRefs)
            {
                Visit(prerequisite);
            }

            visiting.Remove(id);
            visited.Add(id);
        }

        var reports = records.OfType<TirValidationReportRecord>().ToArray();
        foreach (var report in reports)
        {
            if (report.Policy != policy)
            {
                throw new InvalidOperationException("Validation report policy does not match the bundle policy.");
            }
        }

        var verifications = records.OfType<TirSourceVerificationRecord>().ToArray();
        foreach (var verification in verifications)
        {
            if (!artifact.Envelope.SourceFragments.Contains(verification.SourceFragment))
            {
                throw new InvalidOperationException("Source verification does not bind a subject fragment.");
            }
        }

        var findings = new Dictionary<string, TirFindingReference>(StringComparer.Ordinal);
        foreach (var diagnostic in document.Diagnostics.Where(item =>
            item.ArtifactId is null || item.ArtifactId == subject.ArtifactId))
        {
            var id = TirDiagnosticIdentity.Create(subject, diagnostic);
            if (!findings.TryAdd(id, new TirFindingReference(
                TirFindingOrigin.ExtractedDiagnostic,
                id,
                diagnostic.Code,
                diagnostic.Severity,
                null)))
            {
                throw new InvalidOperationException($"Duplicate extracted diagnostic identity: {id}.");
            }
        }

        foreach (var report in reports)
        {
            var reportId = TirReviewCanonicalJson.CreateRecordId(report);
            foreach (var finding in report.Findings)
            {
                if (!findings.TryAdd(finding.FindingId, new TirFindingReference(
                    TirFindingOrigin.ValidationFinding,
                    finding.FindingId,
                    finding.Code,
                    finding.Severity,
                    reportId)))
                {
                    throw new InvalidOperationException($"Duplicate finding identity: {finding.FindingId}.");
                }
            }
        }

        var dispositions = new Dictionary<string, TirDiagnosticDispositionRecord>(StringComparer.Ordinal);
        foreach (var disposition in records.OfType<TirDiagnosticDispositionRecord>())
        {
            if (!findings.TryGetValue(disposition.Finding.FindingId, out var original)
                || original != disposition.Finding
                || !dispositions.TryAdd(disposition.Finding.FindingId, disposition))
            {
                throw new InvalidOperationException("Disposition does not bind one unique finding with its original severity.");
            }

            if (disposition.Finding.Origin == TirFindingOrigin.ValidationFinding
                && (!byId.TryGetValue(disposition.Finding.ReportRecordRef!, out var referenced)
                    || referenced is not TirValidationReportRecord report
                    || !report.Findings.Any(item => item.FindingId == disposition.Finding.FindingId
                        && item.Code == disposition.Finding.Code
                        && item.Severity == disposition.Finding.Severity)))
            {
                throw new InvalidOperationException("Validation disposition does not bind its exact report and finding.");
            }
        }

        // A disposition never erases a finding in the captured snapshot. Resolution
        // requires a new subject; scope adjudication is deferred to a semantic use.
        var open = findings.Keys.Order(StringComparer.Ordinal).ToArray();
        var blocking = findings
            .Where(static item => item.Value.Severity is TirDiagnosticSeverity.Error or TirDiagnosticSeverity.Warning)
            .Select(static item => item.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return new TirReviewProjection(
            TirReviewStatus.Captured,
            TirFormalizationStatus.Unmodeled,
            open,
            blocking);
    }

    public static TirReviewBundle CreateBundle(
        TirDocument document,
        string artifactId,
        TirValidationPolicy policy,
        string declaredUse,
        IReadOnlyList<string> coverageRefs,
        IReadOnlyList<TirReviewRecord> records,
        DateTimeOffset createdAt,
        string createdAtSource)
    {
        ArgumentNullException.ThrowIfNull(coverageRefs);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdAtSource);
        if (createdAt.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("Review bundle timestamp must be supplied in UTC.");
        }

        var subject = TirReviewSubjects.Create(document, artifactId);
        var projection = Project(document, subject, policy, records, declaredUse);
        return new TirReviewBundle(
            document,
            subject,
            policy,
            declaredUse,
            coverageRefs,
            records,
            projection,
            createdAt,
            createdAtSource);
    }
}
