using LimboDancer.Domains.Asl.MapStudio.Components.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The user, 2026-10-21: every list that offers units groups them by hex in map order, a leader first in his group, and a leader the view may not
/// know is not put first (the Memory and Performance Guidelines' disclosure rule).
/// </summary>
public sealed class UnitGroupingTests
{
    private static UnitPlace Place(string column, int row, bool leader = false) => new($"[{column}{row}]", $"[{column}{row}]", (column.Length, column, row), leader);

    [Fact]
    public void UnitsAreGroupedByHexInMapOrderLeadersFirstAndUnplacedLast()
    {
        var places = new Dictionary<string, UnitPlace>(StringComparer.Ordinal)
        {
            ["s1"] = Place("Z", 6),
            ["s2"] = Place("C", 3),
            ["l1"] = Place("Z", 6, leader: true),
            ["s3"] = Place("AA", 2),
            ["s4"] = Place("C", 10),
            ["s5"] = Place("C", 3),
        };
        string[] items = ["s1", "s2", "l1", "w1", "s3", "s4", "s5"];
        var groups = UnitGrouping.Groups(items, id => id, id => places.GetValueOrDefault(id));

        // Column by its width, then its letters, then the row: [C3], [C10], [Z6], [AA2]; the weapon w1 stands on no hex and comes last.
        Assert.Equal(["[C3]", "[C10]", "[Z6]", "[AA2]", null], groups.Select(group => group.Hex));
        Assert.Equal(["s2", "s5"], groups[0].Items);
        Assert.Equal(["l1", "s1"], groups[2].Items);
        Assert.Equal(["w1"], groups[^1].Items);
    }

    [Fact]
    public void WithNoPlacesOrOneItemTheListIsOneGroupInTheCallersOrder()
    {
        string[] items = ["b", "a"];
        Assert.Equal(["b", "a"], Assert.Single(UnitGrouping.Groups(items, id => id, null)).Items);
        Assert.Null(Assert.Single(UnitGrouping.Groups(["a"], id => id, _ => Place("A", 1))).Hex);
    }

    [Theory]
    [InlineData(true, false, true, false, true)]
    [InlineData(true, false, false, false, true)]
    [InlineData(true, false, false, true, false)]
    [InlineData(true, false, true, true, true)]
    [InlineData(true, true, false, true, true)]
    [InlineData(false, true, true, false, false)]
    public void ALeaderIsPutFirstOnlyWhenTheViewMayKnowHim(bool leader, bool adjudicator, bool ownSide, bool concealedOrHidden, bool first) =>
        Assert.Equal(first, UnitGrouping.KnownLeader(leader, adjudicator, ownSide, concealedOrHidden));
}
