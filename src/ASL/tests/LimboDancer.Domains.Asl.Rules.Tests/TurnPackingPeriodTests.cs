using System.Text.Json;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnPackingPeriodTests
{
    private static readonly TurnPackingSpecification Specification = new("pack", "british", new("gun", "mule", "crew"), TurnPackingWeapon.Gun, 10, false);
    private static readonly TurnPackingFacts Eligible = new(TurnPackingWeapon.Gun, true, true, true, true, true, true, true, true, null, true, true, true, true);
    private static TurnPackingPeriod Begin() => TurnPackingPeriods.Start(Specification, TurnPackingDirection.Unpack, new(2, true), Eligible);
    private static TurnPackingPeriod Reload(TurnPackingPeriod period) => JsonSerializer.Deserialize<TurnPackingPeriod>(JsonSerializer.Serialize(period))!;
    private static TurnPackingPeriod Next(TurnPackingPeriod period, int turn, bool first) => Reload(TurnPackingPeriods.NextRally(Reload(period), new(turn, first)));

    [Fact]
    public void OddPeriodUnloadsAtSecondRallyAndReleasesAfterTheWholePeriod()
    {
        var period = Begin();
        Assert.True(period.Loaded);
        Assert.Equal(Specification.Actors, Reload(period).Specification.Actors);
        Assert.False(TurnPackingPeriods.MidpointDue(period));
        period = Next(period, 2, false);
        period = Next(period, 3, true);
        Assert.False(TurnPackingPeriods.MidpointDue(period));
        period = Next(period, 3, false);
        Assert.True(TurnPackingPeriods.MidpointDue(period));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.NextRally(period, new(4, true)));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Interrupt(period));
        period = Reload(TurnPackingPeriods.ApplyMidpoint(period));
        Assert.False(period.Loaded);
        Assert.Equal(new TurnPackingRally(3, false), period.MidpointAt);
        Assert.True(period.WeaponAndMulePackTi);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.ApplyMidpoint(period));
        period = Next(period, 4, true);
        period = Next(period, 4, false);
        Assert.False(TurnPackingPeriods.CompletionDue(period));
        period = Next(period, 5, true);
        Assert.True(TurnPackingPeriods.CompletionDue(period));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.NextRally(period, new(5, false)));
        var completed = Reload(TurnPackingPeriods.Complete(period));
        Assert.True(completed.Completed);
        Assert.False(completed.WeaponAndMulePackTi);
        Assert.False(completed.PersonnelPackTi);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Complete(completed));
    }

    [Fact]
    public void InterruptionPreservesPriorTurnsAndDoesNotReleaseTheGunOrMule()
    {
        var period = Next(Next(Begin(), 2, false), 3, true);
        period = Reload(TurnPackingPeriods.Interrupt(period));
        Assert.Equal(1, period.CompletedGameTurns);
        Assert.Equal(new TurnPackingRally(3, true), period.InterruptedAt);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Resume(period, Eligible));
        Assert.True(period.WeaponAndMulePackTi);
        Assert.False(period.PersonnelPackTi);
        period = Next(period, 3, false);
        Assert.False(TurnPackingPeriods.MidpointDue(period));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Resume(period, Eligible));
        period = Next(period, 4, true);
        Assert.Equal(1, period.CompletedGameTurns);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Resume(period, Eligible with { GoodOrder = null }));
        period = Reload(TurnPackingPeriods.Resume(period, Eligible));
        period = Next(period, 4, false);
        period = TurnPackingPeriods.ApplyMidpoint(period);
        period = Reload(TurnPackingPeriods.Interrupt(period));
        Assert.False(period.Loaded);
        Assert.NotNull(period.MidpointAt);
        period = Next(period, 5, true);
        Assert.Equal(1, period.CompletedGameTurns);
        period = TurnPackingPeriods.Resume(period, Eligible);
        period = Next(Next(period, 5, false), 6, true);
        period = Next(Next(period, 6, false), 7, true);
        Assert.True(TurnPackingPeriods.CompletionDue(period));
        Assert.True(TurnPackingPeriods.Complete(period).Completed);
    }

    [Fact]
    public void PrintedG10ExampleLosesTurnThreeAndReleasesAtTurnSix()
    {
        var period = Next(Next(Begin(), 2, false), 3, true);
        period = TurnPackingPeriods.Interrupt(period);
        period = Next(Next(period, 3, false), 4, true);
        period = TurnPackingPeriods.Resume(period, Eligible);
        period = Next(period, 4, false);
        Assert.True(TurnPackingPeriods.MidpointDue(period));
        period = Reload(TurnPackingPeriods.ApplyMidpoint(period));
        Assert.Equal(new TurnPackingRally(4, false), period.MidpointAt);
        period = Next(Next(period, 5, true), 5, false);
        Assert.False(TurnPackingPeriods.CompletionDue(period));
        period = Next(period, 6, true);
        Assert.Equal(3, period.CompletedGameTurns);
        Assert.True(TurnPackingPeriods.Complete(period).Completed);
    }

    [Fact]
    public void ReversalKeepsThePhysicalLoadUntilItsNewMidpoint()
    {
        var period = Next(Next(Begin(), 2, false), 3, true);
        period = Reload(TurnPackingPeriods.Reverse(period));
        Assert.Equal(TurnPackingDirection.Pack, period.Direction);
        Assert.Equal(1, period.RequiredGameTurns);
        Assert.Equal(0, period.CompletedGameTurns);
        Assert.True(period.Loaded); // Already loaded because the original Unpacking midpoint had not occurred.
        period = Next(period, 3, false);
        period = TurnPackingPeriods.ApplyMidpoint(period);
        Assert.True(period.Loaded);
        period = Next(period, 4, true);
        Assert.True(TurnPackingPeriods.Complete(period).Loaded);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Reverse(period));
    }

    [Fact]
    public void NightPeriodUsesEvenMidpointAndPackDirectionLoadsAtMidpointOnly()
    {
        var period = TurnPackingPeriods.Start(Specification with
        {
            Night = true
        }, TurnPackingDirection.Pack, new(1, true), Eligible);
        Assert.Equal(6, period.RequiredGameTurns);
        Assert.False(period.Loaded);
        for (var turn = 1; turn <= 3; turn++)
        {
            period = Next(period, turn, false);
            Assert.False(TurnPackingPeriods.MidpointDue(period));
            period = Next(period, turn + 1, true);
        }
        Assert.True(TurnPackingPeriods.MidpointDue(period));
        period = TurnPackingPeriods.ApplyMidpoint(period);
        Assert.True(period.Loaded);
        period = TurnPackingPeriods.Reverse(period);
        Assert.Equal(3, period.RequiredGameTurns); // Net elapsed time is not doubled a second time.
        Assert.True(period.Loaded);
        Assert.Null(period.MidpointAt);
    }

    [Fact]
    public void AlteredLoadAndInterruptionReceiptsCannotAdvanceThePeriod()
    {
        var period = Next(Next(Next(Begin(), 2, false), 3, true), 3, false);
        period = Reload(TurnPackingPeriods.ApplyMidpoint(period));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.NextRally(period with { Loaded = true }, new(4, true)));
        period = Next(period, 4, true);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Interrupt(period with { InterruptedAt = new(5, true) }));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Interrupt(period with { InterruptedAt = new(4, false) }));
        period = TurnPackingPeriods.Interrupt(period);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Resume(period with { InterruptedAt = null }, Eligible));
        period = Next(Next(period, 4, false), 5, true);
        Assert.True(TurnPackingPeriods.Resume(Reload(period), Eligible).PersonnelPackTi);
    }

    [Fact]
    public void MissingActorsEligibilityAndDiscontinuousBoundariesAreRefused()
    {
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Start(Specification with { Actors = new("gun", "gun", "crew") }, TurnPackingDirection.Pack, new(1, true), Eligible));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Start(Specification, TurnPackingDirection.Pack, new(1, false), Eligible));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Start(Specification, TurnPackingDirection.Pack, new(1, true), Eligible with { Weapon = TurnPackingWeapon.LightMortar }));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Start(Specification with { ManhandlingNumber = null }, TurnPackingDirection.Pack, new(1, true), Eligible));
        var period = Begin();
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.NextRally(period, new(3, true)));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.NextRally(period, new(2, true)));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Complete(period));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.ApplyMidpoint(period));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Resume(period, Eligible));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Reverse(TurnPackingPeriods.Interrupt(period)));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Reverse(Next(period, 2, false)));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.NextRally(period with { FirstHalfCompleted = true }, new(2, false)));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.NextRally(period with { MidpointAt = new(3, true) }, new(2, false)));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.NextRally(period with { MidpointAt = new(2, false) }, new(2, false)));
        Assert.Throws<ArgumentNullException>(() => TurnPackingPeriods.Interrupt(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnPackingPeriods.Start(Specification, TurnPackingDirection.Pack, new(0, true), Eligible));
    }
}
