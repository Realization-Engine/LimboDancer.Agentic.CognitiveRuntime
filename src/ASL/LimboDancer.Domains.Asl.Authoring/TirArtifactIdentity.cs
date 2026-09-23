namespace LimboDancer.Domains.Asl.Authoring;

public sealed record TirArtifactIdentityInput(
    TirArtifactKind ArtifactKind,
    string SourceRegistryId,
    string? PublishedId,
    string? NormalizedPublishedId,
    IReadOnlyList<string> SourceFragmentIds,
    string Disambiguator);

public static class TirArtifactIdentity
{
    public const string Profile = "asl-tir-artifact-id/v1";

    public static string Create(TirArtifactIdentityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.SourceRegistryId);
        ArgumentNullException.ThrowIfNull(input.SourceFragmentIds);

        if (input.SourceFragmentIds.Count == 0
            || input.SourceFragmentIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one non-empty source fragment identity is required.", nameof(input));
        }

        var structuralIdentity = input.NormalizedPublishedId ?? input.PublishedId ?? string.Empty;
        var material = string.Join(
            '\n',
            Profile,
            TirValues.ArtifactKind(input.ArtifactKind),
            input.SourceRegistryId,
            structuralIdentity,
            string.Join('\n', input.SourceFragmentIds),
            input.Disambiguator);
        return $"asl-tir:sha256:{Hashing.Sha256Text(material)}";
    }
}

internal static class TirValues
{
    public static string ArtifactKind(TirArtifactKind value) => value switch
    {
        TirArtifactKind.SourceFragment => "sourceFragment",
        TirArtifactKind.Rule => "rule",
        TirArtifactKind.Definition => "definition",
        TirArtifactKind.Condition => "condition",
        TirArtifactKind.Effect => "effect",
        TirArtifactKind.Exception => "exception",
        TirArtifactKind.CrossReference => "crossReference",
        TirArtifactKind.Example => "example",
        TirArtifactKind.Table => "table",
        TirArtifactKind.PhaseRestriction => "phaseRestriction",
        TirArtifactKind.Term => "term",
        TirArtifactKind.EntityType => "entityType",
        TirArtifactKind.Property => "property",
        TirArtifactKind.Relation => "relation",
        TirArtifactKind.Enumeration => "enumeration",
        TirArtifactKind.ValidationShape => "validationShape",
        TirArtifactKind.CalculationRequirement => "calculationRequirement",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
