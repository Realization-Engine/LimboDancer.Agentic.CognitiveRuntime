using System.Globalization;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Documents;

/// <summary>A typed attribute value, already checked against its definition.</summary>
public sealed record UnitValue(AttributeDefinition Attribute, int? Number, string? Text, IReadOnlyList<string> Items)
{
    public static UnitValue Of(AttributeDefinition attribute, int number) => new(attribute, number, null, []);

    public static UnitValue Of(AttributeDefinition attribute, string text) => new(attribute, null, text, []);

    public static UnitValue Of(AttributeDefinition attribute, IReadOnlyList<string> items) => new(attribute, null, null, items);

    /// <summary>The value as a face shows it: numbers, signed ratings, a member's short form, a list joined with slashes.</summary>
    public string Display => Attribute.Type switch
    {
        AttributeType.Number => Number!.Value.ToString(CultureInfo.InvariantCulture),
        AttributeType.Rating => Signed(Number!.Value),
        AttributeType.Enumeration => Attribute.Member(Text!)?.Abbreviation ?? Text!,
        AttributeType.List => string.Join(Attribute.Separator, Items),
        _ => Text ?? string.Empty,
    };

    /// <summary>The value as an accessible name or detail panel reads it: a member's label rather than its short form.</summary>
    public string Spoken => Attribute.Type == AttributeType.Enumeration ? Attribute.Member(Text!)?.Label ?? Text! : Display;

    /// <summary>The text a selector such as <c>[class=elite]</c> compares: the member name, or the displayed value.</summary>
    public string SelectorText => Attribute.Type == AttributeType.Enumeration ? Text! : Display;

    private static string Signed(int value) => value > 0
        ? "+" + value.ToString(CultureInfo.InvariantCulture)
        : value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>One face of a unit: its values and the traits present on it.</summary>
public sealed record UnitFace(string Name, IReadOnlyList<UnitValue> Values, IReadOnlyList<string> Traits)
{
    public UnitValue? Value(string attribute) => Values.FirstOrDefault(value => value.Attribute.Name == attribute);

    public bool HasTrait(string trait) => Traits.Contains(trait, StringComparer.Ordinal);
}

/// <summary>
/// A displayed unit (Unit Display Design, section 3.2): a projection of what its viewer may know, in the vocabulary's
/// terms. It never says how anything looks (ASL-UNIT-074). A concealed placeholder (section 7.3) carries only its id,
/// kind, side, location, optional size class, and stack order.
/// </summary>
public sealed record UnitDocument(
    IReadOnlyList<string> Vocabulary,
    string Id,
    string Kind,
    string? Side,
    string? Location,
    IReadOnlyList<UnitFace> Faces,
    IReadOnlyList<UnitValue> Unit,
    IReadOnlyList<string> States,
    IReadOnlyList<UnitDocument> Attached,
    int StackOrder = 0,
    bool Concealed = false,
    int? SizeClass = null,
    UnitFacing? Facing = null,
    UnitFacing? TurretFacing = null,
    UnitHexside? Hexside = null)
{
    public UnitFace? Face(string name) => Faces.FirstOrDefault(face => face.Name == name);

    public bool HasState(string state) => States.Contains(state, StringComparer.Ordinal);

    /// <summary>A value on a face, else on the unit.</summary>
    public UnitValue? Value(string face, string attribute) =>
        Face(face)?.Value(attribute) ?? Unit.FirstOrDefault(value => value.Attribute.Name == attribute);

    /// <summary>
    /// The face the document's states select: the face of the first state, in vocabulary order, that switches to a face
    /// this document has; otherwise <c>front</c>, or the first face the document has.
    /// </summary>
    public string ShownFace(UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        foreach (var state in vocabulary.States)
        {
            if (state.Face is { } face && HasState(state.Name) && Face(face) is not null)
            {
                return face;
            }
        }

        return Face("front") is not null || Faces.Count == 0 ? "front" : Faces[0].Name;
    }

    /// <summary>The number of figures the size glyph shows: the document's size class, else the kind's.</summary>
    public int? Figures(UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        return SizeClass ?? (vocabulary.HasKind(Kind) ? vocabulary.SizeClass(Kind) : null);
    }
}
