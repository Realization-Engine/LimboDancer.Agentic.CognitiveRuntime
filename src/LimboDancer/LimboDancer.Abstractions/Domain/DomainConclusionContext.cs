using System.Collections.ObjectModel;
using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Abstractions.Domain;

public sealed class DomainConclusionContext
{
    public DomainConclusionContext(
        DomainQuestion question,
        DomainPackageDescriptor package,
        IEnumerable<DomainEntityResolution> entityResolutions,
        IEnumerable<Observation> observations,
        IEnumerable<EvidenceReference>? calculationEvidence = null)
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(package);
        if (package.Identity != question.Package)
        {
            throw new ArgumentException("Resolved package must exactly match the question package.", nameof(package));
        }

        ArgumentNullException.ThrowIfNull(entityResolutions);
        var entityValues = entityResolutions.ToArray();
        if (entityValues.Any(item =>
                item is null
                || item.Query.TenantId != question.TenantId
                || item.Query.Package != question.Package))
        {
            throw new ArgumentException(
                "Entity resolutions must match the question tenant and exact package.",
                nameof(entityResolutions));
        }

        ArgumentNullException.ThrowIfNull(observations);
        var observationValues = observations.ToArray();
        if (observationValues.Any(item =>
                item is null
                || item.TenantId != question.TenantId
                || item.DomainPackage != question.Package))
        {
            throw new ArgumentException(
                "Observations must match the question tenant and exact package.",
                nameof(observations));
        }

        var calculationValues = (calculationEvidence ?? []).ToArray();
        if (calculationValues.Any(item =>
                item is null
                || item.Kind != EvidenceKind.Calculation
                || item.TenantId != question.TenantId
                || item.Package != question.Package))
        {
            throw new ArgumentException(
                "Calculation evidence must match the question tenant and exact package.",
                nameof(calculationEvidence));
        }

        Question = question;
        Package = package;
        EntityResolutions = new ReadOnlyCollection<DomainEntityResolution>(entityValues);
        Observations = new ReadOnlyCollection<Observation>(observationValues);
        CalculationEvidence = new ReadOnlyCollection<EvidenceReference>(calculationValues);
    }

    public DomainQuestion Question
    {
        get;
    }

    public DomainPackageDescriptor Package
    {
        get;
    }

    public IReadOnlyList<DomainEntityResolution> EntityResolutions
    {
        get;
    }

    public IReadOnlyList<Observation> Observations
    {
        get;
    }

    public IReadOnlyList<EvidenceReference> CalculationEvidence
    {
        get;
    }
}
