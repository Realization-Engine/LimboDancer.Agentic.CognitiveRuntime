using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Plausibility;

/// <summary>A combination the ASL rules forbid, with the rule. It is a warning only.</summary>
public sealed record PlausibilityWarning(string Rule, string Message, string UnitId);

/// <summary>
/// The optional plausibility check (Unit Display Design, section 11). It reports combinations the ASL rules forbid and
/// never stops a unit from being drawn (principle 4). It applies only to kinds of the <c>asl</c> pack.
/// </summary>
public static class UnitPlausibility
{
    public static IReadOnlyList<PlausibilityWarning> Check(UnitDocument document, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(vocabulary);
        var warnings = new List<PlausibilityWarning>();
        Check(document, vocabulary, warnings);
        foreach (var attached in document.Attached)
        {
            Check(attached, vocabulary, warnings);
        }

        return warnings;
    }

    private static void Check(UnitDocument document, UnitVocabulary vocabulary, List<PlausibilityWarning> warnings)
    {
        if (document.Concealed || !vocabulary.HasKind(document.Kind))
        {
            return;
        }

        bool Is(string kind) => vocabulary.HasKind(kind) && vocabulary.IsA(document.Kind, kind);
        bool Has(UnitFace face, string attribute) => face.Value(attribute) is not null;

        if (Is("asl:leader") && document.Faces.Any(face => Has(face, "asl:range")))
        {
            warnings.Add(new("A1.22, p. 44", "A leader has no Normal Range.", document.Id));
        }

        if (Is("asl:personnel") && document.Face("broken") is { } broken && Has(broken, "asl:firepower"))
        {
            warnings.Add(new("A1.4, p. 45", "A broken face shows no Firepower.", document.Id));
        }

        if (Is("asl:hero") && document.Face("broken") is not null)
        {
            warnings.Add(new("A1.4, p. 45", "A hero has no broken side.", document.Id));
        }

        if (Is("asl:mg") && document.Face("front") is { } front && !Has(front, "asl:breakdown"))
        {
            warnings.Add(new("A9.7, p. 65", "This MG has no Breakdown Number. Unless the inherent B12 is meant, state it.", document.Id));
        }
    }
}
