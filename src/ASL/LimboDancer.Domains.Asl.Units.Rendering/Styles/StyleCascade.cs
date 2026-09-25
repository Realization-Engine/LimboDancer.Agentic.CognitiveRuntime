using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Rendering.Styles;

/// <summary>What a selector is matched against: a document shown with one face.</summary>
public sealed record StyleSubject(UnitDocument Document, string Face)
{
    public UnitFace? FaceContent => Document.Face(Face);
}

/// <summary>
/// Where a winning declaration stands in the cascade: rules in a <c>@detail</c> block for the current tier outrank all
/// other rules; then specificity (states and traits, then attributes, then kind depth); then source order.
/// </summary>
public readonly record struct CascadeRank(int Layer, int Conditions, int Attributes, int Depth, int Order, int Index) : IComparable<CascadeRank>
{
    public int CompareTo(CascadeRank other)
    {
        var comparison = Layer.CompareTo(other.Layer);
        if (comparison == 0)
        {
            comparison = Conditions.CompareTo(other.Conditions);
        }

        if (comparison == 0)
        {
            comparison = Attributes.CompareTo(other.Attributes);
        }

        if (comparison == 0)
        {
            comparison = Depth.CompareTo(other.Depth);
        }

        if (comparison == 0)
        {
            comparison = Order.CompareTo(other.Order);
        }

        return comparison == 0 ? Index.CompareTo(other.Index) : comparison;
    }

    public static bool operator <(CascadeRank left, CascadeRank right) => left.CompareTo(right) < 0;

    public static bool operator >(CascadeRank left, CascadeRank right) => left.CompareTo(right) > 0;

    public static bool operator <=(CascadeRank left, CascadeRank right) => left.CompareTo(right) <= 0;

    public static bool operator >=(CascadeRank left, CascadeRank right) => left.CompareTo(right) >= 0;
}

/// <summary>
/// The declarations that apply to one face or one slot at one detail tier. Each property keeps its winning value; the
/// <c>badge</c> property is additive, so every matching badge declaration contributes, in cascade order.
/// </summary>
public sealed class ComputedStyle
{
    private readonly Dictionary<string, (IReadOnlyList<StyleComponent> Value, CascadeRank Rank, Declaration Declaration)> values = new(StringComparer.Ordinal);
    private readonly List<(IReadOnlyList<StyleComponent> Value, CascadeRank Rank)> badges = [];

    public IReadOnlyList<StyleComponent>? this[string property] => values.TryGetValue(property, out var entry) ? entry.Value : null;

    /// <summary>Badge declarations in cascade order.</summary>
    public IReadOnlyList<IReadOnlyList<StyleComponent>> Badges => [.. badges.OrderBy(badge => badge.Rank).Select(badge => badge.Value)];

    public IEnumerable<string> Properties => values.Keys.Order(StringComparer.Ordinal);

    /// <summary>The declaration that won a property, for diagnostics in the Lab.</summary>
    public Declaration? Winner(string property) => values.TryGetValue(property, out var entry) ? entry.Declaration : null;

    internal void Offer(Declaration declaration, CascadeRank rank)
    {
        if (declaration.Property == "badge")
        {
            badges.Add((declaration.Value, rank));
            return;
        }

        if (!values.TryGetValue(declaration.Property, out var existing) || rank > existing.Rank)
        {
            values[declaration.Property] = (declaration.Value, rank, declaration);
        }
    }
}

/// <summary>
/// The cascade of Unit Display Design section 3.3. Values do not inherit between units; within a unit, a slot's text
/// color and weight fall back to the face's (see <c>UnitRenderer</c>).
/// </summary>
public static class StyleCascade
{
    /// <summary>
    /// The style of a subject's face (<paramref name="slot"/> null) or of one slot, at a tier. For carried equipment,
    /// <paramref name="owner"/> is the owner, and <c>owner::attached(item)</c> rules also apply.
    /// </summary>
    public static ComputedStyle Compute(StyleSheet sheet, UnitVocabulary vocabulary, StyleSubject subject, StyleSubject? owner, string? slot, DetailTier tier)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(vocabulary);
        ArgumentNullException.ThrowIfNull(subject);
        var style = new ComputedStyle();
        foreach (var rule in sheet.Rules)
        {
            if (rule.Tier is { } ruleTier && ruleTier != tier)
            {
                continue;
            }

            CascadeRank? best = null;
            foreach (var selector in rule.Selectors)
            {
                if (Rank(selector, vocabulary, subject, owner, slot, rule) is { } rank && (best is null || rank > best.Value))
                {
                    best = rank;
                }
            }

            if (best is not { } matched)
            {
                continue;
            }

            for (var index = 0; index < rule.Declarations.Count; index++)
            {
                style.Offer(rule.Declarations[index], matched with
                {
                    Index = index
                });
            }
        }

        return style;
    }

    /// <summary>Whether a selector matches, and its rank when it does.</summary>
    public static CascadeRank? Rank(Selector selector, UnitVocabulary vocabulary, StyleSubject subject, StyleSubject? owner, string? slot, StyleRule rule)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(rule);
        if (selector.Slot != slot)
        {
            return null;
        }

        var layer = rule.Tier is null ? 0 : 1;
        if (selector.Attached is { } attached)
        {
            if (owner is null || !Matches(selector.Subject, vocabulary, owner) || !Matches(attached, vocabulary, subject))
            {
                return null;
            }

            return new CascadeRank(layer, selector.Subject.Conditions + attached.Conditions, selector.Subject.Attributes.Count + attached.Attributes.Count,
                Depth(selector.Subject, vocabulary) + Depth(attached, vocabulary), rule.Order, 0);
        }

        return Matches(selector.Subject, vocabulary, subject)
            ? new CascadeRank(layer, selector.Subject.Conditions, selector.Subject.Attributes.Count, Depth(selector.Subject, vocabulary), rule.Order, 0)
            : null;
    }

    public static bool Matches(CompoundSelector selector, UnitVocabulary vocabulary, StyleSubject subject)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(vocabulary);
        ArgumentNullException.ThrowIfNull(subject);
        var document = subject.Document;
        if (selector.Kind is { } kind && !(vocabulary.HasKind(kind) && vocabulary.HasKind(document.Kind) && vocabulary.IsA(document.Kind, kind)))
        {
            return false;
        }

        if (selector.Concealed && !document.Concealed)
        {
            return false;
        }

        if (selector.Face is { } face && (document.Concealed || subject.Face != face))
        {
            return false;
        }

        var faceContent = subject.FaceContent;
        if (selector.Traits.Any(trait => faceContent is null || !faceContent.HasTrait(trait)))
        {
            return false;
        }

        if (selector.States.Any(state => !document.HasState(state)))
        {
            return false;
        }

        foreach (var condition in selector.Attributes)
        {
            var value = AttributeText(condition.Name, vocabulary, subject);
            if (value is null || (condition.Value is not null && value != condition.Value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The text an attribute selector compares: <c>side</c>, <c>id</c>, <c>kind</c>, or an attribute's value.</summary>
    public static string? AttributeText(string name, UnitVocabulary vocabulary, StyleSubject subject)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        ArgumentNullException.ThrowIfNull(subject);
        var document = subject.Document;
        return name switch
        {
            "side" => document.Side,
            "id" => document.Id,
            "kind" => document.Kind,
            _ => vocabulary.HasKind(document.Kind) && vocabulary.TryResolveAttribute(document.Kind, name, out var attribute, out _)
                ? document.Value(subject.Face, attribute.Name)?.SelectorText
                : null,
        };
    }

    private static int Depth(CompoundSelector selector, UnitVocabulary vocabulary) =>
        selector.Kind is { } kind && vocabulary.HasKind(kind) ? vocabulary.Depth(kind) : 0;
}
