using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering.Palettes;
using LimboDancer.Domains.Asl.Units.Rendering.Styles;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Rendering;

/// <summary>A drawing warning, such as a slot naming an attribute the unit does not have. Drawing always continues.</summary>
public sealed record RenderWarning(string UnitId, string Message);

/// <summary>
/// Lays out unit documents under a style sheet and writes SVG (Unit Display Design, section 7). All lengths in a sheet
/// are fractions: <c>face-size</c> of the hex height, and every other length of the face size. Output depends only on
/// the vocabulary, document, sheet, palette, tier, and position, and numbers go through <see cref="SvgWriter"/>
/// (ASL-UNIT-078).
/// </summary>
public sealed class UnitRenderer
{
    public const string Version = "1.0.0";
    public const double DefaultFaceSize = 0.55;
    public const double DefaultAttachmentScale = 0.6;

    private static readonly string[] BadgePositions = ["top-left", "top-right", "bottom-left", "bottom-right"];

    public UnitRenderer(UnitVocabulary vocabulary, StyleSheet sheet, PaletteSet palette)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(palette);
        Vocabulary = vocabulary;
        Sheet = sheet;
        Palette = palette;
    }

    public UnitVocabulary Vocabulary
    {
        get;
    }

    public StyleSheet Sheet
    {
        get;
    }

    public PaletteSet Palette
    {
        get;
    }

    /// <summary>The face side in board pixels for a hex height.</summary>
    public double FaceSize(UnitDocument document, DetailTier tier, double hexHeight)
    {
        ArgumentNullException.ThrowIfNull(document);
        var (subject, style) = FaceStyle(document, null, tier, null);
        var evaluator = new Evaluator(this, subject, document.Side, null, null);
        var fraction = evaluator.Number(style["face-size"]) ?? DefaultFaceSize;
        return hexHeight * Math.Clamp(fraction, 0.05, 3);
    }

    /// <summary>
    /// Writes one unit as an interactive group: <c>data-unit-id</c>, <c>role="button"</c>, <c>tabindex="0"</c>, an
    /// accessible name, and one <c>data-tier</c> group per requested tier, centered on the point.
    /// </summary>
    public void WriteUnit(SvgWriter svg, UnitDocument document, double centerX, double centerY, double hexHeight, IReadOnlyList<DetailTier> tiers,
        string? face, string? nameSuffix, List<RenderWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(svg);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(tiers);
        ArgumentNullException.ThrowIfNull(warnings);
        var name = UnitLabels.AccessibleName(document, Vocabulary, face) + nameSuffix;
        svg.Start("g", ("data-unit-id", document.Id), ("role", "button"), ("tabindex", "0"), ("aria-label", name));
        svg.Start("title");
        svg.Text(name);
        svg.End();
        foreach (var tier in tiers)
        {
            svg.Start("g", ("data-tier", TierName(tier)));
            WriteFace(svg, document, null, centerX, centerY, FaceSize(document, tier, hexHeight), tier, face, warnings);
            svg.End();
        }

        svg.End();
    }

    public static string TierName(DetailTier tier) => tier switch
    {
        DetailTier.Far => "far",
        DetailTier.Mid => "mid",
        _ => "near",
    };

    /// <summary>The face a document shows: a forced face, else its states' face, else a face the sheet's <c>face</c> property names.</summary>
    internal (StyleSubject Subject, ComputedStyle Style) FaceStyle(UnitDocument document, StyleSubject? owner, DetailTier tier, string? forcedFace)
    {
        var shown = forcedFace ?? document.ShownFace(Vocabulary);
        var subject = new StyleSubject(document, shown);
        var style = StyleCascade.Compute(Sheet, Vocabulary, subject, owner, null, tier);
        if (forcedFace is null && !document.Concealed && style["face"] is [IdentComponent { Name: var named }] && named != shown && document.Face(named) is not null)
        {
            subject = new StyleSubject(document, named);
            style = StyleCascade.Compute(Sheet, Vocabulary, subject, owner, null, tier);
        }

        return (subject, style);
    }

    private void WriteFace(SvgWriter svg, UnitDocument document, StyleSubject? owner, double centerX, double centerY, double size, DetailTier tier,
        string? forcedFace, List<RenderWarning> warnings)
    {
        var (subject, style) = FaceStyle(document, owner, tier, forcedFace);
        var side = document.Side ?? owner?.Document.Side;
        var evaluator = new Evaluator(this, subject, side, warnings, null);
        if (evaluator.Ident(style["display"]) == "none")
        {
            return;
        }

        // Carried equipment is tucked under the owner's lower edge, so it is drawn first.
        if (owner is null && document.Attached.Count > 0)
        {
            var scale = evaluator.Number(style["attachment-scale"]) ?? DefaultAttachmentScale;
            var attachedSize = size * Math.Clamp(scale, 0.2, 1);
            var count = document.Attached.Count;
            for (var index = 0; index < count; index++)
            {
                var attached = document.Attached[index];
                var x = centerX + (size * 0.18) + ((index - ((count - 1) / 2.0)) * attachedSize * 0.92);
                var y = centerY + (size / 2) + (attachedSize * 0.18);
                svg.Start("g", ("data-attached-id", attached.Id));
                WriteFace(svg, attached, subject, x, y, attachedSize, tier, null, warnings);
                svg.End();
            }
        }

        var x0 = centerX - (size / 2);
        var y0 = centerY - (size / 2);
        var ink = Palette.For(side).Ink;
        var fill = evaluator.Color(style["fill"]) ?? Palette.For(side).Fill;
        var stroke = evaluator.Color(style["stroke"]) ?? ink;
        var strokeWidth = size * (evaluator.Number(style["stroke-width"]) ?? 0.03);
        var opacity = evaluator.Number(style["opacity"]);
        var shapeAttributes = new List<(string, string)>();
        var shape = evaluator.Ident(style["shape"]) ?? "square";
        if (shape == "circle")
        {
            shapeAttributes.AddRange([("cx", N(centerX)), ("cy", N(centerY)), ("r", N(size / 2))]);
        }
        else
        {
            var radius = size * (evaluator.Number(style["corner-radius"]) ?? 0.08);
            shapeAttributes.AddRange([("x", N(x0)), ("y", N(y0)), ("width", N(size)), ("height", N(size)), ("rx", N(radius))]);
        }

        shapeAttributes.AddRange([("fill", fill), ("stroke", stroke), ("stroke-width", N(strokeWidth))]);
        if (evaluator.Ident(style["stroke-style"]) is "dashed")
        {
            shapeAttributes.Add(("stroke-dasharray", $"{N(strokeWidth * 2)} {N(strokeWidth * 1.2)}"));
        }

        if (opacity is { } alpha)
        {
            shapeAttributes.Add(("opacity", N(Math.Clamp(alpha, 0, 1))));
        }

        shapeAttributes.Add(("data-face", subject.Face));
        svg.Empty(shape == "circle" ? "circle" : "rect", [.. shapeAttributes]);

        var inner = new Box(x0 + (size * 0.07), y0 + (size * 0.07), size * 0.86, size * 0.86);
        if (style["pattern"] is { } pattern && evaluator.Ident([pattern[0]]) == "stripes")
        {
            var band = size * 0.14;
            var color = pattern.Count > 1 ? evaluator.Color(pattern.Skip(1).ToArray()) ?? "#e0a800" : "#e0a800";
            Stripes(svg, new Box(x0 + (strokeWidth / 2), y0 + size - band - (strokeWidth / 2), size - strokeWidth, band), color, ink);
            inner = inner with
            {
                Height = inner.Height - band + (size * 0.03)
            };
        }

        var textColor = evaluator.Color(style["color"]) ?? ink;
        if (style["glyph"] is { } watermark && evaluator.Glyph(watermark) is { } mark)
        {
            svg.Start("g", ("opacity", "0.25"));
            Glyphs.Draw(svg, mark.Name, mark.Argument, inner, textColor, document.Figures(Vocabulary) ?? 1);
            svg.End();
        }

        WriteSlots(svg, subject, owner, style, evaluator, inner, size, tier, textColor, side, warnings);
        if (evaluator.Ident(style["badges"]) != "none")
        {
            WriteBadges(svg, style, evaluator, x0, y0, size, ink);
        }
    }

    private void WriteSlots(SvgWriter svg, StyleSubject subject, StyleSubject? owner, ComputedStyle face, Evaluator faceEvaluator, Box inner, double size,
        DetailTier tier, string textColor, string? side, List<RenderWarning> warnings)
    {
        if (face["face-template"] is not { Count: > 0 } template)
        {
            return;
        }

        var rows = new List<string[]>();
        foreach (var component in template)
        {
            if (component is not StringComponent row)
            {
                warnings.Add(new(subject.Document.Id, "face-template takes strings, one per row."));
                return;
            }

            rows.Add(row.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        var columns = rows.Max(row => row.Length);
        if (columns == 0)
        {
            return;
        }

        var regions = new List<(string Name, int Row0, int Row1, int Column0, int Column1)>();
        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < columns; c++)
            {
                var name = c < rows[r].Length ? rows[r][c] : ".";
                if (name == ".")
                {
                    continue;
                }

                var index = regions.FindIndex(region => region.Name == name);
                if (index < 0)
                {
                    regions.Add((name, r, r, c, c));
                }
                else
                {
                    var region = regions[index];
                    regions[index] = (name, Math.Min(region.Row0, r), Math.Max(region.Row1, r), Math.Min(region.Column0, c), Math.Max(region.Column1, c));
                }
            }
        }

        var cellWidth = inner.Width / columns;
        var cellHeight = inner.Height / rows.Count;
        var faceWeight = faceEvaluator.Ident(face["font-weight"]);
        foreach (var region in regions)
        {
            var box = new Box(inner.X + (region.Column0 * cellWidth), inner.Y + (region.Row0 * cellHeight),
                (region.Column1 - region.Column0 + 1) * cellWidth, (region.Row1 - region.Row0 + 1) * cellHeight);
            var slotStyle = StyleCascade.Compute(Sheet, Vocabulary, subject, owner, region.Name, tier);
            var evaluator = new Evaluator(this, subject, side, warnings, region.Name);
            if (evaluator.Ident(slotStyle["display"]) == "none" || slotStyle["content"] is not { } content)
            {
                continue;
            }

            var color = evaluator.Color(slotStyle["color"]) ?? textColor;
            svg.Start("g", ("data-slot", region.Name));
            if (evaluator.Color(slotStyle["fill"]) is { } background)
            {
                if (evaluator.Ident(slotStyle["badge-shape"]) == "circle")
                {
                    svg.Empty("circle", ("cx", N(box.CenterX)), ("cy", N(box.CenterY)), ("r", N(Math.Min(box.Width, box.Height) * 0.48)), ("fill", background));
                }
                else
                {
                    svg.Empty("rect", ("x", N(box.X + (size * 0.01))), ("y", N(box.Y + (size * 0.01))), ("width", N(box.Width - (size * 0.02))),
                        ("height", N(box.Height - (size * 0.02))), ("rx", N(size * 0.04)), ("fill", background));
                }
            }

            if (evaluator.Glyph(content) is { } glyph)
            {
                if (!Glyphs.Draw(svg, glyph.Name, glyph.Argument, box, color, subject.Document.Figures(Vocabulary) ?? 1))
                {
                    warnings.Add(new(subject.Document.Id, $"Slot '{region.Name}': '{glyph.Name}' is not a glyph."));
                }
            }
            else if (evaluator.Text(content) is { Length: > 0 } text)
            {
                var fontSize = evaluator.Number(slotStyle["font-size"]) is { } fraction
                    ? fraction * size
                    : Math.Min(box.Height * 0.8, box.Width * 0.95 / Math.Max(1, text.Length * 0.6));
                var weight = evaluator.Ident(slotStyle["font-weight"]) ?? faceWeight;
                var align = evaluator.Ident(slotStyle["align"]) ?? "center";
                var width = TextWidth(text, fontSize);
                var (x, anchor, left) = align switch
                {
                    "start" => (box.X + (size * 0.02), "start", box.X + (size * 0.02)),
                    "end" => (box.Right - (size * 0.02), "end", box.Right - (size * 0.02) - width),
                    _ => (box.CenterX, "middle", box.CenterX - (width / 2)),
                };
                var baseline = box.CenterY + (fontSize * 0.35);
                var attributes = new List<(string, string)>
                {
                    ("x", N(x)), ("y", N(baseline)), ("font-size", N(fontSize)), ("font-family", "sans-serif"), ("text-anchor", anchor), ("fill", color),
                };
                if (weight is "bold")
                {
                    attributes.Add(("font-weight", "bold"));
                }

                svg.Start("text", [.. attributes]);
                svg.Text(text);
                svg.End();
                WriteMark(svg, evaluator.Ident(slotStyle["mark"]), left, width, baseline, fontSize, color, subject, warnings);
                if (slotStyle["superscript"] is { } superscript && evaluator.Text(superscript) is { Length: > 0 } raised)
                {
                    svg.Start("text", ("x", N(left + width + (fontSize * 0.04))), ("y", N(baseline - (fontSize * 0.45))), ("font-size", N(fontSize * 0.55)),
                        ("font-family", "sans-serif"), ("fill", color), ("data-superscript", "true"));
                    svg.Text(raised);
                    svg.End();
                }
            }

            svg.End();
        }
    }

    private static void WriteMark(SvgWriter svg, string? mark, double left, double width, double baseline, double fontSize, string color, StyleSubject subject,
        List<RenderWarning> warnings)
    {
        var line = N(fontSize * 0.08);
        switch (mark)
        {
            case null or "none":
                return;
            case "underline":
                svg.Empty("line", ("x1", N(left)), ("y1", N(baseline + (fontSize * 0.14))), ("x2", N(left + width)), ("y2", N(baseline + (fontSize * 0.14))),
                    ("stroke", color), ("stroke-width", line), ("data-mark", mark));
                return;
            case "overline":
                svg.Empty("line", ("x1", N(left)), ("y1", N(baseline - (fontSize * 0.9))), ("x2", N(left + width)), ("y2", N(baseline - (fontSize * 0.9))),
                    ("stroke", color), ("stroke-width", line), ("data-mark", mark));
                return;
            case "box":
                svg.Empty("rect", ("x", N(left - (fontSize * 0.14))), ("y", N(baseline - (fontSize * 0.92))), ("width", N(width + (fontSize * 0.28))),
                    ("height", N(fontSize * 1.14)), ("fill", "none"), ("stroke", color), ("stroke-width", line), ("data-mark", mark));
                return;
            case "circle":
                svg.Empty("circle", ("cx", N(left + (width / 2))), ("cy", N(baseline - (fontSize * 0.35))), ("r", N(Math.Max(width, fontSize) * 0.62)),
                    ("fill", "none"), ("stroke", color), ("stroke-width", line), ("data-mark", mark));
                return;
            default:
                warnings.Add(new(subject.Document.Id, $"'{mark}' is not a mark; use underline, overline, box, circle, or none."));
                return;
        }
    }

    private static void WriteBadges(SvgWriter svg, ComputedStyle style, Evaluator evaluator, double x0, double y0, double size, string ink)
    {
        var fill = evaluator.Color(style["badge-fill"]) ?? ink;
        var color = evaluator.Color(style["badge-color"]) ?? "#ffffff";
        var height = size * 0.26;
        var fontSize = size * 0.19;
        var gap = size * 0.04;
        var badges = new List<(string Text, string Position, double Width)>();
        foreach (var value in style.Badges)
        {
            var position = value[^1] is IdentComponent { Name: var name } && BadgePositions.Contains(name) ? name : "top-right";
            var parts = value[^1] is IdentComponent { Name: var last } && BadgePositions.Contains(last) ? value.Take(value.Count - 1).ToArray() : [.. value];
            if (evaluator.Text(parts) is not { Length: > 0 } text || badges.Any(badge => badge.Text == text))
            {
                continue;
            }

            badges.Add((text, position, Math.Max(height, TextWidth(text, fontSize) + (size * 0.14))));
        }

        // Each edge holds what fits in a little more than the face width; the rest collapse into a count badge.
        foreach (var edge in new[] { "top", "bottom" })
        {
            var onEdge = badges.Where(badge => badge.Position.StartsWith(edge, StringComparison.Ordinal)).ToList();
            var available = size * 1.1;
            var kept = new List<(string Text, string Position, double Width)>();
            var used = 0.0;
            for (var index = 0; index < onEdge.Count; index++)
            {
                var badge = onEdge[index];
                var reserve = index < onEdge.Count - 1 ? height + gap : 0;
                if (used + badge.Width + reserve > available)
                {
                    break;
                }

                kept.Add(badge);
                used += badge.Width + gap;
            }

            var hidden = onEdge.Count - kept.Count;
            if (hidden > 0)
            {
                var more = "+" + hidden.ToString(CultureInfo.InvariantCulture);
                kept.Add((more, edge + "-right", Math.Max(height, TextWidth(more, fontSize) + (size * 0.14))));
            }

            var centerY = edge == "top" ? y0 : y0 + size;
            var leftX = x0 - (size * 0.05);
            var rightX = x0 + size + (size * 0.05);
            foreach (var badge in kept)
            {
                double x;
                if (badge.Position.EndsWith("left", StringComparison.Ordinal))
                {
                    x = leftX;
                    leftX += badge.Width + gap;
                }
                else
                {
                    rightX -= badge.Width;
                    x = rightX;
                    rightX -= gap;
                }

                svg.Start("g", ("data-badge", badge.Text));
                svg.Empty("rect", ("x", N(x)), ("y", N(centerY - (height / 2))), ("width", N(badge.Width)), ("height", N(height)), ("rx", N(height * 0.3)),
                    ("fill", fill));
                svg.Start("text", ("x", N(x + (badge.Width / 2))), ("y", N(centerY + (fontSize * 0.35))), ("font-size", N(fontSize)), ("font-family", "sans-serif"),
                    ("font-weight", "bold"), ("text-anchor", "middle"), ("fill", color));
                svg.Text(badge.Text);
                svg.End();
                svg.End();
            }
        }
    }

    private static void Stripes(SvgWriter svg, Box band, string color, string ink)
    {
        svg.Start("g", ("data-pattern", "stripes"));
        svg.Empty("rect", ("x", N(band.X)), ("y", N(band.Y)), ("width", N(band.Width)), ("height", N(band.Height)), ("fill", color));
        var step = band.Height * 1.6;
        for (var x = band.X + (step / 2); x + band.Height <= band.Right; x += step)
        {
            svg.Empty("line", ("x1", N(x)), ("y1", N(band.Bottom)), ("x2", N(x + band.Height)), ("y2", N(band.Y)), ("stroke", ink),
                ("stroke-width", N(band.Height * 0.35)));
        }

        svg.End();
    }

    /// <summary>An estimate of sans-serif text width; used for marks and badges, never for layout the viewer depends on.</summary>
    private static double TextWidth(string text, double fontSize) => text.Length * fontSize * 0.6;

    private static string N(double value) => SvgWriter.Number(value);

    /// <summary>Resolves declaration values for one subject: tokens, <c>side()</c>, <c>attr()</c>, and <c>glyph()</c>.</summary>
    private sealed class Evaluator(UnitRenderer renderer, StyleSubject subject, string? side, List<RenderWarning>? warnings, string? slot)
    {
        public double? Number(IReadOnlyList<StyleComponent>? value) =>
            Expand(value) is [NumberComponent number] ? number.Value : null;

        public string? Ident(IReadOnlyList<StyleComponent>? value) =>
            Expand(value) is [IdentComponent ident] ? ident.Name : null;

        public string? Color(IReadOnlyList<StyleComponent>? value)
        {
            switch (Expand(value))
            {
                case null:
                    return null;
                case [ColorComponent color]:
                    return color.Hex;
                case [IdentComponent { Name: "none" or "transparent" }]:
                    return "none";
                default:
                    Warn($"'{string.Join(' ', value!)}' is not a color.");
                    return null;
            }
        }

        public (string Name, string? Argument)? Glyph(IReadOnlyList<StyleComponent>? value) =>
            Expand(value) is [FunctionComponent { Name: "glyph", Arguments: [IdentComponent name, ..] arguments }]
                ? (name.Name, arguments.Count > 1 && arguments[1] is IdentComponent argument ? argument.Name : null)
                : null;

        /// <summary>The concatenated text of a value, or null when an <c>attr()</c> it names is missing.</summary>
        public string? Text(IReadOnlyList<StyleComponent>? value)
        {
            if (Expand(value) is not { } components)
            {
                return null;
            }

            if (components is [IdentComponent { Name: "none" }])
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder();
            foreach (var component in components)
            {
                switch (component)
                {
                    case StringComponent text:
                        builder.Append(text.Value);
                        break;
                    case NumberComponent number:
                        builder.Append(number.ToString());
                        break;
                    case IdentComponent ident:
                        builder.Append(ident.Name);
                        break;
                    case FunctionComponent { Name: "attr", Arguments: [IdentComponent name, ..] arguments }:
                        var option = arguments.Count > 1 && arguments[1] is IdentComponent optionName ? optionName.Name : null;
                        if (Attribute(name.Name, option) is not { } attribute)
                        {
                            Warn($"'{name.Name}' is not set on this unit; the {(slot is null ? "value" : $"slot '{slot}'")} is empty.");
                            return null;
                        }

                        builder.Append(attribute);
                        break;
                    default:
                        Warn($"'{component}' cannot be shown as text.");
                        return null;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// An attribute's displayed value. The option <c>first</c> takes a list's first item; a face name, such as
        /// <c>front</c>, reads that face when the shown face lacks the value.
        /// </summary>
        private string? Attribute(string name, string? option)
        {
            var document = subject.Document;
            switch (name)
            {
                case "id":
                    return document.Id;
                case "side":
                    return side is null ? null : renderer.Vocabulary.SideLabel(side);
                case "size-class":
                    return document.Figures(renderer.Vocabulary)?.ToString(CultureInfo.InvariantCulture);
            }

            if (!renderer.Vocabulary.HasKind(document.Kind) ||
                !renderer.Vocabulary.TryResolveAttribute(document.Kind, name, out var attribute, out _) ||
                (document.Value(subject.Face, attribute.Name) ??
                 (option is not null && option != "first" ? document.Face(option)?.Value(attribute.Name) : null)) is not { } value)
            {
                return null;
            }

            return option == "first" && value.Items.Count > 0 ? value.Items[0] : value.Display;
        }

        private List<StyleComponent>? Expand(IReadOnlyList<StyleComponent>? value, int depth = 0)
        {
            if (value is null)
            {
                return null;
            }

            if (depth > 8)
            {
                Warn("Tokens refer to each other in a loop.");
                return null;
            }

            var expanded = new List<StyleComponent>();
            foreach (var component in value)
            {
                switch (component)
                {
                    case FunctionComponent { Name: "token", Arguments: [IdentComponent name] }:
                        if (!renderer.Sheet.Tokens.TryGetValue(name.Name, out var tokenValue) || Expand(tokenValue, depth + 1) is not { } resolved)
                        {
                            Warn($"The token '{name.Name}' is not defined.");
                            return null;
                        }

                        expanded.AddRange(resolved);
                        break;
                    case FunctionComponent { Name: "side", Arguments: [IdentComponent role] }:
                        if (renderer.Palette.For(side).Role(role.Name) is not { } color)
                        {
                            Warn($"'{role.Name}' is not a palette role; use fill, fill-muted, ink, or accent.");
                            return null;
                        }

                        expanded.Add(new ColorComponent(color));
                        break;
                    default:
                        expanded.Add(component);
                        break;
                }
            }

            return expanded;
        }

        private void Warn(string message) => warnings?.Add(new(subject.Document.Id, message));
    }
}
