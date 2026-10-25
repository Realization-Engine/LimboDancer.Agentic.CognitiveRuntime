using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 70, Infantry III. Increment 3a: who sees (A12.1, A12.14, A12.122, A12.16; ruling R70.16), the actions that cost a "?" (A12.141; ruling R70.17),
/// and the Right of Inspection (A12.16; ruling R70.18), read in Rules through its public members.
/// </summary>
public sealed class ScenarioA1Pass70RulesTests
{
    private static SightSeerFacts Seer(int at = 1, bool goodOrder = true, bool unbroken = true, bool dummy = false, bool hidden = false, bool aboard = false,
        bool captured = false, bool emptyVehicle = false, string side = "german") =>
        new(true, side, dummy, hidden, aboard, goodOrder, unbroken, captured, emptyVehicle, at);

    // Each seer at its index; the range is the index itself, the Location asked about being at 0. Reads at the blocked indexes are blocked, at the
    // unclear ones not definitive (clear but not definitively so), and at the missing ones not given.
    private sealed class RangeLos(int[]? blocked = null, int[]? unclear = null, int[]? missing = null) : ISightLosReader
    {
        public int Reads { get; private set; }

        public SightLosFacts? Los(int fromLocation, int toLocation)
        {
            Reads++;
            return missing?.Contains(fromLocation) == true ? null
                : blocked?.Contains(fromLocation) == true ? new SightLosFacts(false, true, fromLocation)
                : new SightLosFacts(unclear?.Contains(fromLocation) != true, false, fromLocation);
        }
    }

    private static readonly int[] Near = [20, 17, 5];
    private static readonly int[] Far = [20, 17];
    private static readonly int[] Five = [5];
    private static readonly int[] Seventeen = [17];
    private static readonly int[] Here = [0, 9];
    private static readonly int[] TwoSeers = [3, 4];
    private static readonly int[] Three = [3];

    [Fact]
    public void EachPurposeCountsItsOwnSeers()
    {
        // A12.1 (p. 76): "Good Order for '?' loss; unbroken (and Dummies) for denial of '?' gain"; a vehicle with no crew, Passengers, or Riders does
        // neither. A12.16 (p. 79): the Good Order ground units of the inspecting side, so Dummies are not ("not being Good order").
        (SightSeerFacts Seer, bool Loss, bool Gain, bool Inspection)[] cases =
        [
            (Seer(), true, true, true),
            (Seer(dummy: true, goodOrder: false), false, true, false),
            (Seer(hidden: true), false, false, true),
            (Seer(goodOrder: false, unbroken: false), false, false, false),
            (Seer(goodOrder: false), false, true, false),
            (Seer(aboard: true), false, true, true),
            (Seer(emptyVehicle: true), false, false, false),
            (Seer(goodOrder: false, captured: true), false, false, false),
            (Seer(at: 0) with { Location = null }, false, false, false),
            (Seer() with { Active = false }, false, false, false),
        ];
        foreach (var (seer, loss, gain, inspection) in cases)
        {
            Assert.Equal(loss, ScenarioA1Sight.Sees(seer, SightPurpose.Loss));
            Assert.Equal(gain, ScenarioA1Sight.Sees(seer, SightPurpose.GainDenial));
            Assert.Equal(inspection, ScenarioA1Sight.Sees(seer, SightPurpose.Inspection));
        }

        // The seers of a watched side are the other side's; the Right of Inspection's are the viewer's own, each Location once.
        SightSeerFacts[] units = [Seer(at: 3), Seer(at: 3), Seer(at: 4, hidden: true), Seer(at: 5, side: "russian")];
        Assert.Equal(TwoSeers, ScenarioA1Sight.OwnSeerLocations(units, "german", SightPurpose.Inspection));
        Assert.Equal(Three, ScenarioA1Sight.SeerLocations(units, "russian", SightPurpose.Loss));
        Assert.Equal(Five, ScenarioA1Sight.SeerLocations(units, "german", SightPurpose.Loss));
    }

    [Fact]
    public void TheNearestSeerIsReadWithinAndBeyondSixteen()
    {
        // A12.121: "the range from the nearest such enemy having a LOS"; A12.14 and A12.141: within 16 hexes and in LOS is the nearest at 16 or less.
        Assert.Equal(5, ScenarioA1Sight.NearestSeer(Near, 0, new RangeLos(), SightLosReading.Clear));
        Assert.True(ScenarioA1Sight.SeenWithin(5, 16));
        Assert.Equal(17, ScenarioA1Sight.NearestSeer(Far, 0, new RangeLos(), SightLosReading.Clear));
        Assert.False(ScenarioA1Sight.SeenWithin(17, 16));
        Assert.True(ScenarioA1Sight.SeenWithin(17, null));
        Assert.False(ScenarioA1Sight.SeenWithin(null, null));

        // A blocked LOS is none; a seer in the Location itself is at 0, with no read.
        Assert.Equal(17, ScenarioA1Sight.NearestSeer(Near, 0, new RangeLos(blocked: Five), SightLosReading.Clear));
        var reader = new RangeLos();
        Assert.Equal(0, ScenarioA1Sight.NearestSeer(Here, 0, reader, SightLosReading.Clear));
        Assert.Equal(0, reader.Reads);

        // Each caller's reading as it was (ruling R70.16): a read not definitive counts for the vehicle's and the gain's reading, not for the move's; a
        // missing read counts only for the Right of Inspection, and gives no range.
        Assert.Null(ScenarioA1Sight.NearestSeer(Five, 0, new RangeLos(unclear: Five), SightLosReading.Clear));
        Assert.Equal(5, ScenarioA1Sight.NearestSeer(Five, 0, new RangeLos(unclear: Five), SightLosReading.NotBlocked));
        Assert.Null(ScenarioA1Sight.NearestSeer(Five, 0, new RangeLos(missing: Five), SightLosReading.NotShownBlocked));
        Assert.True(ScenarioA1Sight.InLos(null, SightLosReading.NotShownBlocked));
        Assert.False(ScenarioA1Sight.InLos(null, SightLosReading.NotBlocked));
        Assert.False(ScenarioA1Sight.InLos(new SightLosFacts(false, true, 3), SightLosReading.NotShownBlocked));
    }

    [Fact]
    public void AStackIsInspectedOnlyAfterSetupAndInTheLosOfASeer()
    {
        // A12.16 (p. 79): after play begins an unconcealed stack is inspected unless out of the LOS of all the viewer's Good Order ground units; before
        // play none is (A2.9), and its seers are not read.
        Assert.False(ScenarioA1Sight.Inspectable(false, () => throw new InvalidOperationException("the seers were read.")));
        Assert.True(ScenarioA1Sight.Inspectable(true, () => true));
        Assert.False(ScenarioA1Sight.Inspectable(true, () => false));

        // In LOS of one seer of two, read up to the first that sees; out of the LOS of both; a missing read counts as in LOS (ruling R70.18).
        var reader = new RangeLos(blocked: Seventeen);
        Assert.True(ScenarioA1Sight.AnySeerInLos(Near, 0, reader, SightLosReading.NotShownBlocked));
        Assert.Equal(1, reader.Reads);
        Assert.False(ScenarioA1Sight.AnySeerInLos(Far, 0, new RangeLos(blocked: Near), SightLosReading.NotShownBlocked));
        Assert.True(ScenarioA1Sight.AnySeerInLos(Far, 0, new RangeLos(missing: Far), SightLosReading.NotShownBlocked));
    }

    [Fact]
    public void AnActionCostsAConcealedUnitsQuestionMarkInTheLosOfASeerWithinSixteen()
    {
        // A12.141 (p. 78; ruling R70.17): the nearest Loss seer is read only for a concealed unit.
        Assert.True(ScenarioA1Sight.ActionLosesConcealment(true, () => 16));
        Assert.False(ScenarioA1Sight.ActionLosesConcealment(true, () => 17));
        Assert.False(ScenarioA1Sight.ActionLosesConcealment(true, () => null));
        Assert.False(ScenarioA1Sight.ActionLosesConcealment(false, () => throw new InvalidOperationException("the seers were read.")));
    }

    [Fact]
    public void AStackOutOfInspectionShowsItsTopAndCountsTheRest()
    {
        // A12.16 (ruling R70.18): after play, a counter in a stack out of the viewer's inspection is shown as before play: the top itself, the rest
        // counted, and the equipment beneath withheld; a stack in LOS is shown in full.
        var beneath = new CounterFacts(true, false, false, false, false, true, false, false) { OutOfInspection = true };
        var top = beneath with { IsTop = true };
        Assert.Equal(CounterPresence.SealedUninspected, ScenarioA1Visibility.UnitPresence(beneath, beforePlay: false));
        Assert.Equal(CounterPresence.Shown, ScenarioA1Visibility.UnitPresence(top, beforePlay: false));
        Assert.Equal(CounterPresence.Shown, ScenarioA1Visibility.UnitPresence(beneath with { OutOfInspection = false }, beforePlay: false));
        Assert.True(ScenarioA1Visibility.Withheld([beneath], beforePlay: false, sealAllowed: false));
        Assert.False(ScenarioA1Visibility.Withheld([beneath], beforePlay: false, sealAllowed: true));
        var weapon = new CounterFacts(true, false, false, false, false, false, false, true) { OutOfInspection = true };
        Assert.True(ScenarioA1Visibility.Withheld([weapon], beforePlay: false, sealAllowed: false));
        Assert.False(ScenarioA1Visibility.Withheld([weapon with { OutOfInspection = false }], beforePlay: false, sealAllowed: false));
    }
}
