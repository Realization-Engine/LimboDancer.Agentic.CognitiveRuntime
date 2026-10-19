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

        // A definition may itself hold references (a face holds its figures), so expand until none is left.
        for (var round = 0; round < 8 && root.Descendants().Any(IsUse); round++)
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

        if (root.Descendants().Any(IsUse))
        {
            throw new InvalidOperationException("References remain after expansion: a definition refers to itself.");
        }

        root.Descendants().Where(element => element.Name.LocalName == "defs").Remove();
        return root;
    }

    /// <summary>The expanded layer as text, for comparison.</summary>
    public static string Expanded(string svg, IEnumerable<XElement>? outside = null) => Expand(svg, outside).ToString(SaveOptions.DisableFormatting);

    private static bool IsUse(XElement element) => element.Name.LocalName == "use";

    private static double Number(string? text) => text is null ? 0 : double.Parse(text, CultureInfo.InvariantCulture);
}
