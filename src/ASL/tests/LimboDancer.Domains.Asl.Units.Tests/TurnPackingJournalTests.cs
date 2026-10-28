using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

public sealed class TurnPackingJournalTests
{
    private static readonly GameScope Scope = new(Guid.Parse("d71fe88d-cfcb-4722-9c58-253880c005ad"), "packing");
    private static readonly TurnPackingSpecification Specification = new("pack", "british", new("gun", "mule", "crew"), TurnPackingWeapon.Gun, 10, false);
    private static readonly TurnPackingFacts Eligible = new(TurnPackingWeapon.Gun, true, true, true, true, true, true, true, true, null, true, true, true, true);
    private static TurnPackingJournalEntry Start() => new(1, "start", 2, null, TurnPackingOperation.Start, new("rph-2-first", 1, new(2, true)), Specification, TurnPackingDirection.Unpack, Eligible);

    private static void Add(List<TurnPackingJournalEntry> entries, TurnPackingOperation operation, TurnPackingRally? rally = null, TurnPackingFacts? facts = null)
    {
        var prior = entries[^1];
        var revision = prior.Revision + 2;
        var step = rally is null ? prior.WorkStep : new TurnPackingWorkStep($"step-{revision - 1}", revision - 1, rally);
        entries.Add(new(1, $"event-{revision}", revision, prior.EventId, operation, step, Eligibility: facts));
    }
    private static TurnPackingHistory Reload(IReadOnlyList<TurnPackingJournalEntry> entries) => TurnPackingJournal.Replay(TurnPackingJournal.Read(TurnPackingJournal.Write(Scope, entries), Scope));

    [Fact]
    public void JournalReplaysPrintedExampleWithDistinctActorsAndCausalMidpointAndReleaseReceipts()
    {
        List<TurnPackingJournalEntry> entries = [Start()];
        Add(entries, TurnPackingOperation.Rally, new(2, false));
        Add(entries, TurnPackingOperation.Rally, new(3, true));
        Add(entries, TurnPackingOperation.Interrupt);
        Add(entries, TurnPackingOperation.Rally, new(3, false));
        Add(entries, TurnPackingOperation.Rally, new(4, true));
        Add(entries, TurnPackingOperation.Resume, facts: Eligible);
        Add(entries, TurnPackingOperation.Rally, new(4, false));
        Add(entries, TurnPackingOperation.Midpoint);
        var unloaded = Reload(entries);
        Assert.False(unloaded.Period.Loaded);
        Assert.True(unloaded.Period.WeaponAndMulePackTi);
        Assert.Equal(Specification.Actors, unloaded.Period.Specification.Actors);
        Assert.Equal(new TurnPackingRally(4, false), Assert.Single(unloaded.Receipts, item => item.Operation == TurnPackingOperation.Midpoint).WorkStep.Rally);
        Add(entries, TurnPackingOperation.Rally, new(5, true));
        Add(entries, TurnPackingOperation.Rally, new(5, false));
        Add(entries, TurnPackingOperation.Rally, new(6, true));
        Add(entries, TurnPackingOperation.Complete);
        var completed = Reload(entries);
        Assert.True(completed.Period.Completed);
        Assert.False(completed.Period.WeaponAndMulePackTi);
        Assert.Equal(entries.Count, completed.Receipts.Count);
        Assert.Equal(entries[^1].EventId, completed.Receipts[^1].EventId);
        Assert.False(unloaded.Period.Completed); // Earlier history is immutable.
    }

    [Fact]
    public void ReversalReplaysWithoutReplacingActorsOrUndoingPhysicalLoad()
    {
        List<TurnPackingJournalEntry> entries = [Start()];
        Add(entries, TurnPackingOperation.Rally, new(2, false));
        Add(entries, TurnPackingOperation.Rally, new(3, true));
        Add(entries, TurnPackingOperation.Reverse);
        var reversed = Reload(entries);
        Assert.Equal(1, reversed.Period.RequiredGameTurns);
        Assert.Equal(TurnPackingDirection.Pack, reversed.Period.Direction);
        Assert.True(reversed.Period.Loaded);
        Assert.Equal(Specification, reversed.Period.Specification);
        Add(entries, TurnPackingOperation.Rally, new(3, false));
        Add(entries, TurnPackingOperation.Midpoint);
        Add(entries, TurnPackingOperation.Rally, new(4, true));
        Add(entries, TurnPackingOperation.Complete);
        Assert.True(Reload(entries).Period.Completed);
    }

    [Fact]
    public void ResumeCannotEraseInterruptionOrUseUnknownEligibility()
    {
        List<TurnPackingJournalEntry> entries = [Start()];
        Add(entries, TurnPackingOperation.Interrupt);
        Add(entries, TurnPackingOperation.Resume, facts: Eligible);
        Assert.Throws<InvalidDataException>(() => Reload(entries));
        entries.RemoveAt(entries.Count - 1);
        Add(entries, TurnPackingOperation.Rally, new(2, false));
        Add(entries, TurnPackingOperation.Rally, new(3, true));
        Add(entries, TurnPackingOperation.Resume, facts: Eligible with
        {
            GoodOrder = null
        });
        Assert.Throws<InvalidDataException>(() => Reload(entries));
        entries[^1] = entries[^1] with
        {
            Eligibility = Eligible
        };
        Assert.True(Reload(entries).Period.PersonnelPackTi);
        Assert.Equal(0, Reload(entries).Period.CompletedGameTurns);
    }

    [Fact]
    public void MissingMidpointCannotBeSkippedAndDuplicatePhysicalEffectsAreRefused()
    {
        List<TurnPackingJournalEntry> entries = [Start()];
        Add(entries, TurnPackingOperation.Rally, new(2, false));
        Add(entries, TurnPackingOperation.Rally, new(3, true));
        Add(entries, TurnPackingOperation.Rally, new(3, false));
        Add(entries, TurnPackingOperation.Rally, new(4, true));
        Assert.Throws<InvalidDataException>(() => Reload(entries));
        entries.RemoveAt(entries.Count - 1);
        Add(entries, TurnPackingOperation.Midpoint);
        var snapshot = Reload(entries);
        Add(entries, TurnPackingOperation.Midpoint);
        Assert.Throws<InvalidDataException>(() => Reload(entries));
        Assert.False(snapshot.Period.Loaded);
        Assert.Single(snapshot.Receipts, item => item.Operation == TurnPackingOperation.Midpoint);
    }

    [Fact]
    public void UnsupportedAndMalformedEntriesNeverBecomeTrustedHistory()
    {
        var start = Start();
        TurnPackingJournalEntry[] malformed =
        [
            start with { Version = 0 }, start with { Version = 2 }, start with { EventId = "" },
            start with { Revision = 0 }, start with { PreviousEventId = "missing" },
            start with { Operation = (TurnPackingOperation)99 }, start with { Operation = TurnPackingOperation.Complete },
            start with { WorkStep = null! }, start with { WorkStep = start.WorkStep with { Rally = null! } },
            start with { WorkStep = start.WorkStep with { EventId = "" } }, start with { WorkStep = start.WorkStep with { EventId = start.EventId } },
            start with { WorkStep = start.WorkStep with { Revision = 0 } }, start with { WorkStep = start.WorkStep with { Revision = start.Revision } },
            start with { WorkStep = start.WorkStep with { Rally = new(0, true) } },
            start with { Specification = null }, start with { Direction = null }, start with { Eligibility = null },
            start with { Specification = Specification with { Actors = new("gun", "gun", "crew") } }
        ];
        foreach (var entry in malformed)
        {
            Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Read(JsonSerializer.Serialize(new TurnPackingJournalDocument(1, Scope, [entry]), JsonSerializerOptions.Web), Scope));
        }
        Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Read("null", Scope));
        Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Read(JsonSerializer.Serialize(new TurnPackingJournalDocument(1, Scope, []), JsonSerializerOptions.Web), Scope));
        Assert.Throws<JsonException>(() => TurnPackingJournal.Read("{", Scope));
        Assert.Throws<ArgumentNullException>(() => TurnPackingJournal.Replay(null!));
        Assert.Throws<ArgumentNullException>(() => TurnPackingJournal.Replay([null!]));
    }

    [Fact]
    public void CodecBindsTheJournalToItsTenantAndGameAndRejectsUnknownSchema()
    {
        var json = TurnPackingJournal.Write(Scope, [Start()]);
        Assert.Single(TurnPackingJournal.Read(json, Scope));
        Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Read(json, Scope with { Game = "other" }));
        Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Read(json, Scope with { Tenant = Guid.NewGuid() }));
        var document = JsonSerializer.Deserialize<TurnPackingJournalDocument>(json, JsonSerializerOptions.Web)!;
        Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Read(JsonSerializer.Serialize(document with { SchemaVersion = 2 }, JsonSerializerOptions.Web), Scope));
        Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Read(JsonSerializer.Serialize(document with { Entries = null! }, JsonSerializerOptions.Web), Scope));
        Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Read(JsonSerializer.Serialize(document with { Scope = null! }, JsonSerializerOptions.Web), Scope));
        Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Write(Scope with { Tenant = Guid.Empty }, [Start()]));
        Assert.Throws<InvalidDataException>(() => TurnPackingJournal.Write(Scope with { Game = "" }, [Start()]));
        Assert.Throws<ArgumentNullException>(() => TurnPackingJournal.Read(json, null!));
    }

    [Fact]
    public void ReorderedDuplicatedAndAlteredCausalEntriesAreRejected()
    {
        List<TurnPackingJournalEntry> entries = [Start()];
        Add(entries, TurnPackingOperation.Rally, new(2, false));
        var next = entries[^1];
        TurnPackingJournalEntry[] changed =
        [
            next with { EventId = entries[0].EventId }, next with { EventId = entries[0].WorkStep.EventId }, next with { Revision = entries[0].Revision },
            next with { PreviousEventId = null }, next with { PreviousEventId = "other" },
            next with { WorkStep = next.WorkStep with { EventId = entries[0].WorkStep.EventId } },
            next with { WorkStep = next.WorkStep with { EventId = entries[0].EventId } },
            next with { WorkStep = next.WorkStep with { Revision = entries[0].Revision } },
            next with { Specification = Specification }, next with { Direction = TurnPackingDirection.Pack },
            next with { Eligibility = Eligible }, next with { Operation = TurnPackingOperation.Start },
            next with { Operation = TurnPackingOperation.Resume },
            next with { WorkStep = next.WorkStep with { Rally = new(3, true) } }
        ];
        foreach (var entry in changed)
        {
            Assert.Throws<InvalidDataException>(() => Reload([entries[0], entry]));
        }
        Assert.Throws<InvalidDataException>(() => Reload([next, entries[0]]));
        var interrupted = next with
        {
            Operation = TurnPackingOperation.Interrupt,
            WorkStep = entries[0].WorkStep
        };
        Assert.False(Reload([entries[0], interrupted]).Period.PersonnelPackTi);
        Assert.Throws<InvalidDataException>(() => Reload([entries[0], interrupted with { WorkStep = next.WorkStep }]));
    }
}
