using System.Text;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class BacklogPass9Tests
{
    private async Task PrepareOriginalSpotterForMarks()
    {
        await Setup("german", 7, 1942,
            Squad("g1", "bd01:B10:0", "german"), Mortar("m1", "attacker-light-mortar", "g1", "german"),
            ReadySpotter("g2"), Squad("r1", "bd01:B6:0", "russian"));
        await Advance();
        Committed(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g2"
        }));
    }

    [Theory]
    [InlineData("pfph", "german", Conditions.PrepFire, true)]
    [InlineData("dfph", "russian", Conditions.FinalFire, false)]
    public async Task RetainedSpottersTakeTheNewShotsPhaseMarksWhileLegacyReplayKeepsItsReading(
        string phase, string phasingSide, string counter, bool barsMovement)
    {
        await PrepareOriginalSpotterForMarks();
        // Leave the designation's phase. Both later firing windows retain the original assignment.
        for (var steps = 0; steps < 17; steps++)
        {
            Committed(await Do(GameActions.AdvancePhase, new DiceRoller(_ => 5), new
            {
            }));
            if (Current.Phase == phase && Current.PhasingSide == phasingSide)
            {
                break;
            }
        }

        Assert.Equal(phase, Current.Phase);
        Assert.Equal(phasingSide, Current.PhasingSide);
        Assert.False(Is(Current.Unit("g2")!, counter));
        Assert.DoesNotContain("g2", Current.NoMoveThisPlayerTurn);
        Assert.Single(Current.MortarSpotterDesignations);
        Committed(await Shoot("m1", "bd01:B6:0", "g2", null, 2, 6));
        var record = store.Read(Scope)!;
        var shotEvent = Assert.Single(record.Events, item => item.Payload is OrdnanceFired);
        var shot = Assert.IsType<OrdnanceFired>(shotEvent.Payload);
        Assert.Equal(1, shot.SpotterWiringVersion);
        Assert.False(shot.Facts.TryGetProperty("spotterWiringVersion", out _));
        Assert.True(Is(Current.Unit("g2")!, counter));
        Assert.Equal(barsMovement, Current.NoMoveThisPlayerTurn.Contains("g2"));

        var written = GameEventWriter.Write(Scope, record);
        Assert.Contains("\"spotterWiringVersion\"", written, StringComparison.Ordinal);
        var loaded = GameEventReader.Read(Encoding.UTF8.GetBytes(written)).Record!;
        Assert.Equal(written, GameEventWriter.Write(Scope, loaded));
        Assert.Equal(1, Assert.IsType<OrdnanceFired>(Assert.Single(loaded.Events, item => item.Payload is OrdnanceFired).Payload).SpotterWiringVersion);
        var shotPrefix = loaded.Events.TakeWhile(item => item.EventId != shotEvent.EventId).Append(shotEvent).ToArray();
        var shotReplay = Planner().Replay(shotPrefix);
        Assert.False(shotReplay.HasErrors);
        Assert.True(Is(shotReplay.Current!.Unit("g2")!, counter));
        Assert.Equal(barsMovement, shotReplay.Current.NoMoveThisPlayerTurn.Contains("g2"));
        var replayed = Planner().Replay(loaded.Events);
        Assert.False(replayed.HasErrors);
        Assert.True(Is(replayed.Current!.Unit("g2")!, counter));
        Assert.Equal(barsMovement, replayed.Current.NoMoveThisPlayerTurn.Contains("g2"));

        var legacy = record with
        {
            Events = record.Events.Select(item => item.EventId == shotEvent.EventId
                ? item with
                {
                    Payload = shot with
                    {
                        SpotterWiringVersion = null
                    }
                } : item).ToArray()
        };
        var legacyWritten = GameEventWriter.Write(Scope, legacy);
        Assert.DoesNotContain("spotterWiringVersion", legacyWritten, StringComparison.Ordinal);
        var legacyLoaded = GameEventReader.Read(Encoding.UTF8.GetBytes(legacyWritten)).Record!;
        Assert.Equal(legacyWritten, GameEventWriter.Write(Scope, legacyLoaded));
        var legacyShot = Assert.Single(legacyLoaded.Events, item => item.Payload is OrdnanceFired);
        Assert.Null(Assert.IsType<OrdnanceFired>(legacyShot.Payload).SpotterWiringVersion);
        var legacyPrefix = legacyLoaded.Events.TakeWhile(item => item.EventId != shotEvent.EventId).Append(legacyShot).ToArray();
        var legacyShotReplay = Planner().Replay(legacyPrefix);
        Assert.False(legacyShotReplay.HasErrors);
        Assert.False(Is(legacyShotReplay.Current!.Unit("g2")!, counter));
        Assert.DoesNotContain("g2", legacyShotReplay.Current.NoMoveThisPlayerTurn);
        var oldReplay = Planner().Replay(legacyLoaded.Events);
        Assert.False(oldReplay.HasErrors);
        // The legacy command also recorded an explicit conditions-changed companion event.
        Assert.True(Is(oldReplay.Current!.Unit("g2")!, counter));
        Assert.DoesNotContain("g2", oldReplay.Current.NoMoveThisPlayerTurn);
        Assert.Contains(oldReplay.Current.SpottedThisPhase, item => item.Spotter == "g2");
    }

    [Fact]
    public async Task AlreadyMarkedSpotterKeepsOneMovementBarAndOneWeaponUseAcrossRepeatedShots()
    {
        await PrepareOriginalSpotterForMarks();
        Assert.True(Is(Current.Unit("g2")!, Conditions.PrepFire));
        Committed(await Shoot("m1", "bd01:B6:0", "g2", null, 2, 6));
        Committed(await Shoot("m1", "bd01:B6:0", "g2", null, 2, 6));
        Assert.True(Is(Current.Unit("g2")!, Conditions.PrepFire));
        Assert.Single(Current.NoMoveThisPlayerTurn, id => id == "g2");
        Assert.Single(Current.SpottedThisPhase, item => item.Spotter == "g2" && item.Gun == "m1");
        Assert.Single(Current.SupportWeaponUses, item => item.Unit == "g2" && item.Weapon == "m1");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task UnknownSpotterWiringVersionCannotReplayAnOtherwiseValidShot()
    {
        await PrepareOriginalSpotterForMarks();
        Committed(await Shoot("m1", "bd01:B6:0", "g2", null, 2, 6));
        var events = store.Read(Scope)!.Events.Select(item => item.Payload is OrdnanceFired shot
            ? item with
            {
                Payload = shot with
                {
                    SpotterWiringVersion = 2
                }
            } : item).ToArray();
        Assert.True(Planner().Replay(events).HasErrors);
    }
}
