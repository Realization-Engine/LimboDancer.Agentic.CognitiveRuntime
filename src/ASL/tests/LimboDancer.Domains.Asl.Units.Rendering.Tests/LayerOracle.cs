using System.Globalization;
using System.Xml.Linq;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>
/// The equivalence oracle of the Counter Layer Design (section 11): a counter layer with every reference expanded, so a layer that shares its
/// definitions (figure outlines, then face definitions) can be compared element by element with one that draws every counter whole. Each
/// <c>&lt;use&gt;</c> becomes a <c>&lt;g&gt;</c> with the use's own attributes (its <c>x</c> and <c>y</c> as a translation) around a copy of what it
/// refers to, without the copy's id; every <c>&lt;defs&gt;</c> is then dropped. A reference that finds nothing is an error.
/// </summary>
internal static class LayerOracle
{
    public static XElement Expand(string svg, IEnumerable<XElement>? outside = null)
    {
        var root = XElement.Parse(svg);
        var definitions = new Dictionary<string, XElement>(StringComparer.Ordinal);
        foreach (var element in root.DescendantsAndSelf().Concat(outside?.SelectMany(item => item.DescendantsAndSelf()) ?? [])
                     .Where(element => element.Attribute("id") is not null && element.Ancestors().Any(ancestor => ancestor.Name.LocalName == "defs")))
        {
            definitions[element.Attribute("id")!.Value] = element;
        }

        // A definition may itself hold references (a face holds its figures), so expand until none is left outside the definitions.
        for (var round = 0; round < 8 && root.Descendants().Any(Drawn); round++)
        {
            foreach (var use in root.Descendants().Where(IsUse).ToList())
            {
                if (use.Ancestors().Any(ancestor => ancestor.Name.LocalName == "defs"))
                {
                    continue;
                }

                var href = (use.Attribute("href") ?? use.Attributes().First(attribute => attribute.Name.LocalName == "href")).Value;
                if (!href.StartsWith('#') || !definitions.TryGetValue(href[1..], out var target))
                {
                    throw new InvalidOperationException($"The reference {href} finds no definition.");
                }

                var copy = new XElement(target);
                copy.SetAttributeValue("id", null);
                var x = Number(use.Attribute("x")?.Value);
                var y = Number(use.Attribute("y")?.Value);
                var transform = use.Attribute("transform")?.Value;
                if (x != 0 || y != 0)
                {
                    transform = string.Create(CultureInfo.InvariantCulture, $"translate({x} {y})") + (transform is null ? string.Empty : " " + transform);
                }

                var group = new XElement("g", use.Attributes().Where(attribute => attribute.Name.LocalName is not ("href" or "x" or "y" or "transform")), copy);
                if (transform is not null)
                {
                    group.SetAttributeValue("transform", transform);
                }

                use.ReplaceWith(group);
            }
        }

        if (root.Descendants().Any(Drawn))
        {
            throw new InvalidOperationException("References remain after expansion: a definition refers to itself.");
        }

        root.Descendants().Where(element => element.Name.LocalName == "defs").Remove();
        return root;
    }

    /// <summary>The expanded layer as text, for comparison.</summary>
    public static string Expanded(string svg, IEnumerable<XElement>? outside = null) => Expand(svg, outside).ToString(SaveOptions.DisableFormatting);

    /// <summary>An overlay's layer expanded with the figure sprite it refers to (option C2), as the viewport holds them together.</summary>
    public static string Expanded(UnitOverlay overlay) => Expanded(overlay.Svg, overlay.Figures is { } sprite ? [XElement.Parse(sprite)] : null);

    /// <summary>
    /// Whether a layer whose faces are drawn at the origin and moved into place (option Q's form) draws what the layer of whole counters draws:
    /// the same elements and texts, every number within <paramref name="tolerance"/> once each face's move is added to its absolute coordinates
    /// (section 8: the position and the offset are rounded apart, so a number may differ by 1/64 pixel). Null when they agree; else where not.
    /// </summary>
    public static string? Differs(XElement whole, XElement atOrigin, double tolerance = 1 / 32.0) => Compare(whole, atOrigin, 0, 0, tolerance, "layer");

    private static readonly System.Text.RegularExpressions.Regex Tokens = new(@"[A-Za-z]|-?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex TransformFunctions = new(@"(\w+)\(([^)]*)\)", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex FocusStyle =
        new(@"^stroke: var\(--unit-focus-stroke, ([^)]+)\); stroke-width: var\(--unit-focus-width, ([^)]+)\)$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string? Compare(XElement whole, XElement moved, double dx, double dy, double tolerance, string at)
    {
        if (whole.Name.LocalName != moved.Name.LocalName)
        {
            return $"{at}: <{whole.Name.LocalName}> against <{moved.Name.LocalName}>";
        }

        // A tier's group: the face inside is drawn at the origin and moved by the group's translation.
        if (moved.Attribute("data-tier") is not null && moved.Attribute("transform")?.Value is { } move && move.StartsWith("translate(", StringComparison.Ordinal))
        {
            var offset = Numbers(move);
            var face = moved.Elements().SingleOrDefault();
            if (face?.Attribute("data-face-def") is null)
            {
                return $"{at}: a tier's group holds no face definition";
            }

            return Children(whole, face, offset[0], offset[1], tolerance, at + "/" + moved.Attribute("data-tier")!.Value);
        }

        var left = Attributes(whole);
        var right = Attributes(moved);
        if (!left.Keys.Order(StringComparer.Ordinal).SequenceEqual(right.Keys.Order(StringComparer.Ordinal)))
        {
            return $"{at}: attributes {string.Join(",", left.Keys)} against {string.Join(",", right.Keys)}";
        }

        foreach (var (name, value) in left)
        {
            if (!SameValue(name, value, right[name], dx, dy, tolerance))
            {
                return $"{at}: {name}=\"{value}\" against \"{right[name]}\" moved by ({dx}, {dy})";
            }
        }

        if (!whole.HasElements && whole.Value != moved.Value)
        {
            return $"{at}: text \"{whole.Value}\" against \"{moved.Value}\"";
        }

        // Inside a group that moves its content, coordinates are its own, so the face's move no longer applies; a rotation about a point does not.
        var local = whole.Attribute("transform")?.Value is { } transform && (transform.Contains("translate", StringComparison.Ordinal)
            || transform.Contains("scale", StringComparison.Ordinal) || transform.Contains("matrix", StringComparison.Ordinal));
        return Children(whole, moved, local ? 0 : dx, local ? 0 : dy, tolerance, at + "/" + whole.Name.LocalName);
    }

    private static string? Children(XElement whole, XElement moved, double dx, double dy, double tolerance, string at)
    {
        var (a, b) = (whole.Elements().ToArray(), moved.Elements().ToArray());
        if (a.Length != b.Length)
        {
            return $"{at}: {a.Length} children against {b.Length}";
        }

        for (var index = 0; index < a.Length; index++)
        {
            if (Compare(a[index], b[index], dx, dy, tolerance, $"{at}[{index}]") is { } difference)
            {
                return difference;
            }
        }

        return null;
    }

    // The attributes compared: a whole counter's attachment ids and a moved face's focus style are written as the other form writes them.
    private static Dictionary<string, string> Attributes(XElement element)
    {
        var attributes = element.Attributes().Where(attribute => attribute.Name.LocalName is not ("data-attached-id" or "style"))
            .ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value, StringComparer.Ordinal);
        if (element.Attribute("style")?.Value is { } style)
        {
            if (FocusStyle.Match(style) is { Success: true } focus)
            {
                attributes["stroke"] = focus.Groups[1].Value;
                attributes["stroke-width"] = focus.Groups[2].Value;
            }
            else
            {
                attributes["style"] = style;
            }
        }

        return attributes;
    }

    // The same text with no move to add is the same value under any tolerance: most of a layer's paths are compared this way, so they skip
    // the tokenizing that dominated the oracle's time.
    private static bool SameValue(string name, string whole, string moved, double dx, double dy, double tolerance) =>
        (dx == 0 && dy == 0 && string.Equals(whole, moved, StringComparison.Ordinal)) || SameMovedValue(name, whole, moved, dx, dy, tolerance);

    private static bool SameMovedValue(string name, string whole, string moved, double dx, double dy, double tolerance) => name switch
    {
        "x" or "cx" or "x1" or "x2" => Near(Numbers(whole), Numbers(moved), [dx], tolerance),
        "y" or "cy" or "y1" or "y2" => Near(Numbers(whole), Numbers(moved), [dy], tolerance),
        "points" => Near(Numbers(whole), Numbers(moved), [dx, dy], tolerance),
        "d" => SamePath(whole, moved, dx, dy, tolerance),
        "transform" => SameTransform(whole, moved, dx, dy, tolerance),
        _ => whole == moved || (Numbers(whole) is { Length: > 0 } a && Numbers(moved) is var b && Near(a, b, [0], tolerance) && Words(whole) == Words(moved)),
    };

    private static bool Near(double[] whole, double[] moved, double[] shift, double tolerance) =>
        whole.Length == moved.Length && whole.Select((value, index) => Math.Abs(value - (moved[index] + shift[index % shift.Length]))).All(gap => gap <= tolerance);

    // A path's absolute coordinates are moved: pairs for M, L, T, C, S, Q; x for H; y for V; the end point of an arc. Relative commands are not.
    private static bool SamePath(string whole, string moved, double dx, double dy, double tolerance)
    {
        var (a, b) = (Tokens.Matches(whole).Select(match => match.Value).ToArray(), Tokens.Matches(moved).Select(match => match.Value).ToArray());
        if (a.Length != b.Length)
        {
            return false;
        }

        var (command, index) = ('M', 0);
        for (var at = 0; at < a.Length; at++)
        {
            if (char.IsLetter(a[at][0]))
            {
                if (a[at] != b[at])
                {
                    return false;
                }

                (command, index) = (a[at][0], 0);
                continue;
            }

            var shift = command switch
            {
                'M' or 'L' or 'T' or 'C' or 'S' or 'Q' => index % 2 == 0 ? dx : dy,
                'H' => dx,
                'V' => dy,
                'A' => (index % 7) switch { 5 => dx, 6 => dy, _ => 0 },
                _ => 0,
            };
            index++;
            if (Math.Abs(Parse(a[at]) - (Parse(b[at]) + shift)) > tolerance)
            {
                return false;
            }
        }

        return true;
    }

    // A translation is moved, as is a rotation's centre; a rotation's angle and a scale are not.
    private static bool SameTransform(string whole, string moved, double dx, double dy, double tolerance)
    {
        var (a, b) = (TransformFunctions.Matches(whole).ToArray(), TransformFunctions.Matches(moved).ToArray());
        return a.Length == b.Length && a.Zip(b).All(pair => pair.First.Groups[1].Value == pair.Second.Groups[1].Value && pair.First.Groups[1].Value switch
        {
            "translate" => Near(Numbers(pair.First.Groups[2].Value), Numbers(pair.Second.Groups[2].Value), [dx, dy], tolerance),
            "rotate" => Near(Numbers(pair.First.Groups[2].Value), Numbers(pair.Second.Groups[2].Value), [0, dx, dy], tolerance),
            _ => Near(Numbers(pair.First.Groups[2].Value), Numbers(pair.Second.Groups[2].Value), [0], tolerance),
        });
    }

    private static double[] Numbers(string text) => [.. Tokens.Matches(text).Where(match => !char.IsLetter(match.Value[0])).Select(match => Parse(match.Value))];

    private static string Words(string text) => Tokens.Replace(text, match => char.IsLetter(match.Value[0]) ? match.Value : "#");

    private static double Parse(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static bool IsUse(XElement element) => element.Name.LocalName == "use";

    // A reference that is drawn: one outside every definition.
    private static bool Drawn(XElement element) => IsUse(element) && element.Ancestors().All(ancestor => ancestor.Name.LocalName != "defs");

    private static double Number(string? text) => text is null ? 0 : double.Parse(text, CultureInfo.InvariantCulture);
}
