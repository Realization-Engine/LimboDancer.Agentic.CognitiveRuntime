using LimboDancer.Domains.Asl.Units.Rendering.Styles;

namespace LimboDancer.Domains.Asl.Units.Rendering;

/// <summary>The style sheets that ship with the renderer: <c>asl-classic</c> and <c>asl-digital</c> (Unit Display Design, section 4).</summary>
public static class UnitStyles
{
    public const string Classic = "asl-classic";
    public const string Digital = "asl-digital";

    public static readonly IReadOnlyList<string> BuiltIn = [Classic, Digital];

    /// <summary>The source text of a built-in sheet.</summary>
    public static string Text(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        using var stream = typeof(UnitStyles).Assembly.GetManifestResourceStream($"Styles.{name}.uss")
            ?? throw new InvalidOperationException($"The {name} style sheet is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static StyleSheet Load(string name)
    {
        var result = StyleSheetParser.Parse(name, Text(name));
        return result.Sheet ?? throw new InvalidDataException($"The {name} style sheet does not parse: {string.Join("; ", result.Diagnostics)}");
    }
}
