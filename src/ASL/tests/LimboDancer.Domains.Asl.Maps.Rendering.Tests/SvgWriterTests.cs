using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Rendering.Tests;

public sealed class SvgWriterTests
{
    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(1.5, "1.5")]
    [InlineData(-16.0, "-16")]
    [InlineData(32.25, "32.25")]
    [InlineData(0.015625, "0.015625")]
    public void NumbersAreExactAndInvariant(double value, string expected) => Assert.Equal(expected, SvgWriter.Number(value));

    // Non-standard hex sizes give geometry that is not exact in 1/64 pixels; it is rounded to the nearest 1/64.
    [Theory]
    [InlineData(0.1, "0.09375")]
    [InlineData(37.541666666666664, "37.546875")]
    [InlineData(-0.0078125, "-0.015625")]
    public void InexactNumbersRoundToTheNearestSixtyFourth(double value, string expected) => Assert.Equal(expected, SvgWriter.Number(value));

    [Fact]
    public void TextAndAttributesAreEscaped()
    {
        var writer = new SvgWriter();
        writer.Start("svg", ("data-note", "a \"b\" & <c>"));
        writer.Text("Fish & <chips>");
        writer.End();
        var svg = writer.ToString();
        Assert.Contains("data-note=\"a &quot;b&quot; &amp; &lt;c>\"", svg, StringComparison.Ordinal);
        Assert.Contains("Fish &amp; &lt;chips&gt;", svg, StringComparison.Ordinal);
        Assert.Equal("a \"b\" & <c>", System.Xml.Linq.XDocument.Parse(svg).Root!.Attribute("data-note")!.Value);
    }

    // An attribute escapes &, <, and "; > stays as it is. The cases cover where an escaped character falls in the value.
    [Theory]
    [InlineData("", "")]
    [InlineData("plain", "plain")]
    [InlineData("&start", "&amp;start")]
    [InlineData("end<", "end&lt;")]
    [InlineData("\"\"", "&quot;&quot;")]
    [InlineData("&<\"", "&amp;&lt;&quot;")]
    [InlineData("a>b", "a>b")]
    [InlineData("Kübelwagen & crew", "Kübelwagen &amp; crew")]
    public void AttributeValuesAreEscapedWherever(string value, string expected)
    {
        var writer = new SvgWriter();
        writer.Empty("g", ("data-note", value));
        Assert.Equal($"<g data-note=\"{expected}\"/>", writer.ToString());
    }

    // Text escapes &, <, and >; " stays as it is.
    [Theory]
    [InlineData("", "")]
    [InlineData("plain", "plain")]
    [InlineData("&start", "&amp;start")]
    [InlineData("end>", "end&gt;")]
    [InlineData("<<", "&lt;&lt;")]
    [InlineData("&<>", "&amp;&lt;&gt;")]
    [InlineData("say \"hi\"", "say \"hi\"")]
    [InlineData("Kübelwagen <crew>", "Kübelwagen &lt;crew&gt;")]
    public void TextIsEscapedWherever(string text, string expected)
    {
        var writer = new SvgWriter();
        writer.Start("title");
        writer.Text(text);
        writer.End();
        Assert.Equal($"<title>{expected}</title>", writer.ToString());
    }

    // The writer's pooled buffer goes back to the pool when the document is made (option G3 of the SVG memory analysis).
    [Fact]
    public void TheDocumentIsMadeOnceAndTheWriterThenTakesNoMore()
    {
        var writer = new SvgWriter();
        writer.Empty("rect", ("x", SvgValue.Number(1.5)), ("width", SvgValue.Number(3)), ("d", new PathData().MoveTo(SvgValue.Fixed(1), "2").Close()));
        var document = writer.ToString();
        Assert.Equal("<rect x=\"1.5\" width=\"3\" d=\"M0.015625 2Z\"/>", document);
        Assert.Same(document, writer.ToString());
        Assert.Throws<InvalidOperationException>(() => writer.Empty("g"));
    }

    [Fact]
    public void ALongDocumentGrowsTheBuffer()
    {
        var writer = new SvgWriter();
        writer.Start("g");
        for (var index = 0; index < 5000; index++)
        {
            writer.Empty("circle", ("r", SvgValue.Number(index)), ("data-note", "a & b"));
        }

        writer.End();
        var document = writer.ToString();
        Assert.Equal(5000, System.Xml.Linq.XDocument.Parse(document).Root!.Elements().Count());
        Assert.EndsWith("<circle r=\"4999\" data-note=\"a &amp; b\"/></g>", document, StringComparison.Ordinal);
    }

    [Fact]
    public void RingsUseAlternatingHorizontalAndVerticalCommands()
    {
        var path = new PathData().Ring([new GridPoint(0, 0), new GridPoint(4, 0), new GridPoint(4, 3), new GridPoint(0, 3)]);
        Assert.Equal("M0 0H4V3H0Z", path.ToString());
    }

    [Fact]
    public void PolygonsUseLineCommands()
    {
        var path = new PathData().Polygon([new PixelPoint(0, 0), new PixelPoint(1.5, 2.25), new PixelPoint(-1, 3)]);
        Assert.Equal("M0 0L1.5 2.25L-1 3Z", path.ToString());
    }
}
