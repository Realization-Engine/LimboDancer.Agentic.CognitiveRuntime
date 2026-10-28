using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

public enum TurnPackingOperation
{
    Start, Rally, Midpoint, Interrupt, Resume, Reverse, Complete, RecoveryStart, Casualty
}

/// <summary>Evidence identifying an ASOP 1.13B work step, not inferred from a phase name. The admitting host must verify its source.</summary>
public sealed record TurnPackingWorkStep(string EventId, long Revision, TurnPackingRally Rally);

/// <summary>A versioned period journal entry. Eligibility evidence belongs only to Start/RecoveryStart/Resume; it is not an adjudication permission.</summary>
public sealed record TurnPackingJournalEntry(int Version, string EventId, long Revision, string? PreviousEventId,
    TurnPackingOperation Operation, TurnPackingWorkStep WorkStep, TurnPackingSpecification? Specification = null,
    TurnPackingDirection? Direction = null, TurnPackingFacts? Eligibility = null, TurnPackingCasualtyDocument? Recovery = null,
    TurnPackingCasualtyDocument? Casualty = null);

public sealed record TurnPackingJournalDocument(int SchemaVersion, GameScope Scope, IReadOnlyList<TurnPackingJournalEntry> Entries);

public sealed record TurnPackingReceipt(string EventId, long Revision, TurnPackingOperation Operation, TurnPackingWorkStep WorkStep);
public sealed record TurnPackingHistory(TurnPackingPeriod Period, IReadOnlyList<TurnPackingReceipt> Receipts);

/// <summary>
/// D3.2 durable period codec/replay. This is a separate, not-yet-admitted journal format, not a GameEvent protocol.
/// Hosts must verify real actors, eligibility, work-step origins and atomic piece effects before accepting entries.
/// Replay checks causal integrity and Rules transitions; it cannot authenticate supplied evidence.
/// </summary>
public static class TurnPackingJournal
{
    public const int Version = 1;

    public static string Write(GameScope scope, IReadOnlyList<TurnPackingJournalEntry> entries)
    {
        ValidateScope(scope);
        _ = Replay(entries, scope);
        return JsonSerializer.Serialize(new TurnPackingJournalDocument(Version, scope, entries), JsonSerializerOptions.Web);
    }

    public static IReadOnlyList<TurnPackingJournalEntry> Read(string json, GameScope expectedScope)
    {
        ArgumentNullException.ThrowIfNull(json);
        ValidateScope(expectedScope);
        var document = JsonSerializer.Deserialize<TurnPackingJournalDocument>(json, JsonSerializerOptions.Web)
            ?? throw new InvalidDataException("Missing packing journal.");
        if (document.SchemaVersion != Version || document.Scope != expectedScope || document.Entries is null)
        {
            throw new InvalidDataException("Unsupported packing journal schema, missing entries or different game scope.");
        }
        // Keep legacy parsing unchanged; nested casualty facts must be validated before missing JSON can turn into defaults.
        if (document.Entries.Any(entry => entry is { Recovery: not null } or { Casualty: not null }))
        {
            using var raw = JsonDocument.Parse(json);
            var rawEntries = raw.RootElement.EnumerateObject().Last(property => property.Name.Equals("entries", StringComparison.OrdinalIgnoreCase)).Value;
            foreach (var entry in rawEntries.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object))
            {
                foreach (var property in entry.EnumerateObject().Where(property => (property.Name.Equals("recovery", StringComparison.OrdinalIgnoreCase) || property.Name.Equals("casualty", StringComparison.OrdinalIgnoreCase)) && property.Value.ValueKind != JsonValueKind.Null))
                {
                    _ = TurnPackingCasualtyJournal.Read(property.Value.GetRawText(), expectedScope);
                }
            }
        }
        _ = Replay(document.Entries, expectedScope);
        return Array.AsReadOnly(document.Entries.ToArray());
    }

    public static TurnPackingHistory Replay(IReadOnlyList<TurnPackingJournalEntry> entries, GameScope? scope = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
        {
            throw new InvalidDataException("A packing journal must begin with a Start entry.");
        }
        TurnPackingPeriod? period = null;
        TurnPackingJournalEntry? prior = null;
        var eventIds = new HashSet<string>(StringComparer.Ordinal);
        var steps = new HashSet<string>(StringComparer.Ordinal);
        var receipts = new List<TurnPackingReceipt>();
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ValidateEnvelope(entry, prior, eventIds);
            if (steps.Contains(entry.EventId))
            {
                throw new InvalidDataException("A packing action cannot reuse a work-step event identity.");
            }
            var newStep = prior is null || entry.Operation == TurnPackingOperation.Rally;
            if (newStep)
            {
                if (!steps.Add(entry.WorkStep.EventId) || eventIds.Contains(entry.WorkStep.EventId) || prior is not null && entry.WorkStep.Revision <= prior.Revision)
                {
                    throw new InvalidDataException("A new packing work boundary needs a new, later source event.");
                }
            }
            else if (entry.WorkStep != prior!.WorkStep)
            {
                throw new InvalidDataException("A packing action cannot replace its recorded work-step origin.");
            }
            if (entry.Operation is TurnPackingOperation.Start or TurnPackingOperation.RecoveryStart)
            {
                if (entry.Casualty is not null || period is not null || entry.Specification is null || entry.Direction is null || entry.Eligibility is null)
                {
                    throw new InvalidDataException("Start binds the specification, direction and eligibility exactly once.");
                }
                if (entry.Operation == TurnPackingOperation.RecoveryStart)
                {
                    if (scope is null || entry.Recovery is null || entry.Direction != TurnPackingDirection.Unpack)
                    {
                        throw new InvalidDataException("Recovery start requires the game scope and its bound casualty journal.");
                    }
                    var recovery = TurnPackingCasualtyJournal.Read(JsonSerializer.Serialize(entry.Recovery, JsonSerializerOptions.Web), scope);
                    var history = TurnPackingCasualtyJournal.Replay(recovery.Origin, recovery.Entries);
                    if (entry.Specification.Owner != history.Origin.Owner || entry.Specification.Actors != history.Origin.Actors
                        || entry.WorkStep.Revision <= history.Receipts[^1].Entry.Revision
                        || entry.Specification.Id == history.Origin.ExistingPeriodId)
                    {
                        throw new InvalidDataException("Recovery binds the same actors and owner to a new period after its casualty effects.");
                    }
                    IEnumerable<string> sources = [history.Origin.Source.EventId, .. history.Receipts.SelectMany(item => new[]
                        { item.Entry.Casualty.EventId, item.Entry.Roll.EventId, item.Entry.EventId })];
                    foreach (var source in sources)
                    {
                        if (steps.Contains(source) || !eventIds.Add(source))
                        {
                            throw new InvalidDataException("Recovery cannot reuse a casualty source as a work step or period event.");
                        }
                    }
                    period = TurnPackingPeriods.StartRecovery(entry.Specification, history.Current, entry.WorkStep.Rally, entry.Eligibility);
                }
                else
                {
                    if (entry.Recovery is not null)
                    {
                        throw new InvalidDataException("An ordinary Start cannot import a casualty recovery.");
                    }
                    period = TurnPackingPeriods.Start(entry.Specification, entry.Direction.Value, entry.WorkStep.Rally, entry.Eligibility);
                }
            }
            else
            {
                if (period is null || entry.Specification is not null || entry.Direction is not null || entry.Recovery is not null
                    || (entry.Operation == TurnPackingOperation.Resume) != (entry.Eligibility is not null)
                    || (entry.Operation == TurnPackingOperation.Casualty) != (entry.Casualty is not null))
                {
                    throw new InvalidDataException("Packing changes retain their original actors and specification; only Resume supplies fresh eligibility.");
                }
                period = entry.Operation switch
                {
                    TurnPackingOperation.Casualty => ApplyCasualty(period, entry, prior!, scope, eventIds, steps),
                    TurnPackingOperation.Rally => TurnPackingPeriods.NextRally(period, entry.WorkStep.Rally),
                    TurnPackingOperation.Midpoint => TurnPackingPeriods.ApplyMidpoint(period),
                    TurnPackingOperation.Interrupt => TurnPackingPeriods.Interrupt(period),
                    TurnPackingOperation.Resume => TurnPackingPeriods.Resume(period, entry.Eligibility!),
                    TurnPackingOperation.Reverse => TurnPackingPeriods.Reverse(period),
                    TurnPackingOperation.Complete => TurnPackingPeriods.Complete(period),
                    _ => throw new InvalidDataException("Unsupported packing operation.")
                };
            }
            receipts.Add(new(entry.EventId, entry.Revision, entry.Operation, entry.WorkStep));
            prior = entry;
        }
        return new(period!, receipts.AsReadOnly());
    }

    private static TurnPackingPeriod ApplyCasualty(TurnPackingPeriod period, TurnPackingJournalEntry entry,
        TurnPackingJournalEntry prior, GameScope? scope, HashSet<string> eventIds, HashSet<string> steps)
    {
        if (scope is null)
        {
            throw new InvalidDataException("A period casualty requires its game scope.");
        }
        var document = TurnPackingCasualtyJournal.Read(JsonSerializer.Serialize(entry.Casualty, JsonSerializerOptions.Web), scope);
        var history = TurnPackingCasualtyJournal.Replay(document.Origin, document.Entries);
        if (history.Origin.Owner != period.Specification.Owner || history.Origin.Actors != period.Specification.Actors
            || history.Origin.ExistingPeriodId != period.Specification.Id
            || history.Origin.Source != new TurnPackingEvidenceSource(prior.EventId, prior.Revision)
            || history.Receipts[^1].Entry.Revision >= entry.Revision)
        {
            throw new InvalidDataException("A casualty must bind this period's owner, actors and immediately preceding state revision.");
        }
        var state = history.Origin.State;
        foreach (var receipt in history.Receipts)
        {
            var casualty = receipt.Entry;
            string[] sources = [casualty.Casualty.EventId, casualty.Roll.EventId, casualty.EventId];
            foreach (var source in sources)
            {
                if (steps.Contains(source) || !eventIds.Add(source))
                {
                    throw new InvalidDataException("Period casualties cannot reuse an earlier casualty, die, work step or period event.");
                }
            }
            period = TurnPackingPeriods.ApplyUnloadedCasualty(period, state, casualty.MuleLoss, casualty.OriginalDr, casualty.Terrain);
            state = period.LatestCasualty!;
        }
        return period;
    }

    private static void ValidateScope(GameScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.Tenant == Guid.Empty || string.IsNullOrWhiteSpace(scope.Game))
        {
            throw new InvalidDataException("Packing history requires a tenant and game identity.");
        }
    }

    private static void ValidateEnvelope(TurnPackingJournalEntry entry, TurnPackingJournalEntry? prior, HashSet<string> ids)
    {
        if (entry.Version != Version || !Enum.IsDefined(entry.Operation) || string.IsNullOrWhiteSpace(entry.EventId)
            || !ids.Add(entry.EventId) || entry.Revision < 1 || prior is not null && entry.Revision <= prior.Revision
            || entry.PreviousEventId != prior?.EventId)
        {
            throw new InvalidDataException("Invalid packing journal version, event identity, revision or predecessor.");
        }
        if (entry.WorkStep is not { Rally: { } rally } step || string.IsNullOrWhiteSpace(step.EventId)
            || step.Revision < 1 || step.Revision >= entry.Revision || rally.GameTurn < 1
            || step.EventId == entry.EventId)
        {
            throw new InvalidDataException("Packing entries require an earlier, identified RPh work-step source.");
        }
    }
}
