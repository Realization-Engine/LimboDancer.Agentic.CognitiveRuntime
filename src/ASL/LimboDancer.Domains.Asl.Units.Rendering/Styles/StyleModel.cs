using System.Globalization;

namespace LimboDancer.Domains.Asl.Units.Rendering.Styles;

/// <summary>The zoom tiers of Unit Display Design section 7.1, from least to most detail.</summary>
public enum DetailTier
{
    Far,
    Mid,
    Near,
}

/// <summary>One part of a declaration value: a literal, or a function such as <c>attr(firepower)</c>.</summary>
public abstract record StyleComponent;

public sealed record StringComponent(string Value) : StyleComponent
{
    public override string ToString() => "\"" + Value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}

public sealed record IdentComponent(string Name) : StyleComponent
{
    public override string ToString() => Name;
}

public sealed record NumberComponent(double Value) : StyleComponent
{
    public override string ToString() => Value.ToString("0.####", CultureInfo.InvariantCulture);
}

/// <summary>A color written <c>#rgb</c> or <c>#rrggbb</c>, kept as lowercase <c>#rrggbb</c>.</summary>
public sealed record ColorComponent(string Hex) : StyleComponent
{
    public override string ToString() => Hex;
}

/// <summary><c>attr()</c>, <c>token()</c>, <c>side()</c>, or <c>glyph()</c>, with one component per argument.</summary>
public sealed record FunctionComponent(string Name, IReadOnlyList<StyleComponent> Arguments) : StyleComponent
{
    public override string ToString() => $"{Name}({string.Join(", ", Arguments)})";
}

public sealed record Declaration(string Property, IReadOnlyList<StyleComponent> Value, int Line, int Column)
{
    public override string ToString() => $"{Property}: {string.Join(' ', Value)}";
}

/// <summary><c>[name]</c> or <c>[name=value]</c>.</summary>
public sealed record AttributeCondition(string Name, string? Value);

/// <summary>
/// One compound selector: an optional kind (null matches any unit), and the traits, attribute conditions, states,
/// <c>:concealed</c>, and <c>:face(name)</c> it requires.
/// </summary>
public sealed record CompoundSelector(
    string? Kind,
    IReadOnlyList<string> Traits,
    IReadOnlyList<AttributeCondition> Attributes,
    IReadOnlyList<string> States,
    bool Concealed,
    string? Face)
{
    /// <summary>States, traits, <c>:concealed</c>, and <c>:face()</c>: the first part of specificity.</summary>
    public int Conditions => Traits.Count + States.Count + (Concealed ? 1 : 0) + (Face is null ? 0 : 1);
}

/// <summary>
/// A full selector. <see cref="Attached"/> is set for <c>owner::attached(item)</c>: <see cref="Subject"/> then matches
/// the owner and <see cref="Attached"/> the carried item. <see cref="Slot"/> is set for <c>::slot(name)</c>.
/// </summary>
public sealed record Selector(CompoundSelector Subject, CompoundSelector? Attached, string? Slot, string Text)
{
    public override string ToString() => Text;
}

/// <summary>A rule: selectors and declarations, with the detail tier of its <c>@detail</c> block and its source order.</summary>
public sealed record StyleRule(IReadOnlyList<Selector> Selectors, IReadOnlyList<Declaration> Declarations, DetailTier? Tier, int Order);

public enum StyleDiagnosticSeverity
{
    Error,
    Warning,
}

public sealed record StyleDiagnostic(StyleDiagnosticSeverity Severity, string Message, int Line, int Column)
{
    public override string ToString() => $"{(Severity == StyleDiagnosticSeverity.Error ? "error" : "warning")} at line {Line}, column {Column}: {Message}";
}

/// <summary>
/// A parsed unit style sheet (Unit Display Design, section 3.3): named tokens, the palette set it selects by default,
/// and its rules in source order. <see cref="Hash"/> identifies the exact text.
/// </summary>
public sealed record StyleSheet(
    string Name,
    IReadOnlyDictionary<string, IReadOnlyList<StyleComponent>> Tokens,
    IReadOnlyList<StyleRule> Rules,
    string? Palette,
    IReadOnlyList<StyleDiagnostic> Warnings,
    string Hash);

/// <summary>The properties the renderer understands. Anything else is a warning and ignored (section 9).</summary>
public static class StyleProperties
{
    public static readonly IReadOnlySet<string> Known = new HashSet<string>(StringComparer.Ordinal)
    {
        // Layout.
        "face-template", "face-size", "shape", "corner-radius", "attachment-scale", "display",

        // Paint.
        "fill", "stroke", "stroke-width", "stroke-style", "opacity", "pattern",

        // Text.
        "content", "font-size", "font-weight", "color", "align",

        // Marks.
        "mark", "superscript",

        // Decoration.
        "badge", "badges", "badge-fill", "badge-color", "badge-shape", "glyph", "face",
    };
}
