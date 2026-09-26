using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Tests;

/// <summary>
/// The layout and the composed read over standard geomorphic boards (Composed Maps Design, sections 3 and 4). Board
/// bd02 sits above bd01, so bd01's hex row 1 meets bd02's row 10 at the seam, and the half hexes of the lettered columns
/// with eleven hexes (bd02's F10, bd01's F0) are shared; bd01, placed later, owns them.
/// </summary>
public sealed class ComposedMapReadTests
{
    private static readonly BoardRef Bd01 = BoardRef.Parse("bd01");
    private static readonly BoardRef Bd02 = BoardRef.Parse("bd02");
    private static readonly BoardRef Bd21 = BoardRef.Parse("bd21");
    private static readonly TerrainType Open = new() { Code = 0, Name = "Open Ground", Category = LosCategory.Open };
    private static readonly TerrainType Wooden = new() { Code = 1, Name = "Wooden Building", Category = LosCategory.Building };
    private static readonly TerrainType Wall = new() { Code = 2, Name = "Wall", Category = LosCategory.Other };

    private static HexName Hex(string name) => HexName.Parse(name);

    [Fact]
    public void NeighboursAndDistancesCrossTheSeam()
    {
        var read = Read(Handle(Bd02), Handle(Bd01));
        Assert.Equal((Bd02, Hex("G10")), read.Neighbor(Bd01, Hex("G1"), HexsideDirection.North));
        Assert.Equal((Bd01, Hex("G1")), read.Neighbor(Bd02, Hex("G10"), HexsideDirection.South));
        Assert.Equal(1, read.Distance(Bd02, Hex("G10"), Bd01, Hex("G1")));
        Assert.Equal(3, read.Distance(Bd02, Hex("G8"), Bd01, Hex("G1")));
        Assert.Equal(1, read.Distance(Bd01, Hex("G1"), Bd01, Hex("G2")));

        // The shared half hex: both names locate it, and bd01, placed later, owns it.
        Assert.Equal(read.Layout.Locate(Bd01, Hex("F0")), read.Layout.Locate(Bd02, Hex("F10")));
        Assert.True(read.Layout.IsOwnerName(Bd01, Hex("F0")));
        Assert.False(read.Layout.IsOwnerName(Bd02, Hex("F10")));
        Assert.Equal((Bd01, Hex("F0")), read.Neighbor(Bd01, Hex("F1"), HexsideDirection.North));
        Assert.Equal(2, read.Layout.Names(read.Layout.Locate(Bd01, Hex("F0"))!.Value).Count);
    }

    [Fact]
    public void AReversedBoardKeepsItsNamesAcrossTheSeam()
    {
        // bd02 rotated 180 degrees: its AA1 now lies above bd01's G1.
        var read = Read(Handle(Bd02), Handle(Bd01), reversedTop: true);
        Assert.Equal((Bd02, Hex("AA1")), read.Neighbor(Bd01, Hex("G1"), HexsideDirection.North));
        Assert.Equal(1, read.Distance(Bd02, Hex("AA1"), Bd01, Hex("G1")));
        var crossed = Assert.IsType<CrossedHexside>(read.Crossed(Bd02, Hex("AA1"), Bd01, Hex("G1")));
        Assert.Equal((HexsideDirection.South, true), (crossed.Side, crossed.AcrossSeam));

        // On bd02's own hexes the crossed side is its North, since the board is upside down.
        Assert.Equal(HexsideDirection.North, crossed.From!.Side);
        Assert.Equal(HexsideDirection.North, crossed.To!.Side);
    }

    [Fact]
    public void AUnitOnAReversedBoardKeepsItsBoardRelativeHex()
    {
        // U3: bd21:N5 on a map with bd21 reversed is placed in the rotated map hex and keeps its name.
        var layout = MapLayout.Create([(new BoardPlacement(Bd21, 0, 0, true, []), BoardGeometry.StandardGeomorphic)]).Layout!;
        var geometry = BoardGeometry.StandardGeomorphic;
        var local = geometry.IndexOf(Hex("N5"));
        var expected = new HexIndex(geometry.WidthInHexes - local.Column - 1, geometry.RowCount(local.Column) - local.Row - 1);
        Assert.Equal(expected, layout.Locate(Bd21, Hex("N5")));
        Assert.Equal((Bd21, Hex("N5")), layout.OwnerOf(expected));
    }

    [Fact]
    public void ASharedHexIsReadOnlyUnderItsOwnerAndOnlyWhenBothBoardsAgree()
    {
        var read = Read(Handle(Bd02), Handle(Bd01));
        Assert.NotNull(read.Resolve(BoardLocation.Parse("bd01:F0:0")).Read);
        Assert.Equal("MAP-READ-003", Assert.Single(read.Resolve(BoardLocation.Parse("bd02:F10:0")).Diagnostics).Code);

        var disagreeing = Read(Handle(Bd02, terrain: hex => hex == "F10" ? Wooden : Open), Handle(Bd01));
        Assert.Equal("MAP-READ-004", Assert.Single(disagreeing.Resolve(BoardLocation.Parse("bd01:F0:0")).Diagnostics).Code);
        Assert.NotNull(disagreeing.Resolve(BoardLocation.Parse("bd01:G1:0")).Read);
    }

    [Fact]
    public void AHexsideAcrossTheSeamCountsOnlyWhenBothRecordsAgree()
    {
        var read = Read(Handle(Bd02), Handle(Bd01));
        var crossed = Assert.IsType<CrossedHexside>(read.Crossed(Bd02, Hex("G10"), Bd01, Hex("G1")));
        Assert.Equal((HexsideDirection.South, true), (crossed.Side, crossed.AcrossSeam));
        Assert.NotNull(crossed.Agreed);

        var walled = Read(Handle(Bd02), Handle(Bd01, wall: ("G1", HexsideDirection.North)));
        Assert.Null(Assert.IsType<CrossedHexside>(walled.Crossed(Bd02, Hex("G10"), Bd01, Hex("G1"))).Agreed);

        // Within a board, the mover's record is used as it always was.
        var within = Assert.IsType<CrossedHexside>(walled.Crossed(Bd01, Hex("G2"), Bd01, Hex("G1")));
        Assert.Equal((false, HexsideDirection.North), (within.AcrossSeam, within.Side));
        Assert.Same(within.From, within.Agreed);
        Assert.Null(read.Crossed(Bd02, Hex("G9"), Bd01, Hex("G1")));
    }

    [Fact]
    public void ABoardPlacedTwiceOrAMismatchedHandleIsRefused()
    {
        Assert.Equal("MAP-READ-003", Assert.Single(ComposedMapRead.Create(
            [(new BoardPlacement(Bd01), Handle(Bd01)), (new BoardPlacement(Bd01, 0, 1), Handle(Bd01))]).Diagnostics).Code);
        Assert.Equal("MAP-READ-003", Assert.Single(ComposedMapRead.Create([(new BoardPlacement(Bd02), Handle(Bd01))]).Diagnostics).Code);
        Assert.Equal("VASL-MAP-003", Assert.Single(ComposedMapRead.Create(
            [(new BoardPlacement(Bd01), Handle(Bd01)), (new BoardPlacement(Bd02, 1, 1), Handle(Bd02))]).Diagnostics).Code);
    }

    private static ComposedMapRead Read(BoardHandle top, BoardHandle bottom, bool reversedTop = false)
    {
        var result = ComposedMapRead.Create([(new BoardPlacement(top.Ref, 0, 0, reversedTop, []), top), (new BoardPlacement(bottom.Ref, 0, 1), bottom)]);
        Assert.Empty(result.Diagnostics);
        return result.Read!;
    }

    /// <summary>A verified synthetic geomorphic board, open ground except where given, with a wall on one hexside if asked.</summary>
    private static BoardHandle Handle(BoardRef board, Func<string, TerrainType>? terrain = null, (string Hex, HexsideDirection Side)? wall = null)
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = geometry.Hexes().Select(index =>
        {
            var name = geometry.NameOf(index);
            var center = new LocationFacts(0, terrain?.Invoke(name.ToString()) ?? Open, null);
            HexsideFacts[] sides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, Open,
                wall is { } w && w.Hex == name.ToString() && w.Side == side ? Wall : null, false, false, false, false, null))];
            return new HexFacts(name, index, 0, false, center, [center], sides, null, CenterTerrainSource.CenterSample);
        }).ToArray();
        return new BoardHandle(board, "v1", BoardReadStatus.Verified, "synthetic", new HexFactSet(geometry, "test", hexes));
    }
}
