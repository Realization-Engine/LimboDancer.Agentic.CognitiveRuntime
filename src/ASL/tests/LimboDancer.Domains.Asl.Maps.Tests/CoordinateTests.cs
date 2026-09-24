using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class CoordinateTests
{
    [Theory]
    [InlineData("A1", 0, 1)]
    [InlineData("B0", 1, 0)]
    [InlineData("Z9", 25, 9)]
    [InlineData("AA8", 26, 8)]
    [InlineData("GG10", 32, 10)]
    [InlineData("AAA2", 52, 2)]
    public void HexNamesParseAndFormat(string text, int column, int rowNumber)
    {
        var name = HexName.Parse(text);
        Assert.Equal(new HexName(column, rowNumber), name);
        Assert.Equal(text, name.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("4")]
    [InlineData("a1")]
    [InlineData("AB1")]
    [InlineData("A01")]
    [InlineData("A1B")]
    [InlineData("A-1")]
    public void MalformedHexNamesAreRejected(string text)
    {
        Assert.False(HexName.TryParse(text, out _));
    }

    [Theory]
    [InlineData("bd01", BoardRefKind.Vasl)]
    [InlineData("bdRB", BoardRefKind.Vasl)]
    [InlineData("ab-river-village", BoardRefKind.Authored)]
    [InlineData("map-scenario-12", BoardRefKind.ComposedMap)]
    public void BoardRefsParseByKind(string text, BoardRefKind kind)
    {
        var boardRef = BoardRef.Parse(text);
        Assert.Equal(kind, boardRef.Kind);
        Assert.Equal(text, boardRef.ToString());
    }

    [Theory]
    [InlineData("bd")]
    [InlineData("bd-01")]
    [InlineData("ab-")]
    [InlineData("ab-River")]
    [InlineData("ab-river-")]
    [InlineData("map-")]
    [InlineData("board01")]
    public void MalformedBoardRefsAreRejected(string text)
    {
        Assert.False(BoardRef.TryParse(text, out _));
    }

    [Fact]
    public void VaslBoardNameComesFromTheRef()
    {
        Assert.Equal("01", BoardRef.Parse("bd01").VaslBoardName);
        Assert.Equal(BoardRef.Parse("bd01"), BoardRef.ForVaslBoard("01"));
        Assert.Throws<InvalidOperationException>(() => BoardRef.Parse("ab-x").VaslBoardName);
    }

    [Theory]
    [InlineData("bd01:E4:0")]
    [InlineData("bd01:D4:0")]
    [InlineData("bd01:E4:1")]
    [InlineData("bd01:E4:-1")]
    [InlineData("bd01:E4:0/2")]
    [InlineData("ab-river-village:AA8:0")]
    public void CanonicalLocationsRoundTrip(string text)
    {
        Assert.Equal(text, BoardLocation.Parse(text).ToString());
    }

    [Theory]
    [InlineData("bd01:E4")]
    [InlineData("bd01:E4:+1")]
    [InlineData("bd01:E4:01")]
    [InlineData("bd01:E4:-0")]
    [InlineData("bd01:E4:0/6")]
    [InlineData("bd01:E4:0/")]
    [InlineData("bd01:e4:0")]
    [InlineData("01:E4:0")]
    [InlineData("bd01:E4:0:1")]
    public void NonCanonicalLocationsAreRejected(string text)
    {
        Assert.False(BoardLocation.TryParse(text, out _));
    }

    [Fact]
    public void HexsideLocationsCanonicalizeOnTheBoard()
    {
        var location = BoardLocation.Parse("bd01:E4:0/5");
        Assert.Equal("bd01:D3:0/2", location.Canonicalize(BoardGeometry.StandardGeomorphic).ToString());
        Assert.Equal("bd01:E4:0", location.AtGroundLevel().ToString());
    }

    [Fact]
    public void PublishedFormsResolveOnlyThroughTheBoardSet()
    {
        var boards = new BoardSet(
            new Dictionary<int, BoardRef> { [1] = BoardRef.Parse("bd01"), [36] = BoardRef.Parse("bd36") },
            BoardRef.Parse("bd01"));

        Assert.Equal("bd01:E4:0", LocationParser.Parse("1E4", boards).Location?.ToString());
        Assert.Equal("bd36:AA8:0", LocationParser.Parse("36AA8", boards).Location?.ToString());
        Assert.Equal("bd01:K3:0", LocationParser.Parse("K3", boards).Location?.ToString());
        Assert.Equal("bd01:E4:1", LocationParser.Parse("bd01:E4:1", boards).Location?.ToString());
    }

    [Fact]
    public void UnresolvableFormsReturnDiagnosticsInsteadOfGuessing()
    {
        Assert.Equal(LocationParseStatus.UnmappedBoardNumber, LocationParser.Parse("3K3", BoardSet.Empty).Status);
        Assert.Equal(LocationParseStatus.NoCurrentBoard, LocationParser.Parse("K3", BoardSet.Empty).Status);
        Assert.Equal(LocationParseStatus.Malformed, LocationParser.Parse("03K3", BoardSet.Empty).Status);
        Assert.Equal(LocationParseStatus.Malformed, LocationParser.Parse("board 3", BoardSet.Empty).Status);
        Assert.Equal(LocationParseStatus.Malformed, LocationParser.Parse("bd01:E4", BoardSet.Empty).Status);
        Assert.Null(LocationParser.Parse("3K3", BoardSet.Empty).Location);
    }
}
