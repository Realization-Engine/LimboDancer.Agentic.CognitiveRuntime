using LimboDancer.Domains.Asl.Units.Rendering.Styles;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>The style language parser (section 3.3) and its diagnostics (section 9).</summary>
public sealed class StyleSheetTests
{
    [Theory]
    [InlineData(UnitStyles.Classic, "asl-customary")]
    [InlineData(UnitStyles.Digital, "limbodancer")]
    public void TheBuiltInSheetsParseWithoutWarnings(string name, string palette)
    {
        var result = StyleSheetParser.Parse(name, UnitStyles.Text(name));
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(palette, result.Sheet!.Palette);
        Assert.Contains(result.Sheet.Rules, rule => rule.Tier == DetailTier.Far);
        Assert.Contains(result.Sheet.Rules, rule => rule.Tier == DetailTier.Mid);
    }

    [Fact]
    public void TheDesignExcerptParses()
    {
        var sheet = RenderingTestData.Sheet("""
            /* The ASL digital theme, Personnel excerpt */
            @tokens {
              side-german: #7b8c97;
              side-russian: #9b7b52;
              ink: #17212b;
            }

            asl|personnel {
              face-template: "class  .     size"
                             "fp     range morale"
                             "ident  ident ident";
              fill: side(fill);
              stroke: token(ink);
            }

            asl|personnel::slot(firepower) { content: attr(firepower); font-weight: bold; }
            asl|personnel.asl\:assault-fire::slot(firepower) { mark: underline; }
            asl|personnel[smoke-exponent]::slot(firepower)   { superscript: attr(smoke-exponent); }
            asl|leader { face-template: "drm" "morale"; }
            asl|unit:asl\:pinned { badge: "PIN" top-right; }
            asl|unit:asl\:broken { face: broken; fill: side(fill-muted); }

            @detail far {
              asl|personnel { face-template: "glyph"; }
              asl|personnel::slot(glyph) { content: glyph(size-figures); }
            }
            """);
        Assert.Equal(3, sheet.Tokens.Count);
        Assert.Equal(new ColorComponent("#7b8c97"), Assert.Single(sheet.Tokens["side-german"]));
        var assault = sheet.Rules[2].Selectors[0];
        Assert.Equal("asl:personnel", assault.Subject.Kind);
        Assert.Equal(["asl:assault-fire"], assault.Subject.Traits);
        Assert.Equal("firepower", assault.Slot);
        Assert.Equal(new AttributeCondition("smoke-exponent", null), Assert.Single(sheet.Rules[3].Selectors[0].Subject.Attributes));
        Assert.Equal(["asl:pinned"], sheet.Rules[5].Selectors[0].Subject.States);
        Assert.Equal(3, sheet.Rules[0].Declarations[0].Value.Count);
        Assert.Equal(DetailTier.Far, sheet.Rules[^1].Tier);
        Assert.Equal("glyph(size-figures)", sheet.Rules[^1].Declarations[0].Value[0].ToString());
    }

    [Fact]
    public void SelectorsCoverAttachedFacesConcealmentAndSides()
    {
        var sheet = RenderingTestData.Sheet("""
            asl|squad::attached(asl|mg)::slot(fp), unit:concealed, [side=german]:face(broken), * { fill: #abc; }
            """);
        var selectors = sheet.Rules[0].Selectors;
        Assert.Equal("asl:mg", selectors[0].Attached!.Kind);
        Assert.Equal("fp", selectors[0].Slot);
        Assert.True(selectors[1].Subject.Concealed);
        Assert.Equal("broken", selectors[2].Subject.Face);
        Assert.Equal(new AttributeCondition("side", "german"), Assert.Single(selectors[2].Subject.Attributes));
        Assert.Null(selectors[3].Subject.Kind);
        Assert.Equal(new ColorComponent("#aabbcc"), sheet.Rules[0].Declarations[0].Value[0]);
    }

    [Theory]
    [InlineData("asl|squad { fill: red", 1, 22, "Expected")]
    [InlineData("asl|squad {\n  fill red;\n}", 2, 8, "':' after 'fill'")]
    [InlineData("asl|personnel asl|squad { fill: #fff; }", 1, 15, "Descendant combinators")]
    [InlineData("@media screen { }", 1, 1, "not an at-rule")]
    [InlineData("@detail huge { }", 1, 9, "far, mid, or near")]
    [InlineData("asl|squad { content: calc(1); }", 1, 22, "not a value function")]
    [InlineData("asl|squad { fill: #12; }", 1, 19, "not a color")]
    [InlineData("asl|squad { content: \"open; }", 1, 22, "string is not closed")]
    [InlineData("/* never closed", 1, 1, "comment is not closed")]
    [InlineData("asl|squad::before { }", 1, 12, "::slot(name)")]
    [InlineData("asl|squad { fill: ; }", 1, 19, "has no value")]
    public void ASyntaxErrorRefusesTheSheetWithItsPosition(string text, int line, int column, string message)
    {
        var result = StyleSheetParser.Parse("test", text);
        Assert.Null(result.Sheet);
        var error = Assert.Single(result.Diagnostics);
        Assert.Equal(StyleDiagnosticSeverity.Error, error.Severity);
        Assert.Equal((line, column), (error.Line, error.Column));
        Assert.Contains(message, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownPropertyIsAWarningAndIgnored()
    {
        var result = StyleSheetParser.Parse("test", "asl|squad {\n  fill: #fff;\n  sparkle: lots;\n}");
        Assert.True(result.Succeeded);
        var warning = Assert.Single(result.Diagnostics);
        Assert.Equal((StyleDiagnosticSeverity.Warning, 3, 3), (warning.Severity, warning.Line, warning.Column));
        Assert.Single(result.Sheet!.Rules[0].Declarations);
    }

    [Fact]
    public void TheSheetHashIgnoresLineEndings()
    {
        var text = UnitStyles.Text(UnitStyles.Digital).ReplaceLineEndings("\n");
        Assert.Equal(StyleSheetParser.Parse("a", text).Sheet!.Hash, StyleSheetParser.Parse("a", text.Replace("\n", "\r\n", StringComparison.Ordinal)).Sheet!.Hash);
    }

    [Fact]
    public void AMissingAttributeRendersAnEmptySlotAndAWarning()
    {
        var sheet = RenderingTestData.Sheet("""
            unit { face-template: "fp range"; }
            unit::slot(fp) { content: attr(leadership); }
            unit::slot(range) { content: "R" attr(range); }
            """);
        var warnings = new List<RenderWarning>();
        var leader = RenderingTestData.Document("""{ "id": "l", "kind": "asl:leader", "faces": { "front": { "morale": 8 } } }""");
        var svg = RenderingTestData.Render(RenderingTestData.Renderer(sheet), leader, warnings: warnings);
        Assert.DoesNotContain(svg.Descendants(), element => element.Name.LocalName == "text" && element.Parent?.Attribute("data-slot") is not null);
        Assert.Equal(2, warnings.Count);
        Assert.All(warnings, warning => Assert.Equal("l", warning.UnitId));
        Assert.Contains("slot 'range'", warnings[1].Message, StringComparison.Ordinal);
    }
}
