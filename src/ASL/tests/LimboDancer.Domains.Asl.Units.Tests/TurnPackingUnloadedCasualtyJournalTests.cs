using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

public sealed class TurnPackingUnloadedCasualtyJournalTests
{
    private static readonly GameScope Scope = new(Guid.Parse("d71fe88d-cfcb-4722-9c58-253880c005ad"), "unloaded-casualty");
    private static readonly TurnPackingSpecification Spec = new("period", "german", new("gun", "mule", "crew"), TurnPackingWeapon.Gun, 10, false);
    private static readonly TurnPackingFacts Facts = new(TurnPackingWeapon.Gun, true, true, true, true, true, true,
        true, true, null, true, true, true, true);
    private static readonly TurnPackingCasualtyState Origin = new(TurnPackingWeapon.Gun, TurnHorseSize.Squad, false, true, false, 0,
        TurnPackingRecovery.ContinueExistingPeriod, true, true);

    private static List<TurnPackingJournalEntry> TurnFive()
    {
        List<TurnPackingJournalEntry> entries = [new(1, "start", 2, null, TurnPackingOperation.Start, new("work-2-first", 1, new(2, true)), Spec, TurnPackingDirection.Unpack, Facts)];
        Add(entries, TurnPackingOperation.Rally, new(2, false));
        Add(entries, TurnPackingOperation.Rally, new(3, true));
        Add(entries, TurnPackingOperation.Interrupt);
        Add(entries, TurnPackingOperation.Rally, new(3, false));
        Add(entries, TurnPackingOperation.Rally, new(4, true));
        Add(entries, TurnPackingOperation.Resume);
        Add(entries, TurnPackingOperation.Rally, new(4, false));
        Add(entries, TurnPackingOperation.Midpoint);
        Add(entries, TurnPackingOperation.Rally, new(5, true));
        return entries;
    }

    private static void Add(List<TurnPackingJournalEntry> entries, TurnPackingOperation operation, TurnPackingRally? rally = null)
    {
        var prior = entries[^1];
        var revision = prior.Revision + 2;
        var step = rally is null ? prior.WorkStep : new TurnPackingWorkStep($"work-{revision}", revision - 1, rally);
        entries.Add(new(1, $"event-{revision}", revision, prior.EventId, operation, step, Eligibility: operation == TurnPackingOperation.Resume ? Facts : null));
    }

    private static TurnPackingJournalEntry Casualty(TurnPackingJournalEntry prior, TurnPackingCasualtyState? origin = null,
        TurnPackingMuleLoss loss = TurnPackingMuleLoss.CasualtyReduced)
    {
        var revision = prior.Revision;
        var document = new TurnPackingCasualtyDocument(1, Scope,
            new(Spec.Owner, Spec.Actors, new(prior.EventId, revision), origin ?? Origin, Spec.Id),
            [new(1, $"effects-{revision}", revision + 3, null, new($"loss-{revision}", revision + 1), new($"die-{revision}", revision + 2), loss, 3, TurnPackingUnloadTerrain.Unknown)]);
        return new(1, $"period-casualty-{revision}", revision + 4, prior.EventId, TurnPackingOperation.Casualty, prior.WorkStep, Casualty: document);
    }

    private static TurnPackingHistory Reload(IReadOnlyList<TurnPackingJournalEntry> entries) =>
        TurnPackingJournal.Replay(TurnPackingJournal.Read(TurnPackingJournal.Write(Scope, entries), Scope), Scope);

    [Fact]
    public void PrintedExampleReplaysMidpointCasualtyAndReleaseWithoutRestartingThePeriod()
    {
        var entries = TurnFive();
        var before = Reload(entries);
        entries.Add(Casualty(entries[^1]));
        var casualty = Reload(entries);
        Assert.Equal(before.Period, casualty.Period with
        {
            LatestCasualty = null
        });
        Assert.Equal(1, casualty.Period.LatestCasualty!.LowAmmoOccurrences);
        Assert.Equal(TurnHorseSize.HalfSquad, casualty.Period.LatestCasualty.MuleSize);
        Assert.Equal(TurnPackingOperation.Casualty, casualty.Receipts[^1].Operation);
        Assert.Equal(casualty.Receipts[^2].WorkStep, casualty.Receipts[^1].WorkStep);
        Add(entries, TurnPackingOperation.Rally, new(5, false));
        Add(entries, TurnPackingOperation.Rally, new(6, true));
        Add(entries, TurnPackingOperation.Complete);
        var complete = Reload(entries);
        Assert.True(complete.Period.Completed);
        Assert.Equal(new(6, true), complete.Period.LastRally);
        Assert.Equal(Spec.Id, complete.Period.Specification.Id);
        Assert.Single(complete.Receipts, item => item.Operation == TurnPackingOperation.Midpoint);
        Assert.Single(complete.Receipts, item => item.Operation == TurnPackingOperation.Start);
        Assert.False(before.Period.Completed);
        Assert.Null(before.Period.LatestCasualty);
    }

    [Fact]
    public void FurtherLossRequiresThePriorCasualtySnapshotAndPreservesAccumulatedAmmo()
    {
        var entries = TurnFive();
        entries.Add(Casualty(entries[^1]));
        var state = Reload(entries).Period.LatestCasualty!;
        var elimination = Casualty(entries[^1], state, TurnPackingMuleLoss.Eliminated);
        Assert.Throws<InvalidDataException>(() => Reload([.. entries, elimination with { Casualty = elimination.Casualty! with { Origin = elimination.Casualty.Origin with { State = state with { LowAmmoOccurrences = 0 } } } }]));
        entries.Add(elimination);
        var history = Reload(entries);
        Assert.Equal(2, history.Period.LatestCasualty!.LowAmmoOccurrences);
        Assert.False(history.Period.MulePackTi);
        Assert.True(history.Period.WeaponPackTi);
        Assert.Equal(2, history.Period.CompletedGameTurns);
        Assert.Throws<InvalidDataException>(() => Reload([.. entries, Casualty(entries[^1], history.Period.LatestCasualty, TurnPackingMuleLoss.Eliminated)]));
    }

    [Fact]
    public void ActiveFullRecoveryAcceptsFurtherLossWithoutASecondStartOrUnload()
    {
        var forced = new TurnPackingCasualtyDocument(1, Scope,
            new(Spec.Owner, Spec.Actors, new("loaded-origin", 1), Origin with
            {
                Loaded = true,
                MulePackTi = false,
                Recovery = TurnPackingRecovery.None
            }),
            [new(1, "forced-unload", 4, null, new("first-loss", 2), new("first-die", 3), TurnPackingMuleLoss.CasualtyReduced, 3, TurnPackingUnloadTerrain.Allowed)]);
        List<TurnPackingJournalEntry> entries = [new(1, "recovery-start", 6, null, TurnPackingOperation.RecoveryStart,
            new("recovery-work", 5, new(2, true)), Spec, TurnPackingDirection.Unpack, Facts, forced)];
        Add(entries, TurnPackingOperation.Rally, new(2, false));
        Add(entries, TurnPackingOperation.Rally, new(3, true));
        var before = Reload(entries).Period;
        var further = Casualty(entries[^1], before.RecoveryOrigin, TurnPackingMuleLoss.Eliminated);
        var reusedDie = further.Casualty!.Entries[0] with
        {
            Roll = new("first-die", further.Casualty.Entries[0].Roll.Revision)
        };
        Assert.Throws<InvalidDataException>(() => Reload([.. entries, further with { Casualty = further.Casualty with { Entries = [reusedDie] } }]));
        entries.Add(further);
        var after = Reload(entries).Period;
        Assert.Equal(before, after with
        {
            LatestCasualty = null
        });
        Assert.Equal(TurnPackingRecovery.FullOrdinaryPeriod, after.LatestCasualty!.Recovery);
        Assert.Equal(2, after.LatestCasualty.LowAmmoOccurrences);
        Assert.True(after.LatestCasualty.Malfunctioned);
        Assert.False(after.MulePackTi);
        for (var turn = 3; turn < 5; turn++)
        {
            Add(entries, TurnPackingOperation.Rally, new(turn, false));
            Add(entries, TurnPackingOperation.Rally, new(turn + 1, true));
        }
        Add(entries, TurnPackingOperation.Complete);
        var complete = Reload(entries);
        Assert.True(complete.Period.Completed);
        Assert.Equal(3, complete.Period.CompletedGameTurns);
        Assert.Null(complete.Period.MidpointAt);
        Assert.DoesNotContain(complete.Receipts, item => item.Operation == TurnPackingOperation.Midpoint);
        Assert.Single(complete.Receipts, item => item.Operation == TurnPackingOperation.RecoveryStart);
    }

    [Fact]
    public void CasualtyRequiresExactScopeOwnerActorsPeriodAndPreviousStateSource()
    {
        var entries = TurnFive();
        var entry = Casualty(entries[^1]);
        var document = entry.Casualty!;
        TurnPackingCasualtyDocument[] invalid = [document with { Scope = Scope with { Game = "other" } },
            document with { Origin = document.Origin with { Owner = "other" } },
            document with { Origin = document.Origin with { Actors = new("gun", "mule", "replacement-crew") } },
            document with { Origin = document.Origin with { ExistingPeriodId = null } },
            document with { Origin = document.Origin with { ExistingPeriodId = "other" } },
            document with { Origin = document.Origin with { Source = new("unbound-snapshot", entries[^1].Revision) } },
            document with { Origin = document.Origin with { Source = document.Origin.Source with { Revision = entries[^1].Revision - 1 } } },
            document with { Origin = document.Origin with { State = Origin with { Loaded = true } } }];
        foreach (var changed in invalid)
        {
            Assert.Throws<InvalidDataException>(() => Reload([.. entries, entry with { Casualty = changed }]));
        }
        Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Replay([.. entries, entry]));
        Assert.Throws<InvalidDataException>(() => Reload([.. entries, entry with { Revision = document.Entries[^1].Revision }]));
    }

    [Fact]
    public void CasualtyCannotChangeWorkStepOrInjectStartAndResumeEvidence()
    {
        var entries = TurnFive();
        var entry = Casualty(entries[^1]);
        TurnPackingJournalEntry[] invalid = [entry with { Casualty = null }, entry with { Specification = Spec },
            entry with { Direction = TurnPackingDirection.Unpack }, entry with { Eligibility = Facts }, entry with { Recovery = entry.Casualty },
            entry with { WorkStep = entry.WorkStep with { EventId = "invented-work-step" } }, entry with { Operation = TurnPackingOperation.Interrupt }];
        foreach (var changed in invalid)
        {
            Assert.Throws<InvalidDataException>(() => Reload([.. entries, changed]));
        }
        Assert.Throws<InvalidDataException>(() => Reload([entries[0] with { Casualty = entry.Casualty }]));
        Assert.Throws<InvalidDataException>(() => Reload([entries[0], Casualty(entries[0])]));
    }

    [Fact]
    public void CasualtyAndDiceCannotReuseWorkStepOrPeriodIdentitiesInEitherDirection()
    {
        var entries = TurnFive();
        var entry = Casualty(entries[^1]);
        var document = entry.Casualty!;
        var effect = document.Entries[0];
        TurnPackingCasualtyEntry[] invalid = [effect with { Casualty = effect.Casualty with { EventId = entries[0].EventId } },
            effect with { Roll = effect.Roll with { EventId = entries[0].WorkStep.EventId } }, effect with { EventId = entry.EventId }];
        foreach (var changed in invalid)
        {
            Assert.Throws<InvalidDataException>(() => Reload([.. entries, entry with { Casualty = document with { Entries = [changed] } }]));
        }
        entries.Add(entry);
        Add(entries, TurnPackingOperation.Rally, new(5, false));
        var rally = entries[^1];
        Assert.Throws<InvalidDataException>(() => Reload([.. entries.Take(entries.Count - 1), rally with { EventId = effect.Roll.EventId }]));
        Assert.Throws<InvalidDataException>(() => Reload([.. entries.Take(entries.Count - 1), rally with { WorkStep = rally.WorkStep with { EventId = effect.EventId } }]));
        Assert.Throws<InvalidDataException>(() => Reload([.. entries.Take(entries.Count - 1), rally with { WorkStep = rally.WorkStep with { Revision = entry.Revision } }]));
    }

    [Fact]
    public void OriginalNestedJsonMustSupplyCasualtyFactsWhileExplicitUnknownsSurvive()
    {
        var entries = TurnFive();
        entries.Add(Casualty(entries[^1], Origin with
        {
            Malfunctioned = null,
            LowAmmoOccurrences = null
        }));
        var json = TurnPackingJournal.Write(Scope, entries);
        var read = TurnPackingJournal.Read(json.Replace("\"casualty\"", "\"Casualty\"", StringComparison.Ordinal), Scope);
        var history = TurnPackingJournal.Replay(read, Scope);
        Assert.Null(history.Period.LatestCasualty!.LowAmmoOccurrences);
        Assert.Null(history.Period.LatestCasualty.Malfunctioned);
        var document = JsonNode.Parse(json)!;
        Assert.True(document["entries"]![entries.Count - 1]!["casualty"]!["origin"]!["state"]!.AsObject().Remove("loaded"));
        Assert.Throws<JsonException>(() => TurnPackingJournal.Read(document.ToJsonString(), Scope));
    }
}
