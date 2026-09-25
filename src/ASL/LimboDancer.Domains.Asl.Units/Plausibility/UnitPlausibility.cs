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

    /// <summary>The eleven Armor Factors of D1.6, p. 194.</summary>
    private static readonly int[] ArmorFactors = [0, 1, 2, 3, 4, 6, 8, 11, 14, 18, 26];

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

        if (Is("asl:vehicle") && document.TurretFacing is not null && document.Faces.Any(face => face.Value("asl:ma-type") is { Text: "nt" }))
        {
            warnings.Add(new("D3.12, p. 199", "A non-turreted MA has no turret facing.", document.Id));
        }

        foreach (var face in document.Faces)
        {
            if (Is("asl:vehicle"))
            {
                foreach (var armor in new[] { "asl:af-front", "asl:af-side" })
                {
                    if (face.Value(armor) is { Number: { } value } && !ArmorFactors.Contains(value))
                    {
                        warnings.Add(new("D1.6, p. 194", $"{value} is not one of the eleven Armor Factors.", document.Id));
                    }
                }

                if (face.HasTrait("asl:unarmored") && (Has(face, "asl:af-front") || Has(face, "asl:af-side") || face.HasTrait("asl:partially-armored")))
                {
                    warnings.Add(new("D1.21, p. 193", $"The {face.Name} face is unarmored but also has armor.", document.Id));
                }
            }

            if (face.HasTrait("asl:no-ap") && face.HasTrait("asl:no-he"))
            {
                warnings.Add(new("C2.21, p. 167", $"The {face.Name} face can fire neither AP nor HE.", document.Id));
            }

            if (Is("asl:gun") && Has(face, "asl:ife") && face.Value("asl:rate-of-fire") is not { Number: > 1 })
            {
                warnings.Add(new("C2.29, p. 168", $"The {face.Name} face has an IFE but no Multiple ROF; IFE is for Guns with a high ROF.", document.Id));
            }

            if (face.Value("asl:range-minimum") is { Number: { } minimum } && face.Value("asl:range-maximum") is { Number: { } maximum } && minimum > maximum)
            {
                warnings.Add(new("C2.25, p. 168", $"The {face.Name} face's minimum range exceeds its maximum.", document.Id));
            }
        }
    }
}
