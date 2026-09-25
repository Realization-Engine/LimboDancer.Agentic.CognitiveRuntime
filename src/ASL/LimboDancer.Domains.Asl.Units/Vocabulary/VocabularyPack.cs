namespace LimboDancer.Domains.Asl.Units.Vocabulary;

/// <summary>The value types an attribute may have (Unit Display Design, section 3.1).</summary>
public enum AttributeType
{
    /// <summary>A whole number, written <c>integer</c>.</summary>
    Number,

    /// <summary>Free text, written <c>text</c>.</summary>
    Text,

    /// <summary>One of the declared members, written <c>enumeration</c>.</summary>
    Enumeration,

    /// <summary>A whole number shown with its sign, such as a leadership DRM of -2, written <c>rating</c>.</summary>
    Rating,

    /// <summary>A list of whole numbers or text items, written <c>list</c>.</summary>
    List,
}

/// <summary>Whether an attribute belongs to one face of a unit or to the whole unit.</summary>
public enum AttributeScope
{
    Face,
    Unit,
}

public sealed record EnumerationMember(string Name, string Label, string Abbreviation);

/// <summary>A typed value a kind may accept. <see cref="Faces"/>, when not empty, names the faces it belongs on.</summary>
public sealed record AttributeDefinition(
    string Name,
    string Label,
    AttributeType Type,
    AttributeScope Scope,
    string? Unit,
    IReadOnlyList<EnumerationMember> Members,
    AttributeType ItemType,
    IReadOnlyList<string> Faces,
    string? Rule)
{
    public string LocalName => VocabularyNames.Local(Name);

    /// <summary>How a list's items are joined when shown, such as <c>6/7/8</c>; <c>/</c> unless the pack says otherwise.</summary>
    public string Separator
    {
        get; init;
    } = "/";

    public EnumerationMember? Member(string name) => Members.FirstOrDefault(member => member.Name == name);
}

/// <summary>A named capability that is present or absent on a face, like an HTML class.</summary>
public sealed record TraitDefinition(string Name, string Label, string? Abbreviation, IReadOnlyList<string> Faces, string? Rule);

/// <summary>
/// A condition a unit can be in, like a CSS pseudo-class. <see cref="Face"/> is the face it shows when the document has
/// it; states sharing a <see cref="Group"/> exclude each other.
/// </summary>
public sealed record StateDefinition(string Name, string Label, string? Abbreviation, string? Face, string? Group, string? Rule);

public sealed record SideDefinition(string Name, string Label);

public sealed record FaceDefinition(string Name, string Label);

/// <summary>
/// An element type. <see cref="Faces"/>, <see cref="Attributes"/>, and <see cref="Traits"/> add to what the parent
/// kind declares. The name templates build accessible names from labels and values, by face with a <c>default</c>.
/// </summary>
public sealed record KindDefinition(
    string Name,
    string Label,
    string? Extends,
    IReadOnlyList<string> Faces,
    IReadOnlyList<string> Attributes,
    IReadOnlyList<string> Traits,
    int? SizeClass,
    IReadOnlyDictionary<string, string> AccessibleName,
    IReadOnlyDictionary<string, string> AttachedName,
    string? Rule)
{
    /// <summary>Whether units of this kind face a hexspine (C3.2); null inherits the parent's answer.</summary>
    public bool? Facing
    {
        get; init;
    }

    /// <summary>Whether units of this kind may carry a turret facing apart from their hull facing (D3.12); null inherits.</summary>
    public bool? Turret
    {
        get; init;
    }
}

/// <summary>Attributes and traits a pack adds to a kind of a pack it extends.</summary>
public sealed record KindAugment(string Kind, IReadOnlyList<string> Attributes, IReadOnlyList<string> Traits);

/// <summary>
/// A versioned, namespaced vocabulary pack (ASL-UNIT-073). <see cref="Hash"/> is the SHA-256 of the pack's bytes with
/// line endings normalized, so a pack's identity changes whenever its content does.
/// </summary>
public sealed record VocabularyPack(
    string Pack,
    string Version,
    string Label,
    IReadOnlyList<string> Extends,
    IReadOnlyList<SideDefinition> Sides,
    IReadOnlyList<FaceDefinition> Faces,
    IReadOnlyList<KindDefinition> Kinds,
    IReadOnlyList<AttributeDefinition> Attributes,
    IReadOnlyList<TraitDefinition> Traits,
    IReadOnlyList<StateDefinition> States,
    IReadOnlyList<KindAugment> Augments,
    string Hash)
{
    /// <summary>The reference documents and style sheets record, such as <c>asl@1.0.0</c>.</summary>
    public string Identity => $"{Pack}@{Version}";
}

/// <summary>Helpers for namespaced names such as <c>asl:squad</c>.</summary>
public static class VocabularyNames
{
    /// <summary>The core root kind every kind extends, directly or through its parents.</summary>
    public const string RootKind = "unit";

    public static string? Namespace(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var colon = name.IndexOf(':', StringComparison.Ordinal);
        return colon < 0 ? null : name[..colon];
    }

    public static string Local(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var colon = name.IndexOf(':', StringComparison.Ordinal);
        return colon < 0 ? name : name[(colon + 1)..];
    }

    /// <summary>Whether a name is <c>pack:local</c> with a lowercase slug on both sides.</summary>
    public static bool IsQualified(string name, string pack) =>
        Namespace(name) == pack && IsSlug(Local(name));

    public static bool IsSlug(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0 || text[0] == '-' || text[^1] == '-')
        {
            return false;
        }

        foreach (var character in text)
        {
            if (!(char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-'))
            {
                return false;
            }
        }

        return true;
    }
}
