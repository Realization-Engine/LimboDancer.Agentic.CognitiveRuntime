using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

public sealed class TurnPackingRecoveryJournalTests
{
    private static readonly GameScope Scope = new(Guid.Parse("d71fe88d-cfcb-4722-9c58-253880c005ad"), "recovery");
    private static readonly TurnPackingSpecification Spec = new("recovery-period", "german", new("gun", "mule", "crew"), TurnPackingWeapon.Mortar76To82, 11, false);
    private static readonly TurnPackingFacts Facts = new(TurnPackingWeapon.Mortar76To82, null, true, true, null, false, false,
        true, true, null, true, true, true, false);
    private static TurnPackingCasualtyDocument Casualties() => new(1, Scope,
        new("german", Spec.Actors, new("origin", 1), new(TurnPackingWeapon.Mortar76To82, TurnHorseSize.Squad, true, false, false, 0, TurnPackingRecovery.None, true, true)),
        [new(1, "casualty-effects", 4, null, new("mule-loss", 2), new("vulnerability-dr", 3), TurnPackingMuleLoss.Eliminated, 6, TurnPackingUnloadTerrain.Allowed)]);
    private static TurnPackingJournalEntry Start() => new(1, "start", 6, null, TurnPackingOperation.RecoveryStart, new("work-2-first", 5, new(2, true)),
        Spec, TurnPackingDirection.Unpack, Facts, Casualties());
    private static TurnPackingJournalEntry Second() => new(1, "second", 8, "start", TurnPackingOperation.Rally, new("work-2-second", 7, new(2, false)));
    private static TurnPackingHistory Reload(params TurnPackingJournalEntry[] entries) =>
        TurnPackingJournal.Replay(TurnPackingJournal.Read(TurnPackingJournal.Write(Scope, entries), Scope), Scope);

    [Fact]
    public void CasualtyBoundRecoveryReplaysToReleaseWithoutAMidpointOrPhantomMule()
    {
        var start = Start();
        var second = Second();
        var third = new TurnPackingJournalEntry(1, "third", 10, "second", TurnPackingOperation.Rally, new("work-3-first", 9, new(3, true)));
        var complete = new TurnPackingJournalEntry(1, "complete", 11, "third", TurnPackingOperation.Complete, third.WorkStep);
        var before = Reload(start);
        var history = Reload(start, second, third, complete);
        Assert.True(history.Period.Completed);
        Assert.False(history.Period.Loaded);
        Assert.False(history.Period.MulePackTi);
        Assert.Null(history.Period.MidpointAt);
        Assert.NotNull(history.Period.RecoveryOrigin);
        Assert.Equal(1, history.Period.CompletedGameTurns);
        Assert.Equal(Spec.Actors, history.Period.Specification.Actors);
        Assert.DoesNotContain(history.Receipts, receipt => receipt.Operation == TurnPackingOperation.Midpoint);
        Assert.True(before.Period.WeaponPackTi);
        Assert.False(before.Period.Completed);
        Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Replay([start]));
    }

    [Fact]
    public void RecoveryRequiresItsScopeActorsFreshStepAndFullRecoveryReceipt()
    {
        var start = Start();
        TurnPackingJournalEntry[] invalid = [start with { Recovery = null }, start with { Direction = TurnPackingDirection.Pack },
            start with { Recovery = Casualties() with { Scope = Scope with { Game = "other" } } },
            start with { Specification = Spec with { Owner = "other" } }, start with { Specification = Spec with { Actors = new("gun", "replacement-mule", "crew") } },
            start with { WorkStep = start.WorkStep with { Revision = 4 } }, start with { EventId = "mule-loss" },
            start with { WorkStep = start.WorkStep with { EventId = "vulnerability-dr" } },
            start with { Recovery = Casualties() with { Origin = Casualties().Origin with { ExistingPeriodId = Spec.Id } } },
            start with { Operation = TurnPackingOperation.Start }];
        foreach (var entry in invalid)
        {
            Assert.Throws<InvalidDataException>(() => Reload(entry));
        }
        var existing = Casualties() with
        {
            Origin = Casualties().Origin with
            {
                State = Casualties().Origin.State with
                {
                    Loaded = false,
                    MulePackTi = true,
                    Recovery = TurnPackingRecovery.ContinueExistingPeriod
                }
            }
        };
        Assert.Throws<InvalidDataException>(() => Reload(start with { Recovery = existing }));
    }

    [Fact]
    public void LaterEntriesCannotReplaceCasualtiesRepeatPhysicalUnloadOrReuseTheirSources()
    {
        var start = Start();
        var second = Second();
        Assert.Throws<InvalidDataException>(() => Reload(start, second with { Recovery = Casualties() }));
        Assert.Throws<InvalidDataException>(() => Reload(start, second with { EventId = "casualty-effects" }));
        Assert.Throws<InvalidDataException>(() => Reload(start, second with { WorkStep = second.WorkStep with { EventId = "origin" } }));
        Assert.Throws<InvalidDataException>(() => Reload(start, new(1, "midpoint", 7, "start", TurnPackingOperation.Midpoint, start.WorkStep)));
        Assert.Throws<InvalidDataException>(() => Reload(start, new(1, "reverse", 7, "start", TurnPackingOperation.Reverse, start.WorkStep)));
    }

    [Fact]
    public void NestedCasualtyFactsMustRemainExplicitInTheOriginalJson()
    {
        var json = TurnPackingJournal.Write(Scope, [Start()]);
        var document = JsonNode.Parse(json)!;
        Assert.True(document["entries"]![0]!["recovery"]!["origin"]!["state"]!.AsObject().Remove("loaded"));
        Assert.Throws<JsonException>(() => TurnPackingJournal.Read(document.ToJsonString(), Scope));
        var read = TurnPackingJournal.Read(json.Replace("\"entries\"", "\"Entries\"", StringComparison.Ordinal), Scope);
        Assert.Equal(TurnPackingOperation.RecoveryStart, Assert.Single(read).Operation);
    }
}
