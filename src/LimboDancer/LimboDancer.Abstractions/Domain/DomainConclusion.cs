using System.Collections.ObjectModel;
using System.Text.Json;

namespace LimboDancer.Abstractions.Domain;

public sealed class DomainConclusion
{
    public DomainConclusion(
        string conclusionId,
        DomainQuestion question,
        ConclusionDisposition disposition,
        JsonElement? value,
        IEnumerable<EvidenceReference> evidence,
        IEnumerable<CanonicalReference>? applicableRules,
        IEnumerable<CanonicalReference>? controllingExceptions,
        IEnumerable<string>? assumptions,
        IEnumerable<string>? ambiguities,
        IEnumerable<string> reasonCodes,
        string explanation,
        string explanationProvenance,
        DateTimeOffset concludedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conclusionId);
        ArgumentNullException.ThrowIfNull(question);
        if (!Enum.IsDefined(disposition))
        {
            throw new ArgumentOutOfRangeException(nameof(disposition));
        }

        var evidenceValues = CopyRequired(evidence, nameof(evidence));
        var ruleValues = CopyOptional(applicableRules, nameof(applicableRules));
        var exceptionValues = CopyOptional(controllingExceptions, nameof(controllingExceptions));
        var assumptionValues = CopyStrings(assumptions, nameof(assumptions));
        var ambiguityValues = CopyStrings(ambiguities, nameof(ambiguities));
        var reasonValues = CopyStrings(reasonCodes, nameof(reasonCodes));
        if (evidenceValues.Count == 0)
        {
            throw new ArgumentException("A domain conclusion requires material evidence.", nameof(evidence));
        }

        if (reasonValues.Count == 0)
        {
            throw new ArgumentException("A domain conclusion requires at least one reason code.", nameof(reasonCodes));
        }

        if (disposition is ConclusionDisposition.Definitive or ConclusionDisposition.Qualified
            && value is null)
        {
            throw new ArgumentException("A definitive or qualified conclusion requires a value.", nameof(value));
        }

        if (disposition == ConclusionDisposition.Definitive && ambiguityValues.Count != 0)
        {
            throw new ArgumentException("A definitive conclusion cannot contain unresolved ambiguity.", nameof(ambiguities));
        }

        if (disposition == ConclusionDisposition.Indeterminate && ambiguityValues.Count == 0)
        {
            throw new ArgumentException("An indeterminate conclusion must identify unresolved evidence.", nameof(ambiguities));
        }

        EnsureScope(question, evidenceValues, ruleValues, exceptionValues);
        ArgumentException.ThrowIfNullOrWhiteSpace(explanation);
        ArgumentException.ThrowIfNullOrWhiteSpace(explanationProvenance);

        ConclusionId = conclusionId;
        Question = question;
        Disposition = disposition;
        Value = value?.Clone();
        Evidence = evidenceValues;
        ApplicableRules = ruleValues;
        ControllingExceptions = exceptionValues;
        Assumptions = assumptionValues;
        Ambiguities = ambiguityValues;
        ReasonCodes = reasonValues;
        Explanation = explanation;
        ExplanationProvenance = explanationProvenance;
        ConcludedAt = concludedAt;
    }

    public string ConclusionId
    {
        get;
    }

    public DomainQuestion Question
    {
        get;
    }

    public ConclusionDisposition Disposition
    {
        get;
    }

    public JsonElement? Value
    {
        get;
    }

    public IReadOnlyList<EvidenceReference> Evidence
    {
        get;
    }

    public IReadOnlyList<CanonicalReference> ApplicableRules
    {
        get;
    }

    public IReadOnlyList<CanonicalReference> ControllingExceptions
    {
        get;
    }

    public IReadOnlyList<string> Assumptions
    {
        get;
    }

    public IReadOnlyList<string> Ambiguities
    {
        get;
    }

    public IReadOnlyList<string> ReasonCodes
    {
        get;
    }

    public string Explanation
    {
        get;
    }

    public string ExplanationProvenance
    {
        get;
    }

    public DateTimeOffset ConcludedAt
    {
        get;
    }

    private static ReadOnlyCollection<T> CopyRequired<T>(IEnumerable<T> values, string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var copied = values.ToArray();
        if (copied.Any(static value => value is null))
        {
            throw new ArgumentException("Collection values cannot be null.", parameterName);
        }

        return new ReadOnlyCollection<T>(copied);
    }

    private static ReadOnlyCollection<T> CopyOptional<T>(IEnumerable<T>? values, string parameterName)
        where T : class => CopyRequired(values ?? [], parameterName);

    private static ReadOnlyCollection<string> CopyStrings(IEnumerable<string>? values, string parameterName)
    {
        var copied = (values ?? []).ToArray();
        if (copied.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Collection values cannot be empty.", parameterName);
        }

        return new ReadOnlyCollection<string>(copied);
    }

    private static void EnsureScope(
        DomainQuestion question,
        IEnumerable<EvidenceReference> evidence,
        IEnumerable<CanonicalReference> rules,
        IEnumerable<CanonicalReference> exceptions)
    {
        if (evidence.Any(item =>
                item.TenantId != question.TenantId
                || item.Package != question.Package))
        {
            throw new ArgumentException("Conclusion evidence must match the question tenant and package.", nameof(evidence));
        }

        if (rules.Concat(exceptions).Any(reference => reference.Package != question.Package))
        {
            throw new ArgumentException("Conclusion rules must match the question package.", nameof(rules));
        }
    }
}
