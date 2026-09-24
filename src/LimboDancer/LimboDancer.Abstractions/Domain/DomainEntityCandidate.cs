namespace LimboDancer.Abstractions.Domain;

public sealed class DomainEntityCandidate
{
    public DomainEntityCandidate(
        SemanticIdentifier identity,
        CanonicalReference canonicalReference,
        EvidenceReference evidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.Value);
        ArgumentNullException.ThrowIfNull(canonicalReference);
        ArgumentNullException.ThrowIfNull(evidence);
        if (identity.DomainId != canonicalReference.Package.DomainId
            || evidence.Package != canonicalReference.Package)
        {
            throw new ArgumentException("Entity identity, canonical reference, and evidence must share one package domain.");
        }

        Identity = identity;
        CanonicalReference = canonicalReference;
        Evidence = evidence;
    }

    public SemanticIdentifier Identity
    {
        get;
    }

    public CanonicalReference CanonicalReference
    {
        get;
    }

    public EvidenceReference Evidence
    {
        get;
    }
}
