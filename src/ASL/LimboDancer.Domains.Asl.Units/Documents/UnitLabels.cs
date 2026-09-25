using System.Text;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Documents;

/// <summary>One row of a unit's detail panel.</summary>
public sealed record UnitDetail(string Label, string Value);

/// <summary>
/// Names and details built from the vocabulary's labels (ASL-UNIT-078): the accessible name the SVG carries, such as
/// "German 1st Line squad A, 4-6-7, pinned, with light MG 3-6", and the detail panel rows the inspector shows.
/// </summary>
public static class UnitLabels
{
    public static string AccessibleName(UnitDocument document, UnitVocabulary vocabulary, string? face = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(vocabulary);
        if (document.Concealed)
        {
            var size = document.SizeClass is { } sizeClass ? $", size class {sizeClass}" : string.Empty;
            return Clean($"{vocabulary.SideLabel(document.Side)} concealed unit{size}");
        }

        var shown = face ?? document.ShownFace(vocabulary);
        var parts = new List<string> { Fill(vocabulary.AccessibleTemplate(document.Kind, shown), document, vocabulary, shown) };
        if (document.Facing is { } facing)
        {
            parts.Add("facing " + facing.Label());
        }

        parts.AddRange(document.States.Select(state => vocabulary.TryGetState(state, out var definition) ? definition.Label : state));
        if (document.Attached.Count > 0)
        {
            parts.Add("with " + string.Join(" and ", document.Attached.Select(attached => AttachedName(attached, vocabulary))));
        }

        return string.Join(", ", parts.Where(part => part.Length > 0));
    }

    /// <summary>How a unit is named when carried: "light MG 3-6".</summary>
    public static string AttachedName(UnitDocument document, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(vocabulary);
        var face = document.ShownFace(vocabulary);
        return Fill(vocabulary.AttachedTemplate(document.Kind, face), document, vocabulary, face);
    }

    /// <summary>The detail panel: kind, side, the shown face's values and traits, unit values, states, other faces, and attachments.</summary>
    public static IReadOnlyList<UnitDetail> Details(UnitDocument document, UnitVocabulary vocabulary, string? face = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(vocabulary);
        var rows = new List<UnitDetail>
        {
            new("Kind", document.Concealed ? "concealed unit" : vocabulary.Kind(document.Kind).Label),
        };
        if (document.Side is not null)
        {
            rows.Add(new("Side", vocabulary.SideLabel(document.Side)));
        }

        if (document.Concealed)
        {
            if (document.SizeClass is { } size)
            {
                rows.Add(new("Size class", size.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }

            return rows;
        }

        var shown = face ?? document.ShownFace(vocabulary);
        rows.Add(new("Face", vocabulary.FaceLabel(shown)));
        if (document.Facing is { } facing)
        {
            rows.Add(new("Facing", facing.Label() + " hexspine"));
        }

        if (document.Face(shown) is { } current)
        {
            rows.AddRange(current.Values.Select(Row));
            rows.AddRange(current.Traits.Select(trait => new UnitDetail("Trait", TraitText(trait, vocabulary))));
        }

        rows.AddRange(document.Unit.Select(Row));
        if (document.States.Count > 0)
        {
            rows.Add(new("States", string.Join(", ", document.States.Select(state => vocabulary.TryGetState(state, out var definition) ? definition.Label : state))));
        }

        foreach (var other in document.Faces.Where(other => other.Name != shown))
        {
            var values = other.Values.Select(value => $"{value.Attribute.Label} {Spoken(value)}")
                .Concat(other.Traits.Select(trait => TraitText(trait, vocabulary)));
            rows.Add(new($"{Capitalize(vocabulary.FaceLabel(other.Name))} face", string.Join(", ", values)));
        }

        rows.AddRange(document.Attached.Select(attached => new UnitDetail("Carries", AttachedName(attached, vocabulary))));
        return rows;
    }

    private static UnitDetail Row(UnitValue value) => new(value.Attribute.Label, Spoken(value));

    private static string Spoken(UnitValue value) => value.Attribute.Unit is { } unit ? value.Spoken + unit : value.Spoken;

    private static string TraitText(string trait, UnitVocabulary vocabulary) =>
        vocabulary.TryGetTrait(trait, out var definition)
            ? definition.Abbreviation is { } code && code != definition.Label ? $"{definition.Label} ({code})" : definition.Label
            : trait;

    private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    /// <summary>
    /// Fills a template such as "{side} {class} {kind} {identity}, {firepower}-{range}-{morale}". A word with a missing
    /// value is dropped, unless the value is optional, as in <c>{caliber}{caliber-suffix?}</c>; a comma-separated part is
    /// dropped when none of its values is present.
    /// </summary>
    internal static string Fill(string template, UnitDocument document, UnitVocabulary vocabulary, string face)
    {
        var segments = new List<string>();
        foreach (var segment in template.Split(", "))
        {
            var words = new List<string>();
            var placeholders = 0;
            var filled = 0;
            foreach (var word in segment.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var builder = new StringBuilder();
                var missing = false;
                var index = 0;
                while (index < word.Length)
                {
                    var open = word.IndexOf('{', index);
                    if (open < 0)
                    {
                        builder.Append(word, index, word.Length - index);
                        break;
                    }

                    var close = word.IndexOf('}', open);
                    if (close < 0)
                    {
                        builder.Append(word, index, word.Length - index);
                        break;
                    }

                    builder.Append(word, index, open - index);
                    var name = word[(open + 1)..close];
                    var optional = name.EndsWith('?');
                    placeholders += optional ? 0 : 1;
                    var value = Resolve(optional ? name[..^1] : name, document, vocabulary, face);
                    if (value is null)
                    {
                        missing |= !optional;
                    }
                    else
                    {
                        filled++;
                        builder.Append(value);
                    }

                    index = close + 1;
                }

                if (!missing)
                {
                    words.Add(builder.ToString());
                }
            }

            if (words.Count > 0 && (placeholders == 0 || filled > 0))
            {
                segments.Add(string.Join(' ', words));
            }
        }

        return Clean(string.Join(", ", segments));
    }

    private static string? Resolve(string name, UnitDocument document, UnitVocabulary vocabulary, string face) => name switch
    {
        "side" => document.Side is null ? null : vocabulary.SideLabel(document.Side),
        "kind" => vocabulary.Kind(document.Kind).Label,
        "id" => document.Id,
        // A name read from another face falls back to the front face, so a broken squad keeps its class and identity.
        _ => vocabulary.TryResolveAttribute(document.Kind, name, out var attribute, out _) &&
             (document.Value(face, attribute.Name) ?? document.Value("front", attribute.Name)) is { } value
            ? value.Spoken
            : null,
    };

    private static string Clean(string text) => string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
