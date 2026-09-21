namespace LimboDancer.Abstractions.State.Ontology;

public enum OntologyTermKind
{
    Property,
    Relation,
}

public sealed record OntologyMapping(
    OntologyTermKind Kind,
    string Predicate,
    string StorageName);
